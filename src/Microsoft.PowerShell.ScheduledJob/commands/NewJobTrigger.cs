// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Host;
using System.Threading;
using System.Diagnostics;

namespace Microsoft.PowerShell.ScheduledJob
{
    /// <summary>
    /// This cmdlet creates a new scheduled job trigger based on the provided
    /// parameter values.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "JobTrigger", DefaultParameterSetName = NewJobTriggerCommand.OnceParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=223912")]
    [OutputType(typeof(ScheduledJobTrigger))]
    public sealed class NewJobTriggerCommand : ScheduleJobCmdletBase
    {
        #region Parameters

        private const string AtLogonParameterSet = "AtLogon";
        private const string AtStartupParameterSet = "AtStartup";
        private const string OnceParameterSet = "Once";
        private const string DailyParameterSet = "Daily";
        private const string WeeklyParameterSet = "Weekly";

        /// <summary>
        /// Daily interval for trigger.
        /// </summary>
        [Parameter(ParameterSetName = NewJobTriggerCommand.DailyParameterSet)]
        public Int32 DaysInterval
        {
            get { return _daysInterval; }

            set { _daysInterval = value; }
        }

        private Int32 _daysInterval = 1;

        /// <summary>
        /// Weekly interval for trigger.
        /// </summary>
        [Parameter(ParameterSetName = NewJobTriggerCommand.WeeklyParameterSet)]
        public Int32 WeeksInterval
        {
            get { return _weeksInterval; }

            set { _weeksInterval = value; }
        }

        private Int32 _weeksInterval = 1;

        /// <summary>
        /// Random delay for trigger.
        /// </summary>
        [Parameter(ParameterSetName = NewJobTriggerCommand.AtLogonParameterSet)]
        [Parameter(ParameterSetName = NewJobTriggerCommand.AtStartupParameterSet)]
        [Parameter(ParameterSetName = NewJobTriggerCommand.OnceParameterSet)]
        [Parameter(ParameterSetName = NewJobTriggerCommand.DailyParameterSet)]
        [Parameter(ParameterSetName = NewJobTriggerCommand.WeeklyParameterSet)]
        public TimeSpan RandomDelay
        {
            get { return _randomDelay; }

            set { _randomDelay = value; }
        }

        private TimeSpan _randomDelay;

        /// <summary>
        /// Job start date/time for trigger.
        /// </summary>
        [Parameter(Mandatory = true,
                   ParameterSetName = NewJobTriggerCommand.OnceParameterSet)]
        [Parameter(Mandatory = true,
                   ParameterSetName = NewJobTriggerCommand.DailyParameterSet)]
        [Parameter(Mandatory = true,
                   ParameterSetName = NewJobTriggerCommand.WeeklyParameterSet)]
        public DateTime At
        {
            get { return _atTime; }

            set { _atTime = value; }
        }

        private DateTime _atTime;

        /// <summary>
        /// User name for AtLogon trigger.  User name is used to determine which user
        /// log on causes the trigger to activate.
        /// </summary>
        [Parameter(ParameterSetName = NewJobTriggerCommand.AtLogonParameterSet)]
        [ValidateNotNullOrEmpty]
        public string User
        {
            get { return _user; }

            set { _user = value; }
        }

        private string _user;

        /// <summary>
        /// Days of week for trigger applies only to the Weekly parameter set.
        /// Specifies which day(s) of the week the weekly trigger is activated.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = NewJobTriggerCommand.WeeklyParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public DayOfWeek[] DaysOfWeek
        {
            get { return _daysOfWeek; }

            set { _daysOfWeek = value; }
        }

        private DayOfWeek[] _daysOfWeek;

