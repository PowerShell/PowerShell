// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/*============================================================
**
**
** Purpose:
** This internal class is a SafeHandle implementation over a
** native EVT_HANDLE - obtained from EventLog Native Methods.
**
============================================================*/

using System.Runtime.InteropServices;

namespace System.Diagnostics.Eventing.Reader
{
    internal sealed class EventLogHandle : SafeHandle
    {
        // Called by P/Invoke when returning SafeHandles
        private EventLogHandle()
            : base(IntPtr.Zero, true)
        {
        }

        internal EventLogHandle(IntPtr handle, bool ownsHandle)
            : base(IntPtr.Zero, ownsHandle)
        {
            SetHandle(handle);
        }

        public override bool IsInvalid
        {
            get
            {
                return IsClosed || handle == IntPtr.Zero;
            }
        }

        protected override bool ReleaseHandle()
        {
            NativeWrapper.EvtClose(handle);
            handle = IntPtr.Zero;
            return true;
        }

        // DONT compare EventLogHandle with EventLogHandle.Zero
        // use IsInvalid instead. Zero is provided where a NULL handle needed
        public static EventLogHandle Zero
        {
            get
            {
                return new EventLogHandle();
            }
        }
    }
}
