// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace System.Management.Automation.Unicode
{
    /// <summary>
    /// Unicode Simple Case Folding.
    /// </summary>
    internal static partial class SimpleCaseFoldingG2
    {
        //private static ref ushort refL1 => ref L1[0];
        //private static ref ushort refL3 => ref L3[0];
        //private static ushort refL1 = L3[0];
        //private static ushort refL3 = L3[0];

        /// <summary>
        /// Simple case folding of the char (Utf16).
        /// </summary>
        /// <param name="c">Source char.</param>
        /// <returns>
        /// Returns folded char.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static char SimpleCaseFold(char c)
        {
            //var v = Unsafe.Add(ref refL1, 1);
            //var ch = Unsafe.Add(ref refL3, v + (c & 0xFF));
            var v = Unsafe.Add(ref L1[0], c >> 8);
            var ch = Unsafe.Add(ref L3[0], v + (c & 0xFF));

            //var v = L1[c >> 8];
            //var ch = L3[v + (c & 0xFF)];
            //ref ushort L1a = ref L1[0];
            //ref ushort L3a = ref L3[0];
            //var v = Unsafe.Add(ref L1a, c >> 8);
            //var ch = Unsafe.Add(ref L3a, v + (c & 0xFF));
            //var v = Unsafe.Add(ref refL1, c >> 8);
            //var ch = Unsafe.Add(ref refL3, v + (c & 0xFF));
            //var ch = Unsafe.Add(ref refL3, Unsafe.Add(ref refL1, c >> 8) + (c & 0xFF));
            //ushort ch = (ushort)v;

            return ch == 0 ? c : Unsafe.As<ushort, char>(ref ch);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static char SimpleCaseFold1(char c)
        {
            ushort v    = L1[c >> 8];
            v           = L3[v + (c & 0xFF)];

            return v == 0 ? c : (char)v;
        }

        /// <summary>
        ///  Simple case folding of the string.
        /// </summary>
        /// <param name="source">Source string.</param>
        /// <returns>
        /// Returns folded string.
        /// </returns>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SimpleCaseFold_g2(this string source)
        {
            return string.Create(source.Length, source, (chars, sourceString) =>
            {
                SpanSimpleCaseFold(chars, sourceString);
            });
        }

        /// <summary>
        /// For performance test only.
        /// </summary>
        /// <param name="source">Source string.</param>
        /// <returns>
        /// Returns folded string.
        /// </returns>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SimpleCaseFoldBase(this string source)
        {
            /*
            var tmp = new char[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                tmp[i] = SimpleCaseFold(source[i]);
            }

            return tmp.ToString();
            */
            return string.Create(source.Length, source, (chars, sourceString) =>
            {
                SpanSimpleCaseFoldBase(chars, sourceString);
            });
        }

        /// <summary>
        ///  Simple case folding of the Span\<char\>.
        /// </summary>
        /// <param name="source">Source string.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SimpleCaseFold(this Span<char> source)
        {
            SpanSimpleCaseFold(source, source);
        }

        /// <summary>
        ///  Simple case folding of the ReadOnlySpan\<char\>.
        /// </summary>
        /// <param name="source">Source string.</param>
        /// <returns>
        /// Returns folded string.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<char> SimpleCaseFold(this ReadOnlySpan<char> source)
        {
            Span<char> destination = new char[source.Length];

            SpanSimpleCaseFold(destination, source);

            return destination;
        }

        internal const char HIGH_SURROGATE_START = '\ud800';
        internal const char HIGH_SURROGATE_END = '\udbff';
        internal const char LOW_SURROGATE_START = '\udc00';
        internal const char LOW_SURROGATE_END = '\udfff';
        internal const int HIGH_SURROGATE_RANGE = 0x3FF;

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
/*        private static void SpanSimpleCaseFoldBase(Span<char> destination, ReadOnlySpan<char> source)
        {
            var length = source.Length;

            for (int i = 0; i < length; i++)
            {
                var ch = source[i];

                if (IsAscii(ch))
                {
                    if ((uint)(ch - 'A') <= (uint)('Z' - 'A'))
                    {
                        destination[i] = (char)(ch | 0x20);
                    }
                    else
                    {
                         destination[i] = ch;
                    }

                    continue;
                }

                if (ch < HIGH_SURROGATE_START || ch > LOW_SURROGATE_END)
                {
                    destination[i] = s_simpleCaseFoldingTableBMPane1[ch];
                }
                else
                {
                    if ((i + 1) < length)
                    {
                        var ch2 = source[i + 1];
                        if ((ch2 >= LOW_SURROGATE_START) && (ch2 <= LOW_SURROGATE_END))
                        {
                            // The index is Utf32 - 0x10000 (UNICODE_PLANE01_START)
                            var index = ((ch - HIGH_SURROGATE_START) * 0x400) + (ch2 - LOW_SURROGATE_START);
                            // The utf32 is Utf32 - 0x10000 (UNICODE_PLANE01_START)
                            var utf32 = s_simpleCaseFoldingTableBMPane2[index];
                            destination[i] = (char)((utf32 / 0x400) + (int)HIGH_SURROGATE_START);
                            i++;
                            destination[i] = (char)((utf32 % 0x400) + (int)LOW_SURROGATE_START);
                        }
                        else
                        {
                            // Broken unicode - throw?
                            destination[i] = ch;
                        }
                    }
                    else
                    {
                        // Broken unicode - throw?
                        destination[i] = ch;
                    }
                }
            }
        }
*/

        // For performance test only.
        private static void SpanSimpleCaseFoldBase(Span<char> destination, ReadOnlySpan<char> source)
        {
            ref char res = ref MemoryMarshal.GetReference(destination);
            ref char src = ref MemoryMarshal.GetReference(source);

            var length = source.Length;
            int i = 0;
            var ch = src;

            for (; i < length; i++)
            {
                //var ch = source[i];
                ch = Unsafe.Add(ref src, i);

                if (IsAscii(ch))
                {
                    if ((uint)(ch - 'A') <= (uint)('Z' - 'A'))
                    {
                        //destination[i] = (char)(ch | 0x20);
                        Unsafe.Add(ref res, i) = (char)(ch | 0x20);
                    }
                    else
                    {
                         //destination[i] = ch;
                         Unsafe.Add(ref res, i) = ch;
                    }

                    continue;
                }

                if (ch < HIGH_SURROGATE_START || ch > LOW_SURROGATE_END)
                {
                    //destination[i] = (char)s_simpleCaseFoldingTableBMPane1[ch];
                    //Unsafe.Add(ref res, i) = s_simpleCaseFoldingTableBMPane1[ch];
                    Unsafe.Add(ref res, i) = SimpleCaseFold(ch);
                }
                else
                {
                    if ((i + 1) < length)
                    {
                        var ch2 = Unsafe.Add(ref src, 1);
                        if ((ch2 >= LOW_SURROGATE_START) && (ch2 <= LOW_SURROGATE_END))
                        {
                            // The index is Utf32 - 0x10000 (UNICODE_PLANE01_START)
                            var index = ((ch - HIGH_SURROGATE_START) * 0x400) + (ch2 - LOW_SURROGATE_START);
                            // The utf32 is Utf32 - 0x10000 (UNICODE_PLANE01_START)
                            var utf32 = SimpleCaseFold((char)index);
                            Unsafe.Add(ref res, i) = (char)((utf32 / 0x400) + (int)HIGH_SURROGATE_START);
                            i++;
                            Unsafe.Add(ref res, i) = (char)((utf32 % 0x400) + (int)LOW_SURROGATE_START);
                        }
                        else
                        {
                            // Broken unicode - throw?
                            Unsafe.Add(ref res, i) = ch;
                            i++;
                            Unsafe.Add(ref res, i) = SimpleCaseFold(ch);
                        }
                    }
                    else
                    {
                        // Broken unicode - throw?
                        Unsafe.Add(ref res, i) = ch;
                    }
                }
            }
        }

        internal static void SpanSimpleCaseFold(Span<char> destination, ReadOnlySpan<char> source)
        {
            //Diagnostics.Assert(destination.Length >= source.Length, "Destination span length must be equal or greater then source span length.");
            ref char res = ref MemoryMarshal.GetReference(destination);
            ref char src = ref MemoryMarshal.GetReference(source);
            //var simpleCaseFoldingTableBMPane1 = s_simpleCaseFoldingTableBMPane1.AsSpan();
            //var simpleCaseFoldingTableBMPane2 = s_simpleCaseFoldingTableBMPane2.AsSpan();

            var length = source.Length;
            int i = 0;
            var ch = src;

            for (; i < length; i++)
            {
                //var ch = source[i];
                ch = Unsafe.Add(ref src, i);

                if (IsAscii(ch))
                {
                    if((uint)(ch - 'A') <= (uint)('Z' - 'A'))
                    {
                        //destination[i] = (char)(ch | 0x20);
                        Unsafe.Add(ref res, i) = (char)(ch | 0x20);
                    }
                    else
                    {
                         //destination[i] = ch;
                         Unsafe.Add(ref res, i) = ch;
                    }

                    continue;
                }

                if (IsNotSurrogate(ch))
                {
                    //destination[i] = (char)s_simpleCaseFoldingTableBMPane1[ch];
                    //Unsafe.Add(ref res, i) = s_simpleCaseFoldingTableBMPane1[ch];
                    //Unsafe.Add(ref res, i) = simpleCaseFoldingTableBMPane1[ch];
                    Unsafe.Add(ref res, i) = SimpleCaseFold(ch);
                }
                else
                {
                    if ((i + 1) < length)
                    {
                        var ch2 = Unsafe.Add(ref src, 1);
                        if ((ch2 >= LOW_SURROGATE_START) && (ch2 <= LOW_SURROGATE_END))
                        {
                            // The index is Utf32 - 0x10000 (UNICODE_PLANE01_START)
                            // We subtract 0x10000 because we packed Plane01 (from 65536 to 131071)
                            // to an array with size uint (index from 0 to 65535).
                            var index = ((ch - HIGH_SURROGATE_START) * 0x400) + (ch2 - LOW_SURROGATE_START);
                            // The utf32 is Utf32 - 0x10000 (UNICODE_PLANE01_START)

                            var utf32 = SimpleCaseFold((char)index);
                            Unsafe.Add(ref res, i) = (char)((utf32 / 0x400) + (int)HIGH_SURROGATE_START);
                            i++;
                            Unsafe.Add(ref res, i) = (char)((utf32 % 0x400) + (int)LOW_SURROGATE_START);
                        }
                        else
                        {
                            // Broken unicode - throw?
                            // We expect a low surrogate on (i + 1) position but get a full char
                            // so we copy a high surrogate and convert the full char.
                            Unsafe.Add(ref res, i) = ch;
                            i++;
                            Unsafe.Add(ref res, i) = SimpleCaseFold(ch);
                        }
                    }
                    else
                    {
                        // Broken unicode - throw?
                        // We catch a surrogate on last position but we had to process it on previous step (i-1)
                        // so we copy the surrogate.
                        Unsafe.Add(ref res, i) = ch;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAscii(char c)
        {
            return c < 0x80;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNotSurrogate(char c)
        {
            return (c < HIGH_SURROGATE_START) || (c > LOW_SURROGATE_END);
        }

        /// <summary>
        /// Search the char position in the string with simple case folding.
        /// </summary>
        /// <param name="source">Source string.</param>
        /// <param name="ch">Char to search.</param>
        /// <returns>
        /// Returns an index the char in the string or -1 if not found.
        /// </returns>
        public static int IndexOfFolded(this string source, char ch)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return IndexOfFolded(source.AsSpan(), ch);
        }

        /// <summary>
        /// Search the char position in the ReadOnlySpan<char> with simple case folding.
        /// </summary>
        /// <param name="source">Source string.</param>
        /// <param name="ch">Char to search.</param>
        /// <returns>
        /// Returns an index the char in the ReadOnlySpan<char> or -1 if not found.
        /// </returns>
        public static int IndexOfFolded(this ReadOnlySpan<char> source, char ch)
        {
            var foldedChar = SimpleCaseFold(ch);

            for (int i = 0; i < source.Length; i++)
            {
                if (SimpleCaseFold(source[i]) == foldedChar)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Compare strings using simple case folding.
        /// </summary>
        /// <param name="strA">String to compare.</param>
        /// <param name="strB">String to compare.</param>
        /// <returns>
        /// Returns -1 if strA < strB, 0 if if strA == strB, 1 if strA < strB.
        /// </returns>
        internal static int CompareUsingSimpleCaseFolding(this string strA, string strB)
        {
            if (object.ReferenceEquals(strA, strB))
            {
                return 0;
            }

            if (strA == null)
            {
                return -1;
            }

            if (strB == null)
            {
                return 1;
            }

            ref char refA = ref MemoryMarshal.GetReference(strA.AsSpan());
            ref char refB = ref MemoryMarshal.GetReference(strB.AsSpan());

            // -1 because char before last can be surrogate in both strings.
            var length = Math.Min(strA.Length, strB.Length) - 1;
            var range = length;
            const char MaxChar = (char)0x7f;

            while (length != 0 && refA <= MaxChar && refB <= MaxChar)
            {
                // Ordinal equals or lowercase equals if the result ends up in the a-z range
                if (refA == refB ||
                    ((refA | 0x20) == (refB | 0x20) &&
                    (uint)((refA | 0x20) - 'a') <= (uint)('z' - 'a')))
                {
                    length--;
                    refA = ref Unsafe.Add(ref refA, 1);
                    refB = ref Unsafe.Add(ref refB, 1);
                }
                else
                {
                    int currentA = refA;
                    int currentB = refB;

                    // Uppercase both chars if needed
                    if ((uint)(refA - 'a') <= 'z' - 'a')
                    {
                        currentA -= 0x20;
                    }

                    if ((uint)(refB - 'a') <= 'z' - 'a')
                    {
                        currentB -= 0x20;
                    }

                    // Return the (case-insensitive) difference between them.
                    return currentA - currentB;
                }
            }

            if (length == 0)
            {
                return strA.Length - strB.Length;
            }

            int i = 0;
            int c;
            range -= length;
            ref char c1 = ref Unsafe.Add(ref refA, -1);
            ref char c2 = ref Unsafe.Add(ref refB, -1);

            for (; i < range; i++)
            {
                //var c1 = Unsafe.Add(ref refA, i);
                //var c2 = Unsafe.Add(ref refB, i);
                c1 = ref Unsafe.Add(ref refA, 1);
                c2 = ref Unsafe.Add(ref refB, 1);

                if (IsAscii(c1))
                {
                    if (IsAscii(c2))
                    {
                        if (c1 == c2)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        return -1;
                    }
                }
                else
                {
                    if (IsAscii(c2))
                    {
                        return 1;
                    }
                }

                if (IsNotSurrogate(c1) && IsNotSurrogate(c2))
                {
                    c = SimpleCaseFold(c1) - SimpleCaseFold(c2);

                    if (c == 0)
                    {
                        continue;
                    }

                    return c;
                }

                if (IsNotSurrogate(c1) || IsNotSurrogate(c2))
                {
                    // Only one char is a surrogate
                    if (IsNotSurrogate(c1))
                    {
                        return 1;
                    }

                    return -1;
                }

                // Both char is surrogates
                ref char  c12 = ref Unsafe.Add(ref refA, 1);
                ref char  c22 = ref Unsafe.Add(ref refB, 1);

                // The index is Utf32 - 0x10000 (UNICODE_PLANE01_START)
                var index1 = ((c1 - HIGH_SURROGATE_START) * 0x400) + (c12 - LOW_SURROGATE_START);

                // The utf32 is Utf32 - 0x10000 (UNICODE_PLANE01_START)
                var utf32_1 = SimpleCaseFold((char)index1);

                // The index is Utf32 - 0x10000 (UNICODE_PLANE01_START)
                var index2 = ((c2 - HIGH_SURROGATE_START) * 0x400) + (c22 - LOW_SURROGATE_START);

                // The utf32 is Utf32 - 0x10000 (UNICODE_PLANE01_START)
                var utf32_2 = SimpleCaseFold((char)index1);

                c = utf32_1 - utf32_2;

                if (c != 0)
                {
                    return c;
                }
            }

            // Last char shouldn't be a surrogate
            //c1 = Unsafe.Add(ref refA, i + 1);
            //c2 = Unsafe.Add(ref refB, i + 1);

            c = SimpleCaseFold(Unsafe.Add(ref refA, i)) - SimpleCaseFold(Unsafe.Add(ref refB, i));

            if (c != 0)
            {
                return c;
            }

            return strA.Length - strB.Length;
        }
    }

    /// <summary>
    /// String comparer with simple case folding.
    /// </summary>
    public class StringComparerUsingSimpleCaseFolding_g2 : IComparer, IEqualityComparer, IComparer<string>, IEqualityComparer<string>
    {
        // Based on CoreFX StringComparer code

        /// <summary>
        /// Constructor implementation.
        /// </summary>
        public StringComparerUsingSimpleCaseFolding_g2()
        {
        }

        /// <summary>
        /// IComparer.Compare() implementation.
        /// </summary>
        /// <param name="x">Object to compare.</param>
        /// <param name="y">Object to compare.</param>
        /// <returns>
        /// Returns 0 - if equal, -1 - if x < y, +1 - if x > y.
        /// </returns>
        public int Compare(object x, object y)
        {
            if (x == y)
            {
                return 0;
            }

            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            if (x is string sa && y is string sb)
            {
                return SimpleCaseFoldingG2.CompareUsingSimpleCaseFolding(sa, sb);
            }

            if (x is IComparable ia)
            {
                return ia.CompareTo(y);
            }

            throw new ArgumentException("SR.Argument_ImplementIComparable");
        }

        /// <summary>
        /// IEqualityComparer.Equal() implementation.
        /// </summary>
        /// <param name="x">Object to compare.</param>
        /// <param name="y">Object to compare.</param>
        /// <returns>
        /// Returns true if equal.
        /// </returns>
        public new bool Equals(object x, object y)
        {
            if (x == y)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            if (x is string sa && y is string sb)
            {
                return Equals(sa, sb);
            }

            return x.Equals(y);
        }

        /// <summary>
        /// IEqualityComparer.GetHashCode() implementation.
        /// </summary>
        /// <param name="obj">Object for which to get a hash.</param>
        /// <returns>
        /// Returns a hash code.
        /// </returns>
        public int GetHashCode(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (obj is string s)
            {
                return GetHashCodeSimpleCaseFolding(s);
            }

            return obj.GetHashCode();
        }

        private static int GetHashCodeSimpleCaseFolding(string source)
        {
            //Diagnostics.Assert(source != null, "source must not be null");

            // Do not allocate on the stack if string is empty
            if (source.Length == 0)
            {
                return source.GetHashCode();
            }

            char[] borrowedArr = null;
            Span<char> span = source.Length <= 255 ?
                stackalloc char[source.Length] :
                (borrowedArr = ArrayPool<char>.Shared.Rent(source.Length));

            SimpleCaseFoldingG2.SpanSimpleCaseFold(span, source);

            int hash = HashByteArray(MemoryMarshal.AsBytes(span));

            // Return the borrowed array if necessary.
            if (borrowedArr != null)
            {
                ArrayPool<char>.Shared.Return(borrowedArr);
            }

            return hash;
        }

        // The code come from CoreFX SqlBinary.HashByteArray()
        internal static int HashByteArray(ReadOnlySpan<byte> rgbValue)
        {
            int length = rgbValue.Length;

            if (length <= 0)
            {
                return 0;
            }

            int ulValue = DefaultSeed;
            int ulHi;

            // Size of CRC window (hashing bytes, ssstr, sswstr, numeric)
            const int XcbCrcWindow = 4;
            // const int IntShiftVal = (sizeof ulValue) * (8*sizeof(char)) - XcbCrcWindow;
            const int IntShiftVal = (4 * 8) - XcbCrcWindow;

            for (int i = 0; i < length; i++)
            {
                ulHi = (ulValue >> IntShiftVal) & 0xff;
                ulValue <<= XcbCrcWindow;
                ulValue = ulValue ^ rgbValue[i] ^ ulHi;
            }

            return ulValue;
        }

        private static int DefaultSeed { get; } = GenerateSeed();

        private static int GenerateSeed()
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[sizeof(ulong)];
                rng.GetBytes(bytes);
                var hash64 = BitConverter.ToUInt64(bytes, 0);
                return ((int)(hash64 >> 32)) ^ (int)hash64;
            }
        }

        /// <summary>
        /// IComparer\<string\>.GetHashCode() implementation.
        /// </summary>
        /// <param name="x">Object to compare.</param>
        /// <param name="y">Object to compare.</param>
        /// <returns>
        /// Returns 0 - if equal, -1 - if x < y, +1 - if x > y.
        /// </returns>
        public int Compare(string x, string y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            return SimpleCaseFoldingG2.CompareUsingSimpleCaseFolding(x, y);
        }

        /// <summary>
        /// IEqualityComparer<string>.Equals() implementation.
        /// </summary>
        /// <param name="x">Object to compare.</param>
        /// <param name="y">Object to compare.</param>
        /// <returns>
        /// Returns true if equal.
        /// </returns>
        public bool Equals(string x, string y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return SimpleCaseFoldingG2.CompareUsingSimpleCaseFolding(x, y) == 0;
        }

        /// <summary>
        /// IEqualityComparer\<string\>.GetHashCode() implementation.
        /// </summary>
        /// <param name="obj">Object for which to get a hash.</param>
        /// <returns>
        /// Returns a hash code.
        /// </returns>
        public int GetHashCode(string obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            return GetHashCodeSimpleCaseFolding(obj);
        }
    }
}
