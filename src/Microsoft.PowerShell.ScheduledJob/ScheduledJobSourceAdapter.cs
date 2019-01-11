// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.IO;
using System.Globalization;
using System.Runtime.Serialization;

namespace Microsoft.PowerShell.ScheduledJob
{
    /// <summary>
    /// This class provides functionality for retrieving scheduled job run results
    /// from the scheduled job store.  An instance of this object will be registered
    /// with the PowerShell JobManager so that GetJobs commands will retrieve schedule
    /// job runs from the file based scheduled job store.  This allows scheduled job
    /// runs to be managed from PowerShell in the same way workflow jobs are managed.
    /// </summary>
    public sealed class ScheduledJobSourceAdapter : JobSourceAdapter
    {
        #region Private Members

        private static FileSystemWatcher StoreWatcher;
        private static object SyncObject = new object();
        private static ScheduledJobRepository JobRepository = new ScheduledJobRepository();
        internal const string AdapterTypeName = "PSScheduledJob";

        #endregion

        #region Public Strings

        /// <summary>
        /// BeforeFilter.
        /// </summary>
        public const string BeforeFilter = "Before";

        /// <summary>
        /// AfterFilter.
        /// </summary>
        public const string AfterFilter = "After";

        /// <summary>
        /// NewestFilter.
        /// </summary>
        public const string NewestFilter = "Newest";

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        public ScheduledJobSourceAdapter()
        {
            Name = AdapterTypeName;
        }

        #endregion

        #region JobSourceAdapter Implementation

        /// <summary>
        /// Create a new Job2 results instance.
        /// </summary>
        /// <param name="specification">Job specification.</param>
        /// <returns>Job2.</returns>
        public override Job2 NewJob(JobInvocationInfo specification)
        {
            if (specification == null)
            {
                throw new PSArgumentNullException("specification");
            }

            ScheduledJobDefinition scheduledJobDef = new ScheduledJobDefinition(
                specification, null, null, null);

            return new ScheduledJob(
                specification.Command,
                specification.Name,
                scheduledJobDef);
        }

        /// <summary>
        /// Creates a new Job2 object based on a definition name
        /// that can be run manually.  If the path parameter is
        /// null then a default location will be used to find the
        /// job definition by name.
        /// </summary>
        /// <param name="definitionName">ScheduledJob definition name.</param>
        /// <param name="definitionPath">ScheduledJob definition file path.</param>
        /// <returns>Job2 object.</returns>
        public override Job2 NewJob(string definitionName, string definitionPath)
        {
            if (string.IsNullOrEmpty(definitionName))
            {
                throw new PSArgumentException("definitionName");
            }

            Job2 rtnJob = null;
            try
            {
                ScheduledJobDefinition scheduledJobDef =
                    ScheduledJobDefinition.LoadFromStore(definitionName, definitionPath);

                rtnJob = new ScheduledJob(
                    scheduledJobDef.Command,
                    scheduledJobDef.Name,
                    scheduledJobDef);
            }
            catch (FileNotFoundException)
            {
                // Return null if no job definition exists.
            }

            return rtnJob;
        }

        /// <summary>
        /// Get the list of jobs that are currently available in this
        /// store.
        /// </summary>
        /// <returns>Collection of job objects.</returns>
        public override IList<Job2> GetJobs()
        {
            RefreshRepository();

            List<Job2> rtnJobs = new List<Job2>();
            foreach (var job in JobRepository.Jobs)
            {
                rtnJobs.Add(job);
            }

            return rtnJobs;
        }

        /// <summary>
        /// Get list of jobs that matches the specified names.
        /// </summary>
        /// <param name="name">names to match, can support
        ///   wildcard if the store supports</param>
        /// <param name="recurse"></param>
        /// <returns>Collection of jobs that match the specified
        /// criteria.</returns>
        public override IList<Job2> GetJobsByName(string name, bool recurse)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new PSArgumentException("name");
            }

