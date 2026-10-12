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
    /// and modified to add ability to set values
    /// </remarks>
    [StructLayout(LayoutKind.Explicit)]
    internal sealed class PropVariant : IDisposable
    {
        // This is actually a VarEnum value, but the VarEnum type requires 4 bytes instead of the expected 2.
        [FieldOffset(0)]
        private readonly ushort _valueType;

        [FieldOffset(8)]
        private readonly IntPtr _ptr;

        // The native PROPVARIANT is 24 bytes on 64-bit (16 bytes on 32-bit), but only the fields above are used.
        // This instance is passed to PropVariantClear, which zeroes the whole native structure, so the managed object
        // must be at least as large as it. Otherwise the clear overwrites the header of the next object on the managed
        // heap, which corrupts the sync block index or hash code stored in it (this crashed the runtime for COM objects).
        // StructLayoutAttribute.Size has no effect on the size of a managed class, so a field is needed to occupy the space.
#pragma warning disable CS0169 // The field is never used
        [FieldOffset(16)]
        private readonly IntPtr _padding;
#pragma warning restore CS0169

        /// <summary>
        /// Set a string value.
        /// </summary>
        internal PropVariant(string value)
        {
            if (value == null)
            {
                throw new ArgumentException("PropVariantNullString", nameof(value));
            }

            _valueType = (ushort)VarEnum.VT_LPWSTR;
            _ptr = Marshal.StringToCoTaskMemUni(value);
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

        private static class PropVariantNativeMethods
        {
            [DllImport("Ole32.dll", PreserveSig = false)]
            internal static extern void PropVariantClear([In, Out] PropVariant pvar);
        }
    }
}
