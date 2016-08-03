#if !UNIX
//
//    Copyright (C) Microsoft.  All rights reserved.
//
using System.Diagnostics.Eventing;
using System.Management.Automation.Internal;
using System.Text;
using System.Collections.Generic;

namespace System.Management.Automation.Tracing
{
    /// <summary>
    /// ETW log provider implementation
    /// </summary>
    internal class PSEtwLogProvider : LogProvider
    {
        private static EventProvider etwProvider;
        private static readonly string PowerShellEventProviderGuid = "A0C1853B-5C40-4b15-8766-3CF1C58F985A";
        private static EventDescriptor _xferEventDescriptor = new EventDescriptor(0x1f05, 0x1, 0x11, 0x5, 0x14, 0x0, (long)0x4000000000000000);

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
        /// Class constructor
        /// </summary>
        static PSEtwLogProvider()
        {
            etwProvider = new EventProvider(new Guid(PowerShellEventProviderGuid));
        }

        /// <summary>
        /// Determines whether any session is requesting the specified event from the provider.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="keywords"></param>
        /// <returns></returns>
        /// <remarks>
        /// Typically, a provider does not call this method to determine whether a session requested the specified event; 
        /// the provider simply writes the event, and ETW determines whether the event is logged to a session. A provider 
        /// may want to call this function if the provider needs to perform extra work to generate the event. In this case,
        ///  calling this function first to determine if a session requested the event or not, may save resources and time.
        /// </remarks>
        internal bool IsEnabled(PSLevel level, PSKeyword keywords)
        {
            return etwProvider.IsEnabled((byte)level, (long)keywords);
        }

        /// <summary>
        /// Provider interface function for logging health event
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="eventId"></param>
        /// <param name="exception"></param>
        /// <param name="additionalInfo"></param>
        /// 
        internal override void LogEngineHealthEvent(LogContext logContext, int eventId, Exception exception, Dictionary<String, String> additionalInfo)
        {
            StringBuilder payload = new StringBuilder();

            AppendException(payload, exception);
            payload.AppendLine();
            AppendAdditionalInfo(payload, additionalInfo);

            WriteEvent(PSEventId.Engine_Health, PSChannel.Operational, PSOpcode.Exception, PSTask.ExecutePipeline, logContext, payload.ToString());
        }

        /// <summary>
        /// Provider interface function for logging engine lifecycle event
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="newState"></param>
        /// <param name="previousState"></param>
        /// 
        internal override void LogEngineLifecycleEvent(LogContext logContext, EngineState newState, EngineState previousState)
        {
            if (IsEnabled(PSLevel.Informational, PSKeyword.Cmdlets | PSKeyword.UseAlwaysAnalytic))
            {
                StringBuilder payload = new StringBuilder();

                payload.AppendLine(StringUtil.Format(EtwLoggingStrings.EngineStateChange, previousState.ToString(), newState.ToString()));

                PSTask task = PSTask.EngineStart;

                if (newState == EngineState.Stopped ||
                    newState == EngineState.OutOfService ||
                    newState == EngineState.None ||
                    newState == EngineState.Degraded)
                {
                    task = PSTask.EngineStop;
                }

                WriteEvent(PSEventId.Engine_Lifecycle, PSChannel.Analytic, PSOpcode.Method, task, logContext, payload.ToString());
            }
        }

        /// <summary>
        /// Provider interface function for logging command health event
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="exception"></param>
        internal override void LogCommandHealthEvent(LogContext logContext, Exception exception)
        {
            StringBuilder payload = new StringBuilder();

            AppendException(payload, exception);

            WriteEvent(PSEventId.Command_Health, PSChannel.Operational, PSOpcode.Exception, PSTask.ExecutePipeline, logContext, payload.ToString());
        }

