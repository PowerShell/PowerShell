// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/*
 * Common file that contains interface definitions for generic server and client
 * transport managers.
 *
 */

using System.Management.Automation.Tracing;
using System.Text;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Management.Automation.Internal;
#if !UNIX
using System.Security.Principal;
#endif

// Don't expose the System.Management.Automation namespace here. This is transport layer
// and it shouldn't know anything about the engine.
using System.Management.Automation.Remoting.Client;
// TODO: this seems ugly...Remoting datatypes should be in remoting namespace
using System.Management.Automation.Runspaces.Internal;
using PSRemotingCryptoHelper = System.Management.Automation.Internal.PSRemotingCryptoHelper;
using RunspaceConnectionInfo = System.Management.Automation.Runspaces.RunspaceConnectionInfo;
using TypeTable = System.Management.Automation.Runspaces.TypeTable;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    #region TransportErrorOccuredEventArgs

    /// <summary>
    /// Transport method for error reporting.
    /// </summary>
    public enum TransportMethodEnum
    {
        /// <summary>
        /// CreateShellEx
        /// </summary>
        CreateShellEx = 0,

        /// <summary>
        /// RunShellCommandEx
        /// </summary>
        RunShellCommandEx = 1,

        /// <summary>
        /// SendShellInputEx
        /// </summary>
        SendShellInputEx = 2,

        /// <summary>
        /// ReceiveShellOutputEx
        /// </summary>
        ReceiveShellOutputEx = 3,

        /// <summary>
        /// CloseShellOperationEx
        /// </summary>
        CloseShellOperationEx = 4,

        /// <summary>
        /// CommandInputEx
        /// </summary>
        CommandInputEx = 5,

        /// <summary>
        /// ReceiveCommandOutputEx
        /// </summary>
        ReceiveCommandOutputEx = 6,

        /// <summary>
        /// DisconnectShellEx
        /// </summary>
        DisconnectShellEx = 7,

        /// <summary>
        /// ReconnectShellEx
        /// </summary>
        ReconnectShellEx = 8,
        
        /// <summary>
        /// ConnectShellEx
        /// </summary>
        ConnectShellEx = 9,

        /// <summary>
        /// ReconnectShellCommandEx
        /// </summary>
        ReconnectShellCommandEx = 10,

        /// <summary>
        /// ConnectShellCommandEx
        /// </summary>
        ConnectShellCommandEx = 11,

        /// <summary>
        /// Unknown
        /// </summary>
        Unknown = 12,
    }

    /// <summary>
    /// Event arguments passed to TransportErrorOccurred handlers.
    /// </summary>
    public sealed class TransportErrorOccuredEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="e">
        /// Error occurred.
        /// </param>
        /// <param name="m">
        /// The transport method that raised the error.
        /// </param>
        public TransportErrorOccuredEventArgs(
            PSRemotingTransportException e,
            TransportMethodEnum m)
        {
            Exception = e;
            ReportingTransportMethod = m;
        }

        /// <summary>
        /// Gets the error occurred.
        /// </summary>
        internal PSRemotingTransportException Exception { get; set; }

        /// <summary>
        /// Transport method that is reporting this error.
        /// </summary>
        internal TransportMethodEnum ReportingTransportMethod { get; }
    }

    #endregion

    #region ConnectionStatusEventArgs

    /// <summary>
    /// Robust Connection notifications.
    /// </summary>
    internal enum ConnectionStatus
    {
        NetworkFailureDetected = 1,
        ConnectionRetryAttempt = 2,
        ConnectionRetrySucceeded = 3,
        AutoDisconnectStarting = 4,
        AutoDisconnectSucceeded = 5,
        InternalErrorAbort = 6
    }

    /// <summary>
    /// ConnectionStatusEventArgs.
    /// </summary>
    internal sealed class ConnectionStatusEventArgs : EventArgs
    {
        internal ConnectionStatusEventArgs(ConnectionStatus notification)
        {
            Notification = notification;
        }

        internal ConnectionStatus Notification { get; }
    }

    #endregion

    #region CreateCompleteEventArgs

    /// <summary>
    /// CreateCompleteEventArgs.
    /// </summary>
    internal sealed class CreateCompleteEventArgs : EventArgs
    {
        internal RunspaceConnectionInfo ConnectionInfo { get; }

        internal CreateCompleteEventArgs(
            RunspaceConnectionInfo connectionInfo)
        {
            ConnectionInfo = connectionInfo;
        }
    }

    #endregion

    /// <summary>
    /// Contains implementation that is common to both client and server
    /// transport managers.
    /// </summary>
    public abstract class BaseTransportManager : IDisposable
    {
        #region tracer

        [TraceSourceAttribute("Transport", "Traces BaseWSManTransportManager")]
        private static readonly PSTraceSource s_baseTracer = PSTraceSource.GetTracer("Transport", "Traces BaseWSManTransportManager");

        #endregion

        #region Global Constants

        // KeepAlive: Server 4 minutes, Client 3 minutes
        // The server timeout value has to be bigger than the client timeout value.
        // This is due to the WinRM implementation on the Listener.
        // So We added a 1 minute network delay to count for this.
        internal const int ServerDefaultKeepAliveTimeoutMs = 4 * 60 * 1000; // milliseconds = 4 minutes
        internal const int ClientDefaultOperationTimeoutMs = 3 * 60 * 1000; // milliseconds = 3 minutes
        // Close timeout: to prevent unbounded close operation, we set a 1 minute bound.
        internal const int ClientCloseTimeoutMs = 60 * 1000;

        // This value instructs the server to use whatever setting it has for idle timeout.
        internal const int UseServerDefaultIdleTimeout = -1;
        internal const uint UseServerDefaultIdleTimeoutUInt = UInt32.MaxValue;

        // Minimum allowed idle timeout time is 60 seconds.
        internal const int MinimumIdleTimeout = 60 * 1000;

        internal const int DefaultFragmentSize = 32 << 10; // 32KB

        // Quota related consts and session variables.
        internal const int MaximumReceivedDataSize = 50 << 20; // 50MB
        internal const int MaximumReceivedObjectSize = 10 << 20; // 10MB

        // Session variables supporting powershell quotas.
        internal const string MAX_RECEIVED_DATA_PER_COMMAND_MB = "PSMaximumReceivedDataSizePerCommandMB";
        internal const string MAX_RECEIVED_OBJECT_SIZE_MB = "PSMaximumReceivedObjectSizeMB";

        #endregion

        #region Private Data

        // fragmentor used to fragment & defragment objects added to this collection.
        private readonly ReceiveDataCollection.OnDataAvailableCallback _onDataAvailableCallback;

        // crypto helper used for encrypting/decrypting
        // secure string

        #endregion

        #region EventHandlers

        internal event EventHandler<TransportErrorOccuredEventArgs> WSManTransportErrorOccured;
        /// <summary>
        /// Event that is raised when a remote object is available. The event is raised
        /// from a WSMan transport thread. Since this thread can hold on to a HTTP
        /// connection, the event handler should complete processing as fast as possible.
        /// Importantly the event handler should not generate any call that results in a
        /// user request like host.ReadLine().
        /// </summary>
        internal event EventHandler<RemoteDataEventArgs> DataReceived;

        /// <summary>
        /// Listen to this event to observe the PowerShell guid of the processed object.
        /// </summary>
        public event EventHandler PowerShellGuidObserver;

        #endregion

        #region Constructor

        internal BaseTransportManager(PSRemotingCryptoHelper cryptoHelper)
        {
            CryptoHelper = cryptoHelper;
            // create a common fragmentor used by this transport manager to send and receive data.
            // so type information is serialized only the first time an object of a particular type
            // is sent. only data is serialized for the rest of the objects of the same type.
            Fragmentor = new Fragmentor(DefaultFragmentSize, cryptoHelper);
            ReceivedDataCollection = new PriorityReceiveDataCollection(Fragmentor, (this is BaseClientTransportManager));
            _onDataAvailableCallback = new ReceiveDataCollection.OnDataAvailableCallback(OnDataAvailableCallback);
        }

        #endregion

        #region Helper Methods

        internal Fragmentor Fragmentor { get; set; }

        /// <summary>
        /// This is needed to deserialize objects coming from the network.
        /// This may be null..in which case type rehydration does not happen.
        /// At construction time we may not have typetable (server runspace
        /// is created only when a request from the client)..so this is
        /// a property on the base transport manager to allow for setting at
        /// a later time.
        /// </summary>
        internal TypeTable TypeTable
        {
            get { return Fragmentor.TypeTable; }

            set { Fragmentor.TypeTable = value; }
        }

        /// <summary>
        /// Uses the "OnDataAvailableCallback" to handle Deserialized objects.
        /// </summary>
        /// <param name="data">
        /// data to process
        /// </param>
        /// <param name="stream">
        /// priority stream this data belongs to
        /// </param>
        internal virtual void ProcessRawData(byte[] data, string stream)
        {
            try
            {
                ProcessRawData(data, stream, _onDataAvailableCallback);
            }
            catch (Exception exception)
            {
                // This will get executed on a thread pool thread..
                // so we need to protect that thread, hence catching
                // all exceptions
                s_baseTracer.WriteLine("Exception processing data. {0}", exception.Message);

                PSRemotingTransportException e = new PSRemotingTransportException(exception.Message, exception);
                TransportErrorOccuredEventArgs eventargs =
                    new TransportErrorOccuredEventArgs(e, TransportMethodEnum.ReceiveShellOutputEx);
                RaiseErrorHandler(eventargs);
                return;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="data">
        /// data to process
        /// </param>
        /// <param name="stream">
        /// priority stream this data belongs to
        /// </param>
        /// <param name="dataAvailableCallback">
        /// used by the caller to supply a callback to handle deserialized object.
        /// </param>
        /// <exception cref="Exception">
        /// Since dataAvailableCallback is called in this method, and the handler
        /// may be handled by 3rd party code (eventually),this may throw any exception.
        /// </exception>
        internal void ProcessRawData(byte[] data,
            string stream,
            ReceiveDataCollection.OnDataAvailableCallback dataAvailableCallback)
        {
            Dbg.Assert(data != null, "Cannot process null data");

            s_baseTracer.WriteLine("Processing incoming data for stream {0}.", stream);

            bool shouldProcess = false;

            DataPriorityType dataPriority = DataPriorityType.Default;
            if (stream.Equals(WSManNativeApi.WSMAN_STREAM_ID_STDIN, StringComparison.OrdinalIgnoreCase) ||
                stream.Equals(WSManNativeApi.WSMAN_STREAM_ID_STDOUT, StringComparison.OrdinalIgnoreCase))
            {
                shouldProcess = true;
            }
            else if (stream.Equals(WSManNativeApi.WSMAN_STREAM_ID_PROMPTRESPONSE, StringComparison.OrdinalIgnoreCase))
            {
                dataPriority = DataPriorityType.PromptResponse;
                shouldProcess = true;
            }

            if (!shouldProcess)
            {
                // we dont support this stream..so ignore the data
                Dbg.Assert(false, string.Format(
                    CultureInfo.InvariantCulture,
                    "Data should be from one of the streams : {0} or {1} or {2}",
                    WSManNativeApi.WSMAN_STREAM_ID_STDIN,
                    WSManNativeApi.WSMAN_STREAM_ID_STDOUT,
                    WSManNativeApi.WSMAN_STREAM_ID_PROMPTRESPONSE));
                s_baseTracer.WriteLine("{0} is not a valid stream", stream);
            }
            // process data
            ReceivedDataCollection.ProcessRawData(data, dataPriority, dataAvailableCallback);
        }

        /// <summary>
        /// </summary>
        /// <param name="remoteObject"></param>
        /// <exception cref="Exception">
        /// The handler may be handled by 3rd party code (eventually),
        /// this may throw any exception.
        /// </exception>
        internal void OnDataAvailableCallback(RemoteDataObject<PSObject> remoteObject)
        {
            // log the data to crimson logs
            PSEtwLog.LogAnalyticInformational(PSEventId.TransportReceivedObject, PSOpcode.Open,
                                                  PSTask.None,
                                                  PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                                                  remoteObject.RunspacePoolId.ToString(),
                                                  remoteObject.PowerShellId.ToString(),
                                                  (UInt32)(remoteObject.Destination),
                                                  (UInt32)(remoteObject.DataType),
                                                  (UInt32)(remoteObject.TargetInterface));

            // This might throw exceptions which the caller handles.
            PowerShellGuidObserver.SafeInvoke(remoteObject.PowerShellId, EventArgs.Empty);

            RemoteDataEventArgs eventArgs = new RemoteDataEventArgs(remoteObject);
            DataReceived.SafeInvoke(this, eventArgs);
        }

        /// <summary>
        /// Copy the DataReceived event handlers to the supplied transport Manager.
        /// </summary>
        /// <param name="transportManager"></param>
        public void MigrateDataReadyEventHandlers(BaseTransportManager transportManager)
        {
            foreach (Delegate handler in transportManager.DataReceived.GetInvocationList())
            {
                DataReceived += (EventHandler<RemoteDataEventArgs>)handler;
            }
        }

        /// <summary>
        /// Raise the error handlers.
        /// </summary>
        /// <param name="eventArgs"></param>
        public virtual void RaiseErrorHandler(TransportErrorOccuredEventArgs eventArgs)
        {
            WSManTransportErrorOccured.SafeInvoke(this, eventArgs);
        }

        /// <summary>
        /// Crypto handler to be used for encrypting/decrypting
        /// secure strings.
        /// </summary>
        internal PSRemotingCryptoHelper CryptoHelper { get; set; }

        /// <summary>
        /// A data buffer used to store data received from remote machine.
        /// </summary>
        internal PriorityReceiveDataCollection ReceivedDataCollection { get; }

        #endregion

        #region IDisposable implementation

        /// <summary>
        /// Dispose the transport and release resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            // if already disposing..no need to let finalizer thread
            // put resources to clean this object.
            System.GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                ReceivedDataCollection.Dispose();
            }
        }

        #endregion
    }
}

