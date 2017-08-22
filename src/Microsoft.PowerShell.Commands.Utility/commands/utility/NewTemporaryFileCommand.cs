
using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The implementation of the "New-TemporaryFile" cmdlet that has an optional 'Extension' Parameter property.
    /// If this cmdlet errors then it throws a non-terminating error.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "TemporaryFile", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=526726")]
    [OutputType(typeof(System.IO.FileInfo))]
    public class NewTemporaryFileCommand : Cmdlet
    {
        /// <summary>
        /// Specify a different file extension other than the default one, which is '.tmp'. The period in this parameter is optional.
        /// </summary>
        [Parameter(Position = 0)]
        [ValidateNotNullOrEmpty]
        public string Extension { get; set; } = ".tmp";

        /// <summary>
        /// Returns a TemporaryFile.
        /// </summary>
        protected override void EndProcessing()
        {
            // Check for invalid characters in extension
            if (Extension.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                WriteError(
                    new ErrorRecord(
                        new ArgumentException(
                                StringUtil.Format(UtilityCommonStrings.InvalidCharacterInParameter,
                                    nameof(Extension), Extension)),
                        "NewTemporaryInvalidArgument",
                        ErrorCategory.InvalidArgument,
                        Extension));
                return;
            }

            string filePath = null;
            string tempPath = Path.GetTempPath();

            if (ShouldProcess(tempPath))
            {
                int attempts = 0;
                bool creationOfFileSuccessful = false;

                // In case the random temporary file already exists, retry 
                while (attempts++ < 10 && !creationOfFileSuccessful)
                {
                    string fileName = Path.GetRandomFileName();
                    try
                    {                     
                        fileName = Path.ChangeExtension(fileName, Extension);
                        filePath = Path.Combine(tempPath, fileName);
                        // Try to create the temporary file.
                        // If this is successful then we will always be able to return it to the user and therefore do not need to remove it.
                        using (new FileStream(filePath, FileMode.CreateNew)) { }
                        creationOfFileSuccessful = true;
                        WriteVerbose($"Created temporary file {filePath}.");
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
                            new IOException(StringUtil.Format(UtilityCommonStrings.CouldNotFindTemporaryFilename, tempPath)),
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
