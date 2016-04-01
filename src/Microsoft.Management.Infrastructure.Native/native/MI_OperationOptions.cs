using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace NativeObject
{
    [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public struct MI_OperationOptionsPtr
    {
        public IntPtr ptr;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public struct MI_OperationOptionsOutPtr
    {
        public IntPtr ptr;
    }

    public class MI_OperationOptions
    {
        public MI_Result SetInterval(
            string optionName,
            MI_Interval value,
            MI_OperationOptionsFlags flags
            )
        {
            MI_Result resultLocal = this.ft.SetInterval(this,
                optionName,
                ref value,
                flags);
            return resultLocal;
        }

        public MI_Result GetInterval(
            string optionName,
            out MI_Interval value,
            out UInt32 index,
            out MI_OperationOptionsFlags flags
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
        private struct MI_OperationOptionsMembers
        {
            public UInt64 reserved1;
            public IntPtr reserved2;
            public IntPtr ft;
        }

        // Marshal implements these with Reflection - pay this hit only once
        private static int MI_OperationOptionsMembersFTOffset = (int)Marshal.OffsetOf<MI_OperationOptionsMembers>("ft");
        private static int MI_OperationOptionsMembersSize = Marshal.SizeOf<MI_OperationOptionsMembers>();

        private MI_OperationOptionsPtr ptr;
        private bool isDirect;
        private Lazy<MI_OperationOptionsFT> mft;

        ~MI_OperationOptions()
        {
            Marshal.FreeHGlobal(this.ptr.ptr);
        }

        private MI_OperationOptions(bool isDirect)
        {
            this.isDirect = isDirect;
            this.mft = new Lazy<MI_OperationOptionsFT>(this.MarshalFT);

            var necessarySize = this.isDirect ? MI_OperationOptionsMembersSize : NativeMethods.IntPtrSize;
            this.ptr.ptr = Marshal.AllocHGlobal(necessarySize);

            unsafe
            {
                NativeMethods.memset((byte*)this.ptr.ptr, 0, (uint)necessarySize);
            }
        }

        public static MI_OperationOptions NewDirectPtr()
        {
            return new MI_OperationOptions(true);
        }

        public static MI_OperationOptions NewIndirectPtr()
        {
            return new MI_OperationOptions(false);
        }

        public static MI_OperationOptions NewFromDirectPtr(IntPtr ptr)
        {
            var res = new MI_OperationOptions(false);
            Marshal.WriteIntPtr(res.ptr.ptr, ptr);
            return res;
        }

        public static implicit operator MI_OperationOptionsPtr(MI_OperationOptions instance)
        {
            // If the indirect pointer is zero then the object has not
            // been initialized and it is not valid to refer to its data
            if (instance != null && instance.Ptr == IntPtr.Zero)
            {
                throw new InvalidCastException();
            }

            return new MI_OperationOptionsPtr() { ptr = instance == null ? IntPtr.Zero : instance.Ptr };
        }

        public static implicit operator MI_OperationOptionsOutPtr(MI_OperationOptions instance)
        {
            // We are not currently supporting the ability to get the address
            // of our direct pointer, though it is technically feasible 
            if (instance != null && instance.isDirect)
            {
                throw new InvalidCastException();
            }

            return new MI_OperationOptionsOutPtr() { ptr = instance == null ? IntPtr.Zero : instance.ptr.ptr };
        }

        public static MI_OperationOptions Null { get { return null; } }
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

        public void Delete()
        {
            this.ft.Delete(this);
        }

        public MI_Result SetString(
            string optionName,
            string value,
            MI_OperationOptionsFlags flags
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
            MI_OperationOptionsFlags flags
            )
        {
            MI_Result resultLocal = this.ft.SetNumber(this,
                optionName,
                value,
                flags);
            return resultLocal;
        }

        public MI_Result SetCustomOption(
            string optionName,
            MI_Type valueType,
            MI_Value value,
            bool mustComply,
            MI_OperationOptionsFlags flags
            )
        {
            MI_Result resultLocal = this.ft.SetCustomOption(this,
                optionName,
                valueType,
                value,
                mustComply,
                flags);
            return resultLocal;
        }

        public MI_Result GetString(
            string optionName,
            out string value,
            out UInt32 index,
            out MI_OperationOptionsFlags flags
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
            out MI_OperationOptionsFlags flags
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
            out MI_OperationOptionsFlags flags
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
            out MI_OperationOptionsFlags flags
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

        public MI_Result GetEnabledChannels(
            string optionName,
            out UInt32 channels,
            UInt32 bufferLength,
            out UInt32 channelCount,
            out MI_OperationOptionsFlags flags
            )
        {
            MI_Result resultLocal = this.ft.GetEnabledChannels(this,
                optionName,
                out channels,
                bufferLength,
                out channelCount,
                out flags);
            return resultLocal;
        }

        public MI_Result Clone(
            MI_OperationOptions newOperationOptions
            )
        {
            MI_Result resultLocal = this.ft.Clone(this,
                newOperationOptions);
            return resultLocal;
        }

        private MI_OperationOptionsFT ft { get { return this.mft.Value; } }
        private MI_OperationOptionsFT MarshalFT()
        {
            MI_OperationOptionsFT res = new MI_OperationOptionsFT();
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

                ftPtr = *((IntPtr*)((byte*)structurePtr + MI_OperationOptionsMembersFTOffset));
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
        public class MI_OperationOptionsFT
        {
            public MI_OperationOptions_Delete Delete;
            public MI_OperationOptions_SetString SetString;
            public MI_OperationOptions_SetNumber SetNumber;
            public MI_OperationOptions_SetCustomOption SetCustomOption;
            public MI_OperationOptions_GetString GetString;
            public MI_OperationOptions_GetNumber GetNumber;
            public MI_OperationOptions_GetOptionCount GetOptionCount;
            public MI_OperationOptions_GetOptionAt GetOptionAt;
            public MI_OperationOptions_GetOption GetOption;
            public MI_OperationOptions_GetEnabledChannels GetEnabledChannels;
            public MI_OperationOptions_Clone Clone;
            public MI_OperationOptions_SetInterval SetInterval;
            public MI_OperationOptions_GetInterval GetInterval;

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate void MI_OperationOptions_Delete(
                MI_OperationOptionsPtr options
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_OperationOptions_SetString(
                MI_OperationOptionsPtr options,
                string optionName,
                string value,
                MI_OperationOptionsFlags flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_OperationOptions_SetNumber(
                MI_OperationOptionsPtr options,
                string optionName,
                UInt32 value,
                MI_OperationOptionsFlags flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_OperationOptions_SetCustomOption(
                MI_OperationOptionsPtr options,
                string optionName,
                MI_Type valueType,
                [In, Out] MI_Value.MIValueBlock value,
                [MarshalAs(UnmanagedType.U1)] bool mustComply,
                MI_OperationOptionsFlags flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_OperationOptions_GetString(
                MI_OperationOptionsPtr options,
                string optionName,
                [In, Out] MI_String value,
                out UInt32 index,
                out MI_OperationOptionsFlags flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_OperationOptions_GetNumber(
                MI_OperationOptionsPtr options,
                string optionName,
                out UInt32 value,
                out UInt32 index,
                out MI_OperationOptionsFlags flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_OperationOptions_GetOptionCount(
                MI_OperationOptionsPtr options,
                out UInt32 count
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_OperationOptions_GetOptionAt(
                MI_OperationOptionsPtr options,
                UInt32 index,
                [In, Out] MI_String optionName,
                [In, Out] MI_Value.MIValueBlock value,
                out MI_Type type,
                out MI_OperationOptionsFlags flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_OperationOptions_GetOption(
                MI_OperationOptionsPtr options,
                string optionName,
                [In, Out] MI_Value.MIValueBlock value,
                out MI_Type type,
                out UInt32 index,
                out MI_OperationOptionsFlags flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_OperationOptions_GetEnabledChannels(
                MI_OperationOptionsPtr options,
                string optionName,
                out UInt32 channels,
                UInt32 bufferLength,
                out UInt32 channelCount,
                out MI_OperationOptionsFlags flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_OperationOptions_Clone(
                MI_OperationOptionsPtr self,
                [In, Out] MI_OperationOptionsPtr newOperationOptions
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_OperationOptions_SetInterval(
                MI_OperationOptionsPtr options,
                string optionName,
                ref MI_Interval value,
                MI_OperationOptionsFlags flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_OperationOptions_GetInterval(
                MI_OperationOptionsPtr options,
                string optionName,
                ref MI_Interval value,
                out UInt32 index,
                out MI_OperationOptionsFlags flags
                );
        }
    }
}
