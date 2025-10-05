// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Configuration;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Security;
using System.Threading;

using Microsoft.Management.Infrastructure;
using Microsoft.PowerShell.Cmdletization;
using Microsoft.PowerShell.Telemetry;

using Dbg = System.Management.Automation.Diagnostics;
using Parser = System.Management.Automation.Language.Parser;
using ScriptBlock = System.Management.Automation.ScriptBlock;
using Token = System.Management.Automation.Language.Token;

#if LEGACYTELEMETRY
using Microsoft.PowerShell.Telemetry.Internal;
#endif

//
// Now define the set of commands for manipulating modules.
//
namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implements a cmdlet that loads a module.
    /// </summary>
    [Cmdlet(VerbsData.Import, "Module", DefaultParameterSetName = ParameterSet_Name, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096585")]
    [OutputType(typeof(PSModuleInfo))]
    public sealed class ImportModuleCommand : ModuleCmdletBase, IDisposable
    {
        #region Cmdlet parameters

        private const string ParameterSet_Name = "Name";
        private const string ParameterSet_FQName = "FullyQualifiedName";
        private const string ParameterSet_ModuleInfo = "ModuleInfo";
        private const string ParameterSet_Assembly = "Assembly";

        private const string ParameterSet_ViaPsrpSession = "PSSession";
        private const string ParameterSet_ViaCimSession = "CimSession";
        private const string ParameterSet_FQName_ViaPsrpSession = "FullyQualifiedNameAndPSSession";
        private const string ParameterSet_ViaWinCompat = "WinCompat";
        private const string ParameterSet_FQName_ViaWinCompat = "FullyQualifiedNameAndWinCompat";

        /// <summary>
        /// This parameter specifies whether to import to the current session state
        /// or to the global / top-level session state.
        /// </summary>
        [Parameter]
        public SwitchParameter Global
        {
            get { return base.BaseGlobal; }

            set { base.BaseGlobal = value; }
        }

        /// <summary>
        /// This parameter specified a prefix used to modify names of imported commands.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public string Prefix
        {
            get { return BasePrefix; }

            set { BasePrefix = value; }
        }

        /// <summary>
        /// This parameter names the module to load.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSet_Name, Mandatory = true, ValueFromPipeline = true, Position = 0)]
        [Parameter(ParameterSetName = ParameterSet_ViaPsrpSession, Mandatory = true, ValueFromPipeline = true, Position = 0)]
        [Parameter(ParameterSetName = ParameterSet_ViaCimSession, Mandatory = true, ValueFromPipeline = true, Position = 0)]
        [Parameter(ParameterSetName = ParameterSet_ViaWinCompat, Mandatory = true, ValueFromPipeline = true, Position = 0)]
        [ValidateTrustedData]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        public string[] Name { get; set; } = Array.Empty<string>();

        /// <summary>
        /// This parameter specifies the current pipeline object.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSet_FQName, Mandatory = true, ValueFromPipeline = true, Position = 0)]
        [Parameter(ParameterSetName = ParameterSet_FQName_ViaPsrpSession, Mandatory = true, ValueFromPipeline = true, Position = 0)]
        [Parameter(ParameterSetName = ParameterSet_FQName_ViaWinCompat, Mandatory = true, ValueFromPipeline = true, Position = 0)]
        [ValidateTrustedData]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        public ModuleSpecification[] FullyQualifiedName { get; set; }

        /// <summary>
        /// A list of assembly objects to process as modules.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        [Parameter(ParameterSetName = ParameterSet_Assembly, Mandatory = true, ValueFromPipeline = true, Position = 0)]
        [ValidateTrustedData]
        public Assembly[] Assembly { get; set; }

        /// <summary>
        /// This patterns matching the names of functions to import from the module...
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        public string[] Function
        {
            get
            {
                return _functionImportList;
            }

            set
            {
                if (value == null)
                    return;
                _functionImportList = value;
                // Create the list of patterns to match at parameter bind time
                // so errors will be reported before loading the module...
                BaseFunctionPatterns = new List<WildcardPattern>();
                foreach (string pattern in _functionImportList)
                {
                    BaseFunctionPatterns.Add(WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase));
                }
            }
        }

        private string[] _functionImportList = Array.Empty<string>();

        /// <summary>
        /// This patterns matching the names of cmdlets to import from the module...
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        public string[] Cmdlet
        {
            get
            {
                return _cmdletImportList;
            }

            set
            {
                if (value == null)
                    return;

                _cmdletImportList = value;
                // Create the list of patterns to match at parameter bind time
                // so errors will be reported before loading the module...
                BaseCmdletPatterns = new List<WildcardPattern>();
                foreach (string pattern in _cmdletImportList)
                {
                    BaseCmdletPatterns.Add(WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase));
                }
            }
        }

        private string[] _cmdletImportList = Array.Empty<string>();

        /// <summary>
        /// This parameter specifies the variables to import from the module...
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        public string[] Variable
        {
            get
            {
                return _variableExportList;
            }

            set
            {
                if (value == null)
                    return;
                _variableExportList = value;
                // Create the list of patterns to match at parameter bind time
                // so errors will be reported before loading the module...
                BaseVariablePatterns = new List<WildcardPattern>();
                foreach (string pattern in _variableExportList)
                {
                    BaseVariablePatterns.Add(WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase));
                }
            }
        }

        private string[] _variableExportList;

        /// <summary>
        /// This parameter specifies the aliases to import from the module...
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        public string[] Alias
        {
            get
            {
                return _aliasExportList;
            }

            set
            {
                if (value == null)
                    return;

                _aliasExportList = value;
                // Create the list of patterns to match at parameter bind time
                // so errors will be reported before loading the module...
                BaseAliasPatterns = new List<WildcardPattern>();
                foreach (string pattern in _aliasExportList)
                {
                    BaseAliasPatterns.Add(WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase));
                }
            }
        }

        private string[] _aliasExportList;

        /// <summary>
        /// This parameter causes a module to be loaded over top of the current one...
        /// </summary>
        [Parameter]
        public SwitchParameter Force
        {
            get { return (SwitchParameter)BaseForce; }

            set { BaseForce = value; }
        }

        /// <summary>
        /// Skips the check on CompatiblePSEditions for modules loaded from the System32 module path.
        /// This is mutually exclusive with UseWindowsPowerShell parameter.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSet_Name)]
        [Parameter(ParameterSetName = ParameterSet_FQName)]
        [Parameter(ParameterSetName = ParameterSet_ModuleInfo)]
        [Parameter(ParameterSetName = ParameterSet_Assembly)]
        [Parameter(ParameterSetName = ParameterSet_ViaPsrpSession)]
        [Parameter(ParameterSetName = ParameterSet_ViaCimSession)]
        [Parameter(ParameterSetName = ParameterSet_FQName_ViaPsrpSession)]
        public SwitchParameter SkipEditionCheck
        {
            get { return (SwitchParameter)BaseSkipEditionCheck; }

            set { BaseSkipEditionCheck = value; }
        }

        /// <summary>
        /// This parameter causes the session state instance to be written...
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru
        {
            get { return (SwitchParameter)BasePassThru; }

            set { BasePassThru = value; }
        }

        /// <summary>
        /// This parameter causes the session state instance to be written as a custom object...
        /// </summary>
        [Parameter]
        public SwitchParameter AsCustomObject
        {
            get { return (SwitchParameter)BaseAsCustomObject; }

            set { BaseAsCustomObject = value; }
        }

        /// <summary>
        /// The minimum version of the module to load.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSet_Name)]
        [Parameter(ParameterSetName = ParameterSet_ViaPsrpSession)]
        [Parameter(ParameterSetName = ParameterSet_ViaCimSession)]
        [Parameter(ParameterSetName = ParameterSet_ViaWinCompat)]
        [Alias("Version")]
        public Version MinimumVersion
        {
            get { return BaseMinimumVersion; }

            set { BaseMinimumVersion = value; }
        }

        /// <summary>
        /// The maximum version of the module to load.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSet_Name)]
        [Parameter(ParameterSetName = ParameterSet_ViaPsrpSession)]
        [Parameter(ParameterSetName = ParameterSet_ViaCimSession)]
        [Parameter(ParameterSetName = ParameterSet_ViaWinCompat)]
        public string MaximumVersion
        {
            get
            {
                if (BaseMaximumVersion == null)
                    return null;
                else
                    return BaseMaximumVersion.ToString();
            }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    BaseMaximumVersion = null;
                }
                else
                {
                    BaseMaximumVersion = GetMaximumVersion(value);
                }
            }
        }

        /// <summary>
        /// The version of the module to load.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSet_Name)]
        [Parameter(ParameterSetName = ParameterSet_ViaPsrpSession)]
        [Parameter(ParameterSetName = ParameterSet_ViaCimSession)]
        [Parameter(ParameterSetName = ParameterSet_ViaWinCompat)]
        public Version RequiredVersion
        {
            get { return BaseRequiredVersion; }

            set { BaseRequiredVersion = value; }
        }

        /// <summary>
        /// This parameter specifies the current pipeline object.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSet_ModuleInfo, Mandatory = true, ValueFromPipeline = true, Position = 0)]
        [ValidateTrustedData]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        public PSModuleInfo[] ModuleInfo { get; set; } = Array.Empty<PSModuleInfo>();

        /// <summary>
        /// The arguments to pass to the module script.
        /// </summary>
        [Parameter]
        [Alias("Args")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        public object[] ArgumentList
        {
            get { return BaseArgumentList; }

            set { BaseArgumentList = value; }
        }

        /// <summary>
        /// Disable warnings on cmdlet and function names that have non-standard verbs
        /// or non-standard characters in the noun.
        /// </summary>
        [Parameter]
        public SwitchParameter DisableNameChecking
        {
            get { return BaseDisableNameChecking; }

            set { BaseDisableNameChecking = value; }
        }

        /// <summary>
        /// Does not import a command if a command with same name exists on the target sessionstate.
        /// </summary>
        [Parameter, Alias("NoOverwrite")]
        public SwitchParameter NoClobber { get; set; }

        /// <summary>
        /// Imports a command to the scope specified.
        /// </summary>
        [Parameter]
        [ValidateSet("Local", "Global")]
        public string Scope
        {
            get
            {
                return _scope;
            }

            set
            {
                _scope = value;
                _isScopeSpecified = true;
            }
        }

        private string _scope = string.Empty;
        private bool _isScopeSpecified = false;

        /// <summary>
        /// If specified, then Import-Module will attempt to import PowerShell modules from a remote computer using the specified session.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSet_ViaPsrpSession, Mandatory = true)]
        [Parameter(ParameterSetName = ParameterSet_FQName_ViaPsrpSession, Mandatory = true)]
        [ValidateNotNull]
        public PSSession PSSession { get; set; }

        /// Construct the Import-Module cmdlet object
        public ImportModuleCommand()
        {
            base.BaseDisableNameChecking = false;
        }

        /// <summary>
        /// If specified, then Import-Module will attempt to import PS-CIM modules from a remote computer using the specified session.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSet_ViaCimSession, Mandatory = true)]
        [ValidateNotNull]
        public CimSession CimSession { get; set; }

        /// <summary>
        /// For interoperability with 3rd party CIM servers, user can specify custom resource URI.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSet_ViaCimSession, Mandatory = false)]
        [ValidateNotNull]
        public Uri CimResourceUri { get; set; }

        /// <summary>
        /// For interoperability with 3rd party CIM servers, user can specify custom namespace.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSet_ViaCimSession, Mandatory = false)]
        [ValidateNotNullOrEmpty]
        public string CimNamespace { get; set; }

        /// <summary>
        /// This parameter causes a module to be loaded into Windows PowerShell.
        /// This is mutually exclusive with SkipEditionCheck parameter.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSet_ViaWinCompat, Mandatory = true)]
        [Parameter(ParameterSetName = ParameterSet_FQName_ViaWinCompat, Mandatory = true)]
        [Alias("UseWinPS")]
        public SwitchParameter UseWindowsPowerShell { get; set; }

        #endregion Cmdlet parameters

        #region Local import

        private void ImportModule_ViaLocalModuleInfo(ImportModuleOptions importModuleOptions, PSModuleInfo module)
        {
            try
            {
                PSModuleInfo alreadyLoadedModule = null;
                TryGetFromModuleTable(module.Path, out alreadyLoadedModule);
                if (!BaseForce && DoesAlreadyLoadedModuleSatisfyConstraints(alreadyLoadedModule))
                {
                    AddModuleToModuleTables(this.Context, this.TargetSessionState.Internal, alreadyLoadedModule);

                    // Even if the module has been loaded, import the specified members...
                    ImportModuleMembers(alreadyLoadedModule, this.BasePrefix, importModuleOptions);

                    if (BaseAsCustomObject)
                    {
                        if (alreadyLoadedModule.ModuleType != ModuleType.Script)
                        {
                            string message = StringUtil.Format(Modules.CantUseAsCustomObjectWithBinaryModule, alreadyLoadedModule.Path);
                            InvalidOperationException invalidOp = new InvalidOperationException(message);
                            ErrorRecord er = new ErrorRecord(invalidOp, "Modules_CantUseAsCustomObjectWithBinaryModule",
                                                             ErrorCategory.PermissionDenied, null);
                            WriteError(er);
                        }
                        else
                        {
                            WriteObject(alreadyLoadedModule.AsCustomObject());
                        }
                    }
                    else if (BasePassThru)
                    {
                        WriteObject(alreadyLoadedModule);
                    }
                }
                else
                {
                    PSModuleInfo moduleToRemove;
                    if (TryGetFromModuleTable(module.Path, out moduleToRemove, toRemove: true))
                    {
                        Dbg.Assert(BaseForce, "We should only remove and reload if -Force was specified");
                        RemoveModule(moduleToRemove);
                    }

                    PSModuleInfo moduleToProcess = module;
                    try
                    {
                        // If we're passing in a dynamic module, then the session state will not be
                        // null and we want to just add the module to the module table. Otherwise, it's
                        // a module info from Get-Module -list so we need to read the actual module file.
                        if (module.SessionState == null)
                        {
                            if (File.Exists(module.Path))
                            {
                                bool found;
                                moduleToProcess = LoadModule(module.Path, null, this.BasePrefix, /*SessionState*/ null,
                                                             ref importModuleOptions,
                                                             ManifestProcessingFlags.LoadElements | ManifestProcessingFlags.WriteErrors | ManifestProcessingFlags.NullOnFirstError,
                                                             out found);
                                Dbg.Assert(found, "Module should be found when referenced by its absolute path");
                            }
                        }
                        else if (!string.IsNullOrEmpty(module.Name))
                        {
                            // It has a session state and a name but it's not in the module
                            // table so it's ok to add it

                            // Add it to the all module tables
                            AddModuleToModuleTables(this.Context, this.TargetSessionState.Internal, moduleToProcess);

                            if (moduleToProcess.SessionState != null)
                            {
                                ImportModuleMembers(moduleToProcess, this.BasePrefix, importModuleOptions);
                            }

                            if (BaseAsCustomObject && moduleToProcess.SessionState != null)
                            {
                                WriteObject(module.AsCustomObject());
                            }
                            else if (BasePassThru)
                            {
                                WriteObject(moduleToProcess);
                            }
                        }
                    }
                    catch (IOException)
                    {
                    }
                }
            }
            catch (PSInvalidOperationException e)
            {
                ErrorRecord er = new ErrorRecord(e.ErrorRecord, e);
                WriteError(er);
            }
        }

        private void ImportModule_ViaAssembly(ImportModuleOptions importModuleOptions, Assembly suppliedAssembly)
        {
            bool moduleLoaded = false;
            string moduleName = "dynamic_code_module_" + suppliedAssembly.FullName;

            // Loop through Module Cache to ensure that the module is not already imported.
            foreach (KeyValuePair<string, PSModuleInfo> pair in Context.Modules.ModuleTable)
            {
                if (pair.Value.Path == string.Empty)
                {
                    // If the module in the moduleTable is an assembly module without path, the moduleName is the key.
                    if (pair.Key.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                    {
                        moduleLoaded = true;
                        if (BasePassThru)
                        {
                            WriteObject(pair.Value);
                        }

                        break;
                    }

                    continue;
                }

                if (pair.Value.Path.Equals(suppliedAssembly.Location, StringComparison.OrdinalIgnoreCase))
                {
                    moduleLoaded = true;
                    if (BasePassThru)
                    {
                        WriteObject(pair.Value);
                    }

                    break;
                }
            }

            if (!moduleLoaded)
            {
                PSModuleInfo module = LoadBinaryModule(
                    parentModule: null,
                    moduleName: null,
                    fileName: null,
                    suppliedAssembly,
                    moduleBase: null,
                    ss: null,
                    importModuleOptions,
                    ManifestProcessingFlags.LoadElements | ManifestProcessingFlags.WriteErrors | ManifestProcessingFlags.NullOnFirstError,
                    BasePrefix,
                    out bool found);

                if (found && module is not null)
                {
                    // Add it to all module tables ...
                    AddModuleToModuleTables(Context, TargetSessionState.Internal, module);
                    if (BasePassThru)
                    {
                        WriteObject(module);
                    }
                }
            }
        }

        private PSModuleInfo ImportModule_LocallyViaName_WithTelemetry(ImportModuleOptions importModuleOptions, string name)
        {
            PSModuleInfo foundModule = ImportModule_LocallyViaName(importModuleOptions, name);
            if (foundModule != null)
            {
                SetModuleBaseForEngineModules(foundModule.Name, this.Context);

                // report loading of the module in telemetry
                // avoid double reporting for WinCompat modules that go through CommandDiscovery\AutoloadSpecifiedModule
                if (!foundModule.IsWindowsPowerShellCompatModule)
                {
                    ApplicationInsightsTelemetry.SendModuleTelemetryMetric(TelemetryType.ModuleLoad, foundModule);
#if LEGACYTELEMETRY
                    TelemetryAPI.ReportModuleLoad(foundModule);
#endif
                }
            }

            return foundModule;
        }

        private PSModuleInfo ImportModule_LocallyViaName(ImportModuleOptions importModuleOptions, string name)
        {
            bool shallWriteError = !importModuleOptions.SkipSystem32ModulesAndSuppressError;

            try
            {
                bool found = false;
                PSModuleInfo foundModule = null;

                string cachedPath = null;
                string rootedPath = null;

                // See if we can use the cached path for the file. If a version number has been specified, then
                // we won't look in the cache
                if (this.MinimumVersion == null && this.MaximumVersion == null && this.RequiredVersion == null && PSModuleInfo.UseAppDomainLevelModuleCache && !this.BaseForce)
                {
                    // See if the name is in the appdomain-level module path name cache...
                    cachedPath = PSModuleInfo.ResolveUsingAppDomainLevelModuleCache(name);
                }

                if (!string.IsNullOrEmpty(cachedPath))
                {
                    if (File.Exists(cachedPath))
                    {
                        rootedPath = cachedPath;
                    }
                    else
                    {
                        PSModuleInfo.RemoveFromAppDomainLevelCache(name);
                    }
                }

                // If null check for full-qualified paths - either absolute or relative
                rootedPath ??= ResolveRootedFilePath(name, this.Context);

                bool alreadyLoaded = false;
                var manifestProcessingFlags = ManifestProcessingFlags.LoadElements | ManifestProcessingFlags.NullOnFirstError;
                if (shallWriteError)
                {
                    manifestProcessingFlags |= ManifestProcessingFlags.WriteErrors;
                }

                if (!string.IsNullOrEmpty(rootedPath))
                {
                    // If the module has already been loaded, just emit it and continue...
                    if (!BaseForce && TryGetFromModuleTable(rootedPath, out PSModuleInfo module))
                    {
                        if (module.ModuleType != ModuleType.Manifest
                            || ModuleIntrinsics.IsVersionMatchingConstraints(module.Version, RequiredVersion, BaseMinimumVersion, BaseMaximumVersion))
                        {
                            alreadyLoaded = true;
                            AddModuleToModuleTables(this.Context, this.TargetSessionState.Internal, module);
                            ImportModuleMembers(module, this.BasePrefix, importModuleOptions);

                            if (BaseAsCustomObject)
                            {
                                if (module.ModuleType != ModuleType.Script)
                                {
                                    string message = StringUtil.Format(Modules.CantUseAsCustomObjectWithBinaryModule, module.Path);
                                    InvalidOperationException invalidOp = new InvalidOperationException(message);
                                    ErrorRecord er = new ErrorRecord(invalidOp, "Modules_CantUseAsCustomObjectWithBinaryModule",
                                                                     ErrorCategory.PermissionDenied, null);
                                    WriteError(er);
                                }
                                else
                                {
                                    WriteObject(module.AsCustomObject());
                                }
                            }
                            else if (BasePassThru)
                            {
                                WriteObject(module);
                            }

                            found = true;
                            foundModule = module;
                        }
                    }

                    if (!alreadyLoaded)
                    {
                        // If the path names a file, load that file...
                        if (File.Exists(rootedPath))
                        {
                            PSModuleInfo moduleToRemove;
                            if (TryGetFromModuleTable(rootedPath, out moduleToRemove, toRemove: true))
                            {
                                RemoveModule(moduleToRemove);
                            }

                            foundModule = LoadModule(
                                fileName: rootedPath,
                                moduleBase: null,
                                prefix: BasePrefix,
                                ss: null, /*SessionState*/
                                ref importModuleOptions,
                                manifestProcessingFlags,
                                out found);
                        }
                        else if (Directory.Exists(rootedPath))
                        {
                            // If the path ends with a directory separator, remove it
                            if (rootedPath.EndsWith(Path.DirectorySeparatorChar))
                            {
                                rootedPath = Path.GetDirectoryName(rootedPath);
                            }

                            // Load the latest valid version if it is a multi-version module directory
                            foundModule = LoadUsingMultiVersionModuleBase(rootedPath, manifestProcessingFlags, importModuleOptions, out found);

                            if (!found)
                            {
                                // If the path is a directory, double up the end of the string
                                // then try to load that using extensions...
                                rootedPath = Path.Combine(rootedPath, Path.GetFileName(rootedPath));
                                foundModule = LoadUsingExtensions(
                                    parentModule: null,
                                    moduleName: rootedPath,
                                    fileBaseName: rootedPath,
                                    extension: null,
                                    moduleBase: null,
                                    prefix: BasePrefix,
                                    ss: null, /*SessionState*/
                                    importModuleOptions,
                                    manifestProcessingFlags,
                                    out found);
                            }
                        }
                    }
                }
                else
                {
                    // Check if module could be a snapin. This was the case for PowerShell version 2 engine modules.
                    if (InitialSessionState.IsEngineModule(name))
                    {
                        PSSnapInInfo snapin = Context.CurrentRunspace.InitialSessionState.GetPSSnapIn(name);

                        // Return the command if we found a module
                        if (snapin != null)
                        {
                            // warn that this module already exists as a snapin
                            string warningMessage = string.Format(
                                CultureInfo.InvariantCulture,
                                Modules.ModuleLoadedAsASnapin,
                                snapin.Name);
                            WriteWarning(warningMessage);
                            found = true;
                            return foundModule;
                        }
                    }

                    // At this point, the name didn't resolve to an existing file or directory.
                    // It may still be rooted (relative or absolute). If it is, then we'll only use
                    // the extension search. If it's not rooted, use a path-based search.
                    if (IsRooted(name))
                    {
                        // If there is no extension, we'll have to search using the extensions
                        if (!string.IsNullOrEmpty(Path.GetExtension(name)))
                        {
                            foundModule = LoadModule(
                                fileName: name,
                                moduleBase: null,
                                prefix: BasePrefix,
                                ss: null, /*SessionState*/
                                ref importModuleOptions,
                                manifestProcessingFlags,
                                out found);
                        }
                        else
                        {
                            foundModule = LoadUsingExtensions(
                                parentModule: null,
                                moduleName: name,
                                fileBaseName: name,
                                extension: null,
                                moduleBase: null,
                                prefix: BasePrefix,
                                ss: null, /*SessionState*/
                                importModuleOptions,
                                manifestProcessingFlags,
                                out found);
                        }
                    }
                    else
                    {
                        IEnumerable<string> modulePath = ModuleIntrinsics.GetModulePath(false, this.Context);

                        if (this.MinimumVersion == null && this.RequiredVersion == null && this.MaximumVersion == null)
                        {
                            this.AddToAppDomainLevelCache = true;
                        }

                        found = LoadUsingModulePath(
                            modulePath,
                            name,
                            ss: null, /* SessionState*/
                            importModuleOptions,
                            manifestProcessingFlags,
                            out foundModule);
                    }
                }

                if (!found && shallWriteError)
                {
                    ErrorRecord er = null;
                    string message = null;
                    if (BaseRequiredVersion != null)
                    {
                        message = StringUtil.Format(Modules.ModuleWithVersionNotFound, name, BaseRequiredVersion);
                    }
                    else if (BaseMinimumVersion != null && BaseMaximumVersion != null)
                    {
                        message = StringUtil.Format(Modules.MinimumVersionAndMaximumVersionNotFound, name, BaseMinimumVersion, BaseMaximumVersion);
                    }
                    else if (BaseMinimumVersion != null)
                    {
                        message = StringUtil.Format(Modules.ModuleWithVersionNotFound, name, BaseMinimumVersion);
                    }
                    else if (BaseMaximumVersion != null)
                    {
                        message = StringUtil.Format(Modules.MaximumVersionNotFound, name, BaseMaximumVersion);
                    }

                    if (BaseRequiredVersion != null || BaseMinimumVersion != null || BaseMaximumVersion != null)
                    {
                        FileNotFoundException fnf = new FileNotFoundException(message);
                        er = new ErrorRecord(fnf, "Modules_ModuleWithVersionNotFound",
                                             ErrorCategory.ResourceUnavailable, name);
                    }
                    else
                    {
                        message = StringUtil.Format(Modules.ModuleNotFound, name);
                        FileNotFoundException fnf = new FileNotFoundException(message);
                        er = new ErrorRecord(fnf, "Modules_ModuleNotFound",
                                             ErrorCategory.ResourceUnavailable, name);
                    }

                    WriteError(er);
                }

                return foundModule;
            }
            catch (PSInvalidOperationException e)
            {
                if (shallWriteError)
                {
                    WriteError(new ErrorRecord(e.ErrorRecord, e));
                }
            }

            return null;
        }

        private PSModuleInfo ImportModule_LocallyViaFQName(ImportModuleOptions importModuleOptions, ModuleSpecification modulespec)
        {
            RequiredVersion = modulespec.RequiredVersion;
            MinimumVersion = modulespec.Version;
            MaximumVersion = modulespec.MaximumVersion;
            BaseGuid = modulespec.Guid;

            PSModuleInfo foundModule = ImportModule_LocallyViaName(importModuleOptions, modulespec.Name);

            if (foundModule != null)
            {
                ApplicationInsightsTelemetry.SendModuleTelemetryMetric(TelemetryType.ModuleLoad, foundModule);
                SetModuleBaseForEngineModules(foundModule.Name, this.Context);
            }

            return foundModule;
        }

        #endregion Local import

        #region Remote import

        #region PSSession parameterset

        private IList<PSModuleInfo> ImportModule_RemotelyViaPsrpSession(
            ImportModuleOptions importModuleOptions,
            IEnumerable<string> moduleNames,
            IEnumerable<ModuleSpecification> fullyQualifiedNames,
            PSSession psSession,
            bool usingWinCompat = false)
        {
            var remotelyImportedModules = new List<PSModuleInfo>();
            if (moduleNames != null)
            {
                foreach (string moduleName in moduleNames)
                {
                    var tmp = ImportModule_RemotelyViaPsrpSession(importModuleOptions, moduleName, null, psSession);
                    remotelyImportedModules.AddRange(tmp);
                }
            }

            if (fullyQualifiedNames != null)
            {
                foreach (var fullyQualifiedName in fullyQualifiedNames)
                {
                    var tmp = ImportModule_RemotelyViaPsrpSession(importModuleOptions, null, fullyQualifiedName, psSession);
                    remotelyImportedModules.AddRange(tmp);
                }
            }

            // Send telemetry on the imported modules
            foreach (PSModuleInfo moduleInfo in remotelyImportedModules)
            {
                ApplicationInsightsTelemetry.SendModuleTelemetryMetric(usingWinCompat ? TelemetryType.WinCompatModuleLoad : TelemetryType.ModuleLoad, moduleInfo);
            }

            return remotelyImportedModules;
        }

        private IList<PSModuleInfo> ImportModule_RemotelyViaPsrpSession(
            ImportModuleOptions importModuleOptions,
            string moduleName,
            ModuleSpecification fullyQualifiedName,
            PSSession psSession)
        {
            //
            // import the module in the remote session first
            //
            List<PSObject> remotelyImportedModules;
            using (var powerShell = System.Management.Automation.PowerShell.Create())
            {
                powerShell.Runspace = psSession.Runspace;
                powerShell.AddCommand("Import-Module");
                powerShell.AddParameter("DisableNameChecking", this.DisableNameChecking);
                powerShell.AddParameter("PassThru", true);

                if (fullyQualifiedName != null)
                {
                    powerShell.AddParameter("FullyQualifiedName", fullyQualifiedName);
                }
                else
                {
                    powerShell.AddParameter("Name", moduleName);

                    if (this.MinimumVersion != null)
                    {
                        powerShell.AddParameter("Version", this.MinimumVersion);
                    }

                    if (this.RequiredVersion != null)
                    {
                        powerShell.AddParameter("RequiredVersion", this.RequiredVersion);
                    }

                    if (this.MaximumVersion != null)
                    {
                        powerShell.AddParameter("MaximumVersion", this.MaximumVersion);
                    }
                }

                if (this.ArgumentList != null)
                {
                    powerShell.AddParameter("ArgumentList", this.ArgumentList);
                }

                if (this.BaseForce)
                {
                    powerShell.AddParameter("Force", true);
                }

                string errorMessageTemplate = string.Format(
                    CultureInfo.InvariantCulture,
                    Modules.RemoteDiscoveryRemotePsrpCommandFailed,
                    string.Create(CultureInfo.InvariantCulture, $"Import-Module -Name '{moduleName}'"));
                remotelyImportedModules = RemoteDiscoveryHelper.InvokePowerShell(
                    powerShell,
                    this,
                    errorMessageTemplate,
                    this.CancellationToken).ToList();
            }

            List<PSModuleInfo> result = new List<PSModuleInfo>();
            foreach (PSObject remotelyImportedModule in remotelyImportedModules)
            {
                PSPropertyInfo nameProperty = remotelyImportedModule.Properties["Name"];
                if (nameProperty != null)
                {
                    string remoteModuleName = (string)LanguagePrimitives.ConvertTo(
                        nameProperty.Value,
                        typeof(string),
                        CultureInfo.InvariantCulture);

                    PSPropertyInfo helpInfoProperty = remotelyImportedModule.Properties["HelpInfoUri"];
                    string remoteHelpInfoUri = null;
                    if (helpInfoProperty != null)
                    {
                        remoteHelpInfoUri = (string)LanguagePrimitives.ConvertTo(
                            helpInfoProperty.Value,
                            typeof(string),
                            CultureInfo.InvariantCulture);
                    }

                    PSPropertyInfo guidProperty = remotelyImportedModule.Properties["Guid"];
                    Guid remoteModuleGuid = Guid.Empty;
                    if (guidProperty != null)
                    {
                        LanguagePrimitives.TryConvertTo(guidProperty.Value, out remoteModuleGuid);
                    }

                    PSPropertyInfo versionProperty = remotelyImportedModule.Properties["Version"];
                    Version remoteModuleVersion = null;
                    if (versionProperty != null)
                    {
                        Version tmp;
                        if (LanguagePrimitives.TryConvertTo<Version>(versionProperty.Value, CultureInfo.InvariantCulture, out tmp))
                        {
                            remoteModuleVersion = tmp;
                        }
                    }

                    PSModuleInfo moduleInfo = ImportModule_RemotelyViaPsrpSession_SinglePreimportedModule(
                        importModuleOptions,
                        remoteModuleName,
                        remoteModuleVersion,
                        psSession);

                    // Set the HelpInfoUri and Guid as necessary, so that Save-Help can work with this module object
                    // to retrieve help files from the remote site.
                    if (moduleInfo != null)
                    {
                        // set the HelpInfoUri if it's needed
                        if (string.IsNullOrEmpty(moduleInfo.HelpInfoUri) && !string.IsNullOrEmpty(remoteHelpInfoUri))
                        {
                            moduleInfo.SetHelpInfoUri(remoteHelpInfoUri);
                        }

                        // set the Guid if it's needed
                        if (remoteModuleGuid != Guid.Empty)
                        {
                            moduleInfo.SetGuid(remoteModuleGuid);
                        }

                        result.Add(moduleInfo);
                    }
                }
            }

            return result;
        }

        private PSModuleInfo ImportModule_RemotelyViaPsrpSession_SinglePreimportedModule(
            ImportModuleOptions importModuleOptions,
            string remoteModuleName,
            Version remoteModuleVersion,
            PSSession psSession)
        {
            string temporaryModulePath = RemoteDiscoveryHelper.GetModulePath(
                remoteModuleName,
                remoteModuleVersion,
                psSession.ComputerName,
                this.Context.CurrentRunspace);
            string wildcardEscapedPath = WildcardPattern.Escape(temporaryModulePath);
            try
            {
                //
                // avoid importing a module twice
                //
                string localPsm1File = Path.Combine(temporaryModulePath, Path.GetFileName(temporaryModulePath) + ".psm1");
                PSModuleInfo alreadyImportedModule = this.IsModuleImportUnnecessaryBecauseModuleIsAlreadyLoaded(
                    localPsm1File, this.BasePrefix, importModuleOptions);
                if (alreadyImportedModule != null)
                {
                    return alreadyImportedModule;
                }

                //
                // create proxy module in a temporary folder
                //
                using (var powerShell = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace))
                {
                    powerShell.AddCommand("Export-PSSession");
                    powerShell.AddParameter("OutputModule", wildcardEscapedPath);
                    if (!importModuleOptions.NoClobberExportPSSession)
                    {
                        powerShell.AddParameter("AllowClobber", true);
                    }
                    powerShell.AddParameter("Module", remoteModuleName); // remoteModulePath is currently unsupported by Get-Command and implicit remoting
                    powerShell.AddParameter("Force", true);
                    powerShell.AddParameter("FormatTypeName", "*");
                    powerShell.AddParameter("Session", psSession);

                    string errorMessageTemplate = string.Format(
                        CultureInfo.InvariantCulture,
                        Modules.RemoteDiscoveryFailedToGenerateProxyForRemoteModule,
                        remoteModuleName);
                    if (!RemoteDiscoveryHelper.InvokePowerShell(powerShell, this, errorMessageTemplate, this.CancellationToken).Any())
                    {
                        return null;
                    }
                }

                //
                // rename the psd1 file
                //
                string localPsd1File = Path.Combine(temporaryModulePath, remoteModuleName + ".psd1");
                if (File.Exists(localPsd1File))
                {
                    File.Delete(localPsd1File);
                }

                File.Move(
                    sourceFileName: Path.Combine(temporaryModulePath, Path.GetFileName(temporaryModulePath) + ".psd1"),
                    destFileName: localPsd1File);
                string wildcardEscapedPsd1Path = WildcardPattern.Escape(localPsd1File);

                //
                // import the proxy module just as any other local module
                //
                object[] oldArgumentList = this.ArgumentList;
                Version originalBaseMinimumVersion = BaseMinimumVersion;
                Version originalBaseMaximumVersion = BaseMaximumVersion;
                Version originalBaseRequiredVersion = BaseRequiredVersion;
                try
                {
                    this.ArgumentList = new object[] { psSession };

                    // The correct module version has already been imported from the remote session and created locally.
                    // The locally created module always has a version of 1.0 regardless of the actual module version
                    // imported from the remote session, and version checking is no longer needed and will not work while
                    // importing this created local module.
                    BaseMinimumVersion = null;
                    BaseMaximumVersion = null;
                    BaseRequiredVersion = null;
                    ImportModule_LocallyViaName(importModuleOptions, wildcardEscapedPsd1Path);
                }
                finally
                {
                    this.ArgumentList = oldArgumentList;
                    BaseMinimumVersion = originalBaseMinimumVersion;
                    BaseMaximumVersion = originalBaseMaximumVersion;
                    BaseRequiredVersion = originalBaseRequiredVersion;
                }

                //
                // make sure the temporary folder gets removed when the module is removed
                //
                string psm1Path = Path.Combine(temporaryModulePath, Path.GetFileName(temporaryModulePath) + ".psm1");
                if (!TryGetFromModuleTable(psm1Path, out PSModuleInfo moduleInfo, toRemove: true))
                {
                    if (Directory.Exists(temporaryModulePath))
                    {
                        Directory.Delete(temporaryModulePath, recursive: true);
                    }

                    return null;
                }

                const string onRemoveScriptBody = @"
                    Microsoft.PowerShell.Management\Remove-Item `
                        -LiteralPath $temporaryModulePath `
                        -Force `
                        -Recurse `
                        -ErrorAction SilentlyContinue

                    if ($null -ne $previousOnRemoveScript)
                    {
                        & $previousOnRemoveScript $args
                    }
                    ";
                ScriptBlock onRemoveScriptBlock = this.Context.Engine.ParseScriptBlock(onRemoveScriptBody, false);
                onRemoveScriptBlock = onRemoveScriptBlock.GetNewClosure(); // create a separate scope for variables set below
                onRemoveScriptBlock.Module.SessionState.PSVariable.Set("temporaryModulePath", temporaryModulePath);
                onRemoveScriptBlock.Module.SessionState.PSVariable.Set("previousOnRemoveScript", moduleInfo.OnRemove);
                moduleInfo.OnRemove = onRemoveScriptBlock;

                return moduleInfo;
            }
            catch
            {
                if (Directory.Exists(temporaryModulePath))
                {
                    Directory.Delete(temporaryModulePath, recursive: true);
                }

                throw;
            }
        }

        #endregion PSSession parameterset

        #region CimSession parameterset

        private static bool IsNonEmptyManifestField(Hashtable manifestData, string key)
        {
            object value = manifestData[key];
            if (value == null)
            {
                return false;
            }

            object[] array;
            if (LanguagePrimitives.TryConvertTo(value, CultureInfo.InvariantCulture, out array))
            {
                return array.Length != 0;
            }
            else
            {
                return true;
            }
        }

        private bool IsMixedModePsCimModule(RemoteDiscoveryHelper.CimModule cimModule)
        {
            string temporaryModuleManifestPath = RemoteDiscoveryHelper.GetModulePath(cimModule.ModuleName, null, string.Empty, this.Context.CurrentRunspace);
            bool containedErrors = false;
            RemoteDiscoveryHelper.CimModuleFile mainManifestFile = cimModule.MainManifest;
            if (mainManifestFile == null)
            {
                return true;
            }

            Hashtable manifestData = RemoteDiscoveryHelper.ConvertCimModuleFileToManifestHashtable(
                    mainManifestFile,
                    temporaryModuleManifestPath,
                    this,
                    ref containedErrors);

            if (containedErrors || manifestData == null)
            {
                return false;
            }

            if (IsNonEmptyManifestField(manifestData, "ScriptsToProcess") ||
                IsNonEmptyManifestField(manifestData, "RequiredAssemblies"))
            {
                return true;
            }

            int numberOfSubmodules = 0;

            string[] nestedModules = null;
            if (LanguagePrimitives.TryConvertTo(manifestData["NestedModules"], CultureInfo.InvariantCulture, out nestedModules))
            {
                if (nestedModules != null)
                {
                    numberOfSubmodules += nestedModules.Length;
                }
            }

            object rootModuleValue = manifestData["RootModule"];
            if (rootModuleValue != null)
            {
                string rootModule;
                if (LanguagePrimitives.TryConvertTo(rootModuleValue, CultureInfo.InvariantCulture, out rootModule))
                {
                    if (!string.IsNullOrEmpty(rootModule))
                    {
                        numberOfSubmodules += 1;
                    }
                }
            }
            else
            {
                object moduleToProcessValue = manifestData["ModuleToProcess"];
                string moduleToProcess;
                if (moduleToProcessValue != null && LanguagePrimitives.TryConvertTo(moduleToProcessValue, CultureInfo.InvariantCulture, out moduleToProcess))
                {
                    if (!string.IsNullOrEmpty(moduleToProcess))
                    {
                        numberOfSubmodules += 1;
                    }
                }
            }

            int numberOfCmdletizationFiles = 0;
            foreach (var moduleFile in cimModule.ModuleFiles)
            {
                if (moduleFile.FileCode == RemoteDiscoveryHelper.CimFileCode.CmdletizationV1)
                    numberOfCmdletizationFiles++;
            }

            bool isMixedModePsCimModule = numberOfSubmodules > numberOfCmdletizationFiles;
            return isMixedModePsCimModule;
        }

        private void ImportModule_RemotelyViaCimSession(
            ImportModuleOptions importModuleOptions,
            string[] moduleNames,
            CimSession cimSession,
            Uri resourceUri,
            string cimNamespace)
        {
            //
            // find all remote PS-CIM modules
            //
            IEnumerable<RemoteDiscoveryHelper.CimModule> remoteModules = RemoteDiscoveryHelper.GetCimModules(
                cimSession,
                resourceUri,
                cimNamespace,
                moduleNames,
                false /* onlyManifests */,
                this,
                this.CancellationToken).ToList();

            IEnumerable<RemoteDiscoveryHelper.CimModule> remotePsCimModules = remoteModules.Where(static cimModule => cimModule.IsPsCimModule);
            IEnumerable<string> remotePsrpModuleNames = remoteModules.Where(static cimModule => !cimModule.IsPsCimModule).Select(static cimModule => cimModule.ModuleName);
            foreach (string psrpModuleName in remotePsrpModuleNames)
            {
                string errorMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    Modules.PsModuleOverCimSessionError,
                    psrpModuleName);
                ErrorRecord errorRecord = new ErrorRecord(
                    new ArgumentException(errorMessage),
                    "PsModuleOverCimSessionError",
                    ErrorCategory.InvalidArgument,
                    psrpModuleName);
                this.WriteError(errorRecord);
            }

            //
            // report an error if some modules were not found
            //
            IEnumerable<string> allFoundModuleNames = remoteModules.Select(static cimModule => cimModule.ModuleName).ToList();
            foreach (string requestedModuleName in moduleNames)
            {
                var wildcardPattern = WildcardPattern.Get(requestedModuleName, WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant);
                bool requestedModuleWasFound = allFoundModuleNames.Any(foundModuleName => wildcardPattern.IsMatch(foundModuleName));
                if (!requestedModuleWasFound)
                {
                    string message = StringUtil.Format(Modules.ModuleNotFound, requestedModuleName);
                    FileNotFoundException fnf = new FileNotFoundException(message);
                    ErrorRecord er = new ErrorRecord(fnf, "Modules_ModuleNotFound",
                        ErrorCategory.ResourceUnavailable, requestedModuleName);
                    WriteError(er);
                }
            }

            //
            // import the PS-CIM modules
            //
            foreach (RemoteDiscoveryHelper.CimModule remoteCimModule in remotePsCimModules)
            {
                ImportModule_RemotelyViaCimModuleData(importModuleOptions, remoteCimModule, cimSession);
                // we don't know the version of the module
                ApplicationInsightsTelemetry.SendModuleTelemetryMetric(TelemetryType.ModuleLoad, remoteCimModule.ModuleName);
            }
        }

        private bool IsPs1xmlFileHelper_IsPresentInEntries(RemoteDiscoveryHelper.CimModuleFile cimModuleFile, IEnumerable<string> manifestEntries)
        {
            const string ps1xmlExt = ".ps1xml";
            string fileName = cimModuleFile.FileName;

            foreach (string entry in manifestEntries)
            {
                if (entry.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            foreach (string entry in manifestEntries)
            {
                string tempName = entry.EndsWith(ps1xmlExt, StringComparison.OrdinalIgnoreCase) ? entry : entry + ps1xmlExt;
                string resolvedPath = ResolveRootedFilePath(tempName, Context);
                if (resolvedPath is not null && resolvedPath.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsPs1xmlFileHelper(RemoteDiscoveryHelper.CimModuleFile cimModuleFile, Hashtable manifestData, string goodKey, string badKey)
        {
            if (!Path.GetExtension(cimModuleFile.FileName).Equals(".ps1xml", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            List<string> goodEntries;
            if (!this.GetListOfStringsFromData(manifestData, null, goodKey, 0, out goodEntries))
            {
                goodEntries = new List<string>();
            }

            goodEntries ??= new List<string>();

            List<string> badEntries;
            if (!this.GetListOfStringsFromData(manifestData, null, badKey, 0, out badEntries))
            {
                badEntries = new List<string>();
            }

            badEntries ??= new List<string>();

            bool presentInGoodEntries = IsPs1xmlFileHelper_IsPresentInEntries(cimModuleFile, goodEntries);
            bool presentInBadEntries = IsPs1xmlFileHelper_IsPresentInEntries(cimModuleFile, badEntries);
            return presentInGoodEntries && !presentInBadEntries;
        }

        private bool IsTypesPs1XmlFile(RemoteDiscoveryHelper.CimModuleFile cimModuleFile, Hashtable manifestData)
        {
            return IsPs1xmlFileHelper(cimModuleFile, manifestData, goodKey: "TypesToProcess", badKey: "FormatsToProcess");
        }

        private bool IsFormatPs1XmlFile(RemoteDiscoveryHelper.CimModuleFile cimModuleFile, Hashtable manifestData)
        {
            return IsPs1xmlFileHelper(cimModuleFile, manifestData, goodKey: "FormatsToProcess", badKey: "TypesToProcess");
        }

        private static bool IsCmdletizationFile(RemoteDiscoveryHelper.CimModuleFile cimModuleFile)
        {
            return cimModuleFile.FileCode == RemoteDiscoveryHelper.CimFileCode.CmdletizationV1;
        }

        private static IEnumerable<string> CreateCimModuleFiles(
            RemoteDiscoveryHelper.CimModule remoteCimModule,
            RemoteDiscoveryHelper.CimFileCode fileCode,
            Func<RemoteDiscoveryHelper.CimModuleFile, bool> filesFilter,
            string temporaryModuleDirectory)
        {
            string fileNameTemplate = null;
            switch (fileCode)
            {
                case RemoteDiscoveryHelper.CimFileCode.CmdletizationV1:
                    fileNameTemplate = "{0}_{1}.cdxml";
                    break;
                case RemoteDiscoveryHelper.CimFileCode.TypesV1:
                    fileNameTemplate = "{0}_{1}.types.ps1xml";
                    break;
                case RemoteDiscoveryHelper.CimFileCode.FormatV1:
                    fileNameTemplate = "{0}_{1}.format.ps1xml";
                    break;
                default:
                    Dbg.Assert(false, "Unrecognized file code");
                    break;
            }

            List<string> relativePathsToCreatedFiles = new List<string>();
            foreach (RemoteDiscoveryHelper.CimModuleFile file in remoteCimModule.ModuleFiles)
            {
                if (!filesFilter(file))
                {
                    continue;
                }

                string originalFileName = Path.GetFileName(file.FileName);
                string fileName = string.Format(
                    CultureInfo.InvariantCulture,
                    fileNameTemplate,
                    originalFileName.Substring(0, Math.Min(originalFileName.Length, 20)),
                    Path.GetRandomFileName());
                relativePathsToCreatedFiles.Add(fileName);

                string fullPath = Path.Combine(temporaryModuleDirectory, fileName);
                File.WriteAllBytes(
                    fullPath,
                    file.RawFileData);

#if !UNIX
                AlternateDataStreamUtilities.SetZoneOfOrigin(fullPath, SecurityZone.Intranet);
#endif
            }

            return relativePathsToCreatedFiles;
        }

        private PSModuleInfo ImportModule_RemotelyViaCimModuleData(
            ImportModuleOptions importModuleOptions,
            RemoteDiscoveryHelper.CimModule remoteCimModule,
            CimSession cimSession)
        {
            try
            {
                if (remoteCimModule.MainManifest == null)
                {
                    string errorMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        Modules.EmptyModuleManifest,
                        remoteCimModule.ModuleName + ".psd1");
                    ArgumentException argumentException = new ArgumentException(errorMessage);
                    throw argumentException;
                }

                bool containedErrors = false;
                PSModuleInfo moduleInfo = null;

                //
                // read the original manifest
                //
                string temporaryModuleDirectory = RemoteDiscoveryHelper.GetModulePath(
                    remoteCimModule.ModuleName,
                    null,
                    cimSession.ComputerName,
                    this.Context.CurrentRunspace);
                string temporaryModuleManifestPath = Path.Combine(
                    temporaryModuleDirectory,
                    remoteCimModule.ModuleName + ".psd1");

                Hashtable data = null;
                Hashtable localizedData = null;
                {
                    ScriptBlockAst scriptBlockAst = null;
                    Token[] throwAwayTokens;
                    ParseError[] parseErrors;
                    scriptBlockAst = Parser.ParseInput(
                        remoteCimModule.MainManifest.FileData,
                        temporaryModuleManifestPath,
                        out throwAwayTokens,
                        out parseErrors);
                    if ((scriptBlockAst == null) ||
                        (parseErrors != null && parseErrors.Length > 0))
                    {
                        throw new ParseException(parseErrors);
                    }

                    ScriptBlock scriptBlock = new ScriptBlock(scriptBlockAst, isFilter: false);
                    data = LoadModuleManifestData(
                        temporaryModuleManifestPath,
                        scriptBlock,
                        ModuleManifestMembers,
                        ManifestProcessingFlags.NullOnFirstError | ManifestProcessingFlags.WriteErrors, /* - don't load elements */
                        ref containedErrors);

                    if ((data == null) || containedErrors)
                    {
                        return null;
                    }

                    localizedData = data;
                }

                //
                // flatten module contents and rewrite the manifest to point to the flattened file hierarchy
                //

                // recalculate module path, taking into account the module version fetched above
                Version moduleVersion;
                if (!GetScalarFromData<Version>(data, null, "ModuleVersion", 0, out moduleVersion))
                {
                    moduleVersion = null;
                }

                temporaryModuleDirectory = RemoteDiscoveryHelper.GetModulePath(
                    remoteCimModule.ModuleName,
                    moduleVersion,
                    cimSession.ComputerName,
                    this.Context.CurrentRunspace);
                temporaryModuleManifestPath = Path.Combine(
                    temporaryModuleDirectory,
                    remoteCimModule.ModuleName + ".psd1");
                // avoid loading an already loaded module
                PSModuleInfo alreadyImportedModule = this.IsModuleImportUnnecessaryBecauseModuleIsAlreadyLoaded(
                    temporaryModuleManifestPath, this.BasePrefix, importModuleOptions);
                if (alreadyImportedModule != null)
                {
                    return alreadyImportedModule;
                }

                try
                {
                    Directory.CreateDirectory(temporaryModuleDirectory);

                    IEnumerable<string> typesToProcess = CreateCimModuleFiles(
                        remoteCimModule,
                        RemoteDiscoveryHelper.CimFileCode.TypesV1,
                        cimModuleFile => IsTypesPs1XmlFile(cimModuleFile, data),
                        temporaryModuleDirectory);
                    IEnumerable<string> formatsToProcess = CreateCimModuleFiles(
                        remoteCimModule,
                        RemoteDiscoveryHelper.CimFileCode.FormatV1,
                        cimModuleFile => IsFormatPs1XmlFile(cimModuleFile, data),
                        temporaryModuleDirectory);
                    IEnumerable<string> nestedModules = CreateCimModuleFiles(
                        remoteCimModule,
                        RemoteDiscoveryHelper.CimFileCode.CmdletizationV1,
                        IsCmdletizationFile,
                        temporaryModuleDirectory);
                    data = RemoteDiscoveryHelper.RewriteManifest(
                        data,
                        nestedModules: nestedModules,
                        typesToProcess: typesToProcess,
                        formatsToProcess: formatsToProcess);
                    localizedData = RemoteDiscoveryHelper.RewriteManifest(localizedData);

                    //
                    // import the module
                    // (from memory - this avoids the authenticode signature problems
                    // that would be introduced by rewriting the contents of the manifest)
                    //
                    moduleInfo = LoadModuleManifest(
                        temporaryModuleManifestPath,
                        null, // scriptInfo
                        data,
                        localizedData,
                        ManifestProcessingFlags.LoadElements | ManifestProcessingFlags.WriteErrors | ManifestProcessingFlags.NullOnFirstError,
                        BaseMinimumVersion,
                        BaseMaximumVersion,
                        BaseRequiredVersion,
                        BaseGuid,
                        ref importModuleOptions,
                        ref containedErrors);
                    if (moduleInfo == null)
                    {
                        return null;
                    }

                    foreach (PSModuleInfo nestedModule in moduleInfo.NestedModules)
                    {
                        Type cmdletAdapter;
                        bool gotCmdletAdapter = PSPrimitiveDictionary.TryPathGet(
                            nestedModule.PrivateData as IDictionary,
                            out cmdletAdapter,
                            "CmdletsOverObjects",
                            "CmdletAdapter");
                        Dbg.Assert(gotCmdletAdapter, "PrivateData from cdxml should always include cmdlet adapter");
                        if (!cmdletAdapter.AssemblyQualifiedName.Equals(StringLiterals.DefaultCmdletAdapter, StringComparison.OrdinalIgnoreCase))
                        {
                            string errorMessage = string.Format(
                                CultureInfo.InvariantCulture,
                                CmdletizationCoreResources.ImportModule_UnsupportedCmdletAdapter,
                                cmdletAdapter.FullName);
                            ErrorRecord errorRecord = new ErrorRecord(
                                new InvalidOperationException(errorMessage),
                                "UnsupportedCmdletAdapter",
                                ErrorCategory.InvalidData,
                                cmdletAdapter);
                            this.ThrowTerminatingError(errorRecord);
                        }
                    }

                    if (IsMixedModePsCimModule(remoteCimModule))
                    {
                        // warn that some commands have not been imported
                        string warningMessage = string.Format(
                            CultureInfo.InvariantCulture,
                            Modules.MixedModuleOverCimSessionWarning,
                            remoteCimModule.ModuleName);
                        this.WriteWarning(warningMessage);
                    }

                    //
                    // store the default session
                    //
                    Dbg.Assert(moduleInfo.ModuleType == ModuleType.Manifest, "Remote discovery should always produce a 'manifest' module");
                    Dbg.Assert(moduleInfo.NestedModules != null, "Remote discovery should always produce a 'manifest' module with nested modules entry");
                    Dbg.Assert(moduleInfo.NestedModules.Count > 0, "Remote discovery should always produce a 'manifest' module with some nested modules");
                    foreach (PSModuleInfo nestedModule in moduleInfo.NestedModules)
                    {
                        IDictionary cmdletsOverObjectsPrivateData;
                        bool cmdletsOverObjectsPrivateDataWasFound = PSPrimitiveDictionary.TryPathGet<IDictionary>(
                            nestedModule.PrivateData as IDictionary,
                            out cmdletsOverObjectsPrivateData,
                            ScriptWriter.PrivateDataKey_CmdletsOverObjects);
                        Dbg.Assert(cmdletsOverObjectsPrivateDataWasFound, "Cmdletization should always set the PrivateData properly");
                        cmdletsOverObjectsPrivateData[ScriptWriter.PrivateDataKey_DefaultSession] = cimSession;
                    }

                    //
                    // make sure the temporary folder gets removed when the module is removed
                    //
                    const string onRemoveScriptBody =
                        @"
                        Microsoft.PowerShell.Management\Remove-Item `
                            -LiteralPath $temporaryModulePath `
                            -Force `
                            -Recurse `
                            -ErrorAction SilentlyContinue

                        if ($null -ne $previousOnRemoveScript)
                        {
                            & $previousOnRemoveScript $args
                        }
                        ";
                    ScriptBlock onRemoveScriptBlock = this.Context.Engine.ParseScriptBlock(onRemoveScriptBody, false);
                    onRemoveScriptBlock = onRemoveScriptBlock.GetNewClosure();
                    // create a separate scope for variables set below
                    onRemoveScriptBlock.Module.SessionState.PSVariable.Set("temporaryModulePath", temporaryModuleDirectory);
                    onRemoveScriptBlock.Module.SessionState.PSVariable.Set("previousOnRemoveScript", moduleInfo.OnRemove);
                    moduleInfo.OnRemove = onRemoveScriptBlock;

                    //
                    // Some processing common for local and remote modules
                    //
                    AddModuleToModuleTables(
                        this.Context,
                        this.TargetSessionState.Internal,
                        moduleInfo);
                    if (BasePassThru)
                    {
                        WriteObject(moduleInfo);
                    }

                    return moduleInfo;
                }
                catch
                {
                    if (Directory.Exists(temporaryModuleDirectory))
                    {
                        Directory.Delete(temporaryModuleDirectory, recursive: true);
                    }

                    throw;
                }
                finally
                {
                    if (moduleInfo == null)
                    {
                        if (Directory.Exists(temporaryModuleDirectory))
                        {
                            Directory.Delete(temporaryModuleDirectory, recursive: true);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ErrorRecord errorRecord = RemoteDiscoveryHelper.GetErrorRecordForProcessingOfCimModule(e, remoteCimModule.ModuleName);
                this.WriteError(errorRecord);
                return null;
            }
        }

        #endregion CimSession parameterset

        #endregion Remote import

        #region Cancellation support

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private CancellationToken CancellationToken
        {
            get
            {
                return _cancellationTokenSource.Token;
            }
        }

        /// <summary>
        /// When overridden in the derived class, interrupts currently
        /// running code within the command. It should interrupt BeginProcessing,
        /// ProcessRecord, and EndProcessing.
        /// Default implementation in the base class just returns.
        /// </summary>
        protected override void StopProcessing()
        {
            _cancellationTokenSource.Cancel();
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Releases resources associated with this object.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases resources associated with this object.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _cancellationTokenSource.Dispose();
            }

            _disposed = true;
        }

        private bool _disposed;

        #endregion

        /// <summary>
        /// BeginProcessing override.
        /// </summary>
        protected override void BeginProcessing()
        {
            // Make sure that only one of (Global | Scope) is specified
            if (Global.IsSpecified && _isScopeSpecified)
            {
                InvalidOperationException ioe = new InvalidOperationException(Modules.GlobalAndScopeParameterCannotBeSpecifiedTogether);
                ErrorRecord er = new ErrorRecord(ioe, "Modules_GlobalAndScopeParameterCannotBeSpecifiedTogether",
                                                 ErrorCategory.InvalidOperation, null);
                ThrowTerminatingError(er);
            }

            if (!string.IsNullOrEmpty(Scope) && Scope.Equals(StringLiterals.Global, StringComparison.OrdinalIgnoreCase))
            {
                base.BaseGlobal = true;
            }
        }

        /// <summary>
        /// Load the specified modules...
        /// </summary>
        /// <remarks>
        /// Examples:
        ///     c:\temp\mdir\mdir.psm1  # load absolute path
        ///     ./mdir.psm1             # load relative path
        ///     c:\temp\mdir\mdir       # resolve by using extensions. mdir is a directory, mdir.xxx is a file.
        ///     c:\temp\mdir            # load default module if mdir is directory
        ///     module                  # $PSScriptRoot/module/module.psd1 (ps1,psm1,dll)
        ///     module/examplemodule.psm1      # $PSScriptRoot/module/module.psm1
        ///     module/examplemodule           # $PSScriptRoot/module/examplemodule.XXX if examplemodule is not a directory...
        ///     module/examplemodule           # $PSScriptRoot/module/examplemodule is a directory and $PSScriptRoot/module/examplemodule/examplemodule.XXX exists
        ///     module/examplemodule/examplemodule.XXX
        /// </remarks>
        protected override void ProcessRecord()
        {
            if (BaseMaximumVersion != null && BaseMinimumVersion != null && BaseMaximumVersion < BaseMinimumVersion)
            {
                string message = StringUtil.Format(Modules.MinimumVersionAndMaximumVersionInvalidRange, BaseMinimumVersion, BaseMaximumVersion);
                throw new PSArgumentOutOfRangeException(message);
            }

            ImportModuleOptions importModuleOptions = new ImportModuleOptions();
            importModuleOptions.NoClobber = NoClobber;
            if (!string.IsNullOrEmpty(Scope) && Scope.Equals(StringLiterals.Local, StringComparison.OrdinalIgnoreCase))
            {
                importModuleOptions.Local = true;
            }

            if (this.ParameterSetName.Equals(ParameterSet_ModuleInfo, StringComparison.OrdinalIgnoreCase))
            {
                // Process all of the specified PSModuleInfo objects. These would typically be coming in as a result
                // of doing Get-Module -list
                foreach (PSModuleInfo module in ModuleInfo)
                {
                    ApplicationInsightsTelemetry.SendModuleTelemetryMetric(TelemetryType.ModuleLoad, module);
                    RemoteDiscoveryHelper.DispatchModuleInfoProcessing(
                        module,
                        localAction: () =>
                        {
                            ImportModule_ViaLocalModuleInfo(importModuleOptions, module);
                            SetModuleBaseForEngineModules(module.Name, this.Context);
                        },

                        cimSessionAction: (cimSession, resourceUri, cimNamespace) => ImportModule_RemotelyViaCimSession(
                            importModuleOptions,
                            new string[] { module.Name },
                            cimSession,
                            resourceUri,
                            cimNamespace),

                        psSessionAction: psSession => ImportModule_RemotelyViaPsrpSession(
                            importModuleOptions,
                            new string[] { module.Path },
                            null,
                            psSession));
                }
            }
            else if (this.ParameterSetName.Equals(ParameterSet_Assembly, StringComparison.OrdinalIgnoreCase))
            {
                // Now load all of the supplied assemblies...
                foreach (Assembly suppliedAssembly in Assembly)
                {
                    // we don't know what the version of the module is.
                    ApplicationInsightsTelemetry.SendModuleTelemetryMetric(TelemetryType.ModuleLoad, suppliedAssembly.GetName().Name);
                    ImportModule_ViaAssembly(importModuleOptions, suppliedAssembly);
                }
            }
            else if (this.ParameterSetName.Equals(ParameterSet_Name, StringComparison.OrdinalIgnoreCase))
            {
                foreach (string name in Name)
                {
                    ImportModule_LocallyViaName_WithTelemetry(importModuleOptions, name);
                }
            }
            else if (this.ParameterSetName.Equals(ParameterSet_ViaPsrpSession, StringComparison.OrdinalIgnoreCase))
            {
                ImportModule_RemotelyViaPsrpSession(importModuleOptions, this.Name, null, this.PSSession);
            }
            else if (this.ParameterSetName.Equals(ParameterSet_ViaCimSession, StringComparison.OrdinalIgnoreCase))
            {
                ImportModule_RemotelyViaCimSession(importModuleOptions, this.Name, this.CimSession, this.CimResourceUri, this.CimNamespace);
            }
            else if (this.ParameterSetName.Equals(ParameterSet_FQName, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var modulespec in FullyQualifiedName)
                {
                    ImportModule_LocallyViaFQName(importModuleOptions, modulespec);
                }
            }
            else if (this.ParameterSetName.Equals(ParameterSet_FQName_ViaPsrpSession, StringComparison.OrdinalIgnoreCase))
            {
                ImportModule_RemotelyViaPsrpSession(importModuleOptions, null, FullyQualifiedName, this.PSSession);
                foreach (ModuleSpecification modulespec in FullyQualifiedName)
                {
                    ApplicationInsightsTelemetry.SendModuleTelemetryMetric(TelemetryType.ModuleLoad, modulespec.Name);
                }
            }
            else if (this.ParameterSetName.Equals(ParameterSet_ViaWinCompat, StringComparison.OrdinalIgnoreCase)
                  || this.ParameterSetName.Equals(ParameterSet_FQName_ViaWinCompat, StringComparison.OrdinalIgnoreCase))
            {
                if (this.UseWindowsPowerShell)
                {
                    ImportModulesUsingWinCompat(this.Name, this.FullyQualifiedName, importModuleOptions);
                }
            }
            else
            {
                Dbg.Assert(false, "Unrecognized parameter set");
            }
        }

        private bool IsModuleInDenyList(string[] moduleDenyList, string moduleName, ModuleSpecification moduleSpec)
        {
            Debug.Assert(string.IsNullOrEmpty(moduleName) ^ (moduleSpec == null), "Either moduleName or moduleSpec must be specified");

            // moduleName can be just a module name and it also can be a full path to psd1 from which we need to extract the module name
            string exactModuleName = ModuleIntrinsics.GetModuleName(moduleSpec == null ? moduleName : moduleSpec.Name);
            bool match = false;

            foreach (var deniedModuleName in moduleDenyList)
            {
                // use case-insensitive module name comparison
                match = exactModuleName.Equals(deniedModuleName, StringComparison.InvariantCultureIgnoreCase);
                if (match)
                {
                    string errorMessage = string.Format(CultureInfo.InvariantCulture, Modules.WinCompatModuleInDenyList, exactModuleName);
                    InvalidOperationException exception = new InvalidOperationException(errorMessage);
                    ErrorRecord er = new ErrorRecord(exception, "Modules_ModuleInWinCompatDenyList", ErrorCategory.ResourceUnavailable, exactModuleName);
                    WriteError(er);
                    break;
                }
            }

            return match;
        }

        private IEnumerable<T> FilterModuleCollection<T>(IEnumerable<T> moduleCollection)
        {
            if (moduleCollection is null)
            {
                return null;
            }

            // The ModuleDeny list is cached in PowerShellConfig object
            string[] moduleDenyList = PowerShellConfig.Instance.GetWindowsPowerShellCompatibilityModuleDenyList();
            if (moduleDenyList is null || moduleDenyList.Length == 0)
            {
                return moduleCollection;
            }

            var filteredModuleCollection = new List<T>();
            foreach (var module in moduleCollection)
            {
                if (!IsModuleInDenyList(moduleDenyList, module as string, module as ModuleSpecification))
                {
                    filteredModuleCollection.Add(module);
                }
            }

            return filteredModuleCollection;
        }

        private void PrepareNoClobberWinCompatModuleImport(string moduleName, ModuleSpecification moduleSpec, ref ImportModuleOptions importModuleOptions)
        {
            Debug.Assert(string.IsNullOrEmpty(moduleName) ^ (moduleSpec == null), "Either moduleName or moduleSpec must be specified");

            // moduleName can be just a module name and it also can be a full path to psd1 from which we need to extract the module name
            string moduleToLoad = ModuleIntrinsics.GetModuleName(moduleSpec is null ? moduleName : moduleSpec.Name);

            var isBuiltInModule = BuiltInModules.TryGetValue(moduleToLoad, out string normalizedName);
            if (isBuiltInModule)
            {
                moduleToLoad = normalizedName;
            }

            string[] noClobberModuleList = PowerShellConfig.Instance.GetWindowsPowerShellCompatibilityNoClobberModuleList();
            if (isBuiltInModule || noClobberModuleList?.Contains(moduleToLoad, StringComparer.OrdinalIgnoreCase) == true)
            {
                bool shouldLoadModuleLocally = true;
                if (isBuiltInModule)
                {
                    PSSnapInInfo loadedSnapin = Context.CurrentRunspace.InitialSessionState.GetPSSnapIn(moduleToLoad);
                    shouldLoadModuleLocally = loadedSnapin is null;

                    if (shouldLoadModuleLocally)
                    {
                        // If it is one of built-in modules, first try loading it from $PSHOME\Modules, otherwise rely on $env:PSModulePath.
                        string expectedCoreModulePath = Path.Combine(ModuleIntrinsics.GetPSHomeModulePath(), moduleToLoad);
                        if (Directory.Exists(expectedCoreModulePath))
                        {
                            moduleToLoad = expectedCoreModulePath;
                        }
                    }
                }

                if (shouldLoadModuleLocally)
                {
                    // Here we want to load a core-edition compatible version of the module, so the loading procedure will skip
                    // the 'System32' module path when searching. Also, we want to suppress writing out errors in case that a
                    // core-compatible version of the module cannot be found, because:
                    //  1. that's OK as long as it's not a PowerShell built-in module such as the 'Utility' moudle;
                    //  2. the error message will be confusing to the user.
                    bool savedValue = importModuleOptions.SkipSystem32ModulesAndSuppressError;
                    importModuleOptions.SkipSystem32ModulesAndSuppressError = true;

                    PSModuleInfo moduleInfo = moduleSpec is null
                        ? ImportModule_LocallyViaName_WithTelemetry(importModuleOptions, moduleToLoad)
                        : ImportModule_LocallyViaFQName(
                            importModuleOptions,
                            new ModuleSpecification()
                            {
                                Guid = moduleSpec.Guid,
                                MaximumVersion = moduleSpec.MaximumVersion,
                                Version = moduleSpec.Version,
                                RequiredVersion = moduleSpec.RequiredVersion,
                                Name = moduleToLoad
                            });

                    // If we failed to load a core-compatible version of a built-in module, we should stop trying to load the
                    // module in 'WinCompat' mode and report an error. This could happen when a user didn't correctly deploy
                    // the built-in modules, which would result in very confusing errors when the module auto-loading silently
                    // attempts to load those built-in modules in 'WinCompat' mode from the 'System32' module path.
                    //
                    // If the loading failed but it's NOT a built-in module, then it's fine to ignore this failure and continue
                    // to load the module in 'WinCompat' mode.
                    if (moduleInfo is null && isBuiltInModule)
                    {
                        throw new InvalidOperationException(
                            StringUtil.Format(
                                Modules.CannotFindCoreCompatibleBuiltInModule,
                                moduleToLoad));
                    }

                    importModuleOptions.SkipSystem32ModulesAndSuppressError = savedValue;
                }

                importModuleOptions.NoClobberExportPSSession = true;
            }
        }

        internal override IList<PSModuleInfo> ImportModulesUsingWinCompat(IEnumerable<string> moduleNames, IEnumerable<ModuleSpecification> moduleFullyQualifiedNames, ImportModuleOptions importModuleOptions)
        {
            IList<PSModuleInfo> moduleProxyList = new List<PSModuleInfo>();
#if !UNIX
            // one of the two parameters can be passed: either ModuleNames (most of the time) or ModuleSpecifications (they are used in different parameter sets)
            IEnumerable<string> filteredModuleNames = FilterModuleCollection(moduleNames);
            IEnumerable<ModuleSpecification> filteredModuleFullyQualifiedNames = FilterModuleCollection(moduleFullyQualifiedNames);

            // do not setup WinCompat resources if we have no modules to import
            if (filteredModuleNames?.Any() != true && filteredModuleFullyQualifiedNames?.Any() != true)
            {
                return moduleProxyList;
            }

            // perform necessary preparations if module has to be imported with NoClobber mode
            if (filteredModuleNames != null)
            {
                foreach (string moduleName in filteredModuleNames)
                {
                    PrepareNoClobberWinCompatModuleImport(moduleName, null, ref importModuleOptions);
                }
            }

            if (filteredModuleFullyQualifiedNames != null)
            {
                foreach (var moduleSpec in filteredModuleFullyQualifiedNames)
                {
                    PrepareNoClobberWinCompatModuleImport(null, moduleSpec, ref importModuleOptions);
                }
            }

            var winPSVersionString = Utils.GetWindowsPowerShellVersionFromRegistry();
            if (!winPSVersionString.StartsWith("5.1", StringComparison.OrdinalIgnoreCase))
            {
                string errorMessage = string.Format(CultureInfo.InvariantCulture, Modules.WinCompatRequredVersionError, winPSVersionString);
                throw new InvalidOperationException(errorMessage);
            }

            PSSession WindowsPowerShellCompatRemotingSession = CreateWindowsPowerShellCompatResources();
            if (WindowsPowerShellCompatRemotingSession == null)
            {
                return new List<PSModuleInfo>();
            }

            // perform the module import / proxy generation
            moduleProxyList = ImportModule_RemotelyViaPsrpSession(importModuleOptions, filteredModuleNames, filteredModuleFullyQualifiedNames, WindowsPowerShellCompatRemotingSession, usingWinCompat: true);

            foreach (PSModuleInfo moduleProxy in moduleProxyList)
            {
                moduleProxy.IsWindowsPowerShellCompatModule = true;
                Interlocked.Increment(ref s_WindowsPowerShellCompatUsageCounter);

                string message = StringUtil.Format(Modules.WinCompatModuleWarning, moduleProxy.Name, WindowsPowerShellCompatRemotingSession.Name);
                WriteWarning(message);
            }

            // register LocationChanged handler so that $PWD in Windows PS process mirrors local $PWD changes
            if (moduleProxyList.Count > 0)
            {
                // make sure that we add registration only once to a multicast delegate
                SyncCurrentLocationDelegate ??= SyncCurrentLocationHandler;
                var alreadyregistered = this.SessionState.InvokeCommand.LocationChangedAction?.GetInvocationList().Contains(SyncCurrentLocationDelegate);

                if (!alreadyregistered ?? true)
                {
                    this.SessionState.InvokeCommand.LocationChangedAction += SyncCurrentLocationDelegate;

                    // first sync has to be triggered manually
                    SyncCurrentLocationHandler(sender: this, args: new LocationChangedEventArgs(sessionState: null, oldPath: null, newPath: this.SessionState.Path.CurrentLocation));
                }
            }
#endif
            return moduleProxyList;
        }

        private static void SetModuleBaseForEngineModules(string moduleName, System.Management.Automation.ExecutionContext context)
        {
            // Set modulebase of engine modules to point to $pshome
            // This is so that Get-Help can load the correct help.
            if (InitialSessionState.IsEngineModule(moduleName))
            {
                foreach (var m in context.EngineSessionState.ModuleTable.Values)
                {
                    if (m.Name.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                    {
                        m.SetModuleBase(Utils.DefaultPowerShellAppBase);
                        // Also set  ModuleBase for nested modules of Engine modules
                        foreach (var nestedModule in m.NestedModules)
                        {
                            nestedModule.SetModuleBase(Utils.DefaultPowerShellAppBase);
                        }
                    }
                }

                foreach (var m in context.Modules.ModuleTable.Values)
                {
                    if (m.Name.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                    {
                        m.SetModuleBase(Utils.DefaultPowerShellAppBase);
                        // Also set  ModuleBase for nested modules of Engine modules
                        foreach (var nestedModule in m.NestedModules)
                        {
                            nestedModule.SetModuleBase(Utils.DefaultPowerShellAppBase);
                        }
                    }
                }
            }
        }
    }
}
