// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Management.Automation.Language;
using System.Reflection;

namespace System.Management.Automation
{
    using DynamicNull = System.Management.Automation.LanguagePrimitives.Null;

    internal abstract class MutableTuple
    {
        private const int MaxSize = 128;

        private static readonly Dictionary<Type, int> s_sizeDict = new Dictionary<Type, int>();

        private int _size;
        protected BitArray _valuesSet;
        private Dictionary<string, int> _nameToIndexMap;

        internal bool IsValueSet(int index)
        {
            if (_size < MutableTuple.MaxSize)
            {
                // fast path
                return _valuesSet[index];
            }

            // slow path
            MutableTuple nestedTuple = this;
            var accessPath = GetAccessPath(_size, index).ToArray();
            int length = accessPath.Length;
            for (int i = 0; i < length - 1; ++i)
            {
                nestedTuple = (MutableTuple)nestedTuple.GetValueImpl(accessPath[i]);
            }

            return nestedTuple._valuesSet[accessPath[length - 1]];
        }

        internal void SetAutomaticVariable(AutomaticVariable auto, object value, ExecutionContext context)
        {
            if (context._debuggingMode > 0)
            {
                context.Debugger.CheckVariableWrite(SpecialVariables.AutomaticVariables[(int)auto]);
            }

            SetValue((int)auto, value);
        }

        internal object GetAutomaticVariable(AutomaticVariable auto)
        {
            return GetValue((int)auto);
        }

        internal void SetPreferenceVariable(PreferenceVariable pref, object value)
        {
            SetValue((int)pref, value);
        }

        internal bool TryGetLocalVariable(string name, bool fromNewOrSet, out PSVariable result)
        {
            int index;
            name = VariableAnalysis.GetUnaliasedVariableName(name);
            if (_nameToIndexMap.TryGetValue(name, out index) && (fromNewOrSet || IsValueSet(index)))
            {
                result = new LocalVariable(name, this, index);
                return true;
            }

            result = null;
            return false;
        }

        internal bool TrySetParameter(string name, object value)
        {
            int index;
            name = VariableAnalysis.GetUnaliasedVariableName(name);
            if (_nameToIndexMap.TryGetValue(name, out index))
            {
                SetValue(index, value);
                return true;
            }

            return false;
        }

        internal PSVariable TrySetVariable(string name, object value)
        {
            int index;
            name = VariableAnalysis.GetUnaliasedVariableName(name);
            if (_nameToIndexMap.TryGetValue(name, out index))
            {
                SetValue(index, value);
                return new LocalVariable(name, this, index);
            }

            return null;
        }

        internal void GetVariableTable(Dictionary<string, PSVariable> result, bool includePrivate)
        {
            // We could cache this array if it was a perf problem.
            var orderedNames = (from keyValuePairs in _nameToIndexMap
                                orderby keyValuePairs.Value
                                select keyValuePairs.Key).ToArray();

            for (int i = 0; i < orderedNames.Length; ++i)
            {
                var name = orderedNames[i];
                if (IsValueSet(i) && !result.ContainsKey(name))
                {
                    result.Add(name, new LocalVariable(name, this, i));
                    if (SpecialVariables.IsUnderbar(name))
                    {
                        result.Add(SpecialVariables.PSItem, new LocalVariable(SpecialVariables.PSItem, this, i));
                    }
                }
            }
        }

        public object GetValue(int index)
        {
            return GetNestedValue(_size, index);
        }

        public void SetValue(int index, object value)
        {
            SetNestedValue(_size, index, value);
        }

        protected abstract object GetValueImpl(int index);

        protected abstract void SetValueImpl(int index, object value);

        /// <summary>
        /// Sets the value at the given index for a tuple of the given size.  This set supports
        /// walking through nested tuples to get the correct final index.
        /// </summary>
        private void SetNestedValue(int size, int index, object value)
        {
            if (size < MutableTuple.MaxSize)
            {
                // fast path
                SetValueImpl(index, value);
            }
            else
            {
                // slow path
                MutableTuple res = this;
                int lastAccess = -1;
                foreach (int i in GetAccessPath(size, index))
                {
                    if (lastAccess != -1)
                    {
                        res = (MutableTuple)res.GetValueImpl(lastAccess);
                    }

                    lastAccess = i;
                }

                res.SetValueImpl(lastAccess, value);
            }
        }

        /// <summary>
        /// Gets the value at the given index for a tuple of the given size.  This get
        /// supports walking through nested tuples to get the correct final index.
        /// </summary>
        private object GetNestedValue(int size, int index)
        {
            if (size < MutableTuple.MaxSize)
            {
                // fast path
                return GetValueImpl(index);
            }
            else
            {
                // slow path
                object res = this;
                foreach (int i in GetAccessPath(size, index))
                {
                    res = ((MutableTuple)res).GetValueImpl(i);
                }

                return res;
            }
        }

        /// <summary>
        /// Gets the unbound generic Tuple type which has at lease size slots or null if a large enough tuple is not available.
        /// </summary>
        private static Type GetTupleType(int size)
        {
            #region Generated Tuple Get From Size

            // *** BEGIN GENERATED CODE ***
            // generated by function: gen_get_size from: generate_tuples.py

            if (size <= MutableTuple.MaxSize)
            {
                if (size <= 1)
                {
                    return typeof(MutableTuple<>);
                }
                else if (size <= 2)
                {
                    return typeof(MutableTuple<,>);
                }
                else if (size <= 4)
                {
                    return typeof(MutableTuple<,,,>);
                }
                else if (size <= 8)
                {
                    return typeof(MutableTuple<,,,,,,,>);
                }
                else if (size <= 16)
                {
                    return typeof(MutableTuple<,,,,,,,,,,,,,,,>);
                }
                else if (size <= 32)
                {
                    return typeof(MutableTuple<,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,>);
                }
                else if (size <= 64)
                {
                    return typeof(MutableTuple<,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,>);
                }
                else
                {
                    return typeof(MutableTuple<,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,>);
                }
            }

            // *** END GENERATED CODE ***

            #endregion

            return null;
        }

        /// <summary>
        /// Creates a generic tuple with the specified types.
        ///
        /// If the number of slots fits within the maximum tuple size then we simply
        /// create a single tuple.  If it's greater then we create nested tuples
        /// (e.g. a Tuple`2 which contains a Tuple`128 and a Tuple`8 if we had a size of 136).
        /// </summary>
        public static Type MakeTupleType(params Type[] types)
        {
            // ContractUtils.RequiresNotNull(types, "types");

            return MakeTupleType(types, 0, types.Length);
        }

        /// <summary>
        /// Gets the number of usable slots in the provided Tuple type including slots available in nested tuples.
        /// </summary>
        public static int GetSize(Type tupleType)
        {
            // ContractUtils.RequiresNotNull(tupleType, "tupleType");

            int count = 0;
            lock (s_sizeDict) if (s_sizeDict.TryGetValue(tupleType, out count))
            {
                return count;
            }

            Stack<Type> types = new Stack<Type>(tupleType.GetGenericArguments());

            while (types.Count != 0)
            {
                Type t = types.Pop();

                if (typeof(MutableTuple).IsAssignableFrom(t))
                {
                    foreach (Type subtype in t.GetGenericArguments())
                    {
                        types.Push(subtype);
                    }

                    continue;
                }

                if (t == typeof(DynamicNull))
                {
                    continue;
                }

                count++;
            }

            lock (s_sizeDict) s_sizeDict[tupleType] = count;
            return count;
        }

        private static readonly ConcurrentDictionary<Type, Func<MutableTuple>> s_tupleCreators =
            new ConcurrentDictionary<Type, Func<MutableTuple>>(concurrencyLevel: 3, capacity: 100);

        public static Func<MutableTuple> TupleCreator(Type type)
        {
            return s_tupleCreators.GetOrAdd(type,
                t =>
                {
                    var newExpr = Expression.New(t);
                    return Expression.Lambda<Func<MutableTuple>>(newExpr.Cast(typeof(MutableTuple))).Compile();
                });
        }

        /// <summary>
        /// Creates a new instance of tupleType with the specified args.  If the tuple is a nested
        /// tuple the values are added in their nested forms.
        /// </summary>
        public static MutableTuple MakeTuple(Type tupleType, Dictionary<string, int> nameToIndexMap, Func<MutableTuple> creator = null)
        {
            // ContractUtils.RequiresNotNull(tupleType, "tupleType");
            // ContractUtils.RequiresNotNull(args, "args");

            int size = GetSize(tupleType);
            var bitArray = new BitArray(size);
            MutableTuple res = MakeTuple(creator, tupleType, size, bitArray);
            res._nameToIndexMap = nameToIndexMap;
            return res;
        }

        /// <summary>
        /// Gets the values from a tuple including unpacking nested values.
        /// </summary>
        public static object[] GetTupleValues(MutableTuple tuple)
        {
            // ContractUtils.RequiresNotNull(tuple, "tuple");

            List<object> res = new List<object>();

            GetTupleValues(tuple, res);

            return res.ToArray();
        }

        /// <summary>
        /// Gets the series of properties that needs to be accessed to access a logical item in a potentially nested tuple.
        /// </summary>
        public static IEnumerable<PropertyInfo> GetAccessPath(Type tupleType, int index)
        {
            return GetAccessProperties(tupleType, GetSize(tupleType), index);
        }

