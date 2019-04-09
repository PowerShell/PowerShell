// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691
#pragma warning disable 56523

using Dbg = System.Management.Automation;
using System.IO;
using System.Management.Automation.Internal;
using System.Management.Automation.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using DWORD = System.UInt32;

namespace System.Management.Automation
{
    /// <summary>
    /// Defines the options that control what data is embedded in the
    /// signature blob.
    /// </summary>
    public enum SigningOption
    {
        /// <summary>
        /// Embeds only the signer's certificate.
        /// </summary>
        AddOnlyCertificate,

        /// <summary>
        /// Embeds the entire certificate chain.
        /// </summary>
        AddFullCertificateChain,

        /// <summary>
        /// Embeds the entire certificate chain, except for the root
        /// certificate.
        /// </summary>
        AddFullCertificateChainExceptRoot,

        /// <summary>
        /// Default: Embeds the entire certificate chain, except for the
        /// root certificate.
        /// </summary>
        Default = AddFullCertificateChainExceptRoot
    }

    /// <summary>
    /// Helper functions for signature functionality.
    /// </summary>
    internal static class SignatureHelper
    {
        /// <summary>
        /// Tracer for SignatureHelper.
        /// </summary>
        [Dbg.TraceSource("SignatureHelper",
                          "tracer for SignatureHelper")]
        private static readonly Dbg.PSTraceSource s_tracer =
            Dbg.PSTraceSource.GetTracer("SignatureHelper",
                          "tracer for SignatureHelper");

