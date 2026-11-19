// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using Xunit;

namespace PSTests.Parallel;

public class PwshConfigurationNameCompletionTests
{
    [Fact]
    public void UnelevatedOrNonWindowsCompletionDoesNotDiscover()
    {
        var results = CompletionCompleters.CompletePwshConfigurationNames(
            string.Empty,
            false,
            () => throw new InvalidOperationException("Must not inspect services"),
            () => throw new InvalidOperationException("Must not discover configurations"));
        Assert.Empty(results);
    }

    [Fact]
    public void StoppedServiceDoesNotDiscover()
    {
        var results = CompletionCompleters.CompletePwshConfigurationNames(
            string.Empty,
            true,
            () => false,
            () => throw new InvalidOperationException("Must not discover configurations"));
        Assert.Empty(results);
    }

    [Fact]
    public void ElevatedDiscoveryFiltersSortsAndDeduplicatesNames()
    {
        var results = CompletionCompleters.CompletePwshConfigurationNames(
            "e",
            true,
            () => true,
            () => new[] { "endpointZulu", "Other", "EndpointAlpha", "ENDPOINTALPHA", null, string.Empty });
        Assert.Equal(new[] { "EndpointAlpha", "endpointZulu" }, results.Select(result => result.ListItemText));
        Assert.All(results, result =>
        {
            Assert.Equal(CompletionResultType.ParameterValue, result.ResultType);
            Assert.Equal(result.ListItemText, result.ToolTip);
        });
    }

    [Fact]
    public void FailedDiscoveryIsBestEffort()
    {
        Assert.Empty(CompletionCompleters.CompletePwshConfigurationNames(
            string.Empty,
            true,
            () => true,
            () => throw new RuntimeException("Unavailable")));
        Assert.Empty(CompletionCompleters.CompletePwshConfigurationNames(
            string.Empty,
            true,
            () => throw new RuntimeException("Unavailable"),
            () => new[] { "Never" }));
    }

    [Theory]
    [InlineData("", "Endpoint With Space", "'Endpoint With Space'")]
    [InlineData("'End'", "Endpoint's Name", "'Endpoint''s Name'")]
    [InlineData("\"End\"", "Endpoint Normal", "\"Endpoint Normal\"")]
    [InlineData("\"End\"", "Endpoint's Name", "\"Endpoint's Name\"")]
    [InlineData("\"End\"", "Endpoint$variable", "'Endpoint$variable'")]
    [InlineData("\"End\"", "Endpoint\"Quoted", "'Endpoint\"Quoted'")]
    [InlineData("", "Endpoint`r`n", "'Endpoint`r`n'")]
    public void ConfigurationNamesRemainLiteralArguments(string prefix, string name, string expected)
    {
        CompletionResult result = Assert.Single(CompletionCompleters.CompletePwshConfigurationNames(
            prefix,
            true,
            () => true,
            () => new[] { name }));
        Assert.Equal(expected, result.CompletionText);
        Ast ast = Parser.ParseInput("pwsh -ConfigurationName " + result.CompletionText, out _, out ParseError[] errors);
        Assert.Empty(errors);
        CommandAst command = (CommandAst)ast.Find(node => node is CommandAst, searchNestedScriptBlocks: false);
        StringConstantExpressionAst argument = Assert.IsType<StringConstantExpressionAst>(command.CommandElements[2]);
        Assert.Equal(name, argument.Value);
    }
}
