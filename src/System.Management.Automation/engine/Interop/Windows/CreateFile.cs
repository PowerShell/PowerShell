// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.IO;
using System.Management.Automation;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {
        // dwDesiredAccess of CreateFile
        [Flags]
        internal enum FileDesiredAccess : uint
        {
            GenericZero = 0,
            GenericRead = 0x80000000,
            GenericWrite = 0x40000000,
            GenericExecute = 0x20000000,
            GenericAll = 0x10000000,
        }

        // dwFlagsAndAttributes
        [Flags]
        internal enum FileAttributes : uint
        {
            Readonly = 0x00000001,
            Hidden = 0x00000002,
            System = 0x00000004,
            Archive = 0x00000020,
            Encrypted = 0x00004000,
            Write_Through = 0x80000000,
            Overlapped = 0x40000000,
            NoBuffering = 0x20000000,
            RandomAccess = 0x10000000,
            SequentialScan = 0x08000000,
            DeleteOnClose = 0x04000000,
            BackupSemantics = 0x02000000,
            PosixSemantics = 0x01000000,
            OpenReparsePoint = 0x00200000,
            OpenNoRecall = 0x00100000,
            SessionAware = 0x00800000,
            Normal = 0x00000080
        }

        // WARNING: This method does not implicitly handle long paths. Use CreateFile.
        [LibraryImport("api-ms-win-core-file-l1-1-0.dll", EntryPoint = "CreateFileW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static unsafe partial SafeFileHandle CreateFilePrivate(
            string lpFileName,
            uint dwDesiredAccess,
            FileShare dwShareMode,
            nint lpSecurityAttributes,
            FileMode dwCreationDisposition,
            FileAttributes dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [LibraryImport("api-ms-win-core-file-l1-1-0.dll", EntryPoint = "CreateFileW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static unsafe partial nint CreateFileWithPipeHandlePrivate(
            string lpFileName,
            uint dwDesiredAccess,
            FileShare dwShareMode,
            nint lpSecurityAttributes,
            FileMode dwCreationDisposition,
            FileAttributes dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        internal static unsafe SafeFileHandle CreateFileWithSafeFileHandle(
            string lpFileName,
            FileAccess dwDesiredAccess,
            FileShare dwShareMode,
            FileMode dwCreationDisposition,
            FileAttributes dwFlagsAndAttributes)
        {
            lpFileName = Path.TrimEndingDirectorySeparator(lpFileName);
            lpFileName = PathUtils.EnsureExtendedPrefixIfNeeded(lpFileName);

            return CreateFilePrivate(lpFileName, (uint)dwDesiredAccess, dwShareMode, nint.Zero, dwCreationDisposition, dwFlagsAndAttributes, nint.Zero);
        }

        internal static unsafe nint CreateFileWithPipeHandle(
            string lpFileName,
            FileAccess dwDesiredAccess,
            FileShare dwShareMode,
            FileMode dwCreationDisposition,
            FileAttributes dwFlagsAndAttributes)
        {
            return CreateFileWithPipeHandlePrivate(lpFileName, (uint)dwDesiredAccess, dwShareMode, nint.Zero, dwCreationDisposition, dwFlagsAndAttributes, nint.Zero);
        }
    }
}
