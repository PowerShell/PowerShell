// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Keep native meth arg names.")]
    internal static unsafe partial class Windows
    {
        [LibraryImport("Crypt32.dll", EntryPoint = "CertEnumSystemStore", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool CertEnumSystemStore(
            uint dwFlags,
            string? pvSystemStoreLocation,
            nint pvArg,
            PfnCertEnumSystemStore pfnEnum);

        internal delegate bool PfnCertEnumSystemStore(
            [MarshalAs(UnmanagedType.LPWStr)] string storeName,
            uint dwFlags,
            nint pStoreInfo,
            nint pvReserved,
            nint pvArg);
    }
}
