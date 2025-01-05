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
        internal static partial bool GetQueuedCompletionStatus(
            SafeIoCompletionPort CompletionPort,
            out int lpNumberOfBytesTransferred,
            out nint lpCompletionKey,
            out nint lpOverlapped,
            int dwMilliseconds);
    }
}
