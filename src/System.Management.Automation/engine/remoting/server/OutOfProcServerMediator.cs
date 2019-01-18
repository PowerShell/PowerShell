// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Management.Automation.Internal;
using System.Management.Automation.Tracing;
using System.Threading;
#if !UNIX
using System.Security.Principal;
#endif
using Microsoft.Win32.SafeHandles;

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
#if !CORECLR
                // CurrentUICulture is not available in Thread Class in CSS
                // WinBlue: 621775. Thread culture is not properly set
                // for local background jobs causing experience differences
                // between local console and local background jobs.
                Thread.CurrentThread.CurrentUICulture = Microsoft.PowerShell.NativeCultureResolver.UICulture;
                Thread.CurrentThread.CurrentCulture = Microsoft.PowerShell.NativeCultureResolver.Culture;
#endif
                string data = state as string;
                OutOfProcessUtils.ProcessData(data, callbacks);
            }
            catch (Exception e)
            {
                PSEtwLog.LogOperationalError(
                    PSEventId.TransportError, PSOpcode.Open, PSTask.None,
                    PSKeyword.UseAlwaysOperational,
                    Guid.Empty.ToString(),
                    Guid.Empty.ToString(),
                    OutOfProcessUtils.EXITCODE_UNHANDLED_EXCEPTION,
                    e.Message,
                    e.StackTrace);

                PSEtwLog.LogAnalyticError(
                    PSEventId.TransportError_Analytic, PSOpcode.Open, PSTask.None,
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
            if (stream.Equals(DataPriorityType.PromptResponse.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                streamTemp = System.Management.Automation.Remoting.Client.WSManNativeApi.WSMAN_STREAM_ID_PROMPTRESPONSE;
            }

            if (Guid.Empty == psGuid)
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
            throw new PSRemotingTransportException(PSRemotingErrorId.IPCUnknownElementReceived, RemotingErrorIdStrings.IPCUnknownElementReceived,
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
                    if (cmdTM != null)
                    {
                        cmdTM.Close(null);
                    }
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

                    if (sessionTM != null)
                    {
                        // it appears that when closing PowerShell ISE, therefore closing OutOfProcServerMediator, there are 2 Close command requests
                        // changing PSRP/IPC at this point is too risky, therefore protecting about this duplication
                        sessionTM.Close(null);
                    }

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
                if (cmdTM != null)
                {
                    cmdTM.Close(null);
                }

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

        protected OutOfProcessServerSessionTransportManager CreateSessionTransportManager(string configurationName, PSRemotingCryptoHelperServer cryptoHelper)
        {
            PSSenderInfo senderInfo;
#if !UNIX
            WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();
            PSPrincipal userPrincipal = new PSPrincipal(new PSIdentity(string.Empty, true, currentIdentity.Name, null),
                currentIdentity);
            senderInfo = new PSSenderInfo(userPrincipal, "http://localhost");
#else
            PSPrincipal userPrincipal = new PSPrincipal(new PSIdentity(string.Empty, true, string.Empty, null),
                null);
            senderInfo = new PSSenderInfo(userPrincipal, "http://localhost");
#endif

            OutOfProcessServerSessionTransportManager tm = new OutOfProcessServerSessionTransportManager(originalStdOut, originalStdErr, cryptoHelper);

            ServerRemoteSession srvrRemoteSession = ServerRemoteSession.CreateServerRemoteSession(senderInfo,
                _initialCommand, tm, configurationName);

            return tm;
        }

        protected void Start(string initialCommand, PSRemotingCryptoHelperServer cryptoHelper, string configurationName = null)
        {
            _initialCommand = initialCommand;

            sessionTM = CreateSessionTransportManager(configurationName, cryptoHelper);

            try
            {
                do
                {
                    string data = originalStdIn.ReadLine();
                    lock (_syncObject)
                    {
                        if (sessionTM == null)
                        {
                            sessionTM = CreateSessionTransportManager(configurationName, cryptoHelper);
                        }
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

                        throw new PSRemotingTransportException(PSRemotingErrorId.IPCUnknownElementReceived,
                            RemotingErrorIdStrings.IPCUnknownElementReceived, string.Empty);
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
                } while (true);
            }
            catch (Exception e)
            {
                PSEtwLog.LogOperationalError(
                    PSEventId.TransportError, PSOpcode.Open, PSTask.None,
                    PSKeyword.UseAlwaysOperational,
                    Guid.Empty.ToString(),
                    Guid.Empty.ToString(),
                    OutOfProcessUtils.EXITCODE_UNHANDLED_EXCEPTION,
                    e.Message,
                    e.StackTrace);

                PSEtwLog.LogAnalyticError(
                    PSEventId.TransportError_Analytic, PSOpcode.Open, PSTask.None,
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

        #region Static Methods

        internal static void AppDomainUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            // args can never be null.
            Exception exception = (Exception)args.ExceptionObject;
            // log the exception to crimson event logs
            PSEtwLog.LogOperationalError(PSEventId.AppDomainUnhandledException,
                PSOpcode.Close, PSTask.None,
                PSKeyword.UseAlwaysOperational,
                exception.GetType().ToString(), exception.Message,
                exception.StackTrace);

            PSEtwLog.LogAnalyticError(PSEventId.AppDomainUnhandledException_Analytic,
                    PSOpcode.Close, PSTask.None,
                    PSKeyword.ManagedPlugin | PSKeyword.UseAlwaysAnalytic,
                    exception.GetType().ToString(), exception.Message,
                    exception.StackTrace);
        }

        #endregion
    }

    internal sealed class OutOfProcessMediator : OutOfProcessMediatorBase
    {
        #region Private Data

        private static OutOfProcessMediator s_singletonInstance;

        #endregion

        #region Constructors

        /// <summary>
        /// The mediator will take actions from the StdIn stream and responds to them.
        /// It will replace StdIn,StdOut and StdErr stream with TextWriter.Null's. This is
        /// to make sure these streams are totally used by our Mediator.
        /// </summary>
        private OutOfProcessMediator() : base(true)
        {
            // Create input stream reader from Console standard input stream.
            // We don't use the provided Console.In TextReader because it can have
            // an incorrect encoding, e.g., Hyper-V Container console where the
            // TextReader has incorrect default console encoding instead of the actual
            // stream encoding.  This way the stream encoding is determined by the
            // stream BOM as needed.
            originalStdIn = new StreamReader(Console.OpenStandardInput(), true);

            // replacing StdIn with Null so that no other app messes with the
            // original stream.
            Console.SetIn(TextReader.Null);

            // replacing StdOut with Null so that no other app messes with the
            // original stream
            originalStdOut = new OutOfProcessTextWriter(Console.Out);
            Console.SetOut(TextWriter.Null);

            // replacing StdErr with Null so that no other app messes with the
            // original stream
            originalStdErr = new OutOfProcessTextWriter(Console.Error);
            Console.SetError(TextWriter.Null);
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// </summary>
        internal static void Run(string initialCommand)
        {
            lock (SyncObject)
            {
                if (s_singletonInstance != null)
                {
                    Dbg.Assert(false, "Run should not be called multiple times");
                    return;
                }

                s_singletonInstance = new OutOfProcessMediator();
            }

#if !CORECLR // AppDomain is not available in CoreCLR
            // Setup unhandled exception to log events
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(AppDomainUnhandledException);
#endif
            s_singletonInstance.Start(initialCommand, new PSRemotingCryptoHelperServer());
        }

        #endregion
    }

    internal sealed class SSHProcessMediator : OutOfProcessMediatorBase
    {
        #region Private Data

        private static SSHProcessMediator s_singletonInstance;

        #endregion

        #region Constructors

        private SSHProcessMediator() : base(true)
        {
#if !UNIX
            var inputHandle = PlatformInvokes.GetStdHandle((uint)PlatformInvokes.StandardHandleId.Input);
            originalStdIn = new StreamReader(
                new FileStream(new SafeFileHandle(inputHandle, false), FileAccess.Read));

            var outputHandle = PlatformInvokes.GetStdHandle((uint)PlatformInvokes.StandardHandleId.Output);
            originalStdOut = new OutOfProcessTextWriter(
                new StreamWriter(
                    new FileStream(new SafeFileHandle(outputHandle, false), FileAccess.Write)));

            var errorHandle = PlatformInvokes.GetStdHandle((uint)PlatformInvokes.StandardHandleId.Error);
            originalStdErr = new OutOfProcessTextWriter(
                new StreamWriter(
                    new FileStream(new SafeFileHandle(errorHandle, false), FileAccess.Write)));
#else
            originalStdIn = new StreamReader(Console.OpenStandardInput(), true);
            originalStdOut = new OutOfProcessTextWriter(
                new StreamWriter(Console.OpenStandardOutput()));
            originalStdErr = new OutOfProcessTextWriter(
                new StreamWriter(Console.OpenStandardError()));
#endif
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// </summary>
        /// <param name="initialCommand"></param>
        internal static void Run(string initialCommand)
        {
            lock (SyncObject)
            {
                if (s_singletonInstance != null)
                {
                    Dbg.Assert(false, "Run should not be called multiple times");
                    return;
                }

                s_singletonInstance = new SSHProcessMediator();
            }

            PSRemotingCryptoHelperServer cryptoHelper;
#if !UNIX
            cryptoHelper = new PSRemotingCryptoHelperServer();
#else
            cryptoHelper = null;
#endif

            s_singletonInstance.Start(initialCommand, cryptoHelper);
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
                throw new PSArgumentNullException("namedPipeServer");
            }

            _namedPipeServer = namedPipeServer;

            // Create transport reader/writers from named pipe.
            originalStdIn = namedPipeServer.TextReader;
            originalStdOut = new OutOfProcessTextWriter(namedPipeServer.TextWriter);
            originalStdErr = new NamedPipeErrorTextWriter(namedPipeServer.TextWriter);

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

#if !CORECLR
            // AppDomain is not available in CoreCLR
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(AppDomainUnhandledException);
#endif
            s_singletonInstance.Start(initialCommand, new PSRemotingCryptoHelperServer(), namedPipeServer.ConfigurationName);
        }

        #endregion
    }

    internal sealed class NamedPipeErrorTextWriter : OutOfProcessTextWriter
    {
        #region Private Members

        private const string _errorPrepend = "__NamedPipeError__:";

        #endregion

        #region Properties

        internal static string ErrorPrepend
        {
            get { return _errorPrepend; }
        }

        #endregion

        #region Constructors

        internal NamedPipeErrorTextWriter(
            TextWriter textWriter) : base(textWriter)
        { }

        #endregion

        #region Base class overrides

        internal override void WriteLine(string data)
        {
            string dataToWrite = (data != null) ? _errorPrepend + data : null;
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

#if !CORECLR
            // AppDomain is not available in CoreCLR
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(AppDomainUnhandledException);
#endif

            s_instance.Start(initialCommand, new PSRemotingCryptoHelperServer(), configurationName);
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

        internal override void WriteLine(string data)
        {
            string dataToWrite = (data != null) ? _errorPrepend + data : null;
            base.WriteLine(dataToWrite);
        }

        #endregion
    }
}
