// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {
        internal delegate void HCS_OPERATION_COMPLETION(nint operationHandle, nint context);

        [LibraryImport("ComputeCore.dll")]
        internal static partial void HcsCreateOperation(HCS_OPERATION_COMPLETION callback, nint context);
    }
}
