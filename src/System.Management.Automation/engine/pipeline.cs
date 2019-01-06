// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Management.Automation.Runspaces;
using Dbg = System.Management.Automation.Diagnostics;
using System.Management.Automation.Tracing;
using System.Runtime.ExceptionServices;

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
            GC.SuppressFinalize(this);
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

        /// <summary>
        /// Finalizer for class PipelineProcessor.
        /// </summary>
        ~PipelineProcessor()
        {
            Dispose(false);
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

        private string GetCommand(InvocationInfo invocationInfo)
        {
            if (invocationInfo == null)
                return string.Empty;

            if (invocationInfo.MyCommand != null)
            {
                return invocationInfo.MyCommand.Name;
            }

            return string.Empty;
        }

        private string GetCommand(Exception exception)
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
                if (hostInterface != null)
                {
                    hostInterface.TranscribeCommand(logElement, invocation);
                }
            }

            if (!string.IsNullOrEmpty(logElement))
            {
                _eventLogBuffer.Add(logElement);
            }
        }

        internal void LogToEventLog()
        {
            if (NeedToLog())
            {
                // We check to see if the command is needs writing (or if there is anything in the buffer)
                // before we flush it. Flushing the empty buffer causes a measurable performance degradation.
                if (_commands == null || _commands.Count <= 0 || _eventLogBuffer.Count == 0)
                    return;

                MshLog.LogPipelineExecutionDetailEvent(_commands[0].Command.Context,
                                                       _eventLogBuffer,
                                                       _commands[0].Command.MyInvocation);
            }
        }

        private bool NeedToLog()
        {
            if (_commands == null)
                return false;

            foreach (CommandProcessorBase commandProcessor in _commands)
            {
                MshCommandRuntime cmdRuntime = commandProcessor.Command.commandRuntime as MshCommandRuntime;

                if (cmdRuntime != null && cmdRuntime.LogPipelineExecutionDetail)
                    return true;
            }

            return false;
        }

        private List<string> _eventLogBuffer = new List<string>();
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
            commandProcessor.CommandRuntime.PipelineProcessor = this;
            return AddCommand(commandProcessor, _commands.Count, false);
        }

        internal void AddRedirectionPipe(PipelineProcessor pipelineProcessor)
        {
            if (pipelineProcessor == null) throw PSTraceSource.NewArgumentNullException("pipelineProcessor");
            if (_redirectionPipes == null)
                _redirectionPipes = new List<PipelineProcessor>();
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
        internal int AddCommand(CommandProcessorBase commandProcessor, int readFromCommand, bool readErrorQueue)
        {
            if (commandProcessor == null)
            {
                throw PSTraceSource.NewArgumentNullException("commandProcessor");
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

            if (0 == _commands.Count)
            {
                if (0 != readFromCommand)
                {
                    // "First command cannot have input"
                    throw PSTraceSource.NewArgumentException(
                        "readFromCommand",
                        PipelineStrings.FirstCommandCannotHaveInput);
                }

                commandProcessor.AddedToPipelineAlready = true;
            }
            // 2003/08/11-JonN Subsequent commands must have predecessor
            else if (readFromCommand > _commands.Count || readFromCommand <= 0)
            {
                // "invalid command number"
                throw PSTraceSource.NewArgumentException(
                    "readFromCommand",
                    PipelineStrings.InvalidCommandNumber);
            }
            else
            {
                CommandProcessorBase prevcommandProcessor = _commands[readFromCommand - 1] as CommandProcessorBase;
                if (prevcommandProcessor == null || prevcommandProcessor.CommandRuntime == null)
                {
                    // "PipelineProcessor.AddCommand(): previous request object == null"
                    throw PSTraceSource.NewInvalidOperationException();
                }

                Pipe UpstreamPipe = (readErrorQueue) ?
                    prevcommandProcessor.CommandRuntime.ErrorOutputPipe : prevcommandProcessor.CommandRuntime.OutputPipe;
                if (UpstreamPipe == null)
                {
                    // "PipelineProcessor.AddCommand(): UpstreamPipe == null"
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
                        if (prevcommandProcessor == null || prevcommandProcessor.CommandRuntime == null)
                        {
                            // "PipelineProcessor.AddCommand(): previous request object == null"
                            throw PSTraceSource.NewInvalidOperationException();
                        }
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

            ExceptionDispatchInfo toRethrowInfo;
            try
            {
                CommandProcessorBase commandRequestingUpstreamCommandsToStop = null;
                try
                {
                    // If the caller specified an input object array,
                    // we run assuming there is an incoming "stream"
                    // of objects. This will prevent the one default call
                    // to ProcessRecord on the first command.
                    Start(input != AutomationNull.Value);

                    // Start has already validated firstcommandProcessor
                    CommandProcessorBase firstCommandProcessor = _commands[0];

                    // Add any input to the first command.
                    if (ExternalInput != null)
                    {
                        firstCommandProcessor.CommandRuntime.InputPipe.ExternalReader
                            = ExternalInput;
                    }

                    Inject(input, enumerate: true);
                }
                catch (PipelineStoppedException)
                {
                    StopUpstreamCommandsException stopUpstreamCommandsException =
                        _firstTerminatingError != null
                            ? _firstTerminatingError.SourceException as StopUpstreamCommandsException
                            : null;
                    if (stopUpstreamCommandsException == null)
                    {
                        throw;
                    }
                    else
                    {
                        _firstTerminatingError = null;
                        commandRequestingUpstreamCommandsToStop = stopUpstreamCommandsException.RequestingCommandProcessor;
                    }
                }

                DoCompleteCore(commandRequestingUpstreamCommandsToStop);

                // By this point, we are sure all commandProcessors hosted by the current pipelineProcess are done execution,
                // so if there are any redirection pipelineProcessors associated with any of those commandProcessors, we should
                // call DoComplete on them.
                if (_redirectionPipes != null)
                {
                    foreach (PipelineProcessor redirectPipelineProcessor in _redirectionPipes)
                    {
                        redirectPipelineProcessor.DoCompleteCore(null);
                    }
                }

                return RetrieveResults();
            }
            catch (RuntimeException e)
            {
                // The error we want to report is the first terminating error
                // which occurred during pipeline execution, regardless
                // of whether other errors occurred afterward.
                toRethrowInfo = _firstTerminatingError ?? ExceptionDispatchInfo.Capture(e);
                this.LogExecutionException(toRethrowInfo.SourceException);
            }
            // NTRAID#Windows Out Of Band Releases-929020-2006/03/14-JonN
            catch (System.Runtime.InteropServices.InvalidComObjectException comException)
            {
                // The error we want to report is the first terminating error
                // which occurred during pipeline execution, regardless
                // of whether other errors occurred afterward.
                if (_firstTerminatingError != null)
                {
                    toRethrowInfo = _firstTerminatingError;
                }
                else
                {
                    string message = StringUtil.Format(ParserStrings.InvalidComObjectException, comException.Message);
                    var rte = new RuntimeException(message, comException);
                    rte.SetErrorId("InvalidComObjectException");
                    toRethrowInfo = ExceptionDispatchInfo.Capture(rte);
                }

                this.LogExecutionException(toRethrowInfo.SourceException);
            }
            finally
            {
                DisposeCommands();
            }

            // By rethrowing the exception outside of the handler,
            // we allow the CLR on X64/IA64 to free from the stack
            // the exception records related to this exception.

            // The only reason we should get here is if
            // an exception should be rethrown.
            Diagnostics.Assert(toRethrowInfo != null, "Alternate protocol path failure");
            toRethrowInfo.Throw();
            return null; // UNREACHABLE
        }

        private void DoCompleteCore(CommandProcessorBase commandRequestingUpstreamCommandsToStop)
        {
            // Call DoComplete() for all the commands. DoComplete() will internally call Complete()
            MshCommandRuntime lastCommandRuntime = null;

            if (_commands != null)
            {
                for (int i = 0; i < _commands.Count; i++)
                {
                    CommandProcessorBase commandProcessor = _commands[i];

                    if (commandProcessor == null)
                    {
                        // "null command " + i
                        throw PSTraceSource.NewInvalidOperationException();
                    }

                    if (object.ReferenceEquals(commandRequestingUpstreamCommandsToStop, commandProcessor))
                    {
                        commandRequestingUpstreamCommandsToStop = null;
                        continue; // do not call DoComplete/EndProcessing on the command that initiated stopping
                    }

                    if (commandRequestingUpstreamCommandsToStop != null)
                    {
                        continue; // do not call DoComplete/EndProcessing on commands that were stopped upstream
                    }

                    try
                    {
                        commandProcessor.DoComplete();
                    }
                    catch (PipelineStoppedException)
                    {
                        StopUpstreamCommandsException stopUpstreamCommandsException =
                            _firstTerminatingError != null
                                ? _firstTerminatingError.SourceException as StopUpstreamCommandsException
                                : null;
                        if (stopUpstreamCommandsException == null)
                        {
                            throw;
                        }
                        else
                        {
                            _firstTerminatingError = null;
                            commandRequestingUpstreamCommandsToStop = stopUpstreamCommandsException.RequestingCommandProcessor;
                        }
                    }

                    EtwActivity.SetActivityId(commandProcessor.PipelineActivityId);

                    // Log a command stopped event
                    MshLog.LogCommandLifecycleEvent(
                        commandProcessor.Command.Context,
                        CommandState.Stopped,
                        commandProcessor.Command.MyInvocation);

                    // Log the execution of a command (not script chunks, as they
                    // are not commands in and of themselves)
                    if (commandProcessor.CommandInfo.CommandType != CommandTypes.Script)
                    {
                        commandProcessor.CommandRuntime.PipelineProcessor.LogExecutionComplete(
                            commandProcessor.Command.MyInvocation, commandProcessor.CommandInfo.Name);
                    }

                    lastCommandRuntime = commandProcessor.CommandRuntime;
                }
            }

            // Log the pipeline completion.
            if (lastCommandRuntime != null)
            {
                // Only log the pipeline completion if this wasn't a nested pipeline, as
                // pipeline state in transcription is associated with the toplevel pipeline
                if ((this.LocalPipeline == null) || (!this.LocalPipeline.IsNested))
                {
                    lastCommandRuntime.PipelineProcessor.LogPipelineComplete();
                }
            }

            // If a terminating error occurred, report it now.
            if (_firstTerminatingError != null)
            {
                this.LogExecutionException(_firstTerminatingError.SourceException);
                _firstTerminatingError.Throw();
            }
        }

        /// <summary>
        /// Implements DoComplete as a stand-alone function for completing
        /// the execution of a steppable pipeline.
        /// </summary>
        /// <returns>The results of the execution.</returns>
        internal Array DoComplete()
        {
            if (Stopping)
            {
                throw new PipelineStoppedException();
            }

            if (!_executionStarted)
            {
                throw PSTraceSource.NewInvalidOperationException(
                    PipelineStrings.PipelineNotStarted);
            }

            ExceptionDispatchInfo toRethrowInfo;
            try
            {
                DoCompleteCore(null);

                return RetrieveResults();
            }
            catch (RuntimeException e)
            {
                // The error we want to report is the first terminating error
                // which occurred during pipeline execution, regardless
                // of whether other errors occurred afterward.
                toRethrowInfo = _firstTerminatingError ?? ExceptionDispatchInfo.Capture(e);
                this.LogExecutionException(toRethrowInfo.SourceException);
            }
            // NTRAID#Windows Out Of Band Releases-929020-2006/03/14-JonN
            catch (System.Runtime.InteropServices.InvalidComObjectException comException)
            {
                // The error we want to report is the first terminating error
                // which occurred during pipeline execution, regardless
                // of whether other errors occurred afterward.
                if (_firstTerminatingError != null)
                {
                    toRethrowInfo = _firstTerminatingError;
                }
                else
                {
                    string message = StringUtil.Format(ParserStrings.InvalidComObjectException, comException.Message);
                    var rte = new RuntimeException(message, comException);
                    rte.SetErrorId("InvalidComObjectException");
                    toRethrowInfo = ExceptionDispatchInfo.Capture(rte);
                }

                this.LogExecutionException(toRethrowInfo.SourceException);
            }
            finally
            {
                DisposeCommands();
            }

            // By rethrowing the exception outside of the handler,
            // we allow the CLR on X64/IA64 to free from the stack
            // the exception records related to this exception.

            // The only reason we should get here is if
            // an exception should be rethrown.
            Diagnostics.Assert(toRethrowInfo != null, "Alternate protocol path failure");
            toRethrowInfo.Throw();
            return null; // UNREACHABLE
        }

        /// <summary>
        /// This routine starts the stepping process. It is optional to
        /// call this but can be useful if you want the begin clauses
        /// of the pipeline to be run even when there may not be any input
        /// to process as is the case for I/O redirection into a file. We
        /// still want the file opened, even if there was nothing to write to it.
        /// </summary>
        /// <param name="expectInput">True if you want to write to this pipeline.</param>
        internal void StartStepping(bool expectInput)
        {
            try
            {
                Start(expectInput);

                // If a terminating error occurred, report it now.
                if (_firstTerminatingError != null)
                {
                    _firstTerminatingError.Throw();
                }
            }
            catch (PipelineStoppedException)
            {
                DisposeCommands();

                // The error we want to report is the first terminating error
                // which occurred during pipeline execution, regardless
                // of whether other errors occurred afterward.
                if (_firstTerminatingError != null)
                {
                    _firstTerminatingError.Throw();
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

            if (!RecordFailure(new PipelineStoppedException(), null))
                return;

            // Retain copy of _commands in case Dispose() is called
            List<CommandProcessorBase> commands = _commands;
            if (commands == null)
                return;

            // Call StopProcessing() for all the commands.
            for (int i = 0; i < commands.Count; i++)
            {
                CommandProcessorBase commandProcessor = commands[i];

                if (commandProcessor == null)
                {
                    throw PSTraceSource.NewInvalidOperationException();
                }
#pragma warning disable 56500
                try
                {
                    commandProcessor.Command.DoStopProcessing();
                }
                catch (Exception)
                {
                    // 2004/04/26-JonN We swallow exceptions
                    // which occur during StopProcessing.
                    continue;
                }
#pragma warning restore 56500
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
            if (Stopping)
            {
                throw new PipelineStoppedException();
            }

            try
            {
                Start(true);
                Inject(input, enumerate: false);

                // If a terminating error occurred, report it now.
                if (_firstTerminatingError != null)
                {
                    _firstTerminatingError.Throw();
                }

                return RetrieveResults();
            }
            catch (PipelineStoppedException)
            {
                DisposeCommands();

                // The error we want to report is the first terminating error
                // which occurred during pipeline execution, regardless
                // of whether other errors occurred afterward.
                if (_firstTerminatingError != null)
                {
                    _firstTerminatingError.Throw();
                }

                throw;
            }
            catch (Exception)
            {
                DisposeCommands();
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
                return;

            if (_commands == null || 0 == _commands.Count)
            {
                throw PSTraceSource.NewInvalidOperationException(
                    PipelineStrings.PipelineExecuteRequiresAtLeastOneCommand);
            }

            CommandProcessorBase firstcommandProcessor = _commands[0];
            if (firstcommandProcessor == null
                || firstcommandProcessor.CommandRuntime == null)
            {
                throw PSTraceSource.NewInvalidOperationException(
                    PipelineStrings.PipelineExecuteRequiresAtLeastOneCommand);
            }

            // Set the execution scope using the current scope
            if (_executionScope == null)
            {
                _executionScope = firstcommandProcessor.Context.EngineSessionState.CurrentScope;
            }

            // add ExternalSuccessOutput to the last command
            CommandProcessorBase LastCommandProcessor = _commands[_commands.Count - 1];
            if (LastCommandProcessor == null
                || LastCommandProcessor.CommandRuntime == null)
            {
                // "PipelineProcessor.Start(): LastCommandProcessor == null"
                throw PSTraceSource.NewInvalidOperationException();
            }

            if (ExternalSuccessOutput != null)
            {
                LastCommandProcessor.CommandRuntime.OutputPipe.ExternalWriter
                    = ExternalSuccessOutput;
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
            // This ensures we use the value from the callers scope, not the callees scope.
            IDictionary psDefaultParameterValues =
                firstcommandProcessor.Context.GetVariableValue(SpecialVariables.PSDefaultParameterValuesVarPath, false) as IDictionary;

            _executionStarted = true;

            //
            // Allocate the pipeline iteration array; note that the pipeline position for
            // each command starts at 1 so we need to allocate _commands.Count + 1 items.
            //
            int[] pipelineIterationInfo = new int[_commands.Count + 1];

            // Prepare all commands from Engine's side,
            // and make sure they are all valid
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

                // commandProcess.PipelineActivityId = new Activity id
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

                // Log the execution of a command (not script chunks, as they
                // are not commands in and of themselves)
                if (commandProcessor.CommandInfo.CommandType != CommandTypes.Script)
                {
                    commandProcessor.CommandRuntime.PipelineProcessor.LogExecutionInfo(
                        commandProcessor.Command.MyInvocation, commandProcessor.CommandInfo.Name);
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
        /// Add ExternalErrorOutput to all commands whose error
        /// output is not yet claimed.
        /// </summary>
        private void SetExternalErrorOutput()
        {
            if (ExternalErrorOutput != null)
            {
                for (int i = 0; i < _commands.Count; i++)
                {
                    CommandProcessorBase commandProcessor = _commands[i];
                    Pipe UpstreamPipe =
                        commandProcessor.CommandRuntime.ErrorOutputPipe;

                    // check whether a cmdlet is consuming the error pipe
                    if (!UpstreamPipe.IsRedirected)
                    {
                        UpstreamPipe.ExternalWriter =
                            ExternalErrorOutput;
                    }
                }
            }
        }

        /// <summary>
        /// Clear ErrorVariable as appropriate.
        /// </summary>
        private void SetupParameterVariables()
        {
            for (int i = 0; i < _commands.Count; i++)
            {
                CommandProcessorBase commandProcessor = _commands[i];
                if (commandProcessor == null || commandProcessor.CommandRuntime == null)
                {
                    // "null command " + i
                    throw PSTraceSource.NewInvalidOperationException();
                }

                commandProcessor.CommandRuntime.SetupOutVariable();
                commandProcessor.CommandRuntime.SetupErrorVariable();
                commandProcessor.CommandRuntime.SetupWarningVariable();
                commandProcessor.CommandRuntime.SetupPipelineVariable();
                commandProcessor.CommandRuntime.SetupInformationVariable();
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
            if (firstcommandProcessor == null
                || firstcommandProcessor.CommandRuntime == null)
            {
                throw PSTraceSource.NewInvalidOperationException(
                    PipelineStrings.PipelineExecuteRequiresAtLeastOneCommand);
            }

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
            // If the error queue has been linked, it's up to the link to
            // deal with the output. Don't do anything here...
            if (!_linkedErrorOutput)
            {
                // Retrieve any accumulated error objects from each of the pipes
                // and add them to the error results hash table.
                for (int i = 0; i < _commands.Count; i++)
                {
                    CommandProcessorBase commandProcessor = _commands[i];
                    if (commandProcessor == null
                        || commandProcessor.CommandRuntime == null)
                    {
                        // "null command or request or ErrorOutputPipe " + i
                        throw PSTraceSource.NewInvalidOperationException();
                    }

                    Pipe ErrorPipe = commandProcessor.CommandRuntime.ErrorOutputPipe;
                    if (ErrorPipe.DownstreamCmdlet == null && !ErrorPipe.Empty)
                    {
                        // 2003/10/02-JonN
                        // Do not return the same error results more than once
                        ErrorPipe.Clear();
                    }
                }
            }

            // If the success queue has been linked, it's up to the link to
            // deal with the output. Don't do anything here...
            if (_linkedSuccessOutput)
                return MshCommandRuntime.StaticEmptyArray;

            CommandProcessorBase LastCommandProcessor = _commands[_commands.Count - 1];
            if (LastCommandProcessor == null
                || LastCommandProcessor.CommandRuntime == null)
            {
                // "PipelineProcessor.RetrieveResults(): LastCommandProcessor == null"
                throw PSTraceSource.NewInvalidOperationException();
            }

            Array results =
                LastCommandProcessor.CommandRuntime.GetResultsAsArray();

            // 2003/10/02-JonN
            // Do not return the same results more than once
            LastCommandProcessor.CommandRuntime.OutputPipe.Clear();

            if (results == null)
                return MshCommandRuntime.StaticEmptyArray;
            return results;
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
            if (LastCommandProcessor == null
                || LastCommandProcessor.CommandRuntime == null)
            {
                // "PipelineProcessor.RetrieveResults(): LastCommandProcessor == null"
                throw PSTraceSource.NewInvalidOperationException();
            }

            LastCommandProcessor.CommandRuntime.OutputPipe = pipeToUse;
            _linkedSuccessOutput = true;
        }

        internal void LinkPipelineErrorOutput(Pipe pipeToUse)
        {
            Dbg.Assert(pipeToUse != null, "Caller should verify pipeToUse != null");

            for (int i = 0; i < _commands.Count; i++)
            {
                CommandProcessorBase commandProcessor = _commands[i];
                if (commandProcessor == null
                    || commandProcessor.CommandRuntime == null)
                {
                    // "null command or request or ErrorOutputPipe " + i
                    throw PSTraceSource.NewInvalidOperationException();
                }

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
            // We do not make Dispose() wait until StopProcessing()
            // has completed.
            _stopping = true;

            LogToEventLog();

            if (_commands != null)
            {
                for (int i = 0; i < _commands.Count; i++)
                {
                    CommandProcessorBase commandProcessor = _commands[i];
                    if (commandProcessor != null)
                    {
#pragma warning disable 56500
                        // If Dispose throws an exception, record it as a
                        // pipeline failure and continue disposing cmdlets.
                        try
                        {
                            commandProcessor.CommandRuntime.RemoveVariableListsInPipe();
                            commandProcessor.Dispose();
                        }
                        // 2005/04/13-JonN: The only vaguely plausible reason
                        // for a failure here is an exception in Command.Dispose.
                        // As such, this should be covered by the overall
                        // exemption.
                        catch (Exception e) // Catch-all OK, 3rd party callout.
                        {
                            InvocationInfo myInvocation = null;
                            if (commandProcessor.Command != null)
                                myInvocation = commandProcessor.Command.MyInvocation;

                            ProviderInvocationException pie =
                                e as ProviderInvocationException;
                            if (pie != null)
                            {
                                e = new CmdletProviderInvocationException(
                                    pie,
                                    myInvocation);
                            }
                            else
                            {
                                e = new CmdletInvocationException(
                                    e,
                                    myInvocation);

                                // Log a command health event

                                MshLog.LogCommandHealthEvent(
                                    commandProcessor.Command.Context,
                                    e,
                                    Severity.Warning);
                            }

                            RecordFailure(e, commandProcessor.Command);
                        }
#pragma warning restore 56500
                    }
                }
            }

            _commands = null;

            // Now dispose any pipes that were used for redirection...
            if (_redirectionPipes != null)
            {
                foreach (PipelineProcessor redirPipe in _redirectionPipes)
                {
#pragma warning disable 56500
                    // The complicated logic of disposing the commands is taken care
                    // of through recursion, this routine should not be getting any
                    // exceptions...
                    try
                    {
                        if (redirPipe != null)
                        {
                            redirPipe.Dispose();
                        }
                    }
                    catch (Exception)
                    {
                    }
#pragma warning restore 56500
                }
            }

            _redirectionPipes = null;
        }

        private object _stopReasonLock = new object();
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
                // 905900-2005/05/12
                // Drop5: Error Architecture: Log/trace second and subsequent RecordFailure
                // Note that the pipeline could have been stopped asynchronously
                // before hitting the error, therefore we check whether
                // firstTerminatingError is PipelineStoppedException.
                else if ((!(_firstTerminatingError.SourceException is PipelineStoppedException))
                    && command != null && command.Context != null)
                {
                    Exception ex = e;
                    while ((ex is TargetInvocationException || ex is CmdletInvocationException)
                            && (ex.InnerException != null))
                    {
                        ex = ex.InnerException;
                    }

                    if (!(ex is PipelineStoppedException))
                    {
                        string message = StringUtil.Format(PipelineStrings.SecondFailure,
                            _firstTerminatingError.GetType().Name,
                            _firstTerminatingError.SourceException.StackTrace,
                            ex.GetType().Name,
                            ex.StackTrace
                        );
                        InvalidOperationException ioe
                            = new InvalidOperationException(message, ex);
                        MshLog.LogCommandHealthEvent(
                            command.Context,
                            ioe,
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
            get { return _externalInputPipe; }

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
            get { return _externalSuccessOutput; }

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
            get { return _externalErrorOutput; }

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
            get { return _executionScope; }

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

