// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {
        public const int INFINITE = -1;

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetQueuedCompletionStatus(
            SafeIoCompletionPort CompletionPort,
            out int lpNumberOfBytesTransferred,
            out nint lpCompletionKey,
            out nint lpOverlapped,
            int dwMilliseconds);

        internal static bool GetQueuedCompletionStatus(
            SafeIoCompletionPort completionPort,
            int timeoutMilliseconds,
            out int status)
        {
            return GetQueuedCompletionStatus(
                completionPort,
                out status,
                lpCompletionKey: out _,
                lpOverlapped: out _,
                timeoutMilliseconds);
        }
    }
}
