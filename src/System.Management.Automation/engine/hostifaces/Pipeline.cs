// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.ObjectModel;
using System.Threading;
using System.Runtime.Serialization;
using Dbg = System.Management.Automation.Diagnostics;
using System.Management.Automation.Internal;

namespace System.Management.Automation.Runspaces
{
    #region Exceptions
    /// <summary>
    /// Defines exception which is thrown when state of the pipeline is different
    /// from expected state.
    /// </summary>
    [Serializable]
    public class InvalidPipelineStateException : SystemException
    {
        /// <summary>
        /// Initializes a new instance of the InvalidPipelineStateException class.
        /// </summary>
        public InvalidPipelineStateException()
            : base(StringUtil.Format(RunspaceStrings.InvalidPipelineStateStateGeneral))
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidPipelineStateException class
        /// with a specified error message.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        public InvalidPipelineStateException(string message)
        : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidPipelineStateException class
        /// with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception.
        /// </param>
        public InvalidPipelineStateException(string message, Exception innerException)
        : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidPipelineStateException and defines value of
        /// CurrentState and ExpectedState.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.
        /// </param>
        /// <param name="currentState">Current state of pipeline.</param>
        /// <param name="expectedState">Expected state of pipeline.</param>
        internal InvalidPipelineStateException(string message, PipelineState currentState, PipelineState expectedState)
        : base(message)
        {
            _expectedState = expectedState;
            _currentState = currentState;
        }

        #region ISerializable Members

        // 2005/04/20-JonN No need to implement GetObjectData
        // if all fields are static or [NonSerialized]

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidPipelineStateException"/>
        ///  class with serialized data.
        /// </summary>
        /// <param name="info">
        /// The <see cref="SerializationInfo"/> that holds the serialized object
        /// data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The <see cref="StreamingContext"/> that contains contextual information
        /// about the source or destination.
        /// </param>
        private InvalidPipelineStateException(SerializationInfo info, StreamingContext context)
        : base(info, context)
        {
        }

        #endregion

        /// <summary>
        /// Gets CurrentState of the pipeline.
        /// </summary>
        public PipelineState CurrentState
        {
            get { return _currentState; }
        }

        /// <summary>
        /// Gets ExpectedState of the pipeline.
        /// </summary>
        public PipelineState ExpectedState
        {
            get { return _expectedState; }
        }

        /// <summary>
        /// State of pipeline when exception was thrown.
        /// </summary>
        [NonSerialized]
        private PipelineState _currentState = 0;

        /// <summary>
        /// States of the pipeline expected in method which throws this exception.
        /// </summary>
        [NonSerialized]
        private PipelineState _expectedState = 0;
    }

    #endregion Exceptions

    #region PipelineState

    /// <summary>
    /// Enumerated type defining the state of the Pipeline.
    /// </summary>
    public enum PipelineState
    {
        /// <summary>
        /// The pipeline has not been started.
        /// </summary>
        NotStarted = 0,
        /// <summary>
        /// The pipeline is executing.
        /// </summary>
        Running = 1,
        /// <summary>
        /// The pipeline is stoping execution.
        /// </summary>
        Stopping = 2,
        /// <summary>
        /// The pipeline is completed due to a stop request.
        /// </summary>
        Stopped = 3,
        /// <summary>
        /// The pipeline has completed.
        /// </summary>
        Completed = 4,
        /// <summary>
        /// The pipeline completed abnormally due to an error.
        /// </summary>
        Failed = 5,
        /// <summary>
        /// The pipeline is disconnected from remote running command.
        /// </summary>
        Disconnected = 6
    }

    /// <summary>
    /// Type which has information about PipelineState and Exception
    /// associated with PipelineState.
    /// </summary>
    public sealed class PipelineStateInfo
    {
        #region constructors

        /// <summary>
        /// Constructor for state changes not resulting from an error.
        /// </summary>
        /// <param name="state">Execution state.</param>
        internal PipelineStateInfo(PipelineState state)
            : this(state, null)
        {
        }

        /// <summary>
        /// Constructor for state changes with an optional error.
        /// </summary>
        /// <param name="state">The new state.</param>
        /// <param name="reason">A non-null exception if the state change was
        /// caused by an error,otherwise; null.
        /// </param>
        internal PipelineStateInfo(PipelineState state, Exception reason)
        {
            State = state;
            Reason = reason;
        }

