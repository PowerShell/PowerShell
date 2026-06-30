// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The implementation of the "New-TemporaryDirectory" cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "TemporaryDirectory", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2097032")]
    [OutputType(typeof(System.IO.DirectoryInfo))]
    public class NewTemporaryDirectoryCommand : Cmdlet
    {
        /// <summary>
        /// Gets or sets an optional prefix for the temporary directory name.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string Prefix { get; set; }

        /// <summary>
        /// Returns a temporary directory.
        /// </summary>
        protected override void EndProcessing()
        {
            string tempPath = Path.GetTempPath();

            if (!string.IsNullOrEmpty(Prefix))
            {
                DirectoryInfo prefixedTargetDirectory = GetTemporaryDirectoryWithPrefix(tempPath, Prefix);
                if (ShouldProcess(prefixedTargetDirectory.FullName))
                {
                    try
                    {
                        DirectoryInfo createdDirectory = CreateTemporaryDirectory(prefixedTargetDirectory.FullName);
                        WriteObject(createdDirectory);
                    }
                    catch (IOException ioException)
                    {
                        ThrowTerminatingError(
                            CreateErrorRecord(
                                ioException,
                                ErrorCategory.WriteError,
                                prefixedTargetDirectory.FullName));
                    }
                    catch (System.UnauthorizedAccessException unauthorizedAccessException)
                    {
                        ThrowTerminatingError(
                            CreateErrorRecord(
                                unauthorizedAccessException,
                                ErrorCategory.PermissionDenied,
                                prefixedTargetDirectory.FullName));
                    }
                }

                return;
            }

            DirectoryInfo targetDirectory = PathUtils.GetTemporaryDirectory();
            if (ShouldProcess(targetDirectory.FullName))
            {
                try
                {
                    DirectoryInfo createdDirectory = Directory.CreateDirectory(targetDirectory.FullName);
                    WriteObject(createdDirectory);
                }
                catch (IOException ioException)
                {
                    ThrowTerminatingError(
                        CreateErrorRecord(
                            ioException,
                            ErrorCategory.WriteError,
                            targetDirectory.FullName));
                }
                catch (System.UnauthorizedAccessException unauthorizedAccessException)
                {
                    ThrowTerminatingError(
                        CreateErrorRecord(
                            unauthorizedAccessException,
                            ErrorCategory.PermissionDenied,
                            targetDirectory.FullName));
                }
            }
        }

        private static DirectoryInfo GetTemporaryDirectoryWithPrefix(string tempPath, string prefix)
        {
            if (prefix.Contains(Path.DirectorySeparatorChar) || prefix.Contains(Path.AltDirectorySeparatorChar))
            {
                throw new System.ArgumentException(
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
            if (System.OperatingSystem.IsWindows())
            {
                return Directory.CreateDirectory(path);
            }

            return Directory.CreateDirectory(
                path,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        private static ErrorRecord CreateErrorRecord(System.Exception exception, ErrorCategory category, string targetPath)
        {
            return new ErrorRecord(
                exception,
                "NewTemporaryDirectoryWriteError",
                category,
                targetPath);
        }
    }
}
