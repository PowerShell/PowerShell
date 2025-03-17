// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using Xunit;

namespace PSTests.Parallel
{
    public class CompletionHelpersTests
    {
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
