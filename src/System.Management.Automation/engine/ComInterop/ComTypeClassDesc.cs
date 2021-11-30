// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace System.Management.Automation.ComInterop
{
    internal class ComTypeClassDesc : ComTypeDesc, IDynamicMetaObjectProvider
    {
        private LinkedList<string> _itfs; // implemented interfaces
        private LinkedList<string> _sourceItfs; // source interfaces supported by this coclass
        private Type _typeObj;

        public object CreateInstance()
        {
            if (_typeObj == null)
            {
                _typeObj = Type.GetTypeFromCLSID(Guid);
            }
            return Activator.CreateInstance(Type.GetTypeFromCLSID(Guid));
        }

        internal ComTypeClassDesc(ComTypes.ITypeInfo typeInfo, ComTypeLibDesc typeLibDesc) : base(typeInfo, typeLibDesc)
        {
            ComTypes.TYPEATTR typeAttr = ComRuntimeHelpers.GetTypeAttrForTypeInfo(typeInfo);
            Guid = typeAttr.guid;

            for (int i = 0; i < typeAttr.cImplTypes; i++)
            {
                typeInfo.GetRefTypeOfImplType(i, out int hRefType);
                typeInfo.GetRefTypeInfo(hRefType, out ComTypes.ITypeInfo currentTypeInfo);
                typeInfo.GetImplTypeFlags(i, out ComTypes.IMPLTYPEFLAGS implTypeFlags);

                bool isSourceItf = (implTypeFlags & ComTypes.IMPLTYPEFLAGS.IMPLTYPEFLAG_FSOURCE) != 0;
                AddInterface(currentTypeInfo, isSourceItf);
            }
        }

        private void AddInterface(ComTypes.ITypeInfo itfTypeInfo, bool isSourceItf)
        {
            string itfName = ComRuntimeHelpers.GetNameOfType(itfTypeInfo);

            if (isSourceItf)
            {
                if (_sourceItfs == null)
                {
                    _sourceItfs = new LinkedList<string>();
                }
                _sourceItfs.AddLast(itfName);
            }
            else
            {
                if (_itfs == null)
                {
                    _itfs = new LinkedList<string>();
                }

                _itfs.AddLast(itfName);
            }
        }

        internal bool Implements(string itfName, bool isSourceItf)
        {
            if (isSourceItf)
                return _sourceItfs.Contains(itfName);

            return _itfs.Contains(itfName);
        }

        #region IDynamicMetaObjectProvider Members

        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new ComClassMetaObject(parameter, this);
        }

        #endregion
    }
}
