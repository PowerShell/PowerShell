// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Management.Automation.Internal;
using System.Security.Cryptography.X509Certificates;

using DWORD = System.UInt32;

namespace System.Management.Automation
{
    internal static class Win32Errors
    {
        internal const DWORD NO_ERROR = 0;
        internal const DWORD E_FAIL = 0x80004005;
        internal const DWORD TRUST_E_NOSIGNATURE = 0x800b0100;
        internal const DWORD TRUST_E_BAD_DIGEST = 0x80096010;
        internal const DWORD TRUST_E_PROVIDER_UNKNOWN = 0x800b0001;
        internal const DWORD TRUST_E_SUBJECT_FORM_UNKNOWN = 0x800B0003;
        internal const DWORD CERT_E_UNTRUSTEDROOT = 0x800b0109;
        internal const DWORD TRUST_E_EXPLICIT_DISTRUST = 0x800B0111;
        internal const DWORD CRYPT_E_BAD_MSG = 0x8009200d;
        internal const DWORD NTE_BAD_ALGID = 0x80090008;
    }

    /// <summary>
    /// Defines the valid status flags that a signature
    /// on a file may have.
    /// </summary>
    public enum SignatureStatus
    {
        /// <summary>
        /// The file has a valid signature.  This means only that
        /// the signature is syntactically valid.  It does not
        /// imply trust in any way.
        /// </summary>
        Valid,

        /// <summary>
        /// The file has an invalid signature.
        /// </summary>
        UnknownError,

        /// <summary>
        /// The file has no signature.
        /// </summary>
        NotSigned,

        /// <summary>
        /// The hash of the file does not match the hash stored
        /// along with the signature.
        /// </summary>
        HashMismatch,

        /// <summary>
        /// The certificate was signed by a publisher not trusted
        /// on the system.
        /// </summary>
        NotTrusted,

        /// <summary>
        /// The specified file format is not supported by the system
        /// for signing operations.  This usually means that the
        /// system does not know how to sign or verify the file
        /// type requested.
        /// </summary>
        NotSupportedFileFormat,

        /// <summary>
        /// The signature cannot be verified because it is incompatible
        /// with the current system.
        /// </summary>
        Incompatible
    }

    /// <summary>
    /// Defines the valid types of signatures.
    /// </summary>
    public enum SignatureType
    {
        /// <summary>
        /// The file is not signed.
        /// </summary>
        None = 0,

        /// <summary>
        /// The signature is an Authenticode signature embedded into the file itself.
        /// </summary>
        Authenticode = 1,

        /// <summary>
        /// The signature is a catalog signature.
        /// </summary>
        Catalog = 2
    }

    /// <summary>
    /// Represents a digital signature on a signed
    /// file.
    /// </summary>
    public sealed class Signature
    {
        private string _path;
        private SignatureStatus _status = SignatureStatus.UnknownError;
        private DWORD _win32Error;
        private X509Certificate2 _signerCert;
        private string _statusMessage = string.Empty;
        private X509Certificate2 _timeStamperCert;
        // private DateTime signedOn = new DateTime(0);

        // Three states:
        //   - True: we can rely on the catalog API to check catalog signature.
        //   - False: we cannot rely on the catalog API, either because it doesn't exist in the OS (win7, nano),
        //            or it's not working properly (OneCore SKUs or dev environment where powershell might
        //            be updated/refreshed).
        //   - Null: it's not determined yet whether catalog API can be relied on or not.
        internal static bool? CatalogApiAvailable = null;

        /// <summary>
        /// Gets the X509 certificate of the publisher that
        /// signed the file.
        /// </summary>
        public X509Certificate2 SignerCertificate
        {
            get
            {
                return _signerCert;
            }
        }

        /// <summary>
        /// Gets the X509 certificate of the authority that
        /// time-stamped the file.
        /// </summary>
        public X509Certificate2 TimeStamperCertificate
        {
            get
            {
                return _timeStamperCert;
            }
        }

