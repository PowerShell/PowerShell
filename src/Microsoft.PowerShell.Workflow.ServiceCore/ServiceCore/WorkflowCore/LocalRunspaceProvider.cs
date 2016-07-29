/*
 * Copyright (c) 2011 Microsoft Corporation. All rights reserved
 */
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Security;
using System.Management.Automation.Tracing;
using System.Threading;
using Microsoft.PowerShell.Activities;

namespace Microsoft.PowerShell.Workflow
{
    internal class LocalRunspaceAsyncResult : ConnectionAsyncResult
    {
        internal LocalRunspaceAsyncResult(object state, AsyncCallback callback, Guid ownerId)
            :base(state, callback, ownerId)
        {
            
        }

        internal Runspace Runspace { get; set; }
    }

    internal class LocalRunspaceProvider : RunspaceProvider, IDisposable
    {
        private readonly TimeBasedCache<Runspace> _runspaceCache;
        private readonly int _maxRunspaces;
        private readonly PSLanguageMode? _languageMode;

        private readonly ConcurrentQueue<LocalRunspaceAsyncResult> _requests =
            new ConcurrentQueue<LocalRunspaceAsyncResult>();

        private readonly ConcurrentQueue<LocalRunspaceAsyncResult> _callbacks =
            new ConcurrentQueue<LocalRunspaceAsyncResult>();

        private int _isServicing;
        private int _isServicingCallbacks;
        private const int Servicing = 1;
        private const int NotServicing = 0;
        private readonly PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();

        /// <summary>
        /// -1 indicates unlimited
        /// </summary>
        private const int MaxRunspacesPossible = -1;
        private const int DefaultMaxRunspaces = MaxRunspacesPossible;

        internal TimeBasedCache<Runspace> RunspaceCache
        {
            get { return _runspaceCache; }
        }

        internal LocalRunspaceProvider(int timeoutSeconds, PSLanguageMode? languageMode) : this(timeoutSeconds, DefaultMaxRunspaces, languageMode)
        {

        }

        internal LocalRunspaceProvider(int timeoutSeconds, int maxRunspaces, PSLanguageMode? languageMode)
        {
            _runspaceCache = new TimeBasedCache<Runspace>(timeoutSeconds);
            _maxRunspaces = maxRunspaces;
            _languageMode = languageMode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionInfo"></param>
        /// <param name="retryCount"></param>
        /// <param name="retryInterval"></param>
        /// <returns></returns>
        public override Runspace GetRunspace(WSManConnectionInfo connectionInfo, uint retryCount, uint retryInterval)
        {
            IAsyncResult asyncResult = BeginGetRunspace(connectionInfo, 0, 0, null, null);

            return EndGetRunspace(asyncResult);
        }

        private Runspace AssignRunspaceIfPossible(PSLanguageMode? sourceLanguageMode = null)
        {
            Runspace runspace = null;
            PSLanguageMode languageMode = (sourceLanguageMode != null) ? sourceLanguageMode.Value :
                (_languageMode != null) ? _languageMode.Value : GetSystemLanguageMode();
            lock (_runspaceCache.TimerServicingSyncObject)
            {
                // Retrieve or create a local runspace having the same language mode as the source, if provided.
                foreach (Item<Runspace> item in _runspaceCache.Cast<Item<Runspace>>().Where(item => !item.Busy))
                {
                    if (item.Value.SessionStateProxy.LanguageMode == languageMode)
                    {
                        item.Idle = false;
                        item.Busy = true;
                        runspace = item.Value;
                        break;
                    }
                }

                if ((runspace == null || runspace.RunspaceStateInfo.State != RunspaceState.Opened) &&
                    (_maxRunspaces == MaxRunspacesPossible || _runspaceCache.Cache.Count < _maxRunspaces))
                {
                    runspace = CreateLocalActivityRunspace(languageMode);
                    
                    runspace.Open();
                    _tracer.WriteMessage("New local runspace created");

                    _runspaceCache.Add(new Item<Runspace>(runspace, runspace.InstanceId));
                }
            }

            return runspace;
        }

        private static PSLanguageMode GetSystemLanguageMode()
        {
            return (SystemPolicy.GetSystemLockdownPolicy() == SystemEnforcementMode.Enforce) ?
                PSLanguageMode.ConstrainedLanguage : PSLanguageMode.FullLanguage;
        }

        private void TraceThreadPoolInfo(string message)
        {
#if DEBUG
            int maxWorkerThreads, maxCompletionPortThreads, availableWorkerThreads, availableCompletionPortThreads,
                minWorkerThreads, minCompletionPortThreads;

            ThreadPool.GetMinThreads(out minWorkerThreads, out minCompletionPortThreads);
            ThreadPool.GetMaxThreads(out maxWorkerThreads, out maxCompletionPortThreads);
            ThreadPool.GetAvailableThreads(out availableWorkerThreads, out availableCompletionPortThreads);

            _tracer.WriteMessage(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                               "LocalRunspaceProvider: {0} minwt:{1} mincpt:{2} wt:{3} ct:{4} awt:{5} act:{6}",
                                               message, minWorkerThreads, minCompletionPortThreads, maxWorkerThreads,
                                               maxCompletionPortThreads, availableWorkerThreads,
                                               availableCompletionPortThreads));
#else
            _tracer.WriteMessage(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                               "PSW ConnMgr: {0}",
                                               message));
#endif
        }

