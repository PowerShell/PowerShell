// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX
using System;
using System.Collections;
using System.Reflection;
using System.Globalization;
using System.Management.Automation;
using System.Diagnostics.CodeAnalysis;
using System.DirectoryServices;
using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// Contains auxiliary ToString CodeMethod implementations for some types.
    /// </summary>
    public static partial class ToStringCodeMethods
    {
        /// <summary>
        /// ToString implementation for PropertyValueCollection.
        /// </summary>
        /// <param name="instance">Instance of PSObject wrapping a PropertyValueCollection.</param>
        public static string PropertyValueCollection(PSObject instance)
        {
            if (instance == null)
                return string.Empty;

            var values = (PropertyValueCollection)instance.BaseObject;
            if (values == null)
                return string.Empty;

            if (values.Count == 1)
            {
                if (values[0] == null)
                {
                    return string.Empty;
                }

                return (PSObject.AsPSObject(values[0]).ToString());
            }

            return PSObject.ToStringEnumerable(null, (IEnumerable)values, null, null, null);
        }
    }

    /// <summary>
    /// Contains CodeMethod implementations for some adapted types like:
    ///
    /// 1. DirectoryEntry Related Code Methods
    ///    (a) Convert from DE LargeInteger to Int64.
    ///    (b) Convert from DE Dn-With-Binary to string.
    /// </summary>
    public static class AdapterCodeMethods
    {
        #region DirectoryEntry related CodeMethods

        /// <summary>
        /// Converts instance of LargeInteger to .net Int64.
        /// </summary>
        /// <param name="deInstance">Instance of PSObject wrapping DirectoryEntry object.</param>
        /// <param name="largeIntegerInstance">Instance of PSObject wrapping LargeInteger instance.</param>
        /// <returns>Converted Int64.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "integer")]
        public static Int64 ConvertLargeIntegerToInt64(PSObject deInstance, PSObject largeIntegerInstance)
        {
            if (largeIntegerInstance == null)
            {
                throw PSTraceSource.NewArgumentException(nameof(largeIntegerInstance));
            }

            object largeIntObject = (object)largeIntegerInstance.BaseObject;
            Type largeIntType = largeIntObject.GetType();

            // the following code might throw exceptions,
            // engine will catch these exceptions
            int highPart = (int)largeIntType.InvokeMember("HighPart",
                BindingFlags.GetProperty | BindingFlags.Public,
                null,
                largeIntObject,
                null,
                CultureInfo.InvariantCulture);
            int lowPart = (int)largeIntType.InvokeMember("LowPart",
                BindingFlags.GetProperty | BindingFlags.Public,
                null,
                largeIntObject,
                null,
                CultureInfo.InvariantCulture);

            // LowPart is not really a signed integer. Do not try to
            // use LowPart as a signed integer or you may get intermittent
            // surprises.

            // (long)highPart << 32 | (uint)lowPart
            byte[] data = new byte[8];
            BitConverter.GetBytes(lowPart).CopyTo(data, 0);
            BitConverter.GetBytes(highPart).CopyTo(data, 4);

            return BitConverter.ToInt64(data, 0);
        }

        /// <summary>
        /// Converts instance of DN-With-Binary to .net String.
        /// </summary>
        /// <param name="deInstance">Instance of PSObject wrapping DirectoryEntry object.</param>
        /// <param name="dnWithBinaryInstance">Instance of PSObject wrapping DN-With-Binary object.</param>
        /// <returns>Converted string.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dn", Justification = "DN represents valid prefix w.r.t Active Directory.")]
        public static string ConvertDNWithBinaryToString(PSObject deInstance, PSObject dnWithBinaryInstance)
        {
            if (dnWithBinaryInstance == null)
            {
                throw PSTraceSource.NewArgumentException(nameof(dnWithBinaryInstance));
            }

            object dnWithBinaryObject = (object)dnWithBinaryInstance.BaseObject;
            Type dnWithBinaryType = dnWithBinaryObject.GetType();

            // the following code might throw exceptions,
            // engine will catch these exceptions
            string dnString = (string)dnWithBinaryType.InvokeMember("DNString",
                BindingFlags.GetProperty | BindingFlags.Public,
                null,
                dnWithBinaryObject,
                null,
                CultureInfo.InvariantCulture);

            return dnString;
        }

        #endregion
    }
}
#endif
