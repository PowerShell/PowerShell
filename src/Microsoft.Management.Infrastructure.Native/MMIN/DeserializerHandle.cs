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
    // TODO: classes with sequential layout cannot inherit.  Create a Data Transfer Object
    [StructLayout(LayoutKind.Sequential)]
    internal class DeserializerHandle// : SafeHandleZeroOrMinusOneIsInvalid
    {
        public NativeObject.MI_Deserializer miDeserializer;
        internal DeserializerHandle()
        {
            this.miDeserializer = new NativeObject.MI_Deserializer();
        }

        // Used when setting the deserializer handle to the return value
        internal DeserializerHandle(IntPtr handle)
        {
            this.miDeserializer = Marshal.PtrToStructure<NativeObject.MI_Deserializer>(handle);
        }

        internal void AssertValidInternalState()
        {
            //throw new NotImplementedException();
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
    }
}
