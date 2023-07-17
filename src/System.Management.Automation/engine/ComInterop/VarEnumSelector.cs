// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable 618 // The *Wrapper classes for COM are obsolete

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Management.Automation.InteropServices;
using System.Runtime.InteropServices;

namespace System.Management.Automation.ComInterop
{
    /// <summary>
    /// If a managed user type (as opposed to a primitive type or a COM object) is passed as an argument to a COM call, we need
    /// to determine the VarEnum type we will marshal it as. We have the following options:
    /// 1. Raise an exception. Languages with their own version of primitive types would not be able to call
    ///    COM methods using the language's types (for eg. strings in IronRuby are not System.String). An explicit
    ///    cast would be needed.
    /// 2. We could marshal it as VT_DISPATCH. Then COM code will be able to access all the APIs in a late-bound manner,
    ///    but old COM components will probably malfunction if they expect a primitive type.
    /// 3. We could guess which primitive type is the closest match. This will make COM components be as easily
    ///    accessible as .NET methods.
    /// 4. We could use the type library to check what the expected type is. However, the type library may not be available.
    ///
    /// VarEnumSelector implements option # 3.
    /// </summary>
    internal class VarEnumSelector
    {
        private static readonly Dictionary<VarEnum, Type> s_comToManagedPrimitiveTypes = CreateComToManagedPrimitiveTypes();
        private static readonly IList<IList<VarEnum>> s_comPrimitiveTypeFamilies = CreateComPrimitiveTypeFamilies();

        internal VarEnumSelector(Type[] explicitArgTypes)
        {
            VariantBuilders = new VariantBuilder[explicitArgTypes.Length];

            for (int i = 0; i < explicitArgTypes.Length; i++)
            {
                VariantBuilders[i] = GetVariantBuilder(explicitArgTypes[i]);
            }
        }

        internal VariantBuilder[] VariantBuilders { get; }

        internal static Type GetTypeForVarEnum(VarEnum vt)
        {
            Type type;

            switch (vt)
            {
                // VarEnums which can be used in VARIANTs, but which cannot occur in a TYPEDESC
                case VarEnum.VT_EMPTY:
                case VarEnum.VT_NULL:
                case VarEnum.VT_RECORD:
                    type = typeof(void);
                    break;

                // VarEnums which are not used in VARIANTs, but which can occur in a TYPEDESC
                case VarEnum.VT_VOID:
                    type = typeof(void);
                    break;

                case VarEnum.VT_HRESULT:
                    type = typeof(int);
                    break;

                case ((VarEnum)37): // VT_INT_PTR:
                    type = typeof(IntPtr);
                    break;

                case ((VarEnum)38): // VT_UINT_PTR:
                    type = typeof(UIntPtr);
                    break;

                case VarEnum.VT_SAFEARRAY:
                case VarEnum.VT_CARRAY:
                    type = typeof(Array);
                    break;

                case VarEnum.VT_LPSTR:
                case VarEnum.VT_LPWSTR:
                    type = typeof(string);
                    break;

                case VarEnum.VT_PTR:
                case VarEnum.VT_USERDEFINED:
                    type = typeof(object);
                    break;

                // For VarEnums that can be used in VARIANTs and well as TYPEDESCs, just use VarEnumSelector
                default:
                    type = VarEnumSelector.GetManagedMarshalType(vt);
                    break;
            }

            return type;
        }

