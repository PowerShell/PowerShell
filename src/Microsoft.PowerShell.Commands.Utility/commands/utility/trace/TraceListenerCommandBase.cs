// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Security;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A base class for the trace cmdlets that allow you to specify
    /// which trace listeners to add to a TraceSource.
    /// </summary>
    public class TraceListenerCommandBase : TraceCommandBase
    {
        #region Parameters

        /// <summary>
        /// The TraceSource parameter determines which TraceSource categories the
        /// operation will take place on.
        /// </summary>
        internal string[] NameInternal { get; set; } = Array.Empty<string>();

        /// <summary>
        /// The flags to be set on the TraceSource.
        /// </summary>
        /// <value></value>
        internal PSTraceSourceOptions OptionsInternal
        {
            get
            {
                return _options;
            }

            set
            {
                _options = value;
                optionsSpecified = true;
            }
        }

        private PSTraceSourceOptions _options = PSTraceSourceOptions.All;

        /// <summary>
        /// True if the Options parameter has been set, or false otherwise.
        /// </summary>
        internal bool optionsSpecified;

        /// <summary>
        /// The parameter which determines the options for output from the trace listeners.
        /// </summary>
        internal TraceOptions ListenerOptionsInternal
        {
            get
            {
                return _traceOptions;
            }

            set
            {
                traceOptionsSpecified = true;
                _traceOptions = value;
            }
        }

        private TraceOptions _traceOptions = TraceOptions.None;

        /// <summary>
        /// True if the TraceOptions parameter was specified, or false otherwise.
        /// </summary>
        internal bool traceOptionsSpecified;

        /// <summary>
        /// Adds the file trace listener using the specified file.
        /// </summary>
        /// <value></value>
        internal string FileListener { get; set; }

        /// <summary>
        /// Property that sets force parameter.  This will clear the
        /// read-only attribute on an existing file if present.
        /// </summary>
        /// <remarks>
        /// Note that we do not attempt to reset the read-only attribute.
        /// </remarks>
        public bool ForceWrite { get; set; }

        /// <summary>
        /// If this parameter is specified the Debugger trace listener will be added.
        /// </summary>
        /// <value></value>
        internal bool DebuggerListener { get; set; }

        /// <summary>
        /// If this parameter is specified the Msh Host trace listener will be added.
        /// </summary>
        /// <value></value>
        internal SwitchParameter PSHostListener
        {
            get { return _host; }

            set { _host = value; }
        }

        private bool _host = false;

        #endregion Parameters

        internal Collection<PSTraceSource> ConfigureTraceSource(
            string[] sourceNames,
            bool preConfigure,
            out Collection<PSTraceSource> preconfiguredSources)
        {
            preconfiguredSources = new Collection<PSTraceSource>();

            // Find the matching and unmatched trace sources.

            Collection<string> notMatched = null;
            Collection<PSTraceSource> matchingSources = GetMatchingTraceSource(sourceNames, false, out notMatched);

            if (preConfigure)
            {
                // Set the flags if they were specified
                if (optionsSpecified)
                {
                    SetFlags(matchingSources);
                }

                AddTraceListenersToSources(matchingSources);
                SetTraceListenerOptions(matchingSources);
            }

            // Now try to preset options for sources which have not yet been
            // constructed.

            foreach (string notMatchedName in notMatched)
            {
                if (string.IsNullOrEmpty(notMatchedName))
                {
                    continue;
                }

                if (WildcardPattern.ContainsWildcardCharacters(notMatchedName))
                {
                    continue;
                }

                PSTraceSource newTraceSource =
                    PSTraceSource.GetNewTraceSource(
                        notMatchedName,
                        string.Empty,
                        true);

                preconfiguredSources.Add(newTraceSource);
            }

            // Preconfigure any trace sources that were not already present

            if (preconfiguredSources.Count > 0)
            {
                if (preConfigure)
                {
                    // Set the flags if they were specified
                    if (optionsSpecified)
                    {
                        SetFlags(preconfiguredSources);
                    }

                    AddTraceListenersToSources(preconfiguredSources);
                    SetTraceListenerOptions(preconfiguredSources);
                }

                // Add the sources to the preconfigured table so that they are found
                // when the trace source finally gets created by the system.

                foreach (PSTraceSource sourceToPreconfigure in preconfiguredSources)
                {
                    if (!PSTraceSource.PreConfiguredTraceSource.ContainsKey(sourceToPreconfigure.Name))
                    {
                        PSTraceSource.PreConfiguredTraceSource.Add(sourceToPreconfigure.Name, sourceToPreconfigure);
                    }
                }
            }

            return matchingSources;
        }

        #region AddTraceListeners
        /// <summary>
        /// Adds the console, debugger, file, or host listener if requested.
        /// </summary>
        internal void AddTraceListenersToSources(Collection<PSTraceSource> matchingSources)
        {
            if (DebuggerListener)
            {
                if (_defaultListener == null)
                {
                    _defaultListener =
                        new DefaultTraceListener();

                    // Note, this is not meant to be localized.
                    _defaultListener.Name = "Debug";
                }

                AddListenerToSources(matchingSources, _defaultListener);
            }

            if (PSHostListener)
            {
                if (_hostListener == null)
                {
                    ((MshCommandRuntime)this.CommandRuntime).DebugPreference = ActionPreference.Continue;
                    _hostListener = new PSHostTraceListener(this);

                    // Note, this is not meant to be localized.
                    _hostListener.Name = "Host";
                }

                AddListenerToSources(matchingSources, _hostListener);
            }

            if (FileListener != null)
            {
                if (_fileListeners == null)
                {
                    _fileListeners = new Collection<TextWriterTraceListener>();
                    FileStreams = new Collection<FileStream>();

                    Exception error = null;

                    try
                    {
                        Collection<string> resolvedPaths = new();
                        try
                        {
                            // Resolve the file path
                            ProviderInfo provider = null;
                            resolvedPaths = this.SessionState.Path.GetResolvedProviderPathFromPSPath(FileListener, out provider);

                            // We can only export aliases to the file system
                            if (!provider.NameEquals(this.Context.ProviderNames.FileSystem))
                            {
                                throw
                                    new PSNotSupportedException(
                                        StringUtil.Format(TraceCommandStrings.TraceFileOnly,
                                            FileListener,
                                            provider.FullName));
                            }
                        }
                        catch (ItemNotFoundException)
                        {
                            // Since the file wasn't found, just make a provider-qualified path out if it
                            // and use that.

                            PSDriveInfo driveInfo = null;
                            ProviderInfo provider = null;
                            string path =
                                this.SessionState.Path.GetUnresolvedProviderPathFromPSPath(
                                    FileListener,
                                    new CmdletProviderContext(this.Context),
                                    out provider,
                                    out driveInfo);

                            // We can only export aliases to the file system
                            if (!provider.NameEquals(this.Context.ProviderNames.FileSystem))
                            {
                                throw
                                    new PSNotSupportedException(
                                        StringUtil.Format(TraceCommandStrings.TraceFileOnly,
                                            FileListener,
                                            provider.FullName));
                            }

                            resolvedPaths.Add(path);
                        }

                        if (resolvedPaths.Count > 1)
                        {
                            throw
                                new PSNotSupportedException(StringUtil.Format(TraceCommandStrings.TraceSingleFileOnly, FileListener));
                        }

                        string resolvedPath = resolvedPaths[0];

                        Exception fileOpenError = null;
                        try
                        {
                            if (ForceWrite && System.IO.File.Exists(resolvedPath))
                            {
                                // remove readonly attributes on the file
                                System.IO.FileInfo fInfo = new(resolvedPath);
                                if (fInfo != null)
                                {
                                    // Save some disk write time by checking whether file is readonly..
                                    if ((fInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                                    {
                                        // Make sure the file is not read only
                                        fInfo.Attributes &= ~(FileAttributes.ReadOnly);
                                    }
                                }
                            }

                            // Trace commands always append..So there is no need to set overwrite with force..
                            FileStream fileStream = new(resolvedPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                            FileStreams.Add(fileStream);

                            // Open the file stream

                            TextWriterTraceListener fileListener =
                                    new(fileStream, resolvedPath);

                            fileListener.Name = FileListener;

                            _fileListeners.Add(fileListener);
                        }
                        catch (IOException ioException)
                        {
                            fileOpenError = ioException;
                        }
                        catch (SecurityException securityException)
                        {
                            fileOpenError = securityException;
                        }
                        catch (UnauthorizedAccessException unauthorized)
                        {
                            fileOpenError = unauthorized;
                        }

                        if (fileOpenError != null)
                        {
                            ErrorRecord errorRecord =
                                new(
                                    fileOpenError,
                                    "FileListenerPathResolutionFailed",
                                    ErrorCategory.OpenError,
                                    resolvedPath);

                            WriteError(errorRecord);
                        }
                    }
                    catch (ProviderNotFoundException providerNotFound)
                    {
                        error = providerNotFound;
                    }
                    catch (System.Management.Automation.DriveNotFoundException driveNotFound)
                    {
                        error = driveNotFound;
                    }
                    catch (NotSupportedException notSupported)
                    {
                        error = notSupported;
                    }

                    if (error != null)
                    {
                        ErrorRecord errorRecord =
                            new(
                                error,
                                "FileListenerPathResolutionFailed",
                                ErrorCategory.InvalidArgument,
                                FileListener);

                        WriteError(errorRecord);
                    }
                }

                foreach (TraceListener listener in _fileListeners)
                {
                    AddListenerToSources(matchingSources, listener);
                }
            }
        }

        private DefaultTraceListener _defaultListener;
        private PSHostTraceListener _hostListener;
        private Collection<TextWriterTraceListener> _fileListeners;

        /// <summary>
        /// The file streams that were open by this command.
        /// </summary>
        internal Collection<FileStream> FileStreams { get; private set; }

        private static void AddListenerToSources(Collection<PSTraceSource> matchingSources, TraceListener listener)
        {
            // Now add the listener to all the sources
            foreach (PSTraceSource source in matchingSources)
            {
                source.Listeners.Add(listener);
            }
        }

        #endregion AddTraceListeners

        #region RemoveTraceListeners

        /// <summary>
        /// Removes the tracelisteners from the specified trace sources.
        /// </summary>
        internal static void RemoveListenersByName(
            Collection<PSTraceSource> matchingSources,
            string[] listenerNames,
            bool fileListenersOnly)
        {
            Collection<WildcardPattern> listenerMatcher =
                SessionStateUtilities.CreateWildcardsFromStrings(
                    listenerNames,
                    WildcardOptions.IgnoreCase);

            // Loop through all the matching sources and remove the matching listeners

            foreach (PSTraceSource source in matchingSources)
            {
                // Get the indexes of the listeners that need to be removed.
                // This is done because we cannot remove the listeners while
                // we are enumerating them.

                for (int index = source.Listeners.Count - 1; index >= 0; --index)
                {
                    TraceListener listenerToRemove = source.Listeners[index];

                    if (fileListenersOnly && listenerToRemove is not TextWriterTraceListener)
                    {
                        // Since we only want to remove file listeners, skip any that
                        // aren't file listeners
                        continue;
                    }

                    // Now match the names

                    if (SessionStateUtilities.MatchesAnyWildcardPattern(
                            listenerToRemove.Name,
                            listenerMatcher,
                            true))
                    {
                        listenerToRemove.Flush();
                        listenerToRemove.Dispose();
                        source.Listeners.RemoveAt(index);
                    }
                }
            }
        }

        #endregion RemoveTraceListeners

        #region SetTraceListenerOptions

        /// <summary>
        /// Sets the trace listener options based on the ListenerOptions parameter.
        /// </summary>
        internal void SetTraceListenerOptions(Collection<PSTraceSource> matchingSources)
        {
            // Set the trace options if they were specified
            if (traceOptionsSpecified)
            {
                foreach (PSTraceSource source in matchingSources)
                {
                    foreach (TraceListener listener in source.Listeners)
                    {
                        listener.TraceOutputOptions = this.ListenerOptionsInternal;
                    }
                }
            }
        }

        #endregion SetTraceListenerOptions

        #region SetFlags

        /// <summary>
        /// Sets the flags for all the specified TraceSources.
        /// </summary>
        internal void SetFlags(Collection<PSTraceSource> matchingSources)
        {
            foreach (PSTraceSource structuredSource in matchingSources)
            {
                structuredSource.Options = this.OptionsInternal;
            }
        }
        #endregion SetFlags

        #region TurnOnTracing

        /// <summary>
        /// Turns on tracing for the TraceSources, flags, and listeners defined by the parameters.
        /// </summary>
        internal void TurnOnTracing(Collection<PSTraceSource> matchingSources, bool preConfigured)
        {
            foreach (PSTraceSource source in matchingSources)
            {
                // Store the current state of the TraceSource
                if (!_storedTraceSourceState.ContainsKey(source))
                {
                    // Copy the listeners into a different collection

                    Collection<TraceListener> listenerCollection = new();
                    foreach (TraceListener listener in source.Listeners)
                    {
                        listenerCollection.Add(listener);
                    }

                    if (preConfigured)
                    {
                        // If the source is a preconfigured source, then the default options
                        // and listeners should be stored as the existing state.

                        _storedTraceSourceState[source] =
                            new KeyValuePair<PSTraceSourceOptions, Collection<TraceListener>>(
                                PSTraceSourceOptions.None,
                                new Collection<TraceListener>());
                    }
                    else
                    {
                        _storedTraceSourceState[source] =
                            new KeyValuePair<PSTraceSourceOptions, Collection<TraceListener>>(
                                source.Options,
                                listenerCollection);
                    }
                }

                // Now set the new flags
                source.Options = this.OptionsInternal;
            }

            // Now turn on the listeners

            AddTraceListenersToSources(matchingSources);
            SetTraceListenerOptions(matchingSources);
        }

        #endregion TurnOnTracing

        #region ResetTracing

        /// <summary>
        /// Resets tracing to the previous level for the TraceSources defined by the parameters.
        /// Note, TurnOnTracing must be called before calling ResetTracing or else all
        /// TraceSources will be turned off.
        /// </summary>
        internal void ResetTracing(Collection<PSTraceSource> matchingSources)
        {
            foreach (PSTraceSource source in matchingSources)
            {
                // First flush all the existing trace listeners

                foreach (TraceListener listener in source.Listeners)
                {
                    listener.Flush();
                }

                if (_storedTraceSourceState.TryGetValue(source, out KeyValuePair<PSTraceSourceOptions, Collection<TraceListener>> storedState))
                {
                    // Restore the TraceSource to its original state

                    source.Listeners.Clear();
                    foreach (TraceListener listener in storedState.Value)
                    {
                        source.Listeners.Add(listener);
                    }

                    source.Options = storedState.Key;
                }
                else
                {
                    // Since we don't have any stored state for this TraceSource,
                    // just turn it off.

                    source.Listeners.Clear();
                    source.Options = PSTraceSourceOptions.None;
                }
            }
        }

        #endregion ResetTracing

        #region stored state

        /// <summary>
        /// Clears the store TraceSource state.
        /// </summary>
        protected void ClearStoredState()
        {
            // First close all listeners

            foreach (KeyValuePair<PSTraceSourceOptions, Collection<TraceListener>> pair in _storedTraceSourceState.Values)
            {
                foreach (TraceListener listener in pair.Value)
                {
                    listener.Flush();
                    listener.Dispose();
                }
            }

            _storedTraceSourceState.Clear();
        }

        private readonly Dictionary<PSTraceSource, KeyValuePair<PSTraceSourceOptions, Collection<TraceListener>>> _storedTraceSourceState =
            new();

        #endregion stored state
    }
}
