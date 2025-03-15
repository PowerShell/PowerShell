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
