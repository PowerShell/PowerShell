// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Keep native method argument names.")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:AccessibleFieldsMustBeginWithUpperCaseLetter", Justification = "Keep native method argument names.")]
    internal static unsafe partial class Windows
    {
        internal const int CRYPT_ACQUIRE_CACHE_FLAG = 0x00000001;
        internal const int CRYPT_ACQUIRE_USE_PROV_INFO_FLAG = 0x00000002;
        internal const int CRYPT_ACQUIRE_COMPARE_KEY_FLAG = 0x00000004;
        internal const int CRYPT_ACQUIRE_NO_HEALING = 0x00000008;
        internal const int CRYPT_ACQUIRE_SILENT_FLAG = 0x00000040;
        internal const int CRYPT_ACQUIRE_WINDOW_HANDLE_FLAG = 0x00000080;
        internal const int CRYPT_ACQUIRE_ALLOW_NCRYPT_KEY_FLAG = 0x00010000;
        internal const int CRYPT_ACQUIRE_PREFER_NCRYPT_KEY_FLAG = 0x00020000;
        internal const int CRYPT_ACQUIRE_ONLY_NCRYPT_KEY_FLAG = 0x00040000;

        internal const int CERT_NCRYPT_KEY_SPEC = unchecked((int)0xFFFFFFFF);

        internal const int NTE_BAD_KEYSET = unchecked((int)0x80090016);
        internal const int CRYPT_E_NO_KEY_PROPERTY = unchecked((int)0x8009200B);

        internal sealed class SafeCryptoPrivateKeyHandle : SafeHandle
        {
            private readonly bool _shouldFree;

            public bool IsNCryptKey { get; }

            internal SafeCryptoPrivateKeyHandle(
                nint handle,
                bool isNCryptKey,
                bool shouldFree,
                bool ownsHandle) : base(handle, ownsHandle)
            {
                IsNCryptKey = isNCryptKey;
                _shouldFree = shouldFree;
            }

            public override bool IsInvalid => handle == nint.Zero;

            protected override bool ReleaseHandle()
            {
                if (!_shouldFree)
                {
                    return true;
                }

                if (IsNCryptKey)
                {
                    return NCryptFreeObject(handle) == 0;
                }
                else
                {
                    return CryptReleaseContext(handle, 0);
                }
            }
        }

        [LibraryImport("Crypt32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CryptAcquireCertificatePrivateKey(
            nint pCert,
            int dwFlags,
            nint pvParameters,
            out nint phCryptProvOrNCryptKey,
            out int pdwKeySpec,
            [MarshalAs(UnmanagedType.Bool)] out bool pfCallerFreeProvOrNCryptKey);

        internal static bool CryptAcquireCertificatePrivateKey(
            nint cert,
            int flags,
            nint parameters,
            out SafeCryptoPrivateKeyHandle keyHandle,
            out int keySpec)
        {
            bool res = CryptAcquireCertificatePrivateKey(
                cert,
                flags,
                parameters,
                out nint key,
                out keySpec,
                out bool shouldFree);

            if (res)
            {
                keyHandle = new SafeCryptoPrivateKeyHandle(
                    key,
                    (keySpec & CERT_NCRYPT_KEY_SPEC) == CERT_NCRYPT_KEY_SPEC,
                    shouldFree,
                    true);
            }
            else
            {
                keyHandle = new SafeCryptoPrivateKeyHandle(nint.Zero, false, false, false);
            }

            return res;
        }
    }
}
