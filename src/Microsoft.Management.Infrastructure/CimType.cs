/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Management.Infrastructure
{
    /// <summary>
    /// <para>
    /// CIM type of a value.
    /// </para>
    /// <para>
    /// This is a .NET representation of intrinsic CIM types (as defined by DSP0004).  
    /// 
    /// The mapping of scalar types is as follows:
    /// - CIM: uint8 -> .NET: System.Byte
    /// - CIM: sint8 -> .NET: System.SByte
    /// - CIM: uint16 -> .NET: System.UInt16
    /// - CIM: sint16 -> .NET: System.Int16
    /// - CIM: uint32 -> .NET: System.UInt32
    /// - CIM: sint32 -> .NET: System.Int32
    /// - CIM: uint64 -> .NET: System.UInt64
    /// - CIM: sint64 -> .NET: System.Int64
    /// - CIM: string -> .NET: System.String
    /// - CIM: boolean -> .NET: System.Boolean
    /// - CIM: real32 -> .NET: System.Single
    /// - CIM: real64 -> .NET: System.Double
    /// - CIM: datetime -> .NET: either System.DateTime or System.TimeSpan
    /// - CIM: class ref -> .NET: CimInstance
    /// - CIM: char16 -> .NET: System.Char
    /// 
    /// The mapping of arrays uses a single-dimensional .NET array of an appropriate type.
    /// The only exception is the CIM: datetime[] -> .NET: System.Object[] mapping
    /// (which is necessary because the CIM array can contain a mixture of dates and intervals).
    /// </para>
    /// </summary>
    public enum CimType
    {
        Unknown = 0,

        Boolean,
        UInt8,
        SInt8,
        UInt16,
        SInt16,
        UInt32,
        SInt32,
        UInt64,
        SInt64,
        Real32,
        Real64,
        Char16,
        DateTime,
        String,
        Reference,
        Instance,

        BooleanArray,
        UInt8Array,
        SInt8Array,
        UInt16Array,
        SInt16Array,
        UInt32Array,
        SInt32Array,
        UInt64Array,
        SInt64Array,
        Real32Array,
        Real64Array,
        Char16Array,
        DateTimeArray,
        StringArray,
        ReferenceArray,
        InstanceArray,
    }
}

namespace Microsoft.Management.Infrastructure.Internal
{
    internal static class CimTypeExtensionMethods
    {
        public static Native.MiType ToMiType(this CimType cimType)
        {
            switch (cimType)
            {
                case CimType.Boolean: return Native.MiType.Boolean;
                case CimType.UInt8: return Native.MiType.UInt8;
                case CimType.SInt8: return Native.MiType.SInt8;
                case CimType.UInt16: return Native.MiType.UInt16;
                case CimType.SInt16: return Native.MiType.SInt16;
                case CimType.UInt32: return Native.MiType.UInt32;
                case CimType.SInt32: return Native.MiType.SInt32;
                case CimType.UInt64: return Native.MiType.UInt64;
                case CimType.SInt64: return Native.MiType.SInt64;
                case CimType.Real32: return Native.MiType.Real32;
                case CimType.Real64: return Native.MiType.Real64;
                case CimType.Char16: return Native.MiType.Char16;
                case CimType.DateTime: return Native.MiType.DateTime;
                case CimType.String: return Native.MiType.String;
                case CimType.Reference: return Native.MiType.Reference;
                case CimType.Instance: return Native.MiType.Instance;

                case CimType.BooleanArray: return Native.MiType.BooleanArray;
                case CimType.UInt8Array: return Native.MiType.UInt8Array;
                case CimType.SInt8Array: return Native.MiType.SInt8Array;
                case CimType.UInt16Array: return Native.MiType.UInt16Array;
                case CimType.SInt16Array: return Native.MiType.SInt16Array;
                case CimType.UInt32Array: return Native.MiType.UInt32Array;
                case CimType.SInt32Array: return Native.MiType.SInt32Array;
                case CimType.UInt64Array: return Native.MiType.UInt64Array;
                case CimType.SInt64Array: return Native.MiType.SInt64Array;
                case CimType.Real32Array: return Native.MiType.Real32Array;
                case CimType.Real64Array: return Native.MiType.Real64Array;
                case CimType.Char16Array: return Native.MiType.Char16Array;
                case CimType.DateTimeArray: return Native.MiType.DateTimeArray;
                case CimType.StringArray: return Native.MiType.StringArray;
                case CimType.ReferenceArray: return Native.MiType.ReferenceArray;
                case CimType.InstanceArray: return Native.MiType.InstanceArray;

                case CimType.Unknown:
                default:
                    Debug.Assert(false, "Unrecognized or unsupported value of CimType");
                    throw new ArgumentOutOfRangeException("cimType");
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "false positive.  this code is invoked from cimasyncmethodresultobserverproxy.cs")]
        public static Type ToDotNetType(this CimType cimType)
        {
            switch (cimType)
            {
                case CimType.Boolean:
                    return typeof(System.Boolean);
                case CimType.UInt8:
                    return typeof(System.Byte);
                case CimType.SInt8:
                    return typeof(System.SByte);
                case CimType.UInt16:
                    return typeof(System.UInt16);
                case CimType.SInt16:
                    return typeof(System.Int16);
                case CimType.UInt32:
                    return typeof(System.UInt32);
                case CimType.SInt32:
                    return typeof(System.Int32);
                case CimType.UInt64:
                    return typeof(System.UInt64);
                case CimType.SInt64:
                    return typeof(System.Int64);
                case CimType.Real32:
                    return typeof(System.Single);
                case CimType.Real64:
                    return typeof(System.Double);
                case CimType.Char16:
                    return typeof(System.Char);
                case CimType.DateTime:
                    return typeof(System.Object);
                case CimType.String:
                    return typeof(System.String);
                case CimType.Reference:
                case CimType.Instance:
                    return typeof(CimInstance);

                case CimType.BooleanArray:
                    return typeof(Boolean[]);
                case CimType.UInt8Array:
                    return typeof(Byte[]);
                case CimType.SInt8Array:
                    return typeof(SByte[]);
                case CimType.UInt16Array:
                    return typeof(UInt16[]);
                case CimType.SInt16Array:
                    return typeof(Int16[]);
                case CimType.UInt32Array:
                    return typeof(UInt32[]);
                case CimType.SInt32Array:
                    return typeof(Int32[]);
                case CimType.UInt64Array:
                    return typeof(UInt64[]);
                case CimType.SInt64Array:
                    return typeof(Int64[]);
                case CimType.Real32Array:
                    return typeof(Single[]);
                case CimType.Real64Array:
                    return typeof(Double[]);
                case CimType.Char16Array:
                    return typeof(Char[]);
                case CimType.DateTimeArray:
                    return typeof(object[]);
                case CimType.StringArray:
                    return typeof(String[]);
                case CimType.ReferenceArray:
                case CimType.InstanceArray:
                    return typeof(CimInstance[]);

                case CimType.Unknown:
                default:
                    Debug.Assert(false, "Unrecognized or unsupported value of CimType");
                    throw new ArgumentOutOfRangeException("cimType");
            }
        }
    }

