// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The base class for all filtering rules.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
<<<<<<< HEAD
    [Serializable]
    public abstract class FilterRule : IEvaluate
=======
    public abstract class FilterRule : IEvaluate, IDeepCloneable
>>>>>>> 625da6ff5 (Remove `OnDeserialized` and `Serializable` attributes from `Microsoft.Management.UI.Internal` project (#25548))
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
