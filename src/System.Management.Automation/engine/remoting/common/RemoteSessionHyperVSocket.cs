// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Management.Automation.Tracing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Buffers;

using Dbg = System.Diagnostics.Debug;
using SMA = System.Management.Automation;

namespace System.Management.Automation.Remoting
{
    [SerializableAttribute]
    internal class HyperVSocketEndPoint : EndPoint
    {
        #region Members

        private readonly System.Net.Sockets.AddressFamily _addressFamily;
        private Guid _vmId;
        private Guid _serviceId;

        public const System.Net.Sockets.AddressFamily AF_HYPERV = (System.Net.Sockets.AddressFamily)34;
        public const int HYPERV_SOCK_ADDR_SIZE = 36;

        #endregion

        #region Constructor

        public HyperVSocketEndPoint(System.Net.Sockets.AddressFamily AddrFamily,
                                   Guid VmId,
                                   Guid ServiceId)
        {
            _addressFamily = AddrFamily;
            _vmId = VmId;
            _serviceId = ServiceId;
        }

        public override System.Net.Sockets.AddressFamily AddressFamily
        {
            get { return _addressFamily; }
        }

        public Guid VmId
        {
            get { return _vmId; }

            set { _vmId = value; }
        }

        public Guid ServiceId
        {
            get { return _serviceId; }

            set { _serviceId = value; }
        }

        #endregion

        #region Overrides

        public override EndPoint Create(SocketAddress SockAddr)
        {
            if (SockAddr == null ||
                SockAddr.Family != AF_HYPERV ||
                SockAddr.Size != 34)
            {
                return null;
            }

            HyperVSocketEndPoint endpoint = new HyperVSocketEndPoint(SockAddr.Family, Guid.Empty, Guid.Empty);

            string sockAddress = SockAddr.ToString();

            endpoint.VmId = new Guid(sockAddress.Substring(4, 16));
            endpoint.ServiceId = new Guid(sockAddress.Substring(20, 16));

            return endpoint;
        }

        public override bool Equals(object obj)
        {
            HyperVSocketEndPoint endpoint = (HyperVSocketEndPoint)obj;

            if (endpoint == null)
            {
                return false;
            }

            if ((_addressFamily == endpoint.AddressFamily) &&
                (_vmId == endpoint.VmId) &&
                (_serviceId == endpoint.ServiceId))
            {
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Serialize().GetHashCode();
        }

        public override SocketAddress Serialize()
        {
            SocketAddress sockAddress = new SocketAddress((System.Net.Sockets.AddressFamily)_addressFamily, HYPERV_SOCK_ADDR_SIZE);

            byte[] vmId = _vmId.ToByteArray();
            byte[] serviceId = _serviceId.ToByteArray();

            sockAddress[2] = (byte)0;

            for (int i = 0; i < vmId.Length; i++)
            {
                sockAddress[i + 4] = vmId[i];
            }

            for (int i = 0; i < serviceId.Length; i++)
            {
                sockAddress[i + 4 + vmId.Length] = serviceId[i];
            }

            return sockAddress;
        }

        public override string ToString()
        {
            return _vmId.ToString() + _serviceId.ToString();
        }

        #endregion
    }

    internal sealed class RemoteSessionHyperVSocketServer : IDisposable
    {
        #region Members

        private readonly object _syncObject;
        private readonly PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();

        // This is to prevent persistent replay attacks.
        // it is not meant to ensure all replay attacks are impossible.
        private const int MAX_TOKEN_LIFE_MINUTES = 10;

        #endregion

        #region Properties

        /// <summary>
        /// Returns the Hyper-V socket object.
        /// </summary>
        public Socket HyperVSocket { get; }

        /// <summary>
        /// Returns the network stream object.
        /// </summary>
        public NetworkStream Stream { get; }

        /// <summary>
        /// Accessor for the Hyper-V socket reader.
        /// </summary>
        public StreamReader TextReader { get; private set; }

        /// <summary>
        /// Accessor for the Hyper-V socket writer.
        /// </summary>
        public StreamWriter TextWriter { get; private set; }

        /// <summary>
        /// Returns true if object is currently disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        #endregion

        #region Constructors

