// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    /// <summary>
    /// Button with images to represent enabled and disabled states.
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
            this.Loaded += this.ImageButton_Loaded;
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
                this.innerButton.SetValue(AutomationProperties.AutomationIdProperty, thisAutomationId);
            }

            object thisAutomationName = this.GetValue(AutomationProperties.NameProperty);
            if (thisAutomationName != null)
            {
                this.innerButton.SetValue(AutomationProperties.NameProperty, thisAutomationName);
            }
        }
    }
}
