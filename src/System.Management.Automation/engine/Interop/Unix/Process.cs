// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Unix
    {
        /// <summary>The <c>SIGKILL</c> signal number (same value on Linux and macOS).</summary>
        internal const int SIGKILL = 9;

        /// <summary>The <c>WNOHANG</c> option for <c>waitpid</c> (same value on Linux and macOS).</summary>
        internal const int WNOHANG = 1;

        /// <summary>Send signal <paramref name="sig"/> to process <paramref name="pid"/>.</summary>
        /// <param name="pid">The target process id.</param>
        /// <param name="sig">The signal to send (for example <see cref="SIGKILL"/>).</param>
        /// <returns>0 on success, -1 on failure (see <c>errno</c>).</returns>
        [LibraryImport("libc", EntryPoint = "kill", SetLastError = true)]
        internal static partial int Kill(int pid, int sig);

        /// <summary>Wait for state changes in a child process.</summary>
        /// <param name="pid">The child process id to wait for.</param>
        /// <param name="status">Pointer to receive status information, or <see cref="IntPtr.Zero"/> to ignore it.</param>
        /// <param name="options">Bitwise options such as <see cref="WNOHANG"/>.</param>
        /// <returns>The pid of the child whose state changed, 0 for <see cref="WNOHANG"/> with no change, or -1 on error.</returns>
        [LibraryImport("libc", EntryPoint = "waitpid", SetLastError = true)]
        internal static partial int WaitPid(int pid, IntPtr status, int options);

        /// <summary>Linux: return the caller's kernel thread id via <c>gettid(2)</c>.</summary>
        /// <returns>The current thread id.</returns>
        [LibraryImport("libc", EntryPoint = "gettid")]
        internal static partial int GetTid();

        /// <summary>macOS: return the caller's integral thread id via <c>pthread_threadid_np</c>.</summary>
        /// <param name="thread">The pthread to query, or <see cref="IntPtr.Zero"/> for the current thread.</param>
        /// <param name="threadId">Receives the integral thread id.</param>
        /// <returns>0 on success.</returns>
        [LibraryImport("libc", EntryPoint = "pthread_threadid_np")]
        internal static partial int PthreadThreadIdNp(IntPtr thread, out ulong threadId);

        // macOS process information via libproc's proc_pidinfo(PROC_PIDTBSDINFO), which returns a
        // struct proc_bsdinfo. Rather than model that whole structure, only the two 32-bit fields
        // that are needed are read from stable offsets in the returned buffer.
        private const int PROC_PIDTBSDINFO = 3;
        private const int PbiPpidOffset = 16;   // offset of pbi_ppid in struct proc_bsdinfo
        private const int PbiUidOffset = 20;    // offset of pbi_uid in struct proc_bsdinfo
        private const int ProcBsdInfoSize = 160; // >= sizeof(struct proc_bsdinfo)

        [LibraryImport("libproc", EntryPoint = "proc_pidinfo", SetLastError = true)]
        private static partial int ProcPidInfo(int pid, int flavor, ulong arg, byte[] buffer, int bufferSize);

        private static bool TryGetProcBsdInfo(int pid, out uint parentPid, out uint userId)
        {
            parentPid = 0;
            userId = 0;

            byte[] buffer = new byte[ProcBsdInfoSize];
            int written = ProcPidInfo(pid, PROC_PIDTBSDINFO, 0, buffer, buffer.Length);
            if (written <= PbiUidOffset + sizeof(uint))
            {
                return false;
            }

            parentPid = BitConverter.ToUInt32(buffer, PbiPpidOffset);
            userId = BitConverter.ToUInt32(buffer, PbiUidOffset);
            return true;
        }

        /// <summary>macOS: get the parent process id for an arbitrary <paramref name="pid"/>.</summary>
        /// <param name="pid">The process id to query.</param>
        /// <returns>The parent process id, or -1 if it could not be determined.</returns>
        internal static int GetParentPid(int pid)
        {
            return TryGetProcBsdInfo(pid, out uint parentPid, out _) ? (int)parentPid : -1;
        }

        /// <summary>macOS: get the (effective) owner user id for an arbitrary <paramref name="pid"/>.</summary>
        /// <param name="pid">The process id to query.</param>
        /// <param name="userId">Receives the owning user id on success.</param>
        /// <returns><see langword="true"/> on success; otherwise <see langword="false"/>.</returns>
        internal static bool TryGetProcessUserId(int pid, out uint userId)
        {
            return TryGetProcBsdInfo(pid, out _, out userId);
        }
    }
}
