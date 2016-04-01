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
}
