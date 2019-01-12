// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Runspaces
{
    /// <summary>
    /// Base class for AsyncResult objects that are returned by various
    /// Async operations supported by RunspacePool , PowerShell types.
    /// </summary>
    internal class AsyncResult : IAsyncResult
    {
        #region Private Data

        private ManualResetEvent _completedWaitHandle;
        // exception occured in the async thread.
        // user supplied state object

        // Invoke on thread (remote debugging support).
        private AutoResetEvent _invokeOnThreadEvent;
        private WaitCallback _invokeCallback;
        private object _invokeCallbackState;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="ownerId">
        /// Instance Id of the object creating this instance
        /// </param>
        /// <param name="callback">
        /// A AsyncCallback to call once the async operation completes.
        /// </param>
        /// <param name="state">
        /// A user supplied state object
        /// </param>
        internal AsyncResult(Guid ownerId, AsyncCallback callback, object state)
        {
            Dbg.Assert(Guid.Empty != ownerId, "ownerId cannot be empty");
            OwnerId = ownerId;
            Callback = callback;
            AsyncState = state;
        }

        #endregion

        #region IAsync Overrides

        /// <summary>
        /// This always returns false.
        /// </summary>
        public bool CompletedSynchronously
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets an indication whether the asynchronous operation has completed.
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// This is not supported and returns null.
        /// </summary>
        public object AsyncState { get; }

        /// <summary>
        /// Gets a System.Threading.WaitHandle that is used to wait for an asynchronous
        /// operation to complete.
        /// </summary>
        public WaitHandle AsyncWaitHandle
        {
            get
            {
                if (_completedWaitHandle == null)
                {
                    lock (SyncObject)
                    {
                        if (_completedWaitHandle == null)
                        {
                            _completedWaitHandle = new ManualResetEvent(IsCompleted);
                        }
                    }
                }

                return _completedWaitHandle;
            }
        }

        #endregion

        #region properties / methods

        /// <summary>
        /// Instance Id of the object owning this async result.
        /// </summary>
        internal Guid OwnerId { get; }

        /// <summary>
        /// Gets the exception that occurred while processing the
        /// async operation.
        /// </summary>
        internal Exception Exception { get; private set; }

        /// <summary>
        /// User supplied callback.
        /// </summary>
        internal AsyncCallback Callback { get; }

        /// <summary>
        /// SyncObject.
        /// </summary>
        internal object SyncObject { get; } = new object();

        /// <summary>
        /// Marks the async operation as completed.
        /// </summary>
        /// <param name="exception">
        /// Exception occured. null if no exception occured
        /// </param>
        internal void SetAsCompleted(Exception exception)
        {
            // Dbg.Assert(!isCompleted, "AsynResult already completed");
            if (IsCompleted)
            {
                return;
            }

            lock (SyncObject)
            {
                if (IsCompleted)
                {
                    return;
                }
                else
                {
                    Exception = exception;
                    IsCompleted = true;

                    // release the threads waiting on this operation.
                    SignalWaitHandle();
                }
            }

            // call the user supplied callback
            if (Callback != null)
            {
                Callback(this);
            }
        }

        /// <summary>
        /// Release the asyncResult without calling the callback.
        /// </summary>
        internal void Release()
        {
            if (!IsCompleted)
            {
                IsCompleted = true;
                SignalWaitHandle();
            }
        }

        #endregion

        #region internal methods

        /// <summary>
        /// Signal wait handle of this async result.
        /// </summary>
        internal void SignalWaitHandle()
        {
            lock (SyncObject)
            {
                if (_completedWaitHandle != null)
                {
                    _completedWaitHandle.Set();
                }
            }
        }

        /// <summary>
        /// Wait for the operation to complete and throw the exception if any.
        /// </summary>
        internal void EndInvoke()
        {
            _invokeOnThreadEvent = new AutoResetEvent(false);

            // Start the thread wait loop.
            WaitHandle[] waitHandles = new WaitHandle[2] { AsyncWaitHandle, _invokeOnThreadEvent };
            bool waiting = true;
            while (waiting)
            {
                int waitIndex = WaitHandle.WaitAny(waitHandles);

                if (waitIndex == 0)
                {
                    waiting = false;
                }
                else
                {
                    // Invoke callback on thread.
                    try
                    {
                        _invokeCallback(_invokeCallbackState);
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            AsyncWaitHandle.Dispose();
            _completedWaitHandle = null;  // Allow early GC

            _invokeOnThreadEvent.Dispose();
            _invokeOnThreadEvent = null;  // Allow early GC

            // Operation is done: if an exception occured, throw it
            if (Exception != null)
            {
                throw Exception;
            }
        }

        /// <summary>
        /// Use blocked thread to invoke callback delegate.
        /// </summary>
        /// <param name="callback">Callback delegate.</param>
        /// <param name="state">Callback state.</param>
        internal bool InvokeCallbackOnThread(WaitCallback callback, object state)
        {
            if (callback == null)
            {
                throw new PSArgumentNullException("callback");
            }

            _invokeCallback = callback;
            _invokeCallbackState = state;

            // Signal thread to run callback.
            if (_invokeOnThreadEvent != null)
            {
                _invokeOnThreadEvent.Set();
                return true;
            }

            return false;
        }

        #endregion
    }
}
