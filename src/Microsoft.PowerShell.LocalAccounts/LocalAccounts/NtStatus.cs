// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace System.Management.Automation.SecurityAccountsManager.Native
{
    internal static class NtStatus
    {
        #region Constants
        //
        //  These values are taken from ntstatus.h
        //

        //
        // Severity codes
        //
        public const UInt32 STATUS_SEVERITY_WARNING         = 0x2;
        public const UInt32 STATUS_SEVERITY_SUCCESS         = 0x0;
        public const UInt32 STATUS_SEVERITY_INFORMATIONAL   = 0x1;
        public const UInt32 STATUS_SEVERITY_ERROR           = 0x3;

        public const UInt32 STATUS_SUCCESS                  = 0x00000000;
        //
        // MessageText:
        //
        // Returned by enumeration APIs to indicate more information is available to successive calls.
        //
        public const UInt32 STATUS_MORE_ENTRIES             = 0x00000105;


        /////////////////////////////////////////////////////////////////////////
        //
        // Standard Information values
        //
        /////////////////////////////////////////////////////////////////////////

        //
        // MessageText:
        //
        // {Object Exists}
        // An attempt was made to create an object and the object name already existed.
        //
        public const UInt32 STATUS_OBJECT_NAME_EXISTS        = 0x40000000;

        //
        // MessageText:
        //
        // {Password Too Complex}
        // The Windows password is too complex to be converted to a LAN Manager password. The LAN Manager password returned is a NULL string.
        //
        public const UInt32 STATUS_NULL_LM_PASSWORD          = 0x4000000D;

        //
        // MessageText:
        //
        // {Access Denied}
        // A process has requested access to an object, but has not been granted those access rights.
        //
        public const UInt32 STATUS_ACCESS_DENIED             = 0xC0000022;

        //
        // MessageText:
        //
        // The name provided is not a properly formed account name.
        //
        public const UInt32 STATUS_INVALID_ACCOUNT_NAME      = 0xC0000062;

        //
        // MessageText:
        //
        // The specified account already exists.
        //
        public const UInt32 STATUS_USER_EXISTS               = 0xC0000063;

        //
        // MessageText:
        //
        // The specified account does not exist.
        //
        public const UInt32 STATUS_NO_SUCH_USER              = 0xC0000064;     // ntsubauth

        //
        // MessageText:
        //
        // The specified group already exists.
        //
        public const UInt32 STATUS_GROUP_EXISTS              = 0xC0000065;

        //
        // MessageText:
        //
        // The specified group does not exist.
        //
        public const UInt32 STATUS_NO_SUCH_GROUP             = 0xC0000066;

        //
        // MessageText:
        //
        // The specified user account is already in the specified group account. Also used to indicate a group cannot be deleted because it contains a member.
        //
        public const UInt32 STATUS_MEMBER_IN_GROUP           = 0xC0000067;

        //
        // MessageText:
        //
        // The specified user account is not a member of the specified group account.
        //
        public const UInt32 STATUS_MEMBER_NOT_IN_GROUP       = 0xC0000068;

        //
        // MessageText:
        //
        // Indicates the requested operation would disable, delete or could prevent logon for an administration account.
        // This is not allowed to prevent creating a situation in which the system cannot be administrated.
        //
        public const UInt32 STATUS_LAST_ADMIN                = 0xC0000069;

        //
        // MessageText:
        //
        // When trying to update a password, this return status indicates that the value provided as the current password is not correct.
        //
        public const UInt32 STATUS_WRONG_PASSWORD            = 0xC000006A;     // ntsubauth

        //
        // MessageText:
        //
        // When trying to update a password, this return status indicates that the value provided for the new password contains values that are not allowed in passwords.
        //
        public const UInt32 STATUS_ILL_FORMED_PASSWORD       = 0xC000006B;

        //
        // MessageText:
        //
        // When trying to update a password, this status indicates that some password update rule has been violated. For example, the password may not meet length criteria.
        //
        public const UInt32 STATUS_PASSWORD_RESTRICTION      = 0xC000006C;     // ntsubauth

        //
        // MessageText:
        //
        // The user account's password has expired.
        //
        public const UInt32 STATUS_PASSWORD_EXPIRED          = 0xC0000071;     // ntsubauth

        //
        // MessageText:
        //
        // The referenced account is currently disabled and may not be logged on to.
        //
        public const UInt32 STATUS_ACCOUNT_DISABLED          = 0xC0000072;     // ntsubauth

        //
        // MessageText:
        //
        // None of the information to be translated has been translated.
        //
        public const UInt32 STATUS_NONE_MAPPED               = 0xC0000073;

        //
        // MessageText:
        //
        // Indicates the sub-authority value is invalid for the particular use.
        //
        public const UInt32 STATUS_INVALID_SUB_AUTHORITY     = 0xC0000076;

        //
        // MessageText:
        //
        // Indicates the ACL structure is not valid.
        //
        public const UInt32 STATUS_INVALID_ACL               = 0xC0000077;

        //
        // MessageText:
        //
        // Indicates the SID structure is not valid.
        //
        public const UInt32 STATUS_INVALID_SID               = 0xC0000078;

        //
        // MessageText:
        //
        // Indicates the SECURITY_DESCRIPTOR structure is not valid.
        //
        public const UInt32 STATUS_INVALID_SECURITY_DESCR    = 0xC0000079;

        //
        // Network specific errors.
        //
        //
        //
        // MessageText:
        //
        // The request is not supported.
        //
        public const UInt32 STATUS_NOT_SUPPORTED             = 0xC00000BB;

        //
        // MessageText:
        //
        // This remote computer is not listening.
        //
        public const UInt32 STATUS_REMOTE_NOT_LISTENING      = 0xC00000BC;

        //
        // MessageText:
        //
        // Network access is denied.
        //
        public const UInt32 STATUS_NETWORK_ACCESS_DENIED     = 0xC00000CA;

        //
        // MessageText:
        //
        // Indicates an attempt was made to operate on the security of an object that does not have security associated with it.
        //
        public const UInt32 STATUS_NO_SECURITY_ON_OBJECT     = 0xC00000D7;

        //
        // MessageText:
        //
        // An internal error occurred.
        //
        public const UInt32 STATUS_INTERNAL_ERROR            = 0xC00000E5;

        //
        // MessageText:
        //
        // Indicates a security descriptor is not in the necessary format (absolute or self-relative).
        //
        public const UInt32 STATUS_BAD_DESCRIPTOR_FORMAT     = 0xC00000E7;

        //
        // MessageText:
        //
        // A specified name string is too long for its intended use.
        //
        public const UInt32 STATUS_NAME_TOO_LONG             = 0xC0000106;

        //
        // MessageText:
        //
        // Indicates a name specified as a remote computer name is syntactically invalid.
        //
        public const UInt32 STATUS_INVALID_COMPUTER_NAME     = 0xC0000122;

        //
        // MessageText:
        //
        // Indicates an operation has been attempted on a built-in (special) SAM account which is incompatible with built-in accounts. For example, built-in accounts cannot be deleted.
        //
        public const UInt32 STATUS_SPECIAL_ACCOUNT           = 0xC0000124;

        //
        // MessageText:
        //
        // The operation requested may not be performed on the specified group because it is a built-in special group.
        //
        public const UInt32 STATUS_SPECIAL_GROUP             = 0xC0000125;

        //
        // MessageText:
        //
        // The operation requested may not be performed on the specified user because it is a built-in special user.
        //
        public const UInt32 STATUS_SPECIAL_USER              = 0xC0000126;

        //
        // MessageText:
        //
        // Indicates a member cannot be removed from a group because the group is currently the member's primary group.
        //
        public const UInt32 STATUS_MEMBERS_PRIMARY_GROUP     = 0xC0000127;

        //
        // MessageText:
        //
        // The specified local group does not exist.
        //
        public const UInt32 STATUS_NO_SUCH_ALIAS             = 0xC0000151;

        //
        // MessageText:
        //
        // The specified account name is not a member of the group.
        //
        public const UInt32 STATUS_MEMBER_NOT_IN_ALIAS       = 0xC0000152;

        //
        // MessageText:
        //
        // The specified account name is already a member of the group.
        //
        public const UInt32 STATUS_MEMBER_IN_ALIAS           = 0xC0000153;

        //
        // MessageText:
        //
        // The specified local group already exists.
        //
        public const UInt32 STATUS_ALIAS_EXISTS              = 0xC0000154;

        //
        // MessageText:
        //
        // A member could not be added to or removed from the local group because the member does not exist.
        //
        public const UInt32 STATUS_NO_SUCH_MEMBER            = 0xC000017A;

        //
        // MessageText:
        //
        // A new member could not be added to a local group because the member has the wrong account type.
        //
        public const UInt32 STATUS_INVALID_MEMBER            = 0xC000017B;

        //
        // MessageText:
        //
        // The user's account has expired.
        //
        public const UInt32 STATUS_ACCOUNT_EXPIRED           = 0xC0000193;    // ntsubauth

        //
        // MessageText:
        //
        // {Invalid ACE Condition}
        // The specified access control entry (ACE) contains an invalid condition.
        //
        public const UInt32 STATUS_INVALID_ACE_CONDITION     = 0xC00001A2;

        //
        // MessageText:
        //
        // The user's password must be changed before signing in.
        //
        public const UInt32 STATUS_PASSWORD_MUST_CHANGE      = 0xC0000224;    // ntsubauth

        //
        // MessageText:
        //
        // The object was not found.
        //
        public const UInt32 STATUS_NOT_FOUND                 = 0xC0000225;

        //
        // MessageText:
        //
        // Could not find a domain controller for this domain.
        //
        public const UInt32 STATUS_DOMAIN_CONTROLLER_NOT_FOUND = 0xC0000233;

        //
        // MessageText:
        //
        // The user account has been automatically locked because too many invalid logon attempts or password change attempts have been requested.
        //
        public const UInt32 STATUS_ACCOUNT_LOCKED_OUT        = 0xC0000234;    // ntsubauth

        //
        // MessageText:
        //
        // The password provided is too short to meet the policy of your user account. Please choose a longer password.
        //
        public const UInt32 STATUS_PWD_TOO_SHORT             = 0xC000025A;

        //
        // MessageText:
        //
        // The policy of your user account does not allow you to change passwords too frequently. This is done to prevent users from changing back to a familiar, but potentially discovered, password. If you feel your password has been compromised then please contact your administrator immediately to have a new one assigned.
        //
        public const UInt32 STATUS_PWD_TOO_RECENT            = 0xC000025B;

        //
        // MessageText:
        //
        // You have attempted to change your password to one that you have used in the past. The policy of your user account does not allow this. Please select a password that you have not previously used.
        //
        public const UInt32 STATUS_PWD_HISTORY_CONFLICT      = 0xC000025C;

        //
        // MessageText:
        //
        // The password provided is too long to meet the policy of your user account. Please choose a shorter password.
        //
        public const UInt32 STATUS_PWD_TOO_LONG              = 0xC000027A;

        //
        // MessageText:
        //
        // Only an administrator can modify the membership list of an administrative group.
        //
        public const UInt32 STATUS_DS_SENSITIVE_GROUP_VIOLATION = 0xC00002CD;

        //
        // MessageText:
        //
        // The specified group type is invalid.
        //
        public const UInt32 STATUS_DS_INVALID_GROUP_TYPE     = 0xC00002D4;

        //
        // MessageText:
        //
        // A local group cannot have another cross domain local group as a member.
        //
        public const UInt32 STATUS_DS_LOCAL_CANT_HAVE_CROSSDOMAIN_LOCAL_MEMBER = 0xC00002DB;

        //
        // MessageText:
        //
        // Cannot change to security disabled group because of having primary members in this group.
        //
        public const UInt32 STATUS_DS_HAVE_PRIMARY_MEMBERS   = 0xC00002DC;

        //
        // MessageText:
        //
        // EAS policy requires that the user change their password before this operation can be performed.
        //
        public const UInt32 STATUS_PASSWORD_CHANGE_REQUIRED  = 0xC000030C;

        #endregion Constants

        #region Public Methods
        /// <summary>
        /// Determine if an NTSTATUS value indicates Success.
        /// </summary>
        /// <param name="ntstatus">The NTSTATUS value returned from native functions.</param>
        /// <returns>
        /// True if the NTSTATUS value indicates success, false otherwise.
        /// </returns>
        public static bool IsSuccess(UInt32 ntstatus)
        {
            return Severity(ntstatus) == STATUS_SEVERITY_SUCCESS;
        }

        /// <summary>
        /// Determine if an NTSTATUS value indicates an Error.
        /// </summary>
        /// <param name="ntstatus">The NTSTATUS value returned from native functions.</param>
        /// <returns>
        /// True if the NTSTATUS value indicates an error, false otherwise.
        /// </returns>
        public static bool IsError(UInt32 ntstatus)
        {
            return Severity(ntstatus) == STATUS_SEVERITY_ERROR;
        }

        /// <summary>
        /// Determine if an NTSTATUS value indicates a Warning.
        /// </summary>
        /// <param name="ntstatus">The NTSTATUS value returned from native functions.</param>
        /// <returns>
        /// True if the NTSTATUS value indicates a warning, false otherwise.
        /// </returns>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static bool IsWarning(UInt32 ntstatus)
        {
            return Severity(ntstatus) == STATUS_SEVERITY_WARNING;
        }

        /// <summary>
        /// Determine if an NTSTATUS value indicates that the value is Informational.
        /// </summary>
        /// <param name="ntstatus">The NTSTATUS value returned from native functions.</param>
        /// <returns>
        /// True if the NTSTATUS value indicates that it is informational, false otherwise.
        /// </returns>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static bool IsInformational(UInt32 ntstatus)
        {
            return Severity(ntstatus) == STATUS_SEVERITY_INFORMATIONAL;
        }

        /// <summary>
        /// Return the Severity part of an NTSTATUS value.
        /// </summary>
        /// <param name="ntstatus">The NTSTATUS value returned from native functions.</param>
        /// <returns>
        /// One of the STATUS_SEVERITY_* values
        /// </returns>
        public static uint Severity(UInt32 ntstatus)
        {
            return ntstatus >> 30;
        }

        /// <summary>
        /// Return the Facility part of an NSTATUS value.
        /// </summary>
        /// <param name="ntstatus">The NTSTATUS value returned from native functions.</param>
        /// <returns>
        /// The value of the Facility portion of an NTSTATUS value.
        /// </returns>

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static uint Facility(UInt32 ntstatus)
        {
            return (ntstatus >> 16) & 0x0FFF;
        }

        /// <summary>
        /// Return the Code part of an NTSTATUS value.
        /// </summary>
        /// <param name="ntstatus">The NTSTATUS value returned from native functions.</param>
        /// <returns>
        /// The value of the Code portion of an NTSTATUS value.
        /// </returns>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static uint Code(UInt32 ntstatus)
        {
            return ntstatus & 0xFFFF;
        }

        #endregion Public Methods
    }
}
