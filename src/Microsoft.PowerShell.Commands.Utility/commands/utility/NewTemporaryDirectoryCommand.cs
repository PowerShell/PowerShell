// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The implementation of the "New-TemporaryDirectory" cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "TemporaryDirectory",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.Low,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=xxxxx")]
    [OutputType(typeof(System.IO.DirectoryInfo))]
    public class NewTemporaryDirectoryCommand : Cmdlet
    {
        /// <summary>
        /// Returns a TemporaryDirectory.
        /// </summary>
        protected override void EndProcessing()
        {
            string baseTempPath = Path.GetTempPath();

            if (ShouldProcess(baseTempPath))
            {
                string tempPath;
                try
                {
                    // Create a unique temporary directory name
                    tempPath = Path.Combine(baseTempPath, Path.GetRandomFileName());
                    Directory.CreateDirectory(tempPath);
                }
                catch (IOException ioException)
                {
                    ThrowTerminatingError(
                        new ErrorRecord(
                            ioException,
                            "NewTemporaryDirectoryError",
                            ErrorCategory.WriteError,
                            baseTempPath));
                    return;
                }

                DirectoryInfo directory = new(tempPath);
                WriteObject(directory);
            }
        }
    }
}