        /// <summary>
        /// Checks to see if a thread is already servicing and if not starts one
        /// </summary>
        private void CheckAndStartRequestServicingThread()
        {
            if (Interlocked.CompareExchange(ref _isServicing, Servicing, NotServicing) != NotServicing) return;

            TraceThreadPoolInfo("QueueUserWorkItem Request Servicing thread");
            ThreadPool.QueueUserWorkItem(ServiceRequests);
        }

        private void ServiceRequests(object state)
        {
            Debug.Assert(_maxRunspaces != MaxRunspacesPossible,
                         "When infinite runspaces are specified, then we should never hit the servicing thread");

            lock(_runspaceCache.TimerServicingSyncObject)
            {
                LocalRunspaceAsyncResult asyncResult;

                Runspace runspace = AssignRunspaceIfPossible();

                bool assigned = false;
                while (runspace != null && _requests.TryDequeue(out asyncResult))
                {
                    asyncResult.Runspace = runspace;
                    assigned = true;
                    AddToPendingCallbacks(asyncResult);

                    runspace = _runspaceCache.Cache.Count < _maxRunspaces ? AssignRunspaceIfPossible() : null;
                }

                if (!assigned && runspace != null)
                    ReleaseRunspace(runspace);
            }

            Interlocked.CompareExchange(ref _isServicing, NotServicing, Servicing);
        }

        private void AddToPendingCallbacks(LocalRunspaceAsyncResult asyncResult)
        {
            _callbacks.Enqueue(asyncResult);

            if (Interlocked.CompareExchange(ref _isServicingCallbacks, Servicing, NotServicing) != NotServicing) return;

            TraceThreadPoolInfo("Callback thread");
            ThreadPool.QueueUserWorkItem(ServiceCallbacks);
        }

        private void ServiceCallbacks(object state)
        {
            LocalRunspaceAsyncResult result;
            while (_callbacks.TryDequeue(out result))
            {
                result.SetAsCompleted(null);
            }
            Interlocked.CompareExchange(ref _isServicingCallbacks, NotServicing, Servicing);
        }

        /// <summary>
        /// Begin for obtaining a runspace for the specified ConnectionInfo
        /// </summary>
        /// <param name="connectionInfo">connection info to be used for remote connections</param>
        /// <param name="retryCount">number of times to retry</param>
        /// <param name="callback">optional user defined callback</param>
        /// <param name="state">optional user specified state</param>
        /// <param name="retryInterval">time in milliseconds before the next retry has to be attempted</param>
        /// <returns>async result</returns>
        public override IAsyncResult BeginGetRunspace(WSManConnectionInfo connectionInfo, uint retryCount, uint retryInterval, AsyncCallback callback, object state)
        {
            if (connectionInfo != null)
            {
                throw new InvalidOperationException();
            }

            LocalRunspaceAsyncResult asyncResult = new LocalRunspaceAsyncResult(state, callback, Guid.Empty);

            // Get the source language mode from the activity arguments if available and pass to runspace fetching.
            PSLanguageMode? sourceLanguageMode = null;
            RunCommandsArguments args = state as RunCommandsArguments;
            if (args != null)
            {
                PSWorkflowRuntime wfRuntime = args.WorkflowHost as PSWorkflowRuntime;
                if (wfRuntime != null)
                {
                    PSWorkflowJob wfJob = wfRuntime.JobManager.GetJob(args.PSActivityContext.JobInstanceId);
                    if (wfJob != null)
                    {
                        sourceLanguageMode = wfJob.SourceLanguageMode;
                    }
                }
            }

            Runspace runspace = AssignRunspaceIfPossible(sourceLanguageMode);
            if (runspace != null)
            {
                asyncResult.Runspace = runspace;
                asyncResult.CompletedSynchronously = true;
                asyncResult.SetAsCompleted(null);
            }
            else
            {
                // queue the request
                _requests.Enqueue(asyncResult);
                CheckAndStartRequestServicingThread();
            }

            return asyncResult;
        }
        
