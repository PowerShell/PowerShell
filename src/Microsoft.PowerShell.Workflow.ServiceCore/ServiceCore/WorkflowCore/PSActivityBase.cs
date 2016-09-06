using System;
using System.Collections;
using System.Data;
using System.Linq;
using System.Activities;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation.Remoting;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Activities.Hosting;
using System.Activities.Statements;
using System.ComponentModel;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;
using System.Management.Automation.Tracing;
using Dbg=System.Diagnostics.Debug;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Host;
using System.Management;
using Microsoft.PowerShell.Workflow;

namespace Microsoft.PowerShell.Activities
{
    internal class ActivityParameters
    {
        internal uint? ConnectionRetryCount
        {
            get;
            private set;
        }

        internal uint? ConnectionRetryInterval
        {
            get;
            private set;
        }

        internal uint? ActionRetryCount
        {
            get;
            private set;
        }

        internal uint? ActionRetryInterval
        {
            get;
            private set;
        }

        internal string[] PSRequiredModules
        {
            get;
            private set;
        }

        internal ActivityParameters(uint? connectionRetryCount, uint? connectionRetryInterval, uint? actionRetryCount, uint? actionRetryInterval, string[] requiredModule)
        {
            this.ConnectionRetryCount = connectionRetryCount;
            this.ConnectionRetryInterval = connectionRetryInterval;
            this.ActionRetryCount = actionRetryCount;
            this.ActionRetryInterval = actionRetryInterval;
            this.PSRequiredModules = requiredModule;
        }
    }

    internal delegate void PrepareSessionDelegate(ActivityImplementationContext implementationContext);

    internal class RunCommandsArguments
    {
        internal ActivityParameters ActivityParameters { get; private set; }
        internal PSDataCollection<PSObject> Output { get; private set; }
        internal PSDataCollection<PSObject> Input { get; private set; }
        internal PSActivityContext PSActivityContext { get; private set; }
        internal Dictionary<string, object> ParameterDefaults { get; private set; }
        internal Type ActivityType { get; private set; }
        internal PrepareSessionDelegate Delegate { get; private set; }
        internal object ActivityObject { get; private set; }
        internal PSWorkflowHost WorkflowHost { get; private set; }
        internal ActivityImplementationContext ImplementationContext { get; private set; }
        internal int CommandExecutionType { get; private set; }
        internal System.Management.Automation.PowerShell HelperCommand { get; set; }
        internal PSDataCollection<object> HelperCommandInput { get; set; }
        internal int CleanupTimeout { get; set; }

        internal RunCommandsArguments(ActivityParameters activityParameters, PSDataCollection<PSObject> output, PSDataCollection<PSObject> input, 
                    PSActivityContext psActivityContext, PSWorkflowHost workflowHost, 
                        bool runInProc, Dictionary<string, object> parameterDefaults, Type activityType, 
                            PrepareSessionDelegate prepareSession, object activityObject, ActivityImplementationContext implementationContext)
        {
            ActivityParameters = activityParameters;
            Output = output;
            Input = input;
            PSActivityContext = psActivityContext;
            ParameterDefaults = parameterDefaults;
            ActivityType = activityType;
            Delegate = prepareSession;
            ActivityObject = activityObject;
            WorkflowHost = workflowHost;
            ImplementationContext = implementationContext;

            CommandExecutionType =
                DetermineCommandExecutionType(
                    implementationContext.ConnectionInfo, runInProc,
                    activityType, psActivityContext);
        }

        internal static int DetermineCommandExecutionType(WSManConnectionInfo connectionInfo, bool runInProc, Type activityType, PSActivityContext psActivityContext)
        {
            // check for cleanup activity first
            if (typeof(PSCleanupActivity).IsAssignableFrom(activityType))
            {
                return PSActivity.CleanupActivity;
            }

            if (connectionInfo != null)
            {
                // check if this is an activity with custom remoting 
                // then we need to run it in proc
                if (psActivityContext != null && psActivityContext.RunWithCustomRemoting)
                    return PSActivity.CommandRunInProc;

                return PSActivity.CommandRunRemotely;
            }

            // if connectionInfo is null, then the command needs to be executed
            // on this machine. #1 to #3 from above is possible
            if (!runInProc)
            {
                return PSActivity.CommandRunOutOfProc;
            }

            // at this point the command is being run in-proc

            // Check for WMI activity
            if (typeof(WmiActivity).IsAssignableFrom(activityType) || typeof(GenericCimCmdletActivity).IsAssignableFrom(activityType))
            {
                return PSActivity.RunInProcNoRunspace;
            }

            if (typeof(PSGeneratedCIMActivity).IsAssignableFrom(activityType))
            {
                return PSActivity.CimCommandRunInProc;
            }

            return PSActivity.CommandRunInProc;            
        }
    }

    /// <summary>
    /// Implementing this interface indicates that the activity supports connection retry.
    /// </summary>
    public interface IImplementsConnectionRetry
    {
        /// <summary>
        /// Defines the number of retries that the activity will make to connect to a remote
        /// machine when it encounters an error. The default is to not retry.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        InArgument<uint?> PSConnectionRetryCount
        {
            get;
            set;
        }

        /// <summary>
        /// Defines the delay, in seconds, between connection retry attempts.
        /// The default is one second.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        InArgument<uint?> PSConnectionRetryIntervalSec
        {
            get;
            set;
        }

    }

    /// <summary>
    /// Special variables that can be defined in a workflow to
    /// control the behaviour of PowerShell activities.
    /// </summary>
    public static class WorkflowPreferenceVariables
    {
        /// <summary>
        /// The parent activity ID to be used for all progress records
        /// that are written in the enclosing scope
        /// </summary>
        public const string PSParentActivityId = "PSParentActivityID";

        /// <summary>
        /// Workflow variable that controls when activities are run
        /// in process. If true, all activities in the enclosing scope
        /// will be run in process
        /// </summary>
        public const string PSRunInProcessPreference = "PSRunInProcessPreference";

        /// <summary>
        /// Workflow variable that is used to determine if a PowerShell activity should
        /// persist when it's done. if true, then all PSActivities in that scope
        /// will not persist at the end of their execution.
        /// </summary>
        public const string PSPersistPreference = "PSPersistPreference";
    }

    /// <summary>
    /// Parameter default, contains all the information which needs to be passed to Workflow context.
    /// </summary>
    public sealed class HostParameterDefaults : IDisposable
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public HostParameterDefaults()
        {
            Parameters = new Dictionary<string, object>();
            HostCommandMetadata = new HostSettingCommandMetadata();
            Runtime = null;
            HostPersistenceDelegate = null;
            ActivateDelegate = null;
            AsyncExecutionCollection = null;
            RemoteActivityState = null;
        }

        /// <summary>
        /// All the activity level default parameter values are passed here.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public Dictionary<string, object> Parameters
        {
            get;
            set;
        }

        /// <summary>
        /// Metadata / symbolic information about the currently-running workflow.
        /// </summary>
        public HostSettingCommandMetadata HostCommandMetadata
        {
            get;
            set;
        }

        /// <summary>
        /// Job instance id.
        /// </summary>
        public Guid JobInstanceId
        {
            get;
            set;
        }

        /// <summary>
        /// The workflow runtime.
        /// </summary>
        public PSWorkflowHost Runtime
        {
            get;
            set;
        }

        /// <summary>
        /// The host persistence delegate.
        /// </summary>
        public Func<bool> HostPersistenceDelegate
        {
            get;
            set;
        }

        /// <summary>
        /// The Workflow activation delegate.
        /// </summary>
        public Action<object> ActivateDelegate
        {
            get;
            set;
        }

        /// <summary>
        /// The asynchronous execution collection.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public Dictionary<string, PSActivityContext> AsyncExecutionCollection
        {
            get;
            set;
        }

        /// <summary>
        /// Currently executing remote activities state with runspace id or completion state.
        /// </summary>
        public PSWorkflowRemoteActivityState RemoteActivityState
        {
            get;
            set;
        }

