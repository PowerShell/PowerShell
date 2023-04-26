// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using System.Management.Automation.ComInterop;

namespace System.Management.Automation.InteropServices
{
    internal partial class ComEventsSink
    {
        private void Initialize(object rcw, Guid iid)
        {
            _iidSourceItf = iid;
            Advise(rcw);
        }

        public void AddHandler(int dispid, object func)
        {
            ComEventsMethod method = FindMethod(dispid);
            method ??= AddMethod(dispid);

            if (func is Delegate d)
            {
                method.AddDelegate(d);
            }
            else
            {
                method.AddDelegate(new SplatCallSite.InvokeDelegate(new SplatCallSite(func).Invoke), wrapArgs: true);
            }
        }

        public void RemoveHandler(int dispid, object func)
        {
            ComEventsMethod sinkEntry = FindMethod(dispid);
            if (sinkEntry == null)
            {
                return;
            }

            if (func is Delegate d)
            {
                sinkEntry.RemoveDelegate(d);
            }
            else
            {
                // Remove the delegate from multicast delegate chain.
                // We will need to find the delegate that corresponds
                // to the func handler we want to remove. This will be
                // easy since we Target property of the delegate object
                // is a ComEventCallContext object.
                sinkEntry.RemoveDelegates(d => d.Target is SplatCallSite callContext && callContext._callable.Equals(func));
            }

            // If the delegates chain is empty - we can remove
            // corresponding ComEvenSinkEntry
            if (sinkEntry.Empty)
                RemoveMethod(sinkEntry);

            if (_methods == null || _methods.Empty)
            {
                Unadvise();
                _iidSourceItf = Guid.Empty;
            }
        }

        public static ComEventsSink FromRuntimeCallableWrapper(object rcw, Guid sourceIid, bool createIfNotFound)
        {
            List<ComEventsSink> comEventSinks = ComEventSinksContainer.FromRuntimeCallableWrapper(rcw, createIfNotFound);
            if (comEventSinks == null)
            {
                return null;
            }

            ComEventsSink comEventSink = null;
            lock (comEventSinks)
            {
                foreach (ComEventsSink sink in comEventSinks)
                {
                    if (sink._iidSourceItf == sourceIid)
                    {
                        comEventSink = sink;
                        break;
                    }

                    if (sink._iidSourceItf == Guid.Empty)
                    {
                        // we found a ComEventSink object that
                        // was previously disposed. Now we will reuse it.
                        sink.Initialize(rcw, sourceIid);
                        comEventSink = sink;
                    }
                }

                if (comEventSink == null && createIfNotFound)
                {
                    comEventSink = new ComEventsSink(rcw, sourceIid);
                    comEventSinks.Add(comEventSink);
                }
            }

            return comEventSink;
        }
    }
}
