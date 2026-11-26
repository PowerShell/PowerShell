// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Collections.Generic;
using System.Management.Automation.Language;

namespace System.Management.Automation;

/// <summary>Identifies which completion behavior owns the cursor in a native pwsh invocation.</summary>
internal enum PwshCompletionContextKind
{
    /// <summary>A startup option name, including an empty argument where another option can be supplied.</summary>
    OptionName,

    /// <summary>The value of a recognized startup option, such as XML after -OutputFormat.</summary>
    OptionValue,

    /// <summary>A script target supplied after -File or implicitly as a positional argument.</summary>
    ScriptPath,

    /// <summary>Arguments following a script target, eligible for delegation to that script's completer.</summary>
    ScriptArguments,

    /// <summary>The source following -Command or -CommandWithArgs, eligible for delegation to PowerShell language completion.</summary>
    Command,

    /// <summary>Input for which pwsh completion deliberately returns no candidates and suppresses fallback completion.</summary>
    /// <remarks>
    /// Used for encoded payloads, input after -Help or -Version,
    /// input after a standard-input sentinel, and input after an option value whose evaluated argument
    /// count cannot be determined syntactically. This does not necessarily mean the input is invalid.
    /// For encoded payloads, only the payload is opaque; later startup options can still be completed.
    /// </remarks>
    Opaque,
}

/// <summary>
/// Identifies the owner of the cursor without evaluating arguments or resolving paths.
/// All offsets and ASTs refer to the original input, including when it contains quotes.
/// </summary>
internal sealed class PwshCompletionContext
{
    private PwshCompletionContext(
        PwshCompletionContextKind kind,
        int cursorOffset,
        PwshStartupOption? option,
        CommandElementAst? currentElement,
        CommandElementAst? scriptTarget)
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

    internal HashSet<string> ProvidedOptions { get; private init; } = new(StringComparer.OrdinalIgnoreCase);

    internal int CommandOffset { get; private init; } = -1;

    internal string? WorkingDirectory { get; private init; }

    internal bool CanOffer(PwshStartupOption option)
        => !ProvidedOptions.Contains(option.Name)
            && !(option.Name == "STA" && ProvidedOptions.Contains("MTA"))
            && !(option.Name == "MTA" && ProvidedOptions.Contains("STA"))
            && !(option.Name == "Interactive" && ProvidedOptions.Contains("NonInteractive"))
            && !(option.Name == "NonInteractive" && ProvidedOptions.Contains("Interactive"))
            && !(option.Name == "Login" && ProvidedOptions.Count > 0);

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
        var scanner = new StartupArgumentScanner(command, cursorOffset);

        // The caller has already identified the executable. Only inspect its arguments.
        for (int i = 1; i < command.CommandElements.Count; i++)
        {
            CommandElementAst element = command.CommandElements[i];
            if (element.Extent.StartOffset > cursorOffset)
            {
                break;
            }

            // At the token's end the user is still completing it; whitespace advances the scan.
            bool current = cursorOffset <= element.Extent.EndOffset;
            PwshCompletionContext? context = scanner.CompleteRemainder(element, current);
            if (context != null)
            {
                return context;
            }

            if (current)
            {
                return scanner.CompleteCurrentElement(element);
            }

            context = scanner.Consume(element);
            if (context != null)
            {
                return context;
            }
        }

