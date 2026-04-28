// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The implementation of the "New-TemporaryDirectory" cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "TemporaryDirectory", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=xxxxx")]
    [OutputType(typeof(System.IO.DirectoryInfo))]
    public class NewTemporaryDirectoryCommand : Cmdlet
    {
        /// <summary>
        /// Returns a TemporaryDirectory.
        /// </summary>
        protected override void EndProcessing()
        {
            string tempDirPath = null;
            string tempPath = Path.GetTempPath();
            if (ShouldProcess(tempPath))
            {
                try
                {
                    tempDirPath = Path.Combine(tempPath, Path.GetRandomFileName());
                    Directory.CreateDirectory(tempDirPath);
                }
                catch (IOException ioException)
                {
                    ThrowTerminatingError(
                        new ErrorRecord(
                            ioException,
                            "NewTemporaryDirectoryError",
                            ErrorCategory.WriteError,
                            tempPath));
                    return;
                }

                if (!string.IsNullOrEmpty(tempDirPath))
                {
                    DirectoryInfo directory = new(tempDirPath);
                    WriteObject(directory);
                }
            }
        }
    }
}
