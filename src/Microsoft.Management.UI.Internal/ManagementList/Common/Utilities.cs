// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Microsoft.Management.UI.Internal
{
    #region UserActionState enum

    /// <summary>
    /// Represents the availability of an action to a user.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public enum UserActionState
    {
        /// <summary>
        /// Indicates that the action is enabled and allowed.
        /// </summary>
        Enabled = 0,

        /// <summary>
        /// Indicates that the action is disabled.
        /// </summary>
        Disabled = 1,

        /// <summary>
        /// Indicates that the action is not visible.
        /// </summary>
        Hidden = 2,
    }

    #endregion

    #region ControlState enum

    /// <summary>
    /// Represents the ready-state of a control.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public enum ControlState
    {
        /// <summary>
        /// Indicates that the control is ready.
        /// </summary>
        Ready = 0,

        /// <summary>
        /// Indicates that the control has an error.
        /// </summary>
        Error = 1,

        /// <summary>
        /// Indicates that the control is refreshing its data.
        /// </summary>
        Refreshing = 2,
    }

    #endregion

    #region Utilities class

    /// <summary>
    /// Provides common methods for use in the library.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public static class Utilities
    {
        /// <summary>
        /// Gets whether all of the items in <paramref name="items"/> are of type T.
        /// </summary>
        /// <typeparam name="T">The type to verify.</typeparam>
        /// <param name="items">The items to check.</param>
        /// <returns>Whether all of the items in <paramref name="items"/> are of type T.</returns>
        /// <exception cref="ArgumentNullException">The specified value is a null reference.</exception>
        public static bool AreAllItemsOfType<T>(IEnumerable items)
        {
            ArgumentNullException.ThrowIfNull(items);

            foreach (object item in items)
            {
                if (item is not T)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Searches for an element that matches the specified type, and returns the first occurrence in the entire <see cref="IEnumerable"/>.
        /// </summary>
        /// <typeparam name="T">The type of the item to find.</typeparam>
        /// <param name="items">The <see cref="IEnumerable"/> to search.</param>
        /// <returns>The first element that matches the specified type, if found; otherwise, the default value for type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">The specified value is a null reference.</exception>
        public static T Find<T>(this IEnumerable items)
        {
            ArgumentNullException.ThrowIfNull(items);

            foreach (object item in items)
            {
                if (item is T)
                {
                    return (T)item;
                }
            }

            return default(T);
        }

        /// <summary>
        /// Method to trim the non null strings.
        /// </summary>
        /// <param name="value">String to Trim.</param>
        /// <returns>Trimmed string.</returns>
        public static string NullCheckTrim(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                return value.Trim();
            }

            return value;
        }

        // A separate copy of ResortObservableCollection is in ADMUX Utility.cs

        /// <summary>
        /// Restore the original order as far as possible.
        /// Columns not in the original set will appear at the end.
        /// </summary>
        /// <typeparam name="T">
        /// Type of <paramref name="modify"/>.
        /// </typeparam>
        /// <param name="modify">
        /// ObservableCollection to resort to order of
        /// <paramref name="sorted"/>.
        /// </param>
        /// <param name="sorted">
        /// Order to which <paramref name="modify"/> should be resorted.
        /// All enumerated objects must be of type T.
        /// </param>
        /// <remarks>
        /// Parameter <paramref name="sorted"/> is not generic to type T
        /// since it may be a collection of a subclass of type T,
        /// and IEnumerable'subclass is not compatible with
        /// IEnumerable'baseclass.
        /// </remarks>
        public static void ResortObservableCollection<T>(
            ObservableCollection<T> modify,
            IEnumerable sorted)
        {
            int orderedPosition = 0;
            foreach (T obj in sorted)
            {
                T sortedObject = (T)obj;
                int foundIndex = modify.IndexOf(sortedObject);
                if (foundIndex >= 0)
                {
                    modify.Move(foundIndex, orderedPosition);
                    orderedPosition++;
                    if (modify.Count <= orderedPosition)
                    {
                        // All objects present are in the original order
                        break;
                    }
                }
            }
        }
    }

    #endregion
}
