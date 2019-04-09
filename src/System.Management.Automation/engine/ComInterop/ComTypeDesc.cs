// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !SILVERLIGHT // ComObject

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace System.Management.Automation.ComInterop
{
    internal class ComTypeDesc : ComTypeLibMemberDesc
    {
        private string _typeName;
        private string _documentation;
        // Hashtable is threadsafe for multiple readers single writer.
        // Enumerating and writing is mutually exclusive so require locking.
        private Hashtable _putRefs;
        private ComMethodDesc _getItem;
        private ComMethodDesc _setItem;

        internal ComTypeDesc(ITypeInfo typeInfo, ComType memberType, ComTypeLibDesc typeLibDesc) : base(memberType)
        {
            if (typeInfo != null)
            {
                ComRuntimeHelpers.GetInfoFromType(typeInfo, out _typeName, out _documentation);
            }

            TypeLib = typeLibDesc;
        }

        internal static ComTypeDesc FromITypeInfo(ComTypes.ITypeInfo typeInfo, ComTypes.TYPEATTR typeAttr)
        {
            if (typeAttr.typekind == ComTypes.TYPEKIND.TKIND_COCLASS)
            {
                return new ComTypeClassDesc(typeInfo, null);
            }
            else if (typeAttr.typekind == ComTypes.TYPEKIND.TKIND_ENUM)
            {
                return new ComTypeEnumDesc(typeInfo, null);
            }
            else if ((typeAttr.typekind == ComTypes.TYPEKIND.TKIND_DISPATCH) ||
                (typeAttr.typekind == ComTypes.TYPEKIND.TKIND_INTERFACE))
            {
                ComTypeDesc typeDesc = new ComTypeDesc(typeInfo, ComType.Interface, null);
                return typeDesc;
            }
            else
            {
                throw new InvalidOperationException("Attempting to wrap an unsupported enum type.");
            }
        }

        internal static ComTypeDesc CreateEmptyTypeDesc()
        {
            ComTypeDesc typeDesc = new ComTypeDesc(null, ComType.Interface, null);
            typeDesc.Funcs = new Hashtable();
            typeDesc.Puts = new Hashtable();
            typeDesc._putRefs = new Hashtable();
            typeDesc.Events = EmptyEvents;

            return typeDesc;
        }

        internal static Dictionary<string, ComEventDesc> EmptyEvents { get; } = new Dictionary<string, ComEventDesc>();

        internal Hashtable Funcs { get; set; }

        internal Hashtable Puts { get; set; }

        internal Hashtable PutRefs
        {
            set { _putRefs = value; }
        }

        internal Dictionary<string, ComEventDesc> Events { get; set; }

        internal bool TryGetFunc(string name, out ComMethodDesc method)
        {
            name = name.ToUpper(System.Globalization.CultureInfo.InvariantCulture);
            if (Funcs.ContainsKey(name))
            {
                method = Funcs[name] as ComMethodDesc;
                return true;
            }

            method = null;
            return false;
        }

        internal void AddFunc(string name, ComMethodDesc method)
        {
            name = name.ToUpper(System.Globalization.CultureInfo.InvariantCulture);
            lock (Funcs)
            {
                Funcs[name] = method;
            }
        }

        internal bool TryGetPut(string name, out ComMethodDesc method)
        {
            name = name.ToUpper(System.Globalization.CultureInfo.InvariantCulture);
            if (Puts.ContainsKey(name))
            {
                method = Puts[name] as ComMethodDesc;
                return true;
            }

            method = null;
            return false;
        }

        internal void AddPut(string name, ComMethodDesc method)
        {
            name = name.ToUpper(System.Globalization.CultureInfo.InvariantCulture);
            lock (Puts)
            {
                Puts[name] = method;
            }
        }

        internal bool TryGetPutRef(string name, out ComMethodDesc method)
        {
            name = name.ToUpper(System.Globalization.CultureInfo.InvariantCulture);
            if (_putRefs.ContainsKey(name))
            {
                method = _putRefs[name] as ComMethodDesc;
                return true;
            }

            method = null;
            return false;
        }

        internal void AddPutRef(string name, ComMethodDesc method)
        {
            name = name.ToUpper(System.Globalization.CultureInfo.InvariantCulture);
            lock (_putRefs)
            {
                _putRefs[name] = method;
            }
        }

        internal bool TryGetEvent(string name, out ComEventDesc @event)
        {
            name = name.ToUpper(System.Globalization.CultureInfo.InvariantCulture);
            return Events.TryGetValue(name, out @event);
        }

        internal string[] GetMemberNames(bool dataOnly)
        {
            var names = new Dictionary<string, object>();

            lock (Funcs)
            {
                foreach (ComMethodDesc func in Funcs.Values)
                {
                    if (!dataOnly || func.IsDataMember)
                    {
                        names.Add(func.Name, null);
                    }
                }
            }

            if (!dataOnly)
            {
                lock (Puts)
                {
                    foreach (ComMethodDesc func in Puts.Values)
                    {
                        if (!names.ContainsKey(func.Name))
                        {
                            names.Add(func.Name, null);
                        }
                    }
                }

                lock (_putRefs)
                {
                    foreach (ComMethodDesc func in _putRefs.Values)
                    {
                        if (!names.ContainsKey(func.Name))
                        {
                            names.Add(func.Name, null);
                        }
                    }
                }

                if (Events != null && Events.Count > 0)
                {
                    foreach (string name in Events.Keys)
                    {
                        if (!names.ContainsKey(name))
                        {
                            names.Add(name, null);
                        }
                    }
                }
            }

            var keys = names.Keys;
            string[] result = new string[keys.Count];
            keys.CopyTo(result, 0);
            return result;
        }

        // this property is public - accessed by an AST
        public string TypeName
        {
            get { return _typeName; }
        }

        internal string Documentation
        {
            get { return _documentation; }
        }

        // this property is public - accessed by an AST
        public ComTypeLibDesc TypeLib { get; }

        internal Guid Guid { get; set; }

        internal ComMethodDesc GetItem
        {
            get { return _getItem; }
        }

        internal void EnsureGetItem(ComMethodDesc candidate)
        {
            Interlocked.CompareExchange(ref _getItem, candidate, null);
        }

        internal ComMethodDesc SetItem
        {
            get { return _setItem; }
        }

        internal void EnsureSetItem(ComMethodDesc candidate)
        {
            Interlocked.CompareExchange(ref _setItem, candidate, null);
        }
    }
}

#endif

