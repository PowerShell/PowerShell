// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    /// <summary>
    /// Exposes the Cmdlet Family Provider's drives to the Cmdlet base class. The methods of this class
    /// get and set provider data in session state.
    /// </summary>
    public sealed class DriveManagementIntrinsics
    {
        #region Constructors

        /// <summary>
        /// Hide the default constructor since we always require an instance of SessionState.
        /// </summary>
        private DriveManagementIntrinsics()
        {
            Dbg.Diagnostics.Assert(
                false,
                "This constructor should never be called. Only the constructor that takes an instance of SessionState should be called.");
        }

        /// <summary>
        /// Constructs a Drive management facade.
        /// </summary>
        /// <param name="sessionState">
        /// The instance of session state that facade wraps.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="sessionState"/> is null.
        /// </exception>
        internal DriveManagementIntrinsics(SessionStateInternal sessionState)
        {
            if (sessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sessionState));
            }

            _sessionState = sessionState;
        }

        #endregion Constructors

        #region Public methods

        /// <summary>
        /// Gets the drive information for the current working drive.
        /// </summary>
        /// <remarks>
        /// This property is readonly. To set the current drive use the
        /// SetLocation method.
        /// </remarks>
        public PSDriveInfo Current
        {
            get
            {
                Dbg.Diagnostics.Assert(
                    _sessionState != null,
                    "The only constructor for this class should always set the sessionState field");

                return _sessionState.CurrentDrive;
            }
        }

        #region New

        /// <summary>
        /// Creates a new PSDrive in session state.
        /// </summary>
        /// <param name="drive">
        /// The drive to be created.
        /// </param>
        /// <param name="scope">
        /// The ID of the scope to create the drive in. This may be one of the scope
        /// keywords like global or local, or it may be an numeric offset of the scope
        /// generation relative to the current scope.
        /// If the scopeID is null or empty the local scope is used.
        /// </param>
        /// <returns>
        /// The drive that was created.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="drive"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the drive already exists,
        /// or
        /// If <paramref name="drive"/>.Name contains one or more invalid characters; ~ / \\ . :
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider is not a DriveCmdletProvider.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// The provider for the <paramref name="drive"/> could not be found.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception or returned null.
        /// </exception>
        public PSDriveInfo New(PSDriveInfo drive, string scope)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.NewDrive(drive, scope);
        }

        /// <summary>
        /// Creates a new MSH drive in session state.
        /// </summary>
        /// <param name="drive">
        /// The drive to be created.
        /// </param>
        /// <param name="scope">
        /// The ID of the scope to create the drive in. This may be one of the scope
        /// keywords like global or local, or it may be an numeric offset of the scope
        /// generation relative to the current scope.
        /// If the scopeID is null or empty the local scope is used.
        /// </param>
        /// <param name="context">
        /// The context under which this command is running.
        /// </param>
        /// <returns>
        /// Nothing. The drive that is created is written to the context.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="drive"/> or <paramref name="context"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the drive already exists
        /// or
        /// If <paramref name="drive"/>.Name contains one or more invalid characters; ~ / \\ . :
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider is not a DriveCmdletProvider.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// The provider for the <paramref name="drive"/> could not be found.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception or returned null.
        /// </exception>
        internal void New(
            PSDriveInfo drive,
            string scope,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.NewDrive(drive, scope, context);
        }

        /// <summary>
        /// Gets an object that defines the additional parameters for the NewDrive implementation
        /// for a provider.
        /// </summary>
        /// <param name="providerId">
        /// The provider ID for the drive that is being created.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// If the <paramref name="providerId"/> is not a DriveCmdletProvider.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If <paramref name="providerId"/> does not exist.
        /// </exception>
        internal object NewDriveDynamicParameters(
            string providerId,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.NewDriveDynamicParameters(providerId, context);
        }

        #endregion New

        #region Remove

        /// <summary>
        /// Removes the specified drive.
        /// </summary>
        /// <param name="driveName">
        /// The name of the drive to be removed.
        /// </param>
        /// <param name="force">
        /// Determines whether drive should be forcefully removed even if there was errors.
        /// </param>
        /// <param name="scope">
        /// The ID of the scope to remove the drive from. This may be one of the scope
        /// keywords like global or local, or it may be an numeric offset of the scope
        /// generation relative to the current scope.
        /// If the scopeID is null or empty the local scope is used.
        /// </param>
        public void Remove(string driveName, bool force, string scope)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.RemoveDrive(driveName, force, scope);
        }

        /// <summary>
        /// Removes the specified drive.
        /// </summary>
        /// <param name="driveName">
        /// The name of the drive to be removed.
        /// </param>
        /// <param name="force">
        /// Determines whether drive should be forcefully removed even if there was errors.
        /// </param>
        /// <param name="scope">
        /// The ID of the scope to remove the drive from. This may be one of the scope
        /// keywords like global or local, or it may be an numeric offset of the scope
        /// generation relative to the current scope.
        /// If the scopeID is null or empty the local scope is used.
        /// </param>
        /// <param name="context">
        /// The context under which this command is running.
        /// </param>
        internal void Remove(
            string driveName,
            bool force,
            string scope,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.RemoveDrive(driveName, force, scope, context);
        }

        #endregion Remove

        #region Get

        /// <summary>
        /// Gets the drive information for the drive specified by name.
        /// </summary>
        /// <param name="driveName">
        /// The name of the drive to get the drive information for.
        /// </param>
        /// <returns>
        /// The drive information that represents the drive of the specified name.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="driveName"/> is null.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If there is no drive with <paramref name="driveName"/>.
        /// </exception>
        public PSDriveInfo Get(string driveName)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetDrive(driveName);
        }

        /// <summary>
        /// Gets the drive information for the drive specified by name.
        /// </summary>
        /// <param name="driveName">
        /// The name of the drive to get the drive information for.
        /// </param>
        /// <param name="scope">
        /// The ID of the scope to get the drive from. This may be one of the scope
        /// keywords like global or local, or it may be an numeric offset of the scope
        /// generation relative to the current scope.
        /// If the scopeID is null or empty the local scope is used.
        /// </param>
        /// <returns>
        /// The drive information that represents the drive of the specified name.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="driveName"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="scope"/> is less than zero, or not
        /// a number and not "script", "global", "local", or "private"
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        public PSDriveInfo GetAtScope(string driveName, string scope)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetDrive(driveName, scope);
        }

        /// <summary>
        /// Retrieves all the drives in the specified scope.
        /// </summary>
        public Collection<PSDriveInfo> GetAll()
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            return _sessionState.Drives(null);
        }

        /// <summary>
        /// Retrieves all the drives in the specified scope.
        /// </summary>
        /// <param name="scope">
        /// The scope to retrieve the drives from. If null, the
        /// drives in all the scopes will be returned.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="scope"/> is less than zero, or not
        /// a number and not "script", "global", "local", or "private"
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        public Collection<PSDriveInfo> GetAllAtScope(string scope)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            return _sessionState.Drives(scope);
        }

        /// <summary>
        /// Gets all the drives for the specified provider.
        /// </summary>
        /// <param name="providerName">
        /// The name of the provider to get the drives for.
        /// </param>
        /// <returns>
        /// All the drives in all the scopes for the given provider.
        /// </returns>
        public Collection<PSDriveInfo> GetAllForProvider(string providerName)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetDrivesForProvider(providerName);
        }

        #endregion GetDrive

        #endregion Public methods

        #region private data

        // A private reference to the internal session state of the engine.
        private readonly SessionStateInternal _sessionState;

        #endregion private data
    }
}
