// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if UNIX
#nullable enable

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Text.RegularExpressions;

using System.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// Class ProviderHelpProvider implement the help provider for manpages.
    /// </summary>
    internal class ManpageHelpProvider : HelpProviderWithCache
    {
        /// <summary>
        /// Constructor for HelpProvider.
        /// </summary>
        internal ManpageHelpProvider(HelpSystem helpSystem) : base(helpSystem)
        {
            _sessionState = helpSystem.ExecutionContext.SessionState;
        }

        private readonly SessionState _sessionState;

        [TraceSource("ManpageHelpProvider", "ManpageHelpProvider")]
        private static readonly PSTraceSource s_tracer = PSTraceSource.GetTracer("ManpageHelpProvider", "ManpageHelpProvider");

        #region Common Properties

        /// <summary>
        /// Name of this help provider.
        /// </summary>
        /// <value>Name of this help provider.</value>
        internal override string Name
        {
            get =>  "Manpage Help Provider";
        }

        /// <summary>
        /// Help category of this provider.
        /// </summary>
        /// <value>Help category of this provider.</value>
        internal override HelpCategory HelpCategory
        {
            get => HelpCategory.Manpage;
        }

        #endregion

        #region Help Provider Interface

        /// <summary>
        /// Do exact match help based on the target.
        /// </summary>
        /// <param name="helpRequest">Help request object.</param>
        internal override IEnumerable<HelpInfo?> ExactMatchHelp(HelpRequest helpRequest)
        {
            foreach (ManpageInfo manpage in ManpageSearch(helpRequest.Target))
            {
                yield return ManpageHelpInfo.GetHelpInfo(manpage);
            }
        }

        /// <summary>
        /// Return manpage info that sutistfies a given target pattern one by one.
        /// </summary>
        /// <param name="pattern">Manpage search eacrh patter.</param>
        internal static Collection<ManpageInfo> ManpageSearch(string pattern)
        {
            Collection<ManpageInfo> matchingManpageInfo = new Collection<ManpageInfo>();

            // Try first to find the exact command name
            matchingManpageInfo = ManpageSearchByProc(pattern, true);

            // If exact command not found, find all commands names that include `pattern`
            if (matchingManpageInfo.Count is 0) {
                matchingManpageInfo = ManpageSearchByProc(pattern, false);
            }

            return matchingManpageInfo;
        }

        // Get short help for `pattern` from commands using `proc`;
        // `exact` indicates whether exact `pattern` should be found.
        private static Collection<ManpageInfo> ManpageSearchByProc(string pattern, bool exact)
        {
            Collection<ManpageInfo> matchingManpageInfo = new Collection<ManpageInfo>();

            try
            {
                using var manProc = new Process();
                manProc.StartInfo.UseShellExecute = false;
                manProc.StartInfo.RedirectStandardInput = false;
                manProc.StartInfo.RedirectStandardOutput = true;
                manProc.StartInfo.RedirectStandardError = true;
                manProc.StartInfo.UseShellExecute = false;
                manProc.StartInfo.CreateNoWindow = true;

                // Choose get help line depending on whether exact pattern should be matched.
                // `whatis` is used for exact matching as MacOs does not support `apropos -e`);
                // `apropos` is used to find all commands including `pattern` as MacOs does not support `whatis -w`)
                string specificCommandPattern;
                if (exact)
                {
                    manProc.StartInfo.FileName = "whatis";
                    manProc.StartInfo.Arguments = pattern;
                    specificCommandPattern = pattern;
                }
                else
                {
                    manProc.StartInfo.FileName = "apropos";
                    // Convert the PS wildcards pattern into RegEx required by `apropos`
                    string re  = psWildcardsToRegEx(pattern);
                    manProc.StartInfo.Arguments = "^" + re;     // Oneline help that start with the `pattern`
                    specificCommandPattern = '^' + re + '$';    // Comamnd name that match `pattern` from start to end
                }

                var patternRegEx = new Regex(specificCommandPattern, RegexOptions.IgnoreCase);

                manProc.Start();
                while (!manProc.StandardOutput.EndOfStream)
                {
                    string? line = manProc.StandardOutput.ReadLine();
                    
                    // Ensure that entry is not empty 
                    if (!string.IsNullOrEmpty(line) && line.Trim().Length > 0)
                    {
                        // `whatis` line is: "name (section)  - short deswcription"
                        string[] lineParts = line.Trim().Split(" -", 2);

                        // Determine "name" and "section" from "name (section)"
                        char[] delim = {'(', ')'};
                        string[] nameParts = lineParts[0].Trim().Split(delim);
                        string? manSectionNum = null;
                        if (nameParts.Length > 1)   // If section part is available
                        {
                            string sec = nameParts[1].Trim();
                            if (sec.Length > 0)
                            {
                                string execManSections = "168"; // Man executables sections
                                if (execManSections.Contains(sec[0]))
                                {
                                    manSectionNum = sec;
                                }
                            }
                        }

                        // Add only exceutables to the list
                        if (manSectionNum != null)
                        {
                            string commandName = nameParts[0].Trim();
                            // Ensur that `pattern` is included in the name, as `apropos` searchs also the description line
                            if (patternRegEx.IsMatch(commandName)) {
                                string commandShortDescription = string.Empty;
                                if (lineParts.Length > 1)
                                {
                                    commandShortDescription = lineParts[1].Trim();
                                }

                                ManpageInfo manpageInfo = new ManpageInfo(commandName, manSectionNum, commandShortDescription);
                                matchingManpageInfo.Add(manpageInfo);
                            }
                        }
                    }
                }
                manProc.Close();
            }
            catch (Exception e)
            {
                // If getting help from man pages failed, just log an error
                s_tracer.WriteLine("Failed to get *nix manpage data for '{0}' - {1}", pattern, e.Message);
            }

            return matchingManpageInfo;
        }

        // Convert powershell string with wildcards to RexEx
        private static string psWildcardsToRegEx(string pattern)
        {
            string s = string.Empty;     // start pattern from line start as PS does not search for patterns "included"
            foreach (char c in pattern) {
                if (c == '*' || c == '?') {
                    s += '.';
                }
                s += c;
            }

            return s;
        }

        #endregion
    }
}

#endif
