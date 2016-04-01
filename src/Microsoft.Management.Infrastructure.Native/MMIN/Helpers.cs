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
    internal class Helpers
    {
        // Methods
        private Helpers()
        {
            throw new NotImplementedException();
        }
        internal static IntPtr GetCurrentSecurityToken()
        {
            throw new NotImplementedException();
        }
        internal static IntPtr StringToHGlobalUni(string s)
        {
            throw new NotImplementedException();
        }
        internal static void ZeroFreeGlobalAllocUnicode(IntPtr s)
        {
            throw new NotImplementedException();
        }
    }
}
