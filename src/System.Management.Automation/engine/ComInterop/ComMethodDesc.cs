// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;

namespace System.Management.Automation.ComInterop
{
    internal class ComMethodDesc
    {
        internal readonly INVOKEKIND InvokeKind;

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
            InvokeKind = invkind;
        }

        internal ComMethodDesc(ITypeInfo typeInfo, FUNCDESC funcDesc)
            : this(funcDesc.memid)
        {
            InvokeKind = funcDesc.invkind;

            string[] rgNames = new string[1 + funcDesc.cParams];
            typeInfo.GetNames(DispId, rgNames, rgNames.Length, out int cNames);

            bool skipLast = false;
            if (IsPropertyPut && rgNames[rgNames.Length - 1] == null)
            {
                rgNames[rgNames.Length - 1] = "value";
                cNames++;
                skipLast = true;
            }
            Debug.Assert(cNames == rgNames.Length);
            Name = rgNames[0];

            ParamCount = funcDesc.cParams;
            ReturnType = ComUtil.GetTypeFromTypeDesc(funcDesc.elemdescFunc.tdesc);
            ParameterInformation = ComUtil.GetParameterInformation(funcDesc, skipLast);
        }

        public string Name { get; }

        public int DispId { get; }

        public bool IsPropertyGet
        {
            get
            {
                return (InvokeKind & INVOKEKIND.INVOKE_PROPERTYGET) != 0;
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
                return (InvokeKind & (INVOKEKIND.INVOKE_PROPERTYPUT | INVOKEKIND.INVOKE_PROPERTYPUTREF)) != 0;
            }
        }

        public bool IsPropertyPutRef
        {
            get
            {
                return (InvokeKind & INVOKEKIND.INVOKE_PROPERTYPUTREF) != 0;
            }
        }

        internal int ParamCount { get; }

        public Type ReturnType { get; set; }

        public Type InputType { get; set; }

        public ParameterInformation[] ParameterInformation
        {
            get;
            set;
        }
    }
}
