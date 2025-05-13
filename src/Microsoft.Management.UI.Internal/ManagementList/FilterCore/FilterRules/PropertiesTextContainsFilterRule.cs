// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Represents a filter rule that searches for text within properties on an object.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    [Serializable]
    public class PropertiesTextContainsFilterRule : TextFilterRule
    {
        private static readonly string TextContainsCharactersRegexPattern = "{0}";
        private static readonly string TextContainsWordsRegexPattern = WordBoundaryRegexPattern + TextContainsCharactersRegexPattern + WordBoundaryRegexPattern;

        private Regex cachedRegex;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertiesTextContainsFilterRule"/> class.
        /// </summary>
        public PropertiesTextContainsFilterRule()
        {
            this.PropertyNames = new List<string>();
            this.EvaluationResultInvalidated += this.PropertiesTextContainsFilterRule_EvaluationResultInvalidated;
        }

        /// <summary>
        /// Initializes a new instance of the  <see cref="PropertiesTextContainsFilterRule"/> class.
        /// </summary>
        /// <param name="source">The source to initialize from.</param>
        public PropertiesTextContainsFilterRule(PropertiesTextContainsFilterRule source)
            : base(source)
        {
            this.PropertyNames = new List<string>(source.PropertyNames);
            this.EvaluationResultInvalidated += this.PropertiesTextContainsFilterRule_EvaluationResultInvalidated;
        }

        /// <summary>
        /// Gets a collection of the names of properties to search in.
        /// </summary>
        public ICollection<string> PropertyNames
        {
            get;
            private set;
        }

        /// <summary>
        /// Evaluates whether the specified properties on <paramref name="item"/> contain the current value.
        /// </summary>
        /// <param name="item">The item to evaluate.</param>
        /// <returns><c>true</c> if <paramref name="item"/> is not <c>null</c>, the current value is valid, and the specified properties on <paramref name="item"/> contain the current value; otherwise, <c>false</c>.</returns>
        public override bool Evaluate(object item)
        {
            if (item == null)
            {
                return false;
            }

            if (!this.IsValid)
            {
                return false;
            }

            foreach (string propertyName in this.PropertyNames)
            {
                object propertyValue;

                if (!FilterRuleCustomizationFactory.FactoryInstance.PropertyValueGetter.TryGetPropertyValue(propertyName, item, out propertyValue))
                {
                    continue;
                }

                if (propertyValue != null)
                {
                    string data = propertyValue.ToString();

                    if (this.Evaluate(data))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Evaluates whether the specified data contains the current value.
        /// </summary>
        /// <param name="data">The data to evaluate.</param>
        /// <returns><c>true</c> if <paramref name="data"/> contains the current value; otherwise, <c>false</c>.</returns>
        protected override bool Evaluate(string data)
        {
            if (this.cachedRegex == null)
            {
                this.UpdateCachedRegex();
            }

            return this.cachedRegex.IsMatch(data);
        }

        /// <summary>
        /// Called when the evaluation result is invalidated.
        /// Updates the cached Regex pattern.
        /// </summary>
        protected virtual void OnEvaluationResultInvalidated()
        {
            this.UpdateCachedRegex();
        }

        /// <summary>
        /// Updates the cached Regex with the current value.
        /// If the current value is invalid, the Regex will not be updated because it will not be evaluated.
        /// </summary>
        private void UpdateCachedRegex()
        {
            if (this.IsValid)
            {
                var parsedPattern = this.GetRegexPattern(TextContainsCharactersRegexPattern, TextContainsWordsRegexPattern);

                this.cachedRegex = new Regex(parsedPattern, this.GetRegexOptions());
            }
        }

        private void PropertiesTextContainsFilterRule_EvaluationResultInvalidated(object sender, EventArgs e)
        {
            this.OnEvaluationResultInvalidated();
        }

        [OnDeserialized]
        private void Initialize(StreamingContext context)
        {
            this.EvaluationResultInvalidated += this.PropertiesTextContainsFilterRule_EvaluationResultInvalidated;
        }
    }
}
