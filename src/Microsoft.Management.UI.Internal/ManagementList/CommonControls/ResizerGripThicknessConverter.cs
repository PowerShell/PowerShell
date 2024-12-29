// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// A converter which creates the proper thickness for the content of the Resizer, depending on the grip visual size
    /// and grip position.
    /// </summary>
    /// <remarks>
    /// The first value needs to be a double which is the visible grip size.
    /// The second value needs to the be ResizeGripLocation value used.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public class ResizerGripThicknessConverter : IMultiValueConverter
    {
        /// <summary>
        /// Creates an instance of ResizerGripThicknessConverter.
        /// </summary>
        public ResizerGripThicknessConverter()
        {
            // nothing
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="values">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value. If the method returns nullNothingnullptra null reference (Nothing in Visual Basic), the valid null value is used.</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            ArgumentNullException.ThrowIfNull(values);

            if (object.ReferenceEquals(values[0], DependencyProperty.UnsetValue) ||
                object.ReferenceEquals(values[1], DependencyProperty.UnsetValue))
            {
                return DependencyProperty.UnsetValue;
            }

            var resizerVisibleGripWidth = (double)values[0];

            var gripLocation = (ResizeGripLocation)values[1];

            return Resizer.CreateGripThickness(resizerVisibleGripWidth, gripLocation);
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetTypes">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted values. If the method returns nullNothingnullptra null reference (Nothing in Visual Basic), the valid null value is used.</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
