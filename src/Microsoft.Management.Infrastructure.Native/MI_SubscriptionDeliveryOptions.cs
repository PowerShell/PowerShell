using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace NativeObject
{
    [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public struct MI_SubscriptionDeliveryOptionsPtr
    {
        public IntPtr ptr;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public struct MI_SubscriptionDeliveryOptionsOutPtr
    {
        public IntPtr ptr;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public class MI_SubscriptionDeliveryOptions
    {
        public MI_Result SetDateTime(
            string optionName,
            MI_Datetime value,
            UInt32 flags
            )
        {
            MI_Result resultLocal = this.ft.SetDateTime(this,
                optionName,
                ref value,
                flags);
            return resultLocal;
        }

        public MI_Result SetInterval(
            string optionName,
            MI_Interval value,
            UInt32 flags
            )
        {
            MI_Result resultLocal = this.ft.SetInterval(this,
                optionName,
                ref value,
                flags);
            return resultLocal;
        }

        public MI_Result GetDateTime(
            string optionName,
            out MI_Datetime value,
            out UInt32 index,
            out UInt32 flags
            )
        {
            MI_Datetime valueLocal = new MI_Datetime();
            MI_Result resultLocal = this.ft.GetDateTime(this,
                optionName,
                ref valueLocal,
                out index,
                out flags);

            value = valueLocal;
            return resultLocal;
        }

        public MI_Result GetInterval(
            string optionName,
            out MI_Interval value,
            out UInt32 index,
            out UInt32 flags
            )
        {
            MI_Interval valueLocal = new MI_Interval();
            MI_Result resultLocal = this.ft.GetInterval(this,
                optionName,
                ref valueLocal,
                out index,
                out flags);

            value = valueLocal;
            return resultLocal;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
        private struct MI_SubscriptionDeliveryOptionsMembers
        {
            public UInt64 reserved1;
            public IntPtr reserved2;
            public IntPtr ft;
        }

        // Marshal implements these with Reflection - pay this hit only once
        private static int MI_SubscriptionDeliveryOptionsMembersFTOffset = (int)Marshal.OffsetOf(typeof(MI_SubscriptionDeliveryOptionsMembers), "ft");
        private static int MI_SubscriptionDeliveryOptionsMembersSize = Marshal.SizeOf(typeof(MI_SubscriptionDeliveryOptionsMembers));

        private MI_SubscriptionDeliveryOptionsPtr ptr;
        private bool isDirect;
        private Lazy<MI_SubscriptionDeliveryOptionsFT> mft;

        ~MI_SubscriptionDeliveryOptions()
        {
            Marshal.FreeHGlobal(this.ptr.ptr);
        }

        private MI_SubscriptionDeliveryOptions(bool isDirect)
        {
            this.isDirect = isDirect;
            this.mft = new Lazy<MI_SubscriptionDeliveryOptionsFT>(this.MarshalFT);

            var necessarySize = this.isDirect ? MI_SubscriptionDeliveryOptionsMembersSize : NativeMethods.IntPtrSize;
            this.ptr.ptr = Marshal.AllocHGlobal(necessarySize);

            unsafe
            {
                NativeMethods.memset((byte*)this.ptr.ptr, 0, (uint)necessarySize);
            }
        }

        public static MI_SubscriptionDeliveryOptions NewDirectPtr()
        {
            return new MI_SubscriptionDeliveryOptions(true);
        }

        public static MI_SubscriptionDeliveryOptions NewIndirectPtr()
        {
            return new MI_SubscriptionDeliveryOptions(false);
        }

        public static MI_SubscriptionDeliveryOptions NewFromDirectPtr(IntPtr ptr)
        {
            var res = new MI_SubscriptionDeliveryOptions(false);
            Marshal.WriteIntPtr(res.ptr.ptr, ptr);
            return res;
        }

        public static implicit operator MI_SubscriptionDeliveryOptionsPtr(MI_SubscriptionDeliveryOptions instance)
        {
            // If the indirect pointer is zero then the object has not
            // been initialized and it is not valid to refer to its data
            if (instance != null && instance.Ptr == IntPtr.Zero)
            {
                throw new InvalidCastException();
            }

            return new MI_SubscriptionDeliveryOptionsPtr() { ptr = instance == null ? IntPtr.Zero : instance.Ptr };
        }

        public static implicit operator MI_SubscriptionDeliveryOptionsOutPtr(MI_SubscriptionDeliveryOptions instance)
        {
            // We are not currently supporting the ability to get the address
            // of our direct pointer, though it is technically feasible 
            if (instance != null && instance.isDirect)
            {
                throw new InvalidCastException();
            }

            return new MI_SubscriptionDeliveryOptionsOutPtr() { ptr = instance == null ? IntPtr.Zero : instance.ptr.ptr };
        }

        public static MI_SubscriptionDeliveryOptions Null { get { return null; } }
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

        public MI_Result SetString(
            string optionName,
            string value,
            UInt32 flags
            )
        {
            MI_Result resultLocal = this.ft.SetString(this,
                optionName,
                value,
                flags);
            return resultLocal;
        }

        public MI_Result SetNumber(
            string optionName,
            UInt32 value,
            UInt32 flags
            )
        {
            MI_Result resultLocal = this.ft.SetNumber(this,
                optionName,
                value,
                flags);
            return resultLocal;
        }

        public MI_Result AddCredentials(
            string optionName,
            MI_UserCredentials credentials,
            UInt32 flags
            )
        {
            MI_Result resultLocal = this.ft.AddCredentials(this,
                optionName,
                credentials,
                flags);
            return resultLocal;
        }

        public MI_Result Delete()
        {
            return this.ft.Delete(this);
        }

        public MI_Result GetString(
            string optionName,
            out string value,
            out UInt32 index,
            out UInt32 flags
            )
        {
            MI_String valueLocal = MI_String.NewIndirectPtr();

            MI_Result resultLocal = this.ft.GetString(this,
                optionName,
                valueLocal,
                out index,
                out flags);

            value = valueLocal.Value;
            return resultLocal;
        }

        public MI_Result GetNumber(
            string optionName,
            out UInt32 value,
            out UInt32 index,
            out UInt32 flags
            )
        {
            MI_Result resultLocal = this.ft.GetNumber(this,
                optionName,
                out value,
                out index,
                out flags);
            return resultLocal;
        }

        public MI_Result GetOptionCount(
            out UInt32 count
            )
        {
            MI_Result resultLocal = this.ft.GetOptionCount(this,
                out count);
            return resultLocal;
        }

        public MI_Result GetOptionAt(
            UInt32 index,
            out string optionName,
            MI_Value value,
            out MI_Type type,
            out UInt32 flags
            )
        {
            MI_String optionNameLocal = MI_String.NewIndirectPtr();

            MI_Result resultLocal = this.ft.GetOptionAt(this,
                index,
                optionNameLocal,
                value,
                out type,
                out flags);

            optionName = optionNameLocal.Value;
            return resultLocal;
        }

        public MI_Result GetOption(
            string optionName,
            MI_Value value,
            out MI_Type type,
            out UInt32 index,
            out UInt32 flags
            )
        {
            MI_Result resultLocal = this.ft.GetOption(this,
                optionName,
                value,
                out type,
                out index,
                out flags);
            return resultLocal;
        }

        public MI_Result GetCredentialsCount(
            out UInt32 count
            )
        {
            MI_Result resultLocal = this.ft.GetCredentialsCount(this,
                out count);
            return resultLocal;
        }

        public MI_Result GetCredentialsAt(
            UInt32 index,
            out string optionName,
            MI_UserCredentials credentials,
            out UInt32 flags
            )
        {
            MI_String optionNameLocal = MI_String.NewIndirectPtr();

            MI_Result resultLocal = this.ft.GetCredentialsAt(this,
                index,
                optionNameLocal,
                credentials,
                out flags);

            optionName = optionNameLocal.Value;
            return resultLocal;
        }

        public MI_Result GetCredentialsPasswordAt(
            UInt32 index,
            out string optionName,
            string password,
            UInt32 bufferLength,
            out UInt32 passwordLength,
            out UInt32 flags
            )
        {
            MI_String optionNameLocal = MI_String.NewIndirectPtr();

            MI_Result resultLocal = this.ft.GetCredentialsPasswordAt(this,
                index,
                optionNameLocal,
                password,
                bufferLength,
                out passwordLength,
                out flags);

            optionName = optionNameLocal.Value;
            return resultLocal;
        }

        public MI_Result Clone(
            MI_SubscriptionDeliveryOptions newSubscriptionDeliveryOptions
            )
        {
            MI_Result resultLocal = this.ft.Clone(this,
                newSubscriptionDeliveryOptions);
            return resultLocal;
        }

        private MI_SubscriptionDeliveryOptionsFT ft { get { return this.mft.Value; } }
        private MI_SubscriptionDeliveryOptionsFT MarshalFT()
        {
            MI_SubscriptionDeliveryOptionsFT res = new MI_SubscriptionDeliveryOptionsFT();
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

                ftPtr = *((IntPtr*)((byte*)structurePtr + MI_SubscriptionDeliveryOptionsMembersFTOffset));
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
        public class MI_SubscriptionDeliveryOptionsFT
        {
            public MI_SubscriptionDeliveryOptions_SetString SetString;
            public MI_SubscriptionDeliveryOptions_SetNumber SetNumber;
            public MI_SubscriptionDeliveryOptions_SetDateTime SetDateTime;
            public MI_SubscriptionDeliveryOptions_SetInterval SetInterval;
            public MI_SubscriptionDeliveryOptions_AddCredentials AddCredentials;
            public MI_SubscriptionDeliveryOptions_Delete Delete;
            public MI_SubscriptionDeliveryOptions_GetString GetString;
            public MI_SubscriptionDeliveryOptions_GetNumber GetNumber;
            public MI_SubscriptionDeliveryOptions_GetDateTime GetDateTime;
            public MI_SubscriptionDeliveryOptions_GetInterval GetInterval;
            public MI_SubscriptionDeliveryOptions_GetOptionCount GetOptionCount;
            public MI_SubscriptionDeliveryOptions_GetOptionAt GetOptionAt;
            public MI_SubscriptionDeliveryOptions_GetOption GetOption;
            public MI_SubscriptionDeliveryOptions_GetCredentialsCount GetCredentialsCount;
            public MI_SubscriptionDeliveryOptions_GetCredentialsAt GetCredentialsAt;
            public MI_SubscriptionDeliveryOptions_GetCredentialsPasswordAt GetCredentialsPasswordAt;
            public MI_SubscriptionDeliveryOptions_Clone Clone;

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_SubscriptionDeliveryOptions_SetString(
                MI_SubscriptionDeliveryOptionsPtr options,
                string optionName,
                string value,
                UInt32 flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_SubscriptionDeliveryOptions_SetNumber(
                MI_SubscriptionDeliveryOptionsPtr options,
                string optionName,
                UInt32 value,
                UInt32 flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_SubscriptionDeliveryOptions_SetDateTime(
                MI_SubscriptionDeliveryOptionsPtr options,
                string optionName,
                ref MI_Datetime value,
                UInt32 flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_SubscriptionDeliveryOptions_SetInterval(
                MI_SubscriptionDeliveryOptionsPtr options,
                string optionName,
                ref MI_Interval value,
                UInt32 flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_SubscriptionDeliveryOptions_AddCredentials(
                MI_SubscriptionDeliveryOptionsPtr options,
                string optionName,
                MI_UserCredentials credentials,
                UInt32 flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_SubscriptionDeliveryOptions_Delete(
                MI_SubscriptionDeliveryOptionsPtr self
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_SubscriptionDeliveryOptions_GetString(
                MI_SubscriptionDeliveryOptionsPtr options,
                string optionName,
                [In, Out] MI_String value,
                out UInt32 index,
                out UInt32 flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_SubscriptionDeliveryOptions_GetNumber(
                MI_SubscriptionDeliveryOptionsPtr options,
                string optionName,
                out UInt32 value,
                out UInt32 index,
                out UInt32 flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_SubscriptionDeliveryOptions_GetDateTime(
                MI_SubscriptionDeliveryOptionsPtr options,
                string optionName,
                ref MI_Datetime value,
                out UInt32 index,
                out UInt32 flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_SubscriptionDeliveryOptions_GetInterval(
                MI_SubscriptionDeliveryOptionsPtr options,
                string optionName,
                ref MI_Interval value,
                out UInt32 index,
                out UInt32 flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_SubscriptionDeliveryOptions_GetOptionCount(
                MI_SubscriptionDeliveryOptionsPtr options,
                out UInt32 count
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_SubscriptionDeliveryOptions_GetOptionAt(
                MI_SubscriptionDeliveryOptionsPtr options,
                UInt32 index,
                [In, Out] MI_String optionName,
                [In, Out] MI_Value.MIValueBlock value,
                out MI_Type type,
                out UInt32 flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_SubscriptionDeliveryOptions_GetOption(
                MI_SubscriptionDeliveryOptionsPtr options,
                string optionName,
                [In, Out] MI_Value.MIValueBlock value,
                out MI_Type type,
                out UInt32 index,
                out UInt32 flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_SubscriptionDeliveryOptions_GetCredentialsCount(
                MI_SubscriptionDeliveryOptionsPtr options,
                out UInt32 count
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_SubscriptionDeliveryOptions_GetCredentialsAt(
                MI_SubscriptionDeliveryOptionsPtr options,
                UInt32 index,
                [In, Out] MI_String optionName,
                MI_UserCredentials credentials,
                out UInt32 flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_SubscriptionDeliveryOptions_GetCredentialsPasswordAt(
                MI_SubscriptionDeliveryOptionsPtr options,
                UInt32 index,
                [In, Out] MI_String optionName,
                string password,
                UInt32 bufferLength,
                out UInt32 passwordLength,
                out UInt32 flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_SubscriptionDeliveryOptions_Clone(
                MI_SubscriptionDeliveryOptionsPtr self,
                [In, Out] MI_SubscriptionDeliveryOptionsPtr newSubscriptionDeliveryOptions
                );
        }
    }
}
