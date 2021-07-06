// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System;
using System.Globalization;
using System.Management.Automation;
using System.Threading;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    /// <summary>
    /// <para>
    /// Subscription result event args
    /// </para>
    /// </summary>
    internal abstract class CimSubscriptionEventArgs : EventArgs
    {
        /// <summary>
        /// <para>
        /// Returns an Object value for an operation context
        /// </para>
        /// </summary>
        public object Context
        {
            get
            {
                return context;
            }
        }

        protected object context;
    }

    /// <summary>
    /// <para>
    /// Subscription result event args
    /// </para>
    /// </summary>
    internal class CimSubscriptionResultEventArgs : CimSubscriptionEventArgs
    {
        /// <summary>
        /// <para>
        /// subscription result
        /// </para>
        /// </summary>
        public CimSubscriptionResult Result { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CimSubscriptionResultEventArgs"/> class.
        /// </summary>
        /// <param name="theResult"></param>
        public CimSubscriptionResultEventArgs(
            CimSubscriptionResult theResult)
        {
            this.context = null;
            this.Result = theResult;
        }
    }

    /// <summary>
    /// <para>
    /// Subscription result event args
    /// </para>
    /// </summary>
    internal class CimSubscriptionExceptionEventArgs : CimSubscriptionEventArgs
    {
        /// <summary>
        /// <para>
        /// subscription result
        /// </para>
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CimSubscriptionExceptionEventArgs"/> class.
        /// </summary>
        /// <param name="theResult"></param>
        public CimSubscriptionExceptionEventArgs(
            Exception theException)
        {
            this.context = null;
            this.Exception = theException;
        }
    }

    /// <summary>
    /// <para>
    /// Implements operations of register-cimindication cmdlet.
    /// </para>
    /// </summary>
    internal sealed class CimRegisterCimIndication : CimAsyncOperation
    {
        /// <summary>
        /// <para>
        /// New subscription result event
        /// </para>
        /// </summary>
        public event EventHandler<CimSubscriptionEventArgs> OnNewSubscriptionResult;

        /// <summary>
        /// Initializes a new instance of the <see cref="CimRegisterCimIndication"/> class.
        /// </summary>
        public CimRegisterCimIndication()
            : base()
        {
            this.ackedEvent = new ManualResetEventSlim(false);
        }

        /// <summary>
        /// Start an indication subscription target to the given computer.
        /// </summary>
        /// <param name="computerName">Null stands for localhost.</param>
        /// <param name="nameSpace"></param>
        /// <param name="queryDialect"></param>
        /// <param name="queryExpression"></param>
        /// <param name="operationTimeout"></param>
        public void RegisterCimIndication(
            string computerName,
            string nameSpace,
            string queryDialect,
            string queryExpression,
            UInt32 operationTimeout)
        {
            DebugHelper.WriteLogEx("queryDialect = '{0}'; queryExpression = '{1}'", 0, queryDialect, queryExpression);
            this.TargetComputerName = computerName;
            CimSessionProxy proxy = CreateSessionProxy(computerName, operationTimeout);
            proxy.SubscribeAsync(nameSpace, queryDialect, queryExpression);
            WaitForAckMessage();
        }

        /// <summary>
        /// Start an indication subscription through a given <see cref="CimSession"/>.
        /// </summary>
        /// <param name="cimSession">Cannot be null.</param>
        /// <param name="nameSpace"></param>
        /// <param name="queryDialect"></param>
        /// <param name="queryExpression"></param>
        /// <param name="operationTimeout"></param>
        /// <exception cref="ArgumentNullException">Throw if cimSession is null.</exception>
        public void RegisterCimIndication(
            CimSession cimSession,
            string nameSpace,
            string queryDialect,
            string queryExpression,
            UInt32 operationTimeout)
        {
            DebugHelper.WriteLogEx("queryDialect = '{0}'; queryExpression = '{1}'", 0, queryDialect, queryExpression);
            if (cimSession == null)
            {
                throw new ArgumentNullException(string.Format(CultureInfo.CurrentUICulture, CimCmdletStrings.NullArgument, nameof(cimSession)));
            }

            this.TargetComputerName = cimSession.ComputerName;
            CimSessionProxy proxy = CreateSessionProxy(cimSession, operationTimeout);
            proxy.SubscribeAsync(nameSpace, queryDialect, queryExpression);
            WaitForAckMessage();
        }

        #region override methods

        /// <summary>
        /// <para>
        /// Subscribe to the events issued by <see cref="CimSessionProxy"/>.
        /// </para>
        /// </summary>
        /// <param name="proxy"></param>
        protected override void SubscribeToCimSessionProxyEvent(CimSessionProxy proxy)
        {
            DebugHelper.WriteLog("SubscribeToCimSessionProxyEvent", 4);
            // Raise event instead of write object to ps
            proxy.OnNewCmdletAction += this.CimIndicationHandler;
            proxy.OnOperationCreated += this.OperationCreatedHandler;
            proxy.OnOperationDeleted += this.OperationDeletedHandler;
            proxy.EnableMethodResultStreaming = false;
        }

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
        private void CimIndicationHandler(object cimSession, CmdletActionEventArgs actionArgs)
        {
            DebugHelper.WriteLogEx("action is {0}. Disposed {1}", 0, actionArgs.Action, this.Disposed);

            if (this.Disposed)
            {
                return;
            }

            // NOTES: should move after this.Disposed, but need to log the exception
            CimWriteError cimWriteError = actionArgs.Action as CimWriteError;
            if (cimWriteError != null)
            {
                this.Exception = cimWriteError.Exception;
                if (!this.ackedEvent.IsSet)
                {
                    // an exception happened
                    DebugHelper.WriteLogEx("an exception happened", 0);
                    this.ackedEvent.Set();
                    return;
                }

                EventHandler<CimSubscriptionEventArgs> temp = this.OnNewSubscriptionResult;
                if (temp != null)
                {
                    DebugHelper.WriteLog("Raise an exception event", 2);

                    temp(this, new CimSubscriptionExceptionEventArgs(this.Exception));
                }

                DebugHelper.WriteLog("Got an exception: {0}", 2, Exception);
            }

            CimWriteResultObject cimWriteResultObject = actionArgs.Action as CimWriteResultObject;
            if (cimWriteResultObject != null)
            {
                CimSubscriptionResult result = cimWriteResultObject.Result as CimSubscriptionResult;
                if (result != null)
                {
                    EventHandler<CimSubscriptionEventArgs> temp = this.OnNewSubscriptionResult;
                    if (temp != null)
                    {
                        DebugHelper.WriteLog("Raise an result event", 2);
                        temp(this, new CimSubscriptionResultEventArgs(result));
                    }
                }
                else
                {
                    if (!this.ackedEvent.IsSet)
                    {
                        // an ACK message returned
                        DebugHelper.WriteLogEx("an ack message happened", 0);
                        this.ackedEvent.Set();
                        return;
                    }
                    else
                    {
                        DebugHelper.WriteLogEx("an ack message should not happen here", 0);
                    }
                }
            }
        }

        /// <summary>
        /// Block the ps thread until ACK message or Error happened.
        /// </summary>
        private void WaitForAckMessage()
        {
            DebugHelper.WriteLogEx();
            this.ackedEvent.Wait();
            if (this.Exception != null)
            {
                DebugHelper.WriteLogEx("error happened", 0);
                if (this.Cmdlet != null)
                {
                    DebugHelper.WriteLogEx("Throw Terminating error", 1);

                    // throw terminating error
                    ErrorRecord errorRecord = ErrorToErrorRecord.ErrorRecordFromAnyException(
                        new InvocationContext(this.TargetComputerName, null), this.Exception, null);
                    this.Cmdlet.ThrowTerminatingError(errorRecord);
                }
                else
                {
                    DebugHelper.WriteLogEx("Throw exception", 1);
                    // throw exception out
                    throw this.Exception;
                }
            }

            DebugHelper.WriteLogEx("ACK happened", 0);
        }
        #endregion

        #region internal property
        /// <summary>
        /// The cmdlet object who issue this subscription,
        /// to throw ThrowTerminatingError
        /// in case there is a subscription failure.
        /// </summary>
        /// <param name="cmdlet"></param>
        internal Cmdlet Cmdlet
        {
            get;
            set;
        }

        /// <summary>
        /// Target computername.
        /// </summary>
        internal string TargetComputerName
        {
            get;
            set;
        }

        #endregion

        #region private methods
        /// <summary>
        /// <para>
        /// Create <see cref="CimSessionProxy"/> and set properties
        /// </para>
        /// </summary>
        /// <param name="computerName"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        private CimSessionProxy CreateSessionProxy(
            string computerName,
            UInt32 timeout)
        {
            CimSessionProxy proxy = CreateCimSessionProxy(computerName);
            proxy.OperationTimeout = timeout;
            return proxy;
        }

        /// <summary>
        /// Create <see cref="CimSessionProxy"/> and set properties.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        private CimSessionProxy CreateSessionProxy(
            CimSession session,
            UInt32 timeout)
        {
            CimSessionProxy proxy = CreateCimSessionProxy(session);
            proxy.OperationTimeout = timeout;
            return proxy;
        }
        #endregion

        #region private members

        /// <summary>
        /// Exception occurred while start the subscription.
        /// </summary>
        internal Exception Exception { get; private set; }

        #endregion

    }
}
