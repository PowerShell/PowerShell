using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace NativeObject
{
    [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public struct MI_ApplicationPtr
    {
        public IntPtr ptr;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public struct MI_ApplicationOutPtr
    {
        public IntPtr ptr;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public partial class MI_Application
    {
        public static MI_Result Initialize(string applicationId, out MI_Instance extendedError, out MI_Application application)
        {
            MI_Application applicationLocal = MI_Application.NewDirectPtr();
            MI_Instance extendedErrorLocal = MI_Instance.NewIndirectPtr();

            MI_Result result = NativeMethods.MI_Application_InitializeV1(0, applicationId, extendedErrorLocal, applicationLocal);

            extendedError = extendedErrorLocal;
            application = applicationLocal;
            return result;
        }

        public MI_Result NewSession(
            string protocol,
            string destination,
            MI_DestinationOptions options,
            MI_SessionCallbacks callbacks,
            out MI_Instance extendedError,
            out MI_Session session
            )
        {
            if (callbacks != null)
            {
                throw new NotImplementedException();
            }

            MI_Instance extendedErrorLocal = MI_Instance.NewIndirectPtr();
            MI_Session sessionLocal = MI_Session.NewDirectPtr();

            MI_Result resultLocal = this.ft.NewSession(this,
                protocol,
                destination,
                options,
                null,
                extendedErrorLocal,
                sessionLocal);

            extendedError = extendedErrorLocal;
            session = sessionLocal;
            return resultLocal;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
        private struct MI_ApplicationMembers
        {
            public UInt64 reserved1;
            public IntPtr reserved2;
            public IntPtr ft;
        }

        // Marshal implements these with Reflection - pay this hit only once
        private static int MI_ApplicationMembersFTOffset = (int)Marshal.OffsetOf(typeof(MI_ApplicationMembers), "ft");
        private static int MI_ApplicationMembersSize = Marshal.SizeOf(typeof(MI_ApplicationMembers));

        private MI_ApplicationPtr ptr;
        private bool isDirect;
        private Lazy<MI_ApplicationFT> mft;

        ~MI_Application()
        {
            Marshal.FreeHGlobal(this.ptr.ptr);
        }

        private MI_Application(bool isDirect)
        {
            this.isDirect = isDirect;
            this.mft = new Lazy<MI_ApplicationFT>(this.MarshalFT);

            var necessarySize = this.isDirect ? MI_ApplicationMembersSize : NativeMethods.IntPtrSize;
            this.ptr.ptr = Marshal.AllocHGlobal(necessarySize);

            unsafe
            {
                NativeMethods.memset((byte*)this.ptr.ptr, 0, (uint)necessarySize);
            }
        }

        public static MI_Application NewDirectPtr()
        {
            return new MI_Application(true);
        }

        public static MI_Application NewIndirectPtr()
        {
            return new MI_Application(false);
        }

        public static MI_Application NewFromDirectPtr(IntPtr ptr)
        {
            var res = new MI_Application(false);
            Marshal.WriteIntPtr(res.ptr.ptr, ptr);
            return res;
        }

        public static implicit operator MI_ApplicationPtr(MI_Application instance)
        {
            // If the indirect pointer is zero then the object has not
            // been initialized and it is not valid to refer to its data
            if (instance != null && instance.Ptr == IntPtr.Zero)
            {
                throw new InvalidCastException();
            }

            return new MI_ApplicationPtr() { ptr = instance == null ? IntPtr.Zero : instance.Ptr };
        }

        public static implicit operator MI_ApplicationOutPtr(MI_Application instance)
        {
            // We are not currently supporting the ability to get the address
            // of our direct pointer, though it is technically feasible 
            if (instance != null && instance.isDirect)
            {
                throw new InvalidCastException();
            }

            return new MI_ApplicationOutPtr() { ptr = instance == null ? IntPtr.Zero : instance.ptr.ptr };
        }

        public static MI_Application Null { get { return null; } }
        public bool IsNull { get { return this.Ptr == IntPtr.Zero; } }
        public IntPtr Ptr
        {
            get
            {
                IntPtr structurePtr = this.ptr.ptr;
                if (!this.isDirect)
                {
                    if (structurePtr == IntPtr.Zero)
                    {
                        throw new InvalidOperationException();
                    }

                    // This can be easily implemented with Marshal.ReadIntPtr
                    // but that has function call overhead
                    unsafe
                    {
                        structurePtr = *(IntPtr*)structurePtr;
                    }
                }

                return structurePtr;
            }
        }

        public MI_Result Close()
        {
            return this.ft.Close(this);
        }

        public MI_Result NewHostedProvider(
            string namespaceName,
            string providerName,
            IntPtr mi_Main,
            out MI_Instance extendedError,
            IntPtr provider
            )
        {
            MI_Instance extendedErrorLocal = MI_Instance.NewIndirectPtr();

            MI_Result resultLocal = this.ft.NewHostedProvider(this,
                namespaceName,
                providerName,
                mi_Main,
                extendedErrorLocal,
                provider);

            extendedError = extendedErrorLocal;
            return resultLocal;
        }

        public MI_Result NewInstance(
            string className,
            MI_ClassDecl classRTTI,
            out MI_Instance instance
            )
        {
            MI_Instance instanceLocal = MI_Instance.NewIndirectPtr();

            MI_Result resultLocal = this.ft.NewInstance(this,
                className,
                classRTTI,
                instanceLocal);

            instance = instanceLocal;
            return resultLocal;
        }

        public MI_Result NewDestinationOptions(
            out MI_DestinationOptions options
            )
        {
            MI_DestinationOptions optionsLocal = MI_DestinationOptions.NewIndirectPtr();
            MI_Result resultLocal = this.ft.NewDestinationOptions(this,
                optionsLocal);

            options = optionsLocal;
            return resultLocal;
        }

        public MI_Result NewOperationOptions(
            bool customOptionsMustUnderstand,
            out MI_OperationOptions operationOptions
            )
        {
            MI_OperationOptions operationOptionsLocal = MI_OperationOptions.NewDirectPtr();

            MI_Result resultLocal = this.ft.NewOperationOptions(this,
                customOptionsMustUnderstand,
                operationOptionsLocal);

            operationOptions = operationOptionsLocal;
            return resultLocal;
        }

        public MI_Result NewSubscriptionDeliveryOptions(
            MI_SubscriptionDeliveryType deliveryType,
            MI_SubscriptionDeliveryOptions deliveryOptions
            )
        {
            MI_Result resultLocal = this.ft.NewSubscriptionDeliveryOptions(this,
                deliveryType,
                deliveryOptions);
            return resultLocal;
        }

        public MI_Result NewSerializer(
            MI_SerializerFlags flags,
            string format,
            out MI_Serializer serializer
            )
        {
            MI_Serializer serializerLocal = new MI_Serializer();

            MI_Result resultLocal = this.ft.NewSerializer(this,
                flags,
                format,
                serializerLocal);

            serializer = serializerLocal;
            return resultLocal;
        }

        public MI_Result NewDeserializer(
            MI_SerializerFlags flags,
            string format,
            out MI_Deserializer deserializer
            )
        {
            MI_Deserializer deserializerLocal = new MI_Deserializer();

            MI_Result resultLocal = this.ft.NewDeserializer(this,
                flags,
                format,
                deserializerLocal);

            deserializer = deserializerLocal;
            return resultLocal;
        }

        public MI_Result NewInstanceFromClass(
            string className,
            MI_Class classObject,
            out MI_Instance instance
            )
        {
            MI_Instance instanceLocal = MI_Instance.NewIndirectPtr();

            MI_Result resultLocal = this.ft.NewInstanceFromClass(this,
                className,
                classObject,
                instanceLocal);

            instance = instanceLocal;
            return resultLocal;
        }

        private MI_ApplicationFT ft { get { return this.mft.Value; } }
        private MI_ApplicationFT MarshalFT()
        {
            MI_ApplicationFT res = new MI_ApplicationFT();
            IntPtr ftPtr = IntPtr.Zero;
            unsafe
            {
                // Just as easily could be implemented with Marshal
                // but that would copy more than the one pointer we need
                IntPtr structurePtr = this.Ptr;
                if (structurePtr == IntPtr.Zero)
                {
                    throw new InvalidOperationException();
                }

                ftPtr = *((IntPtr*)((byte*)structurePtr + MI_ApplicationMembersFTOffset));
            }

            if (ftPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException();
            }

            // No apparent way to implement this in an unsafe block
            Marshal.PtrToStructure(ftPtr, res);
            return res;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
        public class MI_ApplicationFT
        {
            public MI_Application_Close Close;
            public MI_Application_NewSession NewSession;
            public MI_Application_NewHostedProvider NewHostedProvider;
            public MI_Application_NewInstance NewInstance;
            public MI_Application_NewDestinationOptions NewDestinationOptions;
            public MI_Application_NewOperationOptions NewOperationOptions;
            public MI_Application_NewSubscriptionDeliveryOptions NewSubscriptionDeliveryOptions;
            public MI_Application_NewSerializer NewSerializer;
            public MI_Application_NewDeserializer NewDeserializer;
            public MI_Application_NewInstanceFromClass NewInstanceFromClass;

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Application_Close(
                MI_ApplicationPtr application
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Application_NewSession(
                MI_ApplicationPtr application,
                string protocol,
                string destination,
                [In, Out] MI_DestinationOptionsPtr options,
                MI_SessionCallbacksNative callbacks,
                [In, Out] MI_InstanceOutPtr extendedError,
                [In, Out] MI_SessionPtr session
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Application_NewHostedProvider(
                MI_ApplicationPtr application,
                string namespaceName,
                string providerName,
                IntPtr mi_Main,
                [In, Out] MI_InstanceOutPtr extendedError,
                IntPtr provider
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Application_NewInstance(
                MI_ApplicationPtr application,
                string className,
                [In, Out] MI_ClassDeclPtr classRTTI,
                [In, Out] MI_InstanceOutPtr instance
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Application_NewDestinationOptions(
                MI_ApplicationPtr application,
                [In, Out] MI_DestinationOptionsPtr options
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Application_NewOperationOptions(
                MI_ApplicationPtr application,
                [MarshalAs(UnmanagedType.U1)] bool customOptionsMustUnderstand,
                [In, Out] MI_OperationOptionsPtr operationOptions
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Application_NewSubscriptionDeliveryOptions(
                MI_ApplicationPtr application,
                MI_SubscriptionDeliveryType deliveryType,
                [In, Out] MI_SubscriptionDeliveryOptionsPtr deliveryOptions
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Application_NewSerializer(
                MI_ApplicationPtr application,
                MI_SerializerFlags flags,
                string format,
                MI_Serializer serializer
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Application_NewDeserializer(
                MI_ApplicationPtr application,
                MI_SerializerFlags flags,
                string format,
                MI_Deserializer deserializer
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Application_NewInstanceFromClass(
                MI_ApplicationPtr application,
                string className,
                [In, Out] MI_ClassPtr classObject,
                [In, Out] MI_InstanceOutPtr instance
                );
        }
    }
}
