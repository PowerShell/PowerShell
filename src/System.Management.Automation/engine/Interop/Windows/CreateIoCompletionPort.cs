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
            public SafeIoCompletionPort() : base(invalidHandleValue: nint.Zero, ownsHandle: true) { }

            public override bool IsInvalid => handle == nint.Zero;

            protected override bool ReleaseHandle()
                => Windows.CloseHandle(handle);
        }

        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial SafeIoCompletionPort CreateIoCompletionPort(
            nint FileHandle,
            nint ExistingCompletionPort,
            nint CompletionKey,
            int NumberOfConcurrentThreads);

        internal static SafeIoCompletionPort CreateIoCompletionPort()
        {
            return CreateIoCompletionPort(
                FileHandle: -1,
                ExistingCompletionPort: nint.Zero,
                CompletionKey: nint.Zero,
                NumberOfConcurrentThreads: 1);
        }
    }
}
