/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Tracing;
using System.Security.Cryptography.X509Certificates;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PowerShell.Telemetry.Internal;

#if CORECLR
// Use stub for Serializable attribute and ISerializable related types
using Microsoft.PowerShell.CoreClr.Stubs;
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
            this.IsProductCode = isProductCode;
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
                    attributes = Utils.EmptyArray<Attribute>();
                }
                else
                {
                    attributes = Ast.GetScriptBlockAttributes().ToArray();
                    foreach (var attribute in attributes)
                    {
                        if (attribute is CmdletBindingAttribute)
                        {
                            cmdletBindingAttribute = cmdletBindingAttribute ?? (CmdletBindingAttribute)attribute;
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
                var runtimeDefinedParameterDictionary = Ast.GetParameterMetadata(automaticPosition, ref _usesCmdletBinding);

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
            var sw = new Stopwatch();
            sw.Start();

            if (!IsProductCode && SecuritySupport.IsProductBinary(((Ast)_ast).Extent.File))
            {
                this.IsProductCode = true;
            }

            bool etwEnabled = ParserEventSource.Log.IsEnabled();
            if (etwEnabled)
            {
                var extent = _ast.Body.Extent;
                var text = extent.Text;
                ParserEventSource.Log.CompileStart(ParserEventSource.GetFileOrScript(extent.File, text), text.Length, optimize);
            }

            PerformSecurityChecks();

            Compiler compiler = new Compiler();
            compiler.Compile(this, optimize);

            if (!IsProductCode)
            {
                TelemetryAPI.ReportScriptTelemetry((Ast)_ast, !optimize, sw.ElapsedMilliseconds);
            }

            if (etwEnabled) ParserEventSource.Log.CompileStop();
        }

        private void PerformSecurityChecks()
        {
            var scriptBlockAst = Ast as ScriptBlockAst;
            if (scriptBlockAst == null)
            {
                // Checks are only needed at the top level.
                return;
            }

            // Call the AMSI API to determine if the script block has malicious content
            var scriptExtent = scriptBlockAst.Extent;
            if (AmsiUtils.ScanContent(scriptExtent.Text, scriptExtent.File) == AmsiUtils.AmsiNativeMethods.AMSI_RESULT.AMSI_RESULT_DETECTED)
            {
                var parseError = new ParseError(scriptExtent, "ScriptContainedMaliciousContent", ParserStrings.ScriptContainedMaliciousContent);
                throw new ParseException(new[] { parseError });
            }

            if (ScriptBlock.CheckSuspiciousContent(scriptBlockAst) != null)
            {
                HasSuspiciousContent = true;
            }
        }

        // We delay parsing scripts loaded on startup, so we save the text.
        private string _scriptText;
        internal IParameterMetadataProvider Ast { get { return _ast ?? DelayParseScriptText(); } }
        private IParameterMetadataProvider _ast;

        private IParameterMetadataProvider DelayParseScriptText()
        {
            lock (this)
            {
                if (_ast != null)
                    return _ast;

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

        internal Action<FunctionContext> DynamicParamBlock { get; set; }
        internal Action<FunctionContext> UnoptimizedDynamicParamBlock { get; set; }
        internal Action<FunctionContext> BeginBlock { get; set; }
        internal Action<FunctionContext> UnoptimizedBeginBlock { get; set; }
        internal Action<FunctionContext> ProcessBlock { get; set; }
        internal Action<FunctionContext> UnoptimizedProcessBlock { get; set; }
        internal Action<FunctionContext> EndBlock { get; set; }
        internal Action<FunctionContext> UnoptimizedEndBlock { get; set; }

        internal IScriptExtent[] SequencePoints { get; set; }
        private RuntimeDefinedParameterDictionary _runtimeDefinedParameterDictionary;
        private Attribute[] _attributes;
        private bool _usesCmdletBinding;
        private bool _compiledOptimized;
        private bool _compiledUnoptimized;
        private bool _hasSuspiciousContent;
        internal bool DebuggerHidden { get; set; }
        internal bool DebuggerStepThrough { get; set; }
        internal Guid Id { get; private set; }
        internal bool HasLogged { get; set; }
        internal bool IsFilter { get; private set; }
        internal bool IsProductCode { get; private set; }

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
                Diagnostics.Assert(_compiledOptimized || _compiledUnoptimized, "HasSuspiciousContent is not set correctly before being compiled");
                return _hasSuspiciousContent;
            }
            set { _hasSuspiciousContent = value; }
        }

        private MergedCommandParameterMetadata _parameterMetadata;

        internal List<Attribute> GetAttributes()
        {
            if (_attributes == null)
            {
                InitializeMetadata();
            }

            Diagnostics.Assert(_attributes != null, "after initialization, attributes is never null, must be an empty list if no attributes.");
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

                return _usesCmdletBinding ? (CmdletBindingAttribute)_attributes.FirstOrDefault(attr => attr is CmdletBindingAttribute) : null;
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

                return (ObsoleteAttribute)_attributes.FirstOrDefault(attr => attr is ObsoleteAttribute);
            }
        }

        public MergedCommandParameterMetadata GetParameterMetadata(ScriptBlock scriptBlock)
        {
            if (_parameterMetadata == null)
            {
                lock (this)
                {
                    if (_parameterMetadata == null)
                    {
                        CommandMetadata metadata = new CommandMetadata(scriptBlock, "", LocalPipeline.GetExecutionContextFromTLS());
                        _parameterMetadata = metadata.StaticCommandParameterMetadata;
                    }
                }
            }
            return _parameterMetadata;
        }

        public override string ToString()
        {
            if (_scriptText != null)
                return _scriptText;

            var sbAst = _ast as ScriptBlockAst;
            if (sbAst != null)
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

        internal ScriptBlock(IParameterMetadataProvider ast, bool isFilter) :
            this(new CompiledScriptBlockData(ast, isFilter))
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
                Diagnostics.Assert(scriptBlock.SessionStateInternal == null,
                                   "A cached scriptblock should not have it's session state bound, that causes a memory leak.");
                return scriptBlock.Clone();
            }
            return null;
        }

        private static bool IsDynamicKeyword(Ast ast)
        {
            var cmdAst = ast as CommandAst;
            return (cmdAst != null && cmdAst.DefiningKeyword != null);
        }

        private static bool IsUsingTypes(Ast ast)
        {
            var cmdAst = ast as UsingStatementAst;
            return (cmdAst != null && cmdAst.IsUsingModuleOrAssembly());
        }

        internal static void CacheScriptBlock(ScriptBlock scriptBlock, string fileName, string fileContents)
        {
            if (InternalTestHooks.IgnoreScriptBlockCache)
            {
                return;
            }

            //
            // Don't cache scriptblocks that have 
            // a) dynamic keywords 
            // b) 'using module' or 'using assembly'
            // The definition of the dynamic keyword could change, consequently changing how the source text should be parsed.
            // Exported types definitions from 'using module' could change, we need to do all parse-time checks again.
            // TODO(sevoroby): we can optimize it to ignore 'using' if there are no actual type usage in locally defined types.
            //

            // using is always a top-level statements in scriptBlock, we don't need to search in child blocks.
            if (scriptBlock.Ast.Find(ast => IsUsingTypes(ast), false) != null ||
                scriptBlock.Ast.Find(ast => IsDynamicKeyword(ast), true) != null)
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
        /// Clears the cached scriptblocks
        /// </summary>
        internal static void ClearScriptBlockCache()
        {
            s_cachedScripts.Clear();
        }

        internal static ScriptBlock EmptyScriptBlock = ScriptBlock.CreateDelayParsedScriptBlock("", isProductCode: true);

        internal static ScriptBlock Create(Parser parser, string fileName, string fileContents)
        {
            var scriptBlock = TryGetCachedScriptBlock(fileName, fileContents);
            if (scriptBlock != null)
            {
                return scriptBlock;
            }

            ParseError[] errors;
            var ast = parser.Parse(fileName, fileContents, null, out errors, ParseMode.Default);
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

        internal ScriptBlock Clone()
        {
            return new ScriptBlock(_scriptBlockData);
        }

        /// <summary>
        /// Returns the text of the script block.  The return value might not match the original text exactly.
        /// </summary>
        public override string ToString()
        {
            return _scriptBlockData.ToString();
        }

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
                throw PSTraceSource.NewArgumentNullException("info");
            }

            string serializedContent = this.ToString();
            info.AddValue("ScriptText", serializedContent);
            info.SetType(typeof(ScriptBlockSerializationHelper));
        }

        internal PowerShell GetPowerShellImpl(ExecutionContext context, Dictionary<string, object> variables, bool isTrustedInput,
            bool filterNonUsingVariables, bool? createLocalScope, params object[] args)
        {
            return AstInternal.GetPowerShell(context, variables, isTrustedInput, filterNonUsingVariables, createLocalScope, args);
        }

        internal SteppablePipeline GetSteppablePipelineImpl(CommandOrigin commandOrigin, object[] args)
        {
            var pipelineAst = GetSimplePipeline(resourceString => { throw PSTraceSource.NewInvalidOperationException(resourceString); });
            Diagnostics.Assert(pipelineAst != null, "This should be checked by GetSimplePipeline");

            if (!(pipelineAst.PipelineElements[0] is CommandAst))
            {
                throw PSTraceSource.NewInvalidOperationException(
                    AutomationExceptions.CantConvertEmptyPipeline);
            }

            return PipelineOps.GetSteppablePipeline(pipelineAst, commandOrigin, this, args);
        }

        private PipelineAst GetSimplePipeline(Func<string, PipelineAst> errorHandler)
        {
            errorHandler = errorHandler ?? (_ => null);

            if (HasBeginBlock || HasProcessBlock)
            {
                return errorHandler(AutomationExceptions.CanConvertOneClauseOnly);
            }

            var ast = AstInternal;
            var statements = ast.Body.EndBlock.Statements;
            if (!statements.Any())
            {
                return errorHandler(AutomationExceptions.CantConvertEmptyPipeline);
            }
            if (statements.Count > 1)
            {
                return errorHandler(AutomationExceptions.CanOnlyConvertOnePipeline);
            }
            if (ast.Body.EndBlock.Traps != null && ast.Body.EndBlock.Traps.Any())
            {
                return errorHandler(AutomationExceptions.CantConvertScriptBlockWithTrap);
            }

            var pipeAst = statements[0] as PipelineAst;
            if (pipeAst == null)
            {
                return errorHandler(AutomationExceptions.CanOnlyConvertOnePipeline);
            }

            // The old code checked for empty pipeline.
            // That can't happen in the new parser (validated in the constructors),
            // so the resource CantConvertEmptyPipeline is probably unused.

            return pipeAst;
        }

        internal List<Attribute> GetAttributes()
        {
            return _scriptBlockData.GetAttributes();
        }

        internal string GetFileName()
        {
            return AstInternal.Body.Extent.File;
        }

        internal bool IsMetaConfiguration()
        {
            // GetAttributes() is asserted never return null
            return GetAttributes().OfType<DscLocalConfigurationManagerAttribute>().Any();
        }

        internal PSToken GetStartPosition()
        {
            return new PSToken(Ast.Extent);
        }

        internal MergedCommandParameterMetadata ParameterMetadata
        {
            get { return _scriptBlockData.GetParameterMetadata(this); }
        }

        internal bool UsesCmdletBinding
        {
            get { return _scriptBlockData.UsesCmdletBinding; }
        }

        internal bool HasDynamicParameters
        {
            get { return AstInternal.Body.DynamicParamBlock != null; }
        }

        /// <summary>
        /// DebuggerHidden
        /// </summary>
        public bool DebuggerHidden
        {
            get { return _scriptBlockData.DebuggerHidden; }
            set { _scriptBlockData.DebuggerHidden = value; }
        }

        /// <summary>
        /// The unique ID of this script block.
        /// </summary>
        public Guid Id
        {
            get { return _scriptBlockData.Id; }
        }

        internal bool DebuggerStepThrough
        {
            get { return _scriptBlockData.DebuggerStepThrough; }
            set { _scriptBlockData.DebuggerStepThrough = value; }
        }

        internal RuntimeDefinedParameterDictionary RuntimeDefinedParameters
        {
            get { return _scriptBlockData.RuntimeDefinedParameters; }
        }

        internal bool HasLogged
        {
            get { return _scriptBlockData.HasLogged; }
            set { _scriptBlockData.HasLogged = value; }
        }

        internal Assembly AssemblyDefiningPSTypes { set; get; }

        internal HelpInfo GetHelpInfo(ExecutionContext context, CommandInfo commandInfo, bool dontSearchOnRemoteComputer,
            Dictionary<Ast, Token[]> scriptBlockTokenCache, out string helpFile, out string helpUriFromDotLink)
        {
            helpUriFromDotLink = null;

            var commentTokens = HelpCommentsParser.GetHelpCommentTokens(AstInternal, scriptBlockTokenCache);
            if (commentTokens != null)
            {
                return HelpCommentsParser.CreateFromComments(context, commandInfo, commentTokens.Item1,
                                                             commentTokens.Item2, dontSearchOnRemoteComputer, out helpFile, out helpUriFromDotLink);
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
        public void CheckRestrictedLanguage(IEnumerable<string> allowedCommands, IEnumerable<string> allowedVariables, bool allowEnvironmentVariables)
        {
            Parser parser = new Parser();

            var ast = AstInternal;
            if (HasBeginBlock || HasProcessBlock || ast.Body.ParamBlock != null)
            {
                Ast errorAst = ast.Body.BeginBlock ?? (Ast)ast.Body.ProcessBlock ?? ast.Body.ParamBlock;
                parser.ReportError(errorAst.Extent, () => ParserStrings.InvalidScriptBlockInDataSection);
            }

            if (HasEndBlock)
            {
                RestrictedLanguageChecker rlc = new RestrictedLanguageChecker(parser, allowedCommands, allowedVariables, allowEnvironmentVariables);
                var endBlock = ast.Body.EndBlock;
                StatementBlockAst.InternalVisit(rlc, endBlock.Traps, endBlock.Statements, AstVisitAction.Continue);
            }

            if (parser.ErrorList.Any())
            {
                throw new ParseException(parser.ErrorList.ToArray());
            }
        }

        internal string GetWithInputHandlingForInvokeCommand()
        {
            return AstInternal.GetWithInputHandlingForInvokeCommand();
        }

        internal string GetWithInputHandlingForInvokeCommandWithUsingExpression(
            Tuple<List<VariableExpressionAst>, string> usingVariablesTuple)
        {
            Tuple<string, string> result =
                AstInternal.GetWithInputHandlingForInvokeCommandWithUsingExpression(usingVariablesTuple);

            // result.Item1 is ParamText; result.Item2 is ScriptBlockText
            if (result.Item1 == null)
                return result.Item2;
            return result.Item1 + result.Item2;
        }

        internal bool IsUsingDollarInput()
        {
            return AstSearcher.IsUsingDollarInput(this.Ast);
        }

        internal void InvokeWithPipeImpl(bool createLocalScope,
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
            InvokeWithPipeImpl(ScriptBlockClauseToInvoke.ProcessBlockOnly, createLocalScope,
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

        internal void InvokeWithPipeImpl(ScriptBlockClauseToInvoke clauseToInvoke,
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
            if (clauseToInvoke == ScriptBlockClauseToInvoke.Begin && !HasBeginBlock)
            {
                return;
            }
            else if (clauseToInvoke == ScriptBlockClauseToInvoke.Process && !HasProcessBlock)
            {
                return;
            }
            else if (clauseToInvoke == ScriptBlockClauseToInvoke.End && !HasEndBlock)
            {
                return;
            }

            ExecutionContext context = GetContextFromTLS();
            Diagnostics.Assert(SessionStateInternal == null || SessionStateInternal.ExecutionContext == context,
                               "The scriptblock is being invoked in a runspace different than the one where it was created");

            if (context.CurrentPipelineStopping)
            {
                throw new PipelineStoppedException();
            }

            // Validate at the arguments are consistent. The only public API that gets you here never sets createLocalScope to false...
            Diagnostics.Assert(createLocalScope == true || functionsToDefine == null, "When calling ScriptBlock.InvokeWithContext(), if 'functionsToDefine' != null then 'createLocalScope' must be true");
            Diagnostics.Assert(createLocalScope == true || variablesToDefine == null, "When calling ScriptBlock.InvokeWithContext(), if 'variablesToDefine' != null then 'createLocalScope' must be true");

            if (args == null)
            {
                args = Utils.EmptyArray<object>();
            }

            bool runOptimized = context._debuggingMode > 0 ? false : createLocalScope;
            var codeToInvoke = GetCodeToInvoke(ref runOptimized, clauseToInvoke);
            if (codeToInvoke == null)
                return;

            if (outputPipe == null)
            {
                // If we don't have a pipe to write to, we need to discard all results.
                outputPipe = new Pipe { NullPipe = true };
            }

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

            // If the script block has a different language mode than the current,
            // change the language mode.
            PSLanguageMode? oldLanguageMode = null;
            PSLanguageMode? newLanguageMode = null;
            if ((this.LanguageMode.HasValue) &&
                (this.LanguageMode != context.LanguageMode))
            {
                oldLanguageMode = context.LanguageMode;
                newLanguageMode = this.LanguageMode;
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
                    context.EngineSessionState = SessionStateInternal;

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
                            Diagnostics.Assert(!(string.Equals(name, "this") || string.Equals(name, "_") || string.Equals(name, "input")),
                                "The list of variables to set in the scriptblock's scope cannot contain 'this', '_' or 'input'. These variables shoujld be removed before passing the collection to this routine.");
                            index++;
                            newScope.Variables.Add(name, psvar);
                        }
                    }
                }
                else
                {
                    if (context.EngineSessionState.CurrentScope.LocalsTuple == null)
                    {
                        // If the locals tuple is, that means either:
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
                    args, context, !createLocalScope, backupWhenDotting, locals);
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

                ScriptBlock.LogScriptBlockStart(this, context.CurrentRunspace.InstanceId);

                try
                {
                    codeToInvoke(functionContext);
                }
                finally
                {
                    ScriptBlock.LogScriptBlockEnd(this, context.CurrentRunspace.InstanceId);
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
                locals = MutableTuple.MakeTuple(_scriptBlockData.LocalsMutableTupleType, _scriptBlockData.NameToIndexMap, _scriptBlockData.LocalsMutableTupleCreator);
            }
            else
            {
                locals = MutableTuple.MakeTuple(_scriptBlockData.UnoptimizedLocalsMutableTupleType,
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
                    backupWhenDotting[parameter.Name] = context.EngineSessionState.GetVariableAtScope(parameter.Name, "local");
                }
                else
                {
                    valueSet = locals.TrySetParameter(parameter.Name, valueToBind);
                }

                if (!valueSet)
                {
                    var variable = new PSVariable(parameter.Name, valueToBind, ScopedItemOptions.None, parameter.Attributes);
                    context.EngineSessionState.SetVariable(variable, false, CommandOrigin.Internal);
                }

                if (!wasDefaulted)
                {
                    boundParameters.Add(parameter.Name, valueToBind);
                    boundParameters.MarkAsBoundPositionally(parameter.Name);
                }
            }

            locals.SetAutomaticVariable(AutomaticVariable.PSBoundParameters, boundParameters.GetValueToBindToPSBoundParameters(), context);

            var leftOverArgs = args.Length - parameters.Length;
            if (leftOverArgs <= 0)
            {
                return Utils.EmptyArray<object>();
            }

            object[] result = new object[leftOverArgs];
            Array.Copy(args, parameters.Length, result, 0, result.Length);
            return result;
        }

        internal static void SetAutomaticVariable(AutomaticVariable variable, object value, MutableTuple locals)
        {
            locals.SetValue((int)variable, value);
        }

        private Action<FunctionContext> GetCodeToInvoke(ref bool optimized, ScriptBlockClauseToInvoke clauseToInvoke)
        {
            if (clauseToInvoke == ScriptBlockClauseToInvoke.ProcessBlockOnly && (HasBeginBlock || (HasEndBlock && HasProcessBlock)))
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
                default:
                    return HasProcessBlock ? _scriptBlockData.UnoptimizedProcessBlock : _scriptBlockData.UnoptimizedEndBlock;
            }
        }

        internal CmdletBindingAttribute CmdletBindingAttribute
        {
            get { return _scriptBlockData.CmdletBindingAttribute; }
        }

        internal ObsoleteAttribute ObsoleteAttribute
        {
            get { return _scriptBlockData.ObsoleteAttribute; }
        }

        internal bool Compile(bool optimized)
        {
            return _scriptBlockData.Compile(optimized);
        }

        internal static void LogScriptBlockCreation(ScriptBlock scriptBlock, bool force)
        {
            if (force || ShouldLogScriptBlockActivity("EnableScriptBlockLogging"))
            {
                if (!scriptBlock.HasLogged || InternalTestHooks.ForceScriptBlockLogging)
                {
                    // If script block logging is explicitly disabled, or it's from a trusted
                    // file or internal, skip logging.
                    if (ScriptBlockLoggingExplicitlyDisabled() ||
                        scriptBlock.ScriptBlockData.IsProductCode)
                    {
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
            }
        }

        private static bool WriteScriptBlockToLog(ScriptBlock scriptBlock, int segment, int segments, string textToLog)
        {
            // See if we need to encrypt the event log message. This info is all cached by Utils.GetGroupPolicySetting(),
            // so we're not hitting the registry for every script block we compile.
            Dictionary<string, object> protectedEventLoggingSettings = Utils.GetGroupPolicySetting(
                "Software\\Policies\\Microsoft\\Windows\\EventLog", "ProtectedEventLogging", Utils.RegLocalMachine);
            if (protectedEventLoggingSettings != null)
            {
                lock (s_syncObject)
                {
                    // Populates the encryptionRecipients list from the Group Policy, if possible. If not possible,
                    // does all appropriate logging and encryptionRecipients is 'null'. 'CouldLog' being false
                    // implies the engine wasn't ready for logging yet.
                    bool couldLog = GetAndValidateEncryptionRecipients(scriptBlock);
                    if (!couldLog)
                    {
                        return false;
                    }

                    // If we have recipients to encrypt to, then do so. Otherwise, we'll just log the plain text
                    // version.
                    if (s_encryptionRecipients != null)
                    {
                        ExecutionContext executionContext = LocalPipeline.GetExecutionContextFromTLS();
                        ErrorRecord error = null;
                        byte[] contentBytes = System.Text.Encoding.UTF8.GetBytes(textToLog);
                        string encodedContent = CmsUtils.Encrypt(contentBytes, s_encryptionRecipients, executionContext.SessionState, out error);

                        // Can't cache the reporting of encryption errors, as they are likely content-based.
                        if (error != null)
                        {
                            // If we got an error encrypting the content, log an error and continue
                            // logging the (unencrypted) message anyways. Logging trumps protected logging -
                            // being able to detect that an attacker has compromised a box outweighs the danger of the
                            // attacker seeing potentially sensitive data. Because if they aren't detected, then
                            // they can just wait on the compromised box and see the sensitive data eventually anyways.

                            string errorMessage = StringUtil.Format(SecuritySupportStrings.CouldNotEncryptContent, textToLog, error.ToString());
                            PSEtwLog.LogOperationalError(PSEventId.ScriptBlock_Compile_Detail, PSOpcode.Create, PSTask.ExecuteCommand, PSKeyword.UseAlwaysAnalytic,
                                            0, 0, errorMessage, scriptBlock.Id.ToString(), scriptBlock.File ?? String.Empty);
                        }
                        else
                        {
                            textToLog = encodedContent;
                        }
                    }
                }
            }

            if (scriptBlock._scriptBlockData.HasSuspiciousContent)
            {
                PSEtwLog.LogOperationalWarning(PSEventId.ScriptBlock_Compile_Detail, PSOpcode.Create, PSTask.ExecuteCommand, PSKeyword.UseAlwaysAnalytic,
                    segment + 1, segments, textToLog, scriptBlock.Id.ToString(), scriptBlock.File ?? String.Empty);
            }
            else
            {
                PSEtwLog.LogOperationalVerbose(PSEventId.ScriptBlock_Compile_Detail, PSOpcode.Create, PSTask.ExecuteCommand, PSKeyword.UseAlwaysAnalytic,
                    segment + 1, segments, textToLog, scriptBlock.Id.ToString(), scriptBlock.File ?? String.Empty);
            }

            return true;
        }

        private static bool GetAndValidateEncryptionRecipients(ScriptBlock scriptBlock)
        {
            Dictionary<string, object> protectedEventLoggingSettings = Utils.GetGroupPolicySetting(
                "Software\\Policies\\Microsoft\\Windows\\EventLog", "ProtectedEventLogging", Utils.RegLocalMachine);

            // See if protected event logging is enabled
            object enableProtectedEventLogging = null;
            if (protectedEventLoggingSettings.TryGetValue("EnableProtectedEventLogging", out enableProtectedEventLogging))
            {
                if (String.Equals("1", enableProtectedEventLogging.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    // Get the encryption certificate
                    object encryptionCertificate = null;
                    if (protectedEventLoggingSettings.TryGetValue("EncryptionCertificate", out encryptionCertificate))
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

                        string[] encryptionCertificateContent = encryptionCertificate as string[];
                        string fullCertificateContent = null;
                        if (encryptionCertificateContent != null)
                        {
                            fullCertificateContent = String.Join(Environment.NewLine, encryptionCertificateContent);
                        }
                        else
                        {
                            fullCertificateContent = encryptionCertificate as string;
                        }

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
                            string errorMessage = StringUtil.Format(SecuritySupportStrings.CouldNotUseCertificate, error.ToString());
                            PSEtwLog.LogOperationalError(PSEventId.ScriptBlock_Compile_Detail, PSOpcode.Create, PSTask.ExecuteCommand, PSKeyword.UseAlwaysAnalytic,
                                            0, 0, errorMessage, scriptBlock.Id.ToString(), scriptBlock.File ?? String.Empty);

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
                                // Only log the first line of what we pulled from the registry. If this is a path, this will have enough information.
                                // If this is the actual certificate, only include the first line of the certificate content so that we're not permanently keeping the private
                                // key in the log.
                                string certificateForLog = fullCertificateContent;
                                if ((encryptionCertificateContent != null) && (encryptionCertificateContent.Length > 1))
                                {
                                    certificateForLog = encryptionCertificateContent[1];
                                }

                                string errorMessage = StringUtil.Format(SecuritySupportStrings.CertificateContainsPrivateKey, certificateForLog);
                                PSEtwLog.LogOperationalError(PSEventId.ScriptBlock_Compile_Detail, PSOpcode.Create, PSTask.ExecuteCommand, PSKeyword.UseAlwaysAnalytic,
                                                0, 0, errorMessage, scriptBlock.Id.ToString(), scriptBlock.File ?? String.Empty);
                            }
                        }
                    }
                }
            }

            return true;
        }

        private static object s_syncObject = new Object();
        private static string s_lastSeenCertificate = String.Empty;
        private static bool s_hasProcessedCertificate = false;
        private static CmsMessageRecipient[] s_encryptionRecipients = null;

        // Reset any static caches if the certificate has changed
        private static void ResetCertificateCacheIfNeeded(string certificate)
        {
            if (!String.Equals(s_lastSeenCertificate, certificate, StringComparison.Ordinal))
            {
                s_hasProcessedCertificate = false;
                s_lastSeenCertificate = certificate;
                s_encryptionRecipients = null;
            }
        }

        private static bool ShouldLogScriptBlockActivity(string activity)
        {
            // If script block logging is turned on, log this one.
            Dictionary<string, object> groupPolicySettings = Utils.GetGroupPolicySetting("ScriptBlockLogging", Utils.RegLocalMachineThenCurrentUser);
            if (groupPolicySettings != null)
            {
                object logScriptBlockExecution = null;
                if (groupPolicySettings.TryGetValue(activity, out logScriptBlockExecution))
                {
                    if (String.Equals("1", logScriptBlockExecution.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // Quick check for script blocks that may have suspicious content. If this
        // is true, we log them to the event log despite event log settings.
        //
        // Performance notes:
        // 
        // For the current number of search terms, the this approach is about as high
        // performance as we can get. It adds about 1ms to the invocation of a script
        // block (we don't do this at parse time).
        // The manual tokenization is much faster than either Regex.Split
        // or a series of String.Split calls. Lookups in the HashSet are much faster
        // than a ton of calls to String.IndexOf (which .NET implements in native code).
        //
        // If we were to expand this set of keywords much farther, it would make sense
        // to look into implementing the Aho-Corasick algorithm (the one used by antimalware
        // engines), but Aho-Corasick is slower than the current approach for relatively
        // small match sets.
        internal static string CheckSuspiciousContent(Ast scriptBlockAst)
        {
            // Split the script block text into an array of elements that have
            // a-Z A-Z dash.
            string scriptBlockText = scriptBlockAst.Extent.Text;
            IEnumerable<string> elements = TokenizeWordElements(scriptBlockText);

            // First check for plain-text signatures
            ParallelOptions parallelOptions = new ParallelOptions();
            string foundSignature = null;

            Parallel.ForEach(elements, parallelOptions, (element, loopState) =>
            {
                if (foundSignature == null)
                {
                    if (s_signatures.Contains(element))
                    {
                        foundSignature = element;
                        loopState.Break();
                    }
                }
            });

            if (!String.IsNullOrEmpty(foundSignature))
            {
                return foundSignature;
            }

            if (scriptBlockAst.HasSuspiciousContent)
            {
                Ast foundAst = scriptBlockAst.Find(ast =>
                {
                    // Try to find the lowest AST that was not considered suspicious, but its parent
                    // was.
                    return (!ast.HasSuspiciousContent) && (ast.Parent.HasSuspiciousContent);
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

        // Extract tokens of a-z A-Z and dash
        private static IEnumerable<string> TokenizeWordElements(string scriptBlockText)
        {
            StringBuilder currentElement = new StringBuilder(100);

            foreach (char character in scriptBlockText)
            {
                if ((character >= 'a') &&
                   (character <= 'z'))
                {
                    // Capture lowercase a-z
                    currentElement.Append(character);
                    continue;
                }
                else if ((character >= 'A') &&
                   (character <= 'Z'))
                {
                    // Capture uppercase A-Z
                    currentElement.Append(character);
                    continue;
                }
                else if (character == '-')
                {
                    // Capture dash
                    currentElement.Append(character);
                    continue;
                }
                else
                {
                    // We hit a space or something else
                    // Only add if the current element is 4 characters or more
                    // (the length of the shortest string we're looking for)
                    if (currentElement.Length >= 4)
                    {
                        yield return currentElement.ToString();
                    }

                    currentElement.Clear();
                }
            }

            // Clean up any remaining tokens.
            if (currentElement.Length > 0)
            {
                yield return currentElement.ToString();
            }

            yield break;
        }

        // Regular string signatures that can be detected with just string comparison.
        private static HashSet<string> s_signatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            // Calling Add-Type
            "Add-Type", "DllImport",

            // Doing dynamic assembly building / method indirection
            "DefineDynamicAssembly", "DefineDynamicModule", "DefineType", "DefineConstructor", "CreateType",
            "DefineLiteral", "DefineEnum", "DefineField", "ILGenerator", "Emit", "UnverifiableCodeAttribute",
            "DefinePInvokeMethod", "GetTypes", "GetAssemblies", "Methods", "Properties",

            // Suspicious methods / properties on "Type"
            "GetConstructor", "GetConstructors", "GetDefaultMembers", "GetEvent", "GetEvents", "GetField",
            "GetFields", "GetInterface", "GetInterfaceMap", "GetInterfaces", "GetMember", "GetMembers",
            "GetMethod", "GetMethods", "GetNestedType", "GetNestedTypes", "GetProperties", "GetProperty",
            "InvokeMember", "MakeArrayType", "MakeByRefType", "MakeGenericType", "MakePointerType",
            "DeclaringMethod", "DeclaringType", "ReflectedType", "TypeHandle", "TypeInitializer",
            "UnderlyingSystemType",

            // Doing things with System.Runtime.InteropServices
            "InteropServices", "Marshal", "AllocHGlobal", "PtrToStructure", "StructureToPtr",
            "FreeHGlobal", "IntPtr",

            // General Obfuscation
            "MemoryStream", "DeflateStream", "FromBase64String", "EncodedCommand", "Bypass", "ToBase64String",
            "ExpandString", "GetPowerShell",

            // Suspicious Win32 API calls
            "OpenProcess", "VirtualAlloc", "VirtualFree", "WriteProcessMemory", "CreateUserThread", "CloseHandle",
            "GetDelegateForFunctionPointer", "kernel32", "CreateThread", "memcpy", "LoadLibrary", "GetModuleHandle",
            "GetProcAddress", "VirtualProtect", "FreeLibrary", "ReadProcessMemory", "CreateRemoteThread",
            "AdjustTokenPrivileges", "WriteByte", "WriteInt32", "OpenThreadToken", "PtrToString",
            "FreeHGlobal", "ZeroFreeGlobalAllocUnicode", "OpenProcessToken", "GetTokenInformation", "SetThreadToken",
            "ImpersonateLoggedOnUser", "RevertToSelf", "GetLogonSessionData", "CreateProcessWithToken",
            "DuplicateTokenEx", "OpenWindowStation", "OpenDesktop", "MiniDumpWriteDump", "AddSecurityPackage",
            "EnumerateSecurityPackages", "GetProcessHandle", "DangerousGetHandle",

            // Crypto - ransomware, etc.
            "CryptoServiceProvider", "Cryptography", "RijndaelManaged", "SHA1Managed", "CryptoStream",
            "CreateEncryptor", "CreateDecryptor", "TransformFinalBlock", "DeviceIoControl", "SetInformationProcess",
            "PasswordDeriveBytes", 

            // Keylogging
            "GetAsyncKeyState", "GetKeyboardState", "GetForegroundWindow",

            // Using internal types
            "BindingFlags", "NonPublic",

            // Changing logging settings
            "ScriptBlockLogging", "LogPipelineExecutionDetails", "ProtectedEventLogging",
        };

        internal static bool ScriptBlockLoggingExplicitlyDisabled()
        {
            // Verify they haven't explicitly turned off script block logging.
            Dictionary<string, object> groupPolicySettings = Utils.GetGroupPolicySetting("ScriptBlockLogging", Utils.RegLocalMachineThenCurrentUser);
            if (groupPolicySettings != null)
            {
                object logScriptBlockExecution;
                if (groupPolicySettings.TryGetValue("EnableScriptBlockLogging", out logScriptBlockExecution))
                {
                    // If it is configured and explicitly disabled, return true.
                    // (Don't even auto-log ones with suspicious content)
                    if (String.Equals("0", logScriptBlockExecution.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static void LogScriptBlockStart(ScriptBlock scriptBlock, Guid runspaceId)
        {
            // When invoking, log the creation of the script block if it has suspicious
            // content
            bool forceLogCreation = false;
            if (scriptBlock._scriptBlockData.HasSuspiciousContent)
            {
                forceLogCreation = true;
            }

            // We delay logging the creation util the 'Start' so that we can be sure we've
            // properly analyzed the script block's security.
            LogScriptBlockCreation(scriptBlock, forceLogCreation);

            if (ShouldLogScriptBlockActivity("EnableScriptBlockInvocationLogging"))
            {
                PSEtwLog.LogOperationalVerbose(PSEventId.ScriptBlock_Invoke_Start_Detail, PSOpcode.Create, PSTask.CommandStart, PSKeyword.UseAlwaysAnalytic,
                    scriptBlock.Id.ToString(), runspaceId.ToString());
            }
        }

        internal static void LogScriptBlockEnd(ScriptBlock scriptBlock, Guid runspaceId)
        {
            if (ShouldLogScriptBlockActivity("EnableScriptBlockInvocationLogging"))
            {
                PSEtwLog.LogOperationalVerbose(PSEventId.ScriptBlock_Invoke_Complete_Detail, PSOpcode.Create, PSTask.CommandStop, PSKeyword.UseAlwaysAnalytic,
                    scriptBlock.Id.ToString(), runspaceId.ToString());
            }
        }

        internal CompiledScriptBlockData ScriptBlockData { get { return _scriptBlockData; } }

        /// <summary>
        /// Returns the AST corresponding to the script block.
        /// </summary>
        public Ast Ast { get { return (Ast)_scriptBlockData.Ast; } }
        internal IParameterMetadataProvider AstInternal { get { return _scriptBlockData.Ast; } }

        internal IScriptExtent[] SequencePoints { get { return _scriptBlockData.SequencePoints; } }

        internal Action<FunctionContext> DynamicParamBlock { get { return _scriptBlockData.DynamicParamBlock; } }
        internal Action<FunctionContext> UnoptimizedDynamicParamBlock { get { return _scriptBlockData.UnoptimizedDynamicParamBlock; } }
        internal Action<FunctionContext> BeginBlock { get { return _scriptBlockData.BeginBlock; } }
        internal Action<FunctionContext> UnoptimizedBeginBlock { get { return _scriptBlockData.UnoptimizedBeginBlock; } }
        internal Action<FunctionContext> ProcessBlock { get { return _scriptBlockData.ProcessBlock; } }
        internal Action<FunctionContext> UnoptimizedProcessBlock { get { return _scriptBlockData.UnoptimizedProcessBlock; } }
        internal Action<FunctionContext> EndBlock { get { return _scriptBlockData.EndBlock; } }
        internal Action<FunctionContext> UnoptimizedEndBlock { get { return _scriptBlockData.UnoptimizedEndBlock; } }

        internal bool HasBeginBlock { get { return AstInternal.Body.BeginBlock != null; } }
        internal bool HasProcessBlock { get { return AstInternal.Body.ProcessBlock != null; } }
        internal bool HasEndBlock { get { return AstInternal.Body.EndBlock != null; } }
    }

    [Serializable]
    internal class ScriptBlockSerializationHelper : ISerializable, IObjectReference
    {
        private readonly string _scriptText;

        private ScriptBlockSerializationHelper(SerializationInfo info, StreamingContext context)
        {
            if (info == null) throw new ArgumentNullException("info");

            _scriptText = info.GetValue("ScriptText", typeof(string)) as string;
            if (_scriptText == null)
            {
                throw PSTraceSource.NewArgumentNullException("info");
            }
        }

        /// <summary>
        /// Returns a script block that corresponds to the version deserialized
        /// </summary>
        /// <param name="context">The streaming context for this instance</param>
        /// <returns>A script block that corresponds to the version deserialized</returns>
        public Object GetRealObject(StreamingContext context)
        {
            return ScriptBlock.Create(_scriptText);
        }

        /// <summary>
        /// Implements the ISerializable contract for serializing a scriptblock
        /// </summary>
        /// <param name="info">Serialization information for this instance</param>
        /// <param name="context">The streaming context for this instance</param>
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }
    }

    internal sealed class PSScriptCmdlet : PSCmdlet, IDynamicParameters, IDisposable
    {
        private readonly ArrayList _input = new ArrayList();
        private readonly ScriptBlock _scriptBlock;
        private readonly bool _fromScriptFile;
        private readonly bool _useLocalScope;
        private readonly bool _runOptimized;
        private bool _rethrowExitException;
        private MshCommandRuntime _commandRuntime;
        private readonly MutableTuple _localsTuple;
        private bool _exitWasCalled;
        private readonly FunctionContext _functionContext;

        public PSScriptCmdlet(ScriptBlock scriptBlock, bool useNewScope, bool fromScriptFile, ExecutionContext context)
        {
            _scriptBlock = scriptBlock;
            _useLocalScope = useNewScope;
            _fromScriptFile = fromScriptFile;
            _runOptimized = _scriptBlock.Compile(optimized: context._debuggingMode > 0 ? false : useNewScope);
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
                RunClause(_runOptimized ? _scriptBlock.BeginBlock : _scriptBlock.UnoptimizedBeginBlock, AutomationNull.Value, _input);
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
                RunClause(_runOptimized ? _scriptBlock.ProcessBlock : _scriptBlock.UnoptimizedProcessBlock, dollarUnder, _input);
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
                RunClause(_runOptimized ? _scriptBlock.EndBlock : _scriptBlock.UnoptimizedEndBlock, AutomationNull.Value, _input.ToArray());
            }
        }

        private void EnterScope()
        {
            _commandRuntime.SetVariableListsInPipe();

            if (!_useLocalScope)
            {
                this.Context.SessionState.Internal.CurrentScope.DottedScopes.Push(_localsTuple);
            }
        }

        private void ExitScope()
        {
            _commandRuntime.RemoveVariableListsInPipe();

            if (!_useLocalScope)
            {
                this.Context.SessionState.Internal.CurrentScope.DottedScopes.Pop();
            }
        }

        private void RunClause(Action<FunctionContext> clause, object dollarUnderbar, object inputToProcess)
        {
            Pipe oldErrorOutputPipe = this.Context.ShellFunctionErrorOutputPipe;

            // If the script block has a different language mode than the current,
            // change the language mode.
            PSLanguageMode? oldLanguageMode = null;
            PSLanguageMode? newLanguageMode = null;
            if ((_scriptBlock.LanguageMode.HasValue) &&
                (_scriptBlock.LanguageMode != Context.LanguageMode))
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
                    this.Context.RestoreErrorPipe(oldErrorOutputPipe);

                    // Set the language mode
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
                    _commandRuntime.PipelineProcessor.ExecutionFailed = true;
            }
            catch (TerminateException)
            {
                // the debugger is terminating the execution of the current command; bubble up the exception
                throw;
            }
            catch (RuntimeException)
            {
                throw;
            }
            catch (Exception e)
            {
                CommandProcessorBase.CheckForSevereException(e);

                // This cmdlet threw an exception, so
                // wrap it and bubble it up.
                throw;// ManageInvocationException(e);
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
                RunClause(_runOptimized ? _scriptBlock.DynamicParamBlock : _scriptBlock.UnoptimizedDynamicParamBlock,
                          AutomationNull.Value, AutomationNull.Value);
                if (resultList.Count > 1)
                {
                    throw PSTraceSource.NewInvalidOperationException(AutomationExceptions.DynamicParametersWrongType,
                        PSObject.ToStringParser(this.Context, resultList));
                }
                return resultList.Count == 0 ? null : PSObject.Base(resultList[0]);
            }

            return null;
        }

        public void PrepareForBinding(SessionStateScope scope, CommandLineParameters commandLineParameters)
        {
            if (_useLocalScope && scope.LocalsTuple == null)
            {
                scope.LocalsTuple = _localsTuple;
            }
            _localsTuple.SetAutomaticVariable(AutomaticVariable.PSBoundParameters,
                                              commandLineParameters.GetValueToBindToPSBoundParameters(), this.Context);
            _localsTuple.SetAutomaticVariable(AutomaticVariable.MyInvocation, MyInvocation, this.Context);
        }

        private void SetPreferenceVariables()
        {
            if (_commandRuntime.IsDebugFlagSet)
            {
                _localsTuple.SetPreferenceVariable(PreferenceVariable.Debug,
                                                   _commandRuntime.Debug ? ActionPreference.Inquire : ActionPreference.SilentlyContinue);
            }
            if (_commandRuntime.IsVerboseFlagSet)
            {
                _localsTuple.SetPreferenceVariable(PreferenceVariable.Verbose,
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
            if (_commandRuntime.IsWhatIfFlagSet)
            {
                _localsTuple.SetPreferenceVariable(PreferenceVariable.WhatIf, _commandRuntime.WhatIf);
            }
            if (_commandRuntime.IsConfirmFlagSet)
            {
                _localsTuple.SetPreferenceVariable(PreferenceVariable.Confirm,
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
        /// and other members
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            this.DisposingEvent.SafeInvoke(this, EventArgs.Empty);
            commandRuntime = null;
            currentObjectInPipeline = null;
            _input.Clear();
            //_scriptBlock = null;
            //_localsTuple = null;
            //_functionContext = null;

            base.InternalDispose(true);
            _disposed = true;
        }

        #endregion IDispose
    }
}
