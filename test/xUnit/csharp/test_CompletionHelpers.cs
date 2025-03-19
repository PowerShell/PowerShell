// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using Xunit;

namespace PSTests.Parallel
{
    public class CompletionHelpersTests
    {
        [Theory]
        [InlineData("word", "'", "'word'")]
        [InlineData("word", "\"", "\"word\"")]
        [InlineData("word's", "'", "'word''s'")]
        [InlineData("already 'quoted'", "'", "'already ''quoted'''")]
        [InlineData("", "'", "''")]
        [InlineData("", "\"", "\"\"")]
        [InlineData("'", "'", "''''")]
        [InlineData("This has a `backtick` and $dollar.", "\"", "\"This has a ``backtick`` and `$dollar.\"")]
        [InlineData("This has a `backtick` and a $dollar.", "'", "'This has a `backtick` and a $dollar.'")]
        [InlineData("`Escaping` backticks only.", "\"", "\"``Escaping`` backticks only.\"")]
        [InlineData("$Only dollars to escape.", "\"", "\"`$Only dollars to escape.\"")]
        [InlineData("word", "", "word")]
        [InlineData("word's", "", "'word''s'")]
        [InlineData("", "", "''")]
        public void TestQuoteCompletionText(
             string completionText,
             string quote,
             string expected)
        {
            string result = CompletionHelpers.QuoteCompletionText(completionText, quote);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("", "", "")]
        [InlineData("\"", "", "\"")]
        [InlineData("'", "", "'")]
        [InlineData("\"word\"", "word", "\"")]
        [InlineData("'word'", "word", "'")]
        [InlineData("\"word", "word", "\"")]
        [InlineData("'word", "word", "'")]
        [InlineData("word\"", "word\"", "")]
        [InlineData("word'", "word'", "")]
        [InlineData("\"word's\"", "word's", "\"")]
        [InlineData("'word\"", "'word\"", "")]
        [InlineData("\"word'", "\"word'", "")]
        [InlineData("'word\"s'", "word\"s", "'")]
        public void TestHandleDoubleAndSingleQuote(string wordToComplete, string expectedWordToComplete, string expectedQuote)
        {
            string quote = CompletionHelpers.HandleDoubleAndSingleQuote(ref wordToComplete);
            Assert.Equal(expectedQuote, quote);
            Assert.Equal(expectedWordToComplete, wordToComplete);
        }
    }
}
