/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Diagnostics;
using System.Threading;
using System.Diagnostics.CodeAnalysis;


namespace Microsoft.Management.Infrastructure.Internal.Operations
{
    // this class is primarily needed to a avoid a race condition between
    // 1) a thread that calls
    //      1a) CimOperation.Cancel (called via CancellationToken or via IDisposable from IObservable.Subscribe)
    // and
    // 2) a thread that does the main processing: 
    //      2a) CimOperation.Close
    //      2b) Native.OperationMethods.GetInstance/GetClassName/...
    internal class CimOperation : IDisposable
    {
        private Native.OperationHandle _handle;
        private IDisposable _cancellationTokenRegistration;

        private readonly object _cancellationModeLock = new object();

        internal CimOperation(Native.OperationHandle handle, CancellationToken? cancellationToken)
        {
            Debug.Assert(handle != null, "Caller should verify that handle != null");
            handle.AssertValidInternalState();
            this._handle = handle;

            if (cancellationToken.HasValue)
            {
                this._cancellationTokenRegistration = cancellationToken.Value.Register(
                    () => this.Cancel(CancellationMode.ThrowOperationCancelledException));
            }
        }

        internal CancellationMode CancellationMode
        {
            get
            {
                lock (this._cancellationModeLock)
                {
                    return this._cancellationMode;
                }
            }
        }
        private CancellationMode _cancellationMode = CancellationMode.NoCancellationOccured;

        internal void Cancel(CancellationMode cancellationMode)
        {
            Debug.Assert(cancellationMode != CancellationMode.NoCancellationOccured, "Caller should verify the right cancellation mode is used");
            Debug.Assert(cancellationMode != CancellationMode.IgnoreCancellationRequests, "Caller should verify the right cancellation mode is used");
            lock (this._cancellationModeLock)
            {
                if (this._cancellationMode == CancellationMode.IgnoreCancellationRequests)
                {
                    return;
                }
                this._cancellationMode = cancellationMode;
            }

            Native.MiResult result = Native.OperationMethods.Cancel(this._handle, Native.MiCancellationReason.None);
            CimException.ThrowIfMiResultFailure(result);

            this.Cancelled.SafeInvoke(this, EventArgs.Empty);
        }

        internal event EventHandler<EventArgs> Cancelled;

        internal void IgnoreSubsequentCancellationRequests()
        {
            lock (this._cancellationModeLock)
            {
                if (this._cancellationMode == CancellationMode.NoCancellationOccured)
                {
                    this._cancellationMode = CancellationMode.IgnoreCancellationRequests;
                }
            }
        }

        internal Native.OperationHandle Handle
        {
            get
            {
                this._handle.AssertValidInternalState();
                this.AssertNotDisposed();
                return this._handle;
            }
        }

        #region IDisposable Members

        /// <summary>
        /// Releases resources associated with this object
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases resources associated with this object
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                this._handle.Dispose();

                if (this._cancellationTokenRegistration != null)
                {
                    this._cancellationTokenRegistration.Dispose();
                    this._cancellationTokenRegistration = null;
                }
            }

            _disposed = true;
        }

        internal void AssertNotDisposed()
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException(this.ToString());
            }
        }

        private bool _disposed;

        #endregion
    }
}
