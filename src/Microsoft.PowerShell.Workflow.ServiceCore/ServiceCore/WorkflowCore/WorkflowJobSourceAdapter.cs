/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
using System;
using System.Activities;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Tracing;
using System.Management.Automation.Remoting.WSMan;
using Dbg = System.Diagnostics.Debug;
using System.Management.Automation.Remoting;


// Stops compiler from warning about unknown warnings
#pragma warning disable 1634, 1691

namespace Microsoft.PowerShell.Workflow
{
    /// <summary>
    /// Adapter that allows workflow instances to be exposed as jobs in PowerShell.
    /// NOTE: This class has been unsealed to allow extensibility for Opalis. Further
    /// thought is needed around the best way to enable reuse of this class.
    /// </summary>
    public sealed class WorkflowJobSourceAdapter : JobSourceAdapter
    {
        #region ContainerParentJobRepository

        private class ContainerParentJobRepository : Repository<ContainerParentJob>
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="item"></param>
            /// <returns></returns>
            protected override Guid GetKey(ContainerParentJob item)
            {
                return item.InstanceId;
            }

            internal ContainerParentJobRepository(string identifier): base(identifier)
            {
                
            }
        }
        #endregion ContainerParentJobRepository

        #region Members

        private readonly PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();
        private readonly Tracer _structuredTracer = new Tracer();

        private static readonly WorkflowJobSourceAdapter Instance = new WorkflowJobSourceAdapter();

        private readonly ContainerParentJobRepository _jobRepository =
            new ContainerParentJobRepository("WorkflowJobSourceAdapterRepository");

        private readonly object _syncObject = new object();
        private bool _repositoryPopulated;
        internal const string AdapterTypeName = "PSWorkflowJob";

        #endregion Members

        #region Overrides of JobSourceAdapter

        /// <summary>
        /// Gets the WorkflowJobSourceAdapter instance.
        /// </summary>
        public static WorkflowJobSourceAdapter GetInstance()
        {
            return Instance;
        }

        private PSWorkflowJobManager _jobManager;
        /// <summary>
        /// GetJobManager
        /// </summary>
        /// <returns></returns>
        public PSWorkflowJobManager GetJobManager()
        {
            if (_jobManager != null)
                return _jobManager;

            lock (_syncObject)
            {
                if (_jobManager != null)
                    return _jobManager;

                _jobManager = PSWorkflowRuntime.Instance.JobManager;
            }
            return _jobManager;
        }

        /// <summary>
        /// GetPSWorkflowRunTime
        /// </summary>
        /// <returns></returns>
        public PSWorkflowRuntime GetPSWorkflowRuntime()
        {
            if (_runtime != null)
                return _runtime;

            lock (_syncObject)
            {
                if (_runtime != null)
                    return _runtime;

                _runtime = PSWorkflowRuntime.Instance;
            }
            return _runtime;
        }

        private PSWorkflowRuntime _runtime;
        private PSWorkflowValidator _wfValidator;
        internal PSWorkflowValidator GetWorkflowValidator()
        {
            if (_wfValidator != null)
                return _wfValidator;

            lock (_syncObject)
            {
                if (_wfValidator != null)
                    return _wfValidator;

                _wfValidator = new PSWorkflowValidator(PSWorkflowRuntime.Instance.Configuration);
            }
            return _wfValidator;
        }

        /// <summary>
        /// Create a new job with the specified JobSpecification.
        /// </summary>
        /// <param name="specification">specification</param>
        /// <returns>job object</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public override Job2 NewJob(JobInvocationInfo specification)
        {
            if (specification == null)
                throw new ArgumentNullException("specification");

            if (specification.Definition == null)
                throw new ArgumentException(Resources.NewJobDefinitionNull, "specification");

            if (specification.Definition.JobSourceAdapterType != GetType())
                throw new InvalidOperationException(Resources.NewJobWrongType);

            if (specification.Parameters.Count == 0)
            {
                // If there are no parameters passed in, we create one child job with nothing specified.
                specification.Parameters.Add(new CommandParameterCollection());
            }

            // validation has to happen before job creation
            bool? isSuspendable = null;
            Activity activity = ValidateWorkflow(specification, null, ref isSuspendable);
            ContainerParentJob newJob = GetJobManager().CreateJob(specification, activity);

            // We do not want to generate the warning message if the request is coming from
            // Server Manager
            if (PSSessionConfigurationData.IsServerManager == false)
            {
                foreach (PSWorkflowJob job in newJob.ChildJobs)
                {
                    bool? psPersitValue = null;
                    PSWorkflowContext context = job.PSWorkflowInstance.PSWorkflowContext;
                    if (context != null && context.PSWorkflowCommonParameters != null && context.PSWorkflowCommonParameters.ContainsKey(Constants.Persist))
                    {
                        psPersitValue = context.PSWorkflowCommonParameters[Constants.Persist] as bool?;
                    }

                    // check for invocation time pspersist value if not true then there is a possibility that workflow is not suspendable.
                    if (psPersitValue == null || (psPersitValue == false))
                    {
                        // check for authoring time definition of persist activity 
                        if (isSuspendable != null && isSuspendable.Value == false)
                        {
                            job.Warning.Add(new WarningRecord(Resources.WarningMessageForPersistence));
                            job.IsSuspendable = isSuspendable;
                        }
                    }

                }
            }
            
            StoreJobIdForReuse(newJob, true);
            _jobRepository.Add(newJob);

            return newJob;
        }

