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

using System.Collections.Generic;
using System.Globalization;

namespace System.Management.Automation.Interpreter
{
    internal sealed class LoadObjectInstruction : Instruction
    {
        private readonly object _value;

        internal LoadObjectInstruction(object value)
        {
            _value = value;
        }

        public override int ProducedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            frame.Data[frame.StackIndex++] = _value;
            return +1;
        }

        public override string ToString()
        {
            return "LoadObject(" + (_value ?? "null") + ")";
        }
    }

    internal sealed class LoadCachedObjectInstruction : Instruction
    {
        private readonly uint _index;

        internal LoadCachedObjectInstruction(uint index)
        {
            _index = index;
        }

        public override int ProducedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            frame.Data[frame.StackIndex++] = frame.Interpreter._objects[_index];
            return +1;
        }

        public override string ToDebugString(int instructionIndex, object cookie, Func<int, int> labelIndexer, IList<object> objects)
        {
            return string.Create(CultureInfo.InvariantCulture, $"LoadCached({_index}: {objects[(int)_index]})");
        }

        public override string ToString()
        {
            return "LoadCached(" + _index + ")";
        }
    }

    internal sealed class PopInstruction : Instruction
    {
        internal static readonly PopInstruction Instance = new PopInstruction();

        private PopInstruction() { }

        public override int ConsumedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            frame.Pop();
            return +1;
        }

        public override string ToString()
        {
            return "Pop()";
        }
    }

    internal sealed class DupInstruction : Instruction
    {
        internal static readonly DupInstruction Instance = new DupInstruction();

        private DupInstruction() { }

        public override int ConsumedStack { get { return 0; } }

        public override int ProducedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            frame.Data[frame.StackIndex++] = frame.Peek();
            return +1;
        }

        public override string ToString()
        {
            return "Dup()";
        }
    }
}
