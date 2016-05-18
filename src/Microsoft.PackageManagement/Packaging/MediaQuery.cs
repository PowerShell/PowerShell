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

namespace Microsoft.PackageManagement.Internal.Packaging {
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Text.RegularExpressions;
    using Microsoft.PackageManagement.Internal.Utility.Versions;

    public class MediaQuery {
#if MEDIA_QUERY_DOCUMENTATION
         An expression that the document evaluator can use to determine if the
        target of the link is applicable to the current platform (the host
        environment)

        Used as an optimization hint to notify a system that it can
        ignore something when it's not likely to be used.

        The format of this string is modeled upon the MediaQuery definition at
        http://www.w3.org/TR/css3-mediaqueries/

        This is one or more EXPRESSIONs where the items are connected
        with an OPERATOR:

          media="EXPRESSION [[OPERATOR] [EXPRESSION]...]"

        EXPRESSION is processed case-insensitive and defined either :
          (ENVIRONMENT)
            indicates the presence of the environment
        or
          ([PREFIX-]ENVIRONMENT.ATTRIBUTE:VALUE)
            indicates a comparison of an attribute of the environment.

        ENVIRONMENT is a text identifier that specifies any software,hardware
          feature or aspect of the system the software is intended to run in.

          Common ENVIRONMENTs include (but not limited to):
            linux
            windows
            java
            powershell
            ios
            chipset
            peripheral

        ATTRIBUTE is a property of an ENVIRONMENT with a specific value.
          Common attributes include (but not limited to):
            version
            vendor
            architecture

        PREFIX is defined as one of:
          MIN    # property has a minimum value of VALUE
          MAX    # property has a maximum value of VALUE

          if a PREFIX is not provided, then the property should equal VALUE

        OPERATOR is defined of one of:
          AND
          NOT
          OR

        Examples:
          media="(windows)"
              // applies to only systems that identify themselves as 'Windows'

          media="(windows) not (windows.architecture:x64)"
              // applies to only systems that identify
              // themselves as windows and are not for an x64 cpu

          media="(windows) and (min-windows.version:6.1)"
              // applies to systems that identify themselves as
              // windows and at least version 6.1

          media="(linux) and (linux.vendor:redhat) and (min-linux.kernelversion:3.0)"
              // applies to systems that identify themselves as
              // linux, made by redhat and with a kernel version of at least 3.0

          media="(freebsd) and (min-freebsd.kernelversion:6.6)"
              // applies to systems that identify themselves as
              // freebsd, with a kernel version of at least 6.6

          media="(powershell) and (min-powershell.version:3.0)"
              // applies to systems that have powershell 3.0 or greater

        Properties are expected to be able to be resolved by the host
        environment without having to do significant computation.

#endif
        private const RegexOptions _flags = RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture;

        internal const string Environment = "(?<environment>[a-z0-9]+)";
        internal const string Prefix = "(((?<prefix>min)|(?<prefix>max))-)?";
        internal const string Attribute = "(?<attribute>[a-z0-9]+)";
        internal const string Value = "(?<value>[a-z0-9\\.]+)";

        internal static string AttributeEnvironmentValue = string.Format(CultureInfo.InvariantCulture, "(?<attributeenvironmentvalue>{0}{1}\\.{2}:{3})", Prefix, Environment, Attribute, Value);
        internal static string Expression = string.Format(CultureInfo.InvariantCulture, "(?<expression>{0}|{1})", Environment, AttributeEnvironmentValue);

        internal static string Tokenizer = "(?<open>\\()|(?<close>\\))|(?<not>not)|(?<or>or)|(?<and>and)|(?<txt>[\\w:\\-\\.]+)";

        private static readonly Regex _tokenizerRegex = new Regex(Tokenizer, _flags);
        private static readonly Regex _expressionRegex = new Regex(string.Concat("^", Expression, "$"), _flags);

