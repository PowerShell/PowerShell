using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace NativeObject
{
    [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public struct MI_InstancePtr
    {
        public IntPtr ptr;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public struct MI_InstanceOutPtr
    {
        public IntPtr ptr;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public class MI_Instance
    {
        public MI_Result GetElement(
            string name,
            out MI_Value value,
            out MI_Type type,
            out MI_Flags flags,
            out UInt32 index
            )
        {
            MI_Value valueLocal = new MI_Value();
            MI_Result resultLocal = this.ft.GetElement(this,
                name,
                valueLocal,
                out type,
                out flags,
                out index);
            value = valueLocal;
            return resultLocal;
        }

        public MI_Result GetElementAt(
            UInt32 index,
            out string name,
            out MI_Value value,
            out MI_Type type,
            out MI_Flags flags
            )
        {
            MI_Value valueLocal = new MI_Value();
            MI_String nameLocal = MI_String.NewIndirectPtr();
            MI_Result resultLocal = this.ft.GetElementAt(this,
                index,
                nameLocal,
                valueLocal,
                out type,
                out flags);

            value = valueLocal;
            name = nameLocal.Value;
            return resultLocal;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
        private struct MI_InstanceMembers
        {
            public IntPtr ft;
            public MI_ClassDeclPtr classDecl;
            public string serverName;
            public string nameSpace;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public IntPtr[] reserved;
        }

        // Marshal implements these with Reflection - pay this hit only once
        private static int MI_InstanceMembersFTOffset = (int)Marshal.OffsetOf<MI_InstanceMembers>("ft");
        private static int MI_InstanceMembersSize = Marshal.SizeOf<MI_InstanceMembers>();

        private MI_InstancePtr ptr;
        private bool isDirect;
        private Lazy<MI_InstanceFT> mft;

        ~MI_Instance()
        {
            Marshal.FreeHGlobal(this.ptr.ptr);
        }

        private MI_Instance(bool isDirect)
        {
            this.isDirect = isDirect;
            this.mft = new Lazy<MI_InstanceFT>(this.MarshalFT);

            var necessarySize = this.isDirect ? MI_InstanceMembersSize : NativeMethods.IntPtrSize;
            this.ptr.ptr = Marshal.AllocHGlobal(necessarySize);

            unsafe
            {
                NativeMethods.memset((byte*)this.ptr.ptr, 0, (uint)necessarySize);
            }
        }

        public static MI_Instance NewDirectPtr()
        {
            return new MI_Instance(true);
        }

        public static MI_Instance NewIndirectPtr()
        {
            return new MI_Instance(false);
        }

        public static MI_Instance NewFromDirectPtr(IntPtr ptr)
        {
            var res = new MI_Instance(false);
            Marshal.WriteIntPtr(res.ptr.ptr, ptr);
            return res;
        }

        public static implicit operator MI_InstancePtr(MI_Instance instance)
        {
            // If the indirect pointer is zero then the object has not
            // been initialized and it is not valid to refer to its data
            if (instance != null && instance.Ptr == IntPtr.Zero)
            {
                throw new InvalidCastException();
            }

            return new MI_InstancePtr() { ptr = instance == null ? IntPtr.Zero : instance.Ptr };
        }

        public static implicit operator MI_InstanceOutPtr(MI_Instance instance)
        {
            // We are not currently supporting the ability to get the address
            // of our direct pointer, though it is technically feasible 
            if (instance != null && instance.isDirect)
            {
                throw new InvalidCastException();
            }

            return new MI_InstanceOutPtr() { ptr = instance == null ? IntPtr.Zero : instance.ptr.ptr };
        }

        public static MI_Instance Null { get { return null; } }
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

        public MI_Result Clone(
            out MI_Instance newInstance
            )
        {
            MI_Instance newInstanceLocal = MI_Instance.NewIndirectPtr();

            MI_Result resultLocal = this.ft.Clone(this,
                newInstanceLocal);

            newInstance = newInstanceLocal;
            return resultLocal;
        }

        public MI_Result Destruct()
        {
            return this.ft.Destruct(this);
        }

        public MI_Result Delete()
        {
            return this.ft.Delete(this);
        }

        public MI_Result IsA(
            MI_ClassDecl classDecl,
            out bool flag
            )
        {
            MI_Result resultLocal = this.ft.IsA(this,
                classDecl,
                out flag);
            return resultLocal;
        }

        public MI_Result GetClassName(
            out string className
            )
        {
            MI_String classNameLocal = MI_String.NewIndirectPtr();

            MI_Result resultLocal = this.ft.GetClassName(this,
                classNameLocal);

            className = classNameLocal.Value;
            return resultLocal;
        }

        public MI_Result SetNameSpace(
            string nameSpace
            )
        {
            MI_Result resultLocal = this.ft.SetNameSpace(this,
                nameSpace);
            return resultLocal;
        }

        public MI_Result GetNameSpace(
            out string nameSpace
            )
        {
            MI_String nameSpaceLocal = MI_String.NewIndirectPtr();

            MI_Result resultLocal = this.ft.GetNameSpace(this,
                nameSpaceLocal);

            nameSpace = nameSpaceLocal.Value;
            return resultLocal;
        }

        public MI_Result GetElementCount(
            out UInt32 count
            )
        {
            MI_Result resultLocal = this.ft.GetElementCount(this,
                out count);
            return resultLocal;
        }

        public MI_Result AddElement(
            string name,
            MI_Value value,
            MI_Type type,
            MI_Flags flags
            )
        {
            MI_Result resultLocal = this.ft.AddElement(this,
                name,
                value,
                type,
                flags);
            return resultLocal;
        }

        public MI_Result SetElement(
            string name,
            MI_Value value,
            MI_Type type,
            MI_Flags flags
            )
        {
            MI_Result resultLocal = this.ft.SetElement(this,
                name,
                value,
                type,
                flags);
            return resultLocal;
        }

        public MI_Result SetElementAt(
            UInt32 index,
            MI_Value value,
            MI_Type type,
            MI_Flags flags
            )
        {
            MI_Result resultLocal = this.ft.SetElementAt(this,
                index,
                value,
                type,
                flags);
            return resultLocal;
        }

        public MI_Result ClearElement(
            string name
            )
        {
            MI_Result resultLocal = this.ft.ClearElement(this,
                name);
            return resultLocal;
        }

        public MI_Result ClearElementAt(
            UInt32 index
            )
        {
            MI_Result resultLocal = this.ft.ClearElementAt(this,
                index);
            return resultLocal;
        }

        public MI_Result GetServerName(
            out string name
            )
        {
            MI_String nameLocal = MI_String.NewIndirectPtr();

            MI_Result resultLocal = this.ft.GetServerName(this,
                nameLocal);

            name = nameLocal.Value;
            return resultLocal;
        }

        public MI_Result SetServerName(
            string name
            )
        {
            MI_Result resultLocal = this.ft.SetServerName(this,
                name);
            return resultLocal;
        }

        public MI_Result GetClass(
            out MI_Class instanceClass
            )
        {
            MI_Class instanceClassLocal = MI_Class.NewIndirectPtr();

            MI_Result resultLocal = this.ft.GetClass(this,
                instanceClassLocal);

            instanceClass = instanceClassLocal;
            return resultLocal;
        }

        private MI_InstanceFT ft { get { return this.mft.Value; } }
        private MI_InstanceFT MarshalFT()
        {
            MI_InstanceFT res = new MI_InstanceFT();
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

                ftPtr = *((IntPtr*)((byte*)structurePtr + MI_InstanceMembersFTOffset));
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
        public class MI_InstanceFT
        {
            public MI_Instance_Clone Clone;
            public MI_Instance_Destruct Destruct;
            public MI_Instance_Delete Delete;
            public MI_Instance_IsA IsA;
            public MI_Instance_GetClassName GetClassName;
            public MI_Instance_SetNameSpace SetNameSpace;
            public MI_Instance_GetNameSpace GetNameSpace;
            public MI_Instance_GetElementCount GetElementCount;
            public MI_Instance_AddElement AddElement;
            public MI_Instance_SetElement SetElement;
            public MI_Instance_SetElementAt SetElementAt;
            public MI_Instance_GetElement GetElement;
            public MI_Instance_GetElementAt GetElementAt;
            public MI_Instance_ClearElement ClearElement;
            public MI_Instance_ClearElementAt ClearElementAt;
            public MI_Instance_GetServerName GetServerName;
            public MI_Instance_SetServerName SetServerName;
            public MI_Instance_GetClass GetClass;

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Instance_Clone(
                MI_InstancePtr self,
                [In, Out] MI_InstanceOutPtr newInstance
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Instance_Destruct(
                MI_InstancePtr self
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Instance_Delete(
                MI_InstancePtr self
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Instance_IsA(
                MI_InstancePtr self,
                [In, Out] MI_ClassDeclPtr classDecl,
                [MarshalAs(UnmanagedType.U1)] out bool flag
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Instance_GetClassName(
                MI_InstancePtr self,
                [In, Out] MI_String className
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Instance_SetNameSpace(
                MI_InstancePtr self,
                string nameSpace
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Instance_GetNameSpace(
                MI_InstancePtr self,
                [In, Out] MI_String nameSpace
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Instance_GetElementCount(
                MI_InstancePtr self,
                out UInt32 count
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Instance_AddElement(
                MI_InstancePtr self,
                [MarshalAs(MI_PlatformSpecific.AppropriateStringType)]string name,
                [In, Out] MI_Value.MIValueBlock value,
                MI_Type type,
                MI_Flags flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Instance_SetElement(
                MI_InstancePtr self,
                string name,
                [In, Out] MI_Value.MIValueBlock value,
                MI_Type type,
                MI_Flags flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Instance_SetElementAt(
                MI_InstancePtr self,
                UInt32 index,
                [In, Out] MI_Value.MIValueBlock value,
                MI_Type type,
                MI_Flags flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Instance_GetElement(
                MI_InstancePtr self,
                [MarshalAs(MI_PlatformSpecific.AppropriateStringType)] string name,
                [In, Out] MI_Value.MIValueBlock value,
                out MI_Type type,
                out MI_Flags flags,
                out UInt32 index
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Instance_GetElementAt(
                MI_InstancePtr self,
                UInt32 index,
                [In, Out] MI_String name,
                [In, Out] MI_Value.MIValueBlock value,
                out MI_Type type,
                out MI_Flags flags
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Instance_ClearElement(
                MI_InstancePtr self,
                string name
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Instance_ClearElementAt(
                MI_InstancePtr self,
                UInt32 index
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Instance_GetServerName(
                MI_InstancePtr self,
                [In, Out] MI_String name
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Instance_SetServerName(
                MI_InstancePtr self,
                string name
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Instance_GetClass(
                MI_InstancePtr self,
                [In, Out] MI_ClassOutPtr instanceClass
                );
        }
    }
}
