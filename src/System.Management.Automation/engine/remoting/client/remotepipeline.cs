// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation.Runspaces;
using System.Management.Automation.Internal;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Management.Automation.Remoting;
using Dbg = System.Management.Automation.Diagnostics;
using System.Threading;
using System.Management.Automation.Runspaces.Internal;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings

namespace System.Management.Automation
{
    internal class RemotePipeline : Pipeline
    {
        #region Private Members

        private PowerShell _powershell;
        private bool _addToHistory;
        private bool _isNested;
        private bool _isSteppable;
        private Runspace _runspace;
        private object _syncRoot = new object();
        private bool _disposed = false;
        private string _historyString;
        private PipelineStateInfo _pipelineStateInfo = new PipelineStateInfo(PipelineState.NotStarted);
        private CommandCollection _commands = new CommandCollection();
        private string _computerName;
        private Guid _runspaceId;
        private ConnectCommandInfo _connectCmdInfo = null;

        /// <summary>
        /// This is queue of all the state change event which have occured for
        /// this pipeline. RaisePipelineStateEvents raises event for each
        /// item in this queue. We don't raise the event with in SetPipelineState
        /// because often SetPipelineState is called with in a lock.
        /// Raising event in lock introduces chances of deadlock in GUI applications.
        /// </summary>
        private Queue<ExecutionEventQueueItem> _executionEventQueue = new Queue<ExecutionEventQueueItem>();

        private class ExecutionEventQueueItem
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

        private bool _performNestedCheck = true;

        #endregion Private Members

        #region Constructors

        /// <summary>
        /// Private constructor that does most of the work constructing a remote pipeline object.
        /// </summary>
        /// <param name="runspace">RemoteRunspace object.</param>
        /// <param name="addToHistory">AddToHistory.</param>
        /// <param name="isNested">IsNested.</param>
        private RemotePipeline(RemoteRunspace runspace, bool addToHistory, bool isNested)
            : base(runspace)
        {
            _addToHistory = addToHistory;
            _isNested = isNested;
            _isSteppable = false;
            _runspace = runspace;
            _computerName = ((RemoteRunspace)_runspace).ConnectionInfo.ComputerName;
            _runspaceId = _runspace.InstanceId;

            // Initialize streams
            _inputCollection = new PSDataCollection<object>();
            _inputCollection.ReleaseOnEnumeration = true;

            _inputStream = new PSDataCollectionStream<object>(Guid.Empty, _inputCollection);
            _outputCollection = new PSDataCollection<PSObject>();
            _outputStream = new PSDataCollectionStream<PSObject>(Guid.Empty, _outputCollection);
            _errorCollection = new PSDataCollection<ErrorRecord>();
            _errorStream = new PSDataCollectionStream<ErrorRecord>(Guid.Empty, _errorCollection);

            // Create object stream for method executor objects.
            MethodExecutorStream = new ObjectStream();
            IsMethodExecutorStreamEnabled = false;

            SetCommandCollection(_commands);

            // Create event which will be signalled when pipeline execution
            // is completed/failed/stoped.
            // Note:Runspace.Close waits for all the running pipeline
            // to finish.  This Event must be created before pipeline is
            // added to list of running pipelines. This avoids the race condition
            // where Close is called after pipeline is added to list of
            // running pipeline but before event is created.
            PipelineFinishedEvent = new ManualResetEvent(false);
        }

        /// <summary>
        /// Constructs a remote pipeline for the specified runspace and
        /// specified command.
        /// </summary>
        /// <param name="runspace">Runspace in which to create the pipeline.</param>
        /// <param name="command">Command as a string, to be used in pipeline creation.</param>
        /// <param name="addToHistory">Whether to add the command to the runspaces history.</param>
        /// <param name="isNested">Whether this pipeline is nested.</param>
        internal RemotePipeline(RemoteRunspace runspace, string command, bool addToHistory, bool isNested)
            : this(runspace, addToHistory, isNested)
        {
            if (command != null)
            {
                _commands.Add(new Command(command, true));
            }

            // initialize the underlying powershell object
            _powershell = new PowerShell(_inputStream, _outputStream, _errorStream,
                ((RemoteRunspace)_runspace).RunspacePool);

            _powershell.SetIsNested(isNested);

            _powershell.InvocationStateChanged +=
               new EventHandler<PSInvocationStateChangedEventArgs>(HandleInvocationStateChanged);
        }

