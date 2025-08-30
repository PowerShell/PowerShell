// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/*
 * Common file that contains implementation for both server and client transport
 * managers for Out-Of-Process and Named Pipe (on the local machine) remoting implementation.
 * These interfaces are used by *-Job cmdlets to support background jobs and
 * attach-to-process feature without depending on WinRM (WinRM has complex requirements like
 * elevation to support local machine remoting).
 */

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Tracing;
using System.Net;
using System.Threading;
using System.Xml;

using PSRemotingCryptoHelper = System.Management.Automation.Internal.PSRemotingCryptoHelper;
using PSRemotingCryptoHelperServer = System.Management.Automation.Internal.PSRemotingCryptoHelperServer;
using RunspaceConnectionInfo = System.Management.Automation.Runspaces.RunspaceConnectionInfo;
using ClientRemotePowerShell = System.Management.Automation.Runspaces.Internal.ClientRemotePowerShell;
using NewProcessConnectionInfo = System.Management.Automation.Runspaces.NewProcessConnectionInfo;
using PSTask = System.Management.Automation.Internal.PSTask;
using PSOpcode = System.Management.Automation.Internal.PSOpcode;
using PSEventId = System.Management.Automation.Internal.PSEventId;
using TypeTable = System.Management.Automation.Runspaces.TypeTable;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    internal static class OutOfProcessUtils
    {
        #region Helper Fields

        internal const string PS_OUT_OF_PROC_DATA_TAG = "Data";
        internal const string PS_OUT_OF_PROC_DATA_ACK_TAG = "DataAck";
        internal const string PS_OUT_OF_PROC_STREAM_ATTRIBUTE = "Stream";
        internal const string PS_OUT_OF_PROC_PSGUID_ATTRIBUTE = "PSGuid";
        internal const string PS_OUT_OF_PROC_CLOSE_TAG = "Close";
        internal const string PS_OUT_OF_PROC_CLOSE_ACK_TAG = "CloseAck";
        internal const string PS_OUT_OF_PROC_COMMAND_TAG = "Command";
        internal const string PS_OUT_OF_PROC_COMMAND_ACK_TAG = "CommandAck";
        internal const string PS_OUT_OF_PROC_SIGNAL_TAG = "Signal";
        internal const string PS_OUT_OF_PROC_SIGNAL_ACK_TAG = "SignalAck";
        internal const int EXITCODE_UNHANDLED_EXCEPTION = 0x0FA0;

        internal static XmlReaderSettings XmlReaderSettings;

        #endregion

        #region Static Constructor

        static OutOfProcessUtils()
        {
            // data coming from inputs stream is in Xml format. create appropriate
            // xml reader settings only once and reuse the same settings for all
            // the reads from StdIn stream.
            XmlReaderSettings = new XmlReaderSettings();
            XmlReaderSettings.CheckCharacters = false;
            XmlReaderSettings.IgnoreComments = true;
            XmlReaderSettings.IgnoreProcessingInstructions = true;
            XmlReaderSettings.XmlResolver = null;
            XmlReaderSettings.ConformanceLevel = ConformanceLevel.Fragment;
        }

        #endregion

        #region Packet Creation Helper Methods

        internal static string CreateDataPacket(byte[] data, DataPriorityType streamType, Guid psGuid)
        {
            string result = string.Format(
                CultureInfo.InvariantCulture,
                "<{0} {1}='{2}' {3}='{4}'>{5}</{0}>",
                PS_OUT_OF_PROC_DATA_TAG,
                PS_OUT_OF_PROC_STREAM_ATTRIBUTE,
                streamType.ToString(),
                PS_OUT_OF_PROC_PSGUID_ATTRIBUTE,
                psGuid.ToString(),
                Convert.ToBase64String(data));

            return result;
        }

        internal static string CreateDataAckPacket(Guid psGuid)
        {
            return CreatePSGuidPacket(PS_OUT_OF_PROC_DATA_ACK_TAG, psGuid);
        }

        internal static string CreateCommandPacket(Guid psGuid)
        {
            return CreatePSGuidPacket(PS_OUT_OF_PROC_COMMAND_TAG, psGuid);
        }

        internal static string CreateCommandAckPacket(Guid psGuid)
        {
            return CreatePSGuidPacket(PS_OUT_OF_PROC_COMMAND_ACK_TAG, psGuid);
        }

        internal static string CreateClosePacket(Guid psGuid)
        {
            return CreatePSGuidPacket(PS_OUT_OF_PROC_CLOSE_TAG, psGuid);
        }

        internal static string CreateCloseAckPacket(Guid psGuid)
        {
            return CreatePSGuidPacket(PS_OUT_OF_PROC_CLOSE_ACK_TAG, psGuid);
        }

        internal static string CreateSignalPacket(Guid psGuid)
        {
            return CreatePSGuidPacket(PS_OUT_OF_PROC_SIGNAL_TAG, psGuid);
        }

        internal static string CreateSignalAckPacket(Guid psGuid)
        {
            return CreatePSGuidPacket(PS_OUT_OF_PROC_SIGNAL_ACK_TAG, psGuid);
        }

        /// <summary>
        /// Common method to create a packet that contains only a PS Guid
        /// with element name changing.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="psGuid"></param>
        /// <returns></returns>
        private static string CreatePSGuidPacket(string element, Guid psGuid)
        {
            string result = string.Format(
                CultureInfo.InvariantCulture,
                "<{0} {1}='{2}' />",
                element,
                PS_OUT_OF_PROC_PSGUID_ATTRIBUTE,
                psGuid.ToString());

            return result;
        }
        #endregion

        #region Packet Processing Helper Methods / Delegates

        internal delegate void DataPacketReceived(byte[] rawData, string stream, Guid psGuid);

        internal delegate void DataAckPacketReceived(Guid psGuid);

        internal delegate void CommandCreationPacketReceived(Guid psGuid);

        internal delegate void CommandCreationAckReceived(Guid psGuid);

        internal delegate void ClosePacketReceived(Guid psGuid);

        internal delegate void CloseAckPacketReceived(Guid psGuid);

        internal delegate void SignalPacketReceived(Guid psGuid);

        internal delegate void SignalAckPacketReceived(Guid psGuid);

        internal struct DataProcessingDelegates
        {
            internal DataPacketReceived DataPacketReceived;
            internal DataAckPacketReceived DataAckPacketReceived;
            internal CommandCreationPacketReceived CommandCreationPacketReceived;
            internal CommandCreationAckReceived CommandCreationAckReceived;
            internal SignalPacketReceived SignalPacketReceived;
            internal SignalAckPacketReceived SignalAckPacketReceived;
            internal ClosePacketReceived ClosePacketReceived;
            internal CloseAckPacketReceived CloseAckPacketReceived;
        }

        /// <summary>
        /// Process's data. Data should be a valid XML.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="callbacks"></param>
        /// <exception cref="PSRemotingTransportException">
        /// 1. Expected only two attributes with names "{0}" and "{1}" in "{2}" element.
        /// 2. Not enough data available to process "{0}" element.
        /// 3. Unknown node "{0}" in "{1}" element. Only "{2}" is expected in "{1}" element.
        /// </exception>
        internal static void ProcessData(string data, DataProcessingDelegates callbacks)
        {
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            XmlReader reader = XmlReader.Create(new StringReader(data), XmlReaderSettings);
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        ProcessElement(reader, callbacks);
                        break;
                    case XmlNodeType.EndElement:
                        break;
                    case XmlNodeType.Text:
                        throw new PSRemotingTransportException(data);
                    default:
                        throw new PSRemotingTransportException(PSRemotingErrorId.IPCUnknownNodeType, RemotingErrorIdStrings.IPCUnknownNodeType,
                            reader.NodeType.ToString(),
                            nameof(XmlNodeType.Element),
                            nameof(XmlNodeType.EndElement));
                }
            }
        }

        /// <summary>
        /// Process an XmlElement. The element name must be one of the following:
        ///         "Data"
        /// </summary>
        /// <param name="xmlReader"></param>
        /// <param name="callbacks"></param>
        /// <exception cref="PSRemotingTransportException">
        /// 1. Expected only two attributes with names "{0}" and "{1}" in "{2}" element.
        /// 2. Not enough data available to process "{0}" element.
        /// 3. Unknown node "{0}" in "{1}" element. Only "{2}" is expected in "{1}" element.
        /// </exception>
        private static void ProcessElement(XmlReader xmlReader, DataProcessingDelegates callbacks)
        {
            Dbg.Assert(xmlReader != null, "xmlReader cannot be null.");
            Dbg.Assert(xmlReader.NodeType == XmlNodeType.Element, "xmlReader's NodeType should be of type Element");

            PowerShellTraceSource tracer = PowerShellTraceSourceFactory.GetTraceSource();

            switch (xmlReader.LocalName)
            {
                case OutOfProcessUtils.PS_OUT_OF_PROC_DATA_TAG:
                    {
                        // A <Data> should have 1 attribute identifying the stream
                        if (xmlReader.AttributeCount != 2)
                        {
                            throw new PSRemotingTransportException(PSRemotingErrorId.IPCWrongAttributeCountForDataElement,
                                RemotingErrorIdStrings.IPCWrongAttributeCountForDataElement,
                                OutOfProcessUtils.PS_OUT_OF_PROC_STREAM_ATTRIBUTE,
                                OutOfProcessUtils.PS_OUT_OF_PROC_PSGUID_ATTRIBUTE,
                                OutOfProcessUtils.PS_OUT_OF_PROC_DATA_TAG);
                        }

                        string stream = xmlReader.GetAttribute(OutOfProcessUtils.PS_OUT_OF_PROC_STREAM_ATTRIBUTE);
                        string psGuidString = xmlReader.GetAttribute(OutOfProcessUtils.PS_OUT_OF_PROC_PSGUID_ATTRIBUTE);
                        Guid psGuid = new Guid(psGuidString);

                        // Now move the reader to the data portion
                        if (!xmlReader.Read())
                        {
                            throw new PSRemotingTransportException(PSRemotingErrorId.IPCInsufficientDataforElement,
                                RemotingErrorIdStrings.IPCInsufficientDataforElement,
                                OutOfProcessUtils.PS_OUT_OF_PROC_DATA_TAG);
                        }

                        if (xmlReader.NodeType != XmlNodeType.Text)
                        {
                            throw new PSRemotingTransportException(PSRemotingErrorId.IPCOnlyTextExpectedInDataElement,
                                RemotingErrorIdStrings.IPCOnlyTextExpectedInDataElement,
                                xmlReader.NodeType, OutOfProcessUtils.PS_OUT_OF_PROC_DATA_TAG, XmlNodeType.Text);
                        }

                        string data = xmlReader.Value;
                        tracer.WriteMessage("OutOfProcessUtils.ProcessElement : PS_OUT_OF_PROC_DATA received, psGuid : " + psGuid.ToString());
                        byte[] rawData = Convert.FromBase64String(data);
                        callbacks.DataPacketReceived(rawData, stream, psGuid);
                    }

                    break;
                case OutOfProcessUtils.PS_OUT_OF_PROC_DATA_ACK_TAG:
                    {
                        if (xmlReader.AttributeCount != 1)
                        {
                            throw new PSRemotingTransportException(PSRemotingErrorId.IPCWrongAttributeCountForElement,
                                RemotingErrorIdStrings.IPCWrongAttributeCountForElement,
                                OutOfProcessUtils.PS_OUT_OF_PROC_PSGUID_ATTRIBUTE,
                                OutOfProcessUtils.PS_OUT_OF_PROC_DATA_ACK_TAG);
                        }

                        string psGuidString = xmlReader.GetAttribute(OutOfProcessUtils.PS_OUT_OF_PROC_PSGUID_ATTRIBUTE);
                        Guid psGuid = new Guid(psGuidString);

                        tracer.WriteMessage("OutOfProcessUtils.ProcessElement : PS_OUT_OF_PROC_DATA_ACK received, psGuid : " + psGuid.ToString());
                        callbacks.DataAckPacketReceived(psGuid);
                    }

                    break;
                case OutOfProcessUtils.PS_OUT_OF_PROC_COMMAND_TAG:
                    {
                        if (xmlReader.AttributeCount != 1)
                        {
                            throw new PSRemotingTransportException(PSRemotingErrorId.IPCWrongAttributeCountForElement,
                                RemotingErrorIdStrings.IPCWrongAttributeCountForElement,
                                OutOfProcessUtils.PS_OUT_OF_PROC_PSGUID_ATTRIBUTE,
                                OutOfProcessUtils.PS_OUT_OF_PROC_COMMAND_TAG);
                        }

                        string psGuidString = xmlReader.GetAttribute(OutOfProcessUtils.PS_OUT_OF_PROC_PSGUID_ATTRIBUTE);
                        Guid psGuid = new Guid(psGuidString);

                        tracer.WriteMessage("OutOfProcessUtils.ProcessElement : PS_OUT_OF_PROC_COMMAND received, psGuid : " + psGuid.ToString());
                        callbacks.CommandCreationPacketReceived(psGuid);
                    }

                    break;
                case OutOfProcessUtils.PS_OUT_OF_PROC_COMMAND_ACK_TAG:
                    {
                        if (xmlReader.AttributeCount != 1)
                        {
                            throw new PSRemotingTransportException(PSRemotingErrorId.IPCWrongAttributeCountForElement,
                                RemotingErrorIdStrings.IPCWrongAttributeCountForElement,
                                OutOfProcessUtils.PS_OUT_OF_PROC_PSGUID_ATTRIBUTE,
                                OutOfProcessUtils.PS_OUT_OF_PROC_COMMAND_ACK_TAG);
                        }

                        string psGuidString = xmlReader.GetAttribute(OutOfProcessUtils.PS_OUT_OF_PROC_PSGUID_ATTRIBUTE);
                        Guid psGuid = new Guid(psGuidString);
                        tracer.WriteMessage("OutOfProcessUtils.ProcessElement : PS_OUT_OF_PROC_COMMAND_ACK received, psGuid : " + psGuid.ToString());
                        callbacks.CommandCreationAckReceived(psGuid);
                    }

                    break;
                case OutOfProcessUtils.PS_OUT_OF_PROC_CLOSE_TAG:
                    {
                        if (xmlReader.AttributeCount != 1)
                        {
                            throw new PSRemotingTransportException(PSRemotingErrorId.IPCWrongAttributeCountForElement,
                                RemotingErrorIdStrings.IPCWrongAttributeCountForElement,
                                OutOfProcessUtils.PS_OUT_OF_PROC_PSGUID_ATTRIBUTE,
                                OutOfProcessUtils.PS_OUT_OF_PROC_CLOSE_TAG);
                        }

                        string psGuidString = xmlReader.GetAttribute(OutOfProcessUtils.PS_OUT_OF_PROC_PSGUID_ATTRIBUTE);
                        Guid psGuid = new Guid(psGuidString);

                        tracer.WriteMessage("OutOfProcessUtils.ProcessElement : PS_OUT_OF_PROC_CLOSE received, psGuid : " + psGuid.ToString());
                        callbacks.ClosePacketReceived(psGuid);
                    }

                    break;
                case OutOfProcessUtils.PS_OUT_OF_PROC_CLOSE_ACK_TAG:
                    {
                        if (xmlReader.AttributeCount != 1)
                        {
                            throw new PSRemotingTransportException(PSRemotingErrorId.IPCWrongAttributeCountForElement,
                            RemotingErrorIdStrings.IPCWrongAttributeCountForElement,
                                OutOfProcessUtils.PS_OUT_OF_PROC_PSGUID_ATTRIBUTE,
                                OutOfProcessUtils.PS_OUT_OF_PROC_CLOSE_ACK_TAG);
                        }

                        string psGuidString = xmlReader.GetAttribute(OutOfProcessUtils.PS_OUT_OF_PROC_PSGUID_ATTRIBUTE);
                        Guid psGuid = new Guid(psGuidString);
                        tracer.WriteMessage("OutOfProcessUtils.ProcessElement : PS_OUT_OF_PROC_CLOSE_ACK received, psGuid : " + psGuid.ToString());
                        callbacks.CloseAckPacketReceived(psGuid);
                    }

                    break;
                case OutOfProcessUtils.PS_OUT_OF_PROC_SIGNAL_TAG:
                    {
                        if (xmlReader.AttributeCount != 1)
                        {
                            throw new PSRemotingTransportException(PSRemotingErrorId.IPCWrongAttributeCountForElement,
                            RemotingErrorIdStrings.IPCWrongAttributeCountForElement,
                                OutOfProcessUtils.PS_OUT_OF_PROC_PSGUID_ATTRIBUTE,
                                OutOfProcessUtils.PS_OUT_OF_PROC_SIGNAL_TAG);
                        }

                        string psGuidString = xmlReader.GetAttribute(OutOfProcessUtils.PS_OUT_OF_PROC_PSGUID_ATTRIBUTE);
                        Guid psGuid = new Guid(psGuidString);

                        tracer.WriteMessage("OutOfProcessUtils.ProcessElement : PS_OUT_OF_PROC_SIGNAL received, psGuid : " + psGuid.ToString());
                        callbacks.SignalPacketReceived(psGuid);
                    }

                    break;
                case OutOfProcessUtils.PS_OUT_OF_PROC_SIGNAL_ACK_TAG:
                    {
                        if (xmlReader.AttributeCount != 1)
                        {
                            throw new PSRemotingTransportException(PSRemotingErrorId.IPCWrongAttributeCountForElement,
                            RemotingErrorIdStrings.IPCWrongAttributeCountForElement,
                                OutOfProcessUtils.PS_OUT_OF_PROC_PSGUID_ATTRIBUTE,
                                OutOfProcessUtils.PS_OUT_OF_PROC_SIGNAL_ACK_TAG);
                        }

                        string psGuidString = xmlReader.GetAttribute(OutOfProcessUtils.PS_OUT_OF_PROC_PSGUID_ATTRIBUTE);
                        Guid psGuid = new Guid(psGuidString);
                        tracer.WriteMessage("OutOfProcessUtils.ProcessElement : PS_OUT_OF_PROC_SIGNAL_ACK received, psGuid : " + psGuid.ToString());
                        callbacks.SignalAckPacketReceived(psGuid);
                    }

                    break;
                default:
                    throw new PSRemotingTransportException(PSRemotingErrorId.IPCUnknownElementReceived,
                    RemotingErrorIdStrings.IPCUnknownElementReceived,
                        xmlReader.LocalName);
            }
        }

        #endregion
    }

    /// <summary>
    /// A wrapper around TextWriter to allow for synchronized writing to a stream.
    /// Synchronization is required to avoid collision when multiple TransportManager's
    /// write data at the same time to the same writer.
    /// </summary>
    internal class OutOfProcessTextWriter
    {
        #region Private Data

        private readonly TextWriter _writer;
        private bool _isStopped;
        private readonly object _syncObject = new object();
        private const string _errorPrepend = "__NamedPipeError__:";

        #endregion

        #region Properties

        /// <summary>
        /// Prefix for transport error message.
        /// </summary>
        public static string ErrorPrefix
        {
            get => _errorPrepend;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs the wrapper.
        /// </summary>
        /// <param name="writerToWrap"></param>
        public OutOfProcessTextWriter(TextWriter writerToWrap)
        {
            if (writerToWrap is null)
            {
                throw new PSArgumentNullException(nameof(writerToWrap));
            }

            _writer = writerToWrap;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Calls writer.WriteLine() with data.
        /// </summary>
        /// <param name="data"></param>
        public virtual void WriteLine(string data)
        {
            if (_isStopped)
            {
                return;
            }

            lock (_syncObject)
            {
                _writer.WriteLine(data);
                _writer.Flush();
            }
        }

        /// <summary>
        /// Stops the writer from writing anything to the stream.
        /// This is used by session transport manager when the server
        /// process is exited but the session data structure handlers
        /// are not notified yet. So while the data structure handler
        /// is disposing we should not let anyone use the stream.
        /// </summary>
        internal void StopWriting()
        {
            _isStopped = true;
        }

        #endregion
    }
}

namespace System.Management.Automation.Remoting.Client
{
    /// <summary>
    /// Client session transport manager abstract base class.
    /// </summary>
    public abstract class ClientSessionTransportManagerBase : BaseClientSessionTransportManager
    {
        #region Data

        private readonly BlockingCollection<string> _sessionMessageQueue;
        private readonly BlockingCollection<string> _commandMessageQueue;
        private readonly PrioritySendDataCollection.OnDataAvailableCallback _onDataAvailableToSendCallback;
        private OutOfProcessUtils.DataProcessingDelegates _dataProcessingCallbacks;
        private readonly Dictionary<Guid, OutOfProcessClientCommandTransportManager> _cmdTransportManagers;
        private readonly Timer _closeTimeOutTimer;
        internal PowerShellTraceSource _tracer;
        internal OutOfProcessTextWriter _messageWriter;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        protected ClientSessionTransportManagerBase(
            Guid runspaceId,
            PSRemotingCryptoHelper cryptoHelper)
            : base(runspaceId, cryptoHelper)
        {
            _onDataAvailableToSendCallback =
                new PrioritySendDataCollection.OnDataAvailableCallback(OnDataAvailableCallback);

            _cmdTransportManagers = new Dictionary<Guid, OutOfProcessClientCommandTransportManager>();

            _dataProcessingCallbacks = new OutOfProcessUtils.DataProcessingDelegates();
            _dataProcessingCallbacks.DataPacketReceived += new OutOfProcessUtils.DataPacketReceived(OnDataPacketReceived);
            _dataProcessingCallbacks.DataAckPacketReceived += new OutOfProcessUtils.DataAckPacketReceived(OnDataAckPacketReceived);
            _dataProcessingCallbacks.CommandCreationPacketReceived += new OutOfProcessUtils.CommandCreationPacketReceived(OnCommandCreationPacketReceived);
            _dataProcessingCallbacks.CommandCreationAckReceived += new OutOfProcessUtils.CommandCreationAckReceived(OnCommandCreationAckReceived);
            _dataProcessingCallbacks.SignalPacketReceived += new OutOfProcessUtils.SignalPacketReceived(OnSignalPacketReceived);
            _dataProcessingCallbacks.SignalAckPacketReceived += new OutOfProcessUtils.SignalAckPacketReceived(OnSignalAckPacketReceived);
            _dataProcessingCallbacks.ClosePacketReceived += new OutOfProcessUtils.ClosePacketReceived(OnClosePacketReceived);
            _dataProcessingCallbacks.CloseAckPacketReceived += new OutOfProcessUtils.CloseAckPacketReceived(OnCloseAckReceived);

            dataToBeSent.Fragmentor = Fragmentor;
            // session transport manager can receive unlimited data..however each object is limited
            // by maxRecvdObjectSize. this is to allow clients to use a session for an unlimited time..
            // also the messages that can be sent to a session are limited and very controlled.
            // However a command transport manager can be restricted to receive only a fixed amount of data
            // controlled by maxRecvdDataSizeCommand..This is because commands can accept any number of input
            // objects.
            ReceivedDataCollection.MaximumReceivedDataSize = null;
            ReceivedDataCollection.MaximumReceivedObjectSize = BaseTransportManager.MaximumReceivedObjectSize;
            // timers initialization
            _closeTimeOutTimer = new Timer(OnCloseTimeOutTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);

            // Session message processing
            _sessionMessageQueue = new BlockingCollection<string>();
            var sessionThread = new Thread(ProcessMessageProc);
            sessionThread.Name = "SessionMessageProcessing";
            sessionThread.IsBackground = true;
            sessionThread.Start(_sessionMessageQueue);

            // Command message processing
            _commandMessageQueue = new BlockingCollection<string>();
            var commandThread = new Thread(ProcessMessageProc);
            commandThread.Name = "CommandMessageProcessing";
            commandThread.IsBackground = true;
            commandThread.Start(_commandMessageQueue);

            _tracer = PowerShellTraceSourceFactory.GetTraceSource();
        }

        #endregion

        #region Overrides

        internal override void ConnectAsync()
        {
            throw new NotImplementedException(RemotingErrorIdStrings.IPCTransportConnectError);
        }

        /// <summary>
        /// Closes the server process.
        /// </summary>
        public override void CloseAsync()
        {
            bool shouldRaiseCloseCompleted = false;
            lock (syncObject)
            {
                if (isClosed)
                {
                    return;
                }

                // first change the state..so other threads
                // will know that we are closing.
                isClosed = true;

                if (_messageWriter == null)
                {
                    // this will happen if CloseAsync() is called
                    // before ConnectAsync()..in which case we
                    // just need to raise close completed event.
                    shouldRaiseCloseCompleted = true;
                }
            }

            base.CloseAsync();

            if (shouldRaiseCloseCompleted)
            {
                RaiseCloseCompleted();
                return;
            }

            PSEtwLog.LogAnalyticInformational(PSEventId.WSManCloseShell,
                PSOpcode.Disconnect, PSTask.None, PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                RunspacePoolInstanceId.ToString());

            _tracer.WriteMessage("OutOfProcessClientSessionTransportManager.CloseAsync, when sending close session packet, progress command count should be zero, current cmd count: " + _cmdTransportManagers.Count + ", RunSpacePool Id : " + this.RunspacePoolInstanceId);

            try
            {
                // send Close signal to the server and let it die gracefully.
                _messageWriter.WriteLine(OutOfProcessUtils.CreateClosePacket(Guid.Empty));

                // start the timer..so client can fail deterministically
                _closeTimeOutTimer.Change(60 * 1000, Timeout.Infinite);
            }
            catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException)
            {
                // Cannot communicate with server.  Allow client to complete close operation.
                shouldRaiseCloseCompleted = true;
            }

            if (shouldRaiseCloseCompleted)
            {
                RaiseCloseCompleted();
            }
        }

        /// <summary>
        /// Create a transport manager for command.
        /// </summary>
        /// <param name="connectionInfo"></param>
        /// <param name="cmd"></param>
        /// <param name="noInput"></param>
        /// <returns></returns>
        internal override BaseClientCommandTransportManager CreateClientCommandTransportManager(
            RunspaceConnectionInfo connectionInfo,
            ClientRemotePowerShell cmd,
            bool noInput)
        {
            Dbg.Assert(cmd != null, "Cmd cannot be null");

            OutOfProcessClientCommandTransportManager result = new
                OutOfProcessClientCommandTransportManager(cmd, noInput, this, _messageWriter);
            AddCommandTransportManager(cmd.InstanceId, result);

            return result;
        }

        /// <summary>
        /// Terminates the server process and disposes other resources.
        /// </summary>
        /// <param name="isDisposing"></param>
        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            if (isDisposing)
            {
                _cmdTransportManagers.Clear();
                _closeTimeOutTimer.Dispose();
                DisposeMessageQueue();
            }
        }

        #endregion

        #region Helper Methods

        private void AddCommandTransportManager(Guid key, OutOfProcessClientCommandTransportManager cmdTM)
        {
            lock (syncObject)
            {
                if (isClosed)
                {
                    // It is possible for this add command to occur after/during session close via
                    // asynchronous stop pipeline or Stop-Job.  In this case ignore the command.
                    _tracer.WriteMessage("OutOfProcessClientSessionTransportManager.AddCommandTransportManager, Adding command transport on closed session, RunSpacePool Id : " + this.RunspacePoolInstanceId);
                    return;
                }

                Dbg.Assert(!_cmdTransportManagers.ContainsKey(key), "key already exists");
                _cmdTransportManagers.Add(key, cmdTM);
            }
        }

        internal override void RemoveCommandTransportManager(Guid key)
        {
            lock (syncObject)
            {
                // We always need to remove commands from collection, even if isClosed is true.
                // If we don't then we will not respond because CloseAsync() will not complete until all
                // commands are closed.
                if (!_cmdTransportManagers.Remove(key))
                {
                    _tracer.WriteMessage("key does not exist to remove from cmdTransportManagers");
                }
            }
        }

        private OutOfProcessClientCommandTransportManager GetCommandTransportManager(Guid key)
        {
            lock (syncObject)
            {
                OutOfProcessClientCommandTransportManager result = null;
                _cmdTransportManagers.TryGetValue(key, out result);
                return result;
            }
        }

        private void OnCloseSessionCompleted()
        {
            // stop timer
            _closeTimeOutTimer.Change(Timeout.Infinite, Timeout.Infinite);

            RaiseCloseCompleted();
            CleanupConnection();
        }

        /// <summary>
        /// Optional additional connection clean up after a connection is closed.
        /// </summary>
        protected abstract void CleanupConnection();

        private void ProcessMessageProc(object state)
        {
            var messageQueue = state as BlockingCollection<string>;

            try
            {
                while (true)
                {
                    var data = messageQueue.Take();
                    try
                    {
                        OutOfProcessUtils.ProcessData(data, _dataProcessingCallbacks);
                    }
                    catch (Exception exception)
                    {
                        PSRemotingTransportException psrte =
                            new PSRemotingTransportException(
                                PSRemotingErrorId.IPCErrorProcessingServerData,
                                RemotingErrorIdStrings.IPCErrorProcessingServerData,
                                exception.Message);
                        RaiseErrorHandler(new TransportErrorOccuredEventArgs(psrte, TransportMethodEnum.ReceiveShellOutputEx));
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // Normal session message processing thread end.
            }
        }

        #endregion

        #region Event Handlers

        private const string SESSIONDMESSAGETAG = "PSGuid='00000000-0000-0000-0000-000000000000'";

        /// <summary>
        /// Handles protocol output data from a transport.
        /// </summary>
        protected void HandleOutputDataReceived(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                // A null/empty data string indicates a problem in the transport,
                // e.g., named pipe emitting a null packet because it closed or some reason.
                // In this case we simply ignore the packet.
                return;
            }

            try
            {
                // Route protocol message based on whether it is a session or command message.
                if (data.Contains(SESSIONDMESSAGETAG, StringComparison.OrdinalIgnoreCase))
                {
                    // Session message
                    _sessionMessageQueue.Add(data);
                }
                else
                {
                    // Command message
                    _commandMessageQueue.Add(data);
                }
            }
            catch (InvalidOperationException)
            {
                // This exception will be thrown by the BlockingCollection message queue objects
                // after they have been closed.
            }
        }

        /// <summary>
        /// Handles protocol error data.
        /// </summary>
        protected void HandleErrorDataReceived(string data)
        {
            lock (syncObject)
            {
                if (isClosed)
                {
                    return;
                }
            }

            PSRemotingTransportException psrte = new PSRemotingTransportException(PSRemotingErrorId.IPCServerProcessReportedError,
                RemotingErrorIdStrings.IPCServerProcessReportedError,
                data);
            RaiseErrorHandler(new TransportErrorOccuredEventArgs(psrte, TransportMethodEnum.Unknown));
        }

        #endregion

        #region Sending Data related Methods

        /// <summary>
        /// Send any data packet in the queue.
        /// </summary>
        protected void SendOneItem()
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
            PSEtwLog.LogAnalyticInformational(
                       PSEventId.WSManSendShellInputEx, PSOpcode.Send, PSTask.None,
                       PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                       RunspacePoolInstanceId.ToString(),
                       Guid.Empty.ToString(),
                       data.Length.ToString(CultureInfo.InvariantCulture));

            lock (syncObject)
            {
                if (isClosed)
                {
                    return;
                }

                _messageWriter.WriteLine(OutOfProcessUtils.CreateDataPacket(data,
                    priorityType,
                    Guid.Empty));
            }
        }

        private void OnRemoteSessionSendCompleted()
        {
            PSEtwLog.LogAnalyticInformational(PSEventId.WSManSendShellInputExCallbackReceived,
                PSOpcode.Connect, PSTask.None, PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                RunspacePoolInstanceId.ToString(), Guid.Empty.ToString());

            SendOneItem();
        }

        #endregion

        #region Data Processing handlers

        private void OnDataPacketReceived(byte[] rawData, string stream, Guid psGuid)
        {
            string streamTemp = System.Management.Automation.Remoting.Client.WSManNativeApi.WSMAN_STREAM_ID_STDOUT;
            if (stream.Equals(nameof(DataPriorityType.PromptResponse), StringComparison.OrdinalIgnoreCase))
            {
                streamTemp = System.Management.Automation.Remoting.Client.WSManNativeApi.WSMAN_STREAM_ID_PROMPTRESPONSE;
            }

            if (psGuid == Guid.Empty)
            {
                PSEtwLog.LogAnalyticInformational(
                    PSEventId.WSManReceiveShellOutputExCallbackReceived, PSOpcode.Receive, PSTask.None,
                    PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                    RunspacePoolInstanceId.ToString(),
                    Guid.Empty.ToString(),
                    rawData.Length.ToString(CultureInfo.InvariantCulture));

                // this data is meant for session.
                base.ProcessRawData(rawData, streamTemp);
            }
            else
            {
                // this is for a command
                OutOfProcessClientCommandTransportManager cmdTM = GetCommandTransportManager(psGuid);

                // not throwing the exception in null case as the command might have already
                // closed. The RS data structure handler does not wait for the close ack before
                // it clears the command transport manager..so this might happen.
                cmdTM?.OnRemoteCmdDataReceived(rawData, streamTemp);
            }
        }

        private void OnDataAckPacketReceived(Guid psGuid)
        {
            if (psGuid == Guid.Empty)
            {
                // this data is meant for session.
                OnRemoteSessionSendCompleted();
            }
            else
            {
                // this is for a command
                OutOfProcessClientCommandTransportManager cmdTM = GetCommandTransportManager(psGuid);

                // not throwing the exception in null case as the command might have already
                // closed. The RS data structure handler does not wait for the close ack before
                // it clears the command transport manager..so this might happen.
                cmdTM?.OnRemoteCmdSendCompleted();
            }
        }

        private void OnCommandCreationPacketReceived(Guid psGuid)
        {
            throw new PSRemotingTransportException(PSRemotingErrorId.IPCUnknownElementReceived,
                RemotingErrorIdStrings.IPCUnknownElementReceived,
                   OutOfProcessUtils.PS_OUT_OF_PROC_COMMAND_TAG);
        }

        private void OnCommandCreationAckReceived(Guid psGuid)
        {
            OutOfProcessClientCommandTransportManager cmdTM = GetCommandTransportManager(psGuid);
            if (cmdTM == null)
            {
                throw new PSRemotingTransportException(PSRemotingErrorId.IPCUnknownCommandGuid,
                    RemotingErrorIdStrings.IPCUnknownCommandGuid,
                    psGuid.ToString(), OutOfProcessUtils.PS_OUT_OF_PROC_COMMAND_ACK_TAG);
            }

            cmdTM.OnCreateCmdCompleted();

            _tracer.WriteMessage("OutOfProcessClientSessionTransportManager.OnCommandCreationAckReceived, in progress command count after cmd creation ACK : " + _cmdTransportManagers.Count + ", psGuid : " + psGuid.ToString());
        }

        private void OnSignalPacketReceived(Guid psGuid)
        {
            throw new PSRemotingTransportException(
                PSRemotingErrorId.IPCUnknownElementReceived,
                RemotingErrorIdStrings.IPCUnknownElementReceived,
                   OutOfProcessUtils.PS_OUT_OF_PROC_SIGNAL_TAG);
        }

        private void OnSignalAckPacketReceived(Guid psGuid)
        {
            if (psGuid == Guid.Empty)
            {
                throw new PSRemotingTransportException(
                    PSRemotingErrorId.IPCNoSignalForSession,
                    RemotingErrorIdStrings.IPCNoSignalForSession,
                    OutOfProcessUtils.PS_OUT_OF_PROC_SIGNAL_ACK_TAG);
            }
            else
            {
                OutOfProcessClientCommandTransportManager cmdTM = GetCommandTransportManager(psGuid);
                cmdTM?.OnRemoteCmdSignalCompleted();
            }
        }

        private void OnClosePacketReceived(Guid psGuid)
        {
            throw new PSRemotingTransportException(PSRemotingErrorId.IPCUnknownElementReceived,
                RemotingErrorIdStrings.IPCUnknownElementReceived,
                   OutOfProcessUtils.PS_OUT_OF_PROC_CLOSE_TAG);
        }

        private void OnCloseAckReceived(Guid psGuid)
        {
            int commandCount;
            lock (syncObject)
            {
                commandCount = _cmdTransportManagers.Count;
            }

            if (psGuid == Guid.Empty)
            {
                _tracer.WriteMessage("OutOfProcessClientSessionTransportManager.OnCloseAckReceived, progress command count after CLOSE ACK should be zero = " + commandCount + " psGuid : " + psGuid.ToString());

                this.OnCloseSessionCompleted();
            }
            else
            {
                _tracer.WriteMessage("OutOfProcessClientSessionTransportManager.OnCloseAckReceived, in progress command count should be greater than zero: " + commandCount + ", RunSpacePool Id : " + this.RunspacePoolInstanceId + ", psGuid : " + psGuid.ToString());

                OutOfProcessClientCommandTransportManager cmdTM = GetCommandTransportManager(psGuid);

                // this might legitimately happen if cmd is already closed before we get an
                // ACK back from server.
                cmdTM?.OnCloseCmdCompleted();
            }
        }

        #endregion

        #region Private Timeout handlers

        internal void OnCloseTimeOutTimerElapsed(object source)
        {
            PSRemotingTransportException psrte = new PSRemotingTransportException(PSRemotingErrorId.IPCCloseTimedOut, RemotingErrorIdStrings.IPCCloseTimedOut);
            RaiseErrorHandler(new TransportErrorOccuredEventArgs(psrte, TransportMethodEnum.CloseShellOperationEx));
        }

        #endregion
    
        #region Protected Methods

        /// <summary>
        /// Standard handler for data received, to be used by custom transport implementations.
        /// </summary>
        /// <param name="data">Protocol text data received by custom transport.</param>
        protected void HandleDataReceived(string data)
        {
            if (data.StartsWith(OutOfProcessTextWriter.ErrorPrefix, StringComparison.OrdinalIgnoreCase))
            {
                // Error message from the server.
                string errorData = data.Substring(OutOfProcessTextWriter.ErrorPrefix.Length);
                HandleErrorDataReceived(errorData);
            }
            else
            {
                // Normal output data.
                HandleOutputDataReceived(data);
            }
        }

        /// <summary>
        /// Creates the transport message writer from the provided TexWriter object.
        /// </summary>
        /// <param name"textWriter">TextWriter object to be used in the message writer.</param>
        protected void SetMessageWriter(TextWriter textWriter)
        {
            _messageWriter = new OutOfProcessTextWriter(textWriter);
        }

        /// <summary>
        /// Disposes message queue components.
        /// </summary>
        protected void DisposeMessageQueue()
        {
            // Stop session processing thread.
            try
            {
                _sessionMessageQueue.CompleteAdding();
            }
            catch (ObjectDisposedException)
            {
                // Object already disposed.
            }

            _sessionMessageQueue.Dispose();

            // Stop command processing thread.
            try
            {
                _commandMessageQueue.CompleteAdding();
            }
            catch (ObjectDisposedException)
            {
                // Object already disposed.
            }

            _commandMessageQueue.Dispose();
        }

        #endregion
    }

    internal class OutOfProcessClientSessionTransportManager : ClientSessionTransportManagerBase
    {
        #region Private Data

        private Process _serverProcess;
        private readonly NewProcessConnectionInfo _connectionInfo;
        private bool _processCreated = true;
        private PowerShellProcessInstance _processInstance;

        #endregion

        #region Constructor

        internal OutOfProcessClientSessionTransportManager(Guid runspaceId,
            NewProcessConnectionInfo connectionInfo,
            PSRemotingCryptoHelper cryptoHelper)
            : base(runspaceId, cryptoHelper)
        {
            _connectionInfo = connectionInfo;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Launch a new Process (pwsh -s) to perform remoting. This is used by *-Job cmdlets
        /// to support background jobs without depending on WinRM (WinRM has complex requirements like
        /// elevation to support local machine remoting).
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// </exception>
        /// <exception cref="System.ComponentModel.Win32Exception">
        /// 1. There was an error in opening the associated file.
        /// </exception>
        public override void CreateAsync()
        {
            if (_connectionInfo != null)
            {
                _processInstance = _connectionInfo.Process ?? new PowerShellProcessInstance(_connectionInfo.PSVersion,
                                                                                           _connectionInfo.Credential,
                                                                                           _connectionInfo.InitializationScript,
                                                                                           _connectionInfo.RunAs32,
                                                                                           _connectionInfo.WorkingDirectory);
                if (_connectionInfo.Process != null)
                {
                    _processCreated = false;
                }
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
                        return;
                    }

                    // Attach handlers and start the process
                    _serverProcess = _processInstance.Process;

                    if (_processInstance.RunspacePool != null)
                    {
                        _processInstance.RunspacePool.Close();
                        _processInstance.RunspacePool.Dispose();
                    }

                    _serverProcess.Exited += OnExited;
                    _processInstance.Start();

                    StartRedirectionReaderThreads(_serverProcess);
                    SetMessageWriter(_serverProcess.StandardInput);
                    _processInstance.StdInWriter = _messageWriter;
                }
            }
            catch (System.ComponentModel.Win32Exception w32e)
            {
                PSRemotingTransportException psrte = new PSRemotingTransportException(w32e, RemotingErrorIdStrings.IPCExceptionLaunchingProcess,
                    w32e.Message);
                psrte.ErrorCode = w32e.HResult;
                TransportErrorOccuredEventArgs eventargs = new TransportErrorOccuredEventArgs(psrte, TransportMethodEnum.CreateShellEx);
                RaiseErrorHandler(eventargs);
                return;
            }
            catch (Exception e)
            {
                PSRemotingTransportException psrte = new PSRemotingTransportException(PSRemotingErrorId.IPCExceptionLaunchingProcess,
                RemotingErrorIdStrings.IPCExceptionLaunchingProcess,
                    e.Message);
                TransportErrorOccuredEventArgs eventargs = new TransportErrorOccuredEventArgs(psrte, TransportMethodEnum.CreateShellEx);
                RaiseErrorHandler(eventargs);
                return;
            }

            // Send one fragment
            SendOneItem();
        }

        private void StartRedirectionReaderThreads(Process serverProcess)
        {
            Thread outputThread = new Thread(ProcessOutputData);
            outputThread.IsBackground = true;
            outputThread.Name = "Out-of-Proc Job Output Thread";

            Thread errorThread = new Thread(ProcessErrorData);
            errorThread.IsBackground = true;
            errorThread.Name = "Out-of-Proc Job Error Thread";

            outputThread.Start(serverProcess.StandardOutput);
            errorThread.Start(serverProcess.StandardError);
        }

        private void ProcessOutputData(object arg)
        {
            if (arg is StreamReader reader)
            {
                try
                {
                    string data = reader.ReadLine();
                    while (data != null)
                    {
                        HandleOutputDataReceived(data);
                        data = reader.ReadLine();
                    }
                }
                catch (IOException)
                {
                    // Treat this as EOF, the same as what 'Process.BeginOutputReadLine()' does.
                }
                catch (Exception e)
                {
                    _tracer.WriteMessage(
                        "OutOfProcessClientSessionTransportManager",
                        "ProcessOutputThread",
                        Guid.Empty,
                        "Transport manager output reader thread ended with error: {0}",
                        e.Message ?? string.Empty);
                }
            }
            else
            {
                Dbg.Assert(false, "Invalid argument. Expecting a StreamReader object.");
            }
        }

        private void ProcessErrorData(object arg)
        {
            if (arg is StreamReader reader)
            {
                try
                {
                    string data = reader.ReadLine();
                    while (data != null)
                    {
                        HandleErrorDataReceived(data);
                        data = reader.ReadLine();
                    }
                }
                catch (IOException)
                {
                    // Treat this as EOF, the same as what 'Process.BeginErrorReadLine()' does.
                }
                catch (Exception e)
                {
                    _tracer.WriteMessage(
                        "OutOfProcessClientSessionTransportManager",
                        "ProcessErrorThread",
                        Guid.Empty,
                        "Transport manager error reader thread ended with error: {0}",
                        e.Message ?? string.Empty);
                }
            }
            else
            {
                Dbg.Assert(false, "Invalid argument. Expecting a StreamReader object.");
            }
        }

        /// <summary>
        /// Kills the server process and disposes other resources.
        /// </summary>
        /// <param name="isDisposing"></param>
        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            if (isDisposing)
            {
                KillServerProcess();
                if (_serverProcess != null && _processCreated)
                {
                    // null can happen if Dispose is called before ConnectAsync()
                    _serverProcess.Dispose();
                }
            }
        }

        protected override void CleanupConnection()
        {
            // Clean up the child process
            KillServerProcess();
        }

        #endregion

        #region Helper Methods

        private void KillServerProcess()
        {
            if (_serverProcess == null)
            {
                // this can happen if Dispose is called before ConnectAsync()
                return;
            }

            try
            {
                if (!_serverProcess.HasExited)
                {
                    _serverProcess.Exited -= OnExited;

                    if (_processCreated)
                    {
                        _serverProcess.Kill();
                    }
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                try
                {
                    // For processes running in an NTVDM, trying to kill with
                    // the original handle fails with a Win32 error, so we'll
                    // use the ID and try to get a new handle...
                    Process newHandle = Process.GetProcessById(_serverProcess.Id);
                    // If the process was not found, we won't get here...
                    if (_processCreated) newHandle.Kill();
                }
                catch (Exception)
                {
                }
            }
            catch (Exception)
            {
            }
        }

        private void OnExited(object sender, EventArgs e)
        {
            TransportMethodEnum transportMethod = TransportMethodEnum.Unknown;
            lock (syncObject)
            {
                // There is no need to return when IsClosed==true here as in a legitimate case process exits
                // after Close is called..In that legitimate case, Exit handler is removed before
                // calling Exit..So, this Exit must have been called abnormally.
                if (isClosed)
                {
                    transportMethod = TransportMethodEnum.CloseShellOperationEx;
                }

                // dont let the writer write new data as the process is exited.
                // Not assigning null to stdInWriter to fix the race condition between OnExited() and CloseAsync() methods.
                //
                _messageWriter.StopWriting();
            }

            // Try to get details about why the process exited
            // and if they're not available, give information as to why
            string processDiagnosticMessage;
            try
            {
                var jobProcess = (Process)sender;
                processDiagnosticMessage = StringUtil.Format(
                    RemotingErrorIdStrings.ProcessExitInfo,
                    jobProcess.ExitCode,
                    jobProcess.StandardOutput.ReadToEnd(),
                    jobProcess.StandardError.ReadToEnd());
            }
            catch (Exception exception)
            {
                processDiagnosticMessage = StringUtil.Format(
                    RemotingErrorIdStrings.ProcessInfoNotRecoverable,
                    exception.Message);
            }

            string exitErrorMsg = StringUtil.Format(
                RemotingErrorIdStrings.IPCServerProcessExited,
                processDiagnosticMessage);
            var psrte = new PSRemotingTransportException(
                PSRemotingErrorId.IPCServerProcessExited,
                exitErrorMsg);
            RaiseErrorHandler(new TransportErrorOccuredEventArgs(psrte, transportMethod));
        }

        #endregion
    }

    internal abstract class HyperVSocketClientSessionTransportManagerBase : ClientSessionTransportManagerBase
    {
        #region Data

        protected RemoteSessionHyperVSocketClient _client;

        private const string _threadName = "HyperVSocketTransport Reader Thread";

        #endregion

        #region Constructors

        internal HyperVSocketClientSessionTransportManagerBase(
            Guid runspaceId,
            PSRemotingCryptoHelper cryptoHelper)
            : base(runspaceId, cryptoHelper)
        { }

        #endregion

        #region Overrides

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (isDisposing)
            {
                _client?.Dispose();
            }
        }

        protected override void CleanupConnection()
        {
            _client.Close();
        }

        #endregion

        #region Protected Methods

        protected void StartReaderThread(
            StreamReader reader)
        {
            Thread readerThread = new Thread(ProcessReaderThread);
            readerThread.Name = _threadName;
            readerThread.IsBackground = true;
            readerThread.Start(reader);
        }

        protected void ProcessReaderThread(object state)
        {
            try
            {
                StreamReader reader = state as StreamReader;
                Dbg.Assert(reader != null, "Reader cannot be null.");

                // Send one fragment.
                SendOneItem();

                // Start reader loop.
                while (true)
                {
                    string data = reader.ReadLine();
                    if (data == null)
                    {
                        // End of stream indicates the target process was lost.
                        // Raise transport exception to invalidate the client remote runspace.
                        PSRemotingTransportException psrte = new PSRemotingTransportException(
                            PSRemotingErrorId.IPCServerProcessReportedError,
                            RemotingErrorIdStrings.IPCServerProcessReportedError,
                            RemotingErrorIdStrings.HyperVSocketTransportProcessEnded);
                        RaiseErrorHandler(new TransportErrorOccuredEventArgs(psrte, TransportMethodEnum.ReceiveShellOutputEx));
                        break;
                    }

                    if (data.StartsWith(System.Management.Automation.Remoting.Server.HyperVSocketErrorTextWriter.ErrorPrepend, StringComparison.OrdinalIgnoreCase))
                    {
                        // Error message from the server.
                        string errorData = data.Substring(System.Management.Automation.Remoting.Server.HyperVSocketErrorTextWriter.ErrorPrepend.Length);
                        HandleErrorDataReceived(errorData);
                    }
                    else
                    {
                        // Normal output data.
                        HandleOutputDataReceived(data);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Normal reader thread end.
            }
            catch (Exception e)
            {
                if (e is ArgumentOutOfRangeException)
                {
                    Dbg.Assert(false, "Need to adjust transport fragmentor to accommodate read buffer size.");
                }

                string errorMsg = e.Message ?? string.Empty;
                _tracer.WriteMessage("HyperVSocketClientSessionTransportManager", "StartReaderThread", Guid.Empty,
                    "Transport manager reader thread ended with error: {0}", errorMsg);

                PSRemotingTransportException psrte = new PSRemotingTransportException(
                    PSRemotingErrorId.IPCServerProcessReportedError,
                    RemotingErrorIdStrings.IPCServerProcessReportedError,
                    RemotingErrorIdStrings.HyperVSocketTransportProcessEnded);
                RaiseErrorHandler(new TransportErrorOccuredEventArgs(psrte, TransportMethodEnum.ReceiveShellOutputEx));
            }
        }

        #endregion
    }

    internal sealed class VMHyperVSocketClientSessionTransportManager : HyperVSocketClientSessionTransportManagerBase
    {
        #region Private Data

        private readonly Guid _vmGuid;
        private readonly string _configurationName;
        private readonly VMConnectionInfo _connectionInfo;
        private readonly NetworkCredential _networkCredential;

        #endregion

        #region Constructors

        internal VMHyperVSocketClientSessionTransportManager(
            VMConnectionInfo connectionInfo,
            Guid runspaceId,
            PSRemotingCryptoHelper cryptoHelper,
            Guid vmGuid,
            string configurationName)
            : base(runspaceId, cryptoHelper)
        {
            if (connectionInfo == null)
            {
                throw new PSArgumentNullException(nameof(connectionInfo));
            }

            _connectionInfo = connectionInfo;
            _vmGuid = vmGuid;
            _configurationName = configurationName;

            if (connectionInfo.Credential == null)
            {
                _networkCredential = CredentialCache.DefaultNetworkCredentials;
            }
            else
            {
                _networkCredential = connectionInfo.Credential.GetNetworkCredential();
            }
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Create a Hyper-V socket connection to the target process and set up
        /// transport reader/writer.
        /// </summary>
        public override void CreateAsync()
        {
            _client = new RemoteSessionHyperVSocketClient(_vmGuid, true);
            if (!_client.Connect(_networkCredential, _configurationName, true))
            {
                _client.Dispose();
                throw new PSInvalidOperationException(
                    PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.VMSessionConnectFailed),
                    null,
                    nameof(PSRemotingErrorId.VMSessionConnectFailed),
                    ErrorCategory.InvalidOperation,
                    null);
            }

            // TODO: remove below 3 lines when Hyper-V socket duplication is supported in .NET framework.
            _client.Dispose();
            _client = new RemoteSessionHyperVSocketClient(_vmGuid, false);
            if (!_client.Connect(_networkCredential, _configurationName, false))
            {
                _client.Dispose();
                throw new PSInvalidOperationException(
                    PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.VMSessionConnectFailed),
                    null,
                    nameof(PSRemotingErrorId.VMSessionConnectFailed),
                    ErrorCategory.InvalidOperation,
                    null);
            }

            // Create writer for Hyper-V socket.
            SetMessageWriter(_client.TextWriter);

            // Create reader thread for Hyper-V socket.
            StartReaderThread(_client.TextReader);
        }

        #endregion
    }

    internal sealed class ContainerHyperVSocketClientSessionTransportManager : HyperVSocketClientSessionTransportManagerBase
    {
        #region Private Data

        private readonly Guid _targetGuid; // currently this is the utility vm guid in HyperV container scenario
        private readonly ContainerConnectionInfo _connectionInfo;

        #endregion

        #region Constructors

        internal ContainerHyperVSocketClientSessionTransportManager(
            ContainerConnectionInfo connectionInfo,
            Guid runspaceId,
            PSRemotingCryptoHelper cryptoHelper,
            Guid targetGuid)
            : base(runspaceId, cryptoHelper)
        {
            if (connectionInfo == null)
            {
                throw new PSArgumentNullException(nameof(connectionInfo));
            }

            _connectionInfo = connectionInfo;
            _targetGuid = targetGuid;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Create a Hyper-V socket connection to the target process and set up
        /// transport reader/writer.
        /// </summary>
        public override void CreateAsync()
        {
            _client = new RemoteSessionHyperVSocketClient(_targetGuid, false, true);
            if (!_client.Connect(null, string.Empty, false))
            {
                _client.Dispose();
                throw new PSInvalidOperationException(
                    PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.ContainerSessionConnectFailed),
                    null,
                    nameof(PSRemotingErrorId.ContainerSessionConnectFailed),
                    ErrorCategory.InvalidOperation,
                    null);
            }

            // Create writer for Hyper-V socket.
            SetMessageWriter(_client.TextWriter);

            // Create reader thread for Hyper-V socket.
            StartReaderThread(_client.TextReader);
        }

        #endregion
    }

    internal sealed class SSHClientSessionTransportManager : ClientSessionTransportManagerBase
    {
        #region Data

        private readonly SSHConnectionInfo _connectionInfo;
        private int _sshProcessId;
        private StreamWriter _stdInWriter;
        private StreamReader _stdOutReader;
        private StreamReader _stdErrReader;
        private bool _connectionEstablished;
        private Timer _connectionTimer;

        private const string _threadName = "SSHTransport Reader Thread";

        #endregion

        #region Constructors

        internal SSHClientSessionTransportManager(
            SSHConnectionInfo connectionInfo,
            Guid runspaceId,
            PSRemotingCryptoHelper cryptoHelper)
            : base(runspaceId, cryptoHelper)
        {
            if (connectionInfo == null) { throw new PSArgumentException(message: null, nameof(connectionInfo)); }

            _connectionInfo = connectionInfo;
        }

        #endregion

        #region Overrides

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (isDisposing)
            {
                CloseConnection();
            }
        }

        protected override void CleanupConnection()
        {
            CloseConnection();
        }

        /// <summary>
        /// Create an SSH connection to the target host and set up
        /// transport reader/writer.
        /// </summary>
        public override void CreateAsync()
        {
            // Create the ssh client process with connection to host target.
            _sshProcessId = _connectionInfo.StartSSHProcess(
                out _stdInWriter,
                out _stdOutReader,
                out _stdErrReader);

            // Start error reader thread.
            StartErrorThread(_stdErrReader);

            // Create writer for named pipe.
            SetMessageWriter(_stdInWriter);

            // Create reader thread and send first PSRP message.
            StartReaderThread(_stdOutReader);

            if (_connectionInfo.ConnectingTimeout < 0)
            {
                return;
            }

            // Start connection timeout timer if requested.
            // Timer callback occurs only once after timeout time.
            _connectionTimer = new Timer(
                callback: (_) => 
                {
                    if (_connectionEstablished)
                    {
                        return;
                    }

                    // Detect if SSH client process terminates prematurely.
                    bool sshTerminated = false;
                    try
                    {
                        using (var sshProcess = System.Diagnostics.Process.GetProcessById(_sshProcessId))
                        {
                            sshTerminated = sshProcess == null || sshProcess.Handle == IntPtr.Zero || sshProcess.HasExited;
                        }
                    }
                    catch
                    {
                        sshTerminated = true;
                    }

                    var errorMessage = StringUtil.Format(RemotingErrorIdStrings.SSHClientConnectTimeout, _connectionInfo.ConnectingTimeout / 1000);
                    if (sshTerminated)
                    {
                        errorMessage += RemotingErrorIdStrings.SSHClientConnectProcessTerminated;
                    }

                    // Report error and terminate connection attempt.
                    HandleSSHError(
                        new PSRemotingTransportException(errorMessage));
                },
                state: null,
                dueTime: _connectionInfo.ConnectingTimeout,
                period: Timeout.Infinite);
        }

        public override void CloseAsync()
        {
            base.CloseAsync();

            if (!_connectionEstablished)
            {
                // If the connection is not yet estalished then clean up any existing connection state.
                CloseConnection();
            }
        }

        #endregion

        #region Private Methods

        private void CloseConnection()
        {
            // Ensure message queue is disposed.
            DisposeMessageQueue();

            var connectionTimer = Interlocked.Exchange(ref _connectionTimer, null);
            connectionTimer?.Dispose();

            var stdInWriter = Interlocked.Exchange(ref _stdInWriter, null);
            stdInWriter?.Dispose();

            var stdOutReader = Interlocked.Exchange(ref _stdOutReader, null);
            stdOutReader?.Dispose();

            var stdErrReader = Interlocked.Exchange(ref _stdErrReader, null);
            stdErrReader?.Dispose();

            // The CloseConnection() method can be called multiple times from multiple places.
            // Set the _sshProcessId to zero here so that we go through the work of finding
            // and terminating the SSH process just once.
            var sshProcessId = Interlocked.Exchange(ref _sshProcessId, 0);
            if (sshProcessId != 0)
            {
                try
                {
                    _connectionInfo.KillSSHProcess(sshProcessId);
                }
                catch (ArgumentException) { }
                catch (InvalidOperationException) { }
                catch (NotSupportedException) { }
                catch (System.ComponentModel.Win32Exception) { }
            }
        }

        private void StartErrorThread(
            StreamReader stdErrReader)
        {
            Thread errorThread = new Thread(ProcessErrorThread);
            errorThread.Name = "SSH Transport Error Thread";
            errorThread.IsBackground = true;
            errorThread.Start(stdErrReader);
        }

        private void ProcessErrorThread(object state)
        {
            try
            {
                StreamReader reader = state as StreamReader;
                Dbg.Assert(reader != null, "Reader cannot be null.");

                while (true)
                {
                    string error;

                    // Blocking read from StdError stream
                    error = reader.ReadLine();

                    if (error == null)
                    {
                        // Stream is closed unexpectedly.
                        throw new PSInvalidOperationException(RemotingErrorIdStrings.SSHAbruptlyTerminated);
                    }

                    if (error.Length == 0)
                    {
                        // Ignore
                        continue;
                    }

                    try
                    {
                        // Messages in error stream from ssh are unreliable, and may just be warnings or
                        // banner text.
                        // So just report the messages but don't act on them.
                        System.Console.WriteLine(error);
                    }
                    catch (IOException)
                    { }
                }
            }
            catch (ObjectDisposedException)
            {
                // Normal reader thread end.
            }
            catch (Exception e)
            {
                string errorMsg = e.Message ?? string.Empty;
                _tracer.WriteMessage("SSHClientSessionTransportManager", "ProcessErrorThread", Guid.Empty,
                    "Transport manager error thread ended with error: {0}", errorMsg);

                PSRemotingTransportException psrte = new PSRemotingTransportException(
                    StringUtil.Format(RemotingErrorIdStrings.SSHClientEndWithErrorMessage, errorMsg),
                    e);
                HandleSSHError(psrte);
            }
        }

        private void HandleSSHError(PSRemotingTransportException psrte)
        {
            RaiseErrorHandler(new TransportErrorOccuredEventArgs(psrte, TransportMethodEnum.CloseShellOperationEx));
            CloseConnection();
        }

        private void StartReaderThread(
            StreamReader reader)
        {
            Thread readerThread = new Thread(ProcessReaderThread);
            readerThread.Name = _threadName;
            readerThread.IsBackground = true;
            readerThread.Start(reader);
        }

        private void ProcessReaderThread(object state)
        {
            try
            {
                StreamReader reader = state as StreamReader;
                Dbg.Assert(reader != null, "Reader cannot be null.");

                // Send one fragment.
                SendOneItem();

                // Start reader loop.
                while (true)
                {
                    string data = reader.ReadLine();
                    if (data == null)
                    {
                        // End of stream indicates that the SSH transport is broken.
                        // SSH will return the appropriate error in StdErr stream so
                        // let the error reader thread report the error.
                        break;
                    }

                    if (data.StartsWith(System.Management.Automation.Remoting.Server.FormattedErrorTextWriter.ErrorPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        // Error message from the server.
                        string errorData = data.Substring(System.Management.Automation.Remoting.Server.FormattedErrorTextWriter.ErrorPrefix.Length);
                        HandleErrorDataReceived(errorData);
                    }
                    else
                    {
                        // The first received PSRP message from the server indicates that the connection is established and that PSRP is running.
                        if (!_connectionEstablished) { _connectionEstablished = true; }

                        // Normal output data.
                        HandleOutputDataReceived(data);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Normal reader thread end.
            }
            catch (Exception e)
            {
                if (e is ArgumentOutOfRangeException)
                {
                    Dbg.Assert(false, "Need to adjust transport fragmentor to accommodate read buffer size.");
                }

                string errorMsg = e.Message ?? string.Empty;
                _tracer.WriteMessage("SSHClientSessionTransportManager", "ProcessReaderThread", Guid.Empty,
                    "Transport manager reader thread ended with error: {0}", errorMsg);
            }
        }

        #endregion
    }

    internal abstract class NamedPipeClientSessionTransportManagerBase : ClientSessionTransportManagerBase
    {
        #region Data

        private readonly RunspaceConnectionInfo _connectionInfo;
        protected NamedPipeClientBase _clientPipe = new NamedPipeClientBase();
        private readonly string _threadName;

        #endregion

        #region Constructors

        internal NamedPipeClientSessionTransportManagerBase(
            RunspaceConnectionInfo connectionInfo,
            Guid runspaceId,
            PSRemotingCryptoHelper cryptoHelper,
            string threadName)
            : base(runspaceId, cryptoHelper)
        {
            if (connectionInfo == null)
            {
                throw new PSArgumentNullException(nameof(connectionInfo));
            }

            _connectionInfo = connectionInfo;
            _threadName = threadName;
            Fragmentor.FragmentSize = RemoteSessionNamedPipeServer.NamedPipeBufferSizeForRemoting;
        }

        #endregion

        #region Overrides

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (isDisposing)
            {
                _clientPipe?.Dispose();
            }
        }

        protected override void CleanupConnection()
        {
            _clientPipe.Close();
        }

        #endregion

        #region Protected Methods

        protected void StartReaderThread(
            StreamReader reader)
        {
            Thread readerThread = new Thread(ProcessReaderThread);
            readerThread.Name = _threadName;
            readerThread.IsBackground = true;
            readerThread.Start(reader);
        }

        #endregion

        #region Private Methods

        private void ProcessReaderThread(object state)
        {
            try
            {
                StreamReader reader = state as StreamReader;
                Dbg.Assert(reader != null, "Reader cannot be null.");

                // Send one fragment.
                SendOneItem();

                // Start reader loop.
                while (true)
                {
                    string data = reader.ReadLine();
                    if (data == null)
                    {
                        // End of stream indicates the target process was lost.
                        // Raise transport exception to invalidate the client remote runspace.
                        PSRemotingTransportException psrte = new PSRemotingTransportException(
                            PSRemotingErrorId.IPCServerProcessReportedError,
                            RemotingErrorIdStrings.IPCServerProcessReportedError,
                            RemotingErrorIdStrings.NamedPipeTransportProcessEnded);
                        RaiseErrorHandler(new TransportErrorOccuredEventArgs(psrte, TransportMethodEnum.ReceiveShellOutputEx));
                        break;
                    }

                    HandleDataReceived(data);
                }
            }
            catch (ObjectDisposedException)
            {
                // Normal reader thread end.
            }
            catch (Exception e)
            {
                if (e is ArgumentOutOfRangeException)
                {
                    Dbg.Assert(false, "Need to adjust transport fragmentor to accommodate read buffer size.");
                }

                string errorMsg = e.Message ?? string.Empty;
                _tracer.WriteMessage("NamedPipeClientSessionTransportManager", "StartReaderThread", Guid.Empty,
                    "Transport manager reader thread ended with error: {0}", errorMsg);
            }
        }

        #endregion
    }

    internal sealed class NamedPipeClientSessionTransportManager : NamedPipeClientSessionTransportManagerBase
    {
        #region Private Data

        private readonly NamedPipeConnectionInfo _connectionInfo;

        private const string _threadName = "NamedPipeTransport Reader Thread";

        #endregion

        #region Constructors

        internal NamedPipeClientSessionTransportManager(
            NamedPipeConnectionInfo connectionInfo,
            Guid runspaceId,
            PSRemotingCryptoHelper cryptoHelper)
            : base(connectionInfo, runspaceId, cryptoHelper, _threadName)
        {
            if (connectionInfo == null)
            {
                throw new PSArgumentNullException(nameof(connectionInfo));
            }

            _connectionInfo = connectionInfo;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Create a named pipe connection to the target process and set up
        /// transport reader/writer.
        /// </summary>
        public override void CreateAsync()
        {
            _clientPipe = string.IsNullOrEmpty(_connectionInfo.CustomPipeName) ?
                new RemoteSessionNamedPipeClient(_connectionInfo.ProcessId, _connectionInfo.AppDomainName) :
                new RemoteSessionNamedPipeClient(_connectionInfo.CustomPipeName);

            // Wait for named pipe to connect.
            _clientPipe.Connect(_connectionInfo.OpenTimeout);

            // Create writer for named pipe.
            SetMessageWriter(_clientPipe.TextWriter);

            // Create reader thread for named pipe.
            StartReaderThread(_clientPipe.TextReader);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Aborts an existing connection attempt.
        /// </summary>
        public void AbortConnect() => _clientPipe?.AbortConnect();

        #endregion
    }

    internal sealed class ContainerNamedPipeClientSessionTransportManager : NamedPipeClientSessionTransportManagerBase
    {
        #region Private Data

        private readonly ContainerConnectionInfo _connectionInfo;

        private const string _threadName = "ContainerNamedPipeTransport Reader Thread";

        #endregion

        #region Constructors

        internal ContainerNamedPipeClientSessionTransportManager(
            ContainerConnectionInfo connectionInfo,
            Guid runspaceId,
            PSRemotingCryptoHelper cryptoHelper)
            : base(connectionInfo, runspaceId, cryptoHelper, _threadName)
        {
            if (connectionInfo == null)
            {
                throw new PSArgumentNullException(nameof(connectionInfo));
            }

            _connectionInfo = connectionInfo;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Create a named pipe connection to the target process in target container and set up
        /// transport reader/writer.
        /// </summary>
        public override void CreateAsync()
        {
            _clientPipe = new ContainerSessionNamedPipeClient(
                _connectionInfo.ContainerProc.ProcessId,
                string.Empty, // AppDomainName
                _connectionInfo.ContainerProc.ContainerObRoot);

            // Wait for named pipe to connect.
            _clientPipe.Connect(_connectionInfo.OpenTimeout);

            // Create writer for named pipe.
            SetMessageWriter(_clientPipe.TextWriter);

            // Create reader thread for named pipe.
            StartReaderThread(_clientPipe.TextReader);
        }

        protected override void CleanupConnection()
        {
            _clientPipe.Close();

            //
            // We should terminate the PowerShell process inside container that
            // is created for PowerShell Direct.
            //
            if (!_connectionInfo.TerminateContainerProcess())
            {
                _tracer.WriteMessage("ContainerNamedPipeClientSessionTransportManager", "CleanupConnection", Guid.Empty,
                    "Failed to terminate PowerShell process inside container");
            }
        }

        #endregion
    }

    internal class OutOfProcessClientCommandTransportManager : BaseClientCommandTransportManager
    {
        #region Private Data

        private readonly OutOfProcessTextWriter _stdInWriter;
        private readonly PrioritySendDataCollection.OnDataAvailableCallback _onDataAvailableToSendCallback;
        private readonly Timer _signalTimeOutTimer;

        #endregion

        #region Constructors

        internal OutOfProcessClientCommandTransportManager(
            ClientRemotePowerShell cmd,
            bool noInput,
            ClientSessionTransportManagerBase sessnTM,
            OutOfProcessTextWriter stdInWriter) : base(cmd, sessnTM.CryptoHelper, sessnTM)
        {
            _stdInWriter = stdInWriter;
            _onDataAvailableToSendCallback =
                new PrioritySendDataCollection.OnDataAvailableCallback(OnDataAvailableCallback);
            _signalTimeOutTimer = new Timer(OnSignalTimeOutTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
        }

        #endregion

        #region Overrides

        internal override void ConnectAsync()
        {
            throw new NotImplementedException(RemotingErrorIdStrings.IPCTransportConnectError);
        }

        public override void CreateAsync()
        {
            PSEtwLog.LogAnalyticInformational(PSEventId.WSManCreateCommand, PSOpcode.Connect,
                                PSTask.CreateRunspace,
                                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                                RunspacePoolInstanceId.ToString(), powershellInstanceId.ToString());

            _stdInWriter.WriteLine(OutOfProcessUtils.CreateCommandPacket(powershellInstanceId));
        }

        public override void CloseAsync()
        {
            lock (syncObject)
            {
                if (isClosed)
                {
                    return;
                }

                // first change the state..so other threads
                // will know that we are closing.
                isClosed = true;
            }

            base.CloseAsync();

            PSEtwLog.LogAnalyticInformational(PSEventId.WSManCloseCommand,
                PSOpcode.Disconnect, PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                RunspacePoolInstanceId.ToString(), powershellInstanceId.ToString());

            // Send Close information to the server
            if (_stdInWriter != null)
            {
                try
                {
                    _stdInWriter.WriteLine(OutOfProcessUtils.CreateClosePacket(powershellInstanceId));
                }
                catch (IOException e)
                {
                    RaiseErrorHandler(
                        new TransportErrorOccuredEventArgs(
                            new PSRemotingTransportException(RemotingErrorIdStrings.NamedPipeTransportProcessEnded, e), TransportMethodEnum.CloseShellOperationEx));
                }
            }
        }

        internal override void SendStopSignal()
        {
            PSEtwLog.LogAnalyticInformational(PSEventId.WSManSignal,
                PSOpcode.Disconnect, PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                RunspacePoolInstanceId.ToString(), powershellInstanceId.ToString(), "stopsignal");
            // make sure we dont send anymore data from now onwards.
            base.CloseAsync();

            // Stop is equivalent to closing on the server..
            _stdInWriter.WriteLine(OutOfProcessUtils.CreateSignalPacket(powershellInstanceId));

            // start the timer..so client can fail deterministically
            // set the interval to 60 seconds.
            _signalTimeOutTimer.Change(60 * 1000, Timeout.Infinite);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            if (isDisposing)
            {
                StopSignalTimerAndDecrementOperations();
                _signalTimeOutTimer.Dispose();
            }
        }

        #endregion

        #region Helper Methods

        internal void OnCreateCmdCompleted()
        {
            PSEtwLog.LogAnalyticInformational(PSEventId.WSManCreateCommandCallbackReceived,
                PSOpcode.Connect, PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                RunspacePoolInstanceId.ToString(), powershellInstanceId.ToString());

            lock (syncObject)
            {
                // make sure the transport is not closed yet.
                if (isClosed)
                {
                    tracer.WriteLine("Client Session TM: Transport manager is closed. So returning");
                    return;
                }

                // Start sending data if any..and see if we can initiate a receive.
                SendOneItem();
            }
        }

        internal void OnRemoteCmdSendCompleted()
        {
            PSEtwLog.LogAnalyticInformational(PSEventId.WSManSendShellInputExCallbackReceived,
                PSOpcode.Connect, PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                RunspacePoolInstanceId.ToString(), powershellInstanceId.ToString());

            lock (syncObject)
            {
                // if the transport manager is already closed..return immediately
                if (isClosed)
                {
                    tracer.WriteLine("Client Command TM: Transport manager is closed. So returning");
                    return;
                }
            }

            SendOneItem();
        }

        internal void OnRemoteCmdDataReceived(byte[] rawData, string stream)
        {
            PSEtwLog.LogAnalyticInformational(
                    PSEventId.WSManReceiveShellOutputExCallbackReceived, PSOpcode.Receive, PSTask.None,
                    PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                    RunspacePoolInstanceId.ToString(),
                    powershellInstanceId.ToString(),
                    rawData.Length.ToString(CultureInfo.InvariantCulture));

            // if the transport manager is already closed..return immediately
            if (isClosed)
            {
                tracer.WriteLine("Client Command TM: Transport manager is closed. So returning");
                return;
            }

            ProcessRawData(rawData, stream);
        }

        internal void OnRemoteCmdSignalCompleted()
        {
            // log the callback received event.
            PSEtwLog.LogAnalyticInformational(PSEventId.WSManSignalCallbackReceived,
                PSOpcode.Disconnect, PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                RunspacePoolInstanceId.ToString(), powershellInstanceId.ToString());

            StopSignalTimerAndDecrementOperations();

            if (isClosed)
            {
                return;
            }

            // Call Signal completed from callback thread.
            EnqueueAndStartProcessingThread(null, null, true);
        }

        internal void OnSignalTimeOutTimerElapsed(object source)
        {
            // Signal timer is triggered only once

            if (isClosed)
            {
                return;
            }

            PSRemotingTransportException psrte = new PSRemotingTransportException(RemotingErrorIdStrings.IPCSignalTimedOut);
            RaiseErrorHandler(new TransportErrorOccuredEventArgs(psrte, TransportMethodEnum.ReceiveShellOutputEx));
        }

        private void StopSignalTimerAndDecrementOperations()
        {
            lock (syncObject)
            {
                _signalTimeOutTimer.Change(Timeout.Infinite, Timeout.Infinite);
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

        internal void OnCloseCmdCompleted()
        {
            PSEtwLog.LogAnalyticInformational(PSEventId.WSManCloseCommandCallbackReceived,
                PSOpcode.Disconnect, PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                RunspacePoolInstanceId.ToString(), powershellInstanceId.ToString());

            // Raising close completed only after receiving ACK from the server
            // otherwise may introduce race conditions on the server side
            RaiseCloseCompleted();
        }

        private void SendOneItem()
        {
            byte[] data = null;
            DataPriorityType priorityType = DataPriorityType.Default;
            // serializedPipeline is static ie., data is added to this collection at construction time only
            // and data is accessed by only one thread at any given time..so we can depend on this count
            if (serializedPipeline.Length > 0)
            {
                data = serializedPipeline.ReadOrRegisterCallback(null);
            }
            else
            {
                // This will either return data or register callback but doesn't do both.
                data = dataToBeSent.ReadOrRegisterCallback(_onDataAvailableToSendCallback, out priorityType);
            }

            if (data != null)
            {
                SendData(data, priorityType);
            }
        }

        private void SendData(byte[] data, DataPriorityType priorityType)
        {
            PSEtwLog.LogAnalyticInformational(
                    PSEventId.WSManSendShellInputEx, PSOpcode.Send, PSTask.None,
                    PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                    RunspacePoolInstanceId.ToString(),
                    powershellInstanceId.ToString(),
                    data.Length.ToString(CultureInfo.InvariantCulture));

            lock (syncObject)
            {
                if (isClosed)
                {
                    return;
                }

                _stdInWriter.WriteLine(OutOfProcessUtils.CreateDataPacket(data,
                    priorityType,
                    powershellInstanceId));
            }
        }

        private void OnDataAvailableCallback(byte[] data, DataPriorityType priorityType)
        {
            Dbg.Assert(data != null, "data cannot be null in the data available callback");

            tracer.WriteLine("Received data from dataToBeSent store.");
            SendData(data, priorityType);
        }

        #endregion
    }
}

namespace System.Management.Automation.Remoting.Server
{
    internal class OutOfProcessServerSessionTransportManager : AbstractServerSessionTransportManager
    {
        #region Private Data

        private readonly OutOfProcessTextWriter _stdOutWriter;
        private readonly OutOfProcessTextWriter _stdErrWriter;
        private readonly Dictionary<Guid, OutOfProcessServerTransportManager> _cmdTransportManagers;
        private readonly object _syncObject = new object();

        #endregion

        #region Constructors

        internal OutOfProcessServerSessionTransportManager(OutOfProcessTextWriter outWriter, OutOfProcessTextWriter errWriter, PSRemotingCryptoHelperServer cryptoHelper)
            : base(BaseTransportManager.DefaultFragmentSize, cryptoHelper)
        {
            Dbg.Assert(outWriter != null, "outWriter cannot be null.");
            Dbg.Assert(errWriter != null, "errWriter cannot be null.");
            _stdOutWriter = outWriter;
            _stdErrWriter = errWriter;
            _cmdTransportManagers = new Dictionary<Guid, OutOfProcessServerTransportManager>();

            this.WSManTransportErrorOccured += (object sender, TransportErrorOccuredEventArgs e) => 
            {
                string msg = e.Exception.TransportMessage ?? e.Exception.InnerException?.Message ?? string.Empty;
                _stdErrWriter.WriteLine(StringUtil.Format(RemotingErrorIdStrings.RemoteTransportError, msg));
            };
        }

        #endregion

        #region Overrides

        internal override void ProcessRawData(byte[] data, string stream)
        {
            base.ProcessRawData(data, stream);

            // Send ACK back to the client as we have processed data.
            _stdOutWriter.WriteLine(OutOfProcessUtils.CreateDataAckPacket(Guid.Empty));
        }

        internal override void Prepare()
        {
            throw new NotSupportedException();
        }

        protected override void SendDataToClient(byte[] data, bool flush, bool reportAsPending, bool reportAsDataBoundary)
        {
            _stdOutWriter.WriteLine(OutOfProcessUtils.CreateDataPacket(data,
                DataPriorityType.Default, Guid.Empty));
        }

        internal override void ReportExecutionStatusAsRunning()
        {
            // No-OP for outofProc TMs
        }

        internal void CreateCommandTransportManager(Guid powerShellCmdId)
        {
            OutOfProcessServerTransportManager cmdTM = new OutOfProcessServerTransportManager(_stdOutWriter, _stdErrWriter,
                powerShellCmdId, this.TypeTable, this.Fragmentor.FragmentSize, this.CryptoHelper);
            // this will make the Session's DataReady event handler handle
            // the commands data as well. This is because the state machine
            // is per session.
            cmdTM.MigrateDataReadyEventHandlers(this);

            lock (_syncObject)
            {
                // the dictionary is cleared by ServerPowershellDataStructure handler
                // once the clean up is complete for the transport manager
                Dbg.Assert(!_cmdTransportManagers.ContainsKey(powerShellCmdId), "key already exists");
                _cmdTransportManagers.Add(powerShellCmdId, cmdTM);
            }

            // send command ack..so that client can start sending data
            _stdOutWriter.WriteLine(OutOfProcessUtils.CreateCommandAckPacket(powerShellCmdId));
        }

        internal override AbstractServerTransportManager GetCommandTransportManager(Guid powerShellCmdId)
        {
            lock (_syncObject)
            {
                OutOfProcessServerTransportManager result = null;
                _cmdTransportManagers.TryGetValue(powerShellCmdId, out result);
                return result;
            }
        }

        internal override void RemoveCommandTransportManager(Guid powerShellCmdId)
        {
            lock (_syncObject)
            {
                _cmdTransportManagers.Remove(powerShellCmdId);
            }
        }

        internal override void Close(Exception reasonForClose)
        {
            RaiseClosingEvent();
        }

        #endregion
    }

    internal class OutOfProcessServerTransportManager : AbstractServerTransportManager
    {
        #region Private Data

        private readonly OutOfProcessTextWriter _stdOutWriter;
        private readonly OutOfProcessTextWriter _stdErrWriter;
        private readonly Guid _powershellInstanceId;
        private bool _isDataAckSendPending;

        #endregion

        #region Constructors

        internal OutOfProcessServerTransportManager(OutOfProcessTextWriter stdOutWriter, OutOfProcessTextWriter stdErrWriter,
            Guid powershellInstanceId,
            TypeTable typeTableToUse,
            int fragmentSize,
            PSRemotingCryptoHelper cryptoHelper)
            : base(fragmentSize, cryptoHelper)
        {
            _stdOutWriter = stdOutWriter;
            _stdErrWriter = stdErrWriter;
            _powershellInstanceId = powershellInstanceId;
            this.TypeTable = typeTableToUse;

            this.WSManTransportErrorOccured += HandleWSManTransportError;
        }

        #endregion

        #region Private Methods

        private void HandleWSManTransportError(object sender, TransportErrorOccuredEventArgs e)
        {
            _stdErrWriter.WriteLine(StringUtil.Format(RemotingErrorIdStrings.RemoteTransportError, e.Exception.TransportMessage));
        }

        #endregion

        #region Overrides

        internal override void ProcessRawData(byte[] data, string stream)
        {
            _isDataAckSendPending = true;
            base.ProcessRawData(data, stream);

            if (_isDataAckSendPending)
            {
                _isDataAckSendPending = false;
                // Send ACK back to the client as we have processed data.
                _stdOutWriter.WriteLine(OutOfProcessUtils.CreateDataAckPacket(_powershellInstanceId));
            }
        }

        internal override void ReportExecutionStatusAsRunning()
        {
            // No-OP for outofProc TMs
        }

        protected override void SendDataToClient(byte[] data, bool flush, bool reportAsPending, bool reportAsDataBoundary)
        {
            _stdOutWriter.WriteLine(OutOfProcessUtils.CreateDataPacket(data,
                DataPriorityType.Default, _powershellInstanceId));
        }

        internal override void Prepare()
        {
            if (_isDataAckSendPending)
            {
                _isDataAckSendPending = false;
                // let the base class prepare itself.
                base.Prepare();
                // Send ACK back to the client as we have processed data.
                _stdOutWriter.WriteLine(OutOfProcessUtils.CreateDataAckPacket(_powershellInstanceId));
            }
        }

        internal override void Close(Exception reasonForClose)
        {
            RaiseClosingEvent();
        }

        #endregion
    }
}