namespace System.Management.Automation.Remoting.Client
{
    /// <summary>
    /// Remoting base client transport manager.
    /// </summary>
    public abstract class BaseClientTransportManager : BaseTransportManager, IDisposable
    {
        #region Tracer
        [TraceSourceAttribute("ClientTransport", "Traces ClientTransportManager")]
        internal static PSTraceSource tracer = PSTraceSource.GetTracer("ClientTransport", "Traces ClientTransportManager");
        #endregion

        #region Data

        internal bool isClosed;
        internal object syncObject = new object();
        internal PrioritySendDataCollection dataToBeSent;
        // used to handle callbacks from the server..these are used to synchronize received callbacks
        private readonly Queue<CallbackNotificationInformation> _callbackNotificationQueue;
        private readonly ReceiveDataCollection.OnDataAvailableCallback _onDataAvailableCallback;
        private bool _isServicingCallbacks;
        private bool _suspendQueueServicing;
        private bool _isDebuggerSuspend;

        // this is used log crimson messages.

        // keeps track of whether a receive request has been placed on transport
        internal bool receiveDataInitiated;

        #endregion

        #region Constructors

        internal BaseClientTransportManager(Guid runspaceId, PSRemotingCryptoHelper cryptoHelper)
            : base(cryptoHelper)
        {
            RunspacePoolInstanceId = runspaceId;
            dataToBeSent = new PrioritySendDataCollection();
            _onDataAvailableCallback = new ReceiveDataCollection.OnDataAvailableCallback(OnDataAvailableHandler);
            _callbackNotificationQueue = new Queue<CallbackNotificationInformation>();
        }

