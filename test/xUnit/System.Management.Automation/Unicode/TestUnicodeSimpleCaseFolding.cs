// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation.Unicode;
using Xunit;

namespace System.Management.Automation.Unicode.Tests
{
    public class UnitTest1
    {
        [Theory]
        [InlineData("", '\u007f', -1)]
        [InlineData("Hello", 'o', 4)]
        [InlineData("Hello", 'O', 4)]
        [InlineData("Hello", 'h', 0)]
        [InlineData("Hello", 'H', 0)]
        [InlineData("Hello", 'g', -1)]
        [InlineData("Hello", 'G', -1)]
        [InlineData("HelLo", '\0', -1)]
        [InlineData("!@#$%", '%', 4)]
        [InlineData("!@#$", '!', 0)]
        [InlineData("!@#$", '@', 1)]
        [InlineData("_____________\u807f", '\u007f', -1)]
        [InlineData("_____________\u807f__", '\u007f', -1)]
        [InlineData("_____________\u807f\u007f_", '\u007f', 14)]
        [InlineData("__\u807f_______________", '\u007f', -1)]
        [InlineData("__\u807f___\u007f___________", '\u007f', 6)]
        public void Test_IndexOfFolded(string s, char target, int expected)
        {
            Assert.Equal(expected, s.IndexOfFolded(target));
        }

        // Follow tests comes from src\System.Runtime\tests\System\StringTests.netcoreapp.cs
        [Fact]
        public static void IndexOf_TurkishI_TurkishCulture_Char()
        {
            var savedCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

                string s = "Turkish I \u0131s TROUBL\u0130NG!";
                char value = '\u0130';
                Assert.Equal(19, s.IndexOf(value));
                Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(4, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(19, s.IndexOf(value, StringComparison.Ordinal));
                Assert.Equal(19, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

                ReadOnlySpan<char> span = s.AsSpan();
                Assert.Equal(19, span.IndexOf(new char[] { value }, StringComparison.CurrentCulture));
                Assert.Equal(4, span.IndexOf(new char[] { value }, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(19, span.IndexOf(new char[] { value }, StringComparison.Ordinal));
                Assert.Equal(19, span.IndexOf(new char[] { value }, StringComparison.OrdinalIgnoreCase));

                value = '\u0131';
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(10, s.IndexOf(value, StringComparison.Ordinal));
                Assert.Equal(10, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

                Assert.Equal(10, span.IndexOf(new char[] { value }, StringComparison.CurrentCulture));
                Assert.Equal(8, span.IndexOf(new char[] { value }, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(10, span.IndexOf(new char[] { value }, StringComparison.Ordinal));
                Assert.Equal(10, span.IndexOf(new char[] { value }, StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                CultureInfo.CurrentCulture = savedCulture;
            }
        }

        [Fact]
        public static void IndexOf_TurkishI_Char()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            string s = "Turkish I \u0131s TROUBL\u0130NG!";
            ReadOnlySpan<char> span = s.AsSpan();
            char value = '\u0130';
            Assert.Equal(19, s.IndexOfFolded(value));
            Assert.Equal(19, span.IndexOfFolded(value));

            value = '\u0131';
            Assert.Equal(10, s.IndexOfFolded(value));
            Assert.Equal(10, span.IndexOfFolded(value));
        }

        [Fact]
        public static void IndexOf_EquivalentDiacritics_Char()
        {
            string s = "Exhibit a\u0300\u00C0";
            ReadOnlySpan<char> span = s.AsSpan();
            char value = '\u00C0';
            Assert.Equal(10, s.IndexOfFolded(value));
            Assert.Equal(10, span.IndexOfFolded(value));

            value = '\u0300';
            Assert.Equal(9, s.IndexOfFolded(value));
            Assert.Equal(9, span.IndexOfFolded(value));
        }

        [Fact]
        public static void IndexOf_CyrillicE_Char()
        {
            string s = "Foo\u0400Bar";
            char value = '\u0400';

            Assert.Equal(3, s.IndexOfFolded(value));
        }
    }
}
