/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Microsoft.Management.Infrastructure
{
    public static class CimConverter
    {
        static CimConverter()
        {
            InitializeDotNetTypeToCimTypeDictionaries();
        }

        /// <summary>
        /// Gets the dotnet type of a given CimType 
        /// </summary>
        /// <param name="cimType">cimType input.</param>
        /// <returns>A string representing dotnet type.</returns>
        public static Type GetDotNetType(CimType cimType)
        {
            switch (cimType)
            {
                case CimType.SInt8:
                    return typeof(SByte);
                case CimType.UInt8:
                    return typeof(Byte);
                case CimType.SInt16:
                    return typeof(Int16);
                case CimType.UInt16:
                    return typeof(UInt16);
                case CimType.SInt32:
                    return typeof(Int32);
                case CimType.UInt32:
                    return typeof(UInt32);
                case CimType.SInt64:
                    return typeof(Int64);
                case CimType.UInt64:
                    return typeof(UInt64);
                case CimType.Real32:
                    return typeof(Single);
                case CimType.Real64:
                    return typeof(double);
                case CimType.Boolean:
                    return typeof(bool);
                case CimType.String:
                    return typeof(string);
                case CimType.DateTime:
                    //This can be either DateTime or TimeSpan
                    return null;
                case CimType.Reference:
                    return typeof(CimInstance);
                case CimType.Char16:
                    return typeof(char);
                case CimType.Instance:
                    return typeof(CimInstance);

                case CimType.BooleanArray:
                    return typeof(bool[]);
                case CimType.UInt8Array:
                    return typeof(Byte[]);
                case CimType.SInt8Array:
                    return typeof(SByte[]);
                case CimType.UInt16Array:
                    return typeof(UInt16[]);
                case CimType.SInt16Array:
                    return typeof(Int64[]);
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
                    return typeof(double[]);
                case CimType.Char16Array:
                    return typeof(char[]);
                case CimType.DateTimeArray:
                    // This can be either DateTime or TimeSpan
                    return null;
                case CimType.StringArray:
                    return typeof(string[]);
                case CimType.ReferenceArray:
                    return typeof(CimInstance[]);
                case CimType.InstanceArray:
                    return typeof(CimInstance[]);

                case CimType.Unknown:
                    return null;

                default:
                    Debug.Assert(false, "Got unrecognized CimType value");
                    return null;
            }
        }

        private static Dictionary<Type, CimType> _dotNetTypeToScalarCimType;
        private static Dictionary<Type, CimType> _dotNetTypeToArrayCimType;

        private static void InitializeDotNetTypeToCimTypeDictionaries()
        {
            _dotNetTypeToScalarCimType = new Dictionary<Type, CimType>
                                            {
                                                {typeof (SByte), CimType.SInt8},
                                                {typeof (Byte), CimType.UInt8},
                                                {typeof (Int16), CimType.SInt16},
                                                {typeof (UInt16), CimType.UInt16},
                                                {typeof (Int32), CimType.SInt32},
                                                {typeof (UInt32), CimType.UInt32},
                                                {typeof (Int64), CimType.SInt64},
                                                {typeof (UInt64), CimType.UInt64},
                                                {typeof (Single), CimType.Real32},
                                                {typeof (Double), CimType.Real64},
                                                {typeof (Boolean), CimType.Boolean},
                                                {typeof (String), CimType.String},
                                                {typeof (DateTime), CimType.DateTime},
                                                {typeof (TimeSpan), CimType.DateTime},
                                                {typeof (CimInstance), CimType.Instance},
                                                {typeof (Char), CimType.Char16}
                                            };

            _dotNetTypeToArrayCimType = new Dictionary<Type, CimType>
                                           {
                                               {typeof (SByte), CimType.SInt8Array},
                                               {typeof (Byte), CimType.UInt8Array},
                                               {typeof (Int16), CimType.SInt16Array},
                                               {typeof (UInt16), CimType.UInt16Array},
                                               {typeof (Int32), CimType.SInt32Array},
                                               {typeof (UInt32), CimType.UInt32Array},
                                               {typeof (Int64), CimType.SInt64Array},
                                               {typeof (UInt64), CimType.UInt64Array},
                                               {typeof (Single), CimType.Real32Array},
                                               {typeof (Double), CimType.Real64Array},
                                               {typeof (Boolean), CimType.BooleanArray},
                                               {typeof (String), CimType.StringArray},
                                               {typeof (DateTime), CimType.DateTimeArray},
                                               {typeof (TimeSpan), CimType.DateTimeArray},
                                               {typeof (CimInstance), CimType.InstanceArray},
                                               {typeof (Char), CimType.Char16Array}
                                           };
        }

        public static CimType GetCimType(Type dotNetType)
        {
            if (dotNetType == null)
            {
                throw new ArgumentNullException("dotNetType");
            }

            CimType result;

            Type ilistInterface = dotNetType
#if(!_CORECLR)
                .GetInterfaces()
#else
                .GetTypeInfo().ImplementedInterfaces
#endif
                .SingleOrDefault(
                    i =>
#if(!_CORECLR)
                        i.IsGenericType &&
#else
                        i.GetTypeInfo().IsGenericType &&
#endif
                        i.GetGenericTypeDefinition().Equals(typeof (IList<>)));
            if (ilistInterface != null)
            {
#if(!_CORECLR)
                Type elementType = ilistInterface.GetGenericArguments()[0];
#else
                Type elementType = ilistInterface.GetTypeInfo().GenericTypeArguments[0];
#endif
                if (_dotNetTypeToArrayCimType.TryGetValue(elementType, out result))
                {
                    return result;
                }
                return CimType.Unknown;
            }

            if (_dotNetTypeToScalarCimType.TryGetValue(dotNetType, out result))
            {
                return result;
            }
            return CimType.Unknown;
        }

        private static CimType GetCimTypeFromDotNetValue(object dotNetValue)
        {
            CimType cimType = CimType.Unknown;

            if (dotNetValue != null)
            {
                cimType = GetCimType(dotNetValue.GetType());
                if (cimType != CimType.Unknown)
                {
                    return cimType;
                }
            }

            IList valueAsList = dotNetValue as IList;
            if (valueAsList != null)
            {
                List<CimType> possibleCimTypes = valueAsList
                    .Cast<object>()
                    .Select(GetCimTypeFromDotNetValue)
                    .Where(x => x != CimType.Unknown)
                    .Distinct()
                    .ToList();

                if (possibleCimTypes.Count == 1)
                {
                    CimType elementType = possibleCimTypes[0];
                    switch (elementType)
                    {
                        case CimType.SInt8:
                            return CimType.SInt8Array;
                        case CimType.UInt8:
                            return CimType.UInt8Array;
                        case CimType.SInt16:
                            return CimType.SInt16Array;
                        case CimType.UInt16:
                            return CimType.UInt16Array;
                        case CimType.SInt32:
                            return CimType.SInt32Array;
                        case CimType.UInt32:
                            return CimType.UInt32Array;
                        case CimType.SInt64:
                            return CimType.SInt64Array;
                        case CimType.UInt64:
                            return CimType.UInt64Array;
                        case CimType.Real32:
                            return CimType.Real32Array;
                        case CimType.Real64:
                            return CimType.Real64Array;
                        case CimType.Boolean:
                            return CimType.BooleanArray;
                        case CimType.String:
                            return CimType.StringArray;
                        case CimType.DateTime:
                            return CimType.DateTimeArray;
                        case CimType.Reference:
                            return CimType.ReferenceArray;
                        case CimType.Char16:
                            return CimType.Char16Array;
                        case CimType.Instance:
                            return CimType.InstanceArray;
                    }
                }
            }

            return CimType.Unknown;
        }

        internal static CimType GetCimTypeFromDotNetValueOrThrowAnException(object dotNetValue)
        {
            CimType cimType = GetCimTypeFromDotNetValue(dotNetValue);
            if (cimType == CimType.Unknown)
            {
                throw new ArgumentException(Strings.DotNetValueToCimTypeConversionNotPossible);
            }
            return cimType;
        }
    }
}