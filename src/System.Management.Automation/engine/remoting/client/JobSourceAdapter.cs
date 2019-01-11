// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.IO;
using System.Collections;
using System.Runtime.Serialization;

// Stops compiler from warning about unknown warnings
#pragma warning disable 1634, 1691

namespace System.Management.Automation
{
    /// <summary>
    /// Contains the definition of a job which is defined in a
    /// job store.
    /// </summary>
    /// <remarks>The actual implementation of this class will
    /// happen in M2</remarks>
    [Serializable]
    public class JobDefinition : ISerializable
    {
        private string _name;

        /// <summary>
        /// A friendly Name for this definition.
        /// </summary>
        public string Name
        {
            get { return _name; }

            set { _name = value; }
        }

        /// <summary>
        /// The type that derives from JobSourceAdapter
        /// that contains the logic for invocation and
        /// management of this type of job.
        /// </summary>
        public Type JobSourceAdapterType { get; }

        private string _moduleName;

        /// <summary>
        /// Module name for the module containing
        /// the source adapter implementation.
        /// </summary>
        public string ModuleName
        {
            get { return _moduleName; }

            set { _moduleName = value; }
        }

        private string _jobSourceAdapterTypeName;

        /// <summary>
        /// Job source adapter type name.
        /// </summary>
        public string JobSourceAdapterTypeName
        {
            get { return _jobSourceAdapterTypeName; }

            set { _jobSourceAdapterTypeName = value; }
        }

        /// <summary>
        /// Name of the job that needs to be loaded
        /// from the specified module.
        /// </summary>
        public string Command { get; }

        private Guid _instanceId;

        /// <summary>
        /// Unique Guid for this job definition.
        /// </summary>
        public Guid InstanceId
        {
            get
            {
                return _instanceId;
            }

            set
            {
                _instanceId = value;
            }
        }

        /// <summary>
        /// Save this definition to the specified
        /// file on disk.
        /// </summary>
        /// <param name="stream">Stream to save to.</param>
        public virtual void Save(Stream stream)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Load this definition from the specified
        /// file on disk.
        /// </summary>
        /// <param name="stream"></param>
        public virtual void Load(Stream stream)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns information about this job like
        /// name, definition, parameters etc.
        /// </summary>
        public CommandInfo CommandInfo
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Public constructor for testing.
        /// </summary>
        /// <param name="jobSourceAdapterType">Type of adapter to use to create a job.</param>
        /// <param name="command">The command string.</param>
        /// <param name="name">The job name.</param>
        public JobDefinition(Type jobSourceAdapterType, string command, string name)
        {
            JobSourceAdapterType = jobSourceAdapterType;
            if (jobSourceAdapterType != null)
            {
                _jobSourceAdapterTypeName = jobSourceAdapterType.Name;
            }

            Command = command;
            _name = name;
            _instanceId = Guid.NewGuid();
        }

        /// <summary>
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected JobDefinition(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Class that helps define the parameters to
    /// be passed to a job so that the job can be
    /// instantiated without having to specify
    /// the parameters explicitly. Helps in
    /// passing job parameters to disk.
    /// </summary>
    /// <remarks>This class is not required if
    /// CommandParameterCollection adds a public
    /// constructor.The actual implementation of
    /// this class will happen in M2</remarks>
    [Serializable]
    public class JobInvocationInfo : ISerializable
    {
        /// <summary>
        /// Friendly name associated with this specification.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (value == null)
                    throw new PSArgumentNullException("value");
                _name = value;
            }
        }

        private string _name = string.Empty;

        private string _command;

        /// <summary>
        /// Command string to execute.
        /// </summary>
        public string Command
        {
            get
            {
                return _command ?? _definition.Command;
            }

            set
            {
                _command = value;
            }
        }

        private JobDefinition _definition;

        /// <summary>
        /// Definition associated with the job.
        /// </summary>
        public JobDefinition Definition
        {
            get
            {
                return _definition;
            }

            set
            {
                _definition = value;
            }
        }

        private List<CommandParameterCollection> _parameters;

        /// <summary>
        /// Parameters associated with this specification.
        /// </summary>
        public List<CommandParameterCollection> Parameters
        {
            get { return _parameters ?? (_parameters = new List<CommandParameterCollection>()); }
        }

        /// <summary>
        /// Unique identifies for this specification.
        /// </summary>
        public Guid InstanceId { get; } = Guid.NewGuid();

