#if !UNIX
//-----------------------------------------------------------------------
// <copyright file="IMethodInvoker.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace System.Management.Automation.Tracing
{
    using System;

    internal interface IMethodInvoker
    {
        Delegate Invoker { get; }
        object[] CreateInvokerArgs(Delegate methodToInvoke, object[] methodToInvokeArgs);
    }
}


#endif
