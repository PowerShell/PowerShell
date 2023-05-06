// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Windows.Data;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Takes a bool value and returns the inverse.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class InverseBooleanConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to be it's inverse.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <param name="targetType">The parameter is not used.</param>
        /// <param name="parameter">The parameter is not used.</param>
        /// <param name="culture">The parameter is not used.</param>
        /// <returns>The inverted boolean value.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ArgumentNullException.ThrowIfNull(value);

            var boolValue = (bool)value;

            return !boolValue;
        }

        /// <summary>
        /// This method is not used.
        /// </summary>
        /// <param name="value">The parameter is not used.</param>
        /// <param name="targetType">The parameter is not used.</param>
        /// <param name="parameter">The parameter is not used.</param>
        /// <param name="culture">The parameter is not used.</param>
        /// <returns>The parameter is not used.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
