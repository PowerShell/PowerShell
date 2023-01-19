// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Management.Automation;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    /// <summary>
    /// Interaction logic for MultipleSelectionControl.xaml.
    /// </summary>
    public partial class MultipleSelectionControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the MultipleSelectionControl class.
        /// </summary>
        public MultipleSelectionControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Show more items in new dialog.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void ButtonBrowse_Click(object sender, RoutedEventArgs e)
        {
            MultipleSelectionDialog multipleSelectionDialog = new MultipleSelectionDialog();
            multipleSelectionDialog.Title = this.multipleValueButton.ToolTip.ToString();
            multipleSelectionDialog.listboxParameter.ItemsSource = comboxParameter.ItemsSource;
            multipleSelectionDialog.ShowDialog();

            if (multipleSelectionDialog.DialogResult != true)
            {
                return;
            }

            StringBuilder newComboText = new StringBuilder();

            foreach (object selectedItem in multipleSelectionDialog.listboxParameter.SelectedItems)
            {
                newComboText.AppendFormat(CultureInfo.InvariantCulture, $"{selectedItem.ToString()},");
            }

            if (newComboText.Length > 1)
            {
                newComboText.Remove(newComboText.Length - 1, 1);
            }

            comboxParameter.Text = newComboText.ToString();
        }
    }
}