        /// <summary>
        /// Get the list of jobs that are currently available in the workflow instance table.
        /// </summary>
        /// <returns>collection of job objects</returns>
        public override IList<Job2> GetJobs()
        {
            _tracer.WriteMessage("WorkflowJobSourceAdapter: Getting all Workflow jobs");
            PopulateJobRepositoryIfRequired();
            return new List<Job2>(_jobRepository.GetItems());
        }

        /// <summary>
        /// Get list of jobs that matches the specified names
        /// </summary>
        /// <param name="name">names to match, can support
        ///   wildcard if the store supports</param>
        /// <param name="recurse"></param>
        /// <returns>collection of jobs that match the specified
        /// criteria</returns>
        public override IList<Job2> GetJobsByName(string name, bool recurse)
        {
            _tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "WorkflowJobSourceAdapter: Getting Workflow jobs by name: {0}", name));
            PopulateJobRepositoryIfRequired();
            WildcardPattern patternForName = new WildcardPattern(name, WildcardOptions.IgnoreCase | WildcardOptions.Compiled);

            List<Job2> selectedJobs = new List<Job2>();
            var jobs = _jobRepository.GetItems().Where(parentJob => patternForName.IsMatch(parentJob.Name)).Cast<Job2>().
                        ToList();
            selectedJobs.AddRange(jobs);
            
            if (recurse)
            {
                var filter = new Dictionary<string, object> { { Constants.JobMetadataName, patternForName } };
                var childJobs = GetJobManager().GetJobs(GetChildJobsFromRepository(), WorkflowFilterTypes.JobMetadata, filter);
                selectedJobs.AddRange(childJobs);
            }

            return selectedJobs;
        }

        /// <summary>
        /// Get list of jobs that run the specified command
        /// </summary>
        /// <param name="command">command to match</param>
        /// <param name="recurse"></param>
        /// <returns>collection of jobs that match the specified
        /// criteria</returns>
        public override IList<Job2> GetJobsByCommand(string command, bool recurse)
        {
            _tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "WorkflowJobSourceAdapter: Getting Workflow jobs by command: {0}", command));
            PopulateJobRepositoryIfRequired();
            List<Job2> jobs = new List<Job2>();
            WildcardPattern patternForCommand = new WildcardPattern(command, WildcardOptions.IgnoreCase | WildcardOptions.Compiled);

            var list =
                _jobRepository.GetItems().Where(parentJob => patternForCommand.IsMatch(parentJob.Command)).Cast<Job2>().
                    ToList();
            if (list.Count > 0)
                jobs.AddRange(list);

            if (recurse)
            {
                var filter = new Dictionary<string, object> {{Constants.JobMetadataParentCommand, patternForCommand}};
                var listChildJobs = GetJobManager().GetJobs(GetChildJobsFromRepository(), WorkflowFilterTypes.JobMetadata,
                                                   filter);
                jobs.AddRange(listChildJobs);
            }

            return jobs;
        }

        /// <summary>
        /// Get list of jobs that has the specified id
        /// </summary>
        /// <param name="instanceId">Guid to match</param> 
        /// <param name="recurse"></param>
        /// <returns>job with the specified guid</returns>
        public override Job2 GetJobByInstanceId(Guid instanceId, bool recurse)
        {
            _tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "WorkflowJobSourceAdapter: Getting Workflow job by instance id: {0}", instanceId));

            // there can be a lot of jobs in the repository. First search whats in 
            // memory before rehydrating from disk
            Job2 selectedJob = _jobRepository.GetItem(instanceId);
            if (selectedJob == null)
            {
                PopulateJobRepositoryIfRequired();
                selectedJob = _jobRepository.GetItem(instanceId);
            }

            if (selectedJob != null) return selectedJob;

            if (recurse)
            {
                selectedJob = GetChildJobsFromRepository().FirstOrDefault(job => job.InstanceId == instanceId);
            }
            return selectedJob;
        }

        /// <summary>
        /// Get job by session id.
        /// </summary>
        /// <param name="id">The session id.</param>
        /// <param name="recurse"></param>
        /// <returns>The returned job2 object.</returns>
        public override Job2 GetJobBySessionId(int id, bool recurse)
        {
            _tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "WorkflowJobSourceAdapter: Getting Workflow job by session id: {0}", id));

            PopulateJobRepositoryIfRequired();

            Job2 selectedJob = _jobRepository.GetItems().FirstOrDefault(job => job.Id == id);
            if (selectedJob != null) return selectedJob;

            if (recurse)
            {
                selectedJob = GetChildJobsFromRepository().FirstOrDefault(job => job.Id == id);
            }
            return selectedJob;
        }

        /// <summary>
        /// Get list of jobs that are in the specified state
        /// </summary>
        /// <param name="state">state to match</param>
        /// <param name="recurse"></param>
        /// <returns>collection of jobs with the specified
        /// state</returns>
        public override IList<Job2> GetJobsByState(JobState state, bool recurse)
        {
            _tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "WorkflowJobSourceAdapter: Getting Workflow jobs by state: {0}", state));

            PopulateJobRepositoryIfRequired();
            // Parent states are derived from child states, so a simple filter will not work.
            List<Job2> jobs = recurse
                                  ? GetChildJobsFromRepository().Where(job => job.JobStateInfo.State == state).Cast
                                        <Job2>().ToList()
                                  : _jobRepository.GetItems().Where(job => job.JobStateInfo.State == state).Cast<Job2>()
                                        .ToList();
            return jobs;
        }

        /// <summary>
        /// Get list of jobs based on the adapter specific
        /// filter parameters
        /// </summary>
        /// <param name="filter">dictionary containing name value
        ///   pairs for adapter specific filters</param>
        /// <param name="recurse"></param>
        /// <returns>collection of jobs that match the 
        /// specified criteria</returns>
        public override IList<Job2> GetJobsByFilter(Dictionary<string, object> filter, bool recurse)
        {
            if (filter == null)
            {
                throw new ArgumentNullException("filter");
            }
            _tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                                               "WorkflowJobSourceAdapter: Getting Workflow jobs by filter: {0}", filter));
            PopulateJobRepositoryIfRequired();

            // Do not modify the user's collection.
            Dictionary<string, object> filter2 = new Dictionary<string, object>(filter, StringComparer.CurrentCultureIgnoreCase);
            bool addPid = false;
            bool searchParentJobs = true;
            if (filter2.Keys.Count == 0) 
                searchParentJobs = false;
            else
            {
                if (filter2.Keys.Any(key => (((!key.Equals(Constants.JobMetadataSessionId, StringComparison.OrdinalIgnoreCase) && !key.Equals(Constants.JobMetadataInstanceId, StringComparison.OrdinalIgnoreCase)) && !key.Equals(Constants.JobMetadataName, StringComparison.OrdinalIgnoreCase)) && !key.Equals(Constants.JobMetadataCommand, StringComparison.OrdinalIgnoreCase)) && !key.Equals(Constants.JobMetadataFilterState, StringComparison.OrdinalIgnoreCase)))
                {
                    searchParentJobs = false;
                }
            }

            List<Job2> jobs = new List<Job2>();
            // search container parent jobs first
            if (searchParentJobs)
            {
                List<ContainerParentJob> repositoryJobs  = _jobRepository.GetItems();

                List<Job2> searchList = SearchJobsOnV2Parameters(repositoryJobs, filter2);
                repositoryJobs.Clear();

                if (searchList.Count > 0)
                {
                    jobs.AddRange(searchList);
                }
            }

            if (recurse)
            {
                // If the session Id parameter is present, make sure that the Id match is valid by adding the process Id to the filter.
                if (filter2.ContainsKey(Constants.JobMetadataSessionId))
                    addPid = true;

                if (addPid) filter2.Add(Constants.JobMetadataPid, Process.GetCurrentProcess().Id);

                if (filter2.ContainsKey(Constants.JobMetadataFilterState))
                {
                    filter2.Remove(Constants.JobMetadataFilterState);
                }

                LoadWorkflowInstancesFromStore();

                // remove state from filter here and do it separately
                IEnumerable<Job2> workflowInstances = GetJobManager().GetJobs(WorkflowFilterTypes.All, filter2);

                if (filter.ContainsKey(Constants.JobMetadataFilterState))
                {
                    JobState searchState =
                        (JobState)
                        LanguagePrimitives.ConvertTo(filter[Constants.JobMetadataFilterState], typeof(JobState), CultureInfo.InvariantCulture);
                    var list = workflowInstances.Where(job => job.JobStateInfo.State == searchState).ToList();
                    jobs.AddRange(list);
                }
                else
                {
                    jobs.AddRange(workflowInstances);
                }
            }

            List<Job2> cpjs = new List<Job2>();
            foreach (var job in jobs)
            {
                if (job is ContainerParentJob && !cpjs.Contains(job))
                {
                    cpjs.Add(job);
                    continue;
                }

                PSWorkflowJob wfj = job as PSWorkflowJob;
                Dbg.Assert(wfj != null, "if it's not a containerparentjob, it had better be a workflowjob");
                ContainerParentJob cpj = _jobRepository.GetItem((Guid)wfj.JobMetadata[Constants.JobMetadataParentInstanceId]);
                if (!cpjs.Contains(cpj))
                {
                    cpjs.Add(cpj);
                }
            }

            return cpjs;
        }

        /// <summary>
        /// Remove a job from the store 
        /// </summary>
        /// <param name="job">job object to remove</param>
        public override void RemoveJob(Job2 job)
        {
            if (job == null)
            {
                throw new ArgumentNullException("job");
            }
            _structuredTracer.RemoveJobStarted(job.InstanceId);

            var jobInMemory = _jobRepository.GetItem(job.InstanceId);
            if (jobInMemory == null)
            {
                // the specified job is not available in memory
                // load the job repository
                PopulateJobRepositoryIfRequired();
            }

            if (!(job is ContainerParentJob))
            {
                throw new InvalidOperationException(Resources.CannotRemoveWorkflowJobDirectly);
            }

            _tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "WorkflowJobSourceAdapter: Removing Workflow job with instance id: {0}", job.InstanceId));

            Exception innerException = null;

            foreach (Job childJob in job.ChildJobs)
            {
                PSWorkflowJob workflowChildJob = childJob as PSWorkflowJob;

                if (workflowChildJob == null) continue;

                try
                {
                    GetJobManager().RemoveJob(workflowChildJob.InstanceId);
                    _structuredTracer.JobRemoved(job.InstanceId,
                                                 childJob.InstanceId, workflowChildJob.WorkflowGuid);
                }
                catch (ArgumentException exception)
                {
                    //ignoring the error message and just logging them into ETW
                    _tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                                                       "WorkflowJobSourceAdapter: Ignoring the exception. Exception details: {0}",
                                                       exception));
                    innerException = exception;
                    _structuredTracer.JobRemoveError(job.InstanceId,
                                                     childJob.InstanceId, workflowChildJob.WorkflowGuid,
                                                     exception.Message);
                }
            }

            // remove the container parent job from repository
            try
            {
                _jobRepository.Remove((ContainerParentJob) job);
            }
            catch(ArgumentException exception)
            {
                //ignoring the error message and just logging them into ETW
                _tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                                                   "WorkflowJobSourceAdapter: Ignoring the exception. Exception details: {0}",
                                                   exception));
                innerException = exception;
            }
            job.Dispose();

            // Failed to remove the job?
            if (innerException != null)
            {
                ArgumentException exc = new ArgumentException(Resources.WorkflowChildCouldNotBeRemoved, "job", innerException);
                throw exc;
            }
        }

        // in case of multiple child jobs synchronization is very important
        private readonly object _syncRemoveChilJob = new object();
        internal void RemoveChildJob(Job2 childWorkflowJob)
        {
            _structuredTracer.RemoveJobStarted(childWorkflowJob.InstanceId);
            PopulateJobRepositoryIfRequired();

            _tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "WorkflowJobSourceAdapter: Removing Workflow job with instance id: {0}", childWorkflowJob.InstanceId));

            lock (_syncRemoveChilJob)
            {
                PSWorkflowJob childJob = childWorkflowJob as PSWorkflowJob;
                if (childJob == null) return;

                object data;
                PSWorkflowInstance instance = childJob.PSWorkflowInstance;

                if (!instance.PSWorkflowContext.JobMetadata.TryGetValue(Constants.JobMetadataParentInstanceId, out data)) return;
                var parentInstanceId = (Guid)data;
                ContainerParentJob job = _jobRepository.GetItem(parentInstanceId);

                job.ChildJobs.Remove(childJob);


                try
                {
                    GetJobManager().RemoveJob(childJob.InstanceId);
                    _structuredTracer.JobRemoved(job.InstanceId,
                                                 childJob.InstanceId, childJob.WorkflowGuid);
                }
                catch (ArgumentException exception)
                {
                    //ignoring the error message and just logging them into ETW
                    _tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                                                       "WorkflowJobSourceAdapter: Ignoring the exception. Exception details: {0}",
                                                       exception));

                    _structuredTracer.JobRemoveError(job.InstanceId,
                                                     childJob.InstanceId, childJob.WorkflowGuid,
                                                     exception.Message);
                }

                if (job.ChildJobs.Count == 0)
                {
                    // remove the container parent job from repository
                    try
                    {
                        _jobRepository.Remove(job);
                    }
                    catch (ArgumentException exception)
                    {
                        //ignoring the error message and just logging them into ETW
                        _tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                                                           "WorkflowJobSourceAdapter: Ignoring the exception. Exception details: {0}",
                                                           exception));
                    }
                    job.Dispose();
                }
            }
        }

        #endregion

        #region Private Methods

        private Activity ValidateWorkflow(JobInvocationInfo invocationInfo, Activity activity, ref bool? isSuspendable)
        {
            PSWorkflowRuntime runtime = PSWorkflowRuntime.Instance;
            JobDefinition definition = invocationInfo.Definition;
            WorkflowJobDefinition workflowJobDefinition = WorkflowJobDefinition.AsWorkflowJobDefinition(definition);

            bool externalActivity = true;

            if (activity == null)
            {
                bool windowsWorkflow;
                activity = DefinitionCache.Instance.GetActivityFromCache(workflowJobDefinition, out windowsWorkflow);
                // Debug.Assert(activity != null, "WorkflowManager failed validation for a null activity");
                if (activity == null)
                {
                    throw new InvalidOperationException();
                }

                externalActivity = false;

                // skip validation if it is a windows workflow
                if (windowsWorkflow)
                    return activity;
            }

            // If validation is disabled, just return the activity as is...
            if (!runtime.Configuration.EnableValidation)
            {
                return activity;
            }

            if (externalActivity && !DefinitionCache.Instance.AllowExternalActivity)
            {
                // If there is a custom validator activity, then call it. If it returns true
                // then just return the activity.
                if (Validation.CustomHandler == null)
                    throw new InvalidOperationException();

                if (Validation.CustomHandler.Invoke(activity))
                {
                    return activity;
                }

                string displayName = activity.DisplayName;

                if (string.IsNullOrEmpty(displayName))
                    displayName = this.GetType().Name;

                string message = string.Format(CultureInfo.CurrentCulture, Resources.InvalidActivity, displayName);
                throw new ValidationException(message);
            }
            PSWorkflowValidationResults validationResults = GetWorkflowValidator().ValidateWorkflow(
                definition.InstanceId, activity,
                DefinitionCache.Instance.GetRuntimeAssemblyName(workflowJobDefinition));
                
            if (validationResults.Results != null)
            {
                GetWorkflowValidator().ProcessValidationResults(validationResults.Results);
            }

            isSuspendable = validationResults.IsWorkflowSuspendable;

            return activity;
        }

        /// <summary>
        /// Searches a list of jobs with a given set of filters for all
        /// the V2 search parameters. This function searches in a specific
        /// order so that a get call from an API returns without having
        /// to do much processing in terms of wildcard pattern matching
        /// </summary>
        /// <param name="jobsToSearch">incoming enumeration of jobs to
        /// search</param>
        /// <param name="filter">dictionary of filters to use as search
        /// criteria</param>
        /// <returns>narrowed down list of jobs that satisfy the filter
        /// criteria</returns>
        internal static List<Job2> SearchJobsOnV2Parameters(IEnumerable<Job2> jobsToSearch, IDictionary<string, object> filter)
        {
            List<Job2> searchList = new List<Job2>();
            searchList.AddRange(jobsToSearch);

            List<Job2> newlist;
            if (filter.ContainsKey(Constants.JobMetadataSessionId))
            {
                int searchId = (int)filter[Constants.JobMetadataSessionId];
                newlist = searchList.Where(job => job.Id == searchId).ToList();
                searchList.Clear();
                searchList = newlist;
            }
            if (filter.ContainsKey(Constants.JobMetadataInstanceId))
            {
                var value = filter[Constants.JobMetadataInstanceId];
                Guid searchGuid;
                LanguagePrimitives.TryConvertTo(value, CultureInfo.InvariantCulture, out searchGuid);
                newlist = searchList.Where(job => job.InstanceId == searchGuid).ToList();
                searchList.Clear();
                searchList = newlist;
            }
            if (filter.ContainsKey(Constants.JobMetadataFilterState))
            {
                JobState searchState =
                    (JobState)
                    LanguagePrimitives.ConvertTo(filter[Constants.JobMetadataFilterState], typeof(JobState), CultureInfo.InvariantCulture);
                newlist = searchList.Where(job => job.JobStateInfo.State == searchState).ToList();
                searchList.Clear();
                searchList = newlist;
            }
            if (filter.ContainsKey(Constants.JobMetadataName))
            {
                Debug.Assert(filter[Constants.JobMetadataName] is string ||
                             filter[Constants.JobMetadataName] is WildcardPattern, "filter value should be a string or wildcard");

                WildcardPattern patternForName;
                if (filter[Constants.JobMetadataName] is string)
                {
                    string name = (string)filter[Constants.JobMetadataName];
                    patternForName = new WildcardPattern(name, WildcardOptions.IgnoreCase | WildcardOptions.Compiled);
                }
                else
                    patternForName = (WildcardPattern) filter[Constants.JobMetadataName];

                newlist =
                    searchList.Where(parentJob => patternForName.IsMatch(parentJob.Name)).ToList();
                searchList.Clear();
                searchList = newlist;
            }
            if (filter.ContainsKey(Constants.JobMetadataCommand))
            {
                Debug.Assert(filter[Constants.JobMetadataCommand] is string ||
                             filter[Constants.JobMetadataCommand] is WildcardPattern, "filter value should be a string or wildcard");

                WildcardPattern patternForCommand;
                if (filter[Constants.JobMetadataCommand] is string)
                {
                    string command = (string)filter[Constants.JobMetadataCommand];
                    patternForCommand = new WildcardPattern(command, WildcardOptions.IgnoreCase | WildcardOptions.Compiled);
                }
                else
                    patternForCommand = (WildcardPattern)filter[Constants.JobMetadataCommand];

                newlist =
                    searchList.Where(parentJob => patternForCommand.IsMatch(parentJob.Command)).ToList();
                searchList.Clear();
                searchList = newlist;
            }
            return searchList;
        }

        private void PopulateJobRepositoryIfRequired()
        {
            // we need to populate the repository the first time
            if (_repositoryPopulated) return;
            lock (_syncObject)
            {
                if (_repositoryPopulated) return;

                _repositoryPopulated = true;

                LoadWorkflowInstancesFromStore();

                // start the workflow manager - this will either
                // create the store if it doesn't exist or load
                // existing workflows
                foreach (var job in CreateJobsFromWorkflows(GetJobManager().GetJobs(), true))
                {
                    try
                    {
                        _jobRepository.Add((ContainerParentJob) job);
                    }
                    catch(ArgumentException)
                    {
                        // if the job was created using NewJob() then
                        // the repository already has it and an exception
                        // will be thrown. Ignore the exception in this case
                    }
                }
            }
        }

        private ICollection<PSWorkflowJob> GetChildJobsFromRepository()
        {
            return _jobRepository.GetItems().SelectMany(parentJob => parentJob.ChildJobs).Cast<PSWorkflowJob>().ToList();
        }

        private IEnumerable<Job2> CreateJobsFromWorkflows(IEnumerable<Job2> workflowJobs, bool returnParents)
        {
            // Jobs in this collection correspond to the ContainerParentJob objects. PSWorkflowJob objects
            // are children of these.
            var reconstructedParentJobs = new Dictionary<Guid, Job2>();
            var jobs = new List<Job2>();

            if (workflowJobs == null) return jobs;

            // If a workflow instance has incomplete metadata, we do not create the job for it.
            foreach (var job in workflowJobs)
            {
                var wfjob = job as PSWorkflowJob;
                Debug.Assert(wfjob != null, "Job supplied must be of type PSWorkflowJob");
                PSWorkflowInstance instance = wfjob.PSWorkflowInstance;
                Dbg.Assert(instance != null, "PSWorkflowInstance should be reconstructed before attempting to rehydrate job");

                if (!instance.JobStateRetrieved || instance.PSWorkflowContext.JobMetadata == null || instance.PSWorkflowContext.JobMetadata.Count == 0) continue;

                object data;
                string name, command;
                Guid instanceId;
                if (!GetJobInfoFromMetadata(instance, out command, out name, out instanceId)) continue;

                if (!instance.PSWorkflowContext.JobMetadata.TryGetValue(Constants.JobMetadataParentInstanceId, out data))
                    continue;
                var parentInstanceId = (Guid)data;

                // If the parent job is needed, find or create it now so that the ID is sequentially lower.
                if (returnParents && !reconstructedParentJobs.ContainsKey(parentInstanceId))
                {
                    if (!instance.PSWorkflowContext.JobMetadata.TryGetValue(Constants.JobMetadataParentName, out data))
                        continue;
                    var parentName = (string)data;

                    if (!instance.PSWorkflowContext.JobMetadata.TryGetValue(Constants.JobMetadataParentCommand, out data))
                        continue;
                    var parentCommand = (string)data;

                    JobIdentifier parentId = RetrieveJobIdForReuse(parentInstanceId);
                    ContainerParentJob parentJob = parentId != null
                                                       ? new ContainerParentJob(parentCommand, parentName, parentId, AdapterTypeName)
                                                       : new ContainerParentJob(parentCommand, parentName, parentInstanceId, AdapterTypeName);

                    // update job metadata with new parent session Id--needed for filtering.
                    // The pid in the metadata has already been updated at this point.
                    Dbg.Assert(
                        instance.PSWorkflowContext.JobMetadata.ContainsKey(Constants.JobMetadataParentSessionId),
                        "Job Metadata for instance incomplete.");
                    if (instance.PSWorkflowContext.JobMetadata.ContainsKey(Constants.JobMetadataParentSessionId))
                        instance.PSWorkflowContext.JobMetadata[Constants.JobMetadataParentSessionId] = parentJob.Id;

                    reconstructedParentJobs.Add(parentInstanceId, parentJob);
                }

                // update job metadata with new session Id--needed for filtering.
                Dbg.Assert(instance.PSWorkflowContext.JobMetadata.ContainsKey(Constants.JobMetadataSessionId), "Job Metadata for instance incomplete.");
                Dbg.Assert(instance.PSWorkflowContext.JobMetadata.ContainsKey(Constants.JobMetadataPid), "Job Metadata for instance incomplete.");
                if (instance.PSWorkflowContext.JobMetadata.ContainsKey(Constants.JobMetadataSessionId))
                    instance.PSWorkflowContext.JobMetadata[Constants.JobMetadataSessionId] = job.Id;
                if (instance.PSWorkflowContext.JobMetadata.ContainsKey(Constants.JobMetadataPid))
                    instance.PSWorkflowContext.JobMetadata[Constants.JobMetadataPid] = Process.GetCurrentProcess().Id;

                job.StartParameters = new List<CommandParameterCollection>();
                CommandParameterCollection commandParameterCollection = new CommandParameterCollection();
                AddStartParametersFromCollection(instance.PSWorkflowContext.WorkflowParameters, commandParameterCollection);
                AddStartParametersFromCollection(instance.PSWorkflowContext.PSWorkflowCommonParameters, commandParameterCollection);

                bool takesPSPrivateMetadata;
                if (instance.PSWorkflowContext.JobMetadata.ContainsKey(Constants.WorkflowTakesPrivateMetadata))
                {
                    takesPSPrivateMetadata = (bool)instance.PSWorkflowContext.JobMetadata[Constants.WorkflowTakesPrivateMetadata];
                }
                else
                {
                    DynamicActivity da = instance.PSWorkflowDefinition != null ? instance.PSWorkflowDefinition.Workflow as DynamicActivity : null;
                    takesPSPrivateMetadata = da != null && da.Properties.Contains(Constants.PrivateMetadata);
                }

                // If there is Private Metadata and it is not included in the "Input" collection, add it now.
                if (instance.PSWorkflowContext.PrivateMetadata != null
                    && instance.PSWorkflowContext.PrivateMetadata.Count > 0
                    && !takesPSPrivateMetadata)
                {
                    Hashtable privateMetadata = new Hashtable();
                    foreach (var pair in instance.PSWorkflowContext.PrivateMetadata)
                    {
                        privateMetadata.Add(pair.Key, pair.Value);
                    }
                    commandParameterCollection.Add(new CommandParameter(Constants.PrivateMetadata, privateMetadata));
                }
                job.StartParameters.Add(commandParameterCollection);

                if (returnParents)
                {
                    ((ContainerParentJob)reconstructedParentJobs[parentInstanceId]).AddChildJob(job);
                }
                else
                {
                    jobs.Add(job);
                }

                if (!wfjob.WorkflowInstanceLoaded)
                {
                    // RestoreFromWorkflowInstance sets the job state. Because we've used AddChildJob, the parent's state will be
                    // updated automatically.
                    wfjob.RestoreFromWorkflowInstance(instance);
                }
            }

            if (returnParents)
            {
                jobs.AddRange(reconstructedParentJobs.Values);
            }

            return jobs;
        }

        /// <summary>
        /// Handles the wsman server shutting down event.
        /// </summary>
        /// <param name="sender">sender of this event</param>
        /// <param name="e">arguments describing the event</param>
        private void OnWSManServerShuttingDownEventRaised(object sender, EventArgs e)
        {
            try
            {
                IsShutdownInProgress = true;
                PSWorkflowConfigurationProvider _configuration = (PSWorkflowConfigurationProvider)PSWorkflowRuntime.Instance.Configuration;
                int timeout = _configuration.WorkflowShutdownTimeoutMSec;

                GetJobManager().ShutdownWorkflowManager(timeout);
            }
            catch (Exception exception)
            {

                // when exception on shutdown, not much to do much other than logging the exception 
                _tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                                                   "Shutting down WSMan server: Exception details: {0}",
                                                   exception));

                Dbg.Assert(false, "Exception has happened during the shutdown API. [Message] " + exception.Message + "[StackTrace] " + exception.StackTrace);

            }
        }
        internal bool IsShutdownInProgress = false;

        private static void AddStartParametersFromCollection(Dictionary<string, object> collection, CommandParameterCollection allParams)
        {
            if (collection == null || collection.Count <= 0) return;
            foreach (var param in collection.Select(o => new CommandParameter(o.Key, o.Value)))
            {
                allParams.Add(param);
            }
        }

        #endregion Private Methods

        #region Constructors

        internal WorkflowJobSourceAdapter()
        {
            WSManServerChannelEvents.ShuttingDown += OnWSManServerShuttingDownEventRaised;
            Name = AdapterTypeName;
        }

        #endregion Constructors

        #region Internal Methods

        // called from test code to clean up
        internal void ClearRepository()
        {
            foreach (var item in _jobRepository.GetItems())
            {
                _jobRepository.Remove(item);
            }
        }

        internal static bool GetJobInfoFromMetadata(PSWorkflowInstance workflowInstance, out string command, out string name, out Guid instanceId)
        {
            bool retVal = false;
            command = string.Empty;
            name = string.Empty;
            instanceId = Guid.Empty;

            do
            {
                object data;
                if (!workflowInstance.PSWorkflowContext.JobMetadata.TryGetValue(Constants.JobMetadataName, out data)) break;
                name = (string)data;

                if (!workflowInstance.PSWorkflowContext.JobMetadata.TryGetValue(Constants.JobMetadataCommand, out data)) break;
                command = (string)data;

                if (!workflowInstance.PSWorkflowContext.JobMetadata.TryGetValue(Constants.JobMetadataInstanceId, out data)) break;
                instanceId = (Guid)data;

                retVal = true;
            } while (false);

            return retVal;
        }

        private bool _fullyLoaded;
        internal void LoadWorkflowInstancesFromStore()
        {
            if (_fullyLoaded) return;
            lock (_syncObject)
            {
                if (_fullyLoaded) return;

                foreach (PSWorkflowId storedInstanceId in PSWorkflowFileInstanceStore.GetAllWorkflowInstanceIds())
                {
                   try
                   {
                        GetJobManager().LoadJobWithIdentifier(storedInstanceId);
                   }
                   catch (Exception e)
                   {
                        // Auto loading failing so logging into the message trace

                        _tracer.WriteMessage("Getting an exception while loading the previously persisted workflows...");
                        _tracer.TraceException(e);
                        // Intentionally continuing on with exception
                   }
                }

                _fullyLoaded = true;
            }

            GetJobManager().CleanUpWorkflowJobTable();
        }

        /// <summary>
        /// Called from tests to cleanup instance table
        /// </summary>
        internal void ClearWorkflowTable()
        {
            // First clean up existing jobs
            GetJobManager().ClearWorkflowManagerInstanceTable();

            // Now force load all the jobs from disk
            _fullyLoaded = false;
            LoadWorkflowInstancesFromStore();

            // Now clean up the stuff loaded from disk
            GetJobManager().ClearWorkflowManagerInstanceTable();

        }

        #endregion Internal Methods
    }
}
