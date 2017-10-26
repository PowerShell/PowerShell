//
//    Copyright (c) Microsoft Corporation. All rights reserved.
//

using System;
using System.Runtime.InteropServices;

using System.Runtime.ConstrainedExecution;

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


        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
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


        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
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


        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle()
        {
            return (PdhHelper.PdhCloseLog(handle, 0) == 0);
        }
    }
}
