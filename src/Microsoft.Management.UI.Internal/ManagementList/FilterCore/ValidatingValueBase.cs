// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The ValidatingValueBase class provides basic services for base
    /// classes to support validation via the IDataErrorInfo interface.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    [Serializable]
    public abstract class ValidatingValueBase : IDataErrorInfo, INotifyPropertyChanged
    {
        #region Properties

        #region ValidationRules

        private List<DataErrorInfoValidationRule> validationRules = new List<DataErrorInfoValidationRule>();
        private ReadOnlyCollection<DataErrorInfoValidationRule> readonlyValidationRules;
        private bool isValidationRulesCollectionDirty = true;

        [field: NonSerialized]
        private DataErrorInfoValidationResult cachedValidationResult;

        /// <summary>
        /// Gets the collection of validation rules used to validate the value.
        /// </summary>
        public ReadOnlyCollection<DataErrorInfoValidationRule> ValidationRules
        {
            get
            {
                if (this.isValidationRulesCollectionDirty)
                {
                    this.readonlyValidationRules = new ReadOnlyCollection<DataErrorInfoValidationRule>(this.validationRules);
                }

                return this.readonlyValidationRules;
            }
        }

        #endregion ValidationRules

        #region IsValid

        /// <summary>
        /// Gets a value indicating whether the value is valid.
        /// </summary>
        public bool IsValid
        {
            get
            {
                return this.GetValidationResult().IsValid;
            }
        }

        #endregion IsValid

        #region IDataErrorInfo implementation
        #region Item

        /// <summary>
        /// Gets the error message for the property with the given name.
        /// </summary>
        /// <param name="columnName">
        /// The name of the property whose error message will be checked.
        /// </param>
        /// <returns>
        /// The error message for the property, or an empty string ("") if
        /// the property is valid.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="columnName"/> is invalid.
        /// </exception>
        public string this[string columnName]
        {
            get
            {
                ArgumentException.ThrowIfNullOrEmpty(columnName);

                this.UpdateValidationResult(columnName);
                return this.GetValidationResult().ErrorMessage;
            }
        }

        #endregion Item

        #region Error

        /// <summary>
        /// Gets an error message indicating what is wrong with this object.
        /// </summary>
        public string Error
        {
            get
            {
                DataErrorInfoValidationResult result = this.GetValidationResult();
                return (!result.IsValid) ? result.ErrorMessage : string.Empty;
            }
        }

        #endregion Error
        #endregion IDataErrorInfo implementation

        #endregion Properties

        #region Events

        #region PropertyChanged

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        /// <remarks>
        /// The listeners attached to this event are not serialized.
        /// </remarks>
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion PropertyChanged

        #endregion Events

        #region Public Methods

        #region AddValidationRule

        /// <summary>
        /// Adds a validation rule to the ValidationRules collection.
        /// </summary>
        /// <param name="rule">The validation rule to add.</param>
        public void AddValidationRule(DataErrorInfoValidationRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            this.validationRules.Add(rule);

            this.isValidationRulesCollectionDirty = true;
            this.NotifyPropertyChanged("ValidationRules");
        }

        #endregion AddValidationRule

        #region RemoveValidationRule

        /// <summary>
        /// Removes a validation rule from the ValidationRules collection.
        /// </summary>
        /// <param name="rule">The rule to remove.</param>
        public void RemoveValidationRule(DataErrorInfoValidationRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            this.validationRules.Remove(rule);

            this.isValidationRulesCollectionDirty = true;
            this.NotifyPropertyChanged("ValidationRules");
        }

        #endregion RemoveValidationRule

        #region ClearValidationRules

        /// <summary>
        /// Clears the ValidationRules collection.
        /// </summary>
        public void ClearValidationRules()
        {
            this.validationRules.Clear();

            this.isValidationRulesCollectionDirty = true;
            this.NotifyPropertyChanged("ValidationRules");
        }

        #endregion ClearValidationRules

        #region Validate

        /// <summary>
        /// Called to validate the entire object.
        /// </summary>
        /// <returns>
        /// Returns a DataErrorInfoValidationResult which indicates the validation state
        /// of the object.
        /// </returns>
        protected abstract DataErrorInfoValidationResult Validate();

        /// <summary>
        /// Called to validate the property with the given name.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property whose error message will be checked.
        /// </param>
        /// <returns>
        /// Returns a DataErrorInfoValidationResult which indicates the validation state
        /// of the property.
        /// </returns>
        protected abstract DataErrorInfoValidationResult Validate(string propertyName);

        #endregion Validate

        #region EvaluateValidationRules

        internal DataErrorInfoValidationResult EvaluateValidationRules(object value, System.Globalization.CultureInfo cultureInfo)
        {
            foreach (DataErrorInfoValidationRule rule in this.ValidationRules)
            {
                DataErrorInfoValidationResult result = rule.Validate(value, cultureInfo);
                if (result == null)
                {
                    throw new InvalidOperationException(string.Create(CultureInfo.CurrentCulture, $"DataErrorInfoValidationResult not returned by ValidationRule: {rule}"));
                }

                if (!result.IsValid)
                {
                    return result;
                }
            }

            return DataErrorInfoValidationResult.ValidResult;
        }

        #endregion EvaluateValidationRules

        #region InvalidateValidationResult

        /// <summary>
        /// Calling InvalidateValidationResult causes the
        /// Validation to be reevaluated.
        /// </summary>
        protected void InvalidateValidationResult()
        {
            this.ClearValidationResult();
        }

        #endregion InvalidateValidationResult

        #region NotifyPropertyChanged

        /// <summary>
        /// Notifies listeners that a property has changed.
        /// </summary>
        /// <param name="propertyName">
        /// The propertyName which has changed.
        /// </param>
        protected void NotifyPropertyChanged(string propertyName)
        {
            #pragma warning disable IDE1005 // IDE1005: Delegate invocation can be simplified.
            PropertyChangedEventHandler eh = this.PropertyChanged;

            if (eh != null)
            {
                eh(this, new PropertyChangedEventArgs(propertyName));
            }
            #pragma warning restore IDE1005
        }

        #endregion NotifyPropertyChanged

        #endregion Public Methods

        #region Private Methods

        #region GetValidationResult

        private DataErrorInfoValidationResult GetValidationResult()
        {
            if (this.cachedValidationResult == null)
            {
                this.UpdateValidationResult();
            }

            return this.cachedValidationResult;
        }

        #endregion GetValidationResult

        #region UpdateValidationResult

        private void UpdateValidationResult()
        {
            this.cachedValidationResult = this.Validate();
            this.NotifyValidationResultUpdated();
        }

        private void UpdateValidationResult(string columnName)
        {
            this.cachedValidationResult = this.Validate(columnName);
            this.NotifyValidationResultUpdated();
        }

        private void NotifyValidationResultUpdated()
        {
            Debug.Assert(this.cachedValidationResult != null, "not null");
            this.NotifyPropertyChanged("IsValid");
            this.NotifyPropertyChanged("Error");
        }

        #endregion UpdateValidationResult

        #region ClearValidationResult

        private void ClearValidationResult()
        {
            this.cachedValidationResult = null;
        }

        #endregion ClearValidationResult

        #endregion Private Methods
    }
}
