/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

namespace Microsoft.PowerShell.Workflow
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Management.Automation.Tracing;

    /// <summary>
    /// This class implements the encrypt and decrypt functionality.
    /// </summary>
    internal class InstanceStoreCryptography
    {
        /// <summary>
        /// Tracer initialization.
        /// </summary>
        private static readonly PowerShellTraceSource Tracer = PowerShellTraceSourceFactory.GetTraceSource();

        /// <summary>
        /// The additional entry for security 'POWERSHELLWORKFLOW'
        /// </summary>
        private static byte[] s_aditionalEntropy = { (byte)'P', (byte)'O', (byte)'W', (byte)'E', (byte)'R', (byte)'S', (byte)'H', (byte)'E', (byte)'L', (byte)'L', (byte)'W', (byte)'O', (byte)'R', (byte)'K', (byte)'F', (byte)'L', (byte)'O', (byte)'W' };
        
        /// <summary>
        /// Protect the data.
        /// </summary>
        /// <param name="data">The input data for encryption.</param>
        /// <returns>Returns encrypted data.</returns>
        internal static byte[] Protect(byte[] data)
        {
            try
            {
                // Encrypt the data using DataProtectionScope.CurrentUser. The result can be decrypted
                //  only by the same current user.
                return ProtectedData.Protect(data, s_aditionalEntropy, DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException e)
            {
                Tracer.TraceException(e);

                throw e;
            }
        }

        /// <summary>
        /// Unprotect data the encrypted data.
        /// </summary>
        /// <param name="data">Encrypted data.</param>
        /// <returns>Returns decrypted data.</returns>
        internal static byte[] Unprotect(byte[] data)
        {
            try
            {
                return ProtectedData.Unprotect(data, s_aditionalEntropy, DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException e)
            {
                Tracer.TraceException(e);

                throw e;
            }

        }
    }
}
