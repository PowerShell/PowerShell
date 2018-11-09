// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation.Unicode;
using System.Text;
using Xunit;

namespace System.Management.Automation.Unicode.Tests
{

    public class CharUnicodeInfoGetUnicodeCategoryTests
    {
        [Fact]
        public void GetNumericValue()
        {
            foreach (CharUnicodeInfoTestCase testCase in CharUnicodeInfoTestData.TestCases)
            {
                if (testCase.Utf32CodeValue.Length == 1)
                {
                    // Test the char overload for a single char
                    System.IO.File.AppendAllText(@"C:\tmp\WriteText.txt", testCase.Utf32CodeValue);
                }
                // Test the string overload for a surrogate pair
            }
        }
    }

    // The tests come from CoreFX tests: src\System.Runtime\tests\System\

    public class CharTests
    {
        [Theory]
        [InlineData('h', 'h', 0)]
        [InlineData('h', 'a', 1)]
        [InlineData('h', 'z', -1)]
        [InlineData('h', null, 1)]
        public void CompareTo_Other_ReturnsExpected(char c, object value, int expected)
        {
            if (value is char charValue)
            {
                Assert.Equal(expected, Math.Sign(c.CompareTo(charValue)));
            }

            Assert.Equal(expected, Math.Sign(c.CompareTo(value)));
        }

