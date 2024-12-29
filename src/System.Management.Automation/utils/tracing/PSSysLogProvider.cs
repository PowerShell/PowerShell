// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if UNIX

using System.Diagnostics.Eventing;
using System.Management.Automation.Configuration;
using System.Management.Automation.Internal;
using System.Text;
using System.Collections.Generic;

namespace System.Management.Automation.Tracing
{
    /// <summary>
    /// SysLog LogProvider implementation.
    /// </summary>
    internal class PSSysLogProvider : LogProvider
    {
        private static readonly SysLogProvider s_provider;

        // by default, do not include channel bits
        internal const PSKeyword DefaultKeywords = (PSKeyword)(0x00FFFFFFFFFFFFFF);

        // the default enabled channel(s)
        internal const PSChannel DefaultChannels = PSChannel.Operational;

        /// <summary>
        /// Class constructor.
        /// </summary>
        static PSSysLogProvider()
        {
            s_provider = new SysLogProvider(PowerShellConfig.Instance.GetSysLogIdentity(),
                                            PowerShellConfig.Instance.GetLogLevel(),
                                            PowerShellConfig.Instance.GetLogKeywords(),
                                            PowerShellConfig.Instance.GetLogChannels());
        }

        /// <summary>
        /// Defines a thread local StringBuilder for building event payload strings.
        /// </summary>
        /// <remarks>
        /// NOTE: do not access this field directly, use the PayloadBuilder
        /// property to ensure correct thread initialization; otherwise, a null reference can occur.
        /// </remarks>
        [ThreadStatic]
        private static StringBuilder t_payloadBuilder;

        private static StringBuilder PayloadBuilder
        {
            get
            {
                // NOTE: Thread static fields must be explicitly initialized for each thread.
                t_payloadBuilder ??= new StringBuilder(200);

                return t_payloadBuilder;
            }
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
            return s_provider.IsEnabled(level, keywords);
        }

        /// <summary>
        /// Provider interface function for logging health event.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="eventId"></param>
        /// <param name="exception"></param>
        /// <param name="additionalInfo"></param>
        internal override void LogEngineHealthEvent(LogContext logContext, int eventId, Exception exception, Dictionary<string, string> additionalInfo)
        {
            StringBuilder payload = PayloadBuilder;
            payload.Clear();

            AppendException(payload, exception);
            payload.AppendLine();
            AppendAdditionalInfo(payload, additionalInfo);

            WriteEvent(PSEventId.Engine_Health, PSChannel.Operational, PSOpcode.Exception, PSTask.ExecutePipeline, logContext, payload.ToString());
        }

        /// <summary>
        /// Provider interface function for logging provider health event.
        /// </summary>
        /// <param name="state">This the action performed in AmsiUtil class, like init, scan, etc</param>
        /// <param name="context">The amsiContext handled - Session pair</param>
        internal override void LogAmsiUtilStateEvent(string state, string context)
        {
            WriteEvent(PSEventId.Amsi_Init, PSChannel.Analytic, PSOpcode.Method, PSLevel.Informational, PSTask.Amsi, (PSKeyword)0x0, state, context);
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
            WriteEvent(PSEventId.WDAC_Query, PSChannel.Analytic, PSOpcode.Method, PSLevel.Informational, PSTask.WDAC, (PSKeyword)0x0, queryName, fileName, querySuccess, queryResult);
        }

        /// <summary>
        /// Provider interface function for logging WDAC audit event.
        /// </summary>
        /// <param name="title">Title of WDAC audit event.</param>
        /// <param name="message">WDAC audit event message.</param>
        /// <param name="fqid">FullyQualifiedId of WDAC audit event.</param>
        internal override void LogWDACAuditEvent(
            string title,
            string message,
            string fqid)
        {
            WriteEvent(PSEventId.WDAC_Audit, PSChannel.Operational, PSOpcode.Method, PSLevel.Informational, PSTask.WDAC, (PSKeyword)0x0, title, message, fqid);
        }

