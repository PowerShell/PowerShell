// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {
        [LibraryImport("ComputeCore.dll")]
        internal static partial void HcsCloseOperation(nint operationHandle);
    }
}
