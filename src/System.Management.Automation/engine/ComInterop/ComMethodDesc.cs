// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;

namespace System.Management.Automation.ComInterop
{
    internal class ComMethodDesc
    {
        private readonly INVOKEKIND _invokeKind;

        private ComMethodDesc(int dispId)
        {
            DispId = dispId;
        }

        internal ComMethodDesc(string name, int dispId)
            : this(dispId)
        {
            // no ITypeInfo constructor
            Name = name;
        }

        internal ComMethodDesc(string name, int dispId, INVOKEKIND invkind)
            : this(name, dispId)
        {
            _invokeKind = invkind;
        }

        internal ComMethodDesc(ITypeInfo typeInfo, FUNCDESC funcDesc)
            : this(funcDesc.memid)
        {

            _invokeKind = funcDesc.invkind;

            string[] rgNames = new string[1 + funcDesc.cParams];
            typeInfo.GetNames(DispId, rgNames, rgNames.Length, out int cNames);
            if (IsPropertyPut && rgNames[rgNames.Length - 1] == null)
            {
                rgNames[rgNames.Length - 1] = "value";
                cNames++;
            }
            Debug.Assert(cNames == rgNames.Length);
            Name = rgNames[0];

            ParamCount = funcDesc.cParams;
        }

        public string Name { get; }

        public int DispId { get; }

        public bool IsPropertyGet
        {
            get
            {
                return (_invokeKind & INVOKEKIND.INVOKE_PROPERTYGET) != 0;
            }
        }

        public bool IsDataMember
        {
            get
            {
                //must be regular get
                if (!IsPropertyGet || DispId == ComDispIds.DISPID_NEWENUM)
                {
                    return false;
                }

                //must have no parameters
                return ParamCount == 0;
            }
        }

        public bool IsPropertyPut
        {
            get
            {
                return (_invokeKind & (INVOKEKIND.INVOKE_PROPERTYPUT | INVOKEKIND.INVOKE_PROPERTYPUTREF)) != 0;
            }
        }

        public bool IsPropertyPutRef
        {
            get
            {
                return (_invokeKind & INVOKEKIND.INVOKE_PROPERTYPUTREF) != 0;
            }
        }

        internal int ParamCount { get; }
    }
}
