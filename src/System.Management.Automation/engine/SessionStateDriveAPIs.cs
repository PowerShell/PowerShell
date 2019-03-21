// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Management.Automation.Provider;
using Dbg = System.Management.Automation;
using System.Globalization;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings
#pragma warning disable 56500

namespace System.Management.Automation
{
    /// <summary>
    /// Holds the state of a Monad Shell session.
    /// </summary>
    internal sealed partial class SessionStateInternal
    {
        /// <summary>
        /// The currently active drive. It determines the current working directory.
        /// </summary>
        private PSDriveInfo _currentDrive;

        #region NewDrive

        /// <summary>
        /// Adds the specified drive to the current scope.
        /// </summary>
        /// <param name="drive">
        /// The drive to be added to the current scope.
        /// </param>
        /// <param name="scopeID">
        /// The ID for the scope to add the drive to. The scope ID can be any of the
        /// "special" scope identifiers like "global", "local", or "private" or it
        /// can be a numeric identifier that is a count of the number of parent
        /// scopes up from the current scope to put the drive in.
        /// If this parameter is null or empty the drive will be placed in the
        /// current scope.
        /// </param>
        /// <returns>
        /// The drive that was added, if any.
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
        internal PSDriveInfo NewDrive(PSDriveInfo drive, string scopeID)
        {
            if (drive == null)
            {
                throw PSTraceSource.NewArgumentNullException("drive");
            }

            PSDriveInfo result = null;

            // Construct a CmdletProviderContext and call the override

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);

            NewDrive(drive, scopeID, context);

            context.ThrowFirstErrorOrDoNothing();

            Collection<PSObject> successObjects = context.GetAccumulatedObjects();

            if (successObjects != null &&
                successObjects.Count > 0)
            {
                Dbg.Diagnostics.Assert(
                    successObjects.Count == 1,
                    "NewDrive should only add one PSDriveInfo object to the pipeline");

                // set the return value to the first drive (should only be one).

                if (!successObjects[0].ImmediateBaseObjectIsEmpty)
                {
                    result = (PSDriveInfo)successObjects[0].BaseObject;
                }
            }

            return result;
        }

