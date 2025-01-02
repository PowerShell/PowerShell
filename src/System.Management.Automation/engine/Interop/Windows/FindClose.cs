// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {
        [LibraryImport("api-ms-win-core-file-l1-1-0.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool FindClose(nint handle);
    }
}
