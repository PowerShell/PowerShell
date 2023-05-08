// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    /// <summary>
    /// Interaction logic for AllModulesControl.xaml.
    /// </summary>
    public partial class AllModulesControl : UserControl
    {
        #region Construction and Destructor

        /// <summary>
        /// Initializes a new instance of the AllModulesControl class.
        /// </summary>
        public AllModulesControl()
        {
            InitializeComponent();

            this.Loaded += (obj, args) =>
            {
                this.ModulesCombo.Focus();
            };
        }

        #endregion
        /// <summary>
        /// Gets current control of the ShowModuleControl.
        /// </summary>
        internal ShowModuleControl CurrentShowModuleControl
        {
            get { return this.ShowModuleControl; }
        }

        private void RefreshButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AllModulesViewModel viewModel = this.DataContext as AllModulesViewModel;
            if (viewModel == null)
            {
                return;
            }

            viewModel.OnRefresh();
        }
    }
}
