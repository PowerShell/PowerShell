// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Windows;

using Microsoft.Management.UI.Internal.ShowCommand;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    /// <summary>
    /// Interaction logic for CmdletGUI.xaml.
    /// </summary>
    public partial class ShowCommandWindow : Window
    {
        #region Construction and Destructor

        /// <summary>
        /// Initializes a new instance of the ShowCommandWindow class.
        /// </summary>
        public ShowCommandWindow()
        {
            this.InitializeComponent();
            this.SizeChanged += this.ShowCommandWindow_SizeChanged;
            this.LocationChanged += this.ShowCommandWindow_LocationChanged;
            this.StateChanged += this.ShowCommandWindow_StateChanged;
        }

        /// <summary>
        /// Saves the user settings.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected override void OnClosed(System.EventArgs e)
        {
            ShowCommandSettings.Default.Save();
            base.OnClosed(e);
        }

        /// <summary>
        /// Saves size changes in user settings.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void ShowCommandWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ShowCommandSettings.Default.ShowOneCommandWidth = this.Width;
            ShowCommandSettings.Default.ShowOneCommandHeight = this.Height;
        }

        /// <summary>
        /// Saves position changes in user settings.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void ShowCommandWindow_LocationChanged(object sender, System.EventArgs e)
        {
            ShowCommandSettings.Default.ShowOneCommandTop = this.Top;
            ShowCommandSettings.Default.ShowOneCommandLeft = this.Left;
        }

        /// <summary>
        /// Updates the user setting with window state.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void ShowCommandWindow_StateChanged(object sender, System.EventArgs e)
        {
            ShowCommandSettings.Default.ShowOneCommandWindowMaximized = this.WindowState == WindowState.Maximized;
        }
        #endregion
    }
}
