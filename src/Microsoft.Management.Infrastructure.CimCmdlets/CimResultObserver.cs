// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#region Using directives

using System;
using System.Management.Automation;
using System.Globalization;

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
        /// Constructor.
        /// </summary>
        /// <param name="ErrorSource"></param>
        internal CimResultContext(object ErrorSource)
        {
            this.errorSource = ErrorSource;
        }

        /// <summary>
        /// ErrorSource property.
        /// </summary>
        internal object ErrorSource
        {
            get
            {
                return this.errorSource;
            }
        }

        private object errorSource;
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
        /// Constructor.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="observable"></param>
        /// <param name="resultType"></param>
        public AsyncResultEventArgsBase(
            CimSession session,
            IObservable<object> observable,
            AsyncResultType resultType)
        {
            this.session = session;
            this.observable = observable;
            this.resultType = resultType;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="observable"></param>
        /// <param name="resultType"></param>
        /// <param name="context"></param>
        public AsyncResultEventArgsBase(
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
        /// <para>
        /// Constructor
        /// </para>
        /// </summary>
        /// <param name="session"><see cref="CimSession"/> object.</param>
        /// <param name="cancellationDisposable"></param>
        public AsyncResultCompleteEventArgs(
            CimSession session,
            IObservable<object> observable) :
            base(session, observable, AsyncResultType.Completion)
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
        /// Constructor.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="observable"></param>
        /// <param name="resultObject"></param>
        public AsyncResultObjectEventArgs(
            CimSession session,
            IObservable<object> observable,
            object resultObject) :
            base(session, observable, AsyncResultType.Result)
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
        /// Constructor.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="observable"></param>
        /// <param name="error"></param>
        public AsyncResultErrorEventArgs(
            CimSession session,
            IObservable<object> observable,
            Exception error) :
            base(session, observable, AsyncResultType.Exception)
        {
            this.error = error;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="observable"></param>
        /// <param name="error"></param>
        /// <param name="context"></param>
        public AsyncResultErrorEventArgs(
            CimSession session,
            IObservable<object> observable,
            Exception error,
            CimResultContext cimResultContext) :
            base(session, observable, AsyncResultType.Exception, cimResultContext)
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
        /// Define delegate that handles new cmdlet action come from
        /// the operations related to the current CimSession object.
        /// </summary>
        /// <param name="cimSession">CimSession object, which raised the event.</param>
        /// <param name="actionArgs">Event args.</param>
        public delegate void ResultEventHandler(
            object observer,
            AsyncResultEventArgsBase resultArgs);

        /// <summary>
        /// Define an Event based on the NewActionHandler.
        /// </summary>
        public event ResultEventHandler OnNewResult;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="session"><see cref="CimSession"/> object that issued the operation.</param>
        /// <param name="observable">Operation that can be observed.</param>
        public CimResultObserver(CimSession session, IObservable<object> observable)
        {
            this.session = session;
            this.observable = observable;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="session"><see cref="CimSession"/> object that issued the operation.</param>
        /// <param name="observable">Operation that can be observed.</param>
        public CimResultObserver(CimSession session,
            IObservable<object> observable,
            CimResultContext cimResultContext)
        {
            this.session = session;
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
                AsyncResultCompleteEventArgs completeArgs = new AsyncResultCompleteEventArgs(
                    this.session, this.observable);
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
                AsyncResultErrorEventArgs errorArgs = new AsyncResultErrorEventArgs(
                    this.session, this.observable, error, this.context);
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
                AsyncResultObjectEventArgs resultArgs = new AsyncResultObjectEventArgs(
                    this.session, this.observable, value);
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
        protected CimSession CurrentSession
        {
            get
            {
                return session;
            }
        }

        private CimSession session;

        /// <summary>
        /// Async operation that can be observed.
        /// </summary>
        private IObservable<object> observable;

        /// <summary>
        /// <see cref="CimResultContext"/> object used during delivering result.
        /// </summary>
        private CimResultContext context;
        #endregion
    }

    /// <summary>
    /// CimSubscriptionResultObserver class definition.
    /// </summary>
    internal class CimSubscriptionResultObserver : CimResultObserver<CimSubscriptionResult>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="observable"></param>
        public CimSubscriptionResultObserver(CimSession session, IObservable<object> observable)
            : base(session, observable)
        {
        }

        /// <summary>
        /// Constructor.
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
        /// Constructor.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="observable"></param>
        public CimMethodResultObserver(CimSession session, IObservable<object> observable)
            : base(session, observable)
        {
        }

        /// <summary>
        /// Constructor.
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
            CimMethodResult methodResult = value as CimMethodResult;
            if (methodResult != null)
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
                CimMethodStreamedResult methodStreamedResult = value as CimMethodStreamedResult;
                if (methodStreamedResult != null)
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
        /// Constructor.
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
