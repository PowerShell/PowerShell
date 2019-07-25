// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation.Remoting.Internal;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Security;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.PSTasks
{
    #region PSTask

    /// <summary>
    /// Class to encapsulate synchronous running scripts in parallel
    /// </summary>
    internal sealed class PSTask : PSTaskBase
    {
        #region Members

        private readonly PSTaskDataStreamWriter _dataStreamWriter;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public PSTask(
            ScriptBlock scriptBlock,
            Dictionary<string, object> usingValuesMap,
            object dollarUnderbar,
            PSTaskDataStreamWriter dataStreamWriter) 
            : base(
                scriptBlock,
                usingValuesMap,
                dollarUnderbar)
        {
            _dataStreamWriter = dataStreamWriter;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Initialize PowerShell object
        /// </summary>
        protected override void InitializePowershell()
        {
            // Writer data stream handlers
            _output.DataAdded += (sender, args) => HandleOutputData(sender, args);
            _powershell.Streams.Error.DataAdded += (sender, args) => HandleErrorData(sender, args);
            _powershell.Streams.Warning.DataAdded += (sender, args) => HandleWarningData(sender, args);
            _powershell.Streams.Verbose.DataAdded += (sender, args) => HandleVerboseData(sender, args);
            _powershell.Streams.Debug.DataAdded += (sender, args) => HandleDebugData(sender, args);
            _powershell.Streams.Information.DataAdded += (sender, args) => HandleInformationData(sender, args);

            // State change handler
            _powershell.InvocationStateChanged += (sender, args) => HandleStateChanged(sender, args);
        }

        #endregion       

        #region Writer data stream handlers

        private void HandleOutputData(object sender, DataAddedEventArgs args)
        {
            foreach (var item in _output.ReadAll())
            {
                _dataStreamWriter.Add(
                    new PSStreamObject(PSStreamObjectType.Output, item)
                );
            }
        }

        private void HandleErrorData(object sender, DataAddedEventArgs args)
        {
            foreach (var item in _powershell.Streams.Error.ReadAll())
            {
                _dataStreamWriter.Add(
                    new PSStreamObject(PSStreamObjectType.Error, item)
                );
            }
        }

        private void HandleWarningData(object sender, DataAddedEventArgs args)
        {
            foreach (var item in _powershell.Streams.Warning.ReadAll())
            {
                _dataStreamWriter.Add(
                    new PSStreamObject(PSStreamObjectType.Warning, item.Message)
                );
            }
        }

        private void HandleVerboseData(object sender, DataAddedEventArgs args)
        {
            foreach (var item in _powershell.Streams.Verbose.ReadAll())
            {
                _dataStreamWriter.Add(
                    new PSStreamObject(PSStreamObjectType.Verbose, item.Message)
                );
            }
        }

        private void HandleDebugData(object sender, DataAddedEventArgs args)
        {
            foreach (var item in _powershell.Streams.Debug.ReadAll())
            {
                _dataStreamWriter.Add(
                    new PSStreamObject(PSStreamObjectType.Debug, item.Message)
                );
            }
        }

        private void HandleInformationData(object sender, DataAddedEventArgs args)
        {
            foreach (var item in _powershell.Streams.Information.ReadAll())
            {
                _dataStreamWriter.Add(
                    new PSStreamObject(PSStreamObjectType.Information, item)
                );
            }
        }
        
        #endregion

        #region Event handlers

        private void HandleStateChanged(object sender, PSInvocationStateChangedEventArgs stateChangeInfo)
        {
            if (_dataStreamWriter != null)
            {
                // Treat any terminating exception as a non-terminating error record
                var newStateInfo = stateChangeInfo.InvocationStateInfo;
                if (newStateInfo.Reason != null)
                {
                    var errorRecord = new ErrorRecord(
                        newStateInfo.Reason,
                        "PSTaskException",
                        ErrorCategory.InvalidOperation,
                        this);

                    _dataStreamWriter.Add(
                        new PSStreamObject(PSStreamObjectType.Error, errorRecord)
                    );
                }
            }

            RaiseStateChangedEvent(stateChangeInfo);
        }

        #endregion
    }

    /// <summary>
    /// Class to encapsulate asynchronous running scripts in parallel as jobs
    /// </summary>
    internal sealed class PSJobTask : PSTaskBase
    {
        #region Members

        private readonly Job _job;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public PSJobTask(
            ScriptBlock scriptBlock,
            Dictionary<string, object> usingValuesMap,
            object dollarUnderbar,
            Job job) : base(
                scriptBlock,
                usingValuesMap,
                dollarUnderbar)
        {
            _job = job;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Initialize PowerShell object
        /// </summary>
        protected override void InitializePowershell()
        {
            // Job data stream handlers
            _output.DataAdded += (sender, args) => HandleJobOutputData(sender, args);
            _powershell.Streams.Error.DataAdded += (sender, args) => HandleJobErrorData(sender, args);
            _powershell.Streams.Warning.DataAdded += (sender, args) => HandleJobWarningData(sender, args);
            _powershell.Streams.Verbose.DataAdded += (sender, args) => HandleJobVerboseData(sender, args);
            _powershell.Streams.Debug.DataAdded += (sender, args) => HandleJobDebugData(sender, args);
            _powershell.Streams.Information.DataAdded += (sender, args) => HandleJobInformationData(sender, args);

            // State change handler
            _powershell.InvocationStateChanged += (sender, args) => HandleStateChanged(sender, args);
        }

        #endregion

        #region Job data stream handlers

        private void HandleJobOutputData(object sender, DataAddedEventArgs args)
        {
            foreach (var item in _output.ReadAll())
            {
                _job.Output.Add(item);
                _job.Results.Add(
                    new PSStreamObject(PSStreamObjectType.Output, item)
                );
            }
        }

        private void HandleJobErrorData(object sender, DataAddedEventArgs args)
        {
            foreach (var item in _powershell.Streams.Error.ReadAll())
            {
                _job.Error.Add(item);
                _job.Results.Add(
                    new PSStreamObject(PSStreamObjectType.Error, item)
                );
            }
        }

        private void HandleJobWarningData(object sender, DataAddedEventArgs args)
        {
            foreach (var item in _powershell.Streams.Warning.ReadAll())
            {
                _job.Warning.Add(item);
                _job.Results.Add(
                    new PSStreamObject(PSStreamObjectType.Warning, item.Message)
                );
            }
        }

        private void HandleJobVerboseData(object sender, DataAddedEventArgs args)
        {
            foreach (var item in _powershell.Streams.Verbose.ReadAll())
            {
                _job.Verbose.Add(item);
                _job.Results.Add(
                    new PSStreamObject(PSStreamObjectType.Verbose, item.Message)
                );
            }
        }

        private void HandleJobDebugData(object sender, DataAddedEventArgs args)
        {
            foreach (var item in _powershell.Streams.Debug.ReadAll())
            {
                _job.Debug.Add(item);
                _job.Results.Add(
                    new PSStreamObject(PSStreamObjectType.Debug, item.Message)
                );
            }
        }

        private void HandleJobInformationData(object sender, DataAddedEventArgs args)
        {
            foreach (var item in _powershell.Streams.Information.ReadAll())
            {
                _job.Information.Add(item);
                _job.Results.Add(
                    new PSStreamObject(PSStreamObjectType.Information, item)
                );
            }
        }

        #endregion

        #region Event handlers

        private void HandleStateChanged(object sender, PSInvocationStateChangedEventArgs stateChangeInfo)
        {
            RaiseStateChangedEvent(stateChangeInfo);
        }

        #endregion
    }

    /// <summary>
    /// Base class to encapsulate running a PowerShell script concurrently in a cmdlet or job context
    /// </summary>
    internal abstract class PSTaskBase : IDisposable
    {
        #region Members

        private readonly ScriptBlock _scriptBlockToRun;
        private readonly Dictionary<string, object> _usingValuesMap;
        private readonly object _dollarUnderbar;
        private readonly int _id;
        private Runspace _runspace;
        protected PowerShell _powershell;
        protected PSDataCollection<PSObject> _output;

        private const string VERBATIM_ARGUMENT = "--%";
        private const string RunspaceName = "PSTask";

        private static int s_taskId = 0;

        #endregion

        #region Events

        /// <summary>
        /// Event that fires when the task running state changes
        /// </summary>
        public event EventHandler<PSInvocationStateChangedEventArgs> StateChanged;

        internal void RaiseStateChangedEvent(PSInvocationStateChangedEventArgs args)
        {
            StateChanged.SafeInvoke(this, args);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Current running state of the task
        /// </summary>
        public PSInvocationState State
        {
            get
            {
                PowerShell ps = _powershell;
                if (ps != null)
                {
                    return ps.InvocationStateInfo.State;
                }

                return PSInvocationState.NotStarted;
            }
        }

        /// <summary>
        /// Task Id
        /// </summary>
        public int Id
        {
            get { return _id; }
        }

        #endregion

        #region Constructor

        private PSTaskBase() 
        { 
            _id = Interlocked.Increment(ref s_taskId);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public PSTaskBase(
            ScriptBlock scriptBlock,
            Dictionary<string, object> usingValuesMap,
            object dollarUnderbar) : this()
        {
            _scriptBlockToRun = scriptBlock;
            _usingValuesMap = usingValuesMap;
            _dollarUnderbar = dollarUnderbar;
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Initialize PowerShell object
        /// </summary>
        protected abstract void InitializePowershell();

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _runspace.Dispose();
            _powershell.Dispose();
            _output.Dispose();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Start task
        /// </summary>
        public void Start()
        {
            if (_powershell != null)
            {
                Dbg.Assert(false, "A PSTask can be started only once.");
                return;
            }

            // Create and open Runspace for this task to run in
            var iss = InitialSessionState.CreateDefault2();
            iss.LanguageMode = (SystemPolicy.GetSystemLockdownPolicy() == SystemEnforcementMode.Enforce) 
                ? PSLanguageMode.ConstrainedLanguage : PSLanguageMode.FullLanguage;
            _runspace = RunspaceFactory.CreateRunspace(iss);
            _runspace.Name = string.Format(CultureInfo.InvariantCulture, "{0}:{1}", RunspaceName, s_taskId);
            _runspace.Open();

            // Create the PowerShell command pipeline for the provided script block
            // The script will run on the provided Runspace in a new thread by default
            _powershell = PowerShell.Create();
            _powershell.Runspace = _runspace;

            // Initialize PowerShell object data streams and event handlers
            _output = new PSDataCollection<PSObject>();
            InitializePowershell();

            // Start the script running in a new thread
            _powershell.AddScript(_scriptBlockToRun.ToString());
            _powershell.Commands.Commands[0].DollarUnderbar = _dollarUnderbar;
            if (_usingValuesMap != null && _usingValuesMap.Count > 0)
            {
                _powershell.AddParameter(VERBATIM_ARGUMENT, _usingValuesMap);
            }
            _powershell.BeginInvoke<object, PSObject>(null, _output);
        }

        /// <summary>
        /// Signals the running task to stop
        /// </summary>
        public void SignalStop()
        {
            if (_powershell != null)
            {
                _powershell.BeginStop(null, null);
            }
        }

        #endregion
    }

    #endregion

    #region PSTaskDataStreamWriter

    /// <summary>
    /// Class that handles writing task data stream objects to a cmdlet
    /// </summary>
    internal sealed class PSTaskDataStreamWriter : IDisposable
    {
        #region Members

        private readonly PSCmdlet _cmdlet;
        private readonly PSDataCollection<PSStreamObject> _dataStream;
        private readonly int _cmdletThreadId;
        
        #endregion

        #region Constructor

        private PSTaskDataStreamWriter() { }

        /// <summary>
        /// Constructor
        /// </summary>
        public PSTaskDataStreamWriter(PSCmdlet psCmdlet)
        {
            _cmdlet = psCmdlet;
            _cmdletThreadId = Thread.CurrentThread.ManagedThreadId;
            _dataStream = new PSDataCollection<PSStreamObject>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Add data stream object to the writer
        /// </summary>
        public void Add(PSStreamObject streamObject)
        {
            _dataStream.Add(streamObject);
        }

        /// <summary>
        /// Write all objects in data stream collection to the cmdlet data stream
        /// </summary>
        public void WriteImmediate()
        {
            CheckCmdletThread();

            foreach (var item in _dataStream.ReadAll())
            {
                item.WriteStreamObject(_cmdlet, true);
            }
        }

        /// <summary>
        /// Waits for data stream objects to be added to the collection, and writes them
        /// to the cmdlet data stream.
        /// This method returns only after the writer has been closed.
        /// </summary>
        public void WaitAndWrite()
        {
            CheckCmdletThread();

            while (true)
            {
                _dataStream.WaitHandle.WaitOne();
                WriteImmediate();

                if (!_dataStream.IsOpen)
                {
                    WriteImmediate();
                    break;
                }
            }
        }

        /// <summary>
        /// Closes the stream writer
        /// </summary>
        public void Close()
        {
            _dataStream.Complete();
        }

        #endregion

        #region Private Methods

        private void CheckCmdletThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != _cmdletThreadId)
            {
                throw new PSInvalidOperationException(InternalCommandStrings.PSTaskStreamWriterWrongThread);
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes the stream writer
        /// </summary>
        public void Dispose()
        {
            _dataStream.Dispose();
        }

        #endregion
    }

    #endregion

    #region PSTaskPool

    /// <summary>
    /// Pool for running PSTasks, with limit of total number of running tasks at a time.
    /// </summary>
    internal sealed class PSTaskPool : IDisposable
    {
        #region Members

        private readonly int _sizeLimit;
        private bool _isOpen;
        private readonly object _syncObject;
        private readonly ManualResetEvent _addAvailable;
        private readonly ManualResetEvent _stopAll;
        private readonly Dictionary<int, PSTaskBase> _taskPool;

        #endregion

        #region Constructor

        private PSTaskPool() { }

        /// <summary>
        /// Constructor
        /// </summary>
        public PSTaskPool(int size)
        {
            _sizeLimit = size;
            _isOpen = true;
            _syncObject = new object();
            _addAvailable = new ManualResetEvent(true);
            _stopAll = new ManualResetEvent(false);
            _taskPool = new Dictionary<int, PSTaskBase>(size);
        }

        #endregion

        #region Events

        /// <summary>
        /// Event that fires when pool is closed and drained of all tasks
        /// </summary>
        public event EventHandler<EventArgs> PoolComplete;

        #endregion

        #region Public Properties

        /// <summary>
        /// Returns true if pool is currently open for accepting tasks
        /// </summary>
        public bool IsOpen
        {
            get { return _isOpen; }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose task pool
        /// </summary>
        public void Dispose()
        {
            _addAvailable.Dispose();
            _stopAll.Dispose();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Method to add a task to the pool.
        /// If the pool is full, then this method blocks until space is available.
        /// This method is not multi-thread safe and assumes only one thread waits and adds tasks.
        /// </summary>
        public bool Add(PSTaskBase task)
        {
            if (! _isOpen)
            {
                return false;
            }

            // Block until either room is available or a stop is commanded
            var index = WaitHandle.WaitAny(new WaitHandle[] {
                _addAvailable,  // index 0
                _stopAll        // index 1
            });
            
            if (index == 1)
            {
                return false;
            }

            task.StateChanged += (sender, args) => HandleTaskStateChanged(sender, args);

            lock (_syncObject)
            {
                if (! _isOpen)
                {
                    return false;
                }

                _taskPool.Add(task.Id, task);
                if (_taskPool.Count == _sizeLimit)
                {
                    _addAvailable.Reset();
                }

                task.Start();
            }

            return true;
        }

        /// <summary>
        /// Add child job task to task pool
        /// </summary>
        public bool Add(PSTaskChildJob childJob)
        {
            return Add(childJob.Task);
        }

        /// <summary>
        /// Signals all running tasks to stop and closes pool for any new tasks
        /// </summary>
        public void StopAll()
        {
            // Accept no more input
            Close();
            _stopAll.Set();
            
            // Stop all running tasks
            lock (_syncObject)
            {
                foreach (var task in _taskPool.Values)
                {
                    task.Dispose();
                }
            }
        }

        /// <summary>
        /// Closes the pool and prevents any new tasks from being added
        /// </summary>
        public void Close()
        {
            _isOpen = false;
            CheckForComplete();
        }

        #endregion

        #region Private Methods

        private void HandleTaskStateChanged(object sender, PSInvocationStateChangedEventArgs args)
        {
            var task = sender as PSTaskBase;
            Dbg.Assert(task != null, "State changed sender must always be PSTaskBase");
            var stateInfo = args.InvocationStateInfo;
            switch (stateInfo.State)
            {
                // Look for completed state and remove
                case PSInvocationState.Completed:
                case PSInvocationState.Stopped:
                case PSInvocationState.Failed:
                    lock (_syncObject)
                    {
                        _taskPool.Remove(task.Id);
                        if (_taskPool.Count == (_sizeLimit - 1))
                        {
                            _addAvailable.Set();
                        }
                    }
                    task.StateChanged -= (sender, args) => HandleTaskStateChanged(sender, args);
                    task.Dispose();
                    CheckForComplete();
                    break;
            }
        }

        private void CheckForComplete()
        {
            bool isTaskPoolComplete;
            lock (_syncObject)
            {
                isTaskPoolComplete = (!_isOpen && _taskPool.Count == 0);
            }

            if (isTaskPoolComplete)
            {
                try
                {
                    PoolComplete.SafeInvoke(
                        this, 
                        new EventArgs()
                    );
                }
                catch 
                {
                    Dbg.Assert(false, "Exceptions should not be thrown on event thread");
                }
            }
        }

        #endregion
    }

    #endregion

    #region PSTaskJobs

    /// <summary>
    /// Job for running ForEach-Object parallel task child jobs asynchronously
    /// </summary>
    internal sealed class PSTaskJob : Job
    {
        #region Members

        private readonly PSTaskPool _taskPool;
        private bool _isOpen;
        private bool _stopSignaled;

        #endregion

        #region Constructor

        private PSTaskJob() { }

        public PSTaskJob(
            string command,
            int throttleLimit) : base(command, string.Empty)
        {
            _taskPool = new PSTaskPool(throttleLimit);
            _isOpen = true;
            PSJobTypeName = nameof(PSTaskJob);

            _taskPool.PoolComplete += (sender, args) => HandleTaskPoolComplete(sender, args);
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Location
        /// </summary>
        public override string Location
        {
            get { return "PowerShell"; }
        }

        /// <summary>
        /// HasMoreData
        /// </summary>
        public override bool HasMoreData
        {
            get 
            { 
                foreach (var childJob in ChildJobs)
                {
                    if (childJob.HasMoreData)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// StatusMessage
        /// </summary>
        public override string StatusMessage
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// StopJob
        /// </summary>
        public override void StopJob()
        {
            _stopSignaled = true;
            SetJobState(JobState.Stopping);
            
            _taskPool.StopAll();
            SetJobState(JobState.Stopped);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _taskPool.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Add a child job to the collection.
        /// </summary>
        public bool AddJob(PSTaskChildJob childJob)
        {
            if (! _isOpen)
            {
                return false;
            }

            ChildJobs.Add(childJob);
            return true;
        }

        /// <summary>
        /// Closes this parent job to adding more child jobs and starts
        /// the child jobs running with the provided throttle limit.
        /// </summary>
        public void Start()
        {
            _isOpen = false;
            SetJobState(JobState.Running);

            // Submit jobs to the task pool, blocking when throttle limit is reached.
            // This thread will end once all jobs reach a finished state by either running
            // to completion, terminating with error, or stopped.
            System.Threading.ThreadPool.QueueUserWorkItem(
                (state) => 
                {
                    foreach (var childJob in ChildJobs)
                    {
                        _taskPool.Add((PSTaskChildJob) childJob);
                    }
                    _taskPool.Close();
                }
            );
        }

        #endregion

        #region Private Methods

        private void HandleTaskPoolComplete(object sender, EventArgs args)
        {
            try
            {
                if (_stopSignaled)
                {
                    SetJobState(JobState.Stopped, new PipelineStoppedException());
                    return;
                }

                // Final state will be 'Complete', only if all child jobs completed successfully.
                JobState finalState = JobState.Completed;
                foreach (var childJob in ChildJobs)
                {
                    if (childJob.JobStateInfo.State != JobState.Completed)
                    {
                        finalState = JobState.Failed;
                        break;
                    }
                }
                SetJobState(finalState);
            }
            catch (ObjectDisposedException) { }
        }

        #endregion
    }

    /// <summary>
    /// Task child job that wraps asynchronously running tasks
    /// </summary>
    internal sealed class PSTaskChildJob : Job
    {
        #region Members

        private readonly PSJobTask _task;

        #endregion

        #region Constructor

        private PSTaskChildJob() { }

        /// <summary>
        /// Constructor
        /// </summary>
        public PSTaskChildJob(
            ScriptBlock scriptBlock,
            Dictionary<string, object> usingValuesMap,
            object dollarUnderbar)
            : base(scriptBlock.ToString(), string.Empty)

        {
            PSJobTypeName = nameof(PSTaskChildJob);
            _task = new PSJobTask(scriptBlock, usingValuesMap, dollarUnderbar, this);
            _task.StateChanged += (sender, args) => HandleTaskStateChange(sender, args);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Child job task
        /// </summary>
        internal PSTaskBase Task
        {
            get { return _task; }
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Location
        /// </summary>
        public override string Location
        {
            get { return "PowerShell"; }
        }

        /// <summary>
        /// HasMoreData
        /// </summary>
        public override bool HasMoreData
        {
            get 
            { 
                return (this.Output.Count > 0 ||
                        this.Error.Count > 0 ||
                        this.Progress.Count > 0 ||
                        this.Verbose.Count > 0 ||
                        this.Debug.Count > 0 ||
                        this.Warning.Count > 0 ||
                        this.Information.Count > 0);
            }
        }

        /// <summary>
        /// StatusMessage
        /// </summary>
        public override string StatusMessage
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// StopJob
        /// </summary>
        public override void StopJob()
        {
            _task.SignalStop();
        }

        #endregion

        #region Private Methods

        private void HandleTaskStateChange(object sender, PSInvocationStateChangedEventArgs args)
        {
            var stateInfo = args.InvocationStateInfo;

            switch (stateInfo.State)
            {
                case PSInvocationState.Running:
                    SetJobState(JobState.Running);
                    break;

                case PSInvocationState.Stopped:
                    SetJobState(JobState.Stopped, stateInfo.Reason);
                    break;

                case PSInvocationState.Failed:
                    SetJobState(JobState.Failed, stateInfo.Reason);
                    break;

                case PSInvocationState.Completed:
                    SetJobState(JobState.Completed, stateInfo.Reason);
                    break;
            }
        }

        #endregion
    }

    #endregion
}
