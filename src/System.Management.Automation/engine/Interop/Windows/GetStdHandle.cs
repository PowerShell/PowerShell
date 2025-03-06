// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {
        internal const int STD_INPUT_HANDLE = -10;
        internal const int STD_OUTPUT_HANDLE = -11;
        internal const int STD_ERROR_HANDLE = -12;

        [LibraryImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true)]
        private static partial nint GetStdHandlePrivate(
            int nStdHandle);

        internal static SafeFileHandle GetStdHandle(int whichHandle)
        {
            nint handle = GetStdHandlePrivate(whichHandle);

            // The handle is a value stored in the process table and should not be closed.
            return new SafeFileHandle(handle, ownsHandle: false);
        }
    }
}
