/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

#if !SILVERLIGHT // ComObject

using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Marshal = System.Runtime.InteropServices.Marshal;
using VarEnum = System.Runtime.InteropServices.VarEnum;

namespace System.Management.Automation.ComInterop
{
    /// <summary>
    /// The parameter description of a method defined in a type library
    /// </summary>
    internal class ComParamDesc
    {
        # region private fields

        private readonly bool _isOut; // is an output parameter?
        private readonly bool _isOpt; // is an optional parameter?
        private readonly bool _byRef; // is a reference or pointer parameter?
        private readonly bool _isArray;
        private readonly VarEnum _vt;
        private readonly string _name;
        private readonly Type _type;
        private readonly object _defaultValue;

        # endregion

        # region ctor

        /// <summary>
        /// Creates a representation for the paramter of a COM method
        /// </summary>
        internal ComParamDesc(ref ELEMDESC elemDesc, string name)
        {
            // Ensure _defaultValue is set to DBNull.Value regardless of whether or not the 
            // default value is extracted from the parameter description.  Failure to do so
            // yields a runtime exception in the ToString() function.
            _defaultValue = DBNull.Value;

            if (!String.IsNullOrEmpty(name))
            {
                // This is a parameter, not a return value
                _isOut = (elemDesc.desc.paramdesc.wParamFlags & PARAMFLAG.PARAMFLAG_FOUT) != 0;
                _isOpt = (elemDesc.desc.paramdesc.wParamFlags & PARAMFLAG.PARAMFLAG_FOPT) != 0;
                // TODO: The PARAMDESCEX struct has a memory issue that needs to be resolved.  For now, we ignore it.
                //_defaultValue = PARAMDESCEX.GetDefaultValue(ref elemDesc.desc.paramdesc);
            }

            _name = name;
            _vt = (VarEnum)elemDesc.tdesc.vt;
            TYPEDESC typeDesc = elemDesc.tdesc;
            while (true)
            {
                if (_vt == VarEnum.VT_PTR)
                {
                    _byRef = true;
                }
                else if (_vt == VarEnum.VT_ARRAY)
                {
                    _isArray = true;
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
                _byRef = true;
            }

            _type = VarEnumSelector.GetTypeForVarEnum(vtWithoutByref);
        }

        /// <summary>
        /// Creates a representation for the return value of a COM method
        /// TODO: Return values should be represented by a different type
        /// </summary>
        internal ComParamDesc(ref ELEMDESC elemDesc)
            : this(ref elemDesc, String.Empty)
        {
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            if (_isOpt)
            {
                result.Append("[Optional] ");
            }

            if (_isOut)
            {
                result.Append("[out]");
            }

            result.Append(_type.Name);

            if (_isArray)
            {
                result.Append("[]");
            }

            if (_byRef)
            {
                result.Append("&");
            }

            result.Append(" ");
            result.Append(_name);

            if (_defaultValue != DBNull.Value)
            {
                result.Append("=");
                result.Append(_defaultValue.ToString());
            }

            return result.ToString();
        }

        # endregion

        # region properties

        public bool IsOut
        {
            get { return _isOut; }
        }

        public bool IsOptional
        {
            get { return _isOpt; }
        }

        public bool ByReference
        {
            get { return _byRef; }
        }

        public bool IsArray
        {
            get { return _isArray; }
        }

        public Type ParameterType
        {
            get
            {
                return _type;
            }
        }

        /// <summary>
        /// DBNull.Value if there is no default value
        /// </summary>
        internal object DefaultValue
        {
            get
            {
                return _defaultValue;
            }
        }

        # endregion
    }
}

#endif

