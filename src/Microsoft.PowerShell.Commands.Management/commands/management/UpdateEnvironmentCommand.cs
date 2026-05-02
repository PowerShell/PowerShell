// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implements the Update-Environment cmdlet.
    /// </summary>
    [Cmdlet(VerbsData.Update, "Environment")]
    public class UpdateEnvironmentCommand : PSCmdlet
    {
#if !UNIX
        // A list of variables that should never be overwritten
        // by static Machine or User registry reads.
        private static readonly HashSet<string> s_ignoredVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "USERNAME", "USERDOMAIN", "USERDNSDOMAIN", "USERPROFILE",
            "COMPUTERNAME", "LOGONSERVER", "HOMEDRIVE", "HOMEPATH",
            "HOMESHARE", "APPDATA", "LOCALAPPDATA", "SESSIONNAME",
            "CLIENTNAME", "PROMPT", "SYSTEMDRIVE", "SYSTEMROOT",
            "ALLUSERSPROFILE", "PUBLIC", "PROGRAMDATA", "PROGRAMFILES",
            "PROGRAMW6432", "PROGRAMFILES(X86)", "COMMONPROGRAMFILES",
            "COMMONPROGRAMFILES(X86)", "COMMONPROGRAMW6432",
            "PATH", "PSMODULEPATH",
        };
#endif

        /// <summary>
        /// Gets or sets the switch to update machine environment variables.
        /// </summary>
        [Parameter]
        public SwitchParameter Machine { get; set; }

        /// <summary>
        /// Gets or sets the switch to update user environment variables.
        /// </summary>
        [Parameter]
        public SwitchParameter User { get; set; }

#if !UNIX
        private static void AppendUniqueListSegments(List<string> destination, HashSet<string> seenSegments, string variableValue)
        {
            foreach (string segment in SplitListVariable(variableValue))
            {
                if (seenSegments.Add(segment))
                {
                    destination.Add(segment);
                }
            }
        }

        private static IEnumerable<string> SplitListVariable(string variableValue)
        {
            if (string.IsNullOrEmpty(variableValue))
            {
                yield break;
            }

            foreach (string segment in variableValue.Split(Path.PathSeparator))
            {
                if (!string.IsNullOrWhiteSpace(segment))
                {
                    yield return segment;
                }
            }
        }
#endif

        /// <summary>
        /// Executes the environment update logic.
        /// </summary>
        protected override void ProcessRecord()
        {
#if !UNIX
            bool updateAll = !Machine.IsPresent && !User.IsPresent;
            bool updateMachine = updateAll || Machine.IsPresent;
            bool updateUser = updateAll || User.IsPresent;

            if (updateMachine)
            {
                WriteVerbose("Updating Machine environment variables...");
                UpdateFromTarget(EnvironmentVariableTarget.Machine);
            }

            if (updateUser)
            {
                WriteVerbose("Updating User environment variables...");
                UpdateFromTarget(EnvironmentVariableTarget.User);
            }

            if (updateMachine || updateUser)
            {
                FixListVariable("Path", updateMachine, updateUser);
                FixListVariable("PSModulePath", updateMachine, updateUser);
            }
#else
            WriteWarning("The Update-Environment cmdlet is currently only supported on Windows.");
#endif
        }

#if !UNIX
        private void FixListVariable(string variableName, bool includeMachine, bool includeUser)
        {
            string processVal = Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Process);
            List<string> mergedSegments = new List<string>();
            HashSet<string> seenSegments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (includeMachine)
            {
                string machineVal = Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Machine);
                AppendUniqueListSegments(mergedSegments, seenSegments, machineVal);
            }

            if (includeUser)
            {
                string userVal = Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.User);
                AppendUniqueListSegments(mergedSegments, seenSegments, userVal);
            }

            int registrySegmentCount = mergedSegments.Count;

            // Preserve entries that exist only in the current process value
            AppendUniqueListSegments(mergedSegments, seenSegments, processVal);

            string mergedValue = string.Join(Path.PathSeparator.ToString(), mergedSegments);

            if (!string.Equals(mergedValue, processVal, StringComparison.OrdinalIgnoreCase))
            {
                Environment.SetEnvironmentVariable(variableName, mergedValue, EnvironmentVariableTarget.Process);
                if (mergedSegments.Count > registrySegmentCount)
                {
                    WriteVerbose($"Merged selected environment targets for {variableName} and preserved process-only entries");
                }
                else
                {
                    WriteVerbose($"Merged selected environment targets for {variableName}");
                }
            }
        }

        private void UpdateFromTarget(EnvironmentVariableTarget target)
        {
            try
            {
                IDictionary envVars = Environment.GetEnvironmentVariables(target);

                foreach (DictionaryEntry entry in envVars)
                {
                    string key = (string)entry.Key;
                    string value = (string)entry.Value;

                    if (s_ignoredVariables.Contains(key))
                    {
                        continue;
                    }

                    string currentValue = Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process);

                    // Log if the variable is new or value has been changed.
                    if (currentValue != value)
                    {
                        if (currentValue == null)
                        {
                            WriteVerbose($"Added {target} variable: {key}");
                        }
                        else
                        {
                            WriteVerbose($"Updated {target} variable: {key}");
                        }

                        // Update the environment variable for the current process.
                        Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteWarning($"Failed to update environment variables from target {target}: {ex.Message}");
            }
        }
#endif
    }
}
