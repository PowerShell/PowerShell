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
    internal class DeserializerCallbacks
    {
        //public NativeObject.MI_DeserializerCallbacks miDeserializerCallbacks;
        // Fields
        //private ClassObjectNeededCallbackDelegate<backing_store> ClassObjectNeededCallback;
        //private GetIncludedFileBufferCallbackDelegate<backing_store> GetIncludedFileBufferCallback;
        //private object <backing_store>ManagedDeserializerContext;

        // Methods
        internal DeserializerCallbacks()
        {
            // TODO: Implement
            //this.miDeserializerCallbacks = new NativeObject.MI_DeserializerCallbacks();
        }
        //internal static unsafe _MI_Result ClassObjectNeededAppDomainProxy(void* context, ushort modopt(IsConst)* serverName, ushort modopt(IsConst)* namespaceName, ushort modopt(IsConst)* className, _MI_Class** requestedClassObject)
        //internal static unsafe _MI_Result GetIncludedFileBufferAppDomainProxy(void* context, ushort modopt(IsConst)* fileName, byte** fileBuffer, uint* bufferLength)
        //internal static unsafe void ReleaseDeserializerCallbacksProxy(DeserializerCallbacksProxy* pCallbacksProxy)
        //[return: MarshalAs(UnmanagedType.U1)]
        //internal bool SetMiDeserializerCallbacks(NativeObject.MI_DeserializerCallbacks pmiDeserializerCallbacks)
        //{
        //    //TODO: Implement
        //    return true;
        //}
        //private static unsafe void StoreCallbackDelegate(DeserializerCallbacksProxy* pCallbacksProxy, Delegate externalCallback, Delegate appDomainProxyCallback, DeserializerCallbackId callbackId);

        // Properties
        [SuppressMessage("Micros.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal ClassObjectNeededCallbackDelegate ClassObjectNeededCallback { get; set; }
        [SuppressMessage("Micros.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal GetIncludedFileBufferCallbackDelegate GetIncludedFileBufferCallback { get; set; }
        [SuppressMessage("Micros.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp and in cs/CimOperationOptions.cs")]
        internal object ManagedDeserializerContext { get; set; }

        // Nested Types
        //internal unsafe delegate _MI_Result ClassObjectNeededAppDomainProxyDelegate(void* context, ushort modopt(IsConst)* serverName, ushort modopt(IsConst)* namespaceName, ushort modopt(IsConst)* className, _MI_Class** requestedClassObject)
        internal delegate bool ClassObjectNeededCallbackDelegate(string serverName, string namespaceName, string className, out ClassHandle classHandle);
        //internal unsafe delegate _MI_Result GetIncludedFileBufferAppDomainProxyDelegate(void* context, ushort modopt(IsConst)* fileName, byte** fileBuffer, uint* bufferLength)
        internal delegate bool GetIncludedFileBufferCallbackDelegate(string fileName, out byte[] fileBuffer);
    }
}
