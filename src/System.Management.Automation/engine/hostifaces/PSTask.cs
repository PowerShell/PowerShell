// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation.Host;
using System.Management.Automation.Language;
using System.Management.Automation.Remoting.Internal;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Security;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.PSTasks
{
    #region PSTask

    /// <summary>
    /// Class to encapsulate synchronous running scripts in parallel.
    /// </summary>
    internal sealed class PSTask : PSTaskBase
    {
        #region Members

        private readonly PSTaskDataStreamWriter _dataStreamWriter;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="PSTask"/> class.
        /// </summary>
        /// <param name="scriptBlock">Script block to run in task.</param>
        /// <param name="usingValuesMap">Using values passed into script block.</param>
        /// <param name="dollarUnderbar">Dollar underbar variable value.</param>
        /// <param name="currentLocationPath">Current working directory.</param>
        /// <param name="dataStreamWriter">Cmdlet data stream writer.</param>
        public PSTask(
            ScriptBlock scriptBlock,
            Dictionary<string, object> usingValuesMap,
            object dollarUnderbar,
            string currentLocationPath,
            PSTaskDataStreamWriter dataStreamWriter)
            : base(
                scriptBlock,
                usingValuesMap,
                dollarUnderbar,
                currentLocationPath)
        {
            _dataStreamWriter = dataStreamWriter;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Initialize PowerShell object.
        /// </summary>
        protected override void InitializePowershell()
        {
            // Writer data stream handlers
            _output.DataAdded += (sender, args) => HandleOutputData();
            _powershell.Streams.Error.DataAdded += (sender, args) => HandleErrorData();
            _powershell.Streams.Warning.DataAdded += (sender, args) => HandleWarningData();
            _powershell.Streams.Verbose.DataAdded += (sender, args) => HandleVerboseData();
            _powershell.Streams.Debug.DataAdded += (sender, args) => HandleDebugData();
            _powershell.Streams.Progress.DataAdded += (sender, args) => HandleProgressData();
            _powershell.Streams.Information.DataAdded += (sender, args) => HandleInformationData();

            // State change handler
            _powershell.InvocationStateChanged += (sender, args) => HandleStateChanged(args);
        }

        #endregion

        #region Writer data stream handlers

        private void HandleOutputData()
        {
            foreach (var item in _output.ReadAll())
            {
                _dataStreamWriter.Add(
                    new PSStreamObject(PSStreamObjectType.Output, item));
            }
        }

        private void HandleErrorData()
        {
            foreach (var item in _powershell.Streams.Error.ReadAll())
            {
                _dataStreamWriter.Add(
                    new PSStreamObject(PSStreamObjectType.Error, item));
            }
        }

        private void HandleWarningData()
        {
            foreach (var item in _powershell.Streams.Warning.ReadAll())
            {
                _dataStreamWriter.Add(
                    new PSStreamObject(PSStreamObjectType.Warning, item.Message));
            }
        }

        private void HandleVerboseData()
        {
            foreach (var item in _powershell.Streams.Verbose.ReadAll())
            {
                _dataStreamWriter.Add(
                    new PSStreamObject(PSStreamObjectType.Verbose, item.Message));
            }
        }

        private void HandleDebugData()
        {
            foreach (var item in _powershell.Streams.Debug.ReadAll())
            {
                _dataStreamWriter.Add(
                    new PSStreamObject(PSStreamObjectType.Debug, item.Message));
            }
        }

        private void HandleInformationData()
        {
            foreach (var item in _powershell.Streams.Information.ReadAll())
            {
                _dataStreamWriter.Add(
                    new PSStreamObject(PSStreamObjectType.Information, item));
            }
        }

        private void HandleProgressData()
        {
            foreach (var item in _powershell.Streams.Progress.ReadAll())
            {
                _dataStreamWriter.Add(
                    new PSStreamObject(PSStreamObjectType.Progress, item));
            }
        }

        #endregion

        #region Event handlers

        private void HandleStateChanged(PSInvocationStateChangedEventArgs stateChangeInfo)
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
                        new PSStreamObject(PSStreamObjectType.Error, errorRecord));
                }
            }

            RaiseStateChangedEvent(stateChangeInfo);
        }

        #endregion
    }

    /// <summary>
    /// Class to encapsulate asynchronous running scripts in parallel as jobs.
    /// </summary>
    internal sealed class PSJobTask : PSTaskBase
    {
        #region Members

        private readonly Job _job;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="PSJobTask"/> class.
        /// </summary>
        /// <param name="scriptBlock">Script block to run.</param>
        /// <param name="usingValuesMap">Using variable values passed to script block.</param>
        /// <param name="dollarUnderbar">Dollar underbar variable value for script block.</param>
        /// <param name="currentLocationPath">Current working directory.</param>
        /// <param name="job">Job object associated with task.</param>
        public PSJobTask(
            ScriptBlock scriptBlock,
            Dictionary<string, object> usingValuesMap,
            object dollarUnderbar,
            string currentLocationPath,
            Job job) : base(
                scriptBlock,
                usingValuesMap,
                dollarUnderbar,
                currentLocationPath)
        {
            _job = job;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Initialize PowerShell object.
        /// </summary>
        protected override void InitializePowershell()
        {
            // Job data stream handlers
            _output.DataAdded += (sender, args) => HandleJobOutputData();
            _powershell.Streams.Error.DataAdded += (sender, args) => HandleJobErrorData();
            _powershell.Streams.Warning.DataAdded += (sender, args) => HandleJobWarningData();
            _powershell.Streams.Verbose.DataAdded += (sender, args) => HandleJobVerboseData();
            _powershell.Streams.Debug.DataAdded += (sender, args) => HandleJobDebugData();
            _powershell.Streams.Information.DataAdded += (sender, args) => HandleJobInformationData();

            // State change handler
            _powershell.InvocationStateChanged += (sender, args) => HandleStateChanged(args);
        }

        #endregion

        #region Job data stream handlers

        private void HandleJobOutputData()
        {
            foreach (var item in _output.ReadAll())
            {
                _job.Output.Add(item);
                _job.Results.Add(
                    new PSStreamObject(PSStreamObjectType.Output, item));
            }
        }

        private void HandleJobErrorData()
        {
            foreach (var item in _powershell.Streams.Error.ReadAll())
            {
                _job.Error.Add(item);
                _job.Results.Add(
                    new PSStreamObject(PSStreamObjectType.Error, item));
            }
        }

        private void HandleJobWarningData()
        {
            foreach (var item in _powershell.Streams.Warning.ReadAll())
            {
                _job.Warning.Add(item);
                _job.Results.Add(
                    new PSStreamObject(PSStreamObjectType.Warning, item.Message));
            }
        }

        private void HandleJobVerboseData()
        {
            foreach (var item in _powershell.Streams.Verbose.ReadAll())
            {
                _job.Verbose.Add(item);
                _job.Results.Add(
                    new PSStreamObject(PSStreamObjectType.Verbose, item.Message));
            }
        }

        private void HandleJobDebugData()
        {
            foreach (var item in _powershell.Streams.Debug.ReadAll())
            {
                _job.Debug.Add(item);
                _job.Results.Add(
                    new PSStreamObject(PSStreamObjectType.Debug, item.Message));
            }
        }

        private void HandleJobInformationData()
        {
            foreach (var item in _powershell.Streams.Information.ReadAll())
            {
                _job.Information.Add(item);
                _job.Results.Add(
                    new PSStreamObject(PSStreamObjectType.Information, item));
            }
        }

        #endregion

        #region Event handlers

        private void HandleStateChanged(PSInvocationStateChangedEventArgs stateChangeInfo)
        {
            RaiseStateChangedEvent(stateChangeInfo);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Debugger.
        /// </summary>
        public Debugger Debugger
        {
            get => _powershell.Runspace.Debugger;
        }

        #endregion
    }

    /// <summary>
    /// Base class to encapsulate running a PowerShell script concurrently in a cmdlet or job context.
    /// </summary>
    internal abstract class PSTaskBase : IDisposable
    {
        #region Members

        private readonly ScriptBlock _scriptBlockToRun;
        private readonly Dictionary<string, object> _usingValuesMap;
        private readonly object _dollarUnderbar;
        private readonly int _id;
        private readonly string _currentLocationPath;
        private Runspace _runspace;
        protected PowerShell _powershell;
        protected PSDataCollection<PSObject> _output;

        public const string RunspaceName = "PSTask";

        private static int s_taskId;

        #endregion

        #region Events

        /// <summary>
        /// Event that fires when the task running state changes.
        /// </summary>
        public event EventHandler<PSInvocationStateChangedEventArgs> StateChanged;

        internal void RaiseStateChangedEvent(PSInvocationStateChangedEventArgs args)
        {
            StateChanged.SafeInvoke(this, args);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets current running state of the task.
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
        /// Gets Task Id.
        /// </summary>
        public int Id { get => _id; }

        /// <summary>
        /// Gets Task Runspace.
        /// </summary>
        public Runspace Runspace { get => _runspace; }

        #endregion

        #region Constructor

        private PSTaskBase()
        {
            _id = Interlocked.Increment(ref s_taskId);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PSTaskBase"/> class.
        /// </summary>
        /// <param name="scriptBlock">Script block to run.</param>
        /// <param name="usingValuesMap">Using variable values passed to script block.</param>
        /// <param name="dollarUnderbar">Dollar underbar variable value.</param>
        /// <param name="currentLocationPath">Current working directory.</param>
        protected PSTaskBase(
            ScriptBlock scriptBlock,
            Dictionary<string, object> usingValuesMap,
            object dollarUnderbar,
            string currentLocationPath) : this()
        {
            _scriptBlockToRun = scriptBlock;
            _usingValuesMap = usingValuesMap;
            _dollarUnderbar = dollarUnderbar;
            _currentLocationPath = currentLocationPath;
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Initialize PowerShell object.
        /// </summary>
        protected abstract void InitializePowershell();

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose PSTaskBase instance.
        /// </summary>
        public void Dispose()
        {
            _powershell.Dispose();
            _output.Dispose();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Start task.
        /// </summary>
        /// <param name="runspace">Runspace used to run task.</param>
        public void Start(Runspace runspace)
        {
            if (_powershell != null)
            {
                Dbg.Assert(false, "A PSTask can be started only once.");
                return;
            }

            Dbg.Assert(runspace != null, "Task runspace cannot be null.");
            _runspace = runspace;

            // If available, set current working directory on the runspace.
            // Temporarily set the newly created runspace as the thread default runspace for any needed module loading.
            if (_currentLocationPath != null)
            {
                var oldDefaultRunspace = Runspace.DefaultRunspace;
                try
                {
                    Runspace.DefaultRunspace = runspace;
                    var context = new CmdletProviderContext(runspace.ExecutionContext)
                    {
                        // _currentLocationPath denotes the current path as-is, and should not be attempted expanded.
                        SuppressWildcardExpansion = true
                    };
                    runspace.ExecutionContext.SessionState.Internal.SetLocation(_currentLocationPath, context);
                }
                catch (DriveNotFoundException)
                {
                    // Allow task to run if current drive is not available.
                }
                finally
                {
                    Runspace.DefaultRunspace = oldDefaultRunspace;
                }
            }

            // Create the PowerShell command pipeline for the provided script block
            // The script will run on the provided Runspace in a new thread by default
            _powershell = PowerShell.Create(runspace);

            // Initialize PowerShell object data streams and event handlers
            _output = new PSDataCollection<PSObject>();
            InitializePowershell();

            // Start the script running in a new thread
            _powershell.AddScript(_scriptBlockToRun.ToString());
            _powershell.Commands.Commands[0].DollarUnderbar = _dollarUnderbar;
            if (_usingValuesMap != null && _usingValuesMap.Count > 0)
            {
                _powershell.AddParameter(Parser.VERBATIM_ARGUMENT, _usingValuesMap);
            }

            _powershell.BeginInvoke<object, PSObject>(input: null, output: _output);
        }

        /// <summary>
        /// Signals the running task to stop.
        /// </summary>
        public void SignalStop() => _powershell?.BeginStop(null, null);

        #endregion
    }

    #endregion

    #region PSTaskDataStreamWriter

    /// <summary>
    /// Class that handles writing task data stream objects to a cmdlet.
    /// </summary>
    internal sealed class PSTaskDataStreamWriter : IDisposable
    {
        #region Members

        private readonly PSCmdlet _cmdlet;
        private readonly PSDataCollection<PSStreamObject> _dataStream;
        private readonly int _cmdletThreadId;

        #endregion

        #region Properties

        /// <summary>
        /// Gets wait-able handle that signals when new data has been added to
        /// the data stream collection.
        /// </summary>
        /// <returns>Data added wait handle.</returns>
        internal WaitHandle DataAddedWaitHandle
        {
            get => _dataStream.WaitHandle;
        }

        #endregion

        #region Constructor

        private PSTaskDataStreamWriter() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PSTaskDataStreamWriter"/> class.
        /// </summary>
        /// <param name="psCmdlet">Parent cmdlet.</param>
        public PSTaskDataStreamWriter(PSCmdlet psCmdlet)
        {
            _cmdlet = psCmdlet;
            _cmdletThreadId = Environment.CurrentManagedThreadId;
            _dataStream = new PSDataCollection<PSStreamObject>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Add data stream object to the writer.
        /// </summary>
        /// <param name="streamObject">Data stream object to write.</param>
        public void Add(PSStreamObject streamObject)
        {
            _dataStream.Add(streamObject);
        }

        /// <summary>
        /// Write all objects in data stream collection to the cmdlet data stream.
        /// </summary>
        public void WriteImmediate()
        {
            CheckCmdletThread();

            foreach (var item in _dataStream.ReadAll())
            {
                item.WriteStreamObject(cmdlet: _cmdlet, overrideInquire: true);
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
        /// Closes the stream writer.
        /// </summary>
        public void Close()
        {
            _dataStream.Complete();
        }

        #endregion

        #region Private Methods

        private void CheckCmdletThread()
        {
            if (Environment.CurrentManagedThreadId != _cmdletThreadId)
            {
                throw new PSInvalidOperationException(InternalCommandStrings.PSTaskStreamWriterWrongThread);
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose the stream writer.
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

        private readonly ManualResetEvent _addAvailable;
        private readonly int _sizeLimit;
        private readonly ManualResetEvent _stopAll;
        private readonly object _syncObject;
        private readonly Dictionary<int, PSTaskBase> _taskPool;
        private readonly ConcurrentQueue<Runspace> _runspacePool;
        private readonly ConcurrentDictionary<int, Runspace> _activeRunspaces;
        private readonly WaitHandle[] _waitHandles;
        private readonly bool _useRunspacePool;
        private bool _isOpen;
        private bool _stopping;
        private int _createdRunspaceCount;

        private const int AddAvailable = 0;
        private const int Stop = 1;

        #endregion

        #region Constructor

        private PSTaskPool() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PSTaskPool"/> class.
        /// </summary>
        /// <param name="size">Total number of allowed running objects in pool at one time.</param>
        /// <param name="useNewRunspace">When true, a new runspace object is created for the task instead of reusing one from the pool.</param>
        public PSTaskPool(
            int size,
            bool useNewRunspace)
        {
            _sizeLimit = size;
            _useRunspacePool = !useNewRunspace;
            _isOpen = true;
            _syncObject = new object();
            _addAvailable = new ManualResetEvent(true);
            _stopAll = new ManualResetEvent(false);
            _waitHandles = new WaitHandle[]
            {
                _addAvailable,      // index 0
                _stopAll,           // index 1
            };
            _taskPool = new Dictionary<int, PSTaskBase>(size);
            _activeRunspaces = new ConcurrentDictionary<int, Runspace>();
            if (_useRunspacePool)
            {
                _runspacePool = new ConcurrentQueue<Runspace>();
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Event that fires when pool is closed and drained of all tasks.
        /// </summary>
        public event EventHandler<EventArgs> PoolComplete;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether a pool is currently open for accepting tasks.
        /// </summary>
        public bool IsOpen
        {
            get => _isOpen;
        }

        /// <summary>
        /// Gets a value of the count of total runspaces allocated.
        /// </summary>
        public int AllocatedRunspaceCount
        {
            get => _createdRunspaceCount;
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose task pool.
        /// </summary>
        public void Dispose()
        {
            _addAvailable.Dispose();
            _stopAll.Dispose();

            DisposeRunspaces();
        }

        /// <summary>
        /// Dispose runspaces.
        /// </summary>
        internal void DisposeRunspaces()
        {
            foreach (var item in _activeRunspaces)
            {
                item.Value.Dispose();
            }

            _activeRunspaces.Clear();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Method to add a task to the pool.
        /// If the pool is full, then this method blocks until space is available.
        /// This method is not multi-thread safe and assumes only one thread waits and adds tasks.
        /// </summary>
        /// <param name="task">Task to be added to pool.</param>
        /// <returns>True when task is successfully added.</returns>
        public bool Add(PSTaskBase task)
        {
            if (!_isOpen)
            {
                return false;
            }

            // Block until either space is available, or a stop is commanded
            var index = WaitHandle.WaitAny(_waitHandles);

            switch (index)
            {
                case AddAvailable:
                    var runspace = GetRunspace(task.Id);
                    task.StateChanged += HandleTaskStateChangedDelegate;
                    lock (_syncObject)
                    {
                        if (!_isOpen)
                        {
                            return false;
                        }

                        _taskPool.Add(task.Id, task);
                        if (_taskPool.Count == _sizeLimit)
                        {
                            _addAvailable.Reset();
                        }

                        task.Start(runspace);
                    }

                    return true;

                case Stop:
                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Add child job task to task pool.
        /// </summary>
        /// <param name="childJob">Child job to be added to pool.</param>
        /// <returns>True when child job is successfully added.</returns>
        public bool Add(PSTaskChildJob childJob)
        {
            return Add(childJob.Task);
        }

        /// <summary>
        /// Signals all running tasks to stop and closes pool for any new tasks.
        /// </summary>
        public void StopAll()
        {
            _stopping = true;

            // Accept no more input
            Close();
            _stopAll.Set();

            // Stop all running tasks
            PSTaskBase[] tasksToStop;
            lock (_syncObject)
            {
                tasksToStop = new PSTaskBase[_taskPool.Values.Count];
                _taskPool.Values.CopyTo(tasksToStop, 0);
            }

            foreach (var task in tasksToStop)
            {
                task.Dispose();
            }

            // Dispose all active runspaces
            DisposeRunspaces();
            _stopping = false;
        }

        /// <summary>
        /// Closes the pool and prevents any new tasks from being added.
        /// </summary>
        public void Close()
        {
            _isOpen = false;
            CheckForComplete();
        }

        #endregion

        #region Private Methods

        private void HandleTaskStateChangedDelegate(object sender, PSInvocationStateChangedEventArgs args) => HandleTaskStateChanged(sender, args);

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
                    ReturnRunspace(task);
                    lock (_syncObject)
                    {
                        _taskPool.Remove(task.Id);
                        if (_taskPool.Count == (_sizeLimit - 1))
                        {
                            _addAvailable.Set();
                        }
                    }

                    task.StateChanged -= HandleTaskStateChangedDelegate;
                    if (!_stopping || stateInfo.State != PSInvocationState.Stopped)
                    {
                        // StopAll disposes tasks.
                        task.Dispose();
                    }

                    CheckForComplete();
                    break;
            }
        }

        private void CheckForComplete()
        {
            bool isTaskPoolComplete;
            lock (_syncObject)
            {
                isTaskPoolComplete = !_isOpen && _taskPool.Count == 0;
            }

            if (isTaskPoolComplete)
            {
                try
                {
                    PoolComplete.SafeInvoke(
                        this,
                        new EventArgs());
                }
                catch
                {
                    Dbg.Assert(false, "Exceptions should not be thrown on event thread");
                }
            }
        }

        private Runspace GetRunspace(int taskId)
        {
            var runspaceName = string.Create(CultureInfo.InvariantCulture, $"{PSTask.RunspaceName}:{taskId}");

            if (_useRunspacePool && _runspacePool.TryDequeue(out Runspace runspace))
            {
                if (runspace.RunspaceStateInfo.State == RunspaceState.Opened &&
                    runspace.RunspaceAvailability == RunspaceAvailability.Available)
                {
                    try
                    {
                        runspace.ResetRunspaceState();
                        runspace.Name = runspaceName;
                        return runspace;
                    }
                    catch
                    {
                        // If the runspace cannot be reset for any reason, remove it.
                    }
                }

                RemoveActiveRunspace(runspace);
            }

            // Create and initialize a new Runspace
            var iss = InitialSessionState.CreateDefault2();
            switch (SystemPolicy.GetSystemLockdownPolicy())
            {
                case SystemEnforcementMode.Enforce:
                    iss.LanguageMode = PSLanguageMode.ConstrainedLanguage;
                    break;

                case SystemEnforcementMode.Audit:
                    // In audit mode, CL restrictions are not enforced and instead audit
                    // log entries are created.
                    iss.LanguageMode = PSLanguageMode.ConstrainedLanguage;
                    break;

                case SystemEnforcementMode.None:
                    iss.LanguageMode = PSLanguageMode.FullLanguage;
                    break;
            }

            runspace = RunspaceFactory.CreateRunspace(iss);
            runspace.Name = runspaceName;
            _activeRunspaces.TryAdd(runspace.Id, runspace);
            runspace.Open();
            _createdRunspaceCount++;

            return runspace;
        }

        private void ReturnRunspace(PSTaskBase task)
        {
            var runspace = task.Runspace;
            Dbg.Assert(runspace != null, "Task runspace cannot be null.");
            if (_useRunspacePool &&
                runspace.RunspaceStateInfo.State == RunspaceState.Opened &&
                runspace.RunspaceAvailability == RunspaceAvailability.Available)
            {
                _runspacePool.Enqueue(runspace);
                return;
            }

            RemoveActiveRunspace(runspace);
        }

        private void RemoveActiveRunspace(Runspace runspace)
        {
            runspace.Dispose();
            _activeRunspaces.TryRemove(runspace.Id, out Runspace _);
        }

        #endregion
    }

    #endregion

    #region PSTaskJobs

    /// <summary>
    /// Job for running ForEach-Object parallel task child jobs asynchronously.
    /// </summary>
    public sealed class PSTaskJob : Job
    {
        #region Members

        private readonly PSTaskPool _taskPool;
        private bool _isOpen;
        private bool _stopSignaled;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value of the count of total runspaces allocated.
        /// </summary>
        public int AllocatedRunspaceCount
        {
            get => _taskPool.AllocatedRunspaceCount;
        }

        #endregion

        #region Constructor

        private PSTaskJob() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PSTaskJob"/> class.
        /// </summary>
        /// <param name="command">Job command text.</param>
        /// <param name="throttleLimit">Pool size limit for task job.</param>
        /// <param name="useNewRunspace">When true, a new runspace object is created for the task instead of reusing one from the pool.</param>
        internal PSTaskJob(
            string command,
            int throttleLimit,
            bool useNewRunspace) : base(command, string.Empty)
        {
            _taskPool = new PSTaskPool(throttleLimit, useNewRunspace);
            _isOpen = true;
            PSJobTypeName = nameof(PSTaskJob);

            _taskPool.PoolComplete += (sender, args) => HandleTaskPoolComplete(sender, args);
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Gets Location.
        /// </summary>
        public override string Location
        {
            get => "PowerShell";
        }

        /// <summary>
        /// Gets HasMoreData.
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
        /// Gets StatusMessage.
        /// </summary>
        public override string StatusMessage
        {
            get => string.Empty;
        }

        /// <summary>
        /// Stops running job.
        /// </summary>
        public override void StopJob()
        {
            _stopSignaled = true;
            SetJobState(JobState.Stopping);

            _taskPool.StopAll();
            SetJobState(JobState.Stopped);
        }

        /// <summary>
        /// Disposes task job.
        /// </summary>
        /// <param name="disposing">Indicates disposing action.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _taskPool.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Add a child job to the collection.
        /// </summary>
        /// <param name="childJob">Child job to add.</param>
        /// <returns>True when child job is successfully added.</returns>
        internal bool AddJob(PSTaskChildJob childJob)
        {
            if (!_isOpen)
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
        internal void Start()
        {
            _isOpen = false;
            SetJobState(JobState.Running);

            // Submit jobs to the task pool, blocking when throttle limit is reached.
            // This thread will end once all jobs reach a finished state by either running
            // to completion, terminating with error, or stopped.
            System.Threading.ThreadPool.QueueUserWorkItem(
                (_) =>
                {
                    foreach (var childJob in ChildJobs)
                    {
                        _taskPool.Add((PSTaskChildJob)childJob);
                    }

                    _taskPool.Close();
                });
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

                // Release job task pool runspace resources.
                (sender as PSTaskPool).DisposeRunspaces();
            }
            catch (ObjectDisposedException)
            { }
        }

        #endregion
    }

    /// <summary>
    /// PSTaskChildJob debugger wrapper.
    /// </summary>
    internal sealed class PSTaskChildDebugger : Debugger
    {
        #region Members

        private readonly Debugger _wrappedDebugger;
        private readonly string _jobName;

        #endregion

        #region Constructor

        private PSTaskChildDebugger() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PSTaskChildDebugger"/> class.
        /// </summary>
        /// <param name="debugger">Script debugger associated with task.</param>
        /// <param name="jobName">Job name for associated task.</param>
        public PSTaskChildDebugger(
            Debugger debugger,
            string jobName)
        {
            if (debugger == null)
            {
                throw new PSArgumentNullException(nameof(debugger));
            }

            _wrappedDebugger = debugger;
            _jobName = jobName ?? string.Empty;

            // Create handlers for wrapped debugger events.
            _wrappedDebugger.BreakpointUpdated += HandleBreakpointUpdated;
            _wrappedDebugger.DebuggerStop += HandleDebuggerStop;
        }

        #endregion

        #region Debugger overrides

        /// <summary>
        /// Evaluates provided command either as a debugger specific command
        /// or a PowerShell command.
        /// </summary>
        /// <param name="command">PowerShell command.</param>
        /// <param name="output">PowerShell output.</param>
        /// <returns>Debugger command results.</returns>
        public override DebuggerCommandResults ProcessCommand(
            PSCommand command,
            PSDataCollection<PSObject> output)
        {
            // Special handling for the prompt command.
            if (command.Commands[0].CommandText.Trim().Equals("prompt", StringComparison.OrdinalIgnoreCase))
            {
                return HandlePromptCommand(output);
            }

            return _wrappedDebugger.ProcessCommand(command, output);
        }

        /// <summary>
        /// Adds the provided set of breakpoints to the debugger.
        /// </summary>
        /// <param name="breakpoints">List of breakpoints.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        public override void SetBreakpoints(IEnumerable<Breakpoint> breakpoints, int? runspaceId) =>
            _wrappedDebugger.SetBreakpoints(breakpoints, runspaceId);

        /// <summary>
        /// Sets the debugger resume action.
        /// </summary>
        /// <param name="resumeAction">Debugger resume action.</param>
        public override void SetDebuggerAction(DebuggerResumeAction resumeAction)
        {
            _wrappedDebugger.SetDebuggerAction(resumeAction);
        }

        /// <summary>
        /// Get a breakpoint by id, primarily for Enable/Disable/Remove-PSBreakpoint cmdlets.
        /// </summary>
        /// <param name="id">Id of the breakpoint you want.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The breakpoint with the specified id.</returns>
        public override Breakpoint GetBreakpoint(int id, int? runspaceId) =>
            _wrappedDebugger.GetBreakpoint(id, runspaceId);

        /// <summary>
        /// Returns breakpoints on a runspace.
        /// </summary>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>A list of breakpoints in a runspace.</returns>
        public override List<Breakpoint> GetBreakpoints(int? runspaceId) =>
            _wrappedDebugger.GetBreakpoints(runspaceId);

        /// <summary>
        /// Sets a command breakpoint in the debugger.
        /// </summary>
        /// <param name="command">The name of the command that will trigger the breakpoint. This value may not be null.</param>
        /// <param name="action">The action to take when the breakpoint is hit. If null, PowerShell will break into the debugger when the breakpoint is hit.</param>
        /// <param name="path">The path to the script file where the breakpoint may be hit. If null, the breakpoint may be hit anywhere the command is invoked.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The command breakpoint that was set.</returns>
        public override CommandBreakpoint SetCommandBreakpoint(string command, ScriptBlock action, string path, int? runspaceId) =>
            _wrappedDebugger.SetCommandBreakpoint(command, action, path, runspaceId);

        /// <summary>
        /// Sets a variable breakpoint in the debugger.
        /// </summary>
        /// <param name="variableName">The name of the variable that will trigger the breakpoint. This value may not be null.</param>
        /// <param name="accessMode">The variable access mode that will trigger the breakpoint.</param>
        /// <param name="action">The action to take when the breakpoint is hit. If null, PowerShell will break into the debugger when the breakpoint is hit.</param>
        /// <param name="path">The path to the script file where the breakpoint may be hit. If null, the breakpoint may be hit anywhere the variable is accessed using the specified access mode.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The variable breakpoint that was set.</returns>
        public override VariableBreakpoint SetVariableBreakpoint(string variableName, VariableAccessMode accessMode, ScriptBlock action, string path, int? runspaceId) =>
            _wrappedDebugger.SetVariableBreakpoint(variableName, accessMode, action, path, runspaceId);

        /// <summary>
        /// Sets a line breakpoint in the debugger.
        /// </summary>
        /// <param name="path">The path to the script file where the breakpoint may be hit. This value may not be null.</param>
        /// <param name="line">The line in the script file where the breakpoint may be hit. This value must be greater than or equal to 1.</param>
        /// <param name="column">The column in the script file where the breakpoint may be hit. If 0, the breakpoint will trigger on any statement on the line.</param>
        /// <param name="action">The action to take when the breakpoint is hit. If null, PowerShell will break into the debugger when the breakpoint is hit.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The line breakpoint that was set.</returns>
        public override LineBreakpoint SetLineBreakpoint(string path, int line, int column, ScriptBlock action, int? runspaceId) =>
            _wrappedDebugger.SetLineBreakpoint(path, line, column, action, runspaceId);

        /// <summary>
        /// Enables a breakpoint in the debugger.
        /// </summary>
        /// <param name="breakpoint">The breakpoint to enable in the debugger. This value may not be null.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The updated breakpoint if it was found; null if the breakpoint was not found in the debugger.</returns>
        public override Breakpoint EnableBreakpoint(Breakpoint breakpoint, int? runspaceId) =>
            _wrappedDebugger.EnableBreakpoint(breakpoint, runspaceId);

        /// <summary>
        /// Disables a breakpoint in the debugger.
        /// </summary>
        /// <param name="breakpoint">The breakpoint to enable in the debugger. This value may not be null.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The updated breakpoint if it was found; null if the breakpoint was not found in the debugger.</returns>
        public override Breakpoint DisableBreakpoint(Breakpoint breakpoint, int? runspaceId) =>
            _wrappedDebugger.DisableBreakpoint(breakpoint, runspaceId);

        /// <summary>
        /// Removes a breakpoint from the debugger.
        /// </summary>
        /// <param name="breakpoint">The breakpoint to remove from the debugger. This value may not be null.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>True if the breakpoint was removed from the debugger; false otherwise.</returns>
        public override bool RemoveBreakpoint(Breakpoint breakpoint, int? runspaceId) =>
            _wrappedDebugger.RemoveBreakpoint(breakpoint, runspaceId);

        /// <summary>
        /// Stops a running command.
        /// </summary>
        public override void StopProcessCommand()
        {
            _wrappedDebugger.StopProcessCommand();
        }

        /// <summary>
        /// Returns current debugger stop event arguments if debugger is in
        /// debug stop state.  Otherwise returns null.
        /// </summary>
        /// <returns>Debugger stop eventArgs.</returns>
        public override DebuggerStopEventArgs GetDebuggerStopArgs()
        {
            return _wrappedDebugger.GetDebuggerStopArgs();
        }

        /// <summary>
        /// Sets the parent debugger, breakpoints, and other debugging context information.
        /// </summary>
        /// <param name="parent">Parent debugger.</param>
        /// <param name="breakPoints">List of breakpoints.</param>
        /// <param name="startAction">Debugger mode.</param>
        /// <param name="host">PowerShell host.</param>
        /// <param name="path">Current path.</param>
        public override void SetParent(
            Debugger parent,
            IEnumerable<Breakpoint> breakPoints,
            DebuggerResumeAction? startAction,
            PSHost host,
            PathInfo path)
        {
            // For now always enable step mode debugging.
            SetDebuggerStepMode(true);
        }

        /// <summary>
        /// Sets the debugger mode.
        /// </summary>
        /// <param name="mode">Debugger mode to set.</param>
        public override void SetDebugMode(DebugModes mode)
        {
            _wrappedDebugger.SetDebugMode(mode);

            base.SetDebugMode(mode);
        }

        /// <summary>
        /// Returns IEnumerable of CallStackFrame objects.
        /// </summary>
        /// <returns>Enumerable call stack.</returns>
        public override IEnumerable<CallStackFrame> GetCallStack()
        {
            return _wrappedDebugger.GetCallStack();
        }

        /// <summary>
        /// Sets debugger stepping mode.
        /// </summary>
        /// <param name="enabled">True to enable debugger step mode.</param>
        public override void SetDebuggerStepMode(bool enabled)
        {
            _wrappedDebugger.SetDebuggerStepMode(enabled);
        }

        /// <summary>
        /// Gets boolean indicating when debugger is stopped at a breakpoint.
        /// </summary>
        public override bool InBreakpoint
        {
            get => _wrappedDebugger.InBreakpoint;
        }

        #endregion

        #region Private methods

        private void HandleDebuggerStop(object sender, DebuggerStopEventArgs e)
        {
            this.RaiseDebuggerStopEvent(e);
        }

        private void HandleBreakpointUpdated(object sender, BreakpointUpdatedEventArgs e)
        {
            this.RaiseBreakpointUpdatedEvent(e);
        }

        private DebuggerCommandResults HandlePromptCommand(PSDataCollection<PSObject> output)
        {
            // Nested debugged runspace prompt should look like:
            // [DBG]: [JobName]: PS C:\>>
            string promptScript = $"'[DBG]: [{CodeGeneration.EscapeSingleQuotedStringContent(_jobName)}]: ' + "
                                  + @"""PS $($ExecutionContext.SessionState.Path.CurrentLocation.DisplayPath)>> """;
            var promptCommand = new PSCommand();
            promptCommand.AddScript(promptScript);
            _wrappedDebugger.ProcessCommand(promptCommand, output);

            return new DebuggerCommandResults(null, true);
        }

        #endregion
    }

    /// <summary>
    /// Task child job that wraps asynchronously running tasks.
    /// </summary>
    internal sealed class PSTaskChildJob : Job, IJobDebugger
    {
        #region Members

        private readonly PSJobTask _task;
        private PSTaskChildDebugger _jobDebuggerWrapper;

        #endregion

        #region Constructor

        private PSTaskChildJob() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PSTaskChildJob"/> class.
        /// </summary>
        /// <param name="scriptBlock">Script block to run.</param>
        /// <param name="usingValuesMap">Using variable values passed to script block.</param>
        /// <param name="dollarUnderbar">Dollar underbar variable value.</param>
        /// <param name="currentLocationPath">Current working directory.</param>
        public PSTaskChildJob(
            ScriptBlock scriptBlock,
            Dictionary<string, object> usingValuesMap,
            object dollarUnderbar,
            string currentLocationPath)
            : base(scriptBlock.ToString(), string.Empty)

        {
            PSJobTypeName = nameof(PSTaskChildJob);
            _task = new PSJobTask(scriptBlock, usingValuesMap, dollarUnderbar, currentLocationPath, this);
            _task.StateChanged += (sender, args) => HandleTaskStateChange(sender, args);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets child job task.
        /// </summary>
        internal PSTaskBase Task
        {
            get => _task;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Gets Location.
        /// </summary>
        public override string Location
        {
            get => "PowerShell";
        }

        /// <summary>
        /// Gets HasMoreData.
        /// </summary>
        public override bool HasMoreData
        {
            get => this.Output.Count > 0 ||
                   this.Error.Count > 0 ||
                   this.Progress.Count > 0 ||
                   this.Verbose.Count > 0 ||
                   this.Debug.Count > 0 ||
                   this.Warning.Count > 0 ||
                   this.Information.Count > 0;
        }

        /// <summary>
        /// Gets StatusMessage.
        /// </summary>
        public override string StatusMessage
        {
            get => string.Empty;
        }

        /// <summary>
        /// Stops running job.
        /// </summary>
        public override void StopJob()
        {
            _task.SignalStop();
        }

        #endregion

        #region IJobDebugger

        /// <summary>
        /// Gets Job Debugger.
        /// </summary>
        public Debugger Debugger
        {
            get
            {
                _jobDebuggerWrapper ??= new PSTaskChildDebugger(
                    _task.Debugger,
                    this.Name);

                return _jobDebuggerWrapper;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether IAsync.
        /// </summary>
        public bool IsAsync { get; set; }

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
