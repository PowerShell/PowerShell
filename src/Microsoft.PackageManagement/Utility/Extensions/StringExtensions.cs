// 
//  Copyright (c) Microsoft Corporation. All rights reserved. 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//  

namespace Microsoft.PackageManagement.Internal.Utility.Extensions {
    using System;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using Collections;

    internal static class StringExtensions {
        private static readonly char[] _wildcardCharacters = new[] {
            '*', '?'
        };

        private static readonly Regex _escapeFilepathCharacters = new Regex(@"([\\|\$|\^|\{|\[|\||\)|\+|\.|\]|\}|\/])");

        private static string FixMeFormat(string formatString, object[] args) {
            return args.Aggregate(formatString.Replace('{', '\u00ab').Replace('}', '\u00bb'), (current, arg) => current + string.Format(CultureInfo.CurrentCulture, " \u00ab{0}\u00bb", arg));
        }

        public static IEnumerable<string> Quote(this IEnumerable<string> items) {
            return items.Select(each => "'" + each + "'");
        }

        public static string JoinWithComma(this IEnumerable<string> items) {
            return items.JoinWith(",");
        }

        public static string JoinWith(this IEnumerable<string> items, string delimiter) {
            return items.SafeAggregate((current, each) => current + delimiter + each);
        }

        public static TSource SafeAggregate<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, TSource> func) {
            var src = source.ReEnumerable();
            if (source != null && src.Any()) {
                return src.Aggregate(func);
            }
            return default(TSource);
        }

#if !CORECLR
        /// <summary>
        ///     encrypts the given collection of bytes with the user key and salt
        /// </summary>
        /// <param name="binaryData"> The binary data. </param>
        /// <param name="salt"> The salt. </param>
        /// <returns> </returns>
        /// <remarks>
        /// </remarks>
        public static IEnumerable<byte> ProtectBinaryForUser(this IEnumerable<byte> binaryData, string salt) {
            var data = binaryData.ToArray();
            var s = salt.ToByteArray();
            try {
                return ProtectedData.Protect(data, s, DataProtectionScope.CurrentUser);
            } finally {
                Array.Clear(data, 0, data.Length);
                Array.Clear(s, 0, s.Length);
            }
        }

        /// <summary>
        ///     decrypts the given collection of bytes with the user key and salt returns an empty collection of bytes on failure
        /// </summary>
        /// <param name="binaryData"> The binary data. </param>
        /// <param name="salt"> The salt. </param>
        /// <returns> </returns>
        /// <remarks>
        /// </remarks>
        public static IEnumerable<byte> UnprotectBinaryForUser(this IEnumerable<byte> binaryData, string salt) {
            if (binaryData == null) {
                return Enumerable.Empty<byte>();
            }

            try {
                return ProtectedData.Unprotect(binaryData.ToArray(), salt.ToByteArray(), DataProtectionScope.CurrentUser);
            } catch {
                /* suppress */
            }
            return Enumerable.Empty<byte>();
        }

        public static SecureString ToSecureString(this string password) {
            if (password == null) {
                throw new ArgumentNullException("password");
            }

            var ss = new SecureString();
            foreach (var ch in password.ToCharArray()) {
                ss.AppendChar(ch);
            }

            return ss;
        }  

        public static string ToProtectedString(this SecureString secureString, string salt) {
            return Convert.ToBase64String(secureString.ToBytes().ProtectBinaryForUser(salt).ToArray());
        }

        public static SecureString FromProtectedString(this string str, string salt) {
            return Convert.FromBase64String(str).UnprotectBinaryForUser(salt).ToUnicodeString().ToSecureString();
        }

        public static IEnumerable<byte> ToBytes(this SecureString securePassword) {
            if (securePassword == null) {
                throw new ArgumentNullException("securePassword");
            }

            var unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
            var ofs = 0;

            do {
                var x = Marshal.ReadByte(unmanagedString, ofs++);
                var y = Marshal.ReadByte(unmanagedString, ofs++);
                if (x == 0 && y == 0) {
                    break;
                }
                // now we have two bytes!
                yield return x;
                yield return y;
            } while (true);

            Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
        }

#endif

        public static Version ToVersion(this string versionInput) {
            if (string.IsNullOrWhiteSpace(versionInput)) {
                return null;
            }
            Version result;
            return Version.TryParse(versionInput, out result) ? result : null;
        }

        public static bool ContainsIgnoreCase(this IEnumerable<string> collection, string value) {
            if (collection == null) {
                return false;
            }
            return collection.Any(s => s.EqualsIgnoreCase(value));
        }

        public static bool ContainsAnyOfIgnoreCase(this IEnumerable<string> collection, params object[] values) {
            return collection.ContainsAnyOfIgnoreCase(values.Select(value => value == null ? null : value.ToString()));
        }

        public static bool ContainsAnyOfIgnoreCase(this IEnumerable<string> collection, IEnumerable<string> values) {
            if (collection == null) {
                return false;
            }
            var set = values.ReEnumerable();

            return collection.Any(set.ContainsIgnoreCase);
        }

        private static Regex WildcardToRegex(string wildcard, string noEscapePrefix = "^") {
            return new Regex(noEscapePrefix + _escapeFilepathCharacters.Replace(wildcard, "\\$1")
                .Replace("?", @".")
                .Replace("**", @"?")
                .Replace("*", @"[^\\\/\<\>\|]*")
                .Replace("?", @".*") + '$', RegexOptions.IgnoreCase);
        }

        /// <summary>
        ///     Determines whether the specified input has wildcards.
        /// </summary>
        /// <param name="input"> The input. </param>
        /// <returns>
        ///     <c>true</c> if the specified input has wildcards; otherwise, <c>false</c> .
        /// </returns>
        /// <remarks>
        /// </remarks>
        public static bool ContainsWildcards(this string input) {
            return input.IndexOfAny(_wildcardCharacters) > -1;
        }

