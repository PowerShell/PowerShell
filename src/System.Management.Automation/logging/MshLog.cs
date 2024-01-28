// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Tracing;
using System.Security;
using System.Threading;

namespace System.Management.Automation
{
    /// <summary>
    /// Monad Logging in general is a two layer architecture. At the upper layer are the
    /// Msh Log Engine and Logging Api. At the lower layer is the Provider Interface
    /// and Log Providers. This architecture is adopted to achieve independency between
    /// Monad logging and logging details of different logging technology.
    ///
    /// This file implements the upper layer of the Monad Logging architecture.
    /// Lower layer of Msh Log architecture is implemented in LogProvider.cs file.
    ///
    /// Logging Api is made up of following five sets
    ///   1. Engine Health Event
    ///   2. Engine Lifecycle Event
    ///   3. Command Lifecycle Event
    ///   4. Provider Lifecycle Event
    ///   5. Settings Event
    ///
    /// Msh Log Engine provides features in following areas,
    ///   1. Loading and managing logging providers. Based on some "Provider Catalog", engine will try to
    ///      load providers. First provider that is successfully loaded will be used for low level logging.
    ///      If no providers can be loaded, a dummy provider will be used, which will essentially do nothing.
    ///   2. Implementation of logging api functions. These api functions is implemented by calling corresponding
    ///      functions in provider interface.
    ///   3. Sequence Id Generation. Unique id are generated in this class. These id's will be attached to events.
    ///   4. Monad engine state management. Engine state is stored in ExecutionContext class but managed here.
    ///      Later on, this feature may be moved to engine itself (where it should belongs to) when sophisticated
    ///      engine state model is established.
    ///   5. Logging policy support. Events are logged or not logged based on logging policy settings (which is stored
    ///      in session state of the engine.
    ///
    /// MshLog class is defined as a static class. This essentially make the logging api to be a static api.
    ///
    /// We want to provide sufficient synchronization for static functions calls.
    /// This is not needed for now because of following two reasons,
    ///     a. Currently, only one monad engine can be running in one process. So logically only one
    ///        event will be log at a time.
    ///     b. Even in the case of multiple events are logged, underlining logging media should
    ///        provide synchronization.
    /// </summary>
    internal static class MshLog
    {
        #region Initialization

        /// <summary>
        /// A static dictionary to keep track of log providers for different shellId's.
        ///
        /// The value of this dictionary is never empty. A value of type DummyProvider means
        /// no logging.
        /// </summary>
        private static readonly ConcurrentDictionary<string, Collection<LogProvider>> s_logProviders =
            new ConcurrentDictionary<string, Collection<LogProvider>>();

        private const string _crimsonLogProviderAssemblyName = "MshCrimsonLog";
        private const string _crimsonLogProviderTypeName = "System.Management.Automation.Logging.CrimsonLogProvider";

        private static readonly Collection<string> s_ignoredCommands = new Collection<string>();

        /// <summary>
        /// Static constructor.
        /// </summary>
        static MshLog()
        {
            s_ignoredCommands.Add("Out-Lineoutput");
            s_ignoredCommands.Add("Format-Default");
        }

        /// <summary>
        /// Currently initialization is done in following sequence
        ///    a. Try to load CrimsonLogProvider (in the case of Longhorn)
        ///    b. If a fails, use the DummyLogProvider instead. (in low-level OS)
        ///
        /// In the longer turn, we may need to use a "Provider Catalog" for
        /// log provider loading.
        /// </summary>
        /// <param name="shellId"></param>
        /// <returns></returns>
        private static IEnumerable<LogProvider> GetLogProvider(string shellId)
        {
            return s_logProviders.GetOrAdd(shellId, CreateLogProvider);
        }

        /// <summary>
        /// Get Log Provider based on Execution Context.
        /// </summary>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        private static IEnumerable<LogProvider> GetLogProvider(ExecutionContext executionContext)
        {
            if (executionContext == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(executionContext));
            }

            string shellId = executionContext.ShellID;

            return GetLogProvider(shellId);
        }

        /// <summary>
        /// Get Log Provider based on Log Context.
        /// </summary>
        /// <param name="logContext"></param>
        /// <returns></returns>
        private static IEnumerable<LogProvider> GetLogProvider(LogContext logContext)
        {
            System.Diagnostics.Debug.Assert(logContext != null);
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(logContext.ShellId));

