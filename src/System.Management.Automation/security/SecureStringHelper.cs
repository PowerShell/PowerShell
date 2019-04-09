// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Text;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// Helper class for secure string related functionality.
    /// </summary>
    internal static class SecureStringHelper
    {
        // Some random hex characters to identify the beginning of a
        // V2-exported SecureString.
        internal static string SecureStringExportHeader = "76492d1116743f0423413b16050a5345";

        /// <summary>
        /// Create a new SecureString based on the specified binary data.
        ///
        /// The binary data must be byte[] version of unicode char[],
        /// otherwise the results are unpredictable.
        /// </summary>
        /// <param name="data">Input data.</param>
        /// <returns>A SecureString .</returns>
        private static SecureString New(byte[] data)
        {
            if ((data.Length % 2) != 0)
            {
                // If the data is not an even length, they supplied an invalid key
                string error = Serialization.InvalidKey;
                throw new PSArgumentException(error);
            }

            char ch;
            SecureString ss = new SecureString();

            //
            // each unicode char is 2 bytes.
            //
            int len = data.Length / 2;

            for (int i = 0; i < len; i++)
            {
                ch = (char)(data[2 * i + 1] * 256 + data[2 * i]);
                ss.AppendChar(ch);

                //
                // zero out the data slots as soon as we use them
                //
                data[2 * i] = 0;
                data[2 * i + 1] = 0;
            }

            return ss;
        }

        /// <summary>
        /// Get the contents of a SecureString as byte[]
        /// </summary>
        /// <param name="s">Input string.</param>
        /// <returns>Contents of s (char[]) converted to byte[].</returns>
        [ArchitectureSensitive]
        internal static byte[] GetData(SecureString s)
        {
            //
            // each unicode char is 2 bytes.
            //
            byte[] data = new byte[s.Length * 2];

            if (s.Length > 0)
            {
                IntPtr ptr = Marshal.SecureStringToCoTaskMemUnicode(s);

                try
                {
                    Marshal.Copy(ptr, data, 0, data.Length);
                }
                finally
                {
                    Marshal.ZeroFreeCoTaskMemUnicode(ptr);
                }
            }

            return data;
        }

        /// <summary>
        /// Encode the specified byte[] as a unicode string.
        ///
        /// Currently we use simple hex encoding but this
        /// method can be changed to use a better encoding
        /// such as base64.
        /// </summary>
        /// <param name="data">Binary data to encode.</param>
        /// <returns>A string representing encoded data.</returns>
        internal static string ByteArrayToString(byte[] data)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                sb.Append(data[i].ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Convert a string obtained using ByteArrayToString()
        /// back to byte[] format.
        /// </summary>
        /// <param name="s">Encoded input string.</param>
        /// <returns>Bin data as byte[].</returns>
        internal static byte[] ByteArrayFromString(string s)
        {
            //
            // two hex chars per byte
            //
            int dataLen = s.Length / 2;
            byte[] data = new byte[dataLen];

            if (s.Length > 0)
            {
                for (int i = 0; i < dataLen; i++)
                {
                    data[i] = byte.Parse(s.Substring(2 * i, 2),
                                         NumberStyles.AllowHexSpecifier,
                                         System.Globalization.CultureInfo.InvariantCulture);
                }
            }

            return data;
        }

        /// <summary>
        /// Return contents of the SecureString after encrypting
        /// using DPAPI and encoding the encrypted blob as a string.
        /// </summary>
        /// <param name="input">SecureString to protect.</param>
        /// <returns>A string (see summary) .</returns>
        internal static string Protect(SecureString input)
        {
            Utils.CheckSecureStringArg(input, "input");

            string output = string.Empty;
            byte[] data = null;
            byte[] protectedData = null;

            data = GetData(input);
#if UNIX
            // DPAPI doesn't exist on UNIX so we simply use the string as a byte-array
            protectedData = data;
#else
            protectedData = ProtectedData.Protect(data, null,
                                                  DataProtectionScope.CurrentUser);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = 0;
            }
#endif

            output = ByteArrayToString(protectedData);

            return output;
        }

        /// <summary>
        /// Decrypts the specified string using DPAPI and return
        /// equivalent SecureString.
        ///
        /// The string must be obtained earlier by a call to Protect()
        /// </summary>
        /// <param name="input">Encrypted string.</param>
        /// <returns>SecureString .</returns>
        internal static SecureString Unprotect(string input)
        {
            Utils.CheckArgForNullOrEmpty(input, "input");
            if ((input.Length % 2) != 0)
            {
                throw PSTraceSource.NewArgumentException("input", Serialization.InvalidEncryptedString, input);
            }

            byte[] data = null;
            byte[] protectedData = null;
            SecureString s;

            protectedData = ByteArrayFromString(input);

#if UNIX
            // DPAPI isn't supported in UNIX, so we just translate the byte-array back to a string
            data = protectedData;
#else
            data = ProtectedData.Unprotect(protectedData, null,
                                           DataProtectionScope.CurrentUser);

#endif
            s = New(data);

            return s;
        }

        /// <summary>
        /// Return contents of the SecureString after encrypting
        /// using the specified key and encoding the encrypted blob as a string.
        /// </summary>
        /// <param name="input">Input string to encrypt.</param>
        /// <param name="key">Encryption key.</param>
        /// <returns>A string (see summary).</returns>
        internal static EncryptionResult Encrypt(SecureString input, SecureString key)
        {
            EncryptionResult output = null;

            //
            // get clear text key from the SecureString key
            //
            byte[] keyBlob = GetData(key);

            //
            // encrypt the data
            //
            output = Encrypt(input, keyBlob);

            //
            // clear the clear text key
            //
            Array.Clear(keyBlob, 0, keyBlob.Length);

            return output;
        }

        /// <summary>
        /// Return contents of the SecureString after encrypting
        /// using the specified key and encoding the encrypted blob as a string.
        /// </summary>
        /// <param name="input">Input string to encrypt.</param>
        /// <param name="key">Encryption key.</param>
        /// <returns>A string (see summary).</returns>
        internal static EncryptionResult Encrypt(SecureString input, byte[] key)
        {
            return Encrypt(input, key, null);
        }

        internal static EncryptionResult Encrypt(SecureString input, byte[] key, byte[] iv)
        {
            Utils.CheckSecureStringArg(input, "input");
            Utils.CheckKeyArg(key, "key");

            byte[] encryptedData = null;
            MemoryStream ms = null;
            ICryptoTransform encryptor = null;
            CryptoStream cs = null;

            //
            // prepare the crypto stuff. Initialization Vector is
            // randomized by default.
            //
            Aes aes = Aes.Create();
            if (iv == null)
                iv = aes.IV;

            encryptor = aes.CreateEncryptor(key, iv);
            ms = new MemoryStream();

            using (cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                //
                // get clear text data from the input SecureString
                //
                byte[] data = GetData(input);

                //
                // encrypt it
                //
                cs.Write(data, 0, data.Length);
                cs.FlushFinalBlock();

                //
                // clear the clear text data array
                //
                Array.Clear(data, 0, data.Length);

                //
                // convert the encrypted blob to a string
                //
                encryptedData = ms.ToArray();

                EncryptionResult output = new EncryptionResult(ByteArrayToString(encryptedData), Convert.ToBase64String(iv));

                return output;
            }
        }

        /// <summary>
        /// Decrypts the specified string using the specified key
        /// and return equivalent SecureString.
        ///
        /// The string must be obtained earlier by a call to Encrypt()
        /// </summary>
        /// <param name="input">Encrypted string.</param>
        /// <param name="key">Encryption key.</param>
        /// <param name="IV">Encryption initialization vector. If this is set to null, the method uses internally computed strong random number as IV.</param>
        /// <returns>SecureString .</returns>
        internal static SecureString Decrypt(string input, SecureString key, byte[] IV)
        {
            SecureString output = null;

            //
            // get clear text key from the SecureString key
            //
            byte[] keyBlob = GetData(key);

            //
            // decrypt the data
            //
            output = Decrypt(input, keyBlob, IV);

            //
            // clear the clear text key
            //
            Array.Clear(keyBlob, 0, keyBlob.Length);

            return output;
        }

        /// <summary>
        /// Decrypts the specified string using the specified key
        /// and return equivalent SecureString.
        ///
        /// The string must be obtained earlier by a call to Encrypt()
        /// </summary>
        /// <param name="input">Encrypted string.</param>
        /// <param name="key">Encryption key.</param>
        /// <param name="IV">Encryption initialization vector. If this is set to null, the method uses internally computed strong random number as IV.</param>
        /// <returns>SecureString .</returns>
        internal static SecureString Decrypt(string input, byte[] key, byte[] IV)
        {
            Utils.CheckArgForNullOrEmpty(input, "input");
            Utils.CheckKeyArg(key, "key");

            byte[] decryptedData = null;
            byte[] encryptedData = null;
            SecureString s = null;

            //
            // prepare the crypto stuff
            //
            Aes aes = Aes.Create();
            encryptedData = ByteArrayFromString(input);

            var decryptor = aes.CreateDecryptor(key, IV ?? aes.IV);

            MemoryStream ms = new MemoryStream(encryptedData);

            using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            {
                byte[] tempDecryptedData = new byte[encryptedData.Length];

                int numBytesRead = 0;

                //
                // decrypt the data
                //
                numBytesRead = cs.Read(tempDecryptedData, 0,
                                       tempDecryptedData.Length);

                decryptedData = new byte[numBytesRead];

                for (int i = 0; i < numBytesRead; i++)
                {
                    decryptedData[i] = tempDecryptedData[i];
                }

                s = New(decryptedData);
                Array.Clear(decryptedData, 0, decryptedData.Length);
                Array.Clear(tempDecryptedData, 0, tempDecryptedData.Length);

                return s;
            }
        }
    }

    /// <summary>
    /// Helper class to return encryption results, and the IV used to
    /// do the encryption.
    /// </summary>
    internal class EncryptionResult
    {
        internal EncryptionResult(string encrypted, string IV)
        {
            EncryptedData = encrypted;
            this.IV = IV;
        }

        /// <summary>
        /// Gets the encrypted data.
        /// </summary>
        internal string EncryptedData { get; }

        /// <summary>
        /// Gets the IV used to encrypt the data.
        /// </summary>
        internal string IV { get; }
    }

