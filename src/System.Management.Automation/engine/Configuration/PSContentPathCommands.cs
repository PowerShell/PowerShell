// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Configuration;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implements Get-PSContentPath cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PSContentPath", HelpUri = "https://go.microsoft.com/fwlink/?linkid=2344910")]
    [OutputType(typeof(string), typeof(PSObject))]
    public class GetPSContentPathCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets whether to include the size of the content directory.
        /// </summary>
        [Parameter]
        public SwitchParameter Size { get; set; }

        /// <summary>
        /// EndProcessing method of this cmdlet.
        /// Main logic is in EndProcessing to ensure all pipeline input is processed first.
        /// </summary>
        protected override void EndProcessing()
        {
            try
            {
                var psContentPath = Utils.GetPSContentPath();

                if (Size)
                {
                    // Calculate directory size
                    long totalSize = 0;
                    int fileCount = 0;
                    int directoryCount = 0;

                    if (Directory.Exists(psContentPath))
                    {
                        try
                        {
                            var files = Directory.GetFiles(psContentPath, "*", SearchOption.AllDirectories);
                            foreach (var file in files)
                            {
                                try
                                {
                                    var fileInfo = new FileInfo(file);
                                    totalSize += fileInfo.Length;
                                    fileCount++;
                                }
                                catch (UnauthorizedAccessException)
                                {
                                    // Skip files we can't access
                                    WriteVerbose($"Skipping inaccessible file: {file}");
                                }
                                catch (FileNotFoundException)
                                {
                                    // Skip files that were deleted during enumeration
                                    WriteVerbose($"Skipping deleted file: {file}");
                                }
                            }

                            directoryCount = Directory.GetDirectories(psContentPath, "*", SearchOption.AllDirectories).Length;
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            WriteWarning($"Unable to calculate full size: {ex.Message}");
                        }
                    }

                    // Create output object with path and size information
                    var result = new PSObject();
                    result.Properties.Add(new PSNoteProperty("Path", psContentPath));
                    result.Properties.Add(new PSNoteProperty("SizeBytes", totalSize));
                    result.Properties.Add(new PSNoteProperty("SizeMB", Math.Round(totalSize / 1024.0 / 1024.0, 2)));
                    result.Properties.Add(new PSNoteProperty("SizeGB", Math.Round(totalSize / 1024.0 / 1024.0 / 1024.0, 2)));
                    result.Properties.Add(new PSNoteProperty("Files", fileCount));
                    result.Properties.Add(new PSNoteProperty("Directories", directoryCount));
                    
                    WriteObject(result);
                }
                else
                {
                    WriteObject(psContentPath);
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(
                    ex,
                    "GetPSContentPathFailed",
                    ErrorCategory.ReadError,
                    null));
            }
        }
    }

    /// <summary>
    /// Implements Set-PSContentPath cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PSContentPath", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?linkid=2344807")]
    public class SetPSContentPathCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets the PSContentPath to configure.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

        private string validatedPath = null;

        /// <summary>
        /// ProcessRecord method of this cmdlet.
        /// Validates each path from the pipeline and stores the last valid one.
        /// </summary>
        protected override void ProcessRecord()
        {
            // Validate the path from pipeline input
            if (ValidatePath(Path))
            {
                // Store the last valid path from pipeline
                validatedPath = Path;
            }
        }

        /// <summary>
        /// EndProcessing method of this cmdlet.
        /// Main logic is in EndProcessing to use the last valid path from the pipeline.
        /// </summary>
        protected override void EndProcessing()
        {
            // If no valid path was found, exit early
            if (validatedPath == null)
            {
                return;
            }

            if (ShouldProcess($"PSContentPath = {validatedPath}", "Set PSContentPath"))
            {
                try
                {
                    PowerShellConfig.Instance.SetPSContentPath(validatedPath);
                    WriteVerbose($"Successfully set PSContentPath to '{validatedPath}'");
                    WriteWarning("PSContentPath changes will take effect after restarting PowerShell.");
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(
                        ex,
                        "SetPSContentPathFailed",
                        ErrorCategory.WriteError,
                        validatedPath));
                }
            }
        }

        /// <summary>
        /// Validates that the provided path is a valid directory path.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <returns>True if the path is valid, false otherwise.</returns>
        private bool ValidatePath(string path)
        {
            try
            {
                // Expand environment variables if present
                string expandedPath = Environment.ExpandEnvironmentVariables(path);

                // Check if the path contains invalid characters using PowerShell's existing utility
                if (PathUtils.ContainsInvalidPathChars(expandedPath))
                {
                    WriteError(new ErrorRecord(
                        new ArgumentException($"The path '{path}' contains invalid characters."),
                        "InvalidPathCharacters",
                        ErrorCategory.InvalidArgument,
                        path));
                    return false;
                }

                // Check if the path is rooted (absolute path)
                if (!System.IO.Path.IsPathRooted(expandedPath))
                {
                    WriteError(new ErrorRecord(
                        new ArgumentException($"The path '{path}' must be an absolute path."),
                        "RelativePathNotAllowed",
                        ErrorCategory.InvalidArgument,
                        path));
                    return false;
                }

                // Try to get the full path to validate format
                string fullPath = System.IO.Path.GetFullPath(expandedPath);

                // Warn if the directory doesn't exist, but don't fail
                if (!Directory.Exists(fullPath))
                {
                    WriteWarning($"The directory '{fullPath}' does not exist. It will be created when needed.");
                }

                return true;
            }
            catch (ArgumentException ex)
            {
                WriteError(new ErrorRecord(
                    ex,
                    "InvalidPathFormat",
                    ErrorCategory.InvalidArgument,
                    path));
                return false;
            }
            catch (System.Security.SecurityException ex)
            {
                WriteError(new ErrorRecord(
                    ex,
                    "PathAccessDenied",
                    ErrorCategory.PermissionDenied,
                    path));
                return false;
            }
            catch (NotSupportedException ex)
            {
                WriteError(new ErrorRecord(
                    ex,
                    "PathNotSupported",
                    ErrorCategory.InvalidArgument,
                    path));
                return false;
            }
            catch (PathTooLongException ex)
            {
                WriteError(new ErrorRecord(
                    ex,
                    "PathTooLong",
                    ErrorCategory.InvalidArgument,
                    path));
                return false;
            }
        }
    }

    /// <summary>
    /// Implements Move-PSContent cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Move, "PSContent", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High, HelpUri = "https://go.microsoft.com/fwlink/?linkid=2344811")]
    public class MovePSContentCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets the source location.
        /// Must be 'OneDrive' or 'LocalAppData'.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateSet("OneDrive", "LocalAppData")]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the destination location.
        /// Must be 'OneDrive' or 'LocalAppData'.
        /// </summary>
        [Parameter(Mandatory = true, Position = 1)]
        [ValidateSet("OneDrive", "LocalAppData")]
        public string Destination { get; set; }

        /// <summary>
        /// Gets or sets whether to copy instead of move (preserves original).
        /// </summary>
        [Parameter]
        public SwitchParameter Copy { get; set; }

        /// <summary>
        /// Gets or sets whether to force overwrite of existing files.
        /// </summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        private string sourcePath;
        private string destinationPath;

        /// <summary>
        /// BeginProcessing method - validates paths and prepares for migration.
        /// </summary>
        protected override void BeginProcessing()
        {
            // Resolve source path
            string sourceLocationString = ResolveLocation(Path);
            if (string.IsNullOrEmpty(sourceLocationString))
            {
                ThrowTerminatingError(new ErrorRecord(
                    new ArgumentException($"Could not resolve source location '{Path}'."),
                    "InvalidSourceLocation",
                    ErrorCategory.InvalidArgument,
                    Path));
                return;
            }

            // Use GetUnresolvedProviderPathFromPSPath to convert PS paths to file system paths
            sourcePath = GetUnresolvedProviderPathFromPSPath(sourceLocationString);

            // Resolve destination path
            string destinationLocationString = ResolveLocation(Destination);
            destinationPath = GetUnresolvedProviderPathFromPSPath(destinationLocationString);

            // Validate paths
            if (!Directory.Exists(sourcePath))
            {
                ThrowTerminatingError(new ErrorRecord(
                    new DirectoryNotFoundException($"Source directory '{sourcePath}' does not exist."),
                    "SourceDirectoryNotFound",
                    ErrorCategory.ObjectNotFound,
                    sourcePath));
                return;
            }

            // Check if source and destination are the same
            string normalizedSource = System.IO.Path.GetFullPath(sourcePath).TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
            string normalizedDest = System.IO.Path.GetFullPath(destinationPath).TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);

            if (string.Equals(normalizedSource, normalizedDest, StringComparison.OrdinalIgnoreCase))
            {
                ThrowTerminatingError(new ErrorRecord(
                    new ArgumentException("Source and destination paths are the same."),
                    "SameSourceAndDestination",
                    ErrorCategory.InvalidArgument,
                    destinationPath));
                return;
            }

            WriteVerbose($"Source path: {sourcePath}");
            WriteVerbose($"Destination path: {destinationPath}");

            // Check if destination exists and has content
            if (Directory.Exists(destinationPath))
            {
                var existingItems = Directory.GetFileSystemEntries(destinationPath);
                if (existingItems.Length > 0 && !Force)
                {
                    ThrowTerminatingError(new ErrorRecord(
                        new InvalidOperationException($"Destination directory '{destinationPath}' already contains {existingItems.Length} items. Use -Force to merge/overwrite."),
                        "DestinationNotEmpty",
                        ErrorCategory.ResourceExists,
                        destinationPath));
                    return;
                }
                
                if (existingItems.Length > 0)
                {
                    WriteWarning($"Destination directory contains {existingItems.Length} items. Existing files may be overwritten.");
                }
            }
        }

        /// <summary>
        /// ProcessRecord method - performs the migration.
        /// </summary>
        protected override void ProcessRecord()
        {
            // Check if source directory exists
            if (!Directory.Exists(sourcePath))
            {
                WriteWarning($"Source directory does not exist: '{sourcePath}'");
                return;
            }

            // Check if source has any content
            if (Directory.GetFileSystemEntries(sourcePath).Length == 0)
            {
                WriteWarning($"Source directory is empty: '{sourcePath}'");
                return;
            }

            string action = Copy ? "Copy" : "Move";
            string sourcePattern = System.IO.Path.Combine(sourcePath, "*");
            
            if (ShouldProcess($"content from '{sourcePath}' to '{destinationPath}'",
                             $"{action} PowerShell content"))
            {
                try
                {
                    var copyCmd = InvokeCommand.NewScriptBlock(@"
                        param($source, $dest, $force)
                        Get-ChildItem -LiteralPath $source | Copy-Item -Destination $dest -Recurse -Force:$force
                    ");
                    
                    copyCmd.InvokeWithContext(null, new List<PSVariable>(), sourcePath, destinationPath, Force.ToBool());

                    if (!Copy)
                    {
                        // Remove contents of source directory for move operation
                        WriteVerbose($"Removing contents of source directory: {sourcePath}");
                        foreach (var file in Directory.GetFiles(sourcePath))
                        {
                            File.Delete(file);
                        }
                        foreach (var dir in Directory.GetDirectories(sourcePath))
                        {
                            Directory.Delete(dir, recursive: true);
                        }
                    }

                    int itemCount = Directory.GetFileSystemEntries(destinationPath, "*", SearchOption.AllDirectories).Length;
                    WriteObject($"Successfully {(Copy ? "copied" : "moved")} PowerShell content. Destination contains {itemCount} items.");
                                                                
                    // Update PSContentPath to point to new location
                    PowerShellConfig.Instance.SetPSContentPath(destinationPath);
                    WriteWarning($"PSContentPath has been updated to '{destinationPath}'.");
                    WriteWarning("Please restart PowerShell for the changes to take full effect.");

                    if (!Copy)
                    {
                        WriteVerbose("All items migrated successfully. Source directory has been emptied.");
                    }
                }
                catch (Exception ex)
                {
                    ThrowTerminatingError(new ErrorRecord(
                        ex,
                        "MigrationFailed",
                        ErrorCategory.WriteError,
                        destinationPath));
                }
            }
        }

        /// <summary>
        /// Resolves a location name to an actual path.
        /// </summary>
        private static string ResolveLocation(string location)
        {
            switch (location.ToLowerInvariant())
            {
                case "onedrive":
                    // OneDrive is the platform default location (Documents\PowerShell)
                    return Platform.DefaultPSContentDirectory;

                case "localappdata":
                    // LocalAppData location
                    return Platform.LocalAppDataPSContentDirectory;

                default:
                    // Should never reach here due to ValidateSet
                    throw new ArgumentException($"Invalid location: {location}");
            }
        }
    }
}