        /// <summary>
        /// Gets the status of the signature on the file.
        /// </summary>
        public SignatureStatus Status
        {
            get
            {
                return _status;
            }
        }

        /// <summary>
        /// Gets the message corresponding to the status of the
        /// signature on the file.
        /// </summary>
        public string StatusMessage
        {
            get
            {
                return _statusMessage;
            }
        }

        /// <summary>
        /// Gets the path of the file to which this signature
        /// applies.
        /// </summary>
        public string Path
        {
            get
            {
                return _path;
            }
        }

        /// <summary>
        /// Returns the signature type of the signature.
        /// </summary>
        public SignatureType SignatureType { get; internal set; }

        /// <summary>
        /// True if the item is signed as part of an operating system release.
        /// </summary>
        public bool IsOSBinary { get; internal set; }

        /// <summary>
        /// Gets the Subject Alternative Name from the signer certificate.
        /// </summary>
        public string[] SubjectAlternativeName { get; private set; }

        /// <summary>
        /// Constructor for class Signature
        ///
        /// Call this to create a validated time-stamped signature object.
        /// </summary>
        /// <param name="filePath">This signature is found in this file.</param>
        /// <param name="error">Win32 error code.</param>
        /// <param name="signer">Cert of the signer.</param>
        /// <param name="timestamper">Cert of the time stamper.</param>
        /// <returns>Constructed object.</returns>
        internal Signature(string filePath,
                           DWORD error,
                           X509Certificate2 signer,
                           X509Certificate2 timestamper)
        {
            Utils.CheckArgForNullOrEmpty(filePath, "filePath");
            Utils.CheckArgForNull(signer, "signer");
            Utils.CheckArgForNull(timestamper, "timestamper");

            Init(filePath, signer, error, timestamper);
        }

        /// <summary>
        /// Constructor for class Signature
        ///
        /// Call this to create a validated signature object.
        /// </summary>
        /// <param name="filePath">This signature is found in this file.</param>
        /// <param name="signer">Cert of the signer.</param>
        /// <returns>Constructed object.</returns>
        internal Signature(string filePath,
                           X509Certificate2 signer)
        {
            Utils.CheckArgForNullOrEmpty(filePath, "filePath");
            Utils.CheckArgForNull(signer, "signer");

            Init(filePath, signer, 0, null);
        }

        /// <summary>
        /// Constructor for class Signature
        ///
        /// Call this ctor when creating an invalid signature object.
        /// </summary>
        /// <param name="filePath">This signature is found in this file.</param>
        /// <param name="error">Win32 error code.</param>
        /// <param name="signer">Cert of the signer.</param>
        /// <returns>Constructed object.</returns>
        internal Signature(string filePath,
                           DWORD error,
                           X509Certificate2 signer)
        {
            Utils.CheckArgForNullOrEmpty(filePath, "filePath");
            Utils.CheckArgForNull(signer, "signer");

            Init(filePath, signer, error, null);
        }

        /// <summary>
        /// Constructor for class Signature
        ///
        /// Call this ctor when creating an invalid signature object.
        /// </summary>
        /// <param name="filePath">This signature is found in this file.</param>
        /// <param name="error">Win32 error code.</param>
        /// <returns>Constructed object.</returns>
        internal Signature(string filePath, DWORD error)
        {
            Utils.CheckArgForNullOrEmpty(filePath, "filePath");

            Init(filePath, null, error, null);
        }

        private void Init(string filePath,
                          X509Certificate2 signer,
                          DWORD error,
                          X509Certificate2 timestamper)
        {
            _path = filePath;
            _win32Error = error;
            _signerCert = signer;
            _timeStamperCert = timestamper;
            SignatureType = SignatureType.None;

            SignatureStatus isc =
                GetSignatureStatusFromWin32Error(error);

            _status = isc;

            _statusMessage = GetSignatureStatusMessage(isc,
                                                      error,
                                                      filePath);

            // Extract Subject Alternative Name from the signer certificate
            if (signer != null)
            {
                SubjectAlternativeName = GetSubjectAlternativeName(signer);
            }
        }