        /// <summary>
        ///  Determines whether the input string is equals to the source string
        /// ignoring a single / at the end
        /// </summary>
        /// <param name="source"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool EqualsIgnoreEndSlash(this string source, string input)
        {
            return source.EqualsIgnoreCase(input)
                || (string.Concat(source, "/")).EqualsIgnoreCase(input)
                || (string.Concat(input, "/")).EqualsIgnoreCase(source);
        }

        /// <summary>
        ///     Determines whether the input string contains the specified substring
        /// </summary>
        public static bool ContainsIgnoreCase(this string source, string input) {
            return source.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool IsFile(this string input) {
            if (string.IsNullOrWhiteSpace(input)) {
                return false;
            }

            try {
                Uri result;
                if (Uri.TryCreate(input, UriKind.Absolute, out result)) {
                    return result.IsFile;
                }
            } catch {
            }
            return false;
        }

        public static bool IsWildcardMatch(this string input, string wildcardMask) {
            if (input == null || string.IsNullOrWhiteSpace(wildcardMask)) {
                return false;
            }
            return WildcardToRegex(wildcardMask).IsMatch(input);
        }

        private static byte FromHexChar(this char c) {
            if ((c >= 'a') && (c <= 'f')) {
                return (byte)(c - 'a' + 10);
            }
            if ((c >= 'A') && (c <= 'F')) {
                return (byte)(c - 'A' + 10);
            }
            if ((c >= '0') && (c <= '9')) {
                return (byte)(c - '0');
            }
            throw new ArgumentException("invalid hex char");
        }

        public static byte[] FromHex(this string hex) {
            if (string.IsNullOrWhiteSpace(hex)) {
                return new byte[0];
            }

            if ((hex.Length & 0x1) == 0x1) {
                throw new ArgumentException("Length must be a multiple of 2");
            }
            var input = hex.ToCharArray();
            var result = new byte[hex.Length >> 1];

            for (var i = 0; i < input.Length; i += 2) {
                result[i >> 1] = (byte)(((byte)(FromHexChar(input[i]) << 4)) | FromHexChar(input[i + 1]));
            }

            return result;
        }

        public static string FixVersion(this string versionString) {
            if (!string.IsNullOrWhiteSpace(versionString)) {
                if (versionString[0] == '.') {
                    // make sure we have a leading zero when someone says .5
                    versionString = "0" + versionString;
                }

                if (versionString.IndexOf('.') == -1) {
                    // make sure we make a 1 work like 1.0
                    versionString = versionString + ".0";
                }
            }
            return versionString;
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Formats the specified format string.
        /// </summary>
        /// <param name="formatString"> The format string. </param>
        /// <param name="args"> The args. </param>
        /// <returns> </returns>
        /// <remarks>
        /// </remarks>
        public static string format(this string formatString, params object[] args) {
            if (args == null || args.Length == 0) {
                return formatString;
            }

            try {
                var replacedByName = false;
                // first, try to replace
                formatString = new Regex(@"\$\{(?<macro>\w*?)\}").Replace(formatString, new MatchEvaluator((m) => {
                    var key = m.Groups["macro"].Value;

                    var p = args[0].GetType().GetProperty(key);
                    if (p != null) {
                        replacedByName = true;
                        return p.GetValue(args[0], null).ToString();
                    }
                    return "${{" + m.Groups["macro"].Value + "}}";
                }));

                // if it looks like it doesn't take parameters, (and yet we have args!)
                // let's return a fix-me-format string.
                if (!replacedByName && formatString.IndexOf('{') < 0) {
                    return FixMeFormat(formatString, args);
                }

                return String.Format(CultureInfo.CurrentCulture, formatString, args);
            } catch (Exception) {
                // if we got an exception, let's at least return a string that we can use to figure out what parameters should have been matched vs what was passed.
                return FixMeFormat(formatString, args);
            }
        }

        /// <summary>
        ///     Encodes the string as an array of UTF8 bytes.
        /// </summary>
        /// <param name="text"> The text. </param>
        /// <returns> </returns>
        /// <remarks>
        /// </remarks>
        internal static byte[] ToByteArray(this string text) {
            return Encoding.UTF8.GetBytes(text);
        }

        internal static string ToUnicodeString(this IEnumerable<byte> bytes) {
            var data = bytes.ToArray();
            try {
                return Encoding.Unicode.GetString(data);
            } finally {
                Array.Clear(data, 0, data.Length);
            }
        }

        public static bool IsTrue(this string text) {
            return !string.IsNullOrWhiteSpace(text) && text.Equals("true", StringComparison.CurrentCultureIgnoreCase);
        }

        public static bool? IsTruePreserveNull(this string text) {
            if (text == null) {
                return null;
            }
            return !string.IsNullOrWhiteSpace(text) && text.Equals("true", StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        ///     coerces a string to an int32, defaults to zero.
        /// </summary>
        /// <param name="str"> The STR. </param>
        /// <param name="defaultValue"> The default value if the string isn't a valid int. </param>
        /// <returns> </returns>
        /// <remarks>
        /// </remarks>
        public static int ToInt32(this string str, int defaultValue) {
            int i;
            return Int32.TryParse(str, out i) ? i : defaultValue;
        }

        public static bool EqualsIgnoreCase(this string str, string str2) {
            if (str == null && str2 == null) {
                return true;
            }

            if (str == null || str2 == null) {
                return false;
            }

            return str.Equals(str2, StringComparison.OrdinalIgnoreCase);
        }

        // ReSharper restore InconsistentNaming
    }
}