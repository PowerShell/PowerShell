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
        ConfirmImpact = ConfirmImpact.Low,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2097032")]
    [OutputType(typeof(DirectoryInfo))]
    public class NewTemporaryDirectoryCommand : PSCmdlet
    {
        private const string NewTemporaryDirectoryWriteError = "NewTemporaryDirectoryWriteError";

        private static ErrorRecord CreateErrorRecord(Exception exception, ErrorCategory category, string targetPath)
        {
            return new ErrorRecord(
                exception,
                NewTemporaryDirectoryWriteError,
                category,
                targetPath);
        }

        /// <summary>
        /// Gets or sets an optional prefix for the temporary directory name.
        /// The prefix is prepended to the randomly generated name.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
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

        private void CreateWithPrefix(string tempPath)
        {
            string targetDescription = Path.Combine(tempPath, Prefix);
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
            DirectoryInfo targetDirectory = PathUtils.GetTemporaryDirectory();
            if (!ShouldProcess(targetDirectory.FullName))
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(targetDirectory.FullName);
                WriteObject(new DirectoryInfo(targetDirectory.FullName));
            }
            catch (IOException ioException)
            {
                ThrowTerminatingError(CreateErrorRecord(ioException, ErrorCategory.WriteError, targetDirectory.FullName));
            }
            catch (UnauthorizedAccessException unauthorizedAccessException)
            {
                ThrowTerminatingError(CreateErrorRecord(unauthorizedAccessException, ErrorCategory.PermissionDenied, targetDirectory.FullName));
            }
        }
    }
}
