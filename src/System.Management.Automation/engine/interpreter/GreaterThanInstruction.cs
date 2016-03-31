/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Apache License, Version 2.0, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Management.Automation.Interpreter {
    internal abstract class GreaterThanInstruction : Instruction {
        private static Instruction _SByte, _Int16, _Char, _Int32, _Int64, _Byte, _UInt16, _UInt32, _UInt64, _Single, _Double;

        public override int ConsumedStack { get { return 2; } }
        public override int ProducedStack { get { return 1; } }

        private GreaterThanInstruction() {
        }

        internal sealed class GreaterThanSByte : GreaterThanInstruction {
            public override int Run(InterpretedFrame frame) {
                SByte right = (SByte)frame.Pop();
                frame.Push(((SByte)frame.Pop()) > right);
                return +1;
            }
        }

        internal sealed class GreaterThanInt16 : GreaterThanInstruction {
            public override int Run(InterpretedFrame frame) {
                Int16 right = (Int16)frame.Pop();
                frame.Push(((Int16)frame.Pop()) > right);
                return +1;
            }
        }

        internal sealed class GreaterThanChar : GreaterThanInstruction {
            public override int Run(InterpretedFrame frame) {
                Char right = (Char)frame.Pop();
                frame.Push(((Char)frame.Pop()) > right);
                return +1;
            }
        }

        internal sealed class GreaterThanInt32 : GreaterThanInstruction {
            public override int Run(InterpretedFrame frame) {
                Int32 right = (Int32)frame.Pop();
                frame.Push(((Int32)frame.Pop()) > right);
                return +1;
            }
        }

        internal sealed class GreaterThanInt64 : GreaterThanInstruction {
            public override int Run(InterpretedFrame frame) {
                Int64 right = (Int64)frame.Pop();
                frame.Push(((Int64)frame.Pop()) > right);
                return +1;
            }
        }

        internal sealed class GreaterThanByte : GreaterThanInstruction {
            public override int Run(InterpretedFrame frame) {
                Byte right = (Byte)frame.Pop();
                frame.Push(((Byte)frame.Pop()) > right);
                return +1;
            }
        }

        internal sealed class GreaterThanUInt16 : GreaterThanInstruction {
            public override int Run(InterpretedFrame frame) {
                UInt16 right = (UInt16)frame.Pop();
                frame.Push(((UInt16)frame.Pop()) > right);
                return +1;
            }
        }

        internal sealed class GreaterThanUInt32 : GreaterThanInstruction {
            public override int Run(InterpretedFrame frame) {
                UInt32 right = (UInt32)frame.Pop();
                frame.Push(((UInt32)frame.Pop()) > right);
                return +1;
            }
        }

        internal sealed class GreaterThanUInt64 : GreaterThanInstruction {
            public override int Run(InterpretedFrame frame) {
                UInt64 right = (UInt64)frame.Pop();
                frame.Push(((UInt64)frame.Pop()) > right);
                return +1;
            }
        }

        internal sealed class GreaterThanSingle : GreaterThanInstruction {
            public override int Run(InterpretedFrame frame) {
                Single right = (Single)frame.Pop();
                frame.Push(((Single)frame.Pop()) > right);
                return +1;
            }
        }

        internal sealed class GreaterThanDouble : GreaterThanInstruction {
            public override int Run(InterpretedFrame frame) {
                Double right = (Double)frame.Pop();
                frame.Push(((Double)frame.Pop()) > right);
                return +1;
            }
        }

        public static Instruction Create(Type type) {
            Debug.Assert(!type.GetTypeInfo().IsEnum);
            switch (type.GetTypeCode()) {
                case TypeCode.SByte: return _SByte ?? (_SByte = new GreaterThanSByte());
                case TypeCode.Byte: return _Byte ?? (_Byte = new GreaterThanByte());
                case TypeCode.Char: return _Char ?? (_Char = new GreaterThanChar());
                case TypeCode.Int16: return _Int16 ?? (_Int16 = new GreaterThanInt16());
                case TypeCode.Int32: return _Int32 ?? (_Int32 = new GreaterThanInt32());
                case TypeCode.Int64: return _Int64 ?? (_Int64 = new GreaterThanInt64());
                case TypeCode.UInt16: return _UInt16 ?? (_UInt16 = new GreaterThanUInt16());
                case TypeCode.UInt32: return _UInt32 ?? (_UInt32 = new GreaterThanUInt32());
                case TypeCode.UInt64: return _UInt64 ?? (_UInt64 = new GreaterThanUInt64());
                case TypeCode.Single: return _Single ?? (_Single = new GreaterThanSingle());
                case TypeCode.Double: return _Double ?? (_Double = new GreaterThanDouble());

                default:
                    throw Assert.Unreachable;
            }
        }

        public override string ToString() {
            return "GreaterThan()";
        }
    }
}
