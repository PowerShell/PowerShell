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
        [LibraryImport("Advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CryptSetProvParam(
            SafeHandle hProv,
            int dwParam,
            ReadOnlySpan<byte> pbData,
            int dwFlags);
    }
}
