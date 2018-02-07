#if !UNIX


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
