// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Runtime.InteropServices;

namespace System.Management.Automation.Win32Native;

internal class SafeCATAdminHandle : SafeHandle
{
    internal SafeCATAdminHandle() : base(IntPtr.Zero, true) { }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle() => WinTrustMethods.CryptCATAdminReleaseContext(handle, 0);
}

internal class SafeCATHandle : SafeHandle
{
    internal SafeCATHandle() : base(IntPtr.Zero, true) { }

    public override bool IsInvalid => handle == (IntPtr)(-1);

    protected override bool ReleaseHandle() => WinTrustMethods.CryptCATClose(handle);
}

internal class SafeCATCDFHandle : SafeHandle
{
    internal SafeCATCDFHandle() : base(IntPtr.Zero, true) { }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle() => WinTrustMethods.CryptCATCDFClose(handle);
}

[Flags]
internal enum WinTrustUIChoice
{
    WTD_UI_ALL = 1,
    WTD_UI_NONE = 2,
    WTD_UI_NOBAD = 3,
    WTD_UI_NOGOOD = 4
}

[Flags]
internal enum WinTrustUnionChoice
{
    WTD_CHOICE_FILE = 1,
    WTD_CHOICE_CATALOG = 2,
    WTD_CHOICE_BLOB = 3,
    WTD_CHOICE_SIGNER = 4,
    WTD_CHOICE_CERT = 5,
}

[Flags]
internal enum WinTrustAction
{
    WTD_STATEACTION_IGNORE = 0x00000000,
    WTD_STATEACTION_VERIFY = 0x00000001,
    WTD_STATEACTION_CLOSE = 0x00000002,
    WTD_STATEACTION_AUTO_CACHE = 0x00000003,
    WTD_STATEACTION_AUTO_CACHE_FLUSH = 0x00000004
}

[Flags]
internal enum WinTrustProviderFlags
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

/// <summary>
/// Pinvoke methods from wintrust.dll
/// </summary>
internal static class WinTrustMethods
{
    private const string WinTrustDll = "wintrust.dll";