        /// <summary>
        /// Constructs a remote pipeline object associated with a remote running
        /// command but in a disconnected state.
        /// </summary>
        /// <param name="runspace">Remote runspace associated with running command.</param>
        internal RemotePipeline(RemoteRunspace runspace)
            : this(runspace, false, false)
        {
            if (runspace.RemoteCommand == null)
            {
                throw new InvalidOperationException(PipelineStrings.InvalidRemoteCommand);
            }

            _connectCmdInfo = runspace.RemoteCommand;
            _commands.Add(_connectCmdInfo.Command);

            // Beginning state will be disconnected.
            SetPipelineState(PipelineState.Disconnected, null);

            // Create the underlying powershell object.
            _powershell = new PowerShell(_connectCmdInfo, _inputStream, _outputStream, _errorStream,
                ((RemoteRunspace)_runspace).RunspacePool);

            _powershell.InvocationStateChanged +=
                new EventHandler<PSInvocationStateChangedEventArgs>(HandleInvocationStateChanged);
        }

        /// <summary>
        /// Creates a cloned pipeline from the specified one.
        /// </summary>
        /// <param name="pipeline">Pipeline to clone from.</param>
        /// <remarks>This constructor is private because this will
        /// only be called from the copy method</remarks>
        private RemotePipeline(RemotePipeline pipeline) :
            this((RemoteRunspace)pipeline.Runspace, null, false, pipeline.IsNested)
        {
            _isSteppable = pipeline._isSteppable;

            // NTRAID#Windows Out Of Band Releases-915851-2005/09/13
            // the above comment copied from RemotePipelineBase which
            // originally copied it from PipelineBase
            if (pipeline == null)
            {
                throw PSTraceSource.NewArgumentNullException("pipeline");
            }

            if (pipeline._disposed)
            {
                throw PSTraceSource.NewObjectDisposedException("pipeline");
            }

            _addToHistory = pipeline._addToHistory;
            _historyString = pipeline._historyString;
            foreach (Command command in pipeline.Commands)
            {
                Command clone = command.Clone();

                // Attach the cloned Command to this pipeline.
                Commands.Add(clone);
            }
        }

