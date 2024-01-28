// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    /// <summary>
    /// A variable that represents $?
    /// </summary>
    internal sealed class QuestionMarkVariable : PSVariable
    {
        /// <summary>
        /// Constructs an instance of the variable with execution context.
        /// </summary>
        /// <param name="context">
        /// Execution context
        /// </param>
        internal QuestionMarkVariable(ExecutionContext context)
            : base(SpecialVariables.Question, true, ScopedItemOptions.ReadOnly | ScopedItemOptions.AllScope, RunspaceInit.DollarHookDescription)
        {
            _context = context;
        }

        private readonly ExecutionContext _context;

        /// <summary>
        /// Gets or sets the value of the variable.
        /// </summary>
        public override object Value
        {
            get
            {
                DebuggerCheckVariableRead();
                return _context.QuestionMarkVariableValue;
            }

            set
            {
                // Call base's setter to force an error (because the variable is readonly).
                base.Value = value;
            }
        }
    }
}
