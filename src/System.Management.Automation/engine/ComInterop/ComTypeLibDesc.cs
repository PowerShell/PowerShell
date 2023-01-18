// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
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
        private readonly LinkedList<ComTypeClassDesc> _classes;
        private readonly Dictionary<string, ComTypeEnumDesc> _enums;
        private ComTypes.TYPELIBATTR _typeLibAttributes;

        private static readonly Dictionary<Guid, ComTypeLibDesc> s_cachedTypeLibDesc = new Dictionary<Guid, ComTypeLibDesc>();

        private ComTypeLibDesc()
        {
            _enums = new Dictionary<string, ComTypeEnumDesc>();
            _classes = new LinkedList<ComTypeClassDesc>();
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, $"<type library {Name}>");
        }

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

            typeLibDesc = new ComTypeLibDesc
            {
                Name = ComRuntimeHelpers.GetNameOfLib(typeLib),
                _typeLibAttributes = typeLibAttr
            };

            int countTypes = typeLib.GetTypeInfoCount();
            for (int i = 0; i < countTypes; i++)
            {
                typeLib.GetTypeInfoType(i, out ComTypes.TYPEKIND typeKind);

                typeLib.GetTypeInfo(i, out ComTypes.ITypeInfo typeInfo);
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
                        ComRuntimeHelpers.GetInfoFromType(typeInfo, out string aliasName, out _);

                        typeInfo.GetRefTypeInfo(typeAttr.tdescAlias.lpValue.ToInt32(), out ComTypes.ITypeInfo referencedTypeInfo);

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

            if (_enums != null && _enums.TryGetValue(member, out ComTypeEnumDesc enumDesc))
                return enumDesc;

            return null;
        }

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

            if (_enums.ContainsKey(member))
                return true;

            return false;
        }

        public Guid Guid => _typeLibAttributes.guid;

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
