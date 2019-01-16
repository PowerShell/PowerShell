// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

//using System.Management.Automation.SecurityAccountsManager.Native;

namespace System.Management.Automation.SecurityAccountsManager.Native.NtSam
{
    #region Enums
    internal enum ALIAS_INFORMATION_CLASS
    {
        AliasGeneralInformation = 1,
        AliasNameInformation,
        AliasAdminCommentInformation,
        AliasReplicationInformation,
        AliasExtendedInformation,
    }

    internal enum GROUP_INFORMATION_CLASS
    {
        GroupGeneralInformation = 1,
        GroupNameInformation,
        GroupAttributeInformation,
        GroupAdminCommentInformation,
        GroupReplicationInformation
    }

    internal enum USER_INFORMATION_CLASS
    {
        UserGeneralInformation      = 1,
        UserPreferencesInformation,
        UserLogonInformation,
        UserLogonHoursInformation,
        UserAccountInformation,
        UserNameInformation,
        UserAccountNameInformation,
        UserFullNameInformation,
        UserPrimaryGroupInformation,
        UserHomeInformation,
        UserScriptInformation,
        UserProfileInformation,
        UserAdminCommentInformation,
        UserWorkStationsInformation,
        UserSetPasswordInformation,
        UserControlInformation,
        UserExpiresInformation,
        UserInternal1Information,
        UserInternal2Information,
        UserParametersInformation,
        UserAllInformation,
        UserInternal3Information,
        UserInternal4Information,
        UserInternal5Information,
        UserInternal4InformationNew,
        UserInternal5InformationNew,
        UserInternal6Information,
        UserExtendedInformation,
        UserLogonUIInformation,
    }

    #endregion Enums

    #region Structures
    [StructLayout(LayoutKind.Sequential)]
    internal struct SR_SECURITY_DESCRIPTOR
    {
        public UInt32 Length;
        public IntPtr SecurityDescriptor;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LOGON_HOURS
    {
        public UInt16 UnitsPerWeek;

        //
        // UnitsPerWeek is the number of equal length time units the week is
        // divided into.  This value is used to compute the length of the bit
        // string in logon_hours.  Must be less than or equal to
        // SAM_UNITS_PER_WEEK (10080) for this release.
        //
        // LogonHours is a bit map of valid logon times.  Each bit represents
        // a unique division in a week.  The largest bit map supported is 1260
        // bytes (10080 bits), which represents minutes per week.  In this case
        // the first bit (bit 0, byte 0) is Sunday, 00:00:00 - 00-00:59; bit 1,
        // byte 0 is Sunday, 00:01:00 - 00:01:59, etc.  A NULL pointer means
        // DONT_CHANGE for SamSetInformationUser() calls.
        //

        public IntPtr LogonHours;
    }

    [StructLayout(LayoutKind.Sequential, Pack=4)]
    internal struct USER_ALL_INFORMATION
    {
        public LARGE_INTEGER LastLogon;
        public LARGE_INTEGER LastLogoff;
        public LARGE_INTEGER PasswordLastSet;
        public LARGE_INTEGER AccountExpires;
        public LARGE_INTEGER PasswordCanChange;
        public LARGE_INTEGER PasswordMustChange;
        public UNICODE_STRING UserName;
        public UNICODE_STRING FullName;
        public UNICODE_STRING HomeDirectory;
        public UNICODE_STRING HomeDirectoryDrive;
        public UNICODE_STRING ScriptPath;
        public UNICODE_STRING ProfilePath;
        public UNICODE_STRING AdminComment;
        public UNICODE_STRING WorkStations;
        public UNICODE_STRING UserComment;
        public UNICODE_STRING Parameters;
        public UNICODE_STRING LmPassword;
        public UNICODE_STRING NtPassword;
        public UNICODE_STRING PrivateData;
        public SR_SECURITY_DESCRIPTOR SecurityDescriptor;
        public UInt32 UserId;
        public UInt32 PrimaryGroupId;
        public UInt32 UserAccountControl;
        public UInt32 WhichFields;
        public LOGON_HOURS LogonHours;
        public UInt16 BadPasswordCount;
        public UInt16 LogonCount;
        public UInt16 CountryCode;
        public UInt16 CodePage;
        [MarshalAs(UnmanagedType.I1)]
        public bool LmPasswordPresent;
        [MarshalAs(UnmanagedType.I1)]
        public bool NtPasswordPresent;
        [MarshalAs(UnmanagedType.I1)]
        public bool PasswordExpired;
        [MarshalAs(UnmanagedType.I1)]
        public bool PrivateDataSensitive;
    }