        /// <summary>
        /// Dispose method
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose of managed resources
        /// </summary>
        /// <param name="disposing">true if disposing</param>
        private void Dispose(bool disposing)
        {
            if (!disposing) return;

            Parameters = null;
            ActivateDelegate = null;
            AsyncExecutionCollection = null;
            RemoteActivityState = null;
            HostPersistenceDelegate = null;
            Runtime = null;
        }
    }

    /// <summary>
    /// Runtime metadata that represents the currently-running command.
    /// </summary>
    public class HostSettingCommandMetadata
    {
        /// <summary>
        /// The command name that generated this workflow.
        /// </summary>
        public string CommandName
        {
            get;
            set;
        }

        /// <summary>
        /// The start line of the command name that generated this section of
        /// the workflow.
        /// </summary>
        public int StartLineNumber
        {
            get;
            set;
        }

        /// <summary>
        /// The start column of the command name that generated this section of
        /// the workflow.
        /// </summary>
        public int StartColumnNumber
        {
            get;
            set;
        }

        /// <summary>
        /// The end line of the command name that generated this section of
        /// the workflow.
        /// </summary>
        public int EndLineNumber
        {
            get;
            set;
        }

        /// <summary>
        /// The end column of the command name that generated this section of
        /// the workflow.
        /// </summary>
        public int EndColumnNumber
        {
            get;
            set;
        }
    }

    /// <summary>
    /// The activity context.
    /// </summary>
    [Serializable]
    public class PSActivityContext : IDisposable
    {
        // Holds our list of commands to run, mapped to a RetryCount class of:
        //     Connection Attempts, Action Attempts

        [NonSerialized]
        private readonly PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();

        [NonSerialized]
        internal Dictionary<System.Management.Automation.PowerShell, RetryCount> runningCommands;

        [NonSerialized]
        internal ConcurrentQueue<ActivityImplementationContext> commandQueue;

        /// <summary>
        /// IsCanceled.
        /// </summary>
        public bool IsCanceled { get; set; }
        internal Dictionary<string, object> UserVariables = new Dictionary<string, object>();
        internal List<Exception> exceptions = new List<Exception>();
        internal PSDataCollection<ErrorRecord> errors = new PSDataCollection<ErrorRecord>();
        internal bool Failed;
        internal bool SuspendOnError;

        [NonSerialized]
        internal readonly ConcurrentQueue<IAsyncResult> AsyncResults = new ConcurrentQueue<IAsyncResult>();

        [NonSerialized]
        internal EventHandler<RunspaceStateEventArgs> HandleRunspaceStateChanged;

        [NonSerialized]
        internal PSDataCollection<ProgressRecord> progress;

        [NonSerialized] internal object SyncRoot = new object();
        [NonSerialized] internal bool AllCommandsStarted;
        [NonSerialized] internal int CommandsRunningCount = 0;

        [NonSerialized] internal WaitCallback Callback;
        [NonSerialized] internal object AsyncState;
        [NonSerialized] internal Guid JobInstanceId;

        [NonSerialized]
        internal ActivityParameters ActivityParams;

        [NonSerialized]
        internal PSDataCollection<PSObject> Input;

        [NonSerialized]
        internal PSDataCollection<PSObject> Output;

        [NonSerialized]
        internal PSWorkflowHost WorkflowHost;

        [NonSerialized]
        internal HostParameterDefaults HostExtension;

        [NonSerialized]
        internal bool RunInProc;

        [NonSerialized]
        internal bool MergeErrorToOutput;

        [NonSerialized]
        internal Dictionary<string, object> ParameterDefaults;

        [NonSerialized]
        internal Type ActivityType;

        [NonSerialized]
        internal PrepareSessionDelegate PrepareSession;

        [NonSerialized]
        internal object ActivityObject;

        /// <summary>
        /// The .NET type implementing the cmdlet to call, used for 
        /// direct-call cmdlets.
        /// </summary>
        internal Type TypeImplementingCmdlet { get; set; }

        internal bool RunWithCustomRemoting { get; set; }

        /// <summary>
        /// Cancelling the Async execution.
        /// </summary>
        public void Cancel()
        {
            if (WorkflowHost == null)
            {
                throw new InvalidOperationException("WorkflowHost");
            }

            // First, clear pending items
            if (this.commandQueue != null)
            {
                while (!this.commandQueue.IsEmpty)
                {
                    ActivityImplementationContext implementationContext;
                    bool gotCommand = this.commandQueue.TryDequeue(out implementationContext);
                    if (gotCommand)
                    {
                        System.Management.Automation.PowerShell command = implementationContext.PowerShellInstance;
                        _tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity: Cancelling pending command {0}.", command));

                        command.Dispose();
                    }
                }
            }

            // cancel all pending invocations in activityhostmanager
            PSResumableActivityHostController resumablecontroller = WorkflowHost.PSActivityHostController as PSResumableActivityHostController;
            if (resumablecontroller != null)
            {
                resumablecontroller.StopAllResumablePSCommands(this.JobInstanceId);
            }
            else
            {
                PSOutOfProcessActivityController delegateController = WorkflowHost.PSActivityHostController as PSOutOfProcessActivityController;
                if (delegateController != null)
                {
                    while (this.AsyncResults.Count > 0)
                    {
                        IAsyncResult result;
                        this.AsyncResults.TryDequeue(out result);
                        delegateController.CancelInvokePowerShell(result);
                    }
                }
            }

            while (this.runningCommands.Count > 0)
            {
                System.Management.Automation.PowerShell currentCommand = null;

                lock (this.runningCommands)
                {
                    foreach (System.Management.Automation.PowerShell command in this.runningCommands.Keys)
                    {
                        currentCommand = command;
                        break;
                    }

                    if (currentCommand == null)
                    {
                        return;
                    }
                }

                if (currentCommand.InvocationStateInfo.State == PSInvocationState.Running)
                {
                    _tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity: Stopping command {0}.", currentCommand));

                    try
                    {
                        currentCommand.Stop();
                    }
                    catch (NullReferenceException)
                    {
                        // The PowerShell API has a bug where it sometimes throws NullReferenceException
                        // when being stopped. Ignore in this case.
                    }
                    catch (InvalidOperationException)
                    {
                        // It's possible that the pipeline stopped just before stopping it.
                        // (In-process case)
                    }
                }


                // If anything fails, then set the  overall failure state for the activity...
                if (currentCommand.InvocationStateInfo.State != PSInvocationState.Completed || currentCommand.HadErrors)
                {
                    this.Failed = true;
                }

                int commandType =
                    RunCommandsArguments.DetermineCommandExecutionType(
                        currentCommand.Runspace.ConnectionInfo as WSManConnectionInfo, RunInProc, ActivityType, this);
                if (commandType != PSActivity.RunInProcNoRunspace)
                {
                    PSActivity.CloseRunspaceAndDisposeCommand(currentCommand, WorkflowHost, this, commandType);
                }

                this.runningCommands.Remove(currentCommand);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <remarks>This function is designed to be lightweight and to 
        /// run on the Workflow thread when Execute() is called. When
        /// any changes are made to this function the contract needs to
        /// be honored</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
                    "Microsoft.Reliability",
                    "CA2000:Dispose objects before losing scope",
                    Justification = "Disposed in EndExecute.")]
        public bool Execute()
        {
            // Create the template of a thread that just pulls from the commandQueue
            // and processes the SMA.PowerShell given to it.
            /*
             
                A note about cancellation
                -------------------------
              
                While a worker thread is running, there's
                the possibility of the user cancelling the activity as a whole.
                That means, at any line, there's the potential of us wanting
                to stop what the thread is doing.
                In the worst case of a race condition, the user attempts to cancel
                the activity, but we ignore it.
                The natural way to prevent this kind of race condition is through
                locking so that the 'if(canceled)' condition doesn't change during
                individual lines of the thread's invocation.
                However, adding this lock would mean that the running thread would
                _have_ the lock, which would would prevent the cancellation thread
                from _entering_ the lock, preventing the cancellation thread from
                signalling its condition. This is, in fact, the very worst case of
                the race condition: the user wants to cancel the activity, but we
                ignore it.
                Verification of cancellation, then, is on a "best-effort" basis.
            */

            if (commandQueue == null)
            {
                throw new InvalidOperationException("commandQueue");
            }

            ActivityImplementationContext implementationContext;
            bool result = commandQueue.TryDequeue(out implementationContext);

            lock (SyncRoot)
            {
                while (result)
                {
                    RunCommandsArguments args = new RunCommandsArguments(ActivityParams, Output, Input,
                                                                         this, WorkflowHost, RunInProc,
                                                                         ParameterDefaults, ActivityType, PrepareSession,
                                                                         ActivityObject, implementationContext);
                    // increment count of running commands
                    Interlocked.Increment(ref CommandsRunningCount);
                    PSActivity.BeginRunOneCommand(args);

                    result = commandQueue.TryDequeue(out implementationContext);
                }

                // at this point all commands in the activity context should have been 
                // started
                AllCommandsStarted = true;
            }

            return true;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", Justification = "errors is disposed at the time workflow is removed.")]
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            _tracer.Dispose();

            Callback = null;
            PrepareSession = null;
            HandleRunspaceStateChanged = null;
            ActivityObject = null;
            ParameterDefaults = null;
            Input = null;
            Output = null;
            errors = null;
            progress = null;
            WorkflowHost = null;
            ActivityParams = null;
            exceptions = null;
            runningCommands = null;
            commandQueue = null;

        }
    }

    /// <summary>
    /// Defines the activity on-resume action.
    /// </summary>
    public enum ActivityOnResumeAction
    {
        /// <summary>
        /// Indicates the resumption is normal.
        /// </summary>
        Resume = 0,

        /// <summary>
        /// Indicates that the activity needs to be restarted.
        /// </summary>
        Restart = 1,
    }

    /// <summary>
    /// Activities derived from this class can be used in the Pipeline activity
    /// </summary>
    public abstract class PipelineEnabledActivity : NativeActivity
    {

        /// <summary>
        /// The Input stream for the activity.
        /// </summary>
        [InputAndOutputCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<PSDataCollection<PSObject>> Input
        {
            get;
            set;
        }

        /// <summary>
        /// Determines whether to connect the input stream for this activity.
        /// </summary>
        [InputAndOutputCategory]
        [DefaultValue(false)]
        public bool UseDefaultInput
        {
            get;
            set;
        }

        /// <summary>
        /// The output stream from the activity
        /// </summary>
        [InputAndOutputCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InOutArgument<PSDataCollection<PSObject>> Result
        {
            get;
            set;
        }

        /// <summary>
        /// Determines whether to append output to Result.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public bool? AppendOutput
        {
            get;
            set;
        }
    }

    internal class BookmarkContext
    {
        internal PSWorkflowInstanceExtension BookmarkResumingExtension { get; set; }
        internal Bookmark CurrentBookmark { get; set; }
    }

    /// <summary>
    /// Base class for PowerShell-based workflow activities
    /// </summary>
    public abstract class PSActivity : PipelineEnabledActivity
    {
        /// <summary>
        /// The bookmark prefix for Powershell activities.
        /// </summary>
        public static readonly string PSBookmarkPrefix = "Microsoft_PowerShell_Workflow_Bookmark_";

        /// <summary>
        /// The bookmark prefix for Powershell suspend activities.
        /// </summary>
        public static readonly string PSSuspendBookmarkPrefix = "Microsoft_PowerShell_Workflow_Bookmark_Suspend_";

        /// <summary>
        /// The bookmark prefix for Powershell persist activities.
        /// </summary>
        public static readonly string PSPersistBookmarkPrefix = "Microsoft_PowerShell_Workflow_Bookmark_PSPersist_";

        /// <summary>
        /// Constructor for the PSActivity class.
        /// </summary>
        protected PSActivity()
        {
            cancelTimer = new Delay
            {
                Duration = new InArgument<TimeSpan>((context) =>
                    new TimeSpan(0, 0, (int)context.GetValue(this.PSActionRunningTimeoutSec)))
            };
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public virtual string PSCommandName
        {
            get
            {
                return String.Empty;
            }
        }

        /// <summary>
        /// Returns the module defining the command called by this activity.
        /// It may be null.
        /// </summary>
        protected virtual string PSDefiningModule
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Tracer initialization.
        /// </summary>
        protected PowerShellTraceSource Tracer
        {
            get
            {
                return _tracer;
            }
        }

        /// <summary>
        /// In addition to the display name PSProgress Message will provide
        /// the way to append the additional information into the activity progress message
        /// like branch name or iteration number in case of parallel foreach.
        /// </summary>
        [DefaultValue(null)]
        public InArgument<string> PSProgressMessage
        {
            get;
            set;
        }

        /// <summary>
        /// The Error stream / collection for the activity.
        /// </summary>
        [InputAndOutputCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InOutArgument<PSDataCollection<ErrorRecord>> PSError
        {
            get;
            set;
        }

        /// <summary>
        /// The Progress stream / collection for the activity.
        /// </summary>
        [InputAndOutputCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InOutArgument<PSDataCollection<ProgressRecord>> PSProgress
        {
            get;
            set;
        }

        /// <summary>
        /// The Verbose stream / collection for the activity.
        /// </summary>
        [InputAndOutputCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InOutArgument<PSDataCollection<VerboseRecord>> PSVerbose
        {
            get;
            set;
        }

        /// <summary>
        /// The Debug stream / collection for the activity.
        /// </summary>
        [InputAndOutputCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InOutArgument<PSDataCollection<DebugRecord>> PSDebug
        {
            get;
            set;
        }

        /// <summary>
        /// The Warning stream / collection for the activity.
        /// </summary>
        [InputAndOutputCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InOutArgument<PSDataCollection<WarningRecord>> PSWarning
        {
            get;
            set;
        }

        /// <summary>
        /// The Information stream / collection for the activity.
        /// </summary>
        [InputAndOutputCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InOutArgument<PSDataCollection<InformationRecord>> PSInformation
        {
            get;
            set;
        }

        /// <summary>
        /// Forces the activity to return non-serialized objects. Resulting objects
        /// have functional methods and properties (as opposed to serialized versions
        /// of them), but will not survive persistence when the Workflow crashes or is
        /// persisted.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<bool?> PSDisableSerialization
        {
            get;
            set;
        }

        /// <summary>
        /// Forces the activity to not call the persist functionality, which will be responsible for 
        /// persisting the workflow state onto the disk.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<bool?> PSPersist
        {
            get;
            set;
        }

        /// <summary>
        /// Determines whether to merge error data to the output stream
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<bool?> MergeErrorToOutput
        {
            get;
            set;
        }

        /// <summary>
        /// Defines the maximum amount of time, in seconds, that this activity may run.
        /// The default is unlimited.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<uint?> PSActionRunningTimeoutSec
        {
            get;
            set;
        }

        /// <summary>
        /// This the list of module names (or paths) that are required to run this Activity successfully.
        /// The default is null.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<string[]> PSRequiredModules
        {
            get;
            set;
        }

        /// <summary>
        /// Defines the number of retries that the activity will make when it encounters
        /// an error during execution of its action. The default is to not retry.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<uint?> PSActionRetryCount
        {
            get;
            set;
        }

        /// <summary>
        /// Defines the delay, in seconds, between action retry attempts.
        /// The default is one second.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<uint?> PSActionRetryIntervalSec
        {
            get;
            set;
        }

        /// <summary>
        /// Determines whether to emit verbose output of the activity.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<bool?> Verbose
        {
            get;
            set;
        }

        /// <summary>
        /// Determines whether to emit debug output of the activity.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<bool?> Debug
        {
            get;
            set;
        }

        /// <summary>
        /// Determines how errors should be handled by the activity.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<ActionPreference?> ErrorAction
        {
            get;
            set;
        }

        /// <summary>
        /// Determines how warnings should be handled by the activity.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<ActionPreference?> WarningAction
        {
            get;
            set;
        }

        /// <summary>
        /// Determines how information records should be handled by the activity.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<ActionPreference?> InformationAction
        {
            get;
            set;
        }

        /// <summary>
        /// Provides access to the parameter defaults dictionary
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design","CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        protected Variable<Dictionary<string, object>> ParameterDefaults
        {
            get;
            set;
        }

        private Variable<PSActivityContext> psActivityContextImplementationVariable = new Variable<PSActivityContext>("psActivityContextImplementationVariable");
        private Variable<ActivityInstance> psRunningTimeoutDelayActivityInstanceVar = new Variable<ActivityInstance>("psRunningTimeoutDelayActivityInstanceVar");
        private Delay cancelTimer;                

        // Instance variables. The following are OK to be an instance variables, as they are
        // not modified during runtime. If any variable holds activity-specific state,
        // it must be stored in PSActivityContext.
        private readonly PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();
        private static readonly Tracer _structuredTracer = new Tracer();

        private Variable<NoPersistHandle> noPersistHandle = new Variable<NoPersistHandle>();
        private Variable<bool> bookmarking = new Variable<bool>();
        private TerminateWorkflow terminateActivity = new TerminateWorkflow() { Reason = Resources.RunningTimeExceeded };
        private SuspendOnError suspendActivity = new SuspendOnError();

        /// <summary>
        /// Records a non-terminating error. If the runtime has associated an error stream, this
        /// error will be written to that stream. Otherwise, this will be thrown as an exception.
        /// </summary>
        /// <param name="exception">The exception associated with the error.</param>
        /// <param name="errorId">The error ID associated with the error. This should be a non-localized string.</param>
        /// <param name="errorCategory">The error category associated with the error.</param>
        /// <param name="originalTarget">The object that was being processed while encountering this error.</param>
        /// <param name="psActivityContext">The powershell activity context.</param>
        private static void WriteError(Exception exception, string errorId, ErrorCategory errorCategory, object originalTarget, PSActivityContext psActivityContext)
        {
            if (psActivityContext.errors != null)
            {
                ErrorRecord errorRecord = new ErrorRecord(exception, errorId, errorCategory, originalTarget);
                lock (psActivityContext.errors)
                {
                    psActivityContext.errors.Add(errorRecord);
                }
            }
            else
            {
                lock (psActivityContext.exceptions)
                {
                    psActivityContext.exceptions.Add(exception);
                }
            }
        }

        /// <summary>
        /// In order for an activity to go idle, 'CanInduceIdle' should be true.
        /// </summary>
        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Begins the execution of the activity.
        /// </summary>
        /// <param name="context">The NativeActivityContext provided by the workflow.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Reliability",
            "CA2000:Dispose objects before losing scope",
            Justification = "Disposed in EndExecute.")]
        protected override void Execute(NativeActivityContext context)
        {
            Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity ID={0}: Beginning execution.", context.ActivityInstanceId));

            string displayName = this.DisplayName;

            if (string.IsNullOrEmpty(displayName))
                displayName = this.GetType().Name;

            if (_structuredTracer.IsEnabled)
            {
                _structuredTracer.ActivityExecutionStarted(displayName, this.GetType().FullName);
            }

            // bookmarking will only be enabled when the PSPersist variable is set to 'true' by the author.
            // so we are retrieving the information before host overrides.
            // if the value is false or not provided then we will not go into the unloaded mode.

            bool intBookmarking = InternalBookmarkingRequired(context);

            if (intBookmarking == false)
            {
                NoPersistHandle handle = this.noPersistHandle.Get(context);
                handle.Enter(context);
            }
            this.bookmarking.Set(context, intBookmarking);

            Dictionary<string, object> parameterDefaults = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            HostParameterDefaults hostExtension = null;

            // Retrieve our host overrides
            hostExtension = context.GetExtension<HostParameterDefaults>();
            if (hostExtension != null)
            {
                Dictionary<string, object> incomingArguments = hostExtension.Parameters;
                foreach (KeyValuePair<string, object> parameterDefault in incomingArguments)
                {
                    parameterDefaults[parameterDefault.Key] = parameterDefault.Value;
                }

                if (parameterDefaults.ContainsKey("PSComputerName"))
                {
                    if (parameterDefaults["PSComputerName"] is String)
                    {
                        // If we are given a single string as a parameter default, change it to our required array
                        parameterDefaults["PSComputerName"] = new object[] { (string) parameterDefaults["PSComputerName"] };
                    }
                }

            }

            // If they haven't specified 'UseDefaultInput', ignore that host override
            if( (! this.UseDefaultInput) &&
                parameterDefaults.ContainsKey("Input"))
            {
                parameterDefaults.Remove("Input");
            }

            // Store the values in context
            context.SetValue<Dictionary<string, object>>(this.ParameterDefaults, parameterDefaults);

            // Prepare our state
            PSActivityContext psActivityContextInstance = new PSActivityContext();
            psActivityContextInstance.runningCommands = new Dictionary<System.Management.Automation.PowerShell, RetryCount>();
            psActivityContextInstance.commandQueue = new ConcurrentQueue<ActivityImplementationContext>();
            psActivityContextInstance.IsCanceled = false;
            psActivityContextInstance.HostExtension = hostExtension;

            // If this is a GenericCimCmdletActivity, then copy the type of the cmdlet int0
            // the context so we'll know which type to instantiate at runtime.
            GenericCimCmdletActivity genericCimActivity = this as GenericCimCmdletActivity;
            if (genericCimActivity != null)
            {
                psActivityContextInstance.TypeImplementingCmdlet = genericCimActivity.TypeImplementingCmdlet;
            }

            foreach (PSActivityArgumentInfo currentArgument in GetActivityArguments())
            {
                Argument argumentInstance = currentArgument.Value;
                PopulateParameterFromDefault(argumentInstance, context, currentArgument.Name, parameterDefaults);

                // Log the bound parameter
                if (argumentInstance.Get(context) != null)
                {
                    Tracer.WriteMessage(
                        String.Format(
                            CultureInfo.InvariantCulture, "PowerShell activity ID={0}: Using parameter {1}, with value '{2}'.",
                            context.ActivityInstanceId,
                            currentArgument.Name,
                            argumentInstance.Get(context)));
                }
            }

            // Commands that get hooked up to the input stream when that
            // input stream is empty should not run:
            // PS > function foo { $input | Write-Output "Hello" }
            // PS > foo
            // PS >
            PSDataCollection<PSObject> input = Input.Get(context);
            if ((input != null) && (input.Count == 0))
            {
                Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                    "PowerShell activity ID={0}: Execution skipped due to supplied (but empty) pipeline input.", context.ActivityInstanceId));
                return;
            }

            // Get the output. If it's null (but has been assigned by the user),
            // then populate it.
            bool needToCreateOutput = false;
            PSDataCollection<PSObject> output = Result.Get(context);

            // Set serialization properties on the stream if there is one.
            // If there isn't one, the properties will be set when we
            // create it.
            if (output != null)
            {
                // Set the collection to serialize by default if that option is specified
                if (!GetDisableSerialization(context))
                {
                    output.SerializeInput = true;
                }
                else
                {
                    output.SerializeInput = false;
                }
            }

            if (((output == null) || (! output.IsOpen)) && (this.Result.Expression != null))
            {
                if (output == null)
                {
                    output = CreateOutputStream(context);
                }
                else
                {
                    needToCreateOutput = true;
                }
            }
            else
            {
                // See if it's a host default stream. If so, the expectation is that this stream will
                // remain and hold all results at the end. Otherwise, clear it every invocation.
                if ((ParameterDefaults != null) &&
                    parameterDefaults.ContainsKey("Result") &&
                    (parameterDefaults["Result"] == this.Result.Get(context)))
                {
                    // Do nothing
                }
                else
                {
                    // This is a user-supplied output stream, and should be cleared the same
                    // way that variables are overwritten when assigned multiple times in a
                    // C#  method.
                    if (output != null)
                    {
                        bool appendOutput = false;
                        if ((this.AppendOutput != null) && (this.AppendOutput.Value))
                        {
                            appendOutput = true;
                        }

                        if (!appendOutput)
                        {
                            needToCreateOutput = true;
                        }
                    }
                }
            }

            // Get the error stream. If it's null (but has been assigned by the user),
            // then populate it.
            psActivityContextInstance.errors = PSError.Get(context);
            if (this.PSError.Expression != null)
            {
                if ((psActivityContextInstance.errors == null) || psActivityContextInstance.errors.IsAutoGenerated)
                {
                    psActivityContextInstance.errors = new PSDataCollection<ErrorRecord>();
                    psActivityContextInstance.errors.IsAutoGenerated = true;

                    this.PSError.Set(context, psActivityContextInstance.errors);
                    Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                                                      "PowerShell activity ID={0}: No ErrorStream was passed in; creating a new stream.",
                                                      context.ActivityInstanceId));
                }
            }

            // Merge error stream to the output stream
            if (this.MergeErrorToOutput.Get(context) != null &&
                this.MergeErrorToOutput.Get(context).GetValueOrDefault(false) &&
                output != null && psActivityContextInstance.errors != null)
            {
                // See if it's a host default stream. If so, we need to create our own instance
                // to hold the error, so that the host won't consume the event first
                if (ParameterDefaults != null &&
                    parameterDefaults.ContainsKey("PSError") &&
                    (parameterDefaults["PSError"] == PSError.Get(context)))
                {
                    psActivityContextInstance.errors = new PSDataCollection<ErrorRecord>();
                    psActivityContextInstance.errors.IsAutoGenerated = true;

                    this.PSError.Set(context, psActivityContextInstance.errors);
                    Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                                                      "PowerShell activity ID={0}: Merge error to the output stream and current error stream is the host default; creating a new stream.",
                                                      context.ActivityInstanceId));
                }

                psActivityContextInstance.MergeErrorToOutput = true;
            }

            // Get the progress stream. If it's null (but has been assigned by the user),
            // then populate it.
            PSDataCollection<ProgressRecord> progress = PSProgress.Get(context);
            if (this.PSProgress.Expression != null)
            {
                if ((progress == null) || progress.IsAutoGenerated)
                {
                    progress = new PSDataCollection<ProgressRecord>();
                    progress.IsAutoGenerated = true;

                    this.PSProgress.Set(context, progress);
                    Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity ID={0}: No ProgressStream was passed in; creating a new stream.", context.ActivityInstanceId));
                }
            }

            psActivityContextInstance.progress = progress;

            // Write the Activity Starting progress record...
            WriteProgressRecord(context, progress, Resources.RunningString, ProgressRecordType.Processing);

            // Get the verbose stream. If it's null (but has been assigned by the user),
            // then populate it.
            PSDataCollection<VerboseRecord> verbose = PSVerbose.Get(context);
            if (this.PSVerbose.Expression != null)
            {
                if ((verbose == null) || verbose.IsAutoGenerated)
                {
                    verbose = new PSDataCollection<VerboseRecord>();
                    verbose.IsAutoGenerated = true;

                    this.PSVerbose.Set(context, verbose);
                    Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity ID={0}: No VerboseStream was passed in; creating a new stream.", context.ActivityInstanceId));
                }
            }

            // Get the debug stream. If it's null (but has been assigned by the user),
            // then populate it.
            PSDataCollection<DebugRecord> debug = PSDebug.Get(context);
            if (this.PSDebug.Expression != null)
            {
                if ((debug == null) || debug.IsAutoGenerated)
                {
                    debug = new PSDataCollection<DebugRecord>();
                    debug.IsAutoGenerated = true;

                    this.PSDebug.Set(context, debug);
                    Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity ID={0}: No DebugStream was passed in; creating a new stream.", context.ActivityInstanceId));
                }
            }

            // Get the warning stream. If it's null (but has been assigned by the user),
            // then populate it.
            PSDataCollection<WarningRecord> warning = PSWarning.Get(context);
            if (this.PSWarning.Expression != null)
            {
                if ((warning == null) || warning.IsAutoGenerated)
                {
                    warning = new PSDataCollection<WarningRecord>();
                    warning.IsAutoGenerated = true;

                    this.PSWarning.Set(context, warning);
                    Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity ID={0}: No WarningStream was passed in; creating a new stream.", context.ActivityInstanceId));
                }
            }

            // Get the information stream. If it's null (but has been assigned by the user),
            // then populate it.
            PSDataCollection<InformationRecord> information = PSInformation.Get(context);
            if (this.PSInformation.Expression != null)
            {
                if ((information == null) || information.IsAutoGenerated)
                {
                    information = new PSDataCollection<InformationRecord>();
                    information.IsAutoGenerated = true;

                    this.PSInformation.Set(context, information);
                    Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity ID={0}: No InformationStream was passed in; creating a new stream.", context.ActivityInstanceId));
                }
            }

            // Clear the input stream so that it is consumed, but only if streamed from
            // the host. We don't want to clear user-supplied streams.
            input = Input.Get(context);
            if (input != null)
            {
                bool usedDefaultOutput = false;

                if ((ParameterDefaults != null) &&
                    parameterDefaults.ContainsKey("Input") &&
                    (parameterDefaults["Input"] == this.Input.Get(context)))
                {
                    usedDefaultOutput = true;
                }

                if (usedDefaultOutput)
                {
                    PSDataCollection<PSObject> newInput = new PSDataCollection<PSObject>(input);
                    newInput.IsAutoGenerated = true;

                    this.Input.Set(context, newInput);
                    input.Clear();

                    input = Input.Get(context);
                }
            }

            // Auto reconnect feature is enabled only for the remoting activity with persistence
            bool enableAutoConnectToManagedNode = this is PSRemotingActivity && intBookmarking;
            var hostParamValues = context.GetExtension<HostParameterDefaults>();
            bool remoteActivityResumeOperation = false;
            PSWorkflowRemoteActivityState remoteActivityState = null;
            if (enableAutoConnectToManagedNode && hostParamValues != null && hostParamValues.RemoteActivityState != null)
            {
                remoteActivityResumeOperation = hostParamValues.RemoteActivityState.RemoteActivityResumeRequired(this, false);
                remoteActivityState = hostParamValues.RemoteActivityState;
            }

            // Prepare the command(s)
            List<ActivityImplementationContext> commandsToRun = GetTasks(context);
            int taskId = 1;
            foreach (ActivityImplementationContext commandToRun in commandsToRun)
            {
                if (remoteActivityState != null)
                {
                    if (remoteActivityResumeOperation == true)
                    {
                        // task state is maintained in remoteActivityState as <activity id, task id, state>
                        // state value can be NotStarted, RunspaceId (running), Completed

                        object taskEntry = null;
                        taskEntry = remoteActivityState.GetRemoteActivityRunspaceEntry(this.Id, taskId);

                        if (taskEntry != null)
                        {
                            // re-execution of completed tasks is not required during the resume operation
                            string taskCompletion = taskEntry as string;
                            if ((taskCompletion != null) && (String.Compare(taskCompletion, "completed", StringComparison.OrdinalIgnoreCase) == 0))
                            {
                                taskId++;
                                continue;
                            }

                            if (taskEntry is Guid)
                                commandToRun.DisconnectedRunspaceInstanceId = (Guid)taskEntry;
                        }
                    }
                    else
                    {
                        // After crash/shutdown, this entry is required for checking all activity level fanout tasks are completed
                        remoteActivityState.SetRemoteActivityRunspaceEntry(this.Id, taskId, "notstarted", 
                            commandToRun.ConnectionInfo != null ? commandToRun.ConnectionInfo.ComputerName: null);
                    }
                }


                // If the activity contains a ScriptBlock, then we set the user variables from the workflow.
                bool hasScriptBlock = false;

                foreach (PropertyInfo field in this.GetType().GetProperties())
                {
                    // See if it's an argument
                    if (typeof(Argument).IsAssignableFrom(field.PropertyType))
                    {
                        // Get the argument
                        Argument currentArgument = (Argument)field.GetValue(this, null);

                        if (currentArgument != null &&
                            (currentArgument.ArgumentType.IsAssignableFrom(typeof(ScriptBlock)) ||
                             currentArgument.ArgumentType.IsAssignableFrom(typeof(ScriptBlock[]))))
                        {
                            hasScriptBlock = true;
                            break;
                        }
                    }
                }

                // Populate the user variables for locally running activities or activities with scriptblock
                if (hasScriptBlock || !(this is PSRemotingActivity))
                {
                    PopulateRunspaceFromContext(commandToRun, psActivityContextInstance, context);
                }

                commandToRun.Id = taskId++;
                commandToRun.EnableRemotingActivityAutoResume = enableAutoConnectToManagedNode;

                // If user execute activity with credential but no computername/connectionUri, credential will be bypassed.
                // We throw a warning here.
                if ((commandToRun.PSCredential != null) &&
                !(((commandToRun.PSComputerName != null) && !commandToRun.PSComputerName.All(name => string.IsNullOrEmpty(name))) ||
                ((commandToRun.PSConnectionUri != null) && (commandToRun.PSConnectionUri.Length > 0))))

                {
                    commandToRun.PSWarning.Add(new WarningRecord(Resources.CredentialParameterAssignedWithNoComputerName));
                }
                psActivityContextInstance.commandQueue.Enqueue(commandToRun);
            }

            // Launch our delegate to actually run the given command,
            // possibly across many machines.
            //Func<ActivityParameters, PSDataCollection<PSObject>, PSDataCollection<PSObject>, PSActivityContext, string,
            //    PSWorkflowHost, bool, Dictionary<string, object>, Type, PrepareSessionDelegate, object, bool> RunCommandsDelegate = Execute;

            // Set up the max running time trigger
            uint? maxRunningTime = PSActionRunningTimeoutSec.Get(context);
            Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity ID={0}: Max running time: {1}.", context.ActivityInstanceId, maxRunningTime));

            if (maxRunningTime.HasValue)
            {
                psRunningTimeoutDelayActivityInstanceVar.Set(context, context.ScheduleActivity(cancelTimer, MaxRunTimeElapsed));                
            }

            Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity ID={0}: Invoking command.", context.ActivityInstanceId));

            OnActivityCreated(this, new ActivityCreatedEventArgs(null));

            uint? connectionRetryCount = null;
            uint? connectionRetryInterval = null;

            IImplementsConnectionRetry implementsRetry = this as IImplementsConnectionRetry;
            if (implementsRetry != null)
            {
                connectionRetryCount = implementsRetry.PSConnectionRetryCount.Get(context);
                connectionRetryInterval = implementsRetry.PSConnectionRetryIntervalSec.Get(context);
            }

            List<string> modulesToLoad = new List<string>();
            if (!string.IsNullOrEmpty(PSDefiningModule))
            {
                modulesToLoad.Add(PSDefiningModule);
            }
            string[] requiredModules = PSRequiredModules.Get(context);
            if (requiredModules != null)
            {
                modulesToLoad.AddRange(requiredModules);
            }

            Action<object> activateDelegate = null;
            if ((hostExtension != null) && (hostExtension.ActivateDelegate != null))
            {
                activateDelegate = hostExtension.ActivateDelegate;
            }

            Guid jobInstanceId = Guid.Empty;
            object asyncState = null;
            WaitCallback callback = null;
            Bookmark bookmark = context.CreateBookmark(PSBookmarkPrefix + Guid.NewGuid().ToString(), this.OnResumeBookmark);

            if (activateDelegate != null)
            {
                asyncState = bookmark;
                callback = new WaitCallback(activateDelegate);
                jobInstanceId = hostExtension.JobInstanceId;

                if ((hostExtension != null) && (hostExtension.AsyncExecutionCollection != null))
                {
                    Dictionary<string, PSActivityContext> asyncExecutionCollection = null;
                    asyncExecutionCollection = hostExtension.AsyncExecutionCollection;
                    if (asyncExecutionCollection != null)
                    {
                        if (asyncExecutionCollection.ContainsKey(context.ActivityInstanceId))
                        {
                            asyncExecutionCollection.Remove(context.ActivityInstanceId);
                        }

                        asyncExecutionCollection.Add(context.ActivityInstanceId, psActivityContextInstance);
                    }
                }
            }
            else
            {
                PSWorkflowInstanceExtension extension = context.GetExtension<PSWorkflowInstanceExtension>();
                BookmarkContext resumeContext = new BookmarkContext
                {
                    CurrentBookmark = bookmark,
                    BookmarkResumingExtension = extension
                };

                callback = OnComplete;
                asyncState = resumeContext;
            }

            psActivityContextImplementationVariable.Set(context, psActivityContextInstance);

            psActivityContextInstance.Callback = callback;
            psActivityContextInstance.AsyncState = asyncState;
            psActivityContextInstance.JobInstanceId = jobInstanceId;

            psActivityContextInstance.ActivityParams = new ActivityParameters(connectionRetryCount, connectionRetryInterval,
                                       PSActionRetryCount.Get(context), PSActionRetryIntervalSec.Get(context),
                                       modulesToLoad.ToArray());
            psActivityContextInstance.Input = input;

            if (needToCreateOutput)
            {
                output = CreateOutputStream(context);
            }

            psActivityContextInstance.Output = output;
            psActivityContextInstance.WorkflowHost = GetWorkflowHost(hostExtension);
            psActivityContextInstance.RunInProc = GetRunInProc(context);
            psActivityContextInstance.ParameterDefaults = parameterDefaults;
            psActivityContextInstance.ActivityType = GetType();
            psActivityContextInstance.PrepareSession = PrepareSession;
            psActivityContextInstance.ActivityObject = this;          

            if (IsActivityInlineScript(this) && RunWithCustomRemoting(context))
            {
                psActivityContextInstance.RunWithCustomRemoting = true;
            }

            // One more time writing the parameter defaults into the context
            context.SetValue<Dictionary<string, object>>(this.ParameterDefaults, parameterDefaults);

            // Execution
            psActivityContextInstance.Execute();

            if (_structuredTracer.IsEnabled)
            {
                _structuredTracer.ActivityExecutionFinished(displayName);
            }

        }

        private bool GetDisableSerialization(NativeActivityContext context)
        {
            // First check parameter override
            bool valueByParameter = PSDisableSerialization.Get(context).GetValueOrDefault(false);
            if (valueByParameter) { return valueByParameter; }

            // Next check preference variable
            foreach (System.ComponentModel.PropertyDescriptor property in context.DataContext.GetProperties())
            {
                if (string.Equals(property.DisplayName, "PSDisableSerializationPreference", StringComparison.OrdinalIgnoreCase))
                {
                    object serializationPreference = property.GetValue(context.DataContext);
                    if (serializationPreference != null)
                    {
                        bool variableValue;
                        if (LanguagePrimitives.TryConvertTo<bool>(serializationPreference, CultureInfo.InvariantCulture, out variableValue))
                        {
                            return variableValue;
                        }
                    }
                }
            }

            // Finally, check URI setting.
            // Always say "disable serialization" for the Server Manager endpoint.
            // This should NOT be removed at the same time as any RDS changes and should be
            // evaluated separately.
            if (PSSessionConfigurationData.IsServerManager)
            {
                return true;
            }

            return false;
        }

        private bool InternalBookmarkingRequired(NativeActivityContext context)
        {
            bool? activityPersistFlag = GetActivityPersistFlag(context);
            return activityPersistFlag.HasValue ? (bool)activityPersistFlag : false;
        }

        private void MaxRunTimeElapsed(NativeActivityContext context, ActivityInstance instance)
        {
            if (instance.State != ActivityInstanceState.Canceled)
            {
                string message = String.Format(System.Globalization.CultureInfo.CurrentCulture,
                                                    Resources.RunningTimeExceeded, PSActionRunningTimeoutSec.Get(context));
                throw new TimeoutException(message);
            }
        }

        private PSDataCollection<PSObject> CreateOutputStream(NativeActivityContext context)
        {
            PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();
            output.IsAutoGenerated = true;
            output.EnumeratorNeverBlocks = true;

            // Set the collection to serialize by default if that option is specified
            if (!GetDisableSerialization(context))
            {
                output.SerializeInput = true;
            }
            else
            {
                output.SerializeInput = false;
            }

            this.Result.Set(context, output);
            Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity ID={0}: No OutputStream was passed in; creating a new stream.", context.ActivityInstanceId));

            return output;
        }

        /// <summary>
        /// Write a progress record fo the current activity
        /// </summary>
        /// <param name="context">Workflow engine context</param>
        /// <param name="progress">The progress stream to write to</param>
        /// <param name="statusDescription">The status string to display</param>
        /// <param name="type">the Progress record type</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        protected void WriteProgressRecord(NativeActivityContext context, PSDataCollection<ProgressRecord> progress, string statusDescription, ProgressRecordType type)
        {
            if (progress == null)
            {
                // While it seems like we should throw and exception, since we want to run in a non-M3P
                // environment, we need to silently ignore this....
                return;
            }

            string activityProgMsg = null;

            if ((context != null) && (this.PSProgressMessage != null))
            {
                activityProgMsg = this.PSProgressMessage.Get(context);

                // there is no need to write the progress message since the value of psprogressmessage is explicitly provided with null
                if (this.PSProgressMessage.Expression != null && string.IsNullOrEmpty(activityProgMsg))
                    return;
            }


            string progressActivity;

            if (activityProgMsg == null)
            {
                progressActivity = this.DisplayName;

                if (string.IsNullOrEmpty(progressActivity))
                    progressActivity = this.GetType().Name;
            }
            else
            {
                progressActivity = this.DisplayName + ": " + activityProgMsg;
            }

            ProgressRecord pr = new ProgressRecord(0, progressActivity, statusDescription);
            pr.RecordType = type;

            string currentOperation = this.Id + ":";

            // Add in position information from the host override
            if (context != null)
            {
                HostParameterDefaults hostExtension = context.GetExtension<HostParameterDefaults>();
                if (hostExtension != null)
                {
                    HostSettingCommandMetadata commandMetadata = hostExtension.HostCommandMetadata;
                    if (commandMetadata != null)
                    {
                        currentOperation += String.Format(CultureInfo.CurrentCulture,
                            Resources.ProgressPositionMessage,
                            commandMetadata.CommandName, commandMetadata.StartLineNumber, commandMetadata.StartColumnNumber);
                    }
                }
            }

            pr.CurrentOperation = currentOperation;

            // Look to see if the parent id has been set for this scope...
            if (context != null)
            {
                foreach (System.ComponentModel.PropertyDescriptor property in context.DataContext.GetProperties())
                {
                    if (string.Equals(property.DisplayName, WorkflowPreferenceVariables.PSParentActivityId, StringComparison.OrdinalIgnoreCase))
                    {
                        object parentId = property.GetValue(context.DataContext);
                        if (parentId != null)
                        {
                            int idToUse;
                            if (LanguagePrimitives.TryConvertTo<int>(parentId, CultureInfo.InvariantCulture, out idToUse))
                            {
                                pr.ParentActivityId = idToUse;
                            }
                        }
                    }
                    else if (string.Equals(property.DisplayName, "ProgressPreference", StringComparison.OrdinalIgnoreCase))
                    {
                        string progressPreference = property.GetValue(context.DataContext) as string;
                        if (!string.IsNullOrEmpty(progressPreference) &&
                            (string.Equals(progressPreference, "SilentlyContinue", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(progressPreference, "Ignore", StringComparison.OrdinalIgnoreCase)))
                        {
                            // See if we should skip writing out the progress record...
                            return;
                        }
                    }
                }
            }

            progress.Add(pr);
        }


        private static void OnComplete(object state)
        {
            _structuredTracer.Correlate();

            //BookmarkContext bookmarkContext = (BookmarkContext)result.AsyncState;
            Dbg.Assert(state != null, "State not passed correctly to OnComplete");
            BookmarkContext bookmarkContext = state as BookmarkContext;
            Dbg.Assert(bookmarkContext != null, "BookmarkContext not passed correctly to OnComplete");
            PSWorkflowInstanceExtension extension = bookmarkContext.BookmarkResumingExtension;
            Bookmark bookmark = bookmarkContext.CurrentBookmark;

            // Resuming the bookmark
            ThreadPool.QueueUserWorkItem(o => extension.BeginResumeBookmark(bookmark, null, ar => extension.EndResumeBookmark(ar), null));

        }

        private void OnResumeBookmark(NativeActivityContext context, Bookmark bookmark, object value)
        {
            _structuredTracer.Correlate();

            if (this.bookmarking.Get(context) == false)
            {
                NoPersistHandle handle = this.noPersistHandle.Get(context);
                handle.Exit(context);
            }

            //context.RemoveBookmark(bookmark);

            // Check for the activity if it is restarting
            // this is possible when the activity in bookmarked stated get crashed/terminated
            // in this case we restart this activity
            ActivityOnResumeAction action = ActivityOnResumeAction.Resume;
            if (value != null && value.GetType() == typeof(ActivityOnResumeAction))
                action = (ActivityOnResumeAction) value;

            // All activity level fanout tasks/commands might have finished before the process crash for the remoting activity
            // Check activity restart is not required.
            var hostParamValues = context.GetExtension<HostParameterDefaults>();
            if (this is PSRemotingActivity && hostParamValues != null && hostParamValues.RemoteActivityState != null)
            {
                // Auto reconnect is enabled for remoting activity with PSPersist value true
                if (InternalBookmarkingRequired(context))
                {
                    bool remoteActivityCompleted = !(hostParamValues.RemoteActivityState.RemoteActivityResumeRequired(this, true));

                    if (remoteActivityCompleted == true)
                        action = ActivityOnResumeAction.Resume;
                }
            }

            if (action == ActivityOnResumeAction.Restart)
            {
                this.Execute(context);
                return;
            }

            // this is expected when there would be a disconnected execution
            // here we expects the PSActivityHostArguments to be passed
            // argument contains the result from the execution
            PSResumableActivityContext arguments = null;
            if (value != null && value.GetType() == typeof(PSResumableActivityContext))
            {
                arguments = (PSResumableActivityContext)value;
            }
            if (arguments != null)
            {
                HostParameterDefaults hostValues = context.GetExtension<HostParameterDefaults>();
                if (hostValues != null)
                {
                    if (arguments.SupportDisconnectedStreams && arguments.Streams != null)
                    {
                        PopulateSteamsData(arguments, context, hostValues);
                    }

                    PSDataCollection<ProgressRecord> psprogress = null;
                    if (this.PSProgress.Expression != null)
                    {
                        psprogress = PSProgress.Get(context);
                    }
                    else
                    {
                        if (hostValues.Parameters["PSProgress"] != null && hostValues.Parameters["PSProgress"].GetType() == typeof(PSDataCollection<ProgressRecord>))
                        {
                            psprogress = hostValues.Parameters["PSProgress"] as PSDataCollection<ProgressRecord>;
                        }
                    }

                    // If we got an exception, throw it here.
                    if (arguments.Error != null)
                    {
                        WriteProgressRecord(context, psprogress, Resources.FailedString, ProgressRecordType.Completed);
                        
                        Tracer.WriteMessage("PSActivity", "OnResumeBookmark", context.WorkflowInstanceId, 
                                            @"We are about to rethrow the exception in order to preserve the stack trace writing it into the logs.");
                        Tracer.TraceException(arguments.Error);
                        Dbg.Assert(String.IsNullOrWhiteSpace(arguments.Error.Message) == false, "Exception to be thrown doesnt have proper error message, throwing this will results in Suspended job state instead of Failed state !");
                        throw arguments.Error;
                    }

                    // Perform persistence
                    this.ActivityEndPersistence(context);


                    // Write the activity completed progress record
                    if (arguments.Failed)
                    {
                        WriteProgressRecord(context, psprogress, Resources.FailedString, ProgressRecordType.Completed);
                    }
                    else
                    {
                        WriteProgressRecord(context, psprogress,Resources.CompletedString, ProgressRecordType.Completed);
                    }
                }
                
                return;
            }

            // In the most of the cases where the state is null
            PSActivityContext psActivityContextInstance = null;
            PSDataCollection<ProgressRecord> progress = null;

            try
            {
                if (this.bookmarking.Get(context) == false)
                {
                    progress = PSProgress.Get(context);
                    psActivityContextInstance = psActivityContextImplementationVariable.Get(context);

                    //TODO: add below block in finally for whole function
                    psActivityContextImplementationVariable.Set(context, null);

                    HostParameterDefaults hostValues = context.GetExtension<HostParameterDefaults>();
                    if (hostValues != null)
                    {

                        if ((hostValues != null) && (hostValues.AsyncExecutionCollection != null))
                        {
                            Dictionary<string, PSActivityContext> asyncExecutionCollection = null;
                            asyncExecutionCollection = hostValues.AsyncExecutionCollection;
                            if (asyncExecutionCollection != null)
                            {
                                if (asyncExecutionCollection.ContainsKey(context.ActivityInstanceId))
                                {
                                    asyncExecutionCollection.Remove(context.ActivityInstanceId);
                                }
                            }
                        }
                    }

                
                }
                else
                {
                    HostParameterDefaults hostValues = context.GetExtension<HostParameterDefaults>();
                    if (hostValues != null)
                    {

                        if ((hostValues != null) && (hostValues.AsyncExecutionCollection != null))
                        {
                            Dictionary<string, PSActivityContext> asyncExecutionCollection = null;
                            asyncExecutionCollection = hostValues.AsyncExecutionCollection;
                            if (asyncExecutionCollection != null)
                            {
                                if (asyncExecutionCollection.ContainsKey(context.ActivityInstanceId))
                                {
                                    psActivityContextInstance = asyncExecutionCollection[context.ActivityInstanceId];
                                    asyncExecutionCollection.Remove(context.ActivityInstanceId);
                                }
                            }
                        }
                    }

                    if (psActivityContextInstance != null)
                    {
                        progress = psActivityContextInstance.progress;

                        if (this.Result.Expression != null)
                        {
                            this.Result.Set(context, psActivityContextInstance.Output);
                        }
                    }
                }


                // Kill the MaxRunningTime timer
                var runningCancelTimerActivityInstance = psRunningTimeoutDelayActivityInstanceVar.Get(context);
                if (runningCancelTimerActivityInstance != null)
                {
                    psRunningTimeoutDelayActivityInstanceVar.Set(context, null);
                    context.CancelChild(runningCancelTimerActivityInstance);
                }

                if (psActivityContextInstance != null)
                {
                    // If we got an exception, throw it here.
                    if (psActivityContextInstance.exceptions.Count > 0)
                    {
                        WriteProgressRecord(context, progress, Resources.FailedString, ProgressRecordType.Completed);

                        Tracer.WriteMessage("PSActivity", "OnResumeBookmark", context.WorkflowInstanceId,
                                                @"We are about to rethrow the exception in order to preserve the stack trace writing it into the logs.");
                        Tracer.TraceException(psActivityContextInstance.exceptions[0]);

                        Dbg.Assert(String.IsNullOrWhiteSpace(psActivityContextInstance.exceptions[0].Message) == false, "Exception to be thrown doesnt have proper error message, throwing this will results in Suspended job state instead of Failed state !");
                        throw psActivityContextInstance.exceptions[0];
                    }
                }

                // Perform persistence
                this.ActivityEndPersistence(context);


                // Write the activity completed progress record
                if (psActivityContextInstance != null && psActivityContextInstance.Failed)
                {
                    WriteProgressRecord(context, progress, Resources.FailedString, ProgressRecordType.Completed);
                }
                else
                {
                    WriteProgressRecord(context, progress, Resources.CompletedString, ProgressRecordType.Completed);
                }
            }
            finally
            {
                if (psActivityContextInstance != null)
                {
                    // If we had an error to suspend on, schedule the suspend activity
                    if (psActivityContextInstance.SuspendOnError)
                    {
                        context.ScheduleActivity(this.suspendActivity);
                    }

                    psActivityContextInstance.Dispose();
                    psActivityContextInstance = null;
                }
            }
        }

        /// <summary>
        /// The method is override-able by the derived classes in case they would like to implement different logic at the end of persistence.
        /// The default behavior would be to schedule the 'Persist' activity if the PSPersist flag is true or Host is asking for it.
        /// </summary>
        /// <param name="context">The native activity context of execution engine.</param>
        protected virtual void ActivityEndPersistence(NativeActivityContext context)
        {
            bool? activityPersist = GetActivityPersistFlag(context);

            bool? HostPSPersistVariable = null;
            if (this.PSPersist.Expression == null && this.PSPersist.Get(context).HasValue)
            {
                HostPSPersistVariable = this.PSPersist.Get(context).Value;
            }
            
            bool hostPersist = false;
            hostPersist = this.GetHostPersistFlag(context);

            // Schedule Persist activity if the preference variable was true, the activity explicitly requests it
            // or the host has requested persistence.            
            if (((activityPersist == null) && (HostPSPersistVariable == true)) || hostPersist == true || activityPersist == true)
            {
                string bookmarkname = PSActivity.PSPersistBookmarkPrefix + Guid.NewGuid().ToString().Replace("-", "_");
                context.CreateBookmark(bookmarkname, BookmarkResumed);
            }
        }
        private void BookmarkResumed(NativeActivityContext context, Bookmark bookmark, object value)
        {
        }


        private bool GetHostPersistFlag(NativeActivityContext context)
        {
            Func<bool> hostDelegate = null;

            HostParameterDefaults hostValues = context.GetExtension<HostParameterDefaults>();

            if (hostValues != null)
            {
                if ((hostValues != null) && (hostValues.HostPersistenceDelegate != null))
                {
                    hostDelegate = hostValues.HostPersistenceDelegate;
                }
            }

            if (hostDelegate == null)
            {
                return false;
            }

            bool value = hostDelegate();

            return value;
        }
        private bool? GetActivityPersistFlag(NativeActivityContext context)
        {
            bool? activityPersist = null;
            if (this.PSPersist.Expression != null && this.PSPersist.Get(context).HasValue)
            {
                activityPersist = this.PSPersist.Get(context).Value;
            }            
            else
            {
                // Look to see if there is a PSPersistPreference variable in scope...
                foreach (System.ComponentModel.PropertyDescriptor property in context.DataContext.GetProperties())
                {
                    if (string.Equals(property.DisplayName, WorkflowPreferenceVariables.PSPersistPreference, StringComparison.OrdinalIgnoreCase))
                    {
                        object variableValue = property.GetValue(context.DataContext);
                        if (variableValue != null)
                        {
                            bool persist;
                            if (LanguagePrimitives.TryConvertTo<bool>(variableValue, CultureInfo.InvariantCulture, out persist))
                            {
                                activityPersist = persist;
                            }
                        }
                    }
                }
            }

            // if activity level PSPersist value OR $PSPersistPreference are not found try to get the PSPersist value specified at workflow level
            if (activityPersist == null)
            {
                HostParameterDefaults hostExtension = context.GetExtension<HostParameterDefaults>();
                if (hostExtension != null)
                {
                    Dictionary<string, object> incomingArguments = hostExtension.Parameters;
                    if (incomingArguments.ContainsKey(Constants.Persist))
                    {
                        activityPersist = (bool)incomingArguments[Constants.Persist];
                    }                    
                }
            }
            return activityPersist;
        }

        private void PopulateSteamsData(PSResumableActivityContext arguments, NativeActivityContext context, HostParameterDefaults hostValues)
        {
            // setting the output from arguments
            if (arguments.Streams.OutputStream != null)
            {
                if (this.Result.Expression != null)
                {
                    this.Result.Set(context, arguments.Streams.OutputStream);
                }
                else
                {
                    if (hostValues.Parameters["Result"] != null && hostValues.Parameters["Result"].GetType() == typeof(PSDataCollection<PSObject>))
                    {
                        PSDataCollection<PSObject> output = hostValues.Parameters["Result"] as PSDataCollection<PSObject>;
                        if (output != arguments.Streams.OutputStream && output != null && output.IsOpen)
                        {
                            foreach (PSObject obj in arguments.Streams.OutputStream)
                            {
                                output.Add(obj);
                            }
                        }
                    }
                }
            }

            // setting the input from the arguments
            if (arguments.Streams.InputStream != null)
            {
                if (this.Input.Expression != null)
                {
                    this.Input.Set(context, arguments.Streams.InputStream);
                }
                else
                {
                    if (hostValues.Parameters["Input"] != null && hostValues.Parameters["Input"].GetType() == typeof(PSDataCollection<PSObject>))
                    {
                        hostValues.Parameters["Input"] = arguments.Streams.InputStream;
                    }
                }
            }

            // setting the error from arguments
            if (arguments.Streams.ErrorStream != null)
            {
                if (this.PSError.Expression != null)
                {
                    this.PSError.Set(context, arguments.Streams.ErrorStream);
                }
                else
                {
                    if (hostValues.Parameters["PSError"] != null && hostValues.Parameters["PSError"].GetType() == typeof(PSDataCollection<ErrorRecord>))
                    {
                        PSDataCollection<ErrorRecord> error = hostValues.Parameters["PSError"] as PSDataCollection<ErrorRecord>;
                        if (error != arguments.Streams.ErrorStream && error != null && error.IsOpen)
                        {
                            foreach (ErrorRecord obj in arguments.Streams.ErrorStream)
                            {
                                error.Add(obj);
                            }
                        }
                    }
                }
            }

            // setting the warning from arguments                        
            if (arguments.Streams.WarningStream != null)
            {
                if (this.PSWarning.Expression != null)
                {
                    this.PSWarning.Set(context, arguments.Streams.WarningStream);
                }
                else
                {
                    if (hostValues.Parameters["PSWarning"] != null && hostValues.Parameters["PSWarning"].GetType() == typeof(PSDataCollection<WarningRecord>))
                    {
                        PSDataCollection<WarningRecord> warning = hostValues.Parameters["PSWarning"] as PSDataCollection<WarningRecord>;
                        if (warning != arguments.Streams.WarningStream && warning != null && warning.IsOpen)
                        {
                            foreach (WarningRecord obj in arguments.Streams.WarningStream)
                            {
                                warning.Add(obj);
                            }
                        }
                    }
                }
            }
            // setting the progress from arguments
            if (arguments.Streams.ProgressStream != null)
            {
                if (this.PSProgress.Expression != null)
                {
                    this.PSProgress.Set(context, arguments.Streams.ProgressStream);
                }
                else
                {
                    if (hostValues.Parameters["PSProgress"] != null && hostValues.Parameters["PSProgress"].GetType() == typeof(PSDataCollection<ProgressRecord>))
                    {
                        PSDataCollection<ProgressRecord> tmpProgress = hostValues.Parameters["PSProgress"] as PSDataCollection<ProgressRecord>;
                        if (tmpProgress != arguments.Streams.ProgressStream && tmpProgress != null && tmpProgress.IsOpen)
                        {
                            foreach (ProgressRecord obj in arguments.Streams.ProgressStream)
                            {
                                tmpProgress.Add(obj);
                            }
                        }
                    }
                }
            }

            // setting the verbose from arguments
            if (arguments.Streams.VerboseStream != null)
            {
                if (this.PSVerbose.Expression != null)
                {
                    this.PSVerbose.Set(context, arguments.Streams.VerboseStream);
                }
                else
                {
                    if (hostValues.Parameters["PSVerbose"] != null && hostValues.Parameters["PSVerbose"].GetType() == typeof(PSDataCollection<VerboseRecord>))
                    {
                        PSDataCollection<VerboseRecord> verbose = hostValues.Parameters["PSVerbose"] as PSDataCollection<VerboseRecord>;
                        if (verbose != arguments.Streams.VerboseStream && verbose != null && verbose.IsOpen)
                        {
                            foreach (VerboseRecord obj in arguments.Streams.VerboseStream)
                            {
                                verbose.Add(obj);
                            }
                        }
                    }
                }
            }
            // setting the debug from arguments
            if (arguments.Streams.DebugStream != null)
            {
                if (this.PSDebug.Expression != null)
                {
                    this.PSDebug.Set(context, arguments.Streams.DebugStream);
                }
                else
                {
                    if (hostValues.Parameters["PSDebug"] != null && hostValues.Parameters["PSDebug"].GetType() == typeof(PSDataCollection<DebugRecord>))
                    {
                        PSDataCollection<DebugRecord> debug = hostValues.Parameters["PSDebug"] as PSDataCollection<DebugRecord>;
                        if (debug != arguments.Streams.DebugStream && debug != null && debug.IsOpen)
                        {
                            foreach (DebugRecord obj in arguments.Streams.DebugStream)
                            {
                                debug.Add(obj);
                            }
                        }
                    }
                }
            }

            // setting the information stream from arguments
            if (arguments.Streams.InformationStream != null)
            {
                if (this.PSInformation.Expression != null)
                {
                    this.PSInformation.Set(context, arguments.Streams.InformationStream);
                }
                else
                {
                    if (hostValues.Parameters["PSInformation"] != null && hostValues.Parameters["PSInformation"].GetType() == typeof(PSDataCollection<InformationRecord>))
                    {
                        PSDataCollection<InformationRecord> information = hostValues.Parameters["PSInformation"] as PSDataCollection<InformationRecord>;
                        if (information != arguments.Streams.InformationStream && information != null && information.IsOpen)
                        {
                            foreach (InformationRecord obj in arguments.Streams.InformationStream)
                            {
                                information.Add(obj);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Populates the runspace with user variables from the current context
        /// </summary>
        /// <param name="context"></param>
        /// <param name="activityContext"></param>
        /// <param name="implementationContext"></param>
        private void PopulateRunspaceFromContext(ActivityImplementationContext implementationContext, PSActivityContext activityContext, NativeActivityContext context)
        {
            System.Diagnostics.Debug.Assert(implementationContext != null, "Implementation context cannot be null");

            if (implementationContext.PowerShellInstance != null)
            {
                // Then, set the variables from the workflow
                SetActivityVariables(context, activityContext.UserVariables);
            }           
        }

        /// <summary>
        /// Populates the required variables to set in runspace
        /// </summary>
        /// <param name="context"></param>
        /// <param name="activityVariables"></param>
        internal void SetActivityVariables(NativeActivityContext context, Dictionary<string, object> activityVariables)
        {
            Dictionary<string, object> defaults = this.ParameterDefaults.Get(context);

            string[] streams =
                    {
                        "Result", "PSError", "PSWarning", "PSVerbose", "PSDebug", "PSProgress", "PSInformation"
                    };

            // First, set the variables from the user's variables
            foreach (System.ComponentModel.PropertyDescriptor property in context.DataContext.GetProperties())
            {
                if (String.Equals(property.Name, "ParameterDefaults", StringComparison.OrdinalIgnoreCase))
                    continue;

                Object value = property.GetValue(context.DataContext);
                if (value != null)
                {
                    object tempValue = value;

                    PSDataCollection<PSObject> collectionObject = value as PSDataCollection<PSObject>;

                    if (collectionObject != null && collectionObject.Count == 1)
                    {
                        tempValue = collectionObject[0];
                    }

                    activityVariables[property.Name] = tempValue;
                }
            }

            // Then, set anything we received from parameters
            foreach (PSActivityArgumentInfo currentArgument in GetActivityArguments())
            {
                string @default = currentArgument.Name;
                if (streams.Any(item => string.Equals(item, @default, StringComparison.OrdinalIgnoreCase)))
                    continue;

                object argumentValue = currentArgument.Value.Get(context);
                if (argumentValue != null && !activityVariables.ContainsKey(currentArgument.Name))
                {
                    activityVariables[currentArgument.Name] = argumentValue;
                }
            }

            // Then, set the variables from the host defaults
            if (defaults != null)
            {
                foreach (string hostDefault in defaults.Keys)
                {
                    string @default = hostDefault;
                    if (streams.Any(item => string.Equals(item, @default, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    object propertyValue = defaults[hostDefault];
                    if (propertyValue != null && !activityVariables.ContainsKey(hostDefault))
                    {
                        activityVariables[hostDefault] = propertyValue;
                    }
                }
            }
        }

        /// <summary>
        /// Populates a parameter from the defaults supplied by the workflow host
        /// </summary>
        /// <param name="argument">The argument to modify (i.e.: Input, Result, ComputerName, etc)</param>
        /// <param name="context">The activity context to use</param>
        /// <param name="argumentName">The name of the argument</param>
        /// <param name="parameterDefaults">The parameter defaults</param>
        private void PopulateParameterFromDefault(Argument argument, NativeActivityContext context, string argumentName, Dictionary<string, object> parameterDefaults)
        {
            // See if they haven't specified a value
            if ((argument != null) &&
                (argument.Expression == null) &&
                (argument.Direction != ArgumentDirection.Out))
            {
                // See if we have a parameter defaults collection
                if (ParameterDefaults != null)
                {
                    // See if it has this parameter
                    if (parameterDefaults.ContainsKey(argumentName))
                    {
                        Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity ID={0}: Using default {1} value.", context.ActivityInstanceId, argumentName));
                        Object parameterDefault = parameterDefaults[argumentName];

                        // Map any switch parameters to booleans if required
                        if ((argument.ArgumentType == typeof(Boolean)) ||
                            (argument.ArgumentType == typeof(Nullable<Boolean>)))
                        {
                            if (parameterDefault is SwitchParameter)
                            {
                                parameterDefault = ((SwitchParameter) parameterDefault).ToBool();
                            }
                        }

                        // If the argument type is nullable, but the provided default
                        // is a non-nullable version, create a nullable version of it
                        // to set.
                        if ((argument.ArgumentType.IsGenericType) &&
                            (argument.ArgumentType.GetGenericTypeDefinition() == typeof(Nullable<>)) &&
                            (!(parameterDefault is Nullable)))
                        {
                            parameterDefault = LanguagePrimitives.ConvertTo(parameterDefault,
                                argument.ArgumentType,
                                CultureInfo.InvariantCulture);
                        }

                        if (argument.ArgumentType.IsAssignableFrom(typeof(PSCredential)) &&
                            parameterDefault.GetType().IsAssignableFrom(typeof(PSObject)))
                        {
                            parameterDefault = LanguagePrimitives.ConvertTo(parameterDefault,
                                typeof(PSCredential), CultureInfo.InvariantCulture);
                        }

                        argument.Set(context, parameterDefault);
                    }
                }
            }
        }

        // Populates the activity task variables from the current PSActivity instance
        private void PopulateActivityImplementationContext(ActivityImplementationContext implementationContext, NativeActivityContext context, int index)
        {
            // Go through all of the public PSActivity arguments, and populate their value
            // into the ActivityImplementationContext.
            foreach (PSActivityArgumentInfo field in GetActivityArguments())
            {
                // Get the field of the same name from the 'ActivityImplementationContext' class
                PropertyInfo implementationContextField = implementationContext.GetType().GetProperty(field.Name);
                if (implementationContextField == null)
                {
                    // If you have added a new argument then please added it to 'ActivityImplementationContext' classs.
                    throw new Exception("Could not find corresponding task context field for activity argument: " + field.Name);
                }

                if (string.Equals(field.Name, PSComputerName, StringComparison.OrdinalIgnoreCase) && index != -1)
                {
                    // set only the corresponding entry for computername
                    PopulatePSComputerName(implementationContext, context, field, index);
                }
                else
                {
                    // And set it in the task context
                    implementationContextField.SetValue(implementationContext, field.Value.Get(context), null);
                }
            }
        }

        private const string PSComputerName = "PSComputerName";
        private static void PopulatePSComputerName(ActivityImplementationContext implementationContext, NativeActivityContext context, PSActivityArgumentInfo field, int index)
        {
            Dbg.Assert(string.Equals(field.Name, PSComputerName, StringComparison.OrdinalIgnoreCase), "PSActivityArgumentInfo passed should be for PSComputerName");

            PropertyInfo computerNameField = implementationContext.GetType().GetProperty(field.Name);
            string[] computerNames = (string[]) field.Value.Get(context);

            computerNameField.SetValue(implementationContext, new string[]{computerNames[index]}, null);
        }

        /// <summary>
        /// Overload this method to implement any command-type specific preparations.
        /// If this command needs any workflow-specific information during its PrepareSession call,
        /// it should be stored in ActivityImplementationContext.WorkflowContext.
        /// </summary>
        /// <param name="context">The activity context to use</param>
        protected virtual List<ActivityImplementationContext> GetImplementation(NativeActivityContext context)
        {
            ActivityImplementationContext implementationContext = GetPowerShell(context);
            UpdateImplementationContextForLocalExecution(implementationContext, context);

            return new List<ActivityImplementationContext>() { implementationContext };
        }

        /// <summary>
        /// Updates the ImplementationContext returned from GetPowerShell() to support local execution
        /// against the host's runspace pool.
        /// </summary>
        /// <param name="implementationContext">The implementation context returned by GetPowerShell()</param>
        /// <param name="context">The activity context to use</param>
        protected internal void UpdateImplementationContextForLocalExecution(ActivityImplementationContext implementationContext, ActivityContext context)
        {
        }


        /// <summary>
        /// The method for derived activities to return a configured instance of System.Management.Automation.PowerShell.
        /// The implementor should have added all of the commands and parameters required to launch their command through
        /// the standard AddCommand() and AddParameter() methods. Derived activities should not manage the Runspace property
        /// directly, as the PSActivity class configures the runspace afterward to enable remote connections.
        /// </summary>
        /// <param name="context">The NativeActivityContext for the currently running activity.</param>
        /// <returns>A populated instance of System.Management.Automation.PowerShell</returns>
        /// <remarks>The infrastructure takes responsibility for closing and disposing the PowerShell instance returned.</remarks>
        protected abstract ActivityImplementationContext GetPowerShell(NativeActivityContext context);


        /// <summary>
        /// The method for derived activities to customize the runspace of a System.Management.Automation.PowerShell instance
        /// that the runtime has prepared for them.
        /// If the command needs any workflow-specific information during this PrepareSession call,
        /// it should be stored in ActivityImplementationContext.WorkflowContext during the GetCommand preparation phase.
        /// </summary>
        /// <param name="implementationContext">The ActivityImplementationContext returned by the call to GetCommand.</param>
        protected virtual void PrepareSession(ActivityImplementationContext implementationContext)
        {
        }

        /// <summary>
        /// If an activity needs to load a module before it can execute, override this member
        /// to return the name of that module.
        /// </summary>
        protected string DefiningModule
        {
            get
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Retrieve all of the default arguments from the type and its parents.
        /// </summary>
        /// <returns>All of the default arguments from the type and its parents</returns>
        protected IEnumerable<PSActivityArgumentInfo> GetActivityArguments()
        {
            // Walk up the type hierarchy, looking for types that we should pull in
            // parameter defaults.
            Type currentType = this.GetType();

            while (currentType != null)
            {
                // We don't want to support parameter defaults for arguments on
                // concrete types (as they almost guaranteed to collide with other types),
                // but base classes make sense.
                if (currentType.IsAbstract)
                {
                    // Populate any parameter defaults. We only look at fields that are defined on this
                    // specific type (as opposed to derived types) so that we don't make assumptions about
                    // other activities and their defaults.
                    foreach (PropertyInfo field in currentType.GetProperties())
                    {
                        // See if it's an argument
                        if (typeof(Argument).IsAssignableFrom(field.PropertyType))
                        {
                            // Get the argument
                            Argument currentArgument = (Argument)field.GetValue(this, null);
                            yield return new PSActivityArgumentInfo { Name = field.Name, Value = currentArgument };
                        }
                    }
                }

                // Go to our base type, but stop when we go above PSActivity
                currentType = currentType.BaseType;
                if (!typeof(PSActivity).IsAssignableFrom(currentType))
                    currentType = null;
            }
        }

        // Get the tasks from the derived activity, and save the state we require.
        private List<ActivityImplementationContext> GetTasks(NativeActivityContext context)
        {
            List<ActivityImplementationContext> tasks = GetImplementation(context);

            if (IsActivityInlineScript(this))
            {
                // in case of inlinescript activity supporting custom remoting we need
                // to ensure that PSComputerName is populated only for the specified
                // index                
                if (RunWithCustomRemoting(context))
                {
                    for(int i=0; i<tasks.Count;i++)
                    {
                        PopulateActivityImplementationContext(tasks[i], context, i);
                    }
                    return tasks;
                }
            }

            foreach (ActivityImplementationContext task in tasks)
            {
                PopulateActivityImplementationContext(task, context, -1);
            }

            return tasks;
        }

        /// <summary>
        /// Indicates if preference variables need to be updated
        /// </summary>
        protected virtual bool UpdatePreferenceVariable
        {
            get { return true; }
        }

        // Common configuration of a SMA.PowerShell instance.
        /// <summary>
        /// Add the other streams and enqueue the command to be run
        /// </summary>
        /// <param name="implementationContext">The activity context to use</param>
        /// <param name="psActivityContext">The powershell activity context.</param>
        /// <param name="ActivityType">The activity type.</param>
        /// <param name="PrepareSession">The prepare session delegate.</param>
        /// <param name="activityObject">This object representing activity.</param>
        private static void UpdatePowerShell(ActivityImplementationContext implementationContext, PSActivityContext psActivityContext, Type ActivityType, PrepareSessionDelegate PrepareSession, object activityObject)
        {
            try
            {
                PrepareSession(implementationContext);
                System.Management.Automation.PowerShell invoker = implementationContext.PowerShellInstance;

                // Prepare the streams
                if (implementationContext.PSError != null)
                {
                    invoker.Streams.Error = implementationContext.PSError;
                }

                if (implementationContext.PSProgress != null)
                {
                    invoker.Streams.Progress = implementationContext.PSProgress;
                }

                if (implementationContext.PSVerbose != null)
                {
                    invoker.Streams.Verbose = implementationContext.PSVerbose;
                }

                if (implementationContext.PSDebug != null)
                {
                    invoker.Streams.Debug = implementationContext.PSDebug;
                }

                if (implementationContext.PSWarning != null)
                {
                    invoker.Streams.Warning = implementationContext.PSWarning;
                }

                if (implementationContext.PSInformation != null)
                {
                    invoker.Streams.Information = implementationContext.PSInformation;
                }

                // InlineScript activity needs to handle these in its own way
                PSActivity activityBase = activityObject as PSActivity;
                Dbg.Assert(activityBase != null, "Only activities derived from PSActivityBase are supported");
                if (activityBase.UpdatePreferenceVariable)
                {
                    UpdatePreferenceVariables(implementationContext);
                }

                //OnActivityCreated(activityObject, new ActivityCreatedEventArgs(invoker));
            }
            catch (Exception e)
            {
                // Catch all exceptions and add them to the exception list.
                // This way, they will be reported on EndExecute(), rather than
                // killing the process if an exception happens on the background thread.
                lock (psActivityContext.exceptions)
                {
                    psActivityContext.exceptions.Add(e);
                }
            }
        }

        private static void UpdatePreferenceVariables(ActivityImplementationContext implementationContext)
        {
            // Update the PowerShell ubiquitous parameters
            Command activityCommand = implementationContext.PowerShellInstance.Commands.Commands[0];

            if (implementationContext.Verbose != null)
            {
                activityCommand.Parameters.Add("Verbose", implementationContext.Verbose);
            }

            if (implementationContext.Debug != null)
            {
                activityCommand.Parameters.Add("Debug", implementationContext.Debug);
            }

            if (implementationContext.WhatIf != null)
            {
                activityCommand.Parameters.Add("WhatIf", implementationContext.WhatIf);
            }

            if (implementationContext.ErrorAction != null)
            {
                activityCommand.Parameters.Add("ErrorAction", implementationContext.ErrorAction);
            }

            if (implementationContext.WarningAction != null)
            {
                activityCommand.Parameters.Add("WarningAction", implementationContext.WarningAction);
            }

            if (implementationContext.InformationAction != null)
            {
                activityCommand.Parameters.Add("InformationAction", implementationContext.InformationAction);
            }
        }

        internal const int CommandRunInProc = 0;
        internal const int RunInProcNoRunspace = 1;
        internal const int CommandRunOutOfProc = 2;
        internal const int CommandRunRemotely = 3;
        internal const int CimCommandRunInProc = 4;
        internal const int CleanupActivity = 5;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <remarks>THREADING CONTRACT: This function is designed to be 
        /// lightweight and to run on the Workflow thread when Execute() 
        /// is called. When any changes are made to this function the contract 
        /// needs to be honored</remarks>
        internal static void BeginRunOneCommand(RunCommandsArguments args)
        {
            PSActivityContext psActivityContext = args.PSActivityContext;
            ActivityImplementationContext implementationContext = args.ImplementationContext;

            using (PowerShellTraceSource actionTracer = PowerShellTraceSourceFactory.GetTraceSource())
            {
                System.Management.Automation.PowerShell commandToRun = implementationContext.PowerShellInstance;
                actionTracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                                                        "Beginning action to run command {0}.", commandToRun));

                if (CheckForCancel(psActivityContext)) return;

                // initialize for command execution based on type
                InitializeOneCommand(args);

                // run the command based on type
                if (args.CommandExecutionType != CommandRunRemotely && 
                    args.CommandExecutionType != CommandRunInProc &&
                    args.CommandExecutionType != CimCommandRunInProc)
                {
                    // when the command is run on a remote runspace it is
                    // executed on the callback thread from connection
                    // manager
                    BeginExecuteOneCommand(args);
                }
            }
        }

        /// <summary>
        /// Initialize a single command for execution.
        /// </summary>
        /// <param name="args"></param>
        /// <remarks>THREADING CONTRACT: This function is designed to be 
        /// lightweight and to run on the Workflow thread when Execute() 
        /// is called. When any changes are made to this function the 
        /// contract needs to be honored</remarks>
        private static void InitializeOneCommand(RunCommandsArguments args)
        {
            ActivityParameters activityParameters = args.ActivityParameters;
            PSActivityContext psActivityContext = args.PSActivityContext;
            PSWorkflowHost workflowHost = args.WorkflowHost;
            Dictionary<string, object> parameterDefaults = args.ParameterDefaults;
            Type activityType = args.ActivityType;
            PrepareSessionDelegate prepareSession = args.Delegate;
            object activityObject = args.ActivityObject;
            ActivityImplementationContext implementationContext = args.ImplementationContext;
            int commandExecutionType = args.CommandExecutionType;

            using (PowerShellTraceSource actionTracer = PowerShellTraceSourceFactory.GetTraceSource())
            {
                System.Management.Automation.PowerShell commandToRun = implementationContext.PowerShellInstance;
                actionTracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                                                        "Beginning initialization for command '{0}'.", commandToRun));
                //
                // Common Initialization
                //

                // Store which command we're running. We need to lock this one
                // because Dictionary<T,T> is not thread-safe (while ConcurrentQueue is,
                // by definition.)
                lock (psActivityContext.runningCommands)
                {
                    if (CheckForCancel(psActivityContext)) return;

                    if (!psActivityContext.runningCommands.ContainsKey(commandToRun))
                        psActivityContext.runningCommands[commandToRun] = new RetryCount();
                }

                if (CheckForCancel(psActivityContext)) return;

                // Record the invocation attempt
                psActivityContext.runningCommands[commandToRun].ActionAttempts++;

                // NOTE: previously UpdatePowerShell was called after the runspace
                // for the command was open and available. An assumption is made
                // that this is not required and hence the code is moved before
                // the runspace is available. If there are any issues found flip
                // the order back

                // Let the activity prepare the PowerShell instance.
                if (commandExecutionType != CleanupActivity)
                    UpdatePowerShell(implementationContext, psActivityContext, activityType, prepareSession,
                                     activityObject);

                //
                // Initialization based on type
                //
                switch (commandExecutionType)
                {
                    case CommandRunInProc:
                        {
                            // good to run on current thread
                            InitializeActivityEnvironmentAndAddRequiredModules(implementationContext, activityParameters);
                            workflowHost.LocalRunspaceProvider.BeginGetRunspace(null, 0, 0,
                                                                                LocalRunspaceProviderCallback, args);
                        }
                        break;

                    case RunInProcNoRunspace:
                        {

                            // These commands don't have a runspace
                            // so we don't need to do anything here
                        }
                        break;

                    case CimCommandRunInProc:
                        {
                            workflowHost.LocalRunspaceProvider.BeginGetRunspace(null, 0, 0,
                                                                                LocalRunspaceProviderCallback, args);
                        }
                        break;

                    case CommandRunOutOfProc:
                        {
                            // out of proc commands do not require the runspace in the 
                            // powershell. However since this runspace was created by
                            // the local runspace provider it need to be released and
                            // not disposed We need to release the connection
                            // before calling into activity host manager so as to not
                            // risk closing an out of process runspace if
                            // things happen too quickly

                            InitializeActivityEnvironmentAndAddRequiredModules(implementationContext, activityParameters);

                        }
                        break;
                    case CommandRunRemotely:
                        {
                            // when a command is run remotely, the connection manager
                            // assigns a remote runspace. Dispose the existing one
                            // We should not set the runspace to null as it required in
                            // the callback to retrieve connection info in case there
                            // is a connection failure. We need to close the connection
                            // before calling into connection manager so as to not
                            // risk closing a connection manager assigned runspace if
                            // things happen too quickly
                            DisposeRunspaceInPowerShell(commandToRun, false);

                            InitializeActivityEnvironmentAndAddRequiredModules(implementationContext, activityParameters);

                            // can block - should be run on a different thread
                            WSManConnectionInfo connectionInfo =
                                commandToRun.Runspace.ConnectionInfo as WSManConnectionInfo;
                            workflowHost.RemoteRunspaceProvider.BeginGetRunspace(connectionInfo,
                                                                           activityParameters.ConnectionRetryCount.
                                                                               GetValueOrDefault(0),
                                                                           activityParameters.ConnectionRetryInterval.
                                                                               GetValueOrDefault(0),
                                                                           ConnectionManagerCallback,
                                                                           args);

                        }
                        break;

                        case CleanupActivity:
                        {
                            // no initialization required
                        }
                        break;
                }
            }
        }        

        private static void InitializeActivityEnvironmentAndAddRequiredModules(ActivityImplementationContext implementationContext,
            ActivityParameters activityParameters)
        {
            using (PowerShellTraceSource actionTracer = PowerShellTraceSourceFactory.GetTraceSource())
            {
                if (implementationContext.PSActivityEnvironment == null)
                    implementationContext.PSActivityEnvironment = new PSActivityEnvironment();

                PSActivityEnvironment policy = implementationContext.PSActivityEnvironment;

                foreach (string module in activityParameters.PSRequiredModules ?? new string[0])
                {
                    actionTracer.WriteMessage("Adding dependent module to policy: " + module);
                    policy.Modules.Add(module);
                }
            }
        }

        private static void DisposeRunspaceInPowerShell(System.Management.Automation.PowerShell commandToRun, bool setToNull=true)
        {
            Dbg.Assert(commandToRun.Runspace != null, "Method cannot be called when runspace is null");
            commandToRun.Runspace.Dispose();
            commandToRun.Runspace.Close();
            if (setToNull)
                commandToRun.Runspace = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <remarks>
        /// THREADING CONTRACT:
        /// This function runs on the workflow thread when Execute() 
        /// is called for all cases except the remote runspace case 
        /// where it runs on a WinRM thread or the connection manager
        /// servicing thread
        /// Therefore this function is designed to be lightweight in 
        /// all cases
        /// When any changes are made to this function the contract needs to
        /// be honored</remarks>
        private static void BeginExecuteOneCommand(RunCommandsArguments args)
        {
            PSActivityContext psActivityContext = args.PSActivityContext;
            PSWorkflowHost workflowHost = args.WorkflowHost;
            ActivityImplementationContext implementationContext = args.ImplementationContext;
            PSDataCollection<PSObject> input = args.Input;
            PSDataCollection<PSObject> output = args.Output;
            int commandExecutionType = args.CommandExecutionType;

            using (PowerShellTraceSource actionTracer = PowerShellTraceSourceFactory.GetTraceSource())
            {
                System.Management.Automation.PowerShell commandToRun = implementationContext.PowerShellInstance;
                actionTracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                                                        "BEGIN BeginExecuteOneCommand {0}.", commandToRun));

                switch (commandExecutionType)
                {
                    case RunInProcNoRunspace:
                        {
                            // execute on threadpool thread
                            ThreadPool.QueueUserWorkItem(ExecuteOneRunspaceFreeCommandWorker, args);
                        }
                        break;

                    case CimCommandRunInProc:
                        {
                            // execute on threadpool thread
                            ThreadPool.QueueUserWorkItem(InitializeRunspaceAndExecuteCommandWorker, args);
                        }
                        break;

                    case CommandRunOutOfProc:
                        {
                            Dbg.Assert(implementationContext.PSActivityEnvironment != null,
                                       "Policy should have been initialized correctly by the initialization method");

                            if (CheckForCancel(psActivityContext)) return;

                            PSResumableActivityHostController resumableController = workflowHost.PSActivityHostController as PSResumableActivityHostController;
                            if (resumableController != null)
                            {
                                PowerShellStreams<PSObject, PSObject> streams = new PowerShellStreams<PSObject, PSObject>();

                                if (resumableController.SupportDisconnectedPSStreams)
                                {
                                    streams.InputStream = input;
                                    streams.OutputStream = new PSDataCollection<PSObject>();
                                    streams.ErrorStream = new PSDataCollection<ErrorRecord>();
                                    streams.DebugStream = new PSDataCollection<DebugRecord>();
                                    streams.ProgressStream = new PSDataCollection<ProgressRecord>();
                                    streams.VerboseStream = new PSDataCollection<VerboseRecord>();
                                    streams.WarningStream = new PSDataCollection<WarningRecord>();
                                    streams.InformationStream = new PSDataCollection<InformationRecord>();
                                }
                                else
                                {
                                    streams.InputStream = input;
                                    streams.OutputStream = output; 
                                    streams.DebugStream = commandToRun.Streams.Debug;
                                    streams.ErrorStream = commandToRun.Streams.Error;
                                    streams.ProgressStream = commandToRun.Streams.Progress;
                                    streams.VerboseStream = commandToRun.Streams.Verbose;
                                    streams.WarningStream = commandToRun.Streams.Warning;
                                    streams.InformationStream = commandToRun.Streams.Information;
                                }

                                resumableController.StartResumablePSCommand(args.PSActivityContext.JobInstanceId,
                                                                                        (Bookmark)args.PSActivityContext.AsyncState,
                                                                                        commandToRun,
                                                                                        streams,
                                                                                        implementationContext.PSActivityEnvironment,
                                                                                        (PSActivity)psActivityContext.ActivityObject);
                            }
                            else
                            {
                                PSOutOfProcessActivityController delegateController = workflowHost.PSActivityHostController as PSOutOfProcessActivityController;
                                if (delegateController != null)
                                {
                                    AddHandlersToStreams(commandToRun, args);

                                    IAsyncResult asyncResult =
                                        delegateController.BeginInvokePowerShell(commandToRun, input,
                                                                                               output,
                                                                                               implementationContext.PSActivityEnvironment,
                                                                                               ActivityHostManagerCallback,
                                                                                               args);
                                    psActivityContext.AsyncResults.Enqueue(asyncResult);
                                }
                            }
                        }
                        break;

                    case CommandRunRemotely:
                        {
                            ArgsTableForRunspaces.TryAdd(commandToRun.Runspace.InstanceId, args);
                            InitializeRunspaceAndExecuteCommandWorker(args);
                        }
                        break;
                    case CommandRunInProc:
                        {
                            commandToRun.Runspace.ThreadOptions = PSThreadOptions.UseCurrentThread;
                            ThreadPool.QueueUserWorkItem(InitializeRunspaceAndExecuteCommandWorker, args);
                        }
                        break;
                    case CleanupActivity:
                        {
                            ExecuteCleanupActivity(args);        
                        }
                        break;
                }
                actionTracer.WriteMessage("END BeginExecuteOneCommand");
            }
        }

        /// <summary>
        /// Calls the DoCleanup method of the cleanup activity
        /// </summary>
        /// <param name="args">RunCommandsArguments</param>
        /// <remarks>
        /// THREADING CONTRACT:
        /// This function runs on the workflow thread when Execute() 
        /// is called 
        /// Therefore this function is designed to be lightweight in 
        /// all cases
        /// When any changes are made to this function the contract needs to
        /// be honored</remarks>
        private static void ExecuteCleanupActivity(RunCommandsArguments args)
        {
            PSCleanupActivity cleanupActivity = args.ActivityObject as PSCleanupActivity;
            if (cleanupActivity == null)
            {
                throw new ArgumentNullException("args");
            }
            cleanupActivity.DoCleanup(args, CleanupActivityCallback);
        }

        /// <summary>
        /// Callback when all connections to the specified computer
        /// are closed
        /// </summary>
        /// <param name="state">RunCommandsArguments that the activity
        /// passed int</param>
        /// <remarks>
        /// THREADING CONTRACT:
        /// The callback will happen on a WinRM thread. Hence the 
        /// function needs to be lightweight to release the thread
        /// back to WinRM
        /// </remarks>
        private static void CleanupActivityCallback(object state)
        {
            RunCommandsArguments args = state as RunCommandsArguments;
            Dbg.Assert(args != null, "Clean up activity should pass back RunCommandsArguments");
            DecrementRunningCountAndCheckForEnd(args.PSActivityContext);
        }

        private static void ExecuteOneRunspaceFreeCommandWorker(object state)
        {
            Dbg.Assert(state != null, "State needs to be passed to ExecuteOneWmiCommandWorker");
            RunCommandsArguments args = state as RunCommandsArguments;
            Dbg.Assert(args != null, "RunCommandsArguments not passed correctly to ExecuteOneWmiCommandWorker");

            PSActivityContext psActivityContext = args.PSActivityContext;
            ActivityImplementationContext implementationContext = args.ImplementationContext;
            PSDataCollection<PSObject> input = args.Input;
            PSDataCollection<PSObject> output = args.Output;

            using (PowerShellTraceSource actionTracer = PowerShellTraceSourceFactory.GetTraceSource())
            {
                bool attemptRetry = false;
                try
                {
                    actionTracer.WriteMessage("Running WMI/CIM generic activity on ThreadPool thread");
                    RunDirectExecutionActivity(implementationContext.PowerShellInstance, input, output, psActivityContext, implementationContext);
                }
                catch (Exception e)
                {
                    PowerShellTraceSource tracer = PowerShellTraceSourceFactory.GetTraceSource();
                    tracer.TraceException(e);

                    attemptRetry = HandleRunOneCommandException(args, e);
                    if (attemptRetry)
                        BeginActionRetry(args);
                }
                finally
                {
                    implementationContext.CleanUp();

                    RunOneCommandFinally(args, attemptRetry);
                    actionTracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                                                            "PowerShell activity: Finished running command."));
                    // decrement count of running commands
                    DecrementRunningCountAndCheckForEnd(psActivityContext);
                }
            }

        }

        private static void BeginActionRetry(RunCommandsArguments args)
        {
            Dbg.Assert(args != null, "Arguments not passed correctly to BeginActionRetry");
            PSActivityContext psActivityContext = args.PSActivityContext;

            Interlocked.Increment(ref psActivityContext.CommandsRunningCount);
            BeginRunOneCommand(args);
        }

        private static bool HandleRunOneCommandException(RunCommandsArguments args, Exception e)
        {
            bool attemptRetry = false;

            PSActivityContext psActivityContext = args.PSActivityContext;
            ActivityImplementationContext implementationContext = args.ImplementationContext;
            ActivityParameters activityParameters = args.ActivityParameters;

            using (PowerShellTraceSource actionTracer = PowerShellTraceSourceFactory.GetTraceSource())
            {
                System.Management.Automation.PowerShell commandToRun = implementationContext.PowerShellInstance;
                actionTracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                                                        "Exception handling for command {0}.", commandToRun));
                actionTracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                                                        "Got exception running command: {0}.", e.Message));

#if(DEBUG)
                // Synchronization code to help tests verify that canceling during error handling doesn't
                // cause unhandled exceptions.
                const string timingTest = "Test.Cancellation.During.Error.Handling";
                if (Environment.GetEnvironmentVariable(timingTest) != null)
                {
                    while (!String.Equals(Environment.GetEnvironmentVariable(timingTest),
                                          "TestReady", StringComparison.OrdinalIgnoreCase))
                    {
                        Thread.Sleep(50);
                    }
                    Environment.SetEnvironmentVariable(timingTest, null);
                }
                Thread.Sleep(100);
#endif

                // If we've used more attempts than retries,
                // this is a fatal error. We initialize this to MaxValue in case the activity was canceled,
                // in which case we want no more retries.
                int attempts = Int32.MaxValue;

                // No need add the exceptions in the list if the activity is already canceled.
                if (!psActivityContext.IsCanceled)
                {
                    // if there is a connection failure then Connection manager
                    // callback will handle the same. However if we got a connection
                    // and it broke while we are preparing we need to attempt a retry
                    if (psActivityContext.runningCommands.ContainsKey(commandToRun))
                        attempts = psActivityContext.runningCommands[commandToRun].ActionAttempts;

                    attemptRetry = HandleFailure(attempts, activityParameters.ActionRetryCount,
                                                    activityParameters.ActionRetryInterval,
                                                    implementationContext, "ActivityActionFailed", e,
                                                    psActivityContext);
                }
            }

            return attemptRetry;
        }

        private static void RunOneCommandFinally(RunCommandsArguments args, bool attemptRetry)
        {
            if (attemptRetry) return;

            PSActivityContext psActivityContext = args.PSActivityContext;
            ActivityImplementationContext implementationContext = args.ImplementationContext;
            PSWorkflowHost workflowHost = args.WorkflowHost;
            System.Management.Automation.PowerShell commandToRun = implementationContext.PowerShellInstance;

            // Once we're done, remove this command from the list
            // of running commands.
            lock (psActivityContext.runningCommands)
            {
                psActivityContext.runningCommands.Remove(commandToRun);
            }

            // Discard the runspace - don't need to do this for activities that don't use a runspace
            if (!psActivityContext.IsCanceled && args.CommandExecutionType != RunInProcNoRunspace)
            {
                CloseRunspaceAndDisposeCommand(commandToRun, workflowHost, psActivityContext, args.CommandExecutionType);
            }
        }

        /// <summary>
        /// Callback from connection manager
        /// </summary>
        /// <param name="asyncResult"></param>
        /// <remarks>
        /// THREADING CONTRACT:
        /// The callback happens either in a WinRM thread or in the
        /// connection manager servicing thread. Therefore any 
        /// operations that this thread initiates is supposed to
        /// be very small. Make sure that this contract is maintained
        /// when any changes are made to the function</remarks>
        private static void ConnectionManagerCallback(IAsyncResult asyncResult)
        {
            object asyncState = asyncResult.AsyncState;
            Dbg.Assert(asyncState != null, "AsyncState not returned correctly by connection manager");
            RunCommandsArguments args = asyncState as RunCommandsArguments;
            Dbg.Assert(args != null, "AsyncState casting to RunCommandsArguments failed");

            ActivityImplementationContext implementationContext = args.ImplementationContext;
            System.Management.Automation.PowerShell commandToRun = implementationContext.PowerShellInstance;
            PSWorkflowHost workflowHost = args.WorkflowHost;
            PSActivityContext psActivityContext = args.PSActivityContext;
            WSManConnectionInfo connectionInfo = commandToRun.Runspace.ConnectionInfo as WSManConnectionInfo;
            Dbg.Assert(connectionInfo != null, "ConnectionInfo cannot be null");
            string[] psComputerName = implementationContext.PSComputerName;

            using (PowerShellTraceSource tracer = PowerShellTraceSourceFactory.GetTraceSource())
            {
                tracer.WriteMessage("Executing callback for GetRunspace for computer ",
                                    commandToRun.Runspace.ConnectionInfo.ComputerName);
                Runspace runspace = null;
                try
                {
                    runspace = workflowHost.RemoteRunspaceProvider.EndGetRunspace(asyncResult);
                }
                catch (Exception exception)
                {
                    // there is an error in connecting to the specified computer
                    // handle the same
                    tracer.WriteMessage("Error in connecting to remote computer ",
                                        commandToRun.Runspace.ConnectionInfo.ComputerName);
                    // If this was a multi-machine activity, this is an error, not an exception.)
                    if ((psComputerName != null) && (psComputerName.Length > 1))
                    {
                        WriteError(exception, "ConnectionAttemptFailed",
                                   ErrorCategory.InvalidResult,
                                   psComputerName, psActivityContext);
                    }
                    else
                    {
                        var failureErrorRecord = new ErrorRecord(exception, "ConnectionAttemptFailed", ErrorCategory.OpenError,
                                                             psComputerName);
                        lock (psActivityContext.exceptions)
                        {
                            psActivityContext.exceptions.Add(new RuntimeException(exception.Message, exception,
                                                                                  failureErrorRecord));
                        }
                    }

                    // when runspace connection cannot be obtained we will not retry
                    RunOneCommandFinally(args, false);
                    // when runspace cannot be obtained we cannot execute the command
                    // simply decrement count and return
                    DecrementRunningCountAndCheckForEnd(psActivityContext);
                    return;
                }

                // runspace should be in an opened state when connection manager
                // assigns the same
                tracer.WriteMessage("Runspace successfully obtained with guid ", runspace.InstanceId.ToString());

                // Before the connection was assigned, the activity could have been cancelled, therefore
                // we need to check and release before return.
                if (psActivityContext.IsCanceled)
                {
                    CloseRunspace(runspace, CommandRunRemotely, workflowHost, psActivityContext);
                    if (CheckForCancel(psActivityContext)) return;
                }

                // In case of auto reconnect after crash/shutdown, the disconnected remote runspace is already assigned to the disconnected powershell instance
                // Assign the runspace only if instance ids are different
                if (!commandToRun.Runspace.InstanceId.Equals(runspace.InstanceId))
                    commandToRun.Runspace = runspace;

                // the handler is saved in the context so that the registration
                // can be unregistered later
                psActivityContext.HandleRunspaceStateChanged = HandleRunspaceStateChanged;
                commandToRun.Runspace.StateChanged += psActivityContext.HandleRunspaceStateChanged;

                // Invocation state can be in running or final state in reconnect scenario
                // Command should be invoked only when invocation state is not started,
                // during the normal scenario, invocation state is always NotStarted as runspace is just assigned
                if (commandToRun.InvocationStateInfo.State == PSInvocationState.NotStarted)
                {
                    BeginExecuteOneCommand(args);
                }

                tracer.WriteMessage("Returning from callback for GetRunspace for computer ",
                                    commandToRun.Runspace.ConnectionInfo.ComputerName);
            }
        }

        private static void HandleRunspaceStateChanged(object sender, RunspaceStateEventArgs eventArgs)
        {
            if (!( (eventArgs.RunspaceStateInfo.State == RunspaceState.Opened)  ||
                   (eventArgs.RunspaceStateInfo.State == RunspaceState.Disconnected)))
                return;

            Runspace borrowedRunspace = sender as Runspace;
            RunCommandsArguments args;

            Dbg.Assert(borrowedRunspace != null, "Sender needs to be Runspace object");
            ArgsTableForRunspaces.TryGetValue(borrowedRunspace.InstanceId, out args);

            if (args == null)
                return;

            System.Management.Automation.PowerShell commandToRun = args.ImplementationContext.PowerShellInstance;
            PSWorkflowHost workflowHost = args.PSActivityContext.WorkflowHost;

            if (eventArgs.RunspaceStateInfo.State == RunspaceState.Opened &&
                                commandToRun.InvocationStateInfo.State == PSInvocationState.Disconnected)
            {
                commandToRun.ConnectAsync();
                return;
            }

            if (eventArgs.RunspaceStateInfo.State == RunspaceState.Disconnected)
            {
                // the connection manager can disconnect a runspace on purpose, if that
                // is not the case then it is a case of network disconnect. In this case
                // we force a failure
                if (!workflowHost.RemoteRunspaceProvider.IsDisconnectedByRunspaceProvider(borrowedRunspace))
                {
                    ArgsTableForRunspaces.TryRemove(borrowedRunspace.InstanceId, out args);
                    RuntimeException exception =
                        new RuntimeException(
                            String.Format(CultureInfo.CurrentCulture,
                                          Resources.ActivityFailedDueToRunspaceDisconnect,
                                          borrowedRunspace.ConnectionInfo.ComputerName),
                            eventArgs.RunspaceStateInfo.Reason);
                    RunspaceDisconnectedCallback(args, exception);
                }
            }
        }

        /// <summary>
        /// Sets the $pwd variable in the current runspace
        /// </summary>
        /// <param name="psActivityContext"></param>
        /// <param name="runspace"></param>
        private static void SetCurrentDirectory(PSActivityContext psActivityContext, Runspace runspace)
        {
            if (psActivityContext.ParameterDefaults != null)
            {
                if (psActivityContext.ParameterDefaults.ContainsKey(Constants.PSCurrentDirectory))
                {
                    string path = psActivityContext.ParameterDefaults[Constants.PSCurrentDirectory] as string;

                    if (path != null)
                    {
                        runspace.SessionStateProxy.Path.SetLocation(path);
                    }
                }
            }
        }


        /// <summary>
        /// Callback from local runspace provider
        /// </summary>
        /// <param name="asyncResult">This callback happens in the workflow
        /// thread or a threadpool callback servicing thread.
        /// However there is only one thread for servicing all callbacks and
        /// so all operations have to be small</param>
        private static void LocalRunspaceProviderCallback(IAsyncResult asyncResult)
        {
            object asyncState = asyncResult.AsyncState;
            Dbg.Assert(asyncState != null, "AsyncState not returned correctly by LocalRunspaceProvider");
            RunCommandsArguments args = asyncState as RunCommandsArguments;
            Dbg.Assert(args != null, "AsyncState casting to RunCommandsArguments failed");

            ActivityImplementationContext implementationContext = args.ImplementationContext;
            System.Management.Automation.PowerShell commandToRun = implementationContext.PowerShellInstance;
            PSWorkflowHost workflowHost = args.WorkflowHost;
            PSActivityContext psActivityContext = args.PSActivityContext;

            using (PowerShellTraceSource tracer = PowerShellTraceSourceFactory.GetTraceSource())
            {
                tracer.WriteMessage("Executing callback for LocalRunspaceProvider");
                Runspace runspace = null;
                try
                {
                    runspace = workflowHost.LocalRunspaceProvider.EndGetRunspace(asyncResult);

                    if (runspace.ConnectionInfo == null)
                    {
                        if (psActivityContext.UserVariables.Count != 0)
                        {
                            foreach (KeyValuePair<string, object> entry in psActivityContext.UserVariables)
                            {
                                runspace.SessionStateProxy.SetVariable(entry.Key, entry.Value);
                            }
                        }

                        SetCurrentDirectory(psActivityContext, runspace);
                    }
                }
                catch (Exception exception)
                {

                    lock (psActivityContext.exceptions)
                    {
                        psActivityContext.exceptions.Add(exception);
                    }

                    // when runspace connection cannot be obtained we will not retry
                    RunOneCommandFinally(args, false);
                    // when runspace cannot be obtained we cannot execute the command
                    // simply decrement count and return
                    DecrementRunningCountAndCheckForEnd(psActivityContext);
                    return;
                }

                // runspace should be in an opened state when connection manager
                // assigns the same
                tracer.WriteMessage("Local Runspace successfully obtained with guid ", runspace.InstanceId.ToString());

                // Before the runspace is assigned, the activity could have been cancelled, therefore
                // we need to check and release before return.
                if (psActivityContext.IsCanceled)
                {
                    CloseRunspace(runspace, CommandRunInProc, workflowHost, psActivityContext);
                    if (CheckForCancel(psActivityContext)) return;
                }

                commandToRun.Runspace = runspace;               

                // only after the runspace is set to commandToRun is the
                // activity completely created. Raise the ActivityCreated
                // event now
                OnActivityCreated(args.ActivityObject, new ActivityCreatedEventArgs(commandToRun));

                if (args.CommandExecutionType == CimCommandRunInProc)
                {
                    CimActivityImplementationContext cimImplContext =
                                                    implementationContext as CimActivityImplementationContext;
                    Dbg.Assert(cimImplContext != null,
                               "If CommandExecutionType is CimCommand, then implementationContext must be of type CimActivityImplementationContext");

                    // Configure the runspace by executing the module definition scriptblock...
                    Dbg.Assert(cimImplContext.ModuleScriptBlock != null, "A Generated CIM activity should never have a null module scriptblock");
                    runspace.SessionStateProxy.InvokeCommand.InvokeScript(false, cimImplContext.ModuleScriptBlock, null);

                    // Get the CIM session if needed...
                    if (cimImplContext.Session == null && !string.IsNullOrEmpty(cimImplContext.ComputerName) &&
                        !string.Equals(cimImplContext.ComputerName, "localhost",
                                       StringComparison.OrdinalIgnoreCase))
                    {
                        bool useSsl = false;
                        if (cimImplContext.PSUseSsl.HasValue)
                        {
                            useSsl = cimImplContext.PSUseSsl.Value;
                        }   

                        uint port = 0;
                        if (cimImplContext.PSPort.HasValue)
                        {
                            port = cimImplContext.PSPort.Value;
                        }


                        AuthenticationMechanism authenticationMechanism = AuthenticationMechanism.Default;
                        if (cimImplContext.PSAuthentication.HasValue)
                        {
                            authenticationMechanism = cimImplContext.PSAuthentication.Value;
                        }

                        cimImplContext.Session =
                            CimConnectionManager.GetGlobalCimConnectionManager().GetSession(cimImplContext.ComputerName, 
                                                                                            cimImplContext.PSCredential, 
                                                                                            cimImplContext.PSCertificateThumbprint,
                                                                                            authenticationMechanism,
                                                                                            cimImplContext.SessionOptions, 
                                                                                            useSsl, 
                                                                                            port, 
                                                                                            cimImplContext.PSSessionOption);
                        
                        if (cimImplContext.Session == null)
                        {
                            throw new InvalidOperationException();
                        }
                        commandToRun.AddParameter("CimSession", cimImplContext.Session);
                    }
                }

                BeginExecuteOneCommand(args);

                tracer.WriteMessage("Returning from callback for GetRunspace for LocalRunspaceProvider");
            }            
        }

        private static void ActivityHostManagerCallback(IAsyncResult asyncResult)
        {
            object asyncState = asyncResult.AsyncState;
            Dbg.Assert(asyncState != null, "AsyncState not returned correctly by activity host manager");
            RunCommandsArguments args = asyncState as RunCommandsArguments;
            Dbg.Assert(args != null, "AsyncState casting to RunCommandsArguments failed");

            PSWorkflowHost workflowHost = args.WorkflowHost;
            PSActivityContext psActivityContext = args.PSActivityContext;

            using (PowerShellTraceSource tracer = PowerShellTraceSourceFactory.GetTraceSource())
            {
                tracer.WriteMessage("Executing callback for Executing command out of proc");
                bool attemptRetry = false;
                try
                {
                    ((PSOutOfProcessActivityController)workflowHost.PSActivityHostController).EndInvokePowerShell(asyncResult);
                }
                catch (Exception e)
                {
                    attemptRetry = HandleRunOneCommandException(args, e);
                    if (attemptRetry)
                        BeginActionRetry(args);
                }
                finally
                {
                    RemoveHandlersFromStreams(args.ImplementationContext.PowerShellInstance, args);
                    RunOneCommandFinally(args, attemptRetry);
                    tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                                                            "PowerShell activity: Finished running command."));
                    DecrementRunningCountAndCheckForEnd(psActivityContext);
                }
            }
        }

        private static void DecrementRunningCountAndCheckForEnd(PSActivityContext psActivityContext)
        {
            Interlocked.Decrement(ref psActivityContext.CommandsRunningCount);

            if (psActivityContext.CommandsRunningCount != 0) return;

            RaiseTerminalCallback(psActivityContext);
        }

        private static void RaiseTerminalCallback(PSActivityContext psActivityContext)
        {
            lock (psActivityContext.SyncRoot)
            {
                if (psActivityContext.AllCommandsStarted || psActivityContext.commandQueue.Count == 0)
                {
                    // This signals all commands are done
                    // time to start finish routines
                    Dbg.Assert(psActivityContext.Callback != null, "A callback should have been assigned in the context");
                    ThreadPool.QueueUserWorkItem(psActivityContext.Callback, psActivityContext.AsyncState);
                }
            }            
        }

        private static bool CheckForCancel(PSActivityContext psActivityContext)
        {
            bool canceled = psActivityContext.IsCanceled;

            if (canceled)
                RaiseTerminalCallback(psActivityContext);

            return canceled;
        }

        private static void RunDirectExecutionActivity(System.Management.Automation.PowerShell commandToRun, PSDataCollection<PSObject> input,
            PSDataCollection<PSObject> output, PSActivityContext psActivityContext, ActivityImplementationContext implementationContext)
        {
            Command command = commandToRun.Commands.Commands[0];
            var cmdName = command.CommandText;

            Cmdlet cmdlet = null;
            bool takesInput = false;
            if (string.Equals(cmdName, "Get-WMIObject", StringComparison.OrdinalIgnoreCase))
            {
                cmdlet = new Microsoft.PowerShell.Commands.GetWmiObjectCommand();
            }
            else if (string.Equals(cmdName, "Invoke-WMIMethod", StringComparison.OrdinalIgnoreCase))
            {
                cmdlet = new Microsoft.PowerShell.Commands.InvokeWmiMethod();
                takesInput = true;
            }

            if (CheckForCancel(psActivityContext)) return;

            // Set up the command runtime instance...
            DirectExecutionActivitiesCommandRuntime cmdRuntime = new DirectExecutionActivitiesCommandRuntime(output != null ? output : new PSDataCollection<PSObject>(),
                implementationContext, cmdlet != null ? cmdlet.GetType() : psActivityContext.TypeImplementingCmdlet);
            cmdRuntime.Error = commandToRun.Streams.Error;
            cmdRuntime.Warning = commandToRun.Streams.Warning;
            cmdRuntime.Progress = commandToRun.Streams.Progress;
            cmdRuntime.Verbose = commandToRun.Streams.Verbose;
            cmdRuntime.Debug = commandToRun.Streams.Debug;
            cmdRuntime.Information = commandToRun.Streams.Information;

            // If the cmdlet takes input and there is or may be some input, then
            // iterate processing the input

            if (cmdlet != null)
            {
                cmdlet.CommandRuntime = cmdRuntime;

                // Copy the parameters from the PowerShell object to the cmdlet object
                PSObject wrappedCmdlet = PSObject.AsPSObject(cmdlet);
                InitializeCmdletInstanceParameters(command, wrappedCmdlet, false, psActivityContext, null, implementationContext);

                if (takesInput && input != null && (input.Count > 0 || input.IsOpen))
                {
                    Microsoft.PowerShell.Commands.InvokeWmiMethod iwm = cmdlet as Microsoft.PowerShell.Commands.InvokeWmiMethod;
                    foreach (PSObject inputObject in input)
                    {
                        try
                        {
                            var managementObject = LanguagePrimitives.ConvertTo<System.Management.ManagementObject>(inputObject);

                            iwm.InputObject = managementObject;
                            iwm.Invoke().GetEnumerator().MoveNext();
                        }
                        catch (PSInvalidCastException psice)
                        {
                            if (psice.ErrorRecord != null)
                            {
                                cmdRuntime.Error.Add(psice.ErrorRecord);
                            }
                        }

                        if (CheckForCancel(psActivityContext)) return;
                    }
                }
                else
                {
                    cmdlet.Invoke().GetEnumerator().MoveNext();
                }
            }
            else
            {
                // See if any session options were passed..
                CimActivityImplementationContext cimActivityImplementationContext = implementationContext as CimActivityImplementationContext;
                CimSessionOptions cimSessionOptionsToUse = cimActivityImplementationContext != null ? cimActivityImplementationContext.SessionOptions : null;

                if (psActivityContext.TypeImplementingCmdlet == null)
                {
                    throw new InvalidOperationException(cmdName);
                }


                if (input != null && (input.Count > 0 || input.IsOpen))
                {
                    // Only deals with InputObject property for input
                    if (psActivityContext.TypeImplementingCmdlet.GetProperty("InputObject") == null)
                    {
                        // Throw if the cmdlet does not implement a InputObject property to bind to....
                        throw new NotImplementedException(String.Format(CultureInfo.CurrentCulture,
                                          Resources.CmdletDoesNotImplementInputObjectProperty,
                                          cmdName));
                    }

                    foreach (PSObject inputObject in input)
                    {
                        try
                        {
                            using (var cimCmdlet = (Microsoft.Management.Infrastructure.CimCmdlets.CimBaseCommand)Activator.CreateInstance(psActivityContext.TypeImplementingCmdlet))
                            {
                                cimCmdlet.CommandRuntime = cmdRuntime;
                                var cimInstance = LanguagePrimitives.ConvertTo<CimInstance>(inputObject);
                                PSObject wrapper = PSObject.AsPSObject(cimCmdlet);
                                InitializeCmdletInstanceParameters(command, wrapper, true, psActivityContext, cimSessionOptionsToUse, implementationContext);
                                var prop = wrapper.Properties["InputObject"];
                                prop.Value = cimInstance;
                                cimCmdlet.Invoke().GetEnumerator().MoveNext();
                            }
                        }
                        catch (PSInvalidCastException psice)
                        {
                            if (psice.ErrorRecord != null)
                            {
                                cmdRuntime.Error.Add(psice.ErrorRecord);
                            }
                        }

                        if (CheckForCancel(psActivityContext)) return;
                    }
                }
                else
                {
                    using (var cimCmdlet = (Microsoft.Management.Infrastructure.CimCmdlets.CimBaseCommand)Activator.CreateInstance(psActivityContext.TypeImplementingCmdlet))
                    {
                        cimCmdlet.CommandRuntime = cmdRuntime;
                        PSObject wrapper = PSObject.AsPSObject(cimCmdlet);
                        InitializeCmdletInstanceParameters(command, wrapper, true, psActivityContext, cimSessionOptionsToUse, implementationContext);
                        cimCmdlet.Invoke().GetEnumerator().MoveNext();
                    }
                }
            }
        }

        private static void InitializeCmdletInstanceParameters(Command command, PSObject wrappedCmdlet, bool isGenericCim, 
                                PSActivityContext psActivityContext, CimSessionOptions cimSessionOptions, ActivityImplementationContext implementationContext)
        {

            bool sessionSet = false;

            foreach (CommandParameter p in command.Parameters)
            {
                // Note - skipping null common parameters avoids a null pointer exception.
                // Also need to replicate parameter set validation for the WMI activities at this point
                // since we aren't going through the parameter binder
                if (Cmdlet.CommonParameters.Contains(p.Name))
                {
                    continue;
                }

                // If a session was explicitly passed, use it instead of an ambient session for the current computer.
                if (p.Name.Equals("CimSession"))
                {
                    sessionSet = true;
                }

                if (wrappedCmdlet.Properties[p.Name] != null)
                {
                    wrappedCmdlet.Properties[p.Name].Value = p.Value;
                }
                else
                {
                    wrappedCmdlet.Properties.Add(new PSNoteProperty(p.Name, p.Value));
                }
            }

            string[] computerNameArray = null;
            CimActivityImplementationContext cimActivityImplementationContext = 
                implementationContext as CimActivityImplementationContext;

            // Set the target computer for this command...
            if (cimActivityImplementationContext != null && !String.IsNullOrEmpty(cimActivityImplementationContext.ComputerName))
            {
                computerNameArray = new string[] { cimActivityImplementationContext.ComputerName };
            }
            else if(psActivityContext.ParameterDefaults.ContainsKey("PSComputerName"))
            {
                computerNameArray = psActivityContext.ParameterDefaults["PSComputerName"] as string[];
            }

            if (computerNameArray != null && computerNameArray.Length > 0)
            {
                // Not all of the generic CIM cmdlets take a session (e.g. New-CimSession)
                if (isGenericCim && wrappedCmdlet.Properties["CimSession"] != null)
                {
                    if (!sessionSet)
                    {                            		                   
                        if (cimActivityImplementationContext == null)
			                    throw new ArgumentException(Resources.InvalidImplementationContext);

                        bool useSsl = false;
                        if (cimActivityImplementationContext.PSUseSsl.HasValue)
                        {
                            useSsl = cimActivityImplementationContext.PSUseSsl.Value;
                        }

                        uint port = 0;
                        if (cimActivityImplementationContext.PSPort.HasValue)
                        {
                            port = cimActivityImplementationContext.PSPort.Value;
                        }

                        AuthenticationMechanism authenticationMechanism = AuthenticationMechanism.Default;                            
                        if (cimActivityImplementationContext.PSAuthentication.HasValue)
                        {
                            authenticationMechanism = cimActivityImplementationContext.PSAuthentication.Value;
                        }

                        // Convert the computer names to session objects.
                        List<CimSession> cimSessions = computerNameArray
                            .ToList()
                                .ConvertAll<CimSession>(
                                    (string computer) => CimConnectionManager.GetGlobalCimConnectionManager().GetSession(computer, 
                                                                                                                            cimActivityImplementationContext.PSCredential, 
                                                                                                                            cimActivityImplementationContext.PSCertificateThumbprint, 
                                                                                                                            authenticationMechanism, 
                                                                                                                            cimSessionOptions, 
                                                                                                                            useSsl, 
                                                                                                                            port, 
                                                                                                                            cimActivityImplementationContext.PSSessionOption));
                        wrappedCmdlet.Properties["CimSession"].Value = cimSessions.ToArray<CimSession>();

                        if (computerNameArray.Length > 1)
                        {
                            Dbg.Assert(false, "Something in this fix is not right, have a look");
                        }
                        cimActivityImplementationContext.Session = cimSessions[0];
                    }
                }
                else if (wrappedCmdlet.Properties["ComputerName"] == null)
                {
                    // If the cmdlet takes ComputerName and it wasn't explicitly set already, 
                    // then set it to the ambient PSComputerName we got from the context...
                    wrappedCmdlet.Properties.Add(new PSNoteProperty("ComputerName", computerNameArray));
                }
            }
        }

        // Script to initialize the runspace variables...
        private const string RunspaceInitScript = @"
            Get-Variable -Exclude input | Remove-Variable -ErrorAction Ignore; $input | Foreach-Object {$nvp=$_}; foreach($k in $nvp.keys){set-variable -name $k -value $nvp[$k]}
        ";
        internal static readonly InitialSessionState Iss = InitialSessionState.CreateDefault();

        private static void SetVariablesInRunspaceUsingProxy(PSActivityEnvironment activityEnvironment, Runspace runspace)
        {
            using (PowerShellTraceSource actionTracer = PowerShellTraceSourceFactory.GetTraceSource())
            {
                actionTracer.WriteMessage("BEGIN SetVariablesInRunspaceUsingProxy");
                Dictionary<string, object> nameValuePairs = GetVariablesToSetInRunspace(activityEnvironment);
                // Copy in the workflow variables
                foreach (string name in nameValuePairs.Keys)
                {
                    object value = nameValuePairs[name];
                    if (value != null)
                    {
                        try
                        {
                            var psvar = runspace.SessionStateProxy.PSVariable.Get(name);
                            if (psvar == null || (psvar.Options & ScopedItemOptions.ReadOnly) == 0)
                            {
                                // don't try to overwrite read-only variables values
                                runspace.SessionStateProxy.PSVariable.Set(name, value);
                            }
                        }
                        catch (PSNotSupportedException)
                        {
                            actionTracer.WriteMessage("SetVariablesInRunspaceUsingProxy: Copying the workflow variables to a RemoteSessionStateProxy is not supported.");
                            return;
                        }
                    }
                }
                actionTracer.WriteMessage("END SetVariablesInRunspaceUsingProxy");
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <remarks>
        /// THREADING CONTRACT:
        /// This function runs in either a WinRM thread or in the
        /// connection manager servicing thread. Therefore any 
        /// operations that this thread initiates is supposed to
        /// be very small. Make sure that this contract is maintained
        /// when any changes are made to the function
        /// </remarks>
        private static void BeginSetVariablesInRemoteRunspace(RunCommandsArguments args)
        {
            Runspace runspace = args.ImplementationContext.PowerShellInstance.Runspace;
            PSActivityEnvironment activityEnvironment = args.ImplementationContext.PSActivityEnvironment;

            Dbg.Assert(runspace.ConnectionInfo != null,
                       "BeginSetVariablesInRemoteRunspace can only be called for remote runspaces");

            using (PowerShellTraceSource actionTracer = PowerShellTraceSourceFactory.GetTraceSource())
            {
                actionTracer.WriteMessage("BEGIN BeginSetVariablesInRemoteRunspace");

                System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create();              
                ps.Runspace = runspace;
                ps.AddScript(RunspaceInitScript);

                Dictionary<string, object> nameValuePairs = GetVariablesToSetInRunspace(activityEnvironment);

                PSDataCollection<object> vars = new PSDataCollection<object> {nameValuePairs};
                vars.Complete();
                args.HelperCommand = ps;
                args.HelperCommandInput = vars;

                BeginInvokeOnPowershellCommand(ps, vars, null, SetVariablesCallback, args);

                actionTracer.WriteMessage("END BeginSetVariablesInRemoteRunspace");
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <remarks>
        /// THREADING CONTRACT:
        /// This function either runs on a thread pool thread, or in the 
        /// remote case runs on a WinRM/CM servicing thread. Therefore operations
        /// need to be light so that the thread can be released back quickly
        /// When changes are made this assumption need to be validated</remarks>
        private static void BeginRunspaceInitializeSetup(RunCommandsArguments args)
        {
            PSActivityEnvironment activityEnvironment= args.ImplementationContext.PSActivityEnvironment;
            Runspace runspace = args.ImplementationContext.PowerShellInstance.Runspace;
            string[] requiredModules = args.ActivityParameters.PSRequiredModules;
            PSActivityContext psActivityContext = args.PSActivityContext;

            using (PowerShellTraceSource actionTracer = PowerShellTraceSourceFactory.GetTraceSource())
            {
                actionTracer.WriteMessage("BEGIN BeginRunspaceInitializeSetup");
                if (args.CommandExecutionType != CimCommandRunInProc)
                {
                    if (runspace.ConnectionInfo != null)
                    {
                        // for a remote runspace do an async invocation
                        BeginSetVariablesInRemoteRunspace(args);
                        return;
                    }

                    // for a local runspace set the variables using proxy
                    // on the same thread
                    try
                    {
                        SetVariablesInRunspaceUsingProxy(activityEnvironment, runspace);
                    }
                    catch (Exception e)
                    {
                        bool attemptRetry = HandleRunOneCommandException(args, e);

                        if (attemptRetry)
                        {
                            actionTracer.WriteMessage("Setting variables for command failed, attempting retry");
                            // before attempting a retry we will have to ditch this runspace
                            // and use a new one
                            CloseRunspace(args.ImplementationContext.PowerShellInstance.Runspace,
                                          args.CommandExecutionType,
                                          args.WorkflowHost, psActivityContext);
                            BeginActionRetry(args);
                        }
                        else
                        {
                            // if we are not attempting a retry we just need to
                            // return
                            actionTracer.WriteMessage("Setting variables for command failed, returning");
                            RunOneCommandFinally(args, false);
                        }
                        DecrementRunningCountAndCheckForEnd(psActivityContext);
                        return;
                    }
                }

                if (requiredModules.Length > 0)
                {
                    BeginImportRequiredModules(args);
                }
                else
                {
                    // else we are good to call command invocation this point
                    // since this function is guaranteed to run on a threadpool
                    // thread BeginPowerShellInvocation can be directly called here
                    BeginPowerShellInvocation(args);
                }

                actionTracer.WriteMessage("END BeginRunspaceInitializeSetup");
            }
        }

        private static void BeginImportRequiredModules(RunCommandsArguments args)
        {
            Runspace runspace = args.ImplementationContext.PowerShellInstance.Runspace;
            PSActivityEnvironment activityEnvironment = args.ImplementationContext.PSActivityEnvironment;
            System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create();
            if (activityEnvironment != null)
            {
                Dbg.Assert(activityEnvironment.Modules.Count > 0,
                           "When PSActivityEnvironment is specified and modules are imported, PSActivityEnvironment.Modules need to be populated");
                // Setting erroraction to stop for import-module since they are required modules. If not present, stop the execution
                ps.AddCommand("Import-Module").AddParameter("Name", activityEnvironment.Modules).AddParameter(
                    "ErrorAction", ActionPreference.Stop);
            }
            else
            {
                // Setting erroraction to stop for import-module since they are required modules. If not present, stop the execution
                ps.AddCommand("Import-Module").AddParameter("Name", args.ActivityParameters.PSRequiredModules).AddParameter(
                    "ErrorAction", ActionPreference.Stop);
            }

            using (PowerShellTraceSource tracer = PowerShellTraceSourceFactory.GetTraceSource())
            {
                // we need to import required modules only if any are specified
                tracer.WriteMessage("Importing modules in runspace ", runspace.InstanceId);

                ps.Runspace = runspace;
                args.HelperCommand = ps;

                BeginInvokeOnPowershellCommand(ps, null, null, ImportRequiredModulesCallback, args);
            }
        }

        private static void BeginInvokeOnPowershellCommand(System.Management.Automation.PowerShell ps, PSDataCollection<object> varsInput, PSInvocationSettings settings, AsyncCallback callback, RunCommandsArguments args)
        {
            using (PowerShellTraceSource tracer = PowerShellTraceSourceFactory.GetTraceSource())
            {
                try
                {
                    ps.BeginInvoke(varsInput, settings, callback, args);
                }
                catch (Exception e)
                {
                    bool attemptRetry = ProcessException(args, e);

                    if (attemptRetry)
                    {
                        BeginActionRetry(args);
                    }
                    else
                    {
                        ReleaseResourcesAndCheckForEnd(ps, args, false, false);
                    }

                    tracer.TraceException(e);
                }
            }
        }

        private static void ReleaseResourcesAndCheckForEnd(System.Management.Automation.PowerShell ps, RunCommandsArguments args, bool removeHandlersFromStreams, bool attemptRetry)
        {
            using (PowerShellTraceSource tracer = PowerShellTraceSourceFactory.GetTraceSource())
            {
                PSActivityContext psActivityContext = args.PSActivityContext;

                if (removeHandlersFromStreams)
                {
                    RemoveHandlersFromStreams(ps, args);
                }

                RunOneCommandFinally(args, attemptRetry);

                tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                                                        "PowerShell activity: Finished running command."));

                var remotingActivity = args.ActivityObject as PSRemotingActivity;
                if ((remotingActivity != null) && 
                    (args.PSActivityContext.HostExtension != null) &&
                    (args.PSActivityContext.HostExtension.RemoteActivityState != null) &&
                    (args.ImplementationContext != null))
                {
                    // remote activity task execution has finished 
                    // change the task's state to Completed in RemoteActivityState
                    args.PSActivityContext.HostExtension.RemoteActivityState.SetRemoteActivityRunspaceEntry(
                        remotingActivity.Id, 
                        args.ImplementationContext.Id, 
                        "completed", null);
                }

                DecrementRunningCountAndCheckForEnd(psActivityContext);
            }
        }

        private static bool ProcessException(RunCommandsArguments args, Exception e)
        {
            bool attemptRetry = false;
            PSActivityContext psActivityContext = args.PSActivityContext;
            ActivityImplementationContext implementationContext = args.ImplementationContext;
            System.Management.Automation.PowerShell commandToRun = implementationContext.PowerShellInstance;

            if ((args.ActivityParameters.ConnectionRetryCount.HasValue ||
                args.ActivityParameters.ConnectionRetryInterval.HasValue) && e.InnerException != null &&
                e.InnerException is IContainsErrorRecord)
            {
                IContainsErrorRecord er = e as IContainsErrorRecord;

                // CIM specific case
                if (er.ErrorRecord.FullyQualifiedErrorId.StartsWith("CimJob_BrokenCimSession", StringComparison.OrdinalIgnoreCase))
                {
                    int attempts = Int32.MaxValue;

                    if (!psActivityContext.IsCanceled)
                    {
                        if (psActivityContext.runningCommands.ContainsKey(commandToRun))
                            attempts = psActivityContext.runningCommands[commandToRun].ActionAttempts;

                        attemptRetry = HandleFailure(attempts, args.ActivityParameters.ConnectionRetryCount,
                            args.ActivityParameters.ConnectionRetryInterval, implementationContext, "ActivityActionFailed",
                            null, psActivityContext);
                    }

                    if(!attemptRetry)
                    {
                        ErrorRecord errorRecord = er.ErrorRecord;
                        String computerName;
                        Guid jobInstanceId;
                        if (GetComputerNameAndJobIdForCommand(commandToRun.InstanceId, out computerName, out jobInstanceId))
                        {
                            AddIdentifierInfoToErrorRecord(errorRecord, computerName, jobInstanceId);
                        }

                        // If this was a multi-machine activity, this is an error, not an exception.
                        if ((implementationContext.PSComputerName != null) && (implementationContext.PSComputerName.Length > 1))
                        {
                            WriteError(e, "ActivityActionFailed", ErrorCategory.InvalidResult,
                                implementationContext.PowerShellInstance.Runspace.ConnectionInfo, psActivityContext);
                        }
                        else
                        {
                            lock (psActivityContext.exceptions)
                            {
                                psActivityContext.exceptions.Add(e);
                            }
                        }
                    }

                    return attemptRetry;
                }
            }

            IContainsErrorRecord containsErrorRecord = e as IContainsErrorRecord;
            if (containsErrorRecord != null)
            {
                ErrorRecord errorRecord = containsErrorRecord.ErrorRecord;
                String computerName;
                Guid jobInstanceId;
                if (GetComputerNameAndJobIdForCommand(commandToRun.InstanceId, out computerName, out jobInstanceId))
                {
                    AddIdentifierInfoToErrorRecord(errorRecord, computerName, jobInstanceId);
                }
            }
            attemptRetry = HandleRunOneCommandException(args, e);

            return attemptRetry;
        }

        private static Dictionary<string, object> GetVariablesToSetInRunspace(PSActivityEnvironment activityEnvironment)
        {
            // first set the predefined variables to reset the runspace before
            // setting the activity specified ones
            Dictionary<string, object> nameValuePairs = Iss.Variables.ToDictionary(entry => entry.Name, entry => entry.Value);

            if (activityEnvironment != null && activityEnvironment.Variables != null)
            {
                foreach (string name in activityEnvironment.Variables.Keys)
                {
                    object value = activityEnvironment.Variables[name];
                    if (value == null) continue;
                    if (nameValuePairs.ContainsKey(name))
                    {
                        nameValuePairs[name] = value;
                    }
                    else
                    {
                        nameValuePairs.Add(name, value);
                    }
                }
            }

            // workaround: PSPR does not support rehydration of System.Text.ASCIIEncoding 
            // therefore we need to remove this otherwise FS RI is blocked - per their dev mgr
            // once the DCR is implemented on Engines side, we should remove
            if (nameValuePairs.ContainsKey("OutputEncoding"))
            {
                nameValuePairs.Remove("OutputEncoding");
            }

            return nameValuePairs;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="asyncResult"></param>
        /// THREADING CONTRACT:
        /// This callback runs in a WinRM thread - in the normal 
        /// course of operation i.e unless PowerShell.EndInvoke() throws
        /// an exception, the operations performed on this thread
        /// should be minimal
        private static void SetVariablesCallback(IAsyncResult asyncResult)
        {
            object asyncState = asyncResult.AsyncState;
            Dbg.Assert(asyncState != null, "AsyncState was not set correctly by SetVariablesInActivityHost() method");
            RunCommandsArguments args = asyncState as RunCommandsArguments;
            Dbg.Assert(args != null, "AsyncState casting to RunCommandsArguments failed");
            System.Management.Automation.PowerShell powerShell = args.HelperCommand;
            Dbg.Assert(powerShell != null, "Caller did not pass PowerShell instance correctly");

            PSActivityContext psActivityContext = args.PSActivityContext;
            PSActivityEnvironment activityEnvironment = args.ImplementationContext.PSActivityEnvironment;

            using (PowerShellTraceSource tracer = PowerShellTraceSourceFactory.GetTraceSource())
            {
                tracer.WriteMessage("Executing callback for setting variables in remote runspace");
                try
                {
                    powerShell.EndInvoke(asyncResult);
                }
                catch (Exception)
                {
                    // Exception setting variables, try using proxy
                    tracer.WriteMessage("Setting variables in remote runspace failed using script, trying with proxy");

                    try
                    {                        
                        SetVariablesInRunspaceUsingProxy(activityEnvironment, powerShell.Runspace);
                    }
                    catch (Exception ex)
                    {
                        bool attemptRetry = HandleRunOneCommandException(args, ex);
                        if (attemptRetry)
                        {
                            tracer.WriteMessage("Runspace initialization failed, attempting retry");
                            // Since the error is in runspace initialization, we need to discard this
                            // runspace and try with a new one
                            CloseRunspace(args.ImplementationContext.PowerShellInstance.Runspace,
                                          args.CommandExecutionType,
                                          args.WorkflowHost, psActivityContext);
                            BeginActionRetry(args);
                        }
                        else
                        {
                            RunOneCommandFinally(args, false);
                        }
                        DecrementRunningCountAndCheckForEnd(psActivityContext);
                        return;
                    }
                }
                finally
                {
                    powerShell.Dispose();
                    args.HelperCommand = null;
                    args.HelperCommandInput.Dispose();
                    args.HelperCommandInput = null;
                }

                if (CheckForCancel(psActivityContext)) return;

                if ((activityEnvironment != null && activityEnvironment.Modules != null && activityEnvironment.Modules.Count > 0) ||
                    (args.ActivityParameters != null && args.ActivityParameters.PSRequiredModules != null && args.ActivityParameters.PSRequiredModules.Length > 0))
                {
                    // if there are modules to import, begin async invocation
                    // for importing modules
                    BeginImportRequiredModules(args);
                }
                else
                {
                    // else we are good to call command invocation this point
                    // since this function is guaranteed to run on a threadpool
                    // thread BeginPowerShellInvocation can be directly called here
                    BeginPowerShellInvocation(args);
                }
            }
        }

        private static void ImportRequiredModulesCallback(IAsyncResult asyncResult)
        {
            object asyncState = asyncResult.AsyncState;
            Dbg.Assert(asyncState != null, "AsyncState not returned correctly by activity host manager");
            RunCommandsArguments args = asyncState as RunCommandsArguments;
            Dbg.Assert(args != null, "AsyncState casting to RunCommandsArguments failed");
            System.Management.Automation.PowerShell powerShell = args.HelperCommand;
            Dbg.Assert(powerShell != null, "Caller did not pass PowerShell instance correctly");

            PSActivityContext psActivityContext = args.PSActivityContext;
            ActivityParameters activityParameters = args.ActivityParameters;
            Type activityType = args.ActivityType;

            using (PowerShellTraceSource tracer = PowerShellTraceSourceFactory.GetTraceSource())
            {
                tracer.WriteMessage("Executing callback for importing required modules");
                try
                {
                    powerShell.EndInvoke(asyncResult);
                }
                catch (Exception e)
                {
                    string psRequiredModuleNames = "";
                    foreach (string moduleName in activityParameters.PSRequiredModules)
                    {
                        psRequiredModuleNames += moduleName + ", ";
                    }
                    psRequiredModuleNames = psRequiredModuleNames.TrimEnd(',',' ');

                    string msg = string.Format(CultureInfo.InvariantCulture,
                                                                       Resources.DependModuleImportFailed, 
                                                                       psRequiredModuleNames,
                                                                       activityType.Name);
                    Exception ex = new Exception(msg, e);
                    tracer.TraceException(ex);

                    bool attemptRetry = HandleRunOneCommandException(args, ex);
                    if (attemptRetry)
                    {
                        tracer.WriteMessage("Runspace initialization failed, attempting retry");
                        // Since the error is in runspace initialization, we need to discard this
                        // runspace and try with a new one
                        CloseRunspace(args.ImplementationContext.PowerShellInstance.Runspace, args.CommandExecutionType,
                                      args.WorkflowHost, psActivityContext);
                        BeginActionRetry(args);
                    }
                    else
                    {
                        RunOneCommandFinally(args, false);
                    }
                    DecrementRunningCountAndCheckForEnd(psActivityContext);
                    return;
                }
                finally
                {
                    powerShell.Dispose();
                    args.HelperCommand = null;
                }

                if (CheckForCancel(psActivityContext)) return;

                // at this point the initialization is done and we need to execute
                // the command
                BeginPowerShellInvocation(args);                
            }                        
        }

        private static void InitializeRunspaceAndExecuteCommandWorker(object state)
        {
            Dbg.Assert(state != null, "State not passed correctly to worker");
            RunCommandsArguments args = state as RunCommandsArguments;
            Dbg.Assert(args != null, "RunCommandsArguments not passed correctly");

            System.Management.Automation.PowerShell commandToRun = args.ImplementationContext.PowerShellInstance;
            PSActivityContext psActivityContext = args.PSActivityContext;
         
            // Invoke the command. If we want to return the output, then call the overload
            // that streams output as well.
            try
            {
                if (CheckForCancel(psActivityContext)) return;

                // we need to set the variables in the runspace before
                // invoking the command
                BeginRunspaceInitializeSetup(args);

            }
            catch (PipelineStoppedException)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <remarks>
        /// THREADING CONTRACT:
        /// This function is designed to be lightweight. It always runs
        /// on a callback thread - which is a threadpool thread</remarks>
        private static void BeginPowerShellInvocation(object state)
        {
            Dbg.Assert(state != null, "State not passed correctly to BeginPowerShellInvocation");
            RunCommandsArguments args = state as RunCommandsArguments;
            Dbg.Assert(args != null, "Args not passed correctly to BeginPowerShellInvocation");

            ActivityImplementationContext implementationContext = args.ImplementationContext;
            PSDataCollection<PSObject> output = args.Output;
            PSDataCollection<PSObject> input = args.Input;
            System.Management.Automation.PowerShell commandToRun = implementationContext.PowerShellInstance;
            PSWorkflowHost workflowHost = args.WorkflowHost;
            PSActivityContext psActivityContext = args.PSActivityContext;

            using (PowerShellTraceSource tracer = PowerShellTraceSourceFactory.GetTraceSource())
            {
                PSDataCollection<PSObject> outputForInvoke = output ??
                                                             new PSDataCollection<PSObject>();
                bool exceptionOccurred = false;

                try
                {
                    if (CheckForCancel(psActivityContext)) return;

                    AddHandlersToStreams(commandToRun, args);

                    if (CheckForCancel(psActivityContext)) return;

                    commandToRun.BeginInvoke(input, outputForInvoke, null,
                                             PowerShellInvocationCallback,
                                             args);
                }
                catch (Exception e)
                {
                    exceptionOccurred = true;

                    bool attemptRetry = ProcessException(args, e);

                    if (attemptRetry)
                    {
                        BeginActionRetry(args);
                    }
                    else
                    {
                        ReleaseResourcesAndCheckForEnd(commandToRun, args, true, false);
                    }

                    tracer.TraceException(e);
                }

                tracer.WriteMessage("Completed BeginInvoke call on PowerShell");

                if (exceptionOccurred == false &&
                    args.CommandExecutionType == CommandRunRemotely)
                {
                    if ((args.ImplementationContext != null) && (args.ImplementationContext.EnableRemotingActivityAutoResume))
                    {
                        var remotingActivity = args.ActivityObject as PSRemotingActivity;
                        if ((remotingActivity != null) &&
                            (args.PSActivityContext.HostExtension != null) &&
                            (args.PSActivityContext.HostExtension.RemoteActivityState != null))
                        {
                            // Command invocation has started, save the tasks's runspace instance id to RemoteActivityState
                            args.PSActivityContext.HostExtension.RemoteActivityState.SetRemoteActivityRunspaceEntry(
                                remotingActivity.Id, 
                                args.ImplementationContext.Id, 
                                commandToRun.Runspace.InstanceId, null); 
                        }
                    }

                    // if the runspace was obtained from the connection manager
                    // then it need to be signaled as ready for disconnect/reconnect
                    workflowHost.RemoteRunspaceProvider.ReadyForDisconnect(commandToRun.Runspace);
                }
            }
        }

        private static readonly ConcurrentDictionary<Guid, RunCommandsArguments> ArgsTableForRunspaces
            = new ConcurrentDictionary<Guid, RunCommandsArguments>();

        #region Object Decoration

        internal static void AddHandlersToStreams(System.Management.Automation.PowerShell commandToRun, RunCommandsArguments args)
        {
            if (commandToRun == null || args == null)
                return;

            bool hasErrorMerged = args.PSActivityContext.MergeErrorToOutput;

            if (hasErrorMerged)
            {
                commandToRun.Streams.Error.DataAdded += HandleErrorDataAdded;
            }

            if (args.PSActivityContext.Output != null)
                args.PSActivityContext.Output.DataAdding += HandleOutputDataAdding;
            commandToRun.Streams.Error.DataAdding += HandleErrorDataAdding;
            commandToRun.Streams.Progress.DataAdding += HandleProgressDataAdding;
            commandToRun.Streams.Verbose.DataAdding += HandleInformationalRecordDataAdding;
            commandToRun.Streams.Warning.DataAdding += HandleInformationalRecordDataAdding;
            commandToRun.Streams.Debug.DataAdding += HandleInformationalRecordDataAdding;
            commandToRun.Streams.Information.DataAdding += HandleInformationDataAdding;

            ArgsTable.TryAdd(commandToRun.InstanceId, args);
        }

        private static void HandleInformationalRecordDataAdding(object sender, DataAddingEventArgs e)
        {
            InformationalRecord informationalRecord = (InformationalRecord) e.ItemAdded;
            if (informationalRecord == null) return;

            string computerName;
            Guid jobInstanceId;
            if (GetComputerNameAndJobIdForCommand(e.PowerShellInstanceId, out computerName, out jobInstanceId))
            {
                informationalRecord.Message =
                    AddIdentifierInfoToString(jobInstanceId, computerName, informationalRecord.Message);
            }
        }

        private static void HandleProgressDataAdding(object sender, DataAddingEventArgs e)
        {
            ProgressRecord progressRecord = (ProgressRecord)e.ItemAdded;
            if (progressRecord == null) return;
           
            string computerName;
            Guid jobInstanceId;

            if (GetComputerNameAndJobIdForCommand(e.PowerShellInstanceId, out computerName, out jobInstanceId))
            {
                progressRecord.CurrentOperation = AddIdentifierInfoToString(jobInstanceId, computerName,
                                                                            progressRecord.CurrentOperation);
            }
        }

        private static void HandleInformationDataAdding(object sender, DataAddingEventArgs e)
        {
            InformationRecord informationRecord = (InformationRecord)e.ItemAdded;
            if (informationRecord == null) return;

            string computerName;
            Guid jobInstanceId;

            if (GetComputerNameAndJobIdForCommand(e.PowerShellInstanceId, out computerName, out jobInstanceId))
            {
                informationRecord.Source = AddIdentifierInfoToString(jobInstanceId, computerName,
                                                                            informationRecord.Source);
            }
        }

        internal static void AddIdentifierInfoToOutput(PSObject psObject, Guid jobInstanceId, string computerName)
        {
            Dbg.Assert(psObject != null, "PSObject not passed correctly");

            if (psObject.Properties["PSComputerName"] != null)
            {
                // if it is a NoteProperty then try changing the same
                PSNoteProperty noteProperty = psObject.Properties["PSComputerName"]  as PSNoteProperty;
                if (noteProperty != null)
                {
                    try
                    {
                        noteProperty.Value = computerName;
                    }
                    catch(SetValueException)
                    {
                        // this is a best attempt so fine to
                        // eat exception if PSComputerName is
                        // defined in type data
                    }
                }
            }
            else
                psObject.Properties.Add(new PSNoteProperty("PSComputerName", computerName));

            if (psObject.Properties["PSShowComputerName"] != null)
                psObject.Properties.Remove("PSShowComputerName");
            psObject.Properties.Add(new PSNoteProperty("PSShowComputerName", true));

            if (psObject.Properties["PSSourceJobInstanceId"] != null)
                psObject.Properties.Remove("PSSourceJobInstanceId");
            psObject.Properties.Add(new PSNoteProperty("PSSourceJobInstanceId", jobInstanceId));
        }

        private static void HandleOutputDataAdding(object sender, DataAddingEventArgs e)
        {
            PSObject psObject = (PSObject)e.ItemAdded;
            if (psObject == null) return;

            string computerName;
            Guid jobInstanceId;

            RunCommandsArguments args;
            args = GetArgsForCommand(e.PowerShellInstanceId, out computerName, out jobInstanceId);

            if (args == null)
                return;

            AddIdentifierInfoToOutput(psObject, jobInstanceId, computerName);
        }

        private static void HandleErrorDataAdding(object sender, DataAddingEventArgs e)
        {
            ErrorRecord errorRecord = (ErrorRecord)e.ItemAdded;
            if (errorRecord == null) return;

            string computerName;
            Guid jobInstanceId;
            RunCommandsArguments args = GetArgsForCommand(e.PowerShellInstanceId, out computerName, out jobInstanceId);

            if (args == null) return;

            AddIdentifierInfoToErrorRecord(errorRecord, computerName, args.PSActivityContext.JobInstanceId);

            bool hasHostExtension = args.PSActivityContext.HostExtension != null;
            bool hasErrorMerged = args.PSActivityContext.MergeErrorToOutput;

            if (!hasErrorMerged && !hasHostExtension) return;

            HostSettingCommandMetadata sourceCommandMetadata = hasHostExtension
                                                                   ? args.PSActivityContext.HostExtension.
                                                                         HostCommandMetadata
                                                                   : null;
            PSDataCollection<PSObject> outputStream = hasErrorMerged ? args.PSActivityContext.Output : null;

            PowerShellInvocation_ErrorAdding(sender, e, sourceCommandMetadata, outputStream);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="powerShellId"></param>
        /// <param name="computerName"></param>
        /// <param name="jobInstanceId"></param>
        /// <returns>false if default values were substituted</returns>
        private static bool GetComputerNameAndJobIdForCommand(Guid powerShellId, out string computerName, out Guid jobInstanceId)
        {
            RunCommandsArguments args = GetArgsForCommand(powerShellId, out computerName, out jobInstanceId);
            return args != null;
        }

        private static RunCommandsArguments GetArgsForCommand(Guid powerShellId, out string computerName, out Guid jobInstanceId)
        {
            RunCommandsArguments args;
            ArgsTable.TryGetValue(powerShellId, out args);

            computerName = LocalHost;

            jobInstanceId = args == null
                                ? Guid.Empty
                                : args.PSActivityContext.JobInstanceId;

            if (args != null )
            {
                if (args.PSActivityContext.HostExtension != null &&
                    args.PSActivityContext.HostExtension.Parameters != null &&
                    args.PSActivityContext.HostExtension.Parameters.ContainsKey("PSComputerName"))
                {
                    string[] computerNames =
                        (string[]) args.PSActivityContext.HostExtension.Parameters["PSComputerName"];

                    if (computerNames.Length == 1)
                    {
                        // this is the workflow fan-out scenario. Use the intended computer
                        // name in this case
                        computerName = computerNames[0];
                        return args;
                    }
                }

                // looks like an activity level fan-out. If this is a command
                // run with a runspace, try to extract the computer name from
                // the runspace
                switch (args.CommandExecutionType)
                {
                    case CommandRunOutOfProc:
                    case CommandRunInProc:
                    case CommandRunRemotely:
                        computerName = GetComputerNameFromCommand(args.ImplementationContext.PowerShellInstance);
                        break;
                }
            }

            if (args != null)
            {
                ActivityImplementationContext implementationContext = args.ImplementationContext;

                if (implementationContext != null)
                {
                    if (implementationContext.PSRemotingBehavior == RemotingBehavior.Custom)
                    {
                        if (implementationContext.PSComputerName != null &&
                            implementationContext.PSComputerName.Length != 0)
                        {
                            computerName = implementationContext.PSComputerName[0];
                        }
                    }
                }
            }
        
            return args;
        }

        internal static void AddIdentifierInfoToErrorRecord(ErrorRecord errorRecord, string computerName, Guid jobInstanceId)
        {
            RemotingErrorRecord remoteErrorRecord = errorRecord as RemotingErrorRecord;

            if (remoteErrorRecord != null) return;

            if (errorRecord.ErrorDetails == null)
            {
                errorRecord.ErrorDetails = new ErrorDetails(String.Empty);
                errorRecord.ErrorDetails.RecommendedAction = AddIdentifierInfoToString(jobInstanceId, computerName,
                                                                                       errorRecord.ErrorDetails.
                                                                                           RecommendedAction);
            }
            else
            {
                errorRecord.ErrorDetails.RecommendedAction =
                    AddIdentifierInfoToString(jobInstanceId, computerName, errorRecord.ErrorDetails.RecommendedAction);
            }
        }

        internal static string AddIdentifierInfoToString(Guid instanceId, string computerName, string message)
        {
            Guid jobInstanceId;
            string originalComputerName;
            string originalMessage;
            string messageToAppend = StringContainsIdentifierInfo(message, out jobInstanceId, out originalComputerName,
                                                                  out originalMessage)
                                         ? originalMessage
                                         : message;

            var newMessage = new StringBuilder(instanceId.ToString());
            newMessage.Append(":[");
            newMessage.Append(computerName);
            newMessage.Append("]:");
            newMessage.Append(messageToAppend);
            return newMessage.ToString();
        }

        private const string MessagePattern = @"^([\d\w]{8}\-[\d\w]{4}\-[\d\w]{4}\-[\d\w]{4}\-[\d\w]{12}:\[.*\]:).*";
        private static readonly char[] Delimiter = new[]{':'};

        private static bool StringContainsIdentifierInfo(string message, out Guid jobInstanceId, out string computerName, out string originalString)
        {
            jobInstanceId = Guid.Empty;
            computerName = string.Empty;
            originalString = string.Empty;

            if (string.IsNullOrEmpty(message)) return false;


            if (!Regex.IsMatch(message, MessagePattern))
                return false;

            String[] parts = message.Split(Delimiter, 3);
            if (parts.Length != 3) return false;

            if (!Guid.TryParse(parts[0], out jobInstanceId))
                jobInstanceId = Guid.Empty;

            computerName = parts[1];
            originalString = parts[2].Trim();
            return true;
        }

        private const string LocalHost = "localhost";
        private static String GetComputerNameFromCommand(System.Management.Automation.PowerShell commandToRun)
        {            
            Runspace runspace = commandToRun.Runspace;

            return runspace.ConnectionInfo == null ? LocalHost : runspace.ConnectionInfo.ComputerName;
        }

        private readonly static ConcurrentDictionary<Guid, RunCommandsArguments> ArgsTable =
            new ConcurrentDictionary<Guid, RunCommandsArguments>();

        private static void HandleErrorDataAdded(object sender, DataAddedEventArgs e)
        {
            RunCommandsArguments args;
            ArgsTable.TryGetValue(e.PowerShellInstanceId, out args);
            if (args == null) return;

            bool hasErrorMerged = args.PSActivityContext.MergeErrorToOutput;

            if (hasErrorMerged)
            {
                MergeError_DataAdded(sender, e, args.PSActivityContext.errors);
            }
        }

        internal static void RemoveHandlersFromStreams(System.Management.Automation.PowerShell commandToRun, RunCommandsArguments args)
        {
            if (commandToRun == null || args == null)
                return;

            bool hasErrorMerged = args.PSActivityContext.MergeErrorToOutput;

            if (hasErrorMerged)
            {
                commandToRun.Streams.Error.DataAdded -= HandleErrorDataAdded;
            }

            if (args.PSActivityContext.Output != null)
                args.PSActivityContext.Output.DataAdding -= HandleOutputDataAdding;
            commandToRun.Streams.Error.DataAdding -= HandleErrorDataAdding;
            commandToRun.Streams.Progress.DataAdding -= HandleProgressDataAdding;
            commandToRun.Streams.Verbose.DataAdding -= HandleInformationalRecordDataAdding;
            commandToRun.Streams.Warning.DataAdding -= HandleInformationalRecordDataAdding;
            commandToRun.Streams.Debug.DataAdding -= HandleInformationalRecordDataAdding;
            commandToRun.Streams.Information.DataAdding -= HandleInformationDataAdding;

            RunCommandsArguments arguments;
            ArgsTable.TryRemove(commandToRun.InstanceId, out arguments);
        }

        private static void MergeError_DataAdded(object sender, DataAddedEventArgs e, PSDataCollection<ErrorRecord> errors)
        {
            if (errors != null)
            {
                // Don't use the index from "e" because that may cause race condition.
                // It's safe to always remove the first item.
                errors.RemoveAt(0);
            }
        }

        private static void PowerShellInvocation_ErrorAdding(object sender, DataAddingEventArgs e, HostSettingCommandMetadata commandMetadata, PSDataCollection<PSObject> output)
        {
            ErrorRecord errorRecord = e.ItemAdded as ErrorRecord;

            if (errorRecord != null)
            {
                if (commandMetadata != null)
                {
                    ScriptPosition scriptStart = new ScriptPosition(
                        commandMetadata.CommandName,
                        commandMetadata.StartLineNumber,
                        commandMetadata.StartColumnNumber,
                        null);
                    ScriptPosition scriptEnd = new ScriptPosition(
                        commandMetadata.CommandName,
                        commandMetadata.EndLineNumber,
                        commandMetadata.EndColumnNumber,
                        null);
                    ScriptExtent extent = new ScriptExtent(scriptStart, scriptEnd);

                    if (errorRecord.InvocationInfo != null)
                    {
                        errorRecord.InvocationInfo.DisplayScriptPosition = extent;
                    }
                }

                if (output != null)
                {
                    output.Add(PSObject.AsPSObject(errorRecord));
                }
            }
        }

        #endregion Object Decoration

        /// <summary>
        /// 
        /// </summary>
        /// <param name="asyncResult"></param>
        /// <remarks>
        /// THREADING CONTRACT:
        /// This methods executes on a WinRM thread
        /// </remarks>
        internal static void PowerShellInvocationCallback(IAsyncResult asyncResult)
        {
            object asyncState = asyncResult.AsyncState;
            Dbg.Assert(asyncState != null, "AsyncState not returned correctly by PowerShell");
            RunCommandsArguments args = asyncState as RunCommandsArguments;
            Dbg.Assert(args != null, "AsyncState casting to RunCommandsArguments failed");

            PSActivityContext psActivityContext = args.PSActivityContext;
            ActivityImplementationContext implementationContext = args.ImplementationContext;
            System.Management.Automation.PowerShell commandToRun = implementationContext.PowerShellInstance;

            using (PowerShellTraceSource tracer = PowerShellTraceSourceFactory.GetTraceSource())
            {
                tracer.WriteMessage("Executing callback for Executing command using PowerShell - either inproc or remote");
                bool attemptRetry = false;
                try
                {
                    if (CheckForCancel(psActivityContext)) return;
                    commandToRun.EndInvoke(asyncResult);

                    // Do any required cleanup here...
                    implementationContext.CleanUp();

                    if (commandToRun.HadErrors)
                    {
                        tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                                                                "Errors occurred executing the command."));
                        psActivityContext.Failed = true;
                    }
                }
                catch (Exception e)
                {
                    attemptRetry = ProcessException(args, e); 
                }
                finally
                {
                    if (attemptRetry)
                    {
                        Interlocked.Decrement(ref psActivityContext.CommandsRunningCount);
                        BeginActionRetry(args);
                    }
                    else
                    {
                        ReleaseResourcesAndCheckForEnd(commandToRun, args, true, false);
                    }
                }
            }                                    
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <param name="runspaceDisconnectedException"></param>
        /// <remarks>
        /// THREADING CONTRACT:
        /// This methods executes on a WinRM thread
        /// </remarks>
        private static void RunspaceDisconnectedCallback(RunCommandsArguments args, Exception runspaceDisconnectedException)
        {            
            PSActivityContext psActivityContext = args.PSActivityContext;
            ActivityImplementationContext implementationContext = args.ImplementationContext;
            System.Management.Automation.PowerShell commandToRun = implementationContext.PowerShellInstance;
            Dbg.Assert(runspaceDisconnectedException != null, "Exception needs to be passed to RunspaceDisconnectedCallback");
            using (PowerShellTraceSource tracer = PowerShellTraceSourceFactory.GetTraceSource())
            {
                tracer.WriteMessage("Executing callback when remote runspace got disconnected");
                bool attemptRetry = false;
                try
                {
                    if (CheckForCancel(psActivityContext)) return;

                    // Do any required cleanup here...
                    implementationContext.CleanUp();

                    // if there are any helper commands they need to be disposed
                    if (args.HelperCommand != null)
                    {
                        args.HelperCommand.Dispose();
                        args.HelperCommand = null;
                    }

                    tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                                                            "Runspace disconnected is treated as an errors in executing the command."));
                    psActivityContext.Failed = true;

                    throw runspaceDisconnectedException;
                }
                catch (Exception e)
                {
                    attemptRetry = HandleRunOneCommandException(args, e);
                    if (attemptRetry)
                        BeginActionRetry(args);
                }
                finally
                {
                    RemoveHandlersFromStreams(commandToRun, args);
                    RunOneCommandFinally(args, attemptRetry);
                    tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                                                            "PowerShell activity: Finished running command."));
                    DecrementRunningCountAndCheckForEnd(psActivityContext);
                }
            }
        }      

        // Handle a failure. Check if the attempt has gone over the number of allowed attempts,
        // and if not, sleep for the retry delay.
        static private bool HandleFailure(int attempts, uint? retryCount, uint? retryInterval,
            ActivityImplementationContext implementationContext, string errorId, Exception e, PSActivityContext psActivityContext)
        {
            bool attemptRetry = false;

            // See if the attempt has gone over the quota
            if (attempts > retryCount.GetValueOrDefault(0))
            {
                // If this was a multi-machine activity, this is an error, not an exception.
                if ((implementationContext.PSComputerName != null) && (implementationContext.PSComputerName.Length > 1))
                {
                    if (e != null)
                    {
                        WriteError(e, errorId, ErrorCategory.InvalidResult, implementationContext.PowerShellInstance.Runspace.ConnectionInfo, psActivityContext);
                    }
                }
                else
                {
                    if (e != null)
                    {
                        // If they wanted to suspend on error, do so.
                        if (psActivityContext.ParameterDefaults.ContainsKey(Constants.PSSuspendOnError) &&
                            (((bool) psActivityContext.ParameterDefaults[Constants.PSSuspendOnError]) == true))
                        {
                            psActivityContext.SuspendOnError = true;
                            WriteError(e, errorId, ErrorCategory.InvalidResult, implementationContext.PowerShellInstance.Runspace.ConnectionInfo, psActivityContext);
                        }
                        else
                        {
                            lock (psActivityContext.exceptions)
                            {
                                psActivityContext.exceptions.Add(e);
                            }
                        }
                    }
                }
            }
            // We should do a retry
            else
            {
                // Write / Log that an activity retry was required.
                if (psActivityContext.progress != null)
                {
                    string progressActivity = ((Activity) psActivityContext.ActivityObject).DisplayName;

                    if (string.IsNullOrEmpty(progressActivity))
                        progressActivity = psActivityContext.ActivityType.Name;

                    string retryMessage = String.Format(CultureInfo.CurrentCulture, Resources.RetryingAction, attempts);

                    ProgressRecord progressRecord = new ProgressRecord(0, progressActivity, retryMessage);
                    lock (psActivityContext.progress)
                    {
                        psActivityContext.progress.Add(progressRecord);
                    }
                }

                if (e != null)
                {
                    WriteError(e, errorId, ErrorCategory.InvalidResult, implementationContext.PowerShellInstance.Runspace.ConnectionInfo, psActivityContext);
                }

                // Reschedule the command for another attempt.
                if (!psActivityContext.IsCanceled)
                    attemptRetry = true;

                // Wait for the retry delay
                for (int currentRetry = 0; currentRetry < retryInterval.GetValueOrDefault(1); currentRetry++)
                {
                    if (CheckForCancel(psActivityContext))
                        break;
                    Thread.Sleep(1000);
                }
            }

            return attemptRetry;
        }

        /// <summary>
        /// Cancel the running activity
        /// </summary>
        /// <param name="context">The NativeActivityContext provided by the workflow.</param>
        protected override void Cancel(NativeActivityContext context)
        {
            try
            {
                if (this.bookmarking.Get(context) == true)
                {
                    NoPersistHandle handle = this.noPersistHandle.Get(context);
                    handle.Enter(context);
                }

                PSActivityContext psActivityContextInstance = null;

                HostParameterDefaults hostValues = context.GetExtension<HostParameterDefaults>();

                if ((hostValues != null) && (hostValues.AsyncExecutionCollection != null))
                {
                    Dictionary<string, PSActivityContext> asyncExecutionCollection = hostValues.AsyncExecutionCollection;

                    if (asyncExecutionCollection.ContainsKey(context.ActivityInstanceId))
                    {
                        psActivityContextInstance = asyncExecutionCollection[context.ActivityInstanceId];
                        asyncExecutionCollection.Remove(context.ActivityInstanceId);
                    }

                }

                if (psActivityContextInstance == null)
                {
                    psActivityContextInstance = psActivityContextImplementationVariable.Get(context);
                }

                psActivityContextInstance.IsCanceled = true;
                psActivityContextImplementationVariable.Set(context, psActivityContextInstance);

                Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity: Executing cancel request."));

                if (psActivityContextInstance.commandQueue != null && !psActivityContextInstance.commandQueue.IsEmpty)
                {
                    foreach(ActivityImplementationContext activityImplementationContext in psActivityContextInstance.commandQueue.ToArray() )
                    {
                        RunCommandsArguments args = null;
                        ArgsTable.TryGetValue(activityImplementationContext.PowerShellInstance.InstanceId, out args);

                        if (args != null)
                        {
                            RemoveHandlersFromStreams(activityImplementationContext.PowerShellInstance, args);
                        }
                    }
                }

                if (psActivityContextInstance.runningCommands != null && psActivityContextInstance.runningCommands.Count > 0)
                {
                    lock (psActivityContextInstance.runningCommands)
                    {
                        foreach (System.Management.Automation.PowerShell command in psActivityContextInstance.runningCommands.Keys)
                        {
                            RunCommandsArguments args = null;
                            ArgsTable.TryGetValue(command.InstanceId, out args);

                            if (args != null)
                            {
                                RemoveHandlersFromStreams(command, args);
                            }
                        }
                    }
                }

                psActivityContextInstance.Cancel();

            }
            finally
            {
                // Marking the workflow state as cancelled.
                context.MarkCanceled();
            }
        }

        private static void UnregisterAndReleaseRunspace(Runspace runspace, PSWorkflowHost workflowHost, PSActivityContext psActivityContext)
        {
            Dbg.Assert(runspace.ConnectionInfo != null, "Only remote runspaces can be passed to UnregisterAndReleaseRunspace");
            Dbg.Assert(workflowHost != null, "For remote types PSWorkflowHost should be passed");
            Dbg.Assert(psActivityContext != null, "For remote types, activity context must be passed");
            RunCommandsArguments args;
            ArgsTableForRunspaces.TryRemove(runspace.InstanceId, out args);
            args = null;
            if (psActivityContext.HandleRunspaceStateChanged != null)
                runspace.StateChanged -= psActivityContext.HandleRunspaceStateChanged;
            workflowHost.RemoteRunspaceProvider.ReleaseRunspace(runspace);
        }

        private static void CloseRunspace(Runspace runspace, int commandType = CommandRunInProc, PSWorkflowHost workflowHost = null, PSActivityContext psActivityContext = null)
        {
            switch (commandType)
            {
                case CommandRunInProc:
                    Dbg.Assert(workflowHost != null, "For commands to run in proc PSWorkflowHost should be passed");
                    workflowHost.LocalRunspaceProvider.ReleaseRunspace(runspace);
                    break;

                case CommandRunRemotely:                    
                    UnregisterAndReleaseRunspace(runspace, workflowHost, psActivityContext);
                    break;

                case CimCommandRunInProc:
                    Dbg.Assert(workflowHost != null, "For CIM commands to run in proc, PSWorkflowHost should be passed");
                    workflowHost.LocalRunspaceProvider.ReleaseRunspace(runspace);
                    break;

                case CommandRunOutOfProc:
                case RunInProcNoRunspace:
                    {
                        // do nothing        
                    }
                    break;
                default:
                    Dbg.Assert(false, "Attempting to close runspace for a command type that is not supported");
                    break;
            }
        }

        internal static void CloseRunspaceAndDisposeCommand(System.Management.Automation.PowerShell currentCommand, PSWorkflowHost WorkflowHost, PSActivityContext psActivityContext, int commandType)
        {
            Dbg.Assert(commandType != RunInProcNoRunspace, "Disposing runspaces should not occur for WMI");
            if (!currentCommand.IsRunspaceOwner && (currentCommand.Runspace.RunspaceStateInfo.State == RunspaceState.Opened || currentCommand.Runspace.RunspaceStateInfo.State == RunspaceState.Disconnected))
                CloseRunspace(currentCommand.Runspace, commandType, WorkflowHost, psActivityContext);
            currentCommand.Dispose();
        }

        /// <summary>
        /// Retrieves the stream and ubiquitous parameter information from the hosting application.
        /// These must be passed in as "Streams" and "UbiquitousParameters", respectively.
        /// </summary>
        /// <param name="metadata">The metadata provided by the hosting application.</param>
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);

            metadata.AddImplementationVariable(this.bookmarking);
            metadata.AddImplementationVariable(this.noPersistHandle);
            metadata.AddImplementationChild(this.cancelTimer);
            metadata.AddImplementationVariable(psRunningTimeoutDelayActivityInstanceVar);
            metadata.AddImplementationChild(this.terminateActivity);
            metadata.AddImplementationChild(this.suspendActivity);
            metadata.AddDefaultExtensionProvider(() => new PSWorkflowInstanceExtension());

            this.ParameterDefaults = new Variable<Dictionary<string, object>>();
            metadata.AddImplementationVariable(this.ParameterDefaults);

            Tracer.WriteMessage(this.GetType().Name, "CacheMetadata", Guid.Empty,
                "Adding PowerShell specific extensions to metadata, CommonParameters are {0} available.",
                "not");

            metadata.AddImplementationVariable(psActivityContextImplementationVariable);
        }

        /// <summary>
        /// The event fired when the PSActivity-derived activity has initialized its its instance
        /// of System.Management.Automation.PowerShell, but has not yet invoked its action. This event
        /// is for diagnostic, tracing, and testing purposes.
        /// </summary>
        internal static event ActivityCreatedEventHandler ActivityCreated;

        private static void OnActivityCreated(Object sender, ActivityCreatedEventArgs e)
        {
            if (ActivityCreated != null)
            {
                ActivityCreated(sender, e);
            }
        }

        internal static bool IsActivityInlineScript(Activity activity)
        {
            return String.Equals(activity.GetType().FullName, "Microsoft.PowerShell.Activities.InlineScript", StringComparison.OrdinalIgnoreCase);
        }

        internal bool RunWithCustomRemoting(ActivityContext context)
        {
            if (typeof(PSRemotingActivity).IsAssignableFrom(GetType()))
            {
                PSRemotingActivity remotingActivity = (PSRemotingActivity) this;
                if (remotingActivity.PSRemotingBehavior.Get(context) == RemotingBehavior.Custom)
                    return true;
            }

            return false;            
        }

        #region Check In-Proc vs Out-of-Proc

        /// <summary>
        /// Determine if this activity should be run in or out of process
        /// when run locally/
        /// </summary>
        /// <param name="context">The native activity context for this workflow instance</param>
        /// <returns>True if it should be run in process with the workflow engine.</returns>
        protected bool GetRunInProc(ActivityContext context)
        {
            HostParameterDefaults defaults = context.GetExtension<HostParameterDefaults>();
            if (this is PSGeneratedCIMActivity)
            {
                return true;
            }

            // Look to see if there is a PSRunInProc variable in scope...
            foreach (System.ComponentModel.PropertyDescriptor property in context.DataContext.GetProperties())
            {
                if (string.Equals(property.DisplayName, WorkflowPreferenceVariables.PSRunInProcessPreference, StringComparison.OrdinalIgnoreCase))
                {
                    object parentId = property.GetValue(context.DataContext);
                    if (parentId != null)
                    {
                        bool variableValue;
                        if (LanguagePrimitives.TryConvertTo<bool>(parentId, CultureInfo.InvariantCulture, out variableValue))
                        {
                            return variableValue;
                        }
                    }
                }
            }

            PSActivityContext activityContext = psActivityContextImplementationVariable.Get(context);
            PSWorkflowHost workflowHost = GetWorkflowHost(defaults);

            return workflowHost.PSActivityHostController.RunInActivityController(this);
        }

        internal static PSWorkflowHost GetWorkflowHost(HostParameterDefaults defaults)
        {
            PSWorkflowHost _psWorkflowHost = null;

            if ((defaults != null) && (defaults.Runtime != null))
            {
                PSWorkflowHost workflowHost = defaults.Runtime;
                Interlocked.CompareExchange(ref _psWorkflowHost, workflowHost, null);

                if (_psWorkflowHost != workflowHost)
                {
                    System.Diagnostics.Debug.Assert(false, "Workflow host has been set before the incoming value was processed");
                }
            }
            if (_psWorkflowHost == null)
            {
                _psWorkflowHost = DefaultWorkflowHost.Instance;
            }

            return _psWorkflowHost;
        }

        #endregion Check In-Proc vs Out-of-Proc
    }


    /// <summary>
    /// The delegate invoked when an activity is created.
    /// </summary>
    /// <param name="sender">The PSActivity instance being invoked.</param>
    /// <param name="e">The ActivityCreatedEventArgs associated with this invocation.</param>
    internal delegate void ActivityCreatedEventHandler(object sender, ActivityCreatedEventArgs e);

    /// <summary>
    /// Holds the event arguments when a new PSActivity instance is created.
    /// </summary>
    internal class ActivityCreatedEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new ActivityCreatedEventArgs instance.
        /// </summary>
        /// <param name="instance">The instance of System.Management.Automation.PowerShell the activity has prepared.</param>
        internal ActivityCreatedEventArgs(System.Management.Automation.PowerShell instance)
        {
            PowerShellInstance = instance;
        }

        /// <summary>
        /// The instance of System.Management.Automation.PowerShell the activity has prepared.
        /// </summary>
        public System.Management.Automation.PowerShell PowerShellInstance
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Holds an instance of System.Management.Automation.PowerShell, and the context it needs to run.
    /// </summary>
    public class ActivityImplementationContext
    {
        /// <summary>
        /// The instance of System.Management.Automation.PowerShell the activity has prepared.
        /// </summary>
        public System.Management.Automation.PowerShell PowerShellInstance
        {
            get;
            set;
        }

        /// <summary>
        /// Any context required by the command.
        /// </summary>
        public Object WorkflowContext
        {
            get;
            set;
        }

        /// <summary>
        /// context id.
        /// </summary>
        internal int Id
        {
            get;
            set;
        }

        /// <summary>
        /// DisconnectedRunspaceInstanceId
        /// </summary>
        internal Guid DisconnectedRunspaceInstanceId
        {
            get;
            set;
        }

        /// <summary>
        /// DisconnectedRunspaceInstanceId
        /// </summary>
        internal bool EnableRemotingActivityAutoResume
        {
            get;
            set;
        }

        /// <summary>
        /// The Input stream / collection for the activity.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
            "CA2227:CollectionPropertiesShouldBeReadOnly",
            Justification = "This is needs to mimic the properties of the PSActivity class.")]
        public PSDataCollection<PSObject> Input
        {
            get;
            set;
        }

        /// <summary>
        /// The collection to hold the results of the activity.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
            "CA2227:CollectionPropertiesShouldBeReadOnly",
            Justification = "This is needs to mimic the properties of the PSActivity class.")]
        public PSDataCollection<PSObject> Result
        {
            get;
            set;
        }

        /// <summary>
        /// The Error stream / collection for the activity.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
            "CA2227:CollectionPropertiesShouldBeReadOnly",
            Justification = "This is needs to mimic the properties of the PSActivity class.")]
        public PSDataCollection<ErrorRecord> PSError
        {
            get;
            set;
        }

        /// <summary>
        /// The Progress stream / collection for the activity.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
            "CA2227:CollectionPropertiesShouldBeReadOnly",
            Justification = "This is needs to mimic the properties of the PSActivity class.")]
        public PSDataCollection<ProgressRecord> PSProgress
        {
            get;
            set;
        }

        /// <summary>
        /// The Verbose stream / collection for the activity.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
            "CA2227:CollectionPropertiesShouldBeReadOnly",
            Justification = "This is needs to mimic the properties of the PSActivity class.")]
        public PSDataCollection<VerboseRecord> PSVerbose
        {
            get;
            set;
        }

        /// <summary>
        /// The Debug stream / collection for the activity.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
            "CA2227:CollectionPropertiesShouldBeReadOnly",
            Justification = "This is needs to mimic the properties of the PSActivity class.")]
        public PSDataCollection<DebugRecord> PSDebug
        {
            get;
            set;
        }

        /// <summary>
        /// The Warning stream / collection for the activity.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
            "CA2227:CollectionPropertiesShouldBeReadOnly",
            Justification = "This is needs to mimic the properties of the PSActivity class.")]
        public PSDataCollection<WarningRecord> PSWarning
        {
            get;
            set;
        }

        /// <summary>
        /// The Information stream / collection for the activity.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
            "CA2227:CollectionPropertiesShouldBeReadOnly",
            Justification = "This is needs to mimic the properties of the PSActivity class.")]
        public PSDataCollection<InformationRecord> PSInformation
        {
            get;
            set;
        }

        /// <summary>
        /// The computer name to invoke this activity on.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "This is needs to mimic the properties of the PSActivity class.")]
        public string[] PSComputerName
        {
            get;
            set;
        }

        /// <summary>
        /// Defines the credential to use in the remote connection.
        /// </summary>
        public PSCredential PSCredential
        {
            get;
            set;
        }

        /// <summary>
        /// Defines the remoting behavior to use when invoking this activity.
        /// </summary>
        public RemotingBehavior PSRemotingBehavior { get; set; }

        /// <summary>
        /// Defines the number of retries that the activity will make to connect to a remote
        /// machine when it encounters an error. The default is to not retry.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public uint? PSConnectionRetryCount { get; set; }

        /// <summary>
        /// The port to use in a remote connection attempt. The default is:
        /// HTTP: 5985, HTTPS: 5986.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public uint? PSPort { get; set; }

        /// <summary>
        /// Determines whether to use SSL in the connection attempt. The default is false.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public bool? PSUseSsl { get; set; }

        /// <summary>
        /// Determines whether to allow redirection by the remote computer. The default is false.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public bool? PSAllowRedirection { get; set; }

        /// <summary>
        /// Defines the remote application name to connect to. The default is "wsman".
        /// </summary>
        public string PSApplicationName { get; set; }

        /// <summary>
        /// Defines the remote configuration name to connect to. The default is "Microsoft.PowerShell".
        /// </summary>
        public string PSConfigurationName { get; set; }

        /// <summary>
        /// Defines the fully-qualified remote URI to connect to. When specified, the PSComputerName,
        /// PSApplicationName, PSConfigurationName, and PSPort are not used.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "This is needs to mimic the properties of the PSActivity class.")]
        public string[] PSConnectionUri { get; set; }

        /// <summary>
        /// Defines the authentication type to be used in the remote connection.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public AuthenticationMechanism? PSAuthentication { get; set; }

        /// <summary>
        /// Defines the certificate thumbprint to be used in the remote connection.
        /// </summary>
        public string PSCertificateThumbprint { get; set; }

        /// <summary>
        /// Defines any session options to be used in the remote connection.
        /// </summary>
        public System.Management.Automation.Remoting.PSSessionOption PSSessionOption { get; set; }


        /// <summary>
        /// Forces the activity to return non-serialized objects. Resulting objects
        /// have functional methods and properties (as opposed to serialized versions
        /// of them), but will not survive persistence when the Workflow crashes or is
        /// persisted.
        /// </summary>
        public bool? PSDisableSerialization
        {
            get;
            set;
        }

        /// <summary>
        /// Forces the activity to not call the persist functionality, which will be responsible for 
        /// persisting the workflow state onto the disk.
        /// </summary>
        public bool? PSPersist
        {
            get;
            set;
        }

        /// <summary>
        /// Determines whether to append output to Result.
        /// </summary>
        public bool? AppendOutput
        {
            get;
            set;
        }

        /// <summary>
        /// Determines whether to merge the error data to the output stream
        /// </summary>
        public bool? MergeErrorToOutput
        {
            get; 
            set;
        }

        /// <summary>
        /// Defines the maximum amount of time, in seconds, that this activity may run.
        /// The default is unlimited.
        /// </summary>
        public uint? PSActionRunningTimeoutSec
        {
            get;
            set;
        }

        /// <summary>
        /// Defines the delay, in seconds, between connection retry attempts.
        /// The default is one second.
        /// </summary>
        public uint? PSConnectionRetryIntervalSec
        {
            get;
            set;
        }

        /// <summary>
        /// Defines the number of retries that the activity will make when it encounters
        /// an error during execution of its action. The default is to not retry.
        /// </summary>
        public uint? PSActionRetryCount
        {
            get;
            set;
        }

        /// <summary>
        /// Defines the delay, in seconds, between action retry attempts.
        /// The default is one second.
        /// </summary>
        public uint? PSActionRetryIntervalSec
        {
            get;
            set;
        }

        /// <summary>
        /// Defines the PSProgressMessage.
        /// </summary>
        public string PSProgressMessage
        {
            get;
            set;
        }

        /// <summary>
        /// The connection info to use for this command (may be null)
        /// </summary>
        public WSManConnectionInfo ConnectionInfo
        {
            get;
            set;
        }

        /// <summary>
        /// This the list of module names (or paths) that are required to run this Activity successfully.
        /// The default is null.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "This is needs to mimic the properties of the PSActivity class.")]
        public string[] PSRequiredModules
        {
            get;
            set;
        }

        /// <summary>
        /// The path that the workflow was imported from.
        /// </summary>
        public string PSWorkflowPath
        {
            get;
            set;
        }

        /// <summary>
        /// Determines whether to emit verbose output of the activity.
        /// </summary>
        public bool? Verbose
        {
            get;
            set;
        }

        /// <summary>
        /// Determines whether to emit debug output of the activity.
        /// </summary>
        public bool? Debug
        {
            get;
            set;
        }

        /// <summary>
        /// Determines whether to emit whatif output of the activity.
        /// </summary>
        public bool? WhatIf
        {
            get;
            set;
        }

        /// <summary>
        /// Determines how errors should be handled by the activity.
        /// </summary>
        public ActionPreference? ErrorAction
        {
            get;
            set;
        }

        /// <summary>
        /// Determines how warnings should be handled by the activity.
        /// </summary>
        public ActionPreference? WarningAction
        {
            get;
            set;
        }

        /// <summary>
        /// Determines how information messages should be handled by the activity.
        /// </summary>
        public ActionPreference? InformationAction
        {
            get;
            set;
        }
        /// <summary>
        /// Policy for activity host that will execute this activity
        /// </summary>
        public PSActivityEnvironment PSActivityEnvironment
        {
            get;
            set;
        }

        /// <summary>
        ///  Specifies the authentication level to be used with the WMI connection. Valid values are:
        ///   -1: Unchanged
        ///    0: Default
        ///    1: None (No authentication in performed.)
        ///    2: Connect (Authentication is performed only when the client establishes a relationship with the application.)
        ///    3: Call (Authentication is performed only at the beginning of each call when the application receives the request.)
        ///    4: Packet (Authentication is performed on all the data that is received from the client.)
        ///    5: PacketIntegrity (All the data that is transferred between the client and the application is authenticated and verified.)
        ///    6: PacketPrivacy (The properties of the other authentication levels are used, and all the data is encrypted.)
        /// </summary>
        public AuthenticationLevel PSAuthenticationLevel { get; set; }

        /// <summary>
        /// Specifies the impersonation level to use. Valid values are: 
        /// 0: Default (reads the local registry for the default impersonation level , which is usually set to "3: Impersonate".)
        ///  1: Anonymous (Hides the credentials of the caller.)
        ///  2: Identify (Allows objects to query the credentials of the caller.)
        ///  3: Impersonate (Allows objects to use the credentials of the caller.)
        ///  4: Delegate (Allows objects to permit other objects to use the credentials of the caller.)
        /// </summary>
        public ImpersonationLevel Impersonation { get; set; }

        /* 
         * Enables all the privileges of the current user before the command makes the WMI call.
         */
        /// <summary>
        /// Enables all the privileges of the current user before the command makes the WMI call.
        /// </summary>
        public bool EnableAllPrivileges { get; set; }

        /// <summary>
        /// Specifies the authority to use to authenticate the WMI connection. You can specify
        /// standard NTLM or Kerberos authentication. To use NTLM, set the authority setting 
        /// to ntlmdomain:"DomainName", where "DomainName" identifies a valid NTLM domain name.
        /// To use Kerberos, specify kerberos:"DomainName>\ServerName". You cannot include
        /// the authority setting when you connect to the local computer.
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// When used with the Class parameter, this parameter specifies the WMI repository namespace
        /// where the referenced WMI class is located. When used with the List parameter, it specifies
        /// the namespace from which to gather WMI class information.
        /// </summary>summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Specifies the preferred locale for WMI objects. Specify the value of the Locale
        /// parameter as an array in the MS_"LCID" format in the preferred order .
        /// </summary>
        public string Locale { get; set; }

        /// <summary>
        /// CIM Sessions to use for this activity.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "This is needs to mimic the properties of the PSActivity class.")]
        public CimSession[] CimSession { get; set; }


        /// <summary>
        /// Perform any cleanup activities needed by this activity implementation
        /// </summary>
        public virtual void CleanUp()
        {
        }        
    }

    class RetryCount
    {
        //internal int ConnectionAttempts { get; set; }
        internal int ActionAttempts
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Defining resuming extension.
    /// </summary>
    public class PSWorkflowInstanceExtension : IWorkflowInstanceExtension
    {
        private WorkflowInstanceProxy instance;

        /// <summary>
        /// Get all additional extensions.
        /// </summary>
        /// <returns>Returns no extensions.</returns>
        public IEnumerable<object> GetAdditionalExtensions()
        {
            return null;
        }

        /// <summary>
        /// Set the instance of the workflow.
        /// </summary>
        /// <param name="instance">The workflow instance proxy.</param>
        public void SetInstance(WorkflowInstanceProxy instance)
        {
            this.instance = instance;
        }

        /// <summary>
        /// Begin resuming book mark.
        /// </summary>
        /// <param name="bookmark">The bookmark where it will be resumed.</param>
        /// <param name="value">The value which need to be passed to the bookmark.</param>
        /// <param name="callback">The call back function when resuming the bookmark.</param>
        /// <param name="state">The state of the async call.</param>
        /// <returns>Returns the result of async call.</returns>
        public IAsyncResult BeginResumeBookmark(Bookmark bookmark, object value, AsyncCallback callback, object state)
        {
            return instance.BeginResumeBookmark(bookmark, value, callback, state);
        }

        /// <summary>
        /// End resuming bookmark.
        /// </summary>
        /// <param name="asyncResult">The result of async all.</param>
        /// <returns>Returns the bookmark resumption result.</returns>
        public BookmarkResumptionResult EndResumeBookmark(IAsyncResult asyncResult)
        {
            return instance.EndResumeBookmark(asyncResult);
        }
    }

    /// <summary>
    /// Stores information about an activity argument
    /// </summary>
    public sealed class PSActivityArgumentInfo
    {
        /// <summary>
        /// The name of the argument.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The actual argument.
        /// </summary>
        public Argument Value { get; set; }
    }

    /// <summary>
    /// Abstract base containing the common members and invocation code for the WMI cmdlets.
    /// </summary>
    public abstract class WmiActivity : PSActivity
    {
        /// <summary>
        /// The computer name to invoke this activity on.
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
        public InArgument<string[]> PSComputerName
        {
            get;
            set;
        }

        /// <summary>
        /// Defines the credential to use in the remote connection.
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
        public InArgument<PSCredential> PSCredential
        {
            get;
            set;
        }

        /// <summary>
        ///  Specifies the authentication level to be used with the WMI connection. Valid values are:
        ///   -1: Unchanged
        ///    0: Default
        ///    1: None (No authentication in performed.)
        ///    2: Connect (Authentication is performed only when the client establishes a relationship with the application.)
        ///    3: Call (Authentication is performed only at the beginning of each call when the application receives the request.)
        ///    4: Packet (Authentication is performed on all the data that is received from the client.)
        ///    5: PacketIntegrity (All the data that is transferred between the client and the application is authenticated and verified.)
        ///    6: PacketPrivacy (The properties of the other authentication levels are used, and all the data is encrypted.)
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
        public AuthenticationLevel PSAuthenticationLevel { get; set; }

        /// <summary>
        /// Specifies the impersonation level to use. Valid values are: 
        /// 0: Default (reads the local registry for the default impersonation level , which is usually set to "3: Impersonate".)
        ///  1: Anonymous (Hides the credentials of the caller.)
        ///  2: Identify (Allows objects to query the credentials of the caller.)
        ///  3: Impersonate (Allows objects to use the credentials of the caller.)
        ///  4: Delegate (Allows objects to permit other objects to use the credentials of the caller.)
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
        public ImpersonationLevel Impersonation { get; set; }

        /* 
         * Enables all the privileges of the current user before the command makes the WMI call.
         */
        /// <summary>
        /// Enables all the privileges of the current user before the command makes the WMI call.
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
        public bool EnableAllPrivileges { get; set; }

        /// <summary>
        /// Specifies the authority to use to authenticate the WMI connection. You can specify
        /// standard NTLM or Kerberos authentication. To use NTLM, set the authority setting 
        /// to ntlmdomain:"DomainName", where "DomainName" identifies a valid NTLM domain name.
        /// To use Kerberos, specify kerberos:"DomainName>\ServerName". You cannot include
        /// the authority setting when you connect to the local computer.
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
        public string Authority { get; set; }

        /// <summary>
        /// When used with the Class parameter, this parameter specifies the WMI repository namespace
        /// where the referenced WMI class is located. When used with the List parameter, it specifies
        /// the namespace from which to gather WMI class information.
        /// </summary>summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        public InArgument<string> Namespace { get; set; }

        /// <summary>
        /// Specifies the preferred locale for WMI objects. Specify the value of the Locale
        /// parameter as an array in the MS_"LCID" format in the preferred order .
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        public string Locale { get; set; }

        /// <summary>
        /// Generic version of the function to handle value types
        /// </summary>
        /// <typeparam name="T">The type of the intended argument</typeparam>
        /// <param name="parameterName"></param>
        /// <param name="parameterDefaults"></param>
        /// <returns></returns>
        protected T GetUbiquitousParameter<T>(string parameterName, Dictionary<string, object> parameterDefaults)
        {
            if (ParameterDefaults != null && parameterDefaults.ContainsKey(parameterName))
                return (T)parameterDefaults[parameterName];
            else
                return default(T);
        }

        /// <summary>
        /// Sets to execute the command that was passed in.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="name"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        protected System.Management.Automation.PowerShell GetWmiCommandCore(NativeActivityContext context, string name)
        {
            System.Management.Automation.PowerShell command;
            command = System.Management.Automation.PowerShell.Create().AddCommand(name);
            Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity ID={0}: WMI Command '{1}'.",
                context.ActivityInstanceId, name));


            if (Impersonation != ImpersonationLevel.Default)
            {
                command.AddParameter("Impersonation", Impersonation);
                Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity ID={0}: Setting parameter {1} to {2}.",
                    context.ActivityInstanceId, "Impersonation", Impersonation));

            }

            Dictionary<string, object> parameterDefaults = context.GetValue<Dictionary<string, object>>(this.ParameterDefaults);

            if (PSAuthenticationLevel != AuthenticationLevel.Default)
            {
                command.AddParameter("Authentication", PSAuthenticationLevel);
                Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity ID={0}: Setting parameter {1} to {2}.",
                    context.ActivityInstanceId, "Authentication", PSAuthenticationLevel));
            }
            else if (GetUbiquitousParameter<AuthenticationLevel>("PSAuthenticationLevel", parameterDefaults) != AuthenticationLevel.Default)
            {
                var authLevel = GetUbiquitousParameter<AuthenticationLevel>("PSAuthenticationLevel", parameterDefaults);
                command.AddParameter("Authentication", authLevel);
                Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                    "PowerShell activity ID={0}: Setting parameter {1} to {2} from ubiquitous parameters.",
                        context.ActivityInstanceId, "AuthenticationLevel", authLevel));
            }

            if (Locale != null)
            {
                command.AddParameter("Locale", Locale);
                Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                    "PowerShell activity ID={0}: Setting parameter {1} to {2}.",
                        context.ActivityInstanceId, "Locale", Locale));

            }

            if (EnableAllPrivileges)
            {
                command.AddParameter("EnableAllPrivileges", EnableAllPrivileges);
                Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                    "PowerShell activity ID={0}: Setting parameter {1} to {2}.",
                        context.ActivityInstanceId, "EnableAllPrivileges", EnableAllPrivileges));
            }

            if (Authority != null)
            {
                command.AddParameter("Authority", Authority);
                Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                    "PowerShell activity ID={0}: Setting parameter {1} to {2}.", context.ActivityInstanceId, "Authority", Authority));
            }

            if (Namespace.Get(context) != null)
            {
                command.AddParameter("Namespace", Namespace.Get(context));
                Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity ID={0}: Setting parameter {1} to {2}.",
                    context.ActivityInstanceId, "Namespace", Namespace.Get(context)));
            }

            // WMI does it's own remoting so we need to handle the PSCredential/Credential parameter
            // explicitly ourselves.
            if (PSCredential.Get(context) != null)
            {
                command.AddParameter("Credential", PSCredential.Get(context));
                Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity ID={0}: Setting parameter {1} to {2}.",
                    context.ActivityInstanceId, "Credential", PSCredential.Get(context)));
            }

            return command;
        }

        /// <summary>
        /// Perform necessary steps to prepare the WMI commands
        /// </summary>
        /// <param name="context">The activity context to use</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        protected override List<ActivityImplementationContext> GetImplementation(NativeActivityContext context)
        {
            List<ActivityImplementationContext> commands = new List<ActivityImplementationContext>();
            string[] computernames = PSComputerName.Get(context);

            // Configure the remote connectivity options
            if (computernames == null || computernames.Length == 0)
            {
                computernames = new string[] { "localhost" };
            }

            foreach (string computername in computernames)
            {
                // Create the PowerShell instance, and add the command to it.
                ActivityImplementationContext implementationContext = GetPowerShell(context);
                System.Management.Automation.PowerShell invoker = implementationContext.PowerShellInstance;

                // Don't add the computer if it's empty or localhost...
                if (!String.IsNullOrEmpty(computername) && !String.Equals(computername, "localhost", StringComparison.OrdinalIgnoreCase))
                {
                    invoker.AddParameter("ComputerName", computername);
                }

                commands.Add(
                    new ActivityImplementationContext() { PowerShellInstance = invoker }
                );
            }

            return commands;
        }
    }

    /// <summary>
    /// Implementation of ICommandRuntime for running the WMI cmdlets in
    /// workflow without PowerShell.
    /// </summary>
    internal class DirectExecutionActivitiesCommandRuntime : ICommandRuntime
    {

        /// <summary>
        /// Constructs an instance of the default ICommandRuntime object
        /// that will write objects into the arraylist that was passed.
        /// </summary>
        public DirectExecutionActivitiesCommandRuntime(PSDataCollection<PSObject> output, ActivityImplementationContext implementationContext, Type cmdletType)
        {
            if (output == null) throw new ArgumentNullException("output");
            if (implementationContext == null) throw new ArgumentNullException("implementationContext");
            if (cmdletType == null) throw new ArgumentNullException("cmdletType");

            _output = output;
            _implementationContext = implementationContext;
            _cmdletType = cmdletType;
        }

        PSDataCollection<PSObject> _output;
        ActivityImplementationContext _implementationContext;
        Type _cmdletType;

        /// <summary>
        /// THe error record stream
        /// </summary>
        public PSDataCollection<ErrorRecord> Error { get; set; }

        /// <summary>
        /// The progress record stream
        /// </summary>
        public PSDataCollection<ProgressRecord> Progress { get; set; }

        /// <summary>
        /// The verbose record stream
        /// </summary>
        public PSDataCollection<VerboseRecord> Verbose { get; set; }

        /// <summary>
        /// The warning record stream
        /// </summary>
        public PSDataCollection<WarningRecord> Warning { get; set; }

        /// <summary>
        /// The debug output stream
        /// </summary>
        public PSDataCollection<DebugRecord> Debug { get; set; }

        /// <summary>
        /// The information record stream
        /// </summary>
        public PSDataCollection<InformationRecord> Information { get; set; }

        /// <summary>
        /// Return the instance of PSHost - null by default.
        /// </summary>
        public PSHost Host { get { return null; } }

        #region Write
        /// <summary>
        /// Implementation of WriteDebug - just discards the input.
        /// </summary>
        /// <param name="text">Text to write</param>
        public void WriteDebug(string text)
        {
            if (Debug == null)
                return;

            if (text != null)
            {
                Debug.Add(new DebugRecord(text));
            }
        }

        /// <summary>
        /// Default implementation of WriteError - if the error record contains
        /// an exception then that exception will be thrown. If not, then an
        /// InvalidOperationException will be constructed and thrown.
        /// </summary>
        /// <param name="errorRecord">Error record instance to process</param>
        public void WriteError(ErrorRecord errorRecord)
        {
            if (Error == null)
                return;

            ErrorRecord updatedErrorRecord = new ErrorRecord(errorRecord.Exception, errorRecord.FullyQualifiedErrorId + ',' + _cmdletType.FullName, errorRecord.CategoryInfo.Category, errorRecord.TargetObject);

            ActionPreference preference = (_implementationContext.ErrorAction == null) ?
                ActionPreference.Continue : _implementationContext.ErrorAction.Value;

            switch (preference)
            {
                case ActionPreference.SilentlyContinue:
                case ActionPreference.Ignore:
                    break;
                case ActionPreference.Inquire:
                case ActionPreference.Continue:
                    if (errorRecord != null)
                    {
                        Error.Add(updatedErrorRecord);
                    }
                    break;
                case ActionPreference.Stop:
                    ThrowTerminatingError(updatedErrorRecord);
                    break;
            }
        }

        /// <summary>
        /// Default implementation of WriteObject - adds the object to the arraylist
        /// passed to the objects constructor.
        /// </summary>
        /// <param name="sendToPipeline">Object to write</param>
        public void WriteObject(object sendToPipeline)
        {
            _output.Add(PSObject.AsPSObject(sendToPipeline));
        }

        /// <summary>
        /// Write objects to the output collection
        /// </summary>
        /// <param name="sendToPipeline">Object to write</param>
        /// <param name="enumerateCollection">If true, the collection is enumerated, otherwise
        /// it's written as a scalar.
        /// </param>
        public void WriteObject(object sendToPipeline, bool enumerateCollection)
        {
            if (enumerateCollection)
            {
                IEnumerator e = LanguagePrimitives.GetEnumerator(sendToPipeline);
                if (e == null)
                {
                    WriteObject(sendToPipeline);
                }
                else
                {
                    while (e.MoveNext())
                    {
                        WriteObject(e.Current);
                    }
                }
            }
            else
            {
                WriteObject(sendToPipeline);
            }
        }

        /// <summary>
        /// Write a progress record
        /// </summary>
        /// <param name="progressRecord">progress record to write.</param>
        public void WriteProgress(ProgressRecord progressRecord)
        {
            WriteProgress(1, progressRecord);
        }

        /// <summary>
        /// Write a progress record, ignore the id field
        /// </summary>
        /// <param name="sourceId">Source ID to write for</param>
        /// <param name="progressRecord">record to write.</param>
        public void WriteProgress(Int64 sourceId, ProgressRecord progressRecord)
        {
            if (Progress == null)
                return;

            if (progressRecord != null)
            {
                Progress.Add(progressRecord);
            }
        }

        /// <summary>
        /// Write a verbose record
        /// </summary>
        /// <param name="text">Text to write.</param>
        public void WriteVerbose(string text)
        {
            if (_implementationContext.Verbose != true)
                return;

            if (Verbose == null)
                return;

            if (text != null)
            {
                Verbose.Add(new VerboseRecord(text));
            }
        }

        /// <summary>
        /// Write a warning record
        /// </summary>
        /// <param name="text">Text to write.</param>
        public void WriteWarning(string text)
        {
            if (_implementationContext.WarningAction != ActionPreference.Continue)
                return;

            if (Warning == null)
                return;

            if (text != null)
            {
                Warning.Add(new WarningRecord(text));
            }
        }

        /// <summary>
        /// Write a information record
        /// </summary>
        /// <param name="record">Record to write.</param>
        public void WriteInformation(InformationRecord record)
        {
            if (_implementationContext.InformationAction != ActionPreference.Continue)
                return;

            if (Information == null)
                return;

            if (record != null)
            {
                Information.Add(record);
            }
        }

        /// <summary>
        /// Write command detail info to the eventlog.
        /// </summary>
        /// <param name="text">Text to write.</param>
        public void WriteCommandDetail(string text) 
        {
            PowerShellTraceSource tracer = PowerShellTraceSourceFactory.GetTraceSource();
            tracer.WriteMessage(text);
        }

        #endregion Write

        #region Should
        /// <summary>
        /// Default implementation - always returns true.
        /// </summary>
        /// <param name="target">ignored</param>
        /// <returns>true</returns>
        public bool ShouldProcess(string target) { return true; }
        /// <summary>
        /// Default implementation - always returns true.
        /// </summary>
        /// <param name="target">ignored</param>
        /// <param name="action">ignored</param>
        /// <returns>true</returns>
        public bool ShouldProcess(string target, string action) { return true; }
        /// <summary>
        /// Default implementation - always returns true.
        /// </summary>
        /// <param name="verboseDescription">ignored</param>
        /// <param name="verboseWarning">ignored</param>
        /// <param name="caption">ignored</param>
        /// <returns>true</returns>
        public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption) { return true; }
        /// <summary>
        /// Default implementation - always returns true.
        /// </summary>
        /// <param name="verboseDescription">ignored</param>
        /// <param name="verboseWarning">ignored</param>
        /// <param name="caption">ignored</param>
        /// <param name="shouldProcessReason">ignored</param>
        /// <returns>true</returns>
        public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption, out ShouldProcessReason shouldProcessReason) { shouldProcessReason = ShouldProcessReason.None; return true; }
        /// <summary>
        /// Default implementation - always returns true.
        /// </summary>
        /// <param name="query">ignored</param>
        /// <param name="caption">ignored</param>
        /// <returns>true</returns>
        public bool ShouldContinue(string query, string caption) { return true; }
        /// <summary>
        /// Default implementation - always returns true.
        /// </summary>
        /// <param name="query">ignored</param>
        /// <param name="caption">ignored</param>
        /// <param name="yesToAll">ignored</param>
        /// <param name="noToAll">ignored</param>
        /// <returns>true</returns>
        public bool ShouldContinue(string query, string caption, ref bool yesToAll, ref bool noToAll) { return true; }
        #endregion Should

        #region Transaction Support
        /// <summary>
        /// Returns true if a transaction is available and active.
        /// </summary>
        public bool TransactionAvailable() { return false; }

        /// <summary>
        /// Gets an object that surfaces the current PowerShell transaction.
        /// When this object is disposed, PowerShell resets the active transaction
        /// </summary>
        public PSTransactionContext CurrentPSTransaction
        {
            get
            {
                // We want to throw in this situation, and want to use a
                // property because it mimics the C# using(TransactionScope ...) syntax
                throw new InvalidOperationException();
            }
        }
        #endregion Transaction Support

        #region Misc
        /// <summary>
        /// Implementation of the dummy default ThrowTerminatingError API - it just
        /// does what the base implementation does anyway - rethrow the exception
        /// if it exists, otherwise throw an invalid operation exception.
        /// </summary>
        /// <param name="errorRecord">The error record to throw</param>
        public void ThrowTerminatingError(ErrorRecord errorRecord)
        {
            if (errorRecord.Exception != null)
            {
                throw errorRecord.Exception;
            }
            else
            {
                throw new System.InvalidOperationException(errorRecord.ToString());
            }
        }
        #endregion
    }

    /// <summary>
    /// Suspends the current workflow.
    /// </summary>
    internal class SuspendOnError : NativeActivity
    {
        /// <summary>
        /// Returns true if the activity can induce an idle.
        /// </summary>
        protected override bool CanInduceIdle { get { return true; } }

        /// <summary>
        /// Invokes the activity
        /// </summary>
        /// <param name="context">The activity context.</param>
        /// <returns>True if the given argument is set.</returns>
        protected override void Execute(NativeActivityContext context)
        {
            string bookmarkname = PSActivity.PSSuspendBookmarkPrefix;
            bookmarkname += Guid.NewGuid().ToString().Replace("-", "_");

            context.CreateBookmark(bookmarkname, BookmarkResumed);
        }

        private void BookmarkResumed(NativeActivityContext context, Bookmark bookmark, object value)
        {
        }
    }
}