        /// <summary>
        /// Copy constructor to support cloning.
        /// </summary>
        /// <param name="pipelineStateInfo">Source information.</param>
        /// <throws>
        /// ArgumentNullException when <paramref name="pipelineStateInfo"/> is null.
        /// </throws>
        internal PipelineStateInfo(PipelineStateInfo pipelineStateInfo)
        {
            Dbg.Assert(pipelineStateInfo != null, "caller should validate the parameter");

            State = pipelineStateInfo.State;
            Reason = pipelineStateInfo.Reason;
        }

        #endregion constructors

        #region public_properties

        /// <summary>
        /// The state of the runspace.
        /// </summary>
        /// <remarks>
        /// This value indicates the state of the pipeline after the change.
        /// </remarks>
        public PipelineState State { get; }

        /// <summary>
        /// The reason for the state change, if caused by an error.
        /// </summary>
        /// <remarks>
        /// The value of this property is non-null if the state
        /// changed due to an error. Otherwise, the value of this
        /// property is null.
        /// </remarks>
        public Exception Reason { get; }

        #endregion public_properties

        /// <summary>
        /// Clones this object.
        /// </summary>
        /// <returns>Cloned object.</returns>
        internal PipelineStateInfo Clone()
        {
            return new PipelineStateInfo(this);
        }
    }

    /// <summary>
    /// Event arguments passed to PipelineStateEvent handlers
    /// <see cref="Pipeline.StateChanged"/> event.
    /// </summary>
    public sealed class PipelineStateEventArgs : EventArgs
    {
        #region constructors

        /// <summary>
        /// Constructor PipelineStateEventArgs from PipelineStateInfo.
        /// </summary>
        /// <param name="pipelineStateInfo">The current state of the
        /// pipeline.</param>
        /// <throws>
        /// ArgumentNullException when <paramref name="pipelineStateInfo"/> is null.
        /// </throws>
        internal PipelineStateEventArgs(PipelineStateInfo pipelineStateInfo)
        {
            Dbg.Assert(pipelineStateInfo != null, "caller should validate the parameter");
            PipelineStateInfo = pipelineStateInfo;
        }

        #endregion constructors

        #region public_properties

        /// <summary>
        /// Info about current state of pipeline.
        /// </summary>
        public PipelineStateInfo PipelineStateInfo { get; }

        #endregion public_properties
    }
    #endregion ExecutionState

    /// <summary>
    /// Defines a class which can be used to invoke a pipeline of commands.
    /// </summary>
    public abstract class Pipeline : IDisposable
    {
        #region constructor

        /// <summary>
        /// Explicit default constructor.
        /// </summary>
        internal Pipeline(Runspace runspace)
            : this(runspace, new CommandCollection())
        {
        }

        /// <summary>
        /// Constructor to initialize both Runspace and Command to invoke.
        /// Caller should make sure that "command" is not null.
        /// </summary>
        /// <param name="runspace">
        /// Runspace to use for the command invocation.
        /// </param>
        /// <param name="command">
        /// command to Invoke.
        /// Caller should make sure that "command" is not null.
        /// </param>
        internal Pipeline(Runspace runspace, CommandCollection command)
        {
            if (runspace == null)
            {
                PSTraceSource.NewArgumentNullException("runspace");
            }
            // This constructor is used only internally.
            // Caller should make sure the input is valid
            Dbg.Assert(command != null, "Command cannot be null");
            InstanceId = runspace.GeneratePipelineId();
            Commands = command;

            // Reset the AMSI session so that it is re-initialized
            // when the next script block is parsed.
            AmsiUtils.CloseSession();
        }

        #endregion constructor

        #region properties

        /// <summary>
        /// Gets the runspace this pipeline is created on.
        /// </summary>
        public abstract Runspace Runspace { get; }

        /// <summary>
        /// Gets the property which indicates if this pipeline is nested.
        /// </summary>
        public abstract bool IsNested { get; }

