// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

using COM = System.Runtime.InteropServices.ComTypes;

namespace System.Management.Automation
{
    /// <summary>
    /// Defines a property in the COM object.
    /// </summary>
    internal class ComProperty
    {
        private bool _hasSetter = false;
        private bool _hasSetterByRef = false;
        private int _dispId;
        private int _setterIndex;
        private int _setterByRefIndex;
        private int _getterIndex;
        private readonly COM.ITypeInfo _typeInfo;

        /// <summary>
        /// Initializes a new instance of ComProperty.
        /// </summary>
        /// <param name="typeinfo">Reference to the ITypeInfo of the COM object.</param>
        /// <param name="name">Name of the property being created.</param>
        internal ComProperty(COM.ITypeInfo typeinfo, string name)
        {
            _typeInfo = typeinfo;
            Name = name;
        }

        /// <summary>
        /// Defines the name of the property.
        /// </summary>
        internal string Name { get; }

        private Type _cachedType;

        /// <summary>
        /// Defines the type of the property.
        /// </summary>
        internal Type Type
        {
            get
            {
                _cachedType = null;

                if (_cachedType == null)
                {
                    IntPtr pFuncDesc = IntPtr.Zero;

                    try
                    {
                        _typeInfo.GetFuncDesc(GetFuncDescIndex(), out pFuncDesc);
                        COM.FUNCDESC funcdesc = Marshal.PtrToStructure<COM.FUNCDESC>(pFuncDesc);

                        if (IsGettable)
                        {
                            // use the return type of the getter
                            _cachedType = ComUtil.GetTypeFromTypeDesc(funcdesc.elemdescFunc.tdesc);
                        }
                        else
                        {
                            // use the type of the first argument to the setter
                            ParameterInformation[] parameterInformation = ComUtil.GetParameterInformation(funcdesc, false);
                            Diagnostics.Assert(parameterInformation.Length == 1, "Invalid number of parameters in a property setter");
                            _cachedType = parameterInformation[0].parameterType;
                        }
                    }
                    finally
                    {
                        if (pFuncDesc != IntPtr.Zero)
                        {
                            _typeInfo.ReleaseFuncDesc(pFuncDesc);
                        }
                    }
                }

                return _cachedType;
            }
        }

        /// <summary>
        /// Retrieves the index of the FUNCDESC for the current property.
        /// </summary>
        private int GetFuncDescIndex()
        {
            if (IsGettable)
            {
                return _getterIndex;
            }
            else if (_hasSetter)
            {
                return _setterIndex;
            }
            else
            {
                Diagnostics.Assert(_hasSetterByRef, "Invalid property setter type");
                return _setterByRefIndex;
            }
        }

        /// <summary>
        /// Defines whether the property has parameters or not.
        /// </summary>
        internal bool IsParameterized { get; private set; } = false;

        /// <summary>
        /// Returns the number of parameters in this property.
        /// This is applicable only for parameterized properties.
        /// </summary>
        internal int ParamCount
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Defines whether this property is settable.
        /// </summary>
        internal bool IsSettable
        {
            get
            {
                return _hasSetter || _hasSetterByRef;
            }
        }

        /// <summary>
        /// Defines whether this property is gettable.
        /// </summary>
        internal bool IsGettable { get; private set; } = false;

        /// <summary>
        /// Get value of this property.
        /// </summary>
        /// <param name="target">Instance of the object from which to get the property value.</param>
        /// <returns>Value of the property.</returns>
        internal object GetValue(object target)
        {
            try
            {
                return ComInvoker.Invoke(target as IDispatch, _dispId, null, null, COM.INVOKEKIND.INVOKE_PROPERTYGET);
            }
            catch (TargetInvocationException te)
            {
                var innerCom = te.InnerException as COMException;
                if (innerCom == null || innerCom.HResult != ComUtil.DISP_E_MEMBERNOTFOUND)
                {
                    throw;
                }
            }
            catch (COMException ce)
            {
                if (ce.HResult != ComUtil.DISP_E_UNKNOWNNAME)
                {
                    throw;
                }
            }

            return null;
        }

        /// <summary>
        /// Get value of this property.
        /// </summary>
        /// <param name="target">Instance of the object from which to get the property value.</param>
        /// <param name="arguments">Parameters to get the property value.</param>
        /// <returns>Value of the property</returns>
        internal object GetValue(object target, object[] arguments)
        {
            try
            {
                object[] newarguments;
                var getterCollection = new Collection<int> { _getterIndex };
                var methods = ComUtil.GetMethodInformationArray(_typeInfo, getterCollection, false);
                var bestMethod = (ComMethodInformation)Adapter.GetBestMethodAndArguments(Name, methods, arguments, out newarguments);

                object returnValue = ComInvoker.Invoke(target as IDispatch,
                                                       bestMethod.DispId,
                                                       newarguments,
                                                       ComInvoker.GetByRefArray(bestMethod.parameters,
                                                                                newarguments.Length,
                                                                                isPropertySet: false),
                                                       bestMethod.InvokeKind);
                Adapter.SetReferences(newarguments, bestMethod, arguments);
                return returnValue;
            }
            catch (TargetInvocationException te)
            {
                var innerCom = te.InnerException as COMException;
                if (innerCom == null || innerCom.HResult != ComUtil.DISP_E_MEMBERNOTFOUND)
                {
                    throw;
                }
            }
            catch (COMException ce)
            {
                if (ce.HResult != ComUtil.DISP_E_UNKNOWNNAME)
                {
                    throw;
                }
            }

