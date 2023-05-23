// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Tracing;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text.RegularExpressions;

namespace Microsoft.PowerShell.ScheduledJob
{
    /// <summary>
    /// This class contains all information needed to define a PowerShell job that
    /// can be scheduled to run through either stand-alone or through the Windows
    /// Task Scheduler.
    /// </summary>
    public sealed class ScheduledJobDefinition : ISerializable, IDisposable
    {
        #region Private Members

        private JobInvocationInfo _invocationInfo;
        private ScheduledJobOptions _options;
        private PSCredential _credential;
        private Guid _globalId = Guid.NewGuid();
        private string _name = string.Empty;
        private int _id = GetCurrentId();
        private int _executionHistoryLength = DefaultExecutionHistoryLength;
        private bool _enabled = true;
        private Dictionary<Int32, ScheduledJobTrigger> _triggers = new Dictionary<Int32, ScheduledJobTrigger>();
        private Int32 _currentTriggerId;

        private string _definitionFilePath;
        private string _definitionOutputPath;

        private bool _isDisposed;

        // Task Action strings.
        private const string TaskExecutionPath = @"pwsh.exe";
        private const string TaskArguments = @"-NoLogo -NonInteractive -WindowStyle Hidden -Command ""Import-Module PSScheduledJob; $jobDef = [Microsoft.PowerShell.ScheduledJob.ScheduledJobDefinition]::LoadFromStore('{0}', '{1}'); $jobDef.Run()""";
        private static object LockObject = new object();
        private static int CurrentId = 0;
        private static int DefaultExecutionHistoryLength = 32;

        internal static ScheduledJobDefinitionRepository Repository = new ScheduledJobDefinitionRepository();

        // Task Scheduler COM error codes.
        private const int TSErrorDisabledTask = -2147216602;

        #endregion

        #region Public Properties

        /// <summary>
        /// Contains information needed to run the job such as script parameters,
        /// job definition, user credentials, etc.
        /// </summary>
        public JobInvocationInfo InvocationInfo
        {
            get { return _invocationInfo; }
        }

        /// <summary>
        /// Contains the script commands that define the job.
        /// </summary>
        public JobDefinition Definition
        {
            get { return _invocationInfo.Definition; }
        }

        /// <summary>
        /// Specifies Task Scheduler options for the scheduled job.
        /// </summary>
        public ScheduledJobOptions Options
        {
            get { return new ScheduledJobOptions(_options); }
        }

        /// <summary>
        /// Credential.
        /// </summary>
        public PSCredential Credential
        {
            get { return _credential; }

            internal set { _credential = value; }
        }

        /// <summary>
        /// An array of trigger objects that specify a time/condition
        /// for when the job is run.
        /// </summary>
        public List<ScheduledJobTrigger> JobTriggers
        {
            get
            {
                List<Int32> notFoundIds;
                return GetTriggers(null, out notFoundIds);
            }
        }

        /// <summary>
        /// Local instance Id for object instance.
        /// </summary>
        public int Id
        {
            get { return _id; }
        }

        /// <summary>
        /// Global Id for scheduled job definition.
        /// </summary>
        public Guid GlobalId
        {
            get { return _globalId; }
        }

        /// <summary>
        /// Name of scheduled job definition.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Job command.
        /// </summary>
        public string Command
        {
            get { return _invocationInfo.Command; }
        }

        /// <summary>
        /// Returns the maximum number of job execution data
        /// allowed in the job store.
        /// </summary>
        public int ExecutionHistoryLength
        {
            get { return _executionHistoryLength; }
        }

        /// <summary>
        /// Determines whether this scheduled job definition is enabled
        /// in Task Scheduler.
        /// </summary>
        public bool Enabled
        {
            get { return _enabled; }
        }

        /// <summary>
        /// Returns the PowerShell command line execution path.
        /// </summary>
        public string PSExecutionPath
        {
            get { return TaskExecutionPath; }
        }

        /// <summary>
        /// Returns PowerShell command line arguments to run
        /// the scheduled job.
        /// </summary>
        public string PSExecutionArgs
        {
            get
            {
                // Escape single quotes in name.  Double quotes are not allowed
                // and are caught during name validation.
                string nameEscapeQuotes = _invocationInfo.Name.Replace("'", "''");

                return string.Format(CultureInfo.InvariantCulture, TaskArguments, nameEscapeQuotes, _definitionFilePath);
            }
        }

