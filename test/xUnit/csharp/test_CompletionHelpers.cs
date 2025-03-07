// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
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
        public void TestHandleDoubleAndSingleQuote(string wordToComplete, string expectedWordToComplete, string expectedQuote)
        {
            string quote = CompletionHelpers.HandleDoubleAndSingleQuote(ref wordToComplete);
            Assert.Equal(expectedQuote, quote);
            Assert.Equal(expectedWordToComplete, wordToComplete);
        }

        [Theory]
        [InlineData("", false, true)]
        [InlineData("simpleWord", false, false)]
        [InlineData("word1 word2", false, true)]
        [InlineData("keyword", false, false)]
        [InlineData("word*", true, false)]
        [InlineData("word*", false, false)]
        [InlineData("\"alreadyQuoted\"", false, false)]
        [InlineData("var-name", false, false)]
        [InlineData("${variable}", false, false)]
        [InlineData("${variable}", true, false)]
        [InlineData("file[name]", false, false)]
        [InlineData("file[name]", true, true)]
        [InlineData("`command`", false, true)]
        [InlineData("word`command`", false, true)]
        [InlineData("`unmatchedBacktick", false, true)]
        [InlineData("`", false, true)]
        [InlineData("word1 `word2`", false, true)]
        [InlineData("`command with spaces`", false, true)]
        [InlineData("word`another`word", false, true)]
        public void TestCompletionRequiresQuotes(string completion, bool escapeGlobbingPathChars, bool expected)
        {
            bool result = CompletionHelpers.CompletionRequiresQuotes(completion, escapeGlobbingPathChars);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("word", "'", false, "'word'")]
        [InlineData("word", "\"", false, "\"word\"")]
        [InlineData("word's", "'", false, "'word''s'")]
        [InlineData("`command`", "\"", false, "\"``command``\"")]
        [InlineData("word$", "\"", false, "\"word`$\"")]
        [InlineData("[word]", "'", true, "'`[word`]\'")]
        [InlineData("[word]", "\"", true, "\"``[word``]\"")]
        [InlineData("word", "", false, "word")]
        [InlineData("word [value]", "'", true, "'word `[value`]\'")]
        [InlineData("word [value]", "'", false, "'word [value]'")]
        [InlineData("", "'", false, "''")]
        [InlineData("", "", false, "''")]
        public void TestQuoteCompletionText(string completionText, string quote, bool escapeGlobbingPathChars, string expected)
        {
            string result = CompletionHelpers.QuoteCompletionText(completionText, quote, escapeGlobbingPathChars);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestGetMatchingResults_BasicMatch()
        {
            string wordToComplete = "word";
            var possibleCompletionValues = new[] { "word", "word2", "anotherWord" };

            var results = CompletionHelpers.GetMatchingResults(wordToComplete, possibleCompletionValues).ToList();

            Assert.Equal(2, results.Count);
            Assert.Contains(results, r => r.CompletionText == "word");
            Assert.Contains(results, r => r.CompletionText == "word2");
            Assert.All(results, r => Assert.Equal(CompletionResultType.Text, r.ResultType));
        }

        [Fact]
        public void TestGetMatchingResults_NoMatch()
        {
            string wordToComplete = "noMatch";
            var possibleCompletionValues = new[] { "word", "word2", "anotherWord" };

            var results = CompletionHelpers.GetMatchingResults(wordToComplete, possibleCompletionValues).ToList();

            Assert.Empty(results);
        }

        [Fact]
        public void TestGetMatchingResults_EscapeGlobbingPathChars_Enabled()
        {
            string wordToComplete = "file";
            var possibleCompletionValues = new[] { "file[name]", "file[other]", "file*" };

            var results = CompletionHelpers.GetMatchingResults(wordToComplete, possibleCompletionValues, escapeGlobbingPathChars: true).ToList();

            Assert.Equal(3, results.Count);
            Assert.Contains(results, r => r.CompletionText == "'file`[name`]'");
            Assert.Contains(results, r => r.CompletionText == "'file`[other`]'");
            Assert.Contains(results, r => r.CompletionText == "file*");
            Assert.All(results, r => Assert.Equal(CompletionResultType.Text, r.ResultType));
        }

        [Fact]
        public void TestGetMatchingResults_EscapeGlobbingPathChars_Disabled()
        {
            string wordToComplete = "file";
            var possibleCompletionValues = new[] { "file[name]", "file[other]", "file*" };

            var results = CompletionHelpers.GetMatchingResults(wordToComplete, possibleCompletionValues, escapeGlobbingPathChars: false).ToList();

            Assert.Equal(3, results.Count);
            Assert.Contains(results, r => r.CompletionText == "file[name]");
            Assert.Contains(results, r => r.CompletionText == "file[other]");
            Assert.Contains(results, r => r.CompletionText == "file*");
            Assert.All(results, r => Assert.Equal(CompletionResultType.Text, r.ResultType));
        }

        [Fact]
        public void TestGetMatchingResults_WithToolTipMapping()
        {
            string wordToComplete = "word";
            var possibleCompletionValues = new[] { "word", "word2" };
            Func<string, string> toolTipMapping = value => $"Tooltip for {value}";

            var results = CompletionHelpers.GetMatchingResults(wordToComplete, possibleCompletionValues, toolTipMapping: toolTipMapping).ToList();

            Assert.Equal(2, results.Count);
            Assert.Contains(results, r => r.CompletionText == "word");
            Assert.Contains(results, r => r.CompletionText == "word2");
            Assert.Contains(results, r => r.ToolTip == "Tooltip for word");
            Assert.Contains(results, r => r.ToolTip == "Tooltip for word2");
            Assert.All(results, r => Assert.Equal(CompletionResultType.Text, r.ResultType));
        }

        [Fact]
        public void TestGetMatchingResults_WithListItemTextMapping()
        {
            string wordToComplete = "word";
            var possibleCompletionValues = new[] { "word", "word2" };
            Func<string, string> listItemTextMapping = value => $"Item: {value}";

            var results = CompletionHelpers.GetMatchingResults(wordToComplete, possibleCompletionValues, listItemTextMapping: listItemTextMapping).ToList();

            Assert.Equal(2, results.Count);
            Assert.Contains(results, r => r.CompletionText == "word");
            Assert.Contains(results, r => r.CompletionText == "word2");
            Assert.Contains(results, r => r.ListItemText == "Item: word");
            Assert.Contains(results, r => r.ListItemText == "Item: word2");
            Assert.All(results, r => Assert.Equal(CompletionResultType.Text, r.ResultType));
        }

        [Fact]
        public void TestGetMatchingResults_SingleQuote()
        {
            string wordToComplete = "'word";
            var possibleCompletionValues = new[] { "word", "word2" };

            var results = CompletionHelpers.GetMatchingResults(wordToComplete, possibleCompletionValues).ToList();

            Assert.Equal(2, results.Count);
            Assert.Contains(results, r => r.CompletionText == "'word'");
            Assert.Contains(results, r => r.CompletionText == "'word2'");
            Assert.All(results, r => Assert.Equal(CompletionResultType.Text, r.ResultType));
        }

        [Fact]
        public void TestGetMatchingResults_DoubleQuote()
        {
            string wordToComplete = "\"word";
            var possibleCompletionValues = new[] { "word", "word2" };

            var results = CompletionHelpers.GetMatchingResults(wordToComplete, possibleCompletionValues).ToList();

            Assert.Equal(2, results.Count);
            Assert.Contains(results, r => r.CompletionText == "\"word\"");
            Assert.Contains(results, r => r.CompletionText == "\"word2\"");
            Assert.All(results, r => Assert.Equal(CompletionResultType.Text, r.ResultType));
        }

        [Fact]
        public void TestGetMatchingResults_MixedCasesAndPatternMatch()
        {
            string wordToComplete = "Word";
            var possibleCompletionValues = new[] { "word1", "Word2", "anotherWord" };

            var results = CompletionHelpers.GetMatchingResults(wordToComplete, possibleCompletionValues).ToList();

            Assert.Equal(2, results.Count);
            Assert.Contains(results, r => r.CompletionText == "word1");
            Assert.Contains(results, r => r.CompletionText == "Word2");
            Assert.All(results, r => Assert.Equal(CompletionResultType.Text, r.ResultType));
        }

        [Fact]
        public void GetMatchingResults_WithParameterValueResultType()
        {
            string wordToComplete = "word";
            var possibleCompletionValues = new[] { "word", "word2" };
            CompletionResultType resultType = CompletionResultType.ParameterValue;

            var results = CompletionHelpers.GetMatchingResults(wordToComplete, possibleCompletionValues, resultType: resultType).ToList();

            Assert.Equal(2, results.Count);
            Assert.Contains(results, r => r.CompletionText == "word");
            Assert.Contains(results, r => r.CompletionText == "word2");
            Assert.All(results, r => Assert.Equal(CompletionResultType.ParameterValue, r.ResultType));
        }
    }
}
