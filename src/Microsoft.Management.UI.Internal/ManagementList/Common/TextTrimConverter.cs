// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Removes whitespace at beginning and end of a string.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class TextTrimConverter : IValueConverter
    {
        /// <summary>
        /// Creates a new TextTrimConverter. By default, both conversion directions are trimmed.
        /// </summary>
        public TextTrimConverter()
        {
        }

        #region IValueConverter Members

        /// <summary>
        /// Trims excess whitespace from the given string.
        /// </summary>
        /// <param name="value">Original string.</param>
        /// <param name="targetType">The parameter is not used.</param>
        /// <param name="parameter">The parameter is not used.</param>
        /// <param name="culture">The parameter is not used.</param>
        /// <returns>The trimmed string.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return TrimValue(value);
        }

        private static object TrimValue(object value)
        {
            string strValue = value as string;
            if (strValue == null)
            {
                return value;
            }

            return strValue.Trim();
        }

        /// <summary>
        /// Trims extra whitespace from the given string during backward conversion.
        /// </summary>
        /// <param name="value">Original string.</param>
        /// <param name="targetType">The parameter is not used.</param>
        /// <param name="parameter">The parameter is not used.</param>
        /// <param name="culture">The parameter is not used.</param>
        /// <returns>The trimmed string.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return TrimValue(value);
        }

        #endregion
    }
}
