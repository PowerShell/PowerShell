using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Globalization;
using NativeObject;

namespace Microsoft.Management.Infrastructure.Native
{
    internal class DangerousHandleAccessor : IDisposable
    {
        // Fields
        //private bool needToCallDangerousRelease;
        //private SafeHandle safeHandle;

        // Methods
        internal DangerousHandleAccessor(SafeHandle safeHandle)
        {
            throw new NotImplementedException();
        }
        [SuppressMessage("Micros.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Runtime.InteropServices.SafeHandle.DangerousGetHandle", Justification = "We are calling DangerousAddRef/Release as prescribed in the docs + have to do this to call inline methods")]
        internal IntPtr DangerousGetHandle()
        {
            throw new NotImplementedException();
        }
        //public sealed override void Dispose()
        public void Dispose()
        {
            throw new NotImplementedException();
        }
        //protected virtual void Dispose([MarshalAs(UnmanagedType.U1)] bool A_0)
    }
}
