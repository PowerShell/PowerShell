// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.PowerShell.Commands;

internal class JsonSchemaReferenceResolutionException : Exception
{
    /// <summary>
    /// Thrown during evaluation of <see cref="TestJsonCommand"/> when an attempt
    /// to resolve a <code>$ref</code> or <code>$dynamicRef</code> fails.
    /// </summary>
    /// <param name="innerException">
    /// The exception that is the cause of the current exception, or a null reference
    /// (<code>Nothing</code> in Visual Basic) if no inner exception is specified.
    /// </param>
    public JsonSchemaReferenceResolutionException(Exception innerException)
        : base("An error occurred attempting to resolve a schema reference", innerException)
    {
    }
}
