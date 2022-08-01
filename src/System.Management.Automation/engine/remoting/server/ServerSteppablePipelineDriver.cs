// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation.Host;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// Execution context used for stepping.
    /// </summary>
    internal sealed class ExecutionContextForStepping : IDisposable
    {
        private readonly ExecutionContext _executionContext;
        private PSInformationalBuffers _originalInformationalBuffers;
        private PSHost _originalHost;

        private ExecutionContextForStepping(ExecutionContext ctxt)
        {
            Dbg.Assert(ctxt != null, "ExecutionContext cannot be null.");
            _executionContext = ctxt;
        }

        internal static ExecutionContextForStepping PrepareExecutionContext(
            ExecutionContext ctxt,
            PSInformationalBuffers newBuffers,
            PSHost newHost)
        {
            ExecutionContextForStepping result = new ExecutionContextForStepping(ctxt);

            result._originalInformationalBuffers
                = ctxt.InternalHost.InternalUI.GetInformationalMessageBuffers();
            result._originalHost = ctxt.InternalHost.ExternalHost;

            ctxt.InternalHost.InternalUI.SetInformationalMessageBuffers(newBuffers);
            ctxt.InternalHost.SetHostRef(newHost);

            return result;
        }

        // Summary:
        //     Performs application-defined tasks associated with freeing, releasing, or
        //     resetting unmanaged resources.
        void IDisposable.Dispose()
        {
            _executionContext.InternalHost.InternalUI.SetInformationalMessageBuffers(_originalInformationalBuffers);
            _executionContext.InternalHost.SetHostRef(_originalHost);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// This class wraps a RunspacePoolInternal object. It is used to function
    /// as a server side runspacepool.
    /// </summary>
    internal class ServerSteppablePipelineDriver
    {
        #region Private Data

        // that is associated with this
        // powershell driver
        // associated with this powershell
        // data structure handler object to handle all
        // communications with the client
        // private bool datasent = false;          // if the remaining data has been sent
        // to the client before sending state
        // information
        // data to client
        // was created
        private readonly bool _addToHistory;
        // associated with this powershell
        private readonly ApartmentState apartmentState;  // apartment state for this powershell

        // pipeline that runs the actual command.
        private readonly ServerSteppablePipelineSubscriber _eventSubscriber;
        private readonly PSDataCollection<object> _powershellInput; // input collection of the PowerShell pipeline

        #endregion

        /// <summary>
        /// Default constructor for creating ServerSteppablePipelineDriver...Used by server to concurrently
        /// run 2 pipelines.
        /// </summary>
        /// <param name="powershell">Decoded powershell object.</param>
        /// <param name="noInput">Whether there is input for this powershell.</param>
        /// <param name="clientPowerShellId">The client powershell id.</param>
        /// <param name="clientRunspacePoolId">The client runspacepool id.</param>
        /// <param name="runspacePoolDriver">runspace pool driver
        /// which is creating this powershell driver</param>
        /// <param name="apartmentState">Apartment state for this powershell.</param>
        /// <param name="hostInfo">host info using which the host for
        /// this powershell will be constructed</param>
        /// <param name="streamOptions">Serialization options for the streams in this powershell.</param>
        /// <param name="addToHistory">
        /// true if the command is to be added to history list of the runspace. false, otherwise.
        /// </param>
        /// <param name="rsToUse">
        /// If not null, this Runspace will be used to invoke Powershell.
        /// If null, the RunspacePool pointed by <paramref name="runspacePoolDriver"/> will be used.
        /// </param>
        /// <param name="eventSubscriber">
        /// Steppable pipeline event subscriber
        /// </param>
        /// <param name="powershellInput">Input collection of the PowerShell pipeline.</param>
        internal ServerSteppablePipelineDriver(PowerShell powershell, bool noInput, Guid clientPowerShellId,
            Guid clientRunspacePoolId, ServerRunspacePoolDriver runspacePoolDriver,
            ApartmentState apartmentState, HostInfo hostInfo, RemoteStreamOptions streamOptions,
            bool addToHistory, Runspace rsToUse, ServerSteppablePipelineSubscriber eventSubscriber, PSDataCollection<object> powershellInput)
        {
            LocalPowerShell = powershell;
            InstanceId = clientPowerShellId;
            RunspacePoolId = clientRunspacePoolId;
            RemoteStreamOptions = streamOptions;
            this.apartmentState = apartmentState;
            NoInput = noInput;
            _addToHistory = addToHistory;
            _eventSubscriber = eventSubscriber;
            _powershellInput = powershellInput;

            Input = new PSDataCollection<object>();
            InputEnumerator = Input.GetEnumerator();
            Input.ReleaseOnEnumeration = true;

            DataStructureHandler = runspacePoolDriver.DataStructureHandler.CreatePowerShellDataStructureHandler(clientPowerShellId, clientRunspacePoolId, RemoteStreamOptions, null);
            RemoteHost = DataStructureHandler.GetHostAssociatedWithPowerShell(hostInfo, runspacePoolDriver.ServerRemoteHost);

            // subscribe to various data structure handler events
            DataStructureHandler.InputEndReceived += HandleInputEndReceived;
            DataStructureHandler.InputReceived += HandleInputReceived;
            DataStructureHandler.StopPowerShellReceived += HandleStopReceived;
            DataStructureHandler.HostResponseReceived += HandleHostResponseReceived;
            DataStructureHandler.OnSessionConnected += HandleSessionConnected;

            if (rsToUse == null)
            {
                throw PSTraceSource.NewInvalidOperationException(RemotingErrorIdStrings.NestedPipelineMissingRunspace);
            }

            // else, set the runspace pool and invoke this powershell
            LocalPowerShell.Runspace = rsToUse;
            eventSubscriber.SubscribeEvents(this);

            PipelineState = PSInvocationState.NotStarted;
        }

        #region Internal Methods

        /// <summary>
        /// Local PowerShell instance.
        /// </summary>
        internal PowerShell LocalPowerShell { get; }

        /// <summary>
        /// Instance id by which this powershell driver is
        /// identified. This is the same as the id of the
        /// powershell on the client side.
        /// </summary>
        internal Guid InstanceId { get; }

        /// <summary>
        /// Server remote host.
        /// </summary>
        internal ServerRemoteHost RemoteHost { get; }

        /// <summary>
        /// Serialization options for the streams in this powershell.
        /// </summary>
        internal RemoteStreamOptions RemoteStreamOptions { get; }

        /// <summary>
        /// Id of the runspace pool driver which created
        /// this object. This is the same as the id of
        /// the runspace pool at the client side which
        /// is associated with the powershell on the
        /// client side.
        /// </summary>
        internal Guid RunspacePoolId { get; }

        /// <summary>
        /// ServerPowerShellDataStructureHandler associated with this
        /// powershell driver.
        /// </summary>
        internal ServerPowerShellDataStructureHandler DataStructureHandler { get; }

        /// <summary>
        /// Pipeline invocation state.
        /// </summary>
        internal PSInvocationState PipelineState { get; private set; }

        /// <summary>
        /// Checks if the steppable pipeline has input.
        /// </summary>
        internal bool NoInput { get; }

        /// <summary>
        /// Steppablepipeline object.
        /// </summary>
        internal SteppablePipeline SteppablePipeline { get; set; }

        /// <summary>
        /// Synchronization object.
        /// </summary>
        internal object SyncObject { get; } = new object();

        /// <summary>
        /// Processing input.
        /// </summary>
        internal bool ProcessingInput { get; set; }

        /// <summary>
        /// Input enumerator.
        /// </summary>
        internal IEnumerator<object> InputEnumerator { get; }

        /// <summary>
        /// Input collection.
        /// </summary>
        internal PSDataCollection<object> Input { get; }

        /// <summary>
        /// Is the pipeline pulsed.
        /// </summary>
        internal bool Pulsed { get; set; }

        /// <summary>
        /// Total objects processed.
        /// </summary>
        internal int TotalObjectsProcessed { get; set; }

        /// <summary>
        /// Starts the exectution.
        /// </summary>
        internal void Start()
        {
            PipelineState = PSInvocationState.Running;

            _eventSubscriber.FireStartSteppablePipeline(this);

            _powershellInput?.Pulse();
        }

        #endregion Internal Methods

        #region DataStructure related event handling / processing

        /// <summary>
        /// Close the input collection of the local powershell.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="eventArgs">Arguments describing this event.</param>
        internal void HandleInputEndReceived(object sender, EventArgs eventArgs)
        {
            Input.Complete();

            CheckAndPulseForProcessing(true);

            _powershellInput?.Pulse();
        }

        private void HandleSessionConnected(object sender, EventArgs eventArgs)
        {
            // Close input if its active. no need to synchronize as input stream would have already been processed
            // when connect call came into PS plugin
            Input?.Complete();
        }

        /// <summary>
        /// Handle a host message response received.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="eventArgs">Arguments describing this event.</param>
        internal void HandleHostResponseReceived(object sender, RemoteDataEventArgs<RemoteHostResponse> eventArgs)
        {
            RemoteHost.ServerMethodExecutor.HandleRemoteHostResponseFromClient(eventArgs.Data);
        }

        /// <summary>
        /// Stop the local powershell.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="eventArgs">Unused.</param>
        private void HandleStopReceived(object sender, EventArgs eventArgs)
        {
            lock (SyncObject)
            {
                PipelineState = PSInvocationState.Stopping;
            }

            PerformStop();

            _powershellInput?.Pulse();
        }

        /// <summary>
        /// Add input to the local powershell's input collection.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="eventArgs">Arguments describing this event.</param>
        private void HandleInputReceived(object sender, RemoteDataEventArgs<object> eventArgs)
        {
            Dbg.Assert(!NoInput, "Input data should not be received for powershells created with no input");

            if (Input != null)
            {
                lock (SyncObject)
                {
                    Input.Add(eventArgs.Data);
                }

                CheckAndPulseForProcessing(false);

                _powershellInput?.Pulse();
            }
        }

        /// <summary>
        /// Checks if there is any pending input that needs processing. If so, triggers RunProcessRecord
        /// event. The pipeline execution thread catches this and calls us back when the pipeline is
        /// suspended.
        /// </summary>
        /// <param name="complete"></param>
        internal void CheckAndPulseForProcessing(bool complete)
        {
            if (complete)
            {
                _eventSubscriber.FireHandleProcessRecord(this);
            }
            else if (!Pulsed)
            {
                bool shouldPulse = false;
                lock (SyncObject)
                {
                    if (Pulsed)
                    {
                        return;
                    }

                    if (!ProcessingInput && ((Input.Count > TotalObjectsProcessed)))
                    {
                        shouldPulse = true;
                        Pulsed = true;
                    }
                }

                if (shouldPulse && (PipelineState == PSInvocationState.Running))
                {
                    _eventSubscriber.FireHandleProcessRecord(this);
                }
            }
        }

        /// <summary>
        /// Performs the stop operation.
        /// </summary>
        internal void PerformStop()
        {
            bool shouldPerformStop = false;
            lock (SyncObject)
            {
                if (!ProcessingInput && (PipelineState == PSInvocationState.Stopping))
                {
                    shouldPerformStop = true;
                }
            }

            if (shouldPerformStop)
            {
                SetState(PSInvocationState.Stopped, new PipelineStoppedException());
            }
        }

        /// <summary>
        /// Changes state and sends message to the client as needed.
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="reason"></param>
        internal void SetState(PSInvocationState newState, Exception reason)
        {
            PSInvocationState copyState = PSInvocationState.NotStarted;
            bool shouldRaiseEvents = false;
            lock (SyncObject)
            {
                switch (PipelineState)
                {
                    case PSInvocationState.NotStarted:
                        {
                            switch (newState)
                            {
                                case PSInvocationState.Running:
                                case PSInvocationState.Stopping:
                                case PSInvocationState.Completed:
                                case PSInvocationState.Stopped:
                                case PSInvocationState.Failed:
                                    copyState = newState;
                                    // NotStarted -> Running..we dont send
                                    // state back to client.
                                    break;
                            }
                        }

                        break;

                    case PSInvocationState.Running:
                        {
                            switch (newState)
                            {
                                case PSInvocationState.NotStarted:
                                    throw new InvalidOperationException();
                                case PSInvocationState.Running:
                                    break;
                                case PSInvocationState.Stopping:
                                    copyState = newState;
                                    break;
                                case PSInvocationState.Completed:
                                case PSInvocationState.Stopped:
                                case PSInvocationState.Failed:
                                    copyState = newState;
                                    shouldRaiseEvents = true;
                                    break;
                            }
                        }

                        break;

                    case PSInvocationState.Stopping:
                        {
                            switch (newState)
                            {
                                case PSInvocationState.Completed:
                                case PSInvocationState.Failed:
                                    copyState = PSInvocationState.Stopped;
                                    shouldRaiseEvents = true;
                                    break;
                                case PSInvocationState.Stopped:
                                    copyState = newState;
                                    shouldRaiseEvents = true;
                                    break;
                                default:
                                    throw new InvalidOperationException();
                            }
                        }

                        break;

                    case PSInvocationState.Stopped:
                    case PSInvocationState.Completed:
                    case PSInvocationState.Failed:
                        break;
                }

                PipelineState = copyState;
            }

            if (shouldRaiseEvents)
            {
                // send the state change notification to the client
                DataStructureHandler.SendStateChangedInformationToClient(
                    new PSInvocationStateInfo(copyState, reason));
            }

            if (PipelineState == PSInvocationState.Completed
                || PipelineState == PSInvocationState.Stopped
                || PipelineState == PSInvocationState.Failed)
            {
                // Remove itself from the runspace pool
                DataStructureHandler.RaiseRemoveAssociationEvent();
            }
        }

        #endregion
    }
}
