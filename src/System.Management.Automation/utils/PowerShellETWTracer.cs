// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if !UNIX

using System.Globalization;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Diagnostics.Eventing;
using System.Management.Automation.Internal;

namespace System.Management.Automation.Tracing
{
    // pragma warning disable 16001,16003
#region Constants

    /// <summary>
    /// Defines enumerations for event ids.
    /// </summary>
    /// <remarks>add an entry for a new event that you
    /// add to the manifest. Set it to the same value
    /// that was set in the manifest</remarks>
    public enum PowerShellTraceEvent : int
    {
        /// <summary>
        /// None. (Should not be used)
        /// </summary>
        None = 0,

        /// <summary>
        /// HostNameResolve.
        /// </summary>
        HostNameResolve = 0x1001,

        /// <summary>
        /// SchemeResolve.
        /// </summary>
        SchemeResolve = 0x1002,

        /// <summary>
        /// ShellResolve.
        /// </summary>
        ShellResolve = 0x1003,

        /// <summary>
        /// RunspaceConstructor.
        /// </summary>
        RunspaceConstructor = 0x2001,

        /// <summary>
        /// RunspacePoolConstructor.
        /// </summary>
        RunspacePoolConstructor = 0x2002,

        /// <summary>
        /// RunspacePoolOpen.
        /// </summary>
        RunspacePoolOpen = 0x2003,

        /// <summary>
        /// OperationalTransferEventRunspacePool.
        /// </summary>
        OperationalTransferEventRunspacePool = 0x2004,

        /// <summary>
        /// RunspacePort.
        /// </summary>
        RunspacePort = 0x2F01,

        /// <summary>
        /// AppName.
        /// </summary>
        AppName = 0x2F02,

        /// <summary>
        /// ComputerName.
        /// </summary>
        ComputerName = 0x2F03,

        /// <summary>
        /// Scheme.
        /// </summary>
        Scheme = 0x2F04,

        /// <summary>
        /// TestAnalytic.
        /// </summary>
        TestAnalytic = 0x2F05,

        /// <summary>
        /// WSManConnectionInfoDump.
        /// </summary>
        WSManConnectionInfoDump = 0x2F06,

        /// <summary>
        /// AnalyticTransferEventRunspacePool.
        /// </summary>
        AnalyticTransferEventRunspacePool = 0x2F07,

        // Start: Transport related events

        /// <summary>
        /// TransportReceivedObject.
        /// </summary>
        TransportReceivedObject = 0x8001,

        /// <summary>
        /// AppDomainUnhandledExceptionAnalytic.
        /// </summary>
        AppDomainUnhandledExceptionAnalytic = 0x8007,

        /// <summary>
        /// TransportErrorAnalytic.
        /// </summary>
        TransportErrorAnalytic = 0x8008,

        /// <summary>
        /// AppDomainUnhandledException.
        /// </summary>
        AppDomainUnhandledException = 0x8009,

        /// <summary>
        /// TransportError.
        /// </summary>
        TransportError = 0x8010,

        /// <summary>
        /// WSManCreateShell.
        /// </summary>
        WSManCreateShell = 0x8011,

        /// <summary>
        /// WSManCreateShellCallbackReceived.
        /// </summary>
        WSManCreateShellCallbackReceived = 0x8012,

        /// <summary>
        /// WSManCloseShell.
        /// </summary>
        WSManCloseShell = 0x8013,

        /// <summary>
        /// WSManCloseShellCallbackReceived.
        /// </summary>
        WSManCloseShellCallbackReceived = 0x8014,

        /// <summary>
        /// WSManSendShellInputExtended.
        /// </summary>
        WSManSendShellInputExtended = 0x8015,

        /// <summary>
        /// WSManSendShellInputExCallbackReceived.
        /// </summary>
        WSManSendShellInputExtendedCallbackReceived = 0x8016,

        /// <summary>
        /// WSManReceiveShellOutputExtended.
        /// </summary>
        WSManReceiveShellOutputExtended = 0x8017,

        /// <summary>
        /// WSManReceiveShellOutputExCallbackReceived.
        /// </summary>
        WSManReceiveShellOutputExtendedCallbackReceived = 0x8018,

        /// <summary>
        /// WSManCreateCommand.
        /// </summary>
        WSManCreateCommand = 0x8019,

        /// <summary>
        /// WSManCreateCommandCallbackReceived.
        /// </summary>
        WSManCreateCommandCallbackReceived = 0x8020,

        /// <summary>
        /// WSManCloseCommand.
        /// </summary>
        WSManCloseCommand = 0x8021,

        /// <summary>
        /// WSManCloseCommandCallbackReceived.
        /// </summary>
        WSManCloseCommandCallbackReceived = 0x8022,

        /// <summary>
        /// WSManSignal.
        /// </summary>
        WSManSignal = 0x8023,

        /// <summary>
        /// WSManSignalCallbackReceived.
        /// </summary>
        WSManSignalCallbackReceived = 0x8024,

        /// <summary>
        /// UriRedirection.
        /// </summary>
        UriRedirection = 0x8025,

        /// <summary>
        /// ServerSendData.
        /// </summary>
        ServerSendData = 0x8051,

