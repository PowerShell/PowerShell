// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Management.UI.Internal
{
    using System;

    /// <summary>
    /// An EventArgs which holds the old and new values for a property change.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class PropertyChangedEventArgs<T> : EventArgs
    {
        /// <summary>
        /// Creates an instance of PropertyChangedEventArgs.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new, current, value.</param>
        public PropertyChangedEventArgs(T oldValue, T newValue)
        {
            this.OldValue = oldValue;
            this.NewValue = newValue;
        }

        /// <summary>
        /// Gets the previous value for the property.
        /// </summary>
        public T OldValue
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the new value for the property.
        /// </summary>
        public T NewValue
        {
            get;
            private set;
        }
    }
}
