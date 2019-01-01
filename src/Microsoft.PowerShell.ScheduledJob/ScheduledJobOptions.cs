// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Management.Automation;
using System.Security.Permissions;

namespace Microsoft.PowerShell.ScheduledJob
{
    /// <summary>
    /// This class contains Windows Task Scheduler options.
    /// </summary>
    [Serializable]
    public sealed class ScheduledJobOptions : ISerializable
    {
        #region Private Members

        // Power settings
        private bool _startIfOnBatteries;
        private bool _stopIfGoingOnBatteries;
        private bool _wakeToRun;

        // Idle settings
        private bool _startIfNotIdle;
        private bool _stopIfGoingOffIdle;
        private bool _restartOnIdleResume;
        private TimeSpan _idleDuration;
        private TimeSpan _idleTimeout;

        // Security settings
        private bool _showInTaskScheduler;
        private bool _runElevated;

        // Misc
        private bool _runWithoutNetwork;
        private bool _donotAllowDemandStart;
        private TaskMultipleInstancePolicy _multipleInstancePolicy;

        // ScheduledJobDefinition object associated with this options object.
        private ScheduledJobDefinition _jobDefAssociation;

        #endregion

        #region Public Properties

        /// <summary>
        /// Start task if on batteries.
        /// </summary>
        public bool StartIfOnBatteries
        {
            get { return _startIfOnBatteries; }

            set { _startIfOnBatteries = value; }
        }

        /// <summary>
        /// Stop task if computer is going on batteries.
        /// </summary>
        public bool StopIfGoingOnBatteries
        {
            get { return _stopIfGoingOnBatteries; }

            set { _stopIfGoingOnBatteries = value; }
        }

        /// <summary>
        /// Wake computer to run task.
        /// </summary>
        public bool WakeToRun
        {
            get { return _wakeToRun; }

            set { _wakeToRun = value; }
        }

        /// <summary>
        /// Start task only if computer is not idle.
        /// </summary>
        public bool StartIfNotIdle
        {
            get { return _startIfNotIdle; }

            set { _startIfNotIdle = value; }
        }

        /// <summary>
        /// Stop task if computer is no longer idle.
        /// </summary>
        public bool StopIfGoingOffIdle
        {
            get { return _stopIfGoingOffIdle; }

            set { _stopIfGoingOffIdle = value; }
        }
        /// <summary>
        /// Restart task on idle resuming.
        /// </summary>
        public bool RestartOnIdleResume
        {
            get { return _restartOnIdleResume; }

            set { _restartOnIdleResume = value; }
        }

        /// <summary>
        /// How long computer must be idle before task starts.
        /// </summary>
        public TimeSpan IdleDuration
        {
            get { return _idleDuration; }

            set { _idleDuration = value; }
        }

        /// <summary>
        /// How long task manager will wait for required idle duration.
        /// </summary>
        public TimeSpan IdleTimeout
        {
            get { return _idleTimeout; }

            set { _idleTimeout = value; }
        }

        /// <summary>
        /// When true task is not shown in Task Scheduler UI.
        /// </summary>
        public bool ShowInTaskScheduler
        {
            get { return _showInTaskScheduler; }

            set { _showInTaskScheduler = value; }
        }

        /// <summary>
        /// Run task with elevated privileges.
        /// </summary>
        public bool RunElevated
        {
            get { return _runElevated; }

            set { _runElevated = value; }
        }

        /// <summary>
        /// Run task even if network is not available.
        /// </summary>
        public bool RunWithoutNetwork
        {
            get { return _runWithoutNetwork; }

            set { _runWithoutNetwork = value; }
        }

        /// <summary>
        /// Do not allow a task to be started on demand.
        /// </summary>
        public bool DoNotAllowDemandStart
        {
            get { return _donotAllowDemandStart; }

            set { _donotAllowDemandStart = value; }
        }

        /// <summary>
        /// Multiple task instance policy.
        /// </summary>
        public TaskMultipleInstancePolicy MultipleInstancePolicy
        {
            get { return _multipleInstancePolicy; }

            set { _multipleInstancePolicy = value; }
        }

