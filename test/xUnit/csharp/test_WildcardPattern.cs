// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using Xunit;

namespace PSTests.Parallel
{
    public class WildcardPatternTests
    {
        [Fact]
        public void TestEscape_Null()
        {
            Assert.Throws<System.Management.Automation.PSArgumentNullException>(delegate { WildcardPattern.Escape(null); });
        }

        [Fact]
        public void TestEscape_Empty()
        {
            Assert.Equal(WildcardPattern.Escape(string.Empty), string.Empty);
        }

        [Theory]
        [InlineData("a", "a")]
        [InlineData("a*", "a`*")]
        [InlineData("*?[]", "`*`?`[`]")]
        public void TestEscape_String(string source, string expected)
        {
            Assert.Equal(WildcardPattern.Escape(source), expected);
        }

        [Theory]
        [InlineData("a", "a")]
        [InlineData("a*", "a*")]
        [InlineData("*?[]", "*?[]")]
        public void TestEscape_String_NotEscape(string source, string expected)
        {
            Assert.Equal(WildcardPattern.Escape(source, new[] { '*', '?', '[', ']' }), expected);
        }

        [Fact]
        public void TestUnescape_Null()
        {
            Assert.Throws<System.Management.Automation.PSArgumentNullException>(delegate { WildcardPattern.Unescape(null); });
        }

        [Fact]
        public void TestUnescape_Empty()
        {
            Assert.Equal(WildcardPattern.Unescape(string.Empty), string.Empty);
        }

        [Theory]
        [InlineData("a", "a")]
        [InlineData("a`*", "a*")]
        [InlineData("`*`?`[`]", "*?[]")]
        public void TestUnescape_String(string source, string expected)
        {
            Assert.Equal(WildcardPattern.Unescape(source), expected);
        }
    }
}
