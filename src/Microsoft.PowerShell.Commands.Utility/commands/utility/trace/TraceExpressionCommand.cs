/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Threading;
using Dbg=System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A cmdlet that traces the specified categories and flags for the duration of the 
    /// specified expression.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Trace, "Command", DefaultParameterSetName = "expressionSet", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113419")]
    public class TraceCommandCommand : TraceListenerCommandBase, IDisposable
    {
        #region Parameters

        /// <summary>
        /// This parameter specifies the current pipeline object 
        /// </summary>
        [Parameter (ValueFromPipeline = true)]
        public PSObject InputObject
        {
            set { _inputObject = value; }
            get { return _inputObject; }
        }
        private PSObject _inputObject = AutomationNull.Value;

        /// <summary>
        /// The TraceSource parameter determines which TraceSource categories the
        /// operation will take place on.
        /// </summary>
        /// 
        [Parameter(Position = 0, Mandatory = true)]
        public string[] Name
        {
            get { return base.NameInternal; }
            set { base.NameInternal = value; }
        }

        /// <summary>
        /// The flags to be set on the TraceSource
        /// </summary>
        /// <value></value>
        [Parameter(Position = 2)]
        public PSTraceSourceOptions Option
        {
            get { return base.OptionsInternal; }
            set
            {
                base.OptionsInternal = value;
            }
        } // Options

        /// <summary>
        /// The parameter for the expression that should be traced.
        /// </summary>
        /// <value></value>
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = "expressionSet")]
        public ScriptBlock Expression
        {
            get { return expression; }
            set { expression = value; }
        }
        private ScriptBlock expression;

        /// <summary>
        /// The parameter for the expression that should be traced.
        /// </summary>
        /// <value></value>
        [Parameter(Position = 1, Mandatory = true, ParameterSetName  = "commandSet")]
        public string Command
        {
            get { return command; }
            set { command = value; }
        }
        private string command;

        /// <summary>
        /// When set, this parameter is the arguments to pass to the command specified by
        /// the -Command parameter
        /// </summary>
        [Parameter(ParameterSetName  = "commandSet", ValueFromRemainingArguments = true)]
        [Alias("Args")]
        public object[] ArgumentList
        {
            get { return commandArgs; }
            set { commandArgs = value; }
        }
        private object[] commandArgs;

        /// <summary>
        /// The parameter which determines the options for output from the
        /// trace listeners.
        /// </summary>
        /// 
        [Parameter]
        public TraceOptions ListenerOption
        {
            get { return base.ListenerOptionsInternal; }
            set
            {
                base.ListenerOptionsInternal = value;
            }
        }

        /// <summary>
        /// Adds the file trace listener using the specified file
        /// </summary>
        /// <value></value>
        [Parameter]
        [Alias("PSPath")]
        public string FilePath
        {
            get { return base.FileListener; }
            set { base.FileListener = value; }
        } // File

        /// <summary>
        /// Force parameter to control read-only files
        /// </summary>
        [Parameter]
        public SwitchParameter Force
        {
            get { return base.ForceWrite; }
            set { base.ForceWrite = value; }
        }

        /// <summary>
        /// If this parameter is specified the Debugger trace listener
        /// will be added.
        /// </summary>
        /// <value></value>
        [Parameter]
        public SwitchParameter Debugger
        {
            get { return base.DebuggerListener; }
            set { base.DebuggerListener = value; }
        } // Debugger

        /// <summary>
        /// If this parameter is specified the Msh Host trace listener
        /// will be added.
        /// </summary>
        /// <value></value>
        [Parameter]
        public SwitchParameter PSHost
        {
            get { return base.PSHostListener; }
            set { base.PSHostListener = value; }
        } // PSHost


        #endregion Parameters

        #region Cmdlet code

        private Collection<PSTraceSource> matchingSources;

        /// <summary>
        /// Gets the PSTraceSource instances that match the names specified.
        /// </summary>
        protected override void BeginProcessing()
        {
            Collection<PSTraceSource> preconfiguredSources = null;
            matchingSources = ConfigureTraceSource(base.NameInternal, false, out preconfiguredSources);


            TurnOnTracing(matchingSources, false);
            TurnOnTracing(preconfiguredSources, true);

            // Now that tracing has been configured, move all the sources into a 
            // single collection

            foreach (PSTraceSource preconfiguredSource in preconfiguredSources)
            {
                matchingSources.Add(preconfiguredSource);
            }

            if (ParameterSetName == "commandSet")
            {

                // Create the CommmandProcessor and add it to a pipeline

                CommandProcessorBase commandProcessor =
                    this.Context.CommandDiscovery.LookupCommandProcessor(command, CommandOrigin.Runspace, false);

                // Add the parameters that were specified

                ParameterBinderController.AddArgumentsToCommandProcessor(commandProcessor, ArgumentList);
               
                pipeline = new PipelineProcessor();
                pipeline.Add(commandProcessor);

                // Hook up the success and error pipelines to this cmdlet's WriteObject and
                // WriteError methods

                pipeline.ExternalErrorOutput = new TracePipelineWriter(this, true, matchingSources);
                pipeline.ExternalSuccessOutput = new TracePipelineWriter(this, false, matchingSources);

            }
            ResetTracing(matchingSources);
        }

        /// <summary>
        /// Executes the expression.
        /// 
        /// Note, this was taken from apply-expression
        /// </summary>
        protected override void ProcessRecord ()
        {
            TurnOnTracing (matchingSources, false);

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
            ResetTracing (matchingSources);

            if (result == null)
            {
                return;
            }


            if (!LanguagePrimitives.IsNull(result))
            {
                WriteObject(result, true);
            }

        } // ProcessRecord

        
        /// <summary>
        /// Finishes running the command if specified and then sets the
        /// tracing options and listeners back to their original values.
        /// </summary>
        protected override void EndProcessing ()
        {
            if (pipeline != null)
            {
                TurnOnTracing(matchingSources, false);

                Array results = pipeline.SynchronousExecuteEnumerate(AutomationNull.Value);

                ResetTracing(matchingSources);
                
                WriteObject(results, true);

            }
            this.Dispose ();
        }

        /// <summary>
        /// Ensures that the sub-pipeline we created gets stopped as well.
        /// </summary>
        /// 
        protected override void StopProcessing()
        {
            if (pipeline != null)
            {
                pipeline.Stop();
            }
        }

        #endregion Cmdlet code

        private object RunExpression()
        {
            return expression.DoInvokeReturnAsIs(
                useLocalScope:         false,
                errorHandlingBehavior: ScriptBlock.ErrorHandlingBehavior.WriteToCurrentErrorPipe, 
                dollarUnder:           InputObject,
                input:                 new object[] {InputObject},
                scriptThis:            AutomationNull.Value,
                args:                  Utils.EmptyArray<object>());
        }

        private object StepCommand()
        {
            if (InputObject != AutomationNull.Value)
            {
                pipeline.Step(InputObject);
            }
            return null;
        }

        private PipelineProcessor pipeline;

        #region IDisposable

        /// <summary>
        /// Resets the TraceSource flags back to their original value and restores
        /// the original TraceListeners.
        /// </summary>
        public void Dispose ()
        {
            if (!disposed)
            {
                disposed = true;

                // Reset the flags for the trace switch back to the original value
                ResetTracing (matchingSources);
                ClearStoredState();
                matchingSources = null;

                if (pipeline != null)
                {
                    pipeline.Dispose();
                    pipeline = null;
                }

                // If there are any file streams, close those as well.

                if (this.FileStreams != null)
                {
                    foreach (FileStream fileStream in this.FileStreams)
                    {
                        fileStream.Flush();
                        fileStream.Close();
                    }
                }
                GC.SuppressFinalize(this);
            }
            
        } // Dispose
        private bool disposed;
        #endregion IDisposable
    }

    /// <summary>
    /// This class acts a pipe redirector for the sub-pipeline created by the Trace-Command
    /// cmdlet.  It gets attached to the sub-pipelines success or error pipeline and redirects
    /// all objects written to these pipelines to trace-command pipeline.
    /// </summary>
    /// 
    internal class TracePipelineWriter : PipelineWriter
    {
        internal TracePipelineWriter(
            TraceListenerCommandBase cmdlet, 
            bool writeError, 
            Collection<PSTraceSource> matchingSources)
        {
            if (cmdlet == null)
            {
                throw new ArgumentNullException("cmdlet");
            }

            if (matchingSources == null)
            {
                throw new ArgumentNullException("matchingSources");
            }

            this.cmdlet = cmdlet;
            this.writeError = writeError;
            this.matchingSources = matchingSources;
        }

        /// <summary>
        /// Get the wait handle signaled when buffer space is available
        /// in the underlying stream.
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
                return isOpen;
            }
        }

        /// <summary>
        /// Returns the number of objects in the underlying stream
        /// </summary>
        public override int Count
        {
            get { return 0; }
        }

        /// <summary>
        /// Get the capacity of the stream
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
        /// Close the stream
        /// </summary>
        /// <remarks>
        /// Causes subsequent calls to IsOpen to return false and calls to
        /// a write operation to throw an ObjectDisposedException.
        /// All calls to Close() after the first call are silently ignored.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// The stream is already disposed
        /// </exception>
        public override void Close()
        {
            if (isOpen)
            {
                Flush();
                isOpen = false;
            }
        }

        /// <summary>
        /// Flush the data from the stream.  Closed streams may be flushed,
        /// but disposed streams may not.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// The underlying stream is disposed
        /// </exception>
        public override void Flush()
        {
        }

        /// <summary>
        /// Write a single object into the underlying stream
        /// </summary>
        /// <param name="obj">The object to add to the stream</param>
        /// <returns>
        /// One, if the write was successful, otherwise;
        /// zero if the stream was closed before the object could be written,
        /// or if the object was AutomationNull.Value.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// The underlying stream is closed
        /// </exception>
        public override int Write(object obj)
        {
            cmdlet.ResetTracing(matchingSources);

            if (writeError)
            {
                ErrorRecord errorRecord = ConvertToErrorRecord(obj);

                if (errorRecord != null)
                {
                    cmdlet.WriteError(errorRecord);
                }
            }
            else
            {
                cmdlet.WriteObject(obj);
            }

            cmdlet.TurnOnTracing(matchingSources, false);
            return 1;
        }

        /// <summary>
        /// Write objects to the underlying stream
        /// </summary>
        /// <param name="obj">object or enumeration to read from</param>
        /// <param name="enumerateCollection">
        /// If enumerateCollection is true, and <paramref name="obj"/>
        /// is an enumeration according to LanguagePrimitives.GetEnumerable,
        /// the objects in the enumeration will be unrolled and
        /// written seperately.  Otherwise, <paramref name="obj"/>
        /// will be written as a single object.
        /// </param>
        /// <returns>The number of objects written</returns>
        /// <exception cref="ObjectDisposedException">
        /// The underlying stream is closed
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="obj"/> contains AutomationNull.Value
        /// </exception>
        public override int Write(object obj, bool enumerateCollection)
        {
            cmdlet.ResetTracing(matchingSources);

            int numWritten = 0;
            if (writeError)
            {
                if (enumerateCollection)
                {
                    foreach (object o in LanguagePrimitives.GetEnumerable(obj))
                    {
                        ErrorRecord errorRecord = ConvertToErrorRecord(o);
                        if (errorRecord != null)
                        {
                            numWritten++;
                            cmdlet.WriteError(errorRecord);
                        }
                    }
                }
                else
                {
                    ErrorRecord errorRecord = ConvertToErrorRecord(obj);
                    if (errorRecord != null)
                    {
                        numWritten++;
                        cmdlet.WriteError(errorRecord);
                    }
                }
            }
            else
            {
                numWritten++;
                cmdlet.WriteObject(obj, enumerateCollection);
            }

            cmdlet.TurnOnTracing(matchingSources, false);

            return numWritten;
        }

        private static ErrorRecord ConvertToErrorRecord(object obj)
        {
            ErrorRecord result = null;
            PSObject mshobj = obj as PSObject;
            if (mshobj != null)
            {
                object baseObject = mshobj.BaseObject;
                if (!(baseObject is PSCustomObject))
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

        private TraceListenerCommandBase cmdlet;
        private bool writeError;
        private bool isOpen = true;
        private Collection<PSTraceSource> matchingSources = new Collection<PSTraceSource>();
    }
}
