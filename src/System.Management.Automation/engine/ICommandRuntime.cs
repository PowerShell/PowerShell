// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Management.Automation.Host;

namespace System.Management.Automation
{
    /// <summary>
    /// This interface defines the set of functionality that must be implemented to directly
    /// execute an instance of a Cmdlet.
    /// </summary>
    /// <remarks>
    /// When a cmdlet is instantiated and run directly, all calls to the stream APIs will be proxied
    /// through to an instance of this class. For example, when a cmdlet calls WriteObject, the
    /// WriteObject implementation on the instance of the class implementing this interface will be
    /// called. PowerShell implementation provides a default implementation of this class for use with
    /// standalone cmdlets as well as the implementation provided for running in the engine itself.
    ///
    /// If you do want to run Cmdlet instances standalone and capture their output with more
    /// fidelity than is provided for with the default implementation, then you should create your own
    /// implementation of this class and pass it to cmdlets before calling the Cmdlet Invoke() or
    /// Execute() methods.
    /// </remarks>
    public interface ICommandRuntime
    {
        /// <summary>
        /// Returns an instance of the PSHost implementation for this environment.
        /// </summary>
        PSHost? Host { get; }
        #region Write
        /// <summary>
        /// Display debug information.
        /// </summary>
        /// <param name="text">Debug output.</param>
        /// <remarks>
        /// This API is called by the cmdlet to display debug information on the inner workings
        /// of the Cmdlet. An implementation of this interface should display this information in
        /// an appropriately distinctive manner (e.g. through a different color or in a separate
        /// status window. In simple implementations, just ignoring the text and returning is sufficient.
        /// </remarks>
        void WriteDebug(string text);

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
        void WriteError(ErrorRecord errorRecord);

        /// <summary>
        /// Called to write objects to the output pipe.
        /// </summary>
        /// <param name="sendToPipeline">
        /// The object that needs to be written.  This will be written as
        /// a single object, even if it is an enumeration.
        /// </param>
        /// <remarks>
        /// When the cmdlet wants to write a single object out, it will call this
        /// API. It is up to the implementation to decide what to do with these objects.
        /// </remarks>
        void WriteObject(object? sendToPipeline);

        /// <summary>
        /// Called to write one or more objects to the output pipe.
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
        /// <remarks>
        ///  When the cmdlet wants to write multiple objects out, it will call this
        /// API. It is up to the implementation to decide what to do with these objects.
        /// </remarks>
        void WriteObject(object? sendToPipeline, bool enumerateCollection);

        /// <summary>
        /// Called by the cmdlet to display progress information.
        /// </summary>
        /// <param name="progressRecord">Progress information.</param>
        /// <remarks>
        /// Use WriteProgress to display progress information about
        /// the activity of your Task, when the operation of your Task
        /// could potentially take a long time.
        ///
        /// By default, progress output will
        /// be displayed, although this can be configured with the
        /// ProgressPreference shell variable.
        ///
        /// The implementation of the API should display these progress records
        /// in a fashion appropriate for the application. For example, a GUI application
        /// would implement this as a progress bar of some sort.
        /// </remarks>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.WriteDebug(string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.WriteWarning(string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.WriteVerbose(string)"/>
        void WriteProgress(ProgressRecord progressRecord);

        /// <summary>
        /// Displays progress output if enabled.
        /// </summary>
        /// <param name="sourceId">
        /// Identifies which command is reporting progress
        /// </param>
        /// <param name="progressRecord">
        /// Progress status to be displayed
        /// </param>
        /// <remarks>
        /// The implementation of the API should display these progress records
        /// in a fashion appropriate for the application. For example, a GUI application
        /// would implement this as a progress bar of some sort.
        /// </remarks>
        void WriteProgress(long sourceId, ProgressRecord progressRecord);

        /// <summary>
        /// Called when the cmdlet want to display verbose information.
        /// </summary>
        /// <param name="text">Verbose output.</param>
        /// <remarks>
        /// Cmdlets use WriteVerbose to display more detailed information about
        /// the activity of the Cmdlet.  By default, verbose output will
        /// not be displayed, although this can be configured with the
        /// VerbosePreference shell variable
        /// or the -Verbose and -Debug command-line options.
        ///
        /// The implementation of this API should display this addition information
        /// in an appropriate manner e.g. in a different color in a console application
        /// or in a separate window in a GUI application.
        /// </remarks>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.WriteDebug(string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.WriteWarning(string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.WriteProgress(ProgressRecord)"/>
        void WriteVerbose(string text);

        /// <summary>
        /// Called by the cmdlet to display warning information.
        /// </summary>
        /// <param name="text">Warning output.</param>
        /// <remarks>
        /// Use WriteWarning to display warnings about
        /// the activity of your Cmdlet.  By default, warning output will
        /// be displayed, although this can be configured with the
        /// WarningPreference shell variable
        /// or the -Verbose and -Debug command-line options.
        ///
        /// The implementation of this API should display this addition information
        /// in an appropriate manner e.g. in a different color in a console application
        /// or in a separate window in a GUI application.
        /// </remarks>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.WriteDebug(string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.WriteVerbose(string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.WriteProgress(ProgressRecord)"/>
        void WriteWarning(string text);

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
        void WriteCommandDetail(string text);

        #endregion Write

