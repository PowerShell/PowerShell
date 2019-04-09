// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Reflection;
using Dbg = System.Management.Automation.Diagnostics;

//
// Now define the set of commands for manipulating modules.
//

namespace Microsoft.PowerShell.Commands
{
    #region Remove-Module
    /// <summary>
    /// Implements a cmdlet that gets the list of loaded modules...
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "Module", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=141556")]
    public sealed class RemoveModuleCommand : ModuleCmdletBase
    {
        /// <summary>
        /// This parameter specifies the current pipeline object.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "name", ValueFromPipeline = true, Position = 0)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        public string[] Name
        {
            set { _name = value; }

            get { return _name; }
        }

        private string[] _name = Array.Empty<string>();

        /// <summary>
        /// This parameter specifies the current pipeline object.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "FullyQualifiedName", ValueFromPipeline = true, Position = 0)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        public ModuleSpecification[] FullyQualifiedName { get; set; }

        /// <summary>
        /// This parameter specifies the current pipeline object.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ModuleInfo", ValueFromPipeline = true, Position = 0)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        public PSModuleInfo[] ModuleInfo
        {
            set { _moduleInfo = value; }

            get { return _moduleInfo; }
        }

        private PSModuleInfo[] _moduleInfo = Array.Empty<PSModuleInfo>();

        /// <summary>
        /// If provided, this parameter will allow readonly modules to be removed.
        /// </summary>
        [Parameter]
        public SwitchParameter Force
        {
            get { return BaseForce; }

            set { BaseForce = value; }
        }

        private int _numberRemoved = 0;  // Maintains a count of the number of modules removed...

