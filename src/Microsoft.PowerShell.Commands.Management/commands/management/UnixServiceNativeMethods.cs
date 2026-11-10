// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

#if UNIX

using System.Runtime.InteropServices;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Native methods used by the Unix service cmdlets.
    /// </summary>
    internal static class UnixServiceNativeMethods
    {
        /// <summary>
        /// Gets the effective user id of the current process.
        /// </summary>
        /// <returns>The effective user id.</returns>
        internal static uint GetEffectiveUserId() => geteuid();

        /// <summary>
        /// Gets the real user id of the current process.
        /// </summary>
        /// <returns>The real user id.</returns>
        internal static uint GetUserId() => getuid();

        [DllImport("libc",
            EntryPoint = "geteuid",
            CallingConvention = CallingConvention.Cdecl,
            SetLastError = false)]
        private static extern uint geteuid();

        [DllImport("libc",
            EntryPoint = "getuid",
            CallingConvention = CallingConvention.Cdecl,
            SetLastError = false)]
        private static extern uint getuid();
    }
}

#endif
