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
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;

    internal class CancellableEnumerator<T> : IEnumerator<T>, ICancellableEnumerator<T> {
        private IEnumerator _enumerator;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public CancellableEnumerator(CancellationTokenSource cts, IEnumerator enumerator) {
            _enumerator = enumerator;
            _cancellationTokenSource = cts;
        }

        public void Cancel() {
            _cancellationTokenSource.Cancel();
        }

        public bool MoveNext() {
            // if the collection has been cancelled, then don't advance anymore.
            return !_cancellationTokenSource.IsCancellationRequested && _enumerator.MoveNext();
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Reset() {
            _enumerator.Reset();
        }

        public T Current {
            get {
                return (T)_enumerator.Current;
            }
        }

        object IEnumerator.Current {
            get {
                return _enumerator.Current;
            }
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                if (_enumerator is IDisposable) {
                    (_enumerator as IDisposable).Dispose();
                }
                _enumerator = null;
            }
        }
    }
}