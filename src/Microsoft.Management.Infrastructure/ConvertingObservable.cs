/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Diagnostics;

namespace Microsoft.Management.Infrastructure.Internal.Operations
{
    internal sealed class ConvertingObservable<TWrappedType, TTargetType> : IObservable<TTargetType>
        where TTargetType : class
    {
        private readonly IObservable<TWrappedType> _wrappedObservable;

        internal ConvertingObservable(IObservable<TWrappedType> wrappedObservable)
        {
            Debug.Assert(wrappedObservable != null, "Caller should verify wrappedObservable != null");
            this._wrappedObservable = wrappedObservable;
        }

        public IDisposable Subscribe(IObserver<TTargetType> observer)
        {
            var observerProxy = new ConvertingObserverProxy(observer);
            return this._wrappedObservable.Subscribe(observerProxy);
        }

        private class ConvertingObserverProxy : IObserver<TWrappedType>
        {
            private readonly IObserver<TTargetType> _targetObserver;
            internal ConvertingObserverProxy(IObserver<TTargetType> targetObserver)
            {
                Debug.Assert(targetObserver != null, "targetObserver != null");
                this._targetObserver = targetObserver;
            }

            public void OnCompleted()
            {
                this._targetObserver.OnCompleted();
            }

            public void OnError(Exception error)
            {
                this._targetObserver.OnError(error);
            }

            public void OnNext(TWrappedType value)
            {
                var targetValue = value as TTargetType;
                Debug.Assert(targetValue != null, "Expecting the conversion to always succeed");
                this._targetObserver.OnNext(targetValue);
            }
        }
    }
}