// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation.Configuration;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Security;
using System.Management.Automation.Tracing;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Text;
#if LEGACYTELEMETRY
using Microsoft.PowerShell.Telemetry.Internal;
#endif

namespace System.Management.Automation
{
    internal enum CompileInterpretChoice
    {
        NeverCompile,
        AlwaysCompile,
        CompileOnDemand
    }

    internal enum ScriptBlockClauseToInvoke
    {
        Begin,
        Process,
        End,
        Clean,
        ProcessBlockOnly,
    }

    internal class CompiledScriptBlockData
    {
        internal CompiledScriptBlockData(IParameterMetadataProvider ast, bool isFilter)
        {
            _ast = ast;
            this.IsFilter = isFilter;
            this.Id = Guid.NewGuid();
        }

        internal CompiledScriptBlockData(string scriptText, bool isProductCode)
        {
            _isProductCode = isProductCode;
            _scriptText = scriptText;
            this.Id = Guid.NewGuid();
        }

        internal bool Compile(bool optimized)
        {
            if (_attributes == null)
            {
                InitializeMetadata();
            }

            // We need the name to index map to check if any allscope variables are assigned.  If they
            // are, we can't run the optimized version, so we'll compile once more unoptimized and run that.
            if (optimized && NameToIndexMap == null)
            {
                CompileOptimized();
            }

            optimized = optimized && !VariableAnalysis.AnyVariablesCouldBeAllScope(NameToIndexMap);

            if (!optimized && !_compiledUnoptimized)
            {
                CompileUnoptimized();
            }
            else if (optimized && !_compiledOptimized)
            {
                CompileOptimized();
            }

            return optimized;
        }

        private void InitializeMetadata()
        {
            lock (this)
            {
                if (_attributes != null)
                {
                    // Another thread must have initialized the metadata.
                    return;
                }

                Attribute[] attributes;
                CmdletBindingAttribute cmdletBindingAttribute = null;
                if (!Ast.HasAnyScriptBlockAttributes())
                {
                    attributes = Array.Empty<Attribute>();
                }
                else
                {
                    attributes = Ast.GetScriptBlockAttributes().ToArray();
                    foreach (var attribute in attributes)
                    {
                        if (attribute is CmdletBindingAttribute c)
                        {
                            cmdletBindingAttribute ??= c;
                        }
                        else if (attribute is DebuggerHiddenAttribute)
                        {
                            DebuggerHidden = true;
                        }
                        else if (attribute is DebuggerStepThroughAttribute || attribute is DebuggerNonUserCodeAttribute)
                        {
                            DebuggerStepThrough = true;
                        }
                    }

                    _usesCmdletBinding = cmdletBindingAttribute != null;
                }

                bool automaticPosition = cmdletBindingAttribute == null || cmdletBindingAttribute.PositionalBinding;
                var runtimeDefinedParameterDictionary =
                    Ast.GetParameterMetadata(automaticPosition, ref _usesCmdletBinding);

                // Initialize these fields last - if there were any exceptions, we don't want the partial results cached.
                _attributes = attributes;
                _runtimeDefinedParameterDictionary = runtimeDefinedParameterDictionary;
            }
        }

        private void CompileUnoptimized()
        {
            lock (this)
            {
                if (_compiledUnoptimized)
                {
                    // Another thread must have compiled while we were waiting on the lock.
                    return;
                }

                ReallyCompile(false);
                _compiledUnoptimized = true;
            }
        }

        private void CompileOptimized()
        {
            lock (this)
            {
                if (_compiledOptimized)
                {
                    // Another thread must have compiled while we were waiting on the lock.
                    return;
                }

                ReallyCompile(true);
                _compiledOptimized = true;
            }
        }

        private void ReallyCompile(bool optimize)
        {
#if LEGACYTELEMETRY
            var sw = new Stopwatch();
            sw.Start();
#endif
            bool etwEnabled = ParserEventSource.Log.IsEnabled();
            if (etwEnabled)
            {
                var extent = _ast.Body.Extent;
                var text = extent.Text;
                ParserEventSource.Log.CompileStart(
                    FileName: ParserEventSource.GetFileOrScript(extent.File, text),
                    text.Length,
                    optimize);
            }

            PerformSecurityChecks();

            Compiler compiler = new Compiler();
            compiler.Compile(this, optimize);

#if LEGACYTELEMETRY
            if (!IsProductCode)
            {
                TelemetryAPI.ReportScriptTelemetry((Ast)_ast, !optimize, sw.ElapsedMilliseconds);
            }
#endif
            if (etwEnabled)
            {
                ParserEventSource.Log.CompileStop();
            }
        }

        private void PerformSecurityChecks()
        {
            if (!(Ast is ScriptBlockAst scriptBlockAst))
            {
                // Checks are only needed at the top level.
                return;
            }

            var scriptExtent = scriptBlockAst.Extent;
            var scriptFile = scriptExtent.File;

            if (scriptFile != null
                && scriptFile.EndsWith(StringLiterals.PowerShellDataFileExtension, StringComparison.OrdinalIgnoreCase)
                && IsScriptBlockInFactASafeHashtable())
            {
                // Skip the scan for .psd1 files if their content is in fact a safe HashtableAst.
                return;
            }

            // Call the AMSI API to determine if the script block has malicious content
            var amsiResult = AmsiUtils.ScanContent(scriptExtent.Text, scriptFile);

            if (amsiResult == AmsiUtils.AmsiNativeMethods.AMSI_RESULT.AMSI_RESULT_DETECTED)
            {
                var parseError = new ParseError(
                    scriptExtent,
                    "ScriptContainedMaliciousContent",
                    ParserStrings.ScriptContainedMaliciousContent);
                throw new ParseException(new[] { parseError });
            }
            else if (amsiResult >= AmsiUtils.AmsiNativeMethods.AMSI_RESULT.AMSI_RESULT_BLOCKED_BY_ADMIN_BEGIN
                && amsiResult <= AmsiUtils.AmsiNativeMethods.AMSI_RESULT.AMSI_RESULT_BLOCKED_BY_ADMIN_END)
            {
                // Certain policies set by an administrator blocked this content on this machine
                var parseError = new ParseError(
                    scriptExtent,
                    "ScriptHasAdminBlockedContent",
                    StringUtil.Format(ParserStrings.ScriptHasAdminBlockedContent, amsiResult));
                throw new ParseException(new[] { parseError });
            }

            if (ScriptBlock.CheckSuspiciousContent(scriptBlockAst) != null)
            {
                HasSuspiciousContent = true;
            }

            // A local function to check if the ScriptBlockAst is in fact a safe HashtableAst.
            bool IsScriptBlockInFactASafeHashtable()
            {
                // NOTE: The code below depends on the current member structure of 'ScriptBlockAst'
                // to determine if the ScriptBlockAst is in fact just a HashtableAst. If AST types
                // are enhanced, such as new members added to 'ScriptBlockAst', the code here needs
                // to be reviewed and changed accordingly.

                if (scriptBlockAst.BeginBlock != null
                    || scriptBlockAst.ProcessBlock != null
                    || scriptBlockAst.CleanBlock != null
                    || scriptBlockAst.ParamBlock != null
                    || scriptBlockAst.DynamicParamBlock != null
                    || scriptBlockAst.ScriptRequirements != null
                    || scriptBlockAst.UsingStatements.Count > 0
                    || scriptBlockAst.Attributes.Count > 0)
                {
                    return false;
                }

                NamedBlockAst endBlock = scriptBlockAst.EndBlock;
                if (!endBlock.Unnamed || endBlock.Traps != null || endBlock.Statements.Count != 1)
                {
                    return false;
                }

                if (!(endBlock.Statements[0] is PipelineAst pipelineAst))
                {
                    return false;
                }

                if (!(pipelineAst.GetPureExpression() is HashtableAst hashtableAst))
                {
                    return false;
                }

                // After the above steps, we know the ScriptBlockAst is in fact just a HashtableAst,
                // now we need to check if the HashtableAst is safe.
                return IsSafeValueVisitor.Default.IsAstSafe(hashtableAst);
            }
        }

        // We delay parsing scripts loaded on startup, so we save the text.
        private string _scriptText;

        internal IParameterMetadataProvider Ast { get => _ast ?? DelayParseScriptText(); }

        private IParameterMetadataProvider _ast;

        private IParameterMetadataProvider DelayParseScriptText()
        {
            lock (this)
            {
                if (_ast != null)
                {
                    return _ast;
                }

                ParseError[] errors;
                _ast = (new Parser()).Parse(null, _scriptText, null, out errors, ParseMode.Default);
                if (errors.Length != 0)
                {
                    throw new ParseException(errors);
                }

                _scriptText = null;
                return _ast;
            }
        }

        internal Type LocalsMutableTupleType { get; set; }

        internal Type UnoptimizedLocalsMutableTupleType { get; set; }

        internal Func<MutableTuple> LocalsMutableTupleCreator { get; set; }

        internal Func<MutableTuple> UnoptimizedLocalsMutableTupleCreator { get; set; }

        internal Dictionary<string, int> NameToIndexMap { get; set; }

        #region Named Blocks

        internal Action<FunctionContext> DynamicParamBlock { get; set; }

        internal Action<FunctionContext> UnoptimizedDynamicParamBlock { get; set; }

        internal Action<FunctionContext> BeginBlock { get; set; }

        internal Action<FunctionContext> UnoptimizedBeginBlock { get; set; }

        internal Action<FunctionContext> ProcessBlock { get; set; }

        internal Action<FunctionContext> UnoptimizedProcessBlock { get; set; }

        internal Action<FunctionContext> EndBlock { get; set; }

        internal Action<FunctionContext> UnoptimizedEndBlock { get; set; }

        internal Action<FunctionContext> CleanBlock { get; set; }

        internal Action<FunctionContext> UnoptimizedCleanBlock { get; set; }

        #endregion Named Blocks

        internal IScriptExtent[] SequencePoints { get; set; }

        private RuntimeDefinedParameterDictionary _runtimeDefinedParameterDictionary;
        private Attribute[] _attributes;
        private bool _usesCmdletBinding;
        private bool _compiledOptimized;
        private bool _compiledUnoptimized;
        private bool _hasSuspiciousContent;
        private bool? _isProductCode;

        internal bool DebuggerHidden { get; set; }

        internal bool DebuggerStepThrough { get; set; }

        internal Guid Id { get; private set; }

        internal bool HasLogged { get; set; }

        internal bool SkipLogging { get; set; }

        internal bool IsFilter { get; }

        internal bool IsProductCode
        {
            get
            {
                _isProductCode ??= SecuritySupport.IsProductBinary(((Ast)_ast).Extent.File);

                return _isProductCode.Value;
            }
        }

