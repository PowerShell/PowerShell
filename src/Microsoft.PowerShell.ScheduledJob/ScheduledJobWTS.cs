// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;

using TaskScheduler;

namespace Microsoft.PowerShell.ScheduledJob
{
    /// <summary>
    /// Managed code class to provide Windows Task Scheduler functionality for
    /// scheduled jobs.
    /// </summary>
    internal sealed class ScheduledJobWTS : IDisposable
    {
        #region Private Members

        private ITaskService _taskScheduler;
        private ITaskFolder _iRootFolder;

        private const short WTSSunday = 0x01;
        private const short WTSMonday = 0x02;
        private const short WTSTuesday = 0x04;
        private const short WTSWednesday = 0x08;
        private const short WTSThursday = 0x10;
        private const short WTSFriday = 0x20;
        private const short WTSSaturday = 0x40;

        // Task Scheduler folders for PowerShell scheduled job tasks.
        private const string TaskSchedulerWindowsFolder = @"\Microsoft\Windows";
        private const string ScheduledJobSubFolder = @"PowerShell\ScheduledJobs";
        private const string ScheduledJobTasksRootFolder = @"\Microsoft\Windows\PowerShell\ScheduledJobs";

        // Define a single Action Id since PowerShell Scheduled Job tasks will have only one action.
        private const string ScheduledJobTaskActionId = "StartPowerShellJob";

        #endregion

        #region Constructors

