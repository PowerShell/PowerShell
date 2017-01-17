//-----------------------------------------------------------------------
// <copyright file="ImageButton.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Implements ImageButton.
// </summary>
//-----------------------------------------------------------------------

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows.Automation;

    /// <summary>
    /// Button with images to represent enabled and disabled states
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Justification = "Required by XAML")]
    public partial class ImageButton : ImageButtonBase
    {
        /// <summary>
        /// Initializes a new instance of the ImageButton class.
        /// </summary>
        public ImageButton()
        {
            InitializeComponent();
            this.Loaded += new System.Windows.RoutedEventHandler(this.ImageButton_Loaded);
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
                this.innerButton.SetValue(AutomationProperties.AutomationIdProperty, thisAutomationId);
            }
        }
    }
}
