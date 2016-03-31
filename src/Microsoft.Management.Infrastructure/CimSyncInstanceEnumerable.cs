/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using Microsoft.Management.Infrastructure.Options;
using Microsoft.Management.Infrastructure.Options.Internal;

namespace Microsoft.Management.Infrastructure.Internal.Operations
{
    internal class CimSyncInstanceEnumerable : CimSyncEnumerableBase<CimInstance, CimSyncInstanceEnumerator>
    {
        private readonly bool _shortenLifetimeOfResults;
        private readonly Guid _CimSessionInstanceID;
        private readonly string _CimSessionComputerName;

        internal CimSyncInstanceEnumerable(
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

        internal override CimSyncInstanceEnumerator CreateEnumerator()
        {
            return new CimSyncInstanceEnumerator(
                this._CimSessionInstanceID,
                this._CimSessionComputerName,
                this._shortenLifetimeOfResults);
        }
    }
}