// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Converter from ViewGroup to group title string.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class ViewGroupToStringConverter : IValueConverter
    {
        /// <summary>
        /// Convert each ViewGroup into its name and its count.
        /// </summary>
        /// <param name="value">Value to be converted.</param>
        /// <param name="targetType">Type to convert the value to.</param>
        /// <param name="parameter">The conversion parameter.</param>
        /// <param name="culture">Conversion culture.</param>
        /// <returns>The converted string.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            CollectionViewGroup cvg = value as CollectionViewGroup;
            if (cvg == null)
            {
                throw new ArgumentException("value must be of type CollectionViewGroup", "value");
            }

            string name = (!string.IsNullOrEmpty(cvg.Name.ToString())) ? cvg.Name.ToString() : UICultureResources.GroupTitleNone;
            string display = string.Format(CultureInfo.CurrentCulture, $"{name} ({cvg.ItemCount})");

            return display;
        }

        /// <summary>
        /// ConvertBack is not supported.
        /// </summary>
        /// <param name="value">Value to be converted.</param>
        /// <param name="targetType">Type to convert the value to.</param>
        /// <param name="parameter">The conversion parameter.</param>
        /// <param name="culture">Conversion culture.</param>
        /// <returns>This method is not supported.</returns>
        /// <exception cref="NotSupportedException">when calling the method.</exception>
        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            System.Globalization.CultureInfo culture)
        {
            // I can't think of nothing that could be added to the exception message
            // that would be of further help
            throw new NotSupportedException();
        }
    }
}
