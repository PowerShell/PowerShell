// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Provides common utilities for filtering.
    /// </summary>
    internal static class FilterUtilities
    {
        internal static bool TryCastItem<T>(object item, out T castItem)
        {
            castItem = default(T);

            bool isItemUncastable = item == null && typeof(T).IsValueType;
            if (isItemUncastable)
            {
                return false;
            }

            bool shouldCastToString = item != null && typeof(string) == typeof(T);
            if (shouldCastToString)
            {
                // NOTE: string => T doesn't compile. We confuse the type system
                // and use string => object => T to make this work.
                object stringPropertyValue = item.ToString();
                castItem = (T)stringPropertyValue;
                return true;
            }

            try
            {
                castItem = (T)item;
                return true;
            }
            catch (InvalidCastException e)
            {
                Debug.Print(e.ToString());
            }

            return false;
        }
    }
}
