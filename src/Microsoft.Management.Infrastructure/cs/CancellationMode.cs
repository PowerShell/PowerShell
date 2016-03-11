/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

namespace Microsoft.Management.Infrastructure.Internal.Operations
{
    internal enum CancellationMode
    {
        NoCancellationOccured,
        ThrowOperationCancelledException,
        SilentlyStopProducingResults,
        IgnoreCancellationRequests
    }
}