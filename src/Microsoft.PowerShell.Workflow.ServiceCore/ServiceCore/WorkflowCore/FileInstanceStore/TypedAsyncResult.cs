/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

namespace Microsoft.PowerShell.Workflow
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    /// A strongly typed AsyncResult.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    abstract class TypedAsyncResult<T> : FileStoreAsyncResult
    {
        T data;

        protected TypedAsyncResult(AsyncCallback callback, object state)
            : base(callback, state)
        {
        }

        public T Data
        {
            get { return data; }
        }

        protected void Complete(T data, bool completedSynchronously)
        {
            this.data = data;
            Complete(completedSynchronously);
        }

        public static T End(IAsyncResult result)
        {
            TypedAsyncResult<T> typedResult = FileStoreAsyncResult.End<TypedAsyncResult<T>>(result);
            return typedResult.Data;
        }
    }
}