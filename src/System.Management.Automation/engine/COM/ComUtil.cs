// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Management.Automation.ComInterop;
using System.Runtime.InteropServices;
using System.Text;

using COM = System.Runtime.InteropServices.ComTypes;

// Stops compiler from warning about unknown warnings. Prefast warning numbers are not recognized by C# compiler
#pragma warning disable 1634, 1691

namespace System.Management.Automation
{
    /// <summary>
    /// Defines a utility class that is used by COM adapter.
    /// </summary>
    internal static class ComUtil
    {
        // HResult error code '-2147352573' - Member not found.
        internal const int DISP_E_MEMBERNOTFOUND = unchecked((int)0x80020003);
        // HResult error code '-2147352570' - Unknown Name.
        internal const int DISP_E_UNKNOWNNAME = unchecked((int)0x80020006);
        // HResult error code '-2147319765' - Element not found.
        internal const int TYPE_E_ELEMENTNOTFOUND = unchecked((int)0x8002802b);

        /// <summary>
        /// Gets Method Signature from FuncDesc describing the method.
        /// </summary>
        /// <param name="typeinfo">ITypeInfo interface of the object.</param>
        /// <param name="funcdesc">FuncDesc which defines the method.</param>
        /// <param name="isPropertyPut">True if this is a property put; these properties take their return type from their first parameter.</param>
        /// <returns>Signature of the method.</returns>
        internal static string GetMethodSignatureFromFuncDesc(COM.ITypeInfo typeinfo, COM.FUNCDESC funcdesc, bool isPropertyPut)
        {
            StringBuilder builder = new StringBuilder();

            // First value is function name
            int namesCount = funcdesc.cParams + 1;
            string[] names = new string[funcdesc.cParams + 1];
            typeinfo.GetNames(funcdesc.memid, names, namesCount, out namesCount);

            if (!isPropertyPut)
            {
                // First get the string for return type.
                string retstring = GetStringFromTypeDesc(typeinfo, funcdesc.elemdescFunc.tdesc);
                builder.Append(retstring + " ");
            }

            // Append the function name
            builder.Append(names[0]);
            builder.Append(" (");

            IntPtr ElementDescriptionArrayPtr = funcdesc.lprgelemdescParam;
            int ElementDescriptionSize = Marshal.SizeOf<COM.ELEMDESC>();

            for (int i = 0; i < funcdesc.cParams; i++)
            {
                COM.ELEMDESC ElementDescription;
                int ElementDescriptionArrayByteOffset;
                IntPtr ElementDescriptionPointer;

                ElementDescription = new COM.ELEMDESC();
                ElementDescriptionArrayByteOffset = i * ElementDescriptionSize;
                ElementDescriptionPointer = ElementDescriptionArrayPtr + ElementDescriptionArrayByteOffset;
                ElementDescription = Marshal.PtrToStructure<COM.ELEMDESC>(ElementDescriptionPointer);

                string paramstring = GetStringFromTypeDesc(typeinfo, ElementDescription.tdesc);

                if (i == 0 && isPropertyPut) // use the type of the first argument as the return type
                {
                    builder.Insert(0, paramstring + " ");
                }
                else
                {
                    builder.Append(paramstring);
                    builder.Append(" " + names[i + 1]);

                    if (i < funcdesc.cParams - 1)
                    {
                        builder.Append(", ");
                    }
                }
            }

            builder.Append(')');

            return builder.ToString();
        }

        /// <summary>
        /// Gets the name of the method or property defined by funcdesc.
        /// </summary>
        /// <param name="typeinfo">ITypeInfo interface of the type.</param>
        /// <param name="funcdesc">FuncDesc of property of method.</param>
        /// <returns>Name of the method or property.</returns>
        internal static string GetNameFromFuncDesc(COM.ITypeInfo typeinfo, COM.FUNCDESC funcdesc)
        {
            // Get the method or property name.
            string strName, strDoc, strHelp;
            int id;
            typeinfo.GetDocumentation(funcdesc.memid, out strName, out strDoc, out id, out strHelp);
            return strName;
        }

