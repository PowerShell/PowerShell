/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation.
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A
 * copy of the license can be found in the License.html file at the root of this distribution. If
 * you cannot locate the Apache License, Version 2.0, please send an email to
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;

//using Microsoft.Scripting.Generation;

using AstUtils = System.Management.Automation.Interpreter.Utils;

namespace System.Management.Automation.Interpreter
{
    internal sealed class LightLambdaCompileEventArgs : EventArgs
    {
        public Delegate Compiled { get; }

        internal LightLambdaCompileEventArgs(Delegate compiled)
        {
            Compiled = compiled;
        }
    }

    internal partial class LightLambda
    {
        private readonly StrongBox<object>[] _closure;
        private readonly Interpreter _interpreter;
        private static readonly CacheDict<Type, Func<LightLambda, Delegate>> s_runCache = new CacheDict<Type, Func<LightLambda, Delegate>>(100);

        // Adaptive compilation support
        private readonly LightDelegateCreator _delegateCreator;
        private Delegate _compiled;
        private int _compilationThreshold;

        /// <summary>
        /// Provides notification that the LightLambda has been compiled.
        /// </summary>
        public event EventHandler<LightLambdaCompileEventArgs> Compile;

        internal LightLambda(LightDelegateCreator delegateCreator, StrongBox<object>[] closure, int compilationThreshold)
        {
            _delegateCreator = delegateCreator;
            _closure = closure;
            _interpreter = delegateCreator.Interpreter;
            _compilationThreshold = compilationThreshold;
        }

        private static Func<LightLambda, Delegate> GetRunDelegateCtor(Type delegateType)
        {
            lock (s_runCache)
            {
                Func<LightLambda, Delegate> fastCtor;
                if (s_runCache.TryGetValue(delegateType, out fastCtor))
                {
                    return fastCtor;
                }

                return MakeRunDelegateCtor(delegateType);
            }
        }

