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
        /// Returns a TemporaryDirectory.
        /// </summary>
        protected override void EndProcessing()
        {
            string tempPath = Path.GetTempPath();
            if (ShouldProcess(tempPath))
            {
                string directoryPath = Path.Combine(tempPath, Path.GetRandomFileName());

                try
                {
                    DirectoryInfo directory = Directory.CreateDirectory(directoryPath);
                    WriteObject(directory);
                }
                catch (IOException ioException)
                {
                    ThrowTerminatingError(
                        new ErrorRecord(
                            ioException,
                            "NewTemporaryDirectoryWriteError",
                            ErrorCategory.WriteError,
                            tempPath));
                }
            }
        }
    }
}
