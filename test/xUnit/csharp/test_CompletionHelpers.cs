// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using Xunit;

namespace PSTests.Parallel
{
    public class CompletionHelpersTests
    {
        [Theory]
        [InlineData("word", "'", false, false, false, "'word'")]
        [InlineData("word", "\"", false, false, false, "\"word\"")]
        [InlineData("word's", "'", true, false, false, "'word''s'")]
        [InlineData("`command`", "\"", false, true, false, "\"``command``\"")]
        [InlineData("word$", "\"", false, true, false, "\"word`$\"")]
        [InlineData("[word]", "'", true, false, true, "'`[word`]\'")]
        [InlineData("[word]", "\"", false, true, true, "\"``[word``]\"")]
        [InlineData("word", "", false, false, false, "word")]
        [InlineData("word [value]", "'", true, false, true, "'word `[value`]\'")]
        [InlineData("word [value]", "'", false, false, false, "'word [value]'")]
        [InlineData("", "'", false, false, false, "''")]
        [InlineData("", "", false, false, false, "''")]
        [InlineData("word's", "'", false, false, false, "'word's'")]
        [InlineData("word$", "\"", false, false, false, "\"word$\"")]
        public void TestQuoteCompletionText(
            string completionText,
            string quote,
            bool escapeSingleQuoteChars,
            bool escapeDoubleQuoteChars,
            bool escapeGlobbingPathChars,
            string expected)
        {
            string result = CompletionHelpers.QuoteCompletionText(
                completionText,
                quote,
                escapeSingleQuoteChars,
                escapeDoubleQuoteChars,
                escapeGlobbingPathChars);
            Assert.Equal(expected, result);
        }
    }
}
