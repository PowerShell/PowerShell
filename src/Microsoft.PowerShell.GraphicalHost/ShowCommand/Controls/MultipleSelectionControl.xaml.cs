//-----------------------------------------------------------------------
// <copyright file="MultipleSelectionControl.xaml.cs" company="Microsoft">
//     Copyright © Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    using System.Globalization;
    using System.Management.Automation;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for MultipleSelectionControl.xaml
    /// </summary>
    public partial class MultipleSelectionControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the MultipleSelectionControl class
        /// </summary>
        public MultipleSelectionControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Show more items in new dialog
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
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
                newComboText.AppendFormat(CultureInfo.InvariantCulture, "{0},", selectedItem.ToString());
            }

            if (newComboText.Length > 1)
            {
                newComboText.Remove(newComboText.Length - 1, 1);
            }

            comboxParameter.Text = newComboText.ToString();
        }
    }
}