        /// <summary>
        /// Save this specification to a file.
        /// </summary>
        /// <param name="stream">Stream to save to.</param>
        public virtual void Save(Stream stream)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Load this specification from a file.
        /// </summary>
        /// <param name="stream">Stream to load from.</param>
        public virtual void Load(Stream stream)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        protected JobInvocationInfo()
        { }

        /// <summary>
        /// Create a new job definition with a single set of parameters.
        /// </summary>
        /// <param name="definition">The job definition.</param>
        /// <param name="parameters">The parameter collection to use.</param>
        public JobInvocationInfo(JobDefinition definition, Dictionary<string, object> parameters)
        {
            _definition = definition;
            var convertedCollection = ConvertDictionaryToParameterCollection(parameters);
            if (convertedCollection != null)
            {
                Parameters.Add(convertedCollection);
            }
        }

        /// <summary>
        /// Create a new job definition with a multiple sets of parameters. This allows
        /// different parameters for different machines.
        /// </summary>
        /// <param name="definition">The job definition.</param>
        /// <param name="parameterCollectionList">Collection of sets of parameters to use for the child jobs.</param>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public JobInvocationInfo(JobDefinition definition, IEnumerable<Dictionary<string, object>> parameterCollectionList)
        {
            _definition = definition;
            if (parameterCollectionList == null) return;
            foreach (var parameterCollection in parameterCollectionList)
            {
                if (parameterCollection == null) continue;
                CommandParameterCollection convertedCollection = ConvertDictionaryToParameterCollection(parameterCollection);
                if (convertedCollection != null)
                {
                    Parameters.Add(convertedCollection);
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="definition"></param>
        /// <param name="parameters"></param>
        public JobInvocationInfo(JobDefinition definition, CommandParameterCollection parameters)
        {
            _definition = definition;
            Parameters.Add(parameters ?? new CommandParameterCollection());
        }

        /// <summary>
        /// </summary>
        /// <param name="definition"></param>
        /// <param name="parameters"></param>
        public JobInvocationInfo(JobDefinition definition, IEnumerable<CommandParameterCollection> parameters)
        {
            _definition = definition;
            if (parameters == null) return;
            foreach (var parameter in parameters)
            {
                Parameters.Add(parameter);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected JobInvocationInfo(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Utility function to turn a dictionary of name/value pairs into a parameter collection.
        /// </summary>
        /// <param name="parameters">The dictionary to convert.</param>
        /// <returns>The converted collection.</returns>
        private static CommandParameterCollection ConvertDictionaryToParameterCollection(IEnumerable<KeyValuePair<string, object>> parameters)
        {
            if (parameters == null)
                return null;
            CommandParameterCollection paramCollection = new CommandParameterCollection();
            foreach (CommandParameter paramItem in
                parameters.Select(param => new CommandParameter(param.Key, param.Value)))
            {
                paramCollection.Add(paramItem);
            }

            return paramCollection;
        }
    }

    /// <summary>
    /// Abstract class for a job store which will
    /// contain the jobs of a specific type.
    /// </summary>
    public abstract class JobSourceAdapter
    {
        /// <summary>
        /// Name for this store.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Get a token that allows for construction of a job with a previously assigned
        /// Id and InstanceId. This is only possible if this JobSourceAdapter is the
        /// creator of the original job.
        /// The original job must have been saved using "SaveJobIdForReconstruction"
        /// </summary>
        /// <param name="instanceId">Instance Id of the job to recreate.</param>
        /// <returns>JobIdentifier to be used in job construction.</returns>
        protected JobIdentifier RetrieveJobIdForReuse(Guid instanceId)
        {
            return JobManager.GetJobIdentifier(instanceId, this.GetType().Name);
        }

        /// <summary>
        /// Saves the Id information for a job so that it can be constructed at a later time.
        /// This will only allow this job source adapter type to recreate the job.
        /// </summary>
        /// <param name="job">The job whose id information to store.</param>
        /// <param name="recurse">Recurse to save child job Ids.</param>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Only jobs that derive from Job2 should have reusable IDs.")]
        public void StoreJobIdForReuse(Job2 job, bool recurse)
        {
            if (job == null)
            {
                PSTraceSource.NewArgumentNullException("job", RemotingErrorIdStrings.JobSourceAdapterCannotSaveNullJob);
            }

            JobManager.SaveJobId(job.InstanceId, job.Id, this.GetType().Name);
            if (recurse && job.ChildJobs != null && job.ChildJobs.Count > 0)
            {
                Hashtable duplicateDetector = new Hashtable();
                duplicateDetector.Add(job.InstanceId, job.InstanceId);
                foreach (Job child in job.ChildJobs)
                {
                    Job2 childJob = child as Job2;
                    if (childJob == null) continue;
                    StoreJobIdForReuseHelper(duplicateDetector, childJob, true);
                }
            }
        }

        private void StoreJobIdForReuseHelper(Hashtable duplicateDetector, Job2 job, bool recurse)
        {
            if (duplicateDetector.ContainsKey(job.InstanceId)) return;
            duplicateDetector.Add(job.InstanceId, job.InstanceId);

            JobManager.SaveJobId(job.InstanceId, job.Id, this.GetType().Name);

            if (!recurse || job.ChildJobs == null) return;
            foreach (Job child in job.ChildJobs)
            {
                Job2 childJob = child as Job2;
                if (childJob == null) continue;
                StoreJobIdForReuseHelper(duplicateDetector, childJob, recurse);
            }
        }

        /// <summary>
        /// Create a new job with the specified definition.
        /// </summary>
        /// <param name="definition">Job definition to use.</param>
        /// <returns>Job object.</returns>
        public Job2 NewJob(JobDefinition definition)
        {
            return NewJob(new JobInvocationInfo(definition, new Dictionary<string, object>()));
        }

        /// <summary>
        /// Creates a new job with the definition as specified by
        /// the provided definition name and path.  If path is null
        /// then a default location will be used to find the job
        /// definition by name.
        /// </summary>
        /// <param name="definitionName">Job definition name.</param>
        /// <param name="definitionPath">Job definition file path.</param>
        /// <returns>Job2 object.</returns>
        public virtual Job2 NewJob(string definitionName, string definitionPath)
        {
            return null;
        }

        /// <summary>
        /// Create a new job with the specified JobSpecification.
        /// </summary>
        /// <param name="specification">Specification.</param>
        /// <returns>Job object.</returns>
        public abstract Job2 NewJob(JobInvocationInfo specification);

        /// <summary>
        /// Get the list of jobs that are currently available in this
        /// store.
        /// </summary>
        /// <returns>Collection of job objects.</returns>
        public abstract IList<Job2> GetJobs();

        /// <summary>
        /// Get list of jobs that matches the specified names.
        /// </summary>
        /// <param name="name">names to match, can support
        ///   wildcard if the store supports</param>
        /// <param name="recurse"></param>
        /// <returns>Collection of jobs that match the specified
        /// criteria.</returns>
        public abstract IList<Job2> GetJobsByName(string name, bool recurse);

        /// <summary>
        /// Get list of jobs that run the specified command.
        /// </summary>
        /// <param name="command">Command to match.</param>
        /// <param name="recurse"></param>
        /// <returns>Collection of jobs that match the specified
        /// criteria.</returns>
        public abstract IList<Job2> GetJobsByCommand(string command, bool recurse);

        /// <summary>
        /// Get list of jobs that has the specified id.
        /// </summary>
        /// <param name="instanceId">Guid to match.</param>
        /// <param name="recurse"></param>
        /// <returns>Job with the specified guid.</returns>
        public abstract Job2 GetJobByInstanceId(Guid instanceId, bool recurse);

        /// <summary>
        /// Get job that has specific session id.
        /// </summary>
        /// <param name="id">Id to match.</param>
        /// <param name="recurse"></param>
        /// <returns>Job with the specified id.</returns>
        public abstract Job2 GetJobBySessionId(int id, bool recurse);

        /// <summary>
        /// Get list of jobs that are in the specified state.
        /// </summary>
        /// <param name="state">State to match.</param>
        /// <param name="recurse"></param>
        /// <returns>Collection of jobs with the specified
        /// state.</returns>
        public abstract IList<Job2> GetJobsByState(JobState state, bool recurse);

        /// <summary>
        /// Get list of jobs based on the adapter specific
        /// filter parameters.
        /// </summary>
        /// <param name="filter">dictionary containing name value
        ///   pairs for adapter specific filters</param>
        /// <param name="recurse"></param>
        /// <returns>Collection of jobs that match the
        /// specified criteria.</returns>
        public abstract IList<Job2> GetJobsByFilter(Dictionary<string, object> filter, bool recurse);

        /// <summary>
        /// Remove a job from the store.
        /// </summary>
        /// <param name="job">Job object to remove.</param>
        public abstract void RemoveJob(Job2 job);

        /// <summary>
        /// Saves the job to a persisted store.
        /// </summary>
        /// <param name="job">Job2 type job to persist.</param>
        public virtual void PersistJob(Job2 job)
        {
            // Implemented only if job needs to be told when to persist.
        }
    }
}
