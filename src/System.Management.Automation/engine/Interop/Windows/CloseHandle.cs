// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Windows
    {
        [LibraryImport("api-ms-win-core-handle-l1-1-0.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CloseHandle(nint hObject);
    }
}
