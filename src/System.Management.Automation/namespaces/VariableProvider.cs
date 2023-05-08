// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Provider;

using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This provider is the data accessor for shell variables. It uses
    /// the HashtableProvider as the base class to get a hashtable as
    /// a data store.
    /// </summary>
    [CmdletProvider(VariableProvider.ProviderName, ProviderCapabilities.ShouldProcess)]
    [OutputType(typeof(PSVariable), ProviderCmdlet = ProviderCmdlet.SetItem)]
    [OutputType(typeof(PSVariable), ProviderCmdlet = ProviderCmdlet.RenameItem)]
    [OutputType(typeof(PSVariable), ProviderCmdlet = ProviderCmdlet.CopyItem)]
    [OutputType(typeof(PSVariable), ProviderCmdlet = ProviderCmdlet.GetItem)]
    [OutputType(typeof(PSVariable), ProviderCmdlet = ProviderCmdlet.NewItem)]
    public sealed class VariableProvider : SessionStateProviderBase
    {
        /// <summary>
        /// Gets the name of the provider.
        /// </summary>
        public const string ProviderName = "Variable";

        #region Constructor

        /// <summary>
        /// The constructor for the provider that exposes variables to the user
        /// as drives.
        /// </summary>
        public VariableProvider()
        {
        }

        #endregion Constructor

        #region DriveCmdletProvider overrides

        /// <summary>
        /// Initializes the variables drive.
        /// </summary>
        /// <returns>
        /// An array of a single PSDriveInfo object representing the variables drive.
        /// </returns>
        protected override Collection<PSDriveInfo> InitializeDefaultDrives()
        {
            string description = SessionStateStrings.VariableDriveDescription;

            PSDriveInfo variableDrive =
                new PSDriveInfo(
                    DriveNames.VariableDrive,
                    ProviderInfo,
                    string.Empty,
                    description,
                    null);

            Collection<PSDriveInfo> drives = new Collection<PSDriveInfo>();
            drives.Add(variableDrive);
            return drives;
        }

        #endregion DriveCmdletProvider overrides

        #region protected members

        /// <summary>
        /// Gets a variable from session state.
        /// </summary>
        /// <param name="name">
        /// The name of the variable to retrieve.
        /// </param>
        /// <returns>
        /// A PSVariable that represents the variable.
        /// </returns>
        internal override object GetSessionStateItem(string name)
        {
            Dbg.Diagnostics.Assert(
                !string.IsNullOrEmpty(name),
                "The caller should verify this parameter");

            return (PSVariable)SessionState.Internal.GetVariable(name, Context.Origin);
        }

        /// <summary>
        /// Sets the variable of the specified name to the specified value.
        /// </summary>
        /// <param name="name">
        /// The name of the variable to set.
        /// </param>
        /// <param name="value">
        /// The new value for the variable.
        /// </param>
        /// <param name="writeItem">
        /// If true, the item that was set should be written to WriteItemObject.
        /// </param>
        internal override void SetSessionStateItem(string name, object value, bool writeItem)
        {
            Dbg.Diagnostics.Assert(
                !string.IsNullOrEmpty(name),
                "The caller should verify this parameter");

            PSVariable variable = null;

            if (value != null)
            {
                variable = value as PSVariable;
                if (variable == null)
                {
                    variable = new PSVariable(name, value);
                }
                else
                {
                    // ensure the name matches

                    if (!string.Equals(name, variable.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        PSVariable newVar = new PSVariable(name, variable.Value, variable.Options, variable.Attributes);
                        newVar.Description = variable.Description;
                        variable = newVar;
                    }
                }
            }
            else
            {
                variable = new PSVariable(name, null);
            }

            PSVariable item = SessionState.Internal.SetVariable(variable, Force, Context.Origin) as PSVariable;

            if (writeItem && item != null)
            {
                WriteItemObject(item, item.Name, false);
            }
        }

        /// <summary>
        /// Removes the specified variable from session state.
        /// </summary>
        /// <param name="name">
        /// The name of the variable to remove from session state.
        /// </param>
        internal override void RemoveSessionStateItem(string name)
        {
            Dbg.Diagnostics.Assert(
                !string.IsNullOrEmpty(name),
                "The caller should verify this parameter");

            SessionState.Internal.RemoveVariable(name, Force);
        }

        /// <summary>
        /// Gets a flattened view of the variables in session state.
        /// </summary>
        /// <returns>
        /// An IDictionary representing the flattened view of the variables in
        /// session state.
        /// </returns>
        internal override IDictionary GetSessionStateTable()
        {
            return (IDictionary)SessionState.Internal.GetVariableTable();
        }

        /// <summary>
        /// Gets the value of the item that is returned from GetItem by
        /// extracting the PSVariable value.
        /// </summary>
        /// <param name="item">
        /// The item to extract the value from.
        /// </param>
        /// <returns>
        /// The value of the specified item.
        /// </returns>
        internal override object GetValueOfItem(object item)
        {
            Dbg.Diagnostics.Assert(
                item != null,
                "Caller should verify the item parameter");

            // Call the base class to unwrap the DictionaryEntry
            // if necessary

            object value = base.GetValueOfItem(item);

            PSVariable var = item as PSVariable;
            if (var != null)
            {
                value = var.Value;
            }

            return value;
        }

        /// <summary>
        /// Determines if the item can be renamed. Derived classes that need
        /// to perform a check should override this method.
        /// </summary>
        /// <param name="item">
        /// The item to verify if it can be renamed.
        /// </param>
        /// <returns>
        /// true if the item can be renamed or false otherwise.
        /// </returns>
        internal override bool CanRenameItem(object item)
        {
            bool result = false;

            PSVariable variable = item as PSVariable;
            if (variable != null)
            {
                if ((variable.Options & ScopedItemOptions.Constant) != 0 ||
                    ((variable.Options & ScopedItemOptions.ReadOnly) != 0 && !Force))
                {
                    SessionStateUnauthorizedAccessException e =
                        new SessionStateUnauthorizedAccessException(
                            variable.Name,
                            SessionStateCategory.Variable,
                            "CannotRenameVariable",
                            SessionStateStrings.CannotRenameVariable);

                    throw e;
                }

                result = true;
            }

            return result;
        }
        #endregion protected members

    }
}
