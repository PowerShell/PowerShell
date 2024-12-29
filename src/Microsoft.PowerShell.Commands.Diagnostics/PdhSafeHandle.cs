// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Powershell.Commands.GetCounter.PdhNative
{
    internal sealed class PdhSafeDataSourceHandle : SafeHandle
    {
        private PdhSafeDataSourceHandle() : base(IntPtr.Zero, true) { }

        public override bool IsInvalid
        {
            get
            {
                return handle == IntPtr.Zero;
            }
        }

        protected override bool ReleaseHandle()
        {
            return (PdhHelper.PdhCloseLog(handle, 0) == 0);
        }
    }

    internal sealed class PdhSafeQueryHandle : SafeHandle
    {
        private PdhSafeQueryHandle() : base(IntPtr.Zero, true) { }

        public override bool IsInvalid
        {
            get
            {
                return handle == IntPtr.Zero;
            }
        }

        protected override bool ReleaseHandle()
        {
            return (PdhHelper.PdhCloseQuery(handle) == 0);
        }
    }

    internal sealed class PdhSafeLogHandle : SafeHandle
    {
        private PdhSafeLogHandle() : base(IntPtr.Zero, true) { }

        public override bool IsInvalid
        {
            get
            {
                return handle == IntPtr.Zero;
            }
        }

        protected override bool ReleaseHandle()
        {
            return (PdhHelper.PdhCloseLog(handle, 0) == 0);
        }
    }
}
