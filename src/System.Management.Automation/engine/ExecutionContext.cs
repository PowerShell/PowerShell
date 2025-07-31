// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Internal.Host;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

using Microsoft.PowerShell;
using Microsoft.PowerShell.Commands.Internal.Format;

namespace System.Management.Automation
{
    /// <summary>
    /// This class contains the execution context that gets passed
    /// around to commands. This is all of the information that lets you get
    /// at session state and the host interfaces.
    /// </summary>
    internal class ExecutionContext
    {
        #region Properties

        /// <summary>
        /// The events received by this runspace.
        /// </summary>
        internal PSLocalEventManager Events { get; private set; }

        internal HashSet<string> AutoLoadingModuleInProgress { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The debugger for the interpreter.
        /// </summary>
        internal ScriptDebugger Debugger
        {
            get { return _debugger; }
        }

        internal ScriptDebugger _debugger;

        internal int _debuggingMode;

        /// <summary>
        /// Reset or clear the various context managers so the runspace can be reused without contamination.
        /// </summary>
        internal void ResetManagers()
        {
            _debugger?.ResetDebugger();

            Events?.Dispose();
            Events = new PSLocalEventManager(this);

            this.transactionManager?.Dispose();
            this.transactionManager = new PSTransactionManager();
        }
        /// <summary>
        /// The tracing mode for the interpreter.
        /// </summary>
        /// <value>True if tracing is turned on, false if it's turned off.</value>
        internal int PSDebugTraceLevel
        {
            get
            {
                // Pretend that tracing is off if ignoreScriptDebug is true
                return IgnoreScriptDebug ? 0 : _debugTraceLevel;
            }

            set
            {
                _debugTraceLevel = value;
            }
        }

        private int _debugTraceLevel;

        /// <summary>
        /// The step mode for the interpreter.
        /// </summary>
        /// <value>True of stepping is turned on, false if it's turned off.</value>
        internal bool PSDebugTraceStep
        {
            get
            {
                // Pretend that tracing is off if ignoreScriptDebug is true
                return !IgnoreScriptDebug && _debugTraceStep;
            }

            set
            {
                _debugTraceStep = value;
            }
        }

        private bool _debugTraceStep;

        // Helper for generated code to handle running w/ no execution context
        internal static bool IsStrictVersion(ExecutionContext context, int majorVersion)
        {
            context ??= LocalPipeline.GetExecutionContextFromTLS();

            return (context != null) && context.IsStrictVersion(majorVersion);
        }
        /// <summary>
        /// Check to see a specific version of strict mode is enabled.  The check is always scoped,
        /// even though in version 1 the check was engine wide.
        /// </summary>
        /// <param name="majorVersion">The version for a strict check about to be performed.</param>
        /// <returns></returns>
        internal bool IsStrictVersion(int majorVersion)
        {
            SessionStateScope scope = EngineSessionState.CurrentScope;
            while (scope != null)
            {
                // If StrictModeVersion is null, we haven't seen set-strictmode, so check the parent scope.
                if (scope.StrictModeVersion != null)
                {
                    // If StrictModeVersion is not null, just check the major version.  A version of 0
                    // is the same as off to make this a simple check.
                    return (scope.StrictModeVersion.Major >= majorVersion);
                }
                // We shouldn't check global scope if we were in a module.
                if (scope == EngineSessionState.ModuleScope)
                {
                    break;
                }

                scope = scope.Parent;
            }

            // set-strictmode hasn't been used.
            return false;
        }

        /// <summary>
        /// Is true if the current statement in the interpreter should be traced...
        /// </summary>
        internal bool ShouldTraceStatement
        {
            get
            {
                // Pretend that tracing is off if ignoreScriptDebug is true
                return !IgnoreScriptDebug && (_debugTraceLevel > 0 || _debugTraceStep);
            }
        }

        /// <summary>
        /// If true, then a script command processor should rethrow the exit exception instead of
        /// simply capturing it. This is used by the -file option on the console host.
        /// </summary>
        internal bool ScriptCommandProcessorShouldRethrowExit { get; set; } = false;

        /// <summary>
        /// If this flag is set to true, script trace output
        /// will not be generated regardless of the state of the
        /// trace flag.
        /// </summary>
        /// <value>The current state of the IgnoreScriptDebug flag.</value>
        internal bool IgnoreScriptDebug { get; set; } = true;

        /// <summary>
        /// Gets the automation engine instance.
        /// </summary>
        internal AutomationEngine Engine { get; private set; }

        internal InitialSessionState InitialSessionState { get; }

        /// <summary>
        /// Added for Win8: 336382
        /// Contains the name of the previous module that was processed. This
        /// allows you to skip this module when doing a lookup.
        /// </summary>
        internal string PreviousModuleProcessed { get; set; }

        /// <summary>
        /// Added for 4980967
        /// Contains the name of the latest module that was imported,
        /// Allows "module\function" to call the function from latest imported module instead of randomly choosing the first module in the moduletable.
        /// </summary>
        internal Hashtable previousModuleImported { get; set; } = new Hashtable();

        /// <summary>
        /// Contains the name of the module currently being processed. This
        /// allows you to skip this module when doing a lookup.
        /// </summary>
        internal string ModuleBeingProcessed { get; set; }

        /// <summary>
        /// Authorization manager for this runspace.
        /// </summary>
        internal AuthorizationManager AuthorizationManager { get; private set; }

        /// <summary>
        /// Gets the appropriate provider names for the default
        /// providers based on the type of the shell
        /// (single shell or custom shell).
        /// </summary>
        internal ProviderNames ProviderNames
        {
            get
            {
                _providerNames ??= new SingleShellProviderNames();

                return _providerNames;
            }
        }

        private ProviderNames _providerNames;

        /// <summary>
        /// The module information for this engine...
        /// </summary>
        internal ModuleIntrinsics Modules { get; private set; }

        /// <summary>
        /// Get the shellID for this runspace...
        /// </summary>
        internal string ShellID
        {
            get
            {
                if (_shellId == null)
                {
                    // Use the ShellID from PSAuthorizationManager before everything else because that's what's used
                    // to check execution policy...
                    if (AuthorizationManager is PSAuthorizationManager && !string.IsNullOrEmpty(AuthorizationManager.ShellId))
                    {
                        _shellId = AuthorizationManager.ShellId;
                    }
                    else
                    {
                        // Finally fall back to the default shell id...
                        _shellId = Utils.DefaultPowerShellShellID;
                    }
                }

                return _shellId;
            }
        }

        private string _shellId;

        /// <summary>
        /// Session State with which this instance of engine works.
        /// </summary>
        internal SessionStateInternal EngineSessionState { get; set; }

        /// <summary>
        /// The default or top-level session state instance for the
        /// engine.
        /// </summary>
        internal SessionStateInternal TopLevelSessionState { get; private set; }

        /// <summary>
        /// Get the SessionState facade for the internal session state APIs.
        /// </summary>
        internal SessionState SessionState
        {
            get
            {
                return EngineSessionState.PublicSessionState;
            }
        }

        /// <summary>
        /// Get/set constraints for this execution environment.
        /// </summary>
        internal PSLanguageMode LanguageMode
        {
            get
            {
                return _languageMode;
            }

            set
            {
                // If we're moving to ConstrainedLanguage, invalidate the binding
                // caches. After that, the binding rules encode the language mode.
                if (value == PSLanguageMode.ConstrainedLanguage)
                {
                    HasRunspaceEverUsedConstrainedLanguageMode = true;

                    // If 'ExecutionContext.HasEverUsedConstrainedLanguage' is already set to True, then we have
                    // already invalidated all cached binders, and binders already started to generate code with
                    // consideration of 'LanguageMode'. In such case, we don't need to invalidate cached binders
                    // again.
                    // Note that when executing script blocks marked as 'FullLanguage' in a 'ConstrainedLanguage'
                    // environment, we will set and Restore 'context.LanguageMode' very often. But we should not
                    // invalidate the cached binders every time we restore to 'ConstrainedLanguage'.
                    if (!ExecutionContext.HasEverUsedConstrainedLanguage)
                    {
                        lock (lockObject)
                        {
                            // If another thread has already set 'ExecutionContext.HasEverUsedConstrainedLanguage'
                            // while we are waiting on the lock, then nothing needs to be done.
                            if (!ExecutionContext.HasEverUsedConstrainedLanguage)
                            {
                                PSSetMemberBinder.InvalidateCache();
                                PSInvokeMemberBinder.InvalidateCache();
                                PSConvertBinder.InvalidateCache();
                                PSBinaryOperationBinder.InvalidateCache();
                                PSGetIndexBinder.InvalidateCache();
                                PSSetIndexBinder.InvalidateCache();
                                PSCreateInstanceBinder.InvalidateCache();

                                // Set 'HasEverUsedConstrainedLanguage' at the very end to guarantee other threads to wait until
                                // all invalidation operations are done.
                                UntrustedObjects = new ConditionalWeakTable<object, object>();
                                ExecutionContext.HasEverUsedConstrainedLanguage = true;
                            }
                        }
                    }
                }

                // Conversion caches don't have version info / binding rules, so must be
                // cleared every time.
                LanguagePrimitives.RebuildConversionCache();

                _languageMode = value;
            }
        }

        private PSLanguageMode _languageMode = PSLanguageMode.FullLanguage;

        /// <summary>
        /// True if this runspace has ever used constrained language mode.
        /// </summary>
        internal bool HasRunspaceEverUsedConstrainedLanguageMode { get; private set; }

        /// <summary>
        /// Indicate if a parameter binding is happening that transitions the execution from ConstrainedLanguage
        /// mode to a trusted FullLanguage command.
        /// </summary>
        internal bool LanguageModeTransitionInParameterBinding { get; set; }

        /// <summary>
        /// True if we've ever used ConstrainedLanguage. If this is the case, then the binding restrictions
        /// need to also validate against the language mode.
        /// </summary>
        internal static bool HasEverUsedConstrainedLanguage { get; private set; }

        #region Variable Tracking

        /// <summary>
        /// Initialized when 'ConstrainedLanguage' is applied.
        /// The objects contained in this table are considered to be untrusted.
        /// </summary>
        private static ConditionalWeakTable<object, object> UntrustedObjects { get; set; }

        /// <summary>
        /// Helper for checking if the given value is marked as untrusted.
        /// </summary>
        internal static bool IsMarkedAsUntrusted(object value)
        {
            bool result = false;
            var baseValue = PSObject.Base(value);
            if (baseValue != null && baseValue != NullString.Value)
            {
                result = UntrustedObjects.TryGetValue(baseValue, out _);
            }

            return result;
        }

        /// <summary>
        /// Helper for marking a value as untrusted.
        /// </summary>
        internal static void MarkObjectAsUntrusted(object value)
        {
            // If the value is a PSObject, then we mark its base object untrusted
            var baseValue = PSObject.Base(value);
            if (baseValue != null && baseValue != NullString.Value)
            {
                // It's actually setting a key value pair when the key doesn't exist
                UntrustedObjects.GetValue(baseValue, static key => null);

                try
                {
                    // If it's a PSReference object, we need to also mark the value it's holding on.
                    // This could result in a recursion if psRef.Value points to itself directly or indirectly, so we check if psRef.Value is already
                    // marked before making a recursive call. The additional check adds extra overhead for handling PSReference object, but it should
                    // be rare in practice.
                    var psRef = baseValue as PSReference;
                    if (psRef != null && !IsMarkedAsUntrusted(psRef.Value))
                    {
                        MarkObjectAsUntrusted(psRef.Value);
                    }
                }
                catch { /* psRef.Value may call PSVariable.Value under the hood, which may throw arbitrary exception */ }
            }
        }

        /// <summary>
        /// Helper for setting the untrusted value of an assignment to either a 'Global:' variable, or a 'Script:' variable in a module scope.
        /// </summary>
        /// <remarks>
        /// This method is for tracking assignment to global variables and module script scope varaibles in ConstrainedLanguage mode. Those variables
        /// can go across boundaries between ConstrainedLanguage and FullLanguage, and make it easy for a trusted script to use data from an untrusted
        /// environment. Therefore, in ConstrainedLanguage mode, we need to mark the value objects assigned to those variables as untrusted.
        /// </remarks>
        internal static void MarkObjectAsUntrustedForVariableAssignment(PSVariable variable, SessionStateScope scope, SessionStateInternal sessionState)
        {
            if (scope.Parent == null ||  // If it's the global scope, OR
                (sessionState.Module != null &&  // it's running in a module AND
                 scope.ScriptScope == scope && scope.Parent.Parent == null)) // it's the module's script scope (scope.Parent is global scope and scope.ScriptScope points to itself)
            {
                // We are setting value for either a 'Global:' variable, or a 'Script:' variable within a module in 'ConstrainedLanguage' mode.
                // Global variable may be referenced within trusted script block (scriptBlock.LanguageMode == 'FullLanguage'), and users could
                // also set a 'Script:' variable in a trusted module scope from 'ConstrainedLanguage' environment via '& $mo { $script:<var> }'.
                // So we need to mark the value as untrusted.
                MarkObjectAsUntrusted(variable.Value);
            }
        }

        /// <summary>
        /// The result object is assumed generated by operating on the original object.
        /// So if the original object is from an untrusted input source, we mark the result object as untrusted.
        /// </summary>
        internal static void PropagateInputSource(object originalObject, object resultObject, PSLanguageMode currentLanguageMode)
        {
            // The untrusted flag is populated only in FullLanguage mode and ConstrainedLanguage has been used in the process before.
            if (ExecutionContext.HasEverUsedConstrainedLanguage && currentLanguageMode == PSLanguageMode.FullLanguage && IsMarkedAsUntrusted(originalObject))
            {
                MarkObjectAsUntrusted(resultObject);
            }
        }

        #endregion

        /// <summary>
        /// If true the PowerShell debugger will use FullLanguage mode, otherwise it will use the current language mode.
        /// </summary>
        internal bool UseFullLanguageModeInDebugger
        {
            get
            {
                return InitialSessionState != null && InitialSessionState.UseFullLanguageModeInDebugger;
            }
        }

        internal static readonly List<string> ModulesWithJobSourceAdapters = new List<string>
            {
                Utils.ScheduledJobModuleName,
            };

        /// <summary>
        /// Is true if the PSScheduledJob module is loaded for this runspace.
        /// </summary>
        internal bool IsModuleWithJobSourceAdapterLoaded
        {
            get; set;
        }

        /// <summary>
        /// Gets the location globber for the session state for
        /// this instance of the runspace.
        /// </summary>
        internal LocationGlobber LocationGlobber => _locationGlobber ??= new LocationGlobber(SessionState);

        private LocationGlobber _locationGlobber = null;

        /// <summary>
        /// The assemblies that have been loaded for this runspace.
        /// </summary>
        internal Dictionary<string, Assembly> AssemblyCache { get; private set; }
        #endregion Properties

        #region Engine State

        /// <summary>
        /// The state for current engine that is running.
        /// </summary>
        /// <value></value>
        internal EngineState EngineState { get; set; } = EngineState.None;

        #endregion

        #region GetSetVariable methods

        /// <summary>
        /// Get a variable out of session state.
        /// </summary>
        internal object GetVariableValue(VariablePath path)
        {
            CmdletProviderContext context;
            SessionStateScope scope;
            return EngineSessionState.GetVariableValue(path, out context, out scope);
        }

        /// <summary>
        /// Get a variable out of session state. This calls GetVariable(name) and returns the
        /// value unless it is null in which case it returns the defaultValue provided by the caller.
        /// </summary>
        internal object GetVariableValue(VariablePath path, object defaultValue)
        {
            return EngineSessionState.GetVariableValue(path, out _, out _) ?? defaultValue;
        }

        /// <summary>
        /// Set a variable in session state.
        /// </summary>
        internal void SetVariable(VariablePath path, object newValue)
        {
            EngineSessionState.SetVariable(path, newValue, true, CommandOrigin.Internal);
        }

        internal T GetEnumPreference<T>(VariablePath preferenceVariablePath, T defaultPref, out bool defaultUsed)
        {
            object val = EngineSessionState.GetVariableValue(preferenceVariablePath, out _, out _);
            if (val is T)
            {
                if (val is ActionPreference actionPreferenceValue)
                {
                    CheckActionPreference(preferenceVariablePath, actionPreferenceValue, defaultPref);
                }

                T convertedResult = (T)val;

                defaultUsed = false;
                return convertedResult;
            }

            defaultUsed = true;
            T result = defaultPref;

            if (val != null)
            {
                try
                {
                    string valString = val as string;
                    if (valString != null)
                    {
                        result = (T)Enum.Parse(typeof(T), valString, true);
                        defaultUsed = false;
                    }
                    else
                    {
                        result = (T)PSObject.Base(val);
                        defaultUsed = false;
                    }

                    if (result is ActionPreference actionPreferenceValue)
                    {
                        CheckActionPreference(preferenceVariablePath, actionPreferenceValue, defaultPref);
                    }
                }
                catch (InvalidCastException)
                {
                    // default value is used
                }
                catch (ArgumentException)
                {
                    // default value is used
                }
            }

            return result;
        }

        private void CheckActionPreference(VariablePath preferenceVariablePath, ActionPreference preference, object defaultValue)
        {
            if (preference == ActionPreference.Suspend)
            {
                // ActionPreference.Suspend is reserved for future use. When it is used, reset
                // the variable to its default.
                string message = StringUtil.Format(ErrorPackage.ReservedActionPreferenceReplacedError, preference, preferenceVariablePath.UserPath, defaultValue);
                EngineSessionState.SetVariable(preferenceVariablePath, defaultValue, true, CommandOrigin.Internal);
                throw new NotSupportedException(message);
            }
        }

        /// <summary>
        /// Same as GetEnumPreference, but for boolean values.
        /// </summary>
        /// <param name="preferenceVariablePath"></param>
        /// <param name="defaultPref"></param>
        /// <param name="defaultUsed"></param>
        /// <returns></returns>
        internal bool GetBooleanPreference(VariablePath preferenceVariablePath, bool defaultPref, out bool defaultUsed)
        {
            object val = EngineSessionState.GetVariableValue(preferenceVariablePath, out _, out _);
            if (val is null)
            {
                defaultUsed = true;
                return defaultPref;
            }

            defaultUsed = !LanguagePrimitives.TryConvertTo(val, out bool converted);
            return defaultUsed ? defaultPref : converted;
        }
        #endregion GetSetVariable methods

        #region HelpSystem

        /// <summary>
        /// Help system for this engine context.
        /// </summary>
        /// <value></value>
        internal HelpSystem HelpSystem
        {
            get { return _helpSystem ??= new HelpSystem(this); }
        }

        private HelpSystem _helpSystem;

        #endregion

        #region FormatAndOutput
        internal object FormatInfo { get; set; }

        #endregion

        internal Dictionary<string, ScriptBlock> CustomArgumentCompleters { get; set; }

        internal Dictionary<string, ScriptBlock> NativeArgumentCompleters { get; set; }

        /// <summary>
        /// Routine to create a command(processor) instance using the factory.
        /// </summary>
        /// <param name="command">The name of the command to lookup.</param>
        /// <param name="dotSource"></param>
        /// <param name="forCompletion"></param>
        /// <returns>The command processor object.</returns>
        internal CommandProcessorBase CreateCommand(string command, bool dotSource, bool forCompletion = false)
        {
            CommandOrigin commandOrigin = this.EngineSessionState.CurrentScope.ScopeOrigin;
            CommandProcessorBase commandProcessor =
                CommandDiscovery.LookupCommandProcessor(command, commandOrigin, !dotSource, forCompletion);
            // Reset the command origin for script commands... // BUGBUG - dotting can get around command origin checks???
            if (commandProcessor != null && commandProcessor is ScriptCommandProcessorBase)
            {
                commandProcessor.Command.CommandOriginInternal = CommandOrigin.Internal;
            }

            return commandProcessor;
        }

        /// <summary>
        /// Hold the current command.
        /// </summary>
        /// <value>Reference to command discovery</value>
        internal CommandProcessorBase CurrentCommandProcessor { get; set; }

        /// <summary>
        /// Redirect to the CommandDiscovery in the engine.
        /// </summary>
        /// <value>Reference to command discovery</value>
        internal CommandDiscovery CommandDiscovery
        {
            get
            {
                return Engine.CommandDiscovery;
            }
        }

        /// <summary>
        /// Interface that should be used for interaction with host.
        /// </summary>
        internal InternalHost EngineHostInterface
        {
            // set not provided: it's not meaningful to change the host post-construction.
            get; private set;
        }

        /// <summary>
        /// Interface to be used for interaction with internal
        /// host. InternalHost wraps the host supplied
        /// during construction. Use this wrapper to access
        /// functionality specific to InternalHost.
        /// </summary>
        internal InternalHost InternalHost
        {
            get { return EngineHostInterface; }
        }

        /// <summary>
        /// Interface to the public API for the engine.
        /// </summary>
        internal EngineIntrinsics EngineIntrinsics
        {
            get { return _engineIntrinsics ??= new EngineIntrinsics(this); }
        }

        private EngineIntrinsics _engineIntrinsics;

        /// <summary>
        /// Log context cache.
        /// </summary>
        internal LogContextCache LogContextCache { get; } = new LogContextCache();

        #region Output pipes
        /// <summary>
        /// The PipelineWriter provided by the connection object for success output.
        /// </summary>
        internal PipelineWriter ExternalSuccessOutput { get; set; }

        /// <summary>
        /// The PipelineWriter provided by the connection object for error output.
        /// </summary>
        internal PipelineWriter ExternalErrorOutput { get; set; }

        /// <summary>
        /// The PipelineWriter provided by the connection object for progress output.
        /// </summary>
        internal PipelineWriter ExternalProgressOutput { get; set; }

        internal class SavedContextData
        {
            private readonly bool _stepScript;
            private readonly bool _ignoreScriptDebug;
            private readonly int _PSDebug;

            private readonly Pipe _shellFunctionErrorOutputPipe;

            public SavedContextData(ExecutionContext context)
            {
                _stepScript = context.PSDebugTraceStep;
                _ignoreScriptDebug = context.IgnoreScriptDebug;
                _PSDebug = context.PSDebugTraceLevel;

                _shellFunctionErrorOutputPipe = context.ShellFunctionErrorOutputPipe;
            }

            public void RestoreContextData(ExecutionContext context)
            {
                context.PSDebugTraceStep = _stepScript;
                context.IgnoreScriptDebug = _ignoreScriptDebug;
                context.PSDebugTraceLevel = _PSDebug;

                context.ShellFunctionErrorOutputPipe = _shellFunctionErrorOutputPipe;
            }
        }

        /// <summary>
        /// Host uses this to saves context data when entering a nested prompt.
        /// </summary>
        /// <returns></returns>
        internal SavedContextData SaveContextData()
        {
            return new SavedContextData(this);
        }

        internal void ResetShellFunctionErrorOutputPipe()
        {
            ShellFunctionErrorOutputPipe = null;
        }

        internal Pipe RedirectErrorPipe(Pipe newPipe)
        {
            Pipe oldPipe = ShellFunctionErrorOutputPipe;
            ShellFunctionErrorOutputPipe = newPipe;
            return oldPipe;
        }

        /// <summary>
        /// Reset all of the redirection book keeping variables. This routine should be called when starting to
        /// execute a script.
        /// </summary>
        internal void ResetRedirection()
        {
            ShellFunctionErrorOutputPipe = null;
        }

        /// <summary>
        /// Function and Script command processors will route their error output to
        /// this pipe if set, unless explicitly routed elsewhere. We also keep track
        /// of the first time this value is set so we can know if it's the default
        /// error output or not.
        /// </summary>
        internal Pipe ShellFunctionErrorOutputPipe { get; set; }

        /// <summary>
        /// Supports expression Warning output redirection.
        /// </summary>
        internal Pipe ExpressionWarningOutputPipe { get; set; }

        /// <summary>
        /// Supports expression Verbose output redirection.
        /// </summary>
        internal Pipe ExpressionVerboseOutputPipe { get; set; }

        /// <summary>
        /// Supports expression Verbose output redirection.
        /// </summary>
        internal Pipe ExpressionDebugOutputPipe { get; set; }

        /// <summary>
        /// Supports expression Information output redirection.
        /// </summary>
        internal Pipe ExpressionInformationOutputPipe { get; set; }

        #endregion Output pipes

        #region Append to $error
        /// <summary>
        /// Appends the object to $global:error if it's an error record or exception.
        /// </summary>
        /// <param name="obj">
        /// ErrorRecord or Exception to be written to $global:error
        /// </param>
        /// <exception cref="ExtendedTypeSystemException">
        /// (get-only) An error occurred accessing $ERROR.
        /// </exception>
        internal void AppendDollarError(object obj)
        {
            ErrorRecord objAsErrorRecord = obj as ErrorRecord;
            if (objAsErrorRecord is null && obj is not Exception)
            {
                Diagnostics.Assert(false, "Object to append was neither an ErrorRecord nor an Exception in ExecutionContext.AppendDollarError");
                return;
            }

            if (DollarErrorVariable is not ArrayList arraylist)
            {
                Diagnostics.Assert(false, "$error should be a global constant ArrayList");
                return;
            }

            // Don't add the same exception twice...
            if (arraylist.Count > 0)
            {
                // There may be exceptions stored directly in which case
                // the direct comparison will catch them...
                if (arraylist[0] == obj)
                    return;
                // otherwise check the exception members of the error records...
                ErrorRecord er1 = arraylist[0] as ErrorRecord;

                if (er1 != null && objAsErrorRecord != null && er1.Exception == objAsErrorRecord.Exception)
                    return;
            }

            const int maxErrorCount = 256;

            int numToErase = arraylist.Count - (maxErrorCount - 1);
            if (numToErase > 0)
            {
                arraylist.RemoveRange(
                    maxErrorCount - 1,
                    numToErase);
            }

            arraylist.Insert(0, obj);
        }
        #endregion

        #region Scope or Commands (in pipeline) Depth Count

        /// <summary>
        /// Check if the stack would overflow soon, if so, throw ScriptCallDepthException.
        /// </summary>
        /// <exception cref="ScriptCallDepthException">
        /// If the stack would overflow soon.
        /// </exception>
        internal static void CheckStackDepth()
        {
            try
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();
            }
            catch (InsufficientExecutionStackException)
            {
                throw new ScriptCallDepthException();
            }
        }

