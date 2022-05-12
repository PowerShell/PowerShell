// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

//
// NOTE: A vast majority of this code was copied from BCL in
// Namespace: Microsoft.Win32.SafeHandles
//
/*============================================================
**
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
using System.Runtime.ConstrainedExecution;
using System.Security.Permissions;

namespace Microsoft.PowerShell.Commands.Internal
{
    internal sealed class SafeRegistryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // Note: Officially -1 is the recommended invalid handle value for
        // registry keys, but we'll also get back 0 as an invalid handle from
        // RegOpenKeyEx.

        internal SafeRegistryHandle() : base(true) { }

        internal SafeRegistryHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(preexistingHandle);
        }

        [DllImport(PinvokeDllNames.RegCloseKeyDllName),
         SuppressUnmanagedCodeSecurity,
         ResourceExposure(ResourceScope.None)]
        internal static extern int RegCloseKey(IntPtr hKey);

        protected override bool ReleaseHandle()
        {
            // Returns a Win32 error code, 0 for success
            int r = RegCloseKey(handle);
            return r == 0;
        }
    }
}