        /// <summary>
        /// Gets the series of properties that needs to be accessed to access a logical item in a potentially nested tuple.
        /// </summary>
        internal static IEnumerable<PropertyInfo> GetAccessProperties(Type tupleType, int size, int index)
        {
            // ContractUtils.RequiresNotNull(tupleType, "tupleType");

            if (index < 0 || index >= size) throw new ArgumentException("index");

            foreach (int curIndex in GetAccessPath(size, index))
            {
                PropertyInfo pi = tupleType.GetProperty("Item" + string.Create(CultureInfo.InvariantCulture, $"{curIndex:D3}"));
                Diagnostics.Assert(pi != null, "reflection should always find Item");
                yield return pi;
                tupleType = pi.PropertyType;
            }
        }

        internal static IEnumerable<int> GetAccessPath(int size, int index)
        {
            // We get the final index by breaking the index into groups of bits.  The more significant bits
            // represent the indexes into the outermost tuples and the least significant bits index into the
            // inner most tuples.  The mask is initialized to mask the upper bits and adjust is initialized
            // and adjust is the value we need to divide by to get the index in the least significant bits.
            // As we go through we shift the mask and adjust down each loop to pull out the inner slot.  Logically
            // everything in here is shifting bits (not multiplying or dividing) because NewTuple.MaxSize is a
            // power of 2.
            int depth = 0;
            int mask = MutableTuple.MaxSize - 1;
            int adjust = 1;
            int count = size;
            while (count > MutableTuple.MaxSize)
            {
                depth++;
                count /= MutableTuple.MaxSize;
                mask *= MutableTuple.MaxSize;
                adjust *= MutableTuple.MaxSize;
            }

            while (depth-- >= 0)
            {
                Diagnostics.Assert(mask != 0, "mask should never be 0.");

                int curIndex = (index & mask) / adjust;

                yield return curIndex;

                mask /= MutableTuple.MaxSize;
                adjust /= MutableTuple.MaxSize;
            }
        }

        private static void GetTupleValues(MutableTuple tuple, List<object> args)
        {
            Type[] types = tuple.GetType().GetGenericArguments();
            for (int i = 0; i < types.Length; i++)
            {
                if (typeof(MutableTuple).IsAssignableFrom(types[i]))
                {
                    GetTupleValues((MutableTuple)tuple.GetValue(i), args);
                }
                else if (types[i] != typeof(DynamicNull))
                {
                    args.Add(tuple.GetValue(i));
                }
            }
        }

        private static MutableTuple MakeTuple(Func<MutableTuple> creator, Type tupleType, int size, BitArray bitArray)
        {
            MutableTuple res = (creator ?? MutableTuple.TupleCreator(tupleType))();
            res._size = size;
            res._valuesSet = bitArray;
            if (size > MutableTuple.MaxSize)
            {
                while (size > MutableTuple.MaxSize)
                {
                    size = (size + MutableTuple.MaxSize - 1) / MutableTuple.MaxSize;
                }

                for (int i = 0; i < size; i++)
                {
                    PropertyInfo pi = tupleType.GetProperty("Item" + string.Create(CultureInfo.InvariantCulture, $"{i:D3}"));
                    res.SetValueImpl(i, MakeTuple(pi.PropertyType, null, null));
                }
            }

            return res;
        }

        private static Type MakeTupleType(Type[] types, int start, int end)
        {
            int size = end - start;

            Type type = GetTupleType(size);
            if (type != null)
            {
                Type[] typeArr = new Type[type.GetGenericArguments().Length];
                int index = 0;
                for (int i = start; i < end; i++)
                {
                    typeArr[index++] = types[i];
                }
                while (index < typeArr.Length)
                {
                    typeArr[index++] = typeof(DynamicNull);
                }

                return type.MakeGenericType(typeArr);
            }

            int multiplier = 1;
            while (size > MutableTuple.MaxSize)
            {
                size = (size + MutableTuple.MaxSize - 1) / MutableTuple.MaxSize;
                multiplier *= MutableTuple.MaxSize;
            }

            type = MutableTuple.GetTupleType(size);
            Diagnostics.Assert(type != null, "type cannot be null");
            Type[] nestedTypes = new Type[type.GetGenericArguments().Length];
            for (int i = 0; i < size; i++)
            {
                int newStart = start + (i * multiplier);
                int newEnd = System.Math.Min(end, start + ((i + 1) * multiplier));
                nestedTypes[i] = MakeTupleType(types, newStart, newEnd);
            }

            for (int i = size; i < nestedTypes.Length; i++)
            {
                nestedTypes[i] = typeof(DynamicNull);
            }

            return type.MakeGenericType(nestedTypes);
        }

        public abstract int Capacity
        {
            get;
        }

        /// <summary>
        /// Provides an expression for creating a tuple with the specified values.
        /// </summary>
        public static Expression Create(params Expression[] values)
        {
            return CreateNew(MakeTupleType(values.Select(static x => x.Type).ToArray()), 0, values.Length, values);
        }

        private static int PowerOfTwoRound(int value)
        {
            int res = 1;
            while (value > res)
            {
                res <<= 1;
            }

            return res;
        }

        internal static Expression CreateNew(Type tupleType, int start, int end, Expression[] values)
        {
            int size = end - start;
            Diagnostics.Assert(tupleType != null, "tupleType cannot be null");
            Diagnostics.Assert(tupleType.IsSubclassOf(typeof(MutableTuple)), "tupleType must be derived from MutableTuple");

            Expression[] newValues;
            if (size > MutableTuple.MaxSize)
            {
                int multiplier = 1;
                while (size > MutableTuple.MaxSize)
                {
                    size = (size + MutableTuple.MaxSize - 1) / MutableTuple.MaxSize;
                    multiplier *= MutableTuple.MaxSize;
                }

                newValues = new Expression[PowerOfTwoRound(size)];
                for (int i = 0; i < size; i++)
                {
                    int newStart = start + (i * multiplier);
                    int newEnd = System.Math.Min(end, start + ((i + 1) * multiplier));

                    PropertyInfo pi = tupleType.GetProperty("Item" + string.Create(CultureInfo.InvariantCulture, $"{i:D3}"));

                    newValues[i] = CreateNew(pi.PropertyType, newStart, newEnd, values);
                }

                for (int i = size; i < newValues.Length; i++)
                {
                    newValues[i] = Expression.Constant(null, typeof(DynamicNull));
                }
            }
            else
            {
                newValues = new Expression[PowerOfTwoRound(size)];
                for (int i = 0; i < size; i++)
                {
                    newValues[i] = values[i + start];
                }

                for (int i = size; i < newValues.Length; i++)
                {
                    newValues[i] = Expression.Constant(null, typeof(DynamicNull));
                }
            }

            return Expression.New(tupleType.GetConstructor(newValues.Select(static x => x.Type).ToArray()), newValues);
        }
    }

    #region Generated Tuples

    // *** BEGIN GENERATED CODE ***
    // generated by function: gen_tuples from: generate_tuples.py

    [GeneratedCode("DLR", "2.0")]
    internal class MutableTuple<T0> : MutableTuple
    {
        public MutableTuple() { }

        public MutableTuple(T0 item0)
          : base()
        {
            _item0 = item0;
        }

        private T0 _item0;

        public T0 Item000
        {
            get { return _item0; }

            set { _item0 = value; _valuesSet[0] = true; }
        }

        protected override object GetValueImpl(int index)
        {
            switch (index)
            {
                case 0: return Item000;
                default: throw new ArgumentOutOfRangeException("index");
            }
        }

        protected override void SetValueImpl(int index, object value)
        {
            switch (index)
            {
                case 0: Item000 = LanguagePrimitives.ConvertTo<T0>(value); break;
                default: throw new ArgumentOutOfRangeException("index");
            }
        }

        public override int Capacity
        {
            get
            {
                return 1;
            }
        }
    }

    [GeneratedCode("DLR", "2.0")]
    internal class MutableTuple<T0, T1> : MutableTuple<T0>
    {
        public MutableTuple() { }

        public MutableTuple(T0 item0, T1 item1)
          : base(item0)
        {
            _item1 = item1;
        }

        private T1 _item1;

        public T1 Item001
        {
            get { return _item1; }

            set { _item1 = value; _valuesSet[1] = true; }
        }

        protected override object GetValueImpl(int index)
        {
            switch (index)
            {
                case 0: return Item000;
                case 1: return Item001;
                default: throw new ArgumentOutOfRangeException("index");
            }
        }

        protected override void SetValueImpl(int index, object value)
        {
            switch (index)
            {
                case 0: Item000 = LanguagePrimitives.ConvertTo<T0>(value); break;
                case 1: Item001 = LanguagePrimitives.ConvertTo<T1>(value); break;
                default: throw new ArgumentOutOfRangeException("index");
            }
        }

        public override int Capacity
        {
            get
            {
                return 2;
            }
        }
    }

    [GeneratedCode("DLR", "2.0")]
    internal class MutableTuple<T0, T1, T2, T3> : MutableTuple<T0, T1>
    {
        public MutableTuple() { }

        public MutableTuple(T0 item0, T1 item1, T2 item2, T3 item3)
          : base(item0, item1)
        {
            _item2 = item2;
            _item3 = item3;
        }

        private T2 _item2;
        private T3 _item3;

        public T2 Item002
        {
            get { return _item2; }

            set { _item2 = value; _valuesSet[2] = true; }
        }

        public T3 Item003
        {
            get { return _item3; }

            set { _item3 = value; _valuesSet[3] = true; }
        }

        protected override object GetValueImpl(int index)
        {
            switch (index)
            {
                case 0: return Item000;
                case 1: return Item001;
                case 2: return Item002;
                case 3: return Item003;
                default: throw new ArgumentOutOfRangeException("index");
            }
        }

        protected override void SetValueImpl(int index, object value)
        {
            switch (index)
            {
                case 0: Item000 = LanguagePrimitives.ConvertTo<T0>(value); break;
                case 1: Item001 = LanguagePrimitives.ConvertTo<T1>(value); break;
                case 2: Item002 = LanguagePrimitives.ConvertTo<T2>(value); break;
                case 3: Item003 = LanguagePrimitives.ConvertTo<T3>(value); break;
                default: throw new ArgumentOutOfRangeException("index");
            }
        }

