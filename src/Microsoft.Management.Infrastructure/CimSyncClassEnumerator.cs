/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System.Diagnostics;

namespace Microsoft.Management.Infrastructure.Internal.Operations
{
    internal class CimSyncClassEnumerator : CimSyncEnumeratorBase<CimClass>
    {
        internal CimSyncClassEnumerator(bool shortenLifetimeOfResults)
            : base(shortenLifetimeOfResults)
        {
        }

        internal override Native.MiResult NativeMoveNext(Native.OperationHandle operationHandle, out CimClass currentItem,
                    out bool moreResults, out Native.MiResult operationResult,
            out string errorMessage, out Native.InstanceHandle errorDetailsHandle)
        {
            Debug.Assert(operationHandle != null, "Caller should verify operationHandle != null");

            currentItem = null;

            Native.ClassHandle classHandle;
            Native.MiResult functionResult = Native.OperationMethods.GetClass(
                operationHandle,
                out classHandle,
                out moreResults,
                out operationResult,
                out errorMessage,
                out errorDetailsHandle);

            if ((classHandle != null) && !classHandle.IsInvalid)
            {
                if (!this.ShortenLifetimeOfResults)
                {
                    classHandle = classHandle.Clone();
                }
                currentItem = new CimClass(classHandle);
            }

            return functionResult;
        }
    }
}