        /// <summary>
        /// Provider interface function for logging command lifecycle event
        /// </summary>
        /// <param name="getLogContext"></param>
        /// <param name="newState"></param>
        /// 
        internal override void LogCommandLifecycleEvent(Func<LogContext> getLogContext, CommandState newState)
        {
            if (IsEnabled(PSLevel.Informational, PSKeyword.Cmdlets | PSKeyword.UseAlwaysAnalytic))
            {
                LogContext logContext = getLogContext();
                StringBuilder payload = new StringBuilder();

                if (logContext.CommandType != null)
                {
                    if (logContext.CommandType.Equals(StringLiterals.Script, StringComparison.OrdinalIgnoreCase))
                    {
                        payload.AppendLine(StringUtil.Format(EtwLoggingStrings.ScriptStateChange, newState.ToString()));
                    }
                    else
                    {
                        payload.AppendLine(StringUtil.Format(EtwLoggingStrings.CommandStateChange, logContext.CommandName, newState.ToString()));
                    }
                }

                PSTask task = PSTask.CommandStart;

                if (newState == CommandState.Stopped ||
                    newState == CommandState.Terminated)
                {
                    task = PSTask.CommandStop;
                }

                WriteEvent(PSEventId.Command_Lifecycle, PSChannel.Analytic, PSOpcode.Method, task, logContext, payload.ToString());
            }
        }

        /// <summary>
        /// Provider interface function for logging pipeline execution detail.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="pipelineExecutionDetail"></param>
        internal override void LogPipelineExecutionDetailEvent(LogContext logContext, List<String> pipelineExecutionDetail)
        {
            StringBuilder payload = new StringBuilder();

            if (pipelineExecutionDetail != null)
            {
                foreach (String detail in pipelineExecutionDetail)
                {
                    payload.AppendLine(detail);
                }
            }

            WriteEvent(PSEventId.Pipeline_Detail, PSChannel.Operational, PSOpcode.Method, PSTask.ExecutePipeline, logContext, payload.ToString());
        }

        /// <summary>
        /// Provider interface function for logging provider health event
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="providerName"></param>
        /// <param name="exception"></param>
        internal override void LogProviderHealthEvent(LogContext logContext, string providerName, Exception exception)
        {
            StringBuilder payload = new StringBuilder();

            AppendException(payload, exception);
            payload.AppendLine();

            Dictionary<String, String> additionalInfo = new Dictionary<string, string>();

            additionalInfo.Add(EtwLoggingStrings.ProviderNameString, providerName);

            AppendAdditionalInfo(payload, additionalInfo);

            WriteEvent(PSEventId.Provider_Health, PSChannel.Operational, PSOpcode.Exception, PSTask.ExecutePipeline, logContext, payload.ToString());
        }

        /// <summary>
        /// Provider interface function for logging provider lifecycle event
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="providerName"></param>
        /// <param name="newState"></param>
        /// 
        internal override void LogProviderLifecycleEvent(LogContext logContext, string providerName, ProviderState newState)
        {
            if (IsEnabled(PSLevel.Informational, PSKeyword.Cmdlets | PSKeyword.UseAlwaysAnalytic))
            {
                StringBuilder payload = new StringBuilder();

                payload.AppendLine(StringUtil.Format(EtwLoggingStrings.ProviderStateChange, providerName, newState.ToString()));

                PSTask task = PSTask.ProviderStart;

                if (newState == ProviderState.Stopped)
                {
                    task = PSTask.ProviderStop;
                }

                WriteEvent(PSEventId.Provider_Lifecycle, PSChannel.Analytic, PSOpcode.Method, task, logContext, payload.ToString());
            }
        }

        /// <summary>
        /// Provider interface function for logging settings event
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="variableName"></param>
        /// <param name="value"></param>
        /// <param name="previousValue"></param>
        /// 
        internal override void LogSettingsEvent(LogContext logContext, string variableName, string value, string previousValue)
        {
            if (IsEnabled(PSLevel.Informational, PSKeyword.Cmdlets | PSKeyword.UseAlwaysAnalytic))
            {
                StringBuilder payload = new StringBuilder();

                if (previousValue == null)
                {
                    payload.AppendLine(StringUtil.Format(EtwLoggingStrings.SettingChangeNoPrevious, variableName, value));
                }
                else
                {
                    payload.AppendLine(StringUtil.Format(EtwLoggingStrings.SettingChange, variableName, previousValue, value));
                }

                WriteEvent(PSEventId.Settings, PSChannel.Analytic, PSOpcode.Method, PSTask.ExecutePipeline, logContext, payload.ToString());
            }
        }