        public RemoteSessionHyperVSocketServer(bool LoopbackMode)
        {
            _syncObject = new object();

            Exception ex = null;

            try
            {
                Guid serviceId = new Guid("a5201c21-2770-4c11-a68e-f182edb29220"); // HV_GUID_VM_SESSION_SERVICE_ID_2
                Guid loopbackId = new Guid("e0e16197-dd56-4a10-9195-5ee7a155a838"); // HV_GUID_LOOPBACK
                Guid parentId = new Guid("a42e7cda-d03f-480c-9cc2-a4de20abb878"); // HV_GUID_PARENT
                Guid vmId = LoopbackMode ? loopbackId : parentId;
                HyperVSocketEndPoint endpoint = new HyperVSocketEndPoint(HyperVSocketEndPoint.AF_HYPERV, vmId, serviceId);

                Socket listenSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, (System.Net.Sockets.ProtocolType)1);
                listenSocket.Bind(endpoint);

                listenSocket.Listen(1);
                HyperVSocket = listenSocket.Accept();

                Stream = new NetworkStream(HyperVSocket, true);

                // Create reader/writer streams.
                TextReader = new StreamReader(Stream);
                TextWriter = new StreamWriter(Stream);
                TextWriter.AutoFlush = true;

                //
                // listenSocket is not closed when it goes out of scope here. Sometimes it is
                // closed later in this thread, while other times it is not closed at all. This will
                // cause problem when we set up a second PowerShell Direct session. Let's
                // explicitly close listenSocket here for safe.
                //
                if (listenSocket != null)
                {
                    try { listenSocket.Dispose(); }
                    catch (ObjectDisposedException) { }
                }
            }
            catch (Exception e)
            {
                ex = e;
            }

            if (ex != null)
            {
                Dbg.Fail("Unexpected error in RemoteSessionHyperVSocketServer.");

                // Unexpected error.
                string errorMessage = !string.IsNullOrEmpty(ex.Message) ? ex.Message : string.Empty;
                _tracer.WriteMessage("RemoteSessionHyperVSocketServer", "RemoteSessionHyperVSocketServer", Guid.Empty,
                    "Unexpected error in constructor: {0}", errorMessage);

                throw new PSInvalidOperationException(
                    PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.RemoteSessionHyperVSocketServerConstructorFailure),
                    ex,
                    nameof(PSRemotingErrorId.RemoteSessionHyperVSocketServerConstructorFailure),
                    ErrorCategory.InvalidOperation,
                    null);
            }
        }

        public RemoteSessionHyperVSocketServer(bool LoopbackMode, string token, DateTimeOffset tokenCreationTime)
        {
            _syncObject = new object();

            Exception ex = null;

            try
            {
                Guid serviceId = new Guid("a5201c21-2770-4c11-a68e-f182edb29220"); // HV_GUID_VM_SESSION_SERVICE_ID_2
                HyperVSocketEndPoint endpoint = new HyperVSocketEndPoint(HyperVSocketEndPoint.AF_HYPERV, Guid.Empty, serviceId);

                Socket listenSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, (System.Net.Sockets.ProtocolType)1);
                listenSocket.Bind(endpoint);

                listenSocket.Listen(1);
                HyperVSocket = listenSocket.Accept();

                ValidateToken(HyperVSocket, token, tokenCreationTime, MAX_TOKEN_LIFE_MINUTES * 60);

                Stream = new NetworkStream(HyperVSocket, true);

                // Create reader/writer streams.
                TextReader = new StreamReader(Stream);
                TextWriter = new StreamWriter(Stream);
                TextWriter.AutoFlush = true;

                //
                // listenSocket is not closed when it goes out of scope here. Sometimes it is
                // closed later in this thread, while other times it is not closed at all. This will
                // cause problem when we set up a second PowerShell Direct session. Let's
                // explicitly close listenSocket here for safe.
                //
                if (listenSocket != null)
                {
                    try
                    {
                        listenSocket.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            }
            catch (Exception e)
            {
                ex = e;
            }

            if (ex != null)
            {
                Dbg.Fail("Unexpected error in RemoteSessionHyperVSocketServer.");

                // Unexpected error.
                string errorMessage = !string.IsNullOrEmpty(ex.Message) ? ex.Message : string.Empty;
                _tracer.WriteMessage(
                    "RemoteSessionHyperVSocketServer",
                    "RemoteSessionHyperVSocketServer",
                    Guid.Empty,
                    "Unexpected error in constructor: {0}",
                    errorMessage);

                throw new PSInvalidOperationException(
                    PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.RemoteSessionHyperVSocketServerConstructorFailure),
                    ex,
                    nameof(PSRemotingErrorId.RemoteSessionHyperVSocketServerConstructorFailure),
                    ErrorCategory.InvalidOperation,
                    null);
            }
        }
        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            lock (_syncObject)
            {
                if (IsDisposed)
                {
                    return;
                }

                IsDisposed = true;
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

            if (Stream != null)
            {
                try { Stream.Dispose(); }
                catch (ObjectDisposedException) { }
            }

            if (HyperVSocket != null)
            {
                try { HyperVSocket.Dispose(); }
                catch (ObjectDisposedException) { }
            }
        }

        #endregion

