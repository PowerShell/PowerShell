// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerShell.Commands;

using System.Linq;
using System.Management.Automation;

using Xunit;

namespace PSTests.Parallel
{
    public class HttpVersionCompleterTests
    {
        [Theory]
        [InlineData("", 4)]
        [InlineData("1", 2)]
        [InlineData("1.", 2)]
        [InlineData("2", 1)]
        [InlineData("3", 1)]
        [InlineData("4", 0)]
        [InlineData("a", 0)]
        public void WellKnownVersion(string wordToComplete, int expectedCount)
        {
            var completer = new HttpVersionCompleter();
            var result = completer.CompleteArgument(default, default, wordToComplete, default, default).ToArray();

            Assert.Equal(expectedCount, result.Length);
            Assert.All(
                result,
                r =>
                {
                    Assert.Equal(CompletionResultType.Text, r.ResultType);
                    Assert.Contains(r.CompletionText, HttpVersionUtils.AllowedVersions);
                    Assert.StartsWith(wordToComplete, r.CompletionText);
                });
        }
    }
}