        /// <summary>
        /// Override for creating a copy of pipeline.
        /// </summary>
        /// <returns>
        /// Pipeline object which is copy of this pipeline
        /// </returns>
        public override Pipeline Copy()
        {
            if (_disposed)
            {
                throw PSTraceSource.NewObjectDisposedException("pipeline");
            }

            return (Pipeline)new RemotePipeline(this);
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Access the runspace this pipeline is created on.
        /// </summary>
        public override Runspace Runspace
        {
            get
            {
#pragma warning disable 56503
                // NTRAID#Windows Out Of Band Releases-915851-2005/09/13
                if (_disposed)
                {
                    throw PSTraceSource.NewObjectDisposedException("pipeline");
                }
#pragma warning restore 56503

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
        /// Internal method to set the value of IsNested. This is called
        /// by serializer.
        /// </summary>
        internal void SetIsNested(bool isNested)
        {
            _isNested = isNested;
            _powershell.SetIsNested(isNested);
        }

        /// <summary>
        /// Internal method to set the value of IsSteppable. This is called
        /// during DoConcurrentCheck.
        /// </summary>
        internal void SetIsSteppable(bool isSteppable)
        {
            _isSteppable = isSteppable;
        }

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
                lock (_syncRoot)
                {
                    // Note:We do not return internal state.
                    return _pipelineStateInfo.Clone();
                }
            }
        }

        /// <summary>
        /// Access the input writer for this pipeline.
        /// </summary>
        public override PipelineWriter Input
        {
            get
            {
                return _inputStream.ObjectWriter;
            }
        }

        /// <summary>
        /// Access the output reader for this pipeline.
        /// </summary>
        public override PipelineReader<PSObject> Output
        {
            get
            {
                return _outputStream.GetPSObjectReaderForPipeline(_computerName, _runspaceId);
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
                return _errorStream.GetObjectReaderForPipeline(_computerName, _runspaceId);
            }
        }

        /// <summary>
        /// String which is added in the history.
        /// </summary>
        /// <remarks>This needs to be internal so that it can be replaced
        /// by invoke-cmd to place correct string in history.</remarks>
        internal string HistoryString
        {
            get
            {
                return _historyString;
            }

            set
            {
                _historyString = value;
            }
        }

        /// <summary>
        /// Whether the pipeline needs to be added to history of the runspace.
        /// </summary>
        public bool AddToHistory
        {
            get
            {
                return _addToHistory;
            }
        }

        #endregion Properties

        #region streams

        // Stream and Collection go together...a stream wraps
        // a corresponding collection to support
        // streaming behavior of the pipeline.
        private PSDataCollection<PSObject> _outputCollection;
        private PSDataCollectionStream<PSObject> _outputStream;
        private PSDataCollection<ErrorRecord> _errorCollection;
        private PSDataCollectionStream<ErrorRecord> _errorStream;
        private PSDataCollection<object> _inputCollection;
        private PSDataCollectionStream<object> _inputStream;

        /// <summary>
        /// Stream for providing input to PipelineProcessor. Host will write on
        /// ObjectWriter of this stream. PipelineProcessor will read from
        /// ObjectReader of this stream.
        /// </summary>
        protected PSDataCollectionStream<object> InputStream
        {
            get
            {
                return _inputStream;
            }
        }

        #endregion streams

        #region Invoke

        /// <summary>
        /// Invoke the pipeline asynchronously.
        /// </summary>
        /// <remarks>
        /// Results are returned through the <see cref="Pipeline.Output"/> reader.
        /// </remarks>
        public override void InvokeAsync()
        {
            InitPowerShell(false);
            CoreInvokeAsync();
        }

        /// <summary>
        /// Invokes a remote command and immediately disconnects if
        /// transport layer supports it.
        /// </summary>
        internal override void InvokeAsyncAndDisconnect()
        {
            // Initialize PowerShell invocation with "InvokeAndDisconnect" setting.
            InitPowerShell(false, true);
            CoreInvokeAsync();
        }

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
        public override Collection<PSObject> Invoke(System.Collections.IEnumerable input)
        {
            if (input == null)
            {
                this.InputStream.Close();
            }

            InitPowerShell(true);

            Collection<PSObject> results;

            try
            {
                results = _powershell.Invoke(input);
            }
            catch (InvalidRunspacePoolStateException)
            {
                InvalidRunspaceStateException e =
                    new InvalidRunspaceStateException
                    (
                        StringUtil.Format(RunspaceStrings.RunspaceNotOpenForPipeline, _runspace.RunspaceStateInfo.State.ToString()),
                        _runspace.RunspaceStateInfo.State,
                        RunspaceState.Opened
                    );
                throw e;
            }

            return results;
        }

        #endregion Invoke

        #region Connect

        /// <summary>
        /// Connects synchronously to a running command on a remote server.
        /// The pipeline object must be in the disconnected state.
        /// </summary>
        /// <returns>A collection of result objects.</returns>
        public override Collection<PSObject> Connect()
        {
            InitPowerShellForConnect(true);

            Collection<PSObject> results;

            try
            {
                results = _powershell.Connect();
            }
            catch (InvalidRunspacePoolStateException)
            {
                InvalidRunspaceStateException e =
                    new InvalidRunspaceStateException
                    (
                        StringUtil.Format(RunspaceStrings.RunspaceNotOpenForPipelineConnect, _runspace.RunspaceStateInfo.State.ToString()),
                        _runspace.RunspaceStateInfo.State,
                        RunspaceState.Opened
                    );

                throw e;
            }

            // PowerShell object will return empty results if it was provided an alternative object to
            // collect output in.  Check to see if the output was collected in a member variable.
            if (results.Count == 0)
            {
                if (_outputCollection != null && _outputCollection.Count > 0)
                {
                    results = new Collection<PSObject>(_outputCollection);
                }
            }

            return results;
        }

        /// <summary>
        /// Connects asynchronously to a running command on a remote server.
        /// </summary>
        public override void ConnectAsync()
        {
            InitPowerShellForConnect(false);

            try
            {
                _powershell.ConnectAsync();
            }
            catch (InvalidRunspacePoolStateException)
            {
                InvalidRunspaceStateException e =
                    new InvalidRunspaceStateException
                    (
                        StringUtil.Format(RunspaceStrings.RunspaceNotOpenForPipelineConnect, _runspace.RunspaceStateInfo.State.ToString()),
                        _runspace.RunspaceStateInfo.State,
                        RunspaceState.Opened
                    );
                throw e;
            }
        }

        #endregion

        #region Stop

        /// <summary>
        /// Stop the pipeline synchronously.
        /// </summary>
        public override void Stop()
        {
            bool isAlreadyStopping = false;
            if (CanStopPipeline(out isAlreadyStopping))
            {
                // A pipeline can be stopped before it is started.so protecting against that
                if (_powershell != null)
                {
                    IAsyncResult asyncresult = null;
                    try
                    {
                        asyncresult = _powershell.BeginStop(null, null);
                    }
                    catch (ObjectDisposedException)
                    {
                        throw PSTraceSource.NewObjectDisposedException("Pipeline");
                    };

                    asyncresult.AsyncWaitHandle.WaitOne();
                }
            }

            // Waits until pipeline completes stop as this is a sync call.
            PipelineFinishedEvent.WaitOne();
        }

        /// <summary>
        /// Stop the pipeline asynchronously.
        /// This method calls the BeginStop on the underlying
        /// powershell and so any exception will be
        /// thrown on the same thread.
        /// </summary>
        public override void StopAsync()
        {
            bool isAlreadyStopping;
            if (CanStopPipeline(out isAlreadyStopping))
            {
                try
                {
                    _powershell.BeginStop(null, null);
                }
                catch (ObjectDisposedException)
                {
                    throw PSTraceSource.NewObjectDisposedException("Pipeline");
                }
            }
        }

        /// <summary>
        /// Verifies if the pipeline is in a state where it can be stopped.
        /// </summary>
        private bool CanStopPipeline(out bool isAlreadyStopping)
        {
            bool returnResult = false;
            isAlreadyStopping = false;
            lock (_syncRoot)
            {
                // SetPipelineState does not raise events..
                // so locking is ok here.
                switch (_pipelineStateInfo.State)
                {
                    case PipelineState.NotStarted:
                        SetPipelineState(PipelineState.Stopping, null);
                        SetPipelineState(PipelineState.Stopped, null);
                        returnResult = false;
                        break;

                    // If pipeline execution has failed or completed or
                    // stoped, return silently.
                    case PipelineState.Stopped:
                    case PipelineState.Completed:
                    case PipelineState.Failed:
                        return false;

                    // If pipeline is in Stopping state, ignore the second
                    // stop.
                    case PipelineState.Stopping:
                        isAlreadyStopping = true;
                        return false;

                    case PipelineState.Running:
                    case PipelineState.Disconnected:
                        SetPipelineState(PipelineState.Stopping, null);
                        returnResult = true;
                        break;
                }
            }

            RaisePipelineStateEvents();

            return returnResult;
        }

        #endregion Stop

        #region Events

        /// <summary>
        /// Event raised when Pipeline's state changes.
        /// </summary>
        public override event EventHandler<PipelineStateEventArgs> StateChanged = null;

        #endregion Events

        #region Dispose

        /// <summary>
        /// Disposes the pipeline.
        /// </summary>
        /// <param name="disposing">True, when called on Dispose().</param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (_disposed)
                {
                    return;
                }

                lock (_syncRoot)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    _disposed = true;
                }

