// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation;

/// <summary>
/// Identifies an absent runtime view so interpolated expressions can apply the
/// same strict-mode exception as an ordinary variable expression.
/// </summary>
internal sealed class ScopedRuntimeUndefinedVariableException : RuntimeException
{
    internal ScopedRuntimeUndefinedVariableException(string variableName)
        : this(InterpreterError.NewInterpreterException(
            variableName,
            typeof(RuntimeException),
            null,
            "VariableIsUndefined",
            ParserStrings.VariableIsUndefined,
            variableName))
    {
    }

    private ScopedRuntimeUndefinedVariableException(RuntimeException error)
        : base(error.Message, error.InnerException, error.ErrorRecord)
    {
    }
}
