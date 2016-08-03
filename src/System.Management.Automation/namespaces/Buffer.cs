
//
// NOTE: A vast majority of this code was copied from BCL in
// Namespace: System
//

using BCLDebug = System.Diagnostics.Debug;

namespace Microsoft.PowerShell.Commands.Internal
{
    //Only contains static methods.  Does not require serialization

#if CORECLR
    // Use stub for ReliabilityContractAttribute
    using Microsoft.PowerShell.CoreClr.Stubs;
#else
    using System.Runtime.ConstrainedExecution;
#endif

    [System.Runtime.InteropServices.ComVisible(true)]
    internal static class Buffer
    {
        // This is ported from the optimized CRT assembly in memchr.asm. The JIT generates 
        // pretty good code here and this ends up being within a couple % of the CRT asm.
        // It is however cross platform as the CRT hasn't ported their fast version to 64-bit
        // platforms.
        //
        internal static unsafe int IndexOfByte(byte* src, byte value, int index, int count)
        {
            BCLDebug.Assert(src != null, "src should not be null");

            byte* pByte = src + index;

            // Align up the pointer to sizeof(int).
            while (((int)pByte & 3) != 0)
            {
                if (count == 0)
                    return -1;
                else if (*pByte == value)
                    return (int)(pByte - src);

                count--;
                pByte++;
            }

            // Fill comparer with value byte for comparisons
            //
            // comparer = 0/0/value/value
            uint comparer = (((uint)value << 8) + (uint)value);
            // comparer = value/value/value/value
            comparer = (comparer << 16) + comparer;

            // Run through buffer until we hit a 4-byte section which contains
            // the byte we're looking for or until we exhaust the buffer.
            while (count > 3)
            {
                // Test the buffer for presence of value. comparer contains the byte
                // replicated 4 times.
                uint t1 = *(uint*)pByte;
                t1 = t1 ^ comparer;
                uint t2 = 0x7efefeff + t1;
                t1 = t1 ^ 0xffffffff;
                t1 = t1 ^ t2;
                t1 = t1 & 0x81010100;

                // if t1 is zero then these 4-bytes don't contain a match
                if (t1 != 0)
                {
                    // We've found a match for value, figure out which position it's in.
                    int foundIndex = (int)(pByte - src);
                    if (pByte[0] == value)
                        return foundIndex;
                    else if (pByte[1] == value)
                        return foundIndex + 1;
                    else if (pByte[2] == value)
                        return foundIndex + 2;
                    else if (pByte[3] == value)
                        return foundIndex + 3;
                }

                count -= 4;
                pByte += 4;
            }

            // Catch any bytes that might be left at the tail of the buffer
            while (count > 0)
            {
                if (*pByte == value)
                    return (int)(pByte - src);

                count--;
                pByte++;
            }

            // If we don't have a match return -1;
            return -1;
        }

