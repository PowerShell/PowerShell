// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The BuiltinDataErrorInfoValidationRuleFactory creates default settings for the
    /// builtin FilterRules.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class DefaultFilterRuleCustomizationFactory : FilterRuleCustomizationFactory
    {
        private IPropertyValueGetter propertyValueGetter;

        /// <summary>
        /// Gets or sets a <see cref="IPropertyValueGetter"/> that can retrieve the values of properties on a given object.
        /// </summary>
        public override IPropertyValueGetter PropertyValueGetter
        {
            get
            {
                if (this.propertyValueGetter == null)
                {
                    this.propertyValueGetter = new PropertyValueGetter();
                }

                return this.propertyValueGetter;
            }

            set
            {
                ArgumentNullException.ThrowIfNull(value);

                this.propertyValueGetter = value;
            }
        }

        /// <summary>
        /// Returns a collection containing the default rules used by a PropertyValueSelectorFilterRule
        /// for type t.
        /// </summary>
        /// <typeparam name="T">
        /// The type used to determine what rules to include.
        /// </typeparam>
        /// <returns>
        /// Returns a collection of FilterRules.
        /// </returns>
        public override ICollection<FilterRule> CreateDefaultFilterRulesForPropertyValueSelectorFilterRule<T>()
        {
            Collection<FilterRule> rules = new Collection<FilterRule>();

            Type t = typeof(T);

            if (t == typeof(string))
            {
                rules.Add(new TextContainsFilterRule());
                rules.Add(new TextDoesNotContainFilterRule());
                rules.Add(new TextStartsWithFilterRule());
                rules.Add(new TextEqualsFilterRule());
                rules.Add(new TextDoesNotEqualFilterRule());
                rules.Add(new TextEndsWithFilterRule());
                rules.Add(new IsEmptyFilterRule());
                rules.Add(new IsNotEmptyFilterRule());
            }
            else if (t == typeof(bool))
            {
                rules.Add(new EqualsFilterRule<T>());
            }
            else if (t.IsEnum)
            {
                rules.Add(new EqualsFilterRule<T>());
                rules.Add(new DoesNotEqualFilterRule<T>());
            }
            else
            {
                rules.Add(new IsLessThanFilterRule<T>());
                rules.Add(new IsGreaterThanFilterRule<T>());
                rules.Add(new IsBetweenFilterRule<T>());
                rules.Add(new EqualsFilterRule<T>());
                rules.Add(new DoesNotEqualFilterRule<T>());
                rules.Add(new TextContainsFilterRule());
                rules.Add(new TextDoesNotContainFilterRule());
            }

            return rules;
        }

        /// <summary>
        /// Transfers the values from the old rule into the new rule.
        /// </summary>
        /// <param name="oldRule">
        /// The old filter rule.
        /// </param>
        /// <param name="newRule">
        /// The new filter rule.
        /// </param>
        public override void TransferValues(FilterRule oldRule, FilterRule newRule)
        {
            ArgumentNullException.ThrowIfNull(oldRule);

            ArgumentNullException.ThrowIfNull(newRule);

            if (this.TryTransferValuesAsSingleValueComparableValueFilterRule(oldRule, newRule))
            {
                return;
            }
        }

        /// <summary>
        /// Clears the values from the filter rule.
        /// </summary>
        /// <param name="rule">
        /// The rule to clear.
        /// </param>
        public override void ClearValues(FilterRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            if (this.TryClearValueFromSingleValueComparableValueFilterRule(rule))
            {
                return;
            }

            if (this.TryClearIsBetweenFilterRule(rule))
            {
                return;
            }
        }

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
        public override string GetErrorMessageForInvalidValue(string value, Type typeToParseTo)
        {
            ArgumentNullException.ThrowIfNull(typeToParseTo);

            bool isNumericType = typeToParseTo == typeof(byte)
                || typeToParseTo == typeof(sbyte)
                || typeToParseTo == typeof(short)
                || typeToParseTo == typeof(ushort)
                || typeToParseTo == typeof(int)
                || typeToParseTo == typeof(uint)
                || typeToParseTo == typeof(long)
                || typeToParseTo == typeof(ulong)
                || typeToParseTo == typeof(Single)
                || typeToParseTo == typeof(double);

            if (isNumericType)
            {
                return string.Format(CultureInfo.CurrentCulture, UICultureResources.ErrorMessageForUnparsableNumericType);
            }

            if (typeToParseTo == typeof(DateTime))
            {
                return string.Format(CultureInfo.CurrentCulture, UICultureResources.ErrorMessageForUnparsableDateTimeType, CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);
            }

            return string.Format(CultureInfo.CurrentCulture, UICultureResources.ErrorTextBoxTypeConversionErrorText, typeToParseTo.Name);
        }

        #region Private Methods

        #region Helpers

        private bool TryGetGenericParameterForComparableValueFilterRule(FilterRule rule, out Type genericParameter)
        {
            genericParameter = null;

            TextFilterRule textRule = rule as TextFilterRule;
            if (textRule != null)
            {
                genericParameter = typeof(string);
                return true;
            }

            Type ruleType = rule.GetType();
            if (!ruleType.IsGenericType)
            {
                return false;
            }

            genericParameter = ruleType.GetGenericArguments()[0];
            return true;
        }

        private object GetValueFromValidatingValue(FilterRule rule, string propertyName)
        {
            Debug.Assert(rule != null && !string.IsNullOrEmpty(propertyName), "rule and propertyname are not null");

            // NOTE: This isn't needed but OACR is complaining
            ArgumentNullException.ThrowIfNull(rule);

            Type ruleType = rule.GetType();

            PropertyInfo property = ruleType.GetProperty(propertyName);
            object validatingValue = property.GetValue(rule, null);

            property = property.PropertyType.GetProperty("Value");
            return property.GetValue(validatingValue, null);
        }

        private void SetValueOnValidatingValue(FilterRule rule, string propertyName, object value)
        {
            Debug.Assert(rule != null && !string.IsNullOrEmpty(propertyName), "rule and propertyname are not null");

            // NOTE: This isn't needed but OACR is complaining
            ArgumentNullException.ThrowIfNull(rule);

            Type ruleType = rule.GetType();

            PropertyInfo property = ruleType.GetProperty(propertyName);
            object validatingValue = property.GetValue(rule, null);

            property = property.PropertyType.GetProperty("Value");
            property.SetValue(validatingValue, value, null);
        }

        #endregion Helpers

        #region SingleValueComparableValueFilterRule

        private bool TryTransferValuesAsSingleValueComparableValueFilterRule(FilterRule oldRule, FilterRule newRule)
        {
            Debug.Assert(oldRule != null && newRule != null, "oldrule and newrule are not null");

            bool areCorrectType = this.IsSingleValueComparableValueFilterRule(oldRule) && this.IsSingleValueComparableValueFilterRule(newRule);

            if (!areCorrectType)
            {
                return false;
            }

            object value = this.GetValueFromValidatingValue(oldRule, "Value");
            this.SetValueOnValidatingValue(newRule, "Value", value);

            return true;
        }

        private bool TryClearValueFromSingleValueComparableValueFilterRule(FilterRule rule)
        {
            Debug.Assert(rule != null, "rule is not null");

            if (!this.IsSingleValueComparableValueFilterRule(rule))
            {
                return false;
            }

            this.SetValueOnValidatingValue(rule, "Value", null);

            return true;
        }

        private bool IsSingleValueComparableValueFilterRule(FilterRule rule)
        {
            Debug.Assert(rule != null, "rule is not null");

            Type genericParameter;
            if (!this.TryGetGenericParameterForComparableValueFilterRule(rule, out genericParameter))
            {
                return false;
            }

            Type ruleType = rule.GetType();
            Type baseGenericType = typeof(SingleValueComparableValueFilterRule<>);
            Type baseType = baseGenericType.MakeGenericType(genericParameter);

            return baseType.Equals(ruleType) || ruleType.IsSubclassOf(baseType);
        }

        #endregion SingleValueComparableValueFilterRule

        #region IsBetweenFilterRule

        private bool TryClearIsBetweenFilterRule(FilterRule rule)
        {
            Debug.Assert(rule != null, "rule is not null");

            if (!this.IsIsBetweenFilterRule(rule))
            {
                return false;
            }

            this.SetValueOnValidatingValue(rule, "StartValue", null);
            this.SetValueOnValidatingValue(rule, "EndValue", null);

            return true;
        }

        private bool IsIsBetweenFilterRule(FilterRule rule)
        {
            Debug.Assert(rule != null, "rule is not null");

            Type genericParameter;
            if (!this.TryGetGenericParameterForComparableValueFilterRule(rule, out genericParameter))
            {
                return false;
            }

            Type ruleType = rule.GetType();
            Type baseGenericType = typeof(IsBetweenFilterRule<>);
            Type baseType = baseGenericType.MakeGenericType(genericParameter);

            return baseType.Equals(ruleType) || ruleType.IsSubclassOf(baseType);
        }

        #endregion IsBetweenFilterRule

        #endregion Private Methods
    }
}
