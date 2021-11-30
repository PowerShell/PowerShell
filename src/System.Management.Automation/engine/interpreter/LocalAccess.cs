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
#else
using Microsoft.Scripting.Ast;
#endif
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace System.Management.Automation.Interpreter
{

#nullable enable
    internal interface IBoxableInstruction
    {
        Instruction? BoxIfIndexMatches(int index);
    }
#nullable restore

    internal abstract class LocalAccessInstruction : Instruction
    {
        internal readonly int _index;

        protected LocalAccessInstruction(int index)
        {
            _index = index;
        }

        public override string ToDebugString(int instructionIndex, object cookie, Func<int, int> labelIndexer, IList<object> objects)
        {
            return cookie == null ?
                InstructionName + "(" + _index + ")" :
                InstructionName + "(" + cookie + ": " + _index + ")";
        }
    }

    #region Load

    internal sealed class LoadLocalInstruction : LocalAccessInstruction, IBoxableInstruction
    {
        internal LoadLocalInstruction(int index)
            : base(index)
        {
        }

        public override int ProducedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            frame.Data[frame.StackIndex++] = frame.Data[_index];
            // frame.Push(frame.Data[_index]);
            return +1;
        }

        public Instruction BoxIfIndexMatches(int index)
        {
            return (index == _index) ? InstructionList.LoadLocalBoxed(index) : null;
        }
    }

    internal sealed class LoadLocalBoxedInstruction : LocalAccessInstruction
    {
        internal LoadLocalBoxedInstruction(int index)
            : base(index)
        {
        }

        public override int ProducedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            var box = (StrongBox<object>)frame.Data[_index];
            frame.Data[frame.StackIndex++] = box.Value;
            return +1;
        }
    }

    internal sealed class LoadLocalFromClosureInstruction : LocalAccessInstruction
    {
        internal LoadLocalFromClosureInstruction(int index)
            : base(index)
        {
        }

        public override int ProducedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            var box = frame.Closure[_index];
            frame.Data[frame.StackIndex++] = box.Value;
            return +1;
        }
    }

    internal sealed class LoadLocalFromClosureBoxedInstruction : LocalAccessInstruction
    {
        internal LoadLocalFromClosureBoxedInstruction(int index)
            : base(index)
        {
        }

        public override int ProducedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            var box = frame.Closure[_index];
            frame.Data[frame.StackIndex++] = box;
            return +1;
        }
    }

    #endregion

    #region Store, Assign

    internal sealed class AssignLocalInstruction : LocalAccessInstruction, IBoxableInstruction
    {
        internal AssignLocalInstruction(int index)
            : base(index)
        {
        }

        public override int ConsumedStack { get { return 1; } }

        public override int ProducedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            frame.Data[_index] = frame.Peek();
            return +1;
        }

        public Instruction BoxIfIndexMatches(int index)
        {
            return (index == _index) ? InstructionList.AssignLocalBoxed(index) : null;
        }
    }

    internal sealed class StoreLocalInstruction : LocalAccessInstruction, IBoxableInstruction
    {
        internal StoreLocalInstruction(int index)
            : base(index)
        {
        }

        public override int ConsumedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            frame.Data[_index] = frame.Data[--frame.StackIndex];
            // frame.Data[_index] = frame.Pop();
            return +1;
        }

        public Instruction BoxIfIndexMatches(int index)
        {
            return (index == _index) ? InstructionList.StoreLocalBoxed(index) : null;
        }
    }

    internal sealed class AssignLocalBoxedInstruction : LocalAccessInstruction
    {
        internal AssignLocalBoxedInstruction(int index)
            : base(index)
        {
        }

        public override int ConsumedStack { get { return 1; } }

        public override int ProducedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            var box = (StrongBox<object>)frame.Data[_index];
            box.Value = frame.Peek();
            return +1;
        }
    }

    internal sealed class StoreLocalBoxedInstruction : LocalAccessInstruction
    {
        internal StoreLocalBoxedInstruction(int index)
            : base(index)
        {
        }

        public override int ConsumedStack { get { return 1; } }

        public override int ProducedStack { get { return 0; } }

        public override int Run(InterpretedFrame frame)
        {
            var box = (StrongBox<object>)frame.Data[_index];
            box.Value = frame.Data[--frame.StackIndex];
            return +1;
        }
    }

    internal sealed class AssignLocalToClosureInstruction : LocalAccessInstruction
    {
        internal AssignLocalToClosureInstruction(int index)
            : base(index)
        {
        }

        public override int ConsumedStack { get { return 1; } }

        public override int ProducedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            var box = frame.Closure[_index];
            box.Value = frame.Peek();
            return +1;
        }
    }

    #endregion

    #region Initialize

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1012:AbstractTypesShouldNotHaveConstructors")]
    internal abstract class InitializeLocalInstruction : LocalAccessInstruction
    {
        internal InitializeLocalInstruction(int index)
            : base(index)
        {
        }

        internal sealed class Reference : InitializeLocalInstruction, IBoxableInstruction
        {
            internal Reference(int index)
                : base(index)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                frame.Data[_index] = null;
                return 1;
            }

            public Instruction BoxIfIndexMatches(int index)
            {
                return (index == _index) ? InstructionList.InitImmutableRefBox(index) : null;
            }

            public override string InstructionName
            {
                get { return "InitRef"; }
            }
        }

        internal sealed class ImmutableValue : InitializeLocalInstruction, IBoxableInstruction
        {
            private readonly object _defaultValue;

            internal ImmutableValue(int index, object defaultValue)
                : base(index)
            {
                _defaultValue = defaultValue;
            }

            public override int Run(InterpretedFrame frame)
            {
                frame.Data[_index] = _defaultValue;
                return 1;
            }

            public Instruction BoxIfIndexMatches(int index)
            {
                return (index == _index) ? new ImmutableBox(index, _defaultValue) : null;
            }

            public override string InstructionName
            {
                get { return "InitImmutableValue"; }
            }
        }

        internal sealed class ImmutableBox : InitializeLocalInstruction
        {
            // immutable value:
            private readonly object _defaultValue;

            internal ImmutableBox(int index, object defaultValue)
                : base(index)
            {
                _defaultValue = defaultValue;
            }

            public override int Run(InterpretedFrame frame)
            {
                frame.Data[_index] = new StrongBox<object>(_defaultValue);
                return 1;
            }

            public override string InstructionName
            {
                get { return "InitImmutableBox"; }
            }
        }

        internal sealed class ParameterBox : InitializeLocalInstruction
        {
            public ParameterBox(int index)
                : base(index)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                frame.Data[_index] = new StrongBox<object>(frame.Data[_index]);
                return 1;
            }
        }

        internal sealed class Parameter : InitializeLocalInstruction, IBoxableInstruction
        {
            internal Parameter(int index)
                : base(index)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                // nop
                return 1;
            }

            public Instruction BoxIfIndexMatches(int index)
            {
                if (index == _index)
                {
                    return InstructionList.ParameterBox(index);
                }

                return null;
            }

            public override string InstructionName
            {
                get { return "InitParameter"; }
            }
        }

        internal sealed class MutableValue : InitializeLocalInstruction, IBoxableInstruction
        {
            private readonly Type _type;

            internal MutableValue(int index, Type type)
                : base(index)
            {
                _type = type;
            }

            public override int Run(InterpretedFrame frame)
            {
                try
                {
                    frame.Data[_index] = Activator.CreateInstance(_type);
                }
                catch (TargetInvocationException e)
                {
                    ExceptionHelpers.UpdateForRethrow(e.InnerException);
                    throw e.InnerException;
                }

                return 1;
            }

            public Instruction BoxIfIndexMatches(int index)
            {
                return (index == _index) ? new MutableBox(index, _type) : null;
            }

            public override string InstructionName
            {
                get { return "InitMutableValue"; }
            }
        }

        internal sealed class MutableBox : InitializeLocalInstruction
        {
            private readonly Type _type;

            internal MutableBox(int index, Type type)
                : base(index)
            {
                _type = type;
            }

            public override int Run(InterpretedFrame frame)
            {
                frame.Data[_index] = new StrongBox<object>(Activator.CreateInstance(_type));
                return 1;
            }

            public override string InstructionName
            {
                get { return "InitMutableBox"; }
            }
        }
    }

    #endregion

    #region RuntimeVariables

    internal sealed class RuntimeVariablesInstruction : Instruction
    {
        private readonly int _count;

        public RuntimeVariablesInstruction(int count)
        {
            _count = count;
        }

        public override int ProducedStack { get { return 1; } }

        public override int ConsumedStack { get { return _count; } }

        public override int Run(InterpretedFrame frame)
        {
            var ret = new IStrongBox[_count];
            for (int i = ret.Length - 1; i >= 0; i--)
            {
                ret[i] = (IStrongBox)frame.Pop();
            }

            frame.Push(RuntimeVariables.Create(ret));
            return +1;
        }

        public override string ToString()
        {
            return "GetRuntimeVariables()";
        }
    }

    #endregion
}
