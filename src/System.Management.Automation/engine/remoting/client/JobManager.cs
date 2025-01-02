// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Tracing;
using System.Reflection;
using System.Security;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

// Stops compiler from warning about unknown warnings
#pragma warning disable 1634, 1691

namespace System.Management.Automation
{
    /// <summary>
    /// Manager for JobSourceAdapters for invocation and management of specific Job types.
    /// </summary>
    public sealed class JobManager
    {
        private readonly PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();

        /// <summary>
        /// Collection of registered JobSourceAdapters.
        /// </summary>
        private readonly Dictionary<string, JobSourceAdapter> _sourceAdapters =
            new Dictionary<string, JobSourceAdapter>();

        private readonly object _syncObject = new object();

        /// <summary>
        /// Collection of job IDs that are valid for reuse.
        /// </summary>
        private static readonly Dictionary<Guid, KeyValuePair<int, string>> s_jobIdsForReuse = new Dictionary<Guid, KeyValuePair<int, string>>();

        private static readonly object s_syncObject = new object();

        /// <summary>
        /// Creates a JobManager instance.
        /// </summary>
        internal JobManager()
        {
        }

        /// <summary>
        /// Returns true if the type is already registered.
        /// </summary>
        /// <param name="typeName">Type to check.</param>
        /// <returns>Whether the type is registered already.</returns>
        public bool IsRegistered(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return false;
            }

            lock (_syncObject)
            {
                return _sourceAdapters.ContainsKey(typeName);
            }
        }

        /// <summary>
        /// Adds a new JobSourceAdapter to the JobManager instance.
        /// After addition, creating a NewJob with a JobDefinition
        /// indicating the JobSourceAdapter derivative type will function.
        /// </summary>
        /// <param name="jobSourceAdapterType">The derivative JobSourceAdapter type to
        /// register.</param>
        /// <exception cref="InvalidOperationException">Throws when there is no public
        /// default constructor on the type.</exception>
        internal void RegisterJobSourceAdapter(Type jobSourceAdapterType)
        {
            Dbg.Assert(typeof(JobSourceAdapter).IsAssignableFrom(jobSourceAdapterType), "BaseType of any type being registered with the JobManager should be JobSourceAdapter.");
            Dbg.Assert(jobSourceAdapterType != typeof(JobSourceAdapter), "JobSourceAdapter abstract type itself should never be registered.");
            Dbg.Assert(jobSourceAdapterType != null, "JobSourceAdapterType should never be called with null value.");
            object instance = null;

            ConstructorInfo constructor = jobSourceAdapterType.GetConstructor(Type.EmptyTypes);
            if (!constructor.IsPublic)
            {
                string message = string.Format(CultureInfo.CurrentCulture,
                                                RemotingErrorIdStrings.JobManagerRegistrationConstructorError,
                                                jobSourceAdapterType.FullName);
                throw new InvalidOperationException(message);
            }

            try
            {
                instance = constructor.Invoke(null);
            }
            catch (MemberAccessException exception)
            {
                _tracer.TraceException(exception);
                throw;
            }
            catch (TargetInvocationException exception)
            {
                _tracer.TraceException(exception);
                throw;
            }
            catch (TargetParameterCountException exception)
            {
                _tracer.TraceException(exception);
                throw;
            }
            catch (NotSupportedException exception)
            {
                _tracer.TraceException(exception);
                throw;
            }
            catch (SecurityException exception)
            {
                _tracer.TraceException(exception);
                throw;
            }

            if (instance != null)
            {
                lock (_syncObject)
                {
                    _sourceAdapters.Add(jobSourceAdapterType.Name, (JobSourceAdapter)instance);
                }
            }
        }

        /// <summary>
        /// Returns a token that allows a job to be constructed with a specific id and instanceId.
        /// The original job must have been saved using "SaveJobIdForReconstruction" in the JobSourceAdapter.
        /// </summary>
        /// <param name="instanceId">The instance id desired.</param>
        /// <param name="typeName">The requesting type name for JobSourceAdapter implementation.</param>
        /// <returns>Token for job creation.</returns>
        internal static JobIdentifier GetJobIdentifier(Guid instanceId, string typeName)
        {
            lock (s_syncObject)
            {
                KeyValuePair<int, string> keyValuePair;
                if (s_jobIdsForReuse.TryGetValue(instanceId, out keyValuePair) && keyValuePair.Value.Equals(typeName))
                    return new JobIdentifier(keyValuePair.Key, instanceId);
                return null;
            }
        }

