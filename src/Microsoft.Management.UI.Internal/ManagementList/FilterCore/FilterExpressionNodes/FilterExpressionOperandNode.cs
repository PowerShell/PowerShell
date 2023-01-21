// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The FilterExpressionOperandNode class is responsible for holding a
    /// FilterRule within the FilterExpression tree.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class FilterExpressionOperandNode : FilterExpressionNode
    {
        #region Properties

        /// <summary>
        /// The FilterRule to evaluate.
        /// </summary>
        public FilterRule Rule
        {
            get;
            protected set;
        }

        #endregion Properties

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the FilterExpressionOperandNode
        /// class.
        /// </summary>
        /// <param name="rule">
        /// The FilterRule to hold for evaluation.
        /// </param>
        public FilterExpressionOperandNode(FilterRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            this.Rule = rule;
        }

        #endregion Ctor

        #region Public Methods

        /// <summary>
        /// Evaluates the item against the contained FilterRule.
        /// </summary>
        /// <param name="item">
        /// The item to pass to the contained FilterRule.
        /// </param>
        /// <returns>
        /// Returns true if the contained FilterRule evaluates to
        /// true, false otherwise.
        /// </returns>
        public override bool Evaluate(object item)
        {
            Debug.Assert(this.Rule != null, "rule is not null");

            return this.Rule.Evaluate(item);
        }

        #endregion Public Methods
    }
}
