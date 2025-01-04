// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {
        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool PostQueuedCompletionStatus(
            nint CompletionPort,
            int lpNumberOfBytesTransferred,
            nint lpCompletionKey,
            nint lpOverlapped);
    }
}
