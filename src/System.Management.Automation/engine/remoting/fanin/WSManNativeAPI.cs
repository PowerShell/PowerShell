// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

using Dbg = System.Diagnostics.Debug;

namespace System.Management.Automation.Remoting.Client
{
    internal static class WSManNativeApi
    {
        internal const uint INFINITE = 0xFFFFFFFF;
        internal const string PS_CREATION_XML_TAG = "creationXml";
        internal const string PS_CONNECT_XML_TAG = "connectXml";
        internal const string PS_CONNECTRESPONSE_XML_TAG = "connectResponseXml";
        internal const string PS_XML_NAMESPACE = "http://schemas.microsoft.com/powershell";
        internal const string WSMAN_STREAM_ID_STDOUT = "stdout";
        internal const string WSMAN_STREAM_ID_PROMPTRESPONSE = "pr";
        internal const string WSMAN_STREAM_ID_STDIN = "stdin";
        internal const string ResourceURIPrefix = @"http://schemas.microsoft.com/powershell/";
        internal const string NoProfile = "WINRS_NOPROFILE";
        internal const string CodePage = "WINRS_CODEPAGE";

        internal static readonly Version WSMAN_STACK_VERSION = new Version(3, 0);

        internal const int WSMAN_FLAG_REQUESTED_API_VERSION_1_1 = 1;
        // WSMan's default max env size in V2
        internal const int WSMAN_DEFAULT_MAX_ENVELOPE_SIZE_KB_V2 = 150;
        // WSMan's default max env size in V3
        internal const int WSMAN_DEFAULT_MAX_ENVELOPE_SIZE_KB_V3 = 500;

        #region WSMan errors

        /// <summary>
        /// The WinRM service cannot process the request because the request needs to be sent
        /// to a different machine.
        /// Use the redirect information to send the request to a new machine.
        /// 0x8033819B from sdk\inc\wsmerror.h.
        /// </summary>
        internal const int ERROR_WSMAN_REDIRECT_REQUESTED = -2144108135;

        /// <summary>
        /// The WS-Management service cannot process the request. The resource URI is missing or
        ///  it has an incorrect format. Check the documentation or use the following command for
        /// information on how to construct a resource URI: "winrm help uris".
        /// </summary>
        internal const int ERROR_WSMAN_INVALID_RESOURCE_URI = -2144108485;

        /// <summary>
        /// The WinRM service cannon re-connect the session because the session is no longer
        /// associated with this transportmanager object.
        /// </summary>
        internal const int ERROR_WSMAN_INUSE_CANNOT_RECONNECT = -2144108083;

        /// <summary>
        /// Sending data to a remote command failed with the following error message: The client
        /// cannot connect to the destination specified in the request. Verify that the service on
        /// the destination is running and is accepting requests. Consult the logs and documentation
        /// for the WS-Management service running on the destination, most commonly IIS or WinRM.
        /// If the destination is the WinRM service, run the following command on the destination to
        /// analyze and configure the WinRM service:
        /// </summary>
        internal const int ERROR_WSMAN_SENDDATA_CANNOT_CONNECT = -2144108526;

        /// <summary>
        /// Sending data to a remote command failed with the following error message: The WinRM client
        /// cannot complete the operation within the time specified. Check if the machine name is valid
        /// and is reachable over the network and firewall exception for Windows Remote Management service
        /// is enabled.
        /// </summary>
        internal const int ERROR_WSMAN_SENDDATA_CANNOT_COMPLETE = -2144108250;

        internal const int ERROR_WSMAN_ACCESS_DENIED = 5;

        internal const int ERROR_WSMAN_OUTOF_MEMORY = 14;

        internal const int ERROR_WSMAN_NETWORKPATH_NOTFOUND = 53;

        internal const int ERROR_WSMAN_OPERATION_ABORTED = 995;

        internal const int ERROR_WSMAN_SHUTDOWN_INPROGRESS = 1115;

        internal const int ERROR_WSMAN_AUTHENTICATION_FAILED = 1311;

        internal const int ERROR_WSMAN_NO_LOGON_SESSION_EXIST = 1312;

        internal const int ERROR_WSMAN_LOGON_FAILURE = 1326;

        internal const int ERROR_WSMAN_IMPROPER_RESPONSE = 1722;

        internal const int ERROR_WSMAN_INCORRECT_PROTOCOLVERSION = -2141974624;

        internal const int ERROR_WSMAN_URL_NOTAVAILABLE = -2144108269;

        internal const int ERROR_WSMAN_INVALID_AUTHENTICATION = -2144108274;

        internal const int ERROR_WSMAN_CANNOT_CONNECT_INVALID = -2144108080;

        internal const int ERROR_WSMAN_CANNOT_CONNECT_MISMATCH = -2144108090;

        internal const int ERROR_WSMAN_CANNOT_CONNECT_RUNASFAILED = -2144108065;

        internal const int ERROR_WSMAN_CREATEFAILED_INVALIDNAME = -2144108094;

        internal const int ERROR_WSMAN_TARGETSESSION_DOESNOTEXIST = -2144108453;

        internal const int ERROR_WSMAN_REMOTESESSION_DISALLOWED = -2144108116;

        internal const int ERROR_WSMAN_REMOTECONNECTION_DISALLOWED = -2144108061;

        internal const int ERROR_WSMAN_INVALID_RESOURCE_URI2 = -2144108542;

        internal const int ERROR_WSMAN_CORRUPTED_CONFIG = -2144108539;

        internal const int ERROR_WSMAN_URI_LIMIT = -2144108499;

        internal const int ERROR_WSMAN_CLIENT_KERBEROS_DISABLED = -2144108318;

        internal const int ERROR_WSMAN_SERVER_NOTTRUSTED = -2144108316;

        internal const int ERROR_WSMAN_WORKGROUP_NO_KERBEROS = -2144108276;

        internal const int ERROR_WSMAN_EXPLICIT_CREDENTIALS_REQUIRED = -2144108315;

        internal const int ERROR_WSMAN_REDIRECT_LOCATION_INVALID = -2144108105;

        internal const int ERROR_WSMAN_BAD_METHOD = -2144108428;

        internal const int ERROR_WSMAN_HTTP_SERVICE_UNAVAILABLE = -2144108270;

        internal const int ERROR_WSMAN_HTTP_SERVICE_ERROR = -2144108176;

        internal const int ERROR_WSMAN_COMPUTER_NOTFOUND = -2144108103;

        internal const int ERROR_WSMAN_TARGET_UNKNOWN = -2146893053;

        internal const int ERROR_WSMAN_CANNOTUSE_IP = -2144108101;

        #endregion

        #region MarshalledObject

        /// <summary>
        /// A struct holding marshalled data (IntPtr). This is
        /// created to supply IDisposable pattern to safely
        /// release the unmanaged pointer.
        /// </summary>
        internal struct MarshalledObject : IDisposable
        {
            private IntPtr _dataPtr;

            /// <summary>
            /// Constructs a MarshalledObject with the supplied
            /// ptr.
            /// </summary>
            /// <param name="dataPtr"></param>
            internal MarshalledObject(IntPtr dataPtr)
            {
                _dataPtr = dataPtr;
            }

            /// <summary>
            /// Gets the unmanaged ptr.
            /// </summary>
            internal IntPtr DataPtr { get { return _dataPtr; } }

            /// <summary>
            /// Creates a MarshalledObject for the specified object.
            /// </summary>
            /// <typeparam name="T">
            /// Must be a value type.
            /// </typeparam>
            /// <param name="obj"></param>
            /// <returns>MarshalledObject.</returns>
            internal static MarshalledObject Create<T>(T obj)
            {
                IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
                Marshal.StructureToPtr(obj, ptr, false);

                // Now create the MarshalledObject and return.
                MarshalledObject result = new MarshalledObject();
                result._dataPtr = ptr;

                return result;
            }

            /// <summary>
            /// Dispose the unmanaged IntPtr.
            /// </summary>
            public void Dispose()
            {
                if (_dataPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_dataPtr);
                    _dataPtr = IntPtr.Zero;
                }
            }

            /// <summary>
            /// Implicit cast to IntPtr.
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public static implicit operator IntPtr(MarshalledObject obj)
            {
                return obj._dataPtr;
            }
        }

        #endregion

        #region WSMan_Authentication_Credentials

        /// <summary>
        /// Different Authentication Mechanisms supported by WSMan.
        /// TODO: By the look of it, this appears like a Flags enum.
        /// Need to confirm the behavior with WSMan.
        /// </summary>
        /// <remarks>
        /// Please keep in sync with WSManAuthenticationMechanism
        /// from C:\e\win7_powershell\admin\monad\nttargets\assemblies\logging\ETW\Manifests\Microsoft-Windows-PowerShell-Instrumentation.man
        /// </remarks>
        [Flags]
        internal enum WSManAuthenticationMechanism : int
        {
            /// <summary>
            /// Use the default authentication.
            /// </summary>
            WSMAN_FLAG_DEFAULT_AUTHENTICATION = 0x0,
            /// <summary>
            /// Use no authentication for a remote operation.
            /// </summary>
            WSMAN_FLAG_NO_AUTHENTICATION = 0x1,
            /// <summary>
            /// Use digest authentication for a remote operation.
            /// </summary>
            WSMAN_FLAG_AUTH_DIGEST = 0x2,
            /// <summary>
            /// Use negotiate authentication for a remote operation (may use kerberos or ntlm)
            /// </summary>
            WSMAN_FLAG_AUTH_NEGOTIATE = 0x4,
            /// <summary>
            /// Use basic authentication for a remote operation.
            /// </summary>
            WSMAN_FLAG_AUTH_BASIC = 0x8,
            /// <summary>
            /// Use kerberos authentication for a remote operation.
            /// </summary>
            WSMAN_FLAG_AUTH_KERBEROS = 0x10,
            /// <summary>
            /// Use client certificate authentication for a remote operation.
            /// </summary>
            WSMAN_FLAG_AUTH_CLIENT_CERTIFICATE = 0x20,
            /// <summary>
            /// Use CredSSP authentication for a remote operation.
            /// </summary>
            WSMAN_FLAG_AUTH_CREDSSP = 0x80,
        }

        /// <summary>
        /// This is used to represent _WSMAN_AUTHENTICATION_CREDENTIALS
        /// native structure. _WSMAN_AUTHENTICATION_CREDENTIALS has a union
        /// member which cannot be easily represented in managed code.
        /// So created an interface and each union member is represented
        /// with a different structure.
        /// </summary>
        internal abstract class BaseWSManAuthenticationCredentials : IDisposable
        {
            // used to get Marshalled data of the class.
            public abstract MarshalledObject GetMarshalledObject();

            public void Dispose()
            {
                Dispose(true);
                System.GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool isDisposing)
            {
            }
        }

        /// <summary>
        /// Used to supply _WSMAN_USERNAME_PASSWORD_CREDS type credentials for
        /// WSManCreateSession.
        /// </summary>
        internal class WSManUserNameAuthenticationCredentials : BaseWSManAuthenticationCredentials
        {
            /// <summary>
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            internal struct WSManUserNameCredentialStruct
            {
                /// <summary>
                /// </summary>
                internal WSManAuthenticationMechanism authenticationMechanism;
                /// <summary>
                /// </summary>
                [MarshalAs(UnmanagedType.LPWStr)]
                internal string userName;
                /// <summary>
                /// Making password secure.
                /// </summary>
                [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
                internal IntPtr password;
            }

            private WSManUserNameCredentialStruct _cred;
            private MarshalledObject _data;

            /// <summary>
            /// Default constructor.
            /// </summary>
            internal WSManUserNameAuthenticationCredentials()
            {
                _cred = new WSManUserNameCredentialStruct();
                _data = MarshalledObject.Create<WSManUserNameCredentialStruct>(_cred);
            }

            /// <summary>
            /// Constructs an WSManUserNameAuthenticationCredentials object.
            /// It is upto the caller to verify if <paramref name="name"/>
            /// and <paramref name="pwd"/> are valid. This API wont complain
            /// if they are Empty or Null.
            /// </summary>
            /// <param name="name">
            /// user name.
            /// </param>
            /// <param name="pwd">
            /// password.
            /// </param>
            /// <param name="authMechanism">
            /// can be 0 (the user did not specify an authentication mechanism,
            /// WSMan client will choose between Kerberos and Negotiate only);
            /// if it is not 0, it must be one of the values from
            /// WSManAuthenticationMechanism enumeration.
            /// </param>
            internal WSManUserNameAuthenticationCredentials(string name,
                System.Security.SecureString pwd, WSManAuthenticationMechanism authMechanism)
            {
                _cred = new WSManUserNameCredentialStruct();
                _cred.authenticationMechanism = authMechanism;
                _cred.userName = name;
                if (pwd != null)
                {
                    _cred.password = Marshal.SecureStringToCoTaskMemUnicode(pwd);
                }

                _data = MarshalledObject.Create<WSManUserNameCredentialStruct>(_cred);
            }

            /// <summary>
            /// Gets a structure representation (used for marshalling)
            /// </summary>
            internal WSManUserNameCredentialStruct CredentialStruct
            {
                get { return _cred; }
            }

            /// <summary>
            /// Marshalled Data.
            /// </summary>
            /// <returns></returns>
            public override MarshalledObject GetMarshalledObject()
            {
                return _data;
            }

            /// <summary>
            /// Dispose of the resources.
            /// </summary>
            /// <param name="isDisposing"></param>
            protected override void Dispose(bool isDisposing)
            {
                if (_cred.password != IntPtr.Zero)
                {
                    Marshal.ZeroFreeCoTaskMemUnicode(_cred.password);
                    _cred.password = IntPtr.Zero;
                }

                _data.Dispose();
            }
        }

