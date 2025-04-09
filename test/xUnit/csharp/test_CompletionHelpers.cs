// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using Xunit;

namespace PSTests.Parallel
{
    public class CompletionHelpersTests
    {
        [Theory]
        [InlineData("", "'", "''")]
        [InlineData("", "\"", "\"\"")]
        [InlineData("'", "'", "''''")]
        [InlineData("", "", "''")]
        [InlineData("", null, "''")]
        [InlineData("word", "'", "'word'")]
        [InlineData("word", "\"", "\"word\"")]
        [InlineData("word's", "'", "'word''s'")]
        [InlineData("already 'quoted'", "'", "'already ''quoted'''")]
        [InlineData("multiple 'quotes' in 'text'", "'", "'multiple ''quotes'' in ''text'''")]
        [InlineData("\"word\"", "'", "'\"word\"'")]
        [InlineData("'word'", "'", "''word''")]
        [InlineData("word", "", "word")]
        [InlineData("word", null, "word")]
        [InlineData("\"word\"", "\"", "\"\"word\"\"")]
        [InlineData("'word'", "\"", "\"'word'\"")]
        [InlineData("word with space", "'", "'word with space'")]
        [InlineData("word with space", "\"", "\"word with space\"")]
        [InlineData("word\"with\"quotes", "'", "'word\"with\"quotes'")]
        [InlineData("word'with'quotes", "\"", "\"word'with'quotes\"")]
        [InlineData("while", "'", "'while'")]
        [InlineData("while", "", "while")]
        [InlineData("while", "\"", "\"while\"")]
        [InlineData("$variable", "'", "'$variable'")]
        [InlineData("$variable", "", "$variable")]
        [InlineData("$variable", "\"", "\"$variable\"")]
        [InlineData("key$word", "'", "'key$word'")]
        [InlineData("key$word", "", "'key$word'")]
        [InlineData("key$word", "\"", "\"key$word\"")]
        [InlineData("`r`n", "\"", "\"`r`n\"")]
        [InlineData("`r`n", "'", "\"`r`n\"")]
        [InlineData("`r`n", "", "\"`r`n\"")]
        [InlineData("`r`n    `${0}", "\"", "\"`r`n    `${0}\"")]
        [InlineData("`r`n    `${0}", "'", "\"`r`n    `${0}\"")]
        [InlineData("`r`n    `${0}", "", "\"`r`n    `${0}\"")]
        [InlineData("`n", "\"", "\"`n\"")]
        [InlineData("`n", "'", "\"`n\"")]
        [InlineData("`n", "", "\"`n\"")]
        [InlineData("`n    `${0}", "\"", "\"`n    `${0}\"")]
        [InlineData("`n    `${0}", "'", "\"`n    `${0}\"")]
        [InlineData("`n    `${0}", "", "\"`n    `${0}\"")]
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
        [InlineData("while", false)] // PowerShell keyword
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
        [InlineData(";", true)]
        [InlineData("; ", true)]
        [InlineData(",", true)]
        [InlineData(", ", true)]
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

        [Theory]
        [InlineData("word", "word", true)]
        [InlineData("Word", "word", true)]
        [InlineData("word", "wor", true)]
        [InlineData("word", "words", false)]
        [InlineData("word`nnext", "word`n", true)]
        [InlineData("word`nnext", "word\n", true)]
        [InlineData("word`r`nnext", "word`r`n", true)]
        [InlineData("word`r`nnext", "word\r\n", true)]
        [InlineData("word;next", "word;", true)]
        [InlineData("word,next", "word,", true)]
        [InlineData("word[*]next", "word[*", true)]
        [InlineData("word[abc]next", "word[abc", true)]
        [InlineData("word", "word*", true)]
        [InlineData("testword", "test", true)]
        [InlineData("word", "", true)]
        [InlineData("", "word", false)]
        public void TestIsMatch(string value, string wordToComplete, bool expected)
        {
            bool result = CompletionHelpers.IsMatch(value, wordToComplete);
            Assert.Equal(expected, result);
        }
    }
}
