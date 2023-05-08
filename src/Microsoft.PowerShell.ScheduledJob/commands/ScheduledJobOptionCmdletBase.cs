// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.ScheduledJob
{
    /// <summary>
    /// Base class for NewScheduledJobOption, SetScheduledJobOption cmdlets.
    /// </summary>
    public abstract class ScheduledJobOptionCmdletBase : ScheduleJobCmdletBase
    {
        #region Parameters

        /// <summary>
        /// Options parameter set name.
        /// </summary>
        protected const string OptionsParameterSet = "Options";

        /// <summary>
        /// Scheduled job task is run with elevated privileges when this switch is selected.
        /// </summary>
        [Parameter(ParameterSetName = ScheduledJobOptionCmdletBase.OptionsParameterSet)]
        public SwitchParameter RunElevated
        {
            get { return _runElevated; }

            set { _runElevated = value; }
        }

        private SwitchParameter _runElevated = false;

        /// <summary>
        /// Scheduled job task is hidden in Windows Task Scheduler when true.
        /// </summary>
        [Parameter(ParameterSetName = ScheduledJobOptionCmdletBase.OptionsParameterSet)]
        public SwitchParameter HideInTaskScheduler
        {
            get { return _hideInTaskScheduler; }

            set { _hideInTaskScheduler = value; }
        }

        private SwitchParameter _hideInTaskScheduler = false;

        /// <summary>
        /// Scheduled job task will be restarted when machine becomes idle.  This is applicable
        /// only if the job was configured to stop when no longer idle.
        /// </summary>
        [Parameter(ParameterSetName = ScheduledJobOptionCmdletBase.OptionsParameterSet)]
        public SwitchParameter RestartOnIdleResume
        {
            get { return _restartOnIdleResume; }

            set { _restartOnIdleResume = value; }
        }

        private SwitchParameter _restartOnIdleResume = false;

        /// <summary>
        /// Provides task scheduler options for multiple running instances of the job.
        /// </summary>
        [Parameter(ParameterSetName = ScheduledJobOptionCmdletBase.OptionsParameterSet)]
        public TaskMultipleInstancePolicy MultipleInstancePolicy
        {
            get { return _multipleInstancePolicy; }

            set { _multipleInstancePolicy = value; }
        }

        private TaskMultipleInstancePolicy _multipleInstancePolicy = TaskMultipleInstancePolicy.IgnoreNew;

        /// <summary>
        /// Prevents the job task from being started manually via Task Scheduler UI.
        /// </summary>
        [Parameter(ParameterSetName = ScheduledJobOptionCmdletBase.OptionsParameterSet)]
        public SwitchParameter DoNotAllowDemandStart
        {
            get { return _doNotAllowDemandStart; }

            set { _doNotAllowDemandStart = value; }
        }

        private SwitchParameter _doNotAllowDemandStart = false;

        /// <summary>
        /// Allows the job task to be run only when network connection available.
        /// </summary>
        [Parameter(ParameterSetName = ScheduledJobOptionCmdletBase.OptionsParameterSet)]
        public SwitchParameter RequireNetwork
        {
            get { return _requireNetwork; }

            set { _requireNetwork = value; }
        }

        private SwitchParameter _requireNetwork = false;

        /// <summary>
        /// Stops running job started by Task Scheduler if computer is no longer idle.
        /// </summary>
        [Parameter(ParameterSetName = ScheduledJobOptionCmdletBase.OptionsParameterSet)]
        public SwitchParameter StopIfGoingOffIdle
        {
            get { return _stopIfGoingOffIdle; }

            set { _stopIfGoingOffIdle = value; }
        }

        private SwitchParameter _stopIfGoingOffIdle = false;

        /// <summary>
        /// Will wake the computer to run the job if computer is in sleep mode when
        /// trigger activates.
        /// </summary>
        [Parameter(ParameterSetName = ScheduledJobOptionCmdletBase.OptionsParameterSet)]
        public SwitchParameter WakeToRun
        {
            get { return _wakeToRun; }

            set { _wakeToRun = value; }
        }

        private SwitchParameter _wakeToRun = false;

        /// <summary>
        /// Continue running task job if computer going on battery.
        /// </summary>
        [Parameter(ParameterSetName = ScheduledJobOptionCmdletBase.OptionsParameterSet)]
        public SwitchParameter ContinueIfGoingOnBattery
        {
            get { return _continueIfGoingOnBattery; }

            set { _continueIfGoingOnBattery = value; }
        }

        private SwitchParameter _continueIfGoingOnBattery = false;

        /// <summary>
        /// Will start job task even if computer is running on battery power.
        /// </summary>
        [Parameter(ParameterSetName = ScheduledJobOptionCmdletBase.OptionsParameterSet)]
        public SwitchParameter StartIfOnBattery
        {
            get { return _startIfOnBattery; }

            set { _startIfOnBattery = value; }
        }

        private SwitchParameter _startIfOnBattery = false;

        /// <summary>
        /// Specifies how long Task Scheduler will wait for idle time after a trigger has
        /// activated before giving up trying to run job during computer idle.
        /// </summary>
        [Parameter(ParameterSetName = ScheduledJobOptionCmdletBase.OptionsParameterSet)]
        public TimeSpan IdleTimeout
        {
            get { return _idleTimeout; }

            set { _idleTimeout = value; }
        }

        private TimeSpan _idleTimeout = new TimeSpan(1, 0, 0);

        /// <summary>
        /// How long the computer needs to be idle before a triggered job task is started.
        /// </summary>
        [Parameter(ParameterSetName = ScheduledJobOptionCmdletBase.OptionsParameterSet)]
        public TimeSpan IdleDuration
        {
            get { return _idleDuration; }

            set { _idleDuration = value; }
        }

        private TimeSpan _idleDuration = new TimeSpan(0, 10, 0);

        /// <summary>
        /// Will start job task if machine is idle.
        /// </summary>
        [Parameter(ParameterSetName = ScheduledJobOptionCmdletBase.OptionsParameterSet)]
        public SwitchParameter StartIfIdle
        {
            get { return _startIfIdle; }

            set { _startIfIdle = value; }
        }

        private SwitchParameter _startIfIdle = false;

        #endregion

        #region Cmdlet Overrides

        /// <summary>
        /// Begin processing.
        /// </summary>
        protected override void BeginProcessing()
        {
            // Validate parameters.
            if (MyInvocation.BoundParameters.ContainsKey(nameof(IdleTimeout)) &&
                _idleTimeout < TimeSpan.Zero)
            {
                throw new PSArgumentException(ScheduledJobErrorStrings.InvalidIdleTimeout);
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(IdleDuration)) &&
                _idleDuration < TimeSpan.Zero)
            {
                throw new PSArgumentException(ScheduledJobErrorStrings.InvalidIdleDuration);
            }
        }

        #endregion
    }
}
