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
                CreateWithoutPrefix();
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
            DirectoryInfo prefixedTargetDirectory = GetTemporaryDirectoryWithPrefix(tempPath, Prefix);
            if (!ShouldProcess(prefixedTargetDirectory.FullName))
            {
                return;
            }

            try
            {
                DirectoryInfo createdDirectory = CreateTemporaryDirectory(prefixedTargetDirectory.FullName);
                WriteObject(createdDirectory);
            }
            catch (IOException ioException)
            {
                ThrowTerminatingError(CreateErrorRecord(ioException, ErrorCategory.WriteError, prefixedTargetDirectory.FullName));
            }
            catch (UnauthorizedAccessException unauthorizedAccessException)
            {
                ThrowTerminatingError(CreateErrorRecord(unauthorizedAccessException, ErrorCategory.PermissionDenied, prefixedTargetDirectory.FullName));
            }
        }

        private void CreateWithoutPrefix()
        {
            // Reuse the shared helper introduced alongside this cmdlet so the
            // directory generation logic stays in one place.
            DirectoryInfo targetDirectory = PathUtils.GetTemporaryDirectory();
            if (!ShouldProcess(targetDirectory.FullName))
            {
                return;
            }

            try
            {
                DirectoryInfo createdDirectory = CreateTemporaryDirectory(targetDirectory.FullName);
                WriteObject(createdDirectory);
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

        private static DirectoryInfo GetTemporaryDirectoryWithPrefix(string tempPath, string prefix)
        {
            // The [ValidatePattern] attribute already rejects path separator
            // characters, but guard here too so the invariant holds even if
            // the parameter binding ever changes.
            if (prefix.Contains(Path.DirectorySeparatorChar) || prefix.Contains(Path.AltDirectorySeparatorChar))
            {
                throw new ArgumentException(
                    "The prefix cannot contain directory separator characters.",
                    nameof(prefix));
            }

            DirectoryInfo temporaryDirectory = new(tempPath);
            while (true)
            {
                DirectoryInfo targetDirectory = new(
                    Path.Combine(
                        temporaryDirectory.FullName,
                        prefix + Path.GetRandomFileName()));

                if (!targetDirectory.Exists)
                {
                    return targetDirectory;
                }
            }
        }

        private static DirectoryInfo CreateTemporaryDirectory(string path)
        {
            if (OperatingSystem.IsWindows())
            {
                return Directory.CreateDirectory(path);
            }

            return Directory.CreateDirectory(
                path,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }
}