        internal static unsafe void ZeroMemory(byte* src, long len)
        {
            while (len-- > 0)
                *(src + len) = 0;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static unsafe void memcpy(byte* src, int srcIndex, byte[] dest, int destIndex, int len)
        {
            BCLDebug.Assert((srcIndex >= 0) && (destIndex >= 0) && (len >= 0), "Index and length must be non-negative!");
            BCLDebug.Assert(dest.Length - destIndex >= len, "not enough bytes in dest");
            // If dest has 0 elements, the fixed statement will throw an 
            // IndexOutOfRangeException.  Special-case 0-byte copies.
            if (len == 0)
                return;
            fixed (byte* pDest = dest)
            {
                memcpyimpl(src + srcIndex, pDest + destIndex, len);
            }
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static unsafe void memcpy(byte[] src, int srcIndex, byte* pDest, int destIndex, int len)
        {
            BCLDebug.Assert((srcIndex >= 0) && (destIndex >= 0) && (len >= 0), "Index and length must be non-negative!");
            BCLDebug.Assert(src.Length - srcIndex >= len, "not enough bytes in src");
            // If dest has 0 elements, the fixed statement will throw an 
            // IndexOutOfRangeException.  Special-case 0-byte copies.
            if (len == 0)
                return;
            fixed (byte* pSrc = src)
            {
                memcpyimpl(pSrc + srcIndex, pDest + destIndex, len);
            }
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static unsafe void memcpy(char* pSrc, int srcIndex, char* pDest, int destIndex, int len)
        {
            BCLDebug.Assert((srcIndex >= 0) && (destIndex >= 0) && (len >= 0), "Index and length must be non-negative!");

            // No boundary check for buffer overruns - dangerous
            if (len == 0)
                return;
            memcpyimpl((byte*)(char*)(pSrc + srcIndex), (byte*)(char*)(pDest + destIndex), len * 2);
        }

        // Note - using a long instead of an int for the length parameter
        // slows this method down by ~18%.
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static unsafe void memcpyimpl(byte* src, byte* dest, int len)
        {
            BCLDebug.Assert(len >= 0, "Negative length in memcopy!");

            // It turns out that on AMD64 it is faster to not be careful of alignment issues.
            // On IA64 it is necessary to be careful... Oh well. When we do the IA64 push we 
            // can work on this implementation.
#if IA64
            long dstAlign = 8 - (((long)dest) & 7); // number of bytes to copy before dest is 8-byte aligned
            
            while ((dstAlign > 0) && (len > 0))
            {
                *dest++ = *src++;
                
                len--;
                dstAlign--;
            }

            long srcAlign = 8 - (((long)src) & 7);

            if (len > 0)
            {
                if (srcAlign != 8)
                {
                    if (4 == srcAlign)
                    {
                        while (len >= 4)
                        {
                            ((int*)dest)[0] = ((int*)src)[0];
                            dest += 4;
                            src  += 4;
                            len  -= 4;
                        }

                        srcAlign = 2;   // fall through to 2-byte copies
                    }

                    if ((2 == srcAlign) || (6 == srcAlign))
                    {
                        while (len >= 2)
                        {
                            ((short*)dest)[0] = ((short*)src)[0];
                            dest += 2;
                            src  += 2;
                            len  -= 2;
                        }
                    }

                    while (len-- > 0)
                    {
                        *dest++ = *src++;
                    }
                }
                else
                {
                    if (len >= 16) 
                    {
                        do 
                        {
                            ((long*)dest)[0] = ((long*)src)[0];
                            ((long*)dest)[1] = ((long*)src)[1];
                            dest += 16;
                            src += 16;
                        } while ((len -= 16) >= 16);
                    }
                    if (len > 0)  // protection against negative len and optimization for len==16*N
                    {
                       if ((len & 8) != 0) 
                       {
                           ((long*)dest)[0] = ((long*)src)[0];
                           dest += 8;
                           src += 8;
                       }
                       if ((len & 4) != 0) 
                       {
                           ((int*)dest)[0] = ((int*)src)[0];
                           dest += 4;
                           src += 4;
                       }
                       if ((len & 2) != 0) 
                       {
                           ((short*)dest)[0] = ((short*)src)[0];
                           dest += 2;
                           src += 2;
                       }
                       if ((len & 1) != 0)
                       {
                           *dest++ = *src++;
                       }
                    }
                }
            }

#else
            // AMD64 implementation uses longs instead of ints where possible
            //
            // <STRIP>This is a faster memcpy implementation, from 
            // COMString.cpp.  For our strings, this beat the processor's 
            // repeat & move single byte instruction, which memcpy expands into.  
            // (You read that correctly.)
            // This is 3x faster than a simple while loop copying byte by byte, 
            // for large copies.</STRIP>
            if (len >= 16)
            {
                do
                {
#if AMD64
                    ((long*)dest)[0] = ((long*)src)[0];
                    ((long*)dest)[1] = ((long*)src)[1];
#else                    
                    ((int*)dest)[0] = ((int*)src)[0];
                    ((int*)dest)[1] = ((int*)src)[1];
                    ((int*)dest)[2] = ((int*)src)[2];
                    ((int*)dest)[3] = ((int*)src)[3];
#endif
                    dest += 16;
                    src += 16;
                } while ((len -= 16) >= 16);
            }
            if (len > 0)  // protection against negative len and optimization for len==16*N
            {
                if ((len & 8) != 0)
                {
#if AMD64
                    ((long*)dest)[0] = ((long*)src)[0];
#else
                    ((int*)dest)[0] = ((int*)src)[0];
                    ((int*)dest)[1] = ((int*)src)[1];
#endif
                    dest += 8;
                    src += 8;
                }
                if ((len & 4) != 0)
                {
                    ((int*)dest)[0] = ((int*)src)[0];
                    dest += 4;
                    src += 4;
                }
                if ((len & 2) != 0)
                {
                    ((short*)dest)[0] = ((short*)src)[0];
                    dest += 2;
                    src += 2;
                }
                if ((len & 1) != 0)
                    *dest++ = *src++;
            }

#endif // IA64
        }
    }
}