        /// <summary>
        /// </summary>
        internal class WSManCertificateThumbprintCredentials : BaseWSManAuthenticationCredentials
        {
            /// <summary>
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            private struct WSManThumbprintStruct
            {
                /// <summary>
                /// </summary>
                internal WSManAuthenticationMechanism authenticationMechanism;
                /// <summary>
                /// </summary>
                [MarshalAs(UnmanagedType.LPWStr)]
                internal string certificateThumbprint;
                /// <summary>
                /// This is provided for padding as underlying WSMan's implementation
                /// uses a union, we need to pad up unused fields.
                /// </summary>
                internal IntPtr reserved;
            }

            private MarshalledObject _data;

            /// <summary>
            /// Constructs an WSManCertificateThumbprintCredentials object.
            /// It is upto the caller to verify if <paramref name="thumbPrint"/>
            /// is valid. This API wont complain if it is Empty or Null.
            /// </summary>
            /// <param name="thumbPrint"></param>
            internal WSManCertificateThumbprintCredentials(string thumbPrint)
            {
                WSManThumbprintStruct cred = new WSManThumbprintStruct();
                cred.authenticationMechanism = WSManAuthenticationMechanism.WSMAN_FLAG_AUTH_CLIENT_CERTIFICATE;
                cred.certificateThumbprint = thumbPrint;
                cred.reserved = IntPtr.Zero;

                _data = MarshalledObject.Create<WSManThumbprintStruct>(cred);
            }

            /// <summary>
            /// Marshalled Data.
            /// </summary>
            /// <returns></returns>
            public override MarshalledObject GetMarshalledObject()
            {
                return _data;
            }

            /// <summary>
            /// Dispose of the resources.
            /// </summary>
            /// <param name="isDisposing"></param>
            protected override void Dispose(bool isDisposing)
            {
                // data is of struct type..so there is no need to set it to null..
                _data.Dispose();
            }
        }

        #endregion

        #region WSMan Session Options

        /// <summary>
        /// Enum representing native WSManSessionOption enum.
        /// </summary>
        internal enum WSManSessionOption : int
        {
            #region TimeOuts

            /// <summary>
            /// Int - default timeout in ms that applies to all operations on the client side.
            /// </summary>
            WSMAN_OPTION_DEFAULT_OPERATION_TIMEOUTMS = 1,
            /// <summary>
            /// Int - Robust connection maximum retry time in minutes.
            /// </summary>
            WSMAN_OPTION_MAX_RETRY_TIME = 11,
            /// <summary>
            /// Int - timeout in ms for WSManCreateShellEx operations.
            /// </summary>
            WSMAN_OPTION_TIMEOUTMS_CREATE_SHELL = 12,
            /// <summary>
            /// Int - timeout in ms for WSManReceiveShellOutputEx operations.
            /// </summary>
            WSMAN_OPTION_TIMEOUTMS_RECEIVE_SHELL_OUTPUT = 14,
            /// <summary>
            /// Int - timeout in ms for WSManSendShellInputEx operations.
            /// </summary>
            WSMAN_OPTION_TIMEOUTMS_SEND_SHELL_INPUT = 15,
            /// <summary>
            /// Int - timeout in ms for WSManSignalShellEx operations.
            /// </summary>
            WSMAN_OPTION_TIMEOUTMS_SIGNAL_SHELL = 16,
            /// <summary>
            /// Int - timeout in ms for WSManCloseShellOperationEx operations.
            /// </summary>
            WSMAN_OPTION_TIMEOUTMS_CLOSE_SHELL_OPERATION = 17,

            #endregion

            #region Connection Options

            /// <summary>
            /// Int - 1 to not validate the CA on the server certificate; 0 - default.
            /// </summary>
            WSMAN_OPTION_SKIP_CA_CHECK = 18,
            /// <summary>
            /// Int - 1 to not validate the CN on the server certificate; 0 - default.
            /// </summary>
            WSMAN_OPTION_SKIP_CN_CHECK = 19,
            /// <summary>
            /// Int - 1 to not encrypt the messages; 0 - default.
            /// </summary>
            WSMAN_OPTION_UNENCRYPTED_MESSAGES = 20,
            /// <summary>
            /// Int - 1 Send all network packets for remote operations in UTF16; 0 - default is UTF8.
            /// </summary>
            WSMAN_OPTION_UTF16 = 21,
            /// <summary>
            /// Int - 1 When using negotiate, include port number in the connection SPN; 0 - default.
            /// </summary>
            WSMAN_OPTION_ENABLE_SPN_SERVER_PORT = 22,
            /// <summary>
            /// Int - Used when not talking to the main OS on a machine but, for instance, a BMC
            /// 1 Identify this machine to the server by including the MachineID header; 0 - default.
            /// </summary>
            WSMAN_OPTION_MACHINE_ID = 23,
            /// <summary>
            /// Int -1 Enables host process to be created with interactive token.
            /// </summary>
            WSMAN_OPTION_USE_INTERACTIVE_TOKEN = 34,

            #endregion

            #region Locale
            /// <summary>
            /// String - RFC 3066 language code.
            /// </summary>
            WSMAN_OPTION_LOCALE = 25,
            /// <summary>
            /// String - RFC 3066 language code.
            /// </summary>
            WSMAN_OPTION_UI_LANGUAGE = 26,

            #endregion

            #region Other
            /// <summary>
            /// Int - max SOAP envelope size (kb) - default 150kb from winrm config
            /// (see 'winrm help config' for more details); the client SOAP packet size cannot surpass
            /// this value; this value will be also sent to the server in the SOAP request as a
            /// MaxEnvelopeSize header; the server will use min(MaxEnvelopeSizeKb from server configuration,
            /// MaxEnvelopeSize value from SOAP).
            /// </summary>
            WSMAN_OPTION_MAX_ENVELOPE_SIZE_KB = 28,
            /// <summary>
            /// Int (read only) - max data size (kb) provided by the client, guaranteed by
            /// the winrm client implementation to fit into one SOAP packet; this is an
            /// approximate value calculated based on the WSMAN_OPTION_MAX_ENVELOPE_SIZE_KB (default 150kb),
            /// the maximum possible size of the SOAP headers and the overhead of the base64
            /// encoding which is specific to WSManSendShellInput API; this option can be used
            /// with WSManGetSessionOptionAsDword API; it cannot be used with WSManSetSessionOption API.
            /// </summary>
            WSMAN_OPTION_SHELL_MAX_DATA_SIZE_PER_MESSAGE_KB = 29,
            /// <summary>
            /// String -
            /// </summary>
            WSMAN_OPTION_REDIRECT_LOCATION = 30,
            /// <summary>
            /// DWORD  - 1 to not validate the revocation status on the server certificate; 0 - default.
            /// </summary>
            WSMAN_OPTION_SKIP_REVOCATION_CHECK = 31,
            /// <summary>
            /// DWORD  - 1 to allow default credentials for Negotiate (this is for SSL only); 0 - default.
            /// </summary>
            WSMAN_OPTION_ALLOW_NEGOTIATE_IMPLICIT_CREDENTIALS = 32,
            /// <summary>
            /// DWORD - When using just a machine name in the connection string use an SSL connection.
            /// 0 means HTTP, 1 means HTTPS.  Default is 0.
            /// </summary>
            WSMAN_OPTION_USE_SSL = 33
            #endregion
        }

        /// <summary>
        /// Enum representing WSMan Shell specific options.
        /// </summary>
        internal enum WSManShellFlag : int
        {
            /// <summary>
            /// Turn off compression for Send/Receive operations.  By default compression is
            /// turned on, but if communicating with a down-level box it may be necessary to
            /// do this.  Other reasons for turning it off is due to the extra memory consumption
            /// and CPU utilization that is used as a result of compression.
            /// </summary>
            WSMAN_FLAG_NO_COMPRESSION = 1,
            /// <summary>
            /// Enable the service to drop operation output when running disconnected.
            /// </summary>
            WSMAN_FLAG_SERVER_BUFFERING_MODE_DROP = 0x4,
            /// <summary>
            /// Enable the service to block operation progress when output buffers are full.
            /// </summary>
            WSMAN_FLAG_SERVER_BUFFERING_MODE_BLOCK = 0x8,
            /// <summary>
            /// Enable receive call to not immediately retrieve results. Only applicable for Receive calls on commands.
            /// </summary>
            WSMAN_FLAG_RECEIVE_DELAY_OUTPUT_STREAM = 0X10
        }

        #endregion

