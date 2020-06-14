// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The IsBetweenFilterRule class evaluates an item to see if it is between
    /// the StartValue and EndValue of the rule.
    /// </summary>
    /// <typeparam name="T">
    /// The generic parameter.
    /// </typeparam>
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class IsBetweenFilterRule<T> : ComparableValueFilterRule<T> where T : IComparable
    {
        #region Properties

        /// <summary>
        /// Gets a value indicating whether the FilterRule can be
        /// evaluated in its current state.
        /// </summary>
        public override bool IsValid
        {
            get
            {
                return this.StartValue.IsValid && this.EndValue.IsValid;
            }
        }

        /// <summary>
        /// Gets the start value for the range.
        /// </summary>
        public ValidatingValue<T> StartValue
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets the end value for the range.
        /// </summary>
        public ValidatingValue<T> EndValue
        {
            get;
            protected set;
        }

        #endregion Properties

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the IsBetweenFilterRule class.
        /// </summary>
        public IsBetweenFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_IsBetween;

            this.StartValue = new ValidatingValue<T>();
            this.StartValue.PropertyChanged += this.Value_PropertyChanged;

            this.EndValue = new ValidatingValue<T>();
            this.EndValue.PropertyChanged += this.Value_PropertyChanged;
        }

        #endregion Ctor

        #region Public Methods

        /// <summary>
        /// Evaluates data and determines if it is between
        /// StartValue and EndValue.
        /// </summary>
        /// <param name="data">
        /// The data to evaluate.
        /// </param>
        /// <returns>
        /// Returns true if data is between StartValue and EndValue,
        /// false otherwise.
        /// </returns>
        protected override bool Evaluate(T data)
        {
            Debug.Assert(this.IsValid, "is valid");
            int startValueComparedToData = CustomTypeComparer.Compare<T>(this.StartValue.GetCastValue(), data);
            int endValueComparedToData = CustomTypeComparer.Compare<T>(this.EndValue.GetCastValue(), data);

            bool isBetweenForward = startValueComparedToData < 0 && endValueComparedToData > 0;
            bool isBetweenBackwards = endValueComparedToData < 0 && startValueComparedToData > 0;

            return isBetweenForward || isBetweenBackwards;
        }

        #endregion Public Methods

        #region Value Change Handlers

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
            this.StartValue.PropertyChanged += this.Value_PropertyChanged;
            this.EndValue.PropertyChanged += this.Value_PropertyChanged;
        }

        #endregion Value Change Handlers
    }
}
