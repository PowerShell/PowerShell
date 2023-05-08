// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Windows.Controls;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The ItemsControlFilterEvaluator class provides functionality to
    /// apply a filter against an ItemsControl.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class ItemsControlFilterEvaluator : FilterEvaluator
    {
        #region Properties

        private ItemsControl filterTarget;

        /// <summary>
        /// Gets or sets an ItemsControl which is
        /// the target for filtering.
        /// </summary>
        public ItemsControl FilterTarget
        {
            get
            {
                return this.filterTarget;
            }

            set
            {
                if (this.filterTarget != null)
                {
                    this.StopFilter();
                }

                this.filterTarget = value;
            }
        }

        private FilterExpressionNode CachedFilterExpression
        {
            get;
            set;
        }

        #endregion Properties

        #region Events

        /// <summary>
        /// Used to notify listeners that an unhandled exception has occurred while
        /// evaluating the filter.
        /// </summary>
        public event EventHandler<FilterExceptionEventArgs> FilterExceptionOccurred;

        #endregion Events

        #region Public Methods

        /// <summary>
        /// Applies the filter.
        /// </summary>
        public override void StartFilter()
        {
            if (this.FilterTarget == null)
            {
                throw new InvalidOperationException("FilterTarget is null.");
            }

            // Cache the expression for filtering so subsequent changes are ignored \\
            this.CachedFilterExpression = this.FilterExpression;

            if (this.CachedFilterExpression != null)
            {
                this.FilterTarget.Items.Filter = this.FilterExpressionAdapter;
                this.FilterStatus = FilterStatus.Applied;
            }
            else
            {
                this.StopFilter();
            }
        }

        /// <summary>
        /// Stops the filter.
        /// </summary>
        public override void StopFilter()
        {
            if (this.FilterTarget == null)
            {
                throw new InvalidOperationException("FilterTarget is null.");
            }

            // Only clear the filter if necessary, since clearing it causes sorting to be re-evaluated \\
            if (this.FilterTarget.Items.Filter != null)
            {
                this.FilterTarget.Items.Filter = null;
            }

            this.FilterStatus = FilterStatus.NotApplied;
        }

        #endregion Public Methods

        #region Private Methods

        private bool FilterExpressionAdapter(object item)
        {
            Debug.Assert(this.CachedFilterExpression != null, "not null");

            try
            {
                return this.CachedFilterExpression.Evaluate(item);
            }
            catch (Exception e)
            {
                if (!this.TryNotifyFilterException(e))
                {
                    throw;
                }
            }

            return false;
        }

        private bool TryNotifyFilterException(Exception e)
        {
            EventHandler<FilterExceptionEventArgs> eh = this.FilterExceptionOccurred;
            if (eh != null)
            {
                eh(this, new FilterExceptionEventArgs(e));
                return true;
            }

            return false;
        }

        #endregion Private Methods
    }
}
