//-----------------------------------------------------------------------
// <copyright file="SingleValueComparableValueFilterRule.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI.Internal
{
    using System;
    using System.Runtime.Serialization;
    using System.ComponentModel;

    /// <summary>
    /// The SingleValueComparableValueFilterRule provides support for derived classes
    /// that take a single input and evaluate against IComparable values.
    /// </summary>
    /// <typeparam name="T">The generic parameter.</typeparam>
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public abstract class SingleValueComparableValueFilterRule<T> : ComparableValueFilterRule<T> where T : IComparable
    {
        #region Properties

        /// <summary>
        /// Gets a value that holds user input.
        /// </summary>
        public ValidatingValue<T> Value
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets a value indicating whether the FilterRule can be
        /// evaluated in its current state.
        /// </summary>
        public override bool IsValid
        {
            get
            {
                return this.Value.IsValid;
            }
        }

        #endregion Properties

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the SingleValueComparableValueFilterRule class.
        /// </summary>
        protected SingleValueComparableValueFilterRule()
        {
            this.Value = new ValidatingValue<T>();
            this.Value.PropertyChanged += new PropertyChangedEventHandler(this.Value_PropertyChanged);
        }

        #endregion Ctor

        private void Value_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Value")
            {
                this.NotifyEvaluationResultInvalidated();
            }
        }

        [OnDeserialized]
        private void Initialize(StreamingContext context)
        {
            this.Value.PropertyChanged += new PropertyChangedEventHandler(this.Value_PropertyChanged);
        }
    }
}

