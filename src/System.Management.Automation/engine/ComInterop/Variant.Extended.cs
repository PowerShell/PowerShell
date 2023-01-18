// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using System.Management.Automation.ComInterop;

namespace System.Management.Automation.InteropServices
{
    internal partial struct Variant
    {
        // VT_I1

        public void SetAsByrefI1(ref sbyte value)
        {
            SetAsByref(ref value, VarEnum.VT_I1);
        }

        // VT_I2

        public void SetAsByrefI2(ref short value)
        {
            SetAsByref(ref value, VarEnum.VT_I2);
        }

        // VT_I4

        public void SetAsByrefI4(ref int value)
        {
            SetAsByref(ref value, VarEnum.VT_I4);
        }

        // VT_I8

        public void SetAsByrefI8(ref long value)
        {
            SetAsByref(ref value, VarEnum.VT_I8);
        }

        // VT_UI1

        public void SetAsByrefUi1(ref byte value)
        {
            SetAsByref(ref value, VarEnum.VT_UI1);
        }

        // VT_UI2

        public void SetAsByrefUi2(ref ushort value)
        {
            SetAsByref(ref value, VarEnum.VT_UI2);
        }

        // VT_UI4

        public void SetAsByrefUi4(ref uint value)
        {
            SetAsByref(ref value, VarEnum.VT_UI4);
        }

        // VT_UI8

        public void SetAsByrefUi8(ref ulong value)
        {
            SetAsByref(ref value, VarEnum.VT_UI8);
        }

        // VT_INT

        public void SetAsByrefInt(ref int value)
        {
            SetAsByref(ref value, VarEnum.VT_INT);
        }

        // VT_UINT

        public void SetAsByrefUint(ref uint value)
        {
            SetAsByref(ref value, VarEnum.VT_UINT);
        }

        // VT_BOOL

        public void SetAsByrefBool(ref short value)
        {
            SetAsByref(ref value, VarEnum.VT_BOOL);
        }

        // VT_ERROR

        public void SetAsByrefError(ref int value)
        {
            SetAsByref(ref value, VarEnum.VT_ERROR);
        }

        // VT_R4

        public void SetAsByrefR4(ref float value)
        {
            SetAsByref(ref value, VarEnum.VT_R4);
        }

        // VT_R8

        public void SetAsByrefR8(ref double value)
        {
            SetAsByref(ref value, VarEnum.VT_R8);
        }

        // VT_DECIMAL

        public void SetAsByrefDecimal(ref decimal value)
        {
            SetAsByref(ref value, VarEnum.VT_DECIMAL);
        }

        // VT_CY

        public void SetAsByrefCy(ref long value)
        {
            SetAsByref(ref value, VarEnum.VT_CY);
        }

        // VT_DATE

        public void SetAsByrefDate(ref double value)
        {
            SetAsByref(ref value, VarEnum.VT_DATE);
        }

        // VT_BSTR

        public void SetAsByrefBstr(ref IntPtr value)
        {
            SetAsByref(ref value, VarEnum.VT_BSTR);
        }

        // VT_UNKNOWN

        public void SetAsByrefUnknown(ref IntPtr value)
        {
            SetAsByref(ref value, VarEnum.VT_UNKNOWN);
        }

        // VT_DISPATCH

        public void SetAsByrefDispatch(ref IntPtr value)
        {
            SetAsByref(ref value, VarEnum.VT_DISPATCH);
        }

        // VT_VARIANT

        public object AsVariant
        {
            get
            {
                return Marshal.GetObjectForNativeVariant(UnsafeMethods.ConvertVariantByrefToPtr(ref this));
            }

            set
            {
                Debug.Assert(IsEmpty); // The setter can only be called once as VariantClear might be needed otherwise
                if (value != null)
                {
                    UnsafeMethods.InitVariantForObject(value, ref this);
                }
            }
        }

        public void SetAsByrefVariant(ref Variant value)
        {
            SetAsByref(ref value, VarEnum.VT_VARIANT);
        }