        /// <summary>
        /// Gets the name of the custom type defined in the type library.
        /// </summary>
        /// <param name="typeinfo">ITypeInfo interface of the type.</param>
        /// <param name="refptr">Reference to the custom type.</param>
        /// <returns>Name of the custom type.</returns>
        private static string GetStringFromCustomType(COM.ITypeInfo typeinfo, IntPtr refptr)
        {
            COM.ITypeInfo custtypeinfo;
            int reftype = unchecked((int)(long)refptr); // note that we cast to long first to prevent overflows; this cast is OK since we are only interested in the lower word

            typeinfo.GetRefTypeInfo(reftype, out custtypeinfo);

            if (custtypeinfo != null)
            {
                string strName, strDoc, strHelp;
                int id;
                custtypeinfo.GetDocumentation(-1, out strName, out strDoc, out id, out strHelp);
                return strName;
            }

            return "UnknownCustomtype";
        }

        /// <summary>
        /// This function gets a string representation of the Type Descriptor
        /// This is used in generating signature for Properties and Methods.
        /// </summary>
        /// <param name="typeinfo">Reference to the type info to which the type descriptor belongs.</param>
        /// <param name="typedesc">Reference to type descriptor which is being converted to string from.</param>
        /// <returns>String representation of the type descriptor.</returns>
        private static string GetStringFromTypeDesc(COM.ITypeInfo typeinfo, COM.TYPEDESC typedesc)
        {
            if ((VarEnum)typedesc.vt == VarEnum.VT_PTR)
            {
                COM.TYPEDESC refdesc = Marshal.PtrToStructure<COM.TYPEDESC>(typedesc.lpValue);
                return GetStringFromTypeDesc(typeinfo, refdesc);
            }

            if ((VarEnum)typedesc.vt == VarEnum.VT_SAFEARRAY)
            {
                COM.TYPEDESC refdesc = Marshal.PtrToStructure<COM.TYPEDESC>(typedesc.lpValue);
                return "SAFEARRAY(" + GetStringFromTypeDesc(typeinfo, refdesc) + ")";
            }

            if ((VarEnum)typedesc.vt == VarEnum.VT_USERDEFINED)
            {
                return GetStringFromCustomType(typeinfo, typedesc.lpValue);
            }

            switch ((VarEnum)typedesc.vt)
            {
                case VarEnum.VT_I1:
                    return "char";

                case VarEnum.VT_I2:
                    return "short";

                case VarEnum.VT_I4:
                case VarEnum.VT_INT:
                case VarEnum.VT_HRESULT:
                    return "int";

                case VarEnum.VT_I8:
                    return "int64";

                case VarEnum.VT_R4:
                    return "float";

                case VarEnum.VT_R8:
                    return "double";

                case VarEnum.VT_UI1:
                    return "byte";

                case VarEnum.VT_UI2:
                    return "ushort";

                case VarEnum.VT_UI4:
                case VarEnum.VT_UINT:
                    return "uint";

                case VarEnum.VT_UI8:
                    return "uint64";

                case VarEnum.VT_BSTR:
                case VarEnum.VT_LPSTR:
                case VarEnum.VT_LPWSTR:
                    return "string";

                case VarEnum.VT_DATE:
                    return "Date";

                case VarEnum.VT_BOOL:
                    return "bool";

                case VarEnum.VT_CY:
                    return "currency";

                case VarEnum.VT_DECIMAL:
                    return "decimal";

                case VarEnum.VT_CLSID:
                    return "clsid";

                case VarEnum.VT_DISPATCH:
                    return "IDispatch";

                case VarEnum.VT_UNKNOWN:
                    return "IUnknown";

                case VarEnum.VT_VARIANT:
                    return "Variant";

                case VarEnum.VT_VOID:
                    return "void";

                case VarEnum.VT_ARRAY:
                    return "object[]";

                case VarEnum.VT_EMPTY:
                    return string.Empty;

                default:
                    return "Unknown!";
            }
        }

