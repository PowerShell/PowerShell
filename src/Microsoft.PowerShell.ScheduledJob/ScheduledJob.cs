// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;

namespace Microsoft.PowerShell.ScheduledJob
{
    /// <summary>
    /// This is a Job2 derived class that contains a DefinitionJob for
    /// running job definition based jobs but can also save and load job
    /// results data from file.  This class is used to load job result
    /// data from previously run jobs so that a user can view results of
    /// scheduled job runs.  This class also contains the definition of
    /// the scheduled job and so can run an instance of the scheduled
    /// job and optionally save results to file.
    /// </summary>
    [Serializable]
    public sealed class ScheduledJob : Job2, ISerializable
    {
        #region Private Members

        private ScheduledJobDefinition _jobDefinition;
        private Runspace _runspace;
        private System.Management.Automation.PowerShell _powerShell;
        private Job _job = null;
        private bool _asyncJobStop;
        private bool _allowSetShouldExit;
        private PSHost _host;

        private const string AllowHostSetShouldExit = "AllowSetShouldExitFromRemote";

        private StatusInfo _statusInfo;

        #endregion

        #region Public Properties

        /// <summary>
        /// ScheduledJobDefinition.
        /// </summary>
        public ScheduledJobDefinition Definition
        {
            get { return _jobDefinition; }

            internal set { _jobDefinition = value; }
        }

        /// <summary>
        /// Location of job being run.
        /// </summary>
        public override string Location
        {
            get
            {
                return Status.Location;
            }
        }

        /// <summary>
        /// Status Message associated with the Job.
        /// </summary>
        public override string StatusMessage
        {
            get
            {
                return Status.StatusMessage;
            }
        }

        /// <summary>
        /// Indicates whether more data is available from Job.
        /// </summary>
        public override bool HasMoreData
        {
            get
            {
                return (_job != null) ?
                    _job.HasMoreData
                    :
                    (Output.Count > 0 ||
                     Error.Count > 0 ||
                     Warning.Count > 0 ||
                     Verbose.Count > 0 ||
                     Progress.Count > 0 ||
                     Debug.Count > 0 ||
                     Information.Count > 0
                     );
            }
        }

        /// <summary>
        /// Job command string.
        /// </summary>
        public new string Command
        {
            get
            {
                return Status.Command;
            }
        }

