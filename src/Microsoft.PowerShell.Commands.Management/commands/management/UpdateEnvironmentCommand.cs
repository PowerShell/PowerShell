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
        // A list of variables that should never be overwritten
        // by static Machine or User registry reads.
        private static readonly HashSet<string> _ignoredVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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

        /// <summary>
        /// Executes the environment update logic.
        /// </summary>
        protected override void ProcessRecord()
        {
            // If neither switch is specified, will default to updating both.
            bool updateAll = !Machine.IsPresent && !User.IsPresent;

            if (updateAll || Machine.IsPresent)
            {
                WriteVerbose("Updating Machine environment variables...");
                UpdateFromTarget(EnvironmentVariableTarget.Machine);
            }
            
            if (updateAll || User.IsPresent)
            {
                WriteVerbose("Updating User environment variables...");
                UpdateFromTarget(EnvironmentVariableTarget.User);
            }

            if (updateAll || (Machine.IsPresent && User.IsPresent))
            {
                FixListVariable("Path");
                FixListVariable("PSModulePath");
            }
        }

        private void FixListVariable(string variableName)
        {
            string machineVal = Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Machine);
            string userVal = Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.User);

            if (!string.IsNullOrEmpty(machineVal) && !string.IsNullOrEmpty(userVal))
            {
                string combinedValue = machineVal + Path.PathSeparator + userVal;
                string processVal = Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Process);

                if (!string.Equals(combinedValue, processVal, StringComparison.OrdinalIgnoreCase))
                {
                    Environment.SetEnvironmentVariable(variableName, combinedValue, EnvironmentVariableTarget.Process);
                    WriteVerbose($"Merged User and Machine values for {variableName}");
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

                    if (_ignoredVariables.Contains(key))
                    {
                        continue;
                    }

                    string currentValue = Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process);

                    // Log if the variable is new or value has been changed.
                    if (currentValue != value)
                    {
                        if (currentValue == null)
                        {
                            WriteVerbose($"Added {target} variable: {key} = '{value}'");
                        }
                        else
                        {
                            WriteVerbose($"Updated {target} variable: {key} from '{currentValue}' to '{value}'");
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
    }
}
