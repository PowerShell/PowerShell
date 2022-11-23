// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_BASIC_INFORMATION
        {
            public nint ExitStatus;
            public nint PebBaseAddress;
            public nint AffinityMask;
            public nint BasePriority;
            public nint UniqueProcessId;
            public nint InheritedFromUniqueProcessId;
        }

        [LibraryImport("ntdll.dll")]
        internal static partial int NtQueryInformationProcess(
                nint processHandle,
                int processInformationClass,
                out PROCESS_BASIC_INFORMATION processInformation,
                int processInformationLength,
                out int returnLength);
    }
}
