// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Text.RegularExpressions;

using Microsoft.PowerShell.Commands;
using Microsoft.PowerShell.LocalAccounts;

namespace System.Management.Automation.SecurityAccountsManager.Extensions
{
    /// <summary>
    /// Provides extension methods for the Cmdlet class.
    /// </summary>
    internal static class CmdletExtensions
    {
        /// <summary>
        /// Attempt to create a SID from a string.
        /// </summary>
        /// <param name="cmdlet">The cmdlet being extended with this method.</param>
        /// <param name="s">The string to be converted to a SID.</param>
        /// <param name="allowSidConstants">
        /// A boolean indicating whether SID constants, such as "BA", are considered.
        /// </param>
        /// <returns>
        /// A <see cref="SecurityIdentifier"/> object if the conversion was successful,
        /// null otherwise.
        /// </returns>
        internal static SecurityIdentifier TrySid(this Cmdlet cmdlet,
                                                  string s,
                                                  bool allowSidConstants = false)
        {
            if (!allowSidConstants)
                if (!(s.Length > 2 && s.StartsWith("S-", StringComparison.Ordinal) && char.IsDigit(s[2])))
                    return null;

            SecurityIdentifier sid = null;

            try
            {
                sid = new SecurityIdentifier(s);
            }
            catch (ArgumentException)
            {
                // do nothing here, just fall through to the return
            }

            return sid;
        }
    }

    /// <summary>
    /// Provides extension methods for the PSCmdlet class.
    /// </summary>
    internal static class PSExtensions
    {
        /// <summary>
        /// Determine if a given parameter was provided to the cmdlet.
        /// </summary>
        /// <param name="cmdlet">
        /// The <see cref="PSCmdlet"/> object to check.
        /// </param>
        /// <param name="parameterName">
        /// A string containing the name of the parameter. This should be in the
        /// same letter-casing as the defined parameter.
        /// </param>
        /// <returns>
        /// True if the specified parameter was given on the cmdlet invocation,
        /// false otherwise.
        /// </returns>
        internal static bool HasParameter(this PSCmdlet cmdlet, string parameterName)
        {
            var invocation = cmdlet.MyInvocation;
            if (invocation != null)
            {
                var parameters = invocation.BoundParameters;

                if (parameters != null)
                {
                    // PowerShell sets the parameter names in the BoundParameters dictionary
                    // to their "proper" casing, so we don't have to do a case-insensitive search.
                    if (parameters.ContainsKey(parameterName))
                        return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Provides extension methods for the SecurityIdentifier class.
    /// </summary>
    internal static class SidExtensions
    {
        /// <summary>
        /// Get the Relative ID (RID) from a <see cref="SecurityIdentifier"/> object.
        /// </summary>
        /// <param name="sid">The SecurityIdentifier containing the desired Relative ID.</param>
        /// <returns>
        /// A UInt32 value containing the Relative ID in the SecurityIdentifier.
        /// </returns>
        internal static UInt32 GetRid(this SecurityIdentifier sid)
        {
            byte[] sidBinary = new byte[sid.BinaryLength];
            sid.GetBinaryForm(sidBinary, 0);

            return System.BitConverter.ToUInt32(sidBinary, sidBinary.Length-4);
        }

        /// <summary>
        /// Gets the Identifier Authority portion of a <see cref="SecurityIdentifier"/>
        /// </summary>
        /// <param name="sid">The SecurityIdentifier containing the desired Authority.</param>
        /// <returns>
        /// A long integer value containing the SecurityIdentifier's Identifier Authority value.
        /// </returns>
        /// <remarks>
        /// This method is used primarily for determining the Source of a Principal.
        /// The Win32 API LsaLookupUserAccountType function does not (yet) properly
        /// identify MicrosoftAccount principals.
        /// </remarks>
        internal static long GetIdentifierAuthority(this SecurityIdentifier sid)
        {
            byte[] sidBinary = new byte[sid.BinaryLength];

            sid.GetBinaryForm(sidBinary, 0);

            // The Identifier Authority is six bytes wide,
            // in big-endian format, starting at the third byte
            long authority = (long) (((long)sidBinary[2]) << 40) +
                                    (((long)sidBinary[3]) << 32) +
                                    (((long)sidBinary[4]) << 24) +
                                    (((long)sidBinary[5]) << 16) +
                                    (((long)sidBinary[6]) <<  8) +
                                    (((long)sidBinary[7])      );

            return authority;
        }

        internal static bool IsMsaAccount(this SecurityIdentifier sid)
        {
            return sid.GetIdentifierAuthority() == 11;
        }
    }

    internal static class SecureStringExtensions
    {
        /// <summary>
        /// Extension method to extract clear text from a
        /// <see cref="System.Security.SecureString"/> object.
        /// </summary>
        /// <param name="str">
        /// This SecureString object, containing encrypted text.
        /// </param>
        /// <returns>
        /// A string containing the SecureString object's original text.
        /// </returns>
        internal static string AsString(this SecureString str)
        {
#if CORECLR
            IntPtr buffer = SecureStringMarshal.SecureStringToCoTaskMemUnicode(str);
            string clear = Marshal.PtrToStringUni(buffer);
            Marshal.ZeroFreeCoTaskMemUnicode(buffer);
#else
            var bstr = Marshal.SecureStringToBSTR(str);
            string clear = Marshal.PtrToStringAuto(bstr);
            Marshal.ZeroFreeBSTR(bstr);
#endif
            return clear;
        }
    }

    internal static class ExceptionExtensions
    {
        internal static ErrorRecord MakeErrorRecord(this Exception ex,
                                                    string errorId,
                                                    ErrorCategory errorCategory,
                                                    object target = null)
        {
            return new ErrorRecord(ex, errorId, errorCategory, target);
        }

        internal static ErrorRecord MakeErrorRecord(this Exception ex, object target = null)
        {
            // This part is somewhat less than beautiful, but it prevents
            // having to have multiple exception handlers in every cmdlet command.
            var exTemp = ex as LocalAccountsException;

            if (exTemp != null)
                return MakeErrorRecord(exTemp, target ?? exTemp.Target);

            return new ErrorRecord(ex,
                                   Strings.UnspecifiedError,
                                   ErrorCategory.NotSpecified,
                                   target);
        }

        internal static ErrorRecord MakeErrorRecord(this LocalAccountsException ex, object target = null)
        {
            return ex.MakeErrorRecord(ex.ErrorName, ex.ErrorCategory, target ?? ex.Target);
        }
    }
}
