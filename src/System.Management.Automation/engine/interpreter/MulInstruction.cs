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
    internal abstract class MulInstruction : Instruction
    {
        private static Instruction s_int16,s_int32,s_int64,s_UInt16,s_UInt32,s_UInt64,s_single,s_double;

        public override int ConsumedStack { get { return 2; } }

        public override int ProducedStack { get { return 1; } }

        private MulInstruction()
        {
        }

        internal sealed class MulInt32 : MulInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = ScriptingRuntimeHelpers.Int32ToObject(unchecked((Int32)l * (Int32)r));
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class MulInt16 : MulInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (Int16)unchecked((Int16)l * (Int16)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class MulInt64 : MulInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (Int64)unchecked((Int64)l * (Int64)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class MulUInt16 : MulInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (UInt16)unchecked((UInt16)l * (UInt16)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class MulUInt32 : MulInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (UInt32)unchecked((UInt32)l * (UInt32)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class MulUInt64 : MulInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (UInt64)unchecked((Int16)l * (Int16)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class MulSingle : MulInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (Single)((Single)l * (Single)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class MulDouble : MulInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (Double)l * (Double)r;
                frame.StackIndex--;
                return +1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.IsEnum);
            switch (type.GetTypeCode())
            {
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new MulInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new MulInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new MulInt64());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new MulUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new MulUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new MulUInt64());
                case TypeCode.Single: return s_single ?? (s_single = new MulSingle());
                case TypeCode.Double: return s_double ?? (s_double = new MulDouble());

                default:
                    throw Assert.Unreachable;
            }
        }

        public override string ToString()
        {
            return "Mul()";
        }
    }

    internal abstract class MulOvfInstruction : Instruction
    {
        private static Instruction s_int16,s_int32,s_int64,s_UInt16,s_UInt32,s_UInt64,s_single,s_double;

        public override int ConsumedStack { get { return 2; } }

        public override int ProducedStack { get { return 1; } }

        private MulOvfInstruction()
        {
        }

        internal sealed class MulOvfInt32 : MulOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = ScriptingRuntimeHelpers.Int32ToObject(checked((Int32)l * (Int32)r));
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class MulOvfInt16 : MulOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (Int16)checked((Int16)l * (Int16)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class MulOvfInt64 : MulOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (Int64)checked((Int64)l * (Int64)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class MulOvfUInt16 : MulOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (UInt16)checked((UInt16)l * (UInt16)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class MulOvfUInt32 : MulOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (UInt32)checked((UInt32)l * (UInt32)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class MulOvfUInt64 : MulOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (UInt64)checked((Int16)l * (Int16)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class MulOvfSingle : MulOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (Single)((Single)l * (Single)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class MulOvfDouble : MulOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (Double)l * (Double)r;
                frame.StackIndex--;
                return +1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.IsEnum);
            switch (type.GetTypeCode())
            {
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new MulOvfInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new MulOvfInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new MulOvfInt64());
                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new MulOvfUInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new MulOvfUInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new MulOvfUInt64());
                case TypeCode.Single: return s_single ?? (s_single = new MulOvfSingle());
                case TypeCode.Double: return s_double ?? (s_double = new MulOvfDouble());

                default:
                    throw Assert.Unreachable;
            }
        }

        public override string ToString()
        {
            return "MulOvf()";
        }
    }
}
