// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Microsoft.PowerShell;
using Xunit;

namespace PSTests.Parallel
{
    public static class PropVariantTests
    {
        // How many times to try to allocate an object directly after a PropVariant before giving up.
        private const int MaxAttempts = 100;

        /// <summary>
        /// PropVariant is passed to PropVariantClear as a native PROPVARIANT (24 bytes on 64-bit), which zeroes the
        /// whole structure. If the managed object is smaller than that, the clear overwrites the 8 bytes after the
        /// object, which is the header of the next object on the managed heap. The header holds the sync block index
        /// or the hash code of that object, so for a COM object (which needs its sync block) the runtime later fails
        /// with "Internal CLR error (0x80131506)". This is how PowerShell crashed intermittently on startup, as
        /// the taskbar jump list creates a PropVariant and a COM object on a background thread.
        /// </summary>
        [SkippableFact]
        public static void ClearingAPropVariantDoesNotCorruptTheNextObject()
        {
            Skip.IfNot(Platform.IsWindowsDesktop);

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                if (TryClearPropVariantBeforeAnObject(out int hashCodeBefore, out int hashCodeAfter))
                {
                    // The hash code of an object is stored in its header, so it changes if the header is zeroed.
                    Assert.Equal(hashCodeBefore, hashCodeAfter);
                    return;
                }
            }

            Assert.Fail($"Failed to allocate an object directly after a {nameof(PropVariant)} in {MaxAttempts} attempts.");
        }

        [SkippableFact]
        public static void PropVariantHasTheSameSizeAsTheNativeStructure()
        {
            Skip.IfNot(Platform.IsWindowsDesktop);

            // sizeof(PROPVARIANT) is 24 bytes on 64-bit and 16 bytes on 32-bit.
            int expectedSize = IntPtr.Size == 8 ? 24 : 16;

            Assert.Equal(expectedSize, Marshal.SizeOf<PropVariant>());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool TryClearPropVariantBeforeAnObject(out int hashCodeBefore, out int hashCodeAfter)
        {
            // Allocate consecutively so that they are normally next to each other on the heap.
            var first = new PropVariant("first");
            var second = new PropVariant("second");
            var victim = new object();

            // Store the hash code of the object in its header.
            hashCodeBefore = RuntimeHelpers.GetHashCode(victim);
            hashCodeAfter = hashCodeBefore;

            // Only the object directly after a PropVariant can be overwritten, so check that victim
            // is that object by comparing the distances between the three consecutive objects.
            nint size = Unsafe.ByteOffset(ref GetData(first), ref GetData(second));
            bool isAdjacent = size > 0 && size == Unsafe.ByteOffset(ref GetData(second), ref GetData(victim));

            // Clearing the first overwrites the header of the second, which is harmless.
            // Clearing the second overwrites the header of the victim when the PropVariant is too small.
            first.Dispose();
            second.Dispose();

            if (isAdjacent)
            {
                hashCodeAfter = RuntimeHelpers.GetHashCode(victim);
            }

            GC.KeepAlive(victim);
            return isAdjacent;
        }

        // Returns a reference to the first field of an object, which is stored directly after its method table pointer.
        private static ref byte GetData(object value) => ref Unsafe.As<RawData>(value).Data;

        // Never instantiated: only used to reinterpret an arbitrary object so that the address of its data can be compared.
        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Only used to reinterpret other objects")]
        private sealed class RawData
        {
            public byte Data;
        }
    }
}