        /// <summary>
        /// Validates the token received from the client over the HyperVSocket.
        /// Throws PSDirectException if the token is invalid or not received in time.
        /// </summary>
        /// <param name="socket">The connected HyperVSocket.</param>
        /// <param name="token">The expected token string.</param>
        /// <param name="tokenCreationTime">The creation time of the token.</param>
        /// <param name="maxTokenLifeSeconds">The maximum lifetime of the token in seconds.</param>
        internal static void ValidateToken(Socket socket, string token, DateTimeOffset tokenCreationTime, int maxTokenLifeSeconds)
        {
            TimeSpan timeout = TimeSpan.FromSeconds(maxTokenLifeSeconds);
            DateTimeOffset timeoutExpiry = tokenCreationTime.Add(timeout);
            DateTimeOffset now = DateTimeOffset.UtcNow;

            // Calculate remaining time and create cancellation token
            TimeSpan remainingTime = timeoutExpiry - now;

            // Check if the token has already expired
            if (remainingTime <= TimeSpan.Zero)
            {
                throw new PSDirectException(
                    PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.InvalidCredential, "Token has expired"));
            }

            // Create a cancellation token that will be cancelled when the timeout expires
            using var cancellationTokenSource = new CancellationTokenSource(remainingTime);
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Set socket timeout for receive operations to prevent indefinite blocking
            int timeoutMs = (int)remainingTime.TotalMilliseconds;
            socket.ReceiveTimeout = timeoutMs;
            socket.SendTimeout = timeoutMs;

            // Check for cancellation before starting validation
            cancellationToken.ThrowIfCancellationRequested();

