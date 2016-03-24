using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace NativeObject
{
    [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public struct MI_OperationPtr
    {
        public IntPtr ptr;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public struct MI_OperationOutPtr
    {
        public IntPtr ptr;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public class MI_Operation
    {
        [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
        private struct MI_OperationMembers
        {
            public UInt64 reserved1;
            public IntPtr reserved2;
            public IntPtr ft;
        }

        // Marshal implements these with Reflection - pay this hit only once
        private static int MI_OperationMembersFTOffset = (int)Marshal.OffsetOf(typeof(MI_OperationMembers), "ft");
        private static int MI_OperationMembersSize = Marshal.SizeOf(typeof(MI_OperationMembers));

        private MI_OperationPtr ptr;
        private bool isDirect;
        private Lazy<MI_OperationFT> mft;

        ~MI_Operation()
        {
            Marshal.FreeHGlobal(this.ptr.ptr);
        }

        private MI_Operation(bool isDirect)
        {
            this.isDirect = isDirect;
            this.mft = new Lazy<MI_OperationFT>(this.MarshalFT);

            var necessarySize = this.isDirect ? MI_OperationMembersSize : NativeMethods.IntPtrSize;
            this.ptr.ptr = Marshal.AllocHGlobal(necessarySize);

            unsafe
            {
                NativeMethods.memset((byte*)this.ptr.ptr, 0, (uint)necessarySize);
            }
        }

        public static MI_Operation NewDirectPtr()
        {
            return new MI_Operation(true);
        }

        public static MI_Operation NewIndirectPtr()
        {
            return new MI_Operation(false);
        }

        public static MI_Operation NewFromDirectPtr(IntPtr ptr)
        {
            var res = new MI_Operation(false);
            Marshal.WriteIntPtr(res.ptr.ptr, ptr);
            return res;
        }

        public static implicit operator MI_OperationPtr(MI_Operation instance)
        {
            // If the indirect pointer is zero then the object has not
            // been initialized and it is not valid to refer to its data
            if(instance != null && instance.Ptr == IntPtr.Zero)
            {
                throw new InvalidCastException();
            }

            return new MI_OperationPtr() { ptr = instance == null ? IntPtr.Zero : instance.Ptr };
        }

        public static implicit operator MI_OperationOutPtr(MI_Operation instance)
        {
            // We are not currently supporting the ability to get the address
            // of our direct pointer, though it is technically feasible 
            if(instance != null && instance.isDirect)
            {
                throw new InvalidCastException();
            }

            return new MI_OperationOutPtr() { ptr = instance == null ? IntPtr.Zero : instance.ptr.ptr };
        }

        public static MI_Operation Null { get { return null; } }
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

        public MI_Result Cancel(
            MI_CancellationReason reason
            )
        {
            MI_Result resultLocal = this.ft.Cancel(this,
                reason);
            return resultLocal;
        }

        public MI_Result GetSession(
            MI_Session session
            )
        {
            MI_Result resultLocal = this.ft.GetSession(this,
                session);
            return resultLocal;
        }

        public MI_Result GetInstance(
            out MI_Instance instance,
            out bool moreResults,
            out MI_Result result,
            out string errorMessage,
            out MI_Instance completionDetails
            )
        {
            MI_Instance instanceLocal = MI_Instance.NewIndirectPtr();
            MI_String errorMessageLocal = MI_String.NewIndirectPtr();
            MI_Instance completionDetailsLocal = MI_Instance.NewIndirectPtr();

            MI_Result resultLocal = this.ft.GetInstance(this,
                instanceLocal,
                out moreResults,
                out result,
                errorMessageLocal,
                completionDetailsLocal);

            instance = instanceLocal;
            errorMessage = errorMessageLocal.Value;
            completionDetails = completionDetailsLocal;
            return resultLocal;
        }

        public MI_Result GetIndication(
            out MI_Instance instance,
            out string bookmark,
            out string machineID,
            out bool moreResults,
            out MI_Result result,
            out string errorMessage,
            out MI_Instance completionDetails
            )
        {
            MI_Instance instanceLocal = MI_Instance.NewIndirectPtr();
            MI_String bookmarkLocal = MI_String.NewIndirectPtr();
            MI_String machineIDLocal = MI_String.NewIndirectPtr();
            MI_String errorMessageLocal = MI_String.NewIndirectPtr();
            MI_Instance completionDetailsLocal = MI_Instance.NewIndirectPtr();

            MI_Result resultLocal = this.ft.GetIndication(this,
                instanceLocal,
                bookmarkLocal,
                machineIDLocal,
                out moreResults,
                out result,
                errorMessageLocal,
                completionDetailsLocal);

            instance = instanceLocal;
            bookmark = bookmarkLocal.Value;
            machineID = machineIDLocal.Value;
            errorMessage = errorMessageLocal.Value;
            completionDetails = completionDetailsLocal;
            return resultLocal;
        }

        public MI_Result GetClass(
            out MI_Class classResult,
            out bool moreResults,
            out MI_Result result,
            out string errorMessage,
            out MI_Instance completionDetails
            )
        {
            MI_Class classResultLocal = MI_Class.NewIndirectPtr();
            MI_String errorMessageLocal = MI_String.NewIndirectPtr();
            MI_Instance completionDetailsLocal = MI_Instance.NewIndirectPtr();

            MI_Result resultLocal = this.ft.GetClass(this,
                classResultLocal,
                out moreResults,
                out result,
                errorMessageLocal,
                completionDetailsLocal);

            classResult = classResultLocal;
            errorMessage = errorMessageLocal.Value;
            completionDetails = completionDetailsLocal;
            return resultLocal;
        }

        private MI_OperationFT ft { get { return this.mft.Value; } }
        private MI_OperationFT MarshalFT() 
        {
            MI_OperationFT res = new MI_OperationFT();
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

                ftPtr = *((IntPtr*)((byte*)structurePtr + MI_OperationMembersFTOffset));
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
        public class MI_OperationFT
        {
            public MI_Operation_Close Close;
            public MI_Operation_Cancel Cancel;
            public MI_Operation_GetSession GetSession;
            public MI_Operation_GetInstance GetInstance;
            public MI_Operation_GetIndication GetIndication;
            public MI_Operation_GetClass GetClass;

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Operation_Close(
                MI_OperationPtr operation
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Operation_Cancel(
                MI_OperationPtr operation,
                MI_CancellationReason reason
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Operation_GetSession(
                MI_OperationPtr operation,
                [In, Out] MI_SessionPtr session
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Operation_GetInstance(
                MI_OperationPtr operation,
                [In, Out] MI_InstanceOutPtr instance,
                [MarshalAs(UnmanagedType.U1)] out bool moreResults,
                out MI_Result result,
                [In, Out] MI_String errorMessage,
                [In, Out] MI_InstanceOutPtr completionDetails
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Operation_GetIndication(
                MI_OperationPtr operation,
                [In, Out] MI_InstanceOutPtr instance,
                [In, Out] MI_String bookmark,
                [In, Out] MI_String machineID,
                [MarshalAs(UnmanagedType.U1)] out bool moreResults,
                out MI_Result result,
                [In, Out] MI_String errorMessage,
                [In, Out] MI_InstanceOutPtr completionDetails
                );

            [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
            public delegate MI_Result MI_Operation_GetClass(
                MI_OperationPtr operation,
                [In, Out] MI_ClassOutPtr classResult,
                [MarshalAs(UnmanagedType.U1)] out bool moreResults,
                out MI_Result result,
                [In, Out] MI_String errorMessage,
                [In, Out] MI_InstanceOutPtr completionDetails
                );
        }
    }
}
