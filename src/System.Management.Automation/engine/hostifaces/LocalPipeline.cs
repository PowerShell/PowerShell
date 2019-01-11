// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation.Internal;
using System.Management.Automation.Internal.Host;
using System.Management.Automation.Tracing;
#if !UNIX
using System.Security.Principal;
#endif
using System.Threading;
using Microsoft.PowerShell.Commands;
using Microsoft.Win32;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Runspaces
{
    /// <summary>
    /// Pipeline class to be used for LocalRunspace.
    /// </summary>
    internal sealed class LocalPipeline : PipelineBase
    {
        // Each OS platform uses different default stack size for threads:
        //      - Windows 2 MB
        //      - Linux   8 Mb
        //      - MacOs   512 KB
        // We should use the same stack size for pipeline threads on all platforms to get predictable behavior.
        // The stack size we use for pipeline threads is 10MB, which is inherited from Windows PowerShell.
        internal const int DefaultPipelineStackSize = 10_000_000;

        #region constructors

        /// <summary>
        /// Create a Pipeline with an existing command string.
        /// </summary>
        /// <param name="runspace">The LocalRunspace to associate with this
        /// pipeline.
        /// </param>
        /// <param name="command">The command string to parse.</param>
        /// <param name="addToHistory">If true, add pipeline to history.</param>
        /// <param name="isNested">True for nested pipeline.</param>
        internal LocalPipeline(LocalRunspace runspace, string command, bool addToHistory, bool isNested)
            : base((Runspace)runspace, command, addToHistory, isNested)
        {
            _stopper = new PipelineStopper(this);
            InitStreams();
        }

        /// <summary>
        /// Create a Pipeline with an existing command string.
        /// Caller should validate all the parameters.
        /// </summary>
        /// <param name="runspace">
        /// The LocalRunspace to associate with this pipeline.
        /// </param>
        /// <param name="command">
        /// The command to execute.
        /// </param>
        /// <param name="addToHistory">
        /// If true, add the command(s) to the history list of the runspace.
        /// </param>
        /// <param name="isNested">
        /// If true, mark this pipeline as a nested pipeline.
        /// </param>
        /// <param name="inputStream">
        /// Stream to use for reading input objects.
        /// </param>
        /// <param name="errorStream">
        /// Stream to use for writing error objects.
        /// </param>
        /// <param name="outputStream">
        /// Stream to use for writing output objects.
        /// </param>
        /// <param name="infoBuffers">
        /// Buffers used to write progress, verbose, debug, warning, information
        /// information of an invocation.
        /// </param>
        internal LocalPipeline(LocalRunspace runspace,
            CommandCollection command,
            bool addToHistory,
            bool isNested,
            ObjectStreamBase inputStream,
            ObjectStreamBase outputStream,
            ObjectStreamBase errorStream,
            PSInformationalBuffers infoBuffers)
            : base(runspace, command, addToHistory, isNested, inputStream, outputStream, errorStream, infoBuffers)
        {
            _stopper = new PipelineStopper(this);
            InitStreams();
        }

        /// <summary>
        /// Copy constructor to support cloning.
        /// </summary>
        /// <param name="pipeline">The source pipeline.</param>
        internal LocalPipeline(LocalPipeline pipeline)
            : base((PipelineBase)(pipeline))
        {
            _stopper = new PipelineStopper(this);
            InitStreams();
        }

        #endregion constructors

        #region public_methods

        /// <summary>
        /// Creates a new <see cref="Pipeline"/> that is a copy of the current instance.
        /// </summary>
        /// <returns>A new <see cref="Pipeline"/> that is a copy of this instance.</returns>
        public override Pipeline Copy()
        {
            // NTRAID#Windows Out Of Band Releases-915851-2005/09/13
            if (_disposed)
            {
                throw PSTraceSource.NewObjectDisposedException("pipeline");
            }

            return (Pipeline)new LocalPipeline(this);
        }

        #endregion public_methods

        #region private_methods

        /// <summary>
        /// Invoke the pipeline asynchronously with input.
        /// </summary>
        /// <remarks>
        /// Results are returned through the <see cref="Pipeline.Output"/> reader.
        /// </remarks>
        protected override void StartPipelineExecution()
        {
            // NTRAID#Windows Out Of Band Releases-915851-2005/09/13
            if (_disposed)
            {
                throw PSTraceSource.NewObjectDisposedException("pipeline");
            }

            // Note:This method is called from within a lock by parent class. There
            // is no need to lock further.

            // Use input stream in two cases:
            // 1)inputStream is open. In this case PipelineProcessor
            // will call Invoke only if at least one object is added
            // to inputStream.
            // 2)inputStream is closed but there are objects in the stream.
            // NTRAID#Windows Out Of Band Releases-925566-2005/12/09-JonN
            // Remember this here, in the synchronous thread,
            // to avoid timing dependencies in the pipeline thread.
            _useExternalInput = (InputStream.IsOpen || InputStream.Count > 0);

            PSThreadOptions memberOptions = this.IsNested ? PSThreadOptions.UseCurrentThread : this.LocalRunspace.ThreadOptions;

#if !UNIX
            // Use thread proc that supports impersonation flow for new thread start.
            ThreadStart invokeThreadProcDelegate = InvokeThreadProcImpersonate;
            _identityToImpersonate = null;

            // If impersonation identity flow is requested, then get current thread impersonation, if any.
            if ((InvocationSettings != null) && InvocationSettings.FlowImpersonationPolicy)
            {
                Utils.TryGetWindowsImpersonatedIdentity(out _identityToImpersonate);
            }
#else
            // UNIX does not support thread impersonation flow.
            ThreadStart invokeThreadProcDelegate = InvokeThreadProc;
#endif

            switch (memberOptions)
            {
                case PSThreadOptions.Default:
                case PSThreadOptions.UseNewThread:
                    {
                        // Start execution of pipeline in another thread,
                        // and support impersonation flow as needed (Windows only).
                        Thread invokeThread = new Thread(new ThreadStart(invokeThreadProcDelegate), DefaultPipelineStackSize);
                        SetupInvokeThread(invokeThread, true);
#if !CORECLR
                        // No ApartmentState in CoreCLR
                        ApartmentState apartmentState;

                        if (InvocationSettings != null && InvocationSettings.ApartmentState != ApartmentState.Unknown)
                        {
                            apartmentState = InvocationSettings.ApartmentState; // set the user-defined apartmentstate.
                        }
                        else
                        {
                            apartmentState = this.LocalRunspace.ApartmentState; // use the Runspace apartment state
                        }

                        if (apartmentState != ApartmentState.Unknown)
                        {
                            invokeThread.SetApartmentState(apartmentState);
                        }
#endif
                        invokeThread.Start();

                        break;
                    }

                case PSThreadOptions.ReuseThread:
                    {
                        if (this.IsNested)
                        {
                            // If this a nested pipeline we are already in the appropriate thread so we just execute the pipeline here.
                            // Impersonation flow (Windows only) is not needed when using existing thread.
                            SetupInvokeThread(Thread.CurrentThread, true);
                            InvokeThreadProc();
                        }
                        else
                        {
                            // Otherwise we execute the pipeline in the Runspace's thread,
                            // and support information flow on new thread as needed (Windows only).
                            PipelineThread invokeThread = this.LocalRunspace.GetPipelineThread();
                            SetupInvokeThread(invokeThread.Worker, true);
                            invokeThread.Start(invokeThreadProcDelegate);
                        }

                        break;
                    }

                case PSThreadOptions.UseCurrentThread:
                    {
                        Thread oldNestedPipelineThread = NestedPipelineExecutionThread;

                        CultureInfo oldCurrentCulture = CultureInfo.CurrentCulture;
                        CultureInfo oldCurrentUICulture = CultureInfo.CurrentUICulture;

                        try
                        {
                            // Prepare invoke thread.
                            // Impersonation flow (Windows only) is not needed when using existing thread.
                            SetupInvokeThread(Thread.CurrentThread, false);
                            InvokeThreadProc();
                        }
                        finally
                        {
                            NestedPipelineExecutionThread = oldNestedPipelineThread;
                            Thread.CurrentThread.CurrentCulture = oldCurrentCulture;
                            Thread.CurrentThread.CurrentUICulture = oldCurrentUICulture;
                        }

                        break;
                    }

                default:
                    Debug.Assert(false);
                    break;
            }
        }

        /// <summary>
        /// Prepares the invoke thread for execution.
        /// </summary>
        private void SetupInvokeThread(Thread invokeThread, bool changeName)
        {
            NestedPipelineExecutionThread = invokeThread;

#if !CORECLR // No Thread.CurrentCulture In CoreCLR
            invokeThread.CurrentCulture = this.LocalRunspace.ExecutionContext.EngineHostInterface.CurrentCulture;
            invokeThread.CurrentUICulture = this.LocalRunspace.ExecutionContext.EngineHostInterface.CurrentUICulture;
#endif

            if ((invokeThread.Name == null) && changeName) // setup the invoke thread only once
            {
                invokeThread.Name = "Pipeline Execution Thread";
            }
        }

        ///<summary>
        /// Helper method for asynchronous invoke
        ///<returns>Unhandled FlowControl exception if InvocationSettings.ExposeFlowControlExceptions is true.</returns>
        ///</summary>
        private FlowControlException InvokeHelper()
        {
            FlowControlException flowControlException = null;

            PipelineProcessor pipelineProcessor = null;
            try
            {
#if TRANSACTIONS_SUPPORTED
                // 2004/11/08-JeffJon
                // Transactions will not be supported for the Exchange release

                // Add the transaction to this thread
                System.Transactions.Transaction.Current = this.LocalRunspace.ExecutionContext.CurrentTransaction;
#endif
                // Raise the event for Pipeline.Running
                RaisePipelineStateEvents();

                // Add this pipeline to history
                RecordPipelineStartTime();

                // Add automatic transcription, but don't transcribe nested commands
                if (this.AddToHistory || !IsNested)
                {
                    bool needToAddOutDefault = true;
                    CommandInfo outDefaultCommandInfo = new CmdletInfo("Out-Default", typeof(Microsoft.PowerShell.Commands.OutDefaultCommand), null, null, null);

                    foreach (Command command in this.Commands)
                    {
                        if (command.IsScript && (!this.IsPulsePipeline))
                        {
                            // Transcribe scripts, unless they are the pulse pipeline.
                            this.Runspace.GetExecutionContext.EngineHostInterface.UI.TranscribeCommand(command.CommandText, null);
                        }

                        // Don't need to add Out-Default if the pipeline already has it, or we've got a pipeline evaluating
                        // the PSConsoleHostReadLine command.
                        if (
                            string.Equals(outDefaultCommandInfo.Name, command.CommandText, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals("PSConsoleHostReadLine", command.CommandText, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals("TabExpansion2", command.CommandText, StringComparison.OrdinalIgnoreCase) ||
                            this.IsPulsePipeline)
                        {
                            needToAddOutDefault = false;
                        }
                    }

                    if (this.Runspace.GetExecutionContext.EngineHostInterface.UI.IsTranscribing)
                    {
                        if (needToAddOutDefault)
                        {
                            Command outDefaultCommand = new Command(outDefaultCommandInfo);
                            outDefaultCommand.Parameters.Add(new CommandParameter("Transcript", true));
                            outDefaultCommand.Parameters.Add(new CommandParameter("OutVariable", null));

                            Commands.Add(outDefaultCommand);
                        }
                    }
                }

                try
                {
                    // Create PipelineProcessor to invoke this pipeline
                    pipelineProcessor = CreatePipelineProcessor();
                }
                catch (Exception ex)
                {
                    if (this.SetPipelineSessionState)
                    {
                        SetHadErrors(true);
                        Runspace.ExecutionContext.AppendDollarError(ex);
                    }

                    throw;
                }

                // Supply input stream to PipelineProcessor

                // NTRAID#Windows Out Of Band Releases-925566-2005/12/09-JonN
                if (_useExternalInput)
                {
                    pipelineProcessor.ExternalInput = InputStream.ObjectReader;
                }

                pipelineProcessor.ExternalSuccessOutput = OutputStream.ObjectWriter;
                pipelineProcessor.ExternalErrorOutput = ErrorStream.ObjectWriter;
                // Set Informational Buffers on the host only if this is not a child.
                // Do not overwrite parent's informational buffers.
                if (!this.IsChild)
                {
                    LocalRunspace.ExecutionContext.InternalHost.InternalUI.SetInformationalMessageBuffers(InformationalBuffers);
                }

                bool oldQuestionMarkValue = true;
                bool savedIgnoreScriptDebug = this.LocalRunspace.ExecutionContext.IgnoreScriptDebug;
                // preserve the trap behaviour state variable...
                bool oldTrapState = this.LocalRunspace.ExecutionContext.PropagateExceptionsToEnclosingStatementBlock;
                this.LocalRunspace.ExecutionContext.PropagateExceptionsToEnclosingStatementBlock = false;

                try
                {
                    // Add this pipeline to stopper
                    _stopper.Push(pipelineProcessor);

                    // Preserve the last value of $? across non-interactive commands.
                    if (!AddToHistory)
                    {
                        oldQuestionMarkValue = this.LocalRunspace.ExecutionContext.QuestionMarkVariableValue;
                        this.LocalRunspace.ExecutionContext.IgnoreScriptDebug = true;
                    }
                    else
                    {
                        this.LocalRunspace.ExecutionContext.IgnoreScriptDebug = false;
                    }

                    // Reset the redirection only if the pipeline is neither nested nor is a pulse pipeline (created by EventManager)
                    if (!this.IsNested && !this.IsPulsePipeline)
                    {
                        this.LocalRunspace.ExecutionContext.ResetRedirection();
                    }

                    // Invoke the pipeline.
                    // Note:Since we are using pipes for output, return array is
                    // be empty.
                    try
                    {
                        pipelineProcessor.SynchronousExecuteEnumerate(AutomationNull.Value);
                        SetHadErrors(pipelineProcessor.ExecutionFailed);
                    }
                    catch (ExitException ee)
                    {
                        // The 'exit' command was run so tell the host to exit.
                        // Use the finally clause to make sure that the call is actually made.
                        // We'll default the exit code to 1 instead or zero so that if, for some
                        // reason, we can't get the real error code, we'll indicate a failure.
                        SetHadErrors(pipelineProcessor.ExecutionFailed);
                        int exitCode = 1;
                        if (IsNested)
                        {
                            // set the global LASTEXITCODE to the value passed by exit <code>
                            try
                            {
                                exitCode = (int)ee.Argument;
                                this.LocalRunspace.ExecutionContext.SetVariable(SpecialVariables.LastExitCodeVarPath, exitCode);
                            }
                            finally
                            {
                                try
                                {
                                    this.LocalRunspace.ExecutionContext.EngineHostInterface.ExitNestedPrompt();
                                }
                                catch (ExitNestedPromptException)
                                {
                                    // Already at the top level so we just want to ignore this exception...
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                exitCode = (int)ee.Argument;

                                if ((InvocationSettings != null) && (InvocationSettings.ExposeFlowControlExceptions))
                                {
                                    flowControlException = ee;
                                }
                            }
                            finally
                            {
                                this.LocalRunspace.ExecutionContext.EngineHostInterface.SetShouldExit(exitCode);
                            }
                        }
                    }
                    catch (ExitNestedPromptException)
                    {
                    }
                    catch (FlowControlException e)
                    {
                        if ((InvocationSettings != null) && (InvocationSettings.ExposeFlowControlExceptions) &&
                            ((e is BreakException) || (e is ContinueException) || (e is TerminateException)))
                        {
                            // Save FlowControl exception for return to caller.
                            flowControlException = e;
                        }

                        // Otherwise discard this type of exception generated by the debugger or from an unhandled break, continue or return.
                    }
                    catch (Exception)
                    {
                        // Indicate that there were errors then rethrow...
                        SetHadErrors(true);
                        throw;
                    }
                }
                finally
                {
                    // Call StopProcessing() for all the commands.
                    if (pipelineProcessor != null && pipelineProcessor.Commands != null)
                    {
                        for (int i = 0; i < pipelineProcessor.Commands.Count; i++)
                        {
                            CommandProcessorBase commandProcessor = pipelineProcessor.Commands[i];

                            EtwActivity.SetActivityId(commandProcessor.PipelineActivityId);

                            // Log a command terminated event

                            MshLog.LogCommandLifecycleEvent(
                                commandProcessor.Context,
                                CommandState.Terminated,
                                commandProcessor.Command.MyInvocation);
                        }
                    }

                    PSLocalEventManager eventManager = LocalRunspace.Events as PSLocalEventManager;
                    if (eventManager != null)
                    {
                        eventManager.ProcessPendingActions();
                    }

                    // restore the trap state...
                    this.LocalRunspace.ExecutionContext.PropagateExceptionsToEnclosingStatementBlock = oldTrapState;
                    // clean the buffers on InternalHost only if this is not a child.
                    // Do not clear parent's informational buffers.
                    if (!IsChild)
                        LocalRunspace.ExecutionContext.InternalHost.InternalUI.SetInformationalMessageBuffers(null);

                    // Pop the pipeline processor from stopper.
                    _stopper.Pop(false);

                    if (!AddToHistory)
                    {
                        this.LocalRunspace.ExecutionContext.QuestionMarkVariableValue = oldQuestionMarkValue;
                    }

                    // Restore the IgnoreScriptDebug value.
                    this.LocalRunspace.ExecutionContext.IgnoreScriptDebug = savedIgnoreScriptDebug;
                }
            }
            catch (FlowControlException)
            {
                // Discard this type of exception generated by the debugger or from an unhandled break, continue or return.
            }
            finally
            {
                // 2004/02/26-JonN added IDisposable to PipelineProcessor
                if (pipelineProcessor != null)
                {
                    pipelineProcessor.Dispose();
                    pipelineProcessor = null;
                }
            }

            return flowControlException;
        }

#if !UNIX
        /// <summary>
        /// Invokes the InvokeThreadProc() method on new thread, and flows calling thread
        /// impersonation as needed.
        /// </summary>
        private void InvokeThreadProcImpersonate()
        {
            if (_identityToImpersonate != null)
            {
                WindowsIdentity.RunImpersonated(
                    _identityToImpersonate.AccessToken,
                    () => InvokeThreadProc());

                return;
            }

            InvokeThreadProc();
        }
#endif

        /// <summary>
        /// Start thread method for asynchronous pipeline execution.
        /// </summary>
        private void InvokeThreadProc()
        {
            bool incompleteParseException = false;
            Runspace previousDefaultRunspace = Runspace.DefaultRunspace;

            try
            {
                // Set up pipeline internal host if it is available.
                if (InvocationSettings != null && InvocationSettings.Host != null)
                {
                    InternalHost internalHost = InvocationSettings.Host as InternalHost;

                    if (internalHost != null) // if we are given an internal host, use the external host
                    {
                        LocalRunspace.ExecutionContext.InternalHost.SetHostRef(internalHost.ExternalHost);
                    }
                    else
                    {
                        LocalRunspace.ExecutionContext.InternalHost.SetHostRef(InvocationSettings.Host);
                    }
                }

                if (LocalRunspace.ExecutionContext.InternalHost.ExternalHost.ShouldSetThreadUILanguageToZero)
                {
                    //  BUG: 610329. Pipeline execution happens in a new thread. For
                    //  Console applications SetThreadUILanguage(0) must be called
                    //  inorder for the native MUI loader to load the resources correctly.
                    //  ConsoleHost already does this in its entry point..but the same
                    //  call is not performed in the Pipeline execution threads causing
                    //  cmdlets that load native resources show unreadable messages on
                    //  the console.
                    Microsoft.PowerShell.NativeCultureResolver.SetThreadUILanguage(0);
                }

                // Put Execution Context In TLS
                Runspace.DefaultRunspace = this.LocalRunspace;

                FlowControlException flowControlException = InvokeHelper();

                if (flowControlException != null)
                {
                    // Let pipeline propagate the BreakException.
                    SetPipelineState(Runspaces.PipelineState.Failed, flowControlException);
                }
                else
                {
                    // Invoke finished successfully. Set state to Completed.
                    SetPipelineState(PipelineState.Completed);
                }
            }
            catch (PipelineStoppedException ex)
            {
                SetPipelineState(PipelineState.Stopped, ex);
            }
            catch (RuntimeException ex)
            {
                incompleteParseException = ex is IncompleteParseException;
                SetPipelineState(PipelineState.Failed, ex);
                SetHadErrors(true);
            }
            catch (ScriptCallDepthException ex)
            {
                SetPipelineState(PipelineState.Failed, ex);
                SetHadErrors(true);
            }
            catch (System.Security.SecurityException ex)
            {
                SetPipelineState(PipelineState.Failed, ex);
                SetHadErrors(true);
            }
#if !CORECLR // No ThreadAbortException In CoreCLR
            catch (ThreadAbortException ex)
            {
                SetPipelineState(PipelineState.Failed, ex);
                SetHadErrors(true);
            }
#endif
            // 1021203-2005/05/09-JonN
            // HaltCommandException will cause the command
            // to stop, but not be reported as an error.
            catch (HaltCommandException)
            {
                SetPipelineState(PipelineState.Completed);
            }
            finally
            {
                // Remove pipeline specific host if it was set.
                // Win8:464422 Revert the host only if this pipeline invocation changed it
                // with 464422 a nested pipeline reverts the host, although the nested pipeline did not set it.
                if ((InvocationSettings != null && InvocationSettings.Host != null) &&
                    (LocalRunspace.ExecutionContext.InternalHost.IsHostRefSet))
                {
                    LocalRunspace.ExecutionContext.InternalHost.RevertHostRef();
                }

                // Remove Execution Context From TLS
                Runspace.DefaultRunspace = previousDefaultRunspace;

                // If incomplete parse exception is hit, we should not add to history.
                // This is ensure that in case of multiline commands, command is in the
                // history only once.
                if (!incompleteParseException)
                {
                    try
                    {
                        // do not update the history if we are in the debugger and the history is locked, since that may go into a deadlock
                        bool skipIfLocked = LocalRunspace.ExecutionContext.Debugger.InBreakpoint;

                        if (_historyIdForThisPipeline == -1)
                        {
                            AddHistoryEntry(skipIfLocked);
                        }
                        else
                        {
                            UpdateHistoryEntryAddedByAddHistoryCmdlet(skipIfLocked);
                        }
                    }
                    // Updating the history may trigger variable breakpoints; the debugger may throw a TerminateException to
                    // indicate that the user wants to interrupt the variable access.
                    catch (TerminateException)
                    {
                    }
                }

                // IsChild makes it possible for LocalPipeline to differentiate
                // between a true v1 nested pipeline and the "Cmdlets Calling Cmdlets" case.

                // Close the output stream if it is not closed.
                if (OutputStream.IsOpen && !IsChild)
                {
                    try
                    {
                        OutputStream.Close();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }

                // Close the error stream if it is not closed.
                if (ErrorStream.IsOpen && !IsChild)
                {
                    try
                    {
                        ErrorStream.Close();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }

                // Close the input stream if it is not closed.
                if (InputStream.IsOpen && !IsChild)
                {
                    try
                    {
                        InputStream.Close();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }

                // Clear stream links from ExecutionContext
                ClearStreams();

                // Runspace object maintains a list of pipelines in execution.
                // Remove this pipeline from the list. This method also calls the
                // pipeline finished event.
                LocalRunspace.RemoveFromRunningPipelineList(this);

                // If async call raise the event here. For sync invoke call,
                // thread on which invoke is called will raise the event.
                if (!SyncInvokeCall)
                {
                    // This should be called after signaling PipelineFinishedEvent and
                    // RemoveFromRunningPipelineList. If it is done before, and in the
                    // Event, Runspace.Close is called which waits for pipeline to close.
                    // We will have deadlock
                    RaisePipelineStateEvents();
                }
            }
        }

        #region stop

        /// <summary>
        /// Stop the running pipeline.
        /// </summary>
        /// <param name="syncCall">If true pipeline is stoped synchronously
        /// else asynchronously.</param>
        protected override void ImplementStop(bool syncCall)
        {
            if (syncCall)
            {
                StopHelper();
            }
            else
            {
                Thread stopThread = new Thread(new ThreadStart(this.StopThreadProc));
                stopThread.Start();
            }
        }

        /// <summary>
        /// Start method for asynchronous Stop.
        /// </summary>
        private void StopThreadProc()
        {
            StopHelper();
        }

        private PipelineStopper _stopper;

        /// <summary>
        /// Gets PipelineStopper object which maintains stack of PipelineProcessor
        /// for this pipeline.
        /// </summary>
        /// <value></value>
        internal PipelineStopper Stopper
        {
            get
            {
                return _stopper;
            }
        }
        /// <summary>
        /// Helper method for Stop functionality.
        /// </summary>
        private void StopHelper()
        {
            // Ensure that any saved debugger stop is released
            LocalRunspace.ReleaseDebugger();

            // first stop all child pipelines of this pipeline
            LocalRunspace.StopNestedPipelines(this);

            // close the input pipe if it hasn't been closed.
            // This would release the pipeline thread if it is
            // waiting for input.
            if (InputStream.IsOpen)
            {
                try
                {
                    InputStream.Close();
                }
                catch (ObjectDisposedException)
                {
                }
            }

            _stopper.Stop();
            // Wait for pipeline to finish
            PipelineFinishedEvent.WaitOne();
        }

        /// <summary>
        /// Returns true if pipeline is stopping.
        /// </summary>
        /// <value></value>
        internal bool IsStopping
        {
            get
            {
                return _stopper.IsStopping;
            }
        }
        #endregion stop

        /// <summary>
        /// Creates a PipelineProcessor object from LocalPipeline object.
        /// </summary>
        /// <returns>Created PipelineProcessor object.</returns>
        private PipelineProcessor CreatePipelineProcessor()
        {
            CommandCollection commands = Commands;

            if (commands == null || commands.Count == 0)
            {
                throw PSTraceSource.NewInvalidOperationException(RunspaceStrings.NoCommandInPipeline);
            }

            PipelineProcessor pipelineProcessor = new PipelineProcessor();
            pipelineProcessor.TopLevel = true;

            bool failed = false;

            try
            {
                foreach (Command command in commands)
                {
                    CommandProcessorBase commandProcessorBase;
                    // If CommandInfo is null, proceed with CommandDiscovery to resolve the command name
                    if (command.CommandInfo == null)
                    {
                        try
                        {
                            CommandOrigin commandOrigin = command.CommandOrigin;

                            // Do not set command origin to internal if this is a script debugger originated command (which always
                            // runs nested commands).  This prevents the script debugger command line from seeing private commands.
                            if (IsNested &&
                                !LocalRunspace.InNestedPrompt &&
                                !((LocalRunspace.Debugger != null) && (LocalRunspace.Debugger.InBreakpoint)))
                            {
                                commandOrigin = CommandOrigin.Internal;
                            }

                            commandProcessorBase =
                                command.CreateCommandProcessor
                                    (
                                        LocalRunspace.ExecutionContext,
                                        AddToHistory,
                                        commandOrigin
                                    );
                        }
                        catch
                        {
                            // If we had an error creating a command processor and we are logging, then
                            // log the attempted command invocation anyways.
                            if (this.Runspace.GetExecutionContext.EngineHostInterface.UI.IsTranscribing)
                            {
                                // Don't need to log script commands, as they were already logged during pipeline
                                // setup
                                if (!command.IsScript)
                                {
                                    this.Runspace.ExecutionContext.InternalHost.UI.TranscribeCommand(command.CommandText, null);
                                }
                            }

                            throw;
                        }
                    }
                    else
                    {
                        commandProcessorBase = CreateCommandProcessBase(command);
                        // Set the internal command origin member on the command object at this point...
                        commandProcessorBase.Command.CommandOriginInternal = CommandOrigin.Internal;
                        commandProcessorBase.Command.MyInvocation.InvocationName = command.CommandInfo.Name;
                        if (command.Parameters != null)
                        {
                            foreach (CommandParameter publicParameter in command.Parameters)
                            {
                                CommandParameterInternal internalParameter = CommandParameter.ToCommandParameterInternal(publicParameter, false);
                                commandProcessorBase.AddParameter(internalParameter);
                            }
                        }
                    }

                    commandProcessorBase.RedirectShellErrorOutputPipe = this.RedirectShellErrorOutputPipe;
                    pipelineProcessor.Add(commandProcessorBase);
                }

                return pipelineProcessor;
            }
            catch (RuntimeException)
            {
                failed = true;
                throw;
            }
            catch (Exception e)
            {
                failed = true;
                throw new RuntimeException(PipelineStrings.CannotCreatePipeline, e);
            }
            finally
            {
                if (failed)
                {
                    this.SetHadErrors(true);

                    // 2004/02/26-JonN added IDisposable to PipelineProcessor
                    pipelineProcessor.Dispose();
                }
            }
        }

        /// <summary>
        /// Resolves command.CommandInfo to an appropriate CommandProcessorBase implementation.
        /// </summary>
        /// <param name="command">Command to resolve.</param>
        /// <returns></returns>
        private CommandProcessorBase CreateCommandProcessBase(Command command)
        {
            CommandInfo commandInfo = command.CommandInfo;
            while (commandInfo is AliasInfo)
            {
                commandInfo = ((AliasInfo)commandInfo).ReferencedCommand;
            }

            CmdletInfo cmdletInfo = commandInfo as CmdletInfo;
            if (cmdletInfo != null)
            {
                return new CommandProcessor(cmdletInfo, LocalRunspace.ExecutionContext);
            }

            IScriptCommandInfo functionInfo = commandInfo as IScriptCommandInfo;
            if (functionInfo != null)
            {
                return new CommandProcessor(functionInfo, LocalRunspace.ExecutionContext,
                    useLocalScope: false, fromScriptFile: false, sessionState: LocalRunspace.ExecutionContext.EngineSessionState);
            }

            ApplicationInfo applicationInfo = commandInfo as ApplicationInfo;
            if (applicationInfo != null)
            {
                return new NativeCommandProcessor(applicationInfo, LocalRunspace.ExecutionContext);
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// This method initializes streams and backs up their original states.
        /// This should be only called from constructors.
        /// </summary>
        private void InitStreams()
        {
            if (LocalRunspace.ExecutionContext != null)
            {
                _oldExternalErrorOutput = LocalRunspace.ExecutionContext.ExternalErrorOutput;
                _oldExternalSuccessOutput = LocalRunspace.ExecutionContext.ExternalSuccessOutput;
                LocalRunspace.ExecutionContext.ExternalErrorOutput = ErrorStream.ObjectWriter;
                LocalRunspace.ExecutionContext.ExternalSuccessOutput = OutputStream.ObjectWriter;
            }
        }

        /// <summary>
        /// This method sets streams to their orignal states from execution context.
        /// This is done when Pipeline is completed/failed/stopped ie., termination state.
        /// </summary>
        private void ClearStreams()
        {
            if (LocalRunspace.ExecutionContext != null)
            {
                LocalRunspace.ExecutionContext.ExternalErrorOutput = _oldExternalErrorOutput;
                LocalRunspace.ExecutionContext.ExternalSuccessOutput = _oldExternalSuccessOutput;
            }
        }

        // History object for this pipeline
        private DateTime _pipelineStartTime;

        /// <summary>
        /// Adds an entry in history for this pipeline.
        /// </summary>
        private void RecordPipelineStartTime()
        {
            _pipelineStartTime = DateTime.Now;
        }

        /// <summary>
        /// Add HistoryEntry for this pipeline. Use this function when writing
        /// history at the end of pipeline.
        /// </summary>
        private void AddHistoryEntry(bool skipIfLocked)
        {
            // History id is greater than zero if entry was added to history
            if (AddToHistory)
            {
                LocalRunspace.History.AddEntry(InstanceId, HistoryString, PipelineState, _pipelineStartTime, DateTime.Now, skipIfLocked);
            }
        }

        private long _historyIdForThisPipeline = -1;
        /// <summary>
        /// This method is called Add-History cmdlet to add history entry.
        /// </summary>
        /// <remarks>
        /// In general history entry for current pipeline is added at the
        /// end of pipeline execution.
        /// However when add-history cmdlet is executed, history entry
        /// needs to be added before add-history adds additional entries
        /// in to history.
        /// </remarks>
        internal
        void AddHistoryEntryFromAddHistoryCmdlet()
        {
            // This method can be called by multiple times during a single
            // pipeline execution. For ex: a script can execute add-history
            // command multiple times. However we should add entry only
            // once.
            if (_historyIdForThisPipeline != -1)
            {
                return;
            }

            if (AddToHistory)
            {
                _historyIdForThisPipeline = LocalRunspace.History.AddEntry(InstanceId, HistoryString, PipelineState, _pipelineStartTime, DateTime.Now, false);
            }
        }

        /// <summary>
        /// Add-history cmdlet adds history entry for the pipeline in its
        /// begin processing. This method is called to update the end execution
        /// time and status of pipeline.
        /// </summary>
        internal
        void UpdateHistoryEntryAddedByAddHistoryCmdlet(bool skipIfLocked)
        {
            if (AddToHistory && _historyIdForThisPipeline != -1)
            {
                LocalRunspace.History.UpdateEntry(_historyIdForThisPipeline, PipelineState, DateTime.Now, skipIfLocked);
            }
        }

        /// <summary>
        /// Sets the history string to the specified one.
        /// </summary>
        /// <param name="historyString">History string to set to.</param>
        internal override void SetHistoryString(string historyString)
        {
            HistoryString = historyString;
        }

        #region TLS

        /// <summary>
        /// Gets the execution context in the thread local storage of current
        /// thread.
        /// </summary>
        /// <returns>
        /// ExecutionContext, if it available in TLS
        /// Null, if ExecutionContext is not available in TLS
        /// </returns>
        internal static System.Management.Automation.ExecutionContext GetExecutionContextFromTLS()
        {
            System.Management.Automation.Runspaces.Runspace runspace = Runspace.DefaultRunspace;
            if (runspace == null)
            {
                return null;
            }

            return runspace.ExecutionContext;
        }

        #endregion TLS

        #endregion private_methods

        #region private_fields

        /// <summary>
        /// Holds reference to LocalRunspace to which this pipeline is
        /// associated with.
        /// </summary>
        private LocalRunspace LocalRunspace
        {
            get
            {
                return (LocalRunspace)Runspace;
            }
        }

        private bool _useExternalInput;

        private PipelineWriter _oldExternalErrorOutput;
        private PipelineWriter _oldExternalSuccessOutput;

#if !UNIX
        private WindowsIdentity _identityToImpersonate;
#endif

        #endregion private_fields

        #region invoke_loop_detection

        /// <summary>
        /// This is list of HistoryInfo ids which have been executed in
        /// this pipeline.
        /// </summary>
        private List<long> _invokeHistoryIds = new List<long>();

        internal bool PresentInInvokeHistoryEntryList(HistoryInfo entry)
        {
            return _invokeHistoryIds.Contains(entry.Id);
        }

        internal void AddToInvokeHistoryEntryList(HistoryInfo entry)
        {
            _invokeHistoryIds.Add(entry.Id);
        }

        internal void RemoveFromInvokeHistoryEntryList(HistoryInfo entry)
        {
            _invokeHistoryIds.Remove(entry.Id);
        }

        #endregion invoke_loop_detection

        #region IDisposable Members

        /// <summary>
        /// Set to true when object is disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Protected dispose which can be overridden by derived classes.
        /// </summary>
        /// <param name="disposing"></param>
        protected override
        void
        Dispose(bool disposing)
        {
            try
            {
                if (_disposed == false)
                {
                    _disposed = true;
                    if (disposing)
                    {
                        Stop();
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        #endregion IDisposable Members
    }

    /// <summary>
    /// Helper class that holds the thread used to execute pipelines when CreateThreadOptions.ReuseThread is used.
    /// </summary>
    internal class PipelineThread : IDisposable
    {
        /// <summary>
        /// Creates the worker thread and waits for it to be ready.
        /// </summary>
#if CORECLR
        internal PipelineThread()
        {
            _worker = new Thread(WorkerProc, LocalPipeline.DefaultPipelineStackSize);
            _workItem = null;
            _workItemReady = new AutoResetEvent(false);
            _closed = false;
        }
#else
        internal PipelineThread(ApartmentState apartmentState)
        {
            _worker = new Thread(WorkerProc, LocalPipeline.DefaultPipelineStackSize);
            _workItem = null;
            _workItemReady = new AutoResetEvent(false);
            _closed = false;

            if (apartmentState != ApartmentState.Unknown)
            {
                _worker.SetApartmentState(apartmentState);
            }
        }
#endif

        /// <summary>
        /// Returns the worker thread.
        /// </summary>
        internal Thread Worker
        {
            get
            {
                return _worker;
            }
        }

        /// <summary>
        /// Posts an item to the worker thread and wait for its completion.
        /// </summary>
        internal void Start(ThreadStart workItem)
        {
            if (_closed)
            {
                return;
            }

            _workItem = workItem;
            _workItemReady.Set();

            if (_worker.ThreadState == System.Threading.ThreadState.Unstarted)
            {
                _worker.Start();
            }
        }

        /// <summary>
        /// Shortcut for dispose.
        /// </summary>
        internal void Close()
        {
            Dispose();
        }

        /// <summary>
        /// Implementation of the worker thread.
        /// </summary>
        private void WorkerProc()
        {
            while (!_closed)
            {
                _workItemReady.WaitOne();

                if (!_closed)
                {
                    _workItem();
                }
            }
        }

        /// <summary>
        /// Releases the worker thread.
        /// </summary>
        public void Dispose()
        {
            if (_closed)
            {
                return;
            }

            _closed = true;

            _workItemReady.Set();

            if (_worker.ThreadState != System.Threading.ThreadState.Unstarted && Thread.CurrentThread != _worker)
            {
                _worker.Join();
            }

            _workItemReady.Dispose();

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Ensure we release the worker thread.
        /// </summary>
        ~PipelineThread()
        {
            Dispose();
        }

        private Thread _worker;
        private ThreadStart _workItem;
        private AutoResetEvent _workItemReady;
        private bool _closed;
    }

    /// <summary>
    /// This is helper class for stopping a running pipeline. This
    /// class maintains a stack of currently active pipeline processors.
    /// To stop a pipeline, stop is called on each pipeline processor
    /// in the stack.
    /// </summary>
    internal class PipelineStopper
    {
        /// <summary>
        /// Stack of current executing pipeline processor.
        /// </summary>
        private Stack<PipelineProcessor> _stack = new Stack<PipelineProcessor>();

        /// <summary>
        /// Object used for synchronization.
        /// </summary>
        private object _syncRoot = new object();
        private LocalPipeline _localPipeline;

        /// <summary>
        /// Default constructor.
        /// </summary>
        internal PipelineStopper(LocalPipeline localPipeline)
        {
            _localPipeline = localPipeline;
        }

        /// <summary>
        /// This is set true when stop is called.
        /// </summary>
        private bool _stopping;
        internal bool IsStopping
        {
            get
            {
                return _stopping;
            }

            set
            {
                _stopping = value;
            }
        }

        /// <summary>
        /// Push item in to PipelineProcessor stack.
        /// </summary>
        /// <param name="item"></param>
        internal void Push(PipelineProcessor item)
        {
            if (item == null)
            {
                throw PSTraceSource.NewArgumentNullException("item");
            }

            lock (_syncRoot)
            {
                if (_stopping)
                {
                    PipelineStoppedException e = new PipelineStoppedException();
                    throw e;
                }

                _stack.Push(item);
            }

            item.LocalPipeline = _localPipeline;
        }

        /// <summary>
        /// Pop top item from PipelineProcessor stack.
        /// </summary>
        internal void Pop(bool fromSteppablePipeline)
        {
            lock (_syncRoot)
            {
                // If we are stopping, Stop will pop the entire stack, so
                // we shouldn't do any popping or some PipelineProcessor won't
                // be notified that it is being stopped.
                if (_stopping)
                {
                    return;
                }

                if (_stack.Count > 0)
                {
                    PipelineProcessor oldPipe = _stack.Pop();
                    if (fromSteppablePipeline && oldPipe.ExecutionFailed && _stack.Count > 0)
                    {
                        _stack.Peek().ExecutionFailed = true;
                    }
                    // If this is the last pipeline processor on the stack, then propagate it's execution status
                    if (_stack.Count == 1 && _localPipeline != null)
                    {
                        _localPipeline.SetHadErrors(oldPipe.ExecutionFailed);
                    }
                }
            }
        }

        internal void Stop()
        {
            PipelineProcessor[] copyStack;
            lock (_syncRoot)
            {
                if (_stopping == true)
                {
                    return;
                }

                _stopping = true;

                copyStack = _stack.ToArray();
            }

            // Propagate error from the toplevel operation through to enclosing the LocalPipeline.
            if (copyStack.Length > 0)
            {
                PipelineProcessor topLevel = copyStack[copyStack.Length - 1];
                if (topLevel != null && _localPipeline != null)
                {
                    _localPipeline.SetHadErrors(topLevel.ExecutionFailed);
                }
            }

            // Note: after _stopping is set to true, nothing can be pushed/popped
            // from stack and it is safe to call stop on PipelineProcessors in stack
            // outside the lock
            // Note: you want to do below loop outside the lock so that
            // pipeline execution thread doesn't get blocked on Push and Pop.
            // Note: A copy of the stack is made because we "unstop" a stopped
            // pipeline to execute finally blocks.  We don't want to stop pipelines
            // in the finally block though.
            foreach (PipelineProcessor pp in copyStack)
            {
                pp.Stop();
            }
        }
    }
}