    [StructLayout(LayoutKind.Sequential, Pack=4)]
    internal struct USER_GENERAL_INFORMATION
    {
        public UNICODE_STRING UserName;
        public UNICODE_STRING FullName;
        public UInt32 PrimaryGroupId;
        public UNICODE_STRING AdminComment;
        public UNICODE_STRING UserComment;
    }

    [StructLayout(LayoutKind.Sequential, Pack=4)]
    internal struct USER_LOGON_INFORMATION
    {
        public UNICODE_STRING UserName;
        public UNICODE_STRING FullName;
        public UInt32 UserId;
        public UInt32 PrimaryGroupId;
        public UNICODE_STRING HomeDirectory;
        public UNICODE_STRING HomeDirectoryDrive;
        public UNICODE_STRING ScriptPath;
        public UNICODE_STRING ProfilePath;
        public UNICODE_STRING WorkStations;
        public LARGE_INTEGER LastLogon;
        public LARGE_INTEGER LastLogoff;
        public LARGE_INTEGER PasswordLastSet;
        public LARGE_INTEGER PasswordCanChange;
        public LARGE_INTEGER PasswordMustChange;
        public LOGON_HOURS LogonHours;
        public UInt16 BadPasswordCount;
        public UInt16 LogonCount;
        public UInt32 UserAccountControl;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct USER_ACCOUNT_NAME_INFORMATION
    {
        public UNICODE_STRING UserName;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct USER_FULL_NAME_INFORMATION
    {
        public UNICODE_STRING FullName;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct USER_NAME_INFORMATION
    {
        public UNICODE_STRING UserName;
        public UNICODE_STRING FullName;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct USER_ADMIN_COMMENT_INFORMATION
    {
        UNICODE_STRING AdminComment;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct USER_EXPIRES_INFORMATION
    {
        // LARGE_INTEGER AccountExpires;
        public Int64 AccountExpires;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct USER_SET_PASSWORD_INFORMATION
    {
        public UNICODE_STRING Password;
        public bool PasswordExpired;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct USER_LOGON_HOURS_INFORMATION
    {
        public LOGON_HOURS LogonHours;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct POLICY_PRIMARY_DOMAIN_INFO
    {
        public UNICODE_STRING Name;
        public IntPtr Sid;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ALIAS_GENERAL_INFORMATION
    {
        public UNICODE_STRING Name;
        public UInt32 MemberCount;
        public UNICODE_STRING AdminComment;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ALIAS_NAME_INFORMATION
    {
        public UNICODE_STRING Name;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ALIAS_ADM_COMMENT_INFORMATION
    {
        public UNICODE_STRING AdminComment;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SAMPR_GROUP_GENERAL_INFORMATION
    {
        public UNICODE_STRING Name;
        public UInt32 Attributes;
        public UInt32 MemberCount;
        public UNICODE_STRING AdminComment;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SAMPR_GROUP_NAME_INFORMATION
    {
        public UNICODE_STRING Name;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SAM_RID_ENUMERATION
    {
        public UInt32 RelativeId;
        public UNICODE_STRING Name;
    }
    #endregion Structures

    /// <summary>
    /// Provides methods for invoking functions in the Windows
    /// Security Accounts Manager (SAM) API.
    /// </summary>
    internal static class SamApi
    {
        #region Constants
        // Account enumeration filters, may be combined by bitwise OR
        internal const UInt32 SAM_USER_ENUMERATION_FILTER_LOCAL     = 0x00000001;
        internal const UInt32 SAM_USER_ENUMERATION_FILTER_INTERNET  = 0x00000002;
        internal const UInt32 SAM_SERVER_LOOKUP_DOMAIN              = 0x0020;

        //
        // Bits to be used in UserAllInformation's WhichFields field (to indicate
        // which items were queried or set).
        //
        internal const UInt32 USER_ALL_USERNAME             = 0x00000001;
        internal const UInt32 USER_ALL_FULLNAME             = 0x00000002;
        internal const UInt32 USER_ALL_USERID               = 0x00000004;
        internal const UInt32 USER_ALL_PRIMARYGROUPID       = 0x00000008;
        internal const UInt32 USER_ALL_ADMINCOMMENT         = 0x00000010;
        internal const UInt32 USER_ALL_USERCOMMENT          = 0x00000020;
        internal const UInt32 USER_ALL_HOMEDIRECTORY        = 0x00000040;
        internal const UInt32 USER_ALL_HOMEDIRECTORYDRIVE   = 0x00000080;
        internal const UInt32 USER_ALL_SCRIPTPATH           = 0x00000100;
        internal const UInt32 USER_ALL_PROFILEPATH          = 0x00000200;
        internal const UInt32 USER_ALL_WORKSTATIONS         = 0x00000400;
        internal const UInt32 USER_ALL_LASTLOGON            = 0x00000800;
        internal const UInt32 USER_ALL_LASTLOGOFF           = 0x00001000;
        internal const UInt32 USER_ALL_LOGONHOURS           = 0x00002000;
        internal const UInt32 USER_ALL_BADPASSWORDCOUNT     = 0x00004000;
        internal const UInt32 USER_ALL_LOGONCOUNT           = 0x00008000;
        internal const UInt32 USER_ALL_PASSWORDCANCHANGE    = 0x00010000;
        internal const UInt32 USER_ALL_PASSWORDMUSTCHANGE   = 0x00020000;
        internal const UInt32 USER_ALL_PASSWORDLASTSET      = 0x00040000;
        internal const UInt32 USER_ALL_ACCOUNTEXPIRES       = 0x00080000;
        internal const UInt32 USER_ALL_USERACCOUNTCONTROL   = 0x00100000;
        internal const UInt32 USER_ALL_PARAMETERS           = 0x00200000;   // ntsubauth
        internal const UInt32 USER_ALL_COUNTRYCODE          = 0x00400000;
        internal const UInt32 USER_ALL_CODEPAGE             = 0x00800000;
        internal const UInt32 USER_ALL_NTPASSWORDPRESENT    = 0x01000000;   // field AND boolean
        internal const UInt32 USER_ALL_LMPASSWORDPRESENT    = 0x02000000;   // field AND boolean
        internal const UInt32 USER_ALL_PRIVATEDATA          = 0x04000000;   // field AND boolean
        internal const UInt32 USER_ALL_PASSWORDEXPIRED      = 0x08000000;
        internal const UInt32 USER_ALL_SECURITYDESCRIPTOR   = 0x10000000;
        internal const UInt32 USER_ALL_OWFPASSWORD          = 0x20000000;   // boolean

        internal const UInt32 USER_ALL_UNDEFINED_MASK       = 0xC0000000;

        //
        // Bit masks for the UserAccountControl member of the USER_ALL_INFORMATION structure
        //
        internal const UInt32 USER_ACCOUNT_DISABLED                       = 0x00000001;
        internal const UInt32 USER_HOME_DIRECTORY_REQUIRED                = 0x00000002;
        internal const UInt32 USER_PASSWORD_NOT_REQUIRED                  = 0x00000004;
        internal const UInt32 USER_TEMP_DUPLICATE_ACCOUNT                 = 0x00000008;
        internal const UInt32 USER_NORMAL_ACCOUNT                         = 0x00000010;
        internal const UInt32 USER_MNS_LOGON_ACCOUNT                      = 0x00000020;
        internal const UInt32 USER_INTERDOMAIN_TRUST_ACCOUNT              = 0x00000040;
        internal const UInt32 USER_WORKSTATION_TRUST_ACCOUNT              = 0x00000080;
        internal const UInt32 USER_SERVER_TRUST_ACCOUNT                   = 0x00000100;
        internal const UInt32 USER_DONT_EXPIRE_PASSWORD                   = 0x00000200;
        internal const UInt32 USER_ACCOUNT_AUTO_LOCKED                    = 0x00000400;
        internal const UInt32 USER_ENCRYPTED_TEXT_PASSWORD_ALLOWED        = 0x00000800;
        internal const UInt32 USER_SMARTCARD_REQUIRED                     = 0x00001000;
        internal const UInt32 USER_TRUSTED_FOR_DELEGATION                 = 0x00002000;
        internal const UInt32 USER_NOT_DELEGATED                          = 0x00004000;
        internal const UInt32 USER_USE_DES_KEY_ONLY                       = 0x00008000;
        internal const UInt32 USER_DONT_REQUIRE_PREAUTH                   = 0x00010000;
        internal const UInt32 USER_PASSWORD_EXPIRED                       = 0x00020000;
        internal const UInt32 USER_TRUSTED_TO_AUTHENTICATE_FOR_DELEGATION = 0x00040000;
        internal const UInt32 USER_NO_AUTH_DATA_REQUIRED                  = 0x00080000;
        internal const UInt32 USER_PARTIAL_SECRETS_ACCOUNT                = 0x00100000;
        internal const UInt32 USER_USE_AES_KEYS                           = 0x00200000;

        //
        // Access rights for user object
        //
        internal const UInt16 USER_CHANGE_PASSWORD  = 0x0040;
        #endregion Constants

        #region Sam Functions
        [DllImport("samlib.dll")]
        public static extern UInt32 SamConnect(ref UNICODE_STRING serverName,
                                               out IntPtr serverHandle,
                                               UInt32 desiredAccess,
                                               ref OBJECT_ATTRIBUTES objectAttributes);

        [DllImport("samlib.dll")]
        internal static extern UInt32 SamRidToSid(IntPtr objectHandle, UInt32 rid, out IntPtr sid);

        [DllImport("samlib.dll")]
        internal static extern UInt32 SamCloseHandle(IntPtr serverHandle);

        [DllImport("samlib.dll")]
        internal static extern UInt32 SamFreeMemory(IntPtr buffer);

        #region Domain Functions
        [DllImport("samlib.dll")]
        internal static extern UInt32 SamOpenDomain(IntPtr serverHandle,
                                                    UInt32 desiredAccess,
                                                    IntPtr domainId,
                                                    out IntPtr domainHandle);
        #endregion Domain Functions

        #region Alias Functions
        [DllImport("samlib.dll")]
        internal static extern UInt32 SamEnumerateAliasesInDomain(IntPtr domainHandle,
                                                                  ref UInt32 enumerationContext,
                                                                  out IntPtr buffer,
                                                                  UInt32 preferredMaximumLength,
                                                                  out UInt32 countReturned);

        [DllImport("samlib.dll")]
        internal static extern UInt32 SamCreateAliasInDomain(IntPtr domainHandle,
                                                             IntPtr accountName,        // PUNICODE_STRING
                                                             UInt32 desiredAccess,
                                                             out IntPtr aliasHandle,
                                                             out UInt32 relativeId);    // PULONG

        [DllImport("samlib.dll")]
        internal static extern UInt32 SamOpenAlias(IntPtr domainHandle,
                                                   UInt32 desiredAccess,
                                                   UInt32 aliasId,
                                                   out IntPtr aliasHandle);

        [DllImport("samlib.dll")]
        internal static extern UInt32 SamQueryInformationAlias(IntPtr aliasHandle,
                                                               ALIAS_INFORMATION_CLASS aliasInformationClass,
                                                               out IntPtr buffer);

        [DllImport("samlib.dll")]
        internal static extern UInt32 SamSetInformationAlias(IntPtr aliasHandle,
                                                             ALIAS_INFORMATION_CLASS aliasInformationClass,
                                                             IntPtr buffer);

        [DllImport("samlib.dll")]
        internal static extern UInt32 SamDeleteAlias(IntPtr aliasHandle);

        [DllImport("samlib.dll")]
        internal static extern UInt32 SamAddMemberToAlias(IntPtr aliasHandle,
                                                          byte[] memberId);     // PSID

        [DllImport("samlib.dll")]
        internal static extern UInt32 SamRemoveMemberFromAlias(IntPtr aliasHandle,
                                                               byte[] memberId);    // PSID

        [DllImport("samlib.dll")]
        internal static extern UInt32 SamGetMembersInAlias(IntPtr aliasHandle,
                                                           out IntPtr memberIds,    // PSID **
                                                           out UInt32 memberCount);
        #endregion Alias Functions

        #region User Functions
        [DllImport("samlib.dll")]
        internal static extern UInt32 SamOpenUser(IntPtr domainHandle,
                                                  UInt32 desiredAccess,
                                                  UInt32 userID,
                                                  out IntPtr userHandle);

        [DllImport("samlib.dll")]
        internal static extern UInt32 SamDeleteUser(IntPtr aliasHandle);

        [DllImport("samlib.dll")]
        internal static extern UInt32 SamEnumerateUsersInDomain(IntPtr domainHandle,
                                                                ref UInt32 enumerationContext,
                                                                UInt32 userAccountControl,
                                                                out IntPtr buffer,
                                                                UInt32 preferredMaximumLength,
                                                                out UInt32 countReturned);

        [DllImport("samlib.dll")]
        internal static extern UInt32 SamCreateUser2InDomain(IntPtr domainHandle,
                                                             ref UNICODE_STRING accountName,
                                                             Int32 accountType,
                                                             UInt32 desiredAccess,
                                                             out IntPtr userHandle,
                                                             out UInt32 grantedAccess,
                                                             out UInt32 relativeId);

        [DllImport("samlib.dll")]
        internal static extern UInt32 SamQueryInformationUser(IntPtr userHandle,
                                                              USER_INFORMATION_CLASS userInformationClass,
                                                              out IntPtr buffer);

        [DllImport("samlib.dll")]
        internal static extern UInt32 SamSetInformationUser(IntPtr userHandle,
                                                            USER_INFORMATION_CLASS userInformationClass,
                                                            IntPtr buffer);

        [DllImport("samlib.dll")]
        internal static extern UInt32 SamQuerySecurityObject(IntPtr objectHandle,
                                                             UInt32 securityInformation,
                                                             out IntPtr securityDescriptor);

        [DllImport("samlib.dll")]
        internal static extern UInt32 SamSetSecurityObject(IntPtr objectHandle,
                                                           UInt32 SecurityInformation,
                                                           IntPtr SecurityDescriptor);
        #endregion User Functions
        #endregion Sam Functions
    }
}