        private static bool ParseAndProcessExpression(Match expressionMatch, Hashtable environment)
        {
            string environmentName = expressionMatch.Groups["environment"].Value.ToLowerInvariant();

            // environment name must not be null in media element
            if (string.IsNullOrWhiteSpace(environmentName))
            {
                return false;
            }

            // current environment does not contain this environmentname element
            // we assume that this is not satisfied by the current environment
            if (!environment.ContainsKey(environmentName))
            {
                return false;
            }

            Hashtable environmentDetail = environment[environmentName] as Hashtable;

            // no details return false as no information to proceed
            if (environmentDetail == null)
            {
                return false;
            }

            string environmentAttribute = expressionMatch.Groups["attribute"].Value.ToLowerInvariant();

            // this is the case where there is an environment attribute attached
            if (!string.IsNullOrWhiteSpace(environmentAttribute))
            {
                // no information about the attribute, can't verify
                if (!environmentDetail.ContainsKey(environmentAttribute))
                {
                    return false;
                }

                // this is the expected environmental value
                string expectedEnvironmentValue = expressionMatch.Groups["value"].Value.ToLowerInvariant();

                // if there is attribute then there must be value, so return false if no value
                if (string.IsNullOrWhiteSpace(expectedEnvironmentValue))
                {
                    return false;
                }

                string currentEnvironmentalValue = environmentDetail[environmentAttribute].ToString();
                Version currentVersion;
                Double currentDouble;
                int comparison = 0;

                // check whether currentEnvironmentalValue is a version
                if (Version.TryParse(currentEnvironmentalValue, out currentVersion))
                {
                    // this means the value should be version as well
                    Version expectedVersion;

                    if (!Version.TryParse(expectedEnvironmentValue, out expectedVersion))
                    {
                        return false;
                    }

                    comparison = ((FourPartVersion)expectedVersion).CompareTo((FourPartVersion)currentVersion);
                }
                else if (Double.TryParse(currentEnvironmentalValue, out currentDouble))
                {
                    // this means value should be double as well
                    Double expectedDouble;

                    if (!Double.TryParse(expectedEnvironmentValue, out expectedDouble))
                    {
                        return false;
                    }

                    comparison = expectedDouble.CompareTo(currentDouble);
                }
                else
                {
                    // just compare them as string then
                    comparison = string.Compare(expectedEnvironmentValue, currentEnvironmentalValue, StringComparison.OrdinalIgnoreCase);
                }

                // check whether there is prefix
                if (expressionMatch.Groups["prefix"].Success)
                {
                    // if there is prefix then the value can be compared
                    if (string.Equals(expressionMatch.Groups["prefix"].Value, "max", System.StringComparison.OrdinalIgnoreCase))
                    {
                        // max case which means expectedValue >= currentValue
                        return comparison >= 0;
                    }
                    else if (string.Equals(expressionMatch.Groups["prefix"].Value, "min", System.StringComparison.OrdinalIgnoreCase))
                    {
                        // min case which means expectedValue <= currentValue
                        return comparison <= 0;
                    }

                    // unknown prefix
                    return false;
                }

                // no prefix then expectedValue and currentValue should be the same
                return comparison == 0;
            }

            // no attribute so we just have a standalone environment like "windows"
            // if we reach this point then we already verify that "windows" is already in environment dictionary
            return true;
        }

