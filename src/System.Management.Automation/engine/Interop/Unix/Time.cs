// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Unix
    {
        /// <summary>
        /// The <c>struct timeval</c> layout used by <c>settimeofday</c>.
        /// On all 64-bit Linux and macOS platforms PowerShell supports, both members are 64-bit.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct Timeval
        {
            /// <summary>Whole seconds since the Unix epoch.</summary>
            internal long Seconds;

            /// <summary>Additional microseconds.</summary>
            internal long Microseconds;
        }

        /// <summary>Set the system-wide clock.</summary>
        /// <param name="tv">The new time expressed as seconds/microseconds since the Unix epoch.</param>
        /// <param name="timezone">Should be <see cref="IntPtr.Zero"/>; the <c>timezone</c> argument is obsolete.</param>
        /// <returns>0 on success, -1 on failure (see <c>errno</c>); requires appropriate privileges.</returns>
        [LibraryImport("libc", EntryPoint = "settimeofday", SetLastError = true)]
        internal static partial int SetTimeOfDay(ref Timeval tv, IntPtr timezone);
    }
}
