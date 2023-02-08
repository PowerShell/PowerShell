// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Internal
{
    /// <summary>
    /// Defines enumerations for the keywords.
    /// </summary>
    [Flags]
    internal enum PSKeyword : ulong
    {
        Runspace = 0x1,
        Pipeline = 0x2,
        Protocol = 0x4,
        Transport = 0x8,
        Host = 0x10,
        Cmdlets = 0x20,
        Serializer = 0x40,
        Session = 0x80,
        ManagedPlugin = 0x100,
        UseAlwaysOperational = 0x8000000000000000,
        UseAlwaysAnalytic = 0x4000000000000000,
    }

    /// <summary>
    /// Define enumerations for levels.
    /// </summary>
    internal enum PSLevel : byte
    {
        LogAlways = 0x0,
        Critical = 0x1,
        Error = 0x2,
        Warning = 0x3,
        Informational = 0x4,
        Verbose = 0x5,
        Debug = 0x14
    }

    /// <summary>
    /// Defines enumerations for op codes.
    /// </summary>
    internal enum PSOpcode : byte
    {
        WinStart = 0x1,
        WinStop = 0x2,
        Open = 0xA,
        Close = 0xB,
        Connect = 0xC,
        Disconnect = 0xD,
        Negotiate = 0xE,
        Create = 0xF,
        Constructor = 0x10,
        Dispose = 0x11,
        EventHandler = 0x12,
        Exception = 0x13,
        Method = 0x14,
        Send = 0x15,
        Receive = 0x16,
        Rehydration = 0x17,
        SerializationSettings = 0x18,
        ShuttingDown = 0x19,
    }

    /// <summary>
    /// Defines enumerations for event ids.
    /// </summary>
    /// <remarks>add an entry for a new event that you
    /// add to the manifest. Set it to the same value
    /// that was set in the manifest</remarks>
    internal enum PSEventId : int
    {
        HostNameResolve = 0x1001,
        SchemeResolve = 0x1002,
        ShellResolve = 0x1003,
        RunspaceConstructor = 0x2001,
        RunspacePoolConstructor = 0x2002,
        RunspacePoolOpen = 0x2003,
        OperationalTransferEventRunspacePool = 0x2004,
        RunspaceStateChange = 0x2005,
        RetrySessionCreation = 0x2006,
        Port = 0x2F01,
        AppName = 0x2F02,
        ComputerName = 0x2F03,
        Scheme = 0x2F04,
        TestAnalytic = 0x2F05,
        WSManConnectionInfoDump = 0x2F06,
        AnalyticTransferEventRunspacePool = 0x2F07,

        // Start: Transport related events
        TransportReceivedObject = 0x8001,
        TransportSendingData = 0x8002,
        TransportReceivedData = 0x8003,
        AppDomainUnhandledException_Analytic = 0x8007,
        TransportError_Analytic = 0x8008,
        AppDomainUnhandledException = 0x8009,
        TransportError = 0x8010,
        WSManCreateShell = 0x8011,
        WSManCreateShellCallbackReceived = 0x8012,
        WSManCloseShell = 0x8013,
        WSManCloseShellCallbackReceived = 0x8014,
        WSManSendShellInputEx = 0x8015,
        WSManSendShellInputExCallbackReceived = 0x8016,
        WSManReceiveShellOutputEx = 0x8017,
        WSManReceiveShellOutputExCallbackReceived = 0x8018,
        WSManCreateCommand = 0x8019,
        WSManCreateCommandCallbackReceived = 0x8020,
        WSManCloseCommand = 0x8021,
        WSManCloseCommandCallbackReceived = 0x8022,
        WSManSignal = 0x8023,
        WSManSignalCallbackReceived = 0x8024,
        URIRedirection = 0x8025,
        ServerSendData = 0x8051,
        ServerCreateRemoteSession = 0x8052,
        ReportContext = 0x8053,
        ReportOperationComplete = 0x8054,
        ServerCreateCommandSession = 0x8055,
        ServerStopCommand = 0x8056,
        ServerReceivedData = 0x8057,
        ServerClientReceiveRequest = 0x8058,
        ServerCloseOperation = 0x8059,
        LoadingPSCustomShellAssembly = 0x8061,
        LoadingPSCustomShellType = 0x8062,
        ReceivedRemotingFragment = 0x8063,
        SentRemotingFragment = 0x8064,
        WSManPluginShutdown = 0x8065,
        // End: Transport related events

        // Start: Serialization related events
        Serializer_RehydrationSuccess = 0x7001,
        Serializer_RehydrationFailure = 0x7002,
        Serializer_DepthOverride = 0x7003,
        Serializer_ModeOverride = 0x7004,
        Serializer_ScriptPropertyWithoutRunspace = 0x7005,
        Serializer_PropertyGetterFailed = 0x7006,
        Serializer_EnumerationFailed = 0x7007,
        Serializer_ToStringFailed = 0x7008,
        Serializer_MaxDepthWhenSerializing = 0x700A,
        Serializer_XmlExceptionWhenDeserializing = 0x700B,
        Serializer_SpecificPropertyMissing = 0x700C,
        // End: Serialization related events

        // Start: Perftrack related events
        Perftrack_ConsoleStartupStart = 0xA001,
        Perftrack_ConsoleStartupStop = 0xA002,
        // End: Preftrack related events

        Command_Health = 0x1004,
        Engine_Health = 0x1005,
        Provider_Health = 0x1006,
        Pipeline_Detail = 0x1007,
        ScriptBlock_Compile_Detail = 0x1008,
        ScriptBlock_Invoke_Start_Detail = 0x1009,
        ScriptBlock_Invoke_Complete_Detail = 0x100A,
        Command_Lifecycle = 0x1F01,
        Engine_Lifecycle = 0x1F02,
        Provider_Lifecycle = 0x1F03,
        Settings = 0x1F04,
        Engine_Trace = 0x1F06,
        Amsi_Init = 0x4001,
        WDAC_Query = 0x4002,

        // Experimental Features
        ExperimentalFeature_InvalidName = 0x3001,
        ExperimentalFeature_ReadConfig_Error = 0x3002,

        // Scheduled Jobs
        ScheduledJob_Start = 0xD001,
        ScheduledJob_Complete = 0xD002,
        ScheduledJob_Error = 0xD003,

        // PowerShell IPC Named Pipe Connection
        NamedPipeIPC_ServerListenerStarted = 0xD100,
        NamedPipeIPC_ServerListenerEnded = 0xD101,
        NamedPipeIPC_ServerListenerError = 0xD102,
        NamedPipeIPC_ServerConnect = 0xD103,
        NamedPipeIPC_ServerDisconnect = 0xD104,

        // Start: ISE related events
        ISEExecuteScript = 0x6001,
        ISEExecuteSelection = 0x6002,
        ISEStopCommand = 0x6003,
        ISEResumeDebugger = 0x6004,
        ISEStopDebugger = 0x6005,
        ISEDebuggerStepInto = 0x6006,
        ISEDebuggerStepOver = 0x6007,
        ISEDebuggerStepOut = 0x6008,
        ISEEnableAllBreakpoints = 0x6010,
        ISEDisableAllBreakpoints = 0x6011,
        ISERemoveAllBreakpoints = 0x6012,
        ISESetBreakpoint = 0x6013,
        ISERemoveBreakpoint = 0x6014,
        ISEEnableBreakpoint = 0x6015,
        ISEDisableBreakpoint = 0x6016,
        ISEHitBreakpoint = 0x6017,
        // End: ISE related events
    }

    /// <summary>
    /// Defines enumerations for channels.
    /// </summary>
    /// <remarks>
    /// On Windows, PSChannel is the numeric channel id value.
    /// On Non-Windows, PSChannel is used to filter events and
    /// the underlying channel bitmask values are used instead.
    /// The bit values are the same as used on Windows.
    /// </remarks>
#if UNIX
    [Flags]
    internal enum PSChannel : byte
    {
        Operational = 0x80,
        Analytic = 0x40
    }
#else
    internal enum PSChannel : byte
    {
        Operational = 0x10,
        Analytic = 0x11
    }
#endif

    /// <summary>
    /// Defines enumerations for tasks.
    /// </summary>
    internal enum PSTask : int
    {
        None = 0x0,
        CreateRunspace = 0x1,
        ExecuteCommand = 0x2,
        Serialization = 0x3,
        PowershellConsoleStartup = 0x4,
        EngineStart = 0x64,
        EngineStop = 0x65,
        CommandStart = 0x66,
        CommandStop = 0x67,
        ProviderStart = 0x68,
        ProviderStop = 0x69,
        ExecutePipeline = 0x6A,
        ExperimentalFeature = 0x6B,
        ScheduledJob = 0x6E,
        NamedPipe = 0x6F,
        ISEOperation = 0x78,
        Amsi = 0X82,
        WDAC = 0x83
    }

    /// <summary>
    /// Defines enumerations for version.
    /// </summary>
    /// <remarks>all messages in V2 timeframe
    /// should be of version 1</remarks>
    internal enum PSEventVersion : byte
    {
        One = 0x1,
    }

    /// <summary>
    /// Describes a binary blob to be used as a data item for ETW.
    /// </summary>
    internal sealed class PSETWBinaryBlob
    {
        public PSETWBinaryBlob(byte[] blob, int offset, int length)
        {
            this.blob = blob;
            this.offset = offset;
            this.length = length;
        }

        public readonly byte[] blob;
        public readonly int offset;
        public readonly int length;
    }
}
