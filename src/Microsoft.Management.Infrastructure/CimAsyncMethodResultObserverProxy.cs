/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using Microsoft.Management.Infrastructure.Native;
using Microsoft.Management.Infrastructure.Options;

namespace Microsoft.Management.Infrastructure.Internal.Operations
{
    internal class CimAsyncMethodResultObserverProxy : CimAsyncObserverProxyBase<CimMethodResultBase>
    {
        private readonly bool _shortenLifetimeOfResults;
        private readonly Guid _CimSessionInstanceID;
        private readonly string _CimSessionComputerName;

        internal CimAsyncMethodResultObserverProxy(IObserver<CimMethodResultBase> observer,
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
            CimMethodResult currentItem = null;
            if ((instanceHandle != null) && (!instanceHandle.IsInvalid))
            {
                if (!_shortenLifetimeOfResults)
                {
                    instanceHandle = instanceHandle.Clone();
                }
                var backingInstance = new CimInstance(instanceHandle, null);
                backingInstance.SetCimSessionComputerName(this._CimSessionComputerName);
                backingInstance.SetCimSessionInstanceId(this._CimSessionInstanceID);
                currentItem = new CimMethodResult(backingInstance);
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

        internal void StreamedParameterCallback(
            Native.OperationCallbackProcessingContext callbackProcessingContext,
            Native.OperationHandle operationHandle,
            string parameterName,
            object parameterValue,
            Native.MiType parameterType)
        {
            parameterValue = CimInstance.ConvertFromNativeLayer(
                value: parameterValue,
                sharedParentHandle: null,
                clone: !this._shortenLifetimeOfResults);

            {
                var cimInstance = parameterValue as CimInstance;
                if (cimInstance != null)
                {
                    cimInstance.SetCimSessionComputerName(this._CimSessionComputerName);
                    cimInstance.SetCimSessionInstanceId(this._CimSessionInstanceID);
                }

                var cimInstances = parameterValue as CimInstance[];
                if (cimInstances != null)
                {
                    foreach (var i in cimInstances)
                    {
                        if (i != null)
                        {
                            i.SetCimSessionComputerName(this._CimSessionComputerName);
                            i.SetCimSessionInstanceId(this._CimSessionInstanceID);
                        }
                    }
                }
            }

            try
            {
                CimMethodResultBase currentItem = new CimMethodStreamedResult(parameterName, parameterValue, parameterType.ToCimType());
                this.ProcessNativeCallback(callbackProcessingContext, currentItem, true, Native.MiResult.OK, null, null);
            }
            finally
            {
                if (this._shortenLifetimeOfResults)
                {
                    var cimInstance = parameterValue as CimInstance;
                    if (cimInstance != null)
                    {
                        cimInstance.Dispose();
                    }

                    var cimInstances = parameterValue as CimInstance[];
                    if (cimInstances != null)
                    {
                        foreach (var i in cimInstances)
                        {
                            if (i != null)
                            {
                                i.Dispose();
                            }
                        }
                    }
                }
            }
        }

        public override void RegisterAcceptedAsyncCallbacks(OperationCallbacks operationCallbacks, CimOperationOptions operationOptions)
        {
            base.RegisterAcceptedAsyncCallbacks(operationCallbacks, operationOptions);
            operationCallbacks.InstanceResultCallback = this.InstanceResultCallback;
            if ((operationOptions != null) && (operationOptions.EnableMethodResultStreaming))
            {
                operationCallbacks.StreamedParameterCallback = this.StreamedParameterCallback;
            }
        }
    }
}