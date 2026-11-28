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
        /// <summary>The native error code reported when the service is already running.</summary>
        internal const int ERROR_SERVICE_ALREADY_RUNNING = 1056;

        /// <summary>The native error code reported when the service is not active.</summary>
        internal const int ERROR_SERVICE_NOT_ACTIVE = 1062;

        private const int X_OK = 1;

        /// <summary>
        /// Checks whether the current real user can execute the file at the given path,
        /// honoring ownership and group permissions. Used by command discovery so that a
        /// non-executable or inaccessible file on PATH doesn't hijack manager selection.
        /// </summary>
        /// <param name="path">The file path to check.</param>
        /// <returns>True when the file is executable by the current user.</returns>
        internal static bool CanExecute(string path)
        {
            return access(path, X_OK) == 0;
        }

        [DllImport("libc",
            EntryPoint = "access",
            SetLastError = true)]
        private static extern int access(string pathname, int mode);

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
