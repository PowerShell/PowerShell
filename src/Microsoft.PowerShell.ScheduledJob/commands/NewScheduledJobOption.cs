// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.ScheduledJob
{
    /// <summary>
    /// This cmdlet creates a new scheduled job option object based on the provided
    /// parameter values.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "ScheduledJobOption", DefaultParameterSetName = ScheduledJobOptionCmdletBase.OptionsParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=223919")]
    [OutputType(typeof(ScheduledJobOptions))]
    public sealed class NewScheduledJobOptionCommand : ScheduledJobOptionCmdletBase
    {
        #region Cmdlet Overrides

        /// <summary>
        /// Process input.
        /// </summary>
        protected override void ProcessRecord()
        {
            WriteObject(new ScheduledJobOptions(
                StartIfOnBattery,
                !ContinueIfGoingOnBattery,
                WakeToRun,
                !StartIfIdle,
                StopIfGoingOffIdle,
                RestartOnIdleResume,
                IdleDuration,
                IdleTimeout,
                !HideInTaskScheduler,
                RunElevated,
                !RequireNetwork,
                DoNotAllowDemandStart,
                MultipleInstancePolicy));
        }

        #endregion
    }
}
