// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// ReSharper disable UnusedMember.Global

using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;

namespace System.Management.Automation
{
    internal static class ArrayOps
    {
        internal static object AddObjectArray(object[] lhs, object rhs)
        {
            int newIdx = lhs.Length;
            Array.Resize(ref lhs, newIdx + 1);
            lhs[newIdx] = rhs;

            return lhs;
        }

        internal static object[] SlicingIndex(object target, object[] indexes, Func<object, object, object> indexer)
        {
            var result = new object[indexes.Length];
            int j = 0;
            foreach (object t in indexes)
            {
                var value = indexer(target, t);
                if (value != AutomationNull.Value)
                {
                    result[j++] = value;
                }
            }

            if (j != indexes.Length)
            {
                var shortResult = new object[j];
                Array.Copy(result, shortResult, j);
                return shortResult;
            }

            return result;
        }

        /// <summary>
        /// Efficiently multiplies collection by integer.
        /// </summary>
        /// <param name="array">Collection to multiply.</param>
        /// <param name="times">Number of times the collection is to be multiplied/copied.</param>
        /// <returns>Collection multiplied by integer.</returns>
        internal static T[] Multiply<T>(T[] array, uint times)
        {
            Diagnostics.Assert(array != null, "Caller should verify the arguments for array multiplication");

            if (times == 1)
            {
                return array;
            }

            if (times == 0 || array.Length == 0)
            {
#pragma warning disable CA1825 // Avoid zero-length array allocations
                // Don't use Array.Empty<T>(); always return a new instance.
                return new T[0];
#pragma warning restore CA1825 // Avoid zero-length array allocations
            }

            var context = LocalPipeline.GetExecutionContextFromTLS();
            if (context != null &&
                context.LanguageMode == PSLanguageMode.RestrictedLanguage && (array.Length * times) > 1024)
            {
                throw InterpreterError.NewInterpreterException(times, typeof(RuntimeException),
                    null, "ArrayMultiplyToolongInDataSection", ParserStrings.ArrayMultiplyToolongInDataSection, 1024);
            }

            var uncheckedLength = array.Length * times;
            int elements = -1;
            try
            {
                elements = checked((int)uncheckedLength);
            }
            catch (OverflowException)
            {
                LanguagePrimitives.ThrowInvalidCastException(uncheckedLength, typeof(int));
            }

            // Make the minimum number of calls to Array.Copy by doubling the array up to
            // the most significant bit in times, then do one final Array.Copy to get the
            // remaining copies.

            T[] result = new T[elements];
            int resultLength = array.Length;
            Array.Copy(array, 0, result, 0, resultLength);
            times >>= 1;
            while (times != 0)
            {
                Array.Copy(result, 0, result, resultLength, resultLength);
                resultLength *= 2;
                times >>= 1;
            }

            if (result.Length != resultLength)
            {
                Array.Copy(result, 0, result, resultLength, (result.Length - resultLength));
            }

            return result;
        }

        internal static object GetMDArrayValue(Array array, int[] indexes, bool slicing)
        {
            if (array.Rank != indexes.Length)
            {
                ReportIndexingError(array, indexes, null);
            }

            for (int i = 0; i < indexes.Length; ++i)
            {
                int ub = array.GetUpperBound(i);
                int lb = array.GetLowerBound(i);
                if (indexes[i] < lb)
                {
                    indexes[i] = indexes[i] + ub + 1;
                }

                if (indexes[i] < lb || indexes[i] > ub)
                {
                    // In strict mode, don't return, fall through and let Array.GetValue raise an exception.
                    var context = LocalPipeline.GetExecutionContextFromTLS();
                    if (context != null && !context.IsStrictVersion(3))
                    {
                        // If we're slicing, return AutomationNull.Value to signal no result)
                        return slicing ? AutomationNull.Value : null;
                    }
                }
            }

            // All indexes have been validated, so this won't raise an exception.
            return array.GetValue(indexes);
        }

        internal static object GetMDArrayValueOrSlice(Array array, object indexes)
        {
            Exception whyFailed = null;
            int[] indexArray = null;
            try
            {
                indexArray = (int[])LanguagePrimitives.ConvertTo(indexes, typeof(int[]), NumberFormatInfo.InvariantInfo);
            }
            catch (InvalidCastException ice)
            {
                // Ignore an exception here as we may actually be looking at an array of arrays
                // which could still be ok. Save the exception as we may use it later...
                whyFailed = ice;
            }