        /// <summary>
        /// Gets the property which indicates if this pipeline is a child pipeline.
        ///
        /// IsChild flag makes it possible for the pipeline to differentiate between
        /// a true v1 nested pipeline and the cmdlets calling cmdlets case. See bug
        /// 211462.
        /// </summary>
        internal virtual bool IsChild
        {
            get { return false; }

            set { }
        }

        /// <summary>
        /// Gets input writer for this pipeline.
        /// </summary>
        /// <remarks>
        /// When the caller calls Input.Write(), the caller writes to the
        /// input of the pipeline.  Thus, <paramref name="Input"/>
        /// is a PipelineWriter or "thing which can be written to".
        /// Note:Input must be closed after Pipeline.InvokeAsync for InvokeAsync to
        /// finish.
        /// </remarks>
        public abstract PipelineWriter Input { get; }

        /// <summary>
        /// Gets the output reader for this pipeline.
        /// </summary>
        /// <remarks>
        /// When the caller calls Output.Read(), the caller reads from the
        /// output of the pipeline.  Thus, <paramref name="Output"/>
        /// is a PipelineReader or "thing which can be read from".
        /// </remarks>
        public abstract PipelineReader<PSObject> Output { get; }

        /// <summary>
        /// Gets the error output reader for this pipeline.
        /// </summary>
        /// <remarks>
        /// When the caller calls Error.Read(), the caller reads from the
        /// output of the pipeline.  Thus, <paramref name="Error"/>
        /// is a PipelineReader or "thing which can be read from".
        ///
        /// This is the non-terminating error stream from the command.
        /// In this release, the objects read from this PipelineReader
        /// are PSObjects wrapping ErrorRecords.
        /// </remarks>
        public abstract PipelineReader<object> Error { get; }

        /// <summary>
        /// Gets Info about current state of the pipeline.
        /// </summary>
        /// <remarks>
        /// This value indicates the state of the pipeline after the change.
        /// </remarks>
        public abstract PipelineStateInfo PipelineStateInfo { get; }

        /// <summary>
        /// True if pipeline execution encountered and error.
        /// It will always be true if _reason is non-null
        /// since an exception occurred. For other error types,
        /// It has to be set manually.
        /// </summary>
        public virtual bool HadErrors
        {
            get { return _hadErrors; }
        }

        private bool _hadErrors;

        internal void SetHadErrors(bool status)
        {
            _hadErrors = _hadErrors || status;
        }

        /// <summary>
        /// Gets the unique identifier for this pipeline. This identifier is unique with in
        /// the scope of Runspace.
        /// </summary>
        public long InstanceId { get; }

        /// <summary>
        /// Gets the collection of commands for this pipeline.
        /// </summary>
        public CommandCollection Commands { get; private set; }

        /// <summary>
        /// If this property is true, SessionState is updated for this
        /// pipeline state.
        /// </summary>
        public bool SetPipelineSessionState { get; set; } = true;

        /// <summary>
        /// Settings for the pipeline invocation thread.
        /// </summary>
        internal PSInvocationSettings InvocationSettings { get; set; }

        /// <summary>
        /// If this flag is true, the commands in this Pipeline will redirect the global error output pipe
        /// (ExecutionContext.ShellFunctionErrorOutputPipe) to the command's error output pipe.
        ///
        /// When the global error output pipe is not set, $ErrorActionPreference is not checked and all
        /// errors are treated as terminating errors.
        ///
        /// On V1, the global error output pipe is redirected to the command's error output pipe only when
        /// it has already been redirected. The command-line host achieves this redirection by merging the
        /// error output into the output pipe so it checks $ErrorActionPreference all right. However, when
        /// the Pipeline class is used programmatically the global error output pipe is not set and the first
        /// error terminates the pipeline.
        ///
        /// This flag is used to force the redirection. By default it is false to maintain compatibility with
        /// V1, but the V2 hosting interface (PowerShell class) sets this flag to true to ensure the global
        /// error output pipe is always set and $ErrorActionPreference when invoking the Pipeline.
        /// </summary>
        internal bool RedirectShellErrorOutputPipe { get; set; } = false;

        #endregion properties

        #region events

        /// <summary>
        /// Event raised when Pipeline's state changes.
        /// </summary>
        public abstract event EventHandler<PipelineStateEventArgs> StateChanged;

        #endregion events

        #region methods

