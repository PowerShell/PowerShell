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
    internal abstract class EqualInstruction : Instruction {
        // Perf: EqualityComparer<T> but is 3/2 to 2 times slower.
        private static Instruction _Reference, _Boolean, _SByte, _Int16, _Char, _Int32, _Int64, _Byte, _UInt16, _UInt32, _UInt64, _Single, _Double;

        public override int ConsumedStack { get { return 2; } }
        public override int ProducedStack { get { return 1; } }

        private EqualInstruction() {
        }

        internal sealed class EqualBoolean : EqualInstruction {
            public override int Run(InterpretedFrame frame) {
                frame.Push(((Boolean)frame.Pop()) == ((Boolean)frame.Pop()));
                return +1;
            }
        }

        internal sealed class EqualSByte : EqualInstruction {
            public override int Run(InterpretedFrame frame) {
                frame.Push(((SByte)frame.Pop()) == ((SByte)frame.Pop()));
                return +1;
            }
        }

        internal sealed class EqualInt16 : EqualInstruction {
            public override int Run(InterpretedFrame frame) {
                frame.Push(((Int16)frame.Pop()) == ((Int16)frame.Pop()));
                return +1;
            }
        }

        internal sealed class EqualChar : EqualInstruction {
            public override int Run(InterpretedFrame frame) {
                frame.Push(((Char)frame.Pop()) == ((Char)frame.Pop()));
                return +1;
            }
        }

        internal sealed class EqualInt32 : EqualInstruction {
            public override int Run(InterpretedFrame frame) {
                frame.Push(((Int32)frame.Pop()) == ((Int32)frame.Pop()));
                return +1;
            }
        }

        internal sealed class EqualInt64 : EqualInstruction {
            public override int Run(InterpretedFrame frame) {
                frame.Push(((Int64)frame.Pop()) == ((Int64)frame.Pop()));
                return +1;
            }
        }

        internal sealed class EqualByte : EqualInstruction {
            public override int Run(InterpretedFrame frame) {
                frame.Push(((Byte)frame.Pop()) == ((Byte)frame.Pop()));
                return +1;
            }
        }

        internal sealed class EqualUInt16 : EqualInstruction {
            public override int Run(InterpretedFrame frame) {
                frame.Push(((UInt16)frame.Pop()) == ((UInt16)frame.Pop()));
                return +1;
            }
        }

        internal sealed class EqualUInt32 : EqualInstruction {
            public override int Run(InterpretedFrame frame) {
                frame.Push(((UInt32)frame.Pop()) == ((UInt32)frame.Pop()));
                return +1;
            }
        }

        internal sealed class EqualUInt64 : EqualInstruction {
            public override int Run(InterpretedFrame frame) {
                frame.Push(((UInt64)frame.Pop()) == ((UInt64)frame.Pop()));
                return +1;
            }
        }

        internal sealed class EqualSingle : EqualInstruction {
            public override int Run(InterpretedFrame frame) {
                frame.Push(((Single)frame.Pop()) == ((Single)frame.Pop()));
                return +1;
            }
        }

        internal sealed class EqualDouble : EqualInstruction {
            public override int Run(InterpretedFrame frame) {
                frame.Push(((Double)frame.Pop()) == ((Double)frame.Pop()));
                return +1;
            }
        }

        internal sealed class EqualReference : EqualInstruction {
            public override int Run(InterpretedFrame frame) {
                frame.Push(frame.Pop() == frame.Pop());
                return +1;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static Instruction Create(Type type) {
            var typeInfo = type.GetTypeInfo();
            // Boxed enums can be unboxed as their underlying types:
            var typeToUse = typeInfo.IsEnum ? Enum.GetUnderlyingType(type) : type;
            switch (typeToUse.GetTypeCode()) {
                case TypeCode.Boolean: return _Boolean ?? (_Boolean = new EqualBoolean());
                case TypeCode.SByte: return _SByte ?? (_SByte = new EqualSByte());
                case TypeCode.Byte: return _Byte ?? (_Byte = new EqualByte());
                case TypeCode.Char: return _Char ?? (_Char = new EqualChar());
                case TypeCode.Int16: return _Int16 ?? (_Int16 = new EqualInt16());
                case TypeCode.Int32: return _Int32 ?? (_Int32 = new EqualInt32());
                case TypeCode.Int64: return _Int64 ?? (_Int64 = new EqualInt64());

                case TypeCode.UInt16: return _UInt16 ?? (_UInt16 = new EqualInt16());
                case TypeCode.UInt32: return _UInt32 ?? (_UInt32 = new EqualInt32());
                case TypeCode.UInt64: return _UInt64 ?? (_UInt64 = new EqualInt64());

                case TypeCode.Single: return _Single ?? (_Single = new EqualSingle());
                case TypeCode.Double: return _Double ?? (_Double = new EqualDouble());

                case TypeCode.Object:
                    if (!typeInfo.IsValueType) {
                        return _Reference ?? (_Reference = new EqualReference());
                    }
                    // TODO: Nullable<T>
                    throw new NotImplementedException();

                default:
                    throw new NotImplementedException();
            }
        }

        public override string ToString() {
            return "Equal()";
        }
    }
}
