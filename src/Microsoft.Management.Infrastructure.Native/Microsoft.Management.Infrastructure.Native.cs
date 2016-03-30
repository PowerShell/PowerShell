using Micros.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Globalization;
using NativeObject;

/* MI_ primitive types. Rather than define structs, this is one alternative */
using MI_Boolean = System.Byte; // unsigned char
using MI_Uint8   = System.Byte; // unsigned char
using MI_Sint8   = System.SByte; // signed char
using MI_Uint16  = System.UInt16; // unsigned short
using MI_Sint16  = System.Int16; // signed short
using MI_Uint32  = System.UInt32; // unsigned int
using MI_Sint32  = System.Int32; // signed int
using MI_Uint64  = System.UInt64; // unsigned long long
using MI_Sint64  = System.Int64; // signed long long
using MI_Real32  = System.Single; // float
using MI_Real64  = System.Double; // float
using MI_Char16  = System.UInt16; // unsigned short
using MI_Char    = System.Char;  // char (or wchar_t)
// /Primitive MI data types represented in C#

namespace Micros.Win32.SafeHandles
{
    internal class SafeHandleZeroOrMinusOneIsInvalid
    {
    }
}


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

    internal class AuthType
    {
        // Fields
        internal static string AuthTypeBasic;
        internal static string AuthTypeClientCerts;
        internal static string AuthTypeCredSSP;
        internal static string AuthTypeDefault;
        internal static string AuthTypeDigest;
        internal static string AuthTypeIssuerCert;
        internal static string AuthTypeKerberos;
        internal static string AuthTypeNegoNoCredentials;
        internal static string AuthTypeNegoWithCredentials;
        internal static string AuthTypeNone;
        internal static string AuthTypeNTLM;

        // Methods
        static AuthType()
        {
            throw new NotImplementedException();
        }
        private AuthType()
        {
            throw new NotImplementedException();
        }
    }

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

    internal class ClassMethods
    {
        // Methods
        private ClassMethods()
        {
            throw new NotImplementedException();
        }
        internal static MiResult Clone(ClassHandle ClassHandleToClone, out ClassHandle clonedClassHandle)
        {
            throw new NotImplementedException();
        }
        internal static int GetClassHashCode(ClassHandle handle)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetClassName(ClassHandle handle, out string className)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetClassQualifier_Index(ClassHandle handle, string name, out int index)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetElement_GetIndex(ClassHandle handle, string name, out int index)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetElementAt_GetFlags(ClassHandle handle, int index, out MiFlags flags)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetElementAt_GetName(ClassHandle handle, int index, out string name)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetElementAt_GetReferenceClass(ClassHandle handle, int index, out string referenceClass)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetElementAt_GetType(ClassHandle handle, int index, out MiType type)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetElementAt_GetValue(ClassHandle handle, int index, out object value)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetElementCount(ClassHandle handle, out int count)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethod_GetIndex(ClassHandle handle, string name, out int index)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodAt_GetName(ClassHandle handle, int methodIndex, int parameterIndex, out string name)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodAt_GetReferenceClass(ClassHandle handle, int methodIndex, int parameterIndex, out string referenceClass)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodAt_GetType(ClassHandle handle, int methodIndex, int parameterIndex, out MiType type)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodCount(ClassHandle handle, out int methodCount)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodElement_GetIndex(ClassHandle handle, int methodIndex, string name, out int index)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodElementAt_GetName(ClassHandle handle, int index, out string name)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodElementAt_GetType(ClassHandle handle, int index, out MiType type)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodGetQualifierElement_GetIndex(ClassHandle handle, int methodIndex, int parameterIndex, string name, out int index)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodParameterGetQualifierElementAt_GetFlags(ClassHandle handle, int methodIndex, int parameterName, int index, out MiFlags flags)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodParameterGetQualifierElementAt_GetName(ClassHandle handle, int methodIndex, int parameterName, int index, out string name)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodParameterGetQualifierElementAt_GetType(ClassHandle handle, int methodIndex, int parameterName, int index, out MiType type)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodParameterGetQualifierElementAt_GetValue(ClassHandle handle, int methodIndex, int parameterName, int index, out object value)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodParametersCount(ClassHandle handle, int index, out int parameterCount)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodParametersGetQualifiersCount(ClassHandle handle, int index, int parameterIndex, out int parameterCount)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodQualifierCount(ClassHandle handle, int methodIndex, out int parameterCount)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodQualifierElement_GetIndex(ClassHandle handle, int methodIndex, string name, out int index)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodQualifierElementAt_GetFlags(ClassHandle handle, int methodIndex, int qualifierIndex, out MiFlags flags)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodQualifierElementAt_GetName(ClassHandle handle, int methodIndex, int qualifierIndex, out string name)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodQualifierElementAt_GetType(ClassHandle handle, int methodIndex, int qualifierIndex, out MiType type)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodQualifierElementAt_GetValue(ClassHandle handle, int methodIndex, int qualifierIndex, out object value)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetNamespace(ClassHandle handle, out string nameSpace)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetParentClass(ClassHandle handle, out ClassHandle superClass)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetParentClassName(ClassHandle handle, out string className)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetPropertyQualifier_Count(ClassHandle handle, string name, out int count)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetPropertyQualifier_Index(ClassHandle handle, string propertyName, string name, out int index)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetPropertyQualifierElementAt_GetFlags(ClassHandle handle, int index, string propertyName, out MiFlags flags)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetPropertyQualifierElementAt_GetName(ClassHandle handle, int index, string propertyName, out string name)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetPropertyQualifierElementAt_GetType(ClassHandle handle, int index, string propertyName, out MiType type)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetPropertyQualifierElementAt_GetValue(ClassHandle handle, int index, string propertyName, out object value)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetQualifier_Count(ClassHandle handle, out int qualifierCount)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetQualifierElementAt_GetFlags(ClassHandle handle, int index, out MiFlags flags)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetQualifierElementAt_GetName(ClassHandle handle, int index, out string name)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetQualifierElementAt_GetType(ClassHandle handle, int index, out MiType type)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetQualifierElementAt_GetValue(ClassHandle handle, int index, out object value)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetServerName(ClassHandle handle, out string serverName)
        {
            throw new NotImplementedException();
        }
    }

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

    internal class DeserializerInternalMethods
    {
        // Methods
        private DeserializerInternalMethods()
        {
            //TODO: Implement
            //throw new NotImplementedException();
        }

        /*
         * If classObjects parameter is not null, then set the contents of classArray to that, otherwise, pass in empty
         * The deserializerHandle and serializedBuffer should already be populated by now.
         */
        internal static MiResult DeserializeClassArray(DeserializerHandle deserializerHandle, OperationOptionsHandle options, DeserializerCallbacks callback, byte[] serializedBuffer, uint offset, ClassHandle[] classObjects, string serverName, string nameSpace, out ClassHandle[] deserializedClasses, out uint inputBufferUsed, out InstanceHandle cimErrorDetails)
        {
            throw new NotImplementedException();
            ////TODO: Assert that deserializeHandle exists
            ////TODO: Assert that serializedbuffer exists
            //cimErrorDetails = new InstanceHandle();
            //Console.WriteLine(">>MMI.Native/DeserializeClassArray");
            //// create locals
            //NativeObject.MI_OperationOptions localOpts = options.miOperationOptions;

            ////IntPtr psb = Marshal.AllocHGlobal(serializedBuffer.Length);
            ////Marshal.Copy(serializedBuffer, 0, psb, serializedBuffer.Length);
            ////GCHandle gch = GCHandle.Alloc(serializedBuffer.Length, GCHandleType.Pinned);
            ////IntPtr psb = gch.AddrOfPinnedObject();
            //// if (serializedBuffer != null)
            //// {
            ////     int serializedBuffSize = Marshal.SizeOf(serializedBuffer[0])*serializedBuffer.Length;
            ////     sb = Marshal.AllocHGlobal(serializedBuffSize);
            ////     Marshal.Copy(serializedBuffer, 0, sb, serializedBuffer.Length);
            //// }
            //IntPtr deserializerBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(deserializerHandle.miDeserializer));
            //Marshal.StructureToPtr(deserializerHandle.miDeserializer, deserializerBuffer, false);
            //NativeObject.MI_Instance ErrorIns              = cimErrorDetails.miInstance;
            //IntPtr pErr                                = Marshal.AllocHGlobal(Marshal.SizeOf(ErrorIns));
            //inputBufferUsed                            = 0;
            //NativeObject.MI_DeserializerCallbacks cb       = callback.miDeserializerCallbacks;
            //IntPtr pNativeCallbacks                    = Marshal.AllocHGlobal(Marshal.SizeOf(cb));
            //uint serializedBufferRead;
            //IntPtr classObjectsBuffer;
            //int nativeClassObjectsLength;
            ////byte[] pbyNativeClassObjects;
            //// create an array of classArray structs.  If there is existing classArray information, assign that.
            ////NativeObject.MI_ClassA classArray;
            ////classArray.data = IntPtr.Zero;
            ////classArray.size = 0;
            //IntPtr classArray;
            //// Populate MI_ClassA.data when an array of classObjects is provided.
            //// This will later be passed as a pointer to MI_ClassA
           ///* if ((classObjects != null) && (0 < classObjects.Length))
            //{
            //    nativeClassObjectsLength = classObjects.Length;
            //    //now populate the date with all the MI_Class instances
            //    int iSizeOfOneClassHandle = Marshal.SizeOf(classObjects[0].miClass); 
            //    classObjectsBuffer = Marshal.AllocHGlobal(iSizeOfOneClassHandle * nativeClassObjectsLength);

            //    classObjects = new ClassHandle[nativeClassObjectsLength];
            //    for( uint i = 0; i < nativeClassObjectsLength; i++)
            //    {
            //        classObjects[i] = new ClassHandle();
            //    }
            //    //pbyNativeClassObjects = (byte[])(pNativeClassObjects.ToPointer());
            //    //for ( int i = 0; i < nativeClassObjectsLength; i++, pbyNativeClassObjects += (iSizeOfOneClassHandle) )
            //    //{
            //    //    IntPtr pOneClassObject = new IntPtr(pbyNativeClassObjects);
            //    //    Marshal.StructureToPtr(classObjects[i], pOneClassObject, false);
            //    //}
            //    classArray.data = classObjectsBuffer;
            //    classArray.size = (uint)nativeClassObjectsLength;
            //}*/

            ////if (options) { nativeOptions  }
            //IntPtr pOpts = IntPtr.Zero;
            ////if (callback) { callback.SetMiDeserializerCallbacks(ref pNativeCallbacks); }
            //// TODO: this is wrong- deserializedClasses array of classHandles needs to be created differently
           //// deserializedClasses                 = new ClassHandle[1];
           //// deserializedClasses[0]              = new ClassHandle();
           //// NativeObject.MI_Class dClasses          = deserializedClasses[0].miClass;

            ////var resultClassArray = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(IntPtr)));

            ////NativeObject.MI_ClassA resultClassArray;
            ////IntPtr resultClassABuffer = (IntPtr)Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(IntPtr)));
            ////// convert classObjects to byte array
            ////Console.WriteLine("the pinned address : {0}", gch.AddrOfPinnedObject());
            ////IntPtr gchPtr = gch.AddrOfPinnedObject();

            ////resultClassArray.data     = IntPtr.Zero;
            ////resultClassArray.size     = 1;
            ////IntPtr resultClassABuffer = Marshal.AllocHGlobal(Marshal.SizeOf(resultClassArray));
            ////Marshal.StructureToPtr(resultClassArray, resultClassABuffer, false);

            ////NativeObject.MI_ClassA classA = new NativeObject.MI_ClassA();
            ////IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(classA));
            ////Marshal.StructureToPtr(classA, ptr, false);
            //// execute p/invoke, assign to result. Note: flags= 0
            ////if ( classA == null) { Console.WriteLine("You dun goofed");}
            //IntPtr ptr;// = IntPtr.Zero;
            ////ptr = Marshal.AllocHGlobal(IntPtr.Size);
            //IntPtr bs;
            ////Marshal.StructureToPtr(callback, bs, false);//callbacks
            //MiResult result = NativeObject.MI_Deserializer_DeserializeClassArray(
            //                                            ref deserializerHandle.miDeserializer,
            //                                            0,
            //                                            ref localOpts,
            //                                            out bs, // callbacks
            //                                            serializedBuffer,
            //                                            (uint)serializedBuffer.Length,
            //                                            out classArray,  /* classArray: if null, pass IntPtr.Zero, else classArray */
            //                                            serverName,
            //                                            nameSpace,
            //                                            out serializedBufferRead,
            //                                            out ptr,
            //                                            out pErr);

            //Console.WriteLine("MI_Deserializer_DeserializeClassArray pinvoke complete.  result :{0}", result);
            //Console.WriteLine("classObjects: {0}", ptr);
            ////TODO: classHandle array from resultClassArray needs to be marshalled into managed code, then assign the array of classHandles to 
            //ClassHandle[] d = new ClassHandle[1];
            //deserializedClasses = d;
            ////ClassHandle[] d = Marshal.PtrToStructure<ClassHandle>(resultClassArray);
            ////if ((result == MiResult.OK) && (0 != resultClassArray.Size))
            ////{
            ////    deserializedClasses = new ClassHandle[resultClassArray.size];
            ////    for (int i=0; i < resultClassArray.size; i++)
            ////    {
            ////        deserializedClasses[i] = Marshal.PtrToStructure<ClassHandle>(resultClassArray.data);
            ////    }
            ////}

            //return result;

            ////throw new NotImplementedException();
        }
        internal static MiResult DeserializeInstanceArray(DeserializerHandle deserializerHandle, OperationOptionsHandle options, DeserializerCallbacks callback, byte[] serializedBuffer, uint offset, ClassHandle[] classObjects, out InstanceHandle[] deserializedInstances, out uint inputBufferUsed, out InstanceHandle cimErrorDetails)
        {
            throw new NotImplementedException();
        }
    }

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

    internal class DestinationOptionsHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
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
    }

    internal class DestinationOptionsMethods
    {
        // Fields
        internal static string packetEncoding_Default;
        internal static string packetEncoding_UTF16;
        internal static string packetEncoding_UTF8;
        internal static string proxyType_Auto;
        internal static string proxyType_IE;
        internal static string proxyType_None;
        internal static string proxyType_WinHTTP;
        internal static string transport_Http;
        internal static string transport_Https;

        // Methods
        static DestinationOptionsMethods()
        {
            throw new NotImplementedException();
        }
        private DestinationOptionsMethods()
        {
            throw new NotImplementedException();
        }
        internal static MiResult AddDestinationCredentials(DestinationOptionsHandle destinationOptionsHandle, NativeCimCredentialHandle credentials)
        {
            throw new NotImplementedException();
        }
        internal static MiResult AddProxyCredentials(DestinationOptionsHandle destinationOptionsHandle, NativeCimCredentialHandle credentials)
        {
            throw new NotImplementedException();
        }
        internal static MiResult Clone(DestinationOptionsHandle destinationOptionsHandle, out DestinationOptionsHandle newDestinationOptionsHandle)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetCertCACheck(DestinationOptionsHandle destinationOptionsHandle, out bool check)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetCertCNCheck(DestinationOptionsHandle destinationOptionsHandle, out bool check)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetCertRevocationCheck(DestinationOptionsHandle destinationOptionsHandle, out bool check)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetDataLocale(DestinationOptionsHandle destinationOptionsHandle, out string locale)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetDestinationPort(DestinationOptionsHandle destinationOptionsHandle, out uint port)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetEncodePortInSPN(DestinationOptionsHandle destinationOptionsHandle, out bool encodePort)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetHttpUrlPrefix(DestinationOptionsHandle destinationOptionsHandle, out string prefix)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetImpersonationType(DestinationOptionsHandle destinationOptionsHandle, out MiImpersonationType impersonationType)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMaxEnvelopeSize(DestinationOptionsHandle destinationOptionsHandle, out uint sizeInKB)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetPacketEncoding(DestinationOptionsHandle destinationOptionsHandle, out string encoding)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetPacketIntegrity(DestinationOptionsHandle destinationOptionsHandle, out bool integrity)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetPacketPrivacy(DestinationOptionsHandle destinationOptionsHandle, out bool privacy)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetProxyType(DestinationOptionsHandle destinationOptionsHandle, out string proxyType)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetTimeout(DestinationOptionsHandle destinationOptionsHandle, out TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetTransport(DestinationOptionsHandle destinationOptionsHandle, out string transport)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetUILocale(DestinationOptionsHandle destinationOptionsHandle, out string locale)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetCertCACheck(DestinationOptionsHandle destinationOptionsHandle, [MarshalAs(UnmanagedType.U1)] bool check)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetCertCNCheck(DestinationOptionsHandle destinationOptionsHandle, [MarshalAs(UnmanagedType.U1)] bool check)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetCertRevocationCheck(DestinationOptionsHandle destinationOptionsHandle, [MarshalAs(UnmanagedType.U1)] bool check)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetCustomOption(DestinationOptionsHandle destinationOptionsHandle, string optionName, string optionValue)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetCustomOption(DestinationOptionsHandle destinationOptionsHandle, string optionName, uint optionValue)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetDataLocale(DestinationOptionsHandle destinationOptionsHandle, string locale)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetDestinationPort(DestinationOptionsHandle destinationOptionsHandle, uint port)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetEncodePortInSPN(DestinationOptionsHandle destinationOptionsHandle, [MarshalAs(UnmanagedType.U1)] bool encodePort)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetHttpUrlPrefix(DestinationOptionsHandle destinationOptionsHandle, string prefix)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetImpersonationType(DestinationOptionsHandle destinationOptionsHandle, MiImpersonationType impersonationType)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetMaxEnvelopeSize(DestinationOptionsHandle destinationOptionsHandle, uint sizeInKB)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetPacketEncoding(DestinationOptionsHandle destinationOptionsHandle, string encoding)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetPacketIntegrity(DestinationOptionsHandle destinationOptionsHandle, [MarshalAs(UnmanagedType.U1)] bool integrity)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetPacketPrivacy(DestinationOptionsHandle destinationOptionsHandle, [MarshalAs(UnmanagedType.U1)] bool privacy)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetProxyType(DestinationOptionsHandle destinationOptionsHandle, string proxyType)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetTimeout(DestinationOptionsHandle destinationOptionsHandle, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetTransport(DestinationOptionsHandle destinationOptionsHandle, string transport)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetUILocale(DestinationOptionsHandle destinationOptionsHandle, string locale)
        {
            throw new NotImplementedException();
        }

        // Nested Types
        internal enum MiImpersonationType
        {
            Default,
            None,
            Identify,
            Impersonate,
            Delegate
        }
    }

    internal abstract class ExceptionSafeCallbackBase
    {
        // Fields
        internal OperationCallbackProcessingContext callbackProcessingContext;
        //private OperationCallbacks.InternalErrorCallbackDelegate internalErrorCallback;
        //internal unsafe MI_OperationWrapper* pmiOperationWrapper;

        // Methods
        //protected unsafe ExceptionSafeCallbackBase(void* callbackContext);
        protected abstract void InvokeUserCallback();
        internal void InvokeUserCallbackAndCatchInternalErrors()
        {
            throw new NotImplementedException();
        }
        //[return: MarshalAs(UnmanagedType.U1)]
        //private bool IsInternalException(Exception e);
        //private void ReportInternalError(Exception exception);
    }

    internal class ExceptionSafeClassCallback : ExceptionSafeCallbackBase
    {
        // Fields
        //private unsafe ushort modopt(IsConst)* errorString;
        //private byte moreResults;
        //private unsafe _MI_Class modopt(IsConst)* pmiClass;
        //private unsafe _MI_Instance modopt(IsConst)* pmiErrorDetails;
        //private unsafe _MI_Operation* pmiOperation;
        //private _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement;
        //private _MI_Result resultCode;

        // Methods
        //internal unsafe ExceptionSafeClassCallback(_MI_Operation* pmiOperation, void* callbackContext, _MI_Class modopt(IsConst)* pmiClass, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);
        protected override void InvokeUserCallback()
        {
            throw new NotImplementedException();
        }
    }

    internal class ExceptionSafeIndicationCallback : ExceptionSafeCallbackBase
    {
        // Fields
        //private unsafe ushort modopt(IsConst)* bookmark;
        //private unsafe ushort modopt(IsConst)* errorString;
        //private unsafe ushort modopt(IsConst)* machineID;
        //private byte moreResults;
        //private unsafe _MI_Instance modopt(IsConst)* pmiErrorDetails;
        //private unsafe _MI_Instance modopt(IsConst)* pmiInstance;
        //private unsafe _MI_Operation* pmiOperation;
        //private _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement;
        //private _MI_Result resultCode;

        // Methods
        //internal unsafe ExceptionSafeIndicationCallback(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance modopt(IsConst)* pmiInstance, ushort modopt(IsConst)* bookmark, ushort modopt(IsConst)* machineID, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);
        protected override void InvokeUserCallback()
        {
            throw new NotImplementedException();
        }
    }

    internal class ExceptionSafeInstanceResultCallback : ExceptionSafeCallbackBase
    {
        // Fields
        //private unsafe ushort modopt(IsConst)* errorString;
        //private byte moreResults;
        //private unsafe _MI_Instance modopt(IsConst)* pmiErrorDetails;
        //private unsafe _MI_Instance modopt(IsConst)* pmiInstance;
        //private unsafe _MI_Operation* pmiOperation;
        //private _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement;
        //private _MI_Result resultCode;

        // Methods
        //internal unsafe ExceptionSafeInstanceResultCallback(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance modopt(IsConst)* pmiInstance, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);
        protected override void InvokeUserCallback()
        {
            throw new NotImplementedException();
        }
    }

    internal class ExceptionSafePromptUserCallback : ExceptionSafeCallbackBase
    {
        // Fields
        //private unsafe _MI_Operation* pmiOperation;
        //private _MI_PromptType promptType;
        //private _MI_Result modopt(CallConvCdecl) *(_MI_Operation*, _MI_OperationCallback_ResponseType) promptUserResult;
        //private unsafe ushort modopt(IsConst)* wszMessage;

        // Methods
        //internal unsafe ExceptionSafePromptUserCallback(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszMessage, _MI_PromptType promptType, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*, _MI_OperationCallback_ResponseType) promptUserResult);
        protected override void InvokeUserCallback()
        {
            throw new NotImplementedException();
        }
    }

    internal class ExceptionSafeStreamedParameterResultCallback : ExceptionSafeCallbackBase
    {
        // Fields
        //private _MiType miType;
        //private unsafe _MI_Operation* pmiOperation;
        //private unsafe _MiValue modopt(IsConst)* pmiParameterValue;
        //private _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement;
        //private unsafe ushort modopt(IsConst)* wszParameterName;

        // Methods
        //internal unsafe ExceptionSafeStreamedParameterResultCallback(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszParameterName, _MiType miType, _MiValue modopt(IsConst)* pmiParameterValue, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);
        protected override void InvokeUserCallback()
        {
            throw new NotImplementedException();
        }
    }

    internal class ExceptionSafeWriteErrorCallback : ExceptionSafeCallbackBase
    {
        // Fields
        //private unsafe _MI_Instance* pmiInstance;
        //private unsafe _MI_Operation* pmiOperation;
        //private _MI_Result modopt(CallConvCdecl) *(_MI_Operation*, _MI_OperationCallback_ResponseType) writeErrorResult;

        // Methods
        //internal unsafe ExceptionSafeWriteErrorCallback(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance* pmiInstance, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*, _MI_OperationCallback_ResponseType) writeErrorResult);
        protected override void InvokeUserCallback()
        {
            throw new NotImplementedException();
        }
    }

    internal class ExceptionSafeWriteMessageCallback : ExceptionSafeCallbackBase
    {
        // Fields
        //private uint channel;
        //private unsafe _MI_Operation* pmiOperation;
        //private unsafe ushort modopt(IsConst)* wszMessage;

        // Methods
        //internal unsafe ExceptionSafeWriteMessageCallback(_MI_Operation* pmiOperation, void* callbackContext, uint channel, ushort modopt(IsConst)* wszMessage);
        protected override void InvokeUserCallback()
        {
            throw new NotImplementedException();
        }
    }

    internal class ExceptionSafeWriteProgressCallback : ExceptionSafeCallbackBase
    {
        // Fields
        //private uint percentageComplete;
        //private unsafe _MI_Operation* pmiOperation;
        //private uint secondsRemaining;
        //private unsafe ushort modopt(IsConst)* wszActivity;
        //private unsafe ushort modopt(IsConst)* wszCurrentOperation;
        //private unsafe ushort modopt(IsConst)* wszStatusDescription;

        // Methods
        //internal unsafe ExceptionSafeWriteProgressCallback(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszActivity, ushort modopt(IsConst)* wszCurrentOperation, ushort modopt(IsConst)* wszStatusDescription, uint percentageComplete, uint secondsRemaining);
        protected override void InvokeUserCallback()
        {
            throw new NotImplementedException();
        }
    }

    internal class Helpers
    {
        // Methods
        private Helpers()
        {
            throw new NotImplementedException();
        }
        internal static IntPtr GetCurrentSecurityToken()
        {
            throw new NotImplementedException();
        }
        internal static IntPtr StringToHGlobalUni(string s)
        {
            throw new NotImplementedException();
        }
        internal static void ZeroFreeGlobalAllocUnicode(IntPtr s)
        {
            throw new NotImplementedException();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class InstanceHandle// : SafeHandleZeroOrMinusOneIsInvalid
    {
        public NativeObject.MI_Instance miInstance;
        public IntPtr handle;
        //public static WeakReference _reference;

        public NativeObject.MI_Instance mmiInstance;
        // construct a reference to NativeObject.MI_Instance
        internal InstanceHandle()
        {
            this.mmiInstance = NativeObject.MI_Instance.NewDirectPtr();
            //this.miInstance = new NativeObject.MI_Instance();
            //InstanceHandle._reference = new WeakReference(this.miInstance);
        }

        internal InstanceHandle(InstanceHandle handle)
        {
                this.handle = handle.mmiInstance.Ptr;
                this.mmiInstance = handle.mmiInstance;
        }

        //Stores a pointer to the instance, as well as dereferences the pointer to store the struct
        internal InstanceHandle(NativeObject.MI_Instance handle)
        {
            this.handle = handle.Ptr;
            this.mmiInstance = handle;
            //this.handle = handle;
            //this.miInstance = Marshal.PtrToStructure<NativeObject.MI_Instance>(handle);
            //this.= Marshal.PtrToStructure<NativeObject.MI_Instanc.(this.miInstance.;
            //InstanceHandle._reference = new WeakReference(this.handle);
        }

        public void AssertValidInternalState()
        {
            System.Diagnostics.Debug.Assert(this.handle != IntPtr.Zero, "InstanceHandle.AssertValidInternalState: this.handle != IntPtr.Zero");
        }

        //~InstanceHandle()
        //{
        //    Dispose(false);
        //    GC.SuppressFinalize(this);
        //}
        public void Dispose()
        {
        //    Dispose(true);
        //    GC.SuppressFinalize(this);
        }

        //private bool _isDisposed = false;
        //protected void Dispose(bool disposing)
        //{
        //    MiResult r;
        //    try
        //    {
        //      r = this.Delete(ref this.miInstance); // Release instances created on the heap
        //    }
        //    catch (NullReferenceException)
        //    {
        //        // Object already disposed, nothing to do
        //        _isDisposed = true;
        //        return;
        //    }
        //    Debug.Assert( MiResult.OK == r, "MI Client .NET API should guarantee that calls to MI_Instance_Delete always return MI_RESULT_OK");
        //    _isDisposed = true;
        //}

        //internal delegate void OnHandleReleasedDelegate();
        //internal MiResult ReleaseHandleAsynchronously(OnHandleReleasedDelegate completionCallback)
        //{
        //    throw new NotImplementedException();
        //}
        internal MiResult ReleaseHandleSynchronously()
        {
              throw new NotImplementedException();
        }

        //// should be part of SafeHandleZeroOrMinusOneIsInvalid
        public bool IsInvalid { get; }
    }

    internal class InstanceMethods
    {
        // Fields
        //internal static ValueType modopt(DateTime) modopt(IsBoxed) maxValidCimTimestamp;
        public static DateTime maxValidCimTimestamp = new DateTime(9999, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc);

        // Methods
        static InstanceMethods()
        {
        }
        internal InstanceMethods()
        {
        }
        ///
        /// takes an MI Instance, and adds to it a string representing the name of the instance, the MiValue representing the data value type,
        /// a type flag, and an MI Flag
        internal static MiResult AddElement(InstanceHandle instance, string name, object managedValue, MiType type, MiFlags flags)
        {
            NativeObject.MI_Value val;
            ConvertToMiValue((MI_Type)type, managedValue, out val);

            NativeObject.MI_Type nativeType = (MI_Type)(uint)type;
            NativeObject.MI_Flags nativeFlags = (MI_Flags)(uint)flags;
            MiResult r = (MiResult)(uint)instance.mmiInstance.AddElement(name, val, nativeType, nativeFlags);

            return r;
        }
        internal static MiResult ClearElementAt(InstanceHandle instance, int index)
        {
            MiResult r = (MiResult)(uint)instance.mmiInstance.ClearElementAt( (uint)index);

            return r;
        }
        internal static MiResult Clone(InstanceHandle instanceHandleToClone, out InstanceHandle clonedInstanceHandle)
        {
            Debug.Assert(instanceHandleToClone != null, "Caller should verify that instanceHandleToClone != null");
            clonedInstanceHandle = null;
            NativeObject.MI_Instance ptrClonedInstance = NativeObject.MI_Instance.NewIndirectPtr();

            MiResult r = (MiResult)(uint)instanceHandleToClone.mmiInstance.Clone(out ptrClonedInstance);
            if ( r == MiResult.OK)
            {
                clonedInstanceHandle = new InstanceHandle(ptrClonedInstance);
            }

            return r;
        }

        // Convert from an MI_Value to a .Net object
        internal static object ConvertFromMiValue(MI_Type type, MI_Value miValue)
        {
            object val = null;
            switch (type.ToString())
            {
                case "MI_BOOLEAN":
                    val = (bool)miValue.Boolean;
                    break;
                case "MI_BOOLEANA":
                    val = (bool[])miValue.BooleanA;
                    break;
                case "MI_SINT64":
                    val = (Int64)miValue.Sint64;
                    break;
                case "MI_SINT64A":
                    val = (Int64[])miValue.Sint64A;
                    break;
                case "MI_SINT32":
                    val = (Int32)miValue.Sint32;
                    break;
                case "MI_SINT32A":
                    val = (Int32[])miValue.Sint32A;
                    break;
                case "MI_SINT16":
                    val = (Int16)miValue.Sint16;
                    break;
                case "MI_SINT16A":
                    val = (Int16[])miValue.Sint16A;
                    break;
                case "MI_SINT8":
                    val = (sbyte)miValue.Sint8;
                    break;
                case "MI_SINT8A":
                    val = (sbyte[])miValue.Sint8A;
                    break;
                case "MI_UINT64":
                    val = (UInt64)miValue.Uint64;
                    break;
                case "MI_UINT64A":
                    val = (UInt64[])miValue.Uint64A;
                    break;
                case "MI_UINT32":
                    val = (UInt32)miValue.Uint32;
                    break;
                case "MI_UINT32A":
                    val = (UInt32[])miValue.Uint32A;
                    break;
                case "MI_UINT16":
                    val = (byte)miValue.Uint16;
                    break;
                case "MI_UINT16A":
                    val = (ushort[])miValue.Uint16A;
                    break;
                case "MI_UINT8":
                    val = (byte)miValue.Uint8;
                    break;
                case "MI_UINT8A":
                    val = (byte[])miValue.Uint8A;
                    break;
                case "MI_REAL32":
                    val = (float)miValue.Real32;
                    break;
                case "MI_REAL32A":
                    val = (float[])miValue.Real32A;
                    break;
                case "MI_REAL64":
                    val = (double)miValue.Real64;
                    break;
                case "MI_REAL64A":
                    val = (double[])miValue.Real64A;
                    break;
                case "MI_CHAR16":
                    val  = (char)miValue.Char16;
                    break;
                case "MI_CHAR16A":
                    val = (char[])miValue.Char16A;
                    break;
                case "MI_STRING":
                    val  = (string)miValue.String;
                    break;
                case "MI_STRINGA":
                    val = (string[])miValue.StringA;
                    break;
                case "MI_DATETIME":
                    val = MiDateTimeToManagedObjectDateTime(miValue.Datetime);
                    break;
                case "MI_INSTANCE":
                case "MI_REFERENCE":
                    val = new InstanceHandle(miValue.Instance);
                    break;
                case "MI_DATETIMEA":
                    List<object> dateTimes = new List<object>();

                    foreach (var dt in miValue.DatetimeA)
                    {
                          dateTimes.Add( MiDateTimeToManagedObjectDateTime(dt) );
                    }

                    val = dateTimes.ToArray();
                    break;
                case "MI_REFERENCEA":
                case "MI_INSTANCEA":
                    List<InstanceHandle> instances = new List<InstanceHandle>();

                    foreach (var inst in miValue.InstanceA)
                    {
                        instances.Add( new InstanceHandle(miValue.Instance) );
                    }

                    val = instances.ToArray();
                    break;
                default:
                    Console.WriteLine("ERROR: MI_Value {0} is an unknown MI_Value " + type.ToString());
                    break;
            }
            return val;
        }

        internal static void ConvertArrayToMiValue(MI_Type type, object managedValue, out MI_Value miValue)
        {
            miValue = MI_Value.NewDirectPtr();

            byte[] a = managedValue as byte[]; //TODO: what if this is a boolean?
            if (a != null)
            {
                miValue.Uint8A = a;
                return;
            }

            ushort[] b = managedValue as ushort[];
            if (b != null)
            {
                miValue.Uint16A = b;
                return;
            }

            UInt32[] c = managedValue as UInt32[];
            if (c != null)
            {
                miValue.Uint32A = c;
                return;
            }

            UInt64[] d = managedValue as UInt64[];
            if (d != null)
            {
                miValue.Uint64A = d;
                return;
            }

            sbyte[] e = managedValue as sbyte[];
            if (e != null)
            {
                miValue.Sint8A = e;
                return;
            }

            Int16[] f = managedValue as Int16[];
            if (f != null)
            {
                miValue.Sint16A = f;
                return;
            }

            Int32[] g = managedValue as Int32[];
            if (g != null)
            {
                miValue.Sint32A = g;
                return;
            }

            Int64[] h = managedValue as Int64[];
            if (h != null)
            {
                miValue.Sint64A = h;
                return;
            }

            bool[] i = managedValue as bool[];
            if (i != null)
            {
                miValue.BooleanA = i;
                return;
            }

            double[] j = managedValue as double[];
            if (j != null)
            {
                miValue.Real64A = j;
                return;
            }

            float[] k = managedValue as float[];
            if (k != null)
            {
                miValue.Real32A = k;
                return;
            }

            char[] l = managedValue as char[];
            if (l != null)
            {
                miValue.Char16A = l;
                return;
            }

            string[] m = managedValue as string[];
            if (m != null)
            {
                miValue.StringA = m;
                return;
            }

            System.DateTime[] n = managedValue as System.DateTime[];
            if (n != null)
            {
                MI_Datetime[] dateTimeArray = new MI_Datetime[n.Length];

                for ( int index = 0; index < n.Length; index++ )
                {
                    dateTimeArray[index] = ConvertManagedObjectToMiDateTime(n[index]);
                }
                miValue.DatetimeA = dateTimeArray;
                return;
            }

            InstanceHandle[] o = managedValue as InstanceHandle[];
            if ( o != null )
            {
                NativeObject.MI_Instance[] instanceArray = new NativeObject.MI_Instance[o.Length];

                for ( int index = 0; index < o.Length; index++ )
                {

                    instanceArray[index] = CopyManagedItemToMiInstance(o[index]);
                }
                miValue.InstanceA = instanceArray;
                return;
            }
        }

        // Converts from an object to something that can be consumed by the native MI engine.
        internal static void ConvertToMiValue(MI_Type type, object managedValue, out NativeObject.MI_Value miValue)
        {
            miValue = MI_Value.NewDirectPtr();
            switch (type.ToString())
            {
                case "MI_BOOLEAN":
                    miValue.Boolean = Convert.ToBoolean(managedValue);
                    break;
                case "MI_SINT8":
                    miValue.Sint8 = Convert.ToSByte(managedValue);
                    break;
                case "MI_SINT16":
                    miValue.Sint16 = Convert.ToInt16(managedValue);
                    break;
                case "MI_SINT32":
                    miValue.Sint32 = Convert.ToInt32(managedValue);
                    break;
                case "MI_SINT64":
                    miValue.Sint64 = Convert.ToInt64(managedValue);
                    break;
                case "MI_UINT8":
                    miValue.Uint8 = Convert.ToByte(managedValue);
                    break;
                case "MI_UINT16":
                    miValue.Uint16 = Convert.ToUInt16(managedValue);
                    break;
                case "MI_UINT32":
                    miValue.Uint32 = Convert.ToUInt32(managedValue);
                    break;
                case "MI_UINT64":
                    miValue.Uint64 = Convert.ToUInt64(managedValue);
                    break;
                case "MI_REAL32":
                    miValue.Real32 = Convert.ToSingle(managedValue);
                    break;
                case "MI_REAL64":
                    miValue.Real64 = Convert.ToDouble(managedValue);
                    break;
                case "MI_CHAR16":
                    miValue.Char16 = Convert.ToChar(managedValue);
                    break;
                case "MI_STRING":
                    miValue.String = Convert.ToString(managedValue);
                    break;
                case "MI_DATETIME":
                    miValue.Datetime = ConvertManagedObjectToMiDateTime(managedValue);
                    break;
                case "MI_REFERENCE":
                case "MI_INSTANCE":
                    miValue.Instance = CopyManagedItemToMiInstance(managedValue);
                    break;
                case "MI_BOOLEANA":
                case "MI_SINT8A":
                case "MI_SINT16A":
                case "MI_SINT32A":
                case "MI_SINT64A":
                case "MI_UINT8A":
                case "MI_UINT16A":
                case "MI_UINT64A":
                case "MI_UINT32A":
                case "MI_REAL32A":
                case "MI_REAL64A":
                case "MI_CHAR16A":
                case "MI_STRINGA":
                case "MI_DATETIMEA":
                case "MI_INSTANCEA":
                case "MI_REFERENCEA":
                    ConvertArrayToMiValue(type, managedValue, out miValue);
                    break;
                default:
                    Console.WriteLine("ERROR: unknown MI_Type type.  Type {0} is unknown", type.ToString());
                    break;

            }
        }

        /// Recieves an instanceHandle, creates a pointer to that instancehandle.
        internal static NativeObject.MI_Instance CopyManagedItemToMiInstance(object managedValue)
        {
            InstanceHandle oldHandle = managedValue as InstanceHandle;

            if (oldHandle != null)
            {
                InstanceHandle newHandle = new InstanceHandle(oldHandle.mmiInstance);

                return newHandle.mmiInstance;
            }

            return null;
        }
        // Converts MI_DateTime to System.DateTime
        internal static object MiDateTimeToManagedObjectDateTime(MI_Datetime miDatetime)
        {
            if (miDatetime.isTimestamp)
            {
                // "Now" value defined in line 1934, page 53 of DSP0004, version 2.6.0
                if ((miDatetime.timestamp.year == 0) &&
                    (miDatetime.timestamp.month == 0) &&
                    (miDatetime.timestamp.day == 1) &&
                    (miDatetime.timestamp.hour == 0) &&
                    (miDatetime.timestamp.minute == 0) &&
                    (miDatetime.timestamp.second == 0) &&
                    (miDatetime.timestamp.microseconds == 0) &&
                    (miDatetime.timestamp.utc == 720))
                {
                    return DateTime.Now;
                }
                // "Infinite past" value defined in line 1935, page 54 of DSP0004, version 2.6.0
                else if ((miDatetime.timestamp.year == 0) &&
                         (miDatetime.timestamp.month == 1) &&
                         (miDatetime.timestamp.day == 1) &&
                         (miDatetime.timestamp.hour == 0) &&
                         (miDatetime.timestamp.minute == 0) &&
                         (miDatetime.timestamp.second == 0) &&
                         (miDatetime.timestamp.microseconds == 999999) &&
                         (miDatetime.timestamp.utc == 720))
                {
                    return DateTime.MinValue;
                }
                // "Infinite future" value defined in line 1936, page 54 of DSP0004, version 2.6.0
                else if ((miDatetime.timestamp.year == 9999) &&
                         (miDatetime.timestamp.month == 12) &&
                         (miDatetime.timestamp.day == 31) &&
                         (miDatetime.timestamp.hour == 11) &&
                         (miDatetime.timestamp.minute == 59) &&
                         (miDatetime.timestamp.second == 59) &&
                         (miDatetime.timestamp.microseconds == 999999) &&
                         (miDatetime.timestamp.utc == 720))
                {
                    return DateTime.MaxValue;
                }
                else
                {
                    //If CoreCLR
                    Calendar myCalendar = CultureInfo.InvariantCulture.Calendar;

                    DateTime managedDateTime = myCalendar.ToDateTime(
                                                            (int)miDatetime.timestamp.year,
                                                            (int)miDatetime.timestamp.month,
                                                            (int)miDatetime.timestamp.day,
                                                            (int)miDatetime.timestamp.hour,
                                                            (int)miDatetime.timestamp.minute,
                                                            (int)miDatetime.timestamp.second,
                                                            (int)miDatetime.timestamp.microseconds / 1000);
                    DateTime managedUtcDateTime = DateTime.SpecifyKind(managedDateTime, DateTimeKind.Utc); //TODO: C++/cli uses myDateTime.SpecifyKind(), which fails here.
                    // ^^CoreCLR
                    long microsecondsUnaccounted = miDatetime.timestamp.microseconds % 1000;
                    managedUtcDateTime = managedUtcDateTime.AddTicks(microsecondsUnaccounted * 10); // since 1 microsecond == 10 ticks
                    managedUtcDateTime = managedUtcDateTime.AddMinutes(-(miDatetime.timestamp.utc));

                    DateTime managedLocalDateTime = TimeZoneInfo.ConvertTime(managedUtcDateTime,TimeZoneInfo.Local);
                    return managedLocalDateTime;
                }
           }
           else
           {
               if ( TimeSpan.MaxValue.TotalDays < miDatetime.interval.days )
               {
                   return TimeSpan.MaxValue;
               }

               try
               {
                   TimeSpan managedTimeSpan = new TimeSpan(
                                                    (int)miDatetime.interval.days,
                                                    (int)miDatetime.interval.hours,
                                                    (int)miDatetime.interval.minutes,
                                                    (int)miDatetime.interval.seconds,
                                                    (int)miDatetime.interval.microseconds / 1000);
                   long microsecondsUnaccounted = miDatetime.interval.microseconds % 1000;
                   TimeSpan ticksUnaccountedTimeSpan = new TimeSpan(microsecondsUnaccounted * 10); // since 1 microsecond == 10 ticks
                   TimeSpan correctedTimeSpan = managedTimeSpan.Add(ticksUnaccountedTimeSpan);

                   DateTime dt = new DateTime();
                   DateTime managedDateTime = dt.AddDays(correctedTimeSpan.Days)
                                                .AddHours(correctedTimeSpan.Hours)
                                                .AddMinutes(correctedTimeSpan.Minutes)
                                                .AddSeconds(correctedTimeSpan.Seconds)
                                                .AddMilliseconds(correctedTimeSpan.Milliseconds)
                                                .AddTicks(microsecondsUnaccounted * 10);

                   DateTime returnDate = DateTime.SpecifyKind(managedDateTime, DateTimeKind.Unspecified);
                   return returnDate;
               }
               catch (ArgumentOutOfRangeException)
               {
                       return TimeSpan.MaxValue;
               }
           }
        }
        // Converts System.DateTime to MI_DateTime
        internal static MI_Datetime ConvertManagedObjectToMiDateTime(object managedValue)
        {
            Debug.Assert(managedValue != null, "Caller should verify managedValue != null");
            NativeObject.MI_Datetime miDatetime = new NativeObject.MI_Datetime();

                //long ticks = dt.Ticks;
                //TimeSpan timeSpan = new TimeSpan(ticks);

                if (managedValue is TimeSpan)
                {
                    System.TimeSpan timeSpan = (TimeSpan)managedValue;
                    if (timeSpan.Equals(TimeSpan.MaxValue))
                    {
                        // "Infinite duration" value defined in line 1944, page 54 of DSP0004, version 2.6.0
                        miDatetime.interval.days         = 99999999;
                        miDatetime.interval.hours        = 23;
                        miDatetime.interval.minutes      = 59;
                        miDatetime.interval.seconds      = 59;
                        miDatetime.interval.microseconds = 0;
                    }
                    else
                    {
                        long ticksUnaccounted = timeSpan.Ticks%10000; // since 10000 ticks == 1 millisecond

                        miDatetime.interval.days         = (uint)timeSpan.Days;
                        miDatetime.interval.hours        = (uint)timeSpan.Hours;
                        miDatetime.interval.minutes      = (uint)timeSpan.Minutes;
                        miDatetime.interval.seconds      = (uint)timeSpan.Seconds;
                        miDatetime.interval.microseconds = (uint)(timeSpan.Milliseconds * 1000 + ticksUnaccounted/10); // since 1 tick == 0.1 microsecond
                    }

                    miDatetime.isTimestamp = false;
                }
                else
                {
                    // TimeStamp is null.  check that datetime isn't max
                //    System.DateTime dateTime = Convert.ToDateTime(managedValue); //TODO: do we *really* need this?
                    System.DateTime dateTime = (DateTime)managedValue;

                    if (dateTime.Equals(DateTime.MaxValue))
                    {
                        // "Infinite future" value defined in line 1936, page 54 of DSP0004, version 2.6.0
                        miDatetime.timestamp.year         = 9999;
                        miDatetime.timestamp.month        = 12;
                        miDatetime.timestamp.day          = 31;
                        miDatetime.timestamp.hour         = 11;
                        miDatetime.timestamp.minute       = 59;
                        miDatetime.timestamp.second       = 59;
                        miDatetime.timestamp.microseconds = 999999;
                        miDatetime.timestamp.utc          = (-720);
                    }
                    else if (dateTime.Equals(DateTime.MinValue))
                    {
                        // "Infinite past" value defined in line 1935, page 54 of DSP0004, version 2.6.0
                        miDatetime.timestamp.year         = 0;
                        miDatetime.timestamp.month        = 1;
                        miDatetime.timestamp.day          = 1;
                        miDatetime.timestamp.hour         = 0;
                        miDatetime.timestamp.minute       = 0;
                        miDatetime.timestamp.second       = 0;
                        miDatetime.timestamp.microseconds = 999999;
                        miDatetime.timestamp.utc          = 720;
                    }
                    else if (DateTime.Compare(maxValidCimTimestamp, dateTime) <= 0)
                    {
                        // "Youngest useable timestamp" value defined in line 1930, page 53 of DSP0004, version 2.6.0
                        miDatetime.timestamp.year         = 9999;
                        miDatetime.timestamp.month        = 12;
                        miDatetime.timestamp.day          = 31;
                        miDatetime.timestamp.hour         = 11;
                        miDatetime.timestamp.minute       = 59;
                        miDatetime.timestamp.second       = 59;
                        miDatetime.timestamp.microseconds = 999998;
                        miDatetime.timestamp.utc          = (-720);
                    }
                    else
                    {
                        dateTime = TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Utc);
                        long ticksUnaccounted = dateTime.Ticks%10000;

                        miDatetime.timestamp.year         = (uint)dateTime.Year;
                        miDatetime.timestamp.month        = (uint)dateTime.Month;
                        miDatetime.timestamp.day          = (uint)dateTime.Day;
                        miDatetime.timestamp.hour         = (uint)dateTime.Hour;
                        miDatetime.timestamp.minute       = (uint)dateTime.Minute;
                        miDatetime.timestamp.second       = (uint)dateTime.Second;
                        miDatetime.timestamp.microseconds = (uint)dateTime.Millisecond * 1000 + (uint)ticksUnaccounted/10;
                        miDatetime.timestamp.utc          = 0;

                    }

                    miDatetime.isTimestamp = true;
                }
            return miDatetime;
        }

        //internal static unsafe void ConvertManagedObjectToMiDateTime(object managedValue, _MI_Datetime* pmiValue);
        //internal static unsafe object ConvertMiDateTimeToManagedObject(_MI_Datetime modopt(IsConst)* pmiValue);
        //internal static unsafe IEnumerable<DangerousHandleAccessor> ConvertToMiValue(MiType type, object managedValue, _MiValue* pmiValue);

        internal static MiResult GetClass(InstanceHandle instanceHandle, out ClassHandle classHandle)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetClassName(InstanceHandle instance, out string className)
        {
            MiResult r = (MiResult)(uint)instance.mmiInstance.GetClassName(out className);
            return r;
        }
        internal static MiResult GetElement_GetIndex(InstanceHandle instance, string name, out int index)
        {
            uint i;
            NativeObject.MI_Value v = null;
            MI_Type t;
            MI_Flags f = 0; //flags
            MiResult r = (MiResult)(uint)instance.mmiInstance.GetElement( name, out v, out t, out f, out i);
            index = (int)i;
            return r;
        }
        internal static MiResult GetElementAt_GetFlags(InstanceHandle instance, int index, out MiFlags flags)
        {
            Debug.Assert(instance.handle != IntPtr.Zero, "Caller should verify that instance is not null");
            Debug.Assert(index >= 0, "Caller should verify that index >=0");

            NativeObject.MI_Value val = MI_Value.NewDirectPtr();
            MI_Type nativeType;
            MI_Flags nativeFlags; //flags
            string name;

            MiResult r = (MiResult)(uint)instance.mmiInstance.GetElementAt( (uint)index, out name, out val, out nativeType, out nativeFlags);
            flags = (MiFlags)nativeFlags;
            return r;
        }
        internal static MiResult GetElementAt_GetName(InstanceHandle instance, int index, out string name)
        {
            NativeObject.MI_Value val = MI_Value.NewDirectPtr();
            MI_Type type;
            MI_Flags flags; //flags

            MiResult r = (MiResult)(uint)instance.mmiInstance.GetElementAt( (uint)index, out name, out val, out type, out flags);

            return r;
        }
        internal static MiResult GetElementAt_GetType(InstanceHandle instance, int index, out MiType type)
        {
            NativeObject.MI_Value val = MI_Value.NewDirectPtr();
            MI_Type nativeType;
            MI_Flags nativeFlags; //flags
            string name;

            MiResult r = (MiResult)(uint)instance.mmiInstance.GetElementAt( (uint)index, out name, out val, out nativeType, out nativeFlags);

            type = (MiType)nativeType;
            return r;
        }

        internal static MiResult GetElementAt_GetValue(InstanceHandle instance, int index, out object val)
        {
            Debug.Assert(0 <= index, "Caller should verify index > 0");
            string name;
            MI_Value miValue = MI_Value.NewDirectPtr();
            MI_Type type;
            MI_Flags flags;

            //IntPtr name;
            //IntPtr pVal= Marshal.AllocHGlobal(Marshal.SizeOf<MiValue>());
            //MiType type;
            //MiFlags flags;
            val = null;


            MiResult r = (MiResult)(uint)instance.mmiInstance.GetElementAt( (uint)index, out name, out miValue, out type, out flags);
            if (r == MiResult.OK && miValue != null)
            {
                if (!flags.HasFlag(MI_Flags.MI_FLAG_NULL))
                {
                    // ConvertToManaged
                    val = ConvertFromMiValue(type, miValue);
                }
                else
                {
                    val = null;
                }
            }

            return r;
        }
        internal static MiResult GetElementCount(InstanceHandle instance, out int elementCount)
        {
           uint count;
           MiResult r = (MiResult)(uint)instance.mmiInstance.GetElementCount( out count);
           elementCount = Convert.ToInt32(count);
           return r;
        }
        internal static MiResult GetNamespace(InstanceHandle instance, out string nameSpace)
        {
           MiResult r = (MiResult)(uint)instance.mmiInstance.GetNameSpace( out nameSpace);
           return r;
        }
        internal static MiResult GetServerName(InstanceHandle instance, out string serverName)
        {
           MiResult r = (MiResult)(uint)instance.mmiInstance.GetServerName(out serverName);
           return r;
        }
        //internal static unsafe void ReleaseMiValue(MiType type, _MiValue* pmiValue, IEnumerable<DangerousHandleAccessor> dangerousHandleAccessors);
        /*
         * This function should output a cleansed MiValue struct.  Structs are non-nullable types in C#, so
         * cannot point to null
         */
        internal static void ReleaseMiValue(MiType type, ref MiValue miValue)
        {
            miValue = new MiValue();
        }

        internal static MiResult SetElementAt_SetNotModifiedFlag(InstanceHandle handle, int index, [MarshalAs(UnmanagedType.U1)] bool notModifiedFlag)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetElementAt_SetValue(InstanceHandle instance, int index, object newValue)
        {
                throw new NotImplementedException();
                //MiResult r;
                //MiValue v = default(MiValue);
            //IntPtr t;
            //IntPtr f;
            //IntPtr noUse1;
            //// GetElementAt
            //r = instance.GetElementAt(instance.handle, (uint)index, out noUse1, out v, out t, out f);
            //if (MiResult.OK != r)
            //        return r;

            //// SetElementAt
            //r = instance.SetElementAt(ref instance.handle, (uint)index, v, (MiType)t, 0);
            //return r;
            // No need to release MiValues here as they do in managed C++ implementation.  MiValue is a struct and thus a value type.
        }
        internal static MiResult SetNamespace(InstanceHandle instance, string nameSpace)
        {
            return (MiResult)(uint)instance.mmiInstance.SetNameSpace( nameSpace);
        }

        internal static MiResult SetServerName(InstanceHandle instance, string serverName)
        {
            return (MiResult)(uint)instance.mmiInstance.SetServerName( serverName);
        }
        internal static void ThrowIfMismatchedType(MiType type, object managedValue)
        {
        // TODO: Strings are treated similar to primitive data types in C#.  They will be cleared out the same way. Only need to deal with the
        //        complex data types

        }
    }

    internal enum MiCallbackMode
    {
        CALLBACK_REPORT,
        CALLBACK_INQUIRE,
        CALLBACK_IGNORE
    }

    internal enum MiCancellationReason
    {
        None,
        Timeout,
        Shutdown,
        ServiceStop
    }

    [Flags]
    internal enum MiFlags : uint
    {
        ABSTRACT = 0x20000,
        ADOPT = 0x80000000,
        ANY = 0x7f,
        ASSOCIATION = 0x10,
        BORROW = 0x40000000,
        CLASS = 1,
        DISABLEOVERRIDE = 0x100,
        ENABLEOVERRIDE = 0x80,
        EXPENSIVE = 0x80000,
        IN = 0x2000,
        INDICATION = 0x20,
        KEY = 0x1000,
        METHOD = 2,
        NOTMODIFIED = 0x2000000,
        NULLFLAG = 0x20000000,
        OUT = 0x4000,
        PARAMETER = 8,
        PROPERTY = 4,
        READONLY = 0x200000,
        REFERENCE = 0x40,
        REQUIRED = 0x8000,
        RESTRICTED = 0x200,
        STATIC = 0x10000,
        STREAM = 0x100000,
        TERMINAL = 0x40000,
        TOSUBCLASS = 0x400,
        TRANSLATABLE = 0x800
    }

    [Flags]
    internal enum MiOperationFlags : uint
    {
        BasicRtti = 2,
        ExpensiveProperties = 0x40,
        FullRtti = 4,
        LocalizedQualifiers = 8,
        ManualAckResults = 1,
        NoRtti = 0x400,
        PolymorphismDeepBasePropsOnly = 0x180,
        PolymorphismShallow = 0x80,
        ReportOperationStarted = 0x200,
        StandardRtti = 0x800
    }

    internal enum MiPromptType
    {
        PROMPTTYPE_NORMAL,
        PROMPTTYPE_CRITICAL
    }

    internal enum MIResponseType
    {
        MIResponseTypeNo,
        MIResponseTypeYes,
        MIResponseTypeNoToAll,
        MIResponseTypeYesToAll
    }

    internal enum MiResult
    {
        ACCESS_DENIED = 2,
        ALREADY_EXISTS = 11,
        CLASS_HAS_CHILDREN = 8,
        CLASS_HAS_INSTANCES = 9,
        CONTINUATION_ON_ERROR_NOT_SUPPORTED = 0x1a,
        FAILED = 1,
        FILTERED_ENUMERATION_NOT_SUPPORTED = 0x19,
        INVALID_CLASS = 5,
        INVALID_ENUMERATION_CONTEXT = 0x15,
        INVALID_NAMESPACE = 3,
        INVALID_OPERATION_TIMEOUT = 0x16,
        INVALID_PARAMETER = 4,
        INVALID_QUERY = 15,
        INVALID_SUPERCLASS = 10,
        METHOD_NOT_AVAILABLE = 0x10,
        METHOD_NOT_FOUND = 0x11,
        NAMESPACE_NOT_EMPTY = 20,
        NO_SUCH_PROPERTY = 12,
        NOT_FOUND = 6,
        NOT_SUPPORTED = 7,
        OK = 0,
        PULL_CANNOT_BE_ABANDONED = 0x18,
        PULL_HAS_BEEN_ABANDONED = 0x17,
        QUERY_LANGUAGE_NOT_SUPPORTED = 14,
        SERVER_IS_SHUTTING_DOWN = 0x1c,
        SERVER_LIMITS_EXCEEDED = 0x1b,
        TYPE_MISMATCH = 13
    }

    internal enum MiSubscriptionDeliveryType
    {
        SubscriptionDeliveryType_Pull = 1,
        SubscriptionDeliveryType_Push = 2
    }

    /*
    **==============================================================================
    **
    ** MI_Timestamp
    **
    **     Represents a timestamp as described in the CIM Infrastructure 
    **     specification
    **
    **     [1] MI_ee DSP0004 (http://www.dmtf.org/standards/published_documents)
    **
    **==============================================================================
    */

    [StructLayout(LayoutKind.Sequential)]
    public struct MI_Timestamp
    {
        /* YYYYMMDDHHMMSS.MMMMMMSUTC */
        public MI_Uint32 year;
        public MI_Uint32 month;
        public MI_Uint32 day;
        public MI_Uint32 hour;
        public MI_Uint32 minute;
        public MI_Uint32 second;
        public MI_Uint32 microseconds;  //4*7 = 28
        public MI_Sint32 utc;  //
    }

    /*
    **==============================================================================
    **
    ** struct MI_Interval
    **
    **     Represents an interval as described in the CIM Infrastructure 
    **     specification. This structure is padded to have the same length
    **     as a MI_Timestamp structure.
    **
    **     [1] MI_ee DSP0004 (http://www.dmtf.org/standards/published_documents)
    **
    **==============================================================================
    */

    [StructLayout(LayoutKind.Sequential)]
    public struct MI_Interval
    {
        /* DDDDDDDDHHMMSS.MMMMMM:000 */
        public MI_Uint32 days;
        public MI_Uint32 hours;
        public MI_Uint32 minutes;
        public MI_Uint32 seconds;
        public MI_Uint32 microseconds;
        public MI_Uint32 __padding1;
        public MI_Uint32 __padding2;
        public MI_Uint32 __padding3;
    }

    /*
    **==============================================================================
    **
    ** struct MI_Datetime
    **
    **     Represents a CIM datetime type as described in the CIM Infrastructure
    **     specification. It contains a union of MI_Timestamp and MI_Interval.
    **
    **==============================================================================
    */

    [StructLayout(LayoutKind.Explicit)]
    public struct MiDateTime
    {
        [FieldOffset(0)] public uint isTimestamp;

        [FieldOffset(4)] public MI_Timestamp timestamp;
        [FieldOffset(4)] public MI_Interval interval;
    }

    /*
    **==============================================================================
    **
    ** struct MI_<TYPE>A
    **
    **     These structure represent arrays of the types introduced above.
    **     * Porting notes:
    **        - The data 
    **==============================================================================
    */
    [StructLayout(LayoutKind.Explicit)]
    public struct MI_BooleanA
    {
        [FieldOffset(0)] public IntPtr data;
        [FieldOffset(8)] public uint  size;
    }

    struct MI_Uint8A
    {
        public IntPtr data;
        public uint size;
    }

    public struct MI_Sint8A
    {
        public IntPtr data;
        public uint size;
    }

    public struct MI_Uint16A
    {
        public IntPtr data;
        public uint size;
    }

    public struct MI_Sint16A
    {
        public IntPtr data;
        public uint size;
    }

    public struct MI_Uint32A
    {
        public IntPtr data;
        public uint size;
    }

    public struct MI_Sint32A
    {
        public IntPtr data;
        public uint size;
    }

    public struct MI_Uint64A
    {
        public IntPtr data;
        public uint size;
    }

    public struct MI_Sint64A
    {
        public IntPtr data;
        public uint size;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct MI_Real32A
    {
        [FieldOffset(0)] public IntPtr data;
        [FieldOffset(8)] public uint size;
    }

    public struct MI_Real64A
    {
        public IntPtr data;
        public uint size;
    }

    public struct MI_Char16A
    {
        public IntPtr data;
        public uint size;
    }

    public struct MI_DatetimeA
    {
        public IntPtr data;
        public uint size;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct MI_StringA
    {
        [FieldOffset(0)] public IntPtr data;
        [FieldOffset(8)] public uint size;
    }

    public struct MI_ReferenceA
    {
        public IntPtr data; //struct _MI_Instance** data;
        public uint size;
    }

    public struct MI_InstanceA
    {
        public IntPtr data;
        public uint size;
    }

    public struct MI_Array
    {
        public IntPtr data;
        public uint size;
    }

    internal enum MiType:uint
    {
        Boolean,
        UInt8,
        SInt8,
        UInt16,
        SInt16,
        UInt32,
        SInt32,
        UInt64,
        SInt64,
        Real32,
        Real64,
        Char16,
        DateTime,
        String,
        Reference,
        Instance,
        BooleanArray,
        UInt8Array,
        SInt8Array,
        UInt16Array,
        SInt16Array,
        UInt32Array,
        SInt32Array,
        UInt64Array,
        SInt64Array,
        Real32Array,
        Real64Array,
        Char16Array,
        DateTimeArray,
        StringArray,
        ReferenceArray,
        InstanceArray
    }

    /*
    **==============================================================================
    **
    ** union MiValue
    **
    **     This structure defines a union of all CIM data types.
    **
    **==============================================================================
    */
    [StructLayout(LayoutKind.Explicit)]
    public class MiValue
    {
        [FieldOffset(0)] public MI_Boolean boolean;
        [FieldOffset(0)] public System.Byte uint8;
        [FieldOffset(0)] public System.SByte sint8;
        [FieldOffset(0)] public System.UInt16 uint16;
        [FieldOffset(0)] public System.Int16 sint16;
        [FieldOffset(0)] public System.UInt32 uint32;
        [FieldOffset(0)] public System.Int32 sint32;
        [FieldOffset(0)] public System.UInt64 uint64;
        [FieldOffset(0)] public System.Int64 sint64;
        [FieldOffset(0)] public MI_Real32 real32;
        [FieldOffset(0)] public MI_Real64 real64;
        [FieldOffset(0)] public MI_Char16 char16;
        //[FieldOffset(16)] public MiDateTime datetime;
        //[FieldOffset(32)]  public char[] mistring;  // MI_Char* string;
        ////[FieldOffset(40)]
        //public IntPtr instance;   // MI_Instance *instance
        ////[FieldOffset(48)]
        //public IntPtr reference;  // MI_Instance *reference
        ////[FieldOffset(48)]
        //public MI_BooleanA booleana;
        ////[FieldOffset(48)]
        //public MI_Uint8A uint8a;
        ////[FieldOffset(48)]
        //public MI_Sint8A sint8a;
        ////[FieldOffset(48)]
        //public MI_Uint16A uint16a;
        ////[FieldOffset(48)]
        //public MI_Sint16A sint16a;
        ////[FieldOffset(48)]
        //public MI_Uint32A uint32a;
        ////[FieldOffset(48)]
        //public MI_Sint32A sint32a;
        ////[FieldOffset(48)]
        //public MI_Uint64A uint64a;
        ////[FieldOffset(48)]
        //public MI_Sint64A sint64a;
        ////[FieldOffset(48)]
        //public MI_Real32A real32a;
        ////[FieldOffset(48)]
        //public MI_Real64A real64a;
        ////[FieldOffset(48)]
        //public MI_Char16A char16a;
        ////[FieldOffset(48)]
        //public MI_DatetimeA datetimea;
        ////[FieldOffset(48)]
        //public MI_StringA stringa;
        ////[FieldOffset(48)]
        //public MI_ReferenceA referencea;
        ////[FieldOffset(48)]
        //public MI_InstanceA instancea;
        ////[FieldOffset(48)]
        //public MI_Array array;
    }

    internal enum MIWriteMessageChannel
    {
        MIWriteMessageChannelWarning,
        MIWriteMessageChannelVerbose,
        MIWriteMessageChannelDebug
    }

    internal class NativeCimCredential
    {
        // Methods
        private NativeCimCredential()
        {
            throw new NotImplementedException();
        }
        internal static void CreateCimCredential(string authenticationMechanism, out NativeCimCredentialHandle credentialHandle)
        {
            throw new NotImplementedException();
        }
        internal static void CreateCimCredential(string authenticationMechanism, string certificateThumbprint, out NativeCimCredentialHandle credentialHandle)
        {
            throw new NotImplementedException();
        }
        internal static void CreateCimCredential(string authenticationMechanism, string domain, string userName, SecureString password, out NativeCimCredentialHandle credentialHandle)
        {
            throw new NotImplementedException();
        }
    }

    internal class NativeCimCredentialHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
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
    }

    internal class OperationCallbackProcessingContext
    {
        // Fields
        //private bool inUserCode;
        //private object managedOperationContext;

        // Methods
        internal OperationCallbackProcessingContext(object managedOperationContext)
        {
            throw new NotImplementedException();
        }

        // Properties
        internal bool InUserCode {[return: MarshalAs(UnmanagedType.U1)] get;[param: MarshalAs(UnmanagedType.U1)] set; }
        internal object ManagedOperationContext { get; }
    }

    internal class OperationCallbacks
    {
        // Fields
        //private ClassCallbackDelegate<backing_store> ClassCallback;
        //private IndicationResultCallbackDelegate<backing_store> IndicationResultCallback;
        //private InstanceResultCallbackDelegate<backing_store> InstanceResultCallback;
        //private InternalErrorCallbackDelegate<backing_store> InternalErrorCallback;
        //private object <backing_store>ManagedOperationContext;
        //private PromptUserCallbackDelegate<backing_store> PromptUserCallback;
        //private StreamedParameterCallbackDelegate<backing_store> StreamedParameterCallback;
        //private WriteErrorCallbackDelegate<backing_store> WriteErrorCallback;
        //private WriteMessageCallbackDelegate<backing_store> WriteMessageCallback;
        //private WriteProgressCallbackDelegate<backing_store> WriteProgressCallback;
        //private static Action<Action, Func<Exception, bool>, Action<Exception>> userFilteredExceptionHandler;

        // Methods
        static OperationCallbacks()
        {
            //TODO: Implement
            //throw new NotImplementedException();
        }
        public OperationCallbacks()
        {
            //TODO: Implement
            //throw new NotImplementedException();
        }
        internal static void InvokeWithUserFilteredExceptionHandler(Action tryBody, Func<Exception, bool> userFilter, Action<Exception> catchBody)
        {
            throw new NotImplementedException();
        }
        //[return: MarshalAs(UnmanagedType.U1)]
        //internal unsafe bool SetMiOperationCallbacks(_MI_OperationCallbacks* pmiOperationCallbacks, MI_OperationWrapper* pmiOperationWrapper);

        // Properties
        [SuppressMessage("Micros.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal ClassCallbackDelegate ClassCallback { get; set; }
        [SuppressMessage("Micros.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal IndicationResultCallbackDelegate IndicationResultCallback { get; set; }
        [SuppressMessage("Micros.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal InstanceResultCallbackDelegate InstanceResultCallback { get; set; }
        [SuppressMessage("Micros.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal InternalErrorCallbackDelegate InternalErrorCallback { get; set; }
        [SuppressMessage("Micros.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp and in cs/CimOperationOptions.cs")]
        internal object ManagedOperationContext { get; set; }
        [SuppressMessage("Micros.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal PromptUserCallbackDelegate PromptUserCallback { get; set; }
        [SuppressMessage("Micros.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal StreamedParameterCallbackDelegate StreamedParameterCallback { get; set; }
        [SuppressMessage("Micros.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal WriteErrorCallbackDelegate WriteErrorCallback { get; set; }
        [SuppressMessage("Micros.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal WriteMessageCallbackDelegate WriteMessageCallback { get; set; }
        [SuppressMessage("Micros.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal WriteProgressCallbackDelegate WriteProgressCallback { get; set; }

        // Nested Types
        internal delegate void ClassCallbackDelegate(OperationCallbackProcessingContext callbackProcessingContext, OperationHandle operationHandle, ClassHandle classHandle, [MarshalAs(UnmanagedType.U1)] bool moreResults, MiResult resultCode, string errorString, InstanceHandle errorDetails);

        internal delegate void IndicationResultCallbackDelegate(OperationCallbackProcessingContext callbackProcessingContext, OperationHandle operationHandle, InstanceHandle instanceHandle, string bookmark, string machineID, [MarshalAs(UnmanagedType.U1)] bool moreResults, MiResult resultCode, string errorString, InstanceHandle errorDetails);

        internal delegate void InstanceResultCallbackDelegate(OperationCallbackProcessingContext callbackProcessingContext, OperationHandle operationHandle, InstanceHandle instanceHandle, [MarshalAs(UnmanagedType.U1)] bool moreResults, MiResult resultCode, string errorString, InstanceHandle errorDetails);

        internal delegate void InternalErrorCallbackDelegate(OperationCallbackProcessingContext callbackContextWhereInternalErrorOccurred, Exception exception);

        internal delegate void PromptUserCallbackDelegate(OperationCallbackProcessingContext callbackProcessingContext, OperationHandle operationHandle, string message, MiPromptType promptType, out MIResponseType response);

        internal delegate void StreamedParameterCallbackDelegate(OperationCallbackProcessingContext callbackProcessingContext, OperationHandle operationHandle, string parameterName, object parameterValue, MiType parameterType);

        internal delegate void WriteErrorCallbackDelegate(OperationCallbackProcessingContext callbackProcessingContext, OperationHandle operationHandle, InstanceHandle instanceHandle, out MIResponseType response);

        internal delegate void WriteMessageCallbackDelegate(OperationCallbackProcessingContext callbackProcessingContext, OperationHandle operationHandle, uint channel, string message);

        internal delegate void WriteProgressCallbackDelegate(OperationCallbackProcessingContext callbackProcessingContext, OperationHandle operationHandle, string activity, string currentOperation, string statusDescription, uint percentageComplete, uint secondsRemaining);
    }

    internal class OperationCallbacksDefinitions
    {
        // Methods
        public OperationCallbacksDefinitions()
        {
            throw new NotImplementedException();
        }
        //internal static unsafe void ClassAppDomainProxy(_MI_Operation* pmiOperation, void* callbackContext, _MI_Class modopt(IsConst)* pmiClass, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);
        //internal static unsafe void IndicationAppDomainProxy(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance modopt(IsConst)* pmiInstance, ushort modopt(IsConst)* bookmark, ushort modopt(IsConst)* machineID, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);
        //internal static unsafe void InstanceResultAppDomainProxy(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance modopt(IsConst)* pmiInstance, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);
        //internal static unsafe void PromptUserAppDomainProxy(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszMessage, _MI_PromptType promptType, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*, _MI_OperationCallback_ResponseType) promptUserResult);
        //internal static unsafe void StreamedParameterResultAppDomainProxy(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszParameterName, _MiType miType, _MiValue modopt(IsConst)* pmiParameterValue, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);
        //internal static unsafe void WriteErrorAppDomainProxy(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance* pmiInstance, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*, _MI_OperationCallback_ResponseType) writeErrorResult);
        //internal static unsafe void WriteMessageAppDomainProxy(_MI_Operation* pmiOperation, void* callbackContext, uint channel, ushort modopt(IsConst)* wszMessage);
        //internal static unsafe void WriteProgressAppDomainProxy(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszActivity, ushort modopt(IsConst)* wszCurrentOperation, ushort modopt(IsConst)* wszStatusDescription, uint percentageComplete, uint secondsRemaining);

        // Nested Types
        //internal unsafe delegate void ClassAppDomainProxyDelegate(_MI_Operation* pmiOperation, void* callbackContext, _MI_Class modopt(IsConst)* pmiClass, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);

        //internal unsafe delegate void IndicationAppDomainProxyDelegate(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance modopt(IsConst)* pmiInstance, ushort modopt(IsConst)* bookmark, ushort modopt(IsConst)* machineID, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);

        //internal unsafe delegate void InstanceResultAppDomainProxyDelegate(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance modopt(IsConst)* pmiInstance, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);

        //internal unsafe delegate void PromptUserAppDomainProxyDelegate(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszMessage, _MI_PromptType promptType, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*, _MI_OperationCallback_ResponseType) promptUserResult);

        //internal unsafe delegate void StreamedParameterResultAppDomainProxyDelegate(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszParameterName, _MiType miType, _MiValue modopt(IsConst)* pmiParameterValue, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);

        //internal unsafe delegate void WriteErrorAppDomainProxyDelegate(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance* pmiInstance, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*, _MI_OperationCallback_ResponseType) writeErrorResult);

        //internal unsafe delegate void WriteMessageAppDomainProxyDelegate(_MI_Operation* pmiOperation, void* callbackContext, uint channel, ushort modopt(IsConst)* wszMessage);

        //internal unsafe delegate void WriteProgressAppDomainProxyDelegate(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszActivity, ushort modopt(IsConst)* wszCurrentOperation, ushort modopt(IsConst)* wszStatusDescription, uint percentageComplete, uint secondsRemaining);
    }

    internal class OperationHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public NativeObject.MI_OperationOptions miOperationOptions;
        internal void AssertValidInternalState()
        {
     //       this.miOperationOptions = new NativeObject.MI_OperationOptions();
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

    internal class OperationMethods
    {
        // Methods
        private OperationMethods()
        {
            throw new NotImplementedException();
        }
        [SuppressMessage("Micros.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Runtime.InteropServices.SafeHandle.DangerousGetHandle", Justification = "C# layer internally manages the lifetime of OperationHandle + have to do this to call inline methods")]
        internal static MiResult Cancel(OperationHandle operationHandle, MiCancellationReason cancellationReason)
        {
            throw new NotImplementedException();
        }
        [SuppressMessage("Micros.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Runtime.InteropServices.SafeHandle.DangerousGetHandle", Justification = "C# layer internally manages the lifetime of OperationHandle + have to do this to call inline methods")]
        internal static MiResult GetClass(OperationHandle operationHandle, out ClassHandle classHandle, out bool moreResults, out MiResult result, out string errorMessage, out InstanceHandle completionDetails)
        {
            throw new NotImplementedException();
        }
        [SuppressMessage("Micros.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Runtime.InteropServices.SafeHandle.DangerousGetHandle", Justification = "C# layer internally manages the lifetime of OperationHandle + have to do this to call inline methods")]
        internal static MiResult GetIndication(OperationHandle operationHandle, out InstanceHandle instanceHandle, out string bookmark, out string machineID, out bool moreResults, out MiResult result, out string errorMessage, out InstanceHandle completionDetails)
        {
            throw new NotImplementedException();
        }
        [SuppressMessage("Micros.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Runtime.InteropServices.SafeHandle.DangerousGetHandle", Justification = "C# layer internally manages the lifetime of OperationHandle + have to do this to call inline methods")]
        internal static MiResult GetInstance(OperationHandle operationHandle, out InstanceHandle instanceHandle, out bool moreResults, out MiResult result, out string errorMessage, out InstanceHandle completionDetails)
        {
            throw new NotImplementedException();
        }
    }

    internal class OperationOptionsHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public NativeObject.MI_OperationOptions miOperationOptions;
        internal OperationOptionsHandle()
        {
            // 
            //this.miOperationOptions = new NativeObject.MI_OperationOptions();
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
    }

    internal class OperationOptionsMethods
    {
        // Methods
        private OperationOptionsMethods()
        {
            throw new NotImplementedException();
        }
        internal static MiResult Clone(OperationOptionsHandle operationOptionsHandle, out OperationOptionsHandle newOperationOptionsHandle)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetPromptUserModeOption(OperationOptionsHandle operationOptionsHandle, out MiCallbackMode mode)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetResourceUri(OperationOptionsHandle operationOptionsHandle, out string resourceUri)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetResourceUriPrefix(OperationOptionsHandle operationOptionsHandle, out string resourceUriPrefix)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetTimeout(OperationOptionsHandle operationOptionsHandle, out TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetUseMachineID(OperationOptionsHandle operationOptionsHandle, out bool useMachineId)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetWriteErrorModeOption(OperationOptionsHandle operationOptionsHandle, out MiCallbackMode mode)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetCustomOption(OperationOptionsHandle operationOptionsHandle, string optionName, object optionValue, MiType miType, [MarshalAs(UnmanagedType.U1)] bool mustComply)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetDisableChannelOption(OperationOptionsHandle operationOptionsHandle, uint channel)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetEnableChannelOption(OperationOptionsHandle operationOptionsHandle, uint channel)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetOption(OperationOptionsHandle operationOptionsHandle, string optionName, string optionValue)
        {
            //TODO: Implement
            return MiResult.OK;
            //throw new NotImplementedException();
        }
        internal static MiResult SetOption(OperationOptionsHandle operationOptionsHandle, string optionName, uint optionValue)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetPromptUserModeOption(OperationOptionsHandle operationOptionsHandle, MiCallbackMode mode)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetPromptUserRegularMode(OperationOptionsHandle operationOptionsHandle, MiCallbackMode mode, [MarshalAs(UnmanagedType.U1)] bool ackValue)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetResourceUri(OperationOptionsHandle operationOptionsHandle, string resourceUri)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetResourceUriPrefix(OperationOptionsHandle operationOptionsHandle, string resourceUriPrefix)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetTimeout(OperationOptionsHandle operationOptionsHandle, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetUseMachineID(OperationOptionsHandle operationOptionsHandle, [MarshalAs(UnmanagedType.U1)] bool useMachineId)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetWriteErrorModeOption(OperationOptionsHandle operationOptionsHandle, MiCallbackMode mode)
        {
            throw new NotImplementedException();
        }
    }

    internal class SerializerHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
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
    }

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

    internal class SessionHandleCallbackDefinitions
    {
        // Methods
        public SessionHandleCallbackDefinitions()
        {
            throw new NotImplementedException();
        }
        //internal static unsafe void SessionHandle_ReleaseHandle_CallbackWrapper_Invoke_Managed(_SessionHandle_ReleaseHandle_CallbackWrapper* pCallbackWrapper);
        //internal static unsafe void SessionHandle_ReleaseHandle_CallbackWrapper_Release_Managed(_SessionHandle_ReleaseHandle_CallbackWrapper* pCallbackWrapper);

        // Nested Types
        //internal unsafe delegate void SessionHandle_ReleaseHandle_CallbackWrapper_Invoke_Managed_Delegate(_SessionHandle_ReleaseHandle_CallbackWrapper* pCallbackWrapper);

        //internal unsafe delegate void SessionHandle_ReleaseHandle_CallbackWrapper_Release_Managed_Delegate(_SessionHandle_ReleaseHandle_CallbackWrapper* pCallbackWrapper);
    }

    internal class SessionHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
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
    }

    internal class SessionMethods
    {
        // Methods
        private SessionMethods()
        {
            throw new NotImplementedException();
        }
        internal static void AssociatorInstances(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, InstanceHandle sourceInstance, string assocClass, string resultClass, string sourceRole, string resultRole, [MarshalAs(UnmanagedType.U1)] bool keysOnly, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void CreateInstance(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, InstanceHandle instanceHandle, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void DeleteInstance(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, InstanceHandle instanceHandle, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void EnumerateClasses(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, string className, [MarshalAs(UnmanagedType.U1)] bool classNamesOnly, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void EnumerateInstances(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, string className, [MarshalAs(UnmanagedType.U1)] bool keysOnly, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void GetClass(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, string className, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void GetInstance(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, InstanceHandle instanceHandle, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void Invoke(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, string className, string methodName, InstanceHandle instanceHandleForTargetOfInvocation, InstanceHandle instanceHandleForMethodParameters, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void ModifyInstance(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, InstanceHandle instanceHandle, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        [SuppressMessage("Micros.Usage", "CA1801:ReviewUnusedParameters", MessageId = "keysOnly")]
        internal static void QueryInstances(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, string queryDialect, string queryExpression, [MarshalAs(UnmanagedType.U1)] bool keysOnly, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void ReferenceInstances(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, InstanceHandle sourceInstance, string associationClassName, string sourceRole, [MarshalAs(UnmanagedType.U1)] bool keysOnly, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void Subscribe(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, string queryDialect, string queryExpression, SubscriptionDeliveryOptionsHandle subscriptionDeliveryOptionsHandle, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void TestConnection(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
    }

    internal class SubscriptionDeliveryOptionsHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
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
    }

    internal class SubscriptionDeliveryOptionsMethods
    {
        // Methods
        private SubscriptionDeliveryOptionsMethods()
        {
            throw new NotImplementedException();
        }
        internal static MiResult AddCredentials(SubscriptionDeliveryOptionsHandle OptionsHandle, string optionName, NativeCimCredentialHandle credentials, uint flags)
        {
            throw new NotImplementedException();
        }
        internal static MiResult Clone(SubscriptionDeliveryOptionsHandle subscriptionDeliveryOptionsHandle, out SubscriptionDeliveryOptionsHandle newSubscriptionDeliveryOptionsHandle)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetDateTime(SubscriptionDeliveryOptionsHandle OptionsHandle, string optionName, object value, uint flags)
        {
            throw new NotImplementedException();
        }
        //internal static MiResult SetInterval(SubscriptionDeliveryOptionsHandle OptionsHandle, string optionName, ValueType modopt(TimeSpan) modopt(IsBoxed) value, uint flags)
        internal static MiResult SetInterval(SubscriptionDeliveryOptionsHandle OptionsHandle, string optionName, ValueType value, uint flags)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetNumber(SubscriptionDeliveryOptionsHandle OptionsHandle, string optionName, uint value, uint flags)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetString(SubscriptionDeliveryOptionsHandle OptionsHandle, string optionName, string value, uint flags)
        {
            throw new NotImplementedException();
        }
    }


}
