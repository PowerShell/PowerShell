/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Management.Infrastructure.Native;
using Microsoft.Management.Infrastructure.Options;

namespace Microsoft.Management.Infrastructure.Internal.Operations
{
    internal abstract class CimAsyncCallbacksReceiverBase
    {
        #region Helpers for receiving and managing lifetime of a CimOperation

        private CimOperation _operation;
        private readonly object _operationLock = new object();
        private List<Action<CimOperation>> _operationPendingActions = new List<Action<CimOperation>>();

        internal void SetOperation(CimOperation operation)
        {
            Debug.Assert(operation != null, "Caller should verify operation != null");

            List<Action<CimOperation>> actionsToPerform = null;
            lock (this._operationLock)
            {
                this._operation = operation;

                actionsToPerform = this._operationPendingActions;
                this._operationPendingActions = null;
            }

            operation.Cancelled += SupressCallbacksWhenRequestedViaCancellation;
            SupressCallbacksWhenRequestedViaCancellation(operation, EventArgs.Empty);

            if (actionsToPerform != null)
            {
                foreach (Action<CimOperation> action in actionsToPerform)
                {
                    Debug.Assert(action != null, "Caller of InvokeWhenOperationIsSet should verify action != null");
                    action(operation);
                }
            }
        }

        private void SupressCallbacksWhenRequestedViaCancellation(object sender, EventArgs e)
        {
            bool needToSuppressFurtherCallbacks = false;
            lock (this._operationLock)
            {
                Debug.Assert(this._operation != null, "SupressCallbacksWhenRequestedViaCancellation should only be called when _operation was already set");
                Debug.Assert(object.ReferenceEquals(sender, this._operation), "Cancellation events should only be reported for operation owned by this CimAsyncCallbacksReceivedBase object");

                if (this._operation.CancellationMode == CancellationMode.SilentlyStopProducingResults)
                {
                    needToSuppressFurtherCallbacks = true;
                }
            }

            if (needToSuppressFurtherCallbacks)
            {
                lock (this._suppressFurtherUserCallbacksLock)
                {
                    this._suppressFurtherUserCallbacks = true;
                }
            }
        }

        protected void InvokeWhenOperationIsSet(Action<CimOperation> action)
        {
            Debug.Assert(action != null, "Caller should verify action != null");

            lock (this._operationLock)
            {
                if (this._operation == null)
                {
                    if (this._operationPendingActions == null)
                    {
                        this._operationPendingActions = new List<Action<CimOperation>>();
                    }
                    this._operationPendingActions.Add(action);
                    return;
                }
            }

            Debug.Assert(this._operation != null, "Code above should guarantee this._operation != null");
            action(this._operation);
        }

        protected CimOperation Operation
        {
            get
            {
                lock (this._operationLock)
                {
                    Debug.Assert(
                        this._operation != null, 
                        "Caller should guarantee that this._operation != null OR use CimAsyncCallbacksReceiverBase.InvokeWhenOperationIsSet");
                    return this._operation;
                }
            }
        }

        private static void DisposeOperationWhenPossibleWorker(CimOperation cimOperation)
        {
            cimOperation.IgnoreSubsequentCancellationRequests();
            cimOperation.Dispose();
        }

        protected void DisposeOperationWhenPossible()
        {
            this.InvokeWhenOperationIsSet(DisposeOperationWhenPossibleWorker);
        }

        #endregion Helpers for receiving and managing lifetime of a CimOperation

        #region Dealing with async callbacks

        private readonly object _suppressFurtherUserCallbacksLock = new object();
        private bool _suppressFurtherUserCallbacks;

#if(!_CORECLR)
        private readonly ExecutionContext _threadExecutionContext = ExecutionContext.Capture();
#endif

        internal void CallUnderOriginalExecutionContext(Action action)
        {
            Debug.Assert(action != null, "Caller should make sure that action != null");
#if(!_CORECLR)
            ExecutionContext.Run(this._threadExecutionContext.CreateCopy(), _ => action(), null);
#else
            action();
#endif
        }

        internal void CallIntoUserCallback(
            Native.OperationCallbackProcessingContext callbackProcessingContext, 
            Action userCallback, 
            bool serializeCallbacks = false,
            bool suppressFurtherUserCallbacks = false)
        {
            Debug.Assert(callbackProcessingContext != null, "Caller should make sure callbackProcessingContext != null");
            Debug.Assert(userCallback != null, "Caller should make sure userCallback != null");

            lock (this._suppressFurtherUserCallbacksLock)
            {
                if (this._suppressFurtherUserCallbacks)
                {
                    return;
                }

                if (suppressFurtherUserCallbacks)
                {
                    this._suppressFurtherUserCallbacks = true;
                }

                // need to call user callback inside the lock:
                // reason1: OnNext/OnError/OnComplete need to be serialized/sequentialized (especially wrt to calls to OnError resulting from async ReportInternalError)
                // reason2: extendedSemanticsCallbacks cannot be called after OnError(internalError) and there is a race-condition if callback is done outside the lock
                // (we could also use ReaderWriterLockSlim to allow multiple concurrent callbacks up to the last OnCompleted/OnError, but while this
                //  fullfills IObservable/IObserver serialization/seqeuntialization contract wrt OnNext/OnError/OnCompleted, this at the same time
                //  would unnecessarily weaken the serialization/sequeintializtaion contract for extended semantics callbacks)
                callbackProcessingContext.InUserCode = true;
                this.CallUnderOriginalExecutionContext(userCallback);
                callbackProcessingContext.InUserCode = false;
            }
        }

        internal abstract void ReportInternalError(Native.OperationCallbackProcessingContext callbackProcessingContext, Exception internalError);

        private void ReportInternalErrorCore(Native.OperationCallbackProcessingContext callbackProcessingContext, Exception internalError)
        {
            Debug.Assert(internalError != null, "Caller should make sure internalError != null");

            this.InvokeWhenOperationIsSet(
                delegate(CimOperation cimOperation)
                {
                    lock (this._suppressFurtherUserCallbacksLock)
                    {
                        try
                        {
                            cimOperation.Cancel(CancellationMode.SilentlyStopProducingResults);
                        }
                        catch (Exception internalErrorWhileCancellingOperation)
                        {
                            Exception originalInternalError = internalError;
                            internalError = new AggregateException(originalInternalError,
                                                                   internalErrorWhileCancellingOperation);
                        }

                        this.ReportInternalError(callbackProcessingContext, internalError);

                        this._suppressFurtherUserCallbacks = true;
                    }
                });
        }

        public virtual void RegisterAcceptedAsyncCallbacks(OperationCallbacks operationCallbacks, CimOperationOptions operationOptions)
        {
            operationCallbacks.InternalErrorCallback = this.ReportInternalErrorCore;
            operationCallbacks.ManagedOperationContext = this;
        }

        #endregion Dealing with async callbacks
    }
}