// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The implementation of the "New-TemporaryDirectory" cmdlet.
    /// Creates a new directory under the system temp path and returns
    /// a <see cref="DirectoryInfo"/> pointing to it.
    /// </summary>
    [Cmdlet(
        VerbsCommon.New,
        "TemporaryDirectory",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.Low)]
    [OutputType(typeof(DirectoryInfo))]
    public class NewTemporaryDirectoryCommand : Cmdlet
    {
        private const string NewTemporaryDirectoryWriteError = "NewTemporaryDirectoryWriteError";

        /// <summary>
        /// Gets or sets an optional prefix for the temporary directory name.
        /// The prefix is prepended to the randomly generated name.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [ValidatePattern(@"^[^\\/:*?""<>|]+$")]
        public string Prefix { get; set; }

        /// <summary>
        /// Creates a temporary directory and writes it to the pipeline.
        /// </summary>
        protected override void EndProcessing()
        {
            string tempPath = Path.GetTempPath();

            if (!string.IsNullOrEmpty(Prefix))
            {
                CreateWithPrefix(tempPath);
            }
            else
            {
                CreateWithoutPrefix(tempPath);
            }
        }

        private static ErrorRecord CreateErrorRecord(Exception exception, ErrorCategory category, string targetPath)
        {
            return new ErrorRecord(
                exception,
                NewTemporaryDirectoryWriteError,
                category,
                targetPath);
        }

        private void CreateWithPrefix(string tempPath)
        {
            string targetDescription = $"Temp directory with prefix '{Prefix}' under '{tempPath}'";
            if (!ShouldProcess(targetDescription))
            {
                return;
            }

            try
            {
                DirectoryInfo createdDirectory = Directory.CreateTempSubdirectory(Prefix);
                WriteObject(createdDirectory);
            }
            catch (IOException ioException)
            {
                ThrowTerminatingError(CreateErrorRecord(ioException, ErrorCategory.WriteError, targetDescription));
            }
            catch (UnauthorizedAccessException unauthorizedAccessException)
            {
                ThrowTerminatingError(CreateErrorRecord(unauthorizedAccessException, ErrorCategory.PermissionDenied, targetDescription));
            }
        }

        private void CreateWithoutPrefix(string tempPath)
        {
            string targetDescription = $"Temp directory under '{tempPath}'";
            if (!ShouldProcess(targetDescription))
            {
                return;
            }

            try
            {
                // Loop until we find a non-existent directory name to avoid race conditions,
                // matching the pattern used by PathUtils.CreateTemporaryDirectory().
                DirectoryInfo targetDirectory;
                do
                {
                    targetDirectory = new DirectoryInfo(
                        Path.Combine(tempPath, Path.GetRandomFileName()));
                }
                while (targetDirectory.Exists);

                DirectoryInfo created = Directory.CreateDirectory(targetDirectory.FullName);
                WriteObject(created);
            }
            catch (IOException ioException)
            {
                ThrowTerminatingError(CreateErrorRecord(ioException, ErrorCategory.WriteError, tempPath));
            }
            catch (UnauthorizedAccessException unauthorizedAccessException)
            {
                ThrowTerminatingError(CreateErrorRecord(unauthorizedAccessException, ErrorCategory.PermissionDenied, tempPath));
            }
        }
    }
}
