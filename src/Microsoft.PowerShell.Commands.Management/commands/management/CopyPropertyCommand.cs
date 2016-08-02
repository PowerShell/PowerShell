/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Management.Automation;
using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A command to copy a property on an item.
    /// </summary>
    [Cmdlet(VerbsCommon.Copy, "ItemProperty", DefaultParameterSetName = "Path", SupportsShouldProcess = true, SupportsTransactions = true,
        HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113293")]
    public class CopyItemPropertyCommand : PassThroughItemPropertyCommandBase
    {
        #region Parameters

        /// <summary>
        /// Gets or sets the path parameter to the command
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = "Path",
                   Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string[] Path
        {
            get
            {
                return paths;
            } // get

            set
            {
                paths = value;
            } // set
        } // Path

        /// <summary>
        /// Gets or sets the literal path parameter to the command
        /// </summary>
        [Parameter(ParameterSetName = "LiteralPath",
                   Mandatory = true, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath")]
        public string[] LiteralPath
        {
            get
            {
                return paths;
            } // get

            set
            {
                base.SuppressWildcardExpansion = true;
                paths = value;
            } // set
        } // LiteralPath

        /// <summary>
        /// The name of the property to create on the item
        /// </summary>
        ///
        [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [Alias("PSProperty")]
        public string Name
        {
            get
            {
                return _property;
            } // get

            set
            {
                _property = value;
            }
        } // Property

        /// <summary>
        /// The path to the destination item to copy the property to.
        /// </summary>
        /// 
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true)]
        public string Destination
        {
            get
            {
                return _destination;
            } // get

            set
            {
                _destination = value;
            }
        } // Destination

        /// <summary>
        /// A virtual method for retrieving the dynamic parameters for a cmdlet. Derived cmdlets
        /// that require dynamic parameters should override this method and return the
        /// dynamic parameter object.
        /// </summary>
        /// 
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// 
        /// <returns>
        /// An object representing the dynamic parameters for the cmdlet or null if there
        /// are none.
        /// </returns>
        /// 
        internal override object GetDynamicParameters(CmdletProviderContext context)
        {
            if (Path != null & Path.Length > 0)
            {
                return InvokeProvider.Property.CopyPropertyDynamicParameters(
                    Path[0],
                    _property,
                    Destination,
                    _property,
                    context);
            }
            return InvokeProvider.Property.CopyPropertyDynamicParameters(
                ".",
                _property,
                Destination,
                _property,
                context);
        } // GetDynamicParameters

        #endregion Parameters

        #region parameter data

        /// <summary>
        /// The property to be copied.
        /// </summary>
        private string _property;

        /// <summary>
        /// The destination path of the item to copy the property to.
        /// </summary>
        private string _destination;

        #endregion parameter data

        #region Command code

        /// <summary>
        /// Copies the property from one item to another
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (string path in Path)
            {
                try
                {
                    InvokeProvider.Property.Copy(
                        path,
                        _property,
                        Destination,
                        _property,
                        GetCurrentContext());
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
        } // ProcessRecord
        #endregion Command code


    } // CopyItemPropertyCommand
} // namespace Microsoft.PowerShell.Commands