        /// <summary>
        /// Gets the managed type that an object needs to be converted to in order for it to be able
        /// to be represented as a Variant.
        ///
        /// In general, there is a many-to-many mapping between Type and VarEnum. However, this method
        /// returns a simple mapping that is needed for the current implementation. The reason for the
        /// many-to-many relation is:
        /// 1. Int32 maps to VT_I4 as well as VT_ERROR, and Decimal maps to VT_DECIMAL and VT_CY. However,
        ///    this changes if you throw the wrapper types into the mix.
        /// 2. There is no Type to represent COM types. __ComObject is a private type, and Object is too
        ///    general.
        /// </summary>
        internal static Type GetManagedMarshalType(VarEnum varEnum)
        {
            Debug.Assert((varEnum & VarEnum.VT_BYREF) == 0);

            if (varEnum == VarEnum.VT_CY)
            {
                return typeof(CurrencyWrapper);
            }

            if (Variant.IsPrimitiveType(varEnum))
            {
                return s_comToManagedPrimitiveTypes[varEnum];
            }

            switch (varEnum)
            {
                case VarEnum.VT_EMPTY:
                case VarEnum.VT_NULL:
                case VarEnum.VT_UNKNOWN:
                case VarEnum.VT_DISPATCH:
                case VarEnum.VT_VARIANT:
                    return typeof(object);

                case VarEnum.VT_ERROR:
                    return typeof(ErrorWrapper);

                default:
                    throw Error.UnexpectedVarEnum(varEnum);
            }
        }

        private static Dictionary<VarEnum, Type> CreateComToManagedPrimitiveTypes()
        {
            Dictionary<VarEnum, Type> dict = new Dictionary<VarEnum, Type>()
            {
                { VarEnum.VT_I1,        typeof(sbyte) },
                { VarEnum.VT_I2,        typeof(Int16) },
                { VarEnum.VT_I4,        typeof(Int32) },
                { VarEnum.VT_I8,        typeof(Int64) },
                { VarEnum.VT_UI1,       typeof(byte) },
                { VarEnum.VT_UI2,       typeof(UInt16) },
                { VarEnum.VT_UI4,       typeof(UInt32) },
                { VarEnum.VT_UI8,       typeof(UInt64) },
                { VarEnum.VT_INT,       typeof(Int32) },
                { VarEnum.VT_UINT,      typeof(UInt32) },
                { VarEnum.VT_PTR,       typeof(IntPtr) },
                { VarEnum.VT_BOOL,      typeof(bool) },
                { VarEnum.VT_R4,        typeof(float) },
                { VarEnum.VT_R8,        typeof(double) },
                { VarEnum.VT_DECIMAL,   typeof(decimal) },
                { VarEnum.VT_DATE,      typeof(DateTime) },
                { VarEnum.VT_BSTR,      typeof(string) },
                { VarEnum.VT_CLSID,     typeof(Guid) },
                { VarEnum.VT_CY,        typeof(CurrencyWrapper) },
                { VarEnum.VT_ERROR,     typeof(ErrorWrapper) },
            };

            return dict;
        }

        #region Primitive COM types

        /// <summary>
        /// Creates a family of COM types such that within each family, there is a completely non-lossy
        /// conversion from a type to an earlier type in the family.
        /// </summary>
        private static IList<IList<VarEnum>> CreateComPrimitiveTypeFamilies()
        {
            VarEnum[][] typeFamilies = new VarEnum[][] {
                new VarEnum[] { VarEnum.VT_I8, VarEnum.VT_I4, VarEnum.VT_I2, VarEnum.VT_I1 },
                new VarEnum[] { VarEnum.VT_UI8, VarEnum.VT_UI4, VarEnum.VT_UI2, VarEnum.VT_UI1 },
                new VarEnum[] { VarEnum.VT_INT },
                new VarEnum[] { VarEnum.VT_UINT },
                new VarEnum[] { VarEnum.VT_BOOL },
                new VarEnum[] { VarEnum.VT_DATE },
                new VarEnum[] { VarEnum.VT_R8, VarEnum.VT_R4 },
                new VarEnum[] { VarEnum.VT_DECIMAL },
                new VarEnum[] { VarEnum.VT_BSTR },

                // wrappers
                new VarEnum[] { VarEnum.VT_CY },
                new VarEnum[] { VarEnum.VT_ERROR },
            };

            return typeFamilies;
        }

