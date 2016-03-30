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
    internal class CimAsyncInstanceObservable : CimAsyncObservableBase<CimAsyncInstanceObserverProxy, CimInstance>
    {
        private readonly bool _shortenLifetimeOfResults;
        private readonly Guid _CimSessionInstanceID;
        private readonly string _CimSessionComputerName;

        internal CimAsyncInstanceObservable(
            CimOperationOptions operationOptions,
            Guid cimSessionInstanceID,
            string cimSessionComputerName,
            Func<CimAsyncCallbacksReceiverBase, Native.OperationHandle> operationStarter)
            : base(operationOptions, operationStarter)
        {
            this._shortenLifetimeOfResults = operationOptions.GetShortenLifetimeOfResults();
            this._CimSessionInstanceID = cimSessionInstanceID;
            this._CimSessionComputerName = cimSessionComputerName;
        }

        internal override CimAsyncInstanceObserverProxy CreateObserverProxy(IObserver<CimInstance> observer)
        {
            Debug.Assert(observer != null, "Caller should verify observer != null");
            return new CimAsyncInstanceObserverProxy(observer,
                this._CimSessionInstanceID,
                this._CimSessionComputerName,
                _shortenLifetimeOfResults);
        }
    }
}