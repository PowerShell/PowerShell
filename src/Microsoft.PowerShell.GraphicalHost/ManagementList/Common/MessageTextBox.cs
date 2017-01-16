//-----------------------------------------------------------------------
// <copyright file="MessageTextBox.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// A textbox modified to show Default text when no user text
// is supplied
// </summary>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI.Internal
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;

    /// <content>
    /// Partial class implementation for MessageTextBox control.
    /// </content>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public partial class MessageTextBox : TextBox
    {
        static partial void StaticConstructorImplementation()
        {
            TextProperty.OverrideMetadata(
                                          typeof(MessageTextBox),
                                          new FrameworkPropertyMetadata(
                                                                        String.Empty,
                                                                        null,
                                                                        new CoerceValueCallback(OnTextBoxTextCoerce)));
        }

        #region Non-Public Methods

        private void UpdateIsBackgroundTextShown(string text)
        {
            if (String.IsNullOrEmpty(text) == false && this.IsBackgroundTextShown)
            {
                this.IsBackgroundTextShown = false;
            }
            else if (String.IsNullOrEmpty(text) && this.IsBackgroundTextShown == false)
            {
                this.IsBackgroundTextShown = true;
            }
        }

        private static object OnTextBoxTextCoerce(DependencyObject o, object baseValue)
        {
            MessageTextBox mtb = (MessageTextBox)o;

            mtb.UpdateIsBackgroundTextShown((string)baseValue);

            if (baseValue == null)
            {
                return string.Empty;
            }

            return baseValue;
        }

        #endregion Non-Public Methods
    }
}

