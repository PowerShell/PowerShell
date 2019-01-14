// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Remoting;
using System.Threading;
using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This is the base class for job cmdlet and contains some helper functions.
    /// </summary>
    public class JobCmdletBase : PSRemotingCmdlet
    {
        #region Strings

        // Parametersets used by job cmdlets
        internal const string JobParameterSet = "JobParameterSet";
        internal const string InstanceIdParameterSet = "InstanceIdParameterSet";
        internal const string SessionIdParameterSet = "SessionIdParameterSet";
        internal const string NameParameterSet = "NameParameterSet";
        internal const string StateParameterSet = "StateParameterSet";
        internal const string CommandParameterSet = "CommandParameterSet";
        internal const string FilterParameterSet = "FilterParameterSet";

        // common parameter names
        internal const string JobParameter = "Job";
        internal const string InstanceIdParameter = "InstanceId";
        internal const string SessionIdParameter = "SessionId";
        internal const string NameParameter = "Name";
        internal const string StateParameter = "State";
        internal const string CommandParameter = "Command";
        internal const string FilterParameter = "Filter";

        #endregion Strings

        #region Job Matches

        /// <summary>
        /// Find the jobs in repository which match matching the specified names.
        /// </summary>
        /// <param name="writeobject">if true, method writes the object instead of returning it
        /// in list (an empty list is returned).</param>
        /// <param name="writeErrorOnNoMatch">Write error if no match is found.</param>
        /// <param name="checkIfJobCanBeRemoved">Check if this job can be removed.</param>
        /// <param name="recurse">Recurse and check in child jobs.</param>
        /// <returns>List of matching jobs.</returns>
        internal List<Job> FindJobsMatchingByName(
            bool recurse,
            bool writeobject,
            bool writeErrorOnNoMatch,
            bool checkIfJobCanBeRemoved)
        {
            List<Job> matches = new List<Job>();
            Hashtable duplicateDetector = new Hashtable();

            if (_names == null) return matches;

            foreach (string name in _names)
            {
                if (string.IsNullOrEmpty(name))
                    continue;

                // search all jobs in repository.
                bool jobFound = false;
                duplicateDetector.Clear();
                jobFound = FindJobsMatchingByNameHelper(matches, JobRepository.Jobs, name,
                                    duplicateDetector, recurse, writeobject, checkIfJobCanBeRemoved);

                // search all jobs in JobManager
                List<Job2> jobs2 = JobManager.GetJobsByName(name, this, false, writeobject, recurse, null);

                bool job2Found = (jobs2 != null) && (jobs2.Count > 0);

                if (job2Found)
                {
                    foreach (Job2 job2 in jobs2)
                    {
                        if (CheckIfJob2CanBeRemoved(checkIfJobCanBeRemoved, NameParameter, job2,
                            RemotingErrorIdStrings.JobWithSpecifiedNameNotCompleted, job2.Id, job2.Name))
                        {
                            matches.Add(job2);
                        }
                    }
                }

                jobFound = jobFound || job2Found;

                // if a match is not found, write an error)
                if (jobFound || !writeErrorOnNoMatch || WildcardPattern.ContainsWildcardCharacters(name)) continue;

                Exception ex = PSTraceSource.NewArgumentException(NameParameter, RemotingErrorIdStrings.JobWithSpecifiedNameNotFound, name);
                WriteError(new ErrorRecord(ex, "JobWithSpecifiedNameNotFound", ErrorCategory.ObjectNotFound, name));
            }

            return matches;
        }

        private bool CheckIfJob2CanBeRemoved(bool checkForRemove, string parameterName, Job2 job2, string resourceString, params object[] args)
        {
            if (checkForRemove)
            {
                if (job2.IsFinishedState(job2.JobStateInfo.State))
                    return true;

                string message = PSRemotingErrorInvariants.FormatResourceString(resourceString, args);
                Exception ex = new ArgumentException(message, parameterName);
                WriteError(new ErrorRecord(ex, "JobObjectNotFinishedCannotBeRemoved", ErrorCategory.InvalidOperation, job2));
                return false;
            }

            return true;
        }

        private bool FindJobsMatchingByNameHelper(List<Job> matches, IList<Job> jobsToSearch, string name,
                        Hashtable duplicateDetector, bool recurse, bool writeobject, bool checkIfJobCanBeRemoved)
        {
            Dbg.Assert(!string.IsNullOrEmpty(name), "Caller should ensure that name is not null or empty");

            bool jobFound = false;

            WildcardPattern pattern =
                WildcardPattern.Get(name, WildcardOptions.IgnoreCase | WildcardOptions.Compiled);

            foreach (Job job in jobsToSearch)
            {
                // check if this job has already been searched
                if (duplicateDetector.ContainsKey(job.Id))
                {
                    continue;
                }

                duplicateDetector.Add(job.Id, job.Id);

                // check if the job is available in any of the
                // top level jobs

                // if (string.Equals(job.Name, name, StringComparison.OrdinalIgnoreCase))
                if (pattern.IsMatch(job.Name))
                {
                    jobFound = true;
                    if (!checkIfJobCanBeRemoved || CheckJobCanBeRemoved(job, NameParameter, RemotingErrorIdStrings.JobWithSpecifiedNameNotCompleted, job.Id, job.Name))
                    {
                        if (writeobject)
                        {
                            WriteObject(job);
                        }
                        else
                        {
                            matches.Add(job);
                        }
                    }
                    // break;
                }

                // check if the job is available in any of the childjobs
                if (job.ChildJobs != null && job.ChildJobs.Count > 0 && recurse)
                {
                    bool jobFoundinChildJobs = FindJobsMatchingByNameHelper(matches, job.ChildJobs, name,
                                                    duplicateDetector, recurse, writeobject, checkIfJobCanBeRemoved);

                    if (jobFoundinChildJobs)
                    {
                        jobFound = true;
                    }
                }
            }

            return jobFound;
        }

        /// <summary>
        /// Find the jobs in repository which match the specified instanceid.
        /// </summary>
        /// <param name="writeobject">if true, method writes the object instead of returning it
        /// in list (an empty list is returned).</param>
        /// <param name="writeErrorOnNoMatch">Write error if no match is found.</param>
        /// <param name="checkIfJobCanBeRemoved">Check if this job can be removed.</param>
        /// <param name="recurse">Look in all child jobs.</param>
        /// <returns>List of matching jobs.</returns>
        internal List<Job> FindJobsMatchingByInstanceId(bool recurse, bool writeobject, bool writeErrorOnNoMatch, bool checkIfJobCanBeRemoved)
        {
            List<Job> matches = new List<Job>();

            Hashtable duplicateDetector = new Hashtable();

            if (_instanceIds == null) return matches;

            foreach (Guid id in _instanceIds)
            {
                // search all jobs in Job repository
                duplicateDetector.Clear();
                bool jobFound = FindJobsMatchingByInstanceIdHelper(matches, JobRepository.Jobs, id,
                                    duplicateDetector, recurse, writeobject, checkIfJobCanBeRemoved);

                // TODO: optimize this to not search JobManager since matching by InstanceId is unique
                // search all jobs in JobManager
                Job2 job2 = JobManager.GetJobByInstanceId(id, this, false, writeobject, recurse);

                bool job2Found = job2 != null;

                if (job2Found)
                {
                    if (CheckIfJob2CanBeRemoved(checkIfJobCanBeRemoved, InstanceIdParameter, job2,
                        RemotingErrorIdStrings.JobWithSpecifiedInstanceIdNotCompleted, job2.Id, job2.InstanceId))
                    {
                        matches.Add(job2);
                    }
                }

                jobFound = jobFound || job2Found;

                if (jobFound || !writeErrorOnNoMatch) continue;

                Exception ex = PSTraceSource.NewArgumentException(InstanceIdParameter,
                                                                  RemotingErrorIdStrings.JobWithSpecifiedInstanceIdNotFound,
                                                                  id);

                WriteError(new ErrorRecord(ex, "JobWithSpecifiedInstanceIdNotFound", ErrorCategory.ObjectNotFound, id));
            }

            return matches;
        }

        private bool FindJobsMatchingByInstanceIdHelper(List<Job> matches, IList<Job> jobsToSearch, Guid instanceId,
                        Hashtable duplicateDetector, bool recurse, bool writeobject, bool checkIfJobCanBeRemoved)
        {
            bool jobFound = false;

            // Most likely users will ask for top level jobs.
            // So in order to be more efficient, first look
            // into the top level jobs and only if a match is
            // not found in the top level jobs, recurse. This
            // will ensure that we get a pretty quick hit when
            // the job tree is more than 2 levels deep

            // check if job is found in top level item
            foreach (Job job in jobsToSearch)
            {
                if (duplicateDetector.ContainsKey(job.Id))
                {
                    continue;
                }

                duplicateDetector.Add(job.Id, job.Id);

                if (job.InstanceId == instanceId)
                {
                    jobFound = true;
                    if (!checkIfJobCanBeRemoved || CheckJobCanBeRemoved(job, InstanceIdParameter, RemotingErrorIdStrings.JobWithSpecifiedInstanceIdNotCompleted, job.Id, job.InstanceId))
                    {
                        // instance id is unique, so once a match is found
                        // you can break
                        if (writeobject)
                        {
                            WriteObject(job);
                        }
                        else
                        {
                            matches.Add(job);
                        }

                        break;
                    }
                }
            }

            // check if a match is found in the child jobs
            if (!jobFound && recurse)
            {
                foreach (Job job in jobsToSearch)
                {
                    if (job.ChildJobs != null && job.ChildJobs.Count > 0)
                    {
                        jobFound = FindJobsMatchingByInstanceIdHelper(matches, job.ChildJobs, instanceId,
                                        duplicateDetector, recurse, writeobject, checkIfJobCanBeRemoved);

                        if (jobFound)
                        {
                            break;
                        }
                    }
                }
            }

            return jobFound;
        }

        /// <summary>
        /// Find the jobs in repository which match the specified session ids.
        /// </summary>
        /// <param name="writeobject">if true, method writes the object instead of returning it
        /// in list (an empty list is returned).</param>
        /// <param name="writeErrorOnNoMatch">Write error if no match is found.</param>
        /// <param name="checkIfJobCanBeRemoved">Check if this job can be removed.</param>
        /// <param name="recurse">Look in child jobs as well.</param>
        /// <returns>List of matching jobs.</returns>
        internal List<Job> FindJobsMatchingBySessionId(bool recurse, bool writeobject, bool writeErrorOnNoMatch, bool checkIfJobCanBeRemoved)
        {
            List<Job> matches = new List<Job>();

            if (_sessionIds == null) return matches;

            Hashtable duplicateDetector = new Hashtable();

            foreach (int id in _sessionIds)
            {
                // check jobs in job repository
                bool jobFound = FindJobsMatchingBySessionIdHelper(matches, JobRepository.Jobs, id,
                                    duplicateDetector, recurse, writeobject, checkIfJobCanBeRemoved);

                // check jobs in job manager
                Job2 job2 = JobManager.GetJobById(id, this, false, writeobject, recurse);
                bool job2Found = job2 != null;

                if (job2Found)
                {
                    if (CheckIfJob2CanBeRemoved(checkIfJobCanBeRemoved, SessionIdParameter, job2,
                        RemotingErrorIdStrings.JobWithSpecifiedSessionIdNotCompleted, job2.Id))
                    {
                        matches.Add(job2);
                    }
                }

                jobFound = jobFound || job2Found;

                if (jobFound || !writeErrorOnNoMatch) continue;

                Exception ex = PSTraceSource.NewArgumentException(SessionIdParameter, RemotingErrorIdStrings.JobWithSpecifiedSessionIdNotFound, id);
                WriteError(new ErrorRecord(ex, "JobWithSpecifiedSessionNotFound", ErrorCategory.ObjectNotFound, id));
            }

            return matches;
        }

        private bool FindJobsMatchingBySessionIdHelper(List<Job> matches, IList<Job> jobsToSearch, int sessionId,
                        Hashtable duplicateDetector, bool recurse, bool writeobject, bool checkIfJobCanBeRemoved)
        {
            bool jobFound = false;

            // Most likely users will ask for top level jobs.
            // So in order to be more efficient, first look
            // into the top level jobs and only if a match is
            // not found in the top level jobs, recurse. This
            // will ensure that we get a pretty quick hit when
            // the job tree is more than 2 levels deep

            // check if there is a match in the top level jobs
            foreach (Job job in jobsToSearch)
            {
                if (job.Id == sessionId)
                {
                    jobFound = true;
                    if (!checkIfJobCanBeRemoved || CheckJobCanBeRemoved(job, SessionIdParameter, RemotingErrorIdStrings.JobWithSpecifiedSessionIdNotCompleted, job.Id))
                    {
                        if (writeobject)
                        {
                            WriteObject(job);
                        }
                        else
                        {
                            matches.Add(job);
                        }

                        // session id will be unique for every session, so
                        // can break after the first match
                        break;
                    }
                }
            }

            // check if there is a match found in the child jobs
            if (!jobFound && recurse)
            {
                foreach (Job job in jobsToSearch)
                {
                    if (job.ChildJobs != null && job.ChildJobs.Count > 0)
                    {
                        jobFound = FindJobsMatchingBySessionIdHelper(matches, job.ChildJobs, sessionId,
                                        duplicateDetector, recurse, writeobject, checkIfJobCanBeRemoved);

                        if (jobFound)
                        {
                            break;
                        }
                    }
                }
            }

            return jobFound;
        }

        /// <summary>
        /// Find the jobs in repository which match the specified command.
        /// </summary>
        /// <param name="writeobject">if true, method writes the object instead of returning it
        /// in list (an empty list is returned).</param>
        /// <returns>List of matching jobs.</returns>
        internal List<Job> FindJobsMatchingByCommand(
            bool writeobject)
        {
            List<Job> matches = new List<Job>();

            if (_commands == null) return matches;

            List<Job> jobs = new List<Job>();

            jobs.AddRange(JobRepository.Jobs);

            foreach (string command in _commands)
            {
                List<Job2> jobs2 = JobManager.GetJobsByCommand(command, this, false, false, false, null);

                if (jobs2 != null)
                {
                    foreach (Job2 job2 in jobs2)
                    {
                        jobs.Add(job2);
                    }
                }

                foreach (Job job in jobs)
                {
                    WildcardPattern commandPattern = WildcardPattern.Get(command, WildcardOptions.IgnoreCase);
                    string jobCommand = job.Command.Trim();
                    // Win8: 469830
                    // Win7 code does not have commandPattern.IsMatch. We added wildcard support for Command parameterset
                    // in Win8 which breaks scenarios where the actual command has wildcards.)
                    if (jobCommand.Equals(command.Trim(), StringComparison.OrdinalIgnoreCase) || commandPattern.IsMatch(jobCommand))
                    {
                        if (writeobject)
                        {
                            WriteObject(job);
                        }
                        else
                        {
                            matches.Add(job);
                        }
                    }
                }
            }

            return matches;
        }

        /// <summary>
        /// Find the jobs in repository which match the specified state.
        /// </summary>
        /// <param name="writeobject">if true, method writes the object instead of returning it
        /// in list (an empty list is returned).</param>
        /// <returns>List of matching jobs.</returns>
        internal List<Job> FindJobsMatchingByState(
            bool writeobject)
        {
            List<Job> matches = new List<Job>();
            List<Job> jobs = new List<Job>();

            jobs.AddRange(JobRepository.Jobs);

            List<Job2> jobs2 = JobManager.GetJobsByState(_jobstate, this, false, false, false, null);

            if (jobs2 != null)
            {
                foreach (Job2 job2 in jobs2)
                {
                    jobs.Add(job2);
                }
            }

            foreach (Job job in jobs)
            {
                if (job.JobStateInfo.State != _jobstate) continue;

                if (writeobject)
                {
                    WriteObject(job);
                }
                else
                {
                    matches.Add(job);
                }
            }

            return matches;
        }

        /// <summary>
        /// Find the jobs which match the specified filter.
        /// </summary>
        /// <param name="writeobject"></param>
        /// <returns></returns>
        internal List<Job> FindJobsMatchingByFilter(bool writeobject)
        {
            List<Job> matches = new List<Job>();
            List<Job> jobs = new List<Job>();

            // add Jobs from JobRepository -- only job property based filters are supported.
            FindJobsMatchingByFilterHelper(jobs, JobRepository.Jobs);

            var filterDictionary = new Dictionary<string, object>();
            foreach (string item in _filter.Keys)
            {
                filterDictionary.Add(item, _filter[item]);
            }

            List<Job2> jobs2 = JobManager.GetJobsByFilter(filterDictionary, this, false, false, true);
            if (jobs2 != null)
            {
                foreach (Job2 job2 in jobs2)
                {
                    jobs.Add(job2);
                }
            }

            foreach (Job job in jobs)
            {
                if (writeobject)
                {
                    WriteObject(job);
                }
                else
                {
                    matches.Add(job);
                }
            }

            return matches;
        }

        /// <summary>
        /// Used to find the v2 jobs that match a given filter.
        /// </summary>
        /// <param name="matches"></param>
        /// <param name="jobsToSearch"></param>
        /// <returns></returns>
        private bool FindJobsMatchingByFilterHelper(List<Job> matches, List<Job> jobsToSearch)
        {
            // check that filter only has job properties
            // if so, filter on one at a time using helpers.
            return false;
        }

        /// <summary>
        /// Copies the jobs to list.
        /// </summary>
        /// <param name="jobs"></param>
        /// <param name="writeobject">if true, method writes the object instead of returning it
        /// in list (an empty list is returned).</param>
        /// <param name="checkIfJobCanBeRemoved">If true, only jobs which can be removed will be checked.</param>
        /// <returns></returns>
        internal List<Job> CopyJobsToList(Job[] jobs, bool writeobject, bool checkIfJobCanBeRemoved)
        {
            List<Job> matches = new List<Job>();
            if (jobs == null) return matches;

            foreach (Job job in jobs)
            {
                if (!checkIfJobCanBeRemoved || CheckJobCanBeRemoved(job, "Job", RemotingErrorIdStrings.JobWithSpecifiedSessionIdNotCompleted, job.Id))
                {
                    if (writeobject)
                    {
                        WriteObject(job);
                    }
                    else
                    {
                        matches.Add(job);
                    }
                }
            }

            return matches;
        }

        /// <summary>
        /// Checks that this job object can be removed. If not, writes an error record.
        /// </summary>
        /// <param name="job">Job object to be removed.</param>
        /// <param name="parameterName">Name of the parameter which is associated with this job object.
        /// </param>
        /// <param name="resourceString">Resource String in case of error.</param>
        /// <param name="list">Parameters for resource message.</param>
        /// <returns>True if object should be removed, else false.</returns>
        private bool CheckJobCanBeRemoved(Job job, string parameterName, string resourceString, params object[] list)
        {
            if (job.IsFinishedState(job.JobStateInfo.State))
                return true;
            string message = PSRemotingErrorInvariants.FormatResourceString(resourceString, list);
            Exception ex = new ArgumentException(message, parameterName);
            WriteError(new ErrorRecord(ex, "JobObjectNotFinishedCannotBeRemoved", ErrorCategory.InvalidOperation, job));
            return false;
        }

        #endregion JobMatches

        #region Parameters

        /// <summary>
        /// Name of the jobs to retrieve.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true, Position = 0,
                  Mandatory = true,
                  ParameterSetName = JobCmdletBase.NameParameterSet)]
        [ValidateNotNullOrEmpty]
        public string[] Name
        {
            get
            {
                return _names;
            }

            set
            {
                _names = value;
            }
        }

        /// <summary>
        /// </summary>
        private string[] _names;

        /// <summary>
        /// InstanceIds for which job
        /// need to be obtained.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true, Position = 0,
                   Mandatory = true,
                   ParameterSetName = JobCmdletBase.InstanceIdParameterSet)]
        [ValidateNotNullOrEmpty]
        public Guid[] InstanceId
        {
            get
            {
                return _instanceIds;
            }

            set
            {
                _instanceIds = value;
            }
        }

        /// <summary>
        /// </summary>
        private Guid[] _instanceIds;

        /// <summary>
        /// SessionId for which job
        /// need to be obtained.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true, Position = 0,
                  Mandatory = true,
                  ParameterSetName = JobCmdletBase.SessionIdParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public virtual int[] Id
        {
            get
            {
                return _sessionIds;
            }

            set
            {
                _sessionIds = value;
            }
        }

        /// <summary>
        /// </summary>
        private int[] _sessionIds;

        /// <summary>
        /// All the job objects having this state.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0, ValueFromPipelineByPropertyName = true,
            ParameterSetName = RemoveJobCommand.StateParameterSet)]
        public virtual JobState State
        {
            get
            {
                return _jobstate;
            }

            set
            {
                _jobstate = value;
            }
        }

        /// <summary>
        /// </summary>
        private JobState _jobstate;

        /// <summary>
        /// All the job objects having this command.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
            ParameterSetName = RemoveJobCommand.CommandParameterSet)]
        [ValidateNotNullOrEmpty]
        public virtual string[] Command
        {
            get
            {
                return _commands;
            }

            set
            {
                _commands = value;
            }
        }

        /// <summary>
        /// </summary>
        private string[] _commands;

        /// <summary>
        /// All the job objects matching the values in filter.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0, ValueFromPipelineByPropertyName = true,
            ParameterSetName = RemoveJobCommand.FilterParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual Hashtable Filter
        {
            get { return _filter; }

            set { _filter = value; }
        }

        private Hashtable _filter;

        #endregion Parameters

        #region Overrides

        /// <summary>
        /// All remoting cmdlets other than Start-PSJob should
        /// continue to work even if PowerShell remoting is not
        /// enabled. This is because jobs are based out of APIs
        /// and there can be other job implementations like
        /// eventing or WMI which are not based on PowerShell
        /// remoting.
        /// </summary>
        protected override void BeginProcessing()
        {
            CommandDiscovery.AutoloadModulesWithJobSourceAdapters(this.Context, this.CommandOrigin);
            // intentionally left blank to avoid
            // check being performed in base.BeginProcessing()
        }
        #endregion Overrides
    }

    /// <summary>
    /// This cmdlet removes the Job object from the runspace
    /// wide Job repository.
    ///
    /// Once the Job object is removed, it will not be available
    /// through get-psjob command.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "Job", SupportsShouldProcess = true, DefaultParameterSetName = JobCmdletBase.SessionIdParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113377")]
    [OutputType(typeof(Job), ParameterSetName = new string[] { JobCmdletBase.JobParameterSet })]
    public class RemoveJobCommand : JobCmdletBase, IDisposable
    {
        #region Parameters

        /// <summary>
        /// Specifies the Jobs objects which need to be
        /// removed.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = RemoveJobCommand.JobParameterSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [ValidateNotNullOrEmpty]
        public Job[] Job
        {
            get
            {
                return _jobs;
            }

            set
            {
                _jobs = value;
            }
        }

        private Job[] _jobs;

        /// <summary>
        /// If state of the job is running or notstarted, this will forcefully stop it.
        /// </summary>
        [Parameter(ParameterSetName = RemoveJobCommand.InstanceIdParameterSet)]
        [Parameter(ParameterSetName = RemoveJobCommand.JobParameterSet)]
        [Parameter(ParameterSetName = RemoveJobCommand.NameParameterSet)]
        [Parameter(ParameterSetName = RemoveJobCommand.SessionIdParameterSet)]
        [Parameter(ParameterSetName = RemoveJobCommand.FilterParameterSet)]
        [Alias("F")]
        public SwitchParameter Force
        {
            get
            {
                return _force;
            }

            set
            {
                _force = value;
            }
        }

        private bool _force = false;

        #endregion Parameters

        #region Overrides

        /// <summary>
        /// Gets the job object as per the parameter and removes it.
        /// </summary>
        protected override void ProcessRecord()
        {
            List<Job> listOfJobsToRemove = null;

            switch (ParameterSetName)
            {
                case NameParameterSet:
                    {
                        listOfJobsToRemove = FindJobsMatchingByName(false, false, true, !_force);
                    }

                    break;

                case InstanceIdParameterSet:
                    {
                        listOfJobsToRemove = FindJobsMatchingByInstanceId(true, false, true, !_force);
                    }

                    break;

                case SessionIdParameterSet:
                    {
                        listOfJobsToRemove = FindJobsMatchingBySessionId(true, false, true, !_force);
                    }

                    break;

                case CommandParameterSet:
                    {
                        listOfJobsToRemove = FindJobsMatchingByCommand(false);
                    }

                    break;

                case StateParameterSet:
                    {
                        listOfJobsToRemove = FindJobsMatchingByState(false);
                    }

                    break;

                case FilterParameterSet:
                    {
                        listOfJobsToRemove = FindJobsMatchingByFilter(false);
                    }

                    break;

                default:
                    {
                        listOfJobsToRemove = CopyJobsToList(_jobs, false, !_force);
                    }

                    break;
            }

            // Now actually remove the jobs
            foreach (Job job in listOfJobsToRemove)
            {
                string message = GetMessage(RemotingErrorIdStrings.StopPSJobWhatIfTarget,
                        job.Command, job.Id);

                if (!ShouldProcess(message, VerbsCommon.Remove)) continue;

                Job2 job2 = job as Job2;
                if (!job.IsFinishedState(job.JobStateInfo.State))
                {
                    // if it is a Job2, then async is supported
                    // stop the job asynchronously
                    if (job2 != null)
                    {
                        _cleanUpActions.Add(job2, HandleStopJobCompleted);
                        job2.StopJobCompleted += HandleStopJobCompleted;

                        lock (_syncObject)
                        {
                            if (!job2.IsFinishedState(job2.JobStateInfo.State) &&
                                !_pendingJobs.Contains(job2.InstanceId))
                            {
                                _pendingJobs.Add(job2.InstanceId);
                            }
                        }

                        job2.StopJobAsync();
                    }
                    else
                    {
                        job.StopJob();
                        RemoveJobAndDispose(job, false);
                    }
                }
                else
                {
                    RemoveJobAndDispose(job, job2 != null);
                }
            }
        }

        /// <summary>
        /// Wait for all the stop jobs to be completed.
        /// </summary>
        protected override void EndProcessing()
        {
            bool haveToWait = false;
            lock (_syncObject)
            {
                _needToCheckForWaitingJobs = true;
                if (_pendingJobs.Count > 0)
                    haveToWait = true;
            }

            if (haveToWait)
                _waitForJobs.WaitOne();
        }

        /// <summary>
        /// Release waiting for jobs.
        /// </summary>
        protected override void StopProcessing()
        {
            _waitForJobs.Set();
        }

        #endregion Overrides

        #region Private Methods
        private void RemoveJobAndDispose(Job job, bool jobIsJob2)
        {
            try
            {
                bool job2TypeFound = false;

                if (jobIsJob2)
                {
                    job2TypeFound = JobManager.RemoveJob(job as Job2, this, true, false);
                }

                if (!job2TypeFound)
                {
                    JobRepository.Remove(job);
                }

                job.Dispose();
            }
            catch (ArgumentException ex)
            {
                string message = PSRemotingErrorInvariants.FormatResourceString(
                                        RemotingErrorIdStrings.CannotRemoveJob);

                ArgumentException ex2 = new ArgumentException(message, ex);
                WriteError(new ErrorRecord(ex2, "CannotRemoveJob", ErrorCategory.InvalidOperation, job));
            }
        }

        private void HandleStopJobCompleted(object sender, AsyncCompletedEventArgs eventArgs)
        {
            Job job = sender as Job;
            RemoveJobAndDispose(job, true);

            bool releaseWait = false;
            lock (_syncObject)
            {
                if (_pendingJobs.Contains(job.InstanceId))
                {
                    _pendingJobs.Remove(job.InstanceId);
                }

                if (_needToCheckForWaitingJobs && _pendingJobs.Count == 0)
                    releaseWait = true;
            }
            // end processing has been called
            // set waithandle if this is the last one
            if (releaseWait)
                _waitForJobs.Set();
        }

        #endregion Private Methods

        #region Private Members

        private HashSet<Guid> _pendingJobs = new HashSet<Guid>();
        private readonly ManualResetEvent _waitForJobs = new ManualResetEvent(false);
        private readonly Dictionary<Job2, EventHandler<AsyncCompletedEventArgs>> _cleanUpActions =
            new Dictionary<Job2, EventHandler<AsyncCompletedEventArgs>>();

        private readonly object _syncObject = new object();
        private bool _needToCheckForWaitingJobs;

        #endregion Private Members

        #region Dispose

        /// <summary>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// </summary>
        /// <param name="disposing"></param>
        protected void Dispose(bool disposing)
        {
            if (!disposing) return;
            foreach (var pair in _cleanUpActions)
            {
                pair.Key.StopJobCompleted -= pair.Value;
            }

            _waitForJobs.Dispose();
        }
        #endregion Dispose
    }
}