        /// <summary>
        /// Returns the job run output path for this job definition.
        /// </summary>
        internal string OutputPath
        {
            get { return _definitionOutputPath; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor is not accessible.
        /// </summary>
        private ScheduledJobDefinition()
        { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="invocationInfo">Information to invoke Job.</param>
        /// <param name="triggers">ScheduledJobTriggers.</param>
        /// <param name="options">ScheduledJobOptions.</param>
        /// <param name="credential">Credential.</param>
        public ScheduledJobDefinition(
            JobInvocationInfo invocationInfo,
            IEnumerable<ScheduledJobTrigger> triggers,
            ScheduledJobOptions options,
            PSCredential credential)
        {
            if (invocationInfo == null)
            {
                throw new PSArgumentNullException("invocationInfo");
            }

            _name = invocationInfo.Name;
            _invocationInfo = invocationInfo;

            SetTriggers(triggers, false);
            _options = (options != null) ? new ScheduledJobOptions(options) :
                                           new ScheduledJobOptions();
            _options.JobDefinition = this;

            _credential = credential;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates existing information if scheduled job already exists.
        /// WTS entry includes command line, options, and trigger conditions.
        /// </summary>
        private void UpdateWTSFromDefinition()
        {
            using (ScheduledJobWTS taskScheduler = new ScheduledJobWTS())
            {
                taskScheduler.UpdateTask(this);
            }
        }

        /// <summary>
        /// Compares the current ScheduledJobDefinition task scheduler information
        /// with the corresponding information stored in Task Scheduler.  If the
        /// information is different then the task scheduler information in this
        /// object is updated to match what is in Task Scheduler, since that information
        /// takes precedence.
        ///
        /// Task Scheduler information:
        /// - Triggers
        /// - Options
        /// - Enabled state.
        /// </summary>
        /// <returns>Boolean if this object data is modified.</returns>
        private bool UpdateDefinitionFromWTS()
        {
            bool dataModified = false;

            // Get information from Task Scheduler.
            using (ScheduledJobWTS taskScheduler = new ScheduledJobWTS())
            {
                bool wtsEnabled = taskScheduler.GetTaskEnabled(_name);
                ScheduledJobOptions wtsOptions = taskScheduler.GetJobOptions(_name);
                Collection<ScheduledJobTrigger> wtsTriggers = taskScheduler.GetJobTriggers(_name);

                //
                // Compare with existing object data and modify if necessary.
                //

                // Enabled.
                if (wtsEnabled != _enabled)
                {
                    _enabled = wtsEnabled;
                    dataModified = true;
                }

                // Options.
                if (wtsOptions.DoNotAllowDemandStart != _options.DoNotAllowDemandStart ||
                    wtsOptions.IdleDuration != _options.IdleDuration ||
                    wtsOptions.IdleTimeout != _options.IdleTimeout ||
                    wtsOptions.MultipleInstancePolicy != _options.MultipleInstancePolicy ||
                    wtsOptions.RestartOnIdleResume != _options.RestartOnIdleResume ||
                    wtsOptions.RunElevated != _options.RunElevated ||
                    wtsOptions.RunWithoutNetwork != _options.RunWithoutNetwork ||
                    wtsOptions.ShowInTaskScheduler != _options.ShowInTaskScheduler ||
                    wtsOptions.StartIfNotIdle != _options.StartIfNotIdle ||
                    wtsOptions.StartIfOnBatteries != _options.StartIfOnBatteries ||
                    wtsOptions.StopIfGoingOffIdle != _options.StopIfGoingOffIdle ||
                    wtsOptions.StopIfGoingOnBatteries != _options.StopIfGoingOnBatteries ||
                    wtsOptions.WakeToRun != _options.WakeToRun)
                {
                    // Keep the current scheduled job definition reference.
                    wtsOptions.JobDefinition = _options.JobDefinition;
                    _options = wtsOptions;
                    dataModified = true;
                }

                // Triggers.
                if (_triggers.Count != wtsTriggers.Count)
                {
                    SetTriggers(wtsTriggers, false);
                    dataModified = true;
                }
                else
                {
                    bool foundTriggerDiff = false;

                    // Compare each trigger object.
                    foreach (var wtsTrigger in wtsTriggers)
                    {
                        if (_triggers.ContainsKey(wtsTrigger.Id) == false)
                        {
                            foundTriggerDiff = true;
                            break;
                        }

                        ScheduledJobTrigger trigger = _triggers[wtsTrigger.Id];
                        if (trigger.DaysOfWeek != wtsTrigger.DaysOfWeek ||
                            trigger.Enabled != wtsTrigger.Enabled ||
                            trigger.Frequency != wtsTrigger.Frequency ||
                            trigger.Interval != wtsTrigger.Interval ||
                            trigger.RandomDelay != wtsTrigger.RandomDelay ||
                            trigger.At != wtsTrigger.At ||
                            trigger.User != wtsTrigger.User)
                        {
                            foundTriggerDiff = true;
                            break;
                        }
                    }

                    if (foundTriggerDiff)
                    {
                        SetTriggers(wtsTriggers, false);
                        dataModified = true;
                    }
                }
            }

            return dataModified;
        }

        /// <summary>
        /// Adds this scheduled job definition to the Task Scheduler.
        /// </summary>
        private void AddToWTS()
        {
            using (ScheduledJobWTS taskScheduler = new ScheduledJobWTS())
            {
                taskScheduler.CreateTask(this);
            }
        }

        /// <summary>
        /// Removes this scheduled job definition from the Task Scheduler.
        /// This operation will fail if a current instance of this job definition
        /// is running.
        /// If force == true then all current instances will be stopped.
        /// </summary>
        /// <param name="force">Force removal and stop all running instances.</param>
        private void RemoveFromWTS(bool force)
        {
            using (ScheduledJobWTS taskScheduler = new ScheduledJobWTS())
            {
                taskScheduler.RemoveTask(this, force);
            }
        }

        /// <summary>
        /// Adds this scheduled job definition to the job definition store.
        /// </summary>
        private void AddToJobStore()
        {
            FileStream fs = null;
            try
            {
                fs = ScheduledJobStore.CreateFileForJobDefinition(Name);
                _definitionFilePath = ScheduledJobStore.GetJobDefinitionLocation();
                _definitionOutputPath = ScheduledJobStore.GetJobRunOutputDirectory(Name);

                XmlObjectSerializer serializer = new System.Runtime.Serialization.NetDataContractSerializer();
                serializer.WriteObject(fs, this);
                fs.Flush();
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
            }

            // If credentials are provided then update permissions.
            if (Credential != null)
            {
                UpdateFilePermissions(Credential.UserName);
            }
        }

        /// <summary>
        /// Updates existing file with this definition information.
        /// </summary>
        private void UpdateJobStore()
        {
            FileStream fs = null;
            try
            {
                // Overwrite the existing file.
                fs = GetFileStream(
                    Name,
                    _definitionFilePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None);

                XmlObjectSerializer serializer = new System.Runtime.Serialization.NetDataContractSerializer();
                serializer.WriteObject(fs, this);
                fs.Flush();
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
            }

            // If credentials are provided then update permissions.
            if (Credential != null)
            {
                UpdateFilePermissions(Credential.UserName);
            }
        }

        /// <summary>
        /// Updates definition file permissions for provided user account.
        /// </summary>
        /// <param name="user">Account user name.</param>
        private void UpdateFilePermissions(string user)
        {
            Exception ex = null;
            try
            {
                // Add user for read access to the job definition file.
                ScheduledJobStore.SetReadAccessOnDefinitionFile(Name, user);

                // Add user for write access to the job run Output directory.
                ScheduledJobStore.SetWriteAccessOnJobRunOutput(Name, user);
            }
            catch (System.Security.Principal.IdentityNotMappedException e)
            {
                ex = e;
            }
            catch (IOException e)
            {
                ex = e;
            }
            catch (UnauthorizedAccessException e)
            {
                ex = e;
            }
            catch (ArgumentNullException e)
            {
                ex = e;
            }

            if (ex != null)
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.ErrorSettingAccessPermissions, this.Name, Credential.UserName);
                throw new ScheduledJobException(msg, ex);
            }
        }

        /// <summary>
        /// Removes this scheduled job definition from the job definition store.
        /// </summary>
        private void RemoveFromJobStore()
        {
            ScheduledJobStore.RemoveJobDefinition(Name);
        }

        /// <summary>
        /// Throws exception if object is disposed.
        /// </summary>
        private void IsDisposed()
        {
            if (_isDisposed == true)
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.DefinitionObjectDisposed, Name);
                throw new RuntimeException(msg);
            }
        }

        /// <summary>
        /// If repository is empty try refreshing it from the store.
        /// </summary>
        private void LoadRepository()
        {
            ScheduledJobDefinition.RefreshRepositoryFromStore();
        }

        /// <summary>
        /// Validates all triggers in collection.  An exception is thrown
        /// for invalid triggers.
        /// </summary>
        /// <param name="triggers"></param>
        private void ValidateTriggers(IEnumerable<ScheduledJobTrigger> triggers)
        {
            if (triggers != null)
            {
                foreach (var trigger in triggers)
                {
                    trigger.Validate();
                }
            }
        }

