// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting.Server;
using System.Management.Automation.Tracing;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

using Microsoft.Win32.SafeHandles;

using Dbg = System.Diagnostics.Debug;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// Shared named pipe utilities.
    /// </summary>
    internal static class NamedPipeUtils
    {
        #region Strings

        internal const string NamedPipeNamePrefix = "PSHost.";
#if UNIX
        internal const string DefaultAppDomainName = "None";
        // This `CoreFxPipe` prefix is defined by CoreFx
        internal const string NamedPipeNamePrefixSearch = "CoreFxPipe_PSHost*";
#else
        internal const string DefaultAppDomainName = "DefaultAppDomain";
        internal const string NamedPipeNamePrefixSearch = "PSHost*";
#endif
        // On non-Windows, .NET named pipes are limited to up to 104 characters
        internal const int MaxNamedPipeNameSize = 104;

        #endregion

        #region Static Methods

        /// <summary>
        /// Create a pipe name based on process information.
        /// E.g., "PSHost.ProcessStartTime.ProcessId.DefaultAppDomain.ProcessName"
        /// </summary>
        /// <param name="procId">Process Id.</param>
        /// <returns>Pipe name.</returns>
        internal static string CreateProcessPipeName(
            int procId)
        {
            return CreateProcessPipeName(
                System.Diagnostics.Process.GetProcessById(procId));
        }

        /// <summary>
        /// Create a pipe name based on process information.
        /// E.g., "PSHost.ProcessStartTime.ProcessId.DefaultAppDomain.ProcessName"
        /// </summary>
        /// <param name="proc">Process object.</param>
        /// <returns>Pipe name.</returns>
        internal static string CreateProcessPipeName(
            System.Diagnostics.Process proc)
        {
            return CreateProcessPipeName(proc, DefaultAppDomainName);
        }

        /// <summary>
        /// Create a pipe name based on process Id and appdomain name information.
        /// E.g., "PSHost.ProcessStartTime.ProcessId.DefaultAppDomain.ProcessName"
        /// </summary>
        /// <param name="procId">Process Id.</param>
        /// <param name="appDomainName">Name of process app domain to connect to.</param>
        /// <returns>Pipe name.</returns>
        internal static string CreateProcessPipeName(
            int procId,
            string appDomainName)
        {
            return CreateProcessPipeName(System.Diagnostics.Process.GetProcessById(procId), appDomainName);
        }

        /// <summary>
        /// Create a pipe name based on process and appdomain name information.
        /// E.g., "PSHost.ProcessStartTime.ProcessId.DefaultAppDomain.ProcessName"
        /// </summary>
        /// <param name="proc">Process object.</param>
        /// <param name="appDomainName">Name of process app domain to connect to.</param>
        /// <returns>Pipe name.</returns>
        internal static string CreateProcessPipeName(
            System.Diagnostics.Process proc,
            string appDomainName)
        {
            if (proc == null)
            {
                throw new PSArgumentNullException(nameof(proc));
            }

            if (string.IsNullOrEmpty(appDomainName))
            {
                appDomainName = DefaultAppDomainName;
            }

            System.Text.StringBuilder pipeNameBuilder = new System.Text.StringBuilder(MaxNamedPipeNameSize);
            pipeNameBuilder.Append(NamedPipeNamePrefix)
                // The starttime is there to prevent another process easily guessing the pipe name
                // and squatting on it.
                // There is a limit of 104 characters in total including the temp path to the named pipe file
                // on non-Windows systems, so we'll convert the starttime to hex and just take the first 8 characters.
#if UNIX
                .Append(proc.StartTime.ToFileTime().ToString("X8").AsSpan(1, 8))
#else
                .Append(proc.StartTime.ToFileTime().ToString(CultureInfo.InvariantCulture))
#endif
                .Append('.')
                .Append(proc.Id.ToString(CultureInfo.InvariantCulture))
                .Append('.')
                .Append(CleanAppDomainNameForPipeName(appDomainName))
                .Append('.')
                .Append(proc.ProcessName);
#if UNIX
            int charsToTrim = pipeNameBuilder.Length - MaxNamedPipeNameSize;
            if (charsToTrim > 0)
            {
                // TODO: In the case the pipe name is truncated, the user cannot connect to it using the cmdlet
                // unless we add a `-Force` type switch as it attempts to validate the current process name
                // matches the process name in the pipe name
                pipeNameBuilder.Remove(MaxNamedPipeNameSize + 1, charsToTrim);
            }
#endif

            return pipeNameBuilder.ToString();
        }

        private static string CleanAppDomainNameForPipeName(string appDomainName)
        {
            // Pipe names cannot contain the ':' character.  Remove unwanted characters.
            return appDomainName.Replace(":", string.Empty).Replace(" ", string.Empty);
        }

        /// <summary>
        /// Returns the current process AppDomain name.
        /// </summary>
        /// <returns>AppDomain Name string.</returns>
        internal static string GetCurrentAppDomainName()
        {
#if CORECLR // There is only one AppDomain per application in CoreCLR, which would be the default
            return DefaultAppDomainName;
#else       // Use the AppDomain in which current powershell is running
            return AppDomain.CurrentDomain.IsDefaultAppDomain() ? DefaultAppDomainName : AppDomain.CurrentDomain.FriendlyName;
#endif
        }

        #endregion
    }

    /// <summary>
    /// Native API for Named Pipes.
    /// </summary>
    internal static class NamedPipeNative
    {
        #region Pipe constants

        // Pipe open modes
        internal const uint PIPE_ACCESS_DUPLEX = 0x00000003;
        internal const uint PIPE_ACCESS_OUTBOUND = 0x00000002;
        internal const uint PIPE_ACCESS_INBOUND = 0x00000001;

        // Pipe modes
        internal const uint PIPE_TYPE_BYTE = 0x00000000;
        internal const uint PIPE_TYPE_MESSAGE = 0x00000004;
        internal const uint FILE_FLAG_OVERLAPPED = 0x40000000;
        internal const uint FILE_FLAG_FIRST_PIPE_INSTANCE = 0x00080000;
        internal const uint PIPE_WAIT = 0x00000000;
        internal const uint PIPE_NOWAIT = 0x00000001;
        internal const uint PIPE_READMODE_BYTE = 0x00000000;
        internal const uint PIPE_READMODE_MESSAGE = 0x00000002;
        internal const uint PIPE_ACCEPT_REMOTE_CLIENTS = 0x00000000;
        internal const uint PIPE_REJECT_REMOTE_CLIENTS = 0x00000008;

        // Pipe errors
        internal const uint ERROR_FILE_NOT_FOUND = 2;
        internal const uint ERROR_BROKEN_PIPE = 109;
        internal const uint ERROR_PIPE_BUSY = 231;
        internal const uint ERROR_NO_DATA = 232;
        internal const uint ERROR_MORE_DATA = 234;
        internal const uint ERROR_PIPE_CONNECTED = 535;
        internal const uint ERROR_IO_INCOMPLETE = 996;
        internal const uint ERROR_IO_PENDING = 997;

        #endregion

        #region Data structures

        [StructLayout(LayoutKind.Sequential)]
        internal class SECURITY_ATTRIBUTES
        {
            /// <summary>
            /// The size, in bytes, of this structure. Set this value to the size of the SECURITY_ATTRIBUTES structure.
            /// </summary>
            public int NLength;

            /// <summary>
            /// A pointer to a security descriptor for the object that controls the sharing of it.
            /// </summary>
            public IntPtr LPSecurityDescriptor = IntPtr.Zero;

            /// <summary>
            /// A Boolean value that specifies whether the returned handle is inherited when a new process is created.
            /// </summary>
            public bool InheritHandle;

            /// <summary>
            /// Initializes a new instance of the SECURITY_ATTRIBUTES class.
            /// </summary>
            public SECURITY_ATTRIBUTES()
            {
                this.NLength = 12;
            }
        }

        #endregion

        #region Pipe methods

        [DllImport(PinvokeDllNames.CreateNamedPipeDllName, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern SafePipeHandle CreateNamedPipe(
           string lpName,
           uint dwOpenMode,
           uint dwPipeMode,
           uint nMaxInstances,
           uint nOutBufferSize,
           uint nInBufferSize,
           uint nDefaultTimeOut,
           SECURITY_ATTRIBUTES securityAttributes);

        internal static SECURITY_ATTRIBUTES GetSecurityAttributes(GCHandle securityDescriptorPinnedHandle, bool inheritHandle = false)
        {
            SECURITY_ATTRIBUTES securityAttributes = new NamedPipeNative.SECURITY_ATTRIBUTES();
            securityAttributes.InheritHandle = inheritHandle;
            securityAttributes.NLength = (int)Marshal.SizeOf(securityAttributes);
            securityAttributes.LPSecurityDescriptor = securityDescriptorPinnedHandle.AddrOfPinnedObject();
            return securityAttributes;
        }

        #endregion
    }

    /// <summary>
    /// Event arguments for listener thread end event.
    /// </summary>
    internal sealed class ListenerEndedEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        /// Exception reason for listener end event.  Can be null
        /// which indicates listener thread end is not due to an error.
        /// </summary>
        public Exception Reason { get; }

        /// <summary>
        /// True if listener should be restarted after ending.
        /// </summary>
        public bool RestartListener { get; }

        #endregion

        #region Constructors

        private ListenerEndedEventArgs() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="reason">Listener end reason.</param>
        /// <param name="restartListener">Restart listener.</param>
        public ListenerEndedEventArgs(
            Exception reason,
            bool restartListener)
        {
            Reason = reason;
            RestartListener = restartListener;
        }

        #endregion
    }

    /// <summary>
    /// Light wrapper class for BCL NamedPipeServerStream class, that
    /// creates the named pipe server with process named pipe name,
    /// having correct access restrictions, and provides a listener
    /// thread loop.
    /// </summary>
    public sealed class RemoteSessionNamedPipeServer : IDisposable
    {
        #region Members

        private readonly object _syncObject;
        private readonly PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();

        private const string _threadName = "IPC Listener Thread";
        private const int _namedPipeBufferSizeForRemoting = 32768;
        private const int _maxPipePathLengthLinux = 108;
        private const int _maxPipePathLengthMacOS = 104;

        // Singleton server.
        private static readonly object s_syncObject;
        internal static RemoteSessionNamedPipeServer IPCNamedPipeServer;
        internal static bool IPCNamedPipeServerEnabled;

        // Optional custom server.
        private static RemoteSessionNamedPipeServer _customNamedPipeServer;

        // Access mask constant taken from PipeSecurity access rights and is equivalent to
        // PipeAccessRights.FullControl.
        // See: https://msdn.microsoft.com/library/vstudio/bb348408(v=vs.100).aspx
        //
        private const int _pipeAccessMaskFullControl = 0x1f019f;

        #endregion

        #region Properties

        /// <summary>
        /// Returns the Named Pipe stream object.
        /// </summary>
        internal NamedPipeServerStream Stream { get; }

        /// <summary>
        /// Returns the Named Pipe name.
        /// </summary>
        internal string PipeName { get; }

        /// <summary>
        /// Returns true if listener is currently running.
        /// </summary>
        internal bool IsListenerRunning { get; private set; }

        /// <summary>
        /// Name of session configuration.
        /// </summary>
        internal string ConfigurationName { get; set; }

        /// <summary>
        /// Accessor for the named pipe reader.
        /// </summary>
        internal StreamReader TextReader { get; private set; }

        /// <summary>
        /// Accessor for the named pipe writer.
        /// </summary>
        internal StreamWriter TextWriter { get; private set; }

        /// <summary>
        /// Returns true if object is currently disposed.
        /// </summary>
        internal bool IsDisposed { get; private set; }

        /// <summary>
        /// Buffer size for PSRP fragmentor.
        /// </summary>
        internal static int NamedPipeBufferSizeForRemoting
        {
            get { return _namedPipeBufferSizeForRemoting; }
        }

        #endregion

        #region Events

        /// <summary>
        /// Event raised when the named pipe server listening thread
        /// ends.
        /// </summary>
        internal event EventHandler<ListenerEndedEventArgs> ListenerEnded;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a RemoteSessionNamedPipeServer with the current process and AppDomain information.
        /// </summary>
        /// <returns>RemoteSessionNamedPipeServer.</returns>
        internal static RemoteSessionNamedPipeServer CreateRemoteSessionNamedPipeServer()
        {
            string appDomainName = NamedPipeUtils.GetCurrentAppDomainName();

            return new RemoteSessionNamedPipeServer(NamedPipeUtils.CreateProcessPipeName(
                System.Diagnostics.Process.GetCurrentProcess(), appDomainName));
        }

        /// <summary>
        /// Constructor.  Creates named pipe server with provided pipe name.
        /// </summary>
        /// <param name="pipeName">Named Pipe name.</param>
        internal RemoteSessionNamedPipeServer(
            string pipeName)
        {
            if (pipeName == null)
            {
                throw new PSArgumentNullException(nameof(pipeName));
            }

            _syncObject = new object();
            PipeName = pipeName;

            Stream = CreateNamedPipe(
                serverName: ".",
                namespaceName: "pipe",
                coreName: pipeName,
                securityDesc: GetServerPipeSecurity());
        }

        /// <summary>
        /// Helper method to create a PowerShell transport named pipe via native API, along
        /// with a returned .Net NamedPipeServerStream object wrapping the named pipe.
        /// </summary>
        /// <param name="serverName">Named pipe server name.</param>
        /// <param name="namespaceName">Named pipe namespace name.</param>
        /// <param name="coreName">Named pipe core name.</param>
        /// <param name="securityDesc"></param>
        /// <returns>NamedPipeServerStream.</returns>
        private static NamedPipeServerStream CreateNamedPipe(
            string serverName,
            string namespaceName,
            string coreName,
            CommonSecurityDescriptor securityDesc)
        {
            if (serverName == null) { throw new PSArgumentNullException(nameof(serverName)); }

            if (namespaceName == null) { throw new PSArgumentNullException(nameof(namespaceName)); }

            if (coreName == null) { throw new PSArgumentNullException(nameof(coreName)); }

#if !UNIX
            string fullPipeName = @"\\" + serverName + @"\" + namespaceName + @"\" + coreName;

            // Create optional security attributes based on provided PipeSecurity.
            NamedPipeNative.SECURITY_ATTRIBUTES securityAttributes = null;
            GCHandle? securityDescHandle = null;
            if (securityDesc != null)
            {
                byte[] securityDescBuffer = new byte[securityDesc.BinaryLength];
                securityDesc.GetBinaryForm(securityDescBuffer, 0);
                securityDescHandle = GCHandle.Alloc(securityDescBuffer, GCHandleType.Pinned);
                securityAttributes = NamedPipeNative.GetSecurityAttributes(securityDescHandle.Value);
            }

            // Create named pipe.
            SafePipeHandle pipeHandle = NamedPipeNative.CreateNamedPipe(
                fullPipeName,
                NamedPipeNative.PIPE_ACCESS_DUPLEX | NamedPipeNative.FILE_FLAG_FIRST_PIPE_INSTANCE | NamedPipeNative.FILE_FLAG_OVERLAPPED,
                NamedPipeNative.PIPE_TYPE_MESSAGE | NamedPipeNative.PIPE_READMODE_MESSAGE | NamedPipeNative.PIPE_REJECT_REMOTE_CLIENTS,
                1,
                _namedPipeBufferSizeForRemoting,
                _namedPipeBufferSizeForRemoting,
                0,
                securityAttributes);

            int lastError = Marshal.GetLastWin32Error();
            securityDescHandle?.Free();

            if (pipeHandle.IsInvalid)
            {
                throw new PSInvalidOperationException(
                    StringUtil.Format(RemotingErrorIdStrings.CannotCreateNamedPipe, lastError));
            }

            // Create the .Net NamedPipeServerStream wrapper.
            try
            {
                return new NamedPipeServerStream(
                    PipeDirection.InOut,
                    true,                       // IsAsync
                    false,                      // IsConnected
                    pipeHandle);
            }
            catch (Exception)
            {
                pipeHandle.Dispose();
                throw;
            }
#else
            return new NamedPipeServerStream(
                pipeName: coreName,
                direction: PipeDirection.InOut,
                maxNumberOfServerInstances: 1,
                transmissionMode: PipeTransmissionMode.Byte,
                options: PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly,
                inBufferSize: _namedPipeBufferSizeForRemoting,
                outBufferSize: _namedPipeBufferSizeForRemoting);
#endif
        }

        static RemoteSessionNamedPipeServer()
        {
            s_syncObject = new object();

            // Unless opt-out, all PowerShell instances will start with the named-pipe listener created and running.
            IPCNamedPipeServerEnabled = !Utils.GetEnvironmentVariableAsBool(name: "POWERSHELL_DIAGNOSTICS_OPTOUT", defaultValue: false);

            if (IPCNamedPipeServerEnabled)
            {
                CreateIPCNamedPipeServerSingleton();
                CreateProcessExitHandler();
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
                if (IsDisposed) { return; }

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
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates the custom named pipe server with the given pipename.
        /// </summary>
        /// <param name="pipeName">The name of the pipe to create.</param>
        public static void CreateCustomNamedPipeServer(string pipeName)
        {
            lock (s_syncObject)
            {
                if (_customNamedPipeServer != null && !_customNamedPipeServer.IsDisposed)
                {
                    if (pipeName == _customNamedPipeServer.PipeName)
                    {
                        // we shouldn't recreate the server object if we're using the same pipeName
                        return;
                    }

                    // Dispose of the current pipe server so we can create a new one with the new pipeName
                    _customNamedPipeServer.Dispose();
                }

                if (!Platform.IsWindows)
                {
                    int maxNameLength = (Platform.IsLinux ? _maxPipePathLengthLinux : _maxPipePathLengthMacOS) - Path.GetTempPath().Length;
                    if (pipeName.Length > maxNameLength)
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                RemotingErrorIdStrings.CustomPipeNameTooLong,
                                maxNameLength,
                                pipeName,
                                pipeName.Length));
                    }
                }

                try
                {
                    try
                    {
                        _customNamedPipeServer = new RemoteSessionNamedPipeServer(pipeName);
                    }
                    catch (IOException)
                    {
                        // Expected when named pipe server for this process already exists.
                        // This can happen if process has multiple AppDomains hosting PowerShell (SMA.dll).
                        return;
                    }

                    // Listener ended callback, used to create listening new pipe server.
                    _customNamedPipeServer.ListenerEnded += OnCustomNamedPipeServerEnded;

                    // Start the pipe server listening thread, and provide client connection callback.
                    _customNamedPipeServer.StartListening(ClientConnectionCallback);
                }
                catch (Exception)
                {
                    _customNamedPipeServer = null;
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Starts named pipe server listening thread.  When a client connects this thread
        /// makes a callback to implement the client communication.  When the thread ends
        /// this object is disposed and a new RemoteSessionNamedPipeServer must be created
        /// and a new listening thread started to handle subsequent client connections.
        /// </summary>
        /// <param name="clientConnectCallback">Connection callback.</param>
        internal void StartListening(
            Action<RemoteSessionNamedPipeServer> clientConnectCallback)
        {
            if (clientConnectCallback == null)
            {
                throw new PSArgumentNullException(nameof(clientConnectCallback));
            }

            lock (_syncObject)
            {
                if (IsListenerRunning)
                {
                    throw new InvalidOperationException(RemotingErrorIdStrings.NamedPipeAlreadyListening);
                }

                IsListenerRunning = true;

                // Create listener thread.
                Thread listenerThread = new Thread(ProcessListeningThread);
                listenerThread.Name = _threadName;
                listenerThread.IsBackground = true;
                listenerThread.Start(clientConnectCallback);
            }
        }

        internal static CommonSecurityDescriptor GetServerPipeSecurity()
        {
#if UNIX
            return null;
#else
            // Built-in Admin SID
            SecurityIdentifier adminSID = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
            DiscretionaryAcl dacl = new DiscretionaryAcl(false, false, 1);
            dacl.AddAccess(
                AccessControlType.Allow,
                adminSID,
                _pipeAccessMaskFullControl,
                InheritanceFlags.None,
                PropagationFlags.None);

            CommonSecurityDescriptor securityDesc = new CommonSecurityDescriptor(
                false, false,
                ControlFlags.DiscretionaryAclPresent | ControlFlags.OwnerDefaulted | ControlFlags.GroupDefaulted,
                null, null, null, dacl);

            // Conditionally add User SID
            bool isAdminElevated = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            if (!isAdminElevated)
            {
                securityDesc.DiscretionaryAcl.AddAccess(
                    AccessControlType.Allow,
                    WindowsIdentity.GetCurrent().User,
                    _pipeAccessMaskFullControl,
                    InheritanceFlags.None,
                    PropagationFlags.None);
            }

            return securityDesc;
#endif
        }

        /// <summary>
        /// Wait for client connection.
        /// </summary>
        private void WaitForConnection()
        {
            Stream.WaitForConnection();
        }

        /// <summary>
        /// Process listening thread.
        /// </summary>
        /// <param name="state">Client callback delegate.</param>
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
        private void ProcessListeningThread(object state)
        {
            string processId = Environment.ProcessId.ToString(CultureInfo.InvariantCulture);
            string appDomainName = NamedPipeUtils.GetCurrentAppDomainName();

            // Logging.
            _tracer.WriteMessage("RemoteSessionNamedPipeServer", "StartListening", Guid.Empty,
                "Listener thread started on Process {0} in AppDomainName {1}.", processId, appDomainName);
            PSEtwLog.LogOperationalInformation(
                PSEventId.NamedPipeIPC_ServerListenerStarted, PSOpcode.Open, PSTask.NamedPipe,
                PSKeyword.UseAlwaysOperational,
                processId, appDomainName);

            Exception ex = null;
            string userName = string.Empty;
            bool restartListenerThread = true;

            // Wait for connection.
            try
            {
                // Begin listening for a client connect.
                this.WaitForConnection();

                try
                {
#if UNIX
                    userName = System.Environment.UserName;
#else
                    userName = WindowsIdentity.GetCurrent().Name;
#endif
                }
                catch (System.Security.SecurityException) { }

                // Logging.
                _tracer.WriteMessage("RemoteSessionNamedPipeServer", "StartListening", Guid.Empty,
                    "Client connection started on Process {0} in AppDomainName {1} for User {2}.", processId, appDomainName, userName);
                PSEtwLog.LogOperationalInformation(
                    PSEventId.NamedPipeIPC_ServerConnect, PSOpcode.Connect, PSTask.NamedPipe,
                    PSKeyword.UseAlwaysOperational,
                    processId, appDomainName, userName);

                // Create reader/writer streams.
                TextReader = new StreamReader(Stream);
                TextWriter = new StreamWriter(Stream);
                TextWriter.AutoFlush = true;
            }
            catch (Exception e)
            {
                ex = e;
            }

            if (ex != null)
            {
                // Error during connection handling.  Don't try to restart listening thread.
                string errorMessage = !string.IsNullOrEmpty(ex.Message) ? ex.Message : string.Empty;
                _tracer.WriteMessage("RemoteSessionNamedPipeServer", "StartListening", Guid.Empty,
                    "Unexpected error in listener thread on process {0} in AppDomainName {1}.  Error Message: {2}", processId, appDomainName, errorMessage);
                PSEtwLog.LogOperationalError(PSEventId.NamedPipeIPC_ServerListenerError, PSOpcode.Exception, PSTask.NamedPipe,
                    PSKeyword.UseAlwaysOperational,
                    processId, appDomainName, errorMessage);

                Dispose();
                return;
            }

            // Start server session on new connection.
            ex = null;
            try
            {
                Action<RemoteSessionNamedPipeServer> clientConnectCallback = state as Action<RemoteSessionNamedPipeServer>;
                Dbg.Assert(clientConnectCallback != null, "Client callback should never be null.");

                // Handle a new client connect by making the callback.
                // The callback must handle all exceptions except
                // for a named pipe disposed or disconnected exception
                // which propagates up to the thread listener loop.
                clientConnectCallback(this);
            }
            catch (IOException)
            {
                // Expected connection terminated.
            }
            catch (ObjectDisposedException)
            {
                // Expected from PS transport close/dispose.
            }
            catch (Exception e)
            {
                ex = e;
                restartListenerThread = false;
            }

            // Logging.
            _tracer.WriteMessage("RemoteSessionNamedPipeServer", "StartListening", Guid.Empty,
                "Client connection ended on process {0} in AppDomainName {1} for User {2}.", processId, appDomainName, userName);
            PSEtwLog.LogOperationalInformation(
                PSEventId.NamedPipeIPC_ServerDisconnect, PSOpcode.Close, PSTask.NamedPipe,
                PSKeyword.UseAlwaysOperational,
                processId, appDomainName, userName);

            if (ex == null)
            {
                // Normal listener exit.
                _tracer.WriteMessage("RemoteSessionNamedPipeServer", "StartListening", Guid.Empty,
                    "Listener thread ended on process {0} in AppDomainName {1}.", processId, appDomainName);
                PSEtwLog.LogOperationalInformation(PSEventId.NamedPipeIPC_ServerListenerEnded, PSOpcode.Close, PSTask.NamedPipe,
                    PSKeyword.UseAlwaysOperational,
                    processId, appDomainName);
            }
            else
            {
                // Unexpected error.
                string errorMessage = !string.IsNullOrEmpty(ex.Message) ? ex.Message : string.Empty;
                _tracer.WriteMessage("RemoteSessionNamedPipeServer", "StartListening", Guid.Empty,
                    "Unexpected error in listener thread on process {0} in AppDomainName {1}.  Error Message: {2}", processId, appDomainName, errorMessage);
                PSEtwLog.LogOperationalError(PSEventId.NamedPipeIPC_ServerListenerError, PSOpcode.Exception, PSTask.NamedPipe,
                    PSKeyword.UseAlwaysOperational,
                    processId, appDomainName, errorMessage);
            }

            lock (_syncObject)
            {
                IsListenerRunning = false;
            }

            // Ensure this named pipe server object is disposed.
            Dispose();

            ListenerEnded.SafeInvoke(
                this,
                new ListenerEndedEventArgs(ex, restartListenerThread));
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Ensures the namedpipe singleton server is running and waits for a client connection.
        /// This is a blocking call that returns after the client connection ends.
        /// This method supports PowerShell running in "NamedPipeServerMode", which is used for
        /// PowerShell Direct Windows Server Container connection and management.
        /// </summary>
        /// <param name="configurationName">Name of the configuration to use.</param>
        internal static void RunServerMode(string configurationName)
        {
            IPCNamedPipeServerEnabled = true;
            CreateIPCNamedPipeServerSingleton();

            if (IPCNamedPipeServer == null)
            {
                throw new RuntimeException(RemotingErrorIdStrings.NamedPipeServerCannotStart);
            }

            IPCNamedPipeServer.ConfigurationName = configurationName;

            ManualResetEventSlim clientConnectionEnded = new ManualResetEventSlim(false);
            IPCNamedPipeServer.ListenerEnded -= OnIPCNamedPipeServerEnded;
            IPCNamedPipeServer.ListenerEnded += (sender, e) => clientConnectionEnded.Set();

            // Wait for server to service a single client connection.
            clientConnectionEnded.Wait();
            clientConnectionEnded.Dispose();
            IPCNamedPipeServerEnabled = false;
        }

        /// <summary>
        /// Creates the process named pipe server object singleton and
        /// starts the client listening thread.
        /// </summary>
        internal static void CreateIPCNamedPipeServerSingleton()
        {
            lock (s_syncObject)
            {
                if (!IPCNamedPipeServerEnabled) { return; }

                if (IPCNamedPipeServer == null || IPCNamedPipeServer.IsDisposed)
                {
                    try
                    {
                        try
                        {
                            IPCNamedPipeServer = CreateRemoteSessionNamedPipeServer();
                        }
                        catch (IOException)
                        {
                            // Expected when named pipe server for this process already exists.
                            // This can happen if process has multiple AppDomains hosting PowerShell (SMA.dll).
                            return;
                        }

                        // Listener ended callback, used to create listening new pipe server.
                        IPCNamedPipeServer.ListenerEnded += OnIPCNamedPipeServerEnded;

                        // Start the pipe server listening thread, and provide client connection callback.
                        IPCNamedPipeServer.StartListening(ClientConnectionCallback);
                    }
                    catch (Exception)
                    {
                        IPCNamedPipeServer = null;
                    }
                }
            }
        }

        private static void CreateProcessExitHandler()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
            {
                IPCNamedPipeServerEnabled = false;
                RemoteSessionNamedPipeServer namedPipeServer = IPCNamedPipeServer;
                if (namedPipeServer != null)
                {
                    try
                    {
                        // Terminate the IPC thread.
                        namedPipeServer.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                        // Ignore if object already disposed.
                    }
                    catch (Exception)
                    {
                        // Don't throw an exception on the app domain unload event thread.
                    }
                }
            };
        }

        private static void OnIPCNamedPipeServerEnded(object sender, ListenerEndedEventArgs args)
        {
            if (args.RestartListener)
            {
                CreateIPCNamedPipeServerSingleton();
            }
        }

        private static void OnCustomNamedPipeServerEnded(object sender, ListenerEndedEventArgs args)
        {
            if (args.RestartListener && sender is RemoteSessionNamedPipeServer server)
            {
                CreateCustomNamedPipeServer(server.PipeName);
            }
        }

        private static void ClientConnectionCallback(RemoteSessionNamedPipeServer pipeServer)
        {
            // Create server mediator object and begin remote session with client.
            NamedPipeProcessMediator.Run(
                string.Empty,
                pipeServer);
        }

        #endregion
    }

    /// <summary>
    /// Base class for RemoteSessionNamedPipeClient and ContainerSessionNamedPipeClient.
    /// </summary>
    internal class NamedPipeClientBase : IDisposable
    {
        #region Members

        private NamedPipeClientStream _clientPipeStream;
        private readonly PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();

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
        /// Name of pipe.
        /// </summary>
        public string PipeName
        {
            get;
            internal set;
        }

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

        #region Methods

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

            _tracer.WriteMessage("NamedPipeClientBase", "Connect", Guid.Empty,
                "Connection started on pipe: {0}", PipeName);
        }

        /// <summary>
        /// Closes the named pipe.
        /// </summary>
        public void Close() => _clientPipeStream?.Dispose();

        /// <summary>
        /// Abort connection attempt.
        /// </summary>
        public virtual void AbortConnect()
        { }

        /// <summary>
        /// Begin connection attempt.
        /// </summary>
        protected virtual NamedPipeClientStream DoConnect(int timeout)
        {
            return null;
        }

        #endregion
    }

    /// <summary>
    /// Light wrapper class for BCL NamedPipeClientStream class, that
    /// creates the named pipe name and initiates connection to
    /// target named pipe server.
    /// </summary>
    internal sealed class RemoteSessionNamedPipeClient : NamedPipeClientBase
    {
        #region Members

        private volatile bool _connecting;

        #endregion

        #region Constructors

        private RemoteSessionNamedPipeClient()
        { }

        /// <summary>
        /// Constructor.  Creates Named Pipe based on process object.
        /// </summary>
        /// <param name="process">Target process object for pipe.</param>
        /// <param name="appDomainName">AppDomain name or null for default AppDomain.</param>
        public RemoteSessionNamedPipeClient(System.Diagnostics.Process process, string appDomainName)
            : this(NamedPipeUtils.CreateProcessPipeName(process, appDomainName))
        { }

        /// <summary>
        /// Constructor. Creates Named Pipe based on process Id.
        /// </summary>
        /// <param name="procId">Target process Id for pipe.</param>
        /// <param name="appDomainName">AppDomain name or null for default AppDomain.</param>
        public RemoteSessionNamedPipeClient(int procId, string appDomainName)
            : this(NamedPipeUtils.CreateProcessPipeName(procId, appDomainName))
        { }

        /// <summary>
        /// Constructor. Creates Named Pipe based on name argument.
        /// </summary>
        /// <param name="pipeName">Named Pipe name.</param>
        internal RemoteSessionNamedPipeClient(
           string pipeName)
        {
            if (pipeName == null)
            {
                throw new PSArgumentNullException(nameof(pipeName));
            }

            PipeName = pipeName;

            // Defer creating the .Net NamedPipeClientStream object until we connect.
            // _clientPipeStream == null.
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="namespaceName"></param>
        /// <param name="coreName"></param>
        internal RemoteSessionNamedPipeClient(
            string serverName,
            string namespaceName,
            string coreName)
        {
            if (serverName == null) { throw new PSArgumentNullException(nameof(serverName)); }

            if (namespaceName == null) { throw new PSArgumentNullException(nameof(namespaceName)); }

            if (coreName == null) { throw new PSArgumentNullException(nameof(coreName)); }

            PipeName = @"\\" + serverName + @"\" + namespaceName + @"\" + coreName;

            // Defer creating the .Net NamedPipeClientStream object until we connect.
            // _clientPipeStream == null.
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Abort connection attempt.
        /// </summary>
        public override void AbortConnect()
        {
            _connecting = false;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Begin connection attempt.
        /// </summary>
        protected override NamedPipeClientStream DoConnect(int timeout)
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

            throw new TimeoutException(RemotingErrorIdStrings.ConnectNamedPipeTimeout);
        }

        #endregion
    }

    /// <summary>
    /// Light wrapper class for BCL NamedPipeClientStream class, that
    /// creates the named pipe name and initiates connection to
    /// target named pipe server inside Windows Server container.
    /// </summary>
    internal sealed class ContainerSessionNamedPipeClient : NamedPipeClientBase
    {
        #region Constructors

        /// <summary>
        /// Constructor. Creates Named Pipe based on process Id, app domain name and container object root path.
        /// </summary>
        /// <param name="procId">Target process Id for pipe.</param>
        /// <param name="appDomainName">AppDomain name or null for default AppDomain.</param>
        /// <param name="containerObRoot">Container OB root.</param>
        public ContainerSessionNamedPipeClient(
            int procId,
            string appDomainName,
            string containerObRoot)
        {
            if (string.IsNullOrEmpty(containerObRoot))
            {
                throw new PSArgumentNullException(nameof(containerObRoot));
            }

            //
            // Named pipe inside Windows Server container is under different name space.
            //
            PipeName = containerObRoot + @"\Device\NamedPipe\" +
                NamedPipeUtils.CreateProcessPipeName(procId, appDomainName);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Helper method to open a named pipe via native APIs and return in
        /// .Net NamedPipeClientStream wrapper object.
        /// </summary>
        protected override NamedPipeClientStream DoConnect(int timeout)
        {
#if UNIX
            // TODO: `CreateFileWithSafePipeHandle` pinvoke below clearly says
            // that the code is only for Windows and we could exclude
            // a lot of code from compilation on Unix.
            throw new NotSupportedException(nameof(DoConnect));
#else
            //
            // WaitNamedPipe API is not supported by Windows Server container now, so we need to repeatedly
            // attempt connection to pipe server until timeout expires.
            //
            int startTime = Environment.TickCount;
            int elapsedTime = 0;
            nint handle;

            do
            {
                // Get handle to pipe.
                handle = Interop.Windows.CreateFileWithPipeHandle(
                    lpFileName: PipeName,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    FileMode.Open,
                    Interop.Windows.FileAttributes.Overlapped);

                if (handle == nint.Zero || handle == (nint)(-1))
                {
                    int lastError = Marshal.GetLastPInvokeError();
                    if (lastError == Interop.Windows.ERROR_FILE_NOT_FOUND)
                    {
                        elapsedTime = unchecked(Environment.TickCount - startTime);
                        Thread.Sleep(100);
                        continue;
                    }
                    else
                    {
                        throw new PSInvalidOperationException(
                            StringUtil.Format(RemotingErrorIdStrings.CannotConnectContainerNamedPipe, lastError));
                    }
                }
                else
                {
                    break;
                }
            } while (elapsedTime < timeout);

            SafePipeHandle pipeHandle = null;
            try
            {
                pipeHandle = new SafePipeHandle(handle, ownsHandle: true);
                return new NamedPipeClientStream(
                    PipeDirection.InOut,
                    isAsync: true,
                    pipeHandle);
            }
            catch (Exception)
            {
                pipeHandle?.Dispose();
                throw;
            }
#endif
        }

        #endregion
    }
}
