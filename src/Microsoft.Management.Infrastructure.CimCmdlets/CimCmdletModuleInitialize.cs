// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#region Using directives

using System;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    /// <summary>
    /// <para>
    /// Initialize the cimcmdlets.
    /// </para>
    /// </summary>
    /// <summary>
    /// Provide a hook to the engine for startup initialization
    /// w.r.t compiled assembly loading.
    /// </summary>
    public sealed class CimCmdletsAssemblyInitializer : IModuleAssemblyInitializer
    {
        /// <summary>
        /// <para>
        /// constructor
        /// </para>
        /// </summary>
        public CimCmdletsAssemblyInitializer()
        {
        }

        /// <summary>
        /// PowerShell engine will call this method when the cimcmdlets module
        /// is loaded.
        /// </summary>
        public void OnImport()
        {
            DebugHelper.WriteLogEx();
            using (System.Management.Automation.PowerShell invoker = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace))
            {
                foreach (CimCmdletAliasEntry alias in Aliases)
                {
                    invoker.AddScript(string.Format(CultureInfo.CurrentUICulture, "Set-Alias -Name {0} -Value {1} -Option {2} -ErrorAction SilentlyContinue", alias.Name, alias.Value, alias.Options));
                    DebugHelper.WriteLog(@"Add commands {0} of {1} with option {2} to current runspace.", 1, alias.Name, alias.Value, alias.Options);
                }

                System.Collections.ObjectModel.Collection<PSObject> psObjects = invoker.Invoke();
                DebugHelper.WriteLog(@"Invoke results {0}.", 1, psObjects.Count);
            }
        }

        #region readonly string

        /// <summary>
        /// <para>
        /// CimCmdlet alias entry
        /// </para>
        /// </summary>
        internal sealed class CimCmdletAliasEntry
        {
            /// <summary>
            /// <para>
            /// Constructor
            /// </para>
            /// </summary>
            /// <param name="name"></param>
            /// <param name="value"></param>
            internal CimCmdletAliasEntry(string name, string value)
            {
                this._name = name;
                this._value = value;
            }

            /// <summary>
            /// The string defining the name of this alias.
            /// </summary>
            internal string Name { get { return this._name; } }

            private string _name;

            /// <summary>
            /// The string defining real cmdlet name.
            /// </summary>
            internal string Value { get { return this._value; } }

            private string _value = string.Empty;

            /// <summary>
            /// The string defining real cmdlet name.
            /// </summary>
            internal ScopedItemOptions Options { get { return this._options; } }

            private ScopedItemOptions _options = ScopedItemOptions.AllScope | ScopedItemOptions.ReadOnly;
        }

        /// <summary>
        /// Returns a new array of alias entries everytime it's called.
        /// </summary>
        internal static CimCmdletAliasEntry[] Aliases = new CimCmdletAliasEntry[] {
                    new CimCmdletAliasEntry("gcim", "Get-CimInstance"),
                    new CimCmdletAliasEntry("scim", "Set-CimInstance"),
                    new CimCmdletAliasEntry("ncim", "New-CimInstance "),
                    new CimCmdletAliasEntry("rcim", "Remove-cimInstance"),
                    new CimCmdletAliasEntry("icim", "Invoke-CimMethod"),
                    new CimCmdletAliasEntry("gcai", "Get-CimAssociatedInstance"),
                    new CimCmdletAliasEntry("rcie", "Register-CimIndicationEvent"),
                    new CimCmdletAliasEntry("ncms", "New-CimSession"),
                    new CimCmdletAliasEntry("rcms", "Remove-cimSession"),
                    new CimCmdletAliasEntry("gcms", "Get-CimSession"),
                    new CimCmdletAliasEntry("ncso", "New-CimSessionOption"),
                    new CimCmdletAliasEntry("gcls", "Get-CimClass"),
        };
        #endregion
    }
}
