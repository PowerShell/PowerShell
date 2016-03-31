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
    internal class CimAsyncClassObservable : CimAsyncObservableBase<CimAsyncClassObserverProxy, CimClass>
    {
        private readonly bool _shortenLifetimeOfResults;

        internal CimAsyncClassObservable(
            CimOperationOptions operationOptions,
            Func<CimAsyncCallbacksReceiverBase, Native.OperationHandle> operationStarter)
            : base(operationOptions, operationStarter)
        {
            this._shortenLifetimeOfResults = operationOptions.GetShortenLifetimeOfResults();
        }

        internal override CimAsyncClassObserverProxy CreateObserverProxy(IObserver<CimClass> observer)
        {
            Debug.Assert(observer != null, "Caller should verify observer != null");
            return new CimAsyncClassObserverProxy(observer, _shortenLifetimeOfResults);
        }
    }
}