            return null;
        }

        /// <summary>
        /// Sets value of this property.
        /// </summary>
        /// <param name="target">Instance of the object to which to set the property value.</param>
        /// <param name="setValue">Value to set this property.</param>
        internal void SetValue(object target, object setValue)
        {
            object[] propValue = new object[1];
            setValue = Adapter.PropertySetAndMethodArgumentConvertTo(setValue, this.Type, CultureInfo.InvariantCulture);
            propValue[0] = setValue;

            try
            {
                ComInvoker.Invoke(target as IDispatch, _dispId, propValue, null, COM.INVOKEKIND.INVOKE_PROPERTYPUT);
            }
            catch (TargetInvocationException te)
            {
                var innerCom = te.InnerException as COMException;
                if (innerCom == null || innerCom.HResult != ComUtil.DISP_E_MEMBERNOTFOUND)
                {
                    throw;
                }
            }
            catch (COMException ce)
            {
                if (ce.HResult != ComUtil.DISP_E_UNKNOWNNAME)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Sets the value of the property.
        /// </summary>
        /// <param name="target">Instance of the object to which to set the property value.</param>
        /// <param name="setValue">Value to set this property.</param>
        /// <param name="arguments">Parameters to set this property.</param>
        internal void SetValue(object target, object setValue, object[] arguments)
        {
            object[] newarguments;
            var setterCollection = new Collection<int> { _hasSetterByRef ? _setterByRefIndex : _setterIndex };
            var methods = ComUtil.GetMethodInformationArray(_typeInfo, setterCollection, true);
            var bestMethod = (ComMethodInformation)Adapter.GetBestMethodAndArguments(Name, methods, arguments, out newarguments);

            var finalArguments = new object[newarguments.Length + 1];
            for (int i = 0; i < newarguments.Length; i++)
            {
                finalArguments[i] = newarguments[i];
            }

            finalArguments[newarguments.Length] = Adapter.PropertySetAndMethodArgumentConvertTo(setValue, Type, CultureInfo.InvariantCulture);

            try
            {
                ComInvoker.Invoke(target as IDispatch,
                                  bestMethod.DispId,
                                  finalArguments,
                                  ComInvoker.GetByRefArray(bestMethod.parameters,
                                                           finalArguments.Length,
                                                           isPropertySet: true),
                                  bestMethod.InvokeKind);
                Adapter.SetReferences(finalArguments, bestMethod, arguments);
            }
            catch (TargetInvocationException te)
            {
                var innerCom = te.InnerException as COMException;
                if (innerCom == null || innerCom.HResult != ComUtil.DISP_E_MEMBERNOTFOUND)
                {
                    throw;
                }
            }
            catch (COMException ce)
            {
                if (ce.HResult != ComUtil.DISP_E_UNKNOWNNAME)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Updates the COM property with setter and getter information.
        /// </summary>
        /// <param name="desc">Functional descriptor for property getter or setter.</param>
        /// <param name="index">Index of function descriptor in type information.</param>
        internal void UpdateFuncDesc(COM.FUNCDESC desc, int index)
        {
            _dispId = desc.memid;
            switch (desc.invkind)
            {
                case COM.INVOKEKIND.INVOKE_PROPERTYGET:
                    IsGettable = true;
                    _getterIndex = index;

                    if (desc.cParams > 0)
                    {
                        IsParameterized = true;
                    }

                    break;

                case COM.INVOKEKIND.INVOKE_PROPERTYPUT:
                    _hasSetter = true;
                    _setterIndex = index;

                    if (desc.cParams > 1)
                    {
                        IsParameterized = true;
                    }

                    break;

                case COM.INVOKEKIND.INVOKE_PROPERTYPUTREF:
                    _setterByRefIndex = index;
                    _hasSetterByRef = true;
                    if (desc.cParams > 1)
                    {
                        IsParameterized = true;
                    }

                    break;
            }
        }

        internal string GetDefinition()
        {
            IntPtr pFuncDesc = IntPtr.Zero;

            try
            {
                _typeInfo.GetFuncDesc(GetFuncDescIndex(), out pFuncDesc);
                COM.FUNCDESC funcdesc = Marshal.PtrToStructure<COM.FUNCDESC>(pFuncDesc);

                return ComUtil.GetMethodSignatureFromFuncDesc(_typeInfo, funcdesc, !IsGettable);
            }
            finally
            {
                if (pFuncDesc != IntPtr.Zero)
                {
                    _typeInfo.ReleaseFuncDesc(pFuncDesc);
                }
            }
        }

        /// <summary>
        /// Returns the property signature string.
        /// </summary>
        /// <returns>Property signature.</returns>
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(this.GetDefinition());
            builder.Append(' ');
            if (IsGettable)
            {
                builder.Append("{get} ");
            }

            if (_hasSetter)
            {
                builder.Append("{set} ");
            }

            if (_hasSetterByRef)
            {
                builder.Append("{set by ref}");
            }

            return builder.ToString();
        }
    }
}
