// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation.Internal;
using System.Text;

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
    ///     e. SettingsEvent.
    /// </summary>
    internal abstract class LogProvider
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        internal LogProvider()
        {
        }

        #region Provider api

        /// <summary>
        /// Provider interface function for logging health event.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="eventId"></param>
        /// <param name="exception"></param>
        /// <param name="additionalInfo"></param>
        internal abstract void LogEngineHealthEvent(LogContext logContext, int eventId, Exception exception, Dictionary<string, string> additionalInfo);

        /// <summary>
        /// Provider interface function for logging engine lifecycle event.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="newState"></param>
        /// <param name="previousState"></param>
        internal abstract void LogEngineLifecycleEvent(LogContext logContext, EngineState newState, EngineState previousState);

        /// <summary>
        /// Provider interface function for logging command health event.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="exception"></param>
        internal abstract void LogCommandHealthEvent(LogContext logContext, Exception exception);

        /// <summary>
        /// Provider interface function for logging command lifecycle event.
        /// </summary>
        /// <param name="getLogContext"></param>
        /// <param name="newState"></param>
        internal abstract void LogCommandLifecycleEvent(Func<LogContext> getLogContext, CommandState newState);

        /// <summary>
        /// Provider interface function for logging pipeline execution detail.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="pipelineExecutionDetail"></param>
        internal abstract void LogPipelineExecutionDetailEvent(LogContext logContext, List<string> pipelineExecutionDetail);

        /// <summary>
        /// Provider interface function for logging provider health event.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="providerName"></param>
        /// <param name="exception"></param>
        internal abstract void LogProviderHealthEvent(LogContext logContext, string providerName, Exception exception);

        /// <summary>
        /// Provider interface function for logging provider lifecycle event.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="providerName"></param>
        /// <param name="newState"></param>
        internal abstract void LogProviderLifecycleEvent(LogContext logContext, string providerName, ProviderState newState);

        /// <summary>
        /// Provider interface function for logging settings event.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="variableName"></param>
        /// <param name="value"></param>
        /// <param name="previousValue"></param>
        internal abstract void LogSettingsEvent(LogContext logContext, string variableName, string value, string previousValue);

        /// <summary>
        /// Provider interface function for logging AmsiUtil State event.
        /// </summary>
        /// <param name="state">This the action performed in AmsiUtil class, like init, scan, etc.</param>
        /// <param name="context">The amsiContext handled - Session pair.</param>
        internal abstract void LogAmsiUtilStateEvent(string state, string context);

        /// <summary>
        /// Provider interface function for logging WDAC query event.
        /// </summary>
        /// <param name="queryName">Name of the WDAC query.</param>
        /// <param name="fileName">Name of script file for policy query. Can be null value.</param>
        /// <param name="querySuccess">Query call succeed code.</param>
        /// <param name="queryResult">Result code of WDAC query.</param>
        internal abstract void LogWDACQueryEvent(
            string queryName,
            string fileName,
            int querySuccess,
            int queryResult);

        /// <summary>
        /// True if the log provider needs to use logging variables.
        /// </summary>
        /// <returns></returns>
        internal virtual bool UseLoggingVariables()
        {
            return true;
        }

        #endregion

        #region Shared utilities

        private static class Strings
        {
            // The strings are stored in a different class to defer loading the resources until as late
            // as possible, e.g. if logging is never on, these strings won't be loaded.
            internal static readonly string LogContextSeverity = EtwLoggingStrings.LogContextSeverity;
            internal static readonly string LogContextHostName = EtwLoggingStrings.LogContextHostName;
            internal static readonly string LogContextHostVersion = EtwLoggingStrings.LogContextHostVersion;
            internal static readonly string LogContextHostId = EtwLoggingStrings.LogContextHostId;
            internal static readonly string LogContextHostApplication = EtwLoggingStrings.LogContextHostApplication;
            internal static readonly string LogContextEngineVersion = EtwLoggingStrings.LogContextEngineVersion;
            internal static readonly string LogContextRunspaceId = EtwLoggingStrings.LogContextRunspaceId;
            internal static readonly string LogContextPipelineId = EtwLoggingStrings.LogContextPipelineId;
            internal static readonly string LogContextCommandName = EtwLoggingStrings.LogContextCommandName;
            internal static readonly string LogContextCommandType = EtwLoggingStrings.LogContextCommandType;
            internal static readonly string LogContextScriptName = EtwLoggingStrings.LogContextScriptName;
            internal static readonly string LogContextCommandPath = EtwLoggingStrings.LogContextCommandPath;
            internal static readonly string LogContextSequenceNumber = EtwLoggingStrings.LogContextSequenceNumber;
            internal static readonly string LogContextUser = EtwLoggingStrings.LogContextUser;
            internal static readonly string LogContextConnectedUser = EtwLoggingStrings.LogContextConnectedUser;
            internal static readonly string LogContextTime = EtwLoggingStrings.LogContextTime;
            internal static readonly string LogContextShellId = EtwLoggingStrings.LogContextShellId;
        }

        /// <summary>
        /// Gets PSLogUserData from execution context.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected static string GetPSLogUserData(ExecutionContext context)
        {
            if (context == null)
            {
                return string.Empty;
            }

            object logData = context.GetVariableValue(SpecialVariables.PSLogUserDataPath);

            if (logData == null)
            {
                return string.Empty;
            }

            return logData.ToString();
        }

        /// <summary>
        /// Appends exception information.
        /// </summary>
        /// <param name="sb">String builder.</param>
        /// <param name="except">Exception.</param>
        protected static void AppendException(StringBuilder sb, Exception except)
        {
            sb.AppendLine(StringUtil.Format(EtwLoggingStrings.ErrorRecordMessage, except.Message));

            if (except is IContainsErrorRecord ier)
            {
                ErrorRecord er = ier.ErrorRecord;

                if (er != null)
                {
                    sb.AppendLine(StringUtil.Format(EtwLoggingStrings.ErrorRecordId, er.FullyQualifiedErrorId));

                    ErrorDetails details = er.ErrorDetails;

                    if (details != null)
                    {
                        sb.AppendLine(StringUtil.Format(EtwLoggingStrings.ErrorRecordRecommendedAction, details.RecommendedAction));
                    }
                }
            }
        }

        /// <summary>
        /// Appends additional information.
        /// </summary>
        /// <param name="sb">String builder.</param>
        /// <param name="additionalInfo">Additional information.</param>
        protected static void AppendAdditionalInfo(StringBuilder sb, Dictionary<string, string> additionalInfo)
        {
            if (additionalInfo != null)
            {
                foreach (KeyValuePair<string, string> value in additionalInfo)
                {
                    sb.AppendLine(StringUtil.Format("{0} = {1}", value.Key, value.Value));
                }
            }
        }

        /// <summary>
        /// Gets PSLevel from severity.
        /// </summary>
        /// <param name="severity">Error severity.</param>
        /// <returns>PS log level.</returns>
        protected static PSLevel GetPSLevelFromSeverity(string severity)
        {
            switch (severity)
            {
                case "Critical":
                case "Error":
                    return PSLevel.Error;
                case "Warning":
                    return PSLevel.Warning;
                default:
                    return PSLevel.Informational;
            }
        }

        // Estimate an approximate size to use for the StringBuilder in LogContextToString
        // Estimated length of all Strings.* values
        // Rough estimate of values
        // max path for Command path
        private const int LogContextInitialSize = 30 * 16 + 13 * 20 + 255;

        /// <summary>
        /// Converts log context to string.
        /// </summary>
        /// <param name="context">Log context.</param>
        /// <returns>String representation.</returns>
        protected static string LogContextToString(LogContext context)
        {
            StringBuilder sb = new StringBuilder(LogContextInitialSize);

            sb.Append(Strings.LogContextSeverity);
            sb.AppendLine(context.Severity);
            sb.Append(Strings.LogContextHostName);
            sb.AppendLine(context.HostName);
            sb.Append(Strings.LogContextHostVersion);
            sb.AppendLine(context.HostVersion);
            sb.Append(Strings.LogContextHostId);
            sb.AppendLine(context.HostId);
            sb.Append(Strings.LogContextHostApplication);
            sb.AppendLine(context.HostApplication);
            sb.Append(Strings.LogContextEngineVersion);
            sb.AppendLine(context.EngineVersion);
            sb.Append(Strings.LogContextRunspaceId);
            sb.AppendLine(context.RunspaceId);
            sb.Append(Strings.LogContextPipelineId);
            sb.AppendLine(context.PipelineId);
            sb.Append(Strings.LogContextCommandName);
            sb.AppendLine(context.CommandName);
            sb.Append(Strings.LogContextCommandType);
            sb.AppendLine(context.CommandType);
            sb.Append(Strings.LogContextScriptName);
            sb.AppendLine(context.ScriptName);
            sb.Append(Strings.LogContextCommandPath);
            sb.AppendLine(context.CommandPath);
            sb.Append(Strings.LogContextSequenceNumber);
            sb.AppendLine(context.SequenceNumber);
            sb.Append(Strings.LogContextUser);
            sb.AppendLine(context.User);
            sb.Append(Strings.LogContextConnectedUser);
            sb.AppendLine(context.ConnectedUser);
            sb.Append(Strings.LogContextShellId);
            sb.AppendLine(context.ShellId);

            return sb.ToString();
        }

        #endregion
    }

    /// <summary>
    /// </summary>
    internal class DummyLogProvider : LogProvider
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        internal DummyLogProvider()
        {
        }

        #region Provider api

        /// <summary>
        /// DummyLogProvider does nothing to Logging EngineHealthEvent.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="eventId"></param>
        /// <param name="exception"></param>
        /// <param name="additionalInfo"></param>
        internal override void LogEngineHealthEvent(LogContext logContext, int eventId, Exception exception, Dictionary<string, string> additionalInfo)
        {
        }

        /// <summary>
        /// DummyLogProvider does nothing to Logging EngineLifecycleEvent.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="newState"></param>
        /// <param name="previousState"></param>
        internal override void LogEngineLifecycleEvent(LogContext logContext, EngineState newState, EngineState previousState)
        {
        }

        /// <summary>
        /// Provider interface function for logging command health event.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="exception"></param>
        internal override void LogCommandHealthEvent(LogContext logContext, Exception exception)
        {
        }

        /// <summary>
        /// DummyLogProvider does nothing to Logging CommandLifecycleEvent.
        /// </summary>
        /// <param name="getLogContext"></param>
        /// <param name="newState"></param>
        internal override void LogCommandLifecycleEvent(Func<LogContext> getLogContext, CommandState newState)
        {
        }

        /// <summary>
        /// DummyLogProvider does nothing to Logging PipelineExecutionDetailEvent.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="pipelineExecutionDetail"></param>
        internal override void LogPipelineExecutionDetailEvent(LogContext logContext, List<string> pipelineExecutionDetail)
        {
        }

        /// <summary>
        /// Provider interface function for logging provider health event.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="providerName"></param>
        /// <param name="exception"></param>
        internal override void LogProviderHealthEvent(LogContext logContext, string providerName, Exception exception)
        {
        }

        /// <summary>
        /// DummyLogProvider does nothing to Logging ProviderLifecycleEvent.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="providerName"></param>
        /// <param name="newState"></param>
        internal override void LogProviderLifecycleEvent(LogContext logContext, string providerName, ProviderState newState)
        {
        }

        /// <summary>
        /// DummyLogProvider does nothing to Logging SettingsEvent.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="variableName"></param>
        /// <param name="value"></param>
        /// <param name="previousValue"></param>
        internal override void LogSettingsEvent(LogContext logContext, string variableName, string value, string previousValue)
        {
        }

        /// <summary>
        /// Provider interface function for logging provider health event.
        /// </summary>
        /// <param name="state">This the action performed in AmsiUtil class, like init, scan, etc.</param>
        /// <param name="context">The amsiContext handled - Session pair.</param>
        internal override void LogAmsiUtilStateEvent(string state, string context)
        {
        }

        /// <summary>
        /// Provider interface function for logging WDAC query event.
        /// </summary>
        /// <param name="queryName">Name of the WDAC query.</param>
        /// <param name="fileName">Name of script file for policy query. Can be null value.</param>
        /// <param name="querySuccess">Query call succeed code.</param>
        /// <param name="queryResult">Result code of WDAC query.</param>
        internal override void LogWDACQueryEvent(
            string queryName,
            string fileName,
            int querySuccess,
            int queryResult)
        {
        }

        #endregion
    }
}
