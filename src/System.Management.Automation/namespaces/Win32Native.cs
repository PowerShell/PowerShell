// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Management.Automation;
    using System.Diagnostics.CodeAnalysis;

    /**
     * Win32 encapsulation for MSCORLIB.
     */
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

        [StructLayout(LayoutKind.Sequential)]
        internal struct SID_AND_ATTRIBUTES
        {
            internal IntPtr Sid;
            internal uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
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
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern unsafe bool LookupAccountSid(string lpSystemName,
                                                     IntPtr sid,
                                                     char* lpName,
                                                     ref int cchName,
                                                     char* referencedDomainName,
                                                     ref int cchReferencedDomainName,
                                                     out SID_NAME_USE peUse);

        internal static unsafe bool LookupAccountSid(string lpSystemName,
                                                     IntPtr sid,
                                                     Span<char> userName,
                                                     ref int cchName,
                                                     Span<char> domainName,
                                                     ref int cchDomainName,
                                                     out SID_NAME_USE peUse)
        {
            fixed (char* userNamePtr = &MemoryMarshal.GetReference(userName))
            fixed (char* domainNamePtr = &MemoryMarshal.GetReference(domainName))
            {
                return LookupAccountSid(lpSystemName,
                                        sid,
                                        userNamePtr,
                                        ref cchName,
                                        domainNamePtr,
                                        ref cchDomainName,
                                        out peUse);
            }
        }

        [DllImport(PinvokeDllNames.CloseHandleDllName, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr handle);

        /// <summary>
        /// Retrieves the current process token.
        /// </summary>
        /// <param name="processHandle">Process handle.</param>
        /// <param name="desiredAccess">Token access.</param>
        /// <param name="tokenHandle">Process token.</param>
        /// <returns>The current process token.</returns>
        [DllImport(PinvokeDllNames.OpenProcessTokenDllName, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

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
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetTokenInformation(IntPtr tokenHandle,
                                                        TOKEN_INFORMATION_CLASS tokenInformationClass,
                                                        IntPtr tokenInformation,
                                                        int tokenInformationLength,
                                                        out int returnLength);

        #endregion PInvoke Methods
    }
}