    internal static class MiTypeExtensionMethods
    {
        public static CimType ToCimType(this Native.MiType miType)
        {
            switch (miType)
            {
                case Native.MiType.Boolean: return CimType.Boolean;
                case Native.MiType.UInt8: return CimType.UInt8;
                case Native.MiType.SInt8: return CimType.SInt8;
                case Native.MiType.UInt16: return CimType.UInt16;
                case Native.MiType.SInt16: return CimType.SInt16;
                case Native.MiType.UInt32: return CimType.UInt32;
                case Native.MiType.SInt32: return CimType.SInt32;
                case Native.MiType.UInt64: return CimType.UInt64;
                case Native.MiType.SInt64: return CimType.SInt64;
                case Native.MiType.Real32: return CimType.Real32;
                case Native.MiType.Real64: return CimType.Real64;
                case Native.MiType.Char16: return CimType.Char16;
                case Native.MiType.DateTime: return CimType.DateTime;
                case Native.MiType.String: return CimType.String;
                case Native.MiType.Reference: return CimType.Reference;
                case Native.MiType.Instance: return CimType.Instance;

                case Native.MiType.BooleanArray: return CimType.BooleanArray;
                case Native.MiType.UInt8Array: return CimType.UInt8Array;
                case Native.MiType.SInt8Array: return CimType.SInt8Array;
                case Native.MiType.UInt16Array: return CimType.UInt16Array;
                case Native.MiType.SInt16Array: return CimType.SInt16Array;
                case Native.MiType.UInt32Array: return CimType.UInt32Array;
                case Native.MiType.SInt32Array: return CimType.SInt32Array;
                case Native.MiType.UInt64Array: return CimType.UInt64Array;
                case Native.MiType.SInt64Array: return CimType.SInt64Array;
                case Native.MiType.Real32Array: return CimType.Real32Array;
                case Native.MiType.Real64Array: return CimType.Real64Array;
                case Native.MiType.Char16Array: return CimType.Char16Array;
                case Native.MiType.DateTimeArray: return CimType.DateTimeArray;
                case Native.MiType.StringArray: return CimType.StringArray;
                case Native.MiType.ReferenceArray: return CimType.ReferenceArray;
                case Native.MiType.InstanceArray: return CimType.InstanceArray;

                default:
                    Debug.Assert(false, "Unrecognized or unsupported value of Native.MiType");
                    throw new ArgumentOutOfRangeException("miType");
            }
        }
    }
}