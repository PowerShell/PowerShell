// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A command to clear the value of a property of an item at a specified path.
    /// </summary>
    [Cmdlet(VerbsCommon.Clear, "ItemProperty", DefaultParameterSetName = "Path", SupportsShouldProcess = true, SupportsTransactions = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096903")]
    public class ClearItemPropertyCommand : PassThroughItemPropertyCommandBase
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
        /// The properties to clear from the item.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Name
        {
            get
            {
                return _property;
            }

            set
            {
                _property = value;
            }
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
            Collection<string> propertyCollection = new();
            propertyCollection.Add(_property);

            if (Path != null && Path.Length > 0)
            {
                // Go ahead and let any exception terminate the pipeline.

                return InvokeProvider.Property.ClearPropertyDynamicParameters(
                    Path[0],
                    propertyCollection,
                    context);
            }

            return InvokeProvider.Property.ClearPropertyDynamicParameters(
                ".",
                propertyCollection,
                context);
        }

        #endregion Parameters

        #region parameter data

        /// <summary>
        /// The properties to be cleared.
        /// </summary>
        private string _property;

        #endregion parameter data

        #region Command code

        /// <summary>
        /// Clears the properties of an item at the specified path.
        /// </summary>
        protected override void ProcessRecord()
        {
            CmdletProviderContext currentContext = CmdletProviderContext;
            currentContext.PassThru = PassThru;

            Collection<string> propertyCollection = new();
            propertyCollection.Add(_property);

            foreach (string path in Path)
            {
                try
                {
                    InvokeProvider.Property.Clear(
                        path,
                        propertyCollection,
                        currentContext);
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
        }
        #endregion Command code

    }
}
