// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Language;
using Xunit;

namespace PSTests.Parallel
{
    public static class ScriptBlockTests
    {
        private const string FakeFile = "/fake/Script.ps1";

        // Each source has one function, script block literal, or is the script block itself. The script block has
        // the string 'marker' in its body, the test checks it and any parameters are at the same line and column
        // once the text from GetScriptWithPosition is parsed again.
        [Theory]
        [InlineData("root", "using namespace System.Text\n[StringBuilder]::new()\n  'marker'")]
        [InlineData("root", "\n\n  param($Value = 'default')\n'marker'")]
        [InlineData("literal", "$v = 1\n  $sbk = { 'marker' }")]
        [InlineData("literal", "\n\tStart-Job {\n    param($Value = 'default')\n    $using:v; 'marker'\n}")]
        [InlineData("literal", "& { [CmdletBinding()] param($Value) begin { 'begin' } process { 'process' } end { 'marker' } }")]
        [InlineData("function", "\n  function Test { param($Value = 'default')\n  'marker' }")]
        [InlineData("function", "\n  function Test($a = 'a', $b = 'b') { \"$a $b\"; 'marker' }")]
        [InlineData("function", "function Test(\n    # Comment ) between { the parameters.\n    [string]\n    $Value = 'default'\n    ,\n    [int]$Other = 2 # Trailing ) comment.\n)\n{\n    'marker'\n}")]
        [InlineData("function", "function Test() {\n    'marker'\n}")]
        [InlineData("function", "function global:Test($Value) { 'marker' }")]
        [InlineData("function", "filter Test {\n    'marker'\n}")]
        [InlineData("function", "filter Test($Value = 'default') {\n    'marker'\n}")]
        [InlineData("function", "filter Test {\n    param($Value = 'default')\n    'marker'\n}")]
        [InlineData("function", "function Test($Value = 'default') {\n    begin { 'begin' }\n    process { 'process' }\n    end { 'marker' }\n}")]
        [InlineData("function", "function Test {\n    [CmdletBinding()]\n    param($Value = 'default')\n    'marker'\n}")]
        [InlineData("function", "function Test([Parameter()]$Value = 'default') {\n    'marker'\n}")]
        public static void GetScriptWithPositionKeepsFileAndPosition(string kind, string source)
        {
            foreach (string newLine in new[] { "\n", "\r\n" })
            {
                Ast ast = GetTargetAst(kind, source.Replace("\n", newLine), FakeFile);
                ScriptBlock scriptBlock = NewScriptBlock(ast);

                string text = scriptBlock.GetScriptWithPosition(out string fileName);

                Assert.Equal(FakeFile, fileName);
                ScriptBlockAst actual = Parser.ParseInput(text, fileName, out _, out ParseError[] errors);
                Assert.Empty(errors);
                AssertSamePosition(FindMarker(ast), FindMarker(actual));

                // A parameter list in the function declaration becomes the param block.
                ReadOnlyCollection<ParameterAst> expectedParams = ast is FunctionDefinitionAst { Parameters: not null } fda
                    ? fda.Parameters
                    : GetBody(ast).ParamBlock?.Parameters;
                ReadOnlyCollection<ParameterAst> actualParams = actual.ParamBlock?.Parameters;
                Assert.Equal(expectedParams?.Count ?? 0, actualParams?.Count ?? 0);
                for (int i = 0; i < (expectedParams?.Count ?? 0); i++)
                {
                    Assert.Equal(expectedParams[i].Extent.Text, actualParams[i].Extent.Text);
                    AssertSamePosition(expectedParams[i], actualParams[i]);
                }

                // The script must be the body only, the function isn't defined again in the target.
                Assert.Null(actual.Find(static a => a is FunctionDefinitionAst, searchNestedScriptBlocks: true));
            }
        }

        [Theory]
        [InlineData("root", "\n  'marker'")]
        [InlineData("literal", "\n  $sbk = { 'marker' }")]
        [InlineData("function", "\n  function Test($Value) { 'marker' }")]
        public static void GetScriptWithPositionUsesToStringWithoutFile(string kind, string source)
        {
            // Without a file there is nothing for the line and column to refer to.
            ScriptBlock scriptBlock = NewScriptBlock(GetTargetAst(kind, source, fileName: null));

            string text = scriptBlock.GetScriptWithPosition(out string fileName);

            Assert.Null(fileName);
            Assert.Equal(scriptBlock.ToString(), text);
        }

        private static Ast GetTargetAst(string kind, string source, string fileName)
        {
            ScriptBlockAst root = Parser.ParseInput(source, fileName, out _, out ParseError[] errors);
            Assert.Empty(errors);

            return kind switch
            {
                "root" => root,
                "literal" => ((ScriptBlockExpressionAst)root.Find(static a => a is ScriptBlockExpressionAst, searchNestedScriptBlocks: false)).ScriptBlock,
                "function" => root.Find(static a => a is FunctionDefinitionAst, searchNestedScriptBlocks: false),
                _ => throw new ArgumentException(kind, nameof(kind)),
            };
        }

        private static ScriptBlockAst GetBody(Ast ast)
            => ast as ScriptBlockAst ?? ((FunctionDefinitionAst)ast).Body;

        private static ScriptBlock NewScriptBlock(Ast ast)
            => ast is FunctionDefinitionAst fda ? new ScriptBlock(fda, fda.IsFilter) : new ScriptBlock((ScriptBlockAst)ast, isFilter: false);

        private static Ast FindMarker(Ast ast)
        {
            Ast marker = ast.Find(static a => a is StringConstantExpressionAst { Value: "marker" }, searchNestedScriptBlocks: true);
            Assert.NotNull(marker);
            return marker;
        }

        private static void AssertSamePosition(Ast expected, Ast actual)
        {
            Assert.Equal(expected.Extent.File, actual.Extent.File);
            Assert.Equal(expected.Extent.StartLineNumber, actual.Extent.StartLineNumber);
            Assert.Equal(expected.Extent.StartColumnNumber, actual.Extent.StartColumnNumber);
            Assert.Equal(expected.Extent.EndLineNumber, actual.Extent.EndLineNumber);
            Assert.Equal(expected.Extent.EndColumnNumber, actual.Extent.EndColumnNumber);
        }
    }
}
