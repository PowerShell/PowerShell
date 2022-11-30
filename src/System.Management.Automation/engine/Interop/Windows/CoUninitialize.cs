// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Windows
    {
        [LibraryImport("api-ms-win-core-com-l1-1-0.dll")]
        internal static partial void CoUninitialize();
    }
}