                if (disposing)
                {
                    // wait for the pipeline to stop..this will block
                    // if the pipeline is already stopping.
                    Stop();
                    // _pipelineFinishedEvent.Close();

                    if (_powershell != null)
                    {
                        _powershell.Dispose();
                        _powershell = null;
                    }

                    _inputCollection.Dispose();
                    _inputStream.Dispose();
                    _outputCollection.Dispose();
                    _outputStream.Dispose();
                    _errorCollection.Dispose();
                    _errorStream.Dispose();
                    MethodExecutorStream.Dispose();
                    PipelineFinishedEvent.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        #endregion Dispose

        #region Private Methods

        private void CoreInvokeAsync()
        {
            try
            {
                _powershell.BeginInvoke();
            }
            catch (InvalidRunspacePoolStateException)
            {
                InvalidRunspaceStateException e =
                    new InvalidRunspaceStateException
                    (
                        StringUtil.Format(RunspaceStrings.RunspaceNotOpenForPipeline, _runspace.RunspaceStateInfo.State.ToString()),
                        _runspace.RunspaceStateInfo.State,
                        RunspaceState.Opened
                    );
                throw e;
            }
        }

        private void HandleInvocationStateChanged(object sender, PSInvocationStateChangedEventArgs e)
        {
            SetPipelineState((PipelineState)e.InvocationStateInfo.State, e.InvocationStateInfo.Reason);

            RaisePipelineStateEvents();
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
        private void SetPipelineState(PipelineState state, Exception reason)
        {
            PipelineState copyState = state;
            PipelineStateInfo copyStateInfo = null;

            lock (_syncRoot)
            {
                switch (_pipelineStateInfo.State)
                {
                    case PipelineState.Completed:
                    case PipelineState.Failed:
                    case PipelineState.Stopped:
                        return;

                    case PipelineState.Running:
                        {
                            if (state == PipelineState.Running)
                            {
                                return;
                            }
                        }

                        break;
                    case PipelineState.Stopping:
                        {
                            if (state == PipelineState.Running || state == PipelineState.Stopping)
                            {
                                return;
                            }
                            else
                            {
                                copyState = PipelineState.Stopped;
                            }
                        }

                        break;
                }

                _pipelineStateInfo = new PipelineStateInfo(copyState, reason);
                copyStateInfo = _pipelineStateInfo;

                // Add _pipelineStateInfo to _executionEventQueue.
                // RaisePipelineStateEvents will raise event for each item
                // in this queue.
                // Note:We are doing clone here instead of passing the member
                // _pipelineStateInfo because we donot want outside
                // to change pipeline state.
                RunspaceAvailability previousAvailability = _runspace.RunspaceAvailability;

                Guid? cmdInstanceId = (_powershell != null) ? _powershell.InstanceId : (Guid?)null;
                _runspace.UpdateRunspaceAvailability(_pipelineStateInfo.State, false, cmdInstanceId);

                _executionEventQueue.Enqueue(
                    new ExecutionEventQueueItem(
                        _pipelineStateInfo.Clone(),
                        previousAvailability,
                        _runspace.RunspaceAvailability));
            }

            // using the copyStateInfo here as this piece of code is
            // outside of lock and _pipelineStateInfo might get changed
            // by two threads running concurrently..so its value is
            // not guaranteed to be the same for this entire method call.
            // copyStateInfo is a local variable.
            if (copyStateInfo.State == PipelineState.Completed ||
                copyStateInfo.State == PipelineState.Failed ||
                copyStateInfo.State == PipelineState.Stopped)
            {
                Cleanup();
            }
        }

        /// <summary>
        /// Raises events for changes in execution state.
        /// </summary>
        protected void RaisePipelineStateEvents()
        {
            Queue<ExecutionEventQueueItem> tempEventQueue = null;
            EventHandler<PipelineStateEventArgs> stateChanged = null;
            bool runspaceHasAvailabilityChangedSubscribers = false;

            lock (_syncRoot)
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
                }
            }
        }