        /// <summary>
        /// Get the (one representative type for each) primitive type families that the argument can be converted to.
        /// </summary>
        private static List<VarEnum> GetConversionsToComPrimitiveTypeFamilies(Type argumentType)
        {
            List<VarEnum> compatibleComTypes = new List<VarEnum>();

            foreach (IList<VarEnum> typeFamily in s_comPrimitiveTypeFamilies)
            {
                foreach (VarEnum candidateType in typeFamily)
                {
                    Type candidateManagedType = s_comToManagedPrimitiveTypes[candidateType];
                    if (TypeUtils.IsImplicitlyConvertible(argumentType, candidateManagedType, true))
                    {
                        compatibleComTypes.Add(candidateType);
                        // Move on to the next type family. We need at most one type from each family
                        break;
                    }
                }
            }
            return compatibleComTypes;
        }

        /// <summary>
        /// If there is more than one type family that the argument can be converted to, we will throw a
        /// AmbiguousMatchException instead of randomly picking a winner.
        /// </summary>
        private static void CheckForAmbiguousMatch(Type argumentType, List<VarEnum> compatibleComTypes)
        {
            if (compatibleComTypes.Count <= 1)
            {
                return;
            }

            string typeNames = string.Empty;
            for (int i = 0; i < compatibleComTypes.Count; i++)
            {
                string typeName = s_comToManagedPrimitiveTypes[compatibleComTypes[i]].Name;
                if (i == (compatibleComTypes.Count - 1))
                {
                    typeNames += " and ";
                }
                else if (i != 0)
                {
                    typeNames += ", ";
                }
                typeNames += typeName;
            }

            throw Error.AmbiguousConversion(argumentType.Name, typeNames);
        }

        private static bool TryGetPrimitiveComType(Type argumentType, out VarEnum primitiveVarEnum)
        {
            switch (Type.GetTypeCode(argumentType))
            {
                case TypeCode.Boolean:
                    primitiveVarEnum = VarEnum.VT_BOOL;
                    return true;
                case TypeCode.Char:
                    primitiveVarEnum = VarEnum.VT_UI2;
                    return true;
                case TypeCode.SByte:
                    primitiveVarEnum = VarEnum.VT_I1;
                    return true;
                case TypeCode.Byte:
                    primitiveVarEnum = VarEnum.VT_UI1;
                    return true;
                case TypeCode.Int16:
                    primitiveVarEnum = VarEnum.VT_I2;
                    return true;
                case TypeCode.UInt16:
                    primitiveVarEnum = VarEnum.VT_UI2;
                    return true;
                case TypeCode.Int32:
                    primitiveVarEnum = VarEnum.VT_I4;
                    return true;
                case TypeCode.UInt32:
                    primitiveVarEnum = VarEnum.VT_UI4;
                    return true;
                case TypeCode.Int64:
                    primitiveVarEnum = VarEnum.VT_I8;
                    return true;
                case TypeCode.UInt64:
                    primitiveVarEnum = VarEnum.VT_UI8;
                    return true;
                case TypeCode.Single:
                    primitiveVarEnum = VarEnum.VT_R4;
                    return true;
                case TypeCode.Double:
                    primitiveVarEnum = VarEnum.VT_R8;
                    return true;
                case TypeCode.Decimal:
                    primitiveVarEnum = VarEnum.VT_DECIMAL;
                    return true;
                case TypeCode.DateTime:
                    primitiveVarEnum = VarEnum.VT_DATE;
                    return true;
                case TypeCode.String:
                    primitiveVarEnum = VarEnum.VT_BSTR;
                    return true;
            }

            if (argumentType == typeof(CurrencyWrapper))
            {
                primitiveVarEnum = VarEnum.VT_CY;
                return true;
            }

            if (argumentType == typeof(ErrorWrapper))
            {
                primitiveVarEnum = VarEnum.VT_ERROR;
                return true;
            }

            if (argumentType == typeof(IntPtr))
            {
                primitiveVarEnum = VarEnum.VT_INT;
                return true;
            }

            if (argumentType == typeof(UIntPtr))
            {
                primitiveVarEnum = VarEnum.VT_UINT;
                return true;
            }

            primitiveVarEnum = VarEnum.VT_VOID; // error
            return false;
        }

