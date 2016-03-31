/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using System.Globalization;
using COM = System.Runtime.InteropServices.ComTypes;

namespace System.Management.Automation
{
    /// <summary>
    /// Defines a property in the  COM object.
    /// </summary>
    internal class ComProperty
    {
        private bool hasSetter = false;
        private bool hasSetterByRef = false;
        private bool hasGetter = false;
        private int dispId;
        private int setterIndex;
        private int setterByRefIndex;
        private int getterIndex;
        private COM.ITypeInfo typeInfo;
        private string name;
        private bool isparameterizied = false;


        /// <summary>
        /// Initializes a new instance of ComProperty.
        /// </summary>
        /// <param name="typeinfo">reference to the ITypeInfo of the COM object</param>
        /// <param name="name">name of the property being created.</param>
        internal ComProperty(COM.ITypeInfo typeinfo, string name)
        {
            this.typeInfo = typeinfo;
            this.name = name;
        }

        /// <summary>
        ///  Defines the name of the property.
        /// </summary>
        internal string Name
        {
            get
            {
                return name;
            }
        }

        private Type cachedType;

        /// <summary>
        ///  Defines the type of the property.
        /// </summary>
        internal Type Type
        {
            get
            {
                this.cachedType = null;

                if (this.cachedType == null)
                {
                    IntPtr pFuncDesc = IntPtr.Zero;

                    try
                    {
                        this.typeInfo.GetFuncDesc(GetFuncDescIndex(), out pFuncDesc);
                        COM.FUNCDESC funcdesc = ClrFacade.PtrToStructure<COM.FUNCDESC>(pFuncDesc);

                        if (this.hasGetter)
                        {
                            // use the return type of the getter
                            this.cachedType = ComUtil.GetTypeFromTypeDesc(funcdesc.elemdescFunc.tdesc);
                        }
                        else
                        {
                            // use the type of the first argument to the setter
                            ParameterInformation[] parameterInformation = ComUtil.GetParameterInformation(funcdesc, false);
                            Diagnostics.Assert(parameterInformation.Length == 1, "Invalid number of parameters in a property setter");
                            this.cachedType = parameterInformation[0].parameterType;
                        }
                    }
                    finally
                    {
                        if (pFuncDesc != IntPtr.Zero)
                        {
                            typeInfo.ReleaseFuncDesc(pFuncDesc);
                        }
                    }
                }

                return this.cachedType;
            }
        }

        /// <summary>
        /// Retrieves the index of the FUNCDESC for the current property.
        /// </summary>
        private int GetFuncDescIndex()
        {
            if (this.hasGetter)
            {
                return this.getterIndex;
            }
            else if (this.hasSetter)
            {
                return this.setterIndex;
            }
            else
            {
                Diagnostics.Assert(this.hasSetterByRef, "Invalid property setter type");
                return this.setterByRefIndex;
            }
        }

        /// <summary>
        ///  Defines whether the property has parameters or not.
        /// </summary>
        internal bool IsParameterized
        {
            get
            {
                return isparameterizied;
            }
        }


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
        ///  Defines whether this property is settable.
        /// </summary>
        internal bool IsSettable
        {
            get
            {
                return hasSetter | hasSetterByRef;
            }
        }

        /// <summary>
        ///  Defines whether this property is gettable.
        /// </summary>
        internal bool IsGettable
        {
            get
            {
                return hasGetter;
            }
        }