        #endregion

        /// <summary>
        /// The current connection object.
        /// </summary>
        private Runspace _currentRunspace;
        // This should be internal, but it need to be friend of remoting dll.
        /// <summary>
        /// The current connection object.
        /// </summary>
        internal Runspace CurrentRunspace
        {
            get { return _currentRunspace; }

            set { _currentRunspace = value; }
        }

        /// <summary>
        /// Each pipeline has a stack of pipeline processor. This method
        /// pushes pp in to stack for currently executing pipeline.
        /// </summary>
        /// <param name="pp"></param>
        internal void PushPipelineProcessor(PipelineProcessor pp)
        {
            if (_currentRunspace == null)
                return;
            LocalPipeline lpl = (LocalPipeline)((RunspaceBase)_currentRunspace).GetCurrentlyRunningPipeline();
            if (lpl == null)
                return;
            lpl.Stopper.Push(pp);
        }

        /// <summary>
        /// Each pipeline has a stack of pipeline processor. This method pops the
        /// top item from the stack.
        /// </summary>
        internal void PopPipelineProcessor(bool fromSteppablePipeline)
        {
            if (_currentRunspace == null)
                return;
            LocalPipeline lpl = (LocalPipeline)((RunspaceBase)_currentRunspace).GetCurrentlyRunningPipeline();
            if (lpl == null)
                return;
            lpl.Stopper.Pop(fromSteppablePipeline);
        }

