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
}