        /// <summary>
        /// Invoke the pipeline, synchronously, returning the results as an array of
        /// objects.
        /// </summary>
        /// <remarks>If using synchronous invoke, do not close
        /// input objectWriter. Synchronous invoke will always close the input
        /// objectWriter.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// No command is added to pipeline
        /// </exception>
        /// <exception cref="InvalidPipelineStateException">
        /// PipelineState is not NotStarted.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// 1) A pipeline is already executing. Pipeline cannot execute
        /// concurrently.
        /// 2) Attempt is made to invoke a nested pipeline directly. Nested
        /// pipeline must be invoked from a running pipeline.
        /// </exception>
        /// <exception cref="InvalidRunspaceStateException">
        /// RunspaceState is not Open
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Pipeline already disposed
        /// </exception>
        /// <exception cref="ScriptCallDepthException">
        /// The script recursed too deeply into script functions.
        /// There is a fixed limit on the depth of recursion.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// A CLR security violation occurred.  Typically, this happens
        /// because the current CLR permissions do not allow adequate
        /// reflection access to a cmdlet assembly.
        /// </exception>
        /// <exception cref="ThreadAbortException">
        /// The thread in which the pipeline was executing was aborted.
        /// </exception>
        /// <exception cref="RuntimeException">
        /// Pipeline.Invoke can throw a variety of exceptions derived
        /// from RuntimeException. The most likely of these exceptions
        /// are listed below.
        /// </exception>
        /// <exception cref="ParameterBindingException">
        /// One of more parameters or parameter values specified for
        /// a cmdlet are not valid, or mandatory parameters for a cmdlet
        /// were not specified.
        /// </exception>
        /// <exception cref="CmdletInvocationException">
        /// A cmdlet generated a terminating error.
        /// </exception>
        /// <exception cref="CmdletProviderInvocationException">
        /// A provider generated a terminating error.
        /// </exception>
        /// <exception cref="ActionPreferenceStopException">
        /// The ActionPreference.Stop or ActionPreference.Inquire policy
        /// triggered a terminating error.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// The pipeline was terminated asynchronously.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If there is an error generating the metadata for dynamic parameters.
        /// </exception>
        public Collection<PSObject> Invoke()
        {
            return Invoke(null);
        }

        /// <summary>
        /// Invoke the pipeline, synchronously, returning the results as an array of objects.
        /// </summary>
        /// <param name="input">an array of input objects to pass to the pipeline.
        /// Array may be empty but may not be null</param>
        /// <returns>An array of zero or more result objects.</returns>
        /// <remarks>If using synchronous exectute, do not close
        /// input objectWriter. Synchronous invoke will always close the input
        /// objectWriter.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// No command is added to pipeline
        /// </exception>
        /// <exception cref="InvalidPipelineStateException">
        /// PipelineState is not NotStarted.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// 1) A pipeline is already executing. Pipeline cannot execute
        /// concurrently.
        /// 2) Attempt is made to invoke a nested pipeline directly. Nested
        /// pipeline must be invoked from a running pipeline.
        /// </exception>
        /// <exception cref="InvalidRunspaceStateException">
        /// RunspaceState is not Open
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Pipeline already disposed
        /// </exception>
        /// <exception cref="ScriptCallDepthException">
        /// The script recursed too deeply into script functions.
        /// There is a fixed limit on the depth of recursion.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// A CLR security violation occurred.  Typically, this happens
        /// because the current CLR permissions do not allow adequate
        /// reflection access to a cmdlet assembly.
        /// </exception>
        /// <exception cref="ThreadAbortException">
        /// The thread in which the pipeline was executing was aborted.
        /// </exception>
        /// <exception cref="RuntimeException">
        /// Pipeline.Invoke can throw a variety of exceptions derived
        /// from RuntimeException. The most likely of these exceptions
        /// are listed below.
        /// </exception>
        /// <exception cref="ParameterBindingException">
        /// One of more parameters or parameter values specified for
        /// a cmdlet are not valid, or mandatory parameters for a cmdlet
        /// were not specified.
        /// </exception>
        /// <exception cref="CmdletInvocationException">
        /// A cmdlet generated a terminating error.
        /// </exception>
        /// <exception cref="CmdletProviderInvocationException">
        /// A provider generated a terminating error.
        /// </exception>
        /// <exception cref="ActionPreferenceStopException">
        /// The ActionPreference.Stop or ActionPreference.Inquire policy
        /// triggered a terminating error.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// The pipeline was terminated asynchronously.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If there is an error generating the metadata for dynamic parameters.
        /// </exception>
        public abstract Collection<PSObject> Invoke(IEnumerable input);

