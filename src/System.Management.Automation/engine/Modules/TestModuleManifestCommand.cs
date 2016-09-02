/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Collections;

//
// Now define the set of commands for manipulating modules.
//

namespace Microsoft.PowerShell.Commands
{
    #region Test-ModuleManifest
    /// <summary>
    /// This cmdlet takes a module manifest and validates the contents...
    /// </summary>
    [Cmdlet("Test", "ModuleManifest", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=141557")]
    [OutputType(typeof(PSModuleInfo))]
    public sealed class TestModuleManifestCommand : ModuleCmdletBase
    {
        /// <summary>
        /// The output path for the generated file...
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }
        private string _path;

        /// <summary>
        /// Implements the record processing for this cmdlet
        /// </summary>
        protected override void ProcessRecord()
        {
            ProviderInfo provider = null;
            Collection<string> filePaths;

            try
            {
                if (Context.EngineSessionState.IsProviderLoaded(Context.ProviderNames.FileSystem))
                {
                    filePaths =
                        SessionState.Path.GetResolvedProviderPathFromPSPath(_path, out provider);
                }
                else
                {
                    filePaths = new Collection<string>();
                    filePaths.Add(_path);
                }
            }
            catch (ItemNotFoundException)
            {
                string message = StringUtil.Format(Modules.ModuleNotFound, _path);
                FileNotFoundException fnf = new FileNotFoundException(message);
                ErrorRecord er = new ErrorRecord(fnf, "Modules_ModuleNotFound",
                    ErrorCategory.ResourceUnavailable, _path);
                WriteError(er);
                return;
            }

            // Make sure that the path is in the file system - that's all we can handle currently...
            if (!provider.NameEquals(this.Context.ProviderNames.FileSystem))
            {
                // "The current provider ({0}) cannot open a file"
                throw InterpreterError.NewInterpreterException(_path, typeof(RuntimeException),
                    null, "FileOpenError", ParserStrings.FileOpenError, provider.FullName);
            }

            // Make sure at least one file was found...
            if (filePaths == null || filePaths.Count < 1)
            {
                string message = StringUtil.Format(Modules.ModuleNotFound, _path);
                FileNotFoundException fnf = new FileNotFoundException(message);
                ErrorRecord er = new ErrorRecord(fnf, "Modules_ModuleNotFound",
                    ErrorCategory.ResourceUnavailable, _path);
                WriteError(er);
                return;
            }

            if (filePaths.Count > 1)
            {
                // "The path resolved to more than one file; can only process one file at a time."
                throw InterpreterError.NewInterpreterException(filePaths, typeof(RuntimeException),
                    null, "AmbiguousPath", ParserStrings.AmbiguousPath);
            }

            string filePath = filePaths[0];
            ExternalScriptInfo scriptInfo = null;
            string ext = System.IO.Path.GetExtension(filePath);
            if (ext.Equals(StringLiterals.PowerShellDataFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                // Create a script info for loading the file...
                string scriptName;
                scriptInfo = GetScriptInfoForFile(filePath, out scriptName, false);

                // we should reserve the Context.ModuleBeingProcessed unchanged after loadModuleManifest(), otherwise the module won't be importable next time.
                PSModuleInfo module;
                string _origModuleBeingProcessed = Context.ModuleBeingProcessed;
                try
                {
                    module = LoadModuleManifest(
                    scriptInfo,
                    ManifestProcessingFlags.WriteErrors | ManifestProcessingFlags.WriteWarnings /* but don't stop on first error and don't load elements */,
                    null,
                    null,
                    null,
                    null);

                    if (module != null)
                    {
                        //Validate file existence
                        if (module.RequiredAssemblies != null)
                        {
                            foreach (string requiredAssembliespath in module.RequiredAssemblies)
                            {
                                if (!IsValidFilePath(requiredAssembliespath, module, true) && !IsValidGacAssembly(requiredAssembliespath))
                                {
                                    string errorMsg = StringUtil.Format(Modules.InvalidRequiredAssembliesInModuleManifest, requiredAssembliespath, filePath);
                                    var errorRecord = new ErrorRecord(new DirectoryNotFoundException(errorMsg), "Modules_InvalidRequiredAssembliesInModuleManifest",
                                            ErrorCategory.ObjectNotFound, _path);
                                    WriteError(errorRecord);
                                }
                            }
                        }

                        Hashtable data = null;
                        Hashtable localizedData = null;
                        bool containerErrors = false;
                        LoadModuleManifestData(scriptInfo, ManifestProcessingFlags.WriteErrors | ManifestProcessingFlags.WriteWarnings, out data, out localizedData, ref containerErrors);
                        ModuleSpecification[] nestedModules;
                        GetScalarFromData(data, scriptInfo.Path, "NestedModules", ManifestProcessingFlags.WriteErrors | ManifestProcessingFlags.WriteWarnings, out nestedModules);
                        if (nestedModules != null)
                        {
                            foreach (ModuleSpecification nestedModule in nestedModules)
                            {
                                if (!IsValidFilePath(nestedModule.Name, module, true)
                                    && !IsValidFilePath(nestedModule.Name + StringLiterals.DependentWorkflowAssemblyExtension, module, true)
                                    && !IsValidFilePath(nestedModule.Name + StringLiterals.PowerShellNgenAssemblyExtension, module, true)
                                    && !IsValidFilePath(nestedModule.Name + StringLiterals.PowerShellModuleFileExtension, module, true)
                                    && !IsValidGacAssembly(nestedModule.Name))
                                {
                                    // The nested module could be dependencies. We compare if it can be loaded by loadmanifest
                                    bool isDependency = false;
                                    foreach (PSModuleInfo loadedNestedModule in module.NestedModules)
                                    {
                                        if (string.Equals(loadedNestedModule.Name, nestedModule.Name, StringComparison.OrdinalIgnoreCase))
                                        {
                                            isDependency = true;
                                            break;
                                        }
                                    }

                                    if (!isDependency)
                                    {
                                        string errorMsg = StringUtil.Format(Modules.InvalidNestedModuleinModuleManifest, nestedModule.Name, filePath);
                                        var errorRecord = new ErrorRecord(new DirectoryNotFoundException(errorMsg), "Modules_InvalidNestedModuleinModuleManifest",
                                                ErrorCategory.ObjectNotFound, _path);
                                        WriteError(errorRecord);
                                    }
                                }
                            }
                        }

                        ModuleSpecification[] requiredModules;
                        GetScalarFromData(data, scriptInfo.Path, "RequiredModules", ManifestProcessingFlags.WriteErrors | ManifestProcessingFlags.WriteWarnings, out requiredModules);
                        if (requiredModules != null)
                        {
                            foreach (ModuleSpecification requiredModule in requiredModules)
                            {
                                var modules = GetModule(new[] { requiredModule.Name }, false, true);
                                if (modules.Count == 0)
                                {
                                    string errorMsg = StringUtil.Format(Modules.InvalidRequiredModulesinModuleManifest, requiredModule.Name, filePath);
                                    var errorRecord = new ErrorRecord(new DirectoryNotFoundException(errorMsg), "Modules_InvalidRequiredModulesinModuleManifest",
                                            ErrorCategory.ObjectNotFound, _path);
                                    WriteError(errorRecord);
                                }
                            }
                        }

                        string[] fileListPaths;
                        GetScalarFromData(data, scriptInfo.Path, "FileList", ManifestProcessingFlags.WriteErrors | ManifestProcessingFlags.WriteWarnings, out fileListPaths);
                        if (fileListPaths != null)
                        {
                            foreach (string fileListPath in fileListPaths)
                            {
                                if (!IsValidFilePath(fileListPath, module, true))
                                {
                                    string errorMsg = StringUtil.Format(Modules.InvalidFilePathinModuleManifest, fileListPath, filePath);
                                    var errorRecord = new ErrorRecord(new DirectoryNotFoundException(errorMsg), "Modules_InvalidFilePathinModuleManifest",
                                            ErrorCategory.ObjectNotFound, _path);
                                    WriteError(errorRecord);
                                }
                            }
                        }

                        ModuleSpecification[] moduleListModules;
                        GetScalarFromData(data, scriptInfo.Path, "ModuleList", ManifestProcessingFlags.WriteErrors | ManifestProcessingFlags.WriteWarnings, out moduleListModules);
                        if (moduleListModules != null)
                        {
                            foreach (ModuleSpecification moduleListModule in moduleListModules)
                            {
                                var modules = GetModule(new[] { moduleListModule.Name }, true, true);
                                if (modules.Count == 0)
                                {
                                    string errorMsg = StringUtil.Format(Modules.InvalidModuleListinModuleManifest, moduleListModule.Name, filePath);
                                    var errorRecord = new ErrorRecord(new DirectoryNotFoundException(errorMsg), "Modules_InvalidModuleListinModuleManifest",
                                            ErrorCategory.ObjectNotFound, _path);
                                    WriteError(errorRecord);
                                }
                            }
                        }

                        if (module.CompatiblePSEditions.Any())
                        {
                            // The CompatiblePSEditions module manifest key is supported only on PowerShell version '5.1' or higher.
                            // Ensure that PowerShellVersion module manifest key value is '5.1' or higher.
                            //
                            var minimumRequiredPowerShellVersion = new Version(5, 1);
                            if ((module.PowerShellVersion == null) || module.PowerShellVersion < minimumRequiredPowerShellVersion)
                            {
                                string errorMsg = StringUtil.Format(Modules.InvalidPowerShellVersionInModuleManifest, filePath);
                                var errorRecord = new ErrorRecord(new ArgumentException(errorMsg), "Modules_InvalidPowerShellVersionInModuleManifest", ErrorCategory.InvalidArgument, _path);
                                WriteError(errorRecord);
                            }
                        }
                    }
                }
                finally
                {
                    Context.ModuleBeingProcessed = _origModuleBeingProcessed;
                }
                DirectoryInfo parent = null;
                try
                {
                    parent = Directory.GetParent(filePath);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                catch (ArgumentException) { }

                Version version;
                if (parent != null && Version.TryParse(parent.Name, out version))
                {
                    if (!version.Equals(module.Version))
                    {
                        string message = StringUtil.Format(Modules.InvalidModuleManifestVersion, filePath, module.Version.ToString(), parent.FullName);
                        var ioe = new InvalidOperationException(message);
                        ErrorRecord er = new ErrorRecord(ioe, "Modules_InvalidModuleManifestVersion",
                            ErrorCategory.InvalidArgument, _path);
                        ThrowTerminatingError(er);
                    }

                    WriteVerbose(Modules.ModuleVersionEqualsToVersionFolder);
                }

                if (module != null)
                {
                    WriteObject(module);
                }
            }
            else
            {
                string message = StringUtil.Format(Modules.InvalidModuleManifestPath, filePath);
                InvalidOperationException ioe = new InvalidOperationException(message);
                ErrorRecord er = new ErrorRecord(ioe, "Modules_InvalidModuleManifestPath",
                    ErrorCategory.InvalidArgument, _path);
                ThrowTerminatingError(er);
            }
        }

        /// <summary>
        /// Check if the given path is valid.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="module"></param>
        /// <param name="verifyPathScope"></param>
        /// <returns></returns>
        private bool IsValidFilePath(string path, PSModuleInfo module, bool verifyPathScope)
        {
            try
            {
                if (!System.IO.Path.IsPathRooted(path))
                {
                    // we assume the relative path is under module scope, otherwise we will throw error anyway.
                    path = System.IO.Path.GetFullPath(module.ModuleBase + "\\" + path);
                }

                // First, we validate if the path  does exist.
                if (!File.Exists(path) && !Directory.Exists(path))
                {
                    return false;
                }

                //Then, we validate if the path is under module scope
                if (verifyPathScope && !System.IO.Path.GetFullPath(path).StartsWith(System.IO.Path.GetFullPath(module.ModuleBase), StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            catch (Exception exception)
            {
                if (exception is ArgumentException || exception is ArgumentNullException || exception is NotSupportedException || exception is PathTooLongException)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Check if the given string is a valid gac assembly.
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <returns></returns>
        private bool IsValidGacAssembly(string assemblyName)
        {
            string gacPath = System.Environment.GetEnvironmentVariable("windir") + "\\Microsoft.NET\\assembly";
            string assemblyFile = assemblyName;
            string ngenAssemblyFile = assemblyName;
            if (!assemblyName.EndsWith(StringLiterals.DependentWorkflowAssemblyExtension, StringComparison.OrdinalIgnoreCase))
            {
                assemblyFile = assemblyName + StringLiterals.DependentWorkflowAssemblyExtension;
                ngenAssemblyFile = assemblyName + StringLiterals.PowerShellNgenAssemblyExtension;
            }
            try
            {
                var allFiles = Directory.GetFiles(gacPath, assemblyFile, SearchOption.AllDirectories);

                if (allFiles.Length == 0)
                {
                    var allNgenFiles = Directory.GetFiles(gacPath, ngenAssemblyFile, SearchOption.AllDirectories);
                    if (allNgenFiles.Length == 0)
                    {
                        return false;
                    }
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
    }

    #endregion
} // Microsoft.PowerShell.Commands
