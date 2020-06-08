// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Windows;
using System.Windows.Input;

using Microsoft.Management.UI.Internal;
using Microsoft.Management.UI.Internal.ShowCommand;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    /// <summary>
    /// Interaction logic for CmdletGUI.xaml.
    /// </summary>
    public partial class ShowAllModulesWindow : Window
    {
        /// <summary>
        /// private constants for ZoomLevel.
        /// </summary>
        private double zoomLevel = 1.0;

        /// <summary>
        /// Zoom Increments.
        /// </summary>
        private const double ZOOM_INCREMENT = 0.2;

        /// <summary>
        /// Max ZoomLevel.
        /// </summary>
        private const double ZOOM_MAX = 3.0;

        /// <summary>
        /// Min ZoomLevel.
        /// </summary>
        private const double ZOOM_MIN = 0.5;

        #region Construction and Destructor

        /// <summary>
        /// Initializes a new instance of the ShowAllModulesWindow class.
        /// </summary>
        public ShowAllModulesWindow()
        {
            this.InitializeComponent();

            if (this.AllModulesControl != null && this.AllModulesControl.ShowModuleControl != null)
            {
                this.AllModulesControl.ShowModuleControl.Owner = this;
            }

            this.SizeChanged += this.ShowAllModulesWindow_SizeChanged;
            this.LocationChanged += this.ShowAllModulesWindow_LocationChanged;
            this.StateChanged += this.ShowAllModulesWindow_StateChanged;
            this.Loaded += this.ShowAllModulesWindow_Loaded;

            RoutedCommand plusSettings = new RoutedCommand();
            KeyGestureConverter keyGestureConverter = new KeyGestureConverter();

            try
            {
                plusSettings.InputGestures.Add((KeyGesture)keyGestureConverter.ConvertFromString(UICultureResources.ZoomIn1Shortcut));
                plusSettings.InputGestures.Add((KeyGesture)keyGestureConverter.ConvertFromString(UICultureResources.ZoomIn2Shortcut));
                plusSettings.InputGestures.Add((KeyGesture)keyGestureConverter.ConvertFromString(UICultureResources.ZoomIn3Shortcut));
                plusSettings.InputGestures.Add((KeyGesture)keyGestureConverter.ConvertFromString(UICultureResources.ZoomIn4Shortcut));
                CommandBindings.Add(new CommandBinding(plusSettings, ZoomEventHandlerPlus));
            }
            catch (NotSupportedException)
            {
                // localized has a problematic string - going to default
                plusSettings.InputGestures.Add((KeyGesture)keyGestureConverter.ConvertFromString("Ctrl+Add"));
                plusSettings.InputGestures.Add((KeyGesture)keyGestureConverter.ConvertFromString("Ctrl+Plus"));
                CommandBindings.Add(new CommandBinding(plusSettings, ZoomEventHandlerPlus));
            }

            RoutedCommand minusSettings = new RoutedCommand();
            try
            {
                minusSettings.InputGestures.Add((KeyGesture)keyGestureConverter.ConvertFromString(UICultureResources.ZoomOut1Shortcut));
                minusSettings.InputGestures.Add((KeyGesture)keyGestureConverter.ConvertFromString(UICultureResources.ZoomOut2Shortcut));
                minusSettings.InputGestures.Add((KeyGesture)keyGestureConverter.ConvertFromString(UICultureResources.ZoomOut3Shortcut));
                minusSettings.InputGestures.Add((KeyGesture)keyGestureConverter.ConvertFromString(UICultureResources.ZoomOut4Shortcut));

                CommandBindings.Add(new CommandBinding(minusSettings, ZoomEventHandlerMinus));
            }
            catch (NotSupportedException)
            {
                // localized has a problematic string - going to default
                minusSettings.InputGestures.Add((KeyGesture)keyGestureConverter.ConvertFromString("Ctrl+Subtract"));
                minusSettings.InputGestures.Add((KeyGesture)keyGestureConverter.ConvertFromString("Ctrl+Minus"));
                CommandBindings.Add(new CommandBinding(minusSettings, ZoomEventHandlerMinus));
            }
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
        /// Sets the focus on the CommandName control.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void ShowAllModulesWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.AllModulesControl.CommandName.Focus();
        }

        /// <summary>
        /// Saves size changes in user settings.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void ShowAllModulesWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ShowCommandSettings.Default.ShowCommandsWidth = this.Width;
            ShowCommandSettings.Default.ShowCommandsHeight = this.Height;
        }

        /// <summary>
        /// Saves position changes in user settings.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void ShowAllModulesWindow_LocationChanged(object sender, System.EventArgs e)
        {
            ShowCommandSettings.Default.ShowCommandsTop = this.Top;
            ShowCommandSettings.Default.ShowCommandsLeft = this.Left;
        }

        /// <summary>
        /// Updates the user setting with window state.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void ShowAllModulesWindow_StateChanged(object sender, System.EventArgs e)
        {
            ShowCommandSettings.Default.ShowCommandsWindowMaximized = this.WindowState == WindowState.Maximized;
        }

        /// <summary>
        /// Implements ZoomIn.
        /// </summary>
        /// <param name="sender">.</param>
        /// <param name="e">.</param>
        private void ZoomEventHandlerPlus(object sender, ExecutedRoutedEventArgs e)
        {
            AllModulesViewModel viewModel = this.DataContext as AllModulesViewModel;
            if (viewModel == null)
            {
                return;
            }

            if (this.zoomLevel == 0)
            {
                this.zoomLevel = 1;
            }

            if (this.zoomLevel < ZOOM_MAX)
            {
                // ViewModel applies ZoomLevel after dividing it by 100, So multiply it by 100 and then later reset to normal by dividing for next zoom
                this.zoomLevel = (this.zoomLevel + ZOOM_INCREMENT) * 100;
                viewModel.ZoomLevel = this.zoomLevel;
                this.zoomLevel /= 100;
            }
        }

        /// <summary>
        /// Implements ZoomOut.
        /// </summary>
        /// <param name="sender">.</param>
        /// <param name="e">.</param>
        private void ZoomEventHandlerMinus(object sender, ExecutedRoutedEventArgs e)
        {
            AllModulesViewModel viewModel = this.DataContext as AllModulesViewModel;
            if (viewModel == null)
            {
                return;
            }

            if (this.zoomLevel >= ZOOM_MIN)
            {
                // ViewModel applies ZoomLevel after dividing it by 100, So multiply it by 100 and then later reset to normal by dividing it for next zoom
                this.zoomLevel = (this.zoomLevel - ZOOM_INCREMENT) * 100;
                viewModel.ZoomLevel = this.zoomLevel;
                this.zoomLevel /= 100;
            }
        }
        #endregion
    }
}