        /// <summary>
        /// ServerCreateRemoteSession.
        /// </summary>
        ServerCreateRemoteSession = 0x8052,

        /// <summary>
        /// ReportContext.
        /// </summary>
        ReportContext = 0x8053,

        /// <summary>
        /// ReportOperationComplete.
        /// </summary>
        ReportOperationComplete = 0x8054,

        /// <summary>
        /// ServerCreateCommandSession.
        /// </summary>
        ServerCreateCommandSession = 0x8055,

        /// <summary>
        /// ServerStopCommand.
        /// </summary>
        ServerStopCommand = 0x8056,

        /// <summary>
        /// ServerReceivedData.
        /// </summary>
        ServerReceivedData = 0x8057,

        /// <summary>
        /// ServerClientReceiveRequest.
        /// </summary>
        ServerClientReceiveRequest = 0x8058,

        /// <summary>
        /// ServerCloseOperation.
        /// </summary>
        ServerCloseOperation = 0x8059,

        /// <summary>
        /// LoadingPSCustomShellAssembly.
        /// </summary>
        LoadingPSCustomShellAssembly = 0x8061,

        /// <summary>
        /// LoadingPSCustomShellType.
        /// </summary>
        LoadingPSCustomShellType = 0x8062,

        /// <summary>
        /// ReceivedRemotingFragment.
        /// </summary>
        ReceivedRemotingFragment = 0x8063,

        /// <summary>
        /// SentRemotingFragment.
        /// </summary>
        SentRemotingFragment = 0x8064,

        /// <summary>
        /// WSManPluginShutdown.
        /// </summary>
        WSManPluginShutdown = 0x8065,
        // End: Transport related events

        // Start: Serialization related events

        /// <summary>
        /// SerializerWorkflowLoadSuccess.
        /// </summary>
        SerializerWorkflowLoadSuccess = 0x7001,

        /// <summary>
        /// SerializerWorkflowLoadFailure.
        /// </summary>
        SerializerWorkflowLoadFailure = 0x7002,

        /// <summary>
        /// SerializerDepthOverride.
        /// </summary>
        SerializerDepthOverride = 0x7003,

        /// <summary>
        /// SerializerModeOverride.
        /// </summary>
        SerializerModeOverride = 0x7004,

        /// <summary>
        /// SerializerScriptPropertyWithoutRunspace.
        /// </summary>
        SerializerScriptPropertyWithoutRunspace = 0x7005,

        /// <summary>
        /// SerializerPropertyGetterFailed.
        /// </summary>
        SerializerPropertyGetterFailed = 0x7006,

        /// <summary>
        /// SerializerEnumerationFailed.
        /// </summary>
        SerializerEnumerationFailed = 0x7007,

        /// <summary>
        /// SerializerToStringFailed.
        /// </summary>
        SerializerToStringFailed = 0x7008,

        /// <summary>
        /// SerializerMaxDepthWhenSerializing.
        /// </summary>
        SerializerMaxDepthWhenSerializing = 0x700A,

        /// <summary>
        /// SerializerXmlExceptionWhenDeserializing.
        /// </summary>
        SerializerXmlExceptionWhenDeserializing = 0x700B,

        /// <summary>
        /// SerializerSpecificPropertyMissing.
        /// </summary>
        SerializerSpecificPropertyMissing = 0x700C,
        // End: Serialization related events

        // Start: PerformanceTrack related events
        /// <summary>
        /// PerformanceTrackConsoleStartupStart.
        /// </summary>
        PerformanceTrackConsoleStartupStart = 0xA001,

        /// <summary>
        /// PerformanceTrackConsoleStartupStop.
        /// </summary>
        PerformanceTrackConsoleStartupStop = 0xA002,
        // End: Preftrack related events

        /// <summary>
        /// ErrorRecord.
        /// </summary>
        ErrorRecord = 0xB001,

        /// <summary>
        /// Exception.
        /// </summary>
        Exception = 0xB002,

        /// <summary>
        /// PowerShellObject.
        /// </summary>
        PowerShellObject = 0xB003,

        /// <summary>
        /// Job.
        /// </summary>
        Job = 0xB004,

        /// <summary>
        /// Writing a simple trace message from code.
        /// </summary>
        TraceMessage = 0xB005,

        /// <summary>
        /// Trace the WSManConnectionInfo used for this connection.
        /// </summary>
        TraceWSManConnectionInfo = 0xB006,

        /// <summary>
        /// Writing a simple trace message from code with 2
        /// strings.
        /// </summary>
        TraceMessage2 = 0xC001,

        /// <summary>
        /// Writing a simple trace message from code with 2
        /// strings.
        /// </summary>
        TraceMessageGuid = 0xC002,
    }

    /// <summary>
    /// Defines enumerations for channels.
    /// </summary>
    // pragma warning disable 16001
    public enum PowerShellTraceChannel
    {
        /// <summary>
        /// None (No channel selected, should not be used)
        /// </summary>
        None = 0,
        /// <summary>
        /// Operational Channel.
        /// </summary>
        Operational = 0x10,

        /// <summary>
        /// Analytic Channel.
        /// </summary>
        Analytic = 0x11,

