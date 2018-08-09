// Copyright (c) Microsoft Corporation. All rights reserved.
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
        /// gets the size of a file
        /// </summary>
        /// <param name="filePath"> path to file </param>
        /// <returns> file size  </returns>
        /// <remarks>  </remarks>
        internal static long GetFileSize(string filePath)
        {
            long size = 0;

            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            {
                size = fs.Length;
            }

            return size;
        }

        /// <summary>
        /// present a prompt for a SecureString data
        /// </summary>
        /// <param name="hostUI"> ref to host ui interface </param>
        /// <param name="prompt"> prompt text </param>
        /// <returns> user input as secure string </returns>
        /// <remarks>  </remarks>
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
        /// <param name="resourceStr"> resource string </param>
        /// <param name="errorId"> error identifier </param>
        /// <param name="args"> replacement params for resource string formatting </param>
        /// <returns>  </returns>
        /// <remarks>  </remarks>
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

            FileNotFoundException e =
                new FileNotFoundException(message);

            ErrorRecord er =
                new ErrorRecord(e,
                                errorId,
                                ErrorCategory.ObjectNotFound,
                                null);

            return er;
        }

        /// <summary>
        /// </summary>
        /// <param name="path"> path that was not found </param>
        /// <param name="errorId"> error identifier </param>
        /// <returns> ErrorRecord instance </returns>
        /// <remarks>  </remarks>
        internal static
        ErrorRecord CreatePathNotFoundErrorRecord(string path,
                                                  string errorId)
        {
            ItemNotFoundException e =
                new ItemNotFoundException(path, "PathNotFound", SessionStateStrings.PathNotFound);

            ErrorRecord er =
                new ErrorRecord(e,
                                errorId,
                                ErrorCategory.ObjectNotFound,
                                null);

            return er;
        }

        /// <summary>
        /// Create an error record for 'operation not supported' condition
        /// </summary>
        /// <param name="resourceStr"> resource string </param>
        /// <param name="errorId"> error identifier </param>
        /// <param name="args"> replacement params for resource string formatting </param>
        /// <returns>  </returns>
        /// <remarks>  </remarks>
        internal static
        ErrorRecord CreateNotSupportedErrorRecord(string resourceStr,
                                                  string errorId,
                                                  params object[] args)
        {
            string message = StringUtil.Format(resourceStr, args);

            NotSupportedException e =
                new NotSupportedException(message);

            ErrorRecord er =
                new ErrorRecord(e,
                                errorId,
                                ErrorCategory.NotImplemented,
                                null);

            return er;
        }

        /// <summary>
        /// Create an error record for 'operation not supported' condition
        /// </summary>
        /// <param name="e"> exception to include in ErrorRecord </param>
        /// <param name="errorId"> error identifier </param>
        /// <returns>  </returns>
        /// <remarks>  </remarks>
        internal static
        ErrorRecord CreateInvalidArgumentErrorRecord(Exception e,
                                                     string errorId)
        {
            ErrorRecord er =
                new ErrorRecord(e,
                                errorId,
                                ErrorCategory.InvalidArgument,
                                null);

            return er;
        }

        /// <summary>
        /// convert the specified provider path to a provider path
        /// and make sure that all of the following is true:
        /// -- it represents a FileSystem path
        /// -- it points to a file
        /// -- the file exists
        /// </summary>
        /// <param name="cmdlet"> cmdlet instance </param>
        /// <param name="path"> provider path </param>
        /// <returns>
        /// filesystem path if all conditions are true,
        /// null otherwise
        /// </returns>
        /// <remarks>  </remarks>
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

