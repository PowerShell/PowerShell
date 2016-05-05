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

namespace Microsoft.PackageManagement.Internal.Utility.Plugin {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Extensions;

    public static class WrappedDelegate {
        internal static T CreateProxiedDelegate<T>(this Delegate delegateInstance) {
            return (T)(object)typeof(T).CreateWrappedProxy(delegateInstance);
        }

        internal static T CreateProxiedDelegate<T>(this object instance, MethodInfo method) {
            return (T)(object)CreateProxiedDelegate(instance, method, typeof (T));
        }

        internal static Delegate CreateProxiedDelegate(this object instance, MethodInfo method, Type expectedDelegateType) {
            #region DEAD CODE

            // we need our public delegate to be calling an object that is MarshalByRef
            // instead, we're creating a delegate thats getting bound to the DTI, which isn't
            // extra hoops not needed:
            // var actualDelegate = Delegate.CreateDelegate(expectedDelegateType, duckTypedInstance, method);
            //   var huh = (object)Delegate.CreateDelegate(
            //    proxyDelegateType,
            //    actualDelegate.Target,
            //    actualDelegate.Method,
            //    true);

            #endregion

            // the func/action type for the proxied delegate.
            var proxyDelegateType = GetFuncOrActionType(expectedDelegateType.GetDelegateParameterTypes(), expectedDelegateType.GetDelegateReturnType());

#if CORECLR
            return method.CreateDelegate(proxyDelegateType, instance);
#else
            return Delegate.CreateDelegate(proxyDelegateType, instance, method);
#endif
        }

        internal static object CreateWrappedProxy(this Type expectedDelegateType, Delegate dlg) {
            // the func/action type for the proxied delegate.
            var proxyDelegateType = GetFuncOrActionType(expectedDelegateType.GetDelegateParameterTypes(), expectedDelegateType.GetDelegateReturnType());

#if CORECLR
            MethodInfo method = dlg.GetMethodInfo();
            return (object)method.CreateDelegate(proxyDelegateType, dlg.Target);
#else
            return (object)Delegate.CreateDelegate(
                proxyDelegateType,
                dlg.Target,
                dlg.Method,
                true);
#endif

        }

        public static Type GetFuncOrActionType(IEnumerable<Type> argTypes, Type returnType) {
            return returnType == typeof (void) ? Expression.GetActionType(argTypes.ToArray()) : Expression.GetFuncType(argTypes.ConcatSingleItem(returnType).ToArray());
        }
    }
}