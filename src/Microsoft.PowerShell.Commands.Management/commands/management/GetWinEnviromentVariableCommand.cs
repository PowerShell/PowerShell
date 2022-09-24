// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Defines the implementation of the 'Get-WinEnviromentVariable' cmdlet.
    /// This cmdlet get the content from EnvironemtVariable.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "WinEnvironmentVariable", DefaultParameterSetName = "DefaultSet")]
    [OutputType(typeof(PSObject), ParameterSetName = new[] { "DefaultSet" })]
    [OutputType(typeof(string), ParameterSetName = new[] { "RawSet" })]
    public class GetWinEnvironmentVariableCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets specifies the Name EnvironmentVariable.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = "DefaultSet", Mandatory = false, ValueFromPipelineByPropertyName = true)]
        [Parameter(Position = 0, ParameterSetName = "RawSet", Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the EnvironmentVariableTarget.
        /// </summary>
        [Parameter(Position = 1, Mandatory = false, ParameterSetName = "DefaultSet")]
        [Parameter(Position = 1, Mandatory = false, ParameterSetName = "RawSet")]
        [ValidateNotNullOrEmpty]
        public EnvironmentVariableTarget Target { get; set; } = EnvironmentVariableTarget.Process;
        
        /// <summary>
        /// Gets or sets property that sets delimiter.
        /// </summary>
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, ParameterSetName = "DefaultSet")]
        [ValidateNotNullOrEmpty]
        public char? Delimiter { get; set; } = null;

        /// <summary>
        /// Gets or sets raw parameter. This will allow EnvironmentVariable return text or file list as one string.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "RawSet")]
        public SwitchParameter Raw { get; set; }

        private static readonly List<string> DetectedDelimiterEnvrionmentVariable = new List<string>{ "Path", "PATHEXT", "PSModulePath" };

        /// <summary>
        /// This method implements the ProcessRecord method for Get-WinEnvironmentVariable command.
        /// Returns the Specify Name EnvironmentVariable content as text format.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (string.IsNullOrEmpty(Name))
            {
                Dictionary<string, string> abc = new Dictionary<string, string>();

                foreach (DictionaryEntry kvp in Environment.GetEnvironmentVariables(Target))
                {
                    PSObject env = new PSObject();
                    PSNoteProperty envname = new PSNoteProperty("Name", kvp.Key.ToString());
                    PSNoteProperty envvalue = new PSNoteProperty("Value", kvp.Value.ToString());
                    env.Properties.Add(envname);
                    env.Properties.Add(envvalue);

                    this.WriteObject(env, true);
                }

                return;
            }

            var contentList = new List<string>();

            string textContent = Environment.GetEnvironmentVariable(Name, Target);

            if (string.IsNullOrEmpty(textContent))
            {
                var message = StringUtil.Format(
                    WinEnvironmentVariableResources.EnvironmentVariableNotFound, Name);

                ArgumentException argumentException = new ArgumentException(message);
                ErrorRecord errorRecord = new ErrorRecord(
                    argumentException,
                    "EnvironmentVariableNotFound",
                    ErrorCategory.ObjectNotFound,
                    Name);
                ThrowTerminatingError(errorRecord);
                return;
            }

            if (ParameterSetName == "RawSet")
            {
                contentList.Add(textContent);
                this.WriteObject(textContent, true);
                return;
            }
            else
            {
                if (DetectedDelimiterEnvrionmentVariable.Contains(Name))
                {
                    Delimiter = Path.PathSeparator;
                }

                contentList.AddRange(textContent.Split(Delimiter.ToString() ?? string.Empty, StringSplitOptions.None));
            }

            PSObject result = new PSObject();
            PSNoteProperty member = new PSNoteProperty("Name", Name);
            PSNoteProperty member2 = new PSNoteProperty("Value", contentList);

            result.Properties.Add(member);
            result.Properties.Add(member2);

            this.WriteObject(result, true);
        }
    }
}

#endif
