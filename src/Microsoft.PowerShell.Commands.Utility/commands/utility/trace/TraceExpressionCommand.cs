// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Threading;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A cmdlet that traces the specified categories and flags for the duration of the
    /// specified expression.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Trace, "Command", DefaultParameterSetName = "expressionSet", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097136")]
    public class TraceCommandCommand : TraceListenerCommandBase, IDisposable
    {
        #region Parameters

        /// <summary>
        /// This parameter specifies the current pipeline object.
        /// </summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject { get; set; } = AutomationNull.Value;

        /// <summary>
        /// The TraceSource parameter determines which TraceSource categories the
        /// operation will take place on.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string[] Name
        {
            get { return base.NameInternal; }

            set { base.NameInternal = value; }
        }

        /// <summary>
        /// The flags to be set on the TraceSource.
        /// </summary>
        /// <value></value>
        [Parameter(Position = 2)]
        public PSTraceSourceOptions Option
        {
            get
            {
                return base.OptionsInternal;
            }

            set
            {
                base.OptionsInternal = value;
            }
        }

        /// <summary>
        /// The parameter for the expression that should be traced.
        /// </summary>
        /// <value></value>
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = "expressionSet")]
        public ScriptBlock Expression { get; set; }

        /// <summary>
        /// The parameter for the expression that should be traced.
        /// </summary>
        /// <value></value>
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = "commandSet")]
        public string Command { get; set; }

        /// <summary>
        /// When set, this parameter is the arguments to pass to the command specified by
        /// the -Command parameter.
        /// </summary>
        [Parameter(ParameterSetName = "commandSet", ValueFromRemainingArguments = true)]
        [Alias("Args")]
        public object[] ArgumentList { get; set; }

        /// <summary>
        /// The parameter which determines the options for output from the trace listeners.
        /// </summary>
        [Parameter]
        public TraceOptions ListenerOption
        {
            get
            {
                return base.ListenerOptionsInternal;
            }

            set
            {
                base.ListenerOptionsInternal = value;
            }
        }

        /// <summary>
        /// Adds the file trace listener using the specified file.
        /// </summary>
        /// <value></value>
        [Parameter]
        [Alias("PSPath", "Path")]
        public string FilePath
        {
            get { return base.FileListener; }

            set { base.FileListener = value; }
        }

        /// <summary>
        /// Force parameter to control read-only files.
        /// </summary>
        [Parameter]
        public SwitchParameter Force
        {
            get { return base.ForceWrite; }

            set { base.ForceWrite = value; }
        }

        /// <summary>
        /// If this parameter is specified the Debugger trace listener will be added.
        /// </summary>
        /// <value></value>
        [Parameter]
        public SwitchParameter Debugger
        {
            get { return base.DebuggerListener; }

            set { base.DebuggerListener = value; }
        }

        /// <summary>
        /// If this parameter is specified the Msh Host trace listener will be added.
        /// </summary>
        /// <value></value>
        [Parameter]
        public SwitchParameter PSHost
        {
            get { return base.PSHostListener; }

            set { base.PSHostListener = value; }
        }

        #endregion Parameters

        #region Cmdlet code

        private Collection<PSTraceSource> _matchingSources;

        /// <summary>
        /// Gets the PSTraceSource instances that match the names specified.
        /// </summary>
        protected override void BeginProcessing()
        {
            Collection<PSTraceSource> preconfiguredSources = null;
            _matchingSources = ConfigureTraceSource(base.NameInternal, false, out preconfiguredSources);

            TurnOnTracing(_matchingSources, false);
            TurnOnTracing(preconfiguredSources, true);

            // Now that tracing has been configured, move all the sources into a
            // single collection

            foreach (PSTraceSource preconfiguredSource in preconfiguredSources)
            {
                _matchingSources.Add(preconfiguredSource);
            }

            if (ParameterSetName == "commandSet")
            {
                // Create the CommandProcessor and add it to a pipeline

                CommandProcessorBase commandProcessor =
                    this.Context.CommandDiscovery.LookupCommandProcessor(Command, CommandOrigin.Runspace, false);

                // Add the parameters that were specified

                ParameterBinderController.AddArgumentsToCommandProcessor(commandProcessor, ArgumentList);

                _pipeline = new PipelineProcessor();
                _pipeline.Add(commandProcessor);

                // Hook up the success and error pipelines to this cmdlet's WriteObject and
                // WriteError methods

                _pipeline.ExternalErrorOutput = new TracePipelineWriter(this, true, _matchingSources);
                _pipeline.ExternalSuccessOutput = new TracePipelineWriter(this, false, _matchingSources);
            }

            ResetTracing(_matchingSources);
        }

        /// <summary>
        /// Executes the expression.
        /// Note, this was taken from apply-expression.
        /// </summary>
        protected override void ProcessRecord()
        {
            TurnOnTracing(_matchingSources, false);

            object result = null;
            switch (ParameterSetName)
            {
                case "expressionSet":
                    result = RunExpression();
                    break;

                case "commandSet":
                    result = StepCommand();
                    break;
            }

            ResetTracing(_matchingSources);

            if (result == null)
            {
                return;
            }

            if (!LanguagePrimitives.IsNull(result))
            {
                WriteObject(result, true);
            }
        }

        /// <summary>
        /// Finishes running the command if specified and then sets the
        /// tracing options and listeners back to their original values.
        /// </summary>
        protected override void EndProcessing()
        {
            if (_pipeline != null)
            {
                TurnOnTracing(_matchingSources, false);

                Array results = _pipeline.SynchronousExecuteEnumerate(AutomationNull.Value);

                ResetTracing(_matchingSources);

                WriteObject(results, true);
            }

            this.Dispose();
        }

        /// <summary>
        /// Ensures that the sub-pipeline we created gets stopped as well.
        /// </summary>
        protected override void StopProcessing() => _pipeline?.Stop();

        #endregion Cmdlet code

        private object RunExpression()
        {
            return Expression.DoInvokeReturnAsIs(
                useLocalScope: false,
                errorHandlingBehavior: ScriptBlock.ErrorHandlingBehavior.WriteToCurrentErrorPipe,
                dollarUnder: InputObject,
                input: new object[] { InputObject },
                scriptThis: AutomationNull.Value,
                args: Array.Empty<object>());
        }

        private object StepCommand()
        {
            if (InputObject != AutomationNull.Value)
            {
                _pipeline.Step(InputObject);
            }

            return null;
        }

        private PipelineProcessor _pipeline;

        #region IDisposable

        /// <summary>
        /// Resets the TraceSource flags back to their original value and restores
        /// the original TraceListeners.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                // Reset the flags for the trace switch back to the original value
                ResetTracing(_matchingSources);
                ClearStoredState();
                _matchingSources = null;

                if (_pipeline != null)
                {
                    _pipeline.Dispose();
                    _pipeline = null;
                }

                // If there are any file streams, close those as well.

                if (this.FileStreams != null)
                {
                    foreach (FileStream fileStream in this.FileStreams)
                    {
                        fileStream.Flush();
                        fileStream.Dispose();
                    }
                }

                GC.SuppressFinalize(this);
            }
        }

        private bool _disposed;
        #endregion IDisposable
    }

    /// <summary>
    /// This class acts a pipe redirector for the sub-pipeline created by the Trace-Command
    /// cmdlet.  It gets attached to the sub-pipelines success or error pipeline and redirects
    /// all objects written to these pipelines to trace-command pipeline.
    /// </summary>
    internal class TracePipelineWriter : PipelineWriter
    {
        internal TracePipelineWriter(
            TraceListenerCommandBase cmdlet,
            bool writeError,
            Collection<PSTraceSource> matchingSources)
        {
            ArgumentNullException.ThrowIfNull(cmdlet); 
            ArgumentNullException.ThrowIfNull(matchingSources);

            _cmdlet = cmdlet;
            _writeError = writeError;
            _matchingSources = matchingSources;
        }

        /// <summary>
        /// Get the wait handle signaled when buffer space is available in the underlying stream.
        /// </summary>
        public override WaitHandle WaitHandle
        {
            get { return null; }
        }

        /// <summary>
        /// Check if the stream is open for further writes.
        /// </summary>
        /// <value>true if the underlying stream is open, otherwise; false.</value>
        /// <remarks>
        /// Attempting to write to the underlying stream if IsOpen is false throws
        /// an <see cref="ObjectDisposedException"/>.
        /// </remarks>
        public override bool IsOpen
        {
            get
            {
                return _isOpen;
            }
        }

        /// <summary>
        /// Returns the number of objects in the underlying stream.
        /// </summary>
        public override int Count
        {
            get { return 0; }
        }

        /// <summary>
        /// Get the capacity of the stream.
        /// </summary>
        /// <value>
        /// The capacity of the stream.
        /// </value>
        /// <remarks>
        /// The capacity is the number of objects that stream may contain at one time.  Once this
        /// limit is reached, attempts to write into the stream block until buffer space
        /// becomes available.
        /// </remarks>
        public override int MaxCapacity
        {
            get { return int.MaxValue; }
        }

        /// <summary>
        /// Close the stream.
        /// </summary>
        /// <remarks>
        /// Causes subsequent calls to IsOpen to return false and calls to
        /// a write operation to throw an ObjectDisposedException.
        /// All calls to Close() after the first call are silently ignored.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// The stream is already disposed.
        /// </exception>
        public override void Close()
        {
            if (_isOpen)
            {
                Flush();
                _isOpen = false;
            }
        }

        /// <summary>
        /// Flush the data from the stream.  Closed streams may be flushed,
        /// but disposed streams may not.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// The underlying stream is disposed.
        /// </exception>
        public override void Flush()
        {
        }

        /// <summary>
        /// Write a single object into the underlying stream.
        /// </summary>
        /// <param name="obj">The object to add to the stream.</param>
        /// <returns>
        /// One, if the write was successful, otherwise;
        /// zero if the stream was closed before the object could be written,
        /// or if the object was AutomationNull.Value.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// The underlying stream is closed.
        /// </exception>
        public override int Write(object obj)
        {
            _cmdlet.ResetTracing(_matchingSources);

            if (_writeError)
            {
                ErrorRecord errorRecord = ConvertToErrorRecord(obj);

                if (errorRecord != null)
                {
                    _cmdlet.WriteError(errorRecord);
                }
            }
            else
            {
                _cmdlet.WriteObject(obj);
            }

            _cmdlet.TurnOnTracing(_matchingSources, false);
            return 1;
        }

        /// <summary>
        /// Write objects to the underlying stream.
        /// </summary>
        /// <param name="obj">Object or enumeration to read from.</param>
        /// <param name="enumerateCollection">
        /// If enumerateCollection is true, and <paramref name="obj"/>
        /// is an enumeration according to LanguagePrimitives.GetEnumerable,
        /// the objects in the enumeration will be unrolled and
        /// written separately.  Otherwise, <paramref name="obj"/>
        /// will be written as a single object.
        /// </param>
        /// <returns>The number of objects written.</returns>
        /// <exception cref="ObjectDisposedException">
        /// The underlying stream is closed.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="obj"/> contains AutomationNull.Value
        /// </exception>
        public override int Write(object obj, bool enumerateCollection)
        {
            _cmdlet.ResetTracing(_matchingSources);

            int numWritten = 0;
            if (_writeError)
            {
                if (enumerateCollection)
                {
                    foreach (object o in LanguagePrimitives.GetEnumerable(obj))
                    {
                        ErrorRecord errorRecord = ConvertToErrorRecord(o);
                        if (errorRecord != null)
                        {
                            numWritten++;
                            _cmdlet.WriteError(errorRecord);
                        }
                    }
                }
                else
                {
                    ErrorRecord errorRecord = ConvertToErrorRecord(obj);
                    if (errorRecord != null)
                    {
                        numWritten++;
                        _cmdlet.WriteError(errorRecord);
                    }
                }
            }
            else
            {
                numWritten++;
                _cmdlet.WriteObject(obj, enumerateCollection);
            }

            _cmdlet.TurnOnTracing(_matchingSources, false);

            return numWritten;
        }

        private static ErrorRecord ConvertToErrorRecord(object obj)
        {
            ErrorRecord result = null;
            PSObject mshobj = obj as PSObject;
            if (mshobj != null)
            {
                object baseObject = mshobj.BaseObject;
                if (baseObject is not PSCustomObject)
                {
                    obj = baseObject;
                }
            }

            ErrorRecord errorRecordResult = obj as ErrorRecord;
            if (errorRecordResult != null)
            {
                result = errorRecordResult;
            }

            return result;
        }

        private readonly TraceListenerCommandBase _cmdlet;
        private readonly bool _writeError;
        private bool _isOpen = true;
        private readonly Collection<PSTraceSource> _matchingSources = new();
    }
}
