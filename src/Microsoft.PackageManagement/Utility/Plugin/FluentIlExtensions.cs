// 
//  Copyright (c) Microsoft Corporation. All rights reserved. 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//  

namespace Microsoft.PackageManagement.Internal.Utility.Plugin {
    using System;
    using System.Reflection;
    using System.Reflection.Emit;

    internal static class FluentIlExtensions {
        private static readonly OpCode[] _loadArgumentInstruction = {
            OpCodes.Ldarg_0, OpCodes.Ldarg_1, OpCodes.Ldarg_2, OpCodes.Ldarg_3, OpCodes.Ldarg_S
        };

        private static readonly OpCode[] _storeLocationInstruction = {
            OpCodes.Stloc_0, OpCodes.Stloc_1, OpCodes.Stloc_2, OpCodes.Stloc_3, OpCodes.Stloc
        };

        private static readonly OpCode[] _loadLocationInstruction = {
            OpCodes.Ldloc_0, OpCodes.Ldloc_1, OpCodes.Ldloc_2, OpCodes.Ldloc_3, OpCodes.Ldloc
        };

        public static void EmitLoadArgument(this ILGenerator il, int argument) {
            EmitIndexedInstruction(il, _loadArgumentInstruction, argument);
        }

        public static void EmitStoreLocation(this ILGenerator il, int argument) {
            EmitIndexedInstruction(il, _storeLocationInstruction, argument);
        }

        public static void EmitLoadLocation(this ILGenerator il, int argument) {
            EmitIndexedInstruction(il, _loadLocationInstruction, argument);
        }

        private static void EmitIndexedInstruction(this ILGenerator il, OpCode[] index, int argument) {
            if (argument < index.Length - 1) {
                il.Emit(index[argument]);
                return;
            }
            il.Emit(index[index.Length - 1], argument);
        }

        public static void Return(this ILGenerator il) {
            il.Emit(OpCodes.Ret);
        }

        public static void LoadLocation(this ILGenerator il, int location) {
            il.EmitLoadLocation(location);
        }

        public static void LoadLocation(this ILGenerator il, LocalBuilder local) {
            il.EmitLoadLocation(local.LocalIndex);
        }

        public static void StoreLocation(this ILGenerator il, int location) {
            il.EmitStoreLocation(location);
        }

        public static void StoreLocation(this ILGenerator il, LocalBuilder local) {
            il.EmitStoreLocation(local.LocalIndex);
        }

        public static void LoadArgument(this ILGenerator il, int location) {
            il.EmitLoadArgument(location);
        }

        public static void LoadThis(this ILGenerator il) {
            LoadArgument(il, 0);
        }

        public static void Call(this ILGenerator il, ConstructorInfo constructorInfo) {
            il.Emit(OpCodes.Call, constructorInfo);
        }

        public static void Call(this ILGenerator il, MethodInfo methodInfo) {
            il.EmitCall(OpCodes.Call, methodInfo, null);
        }

        public static void StoreField(this ILGenerator il, FieldInfo field) {
            il.Emit(OpCodes.Stfld, field);
        }

        public static void LoadField(this ILGenerator il, FieldInfo field) {
            il.Emit(OpCodes.Ldfld, field);
        }

        public static void LoadNull(this ILGenerator il) {
            il.Emit(OpCodes.Ldnull);
        }

        public static void BranchFalse(this ILGenerator il, Label label) {
            il.Emit(OpCodes.Brfalse_S, label);
        }

        public static void CallVirutal(this ILGenerator il, MethodInfo methodInfo) {
            il.Emit(OpCodes.Callvirt, methodInfo);
        }

        public static void LoadLocalAddress(this ILGenerator il, LocalBuilder local) {
            il.Emit(OpCodes.Ldloca_S, local);
        }

        public static void LoadInt32(this ILGenerator il, int value) {
            switch (value) {
                case 0:
                    il.Emit(OpCodes.Ldc_I4_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldc_I4_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldc_I4_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldc_I4_3);
                    break;
                case 4:
                    il.Emit(OpCodes.Ldc_I4_4);
                    break;
                case 5:
                    il.Emit(OpCodes.Ldc_I4_5);
                    break;
                case 6:
                    il.Emit(OpCodes.Ldc_I4_6);
                    break;
                case 7:
                    il.Emit(OpCodes.Ldc_I4_7);
                    break;
                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    break;
                case -1:
                    il.Emit(OpCodes.Ldc_I4_M1);
                    break;
                default:
                    il.Emit(OpCodes.Ldc_I4, value);
                    break;
            }
        }

        public static void InitObject(this ILGenerator il, Type t) {
            il.Emit(OpCodes.Initobj, t);
        }

        public static void LoadFloat(this ILGenerator il, float f) {
            il.Emit(OpCodes.Ldc_R4, f);
        }

        public static void LoadDouble(this ILGenerator il, double d) {
            il.Emit(OpCodes.Ldc_R8, d);
        }

        public static void ConvertToInt64(this ILGenerator il) {
            il.Emit(OpCodes.Conv_I8);
        }

        public static void Box(this ILGenerator il, Type type) {
            il.Emit(OpCodes.Box, type);
        }
    }
}