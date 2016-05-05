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

namespace Microsoft.PackageManagement.Internal.Utility.Extensions {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using Plugin;
    using Extensions;

    internal static class DelegateExtensions {
        private static readonly Dictionary<Type, Delegate> _emptyDelegates = new Dictionary<Type, Delegate>();

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "shhh.")]
        internal static Type GetDelegateReturnType(this Delegate delegateInstance) {
            return GetDelegateReturnType(delegateInstance.GetType());
        }

        internal static Type GetDelegateReturnType(this Type delegateType) {
            if (delegateType.GetTypeInfo().BaseType != typeof (MulticastDelegate)) {
                throw new ArgumentException("Not a delegate.");
            }

            var invoke = delegateType.GetMethod("Invoke");
            if (invoke == null) {
                throw new ArgumentException("Not a delegate.");
            }
            return invoke.ReturnType;
        }

        internal static IEnumerable<Type> GetDelegateParameterTypes(this Type delegateType) {
            if (delegateType.GetTypeInfo().BaseType != typeof (MulticastDelegate)) {
                throw new ArgumentException("Not a delegate.");
            }

            var invoke = delegateType.GetMethod("Invoke");
            if (invoke == null) {
                throw new ArgumentException("Not a delegate.");
            }

            return invoke.GetParameters().Select(each => each.ParameterType);
        }

        internal static IEnumerable<string> GetDelegateParameterNames(this Type delegateType) {
            if (delegateType.GetTypeInfo().BaseType != typeof (MulticastDelegate)) {
                throw new ArgumentException("Not a delegate.");
            }

            var invoke = delegateType.GetMethod("Invoke");
            if (invoke == null) {
                throw new ArgumentException("Not a delegate.");
            }

            return invoke.GetParameters().Select(each => each.Name);
        }

        internal static Type[] GetParameterTypes(this MethodInfo methodInfo) {
            return methodInfo.GetParameters().Select(each => each.ParameterType).ToArray();
        }

        internal static bool IsDelegateAssignableFromMethod(this Type delegateType, MethodInfo methodInfo) {
            if (delegateType == null || methodInfo == null) {
                return false;
            }

            // are the return types the same?
            if (methodInfo.ReturnType != delegateType.GetDelegateReturnType() && !methodInfo.ReturnType.IsAssignableFrom(delegateType.GetDelegateReturnType())) {
                return false;
            }



            if (!delegateType.GetDelegateParameterTypes().SequenceEqual(methodInfo.GetParameterTypes(), AssignableTypeComparer.Instance)) {
                return false;
            }
            return true;
        }

        internal static bool IsDelegateAssignableFromDelegate(this Type delegateType, Type candidateDelegateType) {
            if (delegateType == null || candidateDelegateType == null) {
                return false;
            }

            // ensure both are actually delegates
            if (delegateType.GetTypeInfo().BaseType != typeof (MulticastDelegate) || candidateDelegateType.GetTypeInfo().BaseType != typeof (MulticastDelegate)) {
                return false;
            }

            // are the return types the same?
            if (candidateDelegateType.GetDelegateReturnType() != delegateType.GetDelegateReturnType() && !delegateType.GetDelegateReturnType().IsAssignableFrom(candidateDelegateType.GetDelegateReturnType())) {
                return false;
            }

            // are all the parameters the same types?
            if (!delegateType.GetDelegateParameterTypes().SequenceEqual(candidateDelegateType.GetDelegateParameterTypes(), AssignableTypeComparer.Instance)) {
                return false;
            }
            return true;
        }

        internal static Delegate CreateEmptyDelegate(this Type delegateType) {
            if (delegateType == null) {
                throw new ArgumentNullException("delegateType");
            }
            if (delegateType.GetTypeInfo().BaseType != typeof (MulticastDelegate)) {
                throw new ArgumentException("must be a delegate", "delegateType");
            }

            return _emptyDelegates.GetOrAdd(delegateType, () => {
                var delegateReturnType = delegateType.GetDelegateReturnType();

                var dynamicMethod = new DynamicMethod(string.Empty, delegateReturnType, delegateType.GetDelegateParameterTypes().ToArray());
                var il = dynamicMethod.GetILGenerator();

                if (delegateReturnType.FullName != "System.Void") {
                    if (delegateReturnType.GetTypeInfo().IsValueType) {
                        il.Emit(OpCodes.Ldc_I4, 0);
                    } else {
                        il.Emit(OpCodes.Ldnull);
                    }
                }
                il.Emit(OpCodes.Ret);
                return dynamicMethod.CreateDelegate(delegateType);
            });
        }
    }
}