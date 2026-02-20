// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The SelectorFilterRule represents a rule composed of other rules.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    [Serializable]
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
                return this.AvailableRules.IsValid && this.AvailableRules.SelectedValue.IsValid;
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
        /// Initializes a new instance of the <see cref="SelectorFilterRule"/> class.
        /// </summary>
        public SelectorFilterRule()
        {
            this.AvailableRules = new ValidatingSelectorValue<FilterRule>();
            this.AvailableRules.SelectedValueChanged += this.AvailableRules_SelectedValueChanged;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectorFilterRule"/> class.
        /// </summary>
        /// <param name="source">The source to initialize from.</param>
        public SelectorFilterRule(SelectorFilterRule source)
            : base(source)
        {
            this.AvailableRules = (ValidatingSelectorValue<FilterRule>)source.AvailableRules.DeepClone();
            this.AvailableRules.SelectedValueChanged += this.AvailableRules_SelectedValueChanged;
            this.AvailableRules.SelectedValue.EvaluationResultInvalidated += this.SelectedValue_EvaluationResultInvalidated;
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

            oldValue.EvaluationResultInvalidated -= this.SelectedValue_EvaluationResultInvalidated;
            newValue.EvaluationResultInvalidated += this.SelectedValue_EvaluationResultInvalidated;

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
            this.AvailableRules.SelectedValueChanged += this.AvailableRules_SelectedValueChanged;
            this.AvailableRules.SelectedValue.EvaluationResultInvalidated += this.SelectedValue_EvaluationResultInvalidated;
        }

        private void AvailableRules_SelectedValueChanged(object sender, PropertyChangedEventArgs<FilterRule> e)
        {
            this.OnSelectedValueChanged(e.OldValue, e.NewValue);
        }

        #endregion Private Methods
    }
}
