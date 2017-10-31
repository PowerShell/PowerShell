/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

#if !SILVERLIGHT // ComObject

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Management.Automation.ComInterop
{
    /// <summary>
    /// ComEventSinksContainer is just a regular list with a finalizer.
    /// This list is usually attached as a custom data for RCW object and
    /// is finalized whenever RCW is finalized.
    /// </summary>
    internal class ComEventSinksContainer : List<ComEventSink>, IDisposable
    {
        private ComEventSinksContainer()
        {
        }

        private static readonly object s_comObjectEventSinksKey = new object();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists")]
        public static ComEventSinksContainer FromRuntimeCallableWrapper(object rcw, bool createIfNotFound)
        {
            // !!! Marshal.Get/SetComObjectData has a LinkDemand for UnmanagedCode which will turn into
            // a full demand. We need to avoid this by making this method SecurityCritical
            object data = Marshal.GetComObjectData(rcw, s_comObjectEventSinksKey);
            if (data != null || createIfNotFound == false)
            {
                return (ComEventSinksContainer)data;
            }

            lock (s_comObjectEventSinksKey)
            {
                data = Marshal.GetComObjectData(rcw, s_comObjectEventSinksKey);
                if (data != null)
                {
                    return (ComEventSinksContainer)data;
                }

                ComEventSinksContainer comEventSinks = new ComEventSinksContainer();
                if (!Marshal.SetComObjectData(rcw, s_comObjectEventSinksKey, comEventSinks))
                {
                    throw Error.SetComObjectDataFailed();
                }

                return comEventSinks;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            DisposeAll();
            GC.SuppressFinalize(this);
        }

        #endregion

        private void DisposeAll()
        {
            foreach (ComEventSink sink in this)
            {
                sink.Dispose();
            }
        }

        ~ComEventSinksContainer()
        {
            DisposeAll();
        }
    }
}

#endif

