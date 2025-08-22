// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;

namespace Microsoft.PowerShell.Commands;

/// <summary>
/// Thrown during evaluation of <see cref="TestJsonCommand"/> when an attempt
/// to resolve a <code>$ref</code> or <code>$dynamicRef</code> fails.
/// </summary>
internal sealed class JsonSchemaReferenceResolutionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSchemaReferenceResolutionException"/> class.
    /// </summary>
    /// <param name="innerException">
    /// The exception that is the cause of the current exception, or a null reference
    /// (<code>Nothing</code> in Visual Basic) if no inner exception is specified.
    /// </param>
    public JsonSchemaReferenceResolutionException(Exception innerException)
        : base(message: null, innerException)
    {
    }
}
