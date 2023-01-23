// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using System.Threading;

using Microsoft.Management.Infrastructure;

namespace Microsoft.PowerShell.ScheduledJob
{
    /// <summary>
    /// This class contains parameters used to define how/when a PowerShell job is
    /// run via the Windows Task Scheduler (WTS).
    /// </summary>
    [Serializable]
    public sealed class ScheduledJobTrigger : ISerializable
    {
        #region Private Members

        private DateTime? _time;
        private List<DayOfWeek> _daysOfWeek;
        private TimeSpan _randomDelay;
        private Int32 _interval = 1;
        private string _user;
        private TriggerFrequency _frequency = TriggerFrequency.None;
        private TimeSpan? _repInterval;
        private TimeSpan? _repDuration;

        private Int32 _id;
        private bool _enabled = true;
        private ScheduledJobDefinition _jobDefAssociation;

        private static string _allUsers = "*";

        #endregion

        #region Public Properties

        /// <summary>
        /// Trigger time.
        /// </summary>
        public DateTime? At
        {
            get { return _time; }

            set { _time = value; }
        }

        /// <summary>
        /// Trigger days of week.
        /// </summary>
        public List<DayOfWeek> DaysOfWeek
        {
            get { return _daysOfWeek; }

            set { _daysOfWeek = value; }
        }

        /// <summary>
        /// Trigger days or weeks interval.
        /// </summary>
        public Int32 Interval
        {
            get { return _interval; }

            set { _interval = value; }
        }

        /// <summary>
        /// Trigger frequency.
        /// </summary>
        public TriggerFrequency Frequency
        {
            get { return _frequency; }

            set { _frequency = value; }
        }

        /// <summary>
        /// Trigger random delay.
        /// </summary>
        public TimeSpan RandomDelay
        {
            get { return _randomDelay; }

            set { _randomDelay = value; }
        }

        /// <summary>
        /// Trigger Once frequency repetition interval.
        /// </summary>
        public TimeSpan? RepetitionInterval
        {
            get { return _repInterval; }

            set
            {
                // A TimeSpan value of zero is equivalent to a null value.
                _repInterval = (value != null && value.Value == TimeSpan.Zero) ?
                    null : value;
            }
        }

        /// <summary>
        /// Trigger Once frequency repetition duration.
        /// </summary>
        public TimeSpan? RepetitionDuration
        {
            get { return _repDuration; }

            set
            {
                // A TimeSpan value of zero is equivalent to a null value.
                _repDuration = (value != null && value.Value == TimeSpan.Zero) ?
                    null : value;
            }
        }

        /// <summary>
        /// Trigger user name.
        /// </summary>
        public string User
        {
            get { return _user; }

            set { _user = value; }
        }

        /// <summary>
        /// Returns the trigger local Id.
        /// </summary>
        public Int32 Id
        {
            get { return _id; }

            internal set { _id = value; }
        }

        /// <summary>
        /// Defines enabled state of trigger.
        /// </summary>
        public bool Enabled
        {
            get { return _enabled; }

            set { _enabled = value; }
        }