        /// <summary>
        /// Saves the Id information for a job so that it can be constructed at a later time by a JobSourceAdapter
        /// with the same type.
        /// </summary>
        /// <param name="instanceId">The instance id to save.</param>
        /// <param name="id">The session specific id to save.</param>
        /// <param name="typeName">The type name for the JobSourceAdapter implementation doing the save.</param>
        internal static void SaveJobId(Guid instanceId, int id, string typeName)
        {
            lock (s_syncObject)
            {
                if (s_jobIdsForReuse.ContainsKey(instanceId))
                {
                    return;
                }

                s_jobIdsForReuse.Add(instanceId, new KeyValuePair<int, string>(id, typeName));
            }
        }

        #region NewJob

        /// <summary>
        /// Creates a new job of the appropriate type given by JobDefinition passed in.
        /// </summary>
        /// <param name="definition">JobDefinition defining the command.</param>
        /// <returns>Job2 object of the appropriate type specified by the definition.</returns>
        /// <exception cref="InvalidOperationException">If JobSourceAdapter type specified
        /// in definition is not registered.</exception>
        /// <exception cref="Exception">JobSourceAdapter implementation exception thrown on error.
        /// </exception>
        public Job2 NewJob(JobDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);

            JobSourceAdapter sourceAdapter = GetJobSourceAdapter(definition);
            Job2 newJob;

#pragma warning disable 56500
            try
            {
                newJob = sourceAdapter.NewJob(definition);
            }
            catch (Exception exception)
            {
                // Since we are calling into 3rd party code
                // catching Exception is allowed. In all
                // other cases the appropriate exception
                // needs to be caught.

                // sourceAdapter.NewJob returned unknown error.
                _tracer.TraceException(exception);
                throw;
            }
#pragma warning restore 56500

