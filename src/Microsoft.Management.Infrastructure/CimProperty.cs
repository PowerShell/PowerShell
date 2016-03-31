/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Globalization;
using Microsoft.Management.Infrastructure.Internal;
using Microsoft.Management.Infrastructure.Internal.Data;

namespace Microsoft.Management.Infrastructure
{
    /// <summary>
    /// A property of <see cref="CimInstance"/>
    /// </summary>
    public abstract class CimProperty
    {
        internal CimProperty()
        {
            // do not allow 3rd parties to derive from / instantiate this class
        }

        /// <summary>
        /// Name of the property
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// <para>
        /// Value of the property.  <c>null</c> if the property doesn't have a value.
        /// </para>
        /// <para>
        /// See <see cref="CimType"/> for a description of mapping between CIM types and .NET types.
        /// </para>
        /// </summary>
        /// <exception cref="ArgumentException">Thrown by the property setter, when the value doesn't match <see cref="CimProperty.CimType"/></exception>
        public abstract object Value { get; set;  }

        /// <summary>
        /// CIM type of the property
        /// </summary>
        public abstract CimType CimType { get; }

        /// <summary>
        /// Flags of the property.
        /// </summary>
        public abstract CimFlags Flags { get; }

        /// <summary>
        /// Indicates whether the value of a property was modified.
        /// </summary>
        public virtual bool IsValueModified
        {
            get
            {
                CimFlags currentFlags = this.Flags;
                bool isNotModifiedFlagPresent = (CimFlags.NotModified == (currentFlags & CimFlags.NotModified));
                return !isNotModifiedFlagPresent;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Creates a new property. 
        /// This method overload tries to infer <see cref="CimType"/> from the property <paramref name="value"/>
        /// </summary>
        /// <param name="name">Name of the property</param>
        /// <param name="value">Value of the property.  <c>null</c> is the property doesn't have an associated value.</param>
        /// <param name="flags"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null</exception>
        /// <exception cref="ArgumentException">Thrown when the <see cref="CimType"/> cannot be inferred from the property <paramref name="value"/> </exception>
        static public CimProperty Create(string name, object value, CimFlags flags)
        {
            CimType cimType = CimConverter.GetCimTypeFromDotNetValueOrThrowAnException(value);
            return Create(name, value, cimType, flags);
        }

        /// <summary>
        /// Creates a new property. 
        /// </summary>
        /// <param name="name">Name of the property</param>
        /// <param name="value">Value of the property.  <c>null</c> is the property doesn't have an associated value.</param>
        /// <param name="type"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> doesn't match <paramref name="type"/></exception>
        static public CimProperty Create(string name, object value, CimType type, CimFlags flags)
        {
            return new CimPropertyStandalone(name, value, type, flags);
        }

        public override string ToString()
        {
            return Helpers.ToStringFromNameAndValue(this.Name, this.Value);
        }

        internal static object ConvertToNativeLayer(object value, CimType cimType)
        {
            if (value == null) return null;

            var arrayOfObjects = value as object[];
            switch (cimType)
            {
                case CimType.Boolean:
                    return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                case CimType.Char16:
                    return Convert.ToChar(value, CultureInfo.InvariantCulture);
                case CimType.Real32:
                    return Convert.ToSingle(value, CultureInfo.InvariantCulture);
                case CimType.Real64:
                    return Convert.ToDouble(value, CultureInfo.InvariantCulture);
                case CimType.SInt16:
                    return Convert.ToInt16(value, CultureInfo.InvariantCulture);
                case CimType.SInt32:
                    return Convert.ToInt32(value, CultureInfo.InvariantCulture);
                case CimType.SInt64:
                    return Convert.ToInt64(value, CultureInfo.InvariantCulture);
                case CimType.SInt8:
                    return Convert.ToSByte(value, CultureInfo.InvariantCulture);
                case CimType.String:
                    if (value is Boolean)
                    {
#if(!_CORECLR)
                        return Convert.ToString(value, CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture);
#else
                        return Convert.ToString(value, CultureInfo.InvariantCulture).ToLower();
#endif
                    }
                    return Convert.ToString(value, CultureInfo.InvariantCulture);
                case CimType.UInt16:
                    return Convert.ToUInt16(value, CultureInfo.InvariantCulture);
                case CimType.UInt32:
                    return Convert.ToUInt32(value, CultureInfo.InvariantCulture);
                case CimType.UInt64:
                    return Convert.ToUInt64(value, CultureInfo.InvariantCulture);
                case CimType.UInt8:
                    return Convert.ToByte(value, CultureInfo.InvariantCulture);
                case CimType.BooleanArray:
                    if (arrayOfObjects != null)
                    {
                        Boolean[] array = new Boolean[arrayOfObjects.Length];
                        for (int i = 0; i < arrayOfObjects.Length; i++)
                        {
                            array[i] = Convert.ToBoolean(arrayOfObjects[i], CultureInfo.InvariantCulture);
                        }
                        return array;
                    }
                    break;
                case CimType.Char16Array:
                    if (arrayOfObjects != null)
                    {
                        Char[] array = new Char[arrayOfObjects.Length];
                        for (int i = 0; i < arrayOfObjects.Length; i++)
                        {
                            array[i] = Convert.ToChar(arrayOfObjects[i], CultureInfo.InvariantCulture);
                        }
                        return array;
                    }
                    break;
                case CimType.Real32Array:
                    if (arrayOfObjects != null)
                    {
                        Single[] array = new Single[arrayOfObjects.Length];
                        for (int i = 0; i < arrayOfObjects.Length; i++)
                        {
                            array[i] = Convert.ToSingle(arrayOfObjects[i], CultureInfo.InvariantCulture);
                        }
                        return array;
                    }
                    break;
                case CimType.Real64Array:
                    if (arrayOfObjects != null)
                    {
                        Double[] array = new Double[arrayOfObjects.Length];
                        for (int i = 0; i < arrayOfObjects.Length; i++)
                        {
                            array[i] = Convert.ToDouble(arrayOfObjects[i], CultureInfo.InvariantCulture);
                        }
                        return array;
                    }
                    break;
                case CimType.SInt16Array:
                    if (arrayOfObjects != null)
                    {
                        Int16[] array = new Int16[arrayOfObjects.Length];
                        for (int i = 0; i < arrayOfObjects.Length; i++)
                        {
                            array[i] = Convert.ToInt16(arrayOfObjects[i], CultureInfo.InvariantCulture);
                        }
                        return array;
                    }
                    break;
                case CimType.SInt32Array:
                    if (arrayOfObjects != null)
                    {
                        Int32[] array = new Int32[arrayOfObjects.Length];
                        for (int i = 0; i < arrayOfObjects.Length; i++)
                        {
                            array[i] = Convert.ToInt32(arrayOfObjects[i], CultureInfo.InvariantCulture);
                        }
                        return array;
                    }
                    break;
                case CimType.SInt64Array:
                    if (arrayOfObjects != null)
                    {
                        Int64[] array = new Int64[arrayOfObjects.Length];
                        for (int i = 0; i < arrayOfObjects.Length; i++)
                        {
                            array[i] = Convert.ToInt64(arrayOfObjects[i], CultureInfo.InvariantCulture);
                        }
                        return array;
                    }
                    break;
                case CimType.SInt8Array:
                    if (arrayOfObjects != null)
                    {
                        SByte[] array = new SByte[arrayOfObjects.Length];
                        for (int i = 0; i < arrayOfObjects.Length; i++)
                        {
                            array[i] = Convert.ToSByte(arrayOfObjects[i], CultureInfo.InvariantCulture);
                        }
                        return array;
                    }
                    break;
                case CimType.StringArray:
                    if (arrayOfObjects != null)
                    {
                        String[] array = new String[arrayOfObjects.Length];
                        for (int i = 0; i < arrayOfObjects.Length; i++)
                        {
                            if (arrayOfObjects[i] is Boolean)
                            {
#if(!_CORECLR)
                                array[i] = Convert.ToString(arrayOfObjects[i], CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture);
#else
                                array[i] = Convert.ToString(arrayOfObjects[i], CultureInfo.InvariantCulture).ToLower();
#endif
                            }
                            else
                            {
                                array[i] = Convert.ToString(arrayOfObjects[i], CultureInfo.InvariantCulture);
                            }
                        }
                        return array;
                    }
                    break;
                case CimType.UInt16Array:
                    if (arrayOfObjects != null)
                    {
                        UInt16[] array = new UInt16[arrayOfObjects.Length];
                        for (int i = 0; i < arrayOfObjects.Length; i++)
                        {
                            array[i] = Convert.ToUInt16(arrayOfObjects[i], CultureInfo.InvariantCulture);
                        }
                        return array;
                    }
                    break;
                case CimType.UInt32Array:
                    if (arrayOfObjects != null)
                    {
                        UInt32[] array = new UInt32[arrayOfObjects.Length];
                        for (int i = 0; i < arrayOfObjects.Length; i++)
                        {
                            array[i] = Convert.ToUInt32(arrayOfObjects[i], CultureInfo.InvariantCulture);
                        }
                        return array;
                    }
                    break;
                case CimType.UInt64Array:
                    if (arrayOfObjects != null)
                    {
                        UInt64[] array = new UInt64[arrayOfObjects.Length];
                        for (int i = 0; i < arrayOfObjects.Length; i++)
                        {
                            array[i] = Convert.ToUInt64(arrayOfObjects[i], CultureInfo.InvariantCulture);
                        }
                        return array;
                    }
                    break;
                case CimType.UInt8Array:
                    if (arrayOfObjects != null)
                    {
                        Byte[] array = new Byte[arrayOfObjects.Length];
                        for (int i = 0; i < arrayOfObjects.Length; i++)
                        {
                            array[i] = Convert.ToByte(arrayOfObjects[i], CultureInfo.InvariantCulture);
                        }
                        return array;
                    }
                    break;
                case CimType.DateTime:
                    if ((value is TimeSpan) || (value is DateTime))
                    {
                        return value;
                    }
                    else
                    {
                        return Convert.ToDateTime(value, CultureInfo.InvariantCulture);
                    }
                case CimType.DateTimeArray:
                    if (arrayOfObjects != null)
                    {
                        object[] array = new object[arrayOfObjects.Length];
                        for (int i = 0; i < arrayOfObjects.Length; i++)
                        {
                            if ((arrayOfObjects[i] is TimeSpan) || (arrayOfObjects[i] is DateTime))
                            {
                                array[i] = arrayOfObjects[i];
                            }
                            else
                            {
                                array[i] = Convert.ToDateTime(arrayOfObjects[i], CultureInfo.InvariantCulture);
                            }
                        }
                        return array;
                    }
                    break;
                case CimType.Reference:
                case CimType.ReferenceArray:
                case CimType.Instance:
                case CimType.InstanceArray:
                default:
                    break;
            }
            return value;
        }
    }
}
