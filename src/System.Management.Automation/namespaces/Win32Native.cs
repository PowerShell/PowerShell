
// NOTE: A vast majority of this code was copied from BCL in
// Namespace: Microsoft.Win32
//
/*
 * Notes to PInvoke users:  Getting the syntax exactly correct is crucial, and
 * more than a little confusing.  Here's some guidelines.
 *
 * For handles, you should use a SafeHandle subclass specific to your handle
 * type.
*/

namespace Microsoft.PowerShell.Commands.Internal
{
    using System;
    using System.Security;
    using System.Text;
    using System.Runtime.InteropServices;
    using Microsoft.Win32;
    using System.Runtime.Versioning;
    using System.Management.Automation;
    using System.Globalization;
    using System.Diagnostics.CodeAnalysis;

    using BOOL = System.Int32;
    using DWORD = System.UInt32;
    using ULONG = System.UInt32;

#if CORECLR
    // Use stubs for SuppressUnmanagedCodeSecurityAttribute and ReliabilityContractAttribute
    using Microsoft.PowerShell.CoreClr.Stubs;
#else
    using System.Runtime.ConstrainedExecution;
#endif

    /**
     * Win32 encapsulation for MSCORLIB.
     */
    // Remove the default demands for all N/Direct methods with this
    // global declaration on the class.
    //
    [SuppressUnmanagedCodeSecurityAttribute()]
    internal static class Win32Native
    {
        #region Integer Const

        internal const int ERROR_INSUFFICIENT_BUFFER = 0x7A;

        #endregion Integer Const

        #region Enum

        internal enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin
        }

        internal enum SID_NAME_USE
        {
            SidTypeUser = 1,
            SidTypeGroup,
            SidTypeDomain,
            SidTypeAlias,
            SidTypeWellKnownGroup,
            SidTypeDeletedAccount,
            SidTypeInvalid,
            SidTypeUnknown,
            SidTypeComputer
        }

        #endregion Enum

