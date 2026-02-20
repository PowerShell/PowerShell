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
    internal abstract class NumericConvertInstruction : Instruction
    {
        internal readonly TypeCode _from, _to;

        protected NumericConvertInstruction(TypeCode from, TypeCode to)
        {
            _from = from;
            _to = to;
        }

        public override int ConsumedStack { get { return 1; } }

        public override int ProducedStack { get { return 1; } }

        public override string ToString()
        {
            return InstructionName + "(" + _from + "->" + _to + ")";
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        internal sealed class Unchecked : NumericConvertInstruction
        {
            public override string InstructionName { get { return "UncheckedConvert"; } }

            public Unchecked(TypeCode from, TypeCode to)
                : base(from, to)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                frame.Push(Convert(frame.Pop()));
                return +1;
            }

            private object Convert(object obj)
            {
                switch (_from)
                {
                    case TypeCode.Byte: return ConvertInt32((byte)obj);
                    case TypeCode.SByte: return ConvertInt32((sbyte)obj);
                    case TypeCode.Int16: return ConvertInt32((short)obj);
                    case TypeCode.Char: return ConvertInt32((char)obj);
                    case TypeCode.Int32: return ConvertInt32((int)obj);
                    case TypeCode.Int64: return ConvertInt64((long)obj);
                    case TypeCode.UInt16: return ConvertInt32((ushort)obj);
                    case TypeCode.UInt32: return ConvertInt64((uint)obj);
                    case TypeCode.UInt64: return ConvertUInt64((ulong)obj);
                    case TypeCode.Single: return ConvertDouble((float)obj);
                    case TypeCode.Double: return ConvertDouble((double)obj);
                    default: throw Assert.Unreachable;
                }
            }

            private object ConvertInt32(int obj)
            {
                unchecked
                {
                    switch (_to)
                    {
                        case TypeCode.Byte: return (byte)obj;
                        case TypeCode.SByte: return (sbyte)obj;
                        case TypeCode.Int16: return (short)obj;
                        case TypeCode.Char: return (char)obj;
                        case TypeCode.Int32: return (int)obj;
                        case TypeCode.Int64: return (long)obj;
                        case TypeCode.UInt16: return (ushort)obj;
                        case TypeCode.UInt32: return (uint)obj;
                        case TypeCode.UInt64: return (ulong)obj;
                        case TypeCode.Single: return (float)obj;
                        case TypeCode.Double: return (double)obj;
                        default: throw Assert.Unreachable;
                    }
                }
            }

            private object ConvertInt64(long obj)
            {
                unchecked
                {
                    switch (_to)
                    {
                        case TypeCode.Byte: return (byte)obj;
                        case TypeCode.SByte: return (sbyte)obj;
                        case TypeCode.Int16: return (short)obj;
                        case TypeCode.Char: return (char)obj;
                        case TypeCode.Int32: return (int)obj;
                        case TypeCode.Int64: return (long)obj;
                        case TypeCode.UInt16: return (ushort)obj;
                        case TypeCode.UInt32: return (uint)obj;
                        case TypeCode.UInt64: return (ulong)obj;
                        case TypeCode.Single: return (float)obj;
                        case TypeCode.Double: return (double)obj;
                        default: throw Assert.Unreachable;
                    }
                }
            }

            private object ConvertUInt64(ulong obj)
            {
                unchecked
                {
                    switch (_to)
                    {
                        case TypeCode.Byte: return (byte)obj;
                        case TypeCode.SByte: return (sbyte)obj;
                        case TypeCode.Int16: return (short)obj;
                        case TypeCode.Char: return (char)obj;
                        case TypeCode.Int32: return (int)obj;
                        case TypeCode.Int64: return (long)obj;
                        case TypeCode.UInt16: return (ushort)obj;
                        case TypeCode.UInt32: return (uint)obj;
                        case TypeCode.UInt64: return (ulong)obj;
                        case TypeCode.Single: return (float)obj;
                        case TypeCode.Double: return (double)obj;
                        default: throw Assert.Unreachable;
                    }
                }
            }

            private object ConvertDouble(double obj)
            {
                unchecked
                {
                    switch (_to)
                    {
                        case TypeCode.Byte: return (byte)obj;
                        case TypeCode.SByte: return (sbyte)obj;
                        case TypeCode.Int16: return (short)obj;
                        case TypeCode.Char: return (char)obj;
                        case TypeCode.Int32: return (int)obj;
                        case TypeCode.Int64: return (long)obj;
                        case TypeCode.UInt16: return (ushort)obj;
                        case TypeCode.UInt32: return (uint)obj;
                        case TypeCode.UInt64: return (ulong)obj;
                        case TypeCode.Single: return (float)obj;
                        case TypeCode.Double: return (double)obj;
                        default: throw Assert.Unreachable;
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        internal sealed class Checked : NumericConvertInstruction
        {
            public override string InstructionName { get { return "CheckedConvert"; } }

            public Checked(TypeCode from, TypeCode to)
                : base(from, to)
            {
            }

            public override int Run(InterpretedFrame frame)
            {
                frame.Push(Convert(frame.Pop()));
                return +1;
            }

            private object Convert(object obj)
            {
                switch (_from)
                {
                    case TypeCode.Byte: return ConvertInt32((byte)obj);
                    case TypeCode.SByte: return ConvertInt32((sbyte)obj);
                    case TypeCode.Int16: return ConvertInt32((short)obj);
                    case TypeCode.Char: return ConvertInt32((char)obj);
                    case TypeCode.Int32: return ConvertInt32((int)obj);
                    case TypeCode.Int64: return ConvertInt64((long)obj);
                    case TypeCode.UInt16: return ConvertInt32((ushort)obj);
                    case TypeCode.UInt32: return ConvertInt64((uint)obj);
                    case TypeCode.UInt64: return ConvertUInt64((ulong)obj);
                    case TypeCode.Single: return ConvertDouble((float)obj);
                    case TypeCode.Double: return ConvertDouble((double)obj);
                    default: throw Assert.Unreachable;
                }
            }

            private object ConvertInt32(int obj)
            {
                checked
                {
                    switch (_to)
                    {
                        case TypeCode.Byte: return (byte)obj;
                        case TypeCode.SByte: return (sbyte)obj;
                        case TypeCode.Int16: return (short)obj;
                        case TypeCode.Char: return (char)obj;
                        case TypeCode.Int32: return (int)obj;
                        case TypeCode.Int64: return (long)obj;
                        case TypeCode.UInt16: return (ushort)obj;
                        case TypeCode.UInt32: return (uint)obj;
                        case TypeCode.UInt64: return (ulong)obj;
                        case TypeCode.Single: return (float)obj;
                        case TypeCode.Double: return (double)obj;
                        default: throw Assert.Unreachable;
                    }
                }
            }

            private object ConvertInt64(long obj)
            {
                checked
                {
                    switch (_to)
                    {
                        case TypeCode.Byte: return (byte)obj;
                        case TypeCode.SByte: return (sbyte)obj;
                        case TypeCode.Int16: return (short)obj;
                        case TypeCode.Char: return (char)obj;
                        case TypeCode.Int32: return (int)obj;
                        case TypeCode.Int64: return (long)obj;
                        case TypeCode.UInt16: return (ushort)obj;
                        case TypeCode.UInt32: return (uint)obj;
                        case TypeCode.UInt64: return (ulong)obj;
                        case TypeCode.Single: return (float)obj;
                        case TypeCode.Double: return (double)obj;
                        default: throw Assert.Unreachable;
                    }
                }
            }

            private object ConvertUInt64(ulong obj)
            {
                checked
                {
                    switch (_to)
                    {
                        case TypeCode.Byte: return (byte)obj;
                        case TypeCode.SByte: return (sbyte)obj;
                        case TypeCode.Int16: return (short)obj;
                        case TypeCode.Char: return (char)obj;
                        case TypeCode.Int32: return (int)obj;
                        case TypeCode.Int64: return (long)obj;
                        case TypeCode.UInt16: return (ushort)obj;
                        case TypeCode.UInt32: return (uint)obj;
                        case TypeCode.UInt64: return (ulong)obj;
                        case TypeCode.Single: return (float)obj;
                        case TypeCode.Double: return (double)obj;
                        default: throw Assert.Unreachable;
                    }
                }
            }

            private object ConvertDouble(double obj)
            {
                checked
                {
                    switch (_to)
                    {
                        case TypeCode.Byte: return (byte)obj;
                        case TypeCode.SByte: return (sbyte)obj;
                        case TypeCode.Int16: return (short)obj;
                        case TypeCode.Char: return (char)obj;
                        case TypeCode.Int32: return (int)obj;
                        case TypeCode.Int64: return (long)obj;
                        case TypeCode.UInt16: return (ushort)obj;
                        case TypeCode.UInt32: return (uint)obj;
                        case TypeCode.UInt64: return (ulong)obj;
                        case TypeCode.Single: return (float)obj;
                        case TypeCode.Double: return (double)obj;
                        default: throw Assert.Unreachable;
                    }
                }
            }
        }
    }
}