        public override int Capacity
        {
            get
            {
                return 4;
            }
        }
    }

    [GeneratedCode("DLR", "2.0")]
    internal class MutableTuple<T0, T1, T2, T3, T4, T5, T6, T7> : MutableTuple<T0, T1, T2, T3>
    {
        public MutableTuple() { }

        public MutableTuple(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
          : base(item0, item1, item2, item3)
        {
            _item4 = item4;
            _item5 = item5;
            _item6 = item6;
            _item7 = item7;
        }

        private T4 _item4;
        private T5 _item5;
        private T6 _item6;
        private T7 _item7;

        public T4 Item004
        {
            get { return _item4; }

            set { _item4 = value; _valuesSet[4] = true; }
        }

        public T5 Item005
        {
            get { return _item5; }

            set { _item5 = value; _valuesSet[5] = true; }
        }

        public T6 Item006
        {
            get { return _item6; }

            set { _item6 = value; _valuesSet[6] = true; }
        }

        public T7 Item007
        {
            get { return _item7; }

            set { _item7 = value; _valuesSet[7] = true; }
        }

        protected override object GetValueImpl(int index)
        {
            switch (index)
            {
                case 0: return Item000;
                case 1: return Item001;
                case 2: return Item002;
                case 3: return Item003;
                case 4: return Item004;
                case 5: return Item005;
                case 6: return Item006;
                case 7: return Item007;
                default: throw new ArgumentOutOfRangeException("index");
            }
        }

        protected override void SetValueImpl(int index, object value)
        {
            switch (index)
            {
                case 0: Item000 = LanguagePrimitives.ConvertTo<T0>(value); break;
                case 1: Item001 = LanguagePrimitives.ConvertTo<T1>(value); break;
                case 2: Item002 = LanguagePrimitives.ConvertTo<T2>(value); break;
                case 3: Item003 = LanguagePrimitives.ConvertTo<T3>(value); break;
                case 4: Item004 = LanguagePrimitives.ConvertTo<T4>(value); break;
                case 5: Item005 = LanguagePrimitives.ConvertTo<T5>(value); break;
                case 6: Item006 = LanguagePrimitives.ConvertTo<T6>(value); break;
                case 7: Item007 = LanguagePrimitives.ConvertTo<T7>(value); break;
                default: throw new ArgumentOutOfRangeException("index");
            }
        }

        public override int Capacity
        {
            get
            {
                return 8;
            }
        }
    }

    [GeneratedCode("DLR", "2.0")]
    internal class MutableTuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : MutableTuple<T0, T1, T2, T3, T4, T5, T6, T7>
    {
        public MutableTuple() { }

        public MutableTuple(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13, T14 item14, T15 item15)
          : base(item0, item1, item2, item3, item4, item5, item6, item7)
        {
            _item8 = item8;
            _item9 = item9;
            _item10 = item10;
            _item11 = item11;
            _item12 = item12;
            _item13 = item13;
            _item14 = item14;
            _item15 = item15;
        }

        private T8 _item8;
        private T9 _item9;
        private T10 _item10;
        private T11 _item11;
        private T12 _item12;
        private T13 _item13;
        private T14 _item14;
        private T15 _item15;

        public T8 Item008
        {
            get { return _item8; }

            set { _item8 = value; _valuesSet[8] = true; }
        }

        public T9 Item009
        {
            get { return _item9; }

            set { _item9 = value; _valuesSet[9] = true; }
        }

        public T10 Item010
        {
            get { return _item10; }

            set { _item10 = value; _valuesSet[10] = true; }
        }

        public T11 Item011
        {
            get { return _item11; }

            set { _item11 = value; _valuesSet[11] = true; }
        }

        public T12 Item012
        {
            get { return _item12; }

            set { _item12 = value; _valuesSet[12] = true; }
        }

        public T13 Item013
        {
            get { return _item13; }

            set { _item13 = value; _valuesSet[13] = true; }
        }

        public T14 Item014
        {
            get { return _item14; }

            set { _item14 = value; _valuesSet[14] = true; }
        }

        public T15 Item015
        {
            get { return _item15; }

            set { _item15 = value; _valuesSet[15] = true; }
        }

        protected override object GetValueImpl(int index)
        {
            switch (index)
            {
                case 0: return Item000;
                case 1: return Item001;
                case 2: return Item002;
                case 3: return Item003;
                case 4: return Item004;
                case 5: return Item005;
                case 6: return Item006;
                case 7: return Item007;
                case 8: return Item008;
                case 9: return Item009;
                case 10: return Item010;
                case 11: return Item011;
                case 12: return Item012;
                case 13: return Item013;
                case 14: return Item014;
                case 15: return Item015;
                default: throw new ArgumentOutOfRangeException("index");
            }
        }

        protected override void SetValueImpl(int index, object value)
        {
            switch (index)
            {
                case 0: Item000 = LanguagePrimitives.ConvertTo<T0>(value); break;
                case 1: Item001 = LanguagePrimitives.ConvertTo<T1>(value); break;
                case 2: Item002 = LanguagePrimitives.ConvertTo<T2>(value); break;
                case 3: Item003 = LanguagePrimitives.ConvertTo<T3>(value); break;
                case 4: Item004 = LanguagePrimitives.ConvertTo<T4>(value); break;
                case 5: Item005 = LanguagePrimitives.ConvertTo<T5>(value); break;
                case 6: Item006 = LanguagePrimitives.ConvertTo<T6>(value); break;
                case 7: Item007 = LanguagePrimitives.ConvertTo<T7>(value); break;
                case 8: Item008 = LanguagePrimitives.ConvertTo<T8>(value); break;
                case 9: Item009 = LanguagePrimitives.ConvertTo<T9>(value); break;
                case 10: Item010 = LanguagePrimitives.ConvertTo<T10>(value); break;
                case 11: Item011 = LanguagePrimitives.ConvertTo<T11>(value); break;
                case 12: Item012 = LanguagePrimitives.ConvertTo<T12>(value); break;
                case 13: Item013 = LanguagePrimitives.ConvertTo<T13>(value); break;
                case 14: Item014 = LanguagePrimitives.ConvertTo<T14>(value); break;
                case 15: Item015 = LanguagePrimitives.ConvertTo<T15>(value); break;
                default: throw new ArgumentOutOfRangeException("index");
            }
        }

        public override int Capacity
        {
            get
            {
                return 16;
            }
        }
    }

    [GeneratedCode("DLR", "2.0")]
    internal class MutableTuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31> : MutableTuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
    {
        public MutableTuple() { }

        public MutableTuple(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13, T14 item14, T15 item15, T16 item16, T17 item17, T18 item18, T19 item19, T20 item20, T21 item21, T22 item22, T23 item23, T24 item24, T25 item25, T26 item26, T27 item27, T28 item28, T29 item29, T30 item30, T31 item31)
          : base(item0, item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12, item13, item14, item15)
        {
            _item16 = item16;
            _item17 = item17;
            _item18 = item18;
            _item19 = item19;
            _item20 = item20;
            _item21 = item21;
            _item22 = item22;
            _item23 = item23;
            _item24 = item24;
            _item25 = item25;
            _item26 = item26;
            _item27 = item27;
            _item28 = item28;
            _item29 = item29;
            _item30 = item30;
            _item31 = item31;
        }

        private T16 _item16;
        private T17 _item17;
        private T18 _item18;
        private T19 _item19;
        private T20 _item20;
        private T21 _item21;
        private T22 _item22;
        private T23 _item23;
        private T24 _item24;
        private T25 _item25;
        private T26 _item26;
        private T27 _item27;
        private T28 _item28;
        private T29 _item29;
        private T30 _item30;
        private T31 _item31;

        public T16 Item016
        {
            get { return _item16; }

            set { _item16 = value; _valuesSet[16] = true; }
        }

        public T17 Item017
        {
            get { return _item17; }

            set { _item17 = value; _valuesSet[17] = true; }
        }

        public T18 Item018
        {
            get { return _item18; }

            set { _item18 = value; _valuesSet[18] = true; }
        }

        public T19 Item019
        {
            get { return _item19; }

            set { _item19 = value; _valuesSet[19] = true; }
        }

        public T20 Item020
        {
            get { return _item20; }

            set { _item20 = value; _valuesSet[20] = true; }
        }

        public T21 Item021
        {
            get { return _item21; }

            set { _item21 = value; _valuesSet[21] = true; }
        }

        public T22 Item022
        {
            get { return _item22; }

            set { _item22 = value; _valuesSet[22] = true; }
        }

        public T23 Item023
        {
            get { return _item23; }

            set { _item23 = value; _valuesSet[23] = true; }
        }

        public T24 Item024
        {
            get { return _item24; }

            set { _item24 = value; _valuesSet[24] = true; }
        }

        public T25 Item025
        {
            get { return _item25; }

            set { _item25 = value; _valuesSet[25] = true; }
        }

        public T26 Item026
        {
            get { return _item26; }

            set { _item26 = value; _valuesSet[26] = true; }
        }

        public T27 Item027
        {
            get { return _item27; }

            set { _item27 = value; _valuesSet[27] = true; }
        }

        public T28 Item028
        {
            get { return _item28; }

            set { _item28 = value; _valuesSet[28] = true; }
        }

        public T29 Item029
        {
            get { return _item29; }

            set { _item29 = value; _valuesSet[29] = true; }
        }

        public T30 Item030
        {
            get { return _item30; }

            set { _item30 = value; _valuesSet[30] = true; }
        }

