// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Represents a read-only ObservableCollection which also implement IAsyncProgress.
    /// </summary>
    /// <typeparam name="T">The type held by the collection.</typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class ReadOnlyObservableAsyncCollection<T> :
        ReadOnlyCollection<T>,
        IAsyncProgress,
        INotifyPropertyChanged, INotifyCollectionChanged
    {
        #region Private fields
        private IAsyncProgress asyncProgress;
        #endregion Private fields

        #region Constructors
        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="list">The collection with which to create this instance of the ReadOnlyObservableAsyncCollection class.
        /// The object must also implement IAsyncProgress, INotifyCollectionChanged and INotifyPropertyChanged.</param>
        public ReadOnlyObservableAsyncCollection(IList<T> list)
            : base(list)
        {
            this.asyncProgress = list as IAsyncProgress;

            ((INotifyCollectionChanged)this.Items).CollectionChanged += this.HandleCollectionChanged;
            ((INotifyPropertyChanged)this.Items).PropertyChanged += this.HandlePropertyChanged;
        }
        #endregion Constructors

        #region Events
        /// <summary>
        /// Occurs when the collection changes, either by adding or removing an item.
        /// </summary>
        /// <remarks>
        /// see <seealso cref="INotifyCollectionChanged"/>
        /// </remarks>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Occurs when a property changes.
        /// </summary>
        /// <remarks>
        /// see <seealso cref="INotifyPropertyChanged"/>
        /// </remarks>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion Events

        #region IAsyncProgress
        /// <summary>
        /// Gets a value indicating whether the async operation is currently running.
        /// </summary>
        public bool OperationInProgress
        {
            get
            {
                if (this.asyncProgress == null)
                {
                    return false;
                }
                else
                {
                    return this.asyncProgress.OperationInProgress;
                }
            }
        }

        /// <summary>
        /// Gets the error for the async operation.  This field is only valid if
        /// OperationInProgress is false.  null indicates there was no error.
        /// </summary>
        public Exception OperationError
        {
            get
            {
                if (this.asyncProgress == null)
                {
                    return null;
                }
                else
                {
                    return this.asyncProgress.OperationError;
                }
            }
        }
        #endregion IAsyncProgress

        #region Private Methods

        #pragma warning disable IDE1005 // IDE1005: Delegate invocation can be simplified.
        private void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            NotifyCollectionChangedEventHandler eh = this.CollectionChanged;

            if (eh != null)
            {
                eh(this, args);
            }
        }

        private void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChangedEventHandler eh = this.PropertyChanged;

            if (eh != null)
            {
                eh(this, args);
            }
        }

        #pragma warning restore IDE1005

        // forward CollectionChanged events from the base list to our listeners
        private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.OnCollectionChanged(e);
        }

        // forward PropertyChanged events from the base list to our listeners
        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.OnPropertyChanged(e);
        }
        #endregion Private Methods
    }
}