        /// <summary>
        /// Adds a drive to the PowerShell namespace.
        /// </summary>
        /// <param name="drive">
        /// The new drive to be added.
        /// </param>
        /// <param name="scopeID">
        /// The ID for the scope to add the drive to. The scope ID can be any of the
        /// "special" scope identifiers like "global", "local", or "private" or it
        /// can be a numeric identifier that is a count of the number of parent
        /// scopes up from the current scope to put the drive in.
        /// If this parameter is null or empty the drive will be placed in the
        /// current scope.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="drive"/> or <paramref name="context"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the drive already exists
        /// or
        /// If <paramref name="drive"/>.Name contains one or more invalid characters; ~ / \\ . :
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
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
        internal void NewDrive(PSDriveInfo drive, string scopeID, CmdletProviderContext context)
        {
            if (drive == null)
            {
                throw PSTraceSource.NewArgumentNullException("drive");
            }

            if (context == null)
            {
                throw PSTraceSource.NewArgumentNullException("context");
            }

            if (!IsValidDriveName(drive.Name))
            {
                ArgumentException e =
                    PSTraceSource.NewArgumentException(
                        "drive.Name",
                        SessionStateStrings.DriveNameIllegalCharacters);

                throw e;
            }

            // Allow the provider a chance to approve the drive and set
            // provider specific data

            PSDriveInfo result = ValidateDriveWithProvider(drive, context, true);

            // We assume that the provider wrote the error message as they
            // are suppose to.
            if (result == null)
            {
                return;
            }

            if (string.Compare(result.Name, drive.Name, StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                // Set the drive in the current scope.

                try
                {
                    SessionStateScope scope = _currentScope;

                    if (!string.IsNullOrEmpty(scopeID))
                    {
                        scope = GetScopeByID(scopeID);
                    }

                    scope.NewDrive(result);
                }
                catch (ArgumentException argumentException)
                {
                    // Wrap up the exception and write it to the error stream

                    context.WriteError(
                        new ErrorRecord(
                            argumentException,
                            "NewDriveError",
                            ErrorCategory.InvalidArgument,
                            result));
                    return;
                }
                catch (SessionStateException)
                {
                    // This should be a pipeline terminating condition
                    throw;
                }

                if (ProvidersCurrentWorkingDrive[drive.Provider] == null)
                {
                    // Set the new drive as the current
                    // drive for the provider since there isn't one set.

                    ProvidersCurrentWorkingDrive[drive.Provider] = drive;
                }

                // Upon success, write the drive to the pipeline

                context.WriteObject(result);
            }
            else
            {
                ProviderInvocationException e =
                    NewProviderInvocationException(
                        "NewDriveProviderFailed",
                        SessionStateStrings.NewDriveProviderFailed,
                        drive.Provider,
                        drive.Root,
                        PSTraceSource.NewArgumentException("root"));

                throw e;
            }
        }

        private static bool IsValidDriveName(string name)
        {
            bool result = true;

            do
            {
                if (string.IsNullOrEmpty(name))
                {
                    result = false;
                    break;
                }

                if (name.IndexOfAny(s_charactersInvalidInDriveName) >= 0)
                {
                    result = false;
                    break;
                }
            } while (false);

            return result;
        }

        private static char[] s_charactersInvalidInDriveName = new char[] { ':', '/', '\\', '.', '~' };

        /// <summary>
        /// Tries to resolve the drive root as an MSH path. If it successfully resolves
        /// to a single path then the resolved provider internal path is returned. If it
        /// does not resolve to a single MSH path the root is returned as it was passed.
        /// </summary>
        /// <param name="root">
        /// The root path of the drive to be resolved.
        /// </param>
        /// <param name="provider">
        /// The provider that should be used when resolving the path.
        /// </param>
        /// <returns>
        /// The new root path of the drive.
        /// </returns>
        private string GetProviderRootFromSpecifiedRoot(string root, ProviderInfo provider)
        {
            Dbg.Diagnostics.Assert(
                root != null,
                "Caller should have verified the root");

            Dbg.Diagnostics.Assert(
                provider != null,
                "Caller should have verified the provider");

            string result = root;

            SessionState sessionState = new SessionState(ExecutionContext.TopLevelSessionState);
            Collection<string> resolvedPaths = null;
            ProviderInfo resolvedProvider = null;

            try
            {
                // First try to resolve the root as an MSH path

                resolvedPaths =
                    sessionState.Path.GetResolvedProviderPathFromPSPath(root, out resolvedProvider);

                // If a single path was resolved...

                if (resolvedPaths != null &&
                    resolvedPaths.Count == 1)
                {
                    // and the provider used to resolve the path,
                    // matches the one specified by the drive...

                    if (provider.NameEquals(resolvedProvider.FullName))
                    {
                        // and the item exists

                        ProviderIntrinsics providerIntrinsics =
                            new ProviderIntrinsics(this);

                        if (providerIntrinsics.Item.Exists(root))
                        {
                            // then use the resolved path as the root of the drive

                            result = resolvedPaths[0];
                        }
                    }
                }
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            // If any of the following exceptions are thrown we assume that
            // the path is a file system path not an MSH path and try
            // to create the drive with that root.
            catch (DriveNotFoundException)
            {
            }
            catch (ProviderNotFoundException)
            {
            }
            catch (ItemNotFoundException)
            {
            }
            catch (NotSupportedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
            catch (ProviderInvocationException)
            {
            }
            catch (ArgumentException)
            {
            }

            return result;
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
        internal object NewDriveDynamicParameters(string providerId, CmdletProviderContext context)
        {
            if (providerId == null)
            {
                // If the provider hasn't been specified yet, just return null.
                // The provider can be specified as pipeline input.
                return null;
            }

            DriveCmdletProvider provider = GetDriveProviderInstance(providerId);

            object result = null;
            try
            {
                result = provider.NewDriveDynamicParameters(context);
            }
            catch (Exception e) // Catch-all OK, 3rd party callout
            {
                throw
                    NewProviderInvocationException(
                        "NewDriveDynamicParametersProviderException",
                        SessionStateStrings.NewDriveDynamicParametersProviderException,
                        provider.ProviderInfo,
                        null,
                        e);
            }

            return result;
        }

        #endregion NewDrive

        #region GetDrive

        /// <summary>
        /// Searches through the session state scopes to find a drive.
        /// </summary>
        /// <param name="name">
        /// The name of a drive to find.
        /// </param>
        /// <returns>
        /// The drive information if the drive is found.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="name"/> is null.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If there is no drive with <paramref name="name"/>.
        /// </exception>
        internal PSDriveInfo GetDrive(string name)
        {
            return GetDrive(name, true);
        }

        private PSDriveInfo GetDrive(string name, bool automount)
        {
            if (name == null)
            {
                throw PSTraceSource.NewArgumentNullException("name");
            }

            PSDriveInfo result = null;

            // Start searching through the scopes for the drive until the drive
            // is found or the global scope is reached.

            SessionStateScopeEnumerator scopeEnumerator = new SessionStateScopeEnumerator(CurrentScope);

            int scopeID = 0;

            foreach (SessionStateScope processingScope in scopeEnumerator)
            {
                result = processingScope.GetDrive(name);

                if (result != null)
                {
                    if (result.IsAutoMounted)
                    {
                        // Validate or remove the auto-mounted drive

                        if (!ValidateOrRemoveAutoMountedDrive(result, processingScope))
                        {
                            result = null;
                        }
                    }

                    if (result != null)
                    {
                        s_tracer.WriteLine("Drive found in scope {0}", scopeID);
                        break;
                    }
                }

                // Increment the scope ID
                ++scopeID;
            }

            if (result == null && automount)
            {
                // first try to automount as a file system drive
                result = AutomountFileSystemDrive(name);
                // if it didn't work, then try automounting as a BuiltIn drive (e.g. "Cert"/"Certificate"/"WSMan")
                if (result == null)
                {
                    result = AutomountBuiltInDrive(name); // internally this calls GetDrive(name, false)
                }
            }

            if (result == null)
            {
                DriveNotFoundException driveNotFound =
                    new DriveNotFoundException(
                        name,
                        "DriveNotFound",
                        SessionStateStrings.DriveNotFound);

                throw driveNotFound;
            }

            return result;
        }

        /// <summary>
        /// Searches through the session state scopes looking
        /// for a drive of the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the drive to return.
        /// </param>
        /// <param name="scopeID">
        /// The scope ID of the scope to look in for the drive.
        /// If this parameter is null or empty the drive will be
        /// found by searching the scopes using the dynamic scoping
        /// rules.
        /// </param>
        /// <returns>
        /// The drive for the given name in the given scope or null if
        /// the drive was not found.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="name"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="scopeID"/> is less than zero, or not
        /// a number and not "script", "global", "local", or "private"
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        internal PSDriveInfo GetDrive(string name, string scopeID)
        {
            if (name == null)
            {
                throw PSTraceSource.NewArgumentNullException("name");
            }

            PSDriveInfo result = null;

            // The scope ID wasn't defined or wasn't recognizable
            // so do a search through the scopes looking for the
            // drive.

            if (string.IsNullOrEmpty(scopeID))
            {
                SessionStateScopeEnumerator scopeEnumerator =
                    new SessionStateScopeEnumerator(CurrentScope);

                foreach (SessionStateScope scope in scopeEnumerator)
                {
                    result = scope.GetDrive(name);

                    if (result != null)
                    {
                        if (result.IsAutoMounted)
                        {
                            // Validate or remove the auto-mounted drive

                            if (!ValidateOrRemoveAutoMountedDrive(result, scope))
                            {
                                result = null;
                            }
                        }

                        if (result != null)
                        {
                            break;
                        }
                    }
                }

                if (result == null)
                {
                    result = AutomountFileSystemDrive(name);
                }
            }
            else
            {
                SessionStateScope scope = GetScopeByID(scopeID);
                result = scope.GetDrive(name);

                if (result != null)
                {
                    if (result.IsAutoMounted)
                    {
                        // Validate or remove the auto-mounted drive

                        if (!ValidateOrRemoveAutoMountedDrive(result, scope))
                        {
                            result = null;
                        }
                    }
                }
                else
                {
                    if (scope == GlobalScope)
                    {
                        result = AutomountFileSystemDrive(name);
                    }
                }
            }

            return result;
        }

        private PSDriveInfo AutomountFileSystemDrive(string name)
        {
            PSDriveInfo result = null;

            // Check to see if it could be a "auto-mounted"
            // file system drive.  If so, add the new drive
            // to the global scope and return it

            if (name.Length == 1)
            {
                try
                {
                    System.IO.DriveInfo driveInfo = new System.IO.DriveInfo(name);
                    result = AutomountFileSystemDrive(driveInfo);
                }
                catch (LoopFlowException)
                {
                    throw;
                }
                catch (PipelineStoppedException)
                {
                    throw;
                }
                catch (ActionPreferenceStopException)
                {
                    throw;
                }
                catch (Exception)
                {
                    // Catch all exceptions and continue since the drive does not exist
                    // This action wasn't requested by the user and as such we don't
                    // want to expose the user to any error conditions other than
                    // DriveNotFoundException which will be thrown by the caller
                }
            }

            return result;
        }

        private PSDriveInfo AutomountFileSystemDrive(System.IO.DriveInfo systemDriveInfo)
        {
            PSDriveInfo result = null;

            if (!IsProviderLoaded(this.ExecutionContext.ProviderNames.FileSystem))
            {
                s_tracer.WriteLine("The {0} provider is not loaded", this.ExecutionContext.ProviderNames.FileSystem);
                return null;
            }

            // Since the drive does exist, add it.

            try
            {
                // Get the FS provider

                DriveCmdletProvider driveProvider =
                    GetDriveProviderInstance(this.ExecutionContext.ProviderNames.FileSystem);

                if (driveProvider != null)
                {
                    // Create a new drive
                    string systemDriveName = systemDriveInfo.Name.Substring(0, 1);
                    string volumeLabel = string.Empty;
                    string displayRoot = null;

                    try
                    {
                        // When run in an AppContainer, we may not have access to the volume label.
                        volumeLabel = systemDriveInfo.VolumeLabel;
                    }
                    catch (UnauthorizedAccessException) { }

                    // Get the actual root path for Network type drives
                    if (systemDriveInfo.DriveType == DriveType.Network)
                    {
                        try
                        {
                            displayRoot = Microsoft.PowerShell.Commands.FileSystemProvider
                                            .GetRootPathForNetworkDriveOrDosDevice(systemDriveInfo);
                        }
                        // We want to get root path of the network drive as extra information to display to the user.
                        // It's okay we failed to get the root path for some reason. We don't want to throw exception
                        // here as it would break the current behavior.
                        catch (Win32Exception) { }
                        catch (InvalidOperationException) { }
                    }

                    PSDriveInfo newPSDriveInfo =
                        new PSDriveInfo(
                            systemDriveName,
                            driveProvider.ProviderInfo,
                            systemDriveInfo.RootDirectory.FullName,
                            volumeLabel,
                            null,
                            displayRoot);

                    newPSDriveInfo.IsAutoMounted = true;

                    CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);

                    newPSDriveInfo.DriveBeingCreated = true;

                    // Validate the drive with the provider
                    result = ValidateDriveWithProvider(driveProvider, newPSDriveInfo, context, false);

                    newPSDriveInfo.DriveBeingCreated = false;

                    if (result != null && !context.HasErrors())
                    {
                        // Create the drive in the global scope.
                        GlobalScope.NewDrive(result);
                    }
                }
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (Exception e)
            {
                // Since the user isn't expecting this behavior, we don't
                // want to let errors find their way out. If there are any
                // failures we just don't mount the drive.

                MshLog.LogProviderHealthEvent(
                    this.ExecutionContext,
                    this.ExecutionContext.ProviderNames.FileSystem,
                    e,
                    Severity.Warning);
            }

            return result;
        }

        /// <summary>
        /// Auto-mounts a built-in drive.
        /// </summary>
        /// <param name="name">The name of the drive to load.</param>
        /// <returns></returns>
        internal PSDriveInfo AutomountBuiltInDrive(string name)
        {
            MountDefaultDrive(name, ExecutionContext);
            PSDriveInfo result = GetDrive(name, false);

            return result;
        }

        /// <summary>
        /// Automatically mount the specified drive.
        /// </summary>
        /// <remarks>
        /// Neither 'WSMan' nor 'Certificate' provider works in UNIX PS today.
        /// So this method currently does nothing on UNIX.
        /// </remarks>
        internal static void MountDefaultDrive(string name, ExecutionContext context)
        {
#if !UNIX
            PSModuleAutoLoadingPreference moduleAutoLoadingPreference =
                CommandDiscovery.GetCommandDiscoveryPreference(context, SpecialVariables.PSModuleAutoLoadingPreferenceVarPath, "PSModuleAutoLoadingPreference");
            if (moduleAutoLoadingPreference == PSModuleAutoLoadingPreference.None)
            {
                return;
            }

            string moduleName = null;

            // Note: For the certificate provider, we actually support the provider name as an alternative to
            // mount the default drive, since the provider names can be used for provider-qualified paths.
            // The WSMAN drive is the same as the provider name.
            if (
                string.Equals("Cert", name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals("Certificate", name, StringComparison.OrdinalIgnoreCase)
                )
            {
                moduleName = "Microsoft.PowerShell.Security";
            }
            else if (string.Equals("WSMan", name, StringComparison.OrdinalIgnoreCase))
            {
                moduleName = "Microsoft.WSMan.Management";
            }

            if (!string.IsNullOrEmpty(moduleName))
            {
                s_tracer.WriteLine("Auto-mounting built-in drive: {0}", name);
                CommandInfo commandInfo = new CmdletInfo("Import-Module", typeof(Microsoft.PowerShell.Commands.ImportModuleCommand), null, null, context);
                Exception exception = null;
                s_tracer.WriteLine("Attempting to load module: {0}", moduleName);
                CommandDiscovery.AutoloadSpecifiedModule(moduleName, context, commandInfo.Visibility, out exception);
                if (exception != null)
                {
                    // Call-out to user code, catch-all OK
                }
            }
#endif
        }

        /// <summary>
        /// Determines if the specified automounted drive still exists. If not,
        /// the drive is removed.
        /// </summary>
        /// <param name="drive">
        /// The drive to validate or remove.
        /// </param>
        /// <param name="scope">
        /// The scope the drive is in.  This will be used to remove the drive
        /// if necessary.
        /// </param>
        /// <returns>
        /// True if the drive is still valid, false if the drive was removed.
        /// </returns>
        private bool ValidateOrRemoveAutoMountedDrive(PSDriveInfo drive, SessionStateScope scope)
        {
            bool result = true;
            try
            {
                System.IO.DriveInfo systemDriveInfo = new System.IO.DriveInfo(drive.Name);
                result = systemDriveInfo.DriveType != DriveType.NoRootDirectory;
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (Exception)
            {
                // Assume any exception means the drive is no longer valid and needs
                // to be removed.

                result = false;
            }

            if (!result)
            {
                DriveCmdletProvider driveProvider = null;

                try
                {
                    driveProvider =
                        GetDriveProviderInstance(this.ExecutionContext.ProviderNames.FileSystem);
                }
                catch (NotSupportedException)
                {
                }
                catch (ProviderNotFoundException)
                {
                }

                if (driveProvider != null)
                {
                    CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);

                    try
                    {
                        // Give the provider a chance to cleanup
                        driveProvider.RemoveDrive(drive, context);
                    }
                    // Ignore any exceptions the provider throws because we
                    // are doing this without an explicit request from the
                    // user. Since the provider can throw any exception
                    // we must catch all exceptions here.
                    catch (Exception)
                    {
                    }

                    scope.RemoveDrive(drive);
                }
            }

            return result;
        }

        /// <summary>
        /// If a VHD is mounted to a drive prior to the PowerShell session being launched,
        /// then such a drive has to be validated for its existence before performing
        /// any operations on that drive to make sure that the drive is not unmounted.
        /// </summary>
        /// <param name="drive"></param>
        /// <returns>Absence of mounted drive for FileSystem provider or False for other provider types.</returns>
        private bool IsAStaleVhdMountedDrive(PSDriveInfo drive)
        {
            bool result = false;

            // check that drive's provider type is FileSystem
            if ((drive.Provider != null) && (!drive.Provider.NameEquals(this.ExecutionContext.ProviderNames.FileSystem)))
            {
                return false;
            }

            // A VHD mounted drive gets detected with a DriveType of DriveType.Fixed
            // when the VHD is mounted, however if the drive is unmounted, such a
            // stale drive is no longer valid and gets detected with DriveType.NoRootDirectory.
            // We would hit this situation in the following scenario:
            //  1. Launch Powershell session 'A' and mount the VHD.
            //  2. Launch different powershell session 'B'.
            //  3. Unmount the VHD in session 'A'.
            // The drive pointing to VHD in session 'B' gets detected as DriveType.NoRootDirectory
            // after the VHD is removed in session 'A'.
            if (drive != null && !string.IsNullOrEmpty(drive.Name) && drive.Name.Length == 1)
            {
                try
                {
                    char driveChar = Convert.ToChar(drive.Name, CultureInfo.InvariantCulture);

                    if (char.ToUpperInvariant(driveChar) >= 'A' && char.ToUpperInvariant(driveChar) <= 'Z')
                    {
                        DriveInfo systemDriveInfo = new DriveInfo(drive.Name);

                        if (systemDriveInfo.DriveType == DriveType.NoRootDirectory)
                        {
                            if (!Directory.Exists(drive.Root))
                            {
                                result = true;
                            }
                        }
                    }
                }
                catch (ArgumentException)
                {
                    // At this point, We dont care if the drive is not a valid drive that does not host the VHD.
                }
            }

            return result;
        }

        /// <summary>
        /// Gets all the drives for a specific provider.
        /// </summary>
        /// <param name="providerId">
        /// The identifier for the provider to retrieve the drives for.
        /// </param>
        /// <returns>
        /// An IEnumerable that contains the drives for the specified provider.
        /// </returns>
        internal Collection<PSDriveInfo> GetDrivesForProvider(string providerId)
        {
            if (string.IsNullOrEmpty(providerId))
            {
                return Drives(null);
            }

            // Ensure that the provider name resolves to a single provider
            GetSingleProvider(providerId);

            Collection<PSDriveInfo> drives = new Collection<PSDriveInfo>();

            foreach (PSDriveInfo drive in Drives(null))
            {
                if (drive != null &&
                    drive.Provider.NameEquals(providerId))
                {
                    drives.Add(drive);
                }
            }

            return drives;
        }

        #endregion GetDrive

        #region RemoveDrive
        /// <summary>
        /// Removes the drive with the specified name.
        /// </summary>
        /// <param name="driveName">
        /// The name of the drive to remove.
        /// </param>
        /// <param name="force">
        /// Determines whether drive should be forcefully removed even if there was errors.
        /// </param>
        /// <param name="scopeID">
        /// The ID of the scope from which to remove the drive.
        /// If the scope ID is null or empty, the scope hierarchy will be searched
        /// starting at the current scope through all the parent scopes to the
        /// global scope until a drive of the given name is found to remove.
        /// </param>
        internal void RemoveDrive(string driveName, bool force, string scopeID)
        {
            if (driveName == null)
            {
                throw PSTraceSource.NewArgumentNullException("driveName");
            }

            PSDriveInfo drive = GetDrive(driveName, scopeID);

            if (drive == null)
            {
                DriveNotFoundException e = new DriveNotFoundException(
                    driveName,
                    "DriveNotFound",
                    SessionStateStrings.DriveNotFound);
                throw e;
            }

            RemoveDrive(drive, force, scopeID);
        }

        /// <summary>
        /// Removes the drive with the specified name.
        /// </summary>
        /// <param name="driveName">
        /// The name of the drive to remove.
        /// </param>
        /// <param name="force">
        /// Determines whether drive should be forcefully removed even if there was errors.
        /// </param>
        /// <param name="scopeID">
        /// The ID of the scope from which to remove the drive.
        /// If the scope ID is null or empty, the scope hierarchy will be searched
        /// starting at the current scope through all the parent scopes to the
        /// global scope until a drive of the given name is found to remove.
        /// </param>
        /// <param name="context">
        /// The context of the command.
        /// </param>
        internal void RemoveDrive(
            string driveName,
            bool force,
            string scopeID,
            CmdletProviderContext context)
        {
            if (driveName == null)
            {
                throw PSTraceSource.NewArgumentNullException("driveName");
            }

            Dbg.Diagnostics.Assert(
                context != null,
                "The caller should verify the context");

            PSDriveInfo drive = GetDrive(driveName, scopeID);

            if (drive == null)
            {
                DriveNotFoundException e = new DriveNotFoundException(
                    driveName,
                    "DriveNotFound",
                    SessionStateStrings.DriveNotFound);
                context.WriteError(new ErrorRecord(e.ErrorRecord, e));
            }
            else
            {
                RemoveDrive(drive, force, scopeID, context);
            }
        }

        /// <summary>
        /// Removes the specified drive.
        /// </summary>
        /// <param name="drive">
        /// The drive to be removed.
        /// </param>
        /// <param name="force">
        /// Determines whether drive should be forcefully removed even if there was errors.
        /// </param>
        /// <param name="scopeID">
        /// The ID of the scope from which to remove the drive.
        /// If the scope ID is null or empty, the scope hierarchy will be searched
        /// starting at the current scope through all the parent scopes to the
        /// global scope until a drive of the given name is found to remove.
        /// </param>
        internal void RemoveDrive(PSDriveInfo drive, bool force, string scopeID)
        {
            if (drive == null)
            {
                throw PSTraceSource.NewArgumentNullException("drive");
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);

            RemoveDrive(drive, force, scopeID, context);

            if (context.HasErrors() && !force)
            {
                context.ThrowFirstErrorOrDoNothing();
            }
        }

        /// <summary>
        /// Removes the specified drive.
        /// </summary>
        /// <param name="drive">
        /// The drive to be removed.
        /// </param>
        /// <param name="force">
        /// Determines whether drive should be forcefully removed even if there was errors.
        /// </param>
        /// <param name="scopeID">
        /// The ID of the scope from which to remove the drive.
        /// If the scope ID is null or empty, the scope hierarchy will be searched
        /// starting at the current scope through all the parent scopes to the
        /// global scope until a drive of the given name is found to remove.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        internal void RemoveDrive(
            PSDriveInfo drive,
            bool force,
            string scopeID,
            CmdletProviderContext context)
        {
            // Make sure that the CanRemoveDrive is called even if we are forcing
            // the removal because we want the provider to have a chance to
            // cleanup.

            bool canRemove = false;

            try
            {
                canRemove = CanRemoveDrive(drive, context);
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (ProviderInvocationException)
            {
                if (!force)
                {
                    throw;
                }
            }

            // Now remove the drive if there was no error or we are forcing the removal

            if (canRemove || force)
            {
                // The scope ID wasn't defined or wasn't recognizable
                // so do a search through the scopes looking for the
                // drive.

                if (string.IsNullOrEmpty(scopeID))
                {
                    SessionStateScopeEnumerator scopeEnumerator =
                        new SessionStateScopeEnumerator(CurrentScope);

                    foreach (SessionStateScope scope in scopeEnumerator)
                    {
                        try
                        {
                            PSDriveInfo result = scope.GetDrive(drive.Name);
                            if (result != null)
                            {
                                scope.RemoveDrive(drive);

                                // If the drive is the current drive for the provider, remove
                                // it from the current drive list.

                                if (ProvidersCurrentWorkingDrive[drive.Provider] == result)
                                {
                                    ProvidersCurrentWorkingDrive[drive.Provider] = null;
                                }

                                break;
                            }
                        }
                        catch (ArgumentException)
                        {
                        }
                    }
                }
                else
                {
                    SessionStateScope scope = GetScopeByID(scopeID);
                    scope.RemoveDrive(drive);

                    // If the drive is the current drive for the provider, remove
                    // it from the current drive list.

                    if (ProvidersCurrentWorkingDrive[drive.Provider] == drive)
                    {
                        ProvidersCurrentWorkingDrive[drive.Provider] = null;
                    }
                }
            }
            else
            {
                PSInvalidOperationException e =
                    (PSInvalidOperationException)
                    PSTraceSource.NewInvalidOperationException(
                        SessionStateStrings.DriveRemovalPreventedByProvider,
                        drive.Name,
                        drive.Provider);

                context.WriteError(
                    new ErrorRecord(
                        e.ErrorRecord,
                        e));
            }
        }

        /// <summary>
        /// Determines if the drive can be removed by calling the provider
        /// for the drive.
        /// </summary>
        /// <param name="drive">
        /// The drive to test for removal.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// True if the drive can be removed, false otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="drive"/> or <paramref name="context"/> is null.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception when RemoveDrive was called.
        /// </exception>
        private bool CanRemoveDrive(PSDriveInfo drive, CmdletProviderContext context)
        {
            if (context == null)
            {
                throw PSTraceSource.NewArgumentNullException("context");
            }

            if (drive == null)
            {
                throw PSTraceSource.NewArgumentNullException("drive");
            }

            s_tracer.WriteLine("Drive name = {0}", drive.Name);

            // First set the drive data

            context.Drive = drive;

            // Now see if the provider will let us remove the drive

            DriveCmdletProvider driveCmdletProvider =
                GetDriveProviderInstance(drive.Provider);

            bool driveRemovable = false;

            PSDriveInfo result = null;

            try
            {
                result = driveCmdletProvider.RemoveDrive(drive, context);
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout
            {
                throw NewProviderInvocationException(
                    "RemoveDriveProviderException",
                    SessionStateStrings.RemoveDriveProviderException,
                    driveCmdletProvider.ProviderInfo,
                    null,
                    e);
            }

            if (result != null)
            {
                // Make sure the provider didn't try to pull a fast one on us
                // and substitute a different drive.

                if (string.Compare(result.Name, drive.Name, StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    driveRemovable = true;
                }
            }

            return driveRemovable;
        }

        #endregion RemoveDrive

        #region Drives

        /// <summary>
        /// Gets an enumerable list of the drives that are mounted in
        /// the specified scope.
        /// </summary>
        /// <param name="scope">
        /// The scope to retrieve the drives from. If null or empty,
        /// all drives from all scopes will be retrieved.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="scope"/> is less than zero, or not
        /// a number and not "script", "global", "local", or "private"
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        internal Collection<PSDriveInfo> Drives(string scope)
        {
            Dictionary<string, PSDriveInfo> driveTable = new Dictionary<string, PSDriveInfo>();

            SessionStateScope startingScope = _currentScope;

            if (!string.IsNullOrEmpty(scope))
            {
                startingScope = GetScopeByID(scope);
            }

            SessionStateScopeEnumerator scopeEnumerator =
                new SessionStateScopeEnumerator(startingScope);
            DriveInfo[] alldrives = DriveInfo.GetDrives();
            Collection<string> driveNames = new Collection<string>();
            foreach (DriveInfo drive in alldrives)
            {
                driveNames.Add(drive.Name.Substring(0, 1));
            }

            foreach (SessionStateScope lookupScope in scopeEnumerator)
            {
                foreach (PSDriveInfo drive in lookupScope.Drives)
                {
                    // It is the correct behavior for child scope
                    // drives to overwrite parent scope drives of
                    // the same name.

                    if (drive != null)
                    {
                        bool driveIsValid = true;

                        // If the drive is auto-mounted, ensure that it still exists, or remove the drive.
#if !UNIX
                        if (drive.IsAutoMounted || IsAStaleVhdMountedDrive(drive))
                        {
                            driveIsValid = ValidateOrRemoveAutoMountedDrive(drive, lookupScope);
                        }
#endif
                        if (drive.Name.Length == 1)
                        {
                            if (!(driveNames.Contains(drive.Name)))
                                driveTable.Remove(drive.Name);
                        }

                        if (driveIsValid && !driveTable.ContainsKey(drive.Name))
                        {
                            driveTable[drive.Name] = drive;
                        }
                    }
                }

                // If the scope was specified then don't loop
                // through the other scopes

                if (scope != null && scope.Length > 0)
                {
                    break;
                }
            }

            // Now lookup all the file system drives and automount any that are not
            // present

            try
            {
                foreach (System.IO.DriveInfo fsDriveInfo in alldrives)
                {
                    if (fsDriveInfo != null)
                    {
                        string fsDriveName = fsDriveInfo.Name.Substring(0, 1);
                        if (!driveTable.ContainsKey(fsDriveName))
                        {
                            PSDriveInfo automountedDrive = AutomountFileSystemDrive(fsDriveInfo);
                            if (automountedDrive != null)
                            {
                                driveTable[automountedDrive.Name] = automountedDrive;
                            }
                        }
                    }
                }
            }
            // We don't want to have automounting cause an exception. We
            // rather it just fail silently as it wasn't a result of an
            // explicit request by the user anyway.
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            Collection<PSDriveInfo> results = new Collection<PSDriveInfo>();
            foreach (PSDriveInfo drive in driveTable.Values)
            {
                results.Add(drive);
            }

            return results;
        }

        #endregion Drives

        /// <summary>
        /// Gets or sets the current working drive.
        /// </summary>
        internal PSDriveInfo CurrentDrive
        {
            get
            {
                if (this != ExecutionContext.TopLevelSessionState)
                    return ExecutionContext.TopLevelSessionState.CurrentDrive;
                else
                    return _currentDrive;
            }

            set
            {
                if (this != ExecutionContext.TopLevelSessionState)
                    ExecutionContext.TopLevelSessionState.CurrentDrive = value;
                else
                    _currentDrive = value;
            }
        }
    }
}

#pragma warning restore 56500

