// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections;
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
                                WithCommand[0]
                            )
                        ),
                        "CommandNotFound",
                        ErrorCategory.InvalidArgument,
                        WithCommand[0]
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

            var env = Environment.GetEnvironmentVariables();
            var envBlock = new string?[env.Count + 1];
            int j = 0;
            foreach (DictionaryEntry entry in env)
            {
                envBlock[j++] = entry.Key + "=" + entry.Value;
            }

            envBlock[envBlock.Length - 1] = null;

            // setup termios for a child process as .NET modifies termios dynamically for use with ReadKey()
            ConfigureTerminalForChildProcess(true);
            int exitCode = Exec(command.Source, execArgs, envBlock);
            if (exitCode < 0)
            {
                ConfigureTerminalForChildProcess(false);
                ThrowTerminatingError(
                    new ErrorRecord(
                        new Exception(
                            string.Format(
                                System.Globalization.CultureInfo.InvariantCulture,
                                CommandBaseStrings.ExecFailed,
                                Marshal.GetLastPInvokeError(),
                                string.Join(' ', WithCommand)
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
        /// <param name="env">
        /// The environment variables to send through to the executable in the form of "key=value".
        /// Array must have its final element be null.
        /// <returns>
        /// An exit code if exec failed, but if successful the calling process will be overwritten.
        /// </returns>
        [DllImport("libc",
            EntryPoint = "execve",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi,
            SetLastError = true)]
        private static extern int Exec(string path, string?[] args, string?[] env);

        // leverage .NET runtime's native library which abstracts the need to handle different OS and architectures for termios api
        [DllImport("libSystem.Native", EntryPoint = "SystemNative_ConfigureTerminalForChildProcess")]
        private static extern void ConfigureTerminalForChildProcess([MarshalAs(UnmanagedType.Bool)] bool childUsesTerminal);
    }
}

#endif
