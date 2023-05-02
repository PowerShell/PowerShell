// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation.Internal;
using System.Management.Automation.Provider;
using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    /// <summary>
    /// Holds the state of a Monad Shell session.
    /// </summary>
    internal sealed partial class SessionStateInternal
    {
        #region Current working directory/drive

        /// <summary>
        /// Gets the current monad namespace specific working location. If
        /// you want to change the current working directory use the SetLocation
        /// method.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// If a location has not been set yet.
        /// </exception>
        internal PathInfo CurrentLocation
        {
            get
            {
                if (CurrentDrive == null)
                {
                    // We need the error handling, and moving to a method would be
                    // a breaking change
#pragma warning suppress 56503
                    throw PSTraceSource.NewInvalidOperationException();
                }

                PathInfo result =
                    new PathInfo(
                        CurrentDrive,
                        CurrentDrive.Provider,
                        CurrentDrive.CurrentLocation,
                        new SessionState(this));

                return result;
            }
        }

        /// <summary>
        /// Gets the namespace specific path of the current working directory
        /// for the specified namespace.
        /// </summary>
        /// <param name="namespaceID">
        /// An identifier that uniquely identifies the namespace to get the
        /// current working directory for.
        /// </param>
        /// <returns>
        /// The namespace specific path of the current working directory for
        /// the specified namespace.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="namespaceID"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If <paramref name="namespacesID"/> refers to a provider that does not exist.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If a current drive cannot be found for the provider <paramref name="namespaceID"/>
        /// </exception>
        internal PathInfo GetNamespaceCurrentLocation(string namespaceID)
        {
            if (namespaceID == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(namespaceID));
            }

            // If namespace ID is empty, we will use the current working drive
            PSDriveInfo drive = null;

            if (namespaceID.Length == 0)
            {
                ProvidersCurrentWorkingDrive.TryGetValue(CurrentDrive.Provider, out drive);
            }
            else
            {
                // First check to see if the provider exists
                ProvidersCurrentWorkingDrive.TryGetValue(GetSingleProvider(namespaceID), out drive);
            }

            if (drive == null)
            {
                DriveNotFoundException e =
                    new DriveNotFoundException(
                        namespaceID,
                        "DriveNotFound",
                        SessionStateStrings.DriveNotFound);
                throw e;
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Drive = drive;

            // Now make the namespace specific path
            string path = null;

            if (drive.Hidden)
            {
                if (LocationGlobber.IsProviderDirectPath(drive.CurrentLocation))
                {
                    path = drive.CurrentLocation;
                }
                else
                {
                    path = LocationGlobber.GetProviderQualifiedPath(drive.CurrentLocation, drive.Provider);
                }
            }
            else
            {
                path = LocationGlobber.GetDriveQualifiedPath(drive.CurrentLocation, drive);
            }

            return new PathInfo(drive, drive.Provider, path, new SessionState(this));
        }

        /// <summary>
        /// Changes the current working directory to the path specified.
        /// </summary>
        /// <param name="path">
        /// The path of the new current working directory.
        /// </param>
        /// <returns>
        /// The PathInfo object representing the path of the location
        /// that was set.
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
        internal PathInfo SetLocation(string path)
        {
            return SetLocation(path, null);
        }

        /// <summary>
        /// Changes the current working directory to the path specified.
        /// </summary>
        /// <param name="path">
        /// The path of the new current working directory
        /// </param>
        /// <param name="context">
        /// The context the provider uses when performing the operation.
        /// </param>
        /// <returns>
        /// The PathInfo object representing the path of the location
        /// that was set.
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
        /// <exception cref="ItemNotFoundException">
        /// If the <paramref name="path"/> could not be resolved.
        /// </exception>
        internal PathInfo SetLocation(string path, CmdletProviderContext context)
        {
            return SetLocation(path, context, literalPath: false);
        }

        /// <summary>
        /// Changes the current working directory to the path specified.
        /// </summary>
        /// <param name="path">
        /// The path of the new current working directory.
        /// </param>
        /// <param name="context">
        /// The context the provider uses when performing the operation.
        /// </param>
        /// <param name="literalPath">
        /// Indicate if the path is a literal path.
        /// </param>
        /// <returns>
        /// The PathInfo object representing the path of the location
        /// that was set.
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
        /// <exception cref="ItemNotFoundException">
        /// If the <paramref name="path"/> could not be resolved.
        /// </exception>
        internal PathInfo SetLocation(string path, CmdletProviderContext context, bool literalPath)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            PathInfo current = CurrentLocation;
            string originalPath = path;
            string driveName = null;
            ProviderInfo provider = null;
            string providerId = null;

            switch (originalPath)
            {
                case string originalPathSwitch when !literalPath && originalPathSwitch.Equals("-", StringComparison.Ordinal):
                    if (_setLocationHistory.UndoCount <= 0)
                    {
                        throw new InvalidOperationException(SessionStateStrings.LocationUndoStackIsEmpty);
                    }

                    path = _setLocationHistory.Undo(this.CurrentLocation).Path;
                    break;
                case string originalPathSwitch when !literalPath && originalPathSwitch.Equals("+", StringComparison.Ordinal):
                    if (_setLocationHistory.RedoCount <= 0)
                    {
                        throw new InvalidOperationException(SessionStateStrings.LocationRedoStackIsEmpty);
                    }

                    path = _setLocationHistory.Redo(this.CurrentLocation).Path;
                    break;
                default:
                    var pushPathInfo = GetNewPushPathInfo();
                    _setLocationHistory.Push(pushPathInfo);
                    break;
            }

            PSDriveInfo previousWorkingDrive = CurrentDrive;

            // First check to see if the path is a home path
            if (LocationGlobber.IsHomePath(path))
            {
                path = Globber.GetHomeRelativePath(path);
            }

            if (LocationGlobber.IsProviderDirectPath(path))
            {
                // The path is a provider-direct path so use the current
                // provider and its hidden drive but don't modify the path
                // at all.
                provider = CurrentLocation.Provider;
                CurrentDrive = provider.HiddenDrive;
            }
            else if (LocationGlobber.IsProviderQualifiedPath(path, out providerId))
            {
                provider = GetSingleProvider(providerId);
                CurrentDrive = provider.HiddenDrive;
            }
            else
            {
                // See if the path is a relative or absolute
                // path.
                if (Globber.IsAbsolutePath(path, out driveName))
                {
                    // Since the path is an absolute path
                    // we need to change the current working
                    // drive
                    PSDriveInfo newWorkingDrive = GetDrive(driveName);
                    CurrentDrive = newWorkingDrive;

                    // If the path is simply a colon-terminated drive,
                    // not a slash-terminated path to the root of a drive,
                    // set the path to the current working directory of that drive.
                    string colonTerminatedVolume = CurrentDrive.Name + ':';
                    if (CurrentDrive.VolumeSeparatedByColon && (path.Length == colonTerminatedVolume.Length))
                    {
                        path = Path.Combine(colonTerminatedVolume + Path.DirectorySeparatorChar, CurrentDrive.CurrentLocation);
                    }

                    // Now that the current working drive is set,
                    // process the rest of the path as a relative path.
                }
            }

            context ??= new CmdletProviderContext(this.ExecutionContext);

            if (CurrentDrive != null)
            {
                context.Drive = CurrentDrive;
            }

            CmdletProvider providerInstance = null;

            Collection<PathInfo> workingPath = null;

            try
            {
                workingPath =
                    Globber.GetGlobbedMonadPathsFromMonadPath(
                        path,
                        false,
                        context,
                        out providerInstance);
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
                // Reset the drive to the previous drive and
                // then rethrow the error
                CurrentDrive = previousWorkingDrive;
                throw;
            }

            if (workingPath.Count == 0)
            {
                // Set the current working drive back to the previous
                // one in case it was changed.
                CurrentDrive = previousWorkingDrive;

                throw
                    new ItemNotFoundException(
                        path,
                        "PathNotFound",
                        SessionStateStrings.PathNotFound);
            }

            // We allow globbing the location as long as it only resolves a single container.
            bool foundContainer = false;
            bool pathIsContainer = false;
            bool pathIsProviderQualifiedPath = false;
            bool currentPathisProviderQualifiedPath = false;

            for (int index = 0; index < workingPath.Count; ++index)
            {
                CmdletProviderContext normalizePathContext =
                    new CmdletProviderContext(context);

                PathInfo resolvedPath = workingPath[index];
                string currentPath = path;
                try
                {
                    string providerName = null;
                    currentPathisProviderQualifiedPath = LocationGlobber.IsProviderQualifiedPath(resolvedPath.Path, out providerName);
                    if (currentPathisProviderQualifiedPath)
                    {
                        // The path should be the provider-qualified path without the provider ID
                        // or ::
                        string providerInternalPath = LocationGlobber.RemoveProviderQualifier(resolvedPath.Path);

                        try
                        {
                            currentPath = NormalizeRelativePath(GetSingleProvider(providerName), providerInternalPath, string.Empty, normalizePathContext);
                        }
                        catch (NotSupportedException)
                        {
                            // Since the provider does not support normalizing the path, just
                            // use the path we currently have.
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
                            // Reset the drive to the previous drive and
                            // then rethrow the error
                            CurrentDrive = previousWorkingDrive;
                            throw;
                        }
                    }
                    else
                    {
                        try
                        {
                            currentPath = NormalizeRelativePath(resolvedPath.Path, CurrentDrive.Root, normalizePathContext);
                        }
                        catch (NotSupportedException)
                        {
                            // Since the provider does not support normalizing the path, just
                            // use the path we currently have.
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
                            // Reset the drive to the previous drive and
                            // then rethrow the error
                            CurrentDrive = previousWorkingDrive;
                            throw;
                        }
                    }

                    // Now see if there was errors while normalizing the path
                    if (normalizePathContext.HasErrors())
                    {
                        // Set the current working drive back to the previous
                        // one in case it was changed.
                        CurrentDrive = previousWorkingDrive;

                        normalizePathContext.ThrowFirstErrorOrDoNothing();
                    }
                }
                finally
                {
                    normalizePathContext.RemoveStopReferral();
                }

                bool isContainer = false;

                CmdletProviderContext itemContainerContext =
                    new CmdletProviderContext(context);
                itemContainerContext.SuppressWildcardExpansion = true;

                try
                {
                    isContainer =
                        IsItemContainer(
                            resolvedPath.Path,
                            itemContainerContext);

                    if (itemContainerContext.HasErrors())
                    {
                        // Set the current working drive back to the previous
                        // one in case it was changed.
                        CurrentDrive = previousWorkingDrive;

                        itemContainerContext.ThrowFirstErrorOrDoNothing();
                    }
                }
                catch (NotSupportedException)
                {
                    if (currentPath.Length == 0)
                    {
                        // Treat this as a container because providers that only
                        // support the ContainerCmdletProvider interface are really
                        // containers at their root.
                        isContainer = true;
                    }
                }
                finally
                {
                    itemContainerContext.RemoveStopReferral();
                }

                if (isContainer)
                {
                    if (foundContainer)
                    {
                        // The path resolved to more than one container
                        // Set the current working drive back to the previous
                        // one in case it was changed.
                        CurrentDrive = previousWorkingDrive;

                        throw
                            PSTraceSource.NewArgumentException(
                                nameof(path),
                                SessionStateStrings.PathResolvedToMultiple,
                                originalPath);
                    }
                    else
                    {
                        // Set the path to use
                        path = currentPath;

                        // Mark it as a container
                        pathIsContainer = true;

                        // Mark whether or not it was provider-qualified
                        pathIsProviderQualifiedPath = currentPathisProviderQualifiedPath;

                        // Mark that we have already found one container. Finding additional
                        // should be an error
                        foundContainer = true;
                    }
                }
            }

            if (pathIsContainer)
            {
                // Remove the root slash since it is implied that the
                // current working directory is relative to the root.
                if (!LocationGlobber.IsProviderDirectPath(path) &&
                    path.StartsWith(StringLiterals.DefaultPathSeparator) &&
                    !pathIsProviderQualifiedPath)
                {
                    path = path.Substring(1);
                }

                s_tracer.WriteLine(
                    "New working path = {0}",
                    path);

                CurrentDrive.CurrentLocation = path;
            }
            else
            {
                // Set the current working drive back to the previous
                // one in case it was changed.
                CurrentDrive = previousWorkingDrive;

                throw
                    new ItemNotFoundException(
                        originalPath,
                        "PathNotFound",
                        SessionStateStrings.PathNotFound);
            }

            // Now make sure the current drive is set in the provider's
            // current working drive hashtable
            ProvidersCurrentWorkingDrive[CurrentDrive.Provider] =
                CurrentDrive;

            // Set the $PWD variable to the new location
            this.SetVariable(SpecialVariables.PWDVarPath, this.CurrentLocation, false, true, CommandOrigin.Internal);

            // If an action has been defined for location changes, invoke it now.
            if (PublicSessionState.InvokeCommand.LocationChangedAction != null)
            {
                var eventArgs = new LocationChangedEventArgs(PublicSessionState, current, CurrentLocation);
                PublicSessionState.InvokeCommand.LocationChangedAction.Invoke(ExecutionContext.CurrentRunspace, eventArgs);
                s_tracer.WriteLine("Invoked LocationChangedAction");
            }

            return this.CurrentLocation;
        }

        /// <summary>
        /// Determines if the specified path is the current working directory
        /// or a parent of the current working directory.
        /// </summary>
        /// <param name="path">
        /// A monad namespace absolute or relative path.
        /// </param>
        /// <param name="context">
        /// The context the provider uses when performing the operation.
        /// </param>
        /// <returns>
        /// true, if the path is the current working directory or a parent of the current
        /// working directory. false, otherwise.
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
            bool result = false;

            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            PSDriveInfo drive = null;
            ProviderInfo provider = null;

            string providerSpecificPath =
                Globber.GetProviderPath(
                    path,
                    context,
                    out provider,
                    out drive);

            if (drive != null)
            {
                s_tracer.WriteLine("Tracing drive");
                drive.Trace();
            }

            Dbg.Diagnostics.Assert(
                providerSpecificPath != null,
                "There should always be a way to generate a provider path for a " +
                "given path");

            if (drive != null)
            {
                context.Drive = drive;
            }

            // Check to see if the path that was specified is within the current
            // working drive
            if (drive == CurrentDrive)
            {
                // The path needs to be normalized to get rid of relative path tokens
                // so they don't interfere with our path comparisons below
                CmdletProviderContext normalizePathContext
                    = new CmdletProviderContext(context);

                try
                {
                    providerSpecificPath = NormalizeRelativePath(path, null, normalizePathContext);
                }
                catch (NotSupportedException)
                {
                    // Since the provider does not support normalizing the path, just
                    // use the path we currently have.
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
                finally
                {
                    normalizePathContext.RemoveStopReferral();
                }

                if (normalizePathContext.HasErrors())
                {
                    normalizePathContext.ThrowFirstErrorOrDoNothing();
                }

                s_tracer.WriteLine("Provider path = {0}", providerSpecificPath);

                // Get the current working directory provider specific path
                PSDriveInfo currentWorkingDrive = null;
                ProviderInfo currentDriveProvider = null;

                string currentWorkingPath =
                    Globber.GetProviderPath(
                        ".",
                        context,
                        out currentDriveProvider,
                        out currentWorkingDrive);

                Dbg.Diagnostics.Assert(
                    currentWorkingDrive == CurrentDrive,
                    "The current working drive should be the CurrentDrive.");

                s_tracer.WriteLine(
                    "Current working path = {0}",
                    currentWorkingPath);

                // See if the path is the current working directory or a parent
                // of the current working directory
                s_tracer.WriteLine(
                    "Comparing {0} to {1}",
                    providerSpecificPath,
                    currentWorkingPath);

                if (string.Equals(providerSpecificPath, currentWorkingPath, StringComparison.OrdinalIgnoreCase))
                {
                    // The path is the current working directory so
                    // return true
                    s_tracer.WriteLine("The path is the current working directory");

                    result = true;
                }
                else
                {
                    // Check to see if the specified path is a parent
                    // of the current working directory
                    string lockedDirectory = currentWorkingPath;

                    while (lockedDirectory.Length > 0)
                    {
                        // We need to allow the provider to go as far up the tree
                        // as it can even if that means it has to traverse higher
                        // than the mount point for this drive. That is
                        // why we are passing the empty string as the root here.
                        lockedDirectory =
                            GetParentPath(
                                drive.Provider,
                                lockedDirectory,
                                string.Empty,
                                context);

                        s_tracer.WriteLine(
                            "Comparing {0} to {1}",
                            lockedDirectory,
                            providerSpecificPath);

                        if (string.Equals(lockedDirectory, providerSpecificPath, StringComparison.OrdinalIgnoreCase))
                        {
                            // The path is a parent of the current working
                            // directory
                            s_tracer.WriteLine(
                                "The path is a parent of the current working directory: {0}",
                                lockedDirectory);

                            result = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                s_tracer.WriteLine("Drives are not the same");
            }

            return result;
        }

        #endregion Current working directory/drive

        #region push-Pop current working directory

        /// <summary>
        /// Location history for Set-Location that supports Undo/Redo using bounded stacks.
        /// </summary>
        private readonly HistoryStack<PathInfo> _setLocationHistory;

        /// <summary>
        /// A stack of the most recently pushed locations.
        /// </summary>
        private readonly Dictionary<string, Stack<PathInfo>> _workingLocationStack;

        private const string startingDefaultStackName = "default";
        /// <summary>
        /// The name of the default location stack.
        /// </summary>
        private string _defaultStackName = startingDefaultStackName;

        /// <summary>
        /// Pushes the current location onto the working
        /// location stack so that it can be retrieved later.
        /// </summary>
        /// <param name="stackName">
        /// The ID of the stack to push the location on. If
        /// it is null or empty the default stack is used.
        /// </param>
        internal void PushCurrentLocation(string stackName)
        {
            if (string.IsNullOrEmpty(stackName))
            {
                stackName = _defaultStackName;
            }

            // Get the location stack from the hashtable
            Stack<PathInfo> locationStack = null;

            if (!_workingLocationStack.TryGetValue(stackName, out locationStack))
            {
                locationStack = new Stack<PathInfo>();
                _workingLocationStack[stackName] = locationStack;
            }

            // Push the directory/drive pair onto the stack
            var pushPathInfo = GetNewPushPathInfo();
            locationStack.Push(pushPathInfo);
        }

        private PathInfo GetNewPushPathInfo()
        {
            // Create a new instance of the directory/drive pair
            ProviderInfo provider = CurrentDrive.Provider;
            string mshQualifiedPath =
                LocationGlobber.GetMshQualifiedPath(CurrentDrive.CurrentLocation, CurrentDrive);

            PathInfo newPushLocation =
                new PathInfo(
                    CurrentDrive,
                    provider,
                    mshQualifiedPath,
                    new SessionState(this));

            s_tracer.WriteLine(
                "Pushing drive: {0} directory: {1}",
                CurrentDrive.Name,
                mshQualifiedPath);

            return newPushLocation;
        }

        /// <summary>
        /// Resets the current working drive and directory to the first
        /// entry on the working directory stack and removes that entry
        /// from the stack.
        /// </summary>
        /// <param name="stackName">
        /// The ID of the stack to pop the location from. If it is null or
        /// empty the default stack is used.
        /// </param>
        /// <returns>
        /// A PathInfo object representing the location that was popped
        /// from the location stack and set as the new location.
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
        internal PathInfo PopLocation(string stackName)
        {
            if (string.IsNullOrEmpty(stackName))
            {
                stackName = _defaultStackName;
            }

            if (WildcardPattern.ContainsWildcardCharacters(stackName))
            {
                // Need to glob the stack name, but it can only glob to a single.
                bool haveMatch = false;

                WildcardPattern stackNamePattern =
                    WildcardPattern.Get(stackName, WildcardOptions.IgnoreCase);

                foreach (string key in _workingLocationStack.Keys)
                {
                    if (stackNamePattern.IsMatch(key))
                    {
                        if (haveMatch)
                        {
                            throw
                                PSTraceSource.NewArgumentException(
                                    nameof(stackName),
                                    SessionStateStrings.StackNameResolvedToMultiple,
                                    stackName);
                        }

                        haveMatch = true;
                        stackName = key;
                    }
                }
            }

            PathInfo result = CurrentLocation;

            try
            {
                Stack<PathInfo> locationStack = null;
                if (!_workingLocationStack.TryGetValue(stackName, out locationStack))
                {
                    if (!string.Equals(stackName, startingDefaultStackName, StringComparison.OrdinalIgnoreCase))
                    {
                        throw
                            PSTraceSource.NewArgumentException(
                                nameof(stackName),
                                SessionStateStrings.StackNotFound,
                                stackName);
                    }

                    return null;
                }

                PathInfo poppedWorkingDirectory = locationStack.Pop();

                Dbg.Diagnostics.Assert(
                    poppedWorkingDirectory != null,
                    "All items in the workingLocationStack should be " +
                    "of type PathInfo");

                string newPath =
                    LocationGlobber.GetMshQualifiedPath(
                        WildcardPattern.Escape(poppedWorkingDirectory.Path),
                        poppedWorkingDirectory.GetDrive());

                result = SetLocation(newPath);

                if (locationStack.Count == 0 &&
                    !string.Equals(stackName, startingDefaultStackName, StringComparison.OrdinalIgnoreCase))
                {
                    // Remove the stack from the stack list if it
                    // no longer contains any paths.
                    _workingLocationStack.Remove(stackName);
                }
            }
            catch (InvalidOperationException)
            {
                // This is a no-op. We stay with the current working
                // directory.
            }

            return result;
        }

        /// <summary>
        /// Gets the monad namespace paths for all the directories that are
        /// pushed on the working directory stack.
        /// </summary>
        /// <param name="stackName">
        /// The stack of the ID of the location stack to retrieve. If it is
        /// null or empty the default stack is used.
        /// </param>
        /// <returns>
        /// The PathInfoStack representing the location stack for the specified
        /// stack ID.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// If no location stack <paramref name="stackName"/> exists except if
        /// the default stack is requested.
        /// </exception>
        internal PathInfoStack LocationStack(string stackName)
        {
            if (string.IsNullOrEmpty(stackName))
            {
                stackName = _defaultStackName;
            }

            Stack<PathInfo> locationStack = null;

            if (!_workingLocationStack.TryGetValue(stackName, out locationStack))
            {
                // If the request was for the default stack, but it doesn't
                // yet exist, create a dummy stack and return it.
                if (string.Equals(
                        stackName,
                        startingDefaultStackName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    locationStack = new Stack<PathInfo>();
                }
                else
                {
                    throw PSTraceSource.NewArgumentException(nameof(stackName));
                }
            }

            PathInfoStack result = new PathInfoStack(stackName, locationStack);

            return result;
        }

        /// <summary>
        /// Sets the default stack ID to the specified stack ID.
        /// </summary>
        /// <param name="stackName">
        /// The stack ID to be used as the default.
        /// </param>
        /// <returns>
        /// The PathInfoStack for the new default stack or null if the
        /// stack does not exist yet.
        /// </returns>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="stackName"/> does not exist as a location stack.
        /// </exception>
        internal PathInfoStack SetDefaultLocationStack(string stackName)
        {
            if (string.IsNullOrEmpty(stackName))
            {
                stackName = startingDefaultStackName;
            }

            if (!_workingLocationStack.ContainsKey(stackName))
            {
                if (string.Equals(stackName, startingDefaultStackName, StringComparison.OrdinalIgnoreCase))
                {
                    // Since the "default" stack must always exist, create it here
                    return new PathInfoStack(startingDefaultStackName, new Stack<PathInfo>());
                }

                ItemNotFoundException itemNotFound =
                    new ItemNotFoundException(
                        stackName,
                        "StackNotFound",
                        SessionStateStrings.PathNotFound);

                throw itemNotFound;
            }

            _defaultStackName = stackName;

            Stack<PathInfo> locationStack = _workingLocationStack[_defaultStackName];

            if (locationStack != null)
            {
                return new PathInfoStack(_defaultStackName, locationStack);
            }

            return null;
        }

        #endregion push-Pop current working directory
    }

    /// <summary>
    /// Event argument for the LocationChangedAction containing
    /// information about the old location we were in and the new
    /// location we changed to.
    /// </summary>
    public class LocationChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the LocationChangedEventArgs class.
        /// </summary>
        /// <param name="sessionState">
        /// The public session state instance associated with this runspace.
        /// </param>
        /// <param name="oldPath">
        /// The path we changed locations from.
        /// </param>
        /// <param name="newPath">
        /// The path we change locations to.
        /// </param>
        internal LocationChangedEventArgs(SessionState sessionState, PathInfo oldPath, PathInfo newPath)
        {
            SessionState = sessionState;
            OldPath = oldPath;
            NewPath = newPath;
        }

        /// <summary>
        /// Gets the path we changed location from.
        /// </summary>
        public PathInfo OldPath { get; internal set; }

        /// <summary>
        /// Gets the path we changed location to.
        /// </summary>
        public PathInfo NewPath { get; internal set; }

        /// <summary>
        /// Gets the session state instance for the current runspace.
        /// </summary>
        public SessionState SessionState { get; internal set; }
    }
}
