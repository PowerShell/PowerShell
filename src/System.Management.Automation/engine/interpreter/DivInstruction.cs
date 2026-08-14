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
    internal abstract class DivInstruction : Instruction
    {
        private static Instruction s_int16, s_int32, s_int64, s_UInt16, s_UInt32, s_UInt64, s_single, s_double;

        public override int ConsumedStack { get { return 2; } }

        public override int ProducedStack { get { return 1; } }

        private DivInstruction()
        {
        }

        internal sealed class DivInt32 : DivInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = ScriptingRuntimeHelpers.Int32ToObject((int)l / (int)r);
                frame.StackIndex--;
                return 1;
            }
        }

        internal sealed class DivInt16 : DivInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (short)((short)l / (short)r);
                frame.StackIndex--;
                return 1;
            }
        }

        internal sealed class DivInt64 : DivInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (long)((long)l / (long)r);
                frame.StackIndex--;
                return 1;
            }
        }

        internal sealed class DivUInt16 : DivInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (ushort)((ushort)l / (ushort)r);
                frame.StackIndex--;
                return 1;
            }
        }

        internal sealed class DivUInt32 : DivInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (uint)((uint)l / (uint)r);
                frame.StackIndex--;
                return 1;
            }
        }

        internal sealed class DivUInt64 : DivInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (ulong)((short)l / (short)r);
                frame.StackIndex--;
                return 1;
            }
        }

        internal sealed class DivSingle : DivInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (float)((float)l / (float)r);
                frame.StackIndex--;
                return 1;
            }
        }

        internal sealed class DivDouble : DivInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (double)l / (double)r;
                frame.StackIndex--;
                return 1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.IsEnum);
            switch (type.GetTypeCode())
            {
                case TypeCode.Int16: return s_int16 ??= new DivInt16();
                case TypeCode.Int32: return s_int32 ??= new DivInt32();
                case TypeCode.Int64: return s_int64 ??= new DivInt64();
                case TypeCode.UInt16: return s_UInt16 ??= new DivUInt16();
                case TypeCode.UInt32: return s_UInt32 ??= new DivUInt32();
                case TypeCode.UInt64: return s_UInt64 ??= new DivUInt64();
                case TypeCode.Single: return s_single ??= new DivSingle();
                case TypeCode.Double: return s_double ??= new DivDouble();

                default:
                    throw Assert.Unreachable;
            }
        }

        public override string ToString()
        {
            return "Div()";
        }
    }
}
