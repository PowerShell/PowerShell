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
    internal abstract class SubInstruction : Instruction
    {
        private static Instruction s_int16, s_int32, s_int64, s_UInt16, s_UInt32, s_UInt64, s_single, s_double;

        public override int ConsumedStack { get { return 2; } }

        public override int ProducedStack { get { return 1; } }

        private SubInstruction()
        {
        }

        internal sealed class SubInt32 : SubInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = ScriptingRuntimeHelpers.Int32ToObject(unchecked((int)l - (int)r));
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubInt16 : SubInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (short)unchecked((short)l - (short)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubInt64 : SubInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (long)unchecked((long)l - (long)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubUInt16 : SubInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (ushort)unchecked((ushort)l - (ushort)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubUInt32 : SubInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (uint)unchecked((uint)l - (uint)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubUInt64 : SubInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (ulong)unchecked((short)l - (short)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubSingle : SubInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (float)((float)l - (float)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubDouble : SubInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (double)l - (double)r;
                frame.StackIndex--;
                return +1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.IsEnum);
            switch (type.GetTypeCode())
            {
                case TypeCode.Int16: return s_int16 ??= new SubInt16();
                case TypeCode.Int32: return s_int32 ??= new SubInt32();
                case TypeCode.Int64: return s_int64 ??= new SubInt64();
                case TypeCode.UInt16: return s_UInt16 ??= new SubUInt16();
                case TypeCode.UInt32: return s_UInt32 ??= new SubUInt32();
                case TypeCode.UInt64: return s_UInt64 ??= new SubUInt64();
                case TypeCode.Single: return s_single ??= new SubSingle();
                case TypeCode.Double: return s_double ??= new SubDouble();

                default:
                    throw Assert.Unreachable;
            }
        }

        public override string ToString()
        {
            return "Sub()";
        }
    }

    internal abstract class SubOvfInstruction : Instruction
    {
        private static Instruction s_int16, s_int32, s_int64, s_UInt16, s_UInt32, s_UInt64, s_single, s_double;

        public override int ConsumedStack { get { return 2; } }

        public override int ProducedStack { get { return 1; } }

        private SubOvfInstruction()
        {
        }

        internal sealed class SubOvfInt32 : SubOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = ScriptingRuntimeHelpers.Int32ToObject(checked((int)l - (int)r));
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubOvfInt16 : SubOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (short)checked((short)l - (short)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubOvfInt64 : SubOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (long)checked((long)l - (long)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubOvfUInt16 : SubOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (ushort)checked((ushort)l - (ushort)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubOvfUInt32 : SubOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (uint)checked((uint)l - (uint)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubOvfUInt64 : SubOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (ulong)checked((short)l - (short)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubOvfSingle : SubOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (float)((float)l - (float)r);
                frame.StackIndex--;
                return +1;
            }
        }

        internal sealed class SubOvfDouble : SubOvfInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                object l = frame.Data[frame.StackIndex - 2];
                object r = frame.Data[frame.StackIndex - 1];
                frame.Data[frame.StackIndex - 2] = (double)l - (double)r;
                frame.StackIndex--;
                return +1;
            }
        }

        public static Instruction Create(Type type)
        {
            Debug.Assert(!type.IsEnum);
            switch (type.GetTypeCode())
            {
                case TypeCode.Int16: return s_int16 ??= new SubOvfInt16();
                case TypeCode.Int32: return s_int32 ??= new SubOvfInt32();
                case TypeCode.Int64: return s_int64 ??= new SubOvfInt64();
                case TypeCode.UInt16: return s_UInt16 ??= new SubOvfUInt16();
                case TypeCode.UInt32: return s_UInt32 ??= new SubOvfUInt32();
                case TypeCode.UInt64: return s_UInt64 ??= new SubOvfUInt64();
                case TypeCode.Single: return s_single ??= new SubOvfSingle();
                case TypeCode.Double: return s_double ??= new SubOvfDouble();

                default:
                    throw Assert.Unreachable;
            }
        }

        public override string ToString()
        {
            return "SubOvf()";
        }
    }
}
