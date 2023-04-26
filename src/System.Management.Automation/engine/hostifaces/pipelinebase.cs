// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation.Runspaces
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading;
    using Dbg = System.Management.Automation.Diagnostics;
    using System.Management.Automation.Internal;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings

    /// <summary>
    /// This class has common base implementation for Pipeline class.
    /// LocalPipeline and RemotePipeline classes derives from it.
    /// </summary>
    internal abstract class PipelineBase : Pipeline
    {
        #region constructors

        /// <summary>
        /// Create a pipeline initialized with a command string.
        /// </summary>
        /// <param name="runspace">The associated Runspace/></param>
        /// <param name="command">Command string.</param>
        /// <param name="addToHistory">If true, add pipeline to history.</param>
        /// <param name="isNested">True for nested pipeline.</param>
        /// <exception cref="ArgumentNullException">
        /// Command is null and add to history is true
        /// </exception>
        protected PipelineBase(Runspace runspace, string command, bool addToHistory, bool isNested)
            : base(runspace)
        {
            Initialize(runspace, command, addToHistory, isNested);

            // Initialize streams
            InputStream = new ObjectStream();
            OutputStream = new ObjectStream();
            ErrorStream = new ObjectStream();
        }

        /// <summary>
        /// Create a Pipeline with an existing command string.
        /// Caller should validate all the parameters.
        /// </summary>
        /// <param name="runspace">
        /// The LocalRunspace to associate with this pipeline.
        /// </param>
        /// <param name="command">
        /// The command to invoke.
        /// </param>
        /// <param name="addToHistory">
        /// If true, add the command to history.
        /// </param>
        /// <param name="isNested">
        /// If true, mark this pipeline as a nested pipeline.
        /// </param>
        /// <param name="inputStream">
        /// Stream to use for reading input objects.
        /// </param>
        /// <param name="errorStream">
        /// Stream to use for writing error objects.
        /// </param>
        /// <param name="outputStream">
        /// Stream to use for writing output objects.
        /// </param>
        /// <param name="infoBuffers">
        /// Buffers used to write progress, verbose, debug, warning, information
        /// information of an invocation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Command is null and add to history is true
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// 1. InformationalBuffers is null
        /// </exception>
        protected PipelineBase(Runspace runspace,
            CommandCollection command,
            bool addToHistory,
            bool isNested,
            ObjectStreamBase inputStream,
            ObjectStreamBase outputStream,
            ObjectStreamBase errorStream,
            PSInformationalBuffers infoBuffers)
            : base(runspace, command)
        {
            Dbg.Assert(inputStream != null, "Caller Should validate inputstream parameter");
            Dbg.Assert(outputStream != null, "Caller Should validate outputStream parameter");
            Dbg.Assert(errorStream != null, "Caller Should validate errorStream parameter");
            Dbg.Assert(infoBuffers != null, "Caller Should validate informationalBuffers parameter");
            Dbg.Assert(command != null, "Command cannot be null");

            // Since we are constructing this pipeline using a commandcollection we dont need
            // to add cmd to CommandCollection again (Initialize does this).. because of this
            // I am handling history here..
            Initialize(runspace, null, false, isNested);
            if (addToHistory)
            {
                // get command text for history..
                string cmdText = command.GetCommandStringForHistory();
                HistoryString = cmdText;
                AddToHistory = addToHistory;
            }

            // Initialize streams
            InputStream = inputStream;
            OutputStream = outputStream;
            ErrorStream = errorStream;
            InformationalBuffers = infoBuffers;
        }

        /// <summary>
        /// Copy constructor to support cloning.
        /// </summary>
        /// <param name="pipeline">The source pipeline.</param>
        /// <remarks>
        /// The copy constructor's intent is to support the scenario
        /// where a host needs to run the same set of commands multiple
        /// times.  This is accomplished via creating a master pipeline
        /// then cloning it and executing the cloned copy.
        /// </remarks>
        protected PipelineBase(PipelineBase pipeline)
            : this(pipeline.Runspace, null, false, pipeline.IsNested)
        {
            // NTRAID#Windows Out Of Band Releases-915851-2005/09/13
            if (pipeline == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(pipeline));
            }

            if (pipeline._disposed)
            {
                throw PSTraceSource.NewObjectDisposedException("pipeline");
            }

            AddToHistory = pipeline.AddToHistory;
            HistoryString = pipeline.HistoryString;
            foreach (Command command in pipeline.Commands)
            {
                Command clone = command.Clone();

                // Attach the cloned Command to this pipeline.
                Commands.Add(clone);
            }
        }

        #endregion constructors

        #region properties

        private Runspace _runspace;

        /// <summary>
        /// Access the runspace this pipeline is created on.
        /// </summary>
        public override Runspace Runspace
        {
            get
            {
                return _runspace;
            }
        }

        /// <summary>
        /// This internal method doesn't do the _disposed check.
        /// </summary>
        /// <returns></returns>
        internal Runspace GetRunspace()
        {
            return _runspace;
        }

        private bool _isNested;

        /// <summary>
        /// Is this pipeline nested.
        /// </summary>
        public override bool IsNested
        {
            get
            {
                return _isNested;
            }
        }

        /// <summary>
        /// Is this a pulse pipeline (created by the EventManager)
        /// </summary>
        internal bool IsPulsePipeline { get; set; }

        private PipelineStateInfo _pipelineStateInfo = new PipelineStateInfo(PipelineState.NotStarted);

        /// <summary>
        /// Info about current state of the pipeline.
        /// </summary>
        /// <remarks>
        /// This value indicates the state of the pipeline after the change.
        /// </remarks>
        public override PipelineStateInfo PipelineStateInfo
        {
            get
            {
                lock (SyncRoot)
                {
                    // Note:We do not return internal state.
                    return _pipelineStateInfo.Clone();
                }
            }
        }

        // 913921-2005/07/08 ObjectWriter can be retrieved on a closed stream
        /// <summary>
        /// Access the input writer for this pipeline.
        /// </summary>
        public override PipelineWriter Input
        {
            get
            {
                return InputStream.ObjectWriter;
            }
        }

        /// <summary>
        /// Access the output reader for this pipeline.
        /// </summary>
        public override PipelineReader<PSObject> Output
        {
            get
            {
                return OutputStream.PSObjectReader;
            }
        }

        /// <summary>
        /// Access the error output reader for this pipeline.
        /// </summary>
        /// <remarks>
        /// This is the non-terminating error stream from the command.
        /// In this release, the objects read from this PipelineReader
        /// are PSObjects wrapping ErrorRecords.
        /// </remarks>
        public override PipelineReader<object> Error
        {
            get
            {
                return _errorStream.ObjectReader;
            }
        }

        /// <summary>
        /// Is this pipeline a child pipeline?
        ///
        /// IsChild flag makes it possible for the pipeline to differentiate between
        /// a true v1 nested pipeline and the cmdlets calling cmdlets case. See bug
        /// 211462.
        /// </summary>
        internal override bool IsChild { get; set; }

        #endregion properties

        #region stop

        /// <summary>
        /// Synchronous call to stop the running pipeline.
        /// </summary>
        public override void Stop()
        {
            CoreStop(true);
        }

        /// <summary>
        /// Asynchronous call to stop the running pipeline.
        /// </summary>
        public override void StopAsync()
        {
            CoreStop(false);
        }

        /// <summary>
        /// Stop the running pipeline.
        /// </summary>
        /// <param name="syncCall">If true pipeline is stopped synchronously
        /// else asynchronously.</param>
        private void CoreStop(bool syncCall)
        {
            // Is pipeline already in stopping state.
            bool alreadyStopping = false;
            lock (SyncRoot)
            {
                switch (PipelineState)
                {
                    case PipelineState.NotStarted:
                        SetPipelineState(PipelineState.Stopping);
                        SetPipelineState(PipelineState.Stopped);
                        break;

                    // If pipeline execution has failed or completed or
                    // stopped, return silently.
                    case PipelineState.Stopped:
                    case PipelineState.Completed:
                    case PipelineState.Failed:
                        return;
                    // If pipeline is in Stopping state, ignore the second
                    // stop.
                    case PipelineState.Stopping:
                        alreadyStopping = true;
                        break;

                    case PipelineState.Running:
                        SetPipelineState(PipelineState.Stopping);
                        break;
                }
            }

            // If pipeline is already in stopping state. Wait for pipeline
            // to finish. We do need to raise any events here as no
            // change of state has occurred.
            if (alreadyStopping)
            {
                if (syncCall)
                {
                    PipelineFinishedEvent.WaitOne();
                }

                return;
            }

            // Raise the event outside the lock
            RaisePipelineStateEvents();

            // A pipeline can be stopped before it is started. See NotStarted
            // case in above switch statement. This is done to allow stoping a pipeline
            // in another thread before it has been started.
            lock (SyncRoot)
            {
                if (PipelineState == PipelineState.Stopped)
                {
                    // Note:if we have reached here, Stopped state was set
                    // in PipelineState.NotStarted case above. Only other
                    // way Stopped can be set when this method calls
                    // StopHelper below
                    return;
                }
            }

            // Start stop operation in derived class
            ImplementStop(syncCall);
        }

        /// <summary>
        /// Stop execution of pipeline.
        /// </summary>
        /// <param name="syncCall">If false, call is asynchronous.</param>
        protected abstract void ImplementStop(bool syncCall);

        #endregion stop

        #region invoke

        /// <summary>
        /// Invoke the pipeline, synchronously, returning the results as an
        /// array of objects.
        /// </summary>
        /// <param name="input">an array of input objects to pass to the pipeline.
        /// Array may be empty but may not be null</param>
        /// <returns>An array of zero or more result objects.</returns>
        /// <remarks>Caller of synchronous exectute should not close
        /// input objectWriter. Synchronous invoke will always close the input
        /// objectWriter.
        ///
        /// On Synchronous Invoke if output is throttled and no one is reading from
        /// output pipe, Execution will block after buffer is full.
        /// </remarks>
        public override Collection<PSObject> Invoke(IEnumerable input)
        {
            // NTRAID#Windows Out Of Band Releases-915851-2005/09/13
            if (_disposed)
            {
                throw PSTraceSource.NewObjectDisposedException("pipeline");
            }

            CoreInvoke(input, true);

            // Wait for pipeline to finish execution
            PipelineFinishedEvent.WaitOne();

            if (SyncInvokeCall)
            {
                // Raise the pipeline completion events. These events are set in
                // pipeline execution thread. However for Synchronous execution
                // we raise the event in the main thread.
                RaisePipelineStateEvents();
            }

            if (PipelineStateInfo.State == PipelineState.Stopped)
            {
                return new Collection<PSObject>();
            }
            else if (PipelineStateInfo.State == PipelineState.Failed && PipelineStateInfo.Reason != null)
            {
                // If this is an error pipe for a hosting applicationand we are logging,
                // then log the error.
                if (this.Runspace.GetExecutionContext.EngineHostInterface.UI.IsTranscribing)
                {
                    this.Runspace.ExecutionContext.InternalHost.UI.TranscribeResult(this.Runspace, PipelineStateInfo.Reason.Message);
                }

                throw PipelineStateInfo.Reason;
            }

            // Execution completed successfully
            // 2004/06/30-JonN was ReadAll() which was non-blocking
            return Output.NonBlockingRead(Int32.MaxValue);
        }

        /// <summary>
        /// Invoke the pipeline asynchronously.
        /// </summary>
        /// <remarks>
        /// Results are returned through the <see cref="Pipeline.Output"/> reader.
        /// </remarks>
        public override void InvokeAsync()
        {
            CoreInvoke(null, false);
        }

        /// <summary>
        /// This parameter is true if Invoke is called.
        /// It is false if InvokeAsync is called.
        /// </summary>
        protected bool SyncInvokeCall { get; private set; }

        /// <summary>
        /// Invoke the pipeline asynchronously with input.
        /// </summary>
        /// <param name="input">input to provide to pipeline. Input is
        /// used only for synchronous execution</param>
        /// <param name="syncCall">True if this method is called from
        /// synchronous invoke else false</param>
        /// <remarks>
        /// Results are returned through the <see cref="Pipeline.Output"/> reader.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// No command is added to pipeline
        /// </exception>
        /// <exception cref="InvalidPipelineStateException">
        /// PipelineState is not NotStarted.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// 1) A pipeline is already executing. Pipeline cannot execute
        /// concurrently.
        /// 2) InvokeAsync is called on nested pipeline. Nested pipeline
        /// cannot be executed Asynchronously.
        /// 3) Attempt is made to invoke a nested pipeline directly. Nested
        /// pipeline must be invoked from a running pipeline.
        /// </exception>
        /// <exception cref="InvalidRunspaceStateException">
        /// RunspaceState is not Open
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Pipeline already disposed
        /// </exception>
        private void CoreInvoke(IEnumerable input, bool syncCall)
        {
            lock (SyncRoot)
            {
                // NTRAID#Windows Out Of Band Releases-915851-2005/09/13
                if (_disposed)
                {
                    throw PSTraceSource.NewObjectDisposedException("pipeline");
                }

                if (Commands == null || Commands.Count == 0)
                {
                    throw PSTraceSource.NewInvalidOperationException(
                            RunspaceStrings.NoCommandInPipeline);
                }

                if (PipelineState != PipelineState.NotStarted)
                {
                    InvalidPipelineStateException e =
                        new InvalidPipelineStateException
                        (
                            StringUtil.Format(RunspaceStrings.PipelineReInvokeNotAllowed),
                            PipelineState,
                            PipelineState.NotStarted
                        );
                    throw e;
                }

                if (syncCall
                    && InputStream is not PSDataCollectionStream<PSObject>
                    && InputStream is not PSDataCollectionStream<object>)
                {
                    // Method is called from synchronous invoke.
                    if (input != null)
                    {
                        // TO-DO-Add a test make sure that ObjectDisposed
                        // exception is thrown
                        // Write input data in to inputStream and close the input
                        // pipe. If Input stream is already closed an
                        // ObjectDisposed exception will be thrown
                        foreach (object temp in input)
                        {
                            InputStream.Write(temp);
                        }
                    }

                    InputStream.Close();
                }

                SyncInvokeCall = syncCall;

                // Create event which will be signalled when pipeline execution
                // is completed/failed/stopped.
                // Note:Runspace.Close waits for all the running pipeline
                // to finish.  This Event must be created before pipeline is
                // added to list of running pipelines. This avoids the race condition
                // where Close is called after pipeline is added to list of
                // running pipeline but before event is created.
                PipelineFinishedEvent = new ManualResetEvent(false);

                // 1) Do the check to ensure that pipeline no other
                // pipeline is running.
                // 2) Runspace object maintains a list of pipelines in
                // execution. Add this pipeline to the list.
                RunspaceBase.DoConcurrentCheckAndAddToRunningPipelines(this, syncCall);

                // Note: Set PipelineState to Running only after adding pipeline to list
                // of pipelines in execution. AddForExecution checks that runspace is in
                // state where pipeline can be run.
                // StartPipelineExecution raises this event. See Windows Bug 1160481 for
                // more details.
                SetPipelineState(PipelineState.Running);
            }

            try
            {
                // Let the derived class start the pipeline execution.
                StartPipelineExecution();
            }
            catch (Exception exception)
            {
                // If we fail in any of the above three steps, set the correct states.
                RunspaceBase.RemoveFromRunningPipelineList(this);
                SetPipelineState(PipelineState.Failed, exception);

                // Note: we are not raising the events in this case. However this is
                // fine as user is getting the exception.
                throw;
            }
        }

        /// <summary>
        /// Invokes a remote command and immediately disconnects if
        /// transport layer supports it.
        /// </summary>
        internal override void InvokeAsyncAndDisconnect()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Starts execution of pipeline.
        /// </summary>
        protected abstract void StartPipelineExecution();

        #region concurrent pipeline check

        private bool _performNestedCheck = true;

        /// <summary>
        /// For nested pipeline, system checks that Execute is called from
        /// currently executing pipeline.
        /// If PerformNestedCheck is false, this check is bypassed. This
        /// is set to true by remote provider. In remote provider case all
        /// the checks are done by the client proxy.
        /// </summary>
        internal bool PerformNestedCheck
        {
            set
            {
                _performNestedCheck = value;
            }
        }

        /// <summary>
        /// This is the thread on which NestedPipeline can be executed.
        /// In case of LocalPipeline, this is the thread of execution
        /// of LocalPipeline. In case of RemotePipeline, this is thread
        /// on which EnterNestedPrompt is called.
        /// RemotePipeline proxy should set it on at the beginning of
        /// EnterNestedPrompt and clear it on return.
        /// </summary>
        internal Thread NestedPipelineExecutionThread { get; set; }

        /// <summary>
        /// Check if anyother pipeline is executing.
        /// In case of nested pipeline, checks that it is called
        /// from currently executing pipeline's thread.
        /// </summary>
        /// <param name="syncCall">True if method is called from Invoke, false
        /// if called from InvokeAsync</param>
        /// <param name="syncObject">The sync object on which the lock is acquired.</param>
        /// <param name="isInLock">True if the method is invoked in a critical section.</param>
        /// <exception cref="InvalidOperationException">
        /// 1) A pipeline is already executing. Pipeline cannot execute
        /// concurrently.
        /// 2) InvokeAsync is called on nested pipeline. Nested pipeline
        /// cannot be executed Asynchronously.
        /// 3) Attempt is made to invoke a nested pipeline directly. Nested
        /// pipeline must be invoked from a running pipeline.
        /// </exception>
        internal void DoConcurrentCheck(bool syncCall, object syncObject, bool isInLock)
        {
            PipelineBase currentPipeline = (PipelineBase)RunspaceBase.GetCurrentlyRunningPipeline();

            if (!IsNested)
            {
                if (currentPipeline == null)
                {
                    return;
                }
                else
                {
                    // Detect if we're running a pulse pipeline, or we're running a nested pipeline
                    // in a pulse pipeline
                    if (currentPipeline == RunspaceBase.PulsePipeline ||
                        (currentPipeline.IsNested && RunspaceBase.PulsePipeline != null))
                    {
                        // If so, wait and try again
                        if (isInLock)
                        {
                            // If the method is invoked in the lock statement, release the
                            // lock before wait on the pulse pipeline
                            Monitor.Exit(syncObject);
                        }

                        try
                        {
                            RunspaceBase.WaitForFinishofPipelines();
                        }
                        finally
                        {
                            if (isInLock)
                            {
                                // If the method is invoked in the lock statement, acquire the
                                // lock before we carry on with the rest operations
                                Monitor.Enter(syncObject);
                            }
                        }

                        DoConcurrentCheck(syncCall, syncObject, isInLock);
                        return;
                    }

                    throw PSTraceSource.NewInvalidOperationException(
                            RunspaceStrings.ConcurrentInvokeNotAllowed);
                }
            }
            else
            {
                if (_performNestedCheck)
                {
                    if (!syncCall)
                    {
                        throw PSTraceSource.NewInvalidOperationException(
                                RunspaceStrings.NestedPipelineInvokeAsync);
                    }

                    if (currentPipeline == null)
                    {
                        if (this.IsChild)
                        {
                            // OK it's not really a nested pipeline but a call with RunspaceMode=UseCurrentRunspace
                            // This shouldn't fail so we'll clear the IsNested and IsChild flags and then return
                            // That way executions proceeds but everything gets clean up at the end when the pipeline completes
                            this.IsChild = false;
                            _isNested = false;
                            return;
                        }

                        throw PSTraceSource.NewInvalidOperationException(
                                RunspaceStrings.NestedPipelineNoParentPipeline);
                    }

                    Dbg.Assert(currentPipeline.NestedPipelineExecutionThread != null, "Current pipeline should always have NestedPipelineExecutionThread set");
                    Thread th = Thread.CurrentThread;

                    if (!currentPipeline.NestedPipelineExecutionThread.Equals(th))
                    {
                        throw PSTraceSource.NewInvalidOperationException(
                                RunspaceStrings.NestedPipelineNoParentPipeline);
                    }
                }
            }
        }

        #endregion concurrent pipeline check

        #endregion invoke

        #region Connect

        /// <summary>
        /// Connects synchronously to a running command on a remote server.
        /// The pipeline object must be in the disconnected state.
        /// </summary>
        /// <returns>A collection of result objects.</returns>
        public override Collection<PSObject> Connect()
        {
            // Connect semantics not supported on local (non-remoting) pipelines.
            throw PSTraceSource.NewNotSupportedException(PipelineStrings.ConnectNotSupported);
        }

        /// <summary>
        /// Connects asynchronously to a running command on a remote server.
        /// </summary>
        public override void ConnectAsync()
        {
            // Connect semantics not supported on local (non-remoting) pipelines.
            throw PSTraceSource.NewNotSupportedException(PipelineStrings.ConnectNotSupported);
        }

        #endregion

        #region state change event

        /// <summary>
        /// Event raised when Pipeline's state changes.
        /// </summary>
        public override event EventHandler<PipelineStateEventArgs> StateChanged = null;

        /// <summary>
        /// Current state of the pipeline.
        /// </summary>
        /// <remarks>
        /// This value indicates the state of the pipeline after the change.
        /// </remarks>
        protected PipelineState PipelineState
        {
            get
            {
                return _pipelineStateInfo.State;
            }
        }

        /// <summary>
        /// This returns true if pipeline state is Completed, Failed or Stopped.
        /// </summary>
        /// <returns></returns>
        protected bool IsPipelineFinished()
        {
            return (PipelineState == PipelineState.Completed ||
                    PipelineState == PipelineState.Failed ||
                    PipelineState == PipelineState.Stopped);
        }

        /// <summary>
        /// This is queue of all the state change event which have occurred for
        /// this pipeline. RaisePipelineStateEvents raises event for each
        /// item in this queue. We don't raise the event with in SetPipelineState
        /// because often SetPipelineState is called with in a lock.
        /// Raising event in lock introduces chances of deadlock in GUI applications.
        /// </summary>
        private Queue<ExecutionEventQueueItem> _executionEventQueue = new Queue<ExecutionEventQueueItem>();

        private sealed class ExecutionEventQueueItem
        {
            public ExecutionEventQueueItem(PipelineStateInfo pipelineStateInfo, RunspaceAvailability currentAvailability, RunspaceAvailability newAvailability)
            {
                this.PipelineStateInfo = pipelineStateInfo;
                this.CurrentRunspaceAvailability = currentAvailability;
                this.NewRunspaceAvailability = newAvailability;
            }

            public PipelineStateInfo PipelineStateInfo;
            public RunspaceAvailability CurrentRunspaceAvailability;
            public RunspaceAvailability NewRunspaceAvailability;
        }

        /// <summary>
        /// Sets the new execution state.
        /// </summary>
        /// <param name="state">The new state.</param>
        /// <param name="reason">
        /// An exception indicating that state change is the result of an error,
        /// otherwise; null.
        /// </param>
        /// <remarks>
        /// Sets the internal execution state information member variable. It
        /// also adds PipelineStateInfo to a queue. RaisePipelineStateEvents
        /// raises event for each item in this queue.
        /// </remarks>
        protected void SetPipelineState(PipelineState state, Exception reason)
        {
            lock (SyncRoot)
            {
                if (state != PipelineState)
                {
                    _pipelineStateInfo = new PipelineStateInfo(state, reason);

                    // Add _pipelineStateInfo to _executionEventQueue.
                    // RaisePipelineStateEvents will raise event for each item
                    // in this queue.
                    // Note:We are doing clone here instead of passing the member
                    // _pipelineStateInfo because we donot want outside
                    // to change pipeline state.
                    RunspaceAvailability previousAvailability = _runspace.RunspaceAvailability;

                    _runspace.UpdateRunspaceAvailability(_pipelineStateInfo.State, false);

                    _executionEventQueue.Enqueue(
                        new ExecutionEventQueueItem(
                            _pipelineStateInfo.Clone(),
                            previousAvailability,
                            _runspace.RunspaceAvailability));
                }
            }
        }

        /// <summary>
        /// Set the new execution state.
        /// </summary>
        /// <param name="state">The new state.</param>
        protected void SetPipelineState(PipelineState state)
        {
            SetPipelineState(state, null);
        }

        /// <summary>
        /// Raises events for changes in execution state.
        /// </summary>
        protected void RaisePipelineStateEvents()
        {
            Queue<ExecutionEventQueueItem> tempEventQueue = null;
            EventHandler<PipelineStateEventArgs> stateChanged = null;
            bool runspaceHasAvailabilityChangedSubscribers = false;

            lock (SyncRoot)
            {
                stateChanged = this.StateChanged;
                runspaceHasAvailabilityChangedSubscribers = _runspace.HasAvailabilityChangedSubscribers;

                if (stateChanged != null || runspaceHasAvailabilityChangedSubscribers)
                {
                    tempEventQueue = _executionEventQueue;
                    _executionEventQueue = new Queue<ExecutionEventQueueItem>();
                }
                else
                {
                    // Clear the events if there are no EventHandlers. This
                    // ensures that events do not get called for state
                    // changes prior to their registration.
                    _executionEventQueue.Clear();
                }
            }

            if (tempEventQueue != null)
            {
                while (tempEventQueue.Count > 0)
                {
                    ExecutionEventQueueItem queueItem = tempEventQueue.Dequeue();

                    if (runspaceHasAvailabilityChangedSubscribers && queueItem.NewRunspaceAvailability != queueItem.CurrentRunspaceAvailability)
                    {
                        _runspace.RaiseAvailabilityChangedEvent(queueItem.NewRunspaceAvailability);
                    }

                    // this is shipped as part of V1. So disabling the warning here.
#pragma warning disable 56500
                    // Exception raised in the eventhandler are not error in pipeline.
                    // silently ignore them.
                    if (stateChanged != null)
                    {
                        try
                        {
                            stateChanged(this, new PipelineStateEventArgs(queueItem.PipelineStateInfo));
                        }
                        catch (Exception)
                        {
                        }
                    }
#pragma warning restore 56500
                }
            }
        }

        /// <summary>
        /// ManualResetEvent which is signaled when pipeline execution is
        /// completed/failed/stopped.
        /// </summary>
        internal ManualResetEvent PipelineFinishedEvent { get; private set; }

        #endregion

        #region streams

        /// <summary>
        /// OutputStream from PipelineProcessor. Host will read on
        /// ObjectReader of this stream. PipelineProcessor will write to
        /// ObjectWriter of this stream.
        /// </summary>
        protected ObjectStreamBase OutputStream { get; }

        private ObjectStreamBase _errorStream;
        /// <summary>
        /// ErrorStream from PipelineProcessor. Host will read on
        /// ObjectReader of this stream. PipelineProcessor will write to
        /// ObjectWriter of this stream.
        /// </summary>
        protected ObjectStreamBase ErrorStream
        {
            get
            {
                return _errorStream;
            }

            private set
            {
                Dbg.Assert(value != null, "ErrorStream cannot be null");
                _errorStream = value;
                _errorStream.DataReady += OnErrorStreamDataReady;
            }
        }

        // Winblue: 26115. This handler is used to populate Pipeline.HadErrors.
        private void OnErrorStreamDataReady(object sender, EventArgs e)
        {
            if (_errorStream.Count > 0)
            {
                // unsubscribe from further event notifications as
                // this notification is suffice to say there is an
                // error.
                _errorStream.DataReady -= OnErrorStreamDataReady;
                SetHadErrors(true);
            }
        }

        /// <summary>
        /// Informational Buffers that represent verbose, debug, progress,
        /// warning emanating from the command execution.
        /// </summary>
        /// <remarks>
        /// Informational buffers are introduced after 1.0. This can be
        /// null if executing command as part of 1.0 hosting interfaces.
        /// </remarks>
        protected PSInformationalBuffers InformationalBuffers { get; }

        /// <summary>
        /// Stream for providing input to PipelineProcessor. Host will write on
        /// ObjectWriter of this stream. PipelineProcessor will read from
        /// ObjectReader of this stream.
        /// </summary>
        protected ObjectStreamBase InputStream { get; }

        #endregion streams

        #region history

        // History information is internal so that Pipeline serialization code
        // can access it.

        /// <summary>
        /// If true, this pipeline is added in history.
        /// </summary>
        internal bool AddToHistory { get; set; }

        /// <summary>
        /// String which is added in the history.
        /// </summary>
        /// <remarks>This needs to be internal so that it can be replaced
        /// by invoke-cmd to place correct string in history.</remarks>
        internal string HistoryString { get; set; }

        #endregion history

        #region misc

        /// <summary>
        /// Initialized the current pipeline instance with the supplied data.
        /// </summary>
        /// <param name="runspace"></param>
        /// <param name="command"></param>
        /// <param name="addToHistory"></param>
        /// <param name="isNested"></param>
        /// <exception cref="ArgumentNullException">
        /// 1. addToHistory is true and command is null.
        /// </exception>
        private void Initialize(Runspace runspace, string command, bool addToHistory, bool isNested)
        {
            Dbg.Assert(runspace != null, "caller should validate the parameter");
            _runspace = runspace;

            _isNested = isNested;

            if (addToHistory && command == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(command));
            }

            if (command != null)
            {
                Commands.Add(new Command(command, true, false));
            }

            AddToHistory = addToHistory;
            if (AddToHistory)
            {
                HistoryString = command;
            }
        }

        private RunspaceBase RunspaceBase
        {
            get
            {
                return (RunspaceBase)Runspace;
            }
        }

        /// <summary>
        /// Object used for synchronization.
        /// </summary>
        protected internal object SyncRoot { get; } = new object();

        #endregion misc

        #region IDisposable Members

        /// <summary>
        /// Set to true when object is disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Protected dispose which can be overridden by derived classes.
        /// </summary>
        /// <param name="disposing"></param>
        protected override
        void
        Dispose(bool disposing)
        {
            try
            {
                if (!_disposed)
                {
                    _disposed = true;
                    if (disposing)
                    {
                        InputStream.Close();
                        OutputStream.Close();

                        _errorStream.DataReady -= OnErrorStreamDataReady;
                        _errorStream.Close();

                        _executionEventQueue.Clear();
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        #endregion IDisposable Members
    }
}