        public T31 Item031
        {
            get { return _item31; }

            set { _item31 = value; _valuesSet[31] = true; }
        }

        protected override object GetValueImpl(int index)
        {
            switch (index)
            {
                case 0: return Item000;
                case 1: return Item001;
                case 2: return Item002;
                case 3: return Item003;
                case 4: return Item004;
                case 5: return Item005;
                case 6: return Item006;
                case 7: return Item007;
                case 8: return Item008;
                case 9: return Item009;
                case 10: return Item010;
                case 11: return Item011;
                case 12: return Item012;
                case 13: return Item013;
                case 14: return Item014;
                case 15: return Item015;
                case 16: return Item016;
                case 17: return Item017;
                case 18: return Item018;
                case 19: return Item019;
                case 20: return Item020;
                case 21: return Item021;
                case 22: return Item022;
                case 23: return Item023;
                case 24: return Item024;
                case 25: return Item025;
                case 26: return Item026;
                case 27: return Item027;
                case 28: return Item028;
                case 29: return Item029;
                case 30: return Item030;
                case 31: return Item031;
                default: throw new ArgumentOutOfRangeException("index");
            }
        }

        protected override void SetValueImpl(int index, object value)
        {
            switch (index)
            {
                case 0: Item000 = LanguagePrimitives.ConvertTo<T0>(value); break;
                case 1: Item001 = LanguagePrimitives.ConvertTo<T1>(value); break;
                case 2: Item002 = LanguagePrimitives.ConvertTo<T2>(value); break;
                case 3: Item003 = LanguagePrimitives.ConvertTo<T3>(value); break;
                case 4: Item004 = LanguagePrimitives.ConvertTo<T4>(value); break;
                case 5: Item005 = LanguagePrimitives.ConvertTo<T5>(value); break;
                case 6: Item006 = LanguagePrimitives.ConvertTo<T6>(value); break;
                case 7: Item007 = LanguagePrimitives.ConvertTo<T7>(value); break;
                case 8: Item008 = LanguagePrimitives.ConvertTo<T8>(value); break;
                case 9: Item009 = LanguagePrimitives.ConvertTo<T9>(value); break;
                case 10: Item010 = LanguagePrimitives.ConvertTo<T10>(value); break;
                case 11: Item011 = LanguagePrimitives.ConvertTo<T11>(value); break;
                case 12: Item012 = LanguagePrimitives.ConvertTo<T12>(value); break;
                case 13: Item013 = LanguagePrimitives.ConvertTo<T13>(value); break;
                case 14: Item014 = LanguagePrimitives.ConvertTo<T14>(value); break;
                case 15: Item015 = LanguagePrimitives.ConvertTo<T15>(value); break;
                case 16: Item016 = LanguagePrimitives.ConvertTo<T16>(value); break;
                case 17: Item017 = LanguagePrimitives.ConvertTo<T17>(value); break;
                case 18: Item018 = LanguagePrimitives.ConvertTo<T18>(value); break;
                case 19: Item019 = LanguagePrimitives.ConvertTo<T19>(value); break;
                case 20: Item020 = LanguagePrimitives.ConvertTo<T20>(value); break;
                case 21: Item021 = LanguagePrimitives.ConvertTo<T21>(value); break;
                case 22: Item022 = LanguagePrimitives.ConvertTo<T22>(value); break;
                case 23: Item023 = LanguagePrimitives.ConvertTo<T23>(value); break;
                case 24: Item024 = LanguagePrimitives.ConvertTo<T24>(value); break;
                case 25: Item025 = LanguagePrimitives.ConvertTo<T25>(value); break;
                case 26: Item026 = LanguagePrimitives.ConvertTo<T26>(value); break;
                case 27: Item027 = LanguagePrimitives.ConvertTo<T27>(value); break;
                case 28: Item028 = LanguagePrimitives.ConvertTo<T28>(value); break;
                case 29: Item029 = LanguagePrimitives.ConvertTo<T29>(value); break;
                case 30: Item030 = LanguagePrimitives.ConvertTo<T30>(value); break;
                case 31: Item031 = LanguagePrimitives.ConvertTo<T31>(value); break;
                default: throw new ArgumentOutOfRangeException("index");
            }
        }

        public override int Capacity
        {
            get
            {
                return 32;
            }
        }
    }

    [GeneratedCode("DLR", "2.0")]
    internal class MutableTuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32, T33, T34, T35, T36, T37, T38, T39, T40, T41, T42, T43, T44, T45, T46, T47, T48, T49, T50, T51, T52, T53, T54, T55, T56, T57, T58, T59, T60, T61, T62, T63> : MutableTuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>
    {
        public MutableTuple() { }

        public MutableTuple(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13, T14 item14, T15 item15, T16 item16, T17 item17, T18 item18, T19 item19, T20 item20, T21 item21, T22 item22, T23 item23, T24 item24, T25 item25, T26 item26, T27 item27, T28 item28, T29 item29, T30 item30, T31 item31, T32 item32, T33 item33, T34 item34, T35 item35, T36 item36, T37 item37, T38 item38, T39 item39, T40 item40, T41 item41, T42 item42, T43 item43, T44 item44, T45 item45, T46 item46, T47 item47, T48 item48, T49 item49, T50 item50, T51 item51, T52 item52, T53 item53, T54 item54, T55 item55, T56 item56, T57 item57, T58 item58, T59 item59, T60 item60, T61 item61, T62 item62, T63 item63)
          : base(item0, item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12, item13, item14, item15, item16, item17, item18, item19, item20, item21, item22, item23, item24, item25, item26, item27, item28, item29, item30, item31)
        {
            _item32 = item32;
            _item33 = item33;
            _item34 = item34;
            _item35 = item35;
            _item36 = item36;
            _item37 = item37;
            _item38 = item38;
            _item39 = item39;
            _item40 = item40;
            _item41 = item41;
            _item42 = item42;
            _item43 = item43;
            _item44 = item44;
            _item45 = item45;
            _item46 = item46;
            _item47 = item47;
            _item48 = item48;
            _item49 = item49;
            _item50 = item50;
            _item51 = item51;
            _item52 = item52;
            _item53 = item53;
            _item54 = item54;
            _item55 = item55;
            _item56 = item56;
            _item57 = item57;
            _item58 = item58;
            _item59 = item59;
            _item60 = item60;
            _item61 = item61;
            _item62 = item62;
            _item63 = item63;
        }

        private T32 _item32;
        private T33 _item33;
        private T34 _item34;
        private T35 _item35;
        private T36 _item36;
        private T37 _item37;
        private T38 _item38;
        private T39 _item39;
        private T40 _item40;
        private T41 _item41;
        private T42 _item42;
        private T43 _item43;
        private T44 _item44;
        private T45 _item45;
        private T46 _item46;
        private T47 _item47;
        private T48 _item48;
        private T49 _item49;
        private T50 _item50;
        private T51 _item51;
        private T52 _item52;
        private T53 _item53;
        private T54 _item54;
        private T55 _item55;
        private T56 _item56;
        private T57 _item57;
        private T58 _item58;
        private T59 _item59;
        private T60 _item60;
        private T61 _item61;
        private T62 _item62;
        private T63 _item63;

        public T32 Item032
        {
            get { return _item32; }

            set { _item32 = value; _valuesSet[32] = true; }
        }

        public T33 Item033
        {
            get { return _item33; }

            set { _item33 = value; _valuesSet[33] = true; }
        }

        public T34 Item034
        {
            get { return _item34; }

            set { _item34 = value; _valuesSet[34] = true; }
        }

        public T35 Item035
        {
            get { return _item35; }

            set { _item35 = value; _valuesSet[35] = true; }
        }

        public T36 Item036
        {
            get { return _item36; }

            set { _item36 = value; _valuesSet[36] = true; }
        }

        public T37 Item037
        {
            get { return _item37; }

            set { _item37 = value; _valuesSet[37] = true; }
        }

        public T38 Item038
        {
            get { return _item38; }

            set { _item38 = value; _valuesSet[38] = true; }
        }

        public T39 Item039
        {
            get { return _item39; }

            set { _item39 = value; _valuesSet[39] = true; }
        }

        public T40 Item040
        {
            get { return _item40; }

            set { _item40 = value; _valuesSet[40] = true; }
        }

        public T41 Item041
        {
            get { return _item41; }

            set { _item41 = value; _valuesSet[41] = true; }
        }

        public T42 Item042
        {
            get { return _item42; }

            set { _item42 = value; _valuesSet[42] = true; }
        }

        public T43 Item043
        {
            get { return _item43; }

            set { _item43 = value; _valuesSet[43] = true; }
        }

        public T44 Item044
        {
            get { return _item44; }

            set { _item44 = value; _valuesSet[44] = true; }
        }

        public T45 Item045
        {
            get { return _item45; }

            set { _item45 = value; _valuesSet[45] = true; }
        }

        public T46 Item046
        {
            get { return _item46; }

            set { _item46 = value; _valuesSet[46] = true; }
        }

        public T47 Item047
        {
            get { return _item47; }

            set { _item47 = value; _valuesSet[47] = true; }
        }

        public T48 Item048
        {
            get { return _item48; }

            set { _item48 = value; _valuesSet[48] = true; }
        }

        public T49 Item049
        {
            get { return _item49; }

            set { _item49 = value; _valuesSet[49] = true; }
        }

        public T50 Item050
        {
            get { return _item50; }

            set { _item50 = value; _valuesSet[50] = true; }
        }

        public T51 Item051
        {
            get { return _item51; }

            set { _item51 = value; _valuesSet[51] = true; }
        }

        public T52 Item052
        {
            get { return _item52; }

            set { _item52 = value; _valuesSet[52] = true; }
        }

