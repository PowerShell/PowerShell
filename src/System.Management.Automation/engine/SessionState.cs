// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Security;

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    /// <summary>
    /// Holds the state of a Monad Shell session.
    /// </summary>
    [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "This is a bridge class between internal classes and a public interface. It requires this much coupling.")]
    internal sealed partial class SessionStateInternal
    {
        #region tracer

        /// <summary>
        /// An instance of the PSTraceSource class used for trace output
        /// using "SessionState" as the category.
        /// </summary>
        [Dbg.TraceSource(
             "SessionState",
             "SessionState Class")]
        private static readonly Dbg.PSTraceSource s_tracer =
            Dbg.PSTraceSource.GetTracer("SessionState",
             "SessionState Class");

        #endregion tracer

        #region Constructor

        /// <summary>
        /// Constructor for session state object.
        /// </summary>
        /// <param name="context">
        /// The context for the runspace to which this session state object belongs.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// if <paramref name="context"/> is null.
        /// </exception>
        internal SessionStateInternal(ExecutionContext context) : this(null, false, context)
        {
        }

        internal SessionStateInternal(SessionStateInternal parent, bool linkToGlobal, ExecutionContext context)
        {
            if (context == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(context));
            }

            ExecutionContext = context;

            // Create the working directory stack. This
            // is used for the pushd and popd commands

            _workingLocationStack = new Dictionary<string, Stack<PathInfo>>(StringComparer.OrdinalIgnoreCase);

            // Conservative choice to limit the Set-Location history in order to limit memory impact in case of a regression.
            const int locationHistoryLimit = 20;
            _setLocationHistory = new HistoryStack<PathInfo>(locationHistoryLimit);

            GlobalScope = new SessionStateScope(null);
            ModuleScope = GlobalScope;
            _currentScope = GlobalScope;

            InitializeSessionStateInternalSpecialVariables(false);

            // Create the push the global scope on as
            // the starting script scope.  That way, if you dot-source a script
            // that uses variables qualified by script: it works.
            GlobalScope.ScriptScope = GlobalScope;

            if (parent != null)
            {
                GlobalScope.Parent = parent.GlobalScope;

                // Copy the drives and providers from the parent...
                CopyProviders(parent);
                // During loading of core modules, providers are not populated.
                // We set the drive information later
                if (Providers != null && Providers.Count > 0)
                {
                    CurrentDrive = parent.CurrentDrive;
                }

                // Link it to the global scope...
                if (linkToGlobal)
                {
                    GlobalScope = parent.GlobalScope;
                }
            }
            else
            {
                _currentScope.LocalsTuple = MutableTuple.MakeTuple(Compiler.DottedLocalsTupleType, Compiler.DottedLocalsNameIndexMap);
            }
        }

        /// <summary>
        /// Add any special variables to the session state variable table. This routine
        /// must be called at construction time or if the variable table is reset.
        /// </summary>
        internal void InitializeSessionStateInternalSpecialVariables(bool clearVariablesTable)
        {
            if (clearVariablesTable)
            {
                // Clear the variable table
                GlobalScope.Variables.Clear();

                // Add in the per-scope default variables.
                GlobalScope.AddSessionStateScopeDefaultVariables();
            }

            // Set variable $Error
            PSVariable errorvariable = new PSVariable("Error", new ArrayList(), ScopedItemOptions.Constant);
            GlobalScope.SetVariable(errorvariable.Name, errorvariable, false, false, this, fastPath: true);

            // Set variable $PSDefaultParameterValues
            Collection<Attribute> attributes = new Collection<Attribute>();
            attributes.Add(new ArgumentTypeConverterAttribute(typeof(System.Management.Automation.DefaultParameterDictionary)));
            PSVariable psDefaultParameterValuesVariable = new PSVariable(SpecialVariables.PSDefaultParameterValues,
                                                                         new DefaultParameterDictionary(),
                                                                         ScopedItemOptions.None, attributes,
                                                                         RunspaceInit.PSDefaultParameterValuesDescription);
            GlobalScope.SetVariable(psDefaultParameterValuesVariable.Name, psDefaultParameterValuesVariable, false, false, this, fastPath: true);
        }

        #endregion Constructor

        #region Private data

        /// <summary>
        /// Provides all the path manipulation and globbing for Monad paths.
        /// </summary>
        internal LocationGlobber Globber
        {
            get { return _globberPrivate ??= ExecutionContext.LocationGlobber; }
        }

        private LocationGlobber _globberPrivate;

        /// <summary>
        /// The context of the runspace to which this session state object belongs.
        /// </summary>
        internal ExecutionContext ExecutionContext { get; }

        /// <summary>
        /// Returns the public session state facade object for this session state instance.
        /// </summary>
        internal SessionState PublicSessionState
        {
            get { return _publicSessionState ??= new SessionState(this); }

            set { _publicSessionState = value; }
        }

        private SessionState _publicSessionState;

        /// <summary>
        /// Gets the engine APIs to access providers.
        /// </summary>
        internal ProviderIntrinsics InvokeProvider
        {
            get { return _invokeProvider ??= new ProviderIntrinsics(this); }
        }

        private ProviderIntrinsics _invokeProvider;

        /// <summary>
        /// The module info object associated with this session state.
        /// </summary>
        internal PSModuleInfo Module { get; set; } = null;

        // This is used to maintain the order in which modules were imported.
        // This is used by Get-Command -All to order by last imported
        internal List<string> ModuleTableKeys = new List<string>();

        /// <summary>
        /// The private module table for this session state object...
        /// </summary>
        internal Dictionary<string, PSModuleInfo> ModuleTable { get; } = new Dictionary<string, PSModuleInfo>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Get/set constraints for this execution environment.
        /// </summary>
        internal PSLanguageMode LanguageMode
        {
            get
            {
                return ExecutionContext.LanguageMode;
            }

            set
            {
                ExecutionContext.LanguageMode = value;
            }
        }

        /// <summary>
        /// If true the PowerShell debugger will use FullLanguage mode, otherwise it will use the current language mode.
        /// </summary>
        internal bool UseFullLanguageModeInDebugger
        {
            get
            {
                return ExecutionContext.UseFullLanguageModeInDebugger;
            }
        }

        /// <summary>
        /// The list of scripts that are allowed to be run. If the name "*"
        /// is in the list, then all scripts can be run. (This is the default.)
        /// </summary>
        public List<string> Scripts { get; } = new List<string>(new string[] { "*" });

        /// <summary>
        /// See if a script is allowed to be run.
        /// </summary>
        /// <param name="scriptPath">Path to check.</param>
        /// <returns>True if script is allowed.</returns>
        internal SessionStateEntryVisibility CheckScriptVisibility(string scriptPath)
        {
            return checkPathVisibility(Scripts, scriptPath);
        }

        /// <summary>
        /// The list of applications that are allowed to be run. If the name "*"
        /// is in the list, then all applications can be run. (This is the default.)
        /// </summary>
        public List<string> Applications { get; } = new List<string>(new string[] { "*" });

        /// <summary>
        /// List of functions/filters to export from this session state object...
        /// </summary>
        internal List<CmdletInfo> ExportedCmdlets { get; } = new List<CmdletInfo>();

        /// <summary>
        /// Defines the default command visibility for this session state. Binding an InitialSessionState instance
        /// with private members will set this to Private.
        /// </summary>
        internal SessionStateEntryVisibility DefaultCommandVisibility = SessionStateEntryVisibility.Public;

        /// <summary>
        /// Add an new SessionState cmdlet entry to this session state object...
        /// </summary>
        /// <param name="entry">The entry to add.</param>
        internal void AddSessionStateEntry(SessionStateCmdletEntry entry)
        {
            AddSessionStateEntry(entry, /*local*/false);
        }

        /// <summary>
        /// Add an new SessionState cmdlet entry to this session state object...
        /// </summary>
        /// <param name="entry">The entry to add.</param>
        /// <param name="local">If local, add cmdlet to current scope. Else, add to module scope.</param>
        internal void AddSessionStateEntry(SessionStateCmdletEntry entry, bool local)
        {
            ExecutionContext.CommandDiscovery.AddSessionStateCmdletEntryToCache(entry, local);
        }

        /// <summary>
        /// Add an new SessionState cmdlet entry to this session state object...
        /// </summary>
        /// <param name="entry">The entry to add.</param>
        internal void AddSessionStateEntry(SessionStateApplicationEntry entry)
        {
            this.Applications.Add(entry.Path);
        }

        /// <summary>
        /// Add an new SessionState cmdlet entry to this session state object...
        /// </summary>
        /// <param name="entry">The entry to add.</param>
        internal void AddSessionStateEntry(SessionStateScriptEntry entry)
        {
            this.Scripts.Add(entry.Path);
        }

        /// <summary>
        /// Add the variables that must always be present in a SessionState instance...
        /// </summary>
        internal void InitializeFixedVariables()
        {
            //
            // BUGBUG
            //
            // String resources for aliases are currently associated with Runspace init
            //

            // $Host
            PSVariable v = new PSVariable(
                    SpecialVariables.Host,
                    ExecutionContext.EngineHostInterface,
                    ScopedItemOptions.Constant | ScopedItemOptions.AllScope,
                    RunspaceInit.PSHostDescription);
            this.GlobalScope.SetVariable(v.Name, v, asValue: false, force: true, this, CommandOrigin.Internal, fastPath: true);

            // $HOME - indicate where a user's home directory is located in the file system.
            //    -- %USERPROFILE% on windows
            //    -- %HOME% on unix
            string home = Environment.GetEnvironmentVariable(Platform.CommonEnvVariableNames.Home) ?? string.Empty;
            v = new PSVariable(SpecialVariables.Home,
                    home,
                    ScopedItemOptions.ReadOnly | ScopedItemOptions.AllScope,
                    RunspaceInit.HOMEDescription);
            this.GlobalScope.SetVariable(v.Name, v, asValue: false, force: true, this, CommandOrigin.Internal, fastPath: true);

            // $ExecutionContext
            v = new PSVariable(SpecialVariables.ExecutionContext,
                    ExecutionContext.EngineIntrinsics,
                    ScopedItemOptions.Constant | ScopedItemOptions.AllScope,
                    RunspaceInit.ExecutionContextDescription);
            this.GlobalScope.SetVariable(v.Name, v, asValue: false, force: true, this, CommandOrigin.Internal, fastPath: true);

            // $PSVersionTable
            v = new PSVariable(SpecialVariables.PSVersionTable,
                    PSVersionInfo.GetPSVersionTable(),
                    ScopedItemOptions.Constant | ScopedItemOptions.AllScope,
                    RunspaceInit.PSVersionTableDescription);
            this.GlobalScope.SetVariable(v.Name, v, asValue: false, force: true, this, CommandOrigin.Internal, fastPath: true);

            // $PSEdition
            v = new PSVariable(SpecialVariables.PSEdition,
                    PSVersionInfo.PSEditionValue,
                    ScopedItemOptions.Constant | ScopedItemOptions.AllScope,
                    RunspaceInit.PSEditionDescription);
            this.GlobalScope.SetVariable(v.Name, v, asValue: false, force: true, this, CommandOrigin.Internal, fastPath: true);

            // $PID
            v = new PSVariable(
                    SpecialVariables.PID,
                    Environment.ProcessId,
                    ScopedItemOptions.Constant | ScopedItemOptions.AllScope,
                    RunspaceInit.PIDDescription);
            this.GlobalScope.SetVariable(v.Name, v, asValue: false, force: true, this, CommandOrigin.Internal, fastPath: true);

            // $PSCulture
            v = new PSCultureVariable();
            this.GlobalScope.SetVariableForce(v, this);

            // $PSUICulture
            v = new PSUICultureVariable();
            this.GlobalScope.SetVariableForce(v, this);

            // $?
            v = new QuestionMarkVariable(this.ExecutionContext);
            this.GlobalScope.SetVariableForce(v, this);

            // $ShellId - if there is no runspace config, use the default string
            string shellId = ExecutionContext.ShellID;
            v = new PSVariable(SpecialVariables.ShellId, shellId,
                   ScopedItemOptions.Constant | ScopedItemOptions.AllScope,
                    RunspaceInit.MshShellIdDescription);
            this.GlobalScope.SetVariable(v.Name, v, asValue: false, force: true, this, CommandOrigin.Internal, fastPath: true);

            // $PSHOME
            string applicationBase = Utils.DefaultPowerShellAppBase;
            v = new PSVariable(SpecialVariables.PSHome, applicationBase,
                    ScopedItemOptions.Constant | ScopedItemOptions.AllScope,
                    RunspaceInit.PSHOMEDescription);
            this.GlobalScope.SetVariable(v.Name, v, asValue: false, force: true, this, CommandOrigin.Internal, fastPath: true);

            // $PSUserContentPath - the user content path for modules, scripts, and help
            // Uses custom variable class that provides a helpful error message when users try to set it directly
            string psContentPath = Utils.GetPSContentPath();
            v = new PSUserContentPathVariable(psContentPath);
            this.GlobalScope.SetVariable(v.Name, v, asValue: false, force: true, this, CommandOrigin.Internal, fastPath: true);

            // $EnabledExperimentalFeatures
            v = new PSVariable(SpecialVariables.EnabledExperimentalFeatures,
                               ExperimentalFeature.EnabledExperimentalFeatureNames,
                               ScopedItemOptions.Constant | ScopedItemOptions.AllScope,
                               RunspaceInit.EnabledExperimentalFeatures);
            this.GlobalScope.SetVariable(v.Name, v, asValue: false, force: true, this, CommandOrigin.Internal, fastPath: true);
        }

        /// <summary>
        /// Check to see if an application is allowed to be run.
        /// </summary>
        /// <param name="applicationPath">The path to the application to check.</param>
        /// <returns>True if application is permitted.</returns>
        internal SessionStateEntryVisibility CheckApplicationVisibility(string applicationPath)
        {
            return checkPathVisibility(Applications, applicationPath);
        }

        private static SessionStateEntryVisibility checkPathVisibility(List<string> list, string path)
        {
            if (list == null || list.Count == 0)
            {
                return SessionStateEntryVisibility.Private;
            }

            if (string.IsNullOrEmpty(path))
            {
                return SessionStateEntryVisibility.Private;
            }

            if (list.Contains("*"))
            {
                return SessionStateEntryVisibility.Public;
            }

            foreach (string p in list)
            {
                if (string.Equals(p, path, StringComparison.OrdinalIgnoreCase))
                {
                    return SessionStateEntryVisibility.Public;
                }

                if (WildcardPattern.ContainsWildcardCharacters(p))
                {
                    WildcardPattern pattern = WildcardPattern.Get(p, WildcardOptions.IgnoreCase);
                    if (pattern.IsMatch(path))
                    {
                        return SessionStateEntryVisibility.Public;
                    }
                }
            }

            return SessionStateEntryVisibility.Private;
        }

        #endregion Private data

        /// <summary>
        /// Notification for SessionState to do cleanup
        /// before runspace is closed.
        /// </summary>
        internal void RunspaceClosingNotification()
        {
            if (this != ExecutionContext.TopLevelSessionState && Providers.Count > 0)
            {
                // Remove all providers at the top level...

                CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);

                foreach (string providerName in Providers.Keys)
                {
                    // All errors are ignored.

                    RemoveProvider(providerName, true, context);
                }
            }
        }

        #region Errors

        /// <summary>
        /// Constructs a new instance of a ProviderInvocationException
        /// using the specified data.
        /// </summary>
        /// <param name="resourceId">
        /// The resource ID to use as the format message for the error.
        /// </param>
        /// <param name="resourceStr">
        /// This is the message template string.
        /// </param>
        /// <param name="provider">
        /// The provider information used when formatting the error message.
        /// </param>
        /// <param name="path">
        /// The path used when formatting the error message.
        /// </param>
        /// <param name="e">
        /// The exception that was thrown by the provider. This will be set as
        /// the ProviderInvocationException's InnerException and the message will
        /// be used when formatting the error message.
        /// </param>
        /// <returns>
        /// A new instance of a ProviderInvocationException.
        /// </returns>
        /// <exception cref="ProviderInvocationException">
        /// Wraps <paramref name="e"/> in a ProviderInvocationException
        /// and then throws it.
        /// </exception>
        internal ProviderInvocationException NewProviderInvocationException(
            string resourceId,
            string resourceStr,
            ProviderInfo provider,
            string path,
            Exception e)
        {
            return NewProviderInvocationException(resourceId, resourceStr, provider, path, e, true);
        }

        /// <summary>
        /// Constructs a new instance of a ProviderInvocationException
        /// using the specified data.
        /// </summary>
        /// <param name="resourceId">
        /// The resource ID to use as the format message for the error.
        /// </param>
        /// <param name="resourceStr">
        /// This is the message template string.
        /// </param>
        /// <param name="provider">
        /// The provider information used when formatting the error message.
        /// </param>
        /// <param name="path">
        /// The path used when formatting the error message.
        /// </param>
        /// <param name="e">
        /// The exception that was thrown by the provider. This will be set as
        /// the ProviderInvocationException's InnerException and the message will
        /// be used when formatting the error message.
        /// </param>
        /// <param name="useInnerExceptionErrorMessage">
        /// If true, the error record from the inner exception will be used if it contains one.
        /// If false, the error message specified by the resourceId will be used.
        /// </param>
        /// <returns>
        /// A new instance of a ProviderInvocationException.
        /// </returns>
        /// <exception cref="ProviderInvocationException">
        /// Wraps <paramref name="e"/> in a ProviderInvocationException
        /// and then throws it.
        /// </exception>
        internal ProviderInvocationException NewProviderInvocationException(
            string resourceId,
            string resourceStr,
            ProviderInfo provider,
            string path,
            Exception e,
            bool useInnerExceptionErrorMessage)
        {
            //  If the innerException was itself thrown by
            //  ProviderBase.ThrowTerminatingError, it is already a
            //  ProviderInvocationException, and we don't want to
            //  re-wrap it.
            ProviderInvocationException pie = e as ProviderInvocationException;
            if (pie != null)
            {
                pie._providerInfo = provider;
                return pie;
            }

            pie = new ProviderInvocationException(resourceId, resourceStr, provider, path, e, useInnerExceptionErrorMessage);

            // Log a provider health event

            MshLog.LogProviderHealthEvent(
                ExecutionContext,
                provider.Name,
                pie,
                Severity.Warning);

            return pie;
        }
        #endregion Errors
    }
}
