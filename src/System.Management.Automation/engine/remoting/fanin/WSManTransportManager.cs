// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/*
 * Common file that contains implementation for both server and client transport
 * managers based on WSMan protocol.
 *
 */

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting.Server;
using System.Management.Automation.Runspaces.Internal;
using System.Management.Automation.Tracing;
using System.Runtime.InteropServices;
#if !UNIX
using System.Security.Principal;
#endif
using System.Xml;
using System.Threading;

using PSRemotingCryptoHelper = System.Management.Automation.Internal.PSRemotingCryptoHelper;
using WSManConnectionInfo = System.Management.Automation.Runspaces.WSManConnectionInfo;
using RunspaceConnectionInfo = System.Management.Automation.Runspaces.RunspaceConnectionInfo;
using AuthenticationMechanism = System.Management.Automation.Runspaces.AuthenticationMechanism;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting.Client
{
    /// <summary>
    /// WSMan TransportManager related utils.
    /// </summary>
    internal static class WSManTransportManagerUtils
    {
        #region Static Data

        // Fully qualified error Id modifiers based on transport (WinRM) error codes.
        private static readonly Dictionary<int, string> s_transportErrorCodeToFQEID = new Dictionary<int, string>()
        {
            {WSManNativeApi.ERROR_WSMAN_ACCESS_DENIED, "AccessDenied"},
            {WSManNativeApi.ERROR_WSMAN_OUTOF_MEMORY, "ServerOutOfMemory"},
            {WSManNativeApi.ERROR_WSMAN_NETWORKPATH_NOTFOUND, "NetworkPathNotFound"},
            {WSManNativeApi.ERROR_WSMAN_COMPUTER_NOTFOUND, "ComputerNotFound"},
            {WSManNativeApi.ERROR_WSMAN_AUTHENTICATION_FAILED, "AuthenticationFailed"},
            {WSManNativeApi.ERROR_WSMAN_LOGON_FAILURE, "LogonFailure"},
            {WSManNativeApi.ERROR_WSMAN_IMPROPER_RESPONSE, "ImproperResponse"},
            {WSManNativeApi.ERROR_WSMAN_INCORRECT_PROTOCOLVERSION, "IncorrectProtocolVersion"},
            {WSManNativeApi.ERROR_WSMAN_SENDDATA_CANNOT_COMPLETE, "WinRMOperationTimeout"},
            {WSManNativeApi.ERROR_WSMAN_URL_NOTAVAILABLE, "URLNotAvailable"},
            {WSManNativeApi.ERROR_WSMAN_SENDDATA_CANNOT_CONNECT, "CannotConnect"},
            {WSManNativeApi.ERROR_WSMAN_INVALID_RESOURCE_URI, "InvalidResourceUri"},
            {WSManNativeApi.ERROR_WSMAN_INUSE_CANNOT_RECONNECT, "CannotConnectAlreadyConnected"},
            {WSManNativeApi.ERROR_WSMAN_INVALID_AUTHENTICATION, "InvalidAuthentication"},
            {WSManNativeApi.ERROR_WSMAN_SHUTDOWN_INPROGRESS, "ShutDownInProgress"},
            {WSManNativeApi.ERROR_WSMAN_CANNOT_CONNECT_INVALID, "CannotConnectInvalidOperation"},
            {WSManNativeApi.ERROR_WSMAN_CANNOT_CONNECT_MISMATCH, "CannotConnectMismatchSessions"},
            {WSManNativeApi.ERROR_WSMAN_CANNOT_CONNECT_RUNASFAILED, "CannotConnectRunAsFailed"},
            {WSManNativeApi.ERROR_WSMAN_CREATEFAILED_INVALIDNAME, "SessionCreateFailedInvalidName"},
            {WSManNativeApi.ERROR_WSMAN_TARGETSESSION_DOESNOTEXIST, "CannotConnectTargetSessionDoesNotExist"},
            {WSManNativeApi.ERROR_WSMAN_REMOTESESSION_DISALLOWED, "RemoteSessionDisallowed"},
            {WSManNativeApi.ERROR_WSMAN_REMOTECONNECTION_DISALLOWED, "RemoteConnectionDisallowed"},
            {WSManNativeApi.ERROR_WSMAN_INVALID_RESOURCE_URI2, "InvalidResourceUri"},
            {WSManNativeApi.ERROR_WSMAN_CORRUPTED_CONFIG, "CorruptedWinRMConfig"},
            {WSManNativeApi.ERROR_WSMAN_OPERATION_ABORTED, "WinRMOperationAborted"},
            {WSManNativeApi.ERROR_WSMAN_URI_LIMIT, "URIExceedsMaxAllowedSize"},
            {WSManNativeApi.ERROR_WSMAN_CLIENT_KERBEROS_DISABLED, "ClientKerberosDisabled"},
            {WSManNativeApi.ERROR_WSMAN_SERVER_NOTTRUSTED, "ServerNotTrusted"},
            {WSManNativeApi.ERROR_WSMAN_WORKGROUP_NO_KERBEROS, "WorkgroupCannotUseKerberos"},
            {WSManNativeApi.ERROR_WSMAN_EXPLICIT_CREDENTIALS_REQUIRED, "ExplicitCredentialsRequired"},
            {WSManNativeApi.ERROR_WSMAN_REDIRECT_LOCATION_INVALID, "RedirectLocationInvalid"},
            {WSManNativeApi.ERROR_WSMAN_REDIRECT_REQUESTED, "RedirectInformationRequired"},
            {WSManNativeApi.ERROR_WSMAN_BAD_METHOD, "WinRMOperationNotSupportedOnServer"},
            {WSManNativeApi.ERROR_WSMAN_HTTP_SERVICE_UNAVAILABLE, "CannotConnectWinRMService"},
            {WSManNativeApi.ERROR_WSMAN_HTTP_SERVICE_ERROR, "WinRMHttpError"},
            {WSManNativeApi.ERROR_WSMAN_TARGET_UNKNOWN, "TargetUnknown"},
            {WSManNativeApi.ERROR_WSMAN_CANNOTUSE_IP, "CannotUseIPAddress"}
        };

        #endregion

        #region Helper Methods

        /// <summary>
        /// Constructs a WSManTransportErrorOccuredEventArgs instance from the supplied data.
        /// </summary>
        /// <param name="wsmanAPIHandle">
        /// WSMan API handle to use to get error messages from WSMan error id(s)
        /// </param>
        /// <param name="wsmanSessionTM">
        /// Session Transportmanager to use to get error messages (for redirect)
        /// </param>
        /// <param name="errorStruct">
        /// Error structure supplied by callbacks from WSMan API
        /// </param>
        /// <param name="transportMethodReportingError">
        /// The transport method call that reported this error.
        /// </param>
        /// <param name="resourceString">
        /// resource string that holds the message.
        /// </param>
        /// <param name="resourceArgs">
        /// Arguments to pass to the resource
        /// </param>
        /// <returns>
        /// An instance of WSManTransportErrorOccuredEventArgs
        /// </returns>
        internal static TransportErrorOccuredEventArgs ConstructTransportErrorEventArgs(IntPtr wsmanAPIHandle,
            WSManClientSessionTransportManager wsmanSessionTM,
            WSManNativeApi.WSManError errorStruct,
            TransportMethodEnum transportMethodReportingError,
            string resourceString,
            params object[] resourceArgs)
        {
            PSRemotingTransportException e;

            // For the first two special error conditions, it is remotely possible that the wsmanSessionTM is null when the failures are returned
            // as part of command TM operations (could be returned because of RC retries under the hood)
            // Not worth to handle these cases separately as there are very corner scenarios, but need to make sure wsmanSessionTM is not referenced

            // Destination server is reporting that URI redirect is required for this user.
            if ((errorStruct.errorCode == WSManNativeApi.ERROR_WSMAN_REDIRECT_REQUESTED) && (wsmanSessionTM != null))
            {
                IntPtr wsmanSessionHandle = wsmanSessionTM.SessionHandle;
                // populate the transport message with the redirection uri..this will
                // allow caller to make a new connection.
                string redirectLocation = WSManNativeApi.WSManGetSessionOptionAsString(wsmanSessionHandle,
                    WSManNativeApi.WSManSessionOption.WSMAN_OPTION_REDIRECT_LOCATION);
                string winrmMessage = ParseEscapeWSManErrorMessage(
                    WSManNativeApi.WSManGetErrorMessage(wsmanAPIHandle, errorStruct.errorCode)).Trim();

                e = new PSRemotingTransportRedirectException(redirectLocation,
                    PSRemotingErrorId.URIEndPointNotResolved,
                    RemotingErrorIdStrings.URIEndPointNotResolved,
                    winrmMessage,
                    redirectLocation);
            }
            else if ((errorStruct.errorCode == WSManNativeApi.ERROR_WSMAN_INVALID_RESOURCE_URI) && (wsmanSessionTM != null))
            {
                string configurationName =
                    wsmanSessionTM.ConnectionInfo.ShellUri.Replace(Remoting.Client.WSManNativeApi.ResourceURIPrefix, string.Empty);
                string errorMessage = PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.InvalidConfigurationName,
                                                   configurationName,
                                                   wsmanSessionTM.ConnectionInfo.ComputerName);

                e = new PSRemotingTransportException(PSRemotingErrorId.InvalidConfigurationName,
                                                   RemotingErrorIdStrings.ConnectExCallBackError, wsmanSessionTM.ConnectionInfo.ComputerName, errorMessage);

                e.TransportMessage = ParseEscapeWSManErrorMessage(
                   WSManNativeApi.WSManGetErrorMessage(wsmanAPIHandle, errorStruct.errorCode));
            }
            else
            {
                // Construct specific error message and then append this message pointing to our own
                // help topic. PowerShell's about help topic "about_Remote_Troubleshooting" should
                // contain all the trouble shooting information.
                string wsManErrorMessage = PSRemotingErrorInvariants.FormatResourceString(resourceString, resourceArgs);
                e = new PSRemotingTransportException(PSRemotingErrorId.TroubleShootingHelpTopic,
                    RemotingErrorIdStrings.TroubleShootingHelpTopic,
                    wsManErrorMessage);

                e.TransportMessage = ParseEscapeWSManErrorMessage(
                    WSManNativeApi.WSManGetErrorMessage(wsmanAPIHandle, errorStruct.errorCode));
            }

            e.ErrorCode = errorStruct.errorCode;
            TransportErrorOccuredEventArgs eventargs =
                new TransportErrorOccuredEventArgs(e, transportMethodReportingError);
            return eventargs;
        }

        /// <summary>
        /// Helper method that escapes powershell parser recognized strings like "@{" from the error message
        /// string. This is needed to make error messages look authentic. Some WSMan error messages provide a
        /// command line to run to fix certain issues. WSMan command line has syntax that allows use of @{}.
        /// PowerShell parser treats them differently..and so when user cut and paste the command line in a
        /// PowerShell console, it wont work. This escape logic works around the issue.
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        internal static string ParseEscapeWSManErrorMessage(string errorMessage)
        {
            // currently we do special processing only for "@{" construct.
            if (string.IsNullOrEmpty(errorMessage) || (!errorMessage.Contains("@{")))
            {
                return errorMessage;
            }

            string result = errorMessage.Replace("@{", "'@{").Replace("}", "}'");
            return result;

            /*
             * Use this pattern if we need to escape other characters.
             *
            try
            {
                StringBuilder msgSB = new StringBuilder(errorMessage);

                Collection<PSParseError> parserErrors = new Collection<PSParseError>();
                Collection<PSToken> tokens = PSParser.Tokenize(errorMessage, out parserErrors);
                if (parserErrors.Count > 0)
                {
                    tracer.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "There were errors parsing string '{0}'", errorMessage);
                    return errorMessage;
                }

                for (int index = tokens.Count - 1; index > 0; index--)
                {
                    PSToken currentToken = tokens[index];
                    switch(currentToken.Type)
                    {
                        case PSTokenType.GroupStart:
                            msgSB.Insert(currentToken.StartColumn - 1, "'", 1);
                            break;
                        case PSTokenType.GroupEnd:
                            if (msgSB.Length <= currentToken.EndColumn)
                            {
                                msgSB.Append("'");
                            }
                            else
                            {
                                msgSB.Insert(currentToken.EndColumn - 1, ",", 1);
                            }

                            break;
                    }
                }

                return msgSB.ToString();
            }
            // ignore possible exceptions manipulating the string.
            catch(ArgumentOutOfRangeException)
            {
            }
            catch(RuntimeException)
            {
            }

            return errorMessage;*/
        }

        internal enum tmStartModes
        {
            None = 1, Create = 2, Connect = 3
        }

        /// <summary>
        /// Helper method to convert a transport error code value
        /// to a fully qualified error Id string.
        /// </summary>
        /// <param name="transportErrorCode">Transport error code.</param>
        /// <param name="defaultFQEID">Default FQEID.</param>
        /// <returns>Fully qualified error Id string.</returns>
        internal static string GetFQEIDFromTransportError(
            int transportErrorCode,
            string defaultFQEID)
        {
            string specificErrorId;
            if (s_transportErrorCodeToFQEID.TryGetValue(transportErrorCode, out specificErrorId))
            {
                return specificErrorId + "," + defaultFQEID;
            }
            else if (transportErrorCode != 0)
            {
                // Provide error code to uniquely identify the error Id.
                return transportErrorCode.ToString(System.Globalization.NumberFormatInfo.InvariantInfo) + "," + defaultFQEID;
            }

            return defaultFQEID;
        }

        #endregion
    }

    /// <summary>
    /// Class that manages a server session. This doesn't implement IDisposable. Use Close method
    /// to clean the resources.
    /// </summary>
    internal sealed class WSManClientSessionTransportManager : BaseClientSessionTransportManager
    {
        #region Consts

        /// <summary>
        /// Max uri redirection count session variable.
        /// </summary>
        internal const string MAX_URI_REDIRECTION_COUNT_VARIABLE = "WSManMaxRedirectionCount";
        /// <summary>
        /// Default max uri redirection count - wsman.
        /// </summary>
        internal const int MAX_URI_REDIRECTION_COUNT = 5;

        #endregion

        #region Enums

        private enum CompletionNotification
        {
            DisconnectCompleted
        }

        #endregion

        #region CompletionEventArgs

        private sealed class CompletionEventArgs : EventArgs
        {
            internal CompletionEventArgs(CompletionNotification notification)
            {
                Notification = notification;
            }

            internal CompletionNotification Notification { get; }
        }

        #endregion

        #region Private Data
        // operation handles are owned by WSMan
        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private IntPtr _wsManSessionHandle;

        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private IntPtr _wsManShellOperationHandle;

        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private IntPtr _wsManReceiveOperationHandle;

        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private IntPtr _wsManSendOperationHandle;
        // this is used with WSMan callbacks to represent a session transport manager.
        private long _sessionContextID;

        private WSManTransportManagerUtils.tmStartModes _startMode = WSManTransportManagerUtils.tmStartModes.None;

        private readonly string _sessionName;

        // callbacks
        private readonly PrioritySendDataCollection.OnDataAvailableCallback _onDataAvailableToSendCallback;

        // instance callback handlers
        private WSManNativeApi.WSManShellAsync _createSessionCallback;
        private WSManNativeApi.WSManShellAsync _receivedFromRemote;
        private WSManNativeApi.WSManShellAsync _sendToRemoteCompleted;
        private WSManNativeApi.WSManShellAsync _disconnectSessionCompleted;
        private WSManNativeApi.WSManShellAsync _reconnectSessionCompleted;
        private WSManNativeApi.WSManShellAsync _connectSessionCallback;
        // TODO: This GCHandle is required as it seems WSMan is calling create callback
        // after we call Close. This seems wrong. Opened bug on WSMan to track this.
        private GCHandle _createSessionCallbackGCHandle;
        private WSManNativeApi.WSManShellAsync _closeSessionCompleted;

        // used by WSManCreateShell call to send additional data (like negotiation)
        // during shell creation. This is an instance variable to allow for redirection.
        private WSManNativeApi.WSManData_ManToUn _openContent;
        // By default WSMan compresses data sent on the network..use this flag to not do
        // this.
        private bool _noCompression;
        private bool _noMachineProfile;

        private int _connectionRetryCount;

        private const string resBaseName = "remotingerroridstrings";

        // Robust connections maximum retry time value in milliseconds.
        private int _maxRetryTime;

        private void ProcessShellData(string data)
        {
            try
            {
                XmlReaderSettings settings = InternalDeserializer.XmlReaderSettingsForUntrustedXmlDocument.Clone();
                settings.MaxCharactersFromEntities = 1024;      // 1024 is a generous upperbound for shell Xml entries
                settings.MaxCharactersInDocument = 1024 * 30;
                settings.DtdProcessing = System.Xml.DtdProcessing.Prohibit;

                using (XmlReader reader = XmlReader.Create(new StringReader(data), settings))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.LocalName.Equals("IdleTimeOut", StringComparison.OrdinalIgnoreCase) ||
                                reader.LocalName.Equals("MaxIdleTimeOut", StringComparison.OrdinalIgnoreCase))
                            {
                                bool settingIdleTimeout =
                                    !reader.LocalName.Equals("MaxIdleTimeOut", StringComparison.OrdinalIgnoreCase);

                                string timeoutString = reader.ReadElementContentAsString();
                                Dbg.Assert(timeoutString.Substring(0, 2).Equals("PT", StringComparison.OrdinalIgnoreCase),
                                    "IdleTimeout is not in expected format");

                                int decimalIndex = timeoutString.IndexOf('.');
                                try
                                {
                                    int timeout = Convert.ToInt32(timeoutString.Substring(2, decimalIndex - 2), NumberFormatInfo.InvariantInfo) * 1000 + Convert.ToInt32(timeoutString.Substring(decimalIndex + 1, 3), NumberFormatInfo.InvariantInfo);
                                    if (settingIdleTimeout)
                                    {
                                        ConnectionInfo.IdleTimeout = timeout;
                                    }
                                    else
                                    {
                                        ConnectionInfo.MaxIdleTimeout = timeout;
                                    }
                                }
                                catch (InvalidCastException)
                                {
                                    Dbg.Assert(false, "IdleTimeout is not in expected format");
                                }
                            }
                            else if (reader.LocalName.Equals("BufferMode", StringComparison.OrdinalIgnoreCase))
                            {
                                string bufferMode = reader.ReadElementContentAsString();

                                if (bufferMode.Equals("Block", StringComparison.OrdinalIgnoreCase))
                                {
                                    ConnectionInfo.OutputBufferingMode = Runspaces.OutputBufferingMode.Block;
                                }
                                else if (bufferMode.Equals("Drop", StringComparison.OrdinalIgnoreCase))
                                {
                                    ConnectionInfo.OutputBufferingMode = Runspaces.OutputBufferingMode.Drop;
                                }
                                else
                                {
                                    Dbg.Assert(false, "unexpected buffer mode");
                                }
                            }
                        }
                    }
                }
            }
            catch (XmlException)
            {
                Dbg.Assert(false, "shell xml is in unexpected format");
            }
        }

        #endregion

        #region Static Data

        // static callback delegate
        private static WSManNativeApi.WSManShellAsyncCallback s_sessionCreateCallback;
        private static WSManNativeApi.WSManShellAsyncCallback s_sessionCloseCallback;
        private static WSManNativeApi.WSManShellAsyncCallback s_sessionReceiveCallback;
        private static WSManNativeApi.WSManShellAsyncCallback s_sessionSendCallback;
        private static WSManNativeApi.WSManShellAsyncCallback s_sessionDisconnectCallback;
        private static WSManNativeApi.WSManShellAsyncCallback s_sessionReconnectCallback;
        private static WSManNativeApi.WSManShellAsyncCallback s_sessionConnectCallback;

        // This dictionary maintains active session transport managers to be used from various
        // callbacks.
        private static readonly Dictionary<long, WSManClientSessionTransportManager> s_sessionTMHandles =
            new Dictionary<long, WSManClientSessionTransportManager>();

        private static long s_sessionTMSeed;

        // generate unique session id
        private static long GetNextSessionTMHandleId()
        {
            return System.Threading.Interlocked.Increment(ref s_sessionTMSeed);
        }

        // we need a synchronized add and remove so that multiple threads
        // update the data store concurrently
        private static void AddSessionTransportManager(long sessnTMId,
            WSManClientSessionTransportManager sessnTransportManager)
        {
            lock (s_sessionTMHandles)
            {
                s_sessionTMHandles.Add(sessnTMId, sessnTransportManager);
            }
        }

        private static void RemoveSessionTransportManager(long sessnTMId)
        {
            lock (s_sessionTMHandles)
            {
                s_sessionTMHandles.Remove(sessnTMId);
            }
        }

        // we need a synchronized add and remove so that multiple threads
        // update the data store concurrently
        private static bool TryGetSessionTransportManager(IntPtr operationContext,
            out WSManClientSessionTransportManager sessnTransportManager,
            out long sessnTMId)
        {
            sessnTMId = operationContext.ToInt64();
            sessnTransportManager = null;
            lock (s_sessionTMHandles)
            {
                return s_sessionTMHandles.TryGetValue(sessnTMId, out sessnTransportManager);
            }
        }

        #endregion

        #region SHIM: Redirection delegates for test purposes

        private static readonly Delegate s_sessionSendRedirect = null;
        private static readonly Delegate s_protocolVersionRedirect = null;

        #endregion

        #region Constructors

        /// <summary>
        /// Static constructor to initialize WSMan Client stack.
        /// </summary>
        static WSManClientSessionTransportManager()
        {
            // Initialize callback delegates
            WSManNativeApi.WSManShellCompletionFunction createDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnCreateSessionCallback);
            s_sessionCreateCallback = new WSManNativeApi.WSManShellAsyncCallback(createDelegate);

            WSManNativeApi.WSManShellCompletionFunction closeDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnCloseSessionCompleted);
            s_sessionCloseCallback = new WSManNativeApi.WSManShellAsyncCallback(closeDelegate);

            WSManNativeApi.WSManShellCompletionFunction receiveDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnRemoteSessionDataReceived);
            s_sessionReceiveCallback = new WSManNativeApi.WSManShellAsyncCallback(receiveDelegate);

            WSManNativeApi.WSManShellCompletionFunction sendDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnRemoteSessionSendCompleted);
            s_sessionSendCallback = new WSManNativeApi.WSManShellAsyncCallback(sendDelegate);

            WSManNativeApi.WSManShellCompletionFunction disconnectDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnRemoteSessionDisconnectCompleted);
            s_sessionDisconnectCallback = new WSManNativeApi.WSManShellAsyncCallback(disconnectDelegate);

            WSManNativeApi.WSManShellCompletionFunction reconnectDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnRemoteSessionReconnectCompleted);
            s_sessionReconnectCallback = new WSManNativeApi.WSManShellAsyncCallback(reconnectDelegate);

            WSManNativeApi.WSManShellCompletionFunction connectDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnRemoteSessionConnectCallback);
            s_sessionConnectCallback = new WSManNativeApi.WSManShellAsyncCallback(connectDelegate);
        }

        /// <summary>
        /// Constructor. This will create a new PrioritySendDataCollection which should be used to
        /// send data to the server.
        /// </summary>
        /// <param name="runspacePoolInstanceId">
        /// This is used for logging trace/operational crimson messages. Having this id in the logs
        /// helps a user to map which transport is created for which runspace.
        /// </param>
        /// <param name="connectionInfo">
        /// Connection info to use while connecting to the remote machine.
        /// </param>
        /// <param name="cryptoHelper">Crypto helper.</param>
        /// <param name="sessionName">Session friendly name.</param>
        /// <exception cref="PSInvalidOperationException">
        /// 1. Create Session failed with a non-zero error code.
        /// </exception>
        internal WSManClientSessionTransportManager(
            Guid runspacePoolInstanceId,
            WSManConnectionInfo connectionInfo,
            PSRemotingCryptoHelper cryptoHelper,
            string sessionName)
            : base(runspacePoolInstanceId, cryptoHelper)
        {
            // Initialize WSMan instance
            WSManAPIData = new WSManAPIDataCommon();
            if (WSManAPIData.WSManAPIHandle == IntPtr.Zero)
            {
                throw new PSRemotingTransportException(
                    StringUtil.Format(RemotingErrorIdStrings.WSManInitFailed, WSManAPIData.ErrorCode));
            }

            Dbg.Assert(connectionInfo != null, "connectionInfo cannot be null");

            CryptoHelper = cryptoHelper;
            dataToBeSent.Fragmentor = base.Fragmentor;
            _sessionName = sessionName;

            // session transport manager can receive unlimited data..however each object is limited
            // by maxRecvdObjectSize. this is to allow clients to use a session for an unlimited time..
            // also the messages that can be sent to a session are limited and very controlled.
            // However a command transport manager can be restricted to receive only a fixed amount of data
            // controlled by maxRecvdDataSizeCommand..This is because commands can accept any number of input
            // objects.
            ReceivedDataCollection.MaximumReceivedDataSize = null;
            ReceivedDataCollection.MaximumReceivedObjectSize = connectionInfo.MaximumReceivedObjectSize;

            _onDataAvailableToSendCallback =
                new PrioritySendDataCollection.OnDataAvailableCallback(OnDataAvailableCallback);

            Initialize(connectionInfo.ConnectionUri, connectionInfo);
        }

        #endregion

        #region Set Session Options

        /// <summary>
        /// Sets default timeout for all client operations in milliseconds.
        /// TODO: Sync with WSMan and figure out what the default is if we
        /// dont set.
        /// </summary>
        /// <param name="milliseconds"></param>
        /// <returns></returns>
        /// <exception cref="PSInvalidOperationException">
        /// Setting session option failed with a non-zero error code.
        /// </exception>
        internal void SetDefaultTimeOut(int milliseconds)
        {
            Dbg.Assert(_wsManSessionHandle != IntPtr.Zero, "Session handle cannot be null");
            using (tracer.TraceMethod("Setting Default timeout: {0} milliseconds", milliseconds))
            {
                int result = WSManNativeApi.WSManSetSessionOption(_wsManSessionHandle,
                    WSManNativeApi.WSManSessionOption.WSMAN_OPTION_DEFAULT_OPERATION_TIMEOUTMS,
                    new WSManNativeApi.WSManDataDWord(milliseconds));

                if (result != 0)
                {
                    // Get the error message from WSMan
                    string errorMessage = WSManNativeApi.WSManGetErrorMessage(WSManAPIData.WSManAPIHandle, result);

                    PSInvalidOperationException exception = new PSInvalidOperationException(errorMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Sets timeout for Create operation in milliseconds.
        /// </summary>
        /// <param name="milliseconds"></param>
        /// <returns></returns>
        /// <exception cref="PSInvalidOperationException">
        /// Setting session option failed with a non-zero error code.
        /// </exception>
        internal void SetConnectTimeOut(int milliseconds)
        {
            Dbg.Assert(_wsManSessionHandle != IntPtr.Zero, "Session handle cannot be null");
            using (tracer.TraceMethod("Setting CreateShell timeout: {0} milliseconds", milliseconds))
            {
                int result = WSManNativeApi.WSManSetSessionOption(_wsManSessionHandle,
                    WSManNativeApi.WSManSessionOption.WSMAN_OPTION_TIMEOUTMS_CREATE_SHELL,
                    new WSManNativeApi.WSManDataDWord(milliseconds));

                if (result != 0)
                {
                    // Get the error message from WSMan
                    string errorMessage = WSManNativeApi.WSManGetErrorMessage(WSManAPIData.WSManAPIHandle, result);

                    PSInvalidOperationException exception = new PSInvalidOperationException(errorMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Sets timeout for Close operation in milliseconds.
        /// </summary>
        /// <param name="milliseconds"></param>
        /// <returns></returns>
        /// <exception cref="PSInvalidOperationException">
        /// Setting session option failed with a non-zero error code.
        /// </exception>
        internal void SetCloseTimeOut(int milliseconds)
        {
            Dbg.Assert(_wsManSessionHandle != IntPtr.Zero, "Session handle cannot be null");
            using (tracer.TraceMethod("Setting CloseShell timeout: {0} milliseconds", milliseconds))
            {
                int result = WSManNativeApi.WSManSetSessionOption(_wsManSessionHandle,
                    WSManNativeApi.WSManSessionOption.WSMAN_OPTION_TIMEOUTMS_CLOSE_SHELL_OPERATION,
                    new WSManNativeApi.WSManDataDWord(milliseconds));

                if (result != 0)
                {
                    // Get the error message from WSMan
                    string errorMessage = WSManNativeApi.WSManGetErrorMessage(WSManAPIData.WSManAPIHandle, result);

                    PSInvalidOperationException exception = new PSInvalidOperationException(errorMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Sets timeout for SendShellInput operation in milliseconds.
        /// </summary>
        /// <param name="milliseconds"></param>
        /// <returns></returns>
        /// <exception cref="PSInvalidOperationException">
        /// Setting session option failed with a non-zero error code.
        /// </exception>
        internal void SetSendTimeOut(int milliseconds)
        {
            Dbg.Assert(_wsManSessionHandle != IntPtr.Zero, "Session handle cannot be null");
            using (tracer.TraceMethod("Setting SendShellInput timeout: {0} milliseconds", milliseconds))
            {
                int result = WSManNativeApi.WSManSetSessionOption(_wsManSessionHandle,
                    WSManNativeApi.WSManSessionOption.WSMAN_OPTION_TIMEOUTMS_SEND_SHELL_INPUT,
                    new WSManNativeApi.WSManDataDWord(milliseconds));

                if (result != 0)
                {
                    // Get the error message from WSMan
                    string errorMessage = WSManNativeApi.WSManGetErrorMessage(WSManAPIData.WSManAPIHandle, result);

                    PSInvalidOperationException exception = new PSInvalidOperationException(errorMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Sets timeout for Receive operation in milliseconds.
        /// </summary>
        /// <param name="milliseconds"></param>
        /// <returns></returns>
        /// <exception cref="PSInvalidOperationException">
        /// Setting session option failed with a non-zero error code.
        /// </exception>
        internal void SetReceiveTimeOut(int milliseconds)
        {
            Dbg.Assert(_wsManSessionHandle != IntPtr.Zero, "Session handle cannot be null");
            using (tracer.TraceMethod("Setting ReceiveShellOutput timeout: {0} milliseconds", milliseconds))
            {
                int result = WSManNativeApi.WSManSetSessionOption(_wsManSessionHandle,
                    WSManNativeApi.WSManSessionOption.WSMAN_OPTION_TIMEOUTMS_RECEIVE_SHELL_OUTPUT,
                    new WSManNativeApi.WSManDataDWord(milliseconds));

                if (result != 0)
                {
                    // Get the error message from WSMan
                    string errorMessage = WSManNativeApi.WSManGetErrorMessage(WSManAPIData.WSManAPIHandle, result);

                    PSInvalidOperationException exception = new PSInvalidOperationException(errorMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Sets timeout for Signal operation in milliseconds.
        /// </summary>
        /// <param name="milliseconds"></param>
        /// <returns></returns>
        /// <exception cref="PSInvalidOperationException">
        /// Setting session option failed with a non-zero error code.
        /// </exception>
        internal void SetSignalTimeOut(int milliseconds)
        {
            Dbg.Assert(_wsManSessionHandle != IntPtr.Zero, "Session handle cannot be null");
            using (tracer.TraceMethod("Setting SignalShell timeout: {0} milliseconds", milliseconds))
            {
                int result = WSManNativeApi.WSManSetSessionOption(_wsManSessionHandle,
                    WSManNativeApi.WSManSessionOption.WSMAN_OPTION_TIMEOUTMS_SIGNAL_SHELL,
                    new WSManNativeApi.WSManDataDWord(milliseconds));

                if (result != 0)
                {
                    // Get the error message from WSMan
                    string errorMessage = WSManNativeApi.WSManGetErrorMessage(WSManAPIData.WSManAPIHandle, result);

                    PSInvalidOperationException exception = new PSInvalidOperationException(errorMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Sets a DWORD value for a WSMan Session option.
        /// </summary>
        /// <param name="option"></param>
        /// <param name="dwordData"></param>
        /// <exception cref="PSInvalidOperationException">
        /// Setting session option failed with a non-zero error code.
        /// </exception>
        internal void SetWSManSessionOption(WSManNativeApi.WSManSessionOption option, int dwordData)
        {
            int result = WSManNativeApi.WSManSetSessionOption(_wsManSessionHandle,
                option, new WSManNativeApi.WSManDataDWord(dwordData));

            if (result != 0)
            {
                // Get the error message from WSMan
                string errorMessage = WSManNativeApi.WSManGetErrorMessage(WSManAPIData.WSManAPIHandle, result);

                PSInvalidOperationException exception = new PSInvalidOperationException(errorMessage);
                throw exception;
            }
        }

        /// <summary>
        /// Sets a string value for a WSMan Session option.
        /// </summary>
        /// <param name="option"></param>
        /// <param name="stringData"></param>
        /// <exception cref="PSInvalidOperationException">
        /// Setting session option failed with a non-zero error code.
        /// </exception>
        internal void SetWSManSessionOption(WSManNativeApi.WSManSessionOption option, string stringData)
        {
            using (WSManNativeApi.WSManData_ManToUn data = new WSManNativeApi.WSManData_ManToUn(stringData))
            {
                int result = WSManNativeApi.WSManSetSessionOption(_wsManSessionHandle,
                      option, data);

                if (result != 0)
                {
                    // Get the error message from WSMan
                    string errorMessage = WSManNativeApi.WSManGetErrorMessage(WSManAPIData.WSManAPIHandle, result);

                    PSInvalidOperationException exception = new PSInvalidOperationException(errorMessage);
                    throw exception;
                }
            }
        }

        #endregion

        #region Internal Methods / Properties

        internal WSManAPIDataCommon WSManAPIData { get; private set; }

        internal bool SupportsDisconnect { get; private set; }

        internal override void DisconnectAsync()
        {
            Dbg.Assert(!isClosed, "object already disposed");

            // Pass the WSManConnectionInfo object IdleTimeout value if it is
            // valid.  Otherwise pass the default value that instructs the server
            // to use its default IdleTimeout value.
            uint uIdleTimeout = (ConnectionInfo.IdleTimeout > 0) ?
                (uint)ConnectionInfo.IdleTimeout : UseServerDefaultIdleTimeoutUInt;

            // startup info
            WSManNativeApi.WSManShellDisconnectInfo disconnectInfo = new WSManNativeApi.WSManShellDisconnectInfo(uIdleTimeout);

            // Add ETW traces

            // disconnect Callback
            _disconnectSessionCompleted = new WSManNativeApi.WSManShellAsync(new IntPtr(_sessionContextID), s_sessionDisconnectCallback);
            try
            {
                lock (syncObject)
                {
                    if (isClosed)
                    {
                        // the transport is already closed
                        // anymore.
                        return;
                    }

                    int flags = 0;
                    flags |= (ConnectionInfo.OutputBufferingMode == Runspaces.OutputBufferingMode.Block) ?
                                    (int)WSManNativeApi.WSManShellFlag.WSMAN_FLAG_SERVER_BUFFERING_MODE_BLOCK : 0;
                    flags |= (ConnectionInfo.OutputBufferingMode == Runspaces.OutputBufferingMode.Drop) ?
                                    (int)WSManNativeApi.WSManShellFlag.WSMAN_FLAG_SERVER_BUFFERING_MODE_DROP : 0;

                    WSManNativeApi.WSManDisconnectShellEx(_wsManShellOperationHandle,
                            flags,
                            disconnectInfo,
                            _disconnectSessionCompleted);
                }
            }
            finally
            {
                disconnectInfo.Dispose();
            }
        }

        internal override void ReconnectAsync()
        {
            Dbg.Assert(!isClosed, "object already disposed");
            ReceivedDataCollection.PrepareForStreamConnect();

            // Add ETW traces

            // reconnect Callback
            _reconnectSessionCompleted = new WSManNativeApi.WSManShellAsync(new IntPtr(_sessionContextID), s_sessionReconnectCallback);
            lock (syncObject)
            {
                if (isClosed)
                {
                    // the transport is already closed
                    // anymore.
                    return;
                }

                int flags = 0;
                flags |= (ConnectionInfo.OutputBufferingMode == Runspaces.OutputBufferingMode.Block) ?
                                (int)WSManNativeApi.WSManShellFlag.WSMAN_FLAG_SERVER_BUFFERING_MODE_BLOCK : 0;
                flags |= (ConnectionInfo.OutputBufferingMode == Runspaces.OutputBufferingMode.Drop) ?
                                (int)WSManNativeApi.WSManShellFlag.WSMAN_FLAG_SERVER_BUFFERING_MODE_DROP : 0;

                WSManNativeApi.WSManReconnectShellEx(_wsManShellOperationHandle,
                        flags,
                        _reconnectSessionCompleted);
            }
        }

        /// <summary>
        /// Starts connecting to an existing remote session. This will result in a WSManConnectShellEx WSMan
        /// async call. Piggy backs available data in input stream as openXml in connect SOAP.
        /// DSHandler will push negotiation related messages through the open content.
        /// </summary>
        /// <exception cref="PSRemotingTransportException">
        /// WSManConnectShellEx failed.
        /// </exception>
        internal override void ConnectAsync()
        {
            Dbg.Assert(!isClosed, "object already disposed");
            Dbg.Assert(!string.IsNullOrEmpty(ConnectionInfo.ShellUri), "shell uri cannot be null or empty.");

            ReceivedDataCollection.PrepareForStreamConnect();
            // additional content with connect shell call. Negotiation and connect related messages
            // should be included in payload
            if (_openContent == null)
            {
                DataPriorityType additionalDataType;
                byte[] additionalData = dataToBeSent.ReadOrRegisterCallback(null, out additionalDataType);

                if (additionalData != null)
                {
                    // WSMan expects the data to be in XML format (which is text + xml tags)
                    // so convert byte[] into base64 encoded format
                    string base64EncodedDataInXml = string.Create(CultureInfo.InvariantCulture, $"<{WSManNativeApi.PS_CONNECT_XML_TAG} xmlns=\"{WSManNativeApi.PS_XML_NAMESPACE}\">{Convert.ToBase64String(additionalData)}</{WSManNativeApi.PS_CONNECT_XML_TAG}>");
                    _openContent = new WSManNativeApi.WSManData_ManToUn(base64EncodedDataInXml);
                }

                // THERE SHOULD BE NO ADDITIONAL DATA. If there is, it means we are not able to push all initial negotiation related data
                // as part of Connect SOAP. The connect algorithm is based on this assumption. So bail out.
                additionalData = dataToBeSent.ReadOrRegisterCallback(null, out additionalDataType);
                if (additionalData != null)
                {
                    // Negotiation payload does not fit in ConnectShell. bail out.
                    // Assert for now. should be replaced with raising an exception so upper layers can catch.
                    Dbg.Assert(false, "Negotiation payload does not fit in ConnectShell");
                    return;
                }
            }

            // Create and store context for this shell operation. This context is used from various callbacks
            _sessionContextID = GetNextSessionTMHandleId();
            AddSessionTransportManager(_sessionContextID, this);

            // session is implicitly assumed to support disconnect
            SupportsDisconnect = true;

            // Create Callback
            _connectSessionCallback = new WSManNativeApi.WSManShellAsync(new IntPtr(_sessionContextID), s_sessionConnectCallback);
            lock (syncObject)
            {
                if (isClosed)
                {
                    // the transport is already closed..so no need to connect
                    // anymore.
                    return;
                }

                Dbg.Assert(_startMode == WSManTransportManagerUtils.tmStartModes.None, "startMode is not in expected state");
                _startMode = WSManTransportManagerUtils.tmStartModes.Connect;

                int flags = 0;
                flags |= (ConnectionInfo.OutputBufferingMode == Runspaces.OutputBufferingMode.Block) ?
                                (int)WSManNativeApi.WSManShellFlag.WSMAN_FLAG_SERVER_BUFFERING_MODE_BLOCK : 0;
                flags |= (ConnectionInfo.OutputBufferingMode == Runspaces.OutputBufferingMode.Drop) ?
                                (int)WSManNativeApi.WSManShellFlag.WSMAN_FLAG_SERVER_BUFFERING_MODE_DROP : 0;

                WSManNativeApi.WSManConnectShellEx(_wsManSessionHandle,
                    flags,
                    ConnectionInfo.ShellUri,
                    RunspacePoolInstanceId.ToString().ToUpperInvariant(),  // wsman is case sensitive wrt shellId. so consistently using upper case
                    IntPtr.Zero,
                    _openContent,
                    _connectSessionCallback,
                    ref _wsManShellOperationHandle);
            }

            if (_wsManShellOperationHandle == IntPtr.Zero)
            {
                TransportErrorOccuredEventArgs eventargs = WSManTransportManagerUtils.ConstructTransportErrorEventArgs(WSManAPIData.WSManAPIHandle,
                    this,
                    new WSManNativeApi.WSManError(),
                    TransportMethodEnum.ConnectShellEx,
                    RemotingErrorIdStrings.ConnectExFailed, this.ConnectionInfo.ComputerName);
                ProcessWSManTransportError(eventargs);
                return;
            }
        }

        internal override void StartReceivingData()
        {
            lock (syncObject)
            {
                // make sure the transport is not closed.
                if (isClosed)
                {
                    tracer.WriteLine("Client Session TM: Transport manager is closed. So returning");
                    return;
                }

                if (receiveDataInitiated)
                {
                    tracer.WriteLine("Client Session TM: ReceiveData has already been called.");
                    return;
                }

                receiveDataInitiated = true;
                tracer.WriteLine("Client Session TM: Placing Receive request using WSManReceiveShellOutputEx");
                PSEtwLog.LogAnalyticInformational(
                    PSEventId.WSManReceiveShellOutputEx,
                    PSOpcode.Receive, PSTask.None, PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                    RunspacePoolInstanceId.ToString(), Guid.Empty.ToString());

                _receivedFromRemote = new WSManNativeApi.WSManShellAsync(new IntPtr(_sessionContextID), s_sessionReceiveCallback);
                WSManNativeApi.WSManReceiveShellOutputEx(_wsManShellOperationHandle,
                    IntPtr.Zero, 0, WSManAPIData.OutputStreamSet, _receivedFromRemote,
                    ref _wsManReceiveOperationHandle);
            }
        }

        /// <summary>
        /// Starts connecting to remote end asynchronously. This will result in a WSManCreateShellEx WSMan
        /// async call. By the time this call returns, we will have a valid handle, if the operation
        /// succeeds. Make sure other methods are called only after this method returns. Thread
        /// synchronization is left to the caller.
        /// </summary>
        /// <exception cref="PSRemotingTransportException">
        /// WSManCreateShellEx failed.
        /// </exception>
        public override void CreateAsync()
        {
            Dbg.Assert(!isClosed, "object already disposed");
            Dbg.Assert(!string.IsNullOrEmpty(ConnectionInfo.ShellUri), "shell uri cannot be null or empty.");
            Dbg.Assert(WSManAPIData != null, "WSManApiData should always be created before session creation.");

            List<WSManNativeApi.WSManOption> shellOptions = new List<WSManNativeApi.WSManOption>(WSManAPIData.CommonOptionSet);

            #region SHIM: Redirection code for protocol version

            if (s_protocolVersionRedirect != null)
            {
                string newProtocolVersion = (string)s_protocolVersionRedirect.DynamicInvoke();
                shellOptions.Clear();
                WSManNativeApi.WSManOption newPrtVOption = new WSManNativeApi.WSManOption();
                newPrtVOption.name = RemoteDataNameStrings.PS_STARTUP_PROTOCOL_VERSION_NAME;
                newPrtVOption.value = newProtocolVersion;
                newPrtVOption.mustComply = true;
                shellOptions.Add(newPrtVOption);
            }

            #endregion

            // Pass the WSManConnectionInfo object IdleTimeout value if it is
            // valid.  Otherwise pass the default value that instructs the server
            // to use its default IdleTimeout value.
            uint uIdleTimeout = (ConnectionInfo.IdleTimeout > 0) ?
                (uint)ConnectionInfo.IdleTimeout : UseServerDefaultIdleTimeoutUInt;

            // startup info
            WSManNativeApi.WSManShellStartupInfo_ManToUn startupInfo =
                new WSManNativeApi.WSManShellStartupInfo_ManToUn(WSManAPIData.InputStreamSet,
                WSManAPIData.OutputStreamSet,
                uIdleTimeout,
                _sessionName);

            // additional content with create shell call. Piggy back first fragment from
            // the dataToBeSent buffer.
            if (_openContent == null)
            {
                DataPriorityType additionalDataType;
                byte[] additionalData = dataToBeSent.ReadOrRegisterCallback(null, out additionalDataType);

                #region SHIM: Redirection code for session data send.

                bool sendContinue = true;

                if (s_sessionSendRedirect != null)
                {
                    object[] arguments = new object[2] { null, additionalData };
                    sendContinue = (bool)s_sessionSendRedirect.DynamicInvoke(arguments);
                    additionalData = (byte[])arguments[0];
                }

                if (!sendContinue)
                    return;

                #endregion

                if (additionalData != null)
                {
                    // WSMan expects the data to be in XML format (which is text + xml tags)
                    // so convert byte[] into base64 encoded format
                    string base64EncodedDataInXml = string.Create(CultureInfo.InvariantCulture, $"<{WSManNativeApi.PS_CREATION_XML_TAG} xmlns=\"{WSManNativeApi.PS_XML_NAMESPACE}\">{Convert.ToBase64String(additionalData)}</{WSManNativeApi.PS_CREATION_XML_TAG}>");
                    _openContent = new WSManNativeApi.WSManData_ManToUn(base64EncodedDataInXml);
                }
            }

            // Create the session context information only once.  CreateAsync() can be called multiple
            // times by RetrySessionCreation for flaky networks.
            if (_sessionContextID == 0)
            {
                // Create and store context for this shell operation. This context is used from various callbacks
                _sessionContextID = GetNextSessionTMHandleId();
                AddSessionTransportManager(_sessionContextID, this);

                // Create Callback
                _createSessionCallback = new WSManNativeApi.WSManShellAsync(new IntPtr(_sessionContextID), s_sessionCreateCallback);
                _createSessionCallbackGCHandle = GCHandle.Alloc(_createSessionCallback);
            }

            PSEtwLog.LogAnalyticInformational(
                PSEventId.WSManCreateShell,
                PSOpcode.Connect,
                PSTask.CreateRunspace, PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                RunspacePoolInstanceId.ToString());

            try
            {
                lock (syncObject)
                {
                    if (isClosed)
                    {
                        // the transport is already closed..so no need to create a connection
                        // anymore.
                        return;
                    }

                    Dbg.Assert(_startMode == WSManTransportManagerUtils.tmStartModes.None, "startMode is not in expected state");
                    _startMode = WSManTransportManagerUtils.tmStartModes.Create;

                    if (_noMachineProfile)
                    {
                        WSManNativeApi.WSManOption noProfile = new WSManNativeApi.WSManOption();
                        noProfile.name = WSManNativeApi.NoProfile;
                        noProfile.mustComply = true;
                        noProfile.value = "1";
                        shellOptions.Add(noProfile);
                    }

                    int flags = _noCompression ? (int)WSManNativeApi.WSManShellFlag.WSMAN_FLAG_NO_COMPRESSION : 0;
                    flags |= (ConnectionInfo.OutputBufferingMode == Runspaces.OutputBufferingMode.Block) ?
                                    (int)WSManNativeApi.WSManShellFlag.WSMAN_FLAG_SERVER_BUFFERING_MODE_BLOCK : 0;
                    flags |= (ConnectionInfo.OutputBufferingMode == Runspaces.OutputBufferingMode.Drop) ?
                                    (int)WSManNativeApi.WSManShellFlag.WSMAN_FLAG_SERVER_BUFFERING_MODE_DROP : 0;

                    using (WSManNativeApi.WSManOptionSet optionSet = new WSManNativeApi.WSManOptionSet(shellOptions.ToArray()))
                    {
                        WSManNativeApi.WSManCreateShellEx(_wsManSessionHandle,
                            flags,
                            ConnectionInfo.ShellUri,
                            RunspacePoolInstanceId.ToString().ToUpperInvariant(),
                            startupInfo,
                            optionSet,
                            _openContent,
                            _createSessionCallback,
                            ref _wsManShellOperationHandle);
                    }
                }

                if (_wsManShellOperationHandle == IntPtr.Zero)
                {
                    TransportErrorOccuredEventArgs eventargs = WSManTransportManagerUtils.ConstructTransportErrorEventArgs(WSManAPIData.WSManAPIHandle,
                        this,
                        new WSManNativeApi.WSManError(),
                        TransportMethodEnum.CreateShellEx,
                        RemotingErrorIdStrings.ConnectExFailed,
                        this.ConnectionInfo.ComputerName);
                    ProcessWSManTransportError(eventargs);
                    return;
                }
            }
            finally
            {
                startupInfo.Dispose();
            }
        }

        /// <summary>
        /// Closes the pending Create,Send,Receive operations and then closes the shell and release all the resources.
        /// The caller should make sure this method is called only after calling ConnectAsync.
        /// </summary>
        public override void CloseAsync()
        {
            bool shouldRaiseCloseCompleted = false;
            // let other threads release the lock before we clean up the resources.
            lock (syncObject)
            {
                if (isClosed)
                {
                    return;
                }

                if (_startMode == WSManTransportManagerUtils.tmStartModes.None)
                {
                    shouldRaiseCloseCompleted = true;
                }
                else if (_startMode == WSManTransportManagerUtils.tmStartModes.Create ||
                    _startMode == WSManTransportManagerUtils.tmStartModes.Connect)
                {
                    if (_wsManShellOperationHandle == IntPtr.Zero)
                    {
                        shouldRaiseCloseCompleted = true;
                    }
                }
                else
                {
                    Dbg.Assert(false, "startMode is in unexpected state");
                }

                // Set boolean indicating that this session is closing.
                isClosed = true;
            }

            base.CloseAsync();

            if (shouldRaiseCloseCompleted)
            {
                try
                {
                    RaiseCloseCompleted();
                }
                finally
                {
                    RemoveSessionTransportManager(_sessionContextID);
                }

                return;
            }

            // TODO - On unexpected failures on a reconstructed session... we dont want to close server session
            PSEtwLog.LogAnalyticInformational(
                PSEventId.WSManCloseShell,
                PSOpcode.Disconnect, PSTask.None, PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                RunspacePoolInstanceId.ToString());
            _closeSessionCompleted = new WSManNativeApi.WSManShellAsync(new IntPtr(_sessionContextID), s_sessionCloseCallback);
            WSManNativeApi.WSManCloseShell(_wsManShellOperationHandle, 0, _closeSessionCompleted);
        }

        /// <summary>
        /// Adjusts for any variations in different protocol versions. Following changes are considered
        /// - In V2, default max envelope size is 150KB while in V3 it has been changed to 500KB.
        ///   With default configuration remoting from V3 client to V2 server will break as V3 client can send upto 500KB in a single Send packet
        ///   So if server version is known to be V2, we'll downgrade the max env size to 150KB (V2's default) if the current value is 500KB (V3 default)
        /// </summary>
        /// <param name="serverProtocolVersion">Server negotiated protocol version.</param>
        internal void AdjustForProtocolVariations(Version serverProtocolVersion)
        {
            if (serverProtocolVersion <= RemotingConstants.ProtocolVersionWin7RTM)
            {
                int maxEnvSize;
                WSManNativeApi.WSManGetSessionOptionAsDword(_wsManSessionHandle,
                    WSManNativeApi.WSManSessionOption.WSMAN_OPTION_MAX_ENVELOPE_SIZE_KB,
                    out maxEnvSize);

                if (maxEnvSize == WSManNativeApi.WSMAN_DEFAULT_MAX_ENVELOPE_SIZE_KB_V3)
                {
                    int result = WSManNativeApi.WSManSetSessionOption(_wsManSessionHandle,
                    WSManNativeApi.WSManSessionOption.WSMAN_OPTION_MAX_ENVELOPE_SIZE_KB,
                    new WSManNativeApi.WSManDataDWord(WSManNativeApi.WSMAN_DEFAULT_MAX_ENVELOPE_SIZE_KB_V2));

                    if (result != 0)
                    {
                        // Get the error message from WSMan
                        string errorMessage = WSManNativeApi.WSManGetErrorMessage(WSManAPIData.WSManAPIHandle, result);

                        PSInvalidOperationException exception = new PSInvalidOperationException(errorMessage);
                        throw exception;
                    }

                    // retrieve the packet size again
                    int packetSize;
                    WSManNativeApi.WSManGetSessionOptionAsDword(_wsManSessionHandle,
                        WSManNativeApi.WSManSessionOption.WSMAN_OPTION_SHELL_MAX_DATA_SIZE_PER_MESSAGE_KB,
                        out packetSize);
                    // packet size returned is in KB. Convert this into bytes
                    Fragmentor.FragmentSize = packetSize << 10;
                }
            }
        }

        /// <summary>
        /// Used by callers to prepare the session transportmanager for a URI redirection.
        /// This must be called only after Create callback (or Error form create) is received.
        /// This will close the internal WSMan Session handle. Callers must catch the close
        /// completed event and call Redirect to perform the redirection.
        /// </summary>
        internal override void PrepareForRedirection()
        {
            Dbg.Assert(!isClosed, "Transport manager must not be closed while preparing for redirection.");

            _closeSessionCompleted = new WSManNativeApi.WSManShellAsync(new IntPtr(_sessionContextID), s_sessionCloseCallback);
            WSManNativeApi.WSManCloseShell(_wsManShellOperationHandle, 0, _closeSessionCompleted);
        }

        /// <summary>
        /// Redirect the transport manager to point to a new URI.
        /// </summary>
        /// <param name="newUri">
        /// Redirect Uri to connect to.
        /// </param>
        /// <param name="connectionInfo">
        /// Connection info object used for retrieving credential, auth. mechanism etc.
        /// </param>
        /// <exception cref="PSInvalidOperationException">
        /// 1. Create Session failed with a non-zero error code.
        /// </exception>
        internal override void Redirect(Uri newUri, RunspaceConnectionInfo connectionInfo)
        {
            CloseSessionAndClearResources();
            tracer.WriteLine("Redirecting to URI: {0}", newUri);
            PSEtwLog.LogAnalyticInformational(
                PSEventId.URIRedirection,
                PSOpcode.Connect,
                PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                RunspacePoolInstanceId.ToString(),
                newUri.ToString());
            Initialize(newUri, (WSManConnectionInfo)connectionInfo);
            // reset startmode
            _startMode = WSManTransportManagerUtils.tmStartModes.None;
            CreateAsync();
        }

        /// <summary>
        /// Creates a command transport manager. This will create a new PrioritySendDataCollection which should be used to
        /// send data to the server.
        /// </summary>
        /// <param name="connectionInfo">
        /// Connection info to be used for creating the command.
        /// </param>
        /// <param name="cmd">
        /// Command for which transport manager is created.
        /// </param>
        /// <param name="noInput">
        /// true if the command has input.
        /// </param>
        /// <returns></returns>
        internal override BaseClientCommandTransportManager CreateClientCommandTransportManager(RunspaceConnectionInfo connectionInfo,
                    ClientRemotePowerShell cmd, bool noInput)
        {
            Dbg.Assert(cmd != null, "Cmd cannot be null");

            WSManConnectionInfo wsmanConnectionInfo = connectionInfo as WSManConnectionInfo;
            Dbg.Assert(wsmanConnectionInfo != null, "ConnectionInfo must be WSManConnectionInfo");

            WSManClientCommandTransportManager result = new
                WSManClientCommandTransportManager(wsmanConnectionInfo, _wsManShellOperationHandle, cmd, noInput, this);
            return result;
        }

        /// <summary>
        /// Initializes the session.
        /// </summary>
        /// <param name="connectionUri">
        /// Uri to connect to.
        /// </param>
        /// <param name="connectionInfo">
        /// Connection info object used for retrieving credential, auth. mechanism etc.
        /// </param>
        /// <exception cref="PSInvalidOperationException">
        /// 1. Create Session failed with a non-zero error code.
        /// </exception>
        private void Initialize(Uri connectionUri, WSManConnectionInfo connectionInfo)
        {
            Dbg.Assert(connectionInfo != null, "connectionInfo cannot be null.");

            ConnectionInfo = connectionInfo;

            // this will generate: http://ComputerName:port/appname?PSVersion=<version>
            // PSVersion= pattern is needed to make Exchange compatible with PS V2 CTP3
            // release. Using the PSVersion= logic, Exchange R4 server will redirect
            // clients to an R3 endpoint.
            bool isSSLSpecified = false;
            string connectionStr = connectionUri.OriginalString;
            if ((connectionUri == connectionInfo.ConnectionUri) &&
                (connectionInfo.UseDefaultWSManPort))
            {
                connectionStr = WSManConnectionInfo.GetConnectionString(connectionInfo.ConnectionUri,
                    out isSSLSpecified);
            }

            // TODO: Remove this after RDS moved to $using
            string additionalUriSuffixString = string.Empty;
            if (PSSessionConfigurationData.IsServerManager)
            {
                additionalUriSuffixString = ";MSP=7a83d074-bb86-4e52-aa3e-6cc73cc066c8";
            }

            if (string.IsNullOrEmpty(connectionUri.Query))
            {
                // if there is no query string already, create one..see RFC 3986
                connectionStr = string.Format(CultureInfo.InvariantCulture,
                    "{0}?PSVersion={1}{2}",
                    // Trimming the last '/' as this will allow WSMan to
                    // properly apply URLPrefix.
                    // Ex: http://localhost?PSVersion=2.0 will be converted
                    // to http://localhost:<port>/<urlprefix>?PSVersion=2.0
                    // by WSMan
                    connectionStr.TrimEnd('/'),
                    PSVersionInfo.PSVersion,
                    additionalUriSuffixString);
            }
            else
            {
                // if there is already a query string, append using & .. see RFC 3986
                connectionStr = string.Format(CultureInfo.InvariantCulture,
                       "{0};PSVersion={1}{2}",
                       connectionStr,
                       PSVersionInfo.PSVersion,
                       additionalUriSuffixString);
            }

            WSManNativeApi.BaseWSManAuthenticationCredentials authCredentials;
            // use certificate thumbprint for authentication
            if (connectionInfo.CertificateThumbprint != null)
            {
                authCredentials = new WSManNativeApi.WSManCertificateThumbprintCredentials(connectionInfo.CertificateThumbprint);
            }
            else
            {
                // use credential based authentication
                string userName = null;
                System.Security.SecureString password = null;
                if ((connectionInfo.Credential != null) && (!string.IsNullOrEmpty(connectionInfo.Credential.UserName)))
                {
                    userName = connectionInfo.Credential.UserName;
                    password = connectionInfo.Credential.Password;
                }

                WSManNativeApi.WSManUserNameAuthenticationCredentials userNameCredentials =
                    new WSManNativeApi.WSManUserNameAuthenticationCredentials(userName,
                        password,
                        connectionInfo.WSManAuthenticationMechanism);

                authCredentials = userNameCredentials;
            }

            // proxy related data
            WSManNativeApi.WSManUserNameAuthenticationCredentials proxyAuthCredentials = null;
            if (connectionInfo.ProxyCredential != null)
            {
                WSManNativeApi.WSManAuthenticationMechanism authMechanism = WSManNativeApi.WSManAuthenticationMechanism.WSMAN_FLAG_AUTH_NEGOTIATE;
                string userName = null;
                System.Security.SecureString password = null;

                switch (connectionInfo.ProxyAuthentication)
                {
                    case AuthenticationMechanism.Negotiate:
                        authMechanism = WSManNativeApi.WSManAuthenticationMechanism.WSMAN_FLAG_AUTH_NEGOTIATE;
                        break;
                    case AuthenticationMechanism.Basic:
                        authMechanism = WSManNativeApi.WSManAuthenticationMechanism.WSMAN_FLAG_AUTH_BASIC;
                        break;
                    case AuthenticationMechanism.Digest:
                        authMechanism = WSManNativeApi.WSManAuthenticationMechanism.WSMAN_FLAG_AUTH_DIGEST;
                        break;
                }

                if (!string.IsNullOrEmpty(connectionInfo.ProxyCredential.UserName))
                {
                    userName = connectionInfo.ProxyCredential.UserName;
                    password = connectionInfo.ProxyCredential.Password;
                }

                // use credential based authentication
                proxyAuthCredentials = new WSManNativeApi.WSManUserNameAuthenticationCredentials(userName, password, authMechanism);
            }

            WSManNativeApi.WSManProxyInfo proxyInfo = (connectionInfo.ProxyAccessType == ProxyAccessType.None) ?
                null :
                new WSManNativeApi.WSManProxyInfo(connectionInfo.ProxyAccessType, proxyAuthCredentials);

            int result = 0;

            try
            {
                result = WSManNativeApi.WSManCreateSession(WSManAPIData.WSManAPIHandle, connectionStr, 0,
                     authCredentials.GetMarshalledObject(),
                     (proxyInfo == null) ? IntPtr.Zero : (IntPtr)proxyInfo,
                     ref _wsManSessionHandle);
            }
            finally
            {
                // release resources
                proxyAuthCredentials?.Dispose();
                proxyInfo?.Dispose();
                authCredentials?.Dispose();
            }

            if (result != 0)
            {
                // Get the error message from WSMan
                string errorMessage = WSManNativeApi.WSManGetErrorMessage(WSManAPIData.WSManAPIHandle, result);

                PSInvalidOperationException exception = new PSInvalidOperationException(errorMessage);
                throw exception;
            }

            // set the packet size for this session
            int packetSize;
            WSManNativeApi.WSManGetSessionOptionAsDword(_wsManSessionHandle,
                WSManNativeApi.WSManSessionOption.WSMAN_OPTION_SHELL_MAX_DATA_SIZE_PER_MESSAGE_KB,
                out packetSize);
            // packet size returned is in KB. Convert this into bytes..
            Fragmentor.FragmentSize = packetSize << 10;

            // Get robust connections maximum retries time.
            WSManNativeApi.WSManGetSessionOptionAsDword(_wsManSessionHandle,
                WSManNativeApi.WSManSessionOption.WSMAN_OPTION_MAX_RETRY_TIME,
                out _maxRetryTime);

            this.dataToBeSent.Fragmentor = base.Fragmentor;
            _noCompression = !connectionInfo.UseCompression;
            _noMachineProfile = connectionInfo.NoMachineProfile;

            // set other WSMan session related defaults
            if (isSSLSpecified)
            {
                // WSMan Port DCR related changes - BUG 542726
                // this session option will tell WSMan to use port for HTTPS from
                // config provider.
                SetWSManSessionOption(WSManNativeApi.WSManSessionOption.WSMAN_OPTION_USE_SSL, 1);
            }

#if UNIX
            // Explicitly disallow Basic auth over HTTP on Unix.
            if (connectionInfo.AuthenticationMechanism == AuthenticationMechanism.Basic && !isSSLSpecified && connectionUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new PSRemotingTransportException(PSRemotingErrorId.ConnectFailed, RemotingErrorIdStrings.BasicAuthOverHttpNotSupported);
            }

            // The OMI client distributed with PowerShell does not support validating server certificates on Unix.
            // Check if third-party psrpclient and MI support the verification.
            // If WSManGetSessionOptionAsDword does not return 0 then it's not supported.
            bool verificationAvailable = WSManNativeApi.WSManGetSessionOptionAsDword(_wsManSessionHandle,
                WSManNativeApi.WSManSessionOption.WSMAN_OPTION_SKIP_CA_CHECK, out _) == 0;

            if (isSSLSpecified && !verificationAvailable && (!connectionInfo.SkipCACheck || !connectionInfo.SkipCNCheck))
            {
                throw new PSRemotingTransportException(PSRemotingErrorId.ConnectSkipCheckFailed, RemotingErrorIdStrings.UnixOnlyHttpsWithoutSkipCACheckNotSupported);
            }
#endif

            if (connectionInfo.NoEncryption)
            {
                // send unencrypted messages
                SetWSManSessionOption(WSManNativeApi.WSManSessionOption.WSMAN_OPTION_UNENCRYPTED_MESSAGES, 1);
            }
            // check if implicit credentials can be used for Negotiate
            if (connectionInfo.AllowImplicitCredentialForNegotiate)
            {
                result = WSManNativeApi.WSManSetSessionOption(_wsManSessionHandle,
                    WSManNativeApi.WSManSessionOption.WSMAN_OPTION_ALLOW_NEGOTIATE_IMPLICIT_CREDENTIALS,
                    new WSManNativeApi.WSManDataDWord(1));
            }

            if (connectionInfo.UseUTF16)
            {
                SetWSManSessionOption(WSManNativeApi.WSManSessionOption.WSMAN_OPTION_UTF16, 1);
            }

            if (connectionInfo.SkipCACheck)
            {
                SetWSManSessionOption(WSManNativeApi.WSManSessionOption.WSMAN_OPTION_SKIP_CA_CHECK, 1);
            }

            if (connectionInfo.SkipCNCheck)
            {
                SetWSManSessionOption(WSManNativeApi.WSManSessionOption.WSMAN_OPTION_SKIP_CN_CHECK, 1);
            }

            if (connectionInfo.SkipRevocationCheck)
            {
                SetWSManSessionOption(WSManNativeApi.WSManSessionOption.WSMAN_OPTION_SKIP_REVOCATION_CHECK, 1);
            }

            if (connectionInfo.IncludePortInSPN)
            {
                SetWSManSessionOption(WSManNativeApi.WSManSessionOption.WSMAN_OPTION_ENABLE_SPN_SERVER_PORT, 1);
            }

            // Set use interactive token flag based on EnableNetworkAccess property.
            SetWSManSessionOption(WSManNativeApi.WSManSessionOption.WSMAN_OPTION_USE_INTERACTIVE_TOKEN,
                (connectionInfo.EnableNetworkAccess) ? 1 : 0);

            // set UI Culture for this session from current thread's UI Culture
            string currentUICulture = connectionInfo.UICulture.Name;
            if (!string.IsNullOrEmpty(currentUICulture))
            {
                // WSMan API cannot handle empty culture names
                SetWSManSessionOption(WSManNativeApi.WSManSessionOption.WSMAN_OPTION_UI_LANGUAGE, currentUICulture);
            }

            // set Culture for this session from current thread's Culture
            string currentCulture = connectionInfo.Culture.Name;
            if (!string.IsNullOrEmpty(currentCulture))
            {
                SetWSManSessionOption(WSManNativeApi.WSManSessionOption.WSMAN_OPTION_LOCALE, currentCulture);
            }

            // set the PowerShell specific default client timeouts
            SetDefaultTimeOut(connectionInfo.OperationTimeout);
            SetConnectTimeOut(connectionInfo.OpenTimeout);
            SetCloseTimeOut(connectionInfo.CancelTimeout);
            SetSignalTimeOut(connectionInfo.CancelTimeout);
        }

        /// <summary>
        /// Handle transport error - calls EnqueueAndStartProcessingThread to process transport exception
        /// in a different thread
        /// Logic in transport callbacks should always use this to process a transport error.
        /// </summary>
        internal void ProcessWSManTransportError(TransportErrorOccuredEventArgs eventArgs)
        {
            EnqueueAndStartProcessingThread(null, eventArgs, null);
        }

        /// <summary>
        /// Log the error message in the Crimson logger and raise error handler.
        /// </summary>
        /// <param name="eventArgs"></param>
        public override void RaiseErrorHandler(TransportErrorOccuredEventArgs eventArgs)
        {
            // Look for a valid stack trace.
            string stackTrace;
            if (!string.IsNullOrEmpty(eventArgs.Exception.StackTrace))
            {
                stackTrace = eventArgs.Exception.StackTrace;
            }
            else if (eventArgs.Exception.InnerException != null &&
                     !string.IsNullOrEmpty(eventArgs.Exception.InnerException.StackTrace))
            {
                stackTrace = eventArgs.Exception.InnerException.StackTrace;
            }
            else
            {
                stackTrace = string.Empty;
            }

            // Write errors into both Operational and Analytical channels
            PSEtwLog.LogOperationalError(
                PSEventId.TransportError, PSOpcode.Open, PSTask.None, PSKeyword.UseAlwaysOperational,
                RunspacePoolInstanceId.ToString(),
                Guid.Empty.ToString(),
                eventArgs.Exception.ErrorCode.ToString(CultureInfo.InvariantCulture),
                eventArgs.Exception.Message,
                stackTrace);

            PSEtwLog.LogAnalyticError(
                PSEventId.TransportError_Analytic,
                PSOpcode.Open, PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                RunspacePoolInstanceId.ToString(),
                Guid.Empty.ToString(),
                eventArgs.Exception.ErrorCode.ToString(CultureInfo.InvariantCulture),
                eventArgs.Exception.Message,
                stackTrace);

            base.RaiseErrorHandler(eventArgs);
        }

        /// <summary>
        /// Receive/send operation handles and callback handles should be released/disposed from
        /// receive/send callback only. Releasing them after CloseOperation() may not cover all
        /// the scenarios, as WSMan does not guarantee that a rcv/send callback is not called after
        /// Close completed callback.
        /// </summary>
        /// <param name="flags"></param>
        /// <param name="shouldClearSend"></param>
        internal void ClearReceiveOrSendResources(int flags, bool shouldClearSend)
        {
            if (shouldClearSend)
            {
                if (_sendToRemoteCompleted != null)
                {
                    _sendToRemoteCompleted.Dispose();
                    _sendToRemoteCompleted = null;
                }

                // For send..clear always
                if (_wsManSendOperationHandle != IntPtr.Zero)
                {
                    WSManNativeApi.WSManCloseOperation(_wsManSendOperationHandle, 0);
                    _wsManSendOperationHandle = IntPtr.Zero;
                }
            }
            else
            {
                // clearing for receive..Clear only when the end of operation is reached.
                if (flags == (int)WSManNativeApi.WSManCallbackFlags.WSMAN_FLAG_CALLBACK_END_OF_OPERATION)
                {
                    if (_wsManReceiveOperationHandle != IntPtr.Zero)
                    {
                        WSManNativeApi.WSManCloseOperation(_wsManReceiveOperationHandle, 0);
                        _wsManReceiveOperationHandle = IntPtr.Zero;
                    }

                    if (_receivedFromRemote != null)
                    {
                        _receivedFromRemote.Dispose();
                        _receivedFromRemote = null;
                    }
                }
            }
        }

        /// <summary>
        /// Call back from worker thread / queue to raise Robust Connection notification event.
        /// </summary>
        /// <param name="privateData">ConnectionStatusEventArgs.</param>
        internal override void ProcessPrivateData(object privateData)
        {
            // Raise the Robust
            ConnectionStatusEventArgs rcArgs = privateData as ConnectionStatusEventArgs;
            if (rcArgs != null)
            {
                RaiseRobustConnectionNotification(rcArgs);
                return;
            }

            CompletionEventArgs completionArgs = privateData as CompletionEventArgs;
            if (completionArgs != null)
            {
                switch (completionArgs.Notification)
                {
                    case CompletionNotification.DisconnectCompleted:
                        RaiseDisconnectCompleted();
                        break;

                    default:
                        Dbg.Assert(false, "Currently only DisconnectCompleted notification is handled on the worker thread queue.");
                        break;
                }

                return;
            }

            Dbg.Assert(false, "Worker thread callback should always have ConnectionStatusEventArgs or CompletionEventArgs type for privateData.");
        }

        /// <summary>
        /// Robust connection maximum retry time in milliseconds.
        /// </summary>
        internal int MaxRetryConnectionTime
        {
            get { return _maxRetryTime; }
        }

        /// <summary>
        /// Returns the WSMan's session handle that this Session transportmanager
        /// is proxying.
        /// </summary>
        internal IntPtr SessionHandle
        {
            get { return _wsManSessionHandle; }
        }

        /// <summary>
        /// Returns the WSManConnectionInfo used to make the connection.
        /// </summary>
        internal WSManConnectionInfo ConnectionInfo { get; private set; }

        /// <summary>
        /// Examine the session create error code and if the error is one where a
        /// session create/connect retry attempt may be beneficial then do the
        /// retry attempt.
        /// </summary>
        /// <param name="sessionCreateErrorCode">Error code returned from Create response.</param>
        /// <returns>True if a session create retry has been started.</returns>
        private bool RetrySessionCreation(int sessionCreateErrorCode)
        {
            if (_connectionRetryCount >= ConnectionInfo.MaxConnectionRetryCount) { return false; }

            bool retryConnect;
            switch (sessionCreateErrorCode)
            {
                // Continue with connect retry for these errors.
                case WSManNativeApi.ERROR_WSMAN_SENDDATA_CANNOT_CONNECT:
                case WSManNativeApi.ERROR_WSMAN_OPERATION_ABORTED:
                case WSManNativeApi.ERROR_WSMAN_IMPROPER_RESPONSE:
                case WSManNativeApi.ERROR_WSMAN_URL_NOTAVAILABLE:
                case WSManNativeApi.ERROR_WSMAN_CANNOT_CONNECT_INVALID:
                case WSManNativeApi.ERROR_WSMAN_CANNOT_CONNECT_MISMATCH:
                case WSManNativeApi.ERROR_WSMAN_HTTP_SERVICE_UNAVAILABLE:
                case WSManNativeApi.ERROR_WSMAN_HTTP_SERVICE_ERROR:
                    retryConnect = true;
                    break;

                // For any other errors don't do connect retry.
                default:
                    retryConnect = false;
                    break;
            }

            if (retryConnect)
            {
                ++_connectionRetryCount;

                // Write trace output
                tracer.WriteLine("Attempting session creation retry {0} for error code {1} on session Id {2}",
                    _connectionRetryCount, sessionCreateErrorCode, RunspacePoolInstanceId);

                // Create ETW log entry
                PSEtwLog.LogOperationalInformation(
                    PSEventId.RetrySessionCreation, PSOpcode.Open, PSTask.None,
                    PSKeyword.UseAlwaysOperational,
                    _connectionRetryCount.ToString(CultureInfo.InvariantCulture),
                    sessionCreateErrorCode.ToString(CultureInfo.InvariantCulture),
                    RunspacePoolInstanceId.ToString());

                // Use worker pool thread to initiate retry, since WSMan does not allow method
                // calls on its own call back thread.
                System.Threading.ThreadPool.QueueUserWorkItem(StartCreateRetry);
            }

            return retryConnect;
        }

        private void StartCreateRetry(object state)
        {
            // Begin new session create attempt.
            _startMode = WSManTransportManagerUtils.tmStartModes.None;
            CreateAsync();
        }

        #endregion

        #region Static Callbacks from WSMan

        // callback that gets called when createshellex returns.
        private static void OnCreateSessionCallback(IntPtr operationContext,
            int flags,
            IntPtr error,
            IntPtr shellOperationHandle,
            IntPtr commandOperationHandle,
            IntPtr operationHandle,
            IntPtr data)
        {
            tracer.WriteLine("Client Session TM: CreateShell callback received");

            long sessionTMHandle = 0;
            WSManClientSessionTransportManager sessionTM = null;
            if (!TryGetSessionTransportManager(operationContext, out sessionTM, out sessionTMHandle))
            {
                // We dont have the session TM handle..just return.
                tracer.WriteLine("Unable to find a transport manager for context {0}.", sessionTMHandle);
                return;
            }

            // This callback is also used for robust connection notifications.  Check for and
            // handle these notifications here.
            if (HandleRobustConnectionCallback(flags, sessionTM))
            {
                return;
            }

            PSEtwLog.LogAnalyticInformational(
                PSEventId.WSManCreateShellCallbackReceived,
                PSOpcode.Connect,
                PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                sessionTM.RunspacePoolInstanceId.ToString());

            // TODO: 188098 wsManShellOperationHandle should be populated by WSManCreateShellEx,
            // but there is a thread timing bug in WSMan layer causing the callback to
            // be called before WSManCreateShellEx returns. since we already validated the
            // shell context exists, safely assigning the shellOperationHandle to shell transport manager.
            // Remove this once WSMan fixes its code.
            sessionTM._wsManShellOperationHandle = shellOperationHandle;

            lock (sessionTM.syncObject)
            {
                // Already close request is made. So return
                if (sessionTM.isClosed)
                {
                    return;
                }
            }

            if (error != IntPtr.Zero)
            {
                WSManNativeApi.WSManError errorStruct = WSManNativeApi.WSManError.UnMarshal(error);

                if (errorStruct.errorCode != 0)
                {
                    tracer.WriteLine("Got error with error code {0}. Message {1}", errorStruct.errorCode.ToString(), errorStruct.errorDetail);

                    // Test error code for possible session connection retry.
                    if (sessionTM.RetrySessionCreation(errorStruct.errorCode))
                    {
                        // If a connection retry is being attempted (on
                        // another thread) then return without processing error.
                        return;
                    }

                    TransportErrorOccuredEventArgs eventargs = WSManTransportManagerUtils.ConstructTransportErrorEventArgs(
                        sessionTM.WSManAPIData.WSManAPIHandle,
                        sessionTM,
                        errorStruct,
                        TransportMethodEnum.CreateShellEx,
                        RemotingErrorIdStrings.ConnectExCallBackError,
                        new object[] { sessionTM.ConnectionInfo.ComputerName, WSManTransportManagerUtils.ParseEscapeWSManErrorMessage(errorStruct.errorDetail) });
                    sessionTM.ProcessWSManTransportError(eventargs);

                    return;
                }
            }

            // check if the session supports disconnect
            sessionTM.SupportsDisconnect = (flags & (int)WSManNativeApi.WSManCallbackFlags.WSMAN_FLAG_CALLBACK_SHELL_SUPPORTS_DISCONNECT) != 0;

            // openContent is used by redirection ie., while redirecting to
            // a new machine.. this is not needed anymore as the connection
            // is successfully established.
            if (sessionTM._openContent != null)
            {
                sessionTM._openContent.Dispose();
                sessionTM._openContent = null;
            }

            if (data != IntPtr.Zero)
            {
                WSManNativeApi.WSManCreateShellDataResult shellData = WSManNativeApi.WSManCreateShellDataResult.UnMarshal(data);
                if (shellData.data != null)
                {
                    string returnXml = shellData.data;

                    sessionTM.ProcessShellData(returnXml);
                }
            }

            lock (sessionTM.syncObject)
            {
                // make sure the transport is not closed yet.
                if (sessionTM.isClosed)
                {
                    tracer.WriteLine("Client Session TM: Transport manager is closed. So returning");
                    return;
                }

                // Successfully made a connection. Now report this by raising the CreateCompleted event.
                // Pass updated connection information to event.
                sessionTM.RaiseCreateCompleted(
                    new CreateCompleteEventArgs(sessionTM.ConnectionInfo.Copy()));

                // Since create shell is successful, put a receive request.
                sessionTM.StartReceivingData();
            }
            // Start sending data if any.
            sessionTM.SendOneItem();
        }

        // callback that gets called when closeshellex returns.
        private static void OnCloseSessionCompleted(IntPtr operationContext,
            int flags,
            IntPtr error,
            IntPtr shellOperationHandle,
            IntPtr commandOperationHandle,
            IntPtr operationHandle,
            IntPtr data)
        {
            tracer.WriteLine("Client Session TM: CloseShell callback received");

            long sessionTMHandle = 0;
            WSManClientSessionTransportManager sessionTM = null;
            if (!TryGetSessionTransportManager(operationContext, out sessionTM, out sessionTMHandle))
            {
                // We dont have the session TM handle..just return.
                tracer.WriteLine("Unable to find a transport manager for context {0}.", sessionTMHandle);
                return;
            }

            PSEtwLog.LogAnalyticInformational(
                PSEventId.WSManCloseShellCallbackReceived,
                PSOpcode.Disconnect,
                PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                sessionTM.RunspacePoolInstanceId.ToString(),
                "OnCloseSessionCompleted");

            if (error != IntPtr.Zero)
            {
                WSManNativeApi.WSManError errorStruct = WSManNativeApi.WSManError.UnMarshal(error);

                if (errorStruct.errorCode != 0)
                {
                    tracer.WriteLine("Got error with error code {0}. Message {1}", errorStruct.errorCode.ToString(), errorStruct.errorDetail);

                    TransportErrorOccuredEventArgs eventargs = WSManTransportManagerUtils.ConstructTransportErrorEventArgs(
                        sessionTM.WSManAPIData.WSManAPIHandle,
                        sessionTM,
                        errorStruct,
                        TransportMethodEnum.CloseShellOperationEx,
                        RemotingErrorIdStrings.CloseExCallBackError,
                        new object[] { WSManTransportManagerUtils.ParseEscapeWSManErrorMessage(errorStruct.errorDetail) });
                    sessionTM.RaiseErrorHandler(eventargs);

                    return;
                }
            }

            sessionTM.RaiseCloseCompleted();
        }

        private static void OnRemoteSessionDisconnectCompleted(IntPtr operationContext,
            int flags,
            IntPtr error,
            IntPtr shellOperationHandle,
            IntPtr commandOperationHandle,
            IntPtr operationHandle,
            IntPtr data)
        {
            tracer.WriteLine("Client Session TM: CreateShell callback received");

            long sessionTMHandle = 0;
            WSManClientSessionTransportManager sessionTM = null;
            if (!TryGetSessionTransportManager(operationContext, out sessionTM, out sessionTMHandle))
            {
                // We dont have the session TM handle..just return.
                tracer.WriteLine("Unable to find a transport manager for context {0}.", sessionTMHandle);
                return;
            }

            // LOG ETW EVENTS
            PSEtwLog.LogAnalyticInformational(
                PSEventId.WSManCloseShellCallbackReceived,
                PSOpcode.Disconnect,
                PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                sessionTM.RunspacePoolInstanceId.ToString(),
                "OnRemoteSessionDisconnectCompleted");

            // Dispose the OnDisconnect callback as it is not needed anymore
            if (sessionTM._disconnectSessionCompleted != null)
            {
                sessionTM._disconnectSessionCompleted.Dispose();
                sessionTM._disconnectSessionCompleted = null;
            }

            if (error != IntPtr.Zero)
            {
                WSManNativeApi.WSManError errorStruct = WSManNativeApi.WSManError.UnMarshal(error);

                if (errorStruct.errorCode != 0)
                {
                    tracer.WriteLine("Got error with error code {0}. Message {1}", errorStruct.errorCode.ToString(), errorStruct.errorDetail);

                    TransportErrorOccuredEventArgs eventargs = WSManTransportManagerUtils.ConstructTransportErrorEventArgs(
                        sessionTM.WSManAPIData.WSManAPIHandle,
                        sessionTM,
                        errorStruct,
                        TransportMethodEnum.DisconnectShellEx,
                        RemotingErrorIdStrings.DisconnectShellExFailed,
                        new object[] { sessionTM.ConnectionInfo.ComputerName, WSManTransportManagerUtils.ParseEscapeWSManErrorMessage(errorStruct.errorDetail) });
                    sessionTM.ProcessWSManTransportError(eventargs);

                    return;
                }
            }

            lock (sessionTM.syncObject)
            {
                // make sure the transport is not closed yet.
                if (sessionTM.isClosed)
                {
                    tracer.WriteLine("Client Session TM: Transport manager is closed. So returning");
                    return;
                }

                // successfully made a connection. Now report this by raising the ConnectCompleted event.
                sessionTM.EnqueueAndStartProcessingThread(null, null,
                    new CompletionEventArgs(CompletionNotification.DisconnectCompleted));

                // Log ETW traces                
                PSEtwLog.LogAnalyticInformational(
                    PSEventId.WSManCloseShellCallbackReceived,
                    PSOpcode.Disconnect,
                    PSTask.None,
                    PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                    sessionTM.RunspacePoolInstanceId.ToString(),
                    "OnRemoteSessionReconnectCompleted: DisconnectCompleted");
            }

            return;
        }

        private static void OnRemoteSessionReconnectCompleted(IntPtr operationContext,
            int flags,
            IntPtr error,
            IntPtr shellOperationHandle,
            IntPtr commandOperationHandle,
            IntPtr operationHandle,
            IntPtr data)
        {
            tracer.WriteLine("Client Session TM: CreateShell callback received");

            long sessionTMHandle = 0;
            WSManClientSessionTransportManager sessionTM = null;
            if (!TryGetSessionTransportManager(operationContext, out sessionTM, out sessionTMHandle))
            {
                // We dont have the session TM handle..just return.
                tracer.WriteLine("Unable to find a transport manager for context {0}.", sessionTMHandle);
                return;
            }

            // Add ETW events
            PSEtwLog.LogAnalyticInformational(
                PSEventId.WSManCloseShellCallbackReceived,
                PSOpcode.Disconnect,
                PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                sessionTM.RunspacePoolInstanceId.ToString(),
                "OnRemoteSessionReconnectCompleted");

            // Dispose the OnCreate callback as it is not needed anymore
            if (sessionTM._reconnectSessionCompleted != null)
            {
                sessionTM._reconnectSessionCompleted.Dispose();
                sessionTM._reconnectSessionCompleted = null;
            }

            if (error != IntPtr.Zero)
            {
                WSManNativeApi.WSManError errorStruct = WSManNativeApi.WSManError.UnMarshal(error);

                if (errorStruct.errorCode != 0)
                {
                    tracer.WriteLine("Got error with error code {0}. Message {1}", errorStruct.errorCode.ToString(), errorStruct.errorDetail);

                    TransportErrorOccuredEventArgs eventargs = WSManTransportManagerUtils.ConstructTransportErrorEventArgs(
                        sessionTM.WSManAPIData.WSManAPIHandle,
                        sessionTM,
                        errorStruct,
                        TransportMethodEnum.ReconnectShellEx,
                        RemotingErrorIdStrings.ReconnectShellExCallBackErrr,
                        new object[] { sessionTM.ConnectionInfo.ComputerName, WSManTransportManagerUtils.ParseEscapeWSManErrorMessage(errorStruct.errorDetail) });
                    sessionTM.ProcessWSManTransportError(eventargs);

                    return;
                }
            }

            lock (sessionTM.syncObject)
            {
                // make sure the transport is not closed yet.
                if (sessionTM.isClosed)
                {
                    tracer.WriteLine("Client Session TM: Transport manager is closed. So returning");
                    return;
                }

                // successfully made a connection. Now report this by raising the ConnectCompleted event.
                sessionTM.RaiseReconnectCompleted();
            }
        }

        private static bool HandleRobustConnectionCallback(int flags, WSManClientSessionTransportManager sessionTM)
        {
            if (flags != (int)WSManNativeApi.WSManCallbackFlags.WSMAN_FLAG_CALLBACK_SHELL_AUTODISCONNECTED &&
                flags != (int)WSManNativeApi.WSManCallbackFlags.WSMAN_FLAG_CALLBACK_NETWORK_FAILURE_DETECTED &&
                flags != (int)WSManNativeApi.WSManCallbackFlags.WSMAN_FLAG_CALLBACK_RETRYING_AFTER_NETWORK_FAILURE &&
                flags != (int)WSManNativeApi.WSManCallbackFlags.WSMAN_FLAG_CALLBACK_RECONNECTED_AFTER_NETWORK_FAILURE &&
                flags != (int)WSManNativeApi.WSManCallbackFlags.WSMAN_FLAG_CALLBACK_SHELL_AUTODISCONNECTING &&
                flags != (int)WSManNativeApi.WSManCallbackFlags.WSMAN_FLAG_CALLBACK_RETRY_ABORTED_DUE_TO_INTERNAL_ERROR)
            {
                return false;
            }

            // Raise transport event notifying start of robust connections.
            if (flags == (int)WSManNativeApi.WSManCallbackFlags.WSMAN_FLAG_CALLBACK_NETWORK_FAILURE_DETECTED)
            {
                try
                {
                    sessionTM.RobustConnectionsInitiated.SafeInvoke(sessionTM, EventArgs.Empty);
                }
                catch (ObjectDisposedException)
                { }
            }

            // Send robust notification to client.
            sessionTM.QueueRobustConnectionNotification(flags);

            // Raise transport event notifying completion of robust connections.
            if (flags == (int)WSManNativeApi.WSManCallbackFlags.WSMAN_FLAG_CALLBACK_SHELL_AUTODISCONNECTED ||
                flags == (int)WSManNativeApi.WSManCallbackFlags.WSMAN_FLAG_CALLBACK_RECONNECTED_AFTER_NETWORK_FAILURE ||
                flags == (int)WSManNativeApi.WSManCallbackFlags.WSMAN_FLAG_CALLBACK_RETRY_ABORTED_DUE_TO_INTERNAL_ERROR)
            {
                try
                {
                    sessionTM.RobustConnectionsCompleted.SafeInvoke(sessionTM, EventArgs.Empty);
                }
                catch (ObjectDisposedException)
                { }
            }

            return true;
        }

        private static void OnRemoteSessionConnectCallback(IntPtr operationContext,
            int flags,
            IntPtr error,
            IntPtr shellOperationHandle,
            IntPtr commandOperationHandle,
            IntPtr operationHandle,
            IntPtr data)
        {
            tracer.WriteLine("Client Session TM: Connect callback received");

            PSEtwLog.LogAnalyticInformational(
                PSEventId.WSManSendShellInputExCallbackReceived,
                PSOpcode.Connect,
                PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                "OnRemoteSessionConnectCallback:Client Session TM: Connect callback received");

            long sessionTMHandle = 0;
            WSManClientSessionTransportManager sessionTM = null;
            if (!TryGetSessionTransportManager(operationContext, out sessionTM, out sessionTMHandle))
            {
                // We dont have the session TM handle..just return.
                tracer.WriteLine("Unable to find a transport manager for context {0}.", sessionTMHandle);
                return;
            }

            // This callback is also used for robust connection notifications.  Check for and
            // handle these notifications here.
            if (HandleRobustConnectionCallback(flags, sessionTM))
            {
                return;
            }

            if (error != IntPtr.Zero)
            {
                WSManNativeApi.WSManError errorStruct = WSManNativeApi.WSManError.UnMarshal(error);

                if (errorStruct.errorCode != 0)
                {
                    tracer.WriteLine("Got error with error code {0}. Message {1}", errorStruct.errorCode.ToString(), errorStruct.errorDetail);

                    TransportErrorOccuredEventArgs eventargs = WSManTransportManagerUtils.ConstructTransportErrorEventArgs(
                        sessionTM.WSManAPIData.WSManAPIHandle,
                        sessionTM,
                        errorStruct,
                        TransportMethodEnum.ConnectShellEx,
                        RemotingErrorIdStrings.ConnectExCallBackError,
                        new object[] { sessionTM.ConnectionInfo.ComputerName, WSManTransportManagerUtils.ParseEscapeWSManErrorMessage(errorStruct.errorDetail) });
                    sessionTM.ProcessWSManTransportError(eventargs);

                    return;
                }
            }

            // dispose openContent
            if (sessionTM._openContent != null)
            {
                sessionTM._openContent.Dispose();
                sessionTM._openContent = null;
            }

            lock (sessionTM.syncObject)
            {
                // make sure the transport is not closed yet.
                if (sessionTM.isClosed)
                {
                    tracer.WriteLine("Client Session TM: Transport manager is closed. So returning");
                    return;
                }
            }

            // process returned Xml
            Dbg.Assert(data != IntPtr.Zero, "WSManConnectShell callback returned null data");
            WSManNativeApi.WSManConnectDataResult connectData = WSManNativeApi.WSManConnectDataResult.UnMarshal(data);
            if (connectData.data != null)
            {
                byte[] connectResponse = ServerOperationHelpers.ExtractEncodedXmlElement(connectData.data, WSManNativeApi.PS_CONNECTRESPONSE_XML_TAG);
                sessionTM.ProcessRawData(connectResponse, WSManNativeApi.WSMAN_STREAM_ID_STDOUT);
            }

            // Set up the data-to-send callback.
            sessionTM.SendOneItem();

            // successfully made a connection. Now report this by raising the ConnectCompleted event.
            // Microsoft's PS 3.0 Server will return all negotiation related data in one shot in connect Data
            // Note that we are not starting to receive data yet. the DSHandlers above will do that when the session
            // gets to an established state.
            sessionTM.RaiseConnectCompleted();
        }

        private static void OnRemoteSessionSendCompleted(IntPtr operationContext,
            int flags,
            IntPtr error,
            IntPtr shellOperationHandle,
            IntPtr commandOperationHandle,
            IntPtr operationHandle,
            IntPtr data)
        {
            tracer.WriteLine("Client Session TM: SendComplete callback received");

            long sessionTMHandle = 0;
            WSManClientSessionTransportManager sessionTM = null;
            if (!TryGetSessionTransportManager(operationContext, out sessionTM, out sessionTMHandle))
            {
                // We dont have the session TM handle..just return.
                tracer.WriteLine("Unable to find a transport manager for context {0}.", sessionTMHandle);
                return;
            }

            // do the logging for this send
            PSEtwLog.LogAnalyticInformational(
                PSEventId.WSManSendShellInputExCallbackReceived,
                PSOpcode.Connect,
                PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                sessionTM.RunspacePoolInstanceId.ToString(),
                Guid.Empty.ToString());

            if (!shellOperationHandle.Equals(sessionTM._wsManShellOperationHandle))
            {
                // WSMan returned data from a wrong shell..notify the caller
                // about the same.
                PSRemotingTransportException e = new PSRemotingTransportException(
                    PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.SendExFailed, sessionTM.ConnectionInfo.ComputerName));
                TransportErrorOccuredEventArgs eventargs =
                    new TransportErrorOccuredEventArgs(e, TransportMethodEnum.SendShellInputEx);
                sessionTM.ProcessWSManTransportError(eventargs);

                return;
            }

            sessionTM.ClearReceiveOrSendResources(flags, true);

            // if the session is already closed ignore the errors and return.
            if (sessionTM.isClosed)
            {
                tracer.WriteLine("Client Session TM: Transport manager is closed. So returning");
                return;
            }

            if (error != IntPtr.Zero)
            {
                WSManNativeApi.WSManError errorStruct = WSManNativeApi.WSManError.UnMarshal(error);

                // Ignore operation aborted error. operation aborted is raised by WSMan to
                // notify operation complete. PowerShell protocol has its own
                // way of notifying the same using state change events.
                if ((errorStruct.errorCode != 0) && (errorStruct.errorCode != 995))
                {
                    tracer.WriteLine("Got error with error code {0}. Message {1}", errorStruct.errorCode.ToString(), errorStruct.errorDetail);

                    TransportErrorOccuredEventArgs eventargs = WSManTransportManagerUtils.ConstructTransportErrorEventArgs(
                        sessionTM.WSManAPIData.WSManAPIHandle,
                        sessionTM,
                        errorStruct,
                        TransportMethodEnum.SendShellInputEx,
                        RemotingErrorIdStrings.SendExCallBackError,
                        new object[] { sessionTM.ConnectionInfo.ComputerName, WSManTransportManagerUtils.ParseEscapeWSManErrorMessage(errorStruct.errorDetail) });
                    sessionTM.ProcessWSManTransportError(eventargs);

                    return;
                }
            }

            // Send the next item, if available
            sessionTM.SendOneItem();
        }

        // WSMan will make sure this callback is synchronously called ie., if 1 callback
        // is active, the callback will not be called from a different thread.
        private static void OnRemoteSessionDataReceived(IntPtr operationContext,
            int flags,
            IntPtr error,
            IntPtr shellOperationHandle,
            IntPtr commandOperationHandle,
            IntPtr operationHandle,
            IntPtr data)
        {
            tracer.WriteLine("Client Session TM: OnRemoteDataReceived callback.");

            long sessionTMHandle = 0;
            WSManClientSessionTransportManager sessionTM = null;
            if (!TryGetSessionTransportManager(operationContext, out sessionTM, out sessionTMHandle))
            {
                // We dont have the session TM handle..just return.
                tracer.WriteLine("Unable to find a transport manager for context {0}.", sessionTMHandle);
                return;
            }

            sessionTM.ClearReceiveOrSendResources(flags, false);

            if (sessionTM.isClosed)
            {
                tracer.WriteLine("Client Session TM: Transport manager is closed. So returning");
                return;
            }

            if (!shellOperationHandle.Equals(sessionTM._wsManShellOperationHandle))
            {
                // WSMan returned data from a wrong shell..notify the caller
                // about the same.
                PSRemotingTransportException e = new PSRemotingTransportException(
                    PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.ReceiveExFailed, sessionTM.ConnectionInfo.ComputerName));
                TransportErrorOccuredEventArgs eventargs =
                    new TransportErrorOccuredEventArgs(e, TransportMethodEnum.ReceiveShellOutputEx);
                sessionTM.ProcessWSManTransportError(eventargs);

                return;
            }

            if (error != IntPtr.Zero)
            {
                WSManNativeApi.WSManError errorStruct = WSManNativeApi.WSManError.UnMarshal(error);

                if (errorStruct.errorCode != 0)
                {
                    tracer.WriteLine("Got error with error code {0}. Message {1}", errorStruct.errorCode.ToString(), errorStruct.errorDetail);

                    TransportErrorOccuredEventArgs eventargs = WSManTransportManagerUtils.ConstructTransportErrorEventArgs(
                        sessionTM.WSManAPIData.WSManAPIHandle,
                        sessionTM,
                        errorStruct,
                        TransportMethodEnum.ReceiveShellOutputEx,
                        RemotingErrorIdStrings.ReceiveExCallBackError,
                        new object[] { sessionTM.ConnectionInfo.ComputerName, WSManTransportManagerUtils.ParseEscapeWSManErrorMessage(errorStruct.errorDetail) });
                    sessionTM.ProcessWSManTransportError(eventargs);

                    return;
                }
            }

            WSManNativeApi.WSManReceiveDataResult dataReceived = WSManNativeApi.WSManReceiveDataResult.UnMarshal(data);
            if (dataReceived.data != null)
            {
                tracer.WriteLine("Session Received Data : {0}", dataReceived.data.Length);
                PSEtwLog.LogAnalyticInformational(
                    PSEventId.WSManReceiveShellOutputExCallbackReceived, PSOpcode.Receive, PSTask.None,
                    PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                    sessionTM.RunspacePoolInstanceId.ToString(),
                    Guid.Empty.ToString(),
                    dataReceived.data.Length.ToString(CultureInfo.InvariantCulture));
                sessionTM.ProcessRawData(dataReceived.data, dataReceived.stream);
            }
        }

        #endregion

        #region Send Data Handling

        private void SendOneItem()
        {
            DataPriorityType priorityType;
            // This will either return data or register callback but doesn't do both.
            byte[] data = dataToBeSent.ReadOrRegisterCallback(_onDataAvailableToSendCallback,
                out priorityType);
            if (data != null)
            {
                SendData(data, priorityType);
            }
        }

        private void OnDataAvailableCallback(byte[] data, DataPriorityType priorityType)
        {
            Dbg.Assert(data != null, "data cannot be null in the data available callback");

            tracer.WriteLine("Received data to be sent from the callback.");
            SendData(data, priorityType);
        }

        private void SendData(byte[] data, DataPriorityType priorityType)
        {
            tracer.WriteLine("Session sending data of size : {0}", data.Length);
            byte[] package = data;

            #region SHIM: Redirection code for session data send.

            bool sendContinue = true;

            if (s_sessionSendRedirect != null)
            {
                object[] arguments = new object[2] { null, package };
                sendContinue = (bool)s_sessionSendRedirect.DynamicInvoke(arguments);
                package = (byte[])arguments[0];
            }

            if (!sendContinue)
                return;

            #endregion

            using (WSManNativeApi.WSManData_ManToUn serializedContent =
                         new WSManNativeApi.WSManData_ManToUn(package))
            {
                PSEtwLog.LogAnalyticInformational(
                    PSEventId.WSManSendShellInputEx, PSOpcode.Send, PSTask.None,
                    PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                    RunspacePoolInstanceId.ToString(),
                    Guid.Empty.ToString(),
                    serializedContent.BufferLength.ToString(CultureInfo.InvariantCulture));

                lock (syncObject)
                {
                    // make sure the transport is not closed.
                    if (isClosed)
                    {
                        tracer.WriteLine("Client Session TM: Transport manager is closed. So returning");
                        return;
                    }

                    // send callback
                    _sendToRemoteCompleted = new WSManNativeApi.WSManShellAsync(new IntPtr(_sessionContextID), s_sessionSendCallback);
                    WSManNativeApi.WSManSendShellInputEx(_wsManShellOperationHandle, IntPtr.Zero, 0,
                        priorityType == DataPriorityType.Default ?
                            WSManNativeApi.WSMAN_STREAM_ID_STDIN : WSManNativeApi.WSMAN_STREAM_ID_PROMPTRESPONSE,
                        serializedContent,
                        _sendToRemoteCompleted,
                        ref _wsManSendOperationHandle);
                }
            }
        }

        #endregion

        #region Dispose / Destructor pattern

        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed")]
        protected override void Dispose(bool isDisposing)
        {
            tracer.WriteLine("Disposing session with session context: {0} Operation Context: {1}", _sessionContextID, _wsManShellOperationHandle);

            CloseSessionAndClearResources();

            DisposeWSManAPIDataAsync();

            // openContent is used by redirection ie., while redirecting to
            // a new machine and hence this is cleared only when the session
            // is disposing.
            if (isDisposing && (_openContent != null))
            {
                _openContent.Dispose();
                _openContent = null;
            }

            base.Dispose(isDisposing);
        }

        /// <summary>
        /// Closes current session handle by calling WSManCloseSession and clears
        /// session related resources.
        /// </summary>
        private void CloseSessionAndClearResources()
        {
            tracer.WriteLine("Clearing session with session context: {0} Operation Context: {1}", _sessionContextID, _wsManShellOperationHandle);

            // Taking a copy of session handle as we should call WSManCloseSession only once and
            // clear the original value. This will protect us if Dispose() is called twice.
            IntPtr tempWSManSessionHandle = _wsManSessionHandle;
            _wsManSessionHandle = IntPtr.Zero;
            // Call WSManCloseSession on a different thread as Dispose can be called from one of
            // the WSMan callback threads. WSMan does not support closing a session in the callbacks.
            ThreadPool.QueueUserWorkItem(new WaitCallback(
                // wsManSessionHandle is passed as parameter to allow the thread to be independent
                // of the rest of the parent object.
                (object state) =>
                {
                    IntPtr sessionHandle = (IntPtr)state;
                    if (sessionHandle != IntPtr.Zero)
                    {
                        WSManNativeApi.WSManCloseSession(sessionHandle, 0);
                    }
                }), tempWSManSessionHandle);

            // remove session context from session handles dictionary
            RemoveSessionTransportManager(_sessionContextID);

            if (_closeSessionCompleted != null)
            {
                _closeSessionCompleted.Dispose();
                _closeSessionCompleted = null;
            }

            // Dispose the create session completed callback here, since it is
            // used for periodic robust connection retry/auto-disconnect
            // notifications while the shell is active.
            if (_createSessionCallback != null)
            {
                _createSessionCallbackGCHandle.Free();
                _createSessionCallback.Dispose();
                _createSessionCallback = null;
            }

            // Dispose the OnConnect callback if one present
            if (_connectSessionCallback != null)
            {
                _connectSessionCallback.Dispose();
                _connectSessionCallback = null;
            }

            // Reset the session context Id to zero so that a new one will be generated for
            // any following redirected session.
            _sessionContextID = 0;
        }

        private void DisposeWSManAPIDataAsync()
        {
            WSManAPIDataCommon tempWSManApiData = WSManAPIData;
            if (tempWSManApiData == null) { return; }

            WSManAPIData = null;

            // Dispose and de-initialize the WSManAPIData instance object on separate worker thread to ensure
            // it is not run on a WinRM thread (which will fail).
            // Note that WSManAPIData.Dispose() method is thread safe.
            ThreadPool.QueueUserWorkItem((_) => tempWSManApiData.Dispose());
        }

        #endregion

        #region WSManAPIDataCommon

        /// <summary>
        /// Class that manages WSManAPI data. Has information like APIHandle which is created
        /// using WSManInitialize, InputStreamSet, OutputStreamSet.
        /// </summary>
        internal class WSManAPIDataCommon : IDisposable
        {
            [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
            private IntPtr _handle;
            // if any
            private WSManNativeApi.WSManStreamIDSet_ManToUn _inputStreamSet;
            private WSManNativeApi.WSManStreamIDSet_ManToUn _outputStreamSet;
            // Dispose
            private bool _isDisposed;
            private readonly object _syncObject = new object();
#if !UNIX
            private readonly WindowsIdentity _identityToImpersonate;
#endif

            /// <summary>
            /// Initializes handle by calling WSManInitialize API.
            /// </summary>
            internal WSManAPIDataCommon()
            {
#if !UNIX
                // Check for thread impersonation and save identity for later de-initialization.
                Utils.TryGetWindowsImpersonatedIdentity(out _identityToImpersonate);
#endif

                _handle = IntPtr.Zero;

                try
                {
                    ErrorCode = WSManNativeApi.WSManInitialize(WSManNativeApi.WSMAN_FLAG_REQUESTED_API_VERSION_1_1, ref _handle);
                }
                catch (DllNotFoundException ex)
                {
                    PSEtwLog.LogOperationalError(
                        PSEventId.TransportError,
                        PSOpcode.Open,
                        PSTask.None,
                        PSKeyword.UseAlwaysOperational,
                        "WSManAPIDataCommon.ctor",
                        "WSManInitialize",
                        ex.HResult.ToString(CultureInfo.InvariantCulture),
                        ex.Message,
                        ex.StackTrace);
                    throw new PSRemotingTransportException(RemotingErrorIdStrings.WSManClientDllNotAvailable, ex);
                }

                // input / output streams common to all connections
                _inputStreamSet = new WSManNativeApi.WSManStreamIDSet_ManToUn(
                    new string[] {
                        WSManNativeApi.WSMAN_STREAM_ID_STDIN,
                        WSManNativeApi.WSMAN_STREAM_ID_PROMPTRESPONSE
                    });
                _outputStreamSet = new WSManNativeApi.WSManStreamIDSet_ManToUn(
                    new string[] { WSManNativeApi.WSMAN_STREAM_ID_STDOUT });

                // startup options common to all connections
                WSManNativeApi.WSManOption protocolStartupOption = new WSManNativeApi.WSManOption();
                protocolStartupOption.name = RemoteDataNameStrings.PS_STARTUP_PROTOCOL_VERSION_NAME;
                protocolStartupOption.value = RemotingConstants.ProtocolVersion.ToString();
                protocolStartupOption.mustComply = true;

                CommonOptionSet = new List<WSManNativeApi.WSManOption>();
                CommonOptionSet.Add(protocolStartupOption);
            }

            internal int ErrorCode { get; }

            internal WSManNativeApi.WSManStreamIDSet_ManToUn InputStreamSet { get { return _inputStreamSet; } }

            internal WSManNativeApi.WSManStreamIDSet_ManToUn OutputStreamSet { get { return _outputStreamSet; } }

            internal List<WSManNativeApi.WSManOption> CommonOptionSet { get; }

            internal IntPtr WSManAPIHandle { get { return _handle; } }

            /// <summary>
            /// Dispose.
            /// </summary>
            // Suppress this message. The result is actually used, but only in checked builds....
            [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults",
               MessageId = "System.Management.Automation.Remoting.Client.WSManNativeApi.WSManDeinitialize(System.IntPtr,System.Int32)")]
            [SuppressMessage("Microsoft.Usage", "CA2216:Disposabletypesshoulddeclarefinalizer")]
            public void Dispose()
            {
                lock (_syncObject)
                {
                    if (_isDisposed) { return; }

                    _isDisposed = true;
                }

                _inputStreamSet.Dispose();
                _outputStreamSet.Dispose();

                if (_handle != IntPtr.Zero)
                {
                    int result = 0;

#if !UNIX
                    // If we initialized with thread impersonation, make sure de-initialize is run with the same.
                    if (_identityToImpersonate != null)
                    {
                        result = WindowsIdentity.RunImpersonated(
                            _identityToImpersonate.AccessToken,
                            () => WSManNativeApi.WSManDeinitialize(_handle, 0));
                    }
                    else
                    {
#endif
                        result = WSManNativeApi.WSManDeinitialize(_handle, 0);
#if !UNIX
                    }
#endif

                    Dbg.Assert(result == 0, "WSManDeinitialize returned non-zero value");
                    _handle = IntPtr.Zero;
                }
            }
        }

        #endregion

        #region EventHandlers

        internal event EventHandler<EventArgs> RobustConnectionsInitiated;

        internal event EventHandler<EventArgs> RobustConnectionsCompleted;

        #endregion
    }

    /// <summary>
    /// A class maintaining the transport of a command for the shell. Multiple commands will have
    /// multiple transport managers. The Transport manager manages creating / sending /receiving
    /// data and closing (terminating) the command.
    /// </summary>
    internal sealed class WSManClientCommandTransportManager : BaseClientCommandTransportManager
    {
        #region Consts

        internal const string StopSignal = @"powershell/signal/crtl_c";

        #endregion

        #region Private Data

        // operation handles
        private readonly IntPtr _wsManShellOperationHandle;

        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private IntPtr _wsManCmdOperationHandle;

        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private IntPtr _cmdSignalOperationHandle;

        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private IntPtr _wsManReceiveOperationHandle;

        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private IntPtr _wsManSendOperationHandle;
        // this is used with WSMan callbacks to represent a command transport manager.
        private long _cmdContextId;

        private readonly PrioritySendDataCollection.OnDataAvailableCallback _onDataAvailableToSendCallback;

        // should be integrated with receiveDataInitiated
        private bool _shouldStartReceivingData;

        // bools used to track and send stop signal only after Create is completed.
        private bool _isCreateCallbackReceived;
        private bool _isStopSignalPending;
        private bool _isDisconnectPending;
        private bool _isSendingInput;
        private bool _isDisconnectedOnInvoke;

        // callbacks
        private WSManNativeApi.WSManShellAsync _createCmdCompleted;
        private WSManNativeApi.WSManShellAsync _receivedFromRemote;
        private WSManNativeApi.WSManShellAsync _sendToRemoteCompleted;
        private WSManNativeApi.WSManShellAsync _reconnectCmdCompleted;
        private WSManNativeApi.WSManShellAsync _connectCmdCompleted;
        // TODO: This GCHandle is required as it seems WSMan is calling create callback
        // after we call Close. This seems wrong. Opened bug on WSMan to track this.
        private GCHandle _createCmdCompletedGCHandle;
        private WSManNativeApi.WSManShellAsync _closeCmdCompleted;
        private WSManNativeApi.WSManShellAsync _signalCmdCompleted;
        // this is the chunk that got delivered on onDataAvailableToSendCallback
        // will be sent during subsequent SendOneItem()
        private SendDataChunk _chunkToSend;

        private readonly string _cmdLine;
        private readonly WSManClientSessionTransportManager _sessnTm;

        private sealed class SendDataChunk
        {
            public SendDataChunk(byte[] data, DataPriorityType type)
            {
                Data = data;
                Type = type;
            }

            public byte[] Data { get; }

            public DataPriorityType Type { get; }
        }

        #endregion

        #region Static Data

        // static callback delegate
        private static WSManNativeApi.WSManShellAsyncCallback s_cmdCreateCallback;
        private static WSManNativeApi.WSManShellAsyncCallback s_cmdCloseCallback;
        private static WSManNativeApi.WSManShellAsyncCallback s_cmdReceiveCallback;
        private static WSManNativeApi.WSManShellAsyncCallback s_cmdSendCallback;
        private static WSManNativeApi.WSManShellAsyncCallback s_cmdSignalCallback;
        private static WSManNativeApi.WSManShellAsyncCallback s_cmdReconnectCallback;
        private static WSManNativeApi.WSManShellAsyncCallback s_cmdConnectCallback;

        static WSManClientCommandTransportManager()
        {
            // Initialize callback delegates
            WSManNativeApi.WSManShellCompletionFunction createDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnCreateCmdCompleted);
            s_cmdCreateCallback = new WSManNativeApi.WSManShellAsyncCallback(createDelegate);

            WSManNativeApi.WSManShellCompletionFunction closeDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnCloseCmdCompleted);
            s_cmdCloseCallback = new WSManNativeApi.WSManShellAsyncCallback(closeDelegate);

            WSManNativeApi.WSManShellCompletionFunction receiveDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnRemoteCmdDataReceived);
            s_cmdReceiveCallback = new WSManNativeApi.WSManShellAsyncCallback(receiveDelegate);

            WSManNativeApi.WSManShellCompletionFunction sendDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnRemoteCmdSendCompleted);
            s_cmdSendCallback = new WSManNativeApi.WSManShellAsyncCallback(sendDelegate);

            WSManNativeApi.WSManShellCompletionFunction signalDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnRemoteCmdSignalCompleted);
            s_cmdSignalCallback = new WSManNativeApi.WSManShellAsyncCallback(signalDelegate);

            WSManNativeApi.WSManShellCompletionFunction reconnectDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnReconnectCmdCompleted);
            s_cmdReconnectCallback = new WSManNativeApi.WSManShellAsyncCallback(reconnectDelegate);

            WSManNativeApi.WSManShellCompletionFunction connectDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnConnectCmdCompleted);
            s_cmdConnectCallback = new WSManNativeApi.WSManShellAsyncCallback(connectDelegate);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// This is an internal constructor used by WSManClientSessionTransportManager.
        /// </summary>
        /// <param name="connectionInfo">
        /// connection info to be used for creating the command.
        /// </param>
        /// <param name="wsManShellOperationHandle">
        /// Shell operation handle in whose context this transport manager sends/receives
        /// data packets.
        /// </param>
        /// <param name="shell">
        /// The command to be sent to the remote end.
        /// </param>
        /// <param name="noInput">
        /// true if the command has input, false otherwise.
        /// </param>
        /// <param name="sessnTM">
        /// Session transport manager creating this command transport manager instance.
        /// Used by Command TM to apply session specific properties
        /// </param>
        internal WSManClientCommandTransportManager(
            WSManConnectionInfo connectionInfo,
            IntPtr wsManShellOperationHandle,
            ClientRemotePowerShell shell,
            bool noInput,
            WSManClientSessionTransportManager sessnTM)
            : base(shell, sessnTM.CryptoHelper, sessnTM)
        {
            Dbg.Assert(wsManShellOperationHandle != IntPtr.Zero, "Shell operation handle cannot be IntPtr.Zero.");
            Dbg.Assert(connectionInfo != null, "connectionInfo cannot be null");

            _wsManShellOperationHandle = wsManShellOperationHandle;

            // Apply quota limits.. allow for data to be unlimited..
            ReceivedDataCollection.MaximumReceivedDataSize = connectionInfo.MaximumReceivedDataSizePerCommand;
            ReceivedDataCollection.MaximumReceivedObjectSize = connectionInfo.MaximumReceivedObjectSize;
            _cmdLine = shell.PowerShell.Commands.Commands.GetCommandStringForHistory();
            _onDataAvailableToSendCallback =
                new PrioritySendDataCollection.OnDataAvailableCallback(OnDataAvailableCallback);
            _sessnTm = sessnTM;
            // Suspend queue on robust connections initiated event.
            sessnTM.RobustConnectionsInitiated += HandleRobustConnectionsInitiated;

            // Resume queue on robust connections completed event.
            sessnTM.RobustConnectionsCompleted += HandleRobusConnectionsCompleted;
        }

        #endregion

        #region SHIM: Redirection delegate for command code send.

        private static readonly Delegate s_commandCodeSendRedirect = null;

        #endregion

        #region Internal Methods / Properties

        private void HandleRobustConnectionsInitiated(object sender, EventArgs e)
        {
            SuspendQueue();
        }

        private void HandleRobusConnectionsCompleted(object sender, EventArgs e)
        {
            ResumeQueue();
        }

        /// <summary>
        /// </summary>
        /// <exception cref="PSRemotingTransportException">
        /// WSManConnectShellCommandEx failed.
        /// </exception>
        internal override void ConnectAsync()
        {
            Dbg.Assert(!isClosed, "object already disposed");
            ReceivedDataCollection.PrepareForStreamConnect();

            // Empty the serializedPipeline data that contains PowerShell command information created in the
            // constructor.  We are connecting to an existing command on the server and don't want to send
            // information on a new command.
            serializedPipeline.Read();

            // create cmdContextId
            _cmdContextId = GetNextCmdTMHandleId();
            AddCmdTransportManager(_cmdContextId, this);

            // Create Callback
            _connectCmdCompleted = new WSManNativeApi.WSManShellAsync(new IntPtr(_cmdContextId), s_cmdConnectCallback);
            _reconnectCmdCompleted = new WSManNativeApi.WSManShellAsync(new IntPtr(_cmdContextId), s_cmdReconnectCallback);
            lock (syncObject)
            {
                if (isClosed)
                {
                    // the transport is already closed..so no need to create a connection
                    // anymore.
                    return;
                }

                WSManNativeApi.WSManConnectShellCommandEx(_wsManShellOperationHandle,
                    0,
                    PowershellInstanceId.ToString().ToUpperInvariant(),
                    IntPtr.Zero,
                    IntPtr.Zero,
                    _connectCmdCompleted,
                    ref _wsManCmdOperationHandle);
            }

            if (_wsManCmdOperationHandle == IntPtr.Zero)
            {
                PSRemotingTransportException e = new PSRemotingTransportException(RemotingErrorIdStrings.RunShellCommandExFailed);
                TransportErrorOccuredEventArgs eventargs =
                    new TransportErrorOccuredEventArgs(e, TransportMethodEnum.ConnectShellCommandEx);
                ProcessWSManTransportError(eventargs);
                return;
            }
        }

        /// <summary>
        /// Begin connection creation.
        /// </summary>
        /// <exception cref="PSRemotingTransportException">
        /// WSManRunShellCommandEx failed.
        /// </exception>
        public override void CreateAsync()
        {
            byte[] cmdPart1 = serializedPipeline.ReadOrRegisterCallback(null);
            if (cmdPart1 != null)
            {
                #region SHIM: Redirection code for command code send.

                bool sendContinue = true;

                if (s_commandCodeSendRedirect != null)
                {
                    object[] arguments = new object[2] { null, cmdPart1 };
                    sendContinue = (bool)s_commandCodeSendRedirect.DynamicInvoke(arguments);
                    cmdPart1 = (byte[])arguments[0];
                }

                if (!sendContinue)
                    return;

                #endregion

                WSManNativeApi.WSManCommandArgSet argSet = new WSManNativeApi.WSManCommandArgSet(cmdPart1);

                // create cmdContextId
                _cmdContextId = GetNextCmdTMHandleId();
                AddCmdTransportManager(_cmdContextId, this);

                PSEtwLog.LogAnalyticInformational(
                    PSEventId.WSManCreateCommand,
                    PSOpcode.Connect,
                    PSTask.CreateRunspace,
                    PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                    RunspacePoolInstanceId.ToString(),
                    powershellInstanceId.ToString());

                _createCmdCompleted = new WSManNativeApi.WSManShellAsync(new IntPtr(_cmdContextId), s_cmdCreateCallback);
                _createCmdCompletedGCHandle = GCHandle.Alloc(_createCmdCompleted);
                _reconnectCmdCompleted = new WSManNativeApi.WSManShellAsync(new IntPtr(_cmdContextId), s_cmdReconnectCallback);

                using (argSet)
                {
                    lock (syncObject)
                    {
                        if (!isClosed)
                        {
                            WSManNativeApi.WSManRunShellCommandEx(_wsManShellOperationHandle,
                                0,
                                PowershellInstanceId.ToString().ToUpperInvariant(),
                                // WSManRunsShellCommand doesn't accept empty string "".
                                (_cmdLine == null || _cmdLine.Length == 0) ? " " : (_cmdLine.Length <= 256 ? _cmdLine : _cmdLine.Substring(0, 255)),
                                argSet,
                                IntPtr.Zero,
                                _createCmdCompleted,
                                ref _wsManCmdOperationHandle);

                            tracer.WriteLine("Started cmd with command context : {0} Operation context: {1}", _cmdContextId, _wsManCmdOperationHandle);
                        }
                    }
                }
            }

            if (_wsManCmdOperationHandle == IntPtr.Zero)
            {
                PSRemotingTransportException e = new PSRemotingTransportException(RemotingErrorIdStrings.RunShellCommandExFailed);
                TransportErrorOccuredEventArgs eventargs =
                    new TransportErrorOccuredEventArgs(e, TransportMethodEnum.RunShellCommandEx);
                ProcessWSManTransportError(eventargs);
                return;
            }
        }

        /// <summary>
        /// Restores connection on a disconnected command.
        /// </summary>
        internal override void ReconnectAsync()
        {
            ReceivedDataCollection.PrepareForStreamConnect();
            lock (syncObject)
            {
                if (isClosed)
                {
                    return;
                }

                WSManNativeApi.WSManReconnectShellCommandEx(_wsManCmdOperationHandle, 0, _reconnectCmdCompleted);
            }
        }
        /// <summary>
        /// Used by powershell/pipeline to send a stop message to the server command.
        /// </summary>
        internal override void SendStopSignal()
        {
            lock (syncObject)
            {
                if (isClosed)
                {
                    return;
                }

                // WSMan API do not allow a signal/input/receive be sent until RunShellCommand is
                // successful (ie., callback is received)..so note that a signal is to be sent
                // here and return.
                if (!_isCreateCallbackReceived)
                {
                    _isStopSignalPending = true;
                    return;
                }
                // we are about to send a signal..so clear pending bit.
                _isStopSignalPending = false;

                tracer.WriteLine("Sending stop signal with command context: {0} Operation Context {1}", _cmdContextId, _wsManCmdOperationHandle);
                PSEtwLog.LogAnalyticInformational(
                    PSEventId.WSManSignal,
                    PSOpcode.Disconnect,
                    PSTask.None,
                    PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                    RunspacePoolInstanceId.ToString(),
                    powershellInstanceId.ToString(),
                    StopSignal);

                _signalCmdCompleted = new WSManNativeApi.WSManShellAsync(new IntPtr(_cmdContextId), s_cmdSignalCallback);
                WSManNativeApi.WSManSignalShellEx(_wsManShellOperationHandle, _wsManCmdOperationHandle, 0,
                    StopSignal, _signalCmdCompleted, ref _cmdSignalOperationHandle);
            }
        }

        /// <summary>
        /// Closes the pending Create,Send,Receive operations and then closes the shell and release all the resources.
        /// </summary>
        public override void CloseAsync()
        {
            tracer.WriteLine("Closing command with command context: {0} Operation Context {1}", _cmdContextId, _wsManCmdOperationHandle);

            bool shouldRaiseCloseCompleted = false;
            // then let other threads release the lock before we cleaning up the resources.
            lock (syncObject)
            {
                if (isClosed)
                {
                    return;
                }

                // first change the state..so other threads
                // will know that we are closing.
                isClosed = true;

                // There is no valid cmd operation handle..so just
                // raise close completed.
                if (_wsManCmdOperationHandle == IntPtr.Zero)
                {
                    shouldRaiseCloseCompleted = true;
                }
            }

            base.CloseAsync();

            if (shouldRaiseCloseCompleted)
            {
                try
                {
                    RaiseCloseCompleted();
                }
                finally
                {
                    RemoveCmdTransportManager(_cmdContextId);
                }

                return;
            }

            PSEtwLog.LogAnalyticInformational(
                PSEventId.WSManCloseCommand,
                PSOpcode.Disconnect,
                PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                RunspacePoolInstanceId.ToString(),
                powershellInstanceId.ToString());
            _closeCmdCompleted = new WSManNativeApi.WSManShellAsync(new IntPtr(_cmdContextId), s_cmdCloseCallback);
            Dbg.Assert((IntPtr)_closeCmdCompleted != IntPtr.Zero, "closeCmdCompleted callback is null in cmdTM.CloseAsync()");
            WSManNativeApi.WSManCloseCommand(_wsManCmdOperationHandle, 0, _closeCmdCompleted);
        }

        /// <summary>
        /// Handle transport error - calls EnqueueAndStartProcessingThread to process transport exception
        /// in a different thread
        /// Logic in transport callbacks should always use this to process a transport error.
        /// </summary>
        internal void ProcessWSManTransportError(TransportErrorOccuredEventArgs eventArgs)
        {
            EnqueueAndStartProcessingThread(null, eventArgs, null);
        }

        /// <summary>
        /// Log the error message in the Crimson logger and raise error handler.
        /// </summary>
        /// <param name="eventArgs"></param>
        public override void RaiseErrorHandler(TransportErrorOccuredEventArgs eventArgs)
        {
            // Look for a valid stack trace.
            string stackTrace;
            if (!string.IsNullOrEmpty(eventArgs.Exception.StackTrace))
            {
                stackTrace = eventArgs.Exception.StackTrace;
            }
            else if (eventArgs.Exception.InnerException != null &&
                     !string.IsNullOrEmpty(eventArgs.Exception.InnerException.StackTrace))
            {
                stackTrace = eventArgs.Exception.InnerException.StackTrace;
            }
            else
            {
                stackTrace = string.Empty;
            }

            PSEtwLog.LogOperationalError(
                PSEventId.TransportError, PSOpcode.Open, PSTask.None,
                PSKeyword.UseAlwaysOperational,
                RunspacePoolInstanceId.ToString(),
                powershellInstanceId.ToString(),
                eventArgs.Exception.ErrorCode.ToString(CultureInfo.InvariantCulture),
                eventArgs.Exception.Message,
                stackTrace);

            PSEtwLog.LogAnalyticError(
                PSEventId.TransportError_Analytic, PSOpcode.Open, PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                RunspacePoolInstanceId.ToString(),
                powershellInstanceId.ToString(),
                eventArgs.Exception.ErrorCode.ToString(CultureInfo.InvariantCulture),
                eventArgs.Exception.Message,
                stackTrace);

            base.RaiseErrorHandler(eventArgs);
        }

        /// <summary>
        /// Used by ServicePendingCallbacks to give the control to derived classes for
        /// processing data that the base class does not understand.
        /// </summary>
        /// <param name="privateData">
        /// Derived class specific data to process. For command transport manager this
        /// should be a boolean.
        /// </param>
        internal override void ProcessPrivateData(object privateData)
        {
            Dbg.Assert(privateData != null, "privateData cannot be null.");

            // For this version...only a boolean can be used for privateData.
            bool shouldRaiseSignalCompleted = (bool)privateData;
            if (shouldRaiseSignalCompleted)
            {
                base.RaiseSignalCompleted();
            }
        }

        /// <summary>
        /// Receive/send operation handles and callback handles should be released/disposed from
        /// receive/send callback only. Releasing them after CloseOperation() may not cover all
        /// the scenarios, as WSMan does not guarantee that a rcv/send callback is not called after
        /// Close completed callback.
        /// </summary>
        /// <param name="flags"></param>
        /// <param name="shouldClearSend"></param>
        internal void ClearReceiveOrSendResources(int flags, bool shouldClearSend)
        {
            if (shouldClearSend)
            {
                if (_sendToRemoteCompleted != null)
                {
                    _sendToRemoteCompleted.Dispose();
                    _sendToRemoteCompleted = null;
                }

                // For send..clear always
                if (_wsManSendOperationHandle != IntPtr.Zero)
                {
                    WSManNativeApi.WSManCloseOperation(_wsManSendOperationHandle, 0);
                    _wsManSendOperationHandle = IntPtr.Zero;
                }
            }
            else
            {
                // clearing for receive..Clear only when the end of operation is reached.
                if (flags == (int)WSManNativeApi.WSManCallbackFlags.WSMAN_FLAG_CALLBACK_END_OF_OPERATION)
                {
                    if (_wsManReceiveOperationHandle != IntPtr.Zero)
                    {
                        WSManNativeApi.WSManCloseOperation(_wsManReceiveOperationHandle, 0);
                        _wsManReceiveOperationHandle = IntPtr.Zero;
                    }

                    if (_receivedFromRemote != null)
                    {
                        _receivedFromRemote.Dispose();
                        _receivedFromRemote = null;
                    }
                }
            }
        }

        /// <summary>
        /// Method to have transport prepare for a disconnect operation.
        /// </summary>
        internal override void PrepareForDisconnect()
        {
            _isDisconnectPending = true;

            // If there is not input processing and the command has already been created
            // on the server then this object is ready for Disconnect now.
            // Otherwise let the sending input data call back handle it.
            if (this.isClosed || _isDisconnectedOnInvoke ||
                (_isCreateCallbackReceived &&
                 this.serializedPipeline.Length == 0 &&
                 !_isSendingInput))
            {
                RaiseReadyForDisconnect();
            }
        }

        /// <summary>
        /// Method to resume post disconnect operations.
        /// </summary>
        internal override void PrepareForConnect()
        {
            _isDisconnectPending = false;
        }

        #endregion

        #region Callbacks from WSMan

        // callback that gets called when WSManRunShellCommandEx returns.
        private static void OnCreateCmdCompleted(IntPtr operationContext,
            int flags,
            IntPtr error,
            IntPtr shellOperationHandle,
            IntPtr commandOperationHandle,
            IntPtr operationHandle,
            IntPtr data)
        {
            tracer.WriteLine("OnCreateCmdCompleted callback received");

            long cmdContextId = 0;
            WSManClientCommandTransportManager cmdTM = null;
            if (!TryGetCmdTransportManager(operationContext, out cmdTM, out cmdContextId))
            {
                // We dont have the command TM handle..just return.
                tracer.WriteLine("OnCreateCmdCompleted: Unable to find a transport manager for the command context {0}.", cmdContextId);
                return;
            }

            PSEtwLog.LogAnalyticInformational(
                PSEventId.WSManCreateCommandCallbackReceived,
                PSOpcode.Connect,
                PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                cmdTM.RunspacePoolInstanceId.ToString(),
                cmdTM.powershellInstanceId.ToString());

            // dispose the cmdCompleted callback as it is not needed any more
            if (cmdTM._createCmdCompleted != null)
            {
                cmdTM._createCmdCompletedGCHandle.Free();
                cmdTM._createCmdCompleted.Dispose();
                cmdTM._createCmdCompleted = null;
            }

            // TODO: 188098 wsManCmdOperationHandle should be populated by WSManRunShellCommandEx,
            // but there is a thread timing bug in WSMan layer causing the callback to
            // be called before WSManRunShellCommandEx returns. since we already validated the
            // cmd context exists, safely assigning the commandOperationHandle to cmd transport manager.
            // Remove this once WSMan fixes its code.
            cmdTM._wsManCmdOperationHandle = commandOperationHandle;

            if (error != IntPtr.Zero)
            {
                WSManNativeApi.WSManError errorStruct = WSManNativeApi.WSManError.UnMarshal(error);
                if (errorStruct.errorCode != 0)
                {
                    tracer.WriteLine("OnCreateCmdCompleted callback: WSMan reported an error: {0}", errorStruct.errorDetail);

                    TransportErrorOccuredEventArgs eventargs = WSManTransportManagerUtils.ConstructTransportErrorEventArgs(
                        cmdTM._sessnTm.WSManAPIData.WSManAPIHandle,
                        null,
                        errorStruct,
                        TransportMethodEnum.RunShellCommandEx,
                        RemotingErrorIdStrings.RunShellCommandExCallBackError,
                         new object[] { WSManTransportManagerUtils.ParseEscapeWSManErrorMessage(errorStruct.errorDetail) });
                    cmdTM.ProcessWSManTransportError(eventargs);

                    return;
                }
            }

            // Send remaining cmd / parameter fragments.
            lock (cmdTM.syncObject)
            {
                cmdTM._isCreateCallbackReceived = true;

                // make sure the transport is not closed yet.
                if (cmdTM.isClosed)
                {
                    tracer.WriteLine("Client Session TM: Transport manager is closed. So returning");

                    if (cmdTM._isDisconnectPending)
                    {
                        cmdTM.RaiseReadyForDisconnect();
                    }

                    return;
                }

                // If a disconnect is pending at this point then we should not start
                // receiving data or sending input and let the disconnect take place.
                if (cmdTM._isDisconnectPending)
                {
                    cmdTM.RaiseReadyForDisconnect();
                    return;
                }

                if (cmdTM.serializedPipeline.Length == 0)
                {
                    cmdTM._shouldStartReceivingData = true;
                }

                // Start sending data if any..and see if we can initiate a receive.
                cmdTM.SendOneItem();

                // WSMan API does not allow a signal/input/receive be sent until RunShellCommand is
                // successful (ie., callback is received)
                if (cmdTM._isStopSignalPending)
                {
                    cmdTM.SendStopSignal();
                }
            }
        }

        private static void OnConnectCmdCompleted(IntPtr operationContext,
            int flags,
            IntPtr error,
            IntPtr shellOperationHandle,
            IntPtr commandOperationHandle,
            IntPtr operationHandle,
            IntPtr data)
        {
            tracer.WriteLine("OnConnectCmdCompleted callback received");

            PSEtwLog.LogAnalyticInformational(
                PSEventId.WSManCreateCommandCallbackReceived,
                PSOpcode.Connect,
                PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                "OnConnectCmdCompleted: OnConnectCmdCompleted callback received");

            long cmdContextId = 0;
            WSManClientCommandTransportManager cmdTM = null;
            if (!TryGetCmdTransportManager(operationContext, out cmdTM, out cmdContextId))
            {
                // We dont have the command TM handle..just return.
                tracer.WriteLine("OnConnectCmdCompleted: Unable to find a transport manager for the command context {0}.", cmdContextId);
                return;
            }

            // dispose the cmdCompleted callback as it is not needed any more
            if (cmdTM._connectCmdCompleted != null)
            {
                cmdTM._connectCmdCompleted.Dispose();
                cmdTM._connectCmdCompleted = null;
            }

            cmdTM._wsManCmdOperationHandle = commandOperationHandle;

            if (error != IntPtr.Zero)
            {
                WSManNativeApi.WSManError errorStruct = WSManNativeApi.WSManError.UnMarshal(error);
                if (errorStruct.errorCode != 0)
                {
                    tracer.WriteLine("OnConnectCmdCompleted callback: WSMan reported an error: {0}", errorStruct.errorDetail);

                    TransportErrorOccuredEventArgs eventargs = WSManTransportManagerUtils.ConstructTransportErrorEventArgs(
                        cmdTM._sessnTm.WSManAPIData.WSManAPIHandle,
                        null,
                        errorStruct,
                        TransportMethodEnum.ReconnectShellCommandEx,
                        RemotingErrorIdStrings.ReconnectShellCommandExCallBackError,
                         new object[] { WSManTransportManagerUtils.ParseEscapeWSManErrorMessage(errorStruct.errorDetail) });
                    cmdTM.ProcessWSManTransportError(eventargs);

                    return;
                }
            }

            lock (cmdTM.syncObject)
            {
                // If the transport is already closed then we are done.
                if (cmdTM.isClosed)
                {
                    tracer.WriteLine("Client Session TM: Transport manager is closed. So returning");

                    // Release disconnect pending, if any.
                    if (cmdTM._isDisconnectPending)
                    {
                        cmdTM.RaiseReadyForDisconnect();
                    }

                    return;
                }

                // If a disconnect is pending at this point then we should not start
                // receiving data or sending input and let the disconnect take place.
                if (cmdTM._isDisconnectPending)
                {
                    cmdTM.RaiseReadyForDisconnect();
                    return;
                }

                // Allow SendStopSignal.
                cmdTM._isCreateCallbackReceived = true;

                // Send stop signal if it is pending.
                if (cmdTM._isStopSignalPending)
                {
                    cmdTM.SendStopSignal();
                }
            }

            // Establish a client data to server callback so that the client can respond to prompts.
            cmdTM.SendOneItem();

            cmdTM.RaiseConnectCompleted();
            cmdTM.StartReceivingData();
        }

        private static void OnCloseCmdCompleted(IntPtr operationContext,
            int flags,
            IntPtr error,
            IntPtr shellOperationHandle,
            IntPtr commandOperationHandle,
            IntPtr operationHandle,
            IntPtr data)
        {
            tracer.WriteLine("OnCloseCmdCompleted callback received for operation context {0}", commandOperationHandle);

            PSEtwLog.LogAnalyticInformational(
                PSEventId.WSManCloseCommandCallbackReceived,
                PSOpcode.Connect,
                PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                "OnCloseCmdCompleted: OnCloseCmdCompleted callback received");

            long cmdContextId = 0;
            WSManClientCommandTransportManager cmdTM = null;
            if (!TryGetCmdTransportManager(operationContext, out cmdTM, out cmdContextId))
            {
                // We dont have the command TM handle..just return.
                tracer.WriteLine("OnCloseCmdCompleted: Unable to find a transport manager for the command context {0}.", cmdContextId);
                return;
            }

            tracer.WriteLine("Close completed callback received for command: {0}", cmdTM._cmdContextId);
            PSEtwLog.LogAnalyticInformational(
                PSEventId.WSManCloseCommandCallbackReceived,
                PSOpcode.Disconnect,
                PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                cmdTM.RunspacePoolInstanceId.ToString(),
                cmdTM.powershellInstanceId.ToString());

            if (cmdTM._isDisconnectPending)
            {
                cmdTM.RaiseReadyForDisconnect();
            }

            cmdTM.RaiseCloseCompleted();
        }

        private static void OnRemoteCmdSendCompleted(IntPtr operationContext,
            int flags,
            IntPtr error,
            IntPtr shellOperationHandle,
            IntPtr commandOperationHandle,
            IntPtr operationHandle,
            IntPtr data)
        {
            tracer.WriteLine("SendComplete callback received");

            PSEtwLog.LogAnalyticInformational(
                PSEventId.WSManSendShellInputExCallbackReceived,
                PSOpcode.Connect,
                PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                "OnRemoteCmdSendCompleted: SendComplete callback received");

            long cmdContextId = 0;
            WSManClientCommandTransportManager cmdTM = null;
            if (!TryGetCmdTransportManager(operationContext, out cmdTM, out cmdContextId))
            {
                // We dont have the command TM handle..just return.
                tracer.WriteLine("Unable to find a transport manager for the command context {0}.", cmdContextId);
                return;
            }

            cmdTM._isSendingInput = false;

            // do the logging for this send
            PSEtwLog.LogAnalyticInformational(
                PSEventId.WSManSendShellInputExCallbackReceived,
                PSOpcode.Connect,
                PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                cmdTM.RunspacePoolInstanceId.ToString(),
                cmdTM.powershellInstanceId.ToString());

            if ((!shellOperationHandle.Equals(cmdTM._wsManShellOperationHandle)) ||
                (!commandOperationHandle.Equals(cmdTM._wsManCmdOperationHandle)))
            {
                tracer.WriteLine("SendShellInputEx callback: ShellOperationHandles are not the same as the Send is initiated with");
                // WSMan returned data from a wrong shell..notify the caller
                // about the same.
                PSRemotingTransportException e = new PSRemotingTransportException(RemotingErrorIdStrings.CommandSendExFailed);
                TransportErrorOccuredEventArgs eventargs =
                    new TransportErrorOccuredEventArgs(e, TransportMethodEnum.CommandInputEx);
                cmdTM.ProcessWSManTransportError(eventargs);

                return;
            }

            // release the resources related to send
            cmdTM.ClearReceiveOrSendResources(flags, true);

            // if the transport manager is already closed..ignore the errors and return
            if (cmdTM.isClosed)
            {
                tracer.WriteLine("Client Command TM: Transport manager is closed. So returning");

                if (cmdTM._isDisconnectPending)
                {
                    cmdTM.RaiseReadyForDisconnect();
                }

                return;
            }

            if (error != IntPtr.Zero)
            {
                WSManNativeApi.WSManError errorStruct = WSManNativeApi.WSManError.UnMarshal(error);
                // Ignore Command aborted error. Command aborted is raised by WSMan to
                // notify command operation complete. PowerShell protocol has its own
                // way of notifying the same using state change events.
                if ((errorStruct.errorCode != 0) && (errorStruct.errorCode != 995))
                {
                    tracer.WriteLine("CmdSend callback: WSMan reported an error: {0}", errorStruct.errorDetail);

                    TransportErrorOccuredEventArgs eventargs = WSManTransportManagerUtils.ConstructTransportErrorEventArgs(
                        cmdTM._sessnTm.WSManAPIData.WSManAPIHandle,
                        null,
                        errorStruct,
                        TransportMethodEnum.CommandInputEx,
                        RemotingErrorIdStrings.CommandSendExCallBackError,
                        new object[] { WSManTransportManagerUtils.ParseEscapeWSManErrorMessage(errorStruct.errorDetail) });
                    cmdTM.ProcessWSManTransportError(eventargs);

                    return;
                }
            }

            // Send the next item, if available
            cmdTM.SendOneItem();
        }

        private static void OnRemoteCmdDataReceived(IntPtr operationContext,
            int flags,
            IntPtr error,
            IntPtr shellOperationHandle,
            IntPtr commandOperationHandle,
            IntPtr operationHandle,
            IntPtr data)
        {
            tracer.WriteLine("Remote Command DataReceived callback.");

            PSEtwLog.LogAnalyticInformational(
                PSEventId.WSManReceiveShellOutputExCallbackReceived,
                PSOpcode.Connect,
                PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                "OnRemoteCmdDataReceived: Remote Command DataReceived callback");

            long cmdContextId = 0;
            WSManClientCommandTransportManager cmdTM = null;
            if (!TryGetCmdTransportManager(operationContext, out cmdTM, out cmdContextId))
            {
                // We dont have the command TM handle..just return.
                tracer.WriteLine("Unable to find a transport manager for the given command context {0}.", cmdContextId);
                return;
            }

            if ((!shellOperationHandle.Equals(cmdTM._wsManShellOperationHandle)) ||
                (!commandOperationHandle.Equals(cmdTM._wsManCmdOperationHandle)))
            {
                // WSMan returned data from a wrong shell..notify the caller
                // about the same.
                tracer.WriteLine("CmdReceive callback: ShellOperationHandles are not the same as the Receive is initiated with");
                PSRemotingTransportException e = new PSRemotingTransportException(RemotingErrorIdStrings.CommandReceiveExFailed);
                TransportErrorOccuredEventArgs eventargs =
                    new TransportErrorOccuredEventArgs(e, TransportMethodEnum.ReceiveCommandOutputEx);
                cmdTM.ProcessWSManTransportError(eventargs);

                return;
            }

            // release the resources related to receive
            cmdTM.ClearReceiveOrSendResources(flags, false);

            // if the transport manager is already closed..ignore the errors and return
            if (cmdTM.isClosed)
            {
                tracer.WriteLine("Client Command TM: Transport manager is closed. So returning");
                return;
            }

            if (error != IntPtr.Zero)
            {
                WSManNativeApi.WSManError errorStruct = WSManNativeApi.WSManError.UnMarshal(error);
                if (errorStruct.errorCode != 0)
                {
                    tracer.WriteLine("CmdReceive callback: WSMan reported an error: {0}", errorStruct.errorDetail);

                    TransportErrorOccuredEventArgs eventargs = WSManTransportManagerUtils.ConstructTransportErrorEventArgs(
                        cmdTM._sessnTm.WSManAPIData.WSManAPIHandle,
                        null,
                        errorStruct,
                        TransportMethodEnum.ReceiveCommandOutputEx,
                        RemotingErrorIdStrings.CommandReceiveExCallBackError,
                        new object[] { errorStruct.errorDetail });
                    cmdTM.ProcessWSManTransportError(eventargs);

                    return;
                }
            }

            if (flags == (int)WSManNativeApi.WSManCallbackFlags.WSMAN_FLAG_RECEIVE_DELAY_STREAM_REQUEST_PROCESSED)
            {
                cmdTM._isDisconnectedOnInvoke = true;
                cmdTM.RaiseDelayStreamProcessedEvent();
                return;
            }

            WSManNativeApi.WSManReceiveDataResult dataReceived = WSManNativeApi.WSManReceiveDataResult.UnMarshal(data);
            if (dataReceived.data != null)
            {
                tracer.WriteLine("Cmd Received Data : {0}", dataReceived.data.Length);
                PSEtwLog.LogAnalyticInformational(
                    PSEventId.WSManReceiveShellOutputExCallbackReceived, PSOpcode.Receive, PSTask.None,
                    PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                    cmdTM.RunspacePoolInstanceId.ToString(),
                    cmdTM.powershellInstanceId.ToString(),
                    dataReceived.data.Length.ToString(CultureInfo.InvariantCulture));
                cmdTM.ProcessRawData(dataReceived.data, dataReceived.stream);
            }
        }

        private static void OnReconnectCmdCompleted(IntPtr operationContext,
            int flags,
            IntPtr error,
            IntPtr shellOperationHandle,
            IntPtr commandOperationHandle,
            IntPtr operationHandle,
            IntPtr data)
        {
            long cmdContextId = 0;
            WSManClientCommandTransportManager cmdTM = null;

            PSEtwLog.LogAnalyticInformational(
                PSEventId.WSManReceiveShellOutputExCallbackReceived,
                PSOpcode.Connect,
                PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                "OnReconnectCmdCompleted");

            if (!TryGetCmdTransportManager(operationContext, out cmdTM, out cmdContextId))
            {
                // We dont have the command TM handle..just return.
                tracer.WriteLine("Unable to find a transport manager for the given command context {0}.", cmdContextId);
                return;
            }

            if ((!shellOperationHandle.Equals(cmdTM._wsManShellOperationHandle)) ||
               (!commandOperationHandle.Equals(cmdTM._wsManCmdOperationHandle)))
            {
                // WSMan returned data from a wrong shell..notify the caller
                // about the same.
                tracer.WriteLine("Cmd Signal callback: ShellOperationHandles are not the same as the signal is initiated with");
                PSRemotingTransportException e = new PSRemotingTransportException(RemotingErrorIdStrings.ReconnectShellCommandExCallBackError);
                TransportErrorOccuredEventArgs eventargs =
                    new TransportErrorOccuredEventArgs(e, TransportMethodEnum.ReconnectShellCommandEx);
                cmdTM.ProcessWSManTransportError(eventargs);

                return;
            }

            if (error != IntPtr.Zero)
            {
                WSManNativeApi.WSManError errorStruct = WSManNativeApi.WSManError.UnMarshal(error);
                if (errorStruct.errorCode != 0)
                {
                    tracer.WriteLine("OnReconnectCmdCompleted callback: WSMan reported an error: {0}", errorStruct.errorDetail);

                    TransportErrorOccuredEventArgs eventargs = WSManTransportManagerUtils.ConstructTransportErrorEventArgs(
                        cmdTM._sessnTm.WSManAPIData.WSManAPIHandle,
                        null,
                        errorStruct,
                        TransportMethodEnum.ReconnectShellCommandEx,
                        RemotingErrorIdStrings.ReconnectShellCommandExCallBackError,
                         new object[] { WSManTransportManagerUtils.ParseEscapeWSManErrorMessage(errorStruct.errorDetail) });
                    cmdTM.ProcessWSManTransportError(eventargs);

                    return;
                }
            }

            // The command may have been disconnected before all input was read or
            // the returned command data started to be received.
            cmdTM._shouldStartReceivingData = true;
            cmdTM.SendOneItem();

            cmdTM.RaiseReconnectCompleted();
        }

        private static void OnRemoteCmdSignalCompleted(IntPtr operationContext,
            int flags,
            IntPtr error,
            IntPtr shellOperationHandle,
            IntPtr commandOperationHandle,
            IntPtr operationHandle,
            IntPtr data)
        {
            tracer.WriteLine("Signal Completed callback received.");

            PSEtwLog.LogAnalyticInformational(PSEventId.WSManSignalCallbackReceived, PSOpcode.Disconnect, PSTask.None, PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic, "OnRemoteCmdSignalCompleted");

            long cmdContextId = 0;
            WSManClientCommandTransportManager cmdTM = null;
            if (!TryGetCmdTransportManager(operationContext, out cmdTM, out cmdContextId))
            {
                // We dont have the command TM handle..just return.
                tracer.WriteLine("Unable to find a transport manager for the given command context {0}.", cmdContextId);
                return;
            }

            // log the callback received event.
            PSEtwLog.LogAnalyticInformational(
                PSEventId.WSManSignalCallbackReceived,
                PSOpcode.Disconnect,
                PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                cmdTM.RunspacePoolInstanceId.ToString(),
                cmdTM.powershellInstanceId.ToString());

            if ((!shellOperationHandle.Equals(cmdTM._wsManShellOperationHandle)) ||
                (!commandOperationHandle.Equals(cmdTM._wsManCmdOperationHandle)))
            {
                // WSMan returned data from a wrong shell..notify the caller
                // about the same.
                tracer.WriteLine("Cmd Signal callback: ShellOperationHandles are not the same as the signal is initiated with");
                PSRemotingTransportException e = new PSRemotingTransportException(RemotingErrorIdStrings.CommandSendExFailed);
                TransportErrorOccuredEventArgs eventargs =
                    new TransportErrorOccuredEventArgs(e, TransportMethodEnum.CommandInputEx);
                cmdTM.ProcessWSManTransportError(eventargs);

                return;
            }

            // release the resources related to signal
            if (cmdTM._cmdSignalOperationHandle != IntPtr.Zero)
            {
                WSManNativeApi.WSManCloseOperation(cmdTM._cmdSignalOperationHandle, 0);
                cmdTM._cmdSignalOperationHandle = IntPtr.Zero;
            }

            if (cmdTM._signalCmdCompleted != null)
            {
                cmdTM._signalCmdCompleted.Dispose();
                cmdTM._signalCmdCompleted = null;
            }

            // if the transport manager is already closed..ignore the errors and return
            if (cmdTM.isClosed)
            {
                tracer.WriteLine("Client Command TM: Transport manager is closed. So returning");
                return;
            }

            if (error != IntPtr.Zero)
            {
                WSManNativeApi.WSManError errorStruct = WSManNativeApi.WSManError.UnMarshal(error);
                if (errorStruct.errorCode != 0)
                {
                    tracer.WriteLine("Cmd Signal callback: WSMan reported an error: {0}", errorStruct.errorDetail);

                    TransportErrorOccuredEventArgs eventargs = WSManTransportManagerUtils.ConstructTransportErrorEventArgs(
                        cmdTM._sessnTm.WSManAPIData.WSManAPIHandle,
                        null,
                        errorStruct,
                        TransportMethodEnum.CommandInputEx,
                        RemotingErrorIdStrings.CommandSendExCallBackError,
                        new object[] { WSManTransportManagerUtils.ParseEscapeWSManErrorMessage(errorStruct.errorDetail) });
                    cmdTM.ProcessWSManTransportError(eventargs);
                    return;
                }
            }

            cmdTM.EnqueueAndStartProcessingThread(null, null, true);
        }

        private void SendOneItem()
        {
            // If a disconnect is completing then do not send any more data.
            // Also raise the readyfordisconnect event.
            if (_isDisconnectPending)
            {
                RaiseReadyForDisconnect();
                return;
            }

            byte[] data = null;
            DataPriorityType priorityType = DataPriorityType.Default;
            // serializedPipeline is static ie., data is added to this collection at construction time only
            // and data is accessed by only one thread at any given time..so we can depend on this count
            if (serializedPipeline.Length > 0)
            {
                data = serializedPipeline.ReadOrRegisterCallback(null);
                // if there are no command / parameter fragments need to be sent
                // start receiving data. Reason: Command will start its execution
                // once command string + parameters are sent.
                if (serializedPipeline.Length == 0)
                {
                    _shouldStartReceivingData = true;
                }
            }
            else if (_chunkToSend != null) // there is a pending chunk to be sent
            {
                data = _chunkToSend.Data;
                priorityType = _chunkToSend.Type;
                _chunkToSend = null;
            }
            else
            {
                // This will either return data or register callback but doesn't do both.
                data = dataToBeSent.ReadOrRegisterCallback(_onDataAvailableToSendCallback, out priorityType);
            }

            if (data != null)
            {
                _isSendingInput = true;
                SendData(data, priorityType);
            }

            if (_shouldStartReceivingData)
            {
                StartReceivingData();
            }
        }

        private void OnDataAvailableCallback(byte[] data, DataPriorityType priorityType)
        {
            Dbg.Assert(data != null, "data cannot be null in the data available callback");

            tracer.WriteLine("Received data from dataToBeSent store.");
            Dbg.Assert(_chunkToSend == null, "data callback received while a chunk is pending to be sent");
            _chunkToSend = new SendDataChunk(data, priorityType);
            SendOneItem();
        }

        #region SHIM: Redirection delegate for command data send.

        private static readonly Delegate s_commandSendRedirect = null;

        #endregion

        private void SendData(byte[] data, DataPriorityType priorityType)
        {
            tracer.WriteLine("Command sending data of size : {0}", data.Length);
            byte[] package = data;

            #region SHIM: Redirection code for command data send.

            bool sendContinue = true;

            if (s_commandSendRedirect != null)
            {
                object[] arguments = new object[2] { null, package };
                sendContinue = (bool)s_commandSendRedirect.DynamicInvoke(arguments);
                package = (byte[])arguments[0];
            }

            if (!sendContinue)
                return;

            #endregion

            using (WSManNativeApi.WSManData_ManToUn serializedContent =
                         new WSManNativeApi.WSManData_ManToUn(package))
            {
                PSEtwLog.LogAnalyticInformational(
                    PSEventId.WSManSendShellInputEx, PSOpcode.Send, PSTask.None,
                    PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                    RunspacePoolInstanceId.ToString(),
                    powershellInstanceId.ToString(),
                    serializedContent.BufferLength.ToString(CultureInfo.InvariantCulture));

                lock (syncObject)
                {
                    // make sure the transport is not closed.
                    if (isClosed)
                    {
                        tracer.WriteLine("Client Session TM: Transport manager is closed. So returning");
                        return;
                    }

                    // send callback
                    _sendToRemoteCompleted = new WSManNativeApi.WSManShellAsync(new IntPtr(_cmdContextId), s_cmdSendCallback);
                    WSManNativeApi.WSManSendShellInputEx(_wsManShellOperationHandle, _wsManCmdOperationHandle, 0,
                        priorityType == DataPriorityType.Default ?
                            WSManNativeApi.WSMAN_STREAM_ID_STDIN : WSManNativeApi.WSMAN_STREAM_ID_PROMPTRESPONSE,
                        serializedContent,
                        _sendToRemoteCompleted,
                        ref _wsManSendOperationHandle);
                }
            }
        }

        internal override void StartReceivingData()
        {
            PSEtwLog.LogAnalyticInformational(
                PSEventId.WSManReceiveShellOutputEx,
                PSOpcode.Receive, PSTask.None, PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                RunspacePoolInstanceId.ToString(), powershellInstanceId.ToString());

            // We should call Receive only once.. WSMan will call the callback multiple times.
            _shouldStartReceivingData = false;
            lock (syncObject)
            {
                // make sure the transport is not closed.
                if (isClosed)
                {
                    tracer.WriteLine("Client Session TM: Transport manager is closed. So returning");
                    return;
                }

                if (receiveDataInitiated)
                {
                    tracer.WriteLine("Client Session TM: ReceiveData has already been called.");
                    return;
                }

                receiveDataInitiated = true;
                // receive callback
                _receivedFromRemote = new WSManNativeApi.WSManShellAsync(new IntPtr(_cmdContextId), s_cmdReceiveCallback);
                WSManNativeApi.WSManReceiveShellOutputEx(_wsManShellOperationHandle,
                    _wsManCmdOperationHandle, startInDisconnectedMode ? (int)WSManNativeApi.WSManShellFlag.WSMAN_FLAG_RECEIVE_DELAY_OUTPUT_STREAM : 0,
                   _sessnTm.WSManAPIData.OutputStreamSet,
                   _receivedFromRemote, ref _wsManReceiveOperationHandle);
            }
        }

        #endregion

        #region Dispose / Destructor pattern

        protected override void Dispose(bool isDisposing)
        {
            tracer.WriteLine("Disposing command with command context: {0} Operation Context: {1}", _cmdContextId, _wsManCmdOperationHandle);
            base.Dispose(isDisposing);

            // remove command context from cmd handles dictionary
            RemoveCmdTransportManager(_cmdContextId);

            // unregister event handlers
            if (_sessnTm != null)
            {
                _sessnTm.RobustConnectionsInitiated -= HandleRobustConnectionsInitiated;
                _sessnTm.RobustConnectionsCompleted -= HandleRobusConnectionsCompleted;
            }

            if (_closeCmdCompleted != null)
            {
                _closeCmdCompleted.Dispose();
                _closeCmdCompleted = null;
            }

            if (_reconnectCmdCompleted != null)
            {
                _reconnectCmdCompleted.Dispose();
                _reconnectCmdCompleted = null;
            }

            _wsManCmdOperationHandle = IntPtr.Zero;
        }

        #endregion

        #region Static Data / Methods

        // This dictionary maintains active command transport managers to be used from various
        // callbacks.
        private static readonly Dictionary<long, WSManClientCommandTransportManager> s_cmdTMHandles =
            new Dictionary<long, WSManClientCommandTransportManager>();

        private static long s_cmdTMSeed;

        // Generate command transport manager unique id
        private static long GetNextCmdTMHandleId()
        {
            return System.Threading.Interlocked.Increment(ref s_cmdTMSeed);
        }

        // we need a synchronized add and remove so that multiple threads
        // update the data store concurrently
        private static void AddCmdTransportManager(long cmdTMId,
            WSManClientCommandTransportManager cmdTransportManager)
        {
            lock (s_cmdTMHandles)
            {
                s_cmdTMHandles.Add(cmdTMId, cmdTransportManager);
            }
        }

        private static void RemoveCmdTransportManager(long cmdTMId)
        {
            lock (s_cmdTMHandles)
            {
                s_cmdTMHandles.Remove(cmdTMId);
            }
        }

        // we need a synchronized add and remove so that multiple threads
        // update the data store concurrently
        private static bool TryGetCmdTransportManager(IntPtr operationContext,
            out WSManClientCommandTransportManager cmdTransportManager,
            out long cmdTMId)
        {
            cmdTMId = operationContext.ToInt64();
            cmdTransportManager = null;
            lock (s_cmdTMHandles)
            {
                return s_cmdTMHandles.TryGetValue(cmdTMId, out cmdTransportManager);
            }
        }

        #endregion
    }
}
