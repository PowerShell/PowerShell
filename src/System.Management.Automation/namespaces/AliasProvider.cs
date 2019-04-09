// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Dbg = System.Management.Automation;
using System.Collections;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Provider;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This provider is the data accessor for shell aliases. It uses
    /// the SessionStateProviderBase as the base class to produce a view on
    /// session state data.
    /// </summary>
    [CmdletProvider(AliasProvider.ProviderName, ProviderCapabilities.ShouldProcess)]
    [OutputType(typeof(AliasInfo), ProviderCmdlet = ProviderCmdlet.SetItem)]
    [OutputType(typeof(AliasInfo), ProviderCmdlet = ProviderCmdlet.RenameItem)]
    [OutputType(typeof(AliasInfo), ProviderCmdlet = ProviderCmdlet.CopyItem)]
    [OutputType(typeof(AliasInfo), ProviderCmdlet = ProviderCmdlet.GetChildItem)]
    [OutputType(typeof(AliasInfo), ProviderCmdlet = ProviderCmdlet.NewItem)]
    public sealed class AliasProvider : SessionStateProviderBase
    {
        /// <summary>
        /// Gets the name of the provider.
        /// </summary>
        public const string ProviderName = "Alias";

        #region Constructor

        /// <summary>
        /// The constructor for the provider that exposes variables to the user
        /// as drives.
        /// </summary>
        public AliasProvider()
        {
        }

        #endregion Constructor

        #region DriveCmdletProvider overrides

        /// <summary>
        /// Initializes the alias drive.
        /// </summary>
        /// <returns>
        /// An array of a single PSDriveInfo object representing the alias drive.
        /// </returns>
        protected override Collection<PSDriveInfo> InitializeDefaultDrives()
        {
            string description = SessionStateStrings.AliasDriveDescription;

            PSDriveInfo aliasDrive =
                new PSDriveInfo(
                    DriveNames.AliasDrive,
                    ProviderInfo,
                    string.Empty,
                    description,
                    null);

            Collection<PSDriveInfo> drives = new Collection<PSDriveInfo>();
            drives.Add(aliasDrive);
            return drives;
        }

        #endregion DriveCmdletProvider overrides

        #region Dynamic Parameters

        /// <summary>
        /// Gets the dynamic parameters for the NewItem cmdlet.
        /// </summary>
        /// <param name="path">
        /// Ignored.
        /// </param>
        /// <param name="type">
        /// Ignored.
        /// </param>
        /// <param name="newItemValue">
        /// Ignored.
        /// </param>
        /// <returns>
        /// An instance of AliasProviderDynamicParameters which is the dynamic parameters for
        /// NewItem.
        /// </returns>
        protected override object NewItemDynamicParameters(string path, string type, object newItemValue)
        {
            return new AliasProviderDynamicParameters();
        }

        /// <summary>
        /// Gets the dynamic parameters for the NewItem cmdlet.
        /// </summary>
        /// <param name="path">
        /// Ignored.
        /// </param>
        /// <param name="value">
        /// Ignored.
        /// </param>
        /// <returns>
        /// An instance of AliasProviderDynamicParameters which is the dynamic parameters for
        /// SetItem.
        /// </returns>
        protected override object SetItemDynamicParameters(string path, object value)
        {
            return new AliasProviderDynamicParameters();
        }

        #endregion Dynamic Parameters

        #region protected members

        /// <summary>
        /// Gets a alias from session state.
        /// </summary>
        /// <param name="name">
        /// The name of the alias to retrieve.
        /// </param>
        /// <returns>
        /// A DictionaryEntry that represents the value of the alias.
        /// </returns>
        internal override object GetSessionStateItem(string name)
        {
            Dbg.Diagnostics.Assert(
                !string.IsNullOrEmpty(name),
                "The caller should verify this parameter");

            AliasInfo value = SessionState.Internal.GetAlias(name, Context.Origin);

            return value;
        }

        /// <summary>
        /// Since items are often more than their value, this method should
        /// be overridden to provide the value for an item.
        /// </summary>
        /// <param name="item">
        /// The item to extract the value from.
        /// </param>
        /// <returns>
        /// The value of the specified item.
        /// </returns>
        /// <remarks>
        /// The default implementation will get
        /// the Value property of a DictionaryEntry
        /// </remarks>
        internal override object GetValueOfItem(object item)
        {
            Dbg.Diagnostics.Assert(
                item != null,
                "Caller should verify the item parameter");

            object value = item;

            AliasInfo aliasInfo = item as AliasInfo;
            if (aliasInfo != null)
            {
                value = aliasInfo.Definition;
            }

            return value;
        }

        /// <summary>
        /// Sets the alias of the specified name to the specified value.
        /// </summary>
        /// <param name="name">
        /// The name of the alias to set.
        /// </param>
        /// <param name="value">
        /// The new value for the alias.
        /// </param>
        /// <param name="writeItem">
        /// If true, the item that was set should be written to WriteItemObject.
        /// </param>
#pragma warning disable 0162
        internal override void SetSessionStateItem(string name, object value, bool writeItem)
        {
            Dbg.Diagnostics.Assert(
                !string.IsNullOrEmpty(name),
                "The caller should verify this parameter");

            AliasProviderDynamicParameters dynamicParameters =
                DynamicParameters as AliasProviderDynamicParameters;

            AliasInfo item = null;

            bool dynamicParametersSpecified = dynamicParameters != null && dynamicParameters.OptionsSet;

            if (value == null)
            {
                if (dynamicParametersSpecified)
                {
                    item = (AliasInfo)GetSessionStateItem(name);

                    if (item != null)
                    {
                        item.SetOptions(dynamicParameters.Options, Force);
                    }
                }
                else
                {
                    RemoveSessionStateItem(name);
                }
            }
            else
            {
                do // false loop
                {
                    string stringValue = value as string;
                    if (stringValue != null)
                    {
                        if (dynamicParametersSpecified)
                        {
                            item = SessionState.Internal.SetAliasValue(name, stringValue, dynamicParameters.Options, Force, Context.Origin);
                        }
                        else
                        {
                            item = SessionState.Internal.SetAliasValue(name, stringValue, Force, Context.Origin);
                        }

                        break;
                    }

                    AliasInfo alias = value as AliasInfo;
                    if (alias != null)
                    {
                        AliasInfo newAliasInfo =
                            new AliasInfo(
                                name,
                                alias.Definition,
                                this.Context.ExecutionContext,
                                alias.Options);

                        if (dynamicParametersSpecified)
                        {
                            newAliasInfo.SetOptions(dynamicParameters.Options, Force);
                        }

                        item = SessionState.Internal.SetAliasItem(newAliasInfo, Force, Context.Origin);
                        break;
                    }

                    throw PSTraceSource.NewArgumentException("value");
                } while (false);
            }

            if (writeItem && item != null)
            {
                WriteItemObject(item, item.Name, false);
            }
        }
#pragma warning restore 0162

        /// <summary>
        /// Removes the specified alias from session state.
        /// </summary>
        /// <param name="name">
        /// The name of the alias to remove from session state.
        /// </param>
        internal override void RemoveSessionStateItem(string name)
        {
            Dbg.Diagnostics.Assert(
                !string.IsNullOrEmpty(name),
                "The caller should verify this parameter");

            SessionState.Internal.RemoveAlias(name, Force);
        }

        /// <summary>
        /// Gets a flattened view of the alias in session state.
        /// </summary>
        /// <returns>
        /// An IDictionary representing the flattened view of the aliases in
        /// session state.
        /// </returns>
        internal override IDictionary GetSessionStateTable()
        {
            return (IDictionary)SessionState.Internal.GetAliasTable();
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

            AliasInfo aliasInfo = item as AliasInfo;
            if (aliasInfo != null)
            {
                if ((aliasInfo.Options & ScopedItemOptions.Constant) != 0 ||
                    ((aliasInfo.Options & ScopedItemOptions.ReadOnly) != 0 && !Force))
                {
                    SessionStateUnauthorizedAccessException e =
                        new SessionStateUnauthorizedAccessException(
                            aliasInfo.Name,
                            SessionStateCategory.Alias,
                            "CannotRenameAlias",
                            SessionStateStrings.CannotRenameAlias);

                    throw e;
                }

                result = true;
            }

            return result;
        }

        #endregion protected members

    }

    /// <summary>
    /// The dynamic parameter object for the AliasProvider SetItem and NewItem commands.
    /// </summary>
    public class AliasProviderDynamicParameters
    {
        /// <summary>
        /// Gets or sets the option parameter for the alias.
        /// </summary>
        [Parameter]
        public ScopedItemOptions Options
        {
            get { return _options; }

            set
            {
                _optionsSet = true;
                _options = value;
            }
        }

        private ScopedItemOptions _options;

        /// <summary>
        /// Determines if the Options parameter was set.
        /// </summary>
        /// <value></value>
        internal bool OptionsSet
        {
            get { return _optionsSet; }
        }

        private bool _optionsSet = false;
    }
}

