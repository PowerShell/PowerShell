// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Microsoft.Management.UI.Internal
{
    /// <content>
    /// Partial class implementation for InnerList control.
    /// </content>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    public partial class InnerList : System.Windows.Controls.ListView
    {
        #region internal fields
        #region StyleCop Suppression - generated code
        /// <summary>
        /// The current ICollectionView being displayed.
        /// </summary>
        internal ICollectionView CollectionView;

        #endregion StyleCop Suppression - generated code
        #endregion internal fields

        #region private fields
        /// <summary>
        /// The current GridView.
        /// </summary>
        private InnerListGridView innerGrid;

        private InnerListColumn sortedColumn;

        /// <summary>
        /// ContextMenu for InnerList columns.
        /// </summary>
        private ContextMenu contextMenu;

        /// <summary>
        /// Private setter for <see cref="Columns"/>.
        /// </summary>
        private ObservableCollection<InnerListColumn> columns = new ObservableCollection<InnerListColumn>();

        /// <summary>
        /// Gets or sets whether the current items source is non-null and has items.
        /// </summary>
        private bool itemsSourceIsEmpty = false;

        #endregion private fields

        #region constructors

        /// <summary>
        /// Initializes a new instance of this control.
        /// </summary>
        public InnerList()
            : base()
        {
            // This flag is needed to dramatically increase performance of scrolling \\
            VirtualizingStackPanel.SetVirtualizationMode(this, VirtualizationMode.Recycling);

            AutomationProperties.SetAutomationId(this, "InnerList"); // No localization needed
        }
        #endregion constructors

        #region Events

        /// <summary>
        ///  Register PropertyChangedEventHandler ItemSourcesPropertyChanged .
        /// </summary>
        public event PropertyChangedEventHandler ItemSourcesPropertyChanged;
        #endregion Events

        #region public properties

        /// <summary>
        /// Gets ItemsSource instead.
        /// <seealso cref="InnerList"/> Does not support adding to Items.
        /// </summary>
        [Browsable(false)]
        public new ItemCollection Items
        {
            get
            {
                return base.Items;
            }
        }

        /// <summary>
        /// Gets the column that is sorted, or <c>null</c> if no column is sorted.
        /// </summary>
        public InnerListColumn SortedColumn
        {
            get
            {
                return this.sortedColumn;
            }
        }

        /// <summary>
        /// Gets InnerListGridView.
        /// </summary>
        public InnerListGridView InnerGrid
        {
            get { return this.innerGrid; }
            protected set { this.innerGrid = value; }
        }

        /// <summary>
        /// Gets the collection of columns that this list should display.
        /// </summary>
        public ObservableCollection<InnerListColumn> Columns
        {
            get { return this.columns; }
        }

        #endregion public properties

        #region public methods

        /// <summary>
        /// Causes the object to scroll into view.
        /// </summary>
        /// <param name="item">Object to scroll.</param>
        /// <remarks>This method overrides ListBox.ScrollIntoView(), which throws NullReferenceException when VirtualizationMode is set to Recycling.
        /// This implementation uses a workaround recommended by the WPF team.</remarks>
        public new void ScrollIntoView(object item)
        {
            Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                (DispatcherOperationCallback)((arg) =>
                {
                    if (this.IsLoaded)
                    {
                        base.ScrollIntoView(arg);
                    }

                    return null;
                }),
                item);
        }

        /// <summary>
        /// Causes the object to scroll into view from the top, so that it tends to appear at the bottom of the scroll area.
        /// </summary>
        /// <param name="item">Object to scroll.</param>
        public void ScrollIntoViewFromTop(object item)
        {
            if (this.Items.Count > 0)
            {
                this.ScrollIntoView(this.Items[0]);
                this.ScrollIntoView(item);
            }
        }

        /// <summary>
        /// Updates the InnerGrid based upon the columns collection.
        /// </summary>
        public void RefreshColumns()
        {
            this.UpdateView(this.ItemsSource);
        }

        /// <summary>
        /// Sorts the list by the specified column. This has no effect if the list does not have a data source.
        /// </summary>
        /// <param name="column">
        /// The column to sort
        /// </param>
        /// <param name="shouldScrollIntoView">
        /// Indicates whether the SelectedItem should be scrolled into view.
        /// </param>
        /// <exception cref="ArgumentNullException">The specified value is a null reference.</exception>
        public void ApplySort(InnerListColumn column, bool shouldScrollIntoView)
        {
            ArgumentNullException.ThrowIfNull(column);

            // NOTE : By setting the column here, it will be used
            // later to set the sorted column when the UI state
            // is ready.
            this.sortedColumn = column;

            // If the list hasn't been populated, don't do anything \\
            if (this.CollectionView == null)
            {
                return;
            }

            this.UpdatePrimarySortColumn();

            using (this.CollectionView.DeferRefresh())
            {
                ListCollectionView lcv = (ListCollectionView)this.CollectionView;
                lcv.CustomSort = new PropertyValueComparer(this.GetDescriptionsForSorting(), true, FilterRuleCustomizationFactory.FactoryInstance.PropertyValueGetter);
            }

            if (shouldScrollIntoView && this.SelectedIndex > 0)
            {
                this.ScrollIntoView(this.SelectedItem);
            }
        }

        private void UpdatePrimarySortColumn()
        {
            foreach (InnerListColumn column in this.InnerGrid.AvailableColumns)
            {
                bool isPrimarySortColumn = object.ReferenceEquals(this.sortedColumn, column);

                InnerList.SetIsPrimarySortColumn(column, isPrimarySortColumn);
                InnerList.SetIsPrimarySortColumn((GridViewColumnHeader)column.Header, isPrimarySortColumn);
            }
        }

        /// <summary>
        /// Gets a list of data descriptions for the columns that are not the primary sort column.
        /// </summary>
        /// <returns>A list of data descriptions for the columns that are not the primary sort column.</returns>
        private List<UIPropertyGroupDescription> GetDescriptionsForSorting()
        {
            List<UIPropertyGroupDescription> dataDescriptions = new List<UIPropertyGroupDescription>();

            dataDescriptions.Add(this.SortedColumn.DataDescription);

            foreach (InnerListColumn column in this.InnerGrid.Columns)
            {
                if (!object.ReferenceEquals(this.SortedColumn, column))
                {
                    dataDescriptions.Add(column.DataDescription);
                }
            }

            return dataDescriptions;
        }

        /// <summary>
        /// Clears the sort order from the list.
        /// </summary>
        public void ClearSort()
        {
            if (this.CollectionView == null)
            {
                return;
            }

            using (this.CollectionView.DeferRefresh())
            {
                this.sortedColumn = null;
                ListCollectionView lcv = (ListCollectionView)this.CollectionView;
                lcv.CustomSort = null;
            }

            // If columns are shown, update them to show none are sorted \\
            if (this.InnerGrid != null)
            {
                this.UpdatePrimarySortColumn();
            }
        }

        #endregion public methods

        #region internal methods

        #endregion internal methods

        #region protected methods
        /// <summary>
        /// Called when the ItemsSource changes to set internal fields, subscribe to the view change
        /// and possibly autopopulate columns.
        /// </summary>
        /// <param name="oldValue">Previous ItemsSource.</param>
        /// <param name="newValue">Current ItemsSource.</param>
        protected override void OnItemsSourceChanged(System.Collections.IEnumerable oldValue, System.Collections.IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            this.itemsSourceIsEmpty = this.ItemsSource != null && this.ItemsSource.GetEnumerator().MoveNext() == false;

            // A view can be created if there is data to auto-generate columns, or columns are added programmatically \\
            bool canCreateView = (this.ItemsSource != null) &&
                (this.itemsSourceIsEmpty == false || this.AutoGenerateColumns == false);

            if (canCreateView)
            {
                this.UpdateViewAndCollectionView(this.ItemsSource);

                // If there are items, select the first item now \\
                this.SelectedIndex = this.itemsSourceIsEmpty ? -1 : 0;
            }
            else
            {
                // Release the current inner grid \\
                this.ReleaseInnerGridReferences();

                // clean up old state if can not set the state.
                this.SetCollectionView(null);
                this.innerGrid = null;
                this.View = null;
            }
        }

        /// <summary>
        /// Called when ItemsChange to throw an exception indicating we don't support
        /// changing Items directly.
        /// </summary>
        /// <param name="e">Event parameters.</param>
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            if (e.NewItems != null)
            {
                // If the items source now has items, select the first item \\
                if (this.itemsSourceIsEmpty && this.Items.Count > 0)
                {
                    this.SelectedIndex = 0;
                    this.itemsSourceIsEmpty = false;
                }

                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    if (this.InnerGrid == null)
                    {
                        this.UpdateViewAndCollectionView(this.ItemsSource);
                    }
                }
            }
        }

        /// <summary>
        /// Called when a key is pressed while within the InnerList scope.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if ((e.Key == Key.Left || e.Key == Key.Right) &&
                Keyboard.Modifiers == ModifierKeys.None)
            {
                // If pressing Left or Right on a column header, move the focus \\
                GridViewColumnHeader header = e.OriginalSource as GridViewColumnHeader;

                if (header != null)
                {
                    header.MoveFocus(new TraversalRequest(KeyboardHelp.GetNavigationDirection(this, e.Key)));
                    e.Handled = true;
                }
            }
        }

        #endregion protected methods

        #region static private methods

        /// <summary>
        /// Called when the View property is changed.
        /// </summary>
        /// <param name="obj">InnerList whose property is being changed.</param>
        /// <param name="e">Event arguments.</param>
        private static void InnerList_OnViewChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            InnerList thisList = (InnerList)obj;
            GridView newGrid = e.NewValue as GridView;
            InnerListGridView innerGrid = e.NewValue as InnerListGridView;
            if (newGrid != null && innerGrid == null)
            {
                throw new NotSupportedException(string.Format(
                    CultureInfo.InvariantCulture,
                   InvariantResources.ViewSetWithType,
                   nameof(GridView),
                   nameof(InnerListGridView)));
            }

            ((InnerList)obj).innerGrid = innerGrid;
        }

        /// <summary>
        /// Gets the exception to be thrown when using Items.
        /// </summary>
        /// <returns>The exception to be thrown when using Items.</returns>
        private static NotSupportedException GetItemsException()
        {
            return new NotSupportedException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    InvariantResources.NotSupportAddingToItems,
                    nameof(InnerList),
                    ItemsControl.ItemsSourceProperty.Name));
        }
        #endregion static private methods

        #region instance private methods
        /// <summary>
        /// Called from OnItemsSourceChanged to set the collectionView field and
        /// subscribe to the collectionView changed event.
        /// </summary>
        /// <param name="newValue">ITemsSource passed to OnItemsSourceChanged.</param>
        private void SetCollectionView(System.Collections.IEnumerable newValue)
        {
            if (newValue == null)
            {
                this.CollectionView = null;
            }
            else
            {
                CollectionViewSource newValueViewSource = newValue as CollectionViewSource;
                if (newValueViewSource != null && newValueViewSource.View != null)
                {
                    this.CollectionView = newValueViewSource.View;
                }
                else
                {
                    this.CollectionView = CollectionViewSource.GetDefaultView(newValue);
                }
            }
        }

        /// <summary>
        /// Update View And CollectionView.
        /// </summary>
        /// <param name="value">InnerList object.</param>
        private void UpdateViewAndCollectionView(IEnumerable value)
        {
            Debug.Assert(value != null, "value should be non-null");

            // SetCollectionView deals with a null newEnumerable
            this.SetCollectionView(value);

            this.UpdateView(value);

            // Generate property changed event.
            if (this.ItemSourcesPropertyChanged != null)
            {
                this.ItemSourcesPropertyChanged(this, new PropertyChangedEventArgs("ItemsSource"));
            }
        }

        private void UpdateView(IEnumerable value)
        {
            // NOTE : We need to clear the SortDescription before
            // clearing the InnerGrid.Columns so that the Adorners
            // are appropriately cleared.
            InnerListColumn sortedColumn = this.SortedColumn;
            this.ClearSort();

            // Release the current inner grid \\
            this.ReleaseInnerGridReferences();

            if (this.AutoGenerateColumns)
            {
                this.innerGrid = new InnerListGridView();

                // PopulateColumns deals with a null newEnumerable
                this.innerGrid.PopulateColumns(value);
            }
            else
            {
                this.innerGrid = new InnerListGridView(this.Columns);
            }

            this.View = this.innerGrid;
            this.SetColumnHeaderActions();

            if (sortedColumn != null && this.Columns.Contains(sortedColumn))
            {
                this.ApplySort(sortedColumn, false);
            }
        }

        /// <summary>
        /// Releases all references to the current inner grid, if one exists.
        /// </summary>
        private void ReleaseInnerGridReferences()
        {
            if (this.innerGrid != null)
            {
                // Tell the inner grid to release its references \\
                this.innerGrid.ReleaseReferences();

                // Release the column headers \\
                foreach (InnerListColumn column in this.innerGrid.AvailableColumns)
                {
                    GridViewColumnHeader header = column.Header as GridViewColumnHeader;

                    if (header != null)
                    {
                        header.Click -= this.Header_Click;
                        header.PreviewKeyDown -= this.Header_KeyDown;
                    }
                }
            }
        }

        /// <summary>
        /// Called when the ItemsSource changes, after SetGridview to add event handlers
        /// to the column header.
        /// </summary>
        internal void SetColumnHeaderActions()
        {
            if (this.innerGrid == null)
            {
                return;
            }

            // set context menu
            this.innerGrid.ColumnHeaderContextMenu = this.GetListColumnsContextMenu();

            foreach (GridViewColumn column in this.innerGrid.AvailableColumns)
            {
                // A string header needs an explicit GridViewColumnHeader
                // so we can hook up our events
                string headerString = column.Header as string;
                if (headerString != null)
                {
                    GridViewColumnHeader columnHeader = new GridViewColumnHeader();
                    columnHeader.Content = headerString;
                    column.Header = columnHeader;
                }

                GridViewColumnHeader header = column.Header as GridViewColumnHeader;

                if (header != null)
                {
                    // header Click
                    header.Click += this.Header_Click;
                    header.PreviewKeyDown += this.Header_KeyDown;
                }

                // If it is a GridViewColumnHeader we will not have the same nice sorting and grouping
                // capabilities
            }
        }

        #region ApplicationCommands.Copy

        partial void OnCopyCanExecuteImplementation(CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.SelectedItems.Count > 0;
        }

        partial void OnCopyExecutedImplementation(ExecutedRoutedEventArgs e)
        {
            string text = this.GetClipboardTextForSelectedItems();
            this.SetClipboardWithSelectedItemsText(text);
        }

        #region Copy Helpers

        /// <summary>
        /// Gets a tab-delimited string representing the data of the selected rows.
        /// </summary>
        /// <returns>A tab-delimited string representing the data of the selected rows.</returns>
        protected internal string GetClipboardTextForSelectedItems()
        {
            StringBuilder text = new StringBuilder();

            foreach (object value in this.Items)
            {
                if (this.SelectedItems.Contains(value))
                {
                    string entry = this.GetClipboardTextLineForSelectedItem(value);
                    text.AppendLine(entry);
                }
            }

            return text.ToString();
        }

        private string GetClipboardTextLineForSelectedItem(object value)
        {
            StringBuilder entryText = new StringBuilder();

            foreach (InnerListColumn column in this.InnerGrid.Columns)
            {
                object propertyValue;
                if (!FilterRuleCustomizationFactory.FactoryInstance.PropertyValueGetter.TryGetPropertyValue(column.DataDescription.PropertyName, value, out propertyValue))
                {
                    propertyValue = string.Empty;
                }

                entryText.AppendFormat(CultureInfo.CurrentCulture, $"{propertyValue}\t");
            }

            return entryText.ToString();
        }

        private void SetClipboardWithSelectedItemsText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            DataObject data = new DataObject(DataFormats.UnicodeText, text);
            Clipboard.SetDataObject(data);
        }

        #endregion Copy Helpers

        #endregion ApplicationCommands.Copy

        /// <summary>
        /// Called to implement sorting functionality on column header pressed by space or enter key.
        /// </summary>
        /// <param name="sender">Typically a GridViewColumnHeader.</param>
        /// <param name="e">The event information.</param>
        private void Header_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Space && e.Key != Key.Enter)
            {
                return;
            }

            // Call HeaderActionProcess when space or enter key pressed
            this.HeaderActionProcess(sender);
            e.Handled = true;
        }

        /// <summary>
        /// Called to implement sorting functionality on column header click.
        /// </summary>
        /// <param name="sender">Typically a GridViewColumnHeader.</param>
        /// <param name="e">The event information.</param>
        private void Header_Click(object sender, RoutedEventArgs e)
        {
            // Call HeaderActionProcess when mouse clicked on the header
            this.HeaderActionProcess(sender);
        }

        /// <summary>
        /// Called to implement sorting functionality.
        /// </summary>
        /// <param name="sender">Typically a GridViewColumnHeader.</param>
        private void HeaderActionProcess(object sender)
        {
            GridViewColumnHeader header = (GridViewColumnHeader)sender;
            InnerListColumn column = (InnerListColumn)header.Column;
            UIPropertyGroupDescription dataDescription = column.DataDescription;

            if (dataDescription == null)
            {
                return;
            }

            // If the sorted column is sorted again, reverse the sort \\
            if (object.ReferenceEquals(column, this.sortedColumn))
            {
                dataDescription.ReverseSortDirection();
            }

            this.ApplySort(column, true);
        }

        /// <summary>
        /// Create default Context Menu.
        /// </summary>
        /// <returns>ContextMenu of List Columns.</returns>
        private ContextMenu GetListColumnsContextMenu()
        {
            this.contextMenu = new ContextMenu();

            // Add Context Menu item.
            this.SetColumnPickerContextMenuItem();

            return this.contextMenu;
        }

        /// <summary>
        /// Set up context menu item for Column Picker feature.
        /// </summary>
        /// <returns>True if it is successfully set up.</returns>
        private bool SetColumnPickerContextMenuItem()
        {
            MenuItem columnPicker = new MenuItem();
            AutomationProperties.SetAutomationId(columnPicker, "ChooseColumns");
            columnPicker.Header = UICultureResources.ColumnPicker;
            columnPicker.Click += this.innerGrid.OnColumnPicker;

            this.contextMenu.Items.Add(columnPicker);

            return true;
        }

        static partial void StaticConstructorImplementation()
        {
            // Adds notification for the View changing
            ListView.ViewProperty.OverrideMetadata(
                typeof(InnerList),
                new PropertyMetadata(new PropertyChangedCallback(InnerList_OnViewChanged)));
        }

        #endregion instance private methods
    }
}
