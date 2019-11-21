// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Inspired by: https://github.com/mono/mono/blob/58a34261c9af13f9a223920f111005d39e7a3d9e/mcs/class/Mono.Security/Mono.Security.Cryptography/CryptoConvert.cs
// Blob formats: https://docs.microsoft.com/en-us/windows/win32/seccrypto/base-provider-key-blobs

using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace System.Management.Automation.Internal
{
    /// <summary>
    /// Converts between CAPI-compatible blob formats and managed crypto algorithm parameters.
    /// </summary>
    internal static class CryptoConvert
    {
        private static int ToInt32LE (byte[] bytes, int offset)
		{
			return (bytes [offset+3] << 24) | (bytes [offset+2] << 16) | (bytes [offset+1] << 8) | bytes [offset];
		}

		private static uint ToUInt32LE (byte[] bytes, int offset)
		{
			return (uint)((bytes [offset+3] << 24) | (bytes [offset+2] << 16) | (bytes [offset+1] << 8) | bytes [offset]);
		}

        private static byte[] GetBytesLE (int val)
		{
			return new byte [] { 
				(byte) (val & 0xff), 
				(byte) ((val >> 8) & 0xff), 
				(byte) ((val >> 16) & 0xff), 
				(byte) ((val >> 24) & 0xff)
			};
        }

		private static byte[] CreateReverseByteArray(byte[] data)
		{
            byte[] reverseData = new byte[data.Length];
            Array.Copy(data, reverseData, data.Length);
			Array.Reverse(reverseData, 0, reverseData.Length);
			return reverseData;
		}

        internal static RSA FromCapiPublicKeyBlob(byte[] blob) 
		{
			return FromCapiPublicKeyBlob(blob, 0);
		}

		private static RSA FromCapiPublicKeyBlob(byte[] blob, int offset) 
		{
			var rsap = GetParametersFromCapiPublicKeyBlob(blob, offset);

			try 
            {
				RSA rsa = RSA.Create ();
				rsa.ImportParameters (rsap);
				return rsa;
			} 
            catch (Exception ex) 
            {
				throw new CryptographicException ("Invalid public key blob.", ex);
			}
		}

		private static RSAParameters GetParametersFromCapiPublicKeyBlob(byte[] blob, int offset)
		{
			if (blob == null)
				throw new ArgumentNullException("blob");
			if (offset >= blob.Length)
				throw new ArgumentException("Public key blob is too small.");

			try 
            {
				if ((blob [offset]   != 0x06) ||				// PUBLICKEYBLOB (0x06)
				    (blob [offset+1] != 0x02) ||				// Version (0x02)
				    (blob [offset+2] != 0x00) ||				// Reserved (word)
				    (blob [offset+3] != 0x00) || 
				    (ToUInt32LE (blob, offset+8) != 0x31415352))	// DWORD magic = RSA1
					throw new CryptographicException("Invalid blob header");

				// ALGID (CALG_RSA_SIGN, CALG_RSA_KEYX, ...)
				// int algId = ToInt32LE (blob, offset+4);

				// DWORD bitlen
				int bitLen = ToInt32LE(blob, offset+12);

				// DWORD public exponent
				RSAParameters rsap = new RSAParameters();
				rsap.Exponent = new byte [3];
				rsap.Exponent [0] = blob [offset+18];
				rsap.Exponent [1] = blob [offset+17];
				rsap.Exponent [2] = blob [offset+16];
			
				int pos = offset+20;
				// BYTE modulus[rsapubkey.bitlen/8];
				int byteLen = (bitLen >> 3);
				rsap.Modulus = new byte [byteLen];
				Buffer.BlockCopy (blob, pos, rsap.Modulus, 0, byteLen);
				Array.Reverse (rsap.Modulus);
				return rsap;
			} 
            catch (Exception ex) 
            {
				throw new CryptographicException("Invalid public key blob.", ex);
			}
		}

        internal static byte[] ToCapiPublicKeyBlob(RSA rsa) 
		{
			RSAParameters p = rsa.ExportParameters(false);
			int keyLength = p.Modulus.Length; // in bytes
			byte[] blob = new byte [20 + keyLength];

			blob [0] = 0x06;	// Type - PUBLICKEYBLOB (0x06)
			blob [1] = 0x02;	// Version - Always CUR_BLOB_VERSION (0x02)
			// [2], [3]		// RESERVED - Always 0
			blob [5] = 0xa4;	// ALGID - Always 00 a4 00 00 (for CALG_RSA_KEYX)
			blob [8] = 0x52;	// Magic - RSA1 (ASCII in hex)
			blob [9] = 0x53;
			blob [10] = 0x41;
			blob [11] = 0x31;

			byte[] bitlen = GetBytesLE(keyLength << 3);
			blob [12] = bitlen [0];	// bitlen
			blob [13] = bitlen [1];	
			blob [14] = bitlen [2];	
			blob [15] = bitlen [3];

			// public exponent (DWORD)
			int pos = 16;
			int n = p.Exponent.Length;
			while (n > 0)
				blob [pos++] = p.Exponent [--n];
			// modulus
			pos = 20;
			byte[] part = p.Modulus;
			int len = part.Length;
			Array.Reverse(part, 0, len);
			Buffer.BlockCopy(part, 0, blob, pos, len);
			pos += len;
			return blob;
		}

		internal static byte[] FromCapiSimpleKeyBlob(byte[] blob)
		{
			// just ignore the header of the capi blob and go straight for the key
            return CreateReverseByteArray(blob.Skip(12).ToArray());
		}

        internal static byte[] ToCapiSimpleKeyBlob(byte[] encryptedKey) 
        {
            // formulate the PUBLICKEYSTRUCT
			byte[] blob = new byte[12 + encryptedKey.Length];

			blob [0] = 0x01;	// Type - SIMPLEBLOB (0x01)
			blob [1] = 0x02;	// Version - Always CUR_BLOB_VERSION (0x02)
			// [2], [3]		    // RESERVED - Always 0
			blob [4] = 0x10;    // AES-256 algo id 
			blob [5] = 0x66;   	
			// [6], [7], [8]    // 0x00 
			blob [9] = 0xa4;
			// [10], [11]       // 0x00 

            // create a reversed copy and add the encrypted key
			byte[] reversedKey = CreateReverseByteArray(encryptedKey);
			Buffer.BlockCopy(reversedKey, 0, blob, 12, reversedKey.Length);

			return blob;
        }
    }
}