        /// <summary>
        /// Is there a unique primitive type that has the best conversion for the argument.
        /// </summary>
        private static bool TryGetPrimitiveComTypeViaConversion(Type argumentType, out VarEnum primitiveVarEnum)
        {
            // Look for a unique type family that the argument can be converted to.
            List<VarEnum> compatibleComTypes = GetConversionsToComPrimitiveTypeFamilies(argumentType);
            CheckForAmbiguousMatch(argumentType, compatibleComTypes);
            if (compatibleComTypes.Count == 1)
            {
                primitiveVarEnum = compatibleComTypes[0];
                return true;
            }

            primitiveVarEnum = VarEnum.VT_VOID; // error
            return false;
        }

        #endregion

        // Type.InvokeMember tries to marshal objects as VT_DISPATCH, and falls back to VT_UNKNOWN
        // VT_RECORD here just indicates that we have user defined type.
        // We will try VT_DISPATCH and then call GetNativeVariantForObject.
        private const VarEnum VT_DEFAULT = VarEnum.VT_RECORD;

        private static VarEnum GetComType(ref Type argumentType)
        {
            if (argumentType == typeof(Missing))
            {
                //actual variant type will be VT_ERROR | E_PARAMNOTFOUND
                return VarEnum.VT_RECORD;
            }

            if (argumentType.IsArray)
            {
                //actual variant type will be VT_ARRAY | VT_<ELEMENT_TYPE>
                return VarEnum.VT_ARRAY;
            }

            if (argumentType == typeof(UnknownWrapper))
            {
                return VarEnum.VT_UNKNOWN;
            }

            if (argumentType == typeof(DispatchWrapper))
            {
                return VarEnum.VT_DISPATCH;
            }

            if (argumentType == typeof(VariantWrapper))
            {
                return VarEnum.VT_VARIANT;
            }

            if (argumentType == typeof(BStrWrapper))
            {
                return VarEnum.VT_BSTR;
            }

            if (argumentType == typeof(ErrorWrapper))
            {
                return VarEnum.VT_ERROR;
            }

            if (argumentType == typeof(CurrencyWrapper))
            {
                return VarEnum.VT_CY;
            }

            // Many languages require an explicit cast for an enum to be used as the underlying type.
            // However, we want to allow this conversion for COM without requiring an explicit cast
            // so that enums from interop assemblies can be used as arguments.
            if (argumentType.IsEnum)
            {
                argumentType = Enum.GetUnderlyingType(argumentType);
                return GetComType(ref argumentType);
            }

            // COM cannot express valuetype nulls so we will convert to underlying type
            // it will throw if there is no value
            if (argumentType.IsNullableType())
            {
                argumentType = TypeUtils.GetNonNullableType(argumentType);
                return GetComType(ref argumentType);
            }

            //generic types cannot be exposed to COM so they do not implement COM interfaces.
            if (argumentType.IsGenericType)
            {
                return VarEnum.VT_UNKNOWN;
            }

            if (TryGetPrimitiveComType(argumentType, out VarEnum primitiveVarEnum))
            {
                return primitiveVarEnum;
            }

            // We could not find a way to marshal the type as a specific COM type
            return VT_DEFAULT;
        }

