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
        /// <summary>
        /// Exception to signify an early startup failure.
        /// </summary>
        private class StartupException : Exception
        {
            /// <summary>
            /// Construct a new startup exception instance.
            /// </summary>
            /// <param name="callName">The name of the native call that failed.</param>
            /// <param name="exitCode">The exit code the native call returned.</param>
            public StartupException(string callName, int exitCode)
            {
                CallName = callName;
                ExitCode = exitCode;
            }

            /// <summary>
            /// The name of the native call that failed.
            /// </summary>
            public string CallName { get; }

            /// <summary>
            /// The exit code returned by the failed native call.
            /// </summary>
            public int ExitCode { get; }
        }

        // Linux p/Invoke constants
        private const int LINUX_PATH_MAX = 4096;

        // MacOS p/Invoke constants
        private const int MACOS_CTL_KERN = 1;
        private const int MACOS_KERN_ARGMAX = 8;
        private const int MACOS_KERN_PROCARGS2 = 49;
        private const int MACOS_PROC_PIDPATHINFO_MAXSIZE = 4096;

        /// <summary>
        /// Starts the managed MSH.
        /// </summary>
        /// <param name="args">
        /// Command line arguments to the managed MSH
        /// </param>
        public static int Main(string[] args)
        {
#if UNIX
            AttemptExecPwshLogin(args);
#endif
            return UnmanagedPSEntry.Start(string.Empty, args, args.Length);
        }

