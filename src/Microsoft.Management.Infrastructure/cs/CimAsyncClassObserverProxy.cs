/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using Microsoft.Management.Infrastructure.Native;
using Microsoft.Management.Infrastructure.Options;

namespace Microsoft.Management.Infrastructure.Internal.Operations
{
    internal class CimAsyncClassObserverProxy : CimAsyncObserverProxyBase<CimClass>
    {
        private readonly bool _shortenLifetimeOfResults;

        internal CimAsyncClassObserverProxy(IObserver<CimClass> observer, bool shortenLifetimeOfResults)
            : base(observer)
        {
            this._shortenLifetimeOfResults = shortenLifetimeOfResults;
        }

        internal void ClassCallback(
            Native.OperationCallbackProcessingContext callbackProcessingContext,
            Native.OperationHandle operationHandle,
            Native.ClassHandle ClassHandle,
            bool moreResults,
            Native.MiResult operationResult,
            String errorMessage,
            Native.InstanceHandle errorDetailsHandle)
        {
            CimClass currentItem = null;
            if ((ClassHandle != null) && (!ClassHandle.IsInvalid))
            {
                if (!_shortenLifetimeOfResults)
                {
                    ClassHandle = ClassHandle.Clone();
                }
                currentItem = new CimClass(ClassHandle);
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
            operationCallbacks.ClassCallback = this.ClassCallback;
        }
    }
}