        /// <summary>
        /// Get the COM Variant type that argument should be marshalled as for a call to COM.
        /// </summary>
        private VariantBuilder GetVariantBuilder(Type argumentType)
        {
            //argumentType is coming from MarshalType, null means the dynamic object holds
            //a null value and not byref
            if (argumentType == null)
            {
                return new VariantBuilder(VarEnum.VT_EMPTY, new NullArgBuilder());
            }

            if (argumentType == typeof(DBNull))
            {
                return new VariantBuilder(VarEnum.VT_NULL, new NullArgBuilder());
            }

            ArgBuilder argBuilder;

            if (argumentType.IsByRef)
            {
                Type elementType = argumentType.GetElementType();

                VarEnum elementVarEnum;
                if (elementType == typeof(object) || elementType == typeof(DBNull))
                {
                    //no meaningful value to pass ByRef.
                    //perhaps the callee will replace it with something.
                    //need to pass as a variant reference
                    elementVarEnum = VarEnum.VT_VARIANT;
                }
                else
                {
                    elementVarEnum = GetComType(ref elementType);
                }

                argBuilder = GetSimpleArgBuilder(elementType, elementVarEnum);
                return new VariantBuilder(elementVarEnum | VarEnum.VT_BYREF, argBuilder);
            }

            VarEnum varEnum = GetComType(ref argumentType);
            argBuilder = GetByValArgBuilder(argumentType, ref varEnum);

            return new VariantBuilder(varEnum, argBuilder);
        }

        // This helper is called when we are looking for a ByVal marshalling
        // In a ByVal case we can take into account conversions or IConvertible if all other
        // attempts to find marshalling type failed
        private static ArgBuilder GetByValArgBuilder(Type elementType, ref VarEnum elementVarEnum)
        {
            // If VT indicates that marshalling type is unknown.
            if (elementVarEnum == VT_DEFAULT)
            {
                // Trying to find a conversion.
                if (TryGetPrimitiveComTypeViaConversion(elementType, out VarEnum convertibleTo))
                {
                    elementVarEnum = convertibleTo;
                    Type marshalType = GetManagedMarshalType(elementVarEnum);
                    return new ConversionArgBuilder(elementType, GetSimpleArgBuilder(marshalType, elementVarEnum));
                }

                // Checking for IConvertible.
                if (typeof(IConvertible).IsAssignableFrom(elementType))
                {
                    return new ConvertibleArgBuilder();
                }
            }
            return GetSimpleArgBuilder(elementType, elementVarEnum);
        }

        // This helper can produce a builder for types that are directly supported by Variant.
        private static SimpleArgBuilder GetSimpleArgBuilder(Type elementType, VarEnum elementVarEnum)
        {
            SimpleArgBuilder argBuilder;

            switch (elementVarEnum)
            {
                case VarEnum.VT_BSTR:
                    argBuilder = new StringArgBuilder(elementType);
                    break;
                case VarEnum.VT_BOOL:
                    argBuilder = new BoolArgBuilder(elementType);
                    break;
                case VarEnum.VT_DATE:
                    argBuilder = new DateTimeArgBuilder(elementType);
                    break;
                case VarEnum.VT_CY:
                    argBuilder = new CurrencyArgBuilder(elementType);
                    break;
                case VarEnum.VT_DISPATCH:
                    argBuilder = new DispatchArgBuilder(elementType);
                    break;
                case VarEnum.VT_UNKNOWN:
                    argBuilder = new UnknownArgBuilder(elementType);
                    break;
                case VarEnum.VT_VARIANT:
                case VarEnum.VT_ARRAY:
                case VarEnum.VT_RECORD:
                    argBuilder = new VariantArgBuilder(elementType);
                    break;
                case VarEnum.VT_ERROR:
                    argBuilder = new ErrorArgBuilder(elementType);
                    break;
                default:
                    Type marshalType = GetManagedMarshalType(elementVarEnum);
                    if (elementType == marshalType)
                    {
                        argBuilder = new SimpleArgBuilder(elementType);
                    }
                    else
                    {
                        argBuilder = new ConvertArgBuilder(elementType, marshalType);
                    }
                    break;
            }

            return argBuilder;
        }
    }
}
