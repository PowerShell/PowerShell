// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    using System;
    using System.Diagnostics.Eventing;
    using System.Management.Automation.Tracing;
    using System.Threading;

    /// <summary>
    ///     An object that can be used to execute a method on a threadpool thread while correctly
    ///     managing system state, such as flowing ETW activities from the current thread to the
    ///     threadpool thread.
    /// </summary>
    public interface IBackgroundDispatcher
    {
        /// <summary>
        ///     Works the same as <see cref="ThreadPool.QueueUserWorkItem(WaitCallback)" />, except that it
        ///     also manages system state correctly.
        /// </summary>
        bool QueueUserWorkItem(WaitCallback callback);

        /// <summary>
        ///     Works the same as <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)" />, except that it
        ///     also manages system state correctly.
        /// </summary>
        bool QueueUserWorkItem(WaitCallback callback, object state);

        /// <summary>
        ///     Works the same as BeginInvoke would for any other delegate, except that it also manages system state correctly.
        /// </summary>
        IAsyncResult BeginInvoke(WaitCallback callback, object state, AsyncCallback completionCallback, object asyncState);

        /// <summary>
        ///     Works the same as EndInvoke would for any other delegate, except that it also manages system state correctly.
        /// </summary>
        void EndInvoke(IAsyncResult asyncResult);
    }

    /// <summary>
    ///     A simple implementation of <see cref="IBackgroundDispatcher" />.
    /// </summary>
    public class BackgroundDispatcher :
        IBackgroundDispatcher
    {
        #region Instance Data

        private readonly IMethodInvoker _etwActivityMethodInvoker;
        private readonly WaitCallback _invokerWaitCallback;

        #endregion

        #region Creation/Cleanup

        /// <summary>
        ///     Creates a <see cref="BackgroundDispatcher" /> that uses an <see cref="EtwEventCorrelator" />
        ///     for activity creation and correlation.
        /// </summary>
        /// <param name="transferProvider">The <see cref="EventProvider" /> to use when logging transfer events
        ///     during activity correlation.</param>
        /// <param name="transferEvent">The <see cref="EventDescriptor" /> to use when logging transfer events
        ///     during activity correlation.</param>
        public BackgroundDispatcher(EventProvider transferProvider, EventDescriptor transferEvent)
            : this(new EtwActivityReverterMethodInvoker(new EtwEventCorrelator(transferProvider, transferEvent)))
        {
            // nothing
        }

        // internal for unit testing only.  Otherwise, would be private.
        internal BackgroundDispatcher(IMethodInvoker etwActivityMethodInvoker)
        {
            ArgumentNullException.ThrowIfNull(etwActivityMethodInvoker);
            _etwActivityMethodInvoker = etwActivityMethodInvoker;
            _invokerWaitCallback = DoInvoker;
        }

        #endregion

        #region Instance Utilities

        private void DoInvoker(object invokerArgs)
        {
            var invokerArgsArray = (object[])invokerArgs;

            _etwActivityMethodInvoker.Invoker.DynamicInvoke(invokerArgsArray);
        }

        #endregion

        #region Instance Access

        /// <summary>
        ///     Implements <see cref="IBackgroundDispatcher.QueueUserWorkItem(WaitCallback)" />.
        /// </summary>
        public bool QueueUserWorkItem(WaitCallback callback)
        {
            return QueueUserWorkItem(callback, null);
        }

        /// <summary>
        ///     Implements <see cref="IBackgroundDispatcher.QueueUserWorkItem(WaitCallback, object)" />.
        /// </summary>
        public bool QueueUserWorkItem(WaitCallback callback, object state)
        {
            var invokerArgs = _etwActivityMethodInvoker.CreateInvokerArgs(callback, new object[] { state });

            var result = ThreadPool.QueueUserWorkItem(_invokerWaitCallback, invokerArgs);
            return result;
        }

        /// <summary>
        ///     Implements <see cref="IBackgroundDispatcher.BeginInvoke(WaitCallback, object, AsyncCallback, object)" />.
        /// </summary>
        public IAsyncResult BeginInvoke(WaitCallback callback, object state, AsyncCallback completionCallback, object asyncState)
        {
            var invokerArgs = _etwActivityMethodInvoker.CreateInvokerArgs(callback, new object[] { state });

            var result = _invokerWaitCallback.BeginInvoke(invokerArgs, completionCallback, asyncState);
            return result;
        }

        /// <summary>
        ///     Implements <see cref="IBackgroundDispatcher.EndInvoke(IAsyncResult)" />.
        /// </summary>
        public void EndInvoke(IAsyncResult asyncResult)
        {
            _invokerWaitCallback.EndInvoke(asyncResult);
        }

        #endregion
    }
}
