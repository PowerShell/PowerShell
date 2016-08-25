/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.IO;
using System.Management.Automation;
using Microsoft.PowerShell.Commands;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Security;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using DWORD = System.UInt32;
using BOOL = System.UInt32;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace Microsoft.PowerShell
{
    internal static class SecurityUtils
    {
        /// <summary>
        /// gets the size of a file
        /// </summary>
        ///
        /// <param name="filePath"> path to file </param>
        ///
        /// <returns> file size  </returns>
        ///
        /// <remarks>  </remarks>
        ///
        internal static long GetFileSize(string filePath)
        {
            long size = 0;

            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            {
                size = fs.Length;
            }

            return size;
        }

#if false
        /// <summary>
        /// throw if file is smaller than 4 bytes in length
        /// </summary>
        ///
        /// <param name="filePath"> path to file </param>
        ///
        /// <returns> Does not return a value </returns>
        ///
        /// <remarks>  </remarks>
        ///
        internal static void CheckIfFileSmallerThan4Bytes(string filePath)
        {
            if (GetFileSize(filePath) < 4)
            {
                string message =
                    StringUtil.Format(
                        UtilsStrings.FileSmallerThan4Bytes,
                        new object[] { filePath }
                    );

                throw PSTraceSource.NewArgumentException(message, "path");
                /*
                // 2004/10/22-JonN The above form of the constructor
                //  no longer exists.  This should probably be as below,
                //  however I have not tested this.  This method is not
                //  used so I have removed it.
                throw PSTraceSource.NewArgumentException(
                        "path",
                        "Utils",
                        "FileSmallerThan4Bytes",
                        filePath
                        );
                */
            }
        }
#endif

        /// <summary>
        /// present a prompt for a SecureString data
        /// </summary>
        ///
        /// <param name="hostUI"> ref to host ui interface </param>
        ///
        /// <param name="prompt"> prompt text </param>
        ///
        /// <returns> user input as secure string </returns>
        ///
        /// <remarks>  </remarks>
        ///
        internal static SecureString PromptForSecureString(PSHostUserInterface hostUI,
                                                           string prompt)
        {
            SecureString ss = null;

            hostUI.Write(prompt);
            ss = hostUI.ReadLineAsSecureString();
            hostUI.WriteLine("");

            return ss;
        }

#if !CORECLR        
        /// <summary>
        /// get plain text string from a SecureString
        ///
        /// This function will not be required once all of the methods
        /// that we call accept a SecureString. The list below has
        /// classes/methods that will be changed to accept a SecureString
        /// after Whidbey beta1
        ///
        /// -- X509Certificate2.Import (String, String, X509KeyStorageFlags)
        ///    (DCR #33007 in the DevDiv Schedule db)
        ///    
        /// -- NetworkCredential(string, string);
        ///    
        /// </summary>
        ///
        /// <param name="ss"> input data </param>
        ///
        /// <returns> a string representing clear-text equivalent of ss  </returns>
        ///
        /// <remarks>  </remarks>
        ///
        [ArchitectureSensitive]
        internal static string GetStringFromSecureString(SecureString ss)
        {
            IntPtr p = Marshal.SecureStringToGlobalAllocUnicode(ss);
            string s = Marshal.PtrToStringUni(p);

            Marshal.ZeroFreeGlobalAllocUnicode(p);

            return s;
        }
#endif

        /*
        /// <summary>
        /// display sec-desc of a file
        /// </summary>
        ///
        /// <param name="sd"> file security descriptor </param>
        ///
        /// <returns> Does not return a value </returns>
        ///
        /// <remarks>  </remarks>
        ///
        internal static void ShowFileSd(FileSecurity sd)
        {
                string userName = null;
                FileSystemRights rights = 0;
                AccessControlType aceType = 0;
            
                rules = sd.GetAccessRules(true, false, typeof(NTAccount));

                foreach (FileSystemAccessRule r in rules)
                {
                    userName = r.IdentityReference.ToString();
                    aceType  = r.AccessControlType;
                    rights = r.FileSystemRights;

                    Console.WriteLine("{0} : {1} : {2}",
                                      userName,
                                      aceType.ToString(),
                                      rights.ToString());
                }
            }
        }
        */

        /// <summary>
        ///
        /// </summary>
        ///
        /// <param name="resourceStr"> resource string </param>
        ///
        /// <param name="errorId"> error identifier </param>
        ///
        /// <param name="args"> replacement params for resource string formatting </param>
        ///
        /// <returns>  </returns>
        ///
        /// <remarks>  </remarks>
        ///
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
        ///
        /// </summary>
        ///
        /// <param name="path"> path that was not found </param>
        ///
        /// <param name="errorId"> error identifier </param>
        ///
        /// <returns> ErrorRecord instance </returns>
        ///
        /// <remarks>  </remarks>
        ///
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
        ///
        /// <param name="resourceStr"> resource string </param>
        ///
        /// <param name="errorId"> error identifier </param>
        ///
        /// <param name="args"> replacement params for resource string formatting </param>
        ///
        /// <returns>  </returns>
        ///
        /// <remarks>  </remarks>
        ///
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
        ///
        /// <param name="e"> exception to include in ErrorRecord </param>
        ///
        /// <param name="errorId"> error identifier </param>
        ///
        /// <returns>  </returns>
        ///
        /// <remarks>  </remarks>
        ///
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
        ///
        /// <param name="cmdlet"> cmdlet instance </param>
        ///
        /// <param name="path"> provider path </param>
        ///
        /// <returns>
        /// filesystem path if all conditions are true,
        /// null otherwise
        /// </returns>
        ///
        /// <remarks>  </remarks>
        ///
        internal static string GetFilePathOfExistingFile(PSCmdlet cmdlet,
                                                         string path)
        {
            string resolvedProviderPath = cmdlet.SessionState.Path.GetUnresolvedProviderPathFromPSPath(path);
            if (Utils.NativeFileExists(resolvedProviderPath))
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

