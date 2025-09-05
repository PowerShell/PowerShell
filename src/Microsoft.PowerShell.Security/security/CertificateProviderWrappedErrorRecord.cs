// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable
#if !UNIX

using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands;

/// <summary>
/// Used to wrap a provider exception ErrorRecord.
/// </summary>
internal class CertificateProviderWrappedErrorRecord : Exception, IContainsErrorRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CertificateProviderWrappedErrorRecord"/> class.
    /// </summary>
    /// <param name="err">The ErrorRecord to wrap.</param>
    public CertificateProviderWrappedErrorRecord(ErrorRecord err)
        : base(string.Empty)
    {
        this.ErrorRecord = err;
    }

    /// <summary>
    /// Gets the ErrorRecord the exception wraps.
    /// </summary>
    public ErrorRecord ErrorRecord { get; }
}

#endif
