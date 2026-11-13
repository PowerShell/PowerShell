// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A command to convert a drive qualified or provider qualified path to
    /// a provider internal path.
    /// </summary>
    [Cmdlet(VerbsData.Convert, "Path", DefaultParameterSetName = "Path", SupportsTransactions = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096588", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(string))]
    public class ConvertPathCommand : CoreCommandBase
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
        [Parameter(ParameterSetName = "LiteralPath",
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
        /// Gets or sets the force property.
        /// </summary>
        [Parameter]
        public override SwitchParameter Force
        {
            get => base.Force;
            set => base.Force = value;
        }

        #endregion Parameters

        #region parameter data

        /// <summary>
        /// The path(s) to the item(s) to convert.
        /// </summary>
        private string[] _paths;

        #endregion parameter data

        #region Command code

        /// <summary>
        /// Converts a drive qualified or provider qualified path to a provider
        /// internal path.
        /// </summary>
        protected override void ProcessRecord()
        {
            ProviderInfo provider = null;

            foreach (string path in Path)
            {
                try
                {
                    Collection<string> results =
                        SessionState.Path.GetResolvedProviderPathFromPSPath(
                                path,
                                CmdletProviderContext,
                                out provider);

                    WriteObject(results, true);
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