        /// <summary>
        /// Internal property indicating whether a SetShouldExit is honored
        /// while running the scheduled job script.
        /// </summary>
        internal bool AllowSetShouldExit
        {
            get { return _allowSetShouldExit; }

            set { _allowSetShouldExit = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="command">Job command string for display.</param>
        /// <param name="name">Name of job.</param>
        /// <param name="jobDefinition">ScheduledJobDefinition defining job to run.</param>
        public ScheduledJob(
            string command,
            string name,
            ScheduledJobDefinition jobDefinition) :
            base(command, name)
        {
            if (command == null)
            {
                throw new PSArgumentNullException("command");
            }

            if (name == null)
            {
                throw new PSArgumentNullException("name");
            }

            if (jobDefinition == null)
            {
                throw new PSArgumentNullException("jobDefinition");
            }

            _jobDefinition = jobDefinition;

            PSJobTypeName = ScheduledJobSourceAdapter.AdapterTypeName;
        }

        #endregion

        #region Public Overrides

        /// <summary>
        /// Starts a job as defined by the contained ScheduledJobDefinition object.
        /// </summary>
        public override void StartJob()
        {
            lock (SyncRoot)
            {
                if (_job != null && !IsFinishedState(_job.JobStateInfo.State))
                {
                    string msg = StringUtil.Format(ScheduledJobErrorStrings.JobAlreadyRunning, _jobDefinition.Name);
                    throw new PSInvalidOperationException(msg);
                }

                _statusInfo = null;
                _asyncJobStop = false;
                PSBeginTime = DateTime.Now;

                if (_powerShell == null)
                {
                    InitialSessionState iss = InitialSessionState.CreateDefault2();
                    iss.Commands.Clear();
                    iss.Formats.Clear();
                    iss.Commands.Add(
                        new SessionStateCmdletEntry("Start-Job", typeof(Microsoft.PowerShell.Commands.StartJobCommand), null));

                    // Get the default host from the default runspace.
                    _host = GetDefaultHost();
                    _runspace = RunspaceFactory.CreateRunspace(_host, iss);
                    _runspace.Open();
                    _powerShell = System.Management.Automation.PowerShell.Create();
                    _powerShell.Runspace = _runspace;

                    // Indicate SetShouldExit to host.
                    AddSetShouldExitToHost();
                }
                else
                {
                    _powerShell.Commands.Clear();
                }

                _job = StartJobCommand(_powerShell);

                _job.StateChanged += new EventHandler<JobStateEventArgs>(HandleJobStateChanged);
                SetJobState(_job.JobStateInfo.State);

                // Add all child jobs to this object's list so that
                // the user and Receive-Job can retrieve results.
                foreach (Job childJob in _job.ChildJobs)
                {
                    this.ChildJobs.Add(childJob);
                }

                // Add this job to the local repository.
                ScheduledJobSourceAdapter.AddToRepository(this);
            }
        }

        /// <summary>
        /// Start job asynchronously.
        /// </summary>
        public override void StartJobAsync()
        {
            // StartJob();
            throw new PSNotSupportedException();
        }

        /// <summary>
        /// Stop the job.
        /// </summary>
        public override void StopJob()
        {
            Job job;
            JobState state;
            lock (SyncRoot)
            {
                job = _job;
                state = Status.State;
                _asyncJobStop = false;
            }

            if (IsFinishedState(state))
            {
                return;
            }

            if (job == null)
            {
                // Set job state to failed so that it can be removed from the
                // cache using Remove-Job.
                SetJobState(JobState.Failed);
            }
            else
            {
                job.StopJob();
            }
        }

        /// <summary>
        /// Stop the job asynchronously.
        /// </summary>
        public override void StopJobAsync()
        {
            Job job;
            JobState state;
            lock (SyncRoot)
            {
                job = _job;
                state = Status.State;
                _asyncJobStop = true;
            }

            if (IsFinishedState(state))
            {
                return;
            }

            if (job == null)
            {
                // Set job state to failed so that it can be removed from the
                // cache using Remove-Job.
                SetJobState(JobState.Failed);
                HandleJobStateChanged(this,
                    new JobStateEventArgs(
                        new JobStateInfo(JobState.Failed)));
            }
            else
            {
                job.StopJob();
            }
        }

        /// <summary>
        /// SuspendJob.
        /// </summary>
        public override void SuspendJob()
        {
            throw new PSNotSupportedException();
        }

        /// <summary>
        /// SuspendJobAsync.
        /// </summary>
        public override void SuspendJobAsync()
        {
            throw new PSNotSupportedException();
        }

        /// <summary>
        /// ResumeJob.
        /// </summary>
        public override void ResumeJob()
        {
            throw new PSNotSupportedException();
        }

        /// <summary>
        /// ResumeJobAsync.
        /// </summary>
        public override void ResumeJobAsync()
        {
            throw new PSNotSupportedException();
        }

        /// <summary>
        /// UnblockJob.
        /// </summary>
        public override void UnblockJob()
        {
            throw new PSNotSupportedException();
        }

        /// <summary>
        /// UnblockJobAsync.
        /// </summary>
        public override void UnblockJobAsync()
        {
            throw new PSNotSupportedException();
        }

        /// <summary>
        /// StopJob.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        public override void StopJob(bool force, string reason)
        {
            throw new PSNotSupportedException();
        }

        /// <summary>
        /// StopJobAsync.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        public override void StopJobAsync(bool force, string reason)
        {
            throw new PSNotSupportedException();
        }
        /// <summary>
        /// SuspendJob.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        public override void SuspendJob(bool force, string reason)
        {
            throw new PSNotSupportedException();
        }

        /// <summary>
        /// SuspendJobAsync.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        public override void SuspendJobAsync(bool force, string reason)
        {
            throw new PSNotSupportedException();
        }

        #endregion

        #region Implementation of ISerializable

        /// <summary>
        /// Deserialize constructor.
        /// </summary>
        /// <param name="info">SerializationInfo.</param>
        /// <param name="context">StreamingContext.</param>
        private ScheduledJob(
            SerializationInfo info,
            StreamingContext context)
        {
            if (info == null)
            {
                throw new PSArgumentNullException("info");
            }

            DeserializeStatusInfo(info);
            DeserializeResultsInfo(info);
            PSJobTypeName = ScheduledJobSourceAdapter.AdapterTypeName;
        }

        /// <summary>
        /// Serialize method.
        /// </summary>
        /// <param name="info">SerializationInfo.</param>
        /// <param name="context">StreamingContext.</param>
        public void GetObjectData(
            SerializationInfo info,
            StreamingContext context)
        {
            if (info == null)
            {
                throw new PSArgumentException("info");
            }

            SerializeStatusInfo(info);
            SerializeResultsInfo(info);
        }

        private void SerializeStatusInfo(SerializationInfo info)
        {
            StatusInfo statusInfo = new StatusInfo(
                InstanceId,
                Name,
                Location,
                Command,
                StatusMessage,
                (_job != null) ? _job.JobStateInfo.State : JobStateInfo.State,
                HasMoreData,
                PSBeginTime,
                PSEndTime,
                _jobDefinition);

            info.AddValue("StatusInfo", statusInfo);
        }

        private void SerializeResultsInfo(SerializationInfo info)
        {
            // All other job information is in the child jobs.
            Collection<PSObject> output = new Collection<PSObject>();
            Collection<ErrorRecord> error = new Collection<ErrorRecord>();
            Collection<WarningRecord> warning = new Collection<WarningRecord>();
            Collection<VerboseRecord> verbose = new Collection<VerboseRecord>();
            Collection<ProgressRecord> progress = new Collection<ProgressRecord>();
            Collection<DebugRecord> debug = new Collection<DebugRecord>();
            Collection<InformationRecord> information = new Collection<InformationRecord>();

            if (_job != null)
            {
                // Collect data from "live" job.

                if (JobStateInfo.Reason != null)
                {
                    error.Add(new ErrorRecord(JobStateInfo.Reason, "ScheduledJobFailedState", ErrorCategory.InvalidResult, null));
                }

                foreach (var item in _job.Error)
                {
                    error.Add(item);
                }

                foreach (Job childJob in ChildJobs)
                {
                    if (childJob.JobStateInfo.Reason != null)
                    {
                        error.Add(new ErrorRecord(childJob.JobStateInfo.Reason, "ScheduledJobFailedState", ErrorCategory.InvalidResult, null));
                    }

                    foreach (var item in childJob.Output)
                    {
                        output.Add(item);
                    }

                    foreach (var item in childJob.Error)
                    {
                        error.Add(item);
                    }

                    foreach (var item in childJob.Warning)
                    {
                        warning.Add(item);
                    }

                    foreach (var item in childJob.Verbose)
                    {
                        verbose.Add(item);
                    }

                    foreach (var item in childJob.Progress)
                    {
                        progress.Add(item);
                    }

                    foreach (var item in childJob.Debug)
                    {
                        debug.Add(item);
                    }

                    foreach (var item in childJob.Information)
                    {
                        information.Add(item);
                    }
                }
            }
            else
            {
                // Collect data from object collections.

                foreach (var item in Output)
                {
                    // Wrap the base object in a new PSObject.  This is necessary because the
                    // source deserialized PSObject doesn't serialize again correctly and breaks
                    // PS F&O.  Not sure if this is a PSObject serialization bug or not.
                    output.Add(new PSObject(item.BaseObject));
                }

                foreach (var item in Error)
                {
                    error.Add(item);
                }

                foreach (var item in Warning)
                {
                    warning.Add(item);
                }

                foreach (var item in Verbose)
                {
                    verbose.Add(item);
                }

                foreach (var item in Progress)
                {
                    progress.Add(item);
                }

                foreach (var item in Debug)
                {
                    debug.Add(item);
                }

                foreach (var item in Information)
                {
                    information.Add(item);
                }
            }

            ResultsInfo resultsInfo = new ResultsInfo(
                output, error, warning, verbose, progress, debug, information);

           info.AddValue("ResultsInfo", resultsInfo);
        }

        private void DeserializeStatusInfo(SerializationInfo info)
        {
            StatusInfo statusInfo = (StatusInfo)info.GetValue("StatusInfo", typeof(StatusInfo));

            Name = statusInfo.Name;
            PSBeginTime = statusInfo.StartTime;
            PSEndTime = statusInfo.StopTime;
            _jobDefinition = statusInfo.Definition;
            SetJobState(statusInfo.State, null);

            lock (SyncRoot)
            {
                _statusInfo = statusInfo;
            }
        }

        private void DeserializeResultsInfo(SerializationInfo info)
        {
            ResultsInfo resultsInfo = (ResultsInfo)info.GetValue("ResultsInfo", typeof(ResultsInfo));

            // Output
            CopyOutput(resultsInfo.Output);

            // Error
            CopyError(resultsInfo.Error);

            // Warning
            CopyWarning(resultsInfo.Warning);

            // Verbose
            CopyVerbose(resultsInfo.Verbose);

            // Progress
            CopyProgress(resultsInfo.Progress);

            // Debug
            CopyDebug(resultsInfo.Debug);

            // Information
            CopyInformation(resultsInfo.Information);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Method to update a ScheduledJob based on new state and
        /// result data from a provided Job.
        /// </summary>
        /// <param name="fromJob">ScheduledJob to update from.</param>
        internal void Update(ScheduledJob fromJob)
        {
            // We do not update "live" jobs.
            if (_job != null || fromJob == null)
            {
                return;
            }

            //
            // Update status.
            //
            PSEndTime = fromJob.PSEndTime;
            JobState state = fromJob.JobStateInfo.State;
            if (Status.State != state)
            {
                SetJobState(state, null);
            }

            lock (SyncRoot)
            {
                _statusInfo = new StatusInfo(
                    fromJob.InstanceId,
                    fromJob.Name,
                    fromJob.Location,
                    fromJob.Command,
                    fromJob.StatusMessage,
                    state,
                    fromJob.HasMoreData,
                    fromJob.PSBeginTime,
                    fromJob.PSEndTime,
                    fromJob._jobDefinition);
            }

            //
            // Update results.
            //
            CopyOutput(fromJob.Output);
            CopyError(fromJob.Error);
            CopyWarning(fromJob.Warning);
            CopyVerbose(fromJob.Verbose);
            CopyProgress(fromJob.Progress);
            CopyDebug(fromJob.Debug);
            CopyInformation(fromJob.Information);
        }

        #endregion

        #region Private Methods

        private System.Management.Automation.Host.PSHost GetDefaultHost()
        {
            System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace).AddScript("$host");
            Collection<System.Management.Automation.Host.PSHost> hosts = ps.Invoke<System.Management.Automation.Host.PSHost>();
            if (hosts == null || hosts.Count == 0)
            {
                System.Diagnostics.Debug.Assert(false, "Current runspace should always return default host.");
                return null;
            }

            return hosts[0];
        }

        private Job StartJobCommand(System.Management.Automation.PowerShell powerShell)
        {
            Job job = null;

            // Use PowerShell Start-Job cmdlet to run job.
            powerShell.AddCommand("Start-Job");

            powerShell.AddParameter("Name", _jobDefinition.Name);

            // Add job parameters from the JobInvocationInfo object.
            CommandParameterCollection parameters = _jobDefinition.InvocationInfo.Parameters[0];
            foreach (CommandParameter parameter in parameters)
            {
                switch (parameter.Name)
                {
                    case "ScriptBlock":
                        powerShell.AddParameter("ScriptBlock", parameter.Value as ScriptBlock);
                        break;

                    case "FilePath":
                        powerShell.AddParameter("FilePath", parameter.Value as string);
                        break;

                    case "RunAs32":
                        powerShell.AddParameter("RunAs32", (bool)parameter.Value);
                        break;

                    case "Authentication":
                        powerShell.AddParameter("Authentication", (AuthenticationMechanism)parameter.Value);
                        break;

                    case "InitializationScript":
                        powerShell.AddParameter("InitializationScript", parameter.Value as ScriptBlock);
                        break;

                    case "ArgumentList":
                        powerShell.AddParameter("ArgumentList", parameter.Value as object[]);
                        break;
                }
            }

            // Start the job.
            Collection<PSObject> rtn = powerShell.Invoke();
            if (rtn != null && rtn.Count == 1)
            {
                job = rtn[0].BaseObject as Job;
            }

            return job;
        }

        private void HandleJobStateChanged(object sender, JobStateEventArgs e)
        {
            SetJobState(e.JobStateInfo.State);

            if (IsFinishedState(e.JobStateInfo.State))
            {
                PSEndTime = DateTime.Now;

                // Dispose the PowerShell and Runspace objects.
                System.Management.Automation.PowerShell disposePowerShell = null;
                Runspace disposeRunspace = null;
                lock (SyncRoot)
                {
                    if (_job != null &&
                        IsFinishedState(_job.JobStateInfo.State))
                    {
                        disposePowerShell = _powerShell;
                        _powerShell = null;
                        disposeRunspace = _runspace;
                        _runspace = null;
                    }
                }

                if (disposePowerShell != null)
                {
                    disposePowerShell.Dispose();
                }

                if (disposeRunspace != null)
                {
                    disposeRunspace.Dispose();
                }

                // Raise async job stopped event, if needed.
                if (_asyncJobStop)
                {
                    _asyncJobStop = false;
                    OnStopJobCompleted(new AsyncCompletedEventArgs(null, false, null));
                }

                // Remove AllowSetShouldExit from host.
                RemoveSetShouldExitFromHost();
            }
        }

        internal bool IsFinishedState(JobState state)
        {
            return (state == JobState.Completed || state == JobState.Failed || state == JobState.Stopped);
        }

        private StatusInfo Status
        {
            get
            {
                StatusInfo statusInfo;
                lock (SyncRoot)
                {
                    if (_statusInfo != null)
                    {
                        // Pass back static status.
                        statusInfo = _statusInfo;
                    }
                    else if (_job != null)
                    {
                        // Create current job status.
                        statusInfo = new StatusInfo(
                            _job.InstanceId,
                            _job.Name,
                            _job.Location,
                            _job.Command,
                            _job.StatusMessage,
                            _job.JobStateInfo.State,
                            _job.HasMoreData,
                            PSBeginTime,
                            PSEndTime,
                            _jobDefinition);
                    }
                    else
                    {
                        // Create default static empty status.
                        _statusInfo = new StatusInfo(
                            Guid.Empty,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            JobState.NotStarted,
                            false,
                            PSBeginTime,
                            PSEndTime,
                            _jobDefinition);

                        statusInfo = _statusInfo;
                    }
                }

                return statusInfo;
            }
        }

        private void CopyOutput(ICollection<PSObject> fromOutput)
        {
            PSDataCollection<PSObject> output = CopyResults<PSObject>(fromOutput);
            if (output != null)
            {
                try
                {
                    Output = output;
                }
                catch (InvalidJobStateException) { }
            }
        }

        private void CopyError(ICollection<ErrorRecord> fromError)
        {
            PSDataCollection<ErrorRecord> error = CopyResults<ErrorRecord>(fromError);
            if (error != null)
            {
                try
                {
                    Error = error;
                }
                catch (InvalidJobStateException) { }
            }
        }

        private void CopyWarning(ICollection<WarningRecord> fromWarning)
        {
            PSDataCollection<WarningRecord> warning = CopyResults<WarningRecord>(fromWarning);
            if (warning != null)
            {
                try
                {
                    Warning = warning;
                }
                catch (InvalidJobStateException) { }
            }
        }

        private void CopyVerbose(ICollection<VerboseRecord> fromVerbose)
        {
            PSDataCollection<VerboseRecord> verbose = CopyResults<VerboseRecord>(fromVerbose);
            if (verbose != null)
            {
                try
                {
                    Verbose = verbose;
                }
                catch (InvalidJobStateException) { }
            }
        }

        private void CopyProgress(ICollection<ProgressRecord> fromProgress)
        {
            PSDataCollection<ProgressRecord> progress = CopyResults<ProgressRecord>(fromProgress);
            if (progress != null)
            {
                try
                {
                    Progress = progress;
                }
                catch (InvalidJobStateException) { }
            }
        }

        private void CopyDebug(ICollection<DebugRecord> fromDebug)
        {
            PSDataCollection<DebugRecord> debug = CopyResults<DebugRecord>(fromDebug);
            if (debug != null)
            {
                try
                {
                    Debug = debug;
                }
                catch (InvalidJobStateException) { }
            }
        }

        private void CopyInformation(ICollection<InformationRecord> fromInformation)
        {
            PSDataCollection<InformationRecord> information = CopyResults<InformationRecord>(fromInformation);
            if (information != null)
            {
                try
                {
                    Information = information;
                }
                catch (InvalidJobStateException) { }
            }
        }

        private PSDataCollection<T> CopyResults<T>(ICollection<T> fromResults)
        {
            if (fromResults != null && fromResults.Count > 0)
            {
                PSDataCollection<T> returnResults = new PSDataCollection<T>();
                foreach (var item in fromResults)
                {
                    returnResults.Add(item);
                }

                return returnResults;
            }

            return null;
        }

        private void AddSetShouldExitToHost()
        {
            if (!_allowSetShouldExit || _host == null) { return; }

            PSObject hostPrivateData = _host.PrivateData as PSObject;
            if (hostPrivateData != null)
            {
                // Adds or replaces.
                hostPrivateData.Properties.Add(new PSNoteProperty(AllowHostSetShouldExit, true));
            }
        }

        private void RemoveSetShouldExitFromHost()
        {
            if (!_allowSetShouldExit || _host == null) { return; }

            PSObject hostPrivateData = _host.PrivateData as PSObject;
            if (hostPrivateData != null)
            {
                // Removes if exists.
                hostPrivateData.Properties.Remove(AllowHostSetShouldExit);
            }
        }

        #endregion

        #region Private ResultsInfo class

        [Serializable]
        private sealed class ResultsInfo : ISerializable
        {
            // Private Members
            private Collection<PSObject> _output;
            private Collection<ErrorRecord> _error;
            private Collection<WarningRecord> _warning;
            private Collection<VerboseRecord> _verbose;
            private Collection<ProgressRecord> _progress;
            private Collection<DebugRecord> _debug;
            private Collection<InformationRecord> _information;

            // Properties
            internal Collection<PSObject> Output
            {
                get { return _output; }
            }

            internal Collection<ErrorRecord> Error
            {
                get { return _error; }
            }

            internal Collection<WarningRecord> Warning
            {
                get { return _warning; }
            }

            internal Collection<VerboseRecord> Verbose
            {
                get { return _verbose; }
            }

            internal Collection<ProgressRecord> Progress
            {
                get { return _progress; }
            }

            internal Collection<DebugRecord> Debug
            {
                get { return _debug; }
            }

            internal Collection<InformationRecord> Information
            {
                get { return _information; }
            }

            // Constructors
            internal ResultsInfo(
                Collection<PSObject> output,
                Collection<ErrorRecord> error,
                Collection<WarningRecord> warning,
                Collection<VerboseRecord> verbose,
                Collection<ProgressRecord> progress,
                Collection<DebugRecord> debug,
                Collection<InformationRecord> information
                )
            {
                if (output == null)
                {
                    throw new PSArgumentNullException("output");
                }

                if (error == null)
                {
                    throw new PSArgumentNullException("error");
                }

                if (warning == null)
                {
                    throw new PSArgumentNullException("warning");
                }

                if (verbose == null)
                {
                    throw new PSArgumentNullException("verbose");
                }

                if (progress == null)
                {
                    throw new PSArgumentNullException("progress");
                }

                if (debug == null)
                {
                    throw new PSArgumentNullException("debug");
                }

                if (information == null)
                {
                    throw new PSArgumentNullException("information");
                }

                _output = output;
                _error = error;
                _warning = warning;
                _verbose = verbose;
                _progress = progress;
                _debug = debug;
                _information = information;
            }

            // ISerializable
            private ResultsInfo(
                SerializationInfo info,
                StreamingContext context)
            {
                if (info == null)
                {
                    throw new PSArgumentNullException("info");
                }

                _output = (Collection<PSObject>)info.GetValue("Results_Output", typeof(Collection<PSObject>));
                _error = (Collection<ErrorRecord>)info.GetValue("Results_Error", typeof(Collection<ErrorRecord>));
                _warning = (Collection<WarningRecord>)info.GetValue("Results_Warning", typeof(Collection<WarningRecord>));
                _verbose = (Collection<VerboseRecord>)info.GetValue("Results_Verbose", typeof(Collection<VerboseRecord>));
                _progress = (Collection<ProgressRecord>)info.GetValue("Results_Progress", typeof(Collection<ProgressRecord>));
                _debug = (Collection<DebugRecord>)info.GetValue("Results_Debug", typeof(Collection<DebugRecord>));

                try
                {
                    _information = (Collection<InformationRecord>)info.GetValue("Results_Information", typeof(Collection<InformationRecord>));
                }
                catch(SerializationException)
                {
                    // The job might not have the info stream. Ignore.
                    _information = new Collection<InformationRecord>();
                }
            }

            public void GetObjectData(
                SerializationInfo info,
                StreamingContext context)
            {
                if (info == null)
                {
                    throw new PSArgumentException("info");
                }

                info.AddValue("Results_Output", _output);
                info.AddValue("Results_Error", _error);
                info.AddValue("Results_Warning", _warning);
                info.AddValue("Results_Verbose", _verbose);
                info.AddValue("Results_Progress", _progress);
                info.AddValue("Results_Debug", _debug);
                info.AddValue("Results_Information", _information);
            }
        }

        #endregion
    }

    #region Internal StatusInfo Class

    [Serializable]
    internal class StatusInfo : ISerializable
    {
        // Private Members
        private Guid _instanceId;
        private string _name;
        private string _location;
        private string _command;
        private string _statusMessage;
        private JobState _jobState;
        private bool _hasMoreData;
        private DateTime? _startTime;
        private DateTime? _stopTime;
        private ScheduledJobDefinition _definition;

        // Properties
        internal Guid InstanceId
        {
            get { return _instanceId; }
        }

        internal string Name
        {
            get { return _name; }
        }

        internal string Location
        {
            get { return _location; }
        }

        internal string Command
        {
            get { return _command; }
        }

        internal string StatusMessage
        {
            get { return _statusMessage; }
        }

        internal JobState State
        {
            get { return _jobState; }
        }

        internal bool HasMoreData
        {
            get { return _hasMoreData; }
        }

        internal DateTime? StartTime
        {
            get { return _startTime; }
        }

        internal DateTime? StopTime
        {
            get { return _stopTime; }
        }

        internal ScheduledJobDefinition Definition
        {
            get { return _definition; }
        }

        // Constructors
        internal StatusInfo(
            Guid instanceId,
            string name,
            string location,
            string command,
            string statusMessage,
            JobState jobState,
            bool hasMoreData,
            DateTime? startTime,
            DateTime? stopTime,
            ScheduledJobDefinition definition)
        {
            if (definition == null)
            {
                throw new PSArgumentNullException("definition");
            }

            _instanceId = instanceId;
            _name = name;
            _location = location;
            _command = command;
            _statusMessage = statusMessage;
            _jobState = jobState;
            _hasMoreData = hasMoreData;
            _startTime = startTime;
            _stopTime = stopTime;
            _definition = definition;
        }

        // ISerializable
        private StatusInfo(
            SerializationInfo info,
            StreamingContext context)
        {
            if (info == null)
            {
                throw new PSArgumentNullException("info");
            }

            _instanceId = Guid.Parse(info.GetString("Status_InstanceId"));
            _name = info.GetString("Status_Name");
            _location = info.GetString("Status_Location");
            _command = info.GetString("Status_Command");
            _statusMessage = info.GetString("Status_Message");
            _jobState = (JobState)info.GetValue("Status_State", typeof(JobState));
            _hasMoreData = info.GetBoolean("Status_MoreData");
            _definition = (ScheduledJobDefinition)info.GetValue("Status_Definition", typeof(ScheduledJobDefinition));

            DateTime startTime = info.GetDateTime("Status_StartTime");
            if (startTime != DateTime.MinValue)
            {
                _startTime = startTime;
            }
            else
            {
                _startTime = null;
            }

            DateTime stopTime = info.GetDateTime("Status_StopTime");
            if (stopTime != DateTime.MinValue)
            {
                _stopTime = stopTime;
            }
            else
            {
                _stopTime = null;
            }
        }

        public void GetObjectData(
            SerializationInfo info,
            StreamingContext context)
        {
            if (info == null)
            {
                throw new PSArgumentNullException("info");
            }

            info.AddValue("Status_InstanceId", _instanceId);
            info.AddValue("Status_Name", _name);
            info.AddValue("Status_Location", _location);
            info.AddValue("Status_Command", _command);
            info.AddValue("Status_Message", _statusMessage);
            info.AddValue("Status_State", _jobState);
            info.AddValue("Status_MoreData", _hasMoreData);
            info.AddValue("Status_Definition", _definition);

            if (_startTime != null)
            {
                info.AddValue("Status_StartTime", _startTime);
            }
            else
            {
                info.AddValue("Status_StartTime", DateTime.MinValue);
            }

            if (_stopTime != null)
            {
                info.AddValue("Status_StopTime", _stopTime);
            }
            else
            {
                info.AddValue("Status_StopTime", DateTime.MinValue);
            }
        }
    }

    #endregion
}
