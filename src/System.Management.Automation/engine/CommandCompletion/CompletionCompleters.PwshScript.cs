// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Collections.Generic;
using System.IO;
using System.Management.Automation.Language;

namespace System.Management.Automation;

public static partial class CompletionCompleters
{
    /// <summary>
    /// Completes a script's arguments as a direct invocation, then maps the edits back to the native command.
    /// </summary>
    /// <returns>
    /// False when the target or source cannot be resolved, allowing ordinary filename completion.
    /// True after delegation, including when there are no candidates or the proposed edit is outside the suffix.
    /// </returns>
    /// <remarks>
    /// Delegation reuses this runspace's registered completers and parameter binding. It may run dynamic
    /// parameter blocks and argument completers, but does not invoke the target script body or launch pwsh.
    /// </remarks>
    private static bool TryCompletePwshScript(CommandAst command, CompletionContext context,
        PwshCompletionContext startup, List<CompletionResult> results)
    {
        ExternalScriptInfo? script = ResolvePwshScript(startup.ScriptTarget, context.ExecutionContext);
        if (script == null || CreatePwshScriptCompletionInput(command, startup, script.Path) is not { } input)
        {
            return false;
        }

        CommandCompletion delegated = CommandCompletion.CompleteInput(input.Text,
            startup.CursorOffset - input.OffsetDelta, context.Options);
        ApplyPwshScriptCompletion(delegated, input.Text.Length, input.PrefixLength, input.OffsetDelta, context, results);
        return true;
    }

    /// <summary>
    /// Resolves a literal script target for delegation without evaluating the target expression.
    /// </summary>
    /// <returns>Script metadata, or null when ordinary filename completion should remain available.</returns>
    private static ExternalScriptInfo? ResolvePwshScript(CommandElementAst? scriptTarget, ExecutionContext executionContext)
    {
        string? target = PwshCompletionContext.GetStaticText(scriptTarget);
        if (string.IsNullOrEmpty(target))
        {
            return null;
        }

        try
        {
            // Startup -File resolves bare names against the current directory rather
            // than searching PATH. Resolve literally through the runspace's location.
            string path = executionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath(
                target, out ProviderInfo provider, out _);
            // Unix -File also accepts other extensions, but ordinary command completion
            // discovers those files as applications. Keep file completion for them rather
            // than altering the shared binder or completion analysis for this completer.
            if (!provider.NameEquals("FileSystem")
                || !Path.GetExtension(path).Equals(".ps1", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return CommandDiscovery.LookupCommandInfo(path, CommandOrigin.Internal, executionContext) as ExternalScriptInfo;
        }
        catch (Exception exception) when (exception is RuntimeException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>
    /// Replaces the native startup prefix with a quoted script invocation while preserving the argument suffix.
    /// </summary>
    /// <returns>
    /// The delegated input, the end of its synthesized prefix, and the offset to add when mapping delegated
    /// positions to the original source; null if the source suffix is unavailable.
    /// </returns>
    private static (string Text, int PrefixLength, int OffsetDelta)? CreatePwshScriptCompletionInput(
        CommandAst command, PwshCompletionContext startup, string scriptPath)
    {
        string? source = command.Extent.StartScriptPosition.GetFullScript();
        int suffixOffset = startup.ScriptArgumentsOffset;
        // An empty argument can place the cursor in whitespace beyond the command's AST extent.
        int endOffset = Math.Max(command.Extent.EndOffset, startup.CursorOffset);
        if (source == null || suffixOffset < 0 || endOffset > source.Length)
        {
            return null;
        }

        // Only the literal script name is synthesized. The complete argument suffix,
        // including text after the cursor, retains its original quoting and expressions.
        // Escape with the parser's rules, including smart quotes, so a filename cannot become executable syntax.
        string invocation = "& '" + CodeGeneration.EscapeSingleQuotedStringContent(scriptPath) + "'";
        // Both inputs share the suffix, so a single translation works for the cursor and replacement range.
        int offsetDelta = suffixOffset - invocation.Length;
        string input = string.Concat(invocation.AsSpan(), source.AsSpan(suffixOffset, endOffset - suffixOffset));
        return (input, invocation.Length, offsetDelta);
    }

    /// <summary>
    /// Applies delegated candidates only when their replacement range stays within the copied argument suffix.
    /// </summary>
    /// <remarks>
    /// A replacement touching the synthesized command name has no valid counterpart in the native input.
    /// Reject the entire result in that case so completion cannot overwrite startup options or the script path.
    /// </remarks>
    private static void ApplyPwshScriptCompletion(CommandCompletion delegated, int inputLength,
        int prefixLength, int offsetDelta, CompletionContext context, List<CompletionResult> results)
    {
        // Completion must only edit the copied suffix, never the synthesized target.
        if (delegated.ReplacementIndex < prefixLength
            || delegated.ReplacementLength < 0
            || delegated.ReplacementIndex + delegated.ReplacementLength > inputLength)
        {
            return;
        }

        context.ReplacementIndex = delegated.ReplacementIndex + offsetDelta;
        context.ReplacementLength = delegated.ReplacementLength;
        results.AddRange(delegated.CompletionMatches);
    }
}
