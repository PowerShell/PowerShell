// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {
        [LibraryImport("ComputeCore.dll", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial uint HcsCreateProcess(nint computeSystem, string processParameters, nint operationHandle, nint securityDescriptor, out nint processHandle);
    }
}
