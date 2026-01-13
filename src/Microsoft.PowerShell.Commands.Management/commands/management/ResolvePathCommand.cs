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
        [Parameter(ParameterSetName = "Path")]
        [Parameter(ParameterSetName = "LiteralPath")]
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

        /// <summary>
        /// Gets or sets the path the resolved relative path should be based off.
        /// </summary>
        [Parameter]
        public string RelativeBasePath
        {
            get
            {
                return _relativeBasePath;
            }

            set
            {
                _relativeBasePath = value;
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
        /// The path to resolve.
        /// </summary>
        private string[] _paths;

        private PSDriveInfo _relativeDrive;
        private string _relativeBasePath;

        #endregion parameter data

        #region Command code

        /// <summary>
        /// Finds the path and drive that should be used for relative path resolution
        /// represents.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (!string.IsNullOrEmpty(RelativeBasePath))
            {
                try
                {
                    _relativeBasePath = SessionState.Internal.Globber.GetProviderPath(RelativeBasePath, CmdletProviderContext, out _, out _relativeDrive);
                }
                catch (ProviderNotFoundException providerNotFound)
                {
                    ThrowTerminatingError(
                        new ErrorRecord(
                            providerNotFound.ErrorRecord,
                            providerNotFound));
                }
                catch (DriveNotFoundException driveNotFound)
                {
                    ThrowTerminatingError(
                        new ErrorRecord(
                            driveNotFound.ErrorRecord,
                            driveNotFound));
                }
                catch (ProviderInvocationException providerInvocation)
                {
                    ThrowTerminatingError(
                        new ErrorRecord(
                            providerInvocation.ErrorRecord,
                            providerInvocation));
                }
                catch (NotSupportedException notSupported)
                {
                    ThrowTerminatingError(
                        new ErrorRecord(notSupported, "ProviderIsNotNavigationCmdletProvider", ErrorCategory.InvalidArgument, RelativeBasePath));
                }
                catch (InvalidOperationException invalidOperation)
                {
                    ThrowTerminatingError(
                        new ErrorRecord(invalidOperation, "InvalidHomeLocation", ErrorCategory.InvalidOperation, RelativeBasePath));
                }

                return;
            }
            else if (_relative)
            {
                _relativeDrive = SessionState.Path.CurrentLocation.Drive;
                _relativeBasePath = SessionState.Path.CurrentLocation.ProviderPath;
            }
        }

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
                    if (MyInvocation.BoundParameters.ContainsKey("RelativeBasePath"))
                    {
                        // Pushing and popping the location is done because GetResolvedPSPathFromPSPath uses the current path to resolve relative paths.
                        // It's important that we pop the location before writing an object to the pipeline to avoid affecting downstream commands.
                        try
                        {
                            SessionState.Path.PushCurrentLocation(string.Empty);
                            _ = SessionState.Path.SetLocation(_relativeBasePath);
                            result = SessionState.Path.GetResolvedPSPathFromPSPath(path, CmdletProviderContext);
                        }
                        finally
                        {
                            _ = SessionState.Path.PopLocation(string.Empty);
                        }
                    }
                    else
                    {
                        result = SessionState.Path.GetResolvedPSPathFromPSPath(path, CmdletProviderContext);
                    }

                    if (_relative)
                    {
                        ReadOnlySpan<char> baseCache = null;
                        ReadOnlySpan<char> adjustedBaseCache = null;
                        foreach (PathInfo currentPath in result)
                        {
                            // When result path and base path is on different PSDrive
                            // (../)*path should not go beyond the root of base path
                            if (currentPath.Drive != _relativeDrive &&
                                _relativeDrive != null &&
                                !currentPath.ProviderPath.StartsWith(_relativeDrive.Root, StringComparison.OrdinalIgnoreCase))
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
                            string adjustedPath = SessionState.Path.NormalizeRelativePath(currentPath.Path, _relativeBasePath);

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
