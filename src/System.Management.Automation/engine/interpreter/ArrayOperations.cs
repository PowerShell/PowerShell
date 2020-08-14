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

namespace System.Management.Automation.Interpreter
{
    internal sealed class NewArrayInitInstruction<TElement> : Instruction
    {
        private readonly int _elementCount;

        internal NewArrayInitInstruction(int elementCount)
        {
            _elementCount = elementCount;
        }

        internal override int ConsumedStack { get { return _elementCount; } }

        internal override int ProducedStack { get { return 1; } }

        internal override int Run(InterpretedFrame frame)
        {
            TElement[] array = new TElement[_elementCount];
            for (int i = _elementCount - 1; i >= 0; i--)
            {
                array[i] = (TElement)frame.Pop();
            }

            frame.Push(array);
            return +1;
        }
    }

    internal sealed class NewArrayInstruction<TElement> : Instruction
    {
        internal NewArrayInstruction() { }

        internal override int ConsumedStack { get { return 1; } }

        internal override int ProducedStack { get { return 1; } }

        internal override int Run(InterpretedFrame frame)
        {
            int length = (int)frame.Pop();
            frame.Push(new TElement[length]);
            return +1;
        }
    }

    internal sealed class NewArrayBoundsInstruction : Instruction
    {
        private readonly Type _elementType;
        private readonly int _rank;

        internal NewArrayBoundsInstruction(Type elementType, int rank)
        {
            _elementType = elementType;
            _rank = rank;
        }

        internal override int ConsumedStack { get { return _rank; } }

        internal override int ProducedStack { get { return 1; } }

        internal override int Run(InterpretedFrame frame)
        {
            var lengths = new int[_rank];
            for (int i = _rank - 1; i >= 0; i--)
            {
                lengths[i] = (int)frame.Pop();
            }

            var array = Array.CreateInstance(_elementType, lengths);
            frame.Push(array);
            return +1;
        }
    }

    internal sealed class GetArrayItemInstruction<TElement> : Instruction
    {
        internal GetArrayItemInstruction() { }

        internal override int ConsumedStack { get { return 2; } }

        internal override int ProducedStack { get { return 1; } }

        internal override int Run(InterpretedFrame frame)
        {
            int index = (int)frame.Pop();
            TElement[] array = (TElement[])frame.Pop();
            frame.Push(array[index]);
            return +1;
        }

        internal override string InstructionName
        {
            get { return "GetArrayItem"; }
        }
    }

    internal sealed class SetArrayItemInstruction<TElement> : Instruction
    {
        internal SetArrayItemInstruction() { }

        internal override int ConsumedStack { get { return 3; } }

        internal override int ProducedStack { get { return 0; } }

        internal override int Run(InterpretedFrame frame)
        {
            TElement value = (TElement)frame.Pop();
            int index = (int)frame.Pop();
            TElement[] array = (TElement[])frame.Pop();
            array[index] = value;
            return +1;
        }

        internal override string InstructionName
        {
            get { return "SetArrayItem"; }
        }
    }
}
