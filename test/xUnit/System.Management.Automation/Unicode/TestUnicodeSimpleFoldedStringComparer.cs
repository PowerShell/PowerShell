// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation.Unicode;
using Xunit;

namespace PSTests.Parallel.System.Management.Automation.Unicode
{
    public class SimpleFoldedStringComparerTests
    {
        // The tests come from CoreFX tests: src/System.Runtime.Extensions/tests/System/StringComparer.cs

        [Fact]
        public static void TestOrdinal_EmbeddedNull_ReturnsDifferentHashCodes()
        {
            SimpleFoldedStringComparer sc = new SimpleFoldedStringComparer();
            Assert.NotEqual(sc.GetHashCode("\0AAAAAAAAA"), sc.GetHashCode("\0BBBBBBBBBBBB"));
        }

        [Fact]
        public static void TestHash_ReturnsHashCodes()
        {
            SimpleFoldedStringComparer sc = new SimpleFoldedStringComparer();
            Assert.Equal(sc.GetHashCode("AAA"), sc.GetHashCode("aaa"));
            Assert.Equal(sc.GetHashCode("BaC"), sc.GetHashCode("bAc"));
            Assert.Equal(sc.GetHashCode((object)"BaC"), sc.GetHashCode((object)"bAc"));
            Assert.NotEqual(sc.GetHashCode("AAA"), sc.GetHashCode("AAB"));
            Assert.NotEqual(sc.GetHashCode("AAA"), sc.GetHashCode("AAb"));
            Assert.NotEqual(sc.GetHashCode((object)"AAA"), sc.GetHashCode((object)"AAb"));
        }

        [Fact]
        public static void VerifyComparer()
        {
            SimpleFoldedStringComparer sc = new SimpleFoldedStringComparer();
            string s1 = "Hello";
            string s1a = "Hello";
            string s1b = "HELLO";
            string s2 = "There";
            string aa = "\0AAAAAAAAA";
            string bb = "\0BBBBBBBBBBBB";

            Assert.True(sc.Equals(s1, s1a));
            Assert.True(sc.Equals((object)s1, (object)s1a));

            Assert.Equal(0, sc.Compare(s1, s1a));
            Assert.Equal(0, ((IComparer)sc).Compare(s1, s1a));

            Assert.True(sc.Equals(s1, s1));
            Assert.True(((IEqualityComparer)sc).Equals(s1, s1));
            Assert.Equal(0, sc.Compare(s1, s1));
            Assert.Equal(0, ((IComparer)sc).Compare(s1, s1));

            Assert.False(sc.Equals(s1, s2));
            Assert.False(((IEqualityComparer)sc).Equals(s1, s2));
            Assert.True(sc.Compare(s1, s2) < 0);
            Assert.True(((IComparer)sc).Compare(s1, s2) < 0);

            Assert.True(sc.Equals(s1, s1b));
            Assert.True(((IEqualityComparer)sc).Equals(s1, s1b));

            Assert.NotEqual(0, ((IComparer)sc).Compare(aa, bb));
            Assert.False(sc.Equals(aa, bb));
            Assert.False(((IEqualityComparer)sc).Equals(aa, bb));
            Assert.True(sc.Compare(aa, bb) < 0);
            Assert.True(((IComparer)sc).Compare(aa, bb) < 0);

            int result = sc.Compare(s1, s1b);
            Assert.Equal(0, result);

            result = ((IComparer)sc).Compare(s1, s1b);
            Assert.Equal(0, result);
        }
    }
}
