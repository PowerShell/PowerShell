// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The ComparableValueFilterRule provides support for derived classes
    /// that evaluate against IComparable values.
    /// </summary>
    /// <typeparam name="T">
    /// The generic parameter.
    /// </typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public abstract class ComparableValueFilterRule<T> : FilterRule where T : IComparable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ComparableValueFilterRule{T}"/> class.
        /// </summary>
        protected ComparableValueFilterRule()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComparableValueFilterRule{T}"/> class.
        /// </summary>
        /// <param name="source">The source to initialize from.</param>
        protected ComparableValueFilterRule(ComparableValueFilterRule<T> source)
            : base(source)
        {
            this.DefaultNullValueEvaluation = source.DefaultNullValueEvaluation;
        }

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether null objects passed to Evaluate will
        /// evaluate to true or false.
        /// </summary>
        protected bool DefaultNullValueEvaluation
        {
            get;
            set;
        }

        #endregion Properties

        #region Public Methods

        /// <summary>
        /// Determines if item matches a derived classes criteria.
        /// </summary>
        /// <param name="item">
        /// The item to match evaluate.
        /// </param>
        /// <returns>
        /// Returns true if the item matches, false otherwise.
        /// </returns>
        public override bool Evaluate(object item)
        {
            if (item == null)
            {
                return this.DefaultNullValueEvaluation;
            }

            if (!this.IsValid)
            {
                return false;
            }

            T castItem;
            if (!FilterUtilities.TryCastItem<T>(item, out castItem))
            {
                return false;
            }

            return this.Evaluate(castItem);
        }

        /// <summary>
        /// Determines if item matches a derived classes criteria.
        /// </summary>
        /// <param name="data">
        /// The item to match evaluate.
        /// </param>
        /// <returns>
        /// Returns true if the item matches, false otherwise.
        /// </returns>
        protected abstract bool Evaluate(T data);

        #endregion Public Methods
    }
}
