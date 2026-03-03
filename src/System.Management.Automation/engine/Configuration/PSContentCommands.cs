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
    [OutputType(typeof(DirectoryInfo))]
    public class GetPSContentPathCommand : PSCmdlet
    {
        /// <summary>
        /// EndProcessing method of this cmdlet.
        /// Outputs the PSContentPath as a DirectoryInfo object with ConfigFile NoteProperty.
        /// </summary>
        protected override void EndProcessing()
        {
            try
            {
                var psContentPath = Utils.GetPSContentPath();
                var configFilePath = PowerShellConfig.Instance.GetConfigFilePath(ConfigScope.CurrentUser);

                // Create DirectoryInfo object
                var directoryInfo = new DirectoryInfo(psContentPath);
                
                // Wrap in PSObject to add the ConfigFile NoteProperty
                var result = PSObject.AsPSObject(directoryInfo);
                result.Properties.Add(new PSNoteProperty("ConfigFile", configFilePath));
                
                WriteObject(result);
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
    [Cmdlet(VerbsCommon.Set, "PSContentPath", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High, HelpUri = "https://go.microsoft.com/fwlink/?linkid=2344807")]
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
        [Parameter(Mandatory = true, ParameterSetName = "Default")]
        public SwitchParameter Default { get; set; }

        /// <summary>
        /// EndProcessing method of this cmdlet.
        /// Validates the path and sets the PSContentPath in the configuration.
        /// </summary>
        protected override void EndProcessing()
        {
            if (Default)
            {
                ResetToDefault();
                return;
            }

            // Validate the path
            if (!ValidatePath(Path))
            {
                return;
            }

            string expandedPath = Environment.ExpandEnvironmentVariables(Path);
            string currentPath = Utils.GetPSContentPath();
            string configFile = PowerShellConfig.Instance.GetConfigFilePath(ConfigScope.CurrentUser);

            string target = $"Config file: '{configFile}'";
            string action = $"Set PSUserContentPath from '{currentPath}' to '{expandedPath}'";

            if (ShouldProcess(target, action))
            {
                try
                {
                    PowerShellConfig.Instance.SetPSContentPath(Path);

                    // Update the $PSUserContentPath readonly variable in the current session
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
            string currentPath = Utils.GetPSContentPath();
            string configFile = PowerShellConfig.Instance.GetConfigFilePath(ConfigScope.CurrentUser);

            string target = $"Config file: '{configFile}'";
            string action = $"Reset PSUserContentPath from '{currentPath}' to platform default '{defaultPath}'";

            if (ShouldProcess(target, action))
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