        /// <summary>
        /// Debug Channel.
        /// </summary>
        Debug = 0x12,
    }
    // pragma warning restore 16001

    /// <summary>
    /// Define enumerations for levels.
    /// </summary>
    public enum PowerShellTraceLevel
    {
        /// <summary>
        /// LogAlways.
        /// </summary>
        LogAlways = 0,

        /// <summary>
        /// Critical.
        /// </summary>
        Critical = 1,

        /// <summary>
        /// Error.
        /// </summary>
        Error = 2,

        /// <summary>
        /// Warning.
        /// </summary>
        Warning = 3,

        /// <summary>
        /// Informational.
        /// </summary>
        Informational = 4,

        /// <summary>
        /// Verbose.
        /// </summary>
        Verbose = 5,

        /// <summary>
        /// Debug.
        /// </summary>
        Debug = 20,
    }

    /// <summary>
    /// Defines enumerations for op codes.
    /// </summary>
    public enum PowerShellTraceOperationCode
    {
        /// <summary>
        /// None.  (Should not be used)
        /// </summary>
        None = 0,

        /// <summary>
        /// Open.
        /// </summary>
        Open = 10,

        /// <summary>
        /// Close.
        /// </summary>
        Close = 11,

        /// <summary>
        /// Connect.
        /// </summary>
        Connect = 12,

        /// <summary>
        /// Disconnect.
        /// </summary>
        Disconnect = 13,

        /// <summary>
        /// Negotiate.
        /// </summary>
        Negotiate = 14,

        /// <summary>
        /// Create.
        /// </summary>
        Create = 15,

        /// <summary>
        /// Constructor.
        /// </summary>
        Constructor = 16,

        /// <summary>
        /// Dispose.
        /// </summary>
        Dispose = 17,

        /// <summary>
        /// EventHandler.
        /// </summary>
        EventHandler = 18,

        /// <summary>
        /// Exception.
        /// </summary>
        Exception = 19,

        /// <summary>
        /// Method.
        /// </summary>
        Method = 20,

        /// <summary>
        /// Send.
        /// </summary>
        Send = 21,

        /// <summary>
        /// Receive.
        /// </summary>
        Receive = 22,

        /// <summary>
        /// WorkflowLoad.
        /// </summary>
        WorkflowLoad = 23,

        /// <summary>
        /// SerializationSettings.
        /// </summary>
        SerializationSettings = 24,

        /// <summary>
        /// WinInfo.
        /// </summary>
        WinInfo,

        /// <summary>
        /// WinStart.
        /// </summary>
        WinStart,

        /// <summary>
        /// WinStop.
        /// </summary>
        WinStop,

        /// <summary>
        /// WinDCStart.
        /// </summary>
        WinDCStart,

        /// <summary>
        /// WinDCStop.
        /// </summary>
        WinDCStop,

        /// <summary>
        /// WinExtension.
        /// </summary>
        WinExtension,

        /// <summary>
        /// WinReply.
        /// </summary>
        WinReply,

        /// <summary>
        /// WinResume.
        /// </summary>
        WinResume,

        /// <summary>
        /// WinSuspend.
        /// </summary>
        WinSuspend,
    }

    /// <summary>
    /// Defines Tasks.
    /// </summary>
    public enum PowerShellTraceTask
    {
        /// <summary>
        /// None.
        /// </summary>
        None = 0,

        /// <summary>
        /// CreateRunspace.
        /// </summary>
        CreateRunspace = 1,

        /// <summary>
        /// ExecuteCommand.
        /// </summary>
        ExecuteCommand = 2,

        /// <summary>
        /// Serialization.
        /// </summary>
        Serialization = 3,

        /// <summary>
        /// PowerShellConsoleStartup.
        /// </summary>
        PowerShellConsoleStartup = 4,
    }

    /// <summary>
    /// Defines Keywords.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1028")]
    [Flags]
    public enum PowerShellTraceKeywords : ulong
    {
        /// <summary>
        /// None.
        /// </summary>
        None = 0,

        /// <summary>
        /// Runspace.
        /// </summary>
        Runspace = 0x1,

        /// <summary>
        /// Pipeline.
        /// </summary>
        Pipeline = 0x2,

        /// <summary>
        /// Protocol.
        /// </summary>
        Protocol = 0x4,

        /// <summary>
        /// Transport.
        /// </summary>
        Transport = 0x8,

        /// <summary>
        /// Host.
        /// </summary>
        Host = 0x10,

        /// <summary>
        /// Cmdlets.
        /// </summary>
        Cmdlets = 0x20,

        /// <summary>
        /// Serializer.
        /// </summary>
        Serializer = 0x40,

        /// <summary>
        /// Session.
        /// </summary>
        Session = 0x80,

        /// <summary>
        /// ManagedPlugIn.
        /// </summary>
        ManagedPlugIn = 0x100,

        /// <summary>
        /// </summary>
        UseAlwaysDebug = 0x2000000000000000,

        /// <summary>
        /// </summary>
        UseAlwaysOperational = 0x8000000000000000,

        /// <summary>
        /// </summary>
        UseAlwaysAnalytic = 0x4000000000000000,
    }

#endregion

