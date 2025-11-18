// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting.Client;
using System.Management.Automation.Runspaces.Internal;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// Executes methods on the client.
    /// </summary>
    internal sealed class ClientMethodExecutor
    {
        /// <summary>
        /// Transport manager.
        /// </summary>
        private readonly BaseClientTransportManager _transportManager;

        /// <summary>
        /// Client host.
        /// </summary>
        private readonly PSHost _clientHost;

        /// <summary>
        /// Client runspace pool id.
        /// </summary>
        private readonly Guid _clientRunspacePoolId;

        /// <summary>
        /// Client power shell id.
        /// </summary>
        private readonly Guid _clientPowerShellId;

        /// <summary>
        /// Remote host call.
        /// </summary>
        private readonly RemoteHostCall _remoteHostCall;

        /// <summary>
        /// Remote host call.
        /// </summary>
        internal RemoteHostCall RemoteHostCall
        {
            get
            {
                return _remoteHostCall;
            }
        }

        /// <summary>
        /// Constructor for ClientMethodExecutor.
        /// </summary>
        private ClientMethodExecutor(BaseClientTransportManager transportManager, PSHost clientHost, Guid clientRunspacePoolId, Guid clientPowerShellId, RemoteHostCall remoteHostCall)
        {
            Dbg.Assert(transportManager != null, "Expected transportManager != null");
            Dbg.Assert(remoteHostCall != null, "Expected remoteHostCall != null");
            _transportManager = transportManager;
            _remoteHostCall = remoteHostCall;
            _clientHost = clientHost;
            _clientRunspacePoolId = clientRunspacePoolId;
            _clientPowerShellId = clientPowerShellId;
        }

        /// <summary>
        /// Create a new ClientMethodExecutor object and then dispatch it.
        /// </summary>
        internal static void Dispatch(
            BaseClientTransportManager transportManager,
            PSHost clientHost,
            PSDataCollectionStream<ErrorRecord> errorStream,
            ObjectStream methodExecutorStream,
            bool isMethodExecutorStreamEnabled,
            RemoteRunspacePoolInternal runspacePool,
            Guid clientPowerShellId,
            RemoteHostCall remoteHostCall)
        {
            ClientMethodExecutor methodExecutor =
                new ClientMethodExecutor(transportManager, clientHost, runspacePool.InstanceId,
                    clientPowerShellId, remoteHostCall);

            // If the powershell id is not specified, this message is for the runspace pool, execute
            // it immediately and return
            if (clientPowerShellId == Guid.Empty)
            {
                methodExecutor.Execute(errorStream);
                return;
            }

            // Check client host to see if SetShouldExit should be allowed
            bool hostAllowSetShouldExit = false;
            if (clientHost != null)
            {
                PSObject hostPrivateData = clientHost.PrivateData as PSObject;
                if (hostPrivateData != null)
                {
                    PSNoteProperty allowSetShouldExit = hostPrivateData.Properties["AllowSetShouldExitFromRemote"] as PSNoteProperty;
                    hostAllowSetShouldExit = allowSetShouldExit != null && allowSetShouldExit.Value is bool && (bool)allowSetShouldExit.Value;
                }
            }

            // Should we kill remote runspace? Check if "SetShouldExit" and if we are in the
            // cmdlet case. In the API case (when we are invoked from an API not a cmdlet) we
            // should not interpret "SetShouldExit" but should pass it on to the host. The
            // variable IsMethodExecutorStreamEnabled is only true in the cmdlet case. In the
            // API case it is false.

            if (remoteHostCall.IsSetShouldExit && isMethodExecutorStreamEnabled && !hostAllowSetShouldExit)
            {
                runspacePool.Close();
                return;
            }

            // Cmdlet case: queue up the executor in the pipeline stream.
            if (isMethodExecutorStreamEnabled)
            {
                Dbg.Assert(methodExecutorStream != null, "method executor stream can't be null when enabled");
                methodExecutorStream.Write(methodExecutor);
            }

            // API case: execute it immediately.
            else
            {
                methodExecutor.Execute(errorStream);
            }
        }

        /// <summary>
        /// Is runspace pushed.
        /// </summary>
        private static bool IsRunspacePushed(PSHost host)
        {
            if (host is not IHostSupportsInteractiveSession host2)
            {
                return false;
            }

            // IsRunspacePushed can throw (not implemented exception)
            try
            {
                return host2.IsRunspacePushed;
            }
            catch (PSNotImplementedException) { }

            return false;
        }

        /// <summary>
        /// Execute.
        /// </summary>
        internal void Execute(PSDataCollectionStream<ErrorRecord> errorStream)
        {
            Action<ErrorRecord> writeErrorAction = null;

            // If error-stream is null or we are in pushed-runspace - then write error directly to console.
            if (errorStream == null || IsRunspacePushed(_clientHost))
            {
                writeErrorAction = (ErrorRecord errorRecord) =>
                {
                    try
                    {
                        _clientHost.UI?.WriteErrorLine(errorRecord.ToString());
                    }
                    catch (Exception)
                    {
                        // Catch-all OK, 3rd party callout.
                    }
                };
            }

            // Otherwise write it to error-stream.
            else
            {
                writeErrorAction = (ErrorRecord errorRecord) => errorStream.Write(errorRecord);
            }

            this.Execute(writeErrorAction);
        }

        /// <summary>
        /// Execute.
        /// </summary>
        internal void Execute(Cmdlet cmdlet)
        {
            this.Execute(cmdlet.WriteError);
        }

        /// <summary>
        /// Execute.
        /// </summary>
        internal void Execute(Action<ErrorRecord> writeErrorAction)
        {
            if (_remoteHostCall.IsVoidMethod)
            {
                ExecuteVoid(writeErrorAction);
            }
            else
            {
                RemotingDataType remotingDataType =
                    _clientPowerShellId == Guid.Empty ? RemotingDataType.RemoteRunspaceHostResponseData : RemotingDataType.RemotePowerShellHostResponseData;

                RemoteHostResponse remoteHostResponse = _remoteHostCall.ExecuteNonVoidMethod(_clientHost);
                RemoteDataObject<PSObject> dataToBeSent = RemoteDataObject<PSObject>.CreateFrom(
                    RemotingDestination.Server, remotingDataType, _clientRunspacePoolId,
                    _clientPowerShellId, remoteHostResponse.Encode());

                _transportManager.DataToBeSentCollection.Add<PSObject>(dataToBeSent, DataPriorityType.PromptResponse);
            }
        }

        /// <summary>
        /// Execute void.
        /// </summary>
        internal void ExecuteVoid(Action<ErrorRecord> writeErrorAction)
        {
            try
            {
                _remoteHostCall.ExecuteVoidMethod(_clientHost);
            }
            catch (Exception exception)
            {
                // Catch-all OK, 3rd party callout.

                // Extract inner exception.
                if (exception.InnerException != null)
                {
                    exception = exception.InnerException;
                }

                // Create an error record and write it to the stream.
                ErrorRecord errorRecord = new ErrorRecord(
                    exception,
                    nameof(PSRemotingErrorId.RemoteHostCallFailed),
                    ErrorCategory.InvalidArgument,
                    _remoteHostCall.MethodName);
                writeErrorAction(errorRecord);
            }
        }
    }
}
