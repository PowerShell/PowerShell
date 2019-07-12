// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// Defines an entry point for the .NET CLI "powershell" app.
    /// </summary>
    public sealed class ManagedPSEntry
    {
        // MacOS p/Invoke constants
        private const int CTL_KERN = 1;
        private const int KERN_ARGMAX = 8;
        private const int KERN_PROCARGS2 = 49;
        private const int PROC_PIDPATHINFO_MAXSIZE = 4096;

        /// <summary>
        /// Starts the managed MSH.
        /// </summary>
        /// <param name="args">
        /// Command line arguments to the managed MSH
        /// </param>
        public static int Main(string[] args)
        {
#if UNIX
            System.Console.WriteLine($"PID: {System.Diagnostics.Process.GetCurrentProcess().Id}");
            while (!System.Diagnostics.Debugger.IsAttached)
            {
                System.Threading.Thread.Sleep(500);
            }

            int returnCode = AttemptExecPwshLogin(args);
            if (returnCode < 0)
            {
                // TODO: Report error
            }
#endif
            return UnmanagedPSEntry.Start(string.Empty, args, args.Length);
        }

#if UNIX
        private static int AttemptExecPwshLogin(string[] args)
        {
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            byte procNameFirstByte;
            bool procStartsWithMinus = false;
            string pwshPath;

            if (isLinux)
            {
                using (FileStream fs = File.OpenRead("/proc/self/cmdline"))
                {
                    procNameFirstByte = (byte)fs.ReadByte();
                }

                switch (procNameFirstByte)
                {
                    case 0x2B: // '+' signifies we have already done login check
                        return 0;
                    case 0x2D: // '-' means this is a login shell
                        procStartsWithMinus = true;
                        break;

                    // For any other char, we check for a login parameter
                }

                int loginArgIndex = -1;
                if (!procStartsWithMinus && !IsLogin(args, out loginArgIndex))
                {
                    return 0;
                }

                IntPtr linkPathPtr = Marshal.AllocHGlobal(PROC_PIDPATHINFO_MAXSIZE);
                ReadLink("/proc/self/exe", linkPathPtr, (UIntPtr)PROC_PIDPATHINFO_MAXSIZE);
                pwshPath = Marshal.PtrToStringAnsi(linkPathPtr);
                Marshal.FreeHGlobal(linkPathPtr);

                return ExecPwshLogin(args, loginArgIndex, pwshPath, isMacOS: false);
            }

            int pid = GetPid();

            // Set up the mib array and the query for process args size
            Span<int> mib = stackalloc int[3];
            int mibLength = 2;
            mib[0] = CTL_KERN;
            mib[1] = KERN_ARGMAX;
            int size = sizeof(int);
            int maxargs = 0;

            // Get the process args size
            unsafe
            {
                fixed (int *mibptr = mib)
                {
                    if (SysCtl(mibptr, mibLength, &maxargs, &size, IntPtr.Zero, 0) < 0)
                    {
                        throw new Exception("argmax");
                    }
                }
            }

            // Now read the process args into the allocated space
            IntPtr procargs = Marshal.AllocHGlobal(maxargs);
            IntPtr execPathPtr = IntPtr.Zero;
            try
            {
                size = maxargs;
                mib[0] = CTL_KERN;
                mib[1] = KERN_PROCARGS2;
                mib[2] = pid;
                mibLength = 3;

                unsafe
                {
                    fixed (int *mibptr = mib)
                    {
                        if (SysCtl(mibptr, mibLength, procargs.ToPointer(), &size, IntPtr.Zero, 0) < 0)
                        {
                            throw new Exception("procargs");
                        }
                    }

                    // Skip over argc, remember where exec_path is
                    execPathPtr = IntPtr.Add(procargs, sizeof(int));

                    // Skip over exec_path
                    byte *argvPtr = (byte *)execPathPtr;
                    while (*argvPtr != 0) { argvPtr++; }
                    while (*argvPtr == 0) { argvPtr++; }

                    // First char in argv[0]
                    procNameFirstByte = *argvPtr;
                }

                switch (procNameFirstByte)
                {
                    case 0x2B: // '+' signifies we have already done login check
                        return 0;
                    case 0x2D: // '-' means this is a login shell
                        procStartsWithMinus = true;
                        break;

                    // For any other char, we check for a login parameter
                }

                int loginArgIndex = -1;
                if (!procStartsWithMinus && !IsLogin(args, out loginArgIndex))
                {
                    return 0;
                }

                return ExecPwshLogin(args, loginArgIndex, Marshal.PtrToStringAnsi(execPathPtr), isMacOS: true);
            }
            finally
            {
                Marshal.FreeHGlobal(procargs);
            }
        }

        /// <summary>
        /// Checks args to see if -Login has been specified.
        /// </summary>
        /// <param name="args">Arguments passed to the program.</param>
        /// <param name="loginIndex">The arg index (in argv) where -Login was found.</param>
        /// <returns></returns>
        private static bool IsLogin(string[] args, out int loginIndex)
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
        /// Create the exec call to /bin/{z}sh -l -c 'exec -a +pwsh pwsh "$@"' and run it.
        /// </summary>
        /// <param name="args">The argument vector passed to pwsh.</param>
        /// <param name="loginArgIndex">The index of -Login in the argument vector.</param>
        /// <param name="isMacOS">True if we are running on macOS.</param>
        /// <param name="pwshPath">Absolute path to the pwsh executable.</param>
        /// <returns>
        /// The exit code of exec if it fails.
        /// If exec succeeds, this process is overwritten so we never actually return.
        /// </returns>
        private static int ExecPwshLogin(string[] args, int loginArgIndex, string pwshPath, bool isMacOS)
        {
            // Create input for /bin/sh that execs pwsh
            int quotedPwshPathLength = GetQuotedPathLength(pwshPath);
            string pwshInvocation = string.Create(
                19 + quotedPwshPathLength, // exec +pwsh '{pwshPath}' "$@"
                (pwshPath, quotedPwshPathLength),
                CreatePwshInvocation);

            // Set up the arguments for /bin/sh
            var execArgs = new string[args.Length + 5];

            // execArgs[0] is set below to the correct shell executable

            // The command arguments

            // First argument is the command name.
            // Setting this to /bin/sh enables sh emulation in zsh (which examines $0 to determine how it should behave).
            execArgs[0] = "/bin/sh"; 
            execArgs[1] = "-l"; // Login flag
            execArgs[2] = "-c"; // Command parameter
            execArgs[3] = pwshInvocation; // Command to execute
            execArgs[4] = "-"; // Within the shell, exec ignores $0

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

            // On macOS, sh doesn't support login, so we run /bin/zsh in sh emulation mode
            if (isMacOS)
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
            // "exec -a +pwsh "
            strBuf[0]  = 'e';
            strBuf[1]  = 'x';
            strBuf[2]  = 'e';
            strBuf[3]  = 'c';
            strBuf[4]  = ' ';
            strBuf[5]  = '-';
            strBuf[6]  = 'a';
            strBuf[7]  = ' ';
            strBuf[8]  = '+';
            strBuf[9]  = 'p';
            strBuf[10] = 'w';
            strBuf[11] = 's';
            strBuf[12] = 'h';
            strBuf[13] = ' ';
            
            // The quoted path to pwsh, like "'/opt/microsoft/powershell/7/pwsh'"
            Span<char> pathSpan = strBuf.Slice(14, pwshPath.quotedLength);
            QuoteAndWriteToSpan(pwshPath.path, pathSpan);

            // ' "$@"' the argument vector splat to pass pwsh arguments through
            int argIndex = 14 + pwshPath.quotedLength;
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

        [DllImport("libc",
            EntryPoint = "getpid",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        private static extern int GetPid();

        [DllImport("libc",
            EntryPoint = "sysctl",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        private static unsafe extern int SysCtl(int *mib, int mibLength, void *oldp, int *oldlenp, IntPtr newp, int newlenp);

        [DllImport("libc",
            EntryPoint = "proc_pidpath",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        private static extern int ProcPidPath(int pid, IntPtr buf, int buflen);

        [DllImport("libc",
            EntryPoint="readlink",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        private static extern IntPtr ReadLink(string pathname, IntPtr buf, UIntPtr size);
#endif
    }
}