        #endregion

        #region Events
        /// <summary>
        /// Event that is raised when a create operation on transport has been successfully completed
        /// The event is raised
        /// from a WSMan transport thread. Since this thread can hold on to a HTTP
        /// connection, the event handler should complete processing as fast as possible.
        /// Importantly the event handler should not generate any call that results in a
        /// user request like host.ReadLine().
        ///
        /// Errors (occurred during connection attempt) are reported through WSManTransportErrorOccured
        /// event.
        /// </summary>
        internal event EventHandler<CreateCompleteEventArgs> CreateCompleted;
        /// <summary>
        /// Event that is raised when a remote connection is successfully closed. The event is raised
        /// from a WSMan transport thread. Since this thread can hold on to a HTTP
        /// connection, the event handler should complete processing as fast as possible.
        /// Importantly the event handler should not generate any call that results in a
        /// user request like host.ReadLine().
        ///
        /// Errors (occurred during connection attempt) are reported through WSManTransportErrorOccured
        /// event.
        /// </summary>
        /// <remarks>
        /// The eventhandler should make sure not to throw any exceptions.
        /// </remarks>
        internal event EventHandler<EventArgs> CloseCompleted;

        /// <summary>
        /// Indicated successful completion of a connect operation on transport
        ///
        /// Errors are reported through WSManTransportErrorOccured
        /// event.
        /// </summary>
        internal event EventHandler<EventArgs> ConnectCompleted;

        /// <summary>
        /// Indicated successful completion of a disconnect operation on transport
        ///
        /// Errors are reported through WSManTransportErrorOccured
        /// event.
        /// </summary>
        internal event EventHandler<EventArgs> DisconnectCompleted;

        /// <summary>
        /// Indicated successful completion of a reconnect operation on transport
        ///
        /// Errors are reported through WSManTransportErrorOccured
        /// event.
        /// </summary>
        internal event EventHandler<EventArgs> ReconnectCompleted;

        /// <summary>
        /// Indicates that the transport/command is ready for a disconnect operation.
        ///
        /// Errors are reported through WSManTransportErrorOccured event.
        /// </summary>
        internal event EventHandler<EventArgs> ReadyForDisconnect;

        /// <summary>
        /// Event to pass Robust Connection notifications to client.
        /// </summary>
        internal event EventHandler<ConnectionStatusEventArgs> RobustConnectionNotification;

        /// <summary>
        /// Indicates successful processing of a delay stream request on a receive operation
        ///
        /// this event is useful when PS wants to invoke a pipeline in disconnected mode.
        /// </summary>
        internal event EventHandler<EventArgs> DelayStreamRequestProcessed;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the data collection which is used by this transport manager to send
        /// data to the server.
        /// </summary>
        internal PrioritySendDataCollection DataToBeSentCollection
        {
            get { return dataToBeSent; }
        }

        /// <summary>
        /// Used to log crimson messages.
        /// </summary>
        internal Guid RunspacePoolInstanceId { get; }

        /// <summary>
        /// Raise the Connect completed handler.
        /// </summary>
        internal void RaiseCreateCompleted(CreateCompleteEventArgs eventArgs)
        {
            CreateCompleted.SafeInvoke(this, eventArgs);
        }

        internal void RaiseConnectCompleted()
        {
            ConnectCompleted.SafeInvoke(this, EventArgs.Empty);
        }

