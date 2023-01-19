// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Internal.Host;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// Monad internal implementation of the ICommandRuntime2 interface
    /// used for execution in the monad engine environment.
    ///
    /// There will be one instance of this class for each cmdlet added to
    /// a pipeline. When the cmdlet calls its WriteObject API, that API will call
    /// the WriteObject implementation in this class which, in turn, calls
    /// the downstream cmdlet.
    /// </summary>
    internal class MshCommandRuntime : ICommandRuntime2
    {
        #region private_members

        /// <summary>
        /// Gets/Set the execution context value for this runtime object.
        /// </summary>
        internal ExecutionContext Context { get; set; }

        private SessionState _state = null;
        internal InternalHost CBhost;

        /// <summary>
        /// The host object for this object.
        /// </summary>
        public PSHost Host { get; }

        // Output pipes.
        private Pipe _inputPipe;
        private Pipe _outputPipe;
        private Pipe _errorOutputPipe;

        /// <summary>
        /// IsClosed indicates to the Cmdlet whether its upstream partner
        /// could still write more data to its incoming queue.
        /// Note that there may still be data in the incoming queue.
        /// </summary>
        internal bool IsClosed { get; set; }

        /// <summary>
        /// True if we're not closed and the input pipe is non-null...
        /// </summary>
        internal bool IsPipelineInputExpected
        {
            get
            {
                // No objects in the input pipe
                // The pipe is closed. So there can't be any more object
                if (IsClosed && (_inputPipe == null || _inputPipe.Empty))
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// This allows all success output to be set to a variable.  Similar to the way -errorvariable sets
        /// all errors to a variable name.  Semantically this is equivalent to :  cmd |set-var varname -passthru
        /// but it should be MUCH faster as there is no binding that takes place.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">
        /// may not be set to null
        /// </exception>
        /// <remarks>
        /// This is a common parameter via class CommonParameters.
        /// </remarks>
        internal string OutVariable { get; set; }

        internal IList OutVarList { get { return _outVarList; } set { _outVarList = value; } }

        private IList _outVarList = null;

        internal PipelineProcessor PipelineProcessor { get; set; }

        private readonly CommandInfo _commandInfo;
        private readonly InternalCommand _thisCommand;

        #endregion private_members

        internal MshCommandRuntime(ExecutionContext context, CommandInfo commandInfo, InternalCommand thisCommand)
        {
            Context = context;
            Host = context.EngineHostInterface;
            this.CBhost = (InternalHost)context.EngineHostInterface;
            _commandInfo = commandInfo;
            _thisCommand = thisCommand;
            LogPipelineExecutionDetail = InitShouldLogPipelineExecutionDetail();
        }

        /// <summary>
        /// For diagnostic purposes.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (_commandInfo != null)
                return _commandInfo.ToString();
            return "<NullCommandInfo>"; // does not require localization
        }

        private InvocationInfo _myInvocation;
        /// <summary>
        /// Return the invocation data object for this command.
        /// </summary>
        /// <value>The invocation object for this command.</value>
        internal InvocationInfo MyInvocation
        {
            get { return _myInvocation ??= _thisCommand.MyInvocation; }
        }

        /// <summary>
        /// Internal helper. Indicates whether stop has been requested on this command.
        /// </summary>
        internal bool IsStopping
        {
            get { return (this.PipelineProcessor != null && this.PipelineProcessor.Stopping); }
        }

        #region Write

        // Trust: WriteObject needs to respect EmitTrustCategory

        /// <summary>
        /// Writes the object to the output pipe.
        /// </summary>
        /// <param name="sendToPipeline">
        /// The object that needs to be written.  This will be written as
        /// a single object, even if it is an enumeration.
        /// </param>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The pipeline has already been terminated, or was terminated
        /// during the execution of this method.
        /// The Cmdlet should generally just allow PipelineStoppedException
        /// to percolate up to the caller of ProcessRecord etc.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Not permitted at this time or from this thread.
        /// WriteObject may only be called during a call to this Cmdlet's
        /// implementation of ProcessRecord, BeginProcessing or EndProcessing,
        /// and only from that thread.
        /// </exception>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.WriteObject(object,bool)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.WriteError(ErrorRecord)"/>
        public void WriteObject(object sendToPipeline)
        {
            // This check will be repeated in _WriteObjectSkipAllowCheck,
            // but we want PipelineStoppedException to take precedence
            // over InvalidOperationException if the pipeline has been
            // closed.
            ThrowIfStopping();

#if CORECLR
            // SecurityContext is not supported in CoreCLR
            DoWriteObject(sendToPipeline);
#else
            if (UseSecurityContextRun)
            {
                if (PipelineProcessor == null || PipelineProcessor.SecurityContext == null)
                    throw PSTraceSource.NewInvalidOperationException(PipelineStrings.WriteNotPermitted);
                ContextCallback delegateCallback =
                    new ContextCallback(DoWriteObject);

                SecurityContext.Run(
                    PipelineProcessor.SecurityContext.CreateCopy(),
                    delegateCallback,
                    sendToPipeline);
            }
            else
            {
                DoWriteObject(sendToPipeline);
            }
#endif
        }

        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The pipeline has already been terminated, or was terminated
        /// during the execution of this method.
        /// The Cmdlet should generally just allow PipelineStoppedException
        /// to percolate up to the caller of ProcessRecord etc.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Not permitted at this time or from this thread
        /// </exception>
        private void DoWriteObject(object sendToPipeline)
        {
            ThrowIfWriteNotPermitted(true);
            _WriteObjectSkipAllowCheck(sendToPipeline);
        }

        /// <summary>
        /// Writes one or more objects to the output pipe.
        /// If the object is a collection and the enumerateCollection flag
        /// is true, the objects in the collection
        /// will be written individually.
        /// </summary>
        /// <param name="sendToPipeline">
        /// The object that needs to be written to the pipeline.
        /// </param>
        /// <param name="enumerateCollection">
        /// true if the collection should be enumerated
        /// </param>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The pipeline has already been terminated, or was terminated
        /// during the execution of this method.
        /// The Cmdlet should generally just allow PipelineStoppedException
        /// to percolate up to the caller of ProcessRecord etc.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Not permitted at this time or from this thread.
        /// WriteObject may only be called during a call to this Cmdlet's
        /// implementation of ProcessRecord, BeginProcessing or EndProcessing,
        /// and only from that thread.
        /// </exception>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.WriteObject(object)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.WriteError(ErrorRecord)"/>
        public void WriteObject(object sendToPipeline, bool enumerateCollection)
        {
            if (!enumerateCollection)
            {
                WriteObject(sendToPipeline);
                return;
            }

            // This check will be repeated in _WriteObjectsSkipAllowCheck,
            // but we want PipelineStoppedException to take precedence
            // over InvalidOperationException if the pipeline has been
            // closed.
            ThrowIfStopping();

#if CORECLR
            // SecurityContext is not supported in CoreCLR
            DoWriteEnumeratedObject(sendToPipeline);
#else
            if (UseSecurityContextRun)
            {
                if (PipelineProcessor == null || PipelineProcessor.SecurityContext == null)
                    throw PSTraceSource.NewInvalidOperationException(PipelineStrings.WriteNotPermitted);
                ContextCallback delegateCallback =
                    new ContextCallback(DoWriteObjects);
                SecurityContext.Run(
                    PipelineProcessor.SecurityContext.CreateCopy(),
                    delegateCallback,
                    sendToPipeline);
            }
            else
            {
                DoWriteObjects(sendToPipeline);
            }
#endif
        }

        /// <summary>
        /// Writes an object enumerated from a collection to the output pipe.
        /// </summary>
        /// <param name="sendToPipeline">
        /// The enumerated object that needs to be written to the pipeline.
        /// </param>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The pipeline has already been terminated, or was terminated
        /// during the execution of this method.
        /// The Cmdlet should generally just allow PipelineStoppedException
        /// to percolate up to the caller of ProcessRecord etc.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Not permitted at this time or from this thread.
        /// </exception>
        private void DoWriteEnumeratedObject(object sendToPipeline)
        {
            // NOTICE-2004/06/08-JonN 959638
            ThrowIfWriteNotPermitted(true);
            _EnumerateAndWriteObjectSkipAllowCheck(sendToPipeline);
        }
        // Trust:  public void WriteObject(object sendToPipeline, DataTrustCategory trustCategory);     // enumerateCollection defaults to false
        // Trust:  public void WriteObject(object sendToPipeline, bool enumerateCollection, DataTrustCategory trustCategory);

        // Variables needed to generate a unique SourceId for
        // WriteProgress(ProgressRecord).
        private static Int64 s_lastUsedSourceId /* = 0 */;
        private Int64 _sourceId /* = 0 */;

        /// <summary>
        /// Display progress information.
        /// </summary>
        /// <param name="progressRecord">Progress information.</param>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The pipeline has already been terminated, or was terminated
        /// during the execution of this method.
        /// The Cmdlet should generally just allow PipelineStoppedException
        /// to percolate up to the caller of ProcessRecord etc.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Not permitted at this time or from this thread.
        /// WriteProgress may only be called during a call to this Cmdlet's
        /// implementation of ProcessRecord, BeginProcessing or EndProcessing,
        /// and only from that thread.
        /// </exception>
        /// <remarks>
        /// Use WriteProgress to display progress information about
        /// the activity of your Cmdlet, when the operation of your Cmdlet
        /// could potentially take a long time.
        ///
        /// By default, progress output will
        /// be displayed, although this can be configured with the
        /// ProgressPreference shell variable.
        /// </remarks>
        /// <seealso cref="System.Management.Automation.Cmdlet.WriteDebug(string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.WriteWarning(string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.WriteVerbose(string)"/>
        public void WriteProgress(ProgressRecord progressRecord)
        {
            this.WriteProgress(progressRecord, false);
        }

        internal void WriteProgress(ProgressRecord progressRecord, bool overrideInquire)
        {
            // NTRAID#Windows Out Of Band Releases-918023-2005/08/22-JonN
            ThrowIfStopping();

            //
            // WriteError/WriteObject have a check that prevents them to be called from outside
            // Begin/Process/End. This is done because the Pipeline needs to be ready before these
            // functions can be called.
            //
            // WriteDebug/Warning/Verbose/Process used to do the same check, even though it is not
            // strictly needed. If we ever implement pipelines for these objects we may need to
            // enforce the check again.
            //
            // See bug 583774 in the Windows 7 database for more details.
            //
            ThrowIfWriteNotPermitted(false);

            // Bug909439: We need a unique sourceId to send to
            // WriteProgress. The following logic ensures that
            // there is a unique id for each Cmdlet instance.

            if (_sourceId == 0)
            {
                _sourceId = Interlocked.Increment(ref s_lastUsedSourceId);
            }

            this.WriteProgress(_sourceId, progressRecord, overrideInquire);
        }

        /// <summary>
        /// Displays progress output if enabled.
        /// </summary>
        /// <param name="sourceId">
        /// Identifies which command is reporting progress
        /// </param>
        /// <param name="progressRecord">
        /// Progress status to be displayed
        /// </param>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The pipeline has already been terminated, or was terminated
        /// during the execution of this method.
        /// The Cmdlet should generally just allow PipelineStoppedException
        /// to percolate up to the caller of ProcessRecord etc.
        /// </exception>
        /// <remarks>
        /// If the pipeline is terminated due to ActionPreference.Stop
        /// or ActionPreference.Inquire, this method will throw
        /// <see cref="System.Management.Automation.PipelineStoppedException"/>,
        /// but the command failure will ultimately be
        /// <see cref="System.Management.Automation.ActionPreferenceStopException"/>,
        /// </remarks>
        public void WriteProgress(
            Int64 sourceId,
            ProgressRecord progressRecord)
        {
            WriteProgress(sourceId, progressRecord, false);
        }

        internal void WriteProgress(
                Int64 sourceId,
                ProgressRecord progressRecord,
                bool overrideInquire)
        {
            if (progressRecord == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(progressRecord));
            }

            if (Host == null || Host.UI == null)
            {
                Diagnostics.Assert(false, "No host in CommandBase.WriteProgress()");
                throw PSTraceSource.NewInvalidOperationException();
            }

            InternalHostUserInterface ui = Host.UI as InternalHostUserInterface;

            ActionPreference preference = ProgressPreference;
            if (overrideInquire && preference == ActionPreference.Inquire)
            {
                preference = ActionPreference.Continue;
            }

            if (WriteHelper_ShouldWrite(
                preference, lastProgressContinueStatus))
            {
                // Break into the debugger if requested
                if (preference == ActionPreference.Break)
                {
                    CBhost?.Runspace?.Debugger?.Break(progressRecord);
                }

                ui.WriteProgress(sourceId, progressRecord);
            }

            lastProgressContinueStatus = WriteHelper(
                null,
                null,
                preference,
                lastProgressContinueStatus,
                "ProgressPreference",
                progressRecord.Activity);
        }

        /// <summary>
        /// Display debug information.
        /// </summary>
        /// <param name="text">Debug output.</param>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The pipeline has already been terminated, or was terminated
        /// during the execution of this method.
        /// The Cmdlet should generally just allow PipelineStoppedException
        /// to percolate up to the caller of ProcessRecord etc.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Not permitted at this time or from this thread.
        /// WriteDebug may only be called during a call to this Cmdlet's
        /// implementation of ProcessRecord, BeginProcessing or EndProcessing,
        /// and only from that thread.
        /// </exception>
        /// <remarks>
        /// Use WriteDebug to display debug information on the inner workings
        /// of your Cmdlet.  By default, debug output will
        /// not be displayed, although this can be configured with the
        /// DebugPreference shell variable or the -Debug command-line option.
        /// </remarks>
        /// <remarks>
        /// If the pipeline is terminated due to ActionPreference.Stop
        /// or ActionPreference.Inquire, this method will throw
        /// <see cref="System.Management.Automation.PipelineStoppedException"/>,
        /// but the command failure will ultimately be
        /// <see cref="System.Management.Automation.ActionPreferenceStopException"/>,
        /// </remarks>
        /// <seealso cref="System.Management.Automation.Cmdlet.WriteVerbose(string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.WriteWarning(string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.WriteProgress(ProgressRecord)"/>
        public void WriteDebug(string text)
        {
            WriteDebug(new DebugRecord(text));
        }

        /// <summary>
        /// Display debug information.
        /// </summary>
        internal void WriteDebug(DebugRecord record, bool overrideInquire = false)
        {
            ActionPreference preference = DebugPreference;
            if (overrideInquire && preference == ActionPreference.Inquire)
                preference = ActionPreference.Continue;

            if (WriteHelper_ShouldWrite(preference, lastDebugContinueStatus))
            {
                if (record.InvocationInfo == null)
                {
                    record.SetInvocationInfo(MyInvocation);
                }

                // Break into the debugger if requested
                if (preference == ActionPreference.Break)
                {
                    CBhost?.Runspace?.Debugger?.Break(record);
                }

                if (DebugOutputPipe != null)
                {
                    if (CBhost != null && CBhost.InternalUI != null &&
                        DebugOutputPipe.NullPipe)
                    {
                        // If redirecting to a null pipe, still write to
                        // information buffers.
                        CBhost.InternalUI.WriteDebugInfoBuffers(record);
                    }

                    // Set WriteStream so that the debug output is formatted correctly.
                    PSObject debugWrap = PSObject.AsPSObject(record);
                    debugWrap.WriteStream = WriteStreamType.Debug;

                    DebugOutputPipe.Add(debugWrap);
                }
                else
                {
                    //
                    // If no pipe, write directly to host.
                    //
                    if (Host == null || Host.UI == null)
                    {
                        Diagnostics.Assert(false, "No host in CommandBase.WriteDebug()");
                        throw PSTraceSource.NewInvalidOperationException();
                    }

                    CBhost.InternalUI.TranscribeResult(StringUtil.Format(InternalHostUserInterfaceStrings.DebugFormatString, record.Message));
                    CBhost.InternalUI.WriteDebugRecord(record);
                }
            }

            lastDebugContinueStatus = WriteHelper(
                null,
                null,
                preference,
                lastDebugContinueStatus,
                "DebugPreference",
                record.Message);
        }

        /// <summary>
        /// Display verbose information.
        /// </summary>
        /// <param name="text">Verbose output.</param>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The pipeline has already been terminated, or was terminated
        /// during the execution of this method.
        /// The Cmdlet should generally just allow PipelineStoppedException
        /// to percolate up to the caller of ProcessRecord etc.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Not permitted at this time or from this thread.
        /// WriteVerbose may only be called during a call to this Cmdlet's
        /// implementation of ProcessRecord, BeginProcessing or EndProcessing,
        /// and only from that thread.
        /// </exception>
        /// <remarks>
        /// Use WriteVerbose to display more detailed information about
        /// the activity of your Cmdlet.  By default, verbose output will
        /// not be displayed, although this can be configured with the
        /// VerbosePreference shell variable
        /// or the -Verbose and -Debug command-line options.
        /// </remarks>
        /// <seealso cref="System.Management.Automation.Cmdlet.WriteDebug(string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.WriteWarning(string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.WriteProgress(ProgressRecord)"/>
        public void WriteVerbose(string text)
        {
            WriteVerbose(new VerboseRecord(text));
        }

        /// <summary>
        /// Display verbose information.
        /// </summary>
        internal void WriteVerbose(VerboseRecord record, bool overrideInquire = false)
        {
            ActionPreference preference = VerbosePreference;
            if (overrideInquire && preference == ActionPreference.Inquire)
                preference = ActionPreference.Continue;

            if (WriteHelper_ShouldWrite(preference, lastVerboseContinueStatus))
            {
                if (record.InvocationInfo == null)
                {
                    record.SetInvocationInfo(MyInvocation);
                }

                // Break into the debugger if requested
                if (preference == ActionPreference.Break)
                {
                    CBhost?.Runspace?.Debugger?.Break(record);
                }

                if (VerboseOutputPipe != null)
                {
                    if (CBhost != null && CBhost.InternalUI != null &&
                        VerboseOutputPipe.NullPipe)
                    {
                        // If redirecting to a null pipe, still write to
                        // information buffers.
                        CBhost.InternalUI.WriteVerboseInfoBuffers(record);
                    }

                    // Add WriteStream so that the verbose output is formatted correctly.
                    PSObject verboseWrap = PSObject.AsPSObject(record);
                    verboseWrap.WriteStream = WriteStreamType.Verbose;

                    VerboseOutputPipe.Add(verboseWrap);
                }
                else
                {
                    //
                    // If no pipe, write directly to host.
                    //
                    if (Host == null || Host.UI == null)
                    {
                        Diagnostics.Assert(false, "No host in CommandBase.WriteVerbose()");
                        throw PSTraceSource.NewInvalidOperationException();
                    }

                    CBhost.InternalUI.TranscribeResult(StringUtil.Format(InternalHostUserInterfaceStrings.VerboseFormatString, record.Message));
                    CBhost.InternalUI.WriteVerboseRecord(record);
                }
            }

            lastVerboseContinueStatus = WriteHelper(
                null,
                null,
                preference,
                lastVerboseContinueStatus,
                "VerbosePreference",
                record.Message);
        }

        /// <summary>
        /// Display warning information.
        /// </summary>
        /// <param name="text">Warning output.</param>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The pipeline has already been terminated, or was terminated
        /// during the execution of this method.
        /// The Cmdlet should generally just allow PipelineStoppedException
        /// to percolate up to the caller of ProcessRecord etc.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Not permitted at this time or from this thread.
        /// WriteWarning may only be called during a call to this Cmdlet's
        /// implementation of ProcessRecord, BeginProcessing or EndProcessing,
        /// and only from that thread.
        /// </exception>
        /// <remarks>
        /// Use WriteWarning to display warnings about
        /// the activity of your Cmdlet.  By default, warning output will
        /// be displayed, although this can be configured with the
        /// WarningPreference shell variable
        /// or the -Verbose and -Debug command-line options.
        /// </remarks>
        /// <seealso cref="System.Management.Automation.Cmdlet.WriteDebug(string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.WriteVerbose(string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.WriteProgress(ProgressRecord)"/>
        public void WriteWarning(string text)
        {
            WriteWarning(new WarningRecord(text));
        }

        /// <summary>
        /// Display warning information.
        /// </summary>
        internal void WriteWarning(WarningRecord record, bool overrideInquire = false)
        {
            ActionPreference preference = WarningPreference;
            if (overrideInquire && preference == ActionPreference.Inquire)
                preference = ActionPreference.Continue;

            if (WriteHelper_ShouldWrite(preference, lastWarningContinueStatus))
            {
                if (record.InvocationInfo == null)
                {
                    record.SetInvocationInfo(MyInvocation);
                }

                // Break into the debugger if requested
                if (preference == ActionPreference.Break)
                {
                    CBhost?.Runspace?.Debugger?.Break(record);
                }

                if (WarningOutputPipe != null)
                {
                    if (CBhost != null && CBhost.InternalUI != null &&
                        WarningOutputPipe.NullPipe)
                    {
                        // If redirecting to a null pipe, still write to
                        // information buffers.
                        CBhost.InternalUI.WriteWarningInfoBuffers(record);
                    }

                    // Add WriteStream so that the warning output is formatted correctly.
                    PSObject warningWrap = PSObject.AsPSObject(record);
                    warningWrap.WriteStream = WriteStreamType.Warning;

                    WarningOutputPipe.AddWithoutAppendingOutVarList(warningWrap);
                }
                else
                {
                    //
                    // If no pipe, write directly to host.
                    //
                    if (Host == null || Host.UI == null)
                    {
                        Diagnostics.Assert(false, "No host in CommandBase.WriteWarning()");
                        throw PSTraceSource.NewInvalidOperationException();
                    }

                    CBhost.InternalUI.TranscribeResult(StringUtil.Format(InternalHostUserInterfaceStrings.WarningFormatString, record.Message));
                    CBhost.InternalUI.WriteWarningRecord(record);
                }
            }

            AppendWarningVarList(record);

            lastWarningContinueStatus = WriteHelper(
                null,
                null,
                preference,
                lastWarningContinueStatus,
                "WarningPreference",
                record.Message);
        }

        /// <summary>
        /// Display tagged object information.
        /// </summary>
        public void WriteInformation(InformationRecord informationRecord)
        {
            WriteInformation(informationRecord, false);
        }

        /// <summary>
        /// Display tagged object information.
        /// </summary>
        internal void WriteInformation(InformationRecord record, bool overrideInquire = false)
        {
            ActionPreference preference = InformationPreference;
            if (overrideInquire && preference == ActionPreference.Inquire)
                preference = ActionPreference.Continue;

            // Break into the debugger if requested
            if (preference == ActionPreference.Break)
            {
                CBhost?.Runspace?.Debugger?.Break(record);
            }

            if (preference != ActionPreference.Ignore)
            {
                if (InformationOutputPipe != null)
                {
                    if (CBhost != null && CBhost.InternalUI != null &&
                        InformationOutputPipe.NullPipe)
                    {
                        // If redirecting to a null pipe, still write to
                        // information buffers.
                        CBhost.InternalUI.WriteInformationInfoBuffers(record);
                    }

                    // Add WriteStream so that the information output is formatted correctly.
                    PSObject informationWrap = PSObject.AsPSObject(record);
                    informationWrap.WriteStream = WriteStreamType.Information;

                    InformationOutputPipe.Add(informationWrap);
                }
                else
                {
                    //
                    // If no pipe, write directly to host.
                    //
                    if (Host == null || Host.UI == null)
                    {
                        throw PSTraceSource.NewInvalidOperationException("No host in CommandBase.WriteInformation()");
                    }

                    CBhost.InternalUI.WriteInformationRecord(record);

                    if ((record.Tags.Contains("PSHOST") && (!record.Tags.Contains("FORWARDED")))
                        || (preference == ActionPreference.Continue))
                    {
                        HostInformationMessage hostOutput = record.MessageData as HostInformationMessage;
                        if (hostOutput != null)
                        {
                            string message = hostOutput.Message;
                            ConsoleColor? foregroundColor = null;
                            ConsoleColor? backgroundColor = null;
                            bool noNewLine = false;

                            if (hostOutput.ForegroundColor.HasValue)
                            {
                                foregroundColor = hostOutput.ForegroundColor.Value;
                            }

                            if (hostOutput.BackgroundColor.HasValue)
                            {
                                backgroundColor = hostOutput.BackgroundColor.Value;
                            }

                            if (hostOutput.NoNewLine.HasValue)
                            {
                                noNewLine = hostOutput.NoNewLine.Value;
                            }

                            if (foregroundColor.HasValue || backgroundColor.HasValue)
                            {
                                // It is possible for either one or the other to be empty if run from a
                                // non-interactive host, but only one was specified in Write-Host.
                                // So fill them with defaults if they are empty.
                                if (!foregroundColor.HasValue)
                                {
                                    foregroundColor = ConsoleColor.Gray;
                                }

                                if (!backgroundColor.HasValue)
                                {
                                    backgroundColor = ConsoleColor.Black;
                                }

                                if (noNewLine)
                                {
                                    CBhost.InternalUI.Write(foregroundColor.Value, backgroundColor.Value, message);
                                }
                                else
                                {
                                    CBhost.InternalUI.WriteLine(foregroundColor.Value, backgroundColor.Value, message);
                                }
                            }
                            else
                            {
                                if (noNewLine)
                                {
                                    CBhost.InternalUI.Write(message);
                                }
                                else
                                {
                                    CBhost.InternalUI.WriteLine(message);
                                }
                            }
                        }
                        else
                        {
                            CBhost.InternalUI.WriteLine(record.ToString());
                        }
                    }
                }

                // Both informational and PSHost-targeted messages are transcribed here.
                // The only difference between these two is that PSHost-targeted messages are transcribed
                // even if InformationAction is SilentlyContinue.
                if (record.Tags.Contains("PSHOST") || (preference != ActionPreference.SilentlyContinue))
                {
                    CBhost.InternalUI.TranscribeResult(record.ToString());
                }
            }

            AppendInformationVarList(record);

            lastInformationContinueStatus = WriteHelper(
                null,
                null,
                preference,
                lastInformationContinueStatus,
                "InformationPreference",
                record.ToString());
        }

        /// <summary>
        /// Write text into pipeline execution log.
        /// </summary>
        /// <param name="text">Text to be written to log.</param>
        /// <remarks>
        /// Use WriteCommandDetail to write important information about cmdlet execution to
        /// pipeline execution log.
        ///
        /// If LogPipelineExecutionDetail is turned on, this information will be written
        /// to PowerShell log under log category "Pipeline execution detail"
        /// </remarks>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.WriteDebug(string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.WriteVerbose(string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.WriteProgress(ProgressRecord)"/>
        public void WriteCommandDetail(string text)
        {
            this.PipelineProcessor.LogExecutionInfo(_thisCommand.MyInvocation, text);
        }

        internal bool LogPipelineExecutionDetail { get; } = false;

        private bool InitShouldLogPipelineExecutionDetail()
        {
            CmdletInfo cmdletInfo = _commandInfo as CmdletInfo;

            if (cmdletInfo != null)
            {
                if (string.Equals("Add-Type", cmdletInfo.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (cmdletInfo.Module == null && cmdletInfo.PSSnapIn != null)
                {
                    return cmdletInfo.PSSnapIn.LogPipelineExecutionDetails;
                }

                if (cmdletInfo.PSSnapIn == null && cmdletInfo.Module != null)
                {
                    return cmdletInfo.Module.LogPipelineExecutionDetails;
                }

                return false;
            }

            // Logging should be enabled for functions from modules also
            FunctionInfo functionInfo = _commandInfo as FunctionInfo;
            if (functionInfo != null && functionInfo.Module != null)
            {
                return functionInfo.Module.LogPipelineExecutionDetails;
            }

            return false;
        }

        /// <summary>
        /// This allows all success output to be set to a variable, where the variable is reset for each item returned by
        /// the cmdlet. Semantically this is equivalent to :  cmd | % { $pipelineVariable = $_; (...) }
        /// </summary>
        internal string PipelineVariable { get; set; }

        private PSVariable _pipelineVarReference;
        private bool _shouldRemovePipelineVariable;

        internal void SetupOutVariable()
        {
            if (string.IsNullOrEmpty(this.OutVariable))
            {
                return;
            }

            EnsureVariableParameterAllowed();

            // Handle the creation of OutVariable in the case of Out-Default specially,
            // as it needs to handle much of its OutVariable support itself.
            if (!OutVariable.StartsWith('+') &&
                string.Equals("Out-Default", _commandInfo.Name, StringComparison.OrdinalIgnoreCase))
            {
                _state ??= new SessionState(Context.EngineSessionState);

                IList oldValue = null;
                oldValue = PSObject.Base(_state.PSVariable.GetValue(this.OutVariable)) as IList;

                _outVarList = oldValue ?? new ArrayList();

                if (_thisCommand is not PSScriptCmdlet)
                {
                    this.OutputPipe.AddVariableList(VariableStreamKind.Output, _outVarList);
                }

                _state.PSVariable.Set(this.OutVariable, _outVarList);
            }
            else
            {
                SetupVariable(VariableStreamKind.Output, this.OutVariable, ref _outVarList);
            }
        }

        internal void SetupPipelineVariable()
        {
            // This can't use the common SetupVariable implementation, as this needs to persist for an entire
            // pipeline.

            if (string.IsNullOrEmpty(PipelineVariable))
            {
                return;
            }

            EnsureVariableParameterAllowed();

            _state ??= new SessionState(Context.EngineSessionState);

            // Create the pipeline variable
            _pipelineVarReference = new PSVariable(PipelineVariable);
            object varToUse = _state.Internal.SetVariable(
                _pipelineVarReference,
                force: false,
                CommandOrigin.Internal);

            if (ReferenceEquals(_pipelineVarReference, varToUse))
            {
                // The returned variable is the exact same instance, which means we set a new variable.
                // In this case, we will try removing the pipeline variable in the end.
                _shouldRemovePipelineVariable = true;
            }
            else
            {
                // A variable with the same name already exists in the same scope and it was returned.
                // In this case, we update the reference and don't remove the variable in the end.
                _pipelineVarReference = (PSVariable)varToUse;
            }

            if (_thisCommand is not PSScriptCmdlet)
            {
                this.OutputPipe.SetPipelineVariable(_pipelineVarReference);
            }
        }

        internal void RemovePipelineVariable()
        {
            if (_shouldRemovePipelineVariable)
            {
                // Remove pipeline variable when a pipeline is being torn down.
                _state.PSVariable.Remove(PipelineVariable);
            }
        }

        /// <summary>
        /// Configures the number of objects to buffer before calling the downstream Cmdlet.
        /// </summary>
        /// <remarks>
        /// This is a common parameter via class CommonParameters.
        /// </remarks>
        internal int OutBuffer
        {
            get { return OutputPipe.OutBufferCount; }

            set { OutputPipe.OutBufferCount = value; }
        }

        #endregion Write

        #region Should
        #region ShouldProcess
        /// <summary>
        /// Confirm the operation with the user.  Cmdlets which make changes
        /// (e.g. delete files, stop services etc.) should call ShouldProcess
        /// to give the user the opportunity to confirm that the operation
        /// should actually be performed.
        /// </summary>
        /// <param name="target">
        /// Name of the target resource being acted upon. This will
        /// potentially be displayed to the user.
        /// </param>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The pipeline has already been terminated, or was terminated
        /// during the execution of this method.
        /// The Cmdlet should generally just allow PipelineStoppedException
        /// to percolate up to the caller of ProcessRecord etc.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Not permitted at this time or from this thread.
        /// ShouldProcess may only be called during a call to this Cmdlet's
        /// implementation of ProcessRecord, BeginProcessing or EndProcessing,
        /// and only from that thread.
        /// </exception>
        /// <returns>
        /// If ShouldProcess returns true, the operation should be performed.
        /// If ShouldProcess returns false, the operation should not be
        /// performed, and the Cmdlet should move on to the next target resource.
        /// </returns>
        /// <remarks>
        /// A Cmdlet should declare
        /// [Cmdlet( SupportsShouldProcess = true )]
        /// if-and-only-if it calls ShouldProcess before making changes.
        ///
        /// ShouldProcess may only be called during a call to this Cmdlet's
        /// implementation of ProcessRecord, BeginProcessing or EndProcessing,
        /// and only from that thread.
        ///
        /// ShouldProcess will take into account command-line settings
        /// and preference variables in determining what it should return
        /// and whether it should prompt the user.
        /// </remarks>
        /// <remarks>
        /// If the pipeline is terminated due to ActionPreference.Stop
        /// or ActionPreference.Inquire,
        /// <see cref="System.Management.Automation.Cmdlet.ShouldProcess(string)"/>
        /// will throw
        /// <see cref="System.Management.Automation.PipelineStoppedException"/>,
        /// but the command failure will ultimately be
        /// <see cref="System.Management.Automation.ActionPreferenceStopException"/>,
        /// </remarks>
        /// <example>
        ///     <code>
        ///         namespace Microsoft.Samples.Cmdlet
        ///         {
        ///             [Cmdlet(VerbsCommon.Remove,"myobjecttype1")]
        ///             public class RemoveMyObjectType1 : PSCmdlet
        ///             {
        ///                 [Parameter( Mandatory = true )]
        ///                 public string Filename
        ///                 {
        ///                     get { return filename; }
        ///                     set { filename = value; }
        ///                 }
        ///                 private string filename;
        ///
        ///                 public override void ProcessRecord()
        ///                 {
        ///                     if (ShouldProcess(filename))
        ///                     {
        ///                         // delete the object
        ///                     }
        ///                 }
        ///             }
        ///         }
        ///     </code>
        /// </example>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldProcess(string,string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldProcess(string,string,string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldProcess(string,string,string,out ShouldProcessReason)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldContinue(string,string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldContinue(string,string,ref bool,ref bool)"/>
        public bool ShouldProcess(string target)
        {
            string verboseDescription = StringUtil.Format(CommandBaseStrings.ShouldProcessMessage,
                MyInvocation.MyCommand.Name,
                target);
            ShouldProcessReason shouldProcessReason;
            return DoShouldProcess(verboseDescription, null, null, out shouldProcessReason);
        }

        /// <summary>
        /// Confirm the operation with the user.  Cmdlets which make changes
        /// (e.g. delete files, stop services etc.) should call ShouldProcess
        /// to give the user the opportunity to confirm that the operation
        /// should actually be performed.
        ///
        /// This variant allows the caller to specify text for both the
        /// target resource and the action.
        /// </summary>
        /// <param name="target">
        /// Name of the target resource being acted upon. This will
        /// potentially be displayed to the user.
        /// </param>
        /// <param name="action">
        /// Name of the action which is being performed. This will
        /// potentially be displayed to the user. (default is Cmdlet name)
        /// </param>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The pipeline has already been terminated, or was terminated
        /// during the execution of this method.
        /// The Cmdlet should generally just allow PipelineStoppedException
        /// to percolate up to the caller of ProcessRecord etc.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Not permitted at this time or from this thread.
        /// ShouldProcess may only be called during a call to this Cmdlet's
        /// implementation of ProcessRecord, BeginProcessing or EndProcessing,
        /// and only from that thread.
        /// </exception>
        /// <returns>
        /// If ShouldProcess returns true, the operation should be performed.
        /// If ShouldProcess returns false, the operation should not be
        /// performed, and the Cmdlet should move on to the next target resource.
        /// </returns>
        /// <remarks>
        /// A Cmdlet should declare
        /// [Cmdlet( SupportsShouldProcess = true )]
        /// if-and-only-if it calls ShouldProcess before making changes.
        ///
        /// ShouldProcess may only be called during a call to this Cmdlet's
        /// implementation of ProcessRecord, BeginProcessing or EndProcessing,
        /// and only from that thread.
        ///
        /// ShouldProcess will take into account command-line settings
        /// and preference variables in determining what it should return
        /// and whether it should prompt the user.
        /// </remarks>
        /// <remarks>
        /// If the pipeline is terminated due to ActionPreference.Stop
        /// or ActionPreference.Inquire, this method will throw
        /// <see cref="System.Management.Automation.PipelineStoppedException"/>,
        /// but the command failure will ultimately be
        /// <see cref="System.Management.Automation.ActionPreferenceStopException"/>,
        /// </remarks>
        /// <example>
        ///     <code>
        ///         namespace Microsoft.Samples.Cmdlet
        ///         {
        ///             [Cmdlet(VerbsCommon.Remove,"myobjecttype2")]
        ///             public class RemoveMyObjectType2 : PSCmdlet
        ///             {
        ///                 [Parameter( Mandatory = true )]
        ///                 public string Filename
        ///                 {
        ///                     get { return filename; }
        ///                     set { filename = value; }
        ///                 }
        ///                 private string filename;
        ///
        ///                 public override void ProcessRecord()
        ///                 {
        ///                     if (ShouldProcess(filename, "delete"))
        ///                     {
        ///                         // delete the object
        ///                     }
        ///                 }
        ///             }
        ///         }
        ///     </code>
        /// </example>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldProcess(string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldProcess(string,string,string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldProcess(string,string,string,out ShouldProcessReason)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldContinue(string,string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldContinue(string,string,ref bool,ref bool)"/>
        public bool ShouldProcess(string target, string action)
        {
            string verboseDescription = StringUtil.Format(CommandBaseStrings.ShouldProcessMessage,
                action,
                target,
                null);
            ShouldProcessReason shouldProcessReason;
            return DoShouldProcess(verboseDescription, null, null, out shouldProcessReason);
        }

        /// <summary>
        /// Confirm the operation with the user.  Cmdlets which make changes
        /// (e.g. delete files, stop services etc.) should call ShouldProcess
        /// to give the user the opportunity to confirm that the operation
        /// should actually be performed.
        ///
        /// This variant allows the caller to specify the complete text
        /// describing the operation, rather than just the name and action.
        /// </summary>
        /// <param name="verboseDescription">
        /// Textual description of the action to be performed.
        /// This is what will be displayed to the user for
        /// ActionPreference.Continue.
        /// </param>
        /// <param name="verboseWarning">
        /// Textual query of whether the action should be performed,
        /// usually in the form of a question.
        /// This is what will be displayed to the user for
        /// ActionPreference.Inquire.
        /// </param>
        /// <param name="caption">
        /// Caption of the window which may be displayed
        /// if the user is prompted whether or not to perform the action.
        /// <paramref name="caption"/> may be displayed by some hosts, but not all.
        /// </param>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The pipeline has already been terminated, or was terminated
        /// during the execution of this method.
        /// The Cmdlet should generally just allow PipelineStoppedException
        /// to percolate up to the caller of ProcessRecord etc.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Not permitted at this time or from this thread.
        /// ShouldProcess may only be called during a call to this Cmdlet's
        /// implementation of ProcessRecord, BeginProcessing or EndProcessing,
        /// and only from that thread.
        /// </exception>
        /// <returns>
        /// If ShouldProcess returns true, the operation should be performed.
        /// If ShouldProcess returns false, the operation should not be
        /// performed, and the Cmdlet should move on to the next target resource.
        /// </returns>
        /// <remarks>
        /// A Cmdlet should declare
        /// [Cmdlet( SupportsShouldProcess = true )]
        /// if-and-only-if it calls ShouldProcess before making changes.
        ///
        /// ShouldProcess may only be called during a call to this Cmdlet's
        /// implementation of ProcessRecord, BeginProcessing or EndProcessing,
        /// and only from that thread.
        ///
        /// ShouldProcess will take into account command-line settings
        /// and preference variables in determining what it should return
        /// and whether it should prompt the user.
        /// </remarks>
        /// <remarks>
        /// If the pipeline is terminated due to ActionPreference.Stop
        /// or ActionPreference.Inquire, this method will throw
        /// <see cref="System.Management.Automation.PipelineStoppedException"/>,
        /// but the command failure will ultimately be
        /// <see cref="System.Management.Automation.ActionPreferenceStopException"/>,
        /// </remarks>
        /// <example>
        ///     <code>
        ///         namespace Microsoft.Samples.Cmdlet
        ///         {
        ///             [Cmdlet(VerbsCommon.Remove,"myobjecttype3")]
        ///             public class RemoveMyObjectType3 : PSCmdlet
        ///             {
        ///                 [Parameter( Mandatory = true )]
        ///                 public string Filename
        ///                 {
        ///                     get { return filename; }
        ///                     set { filename = value; }
        ///                 }
        ///                 private string filename;
        ///
        ///                 public override void ProcessRecord()
        ///                 {
        ///                     if (ShouldProcess(
        ///                         string.Format($"Deleting file {filename}"),
        ///                         string.Format($"Are you sure you want to delete file {filename}?"),
        ///                         "Delete file"))
        ///                     {
        ///                         // delete the object
        ///                     }
        ///                 }
        ///             }
        ///         }
        ///     </code>
        /// </example>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldProcess(string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldProcess(string,string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldProcess(string,string,string, out ShouldProcessReason)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldContinue(string,string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldContinue(string,string,ref bool,ref bool)"/>
        public bool ShouldProcess(
            string verboseDescription,
            string verboseWarning,
            string caption)
        {
            ShouldProcessReason shouldProcessReason;
            return DoShouldProcess(
                verboseDescription,
                verboseWarning,
                caption,
                out shouldProcessReason);
        }

        /// <summary>
        /// Confirm the operation with the user.  Cmdlets which make changes
        /// (e.g. delete files, stop services etc.) should call ShouldProcess
        /// to give the user the opportunity to confirm that the operation
        /// should actually be performed.
        ///
        /// This variant allows the caller to specify the complete text
        /// describing the operation, rather than just the name and action.
        /// </summary>
        /// <param name="verboseDescription">
        /// Textual description of the action to be performed.
        /// This is what will be displayed to the user for
        /// ActionPreference.Continue.
        /// </param>
        /// <param name="verboseWarning">
        /// Textual query of whether the action should be performed,
        /// usually in the form of a question.
        /// This is what will be displayed to the user for
        /// ActionPreference.Inquire.
        /// </param>
        /// <param name="caption">
        /// Caption of the window which may be displayed
        /// if the user is prompted whether or not to perform the action.
        /// <paramref name="caption"/> may be displayed by some hosts, but not all.
        /// </param>
        /// <param name="shouldProcessReason">
        /// Indicates the reason(s) why ShouldProcess returned what it returned.
        /// Only the reasons enumerated in
        /// <see cref="System.Management.Automation.ShouldProcessReason"/>
        /// are returned.
        /// </param>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The pipeline has already been terminated, or was terminated
        /// during the execution of this method.
        /// The Cmdlet should generally just allow PipelineStoppedException
        /// to percolate up to the caller of ProcessRecord etc.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Not permitted at this time or from this thread.
        /// ShouldProcess may only be called during a call to this Cmdlet's
        /// implementation of ProcessRecord, BeginProcessing or EndProcessing,
        /// and only from that thread.
        /// </exception>
        /// <returns>
        /// If ShouldProcess returns true, the operation should be performed.
        /// If ShouldProcess returns false, the operation should not be
        /// performed, and the Cmdlet should move on to the next target resource.
        /// </returns>
        /// <remarks>
        /// A Cmdlet should declare
        /// [Cmdlet( SupportsShouldProcess = true )]
        /// if-and-only-if it calls ShouldProcess before making changes.
        ///
        /// ShouldProcess may only be called during a call to this Cmdlet's
        /// implementation of ProcessRecord, BeginProcessing or EndProcessing,
        /// and only from that thread.
        ///
        /// ShouldProcess will take into account command-line settings
        /// and preference variables in determining what it should return
        /// and whether it should prompt the user.
        /// </remarks>
        /// <remarks>
        /// If the pipeline is terminated due to ActionPreference.Stop
        /// or ActionPreference.Inquire, this method will throw
        /// <see cref="System.Management.Automation.PipelineStoppedException"/>,
        /// but the command failure will ultimately be
        /// <see cref="System.Management.Automation.ActionPreferenceStopException"/>,
        /// </remarks>
        /// <example>
        ///     <code>
        ///         namespace Microsoft.Samples.Cmdlet
        ///         {
        ///             [Cmdlet(VerbsCommon.Remove,"myobjecttype3")]
        ///             public class RemoveMyObjectType3 : PSCmdlet
        ///             {
        ///                 [Parameter( Mandatory = true )]
        ///                 public string Filename
        ///                 {
        ///                     get { return filename; }
        ///                     set { filename = value; }
        ///                 }
        ///                 private string filename;
        ///
        ///                 public override void ProcessRecord()
        ///                 {
        ///                     ShouldProcessReason shouldProcessReason;
        ///                     if (ShouldProcess(
        ///                         string.Format($"Deleting file {filename}"),
        ///                         string.Format($"Are you sure you want to delete file {filename}?"),
        ///                         "Delete file",
        ///                         out shouldProcessReason))
        ///                     {
        ///                         // delete the object
        ///                     }
        ///                 }
        ///             }
        ///         }
        ///     </code>
        /// </example>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldProcess(string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldProcess(string,string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldProcess(string,string,string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldContinue(string,string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldContinue(string,string,ref bool,ref bool)"/>
        public bool ShouldProcess(
            string verboseDescription,
            string verboseWarning,
            string caption,
            out ShouldProcessReason shouldProcessReason)
        {
            return DoShouldProcess(
                verboseDescription,
                verboseWarning,
                caption,
                out shouldProcessReason);
        }

        private bool CanShouldProcessAutoConfirm()
        {
            // retrieve ConfirmImpact from commandInfo
            CommandMetadata commandMetadata = _commandInfo.CommandMetadata;
            if (commandMetadata == null)
            {
                Dbg.Assert(false, "Expected CommandMetadata");
                return true;
            }

            ConfirmImpact cmdletConfirmImpact = commandMetadata.ConfirmImpact;

            // compare to ConfirmPreference
            ConfirmImpact threshold = ConfirmPreference;
            if ((threshold == ConfirmImpact.None) || (threshold > cmdletConfirmImpact))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Helper function for ShouldProcess APIs.
        /// </summary>
        /// <param name="verboseDescription">
        /// Description of operation, to be printed for Continue or WhatIf
        /// </param>
        /// <param name="verboseWarning">
        /// Warning prompt, to be printed for Inquire
        /// </param>
        /// <param name="caption">
        /// This is the caption of the window which may be displayed
        /// if the user is prompted whether or not to perform the action.
        /// It may be displayed by some hosts, but not all.
        /// </param>
        /// <param name="shouldProcessReason">
        /// Indicates the reason(s) why ShouldProcess returned what it returned.
        /// Only the reasons enumerated in
        /// <see cref="System.Management.Automation.ShouldProcessReason"/>
        /// are returned.
        /// </param>
        /// <remarks>true iff the action should be performed</remarks>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The pipeline has already been terminated, or was terminated
        /// during the execution of this method.
        /// The Cmdlet should generally just allow PipelineStoppedException
        /// to percolate up to the caller of ProcessRecord etc.
        /// </exception>
        /// <remarks>
        /// If the pipeline is terminated due to ActionPreference.Stop
        /// or ActionPreference.Inquire, this method will throw
        /// <see cref="System.Management.Automation.PipelineStoppedException"/>,
        /// but the command failure will ultimately be
        /// <see cref="System.Management.Automation.ActionPreferenceStopException"/>,
        /// </remarks>
        private bool DoShouldProcess(
            string verboseDescription,
            string verboseWarning,
            string caption,
            out ShouldProcessReason shouldProcessReason)
        {
            ThrowIfStopping();

            shouldProcessReason = ShouldProcessReason.None;

            switch (lastShouldProcessContinueStatus)
            {
                case ContinueStatus.NoToAll:
                    return false;
                case ContinueStatus.YesToAll:
                    return true;
            }

            if (WhatIf)
            {
                // 2005/05/24 908827
                // WriteDebug/WriteVerbose/WriteProgress/WriteWarning should only be callable from the main thread
                //
                // WriteError/WriteObject have a check that prevents them to be called from outside
                // Begin/Process/End. This is done because the Pipeline needs to be ready before these
                // functions can be called.
                //
                // WriteDebug/Warning/Verbose/Process used to do the same check, even though it is not
                // strictly needed. If we ever implement pipelines for these objects we may need to
                // enforce the check again.
                //
                // See bug 583774 in the Windows 7 database for more details.
                //
                ThrowIfWriteNotPermitted(false);

                shouldProcessReason = ShouldProcessReason.WhatIf;
                string whatIfMessage =
                    StringUtil.Format(CommandBaseStrings.ShouldProcessWhatIfMessage,
                        verboseDescription);

                CBhost.InternalUI.TranscribeResult(whatIfMessage);
                CBhost.UI.WriteLine(whatIfMessage);
                return false;
            }

            if (this.CanShouldProcessAutoConfirm())
            {
                if (this.Verbose)
                {
                    // 2005/05/24 908827
                    // WriteDebug/WriteVerbose/WriteProgress/WriteWarning should only be callable from the main thread
                    //
                    // WriteError/WriteObject have a check that prevents them to be called from outside
                    // Begin/Process/End. This is done because the Pipeline needs to be ready before these
                    // functions can be called.
                    //
                    // WriteDebug/Warning/Verbose/Process used to do the same check, even though it is not
                    // strictly needed. If we ever implement pipelines for these objects we may need to
                    // enforce the check again.
                    //
                    // See bug 583774 in the Windows 7 database for more details.
                    //
                    ThrowIfWriteNotPermitted(false);

                    WriteVerbose(verboseDescription);
                }

                return true;
            }

            if (string.IsNullOrEmpty(verboseWarning))
                verboseWarning = StringUtil.Format(CommandBaseStrings.ShouldProcessWarningFallback,
                    verboseDescription);

            // 2005/05/24 908827
            // WriteDebug/WriteVerbose/WriteProgress/WriteWarning should only be callable from the main thread
            //
            // WriteError/WriteObject have a check that prevents them to be called from outside
            // Begin/Process/End. This is done because the Pipeline needs to be ready before these
            // functions can be called.
            //
            // WriteDebug/Warning/Verbose/Process used to do the same check, even though it is not
            // strictly needed. If we ever implement pipelines for these objects we may need to
            // enforce the check again.
            //
            // See bug 583774 in the Windows 7 database for more details.
            //
            ThrowIfWriteNotPermitted(false);

            lastShouldProcessContinueStatus = InquireHelper(
                verboseWarning,
                caption,
                true,   // allowYesToAll
                true,   // allowNoToAll
                false,  // replaceNoWithHalt
                false   // hasSecurityImpact
                );

            switch (lastShouldProcessContinueStatus)
            {
                case ContinueStatus.No:
                case ContinueStatus.NoToAll:
                    return false;
            }

            return true;
        }

        internal enum ShouldProcessPossibleOptimization
        {
            AutoYes_CanSkipShouldProcessCall,
            AutoYes_CanCallShouldProcessAsynchronously,

            AutoNo_CanCallShouldProcessAsynchronously,

            NoOptimizationPossible,
        }

        internal ShouldProcessPossibleOptimization CalculatePossibleShouldProcessOptimization()
        {
            if (this.WhatIf)
            {
                return ShouldProcessPossibleOptimization.AutoNo_CanCallShouldProcessAsynchronously;
            }

            if (this.CanShouldProcessAutoConfirm())
            {
                if (this.Verbose)
                {
                    return ShouldProcessPossibleOptimization.AutoYes_CanCallShouldProcessAsynchronously;
                }
                else
                {
                    return ShouldProcessPossibleOptimization.AutoYes_CanSkipShouldProcessCall;
                }
            }

            return ShouldProcessPossibleOptimization.NoOptimizationPossible;
        }

        #endregion ShouldProcess
        #region ShouldContinue
        /// <summary>
        /// Confirm an operation or grouping of operations with the user.
        /// This differs from ShouldProcess in that it is not affected by
        /// preference settings or command-line parameters,
        /// it always does the query.
        /// This variant only offers Yes/No, not YesToAll/NoToAll.
        /// </summary>
        /// <param name="query">
        /// Textual query of whether the action should be performed,
        /// usually in the form of a question.
        /// </param>
        /// <param name="caption">
        /// Caption of the window which may be displayed
        /// when the user is prompted whether or not to perform the action.
        /// It may be displayed by some hosts, but not all.
        /// </param>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The pipeline has already been terminated, or was terminated
        /// during the execution of this method.
        /// The Cmdlet should generally just allow PipelineStoppedException
        /// to percolate up to the caller of ProcessRecord etc.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Not permitted at this time or from this thread.
        /// ShouldContinue may only be called during a call to this Cmdlet's
        /// implementation of ProcessRecord, BeginProcessing or EndProcessing,
        /// and only from that thread.
        /// </exception>
        /// <returns>
        /// If ShouldContinue returns true, the operation should be performed.
        /// If ShouldContinue returns false, the operation should not be
        /// performed, and the Cmdlet should move on to the next target resource.
        /// </returns>
        /// <remarks>
        /// Cmdlets using ShouldContinue should also offer a "bool Force"
        /// parameter which bypasses the calls to ShouldContinue
        /// and ShouldProcess.
        /// If this is not done, it will be difficult to use the Cmdlet
        /// from scripts and non-interactive hosts.
        ///
        /// Cmdlets using ShouldContinue must still verify operations
        /// which will make changes using ShouldProcess.
        /// This will assure that settings such as -WhatIf work properly.
        /// You may call ShouldContinue either before or after ShouldProcess.
        ///
        /// ShouldContinue may only be called during a call to this Cmdlet's
        /// implementation of ProcessRecord, BeginProcessing or EndProcessing,
        /// and only from that thread.
        ///
        /// Cmdlets may have different "classes" of confirmations.  For example,
        /// "del" confirms whether files in a particular directory should be
        /// deleted, whether read-only files should be deleted, etc.
        /// Cmdlets can use ShouldContinue to store YesToAll/NoToAll members
        /// for each such "class" to keep track of whether the user has
        /// confirmed "delete all read-only files" etc.
        /// ShouldProcess offers YesToAll/NoToAll automatically,
        /// but answering YesToAll or NoToAll applies to all subsequent calls
        /// to ShouldProcess for the Cmdlet instance.
        /// </remarks>
        /// <example>
        ///     <code>
        ///         namespace Microsoft.Samples.Cmdlet
        ///         {
        ///             [Cmdlet(VerbsCommon.Remove,"myobjecttype4")]
        ///             public class RemoveMyObjectType4 : PSCmdlet
        ///             {
        ///                 [Parameter( Mandatory = true )]
        ///                 public string Filename
        ///                 {
        ///                     get { return filename; }
        ///                     set { filename = value; }
        ///                 }
        ///                 private string filename;
        ///
        ///                 [Parameter]
        ///                 public SwitchParameter Force
        ///                 {
        ///                     get { return force; }
        ///                     set { force = value; }
        ///                 }
        ///                 private bool force;
        ///
        ///                 public override void ProcessRecord()
        ///                 {
        ///                     if (ShouldProcess(
        ///                         string.Format($"Deleting file {filename}"),
        ///                         string.Format($"Are you sure you want to delete file {filename}"),
        ///                         "Delete file"))
        ///                     {
        ///                         if (IsReadOnly(filename))
        ///                         {
        ///                             if (!Force &amp;&amp; !ShouldContinue(
        ///                                     string.Format("File {0} is read-only.  Are you sure you want to delete read-only file {0}?", filename),
        ///                                     "Delete file"))
        ///                                     )
        ///                             {
        ///                                 return;
        ///                             }
        ///                         }
        ///                         // delete the object
        ///                     }
        ///                 }
        ///             }
        ///         }
        ///     </code>
        /// </example>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldContinue(string,string,ref bool,ref bool)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldProcess(string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldProcess(string,string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldProcess(string,string,string)"/>
        public bool ShouldContinue(string query, string caption)
        {
            bool yesToAll = false;
            bool noToAll = false;
            return DoShouldContinue(
                query,
                caption,
                hasSecurityImpact: false,
                supportsToAllOptions: false,
                ref yesToAll,
                ref noToAll);
        }

        /// <summary>
        /// Confirm an operation or grouping of operations with the user.
        /// This differs from ShouldProcess in that it is not affected by
        /// preference settings or command-line parameters,
        /// it always does the query.
        /// This variant offers Yes, No, YesToAll and NoToAll.
        /// </summary>
        /// <param name="query">
        /// Textual query of whether the action should be performed,
        /// usually in the form of a question.
        /// </param>
        /// <param name="caption">
        /// Caption of the window which may be displayed
        /// when the user is prompted whether or not to perform the action.
        /// It may be displayed by some hosts, but not all.
        /// </param>
        /// <param name="hasSecurityImpact">
        /// true if the operation being confirmed has a security impact. If specified,
        /// the default option selected in the selection menu is 'No'.
        /// </param>
        /// <param name="yesToAll">
        /// true iff user selects YesToAll.  If this is already true,
        /// ShouldContinue will bypass the prompt and return true.
        /// </param>
        /// <param name="noToAll">
        /// true iff user selects NoToAll.  If this is already true,
        /// ShouldContinue will bypass the prompt and return false.
        /// </param>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The pipeline has already been terminated, or was terminated
        /// during the execution of this method.
        /// The Cmdlet should generally just allow PipelineStoppedException
        /// to percolate up to the caller of ProcessRecord etc.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Not permitted at this time or from this thread.
        /// ShouldContinue may only be called during a call to this Cmdlet's
        /// implementation of ProcessRecord, BeginProcessing or EndProcessing,
        /// and only from that thread.
        /// </exception>
        /// <returns>
        /// If ShouldContinue returns true, the operation should be performed.
        /// If ShouldContinue returns false, the operation should not be
        /// performed, and the Cmdlet should move on to the next target resource.
        /// </returns>
        public bool ShouldContinue(
            string query, string caption, bool hasSecurityImpact, ref bool yesToAll, ref bool noToAll)
        {
            return DoShouldContinue(query, caption, hasSecurityImpact, true, ref yesToAll, ref noToAll);
        }

        /// <summary>
        /// Confirm an operation or grouping of operations with the user.
        /// This differs from ShouldProcess in that it is not affected by
        /// preference settings or command-line parameters,
        /// it always does the query.
        /// This variant offers Yes, No, YesToAll and NoToAll.
        /// </summary>
        /// <param name="query">
        /// Textual query of whether the action should be performed,
        /// usually in the form of a question.
        /// </param>
        /// <param name="caption">
        /// Caption of the window which may be displayed
        /// when the user is prompted whether or not to perform the action.
        /// It may be displayed by some hosts, but not all.
        /// </param>
        /// <param name="yesToAll">
        /// true iff user selects YesToAll.  If this is already true,
        /// ShouldContinue will bypass the prompt and return true.
        /// </param>
        /// <param name="noToAll">
        /// true iff user selects NoToAll.  If this is already true,
        /// ShouldContinue will bypass the prompt and return false.
        /// </param>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The pipeline has already been terminated, or was terminated
        /// during the execution of this method.
        /// The Cmdlet should generally just allow PipelineStoppedException
        /// to percolate up to the caller of ProcessRecord etc.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Not permitted at this time or from this thread.
        /// ShouldContinue may only be called during a call to this Cmdlet's
        /// implementation of ProcessRecord, BeginProcessing or EndProcessing,
        /// and only from that thread.
        /// </exception>
        /// <returns>
        /// If ShouldContinue returns true, the operation should be performed.
        /// If ShouldContinue returns false, the operation should not be
        /// performed, and the Cmdlet should move on to the next target resource.
        /// </returns>
        /// <remarks>
        /// Cmdlets using ShouldContinue should also offer a "bool Force"
        /// parameter which bypasses the calls to ShouldContinue
        /// and ShouldProcess.
        /// If this is not done, it will be difficult to use the Cmdlet
        /// from scripts and non-interactive hosts.
        ///
        /// Cmdlets using ShouldContinue must still verify operations
        /// which will make changes using ShouldProcess.
        /// This will assure that settings such as -WhatIf work properly.
        /// You may call ShouldContinue either before or after ShouldProcess.
        ///
        /// ShouldContinue may only be called during a call to this Cmdlet's
        /// implementation of ProcessRecord, BeginProcessing or EndProcessing,
        /// and only from that thread.
        ///
        /// Cmdlets may have different "classes" of confirmations.  For example,
        /// "del" confirms whether files in a particular directory should be
        /// deleted, whether read-only files should be deleted, etc.
        /// Cmdlets can use ShouldContinue to store YesToAll/NoToAll members
        /// for each such "class" to keep track of whether the user has
        /// confirmed "delete all read-only files" etc.
        /// ShouldProcess offers YesToAll/NoToAll automatically,
        /// but answering YesToAll or NoToAll applies to all subsequent calls
        /// to ShouldProcess for the Cmdlet instance.
        /// </remarks>
        /// <example>
        ///     <code>
        ///         namespace Microsoft.Samples.Cmdlet
        ///         {
        ///             [Cmdlet(VerbsCommon.Remove,"myobjecttype4")]
        ///             public class RemoveMyObjectType5 : PSCmdlet
        ///             {
        ///                 [Parameter( Mandatory = true )]
        ///                 public string Filename
        ///                 {
        ///                     get { return filename; }
        ///                     set { filename = value; }
        ///                 }
        ///                 private string filename;
        ///
        ///                 [Parameter]
        ///                 public SwitchParameter Force
        ///                 {
        ///                     get { return force; }
        ///                     set { force = value; }
        ///                 }
        ///                 private bool force;
        ///
        ///                 private bool yesToAll;
        ///                 private bool noToAll;
        ///
        ///                 public override void ProcessRecord()
        ///                 {
        ///                     if (ShouldProcess(
        ///                         string.Format($"Deleting file {filename}"),
        ///                         string.Format($"Are you sure you want to delete file {filename}"),
        ///                         "Delete file"))
        ///                     {
        ///                         if (IsReadOnly(filename))
        ///                         {
        ///                             if (!Force &amp;&amp; !ShouldContinue(
        ///                                     string.Format($"File {filename} is read-only.  Are you sure you want to delete read-only file {filename}?"),
        ///                                     "Delete file"),
        ///                                     ref yesToAll,
        ///                                     ref noToAll
        ///                                     )
        ///                             {
        ///                                 return;
        ///                             }
        ///                         }
        ///                         // delete the object
        ///                     }
        ///                 }
        ///             }
        ///         }
        ///     </code>
        /// </example>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldContinue(string,string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldProcess(string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldProcess(string,string)"/>
        /// <seealso cref="System.Management.Automation.Cmdlet.ShouldProcess(string,string,string)"/>
        public bool ShouldContinue(
            string query, string caption, ref bool yesToAll, ref bool noToAll)
        {
            return DoShouldContinue(query, caption, false, true, ref yesToAll, ref noToAll);
        }

        private bool DoShouldContinue(
            string query,
            string caption,
            bool hasSecurityImpact,
            bool supportsToAllOptions,
            ref bool yesToAll,
            ref bool noToAll)
        {
            ThrowIfStopping();

            //
            // WriteError/WriteObject have a check that prevents them to be called from outside
            // Begin/Process/End. This is done because the Pipeline needs to be ready before these
            // functions can be called.
            //
            // WriteDebug/Warning/Verbose/Process used to do the same check, even though it is not
            // strictly needed. If we ever implement pipelines for these objects we may need to
            // enforce the check again.
            //
            // See bug 583774 in the Windows 7 database for more details.
            //
            ThrowIfWriteNotPermitted(false);

            if (noToAll)
                return false;
            else if (yesToAll)
                return true;

            ContinueStatus continueStatus = InquireHelper(
                query,
                caption,
                supportsToAllOptions, // allowYesToAll
                supportsToAllOptions, // allowNoToAll
                false,                // replaceNoWithHalt
                hasSecurityImpact     // hasSecurityImpact
                );

            switch (continueStatus)
            {
                case ContinueStatus.No:
                    return false;

                case ContinueStatus.NoToAll:
                    noToAll = true;
                    return false;

                case ContinueStatus.YesToAll:
                    yesToAll = true;
                    break;
            }

            return true;
        }
        #endregion ShouldContinue
        #endregion Should

        #region Transaction Support
        /// <summary>
        /// Returns true if a transaction is available for use.
        /// </summary>
        public bool TransactionAvailable()
        {
            return UseTransactionFlagSet && Context.TransactionManager.HasTransaction;
        }

        /// <summary>
        /// Gets an object that surfaces the current PowerShell transaction.
        /// When this object is disposed, PowerShell resets the active transaction.
        /// </summary>
        public PSTransactionContext CurrentPSTransaction
        {
            get
            {
                if (!TransactionAvailable())
                {
                    string error = null;

                    if (!UseTransactionFlagSet)
                        error = TransactionStrings.CmdletRequiresUseTx;
                    else
                        error = TransactionStrings.NoTransactionAvailable;

                    // We want to throw in this situation, and want to use a
                    // property because it mimics the C# using(TransactionScope ...) syntax
#pragma warning suppress 56503
                    throw new InvalidOperationException(error);
                }

                return new PSTransactionContext(Context.TransactionManager);
            }
        }
        #endregion Transaction Support

        #region Misc
        /// <summary>
        /// Implementation of ThrowTerminatingError.
        /// </summary>
        /// <param name="errorRecord">
        /// The error which caused the command to be terminated
        /// </param>
        /// <exception cref="PipelineStoppedException">
        /// always
        /// </exception>
        /// <remarks>
        /// <see cref="System.Management.Automation.Cmdlet.ThrowTerminatingError"/>
        /// terminates the command, where
        /// <see cref="System.Management.Automation.ICommandRuntime.WriteError"/>
        /// allows the command to continue.
        ///
        /// The cmdlet can also terminate the command by simply throwing
        /// any exception.  When the cmdlet's implementation of
        /// <see cref="System.Management.Automation.Cmdlet.ProcessRecord"/>,
        /// <see cref="System.Management.Automation.Cmdlet.BeginProcessing"/> or
        /// <see cref="System.Management.Automation.Cmdlet.EndProcessing"/>
        /// throws an exception, the Engine will always catch the exception
        /// and report it as a terminating error.
        /// However, it is preferred for the cmdlet to call
        /// <see cref="System.Management.Automation.Cmdlet.ThrowTerminatingError"/>,
        /// so that the additional information in
        /// <see cref="System.Management.Automation.ErrorRecord"/>
        /// is available.
        /// <see cref="System.Management.Automation.Cmdlet.ThrowTerminatingError"/>
        /// always throws
        /// <see cref="System.Management.Automation.PipelineStoppedException"/>,
        /// regardless of what error was specified in <paramref name="errorRecord"/>.
        /// The Cmdlet should generally just allow
        /// <see cref="System.Management.Automation.PipelineStoppedException"/>.
        /// to percolate up to the caller of
        /// <see cref="System.Management.Automation.Cmdlet.ProcessRecord"/>.
        /// etc.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
        public void ThrowTerminatingError(ErrorRecord errorRecord)
        {
            ThrowIfStopping();
            if (errorRecord == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(errorRecord));
            }

            errorRecord.SetInvocationInfo(MyInvocation);

            if (errorRecord.ErrorDetails != null
                && errorRecord.ErrorDetails.TextLookupError != null)
            {
                Exception textLookupError = errorRecord.ErrorDetails.TextLookupError;
                errorRecord.ErrorDetails.TextLookupError = null;
                MshLog.LogCommandHealthEvent(
                    Context,
                    textLookupError,
                    Severity.Warning);
            }

            // This code forces the stack trace and source fields to be populated
            if (errorRecord.Exception != null
                && string.IsNullOrEmpty(errorRecord.Exception.StackTrace))
            {
                try
                {
                    throw errorRecord.Exception;
                }
                catch (Exception)
                {
                    // no need to worry about severe exceptions since
                    // it wasn't really thrown originally
                }
            }

            CmdletInvocationException e =
                new CmdletInvocationException(errorRecord);

            // If the error action preference is set to break, break immediately
            // into the debugger
            if (ErrorAction == ActionPreference.Break)
            {
                Context.Debugger?.Break(e.InnerException ?? e);
            }

            // Code sees only that execution stopped
            throw ManageException(e);
        }
        #endregion Misc

        #region Data Merging

        /// <summary>
        /// Data streams available for merging.
        /// </summary>
        internal enum MergeDataStream
        {
            /// <summary>
            /// No data stream available for merging.
            /// </summary>
            None = 0,

            /// <summary>
            /// All data streams.
            /// </summary>
            All = 1,

            /// <summary>
            /// Success output.
            /// </summary>
            Output = 2,

            /// <summary>
            /// Error output.
            /// </summary>
            Error = 3,

            /// <summary>
            /// Warning output.
            /// </summary>
            Warning = 4,

            /// <summary>
            /// Verbose output.
            /// </summary>
            Verbose = 5,

            /// <summary>
            /// Debug output.
            /// </summary>
            Debug = 6,

            /// <summary>
            /// Host output.
            /// </summary>
            Host = 7,

            /// <summary>
            /// Information output.
            /// </summary>
            Information = 8
        }

        /// <summary>
        /// Get/sets error data stream merge state.
        /// </summary>
        internal MergeDataStream ErrorMergeTo { get; set; }

        /// <summary>
        /// Method to set data stream merging based on passed in runtime object.
        /// </summary>
        /// <param name="fromRuntime">MshCommandRuntime object.</param>
        internal void SetMergeFromRuntime(MshCommandRuntime fromRuntime)
        {
            this.ErrorMergeTo = fromRuntime.ErrorMergeTo;

            if (fromRuntime.WarningOutputPipe != null)
            {
                this.WarningOutputPipe = fromRuntime.WarningOutputPipe;
            }

            if (fromRuntime.VerboseOutputPipe != null)
            {
                this.VerboseOutputPipe = fromRuntime.VerboseOutputPipe;
            }

            if (fromRuntime.DebugOutputPipe != null)
            {
                this.DebugOutputPipe = fromRuntime.DebugOutputPipe;
            }

            if (fromRuntime.InformationOutputPipe != null)
            {
                this.InformationOutputPipe = fromRuntime.InformationOutputPipe;
            }
        }

        //
        // Legacy merge hints.
        //

        /// <summary>
        /// Claims the unclaimed error output of all previous commands.
        /// </summary>
        internal bool MergeUnclaimedPreviousErrorResults { get; set; } = false;

        #endregion

        #region Internal Pipes

        /// <summary>
        /// Gets or sets the input pipe.
        /// </summary>
        internal Pipe InputPipe
        {
            get { return _inputPipe ??= new Pipe(); }

            set { _inputPipe = value; }
        }

        /// <summary>
        /// Gets or sets the output pipe.
        /// </summary>
        internal Pipe OutputPipe
        {
            get { return _outputPipe ??= new Pipe(); }

            set { _outputPipe = value; }
        }

        internal object[] GetResultsAsArray()
        {
            if (_outputPipe == null)
                return StaticEmptyArray;
            return _outputPipe.ToArray();
        }

        /// <summary>
        /// An empty array that is declared statically so we don't keep
        /// allocating them over and over...
        /// </summary>
        internal static readonly object[] StaticEmptyArray = Array.Empty<object>();

        /// <summary>
        /// Gets or sets the error pipe.
        /// </summary>
        internal Pipe ErrorOutputPipe
        {
            get { return _errorOutputPipe ??= new Pipe(); }

            set { _errorOutputPipe = value; }
        }

        /// <summary>
        /// Gets or sets the warning output pipe.
        /// </summary>
        internal Pipe WarningOutputPipe { get; set; }

        /// <summary>
        /// Gets or sets the verbose output pipe.
        /// </summary>
        internal Pipe VerboseOutputPipe { get; set; }

        /// <summary>
        /// Gets or sets the debug output pipe.
        /// </summary>
        internal Pipe DebugOutputPipe { get; set; }

        /// <summary>
        /// Gets or sets the informational output pipe.
        /// </summary>
        internal Pipe InformationOutputPipe { get; set; }

        #endregion

        #region Internal helpers
        /// <summary>
        /// Throws if the pipeline is stopping.
        /// </summary>
        /// <exception cref="System.Management.Automation.PipelineStoppedException"></exception>
        internal void ThrowIfStopping()
        {
            if (IsStopping)
                throw new PipelineStoppedException();
        }

        /// <summary>
        /// Throws if the caller is trying to call WriteObject/WriteError
        /// from the wrong thread, or not during a call to
        /// BeginProcessing/ProcessRecord/EndProcessing.
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        internal void ThrowIfWriteNotPermitted(bool needsToWriteToPipeline)
        {
            if (this.PipelineProcessor == null
                || _thisCommand != this.PipelineProcessor._permittedToWrite
                || needsToWriteToPipeline && !this.PipelineProcessor._permittedToWriteToPipeline
                || Thread.CurrentThread != this.PipelineProcessor._permittedToWriteThread
               )
            {
                // Only generate these exceptions if a pipeline has already been declared as the 'writing' pipeline.
                // Otherwise, these are probably infrastructure messages and can be ignored.
                if (this.PipelineProcessor?._permittedToWrite != null)
                {
                    throw PSTraceSource.NewInvalidOperationException(
                        PipelineStrings.WriteNotPermitted);
                }
            }
        }

        /// <summary>
        /// WriteObject/WriteObjecs/WriteError are only allowed during this scope.
        /// Be sure to use this object only in "using" so that it is reliably
        /// disposed and follows stack semantics.
        /// </summary>
        /// <returns>IDisposable.</returns>
        internal IDisposable AllowThisCommandToWrite(bool permittedToWriteToPipeline)
        {
            return new AllowWrite(_thisCommand, permittedToWriteToPipeline);
        }

        private sealed class AllowWrite : IDisposable
        {
            /// <summary>
            /// Begin the scope where WriteObject/WriteError is permitted.
            /// </summary>
            internal AllowWrite(InternalCommand permittedToWrite, bool permittedToWriteToPipeline)
            {
                if (permittedToWrite == null)
                    throw PSTraceSource.NewArgumentNullException(nameof(permittedToWrite));
                if (!(permittedToWrite.commandRuntime is MshCommandRuntime mcr))
                    throw PSTraceSource.NewArgumentNullException("permittedToWrite.CommandRuntime");
                _pp = mcr.PipelineProcessor;
                if (_pp == null)
                    throw PSTraceSource.NewArgumentNullException("permittedToWrite.CommandRuntime.PipelineProcessor");
                _wasPermittedToWrite = _pp._permittedToWrite;
                _wasPermittedToWriteToPipeline = _pp._permittedToWriteToPipeline;
                _wasPermittedToWriteThread = _pp._permittedToWriteThread;
                _pp._permittedToWrite = permittedToWrite;
                _pp._permittedToWriteToPipeline = permittedToWriteToPipeline;
                _pp._permittedToWriteThread = Thread.CurrentThread;
            }
            /// <summary>
            /// End the scope where WriteObject/WriteError is permitted.
            /// </summary>
            /// <!--
            /// Not a true public, since the class is internal.
            /// This is public only due to C# interface rules.
            /// -->
            public void Dispose()
            {
                _pp._permittedToWrite = _wasPermittedToWrite;
                _pp._permittedToWriteToPipeline = _wasPermittedToWriteToPipeline;
                _pp._permittedToWriteThread = _wasPermittedToWriteThread;
                GC.SuppressFinalize(this);
            }

            // There is no finalizer, by design.  This class relies on always
            // being disposed and always following stack semantics.

            private readonly PipelineProcessor _pp = null;
            private readonly InternalCommand _wasPermittedToWrite = null;
            private readonly bool _wasPermittedToWriteToPipeline = false;
            private readonly Thread _wasPermittedToWriteThread = null;
        }

        /// <summary>
        /// Stores the exception to be returned from
        /// PipelineProcessor.SynchronousExecute,
        /// and writes it to the error variable.
        /// The general pattern is to call
        /// throw ManageException(e);
        /// </summary>
        /// <param name="e">The exception.</param>
        /// <returns>PipelineStoppedException.</returns>
        public Exception ManageException(Exception e)
        {
            if (e == null)
                throw PSTraceSource.NewArgumentNullException(nameof(e));

            PipelineProcessor?.RecordFailure(e, _thisCommand);

            // 1021203-2005/05/09-JonN
            // HaltCommandException will cause the command
            // to stop, but not be reported as an error.
            // 913088-2005/06/06
            // PipelineStoppedException should not get added to $Error
            // 2008/06/25 - narrieta: ExistNestedPromptException should not be added to $error either
            // 2019/10/18 - StopUpstreamCommandsException should not be added either
            if (e is not HaltCommandException
                && e is not PipelineStoppedException
                && e is not ExitNestedPromptException
                && e is not StopUpstreamCommandsException)
            {
                try
                {
                    AppendErrorToVariables(e);
                }
                catch
                {
                    // Catch all OK, the error variables might be corrupted.
                }

                // Log a command health event
                MshLog.LogCommandHealthEvent(
                    Context,
                    e,
                    Severity.Warning);
            }

            // Upstream Cmdlets see only that execution stopped
            return new PipelineStoppedException();
        }

        #endregion Internal helpers

        #region Error PSVariable
        private IList _errorVarList;

        /// <summary>
        /// ErrorVariable tells which variable to populate with the errors.
        /// Use +varname to append to the variable rather than clearing it.
        /// </summary>
        /// <remarks>
        /// This is a common parameter via class CommonParameters.
        /// </remarks>
        internal string ErrorVariable { get; set; }

        internal void SetupErrorVariable()
        {
            SetupVariable(VariableStreamKind.Error, this.ErrorVariable, ref _errorVarList);
        }

        private void EnsureVariableParameterAllowed()
        {
            if ((Context.LanguageMode == PSLanguageMode.NoLanguage) ||
                (Context.LanguageMode == PSLanguageMode.RestrictedLanguage))
            {
                throw InterpreterError.NewInterpreterException(null, typeof(RuntimeException),
                    null, "VariableReferenceNotSupportedInDataSection",
                    ParserStrings.VariableReferenceNotSupportedInDataSection,
                    ParserStrings.DefaultAllowedVariablesInDataSection);
            }
        }

        /// <summary>
        /// Append an error to the ErrorVariable if specified, and also to $ERROR.
        /// </summary>
        /// <param name="obj">Exception or ErrorRecord.</param>
        /// <exception cref="System.Management.Automation.ExtendedTypeSystemException">
        /// (An error occurred working with the error variable or $ERROR.
        /// </exception>
        internal void AppendErrorToVariables(object obj)
        {
            if (obj == null)
                return;

            AppendDollarError(obj);

            this.OutputPipe.AppendVariableList(VariableStreamKind.Error, obj);
        }

        /// <summary>
        /// Appends the object to $global:error.  Non-terminating errors
        /// are always added (even if they are redirected to another
        /// Cmdlet), but terminating errors are only added if they are
        /// at the top-level scope (the LocalPipeline scope).
        /// We insert at position 0 and delete from position 63.
        /// </summary>
        /// <param name="obj">
        /// ErrorRecord or Exception to be written to $global:error
        /// </param>
        /// <exception cref="System.Management.Automation.ExtendedTypeSystemException">
        /// An error occurred accessing $ERROR.
        /// </exception>
        private void AppendDollarError(object obj)
        {
            if (obj is Exception)
            {
                if (this.PipelineProcessor == null || !this.PipelineProcessor.TopLevel)
                    return; // not outermost scope
            }

            Context.AppendDollarError(obj);
        }

        #endregion Error PSVariable

        #region Warning PSVariable
        private IList _warningVarList;

        /// <summary>
        /// WarningVariable tells which variable to populate with the warnings.
        /// Use +varname to append to the variable rather than clearing it.
        /// </summary>
        /// <remarks>
        /// This is a common parameter via class CommonParameters.
        /// </remarks>
        internal string WarningVariable { get; set; }

        internal void SetupWarningVariable()
        {
            SetupVariable(VariableStreamKind.Warning, this.WarningVariable, ref _warningVarList);
        }

        /// <summary>
        /// Append a warning to WarningVariable if specified.
        /// </summary>
        /// <param name="obj">The warning message.</param>
        internal void AppendWarningVarList(object obj)
        {
            this.OutputPipe.AppendVariableList(VariableStreamKind.Warning, obj);
        }

        #endregion Warning PSVariable

        #region Information PSVariable
        private IList _informationVarList;

        /// <summary>
        /// InformationVariable tells which variable to populate with informational output.
        /// Use +varname to append to the variable rather than clearing it.
        /// </summary>
        /// <remarks>
        /// This is a common parameter via class CommonParameters.
        /// </remarks>
        internal string InformationVariable { get; set; }

        internal void SetupInformationVariable()
        {
            SetupVariable(VariableStreamKind.Information, this.InformationVariable, ref _informationVarList);
        }

        internal void SetupVariable(VariableStreamKind streamKind, string variableName, ref IList varList)
        {
            if (string.IsNullOrEmpty(variableName))
            {
                return;
            }

            EnsureVariableParameterAllowed();

            _state ??= new SessionState(Context.EngineSessionState);

            if (variableName.StartsWith('+'))
            {
                variableName = variableName.Substring(1);
                object oldValue = PSObject.Base(_state.PSVariable.GetValue(variableName));
                varList = oldValue as IList;
                if (varList == null)
                {
                    varList = new ArrayList();

                    if (oldValue != null && AutomationNull.Value != oldValue)
                    {
                        IEnumerable enumerable = LanguagePrimitives.GetEnumerable(oldValue);
                        if (enumerable != null)
                        {
                            foreach (object o in enumerable)
                            {
                                varList.Add(o);
                            }
                        }
                        else
                        {
                            varList.Add(oldValue);
                        }
                    }
                }
                else if (varList.IsFixedSize)
                {
                    ArrayList varListNew = new ArrayList();
                    varListNew.AddRange(varList);
                    varList = varListNew;
                }
            }
            else
            {
                varList = new ArrayList();
            }

            if (_thisCommand is not PSScriptCmdlet)
            {
                this.OutputPipe.AddVariableList(streamKind, varList);
            }

            _state.PSVariable.Set(variableName, varList);
        }

        /// <summary>
        /// Append a Information to InformationVariable if specified.
        /// </summary>
        /// <param name="obj">The Information message.</param>
        internal void AppendInformationVarList(object obj)
        {
            this.OutputPipe.AppendVariableList(VariableStreamKind.Information, obj);
        }

        #endregion Information PSVariable

        #region Write
        internal bool UseSecurityContextRun = true;

        /// <summary>
        /// Writes an object to the output pipe, skipping the ThrowIfWriteNotPermitted check.
        /// </summary>
        /// <param name="sendToPipeline">
        /// The object to write to the output pipe.
        /// </param>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The pipeline has already been terminated, or was terminated
        /// during the execution of this method.
        /// The Cmdlet should generally just allow PipelineStoppedException
        /// to percolate up to the caller of ProcessRecord etc.
        /// </exception>
        internal void _WriteObjectSkipAllowCheck(object sendToPipeline)
        {
            ThrowIfStopping();

            if (AutomationNull.Value == sendToPipeline)
                return;

            sendToPipeline = LanguagePrimitives.AsPSObjectOrNull(sendToPipeline);

            this.OutputPipe.Add(sendToPipeline);
        }

        /// <summary>
        /// Enumerates and writes an object to the output pipe, skipping the ThrowIfWriteNotPermitted check.
        /// </summary>
        /// <param name="sendToPipeline">
        /// The object to enumerate and write to the output pipe.
        /// </param>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The pipeline has already been terminated, or was terminated
        /// during the execution of this method.
        /// The Cmdlet should generally just allow PipelineStoppedException
        /// to percolate up to the caller of ProcessRecord etc.
        /// </exception>
        internal void _EnumerateAndWriteObjectSkipAllowCheck(object sendToPipeline)
        {
            IEnumerable enumerable = LanguagePrimitives.GetEnumerable(sendToPipeline);
            if (enumerable == null)
            {
                _WriteObjectSkipAllowCheck(sendToPipeline);
                return;
            }

            ThrowIfStopping();

            ArrayList convertedList = new ArrayList();
            foreach (object toConvert in enumerable)
            {
                if (AutomationNull.Value == toConvert)
                {
                    continue;
                }

                object converted = LanguagePrimitives.AsPSObjectOrNull(toConvert);
                convertedList.Add(converted);
            }

            // Writing normal output with "2>&1"
            // bypasses ErrorActionPreference, as intended.
            this.OutputPipe.AddItems(convertedList);
        }

        #endregion Write

        #region WriteError
        /// <summary>
        /// Internal variant: Writes the specified error to the error pipe.
        /// </summary>
        /// <remarks>
        /// Do not call WriteError(e.ErrorRecord).
        /// The ErrorRecord contained in the ErrorRecord property of
        /// an exception which implements IContainsErrorRecord
        /// should not be passed directly to WriteError, since it contains
        /// a <see cref="System.Management.Automation.ParentContainsErrorRecordException"/>
        /// rather than the real exception.
        /// </remarks>
        /// <param name="errorRecord">Error.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Not permitted at this time or from this thread
        /// </exception>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The pipeline has already been terminated, or was terminated
        /// during the execution of this method.
        /// The Cmdlet should generally just allow PipelineStoppedException
        /// to percolate up to the caller of ProcessRecord etc.
        /// </exception>
        /// <remarks>
        /// <see cref="System.Management.Automation.Cmdlet.ThrowTerminatingError"/>
        /// terminates the command, where
        /// <see cref="System.Management.Automation.ICommandRuntime.WriteError"/>
        /// allows the command to continue.
        ///
        /// If the pipeline is terminated due to ActionPreference.Stop
        /// or ActionPreference.Inquire, this method will throw
        /// <see cref="System.Management.Automation.PipelineStoppedException"/>,
        /// but the command failure will ultimately be
        /// <see cref="System.Management.Automation.ActionPreferenceStopException"/>,
        /// </remarks>
        public void WriteError(ErrorRecord errorRecord)
        {
            WriteError(errorRecord, false);
        }

        internal void WriteError(ErrorRecord errorRecord, bool overrideInquire)
        {
            // This check will be repeated in _WriteErrorSkipAllowCheck,
            // but we want PipelineStoppedException to take precedence
            // over InvalidOperationException if the pipeline has been
            // closed.
            ThrowIfStopping();

            ActionPreference preference = ErrorAction;
            if (overrideInquire && preference == ActionPreference.Inquire)
            {
                preference = ActionPreference.Continue;
            }

            // Break into the debugger if requested
            if (preference == ActionPreference.Break)
            {
                CBhost?.Runspace?.Debugger?.Break(errorRecord);
            }

#if CORECLR
            // SecurityContext is not supported in CoreCLR
            DoWriteError(new KeyValuePair<ErrorRecord, ActionPreference>(errorRecord, preference));
#else
            if (UseSecurityContextRun)
            {
                if (PipelineProcessor == null || PipelineProcessor.SecurityContext == null)
                    throw PSTraceSource.NewInvalidOperationException(PipelineStrings.WriteNotPermitted);
                ContextCallback delegateCallback =
                    new ContextCallback(DoWriteError);

                SecurityContext.Run(
                    PipelineProcessor.SecurityContext.CreateCopy(),
                    delegateCallback,
                    new KeyValuePair<ErrorRecord, ActionPreference>(errorRecord, preference));
            }
            else
            {
                DoWriteError(new KeyValuePair<ErrorRecord, ActionPreference>(errorRecord, preference));
            }
#endif
        }

        /// <exception cref="System.InvalidOperationException">
        /// Not permitted at this time or from this thread
        /// </exception>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The pipeline has already been terminated, or was terminated
        /// during the execution of this method.
        /// The Cmdlet should generally just allow PipelineStoppedException
        /// to percolate up to the caller of ProcessRecord etc.
        /// </exception>
        /// <remarks>
        /// If the pipeline is terminated due to ActionPreference.Stop
        /// or ActionPreference.Inquire, this method will throw
        /// <see cref="System.Management.Automation.PipelineStoppedException"/>,
        /// but the command failure will ultimately be
        /// <see cref="System.Management.Automation.ActionPreferenceStopException"/>,
        /// </remarks>
        private void DoWriteError(object obj)
        {
            KeyValuePair<ErrorRecord, ActionPreference> pair = (KeyValuePair<ErrorRecord, ActionPreference>)obj;
            ErrorRecord errorRecord = pair.Key;
            ActionPreference preference = pair.Value;
            if (errorRecord == null)
            {
                throw PSTraceSource.NewArgumentNullException("errorRecord");
            }

            // If this error came from a transacted cmdlet,
            // rollback the transaction
            if (UseTransaction)
            {
                if (
                   (Context.TransactionManager.RollbackPreference != RollbackSeverity.TerminatingError) &&
                   (Context.TransactionManager.RollbackPreference != RollbackSeverity.Never))
                {
                    Context.TransactionManager.Rollback(true);
                }
            }

            // 2005/07/14-913791 "write-error output is confusing and misleading"
            // set InvocationInfo to the script not the command
            if (errorRecord.PreserveInvocationInfoOnce)
                errorRecord.PreserveInvocationInfoOnce = false;
            else
                errorRecord.SetInvocationInfo(MyInvocation);

            // NOTICE-2004/06/08-JonN 959638
            ThrowIfWriteNotPermitted(true);

            _WriteErrorSkipAllowCheck(errorRecord, preference);
        }

        /// <summary>
        /// Write an error, skipping the ThrowIfWriteNotPermitted check.
        /// </summary>
        /// <param name="errorRecord">The error record to write.</param>
        /// <param name="actionPreference">The configured error action preference.</param>
        /// <param name="isFromNativeStdError">
        /// True when this method is called to write from a native command's stderr stream.
        /// When errors are written through a native stderr stream, they do not interact with the error preference system,
        /// but must still present as errors in PowerShell.
        /// </param>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The pipeline has already been terminated, or was terminated
        /// during the execution of this method.
        /// The Cmdlet should generally just allow PipelineStoppedException
        /// to percolate up to the caller of ProcessRecord etc.
        /// </exception>
        /// <remarks>
        /// If the pipeline is terminated due to ActionPreference.Stop
        /// or ActionPreference.Inquire, this method will throw
        /// <see cref="System.Management.Automation.PipelineStoppedException"/>,
        /// but the command failure will ultimately be
        /// <see cref="System.Management.Automation.ActionPreferenceStopException"/>,
        /// </remarks>
        internal void _WriteErrorSkipAllowCheck(ErrorRecord errorRecord, ActionPreference? actionPreference = null, bool isFromNativeStdError = false)
        {
            ThrowIfStopping();

            if (errorRecord.ErrorDetails != null
                && errorRecord.ErrorDetails.TextLookupError != null)
            {
                Exception textLookupError = errorRecord.ErrorDetails.TextLookupError;
                errorRecord.ErrorDetails.TextLookupError = null;
                MshLog.LogCommandHealthEvent(
                    Context,
                    textLookupError,
                    Severity.Warning);
            }

            if (LogPipelineExecutionDetail)
            {
                this.PipelineProcessor.LogExecutionError(_thisCommand.MyInvocation, errorRecord);
            }

            if (!isFromNativeStdError)
            {
                this.PipelineProcessor.ExecutionFailed = true;

                ActionPreference preference = ErrorAction;
                if (actionPreference.HasValue)
                {
                    preference = actionPreference.Value;
                }

                // No trace of the error in the 'Ignore' case
                if (preference == ActionPreference.Ignore)
                {
                    return; // do not write or record to output pipe
                }

                // 2004/05/26-JonN
                // The object is not written in the SilentlyContinue case
                if (preference == ActionPreference.SilentlyContinue)
                {
                    AppendErrorToVariables(errorRecord);
                    return; // do not write to output pipe
                }

                if (lastErrorContinueStatus == ContinueStatus.YesToAll)
                {
                    preference = ActionPreference.Continue;
                }

                switch (preference)
                {
                    case ActionPreference.Stop:
                        ActionPreferenceStopException e =
                            new ActionPreferenceStopException(
                                MyInvocation,
                                errorRecord,
                                StringUtil.Format(CommandBaseStrings.ErrorPreferenceStop,
                                                "ErrorActionPreference",
                                                errorRecord.ToString()));
                        throw ManageException(e);

                    case ActionPreference.Inquire:
                        // ignore return value
                        // this will throw if the user chooses not to continue
                        lastErrorContinueStatus = InquireHelper(
                            RuntimeException.RetrieveMessage(errorRecord),
                            null,
                            true,  // allowYesToAll
                            false, // allowNoToAll
                            true,  // replaceNoWithHalt
                            false  // hasSecurityImpact
                        );
                        break;
                }

                AppendErrorToVariables(errorRecord);
            }

            // Add this note property and set its value to true for F&O
            // to decide whether to call WriteErrorLine or WriteLine.
            // We want errors to print in red in both cases.
            PSObject errorWrap = PSObject.AsPSObject(errorRecord);
            // It's possible we've already added the member (this method is recursive sometimes
            // when tracing), so don't add the member again.

            // We don't add a note property on messages that comes from stderr stream.
            if (!isFromNativeStdError)
            {
                errorWrap.WriteStream = WriteStreamType.Error;
            }

            // 2003/11/19-JonN Previously, PSObject instances in ErrorOutputPipe
            // wrapped the TargetObject and held the CoreException as a note.
            // Now, they wrap the CoreException and hold the TargetObject as a note.
            if (ErrorMergeTo != MergeDataStream.None)
            {
                Dbg.Assert(ErrorMergeTo == MergeDataStream.Output, "Only merging to success output is supported.");
                this.OutputPipe.AddWithoutAppendingOutVarList(errorWrap);
            }
            else
            {
                // If this is an error pipe for a hosting application and we are logging,
                // then create a temporary PowerShell to log the error.
                if (Context.InternalHost.UI.IsTranscribing)
                {
                    Context.InternalHost.UI.TranscribeError(Context, errorRecord.InvocationInfo, errorWrap);
                }

                this.ErrorOutputPipe.AddWithoutAppendingOutVarList(errorWrap);
            }
        }
        #endregion WriteError

        #region Preference

        // These are a set of preference variables which affect the inner
        // workings of the command and when what information will get output.
        // See "User Feedback Mechanisms - Note.doc" for details.

        private bool _isConfirmPreferenceCached = false;
        private ConfirmImpact _confirmPreference = InitialSessionState.DefaultConfirmPreference;
        /// <summary>
        /// Preference setting controlling behavior of ShouldProcess()
        /// </summary>
        /// <remarks>
        /// This is not an independent parameter, it just emerges from the
        /// Verbose, Debug, Confirm, and WhatIf parameters and the
        /// $ConfirmPreference shell variable.
        ///
        /// We only read $ConfirmPreference once, then cache the value.
        /// </remarks>
        internal ConfirmImpact ConfirmPreference
        {
            get
            {
                // WhatIf not relevant, it never gets this far in that case
                if (Confirm)
                    return ConfirmImpact.Low;
                if (Debug)
                {
                    if (IsConfirmFlagSet) // -Debug -Confirm:$false
                        return ConfirmImpact.None;
                    return ConfirmImpact.Low;
                }

                if (IsConfirmFlagSet) // -Confirm:$false
                    return ConfirmImpact.None;

                if (!_isConfirmPreferenceCached)
                {
                    bool defaultUsed = false;
                    _confirmPreference = Context.GetEnumPreference(SpecialVariables.ConfirmPreferenceVarPath, _confirmPreference, out defaultUsed);
                    _isConfirmPreferenceCached = true;
                }

                return _confirmPreference;
            }
        }

        private bool _isDebugPreferenceSet = false;
        private ActionPreference _debugPreference = InitialSessionState.DefaultDebugPreference;
        private bool _isDebugPreferenceCached = false;
        /// <summary>
        /// Preference setting.
        /// </summary>
        /// <exception cref="System.Management.Automation.ExtendedTypeSystemException">
        /// (get-only) An error occurred accessing $DebugPreference.
        /// </exception>
        internal ActionPreference DebugPreference
        {
            get
            {
                if (_isDebugPreferenceSet)
                {
                    return _debugPreference;
                }

                if (IsDebugFlagSet)
                {
                    return Debug ? ActionPreference.Continue : ActionPreference.SilentlyContinue;
                }

                if (!_isDebugPreferenceCached)
                {
                    bool defaultUsed = false;

                    _debugPreference = Context.GetEnumPreference(SpecialVariables.DebugPreferenceVarPath, _debugPreference, out defaultUsed);

                    // If the host couldn't prompt for the debug action anyways, change it to 'Continue'.
                    // This lets hosts still see debug output without having to implement the prompting logic.
                    if ((CBhost.ExternalHost.UI == null) && (_debugPreference == ActionPreference.Inquire))
                    {
                        _debugPreference = ActionPreference.Continue;
                    }

                    _isDebugPreferenceCached = true;
                }

                return _debugPreference;
            }

            set
            {
                if (value == ActionPreference.Suspend)
                {
                    throw PSTraceSource.NewNotSupportedException(ErrorPackage.ActionPreferenceReservedForFutureUseError, value);
                }

                _debugPreference = value;
                _isDebugPreferenceSet = true;
            }
        }

        private readonly bool _isVerbosePreferenceCached = false;
        private ActionPreference _verbosePreference = InitialSessionState.DefaultVerbosePreference;
        /// <summary>
        /// Preference setting.
        /// </summary>
        /// <exception cref="System.Management.Automation.ExtendedTypeSystemException">
        /// An error occurred accessing $VerbosePreference.
        /// </exception>
        internal ActionPreference VerbosePreference
        {
            get
            {
                if (IsVerboseFlagSet)
                {
                    if (Verbose)
                        return ActionPreference.Continue;
                    else
                        return ActionPreference.SilentlyContinue;
                }

                if (Debug)
                {
                    // If the host couldn't prompt for the debug action anyways, use 'Continue'.
                    // This lets hosts still see debug output without having to implement the prompting logic.
                    if (CBhost.ExternalHost.UI == null)
                    {
                        return ActionPreference.Continue;
                    }
                    else
                    {
                        return ActionPreference.Inquire;
                    }
                }

                if (!_isVerbosePreferenceCached)
                {
                    bool defaultUsed = false;
                    _verbosePreference = Context.GetEnumPreference(
                        SpecialVariables.VerbosePreferenceVarPath,
                        _verbosePreference,
                        out defaultUsed);
                }

                return _verbosePreference;
            }
        }

        internal bool IsWarningActionSet { get; private set; } = false;

        private readonly bool _isWarningPreferenceCached = false;
        private ActionPreference _warningPreference = InitialSessionState.DefaultWarningPreference;
        /// <summary>
        /// Preference setting.
        /// </summary>
        /// <exception cref="System.Management.Automation.ExtendedTypeSystemException">
        /// An error occurred accessing $WarningPreference.
        /// </exception>
        internal ActionPreference WarningPreference
        {
            get
            {
                // Setting CommonParameters.WarningAction has highest priority
                if (IsWarningActionSet)
                    return _warningPreference;

                if (Debug)
                    return ActionPreference.Inquire;
                if (Verbose)
                    return ActionPreference.Continue;
                // Debug:$false and Verbose:$false ignored

                if (!_isWarningPreferenceCached)
                {
                    bool defaultUsed = false;
                    _warningPreference = Context.GetEnumPreference(SpecialVariables.WarningPreferenceVarPath, _warningPreference, out defaultUsed);
                }

                return _warningPreference;
            }

            set
            {
                if (value == ActionPreference.Suspend)
                {
                    throw PSTraceSource.NewNotSupportedException(ErrorPackage.ActionPreferenceReservedForFutureUseError, value);
                }

                _warningPreference = value;
                IsWarningActionSet = true;
            }
        }

        // This is used so that people can tell whether the verbose switch
        // was specified.  This is useful in the Cmdlet-calling-Cmdlet case
        // where you'd like the underlying Cmdlet to have the same switches.
        private bool _verboseFlag = false;

        /// <summary>
        /// Echo tells the command to articulate the actions it performs while executing.
        /// </summary>
        /// <remarks>
        /// This is a common parameter via class CommonParameters.
        /// </remarks>
        internal bool Verbose
        {
            get
            {
                return _verboseFlag;
            }

            set
            {
                _verboseFlag = value;
                IsVerboseFlagSet = true;
            }
        }

        internal bool IsVerboseFlagSet { get; private set; } = false;

        private bool _confirmFlag = false;

        /// <summary>
        /// Confirm tells the command to ask the admin before performing an action.
        /// </summary>
        /// <remarks>
        /// This is a common parameter via class ShouldProcessParameters.
        /// </remarks>
        internal SwitchParameter Confirm
        {
            get
            {
                return _confirmFlag;
            }

            set
            {
                _confirmFlag = value;
                IsConfirmFlagSet = true;
            }
        }

        internal bool IsConfirmFlagSet { get; private set; } = false;

        private bool _useTransactionFlag = false;

        /// <summary>
        /// UseTransaction tells the command to activate the current PowerShell transaction.
        /// </summary>
        /// <remarks>
        /// This is a common parameter via class TransactionParameters.
        /// </remarks>
        internal SwitchParameter UseTransaction
        {
            get
            {
                return _useTransactionFlag;
            }

            set
            {
                _useTransactionFlag = value;
                UseTransactionFlagSet = true;
            }
        }

        internal bool UseTransactionFlagSet { get; private set; } = false;

        // This is used so that people can tell whether the debug switch was specified.  This
        // Is useful in the Cmdlet-calling-Cmdlet case where you'd like the underlying Cmdlet to
        // have the same switches.
        private bool _debugFlag = false;

        /// <summary>
        /// Debug tell the command system to provide Programmer/Support type messages to understand what is really occuring
        /// and give the user the opportunity to stop or debug the situation.
        /// </summary>
        /// <remarks>
        /// This is a common parameter via class CommonParameters.
        /// </remarks>
        internal bool Debug
        {
            get
            {
                return _debugFlag;
            }

            set
            {
                _debugFlag = value;
                IsDebugFlagSet = true;
            }
        }

        internal bool IsDebugFlagSet { get; private set; } = false;

        private bool _whatIfFlag = InitialSessionState.DefaultWhatIfPreference;
        private bool _isWhatIfPreferenceCached /* = false */;
        /// <summary>
        /// WhatIf indicates that the command should not
        /// perform any changes to persistent state outside Monad.
        /// </summary>
        /// <remarks>
        /// This is a common parameter via class ShouldProcessParameters.
        /// </remarks>
        internal SwitchParameter WhatIf
        {
            get
            {
                if (!IsWhatIfFlagSet && !_isWhatIfPreferenceCached)
                {
                    _whatIfFlag = Context.GetBooleanPreference(SpecialVariables.WhatIfPreferenceVarPath, _whatIfFlag, out _);
                    _isWhatIfPreferenceCached = true;
                }

                return _whatIfFlag;
            }

            set
            {
                _whatIfFlag = value;
                IsWhatIfFlagSet = true;
            }
        }

        internal bool IsWhatIfFlagSet { get; private set; }

        private ActionPreference _errorAction = InitialSessionState.DefaultErrorActionPreference;
        private bool _isErrorActionPreferenceCached = false;
        /// <summary>
        /// ErrorAction tells the command what to do when an error occurs.
        /// </summary>
        /// <exception cref="System.Management.Automation.ExtendedTypeSystemException">
        /// (get-only) An error occurred accessing $ErrorAction.
        /// </exception>
        /// <remarks>
        /// This is a common parameter via class CommonParameters.
        /// </remarks>
        internal ActionPreference ErrorAction
        {
            get
            {
                // Setting CommonParameters.ErrorAction has highest priority
                if (IsErrorActionSet)
                    return _errorAction;

                if (!_isErrorActionPreferenceCached)
                {
                    bool defaultUsed = false;
                    _errorAction = Context.GetEnumPreference(SpecialVariables.ErrorActionPreferenceVarPath, _errorAction, out defaultUsed);
                    _isErrorActionPreferenceCached = true;
                }

                return _errorAction;
            }

            set
            {
                if (value == ActionPreference.Suspend)
                {
                    throw PSTraceSource.NewNotSupportedException(ErrorPackage.ActionPreferenceReservedForFutureUseError, value);
                }

                _errorAction = value;
                IsErrorActionSet = true;
            }
        }

        internal bool IsErrorActionSet { get; private set; } = false;

        /// <summary>
        /// Preference setting for displaying ProgressRecords when WriteProgress is called.
        /// </summary>
        /// <value></value>
        internal ActionPreference ProgressPreference
        {
            get
            {
                if (IsProgressActionSet)
                    return _progressPreference;

                if (!_isProgressPreferenceCached)
                {
                    bool defaultUsed = false;
                    _progressPreference = Context.GetEnumPreference(SpecialVariables.ProgressPreferenceVarPath, _progressPreference, out defaultUsed);
                    _isProgressPreferenceCached = true;
                }

                return _progressPreference;
            }

            set
            {
                if (value == ActionPreference.Suspend)
                {
                    throw PSTraceSource.NewNotSupportedException(ErrorPackage.ActionPreferenceReservedForFutureUseError, value);
                }

                _progressPreference = value;
                IsProgressActionSet = true;
            }
        }

        private ActionPreference _progressPreference = InitialSessionState.DefaultProgressPreference;

        internal bool IsProgressActionSet { get; private set; } = false;

        private bool _isProgressPreferenceCached = false;

        /// <summary>
        /// Preference setting for displaying InformationRecords when WriteInformation is called.
        /// </summary>
        /// <value></value>
        internal ActionPreference InformationPreference
        {
            get
            {
                if (IsInformationActionSet)
                    return _informationPreference;

                if (!_isInformationPreferenceCached)
                {
                    bool defaultUsed = false;
                    _informationPreference = Context.GetEnumPreference(SpecialVariables.InformationPreferenceVarPath, _informationPreference, out defaultUsed);
                    _isInformationPreferenceCached = true;
                }

                return _informationPreference;
            }

            set
            {
                if (value == ActionPreference.Suspend)
                {
                    throw PSTraceSource.NewNotSupportedException(ErrorPackage.ActionPreferenceReservedForFutureUseError, value);
                }

                _informationPreference = value;
                IsInformationActionSet = true;
            }
        }

        private ActionPreference _informationPreference = InitialSessionState.DefaultInformationPreference;

        internal bool IsInformationActionSet { get; private set; } = false;

        private bool _isInformationPreferenceCached = false;

        internal PagingParameters PagingParameters { get; set; }

        #endregion Preference

        #region Continue/Confirm

        #region Helpers

        /// <summary>
        /// ContinueStatus indicates the last reply from the user
        /// whether or not the command should process an object.
        /// </summary>
        internal enum ContinueStatus
        {
            Yes,
            No,
            YesToAll,
            NoToAll
        }

        internal ContinueStatus lastShouldProcessContinueStatus = ContinueStatus.Yes;
        internal ContinueStatus lastErrorContinueStatus = ContinueStatus.Yes;
        internal ContinueStatus lastDebugContinueStatus = ContinueStatus.Yes;
        internal ContinueStatus lastVerboseContinueStatus = ContinueStatus.Yes;
        internal ContinueStatus lastWarningContinueStatus = ContinueStatus.Yes;
        internal ContinueStatus lastProgressContinueStatus = ContinueStatus.Yes;
        internal ContinueStatus lastInformationContinueStatus = ContinueStatus.Yes;

        /// <summary>
        /// Should the verbose/debug/progress message be printed?
        /// </summary>
        /// <param name="preference"></param>
        /// <param name="lastContinueStatus"></param>
        /// <returns></returns>
        /// <exception cref="System.Management.Automation.PipelineStoppedException"></exception>
        /// <exception cref="System.InvalidOperationException"></exception>
        internal bool WriteHelper_ShouldWrite(
            ActionPreference preference,
            ContinueStatus lastContinueStatus)
        {
            ThrowIfStopping();

            // 2005/05/24 908827
            // WriteDebug/WriteVerbose/WriteProgress/WriteWarning should only be callable from the main thread
            //
            // WriteError/WriteObject have a check that prevents them to be called from outside
            // Begin/Process/End. This is done because the Pipeline needs to be ready before these
            // functions can be called.
            //
            // WriteDebug/Warning/Verbose/Process used to do the same check, even though it is not
            // strictly needed. If we ever implement pipelines for these objects we may need to
            // enforce the check again.
            //
            // See bug 583774 in the Windows 7 database for more details.
            //
            ThrowIfWriteNotPermitted(false);

            switch (lastContinueStatus)
            {
                case ContinueStatus.NoToAll:  // previously answered NoToAll
                    return false;
                case ContinueStatus.YesToAll: // previously answered YesToAll
                    return true;
            }

            switch (preference)
            {
                case ActionPreference.Ignore:
                case ActionPreference.SilentlyContinue:
                    return false;

                case ActionPreference.Continue:
                case ActionPreference.Stop:
                case ActionPreference.Inquire:
                case ActionPreference.Break:
                    return true;

                default:
                    Dbg.Assert(false, "Bad preference value" + preference);
                    return true;
            }
        }

        /// <summary>
        /// Complete implementation of WriteDebug/WriteVerbose/WriteProgress.
        /// </summary>
        /// <param name="inquireCaption"></param>
        /// <param name="inquireMessage"></param>
        /// <param name="preference"></param>
        /// <param name="lastContinueStatus"></param>
        /// <param name="preferenceVariableName"></param>
        /// <param name="message"></param>
        /// <returns>Did Inquire return YesToAll?.</returns>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The pipeline has already been terminated, or was terminated
        /// during the execution of this method.
        /// The Cmdlet should generally just allow PipelineStoppedException
        /// to percolate up to the caller of ProcessRecord etc.
        /// </exception>
        /// <remarks>
        /// If the pipeline is terminated due to ActionPreference.Stop
        /// or ActionPreference.Inquire, this method will throw
        /// <see cref="System.Management.Automation.PipelineStoppedException"/>,
        /// but the command failure will ultimately be
        /// <see cref="System.Management.Automation.ActionPreferenceStopException"/>,
        /// </remarks>
        internal ContinueStatus WriteHelper(
            string inquireCaption,
            string inquireMessage,
            ActionPreference preference,
            ContinueStatus lastContinueStatus,
            string preferenceVariableName,
            string message)
        {
            switch (lastContinueStatus)
            {
                case ContinueStatus.NoToAll:  // previously answered NoToAll
                    return ContinueStatus.NoToAll;
                case ContinueStatus.YesToAll: // previously answered YesToAll
                    return ContinueStatus.YesToAll;
            }

            switch (preference)
            {
                case ActionPreference.Ignore: // YesToAll
                case ActionPreference.SilentlyContinue:
                case ActionPreference.Continue:
                case ActionPreference.Break:
                    return ContinueStatus.Yes;

                case ActionPreference.Stop:
                    ActionPreferenceStopException e =
                        new ActionPreferenceStopException(
                            MyInvocation,
                            StringUtil.Format(CommandBaseStrings.ErrorPreferenceStop, preferenceVariableName, message));
                    throw ManageException(e);

                case ActionPreference.Inquire:
                    break;

                default:
                    Dbg.Assert(false, "Bad preference value" + preference);
                    ActionPreferenceStopException apse =
                        new ActionPreferenceStopException(
                            MyInvocation,
                            StringUtil.Format(CommandBaseStrings.PreferenceInvalid, preferenceVariableName, preference));
                    throw ManageException(apse);
            }

            return InquireHelper(
                inquireMessage,
                inquireCaption,
                true,  // allowYesToAll
                false, // allowNoToAll
                true,  // replaceNoWithHalt
                false  // hasSecurityImpact
            );
        }

        /// <summary>
        /// Helper for continue prompt, handles Inquire.
        /// </summary>
        /// <param name="inquireMessage">May be null.</param>
        /// <param name="inquireCaption">May be null.</param>
        /// <param name="allowYesToAll"></param>
        /// <param name="allowNoToAll"></param>
        /// <param name="replaceNoWithHalt"></param>
        /// <param name="hasSecurityImpact"></param>
        /// <returns>User's selection.</returns>
        /// <exception cref="System.Management.Automation.PipelineStoppedException">
        /// The pipeline has already been terminated, or was terminated
        /// during the execution of this method.
        /// The Cmdlet should generally just allow PipelineStoppedException
        /// to percolate up to the caller of ProcessRecord etc.
        /// </exception>
        /// <remarks>
        /// If the pipeline is terminated due to ActionPreference.Stop
        /// or ActionPreference.Inquire, this method will throw
        /// <see cref="System.Management.Automation.PipelineStoppedException"/>,
        /// but the command failure will ultimately be
        /// <see cref="System.Management.Automation.ActionPreferenceStopException"/>,
        /// </remarks>
        internal ContinueStatus InquireHelper(
            string inquireMessage,
            string inquireCaption,
            bool allowYesToAll,
            bool allowNoToAll,
            bool replaceNoWithHalt,
            bool hasSecurityImpact
            )
        {
            Collection<ChoiceDescription> choices =
                new Collection<ChoiceDescription>();
            int currentOption = 0;

            int continueOneOption = Int32.MaxValue,
                continueAllOption = Int32.MaxValue,
                haltOption = Int32.MaxValue,
                skipOneOption = Int32.MaxValue,
                skipAllOption = Int32.MaxValue,
                pauseOption = Int32.MaxValue;

            string continueOneLabel = CommandBaseStrings.ContinueOneLabel;
            string continueOneHelpMsg = CommandBaseStrings.ContinueOneHelpMessage;
            choices.Add(new ChoiceDescription(continueOneLabel, continueOneHelpMsg));
            continueOneOption = currentOption++;

            if (allowYesToAll)
            {
                string continueAllLabel = CommandBaseStrings.ContinueAllLabel;
                string continueAllHelpMsg = CommandBaseStrings.ContinueAllHelpMessage;
                choices.Add(new ChoiceDescription(continueAllLabel, continueAllHelpMsg));
                continueAllOption = currentOption++;
            }

            if (replaceNoWithHalt)
            {
                string haltLabel = CommandBaseStrings.HaltLabel;
                string haltHelpMsg = CommandBaseStrings.HaltHelpMessage;
                choices.Add(new ChoiceDescription(haltLabel, haltHelpMsg));
                haltOption = currentOption++;
            }
            else
            {
                string skipOneLabel = CommandBaseStrings.SkipOneLabel;
                string skipOneHelpMsg = CommandBaseStrings.SkipOneHelpMessage;
                choices.Add(new ChoiceDescription(skipOneLabel, skipOneHelpMsg));
                skipOneOption = currentOption++;
            }

            if (allowNoToAll)
            {
                string skipAllLabel = CommandBaseStrings.SkipAllLabel;
                string skipAllHelpMsg = CommandBaseStrings.SkipAllHelpMessage;
                choices.Add(new ChoiceDescription(skipAllLabel, skipAllHelpMsg));
                skipAllOption = currentOption++;
            }

            // Hide the "Suspend" option in the remoting case since that is not supported. If the user chooses
            // Suspend that will produce an error message. Why show the user an option that the user cannot use?
            // Related to bug Win7/116823.
            if (IsSuspendPromptAllowed())
            {
                string pauseLabel = CommandBaseStrings.PauseLabel;
                string pauseHelpMsg = StringUtil.Format(CommandBaseStrings.PauseHelpMessage, "exit");
                choices.Add(new ChoiceDescription(pauseLabel, pauseHelpMsg));
                pauseOption = currentOption++;
            }

            if (string.IsNullOrEmpty(inquireMessage))
            {
                inquireMessage = CommandBaseStrings.ShouldContinuePromptCaption;
            }

            if (string.IsNullOrEmpty(inquireCaption))
            {
                inquireCaption = CommandBaseStrings.InquireCaptionDefault;
            }

            while (true)
            {
                // Transcribe the confirmation message
                CBhost.InternalUI.TranscribeResult(inquireCaption);
                CBhost.InternalUI.TranscribeResult(inquireMessage);

                Text.StringBuilder textChoices = new Text.StringBuilder();
                foreach (ChoiceDescription choice in choices)
                {
                    if (textChoices.Length > 0)
                    {
                        textChoices.Append("  ");
                    }

                    textChoices.Append(choice.Label);
                }

                CBhost.InternalUI.TranscribeResult(textChoices.ToString());

                int defaultOption = 0;
                if (hasSecurityImpact)
                {
                    defaultOption = skipOneOption;
                }

                int response = this.CBhost.UI.PromptForChoice(
                    inquireCaption, inquireMessage, choices, defaultOption);

                string chosen = choices[response].Label;
                int labelIndex = chosen.IndexOf('&');
                if (labelIndex > -1)
                {
                    chosen = chosen[labelIndex + 1].ToString();
                }

                CBhost.InternalUI.TranscribeResult(chosen);

                if (continueOneOption == response)
                    return ContinueStatus.Yes;
                else if (continueAllOption == response)
                    return ContinueStatus.YesToAll;
                else if (haltOption == response)
                {
                    ActionPreferenceStopException e =
                        new ActionPreferenceStopException(
                            MyInvocation,
                            CommandBaseStrings.InquireHalt);
                    throw ManageException(e);
                }
                else if (skipOneOption == response)
                    return ContinueStatus.No;
                else if (skipAllOption == response)
                    return ContinueStatus.NoToAll;
                else if (pauseOption == response)
                {
                    // This call returns when the user exits the nested prompt.
                    CBhost.EnterNestedPrompt(_thisCommand);
                    // continue loop
                }
                else if (response == -1)
                {
                    ActionPreferenceStopException e =
                        new ActionPreferenceStopException(
                            MyInvocation,
                            CommandBaseStrings.InquireCtrlC);
                    throw ManageException(e);
                }
                else
                {
                    Dbg.Assert(false, "all cases should be checked");
                    InvalidOperationException e =
                        PSTraceSource.NewInvalidOperationException();
                    throw ManageException(e);
                }
            }
        }

        /// <summary>
        /// Determines if this is being run in the context of a remote host or not.
        /// </summary>
        private bool IsSuspendPromptAllowed()
        {
            Dbg.Assert(this.CBhost != null, "Expected this.CBhost != null");
            Dbg.Assert(this.CBhost.ExternalHost != null, "Expected this.CBhost.ExternalHost != null");
            if (this.CBhost.ExternalHost is ServerRemoteHost)
            {
                return false;
            }

            return true;
        }
        #endregion Helpers

        #endregion Continue/Confirm

        internal void SetVariableListsInPipe()
        {
            Diagnostics.Assert(_thisCommand is PSScriptCmdlet, "this is only done for script cmdlets");

            if (_outVarList != null && !OutputPipe.IgnoreOutVariableList)
            {
                // A null pipe is used when executing the 'Clean' block of a PSScriptCmdlet.
                // In such a case, we don't capture output to the out variable list.
                this.OutputPipe.AddVariableList(VariableStreamKind.Output, _outVarList);
            }

            if (_errorVarList != null)
            {
                this.OutputPipe.AddVariableList(VariableStreamKind.Error, _errorVarList);
            }

            if (_warningVarList != null)
            {
                this.OutputPipe.AddVariableList(VariableStreamKind.Warning, _warningVarList);
            }

            if (_informationVarList != null)
            {
                this.OutputPipe.AddVariableList(VariableStreamKind.Information, _informationVarList);
            }

            if (this.PipelineVariable != null)
            {
                this.OutputPipe.SetPipelineVariable(_pipelineVarReference);
            }
        }

        internal void RemoveVariableListsInPipe()
        {
            if (_outVarList != null && !OutputPipe.IgnoreOutVariableList)
            {
                this.OutputPipe.RemoveVariableList(VariableStreamKind.Output, _outVarList);
            }

            if (_errorVarList != null)
            {
                this.OutputPipe.RemoveVariableList(VariableStreamKind.Error, _errorVarList);
            }

            if (_warningVarList != null)
            {
                this.OutputPipe.RemoveVariableList(VariableStreamKind.Warning, _warningVarList);
            }

            if (_informationVarList != null)
            {
                this.OutputPipe.RemoveVariableList(VariableStreamKind.Information, _informationVarList);
            }

            if (this.PipelineVariable != null)
            {
                this.OutputPipe.RemovePipelineVariable();
            }
        }
    }
}
