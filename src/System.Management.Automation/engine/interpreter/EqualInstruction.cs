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

namespace System.Management.Automation.Interpreter
{
    internal abstract class EqualInstruction : Instruction
    {
        // Perf: EqualityComparer<T> but is 3/2 to 2 times slower.
        private static Instruction s_reference,s_boolean,s_SByte,s_int16,s_char,s_int32,s_int64,s_byte,s_UInt16,s_UInt32,s_UInt64,s_single,s_double;

        public override int ConsumedStack { get { return 2; } }

        public override int ProducedStack { get { return 1; } }

        private EqualInstruction()
        {
        }

        internal sealed class EqualBoolean : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(((bool)frame.Pop()) == ((bool)frame.Pop()));
                return +1;
            }
        }

        internal sealed class EqualSByte : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(((SByte)frame.Pop()) == ((SByte)frame.Pop()));
                return +1;
            }
        }

        internal sealed class EqualInt16 : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(((Int16)frame.Pop()) == ((Int16)frame.Pop()));
                return +1;
            }
        }

        internal sealed class EqualChar : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(((char)frame.Pop()) == ((char)frame.Pop()));
                return +1;
            }
        }

        internal sealed class EqualInt32 : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(((Int32)frame.Pop()) == ((Int32)frame.Pop()));
                return +1;
            }
        }

        internal sealed class EqualInt64 : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(((Int64)frame.Pop()) == ((Int64)frame.Pop()));
                return +1;
            }
        }

        internal sealed class EqualByte : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(((Byte)frame.Pop()) == ((Byte)frame.Pop()));
                return +1;
            }
        }

        internal sealed class EqualUInt16 : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(((UInt16)frame.Pop()) == ((UInt16)frame.Pop()));
                return +1;
            }
        }

        internal sealed class EqualUInt32 : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(((UInt32)frame.Pop()) == ((UInt32)frame.Pop()));
                return +1;
            }
        }

        internal sealed class EqualUInt64 : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(((UInt64)frame.Pop()) == ((UInt64)frame.Pop()));
                return +1;
            }
        }

        internal sealed class EqualSingle : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(((Single)frame.Pop()) == ((Single)frame.Pop()));
                return +1;
            }
        }

        internal sealed class EqualDouble : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(((Double)frame.Pop()) == ((Double)frame.Pop()));
                return +1;
            }
        }

        internal sealed class EqualReference : EqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(frame.Pop() == frame.Pop());
                return +1;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static Instruction Create(Type type)
        {
            // Boxed enums can be unboxed as their underlying types:
            var typeToUse = type.IsEnum ? Enum.GetUnderlyingType(type) : type;
            switch (typeToUse.GetTypeCode())
            {
                case TypeCode.Boolean: return s_boolean ?? (s_boolean = new EqualBoolean());
                case TypeCode.SByte: return s_SByte ?? (s_SByte = new EqualSByte());
                case TypeCode.Byte: return s_byte ?? (s_byte = new EqualByte());
                case TypeCode.Char: return s_char ?? (s_char = new EqualChar());
                case TypeCode.Int16: return s_int16 ?? (s_int16 = new EqualInt16());
                case TypeCode.Int32: return s_int32 ?? (s_int32 = new EqualInt32());
                case TypeCode.Int64: return s_int64 ?? (s_int64 = new EqualInt64());

                case TypeCode.UInt16: return s_UInt16 ?? (s_UInt16 = new EqualInt16());
                case TypeCode.UInt32: return s_UInt32 ?? (s_UInt32 = new EqualInt32());
                case TypeCode.UInt64: return s_UInt64 ?? (s_UInt64 = new EqualInt64());

                case TypeCode.Single: return s_single ?? (s_single = new EqualSingle());
                case TypeCode.Double: return s_double ?? (s_double = new EqualDouble());

                case TypeCode.Object:
                    if (!type.IsValueType)
                    {
                        return s_reference ?? (s_reference = new EqualReference());
                    }
                    // TODO: Nullable<T>
                    throw new NotImplementedException();

                default:
                    throw new NotImplementedException();
            }
        }

        public override string ToString()
        {
            return "Equal()";
        }
    }
}
