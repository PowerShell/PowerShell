// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/*============================================================
**
**
** Purpose:
** This internal class is a SafeHandle implementation over a
** native CoTaskMem allocated via StringToCoTaskMemAuto.
**
============================================================*/

using System.Runtime.InteropServices;

namespace System.Diagnostics.Eventing.Reader
{
    internal sealed class CoTaskMemSafeHandle : SafeHandle
    {
        internal CoTaskMemSafeHandle()
            : base(IntPtr.Zero, true)
        {
        }

        internal void SetMemory(IntPtr handle)
        {
            SetHandle(handle);
        }

        internal IntPtr GetMemory()
        {
            return handle;
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
            Marshal.FreeCoTaskMem(handle);
            handle = IntPtr.Zero;
            return true;
        }

        //
        // DONT compare CoTaskMemSafeHandle with CoTaskMemSafeHandle.Zero
        // use IsInvalid instead. Zero is provided where a NULL handle needed
        //

        public static CoTaskMemSafeHandle Zero
        {
            get
            {
                return new CoTaskMemSafeHandle();
            }
        }
    }
}
