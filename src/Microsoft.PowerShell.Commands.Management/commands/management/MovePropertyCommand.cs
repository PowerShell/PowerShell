// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A command to move a property on an item to another item.
    /// </summary>
    [Cmdlet(VerbsCommon.Move, "ItemProperty", SupportsShouldProcess = true, DefaultParameterSetName = "Path", SupportsTransactions = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113351")]
    public class MoveItemPropertyCommand : PassThroughItemPropertyCommandBase
    {
        #region Parameters

        /// <summary>
        /// Gets or sets the path parameter to the command.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = "Path",
                   Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string[] Path
        {
            get { return paths; }

            set { paths = value; }
        }

        /// <summary>
        /// Gets or sets the literal path parameter to the command.
        /// </summary>
        [Parameter(ParameterSetName = "LiteralPath",
                   Mandatory = true, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        public string[] LiteralPath
        {
            get { return paths; }

            set
            {
                base.SuppressWildcardExpansion = true;
                paths = value;
            }
        }

        /// <summary>
        /// The name of the property to create on the item.
        /// </summary>
        [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [Alias("PSProperty")]
        public string[] Name
        {
            get { return _property; }

            set
            {
                if (value == null)
                {
                    value = Array.Empty<string>();
                }

                _property = value;
            }
        }

        /// <summary>
        /// The path to the destination item to copy the property to.
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true)]
        public string Destination { get; set; }

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
            string propertyName = string.Empty;
            if (Name != null && Name.Length > 0)
            {
                propertyName = Name[0];
            }

            if (Path != null && Path.Length > 0)
            {
                return InvokeProvider.Property.MovePropertyDynamicParameters(Path[0], propertyName, Destination, propertyName, context);
            }

            return InvokeProvider.Property.MovePropertyDynamicParameters(
                ".",
                propertyName,
                Destination,
                propertyName,
                context);
        }

        #endregion Parameters

        #region parameter data

        /// <summary>
        /// The property to be created.
        /// </summary>
        private string[] _property = Array.Empty<string>();

        #endregion parameter data

        #region Command code

        /// <summary>
        /// Creates the property on the item.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (string path in Path)
            {
                foreach (string propertyName in Name)
                {
                    try
                    {
                        InvokeProvider.Property.Move(path, propertyName, Destination, propertyName, GetCurrentContext());
                    }
                    catch (PSNotSupportedException notSupported)
                    {
                        WriteError(
                            new ErrorRecord(
                                notSupported.ErrorRecord,
                                notSupported));
                        continue;
                    }
                    catch (DriveNotFoundException driveNotFound)
                    {
                        WriteError(
                            new ErrorRecord(
                                driveNotFound.ErrorRecord,
                                driveNotFound));
                        continue;
                    }
                    catch (ProviderNotFoundException providerNotFound)
                    {
                        WriteError(
                            new ErrorRecord(
                                providerNotFound.ErrorRecord,
                                providerNotFound));
                        continue;
                    }
                    catch (ItemNotFoundException pathNotFound)
                    {
                        WriteError(
                            new ErrorRecord(
                                pathNotFound.ErrorRecord,
                                pathNotFound));
                        continue;
                    }
                }
            }
        }
        #endregion Command code

    }
}