            return GetLogProvider(logContext.ShellId);
        }

        /// <summary>
        /// Create a log provider based on a shell Id.
        /// </summary>
        /// <param name="shellId"></param>
        /// <returns></returns>
        private static Collection<LogProvider> CreateLogProvider(string shellId)
        {
            Collection<LogProvider> providers = new Collection<LogProvider>();
            // Porting note: Linux does not support ETW

            try
            {
#if UNIX
                LogProvider sysLogProvider = new PSSysLogProvider();
                providers.Add(sysLogProvider);
#else
                LogProvider etwLogProvider = new PSEtwLogProvider();
                providers.Add(etwLogProvider);
#endif

                return providers;
            }
            catch (ArgumentException)
            {
            }
            catch (InvalidOperationException)
            {
            }
            catch (SecurityException)
            {
                // This exception will happen if we try to create an event source
                // (corresponding to the current running minishell)
                // when running as non-admin user. In that case, we will default
                // to dummy log.
            }

            providers.Add(new DummyLogProvider());
            return providers;
        }

        /// <summary>
        /// This will set the current log provider to be dummy log.
        /// </summary>
        /// <param name="shellId"></param>
        internal static void SetDummyLog(string shellId)
        {
            Collection<LogProvider> providers = new Collection<LogProvider> { new DummyLogProvider() };
            s_logProviders.AddOrUpdate(shellId, providers, (key, value) => providers);
        }

        #endregion

        #region Engine Health Event Logging Api

        /// <summary>
        /// LogEngineHealthEvent: Log an engine health event. If engine state is changed, a engine
        /// lifecycle event will be logged also.
        ///
        /// This is the basic form of EngineHealthEvent logging api, in which all parameters are provided.
        ///
        /// Variant form of this function is defined below, which will make parameters additionalInfo
        /// and newEngineState optional.
        /// </summary>
        /// <param name="executionContext">Execution context for the engine that is running.</param>
        /// <param name="eventId">EventId for the event to be logged.</param>
        /// <param name="exception">Exception associated with this event.</param>
        /// <param name="severity">Severity of this event.</param>
        /// <param name="additionalInfo">Additional information for this event.</param>
        /// <param name="newEngineState">New engine state.</param>
        internal static void LogEngineHealthEvent(ExecutionContext executionContext,
                                                int eventId,
                                                Exception exception,
                                                Severity severity,
                                                Dictionary<string, string> additionalInfo,
                                                EngineState newEngineState)
        {
            if (executionContext == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(executionContext));
                return;
            }

            if (exception == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(exception));
                return;
            }

            InvocationInfo invocationInfo = null;
            if (exception is IContainsErrorRecord icer && icer.ErrorRecord != null)
            {
                invocationInfo = icer.ErrorRecord.InvocationInfo;
            }

            foreach (LogProvider provider in GetLogProvider(executionContext))
            {
                if (NeedToLogEngineHealthEvent(provider, executionContext))
                {
                    provider.LogEngineHealthEvent(GetLogContext(executionContext, invocationInfo, severity), eventId, exception, additionalInfo);
                }
            }

            if (newEngineState != EngineState.None)
            {
                LogEngineLifecycleEvent(executionContext, newEngineState, invocationInfo);
            }
        }

        /// <summary>
        /// This is a variation of LogEngineHealthEvent api to make additionalInfo and newEngineState
        /// optional.
        /// </summary>
        /// <param name="executionContext"></param>
        /// <param name="eventId"></param>
        /// <param name="exception"></param>
        /// <param name="severity"></param>
        internal static void LogEngineHealthEvent(ExecutionContext executionContext,
                                                int eventId,
                                                Exception exception,
                                                Severity severity)
        {
            LogEngineHealthEvent(executionContext, eventId, exception, severity, null);
        }

        /// <summary>
        /// This is a variation of LogEngineHealthEvent api to make eventid, additionalInfo and newEngineState
        /// optional.
        ///
        /// A default event id for engine health event will be used.
        /// </summary>
        /// <param name="executionContext"></param>
        /// <param name="exception"></param>
        /// <param name="severity"></param>
        internal static void LogEngineHealthEvent(ExecutionContext executionContext,
                                                Exception exception,
                                                Severity severity)
        {
            LogEngineHealthEvent(executionContext, 100, exception, severity, null);
        }

        /// <summary>
        /// This is a variation of LogEngineHealthEvent api to make newEngineState
        /// optional.
        /// </summary>
        /// <param name="executionContext"></param>
        /// <param name="eventId"></param>
        /// <param name="exception"></param>
        /// <param name="severity"></param>
        /// <param name="additionalInfo"></param>
        internal static void LogEngineHealthEvent(ExecutionContext executionContext,
                                                int eventId,
                                                Exception exception,
                                                Severity severity,
                                                Dictionary<string, string> additionalInfo)
        {
            LogEngineHealthEvent(executionContext, eventId, exception, severity, additionalInfo, EngineState.None);
        }

        /// <summary>
        /// This is a variation of LogEngineHealthEvent api to make additionalInfo
        /// optional.
        /// </summary>
        /// <param name="executionContext"></param>
        /// <param name="eventId"></param>
        /// <param name="exception"></param>
        /// <param name="severity"></param>
        /// <param name="newEngineState"></param>
        internal static void LogEngineHealthEvent(ExecutionContext executionContext,
                                                int eventId,
                                                Exception exception,
                                                Severity severity,
                                                EngineState newEngineState)
        {
            LogEngineHealthEvent(executionContext, eventId, exception, severity, null, newEngineState);
        }

        /// <summary>
        /// LogEngineHealthEvent: This is an API for logging engine health event while execution context
        /// is not available. In this case, caller of this API will directly construct LogContext
        /// instance.
        ///
        /// This API is currently used only by runspace before engine start.
        /// </summary>
        /// <param name="logContext">LogContext to be.</param>
        /// <param name="eventId">EventId for the event to be logged.</param>
        /// <param name="exception">Exception associated with this event.</param>
        /// <param name="additionalInfo">Additional information for this event.</param>
        internal static void LogEngineHealthEvent(LogContext logContext,
                                                int eventId,
                                                Exception exception,
                                                Dictionary<string, string> additionalInfo
                                                )
        {
            if (logContext == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(logContext));
                return;
            }

            if (exception == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(exception));
                return;
            }

            // Here execution context doesn't exist, we will have to log this event regardless.
            // Don't check NeedToLogEngineHealthEvent here.
            foreach (LogProvider provider in GetLogProvider(logContext))
            {
                provider.LogEngineHealthEvent(logContext, eventId, exception, additionalInfo);
            }
        }

        #endregion

        #region Engine Lifecycle Event Logging Api

        /// <summary>
        /// LogEngineLifecycleEvent: Log an engine lifecycle event.
        ///
        /// This is the basic form of EngineLifecycleEvent logging api, in which all parameters are provided.
        ///
        /// Variant form of this function is defined below, which will make parameter additionalInfo
        /// optional.
        /// </summary>
        /// <param name="executionContext">Execution context for current engine instance.</param>
        /// <param name="engineState">New engine state.</param>
        /// <param name="invocationInfo">InvocationInfo for current command that is running.</param>
        internal static void LogEngineLifecycleEvent(ExecutionContext executionContext,
                                                EngineState engineState,
                                                InvocationInfo invocationInfo)
        {
            if (executionContext == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(executionContext));
                return;
            }

            EngineState previousState = GetEngineState(executionContext);
            if (engineState == previousState)
                return;

            foreach (LogProvider provider in GetLogProvider(executionContext))
            {
                if (NeedToLogEngineLifecycleEvent(provider, executionContext))
                {
                    provider.LogEngineLifecycleEvent(GetLogContext(executionContext, invocationInfo), engineState, previousState);
                }
            }

            SetEngineState(executionContext, engineState);
        }

        /// <summary>
        /// This is a variation of basic LogEngineLifeCycleEvent api which makes invocationInfo
        /// optional.
        /// </summary>
        /// <param name="executionContext"></param>
        /// <param name="engineState"></param>
        internal static void LogEngineLifecycleEvent(ExecutionContext executionContext,
                                                EngineState engineState)
        {
            LogEngineLifecycleEvent(executionContext, engineState, null);
        }

        #endregion

        #region Command Health Event Logging Api

        /// <summary>
        /// LogProviderHealthEvent: Log a command health event.
        /// </summary>
        /// <param name="executionContext">Execution context for the engine that is running.</param>
        /// <param name="exception">Exception associated with this event.</param>
        /// <param name="severity">Severity of this event.</param>
        internal static void LogCommandHealthEvent(ExecutionContext executionContext,
                                                Exception exception,
                                                Severity severity
                                                )
        {
            if (executionContext == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(executionContext));
                return;
            }

            if (exception == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(exception));
                return;
            }

            InvocationInfo invocationInfo = null;
            if (exception is IContainsErrorRecord icer && icer.ErrorRecord != null)
            {
                invocationInfo = icer.ErrorRecord.InvocationInfo;
            }

            foreach (LogProvider provider in GetLogProvider(executionContext))
            {
                if (NeedToLogCommandHealthEvent(provider, executionContext))
                {
                    provider.LogCommandHealthEvent(GetLogContext(executionContext, invocationInfo, severity), exception);
                }
            }
        }

        #endregion

        #region Command Lifecycle Event Logging Api

        /// <summary>
        /// LogCommandLifecycleEvent: Log a command lifecycle event.
        ///
        /// This is the only form of CommandLifecycleEvent logging api.
        /// </summary>
        /// <param name="executionContext">Execution Context for the current running engine.</param>
        /// <param name="commandState">New command state.</param>
        /// <param name="invocationInfo">Invocation data for current command that is running.</param>
        internal static void LogCommandLifecycleEvent(ExecutionContext executionContext,
                                                CommandState commandState,
                                                InvocationInfo invocationInfo)
        {
            if (executionContext == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(executionContext));
                return;
            }

            if (invocationInfo == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(invocationInfo));
                return;
            }

            if (s_ignoredCommands.Contains(invocationInfo.MyCommand.Name))
            {
                return;
            }

            LogContext logContext = null;
            foreach (LogProvider provider in GetLogProvider(executionContext))
            {
                if (NeedToLogCommandLifecycleEvent(provider, executionContext))
                {
                    provider.LogCommandLifecycleEvent(
                        () => logContext ??= GetLogContext(executionContext, invocationInfo),
                        commandState);
                }
            }
        }

        /// <summary>
        /// LogCommandLifecycleEvent: Log a command lifecycle event.
        ///
        /// This is a form of CommandLifecycleEvent which takes a commandName instead
        /// of invocationInfo. It is likely that invocationInfo is not available if
        /// the command failed security check.
        /// </summary>
        /// <param name="executionContext">Execution Context for the current running engine.</param>
        /// <param name="commandState">New command state.</param>
        /// <param name="commandName">Current command that is running.</param>
        internal static void LogCommandLifecycleEvent(ExecutionContext executionContext,
                                                CommandState commandState,
                                                string commandName)
        {
            if (executionContext == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(executionContext));
                return;
            }

            LogContext logContext = null;
            foreach (LogProvider provider in GetLogProvider(executionContext))
            {
                if (NeedToLogCommandLifecycleEvent(provider, executionContext))
                {
                    provider.LogCommandLifecycleEvent(
                        () =>
                        {
                            if (logContext == null)
                            {
                                logContext = GetLogContext(executionContext, null);
                                logContext.CommandName = commandName;
                            }

                            return logContext;
                        }, commandState);
                }
            }
        }

        #endregion

        #region Pipeline Execution Detail Event Logging Api

        /// <summary>
        /// LogPipelineExecutionDetailEvent: Log a pipeline execution detail event.
        /// </summary>
        /// <param name="executionContext">Execution Context for the current running engine.</param>
        /// <param name="detail">Detail to be logged for this pipeline execution detail.</param>
        /// <param name="invocationInfo">Invocation data for current command that is running.</param>
        internal static void LogPipelineExecutionDetailEvent(ExecutionContext executionContext,
                                                            List<string> detail,
                                                            InvocationInfo invocationInfo)

        {
            if (executionContext == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(executionContext));
                return;
            }

            foreach (LogProvider provider in GetLogProvider(executionContext))
            {
                if (NeedToLogPipelineExecutionDetailEvent(provider, executionContext))
                {
                    provider.LogPipelineExecutionDetailEvent(GetLogContext(executionContext, invocationInfo), detail);
                }
            }
        }

        /// <summary>
        /// LogPipelineExecutionDetailEvent: Log a pipeline execution detail event.
        ///
        /// This is a form of PipelineExecutionDetailEvent which takes a scriptName and commandLine
        /// instead of invocationInfo. This will save the need to fill in the commandName for
        /// this event.
        /// </summary>
        /// <param name="executionContext">Execution Context for the current running engine.</param>
        /// <param name="detail">Detail to be logged for this pipeline execution detail.</param>
        /// <param name="scriptName">Script that is currently running.</param>
        /// <param name="commandLine">Command line that is currently running.</param>
        internal static void LogPipelineExecutionDetailEvent(ExecutionContext executionContext,
                                                            List<string> detail,
                                                            string scriptName,
                                                            string commandLine)
        {
            if (executionContext == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(executionContext));
                return;
            }

            LogContext logContext = GetLogContext(executionContext, null);
            logContext.CommandLine = commandLine;
            logContext.ScriptName = scriptName;

            foreach (LogProvider provider in GetLogProvider(executionContext))
            {
                if (NeedToLogPipelineExecutionDetailEvent(provider, executionContext))
                {
                    provider.LogPipelineExecutionDetailEvent(logContext, detail);
                }
            }
        }

        #endregion

        #region Provider Health Event Logging Api

        /// <summary>
        /// LogProviderHealthEvent: Log a Provider health event.
        /// </summary>
        /// <param name="executionContext">Execution context for the engine that is running.</param>
        /// <param name="providerName">Name of the provider.</param>
        /// <param name="exception">Exception associated with this event.</param>
        /// <param name="severity">Severity of this event.</param>
        internal static void LogProviderHealthEvent(ExecutionContext executionContext,
                                                string providerName,
                                                Exception exception,
                                                Severity severity
                                                )
        {
            if (executionContext == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(executionContext));
                return;
            }

            if (exception == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(exception));
                return;
            }

            InvocationInfo invocationInfo = null;
            if (exception is IContainsErrorRecord icer && icer.ErrorRecord != null)
            {
                invocationInfo = icer.ErrorRecord.InvocationInfo;
            }

            foreach (LogProvider provider in GetLogProvider(executionContext))
            {
                if (NeedToLogProviderHealthEvent(provider, executionContext))
                {
                    provider.LogProviderHealthEvent(GetLogContext(executionContext, invocationInfo, severity), providerName, exception);
                }
            }
        }

        #endregion

        #region Provider Lifecycle Event Logging Api

        /// <summary>
        /// LogProviderLifecycleEvent: Log a provider lifecycle event.
        ///
        /// This is the only form of ProviderLifecycleEvent logging api.
        /// </summary>
        /// <param name="executionContext">Execution Context for current engine that is running.</param>
        /// <param name="providerName">Provider name.</param>
        /// <param name="providerState">New provider state.</param>
        internal static void LogProviderLifecycleEvent(ExecutionContext executionContext,
                                                     string providerName,
                                                     ProviderState providerState)
        {
            if (executionContext == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(executionContext));
                return;
            }

            foreach (LogProvider provider in GetLogProvider(executionContext))
            {
                if (NeedToLogProviderLifecycleEvent(provider, executionContext))
                {
                    provider.LogProviderLifecycleEvent(GetLogContext(executionContext, null), providerName, providerState);
                }
            }
        }

        #endregion

        #region Settings Event Logging Api

        /// <summary>
        /// LogSettingsEvent: Log a settings event
        ///
        /// This is the basic form of LoggingSettingsEvent API. Variation of this function defined
        /// below will make parameter invocationInfo optional.
        /// </summary>
        /// <param name="executionContext">Execution context for current running engine.</param>
        /// <param name="variableName">Variable name.</param>
        /// <param name="newValue">New value for the variable.</param>
        /// <param name="previousValue">Previous value for the variable.</param>
        /// <param name="invocationInfo">Invocation data for the command that is currently running.</param>
        internal static void LogSettingsEvent(ExecutionContext executionContext,
                                            string variableName,
                                            string newValue,
                                            string previousValue,
                                            InvocationInfo invocationInfo)
        {
            if (executionContext == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(executionContext));
                return;
            }

            foreach (LogProvider provider in GetLogProvider(executionContext))
            {
                if (NeedToLogSettingsEvent(provider, executionContext))
                {
                    provider.LogSettingsEvent(GetLogContext(executionContext, invocationInfo), variableName, newValue, previousValue);
                }
            }
        }

        /// <summary>
        /// This is a variation of basic LogSettingsEvent to make "invocationInfo" optional.
        /// </summary>
        /// <param name="executionContext"></param>
        /// <param name="variableName"></param>
        /// <param name="newValue"></param>
        /// <param name="previousValue"></param>
        internal static void LogSettingsEvent(ExecutionContext executionContext,
                                            string variableName,
                                            string newValue,
                                            string previousValue)
        {
            LogSettingsEvent(executionContext, variableName, newValue, previousValue, null);
        }

        #endregion

        #region Helper Functions

        /// <summary>
        /// Get current engine state for the engine instance corresponding to executionContext
        /// passed in.
        ///
        /// Engine state is stored in ExecutionContext.
        /// </summary>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        private static EngineState GetEngineState(ExecutionContext executionContext)
        {
            return executionContext.EngineState;
        }

        /// <summary>
        /// Set current engine state for the engine instance corresponding to executionContext
        /// passed in.
        ///
        /// Engine state is stored in ExecutionContext.
        /// </summary>
        /// <param name="executionContext"></param>
        /// <param name="engineState"></param>
        private static void SetEngineState(ExecutionContext executionContext, EngineState engineState)
        {
            executionContext.EngineState = engineState;
        }

        /// <summary>
        /// Generate LogContext structure based on executionContext and invocationInfo passed in.
        ///
        /// LogContext structure is used in log provider interface.
        /// </summary>
        /// <param name="executionContext"></param>
        /// <param name="invocationInfo"></param>
        /// <returns></returns>
        internal static LogContext GetLogContext(ExecutionContext executionContext, InvocationInfo invocationInfo)
        {
            return GetLogContext(executionContext, invocationInfo, Severity.Informational);
        }

        /// <summary>
        /// Generate LogContext structure based on executionContext and invocationInfo passed in.
        ///
        /// LogContext structure is used in log provider interface.
        /// </summary>
        /// <param name="executionContext"></param>
        /// <param name="invocationInfo"></param>
        /// <param name="severity"></param>
        /// <returns></returns>
        private static LogContext GetLogContext(ExecutionContext executionContext, InvocationInfo invocationInfo, Severity severity)
        {
            if (executionContext == null)
                return null;

            LogContext logContext = new LogContext();

            string shellId = executionContext.ShellID;

            logContext.ExecutionContext = executionContext;
            logContext.ShellId = shellId;
            logContext.Severity = severity.ToString();

            if (executionContext.EngineHostInterface != null)
            {
                logContext.HostName = executionContext.EngineHostInterface.Name;
                logContext.HostVersion = executionContext.EngineHostInterface.Version.ToString();
                logContext.HostId = (string)executionContext.EngineHostInterface.InstanceId.ToString();
            }

            logContext.HostApplication = string.Join(' ', Environment.GetCommandLineArgs());

            if (executionContext.CurrentRunspace != null)
            {
                logContext.EngineVersion = executionContext.CurrentRunspace.Version.ToString();
                logContext.RunspaceId = executionContext.CurrentRunspace.InstanceId.ToString();

                Pipeline currentPipeline = ((RunspaceBase)executionContext.CurrentRunspace).GetCurrentlyRunningPipeline();
                if (currentPipeline != null)
                {
                    logContext.PipelineId = currentPipeline.InstanceId.ToString(CultureInfo.CurrentCulture);
                }
            }

            logContext.SequenceNumber = NextSequenceNumber;

            try
            {
                if (executionContext.LogContextCache.User == null)
                {
                    logContext.User = Environment.UserDomainName + "\\" + Environment.UserName;
                    executionContext.LogContextCache.User = logContext.User;
                }
                else
                {
                    logContext.User = executionContext.LogContextCache.User;
                }
            }
            catch (InvalidOperationException)
            {
                logContext.User = Logging.UnknownUserName;
            }

            if (executionContext.SessionState.PSVariable.GetValue("PSSenderInfo") is System.Management.Automation.Remoting.PSSenderInfo psSenderInfo)
            {
                logContext.ConnectedUser = psSenderInfo.UserInfo.Identity.Name;
            }

            logContext.Time = DateTime.Now.ToString(CultureInfo.CurrentCulture);

            if (invocationInfo == null)
                return logContext;

            logContext.ScriptName = invocationInfo.ScriptName;
            logContext.CommandLine = invocationInfo.Line;

            if (invocationInfo.MyCommand != null)
            {
                logContext.CommandName = invocationInfo.MyCommand.Name;
                logContext.CommandType = invocationInfo.MyCommand.CommandType.ToString();

                switch (invocationInfo.MyCommand.CommandType)
                {
                    case CommandTypes.Application:
                        logContext.CommandPath = ((ApplicationInfo)invocationInfo.MyCommand).Path;
                        break;
                    case CommandTypes.ExternalScript:
                        logContext.CommandPath = ((ExternalScriptInfo)invocationInfo.MyCommand).Path;
                        break;
                }
            }

            return logContext;
        }

        #endregion

        #region Logging Policy

        /// <summary>
        /// NeedToLogEngineHealthEvent: check whether logging engine health event is necessary.
        ///     Whether to log engine event is controled by session variable "LogEngineHealthEvent"
        ///     The default value for this is true (?).
        /// Reading a session variable from execution context for
        /// every single logging call may be expensive. We may need to use a different
        /// approach for this:
        ///     a. ExecutionContext will cache the value for variable "LogEngineHealthEvent"
        ///     b. If this variable is changed, a notification function will change the cached
        ///        value in engine correspondently.
        /// This applies to other logging preference variable also.
        /// </summary>
        /// <param name="logProvider"></param>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        private static bool NeedToLogEngineHealthEvent(LogProvider logProvider, ExecutionContext executionContext)
        {
            if (!logProvider.UseLoggingVariables())
            {
                return true;
            }

            return LanguagePrimitives.IsTrue(executionContext.GetVariableValue(SpecialVariables.LogEngineHealthEventVarPath, true));
        }

        /// <summary>
        /// NeedToLogEngineLifecycleEvent: check whether logging engine lifecycle event is necessary.
        ///     Whether to log engine lifecycle event is controled by session variable "LogEngineLifecycleEvent"
        ///     The default value for this is false (?).
        /// </summary>
        /// <param name="logProvider"></param>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        private static bool NeedToLogEngineLifecycleEvent(LogProvider logProvider, ExecutionContext executionContext)
        {
            if (!logProvider.UseLoggingVariables())
            {
                return true;
            }

            return LanguagePrimitives.IsTrue(executionContext.GetVariableValue(SpecialVariables.LogEngineLifecycleEventVarPath, true));
        }

        /// <summary>
        /// NeedToLogCommandHealthEvent: check whether logging command health event is necessary.
        ///     Whether to log command health event is controled by session variable "LogCommandHealthEvent"
        ///     The default value for this is false (?).
        /// </summary>
        /// <param name="logProvider"></param>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        private static bool NeedToLogCommandHealthEvent(LogProvider logProvider, ExecutionContext executionContext)
        {
            if (!logProvider.UseLoggingVariables())
            {
                return true;
            }

            return LanguagePrimitives.IsTrue(executionContext.GetVariableValue(SpecialVariables.LogCommandHealthEventVarPath, false));
        }

        /// <summary>
        /// NeedToLogCommandLifecycleEvent: check whether logging command event is necessary.
        ///     Whether to log command lifecycle event is controled by session variable "LogCommandLifecycleEvent"
        ///     The default value for this is false (?).
        /// </summary>
        /// <param name="logProvider"></param>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        private static bool NeedToLogCommandLifecycleEvent(LogProvider logProvider, ExecutionContext executionContext)
        {
            if (!logProvider.UseLoggingVariables())
            {
                return true;
            }

            return LanguagePrimitives.IsTrue(executionContext.GetVariableValue(SpecialVariables.LogCommandLifecycleEventVarPath, false));
        }

        /// <summary>
        /// NeedToLogPipelineExecutionDetailEvent: check whether logging pipeline execution detail event is necessary.
        ///
        /// Whether to log command lifecycle event is controled by PSSnapin set up.
        ///
        /// Should we use session variable "LogPipelineExecutionEvent" to control this also?
        ///
        /// Currently we return true always since pipeline processor already check for whether to log
        /// logic from PSSnapin already. This may need to be changed.
        /// </summary>
        /// <param name="logProvider"></param>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        private static bool NeedToLogPipelineExecutionDetailEvent(LogProvider logProvider, ExecutionContext executionContext)
        {
            if (!logProvider.UseLoggingVariables())
            {
                return true;
            }

            return true;
            // return LanguagePrimitives.IsTrue(executionContext.GetVariable("LogPipelineExecutionDetailEvent", false));
        }

        /// <summary>
        /// NeedToLogProviderHealthEvent: check whether logging Provider health event is necessary.
        ///     Whether to log Provider health event is controled by session variable "LogProviderHealthEvent"
        ///     The default value for this is true.
        /// </summary>
        /// <param name="logProvider"></param>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        private static bool NeedToLogProviderHealthEvent(LogProvider logProvider, ExecutionContext executionContext)
        {
            if (!logProvider.UseLoggingVariables())
            {
                return true;
            }

            return LanguagePrimitives.IsTrue(executionContext.GetVariableValue(SpecialVariables.LogProviderHealthEventVarPath, true));
        }

        /// <summary>
        /// NeedToLogProviderLifecycleEvent: check whether logging Provider lifecycle event is necessary.
        ///     Whether to log Provider lifecycle event is controled by session variable "LogProviderLifecycleEvent"
        ///     The default value for this is true.
        /// </summary>
        /// <param name="logProvider"></param>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        private static bool NeedToLogProviderLifecycleEvent(LogProvider logProvider, ExecutionContext executionContext)
        {
            if (!logProvider.UseLoggingVariables())
            {
                return true;
            }

            return LanguagePrimitives.IsTrue(executionContext.GetVariableValue(SpecialVariables.LogProviderLifecycleEventVarPath, true));
        }

        /// <summary>
        /// NeedToLogSettingsEvent: check whether logging settings event is necessary.
        ///     Whether to log settings event is controled by session variable "LogSettingsEvent"
        ///     The default value for this is false (?).
        /// </summary>
        /// <param name="logProvider"></param>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        private static bool NeedToLogSettingsEvent(LogProvider logProvider, ExecutionContext executionContext)
        {
            if (!logProvider.UseLoggingVariables())
            {
                return true;
            }

            return LanguagePrimitives.IsTrue(executionContext.GetVariableValue(SpecialVariables.LogSettingsEventVarPath, true));
        }

        #endregion

        #region Sequence Id Generator

        private static int s_nextSequenceNumber = 0;

        /// <summary>
        /// Generate next sequence id to be attached to current event.
        /// </summary>
        /// <value></value>
        private static string NextSequenceNumber
        {
            get
            {
                return Convert.ToString(Interlocked.Increment(ref s_nextSequenceNumber), CultureInfo.CurrentCulture);
            }
        }

        #endregion

        #region EventId Constants

        // General health issues.
        internal const int EVENT_ID_GENERAL_HEALTH_ISSUE = 100;

        // Dependency. resource not available
        internal const int EVENT_ID_RESOURCE_NOT_AVAILABLE = 101;
        // Connectivity. network connection failure
        internal const int EVENT_ID_NETWORK_CONNECTIVITY_ISSUE = 102;
        // Settings. fail to set some configuration settings
        internal const int EVENT_ID_CONFIGURATION_FAILURE = 103;
        // Performance. system is experiencing some performance issues
        internal const int EVENT_ID_PERFORMANCE_ISSUE = 104;
        // Security: system is experiencing some security issues
        internal const int EVENT_ID_SECURITY_ISSUE = 105;
        // Workload. system is overloaded.
        internal const int EVENT_ID_SYSTEM_OVERLOADED = 106;

        // Beta 1 only -- Unexpected Exception
        internal const int EVENT_ID_UNEXPECTED_EXCEPTION = 195;

        #endregion EventId Constants
    }

    /// <summary>
    /// Log context cache.
    /// </summary>
    internal sealed class LogContextCache
    {
        internal string User { get; set; } = null;
    }

    #region Command State and Provider State

    /// <summary>
    /// Severity of the event.
    /// </summary>
    internal enum Severity
    {
        /// <summary>
        /// Undefined severity.
        /// </summary>
        None,
        /// <summary>
        /// Critical event causing engine not to work.
        /// </summary>
        Critical,

        /// <summary>
        /// Error causing engine partially work.
        /// </summary>
        Error,

        /// <summary>
        /// Problem that may not cause an immediate problem.
        /// </summary>
        Warning,

        /// <summary>
        /// Informational.
        /// </summary>
        Informational
    }

    /// <summary>
    /// Enum for command states.
    /// </summary>
    internal enum CommandState
    {
        /// <summary>
        /// </summary>
        Started = 0,

        /// <summary>
        /// </summary>
        Stopped = 1,

        /// <summary>
        /// </summary>
        Terminated = 2
    }

    /// <summary>
    /// Enum for provider states.
    /// </summary>
    internal enum ProviderState
    {
        /// <summary>
        /// </summary>
        Started = 0,

        /// <summary>
        /// </summary>
        Stopped = 1,
    }

    #endregion
}
