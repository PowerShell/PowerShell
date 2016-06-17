/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

namespace Microsoft.PowerShell.Workflow
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    /// A strongly typed AsyncResult that completes as soon as it is instantiated.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class TypedCompletedAsyncResult<T> : TypedAsyncResult<T>
    {
        public TypedCompletedAsyncResult(T data, AsyncCallback callback, object state)
            : base(callback, state)
        {
            Complete(data, true);
        }

        public new static T End(IAsyncResult result)
        {
            TypedCompletedAsyncResult<T> completedResult = result as TypedCompletedAsyncResult<T>;

            if (completedResult == null)
            {
                throw new ArgumentException(Resources.InvalidAsyncResult);
            }

            return TypedAsyncResult<T>.End(completedResult);
        }
    }
}