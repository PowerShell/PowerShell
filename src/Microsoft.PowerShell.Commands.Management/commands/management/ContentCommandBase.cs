// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Provider;

using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The base class for the */content commands.
    /// </summary>
    public class ContentCommandBase : CoreCommandWithCredentialsBase, IDisposable
    {
        #region Parameters

        /// <summary>
        /// Gets or sets the path parameter to the command.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = "Path",
                   Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string[] Path { get; set; }

        /// <summary>
        /// Gets or sets the literal path parameter to the command.
        /// </summary>
        [Parameter(ParameterSetName = "LiteralPath",
                   Mandatory = true, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        public string[] LiteralPath
        {
            get { return Path; }

            set
            {
                base.SuppressWildcardExpansion = true;
                Path = value;
            }
        }

        /// <summary>
        /// Gets or sets the filter property.
        /// </summary>
        [Parameter]
        public override string Filter
        {
            get { return base.Filter; }

            set { base.Filter = value; }
        }

        /// <summary>
        /// Gets or sets the include property.
        /// </summary>
        [Parameter]
        public override string[] Include
        {
            get { return base.Include; }

            set { base.Include = value; }
        }

        /// <summary>
        /// Gets or sets the exclude property.
        /// </summary>
        [Parameter]
        public override string[] Exclude
        {
            get { return base.Exclude; }

            set { base.Exclude = value; }
        }

        /// <summary>
        /// Gets or sets the force property.
        /// </summary>
        /// <remarks>
        /// Gives the provider guidance on how vigorous it should be about performing
        /// the operation. If true, the provider should do everything possible to perform
        /// the operation. If false, the provider should attempt the operation but allow
        /// even simple errors to terminate the operation.
        /// For example, if the user tries to copy a file to a path that already exists and
        /// the destination is read-only, if force is true, the provider should copy over
        /// the existing read-only file. If force is false, the provider should write an error.
        /// </remarks>
        [Parameter]
        public override SwitchParameter Force
        {
            get { return base.Force; }

            set { base.Force = value; }
        }

        #endregion Parameters

        #region parameter data

        #endregion parameter data

        #region protected members

        /// <summary>
        /// An array of content holder objects that contain the path information
        /// and content readers/writers for the item represented by the path information.
        /// </summary>
        internal List<ContentHolder> contentStreams = new List<ContentHolder>();

        /// <summary>
        /// Wraps the content into a PSObject and adds context information as notes.
        /// </summary>
        /// <param name="content">
        /// The content being written out.
        /// </param>
        /// <param name="readCount">
        /// The number of blocks that have been read so far.
        /// </param>
        /// <param name="pathInfo">
        /// The context the content was retrieved from.
        /// </param>
        /// <param name="context">
        /// The context the command is being run under.
        /// </param>
        internal void WriteContentObject(object content, long readCount, PathInfo pathInfo, CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                content != null,
                "The caller should verify the content.");

            Dbg.Diagnostics.Assert(
                pathInfo != null,
                "The caller should verify the pathInfo.");

            Dbg.Diagnostics.Assert(
                context != null,
                "The caller should verify the context.");

            if (_currentContentItem == null
                || (_currentContentItem.PathInfo != pathInfo
                    &&
                    string.Compare(pathInfo.Path,_currentContentItem.PathInfo.Path, StringComparison.OrdinalIgnoreCase) != 0)
                )
            {
                // Generate a new cache item and cache the notes
                string parentPath = null;
                string childName = null;
                try
                {
                    var root = pathInfo.Drive != null ? pathInfo.Drive.Root : string.Empty;
                    parentPath = SessionState.Path.ParseParent(pathInfo.Path, root, context);
                    childName = SessionState.Path.ParseChildName(pathInfo.Path, context);

                }
                catch (NotSupportedException)
                {
                    // Ignore. The object just won't have ParentPath or ChildName set.
                }

                _currentContentItem = new ContentPathsCache(pathInfo)
                  {
                      PSPath = pathInfo.Path,
                      ParentPath = parentPath,
                      ChildName = childName,
                      Drive = pathInfo.Drive,
                      Provider = pathInfo.Provider
                  };
            }

            if (ExperimentalFeature.IsEnabled("PSOptimizedProviderObjects"))
            {
                WriteObject(new ContentPSObject(content, pathInfo, _currentContentItem.ParentPath, _currentContentItem.ChildName, readCount));
                return;
            }

            PSObject result = PSObject.AsPSObject(content);
            Dbg.Diagnostics.Assert(
                result != null,
                "A PSObject should always be constructed.");

            // Use the cached notes if the cache exists and the path is still the same
            _currentContentItem.AttachNotes(result);
            WriteObject(result);
        }

        internal class ContentPSObject : PSObject, IPSObjectExtendedMemberInfo
        {
            private readonly PathInfo _pathInfo;
            private readonly string _parentPath;
            private readonly string _childName;
            private readonly long _readCount;

            public ContentPSObject(object obj, PathInfo pathInfo, string parentPath, string childName, long readCount) : base(obj)
            {
                _pathInfo = pathInfo;
                _parentPath = parentPath;
                _childName = childName;
                _readCount = readCount;
            }

            [PSExtensionMember]
            public PSDriveInfo PSDrive => _pathInfo.Drive;

            [PSExtensionMember]
            public string PSParentPath => _parentPath;

            [PSExtensionMember]
            public string PSPath => LocationGlobber.GetProviderQualifiedPath(_pathInfo.Path, _pathInfo.Provider);

            [PSExtensionMember]
            public string PSChildName => _childName;

            [PSExtensionMember]
            public long ReadCount => _readCount;

            [PSExtensionMember]
            public ProviderInfo PSProvider => _pathInfo.Provider;

            public T GetFirstOrDefault<T>(MemberNamePredicate predicate) where T : PSMemberInfo
            {
                string GetMatchedPropertyName(MemberNamePredicate pred)
                {
                    if (pred(nameof(PSParentPath)))
                        return nameof(PSParentPath);
                    if (pred(nameof(PSPath)))
                        return nameof(PSPath);
                    if (pred(nameof(PSChildName)))
                        return nameof(PSChildName);
                    if (pred(nameof(ReadCount)))
                        return nameof(ReadCount);
                    if (pred(nameof(PSProvider)))
                        return nameof(PSProvider);
                    return null;
                }

                var propertyName = GetMatchedPropertyName(predicate);
                return propertyName == null ? null : DotNetInstanceAdapter.GetDotNetProperty<T>(this, propertyName);
            }

            T IPSObjectExtendedMemberInfo.GetMember<T>(string name) => DotNetInstanceAdapter.GetDotNetProperty<T>(this, name);

            void IPSObjectExtendedMemberInfo.AddExtensionMembers<T>(PSMemberInfoInternalCollection<T> returnValue) => DotNetInstanceAdapter.AddExtensionProperties(this, returnValue);
        }

        /// <summary>
        /// A cache of the notes that get added to the content items as they are written
        /// to the pipeline.
        /// </summary>
        private ContentPathsCache _currentContentItem;

        /// <summary>
        /// A class that stores a cache of the notes that get attached to content items
        /// as they get written to the pipeline. An instance of this cache class is
        /// only valid for a single path.
        /// </summary>
        internal class ContentPathsCache
        {
            /// <summary>
            /// Constructs a content cache item.
            /// </summary>
            /// <param name="pathInfo">
            /// The path information for which the cache will be bound.
            /// </param>
            public ContentPathsCache(PathInfo pathInfo)
            {
                PathInfo = pathInfo;
            }

            /// <summary>
            /// The path information for the cached item.
            /// </summary>
            public PathInfo PathInfo { get; }

            /// <summary>
            /// The cached PSPath of the item.
            /// </summary>
            public string PSPath { get; set; }

            /// <summary>
            /// The cached parent path of the item.
            /// </summary>
            public string ParentPath { get; set; }

            /// <summary>
            /// The cached drive for the item.
            /// </summary>
            public PSDriveInfo Drive { get; set; }

            /// <summary>
            /// The cached provider of the item.
            /// </summary>
            public ProviderInfo Provider { get; set; }

            /// <summary>
            /// The cached child name of the item.
            /// </summary>
            public string ChildName { get; set; }

            /// <summary>
            /// Attaches the cached notes to the specified PSObject.
            /// </summary>
            /// <param name="content">
            /// The PSObject to attached the cached notes to.
            /// </param>
            /// <returns>
            /// The PSObject that was passed in with the cached notes added.
            /// </returns>
            public PSObject AttachNotes(PSObject content)
            {
                // Construct a provider qualified path as the Path note

                PSNoteProperty note = new PSNoteProperty("PSPath", PSPath);
                content.Properties.Add(note, true);
                tracer.WriteLine("Attaching {0} = {1}", "PSPath", PSPath);

                // Now attach the parent path and child name

                note = new PSNoteProperty("PSParentPath", ParentPath);
                content.Properties.Add(note, true);
                tracer.WriteLine("Attaching {0} = {1}", "PSParentPath", ParentPath);

                // Attach the child name

                note = new PSNoteProperty("PSChildName", ChildName);
                content.Properties.Add(note, true);
                tracer.WriteLine("Attaching {0} = {1}", "PSChildName", ChildName);

                // PSDriveInfo

                if (PathInfo.Drive != null)
                {
                    note = new PSNoteProperty("PSDrive", Drive);
                    content.Properties.Add(note, true);
                    tracer.WriteLine("Attaching {0} = {1}", "PSDrive", Drive);
                }

                // ProviderInfo

                note = new PSNoteProperty("PSProvider", Provider);
                content.Properties.Add(note, true);
                tracer.WriteLine("Attaching {0} = {1}", "PSProvider", Provider);

                return content;
            }
        }

        /// <summary>
        /// A struct to hold the path information and the content readers/writers
        /// for an item.
        /// </summary>
        internal struct ContentHolder
        {
            internal ContentHolder(
                PathInfo pathInfo,
                IContentReader reader,
                IContentWriter writer)
            {
                if (pathInfo == null)
                {
                    throw PSTraceSource.NewArgumentNullException("pathInfo");
                }

                PathInfo = pathInfo;
                Reader = reader;
                Writer = writer;
            }

            internal PathInfo PathInfo { get; }

            internal IContentReader Reader { get; }

            internal IContentWriter Writer { get; }
        }

        /// <summary>
        /// Closes the content readers and writers in the content holder array.
        /// </summary>
        internal void CloseContent(List<ContentHolder> contentHolders, bool disposing)
        {
            if (contentHolders == null)
            {
                throw PSTraceSource.NewArgumentNullException("contentHolders");
            }

            foreach (ContentHolder holder in contentHolders)
            {
                try
                {
                    if (holder.Writer != null)
                    {
                        holder.Writer.Close();
                    }
                }
                catch (Exception e) // Catch-all OK. 3rd party callout
                {
                    // Catch all the exceptions caused by closing the writer
                    // and write out an error.

                    ProviderInvocationException providerException =
                        new ProviderInvocationException(
                            "ProviderContentCloseError",
                            SessionStateStrings.ProviderContentCloseError,
                            holder.PathInfo.Provider,
                            holder.PathInfo.Path,
                            e);

                    // Log a provider health event

                    MshLog.LogProviderHealthEvent(
                        this.Context,
                        holder.PathInfo.Provider.Name,
                        providerException,
                        Severity.Warning);

                    if (!disposing)
                    {
                        WriteError(
                            new ErrorRecord(
                                providerException.ErrorRecord,
                                providerException));
                    }
                }

                try
                {
                    if (holder.Reader != null)
                    {
                        holder.Reader.Close();
                    }
                }
                catch (Exception e) // Catch-all OK. 3rd party callout
                {
                    // Catch all the exceptions caused by closing the writer
                    // and write out an error.

                    ProviderInvocationException providerException =
                        new ProviderInvocationException(
                            "ProviderContentCloseError",
                            SessionStateStrings.ProviderContentCloseError,
                            holder.PathInfo.Provider,
                            holder.PathInfo.Path,
                            e);

                    // Log a provider health event

                    MshLog.LogProviderHealthEvent(
                        this.Context,
                        holder.PathInfo.Provider.Name,
                        providerException,
                        Severity.Warning);

                    if (!disposing)
                    {
                        WriteError(
                            new ErrorRecord(
                                providerException.ErrorRecord,
                                providerException));
                    }
                }
            }
        }

        /// <summary>
        /// Overridden by derived classes to support ShouldProcess with
        /// the appropriate information.
        /// </summary>
        /// <param name="path">
        /// The path to the item from which the content writer will be
        /// retrieved.
        /// </param>
        /// <returns>
        /// True if the action should continue or false otherwise.
        /// </returns>
        internal virtual bool CallShouldProcess(string path)
        {
            return true;
        }

        /// <summary>
        /// Gets the IContentReaders for the current path(s)
        /// </summary>
        /// <returns>
        /// An array of IContentReaders for the current path(s)
        /// </returns>
        internal List<ContentHolder> GetContentReaders(
            string[] readerPaths,
            CmdletProviderContext currentCommandContext)
        {
            // Resolve all the paths into PathInfo objects

            Collection<PathInfo> pathInfos = ResolvePaths(readerPaths, false, true, currentCommandContext);

            // Create the results array

            List<ContentHolder> results = new List<ContentHolder>();

            foreach (PathInfo pathInfo in pathInfos)
            {
                // For each path, get the content writer

                Collection<IContentReader> readers = null;

                try
                {
                    string pathToProcess = WildcardPattern.Escape(pathInfo.Path);

                    if (currentCommandContext.SuppressWildcardExpansion)
                    {
                        pathToProcess = pathInfo.Path;
                    }

                    readers =
                        InvokeProvider.Content.GetReader(pathToProcess, currentCommandContext);
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

                if (readers != null && readers.Count > 0)
                {
                    if (readers.Count == 1 && readers[0] != null)
                    {
                        ContentHolder holder =
                            new ContentHolder(pathInfo, readers[0], null);

                        results.Add(holder);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Resolves the specified paths to PathInfo objects.
        /// </summary>
        /// <param name="pathsToResolve">
        /// The paths to be resolved. Each path may contain glob characters.
        /// </param>
        /// <param name="allowNonexistingPaths">
        /// If true, resolves the path even if it doesn't exist.
        /// </param>
        /// <param name="allowEmptyResult">
        /// If true, allows a wildcard that returns no results.
        /// </param>
        /// <param name="currentCommandContext">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// An array of PathInfo objects that are the resolved paths for the
        /// <paramref name="pathsToResolve"/> parameter.
        /// </returns>
        internal Collection<PathInfo> ResolvePaths(
            string[] pathsToResolve,
            bool allowNonexistingPaths,
            bool allowEmptyResult,
            CmdletProviderContext currentCommandContext)
        {
            Collection<PathInfo> results = new Collection<PathInfo>();

            foreach (string path in pathsToResolve)
            {
                bool pathNotFound = false;
                bool filtersHidPath = false;

                ErrorRecord pathNotFoundErrorRecord = null;

                try
                {
                    // First resolve each of the paths
                    Collection<PathInfo> pathInfos =
                        SessionState.Path.GetResolvedPSPathFromPSPath(
                            path,
                            currentCommandContext);

                    if (pathInfos.Count == 0)
                    {
                        pathNotFound = true;

                        // If the item simply did not exist,
                        // we would have got an ItemNotFoundException.
                        // If we get here, it's because the filters
                        // excluded the file.
                        if (!currentCommandContext.SuppressWildcardExpansion)
                        {
                            filtersHidPath = true;
                        }
                    }

                    foreach (PathInfo pathInfo in pathInfos)
                    {
                        results.Add(pathInfo);
                    }
                }
                catch (PSNotSupportedException notSupported)
                {
                    WriteError(
                        new ErrorRecord(
                            notSupported.ErrorRecord,
                            notSupported));
                }
                catch (DriveNotFoundException driveNotFound)
                {
                    WriteError(
                        new ErrorRecord(
                            driveNotFound.ErrorRecord,
                            driveNotFound));
                }
                catch (ProviderNotFoundException providerNotFound)
                {
                    WriteError(
                        new ErrorRecord(
                            providerNotFound.ErrorRecord,
                            providerNotFound));
                }
                catch (ItemNotFoundException pathNotFoundException)
                {
                    pathNotFound = true;
                    pathNotFoundErrorRecord = new ErrorRecord(pathNotFoundException.ErrorRecord, pathNotFoundException);
                }

                if (pathNotFound)
                {
                    if (allowNonexistingPaths &&
                        (!filtersHidPath) &&
                        (currentCommandContext.SuppressWildcardExpansion ||
                        (!WildcardPattern.ContainsWildcardCharacters(path))))
                    {
                        ProviderInfo provider = null;
                        PSDriveInfo drive = null;
                        string unresolvedPath =
                            SessionState.Path.GetUnresolvedProviderPathFromPSPath(
                                path,
                                currentCommandContext,
                                out provider,
                                out drive);

                        PathInfo pathInfo =
                            new PathInfo(
                                drive,
                                provider,
                                unresolvedPath,
                                SessionState);
                        results.Add(pathInfo);
                    }
                    else
                    {
                        if (pathNotFoundErrorRecord == null)
                        {
                            // Detect if the path resolution failed to resolve to a file.
                            string error = StringUtil.Format(NavigationResources.ItemNotFound, Path);
                            Exception e = new Exception(error);

                            pathNotFoundErrorRecord = new ErrorRecord(
                                e,
                                "ItemNotFound",
                                ErrorCategory.ObjectNotFound,
                                Path);
                        }

                        WriteError(pathNotFoundErrorRecord);
                    }
                }
            }

            return results;
        }

        #endregion protected members

        #region IDisposable

        internal void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                CloseContent(contentStreams, true);
                contentStreams = new List<ContentHolder>();
            }
        }

        /// <summary>
        /// Dispose method in IDisposable.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~ContentCommandBase()
        {
            Dispose(false);
        }
        #endregion IDisposable

    }
}
