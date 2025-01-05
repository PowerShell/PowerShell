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
            public SafeJobHandle() : base(nint.Zero, true) { }

            public override bool IsInvalid => handle == nint.Zero;

            protected override bool ReleaseHandle()
                => Windows.CloseHandle(handle);
        }

        [LibraryImport("kernel32.dll", EntryPoint = "CreateJobObjectW", SetLastError = true)]
        internal static partial SafeJobHandle CreateJobObject(
            nint lpJobAttributes,
            nint lpName);
    }
}
