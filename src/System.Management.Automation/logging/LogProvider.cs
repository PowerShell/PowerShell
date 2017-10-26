/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System.Collections.Generic;

namespace System.Management.Automation
{
    /// <summary>
    /// Monad Logging in general is a two layer architecture. At the upper layer are the
    /// Msh Log Engine and Logging Api. At the lower layer is the Provider Interface
    /// and Log Providers. This architecture is adopted to achieve independency between
    /// Monad logging and logging details of different logging technology.
    ///
    /// This file implements the lower layer of the Monad Logging architecture.
    /// Upper layer of Msh Log architecture is implemented in MshLog.cs file.
    ///
    /// This class defines the provider interface to be implemented by each providers.
    ///
    /// Provider Interface.
    ///
    /// Corresponding to 5 categories of logging api interface, provider interface provides
    /// functions for logging
    ///     a. EngineHealthEvent
    ///     b. EngineLifecycleEvent
    ///     c. CommandLifecycleEvent
    ///     d. ProviderLifecycleEvent
    ///     e. SettingsEvent
    ///
    /// </summary>
    internal abstract class LogProvider
    {
        /// <summary>
        /// constructor
        /// </summary>
        ///
        internal LogProvider()
        {
        }

        #region Provider api

        /// <summary>
        /// Provider interface function for logging health event
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="eventId"></param>
        /// <param name="exception"></param>
        /// <param name="additionalInfo"></param>
        ///
        internal abstract void LogEngineHealthEvent(LogContext logContext, int eventId, Exception exception, Dictionary<String, String> additionalInfo);

        /// <summary>
        /// Provider interface function for logging engine lifecycle event
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="newState"></param>
        /// <param name="previousState"></param>
        ///
        internal abstract void LogEngineLifecycleEvent(LogContext logContext, EngineState newState, EngineState previousState);

        /// <summary>
        /// Provider interface function for logging command health event
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="exception"></param>
        internal abstract void LogCommandHealthEvent(LogContext logContext, Exception exception);

        /// <summary>
        /// Provider interface function for logging command lifecycle event
        /// </summary>
        /// <param name="getLogContext"></param>
        /// <param name="newState"></param>
        ///
        internal abstract void LogCommandLifecycleEvent(Func<LogContext> getLogContext, CommandState newState);

        /// <summary>
        /// Provider interface function for logging pipeline execution detail.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="pipelineExecutionDetail"></param>
        internal abstract void LogPipelineExecutionDetailEvent(LogContext logContext, List<String> pipelineExecutionDetail);

        /// <summary>
        /// Provider interface function for logging provider health event
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="providerName"></param>
        /// <param name="exception"></param>
        ///
        internal abstract void LogProviderHealthEvent(LogContext logContext, string providerName, Exception exception);

        /// <summary>
        /// Provider interface function for logging provider lifecycle event
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="providerName"></param>
        /// <param name="newState"></param>
        ///
        internal abstract void LogProviderLifecycleEvent(LogContext logContext, string providerName, ProviderState newState);

        /// <summary>
        /// Provider interface function for logging settings event
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="variableName"></param>
        /// <param name="value"></param>
        /// <param name="previousValue"></param>
        ///
        internal abstract void LogSettingsEvent(LogContext logContext, string variableName, string value, string previousValue);

        /// <summary>
        /// True if the log provider needs to use logging variables
        /// </summary>
        /// <returns></returns>
        internal virtual bool UseLoggingVariables()
        {
            return true;
        }

        #endregion
    }

    /// <summary>
    /// </summary>
    internal class DummyLogProvider : LogProvider
    {
        /// <summary>
        /// constructor
        /// </summary>
        ///
        internal DummyLogProvider()
        {
        }

        #region Provider api

        /// <summary>
        /// DummyLogProvider does nothing to Logging EngineHealthEvent
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="eventId"></param>
        /// <param name="exception"></param>
        /// <param name="additionalInfo"></param>
        ///
        internal override void LogEngineHealthEvent(LogContext logContext, int eventId, Exception exception, Dictionary<String, String> additionalInfo)
        {
        }

        /// <summary>
        /// DummyLogProvider does nothing to Logging EngineLifecycleEvent
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="newState"></param>
        /// <param name="previousState"></param>
        ///
        internal override void LogEngineLifecycleEvent(LogContext logContext, EngineState newState, EngineState previousState)
        {
        }

        /// <summary>
        /// Provider interface function for logging command health event
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="exception"></param>
        ///
        internal override void LogCommandHealthEvent(LogContext logContext, Exception exception)
        {
        }

        /// <summary>
        /// DummyLogProvider does nothing to Logging CommandLifecycleEvent
        /// </summary>
        /// <param name="getLogContext"></param>
        /// <param name="newState"></param>
        ///
        internal override void LogCommandLifecycleEvent(Func<LogContext> getLogContext, CommandState newState)
        {
        }

        /// <summary>
        /// DummyLogProvider does nothing to Logging PipelineExecutionDetailEvent.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="pipelineExecutionDetail"></param>
        internal override void LogPipelineExecutionDetailEvent(LogContext logContext, List<String> pipelineExecutionDetail)
        {
        }

        /// <summary>
        /// Provider interface function for logging provider health event
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="providerName"></param>
        /// <param name="exception"></param>
        ///
        internal override void LogProviderHealthEvent(LogContext logContext, string providerName, Exception exception)
        {
        }

        /// <summary>
        /// DummyLogProvider does nothing to Logging ProviderLifecycleEvent
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="providerName"></param>
        /// <param name="newState"></param>
        ///
        internal override void LogProviderLifecycleEvent(LogContext logContext, string providerName, ProviderState newState)
        {
        }

        /// <summary>
        /// DummyLogProvider does nothing to Logging SettingsEvent
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="variableName"></param>
        /// <param name="value"></param>
        /// <param name="previousValue"></param>
        ///
        internal override void LogSettingsEvent(LogContext logContext, string variableName, string value, string previousValue)
        {
        }

        #endregion
    }
}
