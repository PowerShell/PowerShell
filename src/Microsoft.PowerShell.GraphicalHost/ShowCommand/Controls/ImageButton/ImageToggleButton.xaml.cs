//-----------------------------------------------------------------------
// <copyright file="ImageToggleButton.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Implements ImageToggleButton.
// </summary>
//-----------------------------------------------------------------------

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Automation;

    /// <summary>
    /// Toggle button with images to represent enabled and disabled states
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Justification = "Required by XAML")]
    public partial class ImageToggleButton : ImageButtonBase
    {
        /// <summary>
        /// Value indicating the button is checked
        /// </summary>
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register("IsChecked", typeof(bool), typeof(ImageToggleButton));

        /// <summary>
        /// Initializes a new instance of the ImageToggleButton class.
        /// </summary>
        public ImageToggleButton()
        {
            InitializeComponent();
            this.Loaded += new System.Windows.RoutedEventHandler(this.ImageButton_Loaded);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the button is checked
        /// </summary>
        public bool IsChecked
        {
            get { return (bool)GetValue(ImageToggleButton.IsCheckedProperty); }
            set { SetValue(ImageToggleButton.IsCheckedProperty, value); }
        }

        /// <summary>
        /// Copies the automation id from the parent control to the inner button
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void ImageButton_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            object thisAutomationId = this.GetValue(AutomationProperties.AutomationIdProperty);
            if (thisAutomationId != null)
            {
                this.toggleInnerButton.SetValue(AutomationProperties.AutomationIdProperty, thisAutomationId);
            }
        }
    }
}
