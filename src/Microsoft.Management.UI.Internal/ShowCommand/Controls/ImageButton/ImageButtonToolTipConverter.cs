// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;
using System.Windows.Data;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    /// <summary>
    /// Converts a an ImageButtonBase to its corresponding ToolTip.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Justification = "Needed for XAML")]
    public class ImageButtonToolTipConverter : IValueConverter
    {
        // This class is meant to be used like this in XAML:
        //  <Window xmlns:controls="clr-namespace:Microsoft.PowerShell.Commands.ShowCommandInternal" ...>
        //     ...
        //     <Window.Resources>
        //        <controls:RoutedUICommandToString x:Key="routedUICommandToString"/>
        //     </Window.Resources>
        //     ...
        //     <ContentControl ToolTip="{Binding Path=..., Converter={StaticResource routedUICommandToString}"/>
        #region IValueConverter Members

        /// <summary>
        /// Converts a an ImageButtonBase to its corresponding ToolTip by checking if it has a tooltip property
        /// or a command with tooltip text
        /// </summary>
        /// <param name="value">The ImageButtonBase we are trying to Convert.</param>
        /// <param name="targetType"><paramref name="targetType"/> is not used.</param>
        /// <param name="parameter"><paramref name="parameter"/> is not used.</param>
        /// <param name="culture"><paramref name="culture"/> is not used.</param>
        /// <returns>The resulting object obtained from retrieving the property value in <paramref name="parameter"/> (or property values if <paramref name="parameter"/> contains dots) out of <paramref name="value"/>. .</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ImageButtonBase imageButtonBase = value as ImageButtonBase;
            if (imageButtonBase == null)
            {
                return null;
            }

            object toolTipObj = imageButtonBase.GetValue(Button.ToolTipProperty);
            if (toolTipObj != null)
            {
                return toolTipObj.ToString();
            }

            if (imageButtonBase.Command != null && !string.IsNullOrEmpty(imageButtonBase.Command.Text))
            {
                return imageButtonBase.Command.Text.Replace("_", string.Empty);
            }

            return null;
        }

        /// <summary>
        /// This method is not supported.
        /// </summary>
        /// <param name="value"><paramref name="value"/> is not used.</param>
        /// <param name="targetType"><paramref name="targetType"/> is not used.</param>
        /// <param name="parameter"><paramref name="parameter"/> is not used.</param>
        /// <param name="culture"><paramref name="culture"/> is not used.</param>
        /// <returns>No value is returned.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
