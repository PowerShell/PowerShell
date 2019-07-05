// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// Defines an entry point for the .NET CLI "powershell" app.
    /// </summary>
    public sealed class ManagedPSEntry
    {
        /// <summary>
        /// Starts the managed MSH.
        /// </summary>
        /// <param name="args">
        /// Command line arguments to the managed MSH
        /// </param>
        public static int Main(string[] args)
        {
#if UNIX
            if (HasLoginSpecified(args, out int loginIndex))
            {
                return ExecPwshLogin(args, loginIndex);
            }
#endif
            return UnmanagedPSEntry.Start(string.Empty, args, args.Length);
        }

#if UNIX
        /// <summary>
        /// Checks args to see if -Login has been specified.
        /// </summary>
        /// <param name="args">Arguments passed to the program.</param>
        /// <param name="loginIndex">The arg index (in argv) where -Login was found.</param>
        /// <returns></returns>
        private static bool HasLoginSpecified(string[] args, out int loginIndex)
        {
            loginIndex = -1;

            // Parameter comparison strings, stackalloc'd for performance
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                // Must look like '-<name>'
                if (arg == null || arg.Length < 2 || arg[0] != '-')
                { 
                    continue;
                }

                // Check for "-Login" or some prefix thereof
                if (IsParam(arg, "login", "LOGIN"))
                {
                    loginIndex = i;
                    return true;
                }

                // After -File and -Command, all parameters are passed
                // to the invoked file or command, so we can stop looking.
                if (IsParam(arg, "file", "FILE")
                    || IsParam(arg, "command", "COMMAND"))
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if a given parameter is the one we're looking for.
        /// Assumes any prefix determines that parameter (true for -l, -c and -f).
        /// </summary>
        /// <param name="arg">The argument to check.</param>
        /// <param name="paramToCheck">The lowercase name of the parameter to check.</param>
        /// <param name="paramToCheckUpper">The uppercase name of the parameter to check.</param>
        /// <returns></returns>
        private static bool IsParam(
            string arg,
            string paramToCheck,
            string paramToCheckUpper)
        {
            // Quick fail if the argument is longer than the parameter
            if (arg.Length > paramToCheck.Length + 1)
            {
                return false;
            }

            // Check arg chars in order and allow prefixes
            for (int i = 1; i < arg.Length; i++)
            {
                if (arg[i] != paramToCheck[i-1]
                    && arg[i] != paramToCheckUpper[i-1])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Create the exec call to /bin/{ba}sh -l -c 'pwsh "$@"' and run it.
        /// </summary>
        /// <param name="args">The argument vector passed to pwsh.</param>
        /// <param name="loginArgIndex">The index of -Login in the argument vector.</param>
        /// <returns>
        /// The exit code of exec if it fails.
        /// If exec succeeds, this process is overwritten so we never actually return.
        /// </returns>
        private static int ExecPwshLogin(string[] args, int loginArgIndex)
        {
            // We need the path to the current pwsh executable
            string pwshPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

            // Create input for /bin/sh that execs pwsh
            int quotedPwshPathLength = GetQuotedPathLength(pwshPath);
            string pwshInvocation = string.Create(
                10 + quotedPwshPathLength, // exec '{pwshPath}' "$@"
                (pwshPath, quotedPwshPathLength),
                CreatePwshInvocation);

            // Set up the arguments for /bin/sh
            var execArgs = new string[args.Length + 5];

            // execArgs[0] is set below to the correct shell executable

            // The command arguments
            execArgs[0] = "-"; // First argument is ignored
            execArgs[1] = "-l";
            execArgs[2] = "-c";
            execArgs[3] = pwshInvocation;
            execArgs[4] = "-"; // Required since exec ignores $0

            // Add the arguments passed to pwsh on the end
            int i = 0;
            int j = 5;
            for (; i < args.Length; i++)
            {
                if (i == loginArgIndex) { continue; }

                execArgs[j] = args[i];
                j++;
            }

            // A null is required by exec
            execArgs[execArgs.Length - 1] = null;

            // On macOS, sh doesn't support login, so we run /bin/bash
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Exec("/bin/zsh", execArgs);
            }

            return Exec("/bin/sh", execArgs);
        }

        /// <summary>
        /// Gets what the length of the given string will be if it's
        /// quote escaped for /bin/sh.
        /// </summary>
        /// <param name="str">The string to quote escape.</param>
        /// <returns>The length of the string when it's quote escaped.</returns>
        private static int GetQuotedPathLength(string str)
        {
            int length = 2;
            foreach (char c in str)
            {
                length++;
                if (c == '\'') { length++; }
            }

            return length;
        }

        private static void CreatePwshInvocation(Span<char> strBuf, (string path, int quotedLength) pwshPath)
        {
            // "exec "
            strBuf[0] = 'e';
            strBuf[1] = 'x';
            strBuf[2] = 'e';
            strBuf[3] = 'c';
            strBuf[4] = ' ';
            
            // The quoted path to pwsh, like "'/opt/microsoft/powershell/7/pwsh'"
            Span<char> pathSpan = strBuf.Slice(5, pwshPath.quotedLength);
            QuoteAndWriteToSpan(pwshPath.path, pathSpan);

            // ' "$@"' the argument vector splat to pass pwsh arguments through
            int argIndex = 5 + pwshPath.quotedLength;
            strBuf[argIndex]     = ' ';
            strBuf[argIndex + 1] = '"';
            strBuf[argIndex + 2] = '$';
            strBuf[argIndex + 3] = '@';
            strBuf[argIndex + 4] = '"';
        }

        /// <summary>
        /// Quotes (and sh quote escapes) a string and writes it to the given span.
        /// </summary>
        /// <param name="arg">The string to quote.</param>
        /// <param name="span">The span to write to.</param>
        private static void QuoteAndWriteToSpan(string arg, Span<char> span)
        {
            span[0] = '\'';

            int i = 0;
            int j = 1;
            for (; i < arg.Length; i++, j++)
            {
                char c = arg[i];

                if (c == '\'')
                {
                    // /bin/sh quote escaping uses backslashes
                    span[j] = '\\';
                    j++;
                }

                span[j] = c;
            }

            span[j] = '\'';
        }

        /// <summary>
        /// The `execv` syscall we use to exec /bin/sh.
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
        private static extern int Exec(string path, string[] args);
#endif
    }
}
