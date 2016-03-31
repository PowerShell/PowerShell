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
    internal class SerializerMethods
    {
        // Methods
        private SerializerMethods()
        {
            throw new NotImplementedException();
        }
        internal static MiResult SerializeClass(SerializerHandle serializerHandle, uint flags, ClassHandle instanceHandle, byte[] outputBuffer, uint offset, out uint outputBufferUsed)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SerializeInstance(SerializerHandle serializerHandle, uint flags, InstanceHandle instanceHandle, byte[] outputBuffer, uint offset, out uint outputBufferUsed)
        {
            throw new NotImplementedException();
        }
    }
}