            RefreshRepository();

            WildcardPattern namePattern = new WildcardPattern(name, WildcardOptions.IgnoreCase);
            List<Job2> rtnJobs = new List<Job2>();
            foreach (var job in JobRepository.Jobs)
            {
                if (namePattern.IsMatch(job.Name))
                {
                    rtnJobs.Add(job);
                }
            }

            return rtnJobs;
        }

        /// <summary>
        /// Get list of jobs that run the specified command.
        /// </summary>
        /// <param name="command">Command to match.</param>
        /// <param name="recurse"></param>
        /// <returns>Collection of jobs that match the specified
        /// criteria.</returns>
        public override IList<Job2> GetJobsByCommand(string command, bool recurse)
        {
            if (string.IsNullOrEmpty(command))
            {
                throw new PSArgumentException("command");
            }

            RefreshRepository();

            WildcardPattern commandPattern = new WildcardPattern(command, WildcardOptions.IgnoreCase);
            List<Job2> rtnJobs = new List<Job2>();
            foreach (var job in JobRepository.Jobs)
            {
                if (commandPattern.IsMatch(job.Command))
                {
                    rtnJobs.Add(job);
                }
            }

            return rtnJobs;
        }

        /// <summary>
        /// Get job that has the specified id.
        /// </summary>
        /// <param name="instanceId">Guid to match.</param>
        /// <param name="recurse"></param>
        /// <returns>Job with the specified guid.</returns>
        public override Job2 GetJobByInstanceId(Guid instanceId, bool recurse)
        {
            RefreshRepository();

            foreach (var job in JobRepository.Jobs)
            {
                if (Guid.Equals(job.InstanceId, instanceId))
                {
                    return job;
                }
            }

            return null;
        }

        /// <summary>
        /// Get job that has specific session id.
        /// </summary>
        /// <param name="id">Id to match.</param>
        /// <param name="recurse"></param>
        /// <returns>Job with the specified id.</returns>
        public override Job2 GetJobBySessionId(int id, bool recurse)
        {
            RefreshRepository();

            foreach (var job in JobRepository.Jobs)
            {
                if (id == job.Id)
                {
                    return job;
                }
            }

            return null;
        }

        /// <summary>
        /// Get list of jobs that are in the specified state.
        /// </summary>
        /// <param name="state">State to match.</param>
        /// <param name="recurse"></param>
        /// <returns>Collection of jobs with the specified
        /// state.</returns>
        public override IList<Job2> GetJobsByState(JobState state, bool recurse)
        {
            RefreshRepository();

            List<Job2> rtnJobs = new List<Job2>();
            foreach (var job in JobRepository.Jobs)
            {
                if (state == job.JobStateInfo.State)
                {
                    rtnJobs.Add(job);
                }
            }

            return rtnJobs;
        }

        /// <summary>
        /// Get list of jobs based on the adapter specific
        /// filter parameters.
        /// </summary>
        /// <param name="filter">dictionary containing name value
        ///   pairs for adapter specific filters</param>
        /// <param name="recurse"></param>
        /// <returns>Collection of jobs that match the
        /// specified criteria.</returns>
        public override IList<Job2> GetJobsByFilter(Dictionary<string, object> filter, bool recurse)
        {
            if (filter == null)
            {
                throw new PSArgumentNullException("filter");
            }

            List<Job2> rtnJobs = new List<Job2>();
            foreach (var filterItem in filter)
            {
                switch (filterItem.Key)
                {
                    case BeforeFilter:
                        GetJobsBefore((DateTime)filterItem.Value, ref rtnJobs);
                        break;

                    case AfterFilter:
                        GetJobsAfter((DateTime)filterItem.Value, ref rtnJobs);
                        break;

                    case NewestFilter:
                        GetNewestJobs((int)filterItem.Value, ref rtnJobs);
                        break;
                }
            }

            return rtnJobs;
        }

        /// <summary>
        /// Remove a job from the store.
        /// </summary>
        /// <param name="job">Job object to remove.</param>
        public override void RemoveJob(Job2 job)
        {
            if (job == null)
            {
                throw new PSArgumentNullException("job");
            }

            RefreshRepository();

            try
            {
                JobRepository.Remove(job);
                ScheduledJobStore.RemoveJobRun(
                    job.Name,
                    job.PSBeginTime ?? DateTime.MinValue);
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (FileNotFoundException)
            {
            }
        }

        /// <summary>
        /// Saves job to scheduled job run store.
        /// </summary>
        /// <param name="job">ScheduledJob.</param>
        public override void PersistJob(Job2 job)
        {
            if (job == null)
            {
                throw new PSArgumentNullException("job");
            }

            SaveJobToStore(job as ScheduledJob);
        }

        #endregion

        #region Save Job

        /// <summary>
        /// Serializes a ScheduledJob and saves it to store.
        /// </summary>
        /// <param name="job">ScheduledJob.</param>
        internal static void SaveJobToStore(ScheduledJob job)
        {
            string outputPath = job.Definition.OutputPath;
            if (string.IsNullOrEmpty(outputPath))
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.CantSaveJobNoFilePathSpecified,
                    job.Name);
                throw new ScheduledJobException(msg);
            }

            FileStream fsStatus = null;
            FileStream fsResults = null;
            try
            {
                // Check the job store results and if maximum number of results exist
                // remove the oldest results folder to make room for these new results.
                CheckJobStoreResults(outputPath, job.Definition.ExecutionHistoryLength);

                fsStatus = ScheduledJobStore.CreateFileForJobRunItem(
                    outputPath,
                    job.PSBeginTime ?? DateTime.MinValue,
                    ScheduledJobStore.JobRunItem.Status);

                // Save status only in status file stream.
                SaveStatusToFile(job, fsStatus);

                fsResults = ScheduledJobStore.CreateFileForJobRunItem(
                    outputPath,
                    job.PSBeginTime ?? DateTime.MinValue,
                    ScheduledJobStore.JobRunItem.Results);

                // Save entire job in results file stream.
                SaveResultsToFile(job, fsResults);
            }
            finally
            {
                if (fsStatus != null)
                {
                    fsStatus.Close();
                }

                if (fsResults != null)
                {
                    fsResults.Close();
                }
            }
        }

        /// <summary>
        /// Writes the job status information to the provided
        /// file stream.
        /// </summary>
        /// <param name="job">ScheduledJob job to save.</param>
        /// <param name="fs">FileStream.</param>
        private static void SaveStatusToFile(ScheduledJob job, FileStream fs)
        {
            StatusInfo statusInfo = new StatusInfo(
                job.InstanceId,
                job.Name,
                job.Location,
                job.Command,
                job.StatusMessage,
                job.JobStateInfo.State,
                job.HasMoreData,
                job.PSBeginTime,
                job.PSEndTime,
                job.Definition);

            XmlObjectSerializer serializer = new System.Runtime.Serialization.NetDataContractSerializer();
            serializer.WriteObject(fs, statusInfo);
            fs.Flush();
        }

        /// <summary>
        /// Writes the job (which implements ISerializable) to the provided
        /// file stream.
        /// </summary>
        /// <param name="job">ScheduledJob job to save.</param>
        /// <param name="fs">FileStream.</param>
        private static void SaveResultsToFile(ScheduledJob job, FileStream fs)
        {
            XmlObjectSerializer serializer = new System.Runtime.Serialization.NetDataContractSerializer();
            serializer.WriteObject(fs, job);
            fs.Flush();
        }

        /// <summary>
        /// Check the job store results and if maximum number of results exist
        /// remove the oldest results folder to make room for these new results.
        /// </summary>
        /// <param name="outputPath">Output path.</param>
        /// <param name="executionHistoryLength">Maximum size of stored job results.</param>
        private static void CheckJobStoreResults(string outputPath, int executionHistoryLength)
        {
            // Get current results for this job definition.
            Collection<DateTime> jobRuns = ScheduledJobStore.GetJobRunsForDefinitionPath(outputPath);
            if (jobRuns.Count <= executionHistoryLength)
            {
                // There is room for another job run in the store.
                return;
            }

            // Remove the oldest job run from the store.
            DateTime jobRunToRemove = DateTime.MaxValue;
            foreach (DateTime jobRun in jobRuns)
            {
                jobRunToRemove = (jobRun < jobRunToRemove) ? jobRun : jobRunToRemove;
            }

            try
            {
                ScheduledJobStore.RemoveJobRunFromOutputPath(outputPath, jobRunToRemove);
            }
            catch (UnauthorizedAccessException)
            { }
        }

        #endregion

        #region Retrieve Job

        /// <summary>
        /// Finds and load the Job associated with this ScheduledJobDefinition object
        /// having the job run date time provided.
        /// </summary>
        /// <param name="jobRun">DateTime of job run to load.</param>
        /// <param name="definitionName">ScheduledJobDefinition name.</param>
        /// <returns>Job2 job loaded from store.</returns>
        internal static Job2 LoadJobFromStore(string definitionName, DateTime jobRun)
        {
            FileStream fsResults = null;
            Exception ex = null;
            bool corruptedFile = false;
            Job2 job = null;

            try
            {
                // Results
                fsResults = ScheduledJobStore.GetFileForJobRunItem(
                    definitionName,
                    jobRun,
                    ScheduledJobStore.JobRunItem.Results,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

                job = LoadResultsFromFile(fsResults);
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
            catch (System.Runtime.Serialization.SerializationException)
            {
                corruptedFile = true;
            }
            catch (System.Runtime.Serialization.InvalidDataContractException)
            {
                corruptedFile = true;
            }
            catch (System.Xml.XmlException)
            {
                corruptedFile = true;
            }
            catch (System.TypeInitializationException)
            {
                corruptedFile = true;
            }
            finally
            {
                if (fsResults != null)
                {
                    fsResults.Close();
                }
            }

            if (corruptedFile)
            {
                // Remove the corrupted job results file.
                ScheduledJobStore.RemoveJobRun(definitionName, jobRun);
            }

            if (ex != null)
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.CantLoadJobRunFromStore, definitionName, jobRun);
                throw new ScheduledJobException(msg, ex);
            }

            return job;
        }

        /// <summary>
        /// Loads the Job2 object from provided files stream.
        /// </summary>
        /// <param name="fs">FileStream from which to read job object.</param>
        /// <returns>Created Job2 from file stream.</returns>
        private static Job2 LoadResultsFromFile(FileStream fs)
        {
            XmlObjectSerializer serializer = new System.Runtime.Serialization.NetDataContractSerializer();
            return (Job2)serializer.ReadObject(fs);
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Adds a Job2 object to the repository.
        /// </summary>
        /// <param name="job">Job2.</param>
        internal static void AddToRepository(Job2 job)
        {
            if (job == null)
            {
                throw new PSArgumentNullException("job");
            }

            JobRepository.AddOrReplace(job);
        }

        /// <summary>
        /// Clears all items in the repository.
        /// </summary>
        internal static void ClearRepository()
        {
            JobRepository.Clear();
        }

        /// <summary>
        /// Clears all items for given job definition name in the
        /// repository.
        /// </summary>
        /// <param name="definitionName">Scheduled job definition name.</param>
        internal static void ClearRepositoryForDefinition(string definitionName)
        {
            if (string.IsNullOrEmpty(definitionName))
            {
                throw new PSArgumentException("definitionName");
            }

            // This returns a new list object of repository jobs.
            List<Job2> jobList = JobRepository.Jobs;
            foreach (var job in jobList)
            {
                if (string.Compare(definitionName, job.Name,
                    StringComparison.OrdinalIgnoreCase) == 0)
                {
                    JobRepository.Remove(job);
                }
            }
        }

        #endregion

        #region Private Methods

        private void RefreshRepository()
        {
            ScheduledJobStore.CreateDirectoryIfNotExists();
            CreateFileSystemWatcher();

            IEnumerable<string> jobDefinitions = ScheduledJobStore.GetJobDefinitions();
            foreach (string definitionName in jobDefinitions)
            {
                // Create Job2 objects for each job run in store.
                Collection<DateTime> jobRuns = GetJobRuns(definitionName);
                if (jobRuns == null)
                {
                    continue;
                }

                ScheduledJobDefinition definition = null;
                foreach (DateTime jobRun in jobRuns)
                {
                    if (jobRun > JobRepository.GetLatestJobRun(definitionName))
                    {
                        Job2 job;
                        try
                        {
                            if (definition == null)
                            {
                                definition = ScheduledJobDefinition.LoadFromStore(definitionName, null);
                            }

                            job = LoadJobFromStore(definition.Name, jobRun);
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

                        JobRepository.AddOrReplace(job);
                        JobRepository.SetLatestJobRun(definitionName, jobRun);
                    }
                }
            }
        }

        private void CreateFileSystemWatcher()
        {
            // Lazily create the static file system watcher
            // on first use.
            if (StoreWatcher == null)
            {
                lock (SyncObject)
                {
                    if (StoreWatcher == null)
                    {
                        StoreWatcher = new FileSystemWatcher(ScheduledJobStore.GetJobDefinitionLocation());
                        StoreWatcher.IncludeSubdirectories = true;
                        StoreWatcher.NotifyFilter = NotifyFilters.LastWrite;
                        StoreWatcher.Filter = "Results.xml";
                        StoreWatcher.EnableRaisingEvents = true;
                        StoreWatcher.Changed += (object sender, FileSystemEventArgs e) =>
                        {
                            UpdateRepositoryObjects(e);
                        };
                    }
                }
            }
        }

        private static void UpdateRepositoryObjects(FileSystemEventArgs e)
        {
            // Extract job run information from change file path.
            string updateDefinitionName;
            DateTime updateJobRun;
            if (!GetJobRunInfo(e.Name, out updateDefinitionName, out updateJobRun))
            {
                System.Diagnostics.Debug.Assert(false, "All job run updates should have valid directory names.");
                return;
            }

            // Find corresponding job in repository.
            ScheduledJob updateJob = JobRepository.GetJob(updateDefinitionName, updateJobRun);
            if (updateJob == null)
            {
                return;
            }

            // Load updated job information from store.
            Job2 job = null;
            try
            {
                job = LoadJobFromStore(updateDefinitionName, updateJobRun);
            }
            catch (ScheduledJobException)
            { }
            catch (DirectoryNotFoundException)
            { }
            catch (FileNotFoundException)
            { }
            catch (UnauthorizedAccessException)
            { }
            catch (IOException)
            { }

            // Update job in repository based on new job store data.
            if (job != null)
            {
                updateJob.Update(job as ScheduledJob);
            }
        }

        /// <summary>
        /// Parses job definition name and job run DateTime from provided path string.
        /// Example:
        ///   path = "ScheduledJob1\\Output\\20111219-200921-369\\Results.xml"
        ///      'ScheduledJob1' is the definition name.
        ///      '20111219-200921-369' is the jobRun DateTime.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="definitionName"></param>
        /// <param name="jobRunReturn"></param>
        /// <returns></returns>
        private static bool GetJobRunInfo(
            string path,
            out string definitionName,
            out DateTime jobRunReturn)
        {
            // Parse definition name from path.
            string[] pathItems = path.Split(System.IO.Path.DirectorySeparatorChar);
            if (pathItems.Length == 4)
            {
                definitionName = pathItems[0];
                return ScheduledJobStore.ConvertJobRunNameToDateTime(pathItems[2], out jobRunReturn);
            }

            definitionName = null;
            jobRunReturn = DateTime.MinValue;
            return false;
        }

        internal static Collection<DateTime> GetJobRuns(string definitionName)
        {
            Collection<DateTime> jobRuns = null;
            try
            {
                jobRuns = ScheduledJobStore.GetJobRunsForDefinition(definitionName);
            }
            catch (DirectoryNotFoundException)
            { }
            catch (FileNotFoundException)
            { }
            catch (UnauthorizedAccessException)
            { }
            catch (IOException)
            { }

            return jobRuns;
        }

        private void GetJobsBefore(
            DateTime dateTime,
            ref List<Job2> jobList)
        {
            foreach (var job in JobRepository.Jobs)
            {
                if (job.PSEndTime < dateTime &&
                    !jobList.Contains(job))
                {
                    jobList.Add(job);
                }
            }
        }

        private void GetJobsAfter(
            DateTime dateTime,
            ref List<Job2> jobList)
        {
            foreach (var job in JobRepository.Jobs)
            {
                if (job.PSEndTime > dateTime &&
                    !jobList.Contains(job))
                {
                    jobList.Add(job);
                }
            }
        }

        private void GetNewestJobs(
            int maxNumber,
            ref List<Job2> jobList)
        {
            List<Job2> allJobs = JobRepository.Jobs;

            // Sort descending.
            allJobs.Sort((firstJob, secondJob) =>
                {
                    if (firstJob.PSEndTime > secondJob.PSEndTime)
                    {
                        return -1;
                    }
                    else if (firstJob.PSEndTime < secondJob.PSEndTime)
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                });

            int count = 0;
            foreach (var job in allJobs)
            {
                if (++count > maxNumber)
                {
                    break;
                }

                if (!jobList.Contains(job))
                {
                    jobList.Add(job);
                }
            }
        }

        #endregion

        #region Private Repository Class

        /// <summary>
        /// Collection of Job2 objects.
        /// </summary>
        internal class ScheduledJobRepository
        {
            #region Private Members

            private object _syncObject = new object();
            private Dictionary<Guid, Job2> _jobs = new Dictionary<Guid, Job2>();
            private Dictionary<string, DateTime> _latestJobRuns = new Dictionary<string, DateTime>();

            #endregion

            #region Public Properties

            /// <summary>
            /// Returns all job objects in the repository as a List.
            /// </summary>
            public List<Job2> Jobs
            {
                get
                {
                    lock (_syncObject)
                    {
                        return new List<Job2>(_jobs.Values);
                    }
                }
            }

            /// <summary>
            /// Returns count of jobs in repository.
            /// </summary>
            public int Count
            {
                get
                {
                    lock (_syncObject)
                    {
                        return _jobs.Count;
                    }
                }
            }

            #endregion

            #region Public Methods

            /// <summary>
            /// Add Job2 to repository.
            /// </summary>
            /// <param name="job">Job2 to add.</param>
            public void Add(Job2 job)
            {
                if (job == null)
                {
                    throw new PSArgumentNullException("job");
                }

                lock (_syncObject)
                {
                    if (_jobs.ContainsKey(job.InstanceId))
                    {
                        string msg = StringUtil.Format(ScheduledJobErrorStrings.ScheduledJobAlreadyExistsInLocal, job.Name, job.InstanceId);
                        throw new ScheduledJobException(msg);
                    }

                    _jobs.Add(job.InstanceId, job);
                }
            }

            /// <summary>
            /// Add or replace passed in Job2 object to repository.
            /// </summary>
            /// <param name="job">Job2 to add.</param>
            public void AddOrReplace(Job2 job)
            {
                if (job == null)
                {
                    throw new PSArgumentNullException("job");
                }

                lock (_syncObject)
                {
                    if (_jobs.ContainsKey(job.InstanceId))
                    {
                        _jobs.Remove(job.InstanceId);
                    }

                    _jobs.Add(job.InstanceId, job);
                }
            }

            /// <summary>
            /// Remove Job2 from repository.
            /// </summary>
            /// <param name="job"></param>
            public void Remove(Job2 job)
            {
                if (job == null)
                {
                    throw new PSArgumentNullException("job");
                }

                lock (_syncObject)
                {
                    if (_jobs.ContainsKey(job.InstanceId) == false)
                    {
                        string msg = StringUtil.Format(ScheduledJobErrorStrings.ScheduledJobNotInRepository, job.Name);
                        throw new ScheduledJobException(msg);
                    }

                    _jobs.Remove(job.InstanceId);
                }
            }

            /// <summary>
            /// Clears all Job2 items from the repository.
            /// </summary>
            public void Clear()
            {
                lock (_syncObject)
                {
                    _jobs.Clear();
                }
            }

            /// <summary>
            /// Gets the latest job run Date/Time for the given definition name.
            /// </summary>
            /// <param name="definitionName">ScheduledJobDefinition name.</param>
            /// <returns>Job Run DateTime.</returns>
            public DateTime GetLatestJobRun(string definitionName)
            {
                if (string.IsNullOrEmpty(definitionName))
                {
                    throw new PSArgumentException("definitionName");
                }

                lock (_syncObject)
                {
                    if (_latestJobRuns.ContainsKey(definitionName))
                    {
                        return _latestJobRuns[definitionName];
                    }
                    else
                    {
                        DateTime startJobRun = DateTime.MinValue;
                        _latestJobRuns.Add(definitionName, startJobRun);
                        return startJobRun;
                    }
                }
            }

            /// <summary>
            /// Sets the latest job run Date/Time for the given definition name.
            /// </summary>
            /// <param name="definitionName"></param>
            /// <param name="jobRun"></param>
            public void SetLatestJobRun(string definitionName, DateTime jobRun)
            {
                if (string.IsNullOrEmpty(definitionName))
                {
                    throw new PSArgumentException("definitionName");
                }

                lock (_syncObject)
                {
                    if (_latestJobRuns.ContainsKey(definitionName))
                    {
                        _latestJobRuns.Remove(definitionName);
                        _latestJobRuns.Add(definitionName, jobRun);
                    }
                    else
                    {
                        _latestJobRuns.Add(definitionName, jobRun);
                    }
                }
            }

            /// <summary>
            /// Search repository for specific job run.
            /// </summary>
            /// <param name="definitionName">Definition name.</param>
            /// <param name="jobRun">Job run DateTime.</param>
            /// <returns>Scheduled job if found.</returns>
            public ScheduledJob GetJob(string definitionName, DateTime jobRun)
            {
                lock (_syncObject)
                {
                    foreach (ScheduledJob job in _jobs.Values)
                    {
                        if (job.PSBeginTime == null)
                        {
                            continue;
                        }

                        DateTime PSBeginTime = job.PSBeginTime ?? DateTime.MinValue;
                        if (definitionName.Equals(job.Definition.Name, StringComparison.OrdinalIgnoreCase) &&
                            jobRun.Year == PSBeginTime.Year &&
                            jobRun.Month == PSBeginTime.Month &&
                            jobRun.Day == PSBeginTime.Day &&
                            jobRun.Hour == PSBeginTime.Hour &&
                            jobRun.Minute == PSBeginTime.Minute &&
                            jobRun.Second == PSBeginTime.Second &&
                            jobRun.Millisecond == PSBeginTime.Millisecond)
                        {
                            return job;
                        }
                    }
                }

                return null;
            }

            #endregion
        }

        #endregion
    }
}
