// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Keep native method argument names.")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:AccessibleFieldsMustBeginWithUpperCaseLetter", Justification = "Keep native method argument names.")]
    internal static unsafe partial class Windows
    {
        internal const int PP_KEYSET_SEC_DESCR = 8;

        [LibraryImport("Advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CryptGetProvParam(
            SafeHandle hProv,
            int dwParam,
            Span<byte> pbData,
            ref int pdwDataLen,
            int dwFlags);
    }
}
