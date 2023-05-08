// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {
        [Flags]
        internal enum SymbolicLinkFlags
        {
            File = 0,
            Directory = 1,
            AllowUnprivilegedCreate = 2,
        }

        [LibraryImport("api-ms-win-core-file-l2-1-0.dll", EntryPoint = "CreateSymbolicLinkW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool CreateSymbolicLink(string name, string destination, SymbolicLinkFlags symbolicLinkFlags);
    }
}