    [StructLayout(LayoutKind.Sequential)]
    internal struct CRYPT_ATTR_BLOB
    {
        public uint cbData;
        public IntPtr pbData;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    internal struct CRYPT_ALGORITHM_IDENTIFIER
    {
        [MarshalAsAttribute(UnmanagedType.LPStr)] public string pszObjId;
        public CRYPT_ATTR_BLOB Parameters;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CRYPT_ATTRIBUTE_TYPE_VALUE
    {
        [MarshalAs(UnmanagedType.LPStr)] public string pszObjId;
        public CRYPT_ATTR_BLOB Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SIP_INDIRECT_DATA
    {
        public CRYPT_ATTRIBUTE_TYPE_VALUE Data;
        public CRYPT_ALGORITHM_IDENTIFIER DigestAlgorithm;
        public CRYPT_ATTR_BLOB Digest;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CRYPTCATMEMBER
    {
        public uint cbStruct;
        [MarshalAs(UnmanagedType.LPWStr)] public string pwszReferenceTag;
        [MarshalAs(UnmanagedType.LPWStr)] public string pwszFileName;
        public Guid gSubjectType;
        public uint fdwMemberFlags;
        public IntPtr pIndirectData;
        public uint dwCertVersion;
        public uint dwReserved;
        public IntPtr hReserved;
        public CRYPT_ATTR_BLOB sEncodedIndirectData;
        public CRYPT_ATTR_BLOB sEncodedMemberInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CRYPTCATATTRIBUTE
    {
        public uint cbStruct;
        [MarshalAs(UnmanagedType.LPWStr)] public string pwszReferenceTag;
        public uint dwAttrTypeAndAction;
        public uint cbValue;
        public IntPtr pbValue;
        public uint dwReserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CRYPTCATSTORE
    {
        public uint cbStruct;
        public uint dwPublicVersion;
        [MarshalAs(UnmanagedType.LPWStr)] public string pwszP7File;
        public IntPtr hProv;
        public uint dwEncodingType;
        public uint fdwStoreFlags;
        public IntPtr hReserved;
        public IntPtr hAttrs;
        public IntPtr hCryptMsg;
        public IntPtr hSorted;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    internal struct WINTRUST_DATA
    {
        public uint cbStruct;
        public IntPtr pPolicyCallbackData;
        public IntPtr pSIPClientData;
        public WinTrustUIChoice dwUIChoice;
        public uint fdwRevocationChecks;
        public WinTrustUnionChoice dwUnionChoice;
        public unsafe void* pChoice;
        public WinTrustAction dwStateAction;
        public IntPtr hWVTStateData;
        public IntPtr pwszURLReference;
        public WinTrustProviderFlags dwProvFlags;
        public uint dwUIContext;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WINTRUST_FILE_INFO
    {
        public uint cbStruct;
        public unsafe char* pcwszFilePath;
        public IntPtr hFile;
        public IntPtr pgKnownSubject;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    internal struct WINTRUST_BLOB_INFO
    {
        public uint cbStruct;
        public Guid gSubject;
        public unsafe char* pcwszDisplayName;
        public uint cbMemObject;
        public unsafe byte* pbMemObject;
        public uint cbMemSignedMsg;
        public IntPtr pbMemSignedMsg;
    }

    [DllImport(
        WinTrustDll,
        CharSet = CharSet.Unicode,
        EntryPoint = "CryptCATAdminAcquireContext2",
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool NativeCryptCATAdminAcquireContext2(
        out SafeCATAdminHandle phCatAdmin,
        IntPtr pgSubsystem,
        [MarshalAs(UnmanagedType.LPWStr)] string pwszHashAlgorithm,
        IntPtr pStrongHashPolicy,
        uint dwFlags
    );

    internal static SafeCATAdminHandle CryptCATAdminAcquireContext2(string hashAlgorithm)
    {
        if (!NativeCryptCATAdminAcquireContext2(out var adminHandle, IntPtr.Zero, hashAlgorithm, IntPtr.Zero, 0))
        {
            throw new Win32Exception();
        }

        return adminHandle;
    }

    [DllImport(
        WinTrustDll,
        CharSet = CharSet.Unicode,
        EntryPoint = "CryptCATAdminCalcHashFromFileHandle2",
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern unsafe bool NativeCryptCATAdminCalcHashFromFileHandle2(
        SafeCATAdminHandle hCatAdmin,
        SafeHandle hFile,
        [In, Out] ref int pcbHash,
        byte* pbHash,
        uint dwFlags
    );

    internal static byte[] CryptCATAdminCalcHashFromFileHandle2(SafeCATAdminHandle catAdmin, SafeHandle file)
    {
        unsafe
        {
            int hashLength = 0;
            NativeCryptCATAdminCalcHashFromFileHandle2(catAdmin, file, ref hashLength, null, 0);

            byte[] hash = new byte[hashLength];
            fixed (byte* hashPtr = hash)
            {
                if (!NativeCryptCATAdminCalcHashFromFileHandle2(catAdmin, file, ref hashLength, hashPtr, 0))
                {
                    throw new Win32Exception();
                }
            }

            return hash;
        }
    }

    [DllImport(WinTrustDll, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CryptCATAdminReleaseContext(
        IntPtr phCatAdmin,
        uint dwFlags
    );

    [DllImport(WinTrustDll, CharSet = CharSet.Unicode, EntryPoint = "CryptCATCDFOpen")]
    private static extern SafeCATCDFHandle NativeCryptCATCDFOpen(
        [MarshalAs(UnmanagedType.LPWStr)] string pwszFilePath,
        CryptCATCDFParseErrorCallBack pfnParseError
    );

    internal static SafeCATCDFHandle CryptCATCDFOpen(string filePath, CryptCATCDFParseErrorCallBack parseError)
    {
        SafeCATCDFHandle handle = NativeCryptCATCDFOpen(filePath, parseError);
        if (handle.IsInvalid)
        {
            throw new Win32Exception();
        }

        return handle;
    }

    [DllImport(WinTrustDll, CharSet = CharSet.Unicode)]
    internal static extern IntPtr CryptCATCDFEnumCatAttributes(
        SafeCATCDFHandle pCDF,
        IntPtr pPrevAttr,
        CryptCATCDFParseErrorCallBack pfnParseError
    );

    [DllImport(WinTrustDll)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CryptCATCDFClose(
        IntPtr pCDF
    );

    [DllImport(WinTrustDll, CharSet = CharSet.Unicode)]
    internal static extern IntPtr CryptCATCDFEnumMembersByCDFTagEx(
        SafeCATCDFHandle pCDF,
        IntPtr pwszPrevCDFTag,
        CryptCATCDFParseErrorCallBack fn,
        ref IntPtr ppMember,
        bool fContinueOnError,
        IntPtr pvReserved
    );

    [DllImport(WinTrustDll, CharSet = CharSet.Unicode)]
    internal static extern IntPtr CryptCATCDFEnumAttributesWithCDFTag(
        SafeCATCDFHandle pCDF,
        IntPtr pwszMemberTag,
        IntPtr pMember,
        IntPtr pPrevAttr,
        CryptCATCDFParseErrorCallBack fn
    );

    [DllImport(WinTrustDll, CharSet = CharSet.Unicode)]
    internal static extern IntPtr CryptCATEnumerateCatAttr(
        SafeCATHandle hCatalog,
        IntPtr pPrevAttr
    );

    [DllImport(WinTrustDll, CharSet = CharSet.Unicode, EntryPoint = "CryptCATOpen", SetLastError = true)]
    internal static extern SafeCATHandle NativeCryptCATOpen(
        [MarshalAs(UnmanagedType.LPWStr)] string pwszFilePath,
        uint fdwOpenFlags,
        IntPtr hProv,
        uint dwPublicVersion,
        uint dwEncodingType
    );

    internal static SafeCATHandle CryptCATOpen(string filePath, uint openFlags, IntPtr provider, uint publicVersion,
        uint encodingType)
    {
        SafeCATHandle handle = NativeCryptCATOpen(filePath, openFlags, provider, publicVersion, encodingType);
        if (handle.IsInvalid)
        {
            throw new Win32Exception();
        }

        return handle;
    }

    [DllImport(WinTrustDll)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CryptCATClose(
        IntPtr hCatalog
    );

    [DllImport(WinTrustDll, CharSet = CharSet.Unicode, EntryPoint = "CryptCATStoreFromHandle")]
    private static extern IntPtr NativeCryptCATStoreFromHandle(
        SafeCATHandle hCatalog
    );

    internal static CRYPTCATSTORE CryptCATStoreFromHandle(SafeCATHandle catalog)
    {
        IntPtr catStore = NativeCryptCATStoreFromHandle(catalog);
        return Marshal.PtrToStructure<CRYPTCATSTORE>(catStore);
    }

    [DllImport(WinTrustDll, CharSet = CharSet.Unicode)]
    internal static extern IntPtr CryptCATEnumerateMember(
        SafeCATHandle hCatalog,
        IntPtr pPrevMember
    );

    [DllImport(WinTrustDll, CharSet = CharSet.Unicode)]
    internal static extern IntPtr CryptCATEnumerateAttr(
        SafeCATHandle hCatalog,
        IntPtr pCatMember,
        IntPtr pPrevAttr
    );

    [DllImport(WinTrustDll, CharSet = CharSet.Unicode)]
    internal static extern uint WinVerifyTrust(
        IntPtr hWnd,
        ref Guid pgActionID,
        ref WINTRUST_DATA pWVTData
    );

    [DllImport(WinTrustDll, CharSet = CharSet.Unicode, EntryPoint = "WTHelperGetProvCertFromChain")]
    private static extern IntPtr NativeWTHelperGetProvCertFromChain(
        IntPtr pSgnr,
        uint idxCert
    );

    internal static IntPtr WTHelperGetProvCertFromChain(IntPtr signer, uint certIdx)
    {
        IntPtr data = NativeWTHelperGetProvCertFromChain(signer, certIdx);
        if (data == IntPtr.Zero)
        {
            throw new Win32Exception("WTHelperGetProvCertFromChain failed");
        }

        return data;
    }

    [DllImport(WinTrustDll, CharSet = CharSet.Unicode, EntryPoint = "WTHelperGetProvSignerFromChain")]
    private static extern IntPtr NativeWTHelperGetProvSignerFromChain(
        IntPtr pProvData,
        uint idxSigner,
        bool fCounterSigner,
        uint idxCounterSigner
    );

    internal static IntPtr WTHelperGetProvSignerFromChain(IntPtr providerData, uint signerIdx, bool counterSigner,
        uint counterSignerIdx)
    {
        IntPtr data = NativeWTHelperGetProvSignerFromChain(providerData, signerIdx, counterSigner, counterSignerIdx);
        if (data == IntPtr.Zero)
        {
            throw new Win32Exception("WTHelperGetProvSignerFromChain failed");
        }

        return data;
    }

    [DllImport(WinTrustDll, CharSet = CharSet.Unicode, EntryPoint = "WTHelperProvDataFromStateData")]
    private static extern IntPtr NativeWTHelperProvDataFromStateData(
        IntPtr hStateData
    );

    internal static IntPtr WTHelperProvDataFromStateData(IntPtr stateData)
    {
        IntPtr data = NativeWTHelperProvDataFromStateData(stateData);
        if (data == IntPtr.Zero)
        {
            throw new Win32Exception("WTHelperProvDataFromStateData failed");
        }

        return data;
    }

    /// <summary>
    /// Signature of call back function used by CryptCATCDFOpen,
    /// CryptCATCDFEnumCatAttributes, CryptCATCDFEnumAttributesWithCDFTag, and
    /// and CryptCATCDFEnumMembersByCDFTagEx.
    /// </summary>
    internal delegate void CryptCATCDFParseErrorCallBack(
        uint dwErrorArea,
        uint dwLocalArea,
        [MarshalAs(UnmanagedType.LPWStr)] string pwszLine
    );
}
