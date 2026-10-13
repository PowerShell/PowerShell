// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// Represents the OLE struct PROPVARIANT.
    /// This class is intended for internal use only.
    /// </summary>
    /// <remarks>
    /// Originally sourced from https://blogs.msdn.com/adamroot/pages/interop-with-propvariants-in-net.aspx
    /// and modified to add ability to set values.
    /// The layout matches the native PROPVARIANT structure (24 bytes on 64-bit, 16 bytes on 32-bit), as instances are
    /// passed directly to native code such as PropVariantClear, which reads and writes the whole native structure.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit)]
    internal sealed class PropVariant : IDisposable
    {
        // This is actually a VarEnum value, but the VarEnum type requires 4 bytes instead of the expected 2.
        [FieldOffset(0)]
        private readonly ushort _vt;

        [FieldOffset(2)]
        private readonly ushort _wReserved1;

        [FieldOffset(4)]
        private readonly ushort _wReserved2;

        [FieldOffset(6)]
        private readonly ushort _wReserved3;

        [FieldOffset(8)]
        private readonly ValueUnion _value;

        /// <summary>
        /// Set a string value.
        /// </summary>
        internal PropVariant(string value)
        {
            if (value == null)
            {
                throw new ArgumentException("PropVariantNullString", nameof(value));
            }

            _vt = (ushort)VarEnum.VT_LPWSTR;
            _value.PwszVal = Marshal.StringToCoTaskMemUni(value);
        }

        /// <summary>
        /// Disposes the object, calls the clear function.
        /// </summary>
        public void Dispose()
        {
            PropVariantNativeMethods.PropVariantClear(this);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="PropVariant"/> class.
        /// </summary>
        ~PropVariant()
        {
            Dispose();
        }

        /// <summary>
        /// The union of the value members of the native PROPVARIANT structure.
        /// Only the members that are needed to reproduce the native size and the members that are used are declared,
        /// and they are named after the native members, with the first letter capitalized.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct ValueUnion
        {
            // Covers all of the pointer and (up to) 8 byte scalar members, such as pwszVal, pszVal, punkVal and hVal.
            [FieldOffset(0)]
            public IntPtr PwszVal;

            // The counted array members (CAUB, CAI, CALPWSTR and so on) are the largest members of the union,
            // so this is what gives the native structure its full size.
            [FieldOffset(0)]
            public CountedArray Ca;
        }

        /// <summary>
        /// The layout shared by all of the counted array members (CAUB, CAI, CALPWSTR and so on) of the native union.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct CountedArray
        {
            // The cElems member of the native structure.
            public uint CElems;

            // The pElems member of the native structure, which is aligned to the pointer size,
            // so is at offset 8 on 64-bit and offset 4 on 32-bit.
            public IntPtr PElems;
        }

        private static class PropVariantNativeMethods
        {
            [DllImport("Ole32.dll", PreserveSig = false)]
            internal static extern void PropVariantClear([In, Out] PropVariant pvar);
        }
    }
}