        /// <summary>
        /// Provider interface function for logging engine lifecycle event.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="newState"></param>
        /// <param name="previousState"></param>
        internal override void LogEngineLifecycleEvent(LogContext logContext, EngineState newState, EngineState previousState)
        {
            if (IsEnabled(PSLevel.Informational, PSKeyword.Cmdlets | PSKeyword.UseAlwaysAnalytic))
            {
                StringBuilder payload = PayloadBuilder;
                payload.Clear();

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
        /// Provider interface function for logging command health event.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="exception"></param>
        internal override void LogCommandHealthEvent(LogContext logContext, Exception exception)
        {
            StringBuilder payload = PayloadBuilder;
            payload.Clear();

            AppendException(payload, exception);

            WriteEvent(PSEventId.Command_Health, PSChannel.Operational, PSOpcode.Exception, PSTask.ExecutePipeline, logContext, payload.ToString());
        }

        /// <summary>
        /// Provider interface function for logging command lifecycle event.
        /// </summary>
        /// <param name="getLogContext"></param>
        /// <param name="newState"></param>
        internal override void LogCommandLifecycleEvent(Func<LogContext> getLogContext, CommandState newState)
        {
            if (IsEnabled(PSLevel.Informational, PSKeyword.Cmdlets | PSKeyword.UseAlwaysAnalytic))
            {
                LogContext logContext = getLogContext();
                StringBuilder payload = PayloadBuilder;
                payload.Clear();

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
        internal override void LogPipelineExecutionDetailEvent(LogContext logContext, List<string> pipelineExecutionDetail)
        {
            StringBuilder payload = PayloadBuilder;
            payload.Clear();

            if (pipelineExecutionDetail != null)
            {
                foreach (string detail in pipelineExecutionDetail)
                {
                    payload.AppendLine(detail);
                }
            }

            WriteEvent(PSEventId.Pipeline_Detail, PSChannel.Operational, PSOpcode.Method, PSTask.ExecutePipeline, logContext, payload.ToString());
        }

        /// <summary>
        /// Provider interface function for logging provider health event.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="providerName"></param>
        /// <param name="exception"></param>
        internal override void LogProviderHealthEvent(LogContext logContext, string providerName, Exception exception)
        {
            StringBuilder payload = PayloadBuilder;
            payload.Clear();

            AppendException(payload, exception);
            payload.AppendLine();

            Dictionary<string, string> additionalInfo = new Dictionary<string, string>();

            additionalInfo.Add(EtwLoggingStrings.ProviderNameString, providerName);

            AppendAdditionalInfo(payload, additionalInfo);

            WriteEvent(PSEventId.Provider_Health, PSChannel.Operational, PSOpcode.Exception, PSTask.ExecutePipeline, logContext, payload.ToString());
        }

        /// <summary>
        /// Provider interface function for logging provider lifecycle event.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="providerName"></param>
        /// <param name="newState"></param>
        internal override void LogProviderLifecycleEvent(LogContext logContext, string providerName, ProviderState newState)
        {
            if (IsEnabled(PSLevel.Informational, PSKeyword.Cmdlets | PSKeyword.UseAlwaysAnalytic))
            {
                StringBuilder payload = PayloadBuilder;
                payload.Clear();

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
        /// Provider interface function for logging settings event.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="variableName"></param>
        /// <param name="value"></param>
        /// <param name="previousValue"></param>
        internal override void LogSettingsEvent(LogContext logContext, string variableName, string value, string previousValue)
        {
            if (IsEnabled(PSLevel.Informational, PSKeyword.Cmdlets | PSKeyword.UseAlwaysAnalytic))
            {
                StringBuilder payload = PayloadBuilder;
                payload.Clear();

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
        /// The SysLog provider does not use logging variables.
        /// </summary>
        /// <returns></returns>
        internal override bool UseLoggingVariables()
        {
            return false;
        }

        /// <summary>
        /// Writes a single event.
        /// </summary>
        /// <param name="id">Event id.</param>
        /// <param name="channel"></param>
        /// <param name="opcode"></param>
        /// <param name="task"></param>
        /// <param name="logContext">Log context.</param>
        /// <param name="payLoad"></param>
        internal void WriteEvent(PSEventId id, PSChannel channel, PSOpcode opcode, PSTask task, LogContext logContext, string payLoad)
        {
            s_provider.Log(id, channel, task, opcode, GetPSLevelFromSeverity(logContext.Severity), DefaultKeywords,
                           LogContextToString(logContext),
                           GetPSLogUserData(logContext.ExecutionContext),
                           payLoad);
        }

        /// <summary>
        /// Writes an event.
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
            s_provider.Log(id, channel, task, opcode, level, keyword, args);
        }

        /// <summary>
        /// Writes an activity transfer event.
        /// </summary>
        internal void WriteTransferEvent(Guid parentActivityId)
        {
            s_provider.LogTransfer(parentActivityId);
        }

        /// <summary>
        /// Sets the activity id for the current thread.
        /// </summary>
        /// <param name="newActivityId">The GUID identifying the activity.</param>
        internal void SetActivityIdForCurrentThread(Guid newActivityId)
        {
            s_provider.SetActivity(newActivityId);
        }
    }
}

#endif // UNIX