        /// <summary>
        /// ScheduledJobDefinition object this trigger is associated with.
        /// </summary>
        public ScheduledJobDefinition JobDefinition
        {
            get { return _jobDefAssociation; }

            internal set { _jobDefAssociation = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ScheduledJobTrigger()
        { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="enabled">Enabled.</param>
        /// <param name="frequency">Trigger frequency.</param>
        /// <param name="time">Trigger time.</param>
        /// <param name="daysOfWeek">Weekly days of week.</param>
        /// <param name="interval">Daily or Weekly interval.</param>
        /// <param name="randomDelay">Random delay.</param>
        /// <param name="repetitionInterval">Repetition interval.</param>
        /// <param name="repetitionDuration">Repetition duration.</param>
        /// <param name="user">Logon user.</param>
        /// <param name="id">Trigger id.</param>
        private ScheduledJobTrigger(
            bool enabled,
            TriggerFrequency frequency,
            DateTime? time,
            List<DayOfWeek> daysOfWeek,
            Int32 interval,
            TimeSpan randomDelay,
            TimeSpan? repetitionInterval,
            TimeSpan? repetitionDuration,
            string user,
            Int32 id)
        {
            _enabled = enabled;
            _frequency = frequency;
            _time = time;
            _daysOfWeek = daysOfWeek;
            _interval = interval;
            _randomDelay = randomDelay;
            RepetitionInterval = repetitionInterval;
            RepetitionDuration = repetitionDuration;
            _user = user;
            _id = id;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="copyTrigger">ScheduledJobTrigger.</param>
        internal ScheduledJobTrigger(ScheduledJobTrigger copyTrigger)
        {
            if (copyTrigger == null)
            {
                throw new PSArgumentNullException("copyTrigger");
            }

            _enabled = copyTrigger.Enabled;
            _frequency = copyTrigger.Frequency;
            _id = copyTrigger.Id;
            _time = copyTrigger.At;
            _daysOfWeek = copyTrigger.DaysOfWeek;
            _interval = copyTrigger.Interval;
            _randomDelay = copyTrigger.RandomDelay;
            _repInterval = copyTrigger.RepetitionInterval;
            _repDuration = copyTrigger.RepetitionDuration;
            _user = copyTrigger.User;

            _jobDefAssociation = copyTrigger.JobDefinition;
        }

        /// <summary>
        /// Serialization constructor.
        /// </summary>
        /// <param name="info">SerializationInfo.</param>
        /// <param name="context">StreamingContext.</param>
        private ScheduledJobTrigger(
            SerializationInfo info,
            StreamingContext context)
        {
            if (info == null)
            {
                throw new PSArgumentNullException("info");
            }

            DateTime time = info.GetDateTime("Time_Value");
            if (time != DateTime.MinValue)
            {
                _time = time;
            }
            else
            {
                _time = null;
            }

            RepetitionInterval = (TimeSpan?)info.GetValue("RepetitionInterval_Value", typeof(TimeSpan));
            RepetitionDuration = (TimeSpan?)info.GetValue("RepetitionDuration_Value", typeof(TimeSpan));

            _daysOfWeek = (List<DayOfWeek>)info.GetValue("DaysOfWeek_Value", typeof(List<DayOfWeek>));
            _randomDelay = (TimeSpan)info.GetValue("RandomDelay_Value", typeof(TimeSpan));
            _interval = info.GetInt32("Interval_Value");
            _user = info.GetString("User_Value");
            _frequency = (TriggerFrequency)info.GetValue("TriggerFrequency_Value", typeof(TriggerFrequency));
            _id = info.GetInt32("ID_Value");
            _enabled = info.GetBoolean("Enabled_Value");

            // Runtime reference and not saved to store.
            _jobDefAssociation = null;
        }

        #endregion

        #region ISerializable Implementation

        /// <summary>
        /// GetObjectData for ISerializable implementation.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new PSArgumentNullException("info");
            }

            if (_time == null)
            {
                info.AddValue("Time_Value", DateTime.MinValue);
            }
            else
            {
                info.AddValue("Time_Value", _time);
            }

            if (_repInterval == null)
            {
                info.AddValue("RepetitionInterval_Value", TimeSpan.Zero);
            }
            else
            {
                info.AddValue("RepetitionInterval_Value", _repInterval);
            }

            if (_repDuration == null)
            {
                info.AddValue("RepetitionDuration_Value", TimeSpan.Zero);
            }
            else
            {
                info.AddValue("RepetitionDuration_Value", _repDuration);
            }

            info.AddValue("DaysOfWeek_Value", _daysOfWeek);
            info.AddValue("RandomDelay_Value", _randomDelay);
            info.AddValue("Interval_Value", _interval);
            info.AddValue("User_Value", _user);
            info.AddValue("TriggerFrequency_Value", _frequency);
            info.AddValue("ID_Value", _id);
            info.AddValue("Enabled_Value", _enabled);
        }

        #endregion

        #region Internal Methods

        internal void ClearProperties()
        {
            _time = null;
            _daysOfWeek = null;
            _interval = 1;
            _randomDelay = TimeSpan.Zero;
            _repInterval = null;
            _repDuration = null;
            _user = null;
            _frequency = TriggerFrequency.None;
            _enabled = false;
            _id = 0;
        }

        internal void Validate()
        {
            switch (_frequency)
            {
                case TriggerFrequency.None:
                    throw new ScheduledJobException(ScheduledJobErrorStrings.MissingJobTriggerType);

                case TriggerFrequency.AtStartup:
                    // AtStartup has no required parameters.
                    break;

                case TriggerFrequency.AtLogon:
                    // AtLogon has no required parameters.
                    break;

                case TriggerFrequency.Once:
                    if (_time == null)
                    {
                        string msg = StringUtil.Format(ScheduledJobErrorStrings.MissingJobTriggerTime, ScheduledJobErrorStrings.TriggerOnceType);
                        throw new ScheduledJobException(msg);
                    }

                    if (_repInterval != null || _repDuration != null)
                    {
                        ValidateOnceRepetitionParams(_repInterval, _repDuration);
                    }

                    break;

                case TriggerFrequency.Daily:
                    if (_time == null)
                    {
                        string msg = StringUtil.Format(ScheduledJobErrorStrings.MissingJobTriggerTime, ScheduledJobErrorStrings.TriggerDailyType);
                        throw new ScheduledJobException(msg);
                    }

                    if (_interval < 1)
                    {
                        throw new ScheduledJobException(ScheduledJobErrorStrings.InvalidDaysIntervalParam);
                    }

                    break;

                case TriggerFrequency.Weekly:
                    if (_time == null)
                    {
                        string msg = StringUtil.Format(ScheduledJobErrorStrings.MissingJobTriggerTime, ScheduledJobErrorStrings.TriggerWeeklyType);
                        throw new ScheduledJobException(msg);
                    }

                    if (_interval < 1)
                    {
                        throw new ScheduledJobException(ScheduledJobErrorStrings.InvalidWeeksIntervalParam);
                    }

                    if (_daysOfWeek == null || _daysOfWeek.Count == 0)
                    {
                        string msg = StringUtil.Format(ScheduledJobErrorStrings.MissingJobTriggerDaysOfWeek, ScheduledJobErrorStrings.TriggerWeeklyType);
                        throw new ScheduledJobException(msg);
                    }

                    break;
            }
        }

        internal static void ValidateOnceRepetitionParams(
            TimeSpan? repInterval,
            TimeSpan? repDuration)
        {
            // Both Interval and Duration parameters must be specified together.
            if (repInterval == null || repDuration == null)
            {
                throw new PSArgumentException(ScheduledJobErrorStrings.InvalidRepetitionParams);
            }

            // Interval and Duration parameters must not have negative value.
            if (repInterval < TimeSpan.Zero || repDuration < TimeSpan.Zero)
            {
                throw new PSArgumentException(ScheduledJobErrorStrings.InvalidRepetitionParamValues);
            }

            // Zero values are allowed but only if both parameters are set to zero.
            // This removes repetition from the Once trigger.
            if (repInterval == TimeSpan.Zero && repDuration != TimeSpan.Zero)
            {
                throw new PSArgumentException(ScheduledJobErrorStrings.MismatchedRepetitionParamValues);
            }

            // Parameter values must be GE to one minute unless both are zero to remove repetition.
            if (repInterval < TimeSpan.FromMinutes(1) &&
                !(repInterval == TimeSpan.Zero && repDuration == TimeSpan.Zero))
            {
                throw new PSArgumentException(ScheduledJobErrorStrings.InvalidRepetitionIntervalValue);
            }

            // Interval parameter must be LE to Duration parameter.
            if (repInterval > repDuration)
            {
                throw new PSArgumentException(ScheduledJobErrorStrings.InvalidRepetitionInterval);
            }
        }

        internal void CopyTo(ScheduledJobTrigger targetTrigger)
        {
            if (targetTrigger == null)
            {
                throw new PSArgumentNullException("targetTrigger");
            }

            targetTrigger.Enabled = _enabled;
            targetTrigger.Frequency = _frequency;
            targetTrigger.Id = _id;
            targetTrigger.At = _time;
            targetTrigger.DaysOfWeek = _daysOfWeek;
            targetTrigger.Interval = _interval;
            targetTrigger.RandomDelay = _randomDelay;
            targetTrigger.RepetitionInterval = _repInterval;
            targetTrigger.RepetitionDuration = _repDuration;
            targetTrigger.User = _user;
            targetTrigger.JobDefinition = _jobDefAssociation;
        }

        #endregion

        #region Static methods

        /// <summary>
        /// Creates a one time ScheduledJobTrigger object.
        /// </summary>
        /// <param name="time">DateTime when trigger activates.</param>
        /// <param name="delay">Random delay.</param>
        /// <param name="repetitionInterval">Repetition interval.</param>
        /// <param name="repetitionDuration">Repetition duration.</param>
        /// <param name="id">Trigger Id.</param>
        /// <param name="enabled">Trigger enabled state.</param>
        /// <returns>ScheduledJobTrigger.</returns>
        public static ScheduledJobTrigger CreateOnceTrigger(
            DateTime time,
            TimeSpan delay,
            TimeSpan? repetitionInterval,
            TimeSpan? repetitionDuration,
            Int32 id,
            bool enabled)
        {
            return new ScheduledJobTrigger(
                enabled,
                TriggerFrequency.Once,
                time,
                null,
                1,
                delay,
                repetitionInterval,
                repetitionDuration,
                null,
                id);
        }

        /// <summary>
        /// Creates a daily ScheduledJobTrigger object.
        /// </summary>
        /// <param name="time">Time of day when trigger activates.</param>
        /// <param name="interval">Days interval for trigger activation.</param>
        /// <param name="delay">Random delay.</param>
        /// <param name="id">Trigger Id.</param>
        /// <param name="enabled">Trigger enabled state.</param>
        /// <returns>ScheduledJobTrigger.</returns>
        public static ScheduledJobTrigger CreateDailyTrigger(
            DateTime time,
            Int32 interval,
            TimeSpan delay,
            Int32 id,
            bool enabled)
        {
            return new ScheduledJobTrigger(
                enabled,
                TriggerFrequency.Daily,
                time,
                null,
                interval,
                delay,
                null,
                null,
                null,
                id);
        }

        /// <summary>
        /// Creates a weekly ScheduledJobTrigger object.
        /// </summary>
        /// <param name="time">Time of day when trigger activates.</param>
        /// <param name="interval">Weeks interval for trigger activation.</param>
        /// <param name="daysOfWeek">Days of the week for trigger activation.</param>
        /// <param name="delay">Random delay.</param>
        /// <param name="id">Trigger Id.</param>
        /// <param name="enabled">Trigger enabled state.</param>
        /// <returns>ScheduledJobTrigger.</returns>
        public static ScheduledJobTrigger CreateWeeklyTrigger(
            DateTime time,
            Int32 interval,
            IEnumerable<DayOfWeek> daysOfWeek,
            TimeSpan delay,
            Int32 id,
            bool enabled)
        {
            List<DayOfWeek> lDaysOfWeek = (daysOfWeek != null) ? new List<DayOfWeek>(daysOfWeek) : null;

            return new ScheduledJobTrigger(
                enabled,
                TriggerFrequency.Weekly,
                time,
                lDaysOfWeek,
                interval,
                delay,
                null,
                null,
                null,
                id);
        }

        /// <summary>
        /// Creates a trigger that activates after user log on.
        /// </summary>
        /// <param name="user">Name of user.</param>
        /// <param name="delay">Random delay.</param>
        /// <param name="id">Trigger Id.</param>
        /// <param name="enabled">Trigger enabled state.</param>
        /// <returns>ScheduledJobTrigger.</returns>
        public static ScheduledJobTrigger CreateAtLogOnTrigger(
            string user,
            TimeSpan delay,
            Int32 id,
            bool enabled)
        {
            return new ScheduledJobTrigger(
                enabled,
                TriggerFrequency.AtLogon,
                null,
                null,
                1,
                delay,
                null,
                null,
                string.IsNullOrEmpty(user) ? AllUsers : user,
                id);
        }

        /// <summary>
        /// Creates a trigger that activates after OS boot.
        /// </summary>
        /// <param name="delay">Random delay.</param>
        /// <param name="id">Trigger Id.</param>
        /// <param name="enabled">Trigger enabled state.</param>
        /// <returns>ScheduledJobTrigger.</returns>
        public static ScheduledJobTrigger CreateAtStartupTrigger(
            TimeSpan delay,
            Int32 id,
            bool enabled)
        {
            return new ScheduledJobTrigger(
                enabled,
                TriggerFrequency.AtStartup,
                null,
                null,
                1,
                delay,
                null,
                null,
                null,
                id);
        }

        /// <summary>
        /// Compares provided user name to All Users string ("*").
        /// </summary>
        /// <param name="userName">Logon user name.</param>
        /// <returns>Boolean, true if All Users.</returns>
        internal static bool IsAllUsers(string userName)
        {
            return (string.Compare(userName, ScheduledJobTrigger.AllUsers,
                StringComparison.OrdinalIgnoreCase) == 0);
        }

        /// <summary>
        /// Returns the All Users string.
        /// </summary>
        internal static string AllUsers
        {
            get { return _allUsers; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Update the associated ScheduledJobDefinition object with the
        /// current properties of this object.
        /// </summary>
        public void UpdateJobDefinition()
        {
            if (_jobDefAssociation == null)
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.NoAssociatedJobDefinitionForTrigger, _id);
                throw new RuntimeException(msg);
            }

            _jobDefAssociation.UpdateTriggers(new ScheduledJobTrigger[1] { this }, true);
        }

        #endregion
    }

    #region Public Enums

    /// <summary>
    /// Specifies trigger types in terms of the frequency that
    /// the trigger is activated.
    /// </summary>
    public enum TriggerFrequency
    {
        /// <summary>
        /// None.
        /// </summary>
        None = 0,
        /// <summary>
        /// Trigger activates once at a specified time.
        /// </summary>
        Once = 1,
        /// <summary>
        /// Trigger activates daily.
        /// </summary>
        Daily = 2,
        /// <summary>
        /// Trigger activates on a weekly basis and multiple days
        /// during the week.
        /// </summary>
        Weekly = 3,
        /// <summary>
        /// Trigger activates at user logon to the operating system.
        /// </summary>
        AtLogon = 4,
        /// <summary>
        /// Trigger activates after machine boot up.
        /// </summary>
        AtStartup = 5
    }

    #endregion

    #region JobTriggerToCimInstanceConverter
    /// <summary>
    /// Class providing implementation of PowerShell conversions for types in Microsoft.Management.Infrastructure namespace.
    /// </summary>
    public sealed class JobTriggerToCimInstanceConverter : PSTypeConverter
    {
        private static readonly string CIM_TRIGGER_NAMESPACE = @"Root\Microsoft\Windows\TaskScheduler";

        /// <summary>
        /// Determines if the converter can convert the <paramref name="sourceValue"/> parameter to the <paramref name="destinationType"/> parameter.
        /// </summary>
        /// <param name="sourceValue">The value to convert from.</param>
        /// <param name="destinationType">The type to convert to.</param>
        /// <returns>True if the converter can convert the <paramref name="sourceValue"/> parameter to the <paramref name="destinationType"/> parameter, otherwise false.</returns>
        public override bool CanConvertFrom(object sourceValue, Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(destinationType);

            return (sourceValue is ScheduledJobTrigger) && (destinationType.Equals(typeof(CimInstance)));
        }

        /// <summary>
        /// Converts the <paramref name="sourceValue"/> parameter to the <paramref name="destinationType"/> parameter using formatProvider and ignoreCase.
        /// </summary>
        /// <param name="sourceValue">The value to convert from.</param>
        /// <param name="destinationType">The type to convert to.</param>
        /// <param name="formatProvider">The format provider to use like in IFormattable's ToString.</param>
        /// <param name="ignoreCase">True if case should be ignored.</param>
        /// <returns>The <paramref name="sourceValue"/> parameter converted to the <paramref name="destinationType"/> parameter using formatProvider and ignoreCase.</returns>
        /// <exception cref="InvalidCastException">If no conversion was possible.</exception>
        public override object ConvertFrom(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            ArgumentNullException.ThrowIfNull(destinationType);

            ArgumentNullException.ThrowIfNull(sourceValue);

            ScheduledJobTrigger originalTrigger = (ScheduledJobTrigger) sourceValue;
            using (CimSession cimSession = CimSession.Create(null))
            {
                switch (originalTrigger.Frequency)
                {
                    case TriggerFrequency.Weekly:
                        return ConvertToWeekly(originalTrigger, cimSession);
                    case TriggerFrequency.Once:
                        return ConvertToOnce(originalTrigger, cimSession);
                    case TriggerFrequency.Daily:
                        return ConvertToDaily(originalTrigger, cimSession);
                    case TriggerFrequency.AtStartup:
                        return ConvertToAtStartup(originalTrigger, cimSession);
                    case TriggerFrequency.AtLogon:
                        return ConvertToAtLogon(originalTrigger, cimSession);
                    case TriggerFrequency.None:
                        return ConvertToDefault(originalTrigger, cimSession);
                    default:
                        string errorMsg = StringUtil.Format(ScheduledJobErrorStrings.UnknownTriggerFrequency,
                                                            originalTrigger.Frequency.ToString());
                        throw new PSInvalidOperationException(errorMsg);
                }
            }
        }

        /// <summary>
        /// Returns true if the converter can convert the <paramref name="sourceValue"/> parameter to the <paramref name="destinationType"/> parameter.
        /// </summary>
        /// <param name="sourceValue">The value to convert from.</param>
        /// <param name="destinationType">The type to convert to.</param>
        /// <returns>True if the converter can convert the <paramref name="sourceValue"/> parameter to the <paramref name="destinationType"/> parameter, otherwise false.</returns>
        public override bool CanConvertTo(object sourceValue, Type destinationType)
        {
            return false;
        }

        /// <summary>
        /// Converts the <paramref name="sourceValue"/> parameter to the <paramref name="destinationType"/> parameter using formatProvider and ignoreCase.
        /// </summary>
        /// <param name="sourceValue">The value to convert from.</param>
        /// <param name="destinationType">The type to convert to.</param>
        /// <param name="formatProvider">The format provider to use like in IFormattable's ToString.</param>
        /// <param name="ignoreCase">True if case should be ignored.</param>
        /// <returns>SourceValue converted to the <paramref name="destinationType"/> parameter using formatProvider and ignoreCase.</returns>
        /// <exception cref="InvalidCastException">If no conversion was possible.</exception>
        public override object ConvertTo(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            throw new NotImplementedException();
        }

        #region Helper Methods

        private CimInstance ConvertToWeekly(ScheduledJobTrigger trigger, CimSession cimSession)
        {
            CimClass cimClass = cimSession.GetClass(CIM_TRIGGER_NAMESPACE, "MSFT_TaskWeeklyTrigger");
            CimInstance cimInstance = new CimInstance(cimClass);

            cimInstance.CimInstanceProperties["DaysOfWeek"].Value = ScheduledJobWTS.ConvertDaysOfWeekToMask(trigger.DaysOfWeek);
            cimInstance.CimInstanceProperties["RandomDelay"].Value = ScheduledJobWTS.ConvertTimeSpanToWTSString(trigger.RandomDelay);
            cimInstance.CimInstanceProperties["WeeksInterval"].Value = trigger.Interval;

            AddCommonProperties(trigger, cimInstance);
            return cimInstance;
        }

        private CimInstance ConvertToOnce(ScheduledJobTrigger trigger, CimSession cimSession)
        {
            CimClass cimClass = cimSession.GetClass(CIM_TRIGGER_NAMESPACE, "MSFT_TaskTimeTrigger");
            CimInstance cimInstance = new CimInstance(cimClass);

            cimInstance.CimInstanceProperties["RandomDelay"].Value = ScheduledJobWTS.ConvertTimeSpanToWTSString(trigger.RandomDelay);

            if (trigger.RepetitionInterval != null && trigger.RepetitionDuration != null)
            {
                CimClass cimRepClass = cimSession.GetClass(CIM_TRIGGER_NAMESPACE, "MSFT_TaskRepetitionPattern");
                CimInstance cimRepInstance = new CimInstance(cimRepClass);

                cimRepInstance.CimInstanceProperties["Interval"].Value = ScheduledJobWTS.ConvertTimeSpanToWTSString(trigger.RepetitionInterval.Value);

                if (trigger.RepetitionDuration == TimeSpan.MaxValue)
                {
                    cimRepInstance.CimInstanceProperties["StopAtDurationEnd"].Value = false;
                }
                else
                {
                    cimRepInstance.CimInstanceProperties["StopAtDurationEnd"].Value = true;
                    cimRepInstance.CimInstanceProperties["Duration"].Value = ScheduledJobWTS.ConvertTimeSpanToWTSString(trigger.RepetitionDuration.Value);
                }

                cimInstance.CimInstanceProperties["Repetition"].Value = cimRepInstance;
            }

            AddCommonProperties(trigger, cimInstance);
            return cimInstance;
        }

        private CimInstance ConvertToDaily(ScheduledJobTrigger trigger, CimSession cimSession)
        {
            CimClass cimClass = cimSession.GetClass(CIM_TRIGGER_NAMESPACE, "MSFT_TaskDailyTrigger");
            CimInstance cimInstance = new CimInstance(cimClass);

            cimInstance.CimInstanceProperties["RandomDelay"].Value = ScheduledJobWTS.ConvertTimeSpanToWTSString(trigger.RandomDelay);
            cimInstance.CimInstanceProperties["DaysInterval"].Value = trigger.Interval;

            AddCommonProperties(trigger, cimInstance);
            return cimInstance;
        }

        private CimInstance ConvertToAtLogon(ScheduledJobTrigger trigger, CimSession cimSession)
        {
            CimClass cimClass = cimSession.GetClass(CIM_TRIGGER_NAMESPACE, "MSFT_TaskLogonTrigger");
            CimInstance cimInstance = new CimInstance(cimClass);

            cimInstance.CimInstanceProperties["Delay"].Value = ScheduledJobWTS.ConvertTimeSpanToWTSString(trigger.RandomDelay);

            // Convert the "AllUsers" name ("*" character) to null for Task Scheduler.
            string userId = (ScheduledJobTrigger.IsAllUsers(trigger.User)) ? null : trigger.User;
            cimInstance.CimInstanceProperties["UserId"].Value = userId;

            AddCommonProperties(trigger, cimInstance);
            return cimInstance;
        }

        private CimInstance ConvertToAtStartup(ScheduledJobTrigger trigger, CimSession cimSession)
        {
            CimClass cimClass = cimSession.GetClass(CIM_TRIGGER_NAMESPACE, "MSFT_TaskBootTrigger");
            CimInstance cimInstance = new CimInstance(cimClass);

            cimInstance.CimInstanceProperties["Delay"].Value = ScheduledJobWTS.ConvertTimeSpanToWTSString(trigger.RandomDelay);

            AddCommonProperties(trigger, cimInstance);
            return cimInstance;
        }

        private CimInstance ConvertToDefault(ScheduledJobTrigger trigger, CimSession cimSession)
        {
            CimClass cimClass = cimSession.GetClass(CIM_TRIGGER_NAMESPACE, "MSFT_TaskTrigger");
            CimInstance result = new CimInstance(cimClass);
            AddCommonProperties(trigger, result);
            return result;
        }

        private static void AddCommonProperties(ScheduledJobTrigger trigger, CimInstance cimInstance)
        {
            cimInstance.CimInstanceProperties["Enabled"].Value = trigger.Enabled;

            if (trigger.At != null)
            {
                cimInstance.CimInstanceProperties["StartBoundary"].Value = ScheduledJobWTS.ConvertDateTimeToString(trigger.At);
            }
        }

        #endregion
    }

    #endregion
}
