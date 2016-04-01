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
    internal class ApplicationMethodsInternal
    {
        // Methods
        private ApplicationMethodsInternal()
        {
            throw new NotImplementedException();
        }

        // Takes a newly declared DeserializerHandle, and executes a P/Invoke to create a deserialzerMOF instance.  This requires access to mofcodec.  ApplicationHandle should already be created via Native.Initialize.
        internal static MiResult NewDeserializerMOF(ApplicationHandle applicationHandle, string format, uint flags, out DeserializerHandle deserializerHandle)
        {
            throw new NotImplementedException();
            ////TODO: the applicationdHandle should already be set.  Implement guards to verify
            //// create a pointer to the app and deserializer
            //IntPtr appBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(applicationHandle.miApp)); // app size should be 24
            //Marshal.StructureToPtr(applicationHandle.miApp, appBuffer, false);

            //// Create the deserializer
            //deserializerHandle                         = new DeserializerHandle();
            //NativeObject.MI_Deserializer deserializer      = deserializerHandle.miDeserializer;
            //deserializer.reserved1                     = 0;
            //deserializer.reserved2                     = IntPtr.Zero;
            //IntPtr deserializeBuffer                   = Marshal.AllocHGlobal(Marshal.SizeOf(deserializer));
            //Marshal.StructureToPtr(deserializer, deserializeBuffer, false); // map the struct to an IntPtr

            //Console.WriteLine("Creating a New DeserializerMOF");
            //// takes a pointer to the applicationHandle struct, and a pointer to the deserializerHandle struct.
            //// Sets the deserializer struct.
            //MiResult result = NativeObject.MI_Application_NewDeserializer_Mof(ref applicationHandle.miApp, flags, format, out deserializer);
            //Console.WriteLine("Native>> Created new deserializer Mof Pinvoke returned {0}", result);
            //deserializerHandle = Marshal.PtrToStructure<DeserializerHandle>(deserializeBuffer);
            //deserializerHandle.miDeserializer = deserializer;
            //// TODO: if result != MiResult.OK, return result
            //// TODO: Debug.Assert that the structs are populated correctly.

            //// Free the buffers
            //Marshal.FreeHGlobal(deserializeBuffer);
            //Marshal.FreeHGlobal(appBuffer);

            //return result;
        }

        internal static MiResult NewSerializerMOF(ApplicationHandle applicationHandle, string format, uint flags, out SerializerHandle serializerHandle)
        {
            throw new NotImplementedException();
        }
    }
}
