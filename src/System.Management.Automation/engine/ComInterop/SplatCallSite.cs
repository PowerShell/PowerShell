// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Management.Automation.ComInterop
{
    internal sealed class SplatCallSite
    {
        // Stored callable IDynamicMetaObjectProvider.
        internal readonly object _callable;

        // Can the number of arguments to a given event change each call?
        // If not, we don't need this level of indirection--we could cache a
        // delegate that does the splatting.
        private CallSite<Func<CallSite, object, object[], object>> _site;

        internal SplatCallSite(object callable)
        {
            Debug.Assert(callable != null);
            _callable = callable;
        }

        public delegate object InvokeDelegate(object[] args);

        internal object Invoke(object[] args)
        {
            Debug.Assert(args != null);

            // Create a CallSite and invoke it.
            if (_site == null)
            {
                _site = CallSite<Func<CallSite, object, object[], object>>.Create(SplatInvokeBinder.Instance);
            }

            return _site.Target(_site, _callable, args);
        }
    }
}