    /// <summary>
    /// BaseChannelWriter is the abstract base class defines event specific methods that are used to write a trace.
    /// The default implementation does not write any message to any trace channel.
    /// </summary>
    public abstract class BaseChannelWriter : IDisposable
    {
        private bool disposed;

        /// <summary>
        /// Dispose method.
        /// </summary>
        public virtual void Dispose()
        {
            if (!disposed)
            {
                GC.SuppressFinalize(this);
                disposed = true;
            }
        }

        /// <summary>
        /// TraceError.
        /// </summary>
        public virtual bool TraceError(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return true;
        }

        /// <summary>
        /// TraceWarning.
        /// </summary>
        public virtual bool TraceWarning(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return true;
        }

        /// <summary>
        /// TraceInformational.
        /// </summary>
        public virtual bool TraceInformational(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return true;
        }

        /// <summary>
        /// TraceVerbose.
        /// </summary>
        public virtual bool TraceVerbose(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return true;
        }

        /// <summary>
        /// TraceDebug.
        /// </summary>
        public virtual bool TraceDebug(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return true;
        }

        /// <summary>
        /// TraceLogAlways.
        /// </summary>
        public virtual bool TraceLogAlways(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return true;
        }

        /// <summary>
        /// TraceCritical.
        /// </summary>
        public virtual bool TraceCritical(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return true;
        }

        /// <summary>
        /// </summary>
        public virtual PowerShellTraceKeywords Keywords
        {
            get
            {
                return PowerShellTraceKeywords.None;
            }

            set
            {
                PowerShellTraceKeywords powerShellTraceKeywords = value;
            }
        }
    }

    /// <summary>
    /// NullWriter is the implementation of BaseChannelWriter.
    /// This implementation does not write to any trace logs.
    /// This class is singleton and exposes its only instance
    /// through the static Instance property.
    /// </summary>
    public sealed class NullWriter : BaseChannelWriter
    {
        /// <summary>
        /// Static Instance property.
        /// </summary>
        public static BaseChannelWriter Instance { get; } = new NullWriter();

        private NullWriter()
        {
        }
    }

    /// <summary>
    /// ChannelWrite is the concrete implementation of IChannelWrite.  It writes all the traces to the specified traceChannel.
    /// TraceChannel is specified in the constructor.
    /// It always uses PowerShell event provider Id.
    /// </summary>
    public sealed class PowerShellChannelWriter : BaseChannelWriter
    {
        private readonly PowerShellTraceChannel _traceChannel;

        /*
         * Making the provider static to reduce the number of buffers needed to 1.
         * */
        private static readonly EventProvider _provider = new EventProvider(PSEtwLogProvider.ProviderGuid);

        private bool disposed;
        private PowerShellTraceKeywords _keywords;

        /// <summary>
        /// </summary>
        public override PowerShellTraceKeywords Keywords
        {
            get
            {
                return _keywords;
            }

            set
            {
                _keywords = value;
            }
        }

        internal PowerShellChannelWriter(PowerShellTraceChannel traceChannel, PowerShellTraceKeywords keywords)
        {
            _traceChannel = traceChannel;
            _keywords = keywords;
        }

        /// <summary>
        /// Dispose method.
        /// </summary>
        public override void Dispose()
        {
            if (!disposed)
            {
                GC.SuppressFinalize(this);
                disposed = true;
            }
        }

