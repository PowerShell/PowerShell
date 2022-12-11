// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// NOTE: A vast majority of this code was copied from BCL in
// Namespace: Microsoft.Win32
//
/*
 * Notes to PInvoke users:  Getting the syntax exactly correct is crucial, and
 * more than a little confusing.  Here's some guidelines.
 *
 * For handles, you should use a SafeHandle subclass specific to your handle
 * type.
*/

namespace Microsoft.PowerShell.Commands.Internal
{
    using System;
    using System.Security;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Management.Automation;
    using System.Diagnostics.CodeAnalysis;

    /**
     * Win32 encapsulation for MSCORLIB.
     */
    // Remove the default demands for all N/Direct methods with this
    // global declaration on the class.
    //
    [SuppressUnmanagedCodeSecurityAttribute()]
    internal static class Win32Native
    {
        #region PInvoke methods

        [DllImport(PinvokeDllNames.CloseHandleDllName, SetLastError = true)]
        [ResourceExposure(ResourceScope.Machine)]
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr handle);

        #endregion PInvoke Methods
    }
}
