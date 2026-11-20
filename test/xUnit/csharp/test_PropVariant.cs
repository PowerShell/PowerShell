// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX

using System;
using System.Runtime.InteropServices;

using Microsoft.PowerShell;
using Xunit;

namespace PSTests.Parallel
{
    public static class PropVariantTests
    {
        /// <summary>
        /// PropVariant is passed to PropVariantClear as a native PROPVARIANT, which zeroes the whole structure.
        /// If the managed object is smaller than that, the clear overwrites the 8 bytes after the object, which
        /// is the header of the next object on the managed heap. The header holds the sync block index or the hash
        /// code of that object, and a COM object that loses its sync block causes the runtime to fail with
        /// "Internal CLR error (0x80131506)". This is how PowerShell crashed intermittently on startup, as the
        /// taskbar jump list creates a PropVariant and a COM object on a background thread.
        /// </summary>
        [Fact]
        public static void PropVariantHasTheSameSizeAsTheNativeStructure()
        {
            // sizeof(PROPVARIANT) is 24 bytes on 64-bit and 16 bytes on 32-bit.
            int expectedSize = IntPtr.Size == 8 ? 24 : 16;

            Assert.Equal(expectedSize, Marshal.SizeOf<PropVariant>());
        }
    }
}

#endif
