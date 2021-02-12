// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Security;

namespace Microsoft.PowerShell
{
    internal static class SecurityUtils
    {
        /// <summary>
        /// Gets the size of a file.
        /// </summary>
        /// <param name="filePath">Path to file.</param>
        /// <returns>File size.</returns>
        internal static long GetFileSize(string filePath)
        {
            long size = 0;

            using (FileStream fs = new(filePath, FileMode.Open))
            {
                size = fs.Length;
            }

            return size;
        }

        /// <summary>
        /// Present a prompt for a SecureString data.
        /// </summary>
        /// <param name="hostUI">Ref to host ui interface.</param>
        /// <param name="prompt">Prompt text.</param>
        /// <returns> user input as secure string.</returns>
        internal static SecureString PromptForSecureString(PSHostUserInterface hostUI,
                                                           string prompt)
        {
            SecureString ss = null;

            hostUI.Write(prompt);
            ss = hostUI.ReadLineAsSecureString();
            hostUI.WriteLine(string.Empty);

            return ss;
        }

        /// <summary>
        /// </summary>
        /// <param name="resourceStr">Resource string.</param>
        /// <param name="errorId">Error identifier.</param>
        /// <param name="args">Replacement params for resource string formatting.</param>
        /// <returns></returns>
        internal static
        ErrorRecord CreateFileNotFoundErrorRecord(string resourceStr,
                                                  string errorId,
                                                  params object[] args)
        {
            string message =
                StringUtil.Format(
                    resourceStr,
                    args
                );

            FileNotFoundException e = new(message);

            ErrorRecord er = new(
                e,
                errorId,
                ErrorCategory.ObjectNotFound,
                targetObject: null);

            return er;
        }

        /// <summary>
        /// </summary>
        /// <param name="path">Path that was not found.</param>
        /// <param name="errorId">Error identifier.</param>
        /// <returns>ErrorRecord instance.</returns>
        internal static
        ErrorRecord CreatePathNotFoundErrorRecord(string path,
                                                  string errorId)
        {
            ItemNotFoundException e = new(path, "PathNotFound", SessionStateStrings.PathNotFound);

            ErrorRecord er = new(
                e,
                errorId,
                ErrorCategory.ObjectNotFound,
                targetObject: null);

            return er;
        }

        /// <summary>
        /// Create an error record for 'operation not supported' condition.
        /// </summary>
        /// <param name="resourceStr">Resource string.</param>
        /// <param name="errorId">Error identifier.</param>
        /// <param name="args">Replacement params for resource string formatting.</param>
        /// <returns></returns>
        internal static
        ErrorRecord CreateNotSupportedErrorRecord(string resourceStr,
                                                  string errorId,
                                                  params object[] args)
        {
            string message = StringUtil.Format(resourceStr, args);

            NotSupportedException e = new(message);

            ErrorRecord er = new(
                e,
                errorId,
                ErrorCategory.NotImplemented,
                targetObject: null);

            return er;
        }

        /// <summary>
        /// Create an error record for 'operation not supported' condition.
        /// </summary>
        /// <param name="e">Exception to include in ErrorRecord.</param>
        /// <param name="errorId">Error identifier.</param>
        /// <returns></returns>
        internal static
        ErrorRecord CreateInvalidArgumentErrorRecord(Exception e,
                                                     string errorId)
        {
            ErrorRecord er = new(
                e,
                errorId,
                ErrorCategory.InvalidArgument,
                targetObject: null);

            return er;
        }

        /// <summary>
        /// Convert the specified provider path to a provider path
        /// and make sure that all of the following is true:
        /// -- it represents a FileSystem path
        /// -- it points to a file
        /// -- the file exists.
        /// </summary>
        /// <param name="cmdlet">Cmdlet instance.</param>
        /// <param name="path">Provider path.</param>
        /// <returns>
        /// filesystem path if all conditions are true,
        /// null otherwise
        /// </returns>
        internal static string GetFilePathOfExistingFile(PSCmdlet cmdlet,
                                                         string path)
        {
            string resolvedProviderPath = cmdlet.SessionState.Path.GetUnresolvedProviderPathFromPSPath(path);
            if (File.Exists(resolvedProviderPath))
            {
                return resolvedProviderPath;
            }
            else
            {
                return null;
            }
        }
    }
}
