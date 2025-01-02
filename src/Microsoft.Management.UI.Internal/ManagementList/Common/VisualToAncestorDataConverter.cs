// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Provides a way to get the <see cref="FrameworkElement.DataContext"/> of a visual ancestor.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class VisualToAncestorDataConverter : IValueConverter
    {
        /// <summary>
        /// Searches ancestors for data of the specified class type.
        /// </summary>
        /// <param name="value">The visual whose ancestors are searched.</param>
        /// <param name="targetType">The parameter is not used.</param>
        /// <param name="parameter">The type of the data to find. The type must be a class.</param>
        /// <param name="culture">The parameter is not used.</param>
        /// <returns>The data of the specified type; or if not found, <c>null</c>.</returns>
        /// <exception cref="ArgumentException">The specified value is not a class type.</exception>
        /// <exception cref="ArgumentNullException">The specified value is a null reference.</exception>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ArgumentNullException.ThrowIfNull(value);

            ArgumentNullException.ThrowIfNull(parameter);

            Type dataType = (Type)parameter;

            if (dataType.IsClass == false)
            {
                throw new ArgumentException("The specified value is not a class type.", "parameter");
            }

            DependencyObject obj = (DependencyObject)value;
            MethodInfo findVisualAncestorDataMethod = typeof(WpfHelp).GetMethod("FindVisualAncestorData");
            MethodInfo genericFindVisualAncestorDataMethod = findVisualAncestorDataMethod.MakeGenericMethod(dataType);

            return genericFindVisualAncestorDataMethod.Invoke(null, new object[] { obj });
        }

        /// <summary>
        /// This method is not used.
        /// </summary>
        /// <param name="value">The parameter is not used.</param>
        /// <param name="targetType">The parameter is not used.</param>
        /// <param name="parameter">The parameter is not used.</param>
        /// <param name="culture">The parameter is not used.</param>
        /// <returns>The parameter is not used.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
