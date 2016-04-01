using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace NativeObject
{
    [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public struct MI_ClassDeclPtr
    {
        public IntPtr ptr;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public struct MI_ClassDeclOutPtr
    {
        public IntPtr ptr;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public class MI_ClassDecl
    {
        [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
        private struct MI_ClassDeclMembers
        {
            public UInt32 flags;
            public UInt32 code;
            public string name;
            public IntPtr qualifiers;
            public UInt32 numQualifiers;
            public IntPtr properties;
            public UInt32 numProperties;
            public UInt32 size;
            public string superClass;
            public MI_ClassDeclPtr superClassDecl;
            public IntPtr methods;
            public UInt32 numMethods;
            public IntPtr schema;
            public IntPtr providerFT;
            public MI_ClassPtr owningClass;
        }

        // Marshal implements these with Reflection - pay this hit only once
        private static int MI_ClassDeclMembersSize = Marshal.SizeOf<MI_ClassDeclMembers>();

        private MI_ClassDeclPtr ptr;
        private bool isDirect;

        ~MI_ClassDecl()
        {
            Marshal.FreeHGlobal(this.ptr.ptr);
        }

        private MI_ClassDecl(bool isDirect)
        {
            this.isDirect = isDirect;

            var necessarySize = this.isDirect ? MI_ClassDeclMembersSize : NativeMethods.IntPtrSize;
            this.ptr.ptr = Marshal.AllocHGlobal(necessarySize);

            unsafe
            {
                NativeMethods.memset((byte*)this.ptr.ptr, 0, (uint)necessarySize);
            }
        }

        public static MI_ClassDecl NewDirectPtr()
        {
            return new MI_ClassDecl(true);
        }

        public static MI_ClassDecl NewIndirectPtr()
        {
            return new MI_ClassDecl(false);
        }

        public static MI_ClassDecl NewFromDirectPtr(IntPtr ptr)
        {
            var res = new MI_ClassDecl(false);
            Marshal.WriteIntPtr(res.ptr.ptr, ptr);
            return res;
        }

        public static implicit operator MI_ClassDeclPtr(MI_ClassDecl instance)
        {
            // If the indirect pointer is zero then the object has not
            // been initialized and it is not valid to refer to its data
            if (instance != null && instance.Ptr == IntPtr.Zero)
            {
                throw new InvalidCastException();
            }

            return new MI_ClassDeclPtr() { ptr = instance == null ? IntPtr.Zero : instance.Ptr };
        }

        public static implicit operator MI_ClassDeclOutPtr(MI_ClassDecl instance)
        {
            // We are not currently supporting the ability to get the address
            // of our direct pointer, though it is technically feasible 
            if (instance != null && instance.isDirect)
            {
                throw new InvalidCastException();
            }

            return new MI_ClassDeclOutPtr() { ptr = instance == null ? IntPtr.Zero : instance.ptr.ptr };
        }

        public static MI_ClassDecl Null { get { return null; } }
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
    }

}
