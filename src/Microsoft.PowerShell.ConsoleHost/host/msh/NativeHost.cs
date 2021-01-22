// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.IO;
using System.Runtime.Loader;
using System.Runtime.InteropServices;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// Provides helper functions to faciliate calling managed code from a native PowerShell host.
    /// </summary>
    public static class NativeHost
    {
        /// <summary>
        /// Load an assembly in memory from unmanaged code.
        /// </summary>
        /// <param name="data">
        /// Unmanaged pointer to assembly data buffer
        /// </param>
        /// <param name="size">
        /// Size in bytes of the assembly data buffer
        /// </param>
        [UnmanagedCallersOnly]
        public static void LoadAssemblyData(IntPtr data, int size)
        {
            byte[] bytes = new byte[size];
            Marshal.Copy(data, bytes, 0, size);
            Stream stream = new MemoryStream(bytes);
            AssemblyLoadContext.Default.LoadFromStream(stream);
        }
    }
}
