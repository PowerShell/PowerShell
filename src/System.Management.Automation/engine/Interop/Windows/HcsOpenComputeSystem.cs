// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {
        internal const int GENERIC_ALL = 0x10000000;
        internal const int HCS_Ok = 0;

        [LibraryImport("ComputeCore.dll", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial uint HcsOpenComputeSystem(string id, int access, out nint computeSystem);
    }
}
