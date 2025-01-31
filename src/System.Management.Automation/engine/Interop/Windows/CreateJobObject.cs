// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Windows
    {
        internal sealed class SafeJobHandle : SafeHandle
        {
            public SafeJobHandle() : base(invalidHandleValue: nint.Zero, ownsHandle: true) { }

            public override bool IsInvalid => handle == nint.Zero;

            protected override bool ReleaseHandle()
                => Windows.CloseHandle(handle);
        }

        [LibraryImport("kernel32.dll", EntryPoint = "CreateJobObjectW", SetLastError = true)]
        private static partial SafeJobHandle CreateJobObject(
            nint lpJobAttributes,
            nint lpName);

        internal static SafeJobHandle CreateJobObject()
            => CreateJobObject(nint.Zero, nint.Zero);
    }
}
