// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// A common base class for code shared between an interpreted (old) script block and a compiled (new) script block.
    /// </summary>
    internal abstract class ScriptCommandProcessorBase : CommandProcessorBase
    {
        protected ScriptCommandProcessorBase(ScriptBlock scriptBlock, ExecutionContext context, bool useLocalScope, CommandOrigin origin, SessionStateInternal sessionState)
            : base(new ScriptInfo(string.Empty, scriptBlock, context))
        {
            this._dontUseScopeCommandOrigin = false;
            this._fromScriptFile = false;

            CommonInitialization(scriptBlock, context, useLocalScope, origin, sessionState);
        }

        protected ScriptCommandProcessorBase(IScriptCommandInfo commandInfo, ExecutionContext context, bool useLocalScope, SessionStateInternal sessionState)
            : base((CommandInfo)commandInfo)
        {
            Diagnostics.Assert(commandInfo != null, "commandInfo cannot be null");
            Diagnostics.Assert(commandInfo.ScriptBlock != null, "scriptblock cannot be null");

            this._fromScriptFile = (this.CommandInfo is ExternalScriptInfo || this.CommandInfo is ScriptInfo);
            this._dontUseScopeCommandOrigin = true;

            CommonInitialization(commandInfo.ScriptBlock, context, useLocalScope, CommandOrigin.Internal, sessionState);
        }

        /// <summary>
        /// When executing a scriptblock, the command origin needs to be set for the current scope.
        /// If this true, then the scope origin will be set to the command origin. If it's false,
        /// then the scope origin will be set to Internal. This allows public functions to call
        /// private functions but still see $MyInvocation.CommandOrigin as $true.
        /// </summary>
        protected bool _dontUseScopeCommandOrigin;

        /// <summary>
        /// If true, then an exit exception will be rethrown instead of caught and processed...
        /// </summary>
        protected bool _rethrowExitException;

        /// <summary>
        /// This indicates whether exit is called during the execution of
        /// script block.
        /// </summary>
        /// <remarks>
        /// Exit command can be executed in any of begin/process/end blocks.
        ///
        /// If exit is called in one block (for example, begin), any subsequent
        /// blocks (for example, process and end) should not be executed.
        /// </remarks>
        protected bool _exitWasCalled;

        protected ScriptBlock _scriptBlock;

        private ScriptParameterBinderController _scriptParameterBinderController;

        internal ScriptParameterBinderController ScriptParameterBinderController
        {
            get
            {
                if (_scriptParameterBinderController == null)
                {
                    // Set up the hashtable that will be used to hold all of the bound parameters...
                    _scriptParameterBinderController =
                        new ScriptParameterBinderController(((IScriptCommandInfo)CommandInfo).ScriptBlock,
                            Command.MyInvocation, Context, Command, CommandScope);
                    _scriptParameterBinderController.CommandLineParameters.UpdateInvocationInfo(this.Command.MyInvocation);
                    this.Command.MyInvocation.UnboundArguments = _scriptParameterBinderController.DollarArgs;
                }

                return _scriptParameterBinderController;
            }
        }

        /// <summary>
        /// Helper function for setting up command object and commandRuntime object
        /// for script command processor.
        /// </summary>
        protected void CommonInitialization(ScriptBlock scriptBlock, ExecutionContext context, bool useLocalScope, CommandOrigin origin, SessionStateInternal sessionState)
        {
            Diagnostics.Assert(context != null, "execution context cannot be null");
            Diagnostics.Assert(context.Engine != null, "context.engine cannot be null");

            this.CommandSessionState = sessionState;
            this._context = context;
            this._rethrowExitException = this.Context.ScriptCommandProcessorShouldRethrowExit;
            this._context.ScriptCommandProcessorShouldRethrowExit = false;

            ScriptCommand scriptCommand = new ScriptCommand { CommandInfo = this.CommandInfo };

            this.Command = scriptCommand;
            // WinBlue: 219115
            // Set the command origin for the new ScriptCommand object since we're not
            // going through command discovery here where it's usually set.
            this.Command.CommandOriginInternal = origin;
            this.Command.commandRuntime = this.commandRuntime = new MshCommandRuntime(this.Context, this.CommandInfo, scriptCommand);

            this.CommandScope = useLocalScope
                                    ? CommandSessionState.NewScope(this.FromScriptFile)
                                    : CommandSessionState.CurrentScope;

            this.UseLocalScope = useLocalScope;

            _scriptBlock = scriptBlock;

            // If the script has been dotted, throw an error if it's from a different language mode.
            // Unless it was a script loaded through -File, in which case the danger of dotting other
            // language modes (getting internal functions in the user's state) isn't a danger
            if ((!this.UseLocalScope) && (!this._rethrowExitException))
            {
                ValidateCompatibleLanguageMode(_scriptBlock, context.LanguageMode, Command.MyInvocation);
            }
        }

        /// <summary>
        /// Checks if user has requested help (for example passing "-?" parameter for a cmdlet)
        /// and if yes, then returns the help target to display.
        /// </summary>
        /// <param name="helpTarget">Help target to request.</param>
        /// <param name="helpCategory">Help category to request.</param>
        /// <returns><see langword="true"/> if user requested help; <see langword="false"/> otherwise.</returns>
        internal override bool IsHelpRequested(out string helpTarget, out HelpCategory helpCategory)
        {
            if (arguments != null && CommandInfo != null && !string.IsNullOrEmpty(CommandInfo.Name) && _scriptBlock != null)
            {
                foreach (CommandParameterInternal parameter in this.arguments)
                {
                    Dbg.Assert(parameter != null, "CommandProcessor.arguments shouldn't have any null arguments");
                    if (parameter.IsDashQuestion())
                    {
                        Dictionary<Ast, Token[]> scriptBlockTokenCache = new Dictionary<Ast, Token[]>();
                        HelpInfo helpInfo = _scriptBlock.GetHelpInfo(context: Context, commandInfo: CommandInfo,
                            dontSearchOnRemoteComputer: false, scriptBlockTokenCache: scriptBlockTokenCache, helpFile: out _, helpUriFromDotLink: out _);
                        if (helpInfo == null)
                        {
                            break;
                        }

                        helpTarget = helpInfo.Name;
                        helpCategory = helpInfo.HelpCategory;
                        return true;
                    }
                }
            }

            return base.IsHelpRequested(out helpTarget, out helpCategory);
        }
    }

    /// <summary>
    /// This class implements a command processor for script related commands.
    /// </summary>
    /// <remarks>
    /// 1. Usage scenarios
    ///
    /// ScriptCommandProcessor is used for four kinds of commands.
    ///
    /// a. Functions and filters
    ///
    /// For example,
    ///
    ///     function foo($a) {$a}
    ///     foo "my text"
    ///
    /// Second command is an example of a function invocation.
    ///
    /// In this case, a FunctionInfo object is provided while constructing
    /// command processor.
    ///
    /// b. Script File
    ///
    /// For example,
    ///
    ///     . .\my.ps1
    ///
    /// In this case, a ExternalScriptInfo or ScriptInfo object is provided
    /// while constructing command processor.
    ///
    /// c. ScriptBlock
    ///
    /// For example,
    ///
    ///     . {$a = 5}
    ///
    /// In this case, a ScriptBlock object is provided while constructing command
    /// processor.
    ///
    /// d. Script Text
    ///
    /// This is used internally for directly running a text stream of script.
    ///
    /// 2. Design
    ///
    /// a. Script block
    ///
    /// No matter how a script command processor is created, core piece of information
    /// is always a ScriptBlock object, which can come from either a FunctionInfo object,
    /// a ScriptInfo object, or directly parsed from script text.
    ///
    /// b. Script scope
    ///
    /// A script block can be executed either in current scope or in a new scope.
    ///
    /// New scope created should be a scope supporting $script: in case the command
    /// processor is created from a script file.
    ///
    /// c. Begin/Process/End blocks
    ///
    /// Each script block can have one block of script for begin/process/end. These map
    /// to BeginProcessing, ProcessingRecord, and EndProcessing of cmdlet api.
    ///
    /// d. ExitException handling
    ///
    /// If the command processor is created based on a script file, its exit exception
    /// handling is different in the sense that it indicates an exitcode instead of killing
    /// current powershell session.
    /// </remarks>
    internal sealed class DlrScriptCommandProcessor : ScriptCommandProcessorBase
    {
        private readonly ArrayList _input = new ArrayList();
        private readonly object _dollarUnderbar = AutomationNull.Value;
        private new ScriptBlock _scriptBlock;
        private MutableTuple _localsTuple;
        private bool _runOptimizedCode;
        private bool _argsBound;
        private bool _anyClauseExecuted;
        private FunctionContext _functionContext;

        internal DlrScriptCommandProcessor(ScriptBlock scriptBlock, ExecutionContext context, bool useNewScope, CommandOrigin origin, SessionStateInternal sessionState, object dollarUnderbar)
            : base(scriptBlock, context, useNewScope, origin, sessionState)
        {
            Init();
            _dollarUnderbar = dollarUnderbar;
        }

        internal DlrScriptCommandProcessor(ScriptBlock scriptBlock, ExecutionContext context, bool useNewScope, CommandOrigin origin, SessionStateInternal sessionState)
            : base(scriptBlock, context, useNewScope, origin, sessionState)
        {
            Init();
        }

        internal DlrScriptCommandProcessor(FunctionInfo functionInfo, ExecutionContext context, bool useNewScope, SessionStateInternal sessionState)
            : base(functionInfo, context, useNewScope, sessionState)
        {
            Init();
        }

        internal DlrScriptCommandProcessor(ScriptInfo scriptInfo, ExecutionContext context, bool useNewScope, SessionStateInternal sessionState)
            : base(scriptInfo, context, useNewScope, sessionState)
        {
            Init();
        }

        internal DlrScriptCommandProcessor(ExternalScriptInfo scriptInfo, ExecutionContext context, bool useNewScope, SessionStateInternal sessionState)
            : base(scriptInfo, context, useNewScope, sessionState)
        {
            Init();
        }

        private void Init()
        {
            _scriptBlock = base._scriptBlock;
            _obsoleteAttribute = _scriptBlock.ObsoleteAttribute;
            _runOptimizedCode = _scriptBlock.Compile(optimized: _context._debuggingMode <= 0 && UseLocalScope);
            _localsTuple = _scriptBlock.MakeLocalsTuple(_runOptimizedCode);

            if (UseLocalScope)
            {
                Diagnostics.Assert(CommandScope.LocalsTuple == null, "a newly created scope shouldn't have it's tuple set.");
                CommandScope.LocalsTuple = _localsTuple;
            }
        }

        /// <summary>
        /// Get the ObsoleteAttribute of the current command.
        /// </summary>
        internal override ObsoleteAttribute ObsoleteAttribute
        {
            get { return _obsoleteAttribute; }
        }

        private ObsoleteAttribute _obsoleteAttribute;

        internal override void Prepare(IDictionary psDefaultParameterValues)
        {
            _localsTuple.SetAutomaticVariable(AutomaticVariable.MyInvocation, this.Command.MyInvocation, _context);
            _scriptBlock.SetPSScriptRootAndPSCommandPath(_localsTuple, _context);
            _functionContext = new FunctionContext
            {
                _executionContext = _context,
                _outputPipe = commandRuntime.OutputPipe,
                _localsTuple = _localsTuple,
                _scriptBlock = _scriptBlock,
                _file = _scriptBlock.File,
                _debuggerHidden = _scriptBlock.DebuggerHidden,
                _debuggerStepThrough = _scriptBlock.DebuggerStepThrough,
                _sequencePoints = _scriptBlock.SequencePoints,
            };
        }

        /// <summary>
        /// Execute BeginProcessing part of command. It sets up the overall scope
        /// object for this command and runs the begin clause of the script block if
        /// it isn't empty.
        /// </summary>
        /// <exception cref="PipelineStoppedException">
        /// a terminating error occurred, or the pipeline was otherwise stopped
        /// </exception>
        internal override void DoBegin()
        {
            if (!RanBeginAlready)
            {
                RanBeginAlready = true;

                ScriptBlock.LogScriptBlockStart(_scriptBlock, Context.CurrentRunspace.InstanceId);

                // Even if there is no begin, we need to set up the execution scope for this script...
                SetCurrentScopeToExecutionScope();
                CommandProcessorBase oldCurrentCommandProcessor = Context.CurrentCommandProcessor;
                try
                {
                    Context.CurrentCommandProcessor = this;

                    if (_scriptBlock.HasBeginBlock)
                    {
                        RunClause(_runOptimizedCode ? _scriptBlock.BeginBlock : _scriptBlock.UnoptimizedBeginBlock,
                                  AutomationNull.Value, _input);
                    }
                }
                finally
                {
                    Context.CurrentCommandProcessor = oldCurrentCommandProcessor;
                    RestorePreviousScope();
                }
            }
        }

        internal override void ProcessRecord()
        {
            if (_exitWasCalled)
            {
                return;
            }

            if (!this.RanBeginAlready)
            {
                RanBeginAlready = true;

                if (_scriptBlock.HasBeginBlock)
                {
                    RunClause(_runOptimizedCode ? _scriptBlock.BeginBlock : _scriptBlock.UnoptimizedBeginBlock, AutomationNull.Value, _input);
                }
            }

            if (_scriptBlock.HasProcessBlock)
            {
                if (!IsPipelineInputExpected())
                {
                    RunClause(_runOptimizedCode ? _scriptBlock.ProcessBlock : _scriptBlock.UnoptimizedProcessBlock, null, _input);
                }
                else
                {
                    DoProcessRecordWithInput();
                }
            }
            else if (IsPipelineInputExpected())
            {
                // accumulate the input when working in "synchronous" mode
                Debug.Assert(this.Command.MyInvocation.PipelineIterationInfo != null); // this should have been allocated when the pipe was started
                if (this.CommandRuntime.InputPipe.ExternalReader == null)
                {
                    while (Read())
                    {
                        // accumulate all of the objects and execute at the end.
                        _input.Add(Command.CurrentPipelineObject);
                    }
                }
            }
        }

        internal override void Complete()
        {
            try
            {
                if (_exitWasCalled)
                {
                    return;
                }

                // process any items that may still be in the input pipeline
                if (_scriptBlock.HasProcessBlock && IsPipelineInputExpected())
                {
                    DoProcessRecordWithInput();
                }

                if (_scriptBlock.HasEndBlock)
                {
                    var endBlock = _runOptimizedCode ? _scriptBlock.EndBlock : _scriptBlock.UnoptimizedEndBlock;

                    if (this.CommandRuntime.InputPipe.ExternalReader == null)
                    {
                        if (IsPipelineInputExpected())
                        {
                            // read any items that may still be in the input pipe
                            while (Read())
                            {
                                _input.Add(Command.CurrentPipelineObject);
                            }
                        }

                        // run with accumulated input
                        RunClause(endBlock, AutomationNull.Value, _input);
                    }
                    else
                    {
                        // run with asynchronously updated $input enumerator
                        RunClause(endBlock, AutomationNull.Value, this.CommandRuntime.InputPipe.ExternalReader.GetReadEnumerator());
                    }
                }
            }
            finally
            {
                if (!_scriptBlock.HasCleanBlock)
                {
                    ScriptBlock.LogScriptBlockEnd(_scriptBlock, Context.CurrentRunspace.InstanceId);
                }
            }
        }

        protected override void CleanResource()
        {
            if (_scriptBlock.HasCleanBlock && _anyClauseExecuted)
            {
                // The 'Clean' block doesn't write to pipeline.
                Pipe oldOutputPipe = _functionContext._outputPipe;
                _functionContext._outputPipe = new Pipe { NullPipe = true };

                try
                {
                    RunClause(
                        clause: _runOptimizedCode ? _scriptBlock.CleanBlock : _scriptBlock.UnoptimizedCleanBlock,
                        dollarUnderbar: AutomationNull.Value,
                        inputToProcess: AutomationNull.Value);
                }
                finally
                {
                    _functionContext._outputPipe = oldOutputPipe;
                    ScriptBlock.LogScriptBlockEnd(_scriptBlock, Context.CurrentRunspace.InstanceId);
                }
            }
        }

        private void DoProcessRecordWithInput()
        {
            // block for input and execute "process" block for all input objects
            Debug.Assert(this.Command.MyInvocation.PipelineIterationInfo != null); // this should have been allocated when the pipe was started
            var processBlock = _runOptimizedCode ? _scriptBlock.ProcessBlock : _scriptBlock.UnoptimizedProcessBlock;
            while (Read())
            {
                _input.Add(Command.CurrentPipelineObject);

                this.Command.MyInvocation.PipelineIterationInfo[this.Command.MyInvocation.PipelinePosition]++;

                RunClause(processBlock, Command.CurrentPipelineObject, _input);

                // now clear input for next iteration; also makes it clear for the end clause.
                _input.Clear();
            }
        }

        private void RunClause(Action<FunctionContext> clause, object dollarUnderbar, object inputToProcess)
        {
            ExecutionContext.CheckStackDepth();

            _anyClauseExecuted = true;
            Pipe oldErrorOutputPipe = this.Context.ShellFunctionErrorOutputPipe;

            // If the script block has a different language mode than the current,
            // change the language mode.
            PSLanguageMode? oldLanguageMode = null;
            PSLanguageMode? newLanguageMode = null;
            if ((_scriptBlock.LanguageMode.HasValue) &&
                (_scriptBlock.LanguageMode != Context.LanguageMode))
            {
                oldLanguageMode = Context.LanguageMode;
                newLanguageMode = _scriptBlock.LanguageMode;
            }

            try
            {
                var oldScopeOrigin = this.Context.EngineSessionState.CurrentScope.ScopeOrigin;

                try
                {
                    this.Context.EngineSessionState.CurrentScope.ScopeOrigin =
                        this._dontUseScopeCommandOrigin ? CommandOrigin.Internal : this.Command.CommandOrigin;

                    // Set the language mode. We do this before EnterScope(), so that the language
                    // mode is appropriately applied for evaluation parameter defaults.
                    if (newLanguageMode.HasValue)
                    {
                        Context.LanguageMode = newLanguageMode.Value;
                    }

                    bool? oldLangModeTransitionStatus = null;
                    try
                    {
                        // If it's from ConstrainedLanguage to FullLanguage, indicate the transition before parameter binding takes place.
                        if (oldLanguageMode == PSLanguageMode.ConstrainedLanguage && newLanguageMode == PSLanguageMode.FullLanguage)
                        {
                            oldLangModeTransitionStatus = Context.LanguageModeTransitionInParameterBinding;
                            Context.LanguageModeTransitionInParameterBinding = true;
                        }

                        EnterScope();
                    }
                    finally
                    {
                        if (oldLangModeTransitionStatus.HasValue)
                        {
                            // Revert the transition state to old value after doing the parameter binding
                            Context.LanguageModeTransitionInParameterBinding = oldLangModeTransitionStatus.Value;
                        }
                    }

                    if (commandRuntime.ErrorMergeTo == MshCommandRuntime.MergeDataStream.Output)
                    {
                        Context.RedirectErrorPipe(commandRuntime.OutputPipe);
                    }
                    else if (commandRuntime.ErrorOutputPipe.IsRedirected)
                    {
                        Context.RedirectErrorPipe(commandRuntime.ErrorOutputPipe);
                    }

                    if (dollarUnderbar != AutomationNull.Value)
                    {
                        _localsTuple.SetAutomaticVariable(AutomaticVariable.Underbar, dollarUnderbar, _context);
                    }
                    else if (_dollarUnderbar != AutomationNull.Value)
                    {
                        _localsTuple.SetAutomaticVariable(AutomaticVariable.Underbar, _dollarUnderbar, _context);
                    }

                    if (inputToProcess != AutomationNull.Value)
                    {
                        if (inputToProcess == null)
                        {
                            inputToProcess = MshCommandRuntime.StaticEmptyArray.GetEnumerator();
                        }
                        else
                        {
                            IList list = inputToProcess as IList;
                            inputToProcess = (list != null)
                                                 ? list.GetEnumerator()
                                                 : LanguagePrimitives.GetEnumerator(inputToProcess);
                        }

                        _localsTuple.SetAutomaticVariable(AutomaticVariable.Input, inputToProcess, _context);
                    }

                    clause(_functionContext);
                }
                catch (TargetInvocationException tie)
                {
                    // DynamicInvoke wraps exceptions, unwrap them here.
                    throw tie.InnerException;
                }
                finally
                {
                    Context.ShellFunctionErrorOutputPipe = oldErrorOutputPipe;

                    if (oldLanguageMode.HasValue)
                    {
                        Context.LanguageMode = oldLanguageMode.Value;
                    }

                    Context.EngineSessionState.CurrentScope.ScopeOrigin = oldScopeOrigin;
                }
            }
            catch (ExitException ee)
            {
                if (!this.FromScriptFile || _rethrowExitException)
                {
                    throw;
                }

                this._exitWasCalled = true;

                int exitCode = (int)ee.Argument;
                this.Command.Context.SetVariable(SpecialVariables.LastExitCodeVarPath, exitCode);

                if (exitCode != 0)
                    this.commandRuntime.PipelineProcessor.ExecutionFailed = true;
            }
            catch (FlowControlException)
            {
                throw;
            }
            catch (RuntimeException e)
            {
                // This method always throws.
                ManageScriptException(e);
            }
            catch (Exception e)
            {
                // This cmdlet threw an exception, so wrap it and bubble it up.
                throw ManageInvocationException(e);
            }
        }

        private void EnterScope()
        {
            if (!_argsBound)
            {
                _argsBound = true;

                // Parameter binder may need to write warning messages for obsolete parameters
                using (commandRuntime.AllowThisCommandToWrite(false))
                {
                    this.ScriptParameterBinderController.BindCommandLineParameters(arguments);
                }

                _localsTuple.SetAutomaticVariable(AutomaticVariable.PSBoundParameters,
                                                  this.ScriptParameterBinderController.CommandLineParameters.GetValueToBindToPSBoundParameters(), _context);
            }
        }

        protected override void OnSetCurrentScope()
        {
            // When dotting a script, push the locals of automatic variables to
            // the 'DottedScopes' of the current scope.
            if (!UseLocalScope)
            {
                CommandSessionState.CurrentScope.DottedScopes.Push(_localsTuple);
            }
        }

        protected override void OnRestorePreviousScope()
        {
            // When dotting a script, pop the locals of automatic variables from
            // the 'DottedScopes' of the current scope.
            if (!UseLocalScope)
            {
                CommandSessionState.CurrentScope.DottedScopes.Pop();
            }
        }
    }
}
