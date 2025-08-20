// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Configuration;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implements Get-PSContentPath cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PSContentPath", HelpUri = "https://go.microsoft.com/fwlink/?linkid=2096787")]
    public class GetPSContentPathCommand : PSCmdlet
    {
        /// <summary>
        /// ProcessRecord method of this cmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                var psContentPath = Utils.GetPSContentPath();
                WriteObject(psContentPath);
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
    [Cmdlet(VerbsCommon.Set, "PSContentPath", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?linkid=2096787")]
    public class SetPSContentPathCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets the PSContentPath to configure.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

        /// <summary>
        /// ProcessRecord method of this cmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            // Validate the path before processing
            if (!ValidatePath(Path))
            {
                return; // Error already written in ValidatePath
            }

            if (ShouldProcess($"PSContentPath = {Path}", "Set PSContentPath"))
            {
                try
                {
                    PowerShellConfig.Instance.SetPSContentPath(Path);
                    WriteVerbose($"Successfully set PSContentPath to '{Path}'");
                    WriteWarning("PSContentPath changes will take effect after restarting PowerShell.");
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
                if (!Path.IsPathRooted(expandedPath))
                {
                    WriteError(new ErrorRecord(
                        new ArgumentException($"The path '{path}' must be an absolute path."),
                        "RelativePathNotAllowed",
                        ErrorCategory.InvalidArgument,
                        path));
                    return false;
                }

                // Try to get the full path to validate format
                string fullPath = Path.GetFullPath(expandedPath);

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
