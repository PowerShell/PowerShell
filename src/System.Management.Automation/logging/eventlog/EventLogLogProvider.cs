// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Resources;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Threading;

namespace System.Management.Automation
{
    /// <summary>
    /// EventLogLogProvider is a class to implement Msh Provider interface using EventLog technology.
    ///
    /// EventLogLogProvider will be the provider to use if Monad is running in early windows releases
    /// from 2000 to 2003.
    ///
    /// EventLogLogProvider will be packaged in the same dll as Msh Log Engine since EventLog should
    /// always be available.
    /// </summary>
    internal class EventLogLogProvider : LogProvider
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <returns></returns>
        internal EventLogLogProvider(string shellId)
        {
            string source = SetupEventSource(shellId);

            _eventLog = new EventLog();
            _eventLog.Source = source;

            _resourceManager = new ResourceManager("System.Management.Automation.resources.Logging", System.Reflection.Assembly.GetExecutingAssembly());
        }

        internal string SetupEventSource(string shellId)
        {
            string source;

            // In case shellId == null, use the "Default" source.
            if (string.IsNullOrEmpty(shellId))
            {
                source = "Default";
            }
            else
            {
                int index = shellId.LastIndexOf('.');

                if (index < 0)
                    source = shellId;
                else
                    source = shellId.Substring(index + 1);

                // There may be a situation where ShellId ends with a '.'.
                // In that case, use the default source.
                if (string.IsNullOrEmpty(source))
                    source = "Default";
            }

            if (EventLog.SourceExists(source))
            {
                return source;
            }

            string message = string.Format(Thread.CurrentThread.CurrentCulture, "Event source '{0}' is not registered", source);
            throw new InvalidOperationException(message);
        }

        /// <summary>
        /// This represent a handle to EventLog.
        /// </summary>
        private EventLog _eventLog;
        private ResourceManager _resourceManager;

        #region Log Provider Api

        private const int EngineHealthCategoryId = 1;
        private const int CommandHealthCategoryId = 2;
        private const int ProviderHealthCategoryId = 3;
        private const int EngineLifecycleCategoryId = 4;
        private const int CommandLifecycleCategoryId = 5;
        private const int ProviderLifecycleCategoryId = 6;
        private const int SettingsCategoryId = 7;
        private const int PipelineExecutionDetailCategoryId = 8;

        /// <summary>
        /// Log engine health event.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="eventId"></param>
        /// <param name="exception"></param>
        /// <param name="additionalInfo"></param>
        internal override void LogEngineHealthEvent(LogContext logContext, int eventId, Exception exception, Dictionary<string, string> additionalInfo)
        {
            Hashtable mapArgs = new Hashtable();

            IContainsErrorRecord icer = exception as IContainsErrorRecord;
            if (icer != null && icer.ErrorRecord != null)
            {
                mapArgs["ExceptionClass"] = exception.GetType().Name;
                mapArgs["ErrorCategory"] = icer.ErrorRecord.CategoryInfo.Category;
                mapArgs["ErrorId"] = icer.ErrorRecord.FullyQualifiedErrorId;

                if (icer.ErrorRecord.ErrorDetails != null)
                {
                    mapArgs["ErrorMessage"] = icer.ErrorRecord.ErrorDetails.Message;
                }
                else
                {
                    mapArgs["ErrorMessage"] = exception.Message;
                }
            }
            else
            {
                mapArgs["ExceptionClass"] = exception.GetType().Name;
                mapArgs["ErrorCategory"] = string.Empty;
                mapArgs["ErrorId"] = string.Empty;
                mapArgs["ErrorMessage"] = exception.Message;
            }

            FillEventArgs(mapArgs, logContext);

            FillEventArgs(mapArgs, additionalInfo);

            EventInstance entry = new EventInstance(eventId, EngineHealthCategoryId);

            entry.EntryType = GetEventLogEntryType(logContext);

            string detail = GetEventDetail("EngineHealthContext", mapArgs);

            LogEvent(entry, mapArgs["ErrorMessage"], detail);
        }