            // We should move to this pattern and
            // in the tests I found I needed to get a bigger buffer than the token length
            // and test length of the received data similar to this pattern.
            string responseString = RemoteSessionHyperVSocketClient.ReceiveResponse(socket, RemoteSessionHyperVSocketClient.VERSION_REQUEST.Length + 4);
            if (string.IsNullOrEmpty(responseString) || responseString.Length != RemoteSessionHyperVSocketClient.VERSION_REQUEST.Length)
            {
                socket.Send("FAIL"u8);
                throw new PSDirectException(
                    PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.HyperVInvalidResponse, "Client", "Version Request: " + responseString));
            }

            cancellationToken.ThrowIfCancellationRequested();

            socket.Send(Encoding.UTF8.GetBytes(RemoteSessionHyperVSocketClient.CLIENT_VERSION));
            responseString = RemoteSessionHyperVSocketClient.ReceiveResponse(socket, RemoteSessionHyperVSocketClient.CLIENT_VERSION.Length + 4);

            // In the future we may need to handle different versions, differently.
            // For now, we are just checking that we exchanged versions correctly.
            if (string.IsNullOrEmpty(responseString) || !responseString.StartsWith(RemoteSessionHyperVSocketClient.VERSION_PREFIX, StringComparison.Ordinal))
            {
                socket.Send("FAIL"u8);
                throw new PSDirectException(
                    PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.HyperVInvalidResponse, "Client", "Version Response: " + responseString));
            }

            cancellationToken.ThrowIfCancellationRequested();

            socket.Send("PASS"u8);

            // The client should send the token in the format TOKEN <token>
            // the token should be up to 256 bits, which is less than 50 characters.
            // I'll double that to 100 characters to be safe, plus the "TOKEN " prefix.
            // So we expect a response of length 6 + 100 = 106 characters.
            responseString = RemoteSessionHyperVSocketClient.ReceiveResponse(socket, 110);

            // Final check if we got the token before the timeout
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(responseString) || !responseString.StartsWith("TOKEN ", StringComparison.Ordinal))
            {
                socket.Send("FAIL"u8);
                // If the response is not in the expected format, we throw an exception.
                // This is a failure to authenticate the client.
                // don't send this response for risk of information disclosure.
                throw new PSDirectException(
                    PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.HyperVInvalidResponse, "Client", "Token Response"));
            }

            // Extract the token from the response.
            string responseToken = responseString.Substring(6).Trim();

            if (!string.Equals(responseToken, token, StringComparison.Ordinal))
            {
                socket.Send("FAIL"u8);
                throw new PSDirectException(
                    PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.InvalidCredential));
            }

            // Acknowledge the token is valid with "PASS".
            socket.Send("PASS"u8);

            socket.ReceiveTimeout = 0; // Disable the timeout after successful validation
            socket.SendTimeout = 0;
        }
    }

    internal sealed class RemoteSessionHyperVSocketClient : IDisposable
    {
        #region Members

        private readonly object _syncObject;

        #region tracer
        /// <summary>
        /// An instance of the PSTraceSource class used for trace output.
        /// </summary>
        [SMA.TraceSource("RemoteSessionHyperVSocketClient", "Class that has PowerShell Direct Client implementation")]
        private static readonly PSTraceSource s_tracer = PSTraceSource.GetTracer("RemoteSessionHyperVSocketClient", "Class that has PowerShell Direct Client implementation");

        #endregion tracer

        private static readonly ManualResetEvent s_connectDone =
                new ManualResetEvent(false);

        #endregion

        #region constants in hvsocket.h

        public const int HV_PROTOCOL_RAW = 1;
        public const int HVSOCKET_CONTAINER_PASSTHRU = 2;

        #endregion

        #region version constants

        internal const string VERSION_REQUEST = "VERSION";
        internal const string CLIENT_VERSION = "VERSION_2";
        internal const string VERSION_PREFIX = "VERSION_";

        #endregion

        #region Properties

        /// <summary>
        /// Returns the Hyper-V socket endpoint object.
        /// </summary>
        public HyperVSocketEndPoint EndPoint { get; }

        /// <summary>
        /// Returns the Hyper-V socket object.
        /// </summary>
        public Socket HyperVSocket { get; private set; }

        /// <summary>
        /// Returns the network stream object.
        /// </summary>
        public NetworkStream Stream { get; private set; }

        /// <summary>
        /// Accessor for the Hyper-V socket reader.
        /// </summary>
        public StreamReader TextReader { get; private set; }

        /// <summary>
        /// Accessor for the Hyper-V socket writer.
        /// </summary>
        public StreamWriter TextWriter { get; private set; }

        /// <summary>
        /// True if the client is a Hyper-V container.
        /// </summary>
        public bool IsContainer { get; }

        /// <summary>
        /// True if the client is using backwards compatible mode.
        /// This is used to determine if the client should use
        /// the backwards compatible or not.
        /// In modern mode, the vmicvmsession service will
        /// hand off the socket to the PowerShell process
        /// inside the VM automatically.
        /// In backwards compatible mode, the vmicvmsession
        /// service create a new socket to the PowerShell process
        /// inside the VM.
        /// </summary>
        public bool UseBackwardsCompatibleMode { get; private set; }

        /// <summary>
        /// The authentication token used for the session.
        /// This token is provided by the broker and provided to the server to authenticate the server session.
        /// This protocol uses two connections:
        /// 1. The first is to the broker or vmicvmsession service to exchange credentials and configuration.
        ///    The broker will respond with an authentication token.  The broker also launches a PowerShell
        ///    server process with the authentication token.
        /// 2. The second is to the server process, that was launched by the broker,
        ///    inside the VM, which uses the authentication token to verify that the client is the same client
        ///    that connected to the broker.
        /// </summary>
        public string AuthenticationToken { get; private set; }

        /// <summary>
        /// Returns true if object is currently disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        #endregion

        #region Constructors

        internal RemoteSessionHyperVSocketClient(
            Guid vmId,
            bool isFirstConnection,
            bool useBackwardsCompatibleMode = false,
            bool isContainer = false,
            string authenticationToken = null)
        {
            Guid serviceId;

            _syncObject = new object();

            if (isFirstConnection)
            {
                // HV_GUID_VM_SESSION_SERVICE_ID
                serviceId = new Guid("999e53d4-3d5c-4c3e-8779-bed06ec056e1");
            }
            else
            {
                // HV_GUID_VM_SESSION_SERVICE_ID_2
                serviceId = new Guid("a5201c21-2770-4c11-a68e-f182edb29220");
            }

            EndPoint = new HyperVSocketEndPoint(HyperVSocketEndPoint.AF_HYPERV, vmId, serviceId);

            IsContainer = isContainer;

            UseBackwardsCompatibleMode = useBackwardsCompatibleMode;

            if (!isFirstConnection && !useBackwardsCompatibleMode && !string.IsNullOrEmpty(authenticationToken))
            {
                // If this is not the first connection and we are using backwards compatible mode,
                // we should not set the authentication token here.
                // The authentication token will be set during the Connect method.
                AuthenticationToken = authenticationToken;
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            lock (_syncObject)
            {
                if (IsDisposed)
                {
                    return;
                }

                IsDisposed = true;
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

            if (Stream != null)
            {
                try { Stream.Dispose(); }
                catch (ObjectDisposedException) { }
            }

            if (HyperVSocket != null)
            {
                try { HyperVSocket.Dispose(); }
                catch (ObjectDisposedException) { }
            }
        }

        #endregion

        #region Public Methods

        private void ShutdownSocket()
        {
            if (HyperVSocket != null)
            {
                // Ensure the socket is disposed properly.
                try
                {
                    s_tracer.WriteLine("ShutdownSocket: Disposing of the HyperVSocket.");
                    HyperVSocket.Dispose();
                }
                catch (Exception ex)
                {
                    s_tracer.WriteLine("ShutdownSocket: Exception while disposing the socket: {0}", ex.Message);
                }
            }

            // Dispose of the existing stream if it exists.
            if (Stream != null)
            {
                try
                {
                    Stream.Dispose();
                }
                catch (Exception ex)
                {
                    s_tracer.WriteLine("ShutdownSocket: Exception while disposing the stream: {0}", ex.Message);
                }
            }
        }

        /// <summary>
        /// Recreates the HyperVSocket and connects it to the endpoint, updating the Stream if successful.
        /// </summary>
        private bool ConnectSocket()
        {
            HyperVSocket = new Socket(EndPoint.AddressFamily, SocketType.Stream, (System.Net.Sockets.ProtocolType)1);

            //
            // We need to call SetSocketOption() in order to set up Hyper-V socket connection between container host and Hyper-V container.
            // Here is the scenario: the Hyper-V container is inside a utility vm, which is inside the container host
            //
            if (IsContainer)
            {
                var value = new byte[sizeof(uint)];
                value[0] = 1;

                try
                {
                    HyperVSocket.SetSocketOption(
                        (System.Net.Sockets.SocketOptionLevel)HV_PROTOCOL_RAW,
                        (System.Net.Sockets.SocketOptionName)HVSOCKET_CONTAINER_PASSTHRU,
                        value);
                }
                catch
                {
                    throw new PSDirectException(
                        PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.RemoteSessionHyperVSocketClientConstructorSetSocketOptionFailure));
                }
            }

            s_tracer.WriteLine("Connect: Client connecting, to {0}; isContainer: {1}.", EndPoint.ServiceId.ToString(), IsContainer);
            HyperVSocket.Connect(EndPoint);

            // Check if the socket is connected.
            // If it is connected, create a NetworkStream.
            if (HyperVSocket.Connected)
            {
                s_tracer.WriteLine("Connect: Client connected, to {0}; isContainer: {1}.", EndPoint.ServiceId.ToString(), IsContainer);
                Stream = new NetworkStream(HyperVSocket, true);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Connect to Hyper-V socket server.  This is a blocking call until a
        /// connection occurs or the timeout time has elapsed.
        /// </summary>
        /// <param name="networkCredential">The credential used for authentication.</param>
        /// <param name="configurationName">The configuration name of the PS session.</param>
        /// <param name="isFirstConnection">Whether this is the first connection.</param>
        public bool Connect(
            NetworkCredential networkCredential,
            string configurationName,
            bool isFirstConnection)
        {
            bool result = false;

            //
            // Check invalid input and throw exception before setting up socket connection.
            // This check is done only in VM case.
            //
            if (isFirstConnection)
            {
                if (string.IsNullOrEmpty(networkCredential.UserName))
                {
                    throw new PSDirectException(
                        PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.InvalidUsername));
                }
            }

            if (ConnectSocket())
            {
                if (isFirstConnection)
                {
                    var exchangeResult = ExchangeCredentialsAndConfiguration(networkCredential, configurationName, HyperVSocket, this.UseBackwardsCompatibleMode);
                    if (!exchangeResult.success)
                    {
                        // We will not block here for a container because a container does not have a broker.
                        if (IsRequirePsDirectAuthenticationEnabled(@"SOFTWARE\\Microsoft\\PowerShell", Microsoft.Win32.RegistryHive.LocalMachine))
                        {
                            s_tracer.WriteLine("ExchangeCredentialsAndConfiguration: RequirePsDirectAuthentication is enabled, requiring latest transport version.");
                            throw new PSDirectException(
                                PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.HyperVNegotiationFailed));
                        }

                        this.UseBackwardsCompatibleMode = true;
                        s_tracer.WriteLine("ExchangeCredentialsAndConfiguration: Using backwards compatible mode.");

                        // If the first connection fails in modern mode, fall back to backwards compatible mode.
                        ShutdownSocket(); // will terminate the broker
                        ConnectSocket();  // restart the broker
                        exchangeResult = ExchangeCredentialsAndConfiguration(networkCredential, configurationName, HyperVSocket, this.UseBackwardsCompatibleMode);
                        if (!exchangeResult.success)
                        {
                            s_tracer.WriteLine("ExchangeCredentialsAndConfiguration: Failed to exchange credentials and configuration in backwards compatible mode.");
                            throw new PSDirectException(
                                PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.HyperVInvalidResponse, "Broker", "Credential"));
                        }
                    }
                    else
                    {
                        this.AuthenticationToken = exchangeResult.authenticationToken;
                    }
                }

                if (!isFirstConnection)
                {
                    if (!this.UseBackwardsCompatibleMode)
                    {
                        s_tracer.WriteLine("Connect-Server: Performing transport version and token exchange for Hyper-V socket.  isFirstConnection: {0}, UseBackwardsCompatibleMode: {1}", isFirstConnection, this.UseBackwardsCompatibleMode);
                        RemoteSessionHyperVSocketClient.PerformTransportVersionAndTokenExchange(HyperVSocket, this.AuthenticationToken);
                    }
                    else
                    {
                        s_tracer.WriteLine("Connect-Server: Skipping transport version and token exchange for backwards compatible mode.");
                    }
                }

                TextReader = new StreamReader(Stream);
                TextWriter = new StreamWriter(Stream);
                TextWriter.AutoFlush = true;

                result = true;
            }
            else
            {
                s_tracer.WriteLine("Connect: Client unable to connect.");

                result = false;
            }

            return result;
        }

        /// <summary>
        /// Performs the transport version and token exchange sequence for the Hyper-V socket connection.
        /// Throws PSDirectException on failure.
        /// </summary>
        /// <param name="socket">The socket to use for communication.</param>
        /// <param name="authenticationToken">The authentication token to send.</param>
        public static void PerformTransportVersionAndTokenExchange(Socket socket, string authenticationToken)
        {
            if (string.IsNullOrEmpty(authenticationToken))
            {
                s_tracer.WriteLine("PerformTransportVersionAndTokenExchange: Authentication token is null or empty. Aborting transport version and token exchange.");
                throw new PSDirectException(
                    PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.InvalidCredential));
            }

            socket.Send(Encoding.UTF8.GetBytes(VERSION_REQUEST));
            string responseStr = ReceiveResponse(socket, 16);

            // Check if the response starts with the expected version prefix.
            // We will rely on the broker to determine if the two can communicate.
            // At least, for now.
            if (!responseStr.StartsWith(VERSION_PREFIX, StringComparison.Ordinal))
            {
                s_tracer.WriteLine("PerformTransportVersionAndTokenExchange: Server responded with an invalid response of {0}. Notifying the transport manager to downgrade if allowed.", responseStr);
                throw new PSDirectException(
                    PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.HyperVInvalidResponse, "Server", "TransportVersion"));
            }

            socket.Send(Encoding.UTF8.GetBytes(CLIENT_VERSION));
            string response = ReceiveResponse(socket, 4); // either "PASS" or "FAIL"

            if (!string.Equals(response, "PASS", StringComparison.Ordinal))
            {
                s_tracer.WriteLine(
                    "PerformTransportVersionAndTokenExchange: Transport version negotiation with server failed. Response: {0}", response);
                throw new PSDirectException(
                    PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.HyperVInvalidResponse, "Server", "TransportVersion"));
            }

            byte[] tokenBytes = Encoding.UTF8.GetBytes("TOKEN " + authenticationToken);
            socket.Send(tokenBytes);

            // This is the opportunity for the server to tell the client to go away.
            string tokenResponse = ReceiveResponse(socket, 256); // either "PASS" or "FAIL", but get a little more buffer to allow for better error in the future
            if (!string.Equals(tokenResponse, "PASS", StringComparison.Ordinal))
            {
                s_tracer.WriteLine(
                    "PerformTransportVersionAndTokenExchange: Server Authentication Token exchange failed. Response: {0}", tokenResponse);
                throw new PSDirectException(
                    PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.InvalidCredential));
            }
        }

        /// <summary>
        /// Checks if the registry key RequirePsDirectAuthentication is set to 1.
        /// Returns true if fallback should be aborted.
        /// Uses the 64-bit registry view on 64-bit systems to ensure consistent behavior regardless of process architecture.
        /// On 32-bit systems, uses the default registry view since there is no WOW64 redirection.
        /// </summary>
        internal static bool IsRequirePsDirectAuthenticationEnabled(string keyPath, Microsoft.Win32.RegistryHive registryHive)
        {
            const string regValueName = "RequirePsDirectAuthentication";

            try
            {
                Microsoft.Win32.RegistryView registryView = Environment.Is64BitOperatingSystem
                    ? Microsoft.Win32.RegistryView.Registry64
                    : Microsoft.Win32.RegistryView.Default;

                using (Microsoft.Win32.RegistryKey baseKey = Microsoft.Win32.RegistryKey.OpenBaseKey(
                    registryHive,
                    registryView))
                {
                    using (Microsoft.Win32.RegistryKey key = baseKey.OpenSubKey(keyPath))
                    {
                        if (key != null)
                        {
                            var value = key.GetValue(regValueName);
                            if (value is int intValue && intValue != 0)
                            {
                                return true;
                            }
                        }

                        return false;
                    }
                }
            }
            catch (Exception regEx)
            {
                s_tracer.WriteLine("IsRequirePsDirectAuthenticationEnabled: Exception while checking registry key: {0}", regEx.Message);
                return false; // If we cannot read the registry, assume the feature is not enabled.
            }
        }

        /// <summary>
        /// Handles credential and configuration exchange with the VM for the first connection.
        /// </summary>
        public static (bool success, string authenticationToken) ExchangeCredentialsAndConfiguration(NetworkCredential networkCredential, string configurationName, Socket HyperVSocket, bool useBackwardsCompatibleMode)
        {
            // Encoding for the Hyper-V socket communication
            // To send the domain, username, password, and configuration name, use UTF-16 (Encoding.Unicode)
            // All other sends use UTF-8 (Encoding.UTF8)
            // Receiving uses ASCII encoding
            // NOT CONFUSING AT ALL

            if (!useBackwardsCompatibleMode)
            {
                HyperVSocket.Send(Encoding.UTF8.GetBytes(VERSION_REQUEST));
                // vmicvmsession service in VM will respond with "VERSION_2" or newer
                // Version 1 protocol will respond with "PASS" or "FAIL"
                // Receive the response and check for VERSION_2 or newer
                string responseStr = ReceiveResponse(HyperVSocket, 16);
                if (!responseStr.StartsWith(VERSION_PREFIX, StringComparison.Ordinal))
                {
                    s_tracer.WriteLine("When asking for version the server responded with an invalid response of {0}.", responseStr);
                    s_tracer.WriteLine("Session is invalid, continuing session with a fake user to close the session with the broker for stability.");
                    // If not the new protocol, finish the conversation
                    // Send a fake user
                    // Use ? <> that are illegal in user names so no one can create the user
                    string probeUserName = "?<PSDirectVMLegacy>"; // must be less than or equal to 20 characters for Windows Server 2016
                    s_tracer.WriteLine("probeUserName (static): length: {0}", probeUserName.Length);
                    SendUserData(probeUserName, HyperVSocket);
                    responseStr = ReceiveResponse(HyperVSocket, 4); // either "PASS" or "FAIL"
                    s_tracer.WriteLine("When sending user {0}.", responseStr);

                    // Send that the password is empty
                    HyperVSocket.Send("EMPTYPW"u8);
                    responseStr = ReceiveResponse(HyperVSocket, 4); // either "CONF", "PASS" or "FAIL"
                    s_tracer.WriteLine("When sending EMPTYPW: {0}.", responseStr); // server responds with FAIL so we respond with FAIL and the conversation is done
                    HyperVSocket.Send("FAIL"u8);

                    s_tracer.WriteLine("Notifying the transport manager to downgrade if allowed.");
                    // end new code
                    return (false, null);
                }

                HyperVSocket.Send(Encoding.UTF8.GetBytes(CLIENT_VERSION));
                ReceiveResponse(HyperVSocket, 4); // either "PASS" or "FAIL"
            }

            if (string.IsNullOrEmpty(networkCredential.Domain))
            {
                networkCredential.Domain = "localhost";
            }

            System.Security.SecureString securePassword = networkCredential.SecurePassword;
            int passwordLength = securePassword.Length;
            bool emptyPassword = (passwordLength <= 0);
            bool emptyConfiguration = string.IsNullOrEmpty(configurationName);

            string responseString;

            // Send credential to VM so that PowerShell process inside VM can be
            // created under the correct security context.
            SendUserData(networkCredential.Domain, HyperVSocket);
            ReceiveResponse(HyperVSocket, 4); // only "PASS" is expected

            SendUserData(networkCredential.UserName, HyperVSocket);
            ReceiveResponse(HyperVSocket, 4); // only "PASS" is expected

            // We cannot simply send password because if it is empty,
            // the vmicvmsession service in VM will block in recv method.
            if (emptyPassword)
            {
                HyperVSocket.Send("EMPTYPW"u8);
                responseString = ReceiveResponse(HyperVSocket, 4); // either "CONF", "PASS" or "FAIL" (note, "PASS" is not used in VERSION_2 or newer mode)
            }
            else
            {
                HyperVSocket.Send("NONEMPTYPW"u8);
                ReceiveResponse(HyperVSocket, 4); // only "PASS" is expected

                // Get the password bytes from the SecureString, send them, and then zero out the byte array.
                byte[] passwordBytes = Microsoft.PowerShell.SecureStringHelper.GetData(securePassword);
                try
                {
                    HyperVSocket.Send(passwordBytes);
                }
                finally
                {
                    // Zero out the byte array for security
                    Array.Clear(passwordBytes);
                }

                responseString = ReceiveResponse(HyperVSocket, 4); // either "CONF", "PASS" or "FAIL" (note, "PASS" is not used in VERSION_2 or newer mode)
            }

            // Check for invalid response from server
            if (!string.Equals(responseString, "FAIL", StringComparison.Ordinal) &&
                !string.Equals(responseString, "PASS", StringComparison.Ordinal) &&
                !string.Equals(responseString, "CONF", StringComparison.Ordinal))
            {
                s_tracer.WriteLine("ExchangeCredentialsAndConfiguration: Server responded with an invalid response of {0} for credentials.", responseString);
                throw new PSDirectException(
                    PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.HyperVInvalidResponse, "Broker", "Credential"));
            }

            // Credential is invalid.
            if (string.Equals(responseString, "FAIL", StringComparison.Ordinal))
            {
                HyperVSocket.Send("FAIL"u8);
                // should we be doing this?  Disabling the test for now
                // HyperVSocket.Shutdown(SocketShutdown.Both);
                s_tracer.WriteLine("ExchangeCredentialsAndConfiguration: Server responded with FAIL for credentials.");
                throw new PSDirectException(
                    PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.InvalidCredential));
            }

            // If PowerShell Direct in VM supports configuration, send configuration name.
            if (string.Equals(responseString, "CONF", StringComparison.Ordinal))
            {
                if (emptyConfiguration)
                {
                    HyperVSocket.Send("EMPTYCF"u8);
                }
                else
                {
                    HyperVSocket.Send("NONEMPTYCF"u8);
                    ReceiveResponse(HyperVSocket, 4); // only "PASS" is expected

                    SendUserData(configurationName, HyperVSocket);
                }
            }
            else
            {
                HyperVSocket.Send("PASS"u8);
            }

            if (!useBackwardsCompatibleMode)
            {
                // Receive the token from the server
                // Getting 1024 bytes because it is well above the expected token size
                // The expected size at the time of writing this would be about 50 based64 characters,
                // plus the 6 characters for the "TOKEN " prefix.
                // The 50 character size is designed to last 10 years of cryptographic changes.
                // Since the broker completely controls the cryptographic portion here,
                // allowing a significant larger size, allows the broker to make almost arbitrary changes,
                // without breaking the client.
                string token = ReceiveResponse(HyperVSocket, 1024); // either "PASS" or "FAIL"
                if (token == null || !token.StartsWith("TOKEN ", StringComparison.Ordinal))
                {
                    s_tracer.WriteLine("ExchangeCredentialsAndConfiguration: Server did not respond with a valid token. Response: {0}", token);
                    throw new PSDirectException(
                        PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.HyperVInvalidResponse, "Broker", "Token " + token));
                }

                token = token.Substring(6); // remove "TOKEN " prefix

                HyperVSocket.Send("PASS"u8); // acknowledge the token
                return (true, token);
            }

            return (true, null);
        }

        public void Close()
        {
            Stream.Dispose();
            HyperVSocket.Dispose();
        }

        /// <summary>
        /// Receives a response from the socket and decodes it.
        /// </summary>
        /// <param name="socket">The socket to receive from.</param>
        /// <param name="bufferSize">The size of the buffer to use for receiving data.</param>
        /// <returns>The decoded response string.</returns>
        internal static string ReceiveResponse(Socket socket, int bufferSize)
        {
            System.Buffers.ArrayPool<byte> pool = System.Buffers.ArrayPool<byte>.Shared;
            byte[] responseBuffer = pool.Rent(bufferSize);
            int bytesReceived = 0;
            try
            {
                bytesReceived = socket.Receive(responseBuffer);
                if (bytesReceived == 0)
                {
                    return null;
                }

                string response = Encoding.ASCII.GetString(responseBuffer, 0, bytesReceived);

                // Handle null terminators and log if found
                if (response.EndsWith('\0'))
                {
                    int originalLength = response.Length;
                    response = response.TrimEnd('\0');
                    // Cannot log actual response, because we don't know if it is sensitive
                    s_tracer.WriteLine(
                        "ReceiveResponse: Removed null terminator(s). Original length: {0}, New length: {1}",
                        originalLength,
                        response.Length);
                }

                return response;
            }
            finally
            {
                pool.Return(responseBuffer);
            }
        }

        /// <summary>
        /// Sends user data (domain, username, etc.) over the HyperVSocket using Unicode encoding.
        /// </summary>
        private static void SendUserData(string data, Socket socket)
        {
            // this encodes the data in UTF-16 (Unicode)
            byte[] buffer = Encoding.Unicode.GetBytes(data);
            socket.Send(buffer);
        }
        #endregion
    }
}