        // constructs a ByRef variant to pass contents of another variant ByRef.
        public unsafe void SetAsByrefVariantIndirect(ref Variant value)
        {
            Debug.Assert(IsEmpty); // The setter can only be called once as VariantClear might be needed otherwise
            Debug.Assert((value.VariantType & VarEnum.VT_BYREF) == 0, "double indirection");

            switch (value.VariantType)
            {
                case VarEnum.VT_EMPTY:
                case VarEnum.VT_NULL:
                    // these cannot combine with VT_BYREF. Should try passing as a variant reference
                    SetAsByrefVariant(ref value);
                    return;
                case VarEnum.VT_RECORD:
                    // VT_RECORD's are weird in that regardless of is the VT_BYREF flag is set or not
                    // they have the same internal representation.
                    _typeUnion._unionTypes._record = value._typeUnion._unionTypes._record;
                    break;
                case VarEnum.VT_DECIMAL:
                    _typeUnion._unionTypes._byref = (IntPtr)Unsafe.AsPointer(ref value._decimal);
                    break;
                default:
                    _typeUnion._unionTypes._byref = (IntPtr)Unsafe.AsPointer(ref value._typeUnion._unionTypes._byref);
                    break;
            }
            VariantType = (value.VariantType | VarEnum.VT_BYREF);
        }

        private unsafe void SetAsByref<T>(ref T value, VarEnum type)
        {
            Debug.Assert(IsEmpty); // The setter can only be called once as VariantClear might be needed otherwise
            VariantType = type | VarEnum.VT_BYREF;
            _typeUnion._unionTypes._byref = (IntPtr)Unsafe.AsPointer<T>(ref value);
        }

        internal static System.Reflection.PropertyInfo GetAccessor(VarEnum varType)
        {
            switch (varType)
            {
                case VarEnum.VT_I1: return typeof(Variant).GetProperty(nameof(AsI1));
                case VarEnum.VT_I2: return typeof(Variant).GetProperty(nameof(AsI2));
                case VarEnum.VT_I4: return typeof(Variant).GetProperty(nameof(AsI4));
                case VarEnum.VT_I8: return typeof(Variant).GetProperty(nameof(AsI8));
                case VarEnum.VT_UI1: return typeof(Variant).GetProperty(nameof(AsUi1));
                case VarEnum.VT_UI2: return typeof(Variant).GetProperty(nameof(AsUi2));
                case VarEnum.VT_UI4: return typeof(Variant).GetProperty(nameof(AsUi4));
                case VarEnum.VT_UI8: return typeof(Variant).GetProperty(nameof(AsUi8));
                case VarEnum.VT_INT: return typeof(Variant).GetProperty(nameof(AsInt));
                case VarEnum.VT_UINT: return typeof(Variant).GetProperty(nameof(AsUint));
                case VarEnum.VT_BOOL: return typeof(Variant).GetProperty(nameof(AsBool));
                case VarEnum.VT_ERROR: return typeof(Variant).GetProperty(nameof(AsError));
                case VarEnum.VT_R4: return typeof(Variant).GetProperty(nameof(AsR4));
                case VarEnum.VT_R8: return typeof(Variant).GetProperty(nameof(AsR8));
                case VarEnum.VT_DECIMAL: return typeof(Variant).GetProperty(nameof(AsDecimal));
                case VarEnum.VT_CY: return typeof(Variant).GetProperty(nameof(AsCy));
                case VarEnum.VT_DATE: return typeof(Variant).GetProperty(nameof(AsDate));
                case VarEnum.VT_BSTR: return typeof(Variant).GetProperty(nameof(AsBstr));
                case VarEnum.VT_UNKNOWN: return typeof(Variant).GetProperty(nameof(AsUnknown));
                case VarEnum.VT_DISPATCH: return typeof(Variant).GetProperty(nameof(AsDispatch));

                case VarEnum.VT_VARIANT:
                case VarEnum.VT_RECORD:
                case VarEnum.VT_ARRAY:
                    return typeof(Variant).GetProperty(nameof(AsVariant));

                default:
                    throw new NotSupportedException();
            }
        }

