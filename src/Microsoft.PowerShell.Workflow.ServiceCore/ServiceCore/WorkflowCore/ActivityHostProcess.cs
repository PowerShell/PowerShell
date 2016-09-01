/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Remoting;
using System.Management.Automation.Tracing;
using System.Management.Automation.PerformanceData;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Globalization;

namespace Microsoft.PowerShell.Workflow
{
    internal class ActivityHostCrashedEventArgs : EventArgs
    {
        internal bool FailureOnSetup { get; set; }
        internal ActivityInvoker Invoker { get; set; }
    }

    /// <summary>
    /// Encapsulates an out of process activity host
    /// </summary>
    /// <remarks>This class is not thread safe. Caller has to 
    /// ensure thread safety of accessing internal properties</remarks>
    internal class ActivityHostProcess : IDisposable
    {
        #region Private Members
        private static PSPerfCountersMgr _perfCountersMgr = PSPerfCountersMgr.Instance;
        private Runspace _runspace;
        private static readonly WSManConnectionInfo ActivityHostConnectionInfo;
        private const string ActivityHostShellUri = "http://schemas.microsoft.com/powershell/Microsoft.PowerShell.Workflow.ActivityHost";
        private static readonly string[] ActivitiesTypesFiles = new[] {
            @"%windir%\system32\windowspowershell\v1.0\modules\psworkflow\PSWorkflow.types.ps1xml"
        };
        private static readonly TypeTable ActivitiesTypeTable;
        private const int WSManLocalPort = 47001;
        private bool _busy;
        private readonly object _syncObject = new object();
        private readonly PowerShellProcessInstance _processInstance;
        private readonly PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();
        private ActivityInvoker _currentInvoker;

        /// <summary>
        /// This the period of time for which the process will
        /// remain inactive. Afterwards it will be killed
        /// </summary>
        private const int TimeOut = 5*60*1000;
        private readonly Timer _timer;

        /// <summary>
        /// Use this flag to flip between IPC process and
        /// WSMan process in localhost
        /// </summary>
        private readonly bool _useJobIPCProcess;

        private readonly PSLanguageMode? _languageMode;

        /// <summary>
        /// Creating a _PSSetVariable to set multiple variables in a single call --> PERF
        /// Naming it _PSSetVariable to avoid name collision with Set-Variable cmdlet
        /// </summary>
        private const string SetVariableFunction = @"function _PSSetVariable
        {
            [CmdletBinding()]
            param(
        
                [Parameter(Position=0)]
                [string[]]
                $Name,
        
                [Parameter(Position=1)]
                [object[]]
                $Value        
            )
    
            for($i=0; $i -lt $Name.Count; $i++)
            {
                microsoft.powershell.utility\set-variable -name $Name[$i] -value $Value[$i] -scope global
            }

            Set-StrictMode -Off
        }";

        private bool _isDisposed;
        #endregion Private Members

        #region Constructors

        /// <summary>
        /// Initialize a connection info object in the static constructor
        /// It can be reused for all connections
        /// </summary>
        static ActivityHostProcess()
        {
            ActivityHostConnectionInfo = new WSManConnectionInfo { Port = WSManLocalPort, ShellUri = ActivityHostShellUri };

            List<String> typefiles = TypeTable.GetDefaultTypeFiles();
            typefiles.AddRange(ActivitiesTypesFiles.Select(Environment.ExpandEnvironmentVariables));
            ActivitiesTypeTable = new TypeTable(typefiles);

        }

        internal ActivityHostProcess(int activityHostTimeoutSec, PSLanguageMode? languageMode)
        {
            _languageMode = languageMode;
            _useJobIPCProcess = true;
            _tracer.WriteMessage("BEGIN Creating new PowerShell process instance");
            _processInstance = new PowerShellProcessInstance();
            _tracer.WriteMessage("END Creating new PowerShell process instance ");
            _runspace = CreateRunspace();
            _tracer.WriteMessage("New runspace created ", _runspace.InstanceId.ToString());
            _timer = new Timer {AutoReset = false, Interval = TimeOut};
            _timer.Elapsed += TimerElapsed;
            _timer.Interval = activityHostTimeoutSec > 0 ? activityHostTimeoutSec*1000 : TimeOut;
            _perfCountersMgr.UpdateCounterByValue(
                PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                PSWorkflowPerformanceCounterIds.ActivityHostMgrCreatedProcessesCount);
        }

        #endregion Constructors

