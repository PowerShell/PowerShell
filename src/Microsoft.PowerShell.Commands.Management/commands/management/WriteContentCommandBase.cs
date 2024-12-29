// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Provider;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A base class for the commands that write content (set-content, add-content)
    /// </summary>
    public class WriteContentCommandBase : PassThroughContentCommandBase
    {
        #region Parameters

        /// <summary>
        /// The value of the content to set.
        /// </summary>
        /// <value>
        /// This value type is determined by the InvokeProvider.
        /// </value>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [AllowNull]
        [AllowEmptyCollection]
        public object[] Value
        {
            get
            {
                return _content;
            }

            set
            {
                _content = value;
            }
        }

        #endregion Parameters

        #region parameter data

        /// <summary>
        /// The value of the content to be set.
        /// </summary>
        private object[] _content;

        #endregion parameter data

        #region private Data

        /// <summary>
        /// This bool is used to determine if the path
        /// parameter was specified on the command line or via the pipeline.
        /// </summary>
        private bool _pipingPaths;

        /// <summary>
        /// True if the content writers have been open.
        /// This is used in conjunction with pipingPaths
        /// to determine if the content writers need to
        /// be closed each time ProgressRecord is called.
        /// </summary>
        private bool _contentWritersOpen;

        #endregion private Data

        #region Command code

        /// <summary>
        /// Determines if the paths are specified on the command line
        /// or being piped in.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (Path != null && Path.Length > 0)
            {
                _pipingPaths = false;
            }
            else
            {
                _pipingPaths = true;
            }
        }

        /// <summary>
        /// Appends the content to the specified item.
        /// </summary>
        protected override void ProcessRecord()
        {
            CmdletProviderContext currentContext = GetCurrentContext();

            // Initialize the content

            _content ??= Array.Empty<object>();

            if (_pipingPaths)
            {
                // Make sure to clean up the content writers that are already there

                if (contentStreams != null && contentStreams.Count > 0)
                {
                    CloseContent(contentStreams, false);
                    _contentWritersOpen = false;
                    contentStreams = new List<ContentHolder>();
                }
            }

            if (!_contentWritersOpen)
            {
                // Since the paths are being pipelined in, we have
                // to get new content writers for the new paths
                string[] paths = GetAcceptedPaths(Path, currentContext);

                if (paths.Length > 0)
                {
                    BeforeOpenStreams(paths);
                    contentStreams = GetContentWriters(paths, currentContext);
                    SeekContentPosition(contentStreams);
                }

                _contentWritersOpen = true;
            }

            // Now write the content to the item
            try
            {
                foreach (ContentHolder holder in contentStreams)
                {
                    if (holder.Writer != null)
                    {
                        IList result = null;
                        try
                        {
                            result = holder.Writer.Write(_content);
                        }
                        catch (Exception e) // Catch-all OK. 3rd party callout
                        {
                            ProviderInvocationException providerException =
                               new(
                                   "ProviderContentWriteError",
                                   SessionStateStrings.ProviderContentWriteError,
                                   holder.PathInfo.Provider,
                                   holder.PathInfo.Path,
                                   e);

                            // Log a provider health event

                            MshLog.LogProviderHealthEvent(
                                this.Context,
                                holder.PathInfo.Provider.Name,
                                providerException,
                                Severity.Warning);

                            WriteError(
                                new ErrorRecord(
                                    providerException.ErrorRecord,
                                    providerException));
                            continue;
                        }

                        if (result != null && result.Count > 0 && PassThru)
                        {
                            WriteContentObject(result, result.Count, holder.PathInfo, currentContext);
                        }
                    }
                }
            }
            finally
            {
                // Need to close all the writers if the paths are being pipelined

                if (_pipingPaths)
                {
                    CloseContent(contentStreams, false);
                    _contentWritersOpen = false;
                    contentStreams = new List<ContentHolder>();
                }
            }
        }

        /// <summary>
        /// Closes all the content writers.
        /// </summary>
        protected override void EndProcessing()
        {
            Dispose(true);
        }

        #endregion Command code

        #region protected members

        /// <summary>
        /// This method is called by the base class after getting the content writer
        /// from the provider. If the current position needs to be changed before writing
        /// the content, this method should be overridden to do that.
        /// </summary>
        /// <param name="contentHolders">
        /// The content holders that contain the writers to be moved.
        /// </param>
        internal virtual void SeekContentPosition(List<ContentHolder> contentHolders)
        {
            // default does nothing.
        }

        /// <summary>
        /// Called by the base class before the streams are open for the path.
        /// </summary>
        /// <param name="paths">
        /// The path to the items that will be opened for writing content.
        /// </param>
        internal virtual void BeforeOpenStreams(string[] paths)
        {
        }

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
            if (Path != null && Path.Length > 0)
            {
                return InvokeProvider.Content.GetContentWriterDynamicParameters(Path[0], context);
            }

            return InvokeProvider.Content.GetContentWriterDynamicParameters(".", context);
        }

        /// <summary>
        /// Gets the IContentWriters for the current path(s)
        /// </summary>
        /// <returns>
        /// An array of IContentWriters for the current path(s)
        /// </returns>
        internal List<ContentHolder> GetContentWriters(
            string[] writerPaths,
            CmdletProviderContext currentCommandContext)
        {
            // Resolve all the paths into PathInfo objects

            Collection<PathInfo> pathInfos = ResolvePaths(writerPaths, true, false, currentCommandContext);

            // Create the results array

            List<ContentHolder> results = new();

            foreach (PathInfo pathInfo in pathInfos)
            {
                // For each path, get the content writer

                Collection<IContentWriter> writers = null;

                try
                {
                    writers =
                        InvokeProvider.Content.GetWriter(
                            pathInfo.Path,
                            currentCommandContext);
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

                if (writers != null && writers.Count > 0)
                {
                    if (writers.Count == 1 && writers[0] != null)
                    {
                        ContentHolder holder =
                            new(pathInfo, null, writers[0]);

                        results.Add(holder);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Gets the list of paths accepted by the user.
        /// </summary>
        /// <param name="unfilteredPaths">The list of unfiltered paths.</param>
        /// <param name="currentContext">The current context.</param>
        /// <returns>The list of paths accepted by the user.</returns>
        private string[] GetAcceptedPaths(string[] unfilteredPaths, CmdletProviderContext currentContext)
        {
            Collection<PathInfo> pathInfos = ResolvePaths(unfilteredPaths, true, false, currentContext);

            var paths = new List<string>();

            foreach (PathInfo pathInfo in pathInfos)
            {
                if (CallShouldProcess(pathInfo.Path))
                {
                    paths.Add(pathInfo.Path);
                }
            }

            return paths.ToArray();
        }

        #endregion protected members
    }
}
