/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
#if(!_CORECLR)
using System.Net;
using System.Net.Sockets;
#endif
using System.Threading;
using Microsoft.Management.Infrastructure.Generic;
using Microsoft.Management.Infrastructure.Internal;
using Microsoft.Management.Infrastructure.Internal.Operations;
using Microsoft.Management.Infrastructure.Options;
using Microsoft.Management.Infrastructure.Options.Internal;

namespace Microsoft.Management.Infrastructure
{
    /// <summary>
    /// <para>
    /// Represents a communication channel to a CIM server.
    /// </para>
    /// <para>
    /// This is the main entry point of the Microsoft.Management.Infrastructure API.
    /// All CIM operations are represented as methods of this class.
    /// </para>
    /// </summary>
    public class CimSession : IDisposable
    {
        private readonly Native.SessionHandle _handle;

        private CimSession(Native.SessionHandle handle, string computerName)
        {
            Debug.Assert(handle != null, "Caller should verify that handle is valid (1)");
            handle.AssertValidInternalState();
            this._handle = handle;

            this.ComputerName = computerName;
            this.InstanceId = Guid.NewGuid();
        }

        #region Properties

        /// <summary>
        /// Gets the computer name used to create this session
        /// </summary>
        public string ComputerName { get; private set; }

        /// <summary>
        /// Gets the unique instance id of the cimsession object,
        /// which can be used to identify the cimsession unifily.
        /// </summary>
        public Guid InstanceId { get; private set; }

        #endregion

        #region Create

        /// <summary>
        /// <para>
        /// Creates a new <see cref="CimSession"/> using default <see cref="CimSessionOptions"/>.
        /// </para>
        /// <para>
        /// Depending on the transport, the act of creating a session might not perform any communication with the CIM server.
        /// To verify the connection parameters, you can use TestConnection() method.
        /// </para>
        /// </summary>
        /// <param name="computerName">
        /// Name of the CIM server.
        /// <c>null</c> indicates a localhost.
        /// </param>
        /// <returns></returns>
        static public CimSession Create(string computerName)
        {
            return CimSession.Create(computerName, null);
        }

        /// <summary>
        /// <para>
        /// Creates a new <see cref="CimSession"/> using specified <paramref name="sessionOptions"/>
        /// </para>
        /// <para>
        /// Depending on the transport, the act of creating a session might not perform any communication with the CIM server.
        /// To verify the connection parameters, you can use TestConnection() method.
        /// </para>
        /// </summary>
        /// <param name="computerName">
        /// Name of the CIM server.
        /// <c>null</c> indicates a localhost.
        /// </param>
        /// <param name="sessionOptions"></param>
        /// <returns></returns>
        static public CimSession Create(string computerName, CimSessionOptions sessionOptions)
        {
            string normalizedComputerName = computerName;

            if (!string.IsNullOrEmpty(normalizedComputerName))
            {
#if(!_CORECLR)
                //
                // System.Network is support in FULL CLR and not in CORE CLR.
                //

                IPAddress ipAddress;
                if (IPAddress.TryParse(normalizedComputerName, out ipAddress) &&
                    (ipAddress.AddressFamily == AddressFamily.InterNetworkV6) &&
                    (normalizedComputerName[0] != '['))
                {
                    normalizedComputerName = @"[" + normalizedComputerName + @"]";
                }
#else
                //
                // CoreCLR: Check if its IPV6 address.
                //
                // Note: IPAddress.TryParse() basically looks for ':' to check if its a IPv6 
                // address, we are following the same pattern here.
                //
                if ( ( normalizedComputerName.IndexOf( ':' ) != -1 ) &&
                     ( normalizedComputerName[ 0 ] != '[' ) )
                {
                    normalizedComputerName = @"[" + normalizedComputerName + @"]";
                }
#endif
            }

            Native.InstanceHandle extendedErrorHandle;
            Native.SessionHandle sessionHandle;

            Native.MiResult result = Native.ApplicationMethods.NewSession(
                CimApplication.Handle,
                sessionOptions == null ? null : sessionOptions.Protocol,
                normalizedComputerName,
                sessionOptions == null ? null : sessionOptions.DestinationOptionsHandle,
                out extendedErrorHandle,
                out sessionHandle);

            if (result == Native.MiResult.NOT_FOUND)
            {
                throw new CimException(result, null, extendedErrorHandle, Strings.UnrecognizedProtocolName);
            }
            CimException.ThrowIfMiResultFailure(result, extendedErrorHandle);

            var session = new CimSession(sessionHandle, computerName);
            return session;
        }

        /// <summary>
        /// <para>
        /// Creates a new <see cref="CimSession"/> using default <see cref="CimSessionOptions"/>.
        /// </para>
        /// <para>
        /// Depending on the transport, the act of creating a session might not perform any communication with the CIM server.
        /// To verify the connection parameters, you can use TestConnection() method.
        /// </para>
        /// </summary>
        /// <param name="computerName">
        /// Name of the CIM server.
        /// <c>null</c> indicates a localhost.
        /// </param>
        /// <returns></returns>
        static public CimAsyncResult<CimSession> CreateAsync(string computerName)
        {
            return CimSession.CreateAsync(computerName, null);
        }

        /// <summary>
        /// <para>
        /// Creates a new <see cref="CimSession"/> using specified <paramref name="sessionOptions"/>
        /// </para>
        /// <para>
        /// Depending on the transport, the act of creating a session might not perform any communication with the CIM server.
        /// To verify the connection parameters, you can use TestConnection() method.
        /// </para>
        /// </summary>
        /// <param name="computerName">
        /// Name of the CIM server.
        /// <c>null</c> indicates a localhost.
        /// </param>
        /// <param name="sessionOptions"></param>
        /// <returns></returns>
        static public CimAsyncResult<CimSession> CreateAsync(string computerName, CimSessionOptions sessionOptions)
        {
            // native API doesn't provide async functionality in MI_Application_NewSession
            IObservable<CimSession> observable = new CimAsyncDelegatedObservable<CimSession>(
                    delegate(IObserver<CimSession> observer)
                    {
                        Debug.Assert(observer != null, "Caller should verify observer != null");

                        CimSession result = null;
                        try
                        {
                            result = Create(computerName, sessionOptions);
                        }
                        catch (Exception e)
                        {
                            observer.OnError(e);
                        }
                        observer.OnNext(result);
                        observer.OnCompleted();
                    });

            return new CimAsyncResult<CimSession>(observable);
        }