        #region WSManData
        /// <summary>
        /// Types of supported WSMan data.
        /// PowerShell uses only Text and DWORD (in some places).
        /// </summary>
        internal enum WSManDataType : uint
        {
            WSMAN_DATA_NONE = 0,
            WSMAN_DATA_TYPE_TEXT = 1,
            WSMAN_DATA_TYPE_BINARY = 2,
            WSMAN_DATA_TYPE_WS_XML_READER = 3,
            WSMAN_DATA_TYPE_DWORD = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class WSManDataStruct
        {
            internal uint type;
            internal WSManBinaryOrTextDataStruct binaryOrTextData;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class WSManBinaryOrTextDataStruct
        {
            internal int bufferLength;

            [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
            internal IntPtr data;
        }

        /// <summary>
        /// Used to supply WSMAN_DATA_BINARY/WSMAN_DATA_TEXT type in place of _WSMAN_DATA.
        /// </summary>
        internal class WSManData_ManToUn : IDisposable
        {
            private readonly WSManDataStruct _internalData;

            [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
            private IntPtr _marshalledObject = IntPtr.Zero;

            [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
            private IntPtr _marshalledBuffer = IntPtr.Zero;

            /// <summary>
            /// Constructs a WSMAN_DATA_BINARY object. This is used to send
            /// data to remote end.
            /// </summary>
            /// <param name="data"></param>
            internal WSManData_ManToUn(byte[] data)
            {
                Dbg.Assert(data != null, "Data cannot be null");

                _internalData = new WSManDataStruct();
                _internalData.binaryOrTextData = new WSManBinaryOrTextDataStruct();
                _internalData.binaryOrTextData.bufferLength = data.Length;
                _internalData.type = (uint)WSManDataType.WSMAN_DATA_TYPE_BINARY;

                IntPtr dataToSendPtr = Marshal.AllocHGlobal(_internalData.binaryOrTextData.bufferLength);
                _internalData.binaryOrTextData.data = dataToSendPtr;
                _marshalledBuffer = dataToSendPtr; // Stored directly to enable graceful clean up during finalizer scenarios

                Marshal.Copy(data, 0, _internalData.binaryOrTextData.data, _internalData.binaryOrTextData.bufferLength);
                _marshalledObject = Marshal.AllocHGlobal(Marshal.SizeOf<WSManDataStruct>());
                Marshal.StructureToPtr(_internalData, _marshalledObject, false);
            }

            /// <summary>
            /// Constructs a WSMAN_DATA_TEXT object. This is used to send data
            /// to remote end.
            /// </summary>
            /// <param name="data"></param>
            internal WSManData_ManToUn(string data)
            {
                Dbg.Assert(data != null, "Data cannot be null");

                _internalData = new WSManDataStruct();
                _internalData.binaryOrTextData = new WSManBinaryOrTextDataStruct();
                _internalData.binaryOrTextData.bufferLength = data.Length;
                _internalData.type = (uint)WSManDataType.WSMAN_DATA_TYPE_TEXT;

                // marshal text data
                _internalData.binaryOrTextData.data = Marshal.StringToHGlobalUni(data);
                _marshalledBuffer = _internalData.binaryOrTextData.data; // Stored directly to enable graceful clean up during finalizer scenarios
                _marshalledObject = Marshal.AllocHGlobal(Marshal.SizeOf<WSManDataStruct>());
                Marshal.StructureToPtr(_internalData, _marshalledObject, false);
            }

            /// <summary>
            /// Gets the type of data.
            /// </summary>
            internal uint Type
            {
                get { return _internalData.type; }

                set { _internalData.type = value; }
            }

            /// <summary>
            /// Gets the buffer length of data.
            /// </summary>
            internal int BufferLength
            {
                get { return _internalData.binaryOrTextData.bufferLength; }

                set { _internalData.binaryOrTextData.bufferLength = value; }
            }

            /// <summary>
            /// Free unmanaged resources. All users of this class should call Dispose rather than
            /// depending on the finalizer to clean it up.
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool isDisposing)
            {
                // Managed objects should not be deleted when this is called via the finalizer
                // because they may have been collected already. To prevent leaking the marshalledBuffer
                // pointer, we are storing its value as a private member of the class, just like marshalledObject.
                if (_marshalledBuffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_marshalledBuffer);
                    _marshalledBuffer = IntPtr.Zero;
                }

                if (_marshalledObject != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_marshalledObject);
                    _marshalledObject = IntPtr.Zero;
                }
            }

            /// <summary>
            /// Finalizes an instance of the <see cref="WSManData_ManToUn"/> class.
            /// </summary>
            ~WSManData_ManToUn()
            {
                Dispose(false);
            }

            /// <summary>
            /// Implicit IntPtr conversion.
            /// </summary>
            /// <param name="data"></param>
            /// <returns></returns>
            public static implicit operator IntPtr(WSManData_ManToUn data)
            {
                if (data != null)
                {
                    return data._marshalledObject;
                }
                else
                {
                    return IntPtr.Zero;
                }
            }
        }

        internal class WSManData_UnToMan
        {
            /// <summary>
            /// Gets the type of data.
            /// </summary>
            private uint _type;

            internal uint Type
            {
                get { return _type; }

                set { _type = value; }
            }

            /// <summary>
            /// Gets the buffer length of data.
            /// </summary>
            private int _bufferLength;

            internal int BufferLength
            {
                get { return _bufferLength; }

                set { _bufferLength = value; }
            }

            private string _text;

            internal string Text
            {
                get
                {
                    if (this.Type == (uint)WSManDataType.WSMAN_DATA_TYPE_TEXT)
                        return _text;
                    else
                        return string.Empty;
                }
            }

            private byte[] _data;

            internal byte[] Data
            {
                get
                {
                    if (this.Type == (uint)WSManDataType.WSMAN_DATA_TYPE_BINARY)
                        return _data;
                    else
                        return Array.Empty<byte>();
                }
            }

            /// <summary>
            /// Converts the unmanaged structure to a managed class object.
            /// </summary>
            /// <param name="dataStruct"></param>
            /// <returns></returns>
            internal static WSManData_UnToMan UnMarshal(WSManDataStruct dataStruct)
            {
                WSManData_UnToMan newData = new WSManData_UnToMan();

                newData._type = dataStruct.type;
                newData._bufferLength = dataStruct.binaryOrTextData.bufferLength;

                switch (dataStruct.type)
                {
                    case (uint)WSManNativeApi.WSManDataType.WSMAN_DATA_TYPE_TEXT:
                        if (dataStruct.binaryOrTextData.bufferLength > 0)
                        {
                            string tempText = Marshal.PtrToStringUni(dataStruct.binaryOrTextData.data, dataStruct.binaryOrTextData.bufferLength);
                            newData._text = tempText;
                        }

                        break;
                    case (uint)WSManNativeApi.WSManDataType.WSMAN_DATA_TYPE_BINARY:
                        if (dataStruct.binaryOrTextData.bufferLength > 0)
                        {
                            // copy data from unmanaged heap to managed heap.
                            byte[] dataRecvd = new byte[dataStruct.binaryOrTextData.bufferLength];
                            Marshal.Copy(
                                dataStruct.binaryOrTextData.data,
                                dataRecvd,
                                0,
                                dataStruct.binaryOrTextData.bufferLength);
                            newData._data = dataRecvd;
                        }

                        break;
                    default:
                        throw new NotSupportedException();
                }

                return newData;
            }

            /// <summary>
            /// Converts the unmanaged pointer to a managed class object.
            /// </summary>
            /// <param name="unmanagedData"></param>
            /// <returns></returns>
            internal static WSManData_UnToMan UnMarshal(IntPtr unmanagedData)
            {
                WSManData_UnToMan result = null;

                if (unmanagedData != IntPtr.Zero)
                {
                    WSManDataStruct resultInternal = Marshal.PtrToStructure<WSManDataStruct>(unmanagedData);
                    result = WSManData_UnToMan.UnMarshal(resultInternal);
                }

                return result;
            }
        }

        /// <summary>
        /// Used to supply a DWORD data in place of _WSMAN_DATA.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct WSManDataDWord
        {
            private readonly WSManDataType _type;
            private WSManDWordDataInternal _dwordData;

            /// <summary>
            /// Constructs a WSMAN_DATA_DWORD object.
            /// </summary>
            /// <param name="data"></param>
            internal WSManDataDWord(int data)
            {
                _dwordData = new WSManDWordDataInternal();
                _dwordData.number = data;
                _type = WSManDataType.WSMAN_DATA_TYPE_DWORD;
            }

            /// <summary>
            /// Creates an unmanaged ptr which holds the class data.
            /// This unmanaged ptr can be used with WSMan native API.
            /// </summary>
            /// <returns></returns>
            internal MarshalledObject Marshal()
            {
                return MarshalledObject.Create<WSManDataDWord>(this);
            }

            /// <summary>
            /// This struct is created to honor struct boundaries between
            /// x86,amd64 and ia64. WSMan defines a generic WSMAN_DATA
            /// structure that addresses DWORD, binary, text data.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            private struct WSManDWordDataInternal
            {
                internal int number;
                internal IntPtr reserved;
            }
        }

        #endregion

        #region WSManShellStartupInfo / WSManOptionSet / WSManCommandArgSet / WSManProxyInfo

        /// <summary>
        /// WSMan allows multiple streams within a shell but powershell is
        /// using only 1 stream for input and 1 stream for output to allow
        /// sequencing of data. Because of this the following structure will
        /// have only one string to hold stream information.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WSManStreamIDSetStruct
        {
            internal int streamIDsCount;

            [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
            internal IntPtr streamIDs;
        }

        internal struct WSManStreamIDSet_ManToUn
        {
            private WSManStreamIDSetStruct _streamSetInfo;
            private MarshalledObject _data;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="streamIds"></param>
            internal WSManStreamIDSet_ManToUn(string[] streamIds)
            {
                Dbg.Assert(streamIds != null, "stream ids cannot be null or empty");

                int sizeOfIntPtr = Marshal.SizeOf<IntPtr>();
                _streamSetInfo = new WSManStreamIDSetStruct();
                _streamSetInfo.streamIDsCount = streamIds.Length;
                _streamSetInfo.streamIDs = Marshal.AllocHGlobal(sizeOfIntPtr * streamIds.Length);
                for (int index = 0; index < streamIds.Length; index++)
                {
                    IntPtr streamAddress = Marshal.StringToHGlobalUni(streamIds[index]);
                    Marshal.WriteIntPtr(_streamSetInfo.streamIDs, index * sizeOfIntPtr, streamAddress);
                }

                _data = MarshalledObject.Create<WSManStreamIDSetStruct>(_streamSetInfo);
            }

            /// <summary>
            /// Free resources.
            /// </summary>
            internal void Dispose()
            {
                if (_streamSetInfo.streamIDs != IntPtr.Zero)
                {
                    int sizeOfIntPtr = Marshal.SizeOf<IntPtr>();
                    for (int index = 0; index < _streamSetInfo.streamIDsCount; index++)
                    {
                        IntPtr streamAddress = IntPtr.Zero;
                        streamAddress = Marshal.ReadIntPtr(_streamSetInfo.streamIDs, index * sizeOfIntPtr);

                        if (streamAddress != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(streamAddress);
                            streamAddress = IntPtr.Zero;
                        }
                    }

                    Marshal.FreeHGlobal(_streamSetInfo.streamIDs);
                    _streamSetInfo.streamIDs = IntPtr.Zero;
                }

                _data.Dispose();
            }

            /// <summary>
            /// Implicit cast to IntPtr.
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public static implicit operator IntPtr(WSManStreamIDSet_ManToUn obj)
            {
                return obj._data.DataPtr;
            }
        }

        internal class WSManStreamIDSet_UnToMan
        {
            internal string[] streamIDs;
            internal int streamIDsCount;

            /// <summary>
            /// Converts the unmanaged pointer to a managed class object.
            /// </summary>
            /// <param name="unmanagedData"></param>
            /// <returns></returns>
            internal static WSManStreamIDSet_UnToMan UnMarshal(IntPtr unmanagedData)
            {
                WSManStreamIDSet_UnToMan result = null;

                if (unmanagedData != IntPtr.Zero)
                {
                    WSManStreamIDSetStruct resultInternal = Marshal.PtrToStructure<WSManStreamIDSetStruct>(unmanagedData);

                    result = new WSManStreamIDSet_UnToMan();
                    string[] idsArray = null;
                    if (resultInternal.streamIDsCount > 0)
                    {
                        idsArray = new string[resultInternal.streamIDsCount];
                        IntPtr[] ptrs = new IntPtr[resultInternal.streamIDsCount];
                        Marshal.Copy(resultInternal.streamIDs, ptrs, 0, resultInternal.streamIDsCount); // Marshal the array of string pointers
                        for (int i = 0; i < resultInternal.streamIDsCount; i++)
                        {
                            idsArray[i] = Marshal.PtrToStringUni(ptrs[i]); // Marshal the string pointers into strings
                        }
                        /*
                         * // TODO: Why didn't this work? It looks more efficient
                        idsArray = new string[resultInternal.streamIDsCount];
                        int sizeInBytes = Marshal.SizeOf<IntPtr>();
                        IntPtr perElementPtr = resultInternal.streamIDs;

                        for (int i = 0; i < resultInternal.streamIDsCount; i++)
                        {
                            IntPtr p = IntPtr.Add(perElementPtr, (i * sizeInBytes));
                            idsArray[i] = Marshal.PtrToStringUni(p);
                        }
                         */
                    }

                    result.streamIDs = idsArray;
                    result.streamIDsCount = resultInternal.streamIDsCount;
                }

                return result;
            }
        }

        /// <summary>
        /// Managed to Unmanaged: Option struct used to pass optional information with WSManCreateShellEx .
        /// Unmanaged to Managed: Included in WSManPluginRequest.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WSManOption
        {
            /// <summary>
            /// Underlying type = PCWSTR.
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string name;
            /// <summary>
            /// Underlying type = PCWSTR.
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string value;
            /// <summary>
            /// Underlying type = BOOL.
            /// </summary>
            internal bool mustComply;
        }

        /// <summary>
        /// Unmanaged to Managed: WSMAN_OPERATION_INFO includes the struct directly, so this cannot be made internal to WsmanOptionSet.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WSManOptionSetStruct
        {
            internal int optionsCount;
            /// <summary>
            /// Pointer to an array of WSManOption objects.
            /// </summary>
            [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
            internal IntPtr options;

            internal bool optionsMustUnderstand;
        }

        /// <summary>
        /// Option set struct used to pass optional information
        /// with WSManCreateShellEx.
        /// </summary>
        internal struct WSManOptionSet : IDisposable
        {
            #region Managed to Unmanaged

            private WSManOptionSetStruct _optionSet;
            private MarshalledObject _data;

            /// <summary>
            /// Options to construct this OptionSet with.
            /// </summary>
            /// <param name="options"></param>
            internal WSManOptionSet(WSManOption[] options)
            {
                Dbg.Assert(options != null, "options cannot be null");

                int sizeOfOption = Marshal.SizeOf<WSManOption>();
                _optionSet = new WSManOptionSetStruct();
                _optionSet.optionsCount = options.Length;
                _optionSet.optionsMustUnderstand = true;
                _optionSet.options = Marshal.AllocHGlobal(sizeOfOption * options.Length);

                for (int index = 0; index < options.Length; index++)
                {
                    // Look at the structure of native WSManOptionSet.. Options is a pointer..
                    // In C-Style array individual elements are continuous..so I am building
                    // continuous array elements here.
                    Marshal.StructureToPtr(options[index], _optionSet.options + (sizeOfOption * index), false);
                }

                _data = MarshalledObject.Create<WSManOptionSetStruct>(_optionSet);

                // For conformity:
                this.optionsCount = 0;
                this.options = null;
                this.optionsMustUnderstand = false;
            }

            /// <summary>
            /// </summary>
            public void Dispose()
            {
                if (_optionSet.options != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_optionSet.options);
                    _optionSet.options = IntPtr.Zero;
                }
                // dispose option set
                _data.Dispose();
            }

            /// <summary>
            /// Implicit IntPtr cast.
            /// </summary>
            /// <param name="optionSet"></param>
            /// <returns></returns>
            public static implicit operator IntPtr(WSManOptionSet optionSet)
            {
                return optionSet._data.DataPtr;
            }

            #endregion

            #region Unmanaged to Managed

            internal int optionsCount;
            internal WSManOption[] options;
            internal bool optionsMustUnderstand;

            /// <summary>
            /// Converts the unmanaged pointer to a managed class object.
            /// </summary>
            /// <param name="unmanagedData"></param>
            /// <returns></returns>
            internal static WSManOptionSet UnMarshal(IntPtr unmanagedData)
            {
                if (unmanagedData == IntPtr.Zero)
                {
                    return new WSManOptionSet();
                }
                else
                {
                    WSManOptionSetStruct resultInternal = Marshal.PtrToStructure<WSManOptionSetStruct>(unmanagedData);
                    return UnMarshal(resultInternal);
                }
            }

            /// <summary>
            /// Converts the unmanaged structure to a managed class object.
            /// </summary>
            /// <param name="resultInternal"></param>
            /// <returns></returns>
            internal static WSManOptionSet UnMarshal(WSManOptionSetStruct resultInternal)
            {
                WSManOption[] tempOptions = null;
                if (resultInternal.optionsCount > 0)
                {
                    tempOptions = new WSManOption[resultInternal.optionsCount];

                    int sizeInBytes = Marshal.SizeOf<WSManOption>();
                    IntPtr perElementPtr = resultInternal.options;

                    for (int i = 0; i < resultInternal.optionsCount; i++)
                    {
                        IntPtr p = IntPtr.Add(perElementPtr, (i * sizeInBytes));
                        tempOptions[i] = Marshal.PtrToStructure<WSManOption>(p);
                    }
                }

                WSManOptionSet result = new WSManOptionSet();
                result.optionsCount = resultInternal.optionsCount;
                result.options = tempOptions;
                result.optionsMustUnderstand = resultInternal.optionsMustUnderstand;

                return result;
            }

            #endregion
        }

        /// <summary>
        /// </summary>
        internal struct WSManCommandArgSet : IDisposable
        {
            [StructLayout(LayoutKind.Sequential)]
            internal struct WSManCommandArgSetInternal
            {
                internal int argsCount;

                [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
                internal IntPtr args;
            }

            private WSManCommandArgSetInternal _internalData;

            [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
            private MarshalledObject _data;

            #region Managed to Unmanaged

            internal WSManCommandArgSet(byte[] firstArgument)
            {
                _internalData = new WSManCommandArgSetInternal();
                _internalData.argsCount = 1;
                _internalData.args = Marshal.AllocHGlobal(Marshal.SizeOf<IntPtr>());

                // argument set takes only strings..but powershell's serialized pipeline might contain
                // \0 (null characters) which are unacceptable in WSMan. So we are converting to Base64
                // here. The server will convert this back to original string.
                string base64EncodedArgument = Convert.ToBase64String(firstArgument);
                IntPtr firstArgAddress = Marshal.StringToHGlobalUni(base64EncodedArgument);
                Marshal.WriteIntPtr(_internalData.args, firstArgAddress);

                _data = MarshalledObject.Create<WSManCommandArgSet.WSManCommandArgSetInternal>(_internalData);

                // For conformity:
                this.args = null;
                this.argsCount = 0;
            }

            /// <summary>
            /// Free resources.
            /// </summary>
            public void Dispose()
            {
                IntPtr firstArgAddress = Marshal.ReadIntPtr(_internalData.args);
                if (firstArgAddress != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(firstArgAddress);
                }

                Marshal.FreeHGlobal(_internalData.args);

                _data.Dispose();
            }

            /// <summary>
            /// Implicit cast to IntPtr.
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public static implicit operator IntPtr(WSManCommandArgSet obj)
            {
                return obj._data.DataPtr;
            }

            #endregion

            #region Unmanaged to Managed

            internal string[] args;
            internal int argsCount;

            /// <summary>
            /// Since this is a structure, it must be non-null. This differs in behavior from all the other UnMarshals
            /// that are classes since they can be null.
            /// TODO: Do I need to worry about intermediate null characters in the arguments? The managed to unmanaged does!
            /// </summary>
            /// <param name="unmanagedData"></param>
            /// <returns></returns>
            internal static WSManCommandArgSet UnMarshal(IntPtr unmanagedData)
            {
                WSManCommandArgSet result = new WSManCommandArgSet();

                if (unmanagedData != IntPtr.Zero)
                {
                    WSManCommandArgSetInternal resultInternal = Marshal.PtrToStructure<WSManCommandArgSetInternal>(unmanagedData);

                    string[] tempArgs = null;
                    if (resultInternal.argsCount > 0)
                    {
                        tempArgs = new string[resultInternal.argsCount];
                        IntPtr[] ptrs = new IntPtr[resultInternal.argsCount];
                        Marshal.Copy(resultInternal.args, ptrs, 0, resultInternal.argsCount); // Marshal the array of string pointers
                        for (int i = 0; i < resultInternal.argsCount; i++)
                        {
                            tempArgs[i] = Marshal.PtrToStringUni(ptrs[i]); // Marshal the string pointers into strings
                        }
                    }

                    result.argsCount = resultInternal.argsCount;
                    result.args = tempArgs;
                }

                return result;
            }

            #endregion
        }

        internal struct WSManShellDisconnectInfo : IDisposable
        {
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private struct WSManShellDisconnectInfoInternal
            {
                /// <summary>
                /// New idletimeout for the server shell that overrides the original idletimeout specified in WSManCreateShell.
                /// </summary>
                internal uint idleTimeoutMs;
            }

            private WSManShellDisconnectInfoInternal _internalInfo;
            internal MarshalledObject data;

            #region Constructor / Other methods
            internal WSManShellDisconnectInfo(uint serverIdleTimeOut)
            {
                _internalInfo = new WSManShellDisconnectInfoInternal();
                _internalInfo.idleTimeoutMs = serverIdleTimeOut;
                data = MarshalledObject.Create<WSManShellDisconnectInfoInternal>(_internalInfo);
            }

            /// <summary>
            /// Disposes the object.
            /// </summary>
            public void Dispose()
            {
                data.Dispose();
            }

            /// <summary>
            /// Implicit IntPtr.
            /// </summary>
            /// <param name="disconnectInfo"></param>
            /// <returns></returns>
            public static implicit operator IntPtr(WSManShellDisconnectInfo disconnectInfo)
            {
                return disconnectInfo.data.DataPtr;
            }

            #endregion
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WSManShellStartupInfoStruct
        {
            /// <summary>
            /// PowerShell always uses one stream. So no need to expand this.
            /// Maps to WSManStreamIDSet.
            /// </summary>
            internal IntPtr inputStreamSet;
            /// <summary>
            /// PowerShell always uses one stream. So no need to expand this.
            /// Maps to WSManStreamIDSet.
            /// </summary>
            internal IntPtr outputStreamSet;
            /// <summary>
            /// Idle timeout.
            /// </summary>
            internal uint idleTimeoutMs;
            /// <summary>
            /// Working directory of the shell.
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string workingDirectory;
            /// <summary>
            /// Environment variables available to the shell.
            /// Maps to WSManEnvironmentVariableSet.
            /// </summary>
            internal IntPtr environmentVariableSet;
            /// <summary>
            /// Environment variables available to the shell.
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string name;
        }

        /// <summary>
        /// Managed to unmanaged representation of WSMAN_SHELL_STARTUP_INFO.
        /// It converts managed values into an unmanaged compatible WSManShellStartupInfoStruct that
        /// is marshalled into unmanaged memory.
        /// </summary>
        internal struct WSManShellStartupInfo_ManToUn : IDisposable
        {
            private WSManShellStartupInfoStruct _internalInfo;
            internal MarshalledObject data;

            #region Constructor / Other methods

            /// <summary>
            /// Creates a startup info with 1 startup option.
            /// The startup option is intended to specify the version.
            /// </summary>
            /// <param name="inputStreamSet">
            /// </param>
            /// <param name="outputStreamSet">
            /// </param>
            /// <param name="serverIdleTimeOut">
            /// </param>
            /// <param name="name">
            /// </param>
            internal WSManShellStartupInfo_ManToUn(WSManStreamIDSet_ManToUn inputStreamSet, WSManStreamIDSet_ManToUn outputStreamSet, uint serverIdleTimeOut, string name)
            {
                _internalInfo = new WSManShellStartupInfoStruct();
                _internalInfo.inputStreamSet = inputStreamSet;
                _internalInfo.outputStreamSet = outputStreamSet;
                _internalInfo.idleTimeoutMs = serverIdleTimeOut;
                // WSMan uses %USER_PROFILE% as the default working directory.
                _internalInfo.workingDirectory = null;
                _internalInfo.environmentVariableSet = IntPtr.Zero;
                _internalInfo.name = name;

                data = MarshalledObject.Create<WSManShellStartupInfoStruct>(_internalInfo);
            }

            /// <summary>
            /// Disposes the object.
            /// </summary>
            public void Dispose()
            {
                data.Dispose();
            }

            /// <summary>
            /// Implicit IntPtr.
            /// </summary>
            /// <param name="startupInfo"></param>
            /// <returns></returns>
            public static implicit operator IntPtr(WSManShellStartupInfo_ManToUn startupInfo)
            {
                return startupInfo.data.DataPtr;
            }

            #endregion
        }

        /// <summary>
        /// Unmanaged to managed representation of WSMAN_SHELL_STARTUP_INFO.
        /// It unmarshals the unmanaged struct into this object for use by managed code.
        /// </summary>
        internal class WSManShellStartupInfo_UnToMan
        {
            internal WSManStreamIDSet_UnToMan inputStreamSet;
            internal WSManStreamIDSet_UnToMan outputStreamSet;
            internal uint idleTimeoutMS;
            internal string workingDirectory;
            internal WSManEnvironmentVariableSet environmentVariableSet;
            internal string name;

            /// <summary>
            /// Converts the unmanaged pointer to a managed class object.
            /// </summary>
            /// <param name="unmanagedData"></param>
            /// <returns></returns>
            internal static WSManShellStartupInfo_UnToMan UnMarshal(IntPtr unmanagedData)
            {
                WSManShellStartupInfo_UnToMan result = null;

                if (unmanagedData != IntPtr.Zero)
                {
                    WSManShellStartupInfoStruct resultInternal = Marshal.PtrToStructure<WSManShellStartupInfoStruct>(unmanagedData);

                    result = new WSManShellStartupInfo_UnToMan();
                    result.inputStreamSet = WSManStreamIDSet_UnToMan.UnMarshal(resultInternal.inputStreamSet);
                    result.outputStreamSet = WSManStreamIDSet_UnToMan.UnMarshal(resultInternal.outputStreamSet);
                    result.idleTimeoutMS = resultInternal.idleTimeoutMs;
                    result.workingDirectory = resultInternal.workingDirectory; // TODO: Special marshaling required here?
                    result.environmentVariableSet = WSManEnvironmentVariableSet.UnMarshal(resultInternal.environmentVariableSet);
                    result.name = resultInternal.name;
                }

                return result;
            }
        }

        /// <summary>
        /// Managed representation of WSMAN_ENVIRONMENT_VARIABLE_SET.
        /// It wraps WSManEnvironmentVariableSetInternal and UnMarshals the unmanaged
        /// data into the object.
        /// </summary>
        internal class WSManEnvironmentVariableSet
        {
            internal uint varsCount;
            internal WSManEnvironmentVariableInternal[] vars;

            /// <summary>
            /// Converts the unmanaged pointer to a managed class object.
            /// </summary>
            /// <param name="unmanagedData"></param>
            /// <returns></returns>
            internal static WSManEnvironmentVariableSet UnMarshal(IntPtr unmanagedData)
            {
                WSManEnvironmentVariableSet result = null;

                if (unmanagedData != IntPtr.Zero)
                {
                    WSManEnvironmentVariableSetInternal resultInternal = Marshal.PtrToStructure<WSManEnvironmentVariableSetInternal>(unmanagedData);

                    result = new WSManEnvironmentVariableSet();
                    WSManEnvironmentVariableInternal[] varsArray = null;
                    if (resultInternal.varsCount > 0)
                    {
                        varsArray = new WSManEnvironmentVariableInternal[resultInternal.varsCount];
                        int sizeInBytes = Marshal.SizeOf<WSManEnvironmentVariableInternal>();
                        IntPtr perElementPtr = resultInternal.vars;

                        for (int i = 0; i < resultInternal.varsCount; i++)
                        {
                            IntPtr p = IntPtr.Add(perElementPtr, (i * sizeInBytes));
                            varsArray[i] = Marshal.PtrToStructure<WSManEnvironmentVariableInternal>(p);
                        }
                    }

                    result.vars = varsArray;
                    result.varsCount = resultInternal.varsCount;
                }

                return result;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private struct WSManEnvironmentVariableSetInternal
            {
                internal uint varsCount;
                internal IntPtr vars; // Array of WSManEnvironmentVariableInternal structs
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            internal struct WSManEnvironmentVariableInternal
            {
                [MarshalAs(UnmanagedType.LPWStr)]
                internal string name;

                [MarshalAs(UnmanagedType.LPWStr)]
                internal string value;
            }
        }

        /// <summary>
        /// Proxy Info used with WSManCreateSession.
        /// </summary>
        internal class WSManProxyInfo : IDisposable
        {
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private struct WSManProxyInfoInternal
            {
                public int proxyAccessType;
                public WSManUserNameAuthenticationCredentials.WSManUserNameCredentialStruct proxyAuthCredentialsStruct;
            }

            private MarshalledObject _data;

            /// <summary>
            /// </summary>
            /// <param name="proxyAccessType"></param>
            /// <param name="authCredentials"></param>
            internal WSManProxyInfo(ProxyAccessType proxyAccessType,
                WSManUserNameAuthenticationCredentials authCredentials)
            {
                WSManProxyInfoInternal internalInfo = new WSManProxyInfoInternal();
                internalInfo.proxyAccessType = (int)proxyAccessType;
                internalInfo.proxyAuthCredentialsStruct = new WSManUserNameAuthenticationCredentials.WSManUserNameCredentialStruct();
                internalInfo.proxyAuthCredentialsStruct.authenticationMechanism = WSManAuthenticationMechanism.WSMAN_FLAG_DEFAULT_AUTHENTICATION;

                if (authCredentials != null)
                {
                    internalInfo.proxyAuthCredentialsStruct = authCredentials.CredentialStruct;
                }

                _data = MarshalledObject.Create<WSManProxyInfoInternal>(internalInfo);
            }

            public void Dispose()
            {
                // data is of struct type..so no need to set it to null.
                _data.Dispose();
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// Implicit IntPtr.
            /// </summary>
            /// <param name="proxyInfo"></param>
            /// <returns></returns>
            public static implicit operator IntPtr(WSManProxyInfo proxyInfo)
            {
                return proxyInfo._data.DataPtr;
            }
        }

        #endregion

        #region WSMan Shell Async

        /// <summary>
        /// Flags used by all callback functions: WSMAN_COMPLETION_FUNCTION,
        /// WSMAN_SUBSCRIPTION_COMPLETION_FUNCTION and WSMAN_SHELL_COMPLETION_FUNCTION.
        /// </summary>
        internal enum WSManCallbackFlags
        {
            //
            // Flag that marks the end of any single step of multistep operation
            //
            WSMAN_FLAG_CALLBACK_END_OF_OPERATION = 0x1,

            //
            // WSMAN_SHELL_COMPLETION_FUNCTION API specific flags
            //  end of a particular stream; it is used for optimization purposes if the shell
            //  knows that no more output will occur for this stream; in some conditions this
            //  cannot be determined.
            //
            WSMAN_FLAG_CALLBACK_END_OF_STREAM = 0x8,

            //
            // Flag that if present on CreateShell callback indicates that it supports disconnect
            //
            WSMAN_FLAG_CALLBACK_SHELL_SUPPORTS_DISCONNECT = 0x20,

            //
            // Marks the end of an auto-disconnect operation.
            //
            WSMAN_FLAG_CALLBACK_SHELL_AUTODISCONNECTED = 0x40,

            //
            // Network failure notification.
            //
            WSMAN_FLAG_CALLBACK_NETWORK_FAILURE_DETECTED = 0x100,

            //
            // Network connection retry notification.
            //
            WSMAN_FLAG_CALLBACK_RETRYING_AFTER_NETWORK_FAILURE = 0x200,

            //
            // Network retry succeeded, connection re-established notification.
            //
            WSMAN_FLAG_CALLBACK_RECONNECTED_AFTER_NETWORK_FAILURE = 0x400,

            //
            // Retries failed, now auto-disconnecting.
            //
            WSMAN_FLAG_CALLBACK_SHELL_AUTODISCONNECTING = 0x800,

            //
            // Internal error during retries.  Cannot auto-disconnect.  Shell failure.
            //
            WSMAN_FLAG_CALLBACK_RETRY_ABORTED_DUE_TO_INTERNAL_ERROR = 0x1000,

            //
            // Flag that indicates for a receive operation that a delay stream request has been processed
            //
            WSMAN_FLAG_RECEIVE_DELAY_STREAM_REQUEST_PROCESSED = 0X2000
        }

        /// <summary>
        /// Completion function used by all Shell functions. Returns error->code != 0 upon error;
        /// use error->errorDetail structure for extended error informations; the callback is
        /// called for each shell operation; after a WSManReceiveShellOutput operation is initiated,
        /// the callback is called for each output stream element or if error; the underlying
        /// implementation handles the polling of stream data from the command or shell.
        /// If WSMAN_COMMAND_STATE_DONE state is received, no more streams will be received from the command,
        /// so the command can be closed using WSManCloseShellOperationEx(command).
        /// If error->code != 0, the result is guaranteed to be NULL. The error and result objects are
        /// allocated and owned by the WSMan client stack; they are valid during the callback only; the user
        /// has to synchronously copy the data in the callback. This callback function will use the current
        /// access token, whether it is a process or impersonation token.
        /// </summary>
        /// <param name="operationContext">
        /// user supplied operation context.
        /// </param>
        /// <param name="flags">
        /// one or more flags from WSManCallbackFlags
        /// </param>
        /// <param name="error">
        /// error allocated and owned by the winrm stack; valid in the callback only;
        /// </param>
        /// <param name="shellOperationHandle">
        /// shell handle associated with the user context
        /// </param>
        /// <param name="commandOperationHandle">
        /// command handle associated with the user context
        /// </param>
        /// <param name="operationHandle">
        /// operation handle associated with the user context
        /// </param>
        /// <param name="data">
        /// output data from command/shell; allocated internally and owned by the winrm stack.
        /// valid only within this function.
        /// See WSManReceiveDataResult.
        /// </param>
        internal delegate void WSManShellCompletionFunction(
            IntPtr operationContext,
            int flags,
            IntPtr error,
            IntPtr shellOperationHandle,
            IntPtr commandOperationHandle,
            IntPtr operationHandle,
            IntPtr data
            );

        /// <summary>
        /// Struct which holds reference to the callback(delegate) passed to WSMan
        /// API.
        /// </summary>
        internal struct WSManShellAsyncCallback
        {
            // GC handle which prevents garbage collector from collecting this delegate.
            private GCHandle _gcHandle;

            [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
            private readonly IntPtr _asyncCallback;

            internal WSManShellAsyncCallback(WSManShellCompletionFunction callback)
            {
                // if a delegate is re-located by a garbage collection, it will not affect
                // the underlaying managed callback, so Alloc is used to add a reference
                // to the delegate, allowing relocation of the delegate, but preventing
                // disposal. Using GCHandle without pinning reduces fragmentation potential
                // of the managed heap.
                _gcHandle = GCHandle.Alloc(callback);
                _asyncCallback = Marshal.GetFunctionPointerForDelegate(callback);
            }

            public static implicit operator IntPtr(WSManShellAsyncCallback callback)
            {
                return callback._asyncCallback;
            }
        }

        /// <summary>
        /// Used in different WSMan functions to supply async callback.
        /// </summary>
        internal class WSManShellAsync
        {
            [StructLayout(LayoutKind.Sequential)]
            internal struct WSManShellAsyncInternal
            {
                internal IntPtr operationContext;
                internal IntPtr asyncCallback;
            }

            private MarshalledObject _data;
            private WSManShellAsyncInternal _internalData;

            internal WSManShellAsync(IntPtr context, WSManShellAsyncCallback callback)
            {
                _internalData = new WSManShellAsyncInternal();
                _internalData.operationContext = context;

                _internalData.asyncCallback = callback;
                _data = MarshalledObject.Create<WSManShellAsyncInternal>(_internalData);
            }

            public void Dispose()
            {
                _data.Dispose();
            }

            public static implicit operator IntPtr(WSManShellAsync async)
            {
                return async._data;
            }
        }

        /// <summary>
        /// Used in the shell completion function delegate to refer to error.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WSManError
        {
            internal int errorCode;
            /// <summary>
            /// Extended error description from the fault;
            /// </summary>
            internal string errorDetail;
            /// <summary>
            /// Language for error description (RFC 3066 language code); it can be NULL.
            /// </summary>
            internal string language;
            /// <summary>
            /// Machine id; it can be NULL.
            /// </summary>
            internal string machineName;

            /// <summary>
            /// Constructs a WSManError from the unmanaged pointer.
            /// This involves copying data from unmanaged memory to managed heap.
            /// </summary>
            /// <param name="unmanagedData">
            /// Pointer to unmanaged data.
            /// </param>
            /// <returns>
            /// </returns>
            internal static WSManError UnMarshal(IntPtr unmanagedData)
            {
                return Marshal.PtrToStructure<WSManError>(unmanagedData);
            }
        }

        internal class WSManCreateShellDataResult
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct WSManCreateShellDataResultInternal
            {
                internal WSManDataStruct data;
            }

            internal string data;

            internal static WSManCreateShellDataResult UnMarshal(IntPtr unmanagedData)
            {
                WSManCreateShellDataResult result = new WSManCreateShellDataResult();

                if (unmanagedData != IntPtr.Zero)
                {
                    WSManCreateShellDataResultInternal resultInternal = Marshal.PtrToStructure<WSManCreateShellDataResultInternal>(unmanagedData);

                    string connectData = null;
                    if (resultInternal.data.textData.textLength > 0)
                    {
                        connectData = Marshal.PtrToStringUni(resultInternal.data.textData.text, resultInternal.data.textData.textLength);
                    }

                    result.data = connectData;
                }

                return result;
            }

            // The following structures are created to honor struct boundaries
            // on x86,amd64 and ia64
            [StructLayout(LayoutKind.Sequential)]
            private struct WSManDataStruct
            {
                internal uint type;
                internal WSManTextDataInternal textData;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct WSManTextDataInternal
            {
                internal int textLength;
                internal IntPtr text;
            }
        }

        internal class WSManConnectDataResult
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct WSManConnectDataResultInternal
            {
                internal WSManDataStruct data;
            }

            internal string data;

            internal static WSManConnectDataResult UnMarshal(IntPtr unmanagedData)
            {
                WSManConnectDataResultInternal resultInternal = Marshal.PtrToStructure<WSManConnectDataResultInternal>(unmanagedData);

                string connectData = null;
                if (resultInternal.data.textData.textLength > 0)
                {
                    connectData = Marshal.PtrToStringUni(resultInternal.data.textData.text, resultInternal.data.textData.textLength);
                }

                WSManConnectDataResult result = new WSManConnectDataResult();
                result.data = connectData;

                return result;
            }

            // The following structures are created to honor struct boundaries
            // on x86,amd64 and ia64
            [StructLayout(LayoutKind.Sequential)]
            private struct WSManDataStruct
            {
                internal uint type;
                internal WSManTextDataInternal textData;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct WSManTextDataInternal
            {
                internal int textLength;
                internal IntPtr text;
            }
        }
        /// <summary>
        /// Used in the shell completion function delegate to refer to the data.
        /// </summary>
        internal class WSManReceiveDataResult
        {
            /// <summary>
            /// The actual data.
            /// </summary>
            internal byte[] data;
            /// <summary>
            /// Stream the data belongs to.
            /// </summary>
            internal string stream;

            /// <summary>
            /// Constructs a WSManReceiveDataResult from the unmanaged pointer.
            /// This involves copying data from unmanaged memory to managed heap.
            /// Currently PowerShell supports only binary data on the wire, so this
            /// method asserts if the data is not binary.
            /// </summary>
            /// <param name="unmanagedData">
            /// Pointer to unmanaged data.
            /// </param>
            /// <returns>
            /// </returns>
            internal static WSManReceiveDataResult UnMarshal(IntPtr unmanagedData)
            {
                WSManReceiveDataResultInternal result1 =
                    Marshal.PtrToStructure<WSManReceiveDataResultInternal>(unmanagedData);

                // copy data from unmanaged heap to managed heap.
                byte[] dataRecvd = null;
                if (result1.data.binaryData.bufferLength > 0)
                {
                    dataRecvd = new byte[result1.data.binaryData.bufferLength];
                    Marshal.Copy(result1.data.binaryData.buffer,
                        dataRecvd,
                        0,
                        result1.data.binaryData.bufferLength);
                }

#if !UNIX
                Dbg.Assert(result1.data.type == (uint)WSManDataType.WSMAN_DATA_TYPE_BINARY,
                    "ReceiveDataResult can receive only binary data");
#endif

                WSManReceiveDataResult result = new WSManReceiveDataResult();
                result.data = dataRecvd;
                result.stream = result1.streamId;

                return result;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct WSManReceiveDataResultInternal
            {
                [MarshalAs(UnmanagedType.LPWStr)]
                internal string streamId;

                internal WSManDataStruct data;

                [MarshalAs(UnmanagedType.LPWStr)]
                internal string commandState;

                internal int exitCode;
            }

            // The following structures are created to honor struct boundaries
            // on x86,amd64 and ia64
            [StructLayout(LayoutKind.Sequential)]
            private struct WSManDataStruct
            {
                internal uint type;
                internal WSManBinaryDataInternal binaryData;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct WSManBinaryDataInternal
            {
                internal int bufferLength;
                internal IntPtr buffer;
            }
        }

        #endregion

        #region Plugin API Structure Definitions

        /// <summary>
        /// This is the managed representation of the WSMAN_PLUGIN_REQUEST struct.
        /// </summary>
        internal class WSManPluginRequest
        {
            /// <summary>
            /// Unmarshalled WSMAN_SENDER_DETAILS struct.
            /// </summary>
            internal WSManSenderDetails senderDetails;
            internal string locale;
            internal string resourceUri;
            /// <summary>
            /// Unmarshalled WSMAN_OPERATION_INFO struct.
            /// </summary>
            internal WSManOperationInfo operationInfo;

            /// <summary>
            /// Kept around to allow direct access to shutdownNotification and its handle.
            /// </summary>
            private WSManPluginRequestInternal _internalDetails;

            /// <summary>
            /// Volatile value that should be read directly from its unmanaged location.
            /// TODO: Does "volatile" still apply when accessing it in managed code.
            /// </summary>
            internal bool shutdownNotification
            {
                get { return _internalDetails.shutdownNotification; }
            }

            /// <summary>
            /// Left untouched in unmanaged memory because it is passed directly to
            /// RegisterWaitForSingleObject().
            /// </summary>
            internal IntPtr shutdownNotificationHandle
            {
                get { return _internalDetails.shutdownNotificationHandle; }
            }

            /// <summary>
            /// Copy of the unmanagedData value used to create the structure.
            /// </summary>
            internal IntPtr unmanagedHandle;

            /// <summary>
            /// Converts the unmanaged pointer to a managed class object.
            /// </summary>
            /// <param name="unmanagedData"></param>
            /// <returns></returns>
            internal static WSManPluginRequest UnMarshal(IntPtr unmanagedData)
            {
                // Dbg.Assert(IntPtr.Zero != unmanagedData, "unmanagedData must be non-null. This means WinRM sent a bad pointer.");
                WSManPluginRequest result = null;

                if (unmanagedData != IntPtr.Zero)
                {
                    WSManPluginRequestInternal resultInternal = Marshal.PtrToStructure<WSManPluginRequestInternal>(unmanagedData);

                    result = new WSManPluginRequest();
                    result.senderDetails = WSManSenderDetails.UnMarshal(resultInternal.senderDetails);
                    result.locale = resultInternal.locale;
                    result.resourceUri = resultInternal.resourceUri;
                    result.operationInfo = WSManOperationInfo.UnMarshal(resultInternal.operationInfo);
                    result._internalDetails = resultInternal;
                    result.unmanagedHandle = unmanagedData;
                }

                return result;
            }

            /// <summary>
            /// Representation of WSMAN_PLUGIN_REQUEST.
            /// </summary>
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private struct WSManPluginRequestInternal
            {
                /// <summary>
                /// WSManSenderDetails.
                /// </summary>
                internal IntPtr senderDetails;

                [MarshalAs(UnmanagedType.LPWStr)]
                internal string locale;

                [MarshalAs(UnmanagedType.LPWStr)]
                internal string resourceUri;
                /// <summary>
                /// WSManOperationInfo.
                /// </summary>
                internal IntPtr operationInfo;
                internal bool shutdownNotification;
                internal IntPtr shutdownNotificationHandle;
            }
        }

        internal class WSManSenderDetails
        {
            internal string senderName;
            internal string authenticationMechanism;
            internal WSManCertificateDetails certificateDetails;
            internal IntPtr clientToken; // TODO: How should this be marshalled?????
            internal string httpUrl;

            /// <summary>
            /// Converts the unmanaged pointer to a managed class object.
            /// </summary>
            /// <param name="unmanagedData"></param>
            /// <returns></returns>
            internal static WSManSenderDetails UnMarshal(IntPtr unmanagedData)
            {
                WSManSenderDetails result = null;

                if (unmanagedData != IntPtr.Zero)
                {
                    WSManSenderDetailsInternal resultInternal = Marshal.PtrToStructure<WSManSenderDetailsInternal>(unmanagedData);

                    result = new WSManSenderDetails();
                    result.senderName = resultInternal.senderName;
                    result.authenticationMechanism = resultInternal.authenticationMechanism;
                    result.certificateDetails = WSManCertificateDetails.UnMarshal(resultInternal.certificateDetails);
                    result.clientToken = resultInternal.clientToken; // TODO: UnMarshaling needed here!!!!
                    result.httpUrl = resultInternal.httpUrl;
                }

                return result;
            }

            /// <summary>
            /// Managed representation of WSMAN_SENDER_DETAILS.
            /// </summary>
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private struct WSManSenderDetailsInternal
            {
                [MarshalAs(UnmanagedType.LPWStr)]
                internal string senderName;

                [MarshalAs(UnmanagedType.LPWStr)]
                internal string authenticationMechanism;
                /// <summary>
                /// WSManCertificateDetails.
                /// </summary>
                internal IntPtr certificateDetails;
                internal IntPtr clientToken;

                [MarshalAs(UnmanagedType.LPWStr)]
                internal string httpUrl;
            }
        }

        internal class WSManCertificateDetails
        {
            internal string subject;
            internal string issuerName;
            internal string issuerThumbprint;
            internal string subjectName;

            /// <summary>
            /// Converts the unmanaged pointer to a managed class object.
            /// </summary>
            /// <param name="unmanagedData"></param>
            /// <returns></returns>
            internal static WSManCertificateDetails UnMarshal(IntPtr unmanagedData)
            {
                WSManCertificateDetails result = null;

                if (unmanagedData != IntPtr.Zero)
                {
                    WSManCertificateDetailsInternal resultInternal = Marshal.PtrToStructure<WSManCertificateDetailsInternal>(unmanagedData);

                    result = new WSManCertificateDetails();
                    result.subject = resultInternal.subject;
                    result.issuerName = resultInternal.issuerName;
                    result.issuerThumbprint = resultInternal.issuerThumbprint;
                    result.subjectName = resultInternal.subjectName;
                }

                return result;
            }
            /// <summary>
            /// Managed representation of WSMAN_CERTIFICATE_DETAILS.
            /// </summary>
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private struct WSManCertificateDetailsInternal
            {
                [MarshalAs(UnmanagedType.LPWStr)]
                internal string subject;

                [MarshalAs(UnmanagedType.LPWStr)]
                internal string issuerName;

                [MarshalAs(UnmanagedType.LPWStr)]
                internal string issuerThumbprint;

                [MarshalAs(UnmanagedType.LPWStr)]
                internal string subjectName;
            }
        }

        internal class WSManOperationInfo
        {
            internal WSManFragmentInternal fragment;
            internal WSManFilterInternal filter;
            internal WSManSelectorSet selectorSet;
            internal WSManOptionSet optionSet;

            /// <summary>
            /// Converts the unmanaged pointer to a managed class object.
            /// </summary>
            /// <param name="unmanagedData"></param>
            /// <returns></returns>
            internal static WSManOperationInfo UnMarshal(IntPtr unmanagedData)
            {
                WSManOperationInfo result = null;

                if (unmanagedData != IntPtr.Zero)
                {
                    WSManOperationInfoInternal resultInternal = Marshal.PtrToStructure<WSManOperationInfoInternal>(unmanagedData);

                    result = new WSManOperationInfo();
                    result.fragment = resultInternal.fragment;
                    result.filter = resultInternal.filter;
                    result.selectorSet = WSManSelectorSet.UnMarshal(resultInternal.selectorSet);
                    result.optionSet = WSManOptionSet.UnMarshal(resultInternal.optionSet);
                }

                return result;
            }
            /// <summary>
            /// Managed representation of WSMAN_OPERATION_INFO.
            /// selectorSet and optionSet are handled differently because they are structs that contain pointers to arrays of structs.
            /// Most other data structures in the API point to structures using IntPtr rather than including the actual structure.
            /// </summary>
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private struct WSManOperationInfoInternal
            {
                internal WSManFragmentInternal fragment;
                internal WSManFilterInternal filter;
                internal WSManSelectorSet.WSManSelectorSetStruct selectorSet;
                internal WSManOptionSetStruct optionSet;
            }

            /// <summary>
            /// Managed representation of WSMAN_FRAGMENT.
            /// </summary>
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            internal struct WSManFragmentInternal
            {
                [MarshalAs(UnmanagedType.LPWStr)]
                internal string path;

                [MarshalAs(UnmanagedType.LPWStr)]
                internal string dialect;
            }

            /// <summary>
            /// Managed representation of WSMAN_FILTER.
            /// </summary>
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            internal struct WSManFilterInternal
            {
                [MarshalAs(UnmanagedType.LPWStr)]
                internal string filter;

                [MarshalAs(UnmanagedType.LPWStr)]
                internal string dialect;
            }
        }

        internal class WSManSelectorSet
        {
            internal int numberKeys;
            internal WSManKeyStruct[] keys;

            /// <summary>
            /// Converts the unmanaged pointer to a managed class object.
            /// </summary>
            /// <param name="resultInternal"></param>
            /// <returns></returns>
            internal static WSManSelectorSet UnMarshal(WSManSelectorSetStruct resultInternal)
            {
                WSManKeyStruct[] tempKeys = null;
                if (resultInternal.numberKeys > 0)
                {
                    tempKeys = new WSManKeyStruct[resultInternal.numberKeys];
                    int sizeInBytes = Marshal.SizeOf<WSManKeyStruct>();
                    IntPtr perElementPtr = resultInternal.keys;

                    for (int i = 0; i < resultInternal.numberKeys; i++)
                    {
                        IntPtr p = IntPtr.Add(perElementPtr, (i * sizeInBytes));
                        tempKeys[i] = Marshal.PtrToStructure<WSManKeyStruct>(p);
                    }
                }

                WSManSelectorSet result = new WSManSelectorSet();
                result.numberKeys = resultInternal.numberKeys;
                result.keys = tempKeys;

                return result;
            }

            /// <summary>
            /// Managed representation of WSMAN_SELECTOR_SET.
            /// </summary>
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            internal struct WSManSelectorSetStruct
            {
                internal int numberKeys;
                /// <summary>
                /// Array of WSManKeyStruct structures.
                /// </summary>
                internal IntPtr keys;
            }

            /// <summary>
            /// Managed representation of WSMAN_OPTION_SET.
            /// </summary>
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            internal struct WSManKeyStruct
            {
                [MarshalAs(UnmanagedType.LPWStr)]
                internal string key;

                [MarshalAs(UnmanagedType.LPWStr)]
                internal string value;
            }
        }

        #endregion

        #region DllImports ClientAPI

#if !UNIX
        internal const string WSManClientApiDll = @"WsmSvc.dll";
        internal const string WSManProviderApiDll = @"WsmSvc.dll";
#else
        internal const string WSManClientApiDll = @"libpsrpclient";
        internal const string WSManProviderApiDll = @"libpsrpomiprov";
#endif

        /// <summary>
        /// This API is used to initialize the WinRM client;
        /// It can be used by different clients on the same process, ie svchost.exe.
        /// Returns a nonzero error code upon failure.
        /// </summary>
        /// <param name="flags">
        /// </param>
        /// <param name="wsManAPIHandle">
        /// </param>
        /// <returns>
        /// </returns>
        [DllImport(WSManNativeApi.WSManClientApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern int WSManInitialize(int flags,
          [In, Out] ref IntPtr wsManAPIHandle);

        /// <summary>
        /// This API deinitializes the Winrm client stack; all operations will
        /// finish before this API will return; this is a sync call;
        /// it is highly recommended that all operations are explicitly cancelled
        /// and all sessions are closed before calling this API
        /// Returns non zero error code upon failure.
        /// </summary>
        /// <param name="wsManAPIHandle"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        [DllImport(WSManNativeApi.WSManClientApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern int WSManDeinitialize(IntPtr wsManAPIHandle, int flags);

        /// <summary>
        /// Creates a session which can be used to perform subsequent operations
        /// Returns a non zero error code upon failure.
        /// </summary>
        /// <param name="wsManAPIHandle"></param>
        /// <param name="connection">
        /// if NULL, then connection will default to 127.0.0.1
        /// </param>
        /// <param name="flags"></param>
        /// <param name="authenticationCredentials">
        /// can be null.
        /// </param>
        /// <param name="proxyInfo">
        /// </param>
        /// <param name="wsManSessionHandle"></param>
        /// <returns></returns>
        [DllImport(WSManNativeApi.WSManClientApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern int WSManCreateSession(IntPtr wsManAPIHandle,
            [MarshalAs(UnmanagedType.LPWStr)] string connection,
            int flags,
            IntPtr authenticationCredentials,
            IntPtr proxyInfo,
            [In, Out] ref IntPtr wsManSessionHandle);

        /// <summary>
        /// Frees memory of session and closes all related operations before returning;
        /// this is sync call it is recommended that all pending operations are either
        /// completed or cancelled before calling this API. Returns a non zero error
        /// code upon failure.
        /// </summary>
        /// <param name="wsManSessionHandle"></param>
        /// <param name="flags"></param>
        [DllImport(WSManNativeApi.WSManClientApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern void WSManCloseSession(IntPtr wsManSessionHandle,
            int flags);

        /// <summary>
        /// WSManSetSessionOption API - set session options
        /// Returns a non zero error code upon failure.
        /// </summary>
        /// <param name="wsManSessionHandle"></param>
        /// <param name="option"></param>
        /// <param name="data">
        /// An int (DWORD) data.
        /// </param>
        /// <returns></returns>
        internal static int WSManSetSessionOption(IntPtr wsManSessionHandle,
            WSManSessionOption option,
            WSManDataDWord data)
        {
            MarshalledObject marshalObj = data.Marshal();
            using (marshalObj)
            {
                return WSManSetSessionOption(wsManSessionHandle, option, marshalObj.DataPtr);
            }
        }

        [DllImport(WSManNativeApi.WSManClientApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern int WSManSetSessionOption(IntPtr wsManSessionHandle,
            WSManSessionOption option,
            IntPtr data);

        /// <summary>
        /// WSManGetSessionOptionAsDword API - get a session option. Returns a non
        /// zero error code upon failure.
        /// </summary>
        /// <param name="wsManSessionHandle"></param>
        /// <param name="option"></param>
        /// <param name="value">
        /// An int (DWORD) data.
        /// </param>
        /// <returns>Zero on success, otherwise the error code.</returns>
        [DllImport(WSManNativeApi.WSManClientApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern int WSManGetSessionOptionAsDword(IntPtr wsManSessionHandle,
            WSManSessionOption option,
            out int value);

        /// <summary>
        /// Function that retrieves a WSMan session option as string. Thread.CurrentUICulture
        /// will be used as the language code to get the error message in.
        /// </summary>
        /// <param name="wsManAPIHandle"></param>
        /// <param name="option">Session option to get.</param>
        /// <returns></returns>
        internal static string WSManGetSessionOptionAsString(IntPtr wsManAPIHandle,
            WSManSessionOption option)
        {
            Dbg.Assert(wsManAPIHandle != IntPtr.Zero, "wsManAPIHandle cannot be null.");
            // The error code taken from winerror.h used for getting buffer length.
            const int ERROR_INSUFFICIENT_BUFFER = 122;

            string returnval = string.Empty;
            int bufferSize = 0;
            // calculate buffer size required
            if (WSManGetSessionOptionAsString(wsManAPIHandle,
                option, 0, null, out bufferSize) != ERROR_INSUFFICIENT_BUFFER)
            {
                return returnval;
            }
            // calculate space required to store output.
            // StringBuilder will not work for this case as CLR
            // does not copy the entire string if there are delimiters ('\0')
            // in the middle of a string.
            int bufferSizeInBytes = bufferSize * 2;
            byte[] msgBufferPtr = new byte[bufferSizeInBytes];

            // Now get the actual value
            int messageLength;
            if (WSManGetSessionOptionAsString(wsManAPIHandle,
                    option, bufferSizeInBytes, msgBufferPtr, out messageLength) != 0)
            {
                return returnval;
            }

            try
            {
                returnval = Encoding.Unicode.GetString(msgBufferPtr, 0, bufferSizeInBytes);
            }
            catch (ArgumentNullException)
            {
            }
            catch (System.Text.DecoderFallbackException)
            {
            }

            return returnval;
        }

        [DllImport(WSManNativeApi.WSManClientApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        private static extern int WSManGetSessionOptionAsString(IntPtr wsManSessionHandle,
            WSManSessionOption option,
            int optionLength,
            byte[] optionAsString,
            out int optionLengthUsed);

        /// <summary>
        /// Creates a shell on the remote end.
        /// </summary>
        /// <param name="wsManSessionHandle">
        /// Session in which the shell is created.
        /// </param>
        /// <param name="flags">
        /// </param>
        /// <param name="resourceUri">
        /// The resource Uri to use to create the shell.
        /// </param>
        /// <param name="shellId"></param>
        /// <param name="startupInfo">
        /// startup information to be passed to the shell.
        /// </param>
        /// <param name="optionSet">
        /// Options to be passed with CreateShell
        /// </param>
        /// <param name="openContent">
        /// any content that is used by the remote shell to startup.
        /// </param>
        /// <param name="asyncCallback">
        /// callback to notify when the create operation completes.
        /// </param>
        /// <param name="shellOperationHandle">
        /// An out parameter referencing a WSMan shell operation handle
        /// for this shell.
        /// </param>
        /// <returns></returns>
        internal static void WSManCreateShellEx(IntPtr wsManSessionHandle,
            int flags,
            string resourceUri,
            string shellId,
            WSManShellStartupInfo_ManToUn startupInfo,
            WSManOptionSet optionSet,
            WSManData_ManToUn openContent,
            IntPtr asyncCallback,
            ref IntPtr shellOperationHandle)
        {
            WSManCreateShellExInternal(wsManSessionHandle, flags, resourceUri, shellId, startupInfo, optionSet,
                    openContent, asyncCallback, ref shellOperationHandle);
        }

        [DllImport(WSManNativeApi.WSManClientApiDll, EntryPoint = "WSManCreateShellEx", SetLastError = false, CharSet = CharSet.Unicode)]
        private static extern void WSManCreateShellExInternal(IntPtr wsManSessionHandle,
            int flags,
            [MarshalAs(UnmanagedType.LPWStr)] string resourceUri,
            [MarshalAs(UnmanagedType.LPWStr)] string shellId,
            IntPtr startupInfo,
            IntPtr optionSet,
            IntPtr openContent,
            IntPtr asyncCallback,
            [In, Out] ref IntPtr shellOperationHandle);

        /// <summary>
        /// </summary>
        /// <param name="wsManSessionHandle"></param>
        /// <param name="flags"></param>
        /// <param name="resourceUri"></param>
        /// <param name="shellId"></param>
        /// <param name="optionSet"></param>
        /// <param name="connectXml"></param>
        /// <param name="asyncCallback"></param>
        /// <param name="shellOperationHandle"></param>
        [DllImport(WSManNativeApi.WSManClientApiDll, EntryPoint = "WSManConnectShell", SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern void WSManConnectShellEx(IntPtr wsManSessionHandle,
            int flags,
            [MarshalAs(UnmanagedType.LPWStr)] string resourceUri,
            [MarshalAs(UnmanagedType.LPWStr)] string shellId,
            IntPtr optionSet,
            IntPtr connectXml,
            IntPtr asyncCallback,
            [In, Out] ref IntPtr shellOperationHandle);

        /// <summary>
        /// </summary>
        /// <param name="wsManSessionHandle"></param>
        /// <param name="flags"></param>
        /// <param name="disconnectInfo"></param>
        /// <param name="asyncCallback"></param>
        [DllImport(WSManNativeApi.WSManClientApiDll, EntryPoint = "WSManDisconnectShell", SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern void WSManDisconnectShellEx(IntPtr wsManSessionHandle,
            int flags,
            IntPtr disconnectInfo,
            IntPtr asyncCallback);

        /// <summary>
        /// </summary>
        /// <param name="wsManSessionHandle"></param>
        /// <param name="flags"></param>
        /// <param name="asyncCallback"></param>
        [DllImport(WSManNativeApi.WSManClientApiDll, EntryPoint = "WSManReconnectShell", SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern void WSManReconnectShellEx(IntPtr wsManSessionHandle,
            int flags,
            IntPtr asyncCallback);

        [DllImport(WSManNativeApi.WSManClientApiDll, EntryPoint = "WSManReconnectShellCommand", SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern void WSManReconnectShellCommandEx(IntPtr wsManCommandHandle,
            int flags,
            IntPtr asyncCallback);

        /// <summary>
        /// Starts a command on the remote end.
        /// </summary>
        /// <param name="shellOperationHandle">
        /// Shell handle in which the command is created and run.
        /// </param>
        /// <param name="flags"></param>
        /// <param name="commandId"></param>
        /// <param name="commandLine">
        /// command line for the command.
        /// </param>
        /// <param name="commandArgSet">
        /// arguments for the command.
        /// </param>
        /// <param name="optionSet">
        /// options.
        /// </param>
        /// <param name="asyncCallback">
        /// callback to notify when the operation completes.
        /// </param>
        /// <param name="commandOperationHandle">
        /// An out parameter referencing a WSMan shell operation handle
        /// for this command.
        /// </param>
        [DllImport(WSManNativeApi.WSManClientApiDll, EntryPoint = "WSManRunShellCommandEx", SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern void WSManRunShellCommandEx(IntPtr shellOperationHandle,
            int flags,
            [MarshalAs(UnmanagedType.LPWStr)]
            string commandId,
            [MarshalAs(UnmanagedType.LPWStr)]
            string commandLine,
            IntPtr commandArgSet,
            IntPtr optionSet,
            IntPtr asyncCallback,
            ref IntPtr commandOperationHandle);

        [DllImport(WSManNativeApi.WSManClientApiDll, EntryPoint = "WSManConnectShellCommand", SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern void WSManConnectShellCommandEx(IntPtr shellOperationHandle,
            int flags,
            [MarshalAs(UnmanagedType.LPWStr)]
            string commandID,
            IntPtr optionSet,
            IntPtr connectXml,
            IntPtr asyncCallback,
            ref IntPtr commandOperationHandle);

        /// <summary>
        /// Registers a callback with WSMan to receive output from the remote end.
        /// If commandOperationHandle is null, then the receive callback is registered
        /// for shell. It is enough to register the callback only once. WSMan will
        /// keep on calling this callback as and when it has data for a particular
        /// command + shell. There will be only 1 callback active per command or per shell.
        /// So if there are multiple commands active, then there can be 1 callback active
        /// for each of them.
        /// TODO: How to unregister the callback.
        /// </summary>
        /// <param name="shellOperationHandle">
        /// Shell Operation Handle.
        /// </param>
        /// <param name="commandOperationHandle">
        /// Command Operation Handle. If null, the receive request corresponds
        /// to the shell.
        /// </param>
        /// <param name="flags"></param>
        /// <param name="desiredStreamSet"></param>
        /// <param name="asyncCallback">
        /// callback which receives the data asynchronously.
        /// </param>
        /// <param name="receiveOperationHandle">
        /// handle to use to cancel the operation.
        /// </param>
        [DllImport(WSManNativeApi.WSManClientApiDll, EntryPoint = "WSManReceiveShellOutput", SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern void WSManReceiveShellOutputEx(IntPtr shellOperationHandle,
            IntPtr commandOperationHandle,
            int flags,
            IntPtr desiredStreamSet,
            IntPtr asyncCallback,
            [In, Out] ref IntPtr receiveOperationHandle);

        /// <summary>
        /// Send data to the remote end.
        /// </summary>
        /// <param name="shellOperationHandle">
        /// Shell Operation Handle.
        /// </param>
        /// <param name="commandOperationHandle">
        /// Command Operation Handle. If null, the send request corresponds
        /// to the shell.
        /// </param>
        /// <param name="flags"></param>
        /// <param name="streamId"></param>
        /// <param name="streamData"></param>
        /// <param name="asyncCallback">
        /// callback to notify when the operation completes.
        /// </param>
        /// <param name="sendOperationHandle">
        /// handle to use to cancel the operation.
        /// </param>
        internal static void WSManSendShellInputEx(IntPtr shellOperationHandle,
            IntPtr commandOperationHandle,
            int flags,
            [MarshalAs(UnmanagedType.LPWStr)] string streamId,
            WSManData_ManToUn streamData,
            IntPtr asyncCallback,
            ref IntPtr sendOperationHandle)
        {
            WSManSendShellInputExInternal(shellOperationHandle, commandOperationHandle, flags, streamId,
                    streamData, false, asyncCallback, ref sendOperationHandle);
        }

        [DllImport(WSManNativeApi.WSManClientApiDll, EntryPoint = "WSManSendShellInput", SetLastError = false, CharSet = CharSet.Unicode)]
        private static extern void WSManSendShellInputExInternal(IntPtr shellOperationHandle,
            IntPtr commandOperationHandle,
            int flags,
            [MarshalAs(UnmanagedType.LPWStr)] string streamId,
            IntPtr streamData,
            bool endOfStream,
            IntPtr asyncCallback,
            [In, Out] ref IntPtr sendOperationHandle);

        /// <summary>
        /// Closes a shell or a command; if the callback associated with the operation
        /// is pending and have not completed when WSManCloseShellOperationEx is called,
        /// the function waits for the callback to finish; If the operation was not finished,
        /// the operation is cancelled and the operation callback is called with
        /// WSMAN_ERROR_OPERATION_ABORTED error; then the WSManCloseShellOperationEx callback
        /// is called with WSMAN_FLAG_CALLBACK_END_OF_OPERATION flag as result of this operation.
        /// </summary>
        /// <param name="shellHandle">
        /// Shell handle to Close.
        /// </param>
        /// <param name="flags"></param>
        /// <param name="asyncCallback">
        /// callback to notify when the operation completes.
        /// </param>
        [DllImport(WSManNativeApi.WSManClientApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern void WSManCloseShell(IntPtr shellHandle,
            int flags,
            IntPtr asyncCallback);

        /// <summary>
        /// Closes a command (signals the termination of a command); the WSManCloseCommand callback
        /// is called with WSMAN_FLAG_CALLBACK_END_OF_OPERATION flag as result of this operation.
        /// </summary>
        /// <param name="cmdHandle">
        /// Command handle to Close.
        /// </param>
        /// <param name="flags"></param>
        /// <param name="asyncCallback">
        /// callback to notify when the operation completes.
        /// </param>
        [DllImport(WSManNativeApi.WSManClientApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern void WSManCloseCommand(IntPtr cmdHandle,
            int flags,
            IntPtr asyncCallback);

        /// <summary>
        /// Sends a signal. If <paramref name="cmdOperationHandle"/> is null, then the signal will
        /// be sent to shell.
        /// </summary>
        /// <param name="shellOperationHandle"></param>
        /// <param name="cmdOperationHandle"></param>
        /// <param name="flags"></param>
        /// <param name="code"></param>
        /// <param name="asyncCallback"></param>
        /// <param name="signalOperationHandle"></param>
        [DllImport(WSManNativeApi.WSManClientApiDll, EntryPoint = "WSManSignalShell", SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern void WSManSignalShellEx(IntPtr shellOperationHandle,
            IntPtr cmdOperationHandle,
            int flags,
            string code,
            IntPtr asyncCallback,
            [In, Out] ref IntPtr signalOperationHandle);

        /// <summary>
        /// Closes an asynchronous operation; if the callback associated with the operation
        /// is pending and have not completed when WSManCloseOperation is called, then
        /// the function marks the operation for deletion and returns; If the callback was not called,
        /// the operation is cancelled and the operation callback is called with
        /// WSMAN_ERROR_OPERATION_ABORTED error; the operation handle is freed in all cases
        /// after the callback returns.
        /// </summary>
        /// <param name="operationHandle"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        [DllImport(WSManNativeApi.WSManClientApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern void WSManCloseOperation(IntPtr operationHandle, int flags);

        /// <summary>
        /// Function that retrieves WSMan error messages with a particular error code. Thread.CurrentUICulture
        /// will be used as the language code to get the error message in.
        /// </summary>
        /// <param name="wsManAPIHandle"></param>
        /// <param name="errorCode"></param>
        /// <returns></returns>
        internal static string WSManGetErrorMessage(IntPtr wsManAPIHandle, int errorCode)
        {
            Dbg.Assert(wsManAPIHandle != IntPtr.Zero, "wsManAPIHandle cannot be null.");

            // The error code taken from winerror.h used for getting buffer length.
            const int ERROR_INSUFFICIENT_BUFFER = 122;

            // get language code.
            string langCode = CultureInfo.CurrentUICulture.Name;

            string returnval = string.Empty;
            int bufferSize = 0;
            // calculate buffer size required
            if (WSManGetErrorMessage(wsManAPIHandle,
                    0, langCode, errorCode, 0, null, out bufferSize) != ERROR_INSUFFICIENT_BUFFER)
            {
                return returnval;
            }
            // calculate space required to store output.
            // StringBuilder will not work for this case as CLR
            // does not copy the entire string if there are delimiters ('\0')
            // in the middle of a string.
            int bufferSizeInBytes = bufferSize * 2;
            byte[] msgBufferPtr = new byte[bufferSizeInBytes];

            // Now get the actual value
            int messageLength;
            if (WSManGetErrorMessage(wsManAPIHandle,
                    0, langCode, errorCode, bufferSizeInBytes, msgBufferPtr, out messageLength) != 0)
            {
                return returnval;
            }

            try
            {
                returnval = Encoding.Unicode.GetString(msgBufferPtr, 0, bufferSizeInBytes);
            }
            catch (ArgumentNullException)
            {
            }
            catch (System.Text.DecoderFallbackException)
            {
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            return returnval;
        }

        /// <summary>
        /// Function that retrieves WSMan error messages with a particular error code and a language code.
        /// </summary>
        /// <param name="wsManAPIHandle">
        /// The handle returned by WSManInitialize API call. It cannot be NULL.
        /// </param>
        /// <param name="flags">
        /// Reserved for future use. It must be 0.
        /// </param>
        /// <param name="languageCode">
        /// Defines the RFC 3066 language code name that should be used to localize the error. It can be NULL.
        /// if not specified, the thread's UI language will be used.
        /// </param>
        /// <param name="errorCode">
        /// Represents the error code for the requested error message. This error code can be a hexadecimal or
        /// decimal component from WSManagement component, WinHttp component or other Windows operating system
        /// components.
        /// </param>
        /// <param name="messageLength">
        /// Represents the size of the output message buffer in characters, including the NULL terminator.
        /// If 0, then the "message" parameter must be NULL; in this case the function will return
        /// ERROR_INSUFFICIENT_BUFFER error and the "messageLengthUsed" parameter will be set to the number
        /// of characters needed, including NULL terminator.
        /// </param>
        /// <param name="message">
        /// Represents the output buffer to store the message in. It must be allocated/deallocated by the client.
        /// The buffer must be big enough to store the message plus the NULL terminator otherwise an
        /// ERROR_INSUFFICIENT_BUFFER error will be returned and the "messageLengthUsed" parameter will be set
        /// to the number of characters needed, including NULL terminator. If NULL, then the "messageLength" parameter
        /// must be NULL; in this case the function will return ERROR_INSUFFICIENT_BUFFER error and the "messageLengthUsed"
        /// parameter will be set to the number of characters needed, including NULL terminator.
        /// </param>
        /// <param name="messageLengthUsed">
        /// Represents the effective number of characters written to the output buffer, including the NULL terminator.
        /// It cannot be NULL. If both "messageLength" and "message" parameters are 0, the function will return ERROR_INSUFFICIENT_BUFFER
        /// and "messageLengthUsed" parameter will be set to the number of characters needed, including NULL terminator
        /// </param>
        [DllImport(WSManNativeApi.WSManClientApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern int WSManGetErrorMessage(IntPtr wsManAPIHandle,
            int flags,
            string languageCode,
            int errorCode,
            int messageLength,
            byte[] message,
            out int messageLengthUsed);

        #endregion

        #region DllImports PluginAPI

        /// <summary>
        /// Gets operational information for items such as time-outs and data restrictions that
        /// are associated with the operation.
        /// </summary>
        /// <param name="requestDetails">Specifies the resource URI, options, locale, shutdown flag, and handle for the request.</param>
        /// <param name="flags">Specifies the options that are available for retrieval.</param>
        /// <param name="data">Specifies the result object (WSMAN_DATA).</param>
        /// <returns></returns>
        [DllImport(WSManNativeApi.WSManProviderApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern int WSManPluginGetOperationParameters(
            IntPtr requestDetails,
            int flags,
            [In, Out, MarshalAs(UnmanagedType.LPStruct)] WSManDataStruct data);
        // [In, Out] ref IntPtr data);

        /// <summary>
        /// Reports the completion of an operation by all operation entry points except for the
        /// WSManPluginStartup and WSManPluginShutdown methods.
        /// </summary>
        /// <param name="requestDetails">Specifies the resource URI, options, locale, shutdown flag, and handle for the request.</param>
        /// <param name="flags"></param>
        /// <param name="errorCode">Reports any failure in the operation. Terminates on non-NO_ERROR status.</param>
        /// <param name="extendedInformation">XML document containing extra error information.</param>
        /// <returns></returns>
        [DllImport(WSManNativeApi.WSManProviderApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern int WSManPluginOperationComplete(
            IntPtr requestDetails,
            int flags,
            int errorCode,
            [MarshalAs(UnmanagedType.LPWStr)] string extendedInformation);

        internal enum WSManFlagReceive : int
        {
            /// <summary>
            /// No more data on this stream.  Only valid when a stream is specified.
            /// </summary>
            WSMAN_FLAG_RECEIVE_RESULT_NO_MORE_DATA = 1,
            /// <summary>
            /// Send the data as soon as possible.  Normally data is held onto in
            /// order to maximise the size of the response packet.  This should
            /// only be used if a request/response style of data is needed between
            /// the send and receive data streams.
            /// </summary>
            WSMAN_FLAG_RECEIVE_FLUSH = 2,
            /// <summary>
            /// Data reported is at a boundary. Plugins usually serialize and fragment
            /// output data objects and push them along the receive byte stream.
            /// If the current data chunk being reported is an end fragment of the
            /// data object current processed, plugins would set this flag.
            /// </summary>
            WSMAN_FLAG_RECEIVE_RESULT_DATA_BOUNDARY = 4
        }

        internal const string WSMAN_SHELL_NAMESPACE = "http://schemas.microsoft.com/wbem/wsman/1/windows/shell";
        internal const string WSMAN_COMMAND_STATE_DONE = WSMAN_SHELL_NAMESPACE + "/CommandState/Done";
        internal const string WSMAN_COMMAND_STATE_PENDING = WSMAN_SHELL_NAMESPACE + "/CommandState/Pending";
        internal const string WSMAN_COMMAND_STATE_RUNNING = WSMAN_SHELL_NAMESPACE + "/CommandState/Running";

        /// <summary>
        /// Reports results for the WSMAN_PLUGIN_RECEIVE plug-in call and is used by most shell
        /// plug-ins that return results. After all of the data is received, the
        /// WSManPluginOperationComplete method must be called.
        /// </summary>
        /// <param name="requestDetails">Specifies the resource URI, options, locale, shutdown flag, and handle for the request.</param>
        /// <param name="flags"></param>
        /// <param name="stream">Specifies the stream that the data is associated with.</param>
        /// <param name="streamResult">A pointer to a WSMAN_DATA structure that specifies the result object that is returned to the client.</param>
        /// <param name="commandState">Specifies the state of the command. It must be set to a value specified by the plugin.</param>
        /// <param name="exitCode">Only set when the commandState is terminating.</param>
        /// <returns></returns>
        [DllImport(WSManNativeApi.WSManProviderApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern int WSManPluginReceiveResult(
            IntPtr requestDetails,
            int flags,
            [MarshalAs(UnmanagedType.LPWStr)] string stream,
            IntPtr streamResult,
            [MarshalAs(UnmanagedType.LPWStr)] string commandState,
            int exitCode);

        /// <summary>
        /// Reports shell and command context back to the Windows Remote Management (WinRM)
        /// infrastructure so that further operations can be performed against the shell and/or
        /// command. This method is called only for WSManPluginShell and WSManPluginCommand plug-in
        /// entry points.
        /// </summary>
        /// <param name="requestDetails">Specifies the resource URI, options, locale, shutdown flag, and handle for the request.</param>
        /// <param name="flags"></param>
        /// <param name="context">Defines the value to pass into all future shell and command operations. Represents either the shell or the command.</param>
        /// <returns></returns>
        [DllImport(WSManNativeApi.WSManProviderApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern int WSManPluginReportContext(
            IntPtr requestDetails,
            int flags,
            IntPtr context);
#if UNIX
        /// <summary>
        /// Registers the shutdown callback.
        /// </summary>
        /// <param name="requestDetails">Specifies the resource URI, options, locale, shutdown flag, and handle for the request.</param>
        /// <param name="shutdownCallback">Callback to be executed on shutdown.</param>
        /// <param name="shutdownContext"></param>
        /// <returns></returns>
        [DllImport(WSManNativeApi.WSManProviderApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern void WSManPluginRegisterShutdownCallback(
            IntPtr requestDetails,
            IntPtr shutdownCallback,
            IntPtr shutdownContext);
#endif
        #endregion
    }

    /// <summary>
    /// Interface to enable stubbing of the WSManNativeApi PInvoke calls for
    /// unit testing.
    /// Note: It is implemented as a class to avoid exposing it outside the module.
    /// </summary>
#nullable enable
    internal interface IWSManNativeApiFacade
    {
        // TODO: Expand this to cover the rest of the API once I prove that it works!

        int WSManPluginGetOperationParameters(
            IntPtr requestDetails,
            int flags,
            WSManNativeApi.WSManDataStruct data);

        int WSManPluginOperationComplete(
            IntPtr requestDetails,
            int flags,
            int errorCode,
            string extendedInformation);

        int WSManPluginReceiveResult(
            IntPtr requestDetails,
            int flags,
            string? stream,
            IntPtr streamResult,
            string commandState,
            int exitCode);

        int WSManPluginReportContext(
            IntPtr requestDetails,
            int flags,
            IntPtr context);

        void WSManPluginRegisterShutdownCallback(
            IntPtr requestDetails,
            IntPtr shutdownCallback,
            IntPtr shutdownContext);
    }
#nullable restore

    /// <summary>
    /// Concrete implementation of the PInvoke facade for use in the production code.
    /// </summary>
    internal class WSManNativeApiFacade : IWSManNativeApiFacade
    {
        int IWSManNativeApiFacade.WSManPluginGetOperationParameters(
            IntPtr requestDetails,
            int flags,
            WSManNativeApi.WSManDataStruct data)
        {
            return WSManNativeApi.WSManPluginGetOperationParameters(requestDetails, flags, data);
        }

        int IWSManNativeApiFacade.WSManPluginOperationComplete(
            IntPtr requestDetails,
            int flags,
            int errorCode,
            string extendedInformation)
        {
            return WSManNativeApi.WSManPluginOperationComplete(requestDetails, flags, errorCode, extendedInformation);
        }

        int IWSManNativeApiFacade.WSManPluginReceiveResult(
            IntPtr requestDetails,
            int flags,
            string stream,
            IntPtr streamResult,
            string commandState,
            int exitCode)
        {
            return WSManNativeApi.WSManPluginReceiveResult(requestDetails, flags, stream, streamResult, commandState, exitCode);
        }

        int IWSManNativeApiFacade.WSManPluginReportContext(
            IntPtr requestDetails,
            int flags,
            IntPtr context)
        {
            return WSManNativeApi.WSManPluginReportContext(requestDetails, flags, context);
        }

        void IWSManNativeApiFacade.WSManPluginRegisterShutdownCallback(
            IntPtr requestDetails,
            IntPtr shutdownCallback,
            IntPtr shutdownContext)
        {
#if UNIX
            WSManNativeApi.WSManPluginRegisterShutdownCallback(requestDetails, shutdownCallback, shutdownContext);
#endif
        }
    }
}