        internal bool GetIsConfiguration()
        {
            // Use _ast instead of Ast
            //  If we access Ast, we may parse a "delay parsed" script block unnecessarily
            //  if _ast is null - it can't be a configuration as there is no way to create a configuration that way
            var scriptBlockAst = _ast as ScriptBlockAst;
            return scriptBlockAst != null && scriptBlockAst.IsConfiguration;
        }

        internal bool HasSuspiciousContent
        {
            get
            {
                Diagnostics.Assert(
                    _compiledOptimized || _compiledUnoptimized,
                    "HasSuspiciousContent is not set correctly before being compiled");
                return _hasSuspiciousContent;
            }

            set => _hasSuspiciousContent = value;
        }

        private MergedCommandParameterMetadata _parameterMetadata;

        internal List<Attribute> GetAttributes()
        {
            if (_attributes == null)
            {
                InitializeMetadata();
            }

            Diagnostics.Assert(
                _attributes != null,
                "after initialization, attributes is never null, must be an empty list if no attributes.");
            return _attributes.ToList();
        }

        internal bool UsesCmdletBinding
        {
            get
            {
                if (_attributes != null)
                {
                    return _usesCmdletBinding;
                }

                return Ast.UsesCmdletBinding();
            }
        }

        internal RuntimeDefinedParameterDictionary RuntimeDefinedParameters
        {
            get
            {
                if (_runtimeDefinedParameterDictionary == null)
                {
                    InitializeMetadata();
                }

                return _runtimeDefinedParameterDictionary;
            }
        }

        internal CmdletBindingAttribute CmdletBindingAttribute
        {
            get
            {
                if (_runtimeDefinedParameterDictionary == null)
                {
                    InitializeMetadata();
                }

                return _usesCmdletBinding
                    ? (CmdletBindingAttribute)Array.Find(_attributes, static attr => attr is CmdletBindingAttribute)
                    : null;
            }
        }

        internal ObsoleteAttribute ObsoleteAttribute
        {
            get
            {
                if (_runtimeDefinedParameterDictionary == null)
                {
                    InitializeMetadata();
                }

                return (ObsoleteAttribute)Array.Find(_attributes, static attr => attr is ObsoleteAttribute);
            }
        }

        internal ExperimentalAttribute ExperimentalAttribute
        {
            get
            {
                if (_expAttribute == ExperimentalAttribute.None)
                {
                    lock (this)
                    {
                        if (_expAttribute == ExperimentalAttribute.None)
                        {
                            _expAttribute = Ast.GetExperimentalAttributes().FirstOrDefault();
                        }
                    }
                }

                return _expAttribute;
            }
        }

        private ExperimentalAttribute _expAttribute = ExperimentalAttribute.None;

        public MergedCommandParameterMetadata GetParameterMetadata(ScriptBlock scriptBlock)
        {
            if (_parameterMetadata == null)
            {
                lock (this)
                {
                    if (_parameterMetadata == null)
                    {
                        CommandMetadata metadata = new CommandMetadata(
                            scriptBlock,
                            string.Empty,
                            LocalPipeline.GetExecutionContextFromTLS());
                        _parameterMetadata = metadata.StaticCommandParameterMetadata;
                    }
                }
            }

            return _parameterMetadata;
        }

        public override string ToString()
        {
            if (_scriptText != null)
            {
                return _scriptText;
            }

            if (_ast is ScriptBlockAst sbAst)
            {
                return sbAst.ToStringForSerialization();
            }

            var generatedMemberFunctionAst = _ast as CompilerGeneratedMemberFunctionAst;
            if (generatedMemberFunctionAst != null)
            {
                return generatedMemberFunctionAst.Extent.Text;
            }

            var funcDefn = (FunctionDefinitionAst)_ast;
            if (funcDefn.Parameters == null)
            {
                return funcDefn.Body.ToStringForSerialization();
            }

            var sb = new StringBuilder();
            sb.Append(funcDefn.GetParamTextFromParameterList());
            sb.Append(funcDefn.Body.ToStringForSerialization());
            return sb.ToString();
        }
    }

    [Serializable]
    public partial class ScriptBlock : ISerializable
    {
        private readonly CompiledScriptBlockData _scriptBlockData;

        internal ScriptBlock(IParameterMetadataProvider ast, bool isFilter)
            : this(new CompiledScriptBlockData(ast, isFilter))
        {
        }

        private ScriptBlock(CompiledScriptBlockData scriptBlockData)
        {
            _scriptBlockData = scriptBlockData;

            // LanguageMode is a nullable PSLanguageMode enumeration because script blocks
            // need to inherit the language mode from the context in which they are executing.
            // We can't assume FullLanguage by default when there is no context, as there are
            // script blocks (such as the script blocks used in Workflow activities) that are
            // created by the host without a "current language mode" to inherit. They ultimately
            // get their language mode set when they are finally invoked in a constrained
            // language runspace.
            // Script blocks that should always be run under FullLanguage mode (i.e.: set in
            // InitialSessionState, etc.) should explicitly set the LanguageMode to FullLanguage
            // when they are created.
            ExecutionContext context = LocalPipeline.GetExecutionContextFromTLS();
            if (context != null)
            {
                this.LanguageMode = context.LanguageMode;
            }
        }

        /// <summary>
        /// Protected constructor to support ISerializable.
        /// </summary>
        protected ScriptBlock(SerializationInfo info, StreamingContext context)
        {
        }

        private static readonly ConcurrentDictionary<Tuple<string, string>, ScriptBlock> s_cachedScripts =
            new ConcurrentDictionary<Tuple<string, string>, ScriptBlock>();

        internal static ScriptBlock TryGetCachedScriptBlock(string fileName, string fileContents)
        {
            if (InternalTestHooks.IgnoreScriptBlockCache)
            {
                return null;
            }

            ScriptBlock scriptBlock;
            var key = Tuple.Create(fileName, fileContents);
            if (s_cachedScripts.TryGetValue(key, out scriptBlock))
            {
                Diagnostics.Assert(
                    scriptBlock.SessionStateInternal == null,
                    "A cached scriptblock should not have it's session state bound, that causes a memory leak.");
                return scriptBlock.Clone();
            }

            return null;
        }

        private static bool IsDynamicKeyword(Ast ast)
            => ast is CommandAst cmdAst && cmdAst.DefiningKeyword != null;

        private static bool IsUsingTypes(Ast ast)
            => ast is UsingStatementAst cmdAst && cmdAst.IsUsingModuleOrAssembly();

        internal static void CacheScriptBlock(ScriptBlock scriptBlock, string fileName, string fileContents)
        {
            if (InternalTestHooks.IgnoreScriptBlockCache)
            {
                return;
            }

            // Don't cache scriptblocks that have
            // a) dynamic keywords
            // b) 'using module' or 'using assembly'
            // The definition of the dynamic keyword could change, consequently changing how the source text should be parsed.
            // Exported types definitions from 'using module' could change, we need to do all parse-time checks again.
            // TODO(sevoroby): we can optimize it to ignore 'using' if there are no actual type usage in locally defined types.

            // using is always a top-level statements in scriptBlock, we don't need to search in child blocks.
            if (scriptBlock.Ast.Find(static ast => IsUsingTypes(ast), false) != null
                || scriptBlock.Ast.Find(static ast => IsDynamicKeyword(ast), true) != null)
            {
                return;
            }

            if (s_cachedScripts.Count > 1024)
            {
                s_cachedScripts.Clear();
            }

            var key = Tuple.Create(fileName, fileContents);
            s_cachedScripts.TryAdd(key, scriptBlock);
        }

        /// <summary>
        /// Clears the cached scriptblocks.
        /// </summary>
        internal static void ClearScriptBlockCache()
        {
            s_cachedScripts.Clear();
        }

        internal static readonly ScriptBlock EmptyScriptBlock =
            ScriptBlock.CreateDelayParsedScriptBlock(string.Empty, isProductCode: true);

        internal static ScriptBlock Create(Parser parser, string fileName, string fileContents)
        {
            var scriptBlock = TryGetCachedScriptBlock(fileName, fileContents);
            if (scriptBlock != null)
            {
                return scriptBlock;
            }

            var ast = parser.Parse(fileName, fileContents, null, out ParseError[] errors, ParseMode.Default);
            if (errors.Length != 0)
            {
                throw new ParseException(errors);
            }

            var result = new ScriptBlock(ast, isFilter: false);
            CacheScriptBlock(result, fileName, fileContents);

            // The value returned will potentially be bound to a session state.  We don't want
            // the cached script block to end up being bound to any session state, so clone
            // the return value to ensure the cached value has no session state.
            return result.Clone();
        }

        internal ScriptBlock Clone() => new ScriptBlock(_scriptBlockData);

        /// <summary>
        /// Returns the text of the script block.  The return value might not match the original text exactly.
        /// </summary>
        public override string ToString() => _scriptBlockData.ToString();

        /// <summary>
        /// Returns the text of the script block with the handling of $using expressions.
        /// </summary>
        internal string ToStringWithDollarUsingHandling(
            Tuple<List<VariableExpressionAst>, string> usingVariablesTuple)
        {
            FunctionDefinitionAst funcDefn = null;
            var sbAst = Ast as ScriptBlockAst;
            if (sbAst == null)
            {
                funcDefn = (FunctionDefinitionAst)Ast;
                sbAst = funcDefn.Body;
            }

            string sbText = sbAst.ToStringForSerialization(usingVariablesTuple, sbAst.Extent.StartOffset, sbAst.Extent.EndOffset);
            if (sbAst.ParamBlock != null)
            {
                return sbText;
            }

            string paramText;
            string additionalNewParams = usingVariablesTuple.Item2;
            if (funcDefn == null || funcDefn.Parameters == null)
            {
                paramText = "param(" + additionalNewParams + ")" + Environment.NewLine;
            }
            else
            {
                paramText = funcDefn.GetParamTextFromParameterList(usingVariablesTuple);
            }

            sbText = paramText + sbText;
            return sbText;
        }