        internal static System.Reflection.MethodInfo GetByrefSetter(VarEnum varType)
        {
            switch (varType)
            {
                case VarEnum.VT_I1: return typeof(Variant).GetMethod(nameof(SetAsByrefI1));
                case VarEnum.VT_I2: return typeof(Variant).GetMethod(nameof(SetAsByrefI2));
                case VarEnum.VT_I4: return typeof(Variant).GetMethod(nameof(SetAsByrefI4));
                case VarEnum.VT_I8: return typeof(Variant).GetMethod(nameof(SetAsByrefI8));
                case VarEnum.VT_UI1: return typeof(Variant).GetMethod(nameof(SetAsByrefUi1));
                case VarEnum.VT_UI2: return typeof(Variant).GetMethod(nameof(SetAsByrefUi2));
                case VarEnum.VT_UI4: return typeof(Variant).GetMethod(nameof(SetAsByrefUi4));
                case VarEnum.VT_UI8: return typeof(Variant).GetMethod(nameof(SetAsByrefUi8));
                case VarEnum.VT_INT: return typeof(Variant).GetMethod(nameof(SetAsByrefInt));
                case VarEnum.VT_UINT: return typeof(Variant).GetMethod(nameof(SetAsByrefUint));
                case VarEnum.VT_BOOL: return typeof(Variant).GetMethod(nameof(SetAsByrefBool));
                case VarEnum.VT_ERROR: return typeof(Variant).GetMethod(nameof(SetAsByrefError));
                case VarEnum.VT_R4: return typeof(Variant).GetMethod(nameof(SetAsByrefR4));
                case VarEnum.VT_R8: return typeof(Variant).GetMethod(nameof(SetAsByrefR8));
                case VarEnum.VT_DECIMAL: return typeof(Variant).GetMethod(nameof(SetAsByrefDecimal));
                case VarEnum.VT_CY: return typeof(Variant).GetMethod(nameof(SetAsByrefCy));
                case VarEnum.VT_DATE: return typeof(Variant).GetMethod(nameof(SetAsByrefDate));
                case VarEnum.VT_BSTR: return typeof(Variant).GetMethod(nameof(SetAsByrefBstr));
                case VarEnum.VT_UNKNOWN: return typeof(Variant).GetMethod(nameof(SetAsByrefUnknown));
                case VarEnum.VT_DISPATCH: return typeof(Variant).GetMethod(nameof(SetAsByrefDispatch));

                case VarEnum.VT_VARIANT:
                    return typeof(Variant).GetMethod(nameof(SetAsByrefVariant));
                case VarEnum.VT_RECORD:
                case VarEnum.VT_ARRAY:
                    return typeof(Variant).GetMethod(nameof(SetAsByrefVariantIndirect));

                default:
                    throw new NotSupportedException();
            }
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, $"Variant ({VariantType})");
        }

        public void SetAsIConvertible(IConvertible value)
        {
            Debug.Assert(IsEmpty); // The setter can only be called once as VariantClear might be needed otherwise

            TypeCode tc = value.GetTypeCode();
            CultureInfo ci = CultureInfo.CurrentCulture;

            switch (tc)
            {
                case TypeCode.Empty: break;
                case TypeCode.Object: AsUnknown = value; break;
                case TypeCode.DBNull: SetAsNULL(); break;
                case TypeCode.Boolean: AsBool = value.ToBoolean(ci); break;
                case TypeCode.Char: AsUi2 = value.ToChar(ci); break;
                case TypeCode.SByte: AsI1 = value.ToSByte(ci); break;
                case TypeCode.Byte: AsUi1 = value.ToByte(ci); break;
                case TypeCode.Int16: AsI2 = value.ToInt16(ci); break;
                case TypeCode.UInt16: AsUi2 = value.ToUInt16(ci); break;
                case TypeCode.Int32: AsI4 = value.ToInt32(ci); break;
                case TypeCode.UInt32: AsUi4 = value.ToUInt32(ci); break;
                case TypeCode.Int64: AsI8 = value.ToInt64(ci); break;
                case TypeCode.UInt64: AsI8 = value.ToInt64(ci); break;
                case TypeCode.Single: AsR4 = value.ToSingle(ci); break;
                case TypeCode.Double: AsR8 = value.ToDouble(ci); break;
                case TypeCode.Decimal: AsDecimal = value.ToDecimal(ci); break;
                case TypeCode.DateTime: AsDate = value.ToDateTime(ci); break;
                case TypeCode.String: AsBstr = value.ToString(ci); break;

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
