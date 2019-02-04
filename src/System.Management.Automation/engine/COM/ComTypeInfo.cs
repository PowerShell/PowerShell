// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Management.Automation.Internal;
using COM = System.Runtime.InteropServices.ComTypes;

namespace System.Management.Automation
{
    /// <summary>
    /// A Wrapper class for COM object's Type Information.
    /// </summary>
    internal class ComTypeInfo
    {
        /// <summary>
        /// A member with a DISPID equal to –4 is found on a collection interface.
        /// This special member, often called '_NewEnum', returns an interface that enables clients to enumerate objects in a collection.
        /// </summary>
        internal const int DISPID_NEWENUM = -4;

        /// <summary>
        /// A member with a DISPID equal to 0 is considered a default member.
        /// Default members in COM can be transformed to default members in .NET (indexers in C#).
        /// </summary>
        internal const int DISPID_DEFAULTMEMBER = 0;

        /// <summary>
        /// Member variables.
        /// </summary>
        private Dictionary<string, ComProperty> _properties = null;
        private Dictionary<string, ComMethod> _methods = null;
        private COM.ITypeInfo _typeinfo = null;
        private Guid _guid = Guid.Empty;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="info">ITypeInfo object being wrapped by this object.</param>
        internal ComTypeInfo(COM.ITypeInfo info)
        {
            _typeinfo = info;
            _properties = new Dictionary<string, ComProperty>(StringComparer.OrdinalIgnoreCase);
            _methods = new Dictionary<string, ComMethod>(StringComparer.OrdinalIgnoreCase);

            if (_typeinfo != null)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Collection of properties in the COM object.
        /// </summary>
        internal Dictionary<string, ComProperty> Properties
        {
            get
            {
                return _properties;
            }
        }

        /// <summary>
        /// Collection of methods in the COM object.
        /// </summary>
        internal Dictionary<string, ComMethod> Methods
        {
            get
            {
                return _methods;
            }
        }

        /// <summary>
        /// Returns the string of the GUID for the type information.
        /// </summary>
        internal string Clsid
        {
            get
            {
                return _guid.ToString();
            }
        }

        /// <summary>
        /// If 'DISPID_NEWENUM' member is present, return the InvokeKind;
        /// otherwise, return null.
        /// </summary>
        internal COM.INVOKEKIND? NewEnumInvokeKind { get; private set; }

        /// <summary>
        /// Initializes the typeinfo object.
        /// </summary>
        private void Initialize()
        {
            if (_typeinfo != null)
            {
                COM.TYPEATTR typeattr = GetTypeAttr(_typeinfo);

                // Initialize the type information guid
                _guid = typeattr.guid;

                for (int i = 0; i < typeattr.cFuncs; i++)
                {
                    COM.FUNCDESC funcdesc = GetFuncDesc(_typeinfo, i);
                    if (funcdesc.memid == DISPID_NEWENUM) { NewEnumInvokeKind = funcdesc.invkind; }

                    if ((funcdesc.wFuncFlags & 0x1) == 0x1)
                    {
                        // https://msdn.microsoft.com/library/ee488948.aspx
                        // FUNCFLAGS -- FUNCFLAG_FRESTRICTED = 0x1:
                        //     Indicates that the function should not be accessible from macro languages.
                        //     This flag is intended for system-level functions or functions that type browsers should not display.
                        //
                        // For IUnknown methods (AddRef, QueryInterface and Release) and IDispatch methods (GetTypeInfoCount, GetTypeInfo, GetIDsOfNames and Invoke)
                        // FUNCFLAG_FRESTRICTED (0x1) is set for the 'wFuncFlags' field
                        continue;
                    }

                    string strName = ComUtil.GetNameFromFuncDesc(_typeinfo, funcdesc);

                    switch (funcdesc.invkind)
                    {
                        case COM.INVOKEKIND.INVOKE_PROPERTYGET:
                        case COM.INVOKEKIND.INVOKE_PROPERTYPUT:
                        case COM.INVOKEKIND.INVOKE_PROPERTYPUTREF:
                            AddProperty(strName, funcdesc, i);
                            break;

                        case COM.INVOKEKIND.INVOKE_FUNC:
                            AddMethod(strName, i);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Get the typeinfo interface for the given comobject.
        /// </summary>
        /// <param name="comObject">Reference to com object for which we are getting type information.</param>
        /// <returns>ComTypeInfo object which wraps the ITypeInfo interface of the given COM object.</returns>
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", Justification = "Code uses the out parameter of 'GetTypeInfo' to check if the call succeeded.")]
        internal static ComTypeInfo GetDispatchTypeInfo(object comObject)
        {
            ComTypeInfo result = null;
            IDispatch disp = comObject as IDispatch;
            if (disp != null)
            {
                COM.ITypeInfo typeinfo = null;
                disp.GetTypeInfo(0, 0, out typeinfo);
                if (typeinfo != null)
                {
                    COM.TYPEATTR typeattr = GetTypeAttr(typeinfo);

                    if ((typeattr.typekind == COM.TYPEKIND.TKIND_INTERFACE))
                    {
                        // We have typeinfo for custom interface. Get typeinfo for Dispatch interface.
                        typeinfo = GetDispatchTypeInfoFromCustomInterfaceTypeInfo(typeinfo);
                    }

                    if ((typeattr.typekind == COM.TYPEKIND.TKIND_COCLASS))
                    {
                        // We have typeinfo for the COClass.  Find the default interface and get typeinfo for default interface.
                        typeinfo = GetDispatchTypeInfoFromCoClassTypeInfo(typeinfo);
                    }

                    result = new ComTypeInfo(typeinfo);
                }
            }

            return result;
        }

        private void AddProperty(string strName, COM.FUNCDESC funcdesc, int index)
        {
            ComProperty prop;
            if (!_properties.TryGetValue(strName, out prop))
            {
                prop = new ComProperty(_typeinfo, strName);
                _properties[strName] = prop;
            }

            if (prop != null)
            {
                prop.UpdateFuncDesc(funcdesc, index);
            }
        }

        private void AddMethod(string strName, int index)
        {
            ComMethod method;
            if (!_methods.TryGetValue(strName, out method))
            {
                method = new ComMethod(_typeinfo, strName);
                _methods[strName] = method;
            }

            if (method != null)
            {
                method.AddFuncDesc(index);
            }
        }

        /// <summary>
        /// Get TypeAttr for the given type information.
        /// </summary>
        /// <param name="typeinfo">Reference to ITypeInfo from which to get TypeAttr.</param>
        /// <returns></returns>
        [ArchitectureSensitive]
        internal static COM.TYPEATTR GetTypeAttr(COM.ITypeInfo typeinfo)
        {
            IntPtr pTypeAttr;
            typeinfo.GetTypeAttr(out pTypeAttr);
            COM.TYPEATTR typeattr = Marshal.PtrToStructure<COM.TYPEATTR>(pTypeAttr);
            typeinfo.ReleaseTypeAttr(pTypeAttr);
            return typeattr;
        }

        /// <summary>
        /// </summary>
        /// <param name="typeinfo"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        [ArchitectureSensitive]
        internal static COM.FUNCDESC GetFuncDesc(COM.ITypeInfo typeinfo, int index)
        {
            IntPtr pFuncDesc;
            typeinfo.GetFuncDesc(index, out pFuncDesc);
            COM.FUNCDESC funcdesc = Marshal.PtrToStructure<COM.FUNCDESC>(pFuncDesc);
            typeinfo.ReleaseFuncDesc(pFuncDesc);
            return funcdesc;
        }

        /// <summary>
        /// </summary>
        /// <param name="typeinfo"></param>
        /// <returns></returns>
        internal static COM.ITypeInfo GetDispatchTypeInfoFromCustomInterfaceTypeInfo(COM.ITypeInfo typeinfo)
        {
            int href;
            COM.ITypeInfo dispinfo = null;

            try
            {
                // We need the typeinfo for Dispatch Interface
                typeinfo.GetRefTypeOfImplType(-1, out href);
                typeinfo.GetRefTypeInfo(href, out dispinfo);
            }
            catch (COMException ce)
            {
                // check if the error code is TYPE_E_ELEMENTNOTFOUND.
                // This error code is thrown when we can't IDispatch interface.
                if (ce.HResult != ComUtil.TYPE_E_ELEMENTNOTFOUND)
                {
                    // For other codes, rethrow the exception.
                    throw;
                }
            }

            return dispinfo;
        }

        /// <summary>
        /// Get the IDispatch Typeinfo from CoClass typeinfo.
        /// </summary>
        /// <param name="typeinfo">Reference to the type info to which the type descriptor belongs.</param>
        /// <returns>ITypeInfo reference to the Dispatch interface.</returns>
        internal static COM.ITypeInfo GetDispatchTypeInfoFromCoClassTypeInfo(COM.ITypeInfo typeinfo)
        {
            // Get the number of interfaces implemented by this CoClass.
            COM.TYPEATTR typeattr = GetTypeAttr(typeinfo);
            int count = typeattr.cImplTypes;
            int href;
            COM.ITypeInfo interfaceinfo = null;

            // For each interface implemented by this coclass
            for (int i = 0; i < count; i++)
            {
                // Get the type information?
                typeinfo.GetRefTypeOfImplType(i, out href);
                typeinfo.GetRefTypeInfo(href, out interfaceinfo);
                typeattr = GetTypeAttr(interfaceinfo);

                // Is this interface IDispatch compatible interface?
                if (typeattr.typekind == COM.TYPEKIND.TKIND_DISPATCH)
                {
                    return interfaceinfo;
                }

                // Nope. Is this a dual interface
                if ((typeattr.wTypeFlags & COM.TYPEFLAGS.TYPEFLAG_FDUAL) != 0)
                {
                    interfaceinfo = GetDispatchTypeInfoFromCustomInterfaceTypeInfo(interfaceinfo);
                    typeattr = GetTypeAttr(interfaceinfo);

                    if (typeattr.typekind == COM.TYPEKIND.TKIND_DISPATCH)
                    {
                        return interfaceinfo;
                    }
                }
            }

            return null;
        }
    }
}

