//-----------------------------------------------------------------------
// <copyright file="ShowCommandWindow.xaml.cs" company="Microsoft">
//     Copyright © Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    using System.Windows;
    using Microsoft.Management.UI.Internal.ShowCommand;

    /// <summary>
    /// Interaction logic for CmdletGUI.xaml
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
            this.SizeChanged += new SizeChangedEventHandler(this.ShowCommandWindow_SizeChanged);
            this.LocationChanged += new System.EventHandler(this.ShowCommandWindow_LocationChanged);
            this.StateChanged += new System.EventHandler(this.ShowCommandWindow_StateChanged);
        }

        /// <summary>
        /// Saves the user settings
        /// </summary>
        /// <param name="e">event arguments</param>
        protected override void OnClosed(System.EventArgs e)
        {
            ShowCommandSettings.Default.Save();
            base.OnClosed(e);
        }

        /// <summary>
        /// Saves size changes in user settings
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void ShowCommandWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ShowCommandSettings.Default.ShowOneCommandWidth = this.Width;
            ShowCommandSettings.Default.ShowOneCommandHeight = this.Height;
        }

        /// <summary>
        /// Saves position changes in user settings
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void ShowCommandWindow_LocationChanged(object sender, System.EventArgs e)
        {
            ShowCommandSettings.Default.ShowOneCommandTop = this.Top;
            ShowCommandSettings.Default.ShowOneCommandLeft = this.Left;
        }

        /// <summary>
        /// Updates the user setting with window state
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void ShowCommandWindow_StateChanged(object sender, System.EventArgs e)
        {
            ShowCommandSettings.Default.ShowOneCommandWindowMaximized = this.WindowState == WindowState.Maximized;
        }
        #endregion
    }
}