        private static EventLogEntryType GetEventLogEntryType(LogContext logContext)
        {
            switch (logContext.Severity)
            {
                case "Critical":
                case "Error":
                    return EventLogEntryType.Error;
                case "Warning":
                    return EventLogEntryType.Warning;
                default:
                    return EventLogEntryType.Information;
            }
        }

        /// <summary>
        /// Log engine lifecycle event.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="newState"></param>
        /// <param name="previousState"></param>
        internal override void LogEngineLifecycleEvent(LogContext logContext, EngineState newState, EngineState previousState)
        {
            int eventId = GetEngineLifecycleEventId(newState);

            if (eventId == _invalidEventId)
                return;

            Hashtable mapArgs = new Hashtable();

            mapArgs["NewEngineState"] = newState.ToString();
            mapArgs["PreviousEngineState"] = previousState.ToString();

            FillEventArgs(mapArgs, logContext);

            EventInstance entry = new EventInstance(eventId, EngineLifecycleCategoryId);

            entry.EntryType = EventLogEntryType.Information;

            string detail = GetEventDetail("EngineLifecycleContext", mapArgs);

            LogEvent(entry, newState, previousState, detail);
        }

        private const int _baseEngineLifecycleEventId = 400;
        private const int _invalidEventId = -1;

        /// <summary>
        /// Get engine lifecycle event id based on engine state.
        /// </summary>
        /// <param name="engineState"></param>
        /// <returns></returns>
        private static int GetEngineLifecycleEventId(EngineState engineState)
        {
            switch (engineState)
            {
                case EngineState.None:
                    return _invalidEventId;
                case EngineState.Available:
                    return _baseEngineLifecycleEventId;
                case EngineState.Degraded:
                    return _baseEngineLifecycleEventId + 1;
                case EngineState.OutOfService:
                    return _baseEngineLifecycleEventId + 2;
                case EngineState.Stopped:
                    return _baseEngineLifecycleEventId + 3;
            }

            return _invalidEventId;
        }

        private const int _commandHealthEventId = 200;

        /// <summary>
        /// Provider interface function for logging command health event.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="exception"></param>
        internal override void LogCommandHealthEvent(LogContext logContext, Exception exception)
        {
            int eventId = _commandHealthEventId;

            Hashtable mapArgs = new Hashtable();

            IContainsErrorRecord icer = exception as IContainsErrorRecord;
            if (icer != null && icer.ErrorRecord != null)
            {
                mapArgs["ExceptionClass"] = exception.GetType().Name;
                mapArgs["ErrorCategory"] = icer.ErrorRecord.CategoryInfo.Category;
                mapArgs["ErrorId"] = icer.ErrorRecord.FullyQualifiedErrorId;

                if (icer.ErrorRecord.ErrorDetails != null)
                {
                    mapArgs["ErrorMessage"] = icer.ErrorRecord.ErrorDetails.Message;
                }
                else
                {
                    mapArgs["ErrorMessage"] = exception.Message;
                }
            }
            else
            {
                mapArgs["ExceptionClass"] = exception.GetType().Name;
                mapArgs["ErrorCategory"] = string.Empty;
                mapArgs["ErrorId"] = string.Empty;
                mapArgs["ErrorMessage"] = exception.Message;
            }

            FillEventArgs(mapArgs, logContext);

            EventInstance entry = new EventInstance(eventId, CommandHealthCategoryId);

            entry.EntryType = GetEventLogEntryType(logContext);

            string detail = GetEventDetail("CommandHealthContext", mapArgs);

            LogEvent(entry, mapArgs["ErrorMessage"], detail);
        }

        /// <summary>
        /// Log command life cycle event.
        /// </summary>
        /// <param name="getLogContext"></param>
        /// <param name="newState"></param>
        internal override void LogCommandLifecycleEvent(Func<LogContext> getLogContext, CommandState newState)
        {
            LogContext logContext = getLogContext();

            int eventId = GetCommandLifecycleEventId(newState);

            if (eventId == _invalidEventId)
                return;

            Hashtable mapArgs = new Hashtable();

            mapArgs["NewCommandState"] = newState.ToString();

            FillEventArgs(mapArgs, logContext);

            EventInstance entry = new EventInstance(eventId, CommandLifecycleCategoryId);

            entry.EntryType = EventLogEntryType.Information;

            string detail = GetEventDetail("CommandLifecycleContext", mapArgs);

            LogEvent(entry, logContext.CommandName, newState, detail);
        }

