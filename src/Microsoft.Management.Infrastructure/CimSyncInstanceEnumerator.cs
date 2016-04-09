/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Diagnostics;

namespace Microsoft.Management.Infrastructure.Internal.Operations
{
    internal class CimSyncInstanceEnumerator : CimSyncEnumeratorBase<CimInstance>
    {
        private readonly Guid _CimSessionInstanceID;
        private readonly string _CimSessionComputerName;

        internal CimSyncInstanceEnumerator(
            Guid cimSessionInstanceID,
            string cimSessionComputerName,
            bool shortenLifetimeOfResults)
            : base(shortenLifetimeOfResults)
        {
            this._CimSessionInstanceID = cimSessionInstanceID;
            this._CimSessionComputerName = cimSessionComputerName;
        }

        internal override Native.MiResult NativeMoveNext(Native.OperationHandle operationHandle, out CimInstance currentItem, out bool moreResults, out Native.MiResult operationResult, out string errorMessage, out Native.InstanceHandle errorDetailsHandle)
        {
            Debug.Assert(operationHandle != null, "Caller should verify operationHandle != null");

            currentItem = null;

            Native.InstanceHandle instanceHandle;
            Native.MiResult functionResult = Native.OperationMethods.GetInstance(
                operationHandle,
                out instanceHandle,
                out moreResults,
                out operationResult,
                out errorMessage,
                out errorDetailsHandle);

            if ((instanceHandle != null) && !instanceHandle.IsInvalid)
            {
                if (!this.ShortenLifetimeOfResults)
                {
                    instanceHandle = instanceHandle.Clone();
                }
                currentItem = new CimInstance(instanceHandle, null);
                currentItem.SetCimSessionComputerName(this._CimSessionComputerName);
                currentItem.SetCimSessionInstanceId(this._CimSessionInstanceID);
            }

            return functionResult;
        }
    }
}