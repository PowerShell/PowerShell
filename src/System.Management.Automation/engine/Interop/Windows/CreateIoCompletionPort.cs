// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {
        [LibraryImport("kernel32.dll", SetLastError = true)]
        internal static partial nint CreateIoCompletionPort(
            nint FileHandle,
            nint ExistingCompletionPort,
            nint CompletionKey,
            int NumberOfConcurrentThreads);
    }
}
