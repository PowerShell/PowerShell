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
    using System.Reflection;
    using Async;
    using Collections;
    using Extensions;
    using Api;

    internal static class DynamicInterface {
        internal delegate void OnUnhandledException(string method, Exception exception);

        private static readonly Dictionary<Types, bool> _isCreatableFromTypesCache = new Dictionary<Types, bool>();
        private static readonly Dictionary<Types, bool> _isCastableFromTypesCache = new Dictionary<Types, bool>();

        public static TInterface Create<TInterface>(params Type[] types) {
            return (TInterface)typeof (TInterface).Create(types);
        }

        public static TInterface Create<TInterface>(this Type type) {
            return (TInterface)typeof (TInterface).Create(type);
        }

        public static object Create(this Type tInterface, params Type[] types) {
            if (tInterface == null) {
                throw new ArgumentNullException("tInterface");
            }

            types = types ?? new Type[0];

            if (!tInterface.GetVirtualMethods().Any()) {
                throw new Exception("Interface Type '{0}' doesn not have any virtual or abstract methods".format(tInterface.FullNiceName()));
            }

            if (!tInterface.CanCreateFrom(types)) {
                var missing = GetMissingMethods(tInterface, types).ToArray();
                var badctors = FilterOnMissingDefaultConstructors(types).ToArray();

                var msg = badctors.Length == 0 ? ""
                    : "\r\nTypes ({0}) do not support a Default Constructor\r\n".format(badctors.Select(each => each.FullName).Quote().JoinWithComma());

                msg += missing.Length == 0 ? "" :
                    "\r\nTypes ({0}) are missing the following methods from interface ('{1}'):\r\n  {2}".format(
                        types.Select(each => each.FullName).Quote().JoinWithComma(),
                        tInterface.FullNiceName(),
                        missing.Select(each => each.ToSignatureString()).Quote().JoinWith("\r\n  "));

                throw new Exception(msg);
            }

            // create actual instance
            return CreateProxy(tInterface, types.Select(Activator.CreateInstance).ToArray());
        }

        private static IEnumerable<object> Flatten(IEnumerable<object> items) {
            if (items == null) {
                yield break;
            }

            if (items is IAsyncAction) {
                yield return items;
                yield break;
            }

            foreach (var item in items) {
                if (item is object[] || item is IEnumerable<object>) {
                    foreach (var inner in Flatten(item as IEnumerable<object>)) {
                        if (inner != null) {
                            yield return inner;
                        }
                    }
                    continue;
                }
                yield return item;
            }
        }

        private static IEnumerable<object> Flatten(params object[] items) {
            return Flatten(items as IEnumerable<object>);
        }

        public static TInterface DynamicCast<TInterface>(params object[] instances) {
            return (TInterface)DynamicCast(typeof (TInterface), instances);
        }

        private static object DynamicCast(Type tInterface, params object[] instances) {
            if (tInterface == null) {
                throw new ArgumentNullException("tInterface");
            }

            if (instances.Length == 0) {
                throw new ArgumentException("No instances given", "instances");
            }

            if (instances.Length == 1) {
                // shortcut for string coercion
                if (tInterface == typeof (string)) {
                    return instances[0] == null ? null : instances[0].ToString();
                }

                var objects = instances[0] as IEnumerable<object>;
                if (objects != null) {
                    // if the tInterface is an IEnumerable<T>
                    // then we'll just dynamic cast the items in the collection to the target type.
                    if (tInterface.IsIEnumerableT()) {
                        var elementType = tInterface.GetGenericArguments().FirstOrDefault();

                        if (elementType != null) {
                            return objects.Select(each => DynamicCast(elementType, each)).ToIEnumerableT(elementType);
                        }
                    }

                    if (tInterface.IsArray && tInterface.GetArrayRank() == 1) {
                        var elementType = tInterface.GetElementType();
                        if (elementType != null) {
                            return objects.Select(each => DynamicCast(elementType, each)).ToArrayT(elementType);
                        }
                    }
                }
            }

            if (!tInterface.GetVirtualMethods().Any()) {
                throw new Exception("Interface Type '{0}' doesn not have any virtual or abstract methods".format(tInterface.FullNiceName()));
            }
            instances = Flatten(instances).ToArray();

            if (instances.Any(each => each == null)) {
                throw new ArgumentException("One or more instances are null", "instances");
            }

            // shortcut if the interface is already implemented in the object.
            if (instances.Length == 1 && tInterface.IsInstanceOfType(instances[0])) {
                return instances[0];
            }

            if (!tInterface.CanDynamicCastFrom(instances)) {
                var missing = GetMethodsMissingFromInstances(tInterface, instances);
                var msg = "\r\nObjects are missing the following methods from interface ('{0}'):\r\n  {1}".format(
                    tInterface.FullNiceName(),
                    missing.Select(each => each.ToSignatureString()).Quote().JoinWith("\r\n  "));

                throw new Exception(msg);
            }

            return CreateProxy(tInterface, instances);
        }

        public static bool CanCreateFrom(this Type tInterface, params Type[] types) {
            return _isCreatableFromTypesCache.GetOrAdd(new Types(tInterface, types), () => {
                // if there isn't a default constructor, we can't use that type to create instances
                return types.All(actualType => actualType.GetDefaultConstructor() != null) && CanDynamicCastFrom(tInterface, types);
            });
        }

        public static bool CanCreateFrom<TInterface>(params Type[] types) {
            return CanCreateFrom(typeof (TInterface), types);
        }

        public static bool CanDynamicCastFrom(this Type tInterface, params Type[] types) {
            return _isCastableFromTypesCache.GetOrAdd(new Types(tInterface, types), () => !GetMissingMethods(tInterface, types).Any());
        }

        private static IEnumerable<Type> FilterOnMissingDefaultConstructors(params Type[] types) {
            return types.Where(actualType => actualType.GetDefaultConstructor() == null);
        }

        private static IEnumerable<MethodInfo> GetMissingMethods(Type tInterface, params Type[] types) {
            return tInterface.GetRequiredMethods().Where(method => types.GetPublicMethods().FindMethod(method) == null);
        }

        public static bool CanDynamicCastFrom(this Type tInterface, params object[] instances) {
            if (tInterface == null) {
                throw new ArgumentNullException("tInterface");
            }

            if (instances == null) {
                throw new ArgumentNullException("instances");
            }

            if (instances.Length == 0) {
                throw new ArgumentException("No instances given", "instances");
            }

            if (instances.Any(each => each == null)) {
                throw new ArgumentException("One or more instances are null", "instances");
            }

            // this will be faster if this type has been checked before.
            if (CanCreateFrom(tInterface, instances.Select(each => each.GetType()).ToArray())) {
                return true;
            }

#if DEEPDEBUG
            var missing = GetMethodsMissingFromInstances(tInterface,instances).ToArray();

            if (missing.Length > 0 ) {
                var msg = "\r\nObjects are missing the following methods from interface ('{0}'):\r\n  {1}".format(
                    tInterface.FullNiceName(),
                    missing.Select(each => each.ToSignatureString()).Quote().JoinWith("\r\n  "));
                Debug.WriteLine(msg);
            }
#endif
            // see if any specified object has something for every required method.
            return !instances.Aggregate((IEnumerable<MethodInfo>)tInterface.GetRequiredMethods(), GetMethodsMissingFromInstance).Any();
        }

        private static IEnumerable<MethodInfo> GetMethodsMissingFromInstances(Type tInterface, params object[] instances) {
            return instances.Aggregate((IEnumerable<MethodInfo>)tInterface.GetRequiredMethods(), GetMethodsMissingFromInstance);
        }

        private static IEnumerable<MethodInfo> GetMethodsMissingFromInstance(IEnumerable<MethodInfo> methods, object instance) {
            var instanceSupportsMethod = DynamicInterfaceExtensions.GenerateInstanceSupportsMethod(instance);
            var instanceType = instance.GetType();

            var instanceMethods = instanceType.GetPublicMethods();
            var instanceFields = instanceType.GetPublicDelegateFields();
            var instanceProperties = instanceType.GetPublicDelegateProperties();

            // later, we can check for a delegate-creation function that can deliver a delegate to us by name and parameter types.
            // currently, we don't need that, so we're not going to implement it right away.

            return methods.Where(method =>
                !instanceSupportsMethod(method.Name) || (
                    instanceMethods.FindMethod(method) == null &&
                    instanceFields.FindDelegate(instance, method) == null &&
                    instanceProperties.FindDelegate(instance, method) == null
                    ));
        }

        private static object CreateProxy(Type tInterface, params object[] instances) {
            var matrix = instances.SelectMany(instance => {
                var instanceType = instance.GetType();
                // get all the public interfaces for the instances and place them at the top
                // and let it try to bind against those.
                return instanceType.GetInterfaces().Where(each => each.GetTypeInfo().IsPublic).Select(each => new {
                    instance,
                    SupportsMethod = DynamicInterfaceExtensions.GenerateInstanceSupportsMethod(instance),
                    Type = each,
                    Methods = each.GetPublicMethods(),
                    Fields = each.GetPublicDelegateFields(),
                    Properties = each.GetPublicDelegateProperties()
                }).ConcatSingleItem(new {
                    instance,
                    SupportsMethod = DynamicInterfaceExtensions.GenerateInstanceSupportsMethod(instance),
                    Type = instanceType,
                    Methods = instanceType.GetPublicMethods(),
                    Fields = instanceType.GetPublicDelegateFields(),
                    Properties = instanceType.GetPublicDelegateProperties()
                });
            }).ToArray();

            var instanceMethods = new OrderedDictionary<Type, List<MethodInfo, MethodInfo>>();
            var delegateMethods = new List<Delegate, MethodInfo>();
            var stubMethods = new List<MethodInfo>();
            var usedInstances = new List<Type, object>();

            foreach (var method in tInterface.GetVirtualMethods()) {
                // figure out where it's going to get implemented
                var found = false;
                foreach (var instance in matrix) {
                    if (method.Name == "IsMethodImplemented") {
                        // skip for now, we'll implement this at the end
                        found = true;
                        break;
                    }

                    if (instance.SupportsMethod(method.Name)) {
                        var instanceMethod = instance.Methods.FindMethod(method);
                        if (instanceMethod != null) {
                            instanceMethods.GetOrAdd(instance.Type, () => new List<MethodInfo, MethodInfo>()).Add(method, instanceMethod);
                            if (!usedInstances.Contains(instance.Type, instance.instance)) {
                                usedInstances.Add(instance.Type, instance.instance);
                            }
                            found = true;
                            break;
                        }

                        var instanceDelegate = instance.Fields.FindDelegate(instance.instance, method) ?? instance.Properties.FindDelegate(instance.instance, method);
                        if (instanceDelegate != null) {
                            delegateMethods.Add(instanceDelegate, method);
                            found = true;
                            break;
                        }
                    }
                }
                if (!found && (tInterface.GetTypeInfo().IsInterface || method.IsAbstract)) {
#if xDEEPDEBUG
                    Console.WriteLine(" Generating stub method for {0} -> {1}".format(tInterface.NiceName(), method.ToSignatureString()));
#endif
                    stubMethods.Add(method);
                }
            }

            return DynamicType.Create(tInterface, instanceMethods, delegateMethods, stubMethods, usedInstances);
        }

        public static IEnumerable<Type> FindCompatibleTypes<TInterface>(this Assembly assembly) {
            return assembly == null ? Enumerable.Empty<Type>() : assembly.CreatableTypes().Where(each => CanCreateFrom<TInterface>(each));
        }

    }
}