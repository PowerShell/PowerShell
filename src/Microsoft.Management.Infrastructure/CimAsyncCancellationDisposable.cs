/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Diagnostics;
using System.Security;

namespace Microsoft.Management.Infrastructure.Internal.Operations
{
    internal sealed class CimAsyncCancellationDisposable : IDisposable
    {
        private readonly CimOperation _operation;
        private bool _disposed;
        private readonly object _disposeThreadSafetyLock = new object();
#if(!_CORECLR) 
        private readonly SecurityContext _securityContext = SecurityContext.Capture();
#else
        //
        // TODO: WE NEED TO USE WINDOWS IDENTITY
        //
        //private readonly WindowsIdentity _windowsIdentiy = new WindowsIdentity( LogonUser() );

#endif
        internal CimAsyncCancellationDisposable(CimOperation operation)
        {
            Debug.Assert(operation != null, "Caller should verify that operation != null");
            this._operation = operation;
        }

        #region IDisposable Members

        public void Dispose()
        {
            lock (this._disposeThreadSafetyLock)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
            }

#if(!_CORECLR) 
            SecurityContext.Run(
                this._securityContext,
                callback: _ => this._operation.Cancel(CancellationMode.SilentlyStopProducingResults),
                state: null);
            this._securityContext.Dispose();
#endif
        }

        #endregion
    }
}