        /// <summary>
        /// This flag is checked by parser to stop loops etc.
        /// </summary>
        /// <returns></returns>
        internal bool CurrentPipelineStopping
        {
            get
            {
                if (_currentRunspace == null)
                    return false;
                LocalPipeline lpl = (LocalPipeline)((RunspaceBase)_currentRunspace).GetCurrentlyRunningPipeline();
                if (lpl == null)
                    return false;
                return lpl.IsStopping;
            }
        }

        /// <summary>
        /// True means one of these:
        /// 1) there is a trap statement in a dynamically enclosing statement block that might catch an exception.
        /// 2) execution happens inside a PS class and exceptions should be propagated all the way up, even if there is no enclosing try-catch-finally.
        /// </summary>
        /// <value></value>
        internal bool PropagateExceptionsToEnclosingStatementBlock { get; set; }

        internal RuntimeException CurrentExceptionBeingHandled { get; set; }

        /// <summary>
        /// Shortcut to get at $?
        /// </summary>
        /// <value>The current value of $? </value>
        internal bool QuestionMarkVariableValue { get; set; } = true;

        /// <summary>
        /// Shortcut to get at $error.
        /// </summary>
        /// <value>The current value of $global:error </value>
        internal object DollarErrorVariable
        {
            get
            {
                CmdletProviderContext context = null;
                SessionStateScope scope = null;
                object resultItem = null;

                if (!Events.IsExecutingEventAction)
                {
                    resultItem = EngineSessionState.GetVariableValue(
                        SpecialVariables.ErrorVarPath, out context, out scope);
                }
                else
                {
                    resultItem = EngineSessionState.GetVariableValue(
                        SpecialVariables.EventErrorVarPath, out context, out scope);
                }

                return resultItem;
            }

