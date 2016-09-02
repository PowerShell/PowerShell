/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.ScheduledJob
{
    /// <summary>
    /// This cmdlet sets the provided scheduled job options to the provided ScheduledJobOptions objects.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "ScheduledJobOption", DefaultParameterSetName = ScheduledJobOptionCmdletBase.OptionsParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=223921")]
    [OutputType(typeof(ScheduledJobOptions))]
    public class SetScheduledJobOptionCommand : ScheduledJobOptionCmdletBase
    {
        #region Parameters

        /// <summary>
        /// ScheduledJobOptions object.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, 
                   ParameterSetName = ScheduledJobOptionCmdletBase.OptionsParameterSet)]
        [ValidateNotNull]
        public ScheduledJobOptions InputObject
        {
            get { return _jobOptions; }
            set { _jobOptions = value; }
        }
        private ScheduledJobOptions _jobOptions;

        /// <summary>
        /// Pas the ScheduledJobOptions object through to output.
        /// </summary>
        [Parameter(ParameterSetName = ScheduledJobOptionCmdletBase.OptionsParameterSet)]
        public SwitchParameter PassThru
        {
            get { return _passThru; }
            set { _passThru = value; }
        }
        private SwitchParameter _passThru;

        #endregion

        #region Cmdlet Overrides

        /// <summary>
        /// Process input.
        /// </summary>
        protected override void ProcessRecord()
        {
            // Update ScheduledJobOptions object with current parameters.
            // Update switch parameters only if they were selected.
            // Also update the ScheduledJobDefinition object associated with this options object.
            if (MyInvocation.BoundParameters.ContainsKey("StartIfOnBattery"))
            {
                _jobOptions.StartIfOnBatteries = StartIfOnBattery;
            }

            if (MyInvocation.BoundParameters.ContainsKey("ContinueIfGoingOnBattery"))
            {
                _jobOptions.StopIfGoingOnBatteries = !ContinueIfGoingOnBattery;
            }

            if (MyInvocation.BoundParameters.ContainsKey("WakeToRun"))
            {
                _jobOptions.WakeToRun = WakeToRun;
            }

            if (MyInvocation.BoundParameters.ContainsKey("StartIfIdle"))
            {
                _jobOptions.StartIfNotIdle = !StartIfIdle;
            }

            if (MyInvocation.BoundParameters.ContainsKey("StopIfGoingOffIdle"))
            {
                _jobOptions.StopIfGoingOffIdle = StopIfGoingOffIdle;
            }

            if (MyInvocation.BoundParameters.ContainsKey("RestartOnIdleResume"))
            {
                _jobOptions.RestartOnIdleResume = RestartOnIdleResume;
            }

            if (MyInvocation.BoundParameters.ContainsKey("HideInTaskScheduler"))
            {
                _jobOptions.ShowInTaskScheduler = !HideInTaskScheduler;
            }

            if (MyInvocation.BoundParameters.ContainsKey("RunElevated"))
            {
                _jobOptions.RunElevated = RunElevated;
            }

            if (MyInvocation.BoundParameters.ContainsKey("RequireNetwork"))
            {
                _jobOptions.RunWithoutNetwork = !RequireNetwork;
            }

            if (MyInvocation.BoundParameters.ContainsKey("DoNotAllowDemandStart"))
            {
                _jobOptions.DoNotAllowDemandStart = DoNotAllowDemandStart;
            }

            if (MyInvocation.BoundParameters.ContainsKey("IdleDuration"))
            {
                _jobOptions.IdleDuration = IdleDuration;
            }

            if (MyInvocation.BoundParameters.ContainsKey("IdleTimeout"))
            {
                _jobOptions.IdleTimeout = IdleTimeout;
            }

            if (MyInvocation.BoundParameters.ContainsKey("MultipleInstancePolicy"))
            {
                _jobOptions.MultipleInstancePolicy = MultipleInstancePolicy;
            }

            // Update ScheduledJobDefinition with changes.
            if (_jobOptions.JobDefinition != null)
            {
                _jobOptions.UpdateJobDefinition();
            }

            if (_passThru)
            {
                WriteObject(_jobOptions);
            }
        }

        #endregion
    }
}