        [Theory]
        [InlineData("a")]
        [InlineData(234)]
        public void CompareTo_ObjectNotDouble_ThrowsArgumentException(object value)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => ((char)123).CompareTo(value));
        }

        public static IEnumerable<object[]> ConvertFromUtf32_TestData()
        {
            yield return new object[] { 0x10000, "\uD800\uDC00" };
            yield return new object[] { 0x103FF, "\uD800\uDFFF" };
            yield return new object[] { 0xFFFFF, "\uDBBF\uDFFF" };
            yield return new object[] { 0x10FC00, "\uDBFF\uDC00" };
            yield return new object[] { 0x10FFFF, "\uDBFF\uDFFF" };
            yield return new object[] { 0, "\0" };
            yield return new object[] { 0x3FF, "\u03FF" };
            yield return new object[] { 0xE000, "\uE000" };
            yield return new object[] { 0xFFFF, "\uFFFF" };
        }

        [Theory]
        [MemberData(nameof(ConvertFromUtf32_TestData))]
        public static void ConvertFromUtf32(int utf32, string expected)
        {
            Assert.Equal(expected, char.ConvertFromUtf32(utf32));
        }

        [Theory]
        [InlineData(0xD800)]
        [InlineData(0xDC00)]
        [InlineData(0xDFFF)]
        [InlineData(0x110000)]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public static void ConvertFromUtf32_InvalidUtf32_ThrowsArgumentOutOfRangeException(int utf32)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("utf32", () => char.ConvertFromUtf32(utf32));
        }

        public static IEnumerable<object[]> ConvertToUtf32_String_Int_TestData()
        {
            yield return new object[] { "\uD800\uDC00", 0, 0x10000 };
            yield return new object[] { "\uDBBF\uDFFF", 0, 0xFFFFF };
            yield return new object[] { "\uDBFF\uDC00", 0, 0x10FC00 };
            yield return new object[] { "\uDBFF\uDFFF", 0, 0x10FFFF };
            yield return new object[] { "\u0000\u0001", 0, 0 };
            yield return new object[] { "\u0000\u0001", 1, 1 };
            yield return new object[] { "\u0000", 0, 0 };
            yield return new object[] { "\u0020\uD7FF", 0, 32 };
            yield return new object[] { "\u0020\uD7FF", 1, 0xD7FF };
            yield return new object[] { "abcde", 4, 'e' };

            // Invalid unicode
            yield return new object[] { "\uD800\uD800\uDFFF", 1, 0x103FF };
            yield return new object[] { "\uD800\uD7FF", 1, 0xD7FF }; // High, non-surrogate
            yield return new object[] { "\uD800\u0000", 1, 0 }; // High, non-surrogate
            yield return new object[] { "\uDF01\u0000", 1, 0 }; // Low, non-surrogate
        }

        [Theory]
        [MemberData(nameof(ConvertToUtf32_String_Int_TestData))]
        public static void ConvertToUtf32_String_Int(string s, int index, int expected)
        {
            Assert.Equal(expected, char.ConvertToUtf32(s, index));
        }

        [Fact]
        public static void ConvertToUtf32_String_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => char.ConvertToUtf32(null, 0)); // String is null

            AssertExtensions.Throws<ArgumentException>("s", () => char.ConvertToUtf32("\uD800\uD800", 0)); // High, high
            AssertExtensions.Throws<ArgumentException>("s", () => char.ConvertToUtf32("\uD800\uD800", 1)); // High, high
            AssertExtensions.Throws<ArgumentException>("s", () => char.ConvertToUtf32("\uD800\uD7FF", 0)); // High, non-surrogate
            AssertExtensions.Throws<ArgumentException>("s", () => char.ConvertToUtf32("\uD800\u0000", 0)); // High, non-surrogate

            AssertExtensions.Throws<ArgumentException>("s", () => char.ConvertToUtf32("\uDC01\uD940", 0)); // Low, high
            AssertExtensions.Throws<ArgumentException>("s", () => char.ConvertToUtf32("\uDC01\uD940", 1)); // Low, high
            AssertExtensions.Throws<ArgumentException>("s", () => char.ConvertToUtf32("\uDD00\uDE00", 0)); // Low, low
            AssertExtensions.Throws<ArgumentException>("s", () => char.ConvertToUtf32("\uDD00\uDE00", 1)); // Low, hig
            AssertExtensions.Throws<ArgumentException>("s", () => char.ConvertToUtf32("\uDF01\u0000", 0)); // Low, non-surrogateh

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.ConvertToUtf32("abcde", -1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.ConvertToUtf32("abcde", 5)); // Index >= string.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.ConvertToUtf32("", 0)); // Index >= string.Length
        }

        public static IEnumerable<object[]> ConvertToUtf32_Char_Char_TestData()
        {
            yield return new object[] { '\uD800', '\uDC00', 0x10000 };
            yield return new object[] { '\uD800', '\uDFFF', 0x103FF };
            yield return new object[] { '\uDBBF', '\uDFFF', 0xFFFFF };
            yield return new object[] { '\uDBFF', '\uDC00', 0x10FC00 };
            yield return new object[] { '\uDBFF', '\uDFFF', 0x10FFFF };
        }

        [Theory]
        [MemberData(nameof(ConvertToUtf32_Char_Char_TestData))]
        public static void ConvertToUtf32_Char_Char(char highSurrogate, char lowSurrogate, int expected)
        {
            Assert.Equal(expected, char.ConvertToUtf32(highSurrogate, lowSurrogate));
        }

        [Fact]
        public static void ConvertToUtf32_Char_Char_Invalid()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("lowSurrogate", () => char.ConvertToUtf32('\uD800', '\uD800')); // High, high
            AssertExtensions.Throws<ArgumentOutOfRangeException>("lowSurrogate", () => char.ConvertToUtf32('\uD800', '\uD7FF')); // High, non-surrogate
            AssertExtensions.Throws<ArgumentOutOfRangeException>("lowSurrogate", () => char.ConvertToUtf32('\uD800', '\u0000')); // High, non-surrogate

            AssertExtensions.Throws<ArgumentOutOfRangeException>("highSurrogate", () => char.ConvertToUtf32('\uDD00', '\uDE00')); // Low, low
            AssertExtensions.Throws<ArgumentOutOfRangeException>("highSurrogate", () => char.ConvertToUtf32('\uDC01', '\uD940')); // Low, high
            AssertExtensions.Throws<ArgumentOutOfRangeException>("highSurrogate", () => char.ConvertToUtf32('\uDF01', '\u0000')); // Low, non-surrogate

            AssertExtensions.Throws<ArgumentOutOfRangeException>("highSurrogate", () => char.ConvertToUtf32('\u0032', '\uD7FF')); // Non-surrogate, non-surrogate
            AssertExtensions.Throws<ArgumentOutOfRangeException>("highSurrogate", () => char.ConvertToUtf32('\u0000', '\u0000')); // Non-surrogate, non-surrogate
        }

        [Theory]
        [InlineData('a', 'a', true)]
        [InlineData('a', 'A', false)]
        [InlineData('a', 'b', false)]
        [InlineData('a', (int)'a', false)]
        [InlineData('a', "a", false)]
        [InlineData('a', null, false)]
        public static void Equals1(char c, object obj, bool expected)
        {
            if (obj is char)
            {
                Assert.Equal(expected, c.Equals((char)obj));
                Assert.Equal(expected, c.GetHashCode().Equals(obj.GetHashCode()));
            }
            Assert.Equal(expected, c.Equals(obj));
        }

        [Theory]
        [InlineData('0', 0)]
        [InlineData('9', 9)]
        [InlineData('T', -1)]
        public static void GetNumericValue_Char(char c, int expected)
        {
            Assert.Equal(expected, char.GetNumericValue(c));
        }

        [Theory]
        [InlineData("\uD800\uDD07", 0, 1)]
        [InlineData("9", 0, 9)]
        [InlineData("99", 1, 9)]
        [InlineData(" 7  ", 1, 7)]
        [InlineData("Test 7", 5, 7)]
        [InlineData("T", 0, -1)]
        public static void GetNumericValue_String_Int(string s, int index, int expected)
        {
            Assert.Equal(expected, char.GetNumericValue(s, index));
        }

        [Fact]
        public static void GetNumericValue_String_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => char.GetNumericValue(null, 0)); // String is null
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.GetNumericValue("abc", -1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.GetNumericValue("abc", 3)); // Index >= string.Length
        }

        [Fact]
        public void GetTypeCode_Invoke_ReturnsBoolean()
        {
            Assert.Equal(TypeCode.Char, 'a'.GetTypeCode());
        }

        [Fact]
        public static void IsControl_Char()
        {
            foreach (char c in GetTestChars(UnicodeCategory.Control))
                Assert.True(char.IsControl(c));

            foreach (char c in GetTestCharsNotInCategory(UnicodeCategory.Control))
                Assert.False(char.IsControl(c));
        }

        [Fact]
        public static void IsControl_String_Int()
        {
            foreach (char c in GetTestChars(UnicodeCategory.Control))
                Assert.True(char.IsControl(c.ToString(), 0));

            foreach (char c in GetTestCharsNotInCategory(UnicodeCategory.Control))
                Assert.False(char.IsControl(c.ToString(), 0));
        }

        [Fact]
        public static void IsControl_String_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => char.IsControl(null, 0)); // String is null
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsControl("abc", -1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsControl("abc", 3)); // Index >= string.Length
        }

        [Fact]
        public static void IsDigit_Char()
        {
            foreach (char c in GetTestChars(UnicodeCategory.DecimalDigitNumber))
                Assert.True(char.IsDigit(c));

            foreach (char c in GetTestCharsNotInCategory(UnicodeCategory.DecimalDigitNumber))
                Assert.False(char.IsDigit(c));
        }

        [Fact]
        public static void IsDigit_String_Int()
        {
            foreach (char c in GetTestChars(UnicodeCategory.DecimalDigitNumber))
                Assert.True(char.IsDigit(c.ToString(), 0));

            foreach (char c in GetTestCharsNotInCategory(UnicodeCategory.DecimalDigitNumber))
                Assert.False(char.IsDigit(c.ToString(), 0));
        }

        [Fact]
        public static void IsDigit_String_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => char.IsDigit(null, 0)); // String is null
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsDigit("abc", -1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsDigit("abc", 3)); // Index >= string.Length
        }

        [Fact]
        public static void IsLetter_Char()
        {
            var categories = new UnicodeCategory[]
            {
            UnicodeCategory.UppercaseLetter,
            UnicodeCategory.LowercaseLetter,
            UnicodeCategory.TitlecaseLetter,
            UnicodeCategory.ModifierLetter,
            UnicodeCategory.OtherLetter
            };
            foreach (char c in GetTestChars(categories))
                Assert.True(char.IsLetter(c));

            foreach (char c in GetTestCharsNotInCategory(categories))
                Assert.False(char.IsLetter(c));
        }

        [Fact]
        public static void IsLetter_String_Int()
        {
            var categories = new UnicodeCategory[]
            {
            UnicodeCategory.UppercaseLetter,
            UnicodeCategory.LowercaseLetter,
            UnicodeCategory.TitlecaseLetter,
            UnicodeCategory.ModifierLetter,
            UnicodeCategory.OtherLetter
            };
            foreach (char c in GetTestChars(categories))
                Assert.True(char.IsLetter(c.ToString(), 0));

            foreach (char c in GetTestCharsNotInCategory(categories))
                Assert.False(char.IsLetter(c.ToString(), 0));
        }

        [Fact]
        public static void IsLetter_String_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => char.IsLetter(null, 0)); // String is null
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsLetter("abc", -1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsLetter("abc", 3)); // Index >= string.Length
        }

        [Fact]
        public static void IsLetterOrDigit_Char()
        {
            var categories = new UnicodeCategory[]
            {
            UnicodeCategory.UppercaseLetter,
            UnicodeCategory.LowercaseLetter,
            UnicodeCategory.TitlecaseLetter,
            UnicodeCategory.ModifierLetter,
            UnicodeCategory.OtherLetter,
            UnicodeCategory.DecimalDigitNumber
            };
            foreach (char c in GetTestChars(categories))
                Assert.True(char.IsLetterOrDigit(c));

            foreach (char c in GetTestCharsNotInCategory(categories))
                Assert.False(char.IsLetterOrDigit(c));
        }

        [Fact]
        public static void IsLetterOrDigit_String_Int()
        {
            var categories = new UnicodeCategory[]
            {
            UnicodeCategory.UppercaseLetter,
            UnicodeCategory.LowercaseLetter,
            UnicodeCategory.TitlecaseLetter,
            UnicodeCategory.ModifierLetter,
            UnicodeCategory.OtherLetter,
            UnicodeCategory.DecimalDigitNumber
            };
            foreach (char c in GetTestChars(categories))
                Assert.True(char.IsLetterOrDigit(c.ToString(), 0));

            foreach (char c in GetTestCharsNotInCategory(categories))
                Assert.False(char.IsLetterOrDigit(c.ToString(), 0));
        }

        [Fact]
        public static void IsLetterOrDigit_String_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => char.IsLetterOrDigit(null, 0)); // String is null
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsLetterOrDigit("abc", -1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsLetterOrDigit("abc", 3)); // Index >= string.Length
        }

        [Fact]
        public static void IsLower_Char()
        {
            foreach (char c in GetTestChars(UnicodeCategory.LowercaseLetter))
                Assert.True(char.IsLower(c));

            foreach (char c in GetTestCharsNotInCategory(UnicodeCategory.LowercaseLetter))
                Assert.False(char.IsLower(c));
        }

        [Fact]
        public static void IsLower_String_Int()
        {
            foreach (char c in GetTestChars(UnicodeCategory.LowercaseLetter))
                Assert.True(char.IsLower(c.ToString(), 0));

            foreach (char c in GetTestCharsNotInCategory(UnicodeCategory.LowercaseLetter))
                Assert.False(char.IsLower(c.ToString(), 0));
        }

        [Fact]
        public static void IsLower_String_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => char.IsLower(null, 0)); // String is null
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsLower("abc", -1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsLower("abc", 3)); // Index >= string.Length
        }

        [Fact]
        public static void IsNumber_Char()
        {
            var categories = new UnicodeCategory[]
            {
            UnicodeCategory.DecimalDigitNumber,
            UnicodeCategory.LetterNumber,
            UnicodeCategory.OtherNumber
            };
            foreach (char c in GetTestChars(categories))
                Assert.True(char.IsNumber(c));

            foreach (char c in GetTestCharsNotInCategory(categories))
                Assert.False(char.IsNumber(c));
        }

        [Fact]
        public static void IsNumber_String_Int()
        {
            var categories = new UnicodeCategory[]
            {
            UnicodeCategory.DecimalDigitNumber,
            UnicodeCategory.LetterNumber,
            UnicodeCategory.OtherNumber
            };
            foreach (char c in GetTestChars(categories))
                Assert.True(char.IsNumber(c.ToString(), 0));

            foreach (char c in GetTestCharsNotInCategory(categories))
                Assert.False(char.IsNumber(c.ToString(), 0));
        }

        [Fact]
        public static void IsNumber_String_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => char.IsNumber(null, 0)); // String is null
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsNumber("abc", -1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsNumber("abc", 3)); // Index >= string.Length
        }

        [Fact]
        public static void IsPunctuation_Char()
        {
            var categories = new UnicodeCategory[]
            {
            UnicodeCategory.ConnectorPunctuation,
            UnicodeCategory.DashPunctuation,
            UnicodeCategory.OpenPunctuation,
            UnicodeCategory.ClosePunctuation,
            UnicodeCategory.InitialQuotePunctuation,
            UnicodeCategory.FinalQuotePunctuation,
            UnicodeCategory.OtherPunctuation
            };
            foreach (char c in GetTestChars(categories))
                Assert.True(char.IsPunctuation(c));

            foreach (char c in GetTestCharsNotInCategory(categories))
                Assert.False(char.IsPunctuation(c));
        }

        [Fact]
        public static void IsPunctuation_String_Int()
        {
            var categories = new UnicodeCategory[]
            {
            UnicodeCategory.ConnectorPunctuation,
            UnicodeCategory.DashPunctuation,
            UnicodeCategory.OpenPunctuation,
            UnicodeCategory.ClosePunctuation,
            UnicodeCategory.InitialQuotePunctuation,
            UnicodeCategory.FinalQuotePunctuation,
            UnicodeCategory.OtherPunctuation
            };
            foreach (char c in GetTestChars(categories))
                Assert.True(char.IsPunctuation(c.ToString(), 0));

            foreach (char c in GetTestCharsNotInCategory(categories))
                Assert.False(char.IsPunctuation(c.ToString(), 0));
        }

        [Fact]
        public static void IsPunctuation_String_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => char.IsPunctuation(null, 0)); // String is null
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsPunctuation("abc", -1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsPunctuation("abc", 3)); // Index >= string.Length
        }

        [Fact]
        public static void IsSeparator_Char()
        {
            var categories = new UnicodeCategory[]
            {
            UnicodeCategory.SpaceSeparator,
            UnicodeCategory.LineSeparator,
            UnicodeCategory.ParagraphSeparator
            };
            foreach (char c in GetTestChars(categories))
                Assert.True(char.IsSeparator(c));

            foreach (char c in GetTestCharsNotInCategory(categories))
                Assert.False(char.IsSeparator(c));
        }

        [Fact]
        public static void IsSeparator_String_Int()
        {
            var categories = new UnicodeCategory[]
            {
            UnicodeCategory.SpaceSeparator,
            UnicodeCategory.LineSeparator,
            UnicodeCategory.ParagraphSeparator
            };
            foreach (char c in GetTestChars(categories))
                Assert.True(char.IsSeparator(c.ToString(), 0));

            foreach (char c in GetTestCharsNotInCategory(categories))
                Assert.False(char.IsSeparator(c.ToString(), 0));
        }

        [Fact]
        public static void IsSeparator_String_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => char.IsSeparator(null, 0)); // String is null
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsSeparator("abc", -1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsSeparator("abc", 3)); // Index >= string.Length
        }

        [Fact]
        public static void IsLowSurrogate_Char()
        {
            foreach (char c in s_lowSurrogates)
                Assert.True(char.IsLowSurrogate(c));

            foreach (char c in s_highSurrogates)
                Assert.False(char.IsLowSurrogate(c));

            foreach (char c in s_nonSurrogates)
                Assert.False(char.IsLowSurrogate(c));
        }

        [Fact]
        public static void IsLowSurrogate_String_Int()
        {
            foreach (char c in s_lowSurrogates)
                Assert.True(char.IsLowSurrogate(c.ToString(), 0));

            foreach (char c in s_highSurrogates)
                Assert.False(char.IsLowSurrogate(c.ToString(), 0));

            foreach (char c in s_nonSurrogates)
                Assert.False(char.IsLowSurrogate(c.ToString(), 0));
        }

        [Fact]
        public static void IsLowSurrogate_String_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => char.IsLowSurrogate(null, 0)); // String is null
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsLowSurrogate("abc", -1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsLowSurrogate("abc", 3)); // Index >= string.Length
        }

        [Fact]
        public static void IsHighSurrogate_Char()
        {
            foreach (char c in s_highSurrogates)
                Assert.True(char.IsHighSurrogate(c));

            foreach (char c in s_lowSurrogates)
                Assert.False(char.IsHighSurrogate(c));

            foreach (char c in s_nonSurrogates)
                Assert.False(char.IsHighSurrogate(c));
        }

        [Fact]
        public static void IsHighSurrogate_String_Int()
        {
            foreach (char c in s_highSurrogates)
                Assert.True(char.IsHighSurrogate(c.ToString(), 0));

            foreach (char c in s_lowSurrogates)
                Assert.False(char.IsHighSurrogate(c.ToString(), 0));

            foreach (char c in s_nonSurrogates)
                Assert.False(char.IsHighSurrogate(c.ToString(), 0));
        }

        [Fact]
        public static void IsHighSurrogate_String_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => char.IsHighSurrogate(null, 0)); // String is null
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsHighSurrogate("abc", -1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsHighSurrogate("abc", 3)); // Index >= string.Length
        }

        [Fact]
        public static void IsSurrogate_Char()
        {
            foreach (char c in s_highSurrogates)
                Assert.True(char.IsSurrogate(c));

            foreach (char c in s_lowSurrogates)
                Assert.True(char.IsSurrogate(c));

            foreach (char c in s_nonSurrogates)
                Assert.False(char.IsSurrogate(c));
        }

        [Fact]
        public static void IsSurrogate_String_Int()
        {
            foreach (char c in s_highSurrogates)
                Assert.True(char.IsSurrogate(c.ToString(), 0));

            foreach (char c in s_lowSurrogates)
                Assert.True(char.IsSurrogate(c.ToString(), 0));

            foreach (char c in s_nonSurrogates)
                Assert.False(char.IsSurrogate(c.ToString(), 0));
        }

        [Fact]
        public static void IsSurrogate_String_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => char.IsSurrogate(null, 0)); // String is null
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsSurrogate("abc", -1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsSurrogate("abc", 3)); // Index >= string.Length
        }

        [Fact]
        public static void IsSurrogatePair_Char()
        {
            foreach (char hs in s_highSurrogates)
                foreach (char ls in s_lowSurrogates)
                    Assert.True(char.IsSurrogatePair(hs, ls));

            foreach (char hs in s_nonSurrogates)
                foreach (char ls in s_lowSurrogates)
                    Assert.False(char.IsSurrogatePair(hs, ls));

            foreach (char hs in s_highSurrogates)
                foreach (char ls in s_nonSurrogates)
                    Assert.False(char.IsSurrogatePair(hs, ls));
        }

        [Fact]
        public static void IsSurrogatePair_String_Int()
        {
            foreach (char hs in s_highSurrogates)
                foreach (char ls in s_lowSurrogates)
                    Assert.True(char.IsSurrogatePair(hs.ToString() + ls, 0));

            foreach (char hs in s_nonSurrogates)
                foreach (char ls in s_lowSurrogates)
                    Assert.False(char.IsSurrogatePair(hs.ToString() + ls, 0));

            foreach (char hs in s_highSurrogates)
                foreach (char ls in s_nonSurrogates)
                    Assert.False(char.IsSurrogatePair(hs.ToString() + ls, 0));

            Assert.False(char.IsSurrogatePair("\ud800\udc00", 1)); // Index + 1 >= s.Length
        }

        [Fact]
        public static void IsSurrogatePair_String_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => char.IsSurrogatePair(null, 0)); // String is null
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsSurrogatePair("abc", -1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsSurrogatePair("abc", 3)); // Index >= string.Length
        }

        [Fact]
        public static void IsSymbol_Char()
        {
            var categories = new UnicodeCategory[]
            {
            UnicodeCategory.MathSymbol,
            UnicodeCategory.ModifierSymbol,
            UnicodeCategory.CurrencySymbol,
            UnicodeCategory.OtherSymbol
            };
            foreach (char c in GetTestChars(categories))
                Assert.True(char.IsSymbol(c));

            foreach (char c in GetTestCharsNotInCategory(categories))
                Assert.False(char.IsSymbol(c));
        }

        [Fact]
        public static void IsSymbol_String_Int()
        {
            var categories = new UnicodeCategory[]
            {
            UnicodeCategory.MathSymbol,
            UnicodeCategory.ModifierSymbol,
            UnicodeCategory.CurrencySymbol,
            UnicodeCategory.OtherSymbol
            };
            foreach (char c in GetTestChars(categories))
                Assert.True(char.IsSymbol(c.ToString(), 0));

            foreach (char c in GetTestCharsNotInCategory(categories))
                Assert.False(char.IsSymbol(c.ToString(), 0));
        }

        [Fact]
        public static void IsSymbol_String_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => char.IsSymbol(null, 0)); // String is null
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsSymbol("abc", -1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsSymbol("abc", 3)); // Index >= string.Length
        }

        [Fact]
        public static void IsUpper_Char()
        {
            foreach (char c in GetTestChars(UnicodeCategory.UppercaseLetter))
                Assert.True(char.IsUpper(c));

            foreach (char c in GetTestCharsNotInCategory(UnicodeCategory.UppercaseLetter))
                Assert.False(char.IsUpper(c));
        }

        [Fact]
        public static void IsUpper_String_Int()
        {
            foreach (char c in GetTestChars(UnicodeCategory.UppercaseLetter))
                Assert.True(char.IsUpper(c.ToString(), 0));

            foreach (char c in GetTestCharsNotInCategory(UnicodeCategory.UppercaseLetter))
                Assert.False(char.IsUpper(c.ToString(), 0));
        }

        [Fact]
        public static void IsUpper_String_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => char.IsUpper(null, 0)); // String is null
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsUpper("abc", -1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsUpper("abc", 3)); // Index >= string.Length
        }

        [Fact]
        public static void IsWhitespace_Char()
        {
            var categories = new UnicodeCategory[]
            {
            UnicodeCategory.SpaceSeparator,
            UnicodeCategory.LineSeparator,
            UnicodeCategory.ParagraphSeparator
            };
            foreach (char c in GetTestChars(categories))
                Assert.True(char.IsWhiteSpace(c));

            foreach (char c in GetTestCharsNotInCategory(categories))
            {
                // Need to special case some control chars that are treated as whitespace
                if ((c >= '\x0009' && c <= '\x000d') || c == '\x0085') continue;
                Assert.False(char.IsWhiteSpace(c));
            }
        }

        [Fact]
        public static void IsWhiteSpace_String_Int()
        {
            var categories = new UnicodeCategory[]
            {
            UnicodeCategory.SpaceSeparator,
            UnicodeCategory.LineSeparator,
            UnicodeCategory.ParagraphSeparator
            };
            foreach (char c in GetTestChars(categories))
                Assert.True(char.IsWhiteSpace(c.ToString(), 0));

            // Some control chars are also considered whitespace for legacy reasons.
            // if ((c >= '\x0009' && c <= '\x000d') || c == '\x0085')
            Assert.True(char.IsWhiteSpace("\u000b", 0));
            Assert.True(char.IsWhiteSpace("\u0085", 0));

            foreach (char c in GetTestCharsNotInCategory(categories))
            {
                // Need to special case some control chars that are treated as whitespace
                if ((c >= '\x0009' && c <= '\x000d') || c == '\x0085') continue;
                Assert.False(char.IsWhiteSpace(c.ToString(), 0));
            }
        }

        [Fact]
        public static void IsWhiteSpace_String_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => char.IsWhiteSpace(null, 0)); // String is null
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsWhiteSpace("abc", -1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => char.IsWhiteSpace("abc", 3)); // Index >= string.Length
        }

        [Fact]
        public static void MaxValue()
        {
            Assert.Equal(0xffff, char.MaxValue);
        }

        [Fact]
        public static void MinValue()
        {
            Assert.Equal(0, char.MinValue);
        }

        [Fact]
        public static void ToLower()
        {
            Assert.Equal('a', char.ToLower('A'));
            Assert.Equal('a', char.ToLower('a'));

            foreach (char c in GetTestChars(UnicodeCategory.UppercaseLetter))
            {
                char lc = char.ToLower(c);
                Assert.NotEqual(c, lc);
                Assert.True(char.IsLower(lc));
            }

            // TitlecaseLetter can have a lower case form (e.g. \u01C8 'Lj' letter which will be 'lj')
            // LetterNumber can have a lower case form (e.g. \u2162 'III' letter which will be 'iii')
            foreach (char c in GetTestCharsNotInCategory(UnicodeCategory.UppercaseLetter, UnicodeCategory.TitlecaseLetter, UnicodeCategory.LetterNumber))
            {
                Assert.Equal(c, char.ToLower(c));
            }
        }

        [Fact]
        public static void ToLowerInvariant()
        {
            Assert.Equal('a', char.ToLowerInvariant('A'));
            Assert.Equal('a', char.ToLowerInvariant('a'));

            foreach (char c in GetTestChars(UnicodeCategory.UppercaseLetter))
            {
                char lc = char.ToLowerInvariant(c);
                Assert.NotEqual(c, lc);
                Assert.True(char.IsLower(lc));
            }

            // TitlecaseLetter can have a lower case form (e.g. \u01C8 'Lj' letter which will be 'lj')
            // LetterNumber can have a lower case form (e.g. \u2162 'III' letter which will be 'iii')
            foreach (char c in GetTestCharsNotInCategory(UnicodeCategory.UppercaseLetter, UnicodeCategory.TitlecaseLetter, UnicodeCategory.LetterNumber))
            {
                Assert.Equal(c, char.ToLowerInvariant(c));
            }
        }

        [Theory]
        [InlineData('a', "a")]
        [InlineData('\uabcd', "\uabcd")]
        public static void ToString1(char c, string expected)
        {
            Assert.Equal(expected, c.ToString());
            Assert.Equal(expected, char.ToString(c));
        }

        [Fact]
        public static void ToUpper()
        {
            Assert.Equal('A', char.ToUpper('A'));
            Assert.Equal('A', char.ToUpper('a'));

            foreach (char c in GetTestChars(UnicodeCategory.LowercaseLetter))
            {
                char lc = char.ToUpper(c);
                Assert.NotEqual(c, lc);
                Assert.True(char.IsUpper(lc));
            }

            // TitlecaseLetter can have a uppercase form (e.g. \u01C8 'Lj' letter which will be 'LJ')
            // LetterNumber can have a uppercase form (e.g. \u2172 'iii' letter which will be 'III')
            foreach (char c in GetTestCharsNotInCategory(UnicodeCategory.LowercaseLetter, UnicodeCategory.TitlecaseLetter, UnicodeCategory.LetterNumber))
            {
                Assert.Equal(c, char.ToUpper(c));
            }
        }

        [Fact]
        public static void ToUpperInvariant()
        {
            Assert.Equal('A', char.ToUpperInvariant('A'));
            Assert.Equal('A', char.ToUpperInvariant('a'));

            foreach (char c in GetTestChars(UnicodeCategory.LowercaseLetter))
            {
                char lc = char.ToUpperInvariant(c);
                Assert.NotEqual(c, lc);
                Assert.True(char.IsUpper(lc));
            }

            // TitlecaseLetter can have a uppercase form (e.g. \u01C8 'Lj' letter which will be 'LJ')
            // LetterNumber can have a uppercase form (e.g. \u2172 'iii' letter which will be 'III')
            foreach (char c in GetTestCharsNotInCategory(UnicodeCategory.LowercaseLetter, UnicodeCategory.TitlecaseLetter, UnicodeCategory.LetterNumber))
            {
                Assert.Equal(c, char.ToUpperInvariant(c));
            }
        }

        public static IEnumerable<object[]> Parse_TestData()
        {
            yield return new object[] { "a", 'a' };
            yield return new object[] { "4", '4' };
            yield return new object[] { " ", ' ' };
            yield return new object[] { "\n", '\n' };
            yield return new object[] { "\0", '\0' };
            yield return new object[] { "\u0135", '\u0135' };
            yield return new object[] { "\u05d9", '\u05d9' };
            yield return new object[] { "\ue001", '\ue001' }; // Private use codepoint

            // Lone surrogate
            yield return new object[] { "\ud801", '\ud801' }; // High surrogate
            yield return new object[] { "\udc01", '\udc01' }; // Low surrogate
        }

        [Theory]
        [MemberData(nameof(Parse_TestData))]
        public static void Parse(string s, char expected)
        {
            char c;
            Assert.True(char.TryParse(s, out c));
            Assert.Equal(expected, c);

            Assert.Equal(expected, char.Parse(s));
        }

        [Theory]
        [InlineData(null, typeof(ArgumentNullException))]
        [InlineData("", typeof(FormatException))]
        [InlineData("\n\r", typeof(FormatException))]
        [InlineData("kj", typeof(FormatException))]
        [InlineData(" a", typeof(FormatException))]
        [InlineData("a ", typeof(FormatException))]
        [InlineData("\\u0135", typeof(FormatException))]
        [InlineData("\u01356", typeof(FormatException))]
        [InlineData("\ud801\udc01", typeof(FormatException))] // Surrogate pair
        public static void Parse_Invalid(string s, Type exceptionType)
        {
            char c;
            Assert.False(char.TryParse(s, out c));
            Assert.Equal(default(char), c);

            Assert.Throws(exceptionType, () => char.Parse(s));
        }

        private static IEnumerable<char> GetTestCharsNotInCategory(params UnicodeCategory[] categories)
        {
            Assert.Equal(s_latinTestSet.Length, s_unicodeTestSet.Length);

            for (int i = 0; i < s_latinTestSet.Length; i++)
            {
                if (Array.Exists(categories, uc => uc == (UnicodeCategory)i))
                    continue;

                char[] latinSet = s_latinTestSet[i];
                for (int j = 0; j < latinSet.Length; j++)
                    yield return latinSet[j];

                char[] unicodeSet = s_unicodeTestSet[i];
                for (int k = 0; k < unicodeSet.Length; k++)
                    yield return unicodeSet[k];
            }
        }

        private static IEnumerable<char> GetTestChars(params UnicodeCategory[] categories)
        {
            for (int i = 0; i < categories.Length; i++)
            {
                char[] latinSet = s_latinTestSet[(int)categories[i]];
                for (int j = 0; j < latinSet.Length; j++)
                    yield return latinSet[j];

                char[] unicodeSet = s_unicodeTestSet[(int)categories[i]];
                for (int k = 0; k < unicodeSet.Length; k++)
                    yield return unicodeSet[k];
            }
        }

        private static char[][] s_latinTestSet = new char[][]
        {
        new char[] {'\u0047','\u004c','\u0051','\u0056','\u00c0','\u00c5','\u00ca','\u00cf','\u00d4','\u00da'}, // UnicodeCategory.UppercaseLetter
        new char[] {'\u0062','\u0068','\u006e','\u0074','\u007a','\u00e1','\u00e7','\u00ed','\u00f3','\u00fa'}, // UnicodeCategory.LowercaseLetter
        new char[] {}, // UnicodeCategory.TitlecaseLetter
        new char[] {}, // UnicodeCategory.ModifierLetter
        new char[] {}, // UnicodeCategory.OtherLetter
        new char[] {}, // UnicodeCategory.NonSpacingMark
        new char[] {}, // UnicodeCategory.SpacingCombiningMark
        new char[] {}, // UnicodeCategory.EnclosingMark
        new char[] {'\u0030','\u0031','\u0032','\u0033','\u0034','\u0035','\u0036','\u0037','\u0038','\u0039'}, // UnicodeCategory.DecimalDigitNumber
        new char[] {}, // UnicodeCategory.LetterNumber
        new char[] {'\u00b2','\u00b3','\u00b9','\u00bc','\u00bd','\u00be'}, // UnicodeCategory.OtherNumber
        new char[] {'\u0020','\u00a0'}, // UnicodeCategory.SpaceSeparator
        new char[] {}, // UnicodeCategory.LineSeparator
        new char[] {}, // UnicodeCategory.ParagraphSeparator
        new char[] {'\u0005','\u000b','\u0011','\u0017','\u001d','\u0082','\u0085','\u008e','\u0094','\u009a'}, // UnicodeCategory.Control
        new char[] {}, // UnicodeCategory.Format
        new char[] {}, // UnicodeCategory.Surrogate
        new char[] {}, // UnicodeCategory.PrivateUse
        new char[] {'\u005f'}, // UnicodeCategory.ConnectorPunctuation
        new char[] {'\u002d','\u00ad'}, // UnicodeCategory.DashPunctuation
        new char[] {'\u0028','\u005b','\u007b'}, // UnicodeCategory.OpenPunctuation
        new char[] {'\u0029','\u005d','\u007d'}, // UnicodeCategory.ClosePunctuation
        new char[] {'\u00ab'}, // UnicodeCategory.InitialQuotePunctuation
        new char[] {'\u00bb'}, // UnicodeCategory.FinalQuotePunctuation
        new char[] {'\u002e','\u002f','\u003a','\u003b','\u003f','\u0040','\u005c','\u00a1','\u00b7','\u00bf'}, // UnicodeCategory.OtherPunctuation
        new char[] {'\u002b','\u003c','\u003d','\u003e','\u007c','\u007e','\u00ac','\u00b1','\u00d7','\u00f7'}, // UnicodeCategory.MathSymbol
        new char[] {'\u0024','\u00a2','\u00a3','\u00a4','\u00a5'}, // UnicodeCategory.CurrencySymbol
        new char[] {'\u005e','\u0060','\u00a8','\u00af','\u00b4','\u00b8'}, // UnicodeCategory.ModifierSymbol
        new char[] {'\u00a6','\u00a7','\u00a9','\u00ae','\u00b0','\u00b6'}, // UnicodeCategory.OtherSymbol
        new char[] {}, // UnicodeCategory.OtherNotAssigned
        };

        private static char[][] s_unicodeTestSet = new char[][]
        {
        new char[] {'\u0102','\u01ac','\u0392','\u0428','\u0508','\u10c4','\u1eb4','\u1fba','\u2c28','\ua668'}, // UnicodeCategory.UppercaseLetter
        new char[] { '\u0107', '\u012D', '\u0140', '\u0151', '\u013A', '\u01A1', '\u01F9', '\u022D', '\u1E09','\uFF45' }, // UnicodeCategory.LowercaseLetter
        new char[] {'\u01c8','\u1f88','\u1f8b','\u1f8e','\u1f99','\u1f9c','\u1f9f','\u1faa','\u1fad','\u1fbc'}, // UnicodeCategory.TitlecaseLetter
        new char[] {'\u02b7','\u02cd','\u07f4','\u1d2f','\u1d41','\u1d53','\u1d9d','\u1daf','\u2091','\u30fe'}, // UnicodeCategory.ModifierLetter
        new char[] {'\u01c0','\u37be','\u4970','\u5b6c','\u6d1e','\u7ed0','\u9082','\ua271','\ub985','\ucb37'}, // UnicodeCategory.OtherLetter
        new char[] {'\u0303','\u034e','\u05b5','\u0738','\u0a4d','\u0e49','\u0fad','\u180b','\u1dd5','\u2dfd'}, // UnicodeCategory.NonSpacingMark
        new char[] {'\u0982','\u0b03','\u0c41','\u0d40','\u0df3','\u1083','\u1925','\u1b44','\ua8b5'         }, // UnicodeCategory.SpacingCombiningMark
        new char[] {'\u20dd','\u20de','\u20df','\u20e0','\u20e2','\u20e3','\u20e4','\ua670','\ua671','\ua672'}, // UnicodeCategory.EnclosingMark
        new char[] {'\u0660','\u0966','\u0ae6','\u0c66','\u0e50','\u1040','\u1810','\u1b50','\u1c50','\ua900'}, // UnicodeCategory.DecimalDigitNumber
        new char[] {'\u2162','\u2167','\u216c','\u2171','\u2176','\u217b','\u2180','\u2187','\u3023','\u3028'}, // UnicodeCategory.LetterNumber
        new char[] {'\u0c78','\u136b','\u17f7','\u2158','\u2471','\u248a','\u24f1','\u2780','\u3220','\u3280'}, // UnicodeCategory.OtherNumber
        new char[] {'\u2004','\u2005','\u2006','\u2007','\u2008','\u2009','\u200a','\u202f','\u205f','\u3000'}, // UnicodeCategory.SpaceSeparator
        new char[] {'\u2028'}, // UnicodeCategory.LineSeparator
        new char[] {'\u2029'}, // UnicodeCategory.ParagraphSeparator
        new char[] {}, // UnicodeCategory.Control
        new char[] {'\u0603','\u17b4','\u200c','\u200f','\u202c','\u2060','\u2063','\u206b','\u206e','\ufff9'}, // UnicodeCategory.Format
        new char[] {'\ud808','\ud8d4','\ud9a0','\uda6c','\udb38','\udc04','\udcd0','\udd9c','\ude68','\udf34'}, // UnicodeCategory.Surrogate
        new char[] {'\ue000','\ue280','\ue500','\ue780','\uea00','\uec80','\uef00','\uf180','\uf400','\uf680'}, // UnicodeCategory.PrivateUse
        new char[] {'\u203f','\u2040','\u2054','\ufe33','\ufe34','\ufe4d','\ufe4e','\ufe4f','\uff3f'}, // UnicodeCategory.ConnectorPunctuation
        new char[] {'\u2e17','\u2e1a','\u301c','\u3030','\u30a0','\ufe31','\ufe32','\ufe58','\ufe63','\uff0d'}, // UnicodeCategory.DashPunctuation
        new char[] {'\u2768','\u2774','\u27ee','\u298d','\u29d8','\u2e28','\u3014','\ufe17','\ufe3f','\ufe5d'}, // UnicodeCategory.OpenPunctuation
        new char[] {'\u276b','\u27c6','\u2984','\u2990','\u29db','\u3009','\u3017','\ufe18','\ufe40','\ufe5e'}, // UnicodeCategory.ClosePunctuation
        new char[] {'\u201b','\u201c','\u201f','\u2039','\u2e02','\u2e04','\u2e09','\u2e0c','\u2e1c','\u2e20'}, // UnicodeCategory.InitialQuotePunctuation
        new char[] {'\u2019','\u201d','\u203a','\u2e03','\u2e05','\u2e0a','\u2e0d','\u2e1d','\u2e21'}, // UnicodeCategory.FinalQuotePunctuation
        new char[] {'\u0589','\u0709','\u0f10','\u16ec','\u1b5b','\u2034','\u2058','\u2e16','\ua8cf','\ufe55'}, // UnicodeCategory.OtherPunctuation
        new char[] {'\u2052','\u2234','\u2290','\u22ec','\u27dd','\u2943','\u29b5','\u2a17','\u2a73','\u2acf'}, // UnicodeCategory.MathSymbol
        new char[] {'\u17db','\u20a2','\u20a5','\u20a8','\u20ab','\u20ae','\u20b1','\u20b4','\ufe69','\uffe1'}, // UnicodeCategory.CurrencySymbol
        new char[] {'\u02c5','\u02da','\u02e8','\u02f3','\u02fc','\u1fc0','\u1fee','\ua703','\ua70c','\ua715'}, // UnicodeCategory.ModifierSymbol
        new char[] {'\u0bf3','\u2316','\u24ac','\u25b2','\u26af','\u285c','\u2e8f','\u2f8c','\u3292','\u3392'}, // UnicodeCategory.OtherSymbol
        new char[] {'\u09c6','\u0dfa','\u2e5c'}, // UnicodeCategory.OtherNotAssigned
        };

        private static char[] s_highSurrogates = new char[] { '\ud800', '\udaaa', '\udbff' }; // Range from '\ud800' to '\udbff'
        private static char[] s_lowSurrogates = new char[] { '\udc00', '\udeee', '\udfff' }; // Range from '\udc00' to '\udfff'
        private static char[] s_nonSurrogates = new char[] { '\u0000', '\ud7ff', '\ue000', '\uffff' };

        private static readonly UnicodeCategory[] s_categoryForLatin1 =
        {
            UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control,    // 0000 - 0007
            UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control,    // 0008 - 000F
            UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control,    // 0010 - 0017
            UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control,    // 0018 - 001F
            UnicodeCategory.SpaceSeparator, UnicodeCategory.OtherPunctuation, UnicodeCategory.OtherPunctuation, UnicodeCategory.OtherPunctuation, UnicodeCategory.CurrencySymbol, UnicodeCategory.OtherPunctuation, UnicodeCategory.OtherPunctuation, UnicodeCategory.OtherPunctuation,    // 0020 - 0027
            UnicodeCategory.OpenPunctuation, UnicodeCategory.ClosePunctuation, UnicodeCategory.OtherPunctuation, UnicodeCategory.MathSymbol, UnicodeCategory.OtherPunctuation, UnicodeCategory.DashPunctuation, UnicodeCategory.OtherPunctuation, UnicodeCategory.OtherPunctuation,    // 0028 - 002F
            UnicodeCategory.DecimalDigitNumber, UnicodeCategory.DecimalDigitNumber, UnicodeCategory.DecimalDigitNumber, UnicodeCategory.DecimalDigitNumber, UnicodeCategory.DecimalDigitNumber, UnicodeCategory.DecimalDigitNumber, UnicodeCategory.DecimalDigitNumber, UnicodeCategory.DecimalDigitNumber,    // 0030 - 0037
            UnicodeCategory.DecimalDigitNumber, UnicodeCategory.DecimalDigitNumber, UnicodeCategory.OtherPunctuation, UnicodeCategory.OtherPunctuation, UnicodeCategory.MathSymbol, UnicodeCategory.MathSymbol, UnicodeCategory.MathSymbol, UnicodeCategory.OtherPunctuation,    // 0038 - 003F
            UnicodeCategory.OtherPunctuation, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter,    // 0040 - 0047
            UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter,    // 0048 - 004F
            UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter,    // 0050 - 0057
            UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.OpenPunctuation, UnicodeCategory.OtherPunctuation, UnicodeCategory.ClosePunctuation, UnicodeCategory.ModifierSymbol, UnicodeCategory.ConnectorPunctuation,    // 0058 - 005F
            UnicodeCategory.ModifierSymbol, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter,    // 0060 - 0067
            UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter,    // 0068 - 006F
            UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter,    // 0070 - 0077
            UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.OpenPunctuation, UnicodeCategory.MathSymbol, UnicodeCategory.ClosePunctuation, UnicodeCategory.MathSymbol, UnicodeCategory.Control,    // 0078 - 007F
            UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control,    // 0080 - 0087
            UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control,    // 0088 - 008F
            UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control,    // 0090 - 0097
            UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control,    // 0098 - 009F
            UnicodeCategory.SpaceSeparator, UnicodeCategory.OtherPunctuation, UnicodeCategory.CurrencySymbol, UnicodeCategory.CurrencySymbol, UnicodeCategory.CurrencySymbol, UnicodeCategory.CurrencySymbol, UnicodeCategory.OtherSymbol, UnicodeCategory.OtherSymbol,    // 00A0 - 00A7
            UnicodeCategory.ModifierSymbol, UnicodeCategory.OtherSymbol, UnicodeCategory.LowercaseLetter, UnicodeCategory.InitialQuotePunctuation, UnicodeCategory.MathSymbol, UnicodeCategory.DashPunctuation, UnicodeCategory.OtherSymbol, UnicodeCategory.ModifierSymbol,    // 00A8 - 00AF
            UnicodeCategory.OtherSymbol, UnicodeCategory.MathSymbol, UnicodeCategory.OtherNumber, UnicodeCategory.OtherNumber, UnicodeCategory.ModifierSymbol, UnicodeCategory.LowercaseLetter, UnicodeCategory.OtherSymbol, UnicodeCategory.OtherPunctuation,    // 00B0 - 00B7
            UnicodeCategory.ModifierSymbol, UnicodeCategory.OtherNumber, UnicodeCategory.LowercaseLetter, UnicodeCategory.FinalQuotePunctuation, UnicodeCategory.OtherNumber, UnicodeCategory.OtherNumber, UnicodeCategory.OtherNumber, UnicodeCategory.OtherPunctuation,    // 00B8 - 00BF
            UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter,    // 00C0 - 00C7
            UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter,    // 00C8 - 00CF
            UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.MathSymbol,    // 00D0 - 00D7
            UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.LowercaseLetter,    // 00D8 - 00DF
            UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter,    // 00E0 - 00E7
            UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter,    // 00E8 - 00EF
            UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.MathSymbol,    // 00F0 - 00F7
            UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter,    // 00F8 - 00FF
        };

        public static IEnumerable<object[]> UpperLowerCasing_TestData()
        {
            //                          lower          upper    Culture
            yield return new object[] { 'a',          'A',      "en-US" };
            yield return new object[] { 'i',          'I',      "en-US" };
            yield return new object[] { '\u0131',     'I',      "tr-TR" };
            yield return new object[] { 'i',          '\u0130', "tr-TR" };
            yield return new object[] { '\u0660',     '\u0660', "en-US" };
        }

        [Fact]
        public static void LatinRangeTest()
        {
            StringBuilder sb = new StringBuilder(256);
            string latineString = sb.ToString();

            for (int i=0; i < latineString.Length; i++)
            {
                Assert.Equal(s_categoryForLatin1[i], char.GetUnicodeCategory(latineString[i]));
                Assert.Equal(s_categoryForLatin1[i], char.GetUnicodeCategory(latineString, i));
            }
        }

        [Fact]
        public static void NonLatinRangeTest()
        {
            for (int i=256; i <= 0xFFFF; i++)
            {
                Assert.Equal(CharUnicodeInfo.GetUnicodeCategory((char)i), char.GetUnicodeCategory((char)i));
            }

            string nonLatinString = "\u0100\u0200\u0300\u0400\u0500\u0600\u0700\u0800\u0900\u0A00\u0B00\u0C00\u0D00\u0E00\u0F00" +
                                    "\u1000\u2000\u3000\u4000\u5000\u6000\u7000\u8000\u9000\uA000\uB000\uC000\uD000\uE000\uF000";
            for (int i=0; i < nonLatinString.Length; i++)
            {
                Assert.Equal(CharUnicodeInfo.GetUnicodeCategory(nonLatinString[i]), char.GetUnicodeCategory(nonLatinString, i));
            }
        }

        [Theory]
        [MemberData(nameof(UpperLowerCasing_TestData))]
        public static void CasingTest(char lowerForm, char upperForm, string cultureName)
        {
            CultureInfo ci = CultureInfo.GetCultureInfo(cultureName);
            Assert.Equal(lowerForm, char.ToLower(upperForm, ci));
            Assert.Equal(upperForm, char.ToUpper(lowerForm, ci));
        }
    }
}
