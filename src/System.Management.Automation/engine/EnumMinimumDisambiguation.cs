// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text;
using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    /// <summary>
    /// Performs enum minimum disambiguation.
    /// </summary>
    internal static class EnumMinimumDisambiguation
    {
        #region Constructors

        /// <summary>
        /// Initialize the dictionary for special cases of minimum disambiguation.
        /// </summary>
        static EnumMinimumDisambiguation()
        {
            // Add special minimum disambiguation cases here for certain enum types.
            // The current implementation assumes that special names in each type can be
            // differentiated by their first letter.
            s_specialDisambiguateCases.Add(
                typeof(System.IO.FileAttributes),
                new string[] { "Directory", "ReadOnly", "System" });
        }

        #endregion

        /// <summary>
        /// Perform disambiguation on enum names.
        /// </summary>
        /// <returns>Complete enum name after disambiguation.</returns>
        internal static string EnumDisambiguate(string text, Type enumType)
        {
            // Get all enum names in the given enum type
            string[] enumNames = Enum.GetNames(enumType);

            // Get all names that matches the given prefix.
            List<string> namesWithMatchingPrefix = new List<string>();
            foreach (string name in enumNames)
            {
                if (name.StartsWith(text, StringComparison.OrdinalIgnoreCase))
                {
                    namesWithMatchingPrefix.Add(name);
                }
            }

            // Throw error when no match is found.
            if (namesWithMatchingPrefix.Count == 0)
            {
                throw InterpreterError.NewInterpreterException(null, typeof(RuntimeException),
                    null, "NoEnumNameMatch", EnumExpressionEvaluatorStrings.NoEnumNameMatch, text, EnumAllValues(enumType));
            }
            // Return the result if there is only one match.
            else if (namesWithMatchingPrefix.Count == 1)
            {
                return namesWithMatchingPrefix[0];
            }
            // multiple matches situation
            else
            {
                // test for exact match
                foreach (string matchName in namesWithMatchingPrefix)
                {
                    if (matchName.Equals(text, StringComparison.OrdinalIgnoreCase))
                    {
                        return matchName;
                    }
                }
                // test for special cases match
                string[] minDisambiguateNames;
                if (s_specialDisambiguateCases.TryGetValue(enumType, out minDisambiguateNames))
                {
                    foreach (string tName in minDisambiguateNames)
                    {
                        if (tName.StartsWith(text, StringComparison.OrdinalIgnoreCase))
                        {
                            return tName;
                        }
                    }
                }
                // No special cases match, throw error for multiple matches.
                StringBuilder matchListSB = new StringBuilder(namesWithMatchingPrefix[0]);
                string separator = ", ";
                for (int i = 1; i < namesWithMatchingPrefix.Count; i++)
                {
                    matchListSB.Append(separator);
                    matchListSB.Append(namesWithMatchingPrefix[i]);
                }

                throw InterpreterError.NewInterpreterException(null, typeof(RuntimeException),
                    null, "MultipleEnumNameMatch", EnumExpressionEvaluatorStrings.MultipleEnumNameMatch,
                    text, matchListSB.ToString());
            }
        }

        /// <summary>
        /// Produces a string that contains all the enumerator names in an enum type.
        /// </summary>
        /// <param name="enumType"></param>
        /// <returns></returns>
        internal static string EnumAllValues(Type enumType)
        {
            string[] names = Enum.GetNames(enumType);
            string separator = ", ";
            StringBuilder returnValue = new StringBuilder();
            if (names.Length != 0)
            {
                for (int i = 0; i < names.Length; i++)
                {
                    returnValue.Append(names[i]);
                    returnValue.Append(separator);
                }

                returnValue.Remove(returnValue.Length - separator.Length, separator.Length);
            }

            return returnValue.ToString();
        }

        private static Dictionary<Type, string[]> s_specialDisambiguateCases = new Dictionary<Type, string[]>();
    }
}
