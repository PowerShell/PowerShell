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
    internal abstract class AddInstruction : Instruction
    {
        private static Instruction s_int16, s_int32, s_int64, s_UInt16, s_UInt32, s_UInt64, s_single, s_double;

        public override int ConsumedStack { get { return 2; } }

        public override int ProducedStack { get { return 1; } }

        private AddInstruction()
        {
        }

        internal sealed class AddInt32 : AddInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = ScriptingRuntimeHelpers.Int32ToObject(unchecked((int)l + (int)r));
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddInt16 : AddInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (short)unchecked((short)l + (short)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddInt64 : AddInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (long)unchecked((long)l + (long)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddUInt16 : AddInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (ushort)unchecked((ushort)l + (ushort)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddUInt32 : AddInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (uint)unchecked((uint)l + (uint)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddUInt64 : AddInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (ulong)unchecked((short)l + (short)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddSingle : AddInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (float)((float)l + (float)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddDouble : AddInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (double)l + (double)r;
                frame.StackIndex--;
                return +1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.IsEnum);
            switch (type.GetTypeCode())
            {
                case TypeCode.Int16: return s_int16 ??= new AddInt16();
                case TypeCode.Int32: return s_int32 ??= new AddInt32();
                case TypeCode.Int64: return s_int64 ??= new AddInt64();
                case TypeCode.UInt16: return s_UInt16 ??= new AddUInt16();
                case TypeCode.UInt32: return s_UInt32 ??= new AddUInt32();
                case TypeCode.UInt64: return s_UInt64 ??= new AddUInt64();
                case TypeCode.Single: return s_single ??= new AddSingle();
                case TypeCode.Double: return s_double ??= new AddDouble();

                default:
                    throw Assert.Unreachable;
            }
        }

        public override string ToString()
        {
            return "Add()";
        }
    }

    internal abstract class AddOvfInstruction : Instruction
    {
        private static Instruction s_int16, s_int32, s_int64, s_UInt16, s_UInt32, s_UInt64, s_single, s_double;

        public override int ConsumedStack { get { return 2; } }

        public override int ProducedStack { get { return 1; } }

        private AddOvfInstruction()
        {
        }

        internal sealed class AddOvfInt32 : AddOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = ScriptingRuntimeHelpers.Int32ToObject(checked((int)l + (int)r));
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddOvfInt16 : AddOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (short)checked((short)l + (short)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddOvfInt64 : AddOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (long)checked((long)l + (long)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddOvfUInt16 : AddOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (ushort)checked((ushort)l + (ushort)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddOvfUInt32 : AddOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (uint)checked((uint)l + (uint)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddOvfUInt64 : AddOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (ulong)checked((short)l + (short)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddOvfSingle : AddOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (float)((float)l + (float)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class AddOvfDouble : AddOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (double)l + (double)r;
                frame.StackIndex--;
                return +1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.IsEnum);
            switch (type.GetTypeCode())
            {
                case TypeCode.Int16: return s_int16 ??= new AddOvfInt16();
                case TypeCode.Int32: return s_int32 ??= new AddOvfInt32();
                case TypeCode.Int64: return s_int64 ??= new AddOvfInt64();
                case TypeCode.UInt16: return s_UInt16 ??= new AddOvfUInt16();
                case TypeCode.UInt32: return s_UInt32 ??= new AddOvfUInt32();
                case TypeCode.UInt64: return s_UInt64 ??= new AddOvfUInt64();
                case TypeCode.Single: return s_single ??= new AddOvfSingle();
                case TypeCode.Double: return s_double ??= new AddOvfDouble();

                default:
                    throw Assert.Unreachable;
            }
        }

        public override string ToString()
        {
            return "AddOvf()";
        }
    }
}
