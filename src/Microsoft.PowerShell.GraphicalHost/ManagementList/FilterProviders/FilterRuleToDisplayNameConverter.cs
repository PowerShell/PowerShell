//-----------------------------------------------------------------------
// <copyright file="FilterRuleToDisplayNameConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI.Internal
{
    using System;
    using System.Windows;
    using System.Windows.Data;

    /// <summary>
    /// The FilterRuleToDisplayNameConverter is responsible for converting
    /// a FilterRule value to its DisplayName.
    /// </summary>
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class FilterRuleToDisplayNameConverter : IValueConverter
    {
        /// <summary>
        /// Converts a FilterRule value to its DisplayName.
        /// </summary>
        /// <param name="value">
        /// A FilterRule.
        /// </param>
        /// <param name="targetType">
        /// Type of String.
        /// </param>
        /// <param name="parameter">
        /// The parameter is not used.
        /// </param>
        /// <param name="culture">
        /// The parameter is not used.
        /// </param>
        /// <returns>
        /// The display name of the FilterRule.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (null == value)
            {
                return DependencyProperty.UnsetValue;
            }


            FilterRule rule = value as FilterRule;
            if (null == rule)
            {
                throw new ArgumentException("value of type FilterRule expected.", "value");
            }

            return rule.DisplayName;
        }

        /// <summary>
        /// The method is not used.
        /// </summary>
        /// <param name="value">
        /// The parameter is not used.
        /// </param>
        /// <param name="targetType">
        /// The parameter is not used.
        /// </param>
        /// <param name="parameter">
        /// The parameter is not used.
        /// </param>
        /// <param name="culture">
        /// The parameter is not used.
        /// </param>
        /// <returns>
        /// The method does not return a value.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
