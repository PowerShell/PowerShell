// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Tracing;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.PowerShell.Telemetry;

using Dbg = System.Management.Automation.Diagnostics;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings

namespace System.Management.Automation.Internal
{
    /// <summary>
    /// Loads InternalCommand objects and executes them.
    /// </summary>
    /// <remarks>
    /// The PipelineProcessor class is not thread-safe, so methods such as
    /// AddCommand and SynchronousExecute should not be called
    /// simultaneously.  While SynchronousExecute is running, it may access
    /// ExternalInput, ExternalSuccessOutput and ExternalErrorOutput, and
    /// those objects are thread-safe.
    /// </remarks>
    internal class PipelineProcessor : IDisposable
    {
        #region private_members

        private List<CommandProcessorBase> _commands = new List<CommandProcessorBase>();
        private List<PipelineProcessor> _redirectionPipes;
        private PipelineReader<object> _externalInputPipe;
        private PipelineWriter _externalSuccessOutput;
        private PipelineWriter _externalErrorOutput;
        private bool _executionStarted = false;
        private bool _stopping = false;
        private SessionStateScope _executionScope;

        private ExceptionDispatchInfo _firstTerminatingError = null;

        private bool _linkedSuccessOutput = false;
        private bool _linkedErrorOutput = false;

        private NativeCommandProcessor _lastNativeCommand;

        private bool _haveReportedNativePipeUsage;

#if !CORECLR // Impersonation Not Supported On CSS
        // This is the security context when the pipeline was allocated
        internal System.Security.SecurityContext SecurityContext =
            System.Security.SecurityContext.Capture();
#endif
        #endregion private_members

        #region IDispose

        private bool _disposed = false;

        /// <summary>
        /// When the command is complete, PipelineProcessor will be
        /// disposed.
        /// </summary>
        /// <remarks>
        /// This is only public because it implements an interface method.
        /// The class itself is internal.
        /// We use the standard IDispose pattern.
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                DisposeCommands();
                _localPipeline = null;
                _externalSuccessOutput = null;
                _externalErrorOutput = null;
                _executionScope = null;
                _eventLogBuffer = null;
#if !CORECLR // Impersonation Not Supported On CSS
                SecurityContext.Dispose();
                SecurityContext = null;
#endif
            }

