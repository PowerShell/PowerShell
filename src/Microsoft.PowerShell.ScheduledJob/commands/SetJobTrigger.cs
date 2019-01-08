// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Diagnostics;

namespace Microsoft.PowerShell.ScheduledJob
{
    /// <summary>
    /// This cmdlet sets properties on a trigger for a ScheduledJobDefinition.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "JobTrigger", DefaultParameterSetName = SetJobTriggerCommand.DefaultParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=223916")]
    [OutputType(typeof(ScheduledJobTrigger))]
    public sealed class SetJobTriggerCommand : ScheduleJobCmdletBase
    {
        #region Parameters

        private const string DefaultParameterSet = "DefaultParams";

        /// <summary>
        /// ScheduledJobTrigger objects to set properties on.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true,
                   ParameterSetName = SetJobTriggerCommand.DefaultParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public ScheduledJobTrigger[] InputObject
        {
            get { return _triggers; }

            set { _triggers = value; }
        }

        private ScheduledJobTrigger[] _triggers;

        /// <summary>
        /// Daily interval for trigger.
        /// </summary>
        [Parameter(ParameterSetName = SetJobTriggerCommand.DefaultParameterSet)]
        public Int32 DaysInterval
        {
            get { return _daysInterval; }

            set { _daysInterval = value; }
        }

        private Int32 _daysInterval = 1;

        /// <summary>
        /// Weekly interval for trigger.
        /// </summary>
        [Parameter(ParameterSetName = SetJobTriggerCommand.DefaultParameterSet)]
        public Int32 WeeksInterval
        {
            get { return _weeksInterval; }

            set { _weeksInterval = value; }
        }

        private Int32 _weeksInterval = 1;

        /// <summary>
        /// Random delay for trigger.
        /// </summary>
        [Parameter(ParameterSetName = SetJobTriggerCommand.DefaultParameterSet)]
        public TimeSpan RandomDelay
        {
            get { return _randomDelay; }

            set { _randomDelay = value; }
        }

        private TimeSpan _randomDelay;

        /// <summary>
        /// Job start date/time for trigger.
        /// </summary>
        [Parameter(ParameterSetName = SetJobTriggerCommand.DefaultParameterSet)]
        public DateTime At
        {
            get { return _atTime; }

            set { _atTime = value; }
        }

        private DateTime _atTime;

        /// <summary>
        /// User name for AtLogon trigger.  The AtLogon parameter set will create a trigger
        /// that activates after log on for the provided user name.
        /// </summary>
        [Parameter(ParameterSetName = SetJobTriggerCommand.DefaultParameterSet)]
        [ValidateNotNullOrEmpty]
        public string User
        {
            get { return _user; }

            set { _user = value; }
        }

        private string _user;

        /// <summary>
        /// Days of week for trigger.
        /// </summary>
        [Parameter(ParameterSetName = SetJobTriggerCommand.DefaultParameterSet)]
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
        [Parameter(ParameterSetName = SetJobTriggerCommand.DefaultParameterSet)]
        public SwitchParameter AtStartup
        {
            get { return _atStartup; }

            set { _atStartup = value; }
        }

        private SwitchParameter _atStartup;

        /// <summary>
        /// Switch to specify an AtLogon trigger.
        /// </summary>
        [Parameter(ParameterSetName = SetJobTriggerCommand.DefaultParameterSet)]
        public SwitchParameter AtLogOn
        {
            get { return _atLogon; }

            set { _atLogon = value; }
        }

        private SwitchParameter _atLogon;

        /// <summary>
        /// Switch to specify an Once trigger.
        /// </summary>
        [Parameter(ParameterSetName = SetJobTriggerCommand.DefaultParameterSet)]
        public SwitchParameter Once
        {
            get { return _once; }

            set { _once = value; }
        }

        private SwitchParameter _once;

        /// <summary>
        /// Repetition interval of a one time trigger.
        /// </summary>
        [Parameter(ParameterSetName = SetJobTriggerCommand.DefaultParameterSet)]
        public TimeSpan RepetitionInterval
        {
            get { return _repInterval; }

            set { _repInterval = value; }
        }

        private TimeSpan _repInterval;

        /// <summary>
        /// Repetition duration of a one time trigger.
        /// </summary>
        [Parameter(ParameterSetName = SetJobTriggerCommand.DefaultParameterSet)]
        public TimeSpan RepetitionDuration
        {
            get { return _repDuration; }

            set { _repDuration = value; }
        }

