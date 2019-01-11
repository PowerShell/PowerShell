// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;
using Microsoft.PowerShell.Commands;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// Class describing a PowerShell module...
    /// </summary>
    public sealed class PSModuleInfo
    {
        internal const string DynamicModulePrefixString = "__DynamicModule_";

        private static readonly ReadOnlyDictionary<string, TypeDefinitionAst> s_emptyTypeDefinitionDictionary =
            new ReadOnlyDictionary<string, TypeDefinitionAst>(new Dictionary<string, TypeDefinitionAst>(StringComparer.OrdinalIgnoreCase));

        // This dictionary doesn't include ExportedTypes from nested modules.
        private ReadOnlyDictionary<string, TypeDefinitionAst> _exportedTypeDefinitionsNoNested { set; get; }

        private static readonly HashSet<string> s_scriptModuleExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                StringLiterals.PowerShellModuleFileExtension,
            };

        internal static void SetDefaultDynamicNameAndPath(PSModuleInfo module)
        {
            string gs = Guid.NewGuid().ToString();
            module.Path = gs;
            module.Name = "__DynamicModule_" + gs;
        }

        /// <summary>
        /// This object describes a PowerShell module...
        /// </summary>
        /// <param name="path">The absolute path to the module.</param>
        /// <param name="context">The execution context for this engine instance.</param>
        /// <param name="sessionState">The module's sessionstate object - this may be null if the module is a dll.</param>
        internal PSModuleInfo(string path, ExecutionContext context, SessionState sessionState)
            : this(null, path, context, sessionState)
        {
        }

        /// <summary>
        /// This object describes a PowerShell module...
        /// </summary>
        /// <param name="name">The name to use for the module. If null, get it from the path name.</param>
        /// <param name="path">The absolute path to the module.</param>
        /// <param name="context">The execution context for this engine instance.</param>
        /// <param name="sessionState">The module's sessionstate object - this may be null if the module is a dll.</param>
        /// <param name="languageMode">Language mode for script based modules.</param>
        internal PSModuleInfo(string name, string path, ExecutionContext context, SessionState sessionState, PSLanguageMode? languageMode)
            : this(name, path, context, sessionState)
        {
            LanguageMode = languageMode;
        }

        /// <summary>
        /// This object describes a PowerShell module...
        /// </summary>
        /// <param name="name">The name to use for the module. If null, get it from the path name.</param>
        /// <param name="path">The absolute path to the module.</param>
        /// <param name="context">The execution context for this engine instance.</param>
        /// <param name="sessionState">The module's sessionstate object - this may be null if the module is a dll.</param>
        internal PSModuleInfo(string name, string path, ExecutionContext context, SessionState sessionState)
        {
            if (path != null)
            {
                string resolvedPath = ModuleCmdletBase.GetResolvedPath(path, context);
                // The resolved path might be null if we're building a dynamic module and the path
                // is just a GUID, not an actual path that can be resolved.
                Path = resolvedPath ?? path;
            }

            SessionState = sessionState;
            if (sessionState != null)
            {
                sessionState.Internal.Module = this;
            }

            // Use the name of basename of the path as the module name if no module name is supplied.
            Name = name ?? ModuleIntrinsics.GetModuleName(Path);
        }

        /// <summary>
        /// Default constructor to create an empty module info.
        /// </summary>
        public PSModuleInfo(bool linkToGlobal)
            : this(LocalPipeline.GetExecutionContextFromTLS(), linkToGlobal)
        {
        }

        /// <summary>
        /// Default constructor to create an empty module info.
        /// </summary>
        internal PSModuleInfo(ExecutionContext context, bool linkToGlobal)
        {
            if (context == null)
                throw new InvalidOperationException("PSModuleInfo");

            SetDefaultDynamicNameAndPath(this);

            // By default the top-level scope in a session state object is the global scope for the instance.
            // For modules, we need to set its global scope to be another scope object and, chain the top
            // level scope for this sessionstate instance to be the parent. The top level scope for this ss is the
            // script scope for the ss.

            // Allocate the session state instance for this module.
            SessionState = new SessionState(context, true, linkToGlobal);
            SessionState.Internal.Module = this;
        }

        /// <summary>
        /// Construct a PSModuleInfo instance initializing it from a scriptblock instead of a script file.
        /// </summary>
        /// <param name="scriptBlock">The scriptblock to use to initialize the module.</param>
        public PSModuleInfo(ScriptBlock scriptBlock)
        {
            if (scriptBlock == null)
            {
                throw PSTraceSource.NewArgumentException("scriptBlock");
            }

            // Get the ExecutionContext from the thread.
            var context = LocalPipeline.GetExecutionContextFromTLS();

            if (context == null)
                throw new InvalidOperationException("PSModuleInfo");

            SetDefaultDynamicNameAndPath(this);

            // By default the top-level scope in a session state object is the global scope for the instance.
            // For modules, we need to set its global scope to be another scope object and, chain the top
            // level scope for this sessionstate instance to be the parent. The top level scope for this ss is the
            // script scope for the ss.

            // Allocate the session state instance for this module.
            SessionState = new SessionState(context, true, true);
            SessionState.Internal.Module = this;

            LanguageMode = scriptBlock.LanguageMode;

            // Now set up the module's session state to be the current session state
            SessionStateInternal oldSessionState = context.EngineSessionState;
            try
            {
                context.EngineSessionState = SessionState.Internal;

                // Set the PSScriptRoot variable...
                context.SetVariable(SpecialVariables.PSScriptRootVarPath, Path);

                scriptBlock = scriptBlock.Clone();
                scriptBlock.SessionState = SessionState;

                Pipe outputPipe = new Pipe { NullPipe = true };
                // And run the scriptblock...
                scriptBlock.InvokeWithPipe(
                    useLocalScope: false,
                    errorHandlingBehavior: ScriptBlock.ErrorHandlingBehavior.WriteToCurrentErrorPipe,
                    dollarUnder: AutomationNull.Value,
                    input: AutomationNull.Value,
                    scriptThis: AutomationNull.Value,
                    outputPipe: outputPipe,
                    invocationInfo: null
                    );
            }
            finally
            {
                context.EngineSessionState = oldSessionState;
            }
        }

        /// <summary>
        /// Specifies the language mode for script based modules.
        /// </summary>
        internal PSLanguageMode? LanguageMode
        {
            get;
            set;
        } = PSLanguageMode.FullLanguage;

        /// <summary>
        /// Set to true when script module automatically exports all functions by default.
        /// </summary>
        internal bool ModuleAutoExportsAllFunctions { get; set; }

        internal bool ModuleHasPrivateMembers { get; set; }

        /// <summary>
        /// True if the module had errors during loading.
        /// </summary>
        internal bool HadErrorsLoading { get; set; }

        /// <summary>
        /// ToString() implementation which returns the name of the module.
        /// </summary>
        /// <returns>The name of the module.</returns>
        public override string ToString()
        {
            return this.Name;
        }

        /// <summary>
        /// Get/set whether to log Pipeline Execution Detail events.
        /// </summary>
        public bool LogPipelineExecutionDetails { get; set; } = false;

        /// <summary>
        /// The name of this module.
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// Sets the name property of the PSModuleInfo object.
        /// </summary>
        /// <param name="name">The name to set it to.</param>
        internal void SetName(string name)
        {
            Name = name;
        }

        /// <summary>
        /// The path to the file that defined this module...
        /// </summary>
        public string Path { get; internal set; } = string.Empty;

        /// <summary>
        /// If the module is a binary module or a script module that defines
        /// classes, this property if a reference to the assembly, otherwise
        /// it is null.
        /// </summary>
        public Assembly ImplementingAssembly { get; internal set; }

        /// <summary>
        /// If this is a script module, then this property will contain
        /// the PowerShell source text that was used to define this module.
        /// </summary>
        public string Definition
        {
            get { return _definitionExtent == null ? string.Empty : _definitionExtent.Text; }
        }

        internal IScriptExtent _definitionExtent;

        /// <summary>
        /// A description of this module...
        /// </summary>
        public string Description
        {
            get { return _description; }

            set { _description = value ?? string.Empty; }
        }

        private string _description = string.Empty;

        /// <summary>
        /// The guid for this module if one was defined in the module manifest.
        /// </summary>
        public Guid Guid { get; private set; }

        internal void SetGuid(Guid guid)
        {
            Guid = guid;
        }

        /// <summary>
        /// The HelpInfo for this module if one was defined in the module manifest.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
        public string HelpInfoUri { get; private set; }

        internal void SetHelpInfoUri(string uri)
        {
            HelpInfoUri = uri;
        }

        /// <summary>
        /// Get the module base directory for this module. For modules loaded via a module
        /// manifest, this will be the directory containing the manifest file rather than
        /// the directory containing the actual module file. This is particularly useful
        /// when loading a GAC'ed assembly.
        /// </summary>
        public string ModuleBase
        {
            get
            {
                return _moduleBase ??
                       (_moduleBase = !string.IsNullOrEmpty(Path) ? IO.Path.GetDirectoryName(Path) : string.Empty);
            }
        }

        internal void SetModuleBase(string moduleBase)
        {
            _moduleBase = moduleBase;
        }

        private string _moduleBase;

        /// <summary>
        /// This value is set from the PrivateData member in the module manifest.
        /// It allows implementor specific data to be passed to the module
        /// via the manifest file.
        /// </summary>
        public object PrivateData
        {
            get
            {
                return _privateData;
            }

            set
            {
                _privateData = value;
                SetPSDataPropertiesFromPrivateData();
            }
        }

        private object _privateData = null;

        private void SetPSDataPropertiesFromPrivateData()
        {
            // Reset the old values of PSData properties.
            _tags.Clear();
            ReleaseNotes = null;
            LicenseUri = null;
            ProjectUri = null;
            IconUri = null;

            if (_privateData is Hashtable hashData && hashData["PSData"] is Hashtable psData)
            {
                var tagsValue = psData["Tags"];
                if (tagsValue is object[] tags && tags.Length > 0)
                {
                    foreach (var tagString in tags.OfType<string>())
                    {
                        AddToTags(tagString);
                    }
                }
                else if (tagsValue is string tag)
                {
                    AddToTags(tag);
                }

                if (psData["LicenseUri"] is string licenseUri)
                {
                    LicenseUri = GetUriFromString(licenseUri);
                }

                if (psData["ProjectUri"] is string projectUri)
                {
                    ProjectUri = GetUriFromString(projectUri);
                }

                if (psData["IconUri"] is string iconUri)
                {
                    IconUri = GetUriFromString(iconUri);
                }

                ReleaseNotes = psData["ReleaseNotes"] as string;
            }
        }

        private static Uri GetUriFromString(string uriString)
        {
            Uri uri = null;
            if (uriString != null)
            {
                // try creating the Uri object
                // Ignoring the return value from Uri.TryCreate(), as uri value will be null on false or valid uri object on true.
                Uri.TryCreate(uriString, UriKind.Absolute, out uri);
            }

            return uri;
        }

        /// <summary>
        /// Get the experimental features declared in this module.
        /// </summary>
        public IEnumerable<ExperimentalFeature> ExperimentalFeatures { get; internal set; } = Utils.EmptyReadOnlyCollection<ExperimentalFeature>();

        /// <summary>
        /// Tags of this module.
        /// </summary>
        public IEnumerable<string> Tags
        {
            get { return _tags; }
        }

        private readonly List<string> _tags = new List<string>();

        internal void AddToTags(string tag)
        {
            _tags.Add(tag);
        }

        /// <summary>
        /// ProjectUri of this module.
        /// </summary>
        public Uri ProjectUri { get; internal set; }

        /// <summary>
        /// IconUri of this module.
        /// </summary>
        public Uri IconUri { get; internal set; }

        /// <summary>
        /// LicenseUri of this module.
        /// </summary>
        public Uri LicenseUri { get; internal set; }

        /// <summary>
        /// ReleaseNotes of this module.
        /// </summary>
        public string ReleaseNotes { get; internal set; }

        /// <summary>
        /// Repository SourceLocation of this module.
        /// </summary>
        public Uri RepositorySourceLocation { get; internal set; }

        /// <summary>
        /// The version of this module.
        /// </summary>
        public Version Version { get; private set; } = new Version(0, 0);

        /// <summary>
        /// Sets the module version.
        /// </summary>
        /// <param name="version">The version to set...</param>
        internal void SetVersion(Version version)
        {
            Version = version;
        }

        /// <summary>
        /// True if the module was compiled (i.e. a .DLL) instead of
        /// being in PowerShell script...
        /// </summary>
        public ModuleType ModuleType { get; private set; } = ModuleType.Script;

        /// <summary>
        /// This this module as being a compiled module...
        /// </summary>
        internal void SetModuleType(ModuleType moduleType) { ModuleType = moduleType; }

        /// <summary>
        /// Module Author.
        /// </summary>
        public string Author
        {
            get; internal set;
        }

        /// <summary>
        /// Controls the module access mode...
        /// </summary>
        public ModuleAccessMode AccessMode
        {
            get { return _accessMode; }

            set
            {
                if (_accessMode == ModuleAccessMode.Constant)
                {
                    throw PSTraceSource.NewInvalidOperationException();
                }

                _accessMode = value;
            }
        }

        private ModuleAccessMode _accessMode = ModuleAccessMode.ReadWrite;

        /// <summary>
        /// CLR Version.
        /// </summary>
        public Version ClrVersion
        {
            get;
            internal set;
        }

        /// <summary>
        /// Company Name.
        /// </summary>
        public string CompanyName
        {
            get;
            internal set;
        }

        /// <summary>
        /// Copyright.
        /// </summary>
        public string Copyright
        {
            get;
            internal set;
        }

        /// <summary>
        /// .NET Framework Version.
        /// </summary>
        public Version DotNetFrameworkVersion
        {
            get;
            internal set;
        }

        internal Collection<string> DeclaredFunctionExports = null;
        internal Collection<string> DeclaredCmdletExports = null;
        internal Collection<string> DeclaredAliasExports = null;
        internal Collection<string> DeclaredVariableExports = null;

        internal List<string> DetectedFunctionExports = new List<string>();
        internal List<string> DetectedCmdletExports = new List<string>();
        internal Dictionary<string, string> DetectedAliasExports = new Dictionary<string, string>();

        /// <summary>
        /// Lists the functions exported by this module...
        /// </summary>
        public Dictionary<string, FunctionInfo> ExportedFunctions
        {
            get
            {
                Dictionary<string, FunctionInfo> exports = new Dictionary<string, FunctionInfo>(StringComparer.OrdinalIgnoreCase);

                // If the module is not binary, it may also have functions...
                if (DeclaredFunctionExports != null)
                {
                    if (DeclaredFunctionExports.Count == 0) { return exports; }

                    foreach (string fn in DeclaredFunctionExports)
                    {
                        FunctionInfo tempFunction = new FunctionInfo(fn, ScriptBlock.EmptyScriptBlock, null) { Module = this };
                        exports[fn] = tempFunction;
                    }
                }
                else if (SessionState != null)
                {
                    // If there is no session state object associated with this list,
                    // just return a null list of exports...
                    if (SessionState.Internal.ExportedFunctions != null)
                    {
                        foreach (FunctionInfo fi in SessionState.Internal.ExportedFunctions)
                        {
                            if (!exports.ContainsKey(fi.Name))
                            {
                                exports[ModuleCmdletBase.AddPrefixToCommandName(fi.Name, fi.Prefix)] = fi;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var detectedExport in DetectedFunctionExports)
                    {
                        if (!exports.ContainsKey(detectedExport))
                        {
                            FunctionInfo tempFunction = new FunctionInfo(detectedExport, ScriptBlock.EmptyScriptBlock, null) { Module = this };
                            exports[detectedExport] = tempFunction;
                        }
                    }
                }

                return exports;
            }
        }

        private bool IsScriptModuleFile(string path)
        {
            var ext = System.IO.Path.GetExtension(path);
            return ext != null && s_scriptModuleExtensions.Contains(ext);
        }

        /// <summary>
        /// Lists the types (PowerShell classes, enums, interfaces) exported by this module.
        /// This returns ASTs for types, created in parse time.
        /// </summary>
        public ReadOnlyDictionary<string, TypeDefinitionAst> GetExportedTypeDefinitions()
        {
            // We cache exported types from this modules, but not from nestedModules,
            // because we may not have NestedModules list populated on the first call.
            // TODO(sevoroby): it may harm perf a little bit. Can we sort it out?

            if (_exportedTypeDefinitionsNoNested == null)
            {
                string rootedPath = null;
                if (RootModule == null)
                {
                    if (this.Path != null)
                    {
                        rootedPath = this.Path;
                    }
                }
                else
                {
                    rootedPath = IO.Path.Combine(this.ModuleBase, this.RootModule);
                }

                // ExternalScriptInfo.GetScriptBlockAst() uses a cache layer to avoid re-parsing.
                CreateExportedTypeDefinitions(rootedPath != null && IsScriptModuleFile(rootedPath) && IO.File.Exists(rootedPath) ?
                    (new ExternalScriptInfo(rootedPath, rootedPath)).GetScriptBlockAst() : null);
            }

            var res = new Dictionary<string, TypeDefinitionAst>(StringComparer.OrdinalIgnoreCase);
            foreach (var nestedModule in this.NestedModules)
            {
                if (nestedModule == this)
                {
                    // Circular nested modules could happen with ill-organized module structure.
                    // For example, module folder 'test' has two files: 'test.psd1' and 'test.psm1', and 'test.psd1' has the following content:
                    //    "@{ ModuleVersion = '0.0.1'; RootModule = 'test'; NestedModules = @('test') }"
                    // Then, 'Import-Module test.psd1 -PassThru' will return a ModuleInfo object with circular nested modules.
                    continue;
                }

                foreach (var typePairs in nestedModule.GetExportedTypeDefinitions())
                {
                    // The last one name wins! It's the same for command names in nested modules.
                    // For rootModule C with Two nested modules (A, B) the order is: A, B, C
                    res[typePairs.Key] = typePairs.Value;
                }
            }

            foreach (var typePairs in _exportedTypeDefinitionsNoNested)
            {
                res[typePairs.Key] = typePairs.Value;
            }

            return new ReadOnlyDictionary<string, TypeDefinitionAst>(res);
        }

        /// <summary>
        /// Create ExportedTypeDefinitions from ast.
        /// </summary>
        /// <param name="moduleContentScriptBlockAsts"></param>
        internal void CreateExportedTypeDefinitions(ScriptBlockAst moduleContentScriptBlockAsts)
        {
            if (moduleContentScriptBlockAsts == null)
            {
                this._exportedTypeDefinitionsNoNested = s_emptyTypeDefinitionDictionary;
            }
            else
            {
                this._exportedTypeDefinitionsNoNested = new ReadOnlyDictionary<string, TypeDefinitionAst>(
                    moduleContentScriptBlockAsts.FindAll(a => (a is TypeDefinitionAst), false)
                        .OfType<TypeDefinitionAst>()
                        .ToDictionary(a => a.Name, StringComparer.OrdinalIgnoreCase));
            }
        }

        internal void AddDetectedTypeExports(List<TypeDefinitionAst> typeDefinitions)
        {
            this._exportedTypeDefinitionsNoNested = new ReadOnlyDictionary<string, TypeDefinitionAst>(
                typeDefinitions.ToDictionary(a => a.Name, StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Prefix.
        /// </summary>
        public string Prefix
        {
            get;
            internal set;
        }

        /// <summary>
        /// Add function to the fixed exports list.
        /// </summary>
        /// <param name="name">The function to add.</param>
        internal void AddDetectedFunctionExport(string name)
        {
            Dbg.Assert(name != null, "AddDetectedFunctionExport should not be called with a null value");

            if (!DetectedFunctionExports.Contains(name))
            {
                DetectedFunctionExports.Add(name);
            }
        }

        /// <summary>
        /// Lists the functions exported by this module...
        /// </summary>
        public Dictionary<string, CmdletInfo> ExportedCmdlets
        {
            get
            {
                Dictionary<string, CmdletInfo> exports = new Dictionary<string, CmdletInfo>(StringComparer.OrdinalIgnoreCase);

                if (DeclaredCmdletExports != null)
                {
                    if (DeclaredCmdletExports.Count == 0) { return exports; }

                    foreach (string fn in DeclaredCmdletExports)
                    {
                        CmdletInfo tempCmdlet = new CmdletInfo(fn, null, null, null, null) { Module = this };
                        exports[fn] = tempCmdlet;
                    }
                }
                else if ((CompiledExports != null) && (CompiledExports.Count > 0))
                {
                    foreach (CmdletInfo cmdlet in CompiledExports)
                    {
                        exports[cmdlet.Name] = cmdlet;
                    }
                }
                else
                {
                    foreach (string detectedExport in DetectedCmdletExports)
                    {
                        if (!exports.ContainsKey(detectedExport))
                        {
                            CmdletInfo tempCmdlet = new CmdletInfo(detectedExport, null, null, null, null) { Module = this };
                            exports[detectedExport] = tempCmdlet;
                        }
                    }
                }

                return exports;
            }
        }

        /// <summary>
        /// Add CmdletInfo to the fixed exports list...
        /// </summary>
        /// <param name="cmdlet">The cmdlet to add...</param>
        internal void AddDetectedCmdletExport(string cmdlet)
        {
            Dbg.Assert(cmdlet != null, "AddDetectedCmdletExport should not be called with a null value");

            if (!DetectedCmdletExports.Contains(cmdlet))
            {
                DetectedCmdletExports.Add(cmdlet);
            }
        }

        /// <summary>
        /// Gets the aggregated list of visible commands exported from the module. If there are two
        /// commands of different types exported with the same name (e.g. alias 'foo' and cmdlet 'foo') the
        /// combined dictionary will only contain the highest precedence cmdlet (e.g. the alias 'foo' since
        /// aliases shadow cmdlets.
        /// </summary>
        public Dictionary<string, CommandInfo> ExportedCommands
        {
            get
            {
                Dictionary<string, CommandInfo> exports = new Dictionary<string, CommandInfo>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, CmdletInfo> cmdlets = this.ExportedCmdlets;
                if (cmdlets != null)
                {
                    foreach (var cmdlet in cmdlets)
                    {
                        exports[cmdlet.Key] = cmdlet.Value;
                    }
                }

                Dictionary<string, FunctionInfo> functions = this.ExportedFunctions;
                if (functions != null)
                {
                    foreach (var function in functions)
                    {
                        exports[function.Key] = function.Value;
                    }
                }

                Dictionary<string, AliasInfo> aliases = this.ExportedAliases;
                if (aliases != null)
                {
                    foreach (var alias in aliases)
                    {
                        exports[alias.Key] = alias.Value;
                    }
                }

                return exports;
            }
        }

        /// <summary>
        /// Add CmdletInfo to the fixed exports list...
        /// </summary>
        /// <param name="cmdlet">The cmdlet to add...</param>
        internal void AddExportedCmdlet(CmdletInfo cmdlet)
        {
            Dbg.Assert(cmdlet != null, "AddExportedCmdlet should not be called with a null value");
            _compiledExports.Add(cmdlet);
        }

        /// <summary>
        /// Return the merged list of exported cmdlets. This is necessary
        /// because you may have a binary module with nested modules where
        /// some cmdlets come from the module and others come from the nested
        /// module. We need to consolidate the list so it can properly be constrained.
        /// </summary>
        internal List<CmdletInfo> CompiledExports
        {
            get
            {
                // If this module has a session state instance and there are any
                // exported cmdlets in the session state, migrate them to the
                // module info _compiledCmdlets entry.
                if (SessionState != null && SessionState.Internal.ExportedCmdlets != null &&
                    SessionState.Internal.ExportedCmdlets.Count > 0)
                {
                    foreach (CmdletInfo ci in SessionState.Internal.ExportedCmdlets)
                    {
                        _compiledExports.Add(ci);
                    }

                    SessionState.Internal.ExportedCmdlets.Clear();
                }

                return _compiledExports;
            }
        }

        private readonly List<CmdletInfo> _compiledExports = new List<CmdletInfo>();

        /// <summary>
        /// Add AliasInfo to the fixed exports list...
        /// </summary>
        /// <param name="aliasInfo">The cmdlet to add...</param>
        internal void AddExportedAlias(AliasInfo aliasInfo)
        {
            Dbg.Assert(aliasInfo != null, "AddExportedAlias should not be called with a null value");
            CompiledAliasExports.Add(aliasInfo);
        }

        /// <summary>
        /// Return the merged list of exported aliases. This is necessary
        /// because you may have a binary module with nested modules where
        /// some aliases come from the module and others come from the nested
        /// module. We need to consolidate the list so it can properly be constrained.
        /// </summary>
        internal List<AliasInfo> CompiledAliasExports { get; } = new List<AliasInfo>();

        /// <summary>
        /// FileList.
        /// </summary>
        public IEnumerable<string> FileList
        {
            get { return _fileList; }
        }

        private List<string> _fileList = new List<string>();

        internal void AddToFileList(string file)
        {
            _fileList.Add(file);
        }

        /// <summary>
        /// Lists the PowerShell editions this module is compatible with. This should
        /// reflect the module manifest the module was loaded with, or if no manifest was given
        /// or the key was not in the manifest, this should be an empty collection. This
        /// property is never null.
        /// </summary>
        public IEnumerable<string> CompatiblePSEditions
        {
            get { return _compatiblePSEditions; }
        }

        private List<string> _compatiblePSEditions = new List<string>();

        internal void AddToCompatiblePSEditions(string psEdition)
        {
            _compatiblePSEditions.Add(psEdition);
        }

        internal void AddToCompatiblePSEditions(IEnumerable<string> psEditions)
        {
            _compatiblePSEditions.AddRange(psEditions);
        }

        /// <summary>
        /// Describes whether the module was considered compatible at load time.
        /// Any module not on the System32 module path should have this as true.
        /// Modules loaded from the System32 module path will have this as true if they
        /// have declared edition compatibility with PowerShell Core. Currently, this field
        /// is true for all non-psd1 module files, when it should not be. Being able to
        /// load psm1/dll modules from the System32 module path without needing to skip
        /// the edition check is considered a bug and should be fixed.
        /// </summary>
        internal bool IsConsideredEditionCompatible { get; set; } = true;

        /// <summary>
        /// ModuleList.
        /// </summary>
        public IEnumerable<object> ModuleList
        {
            get { return _moduleList; }
        }

        private Collection<object> _moduleList = new Collection<object>();

        internal void AddToModuleList(object m)
        {
            _moduleList.Add(m);
        }

        /// <summary>
        /// Returns the list of child modules of this module. This will only
        /// be non-empty for module manifests.
        /// </summary>
        public ReadOnlyCollection<PSModuleInfo> NestedModules
        {
            get
            {
                return _readonlyNestedModules ??
                       (_readonlyNestedModules = new ReadOnlyCollection<PSModuleInfo>(_nestedModules));
            }
        }

        private ReadOnlyCollection<PSModuleInfo> _readonlyNestedModules;

        /// <summary>
        /// Add a module to the list of child modules.
        /// </summary>
        /// <param name="nestedModule">The module to add.</param>
        internal void AddNestedModule(PSModuleInfo nestedModule)
        {
            AddModuleToList(nestedModule, _nestedModules);
        }

        private readonly List<PSModuleInfo> _nestedModules = new List<PSModuleInfo>();

        /// <summary>
        /// PowerShell Host Name.
        /// </summary>
        public string PowerShellHostName
        {
            get;
            internal set;
        }

        /// <summary>
        /// PowerShell Host Version.
        /// </summary>
        public Version PowerShellHostVersion
        {
            get;
            internal set;
        }

        /// <summary>
        /// PowerShell Version.
        /// </summary>
        public Version PowerShellVersion
        {
            get;
            internal set;
        }

        /// <summary>
        /// Processor Architecture.
        /// </summary>
        public ProcessorArchitecture ProcessorArchitecture
        {
            get;
            internal set;
        }

        /// <summary>
        /// Scripts to Process.
        /// </summary>
        public IEnumerable<string> Scripts
        {
            get { return _scripts; }
        }

        private List<string> _scripts = new List<string>();

        internal void AddScript(string s)
        {
            _scripts.Add(s);
        }

        /// <summary>
        /// Required Assemblies.
        /// </summary>
        public IEnumerable<string> RequiredAssemblies
        {
            get { return _requiredAssemblies; }
        }

        private Collection<string> _requiredAssemblies = new Collection<string>();

        internal void AddRequiredAssembly(string assembly)
        {
            _requiredAssemblies.Add(assembly);
        }

        /// <summary>
        /// Returns the list of required modules of this module. This will only
        /// be non-empty for module manifests.
        /// </summary>
        public ReadOnlyCollection<PSModuleInfo> RequiredModules
        {
            get
            {
                return _readonlyRequiredModules ??
                       (_readonlyRequiredModules = new ReadOnlyCollection<PSModuleInfo>(_requiredModules));
            }
        }

        private ReadOnlyCollection<PSModuleInfo> _readonlyRequiredModules;

        /// <summary>
        /// Add a module to the list of required modules.
        /// </summary>
        /// <param name="requiredModule">The module to add.</param>
        internal void AddRequiredModule(PSModuleInfo requiredModule)
        {
            AddModuleToList(requiredModule, _requiredModules);
        }

        private List<PSModuleInfo> _requiredModules = new List<PSModuleInfo>();

        /// <summary>
        /// Returns the list of required modules specified in the module manifest of this module. This will only
        /// be non-empty for module manifests.
        /// </summary>
        internal ReadOnlyCollection<ModuleSpecification> RequiredModulesSpecification
        {
            get
            {
                return _readonlyRequiredModulesSpecification ??
                       (_readonlyRequiredModulesSpecification = new ReadOnlyCollection<ModuleSpecification>(_requiredModulesSpecification));
            }
        }

        private ReadOnlyCollection<ModuleSpecification> _readonlyRequiredModulesSpecification;

        /// <summary>
        /// Add a module to the list of required modules specification.
        /// </summary>
        /// <param name="requiredModuleSpecification">The module to add.</param>
        internal void AddRequiredModuleSpecification(ModuleSpecification requiredModuleSpecification)
        {
            _requiredModulesSpecification.Add(requiredModuleSpecification);
        }

        private List<ModuleSpecification> _requiredModulesSpecification = new List<ModuleSpecification>();

        /// <summary>
        /// Root Module.
        /// </summary>
        public string RootModule
        {
            get;
            internal set;
        }

        /// <summary>
        /// This member is used to copy over the RootModule in case the module is a manifest module
        /// This is so that only ModuleInfo for modules with type=Manifest have RootModule populated.
        /// </summary>
        internal string RootModuleForManifest
        {
            get;
            set;
        }

        /// <summary>
        /// Add a module to the list of modules, avoiding adding duplicates.
        /// </summary>
        private static void AddModuleToList(PSModuleInfo module, List<PSModuleInfo> moduleList)
        {
            Dbg.Assert(module != null, "AddModuleToList should not be called with a null value");
            // Add the module if it isn't already there...
            foreach (PSModuleInfo m in moduleList)
            {
                if (m.Path.Equals(module.Path, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            moduleList.Add(module);
        }

        internal static string[] _builtinVariables = new string[] { "_", "this", "input", "args", "true", "false", "null",
            "PSDefaultParameterValues", "Error", "PSScriptRoot", "PSCommandPath", "MyInvocation", "ExecutionContext", "StackTrace" };

        /// <summary>
        /// Lists the variables exported by this module.
        /// </summary>
        public Dictionary<string, PSVariable> ExportedVariables
        {
            get
            {
                Dictionary<string, PSVariable> exportedVariables = new Dictionary<string, PSVariable>(StringComparer.OrdinalIgnoreCase);

                if ((DeclaredVariableExports != null) && (DeclaredVariableExports.Count > 0))
                {
                    foreach (string fn in DeclaredVariableExports)
                    {
                        exportedVariables[fn] = null;
                    }
                }
                else
                {
                    // If there is no session state object associated with this list,
                    // just return a null list of exports. This will be true if the
                    // module is a compiled module.
                    if (SessionState == null || SessionState.Internal.ExportedVariables == null)
                    {
                        return exportedVariables;
                    }

                    foreach (PSVariable v in SessionState.Internal.ExportedVariables)
                    {
                        exportedVariables[v.Name] = v;
                    }
                }

                return exportedVariables;
            }
        }

        /// <summary>
        /// Lists the aliases exported by this module.
        /// </summary>
        public Dictionary<string, AliasInfo> ExportedAliases
        {
            get
            {
                Dictionary<string, AliasInfo> exportedAliases = new Dictionary<string, AliasInfo>(StringComparer.OrdinalIgnoreCase);

                if ((DeclaredAliasExports != null) && (DeclaredAliasExports.Count > 0))
                {
                    foreach (string fn in DeclaredAliasExports)
                    {
                        AliasInfo tempAlias = new AliasInfo(fn, null, null) { Module = this };
                        exportedAliases[fn] = tempAlias;
                    }
                }
                else if ((CompiledAliasExports != null) && (CompiledAliasExports.Count > 0))
                {
                    foreach (AliasInfo ai in CompiledAliasExports)
                    {
                        exportedAliases[ai.Name] = ai;
                    }
                }
                else
                {
                    // There is no session state object associated with this list.
                    if (SessionState == null)
                    {
                        // Check if we detected any
                        if (DetectedAliasExports.Count > 0)
                        {
                            foreach (var pair in DetectedAliasExports)
                            {
                                string detectedExport = pair.Key;
                                if (!exportedAliases.ContainsKey(detectedExport))
                                {
                                    AliasInfo tempAlias = new AliasInfo(detectedExport, pair.Value, null) { Module = this };
                                    exportedAliases[detectedExport] = tempAlias;
                                }
                            }
                        }
                        else
                        {
                            // just return a null list of exports. This will be true if the
                            // module is a compiled module.
                            return exportedAliases;
                        }
                    }
                    else
                    {
                        // We have a session state
                        foreach (AliasInfo ai in SessionState.Internal.ExportedAliases)
                        {
                            exportedAliases[ai.Name] = ai;
                        }
                    }
                }

                return exportedAliases;
            }
        }

        /// <summary>
        /// Add alias to the detected alias list.
        /// </summary>
        /// <param name="name">The alias to add.</param>
        /// <param name="value">The command it resolves to.</param>
        internal void AddDetectedAliasExport(string name, string value)
        {
            Dbg.Assert(name != null, "AddDetectedAliasExport should not be called with a null value");

            DetectedAliasExports[name] = value;
        }

        /// <summary>
        /// Lists the workflows exported by this module.
        /// </summary>
        public Dictionary<string, FunctionInfo> ExportedWorkflows
        {
            get
            {
                return new Dictionary<string, FunctionInfo>(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// </summary>
        public ReadOnlyCollection<string> ExportedDscResources
        {
            get
            {
                return _declaredDscResourceExports != null
                    ? new ReadOnlyCollection<string>(_declaredDscResourceExports)
                    : Utils.EmptyReadOnlyCollection<string>();
            }
        }

        internal Collection<string> _declaredDscResourceExports = null;

        /// <summary>
        /// The session state instance associated with this module.
        /// </summary>
        public SessionState SessionState { get; set; }

        /// <summary>
        /// Returns a new scriptblock bound to this module instance.
        /// </summary>
        /// <param name="scriptBlockToBind">The original scriptblock.</param>
        /// <returns>The new bound scriptblock.</returns>
        public ScriptBlock NewBoundScriptBlock(ScriptBlock scriptBlockToBind)
        {
            var context = LocalPipeline.GetExecutionContextFromTLS();
            return NewBoundScriptBlock(scriptBlockToBind, context);
        }

        internal ScriptBlock NewBoundScriptBlock(ScriptBlock scriptBlockToBind, ExecutionContext context)
        {
            if (SessionState == null || context == null)
            {
                throw PSTraceSource.NewInvalidOperationException(Modules.InvalidOperationOnBinaryModule);
            }

            ScriptBlock newsb;

            // Now set up the module's session state to be the current session state
            lock (context.EngineSessionState)
            {
                SessionStateInternal oldSessionState = context.EngineSessionState;

                try
                {
                    context.EngineSessionState = SessionState.Internal;
                    newsb = scriptBlockToBind.Clone();
                    newsb.SessionState = SessionState;
                }
                finally
                {
                    context.EngineSessionState = oldSessionState;
                }
            }

            return newsb;
        }

        /// <summary>
        /// Invoke a scriptblock in the context of this module...
        /// </summary>
        /// <param name="sb">The scriptblock to invoke.</param>
        /// <param name="args">Arguments to the scriptblock.</param>
        /// <returns>The result of the invocation.</returns>
        public object Invoke(ScriptBlock sb, params object[] args)
        {
            if (sb == null)
                return null;

            // Temporarily set the scriptblocks session state to be the
            // modules...
            SessionStateInternal oldSessionState = sb.SessionStateInternal;
            object result;
            try
            {
                sb.SessionStateInternal = SessionState.Internal;
                result = sb.InvokeReturnAsIs(args);
            }
            finally
            {
                // and restore the scriptblocks session state...
                sb.SessionStateInternal = oldSessionState;
            }

            return result;
        }

        /// <summary>
        /// This routine allows you to get access variable objects in the callers module
        /// or from the toplevel sessionstate if there is no calling module.
        /// </summary>
        /// <param name="variableName"></param>
        /// <returns></returns>
        public PSVariable GetVariableFromCallersModule(string variableName)
        {
            if (string.IsNullOrEmpty(variableName))
            {
                throw new ArgumentNullException("variableName");
            }

            var context = LocalPipeline.GetExecutionContextFromTLS();
            SessionState callersSessionState = null;
            foreach (var sf in context.Debugger.GetCallStack())
            {
                var frameModule = sf.InvocationInfo.MyCommand.Module;
                if (frameModule == null)
                {
                    break;
                }

                if (frameModule.SessionState != SessionState)
                {
                    callersSessionState = sf.InvocationInfo.MyCommand.Module.SessionState;
                    break;
                }
            }

            if (callersSessionState != null)
            {
                return callersSessionState.Internal.GetVariable(variableName);
            }
            else
            {
                return context.TopLevelSessionState.GetVariable(variableName);
            }
        }

        /// <summary>
        /// Copies the local variables in the caller's cope into the module...
        /// </summary>
        internal void CaptureLocals()
        {
            if (SessionState == null)
            {
                throw PSTraceSource.NewInvalidOperationException(Modules.InvalidOperationOnBinaryModule);
            }

            var context = LocalPipeline.GetExecutionContextFromTLS();
            var tuple = context.EngineSessionState.CurrentScope.LocalsTuple;
            IEnumerable<PSVariable> variables = context.EngineSessionState.CurrentScope.Variables.Values;
            if (tuple != null)
            {
                var result = new Dictionary<string, PSVariable>();
                tuple.GetVariableTable(result, false);
                variables = result.Values.Concat(variables);
            }

            foreach (PSVariable v in variables)
            {
                try
                {
                    // Only copy simple mutable variables...
                    if (v.Options == ScopedItemOptions.None && !(v is NullVariable))
                    {
                        PSVariable newVar = new PSVariable(v.Name, v.Value, v.Options, v.Description);
                        // The variable is already defined/set in the scope, and that means the attributes
                        // have already been checked if it was needed, so we don't do it again.
                        newVar.AddParameterAttributesNoChecks(v.Attributes);
                        SessionState.Internal.NewVariable(newVar, false);
                    }
                }
                catch (SessionStateException)
                {
                }
            }
        }

        /// <summary>
        /// Build a custom object out of this module...
        /// </summary>
        /// <returns>A custom object.</returns>
        public PSObject AsCustomObject()
        {
            if (SessionState == null)
            {
                throw PSTraceSource.NewInvalidOperationException(Modules.InvalidOperationOnBinaryModule);
            }

            PSObject obj = new PSObject();

            foreach (KeyValuePair<string, FunctionInfo> entry in this.ExportedFunctions)
            {
                FunctionInfo func = entry.Value;
                if (func != null)
                {
                    PSScriptMethod sm = new PSScriptMethod(func.Name, func.ScriptBlock);
                    obj.Members.Add(sm);
                }
            }

            foreach (KeyValuePair<string, PSVariable> entry in this.ExportedVariables)
            {
                PSVariable var = entry.Value;
                if (var != null)
                {
                    PSVariableProperty sm = new PSVariableProperty(var);
                    obj.Members.Add(sm);
                }
            }

            return obj;
        }

        /// <summary>
        /// Optional script that is going to be called just before Remove-Module cmdlet removes the module.
        /// </summary>
        public ScriptBlock OnRemove { get; set; }

        /// <summary>
        /// The list of Format files imported by this module.
        /// </summary>
        public ReadOnlyCollection<string> ExportedFormatFiles { get; private set; } = new ReadOnlyCollection<string>(new List<string>());

        internal void SetExportedFormatFiles(ReadOnlyCollection<string> files)
        {
            ExportedFormatFiles = files;
        }

        /// <summary>
        /// The list of types files imported by this module.
        /// </summary>
        public ReadOnlyCollection<string> ExportedTypeFiles { get; private set; } = new ReadOnlyCollection<string>(new List<string>());

        internal void SetExportedTypeFiles(ReadOnlyCollection<string> files)
        {
            ExportedTypeFiles = files;
        }

        /// <summary>
        /// Implements deep copy of a PSModuleInfo instance.
        /// <returns>A new PSModuleInfo instance.</returns>
        /// </summary>
        public PSModuleInfo Clone()
        {
            PSModuleInfo clone = (PSModuleInfo)this.MemberwiseClone();

            clone._fileList = new List<string>(this.FileList);
            clone._moduleList = new Collection<object>(_moduleList);

            foreach (var n in this.NestedModules)
            {
                clone.AddNestedModule(n);
            }

            clone._readonlyNestedModules = new ReadOnlyCollection<PSModuleInfo>(this.NestedModules);
            clone._readonlyRequiredModules = new ReadOnlyCollection<PSModuleInfo>(this.RequiredModules);
            clone._readonlyRequiredModulesSpecification = new ReadOnlyCollection<ModuleSpecification>(this.RequiredModulesSpecification);
            clone._requiredAssemblies = new Collection<string>(_requiredAssemblies);
            clone._requiredModulesSpecification = new List<ModuleSpecification>();
            clone._requiredModules = new List<PSModuleInfo>();

            foreach (var r in _requiredModules)
            {
                clone.AddRequiredModule(r);
            }

            foreach (var r in _requiredModulesSpecification)
            {
                clone.AddRequiredModuleSpecification(r);
            }

            clone._scripts = new List<string>(this.Scripts);

            clone.SessionState = this.SessionState;

            return clone;
        }

        /// <summary>
        /// Enables or disables the appdomain module path cache.
        /// </summary>
        public static bool UseAppDomainLevelModuleCache { get; set; }

        /// <summary>
        /// Clear out the appdomain-level module path cache.
        /// </summary>
        public static void ClearAppDomainLevelModulePathCache()
        {
            s_appdomainModulePathCache.Clear();
        }

#if DEBUG
        /// <summary>
        /// A method available in debug mode providing access to the module path cache.
        /// </summary>
        /// <returns></returns>
        public static object GetAppDomainLevelModuleCache()
        {
            return s_appdomainModulePathCache;
        }
#endif
        /// <summary>
        /// Look up a module in the appdomain wide module path cache.
        /// </summary>
        /// <param name="moduleName">Module name to look up.</param>
        /// <returns>The path to the matched module.</returns>
        internal static string ResolveUsingAppDomainLevelModuleCache(string moduleName)
        {
            string path;
            if (s_appdomainModulePathCache.TryGetValue(moduleName, out path))
            {
                return path;
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Add an entry to the appdomain level module path cache. By default, if there already is an entry
        /// it won't be replace. If force is specified, then it will be updated. \
        /// </summary>
        /// <param name="moduleName"></param>
        /// <param name="path"></param>
        /// <param name="force"></param>
        internal static void AddToAppDomainLevelModuleCache(string moduleName, string path, bool force)
        {
            if (force)
            {
                s_appdomainModulePathCache.AddOrUpdate(moduleName, path, (modulename, oldPath) => path);
            }
            else
            {
                s_appdomainModulePathCache.TryAdd(moduleName, path);
            }
        }

        /// <summary>
        /// If there is an entry for the named module in the appdomain level module path cache, remove it.
        /// </summary>
        /// <param name="moduleName">The name of the module to remove from the cache.</param>
        /// <returns>True if the module was remove.</returns>
        internal static bool RemoveFromAppDomainLevelCache(string moduleName)
        {
            string outString;
            return s_appdomainModulePathCache.TryRemove(moduleName, out outString);
        }

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> s_appdomainModulePathCache =
            new System.Collections.Concurrent.ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Indicates the type of a module.
    /// </summary>
    public enum ModuleType
    {
        /// <summary>
        /// Indicates that this is a script module (a powershell file with a .PSM1 extension)
        /// </summary>
        Script = 0,
        /// <summary>
        /// Indicates that this is compiled .dll containing cmdlet definitions.
        /// </summary>
        Binary = 1,
        /// <summary>
        /// Indicates that this module entry was derived from a module manifest and
        /// may have child modules.
        /// </summary>
        Manifest,
        /// <summary>
        /// Indicates that this is cmdlets-over-objects module (a powershell file with a .CDXML extension)
        /// </summary>
        Cim,
        /// <summary>
        /// Indicates that this is workflow module (a powershell file with a .XAML extension)
        /// </summary>
        Workflow,
    }

    /// <summary>
    /// Defines the possible access modes for a module...
    /// </summary>
    public enum ModuleAccessMode
    {
        /// <summary>
        /// The default access mode for the module.
        /// </summary>
        ReadWrite = 0,
        /// <summary>
        /// The module is readonly and can only be removed with -force.
        /// </summary>
        ReadOnly = 1,
        /// <summary>
        /// The module cannot be removed.
        /// </summary>
        Constant = 2
    }

    /// <summary>
    /// An EqualityComparer to compare 2 PSModuleInfo instances. 2 PSModuleInfos are
    /// considered equal if their Name,Guid and Version are equal.
    /// </summary>
    internal sealed class PSModuleInfoComparer : IEqualityComparer<PSModuleInfo>
    {
        public bool Equals(PSModuleInfo x, PSModuleInfo y)
        {
            // Check whether the compared objects reference the same data.
            if (Object.ReferenceEquals(x, y)) return true;

            // Check whether any of the compared objects is null.
            if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                return false;

            bool result = string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase) &&
                (x.Guid == y.Guid) && (x.Version == y.Version);

            return result;
        }

        public int GetHashCode(PSModuleInfo obj)
        {
            unchecked // Overflow is fine, just wrap
            {
                int result = 0;

                if (obj != null)
                {
                    // picking two different prime numbers to avoid collisions
                    result = 23;
                    if (obj.Name != null)
                    {
                        result = result * 17 + obj.Name.GetHashCode();
                    }

                    if (obj.Guid != Guid.Empty)
                    {
                        result = result * 17 + obj.Guid.GetHashCode();
                    }

                    if (obj.Version != null)
                    {
                        result = result * 17 + obj.Version.GetHashCode();
                    }
                }

                return result;
            }
        }
    }
}
