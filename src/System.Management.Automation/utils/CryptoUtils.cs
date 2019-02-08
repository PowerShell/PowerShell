// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using System.Security;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.ConstrainedExecution;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics.CodeAnalysis;
using Dbg = System.Management.Automation.Diagnostics;
using System.Management.Automation.Remoting;

namespace System.Management.Automation.Internal
{
    /// <summary>
    /// Class that encapsulates native crypto provider handles and provides a
    /// mechanism for resources released by them.
    /// </summary>
    //    [SecurityPermission(SecurityAction.Demand, UnmanagedCode=true)]
    //    [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode=true)]
    internal class PSSafeCryptProvHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// This safehandle instance "owns" the handle, hence base(true)
        /// is being called. When safehandle is no longer in use it will
        /// call this class's ReleaseHandle method which will release
        /// the resources.
        /// </summary>
        internal PSSafeCryptProvHandle() : base(true) { }

        /// <summary>
        /// Release the crypto handle held by this instance.
        /// </summary>
        /// <returns>True on success, false otherwise.</returns>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle()
        {
            return PSCryptoNativeUtils.CryptReleaseContext(handle, 0);
        }
    }

    /// <summary>
    /// Class the encapsulates native crypto key handles and provides a
    /// mechanism to release resources used by it.
    /// </summary>
    // [SecurityPermission(SecurityAction.Demand, UnmanagedCode=true)]
    // [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode=true)]
    internal class PSSafeCryptKey : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// This safehandle instance "owns" the handle, hence base(true)
        /// is being called. When safehandle is no longer in use it will
        /// call this class's ReleaseHandle method which will release the
        /// resources.
        /// </summary>
        internal PSSafeCryptKey() : base(true) { }

        /// <summary>
        /// Release the crypto handle held by this instance.
        /// </summary>
        /// <returns>True on success, false otherwise.</returns>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle()
        {
            return PSCryptoNativeUtils.CryptDestroyKey(handle);
        }

        /// <summary>
        /// Equivalent of IntPtr.Zero for the safe crypt key.
        /// </summary>
        internal static PSSafeCryptKey Zero { get; } = new PSSafeCryptKey();
    }

    /// <summary>
    /// This class provides the wrapper for all Native CAPI functions.
    /// </summary>
    internal class PSCryptoNativeUtils
    {
        #region Functions

#if UNIX
        /// Return Type: BOOL->int
        ///hProv: HCRYPTPROV->ULONG_PTR->unsigned int
        ///Algid: ALG_ID->unsigned int
        ///dwFlags: DWORD->unsigned int
        ///phKey: HCRYPTKEY*
        public static bool CryptGenKey(
            PSSafeCryptProvHandle hProv,
            uint Algid,
            uint dwFlags,
            ref PSSafeCryptKey phKey)
        {
            throw new PSCryptoException();
        }

        /// Return Type: BOOL->int
        ///hKey: HCRYPTKEY->ULONG_PTR->unsigned int
        public static bool CryptDestroyKey(IntPtr hKey)
        {
            throw new PSCryptoException();
        }

        /// Return Type: BOOL->int
        ///phProv: HCRYPTPROV*
        ///szContainer: LPCWSTR->WCHAR*
        ///szProvider: LPCWSTR->WCHAR*
        ///dwProvType: DWORD->unsigned int
        ///dwFlags: DWORD->unsigned int
        public static bool CryptAcquireContext(ref PSSafeCryptProvHandle phProv,
            [InAttribute()] [MarshalAsAttribute(UnmanagedType.LPWStr)] string szContainer,
            [InAttribute()] [MarshalAsAttribute(UnmanagedType.LPWStr)] string szProvider,
            uint dwProvType,
            uint dwFlags)
        {
            throw new PSCryptoException();
        }

        /// Return Type: BOOL->int
        ///hProv: HCRYPTPROV->ULONG_PTR->unsigned int
        ///dwFlags: DWORD->unsigned int
        public static bool CryptReleaseContext(IntPtr hProv, uint dwFlags)
        {
            throw new PSCryptoException();
        }


        /// Return Type: BOOL->int
        ///hKey: HCRYPTKEY->ULONG_PTR->unsigned int
        ///hHash: HCRYPTHASH->ULONG_PTR->unsigned int
        ///Final: BOOL->int
        ///dwFlags: DWORD->unsigned int
        ///pbData: BYTE*
        ///pdwDataLen: DWORD*
        ///dwBufLen: DWORD->unsigned int
        public static bool CryptEncrypt(PSSafeCryptKey hKey,
            IntPtr hHash,
            [MarshalAsAttribute(UnmanagedType.Bool)] bool Final,
            uint dwFlags,
            byte[] pbData,
            ref int pdwDataLen,
            int dwBufLen)
        {
            throw new PSCryptoException();
        }


        /// Return Type: BOOL->int
        ///hKey: HCRYPTKEY->ULONG_PTR->unsigned int
        ///hHash: HCRYPTHASH->ULONG_PTR->unsigned int
        ///Final: BOOL->int
        ///dwFlags: DWORD->unsigned int
        ///pbData: BYTE*
        ///pdwDataLen: DWORD*
        public static bool CryptDecrypt(PSSafeCryptKey hKey,
            IntPtr hHash,
            [MarshalAsAttribute(UnmanagedType.Bool)] bool Final,
            uint dwFlags,
            byte[] pbData,
            ref int pdwDataLen)
        {
            throw new PSCryptoException();
        }

        /// Return Type: BOOL->int
        ///hKey: HCRYPTKEY->ULONG_PTR->unsigned int
        ///hExpKey: HCRYPTKEY->ULONG_PTR->unsigned int
        ///dwBlobType: DWORD->unsigned int
        ///dwFlags: DWORD->unsigned int
        ///pbData: BYTE*
        ///pdwDataLen: DWORD*
        public static bool CryptExportKey(PSSafeCryptKey hKey,
            PSSafeCryptKey hExpKey,
            uint dwBlobType,
            uint dwFlags,
            byte[] pbData,
            ref uint pdwDataLen)
        {
            throw new PSCryptoException();
        }

        /// Return Type: BOOL->int
        ///hProv: HCRYPTPROV->ULONG_PTR->unsigned int
        ///pbData: BYTE*
        ///dwDataLen: DWORD->unsigned int
        ///hPubKey: HCRYPTKEY->ULONG_PTR->unsigned int
        ///dwFlags: DWORD->unsigned int
        ///phKey: HCRYPTKEY*
        public static bool CryptImportKey(PSSafeCryptProvHandle hProv,
            byte[] pbData,
            int dwDataLen,
            PSSafeCryptKey hPubKey,
            uint dwFlags,
            ref PSSafeCryptKey phKey)
        {
            throw new PSCryptoException();
        }

        /// Return Type: BOOL->int
        ///hKey: HCRYPTKEY->ULONG_PTR->unsigned int
        ///pdwReserved: DWORD*
        ///dwFlags: DWORD->unsigned int
        ///phKey: HCRYPTKEY*
        public static bool CryptDuplicateKey(PSSafeCryptKey hKey,
                                                    ref uint pdwReserved,
                                                    uint dwFlags,
                                                    ref PSSafeCryptKey phKey)
        {
            throw new PSCryptoException();
        }

        /// Return Type: DWORD->unsigned int
        public static uint GetLastError()
        {
            throw new PSCryptoException();
        }
#else

        /// Return Type: BOOL->int
        ///hProv: HCRYPTPROV->ULONG_PTR->unsigned int
        ///Algid: ALG_ID->unsigned int
        ///dwFlags: DWORD->unsigned int
        ///phKey: HCRYPTKEY*
        [DllImportAttribute(PinvokeDllNames.CryptGenKeyDllName, EntryPoint = "CryptGenKey")]
        [return: MarshalAsAttribute(UnmanagedType.Bool)]
        public static extern bool CryptGenKey(PSSafeCryptProvHandle hProv,
                                              uint Algid,
                                              uint dwFlags,
                                              ref PSSafeCryptKey phKey);

        /// Return Type: BOOL->int
        ///hKey: HCRYPTKEY->ULONG_PTR->unsigned int
        [DllImportAttribute(PinvokeDllNames.CryptDestroyKeyDllName, EntryPoint = "CryptDestroyKey")]
        [return: MarshalAsAttribute(UnmanagedType.Bool)]
        public static extern bool CryptDestroyKey(IntPtr hKey);

        /// Return Type: BOOL->int
        ///phProv: HCRYPTPROV*
        ///szContainer: LPCWSTR->WCHAR*
        ///szProvider: LPCWSTR->WCHAR*
        ///dwProvType: DWORD->unsigned int
        ///dwFlags: DWORD->unsigned int
        [DllImportAttribute(PinvokeDllNames.CryptAcquireContextDllName, EntryPoint = "CryptAcquireContext")]
        [return: MarshalAsAttribute(UnmanagedType.Bool)]
        public static extern bool CryptAcquireContext(ref PSSafeCryptProvHandle phProv,
            [InAttribute()] [MarshalAsAttribute(UnmanagedType.LPWStr)] string szContainer,
            [InAttribute()] [MarshalAsAttribute(UnmanagedType.LPWStr)] string szProvider,
            uint dwProvType,
            uint dwFlags);

        /// Return Type: BOOL->int
        ///hProv: HCRYPTPROV->ULONG_PTR->unsigned int
        ///dwFlags: DWORD->unsigned int
        [DllImportAttribute(PinvokeDllNames.CryptReleaseContextDllName, EntryPoint = "CryptReleaseContext")]
        [return: MarshalAsAttribute(UnmanagedType.Bool)]
        public static extern bool CryptReleaseContext(IntPtr hProv, uint dwFlags);

        /// Return Type: BOOL->int
        ///hKey: HCRYPTKEY->ULONG_PTR->unsigned int
        ///hHash: HCRYPTHASH->ULONG_PTR->unsigned int
        ///Final: BOOL->int
        ///dwFlags: DWORD->unsigned int
        ///pbData: BYTE*
        ///pdwDataLen: DWORD*
        ///dwBufLen: DWORD->unsigned int
        [DllImportAttribute(PinvokeDllNames.CryptEncryptDllName, EntryPoint = "CryptEncrypt")]
        [return: MarshalAsAttribute(UnmanagedType.Bool)]
        public static extern bool CryptEncrypt(PSSafeCryptKey hKey,
            IntPtr hHash,
            [MarshalAsAttribute(UnmanagedType.Bool)] bool Final,
            uint dwFlags,
            byte[] pbData,
            ref int pdwDataLen,
            int dwBufLen);

        /// Return Type: BOOL->int
        ///hKey: HCRYPTKEY->ULONG_PTR->unsigned int
        ///hHash: HCRYPTHASH->ULONG_PTR->unsigned int
        ///Final: BOOL->int
        ///dwFlags: DWORD->unsigned int
        ///pbData: BYTE*
        ///pdwDataLen: DWORD*
        [DllImportAttribute(PinvokeDllNames.CryptDecryptDllName, EntryPoint = "CryptDecrypt")]
        [return: MarshalAsAttribute(UnmanagedType.Bool)]
        public static extern bool CryptDecrypt(PSSafeCryptKey hKey,
            IntPtr hHash,
            [MarshalAsAttribute(UnmanagedType.Bool)] bool Final,
            uint dwFlags,
            byte[] pbData,
            ref int pdwDataLen);

        /// Return Type: BOOL->int
        ///hKey: HCRYPTKEY->ULONG_PTR->unsigned int
        ///hExpKey: HCRYPTKEY->ULONG_PTR->unsigned int
        ///dwBlobType: DWORD->unsigned int
        ///dwFlags: DWORD->unsigned int
        ///pbData: BYTE*
        ///pdwDataLen: DWORD*
        [DllImportAttribute(PinvokeDllNames.CryptExportKeyDllName, EntryPoint = "CryptExportKey")]
        [return: MarshalAsAttribute(UnmanagedType.Bool)]
        public static extern bool CryptExportKey(PSSafeCryptKey hKey,
            PSSafeCryptKey hExpKey,
            uint dwBlobType,
            uint dwFlags,
            byte[] pbData,
            ref uint pdwDataLen);

        /// Return Type: BOOL->int
        ///hProv: HCRYPTPROV->ULONG_PTR->unsigned int
        ///pbData: BYTE*
        ///dwDataLen: DWORD->unsigned int
        ///hPubKey: HCRYPTKEY->ULONG_PTR->unsigned int
        ///dwFlags: DWORD->unsigned int
        ///phKey: HCRYPTKEY*
        [DllImportAttribute(PinvokeDllNames.CryptImportKeyDllName, EntryPoint = "CryptImportKey")]
        [return: MarshalAsAttribute(UnmanagedType.Bool)]
        public static extern bool CryptImportKey(PSSafeCryptProvHandle hProv,
            byte[] pbData,
            int dwDataLen,
            PSSafeCryptKey hPubKey,
            uint dwFlags,
            ref PSSafeCryptKey phKey);

        /// Return Type: BOOL->int
        ///hKey: HCRYPTKEY->ULONG_PTR->unsigned int
        ///pdwReserved: DWORD*
        ///dwFlags: DWORD->unsigned int
        ///phKey: HCRYPTKEY*
        [DllImportAttribute(PinvokeDllNames.CryptDuplicateKeyDllName, EntryPoint = "CryptDuplicateKey")]
        [return: MarshalAsAttribute(UnmanagedType.Bool)]
        public static extern bool CryptDuplicateKey(PSSafeCryptKey hKey,
                                                    ref uint pdwReserved,
                                                    uint dwFlags,
                                                    ref PSSafeCryptKey phKey);

        /// Return Type: DWORD->unsigned int
        [DllImportAttribute(PinvokeDllNames.GetLastErrorDllName, EntryPoint = "GetLastError")]
        public static extern uint GetLastError();
#endif

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
    /// One of the issues with RSACryptoServiceProvider is that it never uses CRYPT_VERIFYCONTEXT
    /// to create ephemeral keys.  This class is a facade written on top of native CAPI APIs
    /// to create ephemeral keys.
    /// </summary>
    internal class PSRSACryptoServiceProvider : IDisposable
    {
        #region Private Members

        private PSSafeCryptProvHandle _hProv;
        // handle to the provider
        private bool _canEncrypt = false;            // this flag indicates that this class has a key
        // imported from the remote end and so can be
        // used for encryption
        private PSSafeCryptKey _hRSAKey;
        // handle to the RSA key with which the session
        // key is exchange. This can either be generated
        // or imported
        private PSSafeCryptKey _hSessionKey;
        // handle to the session key. This can either
        // be generated or imported
        private bool _sessionKeyGenerated = false;
        // bool indicating if session key was generated before

        private static PSSafeCryptProvHandle s_hStaticProv;
        private static PSSafeCryptKey s_hStaticRSAKey;
        private static bool s_keyPairGenerated = false;
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
                _hProv = new PSSafeCryptProvHandle();

                // We need PROV_RSA_AES to support AES-256 symmetric key
                // encryption. PROV_RSA_FULL supports only RC2 and RC4
                bool ret = PSCryptoNativeUtils.CryptAcquireContext(ref _hProv,
                    null,
                    null,
                    PSCryptoNativeUtils.PROV_RSA_AES,
                    PSCryptoNativeUtils.CRYPT_VERIFYCONTEXT);

                CheckStatus(ret);

                _hRSAKey = new PSSafeCryptKey();
            }

            _hSessionKey = new PSSafeCryptKey();
        }

        #endregion Constructors

        #region Internal Methods

        /// <summary>
        /// Get the public key as a base64 encoded string.
        /// </summary>
        /// <returns>Public key as base64 encoded string.</returns>
        internal string GetPublicKeyAsBase64EncodedString()
        {
            uint publicKeyLength = 0;

            // Get key length first
            bool ret = PSCryptoNativeUtils.CryptExportKey(_hRSAKey,
                                                          PSSafeCryptKey.Zero,
                                                          PSCryptoNativeUtils.PUBLICKEYBLOB,
                                                          0,
                                                          null,
                                                          ref publicKeyLength);
            CheckStatus(ret);

            // Create enough buffer and get the actual data
            byte[] publicKey = new byte[publicKeyLength];
            ret = PSCryptoNativeUtils.CryptExportKey(_hRSAKey,
                                                      PSSafeCryptKey.Zero,
                                                      PSCryptoNativeUtils.PUBLICKEYBLOB,
                                                      0,
                                                      publicKey,
                                                      ref publicKeyLength);
            CheckStatus(ret);

            // Convert the public key into base64 encoding so that it can be exported to
            // the other end.
            string result = Convert.ToBase64String(publicKey);

            return result;
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
                    bool ret = PSCryptoNativeUtils.CryptGenKey(_hProv,
                                                              PSCryptoNativeUtils.CALG_AES_256,
                                                              0x01000000 |    // key length = 256 bits
                                                              PSCryptoNativeUtils.CRYPT_EXPORTABLE |
                                                              PSCryptoNativeUtils.CRYPT_CREATE_SALT,
                                                              ref _hSessionKey);
                    CheckStatus(ret);
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
            // generate one if not already done.
            GenerateSessionKey();

            uint length = 0;

            // get key length first
            bool ret = PSCryptoNativeUtils.CryptExportKey(_hSessionKey,
                                                     _hRSAKey,
                                                     PSCryptoNativeUtils.SIMPLEBLOB,
                                                     0,
                                                     null,
                                                     ref length);
            CheckStatus(ret);

            // allocate buffer and export the key
            byte[] sessionkey = new byte[length];
            ret = PSCryptoNativeUtils.CryptExportKey(_hSessionKey,
                                                     _hRSAKey,
                                                     PSCryptoNativeUtils.SIMPLEBLOB,
                                                     0,
                                                     sessionkey,
                                                     ref length);
            CheckStatus(ret);

            // now we can encrypt as we have the session key
            _canEncrypt = true;

            // convert the key to base64 before exporting
            return Convert.ToBase64String(sessionkey);
        }

        /// <summary>
        /// Import a public key into the provider whose context
        /// has been obtained.
        /// </summary>
        /// <param name="publicKey">Base64 encoded public key to import.</param>
        internal void ImportPublicKeyFromBase64EncodedString(string publicKey)
        {
            Dbg.Assert(!string.IsNullOrEmpty(publicKey), "key cannot be null or empty");

            byte[] convertedBase64 = Convert.FromBase64String(publicKey);

            bool ret = PSCryptoNativeUtils.CryptImportKey(_hProv,
                                           convertedBase64,
                                           convertedBase64.Length,
                                           PSSafeCryptKey.Zero,
                                           0,
                                           ref _hRSAKey);

            CheckStatus(ret);
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

            byte[] convertedBase64 = Convert.FromBase64String(sessionKey);

            bool ret = PSCryptoNativeUtils.CryptImportKey(_hProv,
                                            convertedBase64,
                                            convertedBase64.Length,
                                            _hRSAKey,
                                            0,
                                            ref _hSessionKey);
            CheckStatus(ret);

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
            // first make a copy of the original data.This is needed
            // as CryptEncrypt uses the same buffer to write the encrypted data
            // into.
            Dbg.Assert(_canEncrypt, "Remote key has not been imported to encrypt");

            byte[] encryptedData = new byte[data.Length];
            Array.Copy(data, 0, encryptedData, 0, data.Length);

            int dataLength = encryptedData.Length;

            // encryption always happens using the session key
            bool ret = PSCryptoNativeUtils.CryptEncrypt(_hSessionKey,
                                                        IntPtr.Zero,
                                                        true,
                                                        0,
                                                        encryptedData,
                                                        ref dataLength,
                                                        data.Length);

            // if encryption failed, then dataLength will contain the length
            // of buffer needed to store the encrypted contents. Recreate
            // the buffer
            if (false == ret)
            {
                // before reallocating the encryptedData buffer,
                // zero out its contents
                for (int i = 0; i < encryptedData.Length; i++)
                {
                    encryptedData[i] = 0;
                }

                encryptedData = new byte[dataLength];

                Array.Copy(data, 0, encryptedData, 0, data.Length);
                dataLength = data.Length;
                ret = PSCryptoNativeUtils.CryptEncrypt(_hSessionKey,
                                                       IntPtr.Zero,
                                                       true,
                                                       0,
                                                       encryptedData,
                                                       ref dataLength,
                                                       encryptedData.Length);

                CheckStatus(ret);
            }

            // make sure we copy only appropriate data
            // dataLength will contain the length of the encrypted
            // data buffer
            byte[] result = new byte[dataLength];
            Array.Copy(encryptedData, 0, result, 0, dataLength);
            return result;
        }

        /// <summary>
        /// Decrypt the specified buffer.
        /// </summary>
        /// <param name="data">Data to decrypt.</param>
        /// <returns>Decrypted buffer.</returns>
        internal byte[] DecryptWithSessionKey(byte[] data)
        {
            // first make a copy of the original data.This is needed
            // as CryptDecrypt uses the same buffer to write the decrypted data
            // into.
            byte[] decryptedData = new byte[data.Length];

            Array.Copy(data, 0, decryptedData, 0, data.Length);

            int dataLength = decryptedData.Length;

            bool ret = PSCryptoNativeUtils.CryptDecrypt(_hSessionKey,
                                                        IntPtr.Zero,
                                                        true,
                                                        0,
                                                        decryptedData,
                                                        ref dataLength);

            // if decryption failed, then dataLength will contain the length
            // of buffer needed to store the decrypted contents. Recreate
            // the buffer
            if (false == ret)
            {
                decryptedData = new byte[dataLength];

                Array.Copy(data, 0, decryptedData, 0, data.Length);
                ret = PSCryptoNativeUtils.CryptDecrypt(_hSessionKey,
                                                       IntPtr.Zero,
                                                       true,
                                                       0,
                                                       decryptedData,
                                                       ref dataLength);
                CheckStatus(ret);
            }

            // make sure we copy only appropriate data
            // dataLength will contain the length of the encrypted
            // data buffer
            byte[] result = new byte[dataLength];

            Array.Copy(decryptedData, 0, result, 0, dataLength);

            // zero out the decryptedData buffer
            for (int i = 0; i < decryptedData.Length; i++)
            {
                decryptedData[i] = 0;
            }

            return result;
        }

        /// <summary>
        /// Generates key pair in a thread safe manner
        /// the first time when required.
        /// </summary>
        internal void GenerateKeyPair()
        {
            if (!s_keyPairGenerated)
            {
                lock (s_syncObject)
                {
                    if (!s_keyPairGenerated)
                    {
                        s_hStaticProv = new PSSafeCryptProvHandle();
                        // We need PROV_RSA_AES to support AES-256 symmetric key
                        // encryption. PROV_RSA_FULL supports only RC2 and RC4
                        bool ret = PSCryptoNativeUtils.CryptAcquireContext(ref s_hStaticProv,
                            null,
                            null,
                            PSCryptoNativeUtils.PROV_RSA_AES,
                            PSCryptoNativeUtils.CRYPT_VERIFYCONTEXT);

                        CheckStatus(ret);

                        s_hStaticRSAKey = new PSSafeCryptKey();
                        ret = PSCryptoNativeUtils.CryptGenKey(s_hStaticProv,
                            PSCryptoNativeUtils.AT_KEYEXCHANGE,
                            0x08000000 | PSCryptoNativeUtils.CRYPT_EXPORTABLE,  // key length -> 2048
                            ref s_hStaticRSAKey);

                        CheckStatus(ret);

                        // key needs to be generated once
                        s_keyPairGenerated = true;
                    }
                }
            }

            _hProv = s_hStaticProv;
            _hRSAKey = s_hStaticRSAKey;
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
            PSRSACryptoServiceProvider cryptoProvider = new PSRSACryptoServiceProvider(false);

            // set the handles for provider and rsa key
            cryptoProvider._hProv = s_hStaticProv;
            cryptoProvider._hRSAKey = s_hStaticRSAKey;

            return cryptoProvider;
        }

        /// <summary>
        /// Returns a crypto service provider for use in the
        /// server. This will not generate a key pair.
        /// </summary>
        /// <returns>Crypto service provider for
        /// the server side.</returns>
        internal static PSRSACryptoServiceProvider GetRSACryptoServiceProviderForServer()
        {
            PSRSACryptoServiceProvider cryptoProvider = new PSRSACryptoServiceProvider(true);

            return cryptoProvider;
        }

        #endregion Internal Static Methods

        #region Private Methods

        /// <summary>
        /// Checks the status of a call, if it had resulted in an error
        /// then obtains the last error, wraps it in an exception and
        /// throws the same.
        /// </summary>
        /// <param name="value">Value to examine.</param>
        private void CheckStatus(bool value)
        {
            if (value)
            {
                return;
            }

            uint errorCode = PSCryptoNativeUtils.GetLastError();
            StringBuilder errorMessage = new StringBuilder(new ComponentModel.Win32Exception(unchecked((int)errorCode)).Message);

            throw new PSCryptoException(errorCode, errorMessage);
        }

        #endregion Private Methods

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
                if (_hSessionKey != null)
                {
                    if (!_hSessionKey.IsInvalid)
                    {
                        _hSessionKey.Dispose();
                    }

                    _hSessionKey = null;
                }

                // we need to dismiss the provider and key
                // only if the static members are not allocated
                // since otherwise, these are just references
                // to the static members

                if (s_hStaticRSAKey == null)
                {
                    if (_hRSAKey != null)
                    {
                        if (!_hRSAKey.IsInvalid)
                        {
                            _hRSAKey.Dispose();
                        }

                        _hRSAKey = null;
                    }
                }

                if (s_hStaticProv == null)
                {
                    if (_hProv != null)
                    {
                        if (!_hProv.IsInvalid)
                        {
                            _hProv.Dispose();
                        }

                        _hProv = null;
                    }
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
#if UNIX
            _rsaCryptoProvider = null;
#else
            _rsaCryptoProvider = PSRSACryptoServiceProvider.GetRSACryptoServiceProviderForServer();
#endif
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

            // _session = new RemoteSession();
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
