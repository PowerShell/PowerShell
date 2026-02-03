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
        /// Gets the size of the content directory.
        /// </summary>
        [Parameter]
        public SwitchParameter Size { get; set; }

        /// <summary>
        /// EndProcessing method of this cmdlet.
        /// Outputs the PSContentPath or its size information.
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
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Path")]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

        /// <summary>
        /// Resets the PSContentPath to the platform default.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "Reset")]
        public SwitchParameter Reset { get; set; }

        /// <summary>
        /// EndProcessing method of this cmdlet.
        /// Validates the path and sets the PSContentPath in the configuration.
        /// </summary>
        protected override void EndProcessing()
        {
            if (Reset)
            {
                ResetToDefault();
                return;
            }

            // Validate the path
            if (!ValidatePath(Path))
            {
                return;
            }

            if (ShouldProcess($"PSContentPath = {Path}", "Set PSContentPath"))
            {
                try
                {
                    PowerShellConfig.Instance.SetPSContentPath(Path);

                    // Update the $PSUserContentPath readonly variable in the current session
                    string expandedPath = Environment.ExpandEnvironmentVariables(Path);
                    UpdatePSUserContentPathVariable(expandedPath);

                    WriteVerbose($"Successfully set PSContentPath to '{Path}'");
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(
                        ex,
                        "SetPSContentPathFailed",
                        ErrorCategory.WriteError,
                        Path));
                }
            }
        }

        /// <summary>
        /// Resets the PSContentPath to the platform default by clearing the custom config.
        /// </summary>
        private void ResetToDefault()
        {
            string defaultPath = Platform.DefaultPSContentDirectory;

            if (ShouldProcess($"PSContentPath = {defaultPath}", "Reset PSContentPath to default"))
            {
                try
                {
                    // Clear the custom path from config (passing null/empty removes the key)
                    PowerShellConfig.Instance.SetPSContentPath(null);

                    // Update the variable to the platform default
                    UpdatePSUserContentPathVariable(defaultPath);

                    WriteVerbose($"Successfully reset PSContentPath to default: '{defaultPath}'");
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(
                        ex,
                        "ResetPSContentPathFailed",
                        ErrorCategory.WriteError,
                        null));
                }
            }
        }

        /// <summary>
        /// Updates the $PSUserContentPath readonly variable in the current session.
        /// </summary>
        /// <param name="newPath">The new path value to set.</param>
        private void UpdatePSUserContentPathVariable(string newPath)
        {
            // Get the existing PSUserContentPathVariable and update its internal value
            var existingVariable = SessionState.PSVariable.Get(SpecialVariables.PSUserContentPath);
            if (existingVariable is PSUserContentPathVariable contentPathVariable)
            {
                contentPathVariable.UpdateValue(newPath);
            }
            else
            {
                // Fallback: create a new PSUserContentPathVariable (shouldn't normally happen)
                var variable = new PSUserContentPathVariable(newPath);
                SessionState.Internal.SetVariableAtScope(variable, "global", force: true, CommandOrigin.Internal);
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
}