#if UNIX
        /// <summary>
        /// Checks whether pwsh has been started as a login shell
        /// and if so, proceeds with the login process.
        /// This method will return early if pwsh was not started as a login shell
        /// and will throw if it detects a native call has failed.
        /// In the event of success, we use an exec() call, so this method never returns.
        /// </summary>
        /// <param name="args">The startup arguments to pwsh.</param>
        private static void AttemptExecPwshLogin(string[] args)
        {
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            // The first byte (ASCII char) of the name of this process, used to detect '-' for login
            byte procNameFirstByte;
            // The path to the executable this process was started from
            string pwshPath;

            // On Linux, we can simply use the /proc filesystem
            if (isLinux)
            {
                // Read the process name byte
                using (FileStream fs = File.OpenRead("/proc/self/cmdline"))
                {
                    procNameFirstByte = (byte)fs.ReadByte();
                }

                // Run login detection logic
                if (!IsLogin(procNameFirstByte, args, out int loginArgIndex))
                {
                    return;
                }

                // Read the symlink to the startup executable
                IntPtr linkPathPtr = Marshal.AllocHGlobal(LINUX_PATH_MAX);
                IntPtr size = ReadLink("/proc/self/exe", linkPathPtr, (UIntPtr)LINUX_PATH_MAX);
                pwshPath = Marshal.PtrToStringAnsi(linkPathPtr, (int)size);
                Marshal.FreeHGlobal(linkPathPtr);

                // exec pwsh
                ThrowIfFails("exec", ExecPwshLogin(args, loginArgIndex, pwshPath, isMacOS: false));
                return;
            }

            // At this point, we are on macOS

            // Set up the mib array and the query for process maximum args size
            Span<int> mib = stackalloc int[3];
            int mibLength = 2;
            mib[0] = MACOS_CTL_KERN;
            mib[1] = MACOS_KERN_ARGMAX;
            int size = IntPtr.Size / 2;
            int argmax = 0;

            // Get the process args size
            unsafe
            {
                fixed (int *mibptr = mib)
                {
                    ThrowIfFails(nameof(argmax), SysCtl(mibptr, mibLength, &argmax, &size, IntPtr.Zero, 0));
                }
            }

            // Get the PID so we can query this process' args
            int pid = GetPid();

            // Now read the process args into the allocated space
            IntPtr procargs = Marshal.AllocHGlobal(argmax);
            IntPtr execPathPtr = IntPtr.Zero;
            try
            {
                mib[0] = MACOS_CTL_KERN;
                mib[1] = MACOS_KERN_PROCARGS2;
                mib[2] = pid;
                mibLength = 3;

                unsafe
                {
                    fixed (int *mibptr = mib)
                    {
                        ThrowIfFails(nameof(procargs), SysCtl(mibptr, mibLength, procargs.ToPointer(), &argmax, IntPtr.Zero, 0));
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

                if (!IsLogin(procNameFirstByte, args, out int loginArgIndex))
                {
                    return;
                }

                // Get the pwshPath from exec_path
                pwshPath = Marshal.PtrToStringAnsi(execPathPtr);

                // exec pwsh
                ThrowIfFails("exec", ExecPwshLogin(args, loginArgIndex, pwshPath, isMacOS: true));
            }
            finally
            {
                Marshal.FreeHGlobal(procargs);
            }
        }

        /// <summary>
        /// Checks args to see if -Login has been specified.
        /// </summary>
        /// <param name="procNameFirstByte">The first byte of the name of the currently running process.</param>
        /// <param name="args">Arguments passed to the program.</param>
        /// <param name="loginIndex">The arg index (in argv) where -Login was found.</param>
        /// <returns></returns>
        private static bool IsLogin(
            byte procNameFirstByte,
            string[] args,
            out int loginIndex)
        {
            loginIndex = -1;

            switch (procNameFirstByte)
            {
                // '+' signifies we have already done login check
                case 0x2B:
                    return false;

                // '-' means this is a login shell
                case 0x2D:
                    return true;

                // For any other char, we check for a login parameter
            }

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

            // /bin/sh does not support the exec -a feature
            int pwshExecInvocationLength = isMacOS
                ? quotedPwshPathLength + 19  // exec -a +pwsh '{pwshPath}' "$@"
                : quotedPwshPathLength + 10; // exec '{pwshPath}' "$@"

            string pwshInvocation = string.Create(
                pwshExecInvocationLength, 
                (pwshPath, quotedPwshPathLength, isMacOS),
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
            execArgs[4] = ""; // Within the shell, exec ignores $0

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

        /// <summary>
        /// Implements a SpanAction&lt;T&gt; for string.Create()
        /// that builds the shell invocation for the login pwsh session.
        /// </summary>
        /// <param name="strBuf">The buffer of the string to be created.</param>
        /// <param name="path">The unquoted pwsh path.</param>
        /// <param name="quotedLength">The length the pwsh path will have when it's quoted.</param>
        /// <param name="supportsDashA">Indicates whether the `exec` builtin supports "-a" to change the process name.</param>
        private static void CreatePwshInvocation(
            Span<char> strBuf,
            (string path, int quotedLength, bool supportsDashA) invocationInfo)
        {
            // "exec "
            int i = 0;
            strBuf[i++]  = 'e';
            strBuf[i++]  = 'x';
            strBuf[i++]  = 'e';
            strBuf[i++]  = 'c';
            strBuf[i++]  = ' ';

            if (invocationInfo.supportsDashA)
            {
                // "-a +pwsh "
                // We use this where -a is supported to prevent a second login check
                strBuf[i++] = '-';
                strBuf[i++] = 'a';
                strBuf[i++] = ' ';
                strBuf[i++] = '+';
                strBuf[i++] = 'p';
                strBuf[i++] = 'w';
                strBuf[i++] = 's';
                strBuf[i++] = 'h';
                strBuf[i++] = ' ';
            }
            
            // The quoted path to pwsh, like "'/opt/microsoft/powershell/7/pwsh'"
            Span<char> pathSpan = strBuf.Slice(i, invocationInfo.quotedLength);
            QuoteAndWriteToSpan(invocationInfo.path, pathSpan);
            i += invocationInfo.quotedLength

            // ' "$@"' the argument vector splat to pass pwsh arguments through
            strBuf[i++] = ' ';
            strBuf[i++] = '"';
            strBuf[i++] = '$';
            strBuf[i++] = '@';
            strBuf[i++] = '"';
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
        /// If the given exit code is negative, throws a StartupException.
        /// </summary>
        /// <param name="call">The native call that was attempted.</param>
        /// <param name="code">The exit code it returned.</param>
        private static void ThrowIfFails(string call, int code)
        {
            if (code < 0)
            {
                throw new StartupException(call, code);
            }
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

        /// <summary>
        /// The `readlink` syscall we use to read the symlink from /proc/self/exe
        /// to get the executable path of pwsh on Linux.
        /// </summary>
        /// <param name="pathname">The path to the symlink to read.</param>
        /// <param name="buf">Pointer to a buffer to fill with the result.</param>
        /// <param name="size">The size of the buffer we have supplied.</param>
        /// <returns>The number of bytes placed in the buffer.</returns>
        [DllImport("libc",
            EntryPoint="readlink",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        private static extern IntPtr ReadLink(string pathname, IntPtr buf, UIntPtr size);

        /// <summary>
        /// The `getpid` POSIX syscall we use to quickly get the current process PID on macOS.
        /// </summary>
        /// <returns>The pid of the current process.</returns>
        [DllImport("libc",
            EntryPoint = "getpid",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        private static extern int GetPid();

        /// <summary>
        /// The `sysctl` BSD sycall used to get system information on macOS.
        /// </summary>
        /// <param name="*mib">The Management Information Base name, used to query information.</param>
        /// <param name="mibLength">The length of the MIB name.</param>
        /// <param name="*oldp">The object passed out of sysctl (may be null)</param>
        /// <param name="*oldlenp">The size of the object passed out of sysctl.</param>
        /// <param name="newp">The object passed in to sysctl.</param>
        /// <param name="newlenp">The length of the object passed in to sysctl.</param>
        /// <returns></returns>
        [DllImport("libc",
            EntryPoint = "sysctl",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        private static unsafe extern int SysCtl(int *mib, int mibLength, void *oldp, int *oldlenp, IntPtr newp, int newlenp);
#endif
    }
}
