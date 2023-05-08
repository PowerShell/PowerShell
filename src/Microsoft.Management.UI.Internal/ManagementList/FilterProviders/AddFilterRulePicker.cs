// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The AddFilterRulePicker class is responsible for allowing users to
    /// add rules to an FilterRulePanel.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public partial class AddFilterRulePicker : Control
    {
        #region Properties

        private ObservableCollection<AddFilterRulePickerItem> shortcutFilterRules = new ObservableCollection<AddFilterRulePickerItem>();

        /// <summary>
        /// Gets the collection of shortcut rules available for addition to the FilterRulePanel.
        /// </summary>
        public ObservableCollection<AddFilterRulePickerItem> ShortcutFilterRules
        {
            get
            {
                return this.shortcutFilterRules;
            }
        }

        private ObservableCollection<AddFilterRulePickerItem> columnFilterRules = new ObservableCollection<AddFilterRulePickerItem>();

        /// <summary>
        /// Gets the collection of column rules available for addition to the FilterRulePanel.
        /// </summary>
        public ObservableCollection<AddFilterRulePickerItem> ColumnFilterRules
        {
            get
            {
                return this.columnFilterRules;
            }
        }

        #endregion Properties

        #region Private Methods

        partial void OnOkAddFilterRulesCanExecuteImplementation(System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (this.AddFilterRulesCommand != null)
                ? CommandHelper.CanExecuteCommand(this.AddFilterRulesCommand, null, this.AddFilterRulesCommandTarget)
                : false;
        }

        partial void OnOkAddFilterRulesExecutedImplementation(System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            this.IsOpen = false;

            Collection<FilterRulePanelItem> addedRules = new Collection<FilterRulePanelItem>();

            foreach (AddFilterRulePickerItem item in this.shortcutFilterRules)
            {
                if (item.IsChecked)
                {
                    addedRules.Add(item.FilterRule);
                    item.IsChecked = false;
                }
            }

            foreach (AddFilterRulePickerItem item in this.columnFilterRules)
            {
                if (item.IsChecked)
                {
                    addedRules.Add(item.FilterRule);
                    item.IsChecked = false;
                }
            }

            if (addedRules.Count > 0)
            {
                CommandHelper.ExecuteCommand(this.AddFilterRulesCommand, addedRules, this.AddFilterRulesCommandTarget);
            }
        }

        partial void OnCancelAddFilterRulesExecutedImplementation(System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            this.IsOpen = false;

            foreach (AddFilterRulePickerItem item in this.shortcutFilterRules)
            {
                item.IsChecked = false;
            }

            foreach (AddFilterRulePickerItem item in this.columnFilterRules)
            {
                item.IsChecked = false;
            }
        }

        #endregion Private Methods
    }
}
