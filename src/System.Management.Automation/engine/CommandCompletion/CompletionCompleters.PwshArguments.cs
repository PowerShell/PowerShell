// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Collections.Generic;
using System.IO;
using System.Management.Automation.Language;
using System.Text;

namespace System.Management.Automation;

public static partial class CompletionCompleters
{
    private static void CompletePwshForwardedArgumentValues(
        string script,
        CommandElementAst commandArgument,
        CompletionContext context,
        PwshCompletionContext startup,
        List<CompletionResult> results)
    {
        ScriptBlockAst ast = Parser.ParseInput(script, out _, out ParseError[] errors);
        // Declared parameters change which arguments remain in $args. Do not guess their binding here.
        if (errors.Length != 0 || ast.ParamBlock != null)
        {
            return;
        }

        var arguments = GetPwshForwardedArguments(commandArgument, startup.CursorOffset, out int argumentIndex);
        for (int i = 0; i < argumentIndex; i++)
        {
            if (PwshCompletionContext.GetStaticText(arguments[i]) == null)
            {
                // Expressions and splatting can change the number of native arguments before the cursor.
                return;
            }
        }

        IndexExpressionAst? reference = FindPwshArgumentReference(ast, argumentIndex);
        if (reference?.Parent is not CommandAst target
            || CreatePwshForwardedCompletionInput(target, reference, arguments, argumentIndex, startup.CursorOffset) is not { } input)
        {
            return;
        }

        CommandCompletion? delegated = CompletePwshChildInput(
            input.Text,
            input.Start + startup.CursorOffset - input.OriginalStart,
            context,
            startup);
        if (delegated == null)
        {
            return;
        }

        var matches = new List<CompletionResult>();
        ApplyPwshScriptCompletion(
            delegated,
            input.Start + input.Length,
            input.Start,
            input.OriginalStart - input.Start,
            context,
            matches);
        foreach (CompletionResult match in matches)
        {
            if (match.ResultType is CompletionResultType.ParameterName or CompletionResultType.Command)
            {
                continue;
            }

            results.Add(match);
        }
    }

    private static List<CommandElementAst?> GetPwshForwardedArguments(
        CommandElementAst commandArgument,
        int cursor,
        out int argumentIndex)
    {
        var arguments = new List<CommandElementAst?>();
        argumentIndex = -1;
        foreach (CommandElementAst element in ((CommandAst)commandArgument.Parent).CommandElements)
        {
            if (element.Extent.StartOffset <= commandArgument.Extent.StartOffset)
            {
                continue;
            }

            if (argumentIndex < 0 && cursor <= element.Extent.EndOffset)
            {
                argumentIndex = arguments.Count;
                if (cursor < element.Extent.StartOffset)
                {
                    arguments.Add(null);
                }
            }

            arguments.Add(element);
        }

        if (argumentIndex < 0)
        {
            argumentIndex = arguments.Count;
            arguments.Add(null);
        }

        return arguments;
    }

    private static IndexExpressionAst? FindPwshArgumentReference(ScriptBlockAst ast, int argumentIndex)
    {
        IndexExpressionAst? result = null;
        foreach (Ast node in ast.FindAll(IsPwshArgsVariable, searchNestedScriptBlocks: false))
        {
            // Assignments, computed indexes, and other uses of $args make this inference ambiguous.
            if (node.Parent is not IndexExpressionAst reference
                || reference.Index is not ConstantExpressionAst { Value: int index }
                || index < 0
                || reference.Parent is not CommandAst)
            {
                return null;
            }

            if (index == argumentIndex)
            {
                if (result != null)
                {
                    return null;
                }

                result = reference;
            }
        }

        return result;
    }

    private static bool IsPwshArgsVariable(Ast ast)
        => ast is VariableExpressionAst variable
            && variable.VariablePath.IsUnqualified
            && variable.VariablePath.UserPath.Equals("args", StringComparison.OrdinalIgnoreCase);

    private static (string Text, int Start, int Length, int OriginalStart)? CreatePwshForwardedCompletionInput(
        CommandAst target,
        IndexExpressionAst reference,
        List<CommandElementAst?> arguments,
        int argumentIndex,
        int cursor)
    {
        if (target.GetCommandName() == null)
        {
            return null;
        }

        var input = new StringBuilder();
        int start = -1;
        int length = 0;
        foreach (CommandElementAst element in target.CommandElements)
        {
            if (input.Length > 0)
            {
                input.Append(' ');
            }

            string text;
            if (element is IndexExpressionAst indexExpression
                && IsPwshArgsVariable(indexExpression.Target)
                && indexExpression.Index is ConstantExpressionAst { Value: int index }
                && index >= 0 && index < arguments.Count)
            {
                CommandElementAst? argument = arguments[index];
                if (argument != null && PwshCompletionContext.GetStaticText(argument) == null)
                {
                    return null;
                }

                text = argument?.Extent.Text ?? string.Empty;
                if (element == reference)
                {
                    start = input.Length;
                    length = text.Length;
                }
            }
            else if (PwshCompletionContext.GetStaticText(element) != null)
            {
                text = element.Extent.Text;
            }
            else
            {
                // Do not evaluate other expressions in the command to discover completion metadata.
                return null;
            }

            input.Append(text);
        }

        return start < 0 ? null : (input.ToString(), start, length, arguments[argumentIndex]?.Extent.StartOffset ?? cursor);
    }

    private static CommandCompletion? CompletePwshChildInput(
        string input,
        int cursor,
        CompletionContext context,
        PwshCompletionContext startup)
    {
        try
        {
            string workingDirectory = startup.WorkingDirectory
                ?? context.ExecutionContext.SessionState.Path.CurrentFileSystemLocation.Path;
            string directory = context.ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath(
                workingDirectory,
                out ProviderInfo provider,
                out _);
            if (!provider.NameEquals("FileSystem") || !Directory.Exists(directory))
            {
                return null;
            }

            // Model the child shell in an isolated runspace. Neither the command body nor the
            // caller's location-change callbacks run, and the caller's location/history stay intact.
            using PowerShell shell = PowerShell.Create(RunspaceMode.NewRunspace);
            shell.AddCommand("Set-Location").AddParameter("LiteralPath", directory).Invoke();
            if (shell.HadErrors)
            {
                return null;
            }

            shell.Commands.Clear();
            var output = shell.AddCommand("TabExpansion2")
                .AddArgument(input)
                .AddArgument(cursor)
                .AddArgument(context.Options)
                .Invoke();
            return output.Count == 1 ? output[0].BaseObject as CommandCompletion : null;
        }
        catch (Exception exception) when (exception is RuntimeException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }
}
