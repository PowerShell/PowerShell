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

#if !CLR2
using BigInt = System.Numerics.BigInteger;
#endif

#if CORECLR
// Used for 'GetField' which is not available under 'Type' in CoreClR but provided as an extenstion method in 'System.Reflection.TypeExtensions'
using System.Reflection;
#endif

using System.Collections.Generic;

//using Microsoft.Scripting.Math;

namespace System.Management.Automation.Interpreter
{
    internal abstract class InstructionFactory
    {
        // TODO: weak table for types in a collectible assembly?
        private static Dictionary<Type, InstructionFactory> s_factories;

        internal static InstructionFactory GetFactory(Type type)
        {
            if (s_factories == null)
            {
                s_factories = new Dictionary<Type, InstructionFactory>() {
                    { typeof(object), InstructionFactory<object>.Factory },
                    { typeof(bool), InstructionFactory<bool>.Factory },
                    { typeof(byte), InstructionFactory<byte>.Factory },
                    { typeof(sbyte), InstructionFactory<sbyte>.Factory },
                    { typeof(short), InstructionFactory<short>.Factory },
                    { typeof(ushort), InstructionFactory<ushort>.Factory },
                    { typeof(int), InstructionFactory<int>.Factory },
                    { typeof(uint), InstructionFactory<uint>.Factory },
                    { typeof(long), InstructionFactory<long>.Factory },
                    { typeof(ulong), InstructionFactory<ulong>.Factory },
                    { typeof(float), InstructionFactory<float>.Factory },
                    { typeof(double), InstructionFactory<double>.Factory },
                    { typeof(char), InstructionFactory<char>.Factory },
                    { typeof(string), InstructionFactory<string>.Factory },
#if !CLR2
                    { typeof(BigInt), InstructionFactory<BigInt>.Factory },
#endif
                    //{ typeof(BigInteger), InstructionFactory<BigInteger>.Factory }  
                };
            }

            lock (s_factories)
            {
                InstructionFactory factory;
                if (!s_factories.TryGetValue(type, out factory))
                {
                    factory = (InstructionFactory)typeof(InstructionFactory<>).MakeGenericType(type).GetField("Factory").GetValue(null);
                    s_factories[type] = factory;
                }
                return factory;
            }
        }

        internal protected abstract Instruction GetArrayItem();
        internal protected abstract Instruction SetArrayItem();
        internal protected abstract Instruction TypeIs();
        internal protected abstract Instruction TypeAs();
        internal protected abstract Instruction DefaultValue();
        internal protected abstract Instruction NewArray();
        internal protected abstract Instruction NewArrayInit(int elementCount);
    }

    internal sealed class InstructionFactory<T> : InstructionFactory
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly InstructionFactory Factory = new InstructionFactory<T>();

        private Instruction _getArrayItem;
        private Instruction _setArrayItem;
        private Instruction _typeIs;
        private Instruction _defaultValue;
        private Instruction _newArray;
        private Instruction _typeAs;

        private InstructionFactory() { }

        internal protected override Instruction GetArrayItem()
        {
            return _getArrayItem ?? (_getArrayItem = new GetArrayItemInstruction<T>());
        }

        internal protected override Instruction SetArrayItem()
        {
            return _setArrayItem ?? (_setArrayItem = new SetArrayItemInstruction<T>());
        }

        internal protected override Instruction TypeIs()
        {
            return _typeIs ?? (_typeIs = new TypeIsInstruction<T>());
        }

        internal protected override Instruction TypeAs()
        {
            return _typeAs ?? (_typeAs = new TypeAsInstruction<T>());
        }

        internal protected override Instruction DefaultValue()
        {
            return _defaultValue ?? (_defaultValue = new DefaultValueInstruction<T>());
        }

        internal protected override Instruction NewArray()
        {
            return _newArray ?? (_newArray = new NewArrayInstruction<T>());
        }

        internal protected override Instruction NewArrayInit(int elementCount)
        {
            return new NewArrayInitInstruction<T>(elementCount);
        }
    }
}
