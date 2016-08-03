/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Collections;
using System.Management.Automation.Language;
using System.Collections.ObjectModel;
using System.Management.Automation.Internal;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// The base class for all command processor classes. It provides
    /// abstract methods to execute a command.
    /// </summary>
    internal abstract class CommandProcessorBase : IDisposable
    {
        #region ctor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// 

        internal CommandProcessorBase()
        {
        }

        /// <summary>
        /// Initializes the base command processor class with the command metadata
        /// </summary>
        /// 
        /// <param name="commandInfo">
        /// The metadata about the command to run.
        /// </param>
        /// 
        internal CommandProcessorBase(
            CommandInfo commandInfo)
        {
            if (commandInfo == null)
            {
                throw PSTraceSource.NewArgumentNullException("commandInfo");
            }

            CommandInfo = commandInfo;
        }

        #endregion ctor

        #region properties

        private InternalCommand _command;

        // marker of whether BeginProcessing() has already run,
        // also used by CommandProcessor
        internal bool RanBeginAlready;

        // marker of whether this command has already been added to
        // a PipelineProcessor.  It is an error to add the same command
        // more than once.
        internal bool AddedToPipelineAlready
        {
            get { return _addedToPipelineAlready; }
            set { _addedToPipelineAlready = value; }
        }
        internal bool _addedToPipelineAlready;

        /// <summary>
        /// Gets the CommandInfo for the command this command processor represents
        /// </summary>
        /// <value></value>
        internal CommandInfo CommandInfo { get; set; }

        /// <summary>
        /// This indicates whether this command processor is created from
        /// a script file. 
        /// </summary>
        /// <remarks> 
        /// Script command processor created from a script file is special
        /// in following two perspect, 
        /// 
        ///     1. New scope created needs to be a 'script' scope in the 
        ///        sense that it needs to handle $script: variables. 
        ///        For normal functions or scriptblocks, script scope
        ///        variables are not supported. 
        ///        
        ///     2. ExitException will be handled by setting lastExitCode. 
        ///        For normal functions or scriptblocks, exit command will
        ///        kill current powershell session. 
        /// </remarks>
        public bool FromScriptFile { get { return _fromScriptFile; } }
        protected bool _fromScriptFile = false;

        /// <summary>
        /// If this flag is true, the commands in this Pipeline will redirect 
        /// the global error output pipe to the command's error output pipe.
        /// 
        /// (see the comment in Pipeline.RedirectShellErrorOutputPipe for an 
        /// explanation of why this flag is needed)
        /// </summary>
        internal bool RedirectShellErrorOutputPipe { get; set; } = false;

        /// <summary>
        /// Gets or sets the command object.
        /// </summary>
        internal InternalCommand Command
        {
            get { return _command; }
            set
            {
                // The command runtime needs to be set up...
                if (value != null)
                {
                    value.commandRuntime = this.commandRuntime;
                    if (_command != null)
                        value.CommandInfo = _command.CommandInfo;

                    // Set the execution context for the command it's currently
                    // null and our context has already been set up.
                    if (value.Context == null && _context != null)
                        value.Context = _context;
                }
                _command = value;
            }
        }

        /// <summary>
        /// Get the ObsoleteAttribute of the current command
        /// </summary>
        internal virtual ObsoleteAttribute ObsoleteAttribute
        {
            get { return null; }
        }
        // Full Qualified ID for the obsolete command warning
        private const string FQIDCommandObsolete = "CommandObsolete";

        /// <summary>
        /// The command runtime used for this instance of a command processor.
        /// </summary>
        protected MshCommandRuntime commandRuntime;
        internal MshCommandRuntime CommandRuntime
        {
            get { return commandRuntime; }
            set { commandRuntime = value; }
        }

        /// <summary>
        /// For commands that use the scope stack, if this flag is
        /// true, don't create a new scope when running this command.
        /// </summary>
        /// <value></value>
        internal bool UseLocalScope
        {
            get { return _useLocalScope; }
            set { _useLocalScope = value; }
        }
        protected bool _useLocalScope;

        /// <summary>
        /// Ensures that the provided script block is compatible with the current language mode - to
        /// be used when a script block is being dotted.
        /// </summary>
        /// <param name="scriptBlock">The script block being dotted</param>
        /// <param name="languageMode">The current language mode</param>
        /// <param name="invocationInfo">The invocation info about the command</param>
        protected static void ValidateCompatibleLanguageMode(ScriptBlock scriptBlock,
            PSLanguageMode languageMode,
            InvocationInfo invocationInfo)
        {
            // If we are in a constrained language mode (Core or Restricted), block it.
            // This goes both ways:
            //    - Can't dot something from a more permissive mode, since that would probably expose
            //      functions that were never designed to handle untrusted data.
            //    - Can't dot something from a less permissive mode, since that might introduce tained
            //      data into the current scope.
            if ((scriptBlock.LanguageMode.HasValue) &&
                (scriptBlock.LanguageMode != languageMode) &&
                ((languageMode == PSLanguageMode.RestrictedLanguage) ||
                (languageMode == PSLanguageMode.ConstrainedLanguage)))
            {
                ErrorRecord errorRecord = new ErrorRecord(
                    new NotSupportedException(
                        DiscoveryExceptions.DotSourceNotSupported),
                        "DotSourceNotSupported",
                        ErrorCategory.InvalidOperation,
                        null);
                errorRecord.SetInvocationInfo(invocationInfo);
                throw new CmdletInvocationException(errorRecord);
            }
        }

        /// <summary>
        /// The execution context used by the system.
        /// </summary>
        protected ExecutionContext _context;
        internal ExecutionContext Context
        {
            get { return _context; }
            set { _context = value; }
        }

        /// <summary>
        /// Etw activity for this pipeline
        /// </summary>
        internal Guid PipelineActivityId { get; set; } = Guid.Empty;

        #endregion properties

        #region methods

        #region handling of -? parameter

        /// <summary>
        /// Checks if user has requested help (for example passing "-?" parameter for a cmdlet)
        /// and if yes, then returns the help target to display.
        /// </summary>
        /// <param name="helpTarget">help target to request</param>
        /// <param name="helpCategory">help category to request</param>
        /// <returns><c>true</c> if user requested help; <c>false</c> otherwise</returns>
        internal virtual bool IsHelpRequested(out string helpTarget, out HelpCategory helpCategory)
        {
            // by default we don't handle "-?" parameter at all
            // (we want to do the checks only for cmdlets - this method is overridden in CommandProcessor)
            helpTarget = null;
            helpCategory = HelpCategory.None;
            return false;
        }

        /// <summary>
        /// Creates a command procesor for "get-help [helpTarget]"
        /// </summary>
        /// <param name="context">context for the command processor</param>
        /// <param name="helpTarget">help target</param>
        /// <param name="helpCategory">help category</param>
        /// <returns>command processor for "get-help [helpTarget]"</returns>
        internal static CommandProcessorBase CreateGetHelpCommandProcessor(
            ExecutionContext context,
            string helpTarget,
            HelpCategory helpCategory)
        {
            if (context == null)
            {
                throw PSTraceSource.NewArgumentNullException("context");
            }
            if (string.IsNullOrEmpty(helpTarget))
            {
                throw PSTraceSource.NewArgumentNullException("helpTarget");
            }

            CommandProcessorBase helpCommandProcessor = context.CreateCommand("get-help", false);
            var cpi = CommandParameterInternal.CreateParameterWithArgument(
                PositionUtilities.EmptyExtent, "Name", "-Name:",
                PositionUtilities.EmptyExtent, helpTarget,
                false);
            helpCommandProcessor.AddParameter(cpi);
            cpi = CommandParameterInternal.CreateParameterWithArgument(
                PositionUtilities.EmptyExtent, "Category", "-Category:",
                PositionUtilities.EmptyExtent, helpCategory.ToString(),
                false);
            helpCommandProcessor.AddParameter(cpi);
            return helpCommandProcessor;
        }

        #endregion

        /// <summary>
        /// Tells whether pipeline input is expected or not.
        /// </summary>
        /// <returns>A bool indicating whether pipeline input is expected.</returns>
        internal bool IsPipelineInputExpected()
        {
            return commandRuntime.IsPipelineInputExpected;
        }

        /// <summary>
        /// If you want this command to execute in other than the default session
        /// state, use this API to get and set that session state instance...
        /// </summary>
        internal SessionStateInternal CommandSessionState { get; set; }

        /// <summary>
        /// Gets sets the session state scope for this command processor object
        /// </summary>
        protected internal SessionStateScope CommandScope { get; protected set; }

        protected virtual void OnSetCurrentScope()
        {
        }

        protected virtual void OnRestorePreviousScope()
        {
        }

        /// <summary>
        /// This method sets the current session state scope to the execution scope for the pipeline
        /// that was stored in the pipeline manager when it was first invoked.
        /// </summary>
        internal void SetCurrentScopeToExecutionScope()
        {
            // Make sure we have a session state instance for this command.
            // If one hasn't been explicitly set, then use the session state
            // available on the engine execution context...
            if (CommandSessionState == null)
            {
                CommandSessionState = Context.EngineSessionState;
            }

            // Store off the current scope
            _previousScope = CommandSessionState.CurrentScope;
            _previousCommandSessionState = Context.EngineSessionState;
            Context.EngineSessionState = CommandSessionState;

            // Set the current scope to the pipeline execution scope
            CommandSessionState.CurrentScope = CommandScope;

            OnSetCurrentScope();
        }

        /// <summary>
        /// Restores the current session state scope to the scope which was active when SetCurrentScopeToExecutionScope
        /// was called.
        /// </summary>
        /// 
        internal void RestorePreviousScope()
        {
            OnRestorePreviousScope();

            Context.EngineSessionState = _previousCommandSessionState;

            if (_previousScope != null)
            {
                // Restore the scope but use the same session state instance we
                // got it from because the command may have changed the execution context
                // session state...
                CommandSessionState.CurrentScope = _previousScope;
            }
        }

        private SessionStateScope _previousScope;
        private SessionStateInternal _previousCommandSessionState;

        /// <summary>
        /// A collection of arguments that have been added by the parser or
        /// host interfaces. These will be sent to the parameter binder controller
        /// for processing.
        /// </summary>
        /// 
        internal Collection<CommandParameterInternal> arguments = new Collection<CommandParameterInternal>();

        /// <summary>
        /// Adds an unbound parameter.
        /// </summary>
        /// <param name="parameter">
        /// The parameter to add to the unbound arguments list
        /// </param>
        internal void AddParameter(CommandParameterInternal parameter)
        {
            Diagnostics.Assert(parameter != null, "Caller to verify parameter argument");
            arguments.Add(parameter);
        } // AddParameter

        /// <summary>
        /// Prepares the command for execution.
        /// This should be called once before ProcessRecord().
        /// </summary>
        internal abstract void Prepare(IDictionary psDefaultParameterValues);

        /// <summary>
        /// Write warning message for an obsolete command
        /// </summary>
        /// <param name="obsoleteAttr"></param>
        private void HandleObsoleteCommand(ObsoleteAttribute obsoleteAttr)
        {
            string commandName =
                String.IsNullOrEmpty(CommandInfo.Name)
                    ? "script block"
                    : String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                    CommandBaseStrings.ObsoleteCommand, CommandInfo.Name);

            string warningMsg = String.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                CommandBaseStrings.UseOfDeprecatedCommandWarning,
                commandName, obsoleteAttr.Message);

            // We ignore the IsError setting because we don't want to break people when obsoleting a command
            using (this.CommandRuntime.AllowThisCommandToWrite(false))
            {
                this.CommandRuntime.WriteWarning(new WarningRecord(FQIDCommandObsolete, warningMsg));
            }
        }

        /// <summary>
        /// Sets the execution scope for the pipeline and then calls the Prepare
        /// abstract method which gets overridden by derived classes.
        /// </summary>
        internal void DoPrepare(IDictionary psDefaultParameterValues)
        {
            CommandProcessorBase oldCurrentCommandProcessor = _context.CurrentCommandProcessor;
            try
            {
                Context.CurrentCommandProcessor = this;
                SetCurrentScopeToExecutionScope();
                Prepare(psDefaultParameterValues);

                // Check obsolete attribute after Prepare so that -WarningAction will be respected for cmdlets
                if (ObsoleteAttribute != null)
                {
                    // Obsolete command is rare. Put the IF here to avoid method call overhead
                    HandleObsoleteCommand(ObsoleteAttribute);
                }
            }
            catch (Exception e)
            {
                CommandProcessorBase.CheckForSevereException(e);
                if (_useLocalScope)
                {
                    // If we had an exception during Prepare, we're done trying to execute the command
                    // so the scope we created needs to release any resources it hold.s
                    CommandSessionState.RemoveScope(CommandScope);
                }
                throw;
            }
            finally
            {
                Context.CurrentCommandProcessor = oldCurrentCommandProcessor;
                RestorePreviousScope();
            }
        }

        /// <summary>
        /// Called once before ProcessRecord(). Internally it calls
        /// BeginProcessing() of the InternalCommand.
        /// </summary>
        /// <exception cref="PipelineStoppedException">
        /// a terminating error occurred, or the pipeline was otherwise stopped
        /// </exception>
        internal virtual void DoBegin()
        {
            // Note that DoPrepare() and DoBegin() should NOT be combined.
            // Reason: Encoding of commandline parameters happen as part
            // of DoPrepare(). If they are combined, the first command's
            // DoBegin() will be called before the next command's
            // DoPrepare(). Since BeginProcessing() can write objects
            // to the downstream commandlet, it will end up calling
            // DoExecute() (from Pipe.Add()) before DoPrepare.
            if (!RanBeginAlready)
            {
                RanBeginAlready = true;
                Pipe oldErrorOutputPipe = _context.ShellFunctionErrorOutputPipe;
                CommandProcessorBase oldCurrentCommandProcessor = _context.CurrentCommandProcessor;
                try
                {
                    // 
                    // On V1 the output pipe was redirected to the command's output pipe only when it 
                    // was already redirected. This is the original comment explaining this behaviour:
                    //
                    //      NTRAID#Windows Out of Band Releases-926183-2005-12-15
                    //      MonadTestHarness has a bad dependency on an artifact of the current implementation
                    //      The following code only redirects the output pipe if it's already redirected
                    //      to preserve the artifact. The test suites need to be fixed and then this
                    //      the check can be removed and the assignment always done.
                    //
                    // However, this makes the hosting APIs behave differently than commands executed
                    // from the command-line host (for example, see bugs Win7:415915 and Win7:108670).
                    // The RedirectShellErrorOutputPipe flag is used by the V2 hosting API to force the
                    // redirection.
                    //
                    if (this.RedirectShellErrorOutputPipe || _context.ShellFunctionErrorOutputPipe != null)
                    {
                        _context.ShellFunctionErrorOutputPipe = this.commandRuntime.ErrorOutputPipe;
                    }
                    _context.CurrentCommandProcessor = this;
                    using (commandRuntime.AllowThisCommandToWrite(true))
                    {
                        using (ParameterBinderBase.bindingTracer.TraceScope(
                            "CALLING BeginProcessing"))
                        {
                            SetCurrentScopeToExecutionScope();

                            if (Context._debuggingMode > 0 && !(Command is PSScriptCmdlet))
                            {
                                Context.Debugger.CheckCommand(this.Command.MyInvocation);
                            }

                            Command.DoBeginProcessing();
                        }
                    }
                }
                catch (Exception e)
                {
                    CommandProcessorBase.CheckForSevereException(e);

                    // This cmdlet threw an exception, so
                    // wrap it and bubble it up.
                    throw ManageInvocationException(e);
                }
                finally
                {
                    _context.ShellFunctionErrorOutputPipe = oldErrorOutputPipe;
                    _context.CurrentCommandProcessor = oldCurrentCommandProcessor;
                    RestorePreviousScope();
                }
            }
        }

        /// <summary>
        /// This calls the command.  It assumes that DoPrepare() has already been called.
        /// </summary>
        internal abstract void ProcessRecord();

        /// <summary>
        /// This method sets the execution scope to the
        /// appropriate scope for the pipeline and then calls
        /// the ProcessRecord abstract method that derived command processors
        /// override.
        /// </summary>
        /// 
        internal void DoExecute()
        {
            ExecutionContext.CheckStackDepth();

            CommandProcessorBase oldCurrentCommandProcessor = _context.CurrentCommandProcessor;
            try
            {
                Context.CurrentCommandProcessor = this;
                SetCurrentScopeToExecutionScope();
                ProcessRecord();
            }
            finally
            {
                Context.CurrentCommandProcessor = oldCurrentCommandProcessor;
                RestorePreviousScope();
            }
        }

        /// <summary>
        /// Called once after ProcessRecord().
        /// Internally it calls EndProcessing() of the InternalCommand.
        /// </summary>
        /// <exception cref="PipelineStoppedException">
        /// a terminating error occurred, or the pipeline was otherwise stopped
        /// </exception>
        internal virtual void Complete()
        {
            // Call ProcessRecord once from complete. Don't call DoExecute...
            ProcessRecord();

            try
            {
                using (commandRuntime.AllowThisCommandToWrite(true))
                {
                    using (ParameterBinderBase.bindingTracer.TraceScope(
                        "CALLING EndProcessing"))
                    {
                        this.Command.DoEndProcessing();
                    }
                }
            }
            // 2004/03/18-JonN This is understood to be
            // an FXCOP violation, cleared by KCwalina.
            catch (Exception e)
            {
                CommandProcessorBase.CheckForSevereException(e);

                // This cmdlet threw an exception, so
                // wrap it and bubble it up.
                throw ManageInvocationException(e);
            }
        } // Complete

        /// <summary>
        /// Calls the virtual Complete method after setting the appropriate session state scope
        /// </summary>
        /// 
        internal void DoComplete()
        {
            Pipe oldErrorOutputPipe = _context.ShellFunctionErrorOutputPipe;
            CommandProcessorBase oldCurrentCommandProcessor = _context.CurrentCommandProcessor;
            try
            {
                // 
                // On V1 the output pipe was redirected to the command's output pipe only when it 
                // was already redirected. This is the original comment explaining this behaviour:
                //
                //      NTRAID#Windows Out of Band Releases-926183-2005-12-15
                //      MonadTestHarness has a bad dependency on an artifact of the current implementation
                //      The following code only redirects the output pipe if it's already redirected
                //      to preserve the artifact. The test suites need to be fixed and then this
                //      the check can be removed and the assignment always done.
                //
                // However, this makes the hosting APIs behave differently than commands executed
                // from the command-line host (for example, see bugs Win7:415915 and Win7:108670).
                // The RedirectShellErrorOutputPipe flag is used by the V2 hosting API to force the
                // redirection.
                //
                if (this.RedirectShellErrorOutputPipe || _context.ShellFunctionErrorOutputPipe != null)
                {
                    _context.ShellFunctionErrorOutputPipe = this.commandRuntime.ErrorOutputPipe;
                }
                _context.CurrentCommandProcessor = this;

                SetCurrentScopeToExecutionScope();
                Complete();
            }
            finally
            {
                OnRestorePreviousScope();

                _context.ShellFunctionErrorOutputPipe = oldErrorOutputPipe;
                _context.CurrentCommandProcessor = oldCurrentCommandProcessor;

                // Destroy the local scope at this point if there is one...
                if (_useLocalScope && CommandScope != null)
                {
                    CommandSessionState.RemoveScope(CommandScope);
                }

                // and the previous scope...
                if (_previousScope != null)
                {
                    // Restore the scope but use the same session state instance we
                    // got it from because the command may have changed the execution context
                    // session state...
                    CommandSessionState.CurrentScope = _previousScope;
                }

                // Restore the previous session state
                if (_previousCommandSessionState != null)
                {
                    Context.EngineSessionState = _previousCommandSessionState;
                }
            }
        }

        /// <summary>
        /// for diagnostic purposes
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (null != CommandInfo)
                return CommandInfo.ToString();
            return "<NullCommandInfo>"; // does not require localization
        }

        /// <summary>
        /// True if Read() has not be called, false otherwise.
        /// </summary>
        private bool _firstCallToRead = true;

        /// <summary>
        /// Entry point used by the engine to reads the input pipeline object
        /// and binds the parameters.
        /// 
        /// This default implementation reads the next pipeline object and sets
        /// it as the CurrentPipelineObject in the InternalCommand.
        /// </summary>
        /// 
        /// <returns>
        /// True if read succeeds.
        /// </returns>
        ///
        /// does not throw
        internal virtual bool Read()
        {
            // Prepare the default value parameter list if this is the first call to Read
            if (_firstCallToRead)
            {
                _firstCallToRead = false;
            }

            // Retrieve the object from the input pipeline
            object inputObject = this.commandRuntime.InputPipe.Retrieve();

            if (inputObject == AutomationNull.Value)
            {
                return false;
            }

            // If we are reading input for the first command in the pipeline increment PipelineIterationInfo[0], which is the number of items read from the input 
            if (this.Command.MyInvocation.PipelinePosition == 1)
            {
                this.Command.MyInvocation.PipelineIterationInfo[0]++;
            }

            Command.CurrentPipelineObject = LanguagePrimitives.AsPSObjectOrNull(inputObject);

            return true;
        }