        /// <summary>
        /// ScheduledJobDefinition object associated with this options object.
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
        public ScheduledJobOptions()
        {
            _startIfOnBatteries = false;
            _stopIfGoingOnBatteries = true;
            _wakeToRun = false;
            _startIfNotIdle = true;
            _stopIfGoingOffIdle = false;
            _restartOnIdleResume = false;
            _idleDuration = new TimeSpan(0, 10, 0);
            _idleTimeout = new TimeSpan(1, 0, 0);
            _showInTaskScheduler = true;
            _runElevated = false;
            _runWithoutNetwork = true;
            _donotAllowDemandStart = false;
            _multipleInstancePolicy = TaskMultipleInstancePolicy.IgnoreNew;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="startIfOnBatteries"></param>
        /// <param name="stopIfGoingOnBatters"></param>
        /// <param name="wakeToRun"></param>
        /// <param name="startIfNotIdle"></param>
        /// <param name="stopIfGoingOffIdle"></param>
        /// <param name="restartOnIdleResume"></param>
        /// <param name="idleDuration"></param>
        /// <param name="idleTimeout"></param>
        /// <param name="showInTaskScheduler"></param>
        /// <param name="runElevated"></param>
        /// <param name="runWithoutNetwork"></param>
        /// <param name="donotAllowDemandStart"></param>
        /// <param name="multipleInstancePolicy"></param>
        internal ScheduledJobOptions(
            bool startIfOnBatteries,
            bool stopIfGoingOnBatters,
            bool wakeToRun,
            bool startIfNotIdle,
            bool stopIfGoingOffIdle,
            bool restartOnIdleResume,
            TimeSpan idleDuration,
            TimeSpan idleTimeout,
            bool showInTaskScheduler,
            bool runElevated,
            bool runWithoutNetwork,
            bool donotAllowDemandStart,
            TaskMultipleInstancePolicy multipleInstancePolicy)
        {
            _startIfOnBatteries = startIfOnBatteries;
            _stopIfGoingOnBatteries = stopIfGoingOnBatters;
            _wakeToRun = wakeToRun;
            _startIfNotIdle = startIfNotIdle;
            _stopIfGoingOffIdle = stopIfGoingOffIdle;
            _restartOnIdleResume = restartOnIdleResume;
            _idleDuration = idleDuration;
            _idleTimeout = idleTimeout;
            _showInTaskScheduler = showInTaskScheduler;
            _runElevated = runElevated;
            _runWithoutNetwork = runWithoutNetwork;
            _donotAllowDemandStart = donotAllowDemandStart;
            _multipleInstancePolicy = multipleInstancePolicy;
        }

        /// <summary>
        /// Copy Constructor.
        /// </summary>
        /// <param name="copyOptions">Copy from.</param>
        internal ScheduledJobOptions(
            ScheduledJobOptions copyOptions)
        {
            if (copyOptions == null)
            {
                throw new PSArgumentNullException("copyOptions");
            }

            _startIfOnBatteries = copyOptions.StartIfOnBatteries;
            _stopIfGoingOnBatteries = copyOptions.StopIfGoingOnBatteries;
            _wakeToRun = copyOptions.WakeToRun;
            _startIfNotIdle = copyOptions.StartIfNotIdle;
            _stopIfGoingOffIdle = copyOptions.StopIfGoingOffIdle;
            _restartOnIdleResume = copyOptions.RestartOnIdleResume;
            _idleDuration = copyOptions.IdleDuration;
            _idleTimeout = copyOptions.IdleTimeout;
            _showInTaskScheduler = copyOptions.ShowInTaskScheduler;
            _runElevated = copyOptions.RunElevated;
            _runWithoutNetwork = copyOptions.RunWithoutNetwork;
            _donotAllowDemandStart = copyOptions.DoNotAllowDemandStart;
            _multipleInstancePolicy = copyOptions.MultipleInstancePolicy;

            _jobDefAssociation = copyOptions.JobDefinition;
        }

        #endregion

        #region ISerializable Implementation

        /// <summary>
        /// Serialization constructor.
        /// </summary>
        /// <param name="info">SerializationInfo.</param>
        /// <param name="context">StreamingContext.</param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        private ScheduledJobOptions(
            SerializationInfo info,
            StreamingContext context)
        {
            if (info == null)
            {
                throw new PSArgumentNullException("info");
            }

            _startIfOnBatteries = info.GetBoolean("StartIfOnBatteries_Value");
            _stopIfGoingOnBatteries = info.GetBoolean("StopIfGoingOnBatteries_Value");
            _wakeToRun = info.GetBoolean("WakeToRun_Value");
            _startIfNotIdle = info.GetBoolean("StartIfNotIdle_Value");
            _stopIfGoingOffIdle = info.GetBoolean("StopIfGoingOffIdle_Value");
            _restartOnIdleResume = info.GetBoolean("RestartOnIdleResume_Value");
            _idleDuration = (TimeSpan)info.GetValue("IdleDuration_Value", typeof(TimeSpan));
            _idleTimeout = (TimeSpan)info.GetValue("IdleTimeout_Value", typeof(TimeSpan));
            _showInTaskScheduler = info.GetBoolean("ShowInTaskScheduler_Value");
            _runElevated = info.GetBoolean("RunElevated_Value");
            _runWithoutNetwork = info.GetBoolean("RunWithoutNetwork_Value");
            _donotAllowDemandStart = info.GetBoolean("DoNotAllowDemandStart_Value");
            _multipleInstancePolicy = (TaskMultipleInstancePolicy)info.GetValue("TaskMultipleInstancePolicy_Value", typeof(TaskMultipleInstancePolicy));

            // Runtime reference and not saved to store.
            _jobDefAssociation = null;
        }

