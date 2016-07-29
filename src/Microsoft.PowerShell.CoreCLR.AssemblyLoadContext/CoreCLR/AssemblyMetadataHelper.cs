/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Cryptography;

namespace System.Management.Automation
{
    internal static class AssemblyMetadataHelper
    {
        /// <summary>
        /// Construct the strong assembly name from metadata
        /// </summary>
        internal static string GetAssemblyStrongName(MetadataReader metadataReader)
        {
            AssemblyDefinition assemblyDefinition = metadataReader.GetAssemblyDefinition();
            string asmName = metadataReader.GetString(assemblyDefinition.Name);
            string asmVersion = assemblyDefinition.Version.ToString();
            string asmCulture = metadataReader.GetString(assemblyDefinition.Culture);
            asmCulture = (asmCulture == string.Empty) ? "neutral" : asmCulture;

            AssemblyHashAlgorithm hashAlgorithm = assemblyDefinition.HashAlgorithm;
            BlobHandle blobHandle = assemblyDefinition.PublicKey;
            BlobReader blobReader = metadataReader.GetBlobReader(blobHandle);

            string publicKeyTokenString = "null";
            // Extract public key token only if PublicKey exists in the metadata
            if (blobReader.Length > 0)
            {
                byte[] publickey = blobReader.ReadBytes(blobReader.Length);

                HashAlgorithm hashImpl = null;
                switch (hashAlgorithm)
                {
                    case AssemblyHashAlgorithm.Sha1:
                        hashImpl = SHA1.Create();
                        break;
                    case AssemblyHashAlgorithm.MD5:
                        hashImpl = MD5.Create();
                        break;
                    case AssemblyHashAlgorithm.Sha256:
                        hashImpl = SHA256.Create();
                        break;
                    case AssemblyHashAlgorithm.Sha384:
                        hashImpl = SHA384.Create();
                        break;
                    case AssemblyHashAlgorithm.Sha512:
                        hashImpl = SHA512.Create();
                        break;
                    default:
                        throw new NotSupportedException();
                }

                byte[] publicKeyHash = hashImpl.ComputeHash(publickey);
                byte[] publicKeyTokenBytes = new byte[8];
                // Note that, the low 8 bytes of the hash of public key in reverse order is the public key tokens.
                for (int i = 1; i <= 8; i++)
                {
                    publicKeyTokenBytes[i - 1] = publicKeyHash[publicKeyHash.Length - i];
                }

                // Convert bytes to hex format strings in lower case.
                publicKeyTokenString = BitConverter.ToString(publicKeyTokenBytes).Replace("-", string.Empty).ToLowerInvariant();
            }

            string strongAssemblyName = string.Format(CultureInfo.InvariantCulture,
                                                      "{0}, Version={1}, Culture={2}, PublicKeyToken={3}",
                                                      asmName, asmVersion, asmCulture, publicKeyTokenString);

            return strongAssemblyName;
        }
    }
}
