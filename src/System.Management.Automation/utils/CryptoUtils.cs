// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Management.Automation.Remoting;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Internal
{
    /// <summary>
    /// This class provides the converters for all Native CAPI key blob formats.
    /// </summary>
    internal static class PSCryptoNativeConverter
    {
        #region Constants

        /// <summary>
        /// The blob version is fixed.
        /// </summary>
        public const uint CUR_BLOB_VERSION = 0x00000002;

        /// <summary>
        /// RSA Key.
        /// </summary>
        public const uint CALG_RSA_KEYX = 0x000000a4;

        /// <summary>
        /// AES 256 symmetric key.
        /// </summary>
        public const uint CALG_AES_256 = 0x00000010;

        /// <summary>
        /// Option for exporting public key blob.
        /// </summary>
        public const uint PUBLICKEYBLOB = 0x00000006;

        /// <summmary>
        /// PUBLICKEYBLOB header length.
        /// </summary>
        public const int PUBLICKEYBLOB_HEADER_LEN = 20;

        /// <summary>
        /// Option for exporting a session key.
        /// </summary>
        public const uint SIMPLEBLOB = 0x00000001;

        /// <summmary>
        /// SIMPLEBLOB header length.
        /// </summary>
        public const int SIMPLEBLOB_HEADER_LEN = 12;

        #endregion Constants

        #region Functions

        private static int ToInt32LE(byte[] bytes, int offset)
        {
            return (bytes[offset + 3] << 24) | (bytes[offset + 2] << 16) | (bytes[offset + 1] << 8) | bytes[offset];
        }

        private static uint ToUInt32LE(byte[] bytes, int offset)
        {
            return (uint)((bytes[offset + 3] << 24) | (bytes[offset + 2] << 16) | (bytes[offset + 1] << 8) | bytes[offset]);
        }

        private static byte[] GetBytesLE(int val)
        {
            return new[] {
                (byte)(val & 0xff),
                (byte)((val >> 8) & 0xff),
                (byte)((val >> 16) & 0xff),
                (byte)((val >> 24) & 0xff)
            };
        }

        private static byte[] CreateReverseByteArray(byte[] data)
        {
            byte[] reverseData = new byte[data.Length];
            Array.Copy(data, reverseData, data.Length);
            Array.Reverse(reverseData);
            return reverseData;
        }

        internal static RSA FromCapiPublicKeyBlob(byte[] blob)
        {
            return FromCapiPublicKeyBlob(blob, 0);
        }

        private static RSA FromCapiPublicKeyBlob(byte[] blob, int offset)
        {
            if (blob == null)
            {
                throw new ArgumentNullException(nameof(blob));
            }

            if (offset > blob.Length)
            {
                throw new ArgumentException(SecuritySupportStrings.InvalidOffset);
            }

            var rsap = GetParametersFromCapiPublicKeyBlob(blob, offset);

            try
            {
                RSA rsa = RSA.Create();
                rsa.ImportParameters(rsap);
                return rsa;
            }
            catch (Exception ex)
            {
                throw new CryptographicException(SecuritySupportStrings.CannotImportPublicKey, ex);
            }
        }

        private static RSAParameters GetParametersFromCapiPublicKeyBlob(byte[] blob, int offset)
        {
            if (blob == null)
            {
                throw new ArgumentNullException(nameof(blob));
            }

            if (offset > blob.Length)
            {
                throw new ArgumentException(SecuritySupportStrings.InvalidOffset);
            }

            if (blob.Length < PUBLICKEYBLOB_HEADER_LEN)
            {
                throw new ArgumentException(SecuritySupportStrings.InvalidPublicKey);
            }

            try
            {
                if ((blob[offset] != PUBLICKEYBLOB) ||            // PUBLICKEYBLOB (0x06)
                    (blob[offset + 1] != CUR_BLOB_VERSION) ||       // Version (0x02)
                    (blob[offset + 2] != 0x00) ||                   // Reserved (word)
                    (blob[offset + 3] != 0x00) ||
                    (ToUInt32LE(blob, offset + 8) != 0x31415352))   // DWORD magic = RSA1
                {
                    throw new CryptographicException(SecuritySupportStrings.InvalidPublicKey);
                }

                // DWORD bitlen
                int bitLen = ToInt32LE(blob, offset + 12);

                // DWORD public exponent
                RSAParameters rsap = new RSAParameters();
                rsap.Exponent = new byte[3];
                rsap.Exponent[0] = blob[offset + 18];
                rsap.Exponent[1] = blob[offset + 17];
                rsap.Exponent[2] = blob[offset + 16];

                int pos = offset + 20;
                int byteLen = (bitLen >> 3);
                rsap.Modulus = new byte[byteLen];
                Buffer.BlockCopy(blob, pos, rsap.Modulus, 0, byteLen);
                Array.Reverse(rsap.Modulus);

                return rsap;
            }
            catch (Exception ex)
            {
                throw new CryptographicException(SecuritySupportStrings.InvalidPublicKey, ex);
            }
        }

        internal static byte[] ToCapiPublicKeyBlob(RSA rsa)
        {
            if (rsa == null)
            {
                throw new ArgumentNullException(nameof(rsa));
            }

            RSAParameters p = rsa.ExportParameters(false);
            int keyLength = p.Modulus.Length;   // in bytes
            byte[] blob = new byte[PUBLICKEYBLOB_HEADER_LEN + keyLength];

            blob[0] = (byte)PUBLICKEYBLOB;      // Type - PUBLICKEYBLOB (0x06)
            blob[1] = (byte)CUR_BLOB_VERSION;   // Version - Always CUR_BLOB_VERSION (0x02)
            // [2], [3]                         // RESERVED - Always 0
            blob[5] = (byte)CALG_RSA_KEYX;      // ALGID - Always 00 a4 00 00 (for CALG_RSA_KEYX)
            blob[8] = 0x52;                     // Magic - RSA1 (ASCII in hex)
            blob[9] = 0x53;
            blob[10] = 0x41;
            blob[11] = 0x31;

            byte[] bitlen = GetBytesLE(keyLength << 3);
            blob[12] = bitlen[0];               // bitlen
            blob[13] = bitlen[1];
            blob[14] = bitlen[2];
            blob[15] = bitlen[3];

            // public exponent (DWORD)
            int pos = 16;
            int n = p.Exponent.Length;

            Dbg.Assert(n <= 4, "RSA exponent byte length cannot exceed allocated segment");

            while (n > 0)
            {
                blob[pos++] = p.Exponent[--n];
            }

            // modulus
            pos = 20;
            byte[] key = p.Modulus;
            Array.Reverse(key);
            Buffer.BlockCopy(key, 0, blob, pos, keyLength);

            return blob;
        }

        internal static byte[] FromCapiSimpleKeyBlob(byte[] blob)
        {
            if (blob == null)
            {
                throw new ArgumentNullException(nameof(blob));
            }

            if (blob.Length < SIMPLEBLOB_HEADER_LEN)
            {
                throw new ArgumentException(SecuritySupportStrings.InvalidSessionKey);
            }

            // just ignore the header of the capi blob and go straight for the key
            return CreateReverseByteArray(blob.Skip(SIMPLEBLOB_HEADER_LEN).ToArray());
        }

        internal static byte[] ToCapiSimpleKeyBlob(byte[] encryptedKey)
        {
            if (encryptedKey == null)
            {
                throw new ArgumentNullException(nameof(encryptedKey));
            }

            // formulate the PUBLICKEYSTRUCT
            byte[] blob = new byte[SIMPLEBLOB_HEADER_LEN + encryptedKey.Length];

            blob[0] = (byte)SIMPLEBLOB;         // Type - SIMPLEBLOB (0x01)
            blob[1] = (byte)CUR_BLOB_VERSION;   // Version - Always CUR_BLOB_VERSION (0x02)
            // [2], [3]                         // RESERVED - Always 0
            blob[4] = (byte)CALG_AES_256;       // AES-256 algo id (0x10)
            blob[5] = 0x66;                     // ??
            // [6], [7], [8]                    // 0x00 
            blob[9] = (byte)CALG_RSA_KEYX;      // 0xa4
            // [10], [11]                       // 0x00 

            // create a reversed copy and add the encrypted key
            byte[] reversedKey = CreateReverseByteArray(encryptedKey);
            Buffer.BlockCopy(reversedKey, 0, blob, SIMPLEBLOB_HEADER_LEN, reversedKey.Length);

            return blob;
        }

        #endregion Functions
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

        private readonly uint _errorCode;

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
        public PSCryptoException()
            : this(0, new StringBuilder(string.Empty)) { }

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
        public PSCryptoException(string message)
            : this(message, null) { }

        /// <summary>
        /// Constructor with inner exception.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="innerException">Inner exception.</param>
        /// <remarks>This constructor is currently not called
        /// explicitly from crypto utils</remarks>
        public PSCryptoException(string message, Exception innerException)
            : base(message, innerException)
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
            : base(info, context)
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
    internal sealed class PSRSACryptoServiceProvider : IDisposable
    {
        #region Private Members

        // handle session key encryption/decryption
        private RSA _rsa;

        // handle to the AES provider object (houses session key and iv)
        private readonly Aes _aes;

        // this flag indicates that this class has a key imported from the 
        // remote end and so can be used for encryption
        private bool _canEncrypt;

        // bool indicating if session key was generated before
        private bool _sessionKeyGenerated = false;

        private static readonly object s_syncObject = new object();

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

            byte[] capiPublicKeyBlob = PSCryptoNativeConverter.ToCapiPublicKeyBlob(_rsa);

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
            byte[] simpleKeyBlob = PSCryptoNativeConverter.ToCapiSimpleKeyBlob(encryptedKey);
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
            _rsa = PSCryptoNativeConverter.FromCapiPublicKeyBlob(publicKeyBlob);
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
            byte[] rsaEncryptedKey = PSCryptoNativeConverter.FromCapiSimpleKeyBlob(sessionKeyBlob);

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
            using (MemoryStream targetStream = new MemoryStream())
            using (MemoryStream sourceStream = new MemoryStream(data))
            {
                using (CryptoStream cryptoStream = new CryptoStream(targetStream, encryptor, CryptoStreamMode.Write))
                {
                    sourceStream.CopyTo(cryptoStream);
                }

                return targetStream.ToArray();
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
            using (MemoryStream sourceStream = new MemoryStream(data))
            using (MemoryStream targetStream = new MemoryStream())
            {
                using (CryptoStream csDecrypt = new CryptoStream(sourceStream, decryptor, CryptoStreamMode.Read))
                {
                    csDecrypt.CopyTo(targetStream);
                }

                return targetStream.ToArray();
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

        private void Dispose(bool disposing)
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
