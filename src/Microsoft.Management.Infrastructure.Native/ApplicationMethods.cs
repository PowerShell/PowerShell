//using Micros.Win32.SafeHandles;
using System;
//using System.Collections.Generic;
//using System.Text;
using System.Runtime.InteropServices;
//using System.Diagnostics;
//using System.Diagnostics.CodeAnalysis;
//using System.Security;
//using System.Globalization;
using NativeObject;

namespace Microsoft.Management.Infrastructure.Native
{
    internal class ApplicationMethods
    {
        // Fields
        internal static string protocol_DCOM;
        internal static string protocol_WSMan;

        // Methods
        static ApplicationMethods()
        {
            //throw new NotImplementedException();
        }
        private ApplicationMethods()
        {
            //throw new NotImplementedException();
        }
        internal static MiResult GetCimErrorFromMiResult(MiResult errorCode, string errorMessage, out InstanceHandle cimError)
        {
            throw new NotImplementedException();
        }

        // Creates a new MI_Application and an instance of MI_Errors
        internal static MiResult Initialize(out InstanceHandle errorDetails, out ApplicationHandle applicationHandle)
        {
            return InitializeCore(out  errorDetails, out applicationHandle);
        }

        // passes in to MI.h two pointers to structs which have been previously declared, sets them, and returns an MiResult
        internal static MiResult InitializeCore(out InstanceHandle errorDetails, out ApplicationHandle applicationHandle)
        {
            // Initialize instance
            string appID       = null;
            errorDetails       = new InstanceHandle();
            IntPtr ePtr        = IntPtr.Zero;
            applicationHandle  = new ApplicationHandle();
            MiResult result    = (MiResult)(uint)NativeObject.MI_Application.Initialize(appID, out errorDetails.mmiInstance, out applicationHandle.miApp);
            return result;
        }

        internal static MiResult NewDeserializer(ApplicationHandle applicationHandle, string format, uint flags, out DeserializerHandle deserializerHandle)
        {
            throw new NotImplementedException();
        }
        internal static MiResult NewDestinationOptions(ApplicationHandle applicationHandle, out DestinationOptionsHandle destinationOptionsHandle)
        {
            throw new NotImplementedException();
        }

        internal static MiResult NewInstance(ApplicationHandle applicationHandle, string className, ClassHandle classHandle, out InstanceHandle newInstance)
        {
            NativeObject.MI_Instance pInstance = NativeObject.MI_Instance.NewIndirectPtr();
            NativeObject.MI_ClassDecl rtti = null;
            MiResult result                         = MiResult.OK;

            result = (MiResult)(uint)applicationHandle.miApp.NewInstance(className, rtti, out pInstance);
            newInstance = new InstanceHandle(pInstance);

            return result;
        }
        internal static MiResult NewOperationOptions(ApplicationHandle applicationHandle, [MarshalAs(UnmanagedType.U1)] bool mustUnderstand, out OperationOptionsHandle operationOptionsHandle)
        {
            // Console.WriteLine(">>Native/NewOperationOptions");
            // //TODO: create OperationOptionsHandle
            // operationOptionsHandle = new OperationOptionsHandle();
            // NativeObject.MI_Application localApp = applicationHandle.miApp;

            // IntPtr appBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(localApp));
            // Marshal.StructureToPtr(localApp, appBuffer, false);
            // NativeObject.MI_OperationOptions localOpts = operationOptionsHandle.miOperationOptions;
            // // Marshal Struct as IntPtr? We may not have to marshalAs.PtrToStructure

            // Console.WriteLine("Break> Pinvoke");
            // // perform p/invoke
            // MiResult result = NativeObject.MI_Application_NewOperationOptions(appBuffer,
            //                                                               mustUnderstand,
            //                                                               out localOpts);

            // Console.WriteLine("NewOperationOptions result : {0}", result);
            // operationOptionsHandle.miOperationOptions = localOpts;
            // Marshal.FreeHGlobal(appBuffer);

            // return result;
            throw new NotImplementedException();
        }
        internal static MiResult NewSerializer(ApplicationHandle applicationHandle, string format, uint flags, out SerializerHandle serializerHandle)
        {
            throw new NotImplementedException();
        }

        internal static MiResult NewSession(ApplicationHandle applicationHandle, string protocol, string destination, DestinationOptionsHandle destinationOptionsHandle, out InstanceHandle extendedError, out SessionHandle sessionHandle)
        {
            // Console.WriteLine("In NewSession");
            // extendedError = new InstanceHandle();
            // sessionHandle = new SessionHandle();
            // Console.WriteLine("instance and sessionHandle created");

            throw new NotImplementedException();
        }
        internal static MiResult NewSubscriptionDeliveryOptions(ApplicationHandle applicationHandle, MiSubscriptionDeliveryType deliveryType, out SubscriptionDeliveryOptionsHandle subscriptionDeliveryOptionsHandle)
        {
            throw new NotImplementedException();
        }
    }
}
