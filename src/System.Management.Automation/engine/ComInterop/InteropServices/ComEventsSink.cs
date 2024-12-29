// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using System.Runtime.InteropServices;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace System.Management.Automation.InteropServices
{
    /// <summary>
    /// Part of ComEventHelpers APIs which allow binding
    /// managed delegates to COM's connection point based events.
    /// </summary>
    internal partial class ComEventsSink : IDispatch, ICustomQueryInterface
    {
        private Guid _iidSourceItf;
        private ComTypes.IConnectionPoint? _connectionPoint;
        private int _cookie;
        private ComEventsMethod? _methods;
        private ComEventsSink? _next;

        public ComEventsSink(object rcw, Guid iid)
        {
            _iidSourceItf = iid;
            this.Advise(rcw);
        }

        public static ComEventsSink? Find(ComEventsSink? sinks, ref Guid iid)
        {
            ComEventsSink? sink = sinks;
            while (sink != null && sink._iidSourceItf != iid)
            {
                sink = sink._next;
            }

            return sink;
        }

        public static ComEventsSink Add(ComEventsSink? sinks, ComEventsSink sink)
        {
            sink._next = sinks;
            return sink;
        }

        public static ComEventsSink? RemoveAll(ComEventsSink? sinks)
        {
            while (sinks != null)
            {
                sinks.Unadvise();
                sinks = sinks._next;
            }

            return null;
        }

        public static ComEventsSink? Remove(ComEventsSink sinks, ComEventsSink sink)
        {
            Debug.Assert(sinks != null, "removing event sink from empty sinks collection");
            Debug.Assert(sink != null, "specify event sink is null");

            ComEventsSink? toReturn = sinks;

            if (sink == sinks)
            {
                toReturn = sinks._next;
            }
            else
            {
                ComEventsSink? current = sinks;
                while (current != null && current._next != sink)
                {
                    current = current._next;
                }

                if (current != null)
                {
                    current._next = sink._next;
                }
            }

            sink.Unadvise();

            return toReturn;
        }

        public ComEventsMethod? RemoveMethod(ComEventsMethod method)
        {
            _methods = ComEventsMethod.Remove(_methods!, method);
            return _methods;
        }

        public ComEventsMethod? FindMethod(int dispid)
        {
            return ComEventsMethod.Find(_methods, dispid);
        }

        public ComEventsMethod AddMethod(int dispid)
        {
            ComEventsMethod method = new ComEventsMethod(dispid);
            _methods = ComEventsMethod.Add(_methods, method);
            return method;
        }

        int IDispatch.GetTypeInfoCount()
        {
            return 0;
        }

        ComTypes.ITypeInfo IDispatch.GetTypeInfo(int iTInfo, int lcid)
        {
            throw new NotImplementedException();
        }

        void IDispatch.GetIDsOfNames(ref Guid iid, string[] names, int cNames, int lcid, int[] rgDispId)
        {
            throw new NotImplementedException();
        }

        private const VarEnum VT_BYREF_VARIANT = VarEnum.VT_BYREF | VarEnum.VT_VARIANT;
        private const VarEnum VT_TYPEMASK = (VarEnum)0x0fff;
        private const VarEnum VT_BYREF_TYPEMASK = VT_TYPEMASK | VarEnum.VT_BYREF;

        private static unsafe ref Variant GetVariant(ref Variant pSrc)
        {
            if (pSrc.VariantType == VT_BYREF_VARIANT)
            {
                // For VB6 compatibility reasons, if the VARIANT is a VT_BYREF | VT_VARIANT that
                // contains another VARIANT with VT_BYREF | VT_VARIANT, then we need to extract the
                // inner VARIANT and use it instead of the outer one. Note that if the inner VARIANT
                // is VT_BYREF | VT_VARIANT | VT_ARRAY, it will pass the below test too.
                Span<Variant> pByRefVariant = new Span<Variant>(pSrc.AsByRefVariant.ToPointer(), 1);
                if ((pByRefVariant[0].VariantType & VT_BYREF_TYPEMASK) == VT_BYREF_VARIANT)
                {
                    return ref pByRefVariant[0];
                }
            }

            return ref pSrc;
        }

        unsafe void IDispatch.Invoke(
            int dispid,
            ref Guid riid,
            int lcid,
            InvokeFlags wFlags,
            ref ComTypes.DISPPARAMS pDispParams,
            IntPtr pVarResult,
            IntPtr pExcepInfo,
            IntPtr puArgErr)
        {
            ComEventsMethod? method = FindMethod(dispid);
            if (method == null)
            {
                return;
            }

            // notice the unsafe pointers we are using. This is to avoid unnecessary
            // arguments marshalling. see code:ComEventsHelper#ComEventsArgsMarshalling

            const int InvalidIdx = -1;
            object[] args = new object[pDispParams.cArgs];
            int[] byrefsMap = new int[pDispParams.cArgs];
            bool[] usedArgs = new bool[pDispParams.cArgs];

            int totalCount = pDispParams.cNamedArgs + pDispParams.cArgs;
            var vars = new Span<Variant>(pDispParams.rgvarg.ToPointer(), totalCount);
            var namedArgs = new Span<int>(pDispParams.rgdispidNamedArgs.ToPointer(), totalCount);

            // copy the named args (positional) as specified
            int i;
            int pos;
            for (i = 0; i < pDispParams.cNamedArgs; i++)
            {
                pos = namedArgs[i];
                ref Variant pvar = ref GetVariant(ref vars[i]);
                args[pos] = pvar.ToObject()!;
                usedArgs[pos] = true;

                int byrefIdx = InvalidIdx;
                if (pvar.IsByRef)
                {
                    byrefIdx = i;
                }

                byrefsMap[pos] = byrefIdx;
            }

            // copy the rest of the arguments in the reverse order
            pos = 0;
            for (; i < pDispParams.cArgs; i++)
            {
                // find the next unassigned argument
                while (usedArgs[pos])
                {
                    pos++;
                }

                ref Variant pvar = ref GetVariant(ref vars[pDispParams.cArgs - 1 - i]);
                args[pos] = pvar.ToObject()!;

                int byrefIdx = InvalidIdx;
                if (pvar.IsByRef)
                {
                    byrefIdx = pDispParams.cArgs - 1 - i;
                }

                byrefsMap[pos] = byrefIdx;

                pos++;
            }

            // Do the actual delegate invocation
            object? result = method.Invoke(args);

            // convert result to VARIANT
            if (pVarResult != IntPtr.Zero)
            {
                Marshal.GetNativeVariantForObject(result, pVarResult);
            }

            // Now we need to marshal all the byrefs back
            for (i = 0; i < pDispParams.cArgs; i++)
            {
                int idxToPos = byrefsMap[i];
                if (idxToPos == InvalidIdx)
                {
                    continue;
                }

                ref Variant pvar = ref GetVariant(ref vars[idxToPos]);
                pvar.CopyFromIndirect(args[i]);
            }
        }

        CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out IntPtr ppv)
        {
            ppv = IntPtr.Zero;
            if (iid == _iidSourceItf || iid == typeof(IDispatch).GUID)
            {
                ppv = Marshal.GetComInterfaceForObject(this, typeof(IDispatch), CustomQueryInterfaceMode.Ignore);
                return CustomQueryInterfaceResult.Handled;
            }

            return CustomQueryInterfaceResult.NotHandled;
        }

        private void Advise(object rcw)
        {
            Debug.Assert(_connectionPoint == null, "COM event sink is already advised");

            ComTypes.IConnectionPointContainer cpc = (ComTypes.IConnectionPointContainer)rcw;
            ComTypes.IConnectionPoint cp;
            cpc.FindConnectionPoint(ref _iidSourceItf, out cp!);

            object sinkObject = this;
            cp.Advise(sinkObject, out _cookie);

            _connectionPoint = cp;
        }

        private void Unadvise()
        {
            Debug.Assert(_connectionPoint != null, "Can not unadvise from empty connection point");
            if (_connectionPoint == null)
                return;

            try
            {
                _connectionPoint.Unadvise(_cookie);
                Marshal.ReleaseComObject(_connectionPoint);
            }
            catch
            {
                // swallow all exceptions on unadvise
                // the host may not be available at this point
            }
            finally
            {
                _connectionPoint = null;
            }
        }
    }
}