        return scanner.CompleteWhitespace();
    }

    /// <summary>Tracks consumed startup arguments and classifies the next completion position.</summary>
    private sealed class StartupArgumentScanner
    {
        private readonly CommandAst _command;
        private readonly int _cursorOffset;
        private readonly HashSet<string> _providedOptions = new(StringComparer.OrdinalIgnoreCase);
        private PwshStartupOption? _pending;
        private CommandElementAst? _scriptTarget;
        private bool _opaque;
        private int _commandOffset = -1;
        private string? _workingDirectory;

        internal StartupArgumentScanner(CommandAst command, int cursorOffset)
        {
            _command = command;
            _cursorOffset = cursorOffset;
        }

        // Once startup parsing ends, the established owner handles the entire remaining suffix.
        internal PwshCompletionContext? CompleteRemainder(CommandElementAst element, bool current)
        {
            if (_commandOffset >= 0)
            {
                return Context(PwshCompletionContextKind.Command, FindCurrentElement());
            }

            if (_opaque)
            {
                return Context(PwshCompletionContextKind.Opaque, current ? element : null);
            }

            if (_scriptTarget != null)
            {
                return Context(PwshCompletionContextKind.ScriptArguments, current ? element : FindCurrentElement());
            }

            return null;
        }

        internal PwshCompletionContext CompleteCurrentElement(CommandElementAst element)
        {
            if (_pending != null)
            {
                return Context(PendingValueKind(), element);
            }

            // Unknown switches can be implicit script paths. Keep filename completion available
            // for dash-prefixed filenames and absolute Unix paths once no option can match.
            string? text = GetStaticText(element);
            bool optionPrefix = text != null
                && PwshStartupOptions.TryGetSwitchKey(text, out string key)
                && IsOptionPrefix(key);
            return Context(optionPrefix ? PwshCompletionContextKind.OptionName
                : PwshCompletionContextKind.ScriptPath, element);
        }

        internal PwshCompletionContext? Consume(CommandElementAst element)
        {
            if (_pending != null)
            {
                return ConsumeOptionValue(element);
            }

            string? text = GetStaticText(element);
            if (text != null && PwshStartupOptions.TryGetSwitchKey(text, out string key))
            {
                ConsumeSwitch(element, text, key);
            }
            else
            {
                _scriptTarget = element;
            }

            return null;
        }

        private PwshCompletionContext? ConsumeOptionValue(CommandElementAst element)
        {
            if (_pending!.Name == "WorkingDirectory")
            {
                _workingDirectory = GetStaticText(element);
            }

            if (_pending!.ArgumentKind == PwshStartupArgumentKind.ScriptPath)
            {
                if (GetStaticText(element) == "-")
                {
                    // Standard input has no script metadata to delegate to.
                    return Context(PwshCompletionContextKind.Opaque, null);
                }

                _scriptTarget = element;
            }
            else
            {
                // An expression is one native argument syntactically, but its evaluated
                // argument count cannot be known by this scanner.
                _opaque = GetStaticText(element) == null;
            }

            _pending = null;
            return null;
        }

        private void ConsumeSwitch(CommandElementAst element, string text, string key)
        {
            _pending = PwshStartupOptions.Find(key);
            if (_pending == null)
            {
                // The host treats unrecognized switches as implicit file names, except for stdin.
                if (text == "-")
                {
                    _opaque = true;
                }
                else
                {
                    _scriptTarget = element;
                }

                return;
            }

            _providedOptions.Add(_pending.Name);
            if (_pending.ArgumentKind == PwshStartupArgumentKind.Command)
            {
                _commandOffset = element.Extent.EndOffset;
            }
            else if (_pending.Name == "Help"
                || (_pending.StopsParsing && _pending.ArgumentKind != PwshStartupArgumentKind.ScriptPath))
            {
                // Help aborts startup even though the host continues parsing its remaining arguments.
                _opaque = true;
            }
            else if (_pending.Consumption == PwshStartupArgumentConsumption.None)
            {
                _pending = null;
            }
        }

        internal PwshCompletionContext CompleteWhitespace()
        {
            // No element owns the cursor; the consumed arguments determine what comes next.
            PwshCompletionContextKind kind = _commandOffset >= 0 ? PwshCompletionContextKind.Command
                : _opaque ? PwshCompletionContextKind.Opaque
                : _scriptTarget != null ? PwshCompletionContextKind.ScriptArguments
                : _pending != null ? PendingValueKind()
                : PwshCompletionContextKind.OptionName;
            return Context(kind, null);
        }

        private PwshCompletionContextKind PendingValueKind()
            => _pending!.ArgumentKind == PwshStartupArgumentKind.ScriptPath ? PwshCompletionContextKind.ScriptPath
                : IsEncodedPayload(_pending) ? PwshCompletionContextKind.Opaque
                : PwshCompletionContextKind.OptionValue;

        private PwshCompletionContext Context(PwshCompletionContextKind kind, CommandElementAst? current)
            => new(kind, _cursorOffset, _pending, current, _scriptTarget)
            {
                ProvidedOptions = _providedOptions,
                CommandOffset = _commandOffset,
                WorkingDirectory = _workingDirectory,
            };

        private CommandElementAst? FindCurrentElement()
        {
            foreach (CommandElementAst candidate in _command.CommandElements)
            {
                if (candidate.Extent.StartOffset <= _cursorOffset && _cursorOffset <= candidate.Extent.EndOffset)
                {
                    return candidate;
                }
            }

            return null;
        }
    }

    // Encoded payloads get no candidates, but consume only one argument; later startup options remain valid.
    private static bool IsEncodedPayload(PwshStartupOption option)
        => option.ArgumentKind is PwshStartupArgumentKind.EncodedCommand
            or PwshStartupArgumentKind.EncodedArguments;

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
