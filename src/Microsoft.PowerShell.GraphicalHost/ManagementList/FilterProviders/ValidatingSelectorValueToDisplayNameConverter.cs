//-----------------------------------------------------------------------
// <copyright file="ValidatingSelectorValueToDisplayNameConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Windows.Data;

    /// <summary>
    /// The ValidatingSelectorValueToDisplayNameConverterTakes class is responsible for returning a display
    /// friendly name for the ValidatingSelectorValue class.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class ValidatingSelectorValueToDisplayNameConverter : IMultiValueConverter
    {
        /// <summary>
        /// Takes in a value and a converter and runs the converter on the value returning
        /// a display friendly name.
        /// </summary>
        /// <param name="values">
        /// The first parameter is the value to get the display name for.
        /// The second parameter is the converter.
        /// </param>
        /// <param name="targetType">
        /// Type of string.
        /// </param>
        /// <param name="parameter">
        /// The parameter is not used.
        /// </param>
        /// <param name="culture">
        /// The parameter is not used.
        /// </param>
        /// <returns>
        /// Returns a display friendly name for the first value.
        /// </returns>
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (null == values)
            {
                throw new ArgumentNullException("values");
            }

            if (2 != values.Length)
            {
                throw new ArgumentException("Two values expected", "values");
            }

            // NOTE : null values are ok.
            object input = values[0];

            IValueConverter converter = values[1] as IValueConverter;
            if (null == converter)
            {
                throw new ArgumentException("Second value should be a IValueConverter", "values");
            }

            if (typeof(string) != targetType)
            {
                throw new ArgumentException("targetType should be of type string", "targetType");
            }

            return converter.Convert(input, targetType, parameter, culture);
        }

        /// <summary>
        /// The method is not used.
        /// </summary>
        /// <param name="value">
        /// The parameter is not used.
        /// </param>
        /// <param name="targetTypes">
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
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