        /// <summary>
        /// Initializes the underlying PowerShell object after verifying
        /// if the pipeline is in a state where it can be invoked.
        /// If invokeAndDisconnect is true then the remote PowerShell
        /// command will be immediately disconnected after it begins
        /// running.
        /// </summary>
        /// <param name="syncCall">True if called from a sync call.</param>
        /// <param name="invokeAndDisconnect">Invoke and Disconnect.</param>
        private void InitPowerShell(bool syncCall, bool invokeAndDisconnect = false)
        {
            if (_commands == null || _commands.Count == 0)
            {
                throw PSTraceSource.NewInvalidOperationException(
                        RunspaceStrings.NoCommandInPipeline);
            }

            if (_pipelineStateInfo.State != PipelineState.NotStarted)
            {
                InvalidPipelineStateException e =
                    new InvalidPipelineStateException
                    (
                        StringUtil.Format(RunspaceStrings.PipelineReInvokeNotAllowed),
                        _pipelineStateInfo.State,
                        PipelineState.NotStarted
                    );
                throw e;
            }

            ((RemoteRunspace)_runspace).DoConcurrentCheckAndAddToRunningPipelines(this, syncCall);

            PSInvocationSettings settings = new PSInvocationSettings();
            settings.AddToHistory = _addToHistory;
            settings.InvokeAndDisconnect = invokeAndDisconnect;

            _powershell.InitForRemotePipeline(_commands, _inputStream, _outputStream, _errorStream, settings, RedirectShellErrorOutputPipe);

            _powershell.RemotePowerShell.HostCallReceived +=
                new EventHandler<RemoteDataEventArgs<RemoteHostCall>>(HandleHostCallReceived);
        }

