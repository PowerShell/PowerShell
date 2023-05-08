// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Management.Automation.SecurityAccountsManager.Native
{
    #region Enums
    internal enum POLICY_INFORMATION_CLASS
    {
        PolicyAuditLogInformation        = 1,
        PolicyAuditEventsInformation,
        PolicyPrimaryDomainInformation,
        PolicyPdAccountInformation,
        PolicyAccountDomainInformation,
        PolicyLsaServerRoleInformation,
        PolicyReplicaSourceInformation,
        PolicyDefaultQuotaInformation,
        PolicyModificationInformation,
        PolicyAuditFullSetInformation,
        PolicyAuditFullQueryInformation,
        PolicyDnsDomainInformation
    }

    [Flags]
    internal enum LSA_AccessPolicy : long
    {
        POLICY_VIEW_LOCAL_INFORMATION   = 0x00000001L,
        POLICY_VIEW_AUDIT_INFORMATION   = 0x00000002L,
        POLICY_GET_PRIVATE_INFORMATION  = 0x00000004L,
        POLICY_TRUST_ADMIN              = 0x00000008L,
        POLICY_CREATE_ACCOUNT           = 0x00000010L,
        POLICY_CREATE_SECRET            = 0x00000020L,
        POLICY_CREATE_PRIVILEGE         = 0x00000040L,
        POLICY_SET_DEFAULT_QUOTA_LIMITS = 0x00000080L,
        POLICY_SET_AUDIT_REQUIREMENTS   = 0x00000100L,
        POLICY_AUDIT_LOG_ADMIN          = 0x00000200L,
        POLICY_SERVER_ADMIN             = 0x00000400L,
        POLICY_LOOKUP_NAMES             = 0x00000800L,
        POLICY_NOTIFICATION             = 0x00001000L
    }

    internal enum SID_NAME_USE
    {
        SidTypeUser             = 1,
        SidTypeGroup,
        SidTypeDomain,
        SidTypeAlias,
        SidTypeWellKnownGroup,
        SidTypeDeletedAccount,
        SidTypeInvalid,
        SidTypeUnknown,
        SidTypeComputer,
        SidTypeLabel
    }

    internal enum LSA_USER_ACCOUNT_TYPE
    {
        UnknownUserAccountType = 0,
        LocalUserAccountType,
        PrimaryDomainUserAccountType,
        ExternalDomainUserAccountType,
        LocalConnectedUserAccountType,  // Microsoft Account
        AADUserAccountType,
        InternetUserAccountType,        // Generic internet User (eg. if the SID supplied is MSA's internet SID)
        MSAUserAccountType      // !!! NOT YET IN THE ENUM SPECIFIED IN THE C API !!!

    }
    #endregion Enums

    #region Structures
    [StructLayout(LayoutKind.Sequential)]
    internal struct SECURITY_DESCRIPTOR
    {
        public byte Revision;
        public byte Sbz1;
        public UInt16 Control;  // SECURITY_DESCRIPTOR_CONTROL
        public IntPtr Owner;
        public IntPtr Group;
        public IntPtr Sacl;
        public IntPtr Dacl;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ACL
    {
        public byte AclRevision;
        public byte Sbz1;
        public UInt16 AclSize;
        public UInt16 AceCount;
        public UInt16 Sbz2;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct USER_INFO_1
    {
        public string name;
        public string password;
        public int password_age;
        public int priv;
        public string home_dir;
        public string comment;
        public uint flags;
        public string script_path;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct USER_INFO_1008
    {
        public uint flags;
    }

    /// <summary>
    /// The UNICODE_STRING structure is passed to a number of the SAM and LSA
    /// API functions. This adds cleanup and managed-string conversion behaviors.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct UNICODE_STRING
    {
        public UInt16 Length;
        public UInt16 MaximumLength;
        [MarshalAs(UnmanagedType.LPWStr)]
        private string buffer;

        public UNICODE_STRING(string s)
        {
            buffer = string.IsNullOrEmpty(s) ? string.Empty : s;
            Length = (UInt16)(2 * buffer.Length);
            MaximumLength = Length;
        }

        public override string ToString()
        {
            // UNICODE_STRING structures that were populated by unmanaged code
            // often have buffers that point to junk if Length = 0, or that
            // point to non-null-terminated strings, resulting in marshaled
            // String objects that have more characters than they should.
            return Length == 0 ? string.Empty
                               : buffer.Substring(0, Length / 2);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct OBJECT_ATTRIBUTES : IDisposable
    {
        public int Length;
        public IntPtr RootDirectory;
        public uint Attributes;
        public IntPtr SecurityDescriptor;
        public IntPtr SecurityQualityOfService;

        private IntPtr objectName;
        public UNICODE_STRING ObjectName;

        public void Dispose()
        {
            if (objectName != IntPtr.Zero)
            {
                Marshal.DestroyStructure<UNICODE_STRING>(objectName);
                Marshal.FreeHGlobal(objectName);
                objectName = IntPtr.Zero;
            }
        }
    }

// These structures are filled in by Marshalling, so fields will be initialized
// invisibly to the C# compiler, and some fields will not be used in C# code.
#pragma warning disable 0649, 0169
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    struct LARGE_INTEGER
    {
        [FieldOffset(0)]
        public Int64 QuadPart;
        [FieldOffset(0)]
        public UInt32 LowPart;
        [FieldOffset(4)]
        public Int32 HighPart;
    }
#pragma warning restore 0649, 0169
    #endregion Structures

    internal static class Win32
    {
        #region Constants
        //  The following are masks for the predefined standard access types
        internal const UInt32 DELETE                    = 0x00010000;
        internal const UInt32 READ_CONTROL              = 0x00020000;
        internal const UInt32 WRITE_DAC                 = 0x00040000;
        internal const UInt32 WRITE_OWNER               = 0x00080000;
        internal const UInt32 SYNCHRONIZE               = 0x00100000;

        internal const UInt32 STANDARD_RIGHTS_REQUIRED  = 0x000F0000;

        internal const UInt32 STANDARD_RIGHTS_READ      = READ_CONTROL;
        internal const UInt32 STANDARD_RIGHTS_WRITE     = READ_CONTROL;
        internal const UInt32 STANDARD_RIGHTS_EXECUTE   = READ_CONTROL;

        internal const UInt32 STANDARD_RIGHTS_ALL       = 0x001F0000;

        internal const UInt32 SPECIFIC_RIGHTS_ALL       = 0x0000FFFF;

        internal const UInt32 ACCESS_SYSTEM_SECURITY    = 0x01000000;

        internal const UInt32 MAXIMUM_ALLOWED           = 0x02000000;

        internal const UInt32 GENERIC_READ              = 0x80000000;
        internal const UInt32 GENERIC_WRITE             = 0x40000000;
        internal const UInt32 GENERIC_EXECUTE           = 0x20000000;
        internal const UInt32 GENERIC_ALL               = 0x10000000;

        // These constants control the behavior of the FormatMessage Windows API function
        internal const uint FORMAT_MESSAGE_ALLOCATE_BUFFER  = 0x00000100;
        internal const uint FORMAT_MESSAGE_IGNORE_INSERTS   = 0x00000200;
        internal const uint FORMAT_MESSAGE_FROM_SYSTEM      = 0x00001000;
        internal const uint FORMAT_MESSAGE_ARGUMENT_ARRAY   = 0x00002000;
        internal const uint FORMAT_MESSAGE_FROM_HMODULE     = 0x00000800;
        internal const uint FORMAT_MESSAGE_FROM_STRING      = 0x00000400;

        #region Win32 Error Codes
        //
        // MessageText:
        //
        // The operation completed successfully.
        //
        internal const Int32 ERROR_SUCCESS              = 0;
        internal const Int32 NO_ERROR                   = ERROR_SUCCESS;

        //
        // MessageId: ERROR_ACCESS_DENIED
        //
        // MessageText:
        //
        // Access is denied.
        //
        internal const int ERROR_ACCESS_DENIED          = 5;

        //
        // MessageId: ERROR_BAD_NETPATH
        //
        // MessageText:
        //
        // The network path was not found.
        //
        internal const int ERROR_BAD_NETPATH            = 53;

        //
        // MessageId: ERROR_NETWORK_ACCESS_DENIED
        //
        // MessageText:
        //
        // Network access is denied.
        //
        internal const int ERROR_NETWORK_ACCESS_DENIED  = 65;

        //
        // MessageId: ERROR_INVALID_PARAMETER
        //
        // MessageText:
        //
        // The parameter is incorrect.
        //
        internal const int ERROR_INVALID_PARAMETER      = 87;

        //
        // MessageText:
        //
        // The file name is too long.
        //
        internal const Int32 ERROR_BUFFER_OVERFLOW      = 111;

        //
        // MessageText:
        //
        // The data area passed to a system call is too small.
        //
        internal const Int32 ERROR_INSUFFICIENT_BUFFER  = 122;

        //
        // MessageId: ERROR_INVALID_LEVEL
        //
        // MessageText:
        //
        // The system call level is not correct.
        //
        internal const int ERROR_INVALID_LEVEL          = 124;

        //
        // MessageId: ERROR_INVALID_FLAGS
        //
        // MessageText:
        //
        // Invalid flags.
        //
        internal const Int32 ERROR_INVALID_FLAGS        = 1004;

        //
        // MessageId: ERROR_ILL_FORMED_PASSWORD
        //
        // MessageText:
        //
        // Unable to update the password. The value provided for the new password contains values that are not allowed in passwords.
        //
        internal const UInt32 ERROR_ILL_FORMED_PASSWORD = 1324;

        //
        // MessageId: ERROR_PASSWORD_RESTRICTION
        //
        // MessageText:
        //
        // Unable to update the password. The value provided for the new password does not meet the length, complexity, or history requirements of the domain.
        //
        internal const UInt32 ERROR_PASSWORD_RESTRICTION    = 1325;

        //
        // MessageText:
        //
        // No mapping between account names and security IDs was done.
        //
        internal const Int32 ERROR_NONE_MAPPED          = 1332;

        internal const int NERR_Success                 = 0;
        // NERR_BASE is the base of error codes from network utilities,
        // chosen to avoid conflict with system and redirector error codes.
        // 2100 is a value that has been assigned to us by system.
        internal const int NERR_BASE                    = 2100;

        internal const int NERR_BadPassword             = NERR_BASE + 103;  // The password parameter is invalid.
        internal const int NERR_UserNotFound            = NERR_BASE + 121;  // The user name could not be found.
        internal const int NERR_NotPrimary              = NERR_BASE + 126;  // This operation is only allowed on the primary domain controller of the domain.
        internal const int NERR_SpeGroupOp              = NERR_BASE + 134;  // This operation is not allowed on this special group.
        internal const int NERR_PasswordTooShort        = NERR_BASE + 145;  // The password does not meet the password policy requirements. Check the minimum password length, password complexity and password history requirements.
        internal const int NERR_InvalidComputer         = NERR_BASE + 251;  // This computer name is invalid.
        internal const int NERR_LastAdmin               = NERR_BASE + 352;  // This operation is not allowed on the last administrative account.
        #endregion Win32 Error Codes

        #region SECURITY_DESCRIPTOR Control Flags
        internal const UInt16 SE_DACL_PRESENT           = 0x0004;
        internal const UInt16 SE_SELF_RELATIVE          = 0x8000;
        #endregion SECURITY_DESCRIPTOR Control Flags

        #region SECURITY_INFORMATION Values
        internal const int DACL_SECURITY_INFORMATION    = 0x00000004;
        #endregion SECURITY_INFORMATION Values
        #endregion Constants

        #region Win32 Functions
        [DllImport(PInvokeDllNames.LookupAccountSidDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool LookupAccountSid(string systemName,
                                                     byte[] accountSid,
                                                     StringBuilder accountName,
                                                     ref Int32 nameLength,
                                                     StringBuilder domainName,
                                                     ref Int32 domainNameLength,
                                                     out SID_NAME_USE use);

        [DllImport(PInvokeDllNames.LookupAccountNameDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool LookupAccountName(string systemName,
                                                      string accountName,
                                                      [MarshalAs(UnmanagedType.LPArray)]
                                                      byte[] sid,
                                                      ref uint sidLength,
                                                      StringBuilder domainName,
                                                      ref uint domainNameLength,
                                                      out SID_NAME_USE peUse);

        [DllImport(PInvokeDllNames.GetSecurityDescriptorDaclDllName, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetSecurityDescriptorDacl(IntPtr pSecurityDescriptor,
                                                              [MarshalAs(UnmanagedType.Bool)]
                                                              out bool bDaclPresent,
                                                              out IntPtr pDacl,
                                                              [MarshalAs(UnmanagedType.Bool)]
                                                              out bool bDaclDefaulted);

        [DllImport(PInvokeDllNames.SetSecurityDescriptorDaclDllName, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetSecurityDescriptorDacl(IntPtr pSecurityDescriptor,
                                                              [MarshalAs(UnmanagedType.Bool)]
                                                              bool bDaclPresent,
                                                              IntPtr pDacl,
                                                              [MarshalAs(UnmanagedType.Bool)]
                                                              bool bDaclDefaulted);

        [DllImport(PInvokeDllNames.FormatMessageDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern uint FormatMessage(uint dwFlags,
                                                  IntPtr lpSource,
                                                  uint dwMessageId,
                                                  uint dwLanguageId,
                                                  [Out] StringBuilder lpBuffer,
                                                  uint nSize,
                                                  string[] Arguments);

        [DllImport("ntdll.dll")]
        internal static extern uint RtlNtStatusToDosError(uint ntStatus);
        #endregion Win32 Functions

        #region LSA Functions
        [DllImport(PInvokeDllNames.LsaOpenPolicyDllName, CharSet = CharSet.Unicode)]
        internal static extern UInt32 LsaOpenPolicy(ref UNICODE_STRING SystemName,
                                                    ref OBJECT_ATTRIBUTES ObjectAttributes,
                                                    uint DesiredAccess,
                                                    out IntPtr PolicyHandle);

        [DllImport(PInvokeDllNames.LsaQueryInformationPolicyDllName, CharSet = CharSet.Unicode)]
        internal static extern UInt32 LsaQueryInformationPolicy(IntPtr lsaHandle,
                                                                POLICY_INFORMATION_CLASS infoClass,
                                                                out IntPtr buffer);

        [DllImport(PInvokeDllNames.LsaFreeMemoryDllName)]
        internal static extern UInt32 LsaFreeMemory(IntPtr buffer);

        [DllImport(PInvokeDllNames.LsaCloseDllName)]
        internal static extern UInt32 LsaClose(IntPtr handle);

        [DllImport("api-ms-win-security-lsalookup-l1-1-2.dll")]
        internal static extern UInt32 LsaLookupUserAccountType([MarshalAs(UnmanagedType.LPArray)] byte[] Sid,
                                                               out LSA_USER_ACCOUNT_TYPE accountType);
        #endregion LSA Functions
    }
}
