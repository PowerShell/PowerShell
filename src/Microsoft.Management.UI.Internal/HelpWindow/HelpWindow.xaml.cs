// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Management.Automation;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

using Microsoft.Management.UI.Internal;

namespace Microsoft.Management.UI
{
    /// <summary>
    /// A window displaying help content and allowing search.
    /// </summary>
    public partial class HelpWindow : Window
    {
        /// <summary>
        /// Minimum zoom in the slider.
        /// </summary>
        public static double MinimumZoom
        {
            get
            {
                return 20;
            }
        }

        /// <summary>
        /// Maximum zoom in the slider.
        /// </summary>
        public static double MaximumZoom
        {
            get
            {
                return 300;
            }
        }

        /// <summary>
        /// Zoom interval.
        /// </summary>
        public static double ZoomInterval
        {
            get
            {
                return 10;
            }
        }

        /// <summary>
        /// The ViewModel for the dialog.
        /// </summary>
        private readonly HelpViewModel viewModel;

        /// <summary>
        /// Initializes a new instance of the HelpWindow class.
        /// </summary>
        /// <param name="helpObject">The object with help information.</param>
        public HelpWindow(PSObject helpObject)
        {
            InitializeComponent();
            this.viewModel = new HelpViewModel(helpObject, this.DocumentParagraph);
            CommonHelper.SetStartingPositionAndSize(
                this,
                HelpWindowSettings.Default.HelpWindowTop,
                HelpWindowSettings.Default.HelpWindowLeft,
                HelpWindowSettings.Default.HelpWindowWidth,
                HelpWindowSettings.Default.HelpWindowHeight,
                double.Parse((string)HelpWindowSettings.Default.Properties["HelpWindowWidth"].DefaultValue, CultureInfo.InvariantCulture.NumberFormat),
                double.Parse((string)HelpWindowSettings.Default.Properties["HelpWindowHeight"].DefaultValue, CultureInfo.InvariantCulture.NumberFormat),
                HelpWindowSettings.Default.HelpWindowMaximized);

            this.ReadZoomUserSetting();

            this.viewModel.PropertyChanged += this.ViewModel_PropertyChanged;
            this.DataContext = this.viewModel;

            this.Loaded += this.HelpDialog_Loaded;
            this.Closed += this.HelpDialog_Closed;
        }

        /// <summary>
        /// Handles the mouse wheel to zoom in/out.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control)
            {
                return;
            }

            if (e.Delta > 0)
            {
                this.viewModel.ZoomIn();
                e.Handled = true;
            }
            else
            {
                this.viewModel.ZoomOut();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles key down to fix the Page/Douwn going to end of help issue
        /// And to implement some additional shortcuts like Ctrl+F and ZoomIn/ZoomOut.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.None)
            {
                if (e.Key == Key.PageUp)
                {
                    this.Scroll.PageUp();
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.PageDown)
                {
                    this.Scroll.PageDown();
                    e.Handled = true;
                    return;
                }
            }

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                this.HandleZoomInAndZoomOut(e);
                if (e.Handled)
                {
                    return;
                }

                if (e.Key == Key.F)
                {
                    this.Find.Focus();
                    e.Handled = true;
                    return;
                }
            }

            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                this.HandleZoomInAndZoomOut(e);
            }
        }

        /// <summary>
        /// Reads the zoom part of the user settings.
        /// </summary>
        private void ReadZoomUserSetting()
        {
            if (HelpWindowSettings.Default.HelpZoom < HelpWindow.MinimumZoom || HelpWindowSettings.Default.HelpZoom > HelpWindow.MaximumZoom)
            {
                HelpWindowSettings.Default.HelpZoom = 100;
            }

            this.viewModel.Zoom = HelpWindowSettings.Default.HelpZoom;
        }

        /// <summary>
        /// Handles Zoom in and Zoom out keys.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        private void HandleZoomInAndZoomOut(KeyEventArgs e)
        {
            if (e.Key == Key.OemPlus || e.Key == Key.Add)
            {
                this.viewModel.ZoomIn();
                e.Handled = true;
            }

            if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
            {
                this.viewModel.ZoomOut();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Listens to changes in the zoom in order to update the user settings.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Zoom")
            {
                HelpWindowSettings.Default.HelpZoom = this.viewModel.Zoom;
            }
        }

        /// <summary>
        /// Saves the user settings.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void HelpDialog_Closed(object sender, System.EventArgs e)
        {
            HelpWindowSettings.Default.Save();
        }

        /// <summary>
        /// Updates the user setting with window state.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void HelpDialog_StateChanged(object sender, System.EventArgs e)
        {
            HelpWindowSettings.Default.HelpWindowMaximized = this.WindowState == WindowState.Maximized;
        }

        /// <summary>
        /// Sets the positions from user settings and start monitoring position changes.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void HelpDialog_Loaded(object sender, RoutedEventArgs e)
        {
            this.StateChanged += this.HelpDialog_StateChanged;
            this.LocationChanged += this.HelpDialog_LocationChanged;
            this.SizeChanged += this.HelpDialog_SizeChanged;
        }

        /// <summary>
        /// Saves size changes in user settings.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void HelpDialog_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            HelpWindowSettings.Default.HelpWindowWidth = this.Width;
            HelpWindowSettings.Default.HelpWindowHeight = this.Height;
        }

        /// <summary>
        /// Saves position changes in user settings.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void HelpDialog_LocationChanged(object sender, System.EventArgs e)
        {
            HelpWindowSettings.Default.HelpWindowTop = this.Top;
            HelpWindowSettings.Default.HelpWindowLeft = this.Left;
        }

        /// <summary>
        /// Called when the settings button is clicked.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsDialog settings = new SettingsDialog();
            settings.Owner = this;

            settings.ShowDialog();

            if (settings.DialogResult == true)
            {
                this.viewModel.HelpBuilder.AddTextToParagraphBuilder();
                this.viewModel.Search();
            }
        }

        /// <summary>
        /// Called when the Previous button is clicked.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void PreviousMatch_Click(object sender, RoutedEventArgs e)
        {
            this.MoveToNextMatch(false);
        }

        /// <summary>
        /// Called when the Next button is clicked.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void NextMatch_Click(object sender, RoutedEventArgs e)
        {
            this.MoveToNextMatch(true);
        }

        /// <summary>
        /// Moves to the previous or next match.
        /// </summary>
        /// <param name="forward">True for forward false for backwards.</param>
        private void MoveToNextMatch(bool forward)
        {
            TextPointer caretPosition = this.HelpText.CaretPosition;
            Run nextRun = this.viewModel.Searcher.MoveAndHighlightNextNextMatch(forward, caretPosition);
            this.MoveToRun(nextRun);
        }

        /// <summary>
        /// Moves to the caret and brings the view to the <paramref name="run"/>.
        /// </summary>
        /// <param name="run">Run to move to.</param>
        private void MoveToRun(Run run)
        {
            if (run == null)
            {
                return;
            }

            run.BringIntoView();
            this.HelpText.CaretPosition = run.ElementEnd;
            this.HelpText.Focus();
        }
    }
}
