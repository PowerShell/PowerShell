// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Management.Automation;
using System.Management.Automation.Language;
using Xunit;

namespace PSTests.Parallel;

public class PwshCompletionContextTests
{
    // The marker makes cursor positions before the end of the input explicit.
    [Theory]
    [InlineData("pwsh |", "OptionName", null)]
    [InlineData("pwsh -NoP|rofile -File later.ps1", "OptionName", null)]
    [InlineData("pwsh -NoProfile |", "OptionName", null)]
    [InlineData("pwsh --noprofile |", "OptionName", null)]
    [InlineData("pwsh /noprofile |", "OptionName", null)]
    [InlineData("pwsh -OutputFormat |", "OptionValue", "OutputFormat")]
    [InlineData("pwsh -o x|ml -File later.ps1", "OptionValue", "OutputFormat")]
    [InlineData("pwsh -of 'x|ml'", "OptionValue", "OutputFormat")]
    [InlineData("pwsh -of xml |", "OptionName", null)]
    [InlineData("pwsh -WorkingDirectory 'a b' |", "OptionName", null)]
    [InlineData("pwsh -f |", "ScriptPath", "File")]
    [InlineData("pwsh -File 'a b|.ps1'", "ScriptPath", "File")]
    [InlineData("pwsh -File \"unfinished|", "ScriptPath", "File")]
    [InlineData("pwsh -File 'unfinished|", "ScriptPath", "File")]
    [InlineData("pwsh -NoProfile ./a|.ps1", "ScriptPath", null)]
    [InlineData("pwsh ./a.ps1 |", "ScriptArguments", null)]
    [InlineData("pwsh ./a.ps1 -NoP|rofile", "ScriptArguments", null)]
    [InlineData("pwsh -File 'a b.ps1' -Par|am value", "ScriptArguments", null)]
    [InlineData("pwsh -File -|", "ScriptPath", "File")]
    [InlineData("pwsh -File '-|'", "ScriptPath", "File")]
    [InlineData("pwsh -File '-'|", "ScriptPath", "File")]
    [InlineData("pwsh -File \"-\"|", "ScriptPath", "File")]
    [InlineData("pwsh -File - |", "Opaque", "File")]
    [InlineData("pwsh -File '-' |", "Opaque", "File")]
    [InlineData("pwsh -File \"-\" |", "Opaque", "File")]
    [InlineData("pwsh -c |", "Opaque", "Command")]
    [InlineData("pwsh -c Get-It -NoP|rofile", "Opaque", "Command")]
    [InlineData("pwsh -cwa |", "Opaque", "CommandWithArgs")]
    [InlineData("pwsh -e |", "Opaque", "EncodedCommand")]
    [InlineData("pwsh -ec ab|cd", "Opaque", "EncodedCommand")]
    [InlineData("pwsh -ec abcd |", "OptionName", null)]
    [InlineData("pwsh -Version |", "Opaque", "Version")]
    [InlineData("pwsh -unknown |", "ScriptArguments", null)]
    [InlineData("pwsh -review|", "ScriptPath", null)]
    [InlineData("pwsh --review|", "ScriptPath", null)]
    [InlineData("pwsh '-review|'", "ScriptPath", null)]
    [InlineData("pwsh -|", "OptionName", null)]
    [InlineData("pwsh --|", "OptionName", null)]
    [InlineData("pwsh -ep|", "OptionName", null)]
    [InlineData("pwsh /tmp/foo|.ps1", "ScriptPath", null)]
    [InlineData("pwsh /tmp/foo.ps1 -P|aram", "ScriptArguments", null)]
    [InlineData("pwsh -script.ps1 -P|aram", "ScriptArguments", null)]
    [InlineData("pwsh - |", "Opaque", null)]
    [InlineData("pwsh $(Get-Thing|", "ScriptPath", null)]
    [InlineData("pwsh $target -P|aram", "ScriptArguments", null)]
    [InlineData("pwsh -of $(Get-Thing) |", "Opaque", null)]
    [InlineData("pwsh -File $target -P|aram", "ScriptArguments", null)]
    public void RecognizesCursorOwner(string markedInput, string kind, string? option)
    {
        PwshCompletionContext context = Scan(markedInput);
        Assert.Equal(kind, context.Kind.ToString());
        Assert.Equal(option, context.Option?.Name);
    }

    [Theory]
    [InlineData("pwsh -File 'a b.ps1' -Pa|ram later", "'a b.ps1'", "-Param")]
    [InlineData("pwsh -NoProfile ./a.ps1 |", "./a.ps1", null)]
    [InlineData("pwsh -File $target -Pa|ram", "$target", "-Param")]
    public void PreservesOriginalSourceExtents(string input, string script, string? current)
    {
        PwshCompletionContext context = Scan(input);
        Assert.NotNull(context.ScriptTarget);
        Assert.Equal(script, context.ScriptTarget.Extent.Text);
        Assert.Equal(current, context.CurrentElement?.Extent.Text);
        Assert.Equal(context.ScriptTarget.Extent.EndOffset, context.ScriptArgumentsOffset);
        Assert.Equal(input.IndexOf('|'), context.CursorOffset);
    }

    private static PwshCompletionContext Scan(string markedInput)
    {
        int cursor = markedInput.IndexOf('|');
        string input = markedInput.Remove(cursor, 1);
        ScriptBlockAst ast = Parser.ParseInput(input, out _, out _);
        var command = (CommandAst)ast.Find(node => node is CommandAst, searchNestedScriptBlocks: true);
        return PwshCompletionContext.Scan(command, cursor);
    }
}
