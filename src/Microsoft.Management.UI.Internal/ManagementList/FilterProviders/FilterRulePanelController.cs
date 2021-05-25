// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The FilterRulePanelController is responsible managing the addition and removal of
    /// <see cref="FilterRulePanelItems" />s to a <see cref="FilterRulePanel"/>.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class FilterRulePanelController : IFilterExpressionProvider
    {
        #region Properties

        private ObservableCollection<FilterRulePanelItem> filterRulePanelItems;
        private ReadOnlyObservableCollection<FilterRulePanelItem> readOnlyFilterRulePanelItems;

        /// <summary>
        /// Gets the collection of FilterRulePanelItems that are currently
        /// displayed in the panel.
        /// </summary>
        public ReadOnlyCollection<FilterRulePanelItem> FilterRulePanelItems
        {
            get { return this.readOnlyFilterRulePanelItems; }
        }

        /// <summary>
        /// Gets a FilterExpression representing the current
        /// relational organization of FilterRules for this provider.
        /// </summary>
        public FilterExpressionNode FilterExpression
        {
            get
            {
                return this.CreateFilterExpression();
            }
        }

        /// <summary>
        /// Gets a value indicating whether this provider currently has a non-empty filter expression.
        /// </summary>
        public bool HasFilterExpression
        {
            get
            {
                return this.FilterExpression != null;
            }
        }

        #endregion Properties

        #region Events

        /// <summary>
        /// Raised when a FilterRulePanelItem has been added or removed.
        /// </summary>
        public event EventHandler FilterExpressionChanged;

        #endregion Events

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the FilterRulePanelController class.
        /// </summary>
        public FilterRulePanelController()
        {
            this.filterRulePanelItems =
                new ObservableCollection<FilterRulePanelItem>();
            this.readOnlyFilterRulePanelItems =
                new ReadOnlyObservableCollection<FilterRulePanelItem>(this.filterRulePanelItems);
        }

        #endregion Ctor

        #region Public Methods

        /// <summary>
        /// Adds an item to the panel.
        /// </summary>
        /// <param name="item">
        /// The item to add.
        /// </param>
        public void AddFilterRulePanelItem(FilterRulePanelItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            int insertionIndex = this.GetInsertionIndex(item);
            this.filterRulePanelItems.Insert(insertionIndex, item);

            item.Rule.EvaluationResultInvalidated += this.Rule_EvaluationResultInvalidated;

            this.UpdateFilterRulePanelItemTypes();

            this.NotifyFilterExpressionChanged();
        }

        private void Rule_EvaluationResultInvalidated(object sender, EventArgs e)
        {
            this.NotifyFilterExpressionChanged();
        }

        /// <summary>
        /// Removes an item from the panel.
        /// </summary>
        /// <param name="item">
        /// The item to remove.
        /// </param>
        public void RemoveFilterRulePanelItem(FilterRulePanelItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            item.Rule.EvaluationResultInvalidated -= this.Rule_EvaluationResultInvalidated;

            this.filterRulePanelItems.Remove(item);
            this.UpdateFilterRulePanelItemTypes();

            this.NotifyFilterExpressionChanged();
        }

        /// <summary>
        /// Removes all items from the panel.
        /// </summary>
        public void ClearFilterRulePanelItems()
        {
            this.filterRulePanelItems.Clear();

            this.NotifyFilterExpressionChanged();
        }

        #endregion Public Methods

        #region Private Methods

        #region CreateFilterExpression

        private FilterExpressionNode CreateFilterExpression()
        {
            List<FilterExpressionNode> groupNodes = new List<FilterExpressionNode>();

            for (int i = 0; i < this.filterRulePanelItems.Count;)
            {
                int endIndex = this.GetExclusiveEndIndexForGroupStartingAt(i);

                FilterExpressionOrOperatorNode operatorOrNode = this.CreateFilterExpressionForGroup(i, endIndex);
                if (operatorOrNode.Children.Count > 0)
                {
                    groupNodes.Add(operatorOrNode);
                }

                i = endIndex;
            }

            if (groupNodes.Count == 0)
            {
                return null;
            }

            return new FilterExpressionAndOperatorNode(groupNodes);
        }

        private int GetExclusiveEndIndexForGroupStartingAt(int startIndex)
        {
            Debug.Assert(this.filterRulePanelItems.Count > 0, "greater than 0");
            Debug.Assert(startIndex >= 0, "greater than or equal to 0");

            int i = startIndex;
            for (; i < this.filterRulePanelItems.Count; i++)
            {
                if (i == startIndex)
                {
                    continue;
                }

                string currentId = this.filterRulePanelItems[i].GroupId;
                string previousId = this.filterRulePanelItems[i - 1].GroupId;

                if (!currentId.Equals(previousId, StringComparison.Ordinal))
                {
                    break;
                }
            }

            return i;
        }

        private FilterExpressionOrOperatorNode CreateFilterExpressionForGroup(int startIndex, int endIndex)
        {
            Debug.Assert(this.filterRulePanelItems.Count > 0, "greater than 0");
            Debug.Assert(startIndex >= 0, "greater than or equal to 0");
            Debug.Assert(this.filterRulePanelItems.Count >= endIndex, "greater than or equal to endIndex");

            FilterExpressionOrOperatorNode groupNode = new FilterExpressionOrOperatorNode();
            for (int i = startIndex; i < endIndex; i++)
            {
                FilterRule rule = this.filterRulePanelItems[i].Rule;
                if (rule.IsValid)
                {
                    groupNode.Children.Add(new FilterExpressionOperandNode(rule.DeepCopy()));
                }
            }

            return groupNode;
        }

        #endregion CreateFilterExpression

        #region Add/Remove Item Helpers

        private int GetInsertionIndex(FilterRulePanelItem item)
        {
            Debug.Assert(item != null, "not null");

            for (int i = this.filterRulePanelItems.Count - 1; i >= 0; i--)
            {
                string uniqueId = this.filterRulePanelItems[i].GroupId;
                if (uniqueId.Equals(item.GroupId, StringComparison.Ordinal))
                {
                    return i + 1;
                }
            }

            return this.filterRulePanelItems.Count;
        }

        private void UpdateFilterRulePanelItemTypes()
        {
            if (this.filterRulePanelItems.Count > 0)
            {
                this.filterRulePanelItems[0].ItemType = FilterRulePanelItemType.FirstHeader;
            }

            for (int i = 1; i < this.filterRulePanelItems.Count; i++)
            {
                string currentId = this.filterRulePanelItems[i].GroupId;
                string previousId = this.filterRulePanelItems[i - 1].GroupId;

                if (!currentId.Equals(previousId, StringComparison.Ordinal))
                {
                    this.filterRulePanelItems[i].ItemType = FilterRulePanelItemType.Header;
                }
                else
                {
                    this.filterRulePanelItems[i].ItemType = FilterRulePanelItemType.Item;
                }
            }
        }

        #endregion Add/Remove Item Helpers

        #region Notify Filter Expression Changed

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
            #pragma warning restore IDE1005
        }

        #endregion Notify Filter Expression Changed

        #endregion Private Methods
    }
}
