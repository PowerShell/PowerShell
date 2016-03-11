/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Management.Infrastructure.Options;
using Microsoft.Management.Infrastructure.Options.Internal;

namespace Microsoft.Management.Infrastructure.Internal.Operations
{
    internal abstract class CimSyncEnumerableBase<TItem, TEnumerator> : IEnumerable<TItem>
        where TEnumerator: CimSyncEnumeratorBase<TItem>, IEnumerator<TItem> 
        where TItem : class
    {
        private readonly CancellationToken? _cancellationToken;
        private readonly Func<CimAsyncCallbacksReceiverBase, Native.OperationHandle> _operationStarter;

        internal CimSyncEnumerableBase(CimOperationOptions operationOptions, Func<CimAsyncCallbacksReceiverBase, Native.OperationHandle> operationStarter)
        {
            Debug.Assert(operationStarter != null, "Caller should verify that operationStarter != null");
            
            this._cancellationToken = operationOptions.GetCancellationToken();
            this._operationStarter = operationStarter;
        }

        internal abstract TEnumerator CreateEnumerator();
    
        #region IEnumerable<CimInstance> Members

        public IEnumerator<TItem> GetEnumerator()
        {
            TEnumerator enumerator = this.CreateEnumerator();
            CimOperation operation;

            Native.OperationHandle operationHandle = this._operationStarter(enumerator);
            operation = new CimOperation(operationHandle, this._cancellationToken);

            enumerator.SetOperation(operation);
            return enumerator;
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
    }
}