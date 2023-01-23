// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    /// <summary>
    /// <para>
    /// Async operation base class, it will issue async operation through
    /// 1...* CimSession object(s), processing the async results, extended
    /// pssemantics operations, and manage the lifecycle of created
    /// CimSession object(s).
    /// </para>
    /// </summary>
    internal abstract class CimAsyncOperation : IDisposable
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CimAsyncOperation"/> class.
        /// </summary>
        protected CimAsyncOperation()
        {
            this.moreActionEvent = new ManualResetEventSlim(false);
            this.actionQueue = new ConcurrentQueue<CimBaseAction>();
            this._disposed = 0;
            this.operationCount = 0;
        }

        #endregion

        #region Event handler

        /// <summary>
        /// <para>
        /// Handler used to handle new action event from
        /// <seealso cref="CimSessionProxy"/> object.
        /// </para>
        /// </summary>
        /// <param name="cimSession">
        /// <seealso cref="CimSession"/> object raised the event
        /// </param>
        /// <param name="actionArgs">Event argument.</param>
        protected void NewCmdletActionHandler(object cimSession, CmdletActionEventArgs actionArgs)
        {
            DebugHelper.WriteLogEx("Disposed {0}, action type = {1}", 0, this.Disposed, actionArgs.Action);

            if (this.Disposed)
            {
                if (actionArgs.Action is CimSyncAction)
                {
                    // unblock the thread waiting for response
                    (actionArgs.Action as CimSyncAction).OnComplete();
                }

                return;
            }

            bool isEmpty = this.actionQueue.IsEmpty;
            this.actionQueue.Enqueue(actionArgs.Action);
            if (isEmpty)
            {
                this.moreActionEvent.Set();
            }
        }

        /// <summary>
        /// <para>
        /// Handler used to handle new operation event from
        /// <seealso cref="CimSessionProxy"/> object.
        /// </para>
        /// </summary>
        /// <param name="cimSession">
        /// <seealso cref="CimSession"/> object raised the event.
        /// </param>
        /// <param name="actionArgs">Event argument.</param>
        protected void OperationCreatedHandler(object cimSession, OperationEventArgs actionArgs)
        {
            DebugHelper.WriteLogEx();

            lock (this.a_lock)
            {
                this.operationCount++;
            }
        }

        /// <summary>
        /// <para>
        /// Handler used to handle operation deletion event from
        /// <seealso cref="CimSessionProxy"/> object.
        /// </para>
        /// </summary>
        /// <param name="cimSession">
        /// <seealso cref="CimSession"/> object raised the event.
        /// </param>
        /// <param name="actionArgs">Event argument.</param>
        protected void OperationDeletedHandler(object cimSession, OperationEventArgs actionArgs)
        {
            DebugHelper.WriteLogEx();

            lock (this.a_lock)
            {
                this.operationCount--;
                if (this.operationCount == 0)
                {
                    this.moreActionEvent.Set();
                }
            }
        }

        #endregion

        /// <summary>
        /// <para>
        /// process all actions in the action queue
        /// </para>
        /// </summary>
        /// <param name="cmdletOperation">
        /// Wrapper of cmdlet, <seealso cref="CmdletOperationBase"/> for details.
        /// </param>
        public void ProcessActions(CmdletOperationBase cmdletOperation)
        {
            if (!this.actionQueue.IsEmpty)
            {
                CimBaseAction action;
                while (GetActionAndRemove(out action))
                {
                    action.Execute(cmdletOperation);
                    if (this.Disposed)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// <para>
        /// Process remaining actions until all operations are completed or
        /// current cmdlet is terminated by user.
        /// </para>
        /// </summary>
        /// <param name="cmdletOperation">
        /// Wrapper of cmdlet, <seealso cref="CmdletOperationBase"/> for details.
        /// </param>
        public void ProcessRemainActions(CmdletOperationBase cmdletOperation)
        {
            DebugHelper.WriteLogEx();

            while (true)
            {
                ProcessActions(cmdletOperation);
                if (!this.IsActive())
                {
                    DebugHelper.WriteLogEx("Either disposed or all operations completed.", 2);
                    break;
                }

                try
                {
                    this.moreActionEvent.Wait();
                    this.moreActionEvent.Reset();
                }
                catch (ObjectDisposedException ex)
                {
                    // This might happen if this object is being disposed,
                    // while another thread is processing the remaining actions
                    DebugHelper.WriteLogEx("moreActionEvent was disposed: {0}.", 2, ex);
                    break;
                }
            }

            ProcessActions(cmdletOperation);
        }

        #region helper methods

        /// <summary>
        /// <para>
        /// Get action object from action queue.
        /// </para>
        /// </summary>
        /// <param name="action">Next action to execute.</param>
        /// <returns>True indicates there is an valid action, otherwise false.</returns>
        protected bool GetActionAndRemove(out CimBaseAction action)
        {
            return this.actionQueue.TryDequeue(out action);
        }

        /// <summary>
        /// <para>
        /// Add temporary <seealso cref="CimSessionProxy"/> object to cache.
        /// </para>
        /// </summary>
        /// <param name="sessionproxy">Cimsession wrapper object.</param>
        protected void AddCimSessionProxy(CimSessionProxy sessionproxy)
        {
            lock (cimSessionProxyCacheLock)
            {
                this.cimSessionProxyCache ??= new List<CimSessionProxy>();

                if (!this.cimSessionProxyCache.Contains(sessionproxy))
                {
                    this.cimSessionProxyCache.Add(sessionproxy);
                }
            }
        }

        /// <summary>
        /// <para>
        /// Are there active operations?
        /// </para>
        /// </summary>
        /// <returns>True for having active operations, otherwise false.</returns>
        protected bool IsActive()
        {
            DebugHelper.WriteLogEx("Disposed {0}, Operation Count {1}", 2, this.Disposed, this.operationCount);
            bool isActive = (!this.Disposed) && (this.operationCount > 0);
            return isActive;
        }

        /// <summary>
        /// Create <see cref="CimSessionProxy"/> object.
        /// </summary>
        /// <param name="session"></param>
        protected CimSessionProxy CreateCimSessionProxy(CimSessionProxy originalProxy)
        {
            CimSessionProxy proxy = new(originalProxy);
            this.SubscribeEventAndAddProxytoCache(proxy);
            return proxy;
        }

        /// <summary>
        /// Create <see cref="CimSessionProxy"/> object.
        /// </summary>
        /// <param name="session"></param>
        protected CimSessionProxy CreateCimSessionProxy(CimSessionProxy originalProxy, bool passThru)
        {
            CimSessionProxy proxy = new CimSessionProxySetCimInstance(originalProxy, passThru);
            this.SubscribeEventAndAddProxytoCache(proxy);
            return proxy;
        }

        /// <summary>
        /// Create <see cref="CimSessionProxy"/> object.
        /// </summary>
        /// <param name="session"></param>
        protected CimSessionProxy CreateCimSessionProxy(CimSession session)
        {
            CimSessionProxy proxy = new(session);
            this.SubscribeEventAndAddProxytoCache(proxy);
            return proxy;
        }

        /// <summary>
        /// Create <see cref="CimSessionProxy"/> object.
        /// </summary>
        /// <param name="session"></param>
        protected CimSessionProxy CreateCimSessionProxy(CimSession session, bool passThru)
        {
            CimSessionProxy proxy = new CimSessionProxySetCimInstance(session, passThru);
            this.SubscribeEventAndAddProxytoCache(proxy);
            return proxy;
        }

        /// <summary>
        /// Create <see cref="CimSessionProxy"/> object, and
        /// add the proxy into cache.
        /// </summary>
        /// <param name="computerName"></param>
        protected CimSessionProxy CreateCimSessionProxy(string computerName)
        {
            CimSessionProxy proxy = new(computerName);
            this.SubscribeEventAndAddProxytoCache(proxy);
            return proxy;
        }

        /// <summary>
        /// Create <see cref="CimSessionProxy"/> object, and
        /// add the proxy into cache.
        /// </summary>
        /// <param name="computerName"></param>
        /// <param name="cimInstance"></param>
        /// <returns></returns>
        protected CimSessionProxy CreateCimSessionProxy(string computerName, CimInstance cimInstance)
        {
            CimSessionProxy proxy = new(computerName, cimInstance);
            this.SubscribeEventAndAddProxytoCache(proxy);
            return proxy;
        }

        /// <summary>
        /// Create <see cref="CimSessionProxy"/> object, and
        /// add the proxy into cache.
        /// </summary>
        /// <param name="computerName"></param>
        /// <param name="cimInstance"></param>
        /// <param name="passThru"></param>
        protected CimSessionProxy CreateCimSessionProxy(string computerName, CimInstance cimInstance, bool passThru)
        {
            CimSessionProxy proxy = new CimSessionProxySetCimInstance(computerName, cimInstance, passThru);
            this.SubscribeEventAndAddProxytoCache(proxy);
            return proxy;
        }

        /// <summary>
        /// Subscribe event from proxy and add proxy to cache.
        /// </summary>
        /// <param name="proxy"></param>
        protected void SubscribeEventAndAddProxytoCache(CimSessionProxy proxy)
        {
            this.AddCimSessionProxy(proxy);
            SubscribeToCimSessionProxyEvent(proxy);
        }

        /// <summary>
        /// <para>
        /// Subscribe to the events issued by <see cref="CimSessionProxy"/>.
        /// </para>
        /// </summary>
        /// <param name="proxy"></param>
        protected virtual void SubscribeToCimSessionProxyEvent(CimSessionProxy proxy)
        {
            DebugHelper.WriteLogEx();

            proxy.OnNewCmdletAction += this.NewCmdletActionHandler;
            proxy.OnOperationCreated += this.OperationCreatedHandler;
            proxy.OnOperationDeleted += this.OperationDeletedHandler;
        }

        /// <summary>
        /// Retrieve the base object out if wrapped in psobject.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected object GetBaseObject(object value)
        {
            PSObject psObject = value as PSObject;
            if (psObject == null)
            {
                return value;
            }
            else
            {
                object baseObject = psObject.BaseObject;
                var arrayObject = baseObject as object[];
                if (arrayObject == null)
                {
                    return baseObject;
                }
                else
                {
                    object[] arraybaseObject = new object[arrayObject.Length];
                    for (int i = 0; i < arrayObject.Length; i++)
                    {
                        arraybaseObject[i] = GetBaseObject(arrayObject[i]);
                    }

                    return arraybaseObject;
                }
            }
        }

        /// <summary>
        /// Retrieve the reference object or reference array object.
        /// The returned object has to be either CimInstance or CImInstance[] type,
        /// if not thrown exception.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="referenceType">Output the cimtype of the value, either Reference or ReferenceArray.</param>
        /// <returns>The object.</returns>
        protected object GetReferenceOrReferenceArrayObject(object value, ref CimType referenceType)
        {
            PSReference cimReference = value as PSReference;
            if (cimReference != null)
            {
                object baseObject = GetBaseObject(cimReference.Value);
                if (!(baseObject is CimInstance cimInstance))
                {
                    return null;
                }

                referenceType = CimType.Reference;
                return cimInstance;
            }
            else
            {
                object[] cimReferenceArray = value as object[];
                if (cimReferenceArray == null)
                {
                    return null;
                }
                else if (cimReferenceArray[0] is not PSReference)
                {
                    return null;
                }

                CimInstance[] cimInstanceArray = new CimInstance[cimReferenceArray.Length];
                for (int i = 0; i < cimReferenceArray.Length; i++)
                {
                    if (!(cimReferenceArray[i] is PSReference tempCimReference))
                    {
                        return null;
                    }

                    object baseObject = GetBaseObject(tempCimReference.Value);
                    cimInstanceArray[i] = baseObject as CimInstance;
                    if (cimInstanceArray[i] == null)
                    {
                        return null;
                    }
                }

                referenceType = CimType.ReferenceArray;
                return cimInstanceArray;
            }
        }
        #endregion

        #region IDisposable

        /// <summary>
        /// <para>
        /// Indicates whether this object was disposed or not
        /// </para>
        /// </summary>
        protected bool Disposed
        {
            get
            {
                return this._disposed == 1;
            }
        }

        private int _disposed;

        /// <summary>
        /// <para>
        /// Dispose() calls Dispose(true).
        /// Implement IDisposable. Do not make this method virtual.
        /// A derived class should not be able to override this method.
        /// </para>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SuppressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// <para>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// If disposing equals false, the method has been called by the
        /// runtime from inside the finalizer and you should not reference
        /// other objects. Only unmanaged resources can be disposed.
        /// </para>
        /// </summary>
        /// <param name="disposing">Whether it is directly called.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref this._disposed, 1, 0) == 0)
            {
                if (disposing)
                {
                    // free managed resources
                    Cleanup();
                }
                // free native resources if there are any
            }
        }

        /// <summary>
        /// <para>
        /// Clean up managed resources.
        /// </para>
        /// </summary>
        private void Cleanup()
        {
            DebugHelper.WriteLogEx();

            // unblock thread that waiting for more actions
            this.moreActionEvent.Set();
            CimBaseAction action;
            while (GetActionAndRemove(out action))
            {
                DebugHelper.WriteLog("Action {0}", 2, action);

                if (action is CimSyncAction)
                {
                    // unblock the thread waiting for response
                    (action as CimSyncAction).OnComplete();
                }
            }

            if (this.cimSessionProxyCache != null)
            {
                List<CimSessionProxy> temporaryProxy;
                lock (this.cimSessionProxyCache)
                {
                    temporaryProxy = new List<CimSessionProxy>(this.cimSessionProxyCache);
                    this.cimSessionProxyCache.Clear();
                }

                // clean up all proxy objects
                foreach (CimSessionProxy proxy in temporaryProxy)
                {
                    DebugHelper.WriteLog("Dispose proxy ", 2);
                    proxy.Dispose();
                }
            }

            this.moreActionEvent.Dispose();
            this.ackedEvent?.Dispose();

            DebugHelper.WriteLog("Cleanup complete.", 2);
        }

        #endregion

        #region private members

        /// <summary>
        /// Lock object.
        /// </summary>
        private readonly object a_lock = new();

        /// <summary>
        /// Number of active operations.
        /// </summary>
        private uint operationCount;

        /// <summary>
        /// Event to notify ps thread that more action is available.
        /// </summary>
        private readonly ManualResetEventSlim moreActionEvent;

        /// <summary>
        /// The following is the definition of action queue.
        /// The queue holding all actions to be executed in the context of either
        /// ProcessRecord or EndProcessing.
        /// </summary>
        private readonly ConcurrentQueue<CimBaseAction> actionQueue;

        /// <summary>
        /// Lock object.
        /// </summary>
        private readonly object cimSessionProxyCacheLock = new();

        /// <summary>
        /// Cache all <see cref="CimSessionProxy"/> objects related to
        /// the current operation.
        /// </summary>
        private List<CimSessionProxy> cimSessionProxyCache;

        #endregion

        #region protected members
        /// <summary>
        /// Event to notify ps thread that either a ACK message sent back
        /// or a error happened. Currently only used by
        /// <see cref="CimRegisterCimIndication"/>.
        /// </summary>
        protected ManualResetEventSlim ackedEvent;
        #endregion

        #region const strings
        internal const string ComputerNameArgument = @"ComputerName";
        internal const string CimSessionArgument = @"CimSession";
        #endregion
    }
}
