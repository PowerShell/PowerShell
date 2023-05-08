// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    /// <summary>
    /// An object that represents a path.
    /// </summary>
    public sealed class PathInfo
    {
        /// <summary>
        /// Gets the drive that contains the path.
        /// </summary>
        public PSDriveInfo Drive
        {
            get
            {
                PSDriveInfo result = null;

                if (_drive != null &&
                    !_drive.Hidden)
                {
                    result = _drive;
                }

                return result;
            }
        }

        /// <summary>
        /// Gets the provider that contains the path.
        /// </summary>
        public ProviderInfo Provider
        {
            get
            {
                return _provider;
            }
        }

        /// <summary>
        /// This is the internal mechanism to get the hidden drive.
        /// </summary>
        /// <returns>
        /// The drive associated with this PathInfo.
        /// </returns>
        internal PSDriveInfo GetDrive()
        {
            return _drive;
        }

        /// <summary>
        /// Gets the provider internal path for the PSPath that this PathInfo represents.
        /// </summary>
        /// <exception cref="ProviderInvocationException">
        /// The provider encountered an error when resolving the path.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The path was a home relative path but the home path was not
        /// set for the provider.
        /// </exception>
        public string ProviderPath
        {
            get
            {
                if (_providerPath == null)
                {
                    // Construct the providerPath

                    LocationGlobber pathGlobber = _sessionState.Internal.ExecutionContext.LocationGlobber;
                    _providerPath = pathGlobber.GetProviderPath(Path);
                }

                return _providerPath;
            }
        }

        private string _providerPath;
        private readonly SessionState _sessionState;

        /// <summary>
        /// Gets the PowerShell path that this object represents.
        /// </summary>
        public string Path
        {
            get
            {
                return this.ToString();
            }
        }

        private readonly PSDriveInfo _drive;
        private readonly ProviderInfo _provider;
        private readonly string _path = string.Empty;

        /// <summary>
        /// Gets a string representing the PowerShell path.
        /// </summary>
        /// <returns>
        /// A string representing the PowerShell path.
        /// </returns>
        public override string ToString()
        {
            string result = _path;

            if (_drive == null ||
                _drive.Hidden)
            {
                // For hidden drives just return the current location
                result =
                    LocationGlobber.GetProviderQualifiedPath(
                        _path,
                        _provider);
            }
            else
            {
                result = LocationGlobber.GetDriveQualifiedPath(_path, _drive);
            }

            return result;
        }

        /// <summary>
        /// The constructor of the PathInfo object.
        /// </summary>
        /// <param name="drive">
        /// The drive that contains the path
        /// </param>
        /// <param name="provider">
        /// The provider that contains the path.
        /// </param>
        /// <param name="path">
        /// The path this object represents.
        /// </param>
        /// <param name="sessionState">
        /// The session state associated with the drive, provider, and path information.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="drive"/>, <paramref name="provider"/>,
        /// <paramref name="path"/>, or <paramref name="sessionState"/> is null.
        /// </exception>
        internal PathInfo(PSDriveInfo drive, ProviderInfo provider, string path, SessionState sessionState)
        {
            if (provider == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(provider));
            }

            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            if (sessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sessionState));
            }

            _drive = drive;
            _provider = provider;
            _path = path;
            _sessionState = sessionState;
        }
    }
}