        public T53 Item053
        {
            get { return _item53; }

            set { _item53 = value; _valuesSet[53] = true; }
        }

        public T54 Item054
        {
            get { return _item54; }

            set { _item54 = value; _valuesSet[54] = true; }
        }

        public T55 Item055
        {
            get { return _item55; }

            set { _item55 = value; _valuesSet[55] = true; }
        }

        public T56 Item056
        {
            get { return _item56; }

            set { _item56 = value; _valuesSet[56] = true; }
        }

        public T57 Item057
        {
            get { return _item57; }

            set { _item57 = value; _valuesSet[57] = true; }
        }

        public T58 Item058
        {
            get { return _item58; }

            set { _item58 = value; _valuesSet[58] = true; }
        }

        public T59 Item059
        {
            get { return _item59; }

            set { _item59 = value; _valuesSet[59] = true; }
        }

        public T60 Item060
        {
            get { return _item60; }

            set { _item60 = value; _valuesSet[60] = true; }
        }

        public T61 Item061
        {
            get { return _item61; }

            set { _item61 = value; _valuesSet[61] = true; }
        }

        public T62 Item062
        {
            get { return _item62; }

            set { _item62 = value; _valuesSet[62] = true; }
        }

        public T63 Item063
        {
            get { return _item63; }

            set { _item63 = value; _valuesSet[63] = true; }
        }

        protected override object GetValueImpl(int index)
        {
            switch (index)
            {
                case 0: return Item000;
                case 1: return Item001;
                case 2: return Item002;
                case 3: return Item003;
                case 4: return Item004;
                case 5: return Item005;
                case 6: return Item006;
                case 7: return Item007;
                case 8: return Item008;
                case 9: return Item009;
                case 10: return Item010;
                case 11: return Item011;
                case 12: return Item012;
                case 13: return Item013;
                case 14: return Item014;
                case 15: return Item015;
                case 16: return Item016;
                case 17: return Item017;
                case 18: return Item018;
                case 19: return Item019;
                case 20: return Item020;
                case 21: return Item021;
                case 22: return Item022;
                case 23: return Item023;
                case 24: return Item024;
                case 25: return Item025;
                case 26: return Item026;
                case 27: return Item027;
                case 28: return Item028;
                case 29: return Item029;
                case 30: return Item030;
                case 31: return Item031;
                case 32: return Item032;
                case 33: return Item033;
                case 34: return Item034;
                case 35: return Item035;
                case 36: return Item036;
                case 37: return Item037;
                case 38: return Item038;
                case 39: return Item039;
                case 40: return Item040;
                case 41: return Item041;
                case 42: return Item042;
                case 43: return Item043;
                case 44: return Item044;
                case 45: return Item045;
                case 46: return Item046;
                case 47: return Item047;
                case 48: return Item048;
                case 49: return Item049;
                case 50: return Item050;
                case 51: return Item051;
                case 52: return Item052;
                case 53: return Item053;
                case 54: return Item054;
                case 55: return Item055;
                case 56: return Item056;
                case 57: return Item057;
                case 58: return Item058;
                case 59: return Item059;
                case 60: return Item060;
                case 61: return Item061;
                case 62: return Item062;
                case 63: return Item063;
                default: throw new ArgumentOutOfRangeException("index");
            }
        }

        protected override void SetValueImpl(int index, object value)
        {
            switch (index)
            {
                case 0: Item000 = LanguagePrimitives.ConvertTo<T0>(value); break;
                case 1: Item001 = LanguagePrimitives.ConvertTo<T1>(value); break;
                case 2: Item002 = LanguagePrimitives.ConvertTo<T2>(value); break;
                case 3: Item003 = LanguagePrimitives.ConvertTo<T3>(value); break;
                case 4: Item004 = LanguagePrimitives.ConvertTo<T4>(value); break;
                case 5: Item005 = LanguagePrimitives.ConvertTo<T5>(value); break;
                case 6: Item006 = LanguagePrimitives.ConvertTo<T6>(value); break;
                case 7: Item007 = LanguagePrimitives.ConvertTo<T7>(value); break;
                case 8: Item008 = LanguagePrimitives.ConvertTo<T8>(value); break;
                case 9: Item009 = LanguagePrimitives.ConvertTo<T9>(value); break;
                case 10: Item010 = LanguagePrimitives.ConvertTo<T10>(value); break;
                case 11: Item011 = LanguagePrimitives.ConvertTo<T11>(value); break;
                case 12: Item012 = LanguagePrimitives.ConvertTo<T12>(value); break;
                case 13: Item013 = LanguagePrimitives.ConvertTo<T13>(value); break;
                case 14: Item014 = LanguagePrimitives.ConvertTo<T14>(value); break;
                case 15: Item015 = LanguagePrimitives.ConvertTo<T15>(value); break;
                case 16: Item016 = LanguagePrimitives.ConvertTo<T16>(value); break;
                case 17: Item017 = LanguagePrimitives.ConvertTo<T17>(value); break;
                case 18: Item018 = LanguagePrimitives.ConvertTo<T18>(value); break;
                case 19: Item019 = LanguagePrimitives.ConvertTo<T19>(value); break;
                case 20: Item020 = LanguagePrimitives.ConvertTo<T20>(value); break;
                case 21: Item021 = LanguagePrimitives.ConvertTo<T21>(value); break;
                case 22: Item022 = LanguagePrimitives.ConvertTo<T22>(value); break;
                case 23: Item023 = LanguagePrimitives.ConvertTo<T23>(value); break;
                case 24: Item024 = LanguagePrimitives.ConvertTo<T24>(value); break;
                case 25: Item025 = LanguagePrimitives.ConvertTo<T25>(value); break;
                case 26: Item026 = LanguagePrimitives.ConvertTo<T26>(value); break;
                case 27: Item027 = LanguagePrimitives.ConvertTo<T27>(value); break;
                case 28: Item028 = LanguagePrimitives.ConvertTo<T28>(value); break;
                case 29: Item029 = LanguagePrimitives.ConvertTo<T29>(value); break;
                case 30: Item030 = LanguagePrimitives.ConvertTo<T30>(value); break;
                case 31: Item031 = LanguagePrimitives.ConvertTo<T31>(value); break;
                case 32: Item032 = LanguagePrimitives.ConvertTo<T32>(value); break;
                case 33: Item033 = LanguagePrimitives.ConvertTo<T33>(value); break;
                case 34: Item034 = LanguagePrimitives.ConvertTo<T34>(value); break;
                case 35: Item035 = LanguagePrimitives.ConvertTo<T35>(value); break;
                case 36: Item036 = LanguagePrimitives.ConvertTo<T36>(value); break;
                case 37: Item037 = LanguagePrimitives.ConvertTo<T37>(value); break;
                case 38: Item038 = LanguagePrimitives.ConvertTo<T38>(value); break;
                case 39: Item039 = LanguagePrimitives.ConvertTo<T39>(value); break;
                case 40: Item040 = LanguagePrimitives.ConvertTo<T40>(value); break;
                case 41: Item041 = LanguagePrimitives.ConvertTo<T41>(value); break;
                case 42: Item042 = LanguagePrimitives.ConvertTo<T42>(value); break;
                case 43: Item043 = LanguagePrimitives.ConvertTo<T43>(value); break;
                case 44: Item044 = LanguagePrimitives.ConvertTo<T44>(value); break;
                case 45: Item045 = LanguagePrimitives.ConvertTo<T45>(value); break;
                case 46: Item046 = LanguagePrimitives.ConvertTo<T46>(value); break;
                case 47: Item047 = LanguagePrimitives.ConvertTo<T47>(value); break;
                case 48: Item048 = LanguagePrimitives.ConvertTo<T48>(value); break;
                case 49: Item049 = LanguagePrimitives.ConvertTo<T49>(value); break;
                case 50: Item050 = LanguagePrimitives.ConvertTo<T50>(value); break;
                case 51: Item051 = LanguagePrimitives.ConvertTo<T51>(value); break;
                case 52: Item052 = LanguagePrimitives.ConvertTo<T52>(value); break;
                case 53: Item053 = LanguagePrimitives.ConvertTo<T53>(value); break;
                case 54: Item054 = LanguagePrimitives.ConvertTo<T54>(value); break;
                case 55: Item055 = LanguagePrimitives.ConvertTo<T55>(value); break;
                case 56: Item056 = LanguagePrimitives.ConvertTo<T56>(value); break;
                case 57: Item057 = LanguagePrimitives.ConvertTo<T57>(value); break;
                case 58: Item058 = LanguagePrimitives.ConvertTo<T58>(value); break;
                case 59: Item059 = LanguagePrimitives.ConvertTo<T59>(value); break;
                case 60: Item060 = LanguagePrimitives.ConvertTo<T60>(value); break;
                case 61: Item061 = LanguagePrimitives.ConvertTo<T61>(value); break;
                case 62: Item062 = LanguagePrimitives.ConvertTo<T62>(value); break;
                case 63: Item063 = LanguagePrimitives.ConvertTo<T63>(value); break;
                default: throw new ArgumentOutOfRangeException("index");
            }
        }

        public override int Capacity
        {
            get
            {
                return 64;
            }
        }
    }