        private TimeSpan _repDuration;

        /// <summary>
        /// Repetition interval repeats indefinitely.
        /// </summary>
        [Parameter(ParameterSetName = SetJobTriggerCommand.DefaultParameterSet)]
        public SwitchParameter RepeatIndefinitely
        {
            get { return _repRepeatIndefinitely; }

            set { _repRepeatIndefinitely = value; }
        }

        private SwitchParameter _repRepeatIndefinitely;

        /// <summary>
        /// Switch to specify an Daily trigger.
        /// </summary>
        [Parameter(ParameterSetName = SetJobTriggerCommand.DefaultParameterSet)]
        public SwitchParameter Daily
        {
            get { return _daily; }

            set { _daily = value; }
        }

        private SwitchParameter _daily;

        /// <summary>
        /// Switch to specify an Weekly trigger.
        /// </summary>
        [Parameter(ParameterSetName = SetJobTriggerCommand.DefaultParameterSet)]
        public SwitchParameter Weekly
        {
            get { return _weekly; }

            set { _weekly = value; }
        }

        private SwitchParameter _weekly;

        /// <summary>
        /// Pass through job trigger object.
        /// </summary>
        [Parameter(ParameterSetName = SetJobTriggerCommand.DefaultParameterSet)]
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
            // Validate the parameter set and write any errors.
            TriggerFrequency newTriggerFrequency = TriggerFrequency.None;
            if (!ValidateParameterSet(ref newTriggerFrequency))
            {
                return;
            }

            // Update each trigger object with the current parameter set.
            // The associated scheduled job definition will also be updated.
            foreach (ScheduledJobTrigger trigger in _triggers)
            {
                ScheduledJobTrigger originalTrigger = new ScheduledJobTrigger(trigger);
                if (!UpdateTrigger(trigger, newTriggerFrequency))
                {
                    continue;
                }

                ScheduledJobDefinition definition = trigger.JobDefinition;
                if (definition != null)
                {
                    bool jobUpdateFailed = false;

                    try
                    {
                        trigger.UpdateJobDefinition();
                    }
                    catch (ScheduledJobException e)
                    {
                        jobUpdateFailed = true;

                        string msg = StringUtil.Format(ScheduledJobErrorStrings.CantUpdateTriggerOnJobDef, definition.Name, trigger.Id);
                        Exception reason = new RuntimeException(msg, e);
                        ErrorRecord errorRecord = new ErrorRecord(reason, "CantSetPropertiesOnJobTrigger", ErrorCategory.InvalidOperation, trigger);
                        WriteError(errorRecord);
                    }

                    if (jobUpdateFailed)
                    {
                        // Restore trigger to original configuration.
                        originalTrigger.CopyTo(trigger);
                    }
                }

                if (_passThru)
                {
                    WriteObject(trigger);
                }
            }
        }

        #endregion

        #region Private Methods

