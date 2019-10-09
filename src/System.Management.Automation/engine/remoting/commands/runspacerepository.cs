// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation.Runspaces;

namespace System.Management.Automation
{
    /// <summary>
    /// Repository of remote runspaces available in a local runspace.
    /// </summary>
    public class RunspaceRepository : Repository<PSSession>
    {
        #region Public Methods

        /// <summary>
        /// Collection of runspaces available.
        /// </summary>
        public List<PSSession> Runspaces
        {
            get
            {
                return Items;
            }
        }

        #endregion Public Methods

        #region Internal Methods

        /// <summary>
        /// Internal constructor.
        /// </summary>
        internal RunspaceRepository() : base("runspace")
        {
        }

        /// <summary>
        /// Gets a key for the specified item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected override Guid GetKey(PSSession item)
        {
            if (item != null)
            {
                return item.InstanceId;
            }

            return Guid.Empty;
        }

        /// <summary>
        /// Adds the PSSession item to the repository if it doesn't already
        /// exist or replaces the existing one.
        /// </summary>
        /// <param name="item">PSSession object.</param>
        internal void AddOrReplace(PSSession item)
        {
            this.Dictionary[GetKey(item)] = item;
        }

        #endregion Private Methods
    }
}
