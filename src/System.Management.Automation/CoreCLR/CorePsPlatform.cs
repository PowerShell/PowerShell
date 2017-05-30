/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Diagnostics;
using System.Text;

namespace System.Management.Automation
{
    /// <summary>
    /// These are platform abstractions and platform specific implementations
    /// </summary>
    public static class Platform
    {
        private static string _tempDirectory = null;

        /// <summary>
        /// True if the current platform is Linux.
        /// </summary>
        public static bool IsLinux
        {
            get
            {
#if CORECLR
                return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// True if the current platform is OS X.
        /// </summary>
        public static bool IsOSX
        {
            get
            {
#if CORECLR
                return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// True if the current platform is Windows.
        /// </summary>
        public static bool IsWindows
        {
            get
            {
#if CORECLR
                return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
                return true;
#endif
            }
        }

        /// <summary>
        /// True if PowerShell was built targeting .NET Core.
        /// </summary>
        public static bool IsCoreCLR
        {
            get
            {
#if CORECLR
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// True if the underlying system is NanoServer.
        /// </summary>
        public static bool IsNanoServer
        {
            get
            {
#if !CORECLR
                return false;
#elif UNIX
                return false;
#else
                if (_isNanoServer.HasValue) { return _isNanoServer.Value; }

                _isNanoServer = false;
                using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Server\ServerLevels"))
                {
                    if (regKey != null)
                    {
                        object value = regKey.GetValue("NanoServer");
                        if (value != null && regKey.GetValueKind("NanoServer") == RegistryValueKind.DWord)
                        {
                            _isNanoServer = (int)value == 1;
                        }
                    }
                }

                return _isNanoServer.Value;
#endif
            }
        }

        /// <summary>
        /// True if the underlying system is IoT.
        /// </summary>
        public static bool IsIoT
        {
            get
            {
#if !CORECLR
                return false;
#elif UNIX
                return false;
#else
                if (_isIoT.HasValue) { return _isIoT.Value; }

                _isIoT = false;
                using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (regKey != null)
                    {
                        object value = regKey.GetValue("ProductName");
                        if (value != null && regKey.GetValueKind("ProductName") == RegistryValueKind.String)
                        {
                            _isIoT = string.Equals("IoTUAP", (string)value, StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }

                return _isIoT.Value;
#endif
            }
        }

#if CORECLR
        /// <summary>
        /// True if it is the inbox powershell for NanoServer or IoT.
        /// </summary>
        internal static bool IsInbox
        {
            get
            {
#if UNIX
                return false;
#else
                if (_isInbox.HasValue) { return _isInbox.Value; }

                _isInbox = false;
                if (IsNanoServer || IsIoT)
                {
                    _isInbox = string.Equals(
                        Utils.GetApplicationBase(Utils.DefaultPowerShellShellID),
                        Utils.GetApplicationBaseFromRegistry(Utils.DefaultPowerShellShellID),
                        StringComparison.OrdinalIgnoreCase);
                }

                return _isInbox.Value;
#endif
            }
        }

#if !UNIX
        private static bool? _isNanoServer = null;
        private static bool? _isIoT = null;
        private static bool? _isInbox = null;
#endif

#endif

        // format files
        internal static List<string> FormatFileNames = new List<string>
            {
                "Certificate.format.ps1xml",
                "Diagnostics.format.ps1xml",
                "DotNetTypes.format.ps1xml",
                "Event.format.ps1xml",
                "FileSystem.format.ps1xml",
                "Help.format.ps1xml",
                "HelpV3.format.ps1xml",
                "PowerShellCore.format.ps1xml",
                "PowerShellTrace.format.ps1xml",
                "Registry.format.ps1xml",
                "WSMan.format.ps1xml"
            };

        /// <summary>
        /// Some common environment variables used in PS have different
        /// names in different OS platforms
        /// </summary>
        internal static class CommonEnvVariableNames
        {
#if UNIX
            internal const string Home = "HOME";
#else
            internal const string Home = "USERPROFILE";
#endif
        }

        /// <summary>
        /// Remove the temporary directory created for the current process
        /// </summary>
        internal static void RemoveTemporaryDirectory()
        {
            if (null == _tempDirectory)
            {
                return;
            }

            try
            {
                Directory.Delete(_tempDirectory, true);
            }
            catch
            {
                // ignore if there is a failure
            }
            _tempDirectory = null;
        }

        /// <summary>
        /// Get a temporary directory to use for the current process
        /// </summary>
        internal static string GetTemporaryDirectory()
        {
            if (null != _tempDirectory)
            {
                return _tempDirectory;
            }

            _tempDirectory = PsUtils.GetTemporaryDirectory();
            return _tempDirectory;
        }

#if UNIX
        /// <summary>
        /// X Desktop Group configuration type enum.
        /// </summary>
        public enum XDG_Type
        {
            /// <summary> XDG_CONFIG_HOME/powershell </summary>
            CONFIG,
            /// <summary> XDG_CACHE_HOME/powershell </summary>
            CACHE,
            /// <summary> XDG_DATA_HOME/powershell </summary>
            DATA,
            /// <summary> XDG_DATA_HOME/powershell/Modules </summary>
            USER_MODULES,
            /// <summary> /usr/local/share/powershell/Modules </summary>
            SHARED_MODULES,
            /// <summary> XDG_CONFIG_HOME/powershell </summary>
            DEFAULT
        }

        /// <summary>
        /// function for choosing directory location of PowerShell for profile loading
        /// </summary>
        public static string SelectProductNameForDirectory(Platform.XDG_Type dirpath)
        {
            //TODO: XDG_DATA_DIRS implementation as per GitHub issue #1060

            string xdgconfighome = System.Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            string xdgdatahome = System.Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            string xdgcachehome = System.Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
            string envHome = System.Environment.GetEnvironmentVariable(CommonEnvVariableNames.Home);
            if (null == envHome)
            {
                envHome = GetTemporaryDirectory();
            }
            string xdgConfigHomeDefault = Path.Combine(envHome, ".config", "powershell");
            string xdgDataHomeDefault = Path.Combine(envHome, ".local", "share", "powershell");
            string xdgModuleDefault = Path.Combine(xdgDataHomeDefault, "Modules");
            string xdgCacheDefault = Path.Combine(envHome, ".cache", "powershell");

            switch (dirpath)
            {
                case Platform.XDG_Type.CONFIG:
                    //the user has set XDG_CONFIG_HOME corresponding to profile path
                    if (String.IsNullOrEmpty(xdgconfighome))
                    {
                        //xdg values have not been set
                        return xdgConfigHomeDefault;
                    }

                    else
                    {
                        return Path.Combine(xdgconfighome, "powershell");
                    }

                case Platform.XDG_Type.DATA:
                    //the user has set XDG_DATA_HOME corresponding to module path
                    if (String.IsNullOrEmpty(xdgdatahome))
                    {
                        // create the xdg folder if needed
                        if (!Directory.Exists(xdgDataHomeDefault))
                        {
                            try
                            {
                                Directory.CreateDirectory(xdgDataHomeDefault);
                            }
                            catch (UnauthorizedAccessException)
                            {
                                //service accounts won't have permission to create user folder
                                return GetTemporaryDirectory();
                            }
                        }
                        return xdgDataHomeDefault;
                    }
                    else
                    {
                        return Path.Combine(xdgdatahome, "powershell");
                    }

                case Platform.XDG_Type.USER_MODULES:
                    //the user has set XDG_DATA_HOME corresponding to module path
                    if (String.IsNullOrEmpty(xdgdatahome))
                    {
                        //xdg values have not been set
                        if (!Directory.Exists(xdgModuleDefault)) //module folder not always guaranteed to exist
                        {
                            try
                            {
                                Directory.CreateDirectory(xdgModuleDefault);
                            }
                            catch (UnauthorizedAccessException)
                            {
                                //service accounts won't have permission to create user folder
                                return GetTemporaryDirectory();
                            }
                        }
                        return xdgModuleDefault;
                    }
                    else
                    {
                        return Path.Combine(xdgdatahome, "powershell", "Modules");
                    }

                case Platform.XDG_Type.SHARED_MODULES:
                    return "/usr/local/share/powershell/Modules";

                case Platform.XDG_Type.CACHE:
                    //the user has set XDG_CACHE_HOME
                    if (String.IsNullOrEmpty(xdgcachehome))
                    {
                        //xdg values have not been set
                        if (!Directory.Exists(xdgCacheDefault)) //module folder not always guaranteed to exist
                        {
                            try
                            {
                                Directory.CreateDirectory(xdgCacheDefault);
                            }
                            catch (UnauthorizedAccessException)
                            {
                                //service accounts won't have permission to create user folder
                                return GetTemporaryDirectory();
                            }
                        }

                        return xdgCacheDefault;
                    }

                    else
                    {
                        if (!Directory.Exists(Path.Combine(xdgcachehome, "powershell")))
                        {
                            try
                            {
                                Directory.CreateDirectory(Path.Combine(xdgcachehome, "powershell"));
                            }
                            catch (UnauthorizedAccessException)
                            {
                                //service accounts won't have permission to create user folder
                                return GetTemporaryDirectory();
                            }                                
                        }

                        return Path.Combine(xdgcachehome, "powershell");
                    }

                case Platform.XDG_Type.DEFAULT:
                    //default for profile location
                    return xdgConfigHomeDefault;

                default:
                    //xdgConfigHomeDefault needs to be created in the edge case that we do not have the folder or it was deleted
                    //This folder is the default in the event of all other failures for data storage
                    if (!Directory.Exists(xdgConfigHomeDefault))
                    {
                        try
                        {
                            Directory.CreateDirectory(xdgConfigHomeDefault);
                        }
                        catch
                        {
                            Console.Error.WriteLine("Failed to create default data directory: " + xdgConfigHomeDefault);
                        }
                    }

                    return xdgConfigHomeDefault;
            }
        }
#endif

        /// <summary>
        /// The code is copied from the .NET implementation.
        /// </summary>
        internal static string GetFolderPath(System.Environment.SpecialFolder folder)
        {
            return InternalGetFolderPath(folder);
        }

        /// <summary>
        /// The API set 'api-ms-win-shell-shellfolders-l1-1-0.dll' was removed from NanoServer, so we cannot depend on 'SHGetFolderPathW'
        /// to get the special folder paths. Instead, we need to rely on the basic environment variables to get the special folder paths.
        /// </summary>
        /// <returns>
        /// The path to the specified system special folder, if that folder physically exists on your computer.
        /// Otherwise, an empty string ("").
        /// </returns>
        private static string InternalGetFolderPath(System.Environment.SpecialFolder folder)
        {
            string folderPath = null;
#if UNIX
            string envHome = System.Environment.GetEnvironmentVariable(Platform.CommonEnvVariableNames.Home);
            if (null == envHome)
            {
                envHome = Platform.GetTemporaryDirectory();
            }
            switch (folder)
            {
                case System.Environment.SpecialFolder.ProgramFiles:
                    folderPath = "/bin";
                    if (!System.IO.Directory.Exists(folderPath)) { folderPath = null; }
                    break;
                case System.Environment.SpecialFolder.ProgramFilesX86:
                    folderPath = "/usr/bin";
                    if (!System.IO.Directory.Exists(folderPath)) { folderPath = null; }
                    break;
                case System.Environment.SpecialFolder.System:
                case System.Environment.SpecialFolder.SystemX86:
                    folderPath = "/sbin";
                    if (!System.IO.Directory.Exists(folderPath)) { folderPath = null; }
                    break;
                case System.Environment.SpecialFolder.Personal:
                    folderPath = envHome; 
                    break;
                case System.Environment.SpecialFolder.LocalApplicationData:
                    folderPath = System.IO.Path.Combine(envHome, ".config");
                    if (!System.IO.Directory.Exists(folderPath)) 
                    {
                        try
                        {
                            System.IO.Directory.CreateDirectory(folderPath); 
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // directory creation may fail if the account doesn't have filesystem permission such as some service accounts
                            folderPath = String.Empty;
                        }
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }
#else
            folderPath = System.Environment.GetFolderPath(folder);
#endif
            return folderPath ?? string.Empty;
        }

        // Platform methods prefixed NonWindows are:
        // - non-windows by the definition of the IsWindows method above
        // - here, because porting to Linux and other operating systems
        //   should not move the original Windows code out of the module
        //   it belongs to, so this way the windows code can remain in it's
        //   original source file and only the non-windows code has been moved
        //   out here
        // - only to be used with the IsWindows feature query, and only if
        //   no other more specific feature query makes sense

        internal static bool NonWindowsIsHardLink(ref IntPtr handle)
        {
            return Unix.IsHardLink(ref handle);
        }

        internal static bool NonWindowsIsHardLink(FileSystemInfo fileInfo)
        {
            return Unix.IsHardLink(fileInfo);
        }

        internal static bool NonWindowsIsSymLink(FileSystemInfo fileInfo)
        {
            return Unix.NativeMethods.IsSymLink(fileInfo.FullName);
        }

        internal static string NonWindowsInternalGetTarget(SafeFileHandle handle)
        {
            // SafeHandle is a Windows concept.  Use the string version instead.
            throw new PlatformNotSupportedException();
        }

        internal static string NonWindowsInternalGetTarget(string path)
        {
            return Unix.NativeMethods.FollowSymLink(path);
        }

        internal static string NonWindowsGetUserFromPid(int path)
        {
            return Unix.NativeMethods.GetUserFromPid(path);
        }

        internal static string NonWindowsInternalGetLinkType(FileSystemInfo fileInfo)
        {
            if (NonWindowsIsSymLink(fileInfo))
            {
                return "SymbolicLink";
            }

            if (NonWindowsIsHardLink(fileInfo))
            {
                return "HardLink";
            }

            return null;
        }

        internal static bool NonWindowsCreateSymbolicLink(string path, string target)
        {
            // Linux doesn't care if target is a directory or not
            return Unix.NativeMethods.CreateSymLink(path, target) == 0;
        }

        internal static bool NonWindowsCreateHardLink(string path, string strTargetPath)
        {
            return Unix.NativeMethods.CreateHardLink(path, strTargetPath) == 0;
        }

        internal static unsafe bool NonWindowsSetDate(DateTime dateToUse)
        {
            Unix.NativeMethods.UnixTm tm = Unix.NativeMethods.DateTimeToUnixTm(dateToUse);
            return Unix.NativeMethods.SetDate(&tm) == 0;
        }

        internal static string NonWindowsGetDomainName()
        {
            string name = Unix.NativeMethods.GetFullyQualifiedName();
            if (!string.IsNullOrEmpty(name))
            {
                // name is hostname.domainname, so extract domainname
                int index = name.IndexOf('.');
                if (index >= 0)
                {
                    return name.Substring(index + 1);
                }
            }
            // if the domain name could not be found, do not throw, just return empty
            return string.Empty;
        }

        // Hostname in this context seems to be the FQDN
        internal static string NonWindowsGetHostName()
        {
            return Unix.NativeMethods.GetFullyQualifiedName() ?? string.Empty;
        }

        internal static bool NonWindowsIsFile(string path)
        {
            return Unix.NativeMethods.IsFile(path);
        }

        internal static bool NonWindowsIsDirectory(string path)
        {
            return Unix.NativeMethods.IsDirectory(path);
        }

        internal static bool NonWindowsIsSameFileSystemItem(string pathOne, string pathTwo)
        {
            return Unix.NativeMethods.IsSameFileSystemItem(pathOne, pathTwo);
        }

        internal static bool NonWindowsIsExecutable(string path)
        {
            return Unix.NativeMethods.IsExecutable(path);
        }

        internal static uint NonWindowsGetThreadId()
        {
            return Unix.NativeMethods.GetCurrentThreadId();
        }

        internal static int NonWindowsGetProcessParentPid(int pid)
        {
            return IsOSX ? Unix.NativeMethods.GetPPid(pid) : Unix.GetProcFSParentPid(pid);
        }

        #region UNIX CreateProcess

        //
        // This code was taken from GitHub DotNet CoreFx
        //

#if UNIX
        private static readonly UTF8Encoding s_utf8NoBom =
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private const int StreamBufferSize = 4096;

        internal static int StartProcess(
            ProcessStartInfo startInfo,
            int creationFlags,
            ref StreamWriter standardInput,
            ref StreamReader standardOutput,
            ref StreamReader standardError)
        {
            string filename;
            string[] argv;

            if (startInfo.UseShellExecute)
            {
                if (startInfo.RedirectStandardInput || startInfo.RedirectStandardOutput || startInfo.RedirectStandardError)
                {
                    throw new InvalidOperationException(RemotingErrorIdStrings.CantRedirectStreams);
                }

                const string ShellPath = "/bin/sh";

                filename = ShellPath;
                argv = new string[3] { ShellPath, "-c", startInfo.FileName + " " + startInfo.Arguments };
            }
            else
            {
                // We currently require a fully qualified path and don't do any path resolution.
                filename = startInfo.FileName;
                argv = ParseArgv(startInfo);
            }

            string[] envp = CreateEnvp(startInfo);
            string cwd = !string.IsNullOrWhiteSpace(startInfo.WorkingDirectory) ? startInfo.WorkingDirectory : null;

            // Invoke the shim fork/execve routine.  It will create pipes for all requested
            // redirects, fork a child process, map the pipe ends onto the appropriate stdin/stdout/stderr
            // descriptors, and execve to execute the requested process.  The shim implementation
            // is used to fork/execve as executing managed code in a forked process is not safe (only
            // the calling thread will transfer, thread IDs aren't stable across the fork, etc.)
            int childPid, stdinFd, stdoutFd, stderrFd;
            CreateProcess(
                filename, argv, envp, cwd,
                startInfo.RedirectStandardInput, startInfo.RedirectStandardOutput, startInfo.RedirectStandardError, creationFlags,
                out childPid,
                out stdinFd, out stdoutFd, out stderrFd);

            Debug.Assert(childPid >= 0);

            // Configure the parent's ends of the redirection streams.
            // We use UTF8 encoding without BOM by-default(instead of Console encoding as on Windows)
            // as there is no good way to get this information from the native layer
            // and we do not want to take dependency on Console contract.
            if (startInfo.RedirectStandardInput)
            {
                Debug.Assert(stdinFd >= 0);
                standardInput = new StreamWriter(OpenStream(stdinFd, FileAccess.Write),
                    s_utf8NoBom, StreamBufferSize)
                { AutoFlush = true };
            }
            if (startInfo.RedirectStandardOutput)
            {
                Debug.Assert(stdoutFd >= 0);
                standardOutput = new StreamReader(OpenStream(stdoutFd, FileAccess.Read),
                    startInfo.StandardOutputEncoding ?? s_utf8NoBom, true, StreamBufferSize);
            }
            if (startInfo.RedirectStandardError)
            {
                Debug.Assert(stderrFd >= 0);
                standardError = new StreamReader(OpenStream(stderrFd, FileAccess.Read),
                    startInfo.StandardErrorEncoding ?? s_utf8NoBom, true, StreamBufferSize);
            }

            return childPid;
        }

        /// <summary>Opens a stream around the specified file descriptor and with the specified access.</summary>
        /// <param name="fd">The file descriptor.</param>
        /// <param name="access">The access mode.</param>
        /// <returns>The opened stream.</returns>
        private static FileStream OpenStream(int fd, FileAccess access)
        {
            Debug.Assert(fd >= 0);
            return new FileStream(
                new SafeFileHandle((IntPtr)fd, ownsHandle: true),
                access, StreamBufferSize, isAsync: false);
        }

        /// <summary>Converts the filename and arguments information from a ProcessStartInfo into an argv array.</summary>
        /// <param name="psi">The ProcessStartInfo.</param>
        /// <returns>The argv array.</returns>
        private static string[] ParseArgv(ProcessStartInfo psi)
        {
            string argv0 = psi.FileName; // pass filename (instead of resolved path) as argv[0], to match what caller supplied
            if (string.IsNullOrEmpty(psi.Arguments))
            {
                return new string[] { argv0 };
            }
            else
            {
                var argvList = new List<string>();
                argvList.Add(argv0);
                ParseArgumentsIntoList(psi.Arguments, argvList);
                return argvList.ToArray();
            }
        }

        /// <summary>Converts the environment variables information from a ProcessStartInfo into an envp array.</summary>
        /// <param name="psi">The ProcessStartInfo.</param>
        /// <returns>The envp array.</returns>
        private static string[] CreateEnvp(ProcessStartInfo psi)
        {
            var envp = new string[psi.Environment.Count];
            int index = 0;
            foreach (var pair in psi.Environment)
            {
                envp[index++] = pair.Key + "=" + pair.Value;
            }
            return envp;
        }

        /// <summary>Parses a command-line argument string into a list of arguments.</summary>
        /// <param name="arguments">The argument string.</param>
        /// <param name="results">The list into which the component arguments should be stored.</param>
        /// <remarks>
        /// This follows the rules outlined in "Parsing C++ Command-Line Arguments" at 
        /// https://msdn.microsoft.com/en-us/library/17w5ykft.aspx.
        /// </remarks>
        private static void ParseArgumentsIntoList(string arguments, List<string> results)
        {
            var currentArgument = new StringBuilder();
            bool inQuotes = false;

            // Iterate through all of the characters in the argument string.
            for (int i = 0; i < arguments.Length; i++)
            {
                // From the current position, iterate through contiguous backslashes.
                int backslashCount = 0;
                for (; i < arguments.Length && arguments[i] == '\\'; i++, backslashCount++) ;
                if (backslashCount > 0)
                {
                    if (i >= arguments.Length || arguments[i] != '"')
                    {
                        // Backslashes not followed by a double quote:
                        // they should all be treated as literal backslashes.
                        currentArgument.Append('\\', backslashCount);
                        i--;
                    }
                    else
                    {
                        // Backslashes followed by a double quote:
                        // - Output a literal slash for each complete pair of slashes
                        // - If one remains, use it to make the subsequent quote a literal.
                        currentArgument.Append('\\', backslashCount / 2);
                        if (backslashCount % 2 == 0)
                        {
                            i--;
                        }
                        else
                        {
                            currentArgument.Append('"');
                        }
                    }
                    continue;
                }

                char c = arguments[i];

                // If this is a double quote, track whether we're inside of quotes or not.
                // Anything within quotes will be treated as a single argument, even if
                // it contains spaces.
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                // If this is a space/tab and we're not in quotes, we're done with the current
                // argument, and if we've built up any characters in the current argument,
                // it should be added to the results and then reset for the next one.
                if ((c == ' ' || c == '\t') && !inQuotes)
                {
                    if (currentArgument.Length > 0)
                    {
                        results.Add(currentArgument.ToString());
                        currentArgument.Clear();
                    }
                    continue;
                }

                // Nothing special; add the character to the current argument.
                currentArgument.Append(c);
            }

            // If we reach the end of the string and we still have anything in our current
            // argument buffer, treat it as an argument to be added to the results.
            if (currentArgument.Length > 0)
            {
                results.Add(currentArgument.ToString());
            }
        }

        internal static unsafe void CreateProcess(
            string filename, string[] argv, string[] envp, string cwd,
            bool redirectStdin, bool redirectStdout, bool redirectStderr, int creationFlags,
            out int lpChildPid, out int stdinFd, out int stdoutFd, out int stderrFd)
        {
            byte** argvPtr = null, envpPtr = null;
            try
            {
                AllocNullTerminatedArray(argv, ref argvPtr);
                AllocNullTerminatedArray(envp, ref envpPtr);
                int result = Unix.NativeMethods.ForkAndExecProcess(
                    filename, argvPtr, envpPtr, cwd,
                    redirectStdin ? 1 : 0, redirectStdout ? 1 : 0, redirectStderr ? 1 : 0, creationFlags,
                    out lpChildPid, out stdinFd, out stdoutFd, out stderrFd);
                if (result != 0)
                {
                    // Normally we'd simply make this method return the result of the native
                    // call and allow the caller to use GetLastWin32Error.  However, we need
                    // to free the native arrays after calling the function, and doing so
                    // stomps on the runtime's captured last error.  So we need to access the
                    // error here, and without SetLastWin32Error available, we can't propagate
                    // the error to the caller via the normal GetLastWin32Error mechanism.  We could
                    // return 0 on success or the GetLastWin32Error value on failure, but that's
                    // technically ambiguous, in the case of a failure with a 0 errno.  Simplest
                    // solution then is just to throw here the same exception the Process caller
                    // would have.  This can be revisited if we ever have another call site.
                    throw new Win32Exception();
                }
            }
            finally
            {
                FreeArray(envpPtr, envp.Length);
                FreeArray(argvPtr, argv.Length);
            }
        }

        private static unsafe void AllocNullTerminatedArray(string[] arr, ref byte** arrPtr)
        {
            int arrLength = arr.Length + 1; // +1 is for null termination

            // Allocate the unmanaged array to hold each string pointer.
            // It needs to have an extra element to null terminate the array.
            arrPtr = (byte**)Marshal.AllocHGlobal(sizeof(IntPtr) * arrLength);
            System.Diagnostics.Debug.Assert(arrPtr != null);

            // Zero the memory so that if any of the individual string allocations fails,
            // we can loop through the array to free any that succeeded.
            // The last element will remain null.
            for (int i = 0; i < arrLength; i++)
            {
                arrPtr[i] = null;
            }

            // Now copy each string to unmanaged memory referenced from the array.
            // We need the data to be an unmanaged, null-terminated array of UTF8-encoded bytes.
            for (int i = 0; i < arr.Length; i++)
            {
                byte[] byteArr = System.Text.Encoding.UTF8.GetBytes(arr[i]);

                arrPtr[i] = (byte*)Marshal.AllocHGlobal(byteArr.Length + 1); //+1 for null termination
                System.Diagnostics.Debug.Assert(arrPtr[i] != null);

                Marshal.Copy(byteArr, 0, (IntPtr)arrPtr[i], byteArr.Length); // copy over the data from the managed byte array
                arrPtr[i][byteArr.Length] = (byte)'\0'; // null terminate
            }
        }

        private static unsafe void FreeArray(byte** arr, int length)
        {
            if (arr != null)
            {
                // Free each element of the array
                for (int i = 0; i < length; i++)
                {
                    if (arr[i] != null)
                    {
                        Marshal.FreeHGlobal((IntPtr)arr[i]);
                        arr[i] = null;
                    }
                }

                // And then the array itself
                Marshal.FreeHGlobal((IntPtr)arr);
            }
        }
#endif

        #endregion

        // Unix specific implementations of required functionality
        //
        // Please note that `Win32Exception(Marshal.GetLastWin32Error())`
        // works *correctly* on Linux in that it creates an exception with
        // the string perror would give you for the last set value of errno.
        // No manual mapping is required. .NET Core maps the Linux errno
        // to a PAL value and calls strerror_r underneath to generate the message.
        internal static class Unix
        {
            // This is a helper that attempts to map errno into a PowerShell ErrorCategory
            internal static ErrorCategory GetErrorCategory(int errno)
            {
                return (ErrorCategory)Unix.NativeMethods.GetErrorCategory(errno);
            }

            private static string s_userName;
            public static string UserName
            {
                get
                {
                    if (string.IsNullOrEmpty(s_userName))
                    {
                        s_userName = NativeMethods.GetUserName();
                    }
                    return s_userName ?? string.Empty;
                }
            }

            public static string TemporaryDirectory
            {
                get
                {
                    // POSIX temporary directory environment variables
                    string[] environmentVariables = { "TMPDIR", "TMP", "TEMP", "TEMPDIR" };
                    string dir = string.Empty;
                    foreach (string s in environmentVariables)
                    {
                        dir = System.Environment.GetEnvironmentVariable(s);
                        if (!string.IsNullOrEmpty(dir))
                        {
                            return dir;
                        }
                    }
                    return "/tmp";
                }
            }

            public static bool IsHardLink(ref IntPtr handle)
            {
                // TODO:PSL implement using fstat to query inode refcount to see if it is a hard link
                return false;
            }


            public static bool IsHardLink(FileSystemInfo fs)
            {
                if (!fs.Exists || (fs.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    return false;
                }

                int count;
                string filePath = fs.FullName;
                int ret = NativeMethods.GetLinkCount(filePath, out count);
                if (ret == 0)
                {
                    return count > 1;
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }

            public static int GetProcFSParentPid(int pid)
            {
                const int invalidPid = -1;
                // read /proc/<pid>/stat
                // 4th column will contain the ppid, 92 in the example below
                // ex: 93 (bash) S 92 93 2 4294967295 ...

                var path = $"/proc/{pid}/stat";
                try
                {
                    var stat = System.IO.File.ReadAllText(path);
                    var parts = stat.Split(new[] { ' ' }, 5);
                    if (parts.Length < 5)
                    {
                        return invalidPid;
                    }
                    return Int32.Parse(parts[3]);
                }
                catch (Exception)
                {
                    return invalidPid;
                }
            }

            internal static class NativeMethods
            {
                private const string psLib = "libpsl-native";

                // Ansi is a misnomer, it is hardcoded to UTF-8 on Linux and OS X

                // C bools are 1 byte and so must be marshaled as I1

                [DllImport(psLib, CharSet = CharSet.Ansi)]
                internal static extern int GetErrorCategory(int errno);

                [DllImport(psLib, CharSet = CharSet.Ansi, SetLastError = true)]
                [return: MarshalAs(UnmanagedType.LPStr)]
                internal static extern string GetUserName();

                [DllImport(psLib)]
                internal static extern int GetPPid(int pid);

                [DllImport(psLib, CharSet = CharSet.Ansi, SetLastError = true)]
                internal static extern int GetLinkCount([MarshalAs(UnmanagedType.LPStr)]string filePath, out int linkCount);

                [DllImport(psLib, CharSet = CharSet.Ansi, SetLastError = true)]
                [return: MarshalAs(UnmanagedType.I1)]
                internal static extern bool IsSymLink([MarshalAs(UnmanagedType.LPStr)]string filePath);

                [DllImport(psLib, CharSet = CharSet.Ansi, SetLastError = true)]
                [return: MarshalAs(UnmanagedType.I1)]
                internal static extern bool IsExecutable([MarshalAs(UnmanagedType.LPStr)]string filePath);

                [DllImport(psLib, CharSet = CharSet.Ansi)]
                internal static extern uint GetCurrentThreadId();

                [DllImport(psLib, CharSet = CharSet.Ansi, SetLastError = true)]
                [return: MarshalAs(UnmanagedType.LPStr)]
                internal static extern string GetFullyQualifiedName();

                // This is a struct tm from <time.h>
                [StructLayout(LayoutKind.Sequential)]
                internal unsafe struct UnixTm
                {
                    public int tm_sec;    /* Seconds (0-60) */
                    public int tm_min;    /* Minutes (0-59) */
                    public int tm_hour;   /* Hours (0-23) */
                    public int tm_mday;   /* Day of the month (1-31) */
                    public int tm_mon;    /* Month (0-11) */
                    public int tm_year;   /* Year - 1900 */
                    public int tm_wday;   /* Day of the week (0-6, Sunday = 0) */
                    public int tm_yday;   /* Day in the year (0-365, 1 Jan = 0) */
                    public int tm_isdst;  /* Daylight saving time */
                }

                internal static UnixTm DateTimeToUnixTm(DateTime date)
                {
                    UnixTm tm;
                    tm.tm_sec = date.Second;
                    tm.tm_min = date.Minute;
                    tm.tm_hour = date.Hour;
                    tm.tm_mday = date.Day;
                    tm.tm_mon = date.Month - 1; // needs to be 0 indexed
                    tm.tm_year = date.Year - 1900; // years since 1900
                    tm.tm_wday = 0; // this is ignored by mktime
                    tm.tm_yday = 0; // this is also ignored
                    tm.tm_isdst = date.IsDaylightSavingTime() ? 1 : 0;
                    return tm;
                }

                [DllImport(psLib, CharSet = CharSet.Ansi, SetLastError = true)]
                internal static extern unsafe int SetDate(UnixTm* tm);

                [DllImport(psLib, CharSet = CharSet.Ansi, SetLastError = true)]
                internal static extern int CreateSymLink([MarshalAs(UnmanagedType.LPStr)]string filePath,
                                                         [MarshalAs(UnmanagedType.LPStr)]string target);

                [DllImport(psLib, CharSet = CharSet.Ansi, SetLastError = true)]
                internal static extern int CreateHardLink([MarshalAs(UnmanagedType.LPStr)]string filePath,
                                                          [MarshalAs(UnmanagedType.LPStr)]string target);

                [DllImport(psLib, CharSet = CharSet.Ansi, SetLastError = true)]
                [return: MarshalAs(UnmanagedType.LPStr)]
                internal static extern string FollowSymLink([MarshalAs(UnmanagedType.LPStr)]string filePath);

                [DllImport(psLib, CharSet = CharSet.Ansi, SetLastError = true)]
                [return: MarshalAs(UnmanagedType.LPStr)]
                internal static extern string GetUserFromPid(int pid);

                [DllImport(psLib, CharSet = CharSet.Ansi, SetLastError = true)]
                [return: MarshalAs(UnmanagedType.I1)]
                internal static extern bool IsFile([MarshalAs(UnmanagedType.LPStr)]string filePath);

                [DllImport(psLib, CharSet = CharSet.Ansi, SetLastError = true)]
                [return: MarshalAs(UnmanagedType.I1)]
                internal static extern bool IsDirectory([MarshalAs(UnmanagedType.LPStr)]string filePath);

                [DllImport(psLib, CharSet = CharSet.Ansi, SetLastError = true)]
                [return: MarshalAs(UnmanagedType.I1)]
                internal static extern bool IsSameFileSystemItem([MarshalAs(UnmanagedType.LPStr)]string filePathOne,
                                                                 [MarshalAs(UnmanagedType.LPStr)]string filePathTwo);

                [DllImport(psLib, CharSet = CharSet.Ansi, SetLastError = true)]
                internal static extern unsafe int ForkAndExecProcess(
                    string filename, byte** argv, byte** envp, string cwd,
                    int redirectStdin, int redirectStdout, int redirectStderr, int creationFlags,
                    out int lpChildPid, out int stdinFd, out int stdoutFd, out int stderrFd);
            }
        }
    }
} // namespace System.Management.Automation
