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
    using System.Reflection.Emit;
    using Collections;
    using Extensions;

    internal class DynamicType {
        private static int _typeCounter = 1;
        private static readonly Dictionary<string, DynamicType> _proxyClassDefinitions = new Dictionary<string, DynamicType>();

        private readonly TypeBuilder _dynamicType;
        private readonly HashSet<string> _implementedMethods = new HashSet<string>();
        private readonly List<FieldBuilder> _storageFields = new List<FieldBuilder>();
#if DEEP_DEBUG || CORECLR
        private AssemblyBuilder _dynamicAssembly;
#else
        private static AssemblyBuilder _dynamicAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("DynamicClasses"), AssemblyBuilderAccess.Run);
#endif

#if DEEP_DEBUG
        private string _directory;
        private string _filename;
        private string _fullpath;
#endif
        private string _proxyName;
        private Type _type;
        private MethodInfo OnUnhandledException;

        internal static object Create(Type tInterface, OrderedDictionary<Type, List<MethodInfo, MethodInfo>> instanceMethods, List<Delegate, MethodInfo> delegateMethods, List<MethodInfo> stubMethods, List<Type, object> usedInstances) {
            // now we can calculate the key based on the content of the *Methods collections
            var key = tInterface.GetTypeInfo().Assembly.FullName + "::" + tInterface.Name + ":::" + instanceMethods.Keys.Select(each => each.GetTypeInfo().Assembly.FullName + "." + each.FullName + "." + instanceMethods[each].Select(mi => mi.Value.ToSignatureString()).JoinWithComma()).JoinWith(";\r\n") +
                      "::" + delegateMethods.Select(each => each.GetType().FullName).JoinWith(";\r\n") +
                      "::" + stubMethods.Select(mi => mi.ToSignatureString()).JoinWithComma();
            // + "!->" + (onUnhandledExceptionMethod == null ? (onUnhandledExceptionDelegate == null ? "GenerateOnUnhandledException" : onUnhandledExceptionDelegate.ToString()) : onUnhandledExceptionMethod.ToSignatureString());

            return _proxyClassDefinitions.GetOrAdd(key, () => new DynamicType(tInterface, instanceMethods, delegateMethods, stubMethods)).CreateInstance(usedInstances, delegateMethods);
        }

        private DynamicType(Type interfaceType, OrderedDictionary<Type, List<MethodInfo, MethodInfo>> methods, List<Delegate, MethodInfo> delegates, List<MethodInfo> stubs) {
            var counter = 0;

            _dynamicType = DefineDynamicType(interfaceType);

            // if the declaring type contains an OUE method declaration, then the host is permitting the plugin to see their oewn unhandled exceptions for debugging purposes.
            OnUnhandledException = interfaceType.GetVirtualMethods().FirstOrDefault(each => each.Name == "OnUnhandledException" && each.GetParameterTypes().SequenceEqual(new Type[] {typeof (string), typeof (Exception)}));

            foreach (var instanceType in methods.Keys) {
                // generate storage for object
                var field = _dynamicType.DefineField("_instance_{0}".format(++counter), instanceType, FieldAttributes.Private);
                _storageFields.Add(field);

                // create methods

                foreach (var method in methods[instanceType]) {
                    _dynamicType.GenerateMethodForDirectCall(method.Key, field, method.Value, OnUnhandledException);
                    _implementedMethods.Add(method.Key.Name);
                }
            }

            foreach (var d in delegates) {
                var field = _dynamicType.DefineField("_delegate_{0}".format(++counter), d.Key.GetType(), FieldAttributes.Private);
                _storageFields.Add(field);
                _implementedMethods.Add(d.Value.Name);

                _dynamicType.GenerateMethodForDelegateCall(d.Value, field, OnUnhandledException);
            }

            foreach (var method in stubs) {
                // did not find a matching method or signature, or the instance told us that it doesn't actually support it
                // that's ok, if we get here, it must not be a required method.
                // we'll implement a placeholder method for it.
                _dynamicType.GenerateStubMethod(method);
            }

            _dynamicType.GenerateIsMethodImplemented();

            // generate the constructor for the class.
            DefineConstructor(interfaceType.GetTypeInfo().IsInterface ? typeof (Object) : interfaceType);
        }

        internal Type Type {
            get {
                lock (_proxyClassDefinitions) {
                    try {
                        if (_type == null) {
#if !CORECLR
                            _type = _dynamicType.CreateType();
#else
                            _type = _dynamicType.CreateTypeInfo().AsType();
#endif
#if DEEP_DEBUG
                            _dynamicAssembly.Save(_filename);
#endif
                        }
                        return _type;
                    } catch (Exception e) {
                        e.Dump();
                        throw;
                    }
                }
            }
        }

        internal object CreateInstance(List<Type, object> instances, List<Delegate, MethodInfo> delegates) {
            var proxyConstructor = Type.GetConstructors()[0];
            var instance = proxyConstructor.Invoke(instances.Select(each => each.Value).Concat(delegates.Select(each => each.Key)).ToArray());
            // set the implemented methods collection
            var imf = Type.GetField("__implementedMethods", BindingFlags.NonPublic | BindingFlags.Instance);
            if (imf != null) {
                imf.SetValue(instance, _implementedMethods);
            }

            return instance;
        }

        private TypeBuilder DefineDynamicType(Type interfaceType) {
            lock (_proxyClassDefinitions) {
                _proxyName = "{0}_proxy_{1}".format(interfaceType.NiceName().MakeSafeFileName(), _typeCounter++);
            }
#if CORECLR
            _dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(_proxyName), AssemblyBuilderAccess.Run);
#endif

#if DEEP_DEBUG
             _fullpath = (_proxyName + ".dll").GenerateTemporaryFilename();
             _directory = Path.GetDirectoryName(_fullpath);
             _filename = Path.GetFileName(_fullpath);
            _dynamicAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(_proxyName), AssemblyBuilderAccess.RunAndSave, _directory);
            var dynamicModule = _dynamicAssembly.DefineDynamicModule(_proxyName, _filename);
#else
            var dynamicModule = _dynamicAssembly.DefineDynamicModule(_proxyName);
#endif

            // Define a runtime class with specified name and attributes.
            if (interfaceType.GetTypeInfo().IsInterface) {
                var dynamicType = dynamicModule.DefineType(_proxyName, TypeAttributes.Public, typeof (Object));
                dynamicType.AddInterfaceImplementation(interfaceType);
                return dynamicType;
            } else {
                var dynamicType = dynamicModule.DefineType(_proxyName, TypeAttributes.Public, interfaceType);
                return dynamicType;
            }
        }

        internal void DefineConstructor(Type parentClassType) {
            // add constructor that takes the specific type of object that we're going to bind

            var types = _storageFields.Select(each => each.FieldType).ToArray();

            var constructor = _dynamicType.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, types);

            var il = constructor.GetILGenerator();

            if (parentClassType != null) {
#if !CORECLR
                var basector = parentClassType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new Type[0], null);
#else
                var constructors = parentClassType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                var basector = constructors.FirstOrDefault(cons => cons.GetParameters() == null || cons.GetParameters().Length == 0);
#endif
                if (basector != null) {
                    il.LoadArgument(0);
                    il.Call(basector);
                }
            }

            var index = 1;
            foreach (var backingField in _storageFields) {
                // store actualInstance in backingField
                il.LoadArgument(0);
                il.LoadArgument(index++);
                il.StoreField(backingField);
            }
            // return
            il.Return();
        }
    }
}