// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Takes a value and returns the largest value which is a integral amount of the second value.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class IntegralConverter : IMultiValueConverter
    {
        /// <summary>
        /// Takes a value and returns the largest value which is a integral amount of the second value.
        /// </summary>
        /// <param name="values">
        /// The first value is the source.  The second is the factor.
        /// </param>
        /// <param name="targetType">The parameter is not used.</param>
        /// <param name="parameter">The padding to subtract from the first value.</param>
        /// <param name="culture">The parameter is not used.</param>
        /// <returns>
        /// The integral value.
        /// </returns>
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ArgumentNullException.ThrowIfNull(values);

            if (values.Length != 2)
            {
                throw new ArgumentException("Two values expected", "values");
            }

            if (values[0] == DependencyProperty.UnsetValue ||
                values[1] == DependencyProperty.UnsetValue)
            {
                return DependencyProperty.UnsetValue;
            }

            var source = (double)values[0];
            var factor = (double)values[1];

            double padding = 0;

            if (parameter != null)
            {
                padding = double.Parse((string)parameter, CultureInfo.InvariantCulture);
            }

            var newSource = source - padding;

            if (newSource < factor)
            {
                return source;
            }

            var remainder = newSource % factor;
            var result = newSource - remainder;

            return result;
        }

        /// <summary>
        /// This method is not used.
        /// </summary>
        /// <param name="value">The parameter is not used.</param>
        /// <param name="targetTypes">The parameter is not used.</param>
        /// <param name="parameter">The parameter is not used.</param>
        /// <param name="culture">The parameter is not used.</param>
        /// <returns>The parameter is not used.</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
