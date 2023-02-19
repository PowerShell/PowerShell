// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Management.Automation.Remoting.Client;
using System.Management.Automation.Runspaces;
using System.Threading;

namespace Microsoft.PowerShell.CustomNamedPipeConnection
{
    #region NamedPipeClient

    /// <summary>
    /// This class is based on PowerShell core source code, and handles creating
    /// a client side named pipe object that can connect to a running PowerShell 
    /// process by its process Id.
    /// </summary>
    internal sealed class NamedPipeClient : IDisposable
    {
        #region Members

        private NamedPipeClientStream _clientPipeStream;
        private volatile bool _connecting;

        #endregion

        #region Properties

        /// <summary>
        /// Accessor for the named pipe reader.
        /// </summary>
        public StreamReader TextReader { get; private set; }

        /// <summary>
        /// Accessor for the named pipe writer.
        /// </summary>
        public StreamWriter TextWriter { get; private set; }

        /// <summary>
        /// Name of the pipe.
        /// </summary>
        public string PipeName { get; private set; }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            if (TextReader != null)
            {
                try { TextReader.Dispose(); }
                catch (ObjectDisposedException) { }

                TextReader = null;
            }

            if (TextWriter != null)
            {
                try { TextWriter.Dispose(); }
                catch (ObjectDisposedException) { }

                TextWriter = null;
            }

            if (_clientPipeStream != null)
            {
                try { _clientPipeStream.Dispose(); }
                catch (ObjectDisposedException) { }
            }
        }

        #endregion

        #region Constructors

        private NamedPipeClient()
        { }

        /// <summary>
        /// Constructor. Creates Named Pipe based on process Id.
        /// </summary>
        /// <param name="procId">Target process Id for pipe.</param>
        public NamedPipeClient(int procId)
        {
            PipeName = CreateProcessPipeName(
                System.Diagnostics.Process.GetProcessById(procId));
        }

        #endregion

        #region Static methods

