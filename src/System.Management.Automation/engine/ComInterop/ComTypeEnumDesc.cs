// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Dynamic;
using System.Globalization;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace System.Management.Automation.ComInterop
{
    internal sealed class ComTypeEnumDesc : ComTypeDesc, IDynamicMetaObjectProvider
    {
        private readonly string[] _memberNames;
        private readonly object[] _memberValues;

        public override string ToString() => $"<enum '{TypeName}'>";

        internal ComTypeEnumDesc(ComTypes.ITypeInfo typeInfo, ComTypeLibDesc typeLibDesc) : base(typeInfo, typeLibDesc)
        {
            ComTypes.TYPEATTR typeAttr = ComRuntimeHelpers.GetTypeAttrForTypeInfo(typeInfo);
            string[] memberNames = new string[typeAttr.cVars];
            object[] memberValues = new object[typeAttr.cVars];

            IntPtr p;

            // For each enum member get name and value.
            for (int i = 0; i < typeAttr.cVars; i++)
            {
                typeInfo.GetVarDesc(i, out p);

                // Get the enum member value (as object).
                ComTypes.VARDESC varDesc;

                try
                {
                    varDesc = (ComTypes.VARDESC)Marshal.PtrToStructure(p, typeof(ComTypes.VARDESC));

                    if (varDesc.varkind == ComTypes.VARKIND.VAR_CONST)
                    {
                        memberValues[i] = Marshal.GetObjectForNativeVariant(varDesc.desc.lpvarValue);
                    }
                }
                finally
                {
                    typeInfo.ReleaseVarDesc(p);
                }

                // Get the enum member name
                memberNames[i] = ComRuntimeHelpers.GetNameOfMethod(typeInfo, varDesc.memid);
            }

            _memberNames = memberNames;
            _memberValues = memberValues;
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
        {
            return new TypeEnumMetaObject(this, parameter);
        }

        public object GetValue(string enumValueName)
        {
            for (int i = 0; i < _memberNames.Length; i++)
            {
                if (_memberNames[i] == enumValueName)
                {
                    return _memberValues[i];
                }
            }

            throw new MissingMemberException(enumValueName);
        }

        internal bool HasMember(string name)
        {
            for (int i = 0; i < _memberNames.Length; i++)
            {
                if (_memberNames[i] == name)
                    return true;
            }

            return false;
        }

        public string[] GetMemberNames()
        {
            return (string[])_memberNames.Clone();
        }
    }
}
