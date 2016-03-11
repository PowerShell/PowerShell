/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Management.Infrastructure.Internal.Operations
{
    internal abstract class CimSyncEnumeratorBase<T> : CimAsyncCallbacksReceiverBase, IEnumerator<T>
        where T : class
    {
        private bool _moreResultsAreExpected = true;

        internal bool ShortenLifetimeOfResults { get; private set; }

        internal CimSyncEnumeratorBase(bool shortenLifetimeOfResults)
        {
            this.ShortenLifetimeOfResults = shortenLifetimeOfResults;
        }

        internal abstract Native.MiResult NativeMoveNext(
            Native.OperationHandle operationHandle,
            out T currentItem,
            out bool moreResults,
            out Native.MiResult operationResult,
            out string errorMessage,
            out Native.InstanceHandle errorDetailsHandle);

        #region IEnumerator<CimInstance> Members

        public T Current { get; private set; }

        #endregion

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
            lock (_disposeThreadSafetyLock)
            {
                if (_disposed)
                {
                    return;
                }

                Debug.Assert(disposing, "We should never call CImSyncEnumeratorBase.Dispose from a finalizer");
                this.DisposeOperationWhenPossible();
                this.DisposeCurrentItemIfNeeded();

                _disposed = true;
            }
        }

        private void DisposeCurrentItemIfNeeded()
        {
            if (this.ShortenLifetimeOfResults)
            {
                var d = this.Current as IDisposable;
                if (d != null)
                {
                    d.Dispose();
                }
            }
        }

        internal void AssertNotDisposed()
        {
            lock (this._disposeThreadSafetyLock)
            {
                if (this._disposed)
                {
                    throw new ObjectDisposedException(this.ToString());
                }
            }
        }

        private bool _disposed;
        private readonly object _disposeThreadSafetyLock = new object();

        #endregion

        #region IEnumerator Members

        object System.Collections.IEnumerator.Current
        {
            get { return this.Current; }
        }

        public bool MoveNext()
        {
            lock (_disposeThreadSafetyLock)
            {
                lock (this._internalErrorWhileProcessingAsyncCallbackLock)
                {
                    if (this._internalErrorWhileProcessingAsyncCallback != null)
                    {
                        throw this._internalErrorWhileProcessingAsyncCallback;
                    }
                }

                if (!_moreResultsAreExpected)
                {
                    return false;
                }

                this.AssertNotDisposed();

                T currentItem;
                Native.MiResult result;
                string errorMessage;
                Native.InstanceHandle errorDetailsHandle;
                Native.MiResult functionResult = NativeMoveNext(
                    this.Operation.Handle,
                    out currentItem,
                    out this._moreResultsAreExpected,
                    out result,
                    out errorMessage,
                    out errorDetailsHandle);

                CimException.ThrowIfMiResultFailure(functionResult);
                if (!this._moreResultsAreExpected)
                {
                    this.Operation.IgnoreSubsequentCancellationRequests();
                }

                lock (this._internalErrorWhileProcessingAsyncCallbackLock)
                {
                    if (this._internalErrorWhileProcessingAsyncCallback != null)
                    {
                        throw this._internalErrorWhileProcessingAsyncCallback;
                    }
                }

                CimException cimException = CimException.GetExceptionIfMiResultFailure(result, errorMessage,
                                                                                       errorDetailsHandle);
                if (cimException != null)
                {
                    CancellationMode cancellationMode = this.Operation.CancellationMode;
                    Debug.Assert(cancellationMode != CancellationMode.SilentlyStopProducingResults,
                                 "CancellationMode.SilentlyStopProducingResults is only applicable to IObservable pattern");
                    if (cancellationMode == CancellationMode.ThrowOperationCancelledException)
                    {
                        throw new OperationCanceledException(cimException.Message, cimException);
                    }
                    else
                    {
                        throw cimException;
                    }
                }

                Debug.Assert(result == Native.MiResult.OK, "Exception should be thrown above in case of error");
                this.DisposeCurrentItemIfNeeded();
                this.Current = currentItem;

                bool currentResultsIsValid;
                if (currentItem != null)
                {
                    currentResultsIsValid = true;
                }
                else
                {
                    // If more results are expected, then we have to treat currentItem=null as a valid result
                    currentResultsIsValid = this._moreResultsAreExpected;

                    // Native MI API contract is that CurrentItem=null + moreResults=true should only happen 1) as the first result and 2) when CimOperationFlags.ReportOperationStarted was used.
                    // Unfortunately operation flags and knowledge whether we have first or subsequent result is not available at this point - we cannot assert the statement above :-(
                }
                return currentResultsIsValid;
            }
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        #endregion

        private Exception _internalErrorWhileProcessingAsyncCallback;
        private readonly object _internalErrorWhileProcessingAsyncCallbackLock = new object();

        internal override void ReportInternalError(Native.OperationCallbackProcessingContext callbackProcessingContext, Exception internalError)
        {
            lock (_internalErrorWhileProcessingAsyncCallbackLock)
            {
                if (this._internalErrorWhileProcessingAsyncCallback == null)
                {
                    this._internalErrorWhileProcessingAsyncCallback = internalError;
                }
                else
                {
                    Exception originalInternalError = this._internalErrorWhileProcessingAsyncCallback;
                    this._internalErrorWhileProcessingAsyncCallback = new AggregateException(
                        originalInternalError, internalError);
                }
            }
        }
    }
}