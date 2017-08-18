
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
        private string _extension = ".tmp";

        /// <summary>
        /// Specify a different file extension other than the default one, which is '.tmp'. The period in this parameter is optional
        /// </summary>
        [Parameter(Position = 0)]
        [ValidateNotNullOrEmpty]
        public string Extension
        {
            get { return _extension; }
            set { _extension = value; }
        }

        /// <summary>
        /// returns a TemporaryFile
        /// </summary>
        protected override void EndProcessing()
        {
            string filePath = null;
            string tempPath = Path.GetTempPath();

            if (ShouldProcess(tempPath))
            {
                int attempts = 0;
                bool creationOfFileSuccessful = false;

                // In case the random temporary file already exists, retry 
                while (attempts++ < 10 && !creationOfFileSuccessful)
                {
                    try
                    {
                        string fileName = Path.GetRandomFileName();
                        fileName = Path.ChangeExtension(fileName, _extension);
                        filePath = Path.Combine(tempPath, fileName);
                        using (new FileStream(filePath, FileMode.CreateNew)) { }
                        creationOfFileSuccessful = true;
                        WriteVerbose("Created temporary file {filePath}.");
                    }
                    catch (IOException) // file already exists -> retry
                    {
                        attempts++;
                    }
                    catch (Exception exception) // fatal error, which could be e.g. insufficient permissions, etc.
                    {
                        WriteError(
                        new ErrorRecord(
                                exception,
                                "NewTemporaryFileWriteError",
                                ErrorCategory.WriteError,
                                tempPath));
                        return;
                    }
                }

                if (!creationOfFileSuccessful)
                {
                    WriteError(
                        new ErrorRecord(
                            new IOException("Could not find an available temporary file name in {tempPath}."),
                                "NewTemporaryFileResourceUnavailable",
                                ErrorCategory.ResourceUnavailable,
                                tempPath));
                    return;
                }

                FileInfo file = new FileInfo(filePath);
                WriteObject(file);
            }
        }
    }
}
