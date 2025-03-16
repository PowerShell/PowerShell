// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using Xunit;

namespace PSTests.Parallel
{
    public class CompletionHelpersTests
    {
        [Theory]
        [InlineData("word", "'", false, "'word'")]
        [InlineData("word", "\"", false, "\"word\"")]
        [InlineData("word's", "'", true, "'word''s'")]
        [InlineData("word's", "'", false, "'word's'")]
        [InlineData("already 'quoted'", "'", true, "'already ''quoted'''")]
        [InlineData("already 'quoted'", "'", false, "'already 'quoted''")]
        [InlineData("", "'", true, "''")]
        [InlineData("", "\"", false, "\"\"")]
        [InlineData("'", "'", true, "''''")]
        public void TestQuoteCompletionText(
             string completionText,
             string quote,
             bool escapeSingleQuoteChars,
             string expected)
        {
            string result = CompletionHelpers.QuoteCompletionText(completionText, quote, escapeSingleQuoteChars);
            Assert.Equal(expected, result);
        }
    }
}