        /// <summary>
        /// GetObjectData for ISerializable implementation.
        /// </summary>
        /// <param name="info">SerializationInfo.</param>
        /// <param name="context">StreamingContext.</param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new PSArgumentNullException("info");
            }

            info.AddValue("StartIfOnBatteries_Value", _startIfOnBatteries);
            info.AddValue("StopIfGoingOnBatteries_Value", _stopIfGoingOnBatteries);
            info.AddValue("WakeToRun_Value", _wakeToRun);
            info.AddValue("StartIfNotIdle_Value", _startIfNotIdle);
            info.AddValue("StopIfGoingOffIdle_Value", _stopIfGoingOffIdle);
            info.AddValue("RestartOnIdleResume_Value", _restartOnIdleResume);
            info.AddValue("IdleDuration_Value", _idleDuration);
            info.AddValue("IdleTimeout_Value", _idleTimeout);
            info.AddValue("ShowInTaskScheduler_Value", _showInTaskScheduler);
            info.AddValue("RunElevated_Value", _runElevated);
            info.AddValue("RunWithoutNetwork_Value", _runWithoutNetwork);
            info.AddValue("DoNotAllowDemandStart_Value", _donotAllowDemandStart);
            info.AddValue("TaskMultipleInstancePolicy_Value", _multipleInstancePolicy);
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
                string msg = StringUtil.Format(ScheduledJobErrorStrings.NoAssociatedJobDefinitionForOption);

                throw new RuntimeException(msg);
            }

            _jobDefAssociation.UpdateOptions(this, true);
        }

        #endregion
    }

    #region Public Enums

    /// <summary>
    /// Enumerates Task Scheduler options for multiple instance polices of
    /// scheduled tasks (jobs).
    /// </summary>
    public enum TaskMultipleInstancePolicy
    {
        /// <summary>
        /// None.
        /// </summary>
        None = 0,
        /// <summary>
        /// Ignore a new instance of the task (job)
        /// </summary>
        IgnoreNew = 1,
        /// <summary>
        /// Allow parallel running of a task (job)
        /// </summary>
        Parallel = 2,
        /// <summary>
        /// Queue up multiple instances of a task (job)
        /// </summary>
        Queue = 3,
        /// <summary>
        /// Stop currently running task (job) and start a new one.
        /// </summary>
        StopExisting = 4
    }

    #endregion
}