        private bool Trace(PowerShellTraceEvent traceEvent, PowerShellTraceLevel level, PowerShellTraceOperationCode operationCode,
            PowerShellTraceTask task, params object[] args)
        {
            EventDescriptor ed = new EventDescriptor((int)traceEvent, 1, (byte)_traceChannel, (byte)level,
                                                     (byte)operationCode, (int)task, (long)_keywords);

            /*
             * Not using locks because the _provider is thread safe itself.
             **/

            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == null)
                    {
                        args[i] = string.Empty;
                    }
                }
            }

            return _provider.WriteEvent(ref ed, args);
        }

        /// <summary>
        /// TraceError.
        /// </summary>
        public override bool TraceError(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return Trace(traceEvent, PowerShellTraceLevel.Error, operationCode, task, args);
        }

        /// <summary>
        /// TraceWarning.
        /// </summary>
        public override bool TraceWarning(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return Trace(traceEvent, PowerShellTraceLevel.Warning, operationCode, task, args);
        }

        /// <summary>
        /// TraceInformational.
        /// </summary>
        public override bool TraceInformational(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return Trace(traceEvent, PowerShellTraceLevel.Informational, operationCode, task, args);
        }

        /// <summary>
        /// TraceVerbose.
        /// </summary>
        public override bool TraceVerbose(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return Trace(traceEvent, PowerShellTraceLevel.Verbose, operationCode, task, args);
        }

        /// <summary>
        /// TraceDebug.
        /// </summary>
        public override bool TraceDebug(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            // TODO: There is some error thrown by the custom debug level
            // hence Informational is being used
            return Trace(traceEvent, PowerShellTraceLevel.Informational, operationCode, task, args);
        }

        /// <summary>
        /// TraceLogAlways.
        /// </summary>
        public override bool TraceLogAlways(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return Trace(traceEvent, PowerShellTraceLevel.LogAlways, operationCode, task, args);
        }

        /// <summary>
        /// TraceCritical.
        /// </summary>
        public override bool TraceCritical(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return Trace(traceEvent, PowerShellTraceLevel.Critical, operationCode, task, args);
        }
    }

    /// <summary>
    /// TraceSource class gives access to the actual TraceWriter channels.
    /// Three channels are pre-defined 1) Debug 2) Analytic and 3) Operations
    /// This class also has strongly types methods that are used for easy tracing.
    /// </summary>
    public sealed class PowerShellTraceSource : IDisposable
    {
        private bool disposed;

        /// <summary>
        /// Constructor.
        /// </summary>
        internal PowerShellTraceSource(PowerShellTraceTask task, PowerShellTraceKeywords keywords)
        {
            if (IsEtwSupported)
            {
                DebugChannel = new PowerShellChannelWriter(PowerShellTraceChannel.Debug,
                                                           keywords | PowerShellTraceKeywords.UseAlwaysDebug);
                AnalyticChannel = new PowerShellChannelWriter(PowerShellTraceChannel.Analytic,
                                                              keywords | PowerShellTraceKeywords.UseAlwaysAnalytic);
                OperationalChannel = new PowerShellChannelWriter(PowerShellTraceChannel.Operational,
                                                                keywords | PowerShellTraceKeywords.UseAlwaysOperational);

                this.Task = task;
                this.Keywords = keywords;
            }
            else
            {
                DebugChannel = NullWriter.Instance;
                AnalyticChannel = NullWriter.Instance;
                OperationalChannel = NullWriter.Instance;
            }
        }

        /// <summary>
        /// Dispose method.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                GC.SuppressFinalize(this);

                DebugChannel.Dispose();
                AnalyticChannel.Dispose();
                OperationalChannel.Dispose();
            }
        }

        /// <summary>
        /// Keywords that were set through constructor when object was instantiated.
        /// </summary>
        public PowerShellTraceKeywords Keywords { get; } = PowerShellTraceKeywords.None;

        /// <summary>
        /// Task that was set through constructor.
        /// </summary>
        public PowerShellTraceTask Task { get; set; } = PowerShellTraceTask.None;

        private bool IsEtwSupported
        {
            get
            {
                return Environment.OSVersion.Version.Major >= 6;
            }
        }

        /// <summary>
        /// TraceErrorRecord.
        /// </summary>
        public bool TraceErrorRecord(ErrorRecord errorRecord)
        {
            if (errorRecord != null)
            {
                Exception exception = errorRecord.Exception;
                string innerException = "None";
                if (exception.InnerException != null)
                {
                    innerException = exception.InnerException.Message;
                }

                ErrorCategoryInfo cinfo = errorRecord.CategoryInfo;
                string message = "None";

                if (errorRecord.ErrorDetails != null)
                {
                    message = errorRecord.ErrorDetails.Message;
                }

                return DebugChannel.TraceError(PowerShellTraceEvent.ErrorRecord,
                                               PowerShellTraceOperationCode.Exception, PowerShellTraceTask.None,
                                               message,
                                               cinfo.Category.ToString(), cinfo.Reason, cinfo.TargetName,
                                               errorRecord.FullyQualifiedErrorId,
                                               exception.Message, exception.StackTrace, innerException);
            }
            else
            {
                return DebugChannel.TraceError(PowerShellTraceEvent.ErrorRecord,
                                               PowerShellTraceOperationCode.Exception, PowerShellTraceTask.None,
                                               "NULL errorRecord");
            }
        }

        /// <summary>
        /// TraceException.
        /// </summary>
        public bool TraceException(Exception exception)
        {
            if (exception != null)
            {
                string innerException = "None";
                if (exception.InnerException != null)
                {
                    innerException = exception.InnerException.Message;
                }

                return DebugChannel.TraceError(PowerShellTraceEvent.Exception,
                                               PowerShellTraceOperationCode.Exception, PowerShellTraceTask.None,
                                           exception.Message, exception.StackTrace, innerException);
            }
            else
            {
                return DebugChannel.TraceError(PowerShellTraceEvent.Exception,
                                               PowerShellTraceOperationCode.Exception, PowerShellTraceTask.None,
                                           "NULL exception");
            }
        }

        /// <summary>
        /// TracePowerShellObject.
        /// </summary>
        public bool TracePowerShellObject(PSObject powerShellObject)
        {
            return this.DebugChannel.TraceDebug(PowerShellTraceEvent.PowerShellObject,
                                                PowerShellTraceOperationCode.Method, PowerShellTraceTask.None);
        }

        /// <summary>
        /// TraceJob.
        /// </summary>
        public bool TraceJob(Job job)
        {
            if (job != null)
            {
                return DebugChannel.TraceDebug(PowerShellTraceEvent.Job,
                                               PowerShellTraceOperationCode.Method, PowerShellTraceTask.None,
                                               job.Id.ToString(CultureInfo.InvariantCulture), job.InstanceId.ToString(), job.Name,
                                               job.Location, job.JobStateInfo.State.ToString(),
                                               job.Command);
            }
            else
            {
                return DebugChannel.TraceDebug(PowerShellTraceEvent.Job,
                                               PowerShellTraceOperationCode.Method, PowerShellTraceTask.None,
                                               string.Empty, string.Empty, "NULL job");
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool WriteMessage(string message)
        {
            return DebugChannel.TraceInformational(PowerShellTraceEvent.TraceMessage,
                                            PowerShellTraceOperationCode.None,
                                            PowerShellTraceTask.None, message);
        }

        /// <summary>
        /// </summary>
        /// <param name="message1"></param>
        /// <param name="message2"></param>
        /// <returns></returns>
        public bool WriteMessage(string message1, string message2)
        {
            return DebugChannel.TraceInformational(PowerShellTraceEvent.TraceMessage2,
                                            PowerShellTraceOperationCode.None,
                                            PowerShellTraceTask.None, message1, message2);
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        public bool WriteMessage(string message, Guid instanceId)
        {
            return DebugChannel.TraceInformational(PowerShellTraceEvent.TraceMessageGuid,
                                            PowerShellTraceOperationCode.None,
                                            PowerShellTraceTask.None, message, instanceId);
        }

        /// <summary>
        /// </summary>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <param name="workflowId"></param>
        /// <param name="message"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public void WriteMessage(string className, string methodName, Guid workflowId, string message, params string[] parameters)
        {
            PSEtwLog.LogAnalyticVerbose(PSEventId.Engine_Trace,
                                        PSOpcode.Method, PSTask.None,
                                        PSKeyword.UseAlwaysAnalytic,
                                        className, methodName, workflowId.ToString(),
                                        parameters == null ? message : StringUtil.Format(message, parameters),
                                        string.Empty, // Job
                                        string.Empty, // Activity name
                                        string.Empty, // Activity GUID
                                        string.Empty);
        }

        /// <summary>
        /// </summary>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <param name="workflowId"></param>
        /// <param name="job"></param>
        /// <param name="message"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public void WriteMessage(string className, string methodName, Guid workflowId, Job job, string message, params string[] parameters)
        {
            StringBuilder sb = new StringBuilder();

            if (job != null)
            {
                try
                {
                    sb.AppendLine(StringUtil.Format(EtwLoggingStrings.JobName, job.Name));
                    sb.AppendLine(StringUtil.Format(EtwLoggingStrings.JobId, job.Id.ToString(CultureInfo.InvariantCulture)));
                    sb.AppendLine(StringUtil.Format(EtwLoggingStrings.JobInstanceId, job.InstanceId.ToString()));
                    sb.AppendLine(StringUtil.Format(EtwLoggingStrings.JobLocation, job.Location));
                    sb.AppendLine(StringUtil.Format(EtwLoggingStrings.JobState, job.JobStateInfo.State.ToString()));
                    sb.AppendLine(StringUtil.Format(EtwLoggingStrings.JobCommand, job.Command));
                }
                catch (Exception e)
                {
                    // Exception in 3rd party code should never cause a crash due to tracing. The
                    // Implementation of the property getters could throw.
                    TraceException(e);

                    // If an exception is thrown, make sure the message is not partially formed.
                    sb.Clear();
                    sb.AppendLine(StringUtil.Format(EtwLoggingStrings.JobName, EtwLoggingStrings.NullJobName));
                }
            }
            else
            {
                sb.AppendLine(StringUtil.Format(EtwLoggingStrings.JobName, EtwLoggingStrings.NullJobName));
            }

            PSEtwLog.LogAnalyticVerbose(PSEventId.Engine_Trace,
                                        PSOpcode.Method, PSTask.None,
                                        PSKeyword.UseAlwaysAnalytic,
                                        className, methodName, workflowId.ToString(),
                                        parameters == null ? message : StringUtil.Format(message, parameters),
                                        sb.ToString(),// Job
                                        string.Empty, // Activity name
                                        string.Empty, // Activity GUID
                                        string.Empty);
        }

        /// <summary>
        /// Writes operational scheduled job start message.
        /// </summary>
        /// <param name="args"></param>
        public void WriteScheduledJobStartEvent(params object[] args)
        {
            PSEtwLog.LogOperationalInformation(PSEventId.ScheduledJob_Start,
                                               PSOpcode.Method,
                                               PSTask.ScheduledJob,
                                               PSKeyword.UseAlwaysOperational,
                                               args);
        }

        /// <summary>
        /// Writes operational scheduled job completed message.
        /// </summary>
        /// <param name="args"></param>
        public void WriteScheduledJobCompleteEvent(params object[] args)
        {
            PSEtwLog.LogOperationalInformation(PSEventId.ScheduledJob_Complete,
                                               PSOpcode.Method,
                                               PSTask.ScheduledJob,
                                               PSKeyword.UseAlwaysOperational,
                                               args);
        }

        /// <summary>
        /// Writes operational scheduled job error message.
        /// </summary>
        /// <param name="args"></param>
        public void WriteScheduledJobErrorEvent(params object[] args)
        {
            PSEtwLog.LogOperationalError(PSEventId.ScheduledJob_Error,
                                         PSOpcode.Exception,
                                         PSTask.ScheduledJob,
                                         PSKeyword.UseAlwaysOperational,
                                         args);
        }

        /// <summary>
        /// Writes operational ISE execute script message.
        /// </summary>
        /// <param name="args"></param>
        public void WriteISEExecuteScriptEvent(params object[] args)
        {
            PSEtwLog.LogOperationalInformation(PSEventId.ISEExecuteScript,
                                               PSOpcode.Method,
                                               PSTask.ISEOperation,
                                               PSKeyword.UseAlwaysOperational,
                                               args);
        }

        /// <summary>
        /// Writes operational ISE execute selection message.
        /// </summary>
        /// <param name="args"></param>
        public void WriteISEExecuteSelectionEvent(params object[] args)
        {
            PSEtwLog.LogOperationalInformation(PSEventId.ISEExecuteSelection,
                                               PSOpcode.Method,
                                               PSTask.ISEOperation,
                                               PSKeyword.UseAlwaysOperational,
                                               args);
        }

        /// <summary>
        /// Writes operational ISE stop command message.
        /// </summary>
        /// <param name="args"></param>
        public void WriteISEStopCommandEvent(params object[] args)
        {
            PSEtwLog.LogOperationalInformation(PSEventId.ISEStopCommand,
                                               PSOpcode.Method,
                                               PSTask.ISEOperation,
                                               PSKeyword.UseAlwaysOperational,
                                               args);
        }

        /// <summary>
        /// Writes operational ISE resume debugger message.
        /// </summary>
        /// <param name="args"></param>
        public void WriteISEResumeDebuggerEvent(params object[] args)
        {
            PSEtwLog.LogOperationalInformation(PSEventId.ISEResumeDebugger,
                                               PSOpcode.Method,
                                               PSTask.ISEOperation,
                                               PSKeyword.UseAlwaysOperational,
                                               args);
        }

        /// <summary>
        /// Writes operational ISE stop debugger message.
        /// </summary>
        /// <param name="args"></param>
        public void WriteISEStopDebuggerEvent(params object[] args)
        {
            PSEtwLog.LogOperationalInformation(PSEventId.ISEStopDebugger,
                                               PSOpcode.Method,
                                               PSTask.ISEOperation,
                                               PSKeyword.UseAlwaysOperational,
                                               args);
        }

        /// <summary>
        /// Writes operational ISE debugger step into message.
        /// </summary>
        /// <param name="args"></param>
        public void WriteISEDebuggerStepIntoEvent(params object[] args)
        {
            PSEtwLog.LogOperationalInformation(PSEventId.ISEDebuggerStepInto,
                                               PSOpcode.Method,
                                               PSTask.ISEOperation,
                                               PSKeyword.UseAlwaysOperational,
                                               args);
        }

        /// <summary>
        /// Writes operational ISE debugger step over message.
        /// </summary>
        /// <param name="args"></param>
        public void WriteISEDebuggerStepOverEvent(params object[] args)
        {
            PSEtwLog.LogOperationalInformation(PSEventId.ISEDebuggerStepOver,
                                               PSOpcode.Method,
                                               PSTask.ISEOperation,
                                               PSKeyword.UseAlwaysOperational,
                                               args);
        }

        /// <summary>
        /// Writes operational ISE debugger step out message.
        /// </summary>
        /// <param name="args"></param>
        public void WriteISEDebuggerStepOutEvent(params object[] args)
        {
            PSEtwLog.LogOperationalInformation(PSEventId.ISEDebuggerStepOut,
                                               PSOpcode.Method,
                                               PSTask.ISEOperation,
                                               PSKeyword.UseAlwaysOperational,
                                               args);
        }

        /// <summary>
        /// Writes operational ISE enable all breakpoints message.
        /// </summary>
        /// <param name="args"></param>
        public void WriteISEEnableAllBreakpointsEvent(params object[] args)
        {
            PSEtwLog.LogOperationalInformation(PSEventId.ISEEnableAllBreakpoints,
                                               PSOpcode.Method,
                                               PSTask.ISEOperation,
                                               PSKeyword.UseAlwaysOperational,
                                               args);
        }

        /// <summary>
        /// Writes operational ISE disable all breakpoints message.
        /// </summary>
        /// <param name="args"></param>
        public void WriteISEDisableAllBreakpointsEvent(params object[] args)
        {
            PSEtwLog.LogOperationalInformation(PSEventId.ISEDisableAllBreakpoints,
                                               PSOpcode.Method,
                                               PSTask.ISEOperation,
                                               PSKeyword.UseAlwaysOperational,
                                               args);
        }

        /// <summary>
        /// Writes operational ISE remove all breakpoints message.
        /// </summary>
        /// <param name="args"></param>
        public void WriteISERemoveAllBreakpointsEvent(params object[] args)
        {
            PSEtwLog.LogOperationalInformation(PSEventId.ISERemoveAllBreakpoints,
                                               PSOpcode.Method,
                                               PSTask.ISEOperation,
                                               PSKeyword.UseAlwaysOperational,
                                               args);
        }

        /// <summary>
        /// Writes operational ISE set breakpoint message.
        /// </summary>
        /// <param name="args"></param>
        public void WriteISESetBreakpointEvent(params object[] args)
        {
            PSEtwLog.LogOperationalInformation(PSEventId.ISESetBreakpoint,
                                               PSOpcode.Method,
                                               PSTask.ISEOperation,
                                               PSKeyword.UseAlwaysOperational,
                                               args);
        }

        /// <summary>
        /// Writes operational ISE remove breakpoint message.
        /// </summary>
        /// <param name="args"></param>
        public void WriteISERemoveBreakpointEvent(params object[] args)
        {
            PSEtwLog.LogOperationalInformation(PSEventId.ISERemoveBreakpoint,
                                               PSOpcode.Method,
                                               PSTask.ISEOperation,
                                               PSKeyword.UseAlwaysOperational,
                                               args);
        }

        /// <summary>
        /// Writes operational ISE enable breakpoint message.
        /// </summary>
        /// <param name="args"></param>
        public void WriteISEEnableBreakpointEvent(params object[] args)
        {
            PSEtwLog.LogOperationalInformation(PSEventId.ISEEnableBreakpoint,
                                               PSOpcode.Method,
                                               PSTask.ISEOperation,
                                               PSKeyword.UseAlwaysOperational,
                                               args);
        }

        /// <summary>
        /// Writes operational ISE disable breakpoint message.
        /// </summary>
        /// <param name="args"></param>
        public void WriteISEDisableBreakpointEvent(params object[] args)
        {
            PSEtwLog.LogOperationalInformation(PSEventId.ISEDisableBreakpoint,
                                               PSOpcode.Method,
                                               PSTask.ISEOperation,
                                               PSKeyword.UseAlwaysOperational,
                                               args);
        }

        /// <summary>
        /// Writes operational ISE hit breakpoint message.
        /// </summary>
        /// <param name="args"></param>
        public void WriteISEHitBreakpointEvent(params object[] args)
        {
            PSEtwLog.LogOperationalInformation(PSEventId.ISEHitBreakpoint,
                                               PSOpcode.Method,
                                               PSTask.ISEOperation,
                                               PSKeyword.UseAlwaysOperational,
                                               args);
        }

        /// <summary>
        /// </summary>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <param name="workflowId"></param>
        /// <param name="activityName"></param>
        /// <param name="activityId"></param>
        /// <param name="message"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public void WriteMessage(string className, string methodName, Guid workflowId, string activityName, Guid activityId, string message, params string[] parameters)
        {
            PSEtwLog.LogAnalyticVerbose(PSEventId.Engine_Trace,
                                        PSOpcode.Method, PSTask.None,
                                        PSKeyword.UseAlwaysAnalytic,
                                        className, methodName, workflowId.ToString(),
                                        parameters == null ? message : StringUtil.Format(message, parameters),
                                        string.Empty, // Job
                                        activityName,
                                        activityId.ToString(),
                                        string.Empty);
        }

        /// <summary>
        /// </summary>
        /// <param name="connectionInfo"></param>
        /// <returns></returns>
        public bool TraceWSManConnectionInfo(WSManConnectionInfo connectionInfo)
        {
            return true;
        }

        /// <summary>
        /// Gives access to Debug channel writer.
        /// </summary>
        public BaseChannelWriter DebugChannel { get; }

        /// <summary>
        /// Gives access to analytical channel writer.
        /// </summary>
        public BaseChannelWriter AnalyticChannel { get; }

        /// <summary>
        /// Gives access to operational channel writer.
        /// </summary>
        public BaseChannelWriter OperationalChannel { get; }
    }

    /// <summary>
    /// TraceSourceFactory will return an instance of TraceSource every time GetTraceSource method is called.
    /// </summary>
    public static class PowerShellTraceSourceFactory
    {
        /// <summary>
        /// Returns an instance of BaseChannelWriter.
        /// If the Etw is not supported by the platform it will return NullWriter.Instance
        ///
        /// A Task and a set of Keywords can be specified in the GetTraceSource method (See overloads).
        ///    The supplied task and keywords are used to pass to the Etw provider in case they are
        /// not defined in the manifest file.
        /// </summary>
        public static PowerShellTraceSource GetTraceSource()
        {
            return new PowerShellTraceSource(PowerShellTraceTask.None, PowerShellTraceKeywords.None);
        }

        /// <summary>
        /// Returns an instance of BaseChannelWriter.
        /// If the Etw is not supported by the platform it will return NullWriter.Instance
        ///
        /// A Task and a set of Keywords can be specified in the GetTraceSource method (See overloads).
        ///    The supplied task and keywords are used to pass to the Etw provider in case they are
        /// not defined in the manifest file.
        /// </summary>
        public static PowerShellTraceSource GetTraceSource(PowerShellTraceTask task)
        {
            return new PowerShellTraceSource(task, PowerShellTraceKeywords.None);
        }

        /// <summary>
        /// Returns an instance of BaseChannelWriter.
        /// If the Etw is not supported by the platform it will return NullWriter.Instance
        ///
        /// A Task and a set of Keywords can be specified in the GetTraceSource method (See overloads).
        ///    The supplied task and keywords are used to pass to the Etw provider in case they are
        /// not defined in the manifest file.
        /// </summary>
        public static PowerShellTraceSource GetTraceSource(PowerShellTraceTask task, PowerShellTraceKeywords keywords)
        {
            return new PowerShellTraceSource(task, keywords);
        }
    }
    // pragma warning restore 16001,16003
}

#endif
