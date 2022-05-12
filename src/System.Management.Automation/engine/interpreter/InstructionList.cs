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
// Enables instruction counting and displaying stats at process exit.
// #define STATS

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Management.Automation.Interpreter
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
    [DebuggerTypeProxy(typeof(InstructionArray.DebugView))]
    internal readonly struct InstructionArray
    {
        internal readonly int MaxStackDepth;
        internal readonly int MaxContinuationDepth;
        internal readonly Instruction[] Instructions;
        internal readonly object[] Objects;
        internal readonly RuntimeLabel[] Labels;

        // list of (instruction index, cookie) sorted by instruction index:
        internal readonly List<KeyValuePair<int, object>> DebugCookies;

        internal InstructionArray(int maxStackDepth, int maxContinuationDepth, Instruction[] instructions,
            object[] objects, RuntimeLabel[] labels, List<KeyValuePair<int, object>> debugCookies)
        {
            MaxStackDepth = maxStackDepth;
            MaxContinuationDepth = maxContinuationDepth;
            Instructions = instructions;
            DebugCookies = debugCookies;
            Objects = objects;
            Labels = labels;
        }

        internal int Length
        {
            get { return Instructions.Length; }
        }

        #region Debug View

        internal sealed class DebugView
        {
            private readonly InstructionArray _array;

            public DebugView(InstructionArray array)
            {
                _array = array;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public InstructionList.DebugView.InstructionView[] A0
            {
                get
                {
                    return InstructionList.DebugView.GetInstructionViews(
                        _array.Instructions,
                        _array.Objects,
                        (index) => _array.Labels[index].Index,
                        _array.DebugCookies
                    );
                }
            }
        }

        #endregion
    }

    [DebuggerTypeProxy(typeof(InstructionList.DebugView))]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    internal sealed class InstructionList
    {
        private readonly List<Instruction> _instructions = new List<Instruction>();
        private List<object> _objects;

        private int _currentStackDepth;
        private int _maxStackDepth;
        private int _currentContinuationsDepth;
        private int _maxContinuationDepth;
        private int _runtimeLabelCount;
        private List<BranchLabel> _labels;

        // list of (instruction index, cookie) sorted by instruction index:
#pragma warning disable IDE0044 // Add readonly modifier
        private List<KeyValuePair<int, object>> _debugCookies = null;
#pragma warning restore IDE0044 // Variable is assigned when DEBUG is defined.

        #region Debug View

        internal sealed class DebugView
        {
            private readonly InstructionList _list;

            public DebugView(InstructionList list)
            {
                _list = list;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public InstructionView[] A0
            {
                get
                {
                    return GetInstructionViews(
                        _list._instructions,
                        _list._objects,
                        (index) => _list._labels[index].TargetIndex,
                        _list._debugCookies
                    );
                }
            }

            internal static InstructionView[] GetInstructionViews(IList<Instruction> instructions, IList<object> objects,
                Func<int, int> labelIndexer, IList<KeyValuePair<int, object>> debugCookies)
            {
                var result = new List<InstructionView>();
                int index = 0;
                int stackDepth = 0;
                int continuationsDepth = 0;

                var cookieEnumerator = (debugCookies ?? Array.Empty<KeyValuePair<int, object>>()).GetEnumerator();
                var hasCookie = cookieEnumerator.MoveNext();

                for (int i = 0; i < instructions.Count; i++)
                {
                    object cookie = null;
                    while (hasCookie && cookieEnumerator.Current.Key == i)
                    {
                        cookie = cookieEnumerator.Current.Value;
                        hasCookie = cookieEnumerator.MoveNext();
                    }

                    int stackDiff = instructions[i].StackBalance;
                    int contDiff = instructions[i].ContinuationsBalance;
                    string name = instructions[i].ToDebugString(i, cookie, labelIndexer, objects);
                    result.Add(new InstructionView(instructions[i], name, i, stackDepth, continuationsDepth));

                    index++;
                    stackDepth += stackDiff;
                    continuationsDepth += contDiff;
                }

                return result.ToArray();
            }

            [DebuggerDisplay("{GetValue(),nq}", Name = "{GetName(),nq}", Type = "{GetDisplayType(), nq}")]
            internal readonly struct InstructionView
            {
                private readonly int _index;
                private readonly int _stackDepth;
                private readonly int _continuationsDepth;
                private readonly string _name;
                private readonly Instruction _instruction;

                internal string GetName()
                {
                    return _index +
                        (_continuationsDepth == 0 ? string.Empty : " C(" + _continuationsDepth + ")") +
                        (_stackDepth == 0 ? string.Empty : " S(" + _stackDepth + ")");
                }

                internal string GetValue()
                {
                    return _name;
                }

                internal string GetDisplayType()
                {
                    return _instruction.ContinuationsBalance + "/" + _instruction.StackBalance;
                }

                public InstructionView(Instruction instruction, string name, int index, int stackDepth, int continuationsDepth)
                {
                    _instruction = instruction;
                    _name = name;
                    _index = index;
                    _stackDepth = stackDepth;
                    _continuationsDepth = continuationsDepth;
                }
            }
        }

        #endregion

        #region Core Emit Ops

        public void Emit(Instruction instruction)
        {
            _instructions.Add(instruction);
            UpdateStackDepth(instruction);
        }

        private void UpdateStackDepth(Instruction instruction)
        {
            Debug.Assert(instruction.ConsumedStack >= 0 && instruction.ProducedStack >= 0 &&
                instruction.ConsumedContinuations >= 0 && instruction.ProducedContinuations >= 0);

            _currentStackDepth -= instruction.ConsumedStack;
            Debug.Assert(_currentStackDepth >= 0);
            _currentStackDepth += instruction.ProducedStack;
            if (_currentStackDepth > _maxStackDepth)
            {
                _maxStackDepth = _currentStackDepth;
            }

            _currentContinuationsDepth -= instruction.ConsumedContinuations;
            Debug.Assert(_currentContinuationsDepth >= 0);
            _currentContinuationsDepth += instruction.ProducedContinuations;
            if (_currentContinuationsDepth > _maxContinuationDepth)
            {
                _maxContinuationDepth = _currentContinuationsDepth;
            }
        }

        /// <summary>
        /// Attaches a cookie to the last emitted instruction.
        /// </summary>
        [Conditional("DEBUG")]
        public void SetDebugCookie(object cookie)
        {
#if DEBUG
            if (_debugCookies == null)
            {
                _debugCookies = new List<KeyValuePair<int, object>>();
            }

            Debug.Assert(Count > 0);
            _debugCookies.Add(new KeyValuePair<int, object>(Count - 1, cookie));
#endif
        }

        public int Count
        {
            get { return _instructions.Count; }
        }

        public int CurrentStackDepth
        {
            get { return _currentStackDepth; }
        }

        public int CurrentContinuationsDepth
        {
            get { return _currentContinuationsDepth; }
        }

        public int MaxStackDepth
        {
            get { return _maxStackDepth; }
        }

        internal Instruction GetInstruction(int index)
        {
            return _instructions[index];
        }

#if STATS
        private static Dictionary<string, int> _executedInstructions = new Dictionary<string, int>();
        private static Dictionary<string, Dictionary<object, bool>> _instances = new Dictionary<string, Dictionary<object, bool>>();

        static InstructionList() {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler((_, __) => {
                PerfTrack.DumpHistogram(_executedInstructions);
                Console.WriteLine("-- Total executed: {0}", _executedInstructions.Values.Aggregate(0, static (sum, value) => sum + value));
                Console.WriteLine("-----");

                var referenced = new Dictionary<string, int>();
                int total = 0;
                foreach (var entry in _instances) {
                    referenced[entry.Key] = entry.Value.Count;
                    total += entry.Value.Count;
                }

                PerfTrack.DumpHistogram(referenced);
                Console.WriteLine("-- Total referenced: {0}", total);
                Console.WriteLine("-----");
            });
        }
#endif
        public InstructionArray ToArray()
        {
#if STATS
            lock (_executedInstructions) {
                _instructions.ForEach((instr) => {
                    int value = 0;
                    var name = instr.GetType().Name;
                    _executedInstructions.TryGetValue(name, out value);
                    _executedInstructions[name] = value + 1;

                    Dictionary<object, bool> dict;
                    if (!_instances.TryGetValue(name, out dict)) {
                        _instances[name] = dict = new Dictionary<object, bool>();
                    }

                    dict[instr] = true;
                });
            }
#endif
            return new InstructionArray(
                _maxStackDepth,
                _maxContinuationDepth,
                _instructions.ToArray(),
                _objects?.ToArray(),
                BuildRuntimeLabels(),
                _debugCookies
            );
        }

        #endregion

        #region Stack Operations

        private const int PushIntMinCachedValue = -100;
        private const int PushIntMaxCachedValue = 100;
        private const int CachedObjectCount = 256;

        private static Instruction s_null;
        private static Instruction s_true;
        private static Instruction s_false;
        private static Instruction[] s_ints;
        private static Instruction[] s_loadObjectCached;

        public void EmitLoad(object value)
        {
            EmitLoad(value, null);
        }

        public void EmitLoad(bool value)
        {
            if ((bool)value)
            {
                Emit(s_true ??= new LoadObjectInstruction(value));
            }
            else
            {
                Emit(s_false ??= new LoadObjectInstruction(value));
            }
        }

        public void EmitLoad(object value, Type type)
        {
            if (value == null)
            {
                Emit(s_null ??= new LoadObjectInstruction(null));
                return;
            }

            if (type == null || type.IsValueType)
            {
                if (value is bool)
                {
                    EmitLoad((bool)value);
                    return;
                }

                if (value is int)
                {
                    int i = (int)value;
                    if (i >= PushIntMinCachedValue && i <= PushIntMaxCachedValue)
                    {
                        if (s_ints == null)
                        {
                            s_ints = new Instruction[PushIntMaxCachedValue - PushIntMinCachedValue + 1];
                        }

                        i -= PushIntMinCachedValue;
                        Emit(s_ints[i] ?? (s_ints[i] = new LoadObjectInstruction(value)));
                        return;
                    }
                }
            }

            if (_objects == null)
            {
                _objects = new List<object>();
                if (s_loadObjectCached == null)
                {
                    s_loadObjectCached = new Instruction[CachedObjectCount];
                }
            }

            if (_objects.Count < s_loadObjectCached.Length)
            {
                uint index = (uint)_objects.Count;
                _objects.Add(value);
                Emit(s_loadObjectCached[index] ?? (s_loadObjectCached[index] = new LoadCachedObjectInstruction(index)));
            }
            else
            {
                Emit(new LoadObjectInstruction(value));
            }
        }

        public void EmitDup()
        {
            Emit(DupInstruction.Instance);
        }

        public void EmitPop()
        {
            Emit(PopInstruction.Instance);
        }

        #endregion

        #region Locals

        internal void SwitchToBoxed(int index, int instructionIndex)
        {
            var instruction = _instructions[instructionIndex] as IBoxableInstruction;

            if (instruction != null)
            {
                var newInstruction = instruction.BoxIfIndexMatches(index);
                if (newInstruction != null)
                {
                    _instructions[instructionIndex] = newInstruction;
                }
            }
        }

        private const int LocalInstrCacheSize = 64;

        private static Instruction[] s_loadLocal;
        private static Instruction[] s_loadLocalBoxed;
        private static Instruction[] s_loadLocalFromClosure;
        private static Instruction[] s_loadLocalFromClosureBoxed;
        private static Instruction[] s_assignLocal;
        private static Instruction[] s_storeLocal;
        private static Instruction[] s_assignLocalBoxed;
        private static Instruction[] s_storeLocalBoxed;
        private static Instruction[] s_assignLocalToClosure;
        private static Instruction[] s_initReference;
        private static Instruction[] s_initImmutableRefBox;
        private static Instruction[] s_parameterBox;
        private static Instruction[] s_parameter;

        public void EmitLoadLocal(int index)
        {
            if (s_loadLocal == null)
            {
                s_loadLocal = new Instruction[LocalInstrCacheSize];
            }

            if (index < s_loadLocal.Length)
            {
                Emit(s_loadLocal[index] ?? (s_loadLocal[index] = new LoadLocalInstruction(index)));
            }
            else
            {
                Emit(new LoadLocalInstruction(index));
            }
        }

        public void EmitLoadLocalBoxed(int index)
        {
            Emit(LoadLocalBoxed(index));
        }

        internal static Instruction LoadLocalBoxed(int index)
        {
            if (s_loadLocalBoxed == null)
            {
                s_loadLocalBoxed = new Instruction[LocalInstrCacheSize];
            }

            if (index < s_loadLocalBoxed.Length)
            {
                return s_loadLocalBoxed[index] ?? (s_loadLocalBoxed[index] = new LoadLocalBoxedInstruction(index));
            }
            else
            {
                return new LoadLocalBoxedInstruction(index);
            }
        }

        public void EmitLoadLocalFromClosure(int index)
        {
            if (s_loadLocalFromClosure == null)
            {
                s_loadLocalFromClosure = new Instruction[LocalInstrCacheSize];
            }

            if (index < s_loadLocalFromClosure.Length)
            {
                Emit(s_loadLocalFromClosure[index] ?? (s_loadLocalFromClosure[index] = new LoadLocalFromClosureInstruction(index)));
            }
            else
            {
                Emit(new LoadLocalFromClosureInstruction(index));
            }
        }

        public void EmitLoadLocalFromClosureBoxed(int index)
        {
            if (s_loadLocalFromClosureBoxed == null)
            {
                s_loadLocalFromClosureBoxed = new Instruction[LocalInstrCacheSize];
            }

            if (index < s_loadLocalFromClosureBoxed.Length)
            {
                Emit(s_loadLocalFromClosureBoxed[index] ?? (s_loadLocalFromClosureBoxed[index] = new LoadLocalFromClosureBoxedInstruction(index)));
            }
            else
            {
                Emit(new LoadLocalFromClosureBoxedInstruction(index));
            }
        }

        public void EmitAssignLocal(int index)
        {
            if (s_assignLocal == null)
            {
                s_assignLocal = new Instruction[LocalInstrCacheSize];
            }

            if (index < s_assignLocal.Length)
            {
                Emit(s_assignLocal[index] ?? (s_assignLocal[index] = new AssignLocalInstruction(index)));
            }
            else
            {
                Emit(new AssignLocalInstruction(index));
            }
        }

        public void EmitStoreLocal(int index)
        {
            if (s_storeLocal == null)
            {
                s_storeLocal = new Instruction[LocalInstrCacheSize];
            }

            if (index < s_storeLocal.Length)
            {
                Emit(s_storeLocal[index] ?? (s_storeLocal[index] = new StoreLocalInstruction(index)));
            }
            else
            {
                Emit(new StoreLocalInstruction(index));
            }
        }

        public void EmitAssignLocalBoxed(int index)
        {
            Emit(AssignLocalBoxed(index));
        }

        internal static Instruction AssignLocalBoxed(int index)
        {
            if (s_assignLocalBoxed == null)
            {
                s_assignLocalBoxed = new Instruction[LocalInstrCacheSize];
            }

            if (index < s_assignLocalBoxed.Length)
            {
                return s_assignLocalBoxed[index] ?? (s_assignLocalBoxed[index] = new AssignLocalBoxedInstruction(index));
            }
            else
            {
                return new AssignLocalBoxedInstruction(index);
            }
        }

        public void EmitStoreLocalBoxed(int index)
        {
            Emit(StoreLocalBoxed(index));
        }

        internal static Instruction StoreLocalBoxed(int index)
        {
            if (s_storeLocalBoxed == null)
            {
                s_storeLocalBoxed = new Instruction[LocalInstrCacheSize];
            }

            if (index < s_storeLocalBoxed.Length)
            {
                return s_storeLocalBoxed[index] ?? (s_storeLocalBoxed[index] = new StoreLocalBoxedInstruction(index));
            }
            else
            {
                return new StoreLocalBoxedInstruction(index);
            }
        }

        public void EmitAssignLocalToClosure(int index)
        {
            if (s_assignLocalToClosure == null)
            {
                s_assignLocalToClosure = new Instruction[LocalInstrCacheSize];
            }

            if (index < s_assignLocalToClosure.Length)
            {
                Emit(s_assignLocalToClosure[index] ?? (s_assignLocalToClosure[index] = new AssignLocalToClosureInstruction(index)));
            }
            else
            {
                Emit(new AssignLocalToClosureInstruction(index));
            }
        }

        public void EmitStoreLocalToClosure(int index)
        {
            EmitAssignLocalToClosure(index);
            EmitPop();
        }

        public void EmitInitializeLocal(int index, Type type)
        {
            object value = ScriptingRuntimeHelpers.GetPrimitiveDefaultValue(type);
            if (value != null)
            {
                Emit(new InitializeLocalInstruction.ImmutableValue(index, value));
            }
            else if (type.IsValueType)
            {
                Emit(new InitializeLocalInstruction.MutableValue(index, type));
            }
            else
            {
                Emit(InitReference(index));
            }
        }

        internal void EmitInitializeParameter(int index)
        {
            Emit(Parameter(index));
        }

        internal static Instruction Parameter(int index)
        {
            if (s_parameter == null)
            {
                s_parameter = new Instruction[LocalInstrCacheSize];
            }

            if (index < s_parameter.Length)
            {
                return s_parameter[index] ?? (s_parameter[index] = new InitializeLocalInstruction.Parameter(index));
            }

            return new InitializeLocalInstruction.Parameter(index);
        }

        internal static Instruction ParameterBox(int index)
        {
            if (s_parameterBox == null)
            {
                s_parameterBox = new Instruction[LocalInstrCacheSize];
            }

            if (index < s_parameterBox.Length)
            {
                return s_parameterBox[index] ?? (s_parameterBox[index] = new InitializeLocalInstruction.ParameterBox(index));
            }

            return new InitializeLocalInstruction.ParameterBox(index);
        }

        internal static Instruction InitReference(int index)
        {
            if (s_initReference == null)
            {
                s_initReference = new Instruction[LocalInstrCacheSize];
            }

            if (index < s_initReference.Length)
            {
                return s_initReference[index] ?? (s_initReference[index] = new InitializeLocalInstruction.Reference(index));
            }

            return new InitializeLocalInstruction.Reference(index);
        }

        internal static Instruction InitImmutableRefBox(int index)
        {
            if (s_initImmutableRefBox == null)
            {
                s_initImmutableRefBox = new Instruction[LocalInstrCacheSize];
            }

            if (index < s_initImmutableRefBox.Length)
            {
                return s_initImmutableRefBox[index] ?? (s_initImmutableRefBox[index] = new InitializeLocalInstruction.ImmutableBox(index, null));
            }

            return new InitializeLocalInstruction.ImmutableBox(index, null);
        }

        public void EmitNewRuntimeVariables(int count)
        {
            Emit(new RuntimeVariablesInstruction(count));
        }

        #endregion

        #region Array Operations

        public void EmitGetArrayItem(Type arrayType)
        {
            var elementType = arrayType.GetElementType();
            if (elementType.IsClass || elementType.IsInterface)
            {
                Emit(InstructionFactory<object>.Factory.GetArrayItem());
            }
            else
            {
                Emit(InstructionFactory.GetFactory(elementType).GetArrayItem());
            }
        }

        public void EmitSetArrayItem(Type arrayType)
        {
            var elementType = arrayType.GetElementType();
            if (elementType.IsClass || elementType.IsInterface)
            {
                Emit(InstructionFactory<object>.Factory.SetArrayItem());
            }
            else
            {
                Emit(InstructionFactory.GetFactory(elementType).SetArrayItem());
            }
        }

        public void EmitNewArray(Type elementType)
        {
            Emit(InstructionFactory.GetFactory(elementType).NewArray());
        }

        public void EmitNewArrayBounds(Type elementType, int rank)
        {
            Emit(new NewArrayBoundsInstruction(elementType, rank));
        }

        public void EmitNewArrayInit(Type elementType, int elementCount)
        {
            // To avoid lock contention in InstructionFactory.GetFactory, we special case the most common
            // types of arrays that the compiler creates.
            if (elementType == typeof(CommandParameterInternal))
            {
                Emit(InstructionFactory<CommandParameterInternal>.Factory.NewArrayInit(elementCount));
            }
            else if (elementType == typeof(CommandParameterInternal[]))
            {
                Emit(InstructionFactory<CommandParameterInternal[]>.Factory.NewArrayInit(elementCount));
            }
            else if (elementType == typeof(object))
            {
                Emit(InstructionFactory<object>.Factory.NewArrayInit(elementCount));
            }
            else if (elementType == typeof(string))
            {
                Emit(InstructionFactory<string>.Factory.NewArrayInit(elementCount));
            }
            else
            {
                Emit(InstructionFactory.GetFactory(elementType).NewArrayInit(elementCount));
            }
        }

        #endregion

        #region Arithmetic Operations

        public void EmitAdd(Type type, bool @checked)
        {
            if (@checked)
            {
                Emit(AddOvfInstruction.Create(type));
            }
            else
            {
                Emit(AddInstruction.Create(type));
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        public void EmitSub(Type type, bool @checked)
        {
            if (@checked)
            {
                Emit(SubOvfInstruction.Create(type));
            }
            else
            {
                Emit(SubInstruction.Create(type));
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        public void EmitMul(Type type, bool @checked)
        {
            if (@checked)
            {
                Emit(MulOvfInstruction.Create(type));
            }
            else
            {
                Emit(MulInstruction.Create(type));
            }
        }

        public void EmitDiv(Type type)
        {
            Emit(DivInstruction.Create(type));
        }

        #endregion

        #region Comparisons

        public void EmitEqual(Type type)
        {
            Emit(EqualInstruction.Create(type));
        }

        public void EmitNotEqual(Type type)
        {
            Emit(NotEqualInstruction.Create(type));
        }

        public void EmitLessThan(Type type)
        {
            Emit(LessThanInstruction.Create(type));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        public void EmitLessThanOrEqual(Type type)
        {
            throw new NotSupportedException();
        }

        public void EmitGreaterThan(Type type)
        {
            Emit(GreaterThanInstruction.Create(type));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        public void EmitGreaterThanOrEqual(Type type)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region Conversions

        public void EmitNumericConvertChecked(TypeCode from, TypeCode to)
        {
            Emit(new NumericConvertInstruction.Checked(from, to));
        }

        public void EmitNumericConvertUnchecked(TypeCode from, TypeCode to)
        {
            Emit(new NumericConvertInstruction.Unchecked(from, to));
        }

        #endregion

        #region Boolean Operators

        public void EmitNot()
        {
            Emit(NotInstruction.Instance);
        }

        #endregion

        #region Types

        public void EmitDefaultValue(Type type)
        {
            Emit(InstructionFactory.GetFactory(type).DefaultValue());
        }

        public void EmitNew(ConstructorInfo constructorInfo)
        {
            Emit(new NewInstruction(constructorInfo));
        }

        internal void EmitCreateDelegate(LightDelegateCreator creator)
        {
            Emit(new CreateDelegateInstruction(creator));
        }

        public void EmitTypeEquals()
        {
            Emit(TypeEqualsInstruction.Instance);
        }

        public void EmitTypeIs(Type type)
        {
            Emit(InstructionFactory.GetFactory(type).TypeIs());
        }

        public void EmitTypeAs(Type type)
        {
            Emit(InstructionFactory.GetFactory(type).TypeAs());
        }

        #endregion

        #region Fields and Methods

        private static readonly Dictionary<FieldInfo, Instruction> s_loadFields = new Dictionary<FieldInfo, Instruction>();

        public void EmitLoadField(FieldInfo field)
        {
            Emit(GetLoadField(field));
        }

        private static Instruction GetLoadField(FieldInfo field)
        {
            lock (s_loadFields)
            {
                Instruction instruction;
                if (!s_loadFields.TryGetValue(field, out instruction))
                {
                    if (field.IsStatic)
                    {
                        instruction = new LoadStaticFieldInstruction(field);
                    }
                    else
                    {
                        instruction = new LoadFieldInstruction(field);
                    }

                    s_loadFields.Add(field, instruction);
                }

                return instruction;
            }
        }

        public void EmitStoreField(FieldInfo field)
        {
            if (field.IsStatic)
            {
                Emit(new StoreStaticFieldInstruction(field));
            }
            else
            {
                Emit(new StoreFieldInstruction(field));
            }
        }

        public void EmitCall(MethodInfo method)
        {
            EmitCall(method, method.GetParameters());
        }

        public void EmitCall(MethodInfo method, ParameterInfo[] parameters)
        {
            Emit(CallInstruction.Create(method, parameters));
        }

        #endregion

        #region Dynamic

        public void EmitDynamic(Type type, CallSiteBinder binder)
        {
            Emit(CreateDynamicInstruction(type, binder));
        }

        #region Generated Dynamic InstructionList Factory

        // *** BEGIN GENERATED CODE ***
        // generated by function: gen_instructionlist_factory from: generate_dynamic_instructions.py

        public void EmitDynamic<T0, TRet>(CallSiteBinder binder)
        {
            Emit(DynamicInstruction<T0, TRet>.Factory(binder));
        }

        public void EmitDynamic<T0, T1, TRet>(CallSiteBinder binder)
        {
            Emit(DynamicInstruction<T0, T1, TRet>.Factory(binder));
        }

        public void EmitDynamic<T0, T1, T2, TRet>(CallSiteBinder binder)
        {
            Emit(DynamicInstruction<T0, T1, T2, TRet>.Factory(binder));
        }

        public void EmitDynamic<T0, T1, T2, T3, TRet>(CallSiteBinder binder)
        {
            Emit(DynamicInstruction<T0, T1, T2, T3, TRet>.Factory(binder));
        }

        public void EmitDynamic<T0, T1, T2, T3, T4, TRet>(CallSiteBinder binder)
        {
            Emit(DynamicInstruction<T0, T1, T2, T3, T4, TRet>.Factory(binder));
        }

        public void EmitDynamic<T0, T1, T2, T3, T4, T5, TRet>(CallSiteBinder binder)
        {
            Emit(DynamicInstruction<T0, T1, T2, T3, T4, T5, TRet>.Factory(binder));
        }

        public void EmitDynamic<T0, T1, T2, T3, T4, T5, T6, TRet>(CallSiteBinder binder)
        {
            Emit(DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, TRet>.Factory(binder));
        }

        public void EmitDynamic<T0, T1, T2, T3, T4, T5, T6, T7, TRet>(CallSiteBinder binder)
        {
            Emit(DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, T7, TRet>.Factory(binder));
        }

        public void EmitDynamic<T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet>(CallSiteBinder binder)
        {
            Emit(DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet>.Factory(binder));
        }

        public void EmitDynamic<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TRet>(CallSiteBinder binder)
        {
            Emit(DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TRet>.Factory(binder));
        }

        public void EmitDynamic<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TRet>(CallSiteBinder binder)
        {
            Emit(DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TRet>.Factory(binder));
        }

        public void EmitDynamic<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TRet>(CallSiteBinder binder)
        {
            Emit(DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TRet>.Factory(binder));
        }

        public void EmitDynamic<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TRet>(CallSiteBinder binder)
        {
            Emit(DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TRet>.Factory(binder));
        }

        public void EmitDynamic<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TRet>(CallSiteBinder binder)
        {
            Emit(DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TRet>.Factory(binder));
        }

        public void EmitDynamic<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TRet>(CallSiteBinder binder)
        {
            Emit(DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TRet>.Factory(binder));
        }

        // *** END GENERATED CODE ***

        #endregion

        private static readonly Dictionary<Type, Func<CallSiteBinder, Instruction>> s_factories =
            new Dictionary<Type, Func<CallSiteBinder, Instruction>>();

        internal static Instruction CreateDynamicInstruction(Type delegateType, CallSiteBinder binder)
        {
            Func<CallSiteBinder, Instruction> factory;
            lock (s_factories)
            {
                if (!s_factories.TryGetValue(delegateType, out factory))
                {
                    if (delegateType.GetMethod("Invoke").ReturnType == typeof(void))
                    {
                        // TODO: We should generally support void returning binders but the only
                        // ones that exist are delete index/member who's perf isn't that critical.
                        return new DynamicInstructionN(delegateType, CallSite.Create(delegateType, binder), true);
                    }

                    Type instructionType = DynamicInstructionN.GetDynamicInstructionType(delegateType);
                    if (instructionType == null)
                    {
                        return new DynamicInstructionN(delegateType, CallSite.Create(delegateType, binder));
                    }

                    factory = (Func<CallSiteBinder, Instruction>)instructionType
                        .GetMethod("Factory")
                        .CreateDelegate(typeof(Func<CallSiteBinder, Instruction>));

                    s_factories[delegateType] = factory;
                }
            }

            return factory(binder);
        }

        #endregion

        #region Control Flow

        private static readonly RuntimeLabel[] s_emptyRuntimeLabels = new RuntimeLabel[] { new RuntimeLabel(Interpreter.RethrowOnReturn, 0, 0) };

        private RuntimeLabel[] BuildRuntimeLabels()
        {
            if (_runtimeLabelCount == 0)
            {
                return s_emptyRuntimeLabels;
            }

            var result = new RuntimeLabel[_runtimeLabelCount + 1];
            foreach (BranchLabel label in _labels)
            {
                if (label.HasRuntimeLabel)
                {
                    result[label.LabelIndex] = label.ToRuntimeLabel();
                }
            }
            // "return and rethrow" label:
            result[result.Length - 1] = new RuntimeLabel(Interpreter.RethrowOnReturn, 0, 0);
            return result;
        }

        public BranchLabel MakeLabel()
        {
            if (_labels == null)
            {
                _labels = new List<BranchLabel>();
            }

            var label = new BranchLabel();
            _labels.Add(label);
            return label;
        }

        internal void FixupBranch(int branchIndex, int offset)
        {
            _instructions[branchIndex] = ((OffsetInstruction)_instructions[branchIndex]).Fixup(offset);
        }

        private int EnsureLabelIndex(BranchLabel label)
        {
            if (label.HasRuntimeLabel)
            {
                return label.LabelIndex;
            }

            label.LabelIndex = _runtimeLabelCount;
            _runtimeLabelCount++;
            return label.LabelIndex;
        }

        public int MarkRuntimeLabel()
        {
            BranchLabel handlerLabel = MakeLabel();
            MarkLabel(handlerLabel);
            return EnsureLabelIndex(handlerLabel);
        }

        public void MarkLabel(BranchLabel label)
        {
            label.Mark(this);
        }

        public void EmitGoto(BranchLabel label, bool hasResult, bool hasValue)
        {
            Emit(GotoInstruction.Create(EnsureLabelIndex(label), hasResult, hasValue));
        }

        private void EmitBranch(OffsetInstruction instruction, BranchLabel label)
        {
            Emit(instruction);
            label.AddBranch(this, Count - 1);
        }

        public void EmitBranch(BranchLabel label)
        {
            EmitBranch(new BranchInstruction(), label);
        }

        public void EmitBranch(BranchLabel label, bool hasResult, bool hasValue)
        {
            EmitBranch(new BranchInstruction(hasResult, hasValue), label);
        }

        public void EmitCoalescingBranch(BranchLabel leftNotNull)
        {
            EmitBranch(new CoalescingBranchInstruction(), leftNotNull);
        }

        public void EmitBranchTrue(BranchLabel elseLabel)
        {
            EmitBranch(new BranchTrueInstruction(), elseLabel);
        }

        public void EmitBranchFalse(BranchLabel elseLabel)
        {
            EmitBranch(new BranchFalseInstruction(), elseLabel);
        }

        public void EmitThrow()
        {
            Emit(ThrowInstruction.Throw);
        }

        public void EmitThrowVoid()
        {
            Emit(ThrowInstruction.VoidThrow);
        }

        public void EmitRethrow()
        {
            Emit(ThrowInstruction.Rethrow);
        }

        public void EmitRethrowVoid()
        {
            Emit(ThrowInstruction.VoidRethrow);
        }

        public void EmitEnterTryFinally(BranchLabel finallyStartLabel)
        {
            Emit(EnterTryCatchFinallyInstruction.CreateTryFinally(EnsureLabelIndex(finallyStartLabel)));
        }

        public void EmitEnterTryCatch()
        {
            Emit(EnterTryCatchFinallyInstruction.CreateTryCatch());
        }

        public void EmitEnterFinally(BranchLabel finallyStartLabel)
        {
            Emit(EnterFinallyInstruction.Create(EnsureLabelIndex(finallyStartLabel)));
        }

        public void EmitLeaveFinally()
        {
            Emit(LeaveFinallyInstruction.Instance);
        }

        public void EmitLeaveFault(bool hasValue)
        {
            Emit(hasValue ? LeaveFaultInstruction.NonVoid : LeaveFaultInstruction.Void);
        }

        public void EmitEnterExceptionHandlerNonVoid()
        {
            Emit(EnterExceptionHandlerInstruction.NonVoid);
        }

        public void EmitEnterExceptionHandlerVoid()
        {
            Emit(EnterExceptionHandlerInstruction.Void);
        }

        public void EmitLeaveExceptionHandler(bool hasValue, BranchLabel tryExpressionEndLabel)
        {
            Emit(LeaveExceptionHandlerInstruction.Create(EnsureLabelIndex(tryExpressionEndLabel), hasValue));
        }

        public void EmitSwitch(Dictionary<int, int> cases)
        {
            Emit(new SwitchInstruction(cases));
        }

        #endregion
    }
}
