// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The implementation of the "New-TemporaryDirectory" cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "TemporaryDirectory", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2378273")]
    [OutputType(typeof(System.IO.DirectoryInfo))]
    public class NewTemporaryDirectoryCommand : Cmdlet
    {
        private const string NewTemporaryDirectoryInvalidPrefix = "NewTemporaryDirectoryInvalidPrefix";
        private const string NewTemporaryDirectoryWriteError = "NewTemporaryDirectoryWriteError";

        /// <summary>
        /// Gets or sets an optional prefix for the temporary directory name.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string Prefix { get; set; }

        /// <summary>
        /// Returns a temporary directory.
        /// </summary>
        protected override void EndProcessing()
        {
            string tempPath = Path.GetTempPath();

            if (!string.IsNullOrEmpty(Prefix) && IsInvalidPrefix(Prefix))
            {
                ThrowTerminatingError(
                    CreateErrorRecord(
                        new System.ArgumentException("The prefix contains invalid characters.", nameof(Prefix)),
                        NewTemporaryDirectoryInvalidPrefix,
                        ErrorCategory.InvalidArgument,
                        Prefix));
                return;
            }

            string targetDescription = string.IsNullOrEmpty(Prefix)
                ? $"temporary directory under '{tempPath}'"
                : $"temporary directory under '{tempPath}' with prefix '{Prefix}'";

            if (!ShouldProcess(targetDescription, "Create temporary directory"))
            {
                return;
            }

            try
            {
                DirectoryInfo createdDirectory = string.IsNullOrEmpty(Prefix)
                    ? Directory.CreateTempSubdirectory()
                    : Directory.CreateTempSubdirectory(Prefix);

                WriteObject(createdDirectory);
            }
            catch (IOException ioException)
            {
                ThrowTerminatingError(
                    CreateErrorRecord(
                        ioException,
                        NewTemporaryDirectoryWriteError,
                        ErrorCategory.WriteError,
                        targetDescription));
            }
            catch (System.UnauthorizedAccessException unauthorizedAccessException)
            {
                ThrowTerminatingError(
                    CreateErrorRecord(
                        unauthorizedAccessException,
                        NewTemporaryDirectoryWriteError,
                        ErrorCategory.PermissionDenied,
                        targetDescription));
            }
        }

        private static bool IsInvalidPrefix(string prefix)
        {
            if (prefix.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return true;
            }

            foreach (char prefixCharacter in prefix)
            {
                if (prefixCharacter is '/' or '\\' || char.IsControl(prefixCharacter))
                {
                    return true;
                }
            }

            return prefix.Equals(".", System.StringComparison.Ordinal)
                || prefix.Equals("..", System.StringComparison.Ordinal);
        }

        private static ErrorRecord CreateErrorRecord(
            System.Exception exception,
            string errorId,
            ErrorCategory category,
            object targetObject)
        {
            return new ErrorRecord(
                exception,
                errorId,
                category,
                targetObject);
        }
    }
}
