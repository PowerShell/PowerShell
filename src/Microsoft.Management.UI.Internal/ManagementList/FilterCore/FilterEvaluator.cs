// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The FilterEvaluator class is responsible for allowing the registeration of
    /// the FilterExpressionProviders and producing a FilterExpression composed of
    /// the FilterExpression returned from the providers.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public abstract class FilterEvaluator : IFilterExpressionProvider, INotifyPropertyChanged
    {
        #region Properties

        private Collection<IFilterExpressionProvider> filterExpressionProviders = new Collection<IFilterExpressionProvider>();

        /// <summary>
        /// Gets a readonly collection of the registered FilterExpressionProviders.
        /// </summary>
        public ReadOnlyCollection<IFilterExpressionProvider> FilterExpressionProviders
        {
            get
            {
                return new ReadOnlyCollection<IFilterExpressionProvider>(this.filterExpressionProviders);
            }
        }

        private FilterStatus filterStatus = FilterStatus.NotApplied;

        /// <summary>
        /// Gets a value indicating the status of the filter evaluation.
        /// </summary>
        public FilterStatus FilterStatus
        {
            get
            {
                return this.filterStatus;
            }

            protected set
            {
                this.filterStatus = value;
                this.NotifyPropertyChanged("FilterStatus");
            }
        }

        private bool startFilterOnExpressionChanged = true;

        /// <summary>
        /// Gets a value indicating the status of the filter evaluation.
        /// </summary>
        public bool StartFilterOnExpressionChanged
        {
            get
            {
                return this.startFilterOnExpressionChanged;
            }

            set
            {
                this.startFilterOnExpressionChanged = value;
                this.NotifyPropertyChanged("StartFilterOnExpressionChanged");
            }
        }

        private bool hasFilterExpression = false;

        /// <summary>
        /// Gets a value indicating whether this provider currently has a non-empty filter expression.
        /// </summary>
        public bool HasFilterExpression
        {
            get
            {
                return this.hasFilterExpression;
            }

            protected set
            {
                this.hasFilterExpression = value;
                this.NotifyPropertyChanged("HasFilterExpression");
            }
        }

        #endregion Properties

        #region Events

        /// <summary>
        /// Notifies listeners that a property has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Events

        #region Public Methods

        /// <summary>
        /// Applies the filter.
        /// </summary>
        public abstract void StartFilter();

        /// <summary>
        /// Stops the filter.
        /// </summary>
        public abstract void StopFilter();

        /// <summary>
        /// Returns a FilterExpression composed of FilterExpressions returned from the
        /// registered providers.
        /// </summary>
        /// <returns>
        /// The FilterExpression composed of FilterExpressions returned from the
        /// registered providers.
        /// </returns>
        public FilterExpressionNode FilterExpression
        {
            get
            {
                FilterExpressionAndOperatorNode andNode = new FilterExpressionAndOperatorNode();
                foreach (IFilterExpressionProvider provider in this.FilterExpressionProviders)
                {
                    FilterExpressionNode node = provider.FilterExpression;
                    if (node != null)
                    {
                        andNode.Children.Add(node);
                    }
                }

                return (andNode.Children.Count != 0) ? andNode : null;
            }
        }

        /// <summary>
        /// Adds a FilterExpressionProvider to the FilterEvaluator.
        /// </summary>
        /// <param name="provider">
        /// The provider to add.
        /// </param>
        public void AddFilterExpressionProvider(IFilterExpressionProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            this.filterExpressionProviders.Add(provider);
            provider.FilterExpressionChanged += this.FilterProvider_FilterExpressionChanged;
        }

        /// <summary>
        /// Removes a FilterExpressionProvider from the FilterEvaluator.
        /// </summary>
        /// <param name="provider">
        /// The provider to remove.
        /// </param>
        public void RemoveFilterExpressionProvider(IFilterExpressionProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            this.filterExpressionProviders.Remove(provider);
            provider.FilterExpressionChanged -= this.FilterProvider_FilterExpressionChanged;
        }

        #region NotifyPropertyChanged

        #pragma warning disable IDE1005 // IDE1005: Delegate invocation can be simplified.

        /// <summary>
        /// Notifies listeners that a property has changed.
        /// </summary>
        /// <param name="propertyName">
        /// The propertyName which has changed.
        /// </param>
        protected void NotifyPropertyChanged(string propertyName)
        {
            Debug.Assert(!string.IsNullOrEmpty(propertyName), "propertyName is not null");

            PropertyChangedEventHandler eh = this.PropertyChanged;

            if (eh != null)
            {
                eh(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion NotifyPropertyChanged

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Occurs when the filter expression has changed.
        /// </summary>
        public event EventHandler FilterExpressionChanged;

        /// <summary>
        /// Notifies any listeners that the filter expression has changed.
        /// </summary>
        protected virtual void NotifyFilterExpressionChanged()
        {
            EventHandler eh = this.FilterExpressionChanged;
            if (eh != null)
            {
                eh(this, new EventArgs());
            }
        }

        #pragma warning restore IDE1005

        private void FilterProvider_FilterExpressionChanged(object sender, EventArgs e)
        {
            // Update HasFilterExpression \\
            var hasFilterExpression = false;

            foreach (IFilterExpressionProvider provider in this.FilterExpressionProviders)
            {
                if (provider.HasFilterExpression)
                {
                    hasFilterExpression = true;
                    break;
                }
            }

            this.HasFilterExpression = hasFilterExpression;

            // Update FilterExpression \\
            this.NotifyFilterExpressionChanged();
            this.NotifyPropertyChanged("FilterExpression");

            // Start filtering if requested \\
            if (this.StartFilterOnExpressionChanged)
            {
                this.StartFilter();
            }
        }

        #endregion Private Methods
    }
}
