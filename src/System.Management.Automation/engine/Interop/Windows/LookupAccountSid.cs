// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Keep native struct names.")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1306:FieldNamesMustBeginWithLowerCaseLetter", Justification = "Keep native struct names.")]
    internal static unsafe partial class Windows
    {
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

        [LibraryImport("api-ms-win-security-lsalookup-l2-1-0.dll", EntryPoint = "LookupAccountSidW",  SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static unsafe partial bool LookupAccountSidPrivate(
            string lpSystemName,
            nint Sid,
            ref char Name,
            ref int cchName,
            ref char ReferencedDomainName,
            ref int cchReferencedDomainName,
            out SID_NAME_USE peUse);

        internal static unsafe bool LookupAccountSid(
            string lpSystemName,
            nint sid,
            Span<char> userName,
            ref int cchName,
            Span<char> domainName,
            ref int cchDomainName,
            out SID_NAME_USE peUse)
        {
            return LookupAccountSidPrivate(
                lpSystemName,
                sid,
                ref userName.GetPinnableReference(),
                ref cchName,
                ref domainName.GetPinnableReference(),
                ref cchDomainName,
                out peUse);
        }
    }
}