        /// <summary>
        /// The ETW provider does not use logging variables
        /// </summary>
        /// <returns></returns>
        internal override bool UseLoggingVariables()
        {
            return false;
        }

        /// <summary>
        /// Gets PSLogUserData from execution context
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private static string GetPSLogUserData(ExecutionContext context)
        {
            if (context == null)
            {
                return String.Empty;
            }

            object logData = context.GetVariableValue(SpecialVariables.PSLogUserDataPath);

            if (logData == null)
            {
                return String.Empty;
            }

            return logData.ToString();
        }

        /// <summary>
        /// Appends exception information
        /// </summary>
        /// <param name="sb">string builder</param>
        /// <param name="except">exception</param>
        internal static void AppendException(StringBuilder sb, Exception except)
        {
            sb.AppendLine(StringUtil.Format(EtwLoggingStrings.ErrorRecordMessage, except.Message));

            IContainsErrorRecord ier = except as IContainsErrorRecord;

            if (ier != null)
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
        /// Appends additional information
        /// </summary>
        /// <param name="sb">string builder</param>
        /// <param name="additionalInfo">additional information</param>
        private static void AppendAdditionalInfo(StringBuilder sb, Dictionary<String, String> additionalInfo)
        {
            if (additionalInfo != null)
            {
                foreach (KeyValuePair<String, String> value in additionalInfo)
                {
                    sb.AppendLine(StringUtil.Format("{0} = {1}", value.Key, value.Value));
                }
            }
        }

        /// <summary>
        /// Gets PSLevel from severity
        /// </summary>
        /// <param name="severity">error severity</param>
        /// <returns>PS log level</returns>
        private static PSLevel GetPSLevelFromSeverity(string severity)
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

        /// <summary>
        /// Converts log context to string
        /// </summary>
        /// <param name="context">log context</param>
        /// <returns>string representation</returns>
        private static string LogContextToString(LogContext context)
        {
            StringBuilder sb = new StringBuilder();

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

        /// <summary>
        /// Writes a single event
        /// </summary>
        /// <param name="id">event id</param>
        /// <param name="channel"></param>
        /// <param name="opcode"></param>
        /// <param name="task"></param>
        /// <param name="logContext">log context</param>
        /// <param name="payLoad"></param>
        internal void WriteEvent(PSEventId id, PSChannel channel, PSOpcode opcode, PSTask task, LogContext logContext, string payLoad)
        {
            WriteEvent(id, channel, opcode, GetPSLevelFromSeverity(logContext.Severity), task, (PSKeyword)0x0,
                LogContextToString(logContext), GetPSLogUserData(logContext.ExecutionContext), payLoad);
        }

        /// <summary>
        /// Writes an event
        /// </summary>
        /// <param name="id"></param>
        /// <param name="channel"></param>
        /// <param name="opcode"></param>
        /// <param name="level"></param>
        /// <param name="task"></param>
        /// <param name="keyword"></param>
        /// <param name="args"></param>
        internal void WriteEvent(PSEventId id, PSChannel channel, PSOpcode opcode, PSLevel level, PSTask task, PSKeyword keyword, params object[] args)
        {
            long longKeyword = 0x00;

            if (keyword == PSKeyword.UseAlwaysAnalytic ||
                keyword == PSKeyword.UseAlwaysOperational)
            {
                longKeyword = 0x00;
            }
            else
            {
                longKeyword = (long)keyword;
            }

            EventDescriptor desc = new EventDescriptor((int)id, (byte)PSEventVersion.One, (byte)channel,
                (byte)level, (byte)opcode, (int)task, longKeyword);

            etwProvider.WriteEvent(ref desc, args);
        }

        /// <summary>
        /// Writes an activity transfer event
        /// </summary>
        internal void WriteTransferEvent(Guid parentActivityId)
        {
            etwProvider.WriteTransferEvent(ref _xferEventDescriptor, parentActivityId, EtwActivity.GetActivityId(), parentActivityId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newActivityId"></param>
        internal void SetActivityIdForCurrentThread(Guid newActivityId)
        {
            Guid result = newActivityId;
            EventProvider.SetActivityId(ref result);
        }
    }
}

#endif
