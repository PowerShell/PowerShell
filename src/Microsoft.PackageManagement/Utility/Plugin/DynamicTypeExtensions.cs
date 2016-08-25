// 
//  Copyright (c) Microsoft Corporation. All rights reserved. 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//  

namespace Microsoft.PackageManagement.Internal.Utility.Plugin
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using Extensions;

    internal static class DynamicTypeExtensions
    {
        private static readonly Type[] _emptyTypes = {
        };

        private static MethodInfo _asMethod;

        private static MethodInfo AsMethod
        {
            get
            {
                var methods = typeof(DynamicInterfaceExtensions).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (_asMethod == null)
                {
                    _asMethod = methods.FirstOrDefault(method => String.Equals(method.Name, "As", StringComparison.OrdinalIgnoreCase)
                        && method.GetParameterTypes().Count() == 1 && method.GetParameterTypes().First() == typeof(Object));
                }

                return _asMethod;
            }
        }

        internal static void OverrideInitializeLifetimeService(this TypeBuilder dynamicType)
        {
            // add override of InitLifetimeService so this object doesn't fall prey to timeouts
            var il = dynamicType.DefineMethod("InitializeLifetimeService", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, CallingConventions.HasThis, typeof(object), _emptyTypes).GetILGenerator();

            il.LoadNull();
            il.Return();
        }

        internal static void GenerateIsMethodImplemented(this TypeBuilder dynamicType)
        {
            // special case -- the IsMethodImplemented method can give the interface owner information as to
            // which methods are actually implemented.
            var implementedMethodsField = dynamicType.DefineField("__implementedMethods", typeof(HashSet<string>), FieldAttributes.Private);

            var il = dynamicType.CreateMethod("IsMethodImplemented", typeof(bool), typeof(string));

            il.LoadThis();
            il.LoadField(implementedMethodsField);
            il.LoadArgument(1);
            il.CallVirutal(typeof(HashSet<string>).GetMethod("Contains"));
            il.Return();
        }

        internal static void GenerateMethodForDirectCall(this TypeBuilder dynamicType, MethodInfo method, FieldBuilder backingField, MethodInfo instanceMethod, MethodInfo onUnhandledException)
        {
            var il = dynamicType.CreateMethod(method);
            // the target object has a method that matches.
            // let's use that.

            var hasReturn = method.ReturnType != typeof(void);
            var hasOue = onUnhandledException != null;

            var exit = il.DefineLabel();
            var setDefaultReturn = il.DefineLabel();

            var ret = hasReturn ? il.DeclareLocal(method.ReturnType) : null;
            var exc = hasOue ? il.DeclareLocal(typeof(Exception)) : null;

            il.BeginExceptionBlock();

            il.LoadThis();
            il.LoadField(backingField);

            var imTypes = instanceMethod.GetParameterTypes();
            var dmTypes = method.GetParameterTypes();

            for (var i = 0; i < dmTypes.Length; i++)
            {
                il.LoadArgument(i + 1);

                // if the types are assignable,
                if (imTypes[i].IsAssignableFrom(dmTypes[i]))
                {
                    // it assigns straight across.
                }
                else
                {
                    // it doesn't, we'll ducktype it.
                    if (dmTypes[i].GetTypeInfo().IsPrimitive)
                    {
                        // box it first?
                        il.Emit(OpCodes.Box, dmTypes[i]);
                    }
                    il.Call(AsMethod.MakeGenericMethod(imTypes[i]));
                }
            }

            // call the actual method implementation
            il.CallVirutal(instanceMethod);

            if (hasReturn)
            {
                // copy the return value in the return
                // check to see if we need to ducktype the return value here.
                if (method.ReturnType.IsAssignableFrom(instanceMethod.ReturnType))
                {
                    // it can store it directly.
                }
                else
                {
                    // it doesn't assign directly, let's ducktype it.
                    if (instanceMethod.ReturnType.GetTypeInfo().IsPrimitive)
                    {
                        il.Emit(OpCodes.Box, instanceMethod.ReturnType);
                    }
                    il.Call(AsMethod.MakeGenericMethod(method.ReturnType));
                }
                il.StoreLocation(ret);
            }
            else
            {
                // this method isn't returning anything.
                if (instanceMethod.ReturnType != typeof(void))
                {
                    // pop the return value because the generated method is void and the
                    // method we called actually gave us a result.
                    il.Emit(OpCodes.Pop);
                }
            }
            il.Emit(OpCodes.Leave_S, exit);

            il.BeginCatchBlock(typeof(Exception));
            if (hasOue)
            {
                // we're going to call the handler.
                il.StoreLocation(exc.LocalIndex);
                il.LoadArgument(0);
                il.Emit(OpCodes.Ldstr, instanceMethod.ToSignatureString());
                il.LoadLocation(exc.LocalIndex);
                il.Call(onUnhandledException);
                il.Emit(OpCodes.Leave_S, setDefaultReturn);
            }
            else
            {
                // suppress the exception quietly
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Leave_S, setDefaultReturn);
            }
            il.EndExceptionBlock();

            // if we can't return the appropriate value, we're returning default(T)
            il.MarkLabel(setDefaultReturn);
            SetDefaultReturnValue(il, method.ReturnType);
            il.Return();

            // looks like we're returning the value that we got back from the implementation.
            il.MarkLabel(exit);
            if (hasReturn)
            {
                il.LoadLocation(ret.LocalIndex);
            }

            il.Return();
        }

        internal static ILGenerator CreateMethod(this TypeBuilder dynamicType, MethodInfo method)
        {
            return dynamicType.CreateMethod(method.Name, method.ReturnType, method.GetParameterTypes());
        }

        internal static ILGenerator CreateMethod(this TypeBuilder dynamicType, string methodName, Type returnType, params Type[] parameterTypes)
        {
            var methodBuilder = dynamicType.DefineMethod(methodName, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, CallingConventions.HasThis, returnType, parameterTypes);

            return methodBuilder.GetILGenerator();
        }

        internal static void GenerateMethodForDelegateCall(this TypeBuilder dynamicType, MethodInfo method, FieldBuilder field, MethodInfo onUnhandledException)
        {
            var il = dynamicType.CreateMethod(method);

            // the target object has a property or field that matches the signature we're looking for.
            // let's use that.

            var delegateType = WrappedDelegate.GetFuncOrActionType(method.GetParameterTypes(), method.ReturnType);

            il.LoadThis();
            il.LoadField(field);
            for (var i = 0; i < method.GetParameterTypes().Length; i++)
            {
                il.LoadArgument(i + 1);
            }
            il.CallVirutal(delegateType.GetMethod("Invoke"));
            il.Return();
        }

        internal static void GenerateStubMethod(this TypeBuilder dynamicType, MethodInfo method)
        {
            var il = dynamicType.CreateMethod(method);
            do
            {
                if (method.ReturnType != typeof(void))
                {
                    if (method.ReturnType.GetTypeInfo().IsPrimitive)
                    {
                        if (method.ReturnType == typeof(double))
                        {
                            il.LoadDouble(0.0);
                            break;
                        }

                        if (method.ReturnType == typeof(float))
                        {
                            il.LoadFloat(0.0F);
                            break;
                        }

                        il.LoadInt32(0);

                        if (method.ReturnType == typeof(long) || method.ReturnType == typeof(ulong))
                        {
                            il.ConvertToInt64();
                        }

                        break;
                    }

                    if (method.ReturnType.GetTypeInfo().IsEnum)
                    {
                        // should really find out the actual default?
                        il.LoadInt32(0);
                        break;
                    }

                    if (method.ReturnType.GetTypeInfo().IsValueType)
                    {
                        var result = il.DeclareLocal(method.ReturnType);
                        il.LoadLocalAddress(result);
                        il.InitObject(method.ReturnType);
                        il.LoadLocation(0);
                        break;
                    }

                    il.LoadNull();
                }
            } while (false);
            il.Return();
        }

        private static void SetDefaultReturnValue(ILGenerator il, Type returnType)
        {
            if (returnType != typeof(void))
            {
                if (returnType.GetTypeInfo().IsPrimitive)
                {
                    if (returnType == typeof(double))
                    {
                        il.LoadDouble(0.0);
                        return;
                    }

                    if (returnType == typeof(float))
                    {
                        il.LoadFloat(0.0F);
                        return;
                    }

                    il.LoadInt32(0);

                    if (returnType == typeof(long) || returnType == typeof(ulong))
                    {
                        il.ConvertToInt64();
                    }

                    return;
                }

                if (returnType.GetTypeInfo().IsEnum)
                {
                    // should really find out the actual default?
                    il.LoadInt32(0);
                    return;
                }

                if (returnType.GetTypeInfo().IsValueType)
                {
                    var result = il.DeclareLocal(returnType);
                    il.LoadLocalAddress(result);
                    il.InitObject(returnType);
                    il.LoadLocation(result.LocalIndex);
                    return;
                }

                // otherwise load null.
                il.LoadNull();
            }
        }
    }
}