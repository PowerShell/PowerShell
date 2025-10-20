// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

namespace System.Management.Automation
{
    internal static class PlatformInvokes
    {
        [StructLayout(LayoutKind.Sequential)]
        internal sealed class FILETIME
        {
            internal uint dwLowDateTime;
            internal uint dwHighDateTime;

            internal FILETIME()
            {
                dwLowDateTime = 0;
                dwHighDateTime = 0;
            }

            internal FILETIME(long fileTime)
            {
                dwLowDateTime = (uint)fileTime;
                dwHighDateTime = (uint)(fileTime >> 32);
            }

            public long ToTicks()
            {
                return ((long)dwHighDateTime << 32) + dwLowDateTime;
            }
        }

        [Flags]
        // dwDesiredAccess of CreateFile
        internal enum FileDesiredAccess : uint
        {
            GenericRead = 0x80000000,
            GenericWrite = 0x40000000,
            GenericExecute = 0x20000000,
            GenericAll = 0x10000000,
        }

        [Flags]
        // dwShareMode of CreateFile
        internal enum FileShareMode : uint
        {
            None = 0x00000000,
            Read = 0x00000001,
            Write = 0x00000002,
            Delete = 0x00000004,
        }

        // dwCreationDisposition of CreateFile
        internal enum FileCreationDisposition : uint
        {
            New = 1,
            CreateAlways = 2,
            OpenExisting = 3,
            OpenAlways = 4,
            TruncateExisting = 5,
        }

        [Flags]
        // dwFlagsAndAttributes
        internal enum FileAttributes : uint
        {
            ReadOnly = 0x00000001,
            Hidden = 0x00000002,
            System = 0x00000004,
            Directory = 0x00000010,
            Archive = 0x00000020,
            Normal = 0x00000080,
            Temporary = 0x00000100,
            Offline = 0x00001000,
            NotContentIndexed = 0x00002000,
            Encrypted = 0x00004000,
            Write_Through = 0x80000000,
            Overlapped = 0x40000000,
            NoBuffering = 0x20000000,
            RandomAccess = 0x10000000,
            SequentialScan = 0x08000000,
            DeleteOnClose = 0x04000000,
            BackupSemantics = 0x02000000,
            PosixSemantics = 0x01000000,
            OpenReparsePoint = 0x00200000,
            OpenNoRecall = 0x00100000,
            SessionAware = 0x00800000
        }

        [StructLayout(LayoutKind.Sequential)]
        internal sealed class SecurityAttributes
        {
            internal int nLength;
            internal SafeLocalMemHandle lpSecurityDescriptor;
            internal bool bInheritHandle;

            internal SecurityAttributes()
            {
                this.nLength = 12;
                this.bInheritHandle = true;
                this.lpSecurityDescriptor = new SafeLocalMemHandle(IntPtr.Zero, true);
            }
        }

        internal sealed class SafeLocalMemHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            // Methods
            internal SafeLocalMemHandle()
                : base(true)
            {
            }

            internal SafeLocalMemHandle(IntPtr existingHandle, bool ownsHandle)
                : base(ownsHandle)
            {
                base.SetHandle(existingHandle);
            }

            [DllImport(PinvokeDllNames.LocalFreeDllName)]
            private static extern IntPtr LocalFree(IntPtr hMem);

            protected override bool ReleaseHandle()
            {
                return (LocalFree(base.handle) == IntPtr.Zero);
            }
        }

