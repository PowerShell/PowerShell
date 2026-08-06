// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The implementation of the "New-TemporaryDirectory" cmdlet.
    /// Creates a new directory under the system temp path and returns
    /// a <see cref="DirectoryInfo"/> pointing to it.
    /// </summary>
    [Cmdlet(
        VerbsCommon.New,
        "TemporaryDirectory",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.Low,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2097032")]
    [OutputType(typeof(DirectoryInfo))]
    public class NewTemporaryDirectoryCommand : Cmdlet
    {
        private const string NewTemporaryDirectoryWriteError = "NewTemporaryDirectoryWriteError";

        /// <summary>
        /// Gets or sets an optional prefix for the temporary directory name.
        /// The prefix is prepended to the randomly generated name.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string Prefix { get; set; }

        /// <summary>
        /// Creates a temporary directory and writes it to the pipeline.
        /// Uses <see cref="Directory.CreateTempSubdirectory(string)"/> for atomic creation,
        /// which guarantees a fresh directory without check-to-create race conditions.
        /// </summary>
        protected override void EndProcessing()
        {
            if (!string.IsNullOrEmpty(Prefix) && !IsValidPrefix(Prefix))
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new ArgumentException($"The prefix '{Prefix}' contains invalid characters."),
                        NewTemporaryDirectoryWriteError,
                        ErrorCategory.InvalidArgument,
                        Prefix));
                return;
            }

            // CreateTempSubdirectory appends a random suffix, so the final directory
            // name is not known upfront. Use a descriptive target so WhatIf/Confirm
            // never shows a path that is not actually created (e.g. <temp>\..).
            string targetDescription = string.IsNullOrEmpty(Prefix)
                ? Path.GetTempPath()
                : $"temporary directory under {Path.GetTempPath()} with prefix '{Prefix}'";

            if (!ShouldProcess(targetDescription))
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
                ThrowTerminatingError(CreateErrorRecord(ioException, ErrorCategory.WriteError, targetDescription));
            }
            catch (UnauthorizedAccessException unauthorizedAccessException)
            {
                ThrowTerminatingError(CreateErrorRecord(unauthorizedAccessException, ErrorCategory.PermissionDenied, targetDescription));
            }
        }

        /// <summary>
        /// Validates that the prefix can be safely used as part of a directory name.
        /// Rejects '.', '..', and any prefix containing path separators or control
        /// characters, which would otherwise produce confusing cross-platform paths
        /// or misleading WhatIf/Confirm targets.
        /// </summary>
        /// <param name="prefix">The prefix to validate.</param>
        /// <returns>True if the prefix is valid, false otherwise.</returns>
        private static bool IsValidPrefix(string prefix)
        {
            if (prefix is "." or "..")
            {
                return false;
            }

            if (prefix.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return false;
            }

            // On Unix, '\\' and control characters are not invalid file name
            // characters, but they can create confusing cross-platform behavior.
            foreach (char c in prefix)
            {
                if (c is '/' or '\\' || char.IsControl(c))
                {
                    return false;
                }
            }

            return true;
        }

        private static ErrorRecord CreateErrorRecord(Exception exception, ErrorCategory category, string targetPath)
        {
            return new ErrorRecord(
                exception,
                NewTemporaryDirectoryWriteError,
                category,
                targetPath);
        }
    }
}
