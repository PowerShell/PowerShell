// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Internal;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// This enum defines the error message ids used by the resource manager to get
    /// localized messages.
    ///
    /// Related error ids are organized in a pre-defined range of values.
    /// </summary>
    internal enum PSRemotingErrorId : uint
    {
        // OS related 1-9
        DefaultRemotingExceptionMessage = 0,
        OutOfMemory = 1,

        // Pipeline related range: 10-99
        PipelineIdsDoNotMatch = 10,
        PipelineNotFoundOnServer = 11,
        PipelineStopped = 12,

        // Runspace, Host, UI and RawUI related range: 200-299
        RunspaceAlreadyExists = 200,
        RunspaceIdsDoNotMatch = 201,
        RemoteRunspaceOpenFailed = 202,
        RunspaceCannotBeFound = 203,
        ResponsePromptIdCannotBeFound = 204,
        RemoteHostCallFailed = 205,
        RemoteHostMethodNotImplemented = 206,
        RemoteHostDataEncodingNotSupported = 207,
        RemoteHostDataDecodingNotSupported = 208,
        NestedPipelineNotSupported = 209,
        RelativeUriForRunspacePathNotSupported = 210,
        RemoteHostDecodingFailed = 211,
        MustBeAdminToOverrideThreadOptions = 212,
        RemoteHostPromptForCredentialModifiedCaption = 213,
        RemoteHostPromptForCredentialModifiedMessage = 214,
        RemoteHostReadLineAsSecureStringPrompt = 215,
        RemoteHostGetBufferContents = 216,
        RemoteHostPromptSecureStringPrompt = 217,
        WinPERemotingNotSupported = 218,

        // reserved range: 300-399

        // Encoding/Decoding and fragmentation related range: 400-499
        ReceivedUnsupportedRemoteHostCall = 400,
        ReceivedUnsupportedAction = 401,
        ReceivedUnsupportedDataType = 402,
        MissingDestination = 403,
        MissingTarget = 404,
        MissingRunspaceId = 405,
        MissingDataType = 406,
        MissingCallId = 407,
        MissingMethodName = 408,
        MissingIsStartFragment = 409,
        MissingProperty = 410,
        ObjectIdsNotMatching = 411,
        FragmentIdsNotInSequence = 412,
        ObjectIsTooBig = 413,
        MissingIsEndFragment = 414,
        DeserializedObjectIsNull = 415,
        BlobLengthNotInRange = 416,
        DecodingErrorForErrorRecord = 417,
        DecodingErrorForPipelineStateInfo = 418,
        DecodingErrorForRunspaceStateInfo = 419,
        ReceivedUnsupportedRemotingTargetInterfaceType = 420,
        UnknownTargetClass = 421,
        MissingTargetClass = 422,
        DecodingErrorForRunspacePoolStateInfo = 423,
        DecodingErrorForMinRunspaces = 424,
        DecodingErrorForMaxRunspaces = 425,
        DecodingErrorForPowerShellStateInfo = 426,
        DecodingErrorForThreadOptions = 427,
        CantCastPropertyToExpectedType = 428,
        CantCastRemotingDataToPSObject = 429,
        CantCastCommandToPSObject = 430,
        CantCastParameterToPSObject = 431,
        ObjectIdCannotBeLessThanZero = 432,
        NotEnoughHeaderForRemoteDataObject = 433,

        // reserved range: 500-599

        // Remote Session related range: 600-699
        RemotingDestinationNotForMe = 600,
        ClientNegotiationTimeout = 601,
        ClientNegotiationFailed = 602,
        ServerRequestedToCloseSession = 603,
        ServerNegotiationFailed = 604,
        ServerNegotiationTimeout = 605,
        ClientRequestedToCloseSession = 606,
        FatalErrorCausingClose = 607,
        ClientKeyExchangeFailed = 608,
        ServerKeyExchangeFailed = 609,
        ClientNotFoundCapabilityProperties = 610,
        ServerNotFoundCapabilityProperties = 611,

        // reserved range: 700-799

        // Transport related range: 800-899

        ConnectFailed = 801,
        CloseIsCalled = 802,
        ForceClosed = 803,
        CloseFailed = 804,
        CloseCompleted = 805,
        UnsupportedWaitHandleType = 806,
        ReceivedDataStreamIsNotStdout = 807,
        StdInIsNotOpen = 808,
        NativeWriteFileFailed = 809,
        NativeReadFileFailed = 810,
        InvalidSchemeValue = 811,
        ClientReceiveFailed = 812,
        ClientSendFailed = 813,
        CommandHandleIsNull = 814,
        StdInCannotBeSetToNoWait = 815,
        PortIsOutOfRange = 816,
        ServerProcessExited = 817,
        CannotGetStdInHandle = 818,
        CannotGetStdOutHandle = 819,
        CannotGetStdErrHandle = 820,
        CannotSetStdInHandle = 821,
        CannotSetStdOutHandle = 822,
        CannotSetStdErrHandle = 823,
        InvalidConfigurationName = 824,
        ConnectSkipCheckFailed = 825,
        // Error codes added to support new WSMan Fan-In Model API
        CreateSessionFailed = 851,
        CreateExFailed = 853,
        ConnectExCallBackError = 854,
        SendExFailed = 855,
        SendExCallBackError = 856,
        ReceiveExFailed = 857,
        ReceiveExCallBackError = 858,
        RunShellCommandExFailed = 859,
        RunShellCommandExCallBackError = 860,
        CommandSendExFailed = 861,
        CommandSendExCallBackError = 862,
        CommandReceiveExFailed = 863,
        CommandReceiveExCallBackError = 864,
        CloseExCallBackError = 866,
        // END: Error codes added to support new WSMan Fan-In Model API
        // BEGIN: Error IDs introduced for URI redirection
        RedirectedURINotWellFormatted = 867,
        URIEndPointNotResolved = 868,
        // END: Error IDs introduced for URI redirection
        // BEGIN: Error IDs introduced for Quota Management
        ReceivedObjectSizeExceededMaximumClient = 869,
        ReceivedDataSizeExceededMaximumClient = 870,
        ReceivedObjectSizeExceededMaximumServer = 871,
        ReceivedDataSizeExceededMaximumServer = 872,
        // END: Error IDs introduced for Quota Management
        // BEGIN: Error IDs introduced for startup script
        StartupScriptThrewTerminatingError = 873,
        // END: Error IDs introduced for startup script
        TroubleShootingHelpTopic = 874,
        // BEGIN: Error IDs introduced for disconnect/reconnect
        DisconnectShellExFailed = 875,
        DisconnectShellExCallBackErrr = 876,
        ReconnectShellExFailed = 877,
        ReconnectShellExCallBackErrr = 878,
        // END: Error IDs introduced for disconnect/reconnect
        // Cmdlets related range: 900-999
        RemoteRunspaceInfoHasDuplicates = 900,
        RemoteRunspaceInfoLimitExceeded = 901,
        RemoteRunspaceOpenUnknownState = 902,
        UriSpecifiedNotValid = 903,
        RemoteRunspaceClosed = 904,
        RemoteRunspaceNotAvailableForSpecifiedComputer = 905,
        RemoteRunspaceNotAvailableForSpecifiedRunspaceId = 906,
        StopPSJobWhatIfTarget = 907,
        InvalidJobStateGeneral = 909,
        JobWithSpecifiedNameNotFound = 910,
        JobWithSpecifiedInstanceIdNotFound = 911,
        JobWithSpecifiedSessionIdNotFound = 912,
        JobWithSpecifiedNameNotCompleted = 913,
        JobWithSpecifiedSessionIdNotCompleted = 914,
        JobWithSpecifiedInstanceIdNotCompleted = 915,
        RemovePSJobWhatIfTarget = 916,
        ComputerNameParamNotSupported = 917,
        RunspaceParamNotSupported = 918,
        RemoteRunspaceNotAvailableForSpecifiedName = 919,
        RemoteRunspaceNotAvailableForSpecifiedSessionId = 920,
        ItemNotFoundInRepository = 921,
        CannotRemoveJob = 922,
        NewRunspaceAmbiguousAuthentication = 923,
        WildCardErrorFilePathParameter = 924,
        FilePathNotFromFileSystemProvider = 925,
        FilePathShouldPS1Extension = 926,
        PSSessionConfigurationName = 927,
        PSSessionAppName = 928,
        // Custom Shell commands
        CSCDoubleParameterOutOfRange = 929,
        URIRedirectionReported = 930,
        NoMoreInputWrites = 931,
        InvalidComputerName = 932,
        ProxyAmbiguousAuthentication = 933,
        ProxyCredentialWithoutAccess = 934,

        // Start-PSSession related error codes.
        PushedRunspaceMustBeOpen = 951,
        HostDoesNotSupportPushRunspace = 952,
        RemoteRunspaceHasMultipleMatchesForSpecifiedRunspaceId = 953,
        RemoteRunspaceHasMultipleMatchesForSpecifiedSessionId = 954,
        RemoteRunspaceHasMultipleMatchesForSpecifiedName = 955,
        RemoteRunspaceDoesNotSupportPushRunspace = 956,
        HostInNestedPrompt = 957,
        InvalidVMId = 959,
        InvalidVMNameNoVM = 960,
        InvalidVMNameMultipleVM = 961,
        HyperVModuleNotAvailable = 962,
        InvalidUsername = 963,
        InvalidCredential = 964,
        VMSessionConnectFailed = 965,
        InvalidContainerId = 966,
        CannotCreateProcessInContainer = 967,
        CannotTerminateProcessInContainer = 968,
        ContainersFeatureNotEnabled = 969,
        RemoteSessionHyperVSocketServerConstructorFailure = 970,
        ContainerSessionConnectFailed = 973,
        RemoteSessionHyperVSocketClientConstructorSetSocketOptionFailure = 974,
        InvalidVMState = 975,

        // Invoke-Command related error codes.
        InvalidVMIdNotSingle = 981,
        InvalidVMNameNotSingle = 982,

        // SessionState Description related messages
        WsmanMaxRedirectionCountVariableDescription = 1001,
        PSDefaultSessionOptionDescription = 1002,
        PSSenderInfoDescription = 1004,

        // IPC for Background jobs related errors: 2000
        IPCUnknownNodeType = 2001,
        IPCInsufficientDataforElement = 2002,
        IPCWrongAttributeCountForDataElement = 2003,
        IPCOnlyTextExpectedInDataElement = 2004,
        IPCWrongAttributeCountForElement = 2005,
        IPCUnknownElementReceived = 2006,
        IPCSupportsOnlyDefaultAuth = 2007,
        IPCWowComponentNotPresent = 2008,
        IPCServerProcessReportedError = 2100,
        IPCServerProcessExited = 2101,
        IPCErrorProcessingServerData = 2102,
        IPCUnknownCommandGuid = 2103,
        IPCNoSignalForSession = 2104,
        IPCSignalTimedOut = 2105,
        IPCCloseTimedOut = 2106,
        IPCExceptionLaunchingProcess = 2107,
    }

    /// <summary>
    /// This static class defines the resource base name used by remoting errors.
    /// It also provides a convenience method to get the localized strings.
    /// </summary>
    internal static class PSRemotingErrorInvariants
    {
        /// <summary>
        /// This method is a convenience method to retrieve the localized string.
        /// </summary>
        /// <param name="resourceString">
        /// This parameter holds the string in the resource file.
        /// </param>
        /// <param name="args">
        /// Optional parameters required by the resource string formatting information.
        /// </param>
        /// <returns>
        /// The formatted localized string.
        /// </returns>
        internal static string FormatResourceString(string resourceString, params object[] args)
        {
            string resourceFormatedString = StringUtil.Format(resourceString, args);

            return resourceFormatedString;
        }
    }

    /// <summary>
    /// This exception is used by remoting code to indicated a data structure handler related error.
    /// </summary>
    public class PSRemotingDataStructureException : RuntimeException
    {
        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PSRemotingDataStructureException()
            : base(PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.DefaultRemotingExceptionMessage, typeof(PSRemotingDataStructureException).FullName))
        {
            SetDefaultErrorRecord();
        }

        /// <summary>
        /// This constructor takes a localized string as the error message.
        /// </summary>
        /// <param name="message">
        /// A localized string as an error message.
        /// </param>
        public PSRemotingDataStructureException(string message)
            : base(message)
        {
            SetDefaultErrorRecord();
        }

        /// <summary>
        /// This constructor takes a localized string as the error message, and an inner exception.
        /// </summary>
        /// <param name="message">
        /// A localized string as an error message.
        /// </param>
        /// <param name="innerException">
        /// Inner exception.
        /// </param>
        public PSRemotingDataStructureException(string message, Exception innerException)
            : base(message, innerException)
        {
            SetDefaultErrorRecord();
        }

        /// <summary>
        /// This constructor takes an error id and optional parameters.
        /// </summary>
        /// <param name="resourceString">
        /// The resource string in the base resource file.
        /// </param>
        /// <param name="args">
        /// Optional parameters required to format the resource string.
        /// </param>
        internal PSRemotingDataStructureException(string resourceString, params object[] args)
            : base(PSRemotingErrorInvariants.FormatResourceString(resourceString, args))
        {
            SetDefaultErrorRecord();
        }

        /// <summary>
        /// This constructor takes an inner exception and an error id.
        /// </summary>
        /// <param name="innerException">
        /// Inner exception.
        /// </param>
        /// <param name="resourceString">
        /// The resource string in the base resource file.
        /// </param>
        /// <param name="args">
        /// Optional parameters required to format the resource string.
        /// </param>
        internal PSRemotingDataStructureException(Exception innerException, string resourceString, params object[] args)
            : base(PSRemotingErrorInvariants.FormatResourceString(resourceString, args), innerException)
        {
            SetDefaultErrorRecord();
        }

        /// <summary>
        /// This constructor is required by serialization.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected PSRemotingDataStructureException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }

        #endregion Constructors

        /// <summary>
        /// Set the default ErrorRecord.
        /// </summary>
        private void SetDefaultErrorRecord()
        {
            SetErrorCategory(ErrorCategory.ResourceUnavailable);
            SetErrorId(typeof(PSRemotingDataStructureException).FullName);
        }
    }

    /// <summary>
    /// This exception is used by remoting code to indicate an error condition in network operations.
    /// </summary>
    public class PSRemotingTransportException : RuntimeException
    {
        private int _errorCode;
        private string _transportMessage;

        #region Constructors

        /// <summary>
        /// This is the default constructor.
        /// </summary>
        public PSRemotingTransportException()
            : base(PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.DefaultRemotingExceptionMessage, typeof(PSRemotingTransportException).FullName))
        {
            SetDefaultErrorRecord();
        }

        /// <summary>
        /// This constructor takes a localized error message.
        /// </summary>
        /// <param name="message">
        /// A localized error message.
        /// </param>
        public PSRemotingTransportException(string message)
            : base(message)
        {
            SetDefaultErrorRecord();
        }

        /// <summary>
        /// This constructor takes a localized message and an inner exception.
        /// </summary>
        /// <param name="message">
        /// Localized error message.
        /// </param>
        /// <param name="innerException">
        /// Inner exception.
        /// </param>
        public PSRemotingTransportException(string message, Exception innerException)
            : base(message, innerException)
        {
            SetDefaultErrorRecord();
        }

        /// <summary>
        /// This constructor takes an error id and optional parameters.
        /// </summary>
        /// <param name="errorId">
        /// The error id in the base resource file.
        /// </param>
        /// <param name="resourceString">
        /// The resource string in the base resource file.
        /// </param>
        /// <param name="args">
        /// Optional parameters required to format the resource string.
        /// </param>
        internal PSRemotingTransportException(PSRemotingErrorId errorId, string resourceString, params object[] args)
            : base(PSRemotingErrorInvariants.FormatResourceString(resourceString, args))
        {
            SetDefaultErrorRecord();
            _errorCode = (int)errorId;
        }

        /// <summary>
        /// This constructor takes an inner exception and an error id.
        /// </summary>
        /// <param name="innerException">
        /// Inner exception.
        /// </param>
        /// <param name="resourceString">
        /// The resource string in the base resource file.
        /// </param>
        /// <param name="args">
        /// Optional parameters required to format the resource string.
        /// </param>
        internal PSRemotingTransportException(Exception innerException, string resourceString, params object[] args)
            : base(PSRemotingErrorInvariants.FormatResourceString(resourceString, args), innerException)
        {
            SetDefaultErrorRecord();
        }

        /// <summary>
        /// This constructor is required by serialization.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        /// <exception cref="ArgumentNullException">
        /// 1. info is null.
        /// </exception>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected PSRemotingTransportException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }

        #endregion Constructors

        /// <summary>
        /// Set the default ErrorRecord.
        /// </summary>
        protected void SetDefaultErrorRecord()
        {
            SetErrorCategory(ErrorCategory.ResourceUnavailable);
            SetErrorId(typeof(PSRemotingDataStructureException).FullName);
        }

        /// <summary>
        /// The error code from native library API call.
        /// </summary>
        public int ErrorCode
        {
            get
            {
                return _errorCode;
            }

            set
            {
                _errorCode = value;
            }
        }

        /// <summary>
        /// This the message from the native transport layer.
        /// </summary>
        public string TransportMessage
        {
            get
            {
                return _transportMessage;
            }

            set
            {
                _transportMessage = value;
            }
        }
    }

    /// <summary>
    /// This exception is used by PowerShell's remoting infrastructure to notify a URI redirection
    /// exception.
    /// </summary>
    public class PSRemotingTransportRedirectException : PSRemotingTransportException
    {
        #region Constructor
        /// <summary>
        /// This is the default constructor.
        /// </summary>
        public PSRemotingTransportRedirectException()
            : base(PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.DefaultRemotingExceptionMessage,
             typeof(PSRemotingTransportRedirectException).FullName))
        {
            SetDefaultErrorRecord();
        }

        /// <summary>
        /// This constructor takes a localized error message.
        /// </summary>
        /// <param name="message">
        /// A localized error message.
        /// </param>
        public PSRemotingTransportRedirectException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// This constructor takes a localized message and an inner exception.
        /// </summary>
        /// <param name="message">
        /// Localized error message.
        /// </param>
        /// <param name="innerException">
        /// Inner exception.
        /// </param>
        public PSRemotingTransportRedirectException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// This constructor takes an inner exception and an error id.
        /// </summary>
        /// <param name="innerException">
        /// Inner exception.
        /// </param>
        /// <param name="resourceString">
        /// The resource string in the base resource file.
        /// </param>
        /// <param name="args">
        /// Optional parameters required to format the resource string.
        /// </param>
        internal PSRemotingTransportRedirectException(Exception innerException, string resourceString, params object[] args)
            : base(innerException, resourceString, args)
        {
        }

        /// <summary>
        /// This constructor is required by serialization.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        /// <exception cref="ArgumentNullException">
        /// 1. info is null.
        /// </exception>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected PSRemotingTransportRedirectException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// This constructor takes an redirect URI, error id and optional parameters.
        /// </summary>
        /// <param name="redirectLocation">
        /// String specifying a redirect location.
        /// </param>
        /// <param name="errorId">
        /// The error id in the base resource file.
        /// </param>
        /// <param name="resourceString">
        /// The resource string in the base resource file.
        /// </param>
        /// <param name="args">
        /// Optional parameters required to format the resource string.
        /// </param>
        internal PSRemotingTransportRedirectException(string redirectLocation, PSRemotingErrorId errorId, string resourceString, params object[] args)
            : base(errorId, resourceString, args)
        {
            RedirectLocation = redirectLocation;
        }

        #endregion

        #region Properties
        /// <summary>
        /// String specifying a redirect location.
        /// </summary>
        public string RedirectLocation { get; }

        #endregion
    }

    /// <summary>
    /// This exception is used by PowerShell Direct errors.
    /// </summary>
    public class PSDirectException : RuntimeException
    {
        #region Constructor

        /// <summary>
        /// This constructor takes a localized string as the error message.
        /// </summary>
        /// <param name="message">
        /// A localized string as an error message.
        /// </param>
        public PSDirectException(string message)
            : base(message)
        {
        }

        #endregion Constructor
    }
}
