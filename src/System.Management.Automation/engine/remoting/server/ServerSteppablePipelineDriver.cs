/********************************************************************++
 * Copyright (c) Microsoft Corporation.  All rights reserved.
 * --********************************************************************/
using System;
using System.Threading;
using System.Collections.Generic;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Remoting;
using System.Management.Automation.Host;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// Execution context used for stepping
    /// </summary>
    internal class ExecutionContextForStepping : IDisposable
    {
        private ExecutionContext executionContext;
        private PSInformationalBuffers originalInformationalBuffers;
        private PSHost originalHost;

        private ExecutionContextForStepping(ExecutionContext ctxt)
        {
            Dbg.Assert(ctxt != null, "ExecutionContext cannot be null.");
            executionContext = ctxt;
        }

        internal static ExecutionContextForStepping PrepareExecutionContext(
            ExecutionContext ctxt,
            PSInformationalBuffers newBuffers,
            PSHost newHost)
        {
            ExecutionContextForStepping result = new ExecutionContextForStepping(ctxt);

            result.originalInformationalBuffers
                = ctxt.InternalHost.InternalUI.GetInformationalMessageBuffers();
            result.originalHost = ctxt.InternalHost.ExternalHost;

            ctxt.InternalHost.
                    InternalUI.SetInformationalMessageBuffers(newBuffers);
            ctxt.InternalHost.SetHostRef(newHost);

            return result;
        }

        // Summary:
        //     Performs application-defined tasks associated with freeing, releasing, or
        //     resetting unmanaged resources.
        void IDisposable.Dispose()
        {
            executionContext.InternalHost.
                    InternalUI.SetInformationalMessageBuffers(originalInformationalBuffers);
            executionContext.InternalHost.SetHostRef(originalHost);
            GC.SuppressFinalize(this);
        }   
    }

    /// <summary>
    /// This class wraps a RunspacePoolInternal object. It is used to function
    /// as a server side runspacepool
    /// </summary>
    internal class ServerSteppablePipelineDriver
    {
        #region Private Data

        private PowerShell localPowerShell;
        private Guid clientPowerShellId;        // the client PowerShell's guid 
        // that is associated with this
        // powershell driver
        private Guid clientRunspacePoolId;      // the id of the client runspace pool
        // associated with this powershell
        private ServerPowerShellDataStructureHandler dsHandler;
        // data structure handler object to handle all
        // communications with the client
        private PSDataCollection<object> input; // input for local powershell invocation
        private IEnumerator<object> inputEnumerator;
        //private bool datasent = false;          // if the remaining data has been sent
        // to the client before sending state
        // information
        private object syncObject = new object(); // sync object for synchronizing sending
        // data to client
        private bool noInput;                   // there is no input when this driver 
        // was created
        private bool addToHistory;
        private ServerRemoteHost remoteHost;   // the server remote host instance
        // associated with this powershell
 #if !CORECLR // No ApartmentState In CoreCLR       
        private ApartmentState apartmentState;  // apartment state for this powershell
#endif
        private RemoteStreamOptions remoteStreamOptions;  // serialization options for the streams in this powershell

        private int totalObjectsProcessedSoFar;
        // pipeline that runs the actual command.
        private SteppablePipeline steppablePipeline;
        private bool isProcessingInput;
        private bool isPulsed;
        private PSInvocationState stateOfSteppablePipeline;
        private ServerSteppablePipelineSubscriber eventSubscriber;
        private PSDataCollection<object> powershellInput; // input collection of the PowerShell pipeline

        #endregion

#if CORECLR // No ApartmentState In CoreCLR
        /// <summary>
        /// Default constructor for creating ServerSteppablePipelineDriver...Used by server to concurrently
        /// run 2 pipelines.
        /// </summary>
        /// <param name="powershell">decoded powershell object</param>
        /// <param name="noInput">whether there is input for this powershell</param>
        /// <param name="clientPowerShellId">the client powershell id</param>
        /// <param name="clientRunspacePoolId">the client runspacepool id</param>
        /// <param name="runspacePoolDriver">runspace pool driver 
        /// which is creating this powershell driver</param>        
        /// <param name="hostInfo">host info using which the host for
        /// this powershell will be constructed</param>
        /// <param name="streamOptions">serialization options for the streams in this powershell</param>
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
        /// <param name="powershellInput">input collection of the PowerShell pipeline</param>
        internal ServerSteppablePipelineDriver(PowerShell powershell, bool noInput, Guid clientPowerShellId, 
            Guid clientRunspacePoolId, ServerRunspacePoolDriver runspacePoolDriver,
            HostInfo hostInfo, RemoteStreamOptions streamOptions,
            bool addToHistory, Runspace rsToUse, ServerSteppablePipelineSubscriber eventSubscriber, PSDataCollection<object> powershellInput)
#else
        /// <summary>
        /// Default constructor for creating ServerSteppablePipelineDriver...Used by server to concurrently
        /// run 2 pipelines.
        /// </summary>
        /// <param name="powershell">decoded powershell object</param>
        /// <param name="noInput">whether there is input for this powershell</param>
        /// <param name="clientPowerShellId">the client powershell id</param>
        /// <param name="clientRunspacePoolId">the client runspacepool id</param>
        /// <param name="runspacePoolDriver">runspace pool driver 
        /// which is creating this powershell driver</param>
        /// <param name="apartmentState">apartment state for this powershell</param>
        /// <param name="hostInfo">host info using which the host for
        /// this powershell will be constructed</param>
        /// <param name="streamOptions">serialization options for the streams in this powershell</param>
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
        /// <param name="powershellInput">input collection of the PowerShell pipeline</param>
        internal ServerSteppablePipelineDriver(PowerShell powershell, bool noInput, Guid clientPowerShellId,
            Guid clientRunspacePoolId, ServerRunspacePoolDriver runspacePoolDriver,
            ApartmentState apartmentState, HostInfo hostInfo, RemoteStreamOptions streamOptions,
            bool addToHistory, Runspace rsToUse, ServerSteppablePipelineSubscriber eventSubscriber, PSDataCollection<object> powershellInput)
#endif
        {
            this.localPowerShell = powershell;
            this.clientPowerShellId = clientPowerShellId;
            this.clientRunspacePoolId = clientRunspacePoolId;
            this.remoteStreamOptions = streamOptions;
#if !CORECLR // No ApartmentState In CoreCLR
            this.apartmentState = apartmentState;
#endif
            this.noInput = noInput;
            this.addToHistory = addToHistory;
            this.eventSubscriber = eventSubscriber;
            this.powershellInput = powershellInput;

            input = new PSDataCollection<object>();
            inputEnumerator = input.GetEnumerator();
            input.ReleaseOnEnumeration = true;

            this.dsHandler = runspacePoolDriver.DataStructureHandler.CreatePowerShellDataStructureHandler(clientPowerShellId, clientRunspacePoolId, remoteStreamOptions, null);
            this.remoteHost = dsHandler.GetHostAssociatedWithPowerShell(hostInfo, runspacePoolDriver.ServerRemoteHost);

            // subscribe to various data structure handler events
            dsHandler.InputEndReceived += new EventHandler(HandleInputEndReceived);
            dsHandler.InputReceived += new EventHandler<RemoteDataEventArgs<object>>(HandleInputReceived);
            dsHandler.StopPowerShellReceived += new EventHandler(HandleStopReceived);
            dsHandler.HostResponseReceived +=
                new EventHandler<RemoteDataEventArgs<RemoteHostResponse>>(HandleHostResponseReceived);
            dsHandler.OnSessionConnected +=new EventHandler(HandleSessionConnected);

            if (rsToUse == null)
            {
                throw PSTraceSource.NewInvalidOperationException(RemotingErrorIdStrings.NestedPipelineMissingRunspace);
            }

            // else, set the runspace pool and invoke this powershell
            localPowerShell.Runspace = rsToUse;
            eventSubscriber.SubscribeEvents(this);

            stateOfSteppablePipeline = PSInvocationState.NotStarted;
        }

        #region Internal Methods

        /// <summary>
        /// Local PowerShell instance
        /// </summary>
        internal PowerShell LocalPowerShell
        {
            get { return localPowerShell; }
        }

        /// <summary>
        /// Instance id by which this powershell driver is 
        /// identified. This is the same as the id of the
        /// powershell on the client side
        /// </summary>
        internal Guid InstanceId
        {
            get { return clientPowerShellId; }
        }

        /// <summary>
        /// Server remote host
        /// </summary>
        internal ServerRemoteHost RemoteHost
        {
            get { return remoteHost; }
        }

        /// <summary>
        /// Serialization options for the streams in this powershell
        /// </summary>
        internal RemoteStreamOptions RemoteStreamOptions
        {
            get { return this.remoteStreamOptions; }
        }

        /// <summary>
        /// Id of the runspace pool driver which created
        /// this object. This is the same as the id of 
        /// the runspace pool at the client side which
        /// is associated with the powershell on the 
        /// client side
        /// </summary>
        internal Guid RunspacePoolId
        {
            get { return clientRunspacePoolId; }
        }

        /// <summary>
        /// ServerPowerShellDataStructureHandler associated with this
        /// powershell driver
        /// </summary>
        internal ServerPowerShellDataStructureHandler DataStructureHandler
        {
            get { return dsHandler; }
        }

        /// <summary>
        /// Pipeline invocation state
        /// </summary>
        internal PSInvocationState PipelineState
        {
            get { return stateOfSteppablePipeline; }
        }

        /// <summary>
        /// Checks if the steppable pipeline has input
        /// </summary>
        internal bool NoInput
        {
            get { return noInput; }
        }

        /// <summary>
        /// Steppablepipeline object
        /// </summary>
        internal SteppablePipeline SteppablePipeline
        {
            get { return steppablePipeline; }
            set { steppablePipeline = value; }
        }

        /// <summary>
        /// Synchronization object
        /// </summary>
        internal object SyncObject
        {
            get { return syncObject; }
        }

        /// <summary>
        /// Processing input
        /// </summary>
        internal bool ProcessingInput
        {
            get { return isProcessingInput; }
            set { isProcessingInput = value; }
        }

        /// <summary>
        /// Input enumerator
        /// </summary>
        internal IEnumerator<object> InputEnumerator
        {
            get { return inputEnumerator; }
        }

        /// <summary>
        /// Input collection
        /// </summary>
        internal PSDataCollection<object> Input
        {
            get { return input; }
        }

        /// <summary>
        /// Is the pipeline pulsed
        /// </summary>
        internal bool Pulsed
        {
            get { return isPulsed; }
            set { isPulsed = value; }
        }

        /// <summary>
        /// Total objects processed
        /// </summary>
        internal int TotalObjectsProcessed
        {
            get { return totalObjectsProcessedSoFar; }
            set { totalObjectsProcessedSoFar = value; }
        }

        /// <summary>
        /// Starts the exectution
        /// </summary>
        internal void Start()
        {
            stateOfSteppablePipeline = PSInvocationState.Running;

            eventSubscriber.FireStartSteppablePipeline(this);

            if (powershellInput != null)
            {
                powershellInput.Pulse();
            }
        }

        #endregion Internal Methods

        #region DataStructure related event handling / processing

        /// <summary>
        /// Close the input collection of the local powershell
        /// </summary>
        /// <param name="sender">sender of this event, unused</param>
        /// <param name="eventArgs">arguments describing this event</param>
        internal void HandleInputEndReceived(object sender, EventArgs eventArgs)
        {
            input.Complete();

            CheckAndPulseForProcessing(true);

            if (powershellInput != null)
            {
                powershellInput.Pulse();
            }
        }

        private void HandleSessionConnected(object sender, EventArgs eventArgs)
        {
            //Close input if its active. no need to synchronize as input stream would have already been processed
            // when connect call came into PS plugin
            if (input != null)
            {
                input.Complete();
            }
        }

        /// <summary>
        /// Handle a host message response received
        /// </summary>
        /// <param name="sender">sender of this event, unused</param>
        /// <param name="eventArgs">arguments describing this event</param>
        internal void HandleHostResponseReceived(object sender, RemoteDataEventArgs<RemoteHostResponse> eventArgs)
        {
            remoteHost.ServerMethodExecutor.HandleRemoteHostResponseFromClient(eventArgs.Data);
        }

        /// <summary>
        /// Stop the local powershell
        /// </summary>
        /// <param name="sender">sender of this event, unused</param>
        /// <param name="eventArgs">unused</param>
        private void HandleStopReceived(object sender, EventArgs eventArgs)
        {
            lock (syncObject)
            {
                stateOfSteppablePipeline = PSInvocationState.Stopping;
            }

            PerformStop();

            if (powershellInput != null)
            {
                powershellInput.Pulse();
            }
        }

        /// <summary>
        /// Add input to the local powershell's input collection
        /// </summary>
        /// <param name="sender">sender of this event, unused</param>
        /// <param name="eventArgs">arguments describing this event</param>
        private void HandleInputReceived(object sender, RemoteDataEventArgs<object> eventArgs)
        {
            Dbg.Assert(!noInput, "Input data should not be received for powershells created with no input");

            if (input != null)
            {
                lock (syncObject)
                {
                    input.Add(eventArgs.Data);
                }
                CheckAndPulseForProcessing(false);

                if (powershellInput != null)
                {
                    powershellInput.Pulse();
                }
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
                eventSubscriber.FireHandleProcessRecord(this);
            }
            else if (!isPulsed)
            {
                bool shouldPulse = false;
                lock (syncObject)
                {
                    if (isPulsed)
                    {
                        return;
                    }

                    if (!isProcessingInput && ((input.Count > totalObjectsProcessedSoFar)))
                    {
                        shouldPulse = true;
                        isPulsed = true;
                    }
                }

                if (shouldPulse && (stateOfSteppablePipeline == PSInvocationState.Running))
                {
                    eventSubscriber.FireHandleProcessRecord(this);
                }
            }
        }

        /// <summary>
        /// Performs the stop operation
        /// </summary>
        internal void PerformStop()
        {
            bool shouldPerformStop = false;
            lock (syncObject)
            {
                if (!isProcessingInput && (stateOfSteppablePipeline == PSInvocationState.Stopping))
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
            bool shoulRaiseEvents = false;
            lock (syncObject)
            {
                switch (stateOfSteppablePipeline)
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
                                    shoulRaiseEvents = true;
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
                                    shoulRaiseEvents = true;
                                    break;
                                case PSInvocationState.Stopped:
                                    copyState = newState;
                                    shoulRaiseEvents = true;
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

                stateOfSteppablePipeline = copyState;
            }

            if (shoulRaiseEvents)
            {
                // send the state change notification to the client
                dsHandler.SendStateChangedInformationToClient(
                    new PSInvocationStateInfo(copyState, reason));
            }

            if (stateOfSteppablePipeline == PSInvocationState.Completed 
                || stateOfSteppablePipeline == PSInvocationState.Stopped
                || stateOfSteppablePipeline == PSInvocationState.Failed)
            {
                // Remove itself from the runspace pool
                dsHandler.RaiseRemoveAssociationEvent();
            }
        }

        #endregion
    }
}