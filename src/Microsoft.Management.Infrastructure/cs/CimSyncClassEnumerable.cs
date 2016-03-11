/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using Microsoft.Management.Infrastructure.Options;
using Microsoft.Management.Infrastructure.Options.Internal;

namespace Microsoft.Management.Infrastructure.Internal.Operations
{
    internal class CimSyncClassEnumerable : CimSyncEnumerableBase<CimClass, CimSyncClassEnumerator>
    {
        private readonly bool _shortenLifetimeOfResults;

        internal CimSyncClassEnumerable(
            CimOperationOptions operationOptions,
            Func<CimAsyncCallbacksReceiverBase, Native.OperationHandle> operationStarter)
            : base(operationOptions, operationStarter)
        {
            this._shortenLifetimeOfResults = operationOptions.GetShortenLifetimeOfResults();
        }

        internal override CimSyncClassEnumerator CreateEnumerator()
        {
            return new CimSyncClassEnumerator(this._shortenLifetimeOfResults);
        }
    }
}
