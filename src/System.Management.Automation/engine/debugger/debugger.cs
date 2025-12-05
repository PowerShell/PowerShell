// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Internal.Host;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace System.Management.Automation
{
    #region Event Args

    /// <summary>
    /// Possible actions for the debugger after hitting a breakpoint/step.
    /// </summary>
    public enum DebuggerResumeAction
    {
        /// <summary>
        /// Continue running until the next breakpoint, or the end of the script.
        /// </summary>
        Continue = 0,
        /// <summary>
        /// Step to next statement, going into functions, scripts, etc.
        /// </summary>
        StepInto = 1,
        /// <summary>
        /// Step to next statement, going over functions, scripts, etc.
        /// </summary>
        StepOut = 2,
        /// <summary>
        /// Step to next statement after the current function, script, etc.
        /// </summary>
        StepOver = 3,
        /// <summary>
        /// Stop executing the script.
        /// </summary>
        Stop = 4,
    }

    /// <summary>
    /// Arguments for the DebuggerStop event.
    /// </summary>
    public class DebuggerStopEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes the DebuggerStopEventArgs.
        /// </summary>
        internal DebuggerStopEventArgs(InvocationInfo invocationInfo, List<Breakpoint> breakpoints)
        {
            this.InvocationInfo = invocationInfo;
            this.Breakpoints = new ReadOnlyCollection<Breakpoint>(breakpoints);
            this.ResumeAction = DebuggerResumeAction.Continue;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="invocationInfo"></param>
        /// <param name="breakpoints"></param>
        /// <param name="resumeAction"></param>
        public DebuggerStopEventArgs(
            InvocationInfo invocationInfo,
            Collection<Breakpoint> breakpoints,
            DebuggerResumeAction resumeAction)
        {
            this.InvocationInfo = invocationInfo;
            this.Breakpoints = new ReadOnlyCollection<Breakpoint>(breakpoints);
            this.ResumeAction = resumeAction;
        }

        /// <summary>
        /// Invocation info of the code being executed.
        /// </summary>
        public InvocationInfo InvocationInfo { get; internal set; }

        /// <summary>
        /// The breakpoint(s) hit.
        /// </summary>
        /// <remarks>
        /// Note there may be more than one breakpoint on the same object (line, variable, command). A single event is
        /// raised for all these breakpoints.
        /// </remarks>
        public ReadOnlyCollection<Breakpoint> Breakpoints { get; }

        /// <summary>
        /// This property must be set in the event handler to indicate the debugger what it should do next.
        /// </summary>
        /// <remarks>
        /// The default action is DebuggerAction.Continue.
        /// DebuggerAction.StepToLine is only valid when debugging an script.
        /// </remarks>
        public DebuggerResumeAction ResumeAction { get; set; }

        /// <summary>
        /// This property is used internally for remote debug stops only.  It is used to signal the remote debugger proxy
        /// that it should *not* send a resume action to the remote debugger.  This is used by runspace debug processing to
        /// leave pending runspace debug sessions suspended until a debugger is attached.
        /// </summary>
        internal bool SuspendRemote { get; set; }
    }

    /// <summary>
    /// Kinds of breakpoint updates.
    /// </summary>
    public enum BreakpointUpdateType
    {
        /// <summary>
        /// A breakpoint was set.
        /// </summary>
        Set = 0,
        /// <summary>
        /// A breakpoint was removed.
        /// </summary>
        Removed = 1,
        /// <summary>
        /// A breakpoint was enabled.
        /// </summary>
        Enabled = 2,
        /// <summary>
        /// A breakpoint was disabled.
        /// </summary>
        Disabled = 3
    }

    /// <summary>
    /// Arguments for the BreakpointUpdated event.
    /// </summary>
    public class BreakpointUpdatedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes the BreakpointUpdatedEventArgs.
        /// </summary>
        internal BreakpointUpdatedEventArgs(Breakpoint breakpoint, BreakpointUpdateType updateType, int breakpointCount)
        {
            this.Breakpoint = breakpoint;
            this.UpdateType = updateType;
            this.BreakpointCount = breakpointCount;
        }

        /// <summary>
        /// Gets the breakpoint that was updated.
        /// </summary>
        public Breakpoint Breakpoint { get; }

        /// <summary>
        /// Gets the type of update.
        /// </summary>
        public BreakpointUpdateType UpdateType { get; }

        /// <summary>
        /// Gets the current breakpoint count.
        /// </summary>
        public int BreakpointCount { get; }
    }

    #region PSJobStartEventArgs

    /// <summary>
    /// Arguments for the script job start callback event.
    /// </summary>
    public sealed class PSJobStartEventArgs : EventArgs
    {
        /// <summary>
        /// Job to be started.
        /// </summary>
        public Job Job { get; }

        /// <summary>
        /// Job debugger.
        /// </summary>
        public Debugger Debugger { get; }

        /// <summary>
        /// Job is run asynchronously.
        /// </summary>
        public bool IsAsync { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="job">Started job.</param>
        /// <param name="debugger">Debugger.</param>
        /// <param name="isAsync">Job started asynchronously.</param>
        public PSJobStartEventArgs(Job job, Debugger debugger, bool isAsync)
        {
            this.Job = job;
            this.Debugger = debugger;
            this.IsAsync = isAsync;
        }
    }

    #endregion

    #region Runspace Debug Processing

    /// <summary>
    /// StartRunspaceDebugProcessing event arguments.
    /// </summary>
    public sealed class StartRunspaceDebugProcessingEventArgs : EventArgs
    {
        /// <summary> The runspace to process </summary>
        public Runspace Runspace { get; }

        /// <summary>
        /// When set to true this will cause PowerShell to process this runspace debug session through its
        /// script debugger.  To use the default processing return from this event call after setting
        /// this property to true.
        /// </summary>
        public bool UseDefaultProcessing
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public StartRunspaceDebugProcessingEventArgs(Runspace runspace)
        {
            if (runspace == null) { throw new PSArgumentNullException(nameof(runspace)); }

            Runspace = runspace;
        }
    }

    /// <summary>
    /// ProcessRunspaceDebugEnd event arguments.
    /// </summary>
    public sealed class ProcessRunspaceDebugEndEventArgs : EventArgs
    {
        /// <summary>
        /// The runspace where internal debug processing has ended.
        /// </summary>
        public Runspace Runspace { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="runspace"></param>
        public ProcessRunspaceDebugEndEventArgs(Runspace runspace)
        {
            if (runspace == null) { throw new PSArgumentNullException(nameof(runspace)); }

            Runspace = runspace;
        }
    }

    #endregion

    #endregion

    #region Enums

    /// <summary>
    /// Defines debugging mode.
    /// </summary>
    [Flags]
    public enum DebugModes
    {
        /// <summary>
        /// PowerShell script debugging is disabled.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Default setting for original PowerShell script debugging.
        /// Compatible with PowerShell Versions 2 and 3.
        /// </summary>
        Default = 0x1,

        /// <summary>
        /// PowerShell script debugging.
        /// </summary>
        LocalScript = 0x2,

        /// <summary>
        /// PowerShell remote script debugging.
        /// </summary>
        RemoteScript = 0x4
    }

    /// <summary>
    /// Defines unhandled breakpoint processing behavior.
    /// </summary>
    internal enum UnhandledBreakpointProcessingMode
    {
        /// <summary>
        /// Ignore unhandled breakpoint events.
        /// </summary>
        Ignore = 1,

        /// <summary>
        /// Wait on unhandled breakpoint events until a handler is available.
        /// </summary>
        Wait
    }

    #endregion

    #region Debugger base class

    /// <summary>
    /// Base class for all PowerShell debuggers.
    /// </summary>
    public abstract class Debugger
    {
        #region Events

        /// <summary>
        /// Event raised when the debugger hits a breakpoint or a step.
        /// </summary>
        public event EventHandler<DebuggerStopEventArgs> DebuggerStop;

        /// <summary>
        /// Event raised when a breakpoint is updated.
        /// </summary>
        public event EventHandler<BreakpointUpdatedEventArgs> BreakpointUpdated;

        /// <summary>
        /// Event raised when nested debugging is cancelled.
        /// </summary>
        internal event EventHandler<EventArgs> NestedDebuggingCancelledEvent;

        #region Runspace Debug Processing Events

        /// <summary>
        /// Event raised when a runspace debugger needs breakpoint processing.
        /// </summary>
        public event EventHandler<StartRunspaceDebugProcessingEventArgs> StartRunspaceDebugProcessing;

        /// <summary>
        /// Event raised when a runspace debugger is finished being processed.
        /// </summary>
        public event EventHandler<ProcessRunspaceDebugEndEventArgs> RunspaceDebugProcessingCompleted;

        /// <summary>
        /// Event raised to indicate that the debugging session is over and runspace debuggers queued for
        /// processing should be released.
        /// </summary>
        public event EventHandler<EventArgs> CancelRunspaceDebugProcessing;

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// True when the debugger is stopped.
        /// </summary>
        protected bool DebuggerStopped
        {
            get;
            private set;
        }

        /// <summary>
        /// IsPushed.
        /// </summary>
        internal virtual bool IsPushed
        {
            get { return false; }
        }

        /// <summary>
        /// IsRemote.
        /// </summary>
        internal virtual bool IsRemote
        {
            get { return false; }
        }

        /// <summary>
        /// Returns true if the debugger is preserving a DebuggerStopEvent
        /// event.  Use ReleaseSavedDebugStop() to allow event to process.
        /// </summary>
        internal virtual bool IsPendingDebugStopEvent
        {
            get { throw new PSNotImplementedException(); }
        }

        /// <summary>
        /// Returns true if debugger has been set to stepInto mode.
        /// </summary>
        internal virtual bool IsDebuggerSteppingEnabled
        {
            get { throw new PSNotImplementedException(); }
        }

        /// <summary>
        /// Returns true if there is a handler for debugger stops.
        /// </summary>
        internal bool IsDebugHandlerSubscribed
        {
            get { return (DebuggerStop != null); }
        }

        /// <summary>
        /// UnhandledBreakpointMode.
        /// </summary>
        internal virtual UnhandledBreakpointProcessingMode UnhandledBreakpointMode
        {
            get { throw new PSNotImplementedException(); }

            set { throw new PSNotImplementedException(); }
        }

        /// <summary>
        /// DebuggerMode.
        /// </summary>
        public DebugModes DebugMode { get; protected set; } = DebugModes.Default;

        /// <summary>
        /// Returns true if debugger has breakpoints set and
        /// is currently active.
        /// </summary>
        public virtual bool IsActive
        {
            get { return false; }
        }

        /// <summary>
        /// InstanceId.
        /// </summary>
        public virtual Guid InstanceId
        {
            get { return s_instanceId; }
        }

        /// <summary>
        /// True when debugger is stopped at a breakpoint.
        /// </summary>
        public virtual bool InBreakpoint
        {
            get { return DebuggerStopped; }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// RaiseDebuggerStopEvent.
        /// </summary>
        /// <param name="args">DebuggerStopEventArgs.</param>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
        protected void RaiseDebuggerStopEvent(DebuggerStopEventArgs args)
        {
            try
            {
                DebuggerStopped = true;
                DebuggerStop.SafeInvoke<DebuggerStopEventArgs>(this, args);
            }
            finally
            {
                DebuggerStopped = false;
            }
        }

        /// <summary>
        /// IsDebuggerStopEventSubscribed.
        /// </summary>
        /// <returns>True if event subscription exists.</returns>
        protected bool IsDebuggerStopEventSubscribed()
        {
            return (DebuggerStop != null);
        }

        /// <summary>
        /// RaiseBreakpointUpdatedEvent.
        /// </summary>
        /// <param name="args">BreakpointUpdatedEventArgs.</param>
        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
        protected void RaiseBreakpointUpdatedEvent(BreakpointUpdatedEventArgs args)
        {
            BreakpointUpdated.SafeInvoke<BreakpointUpdatedEventArgs>(this, args);
        }

        /// <summary>
        /// IsDebuggerBreakpointUpdatedEventSubscribed.
        /// </summary>
        /// <returns>True if event subscription exists.</returns>
        protected bool IsDebuggerBreakpointUpdatedEventSubscribed()
        {
            return (BreakpointUpdated != null);
        }

        #region Runspace Debug Processing

        /// <summary/>
        protected void RaiseStartRunspaceDebugProcessingEvent(StartRunspaceDebugProcessingEventArgs args)
        {
            if (args == null) { throw new PSArgumentNullException(nameof(args)); }

            StartRunspaceDebugProcessing.SafeInvoke<StartRunspaceDebugProcessingEventArgs>(this, args);
        }

        /// <summary/>
        protected void RaiseRunspaceProcessingCompletedEvent(ProcessRunspaceDebugEndEventArgs args)
        {
            if (args == null) { throw new PSArgumentNullException(nameof(args)); }

            RunspaceDebugProcessingCompleted.SafeInvoke<ProcessRunspaceDebugEndEventArgs>(this, args);
        }

        /// <summary/>
        protected bool IsStartRunspaceDebugProcessingEventSubscribed()
        {
            return (StartRunspaceDebugProcessing != null);
        }

        /// <summary/>
        protected void RaiseCancelRunspaceDebugProcessingEvent()
        {
            CancelRunspaceDebugProcessing.SafeInvoke<EventArgs>(this, null);
        }

        #endregion

        #endregion

        #region Public Methods

        /// <summary>
        /// Evaluates provided command either as a debugger specific command
        /// or a PowerShell command.
        /// </summary>
        /// <param name="command">PowerShell command.</param>
        /// <param name="output">Output.</param>
        /// <returns>DebuggerCommandResults.</returns>
        public abstract DebuggerCommandResults ProcessCommand(PSCommand command, PSDataCollection<PSObject> output);

        /// <summary>
        /// Sets the debugger resume action.
        /// </summary>
        /// <param name="resumeAction">DebuggerResumeAction.</param>
        public abstract void SetDebuggerAction(DebuggerResumeAction resumeAction);

        /// <summary>
        /// Stops a running command.
        /// </summary>
        public abstract void StopProcessCommand();

        /// <summary>
        /// Returns current debugger stop event arguments if debugger is in
        /// debug stop state.  Otherwise returns null.
        /// </summary>
        /// <returns>DebuggerStopEventArgs.</returns>
        public abstract DebuggerStopEventArgs GetDebuggerStopArgs();

        /// <summary>
        /// Sets the parent debugger, breakpoints and other debugging context information.
        /// </summary>
        /// <param name="parent">Parent debugger.</param>
        /// <param name="breakPoints">List of breakpoints.</param>
        /// <param name="startAction">Debugger mode.</param>
        /// <param name="host">Host.</param>
        /// <param name="path">Current path.</param>
        public virtual void SetParent(
            Debugger parent,
            IEnumerable<Breakpoint> breakPoints,
            DebuggerResumeAction? startAction,
            PSHost host,
            PathInfo path)
        {
            throw new PSNotImplementedException();
        }

        /// <summary>
        /// Sets the debugger mode.
        /// </summary>
        public virtual void SetDebugMode(DebugModes mode)
        {
            this.DebugMode = mode;
        }

        /// <summary>
        /// Returns IEnumerable of CallStackFrame objects.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<CallStackFrame> GetCallStack()
        {
            return new Collection<CallStackFrame>();
        }

        /// <summary>
        /// Get a breakpoint by id in the current runspace, primarily for Enable/Disable/Remove-PSBreakpoint cmdlets.
        /// </summary>
        /// <param name="id">Id of the breakpoint you want.</param>
        public Breakpoint GetBreakpoint(int id) =>
            GetBreakpoint(id, runspaceId: null);

        /// <summary>
        /// Get a breakpoint by id, primarily for Enable/Disable/Remove-PSBreakpoint cmdlets.
        /// </summary>
        /// <param name="id">Id of the breakpoint you want.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        public virtual Breakpoint GetBreakpoint(int id, int? runspaceId) =>
            throw new PSNotImplementedException();

        /// <summary>
        /// Adds the provided set of breakpoints to the debugger, in the current runspace.
        /// </summary>
        /// <param name="breakpoints">Breakpoints.</param>
        public void SetBreakpoints(IEnumerable<Breakpoint> breakpoints) =>
            SetBreakpoints(breakpoints, runspaceId: null);

        /// <summary>
        /// Adds the provided set of breakpoints to the debugger.
        /// </summary>
        /// <param name="breakpoints">Breakpoints.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with, null being the current runspace.</param>
        public virtual void SetBreakpoints(IEnumerable<Breakpoint> breakpoints, int? runspaceId) =>
            throw new PSNotImplementedException();

        /// <summary>
        /// Returns breakpoints in the current runspace, primarily for the Get-PSBreakpoint cmdlet.
        /// </summary>
        public List<Breakpoint> GetBreakpoints() =>
            GetBreakpoints(runspaceId: null);

        /// <summary>
        /// Returns breakpoints primarily for the Get-PSBreakpoint cmdlet.
        /// </summary>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        public virtual List<Breakpoint> GetBreakpoints(int? runspaceId) =>
            throw new PSNotImplementedException();

        /// <summary>
        /// Sets a command breakpoint in the current runspace in the debugger.
        /// </summary>
        /// <param name="command">The name of the command that will trigger the breakpoint. This value may not be null.</param>
        /// <param name="action">The action to take when the breakpoint is hit. If null, PowerShell will break into the debugger when the breakpoint is hit.</param>
        /// <param name="path">The path to the script file where the breakpoint may be hit. If null, the breakpoint may be hit anywhere the command is invoked.</param>
        /// <returns>The command breakpoint that was set.</returns>
        public CommandBreakpoint SetCommandBreakpoint(string command, ScriptBlock action, string path) =>
            SetCommandBreakpoint(command, action, path, runspaceId: null);

        /// <summary>
        /// Sets a command breakpoint in the debugger.
        /// </summary>
        /// <param name="command">The name of the command that will trigger the breakpoint. This value may not be null.</param>
        /// <param name="action">The action to take when the breakpoint is hit. If null, PowerShell will break into the debugger when the breakpoint is hit.</param>
        /// <param name="path">The path to the script file where the breakpoint may be hit. If null, the breakpoint may be hit anywhere the command is invoked.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A value of null will use the current runspace.</param>
        /// <returns>The command breakpoint that was set.</returns>
        public virtual CommandBreakpoint SetCommandBreakpoint(string command, ScriptBlock action, string path, int? runspaceId) =>
            throw new PSNotImplementedException();

        /// <summary>
        /// Sets a line breakpoint in the current runspace in the debugger.
        /// </summary>
        /// <param name="path">The path to the script file where the breakpoint may be hit. This value may not be null.</param>
        /// <param name="line">The line in the script file where the breakpoint may be hit. This value must be greater than or equal to 1.</param>
        /// <param name="column">The column in the script file where the breakpoint may be hit. If 0, the breakpoint will trigger on any statement on the line.</param>
        /// <param name="action">The action to take when the breakpoint is hit. If null, PowerShell will break into the debugger when the breakpoint is hit.</param>
        /// <returns>The line breakpoint that was set.</returns>
        public LineBreakpoint SetLineBreakpoint(string path, int line, int column, ScriptBlock action) =>
            SetLineBreakpoint(path, line, column, action, runspaceId: null);

        /// <summary>
        /// Sets a line breakpoint in the debugger.
        /// </summary>
        /// <param name="path">The path to the script file where the breakpoint may be hit. This value may not be null.</param>
        /// <param name="line">The line in the script file where the breakpoint may be hit. This value must be greater than or equal to 1.</param>
        /// <param name="column">The column in the script file where the breakpoint may be hit. If 0, the breakpoint will trigger on any statement on the line.</param>
        /// <param name="action">The action to take when the breakpoint is hit. If null, PowerShell will break into the debugger when the breakpoint is hit.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The line breakpoint that was set.</returns>
        public virtual LineBreakpoint SetLineBreakpoint(string path, int line, int column, ScriptBlock action, int? runspaceId) =>
            throw new PSNotImplementedException();

        /// <summary>
        /// Sets a variable breakpoint in the current runspace in the debugger.
        /// </summary>
        /// <param name="variableName">The name of the variable that will trigger the breakpoint. This value may not be null.</param>
        /// <param name="accessMode">The variable access mode that will trigger the breakpoint.</param>
        /// <param name="action">The action to take when the breakpoint is hit. If null, PowerShell will break into the debugger when the breakpoint is hit.</param>
        /// <param name="path">The path to the script file where the breakpoint may be hit. If null, the breakpoint may be hit anywhere the variable is accessed using the specified access mode.</param>
        /// <returns>The variable breakpoint that was set.</returns>
        public VariableBreakpoint SetVariableBreakpoint(string variableName, VariableAccessMode accessMode, ScriptBlock action, string path) =>
            SetVariableBreakpoint(variableName, accessMode, action, path, runspaceId: null);

        /// <summary>
        /// Sets a variable breakpoint in the debugger.
        /// </summary>
        /// <param name="variableName">The name of the variable that will trigger the breakpoint. This value may not be null.</param>
        /// <param name="accessMode">The variable access mode that will trigger the breakpoint.</param>
        /// <param name="action">The action to take when the breakpoint is hit. If null, PowerShell will break into the debugger when the breakpoint is hit.</param>
        /// <param name="path">The path to the script file where the breakpoint may be hit. If null, the breakpoint may be hit anywhere the variable is accessed using the specified access mode.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The variable breakpoint that was set.</returns>
        public virtual VariableBreakpoint SetVariableBreakpoint(string variableName, VariableAccessMode accessMode, ScriptBlock action, string path, int? runspaceId) =>
            throw new PSNotImplementedException();

        /// <summary>
        /// Removes a breakpoint from the debugger in the current runspace.
        /// </summary>
        /// <param name="breakpoint">The breakpoint to remove from the debugger. This value may not be null.</param>
        /// <returns>True if the breakpoint was removed from the debugger; false otherwise.</returns>
        public bool RemoveBreakpoint(Breakpoint breakpoint) =>
            RemoveBreakpoint(breakpoint, runspaceId: null);

        /// <summary>
        /// Removes a breakpoint from the debugger.
        /// </summary>
        /// <param name="breakpoint">The breakpoint to remove from the debugger. This value may not be null.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>True if the breakpoint was removed from the debugger; false otherwise.</returns>
        public virtual bool RemoveBreakpoint(Breakpoint breakpoint, int? runspaceId) =>
            throw new PSNotImplementedException();

        /// <summary>
        /// Enables a breakpoint in the debugger in the current runspace.
        /// </summary>
        /// <param name="breakpoint">The breakpoint to enable in the debugger. This value may not be null.</param>
        /// <returns>The updated breakpoint if it was found; null if the breakpoint was not found in the debugger.</returns>
        public Breakpoint EnableBreakpoint(Breakpoint breakpoint) =>
            EnableBreakpoint(breakpoint, runspaceId: null);

        /// <summary>
        /// Enables a breakpoint in the debugger.
        /// </summary>
        /// <param name="breakpoint">The breakpoint to enable in the debugger. This value may not be null.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The updated breakpoint if it was found; null if the breakpoint was not found in the debugger.</returns>
        public virtual Breakpoint EnableBreakpoint(Breakpoint breakpoint, int? runspaceId) =>
            throw new PSNotImplementedException();

        /// <summary>
        /// Disables a breakpoint in the debugger in the current runspace.
        /// </summary>
        /// <param name="breakpoint">The breakpoint to enable in the debugger. This value may not be null.</param>
        /// <returns>The updated breakpoint if it was found; null if the breakpoint was not found in the debugger.</returns>
        public Breakpoint DisableBreakpoint(Breakpoint breakpoint) =>
            DisableBreakpoint(breakpoint, runspaceId: null);

        /// <summary>
        /// Disables a breakpoint in the debugger.
        /// </summary>
        /// <param name="breakpoint">The breakpoint to enable in the debugger. This value may not be null.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The updated breakpoint if it was found; null if the breakpoint was not found in the debugger.</returns>
        public virtual Breakpoint DisableBreakpoint(Breakpoint breakpoint, int? runspaceId) =>
            throw new PSNotImplementedException();

        /// <summary>
        /// Resets the command processor source information so that it is
        /// updated with latest information on the next debug stop.
        /// </summary>
        public virtual void ResetCommandProcessorSource()
        {
            throw new PSNotImplementedException();
        }

        /// <summary>
        /// Sets debugger stepping mode.
        /// </summary>
        /// <param name="enabled">True if stepping is to be enabled.</param>
        public virtual void SetDebuggerStepMode(bool enabled)
        {
            throw new PSNotImplementedException();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Breaks into the debugger.
        /// </summary>
        /// <param name="triggerObject">The object that triggered the breakpoint, if there is one.</param>
        internal virtual void Break(object triggerObject = null)
        {
            throw new PSNotImplementedException();
        }

        /// <summary>
        /// Returns script position message of current execution stack item.
        /// This is used for WDAC audit mode logging for script information enhancement.
        /// </summary>
        /// <returns>Script position message string.</returns>
        internal virtual string GetCurrentScriptPosition()
        {
            throw new PSNotImplementedException();
        }

        /// <summary>
        /// Passes the debugger command to the internal script debugger command processor.  This
        /// is used internally to handle debugger commands such as list, help, etc.
        /// </summary>
        /// <param name="command">Command string.</param>
        /// <param name="output">Output collection.</param>
        /// <returns>DebuggerCommand containing information on whether and how the command was processed.</returns>
        internal virtual DebuggerCommand InternalProcessCommand(string command, IList<PSObject> output)
        {
            throw new PSNotImplementedException();
        }

        /// <summary>
        /// Creates a source list based on root script debugger source information if available, with
        /// the current source line highlighted.  This is used internally for nested runspace debugging
        /// where the runspace command is run in context of a parent script.
        /// </summary>
        /// <param name="lineNum">Current source line.</param>
        /// <param name="output">Output collection.</param>
        /// <returns>True if source listed successfully.</returns>
        internal virtual bool InternalProcessListCommand(int lineNum, IList<PSObject> output)
        {
            throw new PSNotImplementedException();
        }

        /// <summary>
        /// Sets up debugger to debug provided job or its child jobs.
        /// </summary>
        /// <param name="job">
        /// Job object that is either a debuggable job or a container of
        /// debuggable child jobs.
        /// </param>
        /// <param name="breakAll">
        /// If true, the debugger automatically invokes a break all when it
        /// attaches to the job.
        /// </param>
        internal virtual void DebugJob(Job job, bool breakAll) =>
            throw new PSNotImplementedException();

        /// <summary>
        /// Removes job from debugger job list and pops the its
        /// debugger from the active debugger stack.
        /// </summary>
        /// <param name="job">Job.</param>
        internal virtual void StopDebugJob(Job job)
        {
            throw new PSNotImplementedException();
        }

        /// <summary>
        /// GetActiveDebuggerCallStack.
        /// </summary>
        /// <returns>Array of stack frame objects of active debugger.</returns>
        internal virtual CallStackFrame[] GetActiveDebuggerCallStack()
        {
            throw new PSNotImplementedException();
        }

        /// <summary>
        /// Method to add the provided runspace information to the debugger
        /// for monitoring of debugger events.  This is used to implement nested
        /// debugging of runspaces.
        /// </summary>
        /// <param name="args">PSEntityCreatedRunspaceEventArgs.</param>
        internal virtual void StartMonitoringRunspace(PSMonitorRunspaceInfo args)
        {
            throw new PSNotImplementedException();
        }

        /// <summary>
        /// Method to end the monitoring of a runspace for debugging events.
        /// </summary>
        /// <param name="args">PSEntityCreatedRunspaceEventArgs.</param>
        internal virtual void EndMonitoringRunspace(PSMonitorRunspaceInfo args)
        {
            throw new PSNotImplementedException();
        }

        /// <summary>
        /// If a debug stop event is currently pending then this method will release
        /// the event to continue processing.
        /// </summary>
        internal virtual void ReleaseSavedDebugStop()
        {
            throw new PSNotImplementedException();
        }

        /// <summary>
        /// Sets up debugger to debug provided Runspace in a nested debug session.
        /// </summary>
        /// <param name="runspace">
        /// The runspace to debug.
        /// </param>
        /// <param name="breakAll">
        /// If true, the debugger automatically invokes a break all when it
        /// attaches to the runspace.
        /// </param>
        internal virtual void DebugRunspace(Runspace runspace, bool breakAll) =>
            throw new PSNotImplementedException();

        /// <summary>
        /// Removes the provided Runspace from the nested "active" debugger state.
        /// </summary>
        /// <param name="runspace">Runspace.</param>
        internal virtual void StopDebugRunspace(Runspace runspace)
        {
            throw new PSNotImplementedException();
        }

        /// <summary>
        /// Raises the NestedDebuggingCancelledEvent event.
        /// </summary>
        internal void RaiseNestedDebuggingCancelEvent()
        {
            // Raise event on worker thread.
            Threading.ThreadPool.QueueUserWorkItem(
                (state) =>
                {
                    try
                    {
                        NestedDebuggingCancelledEvent.SafeInvoke<EventArgs>(this, null);
                    }
                    catch (Exception)
                    {
                    }
                });
        }

        #endregion

        #region Runspace Debug Processing Methods

        /// <summary>
        /// Adds the provided Runspace object to the runspace debugger processing queue.
        /// The queue will then raise the StartRunspaceDebugProcessing events for each runspace to allow
        /// a host script debugger implementation to provide an active debugging session.
        /// </summary>
        /// <param name="runspace">Runspace to debug.</param>
        internal virtual void QueueRunspaceForDebug(Runspace runspace)
        {
            throw new PSNotImplementedException();
        }

        /// <summary>
        /// Causes the CancelRunspaceDebugProcessing event to be raised which notifies subscribers that current debugging
        /// sessions should be cancelled.
        /// </summary>
        public virtual void CancelDebuggerProcessing()
        {
            throw new PSNotImplementedException();
        }

        #endregion

        #region Members

        internal const string CannotProcessCommandNotStopped = "Debugger:CannotProcessCommandNotStopped";

        internal const string CannotEnableDebuggerSteppingInvalidMode = "Debugger:CannotEnableDebuggerSteppingInvalidMode";

        private static readonly Guid s_instanceId = new Guid();

        #endregion
    }

    #endregion

    #region ScriptDebugger class

    /// <summary>
    /// Holds the debugging information for a Monad Shell session.
    /// </summary>
    internal sealed class ScriptDebugger : Debugger, IDisposable
    {
        #region constructors

        internal ScriptDebugger(ExecutionContext context)
        {
            _context = context;
            _inBreakpoint = false;
            _idToBreakpoint = new ConcurrentDictionary<int, Breakpoint>();
            // The string key is function context file path. The int key is sequencePoint index.
            _pendingBreakpoints = new ConcurrentDictionary<string, ConcurrentDictionary<int, LineBreakpoint>>(StringComparer.OrdinalIgnoreCase);
            _boundBreakpoints = new ConcurrentDictionary<string, Tuple<WeakReference, ConcurrentDictionary<int, LineBreakpoint>>>(StringComparer.OrdinalIgnoreCase);
            _commandBreakpoints = new ConcurrentDictionary<int, CommandBreakpoint>();
            _variableBreakpoints = new ConcurrentDictionary<string, ConcurrentDictionary<int, VariableBreakpoint>>(StringComparer.OrdinalIgnoreCase);
            _steppingMode = SteppingMode.None;
            _callStack = new CallStackList { _callStackList = new List<CallStackInfo>() };

            _runningJobs = new Dictionary<Guid, PSJobStartEventArgs>();
            _activeDebuggers = new ConcurrentStack<Debugger>();
            _debuggerStopEventArgs = new ConcurrentStack<DebuggerStopEventArgs>();
            _syncObject = new object();
            _syncActiveDebuggerStopObject = new object();

            _runningRunspaces = new Dictionary<Guid, PSMonitorRunspaceInfo>();
        }

        /// <summary>
        /// Static constructor.
        /// </summary>
        static ScriptDebugger()
        {
            s_processDebugPromptMatch = StringUtil.Format(@"""[{0}:", DebuggerStrings.NestedRunspaceDebuggerPromptProcessName);
        }

        #endregion constructors

        #region properties

        /// <summary>
        /// True when debugger is stopped at a breakpoint.
        /// </summary>
        public override bool InBreakpoint
        {
            get
            {
                if (_inBreakpoint)
                {
                    return _inBreakpoint;
                }

                Debugger activeDebugger;
                if (_activeDebuggers.TryPeek(out activeDebugger))
                {
                    return activeDebugger.InBreakpoint;
                }

                return false;
            }
        }

        internal override bool IsPushed
        {
            get { return (!_activeDebuggers.IsEmpty); }
        }

        /// <summary>
        /// Returns true if the debugger is preserving a DebuggerStopEvent
        /// event.  Use ReleaseSavedDebugStop() to allow event to process.
        /// </summary>
        internal override bool IsPendingDebugStopEvent
        {
            get
            {
                return ((_preserveDebugStopEvent != null) && !_preserveDebugStopEvent.IsSet);
            }
        }

        /// <summary>
        /// Returns true if debugger has been set to stepInto mode.
        /// </summary>
        internal override bool IsDebuggerSteppingEnabled
        {
            get
            {
                return ((_context._debuggingMode == (int)InternalDebugMode.Enabled) &&
                        (_currentDebuggerAction == DebuggerResumeAction.StepInto) &&
                        (_steppingMode != SteppingMode.None));
            }
        }

        private bool? _isLocalSession;

        private bool IsLocalSession
        {
            get
            {
                // Remote debug sessions always have a ServerRemoteHost.  Otherwise it is a local session.
                _isLocalSession ??= !((_context.InternalHost.ExternalHost != null) &&
                    (_context.InternalHost.ExternalHost is System.Management.Automation.Remoting.ServerRemoteHost));

                return _isLocalSession.Value;
            }
        }

        /// <summary>
        /// Gets or sets the object that triggered the current breakpoint.
        /// </summary>
        private object TriggerObject { get; set; }

        #endregion properties

        #region internal methods

        #region Reset Debugger

        /// <summary>
        /// Resets debugger to initial state.
        /// </summary>
        internal void ResetDebugger()
        {
            SetDebugMode(DebugModes.None);
            SetInternalDebugMode(InternalDebugMode.Disabled);

            _steppingMode = SteppingMode.None;
            _inBreakpoint = false;
            _idToBreakpoint.Clear();
            _pendingBreakpoints.Clear();
            _boundBreakpoints.Clear();
            _commandBreakpoints.Clear();
            _variableBreakpoints.Clear();
            s_emptyBreakpointList.Clear();
            _callStack.Clear();
            _overOrOutFrame = null;
            _commandProcessor = new DebuggerCommandProcessor();

            _currentInvocationInfo = null;
            _inBreakpoint = false;
            _psDebuggerCommand = null;
            _savedIgnoreScriptDebug = false;
            _isLocalSession = null;

            _nestedDebuggerStop = false;
            _debuggerStopEventArgs.Clear();
            _lastActiveDebuggerAction = DebuggerResumeAction.Continue;
            _currentDebuggerAction = DebuggerResumeAction.Continue;
            _previousDebuggerAction = DebuggerResumeAction.Continue;
            _nestedRunningFrame = null;
            _nestedDebuggerStop = false;
            _processingOutputCount = 0;
            _preserveUnhandledDebugStopEvent = false;

            ClearRunningJobList();
            ClearRunningRunspaceList();
            _activeDebuggers.Clear();

            ReleaseSavedDebugStop();

            SetDebugMode(DebugModes.Default);
        }

        #endregion

        #region Call stack management

        // Called from generated code on entering the script function, called once for each dynamicparam, begin, or end
        // block, and once for each object written to the pipeline.  Also called when entering a trap.
        internal void EnterScriptFunction(FunctionContext functionContext)
        {
            Diagnostics.Assert(functionContext._executionContext == _context, "Wrong debugger is being used.");

            var invocationInfo = (InvocationInfo)functionContext._localsTuple.GetAutomaticVariable(AutomaticVariable.MyInvocation);
            var newCallStackInfo = new CallStackInfo
            {
                InvocationInfo = invocationInfo,
                File = functionContext._file,
                DebuggerStepThrough = functionContext._debuggerStepThrough,
                FunctionContext = functionContext,
                IsFrameHidden = functionContext._debuggerHidden,
            };
            _callStack.Add(newCallStackInfo);

            if (_context._debuggingMode > 0)
            {
                var scriptCommandInfo = invocationInfo.MyCommand as ExternalScriptInfo;
                if (scriptCommandInfo != null)
                {
                    RegisterScriptFile(scriptCommandInfo);
                }

                bool checkLineBp = CheckCommand(invocationInfo);
                SetupBreakpoints(functionContext);

                if (functionContext._debuggerStepThrough && _overOrOutFrame == null && _steppingMode == SteppingMode.StepIn)
                {
                    // Treat like step out, but only if we're not already stepping out
                    ResumeExecution(DebuggerResumeAction.StepOut);
                }

                if (checkLineBp)
                {
                    OnSequencePointHit(functionContext);
                }

                if (_context.PSDebugTraceLevel > 1 && !functionContext._debuggerStepThrough && !functionContext._debuggerHidden)
                {
                    TraceScriptFunctionEntry(functionContext);
                }
            }
        }

        private void SetupBreakpoints(FunctionContext functionContext)
        {
            var scriptDebugData = _mapScriptToBreakpoints.GetValue(functionContext._sequencePoints,
                                                                   _ => Tuple.Create(new Dictionary<int, List<LineBreakpoint>>(),
                                                                                     new BitArray(functionContext._sequencePoints.Length)));
            functionContext._boundBreakpoints = scriptDebugData.Item1;
            functionContext._breakPoints = scriptDebugData.Item2;
            SetPendingBreakpoints(functionContext);
        }

        // Called after exiting the script function, called once for each dynamicparam, begin, or end
        // block, and once for each object written to the pipeline.  Also called when leaving a trap.
        internal void ExitScriptFunction()
        {
            // If it's stepping over to exit the current frame, we need to clear the _overOrOutFrame,
            // so that we will stop at the next statement in the outer frame.
            if (_callStack.Last() == _overOrOutFrame)
            {
                _overOrOutFrame = null;
            }

            _callStack.RemoveAt(_callStack.Count - 1);

            // Don't disable step mode if the user enabled runspace debugging (UnhandledBreakpointMode == Wait)
            if ((_callStack.Count == 0) && (UnhandledBreakpointMode != UnhandledBreakpointProcessingMode.Wait))
            {
                // If we've popped the last entry, don't step into anything else (like prompt, suggestions, etc.)
                _steppingMode = SteppingMode.None;
                _currentDebuggerAction = DebuggerResumeAction.Continue;
                _previousDebuggerAction = DebuggerResumeAction.Continue;
            }
        }

        internal void RegisterScriptFile(ExternalScriptInfo scriptCommandInfo)
        {
            RegisterScriptFile(scriptCommandInfo.Path, scriptCommandInfo.ScriptContents);
        }

        internal void RegisterScriptFile(string path, string scriptContents)
        {
            Tuple<WeakReference, ConcurrentDictionary<int, LineBreakpoint>> boundBreakpoints;
            if (!_boundBreakpoints.TryGetValue(path, out boundBreakpoints))
            {
                _boundBreakpoints[path] = Tuple.Create(new WeakReference(scriptContents), new ConcurrentDictionary<int, LineBreakpoint>());
            }
            else
            {
                // If script contents have changed, or if the file got collected, we must rebind the breakpoints.
                string oldScriptContents;
                boundBreakpoints.Item1.TryGetTarget(out oldScriptContents);
                if (oldScriptContents == null || !oldScriptContents.Equals(scriptContents, StringComparison.Ordinal))
                {
                    UnbindBoundBreakpoints(boundBreakpoints.Item2.Values.ToList());
                    _boundBreakpoints[path] = Tuple.Create(new WeakReference(scriptContents), new ConcurrentDictionary<int, LineBreakpoint>());
                }
            }
        }

        #endregion Call stack management

        #region setting breakpoints

        internal void AddBreakpointCommon(Breakpoint breakpoint)
        {
            if (_context._debuggingMode == 0)
            {
                SetInternalDebugMode(InternalDebugMode.Enabled);
            }

            _idToBreakpoint[breakpoint.Id] = breakpoint;
            OnBreakpointUpdated(new BreakpointUpdatedEventArgs(breakpoint, BreakpointUpdateType.Set, _idToBreakpoint.Count));
        }

        private CommandBreakpoint AddCommandBreakpoint(CommandBreakpoint breakpoint)
        {
            AddBreakpointCommon(breakpoint);
            _commandBreakpoints[breakpoint.Id] = breakpoint;
            return breakpoint;
        }

        private LineBreakpoint AddLineBreakpoint(LineBreakpoint breakpoint)
        {
            AddBreakpointCommon(breakpoint);
            AddPendingBreakpoint(breakpoint);

            return breakpoint;
        }

        private void AddPendingBreakpoint(LineBreakpoint breakpoint)
        {
            _pendingBreakpoints.AddOrUpdate(
                breakpoint.Script,
                new ConcurrentDictionary<int, LineBreakpoint> { [breakpoint.Id] = breakpoint },
                (_, dictionary) => { dictionary.TryAdd(breakpoint.Id, breakpoint); return dictionary; });
        }

        private void AddNewBreakpoint(Breakpoint breakpoint)
        {
            LineBreakpoint lineBreakpoint = breakpoint as LineBreakpoint;
            if (lineBreakpoint != null)
            {
                AddLineBreakpoint(lineBreakpoint);
                return;
            }

            CommandBreakpoint cmdBreakpoint = breakpoint as CommandBreakpoint;
            if (cmdBreakpoint != null)
            {
                AddCommandBreakpoint(cmdBreakpoint);
                return;
            }

            VariableBreakpoint varBreakpoint = breakpoint as VariableBreakpoint;
            if (varBreakpoint != null)
            {
                AddVariableBreakpoint(varBreakpoint);
            }
        }

        internal VariableBreakpoint AddVariableBreakpoint(VariableBreakpoint breakpoint)
        {
            AddBreakpointCommon(breakpoint);

            if (!_variableBreakpoints.TryGetValue(breakpoint.Variable, out ConcurrentDictionary<int, VariableBreakpoint> breakpoints))
            {
                breakpoints = new ConcurrentDictionary<int, VariableBreakpoint>();
                _variableBreakpoints[breakpoint.Variable] = breakpoints;
            }

            breakpoints[breakpoint.Id] = breakpoint;
            return breakpoint;
        }

        private void UpdateBreakpoints(FunctionContext functionContext)
        {
            if (functionContext._breakPoints == null)
            {
                // This should be rare - setting a breakpoint inside a script, but debugger hadn't started.
                SetupBreakpoints(functionContext);
            }
            else
            {
                // Check pending breakpoints to see if any apply to this script.
                if (string.IsNullOrEmpty(functionContext._file))
                {
                    return;
                }

                if (_pendingBreakpoints.TryGetValue(functionContext._file, out var dictionary) && !dictionary.IsEmpty)
                {
                    SetPendingBreakpoints(functionContext);
                }
            }
        }

        /// <summary>
        /// Raises the BreakpointUpdated event.
        /// </summary>
        /// <param name="e"></param>
        private void OnBreakpointUpdated(BreakpointUpdatedEventArgs e)
        {
            RaiseBreakpointUpdatedEvent(e);
        }

        #endregion setting breakpoints

        #region removing breakpoints

        internal bool RemoveVariableBreakpoint(VariableBreakpoint breakpoint) =>
            _variableBreakpoints[breakpoint.Variable].Remove(breakpoint.Id, out _);

        internal bool RemoveCommandBreakpoint(CommandBreakpoint breakpoint) =>
            _commandBreakpoints.Remove(breakpoint.Id, out _);

        internal bool RemoveLineBreakpoint(LineBreakpoint breakpoint)
        {
            bool removed = false;
            if (_pendingBreakpoints.TryGetValue(breakpoint.Script, out var dictionary))
            {
                removed = dictionary.Remove(breakpoint.Id, out _);
            }

            Tuple<WeakReference, ConcurrentDictionary<int, LineBreakpoint>> value;
            if (_boundBreakpoints.TryGetValue(breakpoint.Script, out value))
            {
                removed = value.Item2.Remove(breakpoint.Id, out _);
            }

            return removed;
        }

        #endregion removing breakpoints

        #region finding breakpoints

        // The bit array is used to detect if a breakpoint is set or not for a given scriptblock.  This bit array
        // is checked when hitting sequence points.  Enabling/disabling a line breakpoint is as simple as flipping
        // the bit.
        private readonly ConditionalWeakTable<IScriptExtent[], Tuple<Dictionary<int, List<LineBreakpoint>>, BitArray>> _mapScriptToBreakpoints =
            new ConditionalWeakTable<IScriptExtent[], Tuple<Dictionary<int, List<LineBreakpoint>>, BitArray>>();

        /// <summary>
        /// Checks for command breakpoints.
        /// </summary>
        internal bool CheckCommand(InvocationInfo invocationInfo)
        {
            var functionContext = _callStack.LastFunctionContext();
            if (functionContext != null && functionContext._debuggerHidden)
            {
                // Never stop in DebuggerHidden scripts, don't even call the actions on breakpoints.
                return false;
            }

            List<Breakpoint> breakpoints =
                _commandBreakpoints.Values.Where(bp => bp.Enabled && bp.Trigger(invocationInfo)).ToList<Breakpoint>();

            bool checkLineBp = true;
            if (breakpoints.Count > 0)
            {
                breakpoints = TriggerBreakpoints(breakpoints);
                if (breakpoints.Count > 0)
                {
                    var breakInvocationInfo =
                        functionContext != null
                            ? new InvocationInfo(invocationInfo.MyCommand, functionContext.CurrentPosition)
                            : null;
                    OnDebuggerStop(breakInvocationInfo, breakpoints);
                    checkLineBp = false;
                }
            }

            return checkLineBp;
        }

        internal void CheckVariableRead(string variableName)
        {
            var breakpointsToTrigger = GetVariableBreakpointsToTrigger(variableName, read: true);
            if (breakpointsToTrigger != null && breakpointsToTrigger.Count > 0)
            {
                TriggerVariableBreakpoints(breakpointsToTrigger);
            }
        }

        internal void CheckVariableWrite(string variableName)
        {
            var breakpointsToTrigger = GetVariableBreakpointsToTrigger(variableName, read: false);
            if (breakpointsToTrigger != null && breakpointsToTrigger.Count > 0)
            {
                TriggerVariableBreakpoints(breakpointsToTrigger);
            }
        }

        private List<VariableBreakpoint> GetVariableBreakpointsToTrigger(string variableName, bool read)
        {
            Diagnostics.Assert(_context._debuggingMode == 1, "breakpoints only hit when debugging mode is 1");

            var functionContext = _callStack.LastFunctionContext();
            if (functionContext != null && functionContext._debuggerHidden)
            {
                // Never stop in DebuggerHidden scripts, don't even call the action on any breakpoint.
                return null;
            }

            try
            {
                SetInternalDebugMode(InternalDebugMode.Disabled);

                ConcurrentDictionary<int, VariableBreakpoint> breakpoints;
                if (!_variableBreakpoints.TryGetValue(variableName, out breakpoints))
                {
                    // $PSItem is an alias for $_.  We don't use PSItem internally, but a user might
                    // have set a bp on $PSItem, so look for that if appropriate.
                    if (SpecialVariables.IsUnderbar(variableName))
                    {
                        _variableBreakpoints.TryGetValue(SpecialVariables.PSItem, out breakpoints);
                    }
                }

                if (breakpoints == null)
                    return null;

                var callStackInfo = _callStack.Last();
                var currentScriptFile = callStackInfo?.File;
                return breakpoints.Values.Where(bp => bp.Trigger(currentScriptFile, read: read)).ToList();
            }
            finally
            {
                SetInternalDebugMode(InternalDebugMode.Enabled);
            }
        }

        internal void TriggerVariableBreakpoints(List<VariableBreakpoint> breakpoints)
        {
            var functionContext = _callStack.LastFunctionContext();
            var invocationInfo = functionContext != null ? new InvocationInfo(null, functionContext.CurrentPosition, _context) : null;
            OnDebuggerStop(invocationInfo, breakpoints.ToList<Breakpoint>());
        }

        // Return the line breakpoints bound in a specific script block (used when a sequence point
        // is hit, to find which breakpoints are set on that sequence point.)
        internal Dictionary<int, List<LineBreakpoint>> GetBoundBreakpoints(IScriptExtent[] sequencePoints)
        {
            Tuple<Dictionary<int, List<LineBreakpoint>>, BitArray> tuple;
            if (_mapScriptToBreakpoints.TryGetValue(sequencePoints, out tuple))
            {
                return tuple.Item1;
            }

            return null;
        }

        #endregion finding breakpoints

        #region triggering breakpoints

        private List<Breakpoint> TriggerBreakpoints(List<Breakpoint> breakpoints)
        {
            Diagnostics.Assert(_context._debuggingMode == 1, "breakpoints only hit when debugging mode == 1");

            List<Breakpoint> breaks = new List<Breakpoint>();
            try
            {
                SetInternalDebugMode(InternalDebugMode.InScriptStop);
                foreach (Breakpoint breakpoint in breakpoints)
                {
                    if (breakpoint.Enabled)
                    {
                        try
                        {
                            // Ensure that code being processed during breakpoint triggers
                            // act like they are broken into the debugger.
                            _inBreakpoint = true;
                            if (breakpoint.Trigger() == Breakpoint.BreakpointAction.Break)
                            {
                                breaks.Add(breakpoint);
                            }
                        }
                        finally
                        {
                            _inBreakpoint = false;
                        }
                    }
                }
            }
            finally
            {
                SetInternalDebugMode(InternalDebugMode.Enabled);
            }

            return breaks;
        }

        internal void OnSequencePointHit(FunctionContext functionContext)
        {
            // TraceLine uses ColumnNumber and expects it to be 1 based. For
            // extents added by the engine and not user code the value can be
            // set to 0 causing an exception. This skips those types of extents
            // as tracing them wouldn't be useful for the end user anyway.
            if (_context.ShouldTraceStatement &&
                !_callStack.Last().IsFrameHidden &&
                !functionContext._debuggerStepThrough &&
                functionContext.CurrentPosition is not EmptyScriptExtent &&
                (functionContext.CurrentPosition is InternalScriptExtent ||
                   functionContext.CurrentPosition.StartColumnNumber > 0))
            {
                TraceLine(functionContext.CurrentPosition);
            }

            // If a nested debugger received a stop debug command then all debugging
            // should stop.
            if (_nestedDebuggerStop)
            {
                _nestedDebuggerStop = false;
                _currentDebuggerAction = DebuggerResumeAction.Continue;
                ResumeExecution(DebuggerResumeAction.Stop);
            }

            UpdateBreakpoints(functionContext);

            if (_steppingMode == SteppingMode.StepIn &&
                (_overOrOutFrame == null || _callStack.Last() == _overOrOutFrame))
            {
                if (!_callStack.Last().IsFrameHidden)
                {
                    _overOrOutFrame = null;
                    StopOnSequencePoint(functionContext, s_emptyBreakpointList);
                }
                else if (_overOrOutFrame == null)
                {
                    // Treat like step out, but only if we're not already stepping out
                    ResumeExecution(DebuggerResumeAction.StepOut);
                }
            }
            else
            {
                if (functionContext._breakPoints[functionContext._currentSequencePointIndex])
                {
                    if (functionContext._boundBreakpoints.TryGetValue(functionContext._currentSequencePointIndex, out var sequencePointBreakpoints))
                    {
                        var enabledBreakpoints = new List<Breakpoint>();
                        foreach (Breakpoint breakpoint in sequencePointBreakpoints)
                        {
                            if (breakpoint.Enabled)
                            {
                                enabledBreakpoints.Add(breakpoint);
                            }
                        }

                        if (enabledBreakpoints.Count > 0)
                        {
                            enabledBreakpoints = TriggerBreakpoints(enabledBreakpoints);
                            if (enabledBreakpoints.Count > 0)
                            {
                                StopOnSequencePoint(functionContext, enabledBreakpoints);
                            }
                        }
                    }
                }
            }
        }

        #endregion triggering breakpoints

        #endregion internal methods

        #region private members

        [DebuggerDisplay("{FunctionContext.CurrentPosition}")]
        private sealed class CallStackInfo
        {
            internal InvocationInfo InvocationInfo { get; set; }

            internal string File { get; set; }

            internal bool DebuggerStepThrough { get; set; }

            internal FunctionContext FunctionContext { get; set; }

            /// <summary>
            /// The frame is hidden due to the <see cref="DebuggerHiddenAttribute"/> attribute.
            /// No breakpoints will be set and no stepping in/through.
            /// </summary>
            internal bool IsFrameHidden { get; set; }

            internal bool TopFrameAtBreakpoint { get; set; }
        }

        private struct CallStackList
        {
            internal List<CallStackInfo> _callStackList;

            internal void Add(CallStackInfo item)
            {
                lock (_callStackList)
                {
                    _callStackList.Add(item);
                }
            }

            internal void RemoveAt(int index)
            {
                lock (_callStackList)
                {
                    _callStackList.RemoveAt(index);
                }
            }

            internal CallStackInfo this[int index]
            {
                get
                {
                    lock (_callStackList)
                    {
                        return ((index > -1) && (index < _callStackList.Count)) ? _callStackList[index] : null;
                    }
                }
            }

            internal CallStackInfo Last()
            {
                lock (_callStackList)
                {
                    return (_callStackList.Count > 0) ? _callStackList[_callStackList.Count - 1] : null;
                }
            }

            internal FunctionContext LastFunctionContext()
            {
                var last = Last();
                return last?.FunctionContext;
            }

            internal bool Any()
            {
                lock (_callStackList)
                {
                    return _callStackList.Count > 0;
                }
            }

            internal int Count
            {
                get
                {
                    lock (_callStackList)
                    {
                        return _callStackList.Count;
                    }
                }
            }

            internal CallStackInfo[] ToArray()
            {
                lock (_callStackList)
                {
                    return _callStackList.ToArray();
                }
            }

            internal void Clear()
            {
                lock (_callStackList)
                {
                    _callStackList.Clear();
                }
            }
        }

        private readonly ExecutionContext _context;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, LineBreakpoint>> _pendingBreakpoints;
        private readonly ConcurrentDictionary<string, Tuple<WeakReference, ConcurrentDictionary<int, LineBreakpoint>>> _boundBreakpoints;
        private readonly ConcurrentDictionary<int, CommandBreakpoint> _commandBreakpoints;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, VariableBreakpoint>> _variableBreakpoints;
        private readonly ConcurrentDictionary<int, Breakpoint> _idToBreakpoint;
        private SteppingMode _steppingMode;
        private CallStackInfo _overOrOutFrame;
        private CallStackList _callStack;
        private static readonly List<Breakpoint> s_emptyBreakpointList = new List<Breakpoint>();

        private DebuggerCommandProcessor _commandProcessor = new DebuggerCommandProcessor();
        private InvocationInfo _currentInvocationInfo;
        private bool _inBreakpoint;
        private PowerShell _psDebuggerCommand;

        // Job debugger integration.
        private bool _nestedDebuggerStop;
        private readonly Dictionary<Guid, PSJobStartEventArgs> _runningJobs;
        private readonly ConcurrentStack<Debugger> _activeDebuggers;
        private readonly ConcurrentStack<DebuggerStopEventArgs> _debuggerStopEventArgs;
        private DebuggerResumeAction _lastActiveDebuggerAction;
        private DebuggerResumeAction _currentDebuggerAction;
        private DebuggerResumeAction _previousDebuggerAction;
        private CallStackInfo _nestedRunningFrame;
        private readonly object _syncObject;
        private readonly object _syncActiveDebuggerStopObject;
        private int _processingOutputCount;
        private ManualResetEventSlim _processingOutputCompleteEvent = new ManualResetEventSlim(true);

        // Runspace debugger integration.
        private readonly Dictionary<Guid, PSMonitorRunspaceInfo> _runningRunspaces;

        private const int _jobCallStackOffset = 2;
        private const int _runspaceCallStackOffset = 1;

        private bool _preserveUnhandledDebugStopEvent;
        private ManualResetEventSlim _preserveDebugStopEvent;

        // Process runspace debugger
        private readonly Lazy<ConcurrentQueue<StartRunspaceDebugProcessingEventArgs>> _runspaceDebugQueue = new Lazy<ConcurrentQueue<StartRunspaceDebugProcessingEventArgs>>();
        private volatile int _processingRunspaceDebugQueue;
        private ManualResetEventSlim _runspaceDebugCompleteEvent;

        // System is locked down when true. Used to disable debugger on lock down.
        private bool? _isSystemLockedDown;

        private static readonly string s_processDebugPromptMatch;

        #endregion private members

        #region private methods

        /// <summary>
        /// Raises the DebuggerStop event.
        /// </summary>
        private void OnDebuggerStop(InvocationInfo invocationInfo, List<Breakpoint> breakpoints)
        {
            Diagnostics.Assert(breakpoints != null, "The list of breakpoints should not be null");

            LocalRunspace localRunspace = _context.CurrentRunspace as LocalRunspace;

            Diagnostics.Assert(localRunspace != null, "Debugging is only supported on local runspaces");

            if (localRunspace.PulsePipeline != null && localRunspace.PulsePipeline == localRunspace.GetCurrentlyRunningPipeline())
            {
                _context.EngineHostInterface.UI.WriteWarningLine(
                    breakpoints.Count > 0
                        ? string.Format(CultureInfo.CurrentCulture, DebuggerStrings.WarningBreakpointWillNotBeHit,
                                        breakpoints[0])
                        : new InvalidOperationException().Message);
                return;
            }

            _currentInvocationInfo = invocationInfo;
            _steppingMode = SteppingMode.None;

            // Optionally wait for a debug stop event subscriber if requested.
            _inBreakpoint = true;
            if (!WaitForDebugStopSubscriber())
            {
                // No subscriber.  Ignore this debug stop event.
                _inBreakpoint = false;
                return;
            }

            bool oldQuestionMarkVariableValue = _context.QuestionMarkVariableValue;

            _context.SetVariable(SpecialVariables.PSDebugContextVarPath, new PSDebugContext(invocationInfo, breakpoints, TriggerObject));

            FunctionInfo defaultPromptInfo = null;
            string originalPromptString = null;
            bool hadDefaultPrompt = false;

            try
            {
                Collection<PSObject> items = _context.SessionState.InvokeProvider.Item.Get("function:\\prompt");
                if ((items != null) && (items.Count > 0))
                {
                    defaultPromptInfo = items[0].BaseObject as FunctionInfo;
                    originalPromptString = defaultPromptInfo.Definition as string;

                    if (originalPromptString.Equals(InitialSessionState.DefaultPromptFunctionText, StringComparison.OrdinalIgnoreCase) ||
                        originalPromptString.Trim().StartsWith(s_processDebugPromptMatch, StringComparison.OrdinalIgnoreCase))
                    {
                        hadDefaultPrompt = true;
                    }
                }
            }
            catch (ItemNotFoundException)
            {
                // Ignore, it means they don't have the default prompt
            }

            // Change the context language mode before updating the prompt script.
            // This way the new prompt scriptblock will pick up the current context language mode.
            PSLanguageMode? originalLanguageMode = null;
            if (_context.UseFullLanguageModeInDebugger &&
                (_context.LanguageMode != PSLanguageMode.FullLanguage))
            {
                originalLanguageMode = _context.LanguageMode;
                _context.LanguageMode = PSLanguageMode.FullLanguage;
            }

            // Update the prompt to the debug prompt
            if (hadDefaultPrompt)
            {
                int index = originalPromptString.IndexOf('"', StringComparison.OrdinalIgnoreCase);
                if (index > -1)
                {
                    // Fix up prompt.
                    ++index;
                    string debugPrompt = string.Concat("\"[DBG]: ", originalPromptString.AsSpan(index, originalPromptString.Length - index));

                    defaultPromptInfo.Update(
                        ScriptBlock.Create(debugPrompt), true, ScopedItemOptions.Unspecified);
                }
                else
                {
                    hadDefaultPrompt = false;
                }
            }

            RunspaceAvailability previousAvailability = _context.CurrentRunspace.RunspaceAvailability;

            _context.CurrentRunspace.UpdateRunspaceAvailability(
                _context.CurrentRunspace.GetCurrentlyRunningPipeline() != null
                    ? RunspaceAvailability.AvailableForNestedCommand
                    : RunspaceAvailability.Available,
                true);

            Diagnostics.Assert(_context._debuggingMode == 1, "Should only be stopping when debugger is on.");

            try
            {
                SetInternalDebugMode(InternalDebugMode.InScriptStop);
                if (_callStack.Any())
                {
                    // Get-PSCallStack shouldn't report any frames above this frame, so mark it.  One alternative
                    // to marking the frame would be to not push new frames while debugging, but that limits our
                    // ability to give a full callstack if there are errors during eval.
                    _callStack.Last().TopFrameAtBreakpoint = true;
                }

                // Reset list lines.
                _commandProcessor.Reset();

                // Save a copy of the stop arguments.
                DebuggerStopEventArgs copyArgs = new DebuggerStopEventArgs(invocationInfo, breakpoints);
                _debuggerStopEventArgs.Push(copyArgs);

                // Blocking call to raise debugger stop event.
                DebuggerStopEventArgs e = new DebuggerStopEventArgs(invocationInfo, breakpoints);
                RaiseDebuggerStopEvent(e);
                ResumeExecution(e.ResumeAction);
            }
            finally
            {
                SetInternalDebugMode(InternalDebugMode.Enabled);
                if (_callStack.Any())
                {
                    _callStack.Last().TopFrameAtBreakpoint = false;
                }

                _context.CurrentRunspace.UpdateRunspaceAvailability(previousAvailability, true);

                if (originalLanguageMode.HasValue)
                {
                    _context.LanguageMode = originalLanguageMode.Value;
                }

                _context.EngineSessionState.RemoveVariable(SpecialVariables.PSDebugContext);

                if (hadDefaultPrompt)
                {
                    // Restore the prompt if they had our default
                    defaultPromptInfo.Update(
                        ScriptBlock.Create(originalPromptString), true, ScopedItemOptions.Unspecified);
                }

                DebuggerStopEventArgs oldArgs;
                _debuggerStopEventArgs.TryPop(out oldArgs);

                _context.QuestionMarkVariableValue = oldQuestionMarkVariableValue;

                _inBreakpoint = false;
            }
        }

        /// <summary>
        /// Resumes execution after a breakpoint/step event has been handled.
        /// </summary>
        private void ResumeExecution(DebuggerResumeAction action)
        {
            _previousDebuggerAction = _currentDebuggerAction;
            _currentDebuggerAction = action;

            switch (action)
            {
                case DebuggerResumeAction.StepInto:
                    _steppingMode = SteppingMode.StepIn;
                    _overOrOutFrame = null;
                    break;

                case DebuggerResumeAction.StepOut:
                    if (_callStack.Count > 1)
                    {
                        // When we pop to the parent frame, we'll clear _overOrOutFrame (so OnSequencePointHit
                        // will stop) and continue with a step.
                        _steppingMode = SteppingMode.StepIn;
                        _overOrOutFrame = _callStack[_callStack.Count - 2];
                    }
                    else
                    {
                        // Stepping out of the top frame is just like continue (allow hitting
                        // breakpoints in the current frame, but otherwise just go.)
                        goto case DebuggerResumeAction.Continue;
                    }

                    break;

                case DebuggerResumeAction.StepOver:
                    _steppingMode = SteppingMode.StepIn;
                    _overOrOutFrame = _callStack.Last();
                    break;

                case DebuggerResumeAction.Continue:
                    // nothing to do, just continue
                    _steppingMode = SteppingMode.None;
                    _overOrOutFrame = null;
                    break;

                case DebuggerResumeAction.Stop:
                    _steppingMode = SteppingMode.None;
                    _overOrOutFrame = null;
                    throw new TerminateException();

                default:
                    Debug.Fail("Received an unknown action: " + action);
                    break;
            }
        }

        /// <summary>
        /// Blocking call that blocks until a release occurs via ReleaseSavedDebugStop().
        /// </summary>
        /// <returns>True if there is a DebuggerStop event subscriber.</returns>
        private bool WaitForDebugStopSubscriber()
        {
            if (!IsDebuggerStopEventSubscribed())
            {
                if (_preserveUnhandledDebugStopEvent)
                {
                    // Lazily create the event object.
                    _preserveDebugStopEvent ??= new ManualResetEventSlim(true);

                    // Set the event handle to non-signaled.
                    if (!_preserveDebugStopEvent.IsSet)
                    {
                        Diagnostics.Assert(false, "The _preserveDebugStop event handle should always be in the signaled state at this point.");
                        return false;
                    }

                    _preserveDebugStopEvent.Reset();

                    // Wait indefinitely for a signal event.
                    _preserveDebugStopEvent.Wait();

                    return IsDebuggerStopEventSubscribed();
                }

                return false;
            }

            return true;
        }

        private enum SteppingMode
        {
            StepIn,
            None
        }

        // When a script file changes, we need to rebind all breakpoints in that script.
        private void UnbindBoundBreakpoints(List<LineBreakpoint> boundBreakpoints)
        {
            foreach (var breakpoint in boundBreakpoints)
            {
                // Also remove unbound breakpoints from the script to breakpoint map.
                Tuple<Dictionary<int, List<LineBreakpoint>>, BitArray> lineBreakTuple;
                if (_mapScriptToBreakpoints.TryGetValue(breakpoint.SequencePoints, out lineBreakTuple))
                {
                    if (lineBreakTuple.Item1.TryGetValue(breakpoint.SequencePointIndex, out var lineBreakList))
                    {
                        lineBreakList.Remove(breakpoint);
                    }
                }

                breakpoint.SequencePoints = null;
                breakpoint.SequencePointIndex = -1;
                breakpoint.BreakpointBitArray = null;

                AddPendingBreakpoint(breakpoint);
            }

            boundBreakpoints.Clear();
        }

        private void SetPendingBreakpoints(FunctionContext functionContext)
        {
            var currentScriptFile = functionContext._file;

            // If we're not in a file, we can't have any line breakpoints.
            if (currentScriptFile == null)
                return;

            if (!_pendingBreakpoints.TryGetValue(currentScriptFile, out var breakpoints) || breakpoints.IsEmpty)
            {
                return;
            }

            // Normally we register a script file when the script is run or the module is imported,
            // but if there weren't any breakpoints when the script was run and the script was dotted,
            // we will end up here with pending breakpoints, but we won't have cached the list of
            // breakpoints in the script.
            RegisterScriptFile(currentScriptFile, functionContext.CurrentPosition.StartScriptPosition.GetFullScript());

            Tuple<Dictionary<int, List<LineBreakpoint>>, BitArray> tuple;
            if (!_mapScriptToBreakpoints.TryGetValue(functionContext._sequencePoints, out tuple))
            {
                Diagnostics.Assert(false, "If the script block is still alive, the entry should not be collected.");
            }

            Diagnostics.Assert(tuple.Item1 == functionContext._boundBreakpoints, "What's up?");

            foreach ((int breakpointId, LineBreakpoint breakpoint) in breakpoints)
            {
                bool bound = false;
                if (breakpoint.TrySetBreakpoint(currentScriptFile, functionContext))
                {
                    if (_context._debuggingMode == 0)
                    {
                        SetInternalDebugMode(InternalDebugMode.Enabled);
                    }

                    bound = true;

                    if (tuple.Item1.TryGetValue(breakpoint.SequencePointIndex, out var list))
                    {
                        list.Add(breakpoint);
                    }
                    else
                    {
                        tuple.Item1.Add(breakpoint.SequencePointIndex, new List<LineBreakpoint> { breakpoint });
                    }

                    // We need to keep track of any breakpoints that are bound in each script because they may
                    // need to be rebound if the script changes.
                    var boundBreakpoints = _boundBreakpoints[currentScriptFile].Item2;
                    boundBreakpoints[breakpoint.Id] = breakpoint;
                }

                if (bound)
                {
                    breakpoints.TryRemove(breakpointId, out _);
                }
            }

            // Here could check if all breakpoints for the current functionContext were bound, but because there is no atomic
            // api for conditional removal we either need to lock, or do some trickery that has possibility of race conditions.
            // Instead we keep the item in the dictionary with 0 breakpoint count. This should not be a big issue,
            // because it is single entry per file that had breakpoints, so there won't be thousands of files in a session.
        }

        private void StopOnSequencePoint(FunctionContext functionContext, List<Breakpoint> breakpoints)
        {
            if (functionContext._debuggerHidden)
            {
                // Never stop in a DebuggerHidden scriptblock.
                return;
            }

            if (functionContext._sequencePoints.Length == 1 &&
                functionContext._scriptBlock != null &&
                object.ReferenceEquals(functionContext._sequencePoints[0], functionContext._scriptBlock.Ast.Extent))
            {
                // If the script is empty or only defines functions, we used the script block extent as a sequence point, but that
                // was only intended for error reporting, it was not meant to be hit as a breakpoint.
                return;
            }

            var invocationInfo = new InvocationInfo(null, functionContext.CurrentPosition, _context);
            OnDebuggerStop(invocationInfo, breakpoints);
        }

        private enum InternalDebugMode
        {
            InPushedStop = -2,
            InScriptStop = -1,
            Disabled = 0,
            Enabled = 1
        }

        /// <summary>
        /// Sets the internal Execution context debug mode given the
        /// current DebugMode setting.
        /// </summary>
        /// <param name="mode">Internal debug mode.</param>
        private void SetInternalDebugMode(InternalDebugMode mode)
        {
            lock (_syncObject)
            {
                // Disable script debugger when in system lock down mode
                if (IsSystemLockedDown)
                {
                    if (_context._debuggingMode != (int)InternalDebugMode.Disabled)
                    {
                        _context._debuggingMode = (int)InternalDebugMode.Disabled;
                    }

                    return;
                }

                switch (mode)
                {
                    case InternalDebugMode.InPushedStop:
                    case InternalDebugMode.InScriptStop:
                    case InternalDebugMode.Disabled:
                        _context._debuggingMode = (int)mode;
                        break;

                    case InternalDebugMode.Enabled:
                        _context._debuggingMode = CanEnableDebugger ?
                            (int)InternalDebugMode.Enabled : (int)InternalDebugMode.Disabled;
                        break;
                }
            }
        }

        private bool CanEnableDebugger
        {
            get
            {
                // The debugger can be enabled if we are not in DebugMode.None and if we are
                // not in a local session set only to RemoteScript.
                return !((DebugMode == DebugModes.RemoteScript) && IsLocalSession) && (DebugMode != DebugModes.None);
            }
        }

        private bool CanDisableDebugger
        {
            get
            {
                // The debugger can be disabled if there are no breakpoints
                // left and if we are not currently stepping in the debugger.
                return _idToBreakpoint.IsEmpty &&
                       _currentDebuggerAction != DebuggerResumeAction.StepInto &&
                       _currentDebuggerAction != DebuggerResumeAction.StepOver &&
                       _currentDebuggerAction != DebuggerResumeAction.StepOut;
            }
        }

        private bool IsSystemLockedDown
        {
            get
            {
                if (_isSystemLockedDown == null)
                {
                    lock (_syncObject)
                    {
                        _isSystemLockedDown ??= (System.Management.Automation.Security.SystemPolicy.GetSystemLockdownPolicy() ==
                            System.Management.Automation.Security.SystemEnforcementMode.Enforce);
                    }
                }

                return _isSystemLockedDown.Value;
            }
        }

        private void CheckForBreakpointSupport()
        {
            if (IsSystemLockedDown)
            {
                // Local script debugging is not supported in locked down mode
                throw new PSNotSupportedException();
            }
        }

        #region Enable debug stepping

        [Flags]
        private enum EnableNestedType
        {
            None = 0x0,
            NestedJob = 0x1,
            NestedRunspace = 0x2
        }

        private void EnableDebuggerStepping(EnableNestedType nestedType)
        {
            if (DebugMode == DebugModes.None)
            {
                throw new PSInvalidOperationException(
                    DebuggerStrings.CannotEnableDebuggerSteppingInvalidMode,
                    null,
                    Debugger.CannotEnableDebuggerSteppingInvalidMode,
                    ErrorCategory.InvalidOperation,
                    null);
            }

            lock (_syncObject)
            {
                if (_context._debuggingMode == 0)
                {
                    SetInternalDebugMode(InternalDebugMode.Enabled);
                }

                Debugger activeDebugger;
                if (_activeDebuggers.TryPeek(out activeDebugger))
                {
                    // Set active debugger to StepInto mode.
                    activeDebugger.SetDebugMode(DebugModes.LocalScript | DebugModes.RemoteScript);
                    activeDebugger.SetDebuggerStepMode(true);
                }
                else
                {
                    // Set script debugger to StepInto mode.
                    ResumeExecution(DebuggerResumeAction.StepInto);
                }
            }

            // Look for any runspaces with debuggers and set to setp mode.
            if ((nestedType & EnableNestedType.NestedRunspace) == EnableNestedType.NestedRunspace)
            {
                SetRunspaceListToStep(true);
            }
        }

        /// <summary>
        /// Restores debugger back to non-step mode.
        /// </summary>
        private void DisableDebuggerStepping()
        {
            if (!IsDebuggerSteppingEnabled) { return; }

            lock (_syncObject)
            {
                ResumeExecution(DebuggerResumeAction.Continue);
                RestoreInternalDebugMode();

                Debugger activeDebugger;
                if (_activeDebuggers.TryPeek(out activeDebugger))
                {
                    activeDebugger.SetDebuggerStepMode(false);
                }
            }

            SetRunningJobListToStep(false);
            SetRunspaceListToStep(false);
        }

        private void RestoreInternalDebugMode()
        {
            InternalDebugMode restoreMode = ((DebugMode != DebugModes.None) && (!_idToBreakpoint.IsEmpty)) ? InternalDebugMode.Enabled : InternalDebugMode.Disabled;
            SetInternalDebugMode(restoreMode);
        }

        #endregion

        #endregion

        #region Debugger Overrides

        /// <summary>
        /// Set ScriptDebugger action.
        /// </summary>
        /// <param name="resumeAction">DebuggerResumeAction.</param>
        public override void SetDebuggerAction(DebuggerResumeAction resumeAction)
        {
            throw new PSNotSupportedException(
                StringUtil.Format(DebuggerStrings.CannotSetDebuggerAction));
        }

        /// <summary>
        /// GetDebuggerStopped.
        /// </summary>
        /// <returns>DebuggerStopEventArgs.</returns>
        public override DebuggerStopEventArgs GetDebuggerStopArgs()
        {
            DebuggerStopEventArgs rtnArgs;
            if (_debuggerStopEventArgs.TryPeek(out rtnArgs))
            {
                return rtnArgs;
            }

            return null;
        }

        /// <summary>
        /// ProcessCommand.
        /// </summary>
        /// <param name="command">PowerShell command.</param>
        /// <param name="output">Output.</param>
        /// <returns>DebuggerCommandResults.</returns>
        public override DebuggerCommandResults ProcessCommand(PSCommand command, PSDataCollection<PSObject> output)
        {
            if (command == null)
            {
                throw new PSArgumentNullException(nameof(command));
            }

            if (output == null)
            {
                throw new PSArgumentNullException(nameof(output));
            }

            if (!DebuggerStopped)
            {
                throw new PSInvalidOperationException(
                    DebuggerStrings.CannotProcessDebuggerCommandNotStopped,
                    null,
                    Debugger.CannotProcessCommandNotStopped,
                    ErrorCategory.InvalidOperation,
                    null);
            }

            //
            // Allow an active pushed debugger to process commands
            //
            DebuggerCommandResults results = ProcessCommandForActiveDebugger(command, output);
            if (results != null)
            {
                return results;
            }

            //
            // Otherwise let root script debugger handle it.
            //
            if (_context.CurrentRunspace is not LocalRunspace localRunspace)
            {
                throw new PSInvalidOperationException(
                    DebuggerStrings.CannotProcessDebuggerCommandNotStopped,
                    null,
                    Debugger.CannotProcessCommandNotStopped,
                    ErrorCategory.InvalidOperation,
                    null);
            }

            try
            {
                using (_psDebuggerCommand = PowerShell.Create())
                {
                    if (localRunspace.GetCurrentlyRunningPipeline() != null)
                    {
                        _psDebuggerCommand.SetIsNested(true);
                    }

                    _psDebuggerCommand.Runspace = localRunspace;
                    _psDebuggerCommand.Commands = command;
                    foreach (var cmd in _psDebuggerCommand.Commands.Commands)
                    {
                        cmd.MergeMyResults(PipelineResultTypes.All, PipelineResultTypes.Output);
                    }

                    PSDataCollection<PSObject> internalOutput = new PSDataCollection<PSObject>();
                    internalOutput.DataAdded += (sender, args) =>
                        {
                            foreach (var item in internalOutput.ReadAll())
                            {
                                if (item == null) { continue; }

                                DebuggerCommand dbgCmd = item.BaseObject as DebuggerCommand;
                                if (dbgCmd != null)
                                {
                                    bool executedByDebugger = (dbgCmd.ResumeAction != null || dbgCmd.ExecutedByDebugger);
                                    results = new DebuggerCommandResults(dbgCmd.ResumeAction, executedByDebugger);
                                }
                                else
                                {
                                    output.Add(item);
                                }
                            }
                        };

                    // Allow any exceptions to propagate.
                    _psDebuggerCommand.InvokeWithDebugger(null, internalOutput, null, false);
                }
            }
            finally
            {
                _psDebuggerCommand = null;
            }

            return results ?? new DebuggerCommandResults(null, false);
        }

        /// <summary>
        /// StopProcessCommand.
        /// </summary>
        public override void StopProcessCommand()
        {
            //
            // If we have a pushed debugger then stop that command.
            //
            if (StopCommandForActiveDebugger())
            {
                return;
            }

            PowerShell ps = _psDebuggerCommand;
            ps?.BeginStop(null, null);
        }

        /// <summary>
        /// Set debug mode.
        /// </summary>
        /// <param name="mode"></param>
        public override void SetDebugMode(DebugModes mode)
        {
            lock (_syncObject)
            {
                // Restrict local script debugger mode when in system lock down.
                // DebugModes enum flags provide a combination of values.  To disable local script debugging
                // we have to disallow 'LocalScript' and 'Default' flags and only allow 'None' or 'RemoteScript'
                // flags exclusively.  This allows only no debugging 'None' or remote debugging 'RemoteScript'.
                if (IsSystemLockedDown && (mode != DebugModes.None) && (mode != DebugModes.RemoteScript))
                {
                    mode = DebugModes.RemoteScript;
                }

                base.SetDebugMode(mode);

                if (!CanEnableDebugger)
                {
                    SetInternalDebugMode(InternalDebugMode.Disabled);
                }
                else if ((!_idToBreakpoint.IsEmpty) && (_context._debuggingMode == 0))
                {
                    // Set internal debugger to active.
                    SetInternalDebugMode(InternalDebugMode.Enabled);
                }
            }
        }

        /// <summary>
        /// Returns current call stack.
        /// </summary>
        /// <returns>IEnumerable of CallStackFrame objects.</returns>
        public override IEnumerable<CallStackFrame> GetCallStack()
        {
            CallStackInfo[] callStack = _callStack.ToArray();

            if (callStack.Length > 0)
            {
                int startingIndex = callStack.Length - 1;
                for (int i = startingIndex; i >= 0; i--)
                {
                    if (callStack[i].TopFrameAtBreakpoint)
                    {
                        startingIndex = i;
                        break;
                    }
                }

                for (int i = startingIndex; i >= 0; i--)
                {
                    var funcContext = callStack[i].FunctionContext;

                    yield return new CallStackFrame(funcContext, callStack[i].InvocationInfo);
                }
            }
        }

        /// <summary>
        /// True when debugger is active with breakpoints.
        /// </summary>
        public override bool IsActive
        {
            get
            {
                int debuggerState = _context._debuggingMode;
                return (debuggerState != 0);
            }
        }

        /// <summary>
        /// Resets the command processor source information so that it is
        /// updated with latest information on the next debug stop.
        /// </summary>
        public override void ResetCommandProcessorSource()
        {
            _commandProcessor.Reset();
        }

        /// <summary>
        /// Sets debugger stepping mode.
        /// </summary>
        /// <param name="enabled">True if stepping is to be enabled.</param>
        public override void SetDebuggerStepMode(bool enabled)
        {
            if (enabled)
            {
                EnableDebuggerStepping(EnableNestedType.NestedJob | EnableNestedType.NestedRunspace);
            }
            else
            {
                DisableDebuggerStepping();
            }
        }

        /// <summary>
        /// Breaks into the debugger.
        /// </summary>
        /// <param name="triggerObject">The object that triggered the breakpoint, if there is one.</param>
        internal override void Break(object triggerObject = null)
        {
            if (!IsDebugHandlerSubscribed &&
                (UnhandledBreakpointMode == UnhandledBreakpointProcessingMode.Ignore))
            {
                // No debugger attached and runspace debugging is not enabled.  Enable runspace debugging here
                // so that this command is effective.
                UnhandledBreakpointMode = UnhandledBreakpointProcessingMode.Wait;
            }

            // Store the triggerObject so that we can add it to PSDebugContext
            TriggerObject = triggerObject;

            // Set debugger to step mode so that a break can occur.
            SetDebuggerStepMode(true);

            // If the debugger is enabled and we are not in a breakpoint, trigger an immediate break in the current location
            if (_context._debuggingMode > 0)
            {
                using (IEnumerator<CallStackFrame> enumerator = GetCallStack().GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        OnSequencePointHit(enumerator.Current.FunctionContext);
                    }
                }
            }
        }

        /// <summary>
        /// Returns script position message of current execution stack item.
        /// This is used for WDAC audit mode logging for script information enhancement.
        /// </summary>
        /// <returns>Script position message string.</returns>
        internal override string GetCurrentScriptPosition()
        {
            using (IEnumerator<CallStackFrame> enumerator = GetCallStack().GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    var functionContext = enumerator.Current.FunctionContext;
                    if (functionContext is not null)
                    {
                        var invocationInfo = new InvocationInfo(commandInfo: null, functionContext.CurrentPosition, _context);
                        return $"\n{invocationInfo.PositionMessage}";
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Passes the debugger command to the internal script debugger command processor.  This
        /// is used internally to handle debugger commands such as list, help, etc.
        /// </summary>
        /// <param name="command">Command string.</param>
        /// <param name="output">Output.</param>
        /// <returns>DebuggerCommand containing information on whether and how the command was processed.</returns>
        internal override DebuggerCommand InternalProcessCommand(string command, IList<PSObject> output)
        {
            if (!DebuggerStopped)
            {
                return new DebuggerCommand(command, null, false, false);
            }

            // "Exit" command should always result with "Continue" behavior for legacy compatibility.
            if (command.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                return new DebuggerCommand(command, DebuggerResumeAction.Continue, false, true);
            }

            return _commandProcessor.ProcessCommand(null, command, _currentInvocationInfo, output);
        }

        /// <summary>
        /// Creates a source list based on root script debugger source information if available, with
        /// the current source line highlighted.  This is used internally for nested runspace debugging
        /// where the runspace command is run in context of a parent script.
        /// </summary>
        /// <param name="lineNum">Current source line.</param>
        /// <param name="output">Output collection.</param>
        /// <returns>True if source listed successfully.</returns>
        internal override bool InternalProcessListCommand(int lineNum, IList<PSObject> output)
        {
            if (!DebuggerStopped || (_currentInvocationInfo == null)) { return false; }

            // Create an Invocation object that has full source script from script debugger plus
            // line information provided from caller.
            string fullScript = _currentInvocationInfo.GetFullScript();
            ScriptPosition startScriptPosition = new ScriptPosition(
                _currentInvocationInfo.ScriptName,
                lineNum,
                _currentInvocationInfo.ScriptPosition.StartScriptPosition.Offset,
                _currentInvocationInfo.Line,
                fullScript);
            ScriptPosition endScriptPosition = new ScriptPosition(
                _currentInvocationInfo.ScriptName,
                lineNum,
                _currentInvocationInfo.ScriptPosition.StartScriptPosition.Offset,
                _currentInvocationInfo.Line,
                fullScript);
            InvocationInfo tempInvocationInfo = InvocationInfo.Create(
                _currentInvocationInfo.MyCommand,
                new ScriptExtent(
                    startScriptPosition,
                    endScriptPosition)
                );

            _commandProcessor.ProcessListCommand(tempInvocationInfo, output);

            return true;
        }

        /// <summary>
        /// IsRemote.
        /// </summary>
        internal override bool IsRemote
        {
            get
            {
                Debugger activeDebugger;
                if (_activeDebuggers.TryPeek(out activeDebugger))
                {
                    return (activeDebugger is RemotingJobDebugger);
                }

                return false;
            }
        }

        /// <summary>
        /// Array of stack frame objects of active debugger if any,
        /// otherwise null.
        /// </summary>
        /// <returns>CallStackFrame[].</returns>
        internal override CallStackFrame[] GetActiveDebuggerCallStack()
        {
            Debugger activeDebugger;
            if (_activeDebuggers.TryPeek(out activeDebugger))
            {
                return activeDebugger.GetCallStack().ToArray();
            }

            return null;
        }

        /// <summary>
        /// Sets how the debugger deals with breakpoint events that are not handled.
        ///  Ignore - This is the default behavior and ignores any breakpoint event
        ///           if there is no handler.  Releases any preserved event.
        ///  Wait   - This mode preserves a breakpoint event until a handler is
        ///           subscribed.
        /// </summary>
        internal override UnhandledBreakpointProcessingMode UnhandledBreakpointMode
        {
            get
            {
                return (_preserveUnhandledDebugStopEvent) ? UnhandledBreakpointProcessingMode.Wait : UnhandledBreakpointProcessingMode.Ignore;
            }

            set
            {
                switch (value)
                {
                    case UnhandledBreakpointProcessingMode.Wait:
                        _preserveUnhandledDebugStopEvent = true;
                        break;

                    case UnhandledBreakpointProcessingMode.Ignore:
                        _preserveUnhandledDebugStopEvent = false;
                        ReleaseSavedDebugStop();
                        break;
                }
            }
        }

        #region Breakpoints

        /// <summary>
        /// Adds the provided set of breakpoints to the debugger.
        /// </summary>
        /// <param name="breakpoints">The breakpoints to set.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        public override void SetBreakpoints(IEnumerable<Breakpoint> breakpoints, int? runspaceId)
        {
            if (runspaceId.HasValue)
            {
                GetRunspaceDebugger(runspaceId.Value).SetBreakpoints(breakpoints);
                return;
            }

            foreach (Breakpoint bp in breakpoints)
            {
                switch (bp)
                {
                    case CommandBreakpoint commandBreakpoint:
                        AddCommandBreakpoint(commandBreakpoint);
                        continue;

                    case LineBreakpoint lineBreakpoint:
                        AddLineBreakpoint(lineBreakpoint);
                        continue;

                    case VariableBreakpoint variableBreakpoint:
                        AddVariableBreakpoint(variableBreakpoint);
                        continue;
                }
            }
        }

        /// <summary>
        /// Get a breakpoint by id, primarily for Enable/Disable/Remove-PSBreakpoint cmdlets.
        /// </summary>
        /// <param name="id">Id of the breakpoint you want.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        public override Breakpoint GetBreakpoint(int id, int? runspaceId)
        {
            if (runspaceId.HasValue)
            {
                return GetRunspaceDebugger(runspaceId.Value).GetBreakpoint(id);
            }

            _idToBreakpoint.TryGetValue(id, out Breakpoint breakpoint);
            return breakpoint;
        }

        /// <summary>
        /// Returns breakpoints primarily for the Get-PSBreakpoint cmdlet.
        /// </summary>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        public override List<Breakpoint> GetBreakpoints(int? runspaceId)
        {
            if (runspaceId.HasValue)
            {
                return GetRunspaceDebugger(runspaceId.Value).GetBreakpoints();
            }

            return (from bp in _idToBreakpoint.Values orderby bp.Id select bp).ToList();
        }

        /// <summary>
        /// Sets a command breakpoint in the debugger.
        /// </summary>
        /// <param name="command">The name of the command that will trigger the breakpoint. This value may not be null.</param>
        /// <param name="action">The action to take when the breakpoint is hit. If null, PowerShell will break into the debugger when the breakpoint is hit.</param>
        /// <param name="path">The path to the script file where the breakpoint may be hit. If null, the breakpoint may be hit anywhere the command is invoked.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns></returns>
        public override CommandBreakpoint SetCommandBreakpoint(string command, ScriptBlock action, string path, int? runspaceId)
        {
            if (runspaceId.HasValue)
            {
                return GetRunspaceDebugger(runspaceId.Value).SetCommandBreakpoint(command, action, path);
            }

            Diagnostics.Assert(!string.IsNullOrEmpty(command), "Caller to verify command is not null or empty.");

            WildcardPattern pattern = WildcardPattern.Get(command, WildcardOptions.Compiled | WildcardOptions.IgnoreCase);

            CheckForBreakpointSupport();
            return AddCommandBreakpoint(new CommandBreakpoint(path, pattern, command, action));
        }

        /// <summary>
        /// Sets a line breakpoint in the debugger.
        /// </summary>
        /// <param name="path">The path to the script file where the breakpoint may be hit. This value may not be null.</param>
        /// <param name="line">The line in the script file where the breakpoint may be hit. This value must be greater than or equal to 1.</param>
        /// <param name="column">The column in the script file where the breakpoint may be hit. If 0, the breakpoint will trigger on any statement on the line.</param>
        /// <param name="action">The action to take when the breakpoint is hit. If null, PowerShell will break into the debugger when the breakpoint is hit.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>A LineBreakpoint</returns>
        public override LineBreakpoint SetLineBreakpoint(string path, int line, int column, ScriptBlock action, int? runspaceId)
        {
            if (runspaceId.HasValue)
            {
                return GetRunspaceDebugger(runspaceId.Value).SetLineBreakpoint(path, line, column, action);
            }

            Diagnostics.Assert(path != null, "Caller to verify path is not null.");
            Diagnostics.Assert(line > 0, "Caller to verify line is greater than 0.");

            CheckForBreakpointSupport();
            return AddLineBreakpoint(new LineBreakpoint(path, line, column, action));
        }

        /// <summary>
        /// Sets a variable breakpoint in the debugger.
        /// </summary>
        /// <param name="variableName">The name of the variable that will trigger the breakpoint. This value may not be null.</param>
        /// <param name="accessMode">The variable access mode that will trigger the breakpoint.</param>
        /// <param name="action">The action to take when the breakpoint is hit. If null, PowerShell will break into the debugger when the breakpoint is hit.</param>
        /// <param name="path">The path to the script file where the breakpoint may be hit. If null, the breakpoint may be hit anywhere the variable is accessed using the specified access mode.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>A VariableBreakpoint that was set.</returns>
        public override VariableBreakpoint SetVariableBreakpoint(string variableName, VariableAccessMode accessMode, ScriptBlock action, string path, int? runspaceId)
        {
            if (runspaceId.HasValue)
            {
                return GetRunspaceDebugger(runspaceId.Value).SetVariableBreakpoint(variableName, accessMode, action, path);
            }

            Diagnostics.Assert(!string.IsNullOrEmpty(variableName), "Caller to verify variableName is not null or empty.");

            CheckForBreakpointSupport();
            return AddVariableBreakpoint(new VariableBreakpoint(path, variableName, accessMode, action));
        }

        /// <summary>
        /// This is the implementation of the Remove-PSBreakpoint cmdlet.
        /// </summary>
        /// <param name="breakpoint">Id of the breakpoint you want.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        public override bool RemoveBreakpoint(Breakpoint breakpoint, int? runspaceId)
        {
            if (runspaceId.HasValue)
            {
                return GetRunspaceDebugger(runspaceId.Value).RemoveBreakpoint(breakpoint);
            }

            Diagnostics.Assert(breakpoint != null, "Caller to verify the breakpoint is not null.");

            if (_idToBreakpoint.Remove(breakpoint.Id, out _))
            {
                breakpoint.RemoveSelf(this);

                if (CanDisableDebugger)
                {
                    SetInternalDebugMode(InternalDebugMode.Disabled);
                }

                OnBreakpointUpdated(new BreakpointUpdatedEventArgs(breakpoint, BreakpointUpdateType.Removed, _idToBreakpoint.Count));

                return true;
            }

            return false;
        }

        /// <summary>
        /// This is the implementation of the Enable-PSBreakpoint cmdlet.
        /// </summary>
        /// <param name="breakpoint">Id of the breakpoint you want.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        public override Breakpoint EnableBreakpoint(Breakpoint breakpoint, int? runspaceId)
        {
            if (runspaceId.HasValue)
            {
                return GetRunspaceDebugger(runspaceId.Value).EnableBreakpoint(breakpoint);
            }

            Diagnostics.Assert(breakpoint != null, "Caller to verify the breakpoint is not null.");

            if (_idToBreakpoint.TryGetValue(breakpoint.Id, out _))
            {
                breakpoint.SetEnabled(true);
                OnBreakpointUpdated(new BreakpointUpdatedEventArgs(breakpoint, BreakpointUpdateType.Enabled, _idToBreakpoint.Count));

                return breakpoint;
            }

            return null;
        }

        /// <summary>
        /// This is the implementation of the Disable-PSBreakpoint cmdlet.
        /// </summary>
        /// <param name="breakpoint">Id of the breakpoint you want.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        public override Breakpoint DisableBreakpoint(Breakpoint breakpoint, int? runspaceId)
        {
            if (runspaceId.HasValue)
            {
                return GetRunspaceDebugger(runspaceId.Value).DisableBreakpoint(breakpoint);
            }

            Diagnostics.Assert(breakpoint != null, "Caller to verify the breakpoint is not null.");

            if (_idToBreakpoint.TryGetValue(breakpoint.Id, out _))
            {
                breakpoint.SetEnabled(false);
                OnBreakpointUpdated(new BreakpointUpdatedEventArgs(breakpoint, BreakpointUpdateType.Disabled, _idToBreakpoint.Count));

                return breakpoint;
            }

            return null;
        }

        private static Debugger GetRunspaceDebugger(int runspaceId)
        {
            if (!Runspace.RunspaceDictionary.TryGetValue(runspaceId, out WeakReference<Runspace> wr))
            {
                throw new PSArgumentException(string.Format(DebuggerStrings.InvalidRunspaceId, runspaceId));
            }

            if (!wr.TryGetTarget(out Runspace rs))
            {
                throw new PSArgumentException(DebuggerStrings.UnableToGetRunspace);
            }

            return rs.Debugger;
        }

        #endregion Breakpoints

        #region Job Debugging

        /// <summary>
        /// Sets up debugger to debug provided job or its child jobs.
        /// </summary>
        /// <param name="job">
        /// Job object that is either a debuggable job or a container of
        /// debuggable child jobs.
        /// </param>
        /// <param name="breakAll">
        /// If true, the debugger automatically invokes a break all when it
        /// attaches to the job.
        /// </param>
        internal override void DebugJob(Job job, bool breakAll)
        {
            if (job == null) { throw new PSArgumentNullException(nameof(job)); }

            lock (_syncObject)
            {
                if (_context._debuggingMode < 0)
                {
                    throw new RuntimeException(DebuggerStrings.CannotStartJobDebuggingDebuggerBusy);
                }
            }

            // If a debuggable job was passed in then add it to the
            // job running list.
            bool jobsAdded = TryAddDebugJob(job, breakAll);
            if (!jobsAdded)
            {
                // Otherwise treat as parent Job and iterate over child jobs.
                foreach (Job childJob in job.ChildJobs)
                {
                    if (TryAddDebugJob(childJob, breakAll) && !jobsAdded)
                    {
                        jobsAdded = true;
                    }
                }
            }

            if (!jobsAdded)
            {
                throw new PSInvalidOperationException(DebuggerStrings.NoDebuggableJobsFound);
            }
        }

        private bool TryAddDebugJob(Job job, bool breakAll)
        {
            IJobDebugger debuggableJob = job as IJobDebugger;
            if ((debuggableJob != null) && (debuggableJob.Debugger != null) &&
                ((job.JobStateInfo.State == JobState.Running) || (job.JobStateInfo.State == JobState.AtBreakpoint)))
            {
                // Check to see if job is already stopped in debugger.
                bool jobDebugAlreadyStopped = debuggableJob.Debugger.InBreakpoint;

                // Add to running job list with debugger set to step into.
                SetDebugJobAsync(debuggableJob, false);
                AddToJobRunningList(
                    new PSJobStartEventArgs(job, debuggableJob.Debugger, false),
                    breakAll ? DebuggerResumeAction.StepInto : DebuggerResumeAction.Continue);

                // Raise debug stop event if job is already in stopped state.
                if (jobDebugAlreadyStopped)
                {
                    RemotingJobDebugger remoteJobDebugger = debuggableJob.Debugger as RemotingJobDebugger;
                    if (remoteJobDebugger != null)
                    {
                        remoteJobDebugger.CheckStateAndRaiseStopEvent();
                    }
                    else
                    {
                        Diagnostics.Assert(false, "Should never get debugger stopped job that is not of RemotingJobDebugger type.");
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes job from debugger job list and pops its
        /// debugger from the active debugger stack.
        /// </summary>
        /// <param name="job">Job.</param>
        internal override void StopDebugJob(Job job)
        {
            // Parameter validation.
            if (job == null) { throw new PSArgumentNullException(nameof(job)); }

            SetInternalDebugMode(InternalDebugMode.Disabled);

            RemoveFromRunningJobList(job);
            SetDebugJobAsync(job as IJobDebugger, true);

            foreach (var cJob in job.ChildJobs)
            {
                RemoveFromRunningJobList(cJob);
                SetDebugJobAsync(cJob as IJobDebugger, true);
            }

            RestoreInternalDebugMode();
        }

        /// <summary>
        /// Helper method to set a IJobDebugger job CanDebug property.
        /// </summary>
        /// <param name="debuggableJob">IJobDebugger.</param>
        /// <param name="isAsync">Boolean.</param>
        internal static void SetDebugJobAsync(IJobDebugger debuggableJob, bool isAsync)
        {
            if (debuggableJob != null)
            {
                debuggableJob.IsAsync = isAsync;
            }
        }

        #endregion

        #region Runspace Debugging

        /// <summary>
        /// Sets up debugger to debug provided Runspace in a nested debug session.
        /// </summary>
        /// <param name="runspace">
        /// Runspace to debug.
        /// </param>
        /// <param name="breakAll">
        /// When true, this command will invoke a BreakAll when the debugger is
        /// first attached.
        /// </param>
        internal override void DebugRunspace(Runspace runspace, bool breakAll)
        {
            if (runspace == null)
            {
                throw new PSArgumentNullException(nameof(runspace));
            }

            if (runspace.RunspaceStateInfo.State != RunspaceState.Opened)
            {
                throw new PSInvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, DebuggerStrings.RunspaceDebuggingInvalidRunspaceState, runspace.RunspaceStateInfo.State)
                    );
            }

            lock (_syncObject)
            {
                if (_context._debuggingMode < 0)
                {
                    throw new PSInvalidOperationException(DebuggerStrings.RunspaceDebuggingDebuggerBusy);
                }
            }

            if (runspace.Debugger == null)
            {
                throw new PSInvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, DebuggerStrings.RunspaceDebuggingNoRunspaceDebugger, runspace.Name));
            }

            if (runspace.Debugger.DebugMode == DebugModes.None)
            {
                throw new PSInvalidOperationException(DebuggerStrings.RunspaceDebuggingDebuggerIsOff);
            }

            AddToRunningRunspaceList(new PSStandaloneMonitorRunspaceInfo(runspace));

            if (!runspace.Debugger.InBreakpoint && breakAll)
            {
                EnableDebuggerStepping(EnableNestedType.NestedRunspace);
            }
        }

        /// <summary>
        /// Removes the provided Runspace from the nested "active" debugger state.
        /// </summary>
        /// <param name="runspace">Runspace.</param>
        internal override void StopDebugRunspace(Runspace runspace)
        {
            if (runspace == null) { throw new PSArgumentNullException(nameof(runspace)); }

            SetInternalDebugMode(InternalDebugMode.Disabled);

            RemoveFromRunningRunspaceList(runspace);

            RestoreInternalDebugMode();
        }

        #endregion

        #region Runspace Debug Processing

        /// <summary>
        /// Adds the provided Runspace object to the runspace debugger processing queue.
        /// The queue will then raise the StartRunspaceDebugProcessing events for each runspace to allow
        /// a host script debugger implementation to provide an active debugging session.
        /// </summary>
        /// <param name="runspace">Runspace to debug.</param>
        internal override void QueueRunspaceForDebug(Runspace runspace)
        {
            runspace.StateChanged += RunspaceStateChangedHandler;
            runspace.AvailabilityChanged += RunspaceAvailabilityChangedHandler;
            _runspaceDebugQueue.Value.Enqueue(new StartRunspaceDebugProcessingEventArgs(runspace));
            StartRunspaceForDebugQueueProcessing();
        }

        /// <summary>
        /// Causes the CancelRunspaceDebugProcessing event to be raised which notifies subscribers that these debugging
        /// sessions should be cancelled.
        /// </summary>
        public override void CancelDebuggerProcessing()
        {
            // Empty runspace debugger processing queue and then notify any subscribers.
            ReleaseInternalRunspaceDebugProcessing(null, true);

            try
            {
                RaiseCancelRunspaceDebugProcessingEvent();
            }
            catch (Exception)
            { }
        }

        private void ReleaseInternalRunspaceDebugProcessing(object sender, bool emptyQueue = false)
        {
            Runspace runspace = sender as Runspace;
            if (runspace != null)
            {
                runspace.StateChanged -= RunspaceStateChangedHandler;
                runspace.AvailabilityChanged -= RunspaceAvailabilityChangedHandler;
            }

            if (emptyQueue && _runspaceDebugQueue.IsValueCreated)
            {
                StartRunspaceDebugProcessingEventArgs args;
                while (_runspaceDebugQueue.Value.TryDequeue(out args))
                {
                    args.Runspace.StateChanged -= RunspaceStateChangedHandler;
                    args.Runspace.AvailabilityChanged -= RunspaceAvailabilityChangedHandler;
                    try
                    {
                        args.Runspace.Debugger.UnhandledBreakpointMode = UnhandledBreakpointProcessingMode.Ignore;
                    }
                    catch (Exception) { }
                }
            }

            if (_runspaceDebugCompleteEvent != null)
            {
                try
                {
                    _runspaceDebugCompleteEvent.Set();
                }
                catch (ObjectDisposedException) { }
            }
        }

        private void RunspaceStateChangedHandler(object sender, RunspaceStateEventArgs args)
        {
            switch (args.RunspaceStateInfo.State)
            {
                case RunspaceState.Closed:
                case RunspaceState.Broken:
                case RunspaceState.Disconnected:
                    ReleaseInternalRunspaceDebugProcessing(sender);
                    break;
            }
        }

        private void RunspaceAvailabilityChangedHandler(object sender, RunspaceAvailabilityEventArgs args)
        {
            if (args.RunspaceAvailability == RunspaceAvailability.Available)
            {
                ReleaseInternalRunspaceDebugProcessing(sender);
            }
        }

        #endregion

        #endregion

        #region Job debugger integration

        private void AddToJobRunningList(PSJobStartEventArgs jobArgs, DebuggerResumeAction startAction)
        {
            bool newJob = false;

            lock (_syncObject)
            {
                jobArgs.Job.StateChanged += HandleJobStateChanged;
                if (jobArgs.Job.IsPersistentState(jobArgs.Job.JobStateInfo.State))
                {
                    jobArgs.Job.StateChanged -= HandleJobStateChanged;
                    return;
                }

                if (!_runningJobs.ContainsKey(jobArgs.Job.InstanceId))
                {
                    // For now ignore WF jobs started asynchronously from script.
                    if (jobArgs.IsAsync) { return; }

                    // Turn on output processing monitoring on workflow job so that
                    // the debug stop events can coordinate with end of output processing.
                    jobArgs.Job.OutputProcessingStateChanged += HandleOutputProcessingStateChanged;
                    jobArgs.Job.MonitorOutputProcessing = true;

                    _runningJobs.Add(jobArgs.Job.InstanceId, jobArgs);
                    jobArgs.Debugger.DebuggerStop += HandleMonitorRunningJobsDebuggerStop;

                    newJob = true;
                }
            }

            if (newJob)
            {
                jobArgs.Debugger.SetParent(
                    this,
                    _idToBreakpoint.Values.ToArray<Breakpoint>(),
                    startAction,
                    _context.EngineHostInterface.ExternalHost,
                    _context.SessionState.Path.CurrentLocation);
            }
            else
            {
                // If job already in collection then make sure start action is set.
                // Note that this covers the case where Debug-Job was performed on
                // an async job, which then becomes sync, the user continues execution
                // and then wants to break (step mode) into the debugger *again*.
                jobArgs.Debugger.SetDebuggerStepMode(true);
            }
        }

        private void SetRunningJobListToStep(bool enableStepping)
        {
            PSJobStartEventArgs[] runningJobs;
            lock (_syncObject)
            {
                runningJobs = _runningJobs.Values.ToArray();
            }

            foreach (var item in runningJobs)
            {
                try
                {
                    item.Debugger.SetDebuggerStepMode(enableStepping);
                }
                catch (PSNotImplementedException) { }
            }
        }

        private void SetRunspaceListToStep(bool enableStepping)
        {
            PSMonitorRunspaceInfo[] runspaceList;
            lock (_syncObject)
            {
                runspaceList = _runningRunspaces.Values.ToArray();
            }

            foreach (var item in runspaceList)
            {
                try
                {
                    Debugger nestedDebugger = item.NestedDebugger;
                    nestedDebugger?.SetDebuggerStepMode(enableStepping);
                }
                catch (PSNotImplementedException) { }
            }
        }

        private void RemoveFromRunningJobList(Job job)
        {
            job.StateChanged -= HandleJobStateChanged;
            job.OutputProcessingStateChanged -= HandleOutputProcessingStateChanged;

            PSJobStartEventArgs jobArgs = null;
            lock (_syncObject)
            {
                if (_runningJobs.TryGetValue(job.InstanceId, out jobArgs))
                {
                    jobArgs.Debugger.DebuggerStop -= HandleMonitorRunningJobsDebuggerStop;
                    _runningJobs.Remove(job.InstanceId);
                }
            }

            if (jobArgs != null)
            {
                // Pop from active debugger stack.
                lock (_syncActiveDebuggerStopObject)
                {
                    Debugger activeDebugger;
                    if (_activeDebuggers.TryPeek(out activeDebugger))
                    {
                        if (activeDebugger.Equals(jobArgs.Debugger))
                        {
                            PopActiveDebugger();
                        }
                    }
                }
            }
        }

        private void ClearRunningJobList()
        {
            PSJobStartEventArgs[] runningJobs = null;
            lock (_syncObject)
            {
                if (_runningJobs.Count > 0)
                {
                    runningJobs = new PSJobStartEventArgs[_runningJobs.Values.Count];
                    _runningJobs.Values.CopyTo(runningJobs, 0);
                }
            }

            if (runningJobs != null)
            {
                foreach (var item in runningJobs)
                {
                    RemoveFromRunningJobList(item.Job);
                }
            }
        }

        private bool PushActiveDebugger(Debugger debugger, int callstackOffset)
        {
            // Don't push active debugger if script debugger disabled debugging.
            if (_context._debuggingMode == -1) { return false; }

            // Disable script debugging while another debugger is running.
            // -1 - Indicates script debugging is disabled from script debugger.
            // -2 - Indicates script debugging is disabled from pushed active debugger.
            SetInternalDebugMode(InternalDebugMode.InPushedStop);

            // Save running calling frame.
            _nestedRunningFrame = _callStack[_callStack.Count - callstackOffset];

            _commandProcessor.Reset();

            // Make active debugger.
            _activeDebuggers.Push(debugger);

            return true;
        }

        private Debugger PopActiveDebugger()
        {
            Debugger poppedDebugger = null;
            if (_activeDebuggers.TryPop(out poppedDebugger))
            {
                int runningJobCount;
                lock (_syncObject)
                {
                    runningJobCount = _runningJobs.Count;
                }

                if (runningJobCount == 0)
                {
                    // If we are back to the root debugger and are in step mode, ensure
                    // that the root debugger is in step mode to continue stepping.
                    switch (_lastActiveDebuggerAction)
                    {
                        case DebuggerResumeAction.StepInto:
                        case DebuggerResumeAction.StepOver:
                        case DebuggerResumeAction.StepOut:
                            // Set script debugger to step mode after the WF running
                            // script completes.
                            _steppingMode = SteppingMode.StepIn;
                            _overOrOutFrame = _nestedRunningFrame;
                            _nestedRunningFrame = null;
                            break;

                        case DebuggerResumeAction.Stop:
                            _nestedDebuggerStop = true;
                            break;

                        default:
                            ResumeExecution(DebuggerResumeAction.Continue);
                            break;
                    }

                    // Allow script debugger to continue in debugging mode.
                    _processingOutputCount = 0;
                    SetInternalDebugMode(InternalDebugMode.Enabled);
                    _currentDebuggerAction = _lastActiveDebuggerAction;
                    _lastActiveDebuggerAction = DebuggerResumeAction.Continue;
                }
            }

            return poppedDebugger;
        }

        private void HandleActiveJobDebuggerStop(object sender, DebuggerStopEventArgs args)
        {
            // If we are debugging nested runspaces then ignore job debugger stops
            if (_runningRunspaces.Count > 0) { return; }

            // Forward active debugger event.
            if (args != null)
            {
                // Save copy of arguments.
                DebuggerStopEventArgs copyArgs = new DebuggerStopEventArgs(
                    args.InvocationInfo,
                    new Collection<Breakpoint>(args.Breakpoints),
                    args.ResumeAction);
                _debuggerStopEventArgs.Push(copyArgs);

                CallStackInfo savedCallStackItem = null;
                try
                {
                    // Wait for up to 5 seconds for output processing to complete.
                    _processingOutputCompleteEvent.Wait(5000);

                    // Fix up call stack representing this WF call.
                    savedCallStackItem = FixUpCallStack();

                    // Blocking call that raises stop event.
                    RaiseDebuggerStopEvent(args);
                    _lastActiveDebuggerAction = args.ResumeAction;
                }
                finally
                {
                    RestoreCallStack(savedCallStackItem);
                    _debuggerStopEventArgs.TryPop(out copyArgs);
                }
            }
        }

        private CallStackInfo FixUpCallStack()
        {
            // Remove the top level call stack item, which is
            // the PS script that starts the workflow.  The workflow
            // debugger will add its call stack in its GetCallStack()
            // override.
            int count = _callStack.Count;
            CallStackInfo item = null;
            if (count > 1)
            {
                item = _callStack.Last();
                _callStack.RemoveAt(count - 1);
            }

            return item;
        }

        private void RestoreCallStack(CallStackInfo item)
        {
            if (item != null)
            {
                _callStack.Add(item);
            }
        }

        private void HandleMonitorRunningJobsDebuggerStop(object sender, DebuggerStopEventArgs args)
        {
            if (!IsJobDebuggingMode())
            {
                // Ignore job debugger stop.
                args.ResumeAction = DebuggerResumeAction.Continue;
                return;
            }

            Debugger senderDebugger = sender as Debugger;
            bool pushSucceeded = false;
            lock (_syncActiveDebuggerStopObject)
            {
                Debugger activeDebugger = null;
                if (_activeDebuggers.TryPeek(out activeDebugger))
                {
                    if (activeDebugger.Equals(senderDebugger))
                    {
                        HandleActiveJobDebuggerStop(sender, args);
                        return;
                    }

                    if (IsRunningWFJobsDebugger(activeDebugger))
                    {
                        // Replace current job active debugger by first popping.
                        PopActiveDebugger();
                    }
                }

                pushSucceeded = PushActiveDebugger(senderDebugger, _jobCallStackOffset);
            }

            // Handle debugger stop outside lock.
            if (pushSucceeded)
            {
                // Forward the debug stop event.
                HandleActiveJobDebuggerStop(sender, args);
            }
        }

        private bool IsJobDebuggingMode()
        {
            return ((((DebugMode & DebugModes.LocalScript) == DebugModes.LocalScript) && IsLocalSession) ||
                    (((DebugMode & DebugModes.RemoteScript) == DebugModes.RemoteScript) && !IsLocalSession));
        }

        private bool IsRunningWFJobsDebugger(Debugger debugger)
        {
            lock (_syncObject)
            {
                foreach (var item in _runningJobs.Values)
                {
                    if (item.Debugger.Equals(debugger))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void HandleJobStateChanged(object sender, JobStateEventArgs args)
        {
            Job job = sender as Job;

            if (job.IsPersistentState(args.JobStateInfo.State))
            {
                RemoveFromRunningJobList(job);
            }
        }

        private void HandleOutputProcessingStateChanged(object sender, OutputProcessingStateEventArgs e)
        {
            lock (_syncObject)
            {
                if (e.ProcessingOutput)
                {
                    if (++_processingOutputCount == 1)
                    {
                        _processingOutputCompleteEvent.Reset();
                    }
                }
                else if (_processingOutputCount > 0)
                {
                    if (--_processingOutputCount == 0)
                    {
                        _processingOutputCompleteEvent.Set();
                    }
                }
            }
        }

        private DebuggerCommandResults ProcessCommandForActiveDebugger(PSCommand command, PSDataCollection<PSObject> output)
        {
            // Check for debugger "detach" command which is only applicable to nested debugging.
            bool detachCommand = ((command.Commands.Count > 0) &&
                                  ((command.Commands[0].CommandText.Equals("Detach", StringComparison.OrdinalIgnoreCase)) ||
                                   (command.Commands[0].CommandText.Equals("d", StringComparison.OrdinalIgnoreCase))));

            Debugger activeDebugger;
            if (_activeDebuggers.TryPeek(out activeDebugger))
            {
                if (detachCommand)
                {
                    // Exit command means to cancel the nested debugger session.  This needs to be done by the
                    // owner of the session so we raise an event and release the debugger stop.
                    UnhandledBreakpointMode = UnhandledBreakpointProcessingMode.Ignore;
                    RaiseNestedDebuggingCancelEvent();
                    return new DebuggerCommandResults(DebuggerResumeAction.Continue, true);
                }
                else if ((command.Commands.Count > 0) &&
                         (command.Commands[0].CommandText.IndexOf(".EnterNestedPrompt()", StringComparison.OrdinalIgnoreCase) > 0))
                {
                    // Prevent a host EnterNestedPrompt() call from occurring in an active debugger.
                    // Host nested prompt makes no sense in this case and can cause host to stop responding depending on host implementation.
                    throw new PSNotSupportedException();
                }

                // Get current debugger stop breakpoint info.
                DebuggerStopEventArgs stopArgs;
                if (_debuggerStopEventArgs.TryPeek(out stopArgs))
                {
                    string commandText = command.Commands[0].CommandText;

                    // Check to see if this is a resume command that we handle here.
                    DebuggerCommand dbgCommand = _commandProcessor.ProcessBasicCommand(commandText);
                    if (dbgCommand != null &&
                        dbgCommand.ResumeAction != null)
                    {
                        _lastActiveDebuggerAction = dbgCommand.ResumeAction.Value;
                        return new DebuggerCommandResults(dbgCommand.ResumeAction, true);
                    }
                }

                return activeDebugger.ProcessCommand(command, output);
            }

            if (detachCommand)
            {
                // Detach command only applies to nested debugging.  So if there isn't any active debugger then emit error.
                throw new PSInvalidOperationException(DebuggerStrings.InvalidDetachCommand);
            }

            return null;
        }

        private bool StopCommandForActiveDebugger()
        {
            Debugger activeDebugger;
            if (_activeDebuggers.TryPeek(out activeDebugger))
            {
                activeDebugger.StopProcessCommand();
                return true;
            }

            return false;
        }

        #endregion

        #region Runspace debugger integration

        internal override void StartMonitoringRunspace(PSMonitorRunspaceInfo runspaceInfo)
        {
            if (runspaceInfo == null || runspaceInfo.Runspace == null) { return; }

            if ((runspaceInfo.Runspace.Debugger != null) &&
                runspaceInfo.Runspace.Debugger.Equals(this))
            {
                Debug.Fail("Nested debugger cannot be the root debugger.");
                return;
            }

            DebuggerResumeAction startAction = (_currentDebuggerAction == DebuggerResumeAction.StepInto) ?
                DebuggerResumeAction.StepInto : DebuggerResumeAction.Continue;

            AddToRunningRunspaceList(runspaceInfo.Copy());
        }

        internal override void EndMonitoringRunspace(PSMonitorRunspaceInfo runspaceInfo)
        {
            if (runspaceInfo == null || runspaceInfo.Runspace == null) { return; }

            RemoveFromRunningRunspaceList(runspaceInfo.Runspace);
        }

        /// <summary>
        /// If a debug stop event is currently pending then this method will release
        /// the event to continue processing.
        /// </summary>
        internal override void ReleaseSavedDebugStop()
        {
            if (IsPendingDebugStopEvent)
            {
                _preserveDebugStopEvent.Set();
            }
        }

        private void AddToRunningRunspaceList(PSMonitorRunspaceInfo args)
        {
            Runspace runspace = args.Runspace;
            runspace.StateChanged += HandleRunspaceStateChanged;
            RunspaceState rsState = runspace.RunspaceStateInfo.State;
            if (rsState == RunspaceState.Broken ||
                rsState == RunspaceState.Closed ||
                rsState == RunspaceState.Disconnected)
            {
                runspace.StateChanged -= HandleRunspaceStateChanged;
                return;
            }

            lock (_syncObject)
            {
                if (!_runningRunspaces.ContainsKey(runspace.InstanceId))
                {
                    _runningRunspaces.Add(runspace.InstanceId, args);
                }
            }

            // It is possible for the debugger to be non-null at this point if a runspace
            // is being reused.
            SetUpDebuggerOnRunspace(runspace);
        }

        private void RemoveFromRunningRunspaceList(Runspace runspace)
        {
            runspace.StateChanged -= HandleRunspaceStateChanged;

            // Remove from running list.
            PSMonitorRunspaceInfo runspaceInfo = null;
            lock (_syncObject)
            {
                if (_runningRunspaces.TryGetValue(runspace.InstanceId, out runspaceInfo))
                {
                    _runningRunspaces.Remove(runspace.InstanceId);
                }
            }

            // Clean up nested debugger.
            NestedRunspaceDebugger nestedDebugger = runspaceInfo?.NestedDebugger;
            if (nestedDebugger != null)
            {
                nestedDebugger.DebuggerStop -= HandleMonitorRunningRSDebuggerStop;
                nestedDebugger.Dispose();

                // If current active debugger, then pop.
                lock (_syncActiveDebuggerStopObject)
                {
                    Debugger activeDebugger;
                    if (_activeDebuggers.TryPeek(out activeDebugger))
                    {
                        if (activeDebugger.Equals(nestedDebugger))
                        {
                            PopActiveDebugger();
                        }
                    }
                }
            }
        }

        private void ClearRunningRunspaceList()
        {
            PSMonitorRunspaceInfo[] runningRunspaces = null;
            lock (_syncObject)
            {
                if (_runningRunspaces.Count > 0)
                {
                    runningRunspaces = new PSMonitorRunspaceInfo[_runningRunspaces.Count];
                    _runningRunspaces.Values.CopyTo(runningRunspaces, 0);
                }
            }

            if (runningRunspaces != null)
            {
                foreach (var item in runningRunspaces)
                {
                    RemoveFromRunningRunspaceList(item.Runspace);
                }
            }
        }

        private void HandleRunspaceStateChanged(object sender, RunspaceStateEventArgs e)
        {
            Runspace runspace = sender as Runspace;
            bool remove = false;

            switch (e.RunspaceStateInfo.State)
            {
                // Detect transition to Opened state.
                case RunspaceState.Opened:
                    remove = !SetUpDebuggerOnRunspace(runspace);
                    break;

                // Detect any transition to a finished runspace.
                case RunspaceState.Broken:
                case RunspaceState.Closed:
                case RunspaceState.Disconnected:
                    remove = true;
                    break;
            }

            if (remove)
            {
                RemoveFromRunningRunspaceList(runspace);
            }
        }

        private void HandleMonitorRunningRSDebuggerStop(object sender, DebuggerStopEventArgs args)
        {
            if (sender == null || args == null) { return; }

            Debugger senderDebugger = sender as Debugger;
            bool pushSucceeded = false;
            lock (_syncActiveDebuggerStopObject)
            {
                Debugger activeDebugger;
                if (_activeDebuggers.TryPeek(out activeDebugger))
                {
                    // Replace current runspace debugger by first popping the old debugger.
                    if (IsRunningRSDebugger(activeDebugger))
                    {
                        PopActiveDebugger();
                    }
                }

                // Get nested debugger runspace info.
                if (senderDebugger is not NestedRunspaceDebugger nestedDebugger) { return; }

                PSMonitorRunspaceType runspaceType = nestedDebugger.RunspaceType;

                // Fix up invocation info script extents for embedded nested debuggers where the script source is
                // from the parent.
                args.InvocationInfo = nestedDebugger.FixupInvocationInfo(args.InvocationInfo);

                // Finally push the runspace debugger.
                pushSucceeded = PushActiveDebugger(senderDebugger, _runspaceCallStackOffset);
            }

            // Handle debugger stop outside lock.
            if (pushSucceeded)
            {
                // Forward the debug stop event.
                // This method will always pop the debugger after debugger stop completes.
                HandleActiveRunspaceDebuggerStop(sender, args);
            }
        }

        private void HandleActiveRunspaceDebuggerStop(object sender, DebuggerStopEventArgs args)
        {
            // Save copy of arguments.
            DebuggerStopEventArgs copyArgs = new DebuggerStopEventArgs(
                args.InvocationInfo,
                new Collection<Breakpoint>(args.Breakpoints),
                args.ResumeAction);
            _debuggerStopEventArgs.Push(copyArgs);

            // Forward active debugger event.
            try
            {
                // Blocking call that raises the stop event.
                RaiseDebuggerStopEvent(args);
                _lastActiveDebuggerAction = args.ResumeAction;
            }
            catch (Exception)
            {
                // Catch all external user generated exceptions thrown on event thread.
            }
            finally
            {
                _debuggerStopEventArgs.TryPop(out copyArgs);
                PopActiveDebugger();
            }
        }

        private bool IsRunningRSDebugger(Debugger debugger)
        {
            lock (_syncObject)
            {
                foreach (var item in _runningRunspaces.Values)
                {
                    if (item.Runspace.Debugger.Equals(debugger))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool SetUpDebuggerOnRunspace(Runspace runspace)
        {
            PSMonitorRunspaceInfo runspaceInfo = null;
            lock (_syncObject)
            {
                _runningRunspaces.TryGetValue(runspace.InstanceId, out runspaceInfo);
            }

            // Create nested debugger wrapper if it is not already created and if
            // the runspace debugger is available.
            if ((runspace.Debugger != null) &&
                (runspaceInfo != null) &&
                (runspaceInfo.NestedDebugger == null))
            {
                try
                {
                    NestedRunspaceDebugger nestedDebugger = runspaceInfo.CreateDebugger(this);

                    runspaceInfo.NestedDebugger = nestedDebugger;

                    nestedDebugger.DebuggerStop += HandleMonitorRunningRSDebuggerStop;

                    if (((_lastActiveDebuggerAction == DebuggerResumeAction.StepInto) || (_currentDebuggerAction == DebuggerResumeAction.StepInto)) &&
                        !nestedDebugger.IsActive)
                    {
                        nestedDebugger.SetDebuggerStepMode(true);
                    }

                    // If the nested debugger has a pending (saved) debug stop then
                    // release it here now that we have the debug stop handler added.
                    // Note that the DebuggerStop event is raised on the original execution
                    // thread in the debugger (not this thread).
                    nestedDebugger.CheckStateAndRaiseStopEvent();

                    return true;
                }
                catch (InvalidRunspaceStateException) { }
            }

            return false;
        }

        #endregion

        #region Runspace Debug Processing

        private void StartRunspaceForDebugQueueProcessing()
        {
            int startThread = Interlocked.CompareExchange(ref _processingRunspaceDebugQueue, 1, 0);

            if (startThread == 0)
            {
                var thread = new System.Threading.Thread(
                    new ThreadStart(DebuggerQueueThreadProc));
                thread.Start();
            }
        }

        private void DebuggerQueueThreadProc()
        {
            StartRunspaceDebugProcessingEventArgs runspaceDebugProcessArgs;
            while (_runspaceDebugQueue.Value.TryDequeue(out runspaceDebugProcessArgs))
            {
                if (IsStartRunspaceDebugProcessingEventSubscribed())
                {
                    try
                    {
                        RaiseStartRunspaceDebugProcessingEvent(runspaceDebugProcessArgs);
                    }
                    catch (Exception) { }
                }
                else
                {
                    // If there are no ProcessDebugger event subscribers then default to handling internally.
                    runspaceDebugProcessArgs.UseDefaultProcessing = true;
                }

                // Check for internal handling request.
                if (runspaceDebugProcessArgs.UseDefaultProcessing)
                {
                    try
                    {
                        ProcessRunspaceDebugInternally(runspaceDebugProcessArgs.Runspace);
                    }
                    catch (Exception) { }
                }
            }

            Interlocked.CompareExchange(ref _processingRunspaceDebugQueue, 0, 1);

            if (!_runspaceDebugQueue.Value.IsEmpty)
            {
                StartRunspaceForDebugQueueProcessing();
            }
        }

        private void ProcessRunspaceDebugInternally(Runspace runspace)
        {
            WaitForReadyDebug();

            DebugRunspace(runspace, breakAll: true);

            // Block this event thread until debugging has ended.
            WaitForDebugComplete();

            // Ensure runspace debugger is not stopped in break mode.
            if (runspace.Debugger.InBreakpoint)
            {
                try
                {
                    runspace.Debugger.UnhandledBreakpointMode = UnhandledBreakpointProcessingMode.Ignore;
                }
                catch (Exception) { }
            }

            StopDebugRunspace(runspace);

            // If we return to local script execution in step mode then ensure the debugger is enabled.
            _nestedDebuggerStop = false;
            if ((_steppingMode == SteppingMode.StepIn) && (_currentDebuggerAction != DebuggerResumeAction.Stop) && (_context._debuggingMode == 0))
            {
                SetInternalDebugMode(InternalDebugMode.Enabled);
            }

            RaiseRunspaceProcessingCompletedEvent(
                new ProcessRunspaceDebugEndEventArgs(runspace));
        }

        private void WaitForReadyDebug()
        {
            // Wait up to ten seconds
            System.Threading.Thread.Sleep(500);
            int count = 0;
            bool debugReady = false;
            do
            {
                System.Threading.Thread.Sleep(250);
                debugReady = IsDebuggerReady();
            } while (!debugReady && (count++ < 40));

            if (!debugReady) { throw new PSInvalidOperationException(); }
        }

        private bool IsDebuggerReady()
        {
            return (!this.IsPushed && !this.InBreakpoint && (this._context._debuggingMode > -1) && (this._context.InternalHost.NestedPromptCount == 0));
        }

        private void WaitForDebugComplete()
        {
            if (_runspaceDebugCompleteEvent == null)
            {
                _runspaceDebugCompleteEvent = new ManualResetEventSlim(false);
            }
            else
            {
                _runspaceDebugCompleteEvent.Reset();
            }

            _runspaceDebugCompleteEvent.Wait();
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            // Ensure all job event handlers are removed.
            PSJobStartEventArgs[] runningJobs;
            lock (_syncObject)
            {
                runningJobs = _runningJobs.Values.ToArray();
            }

            foreach (var item in runningJobs)
            {
                Job job = item.Job;
                if (job != null)
                {
                    job.StateChanged -= HandleJobStateChanged;
                    job.OutputProcessingStateChanged -= HandleOutputProcessingStateChanged;
                }
            }

            _processingOutputCompleteEvent.Dispose();
            _processingOutputCompleteEvent = null;

            if (_preserveDebugStopEvent != null)
            {
                _preserveDebugStopEvent.Dispose();
                _preserveDebugStopEvent = null;
            }

            if (_runspaceDebugCompleteEvent != null)
            {
                _runspaceDebugCompleteEvent.Dispose();
                _runspaceDebugCompleteEvent = null;
            }
        }

        #endregion

        #region Tracing

        internal void EnableTracing(int traceLevel, bool? step)
        {
            // Enable might actually be disabling depending on the arguments.
            if (traceLevel < 1 && (step == null || !(bool)step))
            {
                DisableTracing();
                return;
            }

            _savedIgnoreScriptDebug = _context.IgnoreScriptDebug;
            _context.IgnoreScriptDebug = false;

            _context.PSDebugTraceLevel = traceLevel;
            if (step != null)
            {
                _context.PSDebugTraceStep = (bool)step;
            }

            SetInternalDebugMode(InternalDebugMode.Enabled);
        }

        internal void DisableTracing()
        {
            _context.IgnoreScriptDebug = _savedIgnoreScriptDebug;
            _context.PSDebugTraceLevel = 0;
            _context.PSDebugTraceStep = false;
            if (CanDisableDebugger)
            {
                SetInternalDebugMode(InternalDebugMode.Disabled);
            }
        }

        private bool _savedIgnoreScriptDebug = false;

        internal void Trace(string messageId, string resourceString, params object[] args)
        {
            ActionPreference pref = ActionPreference.Continue;

            string message;
            if (args == null || args.Length == 0)
            {
                // Don't format in case the string contains literal curly braces
                message = resourceString;
            }
            else
            {
                message = StringUtil.Format(resourceString, args);
            }

            if (string.IsNullOrEmpty(message))
            {
                message = "Could not load text for msh script tracing message id '" + messageId + "'";
                Diagnostics.Assert(false, message);
            }

            ((InternalHostUserInterface)_context.EngineHostInterface.UI).WriteDebugLine(message, ref pref);
        }

        internal void TraceLine(IScriptExtent extent)
        {
            string msg = PositionUtilities.BriefMessage(extent.StartScriptPosition);
            InternalHostUserInterface ui = (InternalHostUserInterface)_context.EngineHostInterface.UI;

            ActionPreference pref = _context.PSDebugTraceStep ?
                ActionPreference.Inquire : ActionPreference.Continue;

            ui.WriteDebugLine(msg, ref pref);

            if (pref == ActionPreference.Continue)
                _context.PSDebugTraceStep = false;
        }

        internal void TraceScriptFunctionEntry(FunctionContext functionContext)
        {
            var methodName = functionContext._functionName;
            if (string.IsNullOrEmpty(functionContext._file))
            {
                Trace("TraceEnteringFunction", ParserStrings.TraceEnteringFunction, methodName);
            }
            else
            {
                Trace("TraceEnteringFunctionDefinedInFile", ParserStrings.TraceEnteringFunctionDefinedInFile, methodName, functionContext._file);
            }
        }

        internal void TraceVariableSet(string varName, object value)
        {
            // Don't trace into debugger hidden or debugger step through unless the trace level > 2.
            if (_callStack.Any() && _context.PSDebugTraceLevel <= 2)
            {
                // Skip trace messages in hidden/step through frames.
                var frame = _callStack.Last();
                if (frame.IsFrameHidden || frame.DebuggerStepThrough)
                {
                    return;
                }
            }

            // If the value is an IEnumerator, we don't attempt to get its string format via 'ToStringParser' method,
            // because 'ToStringParser' would iterate through the enumerator to get the individual elements, which will
            // make irreversible changes to the enumerator.
            bool isValueAnIEnumerator = PSObject.Base(value) is IEnumerator;
            string valAsString = isValueAnIEnumerator ? nameof(IEnumerator) : PSObject.ToStringParser(_context, value);
            int msgLength = 60 - varName.Length;

            if (valAsString.Length > msgLength)
            {
                valAsString = valAsString.Substring(0, msgLength) + PSObjectHelper.Ellipsis;
            }

            Trace("TraceVariableAssignment", ParserStrings.TraceVariableAssignment, varName, valAsString);
        }

        #endregion Tracing
    }

    #endregion

    #region NestedRunspaceDebugger

    /// <summary>
    /// Base class for nested runspace debugger wrapper.
    /// </summary>
    internal abstract class NestedRunspaceDebugger : Debugger, IDisposable
    {
        #region Members

        private bool _isDisposed;
        protected Runspace _runspace;
        protected Debugger _wrappedDebugger;

        #endregion

        #region Properties

        /// <summary>
        /// Type of runspace being monitored for debugging.
        /// </summary>
        public PSMonitorRunspaceType RunspaceType { get; }

        /// <summary>
        /// Unique parent debugger identifier for monitored runspace.
        /// </summary>
        public Guid ParentDebuggerId
        {
            get;
            private set;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an instance of NestedRunspaceDebugger.
        /// </summary>
        /// <param name="runspace">Runspace.</param>
        /// <param name="runspaceType">Runspace type.</param>
        /// <param name="parentDebuggerId">Debugger Id of parent.</param>
        protected NestedRunspaceDebugger(
            Runspace runspace,
            PSMonitorRunspaceType runspaceType,
            Guid parentDebuggerId)
        {
            if (runspace == null || runspace.Debugger == null)
            {
                throw new PSArgumentNullException(nameof(runspace));
            }

            _runspace = runspace;
            _wrappedDebugger = runspace.Debugger;
            base.SetDebugMode(_wrappedDebugger.DebugMode);
            RunspaceType = runspaceType;
            ParentDebuggerId = parentDebuggerId;

            // Handlers for wrapped debugger events.
            _wrappedDebugger.BreakpointUpdated += HandleBreakpointUpdated;
            _wrappedDebugger.DebuggerStop += HandleDebuggerStop;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Adds the provided set of breakpoints to the debugger.
        /// </summary>
        /// <param name="breakpoints">Breakpoints.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        public override void SetBreakpoints(IEnumerable<Breakpoint> breakpoints, int? runspaceId) =>
            _wrappedDebugger.SetBreakpoints(breakpoints, runspaceId);

        /// <summary>
        /// Process debugger or PowerShell command/script.
        /// </summary>
        /// <param name="command">PowerShell command.</param>
        /// <param name="output">Output collection.</param>
        /// <returns>DebuggerCommandResults.</returns>
        public override DebuggerCommandResults ProcessCommand(PSCommand command, PSDataCollection<PSObject> output)
        {
            if (_isDisposed) { return new DebuggerCommandResults(null, false); }

            // Preprocess debugger commands.
            string cmd = command.Commands[0].CommandText.Trim();

            if (cmd.Equals("prompt", StringComparison.OrdinalIgnoreCase))
            {
                return HandlePromptCommand(output);
            }

            if (cmd.Equals("k", StringComparison.OrdinalIgnoreCase) ||
                cmd.StartsWith("Get-PSCallStack", StringComparison.OrdinalIgnoreCase))
            {
                return HandleCallStack(output);
            }

            if (cmd.Equals("l", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                if (HandleListCommand(output))
                {
                    return new DebuggerCommandResults(null, true);
                }
            }

            return _wrappedDebugger.ProcessCommand(command, output);
        }

        /// <summary>
        /// Get a breakpoint by id.
        /// </summary>
        /// <param name="id">Id of the breakpoint you want.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
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
        /// Removes a breakpoint from the debugger.
        /// </summary>
        /// <param name="breakpoint">The breakpoint to remove from the debugger. This value may not be null.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>True if the breakpoint was removed from the debugger; false otherwise.</returns>
        public override bool RemoveBreakpoint(Breakpoint breakpoint, int? runspaceId) =>
            _wrappedDebugger.RemoveBreakpoint(breakpoint, runspaceId);

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
        /// SetDebuggerAction.
        /// </summary>
        /// <param name="resumeAction">Debugger resume action.</param>
        public override void SetDebuggerAction(DebuggerResumeAction resumeAction)
        {
            _wrappedDebugger.SetDebuggerAction(resumeAction);
        }

        /// <summary>
        /// Stops running command.
        /// </summary>
        public override void StopProcessCommand()
        {
            _wrappedDebugger.StopProcessCommand();
        }

        /// <summary>
        /// Returns current debugger stop event arguments if debugger is in
        /// debug stop state.  Otherwise returns null.
        /// </summary>
        /// <returns>DebuggerStopEventArgs.</returns>
        public override DebuggerStopEventArgs GetDebuggerStopArgs()
        {
            return _wrappedDebugger.GetDebuggerStopArgs();
        }

        /// <summary>
        /// Sets the debugger mode.
        /// </summary>
        /// <param name="mode">Debug mode.</param>
        public override void SetDebugMode(DebugModes mode)
        {
            _wrappedDebugger.SetDebugMode(mode);
        }

        /// <summary>
        /// Sets debugger stepping mode.
        /// </summary>
        /// <param name="enabled">True if stepping is to be enabled.</param>
        public override void SetDebuggerStepMode(bool enabled)
        {
            _wrappedDebugger.SetDebuggerStepMode(enabled);
        }

        /// <summary>
        /// Returns true if debugger is active.
        /// </summary>
        public override bool IsActive
        {
            get { return _wrappedDebugger.IsActive; }
        }

        /// <summary>
        /// Breaks into the debugger.
        /// </summary>
        /// <param name="triggerObject">The object that triggered the breakpoint, if there is one.</param>
        internal override void Break(object triggerObject = null)
        {
            _wrappedDebugger.Break(triggerObject);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose.
        /// </summary>
        public virtual void Dispose()
        {
            _isDisposed = true;

            if (_wrappedDebugger != null)
            {
                _wrappedDebugger.BreakpointUpdated -= HandleBreakpointUpdated;
                _wrappedDebugger.DebuggerStop -= HandleDebuggerStop;
            }

            _wrappedDebugger = null;
            _runspace = null;

            // Call GC.SuppressFinalize since this is an unsealed type, in case derived types
            // have finalizers.
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Protected Methods

        protected virtual void HandleDebuggerStop(object sender, DebuggerStopEventArgs e)
        {
            this.RaiseDebuggerStopEvent(e);
        }

        protected virtual void HandleBreakpointUpdated(object sender, BreakpointUpdatedEventArgs e)
        {
            this.RaiseBreakpointUpdatedEvent(e);
        }

        protected virtual DebuggerCommandResults HandlePromptCommand(PSDataCollection<PSObject> output)
        {
            // Nested debugged runspace prompt should look like:
            // [ComputerName]: [DBG]: [Process:<id>]: [RunspaceName]: PS C:\>
            string computerName = _runspace.ConnectionInfo?.ComputerName;
            const string processPartPattern = "{0}[{1}:{2}]:{3}";
            string processPart = StringUtil.Format(processPartPattern,
                @"""",
                DebuggerStrings.NestedRunspaceDebuggerPromptProcessName,
                @"$($PID)",
                @"""");
            const string locationPart = @"""PS $($executionContext.SessionState.Path.CurrentLocation)> """;
            string promptScript = "'[DBG]: '" + " + " + processPart + " + " + "' [" + CodeGeneration.EscapeSingleQuotedStringContent(_runspace.Name) + "]: '" + " + " + locationPart;

            // Get the command prompt from the wrapped debugger.
            PSCommand promptCommand = new PSCommand();
            promptCommand.AddScript(promptScript);
            PSDataCollection<PSObject> promptOutput = new PSDataCollection<PSObject>();
            _wrappedDebugger.ProcessCommand(promptCommand, promptOutput);
            string promptString = (promptOutput.Count == 1) ? promptOutput[0].BaseObject as string : string.Empty;
            var nestedPromptString = new System.Text.StringBuilder();

            // For remote runspaces display computer name in prompt.
            if (!string.IsNullOrEmpty(computerName))
            {
                nestedPromptString.Append("[" + computerName + "]:");
            }

            nestedPromptString.Append(promptString);

            // Fix up for non-remote runspaces since the runspace is not in a nested prompt
            // but the root runspace is.
            if (string.IsNullOrEmpty(computerName))
            {
                nestedPromptString.Insert(nestedPromptString.Length - 1, ">");
            }

            output.Add(nestedPromptString.ToString());

            return new DebuggerCommandResults(null, true);
        }

        protected virtual DebuggerCommandResults HandleCallStack(PSDataCollection<PSObject> output)
        {
            throw new PSNotImplementedException();
        }

        protected virtual bool HandleListCommand(PSDataCollection<PSObject> output)
        {
            return false;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Attempts to fix up the debugger stop invocation information so that
        /// the correct stack and source can be displayed in the debugger, for
        /// cases where the debugged runspace is called inside a parent script,
        /// such as with script Invoke-Command cases.
        /// </summary>
        /// <param name="debugStopInvocationInfo"></param>
        /// <returns>InvocationInfo.</returns>
        internal virtual InvocationInfo FixupInvocationInfo(InvocationInfo debugStopInvocationInfo)
        {
            // Default is no fix up.
            return debugStopInvocationInfo;
        }

        internal bool IsSameDebugger(Debugger testDebugger)
        {
            return _wrappedDebugger.Equals(testDebugger);
        }

        /// <summary>
        /// Checks to see if the runspace debugger is in a preserved debug
        /// stop state, and if so then allows the debugger stop event to
        /// continue processing and raise the event.
        /// </summary>
        internal void CheckStateAndRaiseStopEvent()
        {
            RemoteDebugger remoteDebugger = _wrappedDebugger as RemoteDebugger;
            if (remoteDebugger != null)
            {
                // Have remote debugger raise existing debugger stop event.
                remoteDebugger.CheckStateAndRaiseStopEvent();
            }
            else if (this._wrappedDebugger.IsPendingDebugStopEvent)
            {
                // Release local debugger preserved debugger stop event.
                this._wrappedDebugger.ReleaseSavedDebugStop();
            }
            else
            {
                // If this is a remote server debugger then we want to convert the pending remote
                // debugger stop to a local debugger stop event for this Debug-Runspace to handle.
                ServerRemoteDebugger serverRemoteDebugger = this._wrappedDebugger as ServerRemoteDebugger;
                serverRemoteDebugger?.ReleaseAndRaiseDebugStopLocal();
            }
        }

        /// <summary>
        /// Gets the callstack of the nested runspace.
        /// </summary>
        /// <returns></returns>
        internal PSDataCollection<PSObject> GetRSCallStack()
        {
            // Get call stack from wrapped debugger
            PSCommand cmd = new PSCommand();
            cmd.AddCommand("Get-PSCallStack");
            PSDataCollection<PSObject> callStackOutput = new PSDataCollection<PSObject>();
            _wrappedDebugger.ProcessCommand(cmd, callStackOutput);

            return callStackOutput;
        }

        #endregion
    }

    /// <summary>
    /// Wrapper class for runspace debugger where it is running in no known
    /// embedding scenario and is assumed to be running independently of
    /// any other running script.
    /// </summary>
    internal sealed class StandaloneRunspaceDebugger : NestedRunspaceDebugger
    {
        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="runspace">Runspace.</param>
        public StandaloneRunspaceDebugger(
            Runspace runspace)
            : base(runspace, PSMonitorRunspaceType.Standalone, Guid.Empty)
        { }

        #endregion

        #region Overrides

        protected override DebuggerCommandResults HandleCallStack(PSDataCollection<PSObject> output)
        {
            PSDataCollection<PSObject> callStackOutput = GetRSCallStack();

            // Display call stack info as formatted.
            using (PowerShell ps = PowerShell.Create())
            {
                ps.AddCommand("Out-String").AddParameter("Stream", true);
                ps.Invoke(callStackOutput, output);
            }

            return new DebuggerCommandResults(null, true);
        }

        protected override void HandleDebuggerStop(object sender, DebuggerStopEventArgs e)
        {
            object runningCmd = null;

            try
            {
                runningCmd = DrainAndBlockRemoteOutput();
                this.RaiseDebuggerStopEvent(e);
            }
            finally
            {
                RestoreRemoteOutput(runningCmd);
            }
        }

        #endregion

        #region Private Methods

        private object DrainAndBlockRemoteOutput()
        {
            // We do this only for remote runspaces.
            if (_runspace is not RemoteRunspace remoteRunspace) { return null; }

            var runningPowerShell = remoteRunspace.GetCurrentBasePowerShell();
            if (runningPowerShell != null)
            {
                runningPowerShell.WaitForServicingComplete();
                runningPowerShell.SuspendIncomingData();
                return runningPowerShell;
            }
            else
            {
                var runningPipe = remoteRunspace.GetCurrentlyRunningPipeline();
                if (runningPipe != null)
                {
                    runningPipe.DrainIncomingData();
                    runningPipe.SuspendIncomingData();
                    return runningPipe;
                }
            }

            return null;
        }

        private static void RestoreRemoteOutput(object runningCmd)
        {
            if (runningCmd == null) { return; }

            var runningPowerShell = runningCmd as PowerShell;
            if (runningPowerShell != null)
            {
                runningPowerShell.ResumeIncomingData();
            }
            else
            {
                var runningPipe = runningCmd as Pipeline;
                runningPipe?.ResumeIncomingData();
            }
        }

        #endregion
    }

    /// <summary>
    /// Wrapper class for runspace debugger where the runspace is being used in an
    /// embedded scenario such as Invoke-Command command inside script.
    /// </summary>
    internal sealed class EmbeddedRunspaceDebugger : NestedRunspaceDebugger
    {
        #region Members

        private PowerShell _command;
        private Debugger _rootDebugger;
        private ScriptBlockAst _parentScriptBlockAst;
        private DebuggerStopEventArgs _sendDebuggerArgs;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor for runspaces executing from script.
        /// </summary>
        /// <param name="runspace">Runspace to debug.</param>
        /// <param name="command">PowerShell command.</param>
        /// <param name="rootDebugger">Root debugger.</param>
        /// <param name="runspaceType">Runspace to monitor type.</param>
        /// <param name="parentDebuggerId">Parent debugger Id.</param>
        public EmbeddedRunspaceDebugger(
            Runspace runspace,
            PowerShell command,
            Debugger rootDebugger,
            PSMonitorRunspaceType runspaceType,
            Guid parentDebuggerId)
            : base(runspace, runspaceType, parentDebuggerId)
        {
            if (rootDebugger == null)
            {
                throw new PSArgumentNullException(nameof(rootDebugger));
            }

            _command = command;
            _rootDebugger = rootDebugger;
        }

        #endregion

        #region Overrides

        protected override void HandleDebuggerStop(object sender, DebuggerStopEventArgs e)
        {
            _sendDebuggerArgs = new DebuggerStopEventArgs(
                e.InvocationInfo,
                new Collection<Breakpoint>(e.Breakpoints),
                e.ResumeAction);

            object remoteRunningCmd = null;
            try
            {
                // For remote debugging drain/block output channel.
                remoteRunningCmd = DrainAndBlockRemoteOutput();

                this.RaiseDebuggerStopEvent(_sendDebuggerArgs);
            }
            finally
            {
                RestoreRemoteOutput(remoteRunningCmd);

                // Return user determined resume action.
                e.ResumeAction = _sendDebuggerArgs.ResumeAction;
            }
        }

        protected override DebuggerCommandResults HandleCallStack(PSDataCollection<PSObject> output)
        {
            // First get call stack from wrapped debugger
            PSCommand cmd = new PSCommand();
            cmd.AddCommand("Get-PSCallStack");
            PSDataCollection<PSObject> callStackOutput = new PSDataCollection<PSObject>();
            _wrappedDebugger.ProcessCommand(cmd, callStackOutput);

            // Next get call stack from parent debugger.
            PSDataCollection<CallStackFrame> callStack = _rootDebugger.GetCallStack().ToArray();

            // Combine call stack info.
            foreach (var item in callStack)
            {
                callStackOutput.Add(new PSObject(item));
            }

            // Display call stack info as formatted.
            using (PowerShell ps = PowerShell.Create())
            {
                ps.AddCommand("Out-String").AddParameter("Stream", true);
                ps.Invoke(callStackOutput, output);
            }

            return new DebuggerCommandResults(null, true);
        }

        protected override bool HandleListCommand(PSDataCollection<PSObject> output)
        {
            if ((_sendDebuggerArgs != null) && (_sendDebuggerArgs.InvocationInfo != null))
            {
                return _rootDebugger.InternalProcessListCommand(_sendDebuggerArgs.InvocationInfo.ScriptLineNumber, output);
            }

            return false;
        }

        /// <summary>
        /// Attempts to fix up the debugger stop invocation information so that
        /// the correct stack and source can be displayed in the debugger, for
        /// cases where the debugged runspace is called inside a parent script,
        /// such as with script Invoke-Command cases.
        /// </summary>
        /// <param name="debugStopInvocationInfo">Invocation information from debugger stop.</param>
        /// <returns>InvocationInfo.</returns>
        internal override InvocationInfo FixupInvocationInfo(InvocationInfo debugStopInvocationInfo)
        {
            if (debugStopInvocationInfo == null) { return null; }

            // Check to see if this nested debug stop is called from within
            // a known parent source.
            int dbStopLineNumber = debugStopInvocationInfo.ScriptLineNumber;
            CallStackFrame topItem = null;
            var parentActiveStack = _rootDebugger.GetActiveDebuggerCallStack();
            if ((parentActiveStack != null) && (parentActiveStack.Length > 0))
            {
                topItem = parentActiveStack[0];
            }
            else
            {
                var parentStack = _rootDebugger.GetCallStack().ToArray();
                if ((parentStack != null) && (parentStack.Length > 0))
                {
                    topItem = parentStack[0];
                    dbStopLineNumber--;
                }
            }

            InvocationInfo debugInvocationInfo = CreateInvocationInfoFromParent(
                topItem,
                dbStopLineNumber,
                debugStopInvocationInfo.ScriptPosition.StartColumnNumber,
                debugStopInvocationInfo.ScriptPosition.EndColumnNumber);

            return debugInvocationInfo ?? debugStopInvocationInfo;
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            _rootDebugger = null;
            _parentScriptBlockAst = null;
            _command = null;
            _sendDebuggerArgs = null;
        }

        #endregion

        #region Private Methods

        private InvocationInfo CreateInvocationInfoFromParent(
            CallStackFrame parentStackFrame,
            int debugLineNumber,
            int debugStartColNumber,
            int debugEndColNumber)
        {
            if (parentStackFrame == null) { return null; }

            // Attempt to find parent script file create script block with Ast to
            // find correct line and offset adjustments.
            if ((_parentScriptBlockAst == null) &&
                !string.IsNullOrEmpty(parentStackFrame.ScriptName) &&
                System.IO.File.Exists(parentStackFrame.ScriptName))
            {
                ParseError[] errors;
                Token[] tokens;
                _parentScriptBlockAst = Parser.ParseInput(
                    System.IO.File.ReadAllText(parentStackFrame.ScriptName),
                    out tokens, out errors);
            }

            if (_parentScriptBlockAst != null)
            {
                int callingLineNumber = parentStackFrame.ScriptLineNumber;

                StatementAst debugStatement = null;
                StatementAst callingStatement = _parentScriptBlockAst.Find(
                    ast => ast is StatementAst && (ast.Extent.StartLineNumber == callingLineNumber), true) as StatementAst;

                if (callingStatement != null)
                {
                    // Find first statement in calling statement.
                    StatementAst firstStatement = callingStatement.Find(
                        ast => ast is StatementAst && ast.Extent.StartLineNumber > callingLineNumber, true) as StatementAst;
                    if (firstStatement != null)
                    {
                        int adjustedLineNumber = firstStatement.Extent.StartLineNumber + debugLineNumber - 1;
                        debugStatement = callingStatement.Find(
                            ast => ast is StatementAst && ast.Extent.StartLineNumber == adjustedLineNumber, true) as StatementAst;
                    }
                }

                if (debugStatement != null)
                {
                    int endColNum = debugStartColNumber + (debugEndColNumber - debugStartColNumber) - 2;
                    string statementExtentText = FixUpStatementExtent(debugStatement.Extent.StartColumnNumber - 1, debugStatement.Extent.Text);

                    ScriptPosition scriptStartPosition = new ScriptPosition(
                        parentStackFrame.ScriptName,
                        debugStatement.Extent.StartLineNumber,
                        debugStartColNumber,
                        statementExtentText);

                    ScriptPosition scriptEndPosition = new ScriptPosition(
                        parentStackFrame.ScriptName,
                        debugStatement.Extent.EndLineNumber,
                        endColNum,
                        statementExtentText);

                    return InvocationInfo.Create(
                        parentStackFrame.InvocationInfo.MyCommand,
                        new ScriptExtent(
                            scriptStartPosition,
                            scriptEndPosition)
                        );
                }
            }

            return null;
        }

        private static string FixUpStatementExtent(int startColNum, string stateExtentText)
        {
            Text.StringBuilder sb = new Text.StringBuilder();
            sb.Append(' ', startColNum);
            sb.Append(stateExtentText);

            return sb.ToString();
        }

        private object DrainAndBlockRemoteOutput()
        {
            // We only do this for remote runspaces.
            if (_runspace is not RemoteRunspace) { return null; }

            try
            {
                if (_command != null)
                {
                    _command.WaitForServicingComplete();
                    _command.SuspendIncomingData();

                    return _command;
                }

                Pipeline runningCmd = _runspace.GetCurrentlyRunningPipeline();
                if (runningCmd != null)
                {
                    runningCmd.DrainIncomingData();
                    runningCmd.SuspendIncomingData();

                    return runningCmd;
                }
            }
            catch (PSNotSupportedException)
            { }

            return null;
        }

        private static void RestoreRemoteOutput(object runningCmd)
        {
            if (runningCmd == null) { return; }

            PowerShell command = runningCmd as PowerShell;
            if (command != null)
            {
                command.ResumeIncomingData();
            }
            else
            {
                Pipeline pipelineCommand = runningCmd as Pipeline;
                pipelineCommand?.ResumeIncomingData();
            }
        }

        #endregion
    }

    #endregion

    #region DebuggerCommandResults

    /// <summary>
    /// Command results returned from Debugger.ProcessCommand.
    /// </summary>
    public sealed class DebuggerCommandResults
    {
        #region Properties

        /// <summary>
        /// Resume action.
        /// </summary>
        public DebuggerResumeAction? ResumeAction
        {
            get;
            private set;
        }

        /// <summary>
        /// True if debugger evaluated command.  Otherwise evaluation was
        /// performed by PowerShell.
        /// </summary>
        public bool EvaluatedByDebugger { get; }

        #endregion

        #region Constructors

        private DebuggerCommandResults()
        { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="resumeAction">Resume action.</param>
        /// <param name="evaluatedByDebugger">True if evaluated by debugger.</param>
        public DebuggerCommandResults(
            DebuggerResumeAction? resumeAction,
            bool evaluatedByDebugger)
        {
            ResumeAction = resumeAction;
            EvaluatedByDebugger = evaluatedByDebugger;
        }

        #endregion
    }

    #endregion

    #region DebuggerCommandProcessor

    /// <summary>
    /// This class is used to pre-process the command read by the host when it is in debug mode; its
    /// main intention is to implement the debugger commands ("s", "c", "o", etc)
    /// </summary>
    internal class DebuggerCommandProcessor
    {
        // debugger commands
        private const string ContinueCommand = "continue";
        private const string ContinueShortcut = "c";
        private const string GetStackTraceShortcut = "k";
        private const string HelpCommand = "h";
        private const string HelpShortcut = "?";
        private const string ListCommand = "list";
        private const string ListShortcut = "l";
        private const string StepCommand = "stepInto";
        private const string StepShortcut = "s";
        private const string StepOutCommand = "stepOut";
        private const string StepOutShortcut = "o";
        private const string StepOverCommand = "stepOver";
        private const string StepOverShortcut = "v";
        private const string StopCommand = "quit";
        private const string StopShortcut = "q";
        private const string DetachCommand = "detach";
        private const string DetachShortcut = "d";

        // default line count for the list command
        private const int DefaultListLineCount = 16;

        // table of debugger commands
        private readonly Dictionary<string, DebuggerCommand> _commandTable;

        // the Help command
        private readonly DebuggerCommand _helpCommand;

        // the List command
        private readonly DebuggerCommand _listCommand;

        // last command processed
        private DebuggerCommand _lastCommand;

        // the source script split into lines
        private string[] _lines;

        // last line displayed by the list command
        private int _lastLineDisplayed;

        private const string Crlf = "\x000D\x000A";

        /// <summary>
        /// Creates the table of debugger commands.
        /// </summary>
        public DebuggerCommandProcessor()
        {
            _commandTable = new Dictionary<string, DebuggerCommand>(StringComparer.OrdinalIgnoreCase);
            _commandTable[StepCommand] = _commandTable[StepShortcut] = new DebuggerCommand(StepCommand, DebuggerResumeAction.StepInto, repeatOnEnter: true, executedByDebugger: false);
            _commandTable[StepOutCommand] = _commandTable[StepOutShortcut] = new DebuggerCommand(StepOutCommand, DebuggerResumeAction.StepOut, repeatOnEnter: false, executedByDebugger: false);
            _commandTable[StepOverCommand] = _commandTable[StepOverShortcut] = new DebuggerCommand(StepOverCommand, DebuggerResumeAction.StepOver, repeatOnEnter: true, executedByDebugger: false);
            _commandTable[ContinueCommand] = _commandTable[ContinueShortcut] = new DebuggerCommand(ContinueCommand, DebuggerResumeAction.Continue, repeatOnEnter: false, executedByDebugger: false);
            _commandTable[StopCommand] = _commandTable[StopShortcut] = new DebuggerCommand(StopCommand, DebuggerResumeAction.Stop, repeatOnEnter: false, executedByDebugger: false);
            _commandTable[GetStackTraceShortcut] = new DebuggerCommand("get-pscallstack", null, repeatOnEnter: false, executedByDebugger: false);
            _commandTable[HelpCommand] = _commandTable[HelpShortcut] = _helpCommand = new DebuggerCommand(HelpCommand, null, repeatOnEnter: false, executedByDebugger: true);
            _commandTable[ListCommand] = _commandTable[ListShortcut] = _listCommand = new DebuggerCommand(ListCommand, null, repeatOnEnter: true, executedByDebugger: true);
            _commandTable[string.Empty] = new DebuggerCommand(string.Empty, null, repeatOnEnter: false, executedByDebugger: true);
        }

        /// <summary>
        /// Resets any state in the command processor.
        /// </summary>
        public void Reset()
        {
            _lines = null;
        }

        /// <summary>
        /// Process the command read by the host and returns the DebuggerResumeAction or the command
        /// that the host should execute (see comments in the DebuggerCommand class above).
        /// </summary>
        public DebuggerCommand ProcessCommand(PSHost host, string command, InvocationInfo invocationInfo)
        {
            return _lastCommand = DoProcessCommand(host, command, invocationInfo, null);
        }

        /// <summary>
        /// ProcessCommand.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="command"></param>
        /// <param name="invocationInfo"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public DebuggerCommand ProcessCommand(PSHost host, string command, InvocationInfo invocationInfo, IList<PSObject> output)
        {
            DebuggerCommand dbgCommand = DoProcessCommand(host, command, invocationInfo, output);
            if (dbgCommand.ExecutedByDebugger || (dbgCommand.ResumeAction != null)) { _lastCommand = dbgCommand; }

            return dbgCommand;
        }

        /// <summary>
        /// Process list command with provided line number.
        /// </summary>
        /// <param name="invocationInfo">Current InvocationInfo.</param>
        /// <param name="output">Output.</param>
        public void ProcessListCommand(InvocationInfo invocationInfo, IList<PSObject> output)
        {
            DoProcessCommand(null, "list", invocationInfo, output);
        }

        /// <summary>
        /// Looks up string command and if it is a debugger command returns the
        /// corresponding DebuggerCommand object.
        /// </summary>
        /// <param name="command">String command.</param>
        /// <returns>DebuggerCommand or null.</returns>
        public DebuggerCommand ProcessBasicCommand(string command)
        {
            if (command.Length == 0 && _lastCommand != null && _lastCommand.RepeatOnEnter)
            {
                return _lastCommand;
            }

            DebuggerCommand debuggerCommand;
            if (_commandTable.TryGetValue(command, out debuggerCommand))
            {
                if (debuggerCommand.ExecutedByDebugger || (debuggerCommand.ResumeAction != null)) { _lastCommand = debuggerCommand; }

                return debuggerCommand;
            }

            return null;
        }

        /// <summary>
        /// Helper for ProcessCommand.
        /// </summary>
        private DebuggerCommand DoProcessCommand(PSHost host, string command, InvocationInfo invocationInfo, IList<PSObject> output)
        {
            // check for <enter>
            if (command.Length == 0 && _lastCommand != null && _lastCommand.RepeatOnEnter)
            {
                if (_lastCommand == _listCommand)
                {
                    if (_lines != null && _lastLineDisplayed < _lines.Length)
                    {
                        DisplayScript(host, output, invocationInfo, _lastLineDisplayed + 1, DefaultListLineCount);
                    }

                    return _listCommand;
                }

                command = _lastCommand.Command;
            }

            // Check for the list command using a regular expression
            Regex listCommandRegex = new Regex(@"^l(ist)?(\s+(?<start>\S+))?(\s+(?<count>\S+))?$", RegexOptions.IgnoreCase);

            Match match = listCommandRegex.Match(command);

            if (match.Success)
            {
                DisplayScript(host, output, invocationInfo, match);
                return _listCommand;
            }

            // Check for the rest of the debugger commands
            DebuggerCommand debuggerCommand = null;

            if (_commandTable.TryGetValue(command, out debuggerCommand))
            {
                // Check for the help command
                if (debuggerCommand == _helpCommand)
                {
                    DisplayHelp(host, output);
                }

                return debuggerCommand;
            }

            // Else return the same command
            return new DebuggerCommand(command, null, false, false);
        }

        /// <summary>
        /// Displays the help text for the debugger commands.
        /// </summary>
        private static void DisplayHelp(PSHost host, IList<PSObject> output)
        {
            WriteLine(string.Empty, host, output);
            WriteLine(StringUtil.Format(DebuggerStrings.StepHelp, StepShortcut, StepCommand), host, output);
            WriteLine(StringUtil.Format(DebuggerStrings.StepOverHelp, StepOverShortcut, StepOverCommand), host, output);
            WriteLine(StringUtil.Format(DebuggerStrings.StepOutHelp, StepOutShortcut, StepOutCommand), host, output);
            WriteLine(string.Empty, host, output);
            WriteLine(StringUtil.Format(DebuggerStrings.ContinueHelp, ContinueShortcut, ContinueCommand), host, output);
            WriteLine(StringUtil.Format(DebuggerStrings.StopHelp, StopShortcut, StopCommand), host, output);
            WriteLine(StringUtil.Format(DebuggerStrings.DetachHelp, DetachShortcut, DetachCommand), host, output);
            WriteLine(string.Empty, host, output);
            WriteLine(StringUtil.Format(DebuggerStrings.GetStackTraceHelp, GetStackTraceShortcut), host, output);
            WriteLine(string.Empty, host, output);
            WriteLine(StringUtil.Format(DebuggerStrings.ListHelp, ListShortcut, ListCommand), host, output);
            WriteLine(StringUtil.Format(DebuggerStrings.AdditionalListHelp1), host, output);
            WriteLine(StringUtil.Format(DebuggerStrings.AdditionalListHelp2), host, output);
            WriteLine(StringUtil.Format(DebuggerStrings.AdditionalListHelp3), host, output);
            WriteLine(string.Empty, host, output);
            WriteLine(StringUtil.Format(DebuggerStrings.EnterHelp, StepCommand, StepOverCommand, ListCommand), host, output);
            WriteLine(string.Empty, host, output);
            WriteLine(StringUtil.Format(DebuggerStrings.HelpCommandHelp, HelpShortcut, HelpCommand), host, output);
            WriteLine("\n", host, output);
            WriteLine(StringUtil.Format(DebuggerStrings.PromptHelp), host, output);
            WriteLine(string.Empty, host, output);
        }

        /// <summary>
        /// Executes the list command.
        /// </summary>
        private void DisplayScript(PSHost host, IList<PSObject> output, InvocationInfo invocationInfo, Match match)
        {
            if (invocationInfo == null) { return; }

            //
            // Get the source code for the script
            //
            if (_lines == null)
            {
                string scriptText = invocationInfo.GetFullScript();
                if (string.IsNullOrEmpty(scriptText))
                {
                    WriteErrorLine(StringUtil.Format(DebuggerStrings.NoSourceCode), host, output);
                    return;
                }

                _lines = scriptText.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            }

            //
            // Get the starting line
            //
            int start = Math.Max(invocationInfo.ScriptLineNumber - 5, 1);

            if (match.Groups["start"].Value.Length > 0)
            {
                try
                {
                    start = int.Parse(match.Groups["start"].Value, CultureInfo.CurrentCulture.NumberFormat);
                }
                catch
                {
                    WriteErrorLine(StringUtil.Format(DebuggerStrings.BadStartFormat, _lines.Length), host, output);
                    return;
                }

                if (start <= 0 || start > _lines.Length)
                {
                    WriteErrorLine(StringUtil.Format(DebuggerStrings.BadStartFormat, _lines.Length), host, output);
                    return;
                }
            }

            //
            // Get the line count
            //
            int count = DefaultListLineCount;

            if (match.Groups["count"].Value.Length > 0)
            {
                try
                {
                    count = int.Parse(match.Groups["count"].Value, CultureInfo.CurrentCulture.NumberFormat);
                }
                catch
                {
                    WriteErrorLine(StringUtil.Format(DebuggerStrings.BadCountFormat, _lines.Length), host, output);
                    return;
                }

                // Limit requested line count to maximum number of existing lines
                count = (count > _lines.Length) ? _lines.Length : count;

                if (count <= 0)
                {
                    WriteErrorLine(DebuggerStrings.BadCountFormat, host, output);
                    return;
                }
            }

            //
            // Execute the command
            //
            DisplayScript(host, output, invocationInfo, start, count);
        }

        /// <summary>
        /// Executes the list command.
        /// </summary>
        private void DisplayScript(PSHost host, IList<PSObject> output, InvocationInfo invocationInfo, int start, int count)
        {
            WriteCR(host, output);

            for (int lineNumber = start; lineNumber <= _lines.Length && lineNumber < start + count; lineNumber++)
            {
                WriteLine(
                    lineNumber == invocationInfo.ScriptLineNumber
                        ? string.Format(CultureInfo.CurrentCulture, "{0,5}:* {1}", lineNumber, _lines[lineNumber - 1])
                        : string.Format(CultureInfo.CurrentCulture, "{0,5}:  {1}", lineNumber, _lines[lineNumber - 1]),
                    host,
                    output);

                _lastLineDisplayed = lineNumber;
            }

            WriteCR(host, output);
        }

        private static void WriteLine(string line, PSHost host, IList<PSObject> output)
        {
            host?.UI.WriteLine(line);

            output?.Add(new PSObject(line));
        }

        private static void WriteCR(PSHost host, IList<PSObject> output)
        {
            host?.UI.WriteLine();

            output?.Add(new PSObject(Crlf));
        }

        private static void WriteErrorLine(string error, PSHost host, IList<PSObject> output)
        {
            host?.UI.WriteErrorLine(error);

            output?.Add(
                new PSObject(
                    new ErrorRecord(
                        new RuntimeException(error),
                        "DebuggerError",
                        ErrorCategory.InvalidOperation,
                        null)));
        }
    }

    /// <summary>
    /// Class used to hold the output of the DebuggerCommandProcessor.
    /// </summary>
    internal class DebuggerCommand
    {
        public DebuggerCommand(string command, DebuggerResumeAction? action, bool repeatOnEnter, bool executedByDebugger)
        {
            ResumeAction = action;
            Command = command;
            RepeatOnEnter = repeatOnEnter;
            ExecutedByDebugger = executedByDebugger;
        }

        /// <summary>
        /// If ResumeAction is not null it indicates that the host must exit the debugger
        /// and resume execution of the suspended pipeline; the debugger will use the
        /// value of this property to decide how to resume the pipeline (i.e. step into,
        /// step-over, continue, etc)
        /// </summary>
        public DebuggerResumeAction? ResumeAction { get; }

        /// <summary>
        /// When ResumeAction is null, this property indicates the command that the
        /// host should pass to the PowerShell engine.
        /// </summary>
        public string Command { get; }

        /// <summary>
        /// If true, the host should repeat this command if the next command in an empty line (enter)
        /// </summary>
        public bool RepeatOnEnter { get; }

        /// <summary>
        /// If true, the command was executed by the debugger and the host should ignore the command.
        /// </summary>
        public bool ExecutedByDebugger { get; }
    }

    #endregion

    #region PSDebugContext class

    /// <summary>
    /// This class exposes the information about the debugger that is available via $PSDebugContext.
    /// </summary>
    public class PSDebugContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PSDebugContext"/> class.
        /// </summary>
        /// <param name="invocationInfo">The invocation information for the current command.</param>
        /// <param name="breakpoints">The breakpoint(s) that caused the script to break in the debugger.</param>
        public PSDebugContext(InvocationInfo invocationInfo, List<Breakpoint> breakpoints)
            : this(invocationInfo, breakpoints, triggerObject: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PSDebugContext"/> class.
        /// </summary>
        /// <param name="invocationInfo">The invocation information for the current command.</param>
        /// <param name="breakpoints">The breakpoint(s) that caused the script to break in the debugger.</param>
        /// <param name="triggerObject">The object that caused the script to break in the debugger.</param>
        public PSDebugContext(InvocationInfo invocationInfo, List<Breakpoint> breakpoints, object triggerObject)
        {
            if (breakpoints == null)
            {
                throw new PSArgumentNullException(nameof(breakpoints));
            }

            this.InvocationInfo = invocationInfo;
            this.Breakpoints = breakpoints.ToArray();
            this.Trigger = triggerObject;
        }

        /// <summary>
        /// InvocationInfo of the command currently being executed.
        /// </summary>
        public InvocationInfo InvocationInfo { get; }

        /// <summary>
        /// If not empty, indicates that the execution was suspended because one or more breakpoints
        /// were hit. Otherwise, the execution was suspended as part of a step operation.
        /// </summary>
        public Breakpoint[] Breakpoints { get; }

        /// <summary>
        /// Gets the object that triggered the current dynamic breakpoint.
        /// </summary>
        public object Trigger { get; }
    }

    #endregion

    #region CallStackFrame class

    /// <summary>
    /// A call stack item returned by the Get-PSCallStack cmdlet.
    /// </summary>
    public sealed class CallStackFrame
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="invocationInfo">Invocation Info.</param>
        public CallStackFrame(InvocationInfo invocationInfo)
            : this(null, invocationInfo)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="functionContext">Function context.</param>
        /// <param name="invocationInfo">Invocation Info.</param>
        internal CallStackFrame(FunctionContext functionContext, InvocationInfo invocationInfo)
        {
            if (invocationInfo == null)
            {
                throw new PSArgumentNullException(nameof(invocationInfo));
            }

            if (functionContext != null)
            {
                this.InvocationInfo = invocationInfo;
                FunctionContext = functionContext;
                this.Position = functionContext.CurrentPosition;
            }
            else
            {
                // WF functions do not have functionContext.  Use InvocationInfo.
                this.InvocationInfo = invocationInfo;
                this.Position = invocationInfo.ScriptPosition;
                FunctionContext = new FunctionContext();
                FunctionContext._functionName = invocationInfo.ScriptName;
            }
        }

        /// <summary>
        /// File name of the current location, or null if the frame is not associated to a script.
        /// </summary>
        public string ScriptName
        {
            get { return Position.File; }
        }

        /// <summary>
        /// Line number of the current location, or 0 if the frame is not associated to a script.
        /// </summary>
        public int ScriptLineNumber
        {
            get { return Position.StartLineNumber; }
        }

        /// <summary>
        /// The InvocationInfo of the command.
        /// </summary>
        public InvocationInfo InvocationInfo { get; }

        /// <summary>
        /// The position information for the current position in the frame.  Null if the frame is not
        /// associated with a script.
        /// </summary>
        public IScriptExtent Position { get; }

        /// <summary>
        /// The name of the function associated with this frame.
        /// </summary>
        public string FunctionName { get { return FunctionContext._functionName; } }

        internal FunctionContext FunctionContext { get; }

        /// <summary>
        /// Returns a formatted string containing the ScriptName and ScriptLineNumber.
        /// </summary>
        public string GetScriptLocation()
        {
            if (string.IsNullOrEmpty(this.ScriptName))
            {
                return DebuggerStrings.NoFile;
            }

            return StringUtil.Format(DebuggerStrings.LocationFormat, Path.GetFileName(this.ScriptName), this.ScriptLineNumber);
        }

        /// <summary>
        /// Return a dictionary with the names and values of variables that are "local"
        /// to the frame.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, PSVariable> GetFrameVariables()
        {
            var result = new Dictionary<string, PSVariable>(StringComparer.OrdinalIgnoreCase);

            if (FunctionContext._executionContext == null) { return result; }

            var scope = FunctionContext._executionContext.EngineSessionState.CurrentScope;
            while (scope != null)
            {
                if (scope.LocalsTuple == FunctionContext._localsTuple)
                {
                    // We can ignore any dotted scopes.
                    break;
                }

                if (scope.DottedScopes != null && scope.DottedScopes.Any(s => s == FunctionContext._localsTuple))
                {
                    var dottedScopes = scope.DottedScopes.ToArray();

                    int i;
                    // Skip dotted scopes above the current scope
                    for (i = 0; i < dottedScopes.Length; ++i)
                    {
                        if (dottedScopes[i] == FunctionContext._localsTuple)
                            break;
                    }

                    for (; i < dottedScopes.Length; ++i)
                    {
                        dottedScopes[i].GetVariableTable(result, true);
                    }

                    break;
                }

                scope = scope.Parent;
            }

            FunctionContext._localsTuple.GetVariableTable(result, true);

            return result;
        }

        /// <summary>
        /// ToString override.
        /// </summary>
        public override string ToString()
        {
            return StringUtil.Format(DebuggerStrings.StackTraceFormat, FunctionName,
                                     ScriptName ?? DebuggerStrings.NoFile, ScriptLineNumber);
        }
    }

    #endregion
}

namespace System.Management.Automation.Internal
{
    #region DebuggerUtils

    /// <summary>
    /// Debugger Utilities class.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Justification = "Needed Internal use only")]
    public static class DebuggerUtils
    {
        private static readonly SortedSet<string> s_noHistoryCommandNames = new SortedSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "prompt",
            "Set-PSDebuggerAction",
            "Get-PSDebuggerStopArgs",
            "Set-PSDebugMode",
            "TabExpansion2"
        };

        /// <summary>
        /// Helper method to determine if command should be added to debugger
        /// history.
        /// </summary>
        /// <param name="command">Command string.</param>
        /// <returns>True if command can be added to history.</returns>
        public static bool ShouldAddCommandToHistory(string command)
        {
            if (command == null)
            {
                throw new PSArgumentNullException(nameof(command));
            }

            lock (s_noHistoryCommandNames)
            {
                return !(s_noHistoryCommandNames.Contains(command, StringComparer.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Start monitoring a runspace on the target debugger.
        /// </summary>
        /// <param name="debugger">Target debugger.</param>
        /// <param name="runspaceInfo">PSMonitorRunspaceInfo.</param>
        public static void StartMonitoringRunspace(Debugger debugger, PSMonitorRunspaceInfo runspaceInfo)
        {
            if (debugger == null)
            {
                throw new PSArgumentNullException(nameof(debugger));
            }

            if (runspaceInfo == null)
            {
                throw new PSArgumentNullException(nameof(runspaceInfo));
            }

            debugger.StartMonitoringRunspace(runspaceInfo);
        }

        /// <summary>
        /// End monitoring a runspace on the target debugger.
        /// </summary>
        /// <param name="debugger">Target debugger.</param>
        /// <param name="runspaceInfo">PSMonitorRunspaceInfo.</param>
        public static void EndMonitoringRunspace(Debugger debugger, PSMonitorRunspaceInfo runspaceInfo)
        {
            if (debugger == null)
            {
                throw new PSArgumentNullException(nameof(debugger));
            }

            if (runspaceInfo == null)
            {
                throw new PSArgumentNullException(nameof(runspaceInfo));
            }

            debugger.EndMonitoringRunspace(runspaceInfo);
        }
    }

    #region PSMonitorRunspaceEvent

    /// <summary>
    /// PSMonitorRunspaceEvent.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Justification = "Needed Internal use only")]
    public enum PSMonitorRunspaceType
    {
        /// <summary>
        /// Standalone runspace.
        /// </summary>
        Standalone = 0,

        /// <summary>
        /// Runspace from remote Invoke-Command script.
        /// </summary>
        InvokeCommand,
    }

    /// <summary>
    /// Runspace information for monitoring runspaces for debugging.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Justification = "Needed Internal use only")]
    public abstract class PSMonitorRunspaceInfo
    {
        #region Properties

        /// <summary>
        /// Created Runspace.
        /// </summary>
        public Runspace Runspace { get; }

        /// <summary>
        /// Type of runspace for monitoring.
        /// </summary>
        public PSMonitorRunspaceType RunspaceType { get; }

        /// <summary>
        /// Nested debugger wrapper for runspace debugger.
        /// </summary>
        internal NestedRunspaceDebugger NestedDebugger { get; set; }

        #endregion

        #region Constructors

        private PSMonitorRunspaceInfo() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="runspace">Runspace.</param>
        /// <param name="runspaceType">Runspace type.</param>
        protected PSMonitorRunspaceInfo(
            Runspace runspace,
            PSMonitorRunspaceType runspaceType)
        {
            if (runspace == null)
            {
                throw new PSArgumentNullException(nameof(runspace));
            }

            Runspace = runspace;
            RunspaceType = runspaceType;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns a copy of this object.
        /// </summary>
        /// <returns></returns>
        internal abstract PSMonitorRunspaceInfo Copy();

        /// <summary>
        /// Creates an instance of a NestedRunspaceDebugger.
        /// </summary>
        /// <param name="rootDebugger">Root debugger or null.</param>
        /// <returns>NestedRunspaceDebugger.</returns>
        internal abstract NestedRunspaceDebugger CreateDebugger(Debugger rootDebugger);

        #endregion
    }

    /// <summary>
    /// Standalone runspace information for monitoring runspaces for debugging.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Justification = "Needed Internal use only")]
    public sealed class PSStandaloneMonitorRunspaceInfo : PSMonitorRunspaceInfo
    {
        #region Constructor

        /// <summary>
        /// Creates instance of PSStandaloneMonitorRunspaceInfo.
        /// </summary>
        /// <param name="runspace">Runspace to monitor.</param>
        public PSStandaloneMonitorRunspaceInfo(
            Runspace runspace)
            : base(runspace, PSMonitorRunspaceType.Standalone)
        { }

        #endregion

        #region Overrides

        /// <summary>
        /// Returns a copy of this object.
        /// </summary>
        /// <returns></returns>
        internal override PSMonitorRunspaceInfo Copy()
        {
            return new PSStandaloneMonitorRunspaceInfo(Runspace);
        }

        /// <summary>
        /// Creates an instance of a NestedRunspaceDebugger.
        /// </summary>
        /// <param name="rootDebugger">Root debugger or null.</param>
        /// <returns>NestedRunspaceDebugger wrapper.</returns>
        internal override NestedRunspaceDebugger CreateDebugger(Debugger rootDebugger)
        {
            return new StandaloneRunspaceDebugger(Runspace);
        }

        #endregion
    }

    /// <summary>
    /// Embedded runspaces running in context of a parent script, used for monitoring
    /// runspace debugging.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Justification = "Needed Internal use only")]
    public sealed class PSEmbeddedMonitorRunspaceInfo : PSMonitorRunspaceInfo
    {
        #region Properties

        /// <summary>
        /// PowerShell command to run.  Can be null.
        /// </summary>
        public PowerShell Command { get; }

        /// <summary>
        /// Unique parent debugger identifier.
        /// </summary>
        public Guid ParentDebuggerId { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates instance of PSEmbeddedMonitorRunspaceInfo.
        /// </summary>
        /// <param name="runspace">Runspace to monitor.</param>
        /// <param name="runspaceType">Type of runspace.</param>
        /// <param name="command">Running command.</param>
        /// <param name="parentDebuggerId">Unique parent debugger id or null.</param>
        public PSEmbeddedMonitorRunspaceInfo(
            Runspace runspace,
            PSMonitorRunspaceType runspaceType,
            PowerShell command,
            Guid parentDebuggerId)
            : base(runspace, runspaceType)
        {
            Command = command;
            ParentDebuggerId = parentDebuggerId;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Returns a copy of this object.
        /// </summary>
        /// <returns></returns>
        internal override PSMonitorRunspaceInfo Copy()
        {
            return new PSEmbeddedMonitorRunspaceInfo(
                Runspace,
                RunspaceType,
                Command,
                ParentDebuggerId);
        }

        /// <summary>
        /// Creates an instance of a NestedRunspaceDebugger.
        /// </summary>
        /// <param name="rootDebugger">Root debugger or null.</param>
        /// <returns>NestedRunspaceDebugger wrapper.</returns>
        internal override NestedRunspaceDebugger CreateDebugger(Debugger rootDebugger)
        {
            return new EmbeddedRunspaceDebugger(
                Runspace,
                Command,
                rootDebugger,
                RunspaceType,
                ParentDebuggerId);
        }

        #endregion
    }

    #endregion

    #endregion
}