        /// <summary>
        /// End for obtaining a runspace for the specified connection info
        /// </summary>
        /// <param name="asyncResult">async result to end on</param>
        /// <returns>remote runspace to invoke commands on</returns>
        public override Runspace EndGetRunspace(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");

            LocalRunspaceAsyncResult result = asyncResult as LocalRunspaceAsyncResult;

            if (result == null)
                throw new ArgumentException(Resources.InvalidAsyncResultSpecified, "asyncResult");

            // this will throw an exeption when a runspace is not successfully
            // available
            result.EndInvoke();

            Debug.Assert(result.Runspace != null, "EndInvoke() should throw an exception if runspace is null");

            _tracer.WriteMessage("LocalRunspaceProvider: Request serviced and runspace returned");
            Runspace runspace = result.Runspace;

            Debug.Assert(runspace.RunspaceStateInfo.State == RunspaceState.Opened, "Only opened runspace should be returned");

            return runspace;  
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="runspace"></param>
        public override void ReleaseRunspace(Runspace runspace)
        {
            runspace.ResetRunspaceState();
            lock(_runspaceCache.TimerServicingSyncObject)
            {
                bool found = false;
                foreach (Item<Runspace> item in
                    _runspaceCache.Cast<Item<Runspace>>().Where(item => item.InstanceId == runspace.InstanceId))
                {
                    item.Busy = false;
                    found = true;
                }

                if (!found)
                    throw new InvalidOperationException();
            }

            if (_maxRunspaces != MaxRunspacesPossible)
                CheckAndStartRequestServicingThread();
        }

        private readonly static object SyncObject = new object();
        private static TypeTable _sharedTypeTable;
        internal static TypeTable SharedTypeTable
        {
            get
            {
                if (_sharedTypeTable == null)
                {
                    lock (SyncObject)
                    {
                        if (_sharedTypeTable == null)
                        {
                            _sharedTypeTable = TypeTable.LoadDefaultTypeFiles();
                        }
                    }
                }
                return _sharedTypeTable;
            }
        }

        private static InitialSessionState GetInitialSessionStateWithSharedTypesAndNoFormat()
        {
            InitialSessionState initialSessionState = InitialSessionState.CreateDefault();

            // clear existing types and format
            initialSessionState.Types.Clear();
            initialSessionState.Formats.Clear();

            // add default types and formats
            initialSessionState.Types.Add(new SessionStateTypeEntry(SharedTypeTable.Clone(unshared: true)));
            initialSessionState.DisableFormatUpdates = true;

            return initialSessionState;
        }

        /// <summary>
        /// Creates a local runspace with the autoloading turned on.
        /// </summary>
        /// <returns></returns>
        internal static Runspace CreateLocalActivityRunspace(PSLanguageMode? languageMode = null, bool useCurrentThreadForExecution = true)
        {
            InitialSessionState iss = GetInitialSessionStateWithSharedTypesAndNoFormat();
            if (SystemPolicy.GetSystemLockdownPolicy() == SystemEnforcementMode.Enforce)
            {
                iss.LanguageMode = PSLanguageMode.ConstrainedLanguage;
            }

            if (languageMode != null && languageMode.HasValue)
            {
                iss.LanguageMode = languageMode.Value;
            }

            // Add a variable indicating that we're in Workflow endpoint. This enables the
            // autoloading feature.
            SessionStateVariableEntry ssve = new SessionStateVariableEntry("RunningInPSWorkflowEndpoint",
                true, "True if we're in a Workflow Endpoint", ScopedItemOptions.Constant);
            iss.Variables.Add(ssve);
            Runspace runspace = RunspaceFactory.CreateRunspace(iss);

            if (useCurrentThreadForExecution)
                runspace.ThreadOptions = PSThreadOptions.UseCurrentThread;

            return runspace;
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                _runspaceCache.Dispose();
            }
        }

        /// <summary>
        /// Helper method only to use from test
        /// </summary>
        internal void Reset()
        {
            foreach(Item<Runspace> item in _runspaceCache)
            {
                item.Value.Dispose();
            }
            _runspaceCache.Cache.Clear();
        }
    }
}