        /// <summary>
        /// Invoke the pipeline asynchronously.
        /// </summary>
        /// <remarks>
        /// 1) Results are returned through the <see cref="Pipeline.Output"/> reader.
        /// 2) When pipeline is invoked using InvokeAsync, invocation doesn't
        /// finish until Input to pipeline is closed. Caller of InvokeAsync must close
        /// the input pipe after all input has been written to input pipe. Input pipe
        /// is closed by calling Pipeline.Input.Close();
        ///
        /// If you want this pipeline to execute as a standalone command
        /// (that is, using command-line parameters only),
        /// be sure to call Pipeline.Input.Close() before calling
        /// InvokeAsync().  Otherwise, the command will be executed
        /// as though it had external input.  If you observe that the
        /// command isn't doing anything, this may be the reason.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// No command is added to pipeline
        /// </exception>
        /// <exception cref="InvalidPipelineStateException">
        /// PipelineState is not NotStarted.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// 1) A pipeline is already executing. Pipeline cannot execute
        /// concurrently.
        /// 2) InvokeAsync is called on nested pipeline. Nested pipeline
        /// cannot be executed Asynchronously.
        /// </exception>
        /// <exception cref="InvalidRunspaceStateException">
        /// RunspaceState is not Open
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Pipeline already disposed
        /// </exception>
        public abstract void InvokeAsync();

        /// <summary>
        /// Synchronous call to stop the running pipeline.
        /// </summary>
        public abstract void Stop();

        /// <summary>
        /// Asynchronous call to stop the running pipeline.
        /// </summary>
        public abstract void StopAsync();

        /// <summary>
        /// Creates a new <see cref="Pipeline"/> that is a copy of the current instance.
        /// </summary>
        /// <returns>A new <see cref="Pipeline"/> that is a copy of this instance.</returns>
        public abstract Pipeline Copy();

        /// <summary>
        /// Connects synchronously to a running command on a remote server.
        /// The pipeline object must be in the disconnected state.
        /// </summary>
        /// <returns>A collection of result objects.</returns>
        public abstract Collection<PSObject> Connect();

        /// <summary>
        /// Connects asynchronously to a running command on a remote server.
        /// </summary>
        public abstract void ConnectAsync();

        /// <summary>
        /// Sets the command collection.
        /// </summary>
        /// <param name="commands">Command collection to set.</param>
        /// <remarks>called by ClientRemotePipeline</remarks>
        internal void SetCommandCollection(CommandCollection commands)
        {
            Commands = commands;
        }

        /// <summary>
        /// Sets the history string to the one that is specified.
        /// </summary>
        /// <param name="historyString">History string to set.</param>
        internal abstract void SetHistoryString(string historyString);

        /// <summary>
        /// Invokes a remote command and immediately disconnects if
        /// transport layer supports it.
        /// </summary>
        internal abstract void InvokeAsyncAndDisconnect();

        #endregion methods

        #region Remote data drain/block methods

        /// <summary>
        /// Blocks data arriving from remote session.
        /// </summary>
        internal virtual void SuspendIncomingData()
        {
            throw new PSNotImplementedException();
        }

        /// <summary>
        /// Resumes data arrive from remote session.
        /// </summary>
        internal virtual void ResumeIncomingData()
        {
            throw new PSNotImplementedException();
        }

        /// <summary>
        /// Blocking call that waits until the current remote data
        /// queue is empty.
        /// </summary>
        internal virtual void DrainIncomingData()
        {
            throw new PSNotImplementedException();
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Disposes the pipeline. If pipeline is running, dispose first
        /// stops the pipeline.
        /// </summary>
        public
        void
        Dispose()
        {
            Dispose(!IsChild);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose which can be overridden by derived classes.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual
        void
        Dispose(bool disposing)
        {
        }

        #endregion IDisposable Members
    }
}

