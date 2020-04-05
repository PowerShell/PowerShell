// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Windows;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    /// <summary>
    /// Interaction logic for MultipleSelectionDialog.xaml.
    /// </summary>
    public partial class MultipleSelectionDialog : Window
    {
        /// <summary>
        /// Initializes a new instance of the MultipleSelectionDialog class.
        /// </summary>
        public MultipleSelectionDialog()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// OK Click event function.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        /// <summary>
        /// Cancel Click event function.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
