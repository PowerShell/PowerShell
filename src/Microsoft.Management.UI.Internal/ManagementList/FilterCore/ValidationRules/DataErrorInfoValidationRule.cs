// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Provides a way to create a custom rule in order to check the validity of user input.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public abstract class DataErrorInfoValidationRule
    {
        /// <summary>
        /// When overridden in a derived class, performs validation checks on a value.
        /// </summary>
        /// <param name="value">
        /// The value to check.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use in this rule.
        /// </param>
        /// <returns>
        /// A DataErrorInfoValidationResult object.
        /// </returns>
        public abstract DataErrorInfoValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo);
    }
}
