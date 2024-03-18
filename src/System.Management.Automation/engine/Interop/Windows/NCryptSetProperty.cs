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
        [LibraryImport("Ncrypt.dll", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int NCryptSetProperty(
            SafeHandle hObject,
            string pszProperty,
            ReadOnlySpan<byte> pbInput,
            int cbInput,
            int dwFlags);
    }
}
