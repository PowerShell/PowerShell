// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Windows
    {
        [LibraryImport("api-ms-win-core-processthreads-l1-1-0.dll")]
        internal static partial uint GetCurrentThreadId();
    }
}