        /// <summary>
        /// Sign a file.
        /// </summary>
        /// <param name="option">Option that controls what gets embedded in the signature blob.</param>
        /// <param name="fileName">Name of file to sign.</param>
        /// <param name="certificate">Signing cert.</param>
        /// <param name="timeStampServerUrl">URL of time stamping server.</param>
        /// <param name="hashAlgorithm"> The name of the hash
        /// algorithm to use.</param>
        /// <returns>Does not return a value.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if argument fileName or certificate is null.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Thrown if
        /// -- argument fileName is empty OR
        /// -- the specified certificate is not suitable for
        ///    signing code
        /// </exception>
        /// <exception cref="System.Security.Cryptography.CryptographicException">
        /// This exception can be thrown if any cryptographic error occurs.
        /// It is not possible to know exactly what went wrong.
        /// This is because of the way CryptographicException is designed.
        /// Possible reasons:
        ///  -- certificate is invalid
        ///  -- certificate has no private key
        ///  -- certificate password mismatch
        ///  -- etc
        /// </exception>
        /// <exception cref="System.IO.FileNotFoundException">
        /// Thrown if the file specified by argument fileName is not found
        /// </exception>
        [ArchitectureSensitive]
        internal static Signature SignFile(SigningOption option,
                                           string fileName,
                                           X509Certificate2 certificate,
                                           string timeStampServerUrl,
                                           string hashAlgorithm)
        {
            bool result = false;
            Signature signature = null;
            IntPtr pSignInfo = IntPtr.Zero;
            DWORD error = 0;
            string hashOid = null;

            Utils.CheckArgForNullOrEmpty(fileName, "fileName");
            Utils.CheckArgForNull(certificate, "certificate");

            // If given, TimeStamp server URLs must begin with http://
            if (!string.IsNullOrEmpty(timeStampServerUrl))
            {
                if ((timeStampServerUrl.Length <= 7) ||
                    (timeStampServerUrl.IndexOf("http://", StringComparison.OrdinalIgnoreCase) != 0))
                {
                    throw PSTraceSource.NewArgumentException(
                        "certificate",
                        Authenticode.TimeStampUrlRequired);
                }
            }

            // Validate that the hash algorithm is valid
            if (!string.IsNullOrEmpty(hashAlgorithm))
            {
                IntPtr intptrAlgorithm = Marshal.StringToHGlobalUni(hashAlgorithm);

                IntPtr oidPtr = NativeMethods.CryptFindOIDInfo(NativeConstants.CRYPT_OID_INFO_NAME_KEY,
                        intptrAlgorithm,
                        0);

                // If we couldn't find an OID for the hash
                // algorithm, it was invalid.
                if (oidPtr == IntPtr.Zero)
                {
                    throw PSTraceSource.NewArgumentException(
                        "certificate",
                        Authenticode.InvalidHashAlgorithm);
                }
                else
                {
                    NativeMethods.CRYPT_OID_INFO oidInfo =
                        Marshal.PtrToStructure<NativeMethods.CRYPT_OID_INFO>(oidPtr);

                    hashOid = oidInfo.pszOID;
                }
            }

            if (!SecuritySupport.CertIsGoodForSigning(certificate))
            {
                throw PSTraceSource.NewArgumentException(
                        "certificate",
                        Authenticode.CertNotGoodForSigning);
            }

            SecuritySupport.CheckIfFileExists(fileName);
            // SecurityUtils.CheckIfFileSmallerThan4Bytes(fileName);

            try
            {
                // CryptUI is not documented either way, but does not
                // support empty strings for the timestamp server URL.
                // It expects null, only.  Instead, it randomly AVs if you
                // try.
                string timeStampServerUrlForCryptUI = null;
                if (!string.IsNullOrEmpty(timeStampServerUrl))
                {
                    timeStampServerUrlForCryptUI = timeStampServerUrl;
                }

                //
                // first initialize the struct to pass to
                // CryptUIWizDigitalSign() function
                //
                NativeMethods.CRYPTUI_WIZ_DIGITAL_SIGN_INFO si = NativeMethods.InitSignInfoStruct(fileName,
                                                              certificate,
                                                              timeStampServerUrlForCryptUI,
                                                              hashOid,
                                                              option);

                pSignInfo = Marshal.AllocCoTaskMem(Marshal.SizeOf(si));
                Marshal.StructureToPtr(si, pSignInfo, false);

                //
                // sign the file
                //
                // The GetLastWin32Error of this is checked, but PreSharp doesn't seem to be
                // able to see that.
#pragma warning disable 56523
                result = NativeMethods.CryptUIWizDigitalSign(
                    (DWORD)NativeMethods.CryptUIFlags.CRYPTUI_WIZ_NO_UI,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    pSignInfo,
                    IntPtr.Zero);
#pragma warning restore 56523

                if (si.pSignExtInfo != null)
                {
                    Marshal.DestroyStructure<NativeMethods.CRYPTUI_WIZ_DIGITAL_SIGN_EXTENDED_INFO>(si.pSignExtInfo);
                    Marshal.FreeCoTaskMem(si.pSignExtInfo);
                }

                if (!result)
                {
                    error = GetLastWin32Error();

                    //
                    // ISSUE-2004/05/08-kumarp : there seems to be a bug
                    // in CryptUIWizDigitalSign().
                    // It returns 80004005 or 80070001
                    // but it signs the file correctly. Mask this error
                    // till we figure out this odd behavior.
                    //
                    if ((error == 0x80004005) ||
                        (error == 0x80070001) ||

                        // CryptUIWizDigitalSign introduced a breaking change in Win8 to return this
                        // error code (ERROR_INTERNET_NAME_NOT_RESOLVED) when you provide an invalid
                        // timestamp server. It used to be 0x80070001.
                        // Also masking this out so that we don't introduce a breaking change ourselves.
                        (error == 0x80072EE7)
                        )
                    {
                        result = true;
                    }
                    else
                    {
                        if (error == Win32Errors.NTE_BAD_ALGID)
                        {
                            throw PSTraceSource.NewArgumentException(
                                "certificate",
                                Authenticode.InvalidHashAlgorithm);
                        }

                        s_tracer.TraceError("CryptUIWizDigitalSign: failed: {0:x}",
                                          error);
                    }
                }

                if (result)
                {
                    signature = GetSignature(fileName, null);
                }
                else
                {
                    signature = new Signature(fileName, (DWORD)error);
                }
            }
            finally
            {
                Marshal.DestroyStructure<NativeMethods.CRYPTUI_WIZ_DIGITAL_SIGN_INFO>(pSignInfo);
                Marshal.FreeCoTaskMem(pSignInfo);
            }

            return signature;
        }