        /// <summary>
        /// Switch to specify an AtStartup trigger.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0,
            ParameterSetName = NewJobTriggerCommand.AtStartupParameterSet)]
        public SwitchParameter AtStartup
        {
            get { return _atStartup; }

            set { _atStartup = value; }
        }

        private SwitchParameter _atStartup;

        /// <summary>
        /// Switch to specify an AtLogon trigger.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0,
            ParameterSetName = NewJobTriggerCommand.AtLogonParameterSet)]
        public SwitchParameter AtLogOn
        {
            get { return _atLogon; }

            set { _atLogon = value; }
        }

        private SwitchParameter _atLogon;

        /// <summary>
        /// Switch to specify a Once (one time) trigger.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0,
            ParameterSetName = NewJobTriggerCommand.OnceParameterSet)]
        public SwitchParameter Once
        {
            get { return _once; }

            set { _once = value; }
        }

        private SwitchParameter _once;

        /// <summary>
        /// Repetition interval of a one time trigger.
        /// </summary>
        [Parameter(ParameterSetName = NewJobTriggerCommand.OnceParameterSet)]
        public TimeSpan RepetitionInterval
        {
            get { return _repInterval; }

            set { _repInterval = value; }
        }

        private TimeSpan _repInterval;

        /// <summary>
        /// Repetition duration of a one time trigger.
        /// </summary>
        [Parameter(ParameterSetName = NewJobTriggerCommand.OnceParameterSet)]
        public TimeSpan RepetitionDuration
        {
            get { return _repDuration; }

            set { _repDuration = value; }
        }

        private TimeSpan _repDuration;

        /// <summary>
        /// Repetition interval repeats indefinitely.
        /// </summary>
        [Parameter(ParameterSetName = NewJobTriggerCommand.OnceParameterSet)]
        public SwitchParameter RepeatIndefinitely
        {
            get { return _repRepeatIndefinitely; }

            set { _repRepeatIndefinitely = value; }
        }

        private SwitchParameter _repRepeatIndefinitely;

        /// <summary>
        /// Switch to specify a Daily trigger.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0,
            ParameterSetName = NewJobTriggerCommand.DailyParameterSet)]
        public SwitchParameter Daily
        {
            get { return _daily; }

            set { _daily = value; }
        }

        private SwitchParameter _daily;

        /// <summary>
        /// Switch to specify a Weekly trigger.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0,
            ParameterSetName = NewJobTriggerCommand.WeeklyParameterSet)]
        public SwitchParameter Weekly
        {
            get { return _weekly; }

            set { _weekly = value; }
        }

        private SwitchParameter _weekly;

        #endregion

        #region Cmdlet Overrides

        /// <summary>
        /// Do begin processing.
        /// </summary>
        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            // Validate parameters.
            if (_daysInterval < 1)
            {
                throw new PSArgumentException(ScheduledJobErrorStrings.InvalidDaysIntervalParam);
            }

            if (_weeksInterval < 1)
            {
                throw new PSArgumentException(ScheduledJobErrorStrings.InvalidWeeksIntervalParam);
            }
        }

        /// <summary>
        /// Process input.
        /// </summary>
        protected override void ProcessRecord()
        {
            switch (ParameterSetName)
            {
                case AtLogonParameterSet:
                    CreateAtLogonTrigger();
                    break;

                case AtStartupParameterSet:
                    CreateAtStartupTrigger();
                    break;

                case OnceParameterSet:
                    CreateOnceTrigger();
                    break;

                case DailyParameterSet:
                    CreateDailyTrigger();
                    break;

                case WeeklyParameterSet:
                    CreateWeeklyTrigger();
                    break;
            }
        }

        #endregion

        #region Private Methods

        private void CreateAtLogonTrigger()
        {
            WriteObject(ScheduledJobTrigger.CreateAtLogOnTrigger(_user, _randomDelay, 0, true));
        }

        private void CreateAtStartupTrigger()
        {
            WriteObject(ScheduledJobTrigger.CreateAtStartupTrigger(_randomDelay, 0, true));
        }

        private void CreateOnceTrigger()
        {
            TimeSpan? repInterval = null;
            TimeSpan? repDuration = null;
            if (MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionInterval)) || MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionDuration)) ||
                MyInvocation.BoundParameters.ContainsKey(nameof(RepeatIndefinitely)))
            {
                if (MyInvocation.BoundParameters.ContainsKey(nameof(RepeatIndefinitely)))
                {
                    if (MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionDuration)))
                    {
                        throw new PSArgumentException(ScheduledJobErrorStrings.InvalidRepeatIndefinitelyParams);
                    }

                    if (!MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionInterval)))
                    {
                        throw new PSArgumentException(ScheduledJobErrorStrings.InvalidRepetitionRepeatParams);
                    }

                    _repDuration = TimeSpan.MaxValue;
                }
                else if (!MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionInterval)) || !MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionDuration)))
                {
                    throw new PSArgumentException(ScheduledJobErrorStrings.InvalidRepetitionParams);
                }

                if (_repInterval < TimeSpan.Zero || _repDuration < TimeSpan.Zero)
                {
                    throw new PSArgumentException(ScheduledJobErrorStrings.InvalidRepetitionParamValues);
                }

                if (_repInterval < TimeSpan.FromMinutes(1))
                {
                    throw new PSArgumentException(ScheduledJobErrorStrings.InvalidRepetitionIntervalValue);
                }

                if (_repInterval > _repDuration)
                {
                    throw new PSArgumentException(ScheduledJobErrorStrings.InvalidRepetitionInterval);
                }

                repInterval = _repInterval;
                repDuration = _repDuration;
            }

            WriteObject(ScheduledJobTrigger.CreateOnceTrigger(_atTime, _randomDelay, repInterval, repDuration, 0, true));
        }

        private void CreateDailyTrigger()
        {
            WriteObject(ScheduledJobTrigger.CreateDailyTrigger(_atTime, _daysInterval, _randomDelay, 0, true));
        }

        private void CreateWeeklyTrigger()
        {
            WriteObject(ScheduledJobTrigger.CreateWeeklyTrigger(_atTime, _weeksInterval, _daysOfWeek, _randomDelay, 0, true));
        }

        #endregion
    }
}
