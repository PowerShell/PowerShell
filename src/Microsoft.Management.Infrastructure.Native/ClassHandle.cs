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
    [StructLayout(LayoutKind.Sequential)]
    internal class ClassHandle// : SafeHandleZeroOrMinusOneIsInvalid
    {
        public NativeObject.MI_Class miClass;

        internal ClassHandle()
        {
            this.miClass = NativeObject.MI_Class.NewDirectPtr();
        }

        internal ClassHandle(IntPtr handle)
        {
            this.miClass = Marshal.PtrToStructure<NativeObject.MI_Class>(handle);
        }

         internal void AssertValidInternalState()
        {
            throw new NotImplementedException();
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
        internal delegate void OnHandleReleasedDelegate();
        internal MiResult ReleaseHandleAsynchronously(OnHandleReleasedDelegate completionCallback)
        {
            throw new NotImplementedException();
        }
        internal MiResult ReleaseHandleSynchronously()
        {
            throw new NotImplementedException();
        }

        // should be part of SafeHandleZeroOrMinusOneIsInvalid
        public bool IsInvalid { get; }
    }
}
