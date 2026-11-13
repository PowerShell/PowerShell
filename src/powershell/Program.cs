// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.IO;
using System.Management.Automation;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// Defines an entry point for the .NET CLI "powershell" app.
    /// </summary>
    public sealed class ManagedPSEntry
    {
#if UNIX
        /// <summary>
        /// Exception to signify an early startup failure.
        /// </summary>
        private sealed class StartupException : Exception
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

        // Environment variable used to short circuit second login check
        private const string LOGIN_ENV_VAR_NAME = "__PWSH_LOGIN_CHECKED";
        private const string LOGIN_ENV_VAR_VALUE = "1";

        // Linux p/Invoke constants
        private const int LINUX_PATH_MAX = 4096;

        // MacOS p/Invoke constants
        private const int MACOS_CTL_KERN = 1;
        private const int MACOS_KERN_ARGMAX = 8;
        private const int MACOS_KERN_PROCARGS2 = 49;
        private const int MACOS_PROC_PIDPATHINFO_MAXSIZE = 4096;
#endif

        /// <summary>
        /// Starts PowerShell.
        /// </summary>
        /// <param name="args">
        /// Command line arguments to PowerShell
        /// </param>
        public static int Main(string[] args)
        {
#if UNIX
            AttemptExecPwshLogin(args);
#endif
            return UnmanagedPSEntry.Start(args, args.Length);
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
            // If the login environment variable is set, we have already done the login logic and have been exec'd
            if (Environment.GetEnvironmentVariable(LOGIN_ENV_VAR_NAME) != null)
            {
                Environment.SetEnvironmentVariable(LOGIN_ENV_VAR_NAME, null);
                return;
            }

            bool isLinux = Platform.IsLinux;

            // The first byte (ASCII char) of the name of this process, used to detect '-' for login
            byte procNameFirstByte;

            // The path to the executable this process was started from
            string? pwshPath;

            // On Linux, we can simply use the /proc filesystem
            if (isLinux)
            {
                // Read the process name byte
                using (FileStream fs = File.OpenRead("/proc/self/cmdline"))
                {
                    procNameFirstByte = (byte)fs.ReadByte();
                }

                // Run login detection logic
                if (!IsLogin(procNameFirstByte, args))
                {
                    return;
                }

                // Read the symlink to the startup executable
                IntPtr linkPathPtr = Marshal.AllocHGlobal(LINUX_PATH_MAX);
                IntPtr bufSize = ReadLink("/proc/self/exe", linkPathPtr, (UIntPtr)LINUX_PATH_MAX);
                pwshPath = Marshal.PtrToStringAnsi(linkPathPtr, (int)bufSize);
                Marshal.FreeHGlobal(linkPathPtr);

                ArgumentNullException.ThrowIfNull(pwshPath);

                // exec pwsh
                ThrowOnFailure("exec", ExecPwshLogin(args, pwshPath, isMacOS: false));
                return;
            }

            // At this point, we are on macOS

            // Set up the mib array and the query for process maximum args size
            Span<int> mib = [MACOS_CTL_KERN, MACOS_KERN_ARGMAX];
            int size = IntPtr.Size / 2;
            int argmax = 0;

            // Get the process args size
            unsafe
            {
                fixed (int *mibptr = mib)
                {
                    ThrowOnFailure(nameof(argmax), SysCtl(mibptr, mib.Length, &argmax, &size, IntPtr.Zero, 0));
                }
            }

            // Get the PID so we can query this process' args
            int pid = GetPid();

            // The following logic is based on https://gist.github.com/nonowarn/770696

            // Now read the process args into the allocated space
            IntPtr procargs = Marshal.AllocHGlobal(argmax);
            IntPtr executablePathPtr = IntPtr.Zero;
            try
            {
                mib = new int[] { MACOS_CTL_KERN, MACOS_KERN_PROCARGS2, pid };

                unsafe
                {
                    fixed (int *mibptr = mib)
                    {
                        ThrowOnFailure(nameof(procargs), SysCtl(mibptr, mib.Length, procargs.ToPointer(), &argmax, IntPtr.Zero, 0));
                    }

                    // The memory block we're reading is a series of null-terminated strings
                    // that looks something like this:
                    //
                    // | argc      | <int>
                    // | exec_path | ... \0
                    // | argv[0]   | ... \0
                    // | argv[1]   | ... \0
                    //   ...
                    //
                    // We care about argv[0], since that's the name the process was started with.
                    // If argv[0][0] == '-', we have been invoked as login.
                    // Doing this, the buffer we populated also recorded `exec_path`,
                    // which is the path to our executable `pwsh`.
                    // We can reuse this value later to prevent needing to call a .NET API
                    // to generate our exec invocation.

                    // We don't care about argc's value, since argv[0] must always exist.
                    // Skip over argc, but remember where exec_path is for later
                    executablePathPtr = IntPtr.Add(procargs, sizeof(int));

                    // Skip over exec_path
                    byte *argvPtr = (byte *)executablePathPtr;
                    while (*argvPtr != 0) { argvPtr++; }
                    while (*argvPtr == 0) { argvPtr++; }

                    // First char in argv[0]
                    procNameFirstByte = *argvPtr;
                }

                if (!IsLogin(procNameFirstByte, args))
                {
                    return;
                }

                // Get the pwshPath from exec_path
                pwshPath = Marshal.PtrToStringAnsi(executablePathPtr);

                ArgumentNullException.ThrowIfNull(pwshPath);

                // exec pwsh
                ThrowOnFailure("exec", ExecPwshLogin(args, pwshPath, isMacOS: true));
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
        /// <returns></returns>
        private static bool IsLogin(
            byte procNameFirstByte,
            string[] args)
        {
            // Process name starting with '-' means this is a login shell
            if (procNameFirstByte == 0x2D)
            {
                return true;
            }

            // Look at the first parameter to see if it is -Login
            // NOTE: -Login is only supported as the first parameter to PowerShell
            return args.Length > 0
                && args[0].Length > 1
                && args[0][0] == '-'
                && IsParam(args[0], "login", "LOGIN");
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
                if (arg[i] != paramToCheck[i - 1]
                    && arg[i] != paramToCheckUpper[i - 1])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Create the exec call to /bin/{z}sh -l -c 'exec pwsh "$@"' and run it.
        /// </summary>
        /// <param name="args">The argument vector passed to pwsh.</param>
        /// <param name="isMacOS">True if we are running on macOS.</param>
        /// <param name="pwshPath">Absolute path to the pwsh executable.</param>
        /// <returns>
        /// The exit code of exec if it fails.
        /// If exec succeeds, this process is overwritten so we never actually return.
        /// </returns>
        private static int ExecPwshLogin(string[] args, string pwshPath, bool isMacOS)
        {
            // Create input for /bin/sh that execs pwsh
            int quotedPwshPathLength = GetQuotedPathLength(pwshPath);

            string pwshInvocation = string.Create(
                quotedPwshPathLength + 10, // exec '{pwshPath}' "$@"
                (pwshPath, quotedPwshPathLength),
                CreatePwshInvocation);

            // Set up the arguments for '/bin/sh'.
            // We need to add 5 slots for the '/bin/sh' invocation parts, plus 1 slot for the null terminator at the end
            var execArgs = new string?[args.Length + 6];

            // The command arguments

            // First argument is the command name.
            // Even when executing 'zsh', we want to set this to '/bin/sh'
            // because this tells 'zsh' to run in sh emulation mode (it examines $0)
            execArgs[0] = "/bin/sh";

            execArgs[1] = "-l"; // Login flag
            execArgs[2] = "-c"; // Command parameter
            execArgs[3] = pwshInvocation; // Command to execute

            // The /bin/sh option spec looks like:
            // sh -c command_string [command_name [argument...]]
            // We must provide a command_name before arguments,
            // but this is never used since "$@" takes argv[1] - argv[n]
            // and the `exec` builtin provides its own argv[0].
            // See https://pubs.opengroup.org/onlinepubs/9699919799.2016edition/
            //
            // Since command_name is ignored and we can't use null (it's the terminator)
            // we use empty string
            execArgs[4] = string.Empty;

            // Add the arguments passed to pwsh on the end.
            args.CopyTo(execArgs, 5);

            // A null is required by exec.
            execArgs[execArgs.Length - 1] = null;

            // We can't use Environment.SetEnvironmentVariable() here.
            // See https://github.com/dotnet/corefx/issues/40130#issuecomment-519420648.
            ThrowOnFailure("setenv", SetEnv(LOGIN_ENV_VAR_NAME, LOGIN_ENV_VAR_VALUE, overwrite: true));

            // On macOS, sh doesn't support login, so we run /bin/zsh in sh emulation mode.
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

                if (c == '\'')
                {
                    length++;
                }
            }

            return length;
        }

        /// <summary>
        /// Implements a SpanAction&lt;T&gt; for string.Create()
        /// that builds the shell invocation for the login pwsh session.
        /// </summary>
        /// <param name="strBuf">The buffer of the string to be created.</param>
        /// <param name="invocationInfo">Information used to build the required string.</param>
        private static void CreatePwshInvocation(
            Span<char> strBuf,
            (string path, int quotedLength) invocationInfo)
        {
            // "exec "
            const string prefix = "exec ";
            prefix.AsSpan().CopyTo(strBuf);

            // The quoted path to pwsh, like "'/opt/microsoft/powershell/7/pwsh'"
            int i = prefix.Length;
            Span<char> pathSpan = strBuf.Slice(i, invocationInfo.quotedLength);
            QuoteAndWriteToSpan(invocationInfo.path, pathSpan);
            i += invocationInfo.quotedLength;

            // ' "$@"' the argument vector splat to pass pwsh arguments through
            const string suffix = " \"$@\"";
            Span<char> bufSuffix = strBuf.Slice(i);
            suffix.AsSpan().CopyTo(bufSuffix);
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
        private static void ThrowOnFailure(string call, int code)
        {
            if (code < 0)
            {
                code = Marshal.GetLastWin32Error();
                Console.Error.WriteLine($"Call to '{call}' failed with errno {code}");
                throw new StartupException(call, code);
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

        /// <summary>
        /// The `readlink` POSIX syscall we use to read the symlink from /proc/self/exe
        /// to get the executable path of pwsh on Linux.
        /// </summary>
        /// <param name="pathname">The path to the symlink to read.</param>
        /// <param name="buf">Pointer to a buffer to fill with the result.</param>
        /// <param name="size">The size of the buffer we have supplied.</param>
        /// <returns>The number of bytes placed in the buffer.</returns>
        [DllImport("libc",
            EntryPoint = "readlink",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi,
            SetLastError = true)]
        private static extern IntPtr ReadLink(string pathname, IntPtr buf, UIntPtr size);

        /// <summary>
        /// The `getpid` POSIX syscall we use to quickly get the current process PID on macOS.
        /// </summary>
        /// <returns>The pid of the current process.</returns>
        [DllImport("libc",
            EntryPoint = "getpid",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi,
            SetLastError = true)]
        private static extern int GetPid();

        /// <summary>
        /// The `setenv` POSIX syscall used to set an environment variable in the process.
        /// </summary>
        /// <param name="name">The name of the environment variable.</param>
        /// <param name="value">The value of the environment variable.</param>
        /// <param name="overwrite">If true, will overwrite an existing environment variable of the same name.</param>
        /// <returns>0 if successful, -1 on error. errno indicates the reason for failure.</returns>
        [DllImport("libc",
            EntryPoint = "setenv",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi,
            SetLastError = true)]
        private static extern int SetEnv(string name, string value, bool overwrite);

        /// <summary>
        /// The `sysctl` BSD sycall used to get system information on macOS.
        /// </summary>
        /// <param name="mib">The Management Information Base name, used to query information.</param>
        /// <param name="mibLength">The length of the MIB name.</param>
        /// <param name="oldp">The object passed out of sysctl (may be null)</param>
        /// <param name="oldlenp">The size of the object passed out of sysctl.</param>
        /// <param name="newp">The object passed in to sysctl.</param>
        /// <param name="newlenp">The length of the object passed in to sysctl.</param>
        /// <returns></returns>
        [DllImport("libc",
            EntryPoint = "sysctl",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi,
            SetLastError = true)]
        private static extern unsafe int SysCtl(int *mib, int mibLength, void *oldp, int *oldlenp, IntPtr newp, int newlenp);
#endif
    }
}
