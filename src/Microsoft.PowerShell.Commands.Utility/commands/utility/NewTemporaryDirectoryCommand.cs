// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The implementation of the "New-TemporaryDirectory" cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "TemporaryDirectory", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    [OutputType(typeof(System.IO.DirectoryInfo))]
    public class NewTemporaryDirectoryCommand : Cmdlet
    {
        /// <summary>
        /// Returns a temporary directory.
        /// </summary>
        protected override void EndProcessing()
        {
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