            _disposed = true;
        }

        #endregion IDispose

        #region Execution Logging

        private bool _executionFailed = false;

        internal List<CommandProcessorBase> Commands
        {
            get { return _commands; }
        }

        internal bool ExecutionFailed
        {
            get
            {
                return _executionFailed;
            }

            set
            {
                _executionFailed = value;
            }
        }

        internal void LogExecutionInfo(InvocationInfo invocationInfo, string text)
        {
            string message = StringUtil.Format(PipelineStrings.PipelineExecutionInformation, GetCommand(invocationInfo), text);
            Log(message, invocationInfo, PipelineExecutionStatus.Started);
        }

        internal void LogExecutionComplete(InvocationInfo invocationInfo, string text)
        {
            string message = StringUtil.Format(PipelineStrings.PipelineExecutionInformation, GetCommand(invocationInfo), text);
            Log(message, invocationInfo, PipelineExecutionStatus.Complete);
        }

        internal void LogPipelineComplete()
        {
            Log(null, null, PipelineExecutionStatus.PipelineComplete);
        }

        internal void LogExecutionParameterBinding(InvocationInfo invocationInfo, string parameterName, string parameterValue)
        {
            string message = StringUtil.Format(PipelineStrings.PipelineExecutionParameterBinding, GetCommand(invocationInfo), parameterName, parameterValue);
            Log(message, invocationInfo, PipelineExecutionStatus.ParameterBinding);
        }

        internal void LogExecutionError(InvocationInfo invocationInfo, ErrorRecord errorRecord)
        {
            if (errorRecord == null)
                return;

            string message = StringUtil.Format(PipelineStrings.PipelineExecutionNonTerminatingError, GetCommand(invocationInfo), errorRecord.ToString());
            Log(message, invocationInfo, PipelineExecutionStatus.Error);
        }

        private bool _terminatingErrorLogged = false;

        internal void LogExecutionException(Exception exception)
        {
            _executionFailed = true;

            // Only log one terminating error for pipeline execution.
            if (_terminatingErrorLogged)
                return;

            _terminatingErrorLogged = true;

            if (exception == null)
                return;

            string message = StringUtil.Format(PipelineStrings.PipelineExecutionTerminatingError, GetCommand(exception), exception.Message);
            Log(message, null, PipelineExecutionStatus.Error);
        }

        private static string GetCommand(InvocationInfo invocationInfo)
        {
            if (invocationInfo == null)
                return string.Empty;

            if (invocationInfo.MyCommand != null)
            {
                return invocationInfo.MyCommand.Name;
            }

            return string.Empty;
        }

        private static string GetCommand(Exception exception)
        {
            IContainsErrorRecord icer = exception as IContainsErrorRecord;
            if (icer != null && icer.ErrorRecord != null)
                return GetCommand(icer.ErrorRecord.InvocationInfo);

            return string.Empty;
        }

        private void Log(string logElement, InvocationInfo invocation, PipelineExecutionStatus pipelineExecutionStatus)
        {
            System.Management.Automation.Host.PSHostUserInterface hostInterface = null;
            if (this.LocalPipeline != null)
            {
                hostInterface = this.LocalPipeline.Runspace.GetExecutionContext.EngineHostInterface.UI;
            }

            // Acknowledge command completion
            if (hostInterface != null)
            {
                if (pipelineExecutionStatus == PipelineExecutionStatus.Complete)
                {
                    hostInterface.TranscribeCommandComplete(invocation);
                    return;
                }
                else if (pipelineExecutionStatus == PipelineExecutionStatus.PipelineComplete)
                {
                    hostInterface.TranscribePipelineComplete();
                    return;
                }
            }

            // Log the cmdlet invocation execution details if we didn't have an associated script line with it.
            if ((invocation == null) || string.IsNullOrEmpty(invocation.Line))
            {
                hostInterface?.TranscribeCommand(logElement, invocation);
            }

            if (_needToLog && !string.IsNullOrEmpty(logElement))
            {
                _eventLogBuffer ??= new List<string>();
                _eventLogBuffer.Add(logElement);
            }
        }

        private void LogToEventLog()
        {
            // We check to see if there is anything in the buffer before we flush it.
            // Flushing the empty buffer causes a measurable performance degradation.
            if (_commands?.Count > 0 && _eventLogBuffer?.Count > 0)
            {
                InternalCommand firstCmd = _commands[0].Command;
                MshLog.LogPipelineExecutionDetailEvent(
                    firstCmd.Context,
                    _eventLogBuffer,
                    firstCmd.MyInvocation);
            }

            // Clear the log buffer after writing the event.
            _eventLogBuffer?.Clear();
        }

        private bool _needToLog = false;
        private List<string> _eventLogBuffer;

        #endregion

        #region public_methods

        /// <summary>
        /// Add a single InternalCommand to the end of the pipeline.
        /// </summary>
        /// <returns>Results from last pipeline stage.</returns>
        /// <exception cref="InvalidOperationException">
        /// see AddCommand
        /// </exception>
        /// <exception cref="ObjectDisposedException"></exception>
        internal int Add(CommandProcessorBase commandProcessor)
        {
            if (ExperimentalFeature.IsEnabled(ExperimentalFeature.PSNativeCommandPreserveBytePipe))
            {
                if (commandProcessor is NativeCommandProcessor nativeCommand)
                {
                    if (_lastNativeCommand is not null)
                    {
                        // Only report experimental feature usage once per pipeline.
                        if (!_haveReportedNativePipeUsage)
                        {
                            ApplicationInsightsTelemetry.SendExperimentalUseData(
                                ExperimentalFeature.PSNativeCommandPreserveBytePipe,
                                "p");
                            _haveReportedNativePipeUsage = true;
                        }

                        _lastNativeCommand.DownStreamNativeCommand = nativeCommand;
                        nativeCommand.UpstreamIsNativeCommand = true;
                    }

                    _lastNativeCommand = nativeCommand;
                }
                else
                {
                    _lastNativeCommand = null;
                }
            }

            commandProcessor.CommandRuntime.PipelineProcessor = this;
            return AddCommand(commandProcessor, _commands.Count, readErrorQueue: false);
        }

        internal void AddRedirectionPipe(PipelineProcessor pipelineProcessor)
        {
            if (pipelineProcessor is null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(pipelineProcessor));
            }

            _redirectionPipes ??= new List<PipelineProcessor>();
            _redirectionPipes.Add(pipelineProcessor);
        }

        // 2004/02/28-JSnover (from spec review) ReadFromErrorQueue
        //   should be an int or enum to allow for more queues
        // 2005/03/08-JonN: This is an internal API
        /// <summary>
        /// Add a command to the pipeline.
        /// </summary>
        /// <param name="commandProcessor"></param>
        /// <param name="readFromCommand">Reference number of command from which to read, 0 for none.</param>
        /// <param name="readErrorQueue">Read from error queue of command readFromCommand.</param>
        /// <returns>Reference number of this command for use in readFromCommand.</returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="ArgumentException">
        /// FirstCommandCannotHaveInput: <paramref name="readFromCommand"/> must be zero
        ///   for the first command in the pipe
        /// InvalidCommandNumber: there is no command numbered <paramref name="readFromCommand"/>
        ///   A command can only read from earlier commands; this prevents circular queues
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// ExecutionAlreadyStarted: pipeline has already started or completed
        /// PipeAlreadyTaken: the downstream pipe of command <paramref name="readFromCommand"/>
        ///   is already taken
        /// </exception>
        private int AddCommand(CommandProcessorBase commandProcessor, int readFromCommand, bool readErrorQueue)
        {
            if (commandProcessor == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(commandProcessor));
            }

            if (_commands == null)
            {
                // "_commands == null"
                throw PSTraceSource.NewInvalidOperationException();
            }

            if (_disposed)
            {
                throw PSTraceSource.NewObjectDisposedException("PipelineProcessor");
            }

            if (_executionStarted)
            {
                throw PSTraceSource.NewInvalidOperationException(
                    PipelineStrings.ExecutionAlreadyStarted);
            }

            if (commandProcessor.AddedToPipelineAlready)
            {
                throw PSTraceSource.NewInvalidOperationException(
                    PipelineStrings.CommandProcessorAlreadyUsed);
            }

            if (_commands.Count == 0)
            {
                if (readFromCommand != 0)
                {
                    // "First command cannot have input"
                    throw PSTraceSource.NewArgumentException(
                        nameof(readFromCommand),
                        PipelineStrings.FirstCommandCannotHaveInput);
                }

                commandProcessor.AddedToPipelineAlready = true;
            }
            // 2003/08/11-JonN Subsequent commands must have predecessor
            else if (readFromCommand > _commands.Count || readFromCommand <= 0)
            {
                // "invalid command number"
                throw PSTraceSource.NewArgumentException(
                    nameof(readFromCommand),
                    PipelineStrings.InvalidCommandNumber);
            }
            else
            {
                var prevcommandProcessor = _commands[readFromCommand - 1] as CommandProcessorBase;
                ValidateCommandProcessorNotNull(prevcommandProcessor, errorMessage: null);

                Pipe UpstreamPipe = (readErrorQueue)
                    ? prevcommandProcessor.CommandRuntime.ErrorOutputPipe
                    : prevcommandProcessor.CommandRuntime.OutputPipe;

                if (UpstreamPipe == null)
                {
                    throw PSTraceSource.NewInvalidOperationException();
                }

                if (UpstreamPipe.DownstreamCmdlet != null)
                {
                    throw PSTraceSource.NewInvalidOperationException(
                        PipelineStrings.PipeAlreadyTaken);
                }

                commandProcessor.AddedToPipelineAlready = true;

                commandProcessor.CommandRuntime.InputPipe = UpstreamPipe;
                UpstreamPipe.DownstreamCmdlet = commandProcessor;

                // 2004/09/14-JonN This code could be moved to SynchronousExecute
                //  if this setting needed to bind at a later time
                //  than AddCommand.
                if (commandProcessor.CommandRuntime.MergeUnclaimedPreviousErrorResults)
                {
                    for (int i = 0; i < _commands.Count; i++)
                    {
                        prevcommandProcessor = _commands[i];
                        ValidateCommandProcessorNotNull(prevcommandProcessor, errorMessage: null);

                        // check whether the error output is already claimed
                        if (prevcommandProcessor.CommandRuntime.ErrorOutputPipe.DownstreamCmdlet != null)
                            continue;
                        if (prevcommandProcessor.CommandRuntime.ErrorOutputPipe.ExternalWriter != null)
                            continue;

                        // Set the upstream cmdlet's error output to go down
                        // the same pipe as the downstream cmdlet's input
                        prevcommandProcessor.CommandRuntime.ErrorOutputPipe = UpstreamPipe;
                    }
                }
            }

            _commands.Add(commandProcessor);

            // We will log event(s) about the pipeline execution details if any command in the pipeline requests that.
            _needToLog |= commandProcessor.CommandRuntime.LogPipelineExecutionDetail;

            // We give the Command a pointer back to the
            // PipelineProcessor so that it can check whether the
            // command has been stopped.
            commandProcessor.CommandRuntime.PipelineProcessor = this;

            return _commands.Count;
        }

        // 2005/03/08-JonN: This is an internal API
        /// <summary>
        /// Execute the accumulated commands and clear the pipeline.
        /// SynchronousExecute does not return until all commands have
        /// completed.  There is no asynchronous variant; instead, once the
        /// pipeline is set up, the caller can spawn a thread and call
        /// SynchronousExecute from that thread.  This does not mean that
        /// PipelineProcessor is thread-safe; once SynchronousExecute is
        /// running, PipelineProcessor should not be accessed through any
        /// other means. This variant of the routine looks at it's input
        /// object to see if it's enumerable or not.
        /// </summary>
        /// <param name="input">
        /// Input objects for first stage. If this is AutomationNull.Value, the
        /// first cmdlet is the beginning of the pipeline.
        /// </param>
        /// <returns>
        /// Results from last pipeline stage.  This will be empty if
        /// ExternalSuccessOutput is set.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// ExecutionAlreadyStarted: pipeline has already started or completed
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// PipelineExecuteRequiresAtLeastOneCommand
        /// </exception>
        /// <exception cref="CmdletInvocationException">
        /// A cmdlet encountered a terminating error
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// The pipeline was stopped asynchronously
        /// </exception>
        /// <exception cref="ActionPreferenceStopException">
        /// The ActionPreference.Stop or ActionPreference.Inquire policy
        /// triggered a terminating error.
        /// </exception>
        /// <exception cref="ParameterBindingException">
        /// If any parameters fail to bind,
        /// or
        /// If any mandatory parameters are missing.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If there is an error generating the metadata for dynamic parameters.
        /// </exception>
        /// <exception cref="ExtendedTypeSystemException">
        /// An error occurred clearing the error variable.
        /// </exception>
        /// <exception cref="HaltCommandException">
        /// HaltCommandException will cause the command
        /// to stop, but should not be reported as an error.
        /// </exception>
        internal Array SynchronousExecuteEnumerate(object input)
        {
            if (Stopping)
            {
                throw new PipelineStoppedException();
            }

            bool pipelineSucceeded = false;
            ExceptionDispatchInfo toRethrowInfo = null;
            CommandProcessorBase commandRequestingUpstreamCommandsToStop = null;

            try
            {
                try
                {
                    try
                    {
                        // If the caller specified an input object array, we run assuming there is an incoming "stream"
                        // of objects. This will prevent the one default call to ProcessRecord on the first command.
                        Start(incomingStream: input != AutomationNull.Value);

                        // Start has already validated firstcommandProcessor
                        CommandProcessorBase firstCommandProcessor = _commands[0];

                        // Add any input to the first command.
                        if (ExternalInput is not null)
                        {
                            firstCommandProcessor.CommandRuntime.InputPipe.ExternalReader = ExternalInput;
                        }

                        Inject(input, enumerate: true);
                    }
                    catch (PipelineStoppedException)
                    {
                        if (_firstTerminatingError?.SourceException is StopUpstreamCommandsException exception)
                        {
                            _firstTerminatingError = null;
                            commandRequestingUpstreamCommandsToStop = exception.RequestingCommandProcessor;
                        }
                        else
                        {
                            throw;
                        }
                    }

                    DoCompleteCore(commandRequestingUpstreamCommandsToStop);
                    pipelineSucceeded = true;
                }
                finally
                {
                    // Clean up resources for script commands, no matter the pipeline succeeded or not.
                    // This method catches and handles all exceptions inside, so it will never throw.
                    Clean();
                }

                if (pipelineSucceeded)
                {
                    // Now, we are sure all 'commandProcessors' hosted by the current 'pipelineProcessor' are done execution,
                    // so if there are any redirection 'pipelineProcessors' associated with any of those 'commandProcessors',
                    // they must have successfully executed 'StartStepping' and 'Step', and thus we should call 'DoComplete'
                    // on them for completeness.
                    if (_redirectionPipes is not null)
                    {
                        foreach (PipelineProcessor redirectPipelineProcessor in _redirectionPipes)
                        {
                            // The 'Clean' block for each 'commandProcessor' might still write to a pipe that is associated
                            // with the redirection 'pipelineProcessor' (e.g. a redirected error pipe), which would trigger
                            // the call to 'pipelineProcessor.Step'.
                            // It's possible (though very unlikely) that the call to 'pipelineProcessor.Step' failed with an
                            // exception, and in such case, the 'pipelineProcessor' would have been disposed, and therefore
                            // the call to 'DoComplete' will simply return, because '_commands' was already set to null.
                            redirectPipelineProcessor.DoCompleteCore(null);
                        }
                    }

                    // The 'Clean' blocks write nothing to the output pipe, so the results won't be affected by them.
                    return RetrieveResults();
                }
            }
            catch (RuntimeException e)
            {
                toRethrowInfo = GetFirstError(e);
            }
            finally
            {
                DisposeCommands();
            }

            // By rethrowing the exception outside of the handler, we allow the CLR on X64/IA64 to free from
            // the stack the exception records related to this exception.

            // The only reason we should get here is if an exception should be rethrown.
            Diagnostics.Assert(toRethrowInfo != null, "Alternate protocol path failure");
            toRethrowInfo.Throw();

            // UNREACHABLE
            return null;
        }

        private ExceptionDispatchInfo GetFirstError(RuntimeException e)
        {
            // The error we want to report is the first terminating error which occurred during pipeline execution,
            // regardless of whether other errors occurred afterward.
            var firstError = _firstTerminatingError ?? ExceptionDispatchInfo.Capture(e);
            LogExecutionException(firstError.SourceException);
            return firstError;
        }

        private void ThrowFirstErrorIfExisting(bool logException)
        {
            if (_firstTerminatingError != null)
            {
                if (logException)
                {
                    LogExecutionException(_firstTerminatingError.SourceException);
                }

                _firstTerminatingError.Throw();
            }
        }

        private void DoCompleteCore(CommandProcessorBase commandRequestingUpstreamCommandsToStop)
        {
            if (_commands is null)
            {
                // This could happen to a redirection pipeline, either for an expression (e.g. 1 > a.txt)
                // or for a command (e.g. command > a.txt).
                // An exception may be thrown from the call to 'StartStepping' or 'Step' on the pipeline,
                // which causes the pipeline commands to be disposed.
                return;
            }

            // Call DoComplete() for all the commands, which will internally call Complete()
            MshCommandRuntime lastCommandRuntime = null;

            for (int i = 0; i < _commands.Count; i++)
            {
                CommandProcessorBase commandProcessor = _commands[i];

                if (commandProcessor is null)
                {
                    // An internal error that should not happen.
                    throw PSTraceSource.NewInvalidOperationException();
                }

                if (object.ReferenceEquals(commandRequestingUpstreamCommandsToStop, commandProcessor))
                {
                    // Do not call DoComplete/EndProcessing on the command that initiated stopping.
                    commandRequestingUpstreamCommandsToStop = null;
                    continue;
                }

                if (commandRequestingUpstreamCommandsToStop is not null)
                {
                    // Do not call DoComplete/EndProcessing on commands that were stopped upstream.
                    continue;
                }

                try
                {
                    commandProcessor.DoComplete();
                }
                catch (PipelineStoppedException)
                {
                    if (_firstTerminatingError?.SourceException is StopUpstreamCommandsException exception)
                    {
                        _firstTerminatingError = null;
                        commandRequestingUpstreamCommandsToStop = exception.RequestingCommandProcessor;
                    }
                    else
                    {
                        throw;
                    }
                }

                EtwActivity.SetActivityId(commandProcessor.PipelineActivityId);

                // Log a command stopped event
                MshLog.LogCommandLifecycleEvent(
                    commandProcessor.Command.Context,
                    CommandState.Stopped,
                    commandProcessor.Command.MyInvocation);

                // Log the execution of a command (not script chunks, as they are not commands in and of themselves).
                if (commandProcessor.CommandInfo.CommandType != CommandTypes.Script)
                {
                    LogExecutionComplete(commandProcessor.Command.MyInvocation, commandProcessor.CommandInfo.Name);
                }

                lastCommandRuntime = commandProcessor.CommandRuntime;
            }

            // Log the pipeline completion.
            if (lastCommandRuntime is not null)
            {
                // Only log the pipeline completion if this wasn't a nested pipeline, as
                // pipeline state in transcription is associated with the toplevel pipeline
                if (LocalPipeline is null || !LocalPipeline.IsNested)
                {
                    lastCommandRuntime.PipelineProcessor.LogPipelineComplete();
                }
            }

            // If a terminating error occurred, report it now.
            // This pipeline could have been stopped asynchronously, by 'Ctrl+c' manually or
            // 'PowerShell.Stop' programatically. We need to check and see if that's the case.
            // An example:
            // - 'Start-Sleep' is running in this pipeline, and 'pipelineProcessor.Stop' gets
            //   called on a different thread, which sets a 'PipelineStoppedException' object
            //   to '_firstTerminatingError' and runs 'StopProcessing' on 'Start-Sleep'.
            // - The 'StopProcessing' will cause 'Start-Sleep' to return from 'ProcessRecord'
            //   call, and thus the pipeline execution will move forward to run 'DoComplete'
            //   for the 'Start-Sleep' command and thus the code flow will reach here.
            // For this given example, we need to check '_firstTerminatingError' and throw out
            // the 'PipelineStoppedException' if the pipeline was indeed being stopped.
            ThrowFirstErrorIfExisting(logException: true);
        }

        /// <summary>
        /// Clean up resources for script commands in this pipeline processor.
        /// </summary>
        /// <remarks>
        /// Exception from a 'Clean' block is not allowed to propagate up and terminate the pipeline
        /// so that other 'Clean' blocks can run without being affected. Therefore, this method will
        /// catch and handle all exceptions inside, and it will never throw.
        /// </remarks>
        private void Clean()
        {
            if (!_executionStarted || _commands is null)
            {
                // Simply return if the pipeline execution wasn't even started, or the commands of
                // the pipeline have already been disposed.
                return;
            }

            // So far, if '_firstTerminatingError' is not null, then it must be a terminating error
            // thrown from one of 'Begin/Process/End' blocks. There can be terminating error thrown
            // from 'Clean' block as well, which needs to be handled in this method.
            // In order to capture the subsequent first terminating error thrown from 'Clean', we
            // need to forget the previous '_firstTerminatingError' value before calling 'DoClean'
            // on each command processor, so we have to save the old value here and restore later.
            ExceptionDispatchInfo oldFirstTerminatingError = _firstTerminatingError;

            // Suspend a stopping pipeline by setting 'IsStopping' to false and restore it afterwards.
            bool oldIsStopping = ExceptionHandlingOps.SuspendStoppingPipelineImpl(LocalPipeline);

            try
            {
                foreach (CommandProcessorBase commandProcessor in _commands)
                {
                    if (commandProcessor is null || !commandProcessor.HasCleanBlock)
                    {
                        continue;
                    }

                    try
                    {
                        // Forget the terminating error we saw before, so a terminating error thrown
                        // from the subsequent 'Clean' block can be recorded and handled properly.
                        _firstTerminatingError = null;
                        commandProcessor.DoCleanup();
                    }
                    catch (RuntimeException e)
                    {
                        // Retrieve and report the terminating error that was thrown in the 'Clean' block.
                        ExceptionDispatchInfo firstError = GetFirstError(e);
                        commandProcessor.ReportCleanupError(firstError.SourceException);
                    }
                    catch (Exception ex)
                    {
                        // Theoretically, only 'RuntimeException' could be thrown out, but we catch
                        // all and log them here just to be safe.
                        // Skip special flow control exceptions and log others.
                        if (ex is not FlowControlException && ex is not HaltCommandException)
                        {
                            MshLog.LogCommandHealthEvent(commandProcessor.Context, ex, Severity.Warning);
                        }
                    }
                }
            }
            finally
            {
                _firstTerminatingError = oldFirstTerminatingError;
                ExceptionHandlingOps.RestoreStoppingPipelineImpl(LocalPipeline, oldIsStopping);
            }
        }

        /// <summary>
        /// Clean up resources for the script commands of a steppable pipeline.
        /// </summary>
        /// <remarks>
        /// The way we handle 'Clean' blocks in 'StartStepping', 'Step', and 'DoComplete' makes sure that:
        ///  1. The 'Clean' blocks get to run if any exception is thrown from the pipeline execution.
        ///  2. The 'Clean' blocks get to run if the pipeline runs to the end successfully.
        /// However, this is not enough for a steppable pipeline, because the function, where the steppable
        /// pipeline gets used, may fail (think about a proxy function). And that may lead to the situation
        /// where "no exception was thrown from the steppable pipeline" but "the steppable pipeline didn't
        /// run to the end". In that case, 'Clean' won't run unless it's triggered explicitly on the steppable
        /// pipeline. This method is how we will expose this functionality to 'SteppablePipeline'.
        /// </remarks>
        internal void DoCleanup()
        {
            Clean();
            DisposeCommands();
        }

        /// <summary>
        /// Implements DoComplete as a stand-alone function for completing
        /// the execution of a steppable pipeline.
        /// </summary>
        /// <returns>The results of the execution.</returns>
        internal Array DoComplete()
        {
            if (!_executionStarted)
            {
                throw PSTraceSource.NewInvalidOperationException(
                    PipelineStrings.PipelineNotStarted);
            }

            try
            {
                if (Stopping)
                {
                    throw new PipelineStoppedException();
                }

                ExceptionDispatchInfo toRethrowInfo;
                try
                {
                    DoCompleteCore(null);
                    return RetrieveResults();
                }
                catch (RuntimeException e)
                {
                    toRethrowInfo = GetFirstError(e);
                }

                // By rethrowing the exception outside of the handler, we allow the CLR on X64/IA64 to free from the stack
                // the exception records related to this exception.

                // The only reason we should get here is an exception should be rethrown.
                Diagnostics.Assert(toRethrowInfo != null, "Alternate protocol path failure");
                toRethrowInfo.Throw();

                // UNREACHABLE
                return null;
            }
            finally
            {
                Clean();
                DisposeCommands();
            }
        }

        /// <summary>
        /// This routine starts the stepping process. It is optional to call this but can be useful
        /// if you want the begin clauses of the pipeline to be run even when there may not be any
        /// input to process as is the case for I/O redirection into a file. We still want the file
        /// opened, even if there was nothing to write to it.
        /// </summary>
        /// <param name="expectInput">True if you want to write to this pipeline.</param>
        internal void StartStepping(bool expectInput)
        {
            bool startSucceeded = false;
            try
            {
                Start(expectInput);
                startSucceeded = true;

                // Check if this pipeline is being stopped asynchronously.
                ThrowFirstErrorIfExisting(logException: false);
            }
            catch (Exception e)
            {
                Clean();
                DisposeCommands();

                if (!startSucceeded && e is PipelineStoppedException)
                {
                    // When a terminating error happens during command execution, PowerShell will first save it
                    // to '_firstTerminatingError', and then throw a 'PipelineStoppedException' to tear down the
                    // pipeline. So when the caught exception here is 'PipelineStoppedException', it may not be
                    // the actual original terminating error.
                    // In this case, we want to report the first terminating error which occurred during pipeline
                    // execution, regardless of whether other errors occurred afterward.
                    ThrowFirstErrorIfExisting(logException: false);
                }

                throw;
            }
        }

        /// <summary>
        /// Request that the pipeline execution should stop.  Unlike other
        /// methods of PipelineProcessor, this method can be called
        /// asynchronously.
        /// </summary>
        internal void Stop()
        {
            // Only call StopProcessing if the pipeline is being stopped
            // for the first time

            if (!RecordFailure(new PipelineStoppedException(), command: null))
            {
                return;
            }

            // Retain copy of _commands in case Dispose() is called
            List<CommandProcessorBase> commands = _commands;
            if (commands is null)
            {
                return;
            }

            // Call StopProcessing() for all the commands.
            foreach (CommandProcessorBase commandProcessor in commands)
            {
                if (commandProcessor == null)
                {
                    throw PSTraceSource.NewInvalidOperationException();
                }

                try
                {
                    commandProcessor.Command.DoStopProcessing();
                }
                catch (Exception)
                {
                    // We swallow exceptions which occur during StopProcessing.
                    continue;
                }
            }
        }

        #endregion public_methods

        #region private_methods

        /// <summary>
        /// Partially execute the pipeline, and retrieve the output
        /// after the input objects have been entered into the pipe.
        /// </summary>
        /// <param name="input">
        /// Array of input objects for first stage
        /// </param>
        /// <returns>
        /// Results from last pipeline stage.  This will be empty if
        /// ExternalSuccessOutput is set.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// PipelineExecuteRequiresAtLeastOneCommand
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// The pipeline has already been stopped, or a cmdlet encountered
        /// a terminating error
        /// </exception>
        /// <exception cref="ParameterBindingException">
        /// If any parameters fail to bind,
        /// or
        /// If any mandatory parameters are missing.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If there is an error generating the metadata for dynamic parameters.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// The pipeline has already been stopped,
        /// or a terminating error occurred.
        /// </exception>
        /// <exception cref="ExtendedTypeSystemException">
        /// An error occurred clearing the error variable.
        /// </exception>
        internal Array Step(object input)
        {
            bool injectSucceeded = false;
            try
            {
                Start(true);
                Inject(input, enumerate: false);
                injectSucceeded = true;

                // Check if this pipeline is being stopped asynchronously.
                ThrowFirstErrorIfExisting(logException: false);
                return RetrieveResults();
            }
            catch (Exception e)
            {
                Clean();
                DisposeCommands();

                if (!injectSucceeded && e is PipelineStoppedException)
                {
                    // When a terminating error happens during command execution, PowerShell will first save it
                    // to '_firstTerminatingError', and then throw a 'PipelineStoppedException' to tear down the
                    // pipeline. So when the caught exception here is 'PipelineStoppedException', it may not be
                    // the actual original terminating error.
                    // In this case, we want to report the first terminating error which occurred during pipeline
                    // execution, regardless of whether other errors occurred afterward.
                    ThrowFirstErrorIfExisting(logException: false);
                }

                throw;
            }
        }

        /// <summary>
        /// Prepares the pipeline for execution.
        /// </summary>
        /// <param name="incomingStream">
        /// Input objects are expected, so do not close the first command.
        /// This will prevent the one default call to ProcessRecord
        /// on the first command.
        /// </param>
        /// <remarks>
        /// Start must always be called in a context where terminating errors will
        /// be caught and result in DisposeCommands.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// PipelineExecuteRequiresAtLeastOneCommand
        /// </exception>
        /// <exception cref="ParameterBindingException">
        /// If any parameters fail to bind,
        /// or
        /// If any mandatory parameters are missing.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If there is an error generating the metadata for dynamic parameters.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// The pipeline has already been stopped,
        /// or a terminating error occurred in a downstream cmdlet.
        /// </exception>
        /// <exception cref="ExtendedTypeSystemException">
        /// An error occurred clearing the error variable.
        /// </exception>
        private void Start(bool incomingStream)
        {
            // Every call to Step or SynchronousExecute will call Start.
            if (_disposed)
            {
                throw PSTraceSource.NewObjectDisposedException("PipelineProcessor");
            }

            if (Stopping)
            {
                throw new PipelineStoppedException();
            }

            if (_executionStarted)
            {
                return;
            }

            if (_commands == null || _commands.Count == 0)
            {
                throw PSTraceSource.NewInvalidOperationException(
                    PipelineStrings.PipelineExecuteRequiresAtLeastOneCommand);
            }

            CommandProcessorBase firstcommandProcessor = _commands[0];
            ValidateCommandProcessorNotNull(firstcommandProcessor, PipelineStrings.PipelineExecuteRequiresAtLeastOneCommand);

            // Set the execution scope using the current scope
            _executionScope ??= firstcommandProcessor.Context.EngineSessionState.CurrentScope;

            // add ExternalSuccessOutput to the last command
            CommandProcessorBase LastCommandProcessor = _commands[_commands.Count - 1];
            ValidateCommandProcessorNotNull(LastCommandProcessor, errorMessage: null);

            if (ExternalSuccessOutput != null)
            {
                LastCommandProcessor.CommandRuntime.OutputPipe.ExternalWriter = ExternalSuccessOutput;
            }

            // add ExternalErrorOutput to all commands whose error
            // output is not yet claimed
            SetExternalErrorOutput();

            if (ExternalInput == null && !incomingStream)
            {
                // no upstream cmdlet from the first command
                firstcommandProcessor.CommandRuntime.IsClosed = true;
            }

            // We want the value of PSDefaultParameterValues before possibly changing to the commands scopes.
            // This ensures we use the value from the caller's scope, not the callee's scope.
            IDictionary psDefaultParameterValues =
                firstcommandProcessor.Context.GetVariableValue(SpecialVariables.PSDefaultParameterValuesVarPath, false) as IDictionary;

            _executionStarted = true;

            // Allocate the pipeline iteration array; note that the pipeline position for
            // each command starts at 1 so we need to allocate _commands.Count + 1 items.
            int[] pipelineIterationInfo = new int[_commands.Count + 1];

            // Prepare all commands from Engine's side, and make sure they are all valid
            for (int i = 0; i < _commands.Count; i++)
            {
                CommandProcessorBase commandProcessor = _commands[i];
                if (commandProcessor == null)
                {
                    // "null command " + i
                    throw PSTraceSource.NewInvalidOperationException();
                }

                // Generate new Activity Id for the thread
                Guid pipelineActivityId = EtwActivity.CreateActivityId();
                EtwActivity.SetActivityId(pipelineActivityId);
                commandProcessor.PipelineActivityId = pipelineActivityId;

                // Log a command started event
                MshLog.LogCommandLifecycleEvent(
                    commandProcessor.Context,
                    CommandState.Started,
                    commandProcessor.Command.MyInvocation);

#if LEGACYTELEMETRY
                Microsoft.PowerShell.Telemetry.Internal.TelemetryAPI.TraceExecutedCommand(commandProcessor.Command.CommandInfo, commandProcessor.Command.CommandOrigin);
#endif

                // Log the execution of a command (not script chunks, as they are not commands in and of themselves)
                if (commandProcessor.CommandInfo.CommandType != CommandTypes.Script)
                {
                    LogExecutionInfo(commandProcessor.Command.MyInvocation, commandProcessor.CommandInfo.Name);
                }

                InvocationInfo myInfo = commandProcessor.Command.MyInvocation;
                myInfo.PipelinePosition = i + 1;
                myInfo.PipelineLength = _commands.Count;
                myInfo.PipelineIterationInfo = pipelineIterationInfo;
                myInfo.ExpectingInput = commandProcessor.IsPipelineInputExpected();
                commandProcessor.DoPrepare(psDefaultParameterValues);
            }

            // Clear ErrorVariable as appropriate
            SetupParameterVariables();

            // Prepare all commands from Command's side.
            // Note that DoPrepare() and DoBegin() should NOT be combined
            // in a single for loop.
            // Reason: Encoding of commandline parameters happen
            // as part of DoPrepare(). If they are combined,
            // the first command's DoBegin() will be called before
            // the next command's DoPrepare(). Since BeginProcessing()
            // can write objects to the downstream commandlet,
            // it will end up calling DoExecute() (from Pipe.Add())
            // before DoPrepare.
            for (int i = 0; i < _commands.Count; i++)
            {
                CommandProcessorBase commandProcessor = _commands[i];

                commandProcessor.DoBegin();
            }
        }

        /// <summary>
        /// Add ExternalErrorOutput to all commands whose error output is not yet claimed.
        /// </summary>
        private void SetExternalErrorOutput()
        {
            if (ExternalErrorOutput != null)
            {
                for (int i = 0; i < _commands.Count; i++)
                {
                    CommandProcessorBase commandProcessor = _commands[i];
                    Pipe errorPipe = commandProcessor.CommandRuntime.ErrorOutputPipe;

                    // check whether a cmdlet is consuming the error pipe
                    if (!errorPipe.IsRedirected)
                    {
                        errorPipe.ExternalWriter = ExternalErrorOutput;
                    }
                }
            }
        }

        /// <summary>
        /// Clear ErrorVariable as appropriate.
        /// </summary>
        private void SetupParameterVariables()
        {
            foreach (CommandProcessorBase commandProcessor in _commands)
            {
                ValidateCommandProcessorNotNull(commandProcessor, errorMessage: null);

                commandProcessor.CommandRuntime.SetupOutVariable();
                commandProcessor.CommandRuntime.SetupErrorVariable();
                commandProcessor.CommandRuntime.SetupWarningVariable();
                commandProcessor.CommandRuntime.SetupPipelineVariable();
                commandProcessor.CommandRuntime.SetupInformationVariable();
            }
        }

        private static void ValidateCommandProcessorNotNull(CommandProcessorBase commandProcessor, string errorMessage)
        {
            if (commandProcessor?.CommandRuntime is null)
            {
                throw errorMessage is null
                    ? PSTraceSource.NewInvalidOperationException()
                    : PSTraceSource.NewInvalidOperationException(errorMessage, Array.Empty<object>());
            }
        }

        /// <summary>
        /// Partially execute the pipeline.  The output remains in
        /// the pipes.
        /// </summary>
        /// <param name="input">
        /// Array of input objects for first stage
        /// </param>
        /// <param name="enumerate">If true, unravel the input otherwise pass as one object.</param>
        /// <throws>
        /// Exception if any cmdlet throws a [terminating] exception
        /// </throws>
        /// <remarks>
        /// Inject must always be called in a context where terminating errors will
        /// be caught and result in DisposeCommands.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// PipelineExecuteRequiresAtLeastOneCommand
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// The pipeline has already been stopped, or a cmdlet encountered
        /// a terminating error
        /// </exception>
        /// <exception cref="PipelineClosedException">
        /// The ExternalWriter stream is closed
        /// </exception>
        private void Inject(object input, bool enumerate)
        {
            // Add any input to the first command.
            CommandProcessorBase firstcommandProcessor = _commands[0];
            ValidateCommandProcessorNotNull(firstcommandProcessor, PipelineStrings.PipelineExecuteRequiresAtLeastOneCommand);

            if (input != AutomationNull.Value)
            {
                if (enumerate)
                {
                    IEnumerator enumerator = LanguagePrimitives.GetEnumerator(input);

                    if (enumerator != null)
                    {
                        firstcommandProcessor.CommandRuntime.InputPipe = new Pipe(enumerator);
                    }
                    else
                    {
                        firstcommandProcessor.CommandRuntime.InputPipe.Add(input);
                    }
                }
                else
                {
                    firstcommandProcessor.CommandRuntime.InputPipe.Add(input);
                }
            }

            // Do not set ExternalInput until SynchronousExecute is called
            // Execute the first command - In the streamlet model, Execute of the first command will
            // automatically call the downstream command incase if there are any objects in the pipe.
            firstcommandProcessor.DoExecute();
        }

        /// <summary>
        /// Retrieve results from the pipeline.
        /// </summary>
        /// <returns>
        /// Results from last pipeline stage.  This will be empty if
        /// ExternalSuccessOutput is set or if this pipeline has been linked.
        /// </returns>
        private Array RetrieveResults()
        {
            if (_commands is null)
            {
                // This could happen to an expression redirection pipeline (e.g. 1 > a.txt).
                // An exception may be thrown from the call to 'StartStepping' or 'Step' on the pipeline,
                // which causes the pipeline commands to be disposed.
                return MshCommandRuntime.StaticEmptyArray;
            }

            // If the error queue has been linked, it's up to the link to
            // deal with the output. Don't do anything here...
            if (!_linkedErrorOutput)
            {
                foreach (CommandProcessorBase commandProcessor in _commands)
                {
                    ValidateCommandProcessorNotNull(commandProcessor, errorMessage: null);

                    Pipe ErrorPipe = commandProcessor.CommandRuntime.ErrorOutputPipe;
                    if (ErrorPipe.DownstreamCmdlet == null && !ErrorPipe.Empty)
                    {
                        // Clear the error pipe if it's not empty and will not be consumed.
                        ErrorPipe.Clear();
                    }
                }
            }

            // If the success queue has been linked, it's up to the link to
            // deal with the output. Don't do anything here...
            if (_linkedSuccessOutput)
            {
                return MshCommandRuntime.StaticEmptyArray;
            }

            CommandProcessorBase LastCommandProcessor = _commands[_commands.Count - 1];
            ValidateCommandProcessorNotNull(LastCommandProcessor, errorMessage: null);

            Array results = LastCommandProcessor.CommandRuntime.GetResultsAsArray();

            // Do not return the same results more than once
            LastCommandProcessor.CommandRuntime.OutputPipe.Clear();
            return results is null ? MshCommandRuntime.StaticEmptyArray : results;
        }

        /// <summary>
        /// Links this pipeline to a pre-existing Pipe object. This allows nested pipes
        /// to write into the parent pipeline. It does this by resetting the terminal
        /// pipeline object.
        /// </summary>
        /// <param name="pipeToUse">The pipeline to write success objects to.</param>
        internal void LinkPipelineSuccessOutput(Pipe pipeToUse)
        {
            Dbg.Assert(pipeToUse != null, "Caller should verify pipeToUse != null");

            CommandProcessorBase LastCommandProcessor = _commands[_commands.Count - 1];
            ValidateCommandProcessorNotNull(LastCommandProcessor, errorMessage: null);

            LastCommandProcessor.CommandRuntime.OutputPipe = pipeToUse;
            _linkedSuccessOutput = true;
        }

        internal void LinkPipelineErrorOutput(Pipe pipeToUse)
        {
            Dbg.Assert(pipeToUse != null, "Caller should verify pipeToUse != null");

            foreach (CommandProcessorBase commandProcessor in _commands)
            {
                ValidateCommandProcessorNotNull(commandProcessor, errorMessage: null);

                if (commandProcessor.CommandRuntime.ErrorOutputPipe.DownstreamCmdlet == null)
                {
                    commandProcessor.CommandRuntime.ErrorOutputPipe = pipeToUse;
                }
            }

            _linkedErrorOutput = true;
        }

        /// <summary>
        /// When the command is complete, Command should be disposed.
        /// This enables cmdlets to reliably release file handles etc.
        /// without waiting for garbage collection.
        /// Exceptions occurring while disposing commands are recorded
        /// but not passed through.
        /// </summary>
        private void DisposeCommands()
        {
            // Note that this is not in a lock.
            // We do not make Dispose() wait until StopProcessing() has completed.
            _stopping = true;

            if (_commands is null && _redirectionPipes is null)
            {
                // Commands were already disposed.
                return;
            }

            LogToEventLog();

            if (_commands is not null)
            {
                foreach (CommandProcessorBase commandProcessor in _commands)
                {
                    if (commandProcessor is null)
                    {
                        continue;
                    }

                    // If Dispose throws an exception, record it as a pipeline failure and continue disposing cmdlets.
                    try
                    {
                        // Only cmdlets can have variables defined via the common parameters.
                        // We handle the cleanup of those variables only if we need to.
                        if (commandProcessor is CommandProcessor)
                        {
                            if (commandProcessor.Command is not PSScriptCmdlet)
                            {
                                // For script cmdlets, the variable lists were already removed when exiting a scope.
                                // So we only need to take care of binary cmdlets here.
                                commandProcessor.CommandRuntime.RemoveVariableListsInPipe();
                            }

                            // Remove the pipeline variable if we need to.
                            commandProcessor.CommandRuntime.RemovePipelineVariable();
                        }

                        commandProcessor.Dispose();
                    }
                    catch (Exception e)
                    {
                        // The only vaguely plausible reason for a failure here is an exception in 'Command.Dispose'.
                        // As such, this should be covered by the overall exemption.
                        InvocationInfo myInvocation = commandProcessor.Command?.MyInvocation;

                        if (e is ProviderInvocationException pie)
                        {
                            e = new CmdletProviderInvocationException(pie, myInvocation);
                        }
                        else
                        {
                            e = new CmdletInvocationException(e, myInvocation);

                            // Log a command health event
                            MshLog.LogCommandHealthEvent(commandProcessor.Command.Context, e, Severity.Warning);
                        }

                        RecordFailure(e, commandProcessor.Command);
                    }
                }
            }

            _commands = null;

            // Now dispose any pipes that were used for redirection...
            if (_redirectionPipes is not null)
            {
                foreach (PipelineProcessor redirPipe in _redirectionPipes)
                {
                    if (redirPipe is null)
                    {
                        continue;
                    }

                    // Clean resources for script commands.
                    // It is possible (though very unlikely) that the call to 'Step' on the redirection pipeline failed.
                    // In such a case, 'Clean' would have run and the 'pipelineProcessor' would have been disposed.
                    // Therefore, calling 'Clean' again will simply return, because '_commands' was already set to null.
                    redirPipe.Clean();

                    // The complicated logic of disposing the commands is taken care
                    // of through recursion, this routine should not be getting any
                    // exceptions...
                    try
                    {
                        redirPipe.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            _redirectionPipes = null;
        }

        private readonly object _stopReasonLock = new object();
        /// <summary>
        /// Makes an internal note of the exception, but only if this is
        /// the first error.
        /// </summary>
        /// <param name="e">Error which terminated the pipeline.</param>
        /// <param name="command">Command against which to log SecondFailure.</param>
        /// <returns>True iff the pipeline was not already stopped.</returns>
        internal bool RecordFailure(Exception e, InternalCommand command)
        {
            bool wasStopping = false;
            lock (_stopReasonLock)
            {
                if (_firstTerminatingError == null)
                {
                    _firstTerminatingError = ExceptionDispatchInfo.Capture(e);
                }
                // Error Architecture: Log/trace second and subsequent RecordFailure.
                // Note that the pipeline could have been stopped asynchronously before hitting the error,
                // therefore we check whether '_firstTerminatingError' is 'PipelineStoppedException'.
                else if (_firstTerminatingError.SourceException is not PipelineStoppedException
                    && command?.Context != null)
                {
                    Exception ex = e;
                    while ((ex is TargetInvocationException || ex is CmdletInvocationException)
                            && (ex.InnerException != null))
                    {
                        ex = ex.InnerException;
                    }

                    if (ex is not PipelineStoppedException)
                    {
                        string message = StringUtil.Format(PipelineStrings.SecondFailure,
                            _firstTerminatingError.GetType().Name,
                            _firstTerminatingError.SourceException.StackTrace,
                            ex.GetType().Name,
                            ex.StackTrace
                        );

                        MshLog.LogCommandHealthEvent(
                            command.Context,
                            new InvalidOperationException(message, ex),
                            Severity.Warning);
                    }
                }

                wasStopping = _stopping;
                _stopping = true;
            }

            return !wasStopping;
        }

        /// <summary>
        /// Sometimes we shouldn't be rethrow the exception we previously caught,
        /// such as when the exception is handled by a trap.
        /// </summary>
        internal void ForgetFailure()
        {
            _firstTerminatingError = null;
        }

        // NOTICE-2004/06/08-JonN 959638
        // Only this InternalCommand from this Thread is allowed to call
        // WriteObject/WriteError
        internal InternalCommand _permittedToWrite = null;
        internal bool _permittedToWriteToPipeline = false;
        internal System.Threading.Thread _permittedToWriteThread = null;

        #endregion private_methods

        #region public_properties

        /// <summary>
        /// ExternalInput allows the caller to specify an asynchronous source for
        /// the input to the first command in the pipeline.  Note that if
        /// ExternalInput is specified, SynchronousExecute will not return
        /// until the ExternalInput is closed.
        /// </summary>
        /// <remarks>
        /// It is the responsibility of the caller to ensure that the object
        /// reader is closed, usually by another thread.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// ExecutionAlreadyStarted: pipeline has already started or completed
        /// </exception>
        internal PipelineReader<object> ExternalInput
        {
            get
            {
                return _externalInputPipe;
            }

            set
            {
                if (_executionStarted)
                {
                    throw PSTraceSource.NewInvalidOperationException(
                        PipelineStrings.ExecutionAlreadyStarted);
                }

                _externalInputPipe = value;
            }
        }

        /// <summary>
        /// ExternalSuccessOutput provides asynchronous access to the
        /// success output of the last command in the pipeline.  Note that
        /// if ExternalSuccessOutput is specified, the result array return value
        /// to SynchronousExecute will always be empty.  PipelineProcessor will
        /// close ExternalSuccessOutput when the pipeline is finished.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// ExecutionAlreadyStarted: pipeline has already started or completed
        /// </exception>
        internal PipelineWriter ExternalSuccessOutput
        {
            get
            {
                return _externalSuccessOutput;
            }

            set
            {
                if (_executionStarted)
                {
                    throw PSTraceSource.NewInvalidOperationException(
                        PipelineStrings.ExecutionAlreadyStarted);
                }

                _externalSuccessOutput = value;
            }
        }

        /// <summary>
        /// ExternalErrorOutput provides asynchronous access to the combined
        /// error output of all commands in the pipeline except what is routed
        /// to other commands in the pipeline.  Note that if
        /// ExternalErrorOutput is specified, the errorResults return parameter to
        /// SynchronousExecute will always be empty.  PipelineProcessor will
        /// close ExternalErrorOutput when the pipeline is finished.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// ExecutionAlreadyStarted: pipeline has already started or completed
        /// </exception>
        internal PipelineWriter ExternalErrorOutput
        {
            get
            {
                return _externalErrorOutput;
            }

            set
            {
                if (_executionStarted)
                {
                    throw PSTraceSource.NewInvalidOperationException(
                        PipelineStrings.ExecutionAlreadyStarted);
                }

                _externalErrorOutput = value;
            }
        }

        /// <summary>
        /// Indicates whether this PipelineProcessor has already started.
        /// If so, some properties can no longer be changed.
        /// </summary>
        internal bool ExecutionStarted
        {
            get { return _executionStarted; }
        }

        /// <summary>
        /// Indicates whether stop has been requested on this PipelineProcessor.
        /// </summary>
        internal bool Stopping
        {
            get { return _localPipeline != null && _localPipeline.IsStopping; }
        }

        private LocalPipeline _localPipeline;

        internal LocalPipeline LocalPipeline
        {
            get { return _localPipeline; }

            set { _localPipeline = value; }
        }

        internal bool TopLevel { get; set; } = false;

        /// <summary>
        /// The scope the pipeline should execute in.
        /// </summary>
        internal SessionStateScope ExecutionScope
        {
            get
            {
                return _executionScope;
            }

            set
            {
                // This needs to be settable so that a steppable pipeline
                // can be stepped in the context of the caller, not where
                // it was created...
                _executionScope = value;
            }
        }
        #endregion public_properties

        internal enum PipelineExecutionStatus
        {
            Started,
            ParameterBinding,
            Complete,
            Error,
            PipelineComplete
        }
    }
}
