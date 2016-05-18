// 
//  Copyright (c) Microsoft Corporation. All rights reserved. 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//  

namespace Microsoft.PackageManagement.Internal.Utility.Async {
    using System;
    using System.Diagnostics;
    using System.Threading;

    public abstract class AsyncAction : IAsyncAction {
        private static readonly TimeSpan DefaultCallTimeout = TimeSpan.FromMinutes(120);
        private static readonly TimeSpan DefaultResponsiveness = TimeSpan.FromMinutes(120);
        private ActionState _actionState;
        protected DateTime _callStart = DateTime.Now;
        private DisposalState _disposalState;
        protected Thread _invocationThread;
        private DateTime _lastActivity = DateTime.Now;
        private TimeSpan _responsiveness = DefaultResponsiveness;
        private TimeSpan _timeout = DefaultCallTimeout;
        private Timer _timer;
        protected readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEventSlim _completed = new ManualResetEventSlim(false);
        private readonly object _lock = new Object();

        protected AsyncAction() {
            _timer = new Timer(Signalled, this, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        }

        private TimeSpan TimeLeft {
            get {
                if (_actionState >= ActionState.Aborting) {
                    return TimeSpan.FromMilliseconds(System.Threading.Timeout.Infinite);
                }

                if (_actionState >= ActionState.Cancelling) {
                    var timeToAbort = _responsiveness.Subtract(DateTime.Now.Subtract(_lastActivity));
                    return timeToAbort < TimeSpan.Zero ? TimeSpan.Zero : timeToAbort;
                }

                var timeToRespond = _responsiveness.Subtract(DateTime.Now.Subtract(_lastActivity));
                if (timeToRespond < TimeSpan.Zero) {
                    timeToRespond = TimeSpan.Zero;
                }

                var timeToTimeout = _timeout.Subtract(DateTime.Now.Subtract(_callStart));
                if (timeToTimeout < TimeSpan.Zero) {
                    timeToTimeout = TimeSpan.Zero;
                }
                return timeToRespond < timeToTimeout ? timeToRespond : timeToTimeout;
            }
        }

        public event Action OnComplete;
        public event Action OnCancel;
        public event Action OnAbort;

        public virtual void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Cancel() {
            // if it's already done, this is a no-op
            lock (_lock) {
                if (_actionState >= ActionState.Cancelling) {
                    return;
                }
                _actionState = ActionState.Cancelling;
            }

            // activate the cancellation token for those who are watching for that.
            _cancellationTokenSource.Cancel();

            // actively tell anyone who is listening that we're trying to cancel this.
            if (OnCancel != null) {
                OnCancel();
            }
            lock (_lock) {
                if (_actionState < ActionState.Canceled) {
                    _actionState = ActionState.Canceled;
                }
            }
        }

        public virtual void Abort() {
            // make sure we're at least cancelled first!
            Cancel();

            // if it's already done, this is a no-op
            lock (_lock) {
                if (_actionState >= ActionState.Aborting) {
                    return;
                }
                _actionState = ActionState.Aborting;
            }

            // we have no need left for this.
            DisposeTimer();

            // notify any listeners that we're about to kill this.
            if (OnAbort != null) {
                OnAbort();
            }

            lock (_lock) {
                if (_actionState < ActionState.Aborted) {
                    _actionState = ActionState.Aborted;
                }
            }

            // and make sure this is marked as complete
            Complete();
        }

        public WaitHandle CompleteEvent {
            get {
                return _completed.WaitHandle;
            }
        }

        public TimeSpan Timeout {
            get {
                return _timeout;
            }
            set {
                _timeout = value;
                ResetTimer();
            }
        }

        public TimeSpan Responsiveness {
            get {
                return _responsiveness;
            }
            set {
                _responsiveness = value;
                ResetTimer();
            }
        }

        public virtual bool IsCanceled {
            get {
                return _cancellationTokenSource.IsCancellationRequested;
            }
        }

        public bool IsAborted {
            get {
                return _actionState == ActionState.Aborting || _actionState == ActionState.Aborted;
            }
        }

        public bool IsCompleted {
            get {
                return _completed.IsSet;
            }
        }

        public virtual void Dispose(bool disposing) {
            lock (_lock) {
                // make sure this kind of thing doesn't happen twice.
                if (_disposalState > DisposalState.None) {
                    return;
                }
                _disposalState = DisposalState.Disposing;
            }

            if (disposing) {
                // Ensure we're cancelled first.
                Cancel();

                if (_actionState < ActionState.Aborting) {
                    // if we're not already in the process of aborting, we'll kick that off in a few seconds.
                    _timer.Change(5000, System.Threading.Timeout.Infinite);
                } else {
                    // stop timer activity
                    DisposeTimer();
                }

                // for all intents, we're completed...even if the abort will run after this.
                _completed.Set();
                _disposalState = DisposalState.Disposed;
                if (_actionState >= ActionState.Completed) {
                    _completed.Dispose();
                    _cancellationTokenSource.Dispose();
                }
            }
        }

        private void DisposeTimer() {
            lock (_lock) {
                if (_timer != null) {
                    _timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                    _timer.Dispose();
                    _timer = null;
                }
            }
        }

        protected virtual void Complete() {
            lock (_lock) {
                if (_actionState == ActionState.Completed) {
                    return;
                }
                _actionState = ActionState.Completed;
            }

            DisposeTimer();

            if (OnComplete != null) {
                OnComplete();
            }
            _completed.Set();
        }

        public virtual void WarnBeforeResponsivenessCancellation() {
        }

        public virtual void WarnBeforeTimeoutCancellation() {
        }

        private void Signalled(object obj) {
            // if we're in the debugger, don't ever automatically cancel or abort.

            if (Debugger.IsAttached) {
                return;
            }

            if (_actionState > ActionState.Aborting) {
                // we don't have anything to do here.
                return;
            }

            if (_actionState < ActionState.Cancelling) {
                if (_responsiveness.Subtract(DateTime.Now.Subtract(_lastActivity)) < TimeSpan.Zero) {
                    // we have to cancel because the provider isn't responsive enough
                    WarnBeforeResponsivenessCancellation();
                } else {
                    // we have to cancel because the provider didn't complete the call in the time required.
                    WarnBeforeTimeoutCancellation();
                }

                // disabling actual cancellation until providers can deal correctly.
                // Cancel();
                return;
            }

            if (_actionState == ActionState.Cancelling) {
                // we were in a cancelled state when we noticed the timer hit zero.
                return;
            }

            if (_actionState == ActionState.Canceled) {
                // we were in a cancelled state when we noticed the timer hit zero.
                Abort();
            }
        }

        protected void Activity() {
            _lastActivity = DateTime.Now;
            ResetTimer();
        }

        protected void StartCall() {
            _callStart = DateTime.Now;
            ResetTimer();
        }

        private void ResetTimer() {
            lock (_lock) {
                if (_actionState <= ActionState.Canceled && _timer != null) {
                    _timer.Change(TimeLeft, TimeSpan.FromMilliseconds(System.Threading.Timeout.Infinite));
                }
            }
        }

        private enum ActionState {
            None,
            Called,
            Cancelling,
            Canceled,
            Aborting,
            Aborted,
            Completed,
        }

        private enum DisposalState {
            None,
            Disposing,
            Disposed
        }
    }
}