        #region Private Methods

        /// <summary>
        /// Get the runspace corresponding to this process
        /// </summary>
        /// <param name="createNew">indicates if a new runspace
        /// needs to be created</param>
        /// <returns>runspace object</returns>
        private Runspace GetRunspace(bool createNew)
        {
            if (_runspace.RunspaceStateInfo.State == RunspaceState.BeforeOpen)
            {
                // if the runspace is not opened, open it and return
                OpenRunspace(_runspace);
            }
            else
            {
                if (_useJobIPCProcess)
                {
                    // dispose the existing runspace and create a new one
                    CloseAndDisposeRunspace();

                    _runspace = CreateRunspace();
                    _tracer.WriteMessage("New runspace created ", _runspace.InstanceId.ToString());
                    OpenRunspace(_runspace);
                }
            }
            return _runspace;
        }

        private void CloseAndDisposeRunspace()
        {
            try
            {
                // Close and dispose the existing runspace
                //
                _runspace.Close();
                _runspace.Dispose();
            }
            catch (Exception e)
            {
                // RemoteRunspace.Close can throw exceptions when Server process has exited or runspace is invalid.
                // Ignoring all exceptions as this runspace was used for previous OOP activity execution.
                //
                _tracer.TraceException(e);

                // do nothing
            }
        }

        /// <summary>
        /// Depending on the option return an Out-of-proc
        /// or remoting runspace on localhost
        /// </summary>
        /// <returns>runspace object for use</returns>
        private Runspace CreateRunspace()
        {
            return _useJobIPCProcess ? RunspaceFactory.CreateOutOfProcessRunspace(ActivitiesTypeTable, _processInstance) : RunspaceFactory.CreateRunspace(ActivityHostConnectionInfo, null, ActivitiesTypeTable);
        }

        /// <summary>
        /// Opens the specified runspace. If there are any errors in
        /// opening the runspace, the method just eats them so that
        /// an unhandled exception in the background thread in which this 
        /// method is invoked does not lead to a process crash
        /// </summary>
        /// <param name="runspace">runspace to open</param>
        private void OpenRunspace(Runspace runspace)
        {
            // a runspace open can fail for a variety of reasons
            // eat the exceptions
            try
            {
                _tracer.WriteMessage("Opening runspace " , _runspace.InstanceId.ToString());
                runspace.Open();
                _tracer.WriteMessage("Runspace opened successfully ", _runspace.InstanceId.ToString());

                if (_languageMode != null && _languageMode.HasValue)
                {
                    using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create())
                    {
                        ps.Runspace = runspace;
                        string langScript = "$ExecutionContext.SessionState.LanguageMode = '" + _languageMode.Value.ToString() + "'";
                        ps.AddScript(langScript);
                        ps.Invoke();
                    }
                }
            }
            catch (PSRemotingTransportRedirectException)
            {
                // we should not be getting this exception 
                // in the normal case
                _tracer.WriteMessage("Opening runspace threw  PSRemotingTransportRedirectException", _runspace.InstanceId.ToString());
                Debug.Assert(false, "We should not get a redirect exception under normal circumstances");
            }
            catch (PSRemotingTransportException transportException)
            {
                _tracer.WriteMessage("Opening runspace threw  PSRemotingTransportException", _runspace.InstanceId.ToString());
                _tracer.TraceException(transportException);
                // throwing PSRemotingTransportException exception as it will be handled at single place in PrepareAndRun() method.
                throw;
            }
            catch(PSRemotingDataStructureException)
            {
                // just eat the exception
                _tracer.WriteMessage("Opening runspace threw  PSRemotingDataStructureException", _runspace.InstanceId.ToString());
                Debug.Assert(false, "We should not get a protocol exception under normal circumstances");
            }
        }

