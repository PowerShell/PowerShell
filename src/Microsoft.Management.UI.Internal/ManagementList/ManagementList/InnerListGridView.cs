// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Extends the basic GrdView class to introduce the Visible concept to the
    /// Columns collection.
    /// </summary>
    /// <!--We create our own version of Columns, that:
    /// 1) Only takes InnerListColumn's
    /// 2) Passes through the underlying ListView Columns, only the InnerListColumns
    /// that have Visible=true.-->
    [ContentProperty("AvailableColumns")]
    public class InnerListGridView : GridView
    {
        /// <summary>
        /// Set to true when we want to change the Columns collection.
        /// </summary>
        private bool canChangeColumns = false;

        /// <summary>
        /// Instanctiates a new object of this class.
        /// </summary>
        public InnerListGridView()
            : this(new ObservableCollection<InnerListColumn>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InnerListGridView"/> class with the specified columns.
        /// </summary>
        /// <param name="availableColumns">The columns this grid should display.</param>
        /// <exception cref="ArgumentNullException">The specified value is a null reference.</exception>
        internal InnerListGridView(ObservableCollection<InnerListColumn> availableColumns)
        {
            ArgumentNullException.ThrowIfNull(availableColumns);

            // Setting the AvailableColumns property won't trigger CollectionChanged, so we have to do it manually \\
            this.AvailableColumns = availableColumns;
            this.AvailableColumns_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, availableColumns));

            availableColumns.CollectionChanged += this.AvailableColumns_CollectionChanged;
            this.Columns.CollectionChanged += this.Columns_CollectionChanged;
        }

        /// <summary>
        /// Gets a collection of all columns which can be
        /// added to the ManagementList, for example through ColumnPicker.
        /// Columns is the collection of the columns which are currently
        /// displayed (in the order in which they are displayed).
        /// </summary>
        internal ObservableCollection<InnerListColumn> AvailableColumns
        {
            get;
            private set;
        }

        /// <summary>
        /// Releases this instance's references to its controls.
        /// This API supports the framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public void ReleaseReferences()
        {
            this.AvailableColumns.CollectionChanged -= this.AvailableColumns_CollectionChanged;
            this.Columns.CollectionChanged -= this.Columns_CollectionChanged;

            foreach (InnerListColumn column in this.AvailableColumns)
            {
                // Unsubscribe from the column's change events \\
                ((INotifyPropertyChanged)column).PropertyChanged -= this.Column_PropertyChanged;

                // If the column is shown, store its last width before releasing \\
                if (column.Visible)
                {
                    column.Width = column.ActualWidth;
                }
            }

            // Remove the columns so they can be added to the next GridView \\
            this.Columns.Clear();
        }

        /// <summary>
        /// Called when the ItemsSource changes to auto populate the GridView columns
        /// with reflection information on the first element of the ItemsSource.
        /// </summary>
        /// <param name="newValue">
        /// The new ItemsSource.
        /// This is used just to fetch .the first collection element.
        /// </param>
        internal void PopulateColumns(System.Collections.IEnumerable newValue)
        {
            if (newValue == null)
            {
                return; // No elements, so we can't populate
            }

            IEnumerator newValueEnumerator = newValue.GetEnumerator();
            if (!newValueEnumerator.MoveNext())
            {
                return; // No first element, so we can't populate
            }

            object first = newValueEnumerator.Current;
            if (first == null)
            {
                return;
            }

            Debug.Assert(this.AvailableColumns.Count == 0, "AvailabeColumns should be empty at this point");

            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(first);
            foreach (PropertyDescriptor property in properties)
            {
                UIPropertyGroupDescription dataDescription = new UIPropertyGroupDescription(property.Name, property.Name, property.PropertyType);
                InnerListColumn column = new InnerListColumn(dataDescription);
                this.AvailableColumns.Add(column);
            }
        }

        /// <summary>
        /// Callback for displaying the Column Picker.
        /// </summary>
        /// <param name="sender">The send object.</param>
        /// <param name="e">The Event RoutedEventArgs.</param>
        internal void OnColumnPicker(object sender, RoutedEventArgs e)
        {
            ColumnPicker columnPicker = new ColumnPicker(
                this.Columns, this.AvailableColumns);
            columnPicker.Owner = Window.GetWindow((DependencyObject)sender);

            bool? retval = columnPicker.ShowDialog();
            if (retval != true)
            {
                return;
            }

            this.canChangeColumns = true;
            try
            {
                this.Columns.Clear();
                ObservableCollection<InnerListColumn> newColumns = columnPicker.SelectedColumns;
                Debug.Assert(newColumns != null, "SelectedColumns not found");
                foreach (InnerListColumn column in newColumns)
                {
                    Debug.Assert(column.Visible, "is visible");

                    // 185977: ML InnerListGridView.PopulateColumns(): Always set Width on new columns
                    // Workaround to GridView issue suggested by Ben Carter
                    // Issue: Once a column has been added to a GridView
                    //   and then removed, auto-sizing does not work
                    //   after it is added back.
                    // Solution: Remove the column, change the width,
                    //   add the column back, then change the width back.
                    double width = column.Width;
                    column.Width = 0d;
                    this.Columns.Add(column);
                    column.Width = width;
                }
            }
            finally
            {
                this.canChangeColumns = false;
            }
        }

        /// <summary>
        /// Called when Columns changes so we can check we are the ones changing it.
        /// </summary>
        /// <param name="sender">The collection changing.</param>
        /// <param name="e">The event parameters.</param>
        private void Columns_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                // Move happens in the GUI drag and drop operation, so we have to allow it.
                case NotifyCollectionChangedAction.Move:
                    return;

                // default means all other operations (Add, Move, Replace and Reset) those are reserved.
                // only we should do it, as we keep AvailableColumns in sync with columns
                default:
                    if (!this.canChangeColumns)
                    {
                        throw new NotSupportedException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                InvariantResources.CannotModified,
                                InvariantResources.Columns,
                                "AvailableColumns"));
                    }

                    break;
            }
        }

        /// <summary>
        /// Called when the AvailableColumns changes to pass through the VisibleColumns to Columns.
        /// </summary>
        /// <param name="sender">The collection changing.</param>
        /// <param name="e">The event parameters.</param>
        private void AvailableColumns_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.AddOrRemoveNotifications(e);
            this.SynchronizeColumns();
        }

        /// <summary>
        /// Called from availableColumns_CollectionChanged to add or remove the notifications
        /// used to track the Visible property.
        /// </summary>
        /// <param name="e">The parameter passed to availableColumns_CollectionChanged.</param>
        private void AddOrRemoveNotifications(NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Move)
            {
                if (e.OldItems != null)
                {
                    foreach (InnerListColumn oldColumn in e.OldItems)
                    {
                        ((INotifyPropertyChanged)oldColumn).PropertyChanged -= this.Column_PropertyChanged;
                    }
                }

                if (e.NewItems != null)
                {
                    foreach (InnerListColumn newColumn in e.NewItems)
                    {
                        ((INotifyPropertyChanged)newColumn).PropertyChanged += this.Column_PropertyChanged;
                    }
                }
            }
        }

        /// <summary>
        /// Syncronizes AvailableColumns and Columns preserving the order from Columns that
        /// comes from the user moving Columns around.
        /// </summary>
        private void SynchronizeColumns()
        {
            this.canChangeColumns = true;
            try
            {
                // Add to listViewColumns all Visible columns in availableColumns not already in listViewColumns
                foreach (InnerListColumn column in this.AvailableColumns)
                {
                    if (column.Visible && !this.Columns.Contains(column))
                    {
                        this.Columns.Add(column);
                    }
                }

                // Remove all columns which are not visible or removed from Available columns.
                for (int i = this.Columns.Count - 1; i >= 0; i--)
                {
                    InnerListColumn listViewColumn = (InnerListColumn)this.Columns[i];
                    if (!listViewColumn.Visible || !this.AvailableColumns.Contains(listViewColumn))
                    {
                        this.Columns.RemoveAt(i);
                    }
                }
            }
            finally
            {
                this.canChangeColumns = false;
            }
        }

        /// <summary>
        /// Called when the Visible property of a column changes.
        /// </summary>
        /// <param name="sender">The column whose property changed.</param>
        /// <param name="e">The event parameters.</param>
        private void Column_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == InnerListColumn.VisibleProperty.Name)
            {
                this.SynchronizeColumns();
            }
        }
    }
}