            return newJob;
        }

        /// <summary>
        /// Creates a new job of the appropriate type given by JobDefinition passed in.
        /// </summary>
        /// <param name="specification">JobInvocationInfo defining the command.</param>
        /// <returns>Job2 object of the appropriate type specified by the definition.</returns>
        /// <exception cref="InvalidOperationException">If JobSourceAdapter type specified
        /// in definition is not registered.</exception>
        /// <exception cref="Exception">JobSourceAdapter implementation exception thrown on error.
        /// </exception>
        public Job2 NewJob(JobInvocationInfo specification)
        {
            ArgumentNullException.ThrowIfNull(specification);

            if (specification.Definition == null)
            {
                throw new ArgumentException(RemotingErrorIdStrings.NewJobSpecificationError, nameof(specification));
            }

            JobSourceAdapter sourceAdapter = GetJobSourceAdapter(specification.Definition);
            Job2 newJob = null;

#pragma warning disable 56500
            try
            {
                newJob = sourceAdapter.NewJob(specification);
            }
            catch (Exception exception)
            {
                // Since we are calling into 3rd party code
                // catching Exception is allowed. In all
                // other cases the appropriate exception
                // needs to be caught.

                // sourceAdapter.NewJob returned unknown error.
                _tracer.TraceException(exception);
                throw;
            }
#pragma warning restore 56500

            return newJob;
        }

        #endregion NewJob

        #region Persist Job

        /// <summary>
        /// Saves the job to a persisted store.
        /// </summary>
        /// <param name="job">Job2 type job to persist.</param>
        /// <param name="definition">Job definition containing source adapter information.</param>
        public void PersistJob(Job2 job, JobDefinition definition)
        {
            if (job == null)
            {
                throw new PSArgumentNullException(nameof(job));
            }

            if (definition == null)
            {
                throw new PSArgumentNullException(nameof(definition));
            }

            JobSourceAdapter sourceAdapter = GetJobSourceAdapter(definition);

            try
            {
                sourceAdapter.PersistJob(job);
            }
            catch (Exception exception)
            {
                // Since we are calling into 3rd party code
                // catching Exception is allowed. In all
                // other cases the appropriate exception
                // needs to be caught.

                // sourceAdapter.NewJob returned unknown error.
                _tracer.TraceException(exception);
                throw;
            }
        }

        #endregion

        /// <summary>
        /// Helper method, finds source adapter if registered, otherwise throws
        /// an InvalidOperationException.
        /// </summary>
        /// <param name="adapterTypeName">The name of the JobSourceAdapter derivative desired.</param>
        /// <returns>The JobSourceAdapter instance.</returns>
        /// <exception cref="InvalidOperationException">If JobSourceAdapter type specified
        /// is not found.</exception>
        private JobSourceAdapter AssertAndReturnJobSourceAdapter(string adapterTypeName)
        {
            JobSourceAdapter adapter;
            lock (_syncObject)
            {
                if (!_sourceAdapters.TryGetValue(adapterTypeName, out adapter))
                {
                    throw new InvalidOperationException(RemotingErrorIdStrings.JobSourceAdapterNotFound);
                }
            }

            return adapter;
        }

        /// <summary>
        /// Helper method to find and return the job source adapter if currently loaded or
        /// otherwise load the associated module and the requested source adapter.
        /// </summary>
        /// <param name="definition">JobDefinition supplies the JobSourceAdapter information.</param>
        /// <returns>JobSourceAdapter.</returns>
        private JobSourceAdapter GetJobSourceAdapter(JobDefinition definition)
        {
            string adapterTypeName;
            if (!string.IsNullOrEmpty(definition.JobSourceAdapterTypeName))
            {
                adapterTypeName = definition.JobSourceAdapterTypeName;
            }
            else if (definition.JobSourceAdapterType != null)
            {
                adapterTypeName = definition.JobSourceAdapterType.Name;
            }
            else
            {
                throw new InvalidOperationException(RemotingErrorIdStrings.JobSourceAdapterNotFound);
            }

            JobSourceAdapter adapter;
            bool adapterFound = false;
            lock (_syncObject)
            {
                adapterFound = _sourceAdapters.TryGetValue(adapterTypeName, out adapter);
            }

            if (!adapterFound)
            {
                if (!string.IsNullOrEmpty(definition.ModuleName))
                {
                    // Attempt to load the module.
                    Exception ex = null;
                    try
                    {
                        InitialSessionState iss = InitialSessionState.CreateDefault2();
                        iss.Commands.Clear();
                        iss.Formats.Clear();
                        iss.Commands.Add(new SessionStateCmdletEntry("Import-Module", typeof(Microsoft.PowerShell.Commands.ImportModuleCommand), null));
                        using (PowerShell powerShell = PowerShell.Create(iss))
                        {
                            powerShell.AddCommand("Import-Module");
                            powerShell.AddParameter("Name", definition.ModuleName);
                            powerShell.Invoke();

                            if (powerShell.ErrorBuffer.Count > 0)
                            {
                                ex = powerShell.ErrorBuffer[0].Exception;
                            }
                        }
                    }
                    catch (RuntimeException e)
                    {
                        ex = e;
                    }
                    catch (InvalidOperationException e)
                    {
                        ex = e;
                    }
                    catch (ScriptCallDepthException e)
                    {
                        ex = e;
                    }
                    catch (SecurityException e)
                    {
                        ex = e;
                    }

                    if (ex != null)
                    {
                        throw new InvalidOperationException(RemotingErrorIdStrings.JobSourceAdapterNotFound, ex);
                    }

                    // Now try getting the job source adapter again.
                    adapter = AssertAndReturnJobSourceAdapter(adapterTypeName);
                }
                else
                {
                    throw new InvalidOperationException(RemotingErrorIdStrings.JobSourceAdapterNotFound);
                }
            }

            return adapter;
        }

        #region GetJobs

        /// <summary>
        /// Get list of all jobs.
        /// </summary>
        /// <param name="cmdlet">Cmdlet requesting this, for error processing.</param>
        /// <param name="writeErrorOnException"></param>
        /// <param name="writeObject"></param>
        /// <param name="jobSourceAdapterTypes">Job source adapter type names.</param>
        /// <returns>Collection of jobs.</returns>
        /// <exception cref="Exception">If cmdlet parameter is null, throws exception on error from
        /// JobSourceAdapter implementation.</exception>
        internal List<Job2> GetJobs(
            Cmdlet cmdlet,
            bool writeErrorOnException,
            bool writeObject,
            string[] jobSourceAdapterTypes)
        {
            return GetFilteredJobs(null, FilterType.None, cmdlet, writeErrorOnException, writeObject, false, jobSourceAdapterTypes);
        }

        /// <summary>
        /// Get list of jobs that matches the specified names.
        /// </summary>
        /// <param name="name">Names to match, can support
        ///   wildcard if the store supports.</param>
        /// <param name="cmdlet">Cmdlet requesting this, for error processing.</param>
        /// <param name="writeErrorOnException"></param>
        /// <param name="writeObject"></param>
        /// <param name="recurse"></param>
        /// <param name="jobSourceAdapterTypes">Job source adapter type names.</param>
        /// <returns>Collection of jobs that match the specified
        /// criteria.</returns>
        /// <exception cref="Exception">If cmdlet parameter is null, throws exception on error from
        /// JobSourceAdapter implementation.</exception>
        internal List<Job2> GetJobsByName(
            string name,
            Cmdlet cmdlet,
            bool writeErrorOnException,
            bool writeObject,
            bool recurse,
            string[] jobSourceAdapterTypes)
        {
            return GetFilteredJobs(name, FilterType.Name, cmdlet, writeErrorOnException, writeObject, recurse, jobSourceAdapterTypes);
        }

        /// <summary>
        /// Get list of jobs that run the specified command.
        /// </summary>
        /// <param name="command">Command to match.</param>
        /// <param name="cmdlet">Cmdlet requesting this, for error processing.</param>
        /// <param name="writeErrorOnException"></param>
        /// <param name="writeObject"></param>
        /// <param name="recurse"></param>
        /// <param name="jobSourceAdapterTypes">Job source adapter type names.</param>
        /// <returns>Collection of jobs that match the specified
        /// criteria.</returns>
        /// <exception cref="Exception">If cmdlet parameter is null, throws exception on error from
        /// JobSourceAdapter implementation.</exception>
        internal List<Job2> GetJobsByCommand(
            string command,
            Cmdlet cmdlet,
            bool writeErrorOnException,
            bool writeObject,
            bool recurse,
            string[] jobSourceAdapterTypes)
        {
            return GetFilteredJobs(command, FilterType.Command, cmdlet, writeErrorOnException, writeObject, recurse, jobSourceAdapterTypes);
        }

        /// <summary>
        /// Get list of jobs that are in the specified state.
        /// </summary>
        /// <param name="state">State to match.</param>
        /// <param name="cmdlet">Cmdlet requesting this, for error processing.</param>
        /// <param name="writeErrorOnException"></param>
        /// <param name="writeObject"></param>
        /// <param name="recurse"></param>
        /// <param name="jobSourceAdapterTypes">Job source adapter type names.</param>
        /// <returns>Collection of jobs with the specified
        /// state.</returns>
        /// <exception cref="Exception">If cmdlet parameter is null, throws exception on error from
        /// JobSourceAdapter implementation.</exception>
        internal List<Job2> GetJobsByState(
            JobState state,
            Cmdlet cmdlet,
            bool writeErrorOnException,
            bool writeObject,
            bool recurse,
            string[] jobSourceAdapterTypes)
        {
            return GetFilteredJobs(state, FilterType.State, cmdlet, writeErrorOnException, writeObject, recurse, jobSourceAdapterTypes);
        }

        /// <summary>
        /// Get list of jobs based on the adapter specific
        /// filter parameters.
        /// </summary>
        /// <param name="filter">Dictionary containing name value
        ///   pairs for adapter specific filters.</param>
        /// <param name="cmdlet">Cmdlet requesting this, for error processing.</param>
        /// <param name="writeErrorOnException"></param>
        /// <param name="writeObject"></param>
        /// <param name="recurse"></param>
        /// <returns>Collection of jobs that match the
        /// specified criteria.</returns>
        /// <exception cref="Exception">If cmdlet parameter is null, throws exception on error from
        /// JobSourceAdapter implementation.</exception>
        internal List<Job2> GetJobsByFilter(Dictionary<string, object> filter, Cmdlet cmdlet, bool writeErrorOnException, bool writeObject, bool recurse)
        {
            return GetFilteredJobs(filter, FilterType.Filter, cmdlet, writeErrorOnException, writeObject, recurse, null);
        }

        /// <summary>
        /// Get a filtered list of jobs based on adapter name.
        /// </summary>
        /// <param name="id">Job id.</param>
        /// <param name="name">Adapter name.</param>
        /// <returns></returns>
        internal bool IsJobFromAdapter(Guid id, string name)
        {
            lock (_syncObject)
            {
                foreach (JobSourceAdapter sourceAdapter in _sourceAdapters.Values)
                {
                    if (sourceAdapter.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return (sourceAdapter.GetJobByInstanceId(id, false) != null);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Get a filtered list of jobs based on filter type.
        /// </summary>
        /// <param name="filter">Object to use for filtering.</param>
        /// <param name="filterType">Type of filter, specifies which "get" from
        ///   JobSourceAdapter to call, and dictates the type for filter.</param>
        /// <param name="cmdlet">Cmdlet requesting this, for error processing.</param>
        /// <param name="writeErrorOnException"></param>
        /// <param name="writeObject"></param>
        /// <param name="recurse"></param>
        /// <param name="jobSourceAdapterTypes">Job source adapter type names.</param>
        /// <returns>Filtered list of jobs.</returns>
        /// <exception cref="Exception">If cmdlet parameter is null, throws exception on error from
        /// JobSourceAdapter implementation.</exception>
        private List<Job2> GetFilteredJobs(
            object filter,
            FilterType filterType,
            Cmdlet cmdlet,
            bool writeErrorOnException,
            bool writeObject,
            bool recurse,
            string[] jobSourceAdapterTypes)
        {
            Diagnostics.Assert(cmdlet != null, "Cmdlet should be passed to JobManager");

            List<Job2> allJobs = new List<Job2>();

            lock (_syncObject)
            {
                foreach (JobSourceAdapter sourceAdapter in _sourceAdapters.Values)
                {
                    List<Job2> jobs = null;

                    // Filter search based on job source adapter types if provided.
                    if (!CheckTypeNames(sourceAdapter, jobSourceAdapterTypes))
                    {
                        continue;
                    }

#pragma warning disable 56500
                    try
                    {
                        jobs = CallJobFilter(sourceAdapter, filter, filterType, recurse);
                    }
                    catch (Exception exception)
                    {
                        // Since we are calling into 3rd party code
                        // catching Exception is allowed. In all
                        // other cases the appropriate exception
                        // needs to be caught.

                        // sourceAdapter.GetJobsByFilter() threw unknown exception.
                        _tracer.TraceException(exception);
                        WriteErrorOrWarning(writeErrorOnException, cmdlet, exception, "JobSourceAdapterGetJobsError", sourceAdapter);
                    }
#pragma warning restore 56500

                    if (jobs == null)
                    {
                        continue;
                    }

                    allJobs.AddRange(jobs);
                }
            }

            if (writeObject)
            {
                foreach (Job2 job in allJobs)
                {
                    cmdlet.WriteObject(job);
                }
            }

            return allJobs;
        }

        /// <summary>
        /// Compare sourceAdapter name with the provided source adapter type
        /// name list.
        /// </summary>
        /// <param name="sourceAdapter"></param>
        /// <param name="jobSourceAdapterTypes"></param>
        /// <returns></returns>
        private static bool CheckTypeNames(JobSourceAdapter sourceAdapter, string[] jobSourceAdapterTypes)
        {
            // If no type names were specified then allow all adapter types.
            if (jobSourceAdapterTypes == null ||
                jobSourceAdapterTypes.Length == 0)
            {
                return true;
            }

            string sourceAdapterName = GetAdapterName(sourceAdapter);
            Diagnostics.Assert(sourceAdapterName != null, "Source adapter should have name or type.");

            // Look for name match allowing wildcards.
            foreach (string typeName in jobSourceAdapterTypes)
            {
                WildcardPattern typeNamePattern = WildcardPattern.Get(typeName, WildcardOptions.IgnoreCase);
                if (typeNamePattern.IsMatch(sourceAdapterName))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetAdapterName(JobSourceAdapter sourceAdapter)
        {
            return (!string.IsNullOrEmpty(sourceAdapter.Name) ?
                sourceAdapter.Name :
                sourceAdapter.GetType().ToString());
        }

        /// <summary>
        /// Gets a filtered list of jobs from the given JobSourceAdapter.
        /// </summary>
        /// <param name="sourceAdapter">JobSourceAdapter to query.</param>
        /// <param name="filter">Filter object.</param>
        /// <param name="filterType">Filter type.</param>
        /// <param name="recurse"></param>
        /// <returns>List of jobs from sourceAdapter filtered on filterType.</returns>
        /// <exception cref="Exception">Throws exception on error from JobSourceAdapter
        /// implementation.</exception>
        private static List<Job2> CallJobFilter(JobSourceAdapter sourceAdapter, object filter, FilterType filterType, bool recurse)
        {
            List<Job2> jobs = new List<Job2>();
            IList<Job2> matches;

            switch (filterType)
            {
                case FilterType.Command:

                    matches = sourceAdapter.GetJobsByCommand((string)filter, recurse);
                    break;
                case FilterType.Filter:
                    matches = sourceAdapter.GetJobsByFilter((Dictionary<string, object>)filter, recurse);
                    break;
                case FilterType.Name:
                    matches = sourceAdapter.GetJobsByName((string)filter, recurse);
                    break;
                case FilterType.State:
                    matches = sourceAdapter.GetJobsByState((JobState)filter, recurse);
                    break;
                case FilterType.None:
                default:
                    matches = sourceAdapter.GetJobs();
                    break;
            }

            if (matches != null)
            {
                jobs.AddRange(matches);
            }

            return jobs;
        }

        /// <summary>
        /// Get job specified by the session specific id provided.
        /// </summary>
        /// <param name="id">Session specific job id.</param>
        /// <param name="cmdlet">Cmdlet requesting this, for error processing.</param>
        /// <param name="writeErrorOnException"></param>
        /// <param name="writeObject"></param>
        /// <param name="recurse"></param>
        /// <returns>Job that match the specified criteria.</returns>
        /// <exception cref="Exception">If cmdlet parameter is null, throws exception on error from
        /// JobSourceAdapter implementation.</exception>
        internal Job2 GetJobById(int id, Cmdlet cmdlet, bool writeErrorOnException, bool writeObject, bool recurse)
        {
            return GetJobThroughId<int>(Guid.Empty, id, cmdlet, writeErrorOnException, writeObject, recurse);
        }

        /// <summary>
        /// Get job that has the specified id.
        /// </summary>
        /// <param name="instanceId">Guid to match.</param>
        /// <param name="cmdlet">Cmdlet requesting this, for error processing.</param>
        /// <param name="writeErrorOnException"></param>
        /// <param name="writeObject"></param>
        /// <param name="recurse"></param>
        /// <returns>Job with the specified guid.</returns>
        /// <exception cref="Exception">If cmdlet parameter is null, throws exception on error from
        /// JobSourceAdapter implementation.</exception>
        internal Job2 GetJobByInstanceId(Guid instanceId, Cmdlet cmdlet, bool writeErrorOnException, bool writeObject, bool recurse)
        {
            return GetJobThroughId<Guid>(instanceId, 0, cmdlet, writeErrorOnException, writeObject, recurse);
        }

        private Job2 GetJobThroughId<T>(Guid guid, int id, Cmdlet cmdlet, bool writeErrorOnException, bool writeObject, bool recurse)
        {
            Diagnostics.Assert(cmdlet != null, "Cmdlet should always be passed to JobManager");
            Job2 job = null;
            lock (_syncObject)
            {
                foreach (JobSourceAdapter sourceAdapter in _sourceAdapters.Values)
                {
                    try
                    {
                        if (typeof(T) == typeof(Guid))
                        {
                            Diagnostics.Assert(id == 0, "id must be zero when invoked with guid");
                            job = sourceAdapter.GetJobByInstanceId(guid, recurse);
                        }
                        else if (typeof(T) == typeof(int))
                        {
                            Diagnostics.Assert(guid == Guid.Empty, "Guid must be empty when used with int");
                            job = sourceAdapter.GetJobBySessionId(id, recurse);
                        }
                    }
                    catch (Exception exception)
                    {
                        // Since we are calling into 3rd party code
                        // catching Exception is allowed. In all
                        // other cases the appropriate exception
                        // needs to be caught.

                        // sourceAdapter.GetJobByInstanceId threw unknown exception.
                        _tracer.TraceException(exception);

                        WriteErrorOrWarning(writeErrorOnException, cmdlet, exception, "JobSourceAdapterGetJobByInstanceIdError", sourceAdapter);
                    }

                    if (job == null)
                    {
                        continue;
                    }

                    if (writeObject)
                    {
                        cmdlet.WriteObject(job);
                    }

                    return job;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets or creates a Job2 object with the given definition name, path
        /// and definition type if specified, that can be run via the StartJob()
        /// method.
        /// </summary>
        /// <param name="definitionName">Job definition name.</param>
        /// <param name="definitionPath">Job definition file path.</param>
        /// <param name="definitionType">JobSourceAdapter type that contains the job definition.</param>
        /// <param name="cmdlet">Cmdlet making call.</param>
        /// <param name="writeErrorOnException">Whether to write jobsourceadapter errors.</param>
        /// <returns>List of matching Job2 objects.</returns>
        internal List<Job2> GetJobToStart(
            string definitionName,
            string definitionPath,
            string definitionType,
            Cmdlet cmdlet,
            bool writeErrorOnException)
        {
            List<Job2> jobs = new List<Job2>();
            WildcardPattern typeNamePattern = (definitionType != null) ?
                WildcardPattern.Get(definitionType, WildcardOptions.IgnoreCase) : null;

            lock (_syncObject)
            {
                foreach (JobSourceAdapter sourceAdapter in _sourceAdapters.Values)
                {
                    try
                    {
                        if (typeNamePattern != null)
                        {
                            string sourceAdapterName = GetAdapterName(sourceAdapter);
                            if (!typeNamePattern.IsMatch(sourceAdapterName))
                            {
                                continue;
                            }
                        }

                        Job2 job = sourceAdapter.NewJob(definitionName, definitionPath);
                        if (job != null)
                        {
                            jobs.Add(job);
                        }

                        if (typeNamePattern != null)
                        {
                            // Adapter type found, can quit.
                            break;
                        }
                    }
                    catch (Exception exception)
                    {
                        // Since we are calling into 3rd party code
                        // catching Exception is allowed. In all
                        // other cases the appropriate exception
                        // needs to be caught.

                        _tracer.TraceException(exception);

                        WriteErrorOrWarning(writeErrorOnException, cmdlet, exception, "JobSourceAdapterGetJobByInstanceIdError", sourceAdapter);
                    }
                }
            }

            return jobs;
        }

        private static void WriteErrorOrWarning(bool writeErrorOnException, Cmdlet cmdlet, Exception exception, string identifier, JobSourceAdapter sourceAdapter)
        {
            try
            {
                if (writeErrorOnException)
                {
                    cmdlet.WriteError(new ErrorRecord(exception, identifier, ErrorCategory.OpenError, sourceAdapter));
                }
                else
                {
                    // Write a warning
                    string message = string.Format(CultureInfo.CurrentCulture,
                                                   RemotingErrorIdStrings.JobSourceAdapterError,
                                                   exception.Message,
                                                   sourceAdapter.Name);
                    cmdlet.WriteWarning(message);
                }
            }
            catch (Exception)
            {
                // if this call is not made from a cmdlet thread or if
                // the cmdlet is closed this will thrown an exception
                // it is fine to eat that exception
            }
        }

        /// <summary>
        /// Returns a List of adapter names currently loaded.
        /// </summary>
        /// <param name="adapterTypeNames">Adapter names to filter on.</param>
        /// <returns>List of names.</returns>
        internal List<string> GetLoadedAdapterNames(string[] adapterTypeNames)
        {
            List<string> adapterNames = new List<string>();
            lock (_syncObject)
            {
                foreach (JobSourceAdapter sourceAdapter in _sourceAdapters.Values)
                {
                    if (CheckTypeNames(sourceAdapter, adapterTypeNames))
                    {
                        adapterNames.Add(GetAdapterName(sourceAdapter));
                    }
                }
            }

            return adapterNames;
        }

        #endregion GetJobs

        #region RemoveJob

        /// <summary>
        /// Remove a job from the appropriate store.
        /// </summary>
        /// <param name="sessionJobId">Session specific Job ID to remove.</param>
        /// <param name="cmdlet"></param>
        /// <param name="writeErrorOnException"></param>
        internal void RemoveJob(int sessionJobId, Cmdlet cmdlet, bool writeErrorOnException)
        {
            Job2 job = GetJobById(sessionJobId, cmdlet, writeErrorOnException, false, false);
            RemoveJob(job, cmdlet, false);
        }

        /// <summary>
        /// Remove a job from the appropriate store.
        /// </summary>
        /// <param name="job">Job object to remove.</param>
        /// <param name="cmdlet"></param>
        /// <param name="writeErrorOnException"></param>
        /// <param name="throwExceptions">If true, will throw all JobSourceAdapter exceptions to caller.
        /// This is needed if RemoveJob is being called from an event handler in Receive-Job.</param>
        /// <returns>True if job is found.</returns>
        internal bool RemoveJob(Job2 job, Cmdlet cmdlet, bool writeErrorOnException, bool throwExceptions = false)
        {
            bool jobFound = false;

            lock (_syncObject)
            {
                foreach (JobSourceAdapter sourceAdapter in _sourceAdapters.Values)
                {
                    Job2 foundJob = null;
#pragma warning disable 56500
                    try
                    {
                        foundJob = sourceAdapter.GetJobByInstanceId(job.InstanceId, true);
                    }
                    catch (Exception exception)
                    {
                        // Since we are calling into 3rd party code
                        // catching Exception is allowed. In all
                        // other cases the appropriate exception
                        // needs to be caught.

                        // sourceAdapter.GetJobByInstanceId() threw unknown exception.
                        _tracer.TraceException(exception);
                        if (throwExceptions)
                        {
                            throw;
                        }

                        WriteErrorOrWarning(writeErrorOnException, cmdlet, exception, "JobSourceAdapterGetJobError", sourceAdapter);
                    }
#pragma warning restore 56500

                    if (foundJob == null)
                    {
                        continue;
                    }

                    jobFound = true;
                    RemoveJobIdForReuse(foundJob);

#pragma warning disable 56500
                    try
                    {
                        sourceAdapter.RemoveJob(job);
                    }
                    catch (Exception exception)
                    {
                        // Since we are calling into 3rd party code
                        // catching Exception is allowed. In all
                        // other cases the appropriate exception
                        // needs to be caught.
                        // sourceAdapter.RemoveJob() threw unknown exception.

                        _tracer.TraceException(exception);
                        if (throwExceptions)
                        {
                            throw;
                        }

                        WriteErrorOrWarning(writeErrorOnException, cmdlet, exception, "JobSourceAdapterRemoveJobError", sourceAdapter);
                    }
#pragma warning restore 56500
                }
            }

            if (!jobFound && throwExceptions)
            {
                var message = PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.ItemNotFoundInRepository,
                            "Job repository", job.InstanceId.ToString());
                throw new ArgumentException(message);
            }

            return jobFound;
        }

        private void RemoveJobIdForReuse(Job job)
        {
            Hashtable duplicateDetector = new Hashtable();
            duplicateDetector.Add(job.Id, job.Id);

            RemoveJobIdForReuseHelper(duplicateDetector, job);
        }

        private void RemoveJobIdForReuseHelper(Hashtable duplicateDetector, Job job)
        {
            lock (s_syncObject)
            {
                s_jobIdsForReuse.Remove(job.InstanceId);
            }

            foreach (Job child in job.ChildJobs)
            {
                if (duplicateDetector.ContainsKey(child.Id))
                {
                    continue;
                }

                duplicateDetector.Add(child.Id, child.Id);

                RemoveJobIdForReuse(child);
            }
        }

        #endregion RemoveJob

        /// <summary>
        /// Filters available for GetJob, used internally to centralize Exception handling.
        /// </summary>
        private enum FilterType
        {
            /// <summary>
            /// Use no filter.
            /// </summary>
            None,

            /// <summary>
            /// Filter on command (string).
            /// </summary>
            Command,

            /// <summary>
            /// Filter on custom dictionary (dictionary(string, object)).
            /// </summary>
            Filter,

            /// <summary>
            /// Filter on name (string).
            /// </summary>
            Name,

            /// <summary>
            /// Filter on job state (JobState).
            /// </summary>
            State
        }
    }
}
