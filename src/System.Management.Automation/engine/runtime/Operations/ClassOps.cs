// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
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
    /// it's either global state or a module state.
    /// In the former case, module can be imported from the different runspaces in the same process.
    /// And so runspaces will share RuntimeType. But in every runspace, Type is associated with just one SessionState.
    /// We want type methods to be able access $script: variables and module-specific methods.
    /// To achieve it, we preserve reference to SessionState that creates type in the private field 'SessionStateFieldName'.
    /// Later, we use it to call scriptBlocks captured in ScriptBlockMemberMethodWrapper with the right sessionState.
    /// </summary>
    public class SessionStateKeeper
    {
        // We use ConditionalWeakTable, because if GC already collect Runspace, then there
        // is no way to call a ctor or a static method on the type in this Runspace.
        private readonly ConditionalWeakTable<Runspace, SessionStateInternal> _stateMap;

        internal SessionStateKeeper()
        {
            _stateMap = new ConditionalWeakTable<Runspace, SessionStateInternal>();
        }

        internal void RegisterRunspace()
        {
            SessionStateInternal sessionStateInMap = null;
            Runspace runspaceToUse = Runspace.DefaultRunspace;
            SessionStateInternal sessionStateToUse = runspaceToUse.ExecutionContext.EngineSessionState;

            // Different threads will operate on different key/value pairs (default-runspace/session-state pairs),
            // and a ConditionalWeakTable itself is thread safe, so there won't be race condition here.
            if (!_stateMap.TryGetValue(runspaceToUse, out sessionStateInMap))
            {
                // If the key doesn't exist yet, add it
                _stateMap.Add(runspaceToUse, sessionStateToUse);
            }
            else if (sessionStateInMap != sessionStateToUse)
            {
                // If the key exists but the corresponding value is not what we should use, then remove the key/value pair and add the new pair.
                // This could happen when a powershell class is defined in a module and the module gets reloaded. In such case, the same TypeDefinitionAst
                // instance will get reused, but should be associated with the SessionState from the new module, instead of the one from the old module.
                _stateMap.AddOrUpdate(runspaceToUse, sessionStateToUse);
            }
            // If the key exists and the corresponding value is the one we should use, then do nothing.
        }

        /// <summary>
        /// This method should be called only from
        ///  - generated ctors for PowerShell classes, AND
        ///  - ScriptBlockMemberMethodWrapper when invoking static methods of PowerShell classes.
        /// It's not intended to be a public API, but because we generate type in a different assembly it has to be public.
        /// Return type should be SessionStateInternal, but it violates accessibility consistency, so we use object.
        /// </summary>
        /// <remarks>
        /// By default, PowerShell class instantiation usually happens in the same Runspace where the class is defined. In
        /// that case, the created instance will be bound to the session state used to define that class in the Runspace.
        /// However, if the instantiation happens in a different Runspace where the class is not defined, or it happens on
        /// a thread without a default Runspace, then the created instance won't be bound to any session state.
        /// </remarks>
        /// <returns>SessionStateInternal.</returns>
        public object GetSessionState()
        {
            SessionStateInternal ss = null;

            // DefaultRunspace could be null when we reach here. For example, create instance of
            // a PowerShell class by using reflection on a thread without DefaultRunspace.
            // Make sure we call 'TryGetValue' with a non-null key, otherwise ArgumentNullException will be thrown.
            Runspace defaultRunspace = Runspace.DefaultRunspace;
            if (defaultRunspace != null)
            {
                _stateMap.TryGetValue(defaultRunspace, out ss);
            }

            return ss;
        }
    }

    /// <summary/>
    public class ScriptBlockMemberMethodWrapper
    {
        /// <summary>Used in codegen</summary>
        public static readonly object[] _emptyArgumentArray = Array.Empty<object>(); // See TypeDefiner.DefineTypeHelper.DefineMethodBody

        /// <summary>
        /// Indicate the wrapper is for a static member method.
        /// </summary>
        private readonly bool _isStatic;

        /// <summary>
        /// The SessionStateKeeper associated with the helper type generated from PowerShell class.
        /// We query it for the SessionState to run static method in.
        /// </summary>
        private readonly SessionStateKeeper _sessionStateKeeper;

        /// <summary>
        /// We use WeakReference object to point to the default SessionState because if GC already collect the SessionState,
        /// or the Runspace it chains to is closed and disposed, then we cannot run the static method there anyways.
        /// </summary>
        /// <remarks>
        /// The default SessionState is used only if a static method is called from a Runspace where the PowerShell class is
        /// never defined, or is called on a thread without a default Runspace. Usage like those should be rare.
        /// </remarks>
        private readonly WeakReference<SessionStateInternal> _defaultSessionStateToUse;

        /// <summary>
        /// The body AST of the member method.
        /// </summary>
        private readonly IParameterMetadataProvider _ast;

        /// <summary>
        /// We use _scriptBlock instance to provide the shared CompiledScriptBlockData.
        /// </summary>
        private readonly Lazy<ScriptBlock> _scriptBlock;

        /// <summary>
        /// We use ThreadLocal boundScriptBlock to allow multi-thread execution of member methods.
        /// </summary>
        private readonly ThreadLocal<ScriptBlock> _boundScriptBlock;

        /// <summary>
        /// Constructor to be called when the wrapper is for a static member method.
        /// </summary>
        internal ScriptBlockMemberMethodWrapper(IParameterMetadataProvider ast, SessionStateKeeper sessionStateKeeper)
            : this(ast)
        {
            _isStatic = true;
            _sessionStateKeeper = sessionStateKeeper;
            _defaultSessionStateToUse = new WeakReference<SessionStateInternal>(null);
        }

        /// <summary>
        /// Constructor to be called when the wrapper is for an instance member method.
        /// </summary>
        internal ScriptBlockMemberMethodWrapper(IParameterMetadataProvider ast)
        {
            _ast = ast;
            // This 'Lazy<T>' constructor ensures that only a single thread can initialize the instance in a thread-safe manner.
            _scriptBlock = new Lazy<ScriptBlock>(() => new ScriptBlock(_ast, isFilter: false));
            _boundScriptBlock = new ThreadLocal<ScriptBlock>(() => _scriptBlock.Value.Clone());
        }

        /// <summary>
        /// Initialization happens when the script that defines PowerShell class is executed.
        /// This initialization is required only if this wrapper is for a static method.
        /// </summary>
        /// <remarks>
        /// When the same script file gets executed multiple times, the .NET type generated from the PowerShell class
        /// defined in the file will be shared in those executions, and thus this method will be called multiple times
        /// possibly in the contexts of different Runspace/SessionState.
        ///
        /// We always use the SessionState from the most recent execution as the default SessionState, so be noted that
        /// the default SessionState may change over time.
        ///
        /// This should be OK because the common usage is to run the static method in the same Runspace where the class
        /// is declared, and thus we can always get the correct SessionState to use by querying the 'SessionStateKeeper'.
        /// The default SessionState is used only if a static method is called from a Runspace where the class is never
        /// defined, or is called on a thread without a default Runspace.
        /// </remarks>
        internal void InitAtRuntime()
        {
            if (_isStatic)
            {
                // WeakReference<T>'s instance methods are not thread-safe, so we need the lock to guarantee
                // 'SetTarget' and 'TryGetTarget' are not called by multiple threads at the same time.
                lock (_defaultSessionStateToUse)
                {
                    var context = Runspace.DefaultRunspace.ExecutionContext;
                    _defaultSessionStateToUse.SetTarget(context.EngineSessionState);
                }
            }
        }

        /// <summary>
        /// Set the SessionState of the script block appropriately.
        /// </summary>
        private void PrepareScriptBlockToInvoke(object instance, object sessionStateInternal)
        {
            SessionStateInternal sessionStateToUse = null;
            if (instance != null)
            {
                // Use the SessionState passed in, which is the one associated with the instance.
                sessionStateToUse = (SessionStateInternal)sessionStateInternal;
            }
            else
            {
                // For static method, it's a little complex.
                // - Check if the current default runspace is registered with the SessionStateKeeper. If so, use the registered SessionState.
                // - Otherwise, check if default SessionState is still alive. If so, use the default SessionState.
                // - Otherwise, the 'SessionStateInternal' property will be set to null, and thus the default runspace of the current thread will be used.
                //              If the current thread doesn't have a default Runspace, then an InvalidOperationException will be thrown when invoking the
                //              script block, which is expected.
                sessionStateToUse = (SessionStateInternal)_sessionStateKeeper.GetSessionState();
                if (sessionStateToUse == null)
                {
                    lock (_defaultSessionStateToUse)
                    {
                        _defaultSessionStateToUse.TryGetTarget(out sessionStateToUse);
                    }
                }
            }

            _boundScriptBlock.Value.SessionStateInternal = sessionStateToUse;
        }

        /// <summary>
        /// </summary>
        /// <param name="instance">Target object or null for static call.</param>
        /// <param name="sessionStateInternal">SessionStateInternal from private field of instance or null for static call.</param>
        /// <param name="args"></param>
        public void InvokeHelper(object instance, object sessionStateInternal, object[] args)
        {
            try
            {
                PrepareScriptBlockToInvoke(instance, sessionStateInternal);
                _boundScriptBlock.Value.InvokeAsMemberFunction(instance, args);
            }
            finally
            {
                // '_boundScriptBlock.Value' for a thread will live until
                //  - the thread is gone, OR
                //  - the dyanmic assembly holding this wrapper instance is GC collected.
                // We don't hold on the SessionState object, so that GC can collect it as appropriate.
                _boundScriptBlock.Value.SessionStateInternal = null;
            }
        }

        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance">Target object or null for static call.</param>
        /// <param name="sessionStateInternal">SessionStateInternal from private field of instance or null for static call.</param>
        /// <param name="args"></param>
        /// <returns></returns>
        public T InvokeHelperT<T>(object instance, object sessionStateInternal, object[] args)
        {
            try
            {
                PrepareScriptBlockToInvoke(instance, sessionStateInternal);
                return _boundScriptBlock.Value.InvokeAsMemberFunctionT<T>(instance, args);
            }
            finally
            {
                // '_boundScriptBlock.Value' for a thread will live until
                //  - the thread is gone, OR
                //  - the dyanmic assembly holding this wrapper instance is GC collected.
                // We don't hold on the SessionState object, so that GC can collect it as appropriate.
                _boundScriptBlock.Value.SessionStateInternal = null;
            }
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
            var engineIntrinsics = executionContext?.EngineIntrinsics;
            foreach (var validateAttribute in validateAttributes)
            {
                validateAttribute.InternalValidate(value, engineIntrinsics);
            }
        }

        /// <summary>
        /// Performs base ctor call as a method call.
        /// </summary>
        /// <param name="target">Object for invocation.</param>
        /// <param name="ci">Ctor info for invocation.</param>
        /// <param name="args">Arguments for invocation.</param>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static void CallBaseCtor(object target, ConstructorInfo ci, object[] args)
        {
            ci.Invoke(target, args);
        }

        /// <summary>
        /// Performs non-virtual method call with return value. Main usage: base class method call inside subclass method.
        /// </summary>
        /// <param name="target">Object for invocation.</param>
        /// <param name="mi">Method info for invocation.</param>
        /// <param name="args">Arguments for invocation.</param>
        public static object CallMethodNonVirtually(object target, MethodInfo mi, object[] args)
        {
            return CallMethodNonVirtuallyImpl(target, mi, args);
        }

        /// <summary>
        /// Performs non-virtual void method call. Main usage: base class method call inside subclass method.
        /// </summary>
        /// <param name="target">Object for invocation.</param>
        /// <param name="mi">Method info for invocation.</param>
        /// <param name="args">Arguments for invocation.</param>
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
        /// <param name="target">Object for invocation.</param>
        /// <param name="mi">Method info for invocation.</param>
        /// <param name="args">Arguments for invocation.</param>
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
            paramTypes.AddRange(mi.GetParameters().Select(static x => x.ParameterType));

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
