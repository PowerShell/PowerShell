using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace NativeObject
{
    [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public struct MI_QualifierSetPtr
    {
        public IntPtr ptr;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public struct MI_QualifierSetOutPtr
    {
        public IntPtr ptr;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public class MI_QualifierSet
    {
        public MI_Result GetQualifier(
            string name,
            out MI_Type qualifierType,
            out MI_Flags qualifierFlags,
            out MI_Value qualifierValue,
            out UInt32 index
            )
        {
            MI_Value qualifierValueLocal = new MI_Value();
            MI_Result resultLocal = this.ft.GetQualifier(this,
                name,
                out qualifierType,
                out qualifierFlags,
                qualifierValueLocal,
                out index);

            qualifierValue = qualifierValueLocal;
            return resultLocal;
        }


        public MI_Result GetQualifierAt(
            UInt32 index,
            out string name,
            out MI_Type qualifierType,
            out MI_Flags qualifierFlags,
            out MI_Value qualifierValue
            )
        {
            MI_String nameLocal = MI_String.NewIndirectPtr();
            MI_Value qualifierValueLocal = new MI_Value();

            MI_Result resultLocal = this.ft.GetQualifierAt(this,
                index,
                nameLocal,
                out qualifierType,
                out qualifierFlags,
                qualifierValueLocal);

            name = nameLocal.Value;
            qualifierValue = qualifierValueLocal;
            return resultLocal;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
        private struct MI_QualifierSetMembers
        {
            public UInt64 reserved1;
            public IntPtr reserved2;
            public IntPtr ft;
        }

        // Marshal implements these with Reflection - pay this hit only once
        private static int MI_QualifierSetMembersFTOffset = (int)Marshal.OffsetOf<MI_QualifierSetMembers>("ft");
        private static int MI_QualifierSetMembersSize = Marshal.SizeOf<MI_QualifierSetMembers>();

        private MI_QualifierSetPtr ptr;
        private bool isDirect;
        private Lazy<MI_QualifierSetFT> mft;

        ~MI_QualifierSet()
        {
            Marshal.FreeHGlobal(this.ptr.ptr);
        }

        private MI_QualifierSet(bool isDirect)
        {
            this.isDirect = isDirect;
            this.mft = new Lazy<MI_QualifierSetFT>(this.MarshalFT);

            var necessarySize = this.isDirect ? MI_QualifierSetMembersSize : NativeMethods.IntPtrSize;
            this.ptr.ptr = Marshal.AllocHGlobal(necessarySize);

            unsafe
            {
                NativeMethods.memset((byte*)this.ptr.ptr, 0, (uint)necessarySize);
            }
        }

        public static MI_QualifierSet NewDirectPtr()
        {
            return new MI_QualifierSet(true);
        }

        public static MI_QualifierSet NewIndirectPtr()
        {
            return new MI_QualifierSet(false);
        }

        public static MI_QualifierSet NewFromDirectPtr(IntPtr ptr)
        {
            var res = new MI_QualifierSet(false);
            Marshal.WriteIntPtr(res.ptr.ptr, ptr);
            return res;
        }

        public static implicit operator MI_QualifierSetPtr(MI_QualifierSet instance)
        {
            // If the indirect pointer is zero then the object has not
            // been initialized and it is not valid to refer to its data
            if (instance != null && instance.Ptr == IntPtr.Zero)
            {
                throw new InvalidCastException();
            }

            return new MI_QualifierSetPtr() { ptr = instance == null ? IntPtr.Zero : instance.Ptr };
        }

        public static implicit operator MI_QualifierSetOutPtr(MI_QualifierSet instance)
        {
            // We are not currently supporting the ability to get the address
            // of our direct pointer, though it is technically feasible 
            if (instance != null && instance.isDirect)
            {
                throw new InvalidCastException();
            }

            return new MI_QualifierSetOutPtr() { ptr = instance == null ? IntPtr.Zero : instance.ptr.ptr };
        }

        public static MI_QualifierSet Null { get { return null; } }
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

        public MI_Result GetQualifierCount(
            out UInt32 count
            )
        {
            MI_Result resultLocal = this.ft.GetQualifierCount(this,
                out count);
            return resultLocal;
        }

        private MI_QualifierSetFT ft { get { return this.mft.Value; } }
        private MI_QualifierSetFT MarshalFT()
        {
            MI_QualifierSetFT res = new MI_QualifierSetFT();
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

                ftPtr = *((IntPtr*)((byte*)structurePtr + MI_QualifierSetMembersFTOffset));
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
        public class MI_QualifierSetFT
        {
            public MI_QualifierSet_GetQualifierCount GetQualifierCount;
            public MI_QualifierSet_GetQualifierAt GetQualifierAt;
            public MI_QualifierSet_GetQualifier GetQualifier;

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_QualifierSet_GetQualifierCount(
                MI_QualifierSetPtr self,
                out UInt32 count
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_QualifierSet_GetQualifierAt(
                MI_QualifierSetPtr self,
                UInt32 index,
                [In, Out] MI_String name,
                out MI_Type qualifierType,
                out MI_Flags qualifierFlags,
                [In, Out] MI_Value.MIValueBlock qualifierValue
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_QualifierSet_GetQualifier(
                MI_QualifierSetPtr self,
                string name,
                out MI_Type qualifierType,
                out MI_Flags qualifierFlags,
                [In, Out] MI_Value.MIValueBlock qualifierValue,
                out UInt32 index
                );
        }
    }
}
