// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;

namespace System.Management.Automation.ComInterop
{
    internal class ComTypeDesc
    {
        private readonly string _typeName;
        private readonly string _documentation;
        private ComMethodDesc _getItem;
        private ComMethodDesc _setItem;

        internal ComTypeDesc(ITypeInfo typeInfo, ComTypeLibDesc typeLibDesc)
        {
            if (typeInfo != null)
            {
                ComRuntimeHelpers.GetInfoFromType(typeInfo, out _typeName, out _documentation);
            }
            TypeLib = typeLibDesc;
        }

        internal static ComTypeDesc FromITypeInfo(ITypeInfo typeInfo, TYPEATTR typeAttr)
        {
            switch (typeAttr.typekind)
            {
                case TYPEKIND.TKIND_COCLASS:
                    return new ComTypeClassDesc(typeInfo, null);
                case TYPEKIND.TKIND_ENUM:
                    return new ComTypeEnumDesc(typeInfo, null);
                case TYPEKIND.TKIND_DISPATCH:
                case TYPEKIND.TKIND_INTERFACE:
                    ComTypeDesc typeDesc = new ComTypeDesc(typeInfo, null);
                    return typeDesc;
                default:
                    throw new InvalidOperationException("Attempting to wrap an unsupported enum type.");
            }
        }

        internal static ComTypeDesc CreateEmptyTypeDesc()
        {
            ComTypeDesc typeDesc = new ComTypeDesc(null, null)
            {
                Funcs = new Hashtable(),
                Puts = new Hashtable(),
                PutRefs = new Hashtable(),
                Events = EmptyEvents
            };

            return typeDesc;
        }

        internal static Dictionary<string, ComEventDesc> EmptyEvents { get; } = new Dictionary<string, ComEventDesc>();

        internal Hashtable Funcs { get; set; }

        internal Hashtable Puts { get; set; }

        internal Hashtable PutRefs { get; set; }

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
            if (PutRefs.ContainsKey(name))
            {
                method = PutRefs[name] as ComMethodDesc;
                return true;
            }
            method = null;
            return false;
        }

        internal void AddPutRef(string name, ComMethodDesc method)
        {
            name = name.ToUpper(System.Globalization.CultureInfo.InvariantCulture);
            lock (PutRefs)
            {
                PutRefs[name] = method;
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

                lock (PutRefs)
                {
                    foreach (ComMethodDesc func in PutRefs.Values)
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

            string[] result = new string[names.Keys.Count];
            names.Keys.CopyTo(result, 0);
            return result;
        }

        public string TypeName => _typeName;

        internal string Documentation => _documentation;

        public ComTypeLibDesc TypeLib { get; }

        internal Guid Guid { get; set; }

        internal ComMethodDesc GetItem => _getItem;

        internal void EnsureGetItem(ComMethodDesc candidate)
        {
            Interlocked.CompareExchange(ref _getItem, candidate, null);
        }

        internal ComMethodDesc SetItem => _setItem;

        internal void EnsureSetItem(ComMethodDesc candidate)
        {
            Interlocked.CompareExchange(ref _setItem, candidate, null);
        }
    }
}
