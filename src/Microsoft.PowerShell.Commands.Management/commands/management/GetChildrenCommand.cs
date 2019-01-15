// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The get-childitem command class.
    /// This command lists the contents of a container.
    /// </summary>
    /// <remarks>
    /// </remarks>
    [Cmdlet(VerbsCommon.Get, "ChildItem", DefaultParameterSetName = "Items", SupportsTransactions = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113308")]
    public class GetChildItemCommand : CoreCommandBase
    {
        /// <summary>
        /// The string declaration for the Items parameter set in this command.
        /// </summary>
        /// <remarks>
        /// The "Items" parameter set includes the following parameters:
        ///     -filter
        ///     -recurse
        /// </remarks>
        private const string childrenSet = "Items";
        private const string literalChildrenSet = "LiteralItems";

        #region Command parameters

        /// <summary>
        /// Gets or sets the path for the operation.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = childrenSet,
                   ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string[] Path
        {
            get
            {
                return _paths;
            }

            set
            {
                _paths = value;
            }
        }

        /// <summary>
        /// Gets or sets the literal path parameter to the command.
        /// </summary>
        [Parameter(ParameterSetName = literalChildrenSet,
                   Mandatory = true, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        public string[] LiteralPath
        {
            get
            {
                return _paths;
            }

            set
            {
                base.SuppressWildcardExpansion = true;
                _paths = value;
            }
        }

        /// <summary>
        /// Gets or sets the filter property.
        /// </summary>
        [Parameter(Position = 1)]
        public override string Filter
        {
            get
            {
                return base.Filter;
            }

            set
            {
                base.Filter = value;
            }
        }

        /// <summary>
        /// Gets or sets the include property.
        /// </summary>
        [Parameter]
        public override string[] Include
        {
            get
            {
                return base.Include;
            }

            set
            {
                base.Include = value;
            }
        }

        /// <summary>
        /// Gets or sets the exclude property.
        /// </summary>
        [Parameter]
        public override string[] Exclude
        {
            get
            {
                return base.Exclude;
            }

            set
            {
                base.Exclude = value;
            }
        }

        /// <summary>
        /// Gets or sets the recurse switch.
        /// </summary>
        [Parameter]
        [Alias("s")]
        public SwitchParameter Recurse
        {
            get
            {
                return _recurse;
            }

            set
            {
                _recurse = value;
            }
        }

        /// <summary>
        /// Gets or sets max depth of recursion; automatically sets Recurse parameter;
        /// Value '0' will show only contents of container specified by -Path (same result as running 'Get-ChildItem' without '-Recurse');
        /// Value '1' will show 1 level deep, etc...;
        /// Default is uint.MaxValue - it performs full recursion (this parameter has no effect).
        /// </summary>
        [Parameter]
        public uint Depth
        {
            get
            {
                return _depth;
            }

            set
            {
                _depth = value;
                this.Recurse = true; // Bug 2391925 - Get-ChildItem -Depth should auto-set -Recurse
            }
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
            get
            {
                return base.Force;
            }

            set
            {
                base.Force = value;
            }
        }

        /// <summary>
        /// Gets or sets the names switch.
        /// </summary>
        [Parameter]
        public SwitchParameter Name
        {
            get
            {
                return _childNames;
            }

            set
            {
                _childNames = value;
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
            object result = null;
            string path = string.Empty;

            if (_paths != null && _paths.Length > 0)
            {
                path = _paths[0];
            }
            else
            {
                path = ".";
            }

            switch (ParameterSetName)
            {
                case childrenSet:
                case literalChildrenSet:
                    if (Name)
                    {
                        result = InvokeProvider.ChildItem.GetChildNamesDynamicParameters(path, context);
                    }
                    else
                    {
                        result = InvokeProvider.ChildItem.GetChildItemsDynamicParameters(path, Recurse, context);
                    }

                    break;

                default:
                    result = InvokeProvider.ChildItem.GetChildItemsDynamicParameters(path, Recurse, context);
                    break;
            }

            return result;
        }

        #endregion Command parameters

        #region command data

        /// <summary>
        /// The path for the get-location operation.
        /// </summary>
        private string[] _paths;

        /// <summary>
        /// Determines if the command should do recursion.
        /// </summary>
        private bool _recurse;

        /// <summary>
        /// Limits the depth of recursion; used with Recurse parameter;
        /// Value '0' will show only contents of container specified by -Path (same result as running 'Get-ChildItem' without '-Recurse');
        /// Value '1' will show 1 level deep, etc...;
        /// Default is uint.MaxValue - it performs full recursion (this parameter has no effect).
        /// </summary>
        private uint _depth = uint.MaxValue;

        /// <summary>
        /// The flag that specifies whether to retrieve the child names or the child items.
        /// </summary>
        private bool _childNames = false;

        #endregion command data

        #region command code

        /// <summary>
        /// The main execution method for the get-childitem command.
        /// </summary>
        protected override void ProcessRecord()
        {
            CmdletProviderContext currentContext = CmdletProviderContext;

            if (_paths == null || _paths.Length == 0)
            {
                _paths = new string[] { string.Empty };
            }

            foreach (string path in _paths)
            {
                switch (ParameterSetName)
                {
                    case childrenSet:
                    case literalChildrenSet:
                        try
                        {
                            if (Name)
                            {
                                // Get the names of the child items using the static namespace method.
                                // The child names should be written directly to the pipeline using the
                                // context.WriteObject method.

                                InvokeProvider.ChildItem.GetNames(path, ReturnContainers.ReturnMatchingContainers, Recurse, Depth, currentContext);
                            }
                            else
                            {
                                // Get the children using the static namespace method.
                                // The children should be written directly to the pipeline using
                                // the context.WriteObject method.

                                InvokeProvider.ChildItem.Get(path, Recurse, Depth, currentContext);
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

                        break;

                    default:
                        Dbg.Diagnostics.Assert(
                            false,
                            "Only one of the specified parameter sets should be called.");
                        break;
                }
            }
        }

        #endregion command code
    }
}

