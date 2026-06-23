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
            string tempPath = Path.GetTempPath();
            string targetDescription = string.IsNullOrEmpty(Prefix)
                ? tempPath
                : Path.Combine(tempPath, Prefix);

            if (ShouldProcess(targetDescription))
            {
                DirectoryInfo directory;
                try
                {
                    // Loop until we find a non-existent directory name to avoid collisions,
                    // matching the pattern used by PathUtils.CreateTemporaryDirectory().
                    DirectoryInfo tempDir;
                    do
                    {
                        string name = string.IsNullOrEmpty(Prefix)
                            ? Path.GetRandomFileName()
                            : string.Concat(Prefix, Path.GetRandomFileName());
                        tempDir = new DirectoryInfo(Path.Combine(tempPath, name));
                    }
                    while (tempDir.Exists);

                    Directory.CreateDirectory(tempDir.FullName);
                    directory = new DirectoryInfo(tempDir.FullName);
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

                WriteObject(directory);
            }
        }
    }
}