        #region Should
        /// <summary>
        /// Called by the cmdlet to confirm the operation with the user.  Cmdlets which make changes
        /// (e.g. delete files, stop services etc.) should call ShouldProcess
        /// to give the user the opportunity to confirm that the operation
        /// should actually be performed.
        /// </summary>
        /// <param name="target">
        /// Name of the target resource being acted upon. This will
        /// potentially be displayed to the user.
        /// </param>
        /// <returns>
        /// If ShouldProcess returns true, the operation should be performed.
        /// If ShouldProcess returns false, the operation should not be
        /// performed, and the Cmdlet should move on to the next target resource.
        ///
        /// An implementation should prompt the user in an appropriate manner
        /// and return true or false. An alternative trivial implementation
        /// would be to just return true all the time.
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
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldProcess(string,string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldProcess(string,string,string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldProcess(string,string,string, out ShouldProcessReason)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldContinue(string,string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldContinue(string,string,ref bool,ref bool)"/>
        bool ShouldProcess(string? target);

        /// <summary>
        /// Called by a cmdlet to confirm the operation with the user.  Cmdlets which make changes
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
        /// <returns>
        /// If ShouldProcess returns true, the operation should be performed.
        /// If ShouldProcess returns false, the operation should not be
        /// performed, and the Cmdlet should move on to the next target resource.
        ///
        /// An implementation should prompt the user in an appropriate manner
        /// and return true or false. An alternative trivial implementation
        /// would be to just return true all the time.
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
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldProcess(string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldProcess(string,string,string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldProcess(string,string,string, out ShouldProcessReason)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldContinue(string,string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldContinue(string,string,ref bool,ref bool)"/>
        bool ShouldProcess(string? target, string? action);

        /// <summary>
        /// Called by a cmdlet to confirm the operation with the user.  Cmdlets which make changes
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
        ///
        /// An implementation should prompt the user in an appropriate manner
        /// and return true or false. An alternative trivial implementation
        /// would be to just return true all the time.
        /// </remarks>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldProcess(string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldProcess(string,string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldProcess(string,string,string, out ShouldProcessReason)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldContinue(string,string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldContinue(string,string,ref bool,ref bool)"/>
        bool ShouldProcess(string? verboseDescription, string? verboseWarning, string? caption);

        /// <summary>
        /// Called by a cmdlet to confirm the operation with the user.  Cmdlets which make changes
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
        ///
        /// An implementation should prompt the user in an appropriate manner
        /// and return true or false. An alternative trivial implementation
        /// would be to just return true all the time.
        /// </remarks>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldProcess(string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldProcess(string,string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldProcess(string,string,string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldContinue(string,string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldContinue(string,string,ref bool,ref bool)"/>
        bool ShouldProcess(string? verboseDescription, string? verboseWarning, string? caption, out ShouldProcessReason shouldProcessReason);

        /// <summary>
        /// Called by a cmdlet to confirm an operation or grouping of operations with the user.
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
        ///
        /// An implementation should prompt the user in an appropriate manner
        /// and return true or false. An alternative trivial implementation
        /// would be to just return true all the time.
        /// </remarks>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldContinue(string,string,ref bool,ref bool)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldProcess(string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldProcess(string,string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldProcess(string,string,string)"/>
        bool ShouldContinue(string? query, string? caption);

        /// <summary>
        /// Called to confirm an operation or grouping of operations with the user.
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
        /// true if-and-only-if user selects YesToAll.  If this is already true,
        /// ShouldContinue will bypass the prompt and return true.
        /// </param>
        /// <param name="noToAll">
        /// true if-and-only-if user selects NoToAll.  If this is already true,
        /// ShouldContinue will bypass the prompt and return false.
        /// </param>
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
        ///
        /// An implementation should prompt the user in an appropriate manner
        /// and return true or false. An alternative trivial implementation
        /// would be to just return true all the time.
        /// </remarks>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldContinue(string,string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldProcess(string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldProcess(string,string)"/>
        /// <seealso cref="System.Management.Automation.ICommandRuntime.ShouldProcess(string,string,string)"/>
        bool ShouldContinue(string? query, string? caption, ref bool yesToAll, ref bool noToAll);

        #endregion Should

        #region Transaction Support
        /// <summary>
        /// Returns true if a transaction is available and active.
        /// </summary>
        bool TransactionAvailable();

        /// <summary>
        /// Gets an object that surfaces the current PowerShell transaction.
        /// When this object is disposed, PowerShell resets the active transaction.
        /// </summary>
        PSTransactionContext? CurrentPSTransaction { get; }
        #endregion Transaction Support

        #region Misc
        #region ThrowTerminatingError
        /// <summary>
        /// This interface will be called to route fatal errors from a cmdlet.
        /// </summary>
        /// <param name="errorRecord">
        /// The error which caused the command to be terminated
        /// </param>
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
        ///
        /// It is up to the implementation of this routine to determine what
        /// if any information is to be added. It should encapsulate the
        /// error record into an exception and then throw that exception.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
        void ThrowTerminatingError(ErrorRecord errorRecord);
        #endregion ThrowTerminatingError
        #endregion misc

    }

    /// <summary>
    /// This interface defines the set of functionality that must be implemented to directly
    /// execute an instance of a Cmdlet. ICommandRuntime2 extends the ICommandRuntime interface
    /// by adding support for the informational data stream.
    /// </summary>
    public interface ICommandRuntime2 : ICommandRuntime
    {
        /// <summary>
        /// Write an informational record to the command runtime.
        /// </summary>
        /// <param name="informationRecord">The informational record that should be transmitted to the host or user.</param>
        void WriteInformation(InformationRecord informationRecord);

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
        /// true if-and-only-if user selects YesToAll.  If this is already true,
        /// ShouldContinue will bypass the prompt and return true.
        /// </param>
        /// <param name="noToAll">
        /// true if-and-only-if user selects NoToAll.  If this is already true,
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        bool ShouldContinue(string? query, string? caption, bool hasSecurityImpact, ref bool yesToAll, ref bool noToAll);
    }
}
