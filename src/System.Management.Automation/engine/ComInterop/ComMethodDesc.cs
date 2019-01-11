// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !SILVERLIGHT // ComObject

using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;

namespace System.Management.Automation.ComInterop
{
    internal class ComMethodDesc
    {
        private readonly string _name;
        internal readonly INVOKEKIND InvokeKind;

        private ComMethodDesc(int dispId)
        {
            DispId = dispId;
        }

        internal ComMethodDesc(string name, int dispId)
            : this(dispId)
        {
            // no ITypeInfo constructor
            _name = name;
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

            int cNames;
            string[] rgNames = new string[1 + funcDesc.cParams];
            typeInfo.GetNames(DispId, rgNames, rgNames.Length, out cNames);

            bool skipLast = false;
            if (IsPropertyPut && rgNames[rgNames.Length - 1] == null)
            {
                rgNames[rgNames.Length - 1] = "value";
                cNames++;
                skipLast = true;
            }

            Debug.Assert(cNames == rgNames.Length);
            _name = rgNames[0];

            ParamCount = funcDesc.cParams;

            ReturnType = ComUtil.GetTypeFromTypeDesc(funcDesc.elemdescFunc.tdesc);
            ParameterInformation = ComUtil.GetParameterInformation(funcDesc, skipLast);
        }

        public string Name
        {
            get
            {
                Debug.Assert(_name != null);
                return _name;
            }
        }

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
                // must be regular get
                if (!IsPropertyGet || DispId == ComDispIds.DISPID_NEWENUM)
                {
                    return false;
                }

                // must have no parameters
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

#endif

