// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Management.Automation;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Keep native struct names.")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:AccessibleFieldsMustBeginWithUpperCaseLetter", Justification = "Keep native struct names.")]
    internal static unsafe partial class Windows
    {
        internal const int MAX_PATH = 260;

        internal struct FILE_TIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal unsafe struct WIN32_FIND_DATA
        {
            internal uint dwFileAttributes;
            internal FILE_TIME ftCreationTime;
            internal FILE_TIME ftLastAccessTime;
            internal FILE_TIME ftLastWriteTime;
            internal uint nFileSizeHigh;
            internal uint nFileSizeLow;
            internal uint dwReserved0;
            internal uint dwReserved1;
            internal fixed char cFileName[MAX_PATH];
            internal fixed char cAlternateFileName[14];
        }

        internal sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private SafeFindHandle() : base(true) { }

            protected override bool ReleaseHandle()
            {
                return Interop.Windows.FindClose(this.handle);
            }
        }

        // We use 'FindFirstFileW' instead of 'FindFirstFileExW' because the latter doesn't work correctly with Unicode file names on FAT32.
        // See https://github.com/PowerShell/PowerShell/issues/16804
        [LibraryImport("api-ms-win-core-file-l1-1-0.dll", EntryPoint = "FindFirstFileW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static partial SafeFindHandle FindFirstFileW(string lpFileName, ref WIN32_FIND_DATA lpFindFileData);

        internal static SafeFindHandle FindFirstFile(string lpFileName, ref WIN32_FIND_DATA lpFindFileData)
        {
            lpFileName = Path.TrimEndingDirectorySeparator(lpFileName);
            lpFileName = PathUtils.EnsureExtendedPrefixIfNeeded(lpFileName);

            return FindFirstFileW(lpFileName, ref lpFindFileData);
        }
    }
}
