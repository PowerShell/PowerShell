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

                    if (IsParam(arg, in fileParam, in fileParamUpper))
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

            for (int i = 1; i < paramToCheck.Length; i++)
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

            var sb = new System.Text.StringBuilder("exec ", capacity: 256).Append(pwshPath);
            for (int i = 0; i < args.Length; i++)
            {
                if (i == loginArgIndex)
                {
                    continue;
                }

                sb.Append(' ');

                string arg = args[i];

                if (arg.StartsWith('-'))
                {
                    sb.Append(arg);
                    continue;
                }

                sb.Append('\'').Append(arg.Replace("'", "'\\''")).Append('\'');
            }
            string pwshInvocation = sb.ToString();

            string[] execArgs = new string[] { "-l", "-c", pwshInvocation, null };

            return Exec("/bin/sh", execArgs);
        }

        [DllImport("libc",
            EntryPoint = "execv",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi,
            SetLastError = true)]
        private static extern int Exec(string path, string[] args);
    }
}
