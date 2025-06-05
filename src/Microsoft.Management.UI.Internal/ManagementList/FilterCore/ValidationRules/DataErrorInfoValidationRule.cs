// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Provides a way to create a custom rule in order to check the validity of user input.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
<<<<<<< HEAD
    [Serializable]
    public abstract class DataErrorInfoValidationRule
=======
    public abstract class DataErrorInfoValidationRule : IDeepCloneable
>>>>>>> 625da6ff5 (Remove `OnDeserialized` and `Serializable` attributes from `Microsoft.Management.UI.Internal` project (#25548))
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
