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
            if (HasLoginSpecified(args, out int loginIndex))
            {
                return ExecPwshLogin(args, loginIndex);
            }
            return UnmanagedPSEntry.Start(string.Empty, args, args.Length);
        }

        private static bool HasLoginSpecified(string[] args, out int loginIndex)
        {
            loginIndex = -1;

            ReadOnlySpan<char> loginParam = stackalloc char[] { 'l', 'o', 'g', 'i', 'n' };
            ReadOnlySpan<char> loginParamUpper = stackalloc char[] { 'L', 'O', 'G', 'I', 'N' };
            ReadOnlySpan<char> fileParam = stackalloc char[] { 'f', 'i', 'l', 'e' };
            ReadOnlySpan<char> fileParamUpper = stackalloc char[] { 'F', 'I', 'L', 'E' };
            ReadOnlySpan<char> commandParam = stackalloc char[] { 'c', 'o', 'm', 'm', 'a', 'n', 'd' };
            ReadOnlySpan<char> commandParamUpper = stackalloc char[] { 'C', 'O', 'M', 'M', 'A', 'N', 'D' };

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                // Too short to be like -Login
                if (arg.Length < 2) { continue; }

                // Check for "-Login" or some prefix thereof
                if (arg[0] == '-')
                {
                    if (IsParam(arg, in loginParam, in loginParamUpper))
                    {
                        loginIndex = i;
                        return true;
                    }

                    if (IsParam(arg, in fileParam, in fileParamUpper)
                        || IsParam(arg, in commandParam, in commandParamUpper))
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        private static bool IsParam(
            string arg,
            in ReadOnlySpan<char> paramToCheck,
            in ReadOnlySpan<char> paramToCheckUpper)
        {
            if (arg.Length > paramToCheck.Length + 1)
            {
                return false;
            }

            for (int i = 1; i < arg.Length - 1; i++)
            {
                if (arg[i] != paramToCheck[i-1]
                    && arg[i] != paramToCheckUpper[i-1])
                {
                    return false;
                }
            }

            return true;
        }

        private static int ExecPwshLogin(string[] args, int loginArgIndex)
        {
            string pwshPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

            int quotedPwshPathLength = GetQuotedPathLength(pwshPath);
            string pwshInvocation = string.Create(
                10 + quotedPwshPathLength, // exec '{pwshPath}' "$@"
                (pwshPath, quotedPwshPathLength),
                CreatePwshInvocation);

            var execArgs = new string[args.Length + 4];
            execArgs[0] = "-l";
            execArgs[1] = "-c";
            execArgs[2] = pwshInvocation;
            execArgs[3] = "";

            int i = 0;
            int j = 4;
            for (; i < args.Length; i++)
            {
                if (i == loginArgIndex) { continue; }

                execArgs[j] = args[i];
                j++;
            }

            execArgs[execArgs.Length - 1] = null;

            int exitCode = Exec("/bin/sh", execArgs);

            if (exitCode != 0)
            {
                int errno = Marshal.GetLastWin32Error();
                Console.WriteLine($"Error: {errno}");
                return errno;
            }

            return 0;
        }

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
            strBuf[0] = 'e';
            strBuf[1] = 'x';
            strBuf[2] = 'e';
            strBuf[3] = 'c';
            strBuf[4] = ' ';
            
            Span<char> pathSpan = strBuf.Slice(5, pwshPath.quotedLength);
            QuoteAndWriteToSpan(pwshPath.path, pathSpan);

            int argIndex = 5 + pwshPath.quotedLength;
            strBuf[argIndex]     = ' ';
            strBuf[argIndex + 1] = '"';
            strBuf[argIndex + 2] = '$';
            strBuf[argIndex + 3] = '@';
            strBuf[argIndex + 4] = '"';
        }

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
                    span[j] = '\\';
                    j++;
                }

                span[j] = c;
            }

            span[j] = '\'';
        }

        [DllImport("libc",
            EntryPoint = "execv",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi,
            SetLastError = true)]
        private static extern int Exec(string path, string[] args);
    }
}