            set
            {
                EngineSessionState.SetVariable(
                    SpecialVariables.ErrorVarPath, value, true, CommandOrigin.Internal);
            }
        }

        internal ActionPreference DebugPreferenceVariable
        {
            get
            {
                bool defaultUsed = false;
                return this.GetEnumPreference(
                    SpecialVariables.DebugPreferenceVarPath,
                    InitialSessionState.DefaultDebugPreference,
                    out defaultUsed);
            }

            set
            {
                this.EngineSessionState.SetVariable(
                    SpecialVariables.DebugPreferenceVarPath,
                    LanguagePrimitives.ConvertTo(value, typeof(ActionPreference), CultureInfo.InvariantCulture),
                    true,
                    CommandOrigin.Internal);
            }
        }

        internal ActionPreference VerbosePreferenceVariable
        {
            get
            {
                bool defaultUsed = false;
                return this.GetEnumPreference(
                    SpecialVariables.VerbosePreferenceVarPath,
                    InitialSessionState.DefaultVerbosePreference,
                    out defaultUsed);
            }

            set
            {
                this.EngineSessionState.SetVariable(
                    SpecialVariables.VerbosePreferenceVarPath,
                    LanguagePrimitives.ConvertTo(value, typeof(ActionPreference), CultureInfo.InvariantCulture),
                    true,
                    CommandOrigin.Internal);
            }
        }

