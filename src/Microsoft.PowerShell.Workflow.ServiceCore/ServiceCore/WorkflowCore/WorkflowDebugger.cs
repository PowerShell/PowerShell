/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;
using System.Activities;
using System.Management.Automation;
using System.Management.Automation.Tracing;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Language;
using System.Management.Automation.Host;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Activities.Tracking;
using Microsoft.PowerShell.Activities;
using Microsoft.PowerShell.Commands;
using System.Collections.ObjectModel;
using System.Activities.Hosting;
using System.Xml;
using System.Linq;
using System.Management.Automation.Security;
using Dbg = System.Diagnostics.Debug;

namespace Microsoft.PowerShell.Workflow
{
    #region PSWorkflowDebuggerMode

    /// <summary>
    /// PSWorkflowDebuggerMode
    /// </summary>
    internal enum WorkflowDebugMode
    {
        /// <summary>
        /// Debugger is off.
        /// </summary>
        Off = 0,

        /// <summary>
        /// Debugger stops at breakpoints.
        /// </summary>
        StopAtBreakpoint,

        /// <summary>
        /// Debugger stops at each Activity within a workflow function,
        /// and will step over nested workflow functions.
        /// </summary>
        StepToNextActivity,

        /// <summary>
        /// Debugger steps into nested workflow functions.
        /// </summary>
        StepIntoFunction,

        /// <summary>
        /// Debugger steps out of nested workflow functions.
        /// </summary>
        StepOutOfFunction
    }

    #endregion

    #region DebuggerActivityPosition

    internal sealed class ActivityPosition
    {
        #region Members

        private Tuple<string, int, int> _activityPosition;

        #endregion

        #region Constructors

        private ActivityPosition() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="line"></param>
        /// <param name="col"></param>
        internal ActivityPosition(
            string name,
            int line,
            int col)
        {
            _activityPosition = new Tuple<string, int, int>(
                (string.IsNullOrEmpty(name)) ? string.Empty : name,
                line,
                col);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Workflow function name
        /// </summary>
        internal string Name
        {
            get { return _activityPosition.Item1; }
        }

        /// <summary>
        /// Script line position
        /// </summary>
        internal int Line
        {
            get { return _activityPosition.Item2; }
        }

        /// <summary>
        /// Script column position
        /// </summary>
        internal int Column
        {
            get { return _activityPosition.Item3; }
        }

        #endregion
    }

    #endregion

    /// <summary>
    /// Workflow Debugger
    /// </summary>
    internal sealed class PSWorkflowDebugger : Debugger, IDisposable
    {
        #region Members

        private Debugger _parent;
        private Dictionary<int, LineBreakpoint> _lineBreakPoints;
        private Dictionary<int, VariableBreakpoint> _variableBreakPoints;
        private Dictionary<int, CommandBreakpoint> _commandBreakpoints;
        private Dictionary<int, Breakpoint> _initalParentBreakpoints;
        private Dictionary<string, object> _variables;
        private Dictionary<string, object> _wfMetaVariables;
        private PSWorkflowInstance _wfInstance;
        private WorkflowDebugMode _mode;
        private string _scriptName;
        private string _script;
        private string[] _scriptLines;
        private string _xamlDefinition;
        private string _outerFnXamlDefinition;
        private SortedSet<int> _validWFLineNumbers;
        private DebuggerStopEventArgs _debuggerStopEventArgs;
        private Stack<InvocationInfo> _nestedCallStack;
        private IEqualityComparer<InvocationInfo> _invocationComparer;
        private int _breakFrame;
        private Dictionary<string, DebugSource> _funcToSourceMap;
        private Dictionary<string, SortedSet<int>> _funcToValidLineNumbersMap;

        private Runspace _runspace;
        private System.Management.Automation.PowerShell _psDebuggerCommand;
        private IAsyncResult _psAsyncResult;
        private PSHost _host;
        private PathInfo _parentPath;

        private const string DefaultWFPrompt = "PS>> ";
        private const string WFDebugPrompt = "[WFDBG:{0}]: ";
        private const string DebugContextName = "PSDebugContext";

        #endregion

        #region Constructors

        private PSWorkflowDebugger()
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="wfInstance">PSWorkflowInstance</param>
        internal PSWorkflowDebugger(PSWorkflowInstance wfInstance)
        {
            if (wfInstance == null)
            {
                throw new PSArgumentNullException("wfInstance");
            }

            _wfInstance = wfInstance;
            _lineBreakPoints = new Dictionary<int, LineBreakpoint>();
            _variableBreakPoints = new Dictionary<int, VariableBreakpoint>();
            _commandBreakpoints = new Dictionary<int, CommandBreakpoint>();
            _initalParentBreakpoints = new Dictionary<int, Breakpoint>();
            _variables = new Dictionary<string, object>();
            _wfMetaVariables = new Dictionary<string, object>();
            _nestedCallStack = new Stack<InvocationInfo>();
            _invocationComparer = new InvocationInfoComparer();
            _mode = WorkflowDebugMode.Off;
            _funcToSourceMap = new Dictionary<string, DebugSource>();
            _funcToValidLineNumbersMap = new Dictionary<string, SortedSet<int>>();
        }

        #endregion

        #region Debugger Overrides

