//-----------------------------------------------------------------------
// <copyright file="SelectorFilterRule.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI.Internal
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// The SelectorFilterRule represents a rule composed of other rules.
    /// </summary>
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class SelectorFilterRule : FilterRule
    {
        #region Properties

        /// <summary>
        /// Gets a value indicating whether the rule can be evaluated.
        /// </summary>
        public override bool IsValid
        {
            get
            {
                return (this.AvailableRules.IsValid && this.AvailableRules.SelectedValue.IsValid);
            }
        }

        /// <summary>
        /// Gets the collection of available rules.
        /// </summary>
        public ValidatingSelectorValue<FilterRule> AvailableRules
        {
            get;
            protected set;
        }

        #endregion Properties

        #region Ctor

        /// <summary>
        /// Creates a new SelectorFilterRule instance.
        /// </summary>
        public SelectorFilterRule()
        {
            this.AvailableRules = new ValidatingSelectorValue<FilterRule>();
            this.AvailableRules.SelectedValueChanged += new EventHandler<PropertyChangedEventArgs<FilterRule>>(this.AvailableRules_SelectedValueChanged);
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

            return this.AvailableRules.SelectedValue.Evaluate(item);
        }

        /// <summary>
        /// Called when the SelectedValue within AvailableRules changes.
        /// </summary>
        /// <param name="oldValue">
        /// The old FilterRule.
        /// </param>
        /// <param name="newValue">
        /// The new FilterRule.
        /// </param>
        protected void OnSelectedValueChanged(FilterRule oldValue, FilterRule newValue)
        {
            FilterRuleCustomizationFactory.FactoryInstance.ClearValues(newValue);
            FilterRuleCustomizationFactory.FactoryInstance.TransferValues(oldValue, newValue);
            FilterRuleCustomizationFactory.FactoryInstance.ClearValues(oldValue);

            newValue.EvaluationResultInvalidated += new EventHandler(this.SelectedValue_EvaluationResultInvalidated);
            oldValue.EvaluationResultInvalidated -= new EventHandler(this.SelectedValue_EvaluationResultInvalidated);

            this.NotifyEvaluationResultInvalidated();
        }

        private void SelectedValue_EvaluationResultInvalidated(object sender, EventArgs e)
        {
            this.NotifyEvaluationResultInvalidated();
        }

        #endregion Public Methods

        #region Private Methods

        [OnDeserialized]
        private void Initialize(StreamingContext context)
        {
            this.AvailableRules.SelectedValueChanged += new EventHandler<PropertyChangedEventArgs<FilterRule>>(this.AvailableRules_SelectedValueChanged);
            this.AvailableRules.SelectedValue.EvaluationResultInvalidated += new EventHandler(this.SelectedValue_EvaluationResultInvalidated);
        }

        private void AvailableRules_SelectedValueChanged(object sender, PropertyChangedEventArgs<FilterRule> e)
        {
            this.OnSelectedValueChanged(e.OldValue, e.NewValue);
        }


        #endregion Private Methods
    }
}
