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
    internal abstract class NotEqualInstruction : Instruction
    {
        // Perf: EqualityComparer<T> but is 3/2 to 2 times slower.
        private static Instruction s_reference, s_boolean, s_SByte, s_int16, s_char, s_int32, s_int64, s_byte, s_UInt16, s_UInt32, s_UInt64, s_single, s_double;

        public override int ConsumedStack { get { return 2; } }

        public override int ProducedStack { get { return 1; } }

        private NotEqualInstruction()
        {
        }

        internal sealed class NotEqualBoolean : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(((bool)frame.Pop()) != ((bool)frame.Pop()));
                return +1;
            }
        }

        internal sealed class NotEqualSByte : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(((sbyte)frame.Pop()) != ((sbyte)frame.Pop()));
                return +1;
            }
        }

        internal sealed class NotEqualInt16 : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(((short)frame.Pop()) != ((short)frame.Pop()));
                return +1;
            }
        }

        internal sealed class NotEqualChar : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(((char)frame.Pop()) != ((char)frame.Pop()));
                return +1;
            }
        }

        internal sealed class NotEqualInt32 : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(((int)frame.Pop()) != ((int)frame.Pop()));
                return +1;
            }
        }

        internal sealed class NotEqualInt64 : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(((long)frame.Pop()) != ((long)frame.Pop()));
                return +1;
            }
        }

        internal sealed class NotEqualByte : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(((byte)frame.Pop()) != ((byte)frame.Pop()));
                return +1;
            }
        }

        internal sealed class NotEqualUInt16 : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(((ushort)frame.Pop()) != ((ushort)frame.Pop()));
                return +1;
            }
        }

        internal sealed class NotEqualUInt32 : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(((uint)frame.Pop()) != ((uint)frame.Pop()));
                return +1;
            }
        }

        internal sealed class NotEqualUInt64 : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(((ulong)frame.Pop()) != ((ulong)frame.Pop()));
                return +1;
            }
        }

        internal sealed class NotEqualSingle : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(((float)frame.Pop()) != ((float)frame.Pop()));
                return +1;
            }
        }

        internal sealed class NotEqualDouble : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(((double)frame.Pop()) != ((double)frame.Pop()));
                return +1;
            }
        }

        internal sealed class NotEqualReference : NotEqualInstruction
        {
            public override int Run(InterpretedFrame frame)
            {
                frame.Push(frame.Pop() != frame.Pop());
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
                case TypeCode.Boolean: return s_boolean ??= new NotEqualBoolean();
                case TypeCode.SByte: return s_SByte ??= new NotEqualSByte();
                case TypeCode.Byte: return s_byte ??= new NotEqualByte();
                case TypeCode.Char: return s_char ??= new NotEqualChar();
                case TypeCode.Int16: return s_int16 ??= new NotEqualInt16();
                case TypeCode.Int32: return s_int32 ??= new NotEqualInt32();
                case TypeCode.Int64: return s_int64 ??= new NotEqualInt64();

                case TypeCode.UInt16: return s_UInt16 ??= new NotEqualInt16();
                case TypeCode.UInt32: return s_UInt32 ??= new NotEqualInt32();
                case TypeCode.UInt64: return s_UInt64 ??= new NotEqualInt64();

                case TypeCode.Single: return s_single ??= new NotEqualSingle();
                case TypeCode.Double: return s_double ??= new NotEqualDouble();

                case TypeCode.Object:
                    if (!type.IsValueType)
                    {
                        return s_reference ??= new NotEqualReference();
                    }
                    // TODO: Nullable<T>
                    throw new NotImplementedException();

                default:
                    throw new NotImplementedException();
            }
        }

        public override string ToString()
        {
            return "NotEqual()";
        }
    }
}
