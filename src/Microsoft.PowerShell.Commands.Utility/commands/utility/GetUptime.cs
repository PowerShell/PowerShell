// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements Get-Uptime.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Uptime", DefaultParameterSetName = TimespanParameterSet, HelpUri = "https://go.microsoft.com/fwlink/?linkid=834862")]
    [OutputType(typeof(TimeSpan), ParameterSetName = new string[] { TimespanParameterSet })]
    [OutputType(typeof(DateTime), ParameterSetName = new string[] { SinceParameterSet })]
    public class GetUptimeCommand : PSCmdlet
    {
        /// <summary>
        /// The system startup time.
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = SinceParameterSet)]
        public SwitchParameter Since { get; set; } = new SwitchParameter();

        /// <summary>
        /// This is the main entry point for the cmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            switch (ParameterSetName)
            {
                case TimespanParameterSet:
                    ProcessTimespanParameterSet();
                    break;
                case SinceParameterSet:
                    ProcessSinceParameterSet();
                    break;
            }
        }

        /// <summary>
        /// Process the Timespan parameter set.
        /// </summary>
        /// <remarks>
        /// Outputs the time of the last system boot as a <see cref="TimeSpan"/>.
        /// </remarks>
        private void ProcessTimespanParameterSet()
        {
            TimeSpan result = TimeSpan.FromMilliseconds(Environment.TickCount64);
            WriteObject(result);
        }

        /// <summary>
        /// Process the Since parameter set.
        /// </summary>
        /// <remarks>
        /// Outputs the time elapsed since the last system boot as a <see cref="DateTime"/>.
        /// </remarks>
        private void ProcessSinceParameterSet()
        {
            DateTime result = DateTime.Now.Subtract(TimeSpan.FromMilliseconds(Environment.TickCount64));
            WriteObject(result);
        }

        /// <summary>
        /// Parameter set name for Timespan OutputType.
        /// </summary>
        private const string TimespanParameterSet = "Timespan";

        /// <summary>
        /// Parameter set name for DateTime OutputType.
        /// </summary>
        private const string SinceParameterSet = "Since";
    }
}
