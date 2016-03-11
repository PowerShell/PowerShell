/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Diagnostics;

namespace Microsoft.Management.Infrastructure.Generic
{
    /// <summary>
    /// <para>
    /// CimAsyncResult represents an asynchronous operation that returns a single object of type <typeparamref name="T"/>.
    /// </para>
    /// <para>
    /// The operation wrapped by CimAsyncResult doesn't start until the <see cref="Subscribe"/> method is called.
    /// It is allowed to call <see cref="Subscribe"/> more than once - each call will start another operation.
    /// </para>
    /// <para>
    /// Results of an operation are asynchronously communicated to the observer passed to the <see cref="Subscribe"/> method.
    /// </para>
    /// <para>
    /// See <see cref="IObservable&lt;T&gt;"/>  and <see cref="IObserver&lt;T&gt;"/> for more details 
    /// about the asynchronous pattern exposed by this class.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Type of results that the wrapped operation returns.</typeparam>
    public class CimAsyncResult<T> : IObservable<T>
    {
        private readonly IObservable<T> _wrappedObservable;

        internal CimAsyncResult(IObservable<T> wrappedObservable)
        {
            Debug.Assert(wrappedObservable != null, "Caller should verify wrappedObservable != null");
            this._wrappedObservable = wrappedObservable;
        }

        #region IObservable<T> Members

        /// <summary>
        /// <para>
        /// Starts the operation and then communicates the results back to the given <paramref name="observer"/>.
        /// </para>
        /// <para>
        /// It is allowed to call <see cref="Subscribe"/> more than once - each call will start another operation.
        /// </para>
        /// </summary>
        /// <param name="observer">Observer that will receive asynchronous notifications about results of the operation</param>
        /// <returns>IDiposable object that will cancel the operation when disposed.</returns>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException("observer");
            }

            return this._wrappedObservable.Subscribe(observer);
        }

        #endregion
    }
}