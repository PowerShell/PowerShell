/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Diagnostics;
using Microsoft.Management.Infrastructure.Options;
using Microsoft.Management.Infrastructure.Options.Internal;

namespace Microsoft.Management.Infrastructure.Internal.Operations
{
    internal class CimAsyncIndicationObservable : CimAsyncObservableBase<CimAsyncIndicationObserverProxy, CimSubscriptionResult>
    {
        private readonly bool _shortenLifetimeOfResults;

        internal CimAsyncIndicationObservable(
            CimOperationOptions operationOptions,
            Func<CimAsyncCallbacksReceiverBase, Native.OperationHandle> operationStarter)
            : base(operationOptions, operationStarter)
        {
            this._shortenLifetimeOfResults = operationOptions.GetShortenLifetimeOfResults();
        }

        internal override CimAsyncIndicationObserverProxy CreateObserverProxy(IObserver<CimSubscriptionResult> observer)
        {
            Debug.Assert(observer != null, "Caller should verify observer != null");
            return new CimAsyncIndicationObserverProxy(observer, _shortenLifetimeOfResults);
        }
    }
}