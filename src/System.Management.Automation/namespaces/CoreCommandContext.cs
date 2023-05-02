// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    /// <summary>
    /// The context of the core command that is being run. This
    /// includes data like the user name and password, as well
    /// as callbacks for streaming output, prompting, and progress.
    ///
    /// This allows the providers to be called in a variety of situations.
    /// The most common will be from the core cmdlets themselves but they
    /// can also be called programmatically either by having the results
    /// accumulated or by providing delegates for the various streams.
    ///
    /// NOTE:  USER Feedback mechanism are only enabled for the CoreCmdlet
    /// case.  This is because we have not seen a use-case for them in the
    /// other scenarios.
    /// </summary>
    internal sealed class CmdletProviderContext
    {
        #region Trace object

        /// <summary>
        /// An instance of the PSTraceSource class used for trace output
        /// using "CmdletProviderContext" as the category.
        /// </summary>
        [Dbg.TraceSourceAttribute(
             "CmdletProviderContext",
             "The context under which a core command is being run.")]
        private static readonly Dbg.PSTraceSource s_tracer =
            Dbg.PSTraceSource.GetTracer("CmdletProviderContext",
             "The context under which a core command is being run.");

        #endregion Trace object

        #region Constructor

        /// <summary>
        /// Constructs the context under which the core command providers
        /// operate.
        /// </summary>
        /// <param name="executionContext">
        /// The context of the engine.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="executionContext"/> is null.
        /// </exception>
        internal CmdletProviderContext(ExecutionContext executionContext)
        {
            if (executionContext == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(executionContext));
            }

            ExecutionContext = executionContext;
            Origin = CommandOrigin.Internal;
            Drive = executionContext.EngineSessionState.CurrentDrive;
            if ((executionContext.CurrentCommandProcessor != null) &&
                (executionContext.CurrentCommandProcessor.Command is Cmdlet))
            {
                _command = (Cmdlet)executionContext.CurrentCommandProcessor.Command;
            }
        }

        /// <summary>
        /// Constructs the context under which the core command providers
        /// operate.
        /// </summary>
        /// <param name="executionContext">
        /// The context of the engine.
        /// </param>
        /// <param name="origin">
        /// The origin of the caller of this API
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="executionContext"/> is null.
        /// </exception>
        internal CmdletProviderContext(ExecutionContext executionContext, CommandOrigin origin)
        {
            if (executionContext == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(executionContext));
            }

            ExecutionContext = executionContext;
            Origin = origin;
        }

        /// <summary>
        /// Constructs the context under which the core command providers
        /// operate.
        /// </summary>
        /// <param name="command">
        /// The command object that is running.
        /// </param>
        /// <param name="credentials">
        /// The credentials the core command provider should use.
        /// </param>
        /// <param name="drive">
        /// The drive under which this context should operate.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="command"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="command"/> contains a null Host or Context reference.
        /// </exception>
        internal CmdletProviderContext(
            PSCmdlet command,
            PSCredential credentials,
            PSDriveInfo drive)
        {
            // verify the command parameter
            if (command == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(command));
            }

            _command = command;
            Origin = command.CommandOrigin;

            if (credentials != null)
            {
                _credentials = credentials;
            }

            Drive = drive;

            if (command.Host == null)
            {
                throw PSTraceSource.NewArgumentException("command.Host");
            }

            if (command.Context == null)
            {
                throw PSTraceSource.NewArgumentException("command.Context");
            }

            ExecutionContext = command.Context;

            // Stream will default to true because command methods will be used.

            PassThru = true;
            _streamErrors = true;
        }

        /// <summary>
        /// Constructs the context under which the core command providers
        /// operate.
        /// </summary>
        /// <param name="command">
        /// The command object that is running.
        /// </param>
        /// <param name="credentials">
        /// The credentials the core command provider should use.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="command"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="command"/> contains a null Host or Context reference.
        /// </exception>
        internal CmdletProviderContext(
            PSCmdlet command,
            PSCredential credentials)
        {
            // verify the command parameter
            if (command == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(command));
            }

            _command = command;
            Origin = command.CommandOrigin;

            if (credentials != null)
            {
                _credentials = credentials;
            }

            if (command.Host == null)
            {
                throw PSTraceSource.NewArgumentException("command.Host");
            }

            if (command.Context == null)
            {
                throw PSTraceSource.NewArgumentException("command.Context");
            }

            ExecutionContext = command.Context;

            // Stream will default to true because command methods will be used.

            PassThru = true;
            _streamErrors = true;
        }

        /// <summary>
        /// Constructs the context under which the core command providers
        /// operate.
        /// </summary>
        /// <param name="command">
        /// The command object that is running.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="command"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="command"/> contains a null Host or Context reference.
        /// </exception>
        internal CmdletProviderContext(
            Cmdlet command)
        {
            // verify the command parameter
            if (command == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(command));
            }

            _command = command;
            Origin = command.CommandOrigin;

            if (command.Context == null)
            {
                throw PSTraceSource.NewArgumentException("command.Context");
            }

            ExecutionContext = command.Context;

            // Stream will default to true because command methods will be used.

            PassThru = true;
            _streamErrors = true;
        }

        /// <summary>
        /// Constructs the context under which the core command providers
        /// operate using an existing context.
        /// </summary>
        /// <param name="contextToCopyFrom">
        /// A CmdletProviderContext instance to copy the filters, ExecutionContext,
        /// Credentials, Drive, and Force options from.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="contextToCopyFrom"/> is null.
        /// </exception>
        internal CmdletProviderContext(
            CmdletProviderContext contextToCopyFrom)
        {
            if (contextToCopyFrom == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(contextToCopyFrom));
            }

            ExecutionContext = contextToCopyFrom.ExecutionContext;

            _command = contextToCopyFrom._command;

            if (contextToCopyFrom.Credential != null)
            {
                _credentials = contextToCopyFrom.Credential;
            }

            Drive = contextToCopyFrom.Drive;
            _force = contextToCopyFrom.Force;
            this.CopyFilters(contextToCopyFrom);
            SuppressWildcardExpansion = contextToCopyFrom.SuppressWildcardExpansion;
            DynamicParameters = contextToCopyFrom.DynamicParameters;
            Origin = contextToCopyFrom.Origin;

            // Copy the stopping state incase the source context
            // has already been signaled for stopping

            Stopping = contextToCopyFrom.Stopping;

            // add this context to the stop referral on the copied
            // context

            contextToCopyFrom.StopReferrals.Add(this);
            _copiedContext = contextToCopyFrom;
        }

        #endregion Constructor

        #region private properties

        /// <summary>
        /// If the constructor that takes a context to copy is
        /// called, this will be set to the context being copied.
        /// </summary>
        private readonly CmdletProviderContext _copiedContext;

        /// <summary>
        /// The credentials under which the operation should run.
        /// </summary>
        private readonly PSCredential _credentials = PSCredential.Empty;

        /// <summary>
        /// The force parameter gives guidance to providers on how vigorously they
        /// should try to perform an operation.
        /// </summary>
        private bool _force;

        /// <summary>
        /// The command which defines the context. This should not be
        /// made visible to anyone and should only be set through the
        /// constructor.
        /// </summary>
        private readonly Cmdlet _command;

        /// <summary>
        /// This makes the origin of the provider request visible to the internals.
        /// </summary>
        internal CommandOrigin Origin { get; } = CommandOrigin.Internal;

        /// <summary>
        /// This defines the default behavior for the WriteError method.
        /// If it is true, a call to this method will result in an immediate call
        /// to the command WriteError method, or to the writeErrorDelegate if
        /// one has been supplied.
        /// If it is false, the objects will be accumulated until the
        /// GetErrorObjects method is called.
        /// </summary>
        private readonly bool _streamErrors;

        /// <summary>
        /// A collection in which objects that are written using the WriteObject(s)
        /// methods are accumulated if <see cref="PassThru"/> is false.
        /// </summary>
        private Collection<PSObject> _accumulatedObjects = new Collection<PSObject>();

        /// <summary>
        /// A collection in which objects that are written using the WriteError
        /// method are accumulated if <see cref="PassThru"/> is false.
        /// </summary>
        private Collection<ErrorRecord> _accumulatedErrorObjects = new Collection<ErrorRecord>();

        /// <summary>
        /// The instance of the provider that is currently executing in this context.
        /// </summary>
        private System.Management.Automation.Provider.CmdletProvider _providerInstance;

        #endregion private properties

        #region Internal properties

        /// <summary>
        /// Gets the execution context of the engine.
        /// </summary>
        internal ExecutionContext ExecutionContext { get; }

        /// <summary>
        /// Gets or sets the provider instance for the current
        /// execution context.
        /// </summary>
        internal System.Management.Automation.Provider.CmdletProvider ProviderInstance
        {
            get
            {
                return _providerInstance;
            }

            set
            {
                _providerInstance = value;
            }
        }

        /// <summary>
        /// Copies the include, exclude, and provider filters from
        /// the specified context to this context.
        /// </summary>
        /// <param name="context">
        /// The context to copy the filters from.
        /// </param>
        private void CopyFilters(CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                context != null,
                "The caller should have verified the context");

            Include = context.Include;
            Exclude = context.Exclude;
            Filter = context.Filter;
        }

        internal void RemoveStopReferral() => _copiedContext?.StopReferrals.Remove(this);

        #endregion Internal properties

        #region Public properties

        /// <summary>
        /// Gets or sets the dynamic parameters for the context.
        /// </summary>
        internal object DynamicParameters { get; set; }

        /// <summary>
        /// Returns MyInvocation from the underlying cmdlet.
        /// </summary>
        internal InvocationInfo MyInvocation
        {
            get
            {
                if (_command != null)
                {
                    return _command.MyInvocation;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Determines if the Write* calls should be passed through to the command
        /// instance if there is one.  The default value is true.
        /// </summary>
        internal bool PassThru { get; set; }

        /// <summary>
        /// The drive associated with this context.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="value"/> is null on set.
        /// </exception>
        internal PSDriveInfo Drive { get; set; }

        /// <summary>
        /// Gets the user name under which the operation should run.
        /// </summary>
        internal PSCredential Credential
        {
            get
            {
                PSCredential result = _credentials;

                // If the username wasn't specified, use the drive credentials

                if (_credentials == null && Drive != null)
                {
                    result = Drive.Credential;
                }

                return result;
            }
        }

        #region Transaction Support

        /// <summary>
        /// Gets the flag that determines if the command requested a transaction.
        /// </summary>
        internal bool UseTransaction
        {
            get
            {
                if ((_command != null) && (_command.CommandRuntime != null))
                {
                    MshCommandRuntime mshRuntime = _command.CommandRuntime as MshCommandRuntime;

                    if (mshRuntime != null)
                    {
                        return mshRuntime.UseTransaction;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Returns true if a transaction is available and active.
        /// </summary>
        public bool TransactionAvailable()
        {
            if (_command != null)
            {
                return _command.TransactionAvailable();
            }

            return false;
        }

        /// <summary>
        /// Gets an object that surfaces the current PowerShell transaction.
        /// When this object is disposed, PowerShell resets the active transaction.
        /// </summary>
        public PSTransactionContext CurrentPSTransaction
        {
            get
            {
                if (_command != null)
                {
                    return _command.CurrentPSTransaction;
                }

                return null;
            }
        }
        #endregion Transaction Support

        /// <summary>
        /// Gets or sets the Force property that is passed to providers.
        /// </summary>
        internal SwitchParameter Force
        {
            get { return _force; }

            set { _force = value; }
        }

        /// <summary>
        /// The provider specific filter that should be used when determining
        /// which items an action should take place on.
        /// </summary>
        internal string Filter { get; set; }

        /// <summary>
        /// A glob string that signifies which items should be included when determining
        /// which items the action should occur on.
        /// </summary>
        internal Collection<string> Include { get; private set; }

        /// <summary>
        /// A glob string that signifies which items should be excluded when determining
        /// which items the action should occur on.
        /// </summary>
        internal Collection<string> Exclude { get; private set; }

        /// <summary>
        /// Gets or sets the property that tells providers (that
        /// declare their own wildcard support) to suppress wildcard
        /// expansion. This is set when the user specifies the
        /// -LiteralPath parameter to one of the core commands.
        /// </summary>
        public bool SuppressWildcardExpansion { get; internal set; }

        #region User feedback mechanisms

        /// <summary>
        /// Confirm the operation with the user.
        /// </summary>
        /// <param name="target">
        /// Name of the target resource being acted upon
        /// </param>
        /// <remarks>true iff the action should be performed</remarks>
        /// <exception cref="PipelineStoppedException">
        /// The ActionPreference.Stop or ActionPreference.Inquire policy
        /// triggered a terminating error.  The pipeline failure will be
        /// ActionPreferenceStopException.
        /// Also, this occurs if the pipeline was already stopped.
        /// </exception>
        internal bool ShouldProcess(
            string target)
        {
            bool result = true;
            if (_command != null)
            {
                result = _command.ShouldProcess(target);
            }

            return result;
        }

        /// <summary>
        /// Confirm the operation with the user.
        /// </summary>
        /// <param name="target">
        /// Name of the target resource being acted upon
        /// </param>
        /// <param name="action">What action was being performed.</param>
        /// <remarks>true iff the action should be performed</remarks>
        /// <exception cref="PipelineStoppedException">
        /// The ActionPreference.Stop or ActionPreference.Inquire policy
        /// triggered a terminating error.  The pipeline failure will be
        /// ActionPreferenceStopException.
        /// Also, this occurs if the pipeline was already stopped.
        /// </exception>
        internal bool ShouldProcess(
            string target,
            string action)
        {
            bool result = true;
            if (_command != null)
            {
                result = _command.ShouldProcess(target, action);
            }

            return result;
        }

        /// <summary>
        /// Confirm the operation with the user.
        /// </summary>
        /// <param name="verboseDescription">
        /// This should contain a textual description of the action to be
        /// performed.  This is what will be displayed to the user for
        /// ActionPreference.Continue.
        /// </param>
        /// <param name="verboseWarning">
        /// This should contain a textual query of whether the action
        /// should be performed, usually in the form of a question.
        /// This is what will be displayed to the user for
        /// ActionPreference.Inquire.
        /// </param>
        /// <param name="caption">
        /// This is the caption of the window which may be displayed
        /// if the user is prompted whether or not to perform the action.
        /// It may be displayed by some hosts, but not all.
        /// </param>
        /// <remarks>true iff the action should be performed</remarks>
        /// <exception cref="PipelineStoppedException">
        /// The ActionPreference.Stop or ActionPreference.Inquire policy
        /// triggered a terminating error.  The pipeline failure will be
        /// ActionPreferenceStopException.
        /// Also, this occurs if the pipeline was already stopped.
        /// </exception>
        internal bool ShouldProcess(
            string verboseDescription,
            string verboseWarning,
            string caption)
        {
            bool result = true;
            if (_command != null)
            {
                result = _command.ShouldProcess(
                    verboseDescription,
                    verboseWarning,
                    caption);
            }

            return result;
        }

        /// <summary>
        /// Confirm the operation with the user.
        /// </summary>
        /// <param name="verboseDescription">
        /// This should contain a textual description of the action to be
        /// performed.  This is what will be displayed to the user for
        /// ActionPreference.Continue.
        /// </param>
        /// <param name="verboseWarning">
        /// This should contain a textual query of whether the action
        /// should be performed, usually in the form of a question.
        /// This is what will be displayed to the user for
        /// ActionPreference.Inquire.
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
        /// <exception cref="PipelineStoppedException">
        /// The ActionPreference.Stop or ActionPreference.Inquire policy
        /// triggered a terminating error.  The pipeline failure will be
        /// ActionPreferenceStopException.
        /// Also, this occurs if the pipeline was already stopped.
        /// </exception>
        internal bool ShouldProcess(
            string verboseDescription,
            string verboseWarning,
            string caption,
            out ShouldProcessReason shouldProcessReason)
        {
            bool result = true;
            if (_command != null)
            {
                result = _command.ShouldProcess(
                    verboseDescription,
                    verboseWarning,
                    caption,
                    out shouldProcessReason);
            }
            else
            {
                shouldProcessReason = ShouldProcessReason.None;
            }

            return result;
        }

        /// <summary>
        /// Ask the user whether to continue/stop or break to a subshell.
        /// </summary>
        /// <param name="query">
        /// Message to display to the user. This routine will append
        /// the text "Continue" to ensure that people know what question
        /// they are answering.
        /// </param>
        /// <param name="caption">
        /// Dialog caption if the host uses a dialog.
        /// </param>
        /// <returns>
        /// True if the user wants to continue, false if not.
        /// </returns>
        internal bool ShouldContinue(
            string query,
            string caption)
        {
            bool result = true;
            if (_command != null)
            {
                result = _command.ShouldContinue(query, caption);
            }

            return result;
        }

        /// <summary>
        /// Ask the user whether to continue/stop or break to a subshell.
        /// </summary>
        /// <param name="query">
        /// Message to display to the user. This routine will append
        /// the text "Continue" to ensure that people know what question
        /// they are answering.
        /// </param>
        /// <param name="caption">
        /// Dialog caption if the host uses a dialog.
        /// </param>
        /// <param name="yesToAll">
        /// Indicates whether the user selected YesToAll
        /// </param>
        /// <param name="noToAll">
        /// Indicates whether the user selected NoToAll
        /// </param>
        /// <returns>
        /// True if the user wants to continue, false if not.
        /// </returns>
        internal bool ShouldContinue(
            string query,
            string caption,
            ref bool yesToAll,
            ref bool noToAll)
        {
            bool result = true;
            if (_command != null)
            {
                result = _command.ShouldContinue(
                    query, caption, ref yesToAll, ref noToAll);
            }
            else
            {
                yesToAll = false;
                noToAll = false;
            }

            return result;
        }

        /// <summary>
        /// Writes the object to the Verbose pipe.
        /// </summary>
        /// <param name="text">
        /// The string that needs to be written.
        /// </param>
        internal void WriteVerbose(string text) => _command?.WriteVerbose(text);

        /// <summary>
        /// Writes the object to the Warning pipe.
        /// </summary>
        /// <param name="text">
        /// The string that needs to be written.
        /// </param>
        internal void WriteWarning(string text) => _command?.WriteWarning(text);

        internal void WriteProgress(ProgressRecord record) => _command?.WriteProgress(record);

        /// <summary>
        /// Writes a debug string.
        /// </summary>
        /// <param name="text">
        /// The String that needs to be written.
        /// </param>
        internal void WriteDebug(string text) => _command?.WriteDebug(text);

        internal void WriteInformation(InformationRecord record) => _command?.WriteInformation(record);

        internal void WriteInformation(object messageData, string[] tags) => _command?.WriteInformation(messageData, tags);

        #endregion User feedback mechanisms

        #endregion Public properties

        #region Public methods

        /// <summary>
        /// Sets the filters that are used within this context.
        /// </summary>
        /// <param name="include">
        /// The include filters which determines which items are included in
        /// operations within this context.
        /// </param>
        /// <param name="exclude">
        /// The exclude filters which determines which items are excluded from
        /// operations within this context.
        /// </param>
        /// <param name="filter">
        /// The provider specific filter for the operation.
        /// </param>
        internal void SetFilters(Collection<string> include, Collection<string> exclude, string filter)
        {
            Include = include;
            Exclude = exclude;
            Filter = filter;
        }

        /// <summary>
        /// Gets an array of the objects that have been accumulated
        /// and the clears the collection.
        /// </summary>
        /// <returns>
        /// An object array of the objects that have been accumulated
        /// through the WriteObject method.
        /// </returns>
        internal Collection<PSObject> GetAccumulatedObjects()
        {
            // Get the contents as an array

            Collection<PSObject> results = _accumulatedObjects;
            _accumulatedObjects = new Collection<PSObject>();

            // Return the array

            return results;
        }

        /// <summary>
        /// Gets an array of the error objects that have been accumulated
        /// and the clears the collection.
        /// </summary>
        /// <returns>
        /// An object array of the objects that have been accumulated
        /// through the WriteError method.
        /// </returns>
        internal Collection<ErrorRecord> GetAccumulatedErrorObjects()
        {
            // Get the contents as an array

            Collection<ErrorRecord> results = _accumulatedErrorObjects;
            _accumulatedErrorObjects = new Collection<ErrorRecord>();

            // Return the array

            return results;
        }

        /// <summary>
        /// If there are any errors accumulated, the first error is thrown.
        /// </summary>
        /// <exception cref="ProviderInvocationException">
        /// If a CmdletProvider wrote any exceptions to the error pipeline, it is
        /// wrapped and then thrown.
        /// </exception>
        internal void ThrowFirstErrorOrDoNothing()
        {
            ThrowFirstErrorOrDoNothing(true);
        }

        /// <summary>
        /// If there are any errors accumulated, the first error is thrown.
        /// </summary>
        /// <param name="wrapExceptionInProviderException">
        /// If true, the error will be wrapped in a ProviderInvocationException before
        /// being thrown. If false, the error will be thrown as is.
        /// </param>
        /// <exception cref="ProviderInvocationException">
        /// If <paramref name="wrapExceptionInProviderException"/> is true, the
        /// first exception that was written to the error pipeline by a CmdletProvider
        /// is wrapped and thrown.
        /// </exception>
        /// <exception>
        /// If <paramref name="wrapExceptionInProviderException"/> is false,
        /// the first exception that was written to the error pipeline by a CmdletProvider
        /// is thrown.
        /// </exception>
        internal void ThrowFirstErrorOrDoNothing(bool wrapExceptionInProviderException)
        {
            if (HasErrors())
            {
                Collection<ErrorRecord> errors = GetAccumulatedErrorObjects();

                if (errors != null && errors.Count > 0)
                {
                    // Throw the first exception

                    if (wrapExceptionInProviderException)
                    {
                        ProviderInfo providerInfo = null;
                        if (this.ProviderInstance != null)
                        {
                            providerInfo = this.ProviderInstance.ProviderInfo;
                        }

                        ProviderInvocationException e =
                            new ProviderInvocationException(
                                providerInfo,
                                errors[0]);

                        // Log a provider health event

                        MshLog.LogProviderHealthEvent(
                            this.ExecutionContext,
                            providerInfo != null ? providerInfo.Name : "unknown provider",
                            e,
                            Severity.Warning);

                        throw e;
                    }
                    else
                    {
                        throw errors[0].Exception;
                    }
                }
            }
        }

        /// <summary>
        /// Writes all the accumulated errors to the specified context using WriteError.
        /// </summary>
        /// <param name="errorContext">
        /// The context to write the errors to.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="errorContext"/> is null.
        /// </exception>
        internal void WriteErrorsToContext(CmdletProviderContext errorContext)
        {
            if (errorContext == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(errorContext));
            }

            if (HasErrors())
            {
                foreach (ErrorRecord errorRecord in GetAccumulatedErrorObjects())
                {
                    errorContext.WriteError(errorRecord);
                }
            }
        }

        /// <summary>
        /// Writes an object to the output.
        /// </summary>
        /// <param name="obj">
        /// The object to be written.
        /// </param>
        /// <remarks>
        /// If streaming is on and the writeObjectHandler was specified then the object
        /// gets written to the writeObjectHandler. If streaming is on and the writeObjectHandler
        /// was not specified and the command object was specified, the object gets written to
        /// the WriteObject method of the command object.
        /// If streaming is off the object gets written to an accumulator collection. The collection
        /// of written object can be retrieved using the AccumulatedObjects method.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// The CmdletProvider could not stream the results because no
        /// cmdlet was specified to stream the output through.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// If the pipeline has been signaled for stopping but
        /// the provider calls this method.
        /// </exception>
        internal void WriteObject(object obj)
        {
            // Making sure to obey the StopProcessing by
            // throwing an exception anytime a provider tries
            // to WriteObject

            if (Stopping)
            {
                PipelineStoppedException stopPipeline =
                    new PipelineStoppedException();

                throw stopPipeline;
            }

            if (PassThru)
            {
                if (_command != null)
                {
                    s_tracer.WriteLine("Writing to command pipeline");

                    // Since there was no writeObject handler use
                    // the command WriteObject method.

                    _command.WriteObject(obj);
                }
                else
                {
                    // The flag was set for streaming but we have no where
                    // to stream to.

                    InvalidOperationException e =
                        PSTraceSource.NewInvalidOperationException(
                            SessionStateStrings.OutputStreamingNotEnabled);
                    throw e;
                }
            }
            else
            {
                s_tracer.WriteLine("Writing to accumulated objects");

                // Convert the object to a PSObject if it's not already
                // one.

                PSObject newObj = PSObject.AsPSObject(obj);

                // Since we are not streaming, just add the object to the accumulatedObjects

                _accumulatedObjects.Add(newObj);
            }
        }

        /// <summary>
        /// Writes the error to the pipeline or accumulates the error in an internal
        /// buffer.
        /// </summary>
        /// <param name="errorRecord">
        /// The error record to write to the pipeline or the internal buffer.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The CmdletProvider could not stream the error because no
        /// cmdlet was specified to stream the output through.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// If the pipeline has been signaled for stopping but
        /// the provider calls this method.
        /// </exception>
        internal void WriteError(ErrorRecord errorRecord)
        {
            // Making sure to obey the StopProcessing by
            // throwing an exception anytime a provider tries
            // to WriteError

            if (Stopping)
            {
                PipelineStoppedException stopPipeline =
                    new PipelineStoppedException();

                throw stopPipeline;
            }

            if (_streamErrors)
            {
                if (_command != null)
                {
                    s_tracer.WriteLine("Writing error package to command error pipe");

                    _command.WriteError(errorRecord);
                }
                else
                {
                    InvalidOperationException e =
                        PSTraceSource.NewInvalidOperationException(
                            SessionStateStrings.ErrorStreamingNotEnabled);
                    throw e;
                }
            }
            else
            {
                // Since we are not streaming, just add the object to the accumulatedErrorObjects
                _accumulatedErrorObjects.Add(errorRecord);

                if (errorRecord.ErrorDetails != null
                    && errorRecord.ErrorDetails.TextLookupError != null)
                {
                    Exception textLookupError = errorRecord.ErrorDetails.TextLookupError;
                    errorRecord.ErrorDetails.TextLookupError = null;
                    MshLog.LogProviderHealthEvent(
                        this.ExecutionContext,
                        this.ProviderInstance.ProviderInfo.Name,
                        textLookupError,
                        Severity.Warning);
                }
            }
        }

        /// <summary>
        /// If the error pipeline hasn't been supplied a delegate or a command then this method
        /// will determine if any errors have accumulated.
        /// </summary>
        /// <returns>
        /// True if the errors are being accumulated and some errors have been accumulated.  False otherwise.
        /// </returns>
        internal bool HasErrors()
        {
            return _accumulatedErrorObjects != null && _accumulatedErrorObjects.Count > 0;
        }

        /// <summary>
        /// Call this on a separate thread when a provider is using
        /// this context to do work. This method will call the StopProcessing
        /// method of the provider.
        /// </summary>
        internal void StopProcessing()
        {
            Stopping = true;

            // We don't need to catch any of the exceptions here because
            // we are terminating the pipeline and any exception will
            // be caught by the engine.
            _providerInstance?.StopProcessing();

            // Call the stop referrals if any

            foreach (CmdletProviderContext referralContext in StopReferrals)
            {
                referralContext.StopProcessing();
            }
        }

        internal bool Stopping { get; private set; }

        /// <summary>
        /// The list of contexts to which the StopProcessing calls
        /// should be referred.
        /// </summary>
        internal Collection<CmdletProviderContext> StopReferrals { get; } = new Collection<CmdletProviderContext>();

        internal bool HasIncludeOrExclude
        {
            get
            {
                return ((Include != null && Include.Count > 0) ||
                        (Exclude != null && Exclude.Count > 0));
            }
        }

        #endregion Public methods
    }
}
