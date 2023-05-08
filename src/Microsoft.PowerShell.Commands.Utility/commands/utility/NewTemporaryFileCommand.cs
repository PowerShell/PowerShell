// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The implementation of the "New-TemporaryFile" cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "TemporaryFile", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2097032")]
    [OutputType(typeof(System.IO.FileInfo))]
    public class NewTemporaryFileCommand : Cmdlet
    {
        /// <summary>
        /// Returns a TemporaryFile.
        /// </summary>
        protected override void EndProcessing()
        {
            string filePath = null;
            string tempPath = Path.GetTempPath();
            if (ShouldProcess(tempPath))
            {
                try
                {
                    filePath = Path.GetTempFileName();
                }
                catch (IOException ioException)
                {
                    ThrowTerminatingError(
                        new ErrorRecord(
                            ioException,
                            "NewTemporaryFileWriteError",
                            ErrorCategory.WriteError,
                            tempPath));
                    return;
                }

                if (!string.IsNullOrEmpty(filePath))
                {
                    FileInfo file = new(filePath);
                    WriteObject(file);
                }
            }
        }
    }
}