#if !UNIX

    // The DPAPIs implemented in this section are temporary workaround.
    // CoreCLR team will bring 'ProtectedData' type to Project K eventually.

    #region DPAPI

    internal enum DataProtectionScope
    {
        CurrentUser = 0x00,
        LocalMachine = 0x01
    }

    internal static class ProtectedData
    {
        /// <summary>
        /// Protect.
        /// </summary>
        public static byte[] Protect(byte[] userData, byte[] optionalEntropy, DataProtectionScope scope)
        {
            if (userData == null)
            {
                throw new ArgumentNullException("userData");
            }

            GCHandle pbDataIn = new GCHandle();
            GCHandle pOptionalEntropy = new GCHandle();
            CAPI.CRYPTOAPI_BLOB blob = new CAPI.CRYPTOAPI_BLOB();

            try
            {
                pbDataIn = GCHandle.Alloc(userData, GCHandleType.Pinned);
                CAPI.CRYPTOAPI_BLOB dataIn = new CAPI.CRYPTOAPI_BLOB();
                dataIn.cbData = (uint)userData.Length;
                dataIn.pbData = pbDataIn.AddrOfPinnedObject();
                CAPI.CRYPTOAPI_BLOB entropy = new CAPI.CRYPTOAPI_BLOB();
                if (optionalEntropy != null)
                {
                    pOptionalEntropy = GCHandle.Alloc(optionalEntropy, GCHandleType.Pinned);
                    entropy.cbData = (uint)optionalEntropy.Length;
                    entropy.pbData = pOptionalEntropy.AddrOfPinnedObject();
                }

                uint dwFlags = CAPI.CRYPTPROTECT_UI_FORBIDDEN;
                if (scope == DataProtectionScope.LocalMachine)
                    dwFlags |= CAPI.CRYPTPROTECT_LOCAL_MACHINE;
                unsafe
                {
                    if (!CAPI.CryptProtectData(
                        pDataIn: new IntPtr(&dataIn),
                        szDataDescr: string.Empty,
                        pOptionalEntropy: new IntPtr(&entropy),
                        pvReserved: IntPtr.Zero,
                        pPromptStruct: IntPtr.Zero,
                        dwFlags: dwFlags,
                        pDataBlob: new IntPtr(&blob)))
                    {
                        int lastWin32Error = Marshal.GetLastWin32Error();

                        // One of the most common reasons that DPAPI operations fail is that the user
                        // profile is not loaded (for instance in the case of impersonation or running in a
                        // service.  In those cases, throw an exception that provides more specific details
                        // about what happened.
                        if (CAPI.ErrorMayBeCausedByUnloadedProfile(lastWin32Error))
                        {
                            throw new CryptographicException("Cryptography_DpApi_ProfileMayNotBeLoaded");
                        }
                        else
                        {
                            throw new CryptographicException(lastWin32Error);
                        }
                    }
                }

                // In some cases, the API would fail due to OOM but simply return a null pointer.
                if (blob.pbData == IntPtr.Zero)
                {
                    throw new OutOfMemoryException();
                }

                byte[] encryptedData = new byte[(int)blob.cbData];
                Marshal.Copy(blob.pbData, encryptedData, 0, encryptedData.Length);

                return encryptedData;
            }
            finally
            {
                if (pbDataIn.IsAllocated)
                {
                    pbDataIn.Free();
                }
                if (pOptionalEntropy.IsAllocated)
                {
                    pOptionalEntropy.Free();
                }
                if (blob.pbData != IntPtr.Zero)
                {
                    CAPI.ZeroMemory(blob.pbData, blob.cbData);
                    CAPI.LocalFree(blob.pbData);
                }
            }
        }

        /// <summary>
        /// Unprotect.
        /// </summary>
        public static byte[] Unprotect(byte[] encryptedData, byte[] optionalEntropy, DataProtectionScope scope)
        {
            if (encryptedData == null)
            {
                throw new ArgumentNullException("encryptedData");
            }

            GCHandle pbDataIn = new GCHandle();
            GCHandle pOptionalEntropy = new GCHandle();
            CAPI.CRYPTOAPI_BLOB userData = new CAPI.CRYPTOAPI_BLOB();

            try
            {
                pbDataIn = GCHandle.Alloc(encryptedData, GCHandleType.Pinned);
                CAPI.CRYPTOAPI_BLOB dataIn = new CAPI.CRYPTOAPI_BLOB();
                dataIn.cbData = (uint)encryptedData.Length;
                dataIn.pbData = pbDataIn.AddrOfPinnedObject();
                CAPI.CRYPTOAPI_BLOB entropy = new CAPI.CRYPTOAPI_BLOB();
                if (optionalEntropy != null)
                {
                    pOptionalEntropy = GCHandle.Alloc(optionalEntropy, GCHandleType.Pinned);
                    entropy.cbData = (uint)optionalEntropy.Length;
                    entropy.pbData = pOptionalEntropy.AddrOfPinnedObject();
                }

                uint dwFlags = CAPI.CRYPTPROTECT_UI_FORBIDDEN;
                if (scope == DataProtectionScope.LocalMachine)
                {
                    dwFlags |= CAPI.CRYPTPROTECT_LOCAL_MACHINE;
                }

                unsafe
                {
                    if (!CAPI.CryptUnprotectData(
                        pDataIn: new IntPtr(&dataIn),
                        ppszDataDescr: IntPtr.Zero,
                        pOptionalEntropy: new IntPtr(&entropy),
                        pvReserved: IntPtr.Zero,
                        pPromptStruct: IntPtr.Zero,
                        dwFlags: dwFlags,
                        pDataBlob: new IntPtr(&userData)))
                    {
                        throw new CryptographicException(Marshal.GetLastWin32Error());
                    }
                }

                // In some cases, the API would fail due to OOM but simply return a null pointer.
                if (userData.pbData == IntPtr.Zero)
                {
                    throw new OutOfMemoryException();
                }

                byte[] data = new byte[(int)userData.cbData];
                Marshal.Copy(userData.pbData, data, 0, data.Length);

                return data;
            }
            finally
            {
                if (pbDataIn.IsAllocated)
                {
                    pbDataIn.Free();
                }
                if (pOptionalEntropy.IsAllocated)
                {
                    pOptionalEntropy.Free();
                }
                if (userData.pbData != IntPtr.Zero)
                {
                    CAPI.ZeroMemory(userData.pbData, userData.cbData);
                    CAPI.LocalFree(userData.pbData);
                }
            }
        }
    }

    internal static class CAPI
    {
        internal const uint CRYPTPROTECT_UI_FORBIDDEN = 0x1;
        internal const uint CRYPTPROTECT_LOCAL_MACHINE = 0x4;

        internal const int E_FILENOTFOUND = unchecked((int)0x80070002); // File not found
        internal const int ERROR_FILE_NOT_FOUND = 2;                    // File not found

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CRYPTOAPI_BLOB
        {
            internal uint cbData;
            internal IntPtr pbData;
        }

        internal static bool ErrorMayBeCausedByUnloadedProfile(int errorCode)
        {
            // CAPI returns a file not found error if the user profile is not yet loaded
            return errorCode == E_FILENOTFOUND ||
                   errorCode == ERROR_FILE_NOT_FOUND;
        }

        [DllImport("CRYPT32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CryptProtectData(
                [In]     IntPtr pDataIn,
                [In]     string szDataDescr,
                [In]     IntPtr pOptionalEntropy,
                [In]     IntPtr pvReserved,
                [In]     IntPtr pPromptStruct,
                [In]     uint dwFlags,
                [In, Out] IntPtr pDataBlob);

        [DllImport("CRYPT32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CryptUnprotectData(
                [In]     IntPtr pDataIn,
                [In]     IntPtr ppszDataDescr,
                [In]     IntPtr pOptionalEntropy,
                [In]     IntPtr pvReserved,
                [In]     IntPtr pPromptStruct,
                [In]     uint dwFlags,
                [In, Out] IntPtr pDataBlob);

        [DllImport("ntdll.dll", EntryPoint = "RtlZeroMemory", SetLastError = true)]
        internal static extern void ZeroMemory(IntPtr handle, uint length);

        [DllImport(PinvokeDllNames.LocalFreeDllName, SetLastError = true)]
        internal static extern IntPtr LocalFree(IntPtr handle);
    }

    #endregion DPAPI

#endif
}

