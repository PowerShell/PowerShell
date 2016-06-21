//-----------------------------------------------------------------------
// <copyright file="MultipleSelectionDialog.xaml.cs" company="Microsoft">
//     Copyright © Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    using System.Windows;

    /// <summary>
    /// Interaction logic for MultipleSelectionDialog.xaml
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
        /// OK Click event function
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        /// <summary>
        /// Cancel Click event function
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
