
using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Threading;

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
        private const string defaultExtension = ".tmp";

        /// <summary>
        /// Specify a different file extension other than the default one, which is '.tmp'. The period in this parameter is optional.
        /// </summary>
        [Parameter()]
        [ValidateNotNullOrEmpty]
        public string Extension { get; set; } = defaultExtension;

        /// <summary>
        /// Validate Extension.
        /// </summary>
        protected override void BeginProcessing()
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
        }

        /// <summary>
        /// Returns a TemporaryFile.
        /// </summary>
        protected override void EndProcessing()
        {
            string temporaryFilePath = null;
            string tempPath = Path.GetTempPath();

            if (ShouldProcess(tempPath))
            {
                if (Extension.Equals(defaultExtension, StringComparison.Ordinal))
                {
                    try
                    {
                        temporaryFilePath = Path.GetTempFileName();
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
                }
                else
                {
                    int attempts = 0;
                    bool creationOfFileSuccessful = false;

                    // In case the random temporary file already exists, retry 
                    while (attempts++ < 10 && !creationOfFileSuccessful)
                    {
                        try
                        {
                            temporaryFilePath = Path.GetTempFileName(); // this already creates the temporary file
                            // rename file
                            var temporaryFileWithCustomExtension = Path.ChangeExtension(temporaryFilePath, Extension);
                            File.Move(temporaryFilePath, temporaryFileWithCustomExtension);
                            temporaryFilePath = temporaryFileWithCustomExtension;
                            creationOfFileSuccessful = true;
                            WriteDebug($"Created temporary file {temporaryFilePath}.");
                        }
                        catch (IOException) // file already exists -> retry
                        {
                            attempts++;
                            if (temporaryFilePath != null)
                            {
                                try
                                {
                                    File.Delete(temporaryFilePath);
                                }
                                catch (Exception exception)
                                {
                                    WriteError(
                                        new ErrorRecord(
                                            exception,
                                            "NewTemporaryFileWriteError",
                                            ErrorCategory.WriteError,
                                            tempPath));
                                    Thread.Sleep(10); // to increase chance of success in the next try
                                }
                            }
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
                                new IOException(StringUtil.Format(UtilityCommonStrings.CouldNotCreateTemporaryFilename, tempPath)),
                                    "NewTemporaryFileResourceUnavailable",
                                    ErrorCategory.ResourceUnavailable,
                                    tempPath));
                        return;
                    }
                }

                var temporaryFileInfo = new FileInfo(temporaryFilePath);
                WriteObject(temporaryFileInfo);
            }
        }
    }
}