        /// <summary>
        /// Create a pipe name based on process and appdomain name information.
        /// E.g., "PSHost.ProcessStartTime.ProcessId.DefaultAppDomain.ProcessName"
        /// </summary>
        /// <param name="proc">Process object.</param>
        /// <returns>Pipe name.</returns>
        private static string CreateProcessPipeName(System.Diagnostics.Process proc)
        {
            System.Text.StringBuilder pipeNameBuilder = new System.Text.StringBuilder();
            pipeNameBuilder.Append(@"PSHost.");

            string DefaultAppDomainName;
            if (OperatingSystem.IsWindows())
            {
                DefaultAppDomainName = "DefaultAppDomain";
                pipeNameBuilder.Append(proc.StartTime.ToFileTime().ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                DefaultAppDomainName = "None";
                pipeNameBuilder.Append(proc.StartTime.ToFileTime().ToString("X8").AsSpan(1, 8));
            }

            pipeNameBuilder.Append('.')
                .Append(proc.Id.ToString(CultureInfo.InvariantCulture))
                .Append('.')
                .Append(DefaultAppDomainName)
                .Append('.')
                .Append(proc.ProcessName);

            return pipeNameBuilder.ToString();
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Connect to named pipe server.  This is a blocking call until a
        /// connection occurs or the timeout time has elapsed.
        /// </summary>
        /// <param name="timeout">Connection attempt timeout in milliseconds.</param>
        public void Connect(
            int timeout)
        {
            // Uses Native API to connect to pipe and return NamedPipeClientStream object.
            _clientPipeStream = DoConnect(timeout);

            // Create reader/writer streams.
            TextReader = new StreamReader(_clientPipeStream);
            TextWriter = new StreamWriter(_clientPipeStream);
            TextWriter.AutoFlush = true;
        }

        /// <summary>
        /// Closes the named pipe.
        /// </summary>
        public void Close()
        {
            if (_clientPipeStream != null)
            {
                _clientPipeStream.Dispose();
            }
        }

        /// <summary>
        /// Abort connection attempt.
        /// </summary>
        public void AbortConnect()
        {
            _connecting = false;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Begin connection attempt.
        /// </summary>
        private NamedPipeClientStream DoConnect(int timeout)
        {
            // Repeatedly attempt connection to pipe until timeout expires.
            int startTime = Environment.TickCount;
            int elapsedTime = 0;
            _connecting = true;

            NamedPipeClientStream namedPipeClientStream = new NamedPipeClientStream(
                serverName: ".",
                pipeName: PipeName,
                direction: PipeDirection.InOut,
                options: PipeOptions.Asynchronous);

            namedPipeClientStream.ConnectAsync(timeout);

            do
            {
                if (!namedPipeClientStream.IsConnected)
                {
                    Thread.Sleep(100);
                    elapsedTime = unchecked(Environment.TickCount - startTime);
                    continue;
                }

                _connecting = false;
                return namedPipeClientStream;
            } while (_connecting && (elapsedTime < timeout));

            _connecting = false;

            throw new TimeoutException(@"Timeout expired before connection could be made to named pipe.");
        }

        #endregion
    }

    #endregion

    #region NamedPipeInfo

    internal sealed class NamedPipeInfo : RunspaceConnectionInfo
    {
        #region Fields

        private NamedPipeClient _clientPipe;
        private readonly string _computerName;

        #endregion

        #region Properties

        /// <summary>
        /// Process Id to attach to.
        /// </summary>
        public int ProcessId
        {
            get;
            set;
        }

        /// <summary>
        /// ConnectingTimeout in Milliseconds
        /// </summary>
        public int ConnectingTimeout
        {
            get;
            set;
        }

        #endregion

        #region Constructors

        private NamedPipeInfo()
        { }

        /// <summary>
        /// Construct instance.
        /// </summary>
        public NamedPipeInfo(
            int processId,
            int connectingTimeout)
        {
            ProcessId = processId;
            ConnectingTimeout = connectingTimeout;
            _computerName = $"LocalMachine:{ProcessId}";
            _clientPipe = new NamedPipeClient(ProcessId);
        }

        #endregion

        #region Overrides

        /// <summary>
        /// ComputerName
        /// </summary>
        public override string ComputerName
        {
            get { return _computerName; }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Credential
        /// </summary>
        public override PSCredential Credential
        {
            get { return null; }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// AuthenticationMechanism
        /// </summary>
        public override AuthenticationMechanism AuthenticationMechanism
        {
            get { return AuthenticationMechanism.Default; }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// CertificateThumbprint
        /// </summary>
        public override string CertificateThumbprint
        {
            get { return string.Empty; }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Create shallow copy of NamedPipeInfo object.
        /// </summary>
        public override RunspaceConnectionInfo Clone()
        {
            var connectionInfo = new NamedPipeInfo(ProcessId, ConnectingTimeout);
            connectionInfo._clientPipe = _clientPipe;
            return connectionInfo;
        }

        /// <summary>
        /// Create an instance of ClientSessionTransportManager.
        /// </summary>
        public override BaseClientSessionTransportManager CreateClientSessionTransportManager(
            Guid instanceId,
            string sessionName,
            PSRemotingCryptoHelper cryptoHelper)
        {
            return new NamedPipeClientSessionTransportMgr(
                connectionInfo: this,
                runspaceId: instanceId,
                cryptoHelper: cryptoHelper);
        }

        #endregion
    
        #region Public Methods

        /// <summary>
        /// Attempt to connect to process Id.
        /// If connection fails, is aborted, or times out, an exception is thrown.
        /// </summary>
        /// <param name="textWriter">Named pipe text stream writer.</param>
        /// <param name="textReader">Named pipe text stream reader.</param>
        /// <exception cref="TimeoutException">Connect attempt times out or is aborted.</exception>
        public void Connect(
            out StreamWriter textWriter,
            out StreamReader textReader)
        {
            // Wait for named pipe to connect.
            _clientPipe.Connect(
                timeout: ConnectingTimeout > -1 ? ConnectingTimeout : int.MaxValue);

            textWriter = _clientPipe.TextWriter;
            textReader = _clientPipe.TextReader;
        }

        /// <summary>
        /// Stops a connection attempt, or closes the connection that has been established.
        /// </summary>
        public void StopConnect()
        {
            _clientPipe?.AbortConnect();
            _clientPipe?.Close();
            _clientPipe?.Dispose();
        }

        #endregion
    }

    #endregion

    #region NamedPipeClientSessionTransportMgr

    internal sealed class NamedPipeClientSessionTransportMgr : ClientSessionTransportManagerBase
    {
        #region Fields

        private readonly NamedPipeInfo _connectionInfo;
        private const string _threadName = "NamedPipeCustomTransport Reader Thread";

        #endregion

        #region Constructor

        internal NamedPipeClientSessionTransportMgr(
            NamedPipeInfo connectionInfo,
            Guid runspaceId,
            PSRemotingCryptoHelper cryptoHelper)
            : base(runspaceId, cryptoHelper)
        {
            if (connectionInfo == null) { throw new PSArgumentException("connectionInfo"); }

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
            _connectionInfo.Connect(
                out StreamWriter pipeTextWriter,
                out StreamReader pipeTextReader);

            // Create writer for named pipe.
            SetMessageWriter(pipeTextWriter);

            // Create reader thread for named pipe.
            StartReaderThread(pipeTextReader);
        }

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

        #endregion
    
        #region Private Methods

        private void CloseConnection()
        {
            _connectionInfo.StopConnect();
        }

        private void HandleSSHError(PSRemotingTransportException ex)
        {
            RaiseErrorHandler(
                new TransportErrorOccurredEventArgs(
                    ex,
                    TransportMethodEnum.CloseShellOperationEx));

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

                    HandleDataReceived(data);
                }
            }
            catch (ObjectDisposedException)
            {
                // Normal reader thread end.
            }
            catch (Exception e)
            {
                string errorMsg = e.Message ?? string.Empty;
                HandleSSHError(new PSRemotingTransportException(
                    $"The SSH client session has ended reader thread with message: {errorMsg}"));
            }
        }

        #endregion
    }

    #endregion

    #region New-NamedPipeSession

    /// <summary>
    /// Attempts to connect to the specified host computer and returns
    /// a PSSession object representing the remote session.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "NamedPipeSession")]
    [OutputType(typeof(PSSession))]
    public sealed class NewNamedPipeSessionCommand : PSCmdlet
    {
        #region Fields

        private NamedPipeInfo _connectionInfo;
        private Runspace _runspace;
        private ManualResetEvent _openAsync;

        #endregion

        #region Parameters

        /// <summary>
        /// Name of host computer to connect to.
        /// </summary>
        [Parameter(Position=0, Mandatory=true)]
        [ValidateNotNullOrEmpty]
        public int ProcessId { get; set; }

        /// <summary>
        /// Optional value in seconds that limits the time allowed for a connection to be established.
        /// </summary>
        [Parameter]
        [ValidateRange(-1, 86400)]
        public int ConnectingTimeout { get; set; } = Timeout.Infinite;

        /// <summary>
        /// Optional name for the new PSSession.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        #endregion

        #region Overrides

        /// <summary>
        /// EndProcessing override.
        /// </summary>
        protected override void BeginProcessing()
        {
            // Convert ConnectingTimeout value from seconds to milliseconds.
            _connectionInfo = new NamedPipeInfo(
                processId: ProcessId,
                connectingTimeout: (ConnectingTimeout == Timeout.Infinite) ? Timeout.Infinite : ConnectingTimeout * 1000);

            _runspace = RunspaceFactory.CreateRunspace(
                connectionInfo: _connectionInfo,
                host: Host,
                typeTable: TypeTable.LoadDefaultTypeFiles(),
                applicationArguments: null,
                name: Name);
            
            _openAsync = new ManualResetEvent(false);
            _runspace.StateChanged += HandleRunspaceStateChanged;

            try
            {
                _runspace.OpenAsync();
                _openAsync.WaitOne();

                WriteObject(
                    PSSession.Create(
                        runspace: _runspace,
                        transportName: "PSNPTest",
                        psCmdlet: this));
            }
            finally
            {
                _openAsync.Dispose();
            }
        }

        /// <summary>
        /// StopProcessing override.
        /// </summary>
        protected override void StopProcessing()
        {
            _connectionInfo?.StopConnect();
        }

        #endregion

        #region Private methods

        private void HandleRunspaceStateChanged(
            object source,
            RunspaceStateEventArgs stateEventArgs)
        {
            switch (stateEventArgs.RunspaceStateInfo.State)
            {
                case RunspaceState.Opened:
                case RunspaceState.Closed:
                case RunspaceState.Broken:
                    _runspace.StateChanged -= HandleRunspaceStateChanged;
                    ReleaseWait();
                    break;
            }
        }

        private void ReleaseWait()
        {
            try
            {
                _openAsync?.Set();
            }
            catch (ObjectDisposedException)
            { }
        }

        #endregion
    }

    #endregion
}
