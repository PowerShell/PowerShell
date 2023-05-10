// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Windows.Data;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Formatting string with a given format.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class StringFormatConverter : IValueConverter
    {
        /// <summary>
        /// Formatting string with a given format.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.  This is not used.</param>
        /// <param name="parameter">The converter parameter to use.  It should be a formatting string.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The formatted string.</returns>
        public object Convert(object value, Type targetType, Object parameter, CultureInfo culture)
        {
            ArgumentNullException.ThrowIfNull(parameter);

            string str = (string)value;
            string formatString = (string)parameter;
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }

            return string.Format(culture, formatString, str);
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <remarks>
        /// This method is not implemented.
        /// </remarks>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
