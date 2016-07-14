/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management.Automation;

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    public abstract class BinaryMiLogBase : PSCmdlet
    {
        internal const string Noun = "BinaryMiLog";

        internal BinaryMiLogBase()
        {
        }

        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

        #region Path utilities copied from internal admin/monad/src/utils/PathUtils.cs

        /* command.Context.ProviderNames.FileSystem is internal */
        private const string FileSystemProviderName = "FileSystem";

        private static string ResolveFilePath(string filePath, PSCmdlet command, bool isLiteralPath)
        {
            string path = null;

            try
            {
                ProviderInfo provider = null;
                PSDriveInfo drive = null;
                List<string> filePaths = new List<string>();

                if (isLiteralPath)
                {
                    filePaths.Add(command.SessionState.Path.GetUnresolvedProviderPathFromPSPath(filePath, out provider, out drive));
                }
                else
                {
                    filePaths.AddRange(command.SessionState.Path.GetResolvedProviderPathFromPSPath(filePath, out provider));
                }

                /* command.Context.ProviderNames.FileSystem is internal */
                if (!provider.Name.Equals(FileSystemProviderName, StringComparison.OrdinalIgnoreCase))
                {
                    ReportWrongProviderType(command, provider.Name);
                }

                if (filePaths.Count > 1)
                {
                    ReportMultipleFilesNotSupported(command);
                }
                if (filePaths.Count == 0)
                {
                    ReportWildcardingFailure(command, filePath);
                }

                path = filePaths[0];
            }
            catch (ItemNotFoundException)
            {
                if (!isLiteralPath)
                {
                    return ResolveFilePath(filePath, command, isLiteralPath: true);
                }
                else
                {
                    throw;
                }
            }
            return path;
        }

        internal string GetVerifiedFilePath(FileAccess fileAccess)
        {
            string resolvedPath = ResolveFilePath(this.Path, this, isLiteralPath: false);

            try
            {
                FileMode fileMode;
                FileShare fileShare;
                switch (fileAccess)
                {
                    case FileAccess.Read:
                        fileMode = FileMode.Open;
                        fileShare = FileShare.Read;
                        break;
                    case FileAccess.Write:
                        fileMode = FileMode.Create;
                        fileShare = FileShare.None;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("fileAccess");
                }

                using (new FileStream(resolvedPath, fileMode, fileAccess, fileShare))
                {
                    /* open and immediately close the file - to get exception in PowerShell style;  
                     * obviously there is a race here - we can still get error from mibincodec.dll later on */
                }
            }
            catch (ArgumentException e)
            {
                // NOTE: this call will throw
                ReportFileOpenFailure(this, resolvedPath, e);
            }
            catch (IOException e)
            {
                // NOTE: this call will throw
                ReportFileOpenFailure(this, resolvedPath, e);
            }
            catch (UnauthorizedAccessException e)
            {
                // NOTE: this call will throw
                ReportFileOpenFailure(this, resolvedPath, e);
            }
            catch (NotSupportedException e)
            {
                // NOTE: this call will throw
                ReportFileOpenFailure(this, resolvedPath, e);
            }
            catch (System.Security.SecurityException e)
            {
                // NOTE: this call will throw
                ReportFileOpenFailure(this, resolvedPath, e);
            }

            return resolvedPath;
        }

        private static void ReportFileOpenFailure(Cmdlet cmdlet, string filePath, Exception e)
        {
            ErrorRecord errorRecord = new ErrorRecord(
                e,
                "FileOpenFailure",
                ErrorCategory.OpenError,
                null);

            cmdlet.ThrowTerminatingError(errorRecord);
        }


        internal static void ReportWrongProviderType(Cmdlet cmdlet, string providerId)
        {
            string msg = string.Format(
                CultureInfo.InvariantCulture, /* providerId is culture invariant */
                Strings.ReadWriteFileNotFileSystemProvider, 
                providerId);

            var errorRecord = new ErrorRecord (
                new InvalidOperationException(msg), 
                "ReadWriteFileNotFileSystemProvider",
                ErrorCategory.InvalidArgument,
                null);

            errorRecord.ErrorDetails = new ErrorDetails(msg);
            cmdlet.ThrowTerminatingError(errorRecord);
        }

        internal static void ReportMultipleFilesNotSupported(Cmdlet cmdlet)
        {
            string msg = Strings.MultipleFilesNotSupported;

            var errorRecord = new ErrorRecord (
                new InvalidOperationException(msg), 
                "ReadWriteMultipleFilesNotSupported",
                ErrorCategory.InvalidArgument,
                null);

            errorRecord.ErrorDetails = new ErrorDetails(msg);
            cmdlet.ThrowTerminatingError(errorRecord);
        }

        internal static void ReportWildcardingFailure(Cmdlet cmdlet, string filePath)
        {
            string msg = string.Format(
                CultureInfo.InvariantCulture, /* filePath is culture invariant */
                Strings.DidNotResolveFile, 
                filePath);

            var errorRecord = new ErrorRecord (
                new FileNotFoundException(msg),
                "FileOpenFailure",
                ErrorCategory.OpenError,
                filePath);

            errorRecord.ErrorDetails = new ErrorDetails(msg);
            cmdlet.ThrowTerminatingError(errorRecord);
        }

        #endregion

    }
}
