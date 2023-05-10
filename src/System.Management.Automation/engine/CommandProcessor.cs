// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Management.Automation.Internal;

using Microsoft.PowerShell.Commands;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// Class definition of CommandProcessor - This class provides interface to create
    /// and execute commands written in CLS compliant languages.
    /// </summary>
    internal class CommandProcessor : CommandProcessorBase
    {
        #region ctor

        static CommandProcessor()
        {
            s_constructInstanceCache = new ConcurrentDictionary<Type, Func<Cmdlet>>();

            // Avoid jitting constructors some of the more commonly used cmdlets in SMA.dll - not meant to be
            // exhaustive b/c many cmdlets aren't called at all, so we'd never even need an entry in the cache.
            s_constructInstanceCache.GetOrAdd(typeof(ForEachObjectCommand), () => new ForEachObjectCommand());
            s_constructInstanceCache.GetOrAdd(typeof(WhereObjectCommand), () => new WhereObjectCommand());
            s_constructInstanceCache.GetOrAdd(typeof(ImportModuleCommand), () => new ImportModuleCommand());
            s_constructInstanceCache.GetOrAdd(typeof(GetModuleCommand), () => new GetModuleCommand());
            s_constructInstanceCache.GetOrAdd(typeof(GetHelpCommand), () => new GetHelpCommand());
            s_constructInstanceCache.GetOrAdd(typeof(InvokeCommandCommand), () => new InvokeCommandCommand());
            s_constructInstanceCache.GetOrAdd(typeof(GetCommandCommand), () => new GetCommandCommand());
            s_constructInstanceCache.GetOrAdd(typeof(OutDefaultCommand), () => new OutDefaultCommand());
            s_constructInstanceCache.GetOrAdd(typeof(OutHostCommand), () => new OutHostCommand());
            s_constructInstanceCache.GetOrAdd(typeof(OutNullCommand), () => new OutNullCommand());
            s_constructInstanceCache.GetOrAdd(typeof(SetStrictModeCommand), () => new SetStrictModeCommand());
            s_constructInstanceCache.GetOrAdd(typeof(FormatDefaultCommand), () => new FormatDefaultCommand());
            s_constructInstanceCache.GetOrAdd(typeof(OutLineOutputCommand), () => new OutLineOutputCommand());
        }

        /// <summary>
        /// Initializes the new instance of CommandProcessor class.
        /// </summary>
        /// <param name="cmdletInfo">
        /// The information about the cmdlet.
        /// </param>
        /// <param name="context">
        /// PowerShell engine execution context for this command.
        /// </param>
        /// <exception cref="CommandNotFoundException">
        /// If there was a failure creating an instance of the cmdlet type.
        /// </exception>
        internal CommandProcessor(CmdletInfo cmdletInfo, ExecutionContext context) : base(cmdletInfo)
        {
            this._context = context;
            Init(cmdletInfo);
        }

        /// <summary>
        /// This is the constructor for script as cmdlet.
        /// </summary>
        /// <param name="scriptCommandInfo">
        /// The information about the cmdlet.
        /// </param>
        /// <param name="context">
        /// PowerShell engine execution context for this command.
        /// </param>
        /// <param name="useLocalScope"></param>
        /// <param name="sessionState"></param>
        /// <param name="fromScriptFile">True when the script to be executed came from a file (as opposed to a function, or interactive input).</param>
        internal CommandProcessor(IScriptCommandInfo scriptCommandInfo, ExecutionContext context, bool useLocalScope, bool fromScriptFile, SessionStateInternal sessionState)
            : base(scriptCommandInfo as CommandInfo)
        {
            this._context = context;
            this._useLocalScope = useLocalScope;
            this._fromScriptFile = fromScriptFile;
            this.CommandSessionState = sessionState;
            Init(scriptCommandInfo);
        }

        #endregion ctor

        #region internal members

        /// <summary>
        /// Returns a CmdletParameterBinderController for the specified command.
        /// </summary>
        /// <param name="command">
        /// The cmdlet to bind parameters to.
        /// </param>
        /// <returns>
        /// A new instance of a CmdletParameterBinderController.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// if <paramref name="command"/> is not a Cmdlet.
        /// </exception>
        internal ParameterBinderController NewParameterBinderController(InternalCommand command)
        {
            if (!(command is Cmdlet cmdlet))
            {
                throw PSTraceSource.NewArgumentException(nameof(command));
            }

            ParameterBinderBase parameterBinder;
            IScriptCommandInfo scriptCommandInfo = CommandInfo as IScriptCommandInfo;
            if (scriptCommandInfo != null)
            {
                parameterBinder = new ScriptParameterBinder(scriptCommandInfo.ScriptBlock, cmdlet.MyInvocation, this._context, cmdlet, CommandScope);
            }
            else
            {
                parameterBinder = new ReflectionParameterBinder(cmdlet, cmdlet);
            }

            _cmdletParameterBinderController = new CmdletParameterBinderController(cmdlet, CommandInfo.CommandMetadata, parameterBinder);

            return _cmdletParameterBinderController;
        }

        internal CmdletParameterBinderController CmdletParameterBinderController
        {
            get
            {
                if (_cmdletParameterBinderController == null)
                {
                    NewParameterBinderController(this.Command);
                }

                return _cmdletParameterBinderController;
            }
        }

        private CmdletParameterBinderController _cmdletParameterBinderController;

        /// <summary>
        /// Get the ObsoleteAttribute of the current command.
        /// </summary>
        internal override ObsoleteAttribute ObsoleteAttribute
        {
            get { return _obsoleteAttribute; }
        }

        private ObsoleteAttribute _obsoleteAttribute;

        /// <summary>
        /// Binds the specified command-line parameters to the target.
        /// </summary>
        /// <returns>
        /// true if encode succeeds otherwise false.
        /// </returns>
        /// <exception cref="ParameterBindingException">
        /// If any parameters fail to bind,
        /// or
        /// If any mandatory parameters are missing.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If there is an error generating the metadata for dynamic parameters.
        /// </exception>
        internal void BindCommandLineParameters()
        {
            using (commandRuntime.AllowThisCommandToWrite(false))
            {
                Diagnostics.Assert(
                    this.CmdletParameterBinderController != null,
                    "A parameter binder controller should always be available");

                // Always set the hash table on MyInvocation so it's available for both interpreted cmdlets
                // as well as compiled ones.
                this.CmdletParameterBinderController.CommandLineParameters.UpdateInvocationInfo(this.Command.MyInvocation);
                this.Command.MyInvocation.UnboundArguments = new Collections.Generic.List<object>();

                this.CmdletParameterBinderController.BindCommandLineParameters(arguments);
            }
        }

        /// <summary>
        /// Prepares the command. Encodes the command-line parameters
        /// JonN     2003-04-02 Split from Execute()
        /// </summary>
        /// <exception cref="ParameterBindingException">
        /// If any parameters fail to bind,
        /// or
        /// If any mandatory parameters are missing.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If there is an error generating the metadata for dynamic parameters.
        /// </exception>
        internal override void Prepare(IDictionary psDefaultParameterValues)
        {
            // Note that Prepare() and DoBegin() should NOT be combined.
            // Reason: Encoding of commandline parameters happen as part of
            // Prepare(). If they are combined, the first command's
            // DoBegin() will be called before the next command's Prepare().
            // Since BeginProcessing() can write objects to the downstream
            // commandlet, it will end up calling DoExecute() (from Pipe.Add())
            // before Prepare.

            // Steps involved:
            // (1) Backup the default parameter values
            // (2) Handle input objects - add them to the input pipe.
            // (3) Bind the parameters to properties (encoding)
            // (4) Execute the command method using DoExecute() (repeatedly)

            this.CmdletParameterBinderController.DefaultParameterValues = psDefaultParameterValues;

            Diagnostics.Assert(
                this.Command != null,
                "CommandProcessor did not initialize Command\n" + this.CommandInfo.Name);

            PSLanguageMode? oldLanguageMode = null;
            bool? oldLangModeTransitionStatus = null;
            try
            {
                var scriptCmdletInfo = this.CommandInfo as IScriptCommandInfo;
                if (scriptCmdletInfo != null &&
                    scriptCmdletInfo.ScriptBlock.LanguageMode.HasValue &&
                    scriptCmdletInfo.ScriptBlock.LanguageMode != Context.LanguageMode)
                {
                    // Set the language mode before parameter binding if it's necessary for a script cmdlet, so that the language
                    // mode is appropriately applied for evaluating parameter defaults and argument type conversion.
                    oldLanguageMode = Context.LanguageMode;
                    Context.LanguageMode = scriptCmdletInfo.ScriptBlock.LanguageMode.Value;

                    // If it's from ConstrainedLanguage to FullLanguage, indicate the transition before parameter binding takes place.
                    // When transitioning to FullLanguage mode, we don't want any ConstrainedLanguage restrictions or incorrect Audit messages.
                    if (oldLanguageMode == PSLanguageMode.ConstrainedLanguage && Context.LanguageMode == PSLanguageMode.FullLanguage)
                    {
                        oldLangModeTransitionStatus = Context.LanguageModeTransitionInParameterBinding;
                        Context.LanguageModeTransitionInParameterBinding = true;
                    }
                }

                BindCommandLineParameters();
            }
            finally
            {
                if (oldLanguageMode.HasValue)
                {
                    // Revert to the original language mode after doing the parameter binding
                    Context.LanguageMode = oldLanguageMode.Value;
                }

                if (oldLangModeTransitionStatus.HasValue)
                {
                    // Revert the transition state to old value after doing the parameter binding
                    Context.LanguageModeTransitionInParameterBinding = oldLangModeTransitionStatus.Value;
                }
            }
        }

        protected override void OnSetCurrentScope()
        {
            // When dotting a script cmdlet, push the locals of automatic variables to
            // the 'DottedScopes' of the current scope.
            PSScriptCmdlet scriptCmdlet = this.Command as PSScriptCmdlet;
            if (scriptCmdlet != null && !UseLocalScope)
            {
                scriptCmdlet.PushDottedScope(CommandSessionState.CurrentScope);
            }
        }

        protected override void OnRestorePreviousScope()
        {
            // When dotting a script cmdlet, pop the locals of automatic variables from
            // the 'DottedScopes' of the current scope.
            PSScriptCmdlet scriptCmdlet = this.Command as PSScriptCmdlet;
            if (scriptCmdlet != null && !UseLocalScope)
            {
                scriptCmdlet.PopDottedScope(CommandSessionState.CurrentScope);
            }
        }

        /// <summary>
        /// Execute BeginProcessing part of command.
        /// </summary>
        internal override void DoBegin()
        {
            if (!RanBeginAlready && CmdletParameterBinderController.ObsoleteParameterWarningList != null)
            {
                using (CommandRuntime.AllowThisCommandToWrite(false))
                {
                    // Write out warning messages for the bound obsolete parameters.
                    // The warning message are generated during parameter binding, but we delay writing
                    // them out until now so that the -WarningAction will be respected as expected.
                    foreach (WarningRecord warningRecord in CmdletParameterBinderController.ObsoleteParameterWarningList)
                    {
                        CommandRuntime.WriteWarning(warningRecord);
                    }
                }

                // Clear up the warning message list
                CmdletParameterBinderController.ObsoleteParameterWarningList.Clear();
            }

            base.DoBegin();
        }

        /// <summary>
        /// This calls the command.  It assumes that Prepare() has already been called.
        /// JonN     2003-04-02 Split from Execute()
        /// </summary>
        /// <exception cref="PipelineStoppedException">
        /// a terminating error occurred, or the pipeline was otherwise stopped
        /// </exception>
        internal override void ProcessRecord()
        {
            // Invoke the Command method with the request object
            if (!this.RanBeginAlready)
            {
                RanBeginAlready = true;
                try
                {
                    using (commandRuntime.AllowThisCommandToWrite(true))
                    {
                        if (Context._debuggingMode > 0 && Command is not PSScriptCmdlet)
                        {
                            Context.Debugger.CheckCommand(this.Command.MyInvocation);
                        }

                        Command.DoBeginProcessing();
                    }
                }
                catch (Exception e)
                {
                    // This cmdlet threw an exception, so wrap it and bubble it up.
                    throw ManageInvocationException(e);
                }
            }

            Debug.Assert(this.Command.MyInvocation.PipelineIterationInfo != null); // this should have been allocated when the pipeline was started

            while (Read())
            {
                Pipe oldErrorOutputPipe = _context.ShellFunctionErrorOutputPipe;
                Exception exceptionToThrow = null;
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

                    // NOTICE-2004/06/08-JonN 959638
                    using (commandRuntime.AllowThisCommandToWrite(true))
                    using (ParameterBinderBase.bindingTracer.TraceScope("CALLING ProcessRecord"))
                    {
                        if (CmdletParameterBinderController.ObsoleteParameterWarningList != null &&
                            CmdletParameterBinderController.ObsoleteParameterWarningList.Count > 0)
                        {
                            // Write out warning messages for the pipeline-value-bound obsolete parameters.
                            // The warning message are generated during parameter binding, but we delay writing
                            // them out until now so that the -WarningAction will be respected as expected.
                            foreach (WarningRecord warningRecord in CmdletParameterBinderController.ObsoleteParameterWarningList)
                            {
                                CommandRuntime.WriteWarning(warningRecord);
                            }

                            // Clear up the warning message list
                            CmdletParameterBinderController.ObsoleteParameterWarningList.Clear();
                        }

                        this.Command.MyInvocation.PipelineIterationInfo[this.Command.MyInvocation.PipelinePosition]++;

                        Command.DoProcessRecord();
                    }
                }
                catch (RuntimeException rte)
                {
                    // Most exceptions get wrapped here, but an exception that originated from
                    // a throw statement should not get wrapped, so it is just rethrown.
                    if (rte.WasThrownFromThrowStatement)
                    {
                        throw;
                    }

                    exceptionToThrow = rte;
                }
                catch (LoopFlowException)
                {
                    // Don't wrap LoopFlowException, we incorrectly raise a PipelineStoppedException
                    // which gets caught by a script try/catch if we wrap here.
                    throw;
                }
                catch (Exception e)
                {
                    // Catch-all OK, 3rd party callout.
                    exceptionToThrow = e;
                }
                finally
                {
                    _context.ShellFunctionErrorOutputPipe = oldErrorOutputPipe;
                }

                if (exceptionToThrow != null)
                {
                    // This cmdlet threw an exception, so
                    // wrap it and bubble it up.
                    throw ManageInvocationException(exceptionToThrow);
                }
            }
        }

        #endregion public_methods

        #region helper_methods

        /// <summary>
        /// Tells whether it is the first call to Read.
        /// </summary>
        private bool _firstCallToRead = true;

        /// <summary>
        /// Tells whether to bail out in the next call to Read.
        /// </summary>
        private bool _bailInNextCall;

        /// <summary>
        /// Populates the parameters specified from the pipeline.
        /// </summary>
        /// <returns>
        /// A bool indicating whether read succeeded.
        /// </returns>
        /// <exception cref="ParameterBindingException">
        /// If a parameter fails to bind.
        /// or
        /// If a mandatory parameter is missing.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// The pipeline was already stopped.
        /// </exception>
        // 2003/10/07-JonN was public, now internal
        internal sealed override bool Read()
        {
            // (1) If Read() is called for the first time and with pipe closed and
            //     no object in the input pipe and
            //     (typically for the first cmdlet in the pipe & during programmatic
            //     execution of a command), Read() will succeed (return true) for
            //     only one time (so that the
            // (2) If Read() is called with some input objects in the pipeline, it
            //     processes the input
            //     object one at a time and adds parameters from the input object
            //     to the list of parameters. If
            //     added to the error pipe and Read() will continue to read the
            //     next object in the pipe.
            // (3) Read() will return false if there are no objects in the pipe
            //     for processing.
            // (4) Read() will return true if the parameters are encoded in the
            //     request - signals ready for execution.
            // (5) Read() will refresh the properties that are encoded via pipeline
            //     parameters in the next
            //     call to Read() [To their default values, so that the
            //     next execution of the command will
            //     not work on previously specified parameter].
            // If the flag 'bail in next call' is true, then bail out returning false.
            if (_bailInNextCall)
                return false;

            // ProcessRecord() will loop on Command.Read(), and continue calling
            // ProcessRecord() until the incoming pipe is empty.  We need to
            // stop this loop if a downstream cmdlet broke guidelines and
            // "swallowed" a PipelineStoppedException.
            Command.ThrowIfStopping();

            // Prepare the default value parameter list if this is the first call to Read
            if (_firstCallToRead)
            {
                _firstCallToRead = false;
                if (!IsPipelineInputExpected())
                {
                    // Cmdlet should operate only with command-line parameters
                    // Let the command Execute with the specified command line parameters
                    // And Read should return false in the next call.
                    _bailInNextCall = true;
                    return true;
                }
            }

            // If this cmdlet has any members that could be bound
            // from the pipeline, do that now. In fact, we always try and
            // do it once anyway because this BindPipelineParameters() does
            // the final binding stage in before executing the cmdlet.

            bool mandatoryParametersSpecified = false;

            while (!mandatoryParametersSpecified)
            {
                // Retrieve the object from the input pipeline
                object inputObject = this.commandRuntime.InputPipe.Retrieve();

                if (inputObject == AutomationNull.Value)
                {
                    // no object in the pipeline, stop reading
                    Command.CurrentPipelineObject = null;
                    return false;
                }

                // If we are reading input for the first command in the pipeline increment PipelineIterationInfo[0], which is the number of items read from the input
                if (this.Command.MyInvocation.PipelinePosition == 1)
                {
                    this.Command.MyInvocation.PipelineIterationInfo[0]++;
                }

                try
                {
                    // Process the input pipeline object
                    if (!ProcessInputPipelineObject(inputObject))
                    {
                        // The input object was not bound to any parameters of the cmdlet.
                        // Write a non-terminating error and continue with the next input
                        // object.
                        WriteInputObjectError(
                            inputObject,
                            ParameterBinderStrings.InputObjectNotBound,
                            "InputObjectNotBound");
                        continue;
                    }
                }
                catch (ParameterBindingException bindingError)
                {
                    // Set the target and write the error
                    bindingError.ErrorRecord.SetTargetObject(inputObject);

                    ErrorRecord errorRecord =
                        new ErrorRecord(
                            bindingError.ErrorRecord,
                            bindingError);

                    this.commandRuntime._WriteErrorSkipAllowCheck(errorRecord);
                    continue;
                }

                Collection<MergedCompiledCommandParameter> missingMandatoryParameters;

                using (ParameterBinderBase.bindingTracer.TraceScope(
                    "MANDATORY PARAMETER CHECK on cmdlet [{0}]",
                    this.CommandInfo.Name))
                {
                    // Check for unbound mandatory parameters but don't prompt
                    mandatoryParametersSpecified =
                        this.CmdletParameterBinderController.HandleUnboundMandatoryParameters(out missingMandatoryParameters);
                }

                if (!mandatoryParametersSpecified)
                {
                    string missingParameters =
                        CmdletParameterBinderController.BuildMissingParamsString(missingMandatoryParameters);

                    // Since the input object did not satisfy all mandatory parameters
                    // for the command, write an ErrorRecord to the error pipe with
                    // the target as the input object.

                    WriteInputObjectError(
                        inputObject,
                        ParameterBinderStrings.InputObjectMissingMandatory,
                        "InputObjectMissingMandatory",
                        missingParameters);
                }
            }

            return true;
        }

        /// <summary>
        /// Writes an ErrorRecord to the commands error pipe because the specified
        /// input object was not bound to the command.
        /// </summary>
        /// <param name="inputObject">
        /// The pipeline input object that was not bound.
        /// </param>
        /// <param name="resourceString">
        /// The error message.
        /// </param>
        /// <param name="errorId">
        /// The resource ID of the error message is also used as error ID
        /// of the ErrorRecord.
        /// </param>
        /// <param name="args">
        /// Additional arguments to be formatted into the error message that represented in <paramref name="resourceString"/>.
        /// </param>
        private void WriteInputObjectError(
            object inputObject,
            string resourceString,
            string errorId,
            params object[] args)
        {
            Type inputObjectType = inputObject?.GetType();

            ParameterBindingException bindingException = new ParameterBindingException(
                ErrorCategory.InvalidArgument,
                this.Command.MyInvocation,
                null,
                null,
                null,
                inputObjectType,
                resourceString,
                errorId,
                args);

            ErrorRecord errorRecord =
                new ErrorRecord(
                    bindingException,
                    errorId,
                    ErrorCategory.InvalidArgument,
                    inputObject);

            errorRecord.SetInvocationInfo(this.Command.MyInvocation);

            this.commandRuntime._WriteErrorSkipAllowCheck(errorRecord);
        }

        /// <summary>
        /// Reads an object from an input pipeline and attempts to bind the parameters.
        /// </summary>
        /// <param name="inputObject">
        /// The pipeline input object to be processed.
        /// </param>
        /// <returns>
        /// False the pipeline input object was not bound in any way to the command.
        /// </returns>
        /// <exception cref="ParameterBindingException">
        /// If a ShouldProcess parameter is specified but the cmdlet does not support
        /// ShouldProcess.
        /// or
        /// If an error occurred trying to bind a parameter from the pipeline object.
        /// </exception>
        private bool ProcessInputPipelineObject(object inputObject)
        {
            PSObject inputToOperateOn = null;

            // Use ETS to retrieve properties from the input object
            // turn it into a shell object if it isn't one already...
            // we depend on PSObject.AsPSObject() being idempotent - if
            // it's already a shell object, don't encapsulate it again.
            if (inputObject != null)
            {
                inputToOperateOn = PSObject.AsPSObject(inputObject);
            }

            Command.CurrentPipelineObject = inputToOperateOn;

            return this.CmdletParameterBinderController.BindPipelineParameters(inputToOperateOn);
        }

        private static readonly ConcurrentDictionary<Type, Func<Cmdlet>> s_constructInstanceCache;

        private static Cmdlet ConstructInstance(Type type)
        {
            // Call the default constructor if type derives from Cmdlet.
            // Return null (and the caller will generate an appropriate error) if
            // type does not derive from Cmdlet.  We do it this way so the expensive type check
            // is performed just once per type.

            return s_constructInstanceCache.GetOrAdd(type,
                t => Expression.Lambda<Func<Cmdlet>>(
                     typeof(Cmdlet).IsAssignableFrom(t)
                        ? (Expression)Expression.New(t)
                        : Expression.Constant(null, typeof(Cmdlet))).Compile())();
        }

        /// <summary>
        /// Initializes the command's request object.
        /// </summary>
        /// <param name="cmdletInformation">
        /// The information about the cmdlet.
        /// </param>
        /// <exception cref="CmdletInvocationException">
        /// If the constructor for the cmdlet threw an exception.
        /// </exception>
        /// <exception cref="MemberAccessException">
        /// The type referenced by <paramref name="cmdletInformation"/> referred to an
        /// abstract type or them member was invoked via a late-binding mechanism.
        /// </exception>
        /// <exception cref="TypeLoadException">
        /// If <paramref name="cmdletInformation"/> refers to a type that is invalid.
        /// </exception>
        private void Init(CmdletInfo cmdletInformation)
        {
            Diagnostics.Assert(cmdletInformation != null, "Constructor should throw exception if LookupCommand returned null.");

            Cmdlet newCmdlet = null;
            Exception initError = null;
            string errorIdAndResourceId = null;
            string resourceStr = null;
            try
            {
                // Create the request object
                newCmdlet = ConstructInstance(cmdletInformation.ImplementingType);
                if (newCmdlet == null)
                {
                    // We could test the inheritance before constructing, but that's
                    // expensive.  Much cheaper to just check for null.
                    initError = new InvalidCastException();
                    errorIdAndResourceId = "CmdletDoesNotDeriveFromCmdletType";
                    resourceStr = DiscoveryExceptions.CmdletDoesNotDeriveFromCmdletType;
                }
            }
            catch (MemberAccessException memberAccessException)
            {
                initError = memberAccessException;
            }
            catch (TypeLoadException typeLoadException)
            {
                initError = typeLoadException;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                // We don't have a Command or InvocationInfo at this point,
                // since the command failed to initialize.
                var commandException = new CmdletInvocationException(e, null);

                // Log a command health event
                MshLog.LogCommandHealthEvent(
                    this._context,
                    commandException,
                    Severity.Warning);

                throw commandException;
            }

            if (initError != null)
            {
                // Log a command health event
                MshLog.LogCommandHealthEvent(
                    this._context,
                    initError,
                    Severity.Warning);

                CommandNotFoundException exception =
                    new CommandNotFoundException(
                        cmdletInformation.Name,
                        initError,
                        errorIdAndResourceId ?? "CmdletNotFoundException",
                        resourceStr ?? DiscoveryExceptions.CmdletNotFoundException,
                        initError.Message);
                throw exception;
            }

            this.Command = newCmdlet;
            this.CommandScope = Context.EngineSessionState.CurrentScope;

            InitCommon();
        }

        private void Init(IScriptCommandInfo scriptCommandInfo)
        {
            var scriptCmdlet = new PSScriptCmdlet(scriptCommandInfo.ScriptBlock, UseLocalScope, FromScriptFile, _context);
            this.Command = scriptCmdlet;
            this.CommandScope = UseLocalScope
                                    ? this.CommandSessionState.NewScope(_fromScriptFile)
                                    : this.CommandSessionState.CurrentScope;

            if (UseLocalScope)
            {
                // Set the 'LocalsTuple' of the new scope to that of the scriptCmdlet
                scriptCmdlet.SetLocalsTupleForNewScope(CommandScope);
            }

            InitCommon();

            // If the script has been dotted, throw an error if it's from a different language mode.
            if (!this.UseLocalScope)
            {
                ValidateCompatibleLanguageMode(scriptCommandInfo.ScriptBlock, _context, Command.MyInvocation);
            }
        }

        private void InitCommon()
        {
            // set the metadata
            this.Command.CommandInfo = this.CommandInfo;

            // set the ObsoleteAttribute of the current command
            _obsoleteAttribute = this.CommandInfo.CommandMetadata.Obsolete;

            // set the execution context
            this.Command.Context = this._context;

            // Now set up the command runtime for this command.
            try
            {
                this.commandRuntime = new MshCommandRuntime(_context, this.CommandInfo, this.Command);
                this.Command.commandRuntime = this.commandRuntime;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                // Log a command health event

                MshLog.LogCommandHealthEvent(
                    this._context,
                    e,
                    Severity.Warning);

                throw;
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
            if (this.arguments != null)
            {
                foreach (CommandParameterInternal parameter in this.arguments)
                {
                    Dbg.Assert(parameter != null, "CommandProcessor.arguments shouldn't have any null arguments");
                    if (parameter.IsDashQuestion())
                    {
                        helpCategory = HelpCategory.All;
                        // using InvocationName mainly to avoid bogus this.CommandInfo.Name
                        // (when CmdletInfo.Name is initialized from "cmdlet" declaration
                        //  of a scriptblock and when "cmdlet" declaration doesn't specify any name)
                        if ((this.Command != null) && (this.Command.MyInvocation != null) &&
                            (!string.IsNullOrEmpty(this.Command.MyInvocation.InvocationName)))
                        {
                            helpTarget = this.Command.MyInvocation.InvocationName;
                            // Win8: 391035 get-help does not work properly for aliased cmdlets
                            // For aliased cmdlets/functions,example Initialize-Volume -> Format-Volume,
                            // MyInvocation.InvocationName is different from CommandInfo.Name
                            // - CommandInfo.Name points to Format-Volume
                            // - MyInvocation.InvocationName points to Initialize-Volume
                            if (string.Equals(this.Command.MyInvocation.InvocationName, this.CommandInfo.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                helpCategory = this.CommandInfo.HelpCategory;
                            }
                        }
                        else
                        {
                            helpTarget = this.CommandInfo.Name;
                            helpCategory = this.CommandInfo.HelpCategory;
                        }

                        return true;
                    }
                }
            }

            return base.IsHelpRequested(out helpTarget, out helpCategory);
        }

        #endregion helper_methods
    }
}
