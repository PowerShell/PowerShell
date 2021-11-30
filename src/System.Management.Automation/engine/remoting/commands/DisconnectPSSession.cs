// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This cmdlet disconnects PS sessions (RemoteRunspaces) that are in the Opened state
    /// and returns the PS session objects in the Disconnected state.  While the PS
    /// sessions are in the disconnected state no commands can be invoked on them and
    /// any existing remote running commands will not return any data.
    /// The PS sessions can be reconnected by using the Connect-PSSession cmdlet.
    ///
    /// The cmdlet can be used in the following ways:
    ///
    /// Disconnect a PS session object:
    /// > $session = New-PSSession serverName
    /// > Disconnect-PSSession $session
    ///
    /// Disconnect a PS session by name:
    /// > Disconnect-PSSession -Name $session.Name
    ///
    /// Disconnect a PS session by Id:
    /// > Disconnect-PSSession -Id $session.Id
    ///
    /// Disconnect a collection of PS sessions:
    /// > Get-PSSession | Disconnect-PSSession.
    /// </summary>
    [SuppressMessage("Microsoft.PowerShell", "PS1012:CallShouldProcessOnlyIfDeclaringSupport")]
    [Cmdlet(VerbsCommunications.Disconnect, "PSSession", SupportsShouldProcess = true, DefaultParameterSetName = DisconnectPSSessionCommand.SessionParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096576", RemotingCapability = RemotingCapability.OwnedByCommand)]
    [OutputType(typeof(PSSession))]
    public class DisconnectPSSessionCommand : PSRunspaceCmdlet, IDisposable
    {
        #region Parameters

        /// <summary>
        /// The PSSession object or objects to be disconnected.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ValueFromPipeline = true,
                   ParameterSetName = DisconnectPSSessionCommand.SessionParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public PSSession[] Session { get; set; }

        /// <summary>
        /// Idle Timeout session option in seconds.  Used in this cmdlet to set server disconnect idletimeout option.
        /// </summary>
        [Parameter(ParameterSetName = DisconnectPSSessionCommand.SessionParameterSet)]
        [Parameter(ParameterSetName = PSRunspaceCmdlet.NameParameterSet)]
        [Parameter(ParameterSetName = PSRunspaceCmdlet.IdParameterSet)]
        [Parameter(ParameterSetName = PSRunspaceCmdlet.InstanceIdParameterSet)]
        [ValidateRange(0, int.MaxValue)]
        public int IdleTimeoutSec
        {
            get { return this.PSSessionOption.IdleTimeout.Seconds; }

            set { this.PSSessionOption.IdleTimeout = TimeSpan.FromSeconds(value); }
        }

        /// <summary>
        /// Output buffering mode session option.  Used in this cmdlet to set server disconnect OutputBufferingMode option.
        /// </summary>
        [Parameter(ParameterSetName = DisconnectPSSessionCommand.SessionParameterSet)]
        [Parameter(ParameterSetName = PSRunspaceCmdlet.NameParameterSet)]
        [Parameter(ParameterSetName = PSRunspaceCmdlet.IdParameterSet)]
        [Parameter(ParameterSetName = PSRunspaceCmdlet.InstanceIdParameterSet)]
        public OutputBufferingMode OutputBufferingMode
        {
            get { return this.PSSessionOption.OutputBufferingMode; }

            set { this.PSSessionOption.OutputBufferingMode = value; }
        }

        /// <summary>
        /// Allows the user of the cmdlet to specify a throttling value
        /// for throttling the number of remote operations that can
        /// be executed simultaneously.
        /// </summary>
        [Parameter(ParameterSetName = DisconnectPSSessionCommand.SessionParameterSet)]
        [Parameter(ParameterSetName = PSRunspaceCmdlet.NameParameterSet)]
        [Parameter(ParameterSetName = PSRunspaceCmdlet.IdParameterSet)]
        [Parameter(ParameterSetName = PSRunspaceCmdlet.InstanceIdParameterSet)]
        public int ThrottleLimit { get; set; } = 0;

        /// <summary>
        /// Disconnect-PSSession does not support ComputerName parameter set.
        /// This may change for later versions.
        /// </summary>
        public override string[] ComputerName { get; set; }

        private PSSessionOption PSSessionOption
        {
            get
            {
                // no need to lock as the cmdlet parameters will not be assigned
                // from multiple threads.
                return _sessionOption ??= new PSSessionOption();
            }
        }

        private PSSessionOption _sessionOption;

        /// <summary>
        /// Overriding to suppress this parameter.
        /// </summary>
        public override string[] ContainerId
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Overriding to suppress this parameter.
        /// </summary>
        public override Guid[] VMId
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Overriding to suppress this parameter.
        /// </summary>
        public override string[] VMName
        {
            get
            {
                return null;
            }
        }

        #endregion

        #region Cmdlet Overrides

        /// <summary>
        /// Set up the ThrottleManager for runspace disconnect processing.
        /// </summary>
        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            _throttleManager.ThrottleLimit = ThrottleLimit;
            _throttleManager.ThrottleComplete += HandleThrottleDisconnectComplete;
        }

        /// <summary>
        /// Perform runspace disconnect processing on all input.
        /// </summary>
        protected override void ProcessRecord()
        {
            Dictionary<Guid, PSSession> psSessions;
            List<IThrottleOperation> disconnectOperations = new List<IThrottleOperation>();

            try
            {
                // Get all remote runspaces to disconnect.
                if (ParameterSetName == DisconnectPSSessionCommand.SessionParameterSet)
                {
                    if (Session == null || Session.Length == 0)
                    {
                        return;
                    }

                    psSessions = new Dictionary<Guid, PSSession>();
                    foreach (PSSession psSession in Session)
                    {
                        psSessions.Add(psSession.InstanceId, psSession);
                    }
                }
                else
                {
                    psSessions = GetMatchingRunspaces(false, true);
                }

                // Look for local sessions that have the EnableNetworkAccess property set and
                // return a string containing all of the session names.  Emit a warning for
                // these sessions.
                string cnNames = GetLocalhostWithNetworkAccessEnabled(psSessions);
                if (!string.IsNullOrEmpty(cnNames))
                {
                    WriteWarning(
                        StringUtil.Format(RemotingErrorIdStrings.EnableNetworkAccessWarning, cnNames));
                }

                // Create a disconnect operation for each runspace to disconnect.
                foreach (PSSession psSession in psSessions.Values)
                {
                    if (ShouldProcess(psSession.Name, VerbsCommunications.Disconnect))
                    {
                        // PS session disconnection is not supported for VM/Container sessions.
                        if (psSession.ComputerType != TargetMachineType.RemoteMachine)
                        {
                            // Write error record.
                            string msg = StringUtil.Format(RemotingErrorIdStrings.RunspaceCannotBeDisconnectedForVMContainerSession,
                                psSession.Name, psSession.ComputerName, psSession.ComputerType);
                            Exception reason = new PSNotSupportedException(msg);
                            ErrorRecord errorRecord = new ErrorRecord(reason, "CannotDisconnectVMContainerSession", ErrorCategory.InvalidOperation, psSession);
                            WriteError(errorRecord);
                            continue;
                        }

                        // Can only disconnect an Opened runspace.
                        if (psSession.Runspace.RunspaceStateInfo.State == RunspaceState.Opened)
                        {
                            // Update the connectionInfo object with passed in session options.
                            if (_sessionOption != null)
                            {
                                psSession.Runspace.ConnectionInfo.SetSessionOptions(_sessionOption);
                            }

                            // Validate the ConnectionInfo IdleTimeout value against the MaxIdleTimeout
                            // value returned by the server and the hard coded minimum allowed value.
                            if (!ValidateIdleTimeout(psSession))
                            {
                                continue;
                            }

                            DisconnectRunspaceOperation disconnectOperation = new DisconnectRunspaceOperation(psSession, _stream);
                            disconnectOperations.Add(disconnectOperation);
                        }
                        else if (psSession.Runspace.RunspaceStateInfo.State != RunspaceState.Disconnected)
                        {
                            // Write error record.
                            string msg = StringUtil.Format(RemotingErrorIdStrings.RunspaceCannotBeDisconnected, psSession.Name);
                            Exception reason = new RuntimeException(msg);
                            ErrorRecord errorRecord = new ErrorRecord(reason, "CannotDisconnectSessionWhenNotOpened", ErrorCategory.InvalidOperation, psSession);
                            WriteError(errorRecord);
                        }
                        else
                        {
                            // Session is already disconnected.  Write to output.
                            WriteObject(psSession);
                        }
                    }
                }
            }
            catch (PSRemotingDataStructureException)
            {
                // Allow cmdlet to end and then re-throw exception.
                _operationsComplete.Set();
                throw;
            }
            catch (PSRemotingTransportException)
            {
                // Allow cmdlet to end and then re-throw exception.
                _operationsComplete.Set();
                throw;
            }
            catch (RemoteException)
            {
                // Allow cmdlet to end and then re-throw exception.
                _operationsComplete.Set();
                throw;
            }
            catch (InvalidRunspaceStateException)
            {
                // Allow cmdlet to end and then re-throw exception.
                _operationsComplete.Set();
                throw;
            }

            if (disconnectOperations.Count > 0)
            {
                // Make sure operations are not set as complete while processing input.
                _operationsComplete.Reset();

                // Submit list of disconnect operations.
                _throttleManager.SubmitOperations(disconnectOperations);

                // Write any output now.
                Collection<object> streamObjects = _stream.ObjectReader.NonBlockingRead();
                foreach (object streamObject in streamObjects)
                {
                    WriteStreamObject((Action<Cmdlet>)streamObject);
                }
            }
        }

        /// <summary>
        /// End processing clean up.
        /// </summary>
        protected override void EndProcessing()
        {
            _throttleManager.EndSubmitOperations();

            // Wait for all disconnect operations to complete.
            _operationsComplete.WaitOne();

            // Read all objects in the stream pipeline.
            while (!_stream.ObjectReader.EndOfPipeline)
            {
                object streamObject = _stream.ObjectReader.Read();
                WriteStreamObject((Action<Cmdlet>)streamObject);
            }
        }

        /// <summary>
        /// User has signaled a stop for this cmdlet.
        /// </summary>
        protected override void StopProcessing()
        {
            // Close the output stream for any further writes.
            _stream.ObjectWriter.Close();

            // Signal the ThrottleManager to stop any further processing
            // of PSSessions.
            _throttleManager.StopAllOperations();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles the connect throttling complete event from the ThrottleManager.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="eventArgs">EventArgs.</param>
        private void HandleThrottleDisconnectComplete(object sender, EventArgs eventArgs)
        {
            _stream.ObjectWriter.Close();
            _operationsComplete.Set();
        }

        private bool ValidateIdleTimeout(PSSession session)
        {
            int idleTimeout = session.Runspace.ConnectionInfo.IdleTimeout;
            int maxIdleTimeout = session.Runspace.ConnectionInfo.MaxIdleTimeout;
            const int minIdleTimeout = BaseTransportManager.MinimumIdleTimeout;

            if (idleTimeout != BaseTransportManager.UseServerDefaultIdleTimeout &&
                (idleTimeout > maxIdleTimeout || idleTimeout < minIdleTimeout))
            {
                string msg = StringUtil.Format(RemotingErrorIdStrings.CannotDisconnectSessionWithInvalidIdleTimeout,
                    session.Name, idleTimeout / 1000, maxIdleTimeout / 1000, minIdleTimeout / 1000);
                ErrorRecord errorRecord = new ErrorRecord(new RuntimeException(msg),
                    "CannotDisconnectSessionWithInvalidIdleTimeout", ErrorCategory.InvalidArgument, session);
                WriteError(errorRecord);

                return false;
            }

            return true;
        }

        private static string GetLocalhostWithNetworkAccessEnabled(Dictionary<Guid, PSSession> psSessions)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (PSSession psSession in psSessions.Values)
            {
                WSManConnectionInfo wsManConnectionInfo = psSession.Runspace.ConnectionInfo as WSManConnectionInfo;

                if ((wsManConnectionInfo != null) && (wsManConnectionInfo.IsLocalhostAndNetworkAccess))
                {
                    sb.Append(psSession.Name + ", ");
                }
            }

            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - 2, 2);
            }

            return sb.ToString();
        }

        #endregion

        #region Private Classes

        /// <summary>
        /// Throttle class to perform a remoterunspace disconnect operation.
        /// </summary>
        private sealed class DisconnectRunspaceOperation : IThrottleOperation
        {
            private readonly PSSession _remoteSession;
            private readonly ObjectStream _writeStream;

            internal DisconnectRunspaceOperation(PSSession session, ObjectStream stream)
            {
                _remoteSession = session;
                _writeStream = stream;
                _remoteSession.Runspace.StateChanged += StateCallBackHandler;
            }

            internal override void StartOperation()
            {
                bool startedSuccessfully = true;

                try
                {
                    _remoteSession.Runspace.DisconnectAsync();
                }
                catch (InvalidRunspacePoolStateException e)
                {
                    startedSuccessfully = false;
                    WriteDisconnectFailed(e);
                }
                catch (PSInvalidOperationException e)
                {
                    startedSuccessfully = false;
                    WriteDisconnectFailed(e);
                }

                if (!startedSuccessfully)
                {
                    // We are done at this point.  Notify throttle manager.
                    _remoteSession.Runspace.StateChanged -= StateCallBackHandler;
                    SendStartComplete();
                }
            }

            internal override void StopOperation()
            {
                // Cannot stop a disconnect attempt.
                _remoteSession.Runspace.StateChanged -= StateCallBackHandler;
                SendStopComplete();
            }

            internal override event EventHandler<OperationStateEventArgs> OperationComplete;

            private void StateCallBackHandler(object sender, RunspaceStateEventArgs eArgs)
            {
                if (eArgs.RunspaceStateInfo.State == RunspaceState.Disconnecting)
                {
                    return;
                }

                if (eArgs.RunspaceStateInfo.State == RunspaceState.Disconnected)
                {
                    // If disconnect succeeded then write the PSSession object.
                    WriteDisconnectedPSSession();
                }
                else
                {
                    // Write error if disconnect did not succeed.
                    WriteDisconnectFailed();
                }

                // Notify throttle manager that the start is complete.
                _remoteSession.Runspace.StateChanged -= StateCallBackHandler;
                SendStartComplete();
            }

            private void SendStartComplete()
            {
                OperationStateEventArgs operationStateEventArgs = new OperationStateEventArgs();
                operationStateEventArgs.OperationState = OperationState.StartComplete;
                OperationComplete.SafeInvoke(this, operationStateEventArgs);
            }

            private void SendStopComplete()
            {
                OperationStateEventArgs operationStateEventArgs = new OperationStateEventArgs();
                operationStateEventArgs.OperationState = OperationState.StopComplete;
                OperationComplete.SafeInvoke(this, operationStateEventArgs);
            }

            private void WriteDisconnectedPSSession()
            {
                if (_writeStream.ObjectWriter.IsOpen)
                {
                    Action<Cmdlet> outputWriter = (Cmdlet cmdlet) => cmdlet.WriteObject(_remoteSession);
                    _writeStream.ObjectWriter.Write(outputWriter);
                }
            }

            private void WriteDisconnectFailed(Exception e = null)
            {
                if (_writeStream.ObjectWriter.IsOpen)
                {
                    string msg;

                    if (e != null && !string.IsNullOrWhiteSpace(e.Message))
                    {
                        msg = StringUtil.Format(RemotingErrorIdStrings.RunspaceDisconnectFailedWithReason, _remoteSession.InstanceId, e.Message);
                    }
                    else
                    {
                        msg = StringUtil.Format(RemotingErrorIdStrings.RunspaceDisconnectFailed, _remoteSession.InstanceId);
                    }

                    Exception reason = new RuntimeException(msg, e);
                    ErrorRecord errorRecord = new ErrorRecord(reason, "PSSessionDisconnectFailed", ErrorCategory.InvalidOperation, _remoteSession);
                    Action<Cmdlet> errorWriter = (Cmdlet cmdlet) => cmdlet.WriteError(errorRecord);
                    _writeStream.ObjectWriter.Write(errorWriter);
                }
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose method of IDisposable. Gets called in the following cases:
        ///     1. Pipeline explicitly calls dispose on cmdlets
        ///     2. Called by the garbage collector.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Internal dispose method which does the actual
        /// dispose operations and finalize suppressions.
        /// </summary>
        /// <param name="disposing">Whether method is called
        /// from Dispose or destructor</param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _throttleManager.Dispose();

                _operationsComplete.WaitOne();
                _operationsComplete.Dispose();

                _throttleManager.ThrottleComplete -= HandleThrottleDisconnectComplete;
                _stream.Dispose();
            }
        }

        #endregion IDisposable Overrides

        #region Private Members

        // Object used to perform network disconnect operations in a limited manner.
        private readonly ThrottleManager _throttleManager = new ThrottleManager();

        // Event indicating that all disconnect operations through the ThrottleManager
        // are complete.
        private readonly ManualResetEvent _operationsComplete = new ManualResetEvent(true);

        // Output data stream.
        private readonly ObjectStream _stream = new ObjectStream();

        #endregion
    }
}