        /// <summary>
        /// Get value of this property
        /// </summary>
        /// <param name="target">instance of the object from which to get the property value</param>
        /// <returns>value of the property</returns>
        internal object GetValue(Object target)
        {
            try
            {
                return ComInvoker.Invoke(target as IDispatch, dispId, null, null, COM.INVOKEKIND.INVOKE_PROPERTYGET);
            }
            catch (TargetInvocationException te)
            {

                //First check if this is a severe exception.
                CommandProcessorBase.CheckForSevereException(te.InnerException);
                
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
        /// Get value of this property
        /// </summary>
        /// <param name="target">instance of the object from which to get the property value</param>
        /// <param name="arguments">parameters to get the property value</param>
        /// <returns>value of the property</returns>
        internal object GetValue(Object target, Object[] arguments)
        {
            try
            {
                object[] newarguments;
                var getterCollection = new Collection<int> {this.getterIndex};
                var methods = ComUtil.GetMethodInformationArray(this.typeInfo, getterCollection, false);
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
                //First check if this is a severe exception.
                CommandProcessorBase.CheckForSevereException(te.InnerException);

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
        /// <param name="target">instance of the object to which to set the property value</param>
        /// <param name="setValue">value to set this property</param>
        internal void SetValue(Object target, Object setValue)
        {
            object[] propValue = new object[1];
            setValue = Adapter.PropertySetAndMethodArgumentConvertTo(setValue, this.Type, CultureInfo.InvariantCulture);
            propValue[0] = setValue;
            
            try
            {
                ComInvoker.Invoke(target as IDispatch, dispId, propValue, null, COM.INVOKEKIND.INVOKE_PROPERTYPUT);
            }
            catch (TargetInvocationException te)
            {
                //First check if this is a severe exception.
                CommandProcessorBase.CheckForSevereException(te.InnerException);
                
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
        /// <param name="target">instance of the object to which to set the property value</param>
        /// <param name="setValue">value to set this property</param>
        /// <param name="arguments">parameters to set this property.</param>
        internal void SetValue(Object target, Object setValue, Object[] arguments)
        {
            object[] newarguments;
            var setterCollection = new Collection<int> { hasSetterByRef ? setterByRefIndex : setterIndex };
            var methods = ComUtil.GetMethodInformationArray(this.typeInfo, setterCollection, true);
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
                //First check if this is a severe exception.
                CommandProcessorBase.CheckForSevereException(te.InnerException);
              
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
        /// <param name="desc">functional descriptor for property getter or setter</param>
        /// <param name="index">index of function descriptor in type information</param>
        internal void UpdateFuncDesc(COM.FUNCDESC desc, int index)
        {
            dispId = desc.memid;
            switch (desc.invkind)
            {
                case COM.INVOKEKIND.INVOKE_PROPERTYGET:
                    hasGetter = true;
                    getterIndex = index;

                    if (desc.cParams > 0)
                    {
                        isparameterizied = true;
                    }
                    break;

                case COM.INVOKEKIND.INVOKE_PROPERTYPUT:
                    hasSetter = true;
                    setterIndex = index;

                    if (desc.cParams > 1)
                    {
                        isparameterizied = true;
                    }
                    break;

                case COM.INVOKEKIND.INVOKE_PROPERTYPUTREF:
                    setterByRefIndex = index;
                    hasSetterByRef = true;
                    if (desc.cParams > 1)
                    {
                        isparameterizied = true;
                    }
                    break;
            }
        }

        internal string GetDefinition()
        {
            IntPtr pFuncDesc = IntPtr.Zero;

            try
            {
                this.typeInfo.GetFuncDesc(GetFuncDescIndex(), out pFuncDesc);
                COM.FUNCDESC funcdesc = ClrFacade.PtrToStructure<COM.FUNCDESC>(pFuncDesc);

                return ComUtil.GetMethodSignatureFromFuncDesc(typeInfo, funcdesc, !this.hasGetter);
            }
            finally
            {
                if (pFuncDesc != IntPtr.Zero)
                {
                    typeInfo.ReleaseFuncDesc(pFuncDesc);
                }
            }
        }

        /// <summary>
        /// Returns the property signature string
        /// </summary>
        /// <returns>property signature</returns>
        public override string  ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(this.GetDefinition());
            builder.Append(" ");
            if (hasGetter)
            {
                builder.Append("{get} ");
            }
            if (hasSetter)
            {
                builder.Append("{set} ");
            }

            if (hasSetterByRef)
            {
                builder.Append("{set by ref}");
            }

            return builder.ToString();
        }
  
    }
}
