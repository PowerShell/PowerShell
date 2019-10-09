// Copyright (c) Microsoft Corporation. All rights reserved.
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
        ushort _valueType;

        [FieldOffset(8)]
        IntPtr _ptr;

        /// <summary>
        /// Set a string value.
        /// </summary>
        internal PropVariant(string value)
        {
            if (value == null)
            {
                throw new ArgumentException("PropVariantNullString", "value");
            }

#pragma warning disable CS0618 // Type or member is obsolete (might get deprecated in future versions
            _valueType = (ushort)VarEnum.VT_LPWSTR;
#pragma warning restore CS0618 // Type or member is obsolete (might get deprecated in future versions
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
        /// Finalizer.
        /// </summary>
        ~PropVariant()
        {
            Dispose();
        }

        private class PropVariantNativeMethods
        {
            [DllImport("Ole32.dll", PreserveSig = false)]
            internal static extern void PropVariantClear([In, Out] PropVariant pvar);
        }
    }
}
