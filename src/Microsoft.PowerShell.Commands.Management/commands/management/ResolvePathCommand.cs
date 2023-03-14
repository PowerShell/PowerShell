// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A command to resolve PowerShell paths containing glob characters to
    /// PowerShell paths that match the glob strings.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Resolve, "Path", DefaultParameterSetName = "Path", SupportsTransactions = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097143")]
    public class ResolvePathCommand : CoreCommandWithCredentialsBase
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
        /// Gets or sets the value that determines if the resolved path should
        /// be resolved to its relative version.
        /// </summary>
        [Parameter()]
        public SwitchParameter Relative
        {
            get
            {
                return _relative;
            }

            set
            {
                _relative = value;
            }
        }

        private SwitchParameter _relative;

        #endregion Parameters

        #region parameter data

        /// <summary>
        /// The path to resolve.
        /// </summary>
        private string[] _paths;

        #endregion parameter data

        #region Command code

        /// <summary>
        /// Resolves the path containing glob characters to the PowerShell paths that it
        /// represents.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (string path in Path)
            {
                Collection<PathInfo> result = null;
                try
                {
                    result = SessionState.Path.GetResolvedPSPathFromPSPath(path, CmdletProviderContext);

                    if (_relative)
                    {
                        ReadOnlySpan<char> baseCache = null;
                        ReadOnlySpan<char> adjustedBaseCache = null;
                        foreach (PathInfo currentPath in result)
                        {
                            // When result path and base path is on different PSDrive
                            // (../)*path should not go beyond the root of base path
                            if (currentPath.Drive != SessionState.Path.CurrentLocation.Drive &&
                                SessionState.Path.CurrentLocation.Drive != null &&
                                !currentPath.ProviderPath.StartsWith(
                                    SessionState.Path.CurrentLocation.Drive.Root, StringComparison.OrdinalIgnoreCase))
                            {
                                WriteObject(currentPath.Path, enumerateCollection: false);
                                continue;
                            }

                            int leafIndex = currentPath.Path.LastIndexOf(currentPath.Provider.ItemSeparator);
                            var basePath = currentPath.Path.AsSpan(0, leafIndex);
                            if (basePath == baseCache)
                            {
                                WriteObject(string.Concat(adjustedBaseCache, currentPath.Path.AsSpan(leafIndex + 1)), enumerateCollection: false);
                                continue;
                            }

                            baseCache = basePath;
                            string adjustedPath = SessionState.Path.NormalizeRelativePath(currentPath.Path,
                                SessionState.Path.CurrentLocation.ProviderPath);

                            // Do not insert './' if result path is not relative
                            if (!adjustedPath.StartsWith(
                                    currentPath.Drive?.Root ?? currentPath.Path, StringComparison.OrdinalIgnoreCase) &&
                                !adjustedPath.StartsWith('.'))
                            {
                                adjustedPath = SessionState.Path.Combine(".", adjustedPath);
                            }

                            leafIndex = adjustedPath.LastIndexOf(currentPath.Provider.ItemSeparator);
                            adjustedBaseCache = adjustedPath.AsSpan(0, leafIndex + 1);

                            WriteObject(adjustedPath, enumerateCollection: false);
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

                if (!_relative)
                {
                    WriteObject(result, enumerateCollection: true);
                }
            }
        }
        #endregion Command code

    }
}
