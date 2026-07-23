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
    public class NewTemporaryDirectoryCommand : Cmdlet
    {
        private const string NewTemporaryDirectoryWriteError = "NewTemporaryDirectoryWriteError";

        /// <summary>
        /// Gets or sets an optional prefix for the temporary directory name.
        /// The prefix is prepended to the randomly generated name.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string Prefix { get; set; }

        private static ErrorRecord CreateErrorRecord(Exception exception, ErrorCategory category, string targetPath)
        {
            return new ErrorRecord(
                exception,
                NewTemporaryDirectoryWriteError,
                category,
                targetPath);
        }

        /// <summary>
        /// Creates a temporary directory and writes it to the pipeline.
        /// Uses <see cref="Directory.CreateTempSubdirectory()"/> for atomic creation,
        /// which guarantees a fresh directory without check-to-create race conditions.
        /// </summary>
        protected override void EndProcessing()
        {
            if (!string.IsNullOrEmpty(Prefix) && Prefix.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new ArgumentException($"The prefix '{Prefix}' contains invalid characters."),
                        NewTemporaryDirectoryWriteError,
                        ErrorCategory.InvalidArgument,
                        Prefix));
                return;
            }

            string targetDescription = string.IsNullOrEmpty(Prefix)
                ? Path.GetTempPath()
                : Path.Combine(Path.GetTempPath(), Prefix);

            if (!ShouldProcess(targetDescription))
            {
                return;
            }

            try
            {
                DirectoryInfo createdDirectory = string.IsNullOrEmpty(Prefix)
                    ? Directory.CreateTempSubdirectory()
                    : Directory.CreateTempSubdirectory(Prefix);

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
    }
}
