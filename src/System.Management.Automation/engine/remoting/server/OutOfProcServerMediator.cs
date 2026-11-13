// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Management.Automation.Internal;
using System.Management.Automation.Tracing;
using System.Threading;
#if !UNIX
using System.Security.Principal;
#endif

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting.Server
{
    internal abstract class OutOfProcessMediatorBase
    {
        #region Protected Data

        protected TextReader originalStdIn;
        protected OutOfProcessTextWriter originalStdOut;
        protected OutOfProcessTextWriter originalStdErr;
        protected OutOfProcessServerSessionTransportManager sessionTM;
        protected OutOfProcessUtils.DataProcessingDelegates callbacks;

        protected static object SyncObject = new object();
        protected object _syncObject = new object();
        protected string _initialCommand;
        protected ManualResetEvent allcmdsClosedEvent;

#if !UNIX
        // Thread impersonation.
        protected WindowsIdentity _windowsIdentityToImpersonate;
#endif

        /// <summary>
        /// Count of commands in progress.
        /// </summary>
        protected int _inProgressCommandsCount = 0;

        protected PowerShellTraceSource tracer = PowerShellTraceSourceFactory.GetTraceSource();

        protected bool _exitProcessOnError;

        #endregion

        #region Constructor

        protected OutOfProcessMediatorBase(bool exitProcessOnError)
        {
            _exitProcessOnError = exitProcessOnError;

            // Set up data handling callbacks.
            callbacks = new OutOfProcessUtils.DataProcessingDelegates();
            callbacks.DataPacketReceived += new OutOfProcessUtils.DataPacketReceived(OnDataPacketReceived);
            callbacks.DataAckPacketReceived += new OutOfProcessUtils.DataAckPacketReceived(OnDataAckPacketReceived);
            callbacks.CommandCreationPacketReceived += new OutOfProcessUtils.CommandCreationPacketReceived(OnCommandCreationPacketReceived);
            callbacks.CommandCreationAckReceived += new OutOfProcessUtils.CommandCreationAckReceived(OnCommandCreationAckReceived);
            callbacks.ClosePacketReceived += new OutOfProcessUtils.ClosePacketReceived(OnClosePacketReceived);
            callbacks.CloseAckPacketReceived += new OutOfProcessUtils.CloseAckPacketReceived(OnCloseAckPacketReceived);
            callbacks.SignalPacketReceived += new OutOfProcessUtils.SignalPacketReceived(OnSignalPacketReceived);
            callbacks.SignalAckPacketReceived += new OutOfProcessUtils.SignalAckPacketReceived(OnSignalAckPacketReceived);

            allcmdsClosedEvent = new ManualResetEvent(true);
        }

        #endregion

        #region Data Processing handlers

        protected void ProcessingThreadStart(object state)
        {
            try
            {
                string data = state as string;
                OutOfProcessUtils.ProcessData(data, callbacks);
            }
            catch (Exception e)
            {
                PSEtwLog.LogOperationalError(
                    PSEventId.TransportError,
                    PSOpcode.Open,
                    PSTask.None,
                    PSKeyword.UseAlwaysOperational,
                    Guid.Empty.ToString(),
                    Guid.Empty.ToString(),
                    OutOfProcessUtils.EXITCODE_UNHANDLED_EXCEPTION,
                    e.Message,
                    e.StackTrace);

                PSEtwLog.LogAnalyticError(
                    PSEventId.TransportError_Analytic,
                    PSOpcode.Open,
                    PSTask.None,
                    PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                    Guid.Empty.ToString(),
                    Guid.Empty.ToString(),
                    OutOfProcessUtils.EXITCODE_UNHANDLED_EXCEPTION,
                    e.Message,
                    e.StackTrace);

                // notify the remote client of any errors and fail gracefully
                if (_exitProcessOnError)
                {
                    originalStdErr.WriteLine(e.Message + e.StackTrace);
                    Environment.Exit(OutOfProcessUtils.EXITCODE_UNHANDLED_EXCEPTION);
                }
            }
        }

        protected void OnDataPacketReceived(byte[] rawData, string stream, Guid psGuid)
        {
            string streamTemp = System.Management.Automation.Remoting.Client.WSManNativeApi.WSMAN_STREAM_ID_STDIN;
            if (stream.Equals(nameof(DataPriorityType.PromptResponse), StringComparison.OrdinalIgnoreCase))
            {
                streamTemp = System.Management.Automation.Remoting.Client.WSManNativeApi.WSMAN_STREAM_ID_PROMPTRESPONSE;
            }

            if (psGuid == Guid.Empty)
            {
                lock (_syncObject)
                {
                    sessionTM.ProcessRawData(rawData, streamTemp);
                }
            }
            else
            {
                // this is for a command
                AbstractServerTransportManager cmdTM = null;

                lock (_syncObject)
                {
                    cmdTM = sessionTM.GetCommandTransportManager(psGuid);
                }

                if (cmdTM != null)
                {
                    // not throwing when there is no associated command as the command might have
                    // legitimately closed while the client is sending data. however the client
                    // should die after timeout as we are not sending an ACK back.
                    cmdTM.ProcessRawData(rawData, streamTemp);
                }
                else
                {
                    // There is no command transport manager to process the input data.
                    // However, we still need to acknowledge to the client that this input data
                    // was received.  This can happen with some cmdlets such as Select-Object -First
                    // where the cmdlet completes before all input data is received.
                    originalStdOut.WriteLine(OutOfProcessUtils.CreateDataAckPacket(psGuid));
                }
            }
        }

        protected void OnDataAckPacketReceived(Guid psGuid)
        {
            throw new PSRemotingTransportException(
                PSRemotingErrorId.IPCUnknownElementReceived,
                RemotingErrorIdStrings.IPCUnknownElementReceived,
                OutOfProcessUtils.PS_OUT_OF_PROC_DATA_ACK_TAG);
        }

        protected void OnCommandCreationPacketReceived(Guid psGuid)
        {
            lock (_syncObject)
            {
                sessionTM.CreateCommandTransportManager(psGuid);

                if (_inProgressCommandsCount == 0)
                    allcmdsClosedEvent.Reset();

                _inProgressCommandsCount++;

                tracer.WriteMessage("OutOfProcessMediator.OnCommandCreationPacketReceived, in progress command count : " + _inProgressCommandsCount + " psGuid : " + psGuid.ToString());
            }
        }

        protected void OnCommandCreationAckReceived(Guid psGuid)
        {
            throw new PSRemotingTransportException(PSRemotingErrorId.IPCUnknownElementReceived, RemotingErrorIdStrings.IPCUnknownElementReceived,
                OutOfProcessUtils.PS_OUT_OF_PROC_COMMAND_ACK_TAG);
        }

        protected void OnSignalPacketReceived(Guid psGuid)
        {
            if (psGuid == Guid.Empty)
            {
                throw new PSRemotingTransportException(PSRemotingErrorId.IPCNoSignalForSession, RemotingErrorIdStrings.IPCNoSignalForSession,
                    OutOfProcessUtils.PS_OUT_OF_PROC_SIGNAL_TAG);
            }
            else
            {
                // this is for a command
                AbstractServerTransportManager cmdTM = null;

                try
                {
                    lock (_syncObject)
                    {
                        cmdTM = sessionTM.GetCommandTransportManager(psGuid);
                    }

                    // dont throw if there is no cmdTM as it might have legitimately closed
                    cmdTM?.Close(null);
                }
                finally
                {
                    // Always send ack signal to avoid not responding in client.
                    originalStdOut.WriteLine(OutOfProcessUtils.CreateSignalAckPacket(psGuid));
                }
            }
        }

        protected void OnSignalAckPacketReceived(Guid psGuid)
        {
            throw new PSRemotingTransportException(PSRemotingErrorId.IPCUnknownElementReceived, RemotingErrorIdStrings.IPCUnknownElementReceived,
                OutOfProcessUtils.PS_OUT_OF_PROC_SIGNAL_ACK_TAG);
        }

        protected void OnClosePacketReceived(Guid psGuid)
        {
            PowerShellTraceSource tracer = PowerShellTraceSourceFactory.GetTraceSource();

            if (psGuid == Guid.Empty)
            {
                tracer.WriteMessage("BEGIN calling close on session transport manager");

                bool waitForAllcmdsClosedEvent = false;

                lock (_syncObject)
                {
                    if (_inProgressCommandsCount > 0)
                        waitForAllcmdsClosedEvent = true;
                }

                // Wait outside sync lock if required for all cmds to be closed
                //
                if (waitForAllcmdsClosedEvent)
                    allcmdsClosedEvent.WaitOne();

                lock (_syncObject)
                {
                    tracer.WriteMessage("OnClosePacketReceived, in progress commands count should be zero : " + _inProgressCommandsCount + ", psGuid : " + psGuid.ToString());

                    // it appears that when closing PowerShell ISE, therefore closing OutOfProcServerMediator, there are 2 Close command requests
                    // changing PSRP/IPC at this point is too risky, therefore protecting about this duplication
                    sessionTM?.Close(null);

                    tracer.WriteMessage("END calling close on session transport manager");
                    sessionTM = null;
                }
            }
            else
            {
                tracer.WriteMessage("Closing command with GUID " + psGuid.ToString());

                // this is for a command
                AbstractServerTransportManager cmdTM = null;

                lock (_syncObject)
                {
                    cmdTM = sessionTM.GetCommandTransportManager(psGuid);
                }

                // dont throw if there is no cmdTM as it might have legitimately closed
                cmdTM?.Close(null);

                lock (_syncObject)
                {
                    tracer.WriteMessage("OnClosePacketReceived, in progress commands count should be greater than zero : " + _inProgressCommandsCount + ", psGuid : " + psGuid.ToString());

                    _inProgressCommandsCount--;

                    if (_inProgressCommandsCount == 0)
                        allcmdsClosedEvent.Set();
                }
            }

            // send close ack
            originalStdOut.WriteLine(OutOfProcessUtils.CreateCloseAckPacket(psGuid));
        }

        protected void OnCloseAckPacketReceived(Guid psGuid)
        {
            throw new PSRemotingTransportException(PSRemotingErrorId.IPCUnknownElementReceived, RemotingErrorIdStrings.IPCUnknownElementReceived,
                OutOfProcessUtils.PS_OUT_OF_PROC_CLOSE_ACK_TAG);
        }

        #endregion

        #region Methods

        protected OutOfProcessServerSessionTransportManager CreateSessionTransportManager(
            string configurationName,
            string configurationFile,
            PSRemotingCryptoHelperServer cryptoHelper,
            string workingDirectory)
        {
            PSSenderInfo senderInfo;
#if !UNIX
            WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();
            PSPrincipal userPrincipal = new PSPrincipal(
                new PSIdentity(string.Empty, true, currentIdentity.Name, null),
                currentIdentity);
            senderInfo = new PSSenderInfo(userPrincipal, "http://localhost");
#else
            PSPrincipal userPrincipal = new PSPrincipal(
                new PSIdentity(string.Empty, true, string.Empty, null),
                null);
            senderInfo = new PSSenderInfo(userPrincipal, "http://localhost");
#endif

            var tm = new OutOfProcessServerSessionTransportManager(
                originalStdOut,
                originalStdErr,
                cryptoHelper);

            ServerRemoteSession.CreateServerRemoteSession(
                senderInfo: senderInfo,
                configurationProviderId: "Microsoft.PowerShell",
                initializationParameters: string.Empty,
                transportManager: tm,
                initialCommand: _initialCommand,
                configurationName: configurationName,
                configurationFile: configurationFile,
                initialLocation: workingDirectory);

            return tm;
        }

        protected void Start(
            string initialCommand,
            PSRemotingCryptoHelperServer cryptoHelper,
            string workingDirectory,
            string configurationName,
            string configurationFile)
        {
            _initialCommand = initialCommand;

            sessionTM = CreateSessionTransportManager(
                configurationName: configurationName,
                configurationFile: configurationFile,
                cryptoHelper: cryptoHelper,
                workingDirectory: workingDirectory);

            try
            {
                while (true)
                {
                    string data = originalStdIn.ReadLine();
                    lock (_syncObject)
                    {
                        sessionTM ??= CreateSessionTransportManager(
                            configurationName: configurationName,
                            configurationFile: configurationFile,
                            cryptoHelper: cryptoHelper,
                            workingDirectory: workingDirectory);
                    }

                    if (string.IsNullOrEmpty(data))
                    {
                        lock (_syncObject)
                        {
                            // give a chance to runspace/pipelines to close (as it looks like the client died
                            // intermittently)
                            sessionTM.Close(null);
                            sessionTM = null;
                        }

                        throw new PSRemotingTransportException(
                            PSRemotingErrorId.IPCUnknownElementReceived,
                            RemotingErrorIdStrings.IPCUnknownElementReceived,
                            string.Empty);
                    }

                    // process data in a thread pool thread..this way Runspace, Command
                    // data can be processed concurrently.
#if !UNIX
                    Utils.QueueWorkItemWithImpersonation(
                            _windowsIdentityToImpersonate,
                            new WaitCallback(ProcessingThreadStart),
                            data);
#else
                    ThreadPool.QueueUserWorkItem(new WaitCallback(ProcessingThreadStart), data);
#endif
                }
            }
            catch (Exception e)
            {
                PSEtwLog.LogOperationalError(
                    PSEventId.TransportError,
                    PSOpcode.Open,
                    PSTask.None,
                    PSKeyword.UseAlwaysOperational,
                    Guid.Empty.ToString(),
                    Guid.Empty.ToString(),
                    OutOfProcessUtils.EXITCODE_UNHANDLED_EXCEPTION,
                    e.Message,
                    e.StackTrace);

                PSEtwLog.LogAnalyticError(
                    PSEventId.TransportError_Analytic,
                    PSOpcode.Open,
                    PSTask.None,
                    PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                    Guid.Empty.ToString(),
                    Guid.Empty.ToString(),
                    OutOfProcessUtils.EXITCODE_UNHANDLED_EXCEPTION,
                    e.Message,
                    e.StackTrace);

                if (_exitProcessOnError)
                {
                    // notify the remote client of any errors and fail gracefully
                    originalStdErr.WriteLine(e.Message);

                    Environment.Exit(OutOfProcessUtils.EXITCODE_UNHANDLED_EXCEPTION);
                }
            }
        }

        #endregion
    }

    internal sealed class StdIOProcessMediator : OutOfProcessMediatorBase
    {
        #region Private Data

        private static StdIOProcessMediator s_singletonInstance;

        #endregion

        #region Constructors

        /// <summary>
        /// The mediator will take actions from the StdIn stream and responds to them.
        /// It will replace StdIn,StdOut and StdErr stream with TextWriter.Null. This is
        /// to make sure these streams are totally used by our Mediator.
        /// </summary>
        /// <param name="combineErrOutStream">Redirects remoting errors to the Out stream.</param>
        private StdIOProcessMediator(bool combineErrOutStream) : base(exitProcessOnError: true)
        {
            // Create input stream reader from Console standard input stream.
            // We don't use the provided Console.In TextReader because it can have
            // an incorrect encoding, e.g., Hyper-V Container console where the
            // TextReader has incorrect default console encoding instead of the actual
            // stream encoding.  This way the stream encoding is determined by the
            // stream BOM as needed.
            originalStdIn = new StreamReader(Console.OpenStandardInput(), true);

            // Remoting errors can optionally be written to stdErr or stdOut with
            // special formatting.
            originalStdOut = new OutOfProcessTextWriter(Console.Out);
            if (combineErrOutStream)
            {
                originalStdErr = new FormattedErrorTextWriter(Console.Out);
            }
            else
            {
                originalStdErr = new OutOfProcessTextWriter(Console.Error);
            }

            // Replacing StdIn, StdOut, StdErr with Null so that no other app messes with the
            // original streams.
            Console.SetIn(TextReader.Null);
            Console.SetOut(TextWriter.Null);
            Console.SetError(TextWriter.Null);
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Starts the out-of-process powershell server instance.
        /// </summary>
        /// <param name="initialCommand">Specifies the initialization script.</param>
        /// <param name="workingDirectory">Specifies the initial working directory. The working directory is set before the initial command.</param>
        /// <param name="configurationName">Specifies an optional configuration name that configures the endpoint session.</param>
        /// <param name="configurationFile">Specifies an optional path to a configuration (.pssc) file for the session.</param>
        /// <param name="combineErrOutStream">Specifies the option to write remoting errors to stdOut stream, with special formatting.</param>
        internal static void Run(
            string initialCommand,
            string workingDirectory,
            string configurationName,
            string configurationFile,
            bool combineErrOutStream)
        {
            lock (SyncObject)
            {
                if (s_singletonInstance != null)
                {
                    Dbg.Assert(false, "Run should not be called multiple times");
                    return;
                }

                s_singletonInstance = new StdIOProcessMediator(combineErrOutStream);
            }

            s_singletonInstance.Start(
                initialCommand: initialCommand,
                cryptoHelper: new PSRemotingCryptoHelperServer(),
                workingDirectory: workingDirectory,
                configurationName: configurationName,
                configurationFile: configurationFile);
        }

        #endregion
    }

    internal sealed class NamedPipeProcessMediator : OutOfProcessMediatorBase
    {
        #region Private Data

        private static NamedPipeProcessMediator s_singletonInstance;

        private readonly RemoteSessionNamedPipeServer _namedPipeServer;

        #endregion

        #region Properties

        internal bool IsDisposed
        {
            get { return _namedPipeServer.IsDisposed; }
        }

        #endregion

        #region Constructors

        private NamedPipeProcessMediator() : base(false) { }

        private NamedPipeProcessMediator(
            RemoteSessionNamedPipeServer namedPipeServer) : base(false)
        {
            if (namedPipeServer == null)
            {
                throw new PSArgumentNullException(nameof(namedPipeServer));
            }

            _namedPipeServer = namedPipeServer;

            // Create transport reader/writers from named pipe.
            originalStdIn = namedPipeServer.TextReader;
            originalStdOut = new OutOfProcessTextWriter(namedPipeServer.TextWriter);
            originalStdErr = new FormattedErrorTextWriter(namedPipeServer.TextWriter);

#if !UNIX
            // Flow impersonation as needed.
            Utils.TryGetWindowsImpersonatedIdentity(out _windowsIdentityToImpersonate);
#endif
        }

        #endregion

        #region Static Methods

        internal static void Run(
            string initialCommand,
            RemoteSessionNamedPipeServer namedPipeServer)
        {
            lock (SyncObject)
            {
                if (s_singletonInstance != null && !s_singletonInstance.IsDisposed)
                {
                    Dbg.Assert(false, "Run should not be called multiple times, unless the singleton was disposed.");
                    return;
                }

                s_singletonInstance = new NamedPipeProcessMediator(namedPipeServer);
            }

            s_singletonInstance.Start(
                initialCommand: initialCommand,
                cryptoHelper: new PSRemotingCryptoHelperServer(),
                workingDirectory: null,
                configurationName: namedPipeServer.ConfigurationName,
                configurationFile: null);
        }

        #endregion
    }

    internal sealed class FormattedErrorTextWriter : OutOfProcessTextWriter
    {
        #region Constructors

        internal FormattedErrorTextWriter(
            TextWriter textWriter) : base(textWriter)
        { }

        #endregion

        #region Base class overrides

        // Write error data to stream with 'ErrorPrefix' prefix that will
        // be interpreted by the client.
        public override void WriteLine(string data)
        {
            string dataToWrite = (data != null) ? ErrorPrefix + data : null;
            base.WriteLine(dataToWrite);
        }

        #endregion
    }

    internal sealed class HyperVSocketMediator : OutOfProcessMediatorBase
    {
        #region Private Data

        private static HyperVSocketMediator s_instance;

        private readonly RemoteSessionHyperVSocketServer _hypervSocketServer;

        #endregion

        #region Properties

        internal bool IsDisposed
        {
            get { return _hypervSocketServer.IsDisposed; }
        }

        #endregion

        #region Constructors

        private HyperVSocketMediator()
            : base(false)
        {
            _hypervSocketServer = new RemoteSessionHyperVSocketServer(false);

            originalStdIn = _hypervSocketServer.TextReader;
            originalStdOut = new OutOfProcessTextWriter(_hypervSocketServer.TextWriter);
            originalStdErr = new HyperVSocketErrorTextWriter(_hypervSocketServer.TextWriter);
        }

        private HyperVSocketMediator(string token,
            DateTimeOffset tokenCreationTime)
            : base(false)
        {
            _hypervSocketServer = new RemoteSessionHyperVSocketServer(false, token: token, tokenCreationTime: tokenCreationTime);

            originalStdIn = _hypervSocketServer.TextReader;
            originalStdOut = new OutOfProcessTextWriter(_hypervSocketServer.TextWriter);
            originalStdErr = new HyperVSocketErrorTextWriter(_hypervSocketServer.TextWriter);
        }
        #endregion

        #region Static Methods

        internal static void Run(
            string initialCommand,
            string configurationName)
        {
            lock (SyncObject)
            {
                s_instance = new HyperVSocketMediator();
            }

            s_instance.Start(
                initialCommand: initialCommand,
                cryptoHelper: new PSRemotingCryptoHelperServer(),
                workingDirectory: null,
                configurationName: configurationName,
                configurationFile: null);
        }

        internal static void Run(
            string initialCommand,
            string configurationName,
            string token,
            DateTimeOffset tokenCreationTime)
        {
            lock (SyncObject)
            {
                s_instance = new HyperVSocketMediator(token, tokenCreationTime);
            }

            s_instance.Start(
                initialCommand: initialCommand,
                cryptoHelper: new PSRemotingCryptoHelperServer(),
                workingDirectory: null,
                configurationName: configurationName,
                configurationFile: null);
        }
        #endregion
    }

    internal sealed class HyperVSocketErrorTextWriter : OutOfProcessTextWriter
    {
        #region Private Members

        private const string _errorPrepend = "__HyperVSocketError__:";

        #endregion

        #region Properties

        internal static string ErrorPrepend
        {
            get { return _errorPrepend; }
        }

        #endregion

        #region Constructors

        internal HyperVSocketErrorTextWriter(
            TextWriter textWriter)
            : base(textWriter)
        { }

        #endregion

        #region Base class overrides

        public override void WriteLine(string data)
        {
            string dataToWrite = (data != null) ? _errorPrepend + data : null;
            base.WriteLine(dataToWrite);
        }

        #endregion
    }
}
