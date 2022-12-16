// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The ValidatingValue class supports setting a value and validating the
    /// value.
    /// </summary>
    /// <typeparam name="T">
    /// The generic parameter.
    /// </typeparam>
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class ValidatingValue<T> : ValidatingValueBase
    {
        #region Properties

        #region Value

        private const string ValuePropertyName = "Value";

        private object value;

        /// <summary>
        /// Gets or sets a value.
        /// </summary>
        public object Value
        {
            get
            {
                return this.value;
            }

            set
            {
                this.value = value;
                this.InvalidateValidationResult();
                this.NotifyPropertyChanged(ValuePropertyName);
            }
        }

        #endregion Value

        #endregion Properties

        #region Public Methods

        /// <summary>
        /// Gets the raw value cast/transformed into
        /// type T.
        /// </summary>
        /// <returns>
        /// The cast value.
        /// </returns>
        public T GetCastValue()
        {
            if (!this.IsValid)
            {
                throw new InvalidOperationException("Cannot return cast value when value is invalid");
            }

            T castValue;
            if (!this.TryGetCastValue(this.Value, out castValue))
            {
                throw new InvalidOperationException("Validation passed yet a cast value was not retrieved");
            }

            return castValue;
        }

        #region ForceValidationUpdate

        /// <summary>
        /// Forces a validation update to occur.
        /// </summary>
        /// <remarks>
        /// The validation update occurs via signaling that
        /// the Value property has changed.
        /// </remarks>
        public void ForceValidationUpdate()
        {
            this.NotifyPropertyChanged("Value");
        }

        #endregion ForceValidationUpdate

        #region Validate

        /// <summary>
        /// Called to validate the entire object.
        /// </summary>
        /// <returns>
        /// Returns a DataErrorInfoValidationResult which indicates the validation state
        /// of the object.
        /// </returns>
        protected override DataErrorInfoValidationResult Validate()
        {
            return this.Validate(ValuePropertyName);
        }

        /// <summary>
        /// Called to validate the property with the given name.
        /// </summary>
        /// <param name="columnName">
        /// The name of the property whose error message will be checked.
        /// </param>
        /// <returns>
        /// Returns a DataErrorInfoValidationResult which indicates
        /// the validation state of the property.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="columnName"/> may only be
        /// <see cref="ValuePropertyName"/>.
        /// </exception>
        protected override DataErrorInfoValidationResult Validate(string columnName)
        {
            if (!columnName.Equals(ValuePropertyName, StringComparison.Ordinal))
            {
                throw new ArgumentOutOfRangeException("columnName");
            }

            if (this.IsValueEmpty())
            {
                return new DataErrorInfoValidationResult(false, null, string.Empty);
            }

            T castValue;
            if (!this.TryGetCastValue(this.Value, out castValue))
            {
                string errorMessage = FilterRuleCustomizationFactory.FactoryInstance.GetErrorMessageForInvalidValue(
                    this.Value.ToString(),
                    typeof(T));

                return new DataErrorInfoValidationResult(
                    false,
                    null,
                    errorMessage);
            }

            return this.EvaluateValidationRules(castValue, System.Globalization.CultureInfo.CurrentCulture);
        }

        private bool IsValueEmpty()
        {
            if (this.Value == null)
            {
                return true;
            }

            string stringValue = this.Value.ToString();
            if (string.IsNullOrEmpty(stringValue))
            {
                return true;
            }

            return false;
        }

        private bool TryGetCastValue(object rawValue, out T castValue)
        {
            castValue = default(T);

            ArgumentNullException.ThrowIfNull(rawValue);

            if (typeof(T).IsEnum)
            {
                return this.TryGetEnumValue(rawValue, out castValue);
            }

            try
            {
                castValue = (T)Convert.ChangeType(rawValue, typeof(T), CultureInfo.CurrentCulture);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        private bool TryGetEnumValue(object rawValue, out T castValue)
        {
            Debug.Assert(rawValue != null, "rawValue not null");
            Debug.Assert(typeof(T).IsEnum, "is enum");

            castValue = default(T);

            try
            {
                castValue = (T)Enum.Parse(typeof(T), rawValue.ToString(), true);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        #endregion Validate

        #endregion Public Methods
    }
}
