// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Data;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The base class for all filtering rules.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    [Serializable]
    public abstract class FilterRule : IEvaluate, IDeepCloneable
    {
        /// <summary>
        /// Gets a value indicating whether the FilterRule can be
        /// evaluated in its current state.
        /// </summary>
        public virtual bool IsValid
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a display friendly name for the FilterRule.
        /// </summary>
        public string DisplayName
        {
            get;
            protected set;
        }

        /// <summary>
        /// Initializes a new instance of the FilterRule class.
        /// </summary>
        protected FilterRule()
        {
        }

        /// <summary>
        /// Initializes a new instance of the FilterRule class from an existing instance.
        /// </summary>
        /// <param name="source">The source to initialize from.</param>
        protected FilterRule(FilterRule source)
        {
            ArgumentNullException.ThrowIfNull(source);
            this.DisplayName = source.DisplayName;
        }

        /// <inheritdoc/>
        public object DeepClone()
        {
            return Activator.CreateInstance(this.GetType(), new object[] { this });
        }

        /// <summary>
        /// Gets a value indicating whether the supplied item meets the
        /// criteria specified by this rule.
        /// </summary>
        /// <param name="item">The item to evaluate.</param>
        /// <returns>Returns true if the item meets the criteria. False otherwise.</returns>
        public abstract bool Evaluate(object item);

        #region EvaluationResultInvalidated

        /// <summary>
        /// Occurs when the values of this rule changes.
        /// </summary>
        [field: NonSerialized]
        public event EventHandler EvaluationResultInvalidated;

        /// <summary>
        /// Fires <see cref="EvaluationResultInvalidated"/>.
        /// </summary>
        protected void NotifyEvaluationResultInvalidated()
        {
            var eh = this.EvaluationResultInvalidated;

            if (eh != null)
            {
                eh(this, new EventArgs());
            }
        }

        #endregion
    }
}
