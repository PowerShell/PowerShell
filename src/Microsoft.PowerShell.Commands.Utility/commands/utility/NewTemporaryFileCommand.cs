
using System;
using System.IO;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The implementation of the "New-TemporaryFile" cmdlet
    /// </summary>
    [Cmdlet(VerbsCommon.New, "TemporaryFile", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=526726")]
    [OutputType(typeof(System.IO.FileInfo))]
    public class NewTemporaryFileCommand : Cmdlet
    {
        /// <summary>
        /// returns a TemporaryFile
        /// </summary>
        protected override void EndProcessing()
        {
            string filePath = null;
            string tempPath = System.Environment.GetEnvironmentVariable("TEMP");
            if (ShouldProcess(tempPath))
            {
                try
                {
                    filePath = Path.GetTempFileName();
                }
                catch (Exception e)
                {
                    WriteError(
                        new ErrorRecord(
                            e,
                            "NewTemporaryFileWriteError",
                            ErrorCategory.WriteError,
                            tempPath));
                    return;
                }
                if (!string.IsNullOrEmpty(filePath))
                {
                    FileInfo file = new FileInfo(filePath);
                    WriteObject(file);
                }
            }
        }
    }
}
