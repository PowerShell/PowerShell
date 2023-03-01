// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The PropertyValueSelectorFilterRule class supports filtering against a
    /// property of an object. Based on the type of the property a collection of
    /// filter rules are available to be used.
    /// </summary>
    /// <typeparam name="T">
    /// The generic parameter.
    /// </typeparam>
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class PropertyValueSelectorFilterRule<T> : SelectorFilterRule where T : IComparable
    {
        #region Properties

        /// <summary>
        /// Gets the name of the property on the item to evaluate which holds
        /// the real value which should be evaluated.
        /// </summary>
        public string PropertyName
        {
            get;
            protected set;
        }

        #endregion Properties

        #region Ctor

        /// <summary>
        /// Creates a new PropertyValueSelectorFilterRule instance.
        /// </summary>
        /// <param name="propertyName">
        /// Gets the name of the property on the item to evaluate which holds
        /// the real value which should be evaluated.
        /// </param>
        /// <param name="propertyDisplayName">
        /// The display friendly representation of the property name.
        /// </param>
        public PropertyValueSelectorFilterRule(string propertyName, string propertyDisplayName)
            : this(propertyName, propertyDisplayName, FilterRuleCustomizationFactory.FactoryInstance.CreateDefaultFilterRulesForPropertyValueSelectorFilterRule<T>())
        {
            // Empty
        }

        /// <summary>
        /// Creates a new PropertyValueSelectorFilterRule instance.
        /// </summary>
        /// <param name="propertyName">
        /// The propertyName on the item to evaluate which holds the real
        /// value which should be evaluated.
        /// </param>
        /// <param name="propertyDisplayName">
        /// The display friendly representation of the propertyName.
        /// </param>
        /// <param name="rules">
        /// The collection of available rules.
        /// </param>
        public PropertyValueSelectorFilterRule(string propertyName, string propertyDisplayName, IEnumerable<FilterRule> rules)
        {
            ArgumentException.ThrowIfNullOrEmpty(propertyName);
            ArgumentException.ThrowIfNullOrEmpty(propertyDisplayName);

            ArgumentNullException.ThrowIfNull(rules);

            this.PropertyName = propertyName;
            this.DisplayName = propertyDisplayName;

            foreach (FilterRule rule in rules)
            {
                ArgumentNullException.ThrowIfNull(rule);

                this.AvailableRules.AvailableValues.Add(rule);
            }

            this.AvailableRules.DisplayNameConverter = new FilterRuleToDisplayNameConverter();
        }

        #endregion Ctor

        #region Public Methods

        /// <summary>
        /// Evaluates whether the item is inclusive.
        /// </summary>
        /// <param name="item">
        /// The item to evaluate.
        /// </param>
        /// <returns>
        /// Returns true if the item matches the filtering criteria, false otherwise.
        /// </returns>
        public override bool Evaluate(object item)
        {
            if (!this.IsValid)
            {
                return false;
            }

            if (item == null)
            {
                return false;
            }

            T propertyValue;
            if (!this.TryGetPropertyValue(item, out propertyValue))
            {
                return false;
            }

            return this.AvailableRules.SelectedValue.Evaluate(propertyValue);
        }

        #endregion Public Methods

        #region Private Methods

        private bool TryGetPropertyValue(object item, out T propertyValue)
        {
            propertyValue = default(T);

            Debug.Assert(item != null, "item not null");

            return FilterRuleCustomizationFactory.FactoryInstance.PropertyValueGetter.TryGetPropertyValue<T>(this.PropertyName, item, out propertyValue);
        }

        #endregion Private Methods
    }
}
