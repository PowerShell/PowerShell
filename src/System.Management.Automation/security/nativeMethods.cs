// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691
#pragma warning disable 56523

using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Management.Automation.Internal;
using DWORD = System.UInt32;
using BOOL = System.UInt32;

namespace System.Management.Automation.Security
{
    // Crypto API native constants
    internal static partial class NativeConstants
    {
        internal const int CRYPT_OID_INFO_OID_KEY = 1;
        internal const int CRYPT_OID_INFO_NAME_KEY = 2;
        internal const int CRYPT_OID_INFO_CNG_ALGID_KEY = 5;
    }

    // Safer native constants
    internal partial class NativeConstants
    {
        /// <Summary>
        /// SAFER_TOKEN_NULL_IF_EQUAL -> 0x00000001.
        /// </Summary>
        public const int SAFER_TOKEN_NULL_IF_EQUAL = 1;

        /// <Summary>
        /// SAFER_TOKEN_COMPARE_ONLY -> 0x00000002.
        /// </Summary>
        public const int SAFER_TOKEN_COMPARE_ONLY = 2;

        /// <Summary>
        /// SAFER_TOKEN_MAKE_INERT -> 0x00000004.
        /// </Summary>
        public const int SAFER_TOKEN_MAKE_INERT = 4;

        /// <Summary>
        /// SAFER_CRITERIA_IMAGEPATH -> 0x00001.
        /// </Summary>
        public const int SAFER_CRITERIA_IMAGEPATH = 1;

        /// <Summary>
        /// SAFER_CRITERIA_NOSIGNEDHASH -> 0x00002.
        /// </Summary>
        public const int SAFER_CRITERIA_NOSIGNEDHASH = 2;

        /// <Summary>
        /// SAFER_CRITERIA_IMAGEHASH -> 0x00004.
        /// </Summary>
        public const int SAFER_CRITERIA_IMAGEHASH = 4;

        /// <Summary>
        /// SAFER_CRITERIA_AUTHENTICODE -> 0x00008.
        /// </Summary>
        public const int SAFER_CRITERIA_AUTHENTICODE = 8;

        /// <Summary>
        /// SAFER_CRITERIA_URLZONE -> 0x00010.
        /// </Summary>
        public const int SAFER_CRITERIA_URLZONE = 16;

        /// <Summary>
        /// SAFER_CRITERIA_IMAGEPATH_NT -> 0x01000.
        /// </Summary>
        public const int SAFER_CRITERIA_IMAGEPATH_NT = 4096;

        /// <Summary>
        /// WTD_UI_NONE -> 0x00002.
        /// </Summary>
        public const int WTD_UI_NONE = 2;

        /// <Summary>
        /// S_OK -> ((HRESULT)0L)
        /// </Summary>
        public const int S_OK = 0;

        /// <Summary>
        /// S_FALSE -> ((HRESULT)1L)
        /// </Summary>
        public const int S_FALSE = 1;

        /// <Summary>
        /// ERROR_MORE_DATA -> 234L.
        /// </Summary>
        public const int ERROR_MORE_DATA = 234;

        /// <Summary>
        /// ERROR_ACCESS_DISABLED_BY_POLICY -> 1260L.
        /// </Summary>
        public const int ERROR_ACCESS_DISABLED_BY_POLICY = 1260;

        /// <Summary>
        /// ERROR_ACCESS_DISABLED_NO_SAFER_UI_BY_POLICY -> 786L.
        /// </Summary>
        public const int ERROR_ACCESS_DISABLED_NO_SAFER_UI_BY_POLICY = 786;

        /// <Summary>
        /// SAFER_MAX_HASH_SIZE -> 64.
        /// </Summary>
        public const int SAFER_MAX_HASH_SIZE = 64;

        /// <Summary>
        /// SRP_POLICY_SCRIPT -> L"SCRIPT"
        /// </Summary>
        public const string SRP_POLICY_SCRIPT = "SCRIPT";

        /// <Summary>
        /// SIGNATURE_DISPLAYNAME_LENGTH -> MAX_PATH.
        /// </Summary>
        internal const int SIGNATURE_DISPLAYNAME_LENGTH = NativeConstants.MAX_PATH;

        /// <Summary>
        /// SIGNATURE_PUBLISHER_LENGTH -> 128.
        /// </Summary>
        internal const int SIGNATURE_PUBLISHER_LENGTH = 128;

        /// <Summary>
        /// SIGNATURE_HASH_LENGTH -> 64.
        /// </Summary>
        internal const int SIGNATURE_HASH_LENGTH = 64;

        /// <Summary>
        /// MAX_PATH -> 260.
        /// </Summary>
        internal const int MAX_PATH = 260;

        /// <Summary>
        /// This function is not supported on this system.
        /// </Summary>
        internal const int FUNCTION_NOT_SUPPORTED = 120;
    }

    /// <summary>
    /// Pinvoke methods from crypt32.dll.
    /// </summary>
    internal static partial class NativeMethods
    {
        // -------------------------------------------------------------------
        // crypt32.dll stuff
        //

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern
        bool CertEnumSystemStore(CertStoreFlags Flags,
                                 IntPtr notUsed1,
                                 IntPtr notUsed2,
                                 CertEnumSystemStoreCallBackProto fn);

        /// <summary>
        /// Signature of call back function used by CertEnumSystemStore.
        /// </summary>
        internal delegate
        bool CertEnumSystemStoreCallBackProto([MarshalAs(UnmanagedType.LPWStr)]
                                               string storeName,
                                               DWORD dwFlagsNotUsed,
                                               IntPtr notUsed1,
                                               IntPtr notUsed2,
                                               IntPtr notUsed3);

        /// <summary>
        /// Signature of cert enumeration function.
        /// </summary>
        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern
        IntPtr CertEnumCertificatesInStore(IntPtr storeHandle,
                                            IntPtr certContext);

        /// <summary>
        /// Signature of cert find function.
        /// </summary>
        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern
        IntPtr CertFindCertificateInStore(
            IntPtr hCertStore,
            Security.NativeMethods.CertOpenStoreEncodingType dwEncodingType,
            DWORD dwFindFlags,                  // 0
            Security.NativeMethods.CertFindType dwFindType,
            [MarshalAs(UnmanagedType.LPWStr)] string pvFindPara,
            IntPtr notUsed1);                   // pPrevCertContext

        [Flags]
        internal enum CertFindType
        {                                                       // pvFindPara:
            CERT_COMPARE_ANY = 0 << 16,         // null
            CERT_FIND_ISSUER_STR = (8 << 16) | 4,   // substring
            CERT_FIND_SUBJECT_STR = (8 << 16) | 7,   // substring
            CERT_FIND_CROSS_CERT_DIST_POINTS = 17 << 16,        // null
            CERT_FIND_SUBJECT_INFO_ACCESS = 19 << 16,        // null
            CERT_FIND_HASH_STR = 20 << 16,        // thumbprint
        }

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern
        bool CertCloseStore(IntPtr hCertStore, int dwFlags);

        [Flags]
        internal enum CertStoreFlags
        {
            CERT_SYSTEM_STORE_CURRENT_USER = 1 << 16,
            CERT_SYSTEM_STORE_LOCAL_MACHINE = 2 << 16,
            CERT_SYSTEM_STORE_CURRENT_SERVICE = 4 << 16,
            CERT_SYSTEM_STORE_SERVICES = 5 << 16,
            CERT_SYSTEM_STORE_USERS = 6 << 16,
            CERT_SYSTEM_STORE_CURRENT_USER_GROUP_POLICY = 7 << 16,
            CERT_SYSTEM_STORE_LOCAL_MACHINE_GROUP_POLICY = 8 << 16,
            CERT_SYSTEM_STORE_LOCAL_MACHINE_ENTERPRISE = 9 << 16,
        }

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern
        bool CertGetEnhancedKeyUsage(IntPtr pCertContext, // PCCERT_CONTEXT
                                      DWORD dwFlags,
                                      IntPtr pUsage,       // PCERT_ENHKEY_USAGE
                                      out int pcbUsage);  // DWORD*

