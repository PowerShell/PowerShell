// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Management.Automation.SecurityAccountsManager.Native
{
    #region Enums
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
        [DllImport("api-ms-win-security-lsalookup-l1-1-2.dll")]
        internal static extern UInt32 LsaLookupUserAccountType([MarshalAs(UnmanagedType.LPArray)] byte[] Sid,
                                                               out LSA_USER_ACCOUNT_TYPE accountType);
        #endregion LSA Functions
    }
}