        private static SignatureStatus GetSignatureStatusFromWin32Error(DWORD error)
        {
            SignatureStatus isc = SignatureStatus.UnknownError;

            switch (error)
            {
                case Win32Errors.NO_ERROR:
                    isc = SignatureStatus.Valid;
                    break;

                case Win32Errors.NTE_BAD_ALGID:
                    isc = SignatureStatus.Incompatible;
                    break;

                case Win32Errors.TRUST_E_NOSIGNATURE:
                    isc = SignatureStatus.NotSigned;
                    break;

                case Win32Errors.TRUST_E_BAD_DIGEST:
                case Win32Errors.CRYPT_E_BAD_MSG:
                    isc = SignatureStatus.HashMismatch;
                    break;

                case Win32Errors.TRUST_E_PROVIDER_UNKNOWN:
                    isc = SignatureStatus.NotSupportedFileFormat;
                    break;

                case Win32Errors.TRUST_E_EXPLICIT_DISTRUST:
                    isc = SignatureStatus.NotTrusted;
                    break;
            }

            return isc;
        }

        private static string GetSignatureStatusMessage(SignatureStatus status,
                                                 DWORD error,
                                                 string filePath)
        {
            string message = null;
            string resourceString = null;
            string arg = null;

            switch (status)
            {
                case SignatureStatus.Valid:
                    resourceString = MshSignature.MshSignature_Valid;
                    break;

                case SignatureStatus.UnknownError:
                    int intError = SecuritySupport.GetIntFromDWORD(error);
                    Win32Exception e = new Win32Exception(intError);
                    message = e.Message;
                    break;

                case SignatureStatus.Incompatible:
                    if (error == Win32Errors.NTE_BAD_ALGID)
                    {
                        resourceString = MshSignature.MshSignature_Incompatible_HashAlgorithm;
                    }
                    else
                    {
                        resourceString = MshSignature.MshSignature_Incompatible;
                    }

                    arg = filePath;
                    break;

                case SignatureStatus.NotSigned:
                    resourceString = MshSignature.MshSignature_NotSigned;
                    arg = filePath;
                    break;

                case SignatureStatus.HashMismatch:
                    resourceString = MshSignature.MshSignature_HashMismatch;
                    arg = filePath;
                    break;

                case SignatureStatus.NotTrusted:
                    resourceString = MshSignature.MshSignature_NotTrusted;
                    arg = filePath;
                    break;

                case SignatureStatus.NotSupportedFileFormat:
                    resourceString = MshSignature.MshSignature_NotSupportedFileFormat;
                    arg = System.IO.Path.GetExtension(filePath);

                    if (string.IsNullOrEmpty(arg))
                    {
                        resourceString = MshSignature.MshSignature_NotSupportedFileFormat_NoExtension;
                        arg = null;
                    }

                    break;
            }

            if (message == null)
            {
                if (arg == null)
                {
                    message = resourceString;
                }
                else
                {
                    message = StringUtil.Format(resourceString, arg);
                }
            }

            return message;
        }

        /// <summary>
        /// Extracts the Subject Alternative Name from the certificate.
        /// </summary>
        /// <param name="certificate">The certificate to extract SAN from.</param>
        /// <returns>Array of SAN entries or null if not found.</returns>
        private static string[] GetSubjectAlternativeName(X509Certificate2 certificate)
        {
            if (certificate == null)
            {
                return null;
            }

            foreach (X509Extension extension in certificate.Extensions)
            {
                if (extension.Oid != null && extension.Oid.Value == CertificateFilterInfo.SubjectAlternativeNameOid)
                {
                    string formatted = extension.Format(multiLine: true);
                    if (string.IsNullOrEmpty(formatted))
                    {
                        return null;
                    }

                    return formatted.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
                }
            }

            return null;
        }
    }
}
