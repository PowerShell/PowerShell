//-----------------------------------------------------------------------
// <copyright file="DefaultFilterRuleCustomizationFactory.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Reflection;
    using System.Globalization;

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
                if (null == value)
                {
                    throw new ArgumentNullException("value");
                }

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
            else if (t == typeof(Boolean))
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
            if (null == oldRule)
            {
                throw new ArgumentNullException("oldRule");
            }

            if (null == newRule)
            {
                throw new ArgumentNullException("newRule");
            }

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
            if (null == rule)
            {
                throw new ArgumentNullException("rule");
            }

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
            if (null == typeToParseTo)
            {
                throw new ArgumentNullException("typeToParseTo");
            }

            bool isNumericType = false
                || typeToParseTo == typeof(Byte)
                || typeToParseTo == typeof(SByte)
                || typeToParseTo == typeof(Int16)
                || typeToParseTo == typeof(UInt16)
                || typeToParseTo == typeof(Int32)
                || typeToParseTo == typeof(UInt32)
                || typeToParseTo == typeof(Int64)
                || typeToParseTo == typeof(UInt64)
                || typeToParseTo == typeof(Single)
                || typeToParseTo == typeof(Double);

            if (isNumericType)
            {
                return String.Format(CultureInfo.CurrentCulture, UICultureResources.ErrorMessageForUnparsableNumericType);
            }

            if (typeToParseTo == typeof(DateTime))
            {
                return String.Format(CultureInfo.CurrentCulture, UICultureResources.ErrorMessageForUnparsableDateTimeType, CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);
            }

            return String.Format(CultureInfo.CurrentCulture, UICultureResources.ErrorTextBoxTypeConversionErrorText, typeToParseTo.Name);
        }

        #region Private Methods

        #region Helpers

        private bool TryGetGenericParameterForComparableValueFilterRule(FilterRule rule, out Type genericParameter)
        {
            genericParameter = null;

            TextFilterRule textRule = rule as TextFilterRule;
            if (null != textRule)
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
            Debug.Assert(null != rule && !String.IsNullOrEmpty(propertyName));

            // NOTE: This isn't needed but OACR is complaining
            if (null == rule)
            {
                throw new ArgumentNullException("rule");
            }

            Type ruleType = rule.GetType();

            PropertyInfo property = ruleType.GetProperty(propertyName);
            object validatingValue = property.GetValue(rule, null);

            property = property.PropertyType.GetProperty("Value");
            return property.GetValue(validatingValue, null);
        }

        private void SetValueOnValidatingValue(FilterRule rule, string propertyName, object value)
        {
            Debug.Assert(null != rule && !String.IsNullOrEmpty(propertyName));

            // NOTE: This isn't needed but OACR is complaining
            if (null == rule)
            {
                throw new ArgumentNullException("rule");
            }

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
            Debug.Assert(null != oldRule && null != newRule);

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
            Debug.Assert(null != rule);

            if (!this.IsSingleValueComparableValueFilterRule(rule))
            {
                return false;
            }

            this.SetValueOnValidatingValue(rule, "Value", null);

            return true;
        }

        private bool IsSingleValueComparableValueFilterRule(FilterRule rule)
        {
            Debug.Assert(null != rule);

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
            Debug.Assert(null != rule);

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
            Debug.Assert(null != rule);

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
