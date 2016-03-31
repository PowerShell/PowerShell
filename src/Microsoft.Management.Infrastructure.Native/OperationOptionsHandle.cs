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
    internal class OperationOptionsHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public NativeObject.MI_OperationOptions miOperationOptions;
        internal OperationOptionsHandle()
        {
            // 
            //this.miOperationOptions = new NativeObject.MI_OperationOptions();
        }

        internal void AssertValidInternalState()
        {
            throw new NotImplementedException();
        }
        public void Dispose()
        {
            throw new NotImplementedException();
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
