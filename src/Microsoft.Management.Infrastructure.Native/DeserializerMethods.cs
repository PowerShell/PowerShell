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
    internal class DeserializerMethods
    {
        // Methods
        private DeserializerMethods()
        {
            throw new NotImplementedException();
        }
        internal static MiResult DeserializeClass(DeserializerHandle deserializerHandle, uint flags, byte[] serializedBuffer, uint offset, ClassHandle parentClass, string serverName, string nameSpace, out ClassHandle deserializedClass, out uint inputBufferUsed, out InstanceHandle cimErrorDetails)
        {
            throw new NotImplementedException();
        }
        internal static MiResult DeserializeInstance(DeserializerHandle deserializerHandle, uint flags, byte[] serializedBuffer, uint offset, ClassHandle[] classObjects, out InstanceHandle deserializedInstance, out uint inputBufferUsed, out InstanceHandle cimErrorDetails)
        {
            throw new NotImplementedException();
        }
    }
}
