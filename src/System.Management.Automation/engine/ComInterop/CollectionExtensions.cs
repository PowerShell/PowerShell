// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace System.Management.Automation.ComInterop
{
    internal static class CollectionExtensions
    {
        internal static T[] RemoveFirst<T>(this T[] array)
        {
            T[] result = new T[array.Length - 1];
            Array.Copy(array, 1, result, 0, result.Length);
            return result;
        }

        internal static T[] AddFirst<T>(this IList<T> list, T item)
        {
            T[] res = new T[list.Count + 1];
            res[0] = item;
            list.CopyTo(res, 1);
            return res;
        }

        internal static T[] ToArray<T>(this IList<T> list)
        {
            T[] res = new T[list.Count];
            list.CopyTo(res, 0);
            return res;
        }

        internal static T[] AddLast<T>(this IList<T> list, T item)
        {
            T[] res = new T[list.Count + 1];
            list.CopyTo(res, 0);
            res[list.Count] = item;
            return res;
        }
    }
}