        private bool ValidateParameterSet(ref TriggerFrequency newTriggerFrequency)
        {
            // First see if a switch parameter was set.
            List<TriggerFrequency> switchParamList = new List<TriggerFrequency>();
            if (MyInvocation.BoundParameters.ContainsKey(nameof(AtStartup)))
            {
                switchParamList.Add(TriggerFrequency.AtStartup);
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(AtLogon)))
            {
                switchParamList.Add(TriggerFrequency.AtLogon);
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(Once)))
            {
                switchParamList.Add(TriggerFrequency.Once);
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(Daily)))
            {
                switchParamList.Add(TriggerFrequency.Daily);
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(Weekly)))
            {
                switchParamList.Add(TriggerFrequency.Weekly);
            }

            if (switchParamList.Count > 1)
            {
                WriteValidationError(ScheduledJobErrorStrings.ConflictingTypeParams);
                return false;
            }

            newTriggerFrequency = (switchParamList.Count == 1) ? switchParamList[0] : TriggerFrequency.None;

            // Validate parameters against the new trigger frequency value.
            bool rtnValue = false;
            switch (newTriggerFrequency)
            {
                case TriggerFrequency.None:
                    rtnValue = true;
                    break;

                case TriggerFrequency.AtStartup:
                    rtnValue = ValidateStartupParams();
                    break;

                case TriggerFrequency.AtLogon:
                    rtnValue = ValidateLogonParams();
                    break;

                case TriggerFrequency.Once:
                    rtnValue = ValidateOnceParams();
                    break;

                case TriggerFrequency.Daily:
                    rtnValue = ValidateDailyParams();
                    break;

                case TriggerFrequency.Weekly:
                    rtnValue = ValidateWeeklyParams();
                    break;

                default:
                    Debug.Assert(false, "Invalid trigger frequency value.");
                    rtnValue = false;
                    break;
            }

            return rtnValue;
        }

        private bool ValidateStartupParams()
        {
            if (MyInvocation.BoundParameters.ContainsKey(nameof(DaysInterval)))
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidDaysInterval, ScheduledJobErrorStrings.TriggerStartUpType);
                WriteValidationError(msg);
                return false;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(WeeksInterval)))
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidWeeksInterval, ScheduledJobErrorStrings.TriggerStartUpType);
                WriteValidationError(msg);
                return false;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(At)))
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidAtTime, ScheduledJobErrorStrings.TriggerStartUpType);
                WriteValidationError(msg);
                return false;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(User)))
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidUser, ScheduledJobErrorStrings.TriggerStartUpType);
                WriteValidationError(msg);
                return false;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(DaysOfWeek)))
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidDaysOfWeek, ScheduledJobErrorStrings.TriggerStartUpType);
                WriteValidationError(msg);
                return false;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionInterval)) || MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionDuration)) ||
                MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionInfiniteDuration)))
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidSetTriggerRepetition, ScheduledJobErrorStrings.TriggerStartUpType);
                WriteValidationError(msg);
                return false;
            }

            return true;
        }

        private bool ValidateLogonParams()
        {
            if (MyInvocation.BoundParameters.ContainsKey(nameof(DaysInterval)))
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidDaysInterval, ScheduledJobErrorStrings.TriggerLogonType);
                WriteValidationError(msg);
                return false;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(WeeksInterval)))
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidWeeksInterval, ScheduledJobErrorStrings.TriggerLogonType);
                WriteValidationError(msg);
                return false;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(At)))
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidAtTime, ScheduledJobErrorStrings.TriggerLogonType);
                WriteValidationError(msg);
                return false;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(DaysOfWeek)))
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidDaysOfWeek, ScheduledJobErrorStrings.TriggerLogonType);
                WriteValidationError(msg);
                return false;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionInterval)) || MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionDuration)) ||
                MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionInfiniteDuration)))
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidSetTriggerRepetition, ScheduledJobErrorStrings.TriggerLogonType);
                WriteValidationError(msg);
                return false;
            }

            return true;
        }

        private bool ValidateOnceParams(ScheduledJobTrigger trigger = null)
        {
            if (MyInvocation.BoundParameters.ContainsKey(nameof(DaysInterval)))
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidDaysInterval, ScheduledJobErrorStrings.TriggerOnceType);
                WriteValidationError(msg);
                return false;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(WeeksInterval)))
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidWeeksInterval, ScheduledJobErrorStrings.TriggerOnceType);
                WriteValidationError(msg);
                return false;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(User)))
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidUser, ScheduledJobErrorStrings.TriggerOnceType);
                WriteValidationError(msg);
                return false;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(DaysOfWeek)))
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidDaysOfWeek, ScheduledJobErrorStrings.TriggerOnceType);
                WriteValidationError(msg);
                return false;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionInfiniteDuration)))
            {
                _repDuration = TimeSpan.MaxValue;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionInterval)) || MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionDuration)) ||
                MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionInfiniteDuration)))
            {
                // Validate Once trigger repetition parameters.
                try
                {
                    ScheduledJobTrigger.ValidateOnceRepetitionParams(_repInterval, _repDuration);
                }
                catch (PSArgumentException e)
                {
                    WriteValidationError(e.Message);
                    return false;
                }
            }

            if (trigger != null)
            {
                if (trigger.At == null && !MyInvocation.BoundParameters.ContainsKey(nameof(At)))
                {
                    string msg = StringUtil.Format(ScheduledJobErrorStrings.MissingAtTime, ScheduledJobErrorStrings.TriggerOnceType);
                    WriteValidationError(msg);
                    return false;
                }
            }

            return true;
        }

        private bool ValidateDailyParams(ScheduledJobTrigger trigger = null)
        {
            if (MyInvocation.BoundParameters.ContainsKey(nameof(DaysInterval)) &&
                _daysInterval < 1)
            {
                WriteValidationError(ScheduledJobErrorStrings.InvalidDaysIntervalParam);
                return false;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(WeeksInterval)))
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidWeeksInterval, ScheduledJobErrorStrings.TriggerDailyType);
                WriteValidationError(msg);
                return false;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(User)))
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidUser, ScheduledJobErrorStrings.TriggerDailyType);
                WriteValidationError(msg);
                return false;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(DaysOfWeek)))
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidDaysOfWeek, ScheduledJobErrorStrings.TriggerDailyType);
                WriteValidationError(msg);
                return false;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionInterval)) || MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionDuration)) ||
                MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionInfiniteDuration)))
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidSetTriggerRepetition, ScheduledJobErrorStrings.TriggerDailyType);
                WriteValidationError(msg);
                return false;
            }

            if (trigger != null)
            {
                if (trigger.At == null && !MyInvocation.BoundParameters.ContainsKey(nameof(At)))
                {
                    string msg = StringUtil.Format(ScheduledJobErrorStrings.MissingAtTime, ScheduledJobErrorStrings.TriggerDailyType);
                    WriteValidationError(msg);
                    return false;
                }
            }

            return true;
        }

        private bool ValidateWeeklyParams(ScheduledJobTrigger trigger = null)
        {
            if (MyInvocation.BoundParameters.ContainsKey(nameof(DaysInterval)))
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidDaysInterval, ScheduledJobErrorStrings.TriggerWeeklyType);
                WriteValidationError(msg);
                return false;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(WeeksInterval)) &&
                _weeksInterval < 1)
            {
                WriteValidationError(ScheduledJobErrorStrings.InvalidWeeksIntervalParam);
                return false;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(User)))
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidUser, ScheduledJobErrorStrings.TriggerWeeklyType);
                WriteValidationError(msg);
                return false;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionInterval)) || MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionDuration)) ||
                MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionInfiniteDuration)))
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidSetTriggerRepetition, ScheduledJobErrorStrings.TriggerWeeklyType);
                WriteValidationError(msg);
                return false;
            }

            if (trigger != null)
            {
                if (trigger.At == null && !MyInvocation.BoundParameters.ContainsKey(nameof(At)))
                {
                    string msg = StringUtil.Format(ScheduledJobErrorStrings.MissingAtTime, ScheduledJobErrorStrings.TriggerDailyType);
                    WriteValidationError(msg);
                    return false;
                }

                if ((trigger.DaysOfWeek == null || trigger.DaysOfWeek.Count == 0) &&
                    !MyInvocation.BoundParameters.ContainsKey(nameof(DaysOfWeek)))
                {
                    string msg = StringUtil.Format(ScheduledJobErrorStrings.MissingDaysOfWeek, ScheduledJobErrorStrings.TriggerDailyType);
                    WriteValidationError(msg);
                    return false;
                }
            }

            return true;
        }

        private bool UpdateTrigger(ScheduledJobTrigger trigger, TriggerFrequency triggerFrequency)
        {
            if (triggerFrequency != TriggerFrequency.None)
            {
                //
                // User has specified a specific trigger type.
                // Parameters have been validated for this trigger type.
                //
                if (triggerFrequency != trigger.Frequency)
                {
                    // Changing to a new trigger type.
                    return CreateTrigger(trigger, triggerFrequency);
                }
                else
                {
                    // Modifying existing trigger type.
                    return ModifyTrigger(trigger, triggerFrequency);
                }
            }
            else
            {
                // We are updating an existing trigger.  Need to validate params
                // against each trigger type we are updating.
                return ModifyTrigger(trigger, trigger.Frequency, true);
            }
        }

        private bool CreateTrigger(ScheduledJobTrigger trigger, TriggerFrequency triggerFrequency)
        {
            switch (triggerFrequency)
            {
                case TriggerFrequency.AtStartup:
                    CreateAtStartupTrigger(trigger);
                    break;

                case TriggerFrequency.AtLogon:
                    CreateAtLogonTrigger(trigger);
                    break;

                case TriggerFrequency.Once:
                    if (trigger.Frequency != triggerFrequency &&
                        !ValidateOnceParams(trigger))
                    {
                        return false;
                    }

                    CreateOnceTrigger(trigger);
                    break;

                case TriggerFrequency.Daily:
                    if (trigger.Frequency != triggerFrequency &&
                        !ValidateDailyParams(trigger))
                    {
                        return false;
                    }

                    CreateDailyTrigger(trigger);
                    break;

                case TriggerFrequency.Weekly:
                    if (trigger.Frequency != triggerFrequency &&
                        !ValidateWeeklyParams(trigger))
                    {
                        return false;
                    }

                    CreateWeeklyTrigger(trigger);
                    break;
            }

            return true;
        }

        private bool ModifyTrigger(ScheduledJobTrigger trigger, TriggerFrequency triggerFrequency, bool validate = false)
        {
            switch (triggerFrequency)
            {
                case TriggerFrequency.AtStartup:
                    if (validate &&
                        !ValidateStartupParams())
                    {
                        return false;
                    }

                    ModifyStartupTrigger(trigger);
                    break;

                case TriggerFrequency.AtLogon:
                    if (validate &&
                        !ValidateLogonParams())
                    {
                        return false;
                    }

                    ModifyLogonTrigger(trigger);
                    break;

                case TriggerFrequency.Once:
                    if (validate &&
                        !ValidateOnceParams())
                    {
                        return false;
                    }

                    ModifyOnceTrigger(trigger);
                    break;

                case TriggerFrequency.Daily:
                    if (validate &&
                        !ValidateDailyParams())
                    {
                        return false;
                    }

                    ModifyDailyTrigger(trigger);
                    break;

                case TriggerFrequency.Weekly:
                    if (validate &&
                        !ValidateWeeklyParams())
                    {
                        return false;
                    }

                    ModifyWeeklyTrigger(trigger);
                    break;
            }

            return true;
        }

        private void ModifyStartupTrigger(ScheduledJobTrigger trigger)
        {
            if (MyInvocation.BoundParameters.ContainsKey(nameof(RandomDelay)))
            {
                trigger.RandomDelay = _randomDelay;
            }
        }

        private void ModifyLogonTrigger(ScheduledJobTrigger trigger)
        {
            if (MyInvocation.BoundParameters.ContainsKey(nameof(RandomDelay)))
            {
                trigger.RandomDelay = _randomDelay;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(User)))
            {
                trigger.User = string.IsNullOrEmpty(_user) ? ScheduledJobTrigger.AllUsers : _user;
            }
        }

        private void ModifyOnceTrigger(ScheduledJobTrigger trigger)
        {
            if (MyInvocation.BoundParameters.ContainsKey(nameof(RandomDelay)))
            {
                trigger.RandomDelay = _randomDelay;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionInterval)))
            {
                trigger.RepetitionInterval = _repInterval;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionDuration)))
            {
                trigger.RepetitionDuration = _repDuration;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(At)))
            {
                trigger.At = _atTime;
            }
        }

        private void ModifyDailyTrigger(ScheduledJobTrigger trigger)
        {
            if (MyInvocation.BoundParameters.ContainsKey(nameof(RandomDelay)))
            {
                trigger.RandomDelay = _randomDelay;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(At)))
            {
                trigger.At = _atTime;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(DaysInterval)))
            {
                trigger.Interval = _daysInterval;
            }
        }

        private void ModifyWeeklyTrigger(ScheduledJobTrigger trigger)
        {
            if (MyInvocation.BoundParameters.ContainsKey(nameof(RandomDelay)))
            {
                trigger.RandomDelay = _randomDelay;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(At)))
            {
                trigger.At = _atTime;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(WeeksInterval)))
            {
                trigger.Interval = _weeksInterval;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(DaysOfWeek)))
            {
                trigger.DaysOfWeek = new List<DayOfWeek>(_daysOfWeek);
            }
        }

        private void CreateAtLogonTrigger(ScheduledJobTrigger trigger)
        {
            bool enabled = trigger.Enabled;
            int id = trigger.Id;
            TimeSpan randomDelay = trigger.RandomDelay;
            string user = string.IsNullOrEmpty(trigger.User) ? ScheduledJobTrigger.AllUsers : trigger.User;

            trigger.ClearProperties();
            trigger.Frequency = TriggerFrequency.AtLogon;
            trigger.Enabled = enabled;
            trigger.Id = id;

            trigger.RandomDelay = MyInvocation.BoundParameters.ContainsKey(nameof(RandomDelay)) ? _randomDelay : randomDelay;
            trigger.User = MyInvocation.BoundParameters.ContainsKey(nameof(User)) ? _user : user;
        }

        private void CreateAtStartupTrigger(ScheduledJobTrigger trigger)
        {
            bool enabled = trigger.Enabled;
            int id = trigger.Id;
            TimeSpan randomDelay = trigger.RandomDelay;

            trigger.ClearProperties();
            trigger.Frequency = TriggerFrequency.AtStartup;
            trigger.Enabled = enabled;
            trigger.Id = id;

            trigger.RandomDelay = MyInvocation.BoundParameters.ContainsKey(nameof(RandomDelay)) ? _randomDelay : randomDelay;
        }

        private void CreateOnceTrigger(ScheduledJobTrigger trigger)
        {
            bool enabled = trigger.Enabled;
            int id = trigger.Id;
            TimeSpan randomDelay = trigger.RandomDelay;
            DateTime? atTime = trigger.At;
            TimeSpan? repInterval = trigger.RepetitionInterval;
            TimeSpan? repDuration = trigger.RepetitionDuration;

            trigger.ClearProperties();
            trigger.Frequency = TriggerFrequency.Once;
            trigger.Enabled = enabled;
            trigger.Id = id;

            trigger.RandomDelay = MyInvocation.BoundParameters.ContainsKey(nameof(RandomDelay)) ? _randomDelay : randomDelay;
            trigger.At = MyInvocation.BoundParameters.ContainsKey(nameof(At)) ? _atTime : atTime;
            trigger.RepetitionInterval = MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionInterval)) ? _repInterval : repInterval;
            trigger.RepetitionDuration = MyInvocation.BoundParameters.ContainsKey(nameof(RepetitionDuration)) ? _repDuration : repDuration;
        }

        private void CreateDailyTrigger(ScheduledJobTrigger trigger)
        {
            bool enabled = trigger.Enabled;
            int id = trigger.Id;
            TimeSpan randomDelay = trigger.RandomDelay;
            DateTime? atTime = trigger.At;
            int interval = trigger.Interval;

            trigger.ClearProperties();
            trigger.Frequency = TriggerFrequency.Daily;
            trigger.Enabled = enabled;
            trigger.Id = id;

            trigger.RandomDelay = MyInvocation.BoundParameters.ContainsKey(nameof(RandomDelay)) ? _randomDelay : randomDelay;
            trigger.At = MyInvocation.BoundParameters.ContainsKey(nameof(At)) ? _atTime : atTime;
            trigger.Interval = MyInvocation.BoundParameters.ContainsKey(nameof(DaysInterval)) ? _daysInterval : interval;
        }

        private void CreateWeeklyTrigger(ScheduledJobTrigger trigger)
        {
            bool enabled = trigger.Enabled;
            int id = trigger.Id;
            TimeSpan randomDelay = trigger.RandomDelay;
            DateTime? atTime = trigger.At;
            int interval = trigger.Interval;
            List<DayOfWeek> daysOfWeek = trigger.DaysOfWeek;

            trigger.ClearProperties();
            trigger.Frequency = TriggerFrequency.Weekly;
            trigger.Enabled = enabled;
            trigger.Id = id;

            trigger.RandomDelay = MyInvocation.BoundParameters.ContainsKey(nameof(RandomDelay)) ? _randomDelay : randomDelay;
            trigger.At = MyInvocation.BoundParameters.ContainsKey(nameof(At)) ? _atTime : atTime;
            trigger.Interval = MyInvocation.BoundParameters.ContainsKey(nameof(WeeksInterval)) ? _weeksInterval : interval;
            trigger.DaysOfWeek = MyInvocation.BoundParameters.ContainsKey(nameof(DaysOfWeek)) ? new List<DayOfWeek>(_daysOfWeek) : daysOfWeek;
        }

        private void WriteValidationError(string msg)
        {
            Exception reason = new RuntimeException(msg);
            ErrorRecord errorRecord = new ErrorRecord(reason, "SetJobTriggerParameterValidationError", ErrorCategory.InvalidArgument, null);
            WriteError(errorRecord);
        }

        #endregion
    }
}