        private static bool EvaluateMediaString(MatchCollection matches, Hashtable environment, ref int currentIndex, ref bool malformed)
        {
            if (currentIndex >= matches.Count)
            {
                malformed = true;
                return false;
            }

            // needs a parenthesis here at the start
            Stack parenthesisStack = new Stack();

            Match match = matches[currentIndex];

            if (!match.Groups["open"].Success)
            {
                malformed = true;
                return false;
            }

            parenthesisStack.Push(true);
            currentIndex += 1;

            bool result = false;

            while (currentIndex < matches.Count && parenthesisStack.Count != 0)
            {
                match = matches[currentIndex];
                currentIndex += 1;
                bool isMalformed = false;
                // if this boolean is set to true, then we perform cleanup and returns false
                // we clean up by getting to the end of whatever opening parenthesis we open
                bool cleanup = false;

                if (match.Groups["close"].Success)
                {
                    // malformed because missing closing parenthesis
                    if (parenthesisStack.Count == 0)
                    {
                        malformed = true;
                        return false;
                    }

                    parenthesisStack.Pop();
                }
                else if (match.Groups["open"].Success)
                {
                    parenthesisStack.Push(true);
                }
                else if (match.Groups["txt"].Success)
                {
                    Match expressionMatch = _expressionRegex.Match(match.Groups["txt"].Value);
                    result = ParseAndProcessExpression(expressionMatch, environment);
                }
                else if (match.Groups["and"].Success || match.Groups["not"].Success)
                {
                    bool evaluated = EvaluateMediaString(matches, environment, ref currentIndex, ref isMalformed);

                    // malformed so just returns
                    if (isMalformed)
                    {
                        malformed = true;
                        return false;
                    }

                    // there are 2 cases that we can proceed, either we evaluate to true with and or we evaluate to false with not.
                    // otherwise return false.
                    if ((evaluated && match.Groups["and"].Success)
                        || (!evaluated && match.Groups["not"].Success))
                    {
                        result = true;
                        continue;
                    }

                    // reach here then result is false. Since this is and/not, there is no need to evaluate any more so set cleanup to true
                    result = false;
                    cleanup = true;
                }
                else if (match.Groups["or"].Success)
                {
                    // match the or case
                    bool evaluated = EvaluateMediaString(matches, environment, ref currentIndex, ref isMalformed);

                    if (isMalformed)
                    {
                        malformed = true;
                        return false;
                    }

                    // if true, since this is or case, set result to true and just clean up since there is no need to evaluate further
                    if (evaluated)
                    {
                        result = true;
                        cleanup = true;
                    }
                    
                    // otherwise we continue evaluating
                }

                if (cleanup)
                {
                    // if we reach here then we know result is false, so just keep iterating til the end parenthesis
                    while (parenthesisStack.Count != 0)
                    {
                        // reached the end without reaching the final closing parenthesis
                        if (currentIndex >= matches.Count)
                        {
                            malformed = true;
                            return false;
                        }

                        match = matches[currentIndex];
                        currentIndex += 1;

                        if (match.Groups["open"].Success)
                        {
                            parenthesisStack.Push(true);
                        }
                        else if (match.Groups["close"].Success)
                        {
                            // malformed because missing closing parenthesis
                            if (parenthesisStack.Count == 0)
                            {
                                malformed = true;
                                return false;
                            }

                            parenthesisStack.Pop();
                        }
                    }

                    return result;
                }
            }

            // if we reached this point and there is still something in the stack
            // then it is malformed
            if (parenthesisStack.Count != 0)
            {
                malformed = true;
                return false;
            }

            return result;
        }

        /// <summary>
        /// Parse a media query and checks whether the hashtable environment supplied
        /// satisfies the query. See documentation above for more details.
        /// </summary>
        /// <param name="mediaQuery"></param>
        /// <param name="environment"></param>
        /// <returns></returns>
        public static bool IsApplicable(string mediaQuery, Hashtable environment) {
            // no media query given or empty environment table then don't check
            if (string.IsNullOrWhiteSpace(mediaQuery) || environment == null || environment.Count == 0)
            {
                return true;
            }

            // append ( and ) to make it easier to parse
            mediaQuery = string.Concat("(", mediaQuery, ")");

            int currentIndex = 0;

            bool isMalformed = false;

            // if we evaluated to true and the string is not malformed, return true.
            if (EvaluateMediaString(_tokenizerRegex.Matches(mediaQuery.Trim()), environment, ref currentIndex, ref isMalformed) && !isMalformed)
            {
                return true;
            }

            return false;
        }
    }
}