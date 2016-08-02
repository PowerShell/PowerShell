/********************************************************************++
 * Copyright (c) Microsoft Corporation.  All rights reserved.
 * --********************************************************************/

using System.Management.Automation.Remoting;

namespace System.Management.Automation
{
    /// <summary>
    /// This abstract class is designed to provide InstandId and self identification for
    /// client and server remote session classes.
    /// </summary>
    internal abstract class RemoteSession
    {
        private Guid _instanceId = new Guid();

        /// <summary>
        /// This is the unique id of a remote session object.
        /// </summary>
        internal Guid InstanceId
        {
            get
            {
                return _instanceId;
            }
        }

        /// <summary>
        /// This indicates the remote session object is Client, Server or Listener.
        /// </summary>
        internal abstract RemotingDestination MySelf { get; }

        #region KeyExchange

        internal abstract void StartKeyExchange();

        internal abstract void CompleteKeyExchange();

        private BaseSessionDataStructureHandler _dsHandler;

        internal BaseSessionDataStructureHandler BaseSessionDataStructureHandler
        {
            get
            {
                return _dsHandler;
            }
            set
            {
                _dsHandler = value;
            }
        }

        #endregion KeyExchange
    }
}

