// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation;
using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A command to rename a property of an item at a specified path.
    /// </summary>
    [Cmdlet(VerbsCommon.Rename, "ItemProperty", DefaultParameterSetName = "Path", SupportsShouldProcess = true, SupportsTransactions = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113383")]
    public class RenameItemPropertyCommand : PassThroughItemPropertyCommandBase
    {
        #region Parameters

        /// <summary>
        /// Gets or sets the path parameter to the command.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = "Path",
                   Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string Path
        {
            get
            {
                return _path;
            }

            set
            {
                _path = value;
            }
        }

        /// <summary>
        /// Gets or sets the literal path parameter to the command.
        /// </summary>
        [Parameter(ParameterSetName = "LiteralPath",
                   Mandatory = true, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        public string LiteralPath
        {
            get
            {
                return _path;
            }

            set
            {
                base.SuppressWildcardExpansion = true;
                _path = value;
            }
        }

        /// <summary>
        /// The properties to be renamed on the item.
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true)]
        [Alias("PSProperty")]
        public string Name { get; set; }

        /// <summary>
        /// The new name of the property on the item.
        /// </summary>
        [Parameter(Mandatory = true, Position = 2, ValueFromPipelineByPropertyName = true)]
        public string NewName { get; set; }

        /// <summary>
        /// A virtual method for retrieving the dynamic parameters for a cmdlet. Derived cmdlets
        /// that require dynamic parameters should override this method and return the
        /// dynamic parameter object.
        /// </summary>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// An object representing the dynamic parameters for the cmdlet or null if there
        /// are none.
        /// </returns>
        internal override object GetDynamicParameters(CmdletProviderContext context)
        {
            if (Path != null)
            {
                return InvokeProvider.Property.RenamePropertyDynamicParameters(Path, Name, NewName, context);
            }

            return InvokeProvider.Property.RenamePropertyDynamicParameters(".", Name, NewName, context);
        }

        #endregion Parameters

        #region parameter data

        /// <summary>
        /// The path to rename the property on.
        /// </summary>
        private string _path;

        #endregion parameter data

        #region Command code

        /// <summary>
        /// Renames a property on an item.
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                CmdletProviderContext currentContext = CmdletProviderContext;
                currentContext.PassThru = PassThru;

                InvokeProvider.Property.Rename(_path, Name, NewName, currentContext);
            }
            catch (PSNotSupportedException notSupported)
            {
                WriteError(
                    new ErrorRecord(
                        notSupported.ErrorRecord,
                        notSupported));
            }
            catch (DriveNotFoundException driveNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        driveNotFound.ErrorRecord,
                        driveNotFound));
            }
            catch (ProviderNotFoundException providerNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        providerNotFound.ErrorRecord,
                        providerNotFound));
            }
            catch (ItemNotFoundException pathNotFound)
            {
                WriteError(
                    new ErrorRecord(
                        pathNotFound.ErrorRecord,
                        pathNotFound));
            }
        }
        #endregion Command code

    }
}
