// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Runtime.InteropServices;

using Dbg = System.Management.Automation.Diagnostics;

#if UNIX

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implements a cmdlet that allows use of execv API.
    /// </summary>
    [Cmdlet(VerbsCommon.Switch, "Process", HelpUri = "https://go.microsoft.com/fwlink/?linkid=2181448")]
    public sealed class SwitchProcessCommand : PSCmdlet
    {
        /// <summary>
        /// Get or set the command and arguments to replace the current pwsh process.
        /// </summary>
        [Parameter(Position = 0, Mandatory = false, ValueFromRemainingArguments = true)]
        public string[] WithCommand { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Execute the command and arguments
        /// </summary>
        protected override void EndProcessing()
        {
            if (WithCommand.Length == 0)
            {
                return;
            }

            // execv requires command to be full path so resolve command to first match
            var command = this.SessionState.InvokeCommand.GetCommand(WithCommand[0], CommandTypes.Application);
            if (command is null)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new CommandNotFoundException(
                            string.Format(
                                System.Globalization.CultureInfo.InvariantCulture,
                                CommandBaseStrings.NativeCommandNotFound,
                                command
                            )
                        ),
                        "CommandNotFound",
                        ErrorCategory.InvalidArgument,
                        WithCommand
                    )
                );
            }

            var execArgs = new string?[WithCommand.Length + 1];

            // execv convention is the first arg is the program name
            execArgs[0] = command.Name;

            for (int i = 1; i < WithCommand.Length; i++)
            {
                execArgs[i] = WithCommand[i];
            }

            // need null terminator at end
            execArgs[execArgs.Length - 1] = null;

            // set termios to reasonble default before execv since .NET modifies it during ReadKey
            Termios t;
            int exitCode = TcGetAttr(STDIN_FILENO, out t);
            bool resetTerminal = false;
            Termios old_t = t;
            if (exitCode == 0)
            {
                t.c_lflag = ECHOE | ECHOK | ECHOCTL | ECHOKE | PENDIN;
                t.c_iflag = IUTF8;
                t.c_cflag = CS8 | ~PARENB;
                t.c_oflag = ~OXTABS;
                exitCode = TcSetAttr(STDIN_FILENO, TCSANOW, ref t);
                if (exitCode == 0)
                {
                    Termios t2;
                    exitCode = TcGetAttr(STDIN_FILENO, out t2);
                    resetTerminal = true;
                }
            }

            exitCode = Exec(command.Source, execArgs);
            if (resetTerminal)
            {
                TcSetAttr(STDIN_FILENO, TCSANOW, ref old_t);
            }

            if (exitCode < 0)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new Exception(
                            string.Format(
                                System.Globalization.CultureInfo.InvariantCulture,
                                CommandBaseStrings.ExecFailed,
                                Marshal.GetLastPInvokeError(),
                                string.Join(" ", WithCommand)
                            )
                        ),
                        "ExecutionFailed",
                        ErrorCategory.InvalidOperation,
                        WithCommand
                    )
                );
            }
        }

        /// <summary>
        /// The `execv` POSIX syscall we use to exec /bin/sh.
        /// </summary>
        /// <param name="path">The path to the executable to exec.</param>
        /// <param name="args">
        /// The arguments to send through to the executable.
        /// Array must have its final element be null.
        /// </param>
        /// <returns>
        /// An exit code if exec failed, but if successful the calling process will be overwritten.
        /// </returns>
        [DllImport("libc",
            EntryPoint = "execv",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi,
            SetLastError = true)]
        private static extern int Exec(string path, string?[] args);

        [StructLayout(LayoutKind.Sequential)]
        private struct Termios
        {
            public int c_iflag; /* input flags */
            public int c_oflag; /* output flags */
            public int c_cflag; /* control flags */
            public int c_lflag; /* local flags */
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] c_cc; /* control chars */
            public int c_ispeed; /* input speed */
            public int c_ospeed; /* output speed */
        }

        [DllImport("libc",
            EntryPoint = "tcgetattr",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi,
            SetLastError = true)]
        private static extern int TcGetAttr(int fd, out Termios termios);

        [DllImport("libc",
            EntryPoint = "tcsetattr",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi,
            SetLastError = true)]
        private static extern int TcSetAttr(int fd, int optional_actions, ref Termios termios);

        private const int STDIN_FILENO = 0;
        private const int TCSANOW = 0;          // change immediately
        private const int ECHOE = (1 << 1); //0x00000010;   // visual erase for ERASE
        private const int ECHOK = (1 << 2); //0x00000020;   // echo newline after kill
        private const int ECHOCTL = (1 << 6); //0x00000200; // echo control characters as ^X
        private const int ECHOKE = (1 << 0); //0x00000800;  // visual erase for KILL
        private const int PENDIN = (1 << 29); //0x00004000;  // retype pending input
        private const int IUTF8 = 0x20000; // 0x00004000;   // UTF8
        private const int OXTABS = (1 << 2);
        private const int PARENB = (1 << 12);
        private const int CS8 = (1 << 8) | (1 << 9);
    }
}

#endif