        /// <summary>
        /// ProcessCommand
        /// </summary>
        /// <param name="command">PSCommand</param>
        /// <param name="output"></param>
        /// <returns>DebuggerCommandResults</returns>
        public override DebuggerCommandResults ProcessCommand(PSCommand command, PSDataCollection<PSObject> output)
        {
            if (command == null)
            {
                throw new PSArgumentNullException("command");
            }

            if (output == null)
            {
                throw new PSArgumentNullException("output");
            }

            if (!DebuggerStopped)
            {
                throw new PSInvalidOperationException(Resources.ProcessDebugCommandNotInDebugStopMode);
            }

            // First give debugger chance to handle command.
            DebuggerCommandResults results = InternalProcessCommand(command, output);
            if (results != null) { return results; }

            // Check to see if command modifies Workflow variables, which is not allowed.
            ErrorRecord varChangedError;
            if (CheckForWorkflowVariableChange(command, output, out varChangedError))
            {
                Dbg.Assert(varChangedError != null, "Error record cannot be null.");

                // Command attempted to modify a workflow variable.
                // Write error message and return.
                PSCommand errorCommand = new PSCommand();
                errorCommand.AddCommand("Write-Error").AddParameter("ErrorRecord", varChangedError);

                if ((_host != null) &&
                    !(_host.GetType().FullName.Equals("System.Management.Automation.Remoting.ServerRemoteHost", StringComparison.OrdinalIgnoreCase)))
                {
                    errorCommand.AddCommand("Out-Default");
                }

                return RunPowerShellCommand(errorCommand, null, output, false);
            }

            bool addToHistory = CheckAddToHistory(command);

            return RunPowerShellCommand(command, null, output, addToHistory);
        }

        /// <summary>
        /// StopProcessCommand
        /// </summary>
        public override void StopProcessCommand()
        {
            System.Management.Automation.PowerShell ps = _psDebuggerCommand;
            if (ps != null)
            {
                ps.BeginStop(null, null);
            }
        }

        /// <summary>
        /// SetDebuggerAction
        /// </summary>
        /// <param name="resumeAction">DebuggerResumeAction to set</param>
        public override void SetDebuggerAction(DebuggerResumeAction resumeAction)
        {
            switch (resumeAction)
            {
                case DebuggerResumeAction.Continue:
                    _mode = WorkflowDebugMode.StopAtBreakpoint;
                    break;

                case DebuggerResumeAction.StepOut:
                    _mode = WorkflowDebugMode.StepOutOfFunction;
                    break;

                case DebuggerResumeAction.StepOver:
                    _mode = WorkflowDebugMode.StepToNextActivity;
                    break;

                case DebuggerResumeAction.StepInto:
                    _mode = WorkflowDebugMode.StepIntoFunction;
                    break;

                case DebuggerResumeAction.Stop:
                    _mode = WorkflowDebugMode.Off;
                    break;

                default:
                    _mode = WorkflowDebugMode.StopAtBreakpoint;
                    break;
            }
        }

        /// <summary>
        /// DebuggerStopEventArgs
        /// </summary>
        /// <returns>DebuggerStopEventArgs</returns>
        public override DebuggerStopEventArgs GetDebuggerStopArgs()
        {
            return _debuggerStopEventArgs;
        }

        /// <summary>
        /// Sets the parent debugger and breakpoints.
        /// </summary>
        /// <param name="parent">Parent debugger</param>
        /// <param name="breakPoints">List of breakpoints</param>
        /// <param name="mode">Debugger mode</param>
        /// <param name="host">host</param>
        /// <param name="path">Current path</param>
        public override void SetParent(
            Debugger parent,
            IEnumerable<Breakpoint> breakPoints,
            DebuggerResumeAction? mode,
            PSHost host,
            PathInfo path)
        {
            if (parent == null) { throw new PSArgumentNullException("parent"); }

            _parent = parent;
            _host = host;
            _parentPath = path;

            // Add breakpoints for this workflow.
            if (breakPoints != null)
            {
                _lineBreakPoints.Clear();
                _variableBreakPoints.Clear();
                _commandBreakpoints.Clear();
                foreach (var bp in breakPoints)
                {
                    // Add breakpoints to local collections.
                    AddBreakpoint(bp);
                }
            }

            if (mode != null)
            {
                SetDebuggerAction(mode.Value);
            }
        }

        /// <summary>
        /// Sets the parent debugger, breakpoints and function source map.
        /// </summary>
        /// <param name="parent">Parent debugger</param>
        /// <param name="breakPoints">List of breakpoints</param>
        /// <param name="mode">Debugger mode</param>
        /// <param name="host">host</param>
        /// <param name="path">Current path</param>
        /// <param name="functionSourceMap">Function to source map</param>
        public override void SetParent(
            Debugger parent,
            IEnumerable<Breakpoint> breakPoints,
            DebuggerResumeAction? mode,
            PSHost host,
            PathInfo path,
            Dictionary<string, DebugSource> functionSourceMap)
        {
            if (functionSourceMap == null) { throw new PSArgumentNullException("functionSourceMap"); }

            // Set parent info using original API.
            SetParent(parent, breakPoints, mode, host, path);

            // Add WF function to source information.
            foreach (var item in functionSourceMap)
            {
                if (!_funcToSourceMap.ContainsKey(item.Key))
                {
                    _funcToSourceMap.Add(item.Key, item.Value);
                }
            }
        }

        /// <summary>
        /// Returns IEnumerable of CallStackFrame objects.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<CallStackFrame> GetCallStack()
        {
            Debugger parentDebugger = _parent;
            if (parentDebugger != null)
            {
                // First return WF call stack items.
                foreach (var invocationInfo in _nestedCallStack)
                {
                    yield return new CallStackFrame(invocationInfo);
                }

                // Then return parent script call stack items.
                foreach (var frame in parentDebugger.GetCallStack())
                {
                    yield return frame;
                }
            }
        }

