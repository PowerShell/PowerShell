/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Diagnostics.CodeAnalysis; // for fxcop
using System.Globalization;
using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    /// <summary>
    /// A variable that represents the maximum capacity for object types in a scope.
    /// An separate instance is created for functions, aliases, variables, and drives.
    /// </summary>
    internal class SessionStateCapacityVariable : PSVariable
    {
        #region ctor

        /// <summary>
        /// Constructs an instance of the variable with the specified name and
        /// initial capacity.
        /// </summary>
        /// 
        /// <param name="name">
        /// The name of the variable.
        /// </param>
        /// 
        /// <param name="defaultCapacity">
        /// The initial capacity
        /// </param>
        /// 
        /// <param name="maxCapacity">
        /// The maximum capacity for the scope.
        /// </param>
        /// 
        /// <param name="minCapacity">
        /// The minimum capacity for the scope.
        /// </param>
        /// 
        /// <param name="options">Scoped item options for this variable</param>
        internal SessionStateCapacityVariable(
            string name,
            int defaultCapacity,
            int maxCapacity,
            int minCapacity,
            ScopedItemOptions options)
            : base(name, defaultCapacity, options)
        {
            // Now add a range constraint to the variable so that
            // it is discoverable...

            ValidateRangeAttribute validateRange =
                new ValidateRangeAttribute(minCapacity, maxCapacity);
            _minCapacity = minCapacity;
            _maxCapacity = maxCapacity;
            base.Attributes.Add(validateRange);
            _fastValue = defaultCapacity;
        }

        /// <summary>
        /// Constructs an instance of the variable with the specified name and
        /// initial capacity.
        /// </summary>
        /// 
        /// <param name="name">
        /// The name of the variable.
        /// </param>
        /// 
        /// <param name="sharedCapacityVariable">
        /// A reference to a SessionStateCapacityVariable in another scope. The value
        /// will be shared in this scope unless the capacity gets set in this scope.
        /// </param>
        /// 
        /// <param name="options">The scoped item options for this variable</param>
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "This is internal code and is verified to behave correctly.")]
        public SessionStateCapacityVariable(
            string name,
            SessionStateCapacityVariable sharedCapacityVariable,
            ScopedItemOptions options)
            : base(name, sharedCapacityVariable.Value, options)
        {
            // Now add range constraints to the variable.

            ValidateRangeAttribute validateRange =
                new ValidateRangeAttribute(0, int.MaxValue);
            base.Attributes.Add(validateRange);

            _sharedCapacityVariable = sharedCapacityVariable;

            // Also propagate the description to prevent re-fetching them from the
            // resource manager.  That causes a measurable performance degradation.
            this.Description = sharedCapacityVariable.Description;

            // Initialize the fast value...
            _fastValue = (int)sharedCapacityVariable.Value;
        }
        #endregion ctor

        /// <summary>
        /// Gets or sets the value of the variable. 
        /// This class will always return an int from the getter. The value is
        /// either inherited from a parent scope, or stored locally
        /// </summary>
        /// 
        public override object Value
        {
            get
            {
                object result;

                if (_sharedCapacityVariable != null)
                {
                    result = _sharedCapacityVariable.Value;
                }
                else
                {
                    result = base.Value;
                }
                return result;
            }

            set
            {
                _sharedCapacityVariable = null;
                base.Value = LanguagePrimitives.ConvertTo(value, typeof(int), CultureInfo.InvariantCulture);
                _fastValue = (int)base.Value;
            }
        }

        /// <summary>
        /// Provides fast access to the capacity variable as an int, eliminating the need
        /// for casts...
        /// </summary>
        internal int FastValue
        {
            get { return _fastValue; }
        }
        private int _fastValue;

        /// <summary>
        /// Overrides the base IsValidValue to ensure the value is an int.
        /// </summary>
        /// 
        /// <param name="value">
        /// The value to test.
        /// </param>
        /// 
        /// <returns>
        /// true if the value is an int and the base class IsValidValue is true, otherwise
        /// false.
        /// </returns>
        /// 
        /// <exception cref="ValidationMetadataException">
        /// If the validation metadata throws an exception.
        /// </exception>
        /// 
        public override bool IsValidValue(object value)
        {
            int capacity = (int)value;
            // If the value is within the limits, then return true
            // otherwise call the base validator so we'll get the error
            // from the validation attribute...
            if (capacity >= _minCapacity && capacity <= _maxCapacity)
                return true;
            else
                return base.IsValidValue(value);
        }

        private int _minCapacity;
        private int _maxCapacity = int.MaxValue;

        private SessionStateCapacityVariable _sharedCapacityVariable;
    } // class SessionStateCapacityVariable
}