        internal ActionPreference ErrorActionPreferenceVariable
        {
            get
            {
                bool defaultUsed = false;
                return this.GetEnumPreference(
                    SpecialVariables.ErrorActionPreferenceVarPath,
                    InitialSessionState.DefaultErrorActionPreference,
                    out defaultUsed);
            }

            set
            {
                this.EngineSessionState.SetVariable(
                    SpecialVariables.ErrorActionPreferenceVarPath,
                    LanguagePrimitives.ConvertTo(value, typeof(ActionPreference), CultureInfo.InvariantCulture),
                    true,
                    CommandOrigin.Internal);
            }
        }

        internal ActionPreference WarningActionPreferenceVariable
        {
            get
            {
                bool defaultUsed = false;
                return this.GetEnumPreference(
                    SpecialVariables.WarningPreferenceVarPath,
                    InitialSessionState.DefaultWarningPreference,
                    out defaultUsed);
            }

            set
            {
                this.EngineSessionState.SetVariable(
                    SpecialVariables.WarningPreferenceVarPath,
                    LanguagePrimitives.ConvertTo(value, typeof(ActionPreference), CultureInfo.InvariantCulture),
                    true,
                    CommandOrigin.Internal);
            }
        }

        internal ActionPreference InformationActionPreferenceVariable
        {
            get
            {
                bool defaultUsed = false;
                return this.GetEnumPreference(
                    SpecialVariables.InformationPreferenceVarPath,
                    InitialSessionState.DefaultInformationPreference,
                    out defaultUsed);
            }

            set
            {
                this.EngineSessionState.SetVariable(
                    SpecialVariables.InformationPreferenceVarPath,
                    LanguagePrimitives.ConvertTo(value, typeof(ActionPreference), CultureInfo.InvariantCulture),
                    true,
                    CommandOrigin.Internal);
            }
        }