        #endregion

        #region Close

        /// <summary>
        /// <para>
        /// Closes the session.  
        /// </para>
        /// <para>
        /// This method cancels all active operations of this session and returns when all of them have completed.
        /// </para>
        /// </summary>
        public void Close()
        {
            lock (this._disposeThreadSafetyLock)
            {
                if (this._disposed)
                {
                    return;
                }
                this._disposed = true;
            }
            Native.MiResult result = this._handle.ReleaseHandleSynchronously();
            CimException.ThrowIfMiResultFailure(result);
        }

        /// <summary>
        /// <para>
        /// Closes the session.  
        /// </para>
        /// <para>
        /// This method cancels all active operations of this session
        /// </para>
        /// </summary>
        /// <returns></returns>
        public CimAsyncStatus CloseAsync()
        {
            IObservable<object> observable = new CimAsyncDelegatedObservable<object>(
                delegate(IObserver<object> observer)
                    {
                        Debug.Assert(observer != null, "Caller should verify observer != null");

                        bool alreadyDisposed;
                        lock (this._disposeThreadSafetyLock)
                        {
                            alreadyDisposed = this._disposed;
                            this._disposed = true;
                        }
                        if (alreadyDisposed)
                        {
                            observer.OnCompleted();
                        }
                        else
                        {
                            CloseAsyncImpersonationWorker worker = new CloseAsyncImpersonationWorker(observer);

                            Native.MiResult result = this._handle.ReleaseHandleAsynchronously(worker.OnCompleted); // native API only reports completion (i.e. no equivalent to IObserver.OnError)
                            CimException exception = CimException.GetExceptionIfMiResultFailure(result, null, null);
                            if (exception != null)
                            {
                                observer.OnError(exception);
                                worker.Dispose();
                            }
                        }
                    });

            return new CimAsyncStatus(observable);
        }

        // helper class for capturing and restoring the ExecutionContext, before calling IObserver.OnCompleted
        private class CloseAsyncImpersonationWorker : IDisposable
        {
#if(!_CORECLR)
            private ExecutionContext _executionContext = ExecutionContext.Capture();
#endif
            private IObserver<object> _wrappedObserver;

            internal CloseAsyncImpersonationWorker(IObserver<object> wrappedObserver)
            {
                Debug.Assert(wrappedObserver != null, "Caller should make sure wrappedObserver != null");
                this._wrappedObserver = wrappedObserver;
            }

            internal void OnCompleted()
            {
#if(!_CORECLR)

                ExecutionContext.Run(this._executionContext, this.OnCompletedCore, state: null);
#else
                //
                // TODO: SHOULD IDEALLY USE OnCompletedCore() call instead
                // TODO: Maybe we should call OnCompletedCore with NULL state
                //
                this._wrappedObserver.OnCompleted();
#endif
                this.Dispose();
            }

#if(!_CORECLR)
            private void OnCompletedCore(object state)
            {
                this._wrappedObserver.OnCompleted();
            }
#endif

            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected void Dispose(bool disposing)
            {
                if (disposing)
                {
#if(!_CORECLR)
                    this._executionContext.Dispose();
#endif
                }
            }
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Releases resources associated with this object
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases resources associated with this object
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            lock (_disposeThreadSafetyLock)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
            }

            if (disposing)
            {
                this._handle.Dispose();
            }
        }

        internal void AssertNotDisposed()
        {
            lock (this._disposeThreadSafetyLock)
            {
                if (this._disposed)
                {
                    throw new ObjectDisposedException(this.ToString());
                }
            }
        }

        private readonly object _disposeThreadSafetyLock = new object();
        private bool _disposed;

        #endregion

        #region GetInstance

        /// <summary>
        /// 
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        public CimInstance GetInstance(string namespaceName, CimInstance instanceId)
        {
            return this.GetInstance(namespaceName, instanceId, null);
        }

        public CimInstance GetInstance(string namespaceName, CimInstance instanceId, CimOperationOptions options)
        {
            this.AssertNotDisposed();
            if (instanceId == null)
            {
                throw new ArgumentNullException("instanceId");
            }

            IEnumerable<CimInstance> enumerable = new CimSyncInstanceEnumerable(
                options,
                this.InstanceId,
                this.ComputerName,
                asyncCallbacksReceiver => GetInstanceCore(namespaceName, instanceId, options, asyncCallbacksReceiver));
            return enumerable.Single();
        }

        public CimAsyncResult<CimInstance> GetInstanceAsync(string namespaceName, CimInstance instanceId)
        {
            return this.GetInstanceAsync(namespaceName, instanceId, null);
        }

        public CimAsyncResult<CimInstance> GetInstanceAsync(string namespaceName, CimInstance instanceId, CimOperationOptions options)
        {
            this.AssertNotDisposed();
            if (instanceId == null)
            {
                throw new ArgumentNullException("instanceId");
            }

            IObservable<CimInstance> observable = new CimAsyncInstanceObservable(
                options,
                this.InstanceId,
                this.ComputerName,
                asyncCallbacksReceiver => GetInstanceCore(namespaceName, instanceId, options, asyncCallbacksReceiver));

            return new CimAsyncResult<CimInstance>(observable);
        }

        private Native.OperationHandle GetInstanceCore(
            string namespaceName, 
            CimInstance instanceId, 
            CimOperationOptions options, 
            CimAsyncCallbacksReceiverBase asyncCallbacksReceiver)
        {
            Debug.Assert(instanceId != null, "Caller should verify instanceId != null");

            Native.OperationHandle operationHandle;
            Native.SessionMethods.GetInstance(
                this._handle,
                options.GetOperationFlags(),
                options.GetOperationOptionsHandle(),
                namespaceName,
                instanceId.InstanceHandle,
                options.GetOperationCallbacks(asyncCallbacksReceiver),
                out operationHandle);

            return operationHandle;
        }

        #endregion GetInstance

        #region ModifyInstance

        public CimInstance ModifyInstance(CimInstance instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }
            if (instance.CimSystemProperties.Namespace == null)
            {
                throw new ArgumentNullException(paramName: "instance", message: Strings.CimInstanceNamespaceIsNull);
            }