        /// <summary>
        /// Validates the job definition name.  Since the job definition
        /// name is used in the job store as a directory name, make sure
        /// it does not contain any invalid characters.
        /// </summary>
        private static void ValidateName(string name)
        {
            if (name.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) != -1)
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidJobDefName, name);
                throw new ScheduledJobException(msg);
            }
        }

        /// <summary>
        /// Iterates through all job run files, opens each job
        /// run and renames it to the provided new name.
        /// </summary>
        /// <param name="newDefName">New job run name.</param>
        private void UpdateJobRunNames(
            string newDefName)
        {
            // Job run results will be under the new scheduled job definition name.
            Collection<DateTime> jobRuns = ScheduledJobSourceAdapter.GetJobRuns(newDefName);
            if (jobRuns == null)
            {
                return;
            }

            // Load and rename each job.
            ScheduledJobDefinition definition = ScheduledJobDefinition.LoadFromStore(newDefName, null);
            foreach (DateTime jobRun in jobRuns)
            {
                ScheduledJob job = null;
                try
                {
                    job = ScheduledJobSourceAdapter.LoadJobFromStore(definition.Name, jobRun) as ScheduledJob;
                }
                catch (ScheduledJobException)
                {
                    continue;
                }
                catch (DirectoryNotFoundException)
                {
                    continue;
                }
                catch (FileNotFoundException)
                {
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
                catch (IOException)
                {
                    continue;
                }

                if (job != null)
                {
                    job.Name = newDefName;
                    job.Definition = definition;
                    ScheduledJobSourceAdapter.SaveJobToStore(job);
                }
            }
        }

        /// <summary>
        /// Handles known Task Scheduler COM error codes.
        /// </summary>
        /// <param name="e">COMException.</param>
        /// <returns>Error message.</returns>
        private string ConvertCOMErrorCode(System.Runtime.InteropServices.COMException e)
        {
            string msg = null;
            switch (e.ErrorCode)
            {
                case TSErrorDisabledTask:
                    msg = ScheduledJobErrorStrings.ReasonTaskDisabled;
                    break;
            }

            return msg;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Save object to store.
        /// </summary>
        internal void SaveToStore()
        {
            IsDisposed();

            UpdateJobStore();
        }

        /// <summary>
        /// Compares the task scheduler information in this object with
        /// what is stored in Task Scheduler.  If there is a difference
        /// then this object is updated with the information from Task
        /// Scheduler and saved to the job store.
        /// </summary>
        internal void SyncWithWTS()
        {
            Exception notFoundEx = null;
            try
            {
                if (UpdateDefinitionFromWTS())
                {
                    SaveToStore();
                }
            }
            catch (DirectoryNotFoundException e)
            {
                notFoundEx = e;
            }
            catch (FileNotFoundException e)
            {
                notFoundEx = e;
            }

            if (notFoundEx != null)
            {
                // There is no corresponding Task Scheduler item for this
                // scheduled job definition.  Remove this definition from
                // the job store for consistency.
                Remove(true);
                throw notFoundEx;
            }
        }

        /// <summary>
        /// Renames scheduled job definition, store directory and task scheduler task.
        /// </summary>
        /// <param name="newName">New name of job definition.</param>
        internal void RenameAndSave(string newName)
        {
            if (InvocationInfo.Name.Equals(newName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            ValidateName(newName);

            // Attempt to rename job store directory.  Detect if new name
            // is not unique.
            string oldName = InvocationInfo.Name;
            Exception ex = null;
            try
            {
                ScheduledJobStore.RenameScheduledJobDefDir(oldName, newName);
            }
            catch (ArgumentException e)
            {
                ex = e;
            }
            catch (DirectoryNotFoundException e)
            {
                ex = e;
            }
            catch (FileNotFoundException e)
            {
                ex = e;
            }
            catch (UnauthorizedAccessException e)
            {
                ex = e;
            }
            catch (IOException e)
            {
                ex = e;
            }

            if (ex != null)
            {
                string msg;
                if (!string.IsNullOrEmpty(ex.Message))
                {
                    msg = StringUtil.Format(ScheduledJobErrorStrings.ErrorRenamingScheduledJobWithMessage, oldName, newName, ex.Message);
                }
                else
                {
                    msg = StringUtil.Format(ScheduledJobErrorStrings.ErrorRenamingScheduledJob, oldName, newName);
                }

                throw new ScheduledJobException(msg, ex);
            }

            try
            {
                // Remove old named Task Scheduler task.
                // This also stops any existing running job.
                RemoveFromWTS(true);

                // Update job definition names.
                _name = newName;
                InvocationInfo.Name = newName;
                InvocationInfo.Definition.Name = newName;
                _definitionOutputPath = ScheduledJobStore.GetJobRunOutputDirectory(Name);

                // Update job definition in new job store location.
                UpdateJobStore();

                // Add new Task Scheduler task with new name.
                // Jobs can start running again.
                AddToWTS();

                // Update any existing job run names.
                UpdateJobRunNames(newName);
            }
            catch (ArgumentException e)
            {
                ex = e;
            }
            catch (DirectoryNotFoundException e)
            {
                ex = e;
            }
            catch (FileNotFoundException e)
            {
                ex = e;
            }
            catch (UnauthorizedAccessException e)
            {
                ex = e;
            }
            catch (IOException e)
            {
                ex = e;
            }
            finally
            {
                // Clear job run cache since job runs now appear in new directory location.
                ScheduledJobSourceAdapter.ClearRepository();
            }

            // If any part of renaming the various scheduled job components fail,
            // aggressively remove scheduled job corrupted state and inform user.
            if (ex != null)
            {
                try
                {
                    Remove(true);
                }
                catch (ScheduledJobException e)
                {
                    ex.Data.Add("SchedJobRemoveError", e);
                }

                string msg;
                if (!string.IsNullOrEmpty(ex.Message))
                {
                    msg = StringUtil.Format(ScheduledJobErrorStrings.BrokenRenamingScheduledJobWithMessage, oldName, newName, ex.Message);
                }
                else
                {
                    msg = StringUtil.Format(ScheduledJobErrorStrings.BrokenRenamingScheduledJob, oldName, newName);
                }

                throw new ScheduledJobException(msg, ex);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers this scheduled job definition object by doing the
        /// following:
        ///  a) Writing this object to the scheduled job object store.
        ///  b) Registering this job as a Windows Task Scheduler task.
        ///  c) Adding this object to the local repository.
        /// </summary>
        public void Register()
        {
            IsDisposed();

            LoadRepository();

            ValidateName(Name);

            // First add to the job store.  If an exception occurs here
            // then this method fails with no clean up.
            Exception ex = null;
            bool corruptedFile = false;
            try
            {
                AddToJobStore();
            }
            catch (ArgumentException e)
            {
                ex = e;
            }
            catch (DirectoryNotFoundException e)
            {
                ex = e;
            }
            catch (FileNotFoundException e)
            {
                ex = e;
            }
            catch (UnauthorizedAccessException e)
            {
                ex = e;
            }
            catch (IOException e)
            {
                ex = e;
            }
            catch (System.Runtime.Serialization.SerializationException e)
            {
                corruptedFile = true;
                ex = e;
            }
            catch (System.Runtime.Serialization.InvalidDataContractException e)
            {
                corruptedFile = true;
                ex = e;
            }
            catch (ScheduledJobException e)
            {
                // Can be thrown for error setting file access permissions with supplied credentials.
                // But file is not considered corrupted if it already exists.
                corruptedFile = !(e.FQEID.Equals(ScheduledJobStore.ScheduledJobDefExistsFQEID, StringComparison.OrdinalIgnoreCase));
                ex = e;
            }

            if (ex != null)
            {
                if (corruptedFile)
                {
                    // Remove from store.
                    try
                    {
                        ScheduledJobStore.RemoveJobDefinition(Name);
                    }
                    catch (DirectoryNotFoundException)
                    { }
                    catch (FileNotFoundException)
                    { }
                    catch (UnauthorizedAccessException)
                    { }
                    catch (IOException)
                    { }
                }

                if (ex is not ScheduledJobException)
                {
                    // Wrap in ScheduledJobException type.
                    string msg = StringUtil.Format(ScheduledJobErrorStrings.ErrorRegisteringDefinitionStore, this.Name);
                    throw new ScheduledJobException(msg, ex);
                }
                else
                {
                    // Otherwise just re-throw.
                    throw ex;
                }
            }

            // Next register with the Task Scheduler.
            ex = null;
            try
            {
                AddToWTS();
            }
            catch (ArgumentException e)
            {
                ex = e;
            }
            catch (DirectoryNotFoundException e)
            {
                ex = e;
            }
            catch (FileNotFoundException e)
            {
                ex = e;
            }
            catch (UnauthorizedAccessException e)
            {
                ex = e;
            }
            catch (IOException e)
            {
                ex = e;
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                ex = e;
            }

            if (ex != null)
            {
                // Clean up job store.
                RemoveFromJobStore();

                string msg = StringUtil.Format(ScheduledJobErrorStrings.ErrorRegisteringDefinitionTask,
                    this.Name,
                    (string.IsNullOrEmpty(ex.Message) == false) ? ex.Message : string.Empty);
                throw new ScheduledJobException(msg, ex);
            }

            // Finally add to the local repository.
            Repository.AddOrReplace(this);
        }

        /// <summary>
        /// Saves this scheduled job definition object:
        ///  a) Rewrites this object to the scheduled job object store.
        ///  b) Updates the Windows Task Scheduler task.
        /// </summary>
        public void Save()
        {
            IsDisposed();

            LoadRepository();

            ValidateName(Name);

            // First update the Task Scheduler.  If an exception occurs here then
            // we fail with no clean up.
            Exception ex = null;
            try
            {
                UpdateWTSFromDefinition();
            }
            catch (ArgumentException e)
            {
                ex = e;
            }
            catch (DirectoryNotFoundException e)
            {
                ex = e;
            }
            catch (FileNotFoundException e)
            {
                ex = e;
            }
            catch (UnauthorizedAccessException e)
            {
                ex = e;
            }
            catch (IOException e)
            {
                ex = e;
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                ex = e;
            }

            if (ex != null)
            {
                // We want this object to remain synchronized with what is in WTS.
                SyncWithWTS();

                string msg = StringUtil.Format(ScheduledJobErrorStrings.ErrorUpdatingDefinitionTask, this.Name);
                throw new ScheduledJobException(msg, ex);
            }

            // Next save to job store.
            ex = null;
            try
            {
                UpdateJobStore();
            }
            catch (ArgumentException e)
            {
                ex = e;
            }
            catch (DirectoryNotFoundException e)
            {
                ex = e;
            }
            catch (FileNotFoundException e)
            {
                ex = e;
            }
            catch (UnauthorizedAccessException e)
            {
                ex = e;
            }
            catch (IOException e)
            {
                ex = e;
            }

            if (ex != null)
            {
                // Remove this from WTS for consistency.
                RemoveFromWTS(true);

                string msg = StringUtil.Format(ScheduledJobErrorStrings.ErrorUpdatingDefinitionStore, this.Name);
                throw new ScheduledJobException(msg, ex);
            }

            // Finally update this object in the local repository.
            ScheduledJobDefinition.RefreshRepositoryFromStore();
            Repository.AddOrReplace(this);
        }

        /// <summary>
        /// Removes this definition object:
        ///  a) Removes from the Task Scheduler
        ///      or fails if an instance is currently running.
        ///      or stops any running instances if force is true.
        ///  b) Removes from the scheduled job definition store.
        ///  c) Removes from the local repository.
        ///  d) Disposes this object.
        /// </summary>
        public void Remove(bool force)
        {
            IsDisposed();

            // First remove from Task Scheduler.  Catch not found
            // exceptions and continue.
            try
            {
                RemoveFromWTS(force);
            }
            catch (System.IO.DirectoryNotFoundException)
            {
                // Continue with removal.
            }
            catch (System.IO.FileNotFoundException)
            {
                // Continue with removal.
            }

            // Remove from the Job Store.  Catch exceptions and continue
            // with removal.
            Exception ex = null;
            try
            {
                RemoveFromJobStore();
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (FileNotFoundException)
            {
            }
            catch (ArgumentException e)
            {
                ex = e;
            }
            catch (UnauthorizedAccessException e)
            {
                ex = e;
            }
            catch (IOException e)
            {
                ex = e;
            }
            finally
            {
                // Remove from the local repository.
                Repository.Remove(this);

                // Remove job runs for this definition from local repository.
                ScheduledJobSourceAdapter.ClearRepositoryForDefinition(this.Name);

                // Dispose this object.
                Dispose();
            }

            if (ex != null)
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.ErrorRemovingDefinitionStore, this.Name);
                throw new ScheduledJobException(msg, ex);
            }
        }

        /// <summary>
        /// Starts the scheduled job immediately.  A ScheduledJob object is
        /// returned that represents the running command, and this returned
        /// job is also added to the local job repository.  Job results are
        /// not written to the job store.
        /// </summary>
        /// <returns>ScheduledJob object for running job.</returns>
        public ScheduledJob StartJob()
        {
            IsDisposed();

            ScheduledJob job = new ScheduledJob(_invocationInfo.Command, _invocationInfo.Name, this);
            job.StartJob();

            return job;
        }

        /// <summary>
        /// Starts registered job definition running from the Task Scheduler.
        /// </summary>
        public void RunAsTask()
        {
            IsDisposed();

            Exception ex = null;
            string reason = null;
            try
            {
                using (ScheduledJobWTS taskScheduler = new ScheduledJobWTS())
                {
                    taskScheduler.RunTask(this);
                }
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                reason = ScheduledJobErrorStrings.reasonJobNotFound;
                ex = e;
            }
            catch (System.IO.FileNotFoundException e)
            {
                reason = ScheduledJobErrorStrings.reasonJobNotFound;
                ex = e;
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                reason = ConvertCOMErrorCode(e);
                ex = e;
            }

            if (ex != null)
            {
                string msg;
                if (reason != null)
                {
                    msg = StringUtil.Format(ScheduledJobErrorStrings.ErrorRunningAsTaskWithReason, this.Name, reason);
                }
                else
                {
                    msg = StringUtil.Format(ScheduledJobErrorStrings.ErrorRunningAsTask, this.Name);
                }

                throw new ScheduledJobException(msg, ex);
            }
        }

        #endregion

        #region Public Trigger Methods

        /// <summary>
        /// Adds new ScheduledJobTriggers.
        /// </summary>
        /// <param name="triggers">Collection of ScheduledJobTrigger objects.</param>
        /// <param name="save">Update Windows Task Scheduler and save to store.</param>
        public void AddTriggers(
            IEnumerable<ScheduledJobTrigger> triggers,
            bool save)
        {
            IsDisposed();

            if (triggers == null)
            {
                throw new PSArgumentNullException("triggers");
            }

            // First validate all triggers.
            ValidateTriggers(triggers);

            Collection<int> newTriggerIds = new Collection<int>();
            foreach (ScheduledJobTrigger trigger in triggers)
            {
                ScheduledJobTrigger newTrigger = new ScheduledJobTrigger(trigger);

                newTrigger.Id = ++_currentTriggerId;
                newTriggerIds.Add(newTrigger.Id);
                newTrigger.JobDefinition = this;
                _triggers.Add(newTrigger.Id, newTrigger);
            }

            if (save)
            {
                Save();
            }
        }

        /// <summary>
        /// Removes triggers matching passed in trigger Ids.
        /// </summary>
        /// <param name="triggerIds">Trigger Ids to remove.</param>
        /// <param name="save">Update Windows Task Scheduler and save to store.</param>
        /// <returns>Trigger Ids not found.</returns>
        public List<Int32> RemoveTriggers(
            IEnumerable<Int32> triggerIds,
            bool save)
        {
            IsDisposed();

            List<Int32> idsNotFound = new List<Int32>();
            bool triggerFound = false;

            // triggerIds is null then remove all triggers.
            if (triggerIds == null)
            {
                _currentTriggerId = 0;
                if (_triggers.Count > 0)
                {
                    triggerFound = true;

                    foreach (ScheduledJobTrigger trigger in _triggers.Values)
                    {
                        trigger.Id = 0;
                        trigger.JobDefinition = null;
                    }

                    // Create new empty trigger collection.
                    _triggers = new Dictionary<int, ScheduledJobTrigger>();
                }
            }
            else
            {
                foreach (Int32 removeId in triggerIds)
                {
                    if (_triggers.ContainsKey(removeId))
                    {
                        _triggers[removeId].JobDefinition = null;
                        _triggers[removeId].Id = 0;
                        _triggers.Remove(removeId);
                        triggerFound = true;
                    }
                    else
                    {
                        idsNotFound.Add(removeId);
                    }
                }
            }

            if (save && triggerFound)
            {
                Save();
            }

            return idsNotFound;
        }

        /// <summary>
        /// Updates triggers with provided trigger objects, matching passed in
        /// trigger Id with existing trigger Id.
        /// </summary>
        /// <param name="triggers">Collection of ScheduledJobTrigger objects to update.</param>
        /// <param name="save">Update Windows Task Scheduler and save to store.</param>
        /// <returns>Trigger Ids not found.</returns>
        public List<Int32> UpdateTriggers(
            IEnumerable<ScheduledJobTrigger> triggers,
            bool save)
        {
            IsDisposed();

            if (triggers == null)
            {
                throw new PSArgumentNullException("triggers");
            }

            // First validate all triggers.
            ValidateTriggers(triggers);

            List<Int32> idsNotFound = new List<Int32>();
            bool triggerFound = false;
            foreach (ScheduledJobTrigger updateTrigger in triggers)
            {
                if (_triggers.ContainsKey(updateTrigger.Id))
                {
                    // Disassociate old trigger from this definition.
                    _triggers[updateTrigger.Id].JobDefinition = null;

                    // Replace older trigger object with new updated one.
                    ScheduledJobTrigger newTrigger = new ScheduledJobTrigger(updateTrigger);
                    newTrigger.Id = updateTrigger.Id;
                    newTrigger.JobDefinition = this;
                    _triggers[newTrigger.Id] = newTrigger;
                    triggerFound = true;
                }
                else
                {
                    idsNotFound.Add(updateTrigger.Id);
                }
            }

            if (save && triggerFound)
            {
                Save();
            }

            return idsNotFound;
        }

        /// <summary>
        /// Creates a new set of ScheduledJobTriggers for this object.
        /// </summary>
        /// <param name="newTriggers">Array of ScheduledJobTrigger objects to set.</param>
        /// <param name="save">Update Windows Task Scheduler and save to store.</param>
        public void SetTriggers(
            IEnumerable<ScheduledJobTrigger> newTriggers,
            bool save)
        {
            IsDisposed();

            // First validate all triggers.
            ValidateTriggers(newTriggers);

            // Disassociate any old trigger objects from this definition.
            foreach (ScheduledJobTrigger trigger in _triggers.Values)
            {
                trigger.JobDefinition = null;
            }

            _currentTriggerId = 0;
            _triggers = new Dictionary<Int32, ScheduledJobTrigger>();
            if (newTriggers != null)
            {
                foreach (ScheduledJobTrigger trigger in newTriggers)
                {
                    ScheduledJobTrigger newTrigger = new ScheduledJobTrigger(trigger);

                    newTrigger.Id = ++_currentTriggerId;
                    newTrigger.JobDefinition = this;
                    _triggers.Add(newTrigger.Id, newTrigger);
                }
            }

            if (save)
            {
                Save();
            }
        }

        /// <summary>
        /// Returns a list of new ScheduledJobTrigger objects corresponding
        /// to the passed in trigger Ids.  Also returns an array of trigger Ids
        /// that were not found in an out parameter.
        /// </summary>
        /// <param name="triggerIds">List of trigger Ids.</param>
        /// <param name="notFoundIds">List of not found trigger Ids.</param>
        /// <returns>List of ScheduledJobTrigger objects.</returns>
        public List<ScheduledJobTrigger> GetTriggers(
            IEnumerable<Int32> triggerIds,
            out List<Int32> notFoundIds)
        {
            IsDisposed();

            List<ScheduledJobTrigger> newTriggers;
            List<Int32> notFoundList = new List<Int32>();
            if (triggerIds == null)
            {
                // Return all triggers.
                newTriggers = new List<ScheduledJobTrigger>();
                foreach (ScheduledJobTrigger trigger in _triggers.Values)
                {
                    newTriggers.Add(new ScheduledJobTrigger(trigger));
                }
            }
            else
            {
                // Filter returned triggers to match requested.
                newTriggers = new List<ScheduledJobTrigger>();
                foreach (Int32 triggerId in triggerIds)
                {
                    if (_triggers.ContainsKey(triggerId))
                    {
                        newTriggers.Add(new ScheduledJobTrigger(_triggers[triggerId]));
                    }
                    else
                    {
                        notFoundList.Add(triggerId);
                    }
                }
            }

            notFoundIds = notFoundList;

            // Return array of ScheduledJobTrigger objects sorted by Id.
            newTriggers.Sort((firstTrigger, secondTrigger) =>
                {
                    return ((int)firstTrigger.Id - (int)secondTrigger.Id);
                });

            return newTriggers;
        }

        /// <summary>
        /// Finds and returns a copy of the ScheduledJobTrigger corresponding to
        /// the passed in trigger Id.
        /// </summary>
        /// <param name="triggerId">Trigger Id.</param>
        /// <returns>ScheduledJobTrigger object.</returns>
        public ScheduledJobTrigger GetTrigger(
            Int32 triggerId)
        {
            IsDisposed();

            if (_triggers.ContainsKey(triggerId))
            {
                return new ScheduledJobTrigger(_triggers[triggerId]);
            }

            return null;
        }

        #endregion

        #region Public Update Methods

        /// <summary>
        /// Updates scheduled job options.
        /// </summary>
        /// <param name="options">ScheduledJobOptions or null for default.</param>
        /// <param name="save">Update Windows Task Scheduler and save to store.</param>
        public void UpdateOptions(
            ScheduledJobOptions options,
            bool save)
        {
            IsDisposed();

            // Disassociate current options object from this definition.
            _options.JobDefinition = null;

            // options == null is allowed and signals the use default
            // Task Scheduler options.
            _options = (options != null) ? new ScheduledJobOptions(options) :
                                           new ScheduledJobOptions();
            _options.JobDefinition = this;

            if (save)
            {
                Save();
            }
        }

        /// <summary>
        /// Sets the execution history length property.
        /// </summary>
        /// <param name="executionHistoryLength">Execution history length.</param>
        /// <param name="save">Save to store.</param>
        public void SetExecutionHistoryLength(
            int executionHistoryLength,
            bool save)
        {
            IsDisposed();

            _executionHistoryLength = executionHistoryLength;

            if (save)
            {
                SaveToStore();
            }
        }

        /// <summary>
        /// Clears all execution results in the job store.
        /// </summary>
        public void ClearExecutionHistory()
        {
            IsDisposed();

            ScheduledJobStore.RemoveAllJobRuns(Name);
            ScheduledJobSourceAdapter.ClearRepositoryForDefinition(Name);
        }

        /// <summary>
        /// Updates the JobInvocationInfo object.
        /// </summary>
        /// <param name="jobInvocationInfo">JobInvocationInfo.</param>
        /// <param name="save">Save to store.</param>
        public void UpdateJobInvocationInfo(
            JobInvocationInfo jobInvocationInfo,
            bool save)
        {
            IsDisposed();

            if (jobInvocationInfo == null)
            {
                throw new PSArgumentNullException("jobInvocationInfo");
            }

            _invocationInfo = jobInvocationInfo;
            _name = jobInvocationInfo.Name;

            if (save)
            {
                SaveToStore();
            }
        }

        /// <summary>
        /// Sets the enabled state of this object.
        /// </summary>
        /// <param name="enabled">True if enabled.</param>
        /// <param name="save">Update Windows Task Scheduler and save to store.</param>
        public void SetEnabled(
            bool enabled,
            bool save)
        {
            IsDisposed();

            _enabled = enabled;

            if (save)
            {
                Save();
            }
        }

        /// <summary>
        /// Sets the name of this scheduled job definition.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="save">Update Windows Task Scheduler and save to store.</param>
        public void SetName(
            string name,
            bool save)
        {
            IsDisposed();

            _name = (name != null) ? name : string.Empty;

            if (save)
            {
                Save();
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            _isDisposed = true;

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Synchronizes the local ScheduledJobDefinition repository with the
        /// scheduled job definitions in the job store.
        /// </summary>
        /// <param name="itemFound">Callback delegate for each discovered item.</param>
        /// <returns>Dictionary of errors.</returns>
        internal static Dictionary<string, Exception> RefreshRepositoryFromStore(
            Action<ScheduledJobDefinition> itemFound = null)
        {
            Dictionary<string, Exception> errors = new Dictionary<string, Exception>();

            // Get current list of job definition files in store, and create hash
            // table for quick look up.
            IEnumerable<string> jobDefinitionPathNames = ScheduledJobStore.GetJobDefinitions();
            HashSet<string> jobDefinitionNamesHash = new HashSet<string>();
            foreach (string pathName in jobDefinitionPathNames)
            {
                // Remove path information and use job definition name only.
                int indx = pathName.LastIndexOf('\\');
                string jobDefName = (indx != -1) ? pathName.Substring(indx + 1) : pathName;
                jobDefinitionNamesHash.Add(jobDefName);
            }

            // First remove definition objects not in store.
            // Repository.Definitions returns a *copy* of current repository items.
            foreach (ScheduledJobDefinition jobDef in Repository.Definitions)
            {
                if (jobDefinitionNamesHash.Contains(jobDef.Name) == false)
                {
                    Repository.Remove(jobDef);
                }
                else
                {
                    jobDefinitionNamesHash.Remove(jobDef.Name);

                    if (itemFound != null)
                    {
                        itemFound(jobDef);
                    }
                }
            }

            // Next add definition items not in local repository.
            foreach (string jobDefinitionName in jobDefinitionNamesHash)
            {
                try
                {
                    // Read the job definition object from file and add to local repository.
                    ScheduledJobDefinition jobDefinition = ScheduledJobDefinition.LoadDefFromStore(jobDefinitionName, null);
                    Repository.AddOrReplace(jobDefinition);

                    if (itemFound != null)
                    {
                        itemFound(jobDefinition);
                    }
                }
                catch (System.IO.IOException e)
                {
                    errors.Add(jobDefinitionName, e);
                }
                catch (System.Xml.XmlException e)
                {
                    errors.Add(jobDefinitionName, e);
                }
                catch (System.TypeInitializationException e)
                {
                    errors.Add(jobDefinitionName, e);
                }
                catch (System.Runtime.Serialization.SerializationException e)
                {
                    errors.Add(jobDefinitionName, e);
                }
                catch (System.ArgumentNullException e)
                {
                    errors.Add(jobDefinitionName, e);
                }
                catch (System.UnauthorizedAccessException e)
                {
                    errors.Add(jobDefinitionName, e);
                }
            }

            return errors;
        }

        /// <summary>
        /// Reads a ScheduledJobDefinition object from file and
        /// returns object.
        /// </summary>
        /// <param name="definitionName">Name of definition to load.</param>
        /// <param name="definitionPath">Path to definition file.</param>
        /// <returns>ScheduledJobDefinition object.</returns>
        internal static ScheduledJobDefinition LoadDefFromStore(
            string definitionName,
            string definitionPath)
        {
            ScheduledJobDefinition definition = null;
            FileStream fs = null;
            try
            {
                fs = GetFileStream(
                    definitionName,
                    definitionPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

                XmlObjectSerializer serializer = new System.Runtime.Serialization.NetDataContractSerializer();
                definition = serializer.ReadObject(fs) as ScheduledJobDefinition;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
            }

            return definition;
        }

        /// <summary>
        /// Creates a new ScheduledJobDefinition object from a file.
        /// </summary>
        /// <param name="definitionName">Name of definition to load.</param>
        /// <param name="definitionPath">Path to definition file.</param>
        /// <returns>ScheduledJobDefinition object.</returns>
        public static ScheduledJobDefinition LoadFromStore(
            string definitionName,
            string definitionPath)
        {
            if (string.IsNullOrEmpty(definitionName))
            {
                throw new PSArgumentNullException("definitionName");
            }

            ScheduledJobDefinition definition = null;
            bool corruptedFile = false;
            Exception ex = null;

            try
            {
                definition = LoadDefFromStore(definitionName, definitionPath);
            }
            catch (DirectoryNotFoundException e)
            {
                ex = e;
            }
            catch (FileNotFoundException e)
            {
                ex = e;
                corruptedFile = true;
            }
            catch (UnauthorizedAccessException e)
            {
                ex = e;
            }
            catch (IOException e)
            {
                ex = e;
            }
            catch (System.Xml.XmlException e)
            {
                ex = e;
                corruptedFile = true;
            }
            catch (System.TypeInitializationException e)
            {
                ex = e;
                corruptedFile = true;
            }
            catch (System.ArgumentNullException e)
            {
                ex = e;
                corruptedFile = true;
            }
            catch (System.Runtime.Serialization.SerializationException e)
            {
                ex = e;
                corruptedFile = true;
            }

            if (ex != null)
            {
                //
                // Remove definition if corrupted.
                // But only if the corrupted file is in the default scheduled jobs
                // path for the current user.
                //
                if (corruptedFile &&
                    (definitionPath == null ||
                     ScheduledJobStore.IsDefaultUserPath(definitionPath)))
                {
                    // Remove corrupted scheduled job definition.
                    RemoveDefinition(definitionName);

                    // Throw exception for corrupted/removed job definition.
                    throw new ScheduledJobException(
                        StringUtil.Format(ScheduledJobErrorStrings.CantLoadDefinitionFromStore, definitionName),
                        ex);
                }

                // Throw exception for not found job definition.
                throw new ScheduledJobException(
                    StringUtil.Format(ScheduledJobErrorStrings.CannotFindJobDefinition, definitionName),
                    ex);
            }

            // Make sure the deserialized ScheduledJobDefinition object contains the same
            // Task Scheduler information that is stored in Task Scheduler.
            definition.SyncWithWTS();

            return definition;
        }

        /// <summary>
        /// Internal helper method to remove a scheduled job definition
        /// by name from job store and Task Scheduler.
        /// </summary>
        /// <param name="definitionName">Scheduled job definition name.</param>
        internal static void RemoveDefinition(
            string definitionName)
        {
            // Remove from store.
            try
            {
                ScheduledJobStore.RemoveJobDefinition(definitionName);
            }
            catch (DirectoryNotFoundException)
            { }
            catch (FileNotFoundException)
            { }
            catch (UnauthorizedAccessException)
            { }
            catch (IOException)
            { }

            // Check and remove from Task Scheduler.
            using (ScheduledJobWTS taskScheduler = new ScheduledJobWTS())
            {
                try
                {
                    taskScheduler.RemoveTaskByName(definitionName, true, true);
                }
                catch (UnauthorizedAccessException)
                { }
                catch (IOException)
                { }
            }
        }

        private static int GetCurrentId()
        {
            lock (LockObject)
            {
                return ++CurrentId;
            }
        }

        /// <summary>
        /// Starts a scheduled job based on definition name and returns the
        /// running job object.  Returned job is also added to the local
        /// job repository.  Job results are not written to store.
        /// </summary>
        /// <param name="DefinitionName">ScheduledJobDefinition name.</param>
        public static Job2 StartJob(
            string DefinitionName)
        {
            // Load scheduled job definition.
            ScheduledJobDefinition jobDefinition = ScheduledJobDefinition.LoadFromStore(DefinitionName, null);

            // Start job.
            return jobDefinition.StartJob();
        }

        private static FileStream GetFileStream(
            string definitionName,
            string definitionPath,
            FileMode fileMode,
            FileAccess fileAccess,
            FileShare fileShare)
        {
            FileStream fs;

            if (definitionPath == null)
            {
                // Look for definition in default current user location.
                fs = ScheduledJobStore.GetFileForJobDefinition(
                    definitionName,
                    fileMode,
                    fileAccess,
                    fileShare);
            }
            else
            {
                // Look for definition in known path.
                fs = ScheduledJobStore.GetFileForJobDefinition(
                    definitionName,
                    definitionPath,
                    fileMode,
                    fileAccess,
                    fileShare);
            }

            return fs;
        }

        #endregion

        #region Running Job

        /// <summary>
        /// Create a Job2 job, runs it and waits for it to complete.
        /// Job status and results are written to the job store.
        /// </summary>
        /// <returns>Job2 job object that was run.</returns>
        public Job2 Run()
        {
            Job2 job = null;

            using (PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource())
            {
                Exception ex = null;
                try
                {
                    JobManager jobManager = Runspace.DefaultRunspace.JobManager;

                    job = jobManager.NewJob(InvocationInfo);

                    // If this is a scheduled job type then include this object so
                    // so that ScheduledJobSourceAdapter knows where the results are
                    // to be stored.
                    ScheduledJob schedJob = job as ScheduledJob;
                    if (schedJob != null)
                    {
                        schedJob.Definition = this;
                        schedJob.AllowSetShouldExit = true;
                    }

                    // Update job store data when job begins.
                    job.StateChanged += (object sender, JobStateEventArgs e) =>
                        {
                            if (e.JobStateInfo.State == JobState.Running)
                            {
                                // Write job to store with this running state.
                                jobManager.PersistJob(job, Definition);
                            }
                        };

                    job.StartJob();

                    // Log scheduled job start.
                    _tracer.WriteScheduledJobStartEvent(
                        job.Name,
                        job.PSBeginTime.ToString());

                    // Wait for job to finish.
                    job.Finished.WaitOne();

                    // Ensure that the job run results are persisted to store.
                    jobManager.PersistJob(job, Definition);

                    // Perform a Receive-Job on the job object.  Output data will be dropped
                    // but we do this to execute any client method calls, in particular we
                    // want SetShouldExit to set the correct exit code on the process for
                    // use inside Task Scheduler.
                    using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create())
                    {
                        // Run on the default runspace.
                        ps.AddCommand("Receive-Job").AddParameter("Job", job).AddParameter("Keep", true);
                        ps.Invoke();
                    }

                    // Log scheduled job finish.
                    _tracer.WriteScheduledJobCompleteEvent(
                        job.Name,
                        job.PSEndTime.ToString(),
                        job.JobStateInfo.State.ToString());
                }
                catch (RuntimeException e)
                {
                    ex = e;
                }
                catch (InvalidOperationException e)
                {
                    ex = e;
                }
                catch (System.Security.SecurityException e)
                {
                    ex = e;
                }
                catch (IOException e)
                {
                    ex = e;
                }
                catch (UnauthorizedAccessException e)
                {
                    ex = e;
                }
                catch (ArgumentException e)
                {
                    ex = e;
                }
                catch (ScriptCallDepthException e)
                {
                    ex = e;
                }
                catch (System.Runtime.Serialization.SerializationException e)
                {
                    ex = e;
                }
                catch (System.Runtime.Serialization.InvalidDataContractException e)
                {
                    ex = e;
                }
                catch (System.Xml.XmlException e)
                {
                    ex = e;
                }
                catch (Microsoft.PowerShell.ScheduledJob.ScheduledJobException e)
                {
                    ex = e;
                }

                if (ex != null)
                {
                    // Log error.
                    _tracer.WriteScheduledJobErrorEvent(
                        this.Name,
                        ex.Message,
                        ex.StackTrace.ToString(),
                        (ex.InnerException != null) ? ex.InnerException.Message : string.Empty);

                    throw ex;
                }
            }

            return job;
        }

        #endregion
    }

    #region ScheduledJobDefinition Repository

    /// <summary>
    /// Collection of ScheduledJobDefinition objects.
    /// </summary>
    internal class ScheduledJobDefinitionRepository
    {
        #region Private Members

        private object _syncObject = new object();
        private Dictionary<string, ScheduledJobDefinition> _definitions = new Dictionary<string, ScheduledJobDefinition>();

        #endregion

        #region Public Properties

        /// <summary>
        /// Returns all definition objects in the repository as a List.
        /// </summary>
        public List<ScheduledJobDefinition> Definitions
        {
            get
            {
                lock (_syncObject)
                {
                    // Sort returned list by Ids.
                    List<ScheduledJobDefinition> rtnList =
                            new List<ScheduledJobDefinition>(_definitions.Values);

                    rtnList.Sort((firstJob, secondJob) =>
                        {
                            if (firstJob.Id > secondJob.Id)
                            {
                                return 1;
                            }
                            else if (firstJob.Id < secondJob.Id)
                            {
                                return -1;
                            }
                            else
                            {
                                return 0;
                            }
                        });

                    return rtnList;
                }
            }
        }

        /// <summary>
        /// Returns count of object in repository.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_syncObject)
                {
                    return _definitions.Count;
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Add ScheduledJobDefinition to repository.
        /// </summary>
        /// <param name="jobDef"></param>
        public void Add(ScheduledJobDefinition jobDef)
        {
            if (jobDef == null)
            {
                throw new PSArgumentNullException("jobDef");
            }

            lock (_syncObject)
            {
                if (_definitions.ContainsKey(jobDef.Name))
                {
                    string msg = StringUtil.Format(ScheduledJobErrorStrings.DefinitionAlreadyExistsInLocal, jobDef.Name, jobDef.GlobalId);
                    throw new ScheduledJobException(msg);
                }

                _definitions.Add(jobDef.Name, jobDef);
            }
        }

        /// <summary>
        /// Add or replace passed in ScheduledJobDefinition object to repository.
        /// </summary>
        /// <param name="jobDef"></param>
        public void AddOrReplace(ScheduledJobDefinition jobDef)
        {
            if (jobDef == null)
            {
                throw new PSArgumentNullException("jobDef");
            }

            lock (_syncObject)
            {
                if (_definitions.ContainsKey(jobDef.Name))
                {
                    _definitions.Remove(jobDef.Name);
                }

                _definitions.Add(jobDef.Name, jobDef);
            }
        }

        /// <summary>
        /// Remove ScheduledJobDefinition from repository.
        /// </summary>
        /// <param name="jobDef"></param>
        public void Remove(ScheduledJobDefinition jobDef)
        {
            if (jobDef == null)
            {
                throw new PSArgumentNullException("jobDef");
            }

            lock (_syncObject)
            {
                if (_definitions.ContainsKey(jobDef.Name))
                {
                    _definitions.Remove(jobDef.Name);
                }
            }
        }

        /// <summary>
        /// Checks to see if a ScheduledJobDefinition object exists with
        /// the provided definition name.
        /// </summary>
        /// <param name="jobDefName">Definition name.</param>
        /// <returns>True if definition exists.</returns>
        public bool Contains(string jobDefName)
        {
            lock (_syncObject)
            {
                return _definitions.ContainsKey(jobDefName);
            }
        }

        /// <summary>
        /// Clears all ScheduledJobDefinition items from the repository.
        /// </summary>
        public void Clear()
        {
            lock (_syncObject)
            {
                _definitions.Clear();
            }
        }

        #endregion
    }

    #endregion

    #region Exceptions

    /// <summary>
    /// Exception thrown for errors in Scheduled Jobs.
    /// </summary>
    [Serializable]
    public class ScheduledJobException : SystemException
    {
        /// <summary>
        /// Creates a new instance of ScheduledJobException class.
        /// </summary>
        public ScheduledJobException()
            : base
            (
                StringUtil.Format(ScheduledJobErrorStrings.GeneralWTSError)
            )
        {
        }

        /// <summary>
        /// Creates a new instance of ScheduledJobException class.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        public ScheduledJobException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance of ScheduledJobException class.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception.
        /// </param>
        public ScheduledJobException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Fully qualified error id for exception.
        /// </summary>
        internal string FQEID
        {
            get { return _fqeid; }

            set { _fqeid = value ?? string.Empty; }
        }

        private string _fqeid = string.Empty;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Simple string formatting helper.
    /// </summary>
    internal class StringUtil
    {
        internal static string Format(string formatSpec, object o)
        {
            return string.Format(System.Threading.Thread.CurrentThread.CurrentCulture, formatSpec, o);
        }

        internal static string Format(string formatSpec, params object[] o)
        {
            return string.Format(System.Threading.Thread.CurrentThread.CurrentCulture, formatSpec, o);
        }
    }

    #endregion

    #region ScheduledJobInvocationInfo Class

    /// <summary>
    /// This class defines the JobInvocationInfo class for PowerShell jobs
    /// for job scheduling.  The following parameters are supported:
    ///
    ///  "ScriptBlock"             -> ScriptBlock
    ///  "FilePath"                -> String
    ///  "InitializationScript"    -> ScriptBlock
    ///  "ArgumentList"            -> object[]
    ///  "RunAs32"                 -> Boolean
    ///  "Authentication"          -> AuthenticationMechanism.
    /// </summary>
    [Serializable]
    public sealed class ScheduledJobInvocationInfo : JobInvocationInfo
    {
        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="definition">JobDefinition.</param>
        /// <param name="parameters">Dictionary of parameters.</param>
        public ScheduledJobInvocationInfo(JobDefinition definition, Dictionary<string, object> parameters)
            : base(definition, parameters)
        {
            if (definition == null)
            {
                throw new PSArgumentNullException("definition");
            }

            Name = definition.Name;
        }

        #endregion

        #region Public Strings

        /// <summary>
        /// ScriptBlock parameter.
        /// </summary>
        public const string ScriptBlockParameter = "ScriptBlock";

        /// <summary>
        /// FilePath parameter.
        /// </summary>
        public const string FilePathParameter = "FilePath";

        /// <summary>
        /// RunAs32 parameter.
        /// </summary>
        public const string RunAs32Parameter = "RunAs32";

        /// <summary>
        /// Authentication parameter.
        /// </summary>
        public const string AuthenticationParameter = "Authentication";

        /// <summary>
        /// InitializationScript parameter.
        /// </summary>
        public const string InitializationScriptParameter = "InitializationScript";

        /// <summary>
        /// ArgumentList parameter.
        /// </summary>
        public const string ArgumentListParameter = "ArgumentList";

        #endregion

        #region Private Methods

        private void SerializeInvocationInfo(SerializationInfo info)
        {
            info.AddValue("InvocationInfo_Command", this.Command);
            info.AddValue("InvocationInfo_Name", this.Name);
            info.AddValue("InvocationInfo_AdapterType", this.Definition.JobSourceAdapterType);
            info.AddValue("InvocationInfo_ModuleName", this.Definition.ModuleName);
            info.AddValue("InvocationInfo_AdapterTypeName", this.Definition.JobSourceAdapterTypeName);

            // Get the job parameters.
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            foreach (var commandParam in this.Parameters[0])
            {
                if (!parameters.ContainsKey(commandParam.Name))
                {
                    parameters.Add(commandParam.Name, commandParam.Value);
                }
            }

            //
            // Serialize only parameters that scheduled job knows about.
            //

            // ScriptBlock
            if (parameters.ContainsKey(ScriptBlockParameter))
            {
                ScriptBlock scriptBlock = (ScriptBlock)parameters[ScriptBlockParameter];
                info.AddValue("InvocationParam_ScriptBlock", scriptBlock.ToString());
            }
            else
            {
                info.AddValue("InvocationParam_ScriptBlock", null);
            }

            // FilePath
            if (parameters.ContainsKey(FilePathParameter))
            {
                string filePath = (string)parameters[FilePathParameter];
                info.AddValue("InvocationParam_FilePath", filePath);
            }
            else
            {
                info.AddValue("InvocationParam_FilePath", string.Empty);
            }

            // InitializationScript
            if (parameters.ContainsKey(InitializationScriptParameter))
            {
                ScriptBlock scriptBlock = (ScriptBlock)parameters[InitializationScriptParameter];
                info.AddValue("InvocationParam_InitScript", scriptBlock.ToString());
            }
            else
            {
                info.AddValue("InvocationParam_InitScript", string.Empty);
            }

            // RunAs32
            if (parameters.ContainsKey(RunAs32Parameter))
            {
                bool runAs32 = (bool)parameters[RunAs32Parameter];
                info.AddValue("InvocationParam_RunAs32", runAs32);
            }
            else
            {
                info.AddValue("InvocationParam_RunAs32", false);
            }

            // Authentication
            if (parameters.ContainsKey(AuthenticationParameter))
            {
                AuthenticationMechanism authentication = (AuthenticationMechanism)parameters[AuthenticationParameter];
                info.AddValue("InvocationParam_Authentication", authentication);
            }
            else
            {
                info.AddValue("InvocationParam_Authentication", AuthenticationMechanism.Default);
            }

            // ArgumentList
            if (parameters.ContainsKey(ArgumentListParameter))
            {
                object[] argList = (object[])parameters[ArgumentListParameter];
                info.AddValue("InvocationParam_ArgList", argList);
            }
            else
            {
                info.AddValue("InvocationParam_ArgList", null);
            }
        }

        private void DeserializeInvocationInfo(SerializationInfo info)
        {
            string command = info.GetString("InvocationInfo_Command");
            string name = info.GetString("InvocationInfo_Name");
            string moduleName = info.GetString("InvocationInfo_ModuleName");
            string adapterTypeName = info.GetString("InvocationInfo_AdapterTypeName");

            //
            // Parameters
            Dictionary<string, object> parameters = new Dictionary<string, object>();

            // ScriptBlock
            string script = info.GetString("InvocationParam_ScriptBlock");
            if (script != null)
            {
                parameters.Add(ScriptBlockParameter, ScriptBlock.Create(script));
            }

            // FilePath
            string filePath = info.GetString("InvocationParam_FilePath");
            if (!string.IsNullOrEmpty(filePath))
            {
                parameters.Add(FilePathParameter, filePath);
            }

            // InitializationScript
            script = info.GetString("InvocationParam_InitScript");
            if (!string.IsNullOrEmpty(script))
            {
                parameters.Add(InitializationScriptParameter, ScriptBlock.Create(script));
            }

            // RunAs32
            bool runAs32 = info.GetBoolean("InvocationParam_RunAs32");
            parameters.Add(RunAs32Parameter, runAs32);

            // Authentication
            AuthenticationMechanism authentication = (AuthenticationMechanism)info.GetValue("InvocationParam_Authentication",
                typeof(AuthenticationMechanism));
            parameters.Add(AuthenticationParameter, authentication);

            // ArgumentList
            object[] argList = (object[])info.GetValue("InvocationParam_ArgList", typeof(object[]));
            if (argList != null)
            {
                parameters.Add(ArgumentListParameter, argList);
            }

            JobDefinition jobDefinition = new JobDefinition(null, command, name);
            jobDefinition.ModuleName = moduleName;
            jobDefinition.JobSourceAdapterTypeName = adapterTypeName;

            // Convert to JobInvocationParameter collection
            CommandParameterCollection paramCollection = new CommandParameterCollection();
            foreach (KeyValuePair<string, object> param in parameters)
            {
                CommandParameter paramItem = new CommandParameter(param.Key, param.Value);
                paramCollection.Add(paramItem);
            }

            this.Definition = jobDefinition;
            this.Name = name;
            this.Command = command;
            this.Parameters.Add(paramCollection);
        }

        #endregion
    }

    #endregion
}
