/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Runspaces.Internal;
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
    /// > Get-PSSession | Disconnect-PSSession
    /// 
    /// </summary>
    [SuppressMessage("Microsoft.PowerShell", "PS1012:CallShouldProcessOnlyIfDeclaringSupport")]
    [Cmdlet(VerbsCommunications.Disconnect, "PSSession", SupportsShouldProcess = true, DefaultParameterSetName = DisconnectPSSessionCommand.SessionParameterSet,
        HelpUri = "http://go.microsoft.com/fwlink/?LinkID=210605", RemotingCapability = RemotingCapability.OwnedByCommand)]
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
        public PSSession[] Session
        {
            get { return this.remotePSSessionInfo; }
            set { this.remotePSSessionInfo = value; }
        }
        private PSSession[] remotePSSessionInfo;

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
        public Int32 ThrottleLimit
        {
            get { return this.throttleLimit; }
            set { this.throttleLimit = value; }
        }
        private Int32 throttleLimit = 0;

        /// <summary>
        /// Disconnect-PSSession does not support ComputerName parameter set.
        /// This may change for later versions.
        /// </summary>
        public override String[] ComputerName
        {
            get;
            set;
        }

        private PSSessionOption PSSessionOption
        {
            get
            {
                // no need to lock as the cmdlet parameters will not be assigned
                // from multiple threads.
                if (null == sessionOption)
                {
                    sessionOption = new PSSessionOption();
                }
                return sessionOption;
            }
        }
        private PSSessionOption sessionOption;

        /// <summary>
        /// Overriding to suppress this parameter
        /// </summary>
        public override string[] ContainerId
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Overriding to suppress this parameter
        /// </summary>
        public override Guid[] VMId
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Overriding to suppress this parameter
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

            this.throttleManager.ThrottleLimit = ThrottleLimit;
            this.throttleManager.ThrottleComplete += new EventHandler<EventArgs>(HandleThrottleDisconnectComplete);
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
                    if (this.remotePSSessionInfo == null || this.remotePSSessionInfo.Length == 0)
                    {
                        return;
                    }

                    psSessions = new Dictionary<Guid, PSSession>();
                    foreach (PSSession psSession in this.remotePSSessionInfo)
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
                            if (sessionOption != null)
                            {
                                psSession.Runspace.ConnectionInfo.SetSessionOptions(sessionOption);
                            }

                            // Validate the ConnectionInfo IdleTimeout value against the MaxIdleTimeout
                            // value returned by the server and the hard coded minimum allowed value.
                            if (!ValidateIdleTimeout(psSession))
                            {
                                continue;
                            }

                            DisconnectRunspaceOperation disconnectOperation = new DisconnectRunspaceOperation(psSession, this.stream);
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
                this.operationsComplete.Set();
                throw;
            }
            catch (PSRemotingTransportException)
            {
                // Allow cmdlet to end and then re-throw exception.
                this.operationsComplete.Set();
                throw;
            }
            catch (RemoteException)
            {
                // Allow cmdlet to end and then re-throw exception.
                this.operationsComplete.Set();
                throw;
            }
            catch (InvalidRunspaceStateException)
            {
                // Allow cmdlet to end and then re-throw exception.
                this.operationsComplete.Set();
                throw;
            }

            if (disconnectOperations.Count > 0)
            {
                // Make sure operations are not set as complete while processing input.
                this.operationsComplete.Reset();

                // Submit list of disconnect operations.
                this.throttleManager.SubmitOperations(disconnectOperations);

                // Write any output now.
                Collection<object> streamObjects = this.stream.ObjectReader.NonBlockingRead();
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
            this.throttleManager.EndSubmitOperations();

            // Wait for all disconnect operations to complete.
            this.operationsComplete.WaitOne();

            // Read all objects in the stream pipeline.
            while (!this.stream.ObjectReader.EndOfPipeline)
            {
                Object streamObject = stream.ObjectReader.Read();
                WriteStreamObject((Action<Cmdlet>)streamObject);
            }
        }

        /// <summary>
        /// User has signaled a stop for this cmdlet.
        /// </summary>
        protected override void StopProcessing()
        {
            // Close the output stream for any further writes.
            this.stream.ObjectWriter.Close();

            // Signal the ThrottleManager to stop any further processing
            // of PSSessions.
            this.throttleManager.StopAllOperations();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles the connect throttling complete event from the ThrottleManager.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="eventArgs">EventArgs</param>
        private void HandleThrottleDisconnectComplete(object sender, EventArgs eventArgs)
        {
            this.stream.ObjectWriter.Close();
            this.operationsComplete.Set();
        }

        private bool ValidateIdleTimeout(PSSession session)
        {
            int idleTimeout = session.Runspace.ConnectionInfo.IdleTimeout;
            int maxIdleTimeout = session.Runspace.ConnectionInfo.MaxIdleTimeout;
            int minIdleTimeout = BaseTransportManager.MinimumIdleTimeout;

            if (idleTimeout != BaseTransportManager.UseServerDefaultIdleTimeout &&
                (idleTimeout > maxIdleTimeout || idleTimeout < minIdleTimeout))
            {
                string msg = StringUtil.Format(RemotingErrorIdStrings.CannotDisconnectSessionWithInvalidIdleTimeout, 
                    session.Name, idleTimeout/1000, maxIdleTimeout/1000, minIdleTimeout/1000);
                ErrorRecord errorRecord = new ErrorRecord(new RuntimeException(msg), 
                    "CannotDisconnectSessionWithInvalidIdleTimeout", ErrorCategory.InvalidArgument, session);
                WriteError(errorRecord);

                return false;
            }

            return true;
        }

        private string GetLocalhostWithNetworkAccessEnabled(Dictionary<Guid, PSSession> psSessions)
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
        private class DisconnectRunspaceOperation : IThrottleOperation
        {
            private PSSession remoteSession;
            private ObjectStream writeStream;

            internal DisconnectRunspaceOperation(PSSession session, ObjectStream stream)
            {
                this.remoteSession = session;
                this.writeStream = stream;
                this.remoteSession.Runspace.StateChanged += StateCallBackHandler;
            }

            internal override void StartOperation()
            {
                bool startedSuccessfully = true;

                try
                {
                    this.remoteSession.Runspace.DisconnectAsync();
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
                    this.remoteSession.Runspace.StateChanged -= StateCallBackHandler;
                    SendStartComplete();
                }
            }

            internal override void StopOperation()
            {
                // Cannot stop a disconnect attempt.
                this.remoteSession.Runspace.StateChanged -= StateCallBackHandler;
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
                this.remoteSession.Runspace.StateChanged -= StateCallBackHandler;
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
                if (this.writeStream.ObjectWriter.IsOpen)
                {
                    Action<Cmdlet> outputWriter = delegate(Cmdlet cmdlet)
                    {
                        cmdlet.WriteObject(this.remoteSession);
                    };
                    this.writeStream.ObjectWriter.Write(outputWriter);
                }
            }

            private void WriteDisconnectFailed(Exception e = null)
            {
                if (this.writeStream.ObjectWriter.IsOpen)
                {
                    string msg;

                    if (e != null && !string.IsNullOrWhiteSpace(e.Message))
                    {
                        msg = StringUtil.Format(RemotingErrorIdStrings.RunspaceDisconnectFailedWithReason, this.remoteSession.InstanceId, e.Message);
                    }
                    else
                    {
                        msg = StringUtil.Format(RemotingErrorIdStrings.RunspaceDisconnectFailed, this.remoteSession.InstanceId);
                    }
                    Exception reason = new RuntimeException(msg, e);
                    ErrorRecord errorRecord = new ErrorRecord(reason, "PSSessionDisconnectFailed", ErrorCategory.InvalidOperation, this.remoteSession);
                    Action<Cmdlet> errorWriter = delegate(Cmdlet cmdlet)
                    {
                        cmdlet.WriteError(errorRecord);
                    };
                    this.writeStream.ObjectWriter.Write(errorWriter);
                }
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose method of IDisposable. Gets called in the following cases:
        ///     1. Pipeline explicitly calls dispose on cmdlets
        ///     2. Called by the garbage collector 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Internal dispose method which does the actual
        /// dispose operations and finalize suppressions
        /// </summary>
        /// <param name="disposing">Whether method is called 
        /// from Dispose or destructor</param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.throttleManager.Dispose();

                this.operationsComplete.WaitOne();
                this.operationsComplete.Dispose();

                this.throttleManager.ThrottleComplete -= new EventHandler<EventArgs>(HandleThrottleDisconnectComplete);
                this.stream.Dispose();
            }
        }

        #endregion IDisposable Overrides

        #region Private Members

        // Object used to perform network disconnect operations in a limited manner.
        private ThrottleManager throttleManager = new ThrottleManager();

        // Event indicating that all disconnect operations through the ThrottleManager
        // are complete.
        private ManualResetEvent operationsComplete = new ManualResetEvent(true);

        // Output data stream.
        private ObjectStream stream = new ObjectStream();

        #endregion

    } // DisconnectPSSessionCommand
}
