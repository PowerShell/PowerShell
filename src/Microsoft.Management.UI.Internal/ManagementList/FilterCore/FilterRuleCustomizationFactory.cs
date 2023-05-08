// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The FilterRuleCustomizationFactory class provides a central location
    /// a return an abstract factory which creates the standard settings and rules
    /// used by the builtin FilterRules.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public abstract class FilterRuleCustomizationFactory
    {
        private static FilterRuleCustomizationFactory factoryInstance;

        /// <summary>
        /// Gets or sets a factory instance which is used by builtin
        /// filter rules.
        /// </summary>
        public static FilterRuleCustomizationFactory FactoryInstance
        {
            get
            {
                Debug.Assert(factoryInstance != null, "factoryInstance not null");
                return factoryInstance;
            }

            set
            {
                ArgumentNullException.ThrowIfNull(value);

                factoryInstance = value;
            }
        }

        /// <summary>
        /// Initializes the static state of the DataErrorInfoValidationRuleFactory class.
        /// </summary>
        static FilterRuleCustomizationFactory()
        {
            FactoryInstance = new DefaultFilterRuleCustomizationFactory();
        }

        /// <summary>
        /// Gets or sets a <see cref="IPropertyValueGetter"/> that can retrieve the values of properties on a given object.
        /// </summary>
        public abstract IPropertyValueGetter PropertyValueGetter
        {
            get;
            set;
        }

        /// <summary>
        /// Returns a collection containing the default rules used by a PropertyValueSelectorFilterRule
        /// for type T.
        /// </summary>
        /// <typeparam name="T">
        /// The type used to determine what rules to include.
        /// </typeparam>
        /// <returns>
        /// Returns a collection of FilterRules.
        /// </returns>
        public abstract ICollection<FilterRule> CreateDefaultFilterRulesForPropertyValueSelectorFilterRule<T>() where T : IComparable;

        /// <summary>
        /// Transfers the values from the old rule into the new rule.
        /// </summary>
        /// <param name="oldRule">
        /// The old filter rule.
        /// </param>
        /// <param name="newRule">
        /// The new filter rule.
        /// </param>
        public abstract void TransferValues(FilterRule oldRule, FilterRule newRule);

        /// <summary>
        /// Clears the values from the filter rule.
        /// </summary>
        /// <param name="rule">
        /// The rule to clear.
        /// </param>
        public abstract void ClearValues(FilterRule rule);

        /// <summary>
        /// Get an error message to display to a user when they
        /// provide a string value that cannot be parsed to type
        /// typeToParseTo.
        /// </summary>
        /// <param name="value">
        /// The value entered by the user.
        /// </param>
        /// <param name="typeToParseTo">
        /// The desired type to parse value to.
        /// </param>
        /// <returns>
        /// An error message to a user to explain how they can
        /// enter a valid value.
        /// </returns>
        public abstract string GetErrorMessageForInvalidValue(string value, Type typeToParseTo);
    }
}
