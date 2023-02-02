// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX

using System.Diagnostics.Eventing;
using System.Management.Automation.Internal;
using System.Text;
using System.Collections.Generic;

namespace System.Management.Automation.Tracing
{
    /// <summary>
    /// ETW log provider implementation.
    /// </summary>
    internal class PSEtwLogProvider : LogProvider
    {
        private static readonly EventProvider etwProvider;
        internal static readonly Guid ProviderGuid = new Guid("F90714A8-5509-434A-BF6D-B1624C8A19A2");
        private static readonly EventDescriptor _xferEventDescriptor = new EventDescriptor(0x1f05, 0x1, 0x11, 0x5, 0x14, 0x0, (long)0x4000000000000000);

        /// <summary>
        /// Class constructor.
        /// </summary>
        static PSEtwLogProvider()
        {
            etwProvider = new EventProvider(ProviderGuid);
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
        /// Provider interface function for logging health event.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="eventId"></param>
        /// <param name="exception"></param>
        /// <param name="additionalInfo"></param>
        internal override void LogEngineHealthEvent(LogContext logContext, int eventId, Exception exception, Dictionary<string, string> additionalInfo)
        {
            StringBuilder payload = new StringBuilder();

            AppendException(payload, exception);
            payload.AppendLine();
            AppendAdditionalInfo(payload, additionalInfo);

            WriteEvent(PSEventId.Engine_Health, PSChannel.Operational, PSOpcode.Exception, PSTask.ExecutePipeline, logContext, payload.ToString());
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
        /// Provider interface function for logging command health event.
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
        /// Provider interface function for logging command lifecycle event.
        /// </summary>
        /// <param name="getLogContext"></param>
        /// <param name="newState"></param>
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
                        if (newState == CommandState.Stopped ||
                            newState == CommandState.Terminated)
                        {
                            // When state is stopped or terminated only log the CommandName
                            payload.AppendLine(StringUtil.Format(EtwLoggingStrings.CommandStateChange, logContext, newState.ToString()));
                        }
                        else
                        {
                            // When state is Start log the CommandLine which has arguments for completeness. 
                            payload.AppendLine(StringUtil.Format(EtwLoggingStrings.CommandStateChange, logContext.CommandLine, newState.ToString()));
                        }
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
            StringBuilder payload = new StringBuilder();

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
            StringBuilder payload = new StringBuilder();

            AppendException(payload, exception);
            payload.AppendLine();

            Dictionary<string, string> additionalInfo = new Dictionary<string, string>();

            additionalInfo.Add(EtwLoggingStrings.ProviderNameString, providerName);

            AppendAdditionalInfo(payload, additionalInfo);

            WriteEvent(PSEventId.Provider_Health, PSChannel.Operational, PSOpcode.Exception, PSTask.ExecutePipeline, logContext, payload.ToString());
        }

        /// <summary>
        /// Provider interface function for logging provider health event.
        /// </summary>
        /// <param name="state">This the action performed in AmsiUtil class, like init, scan, etc.</param>
        /// <param name="context">The amsiContext handled - Session pair.</param>
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
        /// Provider interface function for logging provider lifecycle event.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="providerName"></param>
        /// <param name="newState"></param>
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
        /// The ETW provider does not use logging variables.
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
            WriteEvent(id, channel, opcode, GetPSLevelFromSeverity(logContext.Severity), task, (PSKeyword)0x0,
                LogContextToString(logContext), GetPSLogUserData(logContext.ExecutionContext), payLoad);
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

            etwProvider.WriteEvent(in desc, args);
        }

        /// <summary>
        /// Writes an activity transfer event.
        /// </summary>
        internal void WriteTransferEvent(Guid parentActivityId)
        {
            etwProvider.WriteTransferEvent(in _xferEventDescriptor, parentActivityId, EtwActivity.GetActivityId(), parentActivityId);
        }

        /// <summary>
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
