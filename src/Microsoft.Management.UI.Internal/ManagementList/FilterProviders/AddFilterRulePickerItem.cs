// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The AddFilterRulePicker class is responsible for holding state
    /// information needed by the AddFilterRulePicker class.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class AddFilterRulePickerItem : INotifyPropertyChanged
    {
        private bool isChecked;

        /// <summary>
        /// Gets or sets a value indicating whether this item should
        /// be added to the FilterRulePanel.
        /// </summary>
        public bool IsChecked
        {
            get
            {
                return this.isChecked;
            }

            set
            {
                if (value != this.isChecked)
                {
                    this.isChecked = value;
                    this.NotifyPropertyChanged("IsChecked");
                }
            }
        }

        /// <summary>
        /// Gets the FilterRulePanelItem that will be added to the FilterRulePanel.
        /// </summary>
        public FilterRulePanelItem FilterRule
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of the FilterRulePanelItem class.
        /// </summary>
        /// <param name="filterRule">
        /// The FilterRulePanelItem that will be added to the FilterRulePanel.
        /// </param>
        public AddFilterRulePickerItem(FilterRulePanelItem filterRule)
        {
            this.FilterRule = filterRule;
        }

        /// <summary>
        /// Notifies listeners that a property has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #region NotifyPropertyChanged

        /// <summary>
        /// Notifies listeners that a property has changed.
        /// </summary>
        /// <param name="propertyName">
        /// The propertyName which has changed.
        /// </param>
        protected void NotifyPropertyChanged(string propertyName)
        {
            #pragma warning disable IDE1005 // IDE1005: Delegate invocation can be simplified.
            PropertyChangedEventHandler eh = this.PropertyChanged;

            if (eh != null)
            {
                eh(this, new PropertyChangedEventArgs(propertyName));
            }
            #pragma warning restore IDE1005
        }

        #endregion NotifyPropertyChanged
    }
}