        /// <summary>
        /// Determine .net type for the given type descriptor.
        /// </summary>
        /// <param name="typedesc">COM type descriptor to convert.</param>
        /// <returns>Type represented by the typedesc.</returns>
        internal static Type GetTypeFromTypeDesc(COM.TYPEDESC typedesc)
        {
            VarEnum vt = (VarEnum)typedesc.vt;
            return VarEnumSelector.GetTypeForVarEnum(vt);
        }

        /// <summary>
        /// Converts a FuncDesc out of GetFuncDesc into a MethodInformation.
        /// </summary>
        private static ComMethodInformation GetMethodInformation(COM.FUNCDESC funcdesc, bool skipLastParameter)
        {
            Type returntype = GetTypeFromTypeDesc(funcdesc.elemdescFunc.tdesc);
            ParameterInformation[] parameters = GetParameterInformation(funcdesc, skipLastParameter);
            bool hasOptional = false;
            foreach (ParameterInformation p in parameters)
            {
                if (p.isOptional)
                {
                    hasOptional = true;
                    break;
                }
            }

            return new ComMethodInformation(false, hasOptional, parameters, returntype, funcdesc.memid, funcdesc.invkind);
        }

        /// <summary>
        /// Obtains the parameter information for a given FuncDesc.
        /// </summary>
        internal static ParameterInformation[] GetParameterInformation(COM.FUNCDESC funcdesc, bool skipLastParameter)
        {
            int cParams = funcdesc.cParams;
            if (skipLastParameter)
            {
                Diagnostics.Assert(cParams > 0, "skipLastParameter is only true for property setters where there is at least one parameter");
                cParams--;
            }

            ParameterInformation[] parameters = new ParameterInformation[cParams];

            IntPtr ElementDescriptionArrayPtr = funcdesc.lprgelemdescParam;
            int ElementDescriptionSize = Marshal.SizeOf<COM.ELEMDESC>();

            for (int i = 0; i < cParams; i++)
            {
                COM.ELEMDESC ElementDescription;
                int ElementDescriptionArrayByteOffset;
                IntPtr ElementDescriptionPointer;
                bool fOptional = false;

                ElementDescription = new COM.ELEMDESC();
                ElementDescriptionArrayByteOffset = i * ElementDescriptionSize;
                ElementDescriptionPointer = ElementDescriptionArrayPtr + ElementDescriptionArrayByteOffset;
                ElementDescription = Marshal.PtrToStructure<COM.ELEMDESC>(ElementDescriptionPointer);

                // get the type of parameter
                Type type = ComUtil.GetTypeFromTypeDesc(ElementDescription.tdesc);
                object defaultvalue = null;

                // check is this parameter is optional.
                if ((ElementDescription.desc.paramdesc.wParamFlags & COM.PARAMFLAG.PARAMFLAG_FOPT) != 0)
                {
                    fOptional = true;
                    defaultvalue = Type.Missing;
                }

                bool fByRef = (ElementDescription.desc.paramdesc.wParamFlags & COM.PARAMFLAG.PARAMFLAG_FOUT) != 0;
                parameters[i] = new ParameterInformation(type, fOptional, defaultvalue, fByRef);
            }

            return parameters;
        }

        /// <summary>
        /// Converts a MethodBase[] into a MethodInformation[]
        /// </summary>
        /// <returns>The ComMethodInformation[] corresponding to methods.</returns>
        internal static ComMethodInformation[] GetMethodInformationArray(COM.ITypeInfo typeInfo, Collection<int> methods, bool skipLastParameters)
        {
            int methodCount = methods.Count;
            int count = 0;
            ComMethodInformation[] returnValue = new ComMethodInformation[methodCount];

            foreach (int index in methods)
            {
                IntPtr pFuncDesc;
                typeInfo.GetFuncDesc(index, out pFuncDesc);
                COM.FUNCDESC funcdesc = Marshal.PtrToStructure<COM.FUNCDESC>(pFuncDesc);
                returnValue[count++] = ComUtil.GetMethodInformation(funcdesc, skipLastParameters);
                typeInfo.ReleaseFuncDesc(pFuncDesc);
            }

            return returnValue;
        }
    }
}