    [GeneratedCode("DLR", "2.0")]
    internal class MutableTuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32, T33, T34, T35, T36, T37, T38, T39, T40, T41, T42, T43, T44, T45, T46, T47, T48, T49, T50, T51, T52, T53, T54, T55, T56, T57, T58, T59, T60, T61, T62, T63, T64, T65, T66, T67, T68, T69, T70, T71, T72, T73, T74, T75, T76, T77, T78, T79, T80, T81, T82, T83, T84, T85, T86, T87, T88, T89, T90, T91, T92, T93, T94, T95, T96, T97, T98, T99, T100, T101, T102, T103, T104, T105, T106, T107, T108, T109, T110, T111, T112, T113, T114, T115, T116, T117, T118, T119, T120, T121, T122, T123, T124, T125, T126, T127> : MutableTuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32, T33, T34, T35, T36, T37, T38, T39, T40, T41, T42, T43, T44, T45, T46, T47, T48, T49, T50, T51, T52, T53, T54, T55, T56, T57, T58, T59, T60, T61, T62, T63>
    {
        public MutableTuple() { }

        public MutableTuple(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13, T14 item14, T15 item15, T16 item16, T17 item17, T18 item18, T19 item19, T20 item20, T21 item21, T22 item22, T23 item23, T24 item24, T25 item25, T26 item26, T27 item27, T28 item28, T29 item29, T30 item30, T31 item31, T32 item32, T33 item33, T34 item34, T35 item35, T36 item36, T37 item37, T38 item38, T39 item39, T40 item40, T41 item41, T42 item42, T43 item43, T44 item44, T45 item45, T46 item46, T47 item47, T48 item48, T49 item49, T50 item50, T51 item51, T52 item52, T53 item53, T54 item54, T55 item55, T56 item56, T57 item57, T58 item58, T59 item59, T60 item60, T61 item61, T62 item62, T63 item63, T64 item64, T65 item65, T66 item66, T67 item67, T68 item68, T69 item69, T70 item70, T71 item71, T72 item72, T73 item73, T74 item74, T75 item75, T76 item76, T77 item77, T78 item78, T79 item79, T80 item80, T81 item81, T82 item82, T83 item83, T84 item84, T85 item85, T86 item86, T87 item87, T88 item88, T89 item89, T90 item90, T91 item91, T92 item92, T93 item93, T94 item94, T95 item95, T96 item96, T97 item97, T98 item98, T99 item99, T100 item100, T101 item101, T102 item102, T103 item103, T104 item104, T105 item105, T106 item106, T107 item107, T108 item108, T109 item109, T110 item110, T111 item111, T112 item112, T113 item113, T114 item114, T115 item115, T116 item116, T117 item117, T118 item118, T119 item119, T120 item120, T121 item121, T122 item122, T123 item123, T124 item124, T125 item125, T126 item126, T127 item127)
          : base(item0, item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12, item13, item14, item15, item16, item17, item18, item19, item20, item21, item22, item23, item24, item25, item26, item27, item28, item29, item30, item31, item32, item33, item34, item35, item36, item37, item38, item39, item40, item41, item42, item43, item44, item45, item46, item47, item48, item49, item50, item51, item52, item53, item54, item55, item56, item57, item58, item59, item60, item61, item62, item63)
        {
            _item64 = item64;
            _item65 = item65;
            _item66 = item66;
            _item67 = item67;
            _item68 = item68;
            _item69 = item69;
            _item70 = item70;
            _item71 = item71;
            _item72 = item72;
            _item73 = item73;
            _item74 = item74;
            _item75 = item75;
            _item76 = item76;
            _item77 = item77;
            _item78 = item78;
            _item79 = item79;
            _item80 = item80;
            _item81 = item81;
            _item82 = item82;
            _item83 = item83;
            _item84 = item84;
            _item85 = item85;
            _item86 = item86;
            _item87 = item87;
            _item88 = item88;
            _item89 = item89;
            _item90 = item90;
            _item91 = item91;
            _item92 = item92;
            _item93 = item93;
            _item94 = item94;
            _item95 = item95;
            _item96 = item96;
            _item97 = item97;
            _item98 = item98;
            _item99 = item99;
            _item100 = item100;
            _item101 = item101;
            _item102 = item102;
            _item103 = item103;
            _item104 = item104;
            _item105 = item105;
            _item106 = item106;
            _item107 = item107;
            _item108 = item108;
            _item109 = item109;
            _item110 = item110;
            _item111 = item111;
            _item112 = item112;
            _item113 = item113;
            _item114 = item114;
            _item115 = item115;
            _item116 = item116;
            _item117 = item117;
            _item118 = item118;
            _item119 = item119;
            _item120 = item120;
            _item121 = item121;
            _item122 = item122;
            _item123 = item123;
            _item124 = item124;
            _item125 = item125;
            _item126 = item126;
            _item127 = item127;
        }

        private T64 _item64;
        private T65 _item65;
        private T66 _item66;
        private T67 _item67;
        private T68 _item68;
        private T69 _item69;
        private T70 _item70;
        private T71 _item71;
        private T72 _item72;
        private T73 _item73;
        private T74 _item74;
        private T75 _item75;
        private T76 _item76;
        private T77 _item77;
        private T78 _item78;
        private T79 _item79;
        private T80 _item80;
        private T81 _item81;
        private T82 _item82;
        private T83 _item83;
        private T84 _item84;
        private T85 _item85;
        private T86 _item86;
        private T87 _item87;
        private T88 _item88;
        private T89 _item89;
        private T90 _item90;
        private T91 _item91;
        private T92 _item92;
        private T93 _item93;
        private T94 _item94;
        private T95 _item95;
        private T96 _item96;
        private T97 _item97;
        private T98 _item98;
        private T99 _item99;
        private T100 _item100;
        private T101 _item101;
        private T102 _item102;
        private T103 _item103;
        private T104 _item104;
        private T105 _item105;
        private T106 _item106;
        private T107 _item107;
        private T108 _item108;
        private T109 _item109;
        private T110 _item110;
        private T111 _item111;
        private T112 _item112;
        private T113 _item113;
        private T114 _item114;
        private T115 _item115;
        private T116 _item116;
        private T117 _item117;
        private T118 _item118;
        private T119 _item119;
        private T120 _item120;
        private T121 _item121;
        private T122 _item122;
        private T123 _item123;
        private T124 _item124;
        private T125 _item125;
        private T126 _item126;
        private T127 _item127;

        public T64 Item064
        {
            get { return _item64; }

            set { _item64 = value; _valuesSet[64] = true; }
        }

        public T65 Item065
        {
            get { return _item65; }

            set { _item65 = value; _valuesSet[65] = true; }
        }

        public T66 Item066
        {
            get { return _item66; }

            set { _item66 = value; _valuesSet[66] = true; }
        }

        public T67 Item067
        {
            get { return _item67; }

            set { _item67 = value; _valuesSet[67] = true; }
        }

        public T68 Item068
        {
            get { return _item68; }

            set { _item68 = value; _valuesSet[68] = true; }
        }

        public T69 Item069
        {
            get { return _item69; }

            set { _item69 = value; _valuesSet[69] = true; }
        }

        public T70 Item070
        {
            get { return _item70; }

            set { _item70 = value; _valuesSet[70] = true; }
        }

        public T71 Item071
        {
            get { return _item71; }

            set { _item71 = value; _valuesSet[71] = true; }
        }

        public T72 Item072
        {
            get { return _item72; }

            set { _item72 = value; _valuesSet[72] = true; }
        }

        public T73 Item073
        {
            get { return _item73; }

            set { _item73 = value; _valuesSet[73] = true; }
        }

        public T74 Item074
        {
            get { return _item74; }

            set { _item74 = value; _valuesSet[74] = true; }
        }

        public T75 Item075
        {
            get { return _item75; }

            set { _item75 = value; _valuesSet[75] = true; }
        }

        public T76 Item076
        {
            get { return _item76; }

            set { _item76 = value; _valuesSet[76] = true; }
        }

        public T77 Item077
        {
            get { return _item77; }

            set { _item77 = value; _valuesSet[77] = true; }
        }

        public T78 Item078
        {
            get { return _item78; }

            set { _item78 = value; _valuesSet[78] = true; }
        }

        public T79 Item079
        {
            get { return _item79; }

            set { _item79 = value; _valuesSet[79] = true; }
        }

        public T80 Item080
        {
            get { return _item80; }

            set { _item80 = value; _valuesSet[80] = true; }
        }

        public T81 Item081
        {
            get { return _item81; }

            set { _item81 = value; _valuesSet[81] = true; }
        }

        public T82 Item082
        {
            get { return _item82; }

            set { _item82 = value; _valuesSet[82] = true; }
        }

        public T83 Item083
        {
            get { return _item83; }

            set { _item83 = value; _valuesSet[83] = true; }
        }

        public T84 Item084
        {
            get { return _item84; }

            set { _item84 = value; _valuesSet[84] = true; }
        }

        public T85 Item085
        {
            get { return _item85; }

            set { _item85 = value; _valuesSet[85] = true; }
        }

        public T86 Item086
        {
            get { return _item86; }

            set { _item86 = value; _valuesSet[86] = true; }
        }

        public T87 Item087
        {
            get { return _item87; }

            set { _item87 = value; _valuesSet[87] = true; }
        }

        public T88 Item088
        {
            get { return _item88; }

            set { _item88 = value; _valuesSet[88] = true; }
        }

        public T89 Item089
        {
            get { return _item89; }

            set { _item89 = value; _valuesSet[89] = true; }
        }

        public T90 Item090
        {
            get { return _item90; }

            set { _item90 = value; _valuesSet[90] = true; }
        }

        public T91 Item091
        {
            get { return _item91; }

            set { _item91 = value; _valuesSet[91] = true; }
        }

        public T92 Item092
        {
            get { return _item92; }

            set { _item92 = value; _valuesSet[92] = true; }
        }

        public T93 Item093
        {
            get { return _item93; }

            set { _item93 = value; _valuesSet[93] = true; }
        }

        public T94 Item094
        {
            get { return _item94; }

            set { _item94 = value; _valuesSet[94] = true; }
        }

