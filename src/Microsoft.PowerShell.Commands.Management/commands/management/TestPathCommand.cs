// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The valid values for the -PathType parameter for test-path.
    /// </summary>
    public enum TestPathType
    {
        /// <summary>
        /// If the item at the path exists, true will be returned.
        /// </summary>
        Any,

        /// <summary>
        /// If the item at the path exists and is a container, true will be returned.
        /// </summary>
        Container,

        /// <summary>
        /// If the item at the path exists and is not a container, true will be returned.
        /// </summary>
        Leaf
    }

    /// <summary>
    /// A command to determine if an item exists at a specified path.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "Path", DefaultParameterSetName = "Path", SupportsTransactions = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097057")]
    [OutputType(typeof(bool))]
    public class TestPathCommand : CoreCommandWithCredentialsBase
    {
        #region Parameters

        /// <summary>
        /// Gets or sets the path parameter to the command.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = "Path",
                    Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [AllowNull]
        [AllowEmptyCollection]
        [AllowEmptyString]
        public string[] Path
        {
            get { return _paths; }

            set { _paths = value; }
        }

        /// <summary>
        /// Gets or sets the literal path parameter to the command.
        /// </summary>
        [Parameter(ParameterSetName = "LiteralPath",
                   Mandatory = true, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        [AllowNull]
        [AllowEmptyCollection]
        [AllowEmptyString]
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
        [Parameter]
        public override string Filter
        {
            get { return base.Filter; }

            set { base.Filter = value; }
        }

        /// <summary>
        /// Gets or sets the include property.
        /// </summary>
        [Parameter]
        public override string[] Include
        {
            get { return base.Include; }

            set { base.Include = value; }
        }

        /// <summary>
        /// Gets or sets the exclude property.
        /// </summary>
        [Parameter]
        public override string[] Exclude
        {
            get { return base.Exclude; }

            set { base.Exclude = value; }
        }

        /// <summary>
        /// Gets or sets the isContainer property.
        /// </summary>
        [Parameter]
        [Alias("Type")]
        public TestPathType PathType { get; set; } = TestPathType.Any;

        /// <summary>
        /// Gets or sets the IsValid parameter.
        /// </summary>
        [Parameter]
        public SwitchParameter IsValid { get; set; } = new SwitchParameter();

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

            if (!IsValid)
            {
                if (Path != null && Path.Length > 0 && Path[0] != null)
                {
                    result = InvokeProvider.Item.ItemExistsDynamicParameters(Path[0], context);
                }
                else
                {
                    result = InvokeProvider.Item.ItemExistsDynamicParameters(".", context);
                }
            }

            return result;
        }

        #endregion Parameters

        #region parameter data

        /// <summary>
        /// The path to the item to ping.
        /// </summary>
        private string[] _paths;

        #endregion parameter data

        #region Command code

        /// <summary>
        /// Determines if an item at the specified path exists.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (_paths == null || _paths.Length == 0)
            {
                WriteError(new ErrorRecord(
                    new ArgumentNullException(TestPathResources.PathIsNullOrEmptyCollection),
                    "NullPathNotPermitted",
                    ErrorCategory.InvalidArgument,
                    Path));

                return;
            }

            CmdletProviderContext currentContext = CmdletProviderContext;

            foreach (string path in _paths)
            {
                bool result = false;

                if (string.IsNullOrWhiteSpace(path))
                {
                    if (path is null)
                    {
                        WriteError(new ErrorRecord(
                            new ArgumentNullException(TestPathResources.PathIsNullOrEmptyCollection),
                            "NullPathNotPermitted",
                            ErrorCategory.InvalidArgument,
                            Path));
                    }
                    else
                    {
                        WriteObject(result);
                    }
                    
                    continue;
                }

                try
                {
                    if (IsValid)
                    {
                        result = SessionState.Path.IsValid(path, currentContext);
                    }
                    else
                    {
                        result = InvokeProvider.Item.Exists(path, currentContext);

                        if (this.PathType == TestPathType.Container)
                        {
                            result &= InvokeProvider.Item.IsContainer(path, currentContext);
                        }
                        else if (this.PathType == TestPathType.Leaf)
                        {
                            result &= !InvokeProvider.Item.IsContainer(path, currentContext);
                        }
                    }
                }

                // Any of the known exceptions means the path does not exist.
                catch (PSNotSupportedException)
                {
                }
                catch (DriveNotFoundException)
                {
                }
                catch (ProviderNotFoundException)
                {
                }
                catch (ItemNotFoundException)
                {
                }

                WriteObject(result);
            }
        }
        #endregion Command code
    }
}
