/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;
using System.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements Get-Uptime.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Uptime", DefaultParameterSetName = "Timespan", HelpUri = "")]
    public class GetUptimeCommand : PSCmdlet
    {
        /// <summary>
        /// Timespan parameter
        /// Time since the system started up
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = "Timespan")]
        public SwitchParameter Timespan { get; set; } = new SwitchParameter();

        /// <summary>
        /// Since parameter
        /// The system startup time
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = "Since")]
        public SwitchParameter Since { get; set; } = new SwitchParameter();

        /// <summary>
        /// Pretty parameter
        /// Convert output to default formated string
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = "Timespan")]
        [Parameter(ParameterSetName = "Since")]
        public SwitchParameter Pretty { get; set; } = new SwitchParameter();

        /// <summary>
        /// ProcessRecord() override.
        /// This is the main entry point for the cmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            // Get-Uptime return null if IsHighResolution = false  
            // because stopwatch.GetTimestamp() return DateTime.UtcNow.Ticks
            // instead of ticks from system startup
            if (System.Diagnostics.Stopwatch.IsHighResolution)
            {
                TimeSpan uptime = TimeSpan.FromSeconds(System.Diagnostics.Stopwatch.GetTimestamp()/System.Diagnostics.Stopwatch.Frequency); 
    
                switch (ParameterSetName)
                {
                    case "Timespan":
                        if (Pretty.IsPresent) {
                            WriteObject(uptime.ToString());
                        } 
                        else
                        {
                            // return TimeSpan of time since the system started up
                            WriteObject(uptime);
                        }
                        break;
                    case "Since":
                        if (Pretty.IsPresent) {
                            // return formated string when the system started up
                            WriteObject(System.DateTime.Now.Subtract(uptime).ToString("F"));
                        } 
                        else
                        {
                            // return Datetime when the system started up
                            WriteObject(System.DateTime.Now.Subtract(uptime));
                        }
                        break;
                }
            }
        }
   }
}