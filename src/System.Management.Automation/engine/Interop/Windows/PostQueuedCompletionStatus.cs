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
        private static partial bool PostQueuedCompletionStatus(
            SafeIoCompletionPort CompletionPort,
            int lpNumberOfBytesTransferred,
            nint lpCompletionKey,
            nint lpOverlapped);

        internal static bool PostQueuedCompletionStatus(
            SafeIoCompletionPort completionPort,
            int status)
        {
            return PostQueuedCompletionStatus(completionPort, status, nint.Zero, nint.Zero);
        }
    }
}
