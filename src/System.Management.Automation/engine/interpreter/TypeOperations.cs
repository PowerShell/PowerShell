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

using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Management.Automation.Interpreter
{
    internal sealed class CreateDelegateInstruction : Instruction
    {
        private readonly LightDelegateCreator _creator;

        internal CreateDelegateInstruction(LightDelegateCreator delegateCreator)
        {
            _creator = delegateCreator;
        }

        public override int ConsumedStack { get { return _creator.Interpreter.ClosureSize; } }

        public override int ProducedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            StrongBox<object>[] closure;
            if (ConsumedStack > 0)
            {
                closure = new StrongBox<object>[ConsumedStack];
                for (int i = closure.Length - 1; i >= 0; i--)
                {
                    closure[i] = (StrongBox<object>)frame.Pop();
                }
            }
            else
            {
                closure = null;
            }

            Delegate d = _creator.CreateDelegate(closure);

            frame.Push(d);
            return +1;
        }
    }

    internal sealed class NewInstruction : Instruction
    {
        private readonly ConstructorInfo _constructor;
        private readonly int _argCount;

        public NewInstruction(ConstructorInfo constructor)
        {
            _constructor = constructor;
            _argCount = constructor.GetParameters().Length;
        }

        public override int ConsumedStack { get { return _argCount; } }

        public override int ProducedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            object[] args = new object[_argCount];
            for (int i = _argCount - 1; i >= 0; i--)
            {
                args[i] = frame.Pop();
            }

            object ret;
            try
            {
                ret = _constructor.Invoke(args);
            }
            catch (TargetInvocationException e)
            {
                ExceptionHelpers.UpdateForRethrow(e.InnerException);
                throw e.InnerException;
            }

            frame.Push(ret);
            return +1;
        }

        public override string ToString()
        {
            return "New " + _constructor.DeclaringType.Name + "(" + _constructor + ")";
        }
    }

    internal sealed class DefaultValueInstruction<T> : Instruction
    {
        internal DefaultValueInstruction() { }

        public override int ConsumedStack { get { return 0; } }

        public override int ProducedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            frame.Push(default(T));
            return +1;
        }

        public override string ToString()
        {
            return "New " + typeof(T);
        }
    }

    internal sealed class TypeIsInstruction<T> : Instruction
    {
        internal TypeIsInstruction() { }

        public override int ConsumedStack { get { return 1; } }

        public override int ProducedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            // unfortunately Type.IsInstanceOfType() is 35-times slower than "is T" so we use generic code:
            frame.Push(ScriptingRuntimeHelpers.BooleanToObject(frame.Pop() is T));
            return +1;
        }

        public override string ToString()
        {
            return "TypeIs " + typeof(T).Name;
        }
    }

    internal sealed class TypeAsInstruction<T> : Instruction
    {
        internal TypeAsInstruction() { }

        public override int ConsumedStack { get { return 1; } }

        public override int ProducedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            // can't use as w/o generic constraint
            object value = frame.Pop();
            if (value is T)
            {
                frame.Push(value);
            }
            else
            {
                frame.Push(null);
            }

            return +1;
        }

        public override string ToString()
        {
            return "TypeAs " + typeof(T).Name;
        }
    }

    internal sealed class TypeEqualsInstruction : Instruction
    {
        public static readonly TypeEqualsInstruction Instance = new TypeEqualsInstruction();

        public override int ConsumedStack { get { return 2; } }

        public override int ProducedStack { get { return 1; } }

        private TypeEqualsInstruction()
        {
        }

        public override int Run(InterpretedFrame frame)
        {
            object type = frame.Pop();
            object obj = frame.Pop();
            frame.Push(ScriptingRuntimeHelpers.BooleanToObject(obj != null && (object)obj.GetType() == type));
            return +1;
        }

        public override string InstructionName
        {
            get { return "TypeEquals()"; }
        }
    }
}