#if CORECLR
        // AccessViolationException/StackOverflowException Not In CoreCLR.
        // The CoreCLR team told us to not check for these exceptions because they
        // usually won't be caught.
        internal static void CheckForSevereException(Exception e) { }
#else
        // Keep in sync:
        // S.M.A.CommandProcessorBase.CheckForSevereException
        // S.M.A.Internal.ConsoleHost.CheckForSevereException
        // S.M.A.Commands.CommandsCommon.CheckForSevereException
        // S.M.A.Commands.UtilityCommon.CheckForSevereException
        /// <summary>
        /// Checks whether the exception is a severe exception which should
        /// cause immediate process failure.
        /// </summary>
        /// <param name="e"></param>
        /// <remarks>
        /// CB says 02/23/2005: I personally would err on the side
        /// of treating OOM like an application exception, rather than
        /// a critical system failure.I think this will be easier to justify
        /// in Orcas, if we tease apart the two cases of OOM better.
        /// But even in Whidbey, how likely is it that we couldnt JIT
        /// some backout code?  At that point, the process or possibly
        /// the machine is likely to stop executing soon no matter
        /// what you do in this routine.  So I would just consider
        /// AccessViolationException.  (I understand why you have SO here,
        /// at least temporarily).
        /// </remarks>
        internal static void CheckForSevereException(Exception e)
        {
            if (e is AccessViolationException || e is StackOverflowException)
            {
                try
                {
                    if (!alreadyFailing)
                    {
                        alreadyFailing = true;

                        // Get the ExecutionContext from the thread.
                        ExecutionContext context = Runspaces.LocalPipeline.GetExecutionContextFromTLS();

                        // Log a command health event for this critical error.
                        MshLog.LogCommandHealthEvent(context, e, Severity.Critical);
                    }
                }
                finally
                {
                    WindowsErrorReporting.FailFast(e);
                }
            }
        }
        private static bool alreadyFailing = false;