        [DllImport("Crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern
        IntPtr CertOpenStore(CertOpenStoreProvider storeProvider,
                              CertOpenStoreEncodingType dwEncodingType,
                              IntPtr notUsed1,          // hCryptProv
                              CertOpenStoreFlags dwFlags,
                              [MarshalAs(UnmanagedType.LPWStr)]
                              string storeName);

        [Flags]
        internal enum CertOpenStoreFlags
        {
            CERT_STORE_NO_CRYPT_RELEASE_FLAG = 0x00000001,
            CERT_STORE_SET_LOCALIZED_NAME_FLAG = 0x00000002,
            CERT_STORE_DEFER_CLOSE_UNTIL_LAST_FREE_FLAG = 0x00000004,
            CERT_STORE_DELETE_FLAG = 0x00000010,
            CERT_STORE_UNSAFE_PHYSICAL_FLAG = 0x00000020,
            CERT_STORE_SHARE_STORE_FLAG = 0x00000040,
            CERT_STORE_SHARE_CONTEXT_FLAG = 0x00000080,
            CERT_STORE_MANIFOLD_FLAG = 0x00000100,
            CERT_STORE_ENUM_ARCHIVED_FLAG = 0x00000200,
            CERT_STORE_UPDATE_KEYID_FLAG = 0x00000400,
            CERT_STORE_BACKUP_RESTORE_FLAG = 0x00000800,
            CERT_STORE_READONLY_FLAG = 0x00008000,
            CERT_STORE_OPEN_EXISTING_FLAG = 0x00004000,
            CERT_STORE_CREATE_NEW_FLAG = 0x00002000,
            CERT_STORE_MAXIMUM_ALLOWED_FLAG = 0x00001000,

            CERT_SYSTEM_STORE_CURRENT_USER = 1 << 16,
            CERT_SYSTEM_STORE_LOCAL_MACHINE = 2 << 16,
            CERT_SYSTEM_STORE_CURRENT_SERVICE = 4 << 16,
            CERT_SYSTEM_STORE_SERVICES = 5 << 16,
            CERT_SYSTEM_STORE_USERS = 6 << 16,
            CERT_SYSTEM_STORE_CURRENT_USER_GROUP_POLICY = 7 << 16,
            CERT_SYSTEM_STORE_LOCAL_MACHINE_GROUP_POLICY = 8 << 16,
            CERT_SYSTEM_STORE_LOCAL_MACHINE_ENTERPRISE = 9 << 16,
        }

        [Flags]
        internal enum CertOpenStoreProvider
        {
            CERT_STORE_PROV_MEMORY = 2,
            CERT_STORE_PROV_SYSTEM = 10,
            CERT_STORE_PROV_SYSTEM_REGISTRY = 13,
        }

        [Flags]
        internal enum CertOpenStoreEncodingType
        {
            X509_ASN_ENCODING = 0x00000001,
        }

        [DllImport("Crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern
        bool CertControlStore(
                        IntPtr hCertStore,
                        DWORD dwFlags,
                        CertControlStoreType dwCtrlType,
                        IntPtr pvCtrlPara);

        [Flags]
        internal enum CertControlStoreType : uint
        {
            CERT_STORE_CTRL_RESYNC = 1,
            CERT_STORE_CTRL_COMMIT = 3,
            CERT_STORE_CTRL_AUTO_RESYNC = 4,
        }

        [Flags]
        internal enum AddCertificateContext : uint
        {
            CERT_STORE_ADD_NEW = 1,
            CERT_STORE_ADD_USE_EXISTING = 2,
            CERT_STORE_ADD_REPLACE_EXISTING = 3,
            CERT_STORE_ADD_ALWAYS = 4,
            CERT_STORE_ADD_REPLACE_EXISTING_INHERIT_PROPERTIES = 5,
            CERT_STORE_ADD_NEWER = 6,
            CERT_STORE_ADD_NEWER_INHERIT_PROPERTIES = 7
        }

        [Flags]
        internal enum CertPropertyId
        {
            CERT_KEY_PROV_HANDLE_PROP_ID = 1,
            CERT_KEY_PROV_INFO_PROP_ID = 2,   // CRYPT_KEY_PROV_INFO
            CERT_SHA1_HASH_PROP_ID = 3,
            CERT_MD5_HASH_PROP_ID = 4,
            CERT_SEND_AS_TRUSTED_ISSUER_PROP_ID = 102,
        }

        [Flags]
        internal enum NCryptDeletKeyFlag
        {
            NCRYPT_MACHINE_KEY_FLAG = 0x00000020,  // same as CAPI CRYPT_MACHINE_KEYSET
            NCRYPT_SILENT_FLAG = 0x00000040,  // same as CAPI CRYPT_SILENT
        }

        [Flags]
        internal enum ProviderFlagsEnum : uint
        {
            CRYPT_VERIFYCONTEXT = 0xF0000000,
            CRYPT_NEWKEYSET = 0x00000008,
            CRYPT_DELETEKEYSET = 0x00000010,
            CRYPT_MACHINE_KEYSET = 0x00000020,
            CRYPT_SILENT = 0x00000040,
        }

        internal enum ProviderParam : int
        {
            PP_CLIENT_HWND = 1,
        }

        internal enum PROV : uint
        {
            /// <summary>
            /// The PROV_RSA_FULL type.
            /// </summary>
            RSA_FULL = 1,

            /// <summary>
            /// The PROV_RSA_SIG type.
            /// </summary>
            RSA_SIG = 2,

            /// <summary>
            /// The PROV_RSA_DSS type.
            /// </summary>
            DSS = 3,

            /// <summary>
            /// The PROV_FORTEZZA type.
            /// </summary>
            FORTEZZA = 4,

            /// <summary>
            /// The PROV_MS_EXCHANGE type.
            /// </summary>
            MS_EXCHANGE = 5,

            /// <summary>
            /// The PROV_SSL type.
            /// </summary>
            SSL = 6,

            /// <summary>
            /// The PROV_RSA_SCHANNEL type. SSL certificates are generated with these providers.
            /// </summary>
            RSA_SCHANNEL = 12,

            /// <summary>
            /// The PROV_DSS_DH type.
            /// </summary>
            DSS_DH = 13,

            /// <summary>
            /// The PROV_EC_ECDSA type.
            /// </summary>
            EC_ECDSA_SIG = 14,

            /// <summary>
            /// The PROV_EC_ECNRA_SIG type.
            /// </summary>
            EC_ECNRA_SIG = 15,

            /// <summary>
            /// The PROV_EC_ECDSA_FULL type.
            /// </summary>
            EC_ECDSA_FULL = 16,

            /// <summary>
            /// The PROV_EC_ECNRA_FULL type.
            /// </summary>
            EC_ECNRA_FULL = 17,

            /// <summary>
            /// The PROV_DH_SCHANNEL type.
            /// </summary>
            DH_SCHANNEL = 18,

            /// <summary>
            /// The PROV_SPYRUS_LYNKS type.
            /// </summary>
            SPYRUS_LYNKS = 20,

            /// <summary>
            /// The PROV_RNG type.
            /// </summary>
            RNG = 21,

            /// <summary>
            /// The PROV_INTEL_SEC type.
            /// </summary>
            INTEL_SEC = 22
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CRYPT_KEY_PROV_INFO
        {
            /// <summary>
            /// String naming a key container within a particular CSP.
            /// </summary>
            public string pwszContainerName;

            /// <summary>
            /// String that names a CSP.
            /// </summary>
            public string pwszProvName;

            /// <summary>
            /// CSP type.
            /// </summary>
            public PROV dwProvType;

            /// <summary>
            /// Flags value indicating whether a key container is to be created or destroyed, and
            /// whether an application is allowed access to a key container.
            /// </summary>
            public uint dwFlags;

            /// <summary>
            /// Number of elements in the rgProvParam array.
            /// </summary>
            public uint cProvParam;

            /// <summary>
            /// Array of pointers to CRYPT_KEY_PROV_PARAM structures.
            /// </summary>
            public IntPtr rgProvParam;

            /// <summary>
            /// The specification of the private key to retrieve. AT_KEYEXCHANGE and AT_SIGNATURE
            /// are defined for the default provider.
            /// </summary>
            public uint dwKeySpec;
        }

        internal const string NCRYPT_WINDOW_HANDLE_PROPERTY = "HWND Handle";

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern
        bool CertDeleteCertificateFromStore(IntPtr pCertContext);

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern
        IntPtr CertDuplicateCertificateContext(IntPtr pCertContext);

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern
        bool CertAddCertificateContextToStore(IntPtr hCertStore,
                                              IntPtr pCertContext,
                                              DWORD dwAddDisposition,
                                              ref IntPtr ppStoreContext);

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern
        bool CertFreeCertificateContext(IntPtr certContext);

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern
        bool CertGetCertificateContextProperty(IntPtr pCertContext,
                                               CertPropertyId dwPropId,
                                               IntPtr pvData,
                                               ref int pcbData);

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern
        bool CertSetCertificateContextProperty(IntPtr pCertContext,
                                               CertPropertyId dwPropId,
                                               DWORD dwFlags,
                                               IntPtr pvData);

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern
        IntPtr CryptFindLocalizedName(string pwszCryptName);

        [DllImport(PinvokeDllNames.CryptAcquireContextDllName, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern
        bool CryptAcquireContext(ref IntPtr hProv,
                                 string strContainerName,
                                 string strProviderName,
                                 int nProviderType,
                                 uint uiProviderFlags);

        [DllImport(PinvokeDllNames.CryptReleaseContextDllName, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern
        bool CryptReleaseContext(IntPtr hProv, int dwFlags);

        [DllImport(PinvokeDllNames.CryptSetProvParamDllName, SetLastError = true)]
        internal static extern unsafe
        bool CryptSetProvParam(IntPtr hProv, ProviderParam dwParam, void* pbData, int dwFlags);

        [DllImport("ncrypt.dll", CharSet = CharSet.Unicode)]
        internal static extern
        int NCryptOpenStorageProvider(ref IntPtr hProv,
                                      string strProviderName,
                                      uint dwFlags);

        [DllImport("ncrypt.dll", CharSet = CharSet.Unicode)]
        internal static extern
        int NCryptOpenKey(IntPtr hProv,
                          ref IntPtr hKey,
                          string strKeyName,
                          uint dwLegacySpec,
                          uint dwFlags);

        [DllImport("ncrypt.dll", CharSet = CharSet.Unicode)]
        internal static extern unsafe
        int NCryptSetProperty(IntPtr hProv, string pszProperty, void* pbInput, int cbInput, int dwFlags);

        [DllImport("ncrypt.dll", CharSet = CharSet.Unicode)]
        internal static extern
        int NCryptDeleteKey(IntPtr hKey,
                            uint dwFlags);

        [DllImport("ncrypt.dll", CharSet = CharSet.Unicode)]
        internal static extern
        int NCryptFreeObject(IntPtr hObject);

        // -----------------------------------------------------------------
        // cryptUI.dll stuff
        //

        //
        // CryptUIWizDigitalSign() function and associated structures/enums
        //

        [DllImport("cryptUI.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern
        bool CryptUIWizDigitalSign(DWORD dwFlags,
                                   IntPtr hwndParentNotUsed,
                                   IntPtr pwszWizardTitleNotUsed,
                                   IntPtr pDigitalSignInfo,
                                   IntPtr ppSignContextNotUsed);

        [Flags]
        internal enum CryptUIFlags
        {
            CRYPTUI_WIZ_NO_UI = 0x0001
            // other flags not used
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CRYPTUI_WIZ_DIGITAL_SIGN_INFO
        {
            internal DWORD dwSize;
            internal DWORD dwSubjectChoice;

            [MarshalAs(UnmanagedType.LPWStr)]
            internal string pwszFileName;

            internal DWORD dwSigningCertChoice;
            internal IntPtr pSigningCertContext; // PCCERT_CONTEXT

            [MarshalAs(UnmanagedType.LPWStr)]
            internal string pwszTimestampURL;

            internal DWORD dwAdditionalCertChoice;
            internal IntPtr pSignExtInfo; // PCCRYPTUI_WIZ_DIGITAL_SIGN_EXTENDED_INFO
        }

        [Flags]
        internal enum SignInfoSubjectChoice
        {
            CRYPTUI_WIZ_DIGITAL_SIGN_SUBJECT_FILE = 0x01
            // CRYPTUI_WIZ_DIGITAL_SIGN_SUBJECT_BLOB = 0x02 NotUsed
        }

        [Flags]
        internal enum SignInfoCertChoice
        {
            CRYPTUI_WIZ_DIGITAL_SIGN_CERT = 0x01
            // CRYPTUI_WIZ_DIGITAL_SIGN_STORE = 0x02, NotUsed
            // CRYPTUI_WIZ_DIGITAL_SIGN_PVK = 0x03, NotUsed
        }

        [Flags]
        internal enum SignInfoAdditionalCertChoice
        {
            CRYPTUI_WIZ_DIGITAL_SIGN_ADD_CHAIN = 1,
            CRYPTUI_WIZ_DIGITAL_SIGN_ADD_CHAIN_NO_ROOT = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CRYPTUI_WIZ_DIGITAL_SIGN_EXTENDED_INFO
        {
            internal DWORD dwSize;
            internal DWORD dwAttrFlagsNotUsed;

            [MarshalAs(UnmanagedType.LPWStr)]
            internal string pwszDescription;

            [MarshalAs(UnmanagedType.LPWStr)]
            internal string pwszMoreInfoLocation;

            [MarshalAs(UnmanagedType.LPStr)]
            internal string pszHashAlg;

            internal IntPtr pwszSigningCertDisplayStringNotUsed; // LPCWSTR
            internal IntPtr hAdditionalCertStoreNotUsed; // HCERTSTORE
            internal IntPtr psAuthenticatedNotUsed;      // PCRYPT_ATTRIBUTES
            internal IntPtr psUnauthenticatedNotUsed;    // PCRYPT_ATTRIBUTES
        }

        [ArchitectureSensitive]
        internal static CRYPTUI_WIZ_DIGITAL_SIGN_EXTENDED_INFO
            InitSignInfoExtendedStruct(string description,
                                       string moreInfoUrl,
                                       string hashAlgorithm)
        {
            CRYPTUI_WIZ_DIGITAL_SIGN_EXTENDED_INFO siex =
                new CRYPTUI_WIZ_DIGITAL_SIGN_EXTENDED_INFO();

            siex.dwSize = (DWORD)Marshal.SizeOf(siex);
            siex.dwAttrFlagsNotUsed = 0;
            siex.pwszDescription = description;
            siex.pwszMoreInfoLocation = moreInfoUrl;
            siex.pszHashAlg = null;
            siex.pwszSigningCertDisplayStringNotUsed = IntPtr.Zero;
            siex.hAdditionalCertStoreNotUsed = IntPtr.Zero;
            siex.psAuthenticatedNotUsed = IntPtr.Zero;
            siex.psUnauthenticatedNotUsed = IntPtr.Zero;

            if (hashAlgorithm != null)
            {
                siex.pszHashAlg = hashAlgorithm;
            }

            return siex;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CRYPT_OID_INFO
        {
            /// DWORD->unsigned int
            public uint cbSize;

            /// LPCSTR->CHAR*
            [MarshalAsAttribute(UnmanagedType.LPStr)]
            public string pszOID;

            /// LPCWSTR->WCHAR*
            [MarshalAsAttribute(UnmanagedType.LPWStr)]
            public string pwszName;

            /// DWORD->unsigned int
            public uint dwGroupId;

            /// Anonymous_a3ae7823_8a1d_432c_bc07_a72b6fc6c7d8
            public Anonymous_a3ae7823_8a1d_432c_bc07_a72b6fc6c7d8 Union1;

            /// CRYPT_DATA_BLOB->_CRYPTOAPI_BLOB
            public CRYPT_ATTR_BLOB ExtraInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct Anonymous_a3ae7823_8a1d_432c_bc07_a72b6fc6c7d8
        {
            /// DWORD->unsigned int
            [FieldOffset(0)]
            public uint dwValue;

            /// ALG_ID->unsigned int
            [FieldOffset(0)]
            public uint Algid;

            /// DWORD->unsigned int
            [FieldOffset(0)]
            public uint dwLength;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CRYPT_ATTR_BLOB
        {
            /// DWORD->unsigned int
            public uint cbData;

            /// BYTE*
            public System.IntPtr pbData;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CRYPT_DATA_BLOB
        {
            /// DWORD->unsigned int
            public uint cbData;

            /// BYTE*
            public System.IntPtr pbData;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CERT_CONTEXT
        {
            public int dwCertEncodingType;
            public IntPtr pbCertEncoded;
            public int cbCertEncoded;
            public IntPtr pCertInfo;
            public IntPtr hCertStore;
        }

        // Return the OID info for a given algorithm
        [DllImport("crypt32.dll", EntryPoint = "CryptFindOIDInfo")]
        internal static extern IntPtr CryptFindOIDInfo(
            uint dwKeyType,
            System.IntPtr pvKey,
            uint dwGroupId);

        [ArchitectureSensitive]
        internal static DWORD GetCertChoiceFromSigningOption(
            SigningOption option)
        {
            DWORD cc = 0;

            switch (option)
            {
                case SigningOption.AddOnlyCertificate:
                    cc = 0;
                    break;

                case SigningOption.AddFullCertificateChain:
                    cc = (DWORD)SignInfoAdditionalCertChoice.CRYPTUI_WIZ_DIGITAL_SIGN_ADD_CHAIN;
                    break;

                case SigningOption.AddFullCertificateChainExceptRoot:
                    cc = (DWORD)SignInfoAdditionalCertChoice.CRYPTUI_WIZ_DIGITAL_SIGN_ADD_CHAIN_NO_ROOT;
                    break;

                default:
                    cc = (DWORD)SignInfoAdditionalCertChoice.CRYPTUI_WIZ_DIGITAL_SIGN_ADD_CHAIN_NO_ROOT;
                    break;
            }

            return cc;
        }

        [ArchitectureSensitive]
        internal static CRYPTUI_WIZ_DIGITAL_SIGN_INFO
            InitSignInfoStruct(string fileName,
                               X509Certificate2 signingCert,
                               string timeStampServerUrl,
                               string hashAlgorithm,
                               SigningOption option)
        {
            CRYPTUI_WIZ_DIGITAL_SIGN_INFO si = new CRYPTUI_WIZ_DIGITAL_SIGN_INFO();

            si.dwSize = (DWORD)Marshal.SizeOf(si);
            si.dwSubjectChoice = (DWORD)SignInfoSubjectChoice.CRYPTUI_WIZ_DIGITAL_SIGN_SUBJECT_FILE;
            si.pwszFileName = fileName;
            si.dwSigningCertChoice = (DWORD)SignInfoCertChoice.CRYPTUI_WIZ_DIGITAL_SIGN_CERT;
            si.pSigningCertContext = signingCert.Handle;
            si.pwszTimestampURL = timeStampServerUrl;
            si.dwAdditionalCertChoice = GetCertChoiceFromSigningOption(option);

            CRYPTUI_WIZ_DIGITAL_SIGN_EXTENDED_INFO siex =
                InitSignInfoExtendedStruct(string.Empty, string.Empty, hashAlgorithm);
            IntPtr pSiexBuffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(siex));
            Marshal.StructureToPtr(siex, pSiexBuffer, false);
            si.pSignExtInfo = pSiexBuffer;

            return si;
        }

        // -----------------------------------------------------------------
        // wintrust.dll stuff
        //

        //
        // WinVerifyTrust() function and associated structures/enums
        //

        [DllImport("wintrust.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern
        DWORD WinVerifyTrust(
            IntPtr hWndNotUsed, // HWND
            IntPtr pgActionID, // GUID*
            IntPtr pWinTrustData // WINTRUST_DATA*
        );

        [StructLayout(LayoutKind.Sequential)]
        internal struct WINTRUST_FILE_INFO
        {
            internal DWORD cbStruct;               // = sizeof(WINTRUST_FILE_INFO)

            [MarshalAs(UnmanagedType.LPWStr)]
            internal string pcwszFilePath;         // LPCWSTR

            internal IntPtr hFileNotUsed;          // optional, HANDLE to pcwszFilePath
            internal IntPtr pgKnownSubjectNotUsed; // optional: GUID* : fill if the
                                                   // subject type is known
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct WINTRUST_BLOB_INFO
        {
            /// DWORD->unsigned int
            internal uint cbStruct;

            /// GUID->_GUID
            internal Guid gSubject;

            /// LPCWSTR->WCHAR*
            [MarshalAsAttribute(UnmanagedType.LPWStr)]
            internal string pcwszDisplayName;

            /// DWORD->unsigned int
            internal uint cbMemObject;

            /// BYTE*
            internal System.IntPtr pbMemObject;

            /// DWORD->unsigned int
            internal uint cbMemSignedMsg;

            /// BYTE*
            internal System.IntPtr pbMemSignedMsg;
        }

        [ArchitectureSensitive]
        internal static WINTRUST_FILE_INFO InitWintrustFileInfoStruct(string fileName)
        {
            WINTRUST_FILE_INFO fi = new WINTRUST_FILE_INFO();

            fi.cbStruct = (DWORD)Marshal.SizeOf(fi);
            fi.pcwszFilePath = fileName;
            fi.hFileNotUsed = IntPtr.Zero;
            fi.pgKnownSubjectNotUsed = IntPtr.Zero;

            return fi;
        }

        [ArchitectureSensitive]
        internal static WINTRUST_BLOB_INFO InitWintrustBlobInfoStruct(string fileName, string content)
        {
            WINTRUST_BLOB_INFO bi = new WINTRUST_BLOB_INFO();
            byte[] contentBytes = System.Text.Encoding.Unicode.GetBytes(content);

            // The GUID of the PowerShell SIP
            bi.gSubject = new Guid(0x603bcc1f, 0x4b59, 0x4e08, new byte[] { 0xb7, 0x24, 0xd2, 0xc6, 0x29, 0x7e, 0xf3, 0x51 });
            bi.cbStruct = (DWORD)Marshal.SizeOf(bi);
            bi.pcwszDisplayName = fileName;
            bi.cbMemObject = (uint)contentBytes.Length;
            bi.pbMemObject = Marshal.AllocCoTaskMem(contentBytes.Length);
            Marshal.Copy(contentBytes, 0, bi.pbMemObject, contentBytes.Length);

            return bi;
        }

        [Flags]
        internal enum WintrustUIChoice
        {
            WTD_UI_ALL = 1,
            WTD_UI_NONE = 2,
            WTD_UI_NOBAD = 3,
            WTD_UI_NOGOOD = 4
        }

        [Flags]
        internal enum WintrustUnionChoice
        {
            WTD_CHOICE_FILE = 1,
            // WTD_CHOICE_CATALOG = 2,
            WTD_CHOICE_BLOB = 3,
            // WTD_CHOICE_SIGNER = 4,
            // WTD_CHOICE_CERT = 5,
        }

        [Flags]
        internal enum WintrustProviderFlags
        {
            WTD_PROV_FLAGS_MASK = 0x0000FFFF,
            WTD_USE_IE4_TRUST_FLAG = 0x00000001,
            WTD_NO_IE4_CHAIN_FLAG = 0x00000002,
            WTD_NO_POLICY_USAGE_FLAG = 0x00000004,
            WTD_REVOCATION_CHECK_NONE = 0x00000010,
            WTD_REVOCATION_CHECK_END_CERT = 0x00000020,
            WTD_REVOCATION_CHECK_CHAIN = 0x00000040,
            WTD_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT = 0x00000080,
            WTD_SAFER_FLAG = 0x00000100,
            WTD_HASH_ONLY_FLAG = 0x00000200,
            WTD_USE_DEFAULT_OSVER_CHECK = 0x00000400,
            WTD_LIFETIME_SIGNING_FLAG = 0x00000800,
            WTD_CACHE_ONLY_URL_RETRIEVAL = 0x00001000
        }

        [Flags]
        internal enum WintrustAction
        {
            WTD_STATEACTION_IGNORE = 0x00000000,
            WTD_STATEACTION_VERIFY = 0x00000001,
            WTD_STATEACTION_CLOSE = 0x00000002,
            WTD_STATEACTION_AUTO_CACHE = 0x00000003,
            WTD_STATEACTION_AUTO_CACHE_FLUSH = 0x00000004
        }

        [StructLayoutAttribute(LayoutKind.Explicit)]
        internal struct WinTrust_Choice
        {
            /// WINTRUST_FILE_INFO_*
            [FieldOffsetAttribute(0)]
            internal System.IntPtr pFile;

            /// WINTRUST_CATALOG_INFO_*
            [FieldOffsetAttribute(0)]
            internal System.IntPtr pCatalog;

            /// WINTRUST_BLOB_INFO_*
            [FieldOffsetAttribute(0)]
            internal System.IntPtr pBlob;

            /// WINTRUST_SGNR_INFO_*
            [FieldOffsetAttribute(0)]
            internal System.IntPtr pSgnr;

            /// WINTRUST_CERT_INFO_*
            [FieldOffsetAttribute(0)]
            internal System.IntPtr pCert;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct WINTRUST_DATA
        {
            /// DWORD->unsigned int
            internal uint cbStruct;

            /// LPVOID->void*
            internal System.IntPtr pPolicyCallbackData;

            /// LPVOID->void*
            internal System.IntPtr pSIPClientData;

            /// DWORD->unsigned int
            internal uint dwUIChoice;

            /// DWORD->unsigned int
            internal uint fdwRevocationChecks;

            /// DWORD->unsigned int
            internal uint dwUnionChoice;

            /// WinTrust_Choice struct
            internal WinTrust_Choice Choice;

            /// DWORD->unsigned int
            internal uint dwStateAction;

            /// HANDLE->void*
            internal System.IntPtr hWVTStateData;

            /// WCHAR*
            [MarshalAsAttribute(UnmanagedType.LPWStr)]
            internal string pwszURLReference;

            /// DWORD->unsigned int
            internal uint dwProvFlags;

            /// DWORD->unsigned int
            internal uint dwUIContext;
        }

        [ArchitectureSensitive]
        internal static WINTRUST_DATA InitWintrustDataStructFromFile(WINTRUST_FILE_INFO wfi)
        {
            WINTRUST_DATA wtd = new WINTRUST_DATA();

            wtd.cbStruct = (DWORD)Marshal.SizeOf(wtd);
            wtd.pPolicyCallbackData = IntPtr.Zero;
            wtd.pSIPClientData = IntPtr.Zero;
            wtd.dwUIChoice = (DWORD)WintrustUIChoice.WTD_UI_NONE;
            wtd.fdwRevocationChecks = 0;
            wtd.dwUnionChoice = (DWORD)WintrustUnionChoice.WTD_CHOICE_FILE;

            IntPtr pFileBuffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(wfi));
            Marshal.StructureToPtr(wfi, pFileBuffer, false);
            wtd.Choice.pFile = pFileBuffer;

            wtd.dwStateAction = (DWORD)WintrustAction.WTD_STATEACTION_VERIFY;
            wtd.hWVTStateData = IntPtr.Zero;
            wtd.pwszURLReference = null;
            wtd.dwProvFlags = 0;

            return wtd;
        }

        [ArchitectureSensitive]
        internal static WINTRUST_DATA InitWintrustDataStructFromBlob(WINTRUST_BLOB_INFO wbi)
        {
            WINTRUST_DATA wtd = new WINTRUST_DATA();

            wtd.cbStruct = (DWORD)Marshal.SizeOf(wbi);
            wtd.pPolicyCallbackData = IntPtr.Zero;
            wtd.pSIPClientData = IntPtr.Zero;
            wtd.dwUIChoice = (DWORD)WintrustUIChoice.WTD_UI_NONE;
            wtd.fdwRevocationChecks = 0;
            wtd.dwUnionChoice = (DWORD)WintrustUnionChoice.WTD_CHOICE_BLOB;

            IntPtr pBlob = Marshal.AllocCoTaskMem(Marshal.SizeOf(wbi));
            Marshal.StructureToPtr(wbi, pBlob, false);
            wtd.Choice.pBlob = pBlob;

            wtd.dwStateAction = (DWORD)WintrustAction.WTD_STATEACTION_VERIFY;
            wtd.hWVTStateData = IntPtr.Zero;
            wtd.pwszURLReference = null;
            wtd.dwProvFlags = 0;

            return wtd;
        }

        [ArchitectureSensitive]
        internal static DWORD DestroyWintrustDataStruct(WINTRUST_DATA wtd)
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

                wtd.dwStateAction = (DWORD)WintrustAction.WTD_STATEACTION_CLOSE;
                wtdBuffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(wtd));
                Marshal.StructureToPtr(wtd, wtdBuffer, false);

                // The GetLastWin32Error of this is checked, but PreSharp doesn't seem to be
                // able to see that.
#pragma warning disable 56523
                dwResult = WinVerifyTrust(
                    IntPtr.Zero,
                    WINTRUST_ACTION_GENERIC_VERIFY_V2,
                    wtdBuffer);
#pragma warning restore 56523

                wtd = Marshal.PtrToStructure<WINTRUST_DATA>(wtdBuffer);
            }
            finally
            {
                Marshal.DestroyStructure<WINTRUST_DATA>(wtdBuffer);
                Marshal.FreeCoTaskMem(wtdBuffer);
                Marshal.DestroyStructure<Guid>(WINTRUST_ACTION_GENERIC_VERIFY_V2);
                Marshal.FreeCoTaskMem(WINTRUST_ACTION_GENERIC_VERIFY_V2);
            }

            // Clear the blob or file info, depending on the type of
            // verification that was done.
            if (wtd.dwUnionChoice == (DWORD)WintrustUnionChoice.WTD_CHOICE_BLOB)
            {
                WINTRUST_BLOB_INFO originalBlob =
                    (WINTRUST_BLOB_INFO)Marshal.PtrToStructure<WINTRUST_BLOB_INFO>(wtd.Choice.pBlob);
                Marshal.FreeCoTaskMem(originalBlob.pbMemObject);

                Marshal.DestroyStructure<WINTRUST_BLOB_INFO>(wtd.Choice.pBlob);
                Marshal.FreeCoTaskMem(wtd.Choice.pBlob);
            }
            else
            {
                Marshal.DestroyStructure<WINTRUST_FILE_INFO>(wtd.Choice.pFile);
                Marshal.FreeCoTaskMem(wtd.Choice.pFile);
            }

            return dwResult;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CRYPT_PROVIDER_CERT
        {
#pragma warning disable IDE0044
            private DWORD _cbStruct;
#pragma warning restore IDE0044
            internal IntPtr pCert; // PCCERT_CONTEXT
            private readonly BOOL _fCommercial;
            private readonly BOOL _fTrustedRoot;
            private readonly BOOL _fSelfSigned;
            private readonly BOOL _fTestCert;
            private readonly DWORD _dwRevokedReason;
            private readonly DWORD _dwConfidence;
            private readonly DWORD _dwError;
            private readonly IntPtr _pTrustListContext; // CTL_CONTEXT*
            private readonly BOOL _fTrustListSignerCert;
            private readonly IntPtr _pCtlContext; // PCCTL_CONTEXT
            private readonly DWORD _dwCtlError;
            private readonly BOOL _fIsCyclic;
            private readonly IntPtr _pChainElement; // PCERT_CHAIN_ELEMENT
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CRYPT_PROVIDER_SGNR
        {
            private readonly DWORD _cbStruct;
            private FILETIME _sftVerifyAsOf;
            private readonly DWORD _csCertChain;
            private readonly IntPtr _pasCertChain; // CRYPT_PROVIDER_CERT*
            private readonly DWORD _dwSignerType;
            private readonly IntPtr _psSigner; // CMSG_SIGNER_INFO*
            private readonly DWORD _dwError;
            internal DWORD csCounterSigners;
            internal IntPtr pasCounterSigners; // CRYPT_PROVIDER_SGNR*
            private readonly IntPtr _pChainContext; // PCCERT_CHAIN_CONTEXT
        }

        [DllImport("wintrust.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern
            IntPtr // CRYPT_PROVIDER_DATA*
            WTHelperProvDataFromStateData(IntPtr hStateData);

        [DllImport("wintrust.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern
            IntPtr // CRYPT_PROVIDER_SGNR*
            WTHelperGetProvSignerFromChain(
                IntPtr pProvData, // CRYPT_PROVIDER_DATA*
                DWORD idxSigner,
                BOOL fCounterSigner,
                DWORD idxCounterSigner
            );

        [DllImport("wintrust.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern
            IntPtr // CRYPT_PROVIDER_CERT*
            WTHelperGetProvCertFromChain(
                IntPtr pSgnr, // CRYPT_PROVIDER_SGNR*
                DWORD idxCert
            );

        /// Return Type: HRESULT->LONG->int
        ///pszFile: PCWSTR->WCHAR*
        ///hFile: HANDLE->void*
        ///sigInfoFlags: SIGNATURE_INFO_FLAGS->Anonymous_5157c654_2076_48e7_9241_84ac648615e9
        ///psiginfo: SIGNATURE_INFO*
        ///ppCertContext: void**
        ///phWVTStateData: HANDLE*
        [DllImportAttribute("wintrust.dll", EntryPoint = "WTGetSignatureInfo", CallingConvention = CallingConvention.StdCall)]
        internal static extern int WTGetSignatureInfo([InAttribute()][MarshalAsAttribute(UnmanagedType.LPWStr)] string pszFile, [InAttribute()] System.IntPtr hFile, SIGNATURE_INFO_FLAGS sigInfoFlags, ref SIGNATURE_INFO psiginfo, ref System.IntPtr ppCertContext, ref System.IntPtr phWVTStateData);

        internal static void FreeWVTStateData(System.IntPtr phWVTStateData)
        {
            WINTRUST_DATA wtd = new WINTRUST_DATA();
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

                wtd.cbStruct = (DWORD)Marshal.SizeOf(wtd);
                wtd.dwUIChoice = (DWORD)WintrustUIChoice.WTD_UI_NONE;
                wtd.fdwRevocationChecks = 0;
                wtd.dwUnionChoice = (DWORD)WintrustUnionChoice.WTD_CHOICE_BLOB;
                wtd.dwStateAction = (DWORD)WintrustAction.WTD_STATEACTION_CLOSE;
                wtd.hWVTStateData = phWVTStateData;

                wtdBuffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(wtd));
                Marshal.StructureToPtr(wtd, wtdBuffer, false);

                // The GetLastWin32Error of this is checked, but PreSharp doesn't seem to be
                // able to see that.
#pragma warning disable 56523
                dwResult = WinVerifyTrust(
                    IntPtr.Zero,
                    WINTRUST_ACTION_GENERIC_VERIFY_V2,
                    wtdBuffer);
#pragma warning restore 56523
            }
            finally
            {
                Marshal.DestroyStructure<WINTRUST_DATA>(wtdBuffer);
                Marshal.FreeCoTaskMem(wtdBuffer);
                Marshal.DestroyStructure<Guid>(WINTRUST_ACTION_GENERIC_VERIFY_V2);
                Marshal.FreeCoTaskMem(WINTRUST_ACTION_GENERIC_VERIFY_V2);
            }
        }

        //
        // stuff required for getting cert extensions
        //

        [StructLayout(LayoutKind.Sequential)]
        internal struct CERT_ENHKEY_USAGE
        {
            internal DWORD cUsageIdentifier;
            // [MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStr, SizeParamIndex=0)]
            // internal string[] rgpszUsageIdentifier; // LPSTR*
            internal IntPtr rgpszUsageIdentifier;
        }

        internal enum SIGNATURE_STATE
        {
            /// SIGNATURE_STATE_UNSIGNED_MISSING -> 0
            SIGNATURE_STATE_UNSIGNED_MISSING = 0,

            SIGNATURE_STATE_UNSIGNED_UNSUPPORTED,

            SIGNATURE_STATE_UNSIGNED_POLICY,

            SIGNATURE_STATE_INVALID_CORRUPT,

            SIGNATURE_STATE_INVALID_POLICY,

            SIGNATURE_STATE_VALID,

            SIGNATURE_STATE_TRUSTED,

            SIGNATURE_STATE_UNTRUSTED,
        }

        internal enum SIGNATURE_INFO_FLAGS
        {
            /// SIF_NONE -> 0x0000
            SIF_NONE = 0,

            /// SIF_AUTHENTICODE_SIGNED -> 0x0001
            SIF_AUTHENTICODE_SIGNED = 1,

            /// SIF_CATALOG_SIGNED -> 0x0002
            SIF_CATALOG_SIGNED = 2,

            /// SIF_VERSION_INFO -> 0x0004
            SIF_VERSION_INFO = 4,

            /// SIF_CHECK_OS_BINARY -> 0x0800
            SIF_CHECK_OS_BINARY = 2048,

            /// SIF_BASE_VERIFICATION -> 0x1000
            SIF_BASE_VERIFICATION = 4096,

            /// SIF_CATALOG_FIRST -> 0x2000
            SIF_CATALOG_FIRST = 8192,

            /// SIF_MOTW -> 0x4000
            SIF_MOTW = 16384,
        }

        internal enum SIGNATURE_INFO_AVAILABILITY
        {
            /// SIA_DISPLAYNAME -> 0x0001
            SIA_DISPLAYNAME = 1,

            /// SIA_PUBLISHERNAME -> 0x0002
            SIA_PUBLISHERNAME = 2,

            /// SIA_MOREINFOURL -> 0x0004
            SIA_MOREINFOURL = 4,

            /// SIA_HASH -> 0x0008
            SIA_HASH = 8,
        }

        internal enum SIGNATURE_INFO_TYPE
        {
            /// SIT_UNKNOWN -> 0
            SIT_UNKNOWN = 0,

            SIT_AUTHENTICODE,

            SIT_CATALOG,
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct SIGNATURE_INFO
        {
            /// DWORD->unsigned int
            internal uint cbSize;

            /// SIGNATURE_STATE->Anonymous_7e0526d8_af30_47f9_9233_a77658d0f1e5
            internal SIGNATURE_STATE nSignatureState;

            /// SIGNATURE_INFO_TYPE->Anonymous_27075e4b_faa5_4e57_ada0_6d49fae74187
            internal SIGNATURE_INFO_TYPE nSignatureType;

            /// DWORD->unsigned int
            internal uint dwSignatureInfoAvailability;

            /// DWORD->unsigned int
            internal uint dwInfoAvailability;

            /// PWSTR->WCHAR*
            [MarshalAsAttribute(UnmanagedType.LPWStr)]
            internal string pszDisplayName;

            /// DWORD->unsigned int
            internal uint cchDisplayName;

            /// PWSTR->WCHAR*
            [MarshalAsAttribute(UnmanagedType.LPWStr)]
            internal string pszPublisherName;

            /// DWORD->unsigned int
            internal uint cchPublisherName;

            /// PWSTR->WCHAR*
            [MarshalAsAttribute(UnmanagedType.LPWStr)]
            internal string pszMoreInfoURL;

            /// DWORD->unsigned int
            internal uint cchMoreInfoURL;

            /// LPBYTE->BYTE*
            internal System.IntPtr prgbHash;

            /// DWORD->unsigned int
            internal uint cbHash;

            /// BOOL->int
            internal int fOSBinary;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct CERT_INFO
        {
            /// DWORD->unsigned int
            internal uint dwVersion;

            /// CRYPT_INTEGER_BLOB->_CRYPTOAPI_BLOB
            internal CRYPT_ATTR_BLOB SerialNumber;

            /// CRYPT_ALGORITHM_IDENTIFIER->_CRYPT_ALGORITHM_IDENTIFIER
            internal CRYPT_ALGORITHM_IDENTIFIER SignatureAlgorithm;

            /// CERT_NAME_BLOB->_CRYPTOAPI_BLOB
            internal CRYPT_ATTR_BLOB Issuer;

            /// FILETIME->_FILETIME
            internal FILETIME NotBefore;

            /// FILETIME->_FILETIME
            internal FILETIME NotAfter;

            /// CERT_NAME_BLOB->_CRYPTOAPI_BLOB
            internal CRYPT_ATTR_BLOB Subject;

            /// CERT_PUBLIC_KEY_INFO->_CERT_PUBLIC_KEY_INFO
            internal CERT_PUBLIC_KEY_INFO SubjectPublicKeyInfo;

            /// CRYPT_BIT_BLOB->_CRYPT_BIT_BLOB
            internal CRYPT_BIT_BLOB IssuerUniqueId;

            /// CRYPT_BIT_BLOB->_CRYPT_BIT_BLOB
            internal CRYPT_BIT_BLOB SubjectUniqueId;

            /// DWORD->unsigned int
            internal uint cExtension;

            /// PCERT_EXTENSION->_CERT_EXTENSION*
            internal System.IntPtr rgExtension;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct CRYPT_ALGORITHM_IDENTIFIER
        {
            /// LPSTR->CHAR*
            [MarshalAsAttribute(UnmanagedType.LPStr)]
            internal string pszObjId;

            /// CRYPT_OBJID_BLOB->_CRYPTOAPI_BLOB
            internal CRYPT_ATTR_BLOB Parameters;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct FILETIME
        {
            /// DWORD->unsigned int
            internal uint dwLowDateTime;

            /// DWORD->unsigned int
            internal uint dwHighDateTime;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct CERT_PUBLIC_KEY_INFO
        {
            /// CRYPT_ALGORITHM_IDENTIFIER->_CRYPT_ALGORITHM_IDENTIFIER
            internal CRYPT_ALGORITHM_IDENTIFIER Algorithm;

            /// CRYPT_BIT_BLOB->_CRYPT_BIT_BLOB
            internal CRYPT_BIT_BLOB PublicKey;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct CRYPT_BIT_BLOB
        {
            /// DWORD->unsigned int
            internal uint cbData;

            /// BYTE*
            internal System.IntPtr pbData;

            /// DWORD->unsigned int
            internal uint cUnusedBits;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct CERT_EXTENSION
        {
            /// LPSTR->CHAR*
            [MarshalAsAttribute(UnmanagedType.LPStr)]
            internal string pszObjId;

            /// BOOL->int
            internal int fCritical;

            /// CRYPT_OBJID_BLOB->_CRYPTOAPI_BLOB
            internal CRYPT_ATTR_BLOB Value;
        }
    }

    /// <summary>
    /// Pinvoke methods from certca.dll.
    /// </summary>
    internal static partial class NativeMethods
    {
        internal const int CRYPT_E_NOT_FOUND = unchecked((int)0x80092004);
        internal const int E_INVALID_DATA = unchecked((int)0x8007000d);
        internal const int NTE_NOT_SUPPORTED = unchecked((int)0x80090029);

        internal enum AltNameType : uint
        {
            CERT_ALT_NAME_OTHER_NAME = 1,
            CERT_ALT_NAME_RFC822_NAME = 2,
            CERT_ALT_NAME_DNS_NAME = 3,
            CERT_ALT_NAME_X400_ADDRESS = 4,
            CERT_ALT_NAME_DIRECTORY_NAME = 5,
            CERT_ALT_NAME_EDI_PARTY_NAME = 6,
            CERT_ALT_NAME_URL = 7,
            CERT_ALT_NAME_IP_ADDRESS = 8,
            CERT_ALT_NAME_REGISTERED_ID = 9,
        }

        internal enum CryptDecodeFlags : uint
        {
            CRYPT_DECODE_ENABLE_PUNYCODE_FLAG = 0x02000000,
            CRYPT_DECODE_ENABLE_UTF8PERCENT_FLAG = 0x04000000,
            CRYPT_DECODE_ENABLE_IA5CONVERSION_FLAG = (CRYPT_DECODE_ENABLE_PUNYCODE_FLAG | CRYPT_DECODE_ENABLE_UTF8PERCENT_FLAG),
        }
    }

    #region SAFER_APIs

    // SAFER native methods
    internal static partial class NativeMethods
    {
        /// Return Type: BOOL->int
        ///dwNumProperties: DWORD->unsigned int
        ///pCodeProperties: PSAFER_CODE_PROPERTIES->_SAFER_CODE_PROPERTIES*
        ///pLevelHandle: SAFER_LEVEL_HANDLE*
        ///lpReserved: LPVOID->void*
        [DllImportAttribute("advapi32.dll", EntryPoint = "SaferIdentifyLevel", SetLastError = true)]
        [return: MarshalAsAttribute(UnmanagedType.Bool)]
        internal static extern bool SaferIdentifyLevel(
            uint dwNumProperties,
            [InAttribute()]
            ref SAFER_CODE_PROPERTIES pCodeProperties,
            out IntPtr pLevelHandle,
            [InAttribute()]
            [MarshalAsAttribute(UnmanagedType.LPWStr)]
            string bucket);

        /// Return Type: BOOL->int
        ///LevelHandle: SAFER_LEVEL_HANDLE->SAFER_LEVEL_HANDLE__*
        ///InAccessToken: HANDLE->void*
        ///OutAccessToken: PHANDLE->HANDLE*
        ///dwFlags: DWORD->unsigned int
        ///lpReserved: LPVOID->void*
        [DllImportAttribute("advapi32.dll", EntryPoint = "SaferComputeTokenFromLevel", SetLastError = true)]
        [return: MarshalAsAttribute(UnmanagedType.Bool)]
        internal static extern bool SaferComputeTokenFromLevel(
            [InAttribute()]
            IntPtr LevelHandle,
            [InAttribute()]
            System.IntPtr InAccessToken,
            ref System.IntPtr OutAccessToken,
            uint dwFlags,
            System.IntPtr lpReserved);

        /// Return Type: BOOL->int
        ///hLevelHandle: SAFER_LEVEL_HANDLE->SAFER_LEVEL_HANDLE__*
        [DllImportAttribute("advapi32.dll", EntryPoint = "SaferCloseLevel")]
        [return: MarshalAsAttribute(UnmanagedType.Bool)]
        internal static extern bool SaferCloseLevel([InAttribute()] IntPtr hLevelHandle);

        /// Return Type: BOOL->int
        ///hObject: HANDLE->void*
        [DllImportAttribute(PinvokeDllNames.CloseHandleDllName, EntryPoint = "CloseHandle")]
        [return: MarshalAsAttribute(UnmanagedType.Bool)]
        internal static extern bool CloseHandle([InAttribute()] System.IntPtr hObject);
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    internal struct SAFER_CODE_PROPERTIES
    {
        /// DWORD->unsigned int
        public uint cbSize;

        /// DWORD->unsigned int
        public uint dwCheckFlags;

        /// LPCWSTR->WCHAR*
        [MarshalAsAttribute(UnmanagedType.LPWStr)]
        public string ImagePath;

        /// HANDLE->void*
        public System.IntPtr hImageFileHandle;

        /// DWORD->unsigned int
        public uint UrlZoneId;

        /// BYTE[SAFER_MAX_HASH_SIZE]
        [MarshalAsAttribute(
            UnmanagedType.ByValArray,
            SizeConst = NativeConstants.SAFER_MAX_HASH_SIZE,
            ArraySubType = UnmanagedType.I1)]
        public byte[] ImageHash;

        /// DWORD->unsigned int
        public uint dwImageHashSize;

        /// LARGE_INTEGER->_LARGE_INTEGER
        public LARGE_INTEGER ImageSize;

        /// ALG_ID->unsigned int
        public uint HashAlgorithm;

        /// LPBYTE->BYTE*
        public System.IntPtr pByteBlock;

        /// HWND->HWND__*
        public System.IntPtr hWndParent;

        /// DWORD->unsigned int
        public uint dwWVTUIChoice;
    }

    [StructLayoutAttribute(LayoutKind.Explicit)]
    internal struct LARGE_INTEGER
    {
        /// Anonymous_9320654f_2227_43bf_a385_74cc8c562686
        [FieldOffsetAttribute(0)]
        public Anonymous_9320654f_2227_43bf_a385_74cc8c562686 Struct1;

        /// Anonymous_947eb392_1446_4e25_bbd4_10e98165f3a9
        [FieldOffsetAttribute(0)]
        public Anonymous_947eb392_1446_4e25_bbd4_10e98165f3a9 u;

        /// LONGLONG->__int64
        [FieldOffsetAttribute(0)]
        public long QuadPart;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    internal struct HWND__
    {
        /// int
        public int unused;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    internal struct Anonymous_9320654f_2227_43bf_a385_74cc8c562686
    {
        /// DWORD->unsigned int
        public uint LowPart;

        /// LONG->int
        public int HighPart;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    internal struct Anonymous_947eb392_1446_4e25_bbd4_10e98165f3a9
    {
        /// DWORD->unsigned int
        public uint LowPart;

        /// LONG->int
        public int HighPart;
    }

    #endregion SAFER_APIs

    /// <summary>
    /// Pinvoke methods from advapi32.dll.
    /// </summary>
    internal static partial class NativeMethods
    {
        //
        // This is duplicating some of the effort made in Win32Native.cs,
        // namespace = Microsoft.PowerShell.Commands.Internal.Win32Native
        //
        internal const uint ERROR_SUCCESS = 0;
        internal const uint ERROR_NO_TOKEN = 0x3f0;

        internal const uint STATUS_SUCCESS = 0;
        internal const uint STATUS_INVALID_PARAMETER = 0xC000000D;

        internal const uint ACL_REVISION = 2;

        internal const uint SYSTEM_SCOPED_POLICY_ID_ACE_TYPE = 0x13;

        internal const uint SUB_CONTAINERS_AND_OBJECTS_INHERIT = 0x3;
        internal const uint INHERIT_ONLY_ACE = 0x8;

        internal const uint TOKEN_ASSIGN_PRIMARY = 0x0001;
        internal const uint TOKEN_DUPLICATE = 0x0002;
        internal const uint TOKEN_IMPERSONATE = 0x0004;
        internal const uint TOKEN_QUERY = 0x0008;
        internal const uint TOKEN_QUERY_SOURCE = 0x0010;
        internal const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        internal const uint TOKEN_ADJUST_GROUPS = 0x0040;
        internal const uint TOKEN_ADJUST_DEFAULT = 0x0080;
        internal const uint TOKEN_ADJUST_SESSIONID = 0x0100;

        internal const uint SE_PRIVILEGE_ENABLED_BY_DEFAULT = 0x00000001;
        internal const uint SE_PRIVILEGE_ENABLED = 0x00000002;
        internal const uint SE_PRIVILEGE_REMOVED = 0X00000004;
        internal const uint SE_PRIVILEGE_USED_FOR_ACCESS = 0x80000000;

        internal enum SeObjectType : uint
        {
            SE_UNKNOWN_OBJECT_TYPE = 0,
            SE_FILE_OBJECT = 1,
            SE_SERVICE = 2,
            SE_PRINTER = 3,
            SE_REGISTRY_KEY = 4,
            SE_LMSHARE = 5,
            SE_KERNEL_OBJECT = 6,
            SE_WINDOW_OBJECT = 7,
            SE_DS_OBJECT = 8,
            SE_DS_OBJECT_ALL = 9,
            SE_PROVIDER_DEFINED_OBJECT = 10,
            SE_WMIGUID_OBJECT = 11,
            SE_REGISTRY_WOW64_32KEY = 12
        }

        internal enum SecurityInformation : uint
        {
            OWNER_SECURITY_INFORMATION = 0x00000001,
            GROUP_SECURITY_INFORMATION = 0x00000002,
            DACL_SECURITY_INFORMATION = 0x00000004,
            SACL_SECURITY_INFORMATION = 0x00000008,
            LABEL_SECURITY_INFORMATION = 0x00000010,
            ATTRIBUTE_SECURITY_INFORMATION = 0x00000020,
            SCOPE_SECURITY_INFORMATION = 0x00000040,
            BACKUP_SECURITY_INFORMATION = 0x00010000,
            PROTECTED_DACL_SECURITY_INFORMATION = 0x80000000,
            PROTECTED_SACL_SECURITY_INFORMATION = 0x40000000,
            UNPROTECTED_DACL_SECURITY_INFORMATION = 0x20000000,
            UNPROTECTED_SACL_SECURITY_INFORMATION = 0x10000000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct LUID
        {
            internal uint LowPart;
            internal uint HighPart;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct LUID_AND_ATTRIBUTES
        {
            internal LUID Luid;
            internal uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct TOKEN_PRIVILEGE
        {
            internal uint PrivilegeCount;
            internal LUID_AND_ATTRIBUTES Privilege;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct ACL
        {
            internal byte AclRevision;
            internal byte Sbz1;
            internal ushort AclSize;
            internal ushort AceCount;
            internal ushort Sbz2;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct ACE_HEADER
        {
            internal byte AceType;
            internal byte AceFlags;
            internal ushort AceSize;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct SYSTEM_AUDIT_ACE
        {
            internal ACE_HEADER Header;
            internal uint Mask;
            internal uint SidStart;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct LSA_UNICODE_STRING
        {
            internal ushort Length;
            internal ushort MaximumLength;
            internal IntPtr Buffer;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CENTRAL_ACCESS_POLICY
        {
            internal IntPtr CAPID;
            internal LSA_UNICODE_STRING Name;
            internal LSA_UNICODE_STRING Description;
            internal LSA_UNICODE_STRING ChangeId;
            internal uint Flags;
            internal uint CAPECount;
            internal IntPtr CAPEs;
        }

        [DllImport(PinvokeDllNames.GetNamedSecurityInfoDllName, CharSet = CharSet.Unicode)]
        internal static extern uint GetNamedSecurityInfo(
            string pObjectName,
            SeObjectType ObjectType,
            SecurityInformation SecurityInfo,
            out IntPtr ppsidOwner,
            out IntPtr ppsidGroup,
            out IntPtr ppDacl,
            out IntPtr ppSacl,
            out IntPtr ppSecurityDescriptor
        );

        [DllImport(PinvokeDllNames.SetNamedSecurityInfoDllName, CharSet = CharSet.Unicode)]
        internal static extern uint SetNamedSecurityInfo(
            string pObjectName,
            SeObjectType ObjectType,
            SecurityInformation SecurityInfo,
            IntPtr psidOwner,
            IntPtr psidGroup,
            IntPtr pDacl,
            IntPtr pSacl);

        [DllImport(PinvokeDllNames.ConvertStringSidToSidDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ConvertStringSidToSid(
            string StringSid,
            out IntPtr Sid);

        [DllImport(PinvokeDllNames.IsValidSidDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsValidSid(IntPtr pSid);

        [DllImport(PinvokeDllNames.GetLengthSidDllName, CharSet = CharSet.Unicode)]
        internal static extern uint GetLengthSid(IntPtr pSid);

        [DllImport("Advapi32.dll", CharSet = CharSet.Unicode)]
        internal static extern uint LsaQueryCAPs(
            IntPtr[] CAPIDs,
            uint CAPIDCount,
            out IntPtr CAPs,
            out uint CAPCount);

        [DllImport(PinvokeDllNames.LsaFreeMemoryDllName, CharSet = CharSet.Unicode)]
        internal static extern uint LsaFreeMemory(IntPtr Buffer);

        [DllImport(PinvokeDllNames.InitializeAclDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool InitializeAcl(
            IntPtr pAcl,
            uint nAclLength,
            uint dwAclRevision);

        [DllImport("api-ms-win-security-base-l1-2-0.dll", CharSet = CharSet.Unicode)]
        internal static extern uint AddScopedPolicyIDAce(
            IntPtr Acl,
            uint AceRevision,
            uint AceFlags,
            uint AccessMask,
            IntPtr Sid);

        [DllImport(PinvokeDllNames.GetCurrentProcessDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr GetCurrentProcess();

        [DllImport(PinvokeDllNames.GetCurrentThreadDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr GetCurrentThread();

        [DllImport(PinvokeDllNames.OpenProcessTokenDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool OpenProcessToken(
            IntPtr ProcessHandle,
            uint DesiredAccess,
            out IntPtr TokenHandle);

        [DllImport(PinvokeDllNames.OpenThreadTokenDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool OpenThreadToken(
            IntPtr ThreadHandle,
            uint DesiredAccess,
            bool OpenAsSelf,
            out IntPtr TokenHandle);

        [DllImport(PinvokeDllNames.LookupPrivilegeValueDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool LookupPrivilegeValue(
            string lpSystemName,
            string lpName,
            ref LUID lpLuid);

        [DllImport(PinvokeDllNames.AdjustTokenPrivilegesDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AdjustTokenPrivileges(
            IntPtr TokenHandle,
            bool DisableAllPrivileges,
            ref TOKEN_PRIVILEGE NewState,
            uint BufferLength,
            ref TOKEN_PRIVILEGE PreviousState,
            ref uint ReturnLength);

        [DllImport(PinvokeDllNames.LocalFreeDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr LocalFree(IntPtr hMem);

        internal const uint DONT_RESOLVE_DLL_REFERENCES = 0x00000001;
        internal const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
        internal const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;
        internal const uint LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010;
        internal const uint LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020;
        internal const uint LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040;
        internal const uint LOAD_LIBRARY_REQUIRE_SIGNED_TARGET = 0x00000080;
        internal const uint LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR = 0x00000100;
        internal const uint LOAD_LIBRARY_SEARCH_APPLICATION_DIR = 0x00000200;
        internal const uint LOAD_LIBRARY_SEARCH_USER_DIRS = 0x00000400;
        internal const uint LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800;
        internal const uint LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000;

        [DllImport(PinvokeDllNames.LoadLibraryEx, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr LoadLibraryExW(
            string DllName,
            IntPtr reserved,
            uint Flags);

        [DllImport(PinvokeDllNames.FreeLibrary, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary(
            IntPtr Module);

        internal static bool IsSystem32DllPresent(string DllName)
        {
            bool DllExists = false;

            try
            {
                IntPtr module = LoadLibraryExW(
                                        DllName,
                                        IntPtr.Zero,
                                        NativeMethods.LOAD_LIBRARY_AS_DATAFILE |
                                            NativeMethods.LOAD_LIBRARY_AS_IMAGE_RESOURCE |
                                            NativeMethods.LOAD_LIBRARY_SEARCH_SYSTEM32);
                if (module != IntPtr.Zero)
                {
                    FreeLibrary(module);
                    DllExists = true;
                }
            }
            catch (Exception)
            {
            }

            return DllExists;
        }
    }

    // Constants needed for Catalog Error Handling
    internal partial class NativeConstants
    {
        // CRYPTCAT_E_AREA_HEADER = "0x00000000";
        public const int CRYPTCAT_E_AREA_HEADER = 0;

        // CRYPTCAT_E_AREA_MEMBER = "0x00010000";
        public const int CRYPTCAT_E_AREA_MEMBER = 65536;

        // CRYPTCAT_E_AREA_ATTRIBUTE = "0x00020000";
        public const int CRYPTCAT_E_AREA_ATTRIBUTE = 131072;

        // CRYPTCAT_E_CDF_UNSUPPORTED = "0x00000001";
        public const int CRYPTCAT_E_CDF_UNSUPPORTED = 1;

        // CRYPTCAT_E_CDF_DUPLICATE = "0x00000002";
        public const int CRYPTCAT_E_CDF_DUPLICATE = 2;

        // CRYPTCAT_E_CDF_TAGNOTFOUND = "0x00000004";
        public const int CRYPTCAT_E_CDF_TAGNOTFOUND = 4;

        // CRYPTCAT_E_CDF_MEMBER_FILE_PATH = "0x00010001";
        public const int CRYPTCAT_E_CDF_MEMBER_FILE_PATH = 65537;

        // CRYPTCAT_E_CDF_MEMBER_INDIRECTDATA = "0x00010002";
        public const int CRYPTCAT_E_CDF_MEMBER_INDIRECTDATA = 65538;

        // CRYPTCAT_E_CDF_MEMBER_FILENOTFOUND = "0x00010004";
        public const int CRYPTCAT_E_CDF_MEMBER_FILENOTFOUND = 65540;

        // CRYPTCAT_E_CDF_BAD_GUID_CONV = "0x00020001";
        public const int CRYPTCAT_E_CDF_BAD_GUID_CONV = 131073;

        // CRYPTCAT_E_CDF_ATTR_TOOFEWVALUES = "0x00020002";
        public const int CRYPTCAT_E_CDF_ATTR_TOOFEWVALUES = 131074;

        // CRYPTCAT_E_CDF_ATTR_TYPECOMBO = "0x00020004";
        public const int CRYPTCAT_E_CDF_ATTR_TYPECOMBO = 131076;
    }

    /// <summary>
    /// Pinvoke methods from wintrust.dll
    /// These are added to Generate and Validate Window Catalog Files.
    /// </summary>
    internal static partial class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct CRYPT_ATTRIBUTE_TYPE_VALUE
        {
            [MarshalAs(UnmanagedType.LPStr)]
            internal string pszObjId;

            internal CRYPT_ATTR_BLOB Value;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SIP_INDIRECT_DATA
        {
            internal CRYPT_ATTRIBUTE_TYPE_VALUE Data;
            internal CRYPT_ALGORITHM_IDENTIFIER DigestAlgorithm;
            internal CRYPT_ATTR_BLOB Digest;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct CRYPTCATCDF
        {
            private readonly DWORD _cbStruct;
            private readonly IntPtr _hFile;
            private readonly DWORD _dwCurFilePos;
            private readonly DWORD _dwLastMemberOffset;
            private readonly BOOL _fEOF;

            [MarshalAs(UnmanagedType.LPWStr)]
            private readonly string _pwszResultDir;

            private readonly IntPtr _hCATStore;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CRYPTCATMEMBER
        {
            internal DWORD cbStruct;

            [MarshalAs(UnmanagedType.LPWStr)]
            internal string pwszReferenceTag;

            [MarshalAs(UnmanagedType.LPWStr)]
            internal string pwszFileName;

            internal Guid gSubjectType;
            internal DWORD fdwMemberFlags;
            internal IntPtr pIndirectData;
            internal DWORD dwCertVersion;
            internal DWORD dwReserved;
            internal IntPtr hReserved;
            internal CRYPT_ATTR_BLOB sEncodedIndirectData;
            internal CRYPT_ATTR_BLOB sEncodedMemberInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CRYPTCATATTRIBUTE
        {
            private readonly DWORD _cbStruct;

            [MarshalAs(UnmanagedType.LPWStr)]
            internal string pwszReferenceTag;

            private readonly DWORD _dwAttrTypeAndAction;
            internal DWORD cbValue;
            internal System.IntPtr pbValue;
            private readonly DWORD _dwReserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CRYPTCATSTORE
        {
            private readonly DWORD _cbStruct;
            internal DWORD dwPublicVersion;

            [MarshalAs(UnmanagedType.LPWStr)]
            internal string pwszP7File;

            private readonly IntPtr _hProv;
            private readonly DWORD _dwEncodingType;
            private readonly DWORD _fdwStoreFlags;
            private readonly IntPtr _hReserved;
            private readonly IntPtr _hAttrs;
            private readonly IntPtr _hCryptMsg;
            private readonly IntPtr _hSorted;
        }

        [DllImport("wintrust.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr CryptCATCDFOpen(
            [MarshalAs(UnmanagedType.LPWStr)]
            string pwszFilePath,
            CryptCATCDFOpenCallBack pfnParseError
        );

        [DllImport("wintrust.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CryptCATCDFClose(
            IntPtr pCDF
        );

        [DllImport("wintrust.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr CryptCATCDFEnumCatAttributes(
            IntPtr pCDF,
            IntPtr pPrevAttr,
            CryptCATCDFOpenCallBack pfnParseError
        );

        [DllImport("wintrust.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr CryptCATCDFEnumMembersByCDFTagEx(
            IntPtr pCDF,
            IntPtr pwszPrevCDFTag,
            CryptCATCDFEnumMembersByCDFTagExErrorCallBack fn,
            ref IntPtr ppMember,
            bool fContinueOnError,
            IntPtr pvReserved
        );

        [DllImport("wintrust.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr CryptCATCDFEnumAttributesWithCDFTag(
            IntPtr pCDF,
            IntPtr pwszMemberTag,
            IntPtr pMember,
            IntPtr pPrevAttr,
            CryptCATCDFEnumMembersByCDFTagExErrorCallBack fn
        );

        [DllImport("wintrust.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr CryptCATOpen(
            [MarshalAs(UnmanagedType.LPWStr)]
            string pwszFilePath,
            DWORD fdwOpenFlags,
            IntPtr hProv,
            DWORD dwPublicVersion,
            DWORD dwEncodingType
         );

        [DllImport("wintrust.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CryptCATClose(
          IntPtr hCatalog
        );

        [DllImport("wintrust.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr CryptCATStoreFromHandle(
            IntPtr hCatalog
        );

        [DllImport("wintrust.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CryptCATAdminAcquireContext2(
          ref IntPtr phCatAdmin,
          IntPtr pgSubsystem,
          [MarshalAs(UnmanagedType.LPWStr)]
          string pwszHashAlgorithm,
          IntPtr pStrongHashPolicy,
          DWORD dwFlags
      );

        [DllImport("wintrust.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CryptCATAdminReleaseContext(
            IntPtr phCatAdmin,
            DWORD dwFlags
        );

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern unsafe IntPtr CreateFile(
            string lpFileName,
            DWORD dwDesiredAccess,
            DWORD dwShareMode,
            DWORD lpSecurityAttributes,
            DWORD dwCreationDisposition,
            DWORD dwFlagsAndAttributes,
            IntPtr hTemplateFile
           );

        [DllImport("wintrust.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CryptCATAdminCalcHashFromFileHandle2(
            IntPtr hCatAdmin,
            IntPtr hFile,
            [In, Out] ref DWORD pcbHash,
            IntPtr pbHash,
            DWORD dwFlags
        );

        [DllImport("wintrust.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr CryptCATEnumerateCatAttr(
            IntPtr hCatalog,
            IntPtr pPrevAttr
        );

        [DllImport("wintrust.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr CryptCATEnumerateMember(
                IntPtr hCatalog,
                IntPtr pPrevMember
        );

        [DllImport("wintrust.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr CryptCATEnumerateAttr(
            IntPtr hCatalog,
            IntPtr pCatMember,
            IntPtr pPrevAttr
        );

        /// <summary>
        /// Signature of call back function used by CryptCATCDFOpen.
        /// </summary>
        internal delegate
        void CryptCATCDFOpenCallBack(DWORD NotUsedDWORD1,
                                      DWORD NotUsedDWORD2,
                                      [MarshalAs(UnmanagedType.LPWStr)]
                                      string NotUsedString);

        /// <summary>
        /// Signature of call back function used by CryptCATCDFEnumMembersByCDFTagEx.
        /// </summary>
        internal delegate
        void CryptCATCDFEnumMembersByCDFTagExErrorCallBack(DWORD NotUsedDWORD1,
                                      DWORD NotUsedDWORD2,
                                      [MarshalAs(UnmanagedType.LPWStr)]
                                      string NotUsedString);
    }
}

#pragma warning restore 56523