        /// <summary>
        /// Initializes the underlying PowerShell object after verifying that it is
        /// in a state where it can connect to the remote command.
        /// </summary>
        /// <param name="syncCall"></param>
        private void InitPowerShellForConnect(bool syncCall)
        {
            if (_pipelineStateInfo.State != PipelineState.Disconnected)
            {
                throw new InvalidPipelineStateException(StringUtil.Format(PipelineStrings.PipelineNotDisconnected),
                                                        _pipelineStateInfo.State,
                                                        PipelineState.Disconnected);
            }

            // The connect may be from the same Pipeline that disconnected and in this case
            // the Pipeline state already exists.  Or this could be a new Pipeline object
            // (connect reconstruction case) and new state is created.

            // Check to see if this pipeline already exists in the runspace.
            RemotePipeline currentPipeline = (RemotePipeline)((RemoteRunspace)_runspace).GetCurrentlyRunningPipeline();
            if (!ReferenceEquals(currentPipeline, this))
            {
                ((RemoteRunspace)_runspace).DoConcurrentCheckAndAddToRunningPipelines(this, syncCall);
            }

            // Initialize the PowerShell object if it hasn't been initialized before.
            if ((_powershell.RemotePowerShell) == null || !_powershell.RemotePowerShell.Initialized)
            {
                PSInvocationSettings settings = new PSInvocationSettings();
                settings.AddToHistory = _addToHistory;

                _powershell.InitForRemotePipelineConnect(_inputStream, _outputStream, _errorStream, settings, RedirectShellErrorOutputPipe);

                _powershell.RemotePowerShell.HostCallReceived +=
                    new EventHandler<RemoteDataEventArgs<RemoteHostCall>>(HandleHostCallReceived);
            }
        }

        /// <summary>
        /// Handle host call received.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="eventArgs">Arguments describing the host call to invoke.</param>
        private void HandleHostCallReceived(object sender, RemoteDataEventArgs<RemoteHostCall> eventArgs)
        {
            ClientMethodExecutor.Dispatch(
                _powershell.RemotePowerShell.DataStructureHandler.TransportManager,
                ((RemoteRunspace)_runspace).RunspacePool.RemoteRunspacePoolInternal.Host,
                _errorStream,
                MethodExecutorStream,
                IsMethodExecutorStreamEnabled,
                ((RemoteRunspace)_runspace).RunspacePool.RemoteRunspacePoolInternal,
                _powershell.InstanceId,
                eventArgs.Data);
        }

