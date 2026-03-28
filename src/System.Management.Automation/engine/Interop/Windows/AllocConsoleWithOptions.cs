// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {
        /// <summary>Console allocation mode for AllocConsoleWithOptions.</summary>
        internal enum AllocConsoleMode : int
        {
            /// <summary>Allocate only if the parent process requested it.</summary>
            Default = 0,

            /// <summary>Force allocation of a console with a visible window.</summary>
            NewWindow = 1,

            /// <summary>Allocate console I/O handles without creating a visible window.</summary>
            NoWindow = 2,
        }

        /// <summary>Result of an AllocConsoleWithOptions call.</summary>
        internal enum AllocConsoleResult : int
        {
            /// <summary>No console was allocated.</summary>
            NoConsole = 0,

            /// <summary>A new console session was created.</summary>
            NewConsole = 1,

            /// <summary>An existing console session was attached.</summary>
            ExistingConsole = 2,
        }

        /// <summary>Options struct passed to AllocConsoleWithOptions.</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct AllocConsoleOptions
        {
            /// <summary>The allocation mode (Default, NewWindow, or NoWindow).</summary>
            public AllocConsoleMode Mode;

            /// <summary>If non-zero, the ShowWindow field is used as the initial show state.</summary>
            public int UseShowWindow;

            /// <summary>The initial show state (e.g. SW_HIDE) when UseShowWindow is set.</summary>
            public ushort ShowWindow;
        }

        [LibraryImport("kernel32.dll")]
        internal static partial int AllocConsoleWithOptions(
            ref AllocConsoleOptions allocOptions,
            out AllocConsoleResult result);

        /// <summary>
        /// Attempts to allocate a console without a visible window using AllocConsoleWithOptions.
        /// Returns false if the API is not available (older Windows) or the call fails.
        /// </summary>
        internal static bool TryAllocConsoleNoWindow()
        {
            return TryAllocConsoleWithMode(AllocConsoleMode.NoWindow);
        }

        /// <summary>
        /// Attempts to allocate a console using AllocConsoleWithOptions with Default mode.
        /// Default mode respects DETACHED_PROCESS from the parent's CreateProcess call,
        /// whereas plain AllocConsole() would override it and force-create a console.
        /// Returns false if the API is not available (older Windows) or the call fails.
        /// </summary>
        internal static bool TryAllocConsoleDefault()
        {
            return TryAllocConsoleWithMode(AllocConsoleMode.Default);
        }

        private static bool TryAllocConsoleWithMode(AllocConsoleMode mode)
        {
            try
            {
                var options = new AllocConsoleOptions
                {
                    Mode = mode,
                    UseShowWindow = 0,
                    ShowWindow = 0,
                };

                int hr = AllocConsoleWithOptions(ref options, out _);
                return hr >= 0; // S_OK
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
        }
    }
}
