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

namespace Microsoft.PackageManagement.Internal.Utility.Collections {
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;

    internal class BlockingCollection<T> : System.Collections.Concurrent.BlockingCollection<T>, IEnumerable<T> {
        private MutableEnumerable<T> _blockingEnumerable;
        private readonly ManualResetEventSlim _activity = new ManualResetEventSlim(false);
        private readonly object _lock = new object();

        public WaitHandle Ready {
            get {
                return _activity.WaitHandle;
            }
        }

        public bool HasData {
            get {
                return Count > 0;
            }
        }

        public IEnumerator<T> GetEnumerator() {
            // make sure that iterating on this as enumerable is blocking.
            return this.GetBlockingEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        protected override void Dispose(bool isDisposing) {
            if (isDisposing) {
                _blockingEnumerable = null;
            }
        }

        private void SetActivity() {
            // setting _activity to true allows consumers to Wait for data to show up or for the producer to complete.
            if (HasData || IsCompleted) {
                _activity.Set();
            } else {
                _activity.Reset();
            }
        }

        public new void Add(T item) {
            lock (_lock) {
                if (!IsAddingCompleted) {
                    base.Add(item);
                }
            }
            SetActivity();
        }

        public new void Add(T item, CancellationToken cancellationToken) {
            lock (_lock) {
                if (!IsAddingCompleted) {
                    base.Add(item, cancellationToken);
                }
            }
            SetActivity();
        }

        public new IEnumerable<T> GetConsumingEnumerable() {
            return GetConsumingEnumerable(CancellationToken.None);
        }

        public new IEnumerable<T> GetConsumingEnumerable(CancellationToken cancellationToken) {
            T item;
            while (!IsCompleted && SafeTryTake(out item, 0, cancellationToken)) {
                yield return item;
            }
        }

        private bool SafeTryTake(out T item, int time, CancellationToken cancellationToken) {
            try {
                if (!cancellationToken.IsCancellationRequested && Count > 0) {
                    return TryTake(out item, time, cancellationToken);
                }
            } catch {
                // if this throws, that just means that we're done. (ie, canceled)
            } finally {
                SetActivity();
            }
            item = default(T);
            return false;
        }

        public IEnumerable<T> GetBlockingEnumerable() {
            return GetBlockingEnumerable(CancellationToken.None);
        }

        public IEnumerable<T> GetBlockingEnumerable(CancellationToken cancellationToken) {
            return _blockingEnumerable ?? (_blockingEnumerable = SafeGetBlockingEnumerable(cancellationToken).ReEnumerable());
        }

        private IEnumerable<T> SafeGetBlockingEnumerable(CancellationToken cancellationToken) {
            while (!IsCompleted && !cancellationToken.IsCancellationRequested) {
                T item;
                if (SafeTryTake(out item, -1, cancellationToken)) {
                    yield return item;
                }
                else {
                    _activity.Wait(10,cancellationToken);
                    //cancellationToken.WaitHandle.WaitOne(10);
                }
            }
        }

        public new void CompleteAdding() {
            lock (_lock) {
                base.CompleteAdding();
            }
            SetActivity();
        }
    }
}