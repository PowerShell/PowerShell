// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !SILVERLIGHT // ComObject

using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Marshal = System.Runtime.InteropServices.Marshal;
using VarEnum = System.Runtime.InteropServices.VarEnum;

namespace System.Management.Automation.ComInterop
{
    /// <summary>
    /// The parameter description of a method defined in a type library.
    /// </summary>
    internal class ComParamDesc
    {
        #region private fields

        private readonly VarEnum _vt;
        private readonly string _name;

        #endregion

        #region ctor

        /// <summary>
        /// Creates a representation for the parameter of a COM method.
        /// </summary>
        internal ComParamDesc(ref ELEMDESC elemDesc, string name)
        {
            // Ensure _defaultValue is set to DBNull.Value regardless of whether or not the
            // default value is extracted from the parameter description.  Failure to do so
            // yields a runtime exception in the ToString() function.
            DefaultValue = DBNull.Value;

            if (!string.IsNullOrEmpty(name))
            {
                // This is a parameter, not a return value
                IsOut = (elemDesc.desc.paramdesc.wParamFlags & PARAMFLAG.PARAMFLAG_FOUT) != 0;
                IsOptional = (elemDesc.desc.paramdesc.wParamFlags & PARAMFLAG.PARAMFLAG_FOPT) != 0;
                // TODO: The PARAMDESCEX struct has a memory issue that needs to be resolved.  For now, we ignore it.
                // _defaultValue = PARAMDESCEX.GetDefaultValue(ref elemDesc.desc.paramdesc);
            }

            _name = name;
            _vt = (VarEnum)elemDesc.tdesc.vt;
            TYPEDESC typeDesc = elemDesc.tdesc;
            while (true)
            {
                if (_vt == VarEnum.VT_PTR)
                {
                    ByReference = true;
                }
                else if (_vt == VarEnum.VT_ARRAY)
                {
                    IsArray = true;
                }
                else
                {
                    break;
                }

                TYPEDESC childTypeDesc = (TYPEDESC)Marshal.PtrToStructure(typeDesc.lpValue, typeof(TYPEDESC));
                _vt = (VarEnum)childTypeDesc.vt;
                typeDesc = childTypeDesc;
            }

            VarEnum vtWithoutByref = _vt;
            if ((_vt & VarEnum.VT_BYREF) != 0)
            {
                vtWithoutByref = (_vt & ~VarEnum.VT_BYREF);
                ByReference = true;
            }

            ParameterType = VarEnumSelector.GetTypeForVarEnum(vtWithoutByref);
        }

        /// <summary>
        /// Creates a representation for the return value of a COM method
        /// TODO: Return values should be represented by a different type.
        /// </summary>
        internal ComParamDesc(ref ELEMDESC elemDesc)
            : this(ref elemDesc, string.Empty)
        {
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            if (IsOptional)
            {
                result.Append("[Optional] ");
            }

            if (IsOut)
            {
                result.Append("[out]");
            }

            result.Append(ParameterType.Name);

            if (IsArray)
            {
                result.Append("[]");
            }

            if (ByReference)
            {
                result.Append("&");
            }

            result.Append(" ");
            result.Append(_name);

            if (DefaultValue != DBNull.Value)
            {
                result.Append("=");
                result.Append(DefaultValue.ToString());
            }

            return result.ToString();
        }

        #endregion

        #region properties

        public bool IsOut { get; }

        public bool IsOptional { get; }

        public bool ByReference { get; }

        public bool IsArray { get; }

        public Type ParameterType { get; }

        /// <summary>
        /// DBNull.Value if there is no default value.
        /// </summary>
        internal object DefaultValue { get; }

        #endregion
    }
}

#endif

