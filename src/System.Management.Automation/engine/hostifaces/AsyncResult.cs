/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Threading;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Runspaces
{
    /// <summary>
    /// Base class for AsyncResult objects that are returned by various
    /// Async operations supported by RunspacePool , PowerShell types
    /// </summary>
    internal class AsyncResult : IAsyncResult
    {
        #region Private Data

        private Guid _ownerId;
        private bool _isCompleted;
        private ManualResetEvent _completedWaitHandle;
        // exception occured in the async thread.
        private Exception _exception;
        private AsyncCallback _callback;
        // user supplied state object
        private object _state;
        private object _syncObject = new object();

        // Invoke on thread (remote debugging support).
        private AutoResetEvent _invokeOnThreadEvent;
        private WaitCallback _invokeCallback;
        private object _invokeCallbackState;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ownerId">
        /// Instace Id of the object creating this instance
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
            _ownerId = ownerId;
            _callback = callback;
            _state = state;
        }

        #endregion

        #region IAsync Overrides

        /// <summary>
        /// This always returns false
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
        public bool IsCompleted
        {
            get
            {
                return _isCompleted;
            }
        }

        /// <summary>
        /// This is not supported and returns null. 
        /// </summary>
        public object AsyncState
        {
            get { return _state; }
        }

        /// <summary>
        /// Gets a System.Threading.WaitHandle that is used to wait for an asynchronous
        /// operation to complete.
        /// </summary>
        public WaitHandle AsyncWaitHandle
        {
            get
            {
                if (null == _completedWaitHandle)
                {
                    lock (_syncObject)
                    {
                        if (null == _completedWaitHandle)
                        {
                            _completedWaitHandle = new ManualResetEvent(_isCompleted);
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
        internal Guid OwnerId
        {
            get { return _ownerId; }
        }

        /// <summary>
        /// Gets the exception that occurred while processing the
        /// async operation.
        /// </summary>
        internal Exception Exception
        {
            get { return _exception; }
        }

        /// <summary>
        /// User supplied callback.
        /// </summary>
        internal AsyncCallback Callback
        {
            get { return _callback; }
        }

        /// <summary>
        /// SyncObject
        /// </summary>
        internal object SyncObject
        {
            get { return _syncObject; }
        }

        /// <summary>
        /// Marks the async operation as completed.
        /// </summary>
        /// <param name="exception">
        /// Exception occured. null if no exception occured
        /// </param>
        internal void SetAsCompleted(Exception exception)
        {
            //Dbg.Assert(!isCompleted, "AsynResult already completed");
            if (_isCompleted)
            {
                return;
            }

            lock (_syncObject)
            {
                if (_isCompleted)
                {
                    return;
                }
                else
                {
                    _exception = exception;
                    _isCompleted = true;

                    // release the threads waiting on this operation.
                    SignalWaitHandle();
                }
            }

            // call the user supplied callback
            if (null != _callback)
            {
                _callback(this);
            }
        }

        /// <summary>
        /// Release the asyncResult without calling the callback.
        /// </summary>
        internal void Release()
        {
            if (!_isCompleted)
            {
                _isCompleted = true;
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
            lock (_syncObject)
            {
                if (null != _completedWaitHandle)
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
                    catch (Exception e)
                    {
                        CommandProcessorBase.CheckForSevereException(e);
                    }
                }
            }

            AsyncWaitHandle.Dispose();
            _completedWaitHandle = null;  // Allow early GC

            _invokeOnThreadEvent.Dispose();
            _invokeOnThreadEvent = null;  // Allow early GC

            // Operation is done: if an exception occured, throw it
            if (null != _exception)
            {
                throw _exception;
            }
        }

        /// <summary>
        /// Use blocked thread to invoke callback delegate.
        /// </summary>
        /// <param name="callback">Callback delegate</param>
        /// <param name="state">Callback state</param>
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
