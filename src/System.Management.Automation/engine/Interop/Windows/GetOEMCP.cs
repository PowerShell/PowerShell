// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {
        [LibraryImport("api-ms-win-core-localization-l1-2-0.dll")]
        internal static partial uint GetOEMCP();
    }
}
