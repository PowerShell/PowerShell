// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {
        internal sealed class SafeIoCompletionPort : SafeHandle
        {
            public SafeIoCompletionPort() : base(nint.Zero, true) { }

            public override bool IsInvalid => handle == nint.Zero;

            protected override bool ReleaseHandle()
                => Windows.CloseHandle(handle);
        }

        [LibraryImport("kernel32.dll", SetLastError = true)]
        internal static partial SafeIoCompletionPort CreateIoCompletionPort(
            nint FileHandle,
            nint ExistingCompletionPort,
            nint CompletionKey,
            int NumberOfConcurrentThreads);
    }
}