        /// <summary>
        /// Sets debugger stepping mode.
        /// </summary>
        /// <param name="enabled">True if stepping is to be enabled</param>
        public override void SetDebuggerStepMode(bool enabled)
        {
            if (enabled)
            {
                SetDebuggerAction(DebuggerResumeAction.StepInto);
            }
            else
            {
                SetDebuggerAction(DebuggerResumeAction.Continue);
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (_runspace != null)
            {
                _runspace.Debugger.BreakpointUpdated -= HandleRunspaceBreakpointUpdated;
                _runspace.Dispose();
                _runspace = null;
            }

            _parent = null;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// DebuggerCheck
        /// </summary>
        /// <param name="activityPosition">Defines activity execution position in XAML definition</param>
        internal void DebuggerCheck(
            ActivityPosition activityPosition)
        {
            string funcName = activityPosition.Name;

            //
            // PSWorkflowInstance.PSWorkflowDefinition may be null when debugger 
            // object is created.  Lazily check and create Xaml related info when
            // needed.
            //
            if (!CheckXamlInfo())
            {
                return;
            }

            //
            // PSWorkflowInstance.PSWorkflowJob may be null when debugger
            // object is created.  Lazily check and create script info when
            // needed.
            //
            if (!CheckWorkflowJob(funcName))
            {
                return;
            }

            // Update script information with current WF function.
            UpdateScriptInfo(funcName);

            // Update call stack with current WF function.
            if (!UpdateCallStack(activityPosition)) 
            { 
                return;
            }

            // Let debugger update script and callstack information but
            // if debugger is off or not subscribed-to then return here.
            if (!IsDebuggerStopEventSubscribed() ||
                (_mode == WorkflowDebugMode.Off))
            {
                return;
            }

            SetWorkflowVariablesToRunspace();

            DebuggerStopEventArgs args = EvaluateForStop(activityPosition);
            if (args != null)
            {
                SetWorkflowDebuggerContextVariable(args);

                _debuggerStopEventArgs = new DebuggerStopEventArgs(
                    args.InvocationInfo,
                    new Collection<Breakpoint>(args.Breakpoints),
                    args.ResumeAction);

                try
                {
                    // Blocking call.
                    RaiseDebuggerStopEvent(args);
                }
                finally
                {
                    _debuggerStopEventArgs = null;
                }

                // Update debugger mode based on resumeAction.
                SetDebuggerAction(args.ResumeAction);

                if (args.ResumeAction == DebuggerResumeAction.Stop)
                {
                    _nestedCallStack.Clear();

                    if (_wfInstance.PSWorkflowJob != null)
                    {
                        // Stop and remove job from job manager.
                        _wfInstance.PSWorkflowJob.StopJobAsync(true,
                            Resources.JobStoppedByDebugger, true);
                    }
                }
            }

            RemoveWorkflowVariablesFromRunspace();
        }

        /// <summary>
        /// UpdateVariables
        /// </summary>
        /// <param name="data"></param>
        internal void UpdateVariables(IDictionary<string, object> data)
        {
            if (!IsDebuggerStopEventSubscribed() ||
                (_mode == WorkflowDebugMode.Off))
            {
                return;
            }

            this._variables = new Dictionary<string, object>(data);
        }

        #endregion

        #region Private Methods

        private bool UpdateCallStack(
            ActivityPosition activityPosition)
        {
            var invocationInfo = CreateInvocationInfo(activityPosition);
            if (invocationInfo == null) { return false; }
            if (_nestedCallStack.Contains<InvocationInfo>(invocationInfo, _invocationComparer))
            {
                // Unwind stack to this position and update invocation info.
                // We can do this because workflows does not allow recursion calls
                // and each workflow function name on the stack must be unique.
                while ((_nestedCallStack.Count > 0) &&
                       (_invocationComparer.Equals(_nestedCallStack.Peek(), invocationInfo) == false))
                {
                    _nestedCallStack.Pop();
                }

                Dbg.Assert(_nestedCallStack.Count > 0, "Should have unwound stack to known location.");

                // Replace invocationInfo item on stack.
                _nestedCallStack.Pop();
                _nestedCallStack.Push(invocationInfo);
            }
            else
            {
                // Push invocationInfo item on top of stack.
                _nestedCallStack.Push(invocationInfo);
            }

            return true;
        }

        private DebuggerStopEventArgs EvaluateForStop(
                    ActivityPosition activityPosition)
        {
            WorkflowDebugMode mode = _mode;
            Breakpoint bp = null;
            DebuggerResumeAction resumeAction = DebuggerResumeAction.Continue;
            bool doStop = false;

            switch (mode)
            {
                case WorkflowDebugMode.StopAtBreakpoint:
                    resumeAction = DebuggerResumeAction.Continue;
                    bp = FindBreakpointHit(activityPosition);
                    doStop = (bp != null);
                    break;

                case WorkflowDebugMode.StepToNextActivity:
                    resumeAction = DebuggerResumeAction.StepOver;
                    doStop = (_nestedCallStack.Count <= _breakFrame);
                    break;

                case WorkflowDebugMode.StepIntoFunction:
                    resumeAction = DebuggerResumeAction.StepInto;
                    doStop = true;
                    break;

                case WorkflowDebugMode.StepOutOfFunction:
                    resumeAction = DebuggerResumeAction.StepOut;
                    doStop = (_nestedCallStack.Count < _breakFrame);
                    break;
            }

            if (!doStop &&
                (mode == WorkflowDebugMode.StepOutOfFunction ||
                 mode == WorkflowDebugMode.StepToNextActivity))
            {
                // If we are in step over/out mode check break point, which
                // overrides over/out action.
                bp = FindBreakpointHit(activityPosition);
                doStop = (bp != null);
            }

            if (doStop)
            {
                // Update current break frame value.
                _breakFrame = _nestedCallStack.Count;
                Dbg.Assert(_breakFrame > 0, "Must always have a non zero call stack count at debugger stop.");

                return new DebuggerStopEventArgs(
                    CreateInvocationInfo(activityPosition),
                    (bp != null) ? new Collection<Breakpoint>() { bp } : new Collection<Breakpoint>(),
                    resumeAction);
            }

            return null;
        }

        private Breakpoint FindBreakpointHit(
            ActivityPosition activityPosition)
        {
            // Line breakpoint.
            foreach (var bp in _lineBreakPoints.Values)
            {
                LineBreakpoint lineBp = bp as LineBreakpoint;
                if (CheckLineBreakpoint(lineBp, activityPosition.Line))
                {
                    return lineBp;
                }
            }

            // Command breakpoint.
            // Variable breakpoint.

            return null;
        }

        private bool CheckLineBreakpoint(
            LineBreakpoint bp, 
            int activityLine)
        {
            if (bp == null || !bp.Enabled) { return false; }

            // Check for exact line match.
            bool isGoodBreakpoint = (bp.Line == activityLine);

            if (!isGoodBreakpoint && (_validWFLineNumbers != null) &&
                ((_validWFLineNumbers.Count > 0) && (!_validWFLineNumbers.Contains(bp.Line))))
            {
                // If requested breakpoint is not valid, check for a close line match.
                // Currently this means a breakpoint within +/1 one script line.
                isGoodBreakpoint = (Math.Abs(bp.Line - activityLine) == 1) ? true : false;
            }

            if (!isGoodBreakpoint)
            {
                return isGoodBreakpoint;
            }

            // Check to see if this is a valid line breakpoint with an Action
            // script block and if so evaluate the script block.
            if (bp.Action != null)
            {
                ErrorRecord errorRecord;
                isGoodBreakpoint = CheckBreakpointAction(bp, out errorRecord);

                if (!isGoodBreakpoint && (errorRecord != null) &&
                    (_host != null) && (_host.UI != null))
                {
                    _host.UI.WriteErrorLine(errorRecord.Exception.Message);
                }
            }

            if (!isGoodBreakpoint)
            {
                return isGoodBreakpoint;
            }

            // Validate breakpoint with current source file.
            if ((bp.Script != null) &&
                (_scriptName != null))
            {
                isGoodBreakpoint = bp.Script.Equals(_scriptName, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                isGoodBreakpoint = false;
            }

            return isGoodBreakpoint;
        }

        private bool CheckBreakpointAction(LineBreakpoint bp, out ErrorRecord errorRecord)
        {
            errorRecord = null;
            bool validBreakpoint = false;

            try
            {
                PSCommand cmd = new PSCommand();
                cmd.AddScript(bp.Action.ToString());
                if (CheckForWorkflowVariableChange(cmd, null, out errorRecord)) { return false; }
                RunPowerShellCommand(cmd, null, null, false, true);
            }
            catch (BreakException)
            {
                validBreakpoint = true;
            }
            catch (Exception e)
            {
                // Don't throw exceptions on Participant tracker thread.
                CheckForSevereException(e);
            }

            return validBreakpoint;
        }

        private void GetMetaDataAndCommonParameters(
            Dictionary<string, object> wfMetaData,
            Dictionary<string, object> commonParameters)
        {
            if (wfMetaData != null)
            {
                foreach (var item in wfMetaData)
                {
                    _wfMetaVariables.Add(PSWorkflowApplicationInstance.TranslateMetaDataName(item.Key), item.Value);
                }
            }

            if (commonParameters != null)
            {
                foreach (var item in commonParameters)
                {
                    if (!_wfMetaVariables.ContainsKey(item.Key))
                    {
                        _wfMetaVariables.Add(item.Key, item.Value);
                    }
                }
            }
        }

        private InvocationInfo CreateInvocationInfo(
            ActivityPosition activityPosition)
        {
            string cmdName = activityPosition.Name;
            int line = activityPosition.Line;
            int col = activityPosition.Column;

            Dbg.Assert(!string.IsNullOrEmpty(_scriptName), "Script name should never be null during a debugger stop.");

            string script;
            string scriptLine;
            int endCol;

            if (string.IsNullOrEmpty(_script) ||
                (_scriptLines == null) ||
                (line < 1) ||
                (line > _scriptLines.Length))
            {
                // Create a minimal InvocationInfo object without source information so that
                // the user can get something (function name, line, column) on this workflow sequence point.
                script = string.Empty;
                scriptLine = string.Empty;
                endCol = col;
            }
            else
            {
                // Otherwise provide full source information.
                script = _script;
                scriptLine = _scriptLines[line - 1];
                endCol = scriptLine.Length + 1;
            }

            ScriptPosition scriptStartPosition = new ScriptPosition(_scriptName, line, col, scriptLine, script);
            ScriptPosition scriptEndPosition = new ScriptPosition(_scriptName, line, endCol, scriptLine, script);

            InvocationInfo invocationInfo = InvocationInfo.Create(
                new WorkflowInfo(
                    cmdName, 
                    script, 
                    ScriptBlock.Create(script), 
                    (_xamlDefinition != null) ? _xamlDefinition : _outerFnXamlDefinition,
                    null),
                new ScriptExtent(
                    scriptStartPosition, 
                    scriptEndPosition)
                );

            // For remoting invocationInfo.DisplayScriptPosition does not serialize line string needed
            // for display.  Therefore set it to null so that ScriptPosition will be used which *does*
            // pass the Line string.
            invocationInfo.DisplayScriptPosition = null;

            return invocationInfo;
        }

        private void AddBreakpoint(Breakpoint bp)
        {
            // Keep track of all script breakpoints from the parent so 
            // that *-PSBreakpoint commands can properly reflect this.
            if (!_initalParentBreakpoints.ContainsKey(bp.Id))
            {
                _initalParentBreakpoints.Add(bp.Id, bp);
            }

            // Line breakpoints
            LineBreakpoint lbp = bp as LineBreakpoint;
            if (lbp != null)
            {
                if (!_lineBreakPoints.ContainsKey(lbp.Id)) { _lineBreakPoints.Add(lbp.Id, lbp); }
            }
            else
            {
                // Variable breakpoints
                VariableBreakpoint vbp = bp as VariableBreakpoint;
                if (vbp != null)
                {
                    if (!_variableBreakPoints.ContainsKey(vbp.Id)) { _variableBreakPoints.Add(vbp.Id, vbp); }
                }
                else
                {
                    // Command breakpoints
                    CommandBreakpoint cbp = bp as CommandBreakpoint;
                    if (cbp != null)
                    {
                        if (!_commandBreakpoints.ContainsKey(cbp.Id)) { _commandBreakpoints.Add(cbp.Id, cbp); }
                    }
                }
            }
        }

        private void RemoveBreakpoint(int id)
        {
            _initalParentBreakpoints.Remove(id);

            if (!_lineBreakPoints.Remove(id))
            {
                if (!_variableBreakPoints.Remove(id))
                {
                    _commandBreakpoints.Remove(id);
                }
            }
        }

        /// <summary>
        /// Lazily create Xaml related info.
        /// </summary>
        /// <returns>True if Xaml info is available.</returns>
        private bool CheckXamlInfo()
        {
            if (!string.IsNullOrEmpty(_outerFnXamlDefinition))
            {
                return true;
            }

            if (_wfInstance.PSWorkflowDefinition != null &&
                _wfInstance.PSWorkflowDefinition.WorkflowXaml != null)
            {
                _outerFnXamlDefinition = _wfInstance.PSWorkflowDefinition.WorkflowXaml;

                // It is possible for the workflow definition to contain null/empty Xaml,
                // such as when a workflow job is created from Xaml and not from PS script.
                // In this case the debugger remains inactive.
                return !string.IsNullOrEmpty(_outerFnXamlDefinition);
            }

            return false;
        }

        /// <summary>
        /// Lazily create the job meta data variables.
        /// </summary>
        /// <returns></returns>
        private bool CheckWorkflowJob(string funcName)
        {
            if (_wfMetaVariables.Count > 0)
            {
                return true;
            }

            if (_wfInstance.PSWorkflowJob != null &&
                _wfInstance.PSWorkflowJob.PSWorkflowJobDefinition != null)
            {
                // Look for script file and source info and if needed read script file.
                // Update the function name-to-script map.
                string scriptFile = _wfInstance.PSWorkflowJob.PSWorkflowJobDefinition.WorkflowScriptFile;
                string script = _wfInstance.PSWorkflowJob.PSWorkflowJobDefinition.WorkflowFullScript;
                if (string.IsNullOrEmpty(script) && !string.IsNullOrEmpty(scriptFile))
                {
                    script = ReadScriptFromFile(scriptFile);
                }

                if (!string.IsNullOrEmpty(script))
                {
                    UpdateSourceMap(
                        _funcToSourceMap,
                        funcName,
                        script,
                        scriptFile,
                        _outerFnXamlDefinition);
                }

                // Create job meta data.
                GetMetaDataAndCommonParameters(
                    _wfInstance.PSWorkflowJob.JobMetadata,
                    _wfInstance.PSWorkflowJob.PSWorkflowCommonParameters);

                return true;
            }

            return false;
        }

        private void UpdateSourceMap(
            Dictionary<string, DebugSource> funcToSourceMap,
            string funcName,
            string script,
            string scriptFile,
            string xamlDefinition)
        {
            if (funcToSourceMap == null) { throw new PSArgumentNullException("funcToSourceMap"); }
            if (funcName == null) { throw new PSArgumentNullException("funcName"); }
            if (script == null) { throw new PSArgumentNullException("script"); }

            // Outer function name is always added.
            DebugSource debugSource = new DebugSource(script, scriptFile, xamlDefinition);
            funcToSourceMap.Add(funcName, debugSource);

            // Get list of workflow functions in the script AST and
            // add reference to the debug source.
            ParseScriptForMap(funcToSourceMap, script, debugSource);
        }

        private void ParseScriptForMap(
            Dictionary<string, DebugSource> funcToSourceMap,
            string script, 
            DebugSource debugSource)
        {
            ScriptBlock sb = ScriptBlock.Create(script);
            ScriptBlockAst sbAst = sb.Ast as ScriptBlockAst;
            if (sbAst != null)
            {
                if (sbAst.BeginBlock != null)
                {
                    AddWorkflowFunctionToMap(sbAst.BeginBlock.Statements, funcToSourceMap, debugSource);
                }

                if (sbAst.ProcessBlock != null)
                {
                    AddWorkflowFunctionToMap(sbAst.ProcessBlock.Statements, funcToSourceMap, debugSource);
                }

                if (sbAst.EndBlock != null)
                {
                    AddWorkflowFunctionToMap(sbAst.EndBlock.Statements, funcToSourceMap, debugSource);
                }
            }
        }

        private void AddWorkflowFunctionToMap(
            ReadOnlyCollection<StatementAst> statements,
            Dictionary<string, DebugSource> funcToSourceMap,
            DebugSource debugSource)
        {
            if (statements == null) { return; }

            foreach (var statementAst in statements)
            {
                FunctionDefinitionAst fAst = statementAst as FunctionDefinitionAst;
                if ((fAst != null) && (fAst.IsWorkflow))
                {
                    string fName = fAst.Name;

                    if (funcToSourceMap.ContainsKey(fName))
                    {
                        // Update existing source info.
                        var existingInfo = funcToSourceMap[fName];
                        funcToSourceMap.Remove(fName);

                        debugSource = new DebugSource(
                            debugSource.Script,                 // Update script
                            debugSource.ScriptFile,             // Update script filename
                            existingInfo.XamlDefinition);       // Keep XamlDefintion
                    }

                    funcToSourceMap.Add(fName, debugSource);

                    // Call this recursively for all statements in the workflow function to pick 
                    // up any WF functions defined inside this WF function.
                    if (fAst.Body.BeginBlock != null)
                    {
                        AddWorkflowFunctionToMap(fAst.Body.BeginBlock.Statements, funcToSourceMap, debugSource);
                    }

                    if (fAst.Body.ProcessBlock != null)
                    {
                        AddWorkflowFunctionToMap(fAst.Body.ProcessBlock.Statements, funcToSourceMap, debugSource);
                    }

                    if (fAst.Body.EndBlock != null)
                    {
                        AddWorkflowFunctionToMap(fAst.Body.EndBlock.Statements, funcToSourceMap, debugSource);
                    }
                }
            }
        }

        /// <summary>
        /// UpdateScriptInfo
        /// </summary>
        private void UpdateScriptInfo(string funcName)
        {
            if (funcName.Equals(_scriptName, StringComparison.OrdinalIgnoreCase)) { return; }

            DebugSource debugSource;
            if (!_funcToSourceMap.TryGetValue(funcName, out debugSource))
            {
                // Minimal script info, just the Workflow function name.
                _scriptName = funcName;
                _script = null;
                _scriptLines = null;
                _xamlDefinition = null;
            }
            else
            {
                // Otherwise get full source script info.
                _script = debugSource.Script;
                _scriptName = !string.IsNullOrEmpty(debugSource.ScriptFile) ? debugSource.ScriptFile : funcName;
                _scriptLines = _script.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                _xamlDefinition = debugSource.XamlDefinition;

                if (!_funcToValidLineNumbersMap.TryGetValue(funcName, out _validWFLineNumbers))
                {
                    _validWFLineNumbers = CreateValidScriptLineSet(_xamlDefinition);
                    _funcToValidLineNumbersMap.Add(funcName, _validWFLineNumbers);
                }
            }

            // Reset parent debugger to show new source, if any.
            Debugger parentDebugger = _parent;
            if (parentDebugger != null)
            {
                parentDebugger.ResetCommandProcessorSource();
            }
        }

        private SortedSet<int> CreateValidScriptLineSet(string xamlDefinition)
        {
            if (xamlDefinition == null) { return null; }

            SortedSet<int> validWFLineNumbers = new SortedSet<int>();

            try
            {
                XmlDocument doc = (XmlDocument)LanguagePrimitives.ConvertTo(xamlDefinition, typeof(XmlDocument), CultureInfo.InvariantCulture);
                string nsName = "ns1";
                string ns = "clr-namespace:Microsoft.PowerShell.Activities;assembly=Microsoft.PowerShell.Activities";
                string query = string.Format(CultureInfo.InvariantCulture, @"//{0}:PowerShellValue/@Expression", nsName);
                XmlNamespaceManager nsMgr = new XmlNamespaceManager(doc.NameTable);
                nsMgr.AddNamespace(nsName, ns);

                // Search for all Xaml PowerShellValue/Expression node attributes.
                XmlNodeList nodes = doc.SelectNodes(query, nsMgr);
                foreach (XmlAttribute exprItem in nodes)
                {
                    if (exprItem == null) { continue; }
                    string exprValue = exprItem.InnerText;

                    // Look for line/col expression attribute symbol pattern and retrieve symbols.
                    if (System.Text.RegularExpressions.Regex.IsMatch(exprValue, @"^'\d+:\d+:\S+'$"))
                    {
                        string[] symbols = exprValue.Trim('\'').Split(':');

                        int nLine = -1;
                        try
                        {
                            nLine = Convert.ToInt32(symbols[0], CultureInfo.InvariantCulture);
                        }
                        catch (FormatException)
                        { }
                        catch (OverflowException)
                        { }

                        // Add line number to list.
                        if (nLine > 0 &&
                            !validWFLineNumbers.Contains(nLine))
                        {
                            validWFLineNumbers.Add(nLine);
                        }
                    }
                }
            }
            catch (ArgumentNullException) { }
            catch (PSInvalidCastException) { }
            catch (System.Xml.XPath.XPathException) { }

            return validWFLineNumbers;
        }

        private static string ReadScriptFromFile(string scriptSource)
        {
            // If script content string is a file path then read the script.
            try
            {
                return System.IO.File.ReadAllText(scriptSource);
            }
            catch (ArgumentException) { }
            catch (System.IO.IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (NotSupportedException) { }
            catch (System.Security.SecurityException) { }

            return null;
        }

        private DebuggerCommandResults InternalProcessCommand(PSCommand command, PSDataCollection<PSObject> output)
        {
            string cmd = command.Commands[0].CommandText.Trim();

            if (cmd.Equals("prompt", StringComparison.OrdinalIgnoreCase))
            {
                // WF prompt.
                return HandlePromptCommand(command, output);
            }

            if (cmd.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                // Ignore.
                return new DebuggerCommandResults(DebuggerResumeAction.Continue, true);
            }

            if (cmd.Equals("k", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("Get-PSCallStack", StringComparison.OrdinalIgnoreCase))
            {
                // WF call stack.
                return HandleCallStack(output);
            }

            return null;
        }

        private DebuggerCommandResults HandlePromptCommand(PSCommand command, PSDataCollection<PSObject> output)
        {
            // Create WF Debug prompt.
            string wfDebugPrompt;
            string location = null;
            object locationItem;
            if (_wfMetaVariables.TryGetValue("PSComputerName", out locationItem))
            {
                location = locationItem as string;
            }

            wfDebugPrompt = string.Format(CultureInfo.InvariantCulture,
                WFDebugPrompt,
                !string.IsNullOrEmpty(location) ? location : string.Empty);

            // Run prompt command to get default prompt from runspace.
            string defaultPrompt = null;
            PSDataCollection<PSObject> promptOutput = new PSDataCollection<PSObject>();
            RunPowerShellCommand(command, null, promptOutput, false);
            if (promptOutput.Count == 1)
            {
                defaultPrompt = promptOutput[0].BaseObject as string;
            }

            if (defaultPrompt != null)
            {
                output.Add(wfDebugPrompt + defaultPrompt.TrimEnd() + "> ");
            }
            else
            {
                output.Add(wfDebugPrompt + DefaultWFPrompt);
            }

            return new DebuggerCommandResults(null, true);
        }

        private DebuggerCommandResults HandleCallStack(PSDataCollection<PSObject> output)
        {
            PSDataCollection<CallStackFrame> callStack = GetCallStack().ToArray();
            if (callStack.Count > 0)
            {
                if ((_host != null) && (_host.UI != null) &&
                    !(_host.GetType().FullName).Equals("System.Management.Automation.Remoting.ServerRemoteHost", 
                        StringComparison.OrdinalIgnoreCase))
                {
                    // Use Out-Default to stream and format call stack data.
                    PSCommand cmd = new PSCommand();
                    cmd.AddCommand("Out-Default");

                    RunPowerShellCommand(cmd, callStack, output, false);
                }
                else
                {
                    // Otherwise add directly to output.  This is needed in the WF remote case because
                    // host output is blocked on the client during debugger stop.
                    // Use Out-String so that it gets formatted correctly.
                    PSCommand cmd = new PSCommand();
                    cmd.AddCommand("Out-String").AddParameter("Stream", true);

                    RunPowerShellCommand(cmd, callStack, output, false);
                }
            }

            return new DebuggerCommandResults(null, true);
        }

        private DebuggerCommandResults RunPowerShellCommand(
            PSCommand command, 
            PSDataCollection<object> input,
            PSDataCollection<PSObject> output,
            bool addToHistory,
            bool allowBreakException = false)
        {
            // Wait for any current command to finish.
            IAsyncResult async = _psAsyncResult;
            if (async != null)
            {
                try
                {
                    async.AsyncWaitHandle.WaitOne();
                }
                catch (ObjectDisposedException) { }
            }

            // Lazily create a runspace to run PS commands on.
            if (_runspace == null || (_runspace.RunspaceStateInfo.State != RunspaceState.Opened))
            {
                if (_runspace != null) { _runspace.Dispose(); }

                // Enforce the system lockdown policy if one is defined.
                InitialSessionState iss = InitialSessionState.CreateDefault2();
                if (SystemPolicy.GetSystemLockdownPolicy() == SystemEnforcementMode.Enforce)
                {
                    iss.LanguageMode = PSLanguageMode.ConstrainedLanguage;
                }

                _runspace = (_host != null) ?
                    RunspaceFactory.CreateRunspace(_host, iss) : RunspaceFactory.CreateRunspace(iss);

                _runspace.Open();

                SetPath(_runspace, _parentPath);

                CreateHelperFunctionsAndWorkflowMetaDataOnRunspace(_runspace);

                // Set breakpoints on runspace to support *-PSBreakpoint cmdlets.
                _runspace.Debugger.SetBreakpoints(_initalParentBreakpoints.Values);
                _runspace.Debugger.BreakpointUpdated += HandleRunspaceBreakpointUpdated;
            }

            try
            {
                using (_psDebuggerCommand = System.Management.Automation.PowerShell.Create())
                {
                    // Runspace debugger must be disabled while running commands.
                    _runspace.Debugger.SetDebugMode(DebugModes.None);
                    _psDebuggerCommand.Runspace = _runspace;
                    _psDebuggerCommand.Commands = command;
                    foreach (var cmd in _psDebuggerCommand.Commands.Commands)
                    {
                        cmd.MergeMyResults(PipelineResultTypes.All, PipelineResultTypes.Output);
                    }

                    // Default settings.
                    PSInvocationSettings settings = new PSInvocationSettings();
                    settings.ExposeFlowControlExceptions = allowBreakException;
                    settings.AddToHistory = addToHistory;

                    PSDataCollection<PSObject> collectOutput = (output != null) ? output : new PSDataCollection<PSObject>();

                    // Allow any exceptions to propagate.
                    _psAsyncResult = _psDebuggerCommand.BeginInvoke<object, PSObject>(input, collectOutput, settings, null, null);
                    _psDebuggerCommand.EndInvoke(_psAsyncResult);
                }
            }
            finally
            {
                _psDebuggerCommand = null;
                _psAsyncResult = null;

                // Restore workflow debugging.
                _runspace.Debugger.SetDebugMode(DebugModes.LocalScript);
            }

            return new DebuggerCommandResults(null, false);
        }

        private void HandleRunspaceBreakpointUpdated(object sender, BreakpointUpdatedEventArgs e)
        {
            // Update local workflow breakpoints.
            switch (e.UpdateType)
            {
                case BreakpointUpdateType.Set:
                    AddBreakpoint(e.Breakpoint);
                    break;

                case BreakpointUpdateType.Removed:
                    RemoveBreakpoint(e.Breakpoint.Id);
                    break;
            }
            
            // Forward update to parent.
            RaiseBreakpointUpdatedEvent(e);
        }

        private void SetPath(Runspace runspace, PathInfo path)
        {
            if (path == null ||
                string.IsNullOrEmpty(path.Path))
            {
                return;
            }

            using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create())
            {
                ps.Runspace = runspace;
                ps.AddCommand("Set-Location").AddParameter("Path", path.Path);
                ps.Invoke();
            }
        }

        private void CreateHelperFunctionsAndWorkflowMetaDataOnRunspace(Runspace runspace)
        {
            using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create())
            {
                ps.Runspace = runspace;

                // Add workflow specific debugger functions.
                foreach (var functionScript in System.Management.Automation.Internal.DebuggerUtils.GetWorkflowDebuggerFunctions())
                {
                    ps.Commands.Clear();
                    ps.AddScript(functionScript);
                    ps.Invoke();
                }

                // Add meta workflow variables to runspace.
                ps.Commands.Clear();
                ps.AddCommand("Set-DebuggerVariable").AddParameter("Variables", _wfMetaVariables);
                ps.Invoke();

                // Add private *this* debugger variable.
                ps.Commands.Clear();
                ps.AddCommand("Set-Variable").AddParameter("Name", "PSWorkflowDebugger").AddParameter("Value", this).AddParameter("Visibility", "Private");
                ps.Invoke();
            }
        }

        private void SetWorkflowVariablesToRunspace()
        {
            // Set workflow variables.
            try
            {
                // Set workflow variables from activity context.
                PSCommand cmd = new PSCommand();
                cmd.AddCommand("Set-DebuggerVariable").AddParameter("Variables", _variables);
                RunPowerShellCommand(cmd, null, null, false);
            }
            catch (Exception e)
            {
                // Don't throw exceptions on Participant tracker thread.
                CheckForSevereException(e);
            }
        }

        private void SetWorkflowDebuggerContextVariable(DebuggerStopEventArgs args)
        {
            // Add context variable to variables collection.
            PSDebugContext debugContext = new PSDebugContext(args.InvocationInfo, new List<Breakpoint>(args.Breakpoints));
            if (_variables.ContainsKey(DebugContextName))
            {
                _variables[DebugContextName] = debugContext;
            }
            else
            {
                _variables.Add(DebugContextName, debugContext);
            }

            // Set workflow debugger context variable to runspace.
            try
            {
                PSCommand cmd = new PSCommand();
                cmd.AddCommand("Set-Variable")
                    .AddParameter("Name", DebugContextName)
                    .AddParameter("Value", debugContext);
                RunPowerShellCommand(cmd, null, null, false);
            }
            catch (Exception e)
            {
                // Don't throw exceptions on Participant tracker thread.
                CheckForSevereException(e);
            }
        }

        private void RemoveWorkflowVariablesFromRunspace()
        {
            // Remove workflow variables.
            try
            {
                PSCommand cmd = new PSCommand();
                cmd.AddCommand("Remove-DebuggerVariable").AddParameter("Name", _variables.Keys);
                RunPowerShellCommand(cmd, null, null, false);
            }
            catch (Exception e)
            {
                // Don't throw exceptions on Participant tracker thread.
                CheckForSevereException(e);
            }
        }

        /// <summary>
        /// Helper method to check if user attempted to modify a workflow variable which
        /// isn't supported.  Write error message to host.
        /// </summary>
        private bool CheckForWorkflowVariableChange(
            PSCommand command, 
            PSDataCollection<PSObject> output,
            out ErrorRecord errorRecord)
        {
            errorRecord = null;
            string variableName = string.Empty;

            // Searcher to detect a variable assignment.
            // This is based on searcher from WorkflowJobConverter but is modified.
            Func<Ast, bool> assignmentSearcher = (ast) =>
            {
                AssignmentStatementAst assignment = ast as AssignmentStatementAst;
                UnaryExpressionAst unaryExpression = ast as UnaryExpressionAst;

                if ((assignment == null) && (unaryExpression == null))
                {
                    return false;
                }

                // Check if this is a unary assignment
                if (unaryExpression != null)
                {
                    VariableExpressionAst referenceVariable = unaryExpression.Child as VariableExpressionAst;
                    if (referenceVariable != null && String.Equals(referenceVariable.VariablePath.UserPath, variableName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                // Check if this is a regular assignment
                if (assignment == null) { return false; }

                // Capture $x = 10
                VariableExpressionAst variableExpression = assignment.Left as VariableExpressionAst;
                string detectedVariableName = null;

                if (variableExpression == null)
                {
                    // Capture [int] $x = 10
                    ConvertExpressionAst convertExpression = assignment.Left as ConvertExpressionAst;
                    if (convertExpression != null)
                    {
                        variableExpression = convertExpression.Child as VariableExpressionAst;
                    }
                }

                if (variableExpression != null)
                {
                    detectedVariableName = variableExpression.VariablePath.UserPath;
                }
                else
                {
                    return false;
                }

                // See if it's the variable we're looking for
                // Variable = <Expression>
                string workingVariableName = detectedVariableName;

                // Allow the "WORKFLOW:" scope qualifier in nested scopes
                if (workingVariableName.StartsWith("WORKFLOW:", StringComparison.OrdinalIgnoreCase))
                {
                    workingVariableName = workingVariableName.Remove(0, "WORKFLOW:".Length);
                }

                return String.Equals(workingVariableName, variableName, StringComparison.OrdinalIgnoreCase);
            };

            Dictionary<string, object> allWFVariables = new Dictionary<string, object>(_wfMetaVariables);
            foreach (var wfVar in _variables)
            {
                if (!allWFVariables.ContainsKey(wfVar.Key))
                {
                    allWFVariables.Add(wfVar.Key, wfVar.Value);
                }
            }

            foreach (var cmd in command.Commands)
            {
                // Create scriptblock/AST for each command.
                ScriptBlock sb;
                try
                {
                    sb = ScriptBlock.Create(cmd.CommandText);
                }
                catch (ParseException)
                {
                    // Ignore parse exception here and let normal command
                    // processing handle potential incomplete parse condition.
                    return false;
                }

                // Check assignments to known workflow variables.
                foreach (var workflowVariable in allWFVariables)
                {
                    variableName = workflowVariable.Key;

                    Ast result = sb.Ast.Find(assignmentSearcher, searchNestedScriptBlocks: true);
                    if (result != null)
                    {
                        string msg = string.Format(System.Threading.Thread.CurrentThread.CurrentCulture,
                               Resources.DebuggerCannotModifyWFVars, variableName);

                        errorRecord = new ErrorRecord(
                                        new PSInvalidOperationException(msg),
                                        "PSWorkflowDebuggerCannotModifyWorkflowVariables",
                                        ErrorCategory.InvalidOperation,
                                        this);

                        return true;
                    }
                }
            }

            return false;
        }

        private bool CheckAddToHistory(PSCommand command)
        {
            if (command.Commands.Count == 0) { return false; }

            return System.Management.Automation.Internal.DebuggerUtils.ShouldAddCommandToHistory(
                command.Commands[0].CommandText);
        }

        /// <summary>
        /// Helper method to check for and rethrow severe exceptions.
        /// </summary>
        /// <param name="e">Exception to check</param>
        private static void CheckForSevereException(Exception e)
        {
            if (e is AccessViolationException || e is StackOverflowException)
            {
                throw e;
            }
        }

        #endregion

        #region Private Classes

        private class InvocationInfoComparer : IEqualityComparer<InvocationInfo>
        {
            bool IEqualityComparer<InvocationInfo>.Equals(InvocationInfo obj1, InvocationInfo obj2)
            {
                return obj1.MyCommand.Name.Equals(obj2.MyCommand.Name, StringComparison.OrdinalIgnoreCase);
            }

            int IEqualityComparer<InvocationInfo>.GetHashCode(InvocationInfo info)
            {
                return info.GetHashCode();
            }
        }

        #endregion

    } // PSWorkflowDebugger
}
