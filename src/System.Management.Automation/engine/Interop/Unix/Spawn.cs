// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Unix
    {
        private const int STDIN_FILENO = 0;
        private const int STDOUT_FILENO = 1;
        private const int STDERR_FILENO = 2;

        private const int F_SETFD = 2;
        private const int FD_CLOEXEC = 1;

        // POSIX_SPAWN_SETSID requests that the spawned process be created in a new
        // session (as if it called setsid()). The flag value differs between platforms.
        private const short PosixSpawnSetSidLinux = 0x0080;
        private const short PosixSpawnSetSidMacOS = 0x0400;

        // posix_spawn_file_actions_t and posix_spawnattr_t are opaque objects whose real
        // size varies by platform (largest is glibc's posix_spawnattr_t at a few hundred
        // bytes). A generously sized, zero-initialized buffer is allocated for each and the
        // init routine fills in the fields it needs; the extra space is harmless.
        private const nuint SpawnStructBufferSize = 512;

        [LibraryImport("libc", EntryPoint = "pipe", SetLastError = true)]
        private static unsafe partial int Pipe(int* pipeFds);

        [LibraryImport("libc", EntryPoint = "fcntl", SetLastError = true)]
        private static partial int Fcntl(int fd, int cmd, int arg);

        [LibraryImport("libc", EntryPoint = "close", SetLastError = true)]
        private static partial int Close(int fd);

        [LibraryImport("libc", EntryPoint = "posix_spawn", StringMarshalling = StringMarshalling.Utf8)]
        private static unsafe partial int PosixSpawn(out int pid, string path, IntPtr fileActions, IntPtr attr, byte** argv, byte** envp);

        [LibraryImport("libc", EntryPoint = "posix_spawn_file_actions_init")]
        private static partial int FileActionsInit(IntPtr fileActions);

        [LibraryImport("libc", EntryPoint = "posix_spawn_file_actions_destroy")]
        private static partial int FileActionsDestroy(IntPtr fileActions);

        [LibraryImport("libc", EntryPoint = "posix_spawn_file_actions_adddup2")]
        private static partial int FileActionsAddDup2(IntPtr fileActions, int fd, int newFd);

        [LibraryImport("libc", EntryPoint = "posix_spawn_file_actions_addchdir_np", StringMarshalling = StringMarshalling.Utf8)]
        private static partial int FileActionsAddChdir(IntPtr fileActions, string path);

        [LibraryImport("libc", EntryPoint = "posix_spawnattr_init")]
        private static partial int SpawnAttrInit(IntPtr attr);

        [LibraryImport("libc", EntryPoint = "posix_spawnattr_destroy")]
        private static partial int SpawnAttrDestroy(IntPtr attr);

        [LibraryImport("libc", EntryPoint = "posix_spawnattr_setflags")]
        private static partial int SpawnAttrSetFlags(IntPtr attr, short flags);

        /// <summary>
        /// Spawns a child process via <c>posix_spawn(3)</c>, optionally redirecting its
        /// standard streams through pipes. This replaces the former libpsl-native
        /// <c>ForkAndExecProcess</c> shim used for SSH based remoting. <c>posix_spawn</c> is used
        /// (rather than a hand-rolled <c>fork</c>/<c>execve</c>) because running managed code between
        /// <c>fork</c> and <c>execve</c> is not safe.
        /// </summary>
        /// <param name="filename">Full path of the executable to run (no PATH search is performed).</param>
        /// <param name="argv">Argument vector; <paramref name="argv"/>[0] is conventionally <paramref name="filename"/>.</param>
        /// <param name="envp">Environment as <c>KEY=VALUE</c> entries; fully replaces the child's environment.</param>
        /// <param name="cwd">Working directory for the child, or <see langword="null"/> to inherit the parent's.</param>
        /// <param name="redirectStdin">Whether to redirect the child's standard input through a pipe.</param>
        /// <param name="redirectStdout">Whether to redirect the child's standard output through a pipe.</param>
        /// <param name="redirectStderr">Whether to redirect the child's standard error through a pipe.</param>
        /// <param name="ownSession">
        /// When <see langword="true"/>, the child is created in its own session (POSIX_SPAWN_SETSID) so that
        /// terminal-generated signals such as Ctrl+C (SIGINT) do not propagate to it.
        /// </param>
        /// <param name="stdinFd">Receives the parent's writable end of the stdin pipe, or -1.</param>
        /// <param name="stdoutFd">Receives the parent's readable end of the stdout pipe, or -1.</param>
        /// <param name="stderrFd">Receives the parent's readable end of the stderr pipe, or -1.</param>
        /// <returns>The process id of the spawned child.</returns>
        /// <exception cref="Win32Exception">Thrown if the process could not be created.</exception>
        internal static unsafe int SpawnProcess(
            string filename,
            string[] argv,
            string[] envp,
            string? cwd,
            bool redirectStdin,
            bool redirectStdout,
            bool redirectStderr,
            bool ownSession,
            out int stdinFd,
            out int stdoutFd,
            out int stderrFd)
        {
            stdinFd = -1;
            stdoutFd = -1;
            stderrFd = -1;

            int stdinRead = -1, stdinWrite = -1;
            int stdoutRead = -1, stdoutWrite = -1;
            int stderrRead = -1, stderrWrite = -1;

            byte** argvPtr = null;
            byte** envpPtr = null;
            IntPtr fileActions = IntPtr.Zero;
            IntPtr attr = IntPtr.Zero;
            bool fileActionsInit = false;
            bool attrInit = false;

            try
            {
                if (redirectStdin && CreateCloexecPipe(out stdinRead, out stdinWrite) != 0)
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
                }

                if (redirectStdout && CreateCloexecPipe(out stdoutRead, out stdoutWrite) != 0)
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
                }

                if (redirectStderr && CreateCloexecPipe(out stderrRead, out stderrWrite) != 0)
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
                }

                fileActions = (IntPtr)NativeMemory.AllocZeroed(SpawnStructBufferSize);
                attr = (IntPtr)NativeMemory.AllocZeroed(SpawnStructBufferSize);

                int rc = FileActionsInit(fileActions);
                if (rc != 0)
                {
                    throw new Win32Exception(rc);
                }

                fileActionsInit = true;

                rc = SpawnAttrInit(attr);
                if (rc != 0)
                {
                    throw new Win32Exception(rc);
                }

                attrInit = true;

                // Map the child's ends of the redirection pipes onto its std file descriptors.
                // dup2 clears FD_CLOEXEC on the target descriptor so it survives execve, while the
                // original pipe descriptors (created with FD_CLOEXEC) close automatically on exec.
                if (redirectStdin && (rc = FileActionsAddDup2(fileActions, stdinRead, STDIN_FILENO)) != 0)
                {
                    throw new Win32Exception(rc);
                }

                if (redirectStdout && (rc = FileActionsAddDup2(fileActions, stdoutWrite, STDOUT_FILENO)) != 0)
                {
                    throw new Win32Exception(rc);
                }

                if (redirectStderr && (rc = FileActionsAddDup2(fileActions, stderrWrite, STDERR_FILENO)) != 0)
                {
                    throw new Win32Exception(rc);
                }

                if (cwd is not null && (rc = FileActionsAddChdir(fileActions, cwd)) != 0)
                {
                    throw new Win32Exception(rc);
                }

                if (ownSession)
                {
                    short flags = OperatingSystem.IsMacOS() ? PosixSpawnSetSidMacOS : PosixSpawnSetSidLinux;
                    if ((rc = SpawnAttrSetFlags(attr, flags)) != 0)
                    {
                        throw new Win32Exception(rc);
                    }
                }

                AllocNullTerminatedArray(argv, ref argvPtr);
                AllocNullTerminatedArray(envp, ref envpPtr);

                rc = PosixSpawn(out int childPid, filename, fileActions, attr, argvPtr, envpPtr);
                if (rc != 0)
                {
                    throw new Win32Exception(rc);
                }

                // Success: hand the parent's ends back to the caller and clear the locals so
                // the finally block does not close the descriptors that were returned.
                if (redirectStdin)
                {
                    stdinFd = stdinWrite;
                    stdinWrite = -1;
                }

                if (redirectStdout)
                {
                    stdoutFd = stdoutRead;
                    stdoutRead = -1;
                }

                if (redirectStderr)
                {
                    stderrFd = stderrRead;
                    stderrRead = -1;
                }

                return childPid;
            }
            finally
            {
                // Close the child's ends of the pipes (the parent never uses them). On failure the
                // parent's ends were not handed back, so these locals still hold them and are closed too.
                CloseIfOpen(stdinRead);
                CloseIfOpen(stdoutWrite);
                CloseIfOpen(stderrWrite);
                CloseIfOpen(stdinWrite);
                CloseIfOpen(stdoutRead);
                CloseIfOpen(stderrRead);

                if (fileActionsInit)
                {
                    FileActionsDestroy(fileActions);
                }

                if (attrInit)
                {
                    SpawnAttrDestroy(attr);
                }

                if (fileActions != IntPtr.Zero)
                {
                    NativeMemory.Free((void*)fileActions);
                }

                if (attr != IntPtr.Zero)
                {
                    NativeMemory.Free((void*)attr);
                }

                FreeArray(envpPtr, envp.Length);
                FreeArray(argvPtr, argv.Length);
            }
        }

        /// <summary>Creates a pipe with both ends marked close-on-exec.</summary>
        /// <param name="readFd">Receives the read end of the pipe, or -1 on failure.</param>
        /// <param name="writeFd">Receives the write end of the pipe, or -1 on failure.</param>
        /// <returns>0 on success, -1 on failure (the last P/Invoke error is preserved).</returns>
        private static unsafe int CreateCloexecPipe(out int readFd, out int writeFd)
        {
            readFd = -1;
            writeFd = -1;

            int* fds = stackalloc int[2];
            if (Pipe(fds) != 0)
            {
                return -1;
            }

            if (Fcntl(fds[0], F_SETFD, FD_CLOEXEC) == -1 || Fcntl(fds[1], F_SETFD, FD_CLOEXEC) == -1)
            {
                int savedError = Marshal.GetLastPInvokeError();
                Close(fds[0]);
                Close(fds[1]);
                Marshal.SetLastPInvokeError(savedError);
                return -1;
            }

            readFd = fds[0];
            writeFd = fds[1];
            return 0;
        }

        private static void CloseIfOpen(int fd)
        {
            if (fd >= 0)
            {
                Close(fd);
            }
        }

        private static unsafe void AllocNullTerminatedArray(string[] arr, ref byte** arrPtr)
        {
            int arrLength = arr.Length + 1; // +1 for the null terminator

            // Allocate the array of string pointers, with one extra element to null-terminate it.
            arrPtr = (byte**)Marshal.AllocHGlobal(sizeof(IntPtr) * arrLength);

            // Zero the memory so that a failed individual allocation can be cleaned up by
            // walking the array; the last element remains null (the terminator).
            for (int i = 0; i < arrLength; i++)
            {
                arrPtr[i] = null;
            }

            // Copy each string into unmanaged memory as a null-terminated array of UTF-8 bytes.
            for (int i = 0; i < arr.Length; i++)
            {
                byte[] byteArr = System.Text.Encoding.UTF8.GetBytes(arr[i]);

                arrPtr[i] = (byte*)Marshal.AllocHGlobal(byteArr.Length + 1); // +1 for the null terminator
                Marshal.Copy(byteArr, 0, (IntPtr)arrPtr[i], byteArr.Length);
                arrPtr[i][byteArr.Length] = (byte)'\0';
            }
        }

        private static unsafe void FreeArray(byte** arr, int length)
        {
            if (arr != null)
            {
                for (int i = 0; i < length; i++)
                {
                    if (arr[i] != null)
                    {
                        Marshal.FreeHGlobal((IntPtr)arr[i]);
                        arr[i] = null;
                    }
                }

                Marshal.FreeHGlobal((IntPtr)arr);
            }
        }
    }
}
