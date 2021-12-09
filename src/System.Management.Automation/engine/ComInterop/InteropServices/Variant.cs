// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Management.Automation.InteropServices
{
    /// <summary>
    /// Variant is the basic COM type for late-binding. It can contain any other COM data type.
    /// This type definition precisely matches the unmanaged data layout so that the struct can be passed
    /// to and from COM calls.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal partial struct Variant
    {
#if DEBUG
        static Variant()
        {
            // Variant size is the size of 4 pointers (16 bytes) on a 32-bit processor,
            // and 3 pointers (24 bytes) on a 64-bit processor.
            int variantSize = Marshal.SizeOf(typeof(Variant));
            if (IntPtr.Size == 4)
            {
                Debug.Assert(variantSize == (4 * IntPtr.Size));
            }
            else
            {
                Debug.Assert(IntPtr.Size == 8);
                Debug.Assert(variantSize == (3 * IntPtr.Size));
            }
        }
#endif

        // Most of the data types in the Variant are carried in _typeUnion
        [FieldOffset(0)] private TypeUnion _typeUnion;

        // Decimal is the largest data type and it needs to use the space that is normally unused in TypeUnion._wReserved1, etc.
        // Hence, it is declared to completely overlap with TypeUnion. A Decimal does not use the first two bytes, and so
        // TypeUnion._vt can still be used to encode the type.
        [FieldOffset(0)] private decimal _decimal;

        [StructLayout(LayoutKind.Sequential)]
        private struct TypeUnion
        {
            public ushort _vt;
            public ushort _wReserved1;
            public ushort _wReserved2;
            public ushort _wReserved3;

            public UnionTypes _unionTypes;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Record
        {
            public IntPtr _record;
            public IntPtr _recordInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct UnionTypes
        {
            [FieldOffset(0)] public sbyte _i1;
            [FieldOffset(0)] public short _i2;
            [FieldOffset(0)] public int _i4;
            [FieldOffset(0)] public long _i8;
            [FieldOffset(0)] public byte _ui1;
            [FieldOffset(0)] public ushort _ui2;
            [FieldOffset(0)] public uint _ui4;
            [FieldOffset(0)] public ulong _ui8;
            [FieldOffset(0)] public int _int;
            [FieldOffset(0)] public uint _uint;
            [FieldOffset(0)] public short _bool;
            [FieldOffset(0)] public int _error;
            [FieldOffset(0)] public float _r4;
            [FieldOffset(0)] public double _r8;
            [FieldOffset(0)] public long _cy;
            [FieldOffset(0)] public double _date;
            [FieldOffset(0)] public IntPtr _bstr;
            [FieldOffset(0)] public IntPtr _unknown;
            [FieldOffset(0)] public IntPtr _dispatch;
            [FieldOffset(0)] public IntPtr _pvarVal;
            [FieldOffset(0)] public IntPtr _byref;
            [FieldOffset(0)] public Record _record;
        }

        /// <summary>
        /// Primitive types are the basic COM types. It includes valuetypes like ints, but also reference types
        /// like BStrs. It does not include composite types like arrays and user-defined COM types (IUnknown/IDispatch).
        /// </summary>
        public static bool IsPrimitiveType(VarEnum varEnum)
        {
            switch (varEnum)
            {
                case VarEnum.VT_I1:
                case VarEnum.VT_I2:
                case VarEnum.VT_I4:
                case VarEnum.VT_I8:
                case VarEnum.VT_UI1:
                case VarEnum.VT_UI2:
                case VarEnum.VT_UI4:
                case VarEnum.VT_UI8:
                case VarEnum.VT_INT:
                case VarEnum.VT_UINT:
                case VarEnum.VT_BOOL:
                case VarEnum.VT_ERROR:
                case VarEnum.VT_R4:
                case VarEnum.VT_R8:
                case VarEnum.VT_DECIMAL:
                case VarEnum.VT_CY:
                case VarEnum.VT_DATE:
                case VarEnum.VT_BSTR:
                    return true;
            }

            return false;
        }

        public unsafe void CopyFromIndirect(object value)
        {
            VarEnum vt = (VarEnum)(((int)this.VariantType) & ~((int)VarEnum.VT_BYREF));

            if (value == null)
            {
                if (vt == VarEnum.VT_DISPATCH || vt == VarEnum.VT_UNKNOWN || vt == VarEnum.VT_BSTR)
                {
                    *(IntPtr*)this._typeUnion._unionTypes._byref = IntPtr.Zero;
                }
                return;
            }

            if ((vt & VarEnum.VT_ARRAY) != 0)
            {
                Variant vArray;
                Marshal.GetNativeVariantForObject(value, (IntPtr)(void*)&vArray);
                *(IntPtr*)this._typeUnion._unionTypes._byref = vArray._typeUnion._unionTypes._byref;
                return;
            }

            switch (vt)
            {
                case VarEnum.VT_I1:
                    *(sbyte*)this._typeUnion._unionTypes._byref = (sbyte)value;
                    break;

                case VarEnum.VT_UI1:
                    *(byte*)this._typeUnion._unionTypes._byref = (byte)value;
                    break;

                case VarEnum.VT_I2:
                    *(short*)this._typeUnion._unionTypes._byref = (short)value;
                    break;

                case VarEnum.VT_UI2:
                    *(ushort*)this._typeUnion._unionTypes._byref = (ushort)value;
                    break;

                case VarEnum.VT_BOOL:
                    // VARIANT_TRUE  = -1
                    // VARIANT_FALSE = 0
                    *(short*)this._typeUnion._unionTypes._byref = (bool)value ? (short)-1 : (short)0;
                    break;

                case VarEnum.VT_I4:
                case VarEnum.VT_INT:
                    *(int*)this._typeUnion._unionTypes._byref = (int)value;
                    break;

                case VarEnum.VT_UI4:
                case VarEnum.VT_UINT:
                    *(uint*)this._typeUnion._unionTypes._byref = (uint)value;
                    break;

                case VarEnum.VT_ERROR:
                    *(int*)this._typeUnion._unionTypes._byref = ((ErrorWrapper)value).ErrorCode;
                    break;

                case VarEnum.VT_I8:
                    *(long*)this._typeUnion._unionTypes._byref = (long)value;
                    break;

                case VarEnum.VT_UI8:
                    *(ulong*)this._typeUnion._unionTypes._byref = (ulong)value;
                    break;

                case VarEnum.VT_R4:
                    *(float*)this._typeUnion._unionTypes._byref = (float)value;
                    break;

                case VarEnum.VT_R8:
                    *(double*)this._typeUnion._unionTypes._byref = (double)value;
                    break;

                case VarEnum.VT_DATE:
                    *(double*)this._typeUnion._unionTypes._byref = ((DateTime)value).ToOADate();
                    break;

                case VarEnum.VT_UNKNOWN:
                    *(IntPtr*)this._typeUnion._unionTypes._byref = Marshal.GetIUnknownForObject(value);
                    break;

                case VarEnum.VT_DISPATCH:
                    *(IntPtr*)this._typeUnion._unionTypes._byref = Marshal.GetComInterfaceForObject<object, IDispatch>(value);
                    break;

                case VarEnum.VT_BSTR:
                    *(IntPtr*)this._typeUnion._unionTypes._byref = Marshal.StringToBSTR((string)value);
                    break;

                case VarEnum.VT_CY:
                    *(long*)this._typeUnion._unionTypes._byref = decimal.ToOACurrency((decimal)value);
                    break;

                case VarEnum.VT_DECIMAL:
                    *(decimal*)this._typeUnion._unionTypes._byref = (decimal)value;
                    break;

                case VarEnum.VT_VARIANT:
                    Marshal.GetNativeVariantForObject(value, this._typeUnion._unionTypes._byref);
                    break;

                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>
        /// Get the managed object representing the Variant.
        /// </summary>
        /// <returns></returns>
        public object? ToObject()
        {
            // Check the simple case upfront
            if (IsEmpty)
            {
                return null;
            }

            switch (VariantType)
            {
                case VarEnum.VT_NULL:
                    return DBNull.Value;

                case VarEnum.VT_I1: return AsI1;
                case VarEnum.VT_I2: return AsI2;
                case VarEnum.VT_I4: return AsI4;
                case VarEnum.VT_I8: return AsI8;
                case VarEnum.VT_UI1: return AsUi1;
                case VarEnum.VT_UI2: return AsUi2;
                case VarEnum.VT_UI4: return AsUi4;
                case VarEnum.VT_UI8: return AsUi8;
                case VarEnum.VT_INT: return AsInt;
                case VarEnum.VT_UINT: return AsUint;
                case VarEnum.VT_BOOL: return AsBool;
                case VarEnum.VT_ERROR: return AsError;
                case VarEnum.VT_R4: return AsR4;
                case VarEnum.VT_R8: return AsR8;
                case VarEnum.VT_DECIMAL: return AsDecimal;
                case VarEnum.VT_CY: return AsCy;
                case VarEnum.VT_DATE: return AsDate;
                case VarEnum.VT_BSTR: return AsBstr;
                case VarEnum.VT_UNKNOWN: return AsUnknown;
                case VarEnum.VT_DISPATCH: return AsDispatch;

                default:
                    unsafe
                    {
                        fixed (void* pThis = &this)
                        {
                            return Marshal.GetObjectForNativeVariant((System.IntPtr)pThis);
                        }
                    }
            }
        }

        [DllImport("oleaut32.dll")]
        internal static extern void VariantClear(IntPtr variant);

        /// <summary>
        /// Release any unmanaged memory associated with the Variant
        /// </summary>
        public void Clear()
        {
            // We do not need to call OLE32's VariantClear for primitive types or ByRefs
            // to save ourselves the cost of interop transition.
            // ByRef indicates the memory is not owned by the VARIANT itself while
            // primitive types do not have any resources to free up.
            // Hence, only safearrays, BSTRs, interfaces and user types are
            // handled differently.
            VarEnum vt = VariantType;
            if ((vt & VarEnum.VT_BYREF) != 0)
            {
                VariantType = VarEnum.VT_EMPTY;
            }
            else if (((vt & VarEnum.VT_ARRAY) != 0)
                    || (vt == VarEnum.VT_BSTR)
                    || (vt == VarEnum.VT_UNKNOWN)
                    || (vt == VarEnum.VT_DISPATCH)
                    || (vt == VarEnum.VT_VARIANT)
                    || (vt == VarEnum.VT_RECORD))
            {
                unsafe
                {
                    fixed (void* pThis = &this)
                    {
                        VariantClear((IntPtr)pThis);
                    }
                }

                Debug.Assert(IsEmpty);
            }
            else
            {
                VariantType = VarEnum.VT_EMPTY;
            }
        }

        public VarEnum VariantType
        {
            get => (VarEnum)_typeUnion._vt;
            set => _typeUnion._vt = (ushort)value;
        }

        public bool IsEmpty => _typeUnion._vt == ((ushort)VarEnum.VT_EMPTY);

        public bool IsByRef => (_typeUnion._vt & ((ushort)VarEnum.VT_BYREF)) != 0;

        public void SetAsNULL()
        {
            Debug.Assert(IsEmpty);
            VariantType = VarEnum.VT_NULL;
        }

        // VT_I1

        public sbyte AsI1
        {
            get
            {
                Debug.Assert(VariantType == VarEnum.VT_I1);
                return _typeUnion._unionTypes._i1;
            }

            set
            {
                Debug.Assert(IsEmpty);
                VariantType = VarEnum.VT_I1;
                _typeUnion._unionTypes._i1 = value;
            }
        }

        // VT_I2

        public short AsI2
        {
            get
            {
                Debug.Assert(VariantType == VarEnum.VT_I2);
                return _typeUnion._unionTypes._i2;
            }

            set
            {
                Debug.Assert(IsEmpty);
                VariantType = VarEnum.VT_I2;
                _typeUnion._unionTypes._i2 = value;
            }
        }

        // VT_I4

        public int AsI4
        {
            get
            {
                Debug.Assert(VariantType == VarEnum.VT_I4);
                return _typeUnion._unionTypes._i4;
            }

            set
            {
                Debug.Assert(IsEmpty);
                VariantType = VarEnum.VT_I4;
                _typeUnion._unionTypes._i4 = value;
            }
        }

        // VT_I8

        public long AsI8
        {
            get
            {
                Debug.Assert(VariantType == VarEnum.VT_I8);
                return _typeUnion._unionTypes._i8;
            }

            set
            {
                Debug.Assert(IsEmpty);
                VariantType = VarEnum.VT_I8;
                _typeUnion._unionTypes._i8 = value;
            }
        }

        // VT_UI1

        public byte AsUi1
        {
            get
            {
                Debug.Assert(VariantType == VarEnum.VT_UI1);
                return _typeUnion._unionTypes._ui1;
            }

            set
            {
                Debug.Assert(IsEmpty);
                VariantType = VarEnum.VT_UI1;
                _typeUnion._unionTypes._ui1 = value;
            }
        }

        // VT_UI2

        public ushort AsUi2
        {
            get
            {
                Debug.Assert(VariantType == VarEnum.VT_UI2);
                return _typeUnion._unionTypes._ui2;
            }

            set
            {
                Debug.Assert(IsEmpty);
                VariantType = VarEnum.VT_UI2;
                _typeUnion._unionTypes._ui2 = value;
            }
        }

        // VT_UI4

        public uint AsUi4
        {
            get
            {
                Debug.Assert(VariantType == VarEnum.VT_UI4);
                return _typeUnion._unionTypes._ui4;
            }

            set
            {
                Debug.Assert(IsEmpty);
                VariantType = VarEnum.VT_UI4;
                _typeUnion._unionTypes._ui4 = value;
            }
        }

        // VT_UI8

        public ulong AsUi8
        {
            get
            {
                Debug.Assert(VariantType == VarEnum.VT_UI8);
                return _typeUnion._unionTypes._ui8;
            }

            set
            {
                Debug.Assert(IsEmpty);
                VariantType = VarEnum.VT_UI8;
                _typeUnion._unionTypes._ui8 = value;
            }
        }

        // VT_INT

        public int AsInt
        {
            get
            {
                Debug.Assert(VariantType == VarEnum.VT_INT);
                return _typeUnion._unionTypes._int;
            }

            set
            {
                Debug.Assert(IsEmpty);
                VariantType = VarEnum.VT_INT;
                _typeUnion._unionTypes._int = value;
            }
        }

        // VT_UINT

        public uint AsUint
        {
            get
            {
                Debug.Assert(VariantType == VarEnum.VT_UINT);
                return _typeUnion._unionTypes._uint;
            }

            set
            {
                Debug.Assert(IsEmpty);
                VariantType = VarEnum.VT_UINT;
                _typeUnion._unionTypes._uint = value;
            }
        }

        // VT_BOOL

        public bool AsBool
        {
            get
            {
                Debug.Assert(VariantType == VarEnum.VT_BOOL);
                return _typeUnion._unionTypes._bool != 0;
            }

            set
            {
                Debug.Assert(IsEmpty);
                // VARIANT_TRUE  = -1
                // VARIANT_FALSE = 0
                VariantType = VarEnum.VT_BOOL;
                _typeUnion._unionTypes._bool = value ? (short)-1 : (short)0;
            }
        }

        // VT_ERROR

        public int AsError
        {
            get
            {
                Debug.Assert(VariantType == VarEnum.VT_ERROR);
                return _typeUnion._unionTypes._error;
            }

            set
            {
                Debug.Assert(IsEmpty);
                VariantType = VarEnum.VT_ERROR;
                _typeUnion._unionTypes._error = value;
            }
        }

        // VT_R4

        public float AsR4
        {
            get
            {
                Debug.Assert(VariantType == VarEnum.VT_R4);
                return _typeUnion._unionTypes._r4;
            }

            set
            {
                Debug.Assert(IsEmpty);
                VariantType = VarEnum.VT_R4;
                _typeUnion._unionTypes._r4 = value;
            }
        }

        // VT_R8

        public double AsR8
        {
            get
            {
                Debug.Assert(VariantType == VarEnum.VT_R8);
                return _typeUnion._unionTypes._r8;
            }

            set
            {
                Debug.Assert(IsEmpty);
                VariantType = VarEnum.VT_R8;
                _typeUnion._unionTypes._r8 = value;
            }
        }

        // VT_DECIMAL

        public decimal AsDecimal
        {
            get
            {
                Debug.Assert(VariantType == VarEnum.VT_DECIMAL);
                // The first byte of Decimal is unused, but usually set to 0
                Variant v = this;
                v._typeUnion._vt = 0;
                return v._decimal;
            }

            set
            {
                Debug.Assert(IsEmpty);
                VariantType = VarEnum.VT_DECIMAL;
                _decimal = value;
                // _vt overlaps with _decimal, and should be set after setting _decimal
                _typeUnion._vt = (ushort)VarEnum.VT_DECIMAL;
            }
        }

        // VT_CY

        public decimal AsCy
        {
            get
            {
                Debug.Assert(VariantType == VarEnum.VT_CY);
                return decimal.FromOACurrency(_typeUnion._unionTypes._cy);
            }

            set
            {
                Debug.Assert(IsEmpty);
                VariantType = VarEnum.VT_CY;
                _typeUnion._unionTypes._cy = decimal.ToOACurrency(value);
            }
        }

        // VT_DATE

        public DateTime AsDate
        {
            get
            {
                Debug.Assert(VariantType == VarEnum.VT_DATE);
                return DateTime.FromOADate(_typeUnion._unionTypes._date);
            }

            set
            {
                Debug.Assert(IsEmpty);
                VariantType = VarEnum.VT_DATE;
                _typeUnion._unionTypes._date = value.ToOADate();
            }
        }

        // VT_BSTR

        public string AsBstr
        {
            get
            {
                Debug.Assert(VariantType == VarEnum.VT_BSTR);
                return (string)Marshal.PtrToStringBSTR(this._typeUnion._unionTypes._bstr);
            }

            set
            {
                Debug.Assert(IsEmpty);
                VariantType = VarEnum.VT_BSTR;
                this._typeUnion._unionTypes._bstr = Marshal.StringToBSTR(value);
            }
        }

        // VT_UNKNOWN

        public object? AsUnknown
        {
            get
            {
                Debug.Assert(VariantType == VarEnum.VT_UNKNOWN);
                if (_typeUnion._unionTypes._unknown == IntPtr.Zero)
                {
                    return null;
                }
                return Marshal.GetObjectForIUnknown(_typeUnion._unionTypes._unknown);
            }

            set
            {
                Debug.Assert(IsEmpty);
                VariantType = VarEnum.VT_UNKNOWN;
                if (value == null)
                {
                    _typeUnion._unionTypes._unknown = IntPtr.Zero;
                }
                else
                {
                    _typeUnion._unionTypes._unknown = Marshal.GetIUnknownForObject(value);
                }
            }
        }

        // VT_DISPATCH

        public object? AsDispatch
        {
            get
            {
                Debug.Assert(VariantType == VarEnum.VT_DISPATCH);
                if (_typeUnion._unionTypes._dispatch == IntPtr.Zero)
                {
                    return null;
                }
                return Marshal.GetObjectForIUnknown(_typeUnion._unionTypes._dispatch);
            }

            set
            {
                Debug.Assert(IsEmpty);
                VariantType = VarEnum.VT_DISPATCH;
                if (value == null)
                {
                    _typeUnion._unionTypes._dispatch = IntPtr.Zero;
                }
                else
                {
                    _typeUnion._unionTypes._dispatch = Marshal.GetComInterfaceForObject<object, IDispatch>(value);
                }
            }
        }

        public IntPtr AsByRefVariant
        {
            get
            {
                Debug.Assert(VariantType == (VarEnum.VT_BYREF | VarEnum.VT_VARIANT));
                return _typeUnion._unionTypes._pvarVal;
            }
        }
    }
}
