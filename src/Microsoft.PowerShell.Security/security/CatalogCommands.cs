// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Defines the base class from which all catalog commands are derived.
    /// </summary>
    public abstract class CatalogCommandsBase : PSCmdlet
    {
        /// <summary>
        /// Path of folder/file to generate or validate the catalog file.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "ByPath")]
        public string CatalogFilePath
        {
            get
            {
                return catalogFilePath;
            }

            set
            {
                catalogFilePath = value;
            }
        }

        private string catalogFilePath;

        /// <summary>
        /// Path of folder/file to generate or validate the catalog file.
        /// </summary>
        [Parameter(Position = 1, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "ByPath")]
        public string[] Path
        {
            get
            {
                return path;
            }

            set
            {
                path = value;
            }
        }

        private string[] path;
        //
        // name of this command
        //
        private readonly string commandName;

        /// <summary>
        /// Initializes a new instance of the CatalogCommandsBase class,
        /// using the given command name.
        /// </summary>
        /// <param name="name">
        /// The name of the command.
        /// </param>
        protected CatalogCommandsBase(string name) : base()
        {
            commandName = name;
        }

        private CatalogCommandsBase() : base() { }

        /// <summary>
        /// Processes records from the input pipeline.
        /// For each input object, the command either generate the Catalog or
        /// Validates the existing Catalog.
        /// </summary>
        protected override void ProcessRecord()
        {
            //
            // this cannot happen as we have specified the Path
            // property to be mandatory parameter
            //
            Dbg.Assert((CatalogFilePath != null) && (CatalogFilePath.Length > 0),
                       "CatalogCommands: Param binder did not bind catalogFilePath");

            Collection<string> paths = new();

            if (Path != null)
            {
                foreach (string p in Path)
                {
                    foreach (PathInfo tempPath in SessionState.Path.GetResolvedPSPathFromPSPath(p))
                    {
                        if (ShouldProcess("Including path " + tempPath.ProviderPath, string.Empty, string.Empty))
                        {
                            paths.Add(tempPath.ProviderPath);
                        }
                    }
                }
            }

            string drive = null;

            // resolve catalog destination Path
            if (!SessionState.Path.IsPSAbsolute(catalogFilePath, out drive) && !System.IO.Path.IsPathRooted(catalogFilePath))
            {
                catalogFilePath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(catalogFilePath);
            }

            if (ShouldProcess(catalogFilePath))
            {
                PerformAction(paths, catalogFilePath);
            }
        }

        /// <summary>
        /// Performs the action i.e. Generate or Validate the Windows Catalog File.
        /// </summary>
        /// <param name="path">
        /// The name of the Folder or file on which to perform the action.
        /// </param>
        /// <param name="catalogFilePath">
        /// Path to Catalog
        /// </param>
        protected abstract void PerformAction(Collection<string> path, string catalogFilePath);
    }

    /// <summary>
    /// Defines the implementation of the 'New-FileCatalog' cmdlet.
    /// This cmdlet generates the catalog for File or Folder.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "FileCatalog", SupportsShouldProcess = true, DefaultParameterSetName = "ByPath",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096596")]
    [OutputType(typeof(FileInfo))]
    public sealed class NewFileCatalogCommand : CatalogCommandsBase
    {
        /// <summary>
        /// Initializes a new instance of the New-FileCatalog class.
        /// </summary>
        public NewFileCatalogCommand() : base("New-FileCatalog") { }

        /// <summary>
        /// Catalog version.
        /// </summary>
        [Parameter]
        public int CatalogVersion
        {
            get
            {
                return catalogVersion;
            }

            set
            {
                catalogVersion = value;
            }
        }

        // Based on the Catalog version we will decide which hashing Algorithm to use
        private int catalogVersion = 2;

        /// <summary>
        /// Generate the Catalog for the Path.
        /// </summary>
        /// <param name="path">
        /// File or Folder Path
        /// </param>
        /// <param name="catalogFilePath">
        /// Path to Catalog
        /// </param>
        /// <returns>
        /// True if able to Create Catalog or else False
        /// </returns>
        protected override void PerformAction(Collection<string> path, string catalogFilePath)
        {
            if (path.Count == 0)
            {
                // if user has not provided the path use current directory to generate catalog
                path.Add(SessionState.Path.CurrentFileSystemLocation.Path);
            }

            FileInfo catalogFileInfo = new(catalogFilePath);

            // If Path points to the expected cat file make sure
            // parent Directory exists other wise CryptoAPI fails to create a .cat file
            if (catalogFileInfo.Extension.Equals(".cat", StringComparison.Ordinal))
            {
                System.IO.Directory.CreateDirectory(catalogFileInfo.Directory.FullName);
            }
            else
            {
                // This only creates Directory if it does not exists, Append a default name
                System.IO.Directory.CreateDirectory(catalogFilePath);
                catalogFilePath = System.IO.Path.Combine(catalogFilePath, "catalog.cat");
            }

            FileInfo catalogFile = CatalogHelper.GenerateCatalog(this, path, catalogFilePath, catalogVersion);

            if (catalogFile != null)
            {
                WriteObject(catalogFile);
            }
        }
    }

    /// <summary>
    /// Defines the implementation of the 'Test-FileCatalog' cmdlet.
    /// This cmdlet validates the Integrity of catalog.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "FileCatalog", SupportsShouldProcess = true, DefaultParameterSetName = "ByPath",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096921")]
    [OutputType(typeof(CatalogValidationStatus))]
    [OutputType(typeof(CatalogInformation))]
    public sealed class TestFileCatalogCommand : CatalogCommandsBase
    {
        /// <summary>
        /// Initializes a new instance of the New-FileCatalog class.
        /// </summary>
        public TestFileCatalogCommand() : base("Test-FileCatalog") { }

        /// <summary>
        /// </summary>
        [Parameter]
        public SwitchParameter Detailed
        {
            get { return detailed; }

            set { detailed = value; }
        }

        private bool detailed = false;

        /// <summary>
        /// Patterns used to exclude files from DiskPaths and Catalog.
        /// </summary>
        [Parameter]
        public string[] FilesToSkip
        {
            get
            {
                return filesToSkip;
            }

            set
            {
                filesToSkip = value;
                this.excludedPatterns = new WildcardPattern[filesToSkip.Length];
                for (int i = 0; i < filesToSkip.Length; i++)
                {
                    this.excludedPatterns[i] = WildcardPattern.Get(filesToSkip[i], WildcardOptions.IgnoreCase);
                }
            }
        }

        private string[] filesToSkip = null;
        internal WildcardPattern[] excludedPatterns = null;

        /// <summary>
        /// Validate the Integrity of given Catalog.
        /// </summary>
        /// <param name="path">
        /// File or Folder Path
        /// </param>
        /// <param name="catalogFilePath">
        /// Path to Catalog
        /// </param>
        /// <returns>
        /// True if able to Validate the Catalog and its not tampered or else False
        /// </returns>
        protected override void PerformAction(Collection<string> path, string catalogFilePath)
        {
            if (path.Count == 0)
            {
                // if user has not provided the path use the path of catalog file itself.
                path.Add(new FileInfo(catalogFilePath).Directory.FullName);
            }

            CatalogInformation catalogInfo = CatalogHelper.ValidateCatalog(this, path, catalogFilePath, excludedPatterns);

            if (detailed)
            {
                WriteObject(catalogInfo);
            }
            else
            {
                WriteObject(catalogInfo.Status);
            }
        }
    }
}

#endif