        #region Struct

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct SID_AND_ATTRIBUTES
        {
            internal IntPtr Sid;
            internal uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct TOKEN_USER
        {
            internal SID_AND_ATTRIBUTES User;
        }

        #endregion Struct 

        #region PInvoke methods 

        /// <summary>
        /// The LookupAccountSid function accepts a security identifier (SID) as input. It retrieves the name 
        /// of the account for this SID and the name of the first domain on which this SID is found.
        /// </summary>
        /// <param name="lpSystemName"></param>
        /// <param name="sid"></param>
        /// <param name="lpName"></param>
        /// <param name="cchName"></param>
        /// <param name="referencedDomainName"></param>
        /// <param name="cchReferencedDomainName"></param>
        /// <param name="peUse"></param>
        /// <returns></returns>
        [DllImport(PinvokeDllNames.LookupAccountSidDllName, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        [ResourceExposure(ResourceScope.Machine)]
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool LookupAccountSid(string lpSystemName,
                                                     IntPtr sid,
                                                     StringBuilder lpName,
                                                     ref int cchName,
                                                     StringBuilder referencedDomainName,
                                                     ref int cchReferencedDomainName,
                                                     out SID_NAME_USE peUse);

        [DllImport(PinvokeDllNames.CloseHandleDllName, SetLastError = true)]
        [ResourceExposure(ResourceScope.Machine)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        internal static extern bool CloseHandle(IntPtr handle);

        /// <summary>
        /// Retrieves the current process token.
        /// </summary>
        /// <param name="processHandle">process handle</param>
        /// <param name="desiredAccess">token access</param>
        /// <param name="tokenHandle">process token</param>
        /// <returns>The current process token.</returns>
        [DllImport(PinvokeDllNames.OpenProcessTokenDllName, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        [ResourceExposure(ResourceScope.Machine)]
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool OpenProcessToken(SafeHandle processHandle, uint desiredAccess, out IntPtr tokenHandle);

        /// <summary>
        /// The GetTokenInformation function retrieves a specified type of information about an access token. 
        /// The calling process must have appropriate access rights to obtain the information.
        /// </summary>
        /// <param name="tokenHandle"></param>
        /// <param name="tokenInformationClass"></param>
        /// <param name="tokenInformation"></param>
        /// <param name="tokenInformationLength"></param>
        /// <param name="returnLength"></param>
        /// <returns></returns>
        [DllImport(PinvokeDllNames.GetTokenInformationDllName, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        [ResourceExposure(ResourceScope.Machine)]
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetTokenInformation(IntPtr tokenHandle,
                                                        TOKEN_INFORMATION_CLASS tokenInformationClass,
                                                        IntPtr tokenInformation,
                                                        int tokenInformationLength,
                                                        out int returnLength);

        #endregion PInvoke Methods

        internal enum SECURITY_IMPERSONATION_LEVEL : short
        {
            Anonymous = 0,
            Identification = 0x1,
            Impersonation = 0x2,
            Delegation = 0x4
        }

        // Security Quality of Service flags
        internal const int SECURITY_ANONYMOUS = ((int)SECURITY_IMPERSONATION_LEVEL.Anonymous << 16);
        internal const int SECURITY_SQOS_PRESENT = 0x00100000;

#if !CORECLR // Only enable/port what is needed by CORE CLR. 

        private const string resBaseName = "RegistryProviderStrings";
        internal const int KEY_QUERY_VALUE = 0x0001;
        internal const int KEY_SET_VALUE = 0x0002;
        internal const int KEY_CREATE_SUB_KEY = 0x0004;
        internal const int KEY_ENUMERATE_SUB_KEYS = 0x0008;
        internal const int KEY_NOTIFY = 0x0010;
        internal const int KEY_CREATE_LINK = 0x0020;
        internal const int KEY_READ = ((STANDARD_RIGHTS_READ |
                                                           KEY_QUERY_VALUE |
                                                           KEY_ENUMERATE_SUB_KEYS |
                                                           KEY_NOTIFY)
                                                          &
                                                          (~SYNCHRONIZE));

        internal const int KEY_WRITE = ((STANDARD_RIGHTS_WRITE |
                                                           KEY_SET_VALUE |
                                                           KEY_CREATE_SUB_KEY)
                                                          &
                                                          (~SYNCHRONIZE));
        internal const int KEY_WOW64_64KEY = 0x100;
        internal const int KEY_WOW64_32KEY = 0x200;

        internal const int REG_NONE = 0;     // No value type
        internal const int REG_SZ = 1;     // Unicode nul terminated string
        internal const int REG_EXPAND_SZ = 2;     // Unicode nul terminated string
        // (with environment variable references)
        internal const int REG_BINARY = 3;     // Free form binary
        internal const int REG_DWORD = 4;     // 32-bit number
        internal const int REG_DWORD_LITTLE_ENDIAN = 4;     // 32-bit number (same as REG_DWORD)
        internal const int REG_DWORD_BIG_ENDIAN = 5;     // 32-bit number
        internal const int REG_LINK = 6;     // Symbolic Link (unicode)
        internal const int REG_MULTI_SZ = 7;     // Multiple Unicode strings
        internal const int REG_RESOURCE_LIST = 8;     // Resource list in the resource map
        internal const int REG_FULL_RESOURCE_DESCRIPTOR = 9;   // Resource list in the hardware description
        internal const int REG_RESOURCE_REQUIREMENTS_LIST = 10;
        internal const int REG_QWORD = 11;    // 64-bit number

        internal const int HWND_BROADCAST = 0xffff;
        internal const int WM_SETTINGCHANGE = 0x001A;

        // CryptProtectMemory and CryptUnprotectMemory.
        internal const uint CRYPTPROTECTMEMORY_BLOCK_SIZE = 16;
        internal const uint CRYPTPROTECTMEMORY_SAME_PROCESS = 0x00;
        internal const uint CRYPTPROTECTMEMORY_CROSS_PROCESS = 0x01;
        internal const uint CRYPTPROTECTMEMORY_SAME_LOGON = 0x02;

        // Access Control library.
        internal const string MICROSOFT_KERBEROS_NAME = "Kerberos";
        internal const uint ANONYMOUS_LOGON_LUID = 0x3e6;

        internal const int SECURITY_ANONYMOUS_LOGON_RID = 0x00000007;
        internal const int SECURITY_AUTHENTICATED_USER_RID = 0x0000000B;
        internal const int SECURITY_LOCAL_SYSTEM_RID = 0x00000012;
        internal const int SECURITY_BUILTIN_DOMAIN_RID = 0x00000020;
        internal const int DOMAIN_USER_RID_GUEST = 0x000001F5;

        internal const uint SE_GROUP_MANDATORY = 0x00000001;
        internal const uint SE_GROUP_ENABLED_BY_DEFAULT = 0x00000002;
        internal const uint SE_GROUP_ENABLED = 0x00000004;
        internal const uint SE_GROUP_OWNER = 0x00000008;
        internal const uint SE_GROUP_USE_FOR_DENY_ONLY = 0x00000010;
        internal const uint SE_GROUP_LOGON_ID = 0xC0000000;
        internal const uint SE_GROUP_RESOURCE = 0x20000000;

        internal const uint DUPLICATE_CLOSE_SOURCE = 0x00000001;
        internal const uint DUPLICATE_SAME_ACCESS = 0x00000002;
        internal const uint DUPLICATE_SAME_ATTRIBUTES = 0x00000004;

        // Win32 ACL-related constants:
        internal const int READ_CONTROL = 0x00020000;
        internal const int SYNCHRONIZE = 0x00100000;

        internal const int STANDARD_RIGHTS_READ = READ_CONTROL;
        internal const int STANDARD_RIGHTS_WRITE = READ_CONTROL;

        // STANDARD_RIGHTS_REQUIRED  (0x000F0000L)
        // SEMAPHORE_ALL_ACCESS          (STANDARD_RIGHTS_REQUIRED|SYNCHRONIZE|0x3) 

        // SEMAPHORE and Event both use 0x0002
        // MUTEX uses 0x001 (MUTANT_QUERY_STATE)

        // Note that you may need to specify the SYNCHRONIZE bit as well
        // to be able to open a synchronization primitive.
        internal const int SEMAPHORE_MODIFY_STATE = 0x00000002;
        internal const int EVENT_MODIFY_STATE = 0x00000002;
        internal const int MUTEX_MODIFY_STATE = 0x00000001;
        internal const int MUTEX_ALL_ACCESS = 0x001F0001;


        internal const int LMEM_FIXED = 0x0000;
        internal const int LMEM_ZEROINIT = 0x0040;
        internal const int LPTR = (LMEM_FIXED | LMEM_ZEROINIT);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal class OSVERSIONINFO
        {
            internal OSVERSIONINFO()
            {
                OSVersionInfoSize = (int)Marshal.SizeOf(this);
            }

            // The OSVersionInfoSize field must be set to Marshal.SizeOf(this)
            internal int OSVersionInfoSize = 0;
            internal int MajorVersion = 0;
            internal int MinorVersion = 0;
            internal int BuildNumber = 0;
            internal int PlatformId = 0;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal String CSDVersion = null;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal class OSVERSIONINFOEX
        {
            public OSVERSIONINFOEX()
            {
                OSVersionInfoSize = (int)Marshal.SizeOf(this);
            }

            // The OSVersionInfoSize field must be set to Marshal.SizeOf(this)
            internal int OSVersionInfoSize = 0;
            internal int MajorVersion = 0;
            internal int MinorVersion = 0;
            internal int BuildNumber = 0;
            internal int PlatformId = 0;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string CSDVersion = null;
            internal ushort ServicePackMajor = 0;
            internal ushort ServicePackMinor = 0;
            internal short SuiteMask = 0;
            internal byte ProductType = 0;
            internal byte Reserved = 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SYSTEM_INFO
        {
            internal int dwOemId;    // This is a union of a DWORD and a struct containing 2 WORDs.
            internal int dwPageSize;
            internal IntPtr lpMinimumApplicationAddress;
            internal IntPtr lpMaximumApplicationAddress;
            internal IntPtr dwActiveProcessorMask;
            internal int dwNumberOfProcessors;
            internal int dwProcessorType;
            internal int dwAllocationGranularity;
            internal short wProcessorLevel;
            internal short wProcessorRevision;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class SECURITY_ATTRIBUTES
        {
            internal int nLength = 0;
            internal unsafe byte* pSecurityDescriptor = null;
            internal int bInheritHandle = 0;
        }

        [StructLayout(LayoutKind.Sequential), Serializable]
        internal struct WIN32_FILE_ATTRIBUTE_DATA
        {
            internal int fileAttributes;
            internal uint ftCreationTimeLow;
            internal uint ftCreationTimeHigh;
            internal uint ftLastAccessTimeLow;
            internal uint ftLastAccessTimeHigh;
            internal uint ftLastWriteTimeLow;
            internal uint ftLastWriteTimeHigh;
            internal int fileSizeHigh;
            internal int fileSizeLow;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct FILE_TIME
        {
            public FILE_TIME(long fileTime)
            {
                ftTimeLow = (uint)fileTime;
                ftTimeHigh = (uint)(fileTime >> 32);
            }

            public long ToTicks()
            {
                return ((long)ftTimeHigh << 32) + ftTimeLow;
            }

            internal uint ftTimeLow;
            internal uint ftTimeHigh;
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct KERB_S4U_LOGON
        {
            internal uint MessageType;
            internal uint Flags;
            internal UNICODE_INTPTR_STRING ClientUpn;   // REQUIRED: UPN for client
            internal UNICODE_INTPTR_STRING ClientRealm; // Optional: Client Realm, if known
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct LSA_OBJECT_ATTRIBUTES
        {
            internal int Length;
            internal IntPtr RootDirectory;
            internal IntPtr ObjectName;
            internal int Attributes;
            internal IntPtr SecurityDescriptor;
            internal IntPtr SecurityQualityOfService;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct UNICODE_STRING
        {
            internal ushort Length;
            internal ushort MaximumLength;
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string Buffer;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct UNICODE_INTPTR_STRING
        {
            internal UNICODE_INTPTR_STRING(int length, int maximumLength, IntPtr buffer)
            {
                this.Length = (ushort)length;
                this.MaxLength = (ushort)maximumLength;
                this.Buffer = buffer;
            }
            internal ushort Length;
            internal ushort MaxLength;
            internal IntPtr Buffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct LSA_TRANSLATED_NAME
        {
            internal int Use;
            internal UNICODE_INTPTR_STRING Name;
            internal int DomainIndex;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct LSA_TRANSLATED_SID
        {
            internal int Use;
            internal uint Rid;
            internal int DomainIndex;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct LSA_TRANSLATED_SID2
        {
            internal int Use;
            internal IntPtr Sid;
            internal int DomainIndex;
            private uint Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct LSA_TRUST_INFORMATION
        {
            internal UNICODE_INTPTR_STRING Name;
            internal IntPtr Sid;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct LSA_REFERENCED_DOMAIN_LIST
        {
            internal int Entries;
            internal IntPtr Domains;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct QUOTA_LIMITS
        {
            internal IntPtr PagedPoolLimit;
            internal IntPtr NonPagedPoolLimit;
            internal IntPtr MinimumWorkingSetSize;
            internal IntPtr MaximumWorkingSetSize;
            internal IntPtr PagefileLimit;
            internal IntPtr TimeLimit;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct TOKEN_GROUPS
        {
            internal uint GroupCount;
            internal SID_AND_ATTRIBUTES Groups; // SID_AND_ATTRIBUTES Groups[ANYSIZE_ARRAY];
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class MEMORYSTATUSEX
        {
            internal MEMORYSTATUSEX()
            {
                length = (int)Marshal.SizeOf(this);
            }

            // The length field must be set to the size of this data structure.
            internal int length;
            internal int memoryLoad;
            internal ulong totalPhys;
            internal ulong availPhys;
            internal ulong totalPageFile;
            internal ulong availPageFile;
            internal ulong totalVirtual;
            internal ulong availVirtual;
            internal ulong availExtendedVirtual;
        }

        // Use only on Win9x
        [StructLayout(LayoutKind.Sequential)]
        internal class MEMORYSTATUS
        {
            internal MEMORYSTATUS()
            {
                length = (int)Marshal.SizeOf(this);
            }

            // The length field must be set to the size of this data structure.
            internal int length;
            internal int memoryLoad;
            internal uint totalPhys;
            internal uint availPhys;
            internal uint totalPageFile;
            internal uint availPageFile;
            internal uint totalVirtual;
            internal uint availVirtual;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct MEMORY_BASIC_INFORMATION
        {
            internal UIntPtr BaseAddress;
            internal UIntPtr AllocationBase;
            internal uint AllocationProtect;
            internal UIntPtr RegionSize;
            internal uint State;
            internal uint Protect;
            internal uint Type;
        }

        internal const String KERNEL32 = "kernel32.dll";
        internal const String USER32 = "user32.dll";
        internal const String ADVAPI32 = "advapi32.dll";
        internal const String OLE32 = "ole32.dll";
        internal const String OLEAUT32 = "oleaut32.dll";
        internal const String SHFOLDER = "shfolder.dll";
        internal const String SHIM = "mscoree.dll";
        internal const String CRYPT32 = "crypt32.dll";
        internal const String SECUR32 = "secur32.dll";
        internal const String MSCORWKS = "mscorwks.dll";


        internal const String LSTRCPY = "lstrcpy";
        internal const String LSTRCPYN = "lstrcpyn";
        internal const String LSTRLEN = "lstrlen";
        internal const String LSTRLENA = "lstrlenA";
        internal const String LSTRLENW = "lstrlenW";
        internal const String MOVEMEMORY = "RtlMoveMemory";


        // From WinBase.h
        internal const int SEM_FAILCRITICALERRORS = 1;


        [DllImport(
             ADVAPI32,
             EntryPoint = "GetSecurityDescriptorLength",
             CallingConvention = CallingConvention.Winapi,
             SetLastError = true,
             CharSet = CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern DWORD GetSecurityDescriptorLength(
            IntPtr byteArray);

        [DllImport(
             ADVAPI32,
             EntryPoint = "GetSecurityInfo",
             CallingConvention = CallingConvention.Winapi,
             SetLastError = true,
             CharSet = CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern DWORD GetSecurityInfoByHandle(
            SafeHandle handle,
            DWORD objectType,
            DWORD securityInformation,
            out IntPtr sidOwner,
            out IntPtr sidGroup,
            out IntPtr dacl,
            out IntPtr sacl,
            out IntPtr securityDescriptor);

        [DllImport(
             ADVAPI32,
             EntryPoint = "SetSecurityInfo",
             CallingConvention = CallingConvention.Winapi,
             SetLastError = true,
             CharSet = CharSet.Unicode)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern DWORD SetSecurityInfoByHandle(
            SafeHandle handle,
            DWORD objectType,
            DWORD securityInformation,
            byte[] owner,
            byte[] group,
            byte[] dacl,
            byte[] sacl);


        [DllImport(KERNEL32, SetLastError = true)]
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern IntPtr LocalFree(IntPtr handle);




        internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);  // WinBase.h
        internal static readonly IntPtr NULL = IntPtr.Zero;

        // Error codes from WinError.h
        internal const int ERROR_SUCCESS = 0x0;
        internal const int ERROR_INVALID_FUNCTION = 0x1;
        internal const int ERROR_FILE_NOT_FOUND = 0x2;
        internal const int ERROR_PATH_NOT_FOUND = 0x3;
        internal const int ERROR_ACCESS_DENIED = 0x5;
        internal const int ERROR_INVALID_HANDLE = 0x6;
        internal const int ERROR_NOT_ENOUGH_MEMORY = 0x8;
        internal const int ERROR_INVALID_DATA = 0xd;
        internal const int ERROR_INVALID_DRIVE = 0xf;
        internal const int ERROR_NO_MORE_FILES = 0x12;
        internal const int ERROR_NOT_READY = 0x15;
        internal const int ERROR_BAD_LENGTH = 0x18;
        internal const int ERROR_SHARING_VIOLATION = 0x20;
        internal const int ERROR_NOT_SUPPORTED = 0x32;
        internal const int ERROR_FILE_EXISTS = 0x50;
        internal const int ERROR_INVALID_PARAMETER = 0x57;
        internal const int ERROR_CALL_NOT_IMPLEMENTED = 0x78;
        internal const int ERROR_INVALID_NAME = 0x7B;
        internal const int ERROR_BAD_PATHNAME = 0xA1;
        internal const int ERROR_ALREADY_EXISTS = 0xB7;
        internal const int ERROR_ENVVAR_NOT_FOUND = 0xCB;
        internal const int ERROR_FILENAME_EXCED_RANGE = 0xCE;  // filename too long.
        internal const int ERROR_NO_DATA = 0xE8;
        internal const int ERROR_PIPE_NOT_CONNECTED = 0xE9;
        internal const int ERROR_MORE_DATA = 0xEA;
        internal const int ERROR_OPERATION_ABORTED = 0x3E3;  // 995; For IO Cancellation
        internal const int ERROR_NO_TOKEN = 0x3f0;
        internal const int ERROR_DLL_INIT_FAILED = 0x45A;
        internal const int ERROR_NON_ACCOUNT_SID = 0x4E9;
        internal const int ERROR_NOT_ALL_ASSIGNED = 0x514;
        internal const int ERROR_UNKNOWN_REVISION = 0x519;
        internal const int ERROR_INVALID_OWNER = 0x51B;
        internal const int ERROR_INVALID_PRIMARY_GROUP = 0x51C;
        internal const int ERROR_NO_SUCH_PRIVILEGE = 0x521;
        internal const int ERROR_PRIVILEGE_NOT_HELD = 0x522;
        internal const int ERROR_NONE_MAPPED = 0x534;
        internal const int ERROR_INVALID_ACL = 0x538;
        internal const int ERROR_INVALID_SID = 0x539;
        internal const int ERROR_INVALID_SECURITY_DESCR = 0x53A;
        internal const int ERROR_BAD_IMPERSONATION_LEVEL = 0x542;
        internal const int ERROR_CANT_OPEN_ANONYMOUS = 0x543;
        internal const int ERROR_NO_SECURITY_ON_OBJECT = 0x546;
        internal const int ERROR_TRUSTED_RELATIONSHIP_FAILURE = 0x6FD;

        // These two values come from comments in WinError.h
        internal const int ERROR_MIN_KTM_CODE = 6700;  // 0x1A2C
        internal const int ERROR_INVALID_TRANSACTION = 6700;  // 0x1A2C
        internal const int ERROR_MAX_KTM_CODE = 6799;  // 0x1A8F;

        // Error codes from ntstatus.h
        internal const uint STATUS_SUCCESS = 0x00000000;
        internal const uint STATUS_SOME_NOT_MAPPED = 0x00000107;
        internal const uint STATUS_NO_MEMORY = 0xC0000017;
        internal const uint STATUS_OBJECT_NAME_NOT_FOUND = 0xC0000034;
        internal const uint STATUS_NONE_MAPPED = 0xC0000073;
        internal const uint STATUS_INSUFFICIENT_RESOURCES = 0xC000009A;
        internal const uint STATUS_ACCESS_DENIED = 0xC0000022;

        internal const int INVALID_FILE_SIZE = -1;

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern int RegConnectRegistry(String machineName,
                    SafeRegistryHandle key, out SafeRegistryHandle result);

        // Note: RegCreateKeyEx won't set the last error on failure - it returns
        // an error code if it fails.
        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        [ResourceExposure(ResourceScope.Machine)]
        internal static extern int RegCreateKeyEx(SafeRegistryHandle hKey, String lpSubKey,
                    int Reserved, String lpClass, int dwOptions,
                    int samDesigner, SECURITY_ATTRIBUTES lpSecurityAttributes,
                    out SafeRegistryHandle hkResult, out int lpdwDisposition);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        [ResourceExposure(ResourceScope.Machine)]
        // Suppressed because there is no way for arbitrary data to be passed.
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        internal static extern int RegDeleteKey(SafeRegistryHandle hKey, String lpSubKey);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        [ResourceExposure(ResourceScope.Machine)]
        // Suppressed because there is no way for arbitrary data to be passed.
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        internal static extern int RegDeleteKeyTransacted(SafeRegistryHandle hKey, String lpSubKey, int samDesired,
                                DWORD reserved, SafeTransactionHandle hTransaction, IntPtr pExtendedParameter);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        [ResourceExposure(ResourceScope.Machine)]
        // Suppressed because there is no way for arbitrary data to be passed.
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        internal static extern int RegDeleteValue(SafeRegistryHandle hKey, String lpValueName);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        [ResourceExposure(ResourceScope.None)]
        // Suppressed because there is no way for arbitrary data to be passed.
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        internal static extern int RegEnumKeyEx(SafeRegistryHandle hKey, int dwIndex,
                    StringBuilder lpName, out int lpcbName, int[] lpReserved,
                    StringBuilder lpClass, int[] lpcbClass,
                    long[] lpftLastWriteTime);

        [DllImport(PinvokeDllNames.RegEnumValueDllName, CharSet = CharSet.Auto, BestFitMapping = false)]
        [ResourceExposure(ResourceScope.None)]
        // Suppressed because there is no way for arbitrary data to be passed.
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        internal static extern int RegEnumValue(SafeRegistryHandle hKey, int dwIndex,
                    StringBuilder lpValueName, ref int lpcbValueName,
                    IntPtr lpReserved_MustBeZero, int[] lpType, byte[] lpData,
                    int[] lpcbData);

        [DllImport(ADVAPI32)]
        [ResourceExposure(ResourceScope.None)]
        // Suppressed because there is no way for arbitrary data to be passed.
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        internal static extern int RegFlushKey(SafeRegistryHandle hKey);

        [DllImport(PinvokeDllNames.RegOpenKeyExDllName, CharSet = CharSet.Auto, BestFitMapping = false)]
        [ResourceExposure(ResourceScope.Machine)]
        // Suppressed because there is no way for arbitrary data to be passed.
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        internal static extern int RegOpenKeyEx(SafeRegistryHandle hKey, String lpSubKey,
                    int ulOptions, int samDesired, out SafeRegistryHandle hkResult);

        [DllImport(PinvokeDllNames.RegOpenKeyTransactedDllName, CharSet = CharSet.Auto, BestFitMapping = false)]
        [ResourceExposure(ResourceScope.Machine)]
        // Suppressed because there is no way for arbitrary data to be passed.
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        internal static extern int RegOpenKeyTransacted(SafeRegistryHandle hKey, String lpSubKey,
                    int ulOptions, int samDesired, out SafeRegistryHandle hkResult,
                    SafeTransactionHandle hTransaction, IntPtr pExtendedParameter);

        [DllImport(PinvokeDllNames.RegQueryInfoKeyDllName, CharSet = CharSet.Auto, BestFitMapping = false)]
        [ResourceExposure(ResourceScope.None)]
        // Suppressed because there is no way for arbitrary data to be passed.
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        internal static extern int RegQueryInfoKey(SafeRegistryHandle hKey, StringBuilder lpClass,
                    int[] lpcbClass, IntPtr lpReserved_MustBeZero, ref int lpcSubKeys,
                    int[] lpcbMaxSubKeyLen, int[] lpcbMaxClassLen,
                    ref int lpcValues, int[] lpcbMaxValueNameLen,
                    int[] lpcbMaxValueLen, int[] lpcbSecurityDescriptor,
                    int[] lpftLastWriteTime);

        [DllImport(PinvokeDllNames.RegQueryValueExDllName, CharSet = CharSet.Auto, BestFitMapping = false)]
        [ResourceExposure(ResourceScope.None)]
        // Suppressed because there is no way for arbitrary data to be passed.
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int[] lpReserved, ref int lpType, [Out] byte[] lpData,
                    ref int lpcbData);

        [DllImport(PinvokeDllNames.RegQueryValueExDllName, CharSet = CharSet.Auto, BestFitMapping = false)]
        [ResourceExposure(ResourceScope.None)]
        // Suppressed because there is no way for arbitrary data to be passed.
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int[] lpReserved, ref int lpType, ref int lpData,
                    ref int lpcbData);

        [DllImport(PinvokeDllNames.RegQueryValueExDllName, CharSet = CharSet.Auto, BestFitMapping = false)]
        [ResourceExposure(ResourceScope.None)]
        // Suppressed because there is no way for arbitrary data to be passed.
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int[] lpReserved, ref int lpType, ref long lpData,
                    ref int lpcbData);

        [DllImport(PinvokeDllNames.RegQueryValueExDllName, CharSet = CharSet.Auto, BestFitMapping = false)]
        [ResourceExposure(ResourceScope.None)]
        // Suppressed because there is no way for arbitrary data to be passed.
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName,
                     int[] lpReserved, ref int lpType, [Out] char[] lpData,
                     ref int lpcbData);

        [DllImport(PinvokeDllNames.RegQueryValueExDllName, CharSet = CharSet.Auto, BestFitMapping = false)]
        [ResourceExposure(ResourceScope.None)]
        // Suppressed because there is no way for arbitrary data to be passed.
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int[] lpReserved, ref int lpType, StringBuilder lpData,
                    ref int lpcbData);

        [DllImport(PinvokeDllNames.RegSetValueExDllName, CharSet = CharSet.Auto, BestFitMapping = false)]
        [ResourceExposure(ResourceScope.None)]
        // Suppressed because there is no way for arbitrary data to be passed.
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int Reserved, RegistryValueKind dwType, byte[] lpData, int cbData);

        [DllImport(PinvokeDllNames.RegSetValueExDllName, CharSet = CharSet.Auto, BestFitMapping = false)]
        [ResourceExposure(ResourceScope.None)]
        // Suppressed because there is no way for arbitrary data to be passed.
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int Reserved, RegistryValueKind dwType, ref int lpData, int cbData);

        [DllImport(PinvokeDllNames.RegSetValueExDllName, CharSet = CharSet.Auto, BestFitMapping = false)]
        [ResourceExposure(ResourceScope.None)]
        // Suppressed because there is no way for arbitrary data to be passed.
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int Reserved, RegistryValueKind dwType, ref long lpData, int cbData);

        [DllImport(PinvokeDllNames.RegSetValueExDllName, CharSet = CharSet.Auto, BestFitMapping = false)]
        [ResourceExposure(ResourceScope.None)]
        // Suppressed because there is no way for arbitrary data to be passed.
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int Reserved, RegistryValueKind dwType, String lpData, int cbData);

        [DllImport(PinvokeDllNames.RegCreateKeyTransactedDllName, CharSet = CharSet.Auto, BestFitMapping = false)]
        [ResourceExposure(ResourceScope.Machine)]
        // Suppressed because there is no way for arbitrary data to be passed.
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        internal static extern int RegCreateKeyTransacted(SafeRegistryHandle hKey, String lpSubKey,
                    int Reserved, String lpClass, int dwOptions,
                    int samDesigner, SECURITY_ATTRIBUTES lpSecurityAttributes,
                    out SafeRegistryHandle hkResult, out int lpdwDisposition,
                    SafeTransactionHandle hTransaction, IntPtr pExtendedParameter);



        private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        private const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;

        [DllImport(KERNEL32, CharSet = CharSet.Unicode, BestFitMapping = true)]
        [ResourceExposure(ResourceScope.None)]
        // Suppressed because there is no way for arbitrary data to be passed.
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        internal static extern int FormatMessage(int dwFlags, IntPtr lpSource,
                    int dwMessageId, int dwLanguageId, StringBuilder lpBuffer,
                    int nSize, IntPtr va_list_arguments);

        // Gets an error message for a Win32 error code.
        internal static String GetMessage(int errorCode)
        {
            StringBuilder sb = new StringBuilder(512);
            int result = Win32Native.FormatMessage(FORMAT_MESSAGE_IGNORE_INSERTS |
                FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ARGUMENT_ARRAY,
                Win32Native.NULL, errorCode, 0, sb, sb.Capacity, Win32Native.NULL);
            if (result != 0)
            {
                // result is the # of characters copied to the StringBuilder on NT,
                // but on Win9x, it appears to be the number of MBCS bytes.
                // Just give up and return the String as-is...
                String s = sb.ToString();
                return s;
            }
            else
            {
                string resourceTemplate = RegistryProviderStrings.UnknownError_Num;
                return String.Format(CultureInfo.CurrentCulture, resourceTemplate, errorCode.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        [DllImport(KERNEL32, CharSet = CharSet.Auto, EntryPoint = LSTRLEN)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern int lstrlen(sbyte[] ptr);

        [DllImport(KERNEL32, CharSet = CharSet.Auto, EntryPoint = LSTRLEN)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern int lstrlen(IntPtr ptr);

        [DllImport(KERNEL32, CharSet = CharSet.Ansi, EntryPoint = LSTRLENA)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern int lstrlenA(IntPtr ptr);

        [DllImport(KERNEL32, CharSet = CharSet.Unicode, EntryPoint = LSTRLENW)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern int lstrlenW(IntPtr ptr);

        [DllImport(KERNEL32)]
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        internal static extern UIntPtr VirtualQuery(UIntPtr lpAddress, ref MEMORY_BASIC_INFORMATION lpBuffer, UIntPtr dwLength);

        [DllImport(KERNEL32)]
        internal static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

        internal static readonly uint PAGE_SIZE;

        static Win32Native()
        {
            SYSTEM_INFO systemInfo;
            GetSystemInfo(out systemInfo);
            PAGE_SIZE = (uint)systemInfo.dwPageSize;
        }

#endif
    }


    internal sealed class SafeProcessHandle : SafeHandle
    {
        internal SafeProcessHandle() : base(IntPtr.Zero, true) { }

        internal SafeProcessHandle(IntPtr existingHandle)
            : base(IntPtr.Zero, true)
        {
            SetHandle(existingHandle);
        }

        protected override bool ReleaseHandle()
        {
            return base.IsClosed ? true : Win32Native.CloseHandle(base.handle);
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero || handle == new IntPtr(-1); }
        }
    }
}