        /// <summary>
        /// Remove the specified modules. Modules can be specified either through a ModuleInfo or a name.
        /// </summary>
        protected override void ProcessRecord()
        {
            // This dictionary has the list of modules to be removed.
            // Key - Module specified as a parameter to Remove-Module
            // Values - List of all modules that need to be removed for this key (includes all nested modules of this module)
            Dictionary<PSModuleInfo, List<PSModuleInfo>> modulesToRemove = new Dictionary<PSModuleInfo, List<PSModuleInfo>>();

            foreach (var m in Context.Modules.GetModules(_name, false))
            {
                modulesToRemove.Add(m, new List<PSModuleInfo> { m });
            }

            if (FullyQualifiedName != null)
            {
                // TODO:
                // Paths in the module name may fail here because
                // they the wrong directory separator or are relative.
                // Fix with the code below:
                // FullyQualifiedName = FullyQualifiedName.Select(ms => ms.WithNormalizedName(Context, SessionState.Path.CurrentLocation.Path)).ToArray();
                foreach (var m in Context.Modules.GetModules(FullyQualifiedName, false))
                {
                    modulesToRemove.Add(m, new List<PSModuleInfo> { m });
                }
            }

            foreach (var m in _moduleInfo)
            {
                modulesToRemove.Add(m, new List<PSModuleInfo> { m });
            }

            // Add any of the child modules of a manifests to the list of modules to remove...
            Dictionary<PSModuleInfo, List<PSModuleInfo>> nestedModules = new Dictionary<PSModuleInfo, List<PSModuleInfo>>();
            foreach (var entry in modulesToRemove)
            {
                var module = entry.Key;
                if (module.NestedModules != null && module.NestedModules.Count > 0)
                {
                    List<PSModuleInfo> nestedModulesWithNoCircularReference = new List<PSModuleInfo>();
                    GetAllNestedModules(module, ref nestedModulesWithNoCircularReference);
                    nestedModules.Add(module, nestedModulesWithNoCircularReference);
                }
            }

            // dont add duplicates to our original modulesToRemove list..so that the
            // evaluation loop below will not duplicate in case of WriteError and WriteWarning.
            // A global list of modules to be removed is maintained for this purpose
            HashSet<PSModuleInfo> globalListOfModules = new HashSet<PSModuleInfo>(new PSModuleInfoComparer());

            if (nestedModules.Count > 0)
            {
                foreach (var entry in nestedModules)
                {
                    List<PSModuleInfo> values = null;
                    if (modulesToRemove.TryGetValue(entry.Key, out values))
                    {
                        foreach (var module in entry.Value)
                        {
                            if (!globalListOfModules.Contains(module))
                            {
                                values.Add(module);
                                globalListOfModules.Add(module);
                            }
                        }
                    }
                }
            }

            // Check the list of modules to remove and exclude those that cannot or should not be removed
            Dictionary<PSModuleInfo, List<PSModuleInfo>> actualModulesToRemove = new Dictionary<PSModuleInfo, List<PSModuleInfo>>();

            // We want to remove the modules starting from the nested modules
            // If we start from the parent module, the nested modules do not get removed and are left orphaned in the parent modules's sessionstate.
            foreach (var entry in modulesToRemove)
            {
                List<PSModuleInfo> moduleList = new List<PSModuleInfo>();
                for (int i = entry.Value.Count - 1; i >= 0; i--)
                {
                    PSModuleInfo module = entry.Value[i];
                    // See if the module is constant...
                    if (module.AccessMode == ModuleAccessMode.Constant)
                    {
                        string message = StringUtil.Format(Modules.ModuleIsConstant, module.Name);
                        InvalidOperationException moduleNotRemoved = new InvalidOperationException(message);
                        ErrorRecord er = new ErrorRecord(moduleNotRemoved, "Modules_ModuleIsConstant",
                                                         ErrorCategory.PermissionDenied, module);
                        WriteError(er);
                        continue;
                    }

                    // See if the module is readonly...
                    if (module.AccessMode == ModuleAccessMode.ReadOnly && !BaseForce)
                    {
                        string message = StringUtil.Format(Modules.ModuleIsReadOnly, module.Name);

                        if (InitialSessionState.IsConstantEngineModule(module.Name))
                        {
                            WriteWarning(message);
                        }
                        else
                        {
                            InvalidOperationException moduleNotRemoved = new InvalidOperationException(message);
                            ErrorRecord er = new ErrorRecord(moduleNotRemoved, "Modules_ModuleIsReadOnly",
                                                             ErrorCategory.PermissionDenied, module);
                            WriteError(er);
                        }

                        continue;
                    }

                    if (!ShouldProcess(StringUtil.Format(Modules.ConfirmRemoveModule, module.Name, module.Path)))
                    {
                        continue;
                    }

                    // If this module provides the current session drive, then we cannot remove it.
                    // Abort this command since we don't want to do a partial removal of a module manifest.
                    if (ModuleProvidesCurrentSessionDrive(module))
                    {
                        if (InitialSessionState.IsEngineModule(module.Name))
                        {
                            if (!BaseForce)
                            {
                                string message = StringUtil.Format(Modules.CoreModuleCannotBeRemoved, module.Name);
                                this.WriteWarning(message);
                            }

                            continue;
                        }
                        // Specify the overall module name if there is only one.
                        // Otherwise specify the particular module name.
                        string moduleName = (_name.Length == 1) ? _name[0] : module.Name;

                        PSInvalidOperationException invalidOperation =
                            PSTraceSource.NewInvalidOperationException(
                                Modules.ModuleDriveInUse,
                                moduleName);

                        throw (invalidOperation);
                    }

                    // Add module to remove list.
                    moduleList.Add(module);
                }

                actualModulesToRemove[entry.Key] = moduleList;
            }

            // Now remove the modules, first checking the RequiredModules dependencies
            Dictionary<PSModuleInfo, List<PSModuleInfo>> requiredDependencies = GetRequiredDependencies();

            foreach (var entry in actualModulesToRemove)
            {
                foreach (var module in entry.Value)
                {
                    if (!BaseForce)
                    {
                        List<PSModuleInfo> requiredBy = null;

                        if (requiredDependencies.TryGetValue(module, out requiredBy))
                        {
                            for (int i = requiredBy.Count - 1; i >= 0; i--)
                            {
                                if (actualModulesToRemove.ContainsKey(requiredBy[i]))
                                {
                                    requiredBy.RemoveAt(i);
                                }
                            }

                            if (requiredBy.Count > 0)
                            {
                                string message = StringUtil.Format(Modules.ModuleIsRequired, module.Name, requiredBy[0].Name);
                                InvalidOperationException moduleNotRemoved = new InvalidOperationException(message);
                                ErrorRecord er = new ErrorRecord(moduleNotRemoved, "Modules_ModuleIsRequired",
                                                                 ErrorCategory.PermissionDenied, module);
                                WriteError(er);
                                continue;
                            }
                        }
                    }

                    _numberRemoved++;

                    this.RemoveModule(module, entry.Key.Name);
                }
            }
        }

