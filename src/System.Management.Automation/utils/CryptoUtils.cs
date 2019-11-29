// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Remoting;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Threading;

using Microsoft.Win32.SafeHandles;

using Dbg = System.Management.Automation.Diagnostics;
using System.Security.Cryptography;
using System.IO;

namespace System.Management.Automation.Internal
{
    /// <summary>
    /// This class provides the wrapper for all Native CAPI functions.
    /// </summary>
    internal class PSCryptoNativeUtils
    {
        #region Functions

        #endregion Functions

        #region Constants

        /// <summary>
        /// Do not use persisted private key.
        /// </summary>
        public const uint CRYPT_VERIFYCONTEXT = 0xF0000000;

        /// <summary>
        /// Mark the key for export.
        /// </summary>
        public const uint CRYPT_EXPORTABLE = 0x00000001;

        /// <summary>
        /// Automatically assign a salt value when creating a
        /// session key.
        /// </summary>
        public const int CRYPT_CREATE_SALT = 4;

        /// <summary>
        /// RSA Provider.
        /// </summary>
        public const int PROV_RSA_FULL = 1;

        /// <summary>
        /// RSA Provider that supports AES
        /// encryption.
        /// </summary>
        public const int PROV_RSA_AES = 24;

        /// <summary>
        /// Public key to be used for encryption.
        /// </summary>
        public const int AT_KEYEXCHANGE = 1;

        /// <summary>
        /// RSA Key.
        /// </summary>
        public const int CALG_RSA_KEYX =
            (PSCryptoNativeUtils.ALG_CLASS_KEY_EXCHANGE |
            (PSCryptoNativeUtils.ALG_TYPE_RSA | PSCryptoNativeUtils.ALG_SID_RSA_ANY));

        /// <summary>
        /// Create a key for encryption.
        /// </summary>
        public const int ALG_CLASS_KEY_EXCHANGE = (5) << (13);

        /// <summary>
        /// Create a RSA key pair.
        /// </summary>
        public const int ALG_TYPE_RSA = (2) << (9);

        /// <summary>
        /// </summary>
        public const int ALG_SID_RSA_ANY = 0;

        /// <summary>
        /// Option for exporting public key blob.
        /// </summary>
        public const int PUBLICKEYBLOB = 6;

        /// <summary>
        /// Option for exporting a session key.
        /// </summary>
        public const int SIMPLEBLOB = 1;

        /// <summary>
        /// AES 256 symmetric key.
        /// </summary>
        public const int CALG_AES_256 = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | ALG_SID_AES_256);

        /// <summary>
        /// ALG_CLASS_DATA_ENCRYPT.
        /// </summary>
        public const int ALG_CLASS_DATA_ENCRYPT = (3) << (13);

        /// <summary>
        /// ALG_TYPE_BLOCK.
        /// </summary>
        public const int ALG_TYPE_BLOCK = (3) << (9);

        /// <summary>
        /// ALG_SID_AES_256 -> 16.
        /// </summary>
        public const int ALG_SID_AES_256 = 16;

        /// CALG_AES_128 -> (ALG_CLASS_DATA_ENCRYPT|ALG_TYPE_BLOCK|ALG_SID_AES_128)
        public const int CALG_AES_128 = (ALG_CLASS_DATA_ENCRYPT
                    | (ALG_TYPE_BLOCK | ALG_SID_AES_128));

        /// ALG_SID_AES_128 -> 14
        public const int ALG_SID_AES_128 = 14;

