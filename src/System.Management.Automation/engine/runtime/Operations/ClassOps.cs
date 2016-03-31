/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Management.Automation
{
    /// <summary>
    /// Support methods for PowerShell classes.
    /// </summary>
    public static class ClassOps
    {
        /// <summary>
        /// This method calls all Validate attributes for the property to validate value.
        /// Called from class property setters with ValidateArgumentsAttribute attributes. 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        public static void ValidateSetProperty(Type type, string propertyName, object value)
        {
            var validateAttributes = type.GetProperty(propertyName).GetCustomAttributes<ValidateArgumentsAttribute>();
            var executionContext = LocalPipeline.GetExecutionContextFromTLS();
            var engineIntrinsics = executionContext == null ? null : executionContext.EngineIntrinsics;
            foreach (var validateAttribute in validateAttributes)
            {

                validateAttribute.InternalValidate(value, engineIntrinsics);
            }
        }

        /// <summary>
        /// Performs base ctor call as a method call.
        /// </summary>
        /// <param name="target">object for invocation</param>
        /// <param name="ci">ctor info for invocation</param>
        /// <param name="args">arguments for invocation</param>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static void CallBaseCtor(object target, ConstructorInfo ci, object[] args)
        {
            ci.Invoke(target, args);
        }

        /// <summary>
        /// Performs non-virtual method call with return value. Main usage: base class method call inside subclass method.
        /// </summary>
        /// <param name="target">object for invocation</param>
        /// <param name="mi">method info for invocation</param>
        /// <param name="args">arguments for invocation</param>
        public static object CallMethodNonVirtually(object target, MethodInfo mi, object[] args)
        {
            return CallMethodNonVirtuallyImpl(target, mi, args);
        }

        /// <summary>
        /// Performs non-virtual void method call. Main usage: base class method call inside subclass method.
        /// </summary>
        /// <param name="target">object for invocation</param>
        /// <param name="mi">method info for invocation</param>
        /// <param name="args">arguments for invocation</param>
        public static void CallVoidMethodNonVirtually(object target, MethodInfo mi, object[] args)
        {
            CallMethodNonVirtuallyImpl(target, mi, args);
        }

        /// <summary>
        /// A cache for the DynamicMethod objects that call to base method non-virtually.
        /// The cache can clean up the outdated WeakReference entries by itself.
        /// </summary>
        private static readonly ConditionalWeakTable<MethodInfo, DynamicMethod> NonVirtualCallCache =
            new ConditionalWeakTable<MethodInfo, DynamicMethod>();

        /// <summary>
        /// Implementation of non-virtual method call.
        /// </summary>
        /// <param name="target">object for invocation</param>
        /// <param name="mi">method info for invocation</param>
        /// <param name="args">arguments for invocation</param>
        private static object CallMethodNonVirtuallyImpl(object target, MethodInfo mi, object[] args)
        {
            DynamicMethod dm = NonVirtualCallCache.GetValue(mi, CreateDynamicMethod);

            // The target object will be passed to the hidden parameter 'this' of the instance method 
            var newArgs = new List<object>(args.Length + 1) { target };
            newArgs.AddRange(args);

            return dm.Invoke(null, newArgs.ToArray());
        }

        /// <summary>
        /// Help method to create the DynamicMethod for calling base method non-virtually.
        /// </summary>
        private static DynamicMethod CreateDynamicMethod(MethodInfo mi)
        {
            // Pass in the declaring type because instance method has a hidden parameter 'this' as the first parameter.
            var paramTypes = new List<Type> { mi.DeclaringType };
            paramTypes.AddRange(mi.GetParameters().Select(x => x.ParameterType));

            var dm = new DynamicMethod("PSNonVirtualCall_" + mi.Name, mi.ReturnType, paramTypes.ToArray(), mi.DeclaringType);
            ILGenerator il = dm.GetILGenerator();
            for (int i = 0; i < paramTypes.Count; i++)
            {
                il.Emit(OpCodes.Ldarg, i);
            }
            il.Emit(OpCodes.Tailcall);
            il.EmitCall(OpCodes.Call, mi, null);
            il.Emit(OpCodes.Ret);

            return dm;
        }
    }
}
