// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A command to set the property of an item at a specified path.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "ItemProperty", DefaultParameterSetName = "propertyValuePathSet", SupportsShouldProcess = true, SupportsTransactions = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097147")]
    public class SetItemPropertyCommand : PassThroughItemPropertyCommandBase
    {
        private const string propertyValuePathSet = "propertyValuePathSet";
        private const string propertyValueLiteralPathSet = "propertyValueLiteralPathSet";
        private const string propertyPSObjectPathSet = "propertyPSObjectPathSet";
        private const string propertyPSObjectLiteralPathSet = "propertyPSObjectLiteralPathSet";

        #region Parameters

        /// <summary>
        /// Gets or sets the path parameter to the command.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = propertyPSObjectPathSet,
                   Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [Parameter(Position = 0, ParameterSetName = propertyValuePathSet,
                   Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string[] Path
        {
            get { return paths; }

            set { paths = value; }
        }

        /// <summary>
        /// Gets or sets the literal path parameter to the command.
        /// </summary>
        [Parameter(ParameterSetName = propertyValueLiteralPathSet,
                   Mandatory = true, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        [Parameter(ParameterSetName = propertyPSObjectLiteralPathSet,
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

        #region Property Value set

        /// <summary>
        /// The name of the property to set.
        /// </summary>
        /// <value>
        /// This value type is determined by the InvokeProvider.
        /// </value>
        [Parameter(Position = 1, ParameterSetName = propertyValuePathSet,
                   Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [Parameter(Position = 1, ParameterSetName = propertyValueLiteralPathSet,
                   Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [Alias("PSProperty")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The value of the property to set.
        /// </summary>
        /// <value>
        /// This value type is determined by the InvokeProvider.
        /// </value>
        [Parameter(Position = 2, ParameterSetName = propertyValuePathSet,
                   Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [Parameter(Position = 2, ParameterSetName = propertyValueLiteralPathSet,
                   Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [AllowNull]
        public object Value { get; set; }

        #endregion Property Value set

        #region Shell object set

        /// <summary>
        /// A PSObject that contains the properties and values to be set.
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = propertyPSObjectPathSet, Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ValueFromPipeline = true)]
        [Parameter(ParameterSetName = propertyPSObjectLiteralPathSet, Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ValueFromPipeline = true)]
        public PSObject InputObject { get; set; }

        #endregion Shell object set

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
            PSObject mshObject = null;

            switch (ParameterSetName)
            {
                case propertyValuePathSet:
                case propertyValueLiteralPathSet:
                    if (!string.IsNullOrEmpty(Name))
                    {
                        mshObject = new PSObject();
                        mshObject.Properties.Add(new PSNoteProperty(Name, Value));
                    }

                    break;

                default:
                    mshObject = InputObject;
                    break;
            }

            if (Path != null && Path.Length > 0)
            {
                return InvokeProvider.Property.SetPropertyDynamicParameters(Path[0], mshObject, context);
            }

            return InvokeProvider.Property.SetPropertyDynamicParameters(".", mshObject, context);
        }

        #endregion Parameters

        #region parameter data

        #endregion parameter data

        #region Command code

        /// <summary>
        /// Sets the content of the item at the specified path.
        /// </summary>
        protected override void ProcessRecord()
        {
            // Default to the CmdletProviderContext that will direct output to
            // the pipeline.

            CmdletProviderContext currentCommandContext = CmdletProviderContext;
            currentCommandContext.PassThru = PassThru;

            PSObject mshObject = null;

            switch (ParameterSetName)
            {
                case propertyValuePathSet:
                case propertyValueLiteralPathSet:
                    mshObject = new PSObject();
                    mshObject.Properties.Add(new PSNoteProperty(Name, Value));
                    break;

                case propertyPSObjectPathSet:
                    mshObject = InputObject;
                    break;

                default:
                    Diagnostics.Assert(
                        false,
                        "One of the parameter sets should have been resolved or an error should have been thrown by the command processor");
                    break;
            }

            foreach (string path in Path)
            {
                try
                {
                    InvokeProvider.Property.Set(path, mshObject, currentCommandContext);
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
        #endregion Command code

    }
}
