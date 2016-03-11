/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System.Diagnostics;

namespace Microsoft.Management.Infrastructure.Internal.Operations
{
    internal class CimSyncIndicationEnumerator : CimSyncEnumeratorBase<CimSubscriptionResult>
    {
        internal CimSyncIndicationEnumerator(bool shortenLifetimeOfResults)
            : base(shortenLifetimeOfResults)
        {
        }

        internal override Native.MiResult NativeMoveNext(Native.OperationHandle operationHandle, out CimSubscriptionResult currentItem, 
                    out bool moreResults, out Native.MiResult operationResult, 
            out string errorMessage, out Native.InstanceHandle errorDetailsHandle)
        {
            Debug.Assert(operationHandle != null, "Caller should verify operationHandle != null");

            currentItem = null;

            Native.InstanceHandle instanceHandle;
            string bookmark;
            string machineID;
            Native.MiResult functionResult = Native.OperationMethods.GetIndication(
                operationHandle,
                out instanceHandle,
                out bookmark,
                out machineID,
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
                currentItem = new CimSubscriptionResult(instanceHandle, bookmark, machineID);
            }

            return functionResult;
        }
    }
}