        internal object WhatIfPreferenceVariable
        {
            get
            {
                CmdletProviderContext context = null;
                SessionStateScope scope = null;

                object resultItem = this.EngineSessionState.GetVariableValue(
                    SpecialVariables.WhatIfPreferenceVarPath,
                    out context,
                    out scope);

                return resultItem;
            }

            set
            {
                this.EngineSessionState.SetVariable(
                    SpecialVariables.WhatIfPreferenceVarPath,
                    value,
                    true,
                    CommandOrigin.Internal);
            }
        }

        internal ConfirmImpact ConfirmPreferenceVariable
        {
            get
            {
                bool defaultUsed = false;
                return this.GetEnumPreference(
                    SpecialVariables.ConfirmPreferenceVarPath,
                    InitialSessionState.DefaultConfirmPreference,
                    out defaultUsed);
            }

            set
            {
                this.EngineSessionState.SetVariable(
                    SpecialVariables.ConfirmPreferenceVarPath,
                    LanguagePrimitives.ConvertTo(value, typeof(ConfirmImpact), CultureInfo.InvariantCulture),
                    true,
                    CommandOrigin.Internal);
            }
        }

        internal void RunspaceClosingNotification()
        {
            EngineSessionState.RunspaceClosingNotification();

            _debugger?.Dispose();

            Events?.Dispose();
            Events = null;

            this.transactionManager?.Dispose();
            this.transactionManager = null;
        }

        /// <summary>
        /// Gets the type table instance for this engine.
        /// </summary>
        internal TypeTable TypeTable
        {
            get
            {
                if (_typeTable == null)
                {
                    _typeTable = new TypeTable();
                    _typeTableWeakReference = new WeakReference<TypeTable>(_typeTable);
                }

                return _typeTable;
            }

            set
            {
                _typeTable = value;
                _typeTableWeakReference = (value != null) ? new WeakReference<TypeTable>(value) : null;
            }
        }

        /// <summary>
        /// Here for PSObject, should probably not be used elsewhere, maybe not even in PSObject.
        /// </summary>
        internal WeakReference<TypeTable> TypeTableWeakReference
        {
            get
            {
                if (_typeTable == null)
                {
                    var unused = TypeTable;
                }

                return _typeTableWeakReference;
            }
        }

        private TypeTable _typeTable;
        private WeakReference<TypeTable> _typeTableWeakReference;

        /// <summary>
        /// Gets the format info database for this engine.
        /// </summary>
        internal TypeInfoDataBaseManager FormatDBManager
        {
            get
            {
                if (_formatDBManager == null)
                {
                    // If no Formatter database has been created, then
                    // create and initialize an empty one.
                    _formatDBManager = new TypeInfoDataBaseManager();
                    _formatDBManager.Update(this.AuthorizationManager, this.EngineHostInterface);
                    if (this.InitialSessionState != null)
                    {
                        // Win8:418011: Set DisableFormatTableUpdates only after performing the initial update. Otherwise, formatDBManager will be
                        // in bad state.
                        _formatDBManager.DisableFormatTableUpdates = this.InitialSessionState.DisableFormatUpdates;
                    }
                }

                return _formatDBManager;
            }

            set
            {
                _formatDBManager = value;
            }
        }

        private TypeInfoDataBaseManager _formatDBManager;

        /// <summary>
        /// Gets the TransactionManager instance that controls transactions in the current
        /// instance.
        /// </summary>
        internal PSTransactionManager TransactionManager
        {
            get
            {
                return transactionManager;
            }
        }

        internal PSTransactionManager transactionManager;

