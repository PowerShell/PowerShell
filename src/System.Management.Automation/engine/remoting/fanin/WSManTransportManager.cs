/********************************************************************++
 * Copyright (c) Microsoft Corporation.  All rights reserved.
 * --********************************************************************/

/*
 * Common file that contains implementation for both server and client transport
 * managers based on WSMan protocol.
 * 
 */

using System;
using System.Management.Automation.Tracing;
using System.Text;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Internal;
#if !CORECLR
using System.Security.Principal;
#endif

// Don't expose the System.Management.Automation namespace here. This is transport layer
// and it shouldn't know anything about the engine.
using System.Management.Automation.Remoting;
using System.Management.Automation.Remoting.Client;
using System.Management.Automation.Remoting.Server;
// TODO: this seems ugly...Remoting datatypes should be in remoting namespace
using System.Management.Automation.Runspaces.Internal;

using PSRemotingCryptoHelper = System.Management.Automation.Internal.PSRemotingCryptoHelper;
using WSManConnectionInfo = System.Management.Automation.Runspaces.WSManConnectionInfo;
using RunspaceConnectionInfo = System.Management.Automation.Runspaces.RunspaceConnectionInfo;
using AuthenticationMechanism = System.Management.Automation.Runspaces.AuthenticationMechanism;
using PowerShell = System.Management.Automation.PowerShell;
using RunspacePool = System.Management.Automation.Runspaces.RunspacePool;
using Runspace = System.Management.Automation.Runspaces.Runspace;
using TypeTable = System.Management.Automation.Runspaces.TypeTable;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting.Client
{  
    /// <summary>
    /// WSMan TransportManager related utils
    /// </summary>
    internal static class WSManTransportManagerUtils
    {
        #region Static Data

        // Fully qualified error Id modifiers based on transport (WinRM) error codes.
        private static Dictionary<int, string> _transportErrorCodeToFQEID = new Dictionary<int, string>()
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
            {WSManNativeApi.ERROR_WSMAN_TARGET_UNKOWN, "TargetUnknown"},
            {WSManNativeApi.ERROR_WSMAN_CANNOTUSE_IP, "CannotUseIPAddress"}
        };

        #endregion

        #region Helper Methods

        /// <summary>
        /// Constructs a WSManTransportErrorOccuredEventArgs instance from the supplied data
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
            
            //For the first two special error conditions, it is remotely possible that the wsmanSessionTM is null when the failures are returned 
            //as part of command TM operations (could be returned becuase of RC retries under the hood)
            //Not worth to handle these cases seperately as there are very corner scenarios, but need to make sure wsmanSessionTM is not referenced
           

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
        /// <param name="transportErrorCode">transport error code</param>
        /// <param name="defaultFQEID">Default FQEID</param>
        /// <returns>Fully qualified error Id string</returns>
        internal static string GetFQEIDFromTransportError(
            int transportErrorCode,
            string defaultFQEID)
        {
            string specificErrorId;
            if (_transportErrorCodeToFQEID.TryGetValue(transportErrorCode, out specificErrorId))
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
        /// Default max uri redirection count - wsman
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

        private class CompletionEventArgs : EventArgs
        {
            private CompletionNotification _notification;

            internal CompletionEventArgs(CompletionNotification notification)
            {
                _notification = notification;
            }

            internal CompletionNotification Notification
            {
                get { return _notification; }
            }
        }

        #endregion

        #region Private Data
        // operation handles are owned by WSMan
        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private IntPtr wsManSessionHandle;
        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private IntPtr wsManShellOperationHandle;
        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private IntPtr wsManRecieveOperationHandle;
        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private IntPtr wsManSendOperationHandle;
        // this is used with WSMan callbacks to represent a session transport manager.
        private long sessionContextID;

        private WSManTransportManagerUtils.tmStartModes startMode = WSManTransportManagerUtils.tmStartModes.None;

        private string sessionName;

        private bool supportsDisconnect;

        // callbacks
        private PrioritySendDataCollection.OnDataAvailableCallback onDataAvailableToSendCallback;

        // instance callback handlers
        private WSManNativeApi.WSManShellAsync createSessionCallback;
        private WSManNativeApi.WSManShellAsync receivedFromRemote;
        private WSManNativeApi.WSManShellAsync sendToRemoteCompleted;
        private WSManNativeApi.WSManShellAsync disconnectSessionCompleted;
        private WSManNativeApi.WSManShellAsync reconnectSessionCompleted;
        private WSManNativeApi.WSManShellAsync connectSessionCallback;
        // TODO: This GCHandle is required as it seems WSMan is calling create callback
        // after we call Close. This seems wrong. Opened bug on WSMan to track this.
        private GCHandle createSessionCallbackGCHandle;
        private WSManNativeApi.WSManShellAsync closeSessionCompleted;
        
        // used by WSManCreateShell call to send additional data (like negotiation)
        // during shell creation. This is an instance variable to allow for redirection.
        private WSManNativeApi.WSManData_ManToUn openContent;
        // By default WSMan compresses data sent on the network..use this flag to not do
        // this.
        private bool noCompression;
        private bool noMachineProfile;

        private WSManConnectionInfo _connectionInfo;
        private int _connectionRetryCount;

        
        private const string resBaseName = "remotingerroridstrings";

        // Robust connections maximum retry time value in milliseconds.
        private int maxRetryTime;

        private WSManAPIDataCommon wsManApiData;

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
                                        _connectionInfo.IdleTimeout = timeout;
                                    }
                                    else
                                    {
                                        _connectionInfo.MaxIdleTimeout = timeout;
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
                                    _connectionInfo.OutputBufferingMode = Runspaces.OutputBufferingMode.Block;
                                }
                                else if (bufferMode.Equals("Drop", StringComparison.OrdinalIgnoreCase))
                                {
                                    _connectionInfo.OutputBufferingMode = Runspaces.OutputBufferingMode.Drop;
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
        private static WSManNativeApi.WSManShellAsyncCallback sessionCreateCallback;
        private static WSManNativeApi.WSManShellAsyncCallback sessionCloseCallback;
        private static WSManNativeApi.WSManShellAsyncCallback sessionReceiveCallback;
        private static WSManNativeApi.WSManShellAsyncCallback sessionSendCallback;
        private static WSManNativeApi.WSManShellAsyncCallback sessionDisconnectCallback;
        private static WSManNativeApi.WSManShellAsyncCallback sessionReconnectCallback;
        private static WSManNativeApi.WSManShellAsyncCallback sessionConnectCallback;

        // This dictionary maintains active session transport managers to be used from various
        // callbacks.
        private static Dictionary<long, WSManClientSessionTransportManager> SessionTMHandles =
            new Dictionary<long,WSManClientSessionTransportManager>();
        private static long SessionTMSeed;
        //generate unique session id
        private static long GetNextSessionTMHandleId()
        {
            return System.Threading.Interlocked.Increment(ref SessionTMSeed);
        }

        // we need a synchronized add and remove so that multiple threads
        // update the data store concurrently
        private static void AddSessionTransportManager(long sessnTMId,
            WSManClientSessionTransportManager sessnTransportManager)
        {
            lock (SessionTMHandles)
            {
                SessionTMHandles.Add(sessnTMId, sessnTransportManager);
            }
        }

        private static void RemoveSessionTransportManager(long sessnTMId)
        {
            lock (SessionTMHandles)
            {
                SessionTMHandles.Remove(sessnTMId);
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
            lock (SessionTMHandles)
            {
                return SessionTMHandles.TryGetValue(sessnTMId, out sessnTransportManager);
            }
        }

        #endregion

        #region SHIM: Redirection delegates for test purposes

        private static Delegate sessionSendRedirect = null;
        private static Delegate protocolVersionRedirect = null;

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
            sessionCreateCallback = new WSManNativeApi.WSManShellAsyncCallback(createDelegate);

            WSManNativeApi.WSManShellCompletionFunction closeDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnCloseSessionCompleted);
            sessionCloseCallback = new WSManNativeApi.WSManShellAsyncCallback(closeDelegate);

            WSManNativeApi.WSManShellCompletionFunction receiveDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnRemoteSessionDataReceived);
            sessionReceiveCallback = new WSManNativeApi.WSManShellAsyncCallback(receiveDelegate);

            WSManNativeApi.WSManShellCompletionFunction sendDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnRemoteSessionSendCompleted);
            sessionSendCallback = new WSManNativeApi.WSManShellAsyncCallback(sendDelegate);

            WSManNativeApi.WSManShellCompletionFunction disconnectDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnRemoteSessionDisconnectCompleted);
            sessionDisconnectCallback = new WSManNativeApi.WSManShellAsyncCallback(disconnectDelegate);

            WSManNativeApi.WSManShellCompletionFunction reconnectDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnRemoteSessionReconnectCompleted);
            sessionReconnectCallback = new WSManNativeApi.WSManShellAsyncCallback(reconnectDelegate);

            WSManNativeApi.WSManShellCompletionFunction connectDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnRemoteSessionConnectCallback);
            sessionConnectCallback = new WSManNativeApi.WSManShellAsyncCallback(connectDelegate);
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
        /// <param name="cryptoHelper">crypto helper</param>
        /// <param name="sessionName">session friendly name</param>
        /// <exception cref="PSInvalidOperationException">
        /// 1. Create Session failed with a non-zero error code.
        /// </exception>
        internal WSManClientSessionTransportManager(Guid runspacePoolInstanceId,
            WSManConnectionInfo connectionInfo,
            PSRemotingCryptoHelper cryptoHelper, string sessionName) : base(runspacePoolInstanceId, cryptoHelper)
        {
            // Initialize WSMan instance
            wsManApiData = new WSManAPIDataCommon();
            if (wsManApiData.WSManAPIHandle == IntPtr.Zero)
            {
                throw new PSRemotingTransportException(
                    StringUtil.Format(RemotingErrorIdStrings.WSManInitFailed, wsManApiData.ErrorCode));
            }
            Dbg.Assert(null != connectionInfo, "connectionInfo cannot be null");

            CryptoHelper = cryptoHelper;
            dataToBeSent.Fragmentor = base.Fragmentor;
            this.sessionName = sessionName;
            
            // session transport manager can recieve unlimited data..however each object is limited
            // by maxRecvdObjectSize. this is to allow clients to use a session for an unlimited time..
            // also the messages that can be sent to a session are limited and very controlled.
            // However a command transport manager can be restricted to receive only a fixed amount of data
            // controlled by maxRecvdDataSizeCommand..This is because commands can accept any number of input
            // objects.
            ReceivedDataCollection.MaximumReceivedDataSize = null;
            ReceivedDataCollection.MaximumReceivedObjectSize = connectionInfo.MaximumReceivedObjectSize;

            onDataAvailableToSendCallback =
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
            Dbg.Assert(wsManSessionHandle != IntPtr.Zero, "Session handle cannot be null");
            using (tracer.TraceMethod("Setting Default timeout: {0} milliseconds", milliseconds))
            {
                int result = WSManNativeApi.WSManSetSessionOption(wsManSessionHandle,
                    WSManNativeApi.WSManSessionOption.WSMAN_OPTION_DEFAULT_OPERATION_TIMEOUTMS,
                    new WSManNativeApi.WSManDataDWord(milliseconds));

                if (result != 0)
                {
                    // Get the error message from WSMan
                    string errorMessage = WSManNativeApi.WSManGetErrorMessage(wsManApiData.WSManAPIHandle, result);

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
            Dbg.Assert(wsManSessionHandle != IntPtr.Zero, "Session handle cannot be null");
            using (tracer.TraceMethod("Setting CreateShell timeout: {0} milliseconds", milliseconds))
            {
                int result = WSManNativeApi.WSManSetSessionOption(wsManSessionHandle,
                    WSManNativeApi.WSManSessionOption.WSMAN_OPTION_TIMEOUTMS_CREATE_SHELL,
                    new WSManNativeApi.WSManDataDWord(milliseconds));

                if (result != 0)
                {
                    // Get the error message from WSMan
                    string errorMessage = WSManNativeApi.WSManGetErrorMessage(wsManApiData.WSManAPIHandle, result);

                    PSInvalidOperationException exception = new PSInvalidOperationException(errorMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Sets timeout for Close operation in milliseconds
        /// </summary>
        /// <param name="milliseconds"></param>
        /// <returns></returns>
        /// <exception cref="PSInvalidOperationException">
        /// Setting session option failed with a non-zero error code.
        /// </exception>
        internal void SetCloseTimeOut(int milliseconds)
        {
            Dbg.Assert(wsManSessionHandle != IntPtr.Zero, "Session handle cannot be null");
            using (tracer.TraceMethod("Setting CloseShell timeout: {0} milliseconds", milliseconds))
            {
                int result = WSManNativeApi.WSManSetSessionOption(wsManSessionHandle,
                    WSManNativeApi.WSManSessionOption.WSMAN_OPTION_TIMEOUTMS_CLOSE_SHELL_OPERATION,
                    new WSManNativeApi.WSManDataDWord(milliseconds));
                
                if (result != 0)
                {
                    // Get the error message from WSMan
                    string errorMessage = WSManNativeApi.WSManGetErrorMessage(wsManApiData.WSManAPIHandle, result);

                    PSInvalidOperationException exception = new PSInvalidOperationException(errorMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Sets timeout for SendShellInput operation in milliseconds
        /// </summary>
        /// <param name="milliseconds"></param>
        /// <returns></returns>
        /// <exception cref="PSInvalidOperationException">
        /// Setting session option failed with a non-zero error code.
        /// </exception>
        internal void SetSendTimeOut(int milliseconds)
        {
            Dbg.Assert(wsManSessionHandle != IntPtr.Zero, "Session handle cannot be null");
            using (tracer.TraceMethod("Setting SendShellInput timeout: {0} milliseconds", milliseconds))
            {
                int result = WSManNativeApi.WSManSetSessionOption(wsManSessionHandle,
                    WSManNativeApi.WSManSessionOption.WSMAN_OPTION_TIMEOUTMS_SEND_SHELL_INPUT,
                    new WSManNativeApi.WSManDataDWord(milliseconds));

                if (result != 0)
                {
                    // Get the error message from WSMan
                    string errorMessage = WSManNativeApi.WSManGetErrorMessage(wsManApiData.WSManAPIHandle, result);

                    PSInvalidOperationException exception = new PSInvalidOperationException(errorMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Sets timeout for Receive operation in milliseconds
        /// </summary>
        /// <param name="milliseconds"></param>
        /// <returns></returns>
        /// <exception cref="PSInvalidOperationException">
        /// Setting session option failed with a non-zero error code.
        /// </exception>
        internal void SetReceiveTimeOut(int milliseconds)
        {
            Dbg.Assert(wsManSessionHandle != IntPtr.Zero, "Session handle cannot be null");
            using (tracer.TraceMethod("Setting ReceiveShellOutput timeout: {0} milliseconds", milliseconds))
            {
                int result = WSManNativeApi.WSManSetSessionOption(wsManSessionHandle,
                    WSManNativeApi.WSManSessionOption.WSMAN_OPTION_TIMEOUTMS_RECEIVE_SHELL_OUTPUT,
                    new WSManNativeApi.WSManDataDWord(milliseconds));

                if (result != 0)
                {
                    // Get the error message from WSMan
                    string errorMessage = WSManNativeApi.WSManGetErrorMessage(wsManApiData.WSManAPIHandle, result);

                    PSInvalidOperationException exception = new PSInvalidOperationException(errorMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Sets timeout for Signal operation in milliseconds
        /// </summary>
        /// <param name="milliseconds"></param>
        /// <returns></returns>
        /// <exception cref="PSInvalidOperationException">
        /// Setting session option failed with a non-zero error code.
        /// </exception>
        internal void SetSignalTimeOut(int milliseconds)
        {
            Dbg.Assert(wsManSessionHandle != IntPtr.Zero, "Session handle cannot be null");
            using (tracer.TraceMethod("Setting SignalShell timeout: {0} milliseconds", milliseconds))
            {
                int result = WSManNativeApi.WSManSetSessionOption(wsManSessionHandle,
                    WSManNativeApi.WSManSessionOption.WSMAN_OPTION_TIMEOUTMS_SIGNAL_SHELL,
                    new WSManNativeApi.WSManDataDWord(milliseconds));

                if (result != 0)
                {
                    // Get the error message from WSMan
                    string errorMessage = WSManNativeApi.WSManGetErrorMessage(wsManApiData.WSManAPIHandle, result);

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
            int result = WSManNativeApi.WSManSetSessionOption(wsManSessionHandle,
                option, new WSManNativeApi.WSManDataDWord(dwordData));

            if (result != 0)
            {
                // Get the error message from WSMan
                string errorMessage = WSManNativeApi.WSManGetErrorMessage(wsManApiData.WSManAPIHandle, result);

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
                int result = WSManNativeApi.WSManSetSessionOption(wsManSessionHandle,
                      option, data);

                if (result != 0)
                {
                    // Get the error message from WSMan
                    string errorMessage = WSManNativeApi.WSManGetErrorMessage(wsManApiData.WSManAPIHandle, result);

                    PSInvalidOperationException exception = new PSInvalidOperationException(errorMessage);
                    throw exception;
                }
            }
        }

        #endregion

        #region Internal Methods / Properties

        internal WSManAPIDataCommon WSManAPIData
        {
            get { return wsManApiData; }
        }

        internal bool SupportsDisconnect
        {
            get { return supportsDisconnect; }
        }


        internal override void DisconnectAsync()
        {
            Dbg.Assert(!isClosed, "object already disposed");

            // Pass the WSManConnectionInfo object IdleTimeout value if it is
            // valid.  Otherwise pass the default value that instructs the server
            // to use its default IdleTimeout value.
            uint uIdleTimeout = (_connectionInfo.IdleTimeout > 0) ?
                (uint)_connectionInfo.IdleTimeout : UseServerDefaultIdleTimeoutUInt;

            // startup info 
            WSManNativeApi.WSManShellDisconnectInfo disconnectInfo = new WSManNativeApi.WSManShellDisconnectInfo(uIdleTimeout);

            //Add ETW traces

            // disconnect Callback
            disconnectSessionCompleted = new WSManNativeApi.WSManShellAsync(new IntPtr(sessionContextID), sessionDisconnectCallback);
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
                    flags |= (_connectionInfo.OutputBufferingMode == Runspaces.OutputBufferingMode.Block) ?
                                    (int)WSManNativeApi.WSManShellFlag.WSMAN_FLAG_SERVER_BUFFERING_MODE_BLOCK : 0;
                    flags |= (_connectionInfo.OutputBufferingMode == Runspaces.OutputBufferingMode.Drop) ?
                                    (int)WSManNativeApi.WSManShellFlag.WSMAN_FLAG_SERVER_BUFFERING_MODE_DROP : 0;

                    WSManNativeApi.WSManDisconnectShellEx(wsManShellOperationHandle,
                            flags,
                            disconnectInfo,
                            disconnectSessionCompleted);
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

            //Add ETW traces

            // reconnect Callback
            reconnectSessionCompleted = new WSManNativeApi.WSManShellAsync(new IntPtr(sessionContextID), sessionReconnectCallback);
            lock (syncObject)
            {
                if (isClosed)
                {
                    // the transport is already closed
                    // anymore.
                    return;
                }

                int flags = 0;
                flags |= (_connectionInfo.OutputBufferingMode == Runspaces.OutputBufferingMode.Block) ?
                                (int)WSManNativeApi.WSManShellFlag.WSMAN_FLAG_SERVER_BUFFERING_MODE_BLOCK : 0;
                flags |= (_connectionInfo.OutputBufferingMode == Runspaces.OutputBufferingMode.Drop) ?
                                (int)WSManNativeApi.WSManShellFlag.WSMAN_FLAG_SERVER_BUFFERING_MODE_DROP : 0;

                WSManNativeApi.WSManReconnectShellEx(wsManShellOperationHandle,
                        flags,
                        reconnectSessionCompleted);
            }

        }

        /// <summary>
        /// Starts connecting to an existing remote session. This will result in a WSManConnectShellEx WSMan
        /// async call. Piggy backs available data in input stream as openXml in connect SOAP.
        /// DSHandler will push negotiation related messages through the open content
        /// </summary>
        /// <exception cref="PSRemotingTransportException">
        /// WSManConnectShellEx failed.
        /// </exception>
        internal override void ConnectAsync()
        {
            Dbg.Assert(!isClosed, "object already disposed");
            Dbg.Assert(!String.IsNullOrEmpty(_connectionInfo.ShellUri), "shell uri cannot be null or empty.");

            ReceivedDataCollection.PrepareForStreamConnect();
            // additional content with connect shell call. Negotiation and connect related messages
            // should be included in payload
            if (null == openContent)
            {
                DataPriorityType additionalDataType;
                byte[] additionalData = dataToBeSent.ReadOrRegisterCallback(null, out additionalDataType);

                if (null != additionalData)
                {
                    // WSMan expects the data to be in XML format (which is text + xml tags)
                    // so convert byte[] into base64 encoded format
                    string base64EncodedDataInXml = string.Format(CultureInfo.InvariantCulture, "<{0} xmlns=\"{1}\">{2}</{0}>",
                        WSManNativeApi.PS_CONNECT_XML_TAG,
                        WSManNativeApi.PS_XML_NAMESPACE,
                        Convert.ToBase64String(additionalData));
                    openContent = new WSManNativeApi.WSManData_ManToUn(base64EncodedDataInXml);
                }

                //THERE SHOULD BE NO ADDITIONAL DATA. If there is, it means we are not able to push all initial negotaion related data
                // as part of Connect SOAP. The connect algorithm is based on this assumption. So bail out.
                additionalData = dataToBeSent.ReadOrRegisterCallback(null, out additionalDataType);
                if (additionalData != null)
                {
                    //Negotiation payload does not fit in ConnectShell. bail out. 
                    //Assert for now. should be replaced with raising an exception so upper layers can catch.
                    Dbg.Assert(false, "Negotiation payload does not fit in ConnectShell");
                    return;
                }

            }

            // Create and store context for this shell operation. This context is used from various callbacks
            sessionContextID = GetNextSessionTMHandleId();
            AddSessionTransportManager(sessionContextID, this);

            //session is implicitly assumed to support disconnect
            supportsDisconnect = true;

            // Create Callback
            connectSessionCallback = new WSManNativeApi.WSManShellAsync(new IntPtr(sessionContextID), sessionConnectCallback);
            lock (syncObject)
            {
                if (isClosed)
                {
                    // the transport is already closed..so no need to connect
                    // anymore.
                    return;
                }

                Dbg.Assert(startMode == WSManTransportManagerUtils.tmStartModes.None, "startMode is not in expected state");
                startMode = WSManTransportManagerUtils.tmStartModes.Connect;

                int flags = 0;
                flags |= (_connectionInfo.OutputBufferingMode == Runspaces.OutputBufferingMode.Block) ?
                                (int)WSManNativeApi.WSManShellFlag.WSMAN_FLAG_SERVER_BUFFERING_MODE_BLOCK : 0;
                flags |= (_connectionInfo.OutputBufferingMode == Runspaces.OutputBufferingMode.Drop) ?
                                (int)WSManNativeApi.WSManShellFlag.WSMAN_FLAG_SERVER_BUFFERING_MODE_DROP : 0;

                WSManNativeApi.WSManConnectShellEx(wsManSessionHandle,
                    flags,
                    _connectionInfo.ShellUri,
                    RunspacePoolInstanceId.ToString().ToUpperInvariant(),  //wsman is case sensetive wrt shellId. so consistently using upper case
                    IntPtr.Zero,
                    openContent,
                    connectSessionCallback,
                    ref wsManShellOperationHandle);
            }

            if (wsManShellOperationHandle == IntPtr.Zero)
            {
                TransportErrorOccuredEventArgs eventargs = WSManTransportManagerUtils.ConstructTransportErrorEventArgs(wsManApiData.WSManAPIHandle,
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
                PSEtwLog.LogAnalyticInformational(PSEventId.WSManReceiveShellOutputEx,
                    PSOpcode.Receive, PSTask.None, PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                    RunspacePoolInstanceId.ToString(), Guid.Empty.ToString());

                receivedFromRemote = new WSManNativeApi.WSManShellAsync(new IntPtr(sessionContextID), sessionReceiveCallback);
                WSManNativeApi.WSManReceiveShellOutputEx(wsManShellOperationHandle,
                    IntPtr.Zero, 0, wsManApiData.OutputStreamSet, receivedFromRemote,
                    ref wsManRecieveOperationHandle);
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
        internal override void CreateAsync()
        {
            Dbg.Assert(!isClosed, "object already disposed");
            Dbg.Assert(!String.IsNullOrEmpty(_connectionInfo.ShellUri), "shell uri cannot be null or empty.");
            Dbg.Assert(wsManApiData != null, "WSManApiData should always be created before session creation.");

            List<WSManNativeApi.WSManOption> shellOptions = new List<WSManNativeApi.WSManOption>(wsManApiData.CommonOptionSet);

            #region SHIM: Redirection code for protocol version
            
            if (protocolVersionRedirect != null)
            {
                string newProtocolVersion = (string)protocolVersionRedirect.DynamicInvoke();
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
            uint uIdleTimeout = (_connectionInfo.IdleTimeout > 0) ?
                (uint)_connectionInfo.IdleTimeout : UseServerDefaultIdleTimeoutUInt;

            // startup info           
            WSManNativeApi.WSManShellStartupInfo_ManToUn startupInfo =
                new WSManNativeApi.WSManShellStartupInfo_ManToUn(wsManApiData.InputStreamSet,
                wsManApiData.OutputStreamSet,
                uIdleTimeout,
                sessionName);

            // additional content with create shell call. Piggy back first fragement from
            // the dataToBeSent buffer.
            if (null == openContent)
            {   
                DataPriorityType additionalDataType;
                byte[] additionalData = dataToBeSent.ReadOrRegisterCallback(null, out additionalDataType);

                #region SHIM: Redirection code for session data send.

                bool sendContinue = true;

                if (sessionSendRedirect != null)
                {
                    object[] arguments = new object[2] { null, additionalData };
                    sendContinue = (bool)sessionSendRedirect.DynamicInvoke(arguments);
                    additionalData = (byte[])arguments[0];
                }

                if (!sendContinue)
                    return;

                #endregion

                if (null != additionalData)
                {
                    // WSMan expects the data to be in XML format (which is text + xml tags)
                    // so convert byte[] into base64 encoded format
                    string base64EncodedDataInXml = string.Format(CultureInfo.InvariantCulture, "<{0} xmlns=\"{1}\">{2}</{0}>",
                        WSManNativeApi.PS_CREATION_XML_TAG,
                        WSManNativeApi.PS_XML_NAMESPACE,
                        Convert.ToBase64String(additionalData));
                    openContent = new WSManNativeApi.WSManData_ManToUn(base64EncodedDataInXml);
                }
            }

            // Create the session context information only once.  CreateAsync() can be called multiple
            // times by RetrySessionCreation for flaky networks.
            if (sessionContextID == 0)
            {
                // Create and store context for this shell operation. This context is used from various callbacks
                sessionContextID = GetNextSessionTMHandleId();
                AddSessionTransportManager(sessionContextID, this);

                // Create Callback
                createSessionCallback = new WSManNativeApi.WSManShellAsync(new IntPtr(sessionContextID), sessionCreateCallback);
                createSessionCallbackGCHandle = GCHandle.Alloc(createSessionCallback);
            }

            PSEtwLog.LogAnalyticInformational(PSEventId.WSManCreateShell, PSOpcode.Connect,
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

                    Dbg.Assert(startMode == WSManTransportManagerUtils.tmStartModes.None, "startMode is not in expected state");
                    startMode = WSManTransportManagerUtils.tmStartModes.Create;

                    if (noMachineProfile)
                    {
                        WSManNativeApi.WSManOption noProfile = new WSManNativeApi.WSManOption();
                        noProfile.name = WSManNativeApi.NoProfile;
                        noProfile.mustComply = true;
                        noProfile.value = "1";
                        shellOptions.Add(noProfile);
                    }

                    int flags = noCompression ? (int)WSManNativeApi.WSManShellFlag.WSMAN_FLAG_NO_COMPRESSION : 0;
                    flags |= (_connectionInfo.OutputBufferingMode == Runspaces.OutputBufferingMode.Block) ?
                                    (int)WSManNativeApi.WSManShellFlag.WSMAN_FLAG_SERVER_BUFFERING_MODE_BLOCK : 0;
                    flags |= (_connectionInfo.OutputBufferingMode == Runspaces.OutputBufferingMode.Drop) ? 
                                    (int)WSManNativeApi.WSManShellFlag.WSMAN_FLAG_SERVER_BUFFERING_MODE_DROP : 0;

                    using (WSManNativeApi.WSManOptionSet optionSet = new WSManNativeApi.WSManOptionSet(shellOptions.ToArray()))
                    {
                        WSManNativeApi.WSManCreateShellEx(wsManSessionHandle,
                            flags,
                            _connectionInfo.ShellUri,
                            RunspacePoolInstanceId.ToString().ToUpperInvariant(),
                            startupInfo,
                            optionSet,
                            openContent,
                            createSessionCallback,
                            ref wsManShellOperationHandle);
                    }
                }

                if (wsManShellOperationHandle == IntPtr.Zero)
                {
                    TransportErrorOccuredEventArgs eventargs = WSManTransportManagerUtils.ConstructTransportErrorEventArgs(wsManApiData.WSManAPIHandle,
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
        internal override void CloseAsync()
        {
            bool shouldRaiseCloseCompleted = false;
            // let other threads release the lock before we clean up the resources.
            lock (syncObject)
            {
                if (isClosed == true)
                {
                    return;
                }
                
                if (startMode == WSManTransportManagerUtils.tmStartModes.None)
                {
                    shouldRaiseCloseCompleted = true;
                }
                else if (startMode == WSManTransportManagerUtils.tmStartModes.Create ||
                    startMode == WSManTransportManagerUtils.tmStartModes.Connect)
                {
                    if (IntPtr.Zero == wsManShellOperationHandle)
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
                    RemoveSessionTransportManager(sessionContextID);
                }
                return;
            }

            //TODO - On unexpected failures on a reconstructed session... we dont want to close server session
            PSEtwLog.LogAnalyticInformational(PSEventId.WSManCloseShell,
                PSOpcode.Disconnect, PSTask.None, PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic, 
                RunspacePoolInstanceId.ToString());
            closeSessionCompleted = new WSManNativeApi.WSManShellAsync(new IntPtr(sessionContextID), sessionCloseCallback);
            WSManNativeApi.WSManCloseShell(wsManShellOperationHandle, 0, closeSessionCompleted);
        }

        /// <summary>
        /// Adjusts for any variations in different protocol versions. Following changes are considered
        /// - In V2, default max envelope size is 150KB while in V3 it has been changed to 500KB. 
        ///   With default configuration remoting from V3 client to V2 server will break as V3 client can send upto 500KB in a single Send packet
        ///   So if server version is known to be V2, we'll downgrade the max env size to 150KB (V2's default) if the current value is 500KB (V3 default)
        /// </summary>
        /// <param name="serverProtocolVersion">server negotiated protocol version</param>
        internal void AdjustForProtocolVariations(Version serverProtocolVersion)
        {
            if (serverProtocolVersion <= RemotingConstants.ProtocolVersionWin7RTM)
            {
                int maxEnvSize;
                WSManNativeApi.WSManGetSessionOptionAsDword(wsManSessionHandle,
                    WSManNativeApi.WSManSessionOption.WSMAN_OPTION_MAX_ENVELOPE_SIZE_KB,
                    out maxEnvSize);

                if (maxEnvSize == WSManNativeApi.WSMAN_DEFAULT_MAX_ENVELOPE_SIZE_KB_V3)
                {
                    int result = WSManNativeApi.WSManSetSessionOption(wsManSessionHandle,
                    WSManNativeApi.WSManSessionOption.WSMAN_OPTION_MAX_ENVELOPE_SIZE_KB,
                    new WSManNativeApi.WSManDataDWord(WSManNativeApi.WSMAN_DEFAULT_MAX_ENVELOPE_SIZE_KB_V2));

                    if (result != 0)
                    {
                        // Get the error message from WSMan
                        string errorMessage = WSManNativeApi.WSManGetErrorMessage(wsManApiData.WSManAPIHandle, result);

                        PSInvalidOperationException exception = new PSInvalidOperationException(errorMessage);
                        throw exception;
                    }

                    //retrieve the packet size again
                    int packetSize;
                    WSManNativeApi.WSManGetSessionOptionAsDword(wsManSessionHandle,
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

            closeSessionCompleted = new WSManNativeApi.WSManShellAsync(new IntPtr(sessionContextID), sessionCloseCallback);
            WSManNativeApi.WSManCloseShell(wsManShellOperationHandle, 0, closeSessionCompleted);
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
            PSEtwLog.LogAnalyticInformational(PSEventId.URIRedirection,
                PSOpcode.Connect, PSTask.None, PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                RunspacePoolInstanceId.ToString(), newUri.ToString());
            Initialize(newUri, (WSManConnectionInfo)connectionInfo);
            //reset startmode
            startMode = WSManTransportManagerUtils.tmStartModes.None;
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
            Dbg.Assert(null != cmd, "Cmd cannot be null");

            WSManConnectionInfo wsmanConnectionInfo = connectionInfo as WSManConnectionInfo;
            Dbg.Assert(null != wsmanConnectionInfo, "ConnectionInfo must be WSManConnectionInfo");

            WSManClientCommandTransportManager result = new
                WSManClientCommandTransportManager(wsmanConnectionInfo, wsManShellOperationHandle, cmd, noInput, this);            
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
            Dbg.Assert(null != connectionInfo, "connectionInfo cannot be null.");

            this._connectionInfo = connectionInfo;
            
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
                if ((null != connectionInfo.Credential) && (!string.IsNullOrEmpty(connectionInfo.Credential.UserName)))
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

                switch(connectionInfo.ProxyAuthentication)
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

            WSManNativeApi.WSManProxyInfo proxyInfo = (ProxyAccessType.None == connectionInfo.ProxyAccessType) ?
                null :
                new WSManNativeApi.WSManProxyInfo(connectionInfo.ProxyAccessType, proxyAuthCredentials);

            int result = 0;
            
            try
            {
                result = WSManNativeApi.WSManCreateSession(wsManApiData.WSManAPIHandle, connectionStr, 0,
                     authCredentials.GetMarshalledObject(),
                     (null == proxyInfo) ? IntPtr.Zero : (IntPtr) proxyInfo, 
                     ref wsManSessionHandle);
            }
            finally
            {
                // release resources
                if (null != proxyAuthCredentials)
                {
                    proxyAuthCredentials.Dispose();
                }

                if (null != proxyInfo)
                {
                    proxyInfo.Dispose();
                }

                if (null != authCredentials)
                {
                    authCredentials.Dispose();
                }
            }

            if (result != 0)
            {
                // Get the error message from WSMan
                string errorMessage = WSManNativeApi.WSManGetErrorMessage(wsManApiData.WSManAPIHandle, result);

                PSInvalidOperationException exception = new PSInvalidOperationException(errorMessage);
                throw exception;
            }

            // set the packet size for this session
            int packetSize;
            WSManNativeApi.WSManGetSessionOptionAsDword(wsManSessionHandle,
                WSManNativeApi.WSManSessionOption.WSMAN_OPTION_SHELL_MAX_DATA_SIZE_PER_MESSAGE_KB,
                out packetSize);
            // packet size returned is in KB. Convert this into bytes..
            Fragmentor.FragmentSize = packetSize << 10;

            // Get robust connections maximum retries time.
            WSManNativeApi.WSManGetSessionOptionAsDword(wsManSessionHandle,
                WSManNativeApi.WSManSessionOption.WSMAN_OPTION_MAX_RETRY_TIME,
                out this.maxRetryTime);

            this.dataToBeSent.Fragmentor = base.Fragmentor;
            noCompression = !connectionInfo.UseCompression;
            noMachineProfile = connectionInfo.NoMachineProfile;
            
            // set other WSMan session related defaults            
            if (isSSLSpecified)
            {
                // WSMan Port DCR related changes - BUG 542726
                // this session option will tell WSMan to use port for HTTPS from
                // config provider.
                SetWSManSessionOption(WSManNativeApi.WSManSessionOption.WSMAN_OPTION_USE_SSL, 1);
            }
            if (connectionInfo.NoEncryption)
            {
                // send unencrypted messages
                SetWSManSessionOption(WSManNativeApi.WSManSessionOption.WSMAN_OPTION_UNENCRYPTED_MESSAGES, 1);
            }
            // check if implicit credentials can be used for Negotiate
            if (connectionInfo.AllowImplicitCredentialForNegotiate)
            {
                result = WSManNativeApi.WSManSetSessionOption(wsManSessionHandle,
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
        /// Logic in transport callbacks should always use this to process a transport error
        /// </summary>
        internal void ProcessWSManTransportError(TransportErrorOccuredEventArgs eventArgs)
        {
            EnqueueAndStartProcessingThread(null, eventArgs, null);
        }
        
        /// <summary>
        /// Log the error message in the Crimson logger and raise error handler.
        /// </summary>
        /// <param name="eventArgs"></param>
        internal override void RaiseErrorHandler(TransportErrorOccuredEventArgs eventArgs)
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
            PSEtwLog.LogOperationalError(PSEventId.TransportError, PSOpcode.Open, PSTask.None, PSKeyword.UseAlwaysOperational,
                RunspacePoolInstanceId.ToString(),
                Guid.Empty.ToString(),
                eventArgs.Exception.ErrorCode.ToString(CultureInfo.InvariantCulture),
                eventArgs.Exception.Message,
                stackTrace);

            PSEtwLog.LogAnalyticError(PSEventId.TransportError_Analytic,
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
        /// receive/send operation handles and callback handles should be released/disposed from
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
                if (null != sendToRemoteCompleted)
                {
                    sendToRemoteCompleted.Dispose();
                    sendToRemoteCompleted = null;
                }

                // For send..clear always
                if (IntPtr.Zero != wsManSendOperationHandle)
                {
                    WSManNativeApi.WSManCloseOperation(wsManSendOperationHandle, 0);
                    wsManSendOperationHandle = IntPtr.Zero;
                }
            }
            else
            {
                // clearing for receive..Clear only when the end of operation is reached.
                if (flags == (int)WSManNativeApi.WSManCallbackFlags.WSMAN_FLAG_CALLBACK_END_OF_OPERATION)
                {
                    if (IntPtr.Zero != wsManRecieveOperationHandle)
                    {
                        WSManNativeApi.WSManCloseOperation(wsManRecieveOperationHandle, 0);
                        wsManRecieveOperationHandle = IntPtr.Zero;
                    }

                    if (null != receivedFromRemote)
                    {
                        receivedFromRemote.Dispose();
                        receivedFromRemote = null;
                    }
                }
            }
        }

        /// <summary>
        /// Call back from worker thread / queue to raise Robust Connection notification event.
        /// </summary>
        /// <param name="privateData">ConnectionStatusEventArgs</param>
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
        /// Robust connection maximum retry time in milliseconds
        /// </summary>
        internal int MaxRetryConnectionTime
        {
            get { return this.maxRetryTime; }
        }

        /// <summary>
        /// Returns the WSMan's session handle that this Session transportmanager
        /// is proxying.
        /// </summary>
        internal IntPtr SessionHandle
        {
            get { return this.wsManSessionHandle;  }
        }

        /// <summary>
        /// Returns the WSManConnectionInfo used to make the connection.
        /// </summary>
        internal WSManConnectionInfo ConnectionInfo
        {
            get { return this._connectionInfo;  }
        }

        /// <summary>
        /// Examine the session create error code and if the error is one where a 
        /// session create/connect retry attempt may be beneficial then do the
        /// retry attempt.
        /// </summary>
        /// <param name="sessionCreateErrorCode">Error code returned from Create response</param>
        /// <returns>True if a session create retry has been started.</returns>
        private bool RetrySessionCreation(int sessionCreateErrorCode)
        {
            if (_connectionRetryCount >= _connectionInfo.MaxConnectionRetryCount) { return false; }

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
            startMode = WSManTransportManagerUtils.tmStartModes.None;
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

            PSEtwLog.LogAnalyticInformational(PSEventId.WSManCreateShellCallbackReceived,
                PSOpcode.Connect, PSTask.None, PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                sessionTM.RunspacePoolInstanceId.ToString());

            // TODO: 188098 wsManShellOperationHandle should be populated by WSManCreateShellEx, 
            // but there is a thread timing bug in WSMan layer causing the callback to
            // be called before WSManCreateShellEx returns. since we already validated the
            // shell context exists, safely assigning the shellOperationHandle to shell transport manager.
            // Remove this once WSMan fixes its code.
            sessionTM.wsManShellOperationHandle = shellOperationHandle;

            lock (sessionTM.syncObject)
            {
                // Already close request is made. So return
                if (sessionTM.isClosed)
                {
                    return;
                }
            }

            if (IntPtr.Zero != error)
            {
                WSManNativeApi.WSManError errorStruct = WSManNativeApi.WSManError.UnMarshal(error);
                
                if (errorStruct.errorCode != 0)
                {
                    tracer.WriteLine("Got error with error code {0}. Message {1}", errorStruct.errorCode, errorStruct.errorDetail);

                    // Test error code for possible session connection retry.
                    if (sessionTM.RetrySessionCreation(errorStruct.errorCode))
                    {
                        // If a connection retry is being attempted (on 
                        // another thread) then return without processing error.
                        return;
                    }

                    TransportErrorOccuredEventArgs eventargs = WSManTransportManagerUtils.ConstructTransportErrorEventArgs(
                        sessionTM.wsManApiData.WSManAPIHandle,
                        sessionTM,
                        errorStruct,
                        TransportMethodEnum.CreateShellEx,
                        RemotingErrorIdStrings.ConnectExCallBackError,
                        new object[] { sessionTM.ConnectionInfo.ComputerName, WSManTransportManagerUtils.ParseEscapeWSManErrorMessage(errorStruct.errorDetail) });
                    sessionTM.ProcessWSManTransportError(eventargs);

                    return;
                }
            }

            //check if the session supports disconnect
            sessionTM.supportsDisconnect = ((flags & (int)WSManNativeApi.WSManCallbackFlags.WSMAN_FLAG_CALLBACK_SHELL_SUPPORTS_DISCONNECT) != 0) ? true : false;

            // openContent is used by redirection ie., while redirecting to 
            // a new machine.. this is not needed anymore as the connection
            // is successfully established.
            if (null != sessionTM.openContent)
            {
                sessionTM.openContent.Dispose();
                sessionTM.openContent = null;
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

            PSEtwLog.LogAnalyticInformational(PSEventId.WSManCloseShellCallbackReceived,
                PSOpcode.Disconnect, PSTask.None, PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                sessionTM.RunspacePoolInstanceId.ToString());

            if (IntPtr.Zero != error)
            {
                WSManNativeApi.WSManError errorStruct = WSManNativeApi.WSManError.UnMarshal(error);

                if (errorStruct.errorCode != 0)
                {
                    tracer.WriteLine("Got error with error code {0}. Message {1}", errorStruct.errorCode, errorStruct.errorDetail);

                    TransportErrorOccuredEventArgs eventargs = WSManTransportManagerUtils.ConstructTransportErrorEventArgs(
                        sessionTM.wsManApiData.WSManAPIHandle,
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

            //LOG ETW EVENTS

            // Dispose the OnDisconnect callback as it is not needed anymore
            if (null != sessionTM.disconnectSessionCompleted)
            {
                sessionTM.disconnectSessionCompleted.Dispose();
                sessionTM.disconnectSessionCompleted = null;
            }

            if (IntPtr.Zero != error)
            {
                WSManNativeApi.WSManError errorStruct = WSManNativeApi.WSManError.UnMarshal(error);

                if (errorStruct.errorCode != 0)
                {
                    tracer.WriteLine("Got error with error code {0}. Message {1}", errorStruct.errorCode, errorStruct.errorDetail);

                    TransportErrorOccuredEventArgs eventargs = WSManTransportManagerUtils.ConstructTransportErrorEventArgs(
                        sessionTM.wsManApiData.WSManAPIHandle,
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

                //Log ETW traces                
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

            //Add ETW events

            // Dispose the OnCreate callback as it is not needed anymore
            if (null != sessionTM.reconnectSessionCompleted)
            {
                sessionTM.reconnectSessionCompleted.Dispose();
                sessionTM.reconnectSessionCompleted = null;
            }

            if (IntPtr.Zero != error)
            {
                WSManNativeApi.WSManError errorStruct = WSManNativeApi.WSManError.UnMarshal(error);

                if (errorStruct.errorCode != 0)
                {
                    tracer.WriteLine("Got error with error code {0}. Message {1}", errorStruct.errorCode, errorStruct.errorDetail);

                    TransportErrorOccuredEventArgs eventargs = WSManTransportManagerUtils.ConstructTransportErrorEventArgs(
                        sessionTM.wsManApiData.WSManAPIHandle,
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

            if (IntPtr.Zero != error)
            {
                WSManNativeApi.WSManError errorStruct = WSManNativeApi.WSManError.UnMarshal(error);

                if (errorStruct.errorCode != 0)
                {
                    tracer.WriteLine("Got error with error code {0}. Message {1}", errorStruct.errorCode, errorStruct.errorDetail);

                    TransportErrorOccuredEventArgs eventargs = WSManTransportManagerUtils.ConstructTransportErrorEventArgs(
                        sessionTM.wsManApiData.WSManAPIHandle,
                        sessionTM,
                        errorStruct,
                        TransportMethodEnum.ConnectShellEx,
                        RemotingErrorIdStrings.ConnectExCallBackError,
                        new object[] { sessionTM.ConnectionInfo.ComputerName, WSManTransportManagerUtils.ParseEscapeWSManErrorMessage(errorStruct.errorDetail) });
                    sessionTM.ProcessWSManTransportError(eventargs);

                    return;
                }
            }

            //dispose openContent
            if (null != sessionTM.openContent)
            {
                sessionTM.openContent.Dispose();
                sessionTM.openContent = null;
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

            //process returned Xml
            Dbg.Assert(data != null, "WSManConnectShell callback returned null data");
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
            PSEtwLog.LogAnalyticInformational(PSEventId.WSManSendShellInputExCallbackReceived,
                PSOpcode.Connect, PSTask.None, PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                sessionTM.RunspacePoolInstanceId.ToString(), Guid.Empty.ToString());

            if (!shellOperationHandle.Equals(sessionTM.wsManShellOperationHandle))
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

            if (IntPtr.Zero != error)
            {
                WSManNativeApi.WSManError errorStruct = WSManNativeApi.WSManError.UnMarshal(error);

                // Ignore operation aborted error. operation aborted is raised by WSMan to 
                // notify operation complete. PowerShell protocol has its own
                // way of notifying the same using state change events.
                if ((errorStruct.errorCode != 0) && (errorStruct.errorCode != 995))
                {
                    tracer.WriteLine("Got error with error code {0}. Message {1}", errorStruct.errorCode, errorStruct.errorDetail);

                    TransportErrorOccuredEventArgs eventargs = WSManTransportManagerUtils.ConstructTransportErrorEventArgs(
                        sessionTM.wsManApiData.WSManAPIHandle,
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

            if (!shellOperationHandle.Equals(sessionTM.wsManShellOperationHandle))
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

            if (IntPtr.Zero != error)
            {
                WSManNativeApi.WSManError errorStruct = WSManNativeApi.WSManError.UnMarshal(error);

                if (errorStruct.errorCode != 0)
                {
                    tracer.WriteLine("Got error with error code {0}. Message {1}", errorStruct.errorCode, errorStruct.errorDetail);

                    TransportErrorOccuredEventArgs eventargs = WSManTransportManagerUtils.ConstructTransportErrorEventArgs(
                        sessionTM.wsManApiData.WSManAPIHandle,
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
            if (null != dataReceived.data)
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
            byte[] data = dataToBeSent.ReadOrRegisterCallback(onDataAvailableToSendCallback,
                out priorityType);
            if (null != data)
            {
                SendData(data, priorityType);
            }
        }

        private void OnDataAvailableCallback(byte[] data, DataPriorityType priorityType)
        {
            Dbg.Assert(null != data, "data cannot be null in the data available callback");

            tracer.WriteLine("Received data to be sent from the callback.");
            SendData(data, priorityType);
        }

        private void SendData(byte[] data, DataPriorityType priorityType)
        {
            tracer.WriteLine("Session sending data of size : {0}", data.Length);
            byte[] package = data;

            #region SHIM: Redirection code for session data send.

            bool sendContinue = true;

            if (sessionSendRedirect != null)
            {
                object[] arguments = new object[2] { null, package };
                sendContinue = (bool)sessionSendRedirect.DynamicInvoke(arguments);
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
                    sendToRemoteCompleted = new WSManNativeApi.WSManShellAsync(new IntPtr(sessionContextID), sessionSendCallback);
                    WSManNativeApi.WSManSendShellInputEx(wsManShellOperationHandle, IntPtr.Zero, 0,
                        priorityType == DataPriorityType.Default ? 
                            WSManNativeApi.WSMAN_STREAM_ID_STDIN : WSManNativeApi.WSMAN_STREAM_ID_PROMPTRESPONSE, 
                        serializedContent, 
                        sendToRemoteCompleted, 
                        ref wsManSendOperationHandle);
                }
            }
        }

        #endregion

        #region Dispose / Destructor pattern

        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed")]
        internal override void Dispose(bool isDisposing)
        {
            tracer.WriteLine("Disposing session with session context: {0} Operation Context: {1}", sessionContextID, wsManShellOperationHandle);

            CloseSessionAndClearResources();

            DisposeWSManAPIDataAsync();

            // openContent is used by redirection ie., while redirecting to 
            // a new machine and hence this is cleared only when the session
            // is disposing.
            if (isDisposing && (null != openContent))
            {
                openContent.Dispose();
                openContent = null;
            }

            base.Dispose(isDisposing);
        }

        /// <summary>
        /// Closes current session handle by calling WSManCloseSession and clears
        /// session related resources.
        /// </summary>
        private void CloseSessionAndClearResources()
        {
            tracer.WriteLine("Clearing session with session context: {0} Operation Context: {1}", sessionContextID, wsManShellOperationHandle);

            // Taking a copy of session handle as we should call WSManCloseSession only once and 
            // clear the original value. This will protect us if Dispose() is called twice.
            IntPtr tempWSManSessionHandle = wsManSessionHandle;
            wsManSessionHandle = IntPtr.Zero;
            // Call WSManCloseSession on a different thread as Dispose can be called from one of
            // the WSMan callback threads. WSMan does not support closing a session in the callbacks.
            ThreadPool.QueueUserWorkItem(new WaitCallback(
                // wsManSessionHandle is passed as parameter to allow the thread to be independent
                // of the rest of the parent object.
                delegate(object state)
                {
                    IntPtr sessionHandle = (IntPtr)state;
                    if (sessionHandle != IntPtr.Zero)
                    {
                        WSManNativeApi.WSManCloseSession(sessionHandle, 0);
                    }
                }), tempWSManSessionHandle);

            // remove session context from session handles dictionary
            RemoveSessionTransportManager(sessionContextID);

            if (null != closeSessionCompleted)
            {
                closeSessionCompleted.Dispose();
                closeSessionCompleted = null;
            }

            // Dispose the create session completed callback here, since it is 
            // used for periodic robust connection retry/auto-disconnect 
            // notifications while the shell is active.
            if (null != createSessionCallback)
            {
                createSessionCallbackGCHandle.Free();
                createSessionCallback.Dispose();
                createSessionCallback = null;
            }

            // Dispose the OnConnect callback if one present
            if (null != connectSessionCallback)
            {
                connectSessionCallback.Dispose();
                connectSessionCallback = null;
            }

            // Reset the session context Id to zero so that a new one will be generated for 
            // any following redirected session.
            sessionContextID = 0;
        }

        private void DisposeWSManAPIDataAsync()
        {
            WSManAPIDataCommon tempWSManApiData = wsManApiData;
            if (tempWSManApiData == null) { return; }
            wsManApiData = null;

            // Dispose and de-initialize the WSManAPIData instance object on separate worker thread to ensure 
            // it is not run on a WinRM thread (which will fail).
            // Note that WSManAPIData.Dispose() method is thread safe.
            System.Threading.ThreadPool.QueueUserWorkItem(
                (state) =>
                {
                    tempWSManApiData.Dispose();
                });
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
            private IntPtr handle;
            // if any
            private int errorCode;
            private WSManNativeApi.WSManStreamIDSet_ManToUn inputStreamSet;
            private WSManNativeApi.WSManStreamIDSet_ManToUn outputStreamSet;
            private List<WSManNativeApi.WSManOption> commonOptionSet;
            // Dispose
            private bool isDisposed;
            private object syncObject = new object();
#if !CORECLR
            private WindowsIdentity _identityToImpersonate;
#endif

            /// <summary>
            /// Initializes handle by calling WSManInitialize API
            /// </summary>
            internal WSManAPIDataCommon()
            {
#if !CORECLR
                // Check for thread impersonation and save identity for later de-initialization.
                _identityToImpersonate = WindowsIdentity.GetCurrent();
                _identityToImpersonate = (_identityToImpersonate.ImpersonationLevel == TokenImpersonationLevel.Impersonation) ?
                    _identityToImpersonate : null;
#endif

                handle = IntPtr.Zero;
                errorCode = WSManNativeApi.WSManInitialize(WSManNativeApi.WSMAN_FLAG_REQUESTED_API_VERSION_1_1, ref handle);

                // input / output streams common to all connections
                inputStreamSet = new WSManNativeApi.WSManStreamIDSet_ManToUn(
                    new string[] { 
                        WSManNativeApi.WSMAN_STREAM_ID_STDIN, 
                        WSManNativeApi.WSMAN_STREAM_ID_PROMPTRESPONSE 
                    });
                outputStreamSet = new WSManNativeApi.WSManStreamIDSet_ManToUn(
                    new string[] { WSManNativeApi.WSMAN_STREAM_ID_STDOUT} );


                // startup options common to all connections
                WSManNativeApi.WSManOption protocolStartupOption = new WSManNativeApi.WSManOption();
                protocolStartupOption.name = RemoteDataNameStrings.PS_STARTUP_PROTOCOL_VERSION_NAME;
                protocolStartupOption.value = RemotingConstants.ProtocolVersion.ToString();
                protocolStartupOption.mustComply = true;

                commonOptionSet = new List<WSManNativeApi.WSManOption>();
                commonOptionSet.Add(protocolStartupOption);
            }

            internal int ErrorCode { get { return errorCode; } }
            internal WSManNativeApi.WSManStreamIDSet_ManToUn InputStreamSet { get { return inputStreamSet; } }
            internal WSManNativeApi.WSManStreamIDSet_ManToUn OutputStreamSet { get { return outputStreamSet; } }
            internal List<WSManNativeApi.WSManOption> CommonOptionSet { get { return commonOptionSet; } }
            internal IntPtr WSManAPIHandle { get { return handle; } }

            /// <summary>
            /// Dispose
            /// </summary>
            // Suppress this message. The result is actually used, but only in checked builds....
            [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults",
               MessageId = "System.Management.Automation.Remoting.Client.WSManNativeApi.WSManDeinitialize(System.IntPtr,System.Int32)")]
            [SuppressMessage("Microsoft.Usage", "CA2216:Disposabletypesshoulddeclarefinalizer")]
            public void Dispose()
            {
                lock (syncObject)
                {
                    if (isDisposed) { return; }
                    isDisposed = true;
                }

                inputStreamSet.Dispose();
                outputStreamSet.Dispose();

                if (IntPtr.Zero != handle)
                {
#if !CORECLR
                    // If we initialized with thread impersonation make sure de-initialize is run with same.
                    WindowsImpersonationContext impersonationContext = null;
                    if (_identityToImpersonate != null)
                    {
                        try
                        {
                            _identityToImpersonate.Impersonate() ;
                        }
                        catch (ObjectDisposedException)
                        {
                            handle = IntPtr.Zero;
                            return;
                        }
                    }

                    try
                    {
#endif
                        int result = WSManNativeApi.WSManDeinitialize(handle, 0);
                        Dbg.Assert(result == 0, "WSManDeinitialize returned non-zero value");
#if !CORECLR
                    }
                    finally
                    {
                        if (impersonationContext != null)
                        {
                            try
                            {
                                impersonationContext.Undo();
                                impersonationContext.Dispose();
                            }
                            catch (System.Security.SecurityException) { }
                        }
                    }
#endif
                    handle = IntPtr.Zero;
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
        private IntPtr wsManShellOperationHandle;
        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private IntPtr wsManCmdOperationHandle;
        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private IntPtr cmdSignalOperationHandle;
        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private IntPtr wsManRecieveOperationHandle;
        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private IntPtr wsManSendOperationHandle;
        // this is used with WSMan callbacks to represent a command transport manager.
        private long cmdContextId;
        
        private PrioritySendDataCollection.OnDataAvailableCallback onDataAvailableToSendCallback;

        // should be integrated with receiveDataInitiated
        private bool shouldStartReceivingData;

        // bools used to track and send stop signal only after Create is completed.
        private bool isCreateCallbackReceived;
        private bool isStopSignalPending;
        private bool isDisconnectPending;
        private bool isSendingInput;
        private bool isDisconnectedOnInvoke;

        // callbacks
        private WSManNativeApi.WSManShellAsync createCmdCompleted;
        private WSManNativeApi.WSManShellAsync receivedFromRemote;
        private WSManNativeApi.WSManShellAsync sendToRemoteCompleted;
        private WSManNativeApi.WSManShellAsync reconnectCmdCompleted;
        private WSManNativeApi.WSManShellAsync connectCmdCompleted;
        // TODO: This GCHandle is required as it seems WSMan is calling create callback
        // after we call Close. This seems wrong. Opened bug on WSMan to track this.
        private GCHandle createCmdCompletedGCHandle;
        private WSManNativeApi.WSManShellAsync closeCmdCompleted;
        private WSManNativeApi.WSManShellAsync signalCmdCompleted;
        // this is the chunk that got delivered on onDataAvailableToSendCallback
        // will be sent during subsequent SendOneItem()
        private SendDataChunk chunkToSend;

        private string cmdLine;
        private readonly WSManClientSessionTransportManager _sessnTm;

        private class SendDataChunk
        {

            public SendDataChunk(byte[] data, DataPriorityType type)
            {
                this.data = data;
                this.type = type;
            }

            public byte[] Data
            {
                get
                {
                    return data;
                }
            }

            public DataPriorityType Type
            {
                get
                {
                    return type;
                }
            }
                 
            private byte[] data;
            private DataPriorityType type;
        }

        #endregion

        #region Static Data

        // static callback delegate
        private static WSManNativeApi.WSManShellAsyncCallback cmdCreateCallback;
        private static WSManNativeApi.WSManShellAsyncCallback cmdCloseCallback;
        private static WSManNativeApi.WSManShellAsyncCallback cmdReceiveCallback;
        private static WSManNativeApi.WSManShellAsyncCallback cmdSendCallback;
        private static WSManNativeApi.WSManShellAsyncCallback cmdSignalCallback;
        private static WSManNativeApi.WSManShellAsyncCallback cmdReconnectCallback;
        private static WSManNativeApi.WSManShellAsyncCallback cmdConnectCallback;

        static WSManClientCommandTransportManager()
        {
            // Initialize callback delegates
            WSManNativeApi.WSManShellCompletionFunction createDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnCreateCmdCompleted);
            cmdCreateCallback = new WSManNativeApi.WSManShellAsyncCallback(createDelegate);

            WSManNativeApi.WSManShellCompletionFunction closeDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnCloseCmdCompleted);
            cmdCloseCallback = new WSManNativeApi.WSManShellAsyncCallback(closeDelegate);

            WSManNativeApi.WSManShellCompletionFunction receiveDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnRemoteCmdDataReceived);
            cmdReceiveCallback = new WSManNativeApi.WSManShellAsyncCallback(receiveDelegate);

            WSManNativeApi.WSManShellCompletionFunction sendDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnRemoteCmdSendCompleted);
            cmdSendCallback = new WSManNativeApi.WSManShellAsyncCallback(sendDelegate);

            WSManNativeApi.WSManShellCompletionFunction signalDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnRemoteCmdSignalCompleted);
            cmdSignalCallback = new WSManNativeApi.WSManShellAsyncCallback(signalDelegate);

            WSManNativeApi.WSManShellCompletionFunction reconnectDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnReconnectCmdCompleted);
            cmdReconnectCallback = new WSManNativeApi.WSManShellAsyncCallback(reconnectDelegate);

            WSManNativeApi.WSManShellCompletionFunction connectDelegate =
                new WSManNativeApi.WSManShellCompletionFunction(OnConnectCmdCompleted);
            cmdConnectCallback = new WSManNativeApi.WSManShellAsyncCallback(connectDelegate);
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
        internal WSManClientCommandTransportManager(WSManConnectionInfo connectionInfo,
            IntPtr wsManShellOperationHandle,
            ClientRemotePowerShell shell,
            bool noInput,
            WSManClientSessionTransportManager sessnTM) :
            base(shell, sessnTM.CryptoHelper, sessnTM)
        {
            Dbg.Assert(IntPtr.Zero != wsManShellOperationHandle, "Shell operation handle cannot be IntPtr.Zero.");
            Dbg.Assert(null != connectionInfo, "connectionInfo cannot be null");

            this.wsManShellOperationHandle = wsManShellOperationHandle;
           
            // Apply quota limits.. allow for data to be unlimited..
            ReceivedDataCollection.MaximumReceivedDataSize = connectionInfo.MaximumReceivedDataSizePerCommand;
            ReceivedDataCollection.MaximumReceivedObjectSize = connectionInfo.MaximumReceivedObjectSize;
            cmdLine = shell.PowerShell.Commands.Commands.GetCommandStringForHistory();
            onDataAvailableToSendCallback =
                new PrioritySendDataCollection.OnDataAvailableCallback(OnDataAvailableCallback);
            _sessnTm = sessnTM;
            // Suspend queue on robust connections initiated event.
            sessnTM.RobustConnectionsInitiated  += HandleRobustConnectionsIntiated;

            // Resume queue on robust connections completed event.
            sessnTM.RobustConnectionsCompleted += HandleRobusConnectionsCompleted;
        }

        #endregion

        #region SHIM: Redirection delegate for command code send.

        private static Delegate commandCodeSendRedirect = null;

        #endregion

        #region Internal Methods / Properties

        private void HandleRobustConnectionsIntiated(object sender, EventArgs e)
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
            cmdContextId = GetNextCmdTMHandleId();
            AddCmdTransportManager(cmdContextId, this);

            // Create Callback
            connectCmdCompleted = new WSManNativeApi.WSManShellAsync(new IntPtr(cmdContextId), cmdConnectCallback);
            reconnectCmdCompleted = new WSManNativeApi.WSManShellAsync(new IntPtr(cmdContextId), cmdReconnectCallback);
            lock (syncObject)
            {
                if (isClosed)
                {
                    // the transport is already closed..so no need to create a connection
                    // anymore.
                    return;
                }

                WSManNativeApi.WSManConnectShellCommandEx(wsManShellOperationHandle,
                    0,
                    PowershellInstanceId.ToString().ToUpperInvariant(),
                    IntPtr.Zero,
                    IntPtr.Zero,
                    connectCmdCompleted,
                    ref wsManCmdOperationHandle);
            }

            if (wsManCmdOperationHandle == IntPtr.Zero)
            {
                PSRemotingTransportException e = new PSRemotingTransportException(RemotingErrorIdStrings.RunShellCommandExFailed);
                TransportErrorOccuredEventArgs eventargs =
                    new TransportErrorOccuredEventArgs(e, TransportMethodEnum.ConnectShellCommandEx);
                ProcessWSManTransportError(eventargs);
                return;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="PSRemotingTransportException">
        /// WSManRunShellCommandEx failed.
        /// </exception>
        internal override void CreateAsync()
        {
            byte[] cmdPart1 = serializedPipeline.ReadOrRegisterCallback(null);
            if (null != cmdPart1)
            {
                #region SHIM: Redirection code for command code send.

                bool sendContinue = true;

                if (commandCodeSendRedirect != null)
                {
                    object[] arguments = new object[2] { null, cmdPart1 };
                    sendContinue = (bool)commandCodeSendRedirect.DynamicInvoke(arguments);
                    cmdPart1 = (byte[])arguments[0];
                }

                if (!sendContinue)
                    return;

                #endregion

                WSManNativeApi.WSManCommandArgSet argSet = new WSManNativeApi.WSManCommandArgSet(cmdPart1);

                // create cmdContextId
                cmdContextId = GetNextCmdTMHandleId();
                AddCmdTransportManager(cmdContextId, this);
                
                PSEtwLog.LogAnalyticInformational(PSEventId.WSManCreateCommand, PSOpcode.Connect,
                                PSTask.CreateRunspace, PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                                RunspacePoolInstanceId.ToString(), powershellInstanceId.ToString());
                
                createCmdCompleted = new WSManNativeApi.WSManShellAsync(new IntPtr(cmdContextId), cmdCreateCallback);
                createCmdCompletedGCHandle = GCHandle.Alloc(createCmdCompleted);
                reconnectCmdCompleted = new WSManNativeApi.WSManShellAsync(new IntPtr(cmdContextId), cmdReconnectCallback);

                using (argSet)
                {
                    lock (syncObject)
                    {
                        if (!isClosed)
                        {
                            WSManNativeApi.WSManRunShellCommandEx(wsManShellOperationHandle,
                                0,
                                PowershellInstanceId.ToString().ToUpperInvariant(),
                                // WSManRunsShellCommand doesn't accept empty string "".
                                (cmdLine == null || cmdLine.Length==0)? " ": (cmdLine.Length <= 256? cmdLine : cmdLine.Substring(0, 255)),
                                argSet,
                                IntPtr.Zero,
                                createCmdCompleted,
                                ref wsManCmdOperationHandle);

                            tracer.WriteLine("Started cmd with command context : {0} Operation context: {1}", cmdContextId, wsManCmdOperationHandle);
                        }
                    }
                }
            }

            if (wsManCmdOperationHandle == IntPtr.Zero)
            {
                PSRemotingTransportException e = new PSRemotingTransportException(RemotingErrorIdStrings.RunShellCommandExFailed);
                TransportErrorOccuredEventArgs eventargs =
                    new TransportErrorOccuredEventArgs(e, TransportMethodEnum.RunShellCommandEx);
                ProcessWSManTransportError(eventargs);
                return;
            }
        }

        /// <summary>
        /// Restores connection on a disconnected command
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

                WSManNativeApi.WSManReconnectShellCommandEx(wsManCmdOperationHandle, 0, reconnectCmdCompleted);                
            }
        }
        /// <summary>
        /// Used by powershell/pipeline to send a stop message to the server command
        /// </summary>
        internal override void SendStopSignal()
        {
            lock (syncObject)
            {
                if (isClosed == true)
                {
                    return;
                }

                // WSMan API do not allow a signal/input/receive be sent until RunShellCommand is
                // successfull (ie., callback is received)..so note that a signal is to be sent
                // here and return.
                if (!isCreateCallbackReceived)
                {
                    isStopSignalPending = true;
                    return;
                }
                // we are about to send a signal..so clear pending bit.
                isStopSignalPending = false;

                tracer.WriteLine("Sending stop signal with command context: {0} Operation Context {1}", cmdContextId, wsManCmdOperationHandle);
                PSEtwLog.LogAnalyticInformational(PSEventId.WSManSignal,
                    PSOpcode.Disconnect, PSTask.None, PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                    RunspacePoolInstanceId.ToString(), powershellInstanceId.ToString(), StopSignal);

                signalCmdCompleted = new WSManNativeApi.WSManShellAsync(new IntPtr(cmdContextId), cmdSignalCallback);
                WSManNativeApi.WSManSignalShellEx(wsManShellOperationHandle, wsManCmdOperationHandle, 0,
                    StopSignal, signalCmdCompleted, ref cmdSignalOperationHandle);
            }
        }

        /// <summary>
        /// Closes the pending Create,Send,Receive operations and then closes the shell and release all the resources.
        /// </summary>
        internal override void CloseAsync()
        {
            tracer.WriteLine("Closing command with command context: {0} Operation Context {1}", cmdContextId, wsManCmdOperationHandle);

            bool shouldRaiseCloseCompleted = false;
            // then let other threads release the lock before we cleaning up the resources.
            lock (syncObject)
            {
                if (isClosed == true)
                {
                    return;
                }

                // first change the state..so other threads
                // will know that we are closing.
                isClosed = true;

                // There is no valid cmd operation handle..so just
                // raise close completed.
                if (IntPtr.Zero == wsManCmdOperationHandle)
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
                    RemoveCmdTransportManager(cmdContextId);
                }
                return;
            }

            PSEtwLog.LogAnalyticInformational(PSEventId.WSManCloseCommand,
                PSOpcode.Disconnect, PSTask.None, PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                RunspacePoolInstanceId.ToString(), powershellInstanceId.ToString());
            closeCmdCompleted = new WSManNativeApi.WSManShellAsync(new IntPtr(cmdContextId), cmdCloseCallback);
            Dbg.Assert((IntPtr)closeCmdCompleted != IntPtr.Zero, "closeCmdCompleted callback is null in cmdTM.CloseAsync()");
            WSManNativeApi.WSManCloseCommand(wsManCmdOperationHandle, 0, closeCmdCompleted);
        }
        
        /// <summary>
        /// Handle transport error - calls EnqueueAndStartProcessingThread to process transport exception
        /// in a different thread
        /// Logic in transport callbacks should always use this to process a transport error
        /// </summary>
        internal void ProcessWSManTransportError(TransportErrorOccuredEventArgs eventArgs)
        {
            EnqueueAndStartProcessingThread(null, eventArgs, null);
        }

        /// <summary>
        /// Log the error message in the Crimson logger and raise error handler.
        /// </summary>
        /// <param name="eventArgs"></param>
        internal override void RaiseErrorHandler(TransportErrorOccuredEventArgs eventArgs)
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
            Dbg.Assert(null != privateData, "privateData cannot be null.");

            // For this version...only a boolean can be used for privateData.
            bool shouldRaiseSignalCompleted = (bool)privateData;
            if (shouldRaiseSignalCompleted)
            {
                base.RaiseSignalCompleted();
            }
        }

        /// <summary>
        /// receive/send operation handles and callback handles should be released/disposed from
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
                if (null != sendToRemoteCompleted)
                {
                    sendToRemoteCompleted.Dispose();
                    sendToRemoteCompleted = null;
                }

                // For send..clear always
                if (IntPtr.Zero != wsManSendOperationHandle)
                {
                    WSManNativeApi.WSManCloseOperation(wsManSendOperationHandle, 0);
                    wsManSendOperationHandle = IntPtr.Zero;
                }
            }
            else
            {
                // clearing for receive..Clear only when the end of operation is reached.
                if (flags == (int)WSManNativeApi.WSManCallbackFlags.WSMAN_FLAG_CALLBACK_END_OF_OPERATION)
                {
                    if (IntPtr.Zero != wsManRecieveOperationHandle)
                    {
                        WSManNativeApi.WSManCloseOperation(wsManRecieveOperationHandle, 0);
                        wsManRecieveOperationHandle = IntPtr.Zero;
                    }

                    if (null != receivedFromRemote)
                    {
                        receivedFromRemote.Dispose();
                        receivedFromRemote = null;
                    }
                }
            }
        }

        /// <summary>
        /// Method to have transport prepare for a disconnect operation.
        /// </summary>
        internal override void PrepareForDisconnect()
        {
            this.isDisconnectPending = true;

            // If there is not input processing and the command has already been created
            // on the server then this object is ready for Disconnect now.
            // Otherwise let the sending input data call back handle it.
            if (this.isClosed || this.isDisconnectedOnInvoke ||
                (this.isCreateCallbackReceived &&
                 this.serializedPipeline.Length == 0 &&
                 !this.isSendingInput))
            {
                RaiseReadyForDisconnect();
            }
        }

        /// <summary>
        /// Method to resume post disconnect operations.
        /// </summary>
        internal override void PrepareForConnect()
        {
            this.isDisconnectPending = false;
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

            PSEtwLog.LogAnalyticInformational(PSEventId.WSManCreateCommandCallbackReceived,
                PSOpcode.Connect, PSTask.None, PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                cmdTM.RunspacePoolInstanceId.ToString(), cmdTM.powershellInstanceId.ToString());

            // dispose the cmdCompleted callback as it is not needed any more
            if (null != cmdTM.createCmdCompleted)
            {
                cmdTM.createCmdCompletedGCHandle.Free();
                cmdTM.createCmdCompleted.Dispose();
                cmdTM.createCmdCompleted = null;
            }

            // TODO: 188098 wsManCmdOperationHandle should be populated by WSManRunShellCommandEx, 
            // but there is a thread timing bug in WSMan layer causing the callback to
            // be called before WSManRunShellCommandEx returns. since we already validated the
            // cmd context exists, safely assigning the commandOperationHandle to cmd transport manager.
            // Remove this once WSMan fixes its code.
            cmdTM.wsManCmdOperationHandle = commandOperationHandle;           

            if (IntPtr.Zero != error)
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

            // Send remaing cmd / parameter fragments.
            lock (cmdTM.syncObject)
            {
                cmdTM.isCreateCallbackReceived = true;

                // make sure the transport is not closed yet.
                if (cmdTM.isClosed)
                {
                    tracer.WriteLine("Client Session TM: Transport manager is closed. So returning");

                    if (cmdTM.isDisconnectPending)
                    {
                        cmdTM.RaiseReadyForDisconnect();
                    }

                    return;
                }

                // If a disconnect is pending at this point then we should not start
                // receiving data or sending input and let the disconnect take place.
                if (cmdTM.isDisconnectPending)
                {
                    cmdTM.RaiseReadyForDisconnect();
                    return;
                }

                if (cmdTM.serializedPipeline.Length == 0)
                {
                    cmdTM.shouldStartReceivingData = true;
                }

                // Start sending data if any..and see if we can initiate a receive.
                cmdTM.SendOneItem();

                // WSMan API does not allow a signal/input/receive be sent until RunShellCommand is
                // successfull (ie., callback is received)
                if (cmdTM.isStopSignalPending)
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

            long cmdContextId = 0;
            WSManClientCommandTransportManager cmdTM = null;
            if (!TryGetCmdTransportManager(operationContext, out cmdTM, out cmdContextId))
            {
                // We dont have the command TM handle..just return.
                tracer.WriteLine("OnConnectCmdCompleted: Unable to find a transport manager for the command context {0}.", cmdContextId);
                return;
            }

            // dispose the cmdCompleted callback as it is not needed any more
            if (null != cmdTM.connectCmdCompleted)
            {
                cmdTM.connectCmdCompleted.Dispose();
                cmdTM.connectCmdCompleted = null;
            }

            cmdTM.wsManCmdOperationHandle = commandOperationHandle;

            if (IntPtr.Zero != error)
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
                    if (cmdTM.isDisconnectPending)
                    {
                        cmdTM.RaiseReadyForDisconnect();
                    }

                    return;
                }

                // If a disconnect is pending at this point then we should not start
                // receiving data or sending input and let the disconnect take place.
                if (cmdTM.isDisconnectPending)
                {
                    cmdTM.RaiseReadyForDisconnect();
                    return;
                }

                // Allow SendStopSignal.
                cmdTM.isCreateCallbackReceived = true;

                // Send stop signal if it is pending.
                if (cmdTM.isStopSignalPending)
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

            long cmdContextId = 0;
            WSManClientCommandTransportManager cmdTM = null;
            if (!TryGetCmdTransportManager(operationContext, out cmdTM, out cmdContextId))
            {
                // We dont have the command TM handle..just return.
                tracer.WriteLine("OnCloseCmdCompleted: Unable to find a transport manager for the command context {0}.", cmdContextId);
                return;
            }

            tracer.WriteLine("Close completed callback received for command: {0}", cmdTM.cmdContextId);
            PSEtwLog.LogAnalyticInformational(PSEventId.WSManCloseCommandCallbackReceived,
                PSOpcode.Disconnect, PSTask.None, PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                cmdTM.RunspacePoolInstanceId.ToString(), cmdTM.powershellInstanceId.ToString());

            if (cmdTM.isDisconnectPending)
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

            long cmdContextId = 0;
            WSManClientCommandTransportManager cmdTM = null;
            if (!TryGetCmdTransportManager(operationContext, out cmdTM, out cmdContextId))
            {
                // We dont have the command TM handle..just return.
                tracer.WriteLine("Unable to find a transport manager for the command context {0}.", cmdContextId);
                return;
            }

            cmdTM.isSendingInput = false;

            // do the logging for this send
            PSEtwLog.LogAnalyticInformational(PSEventId.WSManSendShellInputExCallbackReceived,
                PSOpcode.Connect, PSTask.None, PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                cmdTM.RunspacePoolInstanceId.ToString(), cmdTM.powershellInstanceId.ToString());

            if ((!shellOperationHandle.Equals(cmdTM.wsManShellOperationHandle)) ||
                (!commandOperationHandle.Equals(cmdTM.wsManCmdOperationHandle)))
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

                if (cmdTM.isDisconnectPending)
                {
                    cmdTM.RaiseReadyForDisconnect();
                }

                return;
            }

            if (IntPtr.Zero != error)
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

            long cmdContextId = 0;
            WSManClientCommandTransportManager cmdTM = null;
            if (!TryGetCmdTransportManager(operationContext, out cmdTM, out cmdContextId))
            {
                // We dont have the command TM handle..just return.
                tracer.WriteLine("Unable to find a transport manager for the given command context {0}.", cmdContextId);
                return;
            }

            if ((!shellOperationHandle.Equals(cmdTM.wsManShellOperationHandle)) ||
                (!commandOperationHandle.Equals(cmdTM.wsManCmdOperationHandle)))
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

            if (IntPtr.Zero != error)
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

            if (flags == (int) WSManNativeApi.WSManCallbackFlags.WSMAN_FLAG_RECEIVE_DELAY_STREAM_REQUEST_PROCESSED)
            {
                cmdTM.isDisconnectedOnInvoke = true;
                cmdTM.RaiseDelayStreamProcessedEvent();
                return;
            }

            WSManNativeApi.WSManReceiveDataResult dataReceived = WSManNativeApi.WSManReceiveDataResult.UnMarshal(data);
            if (null != dataReceived.data)
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
            if (!TryGetCmdTransportManager(operationContext, out cmdTM, out cmdContextId))
            {
                // We dont have the command TM handle..just return.
                tracer.WriteLine("Unable to find a transport manager for the given command context {0}.", cmdContextId);
                return;
            }

            if ((!shellOperationHandle.Equals(cmdTM.wsManShellOperationHandle)) ||
               (!commandOperationHandle.Equals(cmdTM.wsManCmdOperationHandle)))
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

            if (IntPtr.Zero != error)
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
            cmdTM.shouldStartReceivingData = true;
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

            long cmdContextId = 0;
            WSManClientCommandTransportManager cmdTM = null;
            if (!TryGetCmdTransportManager(operationContext, out cmdTM, out cmdContextId))
            {
                // We dont have the command TM handle..just return.
                tracer.WriteLine("Unable to find a transport manager for the given command context {0}.", cmdContextId);
                return;
            }

            // log the callback received event.
            PSEtwLog.LogAnalyticInformational(PSEventId.WSManSignalCallbackReceived,
                PSOpcode.Disconnect, PSTask.None, PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                cmdTM.RunspacePoolInstanceId.ToString(), cmdTM.powershellInstanceId.ToString());

            if ((!shellOperationHandle.Equals(cmdTM.wsManShellOperationHandle)) ||
                (!commandOperationHandle.Equals(cmdTM.wsManCmdOperationHandle)))
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
            if (IntPtr.Zero != cmdTM.cmdSignalOperationHandle)
            {
                WSManNativeApi.WSManCloseOperation(cmdTM.cmdSignalOperationHandle, 0);
                cmdTM.cmdSignalOperationHandle = IntPtr.Zero;
            }

            if (null != cmdTM.signalCmdCompleted)
            {
                cmdTM.signalCmdCompleted.Dispose();
                cmdTM.signalCmdCompleted = null;
            }
            
            // if the transport manager is already closed..ignore the errors and return            
            if (cmdTM.isClosed)
            {
                tracer.WriteLine("Client Command TM: Transport manager is closed. So returning");
                return;
            }

            if (IntPtr.Zero != error)
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
            if (this.isDisconnectPending)
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
                // if there are no command / parameter fragements need to be sent
                // start receiving data. Reason: Command will start its execution
                // once command string + parameters are sent.
                if (serializedPipeline.Length == 0)
                {
                    shouldStartReceivingData = true;
                }
            }
            else if (chunkToSend != null) // there is a pending chunk to be sent
            {
                data = chunkToSend.Data;
                priorityType = chunkToSend.Type;
                chunkToSend = null;
            }
            else
            {
                // This will either return data or register callback but doesn't do both.
                data = dataToBeSent.ReadOrRegisterCallback(onDataAvailableToSendCallback, out priorityType);
            }

            if (null != data)
            {
                this.isSendingInput = true;
                SendData(data, priorityType);
            }

            if (shouldStartReceivingData)
            {
                StartReceivingData();
            }
        }

        private void OnDataAvailableCallback(byte[] data, DataPriorityType priorityType)
        {
            Dbg.Assert(null != data, "data cannot be null in the data available callback");

            tracer.WriteLine("Received data from dataToBeSent store.");
            Dbg.Assert(chunkToSend == null, "data callback received while a chunk is pending to be sent");
            chunkToSend = new SendDataChunk(data, priorityType);
            SendOneItem();
        }

        #region SHIM: Redirection delegate for command data send.

        private static Delegate commandSendRedirect = null;

        #endregion

        private void SendData(byte[] data, DataPriorityType priorityType)
        {
            tracer.WriteLine("Command sending data of size : {0}", data.Length);
            byte[] package = data;

            #region SHIM: Redirection code for command data send.

            bool sendContinue = true;

            if (commandSendRedirect != null)
            {
                object[] arguments = new object[2] { null, package };
                sendContinue = (bool)commandSendRedirect.DynamicInvoke(arguments);
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
                    sendToRemoteCompleted = new WSManNativeApi.WSManShellAsync(new IntPtr(cmdContextId), cmdSendCallback);
                    WSManNativeApi.WSManSendShellInputEx(wsManShellOperationHandle, wsManCmdOperationHandle, 0,
                        priorityType == DataPriorityType.Default ? 
                            WSManNativeApi.WSMAN_STREAM_ID_STDIN : WSManNativeApi.WSMAN_STREAM_ID_PROMPTRESPONSE, 
                        serializedContent, 
                        sendToRemoteCompleted, 
                        ref wsManSendOperationHandle);
                }
            }
        }

        internal override void StartReceivingData()
        {
            PSEtwLog.LogAnalyticInformational(PSEventId.WSManReceiveShellOutputEx,
                    PSOpcode.Receive, PSTask.None, PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                    RunspacePoolInstanceId.ToString(), powershellInstanceId.ToString());
            // We should call Receive only once.. WSMan will call the callback multiple times.
            shouldStartReceivingData = false;
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
                // recive callback
                receivedFromRemote = new WSManNativeApi.WSManShellAsync(new IntPtr(cmdContextId), cmdReceiveCallback);
                WSManNativeApi.WSManReceiveShellOutputEx(wsManShellOperationHandle,
                    wsManCmdOperationHandle, startInDisconnectedMode ? (int) WSManNativeApi.WSManShellFlag.WSMAN_FLAG_RECEIVE_DELAY_OUTPUT_STREAM : 0,
                   _sessnTm.WSManAPIData.OutputStreamSet,
                   receivedFromRemote, ref wsManRecieveOperationHandle);
            }
        }

        #endregion

        #region Dispose / Destructor pattern

        internal override void Dispose(bool isDisposing)
        {
            tracer.WriteLine("Disposing command with command context: {0} Operation Context: {1}", cmdContextId, wsManCmdOperationHandle);
            base.Dispose(isDisposing);

            // remove command context from cmd handles dictionary
            RemoveCmdTransportManager(cmdContextId);
            
            // unregister event handlers
            if (null != _sessnTm)
            {
                _sessnTm.RobustConnectionsInitiated -= HandleRobustConnectionsIntiated;
                _sessnTm.RobustConnectionsCompleted -= HandleRobusConnectionsCompleted;
            }

            if (null != closeCmdCompleted)
            {
                closeCmdCompleted.Dispose();
                closeCmdCompleted = null;
            }

            if (null != reconnectCmdCompleted)
            {
                reconnectCmdCompleted.Dispose();
                reconnectCmdCompleted = null;
            }

            wsManCmdOperationHandle = IntPtr.Zero;
        }

        #endregion

        #region Static Data / Methods

        // This dictionary maintains active command transport managers to be used from various
        // callbacks.
        private static Dictionary<long, WSManClientCommandTransportManager> CmdTMHandles =
            new Dictionary<long, WSManClientCommandTransportManager>();
        private static long CmdTMSeed;

        //Generate command transport manager unique id
        private static long GetNextCmdTMHandleId()
        {
            return System.Threading.Interlocked.Increment(ref CmdTMSeed);
        }

        // we need a synchronized add and remove so that multiple threads
        // update the data store concurrently
        private static void AddCmdTransportManager(long cmdTMId,
            WSManClientCommandTransportManager cmdTransportManager)
        {
            lock (CmdTMHandles)
            {
                CmdTMHandles.Add(cmdTMId, cmdTransportManager);
            }
        }

        private static void RemoveCmdTransportManager(long cmdTMId)
        {
            lock (CmdTMHandles)
            {
                CmdTMHandles.Remove(cmdTMId);
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
            lock (CmdTMHandles)
            {
                return CmdTMHandles.TryGetValue(cmdTMId, out cmdTransportManager);
            }
        }

        #endregion
    }
}
