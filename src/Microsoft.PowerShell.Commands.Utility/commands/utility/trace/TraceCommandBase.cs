// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A base class for cmdlets that has helper methods for globbing trace source instances.
    /// </summary>
    public class TraceCommandBase : PSCmdlet
    {
        /// <summary>
        /// Gets the matching PSTraceSource instances for the specified patterns.
        /// </summary>
        /// <param name="patternsToMatch">
        /// The patterns used to match the PSTraceSource name.
        /// </param>
        /// <param name="writeErrorIfMatchNotFound">
        /// If true and the pattern does not contain wildcard patterns and no
        /// match is found, then WriteError will be called.
        /// </param>
        /// <returns>
        /// A collection of the matching PSTraceSource instances.
        /// </returns>
        internal Collection<PSTraceSource> GetMatchingTraceSource(
            string[] patternsToMatch,
            bool writeErrorIfMatchNotFound)
        {
            Collection<string> ignored = null;
            return GetMatchingTraceSource(patternsToMatch, writeErrorIfMatchNotFound, out ignored);
        }

        /// <summary>
        /// Gets the matching PSTraceSource instances for the specified patterns.
        /// </summary>
        /// <param name="patternsToMatch">
        /// The patterns used to match the PSTraceSource name.
        /// </param>
        /// <param name="writeErrorIfMatchNotFound">
        /// If true and the pattern does not contain wildcard patterns and no
        /// match is found, then WriteError will be called.
        /// </param>
        /// <param name="notMatched">
        /// The patterns for which a match was not found.
        /// </param>
        /// <returns>
        /// A collection of the matching PSTraceSource instances.
        /// </returns>
        internal Collection<PSTraceSource> GetMatchingTraceSource(
            string[] patternsToMatch,
            bool writeErrorIfMatchNotFound,
            out Collection<string> notMatched)
        {
            notMatched = new Collection<string>();

            Collection<PSTraceSource> results = new();
            foreach (string patternToMatch in patternsToMatch)
            {
                bool matchFound = false;

                if (string.IsNullOrEmpty(patternToMatch))
                {
                    notMatched.Add(patternToMatch);
                    continue;
                }

                WildcardPattern pattern =
                    WildcardPattern.Get(
                        patternToMatch,
                        WildcardOptions.IgnoreCase);

                Dictionary<string, PSTraceSource> traceCatalog = PSTraceSource.TraceCatalog;

                foreach (PSTraceSource source in traceCatalog.Values)
                {
                    // Try matching by full name

                    if (pattern.IsMatch(source.FullName))
                    {
                        matchFound = true;
                        results.Add(source);
                    }
                    // Try matching by the short name.
                    else if (pattern.IsMatch(source.Name))
                    {
                        matchFound = true;
                        results.Add(source);
                    }
                }

                if (!matchFound)
                {
                    notMatched.Add(patternToMatch);

                    // Only write an error if no match was found, the pattern doesn't
                    // contain wildcard characters, and caller wants us to.

                    if (writeErrorIfMatchNotFound &&
                        !WildcardPattern.ContainsWildcardCharacters(patternToMatch))
                    {
                        ItemNotFoundException itemNotFound =
                            new(
                                patternToMatch,
                                "TraceSourceNotFound",
                                SessionStateStrings.TraceSourceNotFound);

                        ErrorRecord errorRecord = new(itemNotFound.ErrorRecord, itemNotFound);
                        WriteError(errorRecord);
                    }
                }
            }

            return results;
        }
    }
}
