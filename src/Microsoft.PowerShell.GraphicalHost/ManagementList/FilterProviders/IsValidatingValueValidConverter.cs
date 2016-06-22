//-----------------------------------------------------------------------
// <copyright file="IsValidatingValueValidConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI.Internal
{
    using System;
    using System.Windows.Data;
    using System.Globalization;

    /// <summary>
    /// The IsValidatingValueValidConverter is responsible for determining if
    /// a ValidatingValueBase object is valid.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class IsValidatingValueValidConverter : IValueConverter
    {
        /// <summary>
        /// Determines if ValidatingValueBase.Error indicates
        /// if the object is valid.
        /// </summary>
        /// <param name="value">
        /// The Error string to check.
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
        /// Returns true if value is null or empty, false otherwise.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string error = (string)value;
            return String.IsNullOrEmpty(error);
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
            throw new NotSupportedException();
        }
    }
}