        public ScheduledJobWTS()
        {
            // Create the Windows Task Scheduler object.
            _taskScheduler = (ITaskService)new TaskScheduler.TaskScheduler();

            // Connect the task scheduler object to the local machine
            // using the current user security token.
            _taskScheduler.Connect(null, null, null, null);

            // Get or create the root folder in Task Scheduler for PowerShell scheduled jobs.
            _iRootFolder = GetRootFolder();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Retrieves job triggers from WTS with provided task Id.
        /// </summary>
        /// <param name="taskId">Task Id.</param>
        /// <exception cref="ScheduledJobException">Task not found.</exception>
        /// <returns>ScheduledJobTriggers.</returns>
        public Collection<ScheduledJobTrigger> GetJobTriggers(
            string taskId)
        {
            if (string.IsNullOrEmpty(taskId))
            {
                throw new PSArgumentException("taskId");
            }

            ITaskDefinition iTaskDefinition = FindTask(taskId);

            Collection<ScheduledJobTrigger> jobTriggers = new Collection<ScheduledJobTrigger>();
            ITriggerCollection iTriggerCollection = iTaskDefinition.Triggers;
            if (iTriggerCollection != null)
            {
                foreach (ITrigger iTrigger in iTriggerCollection)
                {
                    ScheduledJobTrigger jobTrigger = CreateJobTrigger(iTrigger);
                    if (jobTrigger == null)
                    {
                        string msg = StringUtil.Format(ScheduledJobErrorStrings.UnknownTriggerType, taskId, iTrigger.Id);
                        throw new ScheduledJobException(msg);
                    }

                    jobTriggers.Add(jobTrigger);
                }
            }

            return jobTriggers;
        }

        /// <summary>
        /// Retrieves options for the provided task Id.
        /// </summary>
        /// <param name="taskId">Task Id.</param>
        /// <exception cref="ScheduledJobException">Task not found.</exception>
        /// <returns>ScheduledJobOptions.</returns>
        public ScheduledJobOptions GetJobOptions(
            string taskId)
        {
            if (string.IsNullOrEmpty(taskId))
            {
                throw new PSArgumentException("taskId");
            }

            ITaskDefinition iTaskDefinition = FindTask(taskId);

            return CreateJobOptions(iTaskDefinition);
        }

        /// <summary>
        /// Returns a boolean indicating whether the job/task is enabled
        /// in the Task Scheduler.
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        public bool GetTaskEnabled(
            string taskId)
        {
            if (string.IsNullOrEmpty(taskId))
            {
                throw new PSArgumentException("taskId");
            }

            ITaskDefinition iTaskDefinition = FindTask(taskId);

            return iTaskDefinition.Settings.Enabled;
        }

        /// <summary>
        /// Creates a new task in WTS with information from ScheduledJobDefinition.
        /// </summary>
        /// <param name="definition">ScheduledJobDefinition.</param>
        public void CreateTask(
            ScheduledJobDefinition definition)
        {
            if (definition == null)
            {
                throw new PSArgumentNullException("definition");
            }

            // Create task definition
            ITaskDefinition iTaskDefinition = _taskScheduler.NewTask(0);

            // Add task options.
            AddTaskOptions(iTaskDefinition, definition.Options);

            // Add task triggers.
            foreach (ScheduledJobTrigger jobTrigger in definition.JobTriggers)
            {
                AddTaskTrigger(iTaskDefinition, jobTrigger);
            }

            // Add task action.
            AddTaskAction(iTaskDefinition, definition);

            // Create a security descriptor for the current user so that only the user
            // (and Local System account) can see/access the registered task.
            string startSddl = "D:P(A;;GA;;;SY)(A;;GA;;;BA)";   // DACL Allow Generic Access to System and BUILTIN\Administrators.
            System.Security.Principal.SecurityIdentifier userSid =
                System.Security.Principal.WindowsIdentity.GetCurrent().User;
            CommonSecurityDescriptor SDesc = new CommonSecurityDescriptor(false, false, startSddl);
            SDesc.DiscretionaryAcl.AddAccess(AccessControlType.Allow, userSid, 0x10000000, InheritanceFlags.None, PropagationFlags.None);
            string sddl = SDesc.GetSddlForm(AccessControlSections.All);

            // Register this new task with the Task Scheduler.
            if (definition.Credential == null)
            {
                // Register task to run as currently logged on user.
                _iRootFolder.RegisterTaskDefinition(
                    definition.Name,
                    iTaskDefinition,
                    (int)_TASK_CREATION.TASK_CREATE,
                    null,       // User name
                    null,       // Password
                    _TASK_LOGON_TYPE.TASK_LOGON_S4U,
                    sddl);
            }
            else
            {
                // Register task to run under provided user account/credentials.
                _iRootFolder.RegisterTaskDefinition(
                    definition.Name,
                    iTaskDefinition,
                    (int)_TASK_CREATION.TASK_CREATE,
                    definition.Credential.UserName,
                    GetCredentialPassword(definition.Credential),
                    _TASK_LOGON_TYPE.TASK_LOGON_PASSWORD,
                    sddl);
            }
        }

        /// <summary>
        /// Removes the WTS task for this ScheduledJobDefinition.
        /// Throws error if one or more instances of this task are running.
        /// Force parameter will stop all running instances and remove task.
        /// </summary>
        /// <param name="definition">ScheduledJobDefinition.</param>
        /// <param name="force">Force running instances to stop and remove task.</param>
        public void RemoveTask(
            ScheduledJobDefinition definition,
            bool force = false)
        {
            if (definition == null)
            {
                throw new PSArgumentNullException("definition");
            }

            RemoveTaskByName(definition.Name, force, false);
        }

        /// <summary>
        /// Removes a Task Scheduler task from the PowerShell/ScheduledJobs folder
        /// based on a task name.
        /// </summary>
        /// <param name="taskName">Task Scheduler task name.</param>
        /// <param name="force">Force running instances to stop and remove task.</param>
        /// <param name="firstCheckForTask">First check for existence of task.</param>
        public void RemoveTaskByName(
            string taskName,
            bool force,
            bool firstCheckForTask)
        {
            // Get registered task.
            IRegisteredTask iRegisteredTask = null;
            try
            {
                iRegisteredTask = _iRootFolder.GetTask(taskName);
            }
            catch (System.IO.DirectoryNotFoundException)
            {
                if (!firstCheckForTask)
                {
                    throw;
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                if (!firstCheckForTask)
                {
                    throw;
                }
            }

            if (iRegisteredTask == null)
            {
                return;
            }

            // Check to see if any instances of this job/task is running.
            IRunningTaskCollection iRunningTasks = iRegisteredTask.GetInstances(0);
            if (iRunningTasks.Count > 0)
            {
                if (!force)
                {
                    string msg = StringUtil.Format(ScheduledJobErrorStrings.CannotRemoveTaskRunningInstance, taskName);
                    throw new ScheduledJobException(msg);
                }

                // Stop all running tasks.
                iRegisteredTask.Stop(0);
            }

            // Remove task.
            _iRootFolder.DeleteTask(taskName, 0);
        }

        /// <summary>
        /// Starts task running from Task Scheduler.
        /// </summary>
        /// <param name="definition">ScheduledJobDefinition.</param>
        /// <exception cref="System.IO.DirectoryNotFoundException"></exception>
        /// <exception cref="System.IO.FileNotFoundException"></exception>
        public void RunTask(
            ScheduledJobDefinition definition)
        {
            // Get registered task.
            IRegisteredTask iRegisteredTask = _iRootFolder.GetTask(definition.Name);

            // Run task.
            iRegisteredTask.Run(null);
        }

        /// <summary>
        /// Updates an existing task in WTS with information from
        /// ScheduledJobDefinition.
        /// </summary>
        /// <param name="definition">ScheduledJobDefinition.</param>
        public void UpdateTask(
            ScheduledJobDefinition definition)
        {
            if (definition == null)
            {
                throw new PSArgumentNullException("definition");
            }

            // Get task to update.
            ITaskDefinition iTaskDefinition = FindTask(definition.Name);

            // Replace options.
            AddTaskOptions(iTaskDefinition, definition.Options);

            // Set enabled state.
            iTaskDefinition.Settings.Enabled = definition.Enabled;

            // Replace triggers.
            iTaskDefinition.Triggers.Clear();
            foreach (ScheduledJobTrigger jobTrigger in definition.JobTriggers)
            {
                AddTaskTrigger(iTaskDefinition, jobTrigger);
            }

            // Replace action.
            iTaskDefinition.Actions.Clear();
            AddTaskAction(iTaskDefinition, definition);

            // Register updated task.
            if (definition.Credential == null)
            {
                // Register task to run as currently logged on user.
                _iRootFolder.RegisterTaskDefinition(
                    definition.Name,
                    iTaskDefinition,
                    (int)_TASK_CREATION.TASK_UPDATE,
                    null,           // User name
                    null,           // Password
                    _TASK_LOGON_TYPE.TASK_LOGON_S4U,
                    null);
            }
            else
            {
                // Register task to run under provided user account/credentials.
                _iRootFolder.RegisterTaskDefinition(
                    definition.Name,
                    iTaskDefinition,
                    (int)_TASK_CREATION.TASK_UPDATE,
                    definition.Credential.UserName,
                    GetCredentialPassword(definition.Credential),
                    _TASK_LOGON_TYPE.TASK_LOGON_PASSWORD,
                    null);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates a new WTS trigger based on the provided ScheduledJobTrigger object
        /// and adds it to the provided ITaskDefinition object.
        /// </summary>
        /// <param name="iTaskDefinition">ITaskDefinition.</param>
        /// <param name="jobTrigger">ScheduledJobTrigger.</param>
        private void AddTaskTrigger(
            ITaskDefinition iTaskDefinition,
            ScheduledJobTrigger jobTrigger)
        {
            ITrigger iTrigger = null;

            switch (jobTrigger.Frequency)
            {
                case TriggerFrequency.AtStartup:
                    {
                        iTrigger = iTaskDefinition.Triggers.Create(_TASK_TRIGGER_TYPE2.TASK_TRIGGER_BOOT);
                        IBootTrigger iBootTrigger = iTrigger as IBootTrigger;
                        Debug.Assert(iBootTrigger != null);

                        iBootTrigger.Delay = ConvertTimeSpanToWTSString(jobTrigger.RandomDelay);

                        iTrigger.Id = jobTrigger.Id.ToString(CultureInfo.InvariantCulture);
                        iTrigger.Enabled = jobTrigger.Enabled;
                    }

                    break;

                case TriggerFrequency.AtLogon:
                    {
                        iTrigger = iTaskDefinition.Triggers.Create(_TASK_TRIGGER_TYPE2.TASK_TRIGGER_LOGON);
                        ILogonTrigger iLogonTrigger = iTrigger as ILogonTrigger;
                        Debug.Assert(iLogonTrigger != null);

                        iLogonTrigger.UserId = ScheduledJobTrigger.IsAllUsers(jobTrigger.User) ? null : jobTrigger.User;
                        iLogonTrigger.Delay = ConvertTimeSpanToWTSString(jobTrigger.RandomDelay);

                        iTrigger.Id = jobTrigger.Id.ToString(CultureInfo.InvariantCulture);
                        iTrigger.Enabled = jobTrigger.Enabled;
                    }

                    break;

                case TriggerFrequency.Once:
                    {
                        iTrigger = iTaskDefinition.Triggers.Create(_TASK_TRIGGER_TYPE2.TASK_TRIGGER_TIME);
                        ITimeTrigger iTimeTrigger = iTrigger as ITimeTrigger;
                        Debug.Assert(iTimeTrigger != null);

                        iTimeTrigger.RandomDelay = ConvertTimeSpanToWTSString(jobTrigger.RandomDelay);

                        // Time trigger repetition.
                        if (jobTrigger.RepetitionInterval != null &&
                            jobTrigger.RepetitionDuration != null)
                        {
                            iTimeTrigger.Repetition.Interval = ConvertTimeSpanToWTSString(jobTrigger.RepetitionInterval.Value);
                            if (jobTrigger.RepetitionDuration.Value == TimeSpan.MaxValue)
                            {
                                iTimeTrigger.Repetition.StopAtDurationEnd = false;
                            }
                            else
                            {
                                iTimeTrigger.Repetition.StopAtDurationEnd = true;
                                iTimeTrigger.Repetition.Duration = ConvertTimeSpanToWTSString(jobTrigger.RepetitionDuration.Value);
                            }
                        }

                        iTrigger.StartBoundary = ConvertDateTimeToString(jobTrigger.At);
                        iTrigger.Id = jobTrigger.Id.ToString(CultureInfo.InvariantCulture);
                        iTrigger.Enabled = jobTrigger.Enabled;
                    }

                    break;

                case TriggerFrequency.Daily:
                    {
                        iTrigger = iTaskDefinition.Triggers.Create(_TASK_TRIGGER_TYPE2.TASK_TRIGGER_DAILY);
                        IDailyTrigger iDailyTrigger = iTrigger as IDailyTrigger;
                        Debug.Assert(iDailyTrigger != null);

                        iDailyTrigger.RandomDelay = ConvertTimeSpanToWTSString(jobTrigger.RandomDelay);
                        iDailyTrigger.DaysInterval = (short)jobTrigger.Interval;

                        iTrigger.StartBoundary = ConvertDateTimeToString(jobTrigger.At);
                        iTrigger.Id = jobTrigger.Id.ToString(CultureInfo.InvariantCulture);
                        iTrigger.Enabled = jobTrigger.Enabled;
                    }

                    break;

                case TriggerFrequency.Weekly:
                    {
                        iTrigger = iTaskDefinition.Triggers.Create(_TASK_TRIGGER_TYPE2.TASK_TRIGGER_WEEKLY);
                        IWeeklyTrigger iWeeklyTrigger = iTrigger as IWeeklyTrigger;
                        Debug.Assert(iWeeklyTrigger != null);

                        iWeeklyTrigger.RandomDelay = ConvertTimeSpanToWTSString(jobTrigger.RandomDelay);
                        iWeeklyTrigger.WeeksInterval = (short)jobTrigger.Interval;
                        iWeeklyTrigger.DaysOfWeek = ConvertDaysOfWeekToMask(jobTrigger.DaysOfWeek);

                        iTrigger.StartBoundary = ConvertDateTimeToString(jobTrigger.At);
                        iTrigger.Id = jobTrigger.Id.ToString(CultureInfo.InvariantCulture);
                        iTrigger.Enabled = jobTrigger.Enabled;
                    }

                    break;
            }
        }

        /// <summary>
        /// Creates a ScheduledJobTrigger object based on a provided WTS ITrigger.
        /// </summary>
        /// <param name="iTrigger">ITrigger.</param>
        /// <returns>ScheduledJobTrigger.</returns>
        private ScheduledJobTrigger CreateJobTrigger(
            ITrigger iTrigger)
        {
            ScheduledJobTrigger rtnJobTrigger = null;

            if (iTrigger is IBootTrigger)
            {
                IBootTrigger iBootTrigger = (IBootTrigger)iTrigger;
                rtnJobTrigger = ScheduledJobTrigger.CreateAtStartupTrigger(
                    ParseWTSTime(iBootTrigger.Delay),
                    ConvertStringId(iBootTrigger.Id),
                    iBootTrigger.Enabled);
            }
            else if (iTrigger is ILogonTrigger)
            {
                ILogonTrigger iLogonTrigger = (ILogonTrigger)iTrigger;
                rtnJobTrigger = ScheduledJobTrigger.CreateAtLogOnTrigger(
                    iLogonTrigger.UserId,
                    ParseWTSTime(iLogonTrigger.Delay),
                    ConvertStringId(iLogonTrigger.Id),
                    iLogonTrigger.Enabled);
            }
            else if (iTrigger is ITimeTrigger)
            {
                ITimeTrigger iTimeTrigger = (ITimeTrigger)iTrigger;
                TimeSpan repInterval = ParseWTSTime(iTimeTrigger.Repetition.Interval);
                TimeSpan repDuration = (repInterval != TimeSpan.Zero && iTimeTrigger.Repetition.StopAtDurationEnd == false) ?
                    TimeSpan.MaxValue : ParseWTSTime(iTimeTrigger.Repetition.Duration);
                rtnJobTrigger = ScheduledJobTrigger.CreateOnceTrigger(
                    DateTime.Parse(iTimeTrigger.StartBoundary, CultureInfo.InvariantCulture),
                    ParseWTSTime(iTimeTrigger.RandomDelay),
                    repInterval,
                    repDuration,
                    ConvertStringId(iTimeTrigger.Id),
                    iTimeTrigger.Enabled);
            }
            else if (iTrigger is IDailyTrigger)
            {
                IDailyTrigger iDailyTrigger = (IDailyTrigger)iTrigger;
                rtnJobTrigger = ScheduledJobTrigger.CreateDailyTrigger(
                    DateTime.Parse(iDailyTrigger.StartBoundary, CultureInfo.InvariantCulture),
                    (Int32)iDailyTrigger.DaysInterval,
                    ParseWTSTime(iDailyTrigger.RandomDelay),
                    ConvertStringId(iDailyTrigger.Id),
                    iDailyTrigger.Enabled);
            }
            else if (iTrigger is IWeeklyTrigger)
            {
                IWeeklyTrigger iWeeklyTrigger = (IWeeklyTrigger)iTrigger;
                rtnJobTrigger = ScheduledJobTrigger.CreateWeeklyTrigger(
                    DateTime.Parse(iWeeklyTrigger.StartBoundary, CultureInfo.InvariantCulture),
                    (Int32)iWeeklyTrigger.WeeksInterval,
                    ConvertMaskToDaysOfWeekArray(iWeeklyTrigger.DaysOfWeek),
                    ParseWTSTime(iWeeklyTrigger.RandomDelay),
                    ConvertStringId(iWeeklyTrigger.Id),
                    iWeeklyTrigger.Enabled);
            }

            return rtnJobTrigger;
        }

        private void AddTaskOptions(
            ITaskDefinition iTaskDefinition,
            ScheduledJobOptions jobOptions)
        {
            iTaskDefinition.Settings.DisallowStartIfOnBatteries = !jobOptions.StartIfOnBatteries;
            iTaskDefinition.Settings.StopIfGoingOnBatteries = jobOptions.StopIfGoingOnBatteries;
            iTaskDefinition.Settings.WakeToRun = jobOptions.WakeToRun;
            iTaskDefinition.Settings.RunOnlyIfIdle = !jobOptions.StartIfNotIdle;
            iTaskDefinition.Settings.IdleSettings.StopOnIdleEnd = jobOptions.StopIfGoingOffIdle;
            iTaskDefinition.Settings.IdleSettings.RestartOnIdle = jobOptions.RestartOnIdleResume;
            iTaskDefinition.Settings.IdleSettings.IdleDuration = ConvertTimeSpanToWTSString(jobOptions.IdleDuration);
            iTaskDefinition.Settings.IdleSettings.WaitTimeout = ConvertTimeSpanToWTSString(jobOptions.IdleTimeout);
            iTaskDefinition.Settings.Hidden = !jobOptions.ShowInTaskScheduler;
            iTaskDefinition.Settings.RunOnlyIfNetworkAvailable = !jobOptions.RunWithoutNetwork;
            iTaskDefinition.Settings.AllowDemandStart = !jobOptions.DoNotAllowDemandStart;
            iTaskDefinition.Settings.MultipleInstances = ConvertFromMultiInstances(jobOptions.MultipleInstancePolicy);
            iTaskDefinition.Principal.RunLevel = (jobOptions.RunElevated) ?
                                                    _TASK_RUNLEVEL.TASK_RUNLEVEL_HIGHEST : _TASK_RUNLEVEL.TASK_RUNLEVEL_LUA;
        }

        private ScheduledJobOptions CreateJobOptions(
            ITaskDefinition iTaskDefinition)
        {
            ITaskSettings iTaskSettings = iTaskDefinition.Settings;
            IPrincipal iPrincipal = iTaskDefinition.Principal;

            return new ScheduledJobOptions(
                        !iTaskSettings.DisallowStartIfOnBatteries,
                        iTaskSettings.StopIfGoingOnBatteries,
                        iTaskSettings.WakeToRun,
                        !iTaskSettings.RunOnlyIfIdle,
                        iTaskSettings.IdleSettings.StopOnIdleEnd,
                        iTaskSettings.IdleSettings.RestartOnIdle,
                        ParseWTSTime(iTaskSettings.IdleSettings.IdleDuration),
                        ParseWTSTime(iTaskSettings.IdleSettings.WaitTimeout),
                        !iTaskSettings.Hidden,
                        iPrincipal.RunLevel == _TASK_RUNLEVEL.TASK_RUNLEVEL_HIGHEST,
                        !iTaskSettings.RunOnlyIfNetworkAvailable,
                        !iTaskSettings.AllowDemandStart,
                        ConvertToMultiInstances(iTaskSettings));
        }

        private void AddTaskAction(
            ITaskDefinition iTaskDefinition,
            ScheduledJobDefinition definition)
        {
            IExecAction iExecAction = iTaskDefinition.Actions.Create(_TASK_ACTION_TYPE.TASK_ACTION_EXEC) as IExecAction;
            Debug.Assert(iExecAction != null);

            iExecAction.Id = ScheduledJobTaskActionId;
            iExecAction.Path = definition.PSExecutionPath;
            iExecAction.Arguments = definition.PSExecutionArgs;
        }

        /// <summary>
        /// Gets and returns the unsecured password for the provided
        /// PSCredential object.
        /// </summary>
        /// <param name="credential">PSCredential.</param>
        /// <returns>Unsecured password string.</returns>
        private string GetCredentialPassword(PSCredential credential)
        {
            if (credential == null)
            {
                return null;
            }

            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(credential.Password);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        #endregion

        #region Private Utility Methods

        /// <summary>
        /// Gets the Task Scheduler root folder for Scheduled Jobs or
        /// creates it if it does not exist.
        /// </summary>
        /// <returns>Scheduled Jobs root folder.</returns>
        private ITaskFolder GetRootFolder()
        {
            ITaskFolder iTaskRootFolder = null;

            try
            {
                iTaskRootFolder = _taskScheduler.GetFolder(ScheduledJobTasksRootFolder);
            }
            catch (System.IO.DirectoryNotFoundException)
            {
            }
            catch (System.IO.FileNotFoundException)
            {
                // This can be thrown if COM interop tries to load the Microsoft.PowerShell.ScheduledJob
                // assembly again.
            }

            if (iTaskRootFolder == null)
            {
                // Create the PowerShell Scheduled Job root folder.
                ITaskFolder iTSWindowsFolder = _taskScheduler.GetFolder(TaskSchedulerWindowsFolder);
                iTaskRootFolder = iTSWindowsFolder.CreateFolder(ScheduledJobSubFolder);
            }

            return iTaskRootFolder;
        }

        /// <summary>
        /// Finds a task with the provided Task Id and returns it as
        /// a ITaskDefinition object.
        /// </summary>
        /// <param name="taskId">Task Id.</param>
        /// <returns>ITaskDefinition.</returns>
        private ITaskDefinition FindTask(string taskId)
        {
            try
            {
                ITaskFolder iTaskFolder = _taskScheduler.GetFolder(ScheduledJobTasksRootFolder);
                IRegisteredTask iRegisteredTask = iTaskFolder.GetTask(taskId);
                return iRegisteredTask.Definition;
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.CannotFindTaskId, taskId);
                throw new ScheduledJobException(msg, e);
            }
        }

        private Int32 ConvertStringId(string triggerId)
        {
            Int32 triggerIdVal = 0;

            try
            {
                triggerIdVal = Convert.ToInt32(triggerId);
            }
            catch (FormatException)
            { }
            catch (OverflowException)
            { }

            return triggerIdVal;
        }

        /// <summary>
        /// Helper method to parse a WTS time string and return
        /// a corresponding TimeSpan object.  Note that the
        /// year and month values are ignored.
        /// Format:
        /// "PnYnMnDTnHnMnS"
        /// "P" - Date separator
        ///  "nY" - year value.
        ///  "nM" - month value.
        ///  "nD" - day value.
        /// "T" - Time separator
        ///  "nH" - hour value.
        ///  "nM" - minute value.
        ///  "nS" - second value.
        /// </summary>
        /// <param name="wtsTime">Formatted time string.</param>
        /// <returns>TimeSpan.</returns>
        private TimeSpan ParseWTSTime(string wtsTime)
        {
            if (string.IsNullOrEmpty(wtsTime))
            {
                return new TimeSpan(0);
            }

            int days = 0;
            int hours = 0;
            int minutes = 0;
            int seconds = 0;
            int indx = 0;
            int length = wtsTime.Length;
            StringBuilder str = new StringBuilder();

            try
            {
                while (indx != length)
                {
                    char c = wtsTime[indx++];

                    switch (c)
                    {
                        case 'P':
                            str.Clear();
                            while (indx != length &&
                                   wtsTime[indx] != 'T')
                            {
                                char c2 = wtsTime[indx++];
                                if (c2 == 'Y')
                                {
                                    // Ignore year value.
                                    str.Clear();
                                }
                                else if (c2 == 'M')
                                {
                                    // Ignore month value.
                                    str.Clear();
                                }
                                else if (c2 == 'D')
                                {
                                    days = Convert.ToInt32(str.ToString(), CultureInfo.InvariantCulture);
                                    str.Clear();
                                }
                                else if (c2 >= '0' && c2 <= '9')
                                {
                                    str.Append(c2);
                                }
                            }

                            break;

                        case 'T':
                            str.Clear();
                            while (indx != length &&
                                   wtsTime[indx] != 'P')
                            {
                                char c2 = wtsTime[indx++];
                                if (c2 == 'H')
                                {
                                    hours = Convert.ToInt32(str.ToString(), CultureInfo.InvariantCulture);
                                    str.Clear();
                                }
                                else if (c2 == 'M')
                                {
                                    minutes = Convert.ToInt32(str.ToString(), CultureInfo.InvariantCulture);
                                    str.Clear();
                                }
                                else if (c2 == 'S')
                                {
                                    seconds = Convert.ToInt32(str.ToString(), CultureInfo.InvariantCulture);
                                    str.Clear();
                                }
                                else if (c2 >= '0' && c2 <= '9')
                                {
                                    str.Append(c2);
                                }
                            }

                            break;
                    }
                }
            }
            catch (FormatException)
            { }
            catch (OverflowException)
            { }

            return new TimeSpan(days, hours, minutes, seconds);
        }

        /// <summary>
        /// Creates WTS formatted time string based on TimeSpan parameter.
        /// </summary>
        /// <param name="time">TimeSpan.</param>
        /// <returns>WTS time string.</returns>
        internal static string ConvertTimeSpanToWTSString(TimeSpan time)
        {
            return string.Format(
                    CultureInfo.InvariantCulture,
                    "P{0}DT{1}H{2}M{3}S",
                    time.Days,
                    time.Hours,
                    time.Minutes,
                    time.Seconds);
        }

        /// <summary>
        /// Converts DateTime to string for WTS.
        /// </summary>
        /// <param name="dt">DateTime.</param>
        /// <returns>DateTime string.</returns>
        internal static string ConvertDateTimeToString(DateTime? dt)
        {
            if (dt == null)
            {
                return string.Empty;
            }
            else
            {
                return dt.Value.ToString("s", CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Returns a bitmask representing days of week as
        /// required by Windows Task Scheduler API.
        /// </summary>
        /// <param name="daysOfWeek">Array of DayOfWeek.</param>
        /// <returns>WTS days of week mask.</returns>
        internal static short ConvertDaysOfWeekToMask(IEnumerable<DayOfWeek> daysOfWeek)
        {
            short rtnValue = 0;
            foreach (DayOfWeek day in daysOfWeek)
            {
                switch (day)
                {
                    case DayOfWeek.Sunday:
                        rtnValue |= WTSSunday;
                        break;

                    case DayOfWeek.Monday:
                        rtnValue |= WTSMonday;
                        break;

                    case DayOfWeek.Tuesday:
                        rtnValue |= WTSTuesday;
                        break;

                    case DayOfWeek.Wednesday:
                        rtnValue |= WTSWednesday;
                        break;

                    case DayOfWeek.Thursday:
                        rtnValue |= WTSThursday;
                        break;

                    case DayOfWeek.Friday:
                        rtnValue |= WTSFriday;
                        break;

                    case DayOfWeek.Saturday:
                        rtnValue |= WTSSaturday;
                        break;
                }
            }

            return rtnValue;
        }

        /// <summary>
        /// Converts WTS days of week mask to an array of DayOfWeek type.
        /// </summary>
        /// <param name="mask">WTS days of week mask.</param>
        /// <returns>Days of week as List.</returns>
        private List<DayOfWeek> ConvertMaskToDaysOfWeekArray(short mask)
        {
            List<DayOfWeek> daysOfWeek = new List<DayOfWeek>();

            if ((mask & WTSSunday) != 0)
            {
                daysOfWeek.Add(DayOfWeek.Sunday);
            }

            if ((mask & WTSMonday) != 0)
            {
                daysOfWeek.Add(DayOfWeek.Monday);
            }

            if ((mask & WTSTuesday) != 0)
            {
                daysOfWeek.Add(DayOfWeek.Tuesday);
            }

            if ((mask & WTSWednesday) != 0)
            {
                daysOfWeek.Add(DayOfWeek.Wednesday);
            }

            if ((mask & WTSThursday) != 0)
            {
                daysOfWeek.Add(DayOfWeek.Thursday);
            }

            if ((mask & WTSFriday) != 0)
            {
                daysOfWeek.Add(DayOfWeek.Friday);
            }

            if ((mask & WTSSaturday) != 0)
            {
                daysOfWeek.Add(DayOfWeek.Saturday);
            }

            return daysOfWeek;
        }

        private TaskMultipleInstancePolicy ConvertToMultiInstances(
            ITaskSettings iTaskSettings)
        {
            switch (iTaskSettings.MultipleInstances)
            {
                case _TASK_INSTANCES_POLICY.TASK_INSTANCES_IGNORE_NEW:
                    return TaskMultipleInstancePolicy.IgnoreNew;

                case _TASK_INSTANCES_POLICY.TASK_INSTANCES_PARALLEL:
                    return TaskMultipleInstancePolicy.Parallel;

                case _TASK_INSTANCES_POLICY.TASK_INSTANCES_QUEUE:
                    return TaskMultipleInstancePolicy.Queue;

                case _TASK_INSTANCES_POLICY.TASK_INSTANCES_STOP_EXISTING:
                    return TaskMultipleInstancePolicy.StopExisting;
            }

            Debug.Assert(false);
            return TaskMultipleInstancePolicy.None;
        }

        private _TASK_INSTANCES_POLICY ConvertFromMultiInstances(
            TaskMultipleInstancePolicy jobPolicies)
        {
            switch (jobPolicies)
            {
                case TaskMultipleInstancePolicy.IgnoreNew:
                    return _TASK_INSTANCES_POLICY.TASK_INSTANCES_IGNORE_NEW;

                case TaskMultipleInstancePolicy.Parallel:
                    return _TASK_INSTANCES_POLICY.TASK_INSTANCES_PARALLEL;

                case TaskMultipleInstancePolicy.Queue:
                    return _TASK_INSTANCES_POLICY.TASK_INSTANCES_QUEUE;

                case TaskMultipleInstancePolicy.StopExisting:
                    return _TASK_INSTANCES_POLICY.TASK_INSTANCES_STOP_EXISTING;

                default:
                    return _TASK_INSTANCES_POLICY.TASK_INSTANCES_IGNORE_NEW;
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            // Release reference to Task Scheduler object so that the COM
            // object can be released.
            _iRootFolder = null;
            _taskScheduler = null;

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
