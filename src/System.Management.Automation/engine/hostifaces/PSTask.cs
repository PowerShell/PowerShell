// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation.Remoting.Internal;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Security;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.PSTasks
{
    #region PSTask

    /// <summary>
    /// Class to encapsulate running a PowerShell script concurrently in a cmdlet context
    /// </summary>
    internal sealed class PSTask : IDisposable
    {
        #region Members

        private readonly ScriptBlock _scriptBlockToRun;
        private readonly Dictionary<string, object> _usingValuesMap;
        private readonly object _dollarUnderbar;
        private readonly PSTaskDataStreamWriter _dataStreamWriter;
        private readonly Job2 _job;
        private readonly PSCmdlet _cmdlet;
        private readonly int _id;

        private Runspace _runspace;
        private PowerShell _powershell;
        private PSDataCollection<PSObject> _output;

        private const string VERBATIM_ARGUMENT = "--%";

        private static int s_taskId = 0;

        #endregion

        #region Events

        /// <summary>
        /// Event that fires when the task running state changes
        /// </summary>
        public event EventHandler<PSInvocationStateChangedEventArgs> StateChanged;

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

        private PSTask() 
        { 
            _id = Interlocked.Increment(ref s_taskId);
        }

        /// <summary>
        /// Constructor for data streaming
        /// </summary>
        public PSTask(
            ScriptBlock scriptBlock,
            Dictionary<string, object> usingValuesMap,
            object dollarUnderbar,
            PSTaskDataStreamWriter dataStreamWriter,
            PSCmdlet psCmdlet) : this()
        {
            _scriptBlockToRun = scriptBlock;
            _usingValuesMap = usingValuesMap;
            _dollarUnderbar = dollarUnderbar;
            _dataStreamWriter = dataStreamWriter;
            _cmdlet = psCmdlet;
        }

        /// <summary>
        /// Constructor for jobs
        /// </summary>
        public PSTask(
            ScriptBlock scriptBlock,
            Dictionary<string, object> usingValuesMap,
            object dollarUnderbar,
            Job2 job,
            PSCmdlet psCmdlet) : this()
        {
            _scriptBlockToRun = scriptBlock;
            _usingValuesMap = usingValuesMap;
            _dollarUnderbar = dollarUnderbar;
            _job = job;
            _cmdlet = psCmdlet;
        }

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
                // TODO: Localize message
                throw new PSInvalidOperationException("This task has already been started.  A task can be started only once.");
            }

            // Create and open Runspace for this task to run in
            var iss = InitialSessionState.CreateDefault2();
            iss.LanguageMode = (SystemPolicy.GetSystemLockdownPolicy() == SystemEnforcementMode.Enforce) 
                ? PSLanguageMode.ConstrainedLanguage : PSLanguageMode.FullLanguage;
            // TODO: Fix dollarUnderbar variable so that it gets passed correctly to the script block
            //       It needs to be passed to a compiled script block's localsTuple (will likely need ps.Invoke override):
            //       scriptBlock.Compile();
            //       _localsTuple = scriptBlock.MakeLocalsTuple(_runOptimizedCode);
            //       _localsTuple.SetAutomaticVariable(AutomaticVariable.Underbar, dollarUnderbar, _context);
            //      See: ScriptCommandProcessor.cs
            // TODO: Also update argument binding using variable look up to detect ScriptBlock using variable
            //       and recreate ScriptBlock instance using Ast
            iss.Variables.Add(
                new SessionStateVariableEntry("DollarUnderbar", _dollarUnderbar, string.Empty)
            );
            _runspace = RunspaceFactory.CreateRunspace(_cmdlet.Host, iss);
            _runspace.Open();

            // Create the PowerShell command pipeline for the provided script block
            // The script will run on the provided Runspace in a new thread by default
            _powershell = PowerShell.Create();
            _powershell.Runspace = _runspace;

            // Initialize PowerShell object data streams and event handlers
            _output = new PSDataCollection<PSObject>();
            if (_dataStreamWriter != null)
            {
                InitializePowerShellforDataStreaming();
            }
            else
            {
                InitializePowerShellforJobs();
            }

            // State change handler
            _powershell.InvocationStateChanged += new EventHandler<PSInvocationStateChangedEventArgs>(HandleStateChanged);

            // Start the script running in a new thread
            _powershell.AddScript(_scriptBlockToRun.ToString());
            _powershell.BeginInvoke<object, PSObject>(null, _output);
        }

        /// <summary>
        /// Signals the running task to stop
        /// </summary>
        public void SignalStop()
        {
            _powershell.BeginStop(null, null);
        }

        #endregion

        #region Private Methods

        private void InitializePowerShellforDataStreaming()
        {
            Dbg.Assert(_dataStreamWriter != null, "Data stream writer cannot be null");

            // Writer data stream handlers
            _output.DataAdded += (sender, args) =>
            {
                foreach (var item in _output.ReadAll())
                {
                    _dataStreamWriter.Add(
                        new PSStreamObject(PSStreamObjectType.Output, item)
                    );
                }
            };

            _powershell.Streams.Error.DataAdded += (sender, args) =>
            {
                foreach (var item in _powershell.Streams.Error.ReadAll())
                {
                    _dataStreamWriter.Add(
                        new PSStreamObject(PSStreamObjectType.Error, item)
                    );
                }
            };

            _powershell.Streams.Warning.DataAdded += (sender, args) =>
            {
                foreach (var item in _powershell.Streams.Warning.ReadAll())
                {
                    _dataStreamWriter.Add(
                        new PSStreamObject(PSStreamObjectType.WarningRecord, item)
                    );
                }
            };

            _powershell.Streams.Verbose.DataAdded += (sender, args) =>
            {
                foreach (var item in _powershell.Streams.Verbose.ReadAll())
                {
                    _dataStreamWriter.Add(
                        new PSStreamObject(PSStreamObjectType.Verbose, item.Message)
                    );
                }
            };

            _powershell.Streams.Debug.DataAdded += (sender, args) =>
            {
                foreach (var item in _powershell.Streams.Debug.ReadAll())
                {
                    _dataStreamWriter.Add(
                        new PSStreamObject(PSStreamObjectType.Debug, item.Message)
                    );
                }
            };

            _powershell.Streams.Information.DataAdded += (sender, args) =>
            {
                foreach (var item in _powershell.Streams.Debug.ReadAll())
                {
                    _dataStreamWriter.Add(
                        new PSStreamObject(PSStreamObjectType.Information, item)
                    );
                }
            }; 
        }

        private void InitializePowerShellforJobs()
        {
            Dbg.Assert(_job != null, "Data stream writer cannot be null");

            // Job data stream handlers
            _output.DataAdded += (sender, args) =>
            {
                foreach (var item in _output.ReadAll())
                {
                    _job.Output.Add(item);
                    _job.Results.Add(
                        new PSStreamObject(PSStreamObjectType.Output, item)
                    );
                }
            };

            _powershell.Streams.Error.DataAdded += (sender, args) =>
            {
                foreach (var item in _powershell.Streams.Error.ReadAll())
                {
                    _job.Error.Add(item);
                    _job.Results.Add(
                        new PSStreamObject(PSStreamObjectType.Error, item)
                    );
                }
            };

            _powershell.Streams.Warning.DataAdded += (sender, args) =>
            {
                foreach (var item in _powershell.Streams.Warning.ReadAll())
                {
                    _job.Warning.Add(item);
                    _job.Results.Add(
                        new PSStreamObject(PSStreamObjectType.WarningRecord, item)
                    );
                }
            };

            _powershell.Streams.Verbose.DataAdded += (sender, args) =>
            {
                foreach (var item in _powershell.Streams.Verbose.ReadAll())
                {
                    _job.Verbose.Add(item);
                    _job.Results.Add(
                        new PSStreamObject(PSStreamObjectType.Verbose, item.Message)
                    );
                }
            };

            _powershell.Streams.Debug.DataAdded += (sender, args) =>
            {
                foreach (var item in _powershell.Streams.Debug.ReadAll())
                {
                    _job.Debug.Add(item);
                    _job.Results.Add(
                        new PSStreamObject(PSStreamObjectType.Debug, item.Message)
                    );
                }
            };

            _powershell.Streams.Information.DataAdded += (sender, args) =>
            {
                foreach (var item in _powershell.Streams.Information.ReadAll())
                {
                    _job.Information.Add(item);
                    _job.Results.Add(
                        new PSStreamObject(PSStreamObjectType.Information, item)
                    );
                }
            };
        }

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

                    if (_dataStreamWriter != null)
                    {
                        _dataStreamWriter.Add(
                            new PSStreamObject(PSStreamObjectType.Error, errorRecord)
                        );
                    }
                }
            }

            StateChanged.Invoke(this, stateChangeInfo);
        }

        #endregion
    }

    #endregion

    #region PSTaskDataStreamWriter

    /// <summary>
    /// Class that handles writing data stream objects to a cmdlet
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
        /// </summary>
        public void WaitAndWrite()
        {
            CheckCmdletThread();

            while (true)
            {
                _dataStream.WaitHandle.WaitOne();

                if (!_dataStream.IsOpen)
                {
                    break;
                }

                // Data ready to write
                WriteImmediate();
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
                // TODO: Localize message
                throw new PSInvalidOperationException("The method cannot be called from this thread. This method can only be called from the same cmdlet thread that created this object instance.");
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
    /// Event arguments for TaskComplete event
    /// </summary>
    internal sealed class PSTaskCompleteEventArgs : EventArgs
    {
        public PSTask Task
        {
            get;
            private set;
        }

        private PSTaskCompleteEventArgs() { }

        public PSTaskCompleteEventArgs(PSTask task)
        {
            Task = task;
        }
    }

    /// <summary>
    /// Pool for running PSTasks, with limit of total number of running tasks at a time
    /// </summary>
    internal sealed class PSTaskPool : IDisposable
    {
        #region Members

        private readonly int _sizeLimit;
        private bool _isOpen;
        private readonly object _syncObject;
        private readonly ManualResetEvent _addAvailable;
        private readonly ManualResetEvent _stopAll;
        private readonly PSTaskDataStreamWriter _dataStreamWriter;
        private readonly Dictionary<int, PSTask> _taskPool;

        #endregion

        #region Constructor

        private PSTaskPool() { }

        /// <summary>
        /// Constructor
        /// </summary>
        public PSTaskPool(int size, PSTaskDataStreamWriter dataStreamWriter)
        {

            _sizeLimit = size;
            _dataStreamWriter = dataStreamWriter;
            _isOpen = true;
            _syncObject = new object();
            _addAvailable = new ManualResetEvent(true);
            _stopAll = new ManualResetEvent(false);
            _taskPool = new Dictionary<int, PSTask>(size);
        }

        #endregion

        #region Events

        /// <summary>
        /// Event that fires when a running task completes
        /// </summary>
        public event EventHandler<PSTaskCompleteEventArgs> TaskComplete;

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
        /// Method to add a task to the pool
        /// If the pool is full, then this method blocks until room is available
        /// This method is not multi-thread safe and assumes only one thread waits and adds tasks
        /// </summary>
        public bool Add(PSTask task)
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

            task.StateChanged += new EventHandler<PSInvocationStateChangedEventArgs>(HandleTaskStateChanged);

            lock (_syncObject)
            {
                _taskPool.Add(task.Id, task);
                if (_taskPool.Count == _sizeLimit)
                {
                    _addAvailable.Reset();
                }
            }

            task.Start();

            return true;
        }

        /// <summary>
        /// Signals all running tasks to stop and closes pool for any new tasks
        /// </summary>
        public void StopAll()
        {
            // Accept no more input
            Close();
            _stopAll.Set();
            
            // Send stop signal to any running tasks
            lock (_syncObject)
            {
                foreach (var task in _taskPool.Values)
                {
                    task.SignalStop();
                }
            }
        }

        /// <summary>
        /// Closes the pool and prevents any new tasks from being added
        /// </summary>
        public void Close()
        {
            _isOpen = false;
        }

        #endregion

        #region Private Methods

        private void HandleTaskStateChanged(object sender, PSInvocationStateChangedEventArgs args)
        {
            var task = sender as PSTask;
            Dbg.Assert(task != null, "State changed sender must always be PSTask");
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
                    task.StateChanged -= new EventHandler<PSInvocationStateChangedEventArgs>(HandleTaskStateChanged);
                    try
                    {
                        TaskComplete.Invoke(
                            this,
                            new PSTaskCompleteEventArgs(task)
                        );
                    }
                    catch
                    {
                        Dbg.Assert(false, "Exceptions should not be thrown on event thread");
                    }
                    CheckForComplete();
                    break;
            }
        }

        private void CheckForComplete()
        {
            lock (_syncObject)
            {
                if (!_isOpen && _taskPool.Count == 0)
                {
                    _dataStreamWriter.Close();

                    // TODO: Callback?
                }
            }
        }

        #endregion
    }

    #endregion
}
