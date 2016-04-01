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
    internal class OperationCallbackProcessingContext
    {
        // Fields
        //private bool inUserCode;
        //private object managedOperationContext;

        // Methods
        internal OperationCallbackProcessingContext(object managedOperationContext)
        {
            throw new NotImplementedException();
        }

        // Properties
        internal bool InUserCode {[return: MarshalAs(UnmanagedType.U1)] get;[param: MarshalAs(UnmanagedType.U1)] set; }
        internal object ManagedOperationContext { get; }
    }
}
