// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {
        internal enum AllocConsoleMode : int
        {
            Default = 0,
            NewWindow = 1,
            NoWindow = 2,
        }

        internal enum AllocConsoleResult : int
        {
            NoConsole = 0,
            NewConsole = 1,
            ExistingConsole = 2,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AllocConsoleOptions
        {
            public AllocConsoleMode Mode;
            public int UseShowWindow;
            public ushort ShowWindow;
        }

        [LibraryImport("kernel32.dll")]
        internal static partial int AllocConsoleWithOptions(
            ref AllocConsoleOptions allocOptions,
            out AllocConsoleResult result);
    }
}
