// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.PowerShell.Commands;
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
        /// <param name="defaultEncoding">If <see langword="true"/>, then we will use default .NET encoding instead of the encoding specified in <paramref name="encoding"/> parameter.</param>
        /// <param name="Append"></param>
        /// <param name="Force"></param>
        /// <param name="NoClobber"></param>
        /// <param name="fileStream">Result1: <see cref="FileStream"/> opened for writing.</param>
        /// <param name="streamWriter">Result2: <see cref="StreamWriter"/> (inherits from <see cref="TextWriter"/>) opened for writing.</param>
        /// <param name="readOnlyFileInfo">Result3: file info that should be used to restore file attributes after done with the file (<see langword="null"/> is this is not needed).</param>
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
        /// <param name="defaultEncoding">If <see langword="true"/>, then we will use default .NET encoding instead of the encoding specified in <paramref name="encoding"/> parameter.</param>
        /// <param name="Append"></param>
        /// <param name="Force"></param>
        /// <param name="NoClobber"></param>
        /// <param name="fileStream">Result1: <see cref="FileStream"/> opened for writing.</param>
        /// <param name="streamWriter">Result2: <see cref="StreamWriter"/> (inherits from <see cref="TextWriter"/>) opened for writing.</param>
        /// <param name="readOnlyFileInfo">Result3: file info that should be used to restore file attributes after done with the file (<see langword="null"/> is this is not needed).</param>
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
            string resolvedPath = ResolveFilePath(filePath, cmdlet, isLiteralPath);
            try
            {
                MasterStreamOpenImpl(
                    resolvedPath,
                    resolvedEncoding,
                    defaultEncoding,
                    Append,
                    Force,
                    NoClobber,
                    out fileStream,
                    out streamWriter,
                    out readOnlyFileInfo);
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
                    ErrorRecord errorRecord = new(
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

        /// <summary>
        /// THE method for opening a file for writing.
        /// Should be used by all cmdlets that write to a file.
        /// </summary>
        /// <param name="filePath">Path to the file (as specified on the command line - this method will resolve the path).</param>
        /// <param name="resolvedEncoding">Encoding (this method will convert the command line string to an Encoding instance).</param>
        /// <param name="defaultEncoding">If <see langword="true"/>, then we will use default .NET encoding instead of the encoding specified in <paramref name="encoding"/> parameter.</param>
        /// <param name="Append"></param>
        /// <param name="Force"></param>
        /// <param name="NoClobber"></param>
        /// <param name="fileStream">Result1: <see cref="FileStream"/> opened for writing.</param>
        /// <param name="streamWriter">Result2: <see cref="StreamWriter"/> (inherits from <see cref="TextWriter"/>) opened for writing.</param>
        /// <param name="readOnlyFileInfo">Result3: file info that should be used to restore file attributes after done with the file (<see langword="null"/> is this is not needed).</param>
        /// <param name="isLiteralPath">True if wildcard expansion should be bypassed.</param>
        internal static void MasterStreamOpen(
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
            string resolvedPath = ResolveFilePath(filePath, isLiteralPath);
            try
            {
                MasterStreamOpenImpl(
                    resolvedPath,
                    resolvedEncoding,
                    defaultEncoding,
                    Append,
                    Force,
                    NoClobber,
                    out fileStream,
                    out streamWriter,
                    out readOnlyFileInfo);
            }
            // These are the known exceptions for File.Load and StreamWriter.ctor
            catch (ArgumentException e)
            {
                AddFileOpenErrorRecord(e);
                throw;
            }
            catch (IOException e)
            {
                if (NoClobber && File.Exists(resolvedPath))
                {
                    string msg = StringUtil.Format(
                        PathUtilsStrings.UtilityFileExistsNoClobber,
                        filePath,
                        "NoClobber");

                    // This probably happened because the file already exists
                    ErrorRecord errorRecord = new ErrorRecord(
                        e, "NoClobber", ErrorCategory.ResourceExists, resolvedPath);
                    errorRecord.ErrorDetails = new ErrorDetails(msg);

                    e.Data[typeof(ErrorRecord)] = errorRecord;
                    throw;
                }

                AddFileOpenErrorRecord(e);
                throw;
            }
            catch (UnauthorizedAccessException e)
            {
                AddFileOpenErrorRecord(e);
                throw;
            }
            catch (NotSupportedException e)
            {
                AddFileOpenErrorRecord(e);
                throw;
            }
            catch (System.Security.SecurityException e)
            {
                AddFileOpenErrorRecord(e);
                throw;
            }

            static void AddFileOpenErrorRecord(Exception e)
            {
                ErrorRecord errorRecord = new ErrorRecord(
                    e,
                    "FileOpenFailure",
                    ErrorCategory.OpenError,
                    null);

                e.Data[typeof(ErrorRecord)] = errorRecord;
            }
        }

        /// <summary>
        /// THE method for opening a file for writing.
        /// Should be used by all cmdlets that write to a file.
        /// </summary>
        /// <param name="resolvedPath">Path to the file (as specified on the command line - this method will resolve the path).</param>
        /// <param name="resolvedEncoding">Encoding (this method will convert the command line string to an Encoding instance).</param>
        /// <param name="defaultEncoding">If <see langword="true"/>, then we will use default .NET encoding instead of the encoding specified in <paramref name="encoding"/> parameter.</param>
        /// <param name="Append"></param>
        /// <param name="Force"></param>
        /// <param name="NoClobber"></param>
        /// <param name="fileStream">Result1: <see cref="FileStream"/> opened for writing.</param>
        /// <param name="streamWriter">Result2: <see cref="StreamWriter"/> (inherits from <see cref="TextWriter"/>) opened for writing.</param>
        /// <param name="readOnlyFileInfo">Result3: file info that should be used to restore file attributes after done with the file (<see langword="null"/> is this is not needed).</param>
        internal static void MasterStreamOpenImpl(
            string resolvedPath,
            Encoding resolvedEncoding,
            bool defaultEncoding,
            bool Append,
            bool Force,
            bool NoClobber,
            out FileStream fileStream,
            out StreamWriter streamWriter,
            out FileInfo readOnlyFileInfo)
        {
            fileStream = null;
            streamWriter = null;
            readOnlyFileInfo = null;

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

        internal static void ReportFileOpenFailure(Cmdlet cmdlet, string filePath, Exception e)
        {
            ErrorRecord errorRecord = new ErrorRecord(
                e,
                "FileOpenFailure",
                ErrorCategory.OpenError,
                null);

            cmdlet.ThrowTerminatingError(errorRecord);
        }

        internal static void ReportFileOpenFailure(string filePath, Exception e)
        {
            ErrorRecord errorRecord = new ErrorRecord(
                e,
                "FileOpenFailure",
                ErrorCategory.OpenError,
                null);

            throw new RuntimeException(
                e.Message,
                errorRecord.Exception,
                errorRecord);
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

        /// <summary>
        /// Resolve a user provided file name or path (including globbing characters)
        /// to a fully qualified file path, using the file system provider.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="isLiteralPath"></param>
        /// <returns></returns>
        internal static string ResolveFilePath(string filePath, bool isLiteralPath)
        {
            string path = null;

            SessionState sessionState = LocalPipeline.GetExecutionContextFromTLS()?.EngineSessionState?.PublicSessionState;
            if (sessionState is null)
            {
                return null;
            }

            try
            {
                ProviderInfo provider = null;
                PSDriveInfo drive = null;
                List<string> filePaths = new();

                if (isLiteralPath)
                {
                    filePaths.Add(sessionState.Path.GetUnresolvedProviderPathFromPSPath(filePath, out provider, out drive));
                }
                else
                {
                    filePaths.AddRange(sessionState.Path.GetResolvedProviderPathFromPSPath(filePath, out provider));
                }

                if (!provider.NameEquals(FileSystemProvider.ProviderName))
                {
                    ReportWrongProviderType(provider.FullName);
                }

                if (filePaths.Count > 1)
                {
                    ReportMultipleFilesNotSupported();
                }

                if (filePaths.Count == 0)
                {
                    ReportWildcardingFailure(filePath);
                }

                path = filePaths[0];
            }
            catch (ItemNotFoundException)
            {
                path = null;
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

        internal static void ReportWrongProviderType(string providerId)
        {
            string msg = StringUtil.Format(PathUtilsStrings.OutFile_ReadWriteFileNotFileSystemProvider, providerId);

            PSInvalidOperationException exception = PSTraceSource.NewInvalidOperationException();

            ErrorRecord errorRecord = new(
                exception,
                "ReadWriteFileNotFileSystemProvider",
                ErrorCategory.InvalidArgument,
                null);

            errorRecord.ErrorDetails = new ErrorDetails(msg);
            exception.Data[typeof(ErrorRecord)] = errorRecord;
            throw exception;
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

        internal static void ReportMultipleFilesNotSupported()
        {
            string msg = StringUtil.Format(PathUtilsStrings.OutFile_MultipleFilesNotSupported);

            PSInvalidOperationException exception = PSTraceSource.NewInvalidOperationException();

            ErrorRecord errorRecord = new(
                exception,
                "ReadWriteMultipleFilesNotSupported",
                ErrorCategory.InvalidArgument,
                null);

            errorRecord.ErrorDetails = new ErrorDetails(msg);
            exception.Data[typeof(ErrorRecord)] = errorRecord;
            throw exception;
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

        internal static void ReportWildcardingFailure(string filePath)
        {
            string msg = StringUtil.Format(PathUtilsStrings.OutFile_DidNotResolveFile, filePath);

            FileNotFoundException exception = new();
            ErrorRecord errorRecord = new(
                exception,
                "FileOpenFailure",
                ErrorCategory.OpenError,
                filePath);

            errorRecord.ErrorDetails = new ErrorDetails(msg);
            exception.Data[typeof(ErrorRecord)] = errorRecord;
            throw exception;
        }

        internal static DirectoryInfo CreateModuleDirectory(PSCmdlet cmdlet, string moduleNameOrPath, bool force)
        {
            Dbg.Assert(cmdlet != null, "Caller should verify cmdlet != null");
            Dbg.Assert(!string.IsNullOrEmpty(moduleNameOrPath), "Caller should verify !string.IsNullOrEmpty(moduleNameOrPath)");

            DirectoryInfo directoryInfo = null;
            try
            {
                // Even if 'moduleNameOrPath' is a rooted path, 'ResolveRootedFilePath' may return null when the path doesn't exist yet,
                // or when it contains wildcards but cannot be resolved to a single path.
                string rootedPath = ModuleCmdletBase.ResolveRootedFilePath(moduleNameOrPath, cmdlet.Context);
                if (string.IsNullOrEmpty(rootedPath) && moduleNameOrPath.StartsWith('.'))
                {
                    PathInfo currentPath = cmdlet.CurrentProviderLocation(cmdlet.Context.ProviderNames.FileSystem);
                    rootedPath = Path.Combine(currentPath.ProviderPath, moduleNameOrPath);
                }

                if (string.IsNullOrEmpty(rootedPath))
                {
                    if (Path.IsPathRooted(moduleNameOrPath))
                    {
                        rootedPath = moduleNameOrPath;
                    }
                    else
                    {
                        string personalModuleRoot = ModuleIntrinsics.GetPersonalModulePath();
                        if (string.IsNullOrEmpty(personalModuleRoot))
                        {
                            cmdlet.ThrowTerminatingError(
                                new ErrorRecord(
                                    new ArgumentException(StringUtil.Format(PathUtilsStrings.ExportPSSession_ErrorModuleNameOrPath, moduleNameOrPath)),
                                    "ExportPSSession_ErrorModuleNameOrPath",
                                    ErrorCategory.InvalidArgument,
                                    cmdlet));
                        }

                        rootedPath = Path.Combine(personalModuleRoot, moduleNameOrPath);
                    }
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
                catch (UnauthorizedAccessException)
                {
                    // user does not have permissions
                }
            }

            return false;
        }

        #region Helpers for long paths from .Net Runtime

        // Code here is copied from .NET's internal path helper implementation:
        // https://github.com/dotnet/runtime/blob/dcce0f56e10f5ac9539354b049341a2d7c0cdebf/src/libraries/System.Private.CoreLib/src/System/IO/PathInternal.Windows.cs
        // It has been left as a verbatim copy.

#nullable enable

        /// <summary>
        /// Adds the extended path prefix (\\?\) if not already a device path, IF the path is not relative,
        /// AND the path is more than 259 characters. (> MAX_PATH + null). This will also insert the extended
        /// prefix if the path ends with a period or a space. Trailing periods and spaces are normally eaten
        /// away from paths during normalization, but if we see such a path at this point it should be
        /// normalized and has retained the final characters. (Typically from one of the *Info classes).
        /// </summary>
        /// <param name="path">File path.</param>
        /// <returns>File path (with extended prefix if the path is long path).</returns>
        [return: NotNullIfNotNull(nameof(path))]
        internal static string? EnsureExtendedPrefixIfNeeded(string? path)
        {
            if (path != null && (path.Length >= MaxShortPath || EndsWithPeriodOrSpace(path)))
            {
                return EnsureExtendedPrefix(path);
            }
            else
            {
                return path;
            }
        }

        internal static string EnsureExtendedPrefix(string path)
        {
            if (IsPartiallyQualified(path) || IsDevice(path))
                return path;

            // Given \\server\share in longpath becomes \\?\UNC\server\share
            if (path.StartsWith(UncPathPrefix, StringComparison.OrdinalIgnoreCase))
                return path.Insert(2, UncDevicePrefixToInsert);

            return ExtendedDevicePathPrefix + path;
        }

        private const string ExtendedDevicePathPrefix = @"\\?\";
        private const string UncPathPrefix = @"\\";
        private const string UncDevicePrefixToInsert = @"?\UNC\";
        private const string UncExtendedPathPrefix = @"\\?\UNC\";
        private const string DevicePathPrefix = @"\\.\";
        private const int MaxShortPath = 260;

        // \\?\, \\.\, \??\
        private const int DevicePrefixLength = 4;

        private static bool EndsWithPeriodOrSpace(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            char c = path[path.Length - 1];
            return c == ' ' || c == '.';
        }

        /// <summary>
        /// Returns true if the given character is a valid drive letter
        /// </summary>
        private static bool IsValidDriveChar(char value)
        {
            return ((value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z'));
        }

        private static bool IsDevice(string path)
        {
            return IsExtended(path)
                ||
                (
                    path.Length >= DevicePrefixLength
                    && IsDirectorySeparator(path[0])
                    && IsDirectorySeparator(path[1])
                    && (path[2] == '.' || path[2] == '?')
                    && IsDirectorySeparator(path[3])
                );
        }

        private static bool IsExtended(string path)
        {
            return path.Length >= DevicePrefixLength
                && path[0] == '\\'
                && (path[1] == '\\' || path[1] == '?')
                && path[2] == '?'
                && path[3] == '\\';
        }

        /// <summary>
        /// Returns true if the path specified is relative to the current drive or working directory.
        /// Returns false if the path is fixed to a specific drive or UNC path.  This method does no
        /// validation of the path (URIs will be returned as relative as a result).
        /// </summary>
        /// <remarks>
        /// Handles paths that use the alternate directory separator.  It is a frequent mistake to
        /// assume that rooted paths (Path.IsPathRooted) are not relative.  This isn't the case.
        /// "C:a" is drive relative- meaning that it will be resolved against the current directory
        /// for C: (rooted, but relative). "C:\a" is rooted and not relative (the current directory
        /// will not be used to modify the path).
        /// </remarks>
        private static bool IsPartiallyQualified(string path)
        {
            if (path.Length < 2)
            {
                // It isn't fixed, it must be relative.  There is no way to specify a fixed
                // path with one character (or less).
                return true;
            }

            if (IsDirectorySeparator(path[0]))
            {
                // There is no valid way to specify a relative path with two initial slashes or
                // \? as ? isn't valid for drive relative paths and \??\ is equivalent to \\?\
                return !(path[1] == '?' || IsDirectorySeparator(path[1]));
            }

            // The only way to specify a fixed path that doesn't begin with two slashes
            // is the drive, colon, slash format- i.e. C:\
            return !((path.Length >= 3)
                && (path[1] == Path.VolumeSeparatorChar)
                && IsDirectorySeparator(path[2])
                // To match old behavior we'll check the drive character for validity as the path is technically
                // not qualified if you don't have a valid drive. "=:\" is the "=" file's default data stream.
                && IsValidDriveChar(path[0]));
        }
        /// <summary>
        /// True if the given character is a directory separator.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDirectorySeparator(char c)
        {
            return c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
        }

        internal static Encoding GetPathEncoding(string path)
        {
            using StreamReader reader = new(path, Encoding.Default, detectEncodingFromByteOrderMarks: true);
            _ = reader.Read();
            return reader.CurrentEncoding;
        }

        #endregion
    }
}
