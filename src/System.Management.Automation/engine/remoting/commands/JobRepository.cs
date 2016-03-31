/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Collections.Generic;
using System.Management.Automation.Remoting;

namespace System.Management.Automation
{
    /// <summary>
    /// class which has list of job objects currently active in the system.
    /// </summary>
    public abstract class Repository<T> where T: class
    {
        #region Public Methods

        /// <summary>
        /// Add an item to the repository
        /// </summary>
        /// <param name="item">object to add</param>
        public void Add(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(identifier);
            }
            lock (syncObject)
            {
                Guid instanceId = GetKey(item);

                if (!repository.ContainsKey(instanceId))
                {
                    repository.Add(instanceId, item);
                }
                else
                {
                    throw new ArgumentException(identifier);
                }
            }
        }

        /// <summary>
        /// Remove the specified item from the repository
        /// </summary>
        /// <param name="item">object to remove</param>
        public void Remove(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(identifier);
            }
            lock (syncObject)
            {
                Guid instanceId = GetKey(item);

                if (!repository.Remove(instanceId))
                {
                    String message =
                        PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.ItemNotFoundInRepository,
                            "Job repository", instanceId.ToString());

                    throw new ArgumentException(message);
                }
            }
        }
     
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<T> GetItems()
        {
            return Items;
        }
        #endregion Public Methods

        #region Private/Internal Methods

        /// <summary>
        /// Get a key for the specified item
        /// </summary>
        /// <param name="item">item for which the key is required</param>
        /// <returns>returns a key</returns>
        protected abstract Guid GetKey(T item);

        /// <summary>
        /// internal constructor
        /// </summary>
        protected Repository(string identifier)
        {
            this.identifier = identifier;
        }

        /// <summary>
        /// Creates a repository with the specified values
        /// </summary>
        internal List<T> Items
        {
            get
            {
                lock (syncObject)
                {
                    return new List<T>(repository.Values);
                }
            }
        }

        /// <summary>
        /// Gets the specified Item
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        public T GetItem(Guid instanceId)
        {
            lock (syncObject)
            {
                T result;
                repository.TryGetValue(instanceId, out result);
                return result;
            }
        }

        /// <summary>
        /// Gets the Repository dictionary.
        /// </summary>
        internal Dictionary<Guid, T> Dictionary
        {
            get { return this.repository; }
        }

        #endregion Private Methods

        #region Private Members

        private Dictionary<Guid, T> repository = new Dictionary<Guid, T>();
        private object syncObject = new object();      // object for synchronization
        private string identifier;

        #endregion Private Members
    }

    /// <summary>
    /// class which has list of job objects currently active in the system.
    /// </summary>
    public class JobRepository : Repository<Job>
    {
        /// <summary>
        /// Returns the list of available job objects
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
        /// internal constructor
        /// </summary>
        internal JobRepository() : base("job")
        {
        }

        /// <summary>
        /// Returns the instance id of the job as key
        /// </summary>
        /// <param name="item">job for which a key is required</param>
        /// <returns>returns jobs guid</returns>
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
