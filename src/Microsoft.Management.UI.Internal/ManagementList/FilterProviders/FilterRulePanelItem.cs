// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The FilterRulePanelItem class maintains the state for a row item within a <see cref="FilterRulePanel"/>.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class FilterRulePanelItem : INotifyPropertyChanged
    {
        #region Properties

        /// <summary>
        /// Gets a FilterRule that is stored in this FilterRulePanelItem.
        /// </summary>
        public FilterRule Rule
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a string that identifies which group this
        /// item belongs to.
        /// </summary>
        public string GroupId
        {
            get;
            private set;
        }

        private FilterRulePanelItemType itemType = FilterRulePanelItemType.Header;

        /// <summary>
        /// Gets the type of FilterRulePanelItemType.
        /// </summary>
        public FilterRulePanelItemType ItemType
        {
            get
            {
                return this.itemType;
            }

            protected internal set
            {
                if (value == this.itemType)
                {
                    return;
                }

                this.itemType = value;
                this.NotifyPropertyChanged("ItemType");
            }
        }

        /// <summary>
        /// Notifies listeners that a property has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Properties

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the FilterRulePanelItem class.
        /// </summary>
        /// <param name="rule">
        /// The FilterRule to store in this FilterRulePanelItem.
        /// </param>
        /// <param name="groupId">
        /// A string which identifies which group this
        /// item belongs to.
        /// </param>
        public FilterRulePanelItem(FilterRule rule, string groupId)
        {
            ArgumentNullException.ThrowIfNull(rule);

            ArgumentException.ThrowIfNullOrEmpty(groupId);

            this.Rule = rule;
            this.GroupId = groupId;
        }

        #endregion Ctor

        #region Public Methods

        /// <summary>
        /// Notifies listeners that a property has changed.
        /// </summary>
        /// <param name="propertyName">
        /// The name of a property that has changed.
        /// </param>
        protected void NotifyPropertyChanged(string propertyName)
        {
            Debug.Assert(!string.IsNullOrEmpty(propertyName), "not null");

            #pragma warning disable IDE1005 // IDE1005: Delegate invocation can be simplified.

            PropertyChangedEventHandler eh = this.PropertyChanged;
            if (eh != null)
            {
                eh(this, new PropertyChangedEventArgs(propertyName));
            }

            #pragma warning restore IDE1005s
        }

        #endregion Public Methods
    }
}
