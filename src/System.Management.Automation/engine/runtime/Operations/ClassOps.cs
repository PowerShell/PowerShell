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
using System.Management.Automation.Language;
using System.Threading;

// These APIs are not part of the public contract.
// They are implementation details and intended to be called from generated assemblies for PS classes.
//
// Because they are called from other assemblies, we have to make them public. 
// We put them in Internal namespace to emphasise that despite the fact that they are public, it's not part of API contract.

namespace System.Management.Automation.Internal
{
    /// <summary>
    /// Every Runspace in one process contains SessionStateInternal per module (module SessionState).
    /// Every RuntimeType is associated to only one SessionState in the Runspace, which creates it: 
    /// it's ever global state or a module state.
    /// In the former case, module can be imported from the different runspaces in the same process.
    /// And so runspaces will share RuntimeType. But in every runspace, Type is associated with just one SessionState.
    /// We want type methods to be able access $script: variables and module-specific methods.
    /// To achieve it, we preserve reference to SessionState that creates type in the private field 'SessionStateFieldName'.
    /// Later, we use it to call scriptBlocks captured in ScriptBlockMemberMethodWrapper with the right sessionState.
    /// </summary>
    public class SessionStateKeeper
    {
        // We use ConditionalWeakTable, because if GC already collect Runspace, 
        // then there is no way to call a ctor on the type in this Runspace.
        private readonly ConditionalWeakTable<Runspace, SessionStateInternal> _stateMap;

        internal SessionStateKeeper()
        {
            _stateMap = new ConditionalWeakTable<Runspace, SessionStateInternal>();
        }

        internal void RegisterRunspace()
        {
            // it's not get, but really 'Add' value.
            // ConditionalWeakTable.Add throw exception, when you are trying to add a value with the same key.
            _stateMap.GetValue(Runspace.DefaultRunspace, runspace => runspace.ExecutionContext.EngineSessionState);
        }

        /// <summary>
        /// This method should be called only from generated ctors for PowerShell classes.
        /// It's not intended to be a public API, but because we generate type in a different assembly it has to be public.
        /// Return type should be SessionStateInternal, but it violates accessibility consistency, so we use object.
        /// </summary>
        /// <returns>SessionStateInternal</returns>
        public object GetSessionState()
        {
            SessionStateInternal ss = null;
            bool found = _stateMap.TryGetValue(Runspace.DefaultRunspace, out ss);
            Diagnostics.Assert(found, "We always should be able to find corresponding SessionState");
            return ss;
        }
    }

    /// <summary/>
    public class ScriptBlockMemberMethodWrapper
    {
        /// <summary>Used in codegen</summary>
        public static readonly object[] _emptyArgumentArray = Utils.EmptyArray<object>(); // See TypeDefiner.DefineTypeHelper.DefineMethodBody

        // we use this _scriptBlock instance for static methods.
        private Lazy<ScriptBlock> _scriptBlock;
        private IParameterMetadataProvider _ast;

        /// <summary>
        /// We use ThreadLocal boundScriptBlock to allow multi-thread execution of instance methods.
        /// </summary>
        private ThreadLocal<ScriptBlock> _boundScriptBlock;

        internal ScriptBlockMemberMethodWrapper(IParameterMetadataProvider ast)
        {
            _ast = ast;
            _scriptBlock = new Lazy<ScriptBlock>(() => new ScriptBlock(_ast, isFilter: false));
            _boundScriptBlock = new ThreadLocal<ScriptBlock>(
                () =>
                {
                    var sb = _scriptBlock.Value.Clone();
                    return sb;
                });
        }

        internal void InitAtRuntime()
        {
            var context = Runspace.DefaultRunspace.ExecutionContext;
            _scriptBlock.Value.SessionStateInternal = context.EngineSessionState;
        }

        /// <summary>
        /// </summary>
        /// <param name="instance">target object or null for static call</param>
        /// <param name="sessionStateInternal">sessionStateInternal from private field of instance or null for static call</param>
        /// <param name="args"></param>
        public void InvokeHelper(object instance, object sessionStateInternal, object[] args)
        {
            ScriptBlock sb;
            if (instance != null)
            {
                _boundScriptBlock.Value.SessionStateInternal = (SessionStateInternal)sessionStateInternal;
                sb = _boundScriptBlock.Value;
            }
            else
            {
                sb = _scriptBlock.Value;
            }

            sb.InvokeAsMemberFunction(instance, args);
        }

        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance">target object or null for static call</param>
        /// <param name="sessionStateInternal">sessionStateInternal from private field of instance or null for static call</param>
        /// <param name="args"></param>
        /// <returns></returns>
        public T InvokeHelperT<T>(object instance, object sessionStateInternal, object[] args)
        {
            ScriptBlock sb;
            if (instance != null)
            {
                _boundScriptBlock.Value.SessionStateInternal = (SessionStateInternal)sessionStateInternal;
                sb = _boundScriptBlock.Value;
            }
            else
            {
                sb = _scriptBlock.Value;
            }

            return sb.InvokeAsMemberFunctionT<T>(instance, args);
        }
    }

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
        private static readonly ConditionalWeakTable<MethodInfo, DynamicMethod> s_nonVirtualCallCache =
            new ConditionalWeakTable<MethodInfo, DynamicMethod>();

        /// <summary>
        /// Implementation of non-virtual method call.
        /// </summary>
        /// <param name="target">object for invocation</param>
        /// <param name="mi">method info for invocation</param>
        /// <param name="args">arguments for invocation</param>
        private static object CallMethodNonVirtuallyImpl(object target, MethodInfo mi, object[] args)
        {
            DynamicMethod dm = s_nonVirtualCallCache.GetValue(mi, CreateDynamicMethod);

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
