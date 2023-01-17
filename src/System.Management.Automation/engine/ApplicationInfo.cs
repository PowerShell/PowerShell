// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// Provides information for applications that are not directly executable by PowerShell.
    /// </summary>
    /// <remarks>
    /// An application is any file that is executable by Windows either directly or through
    /// file associations excluding any .ps1 files or cmdlets.
    /// </remarks>
    public class ApplicationInfo : CommandInfo
    {
        #region ctor

        /// <summary>
        /// Creates an instance of the ApplicationInfo class with the specified name, and path.
        /// </summary>
        /// <param name="name">
        /// The name of the application.
        /// </param>
        /// <param name="path">
        /// The path to the application executable
        /// </param>
        /// <param name="context">
        /// THe engine execution context for this command...
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="path"/> or <paramref name="name"/> is null or empty
        /// or contains one or more of the invalid
        /// characters defined in InvalidPathChars.
        /// </exception>
        internal ApplicationInfo(string name, string path, ExecutionContext context) : base(name, CommandTypes.Application)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentException(nameof(path));
            }

            if (context == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(context));
            }

            Path = path;
            Extension = System.IO.Path.GetExtension(path);
            _context = context;

            if (ExperimentalFeature.IsEnabled("PSNativeJsonAdapter"))
            {
                // Look for a json adapter.
                // These take the shape of name-json.extension
                FindJsonAdapter();
            }
        }

        private readonly ExecutionContext _context;
        #endregion ctor

        /// <summary>
        /// Gets the path for the application file.
        /// </summary>
        public string Path { get; } = string.Empty;

        /// <summary>
        /// Gets the extension of the application file.
        /// </summary>
        public string Extension { get; } = string.Empty;

        /// <summary>
        /// The Json adapter for this application.
        /// If this is not null, the adapter will be added to the pipeline following the application.
        /// </summary>
        public CommandInfo JsonAdapter { get; private set; } = null;

        /// <summary>
        /// Gets the path of the application file.
        /// </summary>
        public override string Definition
        {
            get
            {
                return Path;
            }
        }

        /// <summary>
        /// Gets the source of this command.
        /// </summary>
        public override string Source
        {
            get { return this.Definition; }
        }

        /// <summary>
        /// Gets the source version.
        /// </summary>
        public override Version Version
        {
            get
            {
                if (_version == null)
                {
                    FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(Path);
                    _version = new Version(versionInfo.ProductMajorPart, versionInfo.ProductMinorPart, versionInfo.ProductBuildPart, versionInfo.ProductPrivatePart);
                }

                return _version;
            }
        }

        private Version _version;

        /// <summary>
        /// Determine the visibility for this script...
        /// </summary>
        public override SessionStateEntryVisibility Visibility
        {
            get
            {
                return _context.EngineSessionState.CheckApplicationVisibility(Path);
            }

            set
            {
                throw PSTraceSource.NewNotImplementedException();
            }
        }

        /// <summary>
        /// An application could return nothing, but commonly it returns a string.
        /// </summary>
        public override ReadOnlyCollection<PSTypeName> OutputType
        {
            get
            {
                if (_outputType == null)
                {
                    List<PSTypeName> l = new List<PSTypeName>();
                    l.Add(new PSTypeName(typeof(string)));
                    _outputType = new ReadOnlyCollection<PSTypeName>(l);
                }

                return _outputType;
            }
        }

        private ReadOnlyCollection<PSTypeName> _outputType = null;

        /// <summary>
        /// Search for a Json adapter for this application.
        /// It will have the shape of name-json.extension, it can be any type of command.
        /// </summary>
        private void FindJsonAdapter()
        {
            string jsonAdapterName = string.Format("{0}-json", System.IO.Path.GetFileNameWithoutExtension(this.Path));
            JsonAdapter = _context.SessionState.InvokeCommand.GetCommand(jsonAdapterName, CommandTypes.All);
            return;
        }
    }
}
