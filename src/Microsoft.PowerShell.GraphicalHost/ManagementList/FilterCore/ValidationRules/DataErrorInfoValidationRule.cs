//-----------------------------------------------------------------------
// <copyright file="DataErrorInfoValidationRule.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI.Internal
{
    using System;

    /// <summary>
    /// Provides a way to create a custom rule in order to check the validity of user input.
    /// </summary>
    [Serializable]
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
