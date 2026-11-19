// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Management.Automation.Language;

namespace System.Management.Automation;

internal enum PwshCompletionContextKind
{
    OptionName,
    OptionValue,
    ScriptPath,
    ScriptArguments,
    Opaque,
}

/// <summary>
/// Identifies the owner of the cursor without evaluating arguments or resolving paths.
/// All offsets and ASTs refer to the original input, including when it contains quotes.
/// </summary>
internal sealed class PwshCompletionContext
{
    private PwshCompletionContext(PwshCompletionContextKind kind, int cursorOffset,
        PwshStartupOption? option, CommandElementAst? currentElement, CommandElementAst? scriptTarget)
    {
        Kind = kind;
        CursorOffset = cursorOffset;
        Option = option;
        CurrentElement = currentElement;
        ScriptTarget = scriptTarget;
    }

    internal PwshCompletionContextKind Kind { get; }

    internal int CursorOffset { get; }

    internal PwshStartupOption? Option { get; }

    internal CommandElementAst? CurrentElement { get; }

    internal CommandElementAst? ScriptTarget { get; }

    /// <summary>The start of the original source suffix to copy for script delegation.</summary>
    internal int ScriptArgumentsOffset => ScriptTarget?.Extent.EndOffset ?? -1;

    /// <summary>
    /// Classifies the cursor by consuming startup arguments up to its position in the original command.
    /// </summary>
    /// <remarks>
    /// The current element is still being edited, so it is classified before committing its effect on
    /// subsequent arguments. Completed elements establish a pending value, a script target, or opaque input.
    /// Keeping this scan syntactic avoids executing expressions merely to determine what to complete.
    /// </remarks>
    internal static PwshCompletionContext Scan(CommandAst command, int cursorOffset)
    {
        PwshStartupOption? pending = null;
        CommandElementAst? scriptTarget = null;
        bool opaque = false;

        // The caller has already identified the executable. Do not inspect command resolution here.
        for (int i = 1; i < command.CommandElements.Count; i++)
        {
            CommandElementAst element = command.CommandElements[i];
            if (element.Extent.StartOffset > cursorOffset)
            {
                break;
            }

            // At the token's end the user is still completing it; whitespace moves to the next argument.
            bool current = cursorOffset <= element.Extent.EndOffset;
            if (opaque)
            {
                return Context(PwshCompletionContextKind.Opaque, current ? element : null);
            }

            if (scriptTarget != null)
            {
                return Context(PwshCompletionContextKind.ScriptArguments, current ? element : FindCurrentElement());
            }

            if (pending != null)
            {
                if (pending.ArgumentKind == PwshStartupArgumentKind.ScriptPath)
                {
                    if (GetStaticText(element) == "-")
                    {
                        // Standard input has no script metadata to delegate to after the sentinel is complete.
                        return Context(current ? PwshCompletionContextKind.ScriptPath
                            : PwshCompletionContextKind.Opaque, current ? element : null);
                    }

                    if (current)
                    {
                        return Context(PwshCompletionContextKind.ScriptPath, element);
                    }

                    scriptTarget = element;
                    pending = null;
                    continue;
                }

                if (current)
                {
                    return Context(IsEncodedPayload(pending) ? PwshCompletionContextKind.Opaque
                        : PwshCompletionContextKind.OptionValue, element);
                }

                // An expression is one native argument syntactically, but its evaluated
                // argument count cannot be known by this scanner.
                opaque = GetStaticText(element) == null;
                pending = null;
                continue;
            }

            string? text = GetStaticText(element);
            if (text == null)
            {
                if (current)
                {
                    return Context(PwshCompletionContextKind.ScriptPath, element);
                }

                scriptTarget = element;
                continue;
            }

            if (PwshStartupOptions.TryGetSwitchKey(text, out string key))
            {
                if (current)
                {
                    // Unknown switches can be implicit script paths. Keep filename completion available
                    // for both dash-prefixed filenames and absolute Unix paths once no option can match.
                    if (!IsOptionPrefix(key))
                    {
                        return Context(PwshCompletionContextKind.ScriptPath, element);
                    }

                    return Context(PwshCompletionContextKind.OptionName, element);
                }

                pending = PwshStartupOptions.Find(key);
                if (pending == null)
                {
                    // Unrecognized switches are interpreted as implicit file names by
                    // the host, including absolute Unix paths beginning with '/'.
                    if (text == "-")
                    {
                        opaque = true;
                    }
                    else
                    {
                        scriptTarget = element;
                    }
                }
                else if (pending.StopsParsing && pending.ArgumentKind != PwshStartupArgumentKind.ScriptPath)
                {
                    // Command payloads, including missing payloads, are deliberately unsupported.
                    opaque = true;
                }
                else if (pending.Consumption == PwshStartupArgumentConsumption.None)
                {
                    pending = null;
                }

                continue;
            }

            if (current)
            {
                return Context(PwshCompletionContextKind.ScriptPath, element);
            }

            scriptTarget = element;
        }

        // No element owns a cursor in trailing whitespace; the consumed arguments determine what comes next.
        return Context(opaque ? PwshCompletionContextKind.Opaque
            : scriptTarget != null ? PwshCompletionContextKind.ScriptArguments
            : pending?.ArgumentKind == PwshStartupArgumentKind.ScriptPath ? PwshCompletionContextKind.ScriptPath
            : pending != null && IsEncodedPayload(pending) ? PwshCompletionContextKind.Opaque
            : pending != null ? PwshCompletionContextKind.OptionValue
            : PwshCompletionContextKind.OptionName, null);

        PwshCompletionContext Context(PwshCompletionContextKind kind, CommandElementAst? current)
            => new PwshCompletionContext(kind, cursorOffset, pending, current, scriptTarget);

        CommandElementAst? FindCurrentElement()
        {
            foreach (CommandElementAst candidate in command.CommandElements)
            {
                if (candidate.Extent.StartOffset <= cursorOffset && cursorOffset <= candidate.Extent.EndOffset)
                {
                    return candidate;
                }
            }

            return null;
        }
    }

    // Encoded payloads get no candidates, but consume only one argument; later startup options remain valid.
    private static bool IsEncodedPayload(PwshStartupOption option)
        => option.ArgumentKind == PwshStartupArgumentKind.EncodedCommand
            || option.ArgumentKind == PwshStartupArgumentKind.EncodedArguments;

    private static bool IsOptionPrefix(string key)
    {
        foreach (PwshStartupOption option in PwshStartupOptions.Options)
        {
            if (option.Name.StartsWith(key, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (string alias in option.Aliases)
            {
                if (alias.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Only literal ASTs are safe to interpret as startup syntax.</summary>
    internal static string? GetStaticText(CommandElementAst? element)
    {
        if (element is StringConstantExpressionAst literal)
        {
            return literal.Value;
        }

        if (element is CommandParameterAst parameter && parameter.Argument == null)
        {
            return parameter.Extent.Text;
        }

        return null;
    }
}
