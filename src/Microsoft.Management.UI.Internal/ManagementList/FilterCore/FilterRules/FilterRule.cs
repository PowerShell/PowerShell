// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The base class for all filtering rules.
    /// </summary>
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public abstract class FilterRule : IEvaluate
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
            // HACK : Is there a way to statically enforce this? No... not ISerializable...
            if (!this.GetType().IsSerializable)
            {
                throw new InvalidOperationException("FilterRules must be serializable.");
            }
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
            #pragma warning disable IDE1005 // IDE1005: Delegate invocation can be simplified.
            var eh = this.EvaluationResultInvalidated;

            if (eh != null)
            {
                eh(this, new EventArgs());
            }
            #pragma warning restore IDE1005
        }

        #endregion
    }
}
