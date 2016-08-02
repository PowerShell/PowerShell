/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Management.Automation.Tracing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Dbg = System.Diagnostics.Debug;

#if CORECLR
// Use stubs for SerializableAttribute.
using Microsoft.PowerShell.CoreClr.Stubs;
#endif

namespace System.Management.Automation.Remoting
{
    [SerializableAttribute]
    internal class HyperVSocketEndPoint : EndPoint
    {
        #region Members

        private System.Net.Sockets.AddressFamily _addressFamily;
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
            set { _vmId = value; }
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

        public override bool Equals(Object obj)
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

        private Socket _socket;
        private NetworkStream _networkStream;
        private StreamReader _streamReader;
        private StreamWriter _streamWriter;
        private readonly object _syncObject;
        private bool _disposed;
        private PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();

        #endregion

        #region Properties

        /// <summary>
        /// Returns the Hyper-V socket object.
        /// </summary>
        public Socket HyperVSocket
        {
            get { return _socket; }
        }

        /// <summary>
        /// Returns the network stream object.
        /// </summary>
        public NetworkStream Stream
        {
            get { return _networkStream; }
        }

        /// <summary>
        /// Accessor for the Hyper-V socket reader.
        /// </summary>
        public StreamReader TextReader
        {
            get { return _streamReader; }
        }

        /// <summary>
        /// Accessor for the Hyper-V socket writer.
        /// </summary>
        public StreamWriter TextWriter
        {
            get { return _streamWriter; }
        }

        /// <summary>
        /// Returns true if object is currently disposed.
        /// </summary>
        public bool IsDisposed
        {
            get { return _disposed; }
        }

        #endregion

        #region Constructors

        public RemoteSessionHyperVSocketServer(bool LoopbackMode)
        {
            // TODO: uncomment below code when .NET supports Hyper-V socket duplication
            /*
            NamedPipeClientStream clientPipeStream;
            byte[] buffer = new byte[1000];
            int bytesRead;
            */
            _syncObject = new object();

            Exception ex = null;

            try
            {
                // TODO: uncomment below code when .NET supports Hyper-V socket duplication
                /*
                if (!LoopbackMode)
                {
                    //
                    // Create named pipe client.
                    //
                    using (clientPipeStream = new NamedPipeClientStream(".",                       
                                                                        "PS_VMSession",
                                                                        PipeDirection.InOut,
                                                                        PipeOptions.None,
                                                                        TokenImpersonationLevel.None))
                    {
                        //
                        // Connect to named pipe server.
                        //
                        clientPipeStream.Connect(10*1000);
                        
                        //
                        // Read LPWSAPROTOCOL_INFO.
                        //
                        bytesRead = clientPipeStream.Read(buffer, 0, 1000);
                    }
                }
                
                //
                // Create duplicate socket.
                //
                byte[] protocolInfo = new byte[bytesRead];
                Array.Copy(buffer, protocolInfo, bytesRead);
                
                SocketInformation sockInfo = new SocketInformation();
                sockInfo.ProtocolInformation = protocolInfo;
                sockInfo.Options = SocketInformationOptions.Connected;
                
                socket = new Socket(sockInfo);
                if (socket == null)
                {
                    Dbg.Assert(false, "Unexpected error in RemoteSessionHyperVSocketServer.");
                
                    tracer.WriteMessage("RemoteSessionHyperVSocketServer", "RemoteSessionHyperVSocketServer", Guid.Empty,
                        "Unexpected error in constructor: {0}", "socket duplication failure");
                }
                */

                // TODO: remove below 6 lines of code when .NET supports Hyper-V socket duplication
                Guid serviceId = new Guid("a5201c21-2770-4c11-a68e-f182edb29220"); // HV_GUID_VM_SESSION_SERVICE_ID_2
                HyperVSocketEndPoint endpoint = new HyperVSocketEndPoint(HyperVSocketEndPoint.AF_HYPERV, Guid.Empty, serviceId);

                Socket listenSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, (System.Net.Sockets.ProtocolType)1);
                listenSocket.Bind(endpoint);

                listenSocket.Listen(1);
                _socket = listenSocket.Accept();

                _networkStream = new NetworkStream(_socket, true);

                // Create reader/writer streams.
                _streamReader = new StreamReader(_networkStream);
                _streamWriter = new StreamWriter(_networkStream);
                _streamWriter.AutoFlush = true;

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
                CommandProcessorBase.CheckForSevereException(e);
                ex = e;
            }

