// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Remoting;

namespace System.Management.Automation
{
    /// <summary>
    /// This abstract class is designed to provide InstanceId and self identification for
    /// client and server remote session classes.
    /// </summary>
    internal abstract class RemoteSession
    {
        /// <summary>
        /// This is the unique id of a remote session object.
        /// </summary>
        internal Guid InstanceId { get; } = new Guid();

        /// <summary>
        /// This indicates the remote session object is Client, Server or Listener.
        /// </summary>
        internal abstract RemotingDestination MySelf { get; }

        #region KeyExchange

        internal abstract void StartKeyExchange();

        internal abstract void CompleteKeyExchange();

        internal BaseSessionDataStructureHandler BaseSessionDataStructureHandler { get; set; }

        #endregion KeyExchange
    }
}
