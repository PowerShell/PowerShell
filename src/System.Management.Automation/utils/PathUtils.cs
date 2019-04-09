// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using System.Management.Automation.Internal;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// Defines generic utilities and helper methods for PowerShell.
    /// </summary>
    internal static class PathUtils
    {
        /// <summary>
        /// THE method for opening a file for writing.
        /// Should be used by all cmdlets that write to a file.
        /// </summary>
        /// <param name="cmdlet">Cmdlet that is opening the file (used mainly for error reporting).</param>
        /// <param name="filePath">Path to the file (as specified on the command line - this method will resolve the path).</param>
        /// <param name="encoding">Encoding (this method will convert the command line string to an Encoding instance).</param>
        /// <param name="defaultEncoding">If <c>true</c>, then we will use default .NET encoding instead of the encoding specified in <paramref name="encoding"/> parameter.</param>
        /// <param name="Append"></param>
        /// <param name="Force"></param>
        /// <param name="NoClobber"></param>
        /// <param name="fileStream">Result1: <see cref="FileStream"/> opened for writing.</param>
        /// <param name="streamWriter">Result2: <see cref="StreamWriter"/> (inherits from <see cref="TextWriter"/>) opened for writing.</param>
        /// <param name="readOnlyFileInfo">Result3: file info that should be used to restore file attributes after done with the file (<c>null</c> is this is not needed).</param>
        /// <param name="isLiteralPath">True if wildcard expansion should be bypassed.</param>
        internal static void MasterStreamOpen(
            PSCmdlet cmdlet,
            string filePath,
            string encoding,
            bool defaultEncoding,
            bool Append,
            bool Force,
            bool NoClobber,
            out FileStream fileStream,
            out StreamWriter streamWriter,
            out FileInfo readOnlyFileInfo,
            bool isLiteralPath
            )
        {
            Encoding resolvedEncoding = EncodingConversion.Convert(cmdlet, encoding);

            MasterStreamOpen(cmdlet, filePath, resolvedEncoding, defaultEncoding, Append, Force, NoClobber, out fileStream, out streamWriter, out readOnlyFileInfo, isLiteralPath);
        }

        /// <summary>
        /// THE method for opening a file for writing.
        /// Should be used by all cmdlets that write to a file.
        /// </summary>
        /// <param name="cmdlet">Cmdlet that is opening the file (used mainly for error reporting).</param>
        /// <param name="filePath">Path to the file (as specified on the command line - this method will resolve the path).</param>
        /// <param name="resolvedEncoding">Encoding (this method will convert the command line string to an Encoding instance).</param>
        /// <param name="defaultEncoding">If <c>true</c>, then we will use default .NET encoding instead of the encoding specified in <paramref name="encoding"/> parameter.</param>
        /// <param name="Append"></param>
        /// <param name="Force"></param>
        /// <param name="NoClobber"></param>
        /// <param name="fileStream">Result1: <see cref="FileStream"/> opened for writing.</param>
        /// <param name="streamWriter">Result2: <see cref="StreamWriter"/> (inherits from <see cref="TextWriter"/>) opened for writing.</param>
        /// <param name="readOnlyFileInfo">Result3: file info that should be used to restore file attributes after done with the file (<c>null</c> is this is not needed).</param>
        /// <param name="isLiteralPath">True if wildcard expansion should be bypassed.</param>
        internal static void MasterStreamOpen(
            PSCmdlet cmdlet,
            string filePath,
            Encoding resolvedEncoding,
            bool defaultEncoding,
            bool Append,
            bool Force,
            bool NoClobber,
            out FileStream fileStream,
            out StreamWriter streamWriter,
            out FileInfo readOnlyFileInfo,
            bool isLiteralPath)
        {
            fileStream = null;
            streamWriter = null;
            readOnlyFileInfo = null;

            // resolve the path and the encoding
            string resolvedPath = ResolveFilePath(filePath, cmdlet, isLiteralPath);

            try
            {
                // variable to track file open mode
                // this is controlled by append/force parameters
                FileMode mode = FileMode.Create;
                if (Append)
                {
                    mode = FileMode.Append;
                }
                else if (NoClobber)
                {
                    // throw IOException if file exists
                    mode = FileMode.CreateNew;
                }

                if (Force && (Append || !NoClobber))
                {
                    if (File.Exists(resolvedPath))
                    {
                        FileInfo fInfo = new FileInfo(resolvedPath);
                        if ((fInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        {
                            // remember to reset the read-only attribute later
                            readOnlyFileInfo = fInfo;
                            // Clear the read-only attribute
                            fInfo.Attributes &= ~(FileAttributes.ReadOnly);
                        }
                    }
                }

                // if the user knows what he/she is doing and uses "-Force" switch,
                // then we let more than 1 process write to the same file at the same time
                FileShare fileShare = Force ? FileShare.ReadWrite : FileShare.Read;

                // mode is controlled by force and ShouldContinue()
                fileStream = new FileStream(resolvedPath, mode, FileAccess.Write, fileShare);

                // create stream writer
                // NTRAID#Windows Out Of Band Releases-931008-2006/03/27
                // For some reason, calling this without specifying
                // the encoding is different from passing Encoding.Default.
                if (defaultEncoding)
                    streamWriter = new StreamWriter(fileStream);
                else
                    streamWriter = new StreamWriter(fileStream, resolvedEncoding);
            }
            // These are the known exceptions for File.Load and StreamWriter.ctor
            catch (ArgumentException e)
            {
                // NOTE: this call will throw
                ReportFileOpenFailure(cmdlet, resolvedPath, e);
            }
            catch (IOException e)
            {
                if (NoClobber && File.Exists(resolvedPath))
                {
                    // This probably happened because the file already exists
                    ErrorRecord errorRecord = new ErrorRecord(
                        e, "NoClobber", ErrorCategory.ResourceExists, resolvedPath);
                    errorRecord.ErrorDetails = new ErrorDetails(
                        cmdlet,
                        "PathUtilsStrings",
                        "UtilityFileExistsNoClobber",
                        filePath,
                        "NoClobber"); // prevents localization

                    // NOTE: this call will throw
                    cmdlet.ThrowTerminatingError(errorRecord);
                }
                // NOTE: this call will throw
                ReportFileOpenFailure(cmdlet, resolvedPath, e);
            }
            catch (UnauthorizedAccessException e)
            {
                // NOTE: this call will throw
                ReportFileOpenFailure(cmdlet, resolvedPath, e);
            }
            catch (NotSupportedException e)
            {
                // NOTE: this call will throw
                ReportFileOpenFailure(cmdlet, resolvedPath, e);
            }
            catch (System.Security.SecurityException e)
            {
                // NOTE: this call will throw
                ReportFileOpenFailure(cmdlet, resolvedPath, e);
            }
        }

        internal static void ReportFileOpenFailure(Cmdlet cmdlet, string filePath, Exception e)
        {
            ErrorRecord errorRecord = new ErrorRecord(
                e,
                "FileOpenFailure",
                ErrorCategory.OpenError,
                null);

            cmdlet.ThrowTerminatingError(errorRecord);
        }

        internal static StreamReader OpenStreamReader(PSCmdlet command, string filePath, Encoding encoding, bool isLiteralPath)
        {
            FileStream fileStream = OpenFileStream(filePath, command, isLiteralPath);
            return new StreamReader(fileStream, encoding);
        }

        internal static FileStream OpenFileStream(string filePath, PSCmdlet command, bool isLiteralPath)
        {
            string resolvedPath = PathUtils.ResolveFilePath(filePath, command, isLiteralPath);

            try
            {
                return new FileStream(resolvedPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            // These are the known exceptions for FileStream.ctor
            catch (ArgumentException e)
            {
                PathUtils.ReportFileOpenFailure(command, filePath, e);
                return null; // the line above will throw - silencing the compiler
            }
            catch (IOException e)
            {
                PathUtils.ReportFileOpenFailure(command, filePath, e);
                return null; // the line above will throw - silencing the compiler
            }
            catch (UnauthorizedAccessException e)
            {
                PathUtils.ReportFileOpenFailure(command, filePath, e);
                return null; // the line above will throw - silencing the compiler
            }
            catch (NotSupportedException e)
            {
                PathUtils.ReportFileOpenFailure(command, filePath, e);
                return null; // the line above will throw - silencing the compiler
            }
            catch (System.Management.Automation.DriveNotFoundException e)
            {
                PathUtils.ReportFileOpenFailure(command, filePath, e);
                return null; // the line above will throw - silencing the compiler
            }
        }

        /// <summary>
        /// Resolve a user provided file name or path (including globbing characters)
        /// to a fully qualified file path, using the file system provider.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        internal static string ResolveFilePath(string filePath, PSCmdlet command)
        {
            return ResolveFilePath(filePath, command, false);
        }

        /// <summary>
        /// Resolve a user provided file name or path (including globbing characters)
        /// to a fully qualified file path, using the file system provider.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="command"></param>
        /// <param name="isLiteralPath"></param>
        /// <returns></returns>
        internal static string ResolveFilePath(string filePath, PSCmdlet command, bool isLiteralPath)
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

                if (!provider.NameEquals(command.Context.ProviderNames.FileSystem))
                {
                    ReportWrongProviderType(command, provider.FullName);
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
                path = null;
            }

            if (string.IsNullOrEmpty(path))
            {
                CmdletProviderContext cmdletProviderContext = new CmdletProviderContext(command);
                ProviderInfo provider = null;
                PSDriveInfo drive = null;
                path =
                    command.SessionState.Path.GetUnresolvedProviderPathFromPSPath(
                        filePath, cmdletProviderContext, out provider, out drive);
                cmdletProviderContext.ThrowFirstErrorOrDoNothing();
                if (!provider.NameEquals(command.Context.ProviderNames.FileSystem))
                {
                    ReportWrongProviderType(command, provider.FullName);
                }
            }

            return path;
        }

        internal static void ReportWrongProviderType(Cmdlet cmdlet, string providerId)
        {
            string msg = StringUtil.Format(PathUtilsStrings.OutFile_ReadWriteFileNotFileSystemProvider, providerId);

            ErrorRecord errorRecord = new ErrorRecord(
                PSTraceSource.NewInvalidOperationException(),
                "ReadWriteFileNotFileSystemProvider",
                ErrorCategory.InvalidArgument,
                null);

            errorRecord.ErrorDetails = new ErrorDetails(msg);
            cmdlet.ThrowTerminatingError(errorRecord);
        }

        internal static void ReportMultipleFilesNotSupported(Cmdlet cmdlet)
        {
            string msg = StringUtil.Format(PathUtilsStrings.OutFile_MultipleFilesNotSupported);

            ErrorRecord errorRecord = new ErrorRecord(
                PSTraceSource.NewInvalidOperationException(),
                "ReadWriteMultipleFilesNotSupported",
                ErrorCategory.InvalidArgument,
                null);

            errorRecord.ErrorDetails = new ErrorDetails(msg);
            cmdlet.ThrowTerminatingError(errorRecord);
        }

        internal static void ReportWildcardingFailure(Cmdlet cmdlet, string filePath)
        {
            string msg = StringUtil.Format(PathUtilsStrings.OutFile_DidNotResolveFile, filePath);

            ErrorRecord errorRecord = new ErrorRecord(
                new FileNotFoundException(),
                "FileOpenFailure",
                ErrorCategory.OpenError,
                filePath);

            errorRecord.ErrorDetails = new ErrorDetails(msg);
            cmdlet.ThrowTerminatingError(errorRecord);
        }

        internal static DirectoryInfo CreateModuleDirectory(PSCmdlet cmdlet, string moduleNameOrPath, bool force)
        {
            Dbg.Assert(cmdlet != null, "Caller should verify cmdlet != null");
            Dbg.Assert(!string.IsNullOrEmpty(moduleNameOrPath), "Caller should verify !string.IsNullOrEmpty(moduleNameOrPath)");

            DirectoryInfo directoryInfo = null;
            try
            {
                string rootedPath = Microsoft.PowerShell.Commands.ModuleCmdletBase.ResolveRootedFilePath(moduleNameOrPath, cmdlet.Context);
                if (string.IsNullOrEmpty(rootedPath) && moduleNameOrPath.StartsWith(".", StringComparison.OrdinalIgnoreCase))
                {
                    PathInfo currentPath = cmdlet.CurrentProviderLocation(cmdlet.Context.ProviderNames.FileSystem);
                    rootedPath = Path.Combine(currentPath.ProviderPath, moduleNameOrPath);
                }

                if (string.IsNullOrEmpty(rootedPath))
                {
                    string personalModuleRoot = ModuleIntrinsics.GetPersonalModulePath();
                    rootedPath = Path.Combine(personalModuleRoot, moduleNameOrPath);
                }

                directoryInfo = new DirectoryInfo(rootedPath);
                if (directoryInfo.Exists)
                {
                    if (!force)
                    {
                        string errorMessage = string.Format(
                            CultureInfo.InvariantCulture, // directory name should be treated as culture-invariant
                            PathUtilsStrings.ExportPSSession_ErrorDirectoryExists,
                            directoryInfo.FullName);
                        ErrorDetails details = new ErrorDetails(errorMessage);
                        ErrorRecord errorRecord = new ErrorRecord(
                            new ArgumentException(details.Message),
                            "ExportProxyCommand_OutputDirectoryExists",
                            ErrorCategory.ResourceExists,
                            directoryInfo);
                        cmdlet.ThrowTerminatingError(errorRecord);
                    }
                }
                else
                {
                    directoryInfo.Create();
                }
            }
            catch (Exception e)
            {
                string errorMessage = string.Format(
                    CultureInfo.InvariantCulture, // directory name should be treated as culture-invariant
                    PathUtilsStrings.ExportPSSession_CannotCreateOutputDirectory,
                    moduleNameOrPath,
                    e.Message);
                ErrorDetails details = new ErrorDetails(errorMessage);
                ErrorRecord errorRecord = new ErrorRecord(
                    new ArgumentException(details.Message, e),
                    "ExportProxyCommand_CannotCreateOutputDirectory",
                    ErrorCategory.ResourceExists,
                    moduleNameOrPath);
                cmdlet.ThrowTerminatingError(errorRecord);
            }

            return directoryInfo;
        }

        internal static DirectoryInfo CreateTemporaryDirectory()
        {
            DirectoryInfo temporaryDirectory = new DirectoryInfo(Path.GetTempPath());
            DirectoryInfo moduleDirectory;
            do
            {
                moduleDirectory = new DirectoryInfo(
                    Path.Combine(
                        temporaryDirectory.FullName,
                        string.Format(
                            null,
                            "tmp_{0}",
                            Path.GetRandomFileName())));
            } while (moduleDirectory.Exists);

            Directory.CreateDirectory(moduleDirectory.FullName);
            return new DirectoryInfo(moduleDirectory.FullName);
        }

        internal static bool TryDeleteFile(string filepath)
        {
            if (IO.File.Exists(filepath))
            {
                try
                {
                    IO.File.Delete(filepath);
                    return true;
                }
                catch (IOException)
                {
                    // file is in use on Windows
                }
            }

            return false;
        }
    }
}