            if (ex != null)
            {
                Dbg.Assert(false, "Unexpected error in RemoteSessionHyperVSocketServer.");

                // Unexpected error.
                string errorMessage = !string.IsNullOrEmpty(ex.Message) ? ex.Message : string.Empty;
                _tracer.WriteMessage("RemoteSessionHyperVSocketServer", "RemoteSessionHyperVSocketServer", Guid.Empty,
                    "Unexpected error in constructor: {0}", errorMessage);

                throw new PSInvalidOperationException(
                    PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.RemoteSessionHyperVSocketServerConstructorFailure),
                    ex,
                    PSRemotingErrorId.RemoteSessionHyperVSocketServerConstructorFailure.ToString(),
                    ErrorCategory.InvalidOperation,
                    null);
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            lock (_syncObject)
            {
                if (_disposed) { return; }
                _disposed = true;
            }

            if (_streamReader != null)
            {
                try { _streamReader.Dispose(); }
                catch (ObjectDisposedException) { }
                _streamReader = null;
            }

            if (_streamWriter != null)
            {
                try { _streamWriter.Dispose(); }
                catch (ObjectDisposedException) { }
                _streamWriter = null;
            }

            if (_networkStream != null)
            {
                try { _networkStream.Dispose(); }
                catch (ObjectDisposedException) { }
            }

            if (_socket != null)
            {
                try { _socket.Dispose(); }
                catch (ObjectDisposedException) { }
            }
        }