        private const int _baseCommandLifecycleEventId = 500;

        /// <summary>
        /// Get command lifecycle event id based on command state.
        /// </summary>
        /// <param name="commandState"></param>
        /// <returns></returns>
        private static int GetCommandLifecycleEventId(CommandState commandState)
        {
            switch (commandState)
            {
                case CommandState.Started:
                    return _baseCommandLifecycleEventId;
                case CommandState.Stopped:
                    return _baseCommandLifecycleEventId + 1;
                case CommandState.Terminated:
                    return _baseCommandLifecycleEventId + 2;
            }

            return _invalidEventId;
        }

        private const int _pipelineExecutionDetailEventId = 800;

        /// <summary>
        /// Log pipeline execution detail event.
        ///
        /// This may end of logging more than one event if the detail string is too long to be fit in 64K.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="pipelineExecutionDetail"></param>
        internal override void LogPipelineExecutionDetailEvent(LogContext logContext, List<string> pipelineExecutionDetail)
        {
            List<string> details = GroupMessages(pipelineExecutionDetail);

            for (int i = 0; i < details.Count; i++)
            {
                LogPipelineExecutionDetailEvent(logContext, details[i], i + 1, details.Count);
            }
        }

        private const int MaxLength = 16000;

        private List<string> GroupMessages(List<string> messages)
        {
            List<string> result = new List<string>();

            if (messages == null || messages.Count == 0)
                return result;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < messages.Count; i++)
            {
                if (sb.Length + messages[i].Length < MaxLength)
                {
                    sb.AppendLine(messages[i]);
                    continue;
                }

                result.Add(sb.ToString());
                sb = new StringBuilder();
                sb.AppendLine(messages[i]);
            }

            result.Add(sb.ToString());

