/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.IO;

namespace System.Management.Automation
{
    /// <summary>
    /// These are platform abstractions and platform specific implementations
    /// </summary>
    public static class Platform
    {
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
            string xdgConfigHomeDefault = Path.Combine(System.Environment.GetEnvironmentVariable("HOME"), ".config", "powershell");
            string xdgDataHomeDefault = Path.Combine(System.Environment.GetEnvironmentVariable("HOME"), ".local", "share", "powershell");
            string xdgModuleDefault = Path.Combine(xdgDataHomeDefault, "Modules");
            string xdgCacheDefault = Path.Combine(System.Environment.GetEnvironmentVariable("HOME"), ".cache", "powershell");

            switch (dirpath)
            {
                case Platform.XDG_Type.CONFIG:
                    //the user has set XDG_CONFIG_HOME corrresponding to profile path
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
                            Directory.CreateDirectory(xdgDataHomeDefault);
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
                            Directory.CreateDirectory(xdgModuleDefault);
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
                            Directory.CreateDirectory(xdgCacheDefault);
                        }

                        return xdgCacheDefault;
                    }

                    else
                    {
                        if (!Directory.Exists(Path.Combine(xdgcachehome, "powershell")))
                        {
                            Directory.CreateDirectory(Path.Combine(xdgcachehome, "powershell"));
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
            return Unix.NativeMethods.CreateSymLink(path, target);
        }

        internal static bool NonWindowsCreateHardLink(string path, string strTargetPath)
        {
            return Unix.CreateHardLink(path, strTargetPath);
        }

        internal static void NonWindowsSetDate(DateTime dateToUse)
        {
            Unix.SetDateInfoInternal date = new Unix.SetDateInfoInternal(dateToUse);
            Unix.SetDate(date);
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

        internal static bool NonWindowsIsExecutable(string path)
        {
            return Unix.NativeMethods.IsExecutable(path);
        }

        internal static uint NonWindowsGetThreadId()
        {
            // TODO:PSL clean this up
            return 0;
        }

        internal static class Unix
        {
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
                if (ret == 1)
                {
                    return count > 1;
                }
                else
                {
                    int lastError = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException("Unix.IsHardLink error: " + lastError);
                }
            }

            public static void SetDate(SetDateInfoInternal info)
            {
                int ret = NativeMethods.SetDate(info);
                if (ret == -1)
                {
                    int lastError = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException("Unix.NonWindowsSetDate error: " + lastError);
                }
            }

            public static bool CreateHardLink(string path, string strTargetPath)
            {
                int ret = NativeMethods.CreateHardLink(path, strTargetPath);
                return ret == 1 ? true : false;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal class SetDateInfoInternal
            {
                public int Year;
                public int Month;
                public int Day;
                public int Hour;
                public int Minute;
                public int Second;
                public int Millisecond;
                public int DST;

                public SetDateInfoInternal(DateTime d)
                {
                    Year = d.Year;
                    Month = d.Month;
                    Day = d.Day;
                    Hour = d.Hour;
                    Minute = d.Minute;
                    Second = d.Second;
                    Millisecond = d.Millisecond;
                    DST = d.IsDaylightSavingTime() ? 1 : 0;
                }

                public override string ToString()
                {
                    string ret = String.Format("Year = {0}; Month = {1}; Day = {2}; Hour = {3}; Minute = {4}; Second = {5}; Millisec = {6}; DST = {7}", Year, Month, Day, Hour, Minute, Second, Millisecond, DST);
                    return ret;
                }
            }

            internal static class NativeMethods
            {
                private const string psLib = "libpsl-native";

                // Ansi is a misnomer, it is hardcoded to UTF-8 on Linux and OS X

                // C bools are 1 byte and so must be marshaled as I1

                [DllImport(psLib, CharSet = CharSet.Ansi, SetLastError = true)]
                [return: MarshalAs(UnmanagedType.LPStr)]
                internal static extern string GetUserName();

                [DllImport(psLib, CharSet = CharSet.Ansi, SetLastError = true)]
                internal static extern int GetLinkCount([MarshalAs(UnmanagedType.LPStr)]string filePath, out int linkCount);

                [DllImport(psLib, CharSet = CharSet.Ansi, SetLastError = true)]
                [return: MarshalAs(UnmanagedType.I1)]
                internal static extern bool IsSymLink([MarshalAs(UnmanagedType.LPStr)]string filePath);

                [DllImport(psLib, CharSet = CharSet.Ansi, SetLastError = true)]
                [return: MarshalAs(UnmanagedType.I1)]
                internal static extern bool IsExecutable([MarshalAs(UnmanagedType.LPStr)]string filePath);

                [DllImport(psLib, CharSet = CharSet.Ansi, SetLastError = true)]
                [return: MarshalAs(UnmanagedType.LPStr)]
                internal static extern string GetFullyQualifiedName();

                [DllImport(psLib, CharSet = CharSet.Ansi, SetLastError = true)]
                internal static extern int SetDate(SetDateInfoInternal info);

                [DllImport(psLib, CharSet = CharSet.Ansi, SetLastError = true)]
                [return: MarshalAs(UnmanagedType.I1)]
                internal static extern bool CreateSymLink([MarshalAs(UnmanagedType.LPStr)]string filePath,
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
            }
        }
    }
} // namespace System.Management.Automation