        /// <summary>
        /// Support for <see cref="ISerializable"/>.
        /// </summary>
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(info));
            }

            string serializedContent = this.ToString();
            info.AddValue("ScriptText", serializedContent);
            info.SetType(typeof(ScriptBlockSerializationHelper));
        }

        internal PowerShell GetPowerShellImpl(
            ExecutionContext context,
            Dictionary<string, object> variables,
            bool isTrustedInput,
            bool filterNonUsingVariables,
            bool? createLocalScope,
            params object[] args)
        {
            return AstInternal.GetPowerShell(
                context,
                variables,
                isTrustedInput,
                filterNonUsingVariables,
                createLocalScope,
                args);
        }

        internal SteppablePipeline GetSteppablePipelineImpl(CommandOrigin commandOrigin, object[] args)
        {
            var pipelineAst = GetSimplePipeline(
                resourceString => throw PSTraceSource.NewInvalidOperationException(resourceString));
            Diagnostics.Assert(pipelineAst != null, "This should be checked by GetSimplePipeline");

            if (pipelineAst.PipelineElements[0] is not CommandAst)
            {
                throw PSTraceSource.NewInvalidOperationException(AutomationExceptions.CantConvertEmptyPipeline);
            }

            return PipelineOps.GetSteppablePipeline(pipelineAst, commandOrigin, this, args);
        }

        private PipelineAst GetSimplePipeline(Func<string, PipelineAst> errorHandler)
        {
            errorHandler ??= (static _ => null);

            if (HasBeginBlock || HasProcessBlock || HasCleanBlock)
            {
                return errorHandler(AutomationExceptions.CanConvertOneClauseOnly);
            }

            var ast = AstInternal;
            var statements = ast.Body.EndBlock.Statements;
            if (statements.Count == 0)
            {
                return errorHandler(AutomationExceptions.CantConvertEmptyPipeline);
            }

            if (statements.Count > 1)
            {
                return errorHandler(AutomationExceptions.CanOnlyConvertOnePipeline);
            }

            if (ast.Body.EndBlock.Traps != null && ast.Body.EndBlock.Traps.Count > 0)
            {
                return errorHandler(AutomationExceptions.CantConvertScriptBlockWithTrap);
            }

            if (!(statements[0] is PipelineAst pipeAst))
            {
                return errorHandler(AutomationExceptions.CanOnlyConvertOnePipeline);
            }

            // The old code checked for empty pipeline.
            // That can't happen in the new parser (validated in the constructors),
            // so the resource CantConvertEmptyPipeline is probably unused.

            return pipeAst;
        }

        internal List<Attribute> GetAttributes() => _scriptBlockData.GetAttributes();

        internal string GetFileName() => AstInternal.Body.Extent.File;

        // GetAttributes() is asserted never return null
        internal bool IsMetaConfiguration() => GetAttributes().OfType<DscLocalConfigurationManagerAttribute>().Any();

        internal PSToken GetStartPosition() => new PSToken(Ast.Extent);

        internal MergedCommandParameterMetadata ParameterMetadata
        {
            get => _scriptBlockData.GetParameterMetadata(this);
        }

        internal bool UsesCmdletBinding { get => _scriptBlockData.UsesCmdletBinding; }

        internal bool HasDynamicParameters { get => AstInternal.Body.DynamicParamBlock != null; }

        /// <summary>
        /// DebuggerHidden.
        /// </summary>
        public bool DebuggerHidden
        {
            get => _scriptBlockData.DebuggerHidden;
            set => _scriptBlockData.DebuggerHidden = value;
        }

        /// <summary>
        /// The unique ID of this script block.
        /// </summary>
        public Guid Id { get => _scriptBlockData.Id; }

        internal bool DebuggerStepThrough
        {
            get { return _scriptBlockData.DebuggerStepThrough; }

            set { _scriptBlockData.DebuggerStepThrough = value; }
        }

        internal RuntimeDefinedParameterDictionary RuntimeDefinedParameters
        {
            get => _scriptBlockData.RuntimeDefinedParameters;
        }

        internal bool HasLogged
        {
            get => _scriptBlockData.HasLogged;
            set => _scriptBlockData.HasLogged = value;
        }

        internal bool SkipLogging
        {
            get { return _scriptBlockData.SkipLogging; }

            set { _scriptBlockData.SkipLogging = value; }
        }

        internal Assembly AssemblyDefiningPSTypes { get; set; }

        internal HelpInfo GetHelpInfo(
            ExecutionContext context,
            CommandInfo commandInfo,
            bool dontSearchOnRemoteComputer,
            Dictionary<Ast, Token[]> scriptBlockTokenCache,
            out string helpFile,
            out string helpUriFromDotLink)
        {
            helpUriFromDotLink = null;

            var commentTokens = HelpCommentsParser.GetHelpCommentTokens(AstInternal, scriptBlockTokenCache);
            if (commentTokens != null)
            {
                return HelpCommentsParser.CreateFromComments(
                    context,
                    commandInfo,
                    commentTokens.Item1,
                    commentTokens.Item2,
                    dontSearchOnRemoteComputer,
                    out helpFile,
                    out helpUriFromDotLink);
            }

            helpFile = null;
            return null;
        }

        /// <summary>
        /// Check the script block to see if it uses any language constructs not allowed in restricted language mode.
        /// </summary>
        /// <param name="allowedCommands">The commands that are allowed.</param>
        /// <param name="allowedVariables">
        /// The variables allowed in this scriptblock. If this is null, then the default variable set
        /// will be allowed. If it is an empty list, no variables will be allowed. If it is "*" then
        /// any variable will be allowed.
        /// </param>
        /// <param name="allowEnvironmentVariables">The environment variables that are allowed.</param>
        public void CheckRestrictedLanguage(
            IEnumerable<string> allowedCommands,
            IEnumerable<string> allowedVariables,
            bool allowEnvironmentVariables)
        {
            Parser parser = new Parser();

            var ast = AstInternal;
            if (HasBeginBlock
                || HasProcessBlock
                || HasCleanBlock
                || ast.Body.ParamBlock is not null)
            {
                Ast errorAst = ast.Body.BeginBlock ?? (Ast)ast.Body.ProcessBlock ?? ast.Body.ParamBlock;
                parser.ReportError(
                    errorAst.Extent,
                    nameof(ParserStrings.InvalidScriptBlockInDataSection),
                    ParserStrings.InvalidScriptBlockInDataSection);
            }

            if (HasEndBlock)
            {
                var rlc = new RestrictedLanguageChecker(
                    parser,
                    allowedCommands,
                    allowedVariables,
                    allowEnvironmentVariables);

                StatementBlockAst.InternalVisit(
                    rlc,
                    ast.Body.EndBlock.Traps,
                    ast.Body.EndBlock.Statements,
                    AstVisitAction.Continue);
            }

            if (parser.ErrorList.Count > 0)
            {
                throw new ParseException(parser.ErrorList.ToArray());
            }
        }

        internal string GetWithInputHandlingForInvokeCommand() => AstInternal.GetWithInputHandlingForInvokeCommand();

        internal string GetWithInputHandlingForInvokeCommandWithUsingExpression(
            Tuple<List<VariableExpressionAst>, string> usingVariablesTuple)
        {
            Tuple<string, string> result =
                AstInternal.GetWithInputHandlingForInvokeCommandWithUsingExpression(usingVariablesTuple);

            // result.Item1 is ParamText; result.Item2 is ScriptBlockText
            return result.Item1 == null ? result.Item2 : result.Item1 + result.Item2;
        }

        internal bool IsUsingDollarInput() => AstSearcher.IsUsingDollarInput(this.Ast);

        internal void InvokeWithPipeImpl(
            bool createLocalScope,
            Dictionary<string, ScriptBlock> functionsToDefine,
            List<PSVariable> variablesToDefine,
            ErrorHandlingBehavior errorHandlingBehavior,
            object dollarUnder,
            object input,
            object scriptThis,
            Pipe outputPipe,
            InvocationInfo invocationInfo,
            params object[] args)
        {
            InvokeWithPipeImpl(
                ScriptBlockClauseToInvoke.ProcessBlockOnly,
                createLocalScope,
                functionsToDefine,
                variablesToDefine,
                errorHandlingBehavior,
                dollarUnder,
                input,
                scriptThis,
                outputPipe,
                invocationInfo,
                args);
        }

        internal void InvokeWithPipeImpl(
            ScriptBlockClauseToInvoke clauseToInvoke,
            bool createLocalScope,
            Dictionary<string, ScriptBlock> functionsToDefine,
            List<PSVariable> variablesToDefine,
            ErrorHandlingBehavior errorHandlingBehavior,
            object dollarUnder,
            object input,
            object scriptThis,
            Pipe outputPipe,
            InvocationInfo invocationInfo,
            params object[] args)
        {
            if (clauseToInvoke == ScriptBlockClauseToInvoke.Clean)
            {
                throw new PSNotSupportedException(ParserStrings.InvokingCleanBlockNotSupported);
            }

            if ((clauseToInvoke == ScriptBlockClauseToInvoke.Begin && !HasBeginBlock)
                || (clauseToInvoke == ScriptBlockClauseToInvoke.Process && !HasProcessBlock)
                || (clauseToInvoke == ScriptBlockClauseToInvoke.End && !HasEndBlock))
            {
                return;
            }

            ExecutionContext context = GetContextFromTLS();
            Diagnostics.Assert(
                SessionStateInternal == null || SessionStateInternal.ExecutionContext == context,
                "The scriptblock is being invoked in a runspace different than the one where it was created");

            if (context.CurrentPipelineStopping)
            {
                throw new PipelineStoppedException();
            }

            // Validate that the arguments are consistent. The only public API that gets you here never sets createLocalScope to false...
            Diagnostics.Assert(
                createLocalScope || functionsToDefine == null,
                "When calling ScriptBlock.InvokeWithContext(), if 'functionsToDefine' != null then 'createLocalScope' must be true");
            Diagnostics.Assert(
                createLocalScope || variablesToDefine == null,
                "When calling ScriptBlock.InvokeWithContext(), if 'variablesToDefine' != null then 'createLocalScope' must be true");

            args ??= Array.Empty<object>();

            bool runOptimized = context._debuggingMode <= 0 && createLocalScope;
            var codeToInvoke = GetCodeToInvoke(ref runOptimized, clauseToInvoke);
            if (codeToInvoke == null)
            {
                return;
            }

            // If we don't have a pipe to write to, we need to discard all results.
            outputPipe ??= new Pipe { NullPipe = true };

            var locals = MakeLocalsTuple(runOptimized);

            if (dollarUnder != AutomationNull.Value)
            {
                locals.SetAutomaticVariable(AutomaticVariable.Underbar, dollarUnder, context);
            }

            if (input != AutomationNull.Value)
            {
                locals.SetAutomaticVariable(AutomaticVariable.Input, input, context);
            }

            if (scriptThis != AutomationNull.Value)
            {
                locals.SetAutomaticVariable(AutomaticVariable.This, scriptThis, context);
            }

            SetPSScriptRootAndPSCommandPath(locals, context);

            var oldShellFunctionErrorOutputPipe = context.ShellFunctionErrorOutputPipe;
            var oldExternalErrorOutput = context.ExternalErrorOutput;
            var oldScopeOrigin = context.EngineSessionState.CurrentScope.ScopeOrigin;
            var oldSessionState = context.EngineSessionState;

            // If the script block has a different language mode than the current context,
            // change the language mode.
            PSLanguageMode? oldLanguageMode = null;
            PSLanguageMode? newLanguageMode = null;
            if (this.LanguageMode.HasValue && this.LanguageMode != context.LanguageMode)
            {
                // Don't allow context: ConstrainedLanguage -> FullLanguage transition if
                // this is dot sourcing into the current scope, unless it is within a trusted module scope.
                if (this.LanguageMode != PSLanguageMode.FullLanguage
                    || createLocalScope
                    || context.EngineSessionState.Module?.LanguageMode == PSLanguageMode.FullLanguage)
                {
                    oldLanguageMode = context.LanguageMode;
                    newLanguageMode = this.LanguageMode;
                }
                else if (SystemPolicy.GetSystemLockdownPolicy() == SystemEnforcementMode.Audit)
                {
                    string scriptBlockId = $"{this.Id}+{this.GetFileName() ?? string.Empty}";
                    SystemPolicy.LogWDACAuditMessage(
                        title: AutomationExceptions.WDACCompiledScriptBlockLogTitle,
                        message: StringUtil.Format(AutomationExceptions.WDACCompiledScriptBlockLogMessage, scriptBlockId, this.LanguageMode, context.LanguageMode),
                        fqid: "ScriptBlockDotSourceNotAllowed",
                        dropIntoDebugger: true);

                    // Since we are in audit mode, go ahead and allow the language transition.
                    oldLanguageMode = context.LanguageMode;
                    newLanguageMode = this.LanguageMode;
                }
            }

            Dictionary<string, PSVariable> backupWhenDotting = null;
            try
            {
                var myInvocationInfo = invocationInfo;
                if (myInvocationInfo == null)
                {
                    var callerFrame = context.Debugger.GetCallStack().LastOrDefault();
                    var extent = (callerFrame != null)
                        ? callerFrame.FunctionContext.CurrentPosition
                        : Ast.Extent;
                    myInvocationInfo = new InvocationInfo(null, extent, context);
                }

                locals.SetAutomaticVariable(AutomaticVariable.MyInvocation, myInvocationInfo, context);

                if (SessionStateInternal != null)
                {
                    context.EngineSessionState = SessionStateInternal;
                }

                // If we don't want errors written, hide the error pipe.
                switch (errorHandlingBehavior)
                {
                    case ErrorHandlingBehavior.WriteToCurrentErrorPipe:
                        // no need to do anything
                        break;
                    case ErrorHandlingBehavior.WriteToExternalErrorPipe:
                        context.ShellFunctionErrorOutputPipe = null;
                        break;
                    case ErrorHandlingBehavior.SwallowErrors:
                        context.ShellFunctionErrorOutputPipe = null;
                        context.ExternalErrorOutput = new DiscardingPipelineWriter();
                        break;
                }

                if (createLocalScope)
                {
                    var newScope = context.EngineSessionState.NewScope(false);
                    context.EngineSessionState.CurrentScope = newScope;
                    newScope.LocalsTuple = locals;
                    // Inject passed in functions into the scope
                    if (functionsToDefine != null)
                    {
                        foreach (var def in functionsToDefine)
                        {
                            if (string.IsNullOrWhiteSpace(def.Key))
                            {
                                PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                                    ParserStrings.EmptyFunctionNameInFunctionDefinitionDictionary);

                                e.SetErrorId("EmptyFunctionNameInFunctionDefinitionDictionary");
                                throw e;
                            }

                            if (def.Value == null)
                            {
                                PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                                    ParserStrings.NullFunctionBodyInFunctionDefinitionDictionary, def.Key);

                                e.SetErrorId("NullFunctionBodyInFunctionDefinitionDictionary");
                                throw e;
                            }

                            newScope.FunctionTable.Add(def.Key, new FunctionInfo(def.Key, def.Value, context));
                        }
                    }
                    // Inject passed in variables into the scope
                    if (variablesToDefine != null)
                    {
                        int index = 0;
                        foreach (var psvar in variablesToDefine)
                        {
                            // Check for null entries.
                            if (psvar == null)
                            {
                                PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                                    ParserStrings.NullEntryInVariablesDefinitionList, index);

                                e.SetErrorId("NullEntryInVariablesDefinitionList");
                                throw e;
                            }

                            string name = psvar.Name;
                            Diagnostics.Assert(
                                !(string.Equals(name, "this") || string.Equals(name, "_") || string.Equals(name, "input")),
                                "The list of variables to set in the scriptblock's scope cannot contain 'this', '_' or 'input'. These variables should be removed before passing the collection to this routine.");
                            index++;
                            newScope.Variables.Add(name, psvar);
                        }
                    }
                }
                else
                {
                    if (context.EngineSessionState.CurrentScope.LocalsTuple == null)
                    {
                        // If the locals tuple is null, that means either:
                        //     * we're invoking a script block for a module
                        //     * something unexpected
                        context.EngineSessionState.CurrentScope.LocalsTuple = locals;
                    }
                    else
                    {
                        context.EngineSessionState.CurrentScope.DottedScopes.Push(locals);
                        backupWhenDotting = new Dictionary<string, PSVariable>();
                    }
                }

                // Set the language mode
                if (newLanguageMode.HasValue)
                {
                    context.LanguageMode = newLanguageMode.Value;
                }

                args = BindArgumentsForScriptblockInvoke(
                    (RuntimeDefinedParameter[])RuntimeDefinedParameters.Data,
                    args,
                    context,
                    !createLocalScope,
                    backupWhenDotting,
                    locals);
                locals.SetAutomaticVariable(AutomaticVariable.Args, args, context);

                context.EngineSessionState.CurrentScope.ScopeOrigin = CommandOrigin.Internal;

                var functionContext = new FunctionContext
                {
                    _executionContext = context,
                    _outputPipe = outputPipe,
                    _localsTuple = locals,
                    _scriptBlock = this,
                    _file = this.File,
                    _debuggerHidden = this.DebuggerHidden,
                    _debuggerStepThrough = this.DebuggerStepThrough,
                    _sequencePoints = SequencePoints,
                };

                LogScriptBlockStart(this, context.CurrentRunspace.InstanceId);

                try
                {
                    codeToInvoke(functionContext);
                }
                finally
                {
                    LogScriptBlockEnd(this, context.CurrentRunspace.InstanceId);
                }
            }
            catch (TargetInvocationException tie)
            {
                // DynamicInvoke always wraps, so unwrap here.
                throw tie.InnerException;
            }
            finally
            {
                // Restore the language mode
                if (oldLanguageMode.HasValue)
                {
                    context.LanguageMode = oldLanguageMode.Value;
                }

                // Now restore the output pipe...
                context.ShellFunctionErrorOutputPipe = oldShellFunctionErrorOutputPipe;
                context.ExternalErrorOutput = oldExternalErrorOutput;

                // Restore the interactive command state...
                context.EngineSessionState.CurrentScope.ScopeOrigin = oldScopeOrigin;

                if (createLocalScope)
                {
                    context.EngineSessionState.RemoveScope(context.EngineSessionState.CurrentScope);
                }
                else if (backupWhenDotting != null)
                {
                    context.EngineSessionState.CurrentScope.DottedScopes.Pop();

                    Diagnostics.Assert(backupWhenDotting != null, "when dotting, this dictionary isn't null");
                    foreach (var pair in backupWhenDotting)
                    {
                        if (pair.Value != null)
                        {
                            context.EngineSessionState.SetVariable(pair.Value, false, CommandOrigin.Internal);
                        }
                        else
                        {
                            context.EngineSessionState.RemoveVariable(pair.Key);
                        }
                    }
                }

                // Restore session state...
                context.EngineSessionState = oldSessionState;
            }
        }

        internal MutableTuple MakeLocalsTuple(bool createLocalScope)
        {
            MutableTuple locals;
            if (createLocalScope)
            {
                locals = MutableTuple.MakeTuple(
                    _scriptBlockData.LocalsMutableTupleType,
                    _scriptBlockData.NameToIndexMap,
                    _scriptBlockData.LocalsMutableTupleCreator);
            }
            else
            {
                locals = MutableTuple.MakeTuple(
                    _scriptBlockData.UnoptimizedLocalsMutableTupleType,
                    UsesCmdletBinding
                        ? Compiler.DottedScriptCmdletLocalsNameIndexMap
                        : Compiler.DottedLocalsNameIndexMap,
                    _scriptBlockData.UnoptimizedLocalsMutableTupleCreator);
            }

            return locals;
        }

        internal static object[] BindArgumentsForScriptblockInvoke(
            RuntimeDefinedParameter[] parameters,
            object[] args,
            ExecutionContext context,
            bool dotting,
            Dictionary<string, PSVariable> backupWhenDotting,
            MutableTuple locals)
        {
            var boundParameters = new CommandLineParameters();

            if (parameters.Length == 0)
            {
                return args;
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                object valueToBind;
                bool wasDefaulted = false;
                if (i >= args.Length)
                {
                    valueToBind = parameter.Value;
                    if (valueToBind is Compiler.DefaultValueExpressionWrapper)
                    {
                        // We pass in a null SessionStateInternal because the current scope is already set correctly.
                        valueToBind = ((Compiler.DefaultValueExpressionWrapper)valueToBind).GetValue(context, null);
                    }

                    wasDefaulted = true;
                }
                else
                {
                    valueToBind = args[i];
                }

                bool valueSet = false;
                if (dotting && backupWhenDotting != null)
                {
                    backupWhenDotting[parameter.Name] =
                        context.EngineSessionState.GetVariableAtScope(parameter.Name, "local");
                }
                else
                {
                    valueSet = locals.TrySetParameter(parameter.Name, valueToBind);
                }

                if (!valueSet)
                {
                    var variable = new PSVariable(
                        parameter.Name,
                        valueToBind,
                        ScopedItemOptions.None,
                        parameter.Attributes);
                    context.EngineSessionState.SetVariable(variable, false, CommandOrigin.Internal);
                }

                if (!wasDefaulted)
                {
                    boundParameters.Add(parameter.Name, valueToBind);
                    boundParameters.MarkAsBoundPositionally(parameter.Name);
                }
            }

            locals.SetAutomaticVariable(
                AutomaticVariable.PSBoundParameters,
                boundParameters.GetValueToBindToPSBoundParameters(),
                context);

            var leftOverArgs = args.Length - parameters.Length;
            if (leftOverArgs <= 0)
            {
                return Array.Empty<object>();
            }

            object[] result = new object[leftOverArgs];
            Array.Copy(args, parameters.Length, result, 0, result.Length);
            return result;
        }

        internal static void SetAutomaticVariable(AutomaticVariable variable, object value, MutableTuple locals)
            => locals.SetValue((int)variable, value);

        private Action<FunctionContext> GetCodeToInvoke(ref bool optimized, ScriptBlockClauseToInvoke clauseToInvoke)
        {
            if (clauseToInvoke == ScriptBlockClauseToInvoke.ProcessBlockOnly
                && (HasBeginBlock || HasCleanBlock || (HasEndBlock && HasProcessBlock)))
            {
                throw PSTraceSource.NewInvalidOperationException(AutomationExceptions.ScriptBlockInvokeOnOneClauseOnly);
            }

            optimized = _scriptBlockData.Compile(optimized);

            if (optimized)
            {
                switch (clauseToInvoke)
                {
                    case ScriptBlockClauseToInvoke.Begin:
                        return _scriptBlockData.BeginBlock;
                    case ScriptBlockClauseToInvoke.Process:
                        return _scriptBlockData.ProcessBlock;
                    case ScriptBlockClauseToInvoke.End:
                        return _scriptBlockData.EndBlock;
                    case ScriptBlockClauseToInvoke.Clean:
                        return _scriptBlockData.CleanBlock;
                    default:
                        return HasProcessBlock ? _scriptBlockData.ProcessBlock : _scriptBlockData.EndBlock;
                }
            }

            switch (clauseToInvoke)
            {
                case ScriptBlockClauseToInvoke.Begin:
                    return _scriptBlockData.UnoptimizedBeginBlock;
                case ScriptBlockClauseToInvoke.Process:
                    return _scriptBlockData.UnoptimizedProcessBlock;
                case ScriptBlockClauseToInvoke.End:
                    return _scriptBlockData.UnoptimizedEndBlock;
                case ScriptBlockClauseToInvoke.Clean:
                    return _scriptBlockData.UnoptimizedCleanBlock;
                default:
                    return HasProcessBlock ? _scriptBlockData.UnoptimizedProcessBlock : _scriptBlockData.UnoptimizedEndBlock;
            }
        }

        internal CmdletBindingAttribute CmdletBindingAttribute { get => _scriptBlockData.CmdletBindingAttribute; }

        internal ObsoleteAttribute ObsoleteAttribute { get => _scriptBlockData.ObsoleteAttribute; }

        internal ExperimentalAttribute ExperimentalAttribute { get => _scriptBlockData.ExperimentalAttribute; }

        internal bool Compile(bool optimized) => _scriptBlockData.Compile(optimized);

        internal static void LogScriptBlockCreation(ScriptBlock scriptBlock, bool force)
        {
            if (scriptBlock.HasLogged && !InternalTestHooks.ForceScriptBlockLogging)
            {
                // Fast exit if the script block is already logged and we are not force re-logging in tests.
                return;
            }

            ScriptBlockLogging logSetting = GetScriptBlockLoggingSetting();
            if (force || logSetting?.EnableScriptBlockLogging == true)
            {
                // If script block logging is explicitly disabled, or it's from a trusted
                // file or internal, skip logging.
                if (logSetting?.EnableScriptBlockLogging == false
                    || scriptBlock.ScriptBlockData.IsProductCode)
                {
                    scriptBlock.SkipLogging = true;
                    return;
                }

                string scriptBlockText = scriptBlock.Ast.Extent.Text;
                bool written = false;

                // Maximum size of ETW events is 64kb. Split a message if it is larger than 20k (Unicode) characters.
                if (scriptBlockText.Length < 20000)
                {
                    written = WriteScriptBlockToLog(scriptBlock, 0, 1, scriptBlock.Ast.Extent.Text);
                }
                else
                {
                    // But split the segments into random sizes (10k + between 0 and 10kb extra)
                    // so that attackers can't creatively force their scripts to span well-known
                    // segments (making simple rules less reliable).
                    int segmentSize = 10000 + (new Random()).Next(10000);
                    int segments = (int)Math.Floor((double)(scriptBlockText.Length / segmentSize)) + 1;
                    int currentLocation = 0;
                    int currentSegmentSize = 0;

                    for (int segment = 0; segment < segments; segment++)
                    {
                        currentLocation = segment * segmentSize;
                        currentSegmentSize = Math.Min(segmentSize, scriptBlockText.Length - currentLocation);

                        string textToLog = scriptBlockText.Substring(currentLocation, currentSegmentSize);
                        written = WriteScriptBlockToLog(scriptBlock, segment, segments, textToLog);
                    }
                }

                if (written)
                {
                    scriptBlock.HasLogged = true;
                }
            }
            else
            {
                scriptBlock.SkipLogging = true;
            }
        }

        private static bool WriteScriptBlockToLog(ScriptBlock scriptBlock, int segment, int segments, string textToLog)
        {
            // See if we need to encrypt the event log message. This info is all cached by Utils.GetPolicySetting(),
            // so we're not hitting the configuration file for every script block we compile.
            ProtectedEventLogging logSetting =
                Utils.GetPolicySetting<ProtectedEventLogging>(Utils.SystemWideOnlyConfig);
            bool wasEncoded = false;
            if (logSetting != null)
            {
                lock (s_syncObject)
                {
                    // Populates the encryptionRecipients list from the Group Policy, if possible. If not possible,
                    // does all appropriate logging and encryptionRecipients is 'null'. 'CouldLog' being false
                    // implies the engine wasn't ready for logging yet.
                    bool couldLog = GetAndValidateEncryptionRecipients(scriptBlock, logSetting);
                    if (!couldLog)
                    {
                        return false;
                    }

                    // If we have recipients to encrypt to, then do so.
                    // Otherwise, we'll just log the plain text version.
                    if (s_encryptionRecipients != null)
                    {
                        // Encrypt the raw text from the scriptblock.
                        // The user may have to deal with any control characters in the data.
                        ExecutionContext executionContext = LocalPipeline.GetExecutionContextFromTLS();
                        ErrorRecord error = null;
                        byte[] contentBytes = System.Text.Encoding.UTF8.GetBytes(textToLog);
                        string encodedContent = CmsUtils.Encrypt(
                            contentBytes,
                            s_encryptionRecipients,
                            executionContext.SessionState,
                            out error);

                        // Can't cache the reporting of encryption errors, as they are likely content-based.
                        if (error != null)
                        {
                            // If we got an error encrypting the content, log an error and continue
                            // logging the (unencrypted) message anyways. Logging trumps protected logging -
                            // being able to detect that an attacker has compromised a box outweighs the danger of the
                            // attacker seeing potentially sensitive data. Because if they aren't detected, then
                            // they can just wait on the compromised box and see the sensitive data eventually anyways.

                            string errorMessage = StringUtil.Format(
                                SecuritySupportStrings.CouldNotEncryptContent,
                                textToLog,
                                error.ToString());
                            PSEtwLog.LogOperationalError(
                                id: PSEventId.ScriptBlock_Compile_Detail,
                                opcode: PSOpcode.Create,
                                task: PSTask.ExecuteCommand,
                                keyword: PSKeyword.UseAlwaysOperational,
                                0,
                                0,
                                errorMessage,
                                scriptBlock.Id.ToString(),
                                scriptBlock.File ?? string.Empty);
                        }
                        else
                        {
                            textToLog = encodedContent;
                            wasEncoded = true;
                        }
                    }
                }
            }

            if (!wasEncoded)
            {
                textToLog = FormatLogString(textToLog);
            }

            if (scriptBlock._scriptBlockData.HasSuspiciousContent)
            {
                PSEtwLog.LogOperationalWarning(
                    id: PSEventId.ScriptBlock_Compile_Detail,
                    opcode: PSOpcode.Create,
                    task: PSTask.ExecuteCommand,
                    keyword: PSKeyword.UseAlwaysOperational,
                    segment + 1,
                    segments,
                    textToLog,
                    scriptBlock.Id.ToString(),
                    scriptBlock.File ?? string.Empty);
            }
            else
            {
                PSEtwLog.LogOperationalVerbose(
                    id: PSEventId.ScriptBlock_Compile_Detail,
                    opcode: PSOpcode.Create,
                    task: PSTask.ExecuteCommand,
                    keyword: PSKeyword.UseAlwaysOperational,
                    segment + 1,
                    segments,
                    textToLog,
                    scriptBlock.Id.ToString(),
                    scriptBlock.File ?? string.Empty);
            }

            return true;
        }

        private static string FormatLogString(string textToLog)
        {
            const char NullControlChar = '\u0000';

            // The null symbol - `␀`
            const char NullSymbolChar = '\u2400';

            // No logging mechanism(s) cannot handle null and rendering may not be able to handle
            // null as we have the string defined as a null terminated string in the manifest.
            // So, replace null characters with the Unicode `SYMBOL FOR NULL`
            // We don't just remove the characters to preserve the fact that a null character was there.
#if UNIX
            const char LinefeedControlChar = '\u000A';
            const char CarriageReturnControlChar = '\u000D';

            // We chose the return symbol because we believe it is more associated with these concepts
            // than the carriage return (␍), line feed (␊), or new line (␤) symbols.
            // The return symbol - `⏎`
            const char ReturnSymbolChar = '\u23CE';

            if (Platform.IsLinux)
            {
                // Because the creation of the string builder is expensive
                // We only do this on Linux where we are doing multiple replace operations
                StringBuilder logBuilder = new StringBuilder(textToLog);

                logBuilder.Replace(NullControlChar, NullSymbolChar);

                // Syslog (only used on Linux) encodes CR and NL to their octal values.
                // We will replace them with a unicode  'RETURN SYMBOL' (U+23CE) charater for easier viewing
                logBuilder.Replace(LinefeedControlChar, ReturnSymbolChar);
                logBuilder.Replace(CarriageReturnControlChar, ReturnSymbolChar);

                return logBuilder.ToString();
            }
            else
            {
                return textToLog.Replace(NullControlChar, NullSymbolChar);
            }
#else
            return textToLog.Replace(NullControlChar, NullSymbolChar);
#endif
        }

        private static bool GetAndValidateEncryptionRecipients(
            ScriptBlock scriptBlock,
            ProtectedEventLogging logSetting)
        {
            // See if protected event logging is enabled
            if (logSetting.EnableProtectedEventLogging == true)
            {
                // Get the encryption certificate
                if (logSetting.EncryptionCertificate != null)
                {
                    ErrorRecord error = null;
                    ExecutionContext executionContext = LocalPipeline.GetExecutionContextFromTLS();
                    SessionState sessionState = null;

                    // Use the session state from the current pipeline, if it exists.
                    if (executionContext != null)
                    {
                        sessionState = executionContext.SessionState;
                    }

                    // If the engine hasn't started up yet, then we're just compiling script
                    // blocks. We'll have to log them when they are used and we have an engine
                    // to work with.
                    if (sessionState == null)
                    {
                        return false;
                    }

                    string fullCertificateContent = string.Join(Environment.NewLine, logSetting.EncryptionCertificate);

                    // If the certificate has changed, drop all of our cached information
                    ResetCertificateCacheIfNeeded(fullCertificateContent);

                    // If we have valid recipients, no need for further analysis.
                    if (s_encryptionRecipients != null)
                    {
                        return true;
                    }

                    // If we've already verified all of the properties of the cert we care about (even if it
                    // didn't result in a valid cert), return now.
                    if (s_hasProcessedCertificate)
                    {
                        return true;
                    }

                    // Resolve the certificate to a recipient
                    CmsMessageRecipient recipient = new CmsMessageRecipient(fullCertificateContent);
                    recipient.Resolve(sessionState, ResolutionPurpose.Encryption, out error);
                    s_hasProcessedCertificate = true;

                    // If there's an error that we haven't already reported, report it in the event log.
                    // We only do this once, as the error will always be the same for a given certificate.
                    if (error != null)
                    {
                        // If we got an error resolving the encryption certificate, log a warning and continue
                        // logging the (unencrypted) message anyways. Logging trumps protected logging -
                        // being able to detect that an attacker has compromised a box outweighs the danger of the
                        // attacker seeing potentially sensitive data. Because if they aren't detected, then
                        // they can just wait on the compromised box and see the sensitive data eventually anyways.
                        string errorMessage = StringUtil.Format(
                            SecuritySupportStrings.CouldNotUseCertificate,
                            error.ToString());
                        PSEtwLog.LogOperationalError(
                            id: PSEventId.ScriptBlock_Compile_Detail,
                            opcode: PSOpcode.Create,
                            task: PSTask.ExecuteCommand,
                            keyword: PSKeyword.UseAlwaysOperational,
                            0,
                            0,
                            errorMessage,
                            scriptBlock.Id.ToString(),
                            scriptBlock.File ?? string.Empty);

                        return true;
                    }

                    // Now, save the certificate. We'll be comfortable using this one from now on.
                    s_encryptionRecipients = new CmsMessageRecipient[] { recipient };

                    // Check if the certificate has a private key, and report a warning if so.
                    // We only do this once, as the error will always be the same for a given certificate.
                    foreach (X509Certificate2 validationCertificate in recipient.Certificates)
                    {
                        if (validationCertificate.HasPrivateKey)
                        {
                            // Only log the first line of what we pulled from the configuration. If this is a path, this will have enough information.
                            // If this is the actual certificate, only include the first line of the certificate content so that we're not permanently keeping the private
                            // key in the log.
                            string certificateForLog = fullCertificateContent;
                            if (logSetting.EncryptionCertificate.Length > 1)
                            {
                                certificateForLog = logSetting.EncryptionCertificate[1];
                            }

                            string errorMessage = StringUtil.Format(
                                SecuritySupportStrings.CertificateContainsPrivateKey,
                                certificateForLog);
                            PSEtwLog.LogOperationalError(
                                id: PSEventId.ScriptBlock_Compile_Detail,
                                opcode: PSOpcode.Create,
                                task: PSTask.ExecuteCommand,
                                keyword: PSKeyword.UseAlwaysOperational,
                                0,
                                0,
                                errorMessage,
                                scriptBlock.Id.ToString(),
                                scriptBlock.File ?? string.Empty);
                        }
                    }
                }
            }

            return true;
        }

        private static readonly object s_syncObject = new object();
        private static string s_lastSeenCertificate = string.Empty;
        private static bool s_hasProcessedCertificate = false;
        private static CmsMessageRecipient[] s_encryptionRecipients = null;

        private static readonly Lazy<ScriptBlockLogging> s_sbLoggingSettingCache = new Lazy<ScriptBlockLogging>(
            static () => Utils.GetPolicySetting<ScriptBlockLogging>(Utils.SystemWideThenCurrentUserConfig),
            isThreadSafe: true);

        // Reset any static caches if the certificate has changed
        private static void ResetCertificateCacheIfNeeded(string certificate)
        {
            if (!string.Equals(s_lastSeenCertificate, certificate, StringComparison.Ordinal))
            {
                s_hasProcessedCertificate = false;
                s_lastSeenCertificate = certificate;
                s_encryptionRecipients = null;
            }
        }

        private static ScriptBlockLogging GetScriptBlockLoggingSetting()
        {
            if (InternalTestHooks.BypassGroupPolicyCaching)
            {
                return Utils.GetPolicySetting<ScriptBlockLogging>(Utils.SystemWideThenCurrentUserConfig);
            }

            return s_sbLoggingSettingCache.Value;
        }

        // Quick check for script blocks that may have suspicious content. If this
        // is true, we log them to the event log despite event log settings.
        internal static string CheckSuspiciousContent(Ast scriptBlockAst)
        {
            var foundSignature = SuspiciousContentChecker.Match(scriptBlockAst.Extent.Text);
            if (foundSignature != null)
            {
                return foundSignature;
            }

            if (scriptBlockAst.HasSuspiciousContent)
            {
                Ast foundAst = scriptBlockAst.Find(
                    ast =>
                    {
                        // Try to find the lowest AST that was not considered suspicious, but its parent was.
                        return (!ast.HasSuspiciousContent) && ast.Parent.HasSuspiciousContent;
                    }, true);

                if (foundAst != null)
                {
                    return foundAst.Parent.Extent.Text;
                }
                else
                {
                    return scriptBlockAst.Extent.Text;
                }
            }

            return null;
        }

        private static class SuspiciousContentChecker
        {
            // Based on a (bad) random number generator, but good enough
            // for our simple needs.
            private const uint LCG = 31;

            /// <summary>
            /// Check if a hash code matches a small set of pre-computed hashes
            /// for suspicious strings in a PowerShell script.
            ///
            /// If you need to add a new string, use the commented out
            /// method HashNewPattern (commented out because it's dead
            /// code - needed only to generate this switch statement below.)
            /// </summary>
            /// <returns>The string matching the hash, or null.</returns>
            private static string LookupHash(uint h)
            {
                switch (h)
                {
                    // Calling Add-Type
                    case 3012981990: return "Add-Type";
                    case 3359423881: return "DllImport";

                    // Doing dynamic assembly building / method indirection
                    case 2713126922: return "DefineDynamicAssembly";
                    case 2407049616: return "DefineDynamicModule";
                    case 3276870517: return "DefineType";
                    case 419507039: return "DefineConstructor";
                    case 1370182198: return "CreateType";
                    case 1973546644: return "DefineLiteral";
                    case 3276413244: return "DefineEnum";
                    case 2785322015: return "DefineField";
                    case 837002512: return "ILGenerator";
                    case 3117011: return "Emit";
                    case 883134515: return "UnverifiableCodeAttribute";
                    case 2920989166: return "DefinePInvokeMethod";
                    case 1996222179: return "GetTypes";
                    case 3935635674: return "GetAssemblies";
                    case 955534258: return "Methods";
                    case 3368914227: return "Properties";

                    // Suspicious methods / properties on "Type"
                    case 398423780: return "GetConstructor";
                    case 3761202703: return "GetConstructors";
                    case 1998297230: return "GetDefaultMembers";
                    case 1982269700: return "GetEvent";
                    case 1320818671: return "GetEvents";
                    case 1982805860: return "GetField";
                    case 1337439631: return "GetFields";
                    case 2784018083: return "GetInterface";
                    case 2864332761: return "GetInterfaceMap";
                    case 405214768: return "GetInterfaces";
                    case 1534378352: return "GetMember";
                    case 321088771: return "GetMembers";
                    case 1534592951: return "GetMethod";
                    case 327741340: return "GetMethods";
                    case 1116240007: return "GetNestedType";
                    case 243701964: return "GetNestedTypes";
                    case 1077700873: return "GetProperties";
                    case 1020114731: return "GetProperty";
                    case 257791250: return "InvokeMember";
                    case 3217683173: return "MakeArrayType";
                    case 821968872: return "MakeByRefType";
                    case 3538448099: return "MakeGenericType";
                    case 3207725129: return "MakePointerType";
                    case 1617553224: return "DeclaringMethod";
                    case 3152745313: return "DeclaringType";
                    case 4144122198: return "ReflectedType";
                    case 3455789538: return "TypeHandle";
                    case 624373608: return "TypeInitializer";
                    case 637454598: return "UnderlyingSystemType";

                    // Doing things with System.Runtime.InteropServices
                    case 1855303451: return "InteropServices";
                    case 839491486: return "Marshal";
                    case 1928879414: return "AllocHGlobal";
                    case 3180922282: return "PtrToStructure";
                    case 1718292736: return "StructureToPtr";
                    case 3390778911: return "FreeHGlobal";
                    case 3111215263: return "IntPtr";

                    // General Obfuscation
                    case 1606191041: return "MemoryStream";
                    case 2147536747: return "DeflateStream";
                    case 1820815050: return "FromBase64String";
                    case 3656724093: return "EncodedCommand";
                    case 2920836328: return "Bypass";
                    case 3473847323: return "ToBase64String";
                    case 4192166699: return "ExpandString";
                    case 2462813217: return "GetPowerShell";

                    // Suspicious Win32 API calls
                    case 2123968741: return "OpenProcess";
                    case 3630248714: return "VirtualAlloc";
                    case 3303847927: return "VirtualFree";
                    case 512407217: return "WriteProcessMemory";
                    case 2357873553: return "CreateUserThread";
                    case 756544032: return "CloseHandle";
                    case 3400025495: return "GetDelegateForFunctionPointer";
                    case 314128220: return "kernel32";
                    case 2469462534: return "CreateThread";
                    case 3217199031: return "memcpy";
                    case 2283745557: return "LoadLibrary";
                    case 3317813738: return "GetModuleHandle";
                    case 2491894472: return "GetProcAddress";
                    case 1757922660: return "VirtualProtect";
                    case 2693938383: return "FreeLibrary";
                    case 2873914970: return "ReadProcessMemory";
                    case 2717270220: return "CreateRemoteThread";
                    case 2867203884: return "AdjustTokenPrivileges";
                    case 2889068903: return "WriteByte";
                    case 3667925519: return "WriteInt32";
                    case 2742077861: return "OpenThreadToken";
                    case 2826980154: return "PtrToString";
                    case 3735047487: return "ZeroFreeGlobalAllocUnicode";
                    case 788615220: return "OpenProcessToken";
                    case 1264589033: return "GetTokenInformation";
                    case 2165372045: return "SetThreadToken";
                    case 197357349: return "ImpersonateLoggedOnUser";
                    case 1259149099: return "RevertToSelf";
                    case 2446460563: return "GetLogonSessionData";
                    case 2534763616: return "CreateProcessWithToken";
                    case 3512478977: return "DuplicateTokenEx";
                    case 3126049082: return "OpenWindowStation";
                    case 3990594194: return "OpenDesktop";
                    case 3195806696: return "MiniDumpWriteDump";
                    case 3990234693: return "AddSecurityPackage";
                    case 611728017: return "EnumerateSecurityPackages";
                    case 4283779521: return "GetProcessHandle";
                    case 845600244: return "DangerousGetHandle";

                    // Crypto - ransomware, etc.
                    case 2691669189: return "CryptoServiceProvider";
                    case 1413809388: return "Cryptography";
                    case 4113841312: return "RijndaelManaged";
                    case 1650652922: return "SHA1Managed";
                    case 1759701889: return "CryptoStream";
                    case 2439640460: return "CreateEncryptor";
                    case 1446703796: return "CreateDecryptor";
                    case 1638240579: return "TransformFinalBlock";
                    case 1464730593: return "DeviceIoControl";
                    case 3966822309: return "SetInformationProcess";
                    case 851965993: return "PasswordDeriveBytes";

                    // Keylogging
                    case 793353336: return "GetAsyncKeyState";
                    case 293877108: return "GetKeyboardState";
                    case 2448894537: return "GetForegroundWindow";

                    // Using internal types
                    case 4059335458: return "BindingFlags";
                    case 1085624182: return "NonPublic";

                    // Changing logging settings
                    case 904148605: return "ScriptBlockLogging";
                    case 4150524432: return "LogPipelineExecutionDetails";
                    case 3704712755: return "ProtectedEventLogging";

                    default: return null;
                }
            }

            /// <summary>
            /// Check the list of running hashes for any matches, but
            /// only up to the limit of <paramref name="upTo"/>.
            ///
            /// If a hash matches, we ignore the possibility of a
            /// collision. If the hash is acceptable, collisions will
            /// be infrequent and we'll just log an occasional script
            /// that isn't really suspicious.
            /// </summary>
            /// <returns>The string matching the hash, or null.</returns>
            private static string CheckForMatches(uint[] runningHash, int upTo)
            {
                var upToMax = runningHash.Length;
                if (upTo == 0 || upTo > upToMax)
                {
                    upTo = upToMax;
                }

                for (var i = 0; i < upTo; i++)
                {
                    var result = LookupHash(runningHash[i]);
                    if (result != null)
                    {
                        return result;
                    }
                }

                return null;
            }

            /// <summary>
            /// Scan a string for suspicious content.
            ///
            /// This is based on the Rubin-Karp algorithm, but heavily
            /// modified to support searching for multiple patterns at
            /// the same time.
            ///
            /// The key difference from Rubin-Karp is that we don't undo
            /// the hash of the first character as we shift along in the
            /// input.
            ///
            /// Instead, we can rely on knowing we need the hashes for
            /// shorter strings anyway, so we reuse their values in
            /// computing the hash for the longer patterns. This lets us
            /// use a much simpler hash as well - we can avoid the use of
            /// mod.
            /// </summary>
            /// <returns>The string matching the hash, or null.</returns>
            public static string Match(string text)
            {
                // The longest pattern is 29 characters.
                // The values in the array are the computed hashes of length
                // index-1 (so runningHash[0] holds the hash for length 1).
                var runningHash = new uint[29];

                int longestPossiblePattern = 0;
                for (int i = 0; i < text.Length; i++)
                {
                    uint h = text[i];
                    if (h >= 'A' && h <= 'Z')
                    {
                        h |= 0x20; // ToLower
                    }
                    else if (!((h >= 'a' && h <= 'z') || h == '-'))
                    {
                        // If the character isn't in any of our patterns,
                        // don't bother hashing and reset the running length.
                        longestPossiblePattern = 0;
                        continue;
                    }

                    for (int j = Math.Min(i, runningHash.Length - 1); j > 0; j--)
                    {
                        // Say our input is: `Emit` (our shortest pattern, len 4).
                        // Towards the end just before matching, we will:
                        //
                        // iter n: compute hash on `Emi` (len 3)
                        // iter n+1: compute hash on `Emit` (len 4) using hash from previous iteration (j-1)
                        // iter n+1: compute hash on `mit` (len 3)
                        //    This overwrites the previous iteration, hence we go from longest to shortest.
                        //
                        // LCG comes from a trivial (bad) random number generator,
                        // but it's sufficient for us - the hashes for our patterns
                        // are unique, and processing of 2200 files found no false matches.
                        runningHash[j] = LCG * runningHash[j - 1] + h;
                    }

                    runningHash[0] = h;

                    if (++longestPossiblePattern >= 4)
                    {
                        var result = CheckForMatches(runningHash, longestPossiblePattern);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }

                return CheckForMatches(runningHash, 0);
            }

#if false
            // This code can be used when adding a new pattern.
            internal static uint HashNewPattern(string pattern)
            {
                char ToLower(char c)
                {
                    if (c >= 'A' && c <= 'Z')
                    {
                        c = (char) (c | 0x20);
                    }

                    return c;
                }

                if (pattern.Length > 29) {
                    throw new Exception(
                        "Update runningHash in match for new longest string.\n" +
                        "Also a longer maximum length could greatly affect the performance of this algorithm, so only increase with care.");
                }

                uint h = 0;
                foreach (var c in pattern)
                {
                    h = LCG * h + ToLower(c);
                }

                return h;
            }
#endif
        }

        internal static void LogScriptBlockStart(ScriptBlock scriptBlock, Guid runspaceId)
        {
            // Fast exit if the script block can skip logging.
            if (!scriptBlock.SkipLogging)
            {
                // When invoking, log the creation of the script block if it has suspicious content
                bool forceLogCreation = scriptBlock._scriptBlockData.HasSuspiciousContent;

                // We delay logging the creation until the 'Start' so that we can be sure we've
                // properly analyzed the script block's security.
                LogScriptBlockCreation(scriptBlock, forceLogCreation);
            }

            if (GetScriptBlockLoggingSetting()?.EnableScriptBlockInvocationLogging == true)
            {
                PSEtwLog.LogOperationalVerbose(
                    id: PSEventId.ScriptBlock_Invoke_Start_Detail,
                    opcode: PSOpcode.Create,
                    task: PSTask.CommandStart,
                    keyword: PSKeyword.UseAlwaysOperational,
                    scriptBlock.Id.ToString(),
                    runspaceId.ToString());
            }
        }

        internal static void LogScriptBlockEnd(ScriptBlock scriptBlock, Guid runspaceId)
        {
            if (GetScriptBlockLoggingSetting()?.EnableScriptBlockInvocationLogging == true)
            {
                PSEtwLog.LogOperationalVerbose(
                    id: PSEventId.ScriptBlock_Invoke_Complete_Detail,
                    opcode: PSOpcode.Create,
                    task: PSTask.CommandStop,
                    keyword: PSKeyword.UseAlwaysOperational,
                    scriptBlock.Id.ToString(),
                    runspaceId.ToString());
            }
        }

        internal CompiledScriptBlockData ScriptBlockData { get => _scriptBlockData; }

        /// <summary>
        /// Returns the AST corresponding to the script block.
        /// </summary>
        public Ast Ast { get => (Ast)_scriptBlockData.Ast; }

        internal IParameterMetadataProvider AstInternal { get => _scriptBlockData.Ast; }

        internal IScriptExtent[] SequencePoints { get => _scriptBlockData.SequencePoints; }

        internal Action<FunctionContext> DynamicParamBlock { get => _scriptBlockData.DynamicParamBlock; }

        internal Action<FunctionContext> UnoptimizedDynamicParamBlock { get => _scriptBlockData.UnoptimizedDynamicParamBlock; }

        internal Action<FunctionContext> BeginBlock { get => _scriptBlockData.BeginBlock; }

        internal Action<FunctionContext> UnoptimizedBeginBlock { get => _scriptBlockData.UnoptimizedBeginBlock; }

        internal Action<FunctionContext> ProcessBlock { get => _scriptBlockData.ProcessBlock; }

        internal Action<FunctionContext> UnoptimizedProcessBlock { get => _scriptBlockData.UnoptimizedProcessBlock; }

        internal Action<FunctionContext> EndBlock { get => _scriptBlockData.EndBlock; }

        internal Action<FunctionContext> UnoptimizedEndBlock { get => _scriptBlockData.UnoptimizedEndBlock; }

        internal Action<FunctionContext> CleanBlock { get => _scriptBlockData.CleanBlock; }

        internal Action<FunctionContext> UnoptimizedCleanBlock { get => _scriptBlockData.UnoptimizedCleanBlock; }

        internal bool HasBeginBlock { get => AstInternal.Body.BeginBlock != null; }

        internal bool HasProcessBlock { get => AstInternal.Body.ProcessBlock != null; }

        internal bool HasEndBlock { get => AstInternal.Body.EndBlock != null; }

        internal bool HasCleanBlock { get => AstInternal.Body.CleanBlock != null; }
    }

    [Serializable]
    internal class ScriptBlockSerializationHelper : ISerializable, IObjectReference
    {
        private readonly string _scriptText;

        private ScriptBlockSerializationHelper(SerializationInfo info, StreamingContext context)
        {
            ArgumentNullException.ThrowIfNull(info);

            _scriptText = info.GetValue("ScriptText", typeof(string)) as string;
            if (_scriptText == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(info));
            }
        }

        /// <summary>
        /// Returns a script block that corresponds to the version deserialized.
        /// </summary>
        /// <param name="context">The streaming context for this instance.</param>
        /// <returns>A script block that corresponds to the version deserialized.</returns>
        public object GetRealObject(StreamingContext context) => ScriptBlock.Create(_scriptText);

        /// <summary>
        /// Implements the ISerializable contract for serializing a scriptblock.
        /// </summary>
        /// <param name="info">Serialization information for this instance.</param>
        /// <param name="context">The streaming context for this instance.</param>
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
            => throw new NotSupportedException();
    }

    internal sealed class PSScriptCmdlet : PSCmdlet, IDynamicParameters, IDisposable
    {
        private readonly ArrayList _input = new ArrayList();
        private readonly ScriptBlock _scriptBlock;
        private readonly bool _fromScriptFile;
        private readonly bool _useLocalScope;
        private readonly bool _runOptimized;
        private readonly bool _rethrowExitException;
        private readonly MutableTuple _localsTuple;
        private readonly FunctionContext _functionContext;

        private MshCommandRuntime _commandRuntime;
        private bool _exitWasCalled;
        private bool _anyClauseExecuted;

        public PSScriptCmdlet(ScriptBlock scriptBlock, bool useNewScope, bool fromScriptFile, ExecutionContext context)
        {
            _scriptBlock = scriptBlock;
            _useLocalScope = useNewScope;
            _fromScriptFile = fromScriptFile;
            _runOptimized = _scriptBlock.Compile(optimized: context._debuggingMode <= 0 && useNewScope);
            _localsTuple = _scriptBlock.MakeLocalsTuple(_runOptimized);
            _localsTuple.SetAutomaticVariable(AutomaticVariable.PSCmdlet, this, context);
            _scriptBlock.SetPSScriptRootAndPSCommandPath(_localsTuple, context);
            _functionContext = new FunctionContext
            {
                _localsTuple = _localsTuple,
                _scriptBlock = _scriptBlock,
                _file = _scriptBlock.File,
                _sequencePoints = _scriptBlock.SequencePoints,
                _debuggerHidden = _scriptBlock.DebuggerHidden,
                _debuggerStepThrough = _scriptBlock.DebuggerStepThrough,
                _executionContext = context,
            };
            _rethrowExitException = context.ScriptCommandProcessorShouldRethrowExit;
            context.ScriptCommandProcessorShouldRethrowExit = false;
        }

        protected override void BeginProcessing()
        {
            // commandRuntime and the execution context are set here and in GetDynamicParameters.  GetDynamicParameters isn't
            // called unless the scriptblock has a dynamicparam block, so we must set these values in both places.  It'd
            // be cleaner to set in the constructor, but neither the commandRuntime nor the context are set when being constructed.
            _commandRuntime = (MshCommandRuntime)commandRuntime;

            // We don't set the output pipe until after GetDynamicParameters because dynamic parameters aren't written to the
            // command processors pipe, but once we enter begin, we will write to the default pipe.
            _functionContext._outputPipe = _commandRuntime.OutputPipe;

            SetPreferenceVariables();

            if (_scriptBlock.HasBeginBlock)
            {
                RunClause(
                    clause: _runOptimized ? _scriptBlock.BeginBlock : _scriptBlock.UnoptimizedBeginBlock,
                    dollarUnderbar: AutomationNull.Value,
                    inputToProcess: _input);
            }
        }

        internal override void DoProcessRecord()
        {
            if (_exitWasCalled)
            {
                return;
            }

            object dollarUnder;
            if (CurrentPipelineObject == AutomationNull.Value)
            {
                dollarUnder = null;
            }
            else
            {
                dollarUnder = CurrentPipelineObject;
                _input.Add(dollarUnder);
            }

            if (_scriptBlock.HasProcessBlock)
            {
                RunClause(
                    clause: _runOptimized ? _scriptBlock.ProcessBlock : _scriptBlock.UnoptimizedProcessBlock,
                    dollarUnderbar: dollarUnder,
                    inputToProcess: _input);
                _input.Clear();
            }
        }

        internal override void DoEndProcessing()
        {
            if (_exitWasCalled)
            {
                return;
            }

            if (_scriptBlock.HasEndBlock)
            {
                RunClause(
                    clause: _runOptimized ? _scriptBlock.EndBlock : _scriptBlock.UnoptimizedEndBlock,
                    dollarUnderbar: AutomationNull.Value,
                    inputToProcess: _input.ToArray());
            }
        }

        internal override void DoCleanResource()
        {
            if (_scriptBlock.HasCleanBlock && _anyClauseExecuted)
            {
                // The 'Clean' block doesn't write any output to pipeline, so we use a 'NullPipe' here and
                // disallow the output to be collected by an 'out' variable. However, the error, warning,
                // and information records should still be collectable by the corresponding variables.
                Pipe oldOutputPipe = _commandRuntime.OutputPipe;
                _functionContext._outputPipe = _commandRuntime.OutputPipe = new Pipe
                {
                    NullPipe = true,
                    IgnoreOutVariableList = true,
                };

                try
                {
                    RunClause(
                        clause: _runOptimized ? _scriptBlock.CleanBlock : _scriptBlock.UnoptimizedCleanBlock,
                        dollarUnderbar: AutomationNull.Value,
                        inputToProcess: AutomationNull.Value);
                }
                finally
                {
                    _functionContext._outputPipe = _commandRuntime.OutputPipe = oldOutputPipe;
                }
            }
        }

        private void EnterScope()
        {
            _commandRuntime.SetVariableListsInPipe();
        }

        private void ExitScope()
        {
            _commandRuntime.RemoveVariableListsInPipe();
        }

        private void RunClause(Action<FunctionContext> clause, object dollarUnderbar, object inputToProcess)
        {
            _anyClauseExecuted = true;
            Pipe oldErrorOutputPipe = this.Context.ShellFunctionErrorOutputPipe;

            // If the script block has a different language mode than the current,
            // change the language mode.
            PSLanguageMode? oldLanguageMode = null;
            PSLanguageMode? newLanguageMode = null;
            if (_scriptBlock.LanguageMode.HasValue &&
                _scriptBlock.LanguageMode != Context.LanguageMode)
            {
                oldLanguageMode = Context.LanguageMode;
                newLanguageMode = _scriptBlock.LanguageMode;
            }

            try
            {
                try
                {
                    EnterScope();

                    if (_commandRuntime.ErrorMergeTo == MshCommandRuntime.MergeDataStream.Output)
                    {
                        Context.RedirectErrorPipe(_commandRuntime.OutputPipe);
                    }
                    else if (_commandRuntime.ErrorOutputPipe.IsRedirected)
                    {
                        Context.RedirectErrorPipe(_commandRuntime.ErrorOutputPipe);
                    }

                    if (dollarUnderbar != AutomationNull.Value)
                    {
                        _localsTuple.SetAutomaticVariable(AutomaticVariable.Underbar, dollarUnderbar, Context);
                    }

                    if (inputToProcess != AutomationNull.Value)
                    {
                        _localsTuple.SetAutomaticVariable(AutomaticVariable.Input, inputToProcess, Context);
                    }

                    // Set the language mode
                    if (newLanguageMode.HasValue)
                    {
                        Context.LanguageMode = newLanguageMode.Value;
                    }

                    clause(_functionContext);
                }
                catch (TargetInvocationException tie)
                {
                    // DynamicInvoke wraps exceptions, unwrap them here.
                    throw tie.InnerException;
                }
                finally
                {
                    Context.ShellFunctionErrorOutputPipe = oldErrorOutputPipe;

                    // Restore the language mode
                    if (oldLanguageMode.HasValue)
                    {
                        Context.LanguageMode = oldLanguageMode.Value;
                    }

                    ExitScope();
                }
            }
            catch (ExitException ee)
            {
                if (!_fromScriptFile || _rethrowExitException)
                {
                    throw;
                }

                _exitWasCalled = true;

                int exitCode = (int)ee.Argument;
                this.Context.SetVariable(SpecialVariables.LastExitCodeVarPath, exitCode);

                if (exitCode != 0)
                {
                    _commandRuntime.PipelineProcessor.ExecutionFailed = true;
                }
            }
        }

        public object GetDynamicParameters()
        {
            _commandRuntime = (MshCommandRuntime)commandRuntime;

            if (_scriptBlock.HasDynamicParameters)
            {
                var resultList = new List<object>();
                Diagnostics.Assert(_functionContext._outputPipe == null, "Output pipe should not be set yet.");
                _functionContext._outputPipe = new Pipe(resultList);
                RunClause(
                    clause: _runOptimized ? _scriptBlock.DynamicParamBlock : _scriptBlock.UnoptimizedDynamicParamBlock,
                    dollarUnderbar: AutomationNull.Value,
                    inputToProcess: AutomationNull.Value);
                if (resultList.Count > 1)
                {
                    throw PSTraceSource.NewInvalidOperationException(
                        AutomationExceptions.DynamicParametersWrongType,
                        PSObject.ToStringParser(this.Context, resultList));
                }

                return resultList.Count == 0 ? null : PSObject.Base(resultList[0]);
            }

            return null;
        }

        /// <summary>
        /// If the script cmdlet will run in a new local scope, this method is used to set the locals to the newly created scope.
        /// </summary>
        internal void SetLocalsTupleForNewScope(SessionStateScope scope)
        {
            Diagnostics.Assert(scope.LocalsTuple == null, "a newly created scope shouldn't have it's tuple set.");
            scope.LocalsTuple = _localsTuple;
        }

        /// <summary>
        /// If the script cmdlet is dotted, this method is used to push the locals to the 'DottedScopes' of the current scope.
        /// </summary>
        internal void PushDottedScope(SessionStateScope scope) => scope.DottedScopes.Push(_localsTuple);

        /// <summary>
        /// If the script cmdlet is dotted, this method is used to pop the locals from the 'DottedScopes' of the current scope.
        /// </summary>
        internal void PopDottedScope(SessionStateScope scope) => scope.DottedScopes.Pop();

        internal void PrepareForBinding(CommandLineParameters commandLineParameters)
        {
            _localsTuple.SetAutomaticVariable(
                AutomaticVariable.PSBoundParameters,
                value: commandLineParameters.GetValueToBindToPSBoundParameters(),
                this.Context);
            _localsTuple.SetAutomaticVariable(AutomaticVariable.MyInvocation, value: MyInvocation, this.Context);
        }

        private void SetPreferenceVariables()
        {
            if (_commandRuntime.IsDebugFlagSet)
            {
                _localsTuple.SetPreferenceVariable(
                    PreferenceVariable.Debug,
                    _commandRuntime.Debug ? ActionPreference.Continue : ActionPreference.SilentlyContinue);
            }

            if (_commandRuntime.IsVerboseFlagSet)
            {
                _localsTuple.SetPreferenceVariable(
                    PreferenceVariable.Verbose,
                    _commandRuntime.Verbose ? ActionPreference.Continue : ActionPreference.SilentlyContinue);
            }

            if (_commandRuntime.IsErrorActionSet)
            {
                _localsTuple.SetPreferenceVariable(PreferenceVariable.Error, _commandRuntime.ErrorAction);
            }

            if (_commandRuntime.IsWarningActionSet)
            {
                _localsTuple.SetPreferenceVariable(PreferenceVariable.Warning, _commandRuntime.WarningPreference);
            }

            if (_commandRuntime.IsInformationActionSet)
            {
                _localsTuple.SetPreferenceVariable(PreferenceVariable.Information, _commandRuntime.InformationPreference);
            }

            if (_commandRuntime.IsProgressActionSet)
            {
                _localsTuple.SetPreferenceVariable(PreferenceVariable.Progress, _commandRuntime.ProgressPreference);
            }

            if (_commandRuntime.IsWhatIfFlagSet)
            {
                _localsTuple.SetPreferenceVariable(PreferenceVariable.WhatIf, _commandRuntime.WhatIf);
            }

            if (_commandRuntime.IsConfirmFlagSet)
            {
                _localsTuple.SetPreferenceVariable(
                    PreferenceVariable.Confirm,
                    _commandRuntime.Confirm ? ConfirmImpact.Low : ConfirmImpact.None);
            }
        }

        #region StopProcessing functionality for script cmdlets

        internal event EventHandler StoppingEvent;

        protected override void StopProcessing()
        {
            this.StoppingEvent.SafeInvoke(this, EventArgs.Empty);
            base.StopProcessing();
        }

        #endregion

        #region IDispose

        private bool _disposed;

        internal event EventHandler DisposingEvent;

        /// <summary>
        /// IDisposable implementation
        /// When the command is complete, release the associated scope
        /// and other members.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            this.DisposingEvent.SafeInvoke(this, EventArgs.Empty);
            commandRuntime = null;
            currentObjectInPipeline = null;
            _input.Clear();

            base.InternalDispose(true);
            _disposed = true;
        }

        #endregion IDispose
    }
}