#endif

        /// <summary>
        /// Wraps the exception which occurred during cmdlet invocation,
        /// stores that as the exception to be returned from
        /// PipelineProcessor.SynchronousExecute, and writes it to
        /// the error variable.
        /// </summary>
        /// 
        /// <param name="e">
        /// The exception to wrap in a CmdletInvocationException or
        /// CmdletProviderInvocationException.
        /// </param>
        /// 
        /// <returns>
        /// Always returns PipelineStoppedException.  The caller should
        /// throw this exception.
        /// </returns>
        /// 
        /// <remarks>
        /// Almost all exceptions which occur during pipeline invocation
        /// are wrapped in CmdletInvocationException before they are stored
        /// in the pipeline.  However, there are several exceptions:
        /// 
        /// AccessViolationException, StackOverflowException:
        /// These are considered to be such severe errors that we
        /// FailFast the process immediately.
        /// 
        /// ProviderInvocationException: In this case, we assume that the
        /// cmdlet is get-item or the like, a thin wrapper around the
        /// provider API.  We discard the original ProviderInvocationException
        /// and re-wrap its InnerException (the real error) in
        /// CmdletProviderInvocationException. This makes it easier to reach
        /// the real error.
        /// 
        /// CmdletInvocationException, ActionPreferenceStopException:
        /// This indicates that the cmdlet itself ran a command which failed.
        /// We could go ahead and wrap the original exception in multiple
        /// layers of CmdletInvocationException, but this makes it difficult
        /// for the caller to access the root problem, plus the serialization
        /// layer might not communicate properties beyond some fixed depth.
        /// Instead, we choose to not re-wrap the exception.
        /// 
        /// PipelineStoppedException: This could mean one of two things.
        /// It usually means that this pipeline has already stopped,
        /// in which case the pipeline already stores the original error.
        /// It could also mean that the cmdlet ran a command which was
        /// stopped by CTRL-C etc, in which case we choose not to
        /// re-wrap the exception as with CmdletInvocationException.
        /// </remarks>
        internal PipelineStoppedException ManageInvocationException(Exception e)
        {
            try
            {
                if (null != Command)
                {
                    do // false loop
                    {
                        ProviderInvocationException pie = e as ProviderInvocationException;
                        if (pie != null)
                        {
                            // If a ProviderInvocationException occurred,
                            // discard the ProviderInvocationException and
                            // re-wrap in CmdletProviderInvocationException 
                            e = new CmdletProviderInvocationException(
                                pie,
                                Command.MyInvocation);
                            break;
                        }

                        // 1021203-2005/05/09-JonN
                        // HaltCommandException will cause the command
                        // to stop, but not be reported as an error.
                        // 906445-2005/05/16-JonN
                        // FlowControlException should not be wrapped
                        if (e is PipelineStoppedException
                            || e is CmdletInvocationException
                            || e is ActionPreferenceStopException
                            || e is HaltCommandException
                            || e is FlowControlException
                            || e is ScriptCallDepthException)
                        {
                            // do nothing; do not rewrap these exceptions
                            break;
                        }

                        RuntimeException rte = e as RuntimeException;
                        if (rte != null && rte.WasThrownFromThrowStatement)
                        {
                            // do not rewrap a script based throw
                            break;
                        }

                        // wrap all other exceptions
                        e = new CmdletInvocationException(
                                    e,
                                    Command.MyInvocation);
                    } while (false);

                    // commandRuntime.ManageException will always throw PipelineStoppedException
                    // Otherwise, just return this exception...

                    // If this exception happened in a transacted cmdlet,
                    // rollback the transaction
                    if (commandRuntime.UseTransaction)
                    {
                        // The "transaction timed out" exception is
                        // exceedingly obtuse. We clarify things here.
                        bool isTimeoutException = false;
                        Exception tempException = e;
                        while (tempException != null)
                        {
                            if (tempException is System.TimeoutException)
                            {
                                isTimeoutException = true;
                                break;
                            }

                            tempException = tempException.InnerException;
                        }

                        if (isTimeoutException)
                        {
                            ErrorRecord errorRecord = new ErrorRecord(
                                new InvalidOperationException(
                                    TransactionStrings.TransactionTimedOut),
                                "TRANSACTION_TIMEOUT",
                                ErrorCategory.InvalidOperation,
                                e);
                            errorRecord.SetInvocationInfo(Command.MyInvocation);

                            e = new CmdletInvocationException(errorRecord);
                        }

                        // Rollback the transaction in the case of errors.
                        if (
                            _context.TransactionManager.HasTransaction
                            &&
                            _context.TransactionManager.RollbackPreference != RollbackSeverity.Never
                           )
                        {
                            Context.TransactionManager.Rollback(true);
                        }
                    }

                    return (PipelineStoppedException)this.commandRuntime.ManageException(e);
                }

                // Upstream cmdlets see only that execution stopped
                // This should only happen if Command is null
                return new PipelineStoppedException();
            }
            catch (Exception)
            {
                // this method shoud not throw exceptions; warn about any violations on checked builds and re-throw
                Diagnostics.Assert(false, "This method should not throw exceptions!");
                throw;
            }
        }

        /// <summary>
        /// Stores the exception to be returned from
        /// PipelineProcessor.SynchronousExecute, and writes it to
        /// the error variable.
        /// </summary>
        /// 
        /// <param name="e">
        /// The exception which occurred during script execution
        /// </param>
        /// 
        /// <exception cref="PipelineStoppedException">
        /// ManageScriptException throws PipelineStoppedException if-and-only-if
        /// the exception is a RuntimeException, otherwise it returns.
        /// This allows the caller to rethrow unexpected exceptions.
        /// </exception>
        internal void ManageScriptException(RuntimeException e)
        {
            if (null != Command && null != commandRuntime.PipelineProcessor)
            {
                commandRuntime.PipelineProcessor.RecordFailure(e, Command);

                // An explicit throw is written to $error as an ErrorRecord, so we
                // skip adding what is more or less a duplicate.
                if (!(e is PipelineStoppedException) && !e.WasThrownFromThrowStatement)
                    commandRuntime.AppendErrorToVariables(e);
            }
            // Upstream cmdlets see only that execution stopped
            throw new PipelineStoppedException();
        }

        /// <summary>
        /// Sometimes we shouldn't be rethrow the exception we previously caught,
        /// such as when the exception is handled by a trap.
        /// </summary>
        internal void ForgetScriptException()
        {
            if (null != Command && null != commandRuntime.PipelineProcessor)
            {
                commandRuntime.PipelineProcessor.ForgetFailure();
            }
        }

        #endregion methods

        #region IDispose
        // 2004/03/05-JonN BrucePay has suggested that the IDispose
        // implementations in PipelineProcessor and CommandProcessor can be
        // removed.
        private bool _disposed;

        /// <summary>
        /// IDisposable implementation
        /// When the command is complete, the CommandProcessorBase should be disposed.
        /// This enables cmdlets to reliably release file handles etc.
        /// without waiting for garbage collection
        /// </summary>
        /// <remarks>We use the standard IDispose pattern</remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // 2004/03/05-JonN Look into using metadata to check
                // whether IDisposable is implemented, in order to avoid
                // this expensive reflection cast.
                IDisposable id = Command as IDisposable;
                if (null != id)
                {
                    id.Dispose();
                }
            }

            _disposed = true;
        }

        /// <summary>
        /// Finalizer for class CommandProcessorBase
        /// </summary>
        ~CommandProcessorBase()
        {
            Dispose(false);
        }

        #endregion IDispose
    }
}

