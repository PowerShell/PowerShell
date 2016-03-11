/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using Microsoft.Management.Infrastructure.Options;
using Microsoft.Management.Infrastructure.Options.Internal;

namespace Microsoft.Management.Infrastructure.Internal.Operations
{
    internal class CimSyncIndicationEnumerable : CimSyncEnumerableBase<CimSubscriptionResult, CimSyncIndicationEnumerator>
    {
        private readonly bool _shortenLifetimeOfResults;

        internal CimSyncIndicationEnumerable(
            CimOperationOptions operationOptions,
            Func<CimAsyncCallbacksReceiverBase, Native.OperationHandle> operationStarter)
            : base(operationOptions, operationStarter)
        {
            this._shortenLifetimeOfResults = operationOptions.GetShortenLifetimeOfResults();
        }

        internal override CimSyncIndicationEnumerator CreateEnumerator()
        {
            return new CimSyncIndicationEnumerator(this._shortenLifetimeOfResults);
        }
    }
}