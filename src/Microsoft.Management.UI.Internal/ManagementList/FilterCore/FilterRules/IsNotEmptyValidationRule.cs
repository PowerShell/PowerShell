// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The IsNotEmptyValidationRule checks a value to see if a value is not empty.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class IsNotEmptyValidationRule : DataErrorInfoValidationRule
    {
        #region Properties

        private static readonly DataErrorInfoValidationResult EmptyValueResult = new DataErrorInfoValidationResult(false, null, string.Empty);

        #endregion Properties

        #region Public Methods

        /// <summary>
        /// Determines if value is not empty.
        /// </summary>
        /// <param name="value">
        /// The value to validate.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture info to use while validating.
        /// </param>
        /// <returns>
        /// Returns true if the value is not empty, false otherwise.
        /// </returns>
        public override DataErrorInfoValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (value == null)
            {
                return EmptyValueResult;
            }

            Type t = value.GetType();

            if (typeof(string) == t)
            {
                return IsStringNotEmpty((string)value) ? DataErrorInfoValidationResult.ValidResult : EmptyValueResult;
            }
            else
            {
                return DataErrorInfoValidationResult.ValidResult;
            }
        }

        #endregion Public Methods

        internal static bool IsStringNotEmpty(string value)
        {
            return !(string.IsNullOrEmpty(value) || value.Trim().Length == 0);
        }
    }
}