        internal void RaiseDisconnectCompleted()
        {
            DisconnectCompleted.SafeInvoke(this, EventArgs.Empty);
        }

        internal void RaiseReconnectCompleted()
        {
            ReconnectCompleted.SafeInvoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raise the close completed handler.
        /// </summary>
        internal void RaiseCloseCompleted()
        {
            CloseCompleted.SafeInvoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raise the ReadyForDisconnect event.
        /// </summary>
        internal void RaiseReadyForDisconnect()
        {
            ReadyForDisconnect.SafeInvoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Queue the robust connection notification event.
        /// </summary>
        /// <param name="flags">Determines what kind of notification.</param>
        internal void QueueRobustConnectionNotification(int flags)
        {
            ConnectionStatusEventArgs args = null;
            switch (flags)
            {
                case (int)WSManNativeApi.WSManCallbackFlags.WSMAN_FLAG_CALLBACK_NETWORK_FAILURE_DETECTED:
                    args = new ConnectionStatusEventArgs(ConnectionStatus.NetworkFailureDetected);
                    break;

                case (int)WSManNativeApi.WSManCallbackFlags.WSMAN_FLAG_CALLBACK_RETRYING_AFTER_NETWORK_FAILURE:
                    args = new ConnectionStatusEventArgs(ConnectionStatus.ConnectionRetryAttempt);
                    break;

                case (int)WSManNativeApi.WSManCallbackFlags.WSMAN_FLAG_CALLBACK_RECONNECTED_AFTER_NETWORK_FAILURE:
                    args = new ConnectionStatusEventArgs(ConnectionStatus.ConnectionRetrySucceeded);
                    break;

                case (int)WSManNativeApi.WSManCallbackFlags.WSMAN_FLAG_CALLBACK_SHELL_AUTODISCONNECTING:
                    args = new ConnectionStatusEventArgs(ConnectionStatus.AutoDisconnectStarting);
                    break;

                case (int)WSManNativeApi.WSManCallbackFlags.WSMAN_FLAG_CALLBACK_SHELL_AUTODISCONNECTED:
                    args = new ConnectionStatusEventArgs(ConnectionStatus.AutoDisconnectSucceeded);
                    break;

                case (int)WSManNativeApi.WSManCallbackFlags.WSMAN_FLAG_CALLBACK_RETRY_ABORTED_DUE_TO_INTERNAL_ERROR:
                    args = new ConnectionStatusEventArgs(ConnectionStatus.InternalErrorAbort);
                    break;
            }

            // Queue worker item to raise the event so that all robust connection
            // events are raised in the same order as received.
            EnqueueAndStartProcessingThread(null, null, args);
        }

        /// <summary>
        /// Raise the Robust Connection notification event.
        /// </summary>
        /// <param name="args">ConnectionStatusEventArgs.</param>
        internal void RaiseRobustConnectionNotification(ConnectionStatusEventArgs args)
        {
            RobustConnectionNotification.SafeInvoke(this, args);
        }

        internal void RaiseDelayStreamProcessedEvent()
        {
            DelayStreamRequestProcessed.SafeInvoke(this, EventArgs.Empty);
        }

        #endregion

        #region Received Data Processing Thread

        internal override void ProcessRawData(byte[] data, string stream)
        {
            if (isClosed)
            {
                return;
            }

            try
            {
                base.ProcessRawData(data, stream, _onDataAvailableCallback);
            }
            catch (PSRemotingTransportException pte)
            {
                // PSRemotingTransportException need not be wrapped in another PSRemotingTransportException.
                tracer.WriteLine("Exception processing data. {0}", pte.Message);
                TransportErrorOccuredEventArgs eventargs = new TransportErrorOccuredEventArgs(pte,
                                    TransportMethodEnum.ReceiveShellOutputEx);
                EnqueueAndStartProcessingThread(null, eventargs, null);

                return;
            }
            catch (Exception exception)
            {
                // Enqueue an Exception to process in a thread-pool thread. Processing
                // Exception in a thread pool thread is important as calling
                // WSManCloseShell/Command from a Receive callback results in a deadlock.
                tracer.WriteLine("Exception processing data. {0}", exception.Message);

                PSRemotingTransportException e = new PSRemotingTransportException(exception.Message);
                TransportErrorOccuredEventArgs eventargs = new TransportErrorOccuredEventArgs(e,
                                    TransportMethodEnum.ReceiveShellOutputEx);
                EnqueueAndStartProcessingThread(null, eventargs, null);
                return;
            }
        }

        private void OnDataAvailableHandler(RemoteDataObject<PSObject> remoteObject)
        {
            EnqueueAndStartProcessingThread(remoteObject, null, null);
        }

        /// <summary>
        /// Enqueue a deserialized object or an Exception to process in a thread pool
        /// thread. Processing Exception in a thread pool thread is important as calling
        /// WSManCloseShell/Command from a Receive callback results in a deadlock.
        /// </summary>
        /// <param name="remoteObject">
        /// Deserialized Object to process in a thread-pool thread. This should be null
        /// when <paramref name="transportException"/> is specified.
        /// </param>
        /// <param name="privateData">
        /// Data that is neither RemoteObject or Exception. This is used by Client Command
        /// Transport manager to raise SignalCompleted callback.
        /// </param>
        /// <param name="transportErrorArgs">
        /// Error containing transport exception.
        /// </param>
        internal void EnqueueAndStartProcessingThread(RemoteDataObject<PSObject> remoteObject,
            TransportErrorOccuredEventArgs transportErrorArgs,
            object privateData)
        {
            if (isClosed)
            {
                return;
            }

            lock (_callbackNotificationQueue)
            {
                if ((remoteObject != null) || (transportErrorArgs != null) || (privateData != null))
                {
                    CallbackNotificationInformation rcvdDataInfo = new CallbackNotificationInformation();
                    rcvdDataInfo.remoteObject = remoteObject;
                    rcvdDataInfo.transportError = transportErrorArgs;
                    rcvdDataInfo.privateData = privateData;

                    if (remoteObject != null && (remoteObject.DataType == RemotingDataType.PublicKey ||
                                                 remoteObject.DataType == RemotingDataType.EncryptedSessionKey ||
                                                 remoteObject.DataType == RemotingDataType.PublicKeyRequest))
                    {
                        CryptoHelper.Session.BaseSessionDataStructureHandler.RaiseKeyExchangeMessageReceived(remoteObject);
                    }
                    else
                    {
                        _callbackNotificationQueue.Enqueue(rcvdDataInfo);
                    }
                }

                if (_suspendQueueServicing && _isDebuggerSuspend)
                {
                    // Remove debugger queue suspension if remoteObject requires user response.
                    _suspendQueueServicing = !CheckForInteractiveHostCall(remoteObject);
                }

                if (_isServicingCallbacks || _suspendQueueServicing)
                {
                    // a thread pool thread is already processing callbacks or
                    // the queue processing is suspended.
                    return;
                }

                if (_callbackNotificationQueue.Count > 0)
                {
                    _isServicingCallbacks = true;

                    // Start a thread pool thread to process callbacks.
#if !UNIX
                    WindowsIdentity identityToImpersonate;
                    Utils.TryGetWindowsImpersonatedIdentity(out identityToImpersonate);

                    Utils.QueueWorkItemWithImpersonation(
                        identityToImpersonate,
                        new WaitCallback(ServicePendingCallbacks),
                        null);
#else
                    ThreadPool.QueueUserWorkItem(new WaitCallback(ServicePendingCallbacks));
#endif
                }
            }
        }

        /// <summary>
        /// Helper method to check RemoteDataObject for a host call requiring user
        /// interaction.
        /// </summary>
        /// <param name="remoteObject">Remote data object.</param>
        /// <returns>True if remote data object requires a user response.</returns>
        private static bool CheckForInteractiveHostCall(RemoteDataObject<PSObject> remoteObject)
        {
            bool interactiveHostCall = false;

            if ((remoteObject != null) &&
                (remoteObject.DataType == RemotingDataType.RemoteHostCallUsingPowerShellHost))
            {
                RemoteHostMethodId methodId = 0;

                try
                {
                    methodId = RemotingDecoder.GetPropertyValue<RemoteHostMethodId>(remoteObject.Data, RemoteDataNameStrings.MethodId);
                }
                catch (PSArgumentNullException) { }
                catch (PSRemotingDataStructureException) { }

                // If new remote host call methods are added then we need to evaluate if they are interactive.
                Dbg.Assert(methodId <= RemoteHostMethodId.PromptForChoiceMultipleSelection, "A new remote host method Id was added.  Update switch statement as needed.");

                switch (methodId)
                {
                    case RemoteHostMethodId.Prompt:
                    case RemoteHostMethodId.PromptForChoice:
                    case RemoteHostMethodId.PromptForChoiceMultipleSelection:
                    case RemoteHostMethodId.PromptForCredential1:
                    case RemoteHostMethodId.PromptForCredential2:
                    case RemoteHostMethodId.ReadKey:
                    case RemoteHostMethodId.ReadLine:
                    case RemoteHostMethodId.ReadLineAsSecureString:
                        interactiveHostCall = true;
                        break;
                }
            }

            return interactiveHostCall;
        }

        internal void ServicePendingCallbacks(object objectToProcess)
        {
            tracer.WriteLine("ServicePendingCallbacks thread is starting");
            PSEtwLog.ReplaceActivityIdForCurrentThread(RunspacePoolInstanceId,
                PSEventId.OperationalTransferEventRunspacePool,
                PSEventId.AnalyticTransferEventRunspacePool,
                PSKeyword.Transport,
                PSTask.None);

            try
            {
                while (true)
                {
                    // if the transport manager is closed return.
                    if (isClosed)
                    {
                        return;
                    }

                    CallbackNotificationInformation rcvdDataInfo = null;
                    lock (_callbackNotificationQueue)
                    {
                        // If queue is empty or if queue servicing is suspended
                        // then break out of loop.
                        if (_callbackNotificationQueue.Count == 0 || _suspendQueueServicing)
                        {
                            break;
                        }

                        rcvdDataInfo = _callbackNotificationQueue.Dequeue();
                    }
                    // Handle callback.
                    if (rcvdDataInfo != null)
                    {
                        // Handling transport exception in thread-pool thread
                        if (rcvdDataInfo.transportError != null)
                        {
                            RaiseErrorHandler(rcvdDataInfo.transportError);
                            break;
                        }
                        else if (rcvdDataInfo.privateData != null)
                        {
                            ProcessPrivateData(rcvdDataInfo.privateData);
                        }
                        else
                        {
                            base.OnDataAvailableCallback(rcvdDataInfo.remoteObject);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                // This will get executed on a thread pool thread..
                // so we need to protect that thread, hence catching
                // all exceptions
                tracer.WriteLine("Exception processing data. {0}", exception.Message);

                PSRemotingTransportException e = new PSRemotingTransportException(exception.Message, exception);
                TransportErrorOccuredEventArgs eventargs =
                    new TransportErrorOccuredEventArgs(e, TransportMethodEnum.ReceiveShellOutputEx);
                RaiseErrorHandler(eventargs);
                return;
            }
            finally
            {
                lock (_callbackNotificationQueue)
                {
                    tracer.WriteLine("ServicePendingCallbacks thread is exiting");
                    _isServicingCallbacks = false;
                    // check if any new runspace request has arrived..
                    EnqueueAndStartProcessingThread(null, null, null);
                }
            }
        }

        internal bool IsServicing
        {
            get
            {
                lock (_callbackNotificationQueue)
                {
                    return _isServicingCallbacks;
                }
            }
        }

        internal void SuspendQueue(bool debuggerSuspend = false)
        {
            lock (_callbackNotificationQueue)
            {
                _isDebuggerSuspend = debuggerSuspend;
                _suspendQueueServicing = true;
            }
        }

        internal void ResumeQueue()
        {
            lock (_callbackNotificationQueue)
            {
                _isDebuggerSuspend = false;

                if (_suspendQueueServicing)
                {
                    _suspendQueueServicing = false;

                    // Process any items in queue.
                    EnqueueAndStartProcessingThread(null, null, null);
                }
            }
        }

        /// <summary>
        /// Used by ServicePendingCallbacks to give the control to derived classes for
        /// processing data that the base class does not understand.
        /// </summary>
        /// <param name="privateData">
        /// Derived class specific data to process. For command transport manager this
        /// should be a boolean.
        /// </param>
        internal virtual void ProcessPrivateData(object privateData)
        {
        }

        internal sealed class CallbackNotificationInformation
        {
            // only one of the following 2 should be present..
            // anyway transportException takes precedence over remoteObject.
            internal RemoteDataObject<PSObject> remoteObject;
            internal TransportErrorOccuredEventArgs transportError;
            // Used by ServicePendingCallbacks to give the control to derived classes for
            // processing data that the base class does not understand.
            internal object privateData;
        }

        #endregion

        #region Abstract / Virtual methods

        /// <summary>
        /// Create the transport manager and initiate connection.
        /// </summary>
        public abstract void CreateAsync();

        internal abstract void ConnectAsync();

        /// <summary>
        /// The caller should make sure the call is synchronized.
        /// </summary>
        public virtual void CloseAsync()
        {
            // Clear the send collection
            dataToBeSent.Clear();
        }

        internal virtual void StartReceivingData()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Method to have transport prepare for a disconnect operation.
        /// </summary>
        internal virtual void PrepareForDisconnect()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Method to resume post disconnect operations.
        /// </summary>
        internal virtual void PrepareForConnect()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Clean up

        /// <summary>
        /// Finalizes an instance of the <see cref="BaseClientTransportManager"/> class.
        /// </summary>
        ~BaseClientTransportManager()
        {
            if (isClosed)
            {
                Dispose(false);
            }
            else
            {
                // wait for the close to be completed and then release the resources.
                this.CloseCompleted += (object source, EventArgs args) => Dispose(false);

                try
                {
                    // looks like Dispose is not called for this transport manager
                    // try closing the transport manager.
                    CloseAsync();
                }
                catch (ObjectDisposedException)
                {
                    // intentionally blank
                }
            }
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        protected override void Dispose(bool isDisposing)
        {
            // clear event handlers
            this.CreateCompleted = null;
            this.CloseCompleted = null;
            this.ConnectCompleted = null;
            this.DisconnectCompleted = null;
            this.ReconnectCompleted = null;

            // let base dispose its resources.
            base.Dispose(isDisposing);
        }

        #endregion
    }

    /// <summary>
    /// Remoting base client session transport manager.
    /// </summary>
    public abstract class BaseClientSessionTransportManager : BaseClientTransportManager, IDisposable
    {
        #region Constructors

        internal BaseClientSessionTransportManager(Guid runspaceId, PSRemotingCryptoHelper cryptoHelper)
            : base(runspaceId, cryptoHelper)
        {
        }

        #endregion

        #region Abstract / Virtual Methods

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
        internal virtual BaseClientCommandTransportManager CreateClientCommandTransportManager(RunspaceConnectionInfo connectionInfo,
                    ClientRemotePowerShell cmd, bool noInput)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// RunspacePool data structure handler uses this method to remove association of a command transport manager
        /// from a session transport manager.
        /// </summary>
        /// <param name="powerShellCmdId"></param>
        internal virtual void RemoveCommandTransportManager(Guid powerShellCmdId)
        {
        }

        /// <summary>
        /// Temporarily disconnect an active session.
        /// </summary>
        internal virtual void DisconnectAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reconnect back a temporarily disconnected session.
        /// </summary>
        internal virtual void ReconnectAsync()
        {
            throw new NotImplementedException();
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
        internal virtual void Redirect(Uri newUri, RunspaceConnectionInfo connectionInfo)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Used by callers to prepare the session transportmanager for a URI redirection.
        /// This must be called only after Create callback (or Error form create) is received.
        /// Callers must catch the close completed event and call Redirect to perform the redirection.
        /// </summary>
        internal virtual void PrepareForRedirection()
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    internal abstract class BaseClientCommandTransportManager : BaseClientTransportManager, IDisposable
    {
        #region Private / Protected Data

        // pipeline in the form cmd1 | cmd2.. this is used by authz module for early validation.
        protected StringBuilder cmdText;
        protected SerializedDataStream serializedPipeline;
        protected Guid powershellInstanceId;

        protected Guid PowershellInstanceId
        {
            get { return powershellInstanceId; }
        }

        #endregion

        internal bool startInDisconnectedMode = false;

        #region Constructors

        protected BaseClientCommandTransportManager(ClientRemotePowerShell shell,
            PSRemotingCryptoHelper cryptoHelper,
            BaseClientSessionTransportManager sessnTM) : base(sessnTM.RunspacePoolInstanceId, cryptoHelper)
        {
            Fragmentor.FragmentSize = sessnTM.Fragmentor.FragmentSize;
            Fragmentor.TypeTable = sessnTM.Fragmentor.TypeTable;
            dataToBeSent.Fragmentor = base.Fragmentor;
            // used for Crimson logging.
            powershellInstanceId = shell.PowerShell.InstanceId;

            cmdText = new StringBuilder();
            foreach (System.Management.Automation.Runspaces.Command cmd in shell.PowerShell.Commands.Commands)
            {
                cmdText.Append(cmd.CommandText);
                cmdText.Append(" | ");
            }

            cmdText.Remove(cmdText.Length - 3, 3); // remove ending " | "

            RemoteDataObject message;
            if (shell.PowerShell.IsGetCommandMetadataSpecialPipeline)
            {
                message = RemotingEncoder.GenerateGetCommandMetadata(shell);
            }
            else
            {
                message = RemotingEncoder.GenerateCreatePowerShell(shell);
            }

            serializedPipeline = new SerializedDataStream(base.Fragmentor.FragmentSize);
            Fragmentor.Fragment<object>(message, serializedPipeline);
        }

        #endregion

        #region Events

        internal event EventHandler<EventArgs> SignalCompleted;

        internal void RaiseSignalCompleted()
        {
            SignalCompleted.SafeInvoke(this, EventArgs.Empty);
        }

        #endregion

        #region Overrides

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (isDisposing)
            {
                // dispose serialized pipeline
                serializedPipeline.Dispose();
            }
        }

        #endregion

        #region Abstract / Virtual Methods

        /// <summary>
        /// Reconnects a previously disconnected commandTM. Implemented by WSMan transport
        /// Note that there is not explicit disconnect on commandTM. It is implicity disconnected
        /// when disconnect is called on sessionTM . The TM's also dont maintain specific connection state
        /// This is done by DSHandlers.
        /// </summary>
        internal virtual void ReconnectAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Used by powershell/pipeline to send a stop message to the server command.
        /// </summary>
        internal virtual void SendStopSignal()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

namespace System.Management.Automation.Remoting.Server
{
    /// <summary>
    /// This represents an abstraction for server transport manager.
    /// </summary>
    internal abstract class AbstractServerTransportManager : BaseTransportManager
    {
        #region Private Data

        private readonly object _syncObject = new object();
        // used to listen to data available events from serialized datastream.
        private readonly SerializedDataStream.OnDataAvailableCallback _onDataAvailable;
        // the following variable are used by onDataAvailableCallback.
        private bool _shouldFlushData;
        private bool _reportAsPending;
        private Guid _runspacePoolInstanceId;
        private Guid _powerShellInstanceId;
        private RemotingDataType _dataType;
        private RemotingTargetInterface _targetInterface;
        // End: the following variable are used by onDataAvailableCallback.
        private Queue<Tuple<RemoteDataObject, bool, bool>> _dataToBeSentQueue;
        private bool _isSerializing;
        #endregion

        #region Constructor

        protected AbstractServerTransportManager(int fragmentSize, PSRemotingCryptoHelper cryptoHelper)
            : base(cryptoHelper)
        {
            base.Fragmentor.FragmentSize = fragmentSize;
            _onDataAvailable = new SerializedDataStream.OnDataAvailableCallback(OnDataAvailable);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Sends an object from the server end. The object is fragmented and each fragment is sent
        /// separately. The call blocks until all the fragments are sent to the client. If there
        /// is a failure sending any of the fragments WSManTransportErrorOccured event is raised.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="flush">
        /// true to immediately send data to client.
        /// </param>
        /// <param name="reportPending">
        /// reported as true when host message requests are sent to client
        /// </param>
        internal void SendDataToClient<T>(RemoteDataObject<T> data, bool flush, bool reportPending = false)
        {
            // make sure only one data packet can be sent in its entirety at any
            // given point of time using this transport manager.
            lock (_syncObject)
            {
                // Win8: 491001 Icm -computername $env:COMPUTERNAME {Get-NetIpInterface} fails with Unexpected ObjectId
                // This is because the output object has some extended script properties. Getter of one of the
                // script properties is calling write-progress. Write-Progress in turn results in a progress record
                // being sent to client. This breaks the fragmentation rule when the original object (without progress)
                // does not fit in fragmented packet.
                // ******************** repro using powershell *********************************
                //  icm . {
                //        $a = new-object psobject
                //        $a.pstypenames.Insert(0, "Microsoft.PowerShell.Test.Bug491001")
                //        Update-TypeData -TypeName Microsoft.PowerShell.Test.Bug491001 -MemberType ScriptProperty -MemberName name -Value {( 1..50kb | % { get-random -min 97 -max 122 | % { [char]$psitem } }) -join ""}
                //        Update-TypeData -TypeName Microsoft.PowerShell.Test.Bug491001 -MemberType ScriptProperty -MemberName Verbose -Value {write-progress "blah" -Completed; "Some verbose data"}
                //        Update-TypeData -TypeName Microsoft.PowerShell.Test.Bug491001 -MemberType ScriptProperty -MemberName zname -Value {( 1..10kb | % { get-random -min 97 -max 122 | % { [char]$psitem } }) -join ""}
                //        $a
                //   }
                // 1. The value of "name" property is huge 50kb and cannot fit in one fragment (with fragment size 32kb)
                // 2. The value of "Verbose" is actually writing a progress record
                // 3. The value of "zname" property is also huge
                // 4. Notice the ascending order of property names. This is because serializer serializes properties in sort order
                // ******************** End of repro ******************************************
                // To fix the issue, I am creating a Queue and enqueuing the data objects if we are already serializing another data object
                // Notice this is in lock() above. An object is serialized in its entirety in one thread. So, in my example above "name",
                // "verbose","zname" properties are serialized in one thread. So lock() essentially protects from serializing other objects
                // and not this (parent)object.
                RemoteDataObject dataToBeSent = RemoteDataObject.CreateFrom(data.Destination, data.DataType,
                                                                            data.RunspacePoolId, data.PowerShellId,
                                                                            data.Data);
                if (_isSerializing)
                {
                    _dataToBeSentQueue ??= new Queue<Tuple<RemoteDataObject, bool, bool>>();

                    _dataToBeSentQueue.Enqueue(new Tuple<RemoteDataObject, bool, bool>(dataToBeSent, flush, reportPending));
                    return;
                }

                _isSerializing = true;
                try
                {
                    do
                    {
                        // tell stream to notify us whenever a fragment is available instead of writing data
                        // into internal buffers. This will save write + read + dispose.)
                        using (SerializedDataStream serializedData =
                            new SerializedDataStream(Fragmentor.FragmentSize, _onDataAvailable))
                        {
                            _shouldFlushData = flush;
                            _reportAsPending = reportPending;
                            _runspacePoolInstanceId = dataToBeSent.RunspacePoolId;
                            _powerShellInstanceId = dataToBeSent.PowerShellId;
                            _dataType = dataToBeSent.DataType;
                            _targetInterface = dataToBeSent.TargetInterface;
                            Fragmentor.Fragment<object>(dataToBeSent, serializedData);
                        }

                        if ((_dataToBeSentQueue != null) && (_dataToBeSentQueue.Count > 0))
                        {
                            Tuple<RemoteDataObject, bool, bool> dataToBeSentQueueItem = _dataToBeSentQueue.Dequeue();
                            dataToBeSent = dataToBeSentQueueItem.Item1;
                            flush = dataToBeSentQueueItem.Item2;
                            reportPending = dataToBeSentQueueItem.Item3;
                        }
                        else
                        {
                            dataToBeSent = null;
                        }
                    } while (dataToBeSent != null);
                }
                finally
                {
                    _isSerializing = false;
                }
            }
        }

        private void OnDataAvailable(byte[] dataToSend, bool isEndFragment)
        {
            Dbg.Assert(dataToSend != null, "ServerTransportManager cannot send null fragment");
            // log to crimson log.
            PSEtwLog.LogAnalyticInformational(PSEventId.ServerSendData, PSOpcode.Send, PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                _runspacePoolInstanceId.ToString(),
                _powerShellInstanceId.ToString(),
                dataToSend.Length.ToString(CultureInfo.InvariantCulture),
                (UInt32)_dataType,
                (UInt32)_targetInterface);

            SendDataToClient(dataToSend, isEndFragment && _shouldFlushData, _reportAsPending, isEndFragment);
        }

        /// <summary>
        /// Sends an object to the server end. The object is fragmented and each fragment is sent
        /// separately. The call blocks until all the fragments are sent to the client. If there
        /// is a failure sending any of the fragments WSManTransportErrorOccured event is raised.
        /// </summary>
        /// <param name="psObjectData"></param>
        /// <param name="flush">
        /// true to immediately send data to client.
        /// </param>
        /// <param name="reportAsPending">
        /// reported as true when sending host message requests are reported true
        /// </param>
        internal void SendDataToClient(RemoteDataObject psObjectData, bool flush, bool reportAsPending = false)
        {
            SendDataToClient<object>((RemoteDataObject<object>)(psObjectData), flush, reportAsPending);
        }

        /// <summary>
        /// Reports error from a thread pool thread. Thread Pool is used in order to
        /// not block Pipeline closing. This method is generally called when the
        /// TransportManager fails to Send data (SendDataToClient). Pipeline Execution
        /// Thread directly calls SendDataToClient method from its execution thread,
        /// so we cannot call Stop from the same thread (as it will result in a deadlock)
        /// </summary>
        internal void ReportError(int errorCode, string methodName)
        {
            string messageResource = RemotingErrorIdStrings.GeneralError;
            string errorMessage = string.Format(CultureInfo.InvariantCulture,
                messageResource, new object[] { errorCode, methodName });

            PSRemotingTransportException e = new PSRemotingTransportException(errorMessage);
            e.ErrorCode = errorCode;

            // Use thread-pool thread to raise the error handler..see explanation
            // in the method summary
            ThreadPool.QueueUserWorkItem(new WaitCallback(
                (object state) =>
                {
                    TransportErrorOccuredEventArgs eventArgs = new TransportErrorOccuredEventArgs(e,
                        TransportMethodEnum.Unknown);
                    RaiseErrorHandler(eventArgs);
                }));
        }

        /// <summary>
        /// Raises the closing event.
        /// </summary>
        internal void RaiseClosingEvent()
        {
            Closing.SafeInvoke(this, EventArgs.Empty);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Event that is raised when this transport manager is closing.
        /// </summary>
        internal event EventHandler Closing;

        #endregion

        #region Abstract interfaces

        /// <summary>
        /// </summary>
        /// <param name="data"></param>
        /// <param name="flush">
        /// flush data by sending data immediately to the client.
        /// </param>
        /// <param name="reportAsPending">
        /// reported as true when sending host message requests to client
        /// </param>
        /// <param name="reportAsDataBoundary">
        /// reported as true when data being reported is as object boundary, i.e the corresponding fragment is an end fragment
        /// </param>
        protected abstract void SendDataToClient(byte[] data, bool flush, bool reportAsPending, bool reportAsDataBoundary);

        /// <summary>
        /// </summary>
        internal abstract void ReportExecutionStatusAsRunning();

        /// <summary>
        /// </summary>
        /// <param name="reasonForClose">
        /// message describing why the transport manager must be closed
        /// </param>
        internal abstract void Close(Exception reasonForClose);

        /// <summary>
        /// Prepare the transport manager to send data (because a command
        /// is about to start). This is used by underlying infrastructure
        /// to send ACK to client..so client can start sending input and
        /// other data to server.
        /// </summary>
        internal virtual void Prepare()
        {
            // command may hijack the calling thread to run the command
            // so this method call notifies the ReceivedData buffer to
            // allow another thread to ProcessRawData.
            ReceivedDataCollection.AllowTwoThreadsToProcessRawData();
        }

        #endregion
    }

    /// <summary>
    /// This represents an abstraction for server session transport manager.
    /// </summary>
    internal abstract class AbstractServerSessionTransportManager : AbstractServerTransportManager
    {
        #region Constructors

        protected AbstractServerSessionTransportManager(int fragmentSize, PSRemotingCryptoHelper cryptoHelper)
            : base(fragmentSize, cryptoHelper)
        {
        }

        #endregion

        #region Abstract interfaces

        /// <summary>
        /// Server RunspacePool driver uses this method to attach to a server transport manager.
        /// </summary>
        /// <param name="powerShellCmdId"></param>
        /// <returns></returns>
        internal abstract AbstractServerTransportManager GetCommandTransportManager(Guid powerShellCmdId);
        /// <summary>
        /// Server RunspacePool driver uses this method to remove association of a command transport manager
        /// from a session transport manager.
        /// </summary>
        /// <param name="powerShellCmdId"></param>
        internal abstract void RemoveCommandTransportManager(Guid powerShellCmdId);

        #endregion
    }

    /// <summary>
    /// A container for helper functions that accomplish common client and server tasks.
    /// </summary>
    internal static class ServerOperationHelpers
    {
        #region Public Helper Methods

        /// <summary>
        /// A helper method to extract a base-64 encoded XML element from a specified input
        /// buffer. The calls required are not compatible with the Managed C++ CoreCLR
        /// mscorlib, but this operation is supported as managed C# code.
        /// </summary>
        /// <param name="xmlBuffer">The input buffer to search. It must be base-64 encoded XML.</param>
        /// <param name="xmlTag">The XML tag used to identify the value to extract.</param>
        /// <returns>The extracted tag converted from a base-64 string.</returns>
        internal static byte[] ExtractEncodedXmlElement(string xmlBuffer, string xmlTag)
        {
            if (xmlBuffer == null || xmlTag == null)
                return new byte[1];

            // the inboundShellInformation is in Xml format as per the SOAP WSMan spec.
            // Retrieve the string (Base64 encoded) we are interested in.
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.CheckCharacters = false;
            readerSettings.IgnoreComments = true;
            readerSettings.IgnoreProcessingInstructions = true;
            readerSettings.XmlResolver = null;
            readerSettings.ConformanceLevel = ConformanceLevel.Fragment;
            readerSettings.MaxCharactersFromEntities = 1024;
            readerSettings.DtdProcessing = System.Xml.DtdProcessing.Prohibit;
            XmlReader reader = XmlReader.Create(new StringReader(xmlBuffer), readerSettings);

            string additionalData;
            if (reader.MoveToContent() == XmlNodeType.Element)
            {
                additionalData = reader.ReadElementContentAsString(xmlTag, reader.NamespaceURI);
            }
            else // No element found, so return a default value
            {
                return new byte[1];
            }

            return Convert.FromBase64String(additionalData);
        }
        #endregion
    }
}
