// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation.Remoting;

namespace System.Management.Automation
{
    /// <summary>
    /// Class which has list of job objects currently active in the system.
    /// </summary>
    public abstract class Repository<T> where T : class
    {
        #region Public Methods

        /// <summary>
        /// Add an item to the repository.
        /// </summary>
        /// <param name="item">Object to add.</param>
        public void Add(T item)
        {
            ArgumentNullException.ThrowIfNull(item, _identifier);

            lock (_syncObject)
            {
                Guid instanceId = GetKey(item);

                if (!_repository.ContainsKey(instanceId))
                {
                    _repository.Add(instanceId, item);
                }
                else
                {
                    throw new ArgumentException(_identifier);
                }
            }
        }

        /// <summary>
        /// Remove the specified item from the repository.
        /// </summary>
        /// <param name="item">Object to remove.</param>
        public void Remove(T item)
        {
            ArgumentNullException.ThrowIfNull(item, _identifier);

            lock (_syncObject)
            {
                Guid instanceId = GetKey(item);

                if (!_repository.Remove(instanceId))
                {
                    string message =
                        PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.ItemNotFoundInRepository,
                            "Job repository", instanceId.ToString());

                    throw new ArgumentException(message);
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public List<T> GetItems()
        {
            return Items;
        }
        #endregion Public Methods

        #region Private/Internal Methods

        /// <summary>
        /// Get a key for the specified item.
        /// </summary>
        /// <param name="item">Item for which the key is required.</param>
        /// <returns>Returns a key.</returns>
        protected abstract Guid GetKey(T item);

        /// <summary>
        /// Internal constructor.
        /// </summary>
        protected Repository(string identifier)
        {
            _identifier = identifier;
        }

        /// <summary>
        /// Creates a repository with the specified values.
        /// </summary>
        internal List<T> Items
        {
            get
            {
                lock (_syncObject)
                {
                    return new List<T>(_repository.Values);
                }
            }
        }

        /// <summary>
        /// Gets the specified Item.
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        public T GetItem(Guid instanceId)
        {
            lock (_syncObject)
            {
                T result;
                _repository.TryGetValue(instanceId, out result);
                return result;
            }
        }

        /// <summary>
        /// Gets the Repository dictionary.
        /// </summary>
        internal Dictionary<Guid, T> Dictionary
        {
            get { return _repository; }
        }

        #endregion Private Methods

        #region Private Members

        private readonly Dictionary<Guid, T> _repository = new Dictionary<Guid, T>();
        private readonly object _syncObject = new object();      // object for synchronization
        private readonly string _identifier;

        #endregion Private Members
    }

    /// <summary>
    /// Class which has list of job objects currently active in the system.
    /// </summary>
    public class JobRepository : Repository<Job>
    {
        /// <summary>
        /// Returns the list of available job objects.
        /// </summary>
        public List<Job> Jobs
        {
            get
            {
                return Items;
            }
        }

        /// <summary>
        /// Returns the Job whose InstanceId matches the parameter.
        /// </summary>
        /// <returns>
        /// The matching Job. Null if no match is found.
        /// </returns>
        public Job GetJob(Guid instanceId)
        {
            return GetItem(instanceId);
        }

        #region Internal Methods

        /// <summary>
        /// Internal constructor.
        /// </summary>
        internal JobRepository() : base("job")
        {
        }

        /// <summary>
        /// Returns the instance id of the job as key.
        /// </summary>
        /// <param name="item">Job for which a key is required.</param>
        /// <returns>Returns jobs guid.</returns>
        protected override Guid GetKey(Job item)
        {
            if (item != null)
            {
                return item.InstanceId;
            }

            return Guid.Empty;
        }

        #endregion Internal Methods
    }
}
