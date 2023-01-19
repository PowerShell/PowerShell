// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows.Data;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The ValidatingSelectorValue class provides support for selecting
    /// a value from a collection of available values.
    /// </summary>
    /// <typeparam name="T">
    /// The generic parameter.
    /// </typeparam>
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class ValidatingSelectorValue<T> : ValidatingValueBase
    {
        #region Properties

        #region Consts

        private static readonly DataErrorInfoValidationResult InvalidSelectionResult = new DataErrorInfoValidationResult(false, null, UICultureResources.ValidatingSelectorValueOutOfBounds);

        #endregion Consts

        #region AvailableValues

        private List<T> availableValues = new List<T>();

        /// <summary>
        /// Gets the collection of values available for selection.
        /// </summary>
        public IList<T> AvailableValues
        {
            get
            {
                return this.availableValues;
            }
        }

        #endregion AvailableValues

        #region SelectedIndex

        private const string SelectedIndexPropertyName = "SelectedIndex";

        private int selectedIndex;

        /// <summary>
        /// Gets or sets the index of the currently selected item or
        /// returns negative one (-1) if the selection is empty.
        /// </summary>
        /// <remarks>
        /// If you set SelectedIndex to a value less that -1, an
        /// ArgumentException is thrown. If you set SelectedIndex to a
        /// value equal or greater than the number of child elements,
        /// the value is ignored.
        /// </remarks>
        public int SelectedIndex
        {
            get
            {
                return this.IsIndexWithinBounds(this.selectedIndex) ? this.selectedIndex : -1;
            }

            set
            {
                if (value < -1)
                {
                    throw new ArgumentException("value out of range", "value");
                }

                if (value < this.availableValues.Count)
                {
                    var oldValue = this.selectedIndex;

                    this.selectedIndex = value;

                    this.InvalidateValidationResult();
                    this.NotifySelectedValueChanged(oldValue, this.selectedIndex);
                    this.NotifyPropertyChanged(SelectedIndexPropertyName);
                    this.NotifyPropertyChanged(SelectedValuePropertyName);
                }
            }
        }

        #endregion SelectedIndex

        #region SelectedValue

        private const string SelectedValuePropertyName = "SelectedValue";

        /// <summary>
        /// Gets the item within AvailableValues at the offset indicated
        /// by SelectedIndex or returns default(T) if the selection is empty.
        /// </summary>
        public T SelectedValue
        {
            get
            {
                if (!this.IsIndexWithinBounds(this.SelectedIndex))
                {
                    return default(T);
                }

                return this.availableValues[this.SelectedIndex];
            }
        }

        #endregion SelectedValue

        #region DisplayNameConverter

        private IValueConverter displayNameConverter;

        /// <summary>
        /// Gets or sets the converter used to display a friendly
        /// value to the user.
        /// </summary>
        public IValueConverter DisplayNameConverter
        {
            get
            {
                return this.displayNameConverter;
            }

            set
            {
                if (value != null && !value.GetType().IsSerializable)
                {
                    throw new ArgumentException("The DisplayNameConverter must be serializable.", "value");
                }

                this.displayNameConverter = value;
            }
        }

        #endregion DisplayNameConverter

        #endregion Properties

        #region Events

        /// <summary>
        /// Notifies listeners that the selected value has changed.
        /// </summary>
        [field: NonSerialized]
        public event EventHandler<PropertyChangedEventArgs<T>> SelectedValueChanged;

        #endregion Events

        #region Public Methods

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
            return this.Validate(SelectedIndexPropertyName);
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
        /// <see cref="SelectedIndexPropertyName"/>.
        /// </exception>
        protected override DataErrorInfoValidationResult Validate(string columnName)
        {
            if (!columnName.Equals(SelectedIndexPropertyName, StringComparison.CurrentCulture))
            {
                throw new ArgumentException(string.Create(CultureInfo.CurrentCulture, $"{columnName} is not a valid column name."), "columnName");
            }

            if (!this.IsIndexWithinBounds(this.SelectedIndex))
            {
                return InvalidSelectionResult;
            }

            return this.EvaluateValidationRules(this.SelectedValue, System.Globalization.CultureInfo.CurrentCulture);
        }

        #endregion Validate

        #region NotifySelectedValueChanged

        /// <summary>
        /// Notifies listeners that the selected value with the available
        /// values has changed.
        /// </summary>
        /// <param name="oldValue">
        /// The previous selected value.
        /// </param>
        /// <param name="newValue">
        /// The current selected value.
        /// </param>
        protected void NotifySelectedValueChanged(T oldValue, T newValue)
        {
            #pragma warning disable IDE1005 // IDE1005: Delegate invocation can be simplified.
            EventHandler<PropertyChangedEventArgs<T>> eh = this.SelectedValueChanged;

            if (eh != null)
            {
                eh(this, new PropertyChangedEventArgs<T>(oldValue, newValue));
            }
            #pragma warning restore IDE1005
        }

        #endregion NotifySelectedValueChanged

        #endregion Public Methods

        #region Private Methods

        #region IsIndexWithinBounds

        private bool IsIndexWithinBounds(int value)
        {
            return value >= 0 && value < this.AvailableValues.Count;
        }

        #endregion IsIndexWithinBounds

        private void NotifySelectedValueChanged(int oldIndex, int newIndex)
        {
            if (this.IsIndexWithinBounds(oldIndex) && this.IsIndexWithinBounds(newIndex))
            {
                this.NotifySelectedValueChanged(this.availableValues[oldIndex], this.availableValues[newIndex]);
            }
        }

        #endregion Private Methods
    }
}