        /// <summary>
        /// Does the cleanup necessary on pipeline completion.
        /// </summary>
        private void Cleanup()
        {
            // Close the output stream if it is not closed.
            if (_outputStream.IsOpen)
            {
                try
                {
                    _outputCollection.Complete();
                    _outputStream.Close();
                }
                catch (ObjectDisposedException)
                {
                }
            }

            // Close the error stream if it is not closed.
            if (_errorStream.IsOpen)
            {
                try
                {
                    _errorCollection.Complete();
                    _errorStream.Close();
                }
                catch (ObjectDisposedException)
                {
                }
            }

            // Close the input stream if it is not closed.
            if (_inputStream.IsOpen)
            {
                try
                {
                    _inputCollection.Complete();
                    _inputStream.Close();
                }
                catch (ObjectDisposedException)
                {
                }
            }

            try
            {
                // Runspace object maintains a list of pipelines in execution.
                // Remove this pipeline from the list. This method also calls the
                // pipeline finished event.
                ((RemoteRunspace)_runspace).RemoveFromRunningPipelineList(this);

                PipelineFinishedEvent.Set();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        #endregion Private Methods

        #region Internal Methods/Properties

        /// <summary>
        /// ManualResetEvent which is signaled when pipeline execution is
        /// completed/failed/stoped.
        /// </summary>
        internal ManualResetEvent PipelineFinishedEvent { get; }

        /// <summary>
        /// Is method executor stream enabled.
        /// </summary>
        internal bool IsMethodExecutorStreamEnabled { get; set; }

        /// <summary>
        /// Method executor stream.
        /// </summary>
        internal ObjectStream MethodExecutorStream { get; }

        /// <summary>
        /// Check if anyother pipeline is executing.
        /// In case of nested pipeline, checks that it is called
        /// from currently executing pipeline's thread.
        /// </summary>
        /// <param name="syncCall">True if method is called from Invoke, false
        /// if called from InvokeAsync</param>
        /// <exception cref="InvalidOperationException">
        /// 1) A pipeline is already executing. Pipeline cannot execute
        /// concurrently.
        /// 2) InvokeAsync is called on nested pipeline. Nested pipeline
        /// cannot be executed Asynchronously.
        /// 3) Attempt is made to invoke a nested pipeline directly. Nested
        /// pipeline must be invoked from a running pipeline.
        /// </exception>
        internal void DoConcurrentCheck(bool syncCall)
        {
            RemotePipeline currentPipeline =
                (RemotePipeline)((RemoteRunspace)_runspace).GetCurrentlyRunningPipeline();

            if (_isNested == false)
            {
                if (currentPipeline == null &&
                    ((RemoteRunspace)_runspace).RunspaceAvailability != RunspaceAvailability.Busy &&
                    ((RemoteRunspace)_runspace).RunspaceAvailability != RunspaceAvailability.RemoteDebug)
                {
                    // We can add a new pipeline to the runspace only if it is
                    // available (not busy).
                    return;
                }

                if (currentPipeline == null &&
                    ((RemoteRunspace)_runspace).RemoteCommand != null &&
                    _connectCmdInfo != null &&
                    Guid.Equals(((RemoteRunspace)_runspace).RemoteCommand.CommandId, _connectCmdInfo.CommandId))
                {
                    // Connect case.  We can add a pipeline to a busy runspace when
                    // that pipeline represents the same command as is currently
                    // running.
                    return;
                }

                if (currentPipeline != null &&
                         ReferenceEquals(currentPipeline, this))
                {
                    // Reconnect case.  We can add a pipeline to a busy runspace when the
                    // pipeline is the same (reconnecting).
                    return;
                }

                if (!_isSteppable)
                {
                    throw PSTraceSource.NewInvalidOperationException(
                            RunspaceStrings.ConcurrentInvokeNotAllowed);
                }
            }
            else
            {
                if (_performNestedCheck)
                {
                    if (_isSteppable)
                    {
                        return;
                    }

                    if (syncCall == false)
                    {
                        throw PSTraceSource.NewInvalidOperationException(
                                RunspaceStrings.NestedPipelineInvokeAsync);
                    }

                    if (currentPipeline == null)
                    {
                        if (!_isSteppable)
                        {
                            throw PSTraceSource.NewInvalidOperationException(
                                    RunspaceStrings.NestedPipelineNoParentPipeline);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The underlying powershell object on which this remote pipeline
        /// is created.
        /// </summary>
        internal PowerShell PowerShell
        {
            get
            {
                return _powershell;
            }
        }

        /// <summary>
        /// Sets the history string to the specified string.
        /// </summary>
        /// <param name="historyString">New history string to set to.</param>
        internal override void SetHistoryString(string historyString)
        {
            _powershell.HistoryString = historyString;
        }

        #endregion Internal Methods/Properties

        #region Remote data drain/block methods

        /// <summary>
        /// Blocks data arriving from remote session.
        /// </summary>
        internal override void SuspendIncomingData()
        {
            _powershell.SuspendIncomingData();
        }

        /// <summary>
        /// Resumes data arrive from remote session.
        /// </summary>
        internal override void ResumeIncomingData()
        {
            _powershell.ResumeIncomingData();
        }

        /// <summary>
        /// Blocking call that waits until the current remote data
        /// queue is empty.
        /// </summary>
        internal override void DrainIncomingData()
        {
            _powershell.WaitForServicingComplete();
        }

        #endregion
    }
}