        /// <summary>
        /// Get signature on the specified file.
        /// </summary>
        /// <param name="fileName">Name of file to check.</param>
        /// <param name="fileContent">Content of file to check.</param>
        /// <returns>Signature object.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if argument fileName is empty.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if argument fileName is null
        /// </exception>
        /// <exception cref="System.IO.FileNotFoundException">
        /// Thrown if the file specified by argument fileName is not found.
        /// </exception>
        [ArchitectureSensitive]
        internal static Signature GetSignature(string fileName, string fileContent)
        {
            Signature signature = null;

            if (fileContent == null)
            {
                // First, try to get the signature from the catalog signature APIs.
                signature = GetSignatureFromCatalog(fileName);
            }

            // If there is no signature or it is invalid, go by the file content
            // with the older WinVerifyTrust APIs
            if ((signature == null) || (signature.Status != SignatureStatus.Valid))
            {
                signature = GetSignatureFromWinVerifyTrust(fileName, fileContent);
            }

            return signature;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods")]
        private static Signature GetSignatureFromCatalog(string filename)
        {
            if (Signature.CatalogApiAvailable.HasValue && !Signature.CatalogApiAvailable.Value)
            {
                // Signature.CatalogApiAvailable would be set to false the first time it is detected that
                // WTGetSignatureInfo API does not exist on the platform, or if the API is not functional on the target platform.
                // Just return from the function instead of revalidating.
                return null;
            }

            Signature signature = null;

            Utils.CheckArgForNullOrEmpty(filename, "fileName");
            SecuritySupport.CheckIfFileExists(filename);

            try
            {
                using (FileStream stream = File.OpenRead(filename))
                {
                    NativeMethods.SIGNATURE_INFO sigInfo = new NativeMethods.SIGNATURE_INFO();
                    sigInfo.cbSize = (uint)Marshal.SizeOf(sigInfo);

                    IntPtr ppCertContext = IntPtr.Zero;
                    IntPtr phStateData = IntPtr.Zero;

                    try
                    {
                        int hresult = NativeMethods.WTGetSignatureInfo(filename, stream.SafeFileHandle.DangerousGetHandle(),
                            NativeMethods.SIGNATURE_INFO_FLAGS.SIF_CATALOG_SIGNED |
                            NativeMethods.SIGNATURE_INFO_FLAGS.SIF_CATALOG_FIRST |
                            NativeMethods.SIGNATURE_INFO_FLAGS.SIF_AUTHENTICODE_SIGNED |
                            NativeMethods.SIGNATURE_INFO_FLAGS.SIF_BASE_VERIFICATION |
                            NativeMethods.SIGNATURE_INFO_FLAGS.SIF_CHECK_OS_BINARY,
                            ref sigInfo, ref ppCertContext, ref phStateData);

                        if (Utils.Succeeded(hresult))
                        {
                            DWORD error = GetErrorFromSignatureState(sigInfo.nSignatureState);

                            X509Certificate2 cert = null;

                            if (ppCertContext != IntPtr.Zero)
                            {
                                cert = new X509Certificate2(ppCertContext);

                                // Get the time stamper certificate if available
                                TryGetProviderSigner(phStateData, out IntPtr pProvSigner, out X509Certificate2 timestamperCert);
                                if (timestamperCert != null)
                                {
                                    signature = new Signature(filename, error, cert, timestamperCert);
                                }
                                else
                                {
                                    signature = new Signature(filename, error, cert);
                                }

                                switch (sigInfo.nSignatureType)
                                {
                                    case NativeMethods.SIGNATURE_INFO_TYPE.SIT_AUTHENTICODE: signature.SignatureType = SignatureType.Authenticode; break;
                                    case NativeMethods.SIGNATURE_INFO_TYPE.SIT_CATALOG: signature.SignatureType = SignatureType.Catalog; break;
                                }

                                if (sigInfo.fOSBinary == 1)
                                {
                                    signature.IsOSBinary = true;
                                }
                            }
                            else
                            {
                                signature = new Signature(filename, error);
                            }

                            if (!Signature.CatalogApiAvailable.HasValue)
                            {
                                string productFile = Path.Combine(Utils.DefaultPowerShellAppBase, "Modules\\PSDiagnostics\\PSDiagnostics.psm1");
                                if (signature.Status != SignatureStatus.Valid)
                                {
                                    if (string.Equals(filename, productFile, StringComparison.OrdinalIgnoreCase))
                                    {
                                        Signature.CatalogApiAvailable = false;
                                    }
                                    else
                                    {
                                        // ProductFile has to be Catalog signed. Hence validating
                                        // to see if the Catalog API is functional using the ProductFile.
                                        Signature productFileSignature = GetSignatureFromCatalog(productFile);
                                        Signature.CatalogApiAvailable = (productFileSignature != null && productFileSignature.Status == SignatureStatus.Valid);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // If calling NativeMethods.WTGetSignatureInfo failed (returned a non-zero value), we still want to set Signature.CatalogApiAvailable to false.
                            Signature.CatalogApiAvailable = false;
                        }
                    }
                    finally
                    {
                        if (phStateData != IntPtr.Zero)
                        {
                            NativeMethods.FreeWVTStateData(phStateData);
                        }

                        if (ppCertContext != IntPtr.Zero)
                        {
                            NativeMethods.CertFreeCertificateContext(ppCertContext);
                        }
                    }
                }
            }
            catch (TypeLoadException)
            {
                // If we don't have WTGetSignatureInfo, don't return a Signature.
                Signature.CatalogApiAvailable = false;
                return null;
            }

            return signature;
        }

        private static DWORD GetErrorFromSignatureState(NativeMethods.SIGNATURE_STATE state)
        {
            switch (state)
            {
                case NativeMethods.SIGNATURE_STATE.SIGNATURE_STATE_UNSIGNED_MISSING: return Win32Errors.TRUST_E_NOSIGNATURE;
                case NativeMethods.SIGNATURE_STATE.SIGNATURE_STATE_UNSIGNED_UNSUPPORTED: return Win32Errors.TRUST_E_NOSIGNATURE;
                case NativeMethods.SIGNATURE_STATE.SIGNATURE_STATE_UNSIGNED_POLICY: return Win32Errors.TRUST_E_NOSIGNATURE;
                case NativeMethods.SIGNATURE_STATE.SIGNATURE_STATE_INVALID_CORRUPT: return Win32Errors.TRUST_E_BAD_DIGEST;
                case NativeMethods.SIGNATURE_STATE.SIGNATURE_STATE_INVALID_POLICY: return Win32Errors.CRYPT_E_BAD_MSG;
                case NativeMethods.SIGNATURE_STATE.SIGNATURE_STATE_VALID: return Win32Errors.NO_ERROR;
                case NativeMethods.SIGNATURE_STATE.SIGNATURE_STATE_TRUSTED: return Win32Errors.NO_ERROR;
                case NativeMethods.SIGNATURE_STATE.SIGNATURE_STATE_UNTRUSTED: return Win32Errors.TRUST_E_EXPLICIT_DISTRUST;

                // Should not happen
                default:
                    System.Diagnostics.Debug.Fail("Should not get here - could not map SIGNATURE_STATE");
                    return Win32Errors.TRUST_E_NOSIGNATURE;
            }
        }

        private static Signature GetSignatureFromWinVerifyTrust(string fileName, string fileContent)
        {
            Signature signature = null;

            NativeMethods.WINTRUST_DATA wtd;
            DWORD error = Win32Errors.E_FAIL;

            if (fileContent == null)
            {
                Utils.CheckArgForNullOrEmpty(fileName, "fileName");
                SecuritySupport.CheckIfFileExists(fileName);
                // SecurityUtils.CheckIfFileSmallerThan4Bytes(fileName);
            }

            try
            {
                error = GetWinTrustData(fileName, fileContent, out wtd);

                if (error != Win32Errors.NO_ERROR)
                {
                    s_tracer.WriteLine("GetWinTrustData failed: {0:x}", error);
                }

                signature = GetSignatureFromWintrustData(fileName, error, wtd);

                error = NativeMethods.DestroyWintrustDataStruct(wtd);

                if (error != Win32Errors.NO_ERROR)
                {
                    s_tracer.WriteLine("DestroyWinTrustDataStruct failed: {0:x}", error);
                }
            }
            catch (AccessViolationException)
            {
                signature = new Signature(fileName, Win32Errors.TRUST_E_NOSIGNATURE);
            }

            return signature;
        }

        [ArchitectureSensitive]
        private static DWORD GetWinTrustData(string fileName, string fileContent,
                                            out NativeMethods.WINTRUST_DATA wtData)
        {
            DWORD dwResult = Win32Errors.E_FAIL;
            IntPtr WINTRUST_ACTION_GENERIC_VERIFY_V2 = IntPtr.Zero;
            IntPtr wtdBuffer = IntPtr.Zero;

            Guid actionVerify =
                new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

            try
            {
                WINTRUST_ACTION_GENERIC_VERIFY_V2 =
                    Marshal.AllocCoTaskMem(Marshal.SizeOf(actionVerify));
                Marshal.StructureToPtr(actionVerify,
                                       WINTRUST_ACTION_GENERIC_VERIFY_V2,
                                       false);

                NativeMethods.WINTRUST_DATA wtd;

                if (fileContent == null)
                {
                    NativeMethods.WINTRUST_FILE_INFO wfi = NativeMethods.InitWintrustFileInfoStruct(fileName);
                    wtd = NativeMethods.InitWintrustDataStructFromFile(wfi);
                }
                else
                {
                    NativeMethods.WINTRUST_BLOB_INFO wbi = NativeMethods.InitWintrustBlobInfoStruct(fileName, fileContent);
                    wtd = NativeMethods.InitWintrustDataStructFromBlob(wbi);
                }

                wtdBuffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(wtd));
                Marshal.StructureToPtr(wtd, wtdBuffer, false);

                // The result is returned to the caller, and handled generically.
                // Disable the PreFast check for Win32 error codes, as we don't care.
#pragma warning disable 56523
                dwResult = NativeMethods.WinVerifyTrust(
                    IntPtr.Zero,
                    WINTRUST_ACTION_GENERIC_VERIFY_V2,
                    wtdBuffer);
#pragma warning restore 56523

                wtData = Marshal.PtrToStructure<NativeMethods.WINTRUST_DATA>(wtdBuffer);
            }
            finally
            {
                Marshal.DestroyStructure<Guid>(WINTRUST_ACTION_GENERIC_VERIFY_V2);
                Marshal.FreeCoTaskMem(WINTRUST_ACTION_GENERIC_VERIFY_V2);
                Marshal.DestroyStructure<NativeMethods.WINTRUST_DATA>(wtdBuffer);
                Marshal.FreeCoTaskMem(wtdBuffer);
            }

            return dwResult;
        }

        [ArchitectureSensitive]
        private static X509Certificate2 GetCertFromChain(IntPtr pSigner)
        {
            X509Certificate2 signerCert = null;

            // We don't care about the Win32 error code here, so disable
            // the PreFast complaint that we're not retrieving it.
#pragma warning disable 56523
            IntPtr pCert =
                NativeMethods.WTHelperGetProvCertFromChain(pSigner, 0);
#pragma warning restore 56523

            if (pCert != IntPtr.Zero)
            {
                NativeMethods.CRYPT_PROVIDER_CERT provCert =
                    (NativeMethods.CRYPT_PROVIDER_CERT)
                    Marshal.PtrToStructure<NativeMethods.CRYPT_PROVIDER_CERT>(pCert);
                signerCert = new X509Certificate2(provCert.pCert);
            }

            return signerCert;
        }

        [ArchitectureSensitive]
        private static Signature GetSignatureFromWintrustData(
            string filePath,
            DWORD error,
            NativeMethods.WINTRUST_DATA wtd)
        {
            s_tracer.WriteLine("GetSignatureFromWintrustData: error: {0}", error);

            Signature signature = null;
            if (TryGetProviderSigner(wtd.hWVTStateData, out IntPtr pProvSigner, out X509Certificate2 timestamperCert))
            {
                //
                // get cert of the signer
                //
                X509Certificate2 signerCert = GetCertFromChain(pProvSigner);

                if (signerCert != null)
                {
                    if (timestamperCert != null)
                    {
                        signature = new Signature(filePath,
                                                  error,
                                                  signerCert,
                                                  timestamperCert);
                    }
                    else
                    {
                        signature = new Signature(filePath,
                                                  error,
                                                  signerCert);
                    }

                    signature.SignatureType = SignatureType.Authenticode;
                }
            }

            Diagnostics.Assert(error != 0 || signature != null, "GetSignatureFromWintrustData: general crypto failure");

            if ((signature == null) && (error != 0))
            {
                signature = new Signature(filePath, error);
            }

            return signature;
        }

        [ArchitectureSensitive]
        private static bool TryGetProviderSigner(IntPtr wvtStateData, out IntPtr pProvSigner, out X509Certificate2 timestamperCert)
        {
            pProvSigner = IntPtr.Zero;
            timestamperCert = null;

            // The GetLastWin32Error of this is checked, but PreSharp doesn't seem to be
            // able to see that.
#pragma warning disable 56523
            IntPtr pProvData =
                NativeMethods.WTHelperProvDataFromStateData(wvtStateData);
#pragma warning restore 56523

            if (pProvData != IntPtr.Zero)
            {
                pProvSigner =
                    NativeMethods.WTHelperGetProvSignerFromChain(pProvData, 0, 0, 0);

                if (pProvSigner != IntPtr.Zero)
                {
                    NativeMethods.CRYPT_PROVIDER_SGNR provSigner =
                        (NativeMethods.CRYPT_PROVIDER_SGNR)
                        Marshal.PtrToStructure<NativeMethods.CRYPT_PROVIDER_SGNR>(pProvSigner);
                    if (provSigner.csCounterSigners == 1)
                    {
                        //
                        // time stamper cert available
                        //
                        timestamperCert = GetCertFromChain(provSigner.pasCounterSigners);
                    }

                    return true;
                }
            }

            return false;
        }

        [ArchitectureSensitive]
        private static DWORD GetLastWin32Error()
        {
            int error = Marshal.GetLastWin32Error();

            return SecuritySupport.GetDWORDFromInt(error);
        }
    }
}

#pragma warning restore 56523
