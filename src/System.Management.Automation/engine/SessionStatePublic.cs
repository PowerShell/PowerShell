// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation.Runspaces;

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    /// <summary>
    /// Exposes the APIs to manipulate the providers, Runspace data, and location to the Cmdlet base class.
    /// </summary>
    public sealed class SessionState
    {
        #region Constructors

        /// <summary>
        /// The internal constructor for this object. It should be the only one that gets called.
        /// </summary>
        /// <param name="sessionState">
        /// An instance of SessionState that the APIs should work against.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="sessionState"/> is null.
        /// </exception>
        internal SessionState(SessionStateInternal sessionState)
        {
            if (sessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sessionState));
            }

            _sessionState = sessionState;
        }

        /// <summary>
        /// The internal constructor for this object. It should be the only one that gets called.
        /// </summary>
        /// <param name="context">
        /// An instance of ExecutionContext whose EngineSessionState represents the parent session state.
        /// </param>
        /// <param name="createAsChild">
        /// True if the session state should be created as a child session state.
        /// </param>
        /// <param name="linkToGlobal">
        /// True if the session state should be linked to the global scope.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="context"/> is null.
        /// </exception>
        internal SessionState(ExecutionContext context, bool createAsChild, bool linkToGlobal)
        {
            if (context == null)
                throw new InvalidOperationException("ExecutionContext");

            if (createAsChild)
            {
                _sessionState = new SessionStateInternal(context.EngineSessionState, linkToGlobal, context);
            }
            else
            {
                _sessionState = new SessionStateInternal(context);
            }

            _sessionState.PublicSessionState = this;
        }

        /// <summary>
        /// Construct a new session state object...
        /// </summary>
        public SessionState()
        {
            ExecutionContext ecFromTLS = LocalPipeline.GetExecutionContextFromTLS();
            if (ecFromTLS == null)
                throw new InvalidOperationException("ExecutionContext");

            _sessionState = new SessionStateInternal(ecFromTLS);
            _sessionState.PublicSessionState = this;
        }

        #endregion Constructors

        #region Public methods

        /// <summary>
        /// Gets the APIs to access drives.
        /// </summary>
        public DriveManagementIntrinsics Drive
        {
            get { return _drive ??= new DriveManagementIntrinsics(_sessionState); }
        }

        /// <summary>
        /// Gets the APIs to access providers.
        /// </summary>
        public CmdletProviderManagementIntrinsics Provider
        {
            get { return _provider ??= new CmdletProviderManagementIntrinsics(_sessionState); }
        }

        /// <summary>
        /// Gets the APIs to access paths and location.
        /// </summary>
        public PathIntrinsics Path
        {
            get { return _path ??= new PathIntrinsics(_sessionState); }
        }

        /// <summary>
        /// Gets the APIs to access variables in session state.
        /// </summary>
        public PSVariableIntrinsics PSVariable
        {
            get { return _variable ??= new PSVariableIntrinsics(_sessionState); }
        }

        /// <summary>
        /// Get/set constraints for this execution environment.
        /// </summary>
        public PSLanguageMode LanguageMode
        {
            get { return _sessionState.LanguageMode; }

            set { _sessionState.LanguageMode = value; }
        }

        /// <summary>
        /// If true the PowerShell debugger will use FullLanguage mode, otherwise it will use the current language mode.
        /// </summary>
        public bool UseFullLanguageModeInDebugger
        {
            get { return _sessionState.UseFullLanguageModeInDebugger; }
        }

        /// <summary>
        /// Public proxy for the list of scripts that are allowed to be run. If the name "*"
        /// is in the list, then all scripts can be run. (This is the default.)
        /// </summary>
        public List<string> Scripts
        {
            get { return _sessionState.Scripts; }
        }

        /// <summary>
        /// Public proxy for the list of applications that are allowed to be run. If the name "*"
        /// is in the list, then all applications can be run. (This is the default.)
        /// </summary>
        public List<string> Applications
        {
            get { return _sessionState.Applications; }
        }

        /// <summary>
        /// The module associated with this session state instance...
        /// </summary>
        public PSModuleInfo Module
        {
            get { return _sessionState.Module; }
        }

        /// <summary>
        /// The provider intrinsics for this session state instance.
        /// </summary>
        public ProviderIntrinsics InvokeProvider
        {
            get { return _sessionState.InvokeProvider; }
        }

        /// <summary>
        /// The command invocation intrinsics for this session state instance.
        /// </summary>
        public CommandInvocationIntrinsics InvokeCommand
        {
            get { return _sessionState.ExecutionContext.EngineIntrinsics.InvokeCommand; }
        }

        /// <summary>
        /// Utility to check the visibility of an object based on the current
        /// command origin. If the object implements IHasSessionStateEntryVisibility
        /// then the check will be made. If the check fails, then an exception will be thrown...
        /// </summary>
        /// <param name="origin">The command origin value to check against...</param>
        /// <param name="valueToCheck">The object to check.</param>
        public static void ThrowIfNotVisible(CommandOrigin origin, object valueToCheck)
        {
            SessionStateException exception;
            if (!IsVisible(origin, valueToCheck))
            {
                PSVariable sv = valueToCheck as PSVariable;
                if (sv != null)
                {
                    exception =
                       new SessionStateException(
                           sv.Name,
                           SessionStateCategory.Variable,
                           "VariableIsPrivate",
                           SessionStateStrings.VariableIsPrivate,
                           ErrorCategory.PermissionDenied);

                    throw exception;
                }

                CommandInfo cinfo = valueToCheck as CommandInfo;
                if (cinfo != null)
                {
                    string commandName = cinfo.Name;
                    if (commandName != null)
                    {
                        // If we have a name, use it in the error message
                        exception =
                            new SessionStateException(
                                commandName,
                                SessionStateCategory.Command,
                                "NamedCommandIsPrivate",
                                SessionStateStrings.NamedCommandIsPrivate,
                                ErrorCategory.PermissionDenied);
                    }
                    else
                    {
                        exception =
                            new SessionStateException(
                                string.Empty,
                                SessionStateCategory.Command,
                                "CommandIsPrivate",
                                SessionStateStrings.CommandIsPrivate,
                                ErrorCategory.PermissionDenied);
                    }

                    throw exception;
                }

                // Catch all error for other types of resources...
                exception =
                    new SessionStateException(
                        null,
                        SessionStateCategory.Resource,
                        "ResourceIsPrivate",
                        SessionStateStrings.ResourceIsPrivate,
                        ErrorCategory.PermissionDenied);

                throw exception;
            }
        }

        /// <summary>
        /// Checks the visibility of an object based on the command origin argument.
        /// </summary>
        /// <param name="origin">The origin to check against.</param>
        /// <param name="valueToCheck">The object to check.</param>
        /// <returns>Returns true if the object is visible, false otherwise.</returns>
        public static bool IsVisible(CommandOrigin origin, object valueToCheck)
        {
            if (origin == CommandOrigin.Internal)
                return true;
            IHasSessionStateEntryVisibility obj = valueToCheck as IHasSessionStateEntryVisibility;
            if (obj != null)
            {
                return (obj.Visibility == SessionStateEntryVisibility.Public);
            }

            return true;
        }
        /// <summary>
        /// Checks the visibility of an object based on the command origin argument.
        /// </summary>
        /// <param name="origin">The origin to check against.</param>
        /// <param name="variable">The variable to check.</param>
        /// <returns>Returns true if the object is visible, false otherwise.</returns>
        public static bool IsVisible(CommandOrigin origin, PSVariable variable)
        {
            if (origin == CommandOrigin.Internal)
                return true;
            if (variable == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(variable));
            }

            return (variable.Visibility == SessionStateEntryVisibility.Public);
        }
        /// <summary>
        /// Checks the visibility of an object based on the command origin argument.
        /// </summary>
        /// <param name="origin">The origin to check against.</param>
        /// <param name="commandInfo">The command to check.</param>
        /// <returns>Returns true if the object is visible, false otherwise.</returns>
        public static bool IsVisible(CommandOrigin origin, CommandInfo commandInfo)
        {
            if (origin == CommandOrigin.Internal)
                return true;
            if (commandInfo == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(commandInfo));
            }

            return (commandInfo.Visibility == SessionStateEntryVisibility.Public);
        }

        #endregion Public methods

        #region Internal methods

        /// <summary>
        /// Gets a reference to the "real" session state object instead of the facade.
        /// </summary>
        internal SessionStateInternal Internal
        {
            get { return _sessionState; }
        }
        #endregion Internal methods

        #region private data

        private readonly SessionStateInternal _sessionState;
        private DriveManagementIntrinsics _drive;
        private CmdletProviderManagementIntrinsics _provider;
        private PathIntrinsics _path;
        private PSVariableIntrinsics _variable;

        #endregion private data
    }

    /// <summary>
    /// This enum defines the visibility of execution environment elements...
    /// </summary>
    public enum SessionStateEntryVisibility
    {
        /// <summary>
        /// Entries are visible to requests from outside the runspace.
        /// </summary>
        Public = 0,

        /// <summary>
        /// Entries are not visible to requests from outside the runspace.
        /// </summary>
        Private = 1
    }

#nullable enable
    internal interface IHasSessionStateEntryVisibility
    {
        SessionStateEntryVisibility Visibility { get; set; }
    }

    /// <summary>
    /// This enum defines what subset of the PowerShell language is permitted when
    /// calling into this execution environment.
    /// </summary>
    public enum PSLanguageMode
    {
        /// <summary>
        /// All PowerShell language elements are available.
        /// </summary>
        FullLanguage = 0,

        /// <summary>
        /// A subset of language elements are available to external requests.
        /// </summary>
        RestrictedLanguage = 1,

        /// <summary>
        /// Commands containing script text to evaluate are not allowed. You can only
        /// call commands using the Runspace APIs when in this mode.
        /// </summary>
        NoLanguage = 2,

        /// <summary>
        /// Exposes a subset of the PowerShell language that limits itself to core PowerShell
        /// types, does not support method invocation (except on those types), and does not
        /// support property setters (except on those types).
        /// </summary>
        ConstrainedLanguage = 3
    }
}