            if (indexArray != null)
            {
                if (indexArray.Length != array.Rank)
                {
                    // rank failed to match so error...
                    ReportIndexingError(array, indexes, null);
                }

                return GetMDArrayValue(array, indexArray, false);
            }

            var indexList = new List<int[]>();

            var ie = LanguagePrimitives.GetEnumerator(indexes);
            while (EnumerableOps.MoveNext(null, ie))
            {
                var currentIndex = EnumerableOps.Current(ie);
                try
                {
                    indexArray = LanguagePrimitives.ConvertTo<int[]>(currentIndex);
                }
                catch (InvalidCastException)
                {
                    indexArray = null;
                }

                if (indexArray == null || indexArray.Length != array.Rank)
                {
                    if (whyFailed != null)
                    {
                        // If the first fails, report the original exception and all indices
                        ReportIndexingError(array, indexes, whyFailed);
                        Diagnostics.Assert(false, "ReportIndexingError must throw");
                    }
                    // If the second or subsequent index fails, report the failure for just that index
                    ReportIndexingError(array, currentIndex, null);
                    Diagnostics.Assert(false, "ReportIndexingError must throw");
                }

                // Only use whyFailed the first time through, otherwise
                whyFailed = null;
                indexList.Add(indexArray);
            }

            // Optimistically assume all indices are valid so the result array is the same size.
            // If that turns out to be wrong, we'll just copy the elements produced.
            var result = new object[indexList.Count];
            int j = 0;
            foreach (var i in indexList)
            {
                var value = GetMDArrayValue(array, i, true);
                if (value != AutomationNull.Value)
                {
                    result[j++] = value;
                }
            }

            if (j != indexList.Count)
            {
                var shortResult = new object[j];
                Array.Copy(result, shortResult, j);
                return shortResult;
            }

            return result;
        }

        private static void ReportIndexingError(Array array, object index, Exception reason)
        {
            // Convert this index into something printable (we hope)...
            string msgString = IndexStringMessage(index);

            if (reason == null)
            {
                throw InterpreterError.NewInterpreterException(index, typeof(RuntimeException), null,
                    "NeedMultidimensionalIndex", ParserStrings.NeedMultidimensionalIndex, array.Rank, msgString);
            }

            throw InterpreterError.NewInterpreterExceptionWithInnerException(index, typeof(RuntimeException), null,
                "NeedMultidimensionalIndex", ParserStrings.NeedMultidimensionalIndex, reason, array.Rank, msgString);
        }

        internal static string IndexStringMessage(object index)
        {
            // Convert this index into something printable (we hope)...
            string msgString = PSObject.ToString(null, index, ",", null, null, true, true);
            if (msgString.Length > 20)
                msgString = string.Concat(msgString.AsSpan(0, 20), " ...");
            return msgString;
        }

        internal static object SetMDArrayValue(Array array, int[] indexes, object value)
        {
            if (array.Rank != indexes.Length)
            {
                ReportIndexingError(array, indexes, null);
            }

            for (int i = 0; i < indexes.Length; ++i)
            {
                int ub = array.GetUpperBound(i);
                int lb = array.GetLowerBound(i);
                if (indexes[i] < lb)
                {
                    indexes[i] = indexes[i] + ub + 1;
                }
            }

            array.SetValue(value, indexes);
            return value;
        }

        internal static object GetNonIndexable(object target, object[] indices)
        {
            // We want to allow:
            //     $x[0]
            // and
            //     $x[-1]
            // to be the same as
            //     $x
            // But disallow anything else:
            //     if in the strict mode, throw exception
            //     otherwise, return AutomationNull.Value to signal no result

            if (indices.Length == 1)
            {
                var index = indices[0];
                if (index != null && (LanguagePrimitives.Equals(0, index) || LanguagePrimitives.Equals(-1, index)))
                {
                    return target;
                }
            }

            var context = LocalPipeline.GetExecutionContextFromTLS();
            if (context == null || !context.IsStrictVersion(2))
            {
                return AutomationNull.Value;
            }

            throw InterpreterError.NewInterpreterException(target, typeof(RuntimeException), null, "CannotIndex",
                                                           ParserStrings.CannotIndex, target.GetType());
        }
    }
}