        private static Func<LightLambda, Delegate> MakeRunDelegateCtor(Type delegateType)
        {
            var method = delegateType.GetMethod("Invoke");
            var paramInfos = method.GetParameters();
            Type[] paramTypes;
            string name = "Run";

            if (paramInfos.Length >= MaxParameters)
            {
                return null;
            }

            if (method.ReturnType == typeof(void))
            {
                name += "Void";
                paramTypes = new Type[paramInfos.Length];
            }
            else
            {
                paramTypes = new Type[paramInfos.Length + 1];
                paramTypes[paramTypes.Length - 1] = method.ReturnType;
            }

            MethodInfo runMethod;

            if (method.ReturnType == typeof(void) && paramTypes.Length == 2 &&
                paramInfos[0].ParameterType.IsByRef && paramInfos[1].ParameterType.IsByRef)
            {
                runMethod = typeof(LightLambda).GetMethod("RunVoidRef2", BindingFlags.NonPublic | BindingFlags.Instance);
                paramTypes[0] = paramInfos[0].ParameterType.GetElementType();
                paramTypes[1] = paramInfos[1].ParameterType.GetElementType();
            }
            else if (method.ReturnType == typeof(void) && paramTypes.Length == 0)
            {
                runMethod = typeof(LightLambda).GetMethod("RunVoid0", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            else
            {
                for (int i = 0; i < paramInfos.Length; i++)
                {
                    paramTypes[i] = paramInfos[i].ParameterType;
                    if (paramTypes[i].IsByRef)
                    {
                        return null;
                    }
                }

                if (DelegateHelpers.MakeDelegate(paramTypes) == delegateType)
                {
                    name = "Make" + name + paramInfos.Length;

                    MethodInfo ctorMethod = typeof(LightLambda).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(paramTypes);
                    return s_runCache[delegateType] = (Func<LightLambda, Delegate>)ctorMethod.CreateDelegate(typeof(Func<LightLambda, Delegate>));
                }

                runMethod = typeof(LightLambda).GetMethod(name + paramInfos.Length, BindingFlags.NonPublic | BindingFlags.Instance);
            }

#if !SILVERLIGHT
            try
            {
                DynamicMethod dm = new DynamicMethod("FastCtor", typeof(Delegate), new[] { typeof(LightLambda) }, typeof(LightLambda), true);
                var ilgen = dm.GetILGenerator();
                ilgen.Emit(OpCodes.Ldarg_0);
                ilgen.Emit(OpCodes.Ldftn, runMethod.IsGenericMethodDefinition ? runMethod.MakeGenericMethod(paramTypes) : runMethod);
                ilgen.Emit(OpCodes.Newobj, delegateType.GetConstructor(new[] { typeof(object), typeof(IntPtr) }));
                ilgen.Emit(OpCodes.Ret);
                return s_runCache[delegateType] = (Func<LightLambda, Delegate>)dm.CreateDelegate(typeof(Func<LightLambda, Delegate>));
            }
            catch (SecurityException)
            {
            }
#endif

            // we don't have permission for restricted skip visibility dynamic methods, use the slower Delegate.CreateDelegate.
            var targetMethod = runMethod.IsGenericMethodDefinition ? runMethod.MakeGenericMethod(paramTypes) : runMethod;
            return s_runCache[delegateType] = lambda => targetMethod.CreateDelegate(delegateType, lambda);
        }

        // TODO enable sharing of these custom delegates
        private Delegate CreateCustomDelegate(Type delegateType)
        {
            // PerfTrack.NoteEvent(PerfTrack.Categories.Compiler, "Synchronously compiling a custom delegate");

            var method = delegateType.GetMethod("Invoke");
            var paramInfos = method.GetParameters();
            var parameters = new ParameterExpression[paramInfos.Length];
            var parametersAsObject = new Expression[paramInfos.Length];
            for (int i = 0; i < paramInfos.Length; i++)
            {
                ParameterExpression parameter = Expression.Parameter(paramInfos[i].ParameterType, paramInfos[i].Name);
                parameters[i] = parameter;
                parametersAsObject[i] = Expression.Convert(parameter, typeof(object));
            }

            var data = Expression.NewArrayInit(typeof(object), parametersAsObject);
            var self = AstUtils.Constant(this);
            var runMethod = typeof(LightLambda).GetMethod("Run");
            var body = Expression.Convert(Expression.Call(self, runMethod, data), method.ReturnType);
            var lambda = Expression.Lambda(delegateType, body, parameters);
            return lambda.Compile();
        }

        internal Delegate MakeDelegate(Type delegateType)
        {
            Func<LightLambda, Delegate> fastCtor = GetRunDelegateCtor(delegateType);
            if (fastCtor != null)
            {
                return fastCtor(this);
            }
            else
            {
                return CreateCustomDelegate(delegateType);
            }
        }

        private bool TryGetCompiled()
        {
            // Use the compiled delegate if available.
            if (_delegateCreator.HasCompiled)
            {
                _compiled = _delegateCreator.CreateCompiledDelegate(_closure);

                // Send it to anyone who's interested.
                var compileEvent = Compile;
                if (compileEvent != null && _delegateCreator.SameDelegateType)
                {
                    compileEvent(this, new LightLambdaCompileEventArgs(_compiled));
                }

                return true;
            }

            // Don't lock here, it's a frequently hit path.
            //
            // There could be multiple threads racing, but that is okay.
            // Two bad things can happen:
            //   * We miss decrements (some thread sets the counter forward)
            //   * We might enter the "if" branch more than once.
            //
            // The first is okay, it just means we take longer to compile.
            // The second we explicitly guard against inside of Compile().
            //
            // We can't miss 0. The first thread that writes -1 must have read 0 and hence start compilation.
            if (unchecked(_compilationThreshold--) == 0)
            {
#if SILVERLIGHT
                if (PlatformAdaptationLayer.IsCompactFramework) {
                    _compilationThreshold = Int32.MaxValue;
                    return false;
                }
#endif
                if (_interpreter.CompileSynchronously)
                {
                    _delegateCreator.Compile(null);
                    return TryGetCompiled();
                }
                else
                {
                    // Kick off the compile on another thread so this one can keep going
                    ThreadPool.QueueUserWorkItem(_delegateCreator.Compile, null);
                }
            }

            return false;
        }

        private InterpretedFrame MakeFrame()
        {
            return new InterpretedFrame(_interpreter, _closure);
        }

        internal void RunVoidRef2<T0, T1>(ref T0 arg0, ref T1 arg1)
        {
            // if (_compiled != null || TryGetCompiled()) {
            //    ((ActionRef<T0, T1>)_compiled)(ref arg0, ref arg1);
            //    return;
            // }

            // copy in and copy out for today...
            var frame = MakeFrame();
            frame.Data[0] = arg0;
            frame.Data[1] = arg1;
            var currentFrame = frame.Enter();
            try
            {
                _interpreter.Run(frame);
            }
            finally
            {
                frame.Leave(currentFrame);
                arg0 = (T0)frame.Data[0];
                arg1 = (T1)frame.Data[1];
            }
        }

        public object Run(params object[] arguments)
        {
            if (_compiled != null || TryGetCompiled())
            {
                return _compiled.DynamicInvoke(arguments);
            }

            var frame = MakeFrame();
            for (int i = 0; i < arguments.Length; i++)
            {
                frame.Data[i] = arguments[i];
            }

            var currentFrame = frame.Enter();
            try
            {
                _interpreter.Run(frame);
            }
            finally
            {
                frame.Leave(currentFrame);
            }

            return frame.Pop();
        }
    }
}
