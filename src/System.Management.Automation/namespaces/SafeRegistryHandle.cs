// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
//
// NOTE: A vast majority of this code was copied from BCL in
// ndp\clr\src\BCL\Microsoft\Win32\SafeHandles\SafeRegistryHandle.cs.
// Namespace: Microsoft.Win32.SafeHandles
//
/*============================================================
**
** Class:  SafeRegistryHandle 
**
**
** A wrapper for registry handles
**
** 
===========================================================*/

using System;
using System.Management.Automation;
using System.Security;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

#if CORECLR 
// Use stubs for SafeHandleZeroOrMinusOneIsInvalid and ReliabilityContractAttribute
using Microsoft.PowerShell.CoreClr.Stubs;
#else
using System.Runtime.ConstrainedExecution;
using System.Security.Permissions;
#endif

namespace Microsoft.PowerShell.Commands.Internal
{

    internal sealed class SafeRegistryHandle : SafeHandleZeroOrMinusOneIsInvalid {

        // Note: Officially -1 is the recommended invalid handle value for
        // registry keys, but we'll also get back 0 as an invalid handle from
        // RegOpenKeyEx.

        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode=true)]
        internal SafeRegistryHandle() : base(true) {}

        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode=true)]
        internal SafeRegistryHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle) {
            SetHandle(preexistingHandle);
        }

        [DllImport(PinvokeDllNames.RegCloseKeyDllName),
         SuppressUnmanagedCodeSecurity,
         ResourceExposure(ResourceScope.None),
         ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern int RegCloseKey(IntPtr hKey);
    
        override protected bool ReleaseHandle()
        {
#if LINUX
            return true;
#else
            // Returns a Win32 error code, 0 for success
            int r = RegCloseKey(handle);
            return r == 0;
#endif
        }
    }
}
