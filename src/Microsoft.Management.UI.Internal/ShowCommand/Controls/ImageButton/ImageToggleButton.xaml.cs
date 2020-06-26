// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Automation;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    /// <summary>
    /// Toggle button with images to represent enabled and disabled states.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Justification = "Required by XAML")]
    public partial class ImageToggleButton : ImageButtonBase
    {
        /// <summary>
        /// Value indicating the button is checked.
        /// </summary>
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register("IsChecked", typeof(bool), typeof(ImageToggleButton));

        /// <summary>
        /// Initializes a new instance of the ImageToggleButton class.
        /// </summary>
        public ImageToggleButton()
        {
            InitializeComponent();
            this.Loaded += this.ImageButton_Loaded;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the button is checked.
        /// </summary>
        public bool IsChecked
        {
            get { return (bool)GetValue(ImageToggleButton.IsCheckedProperty); }
            set { SetValue(ImageToggleButton.IsCheckedProperty, value); }
        }

        /// <summary>
        /// Copies the automation id and name from the parent control to the inner button.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void ImageButton_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            object thisAutomationId = this.GetValue(AutomationProperties.AutomationIdProperty);
            if (thisAutomationId != null)
            {
                this.toggleInnerButton.SetValue(AutomationProperties.AutomationIdProperty, thisAutomationId);
            }

            object thisAutomationName = this.GetValue(AutomationProperties.NameProperty);
            if (thisAutomationName != null)
            {
                this.toggleInnerButton.SetValue(AutomationProperties.NameProperty, thisAutomationName);
            }
        }
    }
}
