// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {
        internal const int JobObjectAssociateCompletionPortInformation = 7;
        internal const int JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO = 4;

        [StructLayout(LayoutKind.Sequential)]
        internal struct JOBOBJECT_ASSOCIATE_COMPLETION_PORT
        {
            public nint CompletionKey;
            public nint CompletionPort;
        }

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetInformationJobObject(
            nint hJob,
            int JobObjectInformationClass,
            nint lpJobObjectInformation,
            int cbJobObjectInformationLength);

        internal static bool SetInformationJobObject(
            nint jobHandle,
            ref JOBOBJECT_ASSOCIATE_COMPLETION_PORT completionPort)
        {
            unsafe
            {
                fixed (JOBOBJECT_ASSOCIATE_COMPLETION_PORT* completionPortPtr = &completionPort)
                {
                    return SetInformationJobObject(
                        jobHandle,
                        JobObjectAssociateCompletionPortInformation,
                        (nint)completionPortPtr,
                        Marshal.SizeOf<JOBOBJECT_ASSOCIATE_COMPLETION_PORT>());
                }
            }
        }
    }
}