            return this.ModifyInstance(instance.CimSystemProperties.Namespace, instance);
        }

        public CimInstance ModifyInstance(string namespaceName, CimInstance instance)
        {
            return this.ModifyInstance(namespaceName, instance, null);
        }

        public CimInstance ModifyInstance(string namespaceName, CimInstance instance, CimOperationOptions options)
        {
            this.AssertNotDisposed();
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            IEnumerable<CimInstance> enumerable = new CimSyncInstanceEnumerable(
                options,
                this.InstanceId,
                this.ComputerName,
                asyncCallbacksReceiver => ModifyInstanceCore(namespaceName, instance, options, asyncCallbacksReceiver));

			return enumerable.SingleOrDefault();
        }

        public CimAsyncResult<CimInstance> ModifyInstanceAsync(CimInstance instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }
            if (instance.CimSystemProperties.Namespace == null)
            {
                throw new ArgumentNullException(paramName: "instance", message: Strings.CimInstanceNamespaceIsNull);
            }

            return this.ModifyInstanceAsync(instance.CimSystemProperties.Namespace, instance);
        }

        public CimAsyncResult<CimInstance> ModifyInstanceAsync(string namespaceName, CimInstance instance)
        {
            return this.ModifyInstanceAsync(namespaceName, instance, null);
        }

        public CimAsyncResult<CimInstance> ModifyInstanceAsync(string namespaceName, CimInstance instance, CimOperationOptions options)
        {
            this.AssertNotDisposed();
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            IObservable<CimInstance> observable = new CimAsyncInstanceObservable(
                options,
                this.InstanceId,
                this.ComputerName,
                asyncCallbacksReceiver => ModifyInstanceCore(namespaceName, instance, options, asyncCallbacksReceiver));

            return new CimAsyncResult<CimInstance>(observable);
        }

        private Native.OperationHandle ModifyInstanceCore(
            string namespaceName, 
            CimInstance instance, 
            CimOperationOptions options, 
            CimAsyncCallbacksReceiverBase asyncCallbacksReceiver)
        {
            Debug.Assert(instance!= null, "Caller should verify instance != null");

            Native.OperationHandle operationHandle;
            Native.SessionMethods.ModifyInstance(
                this._handle,
                options.GetOperationFlags(),
                options.GetOperationOptionsHandle(),
                namespaceName,
                instance.InstanceHandle,
                options.GetOperationCallbacks(asyncCallbacksReceiver),
                out operationHandle);

            return operationHandle;
        }

        #endregion

        #region CreateInstance

        public CimInstance CreateInstance(string namespaceName, CimInstance instance)
        {
            return this.CreateInstance(namespaceName, instance, null);
        }

        public CimInstance CreateInstance(string namespaceName, CimInstance instance, CimOperationOptions options)
        {
            this.AssertNotDisposed();
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            IEnumerable<CimInstance> enumerable = new CimSyncInstanceEnumerable(
                options,
                this.InstanceId,
                this.ComputerName,
                asyncCallbacksReceiver => CreateInstanceCore(namespaceName, instance, options, asyncCallbacksReceiver));
            
            return enumerable.SingleOrDefault();
        }

        public CimAsyncResult<CimInstance> CreateInstanceAsync(string namespaceName, CimInstance instance)
        {
            return this.CreateInstanceAsync(namespaceName, instance, null);
        }

        public CimAsyncResult<CimInstance> CreateInstanceAsync(string namespaceName, CimInstance instance, CimOperationOptions options)
        {
            this.AssertNotDisposed();
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            IObservable<CimInstance> observable = new CimAsyncInstanceObservable(
                options,
                this.InstanceId,
                this.ComputerName,
                asyncCallbacksReceiver => CreateInstanceCore(namespaceName, instance, options, asyncCallbacksReceiver));

            return new CimAsyncResult<CimInstance>(observable);
        }

        private Native.OperationHandle CreateInstanceCore(
            string namespaceName, 
            CimInstance instance, 
            CimOperationOptions options, 
            CimAsyncCallbacksReceiverBase asyncCallbacksReceiver)
        {
            Debug.Assert(instance!= null, "Caller should verify instanceId != null");

            Native.OperationHandle operationHandle;
            Native.SessionMethods.CreateInstance(
                this._handle,
                options.GetOperationFlags(),
                options.GetOperationOptionsHandle(),
                namespaceName,
                instance.InstanceHandle,
                options.GetOperationCallbacks(asyncCallbacksReceiver),
                out operationHandle);

            return operationHandle;
        }

        #endregion

        #region DeleteInstance

        public void DeleteInstance(CimInstance instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }
            if (instance.CimSystemProperties.Namespace == null)
            {
                throw new ArgumentNullException(paramName: "instance", message: Strings.CimInstanceNamespaceIsNull);
            }

            this.DeleteInstance(instance.CimSystemProperties.Namespace, instance);
        }

        public void DeleteInstance(string namespaceName, CimInstance instance)
        {
            this.DeleteInstance(namespaceName, instance, null);
        }

        public void DeleteInstance(string namespaceName, CimInstance instance, CimOperationOptions options)
        {
            this.AssertNotDisposed();
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            IEnumerable<CimInstance> enumerable = new CimSyncInstanceEnumerable(
                options,
                this.InstanceId,
                this.ComputerName,
                asyncCallbacksReceiver => DeleteInstanceCore(namespaceName, instance, options, asyncCallbacksReceiver));
            int count = enumerable.Count();
            Debug.Assert(count == 0, "No instances should be returned by ModifyInstance");
        }

        public CimAsyncStatus DeleteInstanceAsync(CimInstance instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }
            if (instance.CimSystemProperties.Namespace == null)
            {
                throw new ArgumentNullException(paramName: "instance", message: Strings.CimInstanceNamespaceIsNull);
            }

            return this.DeleteInstanceAsync(instance.CimSystemProperties.Namespace, instance);
        }

        public CimAsyncStatus DeleteInstanceAsync(string namespaceName, CimInstance instance)
        {
            return this.DeleteInstanceAsync(namespaceName, instance, null);
        }

        public CimAsyncStatus DeleteInstanceAsync(string namespaceName, CimInstance instance, CimOperationOptions options)
        {
            this.AssertNotDisposed();
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            IObservable<CimInstance> observable = new CimAsyncInstanceObservable(
                options,
                this.InstanceId,
                this.ComputerName,
                asyncCallbacksReceiver => DeleteInstanceCore(namespaceName, instance, options, asyncCallbacksReceiver));

            return new CimAsyncStatus(observable);
        }

        private Native.OperationHandle DeleteInstanceCore(
            string namespaceName, 
            CimInstance instance, 
            CimOperationOptions options, 
            CimAsyncCallbacksReceiverBase asyncCallbacksReceiver)
        {
            Debug.Assert(instance!= null, "Caller should verify instance != null");

            Native.OperationHandle operationHandle;
            Native.SessionMethods.DeleteInstance(
                this._handle,
                options.GetOperationFlags(),
                options.GetOperationOptionsHandle(),
                namespaceName,
                instance.InstanceHandle,
                options.GetOperationCallbacks(asyncCallbacksReceiver),
                out operationHandle);

            return operationHandle;
        }

        #endregion

        #region Subscription
        public IEnumerable<CimSubscriptionResult> Subscribe(string namespaceName, string queryDialect, string queryExpression)
        {
            return this.Subscribe(namespaceName, queryDialect, queryExpression, null , null);
        }
        public IEnumerable<CimSubscriptionResult> Subscribe(string namespaceName, string queryDialect, string queryExpression, CimOperationOptions operationOptions)
        {
            return this.Subscribe(namespaceName, queryDialect, queryExpression, operationOptions , null);
        }
        public IEnumerable<CimSubscriptionResult> Subscribe(string namespaceName, string queryDialect, string queryExpression, CimSubscriptionDeliveryOptions options)
        {
            return this.Subscribe(namespaceName, queryDialect, queryExpression, null , options);
        }
        public IEnumerable<CimSubscriptionResult> Subscribe(string namespaceName, string queryDialect, string queryExpression, 
                        CimOperationOptions operationOptions, CimSubscriptionDeliveryOptions options)
        {
            this.AssertNotDisposed();
            if (string.IsNullOrWhiteSpace(queryDialect))
            {
                throw new ArgumentNullException("queryDialect");
            }
            if (string.IsNullOrWhiteSpace(queryExpression))
            {
                throw new ArgumentNullException("queryExpression");
            }

            return new CimSyncIndicationEnumerable(operationOptions,
                asyncCallbacksReceiver => SubscribeCore(namespaceName, queryDialect, queryExpression, operationOptions, options, asyncCallbacksReceiver));

                        
        }
        public CimAsyncMultipleResults<CimSubscriptionResult> SubscribeAsync(string namespaceName, string queryDialect, string queryExpression)
        {
            return this.SubscribeAsync(namespaceName, queryDialect, queryExpression, null, null );
        }
        public CimAsyncMultipleResults<CimSubscriptionResult> SubscribeAsync(string namespaceName, string queryDialect, string queryExpression, 
                                                    CimOperationOptions operationOptions)
        {
            return this.SubscribeAsync(namespaceName, queryDialect, queryExpression, operationOptions, null );
        }
        public CimAsyncMultipleResults<CimSubscriptionResult> SubscribeAsync(string namespaceName, string queryDialect, string queryExpression,
                                                    CimSubscriptionDeliveryOptions options)
        {
            return this.SubscribeAsync(namespaceName, queryDialect, queryExpression, null, options );
        }
        public CimAsyncMultipleResults<CimSubscriptionResult> SubscribeAsync(string namespaceName, string queryDialect, string queryExpression, 
                            CimOperationOptions operationOptions, CimSubscriptionDeliveryOptions options)
        {
            this.AssertNotDisposed();
            if (string.IsNullOrWhiteSpace(queryDialect))
            {
                throw new ArgumentNullException("queryDialect");
            }
            if (string.IsNullOrWhiteSpace(queryExpression))
            {
                throw new ArgumentNullException("queryExpression");
            }

            IObservable<CimSubscriptionResult> observable = new CimAsyncIndicationObservable(
                operationOptions,
                asyncCallbacksReceiver => SubscribeCore(namespaceName, queryDialect, queryExpression, operationOptions, options, asyncCallbacksReceiver));

            return new CimAsyncMultipleResults<CimSubscriptionResult>(observable);
            
        }

            private Native.OperationHandle SubscribeCore(
            string namespaceName, 
            string queryDialect, 
            string queryExpression,
            CimOperationOptions operationOptions, 
            CimSubscriptionDeliveryOptions options, 
            CimAsyncCallbacksReceiverBase asyncCallbacksReceiver)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(queryDialect), "Caller should verify !string.IsNullOrWhiteSpace(queryDialect)");
            Debug.Assert(!string.IsNullOrWhiteSpace(queryExpression), "Caller should verify !string.IsNullOrWhiteSpace(queryExpression)");            

            Native.OperationHandle operationHandle;
            Native.SessionMethods.Subscribe(
                this._handle,
                operationOptions.GetOperationFlags(),
                operationOptions.GetOperationOptionsHandle(),
                namespaceName,
                queryDialect,
                queryExpression,
                options.GetSubscriptionDeliveryOptionsHandle(),
                operationOptions.GetOperationCallbacks(asyncCallbacksReceiver),
                out operationHandle);

            return operationHandle;
        }
        #endregion

        #region EnumerateInstances

        public IEnumerable<CimInstance> EnumerateInstances(string namespaceName, string className)
        {
            return this.EnumerateInstances(namespaceName, className, null);
        }

        public IEnumerable<CimInstance> EnumerateInstances(string namespaceName, string className, CimOperationOptions options)
        {
            this.AssertNotDisposed();

            return new CimSyncInstanceEnumerable(
                options,
                this.InstanceId,
                this.ComputerName,
                asyncCallbacksReceiver => EnumerateInstancesCore(namespaceName, className, options, asyncCallbacksReceiver));
        }

        public CimAsyncMultipleResults<CimInstance> EnumerateInstancesAsync(string namespaceName, string className)
        {
            return this.EnumerateInstancesAsync(namespaceName, className, null);
        }

        public CimAsyncMultipleResults<CimInstance> EnumerateInstancesAsync(string namespaceName, string className, CimOperationOptions options)
        {
            this.AssertNotDisposed();

            IObservable<CimInstance> observable = new CimAsyncInstanceObservable(
                options,
                this.InstanceId,
                this.ComputerName,
                asyncCallbacksReceiver => EnumerateInstancesCore(namespaceName, className, options, asyncCallbacksReceiver));

            return new CimAsyncMultipleResults<CimInstance>(observable);
        }

        private Native.OperationHandle EnumerateInstancesCore(
            string namespaceName, 
            string className, 
            CimOperationOptions options, 
            CimAsyncCallbacksReceiverBase asyncCallbacksReceiver)
        {

            Native.OperationHandle operationHandle;
            Native.SessionMethods.EnumerateInstances(
                this._handle,
                options.GetOperationFlags(),
                options.GetOperationOptionsHandle(),
                namespaceName,
                className,
                options.GetKeysOnly(),
                options.GetOperationCallbacks(asyncCallbacksReceiver),
                out operationHandle);

            return operationHandle;
        }

        #endregion

        #region QueryInstances

        public IEnumerable<CimInstance> QueryInstances(string namespaceName, string queryDialect, string queryExpression)
        {
            return this.QueryInstances(namespaceName, queryDialect, queryExpression, null);
        }

        public IEnumerable<CimInstance> QueryInstances(string namespaceName, string queryDialect, string queryExpression, CimOperationOptions options)
        {
            this.AssertNotDisposed();
            if (string.IsNullOrWhiteSpace(queryDialect))
            {
                throw new ArgumentNullException("queryDialect");
            }
            if (string.IsNullOrWhiteSpace(queryExpression))
            {
                throw new ArgumentNullException("queryExpression");
            }

            return new CimSyncInstanceEnumerable(
                options,
                this.InstanceId,
                this.ComputerName,
                asyncCallbacksReceiver => QueryInstancesCore(namespaceName, queryDialect, queryExpression, options, asyncCallbacksReceiver));
        }

        public CimAsyncMultipleResults<CimInstance> QueryInstancesAsync(string namespaceName, string queryDialect, string queryExpression)
        {
            return this.QueryInstancesAsync(namespaceName, queryDialect, queryExpression, null);
        }

        public CimAsyncMultipleResults<CimInstance> QueryInstancesAsync(string namespaceName, string queryDialect, string queryExpression, CimOperationOptions options)
        {
            this.AssertNotDisposed();
            if (string.IsNullOrWhiteSpace(queryDialect))
            {
                throw new ArgumentNullException("queryDialect");
            }
            if (string.IsNullOrWhiteSpace(queryExpression))
            {
                throw new ArgumentNullException("queryExpression");
            }

            IObservable<CimInstance> observable = new CimAsyncInstanceObservable(
                options,
                this.InstanceId,
                this.ComputerName,
                asyncCallbacksReceiver => QueryInstancesCore(namespaceName, queryDialect, queryExpression, options, asyncCallbacksReceiver));

            return new CimAsyncMultipleResults<CimInstance>(observable);
        }

        private Native.OperationHandle QueryInstancesCore(
            string namespaceName, 
            string queryDialect, 
            string queryExpression, 
            CimOperationOptions options, 
            CimAsyncCallbacksReceiverBase asyncCallbacksReceiver)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(queryDialect), "Caller should verify !string.IsNullOrWhiteSpace(queryDialect)");
            Debug.Assert(!string.IsNullOrWhiteSpace(queryExpression), "Caller should verify !string.IsNullOrWhiteSpace(queryExpression)");

            Native.OperationHandle operationHandle;
            Native.SessionMethods.QueryInstances(
                this._handle,
                options.GetOperationFlags(),
                options.GetOperationOptionsHandle(),
                namespaceName,
                queryDialect,
                queryExpression,
                options.GetKeysOnly(),
                options.GetOperationCallbacks(asyncCallbacksReceiver),
                out operationHandle);

            return operationHandle;
        }

        #endregion

        #region EnumerateAssociatedInstances (aka AssociatorInstances)

        // REVIEW PLEASE: This is a different name from the API straw-man
        // Need to use a different name (from AssociatorInstances) to follow .NET guidelines: 
        // 1) method names start with verb, 
        // 2) names pass spell checker / google test)
        // So far the name has been reviewed by Wojtek.

        public IEnumerable<CimInstance> EnumerateAssociatedInstances(
            string namespaceName, 
            CimInstance sourceInstance,
            string associationClassName,
            string resultClassName,
            string sourceRole,
            string resultRole)
        {
            return this.EnumerateAssociatedInstances(namespaceName, sourceInstance, associationClassName, resultClassName, sourceRole, resultRole, null);
        }

        public IEnumerable<CimInstance> EnumerateAssociatedInstances(
            string namespaceName, 
            CimInstance sourceInstance,
            string associationClassName,
            string resultClassName,
            string sourceRole,
            string resultRole,
            CimOperationOptions options)
        {
            this.AssertNotDisposed();
            if (sourceInstance == null)
            {
                throw new ArgumentNullException("sourceInstance");
            }
            return new CimSyncInstanceEnumerable(
                options,
                this.InstanceId,
                this.ComputerName,
                asyncCallbacksReceiver => EnumerateAssociatedInstancesCore(namespaceName, sourceInstance, associationClassName, resultClassName, sourceRole, resultRole, options, asyncCallbacksReceiver));
        }

        public CimAsyncMultipleResults<CimInstance> EnumerateAssociatedInstancesAsync(
            string namespaceName,
            CimInstance sourceInstance,
            string associationClassName,
            string resultClassName,
            string sourceRole,
            string resultRole)
        {
            return this.EnumerateAssociatedInstancesAsync(namespaceName, sourceInstance, associationClassName, resultClassName, sourceRole, resultRole, null);
        }

        public CimAsyncMultipleResults<CimInstance> EnumerateAssociatedInstancesAsync(
            string namespaceName, 
            CimInstance sourceInstance,
            string associationClassName,
            string resultClassName,
            string sourceRole,
            string resultRole,
            CimOperationOptions options)
        {
            this.AssertNotDisposed();
            if (sourceInstance == null)
            {
                throw new ArgumentNullException("sourceInstance");
            }

            IObservable<CimInstance> observable = new CimAsyncInstanceObservable(
                options,
                this.InstanceId,
                this.ComputerName,
                asyncCallbacksReceiver => EnumerateAssociatedInstancesCore(namespaceName, sourceInstance, associationClassName, resultClassName, sourceRole, resultRole, options, asyncCallbacksReceiver));

            return new CimAsyncMultipleResults<CimInstance>(observable);
        }

        private Native.OperationHandle EnumerateAssociatedInstancesCore(
            string namespaceName, 
            CimInstance sourceInstance,
            string associationClassName,
            string resultClassName,
            string sourceRole,
            string resultRole,
            CimOperationOptions options, 
            CimAsyncCallbacksReceiverBase asyncCallbacksReceiver)
        {
            Debug.Assert(sourceInstance != null, "Caller should verify sourceInstance != null");

            Native.OperationHandle operationHandle;
            Native.SessionMethods.AssociatorInstances(
                this._handle,
                options.GetOperationFlags(),
                options.GetOperationOptionsHandle(),
                namespaceName,
                sourceInstance.InstanceHandle,
                associationClassName,
                resultClassName,
                sourceRole,
                resultRole,
                options.GetKeysOnly(),
                options.GetOperationCallbacks(asyncCallbacksReceiver),
                out operationHandle);

            return operationHandle;
        }

        #endregion

        #region EnumerateReferencingInstances (aka ReferenceInstances)

        // REVIEW PLEASE: This is a different name from the API straw-man
        // Need to use a different name (from ReferenceInstances) to follow .NET guidelines: 
        // 1) method names start with verb, 
        // 2) names pass spell checker / google test)
        // So far the name has been reviewed by Wojtek.

        public IEnumerable<CimInstance> EnumerateReferencingInstances(
            string namespaceName, 
            CimInstance sourceInstance,
            string associationClassName,
            string sourceRole)
        {
            return this.EnumerateReferencingInstances(namespaceName, sourceInstance, associationClassName, sourceRole, null);
        }

        public IEnumerable<CimInstance> EnumerateReferencingInstances(
            string namespaceName, 
            CimInstance sourceInstance,
            string associationClassName,
            string sourceRole,
            CimOperationOptions options)
        {
            this.AssertNotDisposed();
            if (sourceInstance == null)
            {
                throw new ArgumentNullException("sourceInstance");
            }

            return new CimSyncInstanceEnumerable(
                options,
                this.InstanceId,
                this.ComputerName,
                asyncCallbacksReceiver => EnumerateReferencingInstancesCore(namespaceName, sourceInstance, associationClassName, sourceRole, options, asyncCallbacksReceiver));
        }

        public CimAsyncMultipleResults<CimInstance> EnumerateReferencingInstancesAsync(
            string namespaceName, 
            CimInstance sourceInstance,
            string associationClassName,
            string sourceRole)
        {
            return this.EnumerateReferencingInstancesAsync(namespaceName, sourceInstance, associationClassName, sourceRole, null);
        }

        public CimAsyncMultipleResults<CimInstance> EnumerateReferencingInstancesAsync(
            string namespaceName, 
            CimInstance sourceInstance,
            string associationClassName,
            string sourceRole,
            CimOperationOptions options)
        {
            this.AssertNotDisposed();
            if (sourceInstance == null)
            {
                throw new ArgumentNullException("sourceInstance");
            }

            IObservable<CimInstance> observable = new CimAsyncInstanceObservable(
                options,
                this.InstanceId,
                this.ComputerName,
                asyncCallbacksReceiver => EnumerateReferencingInstancesCore(namespaceName, sourceInstance, associationClassName, sourceRole, options, asyncCallbacksReceiver));

            return new CimAsyncMultipleResults<CimInstance>(observable);
        }

        private Native.OperationHandle EnumerateReferencingInstancesCore(
            string namespaceName, 
            CimInstance sourceInstance,
            string associationClassName,
            string sourceRole,
            CimOperationOptions options, 
            CimAsyncCallbacksReceiverBase asyncCallbacksReceiver)
        {
            Debug.Assert(sourceInstance != null, "Caller should verify sourceInstance != null");

            Native.OperationHandle operationHandle;
            Native.SessionMethods.ReferenceInstances(
                this._handle,
                options.GetOperationFlags(),
                options.GetOperationOptionsHandle(),
                namespaceName,
                sourceInstance.InstanceHandle,
                associationClassName,
                sourceRole,
                options.GetKeysOnly(),
                options.GetOperationCallbacks(asyncCallbacksReceiver),
                out operationHandle);

            return operationHandle;
        }

        #endregion

        #region InvokeMethod

        #region InvokeMethod - instance methods

        public CimMethodResult InvokeMethod(
            CimInstance instance,
            string methodName,
            CimMethodParametersCollection methodParameters)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }
            if (instance.CimSystemProperties.Namespace == null)
            {
                throw new ArgumentNullException(paramName: "instance", message: Strings.CimInstanceNamespaceIsNull);
            }

            return this.InvokeMethod(instance.CimSystemProperties.Namespace, instance, methodName, methodParameters);
        }

        /// <summary>
        /// Invokes an instance method
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="instance"></param>
        /// <param name="methodName"></param>
        /// <param name="methodParameters"></param>
        /// <returns></returns>
        public CimMethodResult InvokeMethod(
            string namespaceName,
            CimInstance instance,
            string methodName,
            CimMethodParametersCollection methodParameters)
        {
            return this.InvokeMethod(namespaceName, instance, methodName, methodParameters, null);
        }

        /// <summary>
        /// Invokes an instance method with a given set of options
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="instance"></param>
        /// <param name="methodName"></param>
        /// <param name="methodParameters"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public CimMethodResult InvokeMethod(
            string namespaceName,
            CimInstance instance,
            string methodName,
            CimMethodParametersCollection methodParameters,
            CimOperationOptions options)
        {
            this.AssertNotDisposed();
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }
            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentNullException("methodName");
            }

            IEnumerable<CimInstance> sequenceOfInstances = new CimSyncInstanceEnumerable(
                options,
                this.InstanceId,
                this.ComputerName,
                asyncCallbacksReceiver => InvokeMethodCore(namespaceName, instance.CimSystemProperties.ClassName, instance, methodName, methodParameters, options, asyncCallbacksReceiver));

            CimInstance resultAsCimInstance = sequenceOfInstances.SingleOrDefault();
            if (resultAsCimInstance != null)
            {
                return new CimMethodResult(resultAsCimInstance);
            }
            return null;
        }

        public CimAsyncResult<CimMethodResult> InvokeMethodAsync(
            CimInstance instance,
            string methodName,
            CimMethodParametersCollection methodParameters)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }
            if (instance.CimSystemProperties.Namespace == null)
            {
                throw new ArgumentNullException(paramName: "instance", message: Strings.CimInstanceNamespaceIsNull);
            }

            return this.InvokeMethodAsync(instance.CimSystemProperties.Namespace, instance, methodName, methodParameters);
        }

        /// <summary>
        /// Invokes an instance method
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="instance"></param>
        /// <param name="methodName"></param>
        /// <param name="methodParameters"></param>
        /// <returns></returns>
        public CimAsyncResult<CimMethodResult> InvokeMethodAsync(
            string namespaceName,
            CimInstance instance,
            string methodName,
            CimMethodParametersCollection methodParameters)
        {
            IObservable<CimMethodResultBase> baseObservable = this.InvokeMethodAsync(namespaceName, instance, methodName, methodParameters, null);
            IObservable<CimMethodResult> targetObservable = new ConvertingObservable<CimMethodResultBase, CimMethodResult>(baseObservable);
            return new CimAsyncResult<CimMethodResult>(targetObservable);
        }

        /// <summary>
        /// Invokes an instance method with a given set of options
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="instance"></param>
        /// <param name="methodName"></param>
        /// <param name="methodParameters"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public CimAsyncMultipleResults<CimMethodResultBase> InvokeMethodAsync(
            string namespaceName,
            CimInstance instance,
            string methodName,
            CimMethodParametersCollection methodParameters,
            CimOperationOptions options)
        {
            this.AssertNotDisposed();
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }
            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentNullException("methodName");
            }

            IObservable<CimMethodResultBase> observable = new CimAsyncMethodResultObservable(
                options,
                this.InstanceId,
                this.ComputerName,
                asyncCallbacksReceiver => InvokeMethodCore(namespaceName, instance.CimSystemProperties.ClassName, instance, methodName, methodParameters, options, asyncCallbacksReceiver));

            return new CimAsyncMultipleResults<CimMethodResultBase>(observable);
        }

        #endregion

        #region InvokeMethod - static methods

        /// <summary>
        /// Invokes a static method
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <param name="methodParameters"></param>
        /// <returns></returns>
        public CimMethodResult InvokeMethod(
            string namespaceName,
            string className,
            string methodName,
            CimMethodParametersCollection methodParameters)
        {
            return this.InvokeMethod(namespaceName, className, methodName, methodParameters, null);
        }

        /// <summary>
        /// Invokes a static method with a given set of options
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <param name="methodParameters"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public CimMethodResult InvokeMethod(
            string namespaceName,
            string className,
            string methodName,
            CimMethodParametersCollection methodParameters,
            CimOperationOptions options)
        {
            this.AssertNotDisposed();
            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentNullException("methodName");
            }

            IEnumerable<CimInstance> sequenceOfInstances = new CimSyncInstanceEnumerable(
                options,
                this.InstanceId,
                this.ComputerName,
                asyncCallbacksReceiver => InvokeMethodCore(namespaceName, className, null /* instance */, methodName, methodParameters, options, asyncCallbacksReceiver));

            CimInstance resultAsCimInstance = sequenceOfInstances.SingleOrDefault();
            if (resultAsCimInstance != null)
            {
                return new CimMethodResult(resultAsCimInstance);
            }
            return null;
        }

        /// <summary>
        /// Invokes a static method
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <param name="methodParameters"></param>
        /// <returns></returns>
        public CimAsyncResult<CimMethodResult> InvokeMethodAsync(
            string namespaceName,
            string className,
            string methodName,
            CimMethodParametersCollection methodParameters)
        {
            IObservable<CimMethodResultBase> baseObservable = this.InvokeMethodAsync(namespaceName, className, methodName, methodParameters, null);
            IObservable<CimMethodResult> targetObservable = new ConvertingObservable<CimMethodResultBase, CimMethodResult>(baseObservable);
            return new CimAsyncResult<CimMethodResult>(targetObservable);
        }

        /// <summary>
        /// Invokes a static method with a given set of options
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <param name="methodParameters"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public CimAsyncMultipleResults<CimMethodResultBase> InvokeMethodAsync(
            string namespaceName,
            string className,
            string methodName,
            CimMethodParametersCollection methodParameters,
            CimOperationOptions options)
        {
            this.AssertNotDisposed();
            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentNullException("methodName");
            }

            IObservable<CimMethodResultBase> observable = new CimAsyncMethodResultObservable(
                options,
                this.InstanceId,
                this.ComputerName,
                asyncCallbacksReceiver => InvokeMethodCore(namespaceName, className, null /* instance */, methodName, methodParameters, options, asyncCallbacksReceiver));

            return new CimAsyncMultipleResults<CimMethodResultBase>(observable);
        }

        #endregion

        private Native.OperationHandle InvokeMethodCore(
            string namespaceName,
            string className,
            CimInstance instance,
            string methodName,
            CimMethodParametersCollection methodParameters,
            CimOperationOptions options,
            CimAsyncCallbacksReceiverBase asyncCallbacksReceiver)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(namespaceName), "Caller should verify !string.IsNullOrWhiteSpace(namespaceName)");
            Debug.Assert(!string.IsNullOrWhiteSpace(methodName), "Caller should verify !string.IsNullOrWhiteSpace(methodName)");

            Native.OperationHandle operationHandle;
            Native.SessionMethods.Invoke(
                this._handle,
                options.GetOperationFlags(),
                options.GetOperationOptionsHandle(),
                namespaceName,
                className,
                methodName,
                instance != null ? instance.InstanceHandle : null,
                methodParameters != null ? methodParameters.InstanceHandleForMethodInvocation : null,
                options.GetOperationCallbacks(asyncCallbacksReceiver),
                out operationHandle);

            return operationHandle;
        }

        #endregion

        #region GetClass

        /// <summary>
        /// 
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="className"></param>
        /// <returns></returns>
        public CimClass GetClass(string namespaceName, string className)
        {
            return this.GetClass(namespaceName, className, null);
        }

        public CimClass GetClass(string namespaceName, string className, CimOperationOptions options)
        {
            this.AssertNotDisposed();

            IEnumerable<CimClass> enumerable = new CimSyncClassEnumerable(
                options,
                asyncCallbacksReceiver => GetClassCore(namespaceName, className, options, asyncCallbacksReceiver));
            return enumerable.Single();
        }

        public CimAsyncResult<CimClass> GetClassAsync(string namespaceName, string className)
        {
            return this.GetClassAsync(namespaceName, className, null);
        }

        public CimAsyncResult<CimClass> GetClassAsync(string namespaceName, string className, CimOperationOptions options)
        {
            this.AssertNotDisposed();

            IObservable<CimClass> observable = new CimAsyncClassObservable(
                options,
                asyncCallbacksReceiver => GetClassCore(namespaceName, className, options, asyncCallbacksReceiver));

            return new CimAsyncResult<CimClass>(observable);
        }

        private Native.OperationHandle GetClassCore(
            string namespaceName,
            string className,
            CimOperationOptions options,
            CimAsyncCallbacksReceiverBase asyncCallbacksReceiver)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(className), "Caller should verify !string.IsNullOrWhiteSpace(className)");

            Native.OperationHandle operationHandle;
            Native.SessionMethods.GetClass(
                this._handle,
                options.GetOperationFlags(),
                options.GetOperationOptionsHandle(),
                namespaceName,
                className,
                options.GetOperationCallbacks(asyncCallbacksReceiver),
                out operationHandle);

            return operationHandle;
        }

        #endregion GetClass

        #region EnumerateClasses

        public IEnumerable<CimClass> EnumerateClasses(string namespaceName )
        {
            return this.EnumerateClasses(namespaceName, null, null);
        }

        public IEnumerable<CimClass> EnumerateClasses(string namespaceName, string className)
        {
            return this.EnumerateClasses(namespaceName, className, null);
        }

        public IEnumerable<CimClass> EnumerateClasses(string namespaceName, string className, CimOperationOptions options)
        {
            this.AssertNotDisposed();
            return new CimSyncClassEnumerable(
                options,
                asyncCallbacksReceiver => EnumerateClassesCore(namespaceName, className, options, asyncCallbacksReceiver));
        }

        public CimAsyncMultipleResults<CimClass> EnumerateClassesAsync(string namespaceName)
        {
            return this.EnumerateClassesAsync(namespaceName, null, null);
        }

        public CimAsyncMultipleResults<CimClass> EnumerateClassesAsync(string namespaceName, string className)
        {
            return this.EnumerateClassesAsync(namespaceName, className, null);
        }

        public CimAsyncMultipleResults<CimClass> EnumerateClassesAsync(string namespaceName, string className, CimOperationOptions options)
        {
            this.AssertNotDisposed();

            IObservable<CimClass> observable = new CimAsyncClassObservable(
                options,
                asyncCallbacksReceiver => EnumerateClassesCore(namespaceName, className, options, asyncCallbacksReceiver));

            return new CimAsyncMultipleResults<CimClass>(observable);
        }

        private Native.OperationHandle EnumerateClassesCore(
            string namespaceName,
            string className,
            CimOperationOptions options,
            CimAsyncCallbacksReceiverBase asyncCallbacksReceiver)
        {

            Native.OperationHandle operationHandle;
            Native.SessionMethods.EnumerateClasses(
                this._handle,
                options.GetOperationFlags(),
                options.GetOperationOptionsHandle(),
                namespaceName,
                className,
                options.GetClassNamesOnly(),
                options.GetOperationCallbacks(asyncCallbacksReceiver),
                out operationHandle);
            return operationHandle;
        }

        #endregion EnumerateClasses

	#region GetClass

        /// <summary>
        /// Tests whether the session can establish a successful connection to the CIM server
        /// </summary>
        /// <returns>
        /// <c>true</c> if the connection succeeded; <c>false</c> otherwise
        /// </returns>
        public  bool TestConnection()
        {
            CimInstance instance;
            CimException exception;
            return this.TestConnection(out instance, out exception);
        }

        /// <summary>
        /// Tests whether the session can establish a successful connection to the CIM server
        /// </summary>
        /// <param name="exception">Error details if the connection attempt fails</param>
        /// <param name="instance">Test data from CIM server (usually identifying server and OS version) if the connection attempt succeeds</param>
        /// <returns>
        /// <c>true</c> if the connection succeeded; <c>false</c> otherwise
        /// </returns>
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId="0#")]
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId="1#")]
        public bool TestConnection(out CimInstance instance, out CimException exception)
        {
            this.AssertNotDisposed();

            bool bGotSuccess = true;
            instance = null;
            exception = null;
            IEnumerable<CimInstance> enumerable = new CimSyncInstanceEnumerable(
                null,
                this.InstanceId,
                this.ComputerName,
                asyncCallbacksReceiver => TestConnectionCore(null, asyncCallbacksReceiver));
            try
            {
                instance = enumerable.SingleOrDefault();
            }
            catch (CimException e)
            {
                exception = e;
                bGotSuccess = false;
            }
            return bGotSuccess;
        }           

        public CimAsyncResult<CimInstance> TestConnectionAsync()
        {
            this.AssertNotDisposed();

            IObservable<CimInstance> observable = new CimAsyncInstanceObservable(
                null,
                this.InstanceId,
                this.ComputerName,
                asyncCallbacksReceiver => TestConnectionCore(null, asyncCallbacksReceiver));

            return new CimAsyncResult<CimInstance>(observable);

        }    

        private Native.OperationHandle TestConnectionCore(
            CimOperationOptions options,
            CimAsyncCallbacksReceiverBase asyncCallbacksReceiver)
        {
            
            Native.OperationHandle operationHandle;
            Native.SessionMethods.TestConnection(
                this._handle,
                options.GetOperationFlags(),
                options.GetOperationCallbacks(asyncCallbacksReceiver),
                out operationHandle);

            return operationHandle;
        }

        #endregion GetInstance        

        public override string ToString()
        {
            string toStringValue = string.Format(
                CultureInfo.InvariantCulture,
                Strings.CimSessionToString,
                this.ComputerName ?? ".");
            return toStringValue;
        }
    }
}