        public T95 Item095
        {
            get { return _item95; }

            set { _item95 = value; _valuesSet[95] = true; }
        }

        public T96 Item096
        {
            get { return _item96; }

            set { _item96 = value; _valuesSet[96] = true; }
        }

        public T97 Item097
        {
            get { return _item97; }

            set { _item97 = value; _valuesSet[97] = true; }
        }

        public T98 Item098
        {
            get { return _item98; }

            set { _item98 = value; _valuesSet[98] = true; }
        }

        public T99 Item099
        {
            get { return _item99; }

            set { _item99 = value; _valuesSet[99] = true; }
        }

        public T100 Item100
        {
            get { return _item100; }

            set { _item100 = value; _valuesSet[100] = true; }
        }

        public T101 Item101
        {
            get { return _item101; }

            set { _item101 = value; _valuesSet[101] = true; }
        }

        public T102 Item102
        {
            get { return _item102; }

            set { _item102 = value; _valuesSet[102] = true; }
        }

        public T103 Item103
        {
            get { return _item103; }

            set { _item103 = value; _valuesSet[103] = true; }
        }

        public T104 Item104
        {
            get { return _item104; }

            set { _item104 = value; _valuesSet[104] = true; }
        }

        public T105 Item105
        {
            get { return _item105; }

            set { _item105 = value; _valuesSet[105] = true; }
        }

        public T106 Item106
        {
            get { return _item106; }

            set { _item106 = value; _valuesSet[106] = true; }
        }

        public T107 Item107
        {
            get { return _item107; }

            set { _item107 = value; _valuesSet[107] = true; }
        }

        public T108 Item108
        {
            get { return _item108; }

            set { _item108 = value; _valuesSet[108] = true; }
        }

        public T109 Item109
        {
            get { return _item109; }

            set { _item109 = value; _valuesSet[109] = true; }
        }

        public T110 Item110
        {
            get { return _item110; }

            set { _item110 = value; _valuesSet[110] = true; }
        }

        public T111 Item111
        {
            get { return _item111; }

            set { _item111 = value; _valuesSet[111] = true; }
        }

        public T112 Item112
        {
            get { return _item112; }

            set { _item112 = value; _valuesSet[112] = true; }
        }

        public T113 Item113
        {
            get { return _item113; }

            set { _item113 = value; _valuesSet[113] = true; }
        }

        public T114 Item114
        {
            get { return _item114; }

            set { _item114 = value; _valuesSet[114] = true; }
        }

        public T115 Item115
        {
            get { return _item115; }

            set { _item115 = value; _valuesSet[115] = true; }
        }

        public T116 Item116
        {
            get { return _item116; }

            set { _item116 = value; _valuesSet[116] = true; }
        }

        public T117 Item117
        {
            get { return _item117; }

            set { _item117 = value; _valuesSet[117] = true; }
        }

        public T118 Item118
        {
            get { return _item118; }

            set { _item118 = value; _valuesSet[118] = true; }
        }

        public T119 Item119
        {
            get { return _item119; }

            set { _item119 = value; _valuesSet[119] = true; }
        }

        public T120 Item120
        {
            get { return _item120; }

            set { _item120 = value; _valuesSet[120] = true; }
        }

        public T121 Item121
        {
            get { return _item121; }

            set { _item121 = value; _valuesSet[121] = true; }
        }

        public T122 Item122
        {
            get { return _item122; }

            set { _item122 = value; _valuesSet[122] = true; }
        }

        public T123 Item123
        {
            get { return _item123; }

            set { _item123 = value; _valuesSet[123] = true; }
        }

        public T124 Item124
        {
            get { return _item124; }

            set { _item124 = value; _valuesSet[124] = true; }
        }

        public T125 Item125
        {
            get { return _item125; }

            set { _item125 = value; _valuesSet[125] = true; }
        }

        public T126 Item126
        {
            get { return _item126; }

            set { _item126 = value; _valuesSet[126] = true; }
        }

        public T127 Item127
        {
            get { return _item127; }

            set { _item127 = value; _valuesSet[127] = true; }
        }

        protected override object GetValueImpl(int index)
        {
            switch (index)
            {
                case 0: return Item000;
                case 1: return Item001;
                case 2: return Item002;
                case 3: return Item003;
                case 4: return Item004;
                case 5: return Item005;
                case 6: return Item006;
                case 7: return Item007;
                case 8: return Item008;
                case 9: return Item009;
                case 10: return Item010;
                case 11: return Item011;
                case 12: return Item012;
                case 13: return Item013;
                case 14: return Item014;
                case 15: return Item015;
                case 16: return Item016;
                case 17: return Item017;
                case 18: return Item018;
                case 19: return Item019;
                case 20: return Item020;
                case 21: return Item021;
                case 22: return Item022;
                case 23: return Item023;
                case 24: return Item024;
                case 25: return Item025;
                case 26: return Item026;
                case 27: return Item027;
                case 28: return Item028;
                case 29: return Item029;
                case 30: return Item030;
                case 31: return Item031;
                case 32: return Item032;
                case 33: return Item033;
                case 34: return Item034;
                case 35: return Item035;
                case 36: return Item036;
                case 37: return Item037;
                case 38: return Item038;
                case 39: return Item039;
                case 40: return Item040;
                case 41: return Item041;
                case 42: return Item042;
                case 43: return Item043;
                case 44: return Item044;
                case 45: return Item045;
                case 46: return Item046;
                case 47: return Item047;
                case 48: return Item048;
                case 49: return Item049;
                case 50: return Item050;
                case 51: return Item051;
                case 52: return Item052;
                case 53: return Item053;
                case 54: return Item054;
                case 55: return Item055;
                case 56: return Item056;
                case 57: return Item057;
                case 58: return Item058;
                case 59: return Item059;
                case 60: return Item060;
                case 61: return Item061;
                case 62: return Item062;
                case 63: return Item063;
                case 64: return Item064;
                case 65: return Item065;
                case 66: return Item066;
                case 67: return Item067;
                case 68: return Item068;
                case 69: return Item069;
                case 70: return Item070;
                case 71: return Item071;
                case 72: return Item072;
                case 73: return Item073;
                case 74: return Item074;
                case 75: return Item075;
                case 76: return Item076;
                case 77: return Item077;
                case 78: return Item078;
                case 79: return Item079;
                case 80: return Item080;
                case 81: return Item081;
                case 82: return Item082;
                case 83: return Item083;
                case 84: return Item084;
                case 85: return Item085;
                case 86: return Item086;
                case 87: return Item087;
                case 88: return Item088;
                case 89: return Item089;
                case 90: return Item090;
                case 91: return Item091;
                case 92: return Item092;
                case 93: return Item093;
                case 94: return Item094;
                case 95: return Item095;
                case 96: return Item096;
                case 97: return Item097;
                case 98: return Item098;
                case 99: return Item099;
                case 100: return Item100;
                case 101: return Item101;
                case 102: return Item102;
                case 103: return Item103;
                case 104: return Item104;
                case 105: return Item105;
                case 106: return Item106;
                case 107: return Item107;
                case 108: return Item108;
                case 109: return Item109;
                case 110: return Item110;
                case 111: return Item111;
                case 112: return Item112;
                case 113: return Item113;
                case 114: return Item114;
                case 115: return Item115;
                case 116: return Item116;
                case 117: return Item117;
                case 118: return Item118;
                case 119: return Item119;
                case 120: return Item120;
                case 121: return Item121;
                case 122: return Item122;
                case 123: return Item123;
                case 124: return Item124;
                case 125: return Item125;
                case 126: return Item126;
                case 127: return Item127;
                default: throw new ArgumentOutOfRangeException("index");
            }
        }

