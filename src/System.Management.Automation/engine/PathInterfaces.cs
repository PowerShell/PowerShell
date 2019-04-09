// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    /// <summary>
    /// Exposes the path manipulation and location APIs to the Cmdlet base class.
    /// </summary>
    public sealed class PathIntrinsics
    {
        #region Constructors

        /// <summary>
        /// Hide the default constructor since we always require an instance of SessionState.
        /// </summary>
        private PathIntrinsics()
        {
            Dbg.Diagnostics.Assert(
                false,
                "This constructor should never be called. Only the constructor that takes an instance of SessionState should be called.");
        }

        /// <summary>
        /// Internal constructor for the PathIntrinsics facade.
        /// </summary>
        /// <param name="sessionState">
        /// The session for which this is a facade.
        /// </param>
        /// <remarks>
        /// This is only public for testing purposes.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="sessionState"/> is null.
        /// </exception>
        internal PathIntrinsics(SessionStateInternal sessionState)
        {
            if (sessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException("sessionState");
            }

            _sessionState = sessionState;
        }

        #endregion Constructors

        #region Public methods

        /// <summary>
        /// Gets the current location.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// If a location has not been set yet.
        /// </exception>
        public PathInfo CurrentLocation
        {
            get
            {
                Dbg.Diagnostics.Assert(
                    _sessionState != null,
                    "The only constructor for this class should always set the sessionState field");

                return _sessionState.CurrentLocation;
            }
        }

        /// <summary>
        /// Gets the current location for a specific provider.
        /// </summary>
        /// <param name="providerName">
        /// The name of the provider to get the current location for.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="providerName"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If <paramref name="namespacesID"/> refers to a provider that does not exist.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If a current drive cannot be found for the provider <paramref name="providerName"/>
        /// </exception>
        public PathInfo CurrentProviderLocation(string providerName)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetNamespaceCurrentLocation(providerName);
        }

        /// <summary>
        /// Gets the current location for the file system provider.
        /// </summary>
        /// <exception cref="DriveNotFoundException">
        /// If a current drive cannot be found for the FileSystem provider
        /// </exception>
        public PathInfo CurrentFileSystemLocation
        {
            get
            {
                Dbg.Diagnostics.Assert(
                    _sessionState != null,
                    "The only constructor for this class should always set the sessionState field");

                return CurrentProviderLocation(_sessionState.ExecutionContext.ProviderNames.FileSystem);
            }
        }

        /// <summary>
        /// Changes the current location to the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to change the location to. This can be either a drive-relative or provider-relative
        /// path. It cannot be a provider-internal path.
        /// </param>
        /// <returns>
        /// The path of the new current location.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="path"/> does not exist, is not a container, or
        /// resolved to multiple containers.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If <paramref name="path"/> refers to a provider that does not exist.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If <paramref name="path"/> refers to a drive that does not exist.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider associated with <paramref name="path"/> threw an
        /// exception.
        /// </exception>
        public PathInfo SetLocation(string path)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.SetLocation(path);
        }

        /// <summary>
        /// Changes the current location to the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to change the location to. This can be either a drive-relative or provider-relative
        /// path. It cannot be a provider-internal path.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// The path of the new current location.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="path"/> does not exist, is not a container, or
        /// resolved to multiple containers.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If <paramref name="path"/> refers to a provider that does not exist.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If <paramref name="path"/> refers to a drive that does not exist.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider associated with <paramref name="path"/> threw an
        /// exception.
        /// </exception>
        internal PathInfo SetLocation(string path, CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.SetLocation(path, context);
        }

        /// <summary>
        /// Changes the current location to the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to change the location to. This can be either a drive-relative or provider-relative
        /// path. It cannot be a provider-internal path.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <param name="literalPath">
        /// Indicates if the path is a literal path.
        /// </param>
        /// <returns>
        /// The path of the new current location.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="path"/> does not exist, is not a container, or
        /// resolved to multiple containers.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If <paramref name="path"/> refers to a provider that does not exist.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If <paramref name="path"/> refers to a drive that does not exist.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider associated with <paramref name="path"/> threw an
        /// exception.
        /// </exception>
        internal PathInfo SetLocation(string path, CmdletProviderContext context, bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.SetLocation(path, context, literalPath);
        }

        /// <summary>
        /// Determines if the specified path is the current location or a parent of the current location.
        /// </summary>
        /// <param name="path">
        /// A drive or provider-qualified path to be compared against the current location.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// True if the path is the current location or a parent of the current location. False otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the path is a provider-qualified path for a provider that is
        /// not loaded into the system.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider used to build the path threw an exception.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> represents is not a NavigationCmdletProvider
        /// or ContainerCmdletProvider.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// If the <paramref name="path"/> starts with "~" and the home location is not set for
        /// the provider.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider specified by <paramref name="providerId"/> threw an
        /// exception when its GetParentPath or MakePath was called while
        /// processing the <paramref name="path"/>.
        /// </exception>
        internal bool IsCurrentLocationOrAncestor(string path, CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.IsCurrentLocationOrAncestor(path, context);
        }

        /// <summary>
        /// Pushes the current location onto the location stack so that it can be retrieved later.
        /// </summary>
        /// <param name="stackName">
        /// The ID of the stack to push the location onto.
        /// </param>
        public void PushCurrentLocation(string stackName)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            _sessionState.PushCurrentLocation(stackName);
        }

        /// <summary>
        /// Gets the location off the top of the location stack.
        /// </summary>
        /// <param name="stackName">
        /// The ID of the stack to pop the location from. If stackName is null or empty
        /// the default stack is used.
        /// </param>
        /// <returns>
        /// The path information for the location that was on the top of the location stack.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// If the path on the stack does not exist, is not a container, or
        /// resolved to multiple containers.
        /// or
        /// If <paramref name="stackName"/> contains wildcard characters and resolves
        /// to multiple location stacks.
        /// or
        /// A stack was not found with the specified name.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the path on the stack refers to a provider that does not exist.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the path on the stack refers to a drive that does not exist.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider associated with the path on the stack threw an
        /// exception.
        /// </exception>
        public PathInfo PopLocation(string stackName)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            return _sessionState.PopLocation(stackName);
        }

        /// <summary>
        /// Gets the location stack and all the locations on it.
        /// </summary>
        /// <param name="stackName">
        /// The stack ID of the stack to get the stack info for.
        /// </param>
        public PathInfoStack LocationStack(string stackName)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            return _sessionState.LocationStack(stackName);
        }

        /// <summary>
        /// Sets the default location stack to that specified by the stack ID.
        /// </summary>
        /// <param name="stackName">
        /// The stack ID of the stack to use as the default location stack.
        /// </param>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="stackName"/> does not exist as a location stack.
        /// </exception>
        public PathInfoStack SetDefaultLocationStack(string stackName)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            return _sessionState.SetDefaultLocationStack(stackName);
        }

        /// <summary>
        /// Resolves a drive or provider qualified absolute or relative path that may contain
        /// wildcard characters into one or more absolute drive or provider qualified paths.
        /// </summary>
        /// <param name="path">
        /// The drive or provider qualified path to be resolved. This path may contain wildcard
        /// characters which will get resolved.
        /// </param>
        /// <returns>
        /// An array of Msh paths that resolved from the given path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If <paramref name="path"/> is a provider-qualified path
        /// and the specified provider does not exist.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If <paramref name="path"/> is a drive-qualified path and
        /// the specified drive does not exist.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider throws an exception when its MakePath gets
        /// called.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider does not support multiple items.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// If the home location for the provider is not set and
        /// <paramref name="path"/> starts with a "~".
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain wildcard characters and
        /// could not be found.
        /// </exception>
        public Collection<PathInfo> GetResolvedPSPathFromPSPath(string path)
        {
            // The parameters will be verified by the path resolver
            Provider.CmdletProvider providerInstance = null;
            return PathResolver.GetGlobbedMonadPathsFromMonadPath(path, false, out providerInstance);
        }

        /// <summary>
        /// Resolves a drive or provider qualified absolute or relative path that may contain
        /// wildcard characters into one or more absolute drive or provider qualified paths.
        /// </summary>
        /// <param name="path">
        /// The drive or provider qualified path to be resolved. This path may contain wildcard
        /// characters which will get resolved.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// An array of Msh paths that resolved from the given path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="context"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If <paramref name="path"/> is a provider-qualified path
        /// and the specified provider does not exist.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider throws an exception when its MakePath gets
        /// called.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider does not support multiple items.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// If the home location for the provider is not set and
        /// <paramref name="path"/> starts with a "~".
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain wildcard characters and
        /// could not be found.
        /// </exception>
        internal Collection<PathInfo> GetResolvedPSPathFromPSPath(
            string path,
            CmdletProviderContext context)
        {
            // The parameters will be verified by the path resolver
            Provider.CmdletProvider providerInstance = null;
            return PathResolver.GetGlobbedMonadPathsFromMonadPath(path, false, context, out providerInstance);
        }

        /// <summary>
        /// Resolves a drive or provider qualified absolute or relative path that may contain
        /// wildcard characters into one or more provider-internal paths.
        /// </summary>
        /// <param name="path">
        /// The drive or provider qualified path to be resolved. This path may contain wildcard
        /// characters which will get resolved.
        /// </param>
        /// <param name="provider">
        /// The provider for which the returned paths should be used.
        /// </param>
        /// <returns>
        /// An array of provider-internal paths that resolved from the given path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the path is a provider-qualified path for a provider that is
        /// not loaded into the system.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider used to build the path threw an exception.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> represents is not a NavigationCmdletProvider
        /// or ContainerCmdletProvider.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// If the <paramref name="path"/> starts with "~" and the home location is not set for
        /// the provider.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider associated with the <paramref name="path"/> threw an
        /// exception when building its path.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain wildcard characters and
        /// could not be found.
        /// </exception>
        public Collection<string> GetResolvedProviderPathFromPSPath(
            string path,
            out ProviderInfo provider)
        {
            // The parameters will be verified by the path resolver
            Provider.CmdletProvider providerInstance = null;
            return PathResolver.GetGlobbedProviderPathsFromMonadPath(path, false, out provider, out providerInstance);
        }

        internal Collection<string> GetResolvedProviderPathFromPSPath(
            string path,
            bool allowNonexistingPaths,
            out ProviderInfo provider)
        {
            // The parameters will be verified by the path resolver
            Provider.CmdletProvider providerInstance = null;
            return PathResolver.GetGlobbedProviderPathsFromMonadPath(path, allowNonexistingPaths, out provider, out providerInstance);
        }

        /// <summary>
        /// Resolves a drive or provider qualified absolute or relative path that may contain
        /// wildcard characters into one or more provider-internal paths.
        /// </summary>
        /// <param name="path">
        /// The drive or provider qualified path to be resolved. This path may contain wildcard
        /// characters which will get resolved.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <param name="provider">
        /// The provider for which the returned paths should be used.
        /// </param>
        /// <returns>
        /// An array of provider-internal paths that resolved from the given path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="context"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the path is a provider-qualified path for a provider that is
        /// not loaded into the system.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider used to build the path threw an exception.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> represents is not a NavigationCmdletProvider
        /// or ContainerCmdletProvider.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// If the <paramref name="path"/> starts with "~" and the home location is not set for
        /// the provider.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider associated with the <paramref name="path"/> threw an
        /// exception when its GetParentPath or MakePath was called while
        /// processing the <paramref name="path"/>.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain wildcard characters and
        /// could not be found.
        /// </exception>
        internal Collection<string> GetResolvedProviderPathFromPSPath(
            string path,
            CmdletProviderContext context,
            out ProviderInfo provider)
        {
            // The parameters will be verified by the path resolver

            Provider.CmdletProvider providerInstance = null;
            return PathResolver.GetGlobbedProviderPathsFromMonadPath(path, false, context, out provider, out providerInstance);
        }

        /// <summary>
        /// Resolves a drive or provider qualified absolute or relative path that may contain
        /// wildcard characters into one or more provider-internal paths.
        /// </summary>
        /// <param name="path">
        /// The drive or provider qualified path to be resolved. This path may contain wildcard
        /// characters which will get resolved.
        /// </param>
        /// <param name="providerId">
        /// The provider for which the returned paths should be used.
        /// </param>
        /// <returns>
        /// An array of provider-internal paths that resolved from the given path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If <paramref name="providerId"/> references a provider that does not exist.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the <paramref name="providerId"/> references a provider that is not
        /// a ContainerCmdletProvider.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider used to build the path threw an exception.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// If the <paramref name="path"/> starts with "~" and the home location is not set for
        /// the provider.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain wildcard characters and
        /// could not be found.
        /// </exception>
        public Collection<string> GetResolvedProviderPathFromProviderPath(
            string path,
            string providerId)
        {
            // The parameters will be verified by the path resolver
            Provider.CmdletProvider providerInstance = null;
            return PathResolver.GetGlobbedProviderPathsFromProviderPath(path, false, providerId, out providerInstance);
        }

        /// <summary>
        /// Resolves a drive or provider qualified absolute or relative path that may contain
        /// wildcard characters into one or more provider-internal paths.
        /// </summary>
        /// <param name="path">
        /// The drive or provider qualified path to be resolved. This path may contain wildcard
        /// characters which will get resolved.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <param name="providerId">
        /// The provider for which the returned paths should be used.
        /// </param>
        /// <returns>
        /// An array of provider-internal paths that resolved from the given path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/>, <paramref name="providerId"/>, or
        /// <paramref name="context"/> is null.
        ///  </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If <paramref name="providerId"/> references a provider that does not exist.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the <paramref name="providerId"/> references a provider that is not
        /// a ContainerCmdletProvider.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider used to build the path threw an exception.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// If the <paramref name="path"/> starts with "~" and the home location is not set for
        /// the provider.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain wildcard characters and
        /// could not be found.
        /// </exception>
        internal Collection<string> GetResolvedProviderPathFromProviderPath(
            string path,
            string providerId,
            CmdletProviderContext context)
        {
            // The parameters will be verified by the path resolver

            Provider.CmdletProvider providerInstance = null;
            return PathResolver.GetGlobbedProviderPathsFromProviderPath(path, false, providerId, context, out providerInstance);
        }

        /// <summary>
        /// Converts a drive or provider qualified absolute or relative path that may contain
        /// wildcard characters into one a provider-internal path still containing the wildcard characters.
        /// </summary>
        /// <param name="path">
        /// The drive or provider qualified path to be converted. This path may contain wildcard
        /// characters which will not get resolved.
        /// </param>
        /// <returns>
        /// A provider-internal path that does not have the wildcard characters resolved.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the path is a provider-qualified path for a provider that is
        /// not loaded into the system.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider used to build the path threw an exception.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> represents is not a NavigationCmdletProvider
        /// or ContainerCmdletProvider.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// If the <paramref name="path"/> starts with "~" and the home location is not set for
        /// the provider.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider specified by <paramref name="path"/> threw an
        /// exception.
        /// </exception>
        public string GetUnresolvedProviderPathFromPSPath(string path)
        {
            // The parameters will be verified by the path resolver

            return PathResolver.GetProviderPath(path);
        }

        /// <summary>
        /// Converts a drive or provider qualified absolute or relative path that may contain
        /// wildcard characters into one a provider-internal path still containing the wildcard characters.
        /// </summary>
        /// <param name="path">
        /// The drive or provider qualified path to be converted. This path may contain wildcard
        /// characters which will not get resolved.
        /// </param>
        /// <param name="provider">
        /// The information for the provider for which the returned path should be used.
        /// </param>
        /// <param name="drive">
        /// The drive of the Msh path that was used to convert the path. Note, this may be null
        /// if the <paramref name="path"/> was a provider-qualified path.
        /// </param>
        /// <returns>
        /// A provider-internal path that does not have the wildcard characters resolved.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="context"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the path is a provider-qualified path for a provider that is
        /// not loaded into the system.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider used to build the path threw an exception.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> represents is not a NavigationCmdletProvider
        /// or ContainerCmdletProvider.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// If the <paramref name="path"/> starts with "~" and the home location is not set for
        /// the provider.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider specified by <paramref name="provider"/> threw an
        /// exception when its GetParentPath or MakePath was called while
        /// processing the <paramref name="path"/>.
        /// </exception>
        public string GetUnresolvedProviderPathFromPSPath(
            string path,
            out ProviderInfo provider,
            out PSDriveInfo drive)
        {
            CmdletProviderContext context = new CmdletProviderContext(_sessionState.ExecutionContext);

            // The parameters will be verified by the path resolver

            string result = PathResolver.GetProviderPath(path, context, out provider, out drive);

            context.ThrowFirstErrorOrDoNothing();

            return result;
        }

        /// <summary>
        /// Converts a drive or provider qualified absolute or relative path that may contain
        /// wildcard characters into one a provider-internal path still containing the wildcard characters.
        /// </summary>
        /// <param name="path">
        /// The drive or provider qualified path to be converted. This path may contain wildcard
        /// characters which will not get resolved.
        /// </param>
        /// <param name="context">
        /// The context under which this command is running.
        /// </param>
        /// <param name="provider">
        /// The information for the provider for which the returned path should be used.
        /// </param>
        /// <param name="drive">
        /// The drive of the Msh path that was used to convert the path.
        /// </param>
        /// <returns>
        /// A provider-internal path that does not have the wildcard characters resolved.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="context"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the path is a provider-qualified path for a provider that is
        /// not loaded into the system.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider used to build the path threw an exception.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> represents is not a NavigationCmdletProvider
        /// or ContainerCmdletProvider.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// If the <paramref name="path"/> starts with "~" and the home location is not set for
        /// the provider.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider specified by <paramref name="provider"/> threw an
        /// exception when its GetParentPath or MakePath was called while
        /// processing the <paramref name="path"/>.
        /// </exception>
        internal string GetUnresolvedProviderPathFromPSPath(
            string path,
            CmdletProviderContext context,
            out ProviderInfo provider,
            out PSDriveInfo drive)
        {
            // The parameters will be verified by the path resolver

            return PathResolver.GetProviderPath(path, context, out provider, out drive);
        }

        /// <summary>
        /// Determines if the give path is an Msh provider-qualified path.
        /// </summary>
        /// <param name="path">
        /// The path to check.
        /// </param>
        /// <returns>
        /// True if the specified path is provider-qualified, false otherwise.
        /// </returns>
        /// <remarks>
        /// A provider-qualified path is a path in the following form:
        /// providerId::provider-internal-path
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        public bool IsProviderQualified(string path)
        {
            // The parameters will be verified by the path resolver

            return LocationGlobber.IsProviderQualifiedPath(path);
        }

        /// <summary>
        /// Determines if the given path is a drive-qualified absolute path.
        /// </summary>
        /// <param name="path">
        /// The path to check.
        /// </param>
        /// <param name="driveName">
        /// If the path is an Msh absolute path then the returned value is
        /// the name of the drive that the path is absolute to.
        /// </param>
        /// <returns>
        /// True if the specified path is an Msh absolute drive-qualified path.
        /// False otherwise.
        /// </returns>
        /// <remarks>
        /// A path is an absolute drive-qualified path if it has the following
        /// form:
        /// drive-name:drive-relative-path
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        public bool IsPSAbsolute(string path, out string driveName)
        {
            // The parameters will be verified by the path resolver

            return PathResolver.IsAbsolutePath(path, out driveName);
        }

        #region Combine

        /// <summary>
        /// Combines two strings with a provider specific path separator.
        /// </summary>
        /// <param name="parent">
        /// The parent path to be joined with the child.
        /// </param>
        /// <param name="child">
        /// The child path to be joined with the parent.
        /// </param>
        /// <returns>
        /// The combined path of the parent and child with the provider
        /// specific path separator between them.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="context"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If both <paramref name="parent"/> and <paramref name="child"/> is null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the <paramref name="providerId"/> does not support this operation.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// If the pipeline is being stopped while executing the command.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public string Combine(string parent, string child)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.MakePath(parent, child);
        }

        /// <summary>
        /// Combines two strings with a provider specific path separator.
        /// </summary>
        /// <param name="parent">
        /// The parent path to be joined with the child.
        /// </param>
        /// <param name="child">
        /// The child path to be joined with the parent.
        /// </param>
        /// <param name="context">
        /// The context under which this command is running.
        /// </param>
        /// <returns>
        /// The combined path of the parent and child with the provider
        /// specific path separator between them.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="context"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If both <paramref name="parent"/> and <paramref name="child"/> is null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the <paramref name="providerId"/> does not support this operation.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// If the pipeline is being stopped while executing the command.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal string Combine(string parent, string child, CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.MakePath(parent, child, context);
        }

        #endregion Combine

        #region ParseParent

        /// <summary>
        /// Gets the parent path of the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to get the parent path from.
        /// </param>
        /// <param name="root">
        /// If the root is specified the path returned will not be any higher than the root.
        /// </param>
        /// <returns>
        /// The parent path of the specified path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the <paramref name="providerInstance"/> does not support this operation.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// If the pipeline is being stopped while executing the command.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public string ParseParent(string path, string root)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetParentPath(path, root);
        }

        /// <summary>
        /// Gets the parent path of the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to get the parent path from.
        /// </param>
        /// <param name="root">
        /// If the root is specified the path returned will not be any higher than the root.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// The parent path of the specified path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the <paramref name="providerInstance"/> does not support this operation.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// If the pipeline is being stopped while executing the command.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal string ParseParent(
            string path,
            string root,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetParentPath(path, root, context, false);
        }

        /// <summary>
        /// Gets the parent path of the specified path.
        /// Allow to use FileSystem as the default provider when the
        /// given path is drive-qualified and the drive cannot be found.
        /// </summary>
        /// <param name="path">
        /// The path to get the parent path from.
        /// </param>
        /// <param name="root">
        /// If the root is specified the path returned will not be any higher than the root.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <param name="useDefaultProvider">
        /// to use default provider when needed.
        /// </param>
        /// <returns>
        /// The parent path of the specified path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the <paramref name="providerInstance"/> does not support this operation.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// If the pipeline is being stopped while executing the command.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal string ParseParent(
            string path,
            string root,
            CmdletProviderContext context,
            bool useDefaultProvider)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetParentPath(path, root, context, useDefaultProvider);
        }

        #endregion ParseParent

        #region ParseChildName

        /// <summary>
        /// Gets the child name of the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to get the child name from.
        /// </param>
        /// <returns>
        /// The last element of the path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public string ParseChildName(string path)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetChildName(path);
        }

        /// <summary>
        /// Gets the child name of the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to get the child name from.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// The last element of the path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal string ParseChildName(
            string path,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetChildName(path, context, false);
        }

        /// <summary>
        /// Gets the child name of the specified path.
        /// Allow to use FileSystem as the default provider when the
        /// given path is drive-qualified and the drive cannot be found.
        /// </summary>
        /// <param name="path">
        /// The path to get the child name from.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <param name="useDefaultProvider">
        /// to use default provider when needed.
        /// </param>
        /// <returns>
        /// The last element of the path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal string ParseChildName(
            string path,
            CmdletProviderContext context,
            bool useDefaultProvider)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetChildName(path, context, useDefaultProvider);
        }

        #endregion ParseChildName

        #region NormalizeRelativePath

        /// <summary>
        /// Normalizes the path that was passed in and returns the normalized path
        /// as a relative path to the basePath that was passed.
        /// </summary>
        /// <param name="path">
        /// An MSH path to an item. The item should exist
        /// or the provider should write out an error.
        /// </param>
        /// <param name="basePath">
        /// The path that the return value should be relative to.
        /// </param>
        /// <returns>
        /// A normalized path that is relative to the basePath that was passed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the <paramref name="providerInstance"/> does not support this operation.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// If the pipeline is being stopped while executing the command.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public string NormalizeRelativePath(string path, string basePath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.NormalizeRelativePath(path, basePath);
        }

        /// <summary>
        /// Normalizes the path that was passed in and returns the normalized path
        /// as a relative path to the basePath that was passed.
        /// </summary>
        /// <param name="path">
        /// An MSH path to an item. The item should exist
        /// or the provider should write out an error.
        /// </param>
        /// <param name="basePath">
        /// The path that the return value should be relative to.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// A normalized path that is relative to the basePath that was passed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the <paramref name="providerInstance"/> does not support this operation.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// If the pipeline is being stopped while executing the command.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal string NormalizeRelativePath(
            string path,
            string basePath,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.NormalizeRelativePath(path, basePath, context);
        }

        #endregion NormalizeRelativePath

        #region IsValid

        /// <summary>
        /// Determines if the MSH path is a syntactically and semantically valid path for the provider.
        /// </summary>
        /// <param name="path">
        /// The path to validate.
        /// </param>
        /// <returns>
        /// true if the object specified by path is syntactically and semantically valid, false otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public bool IsValid(string path)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.IsValidPath(path);
        }

        /// <summary>
        /// Determines if the MSH path is a syntactically and semantically valid path for the provider.
        /// </summary>
        /// <param name="path">
        /// The path to validate.
        /// </param>
        /// <param name="context">
        /// The context under which the call is being made.
        /// </param>
        /// <returns>
        /// true if the object specified by path is syntactically and semantically valid, false otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal bool IsValid(
            string path,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.IsValidPath(path, context);
        }

        #endregion IsValid

        #endregion Public methods

        #region private data

        private LocationGlobber PathResolver
        {
            get
            {
                Dbg.Diagnostics.Assert(
                    _sessionState != null,
                    "The only constructor for this class should always set the sessionState field");

                return _pathResolver ?? (_pathResolver = _sessionState.ExecutionContext.LocationGlobber);
            }
        }

        private LocationGlobber _pathResolver;
        private SessionStateInternal _sessionState;

        #endregion private data
    }
}

