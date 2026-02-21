// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    /// <summary>
    /// A variable that represents $PSUserContentPath.
    /// This variable is read-only and can only be changed via Set-PSContentPath cmdlet.
    /// </summary>
    internal class PSUserContentPathVariable : PSVariable
    {
        /// <summary>
        /// Constructs an instance of the variable.
        /// </summary>
        /// <param name="initialValue">The initial value for the variable.</param>
        internal PSUserContentPathVariable(string initialValue)
            : base(SpecialVariables.PSUserContentPath, true, ScopedItemOptions.ReadOnly | ScopedItemOptions.AllScope,
                   RunspaceInit.PSUserContentPathDescription)
        {
            _value = initialValue;
        }

        private string _value;

        /// <summary>
        /// Gets or sets the value of the variable.
        /// Throws a custom error message directing users to use Set-PSContentPath.
        /// </summary>
        public override object Value
        {
            get
            {
                DebuggerCheckVariableRead();
                return _value;
            }

            set
            {
                // Throw a custom error message directing users to use Set-PSContentPath
                SessionStateUnauthorizedAccessException e =
                    new SessionStateUnauthorizedAccessException(
                            Name,
                            SessionStateCategory.Variable,
                            "VariableNotWritableUseSetPSContentPath",
                            SessionStateStrings.VariableNotWritableUseSetPSContentPath);

                throw e;
            }
        }

        /// <summary>
        /// Updates the internal value. This should only be called by Set-PSContentPath cmdlet.
        /// </summary>
        /// <param name="newValue">The new value to set.</param>
        internal void UpdateValue(string newValue)
        {
            _value = newValue;
        }
    }
}
