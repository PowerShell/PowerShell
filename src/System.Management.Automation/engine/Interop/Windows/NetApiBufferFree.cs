// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {

        [LibraryImport("Netapi32.dll")]
        internal static partial int NetApiBufferFree(nint Buffer);
    }
}
