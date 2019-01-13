// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A command to get the property of an item at a specified path.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "ItemProperty", DefaultParameterSetName = "Path", SupportsTransactions = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113320")]
    public class GetItemPropertyCommand : ItemPropertyCommandBase
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
        /// The properties to retrieve from the item.
        /// </summary>
        [Parameter(Position = 1)]
        [Alias("PSProperty")]
        public string[] Name
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
            if (Path != null && Path.Length > 0)
            {
                return InvokeProvider.Property.GetPropertyDynamicParameters(
                    Path[0],
                    SessionStateUtilities.ConvertArrayToCollection<string>(_property), context);
            }

            return InvokeProvider.Property.GetPropertyDynamicParameters(
                ".",
                SessionStateUtilities.ConvertArrayToCollection<string>(_property), context);
        }

        #endregion Parameters

        #region parameter data

        /// <summary>
        /// The properties to be retrieved.
        /// </summary>
        private string[] _property;

        #endregion parameter data

        #region Command code

        /// <summary>
        /// Gets the properties of an item at the specified path.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (string path in Path)
            {
                try
                {
                    InvokeProvider.Property.Get(
                        path,
                        SessionStateUtilities.ConvertArrayToCollection<string>(_property),
                        CmdletProviderContext);
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

    /// <summary>
    /// A command to get the property value of an item at a specified path.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "ItemPropertyValue", DefaultParameterSetName = "Path", SupportsTransactions = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=389281")]
    public sealed class GetItemPropertyValueCommand : ItemPropertyCommandBase
    {
        #region Parameters

        /// <summary>
        /// Gets or sets the path parameter to the command.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = "Path", Mandatory = false, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
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
        [Parameter(ParameterSetName = "LiteralPath", Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
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
        /// The properties to retrieve from the item.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true)]
        [Alias("PSProperty")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Name
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
            if (Path != null && Path.Length > 0)
            {
                return InvokeProvider.Property.GetPropertyDynamicParameters(
                    Path[0],
                    SessionStateUtilities.ConvertArrayToCollection<string>(_property), context);
            }

            return InvokeProvider.Property.GetPropertyDynamicParameters(
                ".",
                SessionStateUtilities.ConvertArrayToCollection<string>(_property), context);
        }

        #endregion Parameters

        #region parameter data

        /// <summary>
        /// The properties to be retrieved.
        /// </summary>
        private string[] _property;

        #endregion parameter data

        #region Command code

        /// <summary>
        /// Gets the values of the properties of an item at the specified path.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (Path == null || Path.Length == 0)
            {
                paths = new string[] { "." };
            }

            foreach (string path in Path)
            {
                try
                {
                    Collection<PSObject> itemProperties = InvokeProvider.Property.Get(
                        new string[] { path },
                        SessionStateUtilities.ConvertArrayToCollection<string>(_property),
                        base.SuppressWildcardExpansion);

                    if (itemProperties != null)
                    {
                        foreach (PSObject currentItem in itemProperties)
                        {
                            if (this.Name != null)
                            {
                                foreach (string currentPropertyName in this.Name)
                                {
                                    if (currentItem.Properties != null &&
                                        currentItem.Properties[currentPropertyName] != null &&
                                        currentItem.Properties[currentPropertyName].Value != null)
                                    {
                                        CmdletProviderContext.WriteObject(currentItem.Properties[currentPropertyName].Value);
                                    }
                                }
                            }
                        }
                    }
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