        #endregion
    }

    internal sealed class RemoteSessionHyperVSocketClient : IDisposable
    {
        #region Members

        private HyperVSocketEndPoint _endPoint;
        private Socket _socket;
        private NetworkStream _networkStream;
        private StreamReader _streamReader;
        private StreamWriter _streamWriter;
        private readonly object _syncObject;
        private bool _disposed;
        private PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();

        private static ManualResetEvent s_connectDone =
                new ManualResetEvent(false);

        #endregion

        #region constants in hvsocket.h

        public const int HV_PROTOCOL_RAW = 1;
        public const int HVSOCKET_CONTAINER_PASSTHRU = 2;

        #endregion

        #region Properties

        /// <summary>
        /// Returns the Hyper-V socket endpoint object.
        /// </summary>
        public HyperVSocketEndPoint EndPoint
        {
            get { return _endPoint; }
        }

        /// <summary>
        /// Returns the Hyper-V socket object.
        /// </summary>
        public Socket HyperVSocket
        {
            get { return _socket; }
        }

        /// <summary>
        /// Returns the network stream object.
        /// </summary>
        public NetworkStream Stream
        {
            get { return _networkStream; }
        }

        /// <summary>
        /// Accessor for the Hyper-V socket reader.
        /// </summary>
        public StreamReader TextReader
        {
            get { return _streamReader; }
        }

        /// <summary>
        /// Accessor for the Hyper-V socket writer.
        /// </summary>
        public StreamWriter TextWriter
        {
            get { return _streamWriter; }
        }

        /// <summary>
        /// Returns true if object is currently disposed.
        /// </summary>
        public bool IsDisposed
        {
            get { return _disposed; }
        }

        #endregion

        #region Constructors

        internal RemoteSessionHyperVSocketClient(
            Guid vmId,
            bool isFirstConnection,
            bool isContainer = false)
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

            _endPoint = new HyperVSocketEndPoint(HyperVSocketEndPoint.AF_HYPERV, vmId, serviceId);

            _socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, (System.Net.Sockets.ProtocolType)1);

            //
            // We need to call SetSocketOption() in order to set up Hyper-V socket connection between container host and Hyper-V container.
            // Here is the scenario: the Hyper-V container is inside a utility vm, which is inside the container host 
            //
            if (isContainer)
            {
                var value = new byte[sizeof(uint)];
                value[0] = 1;

                try
                {
                    _socket.SetSocketOption((System.Net.Sockets.SocketOptionLevel)HV_PROTOCOL_RAW,
                                            (System.Net.Sockets.SocketOptionName)HVSOCKET_CONTAINER_PASSTHRU,
                                            (byte[])value);
                }
                catch
                {
                    throw new PSDirectException(
                        PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.RemoteSessionHyperVSocketClientConstructorSetSocketOptionFailure));
                }
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            lock (_syncObject)
            {
                if (_disposed) { return; }
                _disposed = true;
            }

            if (_streamReader != null)
            {
                try { _streamReader.Dispose(); }
                catch (ObjectDisposedException) { }
                _streamReader = null;
            }

            if (_streamWriter != null)
            {
                try { _streamWriter.Dispose(); }
                catch (ObjectDisposedException) { }
                _streamWriter = null;
            }

            if (_networkStream != null)
            {
                try { _networkStream.Dispose(); }
                catch (ObjectDisposedException) { }
            }

            if (_socket != null)
            {
                try { _socket.Dispose(); }
                catch (ObjectDisposedException) { }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Connect to Hyper-V socket server.  This is a blocking call until a 
        /// connection occurs or the timeout time has ellapsed.
        /// </summary>
        /// <param name="networkCredential">The credential used for authentication</param>
        /// <param name="configurationName">The configuration name of the PS session</param>
        /// <param name="isFirstConnection">Whether this is the first connection</param>        
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
                if (String.IsNullOrEmpty(networkCredential.UserName))
                {
                    throw new PSDirectException(
                        PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.InvalidUsername));
                }
            }

            _socket.Connect(_endPoint);

            if (_socket.Connected)
            {
                _tracer.WriteMessage("RemoteSessionHyperVSocketClient", "Connect", Guid.Empty,
                    "Client connected.");

                _networkStream = new NetworkStream(_socket, true);

                if (isFirstConnection)
                {
                    if (String.IsNullOrEmpty(networkCredential.Domain))
                    {
                        networkCredential.Domain = "localhost";
                    }

                    bool emptyPassword = String.IsNullOrEmpty(networkCredential.Password);
                    bool emptyConfiguration = String.IsNullOrEmpty(configurationName);

                    Byte[] domain = Encoding.Unicode.GetBytes(networkCredential.Domain);
                    Byte[] userName = Encoding.Unicode.GetBytes(networkCredential.UserName);
                    Byte[] password = Encoding.Unicode.GetBytes(networkCredential.Password);
                    Byte[] response = new Byte[4]; // either "PASS" or "FAIL"
                    string responseString;

                    //
                    // Send credential to VM so that PowerShell process inside VM can be
                    // created under the correct security context.
                    //
                    _socket.Send(domain);
                    _socket.Receive(response);

                    _socket.Send(userName);
                    _socket.Receive(response);

                    //
                    // We cannot simply send password because if it is empty,
                    // the vmicvmsession service in VM will block in recv method.
                    //
                    if (emptyPassword)
                    {
                        _socket.Send(Encoding.ASCII.GetBytes("EMPTYPW"));
                        _socket.Receive(response);
                        responseString = Encoding.ASCII.GetString(response);
                    }
                    else
                    {
                        _socket.Send(Encoding.ASCII.GetBytes("NONEMPTYPW"));
                        _socket.Receive(response);

                        _socket.Send(password);
                        _socket.Receive(response);
                        responseString = Encoding.ASCII.GetString(response);
                    }

                    //
                    // There are 3 cases for the responseString received above.
                    // - "FAIL": credential is invalid
                    // - "PASS": credentail is valid, but PowerShell Direct in VM does not support configuration (Server 2016 TP4 and before)
                    // - "CONF": credentail is valid, and PowerShell Direct in VM supports configuration (Server 2016 TP5 and later)
                    //

                    //
                    // Credential is invalid.
                    //
                    if (String.Compare(responseString, "FAIL", StringComparison.Ordinal) == 0)
                    {
                        _socket.Send(response);

                        throw new PSDirectException(
                            PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.InvalidCredential));
                    }

                    //
                    // If PowerShell Direct in VM supports configuration, send configuration name.
                    //
                    if (String.Compare(responseString, "CONF", StringComparison.Ordinal) == 0)
                    {
                        if (emptyConfiguration)
                        {
                            _socket.Send(Encoding.ASCII.GetBytes("EMPTYCF"));
                        }
                        else
                        {
                            _socket.Send(Encoding.ASCII.GetBytes("NONEMPTYCF"));
                            _socket.Receive(response);

                            Byte[] configName = Encoding.Unicode.GetBytes(configurationName);
                            _socket.Send(configName);
                        }
                    }
                    else
                    {
                        _socket.Send(response);
                    }
                }

                _streamReader = new StreamReader(_networkStream);
                _streamWriter = new StreamWriter(_networkStream);
                _streamWriter.AutoFlush = true;

                result = true;
            }
            else
            {
                _tracer.WriteMessage("RemoteSessionHyperVSocketClient", "Connect", Guid.Empty,
                    "Client unable to connect.");

                result = false;
            }

            return result;
        }

        public void Close()
        {
            _networkStream.Dispose();
            _socket.Dispose();
        }

        #endregion
    }
}