        #endregion Constants
    }

    /// <summary>
    /// Defines a custom exception which is thrown when
    /// a native CAPI call results in an error.
    /// </summary>
    /// <remarks>This exception is currently internal as it's not
    /// surfaced to the user. However, if we decide to surface errors
    /// to the user when something fails on the remote end, then this
    /// can be turned public</remarks>
    [SuppressMessage("Microsoft.Design", "CA1064:ExceptionsShouldBePublic")]
    [Serializable]
    internal class PSCryptoException : Exception
    {
        #region Private Members

        private uint _errorCode;

        #endregion Private Members

        #region Internal Properties

        /// <summary>
        /// Error code returned by the native CAPI call.
        /// </summary>
        internal uint ErrorCode
        {
            get
            {
                return _errorCode;
            }
        }

        #endregion Internal Properties

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PSCryptoException() : this(0, new StringBuilder(string.Empty)) { }

        /// <summary>
        /// Constructor that will be used from within CryptoUtils.
        /// </summary>
        /// <param name="errorCode">error code returned by native
        /// crypto application</param>
        /// <param name="message">Error message associated with this failure.</param>
        public PSCryptoException(uint errorCode, StringBuilder message)
            : base(message.ToString())
        {
            _errorCode = errorCode;
        }

        /// <summary>
        /// Constructor with just message but no inner exception.
        /// </summary>
        /// <param name="message">Error message associated with this failure.</param>
        public PSCryptoException(string message) : this(message, null) { }

        /// <summary>
        /// Constructor with inner exception.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="innerException">Inner exception.</param>
        /// <remarks>This constructor is currently not called
        /// explicitly from crypto utils</remarks>
        public PSCryptoException(string message, Exception innerException) :
            base(message, innerException)
        {
            _errorCode = unchecked((uint)-1);
        }

        /// <summary>
        /// Constructor which has type specific serialization logic.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Context in which this constructor is called.</param>
        /// <remarks>Currently no custom type-specific serialization logic is
        /// implemented</remarks>
        protected PSCryptoException(SerializationInfo info, StreamingContext context)
            :
            base(info, context)
        {
            _errorCode = unchecked(0xFFFFFFF);
            Dbg.Assert(false, "type-specific serialization logic not implemented and so this constructor should not be called");
        }

        #endregion Constructors

        #region ISerializable Overrides
        /// <summary>
        /// Returns base implementation.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Context.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
        #endregion ISerializable Overrides
    }

    /// <summary>
    /// A reverse compatible implementation of session key exchange. This supports the CAPI
    /// keyblob formats but uses dotnet std abstract AES and RSA classes for all crypto operations.
    /// </summary>
    internal class PSRSACryptoServiceProvider : IDisposable
    {
        #region Private Members

        private RSA _rsa;
        // handle session key encryption/decryption
        private Aes _aes;
        // handle to the AES provider object (houses session key and iv)
        private bool _canEncrypt = false;            // this flag indicates that this class has a key
        // imported from the remote end and so can be
        // used for encryption
        private bool _sessionKeyGenerated = false;
        // bool indicating if session key was generated before

        // private static bool s_keyPairGenerated = false;
        private static object s_syncObject = new object();

        #endregion Private Members

        #region Constructors

        /// <summary>
        /// Private constructor.
        /// </summary>
        /// <param name="serverMode">indicates if this service
        /// provider is operating in server mode</param>
        private PSRSACryptoServiceProvider(bool serverMode)
        {
            if (serverMode)
            {
                GenerateKeyPair();
            }

            _aes = Aes.Create();
            _aes.IV = new byte[16];  // iv should be 0
        }

        #endregion Constructors

        #region Internal Methods

        /// <summary>
        /// Get the public key, in CAPI-compatible form, as a base64 encoded string.
        /// </summary>
        /// <returns>Public key as base64 encoded string.</returns>
        internal string GetPublicKeyAsBase64EncodedString()
        {
            Dbg.Assert(_rsa != null, "No public key available.");

            RSAParameters rsaParams = _rsa.ExportParameters(false);
            byte[] capiPublicKeyBlob = CryptoConvert.ToCapiPublicKeyBlob(_rsa);

            return Convert.ToBase64String(capiPublicKeyBlob);
        }

        /// <summary>
        /// Generates an AEX-256 session key if one is not already generated.
        /// </summary>
        internal void GenerateSessionKey()
        {
            if (_sessionKeyGenerated)
                return;

            lock (s_syncObject)
            {
                if (!_sessionKeyGenerated)
                {
                    // Aes object gens key automatically on construction, so this is somewhat redundant, 
                    // but at least the actionable key will not be in-memory until it's requested fwiw.
                    _aes.GenerateKey();  
                    _sessionKeyGenerated = true;
                    _canEncrypt = true;  // we can encrypt and decrypt once session key is available
                }
            }
        }

        /// <summary>
        /// 1. Generate a AES-256 session key
        /// 2. Encrypt the session key with the Imported
        ///    RSA public key
        /// 3. Encode result above as base 64 string and export.
        /// </summary>
        /// <returns>Session key encrypted with receivers public key
        /// and encoded as a base 64 string.</returns>
        internal string SafeExportSessionKey()
        {
            Dbg.Assert(_rsa != null, "No public key available.");

            // generate one if not already done.
            GenerateSessionKey();

            // encrypt it
            byte[] encryptedKey = _rsa.Encrypt(_aes.Key, RSAEncryptionPadding.Pkcs1);

            // convert the key to capi simpleblob format before exporting
            byte[] simpleKeyBlob = CryptoConvert.ToCapiSimpleKeyBlob(encryptedKey);
            return Convert.ToBase64String(simpleKeyBlob);
        }

        /// <summary>
        /// Import a public key into the provider whose context
        /// has been obtained.
        /// </summary>
        /// <param name="publicKey">Base64 encoded public key to import.</param>
        internal void ImportPublicKeyFromBase64EncodedString(string publicKey)
        {
            Dbg.Assert(!string.IsNullOrEmpty(publicKey), "key cannot be null or empty");

            byte[] publicKeyBlob = Convert.FromBase64String(publicKey);
            _rsa = CryptoConvert.FromCapiPublicKeyBlob(publicKeyBlob);
        }

        /// <summary>
        /// Import a session key from the remote side into
        /// the current CSP.
        /// </summary>
        /// <param name="sessionKey">encrypted session key as a
        /// base64 encoded string</param>
        internal void ImportSessionKeyFromBase64EncodedString(string sessionKey)
        {
            Dbg.Assert(!string.IsNullOrEmpty(sessionKey), "key cannot be null or empty");

            byte[] sessionKeyBlob = Convert.FromBase64String(sessionKey);
            byte[] rsaEncryptedKey = CryptoConvert.FromCapiSimpleKeyBlob(sessionKeyBlob);

            _aes.Key = _rsa.Decrypt(rsaEncryptedKey, RSAEncryptionPadding.Pkcs1);

            // now we have imported the key and will be able to
            // encrypt using the session key
            _canEncrypt = true;
        }

        /// <summary>
        /// Encrypt the specified byte array.
        /// </summary>
        /// <param name="data">Data to encrypt.</param>
        /// <returns>Encrypted byte array.</returns>
        internal byte[] EncryptWithSessionKey(byte[] data)
        {
            Dbg.Assert(_canEncrypt, "Remote key has not been imported to encrypt");

            using (ICryptoTransform encryptor = _aes.CreateEncryptor())
            using (MemoryStream msEncrypt = new MemoryStream())
            using (MemoryStream swEncrypt = new MemoryStream(data))
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    swEncrypt.CopyTo(csEncrypt);
                }

                return msEncrypt.ToArray();
            }
        }

        /// <summary>
        /// Decrypt the specified buffer.
        /// </summary>
        /// <param name="data">Data to decrypt.</param>
        /// <returns>Decrypted buffer.</returns>
        internal byte[] DecryptWithSessionKey(byte[] data)
        {
            using (ICryptoTransform decryptor = _aes.CreateDecryptor())
            using (MemoryStream msDecrypt = new MemoryStream(data))
            using (MemoryStream srDecrypt = new MemoryStream())
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    csDecrypt.CopyTo(srDecrypt);
                }

                return srDecrypt.ToArray();
            } 
        }

        /// <summary>
        /// Generates key pair in a thread safe manner
        /// the first time when required.
        /// </summary>
        internal void GenerateKeyPair()
        {
            _rsa = RSA.Create();
            _rsa.KeySize = 2048;
        }

        /// <summary>
        /// Indicates if a key exchange is complete
        /// and this provider can encrypt.
        /// </summary>
        internal bool CanEncrypt
        {
            get
            {
                return _canEncrypt;
            }

            set
            {
                _canEncrypt = value;
            }
        }

        #endregion Internal Methods

        #region Internal Static Methods

        /// <summary>
        /// Returns a crypto service provider for use in the
        /// client. This will reuse the key that has been
        /// generated.
        /// </summary>
        /// <returns>Crypto service provider for
        /// the client side.</returns>
        internal static PSRSACryptoServiceProvider GetRSACryptoServiceProviderForClient()
        {
            return new PSRSACryptoServiceProvider(false);
        }

        /// <summary>
        /// Returns a crypto service provider for use in the
        /// server. This will not generate a key pair.
        /// </summary>
        /// <returns>Crypto service provider for
        /// the server side.</returns>
        internal static PSRSACryptoServiceProvider GetRSACryptoServiceProviderForServer()
        {
            return new PSRSACryptoServiceProvider(true);
        }

        #endregion Internal Static Methods

        #region IDisposable

        /// <summary>
        /// Dispose resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        // [SecurityPermission(SecurityAction.Demand, UnmanagedCode=true)]
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_rsa != null)
                {
                    _rsa.Dispose();
                }

                if (_aes != null)
                {
                    _aes.Dispose();
                }
            }
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~PSRSACryptoServiceProvider()
        {
            // When Dispose() is called, GC.SuppressFinalize()
            // is called and therefore this finalizer will not
            // be invoked. Hence this is run only on process
            // shutdown
            Dispose(true);
        }

        #endregion IDisposable
    }

    /// <summary>
    /// Helper for exchanging keys and encrypting/decrypting
    /// secure strings for serialization in remoting.
    /// </summary>
    internal abstract class PSRemotingCryptoHelper : IDisposable
    {
        #region Protected Members

        /// <summary>
        /// Crypto provider which will be used for importing remote
        /// public key as well as generating a session key, exporting
        /// it and performing symmetric key operations using the
        /// session key.
        /// </summary>
        protected PSRSACryptoServiceProvider _rsaCryptoProvider;

        /// <summary>
        /// Key exchange has been completed and both keys
        /// available.
        /// </summary>
        protected ManualResetEvent _keyExchangeCompleted = new ManualResetEvent(false);

        /// <summary>
        /// Object for synchronizing key exchange.
        /// </summary>
        protected object syncObject = new object();

        private bool _keyExchangeStarted = false;

        /// <summary>
        /// </summary>
        protected void RunKeyExchangeIfRequired()
        {
            Dbg.Assert(Session != null, "data structure handler not set");

            if (!_rsaCryptoProvider.CanEncrypt)
            {
                try
                {
                    lock (syncObject)
                    {
                        if (!_rsaCryptoProvider.CanEncrypt)
                        {
                            if (!_keyExchangeStarted)
                            {
                                _keyExchangeStarted = true;
                                _keyExchangeCompleted.Reset();
                                Session.StartKeyExchange();
                            }
                        }
                    }
                }
                finally
                {
                    // for whatever reason if StartKeyExchange()
                    // throws an exception it should reset the
                    // wait handle, so it should pass this wait
                    // if it doesn't do so, its a bug
                    _keyExchangeCompleted.WaitOne();
                }
            }
        }

        /// <summary>
        /// Core logic to encrypt a string. Assumes session key is already generated.
        /// </summary>
        /// <param name="secureString">
        /// secure string to be encrypted
        /// </param>
        /// <returns></returns>
        protected string EncryptSecureStringCore(SecureString secureString)
        {
            string encryptedDataAsString = null;

            if (_rsaCryptoProvider.CanEncrypt)
            {
                IntPtr ptr = Marshal.SecureStringToCoTaskMemUnicode(secureString);

                if (ptr != IntPtr.Zero)
                {
                    byte[] data = new byte[secureString.Length * 2];
                    for (int i = 0; i < data.Length; i++)
                    {
                        data[i] = Marshal.ReadByte(ptr, i);
                    }

                    Marshal.ZeroFreeCoTaskMemUnicode(ptr);

                    try
                    {
                        byte[] encryptedData = _rsaCryptoProvider.EncryptWithSessionKey(data);
                        encryptedDataAsString = Convert.ToBase64String(encryptedData);
                    }
                    finally
                    {
                        for (int j = 0; j < data.Length; j++)
                        {
                            data[j] = 0;
                        }
                    }
                }
            }
            else
            {
                throw new PSCryptoException(SecuritySupportStrings.CannotEncryptSecureString);
            }

            return encryptedDataAsString;
        }

        /// <summary>
        /// Core logic to decrypt a secure string. Assumes session key is already available.
        /// </summary>
        /// <param name="encryptedString">
        /// encrypted string to be decrypted
        /// </param>
        /// <returns></returns>
        protected SecureString DecryptSecureStringCore(string encryptedString)
        {
            // removing an earlier assert from here. It is
            // possible to encrypt and decrypt empty
            // secure strings
            SecureString secureString = null;

            // before you can decrypt a key exchange should have
            // happened successfully
            if (_rsaCryptoProvider.CanEncrypt)
            {
                byte[] data = null;
                try
                {
                    data = Convert.FromBase64String(encryptedString);
                }
                catch (FormatException)
                {
                    // do nothing
                    // this catch is to ensure that the exception doesn't
                    // go unhandled leading to a crash
                    throw new PSCryptoException();
                }

                if (data != null)
                {
                    byte[] decryptedData = _rsaCryptoProvider.DecryptWithSessionKey(data);

                    secureString = new SecureString();
                    UInt16 value = 0;
                    try
                    {
                        for (int i = 0; i < decryptedData.Length; i += 2)
                        {
                            value = (UInt16)(decryptedData[i] + (UInt16)(decryptedData[i + 1] << 8));
                            secureString.AppendChar((char)value);
                            value = 0;
                        }
                    }
                    finally
                    {
                        // if there was an exception for whatever reason,
                        // clear the last value store in Value
                        value = 0;

                        // zero out the contents
                        for (int i = 0; i < decryptedData.Length; i += 2)
                        {
                            decryptedData[i] = 0;
                            decryptedData[i + 1] = 0;
                        }
                    }
                }
            }
            else
            {
                Dbg.Assert(false, "Session key not available to decrypt");
            }

            return secureString;
        }

        #endregion Protected Members

        #region Internal Methods

        /// <summary>
        /// Encrypt a secure string.
        /// </summary>
        /// <param name="secureString">Secure string to encrypt.</param>
        /// <returns>Encrypted string.</returns>
        /// <remarks>This method zeroes out all interim buffers used</remarks>
        internal abstract string EncryptSecureString(SecureString secureString);

        /// <summary>
        /// Decrypt a string and construct a secure string from its
        /// contents.
        /// </summary>
        /// <param name="encryptedString">Encrypted string.</param>
        /// <returns>Secure string object.</returns>
        /// <remarks>This method zeroes out any interim buffers used</remarks>
        internal abstract SecureString DecryptSecureString(string encryptedString);

        /// <summary>
        /// Represents the session to be used for requesting public key.
        /// </summary>
        internal abstract RemoteSession Session { get; set; }

        /// <summary>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// </summary>
        /// <param name="disposing"></param>
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_rsaCryptoProvider != null)
                {
                    _rsaCryptoProvider.Dispose();
                }

                _rsaCryptoProvider = null;

                _keyExchangeCompleted.Dispose();
            }
        }

        /// <summary>
        /// Resets the wait for key exchange.
        /// </summary>
        internal void CompleteKeyExchange()
        {
            _keyExchangeCompleted.Set();
        }

        #endregion Internal Methods
    }

    /// <summary>
    /// Helper for exchanging keys and encrypting/decrypting
    /// secure strings for serialization in remoting.
    /// </summary>
    internal class PSRemotingCryptoHelperServer : PSRemotingCryptoHelper
    {
        #region Private Members

        /// <summary>
        /// This is the instance of runspace pool data structure handler
        /// to use for negotiations.
        /// </summary>
        private RemoteSession _session;

        #endregion Private Members

        #region Constructors

        /// <summary>
        /// Creates the encryption provider, but generates no key.
        /// The key will be imported later.
        /// </summary>
        internal PSRemotingCryptoHelperServer()
        {
            _rsaCryptoProvider = PSRSACryptoServiceProvider.GetRSACryptoServiceProviderForServer();
        }

        #endregion Constructors

        #region Internal Methods

        internal override string EncryptSecureString(SecureString secureString)
        {
            ServerRemoteSession session = Session as ServerRemoteSession;

            // session!=null check required for DRTs TestEncryptSecureString* entries in CryptoUtilsTest/UTUtils.dll
            // for newer clients, server will never initiate key exchange.
            // for server, just the session key is required to encrypt/decrypt anything
            if ((session != null) && (session.Context.ClientCapability.ProtocolVersion >= RemotingConstants.ProtocolVersionWin8RTM))
            {
                _rsaCryptoProvider.GenerateSessionKey();
            }
            else // older clients
            {
                RunKeyExchangeIfRequired();
            }

            return EncryptSecureStringCore(secureString);
        }

        internal override SecureString DecryptSecureString(string encryptedString)
        {
            RunKeyExchangeIfRequired();

            return DecryptSecureStringCore(encryptedString);
        }

        /// <summary>
        /// Imports a public key from its base64 encoded string representation.
        /// </summary>
        /// <param name="publicKeyAsString">Public key in its string representation.</param>
        /// <returns>True on success.</returns>
        internal bool ImportRemotePublicKey(string publicKeyAsString)
        {
            Dbg.Assert(!string.IsNullOrEmpty(publicKeyAsString), "public key passed in cannot be null");

            // generate the crypto provider to use for encryption
            // _rsaCryptoProvider = GenerateCryptoServiceProvider(false);

            try
            {
                _rsaCryptoProvider.ImportPublicKeyFromBase64EncodedString(publicKeyAsString);
            }
            catch (PSCryptoException)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Represents the session to be used for requesting public key.
        /// </summary>
        internal override RemoteSession Session
        {
            get
            {
                return _session;
            }

            set
            {
                _session = value;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="encryptedSessionKey"></param>
        /// <returns></returns>
        internal bool ExportEncryptedSessionKey(out string encryptedSessionKey)
        {
            try
            {
                encryptedSessionKey = _rsaCryptoProvider.SafeExportSessionKey();
            }
            catch (PSCryptoException)
            {
                encryptedSessionKey = string.Empty;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets a helper with a test session.
        /// </summary>
        /// <returns>Helper for testing.</returns>
        /// <remarks>To be used only for testing</remarks>
        internal static PSRemotingCryptoHelperServer GetTestRemotingCryptHelperServer()
        {
            PSRemotingCryptoHelperServer helper = new PSRemotingCryptoHelperServer();
            helper.Session = new TestHelperSession();

            return helper;
        }

        #endregion Internal Methods
    }

    /// <summary>
    /// Helper for exchanging keys and encrypting/decrypting
    /// secure strings for serialization in remoting.
    /// </summary>
    internal class PSRemotingCryptoHelperClient : PSRemotingCryptoHelper
    {
        #region Private Members

        #endregion Private Members

        #region Constructors

        /// <summary>
        /// Creates the encryption provider, but generates no key.
        /// The key will be imported later.
        /// </summary>
        internal PSRemotingCryptoHelperClient()
        {
            _rsaCryptoProvider = PSRSACryptoServiceProvider.GetRSACryptoServiceProviderForClient();
        }

        #endregion Constructors

        #region Protected Methods

        #endregion Protected Methods

        #region Internal Methods

        internal override string EncryptSecureString(SecureString secureString)
        {
            RunKeyExchangeIfRequired();

            return EncryptSecureStringCore(secureString);
        }

        internal override SecureString DecryptSecureString(string encryptedString)
        {
            RunKeyExchangeIfRequired();

            return DecryptSecureStringCore(encryptedString);
        }

        /// <summary>
        /// Export the public key as a base64 encoded string.
        /// </summary>
        /// <param name="publicKeyAsString">on execution will contain
        /// the public key as string</param>
        /// <returns>True on success.</returns>
        internal bool ExportLocalPublicKey(out string publicKeyAsString)
        {
            // generate keys - the method already takes of creating
            // only when its not already created

            try
            {
                _rsaCryptoProvider.GenerateKeyPair();
            }
            catch (PSCryptoException)
            {
                throw;

                // the caller has to ensure that they
                // complete the key exchange process
            }

            try
            {
                publicKeyAsString = _rsaCryptoProvider.GetPublicKeyAsBase64EncodedString();
            }
            catch (PSCryptoException)
            {
                publicKeyAsString = string.Empty;
                return false;
            }

            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="encryptedSessionKey"></param>
        /// <returns></returns>
        internal bool ImportEncryptedSessionKey(string encryptedSessionKey)
        {
            Dbg.Assert(!string.IsNullOrEmpty(encryptedSessionKey), "encrypted session key passed in cannot be null");

            try
            {
                _rsaCryptoProvider.ImportSessionKeyFromBase64EncodedString(encryptedSessionKey);
            }
            catch (PSCryptoException)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Represents the session to be used for requesting public key.
        /// </summary>
        internal override RemoteSession Session { get; set; }

        /// <summary>
        /// Gets a helper with a test session.
        /// </summary>
        /// <returns>Helper for testing.</returns>
        /// <remarks>To be used only for testing</remarks>
        internal static PSRemotingCryptoHelperClient GetTestRemotingCryptHelperClient()
        {
            PSRemotingCryptoHelperClient helper = new PSRemotingCryptoHelperClient();
            helper.Session = new TestHelperSession();

            return helper;
        }

        #endregion Internal Methods
    }

    #region TestHelpers

    internal class TestHelperSession : RemoteSession
    {
        internal override void StartKeyExchange()
        {
            // intentionally left blank
        }

        internal override RemotingDestination MySelf
        {
            get
            {
                return RemotingDestination.InvalidDestination;
            }
        }

        internal override void CompleteKeyExchange()
        {
            // intentionally left blank
        }
    }
    #endregion TestHelpers
}
