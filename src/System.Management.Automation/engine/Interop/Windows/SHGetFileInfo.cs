// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Keep native struct names.")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:AccessibleFieldsMustBeginWithUpperCaseLetter", Justification = "Keep native struct names.")]
    internal static partial class Windows
    {
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct SHFILEINFO
        {
            internal nint hIcon;
            internal int iIcon;
            internal uint dwAttributes;
            internal fixed char szDisplayName[260];
            internal fixed char szTypeName[80];

            public static readonly uint s_Size = (uint)sizeof(SHFILEINFO);
        }

        [LibraryImport("shell32.dll", EntryPoint = "SHGetFileInfoW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial nint SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        internal static int SHGetFileInfo(string pszPath)
        {
            // flag used to ask to return exe type
            const uint SHGFI_EXETYPE = 0x000002000;
            var shinfo = new SHFILEINFO();
            return (int)SHGetFileInfo(pszPath, 0, ref shinfo, SHFILEINFO.s_Size, SHGFI_EXETYPE);
        }
    }
}
