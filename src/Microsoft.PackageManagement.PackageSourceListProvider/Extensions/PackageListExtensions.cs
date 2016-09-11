#if !UNIX

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.PackageManagement.Internal.Implementation;

namespace Microsoft.PackageManagement.PackageSourceListProvider
{

    internal static class ExceptionExtensions
    {
        internal static void Dump(this Exception ex, Request request)
        {
            request.Debug(ex.ToString());
        }
    }

    internal static class StringExtensions
    {
        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Formats the specified format string.
        /// </summary>
        /// <param name="formatString"> The format string. </param>
        /// <param name="args"> The args. </param>
        /// <returns> </returns>
        /// <remarks>
        /// </remarks>
        public static string format(this string formatString, params object[] args)
        {
            if (args == null || args.Length == 0)
            {
                return formatString;
            }

            try
            {
                var replacedByName = false;
                // first, try to replace
                formatString = new Regex(@"\$\{(?<macro>\w*?)\}").Replace(formatString, new MatchEvaluator((m) => {
                    var key = m.Groups["macro"].Value;

                    var p = args[0].GetType().GetTypeInfo().GetProperty(key);
                    if (p != null)
                    {
                        replacedByName = true;
                        return p.GetValue(args[0], null).ToString();
                    }
                    return "${{" + m.Groups["macro"].Value + "}}";
                }));

                // if it looks like it doesn't take parameters, (and yet we have args!)
                // let's return a fix-me-format string.
                if (!replacedByName && formatString.IndexOf('{') < 0)
                {
                    return FixMeFormat(formatString, args);
                }

                return String.Format(CultureInfo.CurrentCulture, formatString, args);
            }
            catch (Exception)
            {
                // if we got an exception, let's at least return a string that we can use to figure out what parameters should have been matched vs what was passed.
                return FixMeFormat(formatString, args);
            }
        }

        private static string FixMeFormat(string formatString, object[] args)
        {
            if (args == null || args.Length == 0)
            {
                // not really any args, and not really expecting any
                return formatString.Replace('{', '\u00ab').Replace('}', '\u00bb');
            }
            return args.Aggregate(formatString.Replace('{', '\u00ab').Replace('}', '\u00bb'), (current, arg) => current + string.Format(CultureInfo.CurrentCulture, " \u00ab{0}\u00bb", arg));
        }

    }

    internal static class DictionaryExtensions
    {
        internal static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            return dictionary.ContainsKey(key) ? dictionary[key] : default(TValue);
        }
    }
}

#endif
