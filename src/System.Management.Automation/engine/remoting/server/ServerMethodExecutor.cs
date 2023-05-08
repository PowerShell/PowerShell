// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Remoting.Server;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// Responsible for routing messages from the server, blocking the callers and
    /// then waking them up when there is a response to their message.
    /// </summary>
    internal class ServerMethodExecutor
    {
        /// <summary>
        /// Default client pipeline id.
        /// </summary>
        private const long DefaultClientPipelineId = -1;

        /// <summary>
        /// Client runspace pool id.
        /// </summary>
        private readonly Guid _clientRunspacePoolId;

        /// <summary>
        /// Client power shell id.
        /// </summary>
        private readonly Guid _clientPowerShellId;

        /// <summary>
        /// Server dispatch table.
        /// </summary>
        private readonly ServerDispatchTable _serverDispatchTable;

        /// <summary>
        /// Remote host call data type.
        /// </summary>
        private readonly RemotingDataType _remoteHostCallDataType;

        /// <summary>
        /// Transport manager.
        /// </summary>
        private readonly AbstractServerTransportManager _transportManager;

        /// <summary>
        /// Constructor for ServerMethodExecutor.
        /// </summary>
        internal ServerMethodExecutor(
            Guid clientRunspacePoolId, Guid clientPowerShellId,
            AbstractServerTransportManager transportManager)
        {
            _clientRunspacePoolId = clientRunspacePoolId;
            _clientPowerShellId = clientPowerShellId;
            Dbg.Assert(transportManager != null, "Expected transportManager != null");
            _transportManager = transportManager;
            _remoteHostCallDataType =
                clientPowerShellId == Guid.Empty ? RemotingDataType.RemoteHostCallUsingRunspaceHost : RemotingDataType.RemoteHostCallUsingPowerShellHost;
            _serverDispatchTable = new ServerDispatchTable();
        }

        /// <summary>
        /// Handle remote host response from client.
        /// </summary>
        internal void HandleRemoteHostResponseFromClient(RemoteHostResponse remoteHostResponse)
        {
            Dbg.Assert(remoteHostResponse != null, "Expected remoteHostResponse != null");
            _serverDispatchTable.SetResponse(remoteHostResponse.CallId, remoteHostResponse);
        }

        /// <summary>
        /// Abort all calls.
        /// </summary>
        internal void AbortAllCalls()
        {
            _serverDispatchTable.AbortAllCalls();
        }

        /// <summary>
        /// Execute void method.
        /// </summary>
        internal void ExecuteVoidMethod(RemoteHostMethodId methodId)
        {
            ExecuteVoidMethod(methodId, Array.Empty<object>());
        }

        /// <summary>
        /// Execute void method.
        /// </summary>
        internal void ExecuteVoidMethod(RemoteHostMethodId methodId, object[] parameters)
        {
            Dbg.Assert(parameters != null, "Expected parameters != null");

            // Use void call ID so that the call is known to not have a return value.
            const long callId = ServerDispatchTable.VoidCallId;
            RemoteHostCall remoteHostCall = new RemoteHostCall(callId, methodId, parameters);

            // Dispatch the call but don't wait for response since the return value is void.

            // TODO: remove redundant data from the RemoteHostCallPacket.
            RemoteDataObject<PSObject> dataToBeSent = RemoteDataObject<PSObject>.CreateFrom(RemotingDestination.Client,
                _remoteHostCallDataType, _clientRunspacePoolId, _clientPowerShellId,
                remoteHostCall.Encode());
            // flush is not used here..since this is a void method and server host
            // does not expect anything from client..so let the transport manager buffer
            // and send as much data as possible.
            _transportManager.SendDataToClient(dataToBeSent, false);
        }

        /// <summary>
        /// Execute method.
        /// </summary>
        internal T ExecuteMethod<T>(RemoteHostMethodId methodId)
        {
            return ExecuteMethod<T>(methodId, Array.Empty<object>());
        }

        /// <summary>
        /// Execute method.
        /// </summary>
        internal T ExecuteMethod<T>(RemoteHostMethodId methodId, object[] parameters)
        {
            Dbg.Assert(parameters != null, "Expected parameters != null");

            // Create the method call object.
            long callId = _serverDispatchTable.CreateNewCallId();
            RemoteHostCall remoteHostCall = new RemoteHostCall(callId, methodId, parameters);

            RemoteDataObject<PSObject> dataToBeSent = RemoteDataObject<PSObject>.CreateFrom(RemotingDestination.Client,
                _remoteHostCallDataType, _clientRunspacePoolId, _clientPowerShellId,
                remoteHostCall.Encode());
            // report that execution is pending host response
            _transportManager.SendDataToClient(dataToBeSent, false, true);

            // Wait for response.
            RemoteHostResponse remoteHostResponse = _serverDispatchTable.GetResponse(callId, null);

            // Null means that the response PSObject was not received and there was an error.
            if (remoteHostResponse == null)
            {
                throw RemoteHostExceptions.NewRemoteHostCallFailedException(methodId);
            }

            // Process the response.
            object returnValue = remoteHostResponse.SimulateExecution();
            Dbg.Assert(returnValue is T, "Expected returnValue is T");
            return (T)remoteHostResponse.SimulateExecution();
        }
    }
}
