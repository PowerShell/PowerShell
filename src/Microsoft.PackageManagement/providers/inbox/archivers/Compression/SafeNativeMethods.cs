//---------------------------------------------------------------------
// <copyright file="SafeNativeMethods.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Archivers.Internal.Compression
{
    using System.Runtime.InteropServices;
    using System.Security;

#if !CORECLR
    [SuppressUnmanagedCodeSecurity]
#endif
    internal static class SafeNativeMethods
    {
#if !CORECLR
        [DllImport("kernel32.dll", SetLastError = true)]
#else
        [DllImport("api-ms-win-core-kernel32-legacy-l1-1-1.dll", SetLastError=true)]
#endif
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DosDateTimeToFileTime(
            short wFatDate, short wFatTime, out long fileTime);

#if !CORECLR
        [DllImport("kernel32.dll", SetLastError = true)]
#else
        [DllImport("api-ms-win-core-kernel32-legacy-l1-1-1.dll", SetLastError=true)]
#endif
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FileTimeToDosDateTime(
            ref long fileTime, out short wFatDate, out short wFatTime);
    }
}
