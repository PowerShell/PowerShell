// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation.Unicode;
using Xunit;

namespace System.Management.Automation
{
    public class SimpleFoldedStringComparerTests
    {
        // The tests come from CoreFX tests.

        [Fact]
        public static void TestOrdinal_EmbeddedNull_ReturnsDifferentHashCodes()
        {
            SimpleFoldedStringComparer sc = new SimpleFoldedStringComparer();
            Assert.NotEqual(sc.GetHashCode("\0AAAAAAAAA"), sc.GetHashCode("\0BBBBBBBBBBBB"));
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
            Assert.True(sc.Equals(s1, s1a));

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

        public static IEnumerable<object[]> UpperLowerCasing_TestData()
        {
            //                          lower                upper          Culture
            yield return new object[] { "abcd",             "ABCD",         "en-US" };
            yield return new object[] { "latin i",          "LATIN I",      "en-US" };
            yield return new object[] { "turky \u0131",     "TURKY I",      "tr-TR" };
            yield return new object[] { "turky i",          "TURKY \u0130", "tr-TR" };
        }

        [Theory]
        [MemberData(nameof(UpperLowerCasing_TestData))]
        public static void CreateWithCulturesTest(string lowerForm, string upperForm, string cultureName)
        {
            CultureInfo ci = CultureInfo.GetCultureInfo(cultureName);
            StringComparer sc = StringComparer.Create(ci, false);
            Assert.False(sc.Equals(lowerForm, upperForm), "Not expected to have the lowercase equals the uppercase with ignore case is false");
            Assert.False(sc.Equals((object) lowerForm, (object) upperForm), "Not expected to have the lowercase object equals the uppercase with ignore case is false");
            Assert.NotEqual(sc.GetHashCode(lowerForm), sc.GetHashCode(upperForm));
            Assert.NotEqual(sc.GetHashCode((object) lowerForm), sc.GetHashCode((object) upperForm));

            sc = StringComparer.Create(ci, true);
            Assert.True(sc.Equals(lowerForm, upperForm), "It is expected to have the lowercase equals the uppercase with ignore case is true");
            Assert.True(sc.Equals((object) lowerForm, (object) upperForm), "It is expected to have the lowercase object equals the uppercase with ignore case is true");
            Assert.Equal(sc.GetHashCode(lowerForm), sc.GetHashCode(upperForm));
            Assert.Equal(sc.GetHashCode((object) lowerForm), sc.GetHashCode((object) upperForm));
        }

        [Fact]
        public static void InvariantTest()
        {
            Assert.True(StringComparer.InvariantCulture.Equals("test", "test"), "Same casing strings with StringComparer.InvariantCulture should be equal");
            Assert.True(StringComparer.InvariantCulture.Equals((object) "test", (object) "test"), "Same casing objects with StringComparer.InvariantCulture should be equal");
            Assert.Equal(StringComparer.InvariantCulture.GetHashCode("test"), StringComparer.InvariantCulture.GetHashCode("test"));
            Assert.Equal(0, StringComparer.InvariantCulture.Compare("test", "test"));

            Assert.False(StringComparer.InvariantCulture.Equals("test", "TEST"), "different casing strings with StringComparer.InvariantCulture should not be equal");
            Assert.False(StringComparer.InvariantCulture.Equals((object) "test", (object) "TEST"), "different casing objects with StringComparer.InvariantCulture should not be equal");
            Assert.NotEqual(StringComparer.InvariantCulture.GetHashCode("test"), StringComparer.InvariantCulture.GetHashCode("TEST"));
            Assert.NotEqual(0, StringComparer.InvariantCulture.Compare("test", "TEST"));

            Assert.True(StringComparer.InvariantCultureIgnoreCase.Equals("test", "test"), "Same casing strings with StringComparer.InvariantCultureIgnoreCase should be equal");
            Assert.True(StringComparer.InvariantCultureIgnoreCase.Equals((object) "test", (object) "test"), "Same casing objects with StringComparer.InvariantCultureIgnoreCase should be equal");
            Assert.Equal(0, StringComparer.InvariantCultureIgnoreCase.Compare("test", "test"));

            Assert.True(StringComparer.InvariantCultureIgnoreCase.Equals("test", "TEST"), "same strings with different casing with StringComparer.InvariantCultureIgnoreCase should be equal");
            Assert.True(StringComparer.InvariantCultureIgnoreCase.Equals((object) "test", (object) "TEST"), "same objects with different casing with StringComparer.InvariantCultureIgnoreCase should be equal");
            Assert.Equal(StringComparer.InvariantCultureIgnoreCase.GetHashCode("test"), StringComparer.InvariantCultureIgnoreCase.GetHashCode("TEST"));
            Assert.Equal(0, StringComparer.InvariantCultureIgnoreCase.Compare("test", "TEST"));
        }
    }
}