        protected override void SetValueImpl(int index, object value)
        {
            switch (index)
            {
                case 0: Item000 = LanguagePrimitives.ConvertTo<T0>(value); break;
                case 1: Item001 = LanguagePrimitives.ConvertTo<T1>(value); break;
                case 2: Item002 = LanguagePrimitives.ConvertTo<T2>(value); break;
                case 3: Item003 = LanguagePrimitives.ConvertTo<T3>(value); break;
                case 4: Item004 = LanguagePrimitives.ConvertTo<T4>(value); break;
                case 5: Item005 = LanguagePrimitives.ConvertTo<T5>(value); break;
                case 6: Item006 = LanguagePrimitives.ConvertTo<T6>(value); break;
                case 7: Item007 = LanguagePrimitives.ConvertTo<T7>(value); break;
                case 8: Item008 = LanguagePrimitives.ConvertTo<T8>(value); break;
                case 9: Item009 = LanguagePrimitives.ConvertTo<T9>(value); break;
                case 10: Item010 = LanguagePrimitives.ConvertTo<T10>(value); break;
                case 11: Item011 = LanguagePrimitives.ConvertTo<T11>(value); break;
                case 12: Item012 = LanguagePrimitives.ConvertTo<T12>(value); break;
                case 13: Item013 = LanguagePrimitives.ConvertTo<T13>(value); break;
                case 14: Item014 = LanguagePrimitives.ConvertTo<T14>(value); break;
                case 15: Item015 = LanguagePrimitives.ConvertTo<T15>(value); break;
                case 16: Item016 = LanguagePrimitives.ConvertTo<T16>(value); break;
                case 17: Item017 = LanguagePrimitives.ConvertTo<T17>(value); break;
                case 18: Item018 = LanguagePrimitives.ConvertTo<T18>(value); break;
                case 19: Item019 = LanguagePrimitives.ConvertTo<T19>(value); break;
                case 20: Item020 = LanguagePrimitives.ConvertTo<T20>(value); break;
                case 21: Item021 = LanguagePrimitives.ConvertTo<T21>(value); break;
                case 22: Item022 = LanguagePrimitives.ConvertTo<T22>(value); break;
                case 23: Item023 = LanguagePrimitives.ConvertTo<T23>(value); break;
                case 24: Item024 = LanguagePrimitives.ConvertTo<T24>(value); break;
                case 25: Item025 = LanguagePrimitives.ConvertTo<T25>(value); break;
                case 26: Item026 = LanguagePrimitives.ConvertTo<T26>(value); break;
                case 27: Item027 = LanguagePrimitives.ConvertTo<T27>(value); break;
                case 28: Item028 = LanguagePrimitives.ConvertTo<T28>(value); break;
                case 29: Item029 = LanguagePrimitives.ConvertTo<T29>(value); break;
                case 30: Item030 = LanguagePrimitives.ConvertTo<T30>(value); break;
                case 31: Item031 = LanguagePrimitives.ConvertTo<T31>(value); break;
                case 32: Item032 = LanguagePrimitives.ConvertTo<T32>(value); break;
                case 33: Item033 = LanguagePrimitives.ConvertTo<T33>(value); break;
                case 34: Item034 = LanguagePrimitives.ConvertTo<T34>(value); break;
                case 35: Item035 = LanguagePrimitives.ConvertTo<T35>(value); break;
                case 36: Item036 = LanguagePrimitives.ConvertTo<T36>(value); break;
                case 37: Item037 = LanguagePrimitives.ConvertTo<T37>(value); break;
                case 38: Item038 = LanguagePrimitives.ConvertTo<T38>(value); break;
                case 39: Item039 = LanguagePrimitives.ConvertTo<T39>(value); break;
                case 40: Item040 = LanguagePrimitives.ConvertTo<T40>(value); break;
                case 41: Item041 = LanguagePrimitives.ConvertTo<T41>(value); break;
                case 42: Item042 = LanguagePrimitives.ConvertTo<T42>(value); break;
                case 43: Item043 = LanguagePrimitives.ConvertTo<T43>(value); break;
                case 44: Item044 = LanguagePrimitives.ConvertTo<T44>(value); break;
                case 45: Item045 = LanguagePrimitives.ConvertTo<T45>(value); break;
                case 46: Item046 = LanguagePrimitives.ConvertTo<T46>(value); break;
                case 47: Item047 = LanguagePrimitives.ConvertTo<T47>(value); break;
                case 48: Item048 = LanguagePrimitives.ConvertTo<T48>(value); break;
                case 49: Item049 = LanguagePrimitives.ConvertTo<T49>(value); break;
                case 50: Item050 = LanguagePrimitives.ConvertTo<T50>(value); break;
                case 51: Item051 = LanguagePrimitives.ConvertTo<T51>(value); break;
                case 52: Item052 = LanguagePrimitives.ConvertTo<T52>(value); break;
                case 53: Item053 = LanguagePrimitives.ConvertTo<T53>(value); break;
                case 54: Item054 = LanguagePrimitives.ConvertTo<T54>(value); break;
                case 55: Item055 = LanguagePrimitives.ConvertTo<T55>(value); break;
                case 56: Item056 = LanguagePrimitives.ConvertTo<T56>(value); break;
                case 57: Item057 = LanguagePrimitives.ConvertTo<T57>(value); break;
                case 58: Item058 = LanguagePrimitives.ConvertTo<T58>(value); break;
                case 59: Item059 = LanguagePrimitives.ConvertTo<T59>(value); break;
                case 60: Item060 = LanguagePrimitives.ConvertTo<T60>(value); break;
                case 61: Item061 = LanguagePrimitives.ConvertTo<T61>(value); break;
                case 62: Item062 = LanguagePrimitives.ConvertTo<T62>(value); break;
                case 63: Item063 = LanguagePrimitives.ConvertTo<T63>(value); break;
                case 64: Item064 = LanguagePrimitives.ConvertTo<T64>(value); break;
                case 65: Item065 = LanguagePrimitives.ConvertTo<T65>(value); break;
                case 66: Item066 = LanguagePrimitives.ConvertTo<T66>(value); break;
                case 67: Item067 = LanguagePrimitives.ConvertTo<T67>(value); break;
                case 68: Item068 = LanguagePrimitives.ConvertTo<T68>(value); break;
                case 69: Item069 = LanguagePrimitives.ConvertTo<T69>(value); break;
                case 70: Item070 = LanguagePrimitives.ConvertTo<T70>(value); break;
                case 71: Item071 = LanguagePrimitives.ConvertTo<T71>(value); break;
                case 72: Item072 = LanguagePrimitives.ConvertTo<T72>(value); break;
                case 73: Item073 = LanguagePrimitives.ConvertTo<T73>(value); break;
                case 74: Item074 = LanguagePrimitives.ConvertTo<T74>(value); break;
                case 75: Item075 = LanguagePrimitives.ConvertTo<T75>(value); break;
                case 76: Item076 = LanguagePrimitives.ConvertTo<T76>(value); break;
                case 77: Item077 = LanguagePrimitives.ConvertTo<T77>(value); break;
                case 78: Item078 = LanguagePrimitives.ConvertTo<T78>(value); break;
                case 79: Item079 = LanguagePrimitives.ConvertTo<T79>(value); break;
                case 80: Item080 = LanguagePrimitives.ConvertTo<T80>(value); break;
                case 81: Item081 = LanguagePrimitives.ConvertTo<T81>(value); break;
                case 82: Item082 = LanguagePrimitives.ConvertTo<T82>(value); break;
                case 83: Item083 = LanguagePrimitives.ConvertTo<T83>(value); break;
                case 84: Item084 = LanguagePrimitives.ConvertTo<T84>(value); break;
                case 85: Item085 = LanguagePrimitives.ConvertTo<T85>(value); break;
                case 86: Item086 = LanguagePrimitives.ConvertTo<T86>(value); break;
                case 87: Item087 = LanguagePrimitives.ConvertTo<T87>(value); break;
                case 88: Item088 = LanguagePrimitives.ConvertTo<T88>(value); break;
                case 89: Item089 = LanguagePrimitives.ConvertTo<T89>(value); break;
                case 90: Item090 = LanguagePrimitives.ConvertTo<T90>(value); break;
                case 91: Item091 = LanguagePrimitives.ConvertTo<T91>(value); break;
                case 92: Item092 = LanguagePrimitives.ConvertTo<T92>(value); break;
                case 93: Item093 = LanguagePrimitives.ConvertTo<T93>(value); break;
                case 94: Item094 = LanguagePrimitives.ConvertTo<T94>(value); break;
                case 95: Item095 = LanguagePrimitives.ConvertTo<T95>(value); break;
                case 96: Item096 = LanguagePrimitives.ConvertTo<T96>(value); break;
                case 97: Item097 = LanguagePrimitives.ConvertTo<T97>(value); break;
                case 98: Item098 = LanguagePrimitives.ConvertTo<T98>(value); break;
                case 99: Item099 = LanguagePrimitives.ConvertTo<T99>(value); break;
                case 100: Item100 = LanguagePrimitives.ConvertTo<T100>(value); break;
                case 101: Item101 = LanguagePrimitives.ConvertTo<T101>(value); break;
                case 102: Item102 = LanguagePrimitives.ConvertTo<T102>(value); break;
                case 103: Item103 = LanguagePrimitives.ConvertTo<T103>(value); break;
                case 104: Item104 = LanguagePrimitives.ConvertTo<T104>(value); break;
                case 105: Item105 = LanguagePrimitives.ConvertTo<T105>(value); break;
                case 106: Item106 = LanguagePrimitives.ConvertTo<T106>(value); break;
                case 107: Item107 = LanguagePrimitives.ConvertTo<T107>(value); break;
                case 108: Item108 = LanguagePrimitives.ConvertTo<T108>(value); break;
                case 109: Item109 = LanguagePrimitives.ConvertTo<T109>(value); break;
                case 110: Item110 = LanguagePrimitives.ConvertTo<T110>(value); break;
                case 111: Item111 = LanguagePrimitives.ConvertTo<T111>(value); break;
                case 112: Item112 = LanguagePrimitives.ConvertTo<T112>(value); break;
                case 113: Item113 = LanguagePrimitives.ConvertTo<T113>(value); break;
                case 114: Item114 = LanguagePrimitives.ConvertTo<T114>(value); break;
                case 115: Item115 = LanguagePrimitives.ConvertTo<T115>(value); break;
                case 116: Item116 = LanguagePrimitives.ConvertTo<T116>(value); break;
                case 117: Item117 = LanguagePrimitives.ConvertTo<T117>(value); break;
                case 118: Item118 = LanguagePrimitives.ConvertTo<T118>(value); break;
                case 119: Item119 = LanguagePrimitives.ConvertTo<T119>(value); break;
                case 120: Item120 = LanguagePrimitives.ConvertTo<T120>(value); break;
                case 121: Item121 = LanguagePrimitives.ConvertTo<T121>(value); break;
                case 122: Item122 = LanguagePrimitives.ConvertTo<T122>(value); break;
                case 123: Item123 = LanguagePrimitives.ConvertTo<T123>(value); break;
                case 124: Item124 = LanguagePrimitives.ConvertTo<T124>(value); break;
                case 125: Item125 = LanguagePrimitives.ConvertTo<T125>(value); break;
                case 126: Item126 = LanguagePrimitives.ConvertTo<T126>(value); break;
                case 127: Item127 = LanguagePrimitives.ConvertTo<T127>(value); break;
                default: throw new ArgumentOutOfRangeException("index");
            }
        }

        public override int Capacity
        {
            get
            {
                return 128;
            }
        }
    }

    // *** END GENERATED CODE ***

    #endregion
}
