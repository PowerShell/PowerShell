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

using System.Diagnostics;

namespace System.Management.Automation.Interpreter
{
    internal abstract class GreaterThanInstruction : Instruction
    {
        private static Instruction s_SByte,s_int16,s_char,s_int32,s_int64,s_byte,s_UInt16,s_UInt32,s_UInt64,s_single,s_double;

        public override int ConsumedStack { get { return 2; } }

        public override int ProducedStack { get { return 1; } }

        private GreaterThanInstruction()
        {
        }

        internal sealed class GreaterThanSByte : GreaterThanInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                SByte right = (SByte)frame.Pop();
                frame.Push(((SByte)frame.Pop()) > right);
                return +1;
            }
        }

        internal sealed class GreaterThanInt16 : GreaterThanInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                Int16 right = (Int16)frame.Pop();
                frame.Push(((Int16)frame.Pop()) > right);
                return +1;
            }
        }

        internal sealed class GreaterThanChar : GreaterThanInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                char right = (char)frame.Pop();
                frame.Push(((char)frame.Pop()) > right);
                return +1;
            }
        }

        internal sealed class GreaterThanInt32 : GreaterThanInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                Int32 right = (Int32)frame.Pop();
                frame.Push(((Int32)frame.Pop()) > right);
                return +1;
            }
        }

        internal sealed class GreaterThanInt64 : GreaterThanInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                Int64 right = (Int64)frame.Pop();
                frame.Push(((Int64)frame.Pop()) > right);
                return +1;
            }
        }

        internal sealed class GreaterThanByte : GreaterThanInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                Byte right = (Byte)frame.Pop();
                frame.Push(((Byte)frame.Pop()) > right);
                return +1;
            }
        }

        internal sealed class GreaterThanUInt16 : GreaterThanInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                UInt16 right = (UInt16)frame.Pop();
                frame.Push(((UInt16)frame.Pop()) > right);
                return +1;
            }
        }

        internal sealed class GreaterThanUInt32 : GreaterThanInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                UInt32 right = (UInt32)frame.Pop();
                frame.Push(((UInt32)frame.Pop()) > right);
                return +1;
            }
        }

        internal sealed class GreaterThanUInt64 : GreaterThanInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                UInt64 right = (UInt64)frame.Pop();
                frame.Push(((UInt64)frame.Pop()) > right);
                return +1;
            }
        }

        internal sealed class GreaterThanSingle : GreaterThanInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                Single right = (Single)frame.Pop();
                frame.Push(((Single)frame.Pop()) > right);
                return +1;
            }
        }

        internal sealed class GreaterThanDouble : GreaterThanInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                Double right = (Double)frame.Pop();
                frame.Push(((Double)frame.Pop()) > right);
                return +1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.IsEnum);
            switch (type.GetTypeCode())
            {
                case TypeCode.SByte: return s_SByte ?? (s_SByte = new GreaterThanSByte());
                case TypeCode.Byte: return s_byte ?? (s_byte = new GreaterThanByte());
                case TypeCode.Char: return s_char ?? (s_char = new GreaterThanChar());
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new GreaterThanInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new GreaterThanInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new GreaterThanInt64());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new GreaterThanUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new GreaterThanUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new GreaterThanUInt64());
                case TypeCode.Single: return s_single ?? (s_single = new GreaterThanSingle());
                case TypeCode.Double: return s_double ?? (s_double = new GreaterThanDouble());

                default:
                    throw Assert.Unreachable;
            }
        }

        public override string ToString()
        {
            return "GreaterThan()";
        }
    }
}
