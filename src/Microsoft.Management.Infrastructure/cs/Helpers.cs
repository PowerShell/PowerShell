/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Microsoft.Management.Infrastructure.Internal
{
    internal static class Helpers
    {
        public static void SafeInvoke<T>(this EventHandler<T> eventHandler, object sender, T eventArgs) where T : EventArgs
        {
            if (eventHandler != null)
            {
                eventHandler(sender, eventArgs);
            }
        }

        public static void ValidateNoNullElements(IList list)
        {
            if (list == null) // argument wasn't an IList
            {
                return;
            }

            /*
             * Our implementation does not allow individual array elements to be null. 
             * It is an open question whether CIM does. The MOF BNF seems to imply that
             * they are supported but some think it was an oversight. As a practical matter, 
             * it has not come up with any DMTF profiles (which require that all array elements
             * be non-null so far). Also OpenPegasus does not allow individual array elements 
             * to be null either, although CMPI does.
             */

            if (list.Cast<object>().Any(element => element == null))
            {
                throw new ArgumentException(Strings.ArrayCannotContainNullElements);
            }
        }

        public static string ToStringFromNameAndValue(string name, object value)
        {
            if (value == null)
            {
                return name;
            }

            if ((value is CimInstance) || (value is Array))
            {
                value = "...";
            }
            else if ((value is string) || (value is char))
            {
                StringBuilder valueBuilder = new StringBuilder();
                valueBuilder.Append('\"');
                foreach (char c in value.ToString())
                {
                    if (!char.IsControl(c) && (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsWhiteSpace(c)))
                    {
                        valueBuilder.Append(c);
                    }
                    else
                    {
                        valueBuilder.Append('?');
                    }
                }
                valueBuilder.Append('\"');
                value = valueBuilder.ToString();
            }

            const int maxValueStringLength = 40;
            string valueString = value.ToString();
            if (valueString.Length > maxValueStringLength)
            {
                valueString = valueString.Substring(0, maxValueStringLength) + "...";
            }

            string toStringValue = string.Format(
                CultureInfo.InvariantCulture,
                Strings.CimNameAndValueToString,
                name,
                valueString);

            return toStringValue;
        }
    }
}