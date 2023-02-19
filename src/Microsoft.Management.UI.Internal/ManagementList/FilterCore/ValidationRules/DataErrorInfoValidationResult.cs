// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Windows.Controls;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The DataErrorInfoValidationResult supports reporting validation result
    /// data needed for the IDataErrorInfo interface.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class DataErrorInfoValidationResult : ValidationResult
    {
        #region Properties

        /// <summary>
        /// Gets a value indicating whether the error should
        /// be presented to the user.
        /// </summary>
        public bool IsUserVisible
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value used to communicate what the error is.
        /// </summary>
        public string ErrorMessage
        {
            get;
            private set;
        }

        private static readonly DataErrorInfoValidationResult valid = new DataErrorInfoValidationResult(true, null, string.Empty);

        /// <summary>
        /// Get an instance of DataErrorInfoValidationResult that corresponds
        /// to a valid result.
        /// </summary>
        public static new DataErrorInfoValidationResult ValidResult
        {
            get
            {
                return valid;
            }
        }

        #endregion Properties

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the DataErrorInfoValidationResult class.
        /// </summary>
        /// <param name="isValid">
        /// Indicates whether the value checked against the
        /// DataErrorInfoValidationResult is valid
        /// </param>
        /// <param name="errorContent">
        /// Information about the invalidity.
        /// </param>
        /// <param name="errorMessage">
        /// The error message to display to the user. If the result is invalid
        /// and the error message is empty (""), the result will be treated as
        /// invalid but no error will be presented to the user.
        /// </param>
        public DataErrorInfoValidationResult(bool isValid, object errorContent, string errorMessage)
            : base(isValid, errorContent)
        {
            this.IsUserVisible = !string.IsNullOrEmpty(errorMessage);
            this.ErrorMessage = errorMessage ?? string.Empty;
        }

        #endregion Ctor
    }
}
