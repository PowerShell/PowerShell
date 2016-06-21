//-----------------------------------------------------------------------
// <copyright file="IsEqualConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Used to determine whether two objects are equal.
// </summary>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Windows.Data;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;

    /// <summary>
    /// Takes two objects and determines whether they are equal.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class IsEqualConverter : IMultiValueConverter
    {
        /// <summary>
        /// Takes two items and determines whether they are equal.
        /// </summary>
        /// <param name="values">
        /// Two objects of any type.
        /// </param>
        /// <param name="targetType">The parameter is not used.</param>
        /// <param name="parameter">The parameter is not used.</param>
        /// <param name="culture">The parameter is not used.</param>
        /// <returns>
        /// True if-and-only-if the two objects are equal per Object.Equals().
        /// Null is equal only to null.
        /// </returns>
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            if (2 != values.Length)
            {
                throw new ArgumentException("Two values expected", "values");
            }

            object item1 = values[0];
            object item2 = values[1];

            if (null == item1)
            {
                return (null == item2);
            }

            if (null == item2)
            {
                return false;
            }

            bool equal = item1.Equals(item2);
            return equal;
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
