// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Unix
    {
        [LibraryImport("libc", EntryPoint = "geteuid", SetLastError = true)]
        internal static partial uint GetEuid();
    }
}
