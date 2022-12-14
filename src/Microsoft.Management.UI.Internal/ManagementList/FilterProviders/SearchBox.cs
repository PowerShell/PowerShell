// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Microsoft.Management.UI.Internal
{
    /// <content>
    /// Partial class implementation for SearchBox control.
    /// </content>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public partial class SearchBox : Control, IFilterExpressionProvider
    {
        private SearchTextParser parser;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchBox"/> class.
        /// </summary>
        public SearchBox()
        {
            // This constructor intentionally left blank
        }

        #region IFilterExpressionProvider Implementation

        /// <summary>
        /// Gets the filter expression representing the current search text.
        /// </summary>
        public FilterExpressionNode FilterExpression
        {
            get
            {
                return SearchBox.ConvertToFilterExpression(this.Parser.Parse(this.Text));
            }
        }

        /// <summary>
        /// Gets a value indicating whether this provider currently has a non-empty filter expression.
        /// </summary>
        public bool HasFilterExpression
        {
            get
            {
                return string.IsNullOrEmpty(this.Text) == false;
            }
        }

        /// <summary>
        /// Occurs when the filter expression has changed.
        /// </summary>
        public event EventHandler FilterExpressionChanged;

        /// <summary>
        /// Notifies any listeners that the filter expression has changed.
        /// </summary>
        protected virtual void NotifyFilterExpressionChanged()
        {
            #pragma warning disable IDE1005 // IDE1005: Delegate invocation can be simplified.
            EventHandler eh = this.FilterExpressionChanged;
            if (eh != null)
            {
                eh(this, new EventArgs());
            }
            #pragma warning restore IDE1005s
        }

        #endregion

        /// <summary>
        /// Gets or sets the parser used to parse the search text.
        /// </summary>
        public SearchTextParser Parser
        {
            get
            {
                if (this.parser == null)
                {
                    this.parser = new SearchTextParser();
                }

                return this.parser;
            }

            set
            {
                ArgumentNullException.ThrowIfNull(value);

                this.parser = value;
            }
        }

        partial void OnTextChangedImplementation(PropertyChangedEventArgs<string> e)
        {
            this.NotifyFilterExpressionChanged();
        }

        partial void OnClearTextCanExecuteImplementation(CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.HasFilterExpression;
        }

        partial void OnClearTextExecutedImplementation(ExecutedRoutedEventArgs e)
        {
            this.Text = string.Empty;
        }

        /// <summary>
        /// Converts the specified collection of searchbox items to a filter expression.
        /// </summary>
        /// <param name="searchBoxItems">A collection of searchbox items to convert.</param>
        /// <returns>A filter expression.</returns>
        /// <exception cref="ArgumentNullException">The specified value is a null reference.</exception>
        protected static FilterExpressionNode ConvertToFilterExpression(ICollection<SearchTextParseResult> searchBoxItems)
        {
            ArgumentNullException.ThrowIfNull(searchBoxItems);

            if (searchBoxItems.Count == 0)
            {
                return null;
            }
            else
            {
                FilterExpressionAndOperatorNode filterExpression = new FilterExpressionAndOperatorNode();

                foreach (SearchTextParseResult item in searchBoxItems)
                {
                    filterExpression.Children.Add(new FilterExpressionOperandNode(item.FilterRule));
                }

                return filterExpression;
            }
        }
    }
}
