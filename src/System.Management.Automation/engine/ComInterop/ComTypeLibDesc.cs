// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !SILVERLIGHT // ComObject
#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Dynamic;
using System.Globalization;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace System.Management.Automation.ComInterop
{
    /// <summary>
    /// Cached information from a TLB. Only information that is required is saved. CoClasses are used
    /// for event hookup. Enums are stored for accessing symbolic names from scripts.
    /// </summary>
    internal sealed class ComTypeLibDesc : IDynamicMetaObjectProvider
    {
        // typically typelibs contain very small number of coclasses
        // so we will just use the linked list as it performs better
        // on small number of entities
        private LinkedList<ComTypeClassDesc> _classes;
        private Dictionary<string, ComTypeEnumDesc> _enums;
        private ComTypes.TYPELIBATTR _typeLibAttributes;

        private static Dictionary<Guid, ComTypeLibDesc> s_cachedTypeLibDesc = new Dictionary<Guid, ComTypeLibDesc>();

        private ComTypeLibDesc()
        {
            _enums = new Dictionary<string, ComTypeEnumDesc>();
            _classes = new LinkedList<ComTypeClassDesc>();
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "<type library {0}>", Name);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public string Documentation
        {
            get { return string.Empty; }
        }

        #region IDynamicMetaObjectProvider Members

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
        {
            return new TypeLibMetaObject(parameter, this);
        }

        #endregion

        /// <summary>
        /// Reads the latest registered type library for the corresponding GUID,
        /// reads definitions of CoClasses and Enum's from this library
        /// and creates a IDynamicMetaObjectProvider that allows to instantiate coclasses
        /// and get actual values for the enums.
        /// </summary>
        /// <param name="typeLibGuid">Type Library Guid.</param>
        /// <returns>ComTypeLibDesc object.</returns>
        [System.Runtime.Versioning.ResourceExposure(System.Runtime.Versioning.ResourceScope.Machine)]
        [System.Runtime.Versioning.ResourceConsumption(System.Runtime.Versioning.ResourceScope.Machine, System.Runtime.Versioning.ResourceScope.Machine)]
        public static ComTypeLibInfo CreateFromGuid(Guid typeLibGuid)
        {
            // passing majorVersion = -1, minorVersion = -1 will always
            // load the latest typelib
            ComTypes.ITypeLib typeLib = UnsafeMethods.LoadRegTypeLib(ref typeLibGuid, -1, -1, 0);

            return new ComTypeLibInfo(GetFromTypeLib(typeLib));
        }

        /// <summary>
        /// Gets an ITypeLib object from OLE Automation compatible RCW ,
        /// reads definitions of CoClasses and Enum's from this library
        /// and creates a IDynamicMetaObjectProvider that allows to instantiate coclasses
        /// and get actual values for the enums.
        /// </summary>
        /// <param name="rcw">OLE automation compatible RCW.</param>
        /// <returns>ComTypeLibDesc object.</returns>
        public static ComTypeLibInfo CreateFromObject(object rcw)
        {
            if (Marshal.IsComObject(rcw) == false)
            {
                throw new ArgumentException("COM object is expected.");
            }

            ComTypes.ITypeInfo typeInfo = ComRuntimeHelpers.GetITypeInfoFromIDispatch(rcw as IDispatch, true);

            ComTypes.ITypeLib typeLib;
            int typeInfoIndex;
            typeInfo.GetContainingTypeLib(out typeLib, out typeInfoIndex);

            return new ComTypeLibInfo(GetFromTypeLib(typeLib));
        }

        internal static ComTypeLibDesc GetFromTypeLib(ComTypes.ITypeLib typeLib)
        {
            // check whether we have already loaded this type library
            ComTypes.TYPELIBATTR typeLibAttr = ComRuntimeHelpers.GetTypeAttrForTypeLib(typeLib);
            ComTypeLibDesc typeLibDesc;
            lock (s_cachedTypeLibDesc)
            {
                if (s_cachedTypeLibDesc.TryGetValue(typeLibAttr.guid, out typeLibDesc))
                {
                    return typeLibDesc;
                }
            }

            typeLibDesc = new ComTypeLibDesc();

            typeLibDesc.Name = ComRuntimeHelpers.GetNameOfLib(typeLib);
            typeLibDesc._typeLibAttributes = typeLibAttr;

            int countTypes = typeLib.GetTypeInfoCount();
            for (int i = 0; i < countTypes; i++)
            {
                ComTypes.TYPEKIND typeKind;
                typeLib.GetTypeInfoType(i, out typeKind);

                ComTypes.ITypeInfo typeInfo;
                typeLib.GetTypeInfo(i, out typeInfo);
                if (typeKind == ComTypes.TYPEKIND.TKIND_COCLASS)
                {
                    ComTypeClassDesc classDesc = new ComTypeClassDesc(typeInfo, typeLibDesc);
                    typeLibDesc._classes.AddLast(classDesc);
                }
                else if (typeKind == ComTypes.TYPEKIND.TKIND_ENUM)
                {
                    ComTypeEnumDesc enumDesc = new ComTypeEnumDesc(typeInfo, typeLibDesc);
                    typeLibDesc._enums.Add(enumDesc.TypeName, enumDesc);
                }
                else if (typeKind == ComTypes.TYPEKIND.TKIND_ALIAS)
                {
                    ComTypes.TYPEATTR typeAttr = ComRuntimeHelpers.GetTypeAttrForTypeInfo(typeInfo);
                    if (typeAttr.tdescAlias.vt == (short)VarEnum.VT_USERDEFINED)
                    {
                        string aliasName, documentation;
                        ComRuntimeHelpers.GetInfoFromType(typeInfo, out aliasName, out documentation);

                        ComTypes.ITypeInfo referencedTypeInfo;
                        typeInfo.GetRefTypeInfo(typeAttr.tdescAlias.lpValue.ToInt32(), out referencedTypeInfo);

                        ComTypes.TYPEATTR referencedTypeAttr = ComRuntimeHelpers.GetTypeAttrForTypeInfo(referencedTypeInfo);
                        ComTypes.TYPEKIND referencedTypeKind = referencedTypeAttr.typekind;

                        if (referencedTypeKind == ComTypes.TYPEKIND.TKIND_ENUM)
                        {
                            ComTypeEnumDesc enumDesc = new ComTypeEnumDesc(referencedTypeInfo, typeLibDesc);
                            typeLibDesc._enums.Add(aliasName, enumDesc);
                        }
                    }
                }
            }

            // cached the typelib using the guid as the dictionary key
            lock (s_cachedTypeLibDesc)
            {
                s_cachedTypeLibDesc.Add(typeLibAttr.guid, typeLibDesc);
            }

            return typeLibDesc;
        }

        public object GetTypeLibObjectDesc(string member)
        {
            foreach (ComTypeClassDesc coclass in _classes)
            {
                if (member == coclass.TypeName)
                {
                    return coclass;
                }
            }

            ComTypeEnumDesc enumDesc;
            if (_enums != null && _enums.TryGetValue(member, out enumDesc) == true)
                return enumDesc;

            return null;
        }

        // TODO: internal
        public string[] GetMemberNames()
        {
            string[] retval = new string[_enums.Count + _classes.Count];
            int i = 0;
            foreach (ComTypeClassDesc coclass in _classes)
            {
                retval[i++] = coclass.TypeName;
            }

            foreach (KeyValuePair<string, ComTypeEnumDesc> enumDesc in _enums)
            {
                retval[i++] = enumDesc.Key;
            }

            return retval;
        }

        internal bool HasMember(string member)
        {
            foreach (ComTypeClassDesc coclass in _classes)
            {
                if (member == coclass.TypeName)
                {
                    return true;
                }
            }

            if (_enums.ContainsKey(member) == true)
                return true;

            return false;
        }

        public Guid Guid
        {
            get { return _typeLibAttributes.guid; }
        }

        public short VersionMajor
        {
            get { return _typeLibAttributes.wMajorVerNum; }
        }

        public short VersionMinor
        {
            get { return _typeLibAttributes.wMinorVerNum; }
        }

        public string Name { get; private set; }

        internal ComTypeClassDesc GetCoClassForInterface(string itfName)
        {
            foreach (ComTypeClassDesc coclass in _classes)
            {
                if (coclass.Implements(itfName, false))
                {
                    return coclass;
                }
            }

            return null;
        }
    }
}

#endif

