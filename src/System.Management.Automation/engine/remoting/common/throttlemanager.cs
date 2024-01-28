// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    #region OperationState

    /// <summary>
    /// Defines the different states of the operation.
    /// </summary>
    internal enum OperationState
    {
        /// <summary>
        /// Start operation completed successfully.
        /// </summary>
        StartComplete = 0,

        /// <summary>
        /// Stop operation completed successfully.
        /// </summary>
        StopComplete = 1,
    }

    /// <summary>
    /// Class describing event args which a helper class
    /// implementing IThrottleOperation need to throw.
    /// </summary>
    internal sealed class OperationStateEventArgs : EventArgs
    {
        /// <summary>
        /// Operation state.
        /// </summary>
        internal OperationState OperationState { get; set; }

        /// <summary>
        /// The original event which actually resulted in this
        /// event being raised.
        /// </summary>
        internal EventArgs BaseEvent { get; set; }
    }

    #endregion OperationState

    #region IThrottleOperation

    /// <summary>
    /// Interface which needs to be implemented by a class which wants to
    /// submit operations to the throttle manager.
    /// </summary>
    /// <remarks>Any synchronization that needs to be performed between
    /// StartOperation and StopOperation in the class that implements this
    /// interface should take care of handling the same. For instance,
    /// say New-Runspace class internally uses a class A which implements
    /// the IThrottleOperation interface. StartOperation of this
    /// class opens a runspace asynchronously on a remote machine. Stop
    /// operation is supposed to cancel the opening of this runspace. Any
    /// synchronization/cleanup issues should be handled by class A.
    /// </remarks>
    internal abstract class IThrottleOperation
    {
        /// <summary>
        /// This method should handle the actual operation which need to be
        /// controlled and performed. Examples of this can be Opening remote
        /// runspace, invoking expression in a remote runspace, etc. Once
        /// an event is successfully received as a result of this function,
        /// the handler has to ensure that it raises an OperationComplete
        /// event with StartComplete or StopComplete for the throttle manager
        /// to handle.
        /// </summary>
        internal abstract void StartOperation();

        /// <summary>
        /// This method should handle the situation when a stop signal is sent
        /// for this operation. For instance, when trying to open a set of
        /// remote runspaces, the user might hit ctrl-C. In which case, the
        /// pending runspaces to be opened will actually be signalled through
        /// this method to stop operation and return back. This method also
        /// needs to be asynchronous. Once an event is successfully received
        /// as a result of this function, the handler has to ensure that it
        /// raises an OperationComplete event with StopComplete for the
        /// throttle manager to handle. It is important that this function
        /// does not raise a StartComplete which will then result in the
        /// ThrottleComplete event not being raised by the throttle manager.
        /// </summary>
        internal abstract void StopOperation();

        /// <summary>
        /// Event which will be triggered when the operation is complete. It is
        /// assumed that all the operations performed by StartOperation and
        /// StopOperation are asynchronous. The submitter of operations may
        /// subscribe to this event to know when it's complete (or it can handle
        /// the synchronization with its scheduler) and the throttle
        /// manager will subscribe to this event to know that it's complete
        /// and to start the operation on the next item.
        /// </summary>
        internal abstract event EventHandler<OperationStateEventArgs> OperationComplete;

        /// <summary>
        /// This Property indicates whether an operation has been stopped.
        /// </summary>
        /// <remarks>
        /// In the initial implementation of ThrottleManager stopping
        /// individual operations was not supported. When the support
        /// for stopping individual operations was added, there was
        /// the following problem - if an operation is not there in
        /// the pending queue and in the startOperationQueue as well,
        /// then the following two scenarios are possible
        ///      (a) Operation was started and start completed
        ///      (b) Operation was started and stopped and both completed
        /// This property has been added in order to disambiguate between
        /// these two cases. When this property is set, StopOperation
        /// need not be called on the operation (this can be when the
        /// operation has stop completed or stop has been called and is
        /// pending)
        /// </remarks>
        internal bool IgnoreStop
        {
            get
            {
                return _ignoreStop;
            }

            set
            {
                _ignoreStop = true;
            }
        }

        private bool _ignoreStop = false;

        #region Runspace Debug

        /// <summary>
        /// When true enables runspace debugging for operations involving runspaces.
        /// </summary>
        internal bool RunspaceDebuggingEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// When true configures runspace debugging to stop at first opportunity.
        /// </summary>
        internal bool RunspaceDebugStepInEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// Event raised when operation runspace enters a debugger stopped state.
        /// </summary>
        internal event EventHandler<StartRunspaceDebugProcessingEventArgs> RunspaceDebugStop;

        /// <summary>
        /// RaiseRunspaceDebugStopEvent.
        /// </summary>
        /// <param name="runspace">Runspace.</param>
        internal void RaiseRunspaceDebugStopEvent(System.Management.Automation.Runspaces.Runspace runspace)
        {
            RunspaceDebugStop.SafeInvoke(this, new StartRunspaceDebugProcessingEventArgs(runspace));
        }

        #endregion
    }

    #endregion IThrottleOperation

    #region ThrottleManager

    /// <summary>
    /// Class which handles the throttling operations. This class is singleton and therefore
    /// when used either across cmdlets or at the infrastructure level it will ensure that
    /// there aren't more operations by way of accumulation than what is intended by design.
    ///
    /// This class contains a queue of items, each of which has the
    /// <see cref="System.Management.Automation.Remoting.IThrottleOperation">
    /// IThrottleOperation</see> interface implemented. To begin with
    /// THROTTLE_LIMIT number of items will be taken from the queue and the operations on
    /// them will be executed. Subsequently, as and when operations complete, new items from
    /// the queue will be taken and their operations executed.
    ///
    /// Whenever a consumer submits or adds operations, the methods will start as much
    /// operations from the queue as permitted based on the throttle limit. Also the event
    /// handler will start an operation once a previous event is completed.
    ///
    /// The queue used is a generic queue of type IThrottleOperations, as it will offer better
    /// performance.
    /// </summary>
    /// <remarks>Throttle limit is currently set to 50. This value may be modified later based
    /// on a figure that we may arrive at out of experience.</remarks>
    internal sealed class ThrottleManager : IDisposable
    {
        #region Public (internal) Properties

        /// <summary>
        /// Allows the consumer to override the default throttle limit.
        /// </summary>
        internal int ThrottleLimit
        {
            get
            {
                return _throttleLimit;
            }

            set
            {
                if (value > 0 && value <= s_THROTTLE_LIMIT_MAX)
                {
                    _throttleLimit = value;
                }
            }
        }

        private int _throttleLimit = s_DEFAULT_THROTTLE_LIMIT;

        #endregion Public (internal) Properties

        #region Public (internal) Methods

        /// <summary>
        /// Submit a list of operations that need to be throttled.
        /// </summary>
        /// <param name="operations">List of operations to be throttled.</param>
        /// <remarks>Once the operations are added to the queue, the method will
        /// start operations from the queue
        /// </remarks>
        internal void SubmitOperations(List<IThrottleOperation> operations)
        {
            lock (_syncObject)
            {
                // operations can be submitted only until submitComplete
                // is not set to true (happens when EndSubmitOperations is called)
                if (!_submitComplete)
                {
                    // add items to the queue
                    foreach (IThrottleOperation operation in operations)
                    {
                        Dbg.Assert(operation != null,
                            "Operation submitComplete to throttle manager cannot be null");
                        _operationsQueue.Add(operation);
                    }
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            // schedule operations here if possible
            StartOperationsFromQueue();
        }

        /// <summary>
        /// Add a single operation to the queue.
        /// </summary>
        /// <param name="operation">Operation to be added.</param>
        internal void AddOperation(IThrottleOperation operation)
        {
            // add item to the queue
            lock (_syncObject)
            {
                // operations can be submitted only until submitComplete
                // is not set to true (happens when EndSubmitOperations is called)
                if (!_submitComplete)
                {
                    Dbg.Assert(operation != null,
                        "Operation submitComplete to throttle manager cannot be null");

                    _operationsQueue.Add(operation);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            // start operations from queue if possible
            StartOperationsFromQueue();
        }

        /// <summary>
        /// Stop throttling operations.
        /// </summary>
        /// <remarks>Calling this method will also affect other cmdlets which
        /// could have potentially submitComplete operations for processing
        /// </remarks>
        /// <returns>Number of objects cleared from queue without being
        /// stopped.</returns>
        internal void StopAllOperations()
        {
            // if stopping is already in progress, make it a no op
            bool needToReturn = false;

            lock (_syncObject)
            {
                if (!_stopping)
                {
                    _stopping = true;
                }
                else
                {
                    needToReturn = true;
                }
            }

            if (needToReturn)
            {
                RaiseThrottleManagerEvents();
                return;
            }

            IThrottleOperation[] startOperationsInProcessArray;

            lock (_syncObject)
            {
                // no more submissions possible once stopped
                _submitComplete = true;

                // Clear all pending operations in queue so that they are not
                // scheduled when a stop operation completes
                _operationsQueue.Clear();

                // Make a copy of the in process queue so as to stop all
                // operations in progress
                startOperationsInProcessArray =
                        new IThrottleOperation[_startOperationQueue.Count];
                _startOperationQueue.CopyTo(startOperationsInProcessArray);

                // stop all operations in process (using the copy)
                foreach (IThrottleOperation operation in startOperationsInProcessArray)
                {
                    // When iterating through the array of operations in process
                    // it is quite possible that a runspace gets to the open state
                    // before stop is actually called on it. In that case, the
                    // OperationCompleteHandler will remove it from the
                    // operationsInProcess queue. Now when the runspace is closed
                    // the same handler will try removing it again and so there will
                    // be an exception. Hence adding it a second time before stop
                    // will ensure that the operation is available in the queue for
                    // removal. In case the stop succeeds before start succeeds then
                    // both will get removed (it goes without saying that there cannot
                    // be a situation where start succeeds after stop succeeded)
                    _stopOperationQueue.Add(operation);

                    operation.IgnoreStop = true;
                }
            }

            foreach (IThrottleOperation operation in startOperationsInProcessArray)
            {
                operation.StopOperation();
            }

            // Raise event as it can be that at this point, all operations are
            // complete
            RaiseThrottleManagerEvents();
        }

        /// <summary>
        /// Stop the specified operation.
        /// </summary>
        /// <param name="operation">Operation which needs to be stopped.</param>
        internal void StopOperation(IThrottleOperation operation)
        {
            // StopOperation is being called a second time
            // or the stop operation has already completed
            // - in either case just return
            if (operation.IgnoreStop)
            {
                return;
            }

            // If the operation has not yet been started, then
            // remove it from the pending queue
            if (_operationsQueue.IndexOf(operation) != -1)
            {
                lock (_syncObject)
                {
                    if (_operationsQueue.IndexOf(operation) != -1)
                    {
                        _operationsQueue.Remove(operation);
                        RaiseThrottleManagerEvents();
                        return;
                    }
                }
            }

            // The operation has already started, then add it
            // to the inprocess queue and call stop. Refer to
            // comment in StopAllOperations() as to why this is
            // being added a second time
            lock (_syncObject)
            {
                _stopOperationQueue.Add(operation);

                operation.IgnoreStop = true;
            }

            // stop the operation outside of the lock
            operation.StopOperation();
        }

        /// <summary>
        /// Signals that no more operations can be submitComplete
        /// for throttling.
        /// </summary>
        internal void EndSubmitOperations()
        {
            lock (_syncObject)
            {
                _submitComplete = true;
            }

            RaiseThrottleManagerEvents();
        }

        #endregion Public (internal) Methods

        #region Public (internal) Events

        /// <summary>
        /// Event raised when throttling all operations is complete.
        /// </summary>
        internal event EventHandler<EventArgs> ThrottleComplete;

        #endregion Public (internal) Events

        #region Constructors

        /// <summary>
        /// Public constructor.
        /// </summary>
        public ThrottleManager()
        {
            _operationsQueue = new List<IThrottleOperation>();
            _startOperationQueue = new List<IThrottleOperation>();
            _stopOperationQueue = new List<IThrottleOperation>();
            _syncObject = new object();
        }

        #endregion Constructors

        #region Private Methods

        /// <summary>
        /// Handler which handles state change for the object which implements
        /// the <see cref="System.Management.Automation.Remoting.IThrottleOperation"/>
        /// interface.
        /// </summary>
        /// <param name="source">Sender of the event.</param>
        /// <param name="stateEventArgs">Event information object which describes the event
        /// which triggered this method</param>
        private void OperationCompleteHandler(object source, OperationStateEventArgs stateEventArgs)
        {
            // An item has completed operation. If it's a start operation which completed
            // remove the instance from the startOperationqueue. If it's a stop operation
            // which completed, then remove the instance from both queues
            lock (_syncObject)
            {
                IThrottleOperation operation = source as IThrottleOperation;

                Dbg.Assert(operation != null, "Source of event should not be null");

                int index = -1;

                if (stateEventArgs.OperationState == OperationState.StartComplete)
                {
                    // A stop operation can be initiated before a start operation completes.
                    // A stop operation handler cleans up an outstanding start operation.
                    // So it is possible that a start operation complete callback will find the
                    // operation removed from the queue by an earlier stop operation complete.
                    index = _startOperationQueue.IndexOf(operation);
                    if (index != -1)
                    {
                        _startOperationQueue.RemoveAt(index);
                    }
                }
                else
                {
                    // for a stop operation, the same operation object would have been
                    // added to the stopOperationQueue as well. So we need to
                    // remove both the instances.
                    index = _startOperationQueue.IndexOf(operation);
                    if (index != -1)
                    {
                        _startOperationQueue.RemoveAt(index);
                    }

                    index = _stopOperationQueue.IndexOf(operation);
                    if (index != -1)
                    {
                        _stopOperationQueue.RemoveAt(index);
                    }

                    // if an operation signals a stopcomplete, it can mean
                    // that the operation has completed. In this case, we
                    // need to set the isStopped to true
                    operation.IgnoreStop = true;
                }
            }

            // It's possible that all operations are completed at this point
            // and submit is complete. So raise event
            RaiseThrottleManagerEvents();

            // Do necessary things for starting operation for the next item in the queue
            StartOneOperationFromQueue();
        }

        /// <summary>
        /// Method used to start the operation on one item in the queue.
        /// </summary>
        private void StartOneOperationFromQueue()
        {
            IThrottleOperation operation = null;

            lock (_syncObject)
            {
                if (_operationsQueue.Count > 0)
                {
                    operation = _operationsQueue[0];
                    _operationsQueue.RemoveAt(0);
                    operation.OperationComplete += OperationCompleteHandler;
                    _startOperationQueue.Add(operation);
                }
            }

            operation?.StartOperation();
        }

        /// <summary>
        /// Start operations to the limit possible from the queue.
        /// </summary>
        private void StartOperationsFromQueue()
        {
            int operationsInProcessCount = 0;
            int operationsQueueCount = 0;

            lock (_syncObject)
            {
                operationsInProcessCount = _startOperationQueue.Count;
                operationsQueueCount = _operationsQueue.Count;
            }

            int remainingCap = _throttleLimit - operationsInProcessCount;

            if (remainingCap > 0)
            {
                int numOperations = (remainingCap > operationsQueueCount) ? operationsQueueCount : remainingCap;

                for (int i = 0; i < numOperations; i++)
                {
                    StartOneOperationFromQueue();
                }
            }
        }

        /// <summary>
        /// Raise the throttle manager events once the conditions are met.
        /// </summary>
        private void RaiseThrottleManagerEvents()
        {
            bool readyToRaise = false;

            lock (_syncObject)
            {
                // if submit is complete, there are no operations in progress and
                // the pending queue is empty, then raise events
                if (_submitComplete &&
                    _startOperationQueue.Count == 0 &&
                    _stopOperationQueue.Count == 0 &&
                    _operationsQueue.Count == 0)
                {
                    readyToRaise = true;
                }
            }

            if (readyToRaise)
            {
                ThrottleComplete.SafeInvoke(this, EventArgs.Empty);
            }
        }

        #endregion Private Methods

        #region Private Members

        /// <summary>
        /// Default throttle limit - the maximum number of operations
        /// to be processed at a time.
        /// </summary>
        private static readonly int s_DEFAULT_THROTTLE_LIMIT = 32;

        /// <summary>
        /// Maximum value that the throttle limit can be set to.
        /// </summary>
        private static readonly int s_THROTTLE_LIMIT_MAX = int.MaxValue;

        /// <summary>
        /// All pending operations.
        /// </summary>
        private readonly List<IThrottleOperation> _operationsQueue;

        /// <summary>
        /// List of items on which a StartOperation has
        /// been called.
        /// </summary>
        private readonly List<IThrottleOperation> _startOperationQueue;

        /// <summary>
        /// List of items on which a StopOperation has
        /// been called.
        /// </summary>
        private readonly List<IThrottleOperation> _stopOperationQueue;

        /// <summary>
        /// Object used to synchronize access to the queues.
        /// </summary>
        private readonly object _syncObject;

        private bool _submitComplete = false;                    // to check if operations have been submitComplete
        private bool _stopping = false;                      // if stop is in process

        #endregion Private Members

        #region IDisposable Overrides

        /// <summary>
        /// Dispose method of IDisposable. Any cmdlet that uses
        /// the throttle manager needs to call this method from its
        /// Dispose method.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Internal dispose method which does the actual dispose
        /// operations and finalize suppressions.
        /// </summary>
        /// <param name="disposing">If method is called from
        /// disposing of destructor</param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopAllOperations();
            }
        }

        #endregion IDisposable Overrides
    }

    #endregion ThrottleManager

    #region Helper Class for Testing

#if !CORECLR // Skip The Helper Class for Testing (Thread.Abort() Not In CoreCLR)
    internal class Operation : IThrottleOperation
    {
        private ThreadStart workerThreadDelegate;
        private Thread workerThreadStart;
        private Thread workerThreadStop;

        public bool Done { get; set; }

        public int SleepTime { get; set; } = 100;

        private void WorkerThreadMethodStart()
        {
            Thread.Sleep(SleepTime);
            Done = true;
            OperationStateEventArgs operationStateEventArgs =
                    new OperationStateEventArgs();
            operationStateEventArgs.OperationState = OperationState.StartComplete;
            OperationComplete.SafeInvoke(this, operationStateEventArgs);
        }

        private void WorkerThreadMethodStop()
        {
            workerThreadStart.Abort();

            OperationStateEventArgs operationStateEventArgs =
                    new OperationStateEventArgs();
            operationStateEventArgs.OperationState = OperationState.StopComplete;
            OperationComplete.SafeInvoke(this, operationStateEventArgs);
        }

        internal Operation()
        {
            Done = false;
            workerThreadDelegate = new ThreadStart(WorkerThreadMethodStart);
            workerThreadStart = new Thread(workerThreadDelegate);
            workerThreadDelegate = new ThreadStart(WorkerThreadMethodStop);
            workerThreadStop = new Thread(workerThreadDelegate);
        }

        internal override void StartOperation()
        {
            workerThreadStart.Start();
        }

        internal override void StopOperation()
        {
            workerThreadStop.Start();
        }

        internal override event EventHandler<OperationStateEventArgs> OperationComplete;

        internal event EventHandler<EventArgs> InternalEvent = null;

        internal event EventHandler<EventArgs> EventHandler
        {
            add
            {
                bool firstEntry = (InternalEvent == null);

                InternalEvent += value;

                if (firstEntry)
                {
                    OperationComplete += new EventHandler<OperationStateEventArgs>(Operation_OperationComplete);
                }
            }

            remove
            {
                InternalEvent -= value;
            }
        }

        private void Operation_OperationComplete(object sender, OperationStateEventArgs e)
        {
            InternalEvent.SafeInvoke(sender, e);
        }

        internal static void SubmitOperations(List<object> operations, ThrottleManager throttleManager)
        {
            List<IThrottleOperation> newOperations = new List<IThrottleOperation>();
            foreach (object operation in operations)
            {
                newOperations.Add((IThrottleOperation)operation);
            }

            throttleManager.SubmitOperations(newOperations);
        }

        internal static void AddOperation(object operation, ThrottleManager throttleManager)
        {
            throttleManager.AddOperation((IThrottleOperation)operation);
        }
    }
#endif

    #endregion Helper Class for Testing
}
