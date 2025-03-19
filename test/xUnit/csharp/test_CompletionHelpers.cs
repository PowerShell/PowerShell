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
        [InlineData("normaltext", false)]
        [InlineData("$variable", false)]
        [InlineData("abc def", true)]
        [InlineData("keyword", false)]
        [InlineData("key$word", true)]
        [InlineData("abc`def", true)]
        [InlineData("normal-text", false)]
        [InlineData("\"doublequotes\"", false)]
        [InlineData("'singlequotes'", false)]
        [InlineData("normal 'text'", true)]
        [InlineData("normal \"text\"", true)]
        [InlineData("text with ' and \"", true)]
        [InlineData("text\"with\"quotes", false)]
        [InlineData("text'with'quotes", false)]
        [InlineData("\"key$", true)]
        [InlineData("\"", true)]
        [InlineData("'", true)]
        [InlineData("", true)]
        public void TestCompletionRequiresQuotes(string completion, bool expected)
        {
            bool result = CompletionHelpers.CompletionRequiresQuotes(completion);
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