        private bool ModuleProvidesCurrentSessionDrive(PSModuleInfo module)
        {
            if (module.ModuleType == ModuleType.Binary)
            {
                Dictionary<string, List<ProviderInfo>> providers = Context.TopLevelSessionState.Providers;
                foreach (KeyValuePair<string, List<ProviderInfo>> pList in providers)
                {
                    Dbg.Assert(pList.Value != null, "There should never be a null list of entries in the provider table");
                    foreach (ProviderInfo pInfo in pList.Value)
                    {
                        string implTypeAssemblyLocation = pInfo.ImplementingType.Assembly.Location;
                        if (implTypeAssemblyLocation.Equals(module.Path, StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (PSDriveInfo dInfo in Context.TopLevelSessionState.GetDrivesForProvider(pInfo.FullName))
                            {
                                if (dInfo == SessionState.Drive.Current)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        private void GetAllNestedModules(PSModuleInfo module, ref List<PSModuleInfo> nestedModulesWithNoCircularReference)
        {
            List<PSModuleInfo> nestedModules = new List<PSModuleInfo>();
            if (module.NestedModules != null && module.NestedModules.Count > 0)
            {
                foreach (var nestedModule in module.NestedModules)
                {
                    if (!nestedModulesWithNoCircularReference.Contains(nestedModule))
                    {
                        nestedModulesWithNoCircularReference.Add(nestedModule);
                        nestedModules.Add(nestedModule);
                    }
                }

                foreach (PSModuleInfo child in nestedModules)
                {
                    GetAllNestedModules(child, ref nestedModulesWithNoCircularReference);
                }
            }
        }

        /// <summary>
        /// Returns a map from a module to the list of modules that require it.
        /// </summary>
        private Dictionary<PSModuleInfo, List<PSModuleInfo>> GetRequiredDependencies()
        {
            Dictionary<PSModuleInfo, List<PSModuleInfo>> requiredDependencies = new Dictionary<PSModuleInfo, List<PSModuleInfo>>();

            foreach (PSModuleInfo module in Context.Modules.GetModules(new string[] { "*" }, false))
            {
                foreach (PSModuleInfo requiredModule in module.RequiredModules)
                {
                    List<PSModuleInfo> requiredByList = null;

                    if (!requiredDependencies.TryGetValue(requiredModule, out requiredByList))
                    {
                        requiredDependencies.Add(requiredModule, requiredByList = new List<PSModuleInfo>());
                    }

                    requiredByList.Add(module);
                }
            }

            return requiredDependencies;
        }

        /// <summary>
        /// Reports an error if no modules were removed...
        /// </summary>
        protected override void EndProcessing()
        {
            // Write an error record if specific modules were to be removed.
            // By specific, we mean either a name sting with no wildcards or
            // or a PSModuleInfo object. If the removal request only includes patterns
            // then we won't write the error.

            if (_numberRemoved == 0 && !MyInvocation.BoundParameters.ContainsKey("WhatIf"))
            {
                bool hasWildcards = true;
                bool isEngineModule = true;
                foreach (string n in _name)
                {
                    if (!InitialSessionState.IsEngineModule(n))
                    {
                        isEngineModule = false;
                    }

                    if (!WildcardPattern.ContainsWildcardCharacters(n))
                        hasWildcards = false;
                }

                if (FullyQualifiedName != null && (FullyQualifiedName.Any(moduleSpec => !InitialSessionState.IsEngineModule(moduleSpec.Name))))
                {
                    isEngineModule = false;
                }

                if (!isEngineModule && (!hasWildcards || _moduleInfo.Length != 0 || (FullyQualifiedName != null && FullyQualifiedName.Length != 0)))
                {
                    string message = StringUtil.Format(Modules.NoModulesRemoved);
                    InvalidOperationException invalidOp = new InvalidOperationException(message);
                    ErrorRecord er = new ErrorRecord(invalidOp, "Modules_NoModulesRemoved",
                        ErrorCategory.ResourceUnavailable, null);
                    WriteError(er);
                }
            }
        }
    }
    #endregion Remove-Module
}
