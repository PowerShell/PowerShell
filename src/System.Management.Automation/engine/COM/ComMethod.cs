// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Management.Automation.Internal;
using COM = System.Runtime.InteropServices.ComTypes;

namespace System.Management.Automation
{
    internal class ComMethodInformation : MethodInformation
    {
        internal readonly Type ReturnType;
        internal readonly int DispId;
        internal readonly COM.INVOKEKIND InvokeKind;

        internal ComMethodInformation(bool hasvarargs, bool hasoptional, ParameterInformation[] arguments, Type returnType, int dispId, COM.INVOKEKIND invokekind)
            : base(hasvarargs, hasoptional, arguments)
        {
            this.ReturnType = returnType;
            this.DispId = dispId;
            this.InvokeKind = invokekind;
        }
    }

    /// <summary>
    /// Defines a method in the COM object.
    /// </summary>
    internal class ComMethod
    {
        private Collection<int> _methods = new Collection<int>();
        private COM.ITypeInfo _typeInfo;

        /// <summary>
        /// Initializes new instance of ComMethod class.
        /// </summary>
        internal ComMethod(COM.ITypeInfo typeinfo, string name)
        {
            _typeInfo = typeinfo;
            Name = name;
        }

        /// <summary>
        /// Defines the name of the method.
        /// </summary>
        internal string Name { get; }

        /// <summary>
        /// Updates funcdesc for method information.
        /// </summary>
        /// <param name="index">Index of funcdesc for method in type information.</param>
        internal void AddFuncDesc(int index)
        {
            _methods.Add(index);
        }

        /// <summary>
        /// Returns the different method overloads signatures.
        /// </summary>
        /// <returns></returns>
        internal Collection<string> MethodDefinitions()
        {
            Collection<string> result = new Collection<string>();

            foreach (int index in _methods)
            {
                IntPtr pFuncDesc;

                _typeInfo.GetFuncDesc(index, out pFuncDesc);
                COM.FUNCDESC funcdesc = Marshal.PtrToStructure<COM.FUNCDESC>(pFuncDesc);

                string signature = ComUtil.GetMethodSignatureFromFuncDesc(_typeInfo, funcdesc, false);
                result.Add(signature);

                _typeInfo.ReleaseFuncDesc(pFuncDesc);
            }

            return result;
        }

        /// <summary>
        /// Invokes the method on object.
        /// </summary>
        /// <param name="method">Represents the instance of the method we want to invoke.</param>
        /// <param name="arguments">Parameters to be passed to the method.</param>
        /// <returns>Returns the value of method call.</returns>
        internal object InvokeMethod(PSMethod method, object[] arguments)
        {
            try
            {
                object[] newarguments;
                var methods = ComUtil.GetMethodInformationArray(_typeInfo, _methods, false);
                var bestMethod = (ComMethodInformation)Adapter.GetBestMethodAndArguments(Name, methods, arguments, out newarguments);

                object returnValue = ComInvoker.Invoke(method.baseObject as IDispatch,
                                                       bestMethod.DispId, newarguments,
                                                       ComInvoker.GetByRefArray(bestMethod.parameters,
                                                                                newarguments.Length,
                                                                                isPropertySet: false),
                                                       COM.INVOKEKIND.INVOKE_FUNC);
                Adapter.SetReferences(newarguments, bestMethod, arguments);
                return bestMethod.ReturnType != typeof(void) ? returnValue : AutomationNull.Value;
            }
            catch (TargetInvocationException te)
            {
                // First check if this is a severe exception.
                var innerCom = te.InnerException as COMException;
                if (innerCom == null || innerCom.HResult != ComUtil.DISP_E_MEMBERNOTFOUND)
                {
                    string message = te.InnerException == null ? te.Message : te.InnerException.Message;
                    throw new MethodInvocationException(
                        "ComMethodTargetInvocation",
                        te,
                        ExtendedTypeSystem.MethodInvocationException,
                        method.Name, arguments.Length, message);
                }
            }
            catch (COMException ce)
            {
                if (ce.HResult != ComUtil.DISP_E_UNKNOWNNAME)
                {
                    throw new MethodInvocationException(
                        "ComMethodCOMException",
                        ce,
                        ExtendedTypeSystem.MethodInvocationException,
                        method.Name, arguments.Length, ce.Message);
                }
            }

            return null;
        }
    }
}

