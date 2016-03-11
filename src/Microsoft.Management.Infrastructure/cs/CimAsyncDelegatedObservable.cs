/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Management.Infrastructure.Internal.Operations
{
    internal class CimAsyncDelegatedObservable<T> : IObservable<T>
    {
        private readonly Action<IObserver<T>> _subscribe;

        internal CimAsyncDelegatedObservable(Action<IObserver<T>> subscribe)
        {
            Debug.Assert(subscribe != null, "Caller should verify subscribe != null");
            this._subscribe = subscribe;
        }

        #region IObservable<T> Members

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException("observer");
            }

            ThreadPool.QueueUserWorkItem(
                    delegate
                    {
                        this._subscribe(observer);
                    });

            // cannot cancel delegate-based observables
            return EmptyDisposable.Singleton;
        }

        #endregion
    }
}