        /// <summary>
        /// This method is used for assembly loading requests stemmed from 'InitialSessionState' binding and module loading.
        /// </summary>
        /// <param name="source">Source of the assembly loading request, should be a module name when specified.</param>
        /// <param name="assemblyName">Name of the assembly to be loaded.</param>
        /// <param name="filePath">Path of the assembly to be loaded.</param>
        /// <param name="error">Exception that is caught when the loading fails.</param>
        internal Assembly AddAssembly(string source, string assemblyName, string filePath, out Exception error)
        {
            // Search the cache by the path, and return the assembly if we find it.
            // It's common to have two loading requests for the same assembly when loading a module -- the first time for
            // resolving a binary module path, and the second time for actually processing that module.
            //
            // That's not a problem when all the module assemblies are loaded into the default ALC. But in a scenario where
            // a module tries to hide its nested/root binary modules in a custom ALC, that will become a problem. This is
            // because:
            // in that scenario, the module will usually setup a handler to load the specific assemblies to the custom ALC,
            // and that will be how the first loading request gets served. However, after the module path is resolved with
            // the first loading, the path will be used for the second loading upon real module processing. Since we prefer
            // loading-by-path over loading-by-name in the 'LoadAssembly' call, we will end up loading the same assembly in
            // the default ALC (because we use 'Assembly.LoadFrom' which always loads an assembly to the default ALC) if we
            // do not search in the cache first. That will break the scenario, because the module means to isolate all its
            // dependencies from the default ALC, and it failed to do so.
            //
            // Therefore, we need to search the cache first. The reason we use path as the key is to make sure the request
            // is for exactly the same assembly. The same assembly file should not be loaded into different ALC's by module
            // loading within the same PowerShell session (Runspace).
            //
            // An example module targeting the abovementioned scenario will likely have the following file structure:
            // IsolatedModule
            //  │   IsolatedModule.psd1 (has 'NestedModules = @('Test.Isolated.Init.dll', 'Test.Isolated.Nested.dll')')
            //  │   Test.Isolated.Init.dll (contains the custom ALC and code to setup 'Resolving' handler)
            //  └───Dependencies (folder under module base)
            //         Newtonsoft.Json.dll (version 10.0.0.0 dependency)
            //         Test.Isolated.Nested.dll (nested binary module referencing the particular dependency)
            //
            // In this example, the following events will happen in sequence:
            //  1. PowerShell is able to find 'Test.Isolated.Init.dll' under module base folder, so it will be loaded into
            //     the default ALC as expected and setup the 'Resolving' handler via the 'OnImport' call.
            //  2. PowerShell cannot find 'Test.Isolated.Nested.dll' under the module base folder, so it will call the method
            //     'FixFileName(.., bool canLoadAssembly)' to resolve the path of this binary module.
            //     This particular overload will attempt to load the assembly by name, which will be served by the 'Resolving'
            //     handler that was setup in the step 1. So, the assembly will be loaded into the custom ALC and insert to the
            //     assembly cache.
            //  3. Path of the nested module 'Test.Isolated.Init.dll' now has been resolved by the step 2 (assembly.Location).
            //     Now it's time to actually load this binary module for processing in the method 'LoadBinaryModule', which
            //     will make a call to this method with the resolved assembly file path.
            // At this poin, we will have to query the cache first, instead of calling 'LoadAssembly' directly, to make sure
            // that the assembly instance loaded in the custom ALC in step 2 gets returned back. Otherwise, the same assembly
            // file will be loaded in the default ALC because 'Assembly.LoadFrom' is used in 'LoadAssembly' and that API will
            // always load an assembly file to the default ALC, and that will break this scenario.
            if (TryGetFromAssemblyCache(source, filePath, out Assembly loadedAssembly))
            {
                error = null;
                return loadedAssembly;
            }

            // Attempt to load the requested assembly, first by path then by name.
            loadedAssembly = LoadAssembly(assemblyName, filePath, out error);
            if (loadedAssembly is not null)
            {
                AddToAssemblyCache(source, loadedAssembly);
            }

            return loadedAssembly;
        }

        /// <summary>
        /// Add a loaded assembly to the 'AssemblyCache'.
        /// The <paramref name="source"/> is used as a prefix for the key to make it easy to remove all associated
        /// assemblies from the cache when a module gets unloaded.
        /// </summary>
        /// <param name="source">The source where the assembly comes from, should be a module name when specified.</param>
        /// <param name="assembly">The assembly we try to cache.</param>
        internal void AddToAssemblyCache(string source, Assembly assembly)
        {
            // Try caching the assembly by its location if possible.
            // When it's a dynamic assembly, we use it's full name. This could happen with 'Import-Module -Assembly'.
            string key = string.IsNullOrEmpty(assembly.Location) ? assembly.FullName : assembly.Location;

            // When the assembly is from a module loading, we prefix the key with the source,
            // so we can remove it from the cache when the module gets unloaded.
            if (!string.IsNullOrEmpty(source))
            {
                // Both 'source' and 'key' are of the string type, so no need to specify 'InvariantCulture'.
                key = $"{source}@{key}";
            }

            AssemblyCache.TryAdd(key, assembly);
        }

        /// <summary>
        /// Remove all cache entries that are associated with the specified source.
        /// </summary>
        internal void RemoveFromAssemblyCache(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return;
            }

            var keysToRemove = new List<string>();
            string prefix = $"{source}@";

            foreach (string key in AssemblyCache.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (string key in keysToRemove)
            {
                AssemblyCache.Remove(key);
            }
        }

        /// <summary>
        /// Try to get an assembly from the cache.
        /// </summary>
        private bool TryGetFromAssemblyCache(string source, string filePath, out Assembly assembly)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                assembly = null;
                return false;
            }