        /// <summary>
        /// Creates or opens a file, file stream, directory, physical disk, volume, console buffer,
        /// tape drive, communications resource, mailslot, or named pipe. The function returns a
        /// handle that can be used to access the object.
        /// </summary>
        /// <param name="lpFileName">
        /// The name of the object to be created or opened.
        /// In the ANSI version of this function, the name is limited to MAX_PATH characters.
        /// To extend this limit to 32,767 wide characters, call the Unicode version of the
        /// function and prepend "\\?\" to the path. For more information, see Naming a File.
        /// For information on special device names, see Defining an MS-DOS Device Name.
        /// To specify a COM port number greater than 9, use the following syntax: "\\.\COM10".
        /// This syntax works for all port numbers and hardware that allows COM port numbers to be specified.
        /// To create a file stream, specify the name of the file, a colon, and then the name of the
        /// stream. For more information, see File Streams.
        /// </param>
        /// <param name="dwDesiredAccess">
        /// The access to the object, which can be read, write, or both.
        /// You cannot request an access mode that conflicts with the sharing mode that is
        /// specified in an open request that has an open handle.
        /// If this parameter is zero (0), the application can query file and device attributes
        /// without accessing a device. This is useful for an application to determine the size
        /// of a floppy disk drive and the formats it supports without requiring a floppy in a drive.
        /// It can also be used to test for the existence of a file or directory without opening
        /// them for read or write access.
        /// See the "CreateFile desired access" below.
        /// </param>
        /// <param name="dwShareMode">
        /// The sharing mode of an object, which can be read, write, both, or none.
        /// You cannot request a sharing mode that conflicts with the access mode that is specified
        /// in an open request that has an open handle, because that would result in the following
        /// sharing violation: ERROR_SHARING_VIOLATION.
        /// If this parameter is zero (0) and CreateFile succeeds, the object cannot be shared
        /// and cannot be opened again until the handle is closed. For more information, see the
        /// Remarks section of this topic.
        /// The sharing options remain in effect until you close the handle to an object.
        /// To enable a process to share an object while another process has the object open,
        /// use a combination of one or more of the following values to specify the access mode
        /// they can request to open the object.
        /// </param>
        /// <param name="lpSecurityAttributes">
        /// A pointer to a SECURITY_ATTRIBUTES structure that determines whether or not the returned
        /// handle can be inherited by child processes.
        /// If lpSecurityAttributes is NULL, the handle cannot be inherited.
        /// The lpSecurityDescriptor member of the structure specifies a security descriptor
        /// for an object. If lpSecurityAttributes is NULL, the object gets a default security descriptor.
        /// The access control lists (ACL) in the default security descriptor for a file or directory
        /// are inherited from its parent directory.
        /// The target file system must support security on files and directories for this parameter to
        /// have an effect on them, which is indicated when GetVolumeInformation returns FS_PERSISTENT_ACLS.
        /// CreateFile ignores lpSecurityDescriptor when opening an existing file, but continues to
        /// use the other structure members.
        /// </param>
        /// <param name="dwCreationDisposition">
        /// An action to take on files that exist and do not exist.
        /// See "CreateFile creation disposition" below
        /// </param>
        /// <param name="dwFlagsAndAttributes">
        /// The file attributes and flags.
        /// This parameter can include any combination of the file attributes.
        /// All other file attributes override FILE_ATTRIBUTE_NORMAL.
        /// When CreateFile opens a file, it combines the file flags with existing
        /// file attributes, and ignores any supplied file attributes.
        /// </param>
        /// <param name="hTemplateFile">
        /// A handle to a template file with the GENERIC_READ access right.
        /// The template file supplies file attributes and extended attributes for the file that is
        /// being created. This parameter can be NULL.
        /// When opening an existing file, CreateFile ignores the template file.
        /// When opening a new EFS-encrypted file, the file inherits the DACL from its parent directory.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is an open handle to a specified file.
        /// If a specified file exists before the function call and dwCreationDisposition is CREATE_ALWAYS
        /// or OPEN_ALWAYS, a call to GetLastError returns ERROR_ALREADY_EXISTS, even when the
        /// function succeeds. If a file does not exist before the call, GetLastError returns zero (0).
        /// If the function fails, the return value is INVALID_HANDLE_VALUE.
        /// To get extended error information, call GetLastError.
        /// </returns>
        [DllImport(PinvokeDllNames.CreateFileDllName, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr CreateFile(
            string lpFileName,
            FileDesiredAccess dwDesiredAccess,
            FileShareMode dwShareMode,
            IntPtr lpSecurityAttributes,
            FileCreationDisposition dwCreationDisposition,
            FileAttributes dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="handle">
        /// A valid handle to an open object.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero.
        /// If the function fails, the return value is zero. To get extended error information,
        /// call GetLastError.
        /// If the application is running under a debugger, the function will throw an exception
        /// if it receives either a handle value that is not valid or a pseudo-handle value.
        /// This can happen if you close a handle twice, or if you call CloseHandle on a handle
        /// returned by the FindFirstFile function.
        /// </returns>
        [DllImport(PinvokeDllNames.CloseHandleDllName, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr handle);

        [DllImport(PinvokeDllNames.DosDateTimeToFileTimeDllName, SetLastError = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DosDateTimeToFileTime(
            short wFatDate, // _In_   WORD
            short wFatTime, // _In_   WORD
            FILETIME lpFileTime); // _Out_ LPFILETIME

        [DllImport(PinvokeDllNames.LocalFileTimeToFileTimeDllName, SetLastError = false, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool LocalFileTimeToFileTime(
            FILETIME lpLocalFileTime, // _In_   const FILETIME *
            FILETIME lpFileTime); // _Out_ LPFILETIME

        [DllImport(PinvokeDllNames.SetFileTimeDllName, SetLastError = false, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetFileTime(
            IntPtr hFile, // _In_      HANDLE
            FILETIME lpCreationTime, // _In_opt_ const FILETIME *
            FILETIME lpLastAccessTime, // _In_opt_ const FILETIME *
            FILETIME lpLastWriteTime); // _In_opt_ const FILETIME *

        [DllImport(PinvokeDllNames.SetFileAttributesWDllName, SetLastError = false, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetFileAttributesW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpFileName, // _In_ LPCTSTR
            FileAttributes dwFileAttributes); // _In_ DWORD

        /// <summary>
        /// Enable the privilege specified by the privilegeName. If the specified privilege is already enabled, return true
        /// with the oldPrivilegeState.PrivilegeCount set to 0. Otherwise, enable the specified privilege, and the old privilege
        /// state will be saved in oldPrivilegeState.
        /// </summary>
        /// <param name="privilegeName"></param>
        /// <param name="oldPrivilegeState"></param>
        /// <returns></returns>
        internal static bool EnableTokenPrivilege(string privilegeName, ref TOKEN_PRIVILEGE oldPrivilegeState)
        {
            bool success = false;
            TOKEN_PRIVILEGE newPrivilegeState = new TOKEN_PRIVILEGE();

            // Check if the caller has the specified privilege or not
            if (LookupPrivilegeValue(null, privilegeName, ref newPrivilegeState.Privilege.Luid))
            {
                // Get the pseudo handler of the current process
                IntPtr processHandler = GetCurrentProcess();
                if (processHandler != IntPtr.Zero)
                {
                    // Get the handler of the current process's access token
                    IntPtr tokenHandler = IntPtr.Zero;
                    if (OpenProcessToken(processHandler, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out tokenHandler))
                    {
                        // Check if the specified privilege is already enabled
                        PRIVILEGE_SET requiredPrivilege = new PRIVILEGE_SET();
                        requiredPrivilege.Privilege.Luid = newPrivilegeState.Privilege.Luid;
                        requiredPrivilege.PrivilegeCount = 1;
                        // PRIVILEGE_SET_ALL_NECESSARY is defined as 1
                        requiredPrivilege.Control = 1;
                        bool privilegeEnabled = false;

                        if (PrivilegeCheck(tokenHandler, ref requiredPrivilege, out privilegeEnabled) && privilegeEnabled)
                        {
                            // The specified privilege is already enabled
                            oldPrivilegeState.PrivilegeCount = 0;
                            success = true;
                        }
                        else
                        {
                            // The specified privilege is not enabled yet. Enable it.
                            newPrivilegeState.PrivilegeCount = 1;
                            newPrivilegeState.Privilege.Attributes = SE_PRIVILEGE_ENABLED;
                            int bufferSize = Marshal.SizeOf<TOKEN_PRIVILEGE>();
                            int returnSize = 0;

                            // enable the specified privilege
                            if (AdjustTokenPrivileges(tokenHandler, false, ref newPrivilegeState, bufferSize, out oldPrivilegeState, ref returnSize))
                            {
                                // AdjustTokenPrivileges returns true does not mean all specified privileges have been successfully enabled
                                int retCode = Marshal.GetLastWin32Error();
                                if (retCode == ERROR_SUCCESS)
                                {
                                    success = true;
                                }
                                else if (retCode == 1300)
                                {
                                    // 1300 - Not all privileges referenced are assigned to the caller. This means the specified privilege is not
                                    // assigned to the current user. For example, suppose the role of current caller is "User", then privilege "SeRemoteShutdownPrivilege"
                                    // is not assigned to the role. In this case, we just return true and leave the call to "Win32Shutdown" to decide
                                    // whether the permission is granted or not.
                                    // Set oldPrivilegeState.PrivilegeCount to 0 to avoid the privilege restore later (PrivilegeCount - how many privileges are modified)
                                    oldPrivilegeState.PrivilegeCount = 0;
                                    success = true;
                                }
                            }
                        }
                    }

                    // Close the token handler and the process handler
                    if (tokenHandler != IntPtr.Zero)
                    {
                        CloseHandle(tokenHandler);
                    }

                    CloseHandle(processHandler);
                }
            }

            return success;
        }

        /// <summary>
        /// Restore the previous privilege state.
        /// </summary>
        /// <param name="privilegeName"></param>
        /// <param name="previousPrivilegeState"></param>
        /// <returns></returns>
        internal static bool RestoreTokenPrivilege(string privilegeName, ref TOKEN_PRIVILEGE previousPrivilegeState)
        {
            // The privilege was not changed, do not need to restore it.
            if (previousPrivilegeState.PrivilegeCount == 0)
            {
                return true;
            }

            bool success = false;
            TOKEN_PRIVILEGE newState = new TOKEN_PRIVILEGE();

            // Check if the caller has the specified privilege or not. If the caller has it, check the LUID specified in previousPrivilegeState
            // to see if the previousPrivilegeState is defined for the same privilege
            if (LookupPrivilegeValue(null, privilegeName, ref newState.Privilege.Luid) &&
                newState.Privilege.Luid.HighPart == previousPrivilegeState.Privilege.Luid.HighPart &&
                newState.Privilege.Luid.LowPart == previousPrivilegeState.Privilege.Luid.LowPart)
            {
                // Get the pseudo handler of the current process
                IntPtr processHandler = GetCurrentProcess();
                if (processHandler != IntPtr.Zero)
                {
                    // Get the handler of the current process's access token
                    IntPtr tokenHandler = IntPtr.Zero;
                    if (OpenProcessToken(processHandler, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out tokenHandler))
                    {
                        int bufferSize = Marshal.SizeOf<TOKEN_PRIVILEGE>();
                        int returnSize = 0;

                        // restore the privilege state back to the previous privilege state
                        if (AdjustTokenPrivileges(tokenHandler, false, ref previousPrivilegeState, bufferSize, out newState, ref returnSize))
                        {
                            if (Marshal.GetLastWin32Error() == ERROR_SUCCESS)
                            {
                                success = true;
                            }
                        }
                    }

                    if (tokenHandler != IntPtr.Zero)
                    {
                        CloseHandle(tokenHandler);
                    }

                    CloseHandle(processHandler);
                }
            }

            return success;
        }

        /// <summary>
        /// The LookupPrivilegeValue function retrieves the locally unique identifier (LUID) used on a specified system to locally represent
        /// the specified privilege name.
        /// </summary>
        /// <param name="lpSystemName"></param>
        /// <param name="lpName"></param>
        /// <param name="lpLuid"></param>
        /// <returns></returns>
        [DllImport(PinvokeDllNames.LookupPrivilegeValueDllName, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, ref LUID lpLuid);

        /// <summary>
        /// The PrivilegeCheck function determines whether a specified privilege is enabled in an access token.
        /// </summary>
        /// <param name="tokenHandler"></param>
        /// <param name="requiredPrivileges"></param>
        /// <param name="pfResult"></param>
        /// <returns></returns>
        [DllImport(PinvokeDllNames.PrivilegeCheckDllName, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PrivilegeCheck(IntPtr tokenHandler, ref PRIVILEGE_SET requiredPrivileges, out bool pfResult);

        /// <summary>
        /// The AdjustTokenPrivileges function enables or disables privileges in the specified access token. Enabling or disabling privileges in
        /// an access token requires TOKEN_ADJUST_PRIVILEGES access. The TOKEN_ADJUST_PRIVILEGES and TOKEN_QUERY accesses are gained when calling
        /// the OpenProcessToken function.
        /// </summary>
        /// <param name="tokenHandler"></param>
        /// <param name="disableAllPrivilege"></param>
        /// <param name="newPrivilegeState"></param>
        /// <param name="bufferLength"></param>
        /// <param name="previousPrivilegeState"></param>
        /// <param name="returnLength"></param>
        /// <returns></returns>
        [DllImport(PinvokeDllNames.AdjustTokenPrivilegesDllName, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AdjustTokenPrivileges(IntPtr tokenHandler, bool disableAllPrivilege,
                                                          ref TOKEN_PRIVILEGE newPrivilegeState, int bufferLength,
                                                          out TOKEN_PRIVILEGE previousPrivilegeState,
                                                          ref int returnLength);

        [StructLayout(LayoutKind.Sequential)]
        internal struct TOKEN_PRIVILEGE
        {
            internal uint PrivilegeCount;
            internal LUID_AND_ATTRIBUTES Privilege;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct LUID
        {
            internal uint LowPart;
            internal uint HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct LUID_AND_ATTRIBUTES
        {
            internal LUID Luid;
            internal uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PRIVILEGE_SET
        {
            internal uint PrivilegeCount;
            internal uint Control;
            internal LUID_AND_ATTRIBUTES Privilege;
        }

        /// <summary>
        /// Get the pseudo handler of the current process.
        /// </summary>
        /// <returns></returns>
        [DllImport(PinvokeDllNames.GetCurrentProcessDllName)]
        internal static extern IntPtr GetCurrentProcess();

        /// <summary>
        /// Retrieves the current process token.
        /// This function exists just for backward compatibility. It is preferred to use the other override that takes 'SafeHandle' as parameter.
        /// </summary>
        /// <param name="processHandle">Process handle.</param>
        /// <param name="desiredAccess">Token access.</param>
        /// <param name="tokenHandle">Process token.</param>
        /// <returns>The current process token.</returns>
        [DllImport(PinvokeDllNames.OpenProcessTokenDllName, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

        /// <summary>
        /// Required to enable or disable the privileges in an access token.
        /// </summary>
        internal const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;

        /// <summary>
        /// Required to query an access token.
        /// </summary>
        internal const int TOKEN_QUERY = 0x00000008;

        /// <summary>
        /// Combines all possible access rights for a token.
        /// </summary>
        internal const int TOKEN_ALL_ACCESS = 0x001f01ff;

        internal const uint SE_PRIVILEGE_DISABLED = 0x00000000;
        internal const uint SE_PRIVILEGE_ENABLED_BY_DEFAULT = 0x00000001;
        internal const uint SE_PRIVILEGE_ENABLED = 0x00000002;
        internal const uint SE_PRIVILEGE_USED_FOR_ACCESS = 0x80000000;

        internal const int ERROR_SUCCESS = 0x0;

        #region CreateProcess for SSH Remoting

#if !UNIX

        // Fields
        internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        internal static readonly UInt32 GENERIC_READ = 0x80000000;
        internal static readonly UInt32 GENERIC_WRITE = 0x40000000;
        internal static readonly UInt32 FILE_ATTRIBUTE_NORMAL = 0x80000000;
        internal static readonly UInt32 CREATE_ALWAYS = 2;
        internal static readonly UInt32 FILE_SHARE_WRITE = 0x00000002;
        internal static readonly UInt32 FILE_SHARE_READ = 0x00000001;
        internal static readonly UInt32 OF_READWRITE = 0x00000002;
        internal static readonly UInt32 OPEN_EXISTING = 3;

        [StructLayout(LayoutKind.Sequential)]
        internal sealed class PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;

            public PROCESS_INFORMATION()
            {
                this.hProcess = IntPtr.Zero;
                this.hThread = IntPtr.Zero;
            }

            /// <summary>
            /// Dispose.
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
            }

            /// <summary>
            /// Dispose.
            /// </summary>
            /// <param name="disposing"></param>
            private void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (this.hProcess != IntPtr.Zero)
                    {
                        CloseHandle(this.hProcess);
                        this.hProcess = IntPtr.Zero;
                    }

                    if (this.hThread != IntPtr.Zero)
                    {
                        CloseHandle(this.hThread);
                        this.hThread = IntPtr.Zero;
                    }
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal sealed class STARTUPINFO
        {
            public int cb;
            public IntPtr lpReserved;
            public IntPtr lpDesktop;
            public IntPtr lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public SafeFileHandle hStdInput;
            public SafeFileHandle hStdOutput;
            public SafeFileHandle hStdError;

            public STARTUPINFO()
            {
                this.lpReserved = IntPtr.Zero;
                this.lpDesktop = IntPtr.Zero;
                this.lpTitle = IntPtr.Zero;
                this.lpReserved2 = IntPtr.Zero;
                this.hStdInput = new SafeFileHandle(IntPtr.Zero, false);
                this.hStdOutput = new SafeFileHandle(IntPtr.Zero, false);
                this.hStdError = new SafeFileHandle(IntPtr.Zero, false);
                this.cb = Marshal.SizeOf(this);
            }

            public void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if ((this.hStdInput != null) && !this.hStdInput.IsInvalid)
                    {
                        this.hStdInput.Dispose();
                        this.hStdInput = null;
                    }

                    if ((this.hStdOutput != null) && !this.hStdOutput.IsInvalid)
                    {
                        this.hStdOutput.Dispose();
                        this.hStdOutput = null;
                    }

                    if ((this.hStdError != null) && !this.hStdError.IsInvalid)
                    {
                        this.hStdError.Dispose();
                        this.hStdError = null;
                    }
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal sealed class SECURITY_ATTRIBUTES
        {
            public int nLength;
            public SafeLocalMemHandle lpSecurityDescriptor;
            public bool bInheritHandle;

            public SECURITY_ATTRIBUTES()
            {
                this.nLength = 12;
                this.bInheritHandle = true;
                this.lpSecurityDescriptor = new SafeLocalMemHandle(IntPtr.Zero, true);
            }
        }

        [DllImport(PinvokeDllNames.CreateProcessDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CreateProcess(
            [MarshalAs(UnmanagedType.LPWStr)] string lpApplicationName,
            [MarshalAs(UnmanagedType.LPWStr)] string lpCommandLine,
            SECURITY_ATTRIBUTES lpProcessAttributes,
            SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            int dwCreationFlags,
            IntPtr lpEnvironment,
            [MarshalAs(UnmanagedType.LPWStr)] string lpCurrentDirectory,
            STARTUPINFO lpStartupInfo,
            PROCESS_INFORMATION lpProcessInformation);

        [DllImport(PinvokeDllNames.ResumeThreadDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint ResumeThread(IntPtr threadHandle);

        internal static readonly uint RESUME_THREAD_FAILED = System.UInt32.MaxValue; // (DWORD)-1

#endif

        #endregion
    }
}
