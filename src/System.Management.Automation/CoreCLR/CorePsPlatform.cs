// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Management.Automation.Internal;
using Microsoft.Win32;

namespace System.Management.Automation
{
    /// <summary>
    /// These are platform abstractions and platform specific implementations.
    /// </summary>
    public static partial class Platform
    {
        /// <summary>
        /// True if the current platform is Linux.
        /// </summary>
        public static bool IsLinux
        {
            get
            {
                return OperatingSystem.IsLinux();
            }
        }

        /// <summary>
        /// True if the current platform is macOS.
        /// </summary>
        public static bool IsMacOS
        {
            get
            {
                return OperatingSystem.IsMacOS();
            }
        }

        /// <summary>
        /// True if the current platform is Windows.
        /// </summary>
        public static bool IsWindows
        {
            get
            {
                return OperatingSystem.IsWindows();
            }
        }

        /// <summary>
        /// True if PowerShell was built targeting .NET Core.
        /// </summary>
        public static bool IsCoreCLR
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// True if the underlying system is NanoServer.
        /// </summary>
        public static bool IsNanoServer
        {
            get
            {
#if UNIX
                return false;
#else
                if (_isNanoServer.HasValue)
                {
                    return _isNanoServer.Value;
                }

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
#if UNIX
                return false;
#else
                if (_isIoT.HasValue)
                {
                    return _isIoT.Value;
                }

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

        /// <summary>
        /// True if underlying system is Windows Desktop.
        /// </summary>
        public static bool IsWindowsDesktop
        {
            get
            {
#if UNIX
                return false;
#else
                if (_isWindowsDesktop.HasValue)
                {
                    return _isWindowsDesktop.Value;
                }

                _isWindowsDesktop = !IsNanoServer && !IsIoT;
                return _isWindowsDesktop.Value;
#endif
            }
        }

        /// <summary>
        /// Gets a value indicating whether the underlying system supports single-threaded apartment.
        /// </summary>
        public static bool IsStaSupported
        {
            get
            {
#if UNIX
                return false;
#else
                return _isStaSupported.Value;
#endif
            }
        }

#if UNIX
        // Gets the location for cache and config folders.
        internal static readonly string CacheDirectory = Platform.SelectProductNameForDirectory(Platform.XDG_Type.CACHE);
        internal static readonly string ConfigDirectory = Platform.SelectProductNameForDirectory(Platform.XDG_Type.CONFIG);
#else
        // Gets the location for cache and config folders.
        internal static readonly string CacheDirectory = SafeDeriveFromSpecialFolder(
            Environment.SpecialFolder.LocalApplicationData,
            @"Microsoft\PowerShell");

        internal static readonly string ConfigDirectory = SafeDeriveFromSpecialFolder(
            Environment.SpecialFolder.Personal,
            @"PowerShell");

        private static readonly Lazy<bool> _isStaSupported = new Lazy<bool>(() =>
        {
            int result = Interop.Windows.CoInitializeEx(IntPtr.Zero, Interop.Windows.COINIT_APARTMENTTHREADED);

            // Per COM documentation: Each successful call to CoInitializeEx (including S_FALSE)
            // must be balanced by a corresponding call to CoUninitialize.
            //  - S_OK (0) means we initialized for the first time.
            //  - S_FALSE (1) means already initialized, but still increments the reference count.
            // Both require CoUninitialize to decrement the reference count.
            if (result >= 0)
            {
                Interop.Windows.CoUninitialize();
            }

            return result != Interop.Windows.E_NOTIMPL;
        });

        private static bool? _isNanoServer = null;
        private static bool? _isIoT = null;
        private static bool? _isWindowsDesktop = null;
#endif

        internal static bool TryDeriveFromCache(string path1, out string result)
        {
            if (CacheDirectory is null or [])
            {
                result = null;
                return false;
            }

            result = Path.Combine(CacheDirectory, path1);
            return true;
        }

        internal static bool TryDeriveFromCache(string path1, string path2, out string result)
        {
            if (CacheDirectory is null or [])
            {
                result = null;
                return false;
            }

            result = Path.Combine(CacheDirectory, path1, path2);
            return true;
        }

        // format files
        internal static readonly string[] FormatFileNames = new string[]
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
        /// names in different OS platforms.
        /// </summary>
        internal static class CommonEnvVariableNames
        {
#if UNIX
            internal const string Home = "HOME";
#else
            internal const string Home = "USERPROFILE";
#endif
        }

        private static string SafeDeriveFromSpecialFolder(Environment.SpecialFolder specialFolder, string subPath)
        {
            string basePath = Environment.GetFolderPath(specialFolder, Environment.SpecialFolderOption.DoNotVerify);
            if (string.IsNullOrWhiteSpace(basePath))
            {
                return string.Empty;
            }

            return Path.Join(basePath, subPath);
        }

#if UNIX
        private static string s_tempHome = null;

        /// <summary>
        /// Get the 'HOME' environment variable or create a temporary home directory if the environment variable is not set.
        /// </summary>
        private static string GetHomeOrCreateTempHome()
        {
            const string tempHomeFolderName = "pwsh-{0}-98288ff9-5712-4a14-9a11-23693b9cd91a";

            string envHome = Environment.GetEnvironmentVariable("HOME") ?? s_tempHome;
            if (envHome is not null)
            {
                return envHome;
            }

            try
            {
                s_tempHome = Path.Combine(Path.GetTempPath(), StringUtil.Format(tempHomeFolderName, Environment.UserName));
                Directory.CreateDirectory(s_tempHome);
            }
            catch (UnauthorizedAccessException)
            {
                // Directory creation may fail if the account doesn't have filesystem permission such as some service accounts.
                // Return an empty string in this case so the process working directory will be used.
                s_tempHome = string.Empty;
            }

            return s_tempHome;
        }

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
        /// Function for choosing directory location of PowerShell for profile loading.
        /// </summary>
        public static string SelectProductNameForDirectory(XDG_Type dirpath)
        {
            // TODO: XDG_DATA_DIRS implementation as per GitHub issue #1060

            string xdgconfighome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            string xdgdatahome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            string xdgcachehome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
            string envHome = GetHomeOrCreateTempHome();

            string xdgConfigHomeDefault = Path.Combine(envHome, ".config", "powershell");
            string xdgDataHomeDefault = Path.Combine(envHome, ".local", "share", "powershell");
            string xdgModuleDefault = Path.Combine(xdgDataHomeDefault, "Modules");
            string xdgCacheDefault = Path.Combine(envHome, ".cache", "powershell");

            try
            {
                switch (dirpath)
                {
                    case XDG_Type.CONFIG:
                        // Use 'XDG_CONFIG_HOME' if it's set, otherwise use the default path.
                        return string.IsNullOrEmpty(xdgconfighome)
                            ? xdgConfigHomeDefault
                            : Path.Combine(xdgconfighome, "powershell");

                    case XDG_Type.DATA:
                        // Use 'XDG_DATA_HOME' if it's set, otherwise use the default path.
                        if (string.IsNullOrEmpty(xdgdatahome))
                        {
                            // Create the default data directory if it doesn't exist.
                            Directory.CreateDirectory(xdgDataHomeDefault);
                            return xdgDataHomeDefault;
                        }
                        return Path.Combine(xdgdatahome, "powershell");

                    case XDG_Type.USER_MODULES:
                        // Use 'XDG_DATA_HOME' if it's set, otherwise use the default path.
                        if (string.IsNullOrEmpty(xdgdatahome))
                        {
                            Directory.CreateDirectory(xdgModuleDefault);
                            return xdgModuleDefault;
                        }
                        return Path.Combine(xdgdatahome, "powershell", "Modules");

                    case XDG_Type.SHARED_MODULES:
                        return "/usr/local/share/powershell/Modules";

                    case XDG_Type.CACHE:
                        // Use 'XDG_CACHE_HOME' if it's set, otherwise use the default path.
                        if (string.IsNullOrEmpty(xdgcachehome))
                        {
                            Directory.CreateDirectory(xdgCacheDefault);
                            return xdgCacheDefault;
                        }

                        string cachePath = Path.Combine(xdgcachehome, "powershell");
                        Directory.CreateDirectory(cachePath);
                        return cachePath;

                    case XDG_Type.DEFAULT:
                        // Use 'xdgConfigHomeDefault' for 'XDG_Type.DEFAULT' and create the directory if it doesn't exist.
                        Directory.CreateDirectory(xdgConfigHomeDefault);
                        return xdgConfigHomeDefault;

                    default:
                        throw new InvalidOperationException("Unreachable code.");
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Directory creation may fail if the account doesn't have filesystem permission such as some service accounts.
                // Return an empty string in this case so the process working directory will be used.
                return string.Empty;
            }
        }
#endif

        /// <summary>
        /// Mimic 'Environment.GetFolderPath(folder)' on Unix.
        /// </summary>
        internal static string GetFolderPath(Environment.SpecialFolder folder)
        {
#if UNIX
            return folder switch
            {
                Environment.SpecialFolder.ProgramFiles => Directory.Exists("/bin") ? "/bin" : string.Empty,
                Environment.SpecialFolder.MyDocuments => GetHomeOrCreateTempHome(),
                _ => throw new NotSupportedException()
            };
#else
            return Environment.GetFolderPath(folder, Environment.SpecialFolderOption.DoNotVerify);
#endif
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

        internal static bool NonWindowsIsHardLink(FileSystemInfo fileInfo)
        {
            return Unix.IsHardLink(fileInfo);
        }

        internal static string NonWindowsGetUserFromPid(int path)
        {
            return Unix.NativeMethods.GetUserFromPid(path);
        }

        internal static string NonWindowsInternalGetLinkType(FileSystemInfo fileInfo)
        {
            if (fileInfo.Attributes.HasFlag(System.IO.FileAttributes.ReparsePoint))
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

        internal static bool NonWindowsSetDate(DateTime dateToUse)
        {
            return Unix.NativeMethods.SetDate(dateToUse) == 0;
        }

        internal static bool NonWindowsIsSameFileSystemItem(string pathOne, string pathTwo)
        {
            return Unix.NativeMethods.IsSameFileSystemItem(pathOne, pathTwo);
        }

        internal static bool NonWindowsGetInodeData(string path, out ValueTuple<ulong, ulong> inodeData)
        {
            var result = Unix.NativeMethods.GetInodeData(path, out ulong device, out ulong inode);

            inodeData = (device, inode);
            return result == 0;
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
            return IsMacOS ? Unix.NativeMethods.GetPPid(pid) : Unix.GetProcFSParentPid(pid);
        }

        internal static bool NonWindowsKillProcess(int pid)
        {
            return Unix.NativeMethods.KillProcess(pid);
        }

        internal static int NonWindowsWaitPid(int pid, bool nohang)
        {
            return Unix.NativeMethods.WaitPid(pid, nohang);
        }

        // Please note that `Win32Exception(Marshal.GetLastWin32Error())`
        // works *correctly* on Linux in that it creates an exception with
        // the string perror would give you for the last set value of errno.
        // No manual mapping is required. .NET Core maps the Linux errno
        // to a PAL value and calls strerror_r underneath to generate the message.

        /// <summary>Unix specific implementations of required functionality.</summary>
        internal static partial class Unix
        {
            private static readonly Dictionary<int, string> usernameCache = new();
            private static readonly Dictionary<int, string> groupnameCache = new();

            /// <summary>The type of a Unix file system item.</summary>
            public enum ItemType
            {
                /// <summary>The item is a Directory.</summary>
                Directory,

                /// <summary>The item is a File.</summary>
                File,

                /// <summary>The item is a Symbolic Link.</summary>
                SymbolicLink,

                /// <summary>The item is a Block Device.</summary>
                BlockDevice,

                /// <summary>The item is a Character Device.</summary>
                CharacterDevice,

                /// <summary>The item is a Named Pipe.</summary>
                NamedPipe,

                /// <summary>The item is a Socket.</summary>
                Socket,
            }

            /// <summary>The mask to use to retrieve specific mode bits from the mode value in the stat class.</summary>
            public enum StatMask
            {
                /// <summary>The mask to collect the owner mode.</summary>
                OwnerModeMask = 0x1C0,

                /// <summary>The mask to get the owners read bit.</summary>
                OwnerRead = 0x100,

                /// <summary>The mask to get the owners write bit.</summary>
                OwnerWrite = 0x080,

                /// <summary>The mask to get the owners execute bit.</summary>
                OwnerExecute = 0x040,

                /// <summary>The mask to get the group mode.</summary>
                GroupModeMask = 0x038,

                /// <summary>The mask to get the group mode.</summary>
                GroupRead = 0x20,

                /// <summary>The mask to get the group mode.</summary>
                GroupWrite = 0x10,

                /// <summary>The mask to get the group mode.</summary>
                GroupExecute = 0x8,

                /// <summary>The mask to get the "other" mode.</summary>
                OtherModeMask = 0x007,

                /// <summary>The mask to get the "other" read bit.</summary>
                OtherRead = 0x004,

                /// <summary>The mask to get the "other" write bit.</summary>
                OtherWrite = 0x002,

                /// <summary>The mask to get the "other" execute bit.</summary>
                OtherExecute = 0x001,

                /// <summary>The mask to retrieve the sticky bit.</summary>
                SetStickyMask = 0x200,

                /// <summary>The mask to retrieve the setgid bit.</summary>
                SetGidMask = 0x400,

                /// <summary>The mask to retrieve the setuid bit.</summary>
                SetUidMask = 0x800,
            }

            /// <summary>The Common Stat class.</summary>
            public class CommonStat
            {
                /// <summary>The inode of the filesystem item.</summary>
                public long Inode;

                /// <summary>The Mode of the filesystem item.</summary>
                public int Mode;

                /// <summary>The user id of the filesystem item.</summary>
                public int UserId;

                /// <summary>The group id of the filesystem item.</summary>
                public int GroupId;

                /// <summary>The number of hard links for the filesystem item.</summary>
                public int HardlinkCount;

                /// <summary>The size in bytes of the filesystem item.</summary>
                public long Size;

                /// <summary>The last access time of the filesystem item.</summary>
                public DateTime AccessTime;

                /// <summary>The last modified time for the filesystem item.</summary>
                public DateTime ModifiedTime;

                /// <summary>The last time the status changes for the filesystem item.</summary>
                public DateTime StatusChangeTime;

                /// <summary>The block size of the filesystem.</summary>
                public long BlockSize;

                /// <summary>The device id of the filesystem item.</summary>
                public int DeviceId;

                /// <summary>The number of blocks used by the filesystem item.</summary>
                public int NumberOfBlocks;

                /// <summary>The type of the filesystem item.</summary>
                public ItemType ItemType;

                /// <summary>Whether the filesystem item has the setuid bit enabled.</summary>
                public bool IsSetUid;

                /// <summary>Whether the filesystem item has the setgid bit enabled.</summary>
                public bool IsSetGid;

                /// <summary>Whether the filesystem item has the sticky bit enabled. This is only available for directories.</summary>
                public bool IsSticky;

                private const char CanRead = 'r';
                private const char CanWrite = 'w';
                private const char CanExecute = 'x';
                private const char NoPerm = '-';
                private const char SetAndExec = 's';
                private const char SetAndNotExec = 'S';
                private const char StickyAndExec = 't';
                private const char StickyAndNotExec = 'T';

                // The item type and the character representation for the first element in the stat string
                private static readonly Dictionary<ItemType, char> itemTypeTable = new()
                {
                    { ItemType.BlockDevice,     'b' },
                    { ItemType.CharacterDevice, 'c' },
                    { ItemType.Directory,       'd' },
                    { ItemType.File,            '-' },
                    { ItemType.NamedPipe,       'p' },
                    { ItemType.Socket,          's' },
                    { ItemType.SymbolicLink,    'l' },
                };

                // We'll create a few common mode strings here to reduce allocations and improve performance a bit.
                private const string OwnerReadGroupReadOtherRead = "-r--r--r--";
                private const string OwnerReadWriteGroupReadOtherRead = "-rw-r--r--";
                private const string DirectoryOwnerFullGroupReadExecOtherReadExec = "drwxr-xr-x";

                /// <summary>Convert the mode to a string which is usable in our formatting.</summary>
                /// <returns>The mode converted into a Unix style string similar to the output of ls.</returns>
                public string GetModeString()
                {
                    // On an Ubuntu system (docker), these 3 are roughly 70% of all the permissions
                    if ((Mode & 0xFFF) == 292)
                    {
                        return OwnerReadGroupReadOtherRead;
                    }

                    if ((Mode & 0xFFF) == 420)
                    {
                       return OwnerReadWriteGroupReadOtherRead;
                    }

                    if (ItemType == ItemType.Directory & (Mode & 0xFFF) == 493)
                    {
                        return DirectoryOwnerFullGroupReadExecOtherReadExec;
                    }

                    UnixFileMode modeInfo = (UnixFileMode)Mode;

                    Span<char> modeCharacters = [
                        itemTypeTable[ItemType],

                        modeInfo.HasFlag(UnixFileMode.UserRead) ? CanRead : NoPerm,
                        modeInfo.HasFlag(UnixFileMode.UserWrite) ? CanWrite : NoPerm,
                        modeInfo.HasFlag(UnixFileMode.SetUser) ?
                            (modeInfo.HasFlag(UnixFileMode.UserExecute) ? SetAndExec : SetAndNotExec) :
                            (modeInfo.HasFlag(UnixFileMode.UserExecute) ? CanExecute : NoPerm),

                        modeInfo.HasFlag(UnixFileMode.GroupRead) ? CanRead : NoPerm,
                        modeInfo.HasFlag(UnixFileMode.GroupWrite) ? CanWrite : NoPerm,
                        modeInfo.HasFlag(UnixFileMode.SetGroup) ?
                            (modeInfo.HasFlag(UnixFileMode.GroupExecute) ? SetAndExec : SetAndNotExec) :
                            (modeInfo.HasFlag(UnixFileMode.GroupExecute) ? CanExecute : NoPerm),

                        modeInfo.HasFlag(UnixFileMode.OtherRead) ? CanRead : NoPerm,
                        modeInfo.HasFlag(UnixFileMode.OtherWrite) ? CanWrite : NoPerm,
                        modeInfo.HasFlag(UnixFileMode.StickyBit) ?
                            (modeInfo.HasFlag(UnixFileMode.OtherExecute) ? StickyAndExec : StickyAndNotExec) :
                            (modeInfo.HasFlag(UnixFileMode.OtherExecute) ? CanExecute : NoPerm),
                    ];

                    return new string(modeCharacters);
                }

                /// <summary>
                /// Get the user name. This is used in formatting, but we shouldn't
                /// do the pinvoke this unless we're going to use it.
                /// </summary>
                /// <returns>The user name.</returns>
                public string GetUserName()
                {
                    if (usernameCache.TryGetValue(UserId, out string username))
                    {
                        return username;
                    }

                    // Get and add the user name to the cache so we don't need to
                    // have a pinvoke for each file.
                    username = NativeMethods.GetPwUid(UserId);
                    usernameCache.Add(UserId, username);

                    return username;
                }

                /// <summary>
                /// Get the group name. This is used in formatting, but we shouldn't
                /// do the pinvoke this unless we're going to use it.
                /// </summary>
                /// <returns>The name of the group.</returns>
                public string GetGroupName()
                {
                    if (groupnameCache.TryGetValue(GroupId, out string groupname))
                    {
                        return groupname;
                    }

                    // Get and add the group name to the cache so we don't need to
                    // have a pinvoke for each file.
                    groupname = NativeMethods.GetGrGid(GroupId);
                    groupnameCache.Add(GroupId, groupname);

                    return groupname;
                }
            }

            // This is a helper that attempts to map errno into a PowerShell ErrorCategory
            internal static ErrorCategory GetErrorCategory(int errno)
            {
                return (ErrorCategory)Unix.NativeMethods.GetErrorCategory(errno);
            }

            /// <summary>Determine if the item is a hardlink.</summary>
            /// <param name="fs">A FileSystemInfo to check to determine if it is a hardlink.</param>
            /// <returns>A boolean that represents whether the item is a hardlink.</returns>
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

                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            /// <summary>
            /// Create a managed replica of the native stat structure.
            /// </summary>
            /// <param name="css">The common stat structure from which we copy.</param>
            /// <returns>A managed common stat class instance.</returns>
            private static CommonStat CopyStatStruct(NativeMethods.CommonStatStruct css)
            {
                CommonStat cs = new();
                cs.Inode = css.Inode;
                cs.Mode = css.Mode;
                cs.UserId = css.UserId;
                cs.GroupId = css.GroupId;
                cs.HardlinkCount = css.HardlinkCount;
                cs.Size = css.Size;

                // These can sometime throw if we get too large a number back (seen on Raspbian).
                // As a fallback, set the time to UnixEpoch.
                try
                {
                    cs.AccessTime = DateTime.UnixEpoch.AddSeconds(css.AccessTime).ToLocalTime();
                }
                catch
                {
                    cs.AccessTime = DateTime.UnixEpoch.ToLocalTime();
                }

                try
                {
                    cs.ModifiedTime = DateTime.UnixEpoch.AddSeconds(css.ModifiedTime).ToLocalTime();
                }
                catch
                {
                    cs.ModifiedTime = DateTime.UnixEpoch.ToLocalTime();
                }

                try
                {
                    cs.StatusChangeTime = DateTime.UnixEpoch.AddSeconds(css.StatusChangeTime).ToLocalTime();
                }
                catch
                {
                    cs.StatusChangeTime = DateTime.UnixEpoch.ToLocalTime();
                }

                cs.BlockSize = css.BlockSize;
                cs.DeviceId = css.DeviceId;
                cs.NumberOfBlocks = css.NumberOfBlocks;

                if (css.IsDirectory == 1)
                {
                    cs.ItemType = ItemType.Directory;
                }
                else if (css.IsFile == 1)
                {
                    cs.ItemType = ItemType.File;
                }
                else if (css.IsSymbolicLink == 1)
                {
                    cs.ItemType = ItemType.SymbolicLink;
                }
                else if (css.IsBlockDevice == 1)
                {
                    cs.ItemType = ItemType.BlockDevice;
                }
                else if (css.IsCharacterDevice == 1)
                {
                    cs.ItemType = ItemType.CharacterDevice;
                }
                else if (css.IsNamedPipe == 1)
                {
                    cs.ItemType = ItemType.NamedPipe;
                }
                else
                {
                    cs.ItemType = ItemType.Socket;
                }

                cs.IsSetUid = css.IsSetUid == 1;
                cs.IsSetGid = css.IsSetGid == 1;
                cs.IsSticky = css.IsSticky == 1;

                return cs;
            }

            /// <summary>Get the lstat info from a path.</summary>
            /// <param name="path">The path to the lstat information.</param>
            /// <returns>An instance of the CommonStat for the path.</returns>
            public static CommonStat GetLStat(string path)
            {
                NativeMethods.CommonStatStruct css;
                if (NativeMethods.GetCommonLStat(path, out css) == 0)
                {
                    return CopyStatStruct(css);
                }

                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            /// <summary>Get the stat info from a path.</summary>
            /// <param name="path">The path to the stat information.</param>
            /// <returns>An instance of the CommonStat for the path.</returns>
            public static CommonStat GetStat(string path)
            {
                NativeMethods.CommonStatStruct css;
                if (NativeMethods.GetCommonStat(path, out css) == 0)
                {
                    return CopyStatStruct(css);
                }

                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            /// <summary>Read the /proc file system for information about the parent.</summary>
            /// <param name="pid">The process id used to get the parent process.</param>
            /// <returns>The process id.</returns>
            public static int GetProcFSParentPid(int pid)
            {
                const int invalidPid = -1;

                // read /proc/<pid>/status
                // Row beginning with PPid: \d is the parent process id.
                // This used to check /proc/<pid>/stat but that file was meant
                // to be a space delimited line but it contains a value which
                // could contain spaces itself. Using the status file is a lot
                // simpler because each line contains a record with a simple
                // label.
                // https://github.com/PowerShell/PowerShell/issues/17541#issuecomment-1159911577
                var path = $"/proc/{pid}/status";
                try
                {
                    using FileStream fs = File.OpenRead(path);
                    using StreamReader sr = new(fs);
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (!line.StartsWith("PPid:\t", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string[] lineSplit = line.Split('\t', 2, StringSplitOptions.RemoveEmptyEntries);
                        if (lineSplit.Length != 2)
                        {
                            continue;
                        }

                        if (int.TryParse(lineSplit[1].Trim(), out var ppid))
                        {
                            return ppid;
                        }
                    }

                    return invalidPid;
                }
                catch (Exception)
                {
                    return invalidPid;
                }
            }

            /// <summary>The native methods class.</summary>
            internal static partial class NativeMethods
            {
                // errno values below are the standard POSIX numbers, identical on Linux and macOS.
                private const int EPERM = 1;
                private const int ENOENT = 2;
                private const int ESRCH = 3;
                private const int EINTR = 4;
                private const int EACCES = 13;
                private const int EINVAL = 22;

                // Maps a Unix errno to the integer value of a PowerShell ErrorCategory.
                // Mirrors the mapping previously provided by libpsl-native's GetErrorCategory.
                internal static int GetErrorCategory(int errno)
                {
                    switch (errno)
                    {
                        case EINVAL:
                            return (int)ErrorCategory.InvalidArgument;
                        case ENOENT:
                        case ESRCH:
                            return (int)ErrorCategory.ObjectNotFound;
                        case EINTR:
                            return (int)ErrorCategory.OperationStopped;
                        case EACCES:
                        case EPERM:
                            return (int)ErrorCategory.PermissionDenied;
                        default:
                            return (int)ErrorCategory.NotSpecified;
                    }
                }

                // The methods below replace former libpsl-native P/Invokes with direct calls to
                // libc (see engine/Interop/Unix/*). The Interop.Unix declarations are compiled only
                // for non-Windows builds, so the bodies are guarded with #if UNIX. These methods are
                // never invoked on Windows.
#if UNIX
                // access(path, X_OK) returns 0 when the file is executable, -1 otherwise.
                internal static bool IsExecutable(string filePath)
                {
                    return Interop.Unix.Access(filePath, Interop.Unix.X_OK) != -1;
                }

                internal static uint GetCurrentThreadId()
                {
                    if (OperatingSystem.IsMacOS())
                    {
                        Interop.Unix.PthreadThreadIdNp(IntPtr.Zero, out ulong tid);
                        return (uint)tid;
                    }

                    return (uint)Interop.Unix.GetTid();
                }

                internal static bool KillProcess(int pid)
                {
                    return Interop.Unix.Kill(pid, Interop.Unix.SIGKILL) == 0;
                }

                internal static int WaitPid(int pid, bool nohang)
                {
                    return Interop.Unix.WaitPid(pid, IntPtr.Zero, nohang ? Interop.Unix.WNOHANG : 0);
                }

                // Set the system clock. Mirrors libpsl-native's SetDate, which converted a
                // broken-down local time via mktime() and called settimeofday(). DateTimeOffset
                // performs the equivalent local-time-to-Unix-seconds conversion in managed code.
                internal static int SetDate(DateTime date)
                {
                    Interop.Unix.Timeval tv;
                    tv.Seconds = new DateTimeOffset(date).ToUnixTimeSeconds();
                    tv.Microseconds = 0;
                    return Interop.Unix.SetTimeOfDay(ref tv, IntPtr.Zero);
                }

                internal static int CreateSymLink(string filePath, string target)
                {
                    // libpsl-native mapped CreateSymLink(link, target) to symlink(target, link).
                    return Interop.Unix.Symlink(target, filePath);
                }

                internal static int CreateHardLink(string filePath, string target)
                {
                    // libpsl-native mapped CreateHardLink(newlink, target) to link(target, newlink).
                    return Interop.Unix.Link(target, filePath);
                }

                // File type bits (S_IFMT family) and set-user/group/sticky bits. These constants
                // are identical on Linux and macOS.
                private const int S_IFMT = 0xF000;
                private const int S_IFDIR = 0x4000;
                private const int S_IFCHR = 0x2000;
                private const int S_IFBLK = 0x6000;
                private const int S_IFREG = 0x8000;
                private const int S_IFIFO = 0x1000;
                private const int S_IFLNK = 0xA000;
                private const int S_IFSOCK = 0xC000;
                private const int S_ISUID = 0x800;
                private const int S_ISGID = 0x400;
                private const int S_ISVTX = 0x200;

                internal static int GetCommonStat(string filePath, out CommonStatStruct cs)
                {
                    return GetCommonStatImpl(filePath, followSymlink: true, out cs);
                }

                internal static int GetCommonLStat(string filePath, out CommonStatStruct cs)
                {
                    return GetCommonStatImpl(filePath, followSymlink: false, out cs);
                }

                private static int GetCommonStatImpl(string filePath, bool followSymlink, out CommonStatStruct cs)
                {
                    cs = default;
                    int ret = Interop.Unix.Stat(filePath, followSymlink, out Interop.Unix.StatInfo info);
                    if (ret != 0)
                    {
                        return ret;
                    }

                    cs.Inode = info.Inode;
                    cs.Mode = info.Mode;
                    cs.UserId = info.UserId;
                    cs.GroupId = info.GroupId;
                    cs.HardlinkCount = info.HardlinkCount;
                    cs.Size = info.Size;
                    cs.AccessTime = info.AccessTime;
                    cs.ModifiedTime = info.ModifiedTime;
                    cs.StatusChangeTime = info.StatusChangeTime;
                    cs.BlockSize = info.BlockSize;
                    cs.DeviceId = (int)info.Device;
                    cs.NumberOfBlocks = (int)info.NumberOfBlocks;

                    int fmt = info.Mode & S_IFMT;
                    cs.IsDirectory = fmt == S_IFDIR ? 1 : 0;
                    cs.IsFile = fmt == S_IFREG ? 1 : 0;
                    cs.IsSymbolicLink = fmt == S_IFLNK ? 1 : 0;
                    cs.IsBlockDevice = fmt == S_IFBLK ? 1 : 0;
                    cs.IsCharacterDevice = fmt == S_IFCHR ? 1 : 0;
                    cs.IsNamedPipe = fmt == S_IFIFO ? 1 : 0;
                    cs.IsSocket = fmt == S_IFSOCK ? 1 : 0;

                    // Matches libpsl-native: only the corresponding bit among the three special bits is set.
                    cs.IsSetUid = (info.Mode & 0xE00) == S_ISUID ? 1 : 0;
                    cs.IsSetGid = (info.Mode & 0xE00) == S_ISGID ? 1 : 0;
                    cs.IsSticky = (info.Mode & 0xE00) == S_ISVTX ? 1 : 0;
                    return 0;
                }

                // Uses lstat semantics, matching libpsl-native's GetLinkCount.
                internal static int GetLinkCount(string filePath, out int linkCount)
                {
                    int ret = Interop.Unix.Stat(filePath, followSymlink: false, out Interop.Unix.StatInfo info);
                    linkCount = info.HardlinkCount;
                    return ret;
                }

                // Uses stat semantics (follows symlinks), matching libpsl-native's GetInodeData.
                internal static int GetInodeData(string path, out ulong device, out ulong inode)
                {
                    int ret = Interop.Unix.Stat(path, followSymlink: true, out Interop.Unix.StatInfo info);
                    device = (ulong)info.Device;
                    inode = (ulong)info.Inode;
                    return ret;
                }

                internal static bool IsSameFileSystemItem(string filePathOne, string filePathTwo)
                {
                    if (Interop.Unix.Stat(filePathOne, followSymlink: true, out Interop.Unix.StatInfo one) == 0
                        && Interop.Unix.Stat(filePathTwo, followSymlink: true, out Interop.Unix.StatInfo two) == 0)
                    {
                        return one.Device == two.Device && one.Inode == two.Inode;
                    }

                    return false;
                }

                // macOS: getppid() only returns the *current* process's parent, so the parent of an
                // arbitrary pid is obtained via proc_pidinfo. On Linux the caller uses /proc instead.
                internal static int GetPPid(int pid)
                {
                    return Interop.Unix.GetParentPid(pid);
                }

                internal static string GetUserFromPid(int pid)
                {
                    if (Platform.IsMacOS)
                    {
                        if (Interop.Unix.TryGetProcessUserId(pid, out uint uid))
                        {
                            return GetPwUid(unchecked((int)uid));
                        }

                        return null;
                    }

                    // Linux: the owner of /proc/<pid> is the process's real user id.
                    if (Interop.Unix.Stat($"/proc/{pid}", followSymlink: true, out Interop.Unix.StatInfo info) == 0)
                    {
                        return GetPwUid(info.UserId);
                    }

                    return null;
                }

                internal static string GetPwUid(int id)
                {
                    return Interop.Unix.GetPwUid(id);
                }

                internal static string GetGrGid(int id)
                {
                    return Interop.Unix.GetGrGid(id);
                }
#else
                // Windows builds exclude engine/Interop/Unix. These Unix-only helpers are never
                // called on Windows but must be present so the platform-neutral wrappers compile.
                internal static bool IsExecutable(string filePath)
                    => throw new PlatformNotSupportedException();

                internal static uint GetCurrentThreadId()
                    => throw new PlatformNotSupportedException();

                internal static bool KillProcess(int pid)
                    => throw new PlatformNotSupportedException();

                internal static int WaitPid(int pid, bool nohang)
                    => throw new PlatformNotSupportedException();

                internal static int SetDate(DateTime date)
                    => throw new PlatformNotSupportedException();

                internal static int CreateSymLink(string filePath, string target)
                    => throw new PlatformNotSupportedException();

                internal static int CreateHardLink(string filePath, string target)
                    => throw new PlatformNotSupportedException();

                internal static int GetCommonStat(string filePath, out CommonStatStruct cs)
                    => throw new PlatformNotSupportedException();

                internal static int GetCommonLStat(string filePath, out CommonStatStruct cs)
                    => throw new PlatformNotSupportedException();

                internal static int GetLinkCount(string filePath, out int linkCount)
                    => throw new PlatformNotSupportedException();

                internal static int GetInodeData(string path, out ulong device, out ulong inode)
                    => throw new PlatformNotSupportedException();

                internal static bool IsSameFileSystemItem(string filePathOne, string filePathTwo)
                    => throw new PlatformNotSupportedException();

                internal static int GetPPid(int pid)
                    => throw new PlatformNotSupportedException();

                internal static string GetUserFromPid(int pid)
                    => throw new PlatformNotSupportedException();

                internal static string GetPwUid(int id)
                    => throw new PlatformNotSupportedException();

                internal static string GetGrGid(int id)
                    => throw new PlatformNotSupportedException();
#endif

                /// <summary>
                /// This is a struct from getcommonstat.h in the native library.
                /// It presents each member of the stat structure as the largest type of that member across
                /// all stat structures on the platforms we support. This allows us to present a common
                /// stat structure for all our platforms.
                /// </summary>
                [StructLayout(LayoutKind.Sequential)]
                internal struct CommonStatStruct
                {
                    /// <summary>The inode of the filesystem item.</summary>
                    internal long Inode;

                    /// <summary>The mode of the filesystem item.</summary>
                    internal int Mode;

                    /// <summary>The user id of the filesystem item.</summary>
                    internal int UserId;

                    /// <summary>The group id of the filesystem item.</summary>
                    internal int GroupId;

                    /// <summary>The number of hard links to the filesystem item.</summary>
                    internal int HardlinkCount;

                    /// <summary>The size in bytes of the filesystem item.</summary>
                    internal long Size;

                    /// <summary>The time of the last access for the filesystem item.</summary>
                    internal long AccessTime;

                    /// <summary>The time of the last modification for the filesystem item.</summary>
                    internal long ModifiedTime;

                    /// <summary>The time of the last status change for the filesystem item.</summary>
                    internal long StatusChangeTime;

                    /// <summary>The size in bytes of the file system.</summary>
                    internal long BlockSize;

                    /// <summary>The device id for the filesystem item.</summary>
                    internal int DeviceId;

                    /// <summary>The number of filesystem blocks that the filesystem item uses.</summary>
                    internal int NumberOfBlocks;

                    /// <summary>This filesystem item is a directory.</summary>
                    internal int IsDirectory;

                    /// <summary>This filesystem item is a file.</summary>
                    internal int IsFile;

                    /// <summary>This filesystem item is a symbolic link.</summary>
                    internal int IsSymbolicLink;

                    /// <summary>This filesystem item is a block device.</summary>
                    internal int IsBlockDevice;

                    /// <summary>This filesystem item is a character device.</summary>
                    internal int IsCharacterDevice;

                    /// <summary>This filesystem item is a named pipe.</summary>
                    internal int IsNamedPipe;

                    /// <summary>This filesystem item is a socket.</summary>
                    internal int IsSocket;

                    /// <summary>This filesystem item will run as the owner if executed.</summary>
                    internal int IsSetUid;

                    /// <summary>This filesystem item will run as the group if executed.</summary>
                    internal int IsSetGid;

                    /// <summary>Whether the sticky bit is set on the filesystem item.</summary>
                    internal int IsSticky;
                }
            }
        }
    }
}
