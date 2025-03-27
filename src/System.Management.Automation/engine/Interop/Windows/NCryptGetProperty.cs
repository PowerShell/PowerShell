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
        internal const string NCRYPT_SECURITY_DESCR_PROPERTY = "Security Descr";

        [LibraryImport("Ncrypt.dll", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int NCryptGetProperty(
            SafeHandle hObject,
            string pszProperty,
            Span<byte> pbOutput,
            int cbOutput,
            out int pcbResult,
            int dwFlags);
    }
}
