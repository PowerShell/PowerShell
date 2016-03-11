/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using Microsoft.Management.Infrastructure.Native;
using Microsoft.Management.Infrastructure.Options;

namespace Microsoft.Management.Infrastructure.Internal.Operations
{
    internal class CimAsyncInstanceObserverProxy : CimAsyncObserverProxyBase<CimInstance>
    {
        private readonly bool _shortenLifetimeOfResults;
        private readonly Guid _CimSessionInstanceID;
        private readonly string _CimSessionComputerName;

        internal CimAsyncInstanceObserverProxy(IObserver<CimInstance> observer,
            Guid cimSessionInstanceID,
            string cimSessionComputerName,
            bool shortenLifetimeOfResults)
            : base(observer)
        {
            this._shortenLifetimeOfResults = shortenLifetimeOfResults;
            this._CimSessionInstanceID = cimSessionInstanceID;
            this._CimSessionComputerName = cimSessionComputerName;
        }

        internal void InstanceResultCallback(
            Native.OperationCallbackProcessingContext callbackProcessingContext,
            Native.OperationHandle operationHandle,
            Native.InstanceHandle instanceHandle,
            bool moreResults,
            Native.MiResult operationResult,
            String errorMessage,
            Native.InstanceHandle errorDetailsHandle)
        {
            CimInstance currentItem = null;
            if ((instanceHandle != null) && (!instanceHandle.IsInvalid))
            {
                if (!_shortenLifetimeOfResults)
                {
                    instanceHandle = instanceHandle.Clone();
                }
                currentItem = new CimInstance(instanceHandle, null);
                currentItem.SetCimSessionComputerName(this._CimSessionComputerName);
                currentItem.SetCimSessionInstanceId(this._CimSessionInstanceID);
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
            operationCallbacks.InstanceResultCallback = this.InstanceResultCallback;
        }
    }
}