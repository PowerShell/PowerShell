// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System;
using System.Globalization;
using System.Management.Automation;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    #region AsyncResultType
    /// <summary>
    /// <para>
    /// Async result type
    /// </para>
    /// </summary>
    public enum AsyncResultType
    {
        Result,
        Exception,
        Completion
    }
    #endregion

    #region CimResultContext
    /// <summary>
    /// Cim Result Context.
    /// </summary>
    internal class CimResultContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CimResultContext"/> class.
        /// </summary>
        /// <param name="ErrorSource"></param>
        internal CimResultContext(object ErrorSource)
        {
            this.ErrorSource = ErrorSource;
        }

        /// <summary>
        /// ErrorSource property.
        /// </summary>
        internal object ErrorSource { get; }
    }
    #endregion

    #region AsyncResultEventArgsBase
    /// <summary>
    /// <para>
    /// Base class of async result event argument
    /// </para>
    /// </summary>
    internal abstract class AsyncResultEventArgsBase : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncResultEventArgsBase"/> class.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="observable"></param>
        /// <param name="resultType"></param>
        protected AsyncResultEventArgsBase(
            CimSession session,
            IObservable<object> observable,
            AsyncResultType resultType)
        {
            this.session = session;
            this.observable = observable;
            this.resultType = resultType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncResultEventArgsBase"/> class.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="observable"></param>
        /// <param name="resultType"></param>
        /// <param name="context"></param>
        protected AsyncResultEventArgsBase(
            CimSession session,
            IObservable<object> observable,
            AsyncResultType resultType,
            CimResultContext cimResultContext)
        {
            this.session = session;
            this.observable = observable;
            this.resultType = resultType;
            this.context = cimResultContext;
        }

        public readonly CimSession session;
        public readonly IObservable<object> observable;
        public readonly AsyncResultType resultType;

        // property ErrorSource
        public readonly CimResultContext context;
    }

    #endregion

    #region AsyncResult*Args
    /// <summary>
    /// <para>
    /// operation successfully completed event argument
    /// </para>
    /// </summary>
    internal class AsyncResultCompleteEventArgs : AsyncResultEventArgsBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncResultCompleteEventArgs"/> class.
        /// </summary>
        /// <param name="session"><see cref="CimSession"/> object.</param>
        /// <param name="cancellationDisposable"></param>
        public AsyncResultCompleteEventArgs(
            CimSession session,
            IObservable<object> observable)
            : base(session, observable, AsyncResultType.Completion)
        {
        }
    }

    /// <summary>
    /// <para>
    /// async result argument with object
    /// </para>
    /// </summary>
    internal class AsyncResultObjectEventArgs : AsyncResultEventArgsBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncResultObjectEventArgs"/> class.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="observable"></param>
        /// <param name="resultObject"></param>
        public AsyncResultObjectEventArgs(
            CimSession session,
            IObservable<object> observable,
            object resultObject)
            : base(session, observable, AsyncResultType.Result)
        {
            this.resultObject = resultObject;
        }

        public readonly object resultObject;
    }

    /// <summary>
    /// <para>
    /// operation completed with exception event argument
    /// </para>
    /// </summary>
    internal class AsyncResultErrorEventArgs : AsyncResultEventArgsBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncResultErrorEventArgs"/> class.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="observable"></param>
        /// <param name="error"></param>
        public AsyncResultErrorEventArgs(
            CimSession session,
            IObservable<object> observable,
            Exception error)
            : base(session, observable, AsyncResultType.Exception)
        {
            this.error = error;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncResultErrorEventArgs"/> class.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="observable"></param>
        /// <param name="error"></param>
        /// <param name="context"></param>
        public AsyncResultErrorEventArgs(
            CimSession session,
            IObservable<object> observable,
            Exception error,
            CimResultContext cimResultContext)
            : base(session, observable, AsyncResultType.Exception, cimResultContext)
        {
            this.error = error;
        }

        public readonly Exception error;
    }
    #endregion

    #region CimResultObserver
    /// <summary>
    /// <para>
    /// Observer to consume results from asynchronous operations, such as,
    /// EnumerateInstancesAsync operation of <see cref="CimSession"/> object.
    /// </para>
    /// <para>
    /// (See https://channel9.msdn.com/posts/J.Van.Gogh/Reactive-Extensions-API-in-depth-Contract/)
    /// for the IObserver/IObservable contact
    /// - the only possible sequence is OnNext* (OnCompleted|OnError)?
    /// - callbacks are serialized
    /// - Subscribe never throws
    /// </para>
    /// </summary>
    /// <typeparam name="T">object type</typeparam>
    internal class CimResultObserver<T> : IObserver<T>
    {
        /// <summary>
        /// Define an Event based on the NewActionHandler.
        /// </summary>
        public event EventHandler<AsyncResultEventArgsBase> OnNewResult;

        /// <summary>
        /// Initializes a new instance of the <see cref="CimResultObserver{T}"/> class.
        /// </summary>
        /// <param name="session"><see cref="CimSession"/> object that issued the operation.</param>
        /// <param name="observable">Operation that can be observed.</param>
        public CimResultObserver(CimSession session, IObservable<object> observable)
        {
            this.CurrentSession = session;
            this.observable = observable;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CimResultObserver{T}"/> class.
        /// </summary>
        /// <param name="session"><see cref="CimSession"/> object that issued the operation.</param>
        /// <param name="observable">Operation that can be observed.</param>
        public CimResultObserver(CimSession session,
            IObservable<object> observable,
            CimResultContext cimResultContext)
        {
            this.CurrentSession = session;
            this.observable = observable;
            this.context = cimResultContext;
        }

        /// <summary>
        /// <para>
        /// Operation completed successfully
        /// </para>
        /// </summary>
        public virtual void OnCompleted()
        {
            // callbacks should never throw any exception to
            // protocol layer, otherwise the client process will be
            // terminated because of unhandled exception, same with
            // OnNext, OnError
            try
            {
                AsyncResultCompleteEventArgs completeArgs = new(
                    this.CurrentSession, this.observable);
                this.OnNewResult(this, completeArgs);
            }
            catch (Exception ex)
            {
                this.OnError(ex);
                DebugHelper.WriteLogEx("{0}", 0, ex);
            }
        }

        /// <summary>
        /// <para>
        /// Operation completed with an error
        /// </para>
        /// </summary>
        /// <param name="error">Error object.</param>
        public virtual void OnError(Exception error)
        {
            try
            {
                AsyncResultErrorEventArgs errorArgs = new(
                    this.CurrentSession, this.observable, error, this.context);
                this.OnNewResult(this, errorArgs);
            }
            catch (Exception ex)
            {
                // !!ignore the exception
                DebugHelper.WriteLogEx("{0}", 0, ex);
            }
        }

        /// <summary>
        /// Deliver the result value.
        /// </summary>
        /// <param name="value"></param>
        protected void OnNextCore(object value)
        {
            DebugHelper.WriteLogEx("value = {0}.", 1, value);
            try
            {
                AsyncResultObjectEventArgs resultArgs = new(
                    this.CurrentSession, this.observable, value);
                this.OnNewResult(this, resultArgs);
            }
            catch (Exception ex)
            {
                this.OnError(ex);
                DebugHelper.WriteLogEx("{0}", 0, ex);
            }
        }

        /// <summary>
        /// <para>
        /// Operation got a new result object
        /// </para>
        /// </summary>
        /// <param name="value">Result object.</param>
        public virtual void OnNext(T value)
        {
            DebugHelper.WriteLogEx("value = {0}.", 1, value);
            // do not allow null value
            if (value == null)
            {
                return;
            }

            this.OnNextCore(value);
        }

        #region members

        /// <summary>
        /// Session object of the operation.
        /// </summary>
        protected CimSession CurrentSession { get; }

        /// <summary>
        /// Async operation that can be observed.
        /// </summary>
        private readonly IObservable<object> observable;

        /// <summary>
        /// <see cref="CimResultContext"/> object used during delivering result.
        /// </summary>
        private readonly CimResultContext context;
        #endregion
    }

    /// <summary>
    /// CimSubscriptionResultObserver class definition.
    /// </summary>
    internal class CimSubscriptionResultObserver : CimResultObserver<CimSubscriptionResult>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CimSubscriptionResultObserver"/> class.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="observable"></param>
        public CimSubscriptionResultObserver(CimSession session, IObservable<object> observable)
            : base(session, observable)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CimSubscriptionResultObserver"/> class.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="observable"></param>
        public CimSubscriptionResultObserver(
            CimSession session,
            IObservable<object> observable,
            CimResultContext context)
            : base(session, observable, context)
        {
        }

        /// <summary>
        /// Override the OnNext method.
        /// </summary>
        /// <param name="value"></param>
        public override void OnNext(CimSubscriptionResult value)
        {
            DebugHelper.WriteLogEx();
            base.OnNextCore(value);
        }
    }

    /// <summary>
    /// CimMethodResultObserver class definition.
    /// </summary>
    internal class CimMethodResultObserver : CimResultObserver<CimMethodResultBase>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CimMethodResultObserver"/> class.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="observable"></param>
        public CimMethodResultObserver(CimSession session, IObservable<object> observable)
            : base(session, observable)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CimMethodResultObserver"/> class.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="observable"></param>
        /// <param name="context"></param>
        public CimMethodResultObserver(
            CimSession session,
            IObservable<object> observable,
            CimResultContext context)
            : base(session, observable, context)
        {
        }

        /// <summary>
        /// Override the OnNext method.
        /// </summary>
        /// <param name="value"></param>
        public override void OnNext(CimMethodResultBase value)
        {
            DebugHelper.WriteLogEx();
            const string PSTypeCimMethodResult = @"Microsoft.Management.Infrastructure.CimMethodResult";
            const string PSTypeCimMethodStreamedResult = @"Microsoft.Management.Infrastructure.CimMethodStreamedResult";
            const string PSTypeCimMethodResultTemplate = @"{0}#{1}#{2}";

            string resultObjectPSType = null;
            PSObject resultObject = null;
            if (value is CimMethodResult methodResult)
            {
                resultObjectPSType = PSTypeCimMethodResult;
                resultObject = new PSObject();
                foreach (CimMethodParameter param in methodResult.OutParameters)
                {
                    resultObject.Properties.Add(new PSNoteProperty(param.Name, param.Value));
                }
            }
            else
            {
                if (value is CimMethodStreamedResult methodStreamedResult)
                {
                    resultObjectPSType = PSTypeCimMethodStreamedResult;
                    resultObject = new PSObject();
                    resultObject.Properties.Add(new PSNoteProperty(@"ParameterName", methodStreamedResult.ParameterName));
                    resultObject.Properties.Add(new PSNoteProperty(@"ItemType", methodStreamedResult.ItemType));
                    resultObject.Properties.Add(new PSNoteProperty(@"ItemValue", methodStreamedResult.ItemValue));
                }
            }

            if (resultObject != null)
            {
                resultObject.Properties.Add(new PSNoteProperty(@"PSComputerName", this.CurrentSession.ComputerName));
                resultObject.TypeNames.Insert(0, resultObjectPSType);
                resultObject.TypeNames.Insert(0, string.Format(CultureInfo.InvariantCulture, PSTypeCimMethodResultTemplate, resultObjectPSType, ClassName, MethodName));
                base.OnNextCore(resultObject);
            }
        }

        /// <summary>
        /// Methodname.
        /// </summary>
        internal string MethodName
        {
            get;
            set;
        }

        /// <summary>
        /// Classname.
        /// </summary>
        internal string ClassName
        {
            get;
            set;
        }
    }

    /// <summary>
    /// IgnoreResultObserver class definition.
    /// </summary>
    internal class IgnoreResultObserver : CimResultObserver<CimInstance>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IgnoreResultObserver"/> class.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="observable"></param>
        public IgnoreResultObserver(CimSession session, IObservable<object> observable)
            : base(session, observable)
        {
        }

        /// <summary>
        /// Override the OnNext method.
        /// </summary>
        /// <param name="value"></param>
        public override void OnNext(CimInstance value)
        {
            DebugHelper.WriteLogEx();
        }
    }
    #endregion
}
