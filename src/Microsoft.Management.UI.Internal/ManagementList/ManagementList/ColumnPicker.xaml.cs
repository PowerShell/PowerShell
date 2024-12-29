// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Interaction logic for ColumnPicker.xaml.
    /// </summary>
    /// <remarks>
    /// The logic for manipulating the column lists is in
    /// <see cref="InnerListGridView.OnColumnPicker"/>.
    /// </remarks>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public partial class ColumnPicker : Window
    {
        private ObservableCollection<InnerListColumn> notSelectedColumns = new ObservableCollection<InnerListColumn>();
        private ObservableCollection<InnerListColumn> selectedColumns = new ObservableCollection<InnerListColumn>();

        #region constructor
        /// <summary>
        /// Default Constructor.
        /// </summary>
        internal ColumnPicker()
        {
            this.InitializeComponent();
            CollectionViewSource view =
                (CollectionViewSource)this.Resources["SortedAvailableColumns"];
            view.Source = this.notSelectedColumns;
            this.PART_SelectedList.ItemsSource = this.selectedColumns;
        }

        /// <summary>
        /// Constructor which initializes lists.
        /// </summary>
        /// <param name="columns">Initially selected columns.</param>
        /// <param name="availableColumns">
        /// All initial columns, if these include any which are selected
        /// these are excluded.
        /// </param>
        /// <remarks>
        /// It is not sufficient to just get
        /// <paramref name="availableColumns"/>, since this does not
        /// communicate the current ordering of visible columns.
        /// </remarks>
        internal ColumnPicker(
            ICollection<GridViewColumn> columns,
            ICollection<InnerListColumn> availableColumns)
            : this()
        {
            ArgumentNullException.ThrowIfNull(columns);
            
            ArgumentNullException.ThrowIfNull(availableColumns);

            // Add visible columns to Selected list, preserving order
            // Note that availableColumns is not necessarily in the order
            //  in which columns are currently displayed.
            foreach (InnerListColumn column in columns)
            {
                Debug.Assert(availableColumns.Contains(column), "all visible columns should be in availableColumns");
                Debug.Assert(column.Visible, "all visible columns should have Visible==true");

                this.SelectedColumns.Add(column);
            }

            foreach (InnerListColumn column in availableColumns)
            {
                Debug.Assert(column.Visible == columns.Contains(column), "exactly visible columns should have Visible==true");

                // only add columns not in selected list
                if (!columns.Contains(column))
                {
                    Debug.Assert(!column.Required, "Required columns should be visible");
                    this.NotSelectedColumns.Add(column);
                }
            }

            if (this.SelectedColumns.Count > 0)
            {
                this.PART_SelectedList.SelectedIndex = 0;
            }

            if (this.NotSelectedColumns.Count > 0)
            {
                this.PART_NotSelectedList.SelectedIndex = 0;
            }

            // If we don't do this, the last call to OnSelectionChanged
            // may have been made while there was only one column
            // (after the first column was added).
            this.OnSelectionChanged();
        }
        #endregion constructor

        #region properties
        /// <summary>
        /// Gets the columns in "Selected columns" list.
        /// </summary>
        internal ObservableCollection<InnerListColumn> SelectedColumns
        {
            get
            {
                return this.selectedColumns;
            }
        }

        /// <summary>
        /// Gets the columns in "Available columns" list.
        /// </summary>
        internal ObservableCollection<InnerListColumn> NotSelectedColumns
        {
            get
            {
                return this.notSelectedColumns;
            }
        }
        #endregion properties

        #region button clicks

        /// <summary>
        /// OK button was clicked.
        /// </summary>
        /// <param name="sender">OK button.</param>
        /// <param name="e">The RoutedEventArgs.</param>
        internal void OkButtonClick(object sender, RoutedEventArgs e)
        {
            foreach (InnerListColumn column in this.NotSelectedColumns)
            {
                column.Visible = false;
            }

            foreach (InnerListColumn column in this.SelectedColumns)
            {
                column.Visible = true;
            }

            this.DialogResult = true; // close the dialog
        }

        /// <summary>
        /// Move Up button was clicked.
        /// </summary>
        /// <param name="sender">Move Up button.</param>
        /// <param name="e">The RoutedEventArgs.</param>
        /// <remarks>
        /// Moving the selected item in the bound collection does not
        /// trigger the SelectionChanged event in the listbox.
        /// </remarks>
        internal void MoveUpButtonClick(object sender, RoutedEventArgs e)
        {
            int selectedIndex = this.PART_SelectedList.SelectedIndex;
            Debug.Assert(selectedIndex > 0, "Cannot move past top");
            this.SelectedColumns.Move(selectedIndex, selectedIndex - 1);

            // Moving the selected item in the bound collection does not
            // trigger the SelectionChanged event in the listbox,
            // so we call OnSelectionChanged explicitly.
            this.OnSelectionChanged();
        }

        /// <summary>
        /// Move Down button was clicked.
        /// </summary>
        /// <param name="sender">Move Down button.</param>
        /// <param name="e">The RoutedEventArgs.</param>
        internal void MoveDownButtonClick(object sender, RoutedEventArgs e)
        {
            int selectedIndex = this.PART_SelectedList.SelectedIndex;
            Debug.Assert(this.SelectedColumns.Count > selectedIndex + 1, "Cannot move past bottom");
            this.SelectedColumns.Move(selectedIndex, selectedIndex + 1);

            // Moving the selected item in the bound collection does not
            // trigger the SelectionChanged event in the listbox,
            // so we call OnSelectionChanged explicitly.
            this.OnSelectionChanged();
        }

        /// <summary>
        /// Add button was clicked.
        /// </summary>
        /// <param name="sender">Add button.</param>
        /// <param name="e">The RoutedEventArgs.</param>
        internal void AddButtonClick(object sender, RoutedEventArgs e)
        {
            InnerListColumn column = (InnerListColumn)this.PART_NotSelectedList.SelectedItem;
            Debug.Assert(column != null, "not null");

            this.SelectedColumns.Add(column);
            this.NotSelectedColumns.Remove(column);

            // The next item in the NotSelected list
            // is automatically selected.

            // select new item in Selected list
            this.PART_SelectedList.SelectedItem = column;

            // Just to make sure, we call OnSelectionChanged
            // explicitly here as well.
            this.OnSelectionChanged();
        }

        /// <summary>
        /// Remove button was clicked.
        /// </summary>
        /// <param name="sender">Remove button.</param>
        /// <param name="e">The RoutedEventArgs.</param>
        /// <remarks>
        /// Note that we do not attempt to maintain the ordering of items
        /// in the NotSelected list when they are removed and then added back.
        /// In the current implementation, the View of the NotSelected list is
        /// sorted by name through the CollectionViewSource.
        /// </remarks>
        internal void RemoveButtonClick(object sender, RoutedEventArgs e)
        {
            InnerListColumn column = (InnerListColumn)this.PART_SelectedList.SelectedItem;
            Debug.Assert(column != null, "not null");
            int selectedIndex = this.PART_SelectedList.SelectedIndex;
            Debug.Assert(selectedIndex >= 0, "greater than or equal to 0");

            this.NotSelectedColumns.Add(column);
            this.SelectedColumns.Remove(column);

            // Without this, there is no selection after the item is removed.
            if (selectedIndex < this.SelectedColumns.Count)
            {   // Select next item in Selected list
                // Note that we select the next item based on the index
                // in the View rather than the ViewModel.
                this.PART_SelectedList.SelectedIndex = selectedIndex;
            }
            else if (selectedIndex > 0)
            {   // Highest-index item removed, select previous item
                Debug.Assert((selectedIndex - 1) < this.SelectedColumns.Count, "less than count");
                this.PART_SelectedList.SelectedIndex = selectedIndex - 1;
            }   // otherwise there are no more items to select

            // select new item in NotSelected list
            this.PART_NotSelectedList.SelectedItem = column;

            // Just to make sure, we call OnSelectionChanged
            // explicitly here as well.
            this.OnSelectionChanged();
        }
        #endregion button clicks

        #region Automation
        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>).
        /// </summary>
        /// <returns>New AutomationPeer.</returns>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ExtendedFrameworkElementAutomationPeer(this, AutomationControlType.Window);
        }
        #endregion Automation

        #region enable/disable buttons
        /// <summary>
        /// The selection changed in either the Selected or NotSelected list.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The eventargs.</param>
        private void ListSelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            this.OnSelectionChanged();
        }

        /// <summary>
        /// Update which buttons are enabled based on current selection,
        /// also whether RequiredColumnText or LastColumnText
        /// should be visible.
        /// </summary>
        private void OnSelectionChanged()
        {
            Selector selectedList = (Selector)this.FindName(
                "PART_SelectedList");
            Selector notSelectedList = (Selector)this.FindName(
                "PART_NotSelectedList");

            this.AddButton.IsEnabled = notSelectedList.SelectedIndex >= 0;

            int selectedIndex = selectedList.SelectedIndex;
            bool selectionValid = selectedIndex >= 0;
            this.MoveUpButton.IsEnabled = selectedIndex > 0;
            this.MoveDownButton.IsEnabled = selectionValid && this.SelectedColumns.Count > selectedIndex + 1;

            bool hasOneColumn = this.SelectedColumns.Count < 2;
            bool requiredColumn = selectionValid &&
                this.SelectedColumns[selectedIndex].Required;
            this.RemoveButton.IsEnabled = selectionValid && !requiredColumn && !hasOneColumn;

            this.RequiredColumnText.Visibility = requiredColumn ? Visibility.Visible : Visibility.Hidden;
            this.LastColumnText.Visibility = (hasOneColumn && !requiredColumn) ? Visibility.Visible : Visibility.Hidden;
        }
        #endregion enable/disable buttons

        /// <summary>
        /// Handles mouse double-click of items in
        /// <see cref="PART_NotSelectedList"/>.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The eventargs.</param>
        private void NotSelectedList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Ignore right-double-click, and also ignore cases where
            // the add button is disabled (which really shouldn't happen).
            if (e.ChangedButton != MouseButton.Left || !this.AddButton.IsEnabled)
            {
                return;
            }

            // Ignore double-clicks on the listbox whitespace
            if (this.PART_NotSelectedList.ContainerFromElement((DependencyObject)e.OriginalSource) == null)
            {
                return;
            }

            // We rely on the behavior where double-click implicitly
            // selects a list box item.
            this.AddButtonClick(sender, e);
        }

        /// <summary>
        /// Handles mouse double-click of items in
        /// <see cref="PART_SelectedList"/>.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The eventargs.</param>
        private void SelectedList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Ignore right-double-click, and also ignore cases where
            // the remove button is disabled.  This can happen,
            // see OnSelectionChanged for details.
            if (e.ChangedButton != MouseButton.Left || !this.RemoveButton.IsEnabled)
            {
                return;
            }

            // Ignore double-clicks on the listbox whitespace
            if (this.PART_SelectedList.ContainerFromElement((DependencyObject)e.OriginalSource) == null)
            {
                return;
            }

            // We rely on the behavior where double-click implicitly
            // selects a list box item.
            this.RemoveButtonClick(sender, e);
        }
    }
}