        /// <summary>
        /// Import all the specified modules in the specified runspace
        /// </summary>
        /// <param name="runspace">runspace in which to import the modules</param>
        /// <param name="modules">collection of modules to import</param>
        /// <returns>true if setting up of runspace from policy succeeded</returns>
        private void ImportModulesFromPolicy(Runspace runspace, ICollection<string> modules)
        {
            // If any modules are specified, load them into the runspace. 
            // In general, autoloading should be preferred to explicitly loading a module
            // at startup time...
            if (modules.Count <= 0)
                return;

            using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create())
            {
                ps.Runspace = runspace;
                // Setting erroraction to stop for import-module since they are required modules. If not present, stop the execution
                ps.AddCommand("Import-Module").AddArgument(modules).AddParameter("ErrorAction",ActionPreference.Stop).AddParameter("Force");
                ps.Invoke();
            }
        }

        /// <summary>
        /// Set the specified variables in the runspace
        /// </summary>
        /// <param name="runspace">runspace in which the variables need
        /// to be set</param>
        /// <param name="variables">collection of variables that need to be set</param>
        private void SetVariablesFromPolicy(Runspace runspace, IDictionary<string, object> variables)
        {
            using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create())
            {
                ps.Runspace = runspace;                
                ps.AddScript(SetVariableFunction);
                ps.Invoke();

                ps.Commands.Clear();
                ps.AddCommand("_PSSetVariable").AddParameter("Name", variables.Keys).AddParameter("Value", variables.Values);
                ps.Invoke();

                // Remove the temporary function _PSSetVariable after its use is done.
                ps.Commands.Clear();
                ps.AddCommand("Remove-Item").AddParameter("Path", "function:\\_PSSetVariable").AddParameter("Force");
                ps.Invoke();

            }
        }

        /// <summary>
        /// Set the busy flag since the process is being used.
        /// Stop the idle timer
        /// </summary>
        private void SetBusy()
        {
            lock(_syncObject)
            {
                _busy = true;

                // stop the idle timer
                _timer.Stop();
            }
        }

        /// <summary>
        /// Reset the busy flag and start the idle timer
        /// </summary>
        private void ResetBusy()
        {
            lock(_syncObject)
            {
                // Process state should not be set to false if it is already marked for removal
                // Process is marked for removal in two cases: 
                // 1) When PSRemotingTransportException is received in PrepareAndRun, in this case Server process is already crashed/exited. 
                // If busy state is set to false, this process can be assigned to another activity by the servicing thread. 
                // Instead of adding separate boolean variable in PrepareAndRun to call ResetBusy we are checking for not marked for removal here.
                // 2) When idle time of 5 min elapsed, process is marked for removal so that server process can be killed. After this, ResetBusy is never called.
                //
                if (_busy && !_markForRemoval)
                {
                    _busy = false;

                    // enable the idle timer
                    _timer.Enabled = true;

                    this.RaiseProcessFinishedEvent();
                }
            }
        }

        /// <summary>
        /// Idle timeout has occured. If the runspace is not being used
        /// just close it
        /// </summary>
        /// <param name="sender">sender of this event</param>
        /// <param name="e">unused</param>
        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            lock (_syncObject)
            {
                if (_busy) return;

                // Mark this process for removal and set it as busy so that this process will not be assigned to any new activity.
                // Marking for removal will ensure that servicing thread will remove this object from host process collection.
                //
                _busy = true;
                _markForRemoval = true;
            }

            if (OnProcessIdle != null)
                OnProcessIdle(this, new EventArgs());
        }

        /// <summary>
        /// Handles a transport error since it is most likely that
        /// the process crash
        /// </summary>
        /// <param name="transportException">the transport exception
        /// that was raised</param>
        /// <param name="onSetup">true indicates that the crash was
        /// encountered when setting up the process and not when the
        /// command was run</param>
        private void HandleTransportError(PSRemotingTransportException transportException, bool onSetup)
        {
            _tracer.TraceException(transportException);

            if (ProcessCrashed != null)
            {
                ActivityHostCrashedEventArgs eventArgs = new ActivityHostCrashedEventArgs
                                                         {FailureOnSetup = onSetup, 
                                                          Invoker = _currentInvoker};
                ProcessCrashed(this, eventArgs);
            }
        }

        internal void RaiseProcessFinishedEvent()
        {
            if (Finished != null)
                Finished(this, new EventArgs());
        }

        #endregion Private Methods

        #region Internal Methods

        internal static String ActivityHostConfiguration
        {
            get { return ActivityHostShellUri; }
        }
        internal event EventHandler Finished;
        internal event EventHandler OnProcessIdle;

        internal event EventHandler<ActivityHostCrashedEventArgs> ProcessCrashed;

        /// <summary>
        /// Prepare the environment based on the policy and run the powershell
        /// </summary>
        /// <param name="invoker"></param>
        /// <remarks>It is assumed that the caller of this method will spawn appropriate
        /// thread and so it is fine for us to call the callback on the same thread</remarks>
        internal void PrepareAndRun(ActivityInvoker invoker)
        {
            bool setupSucceeded = false;
            try
            {
                // Transport (PSRemotingTransportException) error can happen during setup/prepare phase, so it is getting assigned before setup phase.
                //
                _currentInvoker = invoker;
                Runspace runspace = null;
                // Retry for 10 times if runspace is in Broken state
                //
                // Runspace can be broken, when a remote runspace is getting closed during the next activity execution, 
                // the close ack is getting timedout and that close ack is received by the newly created remote runspace for 
                // the next activity since stdoutput stream of server process is not cleared/discarded, 
                // that is why during new runspace is in broken state while it is being opened.
                //
                for (int i = 1; (i <= 10) && !invoker.IsCancelled; i++)
                {
                    // prepare the runspace with the necessary
                    // modules and variables here
                    runspace = GetRunspace(true);

                    if (runspace.RunspaceStateInfo.State == RunspaceState.Opened)
                        break;

                    System.Threading.Thread.Sleep(i*200);
                }

                if (invoker.IsCancelled)
                {
                    return;
                }

                Debug.Assert((runspace.RunspaceStateInfo.State == RunspaceState.Opened), "Runspace is not in Opened state for running an OOP activity");

                if (invoker.Policy.Variables.Count > 0)
                {
                    // set the variables in the specified runspace
                    _tracer.WriteMessage("BEGIN Setting up variables in runspace ", _runspace.InstanceId.ToString());
                    SetVariablesFromPolicy(runspace, invoker.Policy.Variables);
                    _tracer.WriteMessage("END Setting up variables in runspace ", _runspace.InstanceId.ToString());
                }

                if (invoker.Policy.Modules.Count > 0)
                {
                    // attempt loading the modules in the specified runspace
                    _tracer.WriteMessage("BEGIN Setting up runspace from policy ", _runspace.InstanceId.ToString());
                    ImportModulesFromPolicy(runspace, invoker.Policy.Modules);
                    _tracer.WriteMessage("END Setting up runspace from policy ", _runspace.InstanceId.ToString());
                }

                // Prepare phase is completed without any issues. 
                // setupSucceeded flag is used in HandleTransportError method to enqueue the current activity for retry. 
                // If there is any PSRemotingTransportException during InvokePowershell current activity will not be enqueued to setup failed requests in ActivityHostManager.
                //
                setupSucceeded = true;

                // at this point we assume we have a clean runspace to
                // run the command
                // if the runspace is broken then the invocation will
                // result in an error either ways            
                invoker.InvokePowerShell(runspace);
            }
            catch (PSRemotingTransportException transportException)
            {
                HandleTransportError(transportException, !setupSucceeded);
            }
            catch (Exception e)
            {
                // at this point there is an exception other than a 
                // transport exception that is caused by trying to
                // setup the runspace. Release the asyncresult
                // accordingly
                _tracer.TraceException(e);
                invoker.AsyncResult.SetAsCompleted(e);
            }
            finally
            {
                _currentInvoker = null;
                ResetBusy();
            }
        }

        /// <summary>
        /// If the process represented by this object is busy
        /// </summary>
        internal bool Busy
        {
            get
            {
                lock (_syncObject)
                {
                    return _busy;
                }
            }
            set
            {
                if (value) SetBusy();
                else ResetBusy();
            }
        }

        /// <summary>
        /// Indicates that the associated process crashed and this object
        /// needs to be removed by ActivityHostManager
        /// </summary>
        internal bool MarkForRemoval { 
            get
            {
                // _processInstance.HasExited can not be used to tell as marked for removal 
                // as it will cause inconsistent busy host count
                //
                return _markForRemoval;
            }
            set
            {
                _markForRemoval = value;
            }
        }
        private bool _markForRemoval = false;

        #endregion Internal Methods

        #region IDisposable

        /// <summary>
        /// Dispose 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// dispose of managed resources
        /// </summary>
        /// <param name="disposing">true if being disposed</param>
        protected void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (_isDisposed) return;
            lock (_syncObject)
            {
                if (_isDisposed) return;
                _isDisposed = true;
            }

            Debug.Assert(_runspace != null, "Runspace is already null");

            CloseAndDisposeRunspace();

            _processInstance.Dispose();

            _currentInvoker = null;
            _tracer.Dispose();

            _perfCountersMgr.UpdateCounterByValue(
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterIds.ActivityHostMgrDisposedProcessesCount);
        }

        #endregion IDisposable
    } 
}
