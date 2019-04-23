// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A command to remove a property from an item.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "ItemProperty", DefaultParameterSetName = "Path", SupportsShouldProcess = true, SupportsTransactions = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113374")]
    public class RemoveItemPropertyCommand : ItemPropertyCommandBase
    {
        #region Parameters

        /// <summary>
        /// Gets or sets the path parameter to the command.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = "Path",
                   Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string[] Path
        {
            get
            {
                return paths;
            }

            set
            {
                paths = value;
            }
        }

        /// <summary>
        /// Gets or sets the literal path parameter to the command.
        /// </summary>
        [Parameter(ParameterSetName = "LiteralPath",
                   Mandatory = true, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        public string[] LiteralPath
        {
            get
            {
                return paths;
            }

            set
            {
                base.SuppressWildcardExpansion = true;
                paths = value;
            }
        }

        /// <summary>
        /// The name of the property to create on the item.
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true)]
        [Alias("PSProperty")]
        public string[] Name
        {
            get { return _property; }

            set { _property = value ?? Array.Empty<string>(); }
        }

        /// <summary>
        /// Gets or sets the force property.
        /// </summary>
        /// <remarks>
        /// Gives the provider guidance on how vigorous it should be about performing
        /// the operation. If true, the provider should do everything possible to perform
        /// the operation. If false, the provider should attempt the operation but allow
        /// even simple errors to terminate the operation.
        /// For example, if the user tries to copy a file to a path that already exists and
        /// the destination is read-only, if force is true, the provider should copy over
        /// the existing read-only file. If force is false, the provider should write an error.
        /// </remarks>
        [Parameter]
        public override SwitchParameter Force
        {
            get { return base.Force; }

            set { base.Force = value; }
        }

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
            string propertyName = null;
            if (Name != null && Name.Length > 0)
            {
                propertyName = Name[0];
            }

            if (Path != null && Path.Length > 0)
            {
                return InvokeProvider.Property.RemovePropertyDynamicParameters(Path[0], propertyName, context);
            }

            return InvokeProvider.Property.RemovePropertyDynamicParameters(".", propertyName, context);
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
        /// Removes the property from the item.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (string path in Path)
            {
                foreach (string prop in Name)
                {
                    try
                    {
                        InvokeProvider.Property.Remove(path, prop, CmdletProviderContext);
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
