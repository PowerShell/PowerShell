//-----------------------------------------------------------------------
// <copyright file="FilterUtilities.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI.Internal
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Provides common utilities for filtering.
    /// </summary>
    internal static class FilterUtilities
    {
        internal static bool TryCastItem<T>(object item, out T castItem)
        {
            castItem = default(T);

            bool isItemUncastable = null == item && typeof(T).IsValueType;
            if (isItemUncastable)
            {
                return false;
            }

            bool shouldCastToString = null != item && typeof(string) == typeof(T);
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
