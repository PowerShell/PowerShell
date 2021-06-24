<<<<<<< HEAD
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

#if !UNIX
=======
﻿//-----------------------------------------------------------------------
// <copyright file="IMethodInvoker.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
>>>>>>> origin/source-depot

namespace System.Management.Automation.Tracing
{
    using System;

    internal interface IMethodInvoker
    {
        Delegate Invoker { get; }

        object[] CreateInvokerArgs(Delegate methodToInvoke, object?[]? methodToInvokeArgs);
    }
}
<<<<<<< HEAD

#endif
=======
>>>>>>> origin/source-depot
