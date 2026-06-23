// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The implementation of the "New-TemporaryDirectory" cmdlet.
    /// </summary>
    [Cmdlet(
        VerbsCommon.New,
        "TemporaryDirectory",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.Low,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2097032")]
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
        /// Returns a TemporaryDirectory.
        /// </summary>
        protected override void EndProcessing()
        {
            if (!string.IsNullOrEmpty(Prefix))
            {
                string targetDescription = Path.Combine(Path.GetTempPath(), Prefix);
                if (ShouldProcess(targetDescription))
                {
                    try
                    {
                        DirectoryInfo createdDirectory = Directory.CreateTempSubdirectory(Prefix);
                        WriteObject(createdDirectory);
                    }
                    catch (IOException ioException)
                    {
                        ThrowTerminatingError(
                            CreateErrorRecord(ioException, ErrorCategory.WriteError, targetDescription));
                    }
                    catch (System.UnauthorizedAccessException unauthorizedAccessException)
                    {
                        ThrowTerminatingError(
                            CreateErrorRecord(unauthorizedAccessException, ErrorCategory.PermissionDenied, targetDescription));
                    }
                }

                return;
            }

            DirectoryInfo targetDirectory = PathUtils.GetTemporaryDirectory();
            if (ShouldProcess(targetDirectory.FullName))
            {
                try
                {
                    Directory.CreateDirectory(targetDirectory.FullName);
                    WriteObject(new DirectoryInfo(targetDirectory.FullName));
                }
                catch (IOException ioException)
                {
                    ThrowTerminatingError(
                        CreateErrorRecord(ioException, ErrorCategory.WriteError, targetDirectory.FullName));
                }
                catch (System.UnauthorizedAccessException unauthorizedAccessException)
                {
                    ThrowTerminatingError(
                        CreateErrorRecord(unauthorizedAccessException, ErrorCategory.PermissionDenied, targetDirectory.FullName));
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
