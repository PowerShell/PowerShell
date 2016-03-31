//using Microsoft.Win32.SafeHandles;
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
    // Creates an instance of the struct MI_Application for passing into Management Infrastructure methods.
    [StructLayout(LayoutKind.Sequential)]
    internal class ApplicationHandle// : SafeHandleZeroOrMinusOneIsInvalid
    {
        //public NativeObject.MI_Application miApp;
        public NativeObject.MI_Application miApp;

        internal ApplicationHandle()
        {
            //this.miApp = new NativeObject.MI_Application();
            this.miApp = NativeObject.MI_Application.NewIndirectPtr();
        }

        internal void AssertValidInternalState()
        {
            throw new NotImplementedException();
        }
        public void Dispose()
        {
            // this is already implemented in the NativeObject destructor for MI_Application.
            //NativeObject.MI_Applicatio.table = Marshal.PtrToStructure<NativeObject.MI_Applicatio.(this.miApp.;
            //MiResult r = table.Close(ref this.miApp);
        }
        internal delegate void OnHandleReleasedDelegate();
        internal MiResult ReleaseHandleAsynchronously(OnHandleReleasedDelegate completionCallback)
        {
            throw new NotImplementedException();
        }
        internal MiResult ReleaseHandleSynchronously()
        {
            throw new NotImplementedException();
        }
    }
}
