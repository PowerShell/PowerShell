#if UNIX

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;

using System.Diagnostics;

#nullable enable

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

        #region Common Properties

        /// <summary>
        /// Name of this help provider.
        /// </summary>
        /// <value>Name of this help provider.</value>
        internal override string Name
        {
            get
            {
                return "Manpage Help Provider";
            }
        }

        /// <summary>
        /// Help category of this provider.
        /// </summary>
        /// <value>Help category of this provider</value>
        internal override HelpCategory HelpCategory
        {
            get
            {
                return HelpCategory.Manpage;
            }
        }

        #endregion

        #region Help Provider Interface

        /// <summary>
        /// Do exact match help based on the target.
        /// </summary>
        /// <param name="helpRequest">Help request object.</param>
        internal override IEnumerable<HelpInfo> ExactMatchHelp(HelpRequest helpRequest)
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
        public static IEnumerable<ManpageInfo> ManpageSearch(string pattern)
        {
            Collection<ManpageInfo> matchingManpageInfo = new Collection<ManpageInfo>();

            // Get short help from commands using whatis(1)
            try
            {
                using var manProc = new Process();
                manProc.StartInfo.UseShellExecute = false;
                manProc.StartInfo.RedirectStandardInput = false;
                manProc.StartInfo.RedirectStandardOutput = true;
                manProc.StartInfo.RedirectStandardError = false;
                manProc.StartInfo.UseShellExecute = false;
                manProc.StartInfo.CreateNoWindow = true;
                manProc.StartInfo.FileName = "whatis";
                manProc.StartInfo.Arguments = "-w " + pattern;

                manProc.Start();
                while (!manProc.StandardOutput.EndOfStream)
                {
                    string? line = manProc.StandardOutput.ReadLine();
                    if (!string.IsNullOrEmpty(line) && line.Trim().Length > 0)
                    {
                        // `whatis` line is: "name (section)  - short deswcription"
                        string[] lineParts = line.Trim().Split(" -", 2);

                        // Determine "name" and "section" from "name (section)"
                        char[] delim = {'(', ')'};
                        string[] nameParts = lineParts[0].Trim().Split(delim);
                        string? manSectionNum = null;
                        if (nameParts.Length == 1)
                        {
                            manSectionNum = string.Empty;   // No section - assume execultable
                        }
                        else if (nameParts.Length > 1)
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
            catch (Exception)
            {
                // If getting help from man pages failed, do nothing
                // ????? Should write error using WriteLog()? Similar to Cmdlet.WriteError()? ??????
            }

            foreach (ManpageInfo manpageInfo in matchingManpageInfo)
            {
                yield return manpageInfo;
            }
        }

        #endregion
    }
}

#endif
