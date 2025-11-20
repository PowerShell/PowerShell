// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.Development.ProjectContext
{
    /// <summary>
    /// Get-ProjectContext cmdlet detects the type of software project in the current or specified directory.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "ProjectContext")]
    [OutputType(typeof(ProjectContext))]
    [Alias("gpc")]
    public sealed class GetProjectContextCommand : PSCmdlet
    {
        /// <summary>
        /// Path to the project directory. Defaults to current directory.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

        /// <summary>
        /// Search parent directories if no project detected in current directory.
        /// </summary>
        [Parameter]
        public SwitchParameter SearchParent { get; set; }

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            string searchPath = Path;

            // Default to current directory if not specified
            if (string.IsNullOrEmpty(searchPath))
            {
                searchPath = SessionState.Path.CurrentFileSystemLocation.Path;
            }

            // Resolve path
            try
            {
                var resolvedPaths = SessionState.Path.GetResolvedPSPathFromPSPath(searchPath);
                if (resolvedPaths.Count == 0)
                {
                    WriteError(new ErrorRecord(
                        new ItemNotFoundException($"Path '{searchPath}' not found."),
                        "PathNotFound",
                        ErrorCategory.ObjectNotFound,
                        searchPath));
                    return;
                }

                searchPath = resolvedPaths[0].Path;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(
                    ex,
                    "PathResolutionError",
                    ErrorCategory.InvalidArgument,
                    searchPath));
                return;
            }

            // Detect project type
            ProjectContext context = ProjectDetector.Detect(searchPath);

            // If not found and SearchParent is specified, walk up the directory tree
            if (context == null && SearchParent.IsPresent)
            {
                var currentDir = new System.IO.DirectoryInfo(searchPath);
                while (currentDir.Parent != null)
                {
                    context = ProjectDetector.Detect(currentDir.Parent.FullName);
                    if (context != null)
                    {
                        break;
                    }
                    currentDir = currentDir.Parent;
                }
            }

            if (context == null)
            {
                WriteVerbose($"No known project type detected in '{searchPath}'");
                WriteObject(null);
            }
            else
            {
                WriteVerbose($"Detected {context.ProjectType} project in '{context.RootPath}'");
                WriteObject(context);
            }
        }
    }
}