            return result;
        }

        /// <summary>
        /// Log one pipeline execution detail event. Detail message is already chopped up so that it will
        /// fit in 64K.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="pipelineExecutionDetail"></param>
        /// <param name="detailSequence"></param>
        /// <param name="detailTotal"></param>
        private void LogPipelineExecutionDetailEvent(LogContext logContext, string pipelineExecutionDetail, int detailSequence, int detailTotal)
        {
            int eventId = _pipelineExecutionDetailEventId;

            Hashtable mapArgs = new Hashtable();

            mapArgs["PipelineExecutionDetail"] = pipelineExecutionDetail;
            mapArgs["DetailSequence"] = detailSequence;
            mapArgs["DetailTotal"] = detailTotal;

            FillEventArgs(mapArgs, logContext);

            EventInstance entry = new EventInstance(eventId, PipelineExecutionDetailCategoryId);

            entry.EntryType = EventLogEntryType.Information;

            string pipelineInfo = GetEventDetail("PipelineExecutionDetailContext", mapArgs);

            LogEvent(entry, logContext.CommandLine, pipelineInfo, pipelineExecutionDetail);
        }

        private const int _providerHealthEventId = 300;
        /// <summary>
        /// Provider interface function for logging provider health event.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="providerName"></param>
        /// <param name="exception"></param>
        internal override void LogProviderHealthEvent(LogContext logContext, string providerName, Exception exception)
        {
            int eventId = _providerHealthEventId;

            Hashtable mapArgs = new Hashtable();

            mapArgs["ProviderName"] = providerName;

            IContainsErrorRecord icer = exception as IContainsErrorRecord;
            if (icer != null && icer.ErrorRecord != null)
            {
                mapArgs["ExceptionClass"] = exception.GetType().Name;
                mapArgs["ErrorCategory"] = icer.ErrorRecord.CategoryInfo.Category;
                mapArgs["ErrorId"] = icer.ErrorRecord.FullyQualifiedErrorId;

                if (icer.ErrorRecord.ErrorDetails != null
                    && !string.IsNullOrEmpty(icer.ErrorRecord.ErrorDetails.Message))
                {
                    mapArgs["ErrorMessage"] = icer.ErrorRecord.ErrorDetails.Message;
                }
                else
                {
                    mapArgs["ErrorMessage"] = exception.Message;
                }
            }
            else
            {
                mapArgs["ExceptionClass"] = exception.GetType().Name;
                mapArgs["ErrorCategory"] = string.Empty;
                mapArgs["ErrorId"] = string.Empty;
                mapArgs["ErrorMessage"] = exception.Message;
            }

            FillEventArgs(mapArgs, logContext);

            EventInstance entry = new EventInstance(eventId, ProviderHealthCategoryId);

            entry.EntryType = GetEventLogEntryType(logContext);

            string detail = GetEventDetail("ProviderHealthContext", mapArgs);

            LogEvent(entry, mapArgs["ErrorMessage"], detail);
        }

        /// <summary>
        /// Log provider lifecycle event.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="providerName"></param>
        /// <param name="newState"></param>
        internal override void LogProviderLifecycleEvent(LogContext logContext, string providerName, ProviderState newState)
        {
            int eventId = GetProviderLifecycleEventId(newState);

            if (eventId == _invalidEventId)
                return;

            Hashtable mapArgs = new Hashtable();

            mapArgs["ProviderName"] = providerName;
            mapArgs["NewProviderState"] = newState.ToString();

            FillEventArgs(mapArgs, logContext);

            EventInstance entry = new EventInstance(eventId, ProviderLifecycleCategoryId);

            entry.EntryType = EventLogEntryType.Information;

            string detail = GetEventDetail("ProviderLifecycleContext", mapArgs);

            LogEvent(entry, providerName, newState, detail);
        }

        private const int _baseProviderLifecycleEventId = 600;

        /// <summary>
        /// Get provider lifecycle event id based on provider state.
        /// </summary>
        /// <param name="providerState"></param>
        /// <returns></returns>
        private static int GetProviderLifecycleEventId(ProviderState providerState)
        {
            switch (providerState)
            {
                case ProviderState.Started:
                    return _baseProviderLifecycleEventId;
                case ProviderState.Stopped:
                    return _baseProviderLifecycleEventId + 1;
            }

            return _invalidEventId;
        }

        private const int _settingsEventId = 700;

        /// <summary>
        /// Log settings event.
        /// </summary>
        /// <param name="logContext"></param>
        /// <param name="variableName"></param>
        /// <param name="value"></param>
        /// <param name="previousValue"></param>
        internal override void LogSettingsEvent(LogContext logContext, string variableName, string value, string previousValue)
        {
            int eventId = _settingsEventId;

            Hashtable mapArgs = new Hashtable();

            mapArgs["VariableName"] = variableName;
            mapArgs["NewValue"] = value;
            mapArgs["PreviousValue"] = previousValue;

            FillEventArgs(mapArgs, logContext);

            EventInstance entry = new EventInstance(eventId, SettingsCategoryId);

            entry.EntryType = EventLogEntryType.Information;

            string detail = GetEventDetail("SettingsContext", mapArgs);

            LogEvent(entry, variableName, value, previousValue, detail);
        }

        #endregion Log Provider Api

        #region EventLog helper functions

        /// <summary>
        /// This is the helper function for logging an event with localizable message
        /// to event log. It will trace all exception thrown by eventlog.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="args"></param>
        private void LogEvent(EventInstance entry, params object[] args)
        {
            try
            {
                _eventLog.WriteEvent(entry, args);
            }
            catch (ArgumentException)
            {
                return;
            }
            catch (InvalidOperationException)
            {
                return;
            }
            catch (Win32Exception)
            {
                return;
            }
        }

        #endregion

        #region Event Arguments

        /// <summary>
        /// Fill event arguments with logContext info.
        ///
        /// In EventLog Api, arguments are passed in as an array of objects.
        /// </summary>
        /// <param name="mapArgs">An ArrayList to contain the event arguments.</param>
        /// <param name="logContext">The log context containing the info to fill in.</param>
        private static void FillEventArgs(Hashtable mapArgs, LogContext logContext)
        {
            mapArgs["Severity"] = logContext.Severity;
            mapArgs["SequenceNumber"] = logContext.SequenceNumber;
            mapArgs["HostName"] = logContext.HostName;
            mapArgs["HostVersion"] = logContext.HostVersion;
            mapArgs["HostId"] = logContext.HostId;
            mapArgs["HostApplication"] = logContext.HostApplication;
            mapArgs["EngineVersion"] = logContext.EngineVersion;
            mapArgs["RunspaceId"] = logContext.RunspaceId;
            mapArgs["PipelineId"] = logContext.PipelineId;
            mapArgs["CommandName"] = logContext.CommandName;
            mapArgs["CommandType"] = logContext.CommandType;
            mapArgs["ScriptName"] = logContext.ScriptName;
            mapArgs["CommandPath"] = logContext.CommandPath;
            mapArgs["CommandLine"] = logContext.CommandLine;
            mapArgs["User"] = logContext.User;
            mapArgs["Time"] = logContext.Time;
        }

        /// <summary>
        /// Fill event arguments with additionalInfo stored in a string dictionary.
        /// </summary>
        /// <param name="mapArgs">An arraylist to contain the event arguments.</param>
        /// <param name="additionalInfo">A string dictionary to fill in.</param>
        private static void FillEventArgs(Hashtable mapArgs, Dictionary<string, string> additionalInfo)
        {
            if (additionalInfo == null)
            {
                for (int i = 0; i < 3; i++)
                {
                    string id = ((int)(i + 1)).ToString("d1", CultureInfo.CurrentCulture);

                    mapArgs["AdditionalInfo_Name" + id] = string.Empty;
                    mapArgs["AdditionalInfo_Value" + id] = string.Empty;
                }

                return;
            }

            string[] keys = new string[additionalInfo.Count];
            string[] values = new string[additionalInfo.Count];

            additionalInfo.Keys.CopyTo(keys, 0);
            additionalInfo.Values.CopyTo(values, 0);
            for (int i = 0; i < 3; i++)
            {
                string id = ((int)(i + 1)).ToString("d1", CultureInfo.CurrentCulture);

                if (i < keys.Length)
                {
                    mapArgs["AdditionalInfo_Name" + id] = keys[i];
                    mapArgs["AdditionalInfo_Value" + id] = values[i];
                }
                else
                {
                    mapArgs["AdditionalInfo_Name" + id] = string.Empty;
                    mapArgs["AdditionalInfo_Value" + id] = string.Empty;
                }
            }

            return;
        }

        #endregion Event Arguments

        #region Event Message

        private string GetEventDetail(string contextId, Hashtable mapArgs)
        {
            return GetMessage(contextId, mapArgs);
        }

        private string GetMessage(string messageId, Hashtable mapArgs)
        {
            if (_resourceManager == null)
                return string.Empty;

            string messageTemplate = _resourceManager.GetString(messageId);

            if (string.IsNullOrEmpty(messageTemplate))
                return string.Empty;

            return FillMessageTemplate(messageTemplate, mapArgs);
        }

        private static string FillMessageTemplate(string messageTemplate, Hashtable mapArgs)
        {
            StringBuilder message = new StringBuilder();

            int cursor = 0;

            while (true)
            {
                int startIndex = messageTemplate.IndexOf('[', cursor);

                if (startIndex < 0)
                {
                    message.Append(messageTemplate.Substring(cursor));
                    return message.ToString();
                }

                int endIndex = messageTemplate.IndexOf(']', startIndex + 1);

                if (endIndex < 0)
                {
                    message.Append(messageTemplate.Substring(cursor));
                    return message.ToString();
                }

                message.Append(messageTemplate.Substring(cursor, startIndex - cursor));
                cursor = startIndex;

                string placeHolder = messageTemplate.Substring(startIndex + 1, endIndex - startIndex - 1);

                if (mapArgs.Contains(placeHolder))
                {
                    message.Append(mapArgs[placeHolder]);
                    cursor = endIndex + 1;
                }
                else
                {
                    message.Append("[");
                    cursor++;
                }
            }
        }

        #endregion Event Message
    }
}
