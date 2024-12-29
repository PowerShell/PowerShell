// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Provider;

using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This provider is the data accessor for shell functions. It uses
    /// the SessionStateProviderBase as the base class to produce a view on
    /// session state data.
    /// </summary>
    [CmdletProvider(FunctionProvider.ProviderName, ProviderCapabilities.ShouldProcess)]
    [OutputType(typeof(FunctionInfo), ProviderCmdlet = ProviderCmdlet.SetItem)]
    [OutputType(typeof(FunctionInfo), ProviderCmdlet = ProviderCmdlet.RenameItem)]
    [OutputType(typeof(FunctionInfo), ProviderCmdlet = ProviderCmdlet.CopyItem)]
    [OutputType(typeof(FunctionInfo), ProviderCmdlet = ProviderCmdlet.GetChildItem)]
    [OutputType(typeof(FunctionInfo), ProviderCmdlet = ProviderCmdlet.GetItem)]
    [OutputType(typeof(FunctionInfo), ProviderCmdlet = ProviderCmdlet.NewItem)]
    public sealed class FunctionProvider : SessionStateProviderBase
    {
        /// <summary>
        /// Gets the name of the provider.
        /// </summary>
        public const string ProviderName = "Function";

        #region Constructor

        /// <summary>
        /// The constructor for the provider that exposes variables to the user
        /// as drives.
        /// </summary>
        public FunctionProvider()
        {
        }

        #endregion Constructor

        #region DriveCmdletProvider overrides

        /// <summary>
        /// Initializes the function drive.
        /// </summary>
        /// <returns>
        /// An array of a single PSDriveInfo object representing the functions drive.
        /// </returns>
        protected override Collection<PSDriveInfo> InitializeDefaultDrives()
        {
            string description = SessionStateStrings.FunctionDriveDescription;

            PSDriveInfo functionDrive =
                new PSDriveInfo(
                    DriveNames.FunctionDrive,
                    ProviderInfo,
                    string.Empty,
                    description,
                    null);

            Collection<PSDriveInfo> drives = new Collection<PSDriveInfo>();
            drives.Add(functionDrive);
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
        /// An instance of FunctionProviderDynamicParameters which is the dynamic parameters for
        /// NewItem.
        /// </returns>
        protected override object NewItemDynamicParameters(string path, string type, object newItemValue)
        {
            return new FunctionProviderDynamicParameters();
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
        /// An instance of FunctionProviderDynamicParameters which is the dynamic parameters for
        /// SetItem.
        /// </returns>
        protected override object SetItemDynamicParameters(string path, object value)
        {
            return new FunctionProviderDynamicParameters();
        }

        #endregion Dynamic Parameters

        #region protected members

        /// <summary>
        /// Gets a function from session state.
        /// </summary>
        /// <param name="name">
        /// The name of the function to retrieve.
        /// </param>
        /// <returns>
        /// A ScriptBlock that represents the function.
        /// </returns>
        internal override object GetSessionStateItem(string name)
        {
            Dbg.Diagnostics.Assert(
                !string.IsNullOrEmpty(name),
                "The caller should verify this parameter");

            CommandInfo function = SessionState.Internal.GetFunction(name, Context.Origin);

            return function;
        }

        /// <summary>
        /// Sets the function of the specified name to the specified value.
        /// </summary>
        /// <param name="name">
        /// The name of the function to set.
        /// </param>
        /// <param name="value">
        /// The new value for the function.
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

            FunctionProviderDynamicParameters dynamicParameters =
                DynamicParameters as FunctionProviderDynamicParameters;

            CommandInfo modifiedItem = null;

            bool dynamicParametersSpecified = dynamicParameters != null && dynamicParameters.OptionsSet;

            if (value == null)
            {
                // If the value wasn't specified but the options were, just set the
                // options on the existing function.
                // If the options weren't specified, then remove the function

                if (dynamicParametersSpecified)
                {
                    modifiedItem = (CommandInfo)GetSessionStateItem(name);

                    if (modifiedItem != null)
                    {
                        SetOptions(modifiedItem, dynamicParameters.Options);
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
                    // Unwrap the PSObject before binding it as a scriptblock...
                    PSObject pso = value as PSObject;
                    if (pso != null)
                    {
                        value = pso.BaseObject;
                    }

                    ScriptBlock scriptBlockValue = value as ScriptBlock;
                    if (scriptBlockValue != null)
                    {
                        if (dynamicParametersSpecified)
                        {
                            modifiedItem = SessionState.Internal.SetFunction(name, scriptBlockValue,
                                null, dynamicParameters.Options, Force, Context.Origin);
                        }
                        else
                        {
                            modifiedItem = SessionState.Internal.SetFunction(name, scriptBlockValue, null, Force, Context.Origin);
                        }

                        break;
                    }

                    FunctionInfo function = value as FunctionInfo;
                    if (function != null)
                    {
                        ScopedItemOptions options = function.Options;

                        if (dynamicParametersSpecified)
                        {
                            options = dynamicParameters.Options;
                        }

                        modifiedItem = SessionState.Internal.SetFunction(name, function.ScriptBlock, function, options, Force, Context.Origin);
                        break;
                    }

                    string stringValue = value as string;
                    if (stringValue != null)
                    {
                        ScriptBlock scriptBlock = ScriptBlock.Create(Context.ExecutionContext, stringValue);

                        if (dynamicParametersSpecified)
                        {
                            modifiedItem = SessionState.Internal.SetFunction(name, scriptBlock, null, dynamicParameters.Options, Force, Context.Origin);
                        }
                        else
                        {
                            modifiedItem = SessionState.Internal.SetFunction(name, scriptBlock, null, Force, Context.Origin);
                        }

                        break;
                    }

                    throw PSTraceSource.NewArgumentException(nameof(value));
                } while (false);

                if (writeItem && modifiedItem != null)
                {
                    WriteItemObject(modifiedItem, modifiedItem.Name, false);
                }
            }
        }
#pragma warning restore 0162

        private static void SetOptions(CommandInfo function, ScopedItemOptions options)
        {
            ((FunctionInfo)function).Options = options;
        }

        /// <summary>
        /// Removes the specified function from session state.
        /// </summary>
        /// <param name="name">
        /// The name of the function to remove from session state.
        /// </param>
        internal override void RemoveSessionStateItem(string name)
        {
            Dbg.Diagnostics.Assert(
                !string.IsNullOrEmpty(name),
                "The caller should verify this parameter");

            SessionState.Internal.RemoveFunction(name, Force);
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

            FunctionInfo function = item as FunctionInfo;
            if (function != null)
            {
                value = function.ScriptBlock;
            }

            return value;
        }

        /// <summary>
        /// Gets a flattened view of the functions in session state.
        /// </summary>
        /// <returns>
        /// An IDictionary representing the flattened view of the functions in
        /// session state.
        /// </returns>
        internal override IDictionary GetSessionStateTable()
        {
            return (IDictionary)SessionState.Internal.GetFunctionTable();
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

            FunctionInfo functionInfo = item as FunctionInfo;
            if (functionInfo != null)
            {
                if ((functionInfo.Options & ScopedItemOptions.Constant) != 0 ||
                    ((functionInfo.Options & ScopedItemOptions.ReadOnly) != 0 && !Force))
                {
                    SessionStateUnauthorizedAccessException e =
                        new SessionStateUnauthorizedAccessException(
                            functionInfo.Name,
                            SessionStateCategory.Function,
                            "CannotRenameFunction",
                            SessionStateStrings.CannotRenameFunction);

                    throw e;
                }

                result = true;
            }

            return result;
        }

        #endregion protected members
    }

    /// <summary>
    /// The dynamic parameter object for the FunctionProvider SetItem and NewItem commands.
    /// </summary>
    public class FunctionProviderDynamicParameters
    {
        /// <summary>
        /// Gets or sets the option parameter for the function.
        /// </summary>
        [Parameter]
        public ScopedItemOptions Options
        {
            get
            {
                return _options;
            }

            set
            {
                _optionsSet = true;
                _options = value;
            }
        }

        private ScopedItemOptions _options = ScopedItemOptions.None;

        /// <summary>
        /// Determines if the Options parameter was set.
        /// </summary>
        /// <value></value>
        internal bool OptionsSet
        {
            get { return _optionsSet; }
        }

        private bool _optionsSet;
    }
}
