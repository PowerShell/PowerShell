/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using Microsoft.Management.Infrastructure.Native;
using Microsoft.Management.Infrastructure.Options;

namespace Microsoft.Management.Infrastructure.Internal.Operations
{
    internal class CimAsyncIndicationObserverProxy : CimAsyncObserverProxyBase<CimSubscriptionResult>
    {
        private readonly bool _shortenLifetimeOfResults;

        internal CimAsyncIndicationObserverProxy(IObserver<CimSubscriptionResult> observer, bool shortenLifetimeOfResults)
            : base(observer)
        {
            this._shortenLifetimeOfResults = shortenLifetimeOfResults;
        }

        internal void IndicationResultCallback(
            Native.OperationCallbackProcessingContext callbackProcessingContext,
            Native.OperationHandle operationHandle,
            Native.InstanceHandle instanceHandle,
            String bookMark,
            String machineID,
            bool moreResults,
            Native.MiResult operationResult,
            String errorMessage,
            Native.InstanceHandle errorDetailsHandle)
        {
            CimSubscriptionResult currentItem = null;
            if ((instanceHandle != null) && (!instanceHandle.IsInvalid))
            {
                if (!_shortenLifetimeOfResults)
                {
                    instanceHandle = instanceHandle.Clone();
                }
                currentItem = new CimSubscriptionResult(instanceHandle, bookMark, machineID);
            }

            try
            {
                this.ProcessNativeCallback(callbackProcessingContext, currentItem, moreResults, operationResult, errorMessage, errorDetailsHandle);
            } 
            finally
            {
                if (_shortenLifetimeOfResults)
                {
                    if (currentItem != null)
                    {
                        currentItem.Dispose();
                    }
                }
            }
        }

        public override void RegisterAcceptedAsyncCallbacks(OperationCallbacks operationCallbacks, CimOperationOptions operationOptions)
        {
            base.RegisterAcceptedAsyncCallbacks(operationCallbacks, operationOptions);
            operationCallbacks.IndicationResultCallback = this.IndicationResultCallback;
        }
    }
}