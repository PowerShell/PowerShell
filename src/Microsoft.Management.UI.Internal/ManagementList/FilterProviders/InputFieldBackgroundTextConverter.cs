// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The InputFieldBackgroundTextConverter is responsible for determining the
    /// correct background text to display for a particular type of data.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class InputFieldBackgroundTextConverter : IValueConverter
    {
        private static readonly Type ValidatingValueGenericType = typeof(ValidatingValue<>);

        /// <summary>
        /// Converts a value of type ValidatingValue of T into a background string
        /// which provides a hint to the end user (e.g. Empty, M/d/yy).
        /// </summary>
        /// <param name="value">
        /// A value of type ValidatingValue.
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
        /// Returns a background string for value.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ArgumentNullException.ThrowIfNull(value);

            Type inputType = null;
            if (this.IsOfTypeValidatingValue(value))
            {
                inputType = this.GetGenericParameter(value, culture);
            }

            return this.GetBackgroundTextForType(inputType);
        }

        /// <summary>
        /// This method is not used.
        /// </summary>
        /// <param name="value">The parameter is not used.</param>
        /// <param name="targetType">The parameter is not used.</param>
        /// <param name="parameter">The parameter is not used.</param>
        /// <param name="culture">The parameter is not used.</param>
        /// <returns>The parameter is not used.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #region Helpers

        private bool IsOfTypeValidatingValue(object value)
        {
            Debug.Assert(value != null, "not null");

            Type type = value.GetType();
            if (type.IsGenericType == false)
            {
                return false;
            }

            type = type.GetGenericTypeDefinition();

            return type == ValidatingValueGenericType;
        }

        private Type GetGenericParameter(object value, CultureInfo culture)
        {
            Debug.Assert(value != null, "not null");
            Debug.Assert(this.IsOfTypeValidatingValue(value), "not null");

            return value.GetType().GetGenericArguments()[0];
        }

        private object GetBackgroundTextForType(Type inputType)
        {
            if (typeof(DateTime) == inputType)
            {
                return CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
            }
            else
            {
                // <Empty>
                return XamlLocalizableResources.AutoResXGen_FilterRulePanel_BackgroundText_200;
            }
        }

        #endregion Helpers
    }
}
