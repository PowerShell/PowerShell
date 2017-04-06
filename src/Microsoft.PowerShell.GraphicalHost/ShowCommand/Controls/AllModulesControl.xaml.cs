//-----------------------------------------------------------------------
// <copyright file="AllModulesControl.xaml.cs" company="Microsoft">
//     Copyright © Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for AllModulesControl.xaml
    /// </summary>
    public partial class AllModulesControl : UserControl
    {
        #region Construction and Destructor

        /// <summary>
        /// Initializes a new instance of the AllModulesControl class
        /// </summary>
        public AllModulesControl()
        {
            InitializeComponent();
        }

        #endregion
        /// <summary>
        /// Gets current control of the ShowModuleControl
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
