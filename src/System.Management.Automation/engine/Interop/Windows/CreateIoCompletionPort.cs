// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {
        [LibraryImport("api-ms-win-core-io-l1-1-1.dll", SetLastError = true)]
        internal static partial nint CreateIoCompletionPort(
            nint FileHandle,
            nint ExistingCompletionPort,
            nint CompletionKey,
            int NumberOfConcurrentThreads);
    }
}
