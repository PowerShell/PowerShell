// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Windows
    {
        internal const int TOKEN_QUERY = 0x0008;

        [LibraryImport("api-ms-win-core-processsecurity-l1-1-0.dll",  SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool OpenProcessToken(nint ProcessHandle, int DesiredAccess, out nint TokenHandle);
    }
}
