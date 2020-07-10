// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Remoting.Client;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    internal abstract class BaseSessionDataStructureHandler
    {
        internal abstract void RaiseKeyExchangeMessageReceived(RemoteDataObject<PSObject> receivedData);
    }

    internal abstract class ClientRemoteSessionDataStructureHandler : BaseSessionDataStructureHandler
    {
        #region Abstract_API

        internal abstract void CreateAsync();

        internal abstract event EventHandler<RemoteSessionStateEventArgs> ConnectionStateChanged;

        internal abstract void SendNegotiationAsync(RemoteSessionState sessionState);

        internal abstract event EventHandler<RemoteSessionNegotiationEventArgs> NegotiationReceived;

        internal abstract void CloseConnectionAsync();

        internal abstract void DisconnectAsync();

        internal abstract void ReconnectAsync();

        internal abstract ClientRemoteSessionDSHandlerStateMachine StateMachine
        {
            get;
        }

        internal abstract BaseClientSessionTransportManager TransportManager { get; }

        internal abstract BaseClientCommandTransportManager CreateClientCommandTransportManager(
            System.Management.Automation.Runspaces.Internal.ClientRemotePowerShell cmd,
            bool noInput);

        // TODO: If this is not used, remove this.
        // internal abstract event EventHandler<RemoteDataEventArgs> DataReceived;

        internal abstract event EventHandler<RemoteDataEventArgs<string>> EncryptedSessionKeyReceived;

        internal abstract event EventHandler<RemoteDataEventArgs<string>> PublicKeyRequestReceived;

        internal abstract void SendPublicKeyAsync(string localPublicKey);

        #endregion Abstract_API
    }
}
