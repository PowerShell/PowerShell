// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Windows;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Routed event args which provide the ability to attach an
    /// arbitrary piece of data.
    /// </summary>
    /// <typeparam name="T">There are no restrictions on type T.</typeparam>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class DataRoutedEventArgs<T> : RoutedEventArgs
    {
        private T data;

        /// <summary>
        /// Constructs a new instance of the DataRoutedEventArgs class.
        /// </summary>
        /// <param name="data">The data payload to be stored.</param>
        /// <param name="routedEvent">The routed event.</param>
        public DataRoutedEventArgs(T data, RoutedEvent routedEvent)
        {
            this.data = data;
            this.RoutedEvent = routedEvent;
        }

        /// <summary>
        /// Gets a value containing the data being stored.
        /// </summary>
        public T Data
        {
            get { return this.data; }
        }
    }
}