            // Both 'source' and 'filePath' are of the string type, so no need to specify 'InvariantCulture'.
            string key = string.IsNullOrEmpty(source) ? filePath : $"{source}@{filePath}";
            return AssemblyCache.TryGetValue(key, out assembly);
        }

        private static Assembly LoadAssembly(string name, string filePath, out Exception error)
        {
            // First we try to load the assembly based on the filename
            Assembly loadedAssembly = null;
            error = null;
            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    // codeql[cs/dll-injection-remote] - The dll is loaded during the initial state setup, which is expected behavior. This allows users hosting PowerShell to load additional C# types to enable their specific scenarios.
                    loadedAssembly = Assembly.LoadFrom(filePath);
                    return loadedAssembly;
                }
                catch (FileNotFoundException fileNotFound)
                {
                    error = fileNotFound;
                }
                catch (FileLoadException fileLoadException)
                {
                    error = fileLoadException;
                    return null;
                }
                catch (BadImageFormatException badImage)
                {
                    error = badImage;
                    return null;
                }
                catch (SecurityException securityException)
                {
                    error = securityException;
                    return null;
                }
            }

            // Then we try to load the assembly based on the given name
            if (!string.IsNullOrEmpty(name))
            {
                string fixedName = null;
                // Remove the '.dll' if it's there...
                fixedName = name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                                ? Path.GetFileNameWithoutExtension(name)
                                : name;

                var assemblyString = Utils.IsPowerShellAssembly(fixedName)
                                         ? Utils.GetPowerShellAssemblyStrongName(fixedName)
                                         : fixedName;

                try
                {
                    loadedAssembly = Assembly.Load(new AssemblyName(assemblyString));
                }
                catch (FileNotFoundException fileNotFound)
                {
                    error = fileNotFound;
                }
                catch (FileLoadException fileLoadException)
                {
                    error = fileLoadException;
                }
                catch (BadImageFormatException badImage)
                {
                    error = badImage;
                }
                catch (SecurityException securityException)
                {
                    error = securityException;
                }
            }

            // If the assembly is loaded, we ignore error as it may come from the filepath loading.
            if (loadedAssembly != null)
            {
                error = null;
            }

            return loadedAssembly;
        }

        /// <summary>
        /// Report an initialization-time error.
        /// </summary>
        /// <param name="resourceString">Resource string.</param>
        /// <param name="arguments">Arguments.</param>
        internal void ReportEngineStartupError(string resourceString, params object[] arguments)
        {
            try
            {
                Cmdlet currentRunningModuleCommand;
                string errorId;
                if (IsModuleCommandCurrentlyRunning(out currentRunningModuleCommand, out errorId))
                {
                    RuntimeException rte = InterpreterError.NewInterpreterException(null, typeof(RuntimeException), null, errorId, resourceString, arguments);
                    currentRunningModuleCommand.WriteError(new ErrorRecord(rte.ErrorRecord, rte));
                }
                else
                {
                    PSHost host = EngineHostInterface;
                    if (host == null)
                    {
                        return;
                    }

                    PSHostUserInterface ui = host.UI;
                    if (ui == null)
                    {
                        return;
                    }

                    ui.WriteErrorLine(
                        StringUtil.Format(resourceString, arguments));
                }
            }
            catch (Exception) // swallow all exceptions
            {
            }
        }

        /// <summary>
        /// Report an initialization-time error.
        /// </summary>
        /// <param name="error">Error to report.</param>
        internal void ReportEngineStartupError(string error)
        {
            try
            {
                Cmdlet currentRunningModuleCommand;
                string errorId;
                if (IsModuleCommandCurrentlyRunning(out currentRunningModuleCommand, out errorId))
                {
                    RuntimeException rte = InterpreterError.NewInterpreterException(null, typeof(RuntimeException), null, errorId, "{0}", error);
                    currentRunningModuleCommand.WriteError(new ErrorRecord(rte.ErrorRecord, rte));
                }
                else
                {
                    PSHost host = EngineHostInterface;
                    if (host == null)
                    {
                        return;
                    }

                    PSHostUserInterface ui = host.UI;
                    if (ui == null)
                    {
                        return;
                    }

                    ui.WriteErrorLine(error);
                }
            }
            catch (Exception) // swallow all exceptions
            {
            }
        }

        /// <summary>
        /// Report an initialization-time error.
        /// </summary>
        /// <param name="e"></param>
        internal void ReportEngineStartupError(Exception e)
        {
            try
            {
                Cmdlet currentRunningModuleCommand;
                string errorId;
                if (IsModuleCommandCurrentlyRunning(out currentRunningModuleCommand, out errorId))
                {
                    ErrorRecord error = null;
                    var rte = e as RuntimeException;

                    error = rte != null
                        ? new ErrorRecord(rte.ErrorRecord, rte)
                        : new ErrorRecord(e, errorId, ErrorCategory.OperationStopped, null);

                    currentRunningModuleCommand.WriteError(error);
                }
                else
                {
                    PSHost host = EngineHostInterface;
                    if (host == null)
                    {
                        return;
                    }

                    PSHostUserInterface ui = host.UI;
                    if (ui == null)
                    {
                        return;
                    }

                    ui.WriteErrorLine(e.Message);
                }
            }
            catch (Exception) // swallow all exceptions
            {
            }
        }

        /// <summary>
        /// Report an initialization-time error.
        /// </summary>
        /// <param name="errorRecord"></param>
        internal void ReportEngineStartupError(ErrorRecord errorRecord)
        {
            try
            {
                Cmdlet currentRunningModuleCommand;
                if (IsModuleCommandCurrentlyRunning(out currentRunningModuleCommand, out _))
                {
                    currentRunningModuleCommand.WriteError(errorRecord);
                }
                else
                {
                    PSHost host = EngineHostInterface;
                    if (host == null)
                    {
                        return;
                    }

                    PSHostUserInterface ui = host.UI;
                    if (ui == null)
                    {
                        return;
                    }

                    ui.WriteErrorLine(errorRecord.ToString());
                }
            }
            catch (Exception) // swallow all exceptions
            {
            }
        }

        private bool IsModuleCommandCurrentlyRunning(out Cmdlet command, out string errorId)
        {
            command = null;
            errorId = null;
            bool result = false;
            if (this.CurrentCommandProcessor != null)
            {
                CommandInfo cmdletInfo = this.CurrentCommandProcessor.CommandInfo;
                if ((string.Equals(cmdletInfo.Name, "Import-Module", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(cmdletInfo.Name, "Remove-Module", StringComparison.OrdinalIgnoreCase)) &&
                    cmdletInfo.CommandType.Equals(CommandTypes.Cmdlet) &&
                    InitialSessionState.CoreModule.Equals(cmdletInfo.ModuleName, StringComparison.OrdinalIgnoreCase))
                {
                    result = true;
                    command = (Cmdlet)this.CurrentCommandProcessor.Command;
                    errorId = string.Equals(cmdletInfo.Name, "Import-Module", StringComparison.OrdinalIgnoreCase)
                                  ? "Module_ImportModuleError"
                                  : "Module_RemoveModuleError";
                }
            }

            return result;
        }

        /// <summary>
        /// Constructs an Execution context object for Automation Engine.
        /// </summary>
        /// <param name="engine">
        /// Engine that hosts this execution context
        /// </param>
        /// <param name="hostInterface">
        /// Interface that should be used for interaction with host
        /// </param>
        /// <param name="initialSessionState">
        /// InitialSessionState information
        /// </param>
        internal ExecutionContext(AutomationEngine engine, PSHost hostInterface, InitialSessionState initialSessionState)
        {
            InitialSessionState = initialSessionState;
            AuthorizationManager = initialSessionState.AuthorizationManager;

            InitializeCommon(engine, hostInterface);
        }

        private void InitializeCommon(AutomationEngine engine, PSHost hostInterface)
        {
            Engine = engine;
            Events = new PSLocalEventManager(this);
            transactionManager = new PSTransactionManager();
            _debugger = new ScriptDebugger(this);

            EngineHostInterface = hostInterface as InternalHost ?? new InternalHost(hostInterface, this);

            // Hook up the assembly cache
            AssemblyCache = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

            // Initialize the fixed toplevel session state and the current session state
            TopLevelSessionState = EngineSessionState = new SessionStateInternal(this);

            // if authorizationmanager==null, this means the configuration
            // explicitly asked for dummy authorization manager.
            AuthorizationManager ??= new AuthorizationManager(null);

            // Set up the module intrinsics
            Modules = new ModuleIntrinsics(this);
        }

        private static readonly object lockObject = new object();
    }

    /// <summary>
    /// Enum that defines state of monad engine.
    /// </summary>
    internal enum EngineState
    {
        /// <summary>
        /// Engine state is not defined or initialized.
        /// </summary>
        None = 0,

        /// <summary>
        /// Engine available.
        /// </summary>
        Available = 1,

        /// <summary>
        /// Engine service is degraded.
        /// </summary>
        Degraded = 2,

        /// <summary>
        /// Engine is out of service.
        /// </summary>
        OutOfService = 3,

        /// <summary>
        /// Engine is stopped.
        /// </summary>
        Stopped = 4
    }
}
