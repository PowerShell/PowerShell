// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Text;
using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell
{
    internal partial class ConsoleHostUserInterface : PSHostUserInterface, IHostUISupportsMultipleChoiceSelection
    {
        /// <summary>
        /// See base class.
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="message"></param>
        /// <param name="choices"></param>
        /// <param name="defaultChoice"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="choices"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="choices"/>.Count is 0.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="defaultChoice"/> is greater than
        ///     the length of <paramref name="choices"/>.
        /// </exception>
        /// <exception cref="PromptingException">
        ///  when prompt is canceled by, for example, Ctrl-c.
        /// </exception>
        public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
        {
            HandleThrowOnReadAndPrompt();

            if (choices == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(choices));
            }

            if (choices.Count == 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(choices),
                    ConsoleHostUserInterfaceStrings.EmptyChoicesErrorTemplate, "choices");
            }

            if ((defaultChoice < -1) || (defaultChoice >= choices.Count))
            {
                throw PSTraceSource.NewArgumentOutOfRangeException(nameof(defaultChoice), defaultChoice,
                    ConsoleHostUserInterfaceStrings.InvalidDefaultChoiceErrorTemplate, "defaultChoice", "choice");
            }

            // we lock here so that multiple threads won't interleave the various reads and writes here.

            lock (_instanceLock)
            {
                if (!string.IsNullOrEmpty(caption))
                {
                    // Should be a skin lookup

                    WriteLineToConsole();
                    WriteLineToConsole(PromptColor, RawUI.BackgroundColor, WrapToCurrentWindowWidth(caption));
                }

                if (!string.IsNullOrEmpty(message))
                {
                    WriteLineToConsole(WrapToCurrentWindowWidth(message));
                }

                int result = defaultChoice;

                string[,] hotkeysAndPlainLabels = null;
                HostUIHelperMethods.BuildHotkeysAndPlainLabels(choices, out hotkeysAndPlainLabels);

                Dictionary<int, bool> defaultChoiceKeys = new Dictionary<int, bool>();
                // add the default choice key only if it is valid. -1 is used to specify
                // no default.
                if (defaultChoice >= 0)
                {
                    defaultChoiceKeys.Add(defaultChoice, true);
                }

                while (true)
                {
                    WriteChoicePrompt(hotkeysAndPlainLabels, defaultChoiceKeys, false);

                    ReadLineResult rlResult;
                    string response = ReadChoiceResponse(out rlResult);

                    if (rlResult == ReadLineResult.endedOnBreak)
                    {
                        string msg = ConsoleHostUserInterfaceStrings.PromptCanceledError;
                        PromptingException e = new PromptingException(
                            msg, null, "PromptForChoiceCanceled", ErrorCategory.OperationStopped);
                        throw e;
                    }

                    if (response.Length == 0)
                    {
                        // they just hit enter.

                        if (defaultChoice >= 0)
                        {
                            // if there's a default, pick that one.

                            result = defaultChoice;
                            break;
                        }

                        continue;
                    }

                    // decide which choice they made.

                    if (response.Trim() == "?")
                    {
                        // show the help

                        ShowChoiceHelp(choices, hotkeysAndPlainLabels);
                        continue;
                    }

                    result = HostUIHelperMethods.DetermineChoicePicked(response.Trim(), choices, hotkeysAndPlainLabels);

                    if (result >= 0)
                    {
                        break;
                    }

                    // their input matched none of the choices, so prompt again
                }

                return result;
            }
        }

        /// <summary>
        /// Presents a dialog allowing the user to choose options from a set of options.
        /// </summary>
        /// <param name="caption">
        /// Caption to precede or title the prompt.  E.g. "Parameters for get-foo (instance 1 of 2)"
        /// </param>
        /// <param name="message">
        /// A message that describes what the choice is for.
        /// </param>
        /// <param name="choices">
        /// An Collection of ChoiceDescription objects that describe each choice.
        /// </param>
        /// <param name="defaultChoices">
        /// The index of the labels in the choices collection element to be presented to the user as
        /// the default choice(s).
        /// </param>
        /// <returns>
        /// The indices of the choice elements that corresponds to the options selected.
        /// </returns>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.PromptForChoice"/>
        public Collection<int> PromptForChoice(string caption,
            string message,
            Collection<ChoiceDescription> choices,
            IEnumerable<int> defaultChoices)
        {
            HandleThrowOnReadAndPrompt();

            if (choices == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(choices));
            }

            if (choices.Count == 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(choices),
                    ConsoleHostUserInterfaceStrings.EmptyChoicesErrorTemplate, "choices");
            }

            Dictionary<int, bool> defaultChoiceKeys = new Dictionary<int, bool>();

            if (defaultChoices != null)
            {
                foreach (int defaultChoice in defaultChoices)
                {
                    if ((defaultChoice < 0) || (defaultChoice >= choices.Count))
                    {
                        throw PSTraceSource.NewArgumentOutOfRangeException("defaultChoice", defaultChoice,
                            ConsoleHostUserInterfaceStrings.InvalidDefaultChoiceForMultipleSelection,
                            "defaultChoice",
                            "choices",
                            defaultChoice);
                    }

                    defaultChoiceKeys.TryAdd(defaultChoice, true);
                }
            }

            Collection<int> result = new Collection<int>();
            // we lock here so that multiple threads won't interleave the various reads and writes here.
            lock (_instanceLock)
            {
                // write caption on the console, if present.
                if (!string.IsNullOrEmpty(caption))
                {
                    // Should be a skin lookup
                    WriteLineToConsole();
                    WriteLineToConsole(PromptColor, RawUI.BackgroundColor, WrapToCurrentWindowWidth(caption));
                }
                // write message
                if (!string.IsNullOrEmpty(message))
                {
                    WriteLineToConsole(WrapToCurrentWindowWidth(message));
                }

                string[,] hotkeysAndPlainLabels = null;
                HostUIHelperMethods.BuildHotkeysAndPlainLabels(choices, out hotkeysAndPlainLabels);

                WriteChoicePrompt(hotkeysAndPlainLabels, defaultChoiceKeys, true);
                if (defaultChoiceKeys.Count > 0)
                {
                    WriteLineToConsole();
                }

                // used to display ChoiceMessage like Choice[0],Choice[1] etc
                int choicesSelected = 0;
                while (true)
                {
                    // write the current prompt
                    string choiceMsg = StringUtil.Format(ConsoleHostUserInterfaceStrings.ChoiceMessage, choicesSelected);
                    WriteToConsole(PromptColor, RawUI.BackgroundColor, WrapToCurrentWindowWidth(choiceMsg));

                    ReadLineResult rlResult;
                    string response = ReadChoiceResponse(out rlResult);

                    if (rlResult == ReadLineResult.endedOnBreak)
                    {
                        string msg = ConsoleHostUserInterfaceStrings.PromptCanceledError;
                        PromptingException e = new PromptingException(
                            msg, null, "PromptForChoiceCanceled", ErrorCategory.OperationStopped);
                        throw e;
                    }

                    // they just hit enter
                    if (response.Length == 0)
                    {
                        // this may happen when
                        // 1. user wants to go with the defaults
                        // 2. user selected some choices and wanted those
                        // choices to be picked.

                        // user did not pick up any choices..choose the default
                        if (result.Count == 0)
                        {
                            // if there's a default, pick that one.
                            foreach (int defaultChoice in defaultChoiceKeys.Keys)
                            {
                                result.Add(defaultChoice);
                            }
                        }
                        // allow for no choice selection.
                        break;
                    }

                    // decide which choice they made.
                    if (response.Trim() == "?")
                    {
                        // show the help
                        ShowChoiceHelp(choices, hotkeysAndPlainLabels);
                        continue;
                    }

                    int choicePicked = HostUIHelperMethods.DetermineChoicePicked(response.Trim(), choices, hotkeysAndPlainLabels);

                    if (choicePicked >= 0)
                    {
                        result.Add(choicePicked);
                        choicesSelected++;
                    }
                    // prompt for multiple choices
                }

                return result;
            }
        }

        private void WriteChoicePrompt(string[,] hotkeysAndPlainLabels,
            Dictionary<int, bool> defaultChoiceKeys,
            bool shouldEmulateForMultipleChoiceSelection)
        {
            System.Management.Automation.Diagnostics.Assert(defaultChoiceKeys != null, "defaultChoiceKeys cannot be null.");

            ConsoleColor fg = RawUI.ForegroundColor;
            ConsoleColor bg = RawUI.BackgroundColor;
            int lineLenMax = RawUI.WindowSize.Width - 1;
            int lineLen = 0;

            const string choiceTemplate = "[{0}] {1}  ";

            for (int i = 0; i < hotkeysAndPlainLabels.GetLength(1); ++i)
            {
                ConsoleColor cfg = PromptColor;
                if (defaultChoiceKeys.ContainsKey(i))
                {
                    // Should be a skin lookup
                    cfg = DefaultPromptColor;
                }

                string choice =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        choiceTemplate,
                        hotkeysAndPlainLabels[0, i],
                        hotkeysAndPlainLabels[1, i]);
                WriteChoiceHelper(choice, cfg, bg, ref lineLen, lineLenMax);
                if (shouldEmulateForMultipleChoiceSelection)
                {
                    WriteLineToConsole();
                }
            }

            WriteChoiceHelper(
                ConsoleHostUserInterfaceStrings.PromptForChoiceHelp,
                fg,
                bg,
                ref lineLen,
                lineLenMax);
            if (shouldEmulateForMultipleChoiceSelection)
            {
                WriteLineToConsole();
            }

            string defaultPrompt = string.Empty;
            if (defaultChoiceKeys.Count > 0)
            {
                string prepend = string.Empty;
                StringBuilder defaultChoicesBuilder = new StringBuilder();
                foreach (int defaultChoice in defaultChoiceKeys.Keys)
                {
                    string defaultStr = hotkeysAndPlainLabels[0, defaultChoice];
                    if (string.IsNullOrEmpty(defaultStr))
                    {
                        defaultStr = hotkeysAndPlainLabels[1, defaultChoice];
                    }

                    defaultChoicesBuilder.Append(string.Format(CultureInfo.InvariantCulture,
                        "{0}{1}", prepend, defaultStr));
                    prepend = ",";
                }

                string defaultChoices = defaultChoicesBuilder.ToString();

                if (defaultChoiceKeys.Count == 1)
                {
                    defaultPrompt = shouldEmulateForMultipleChoiceSelection ?
                        StringUtil.Format(ConsoleHostUserInterfaceStrings.DefaultChoiceForMultipleChoices, defaultChoices)
                        :
                        StringUtil.Format(ConsoleHostUserInterfaceStrings.DefaultChoicePrompt, defaultChoices);
                }
                else
                {
                    defaultPrompt = StringUtil.Format(ConsoleHostUserInterfaceStrings.DefaultChoicesForMultipleChoices,
                        defaultChoices);
                }
            }

            WriteChoiceHelper(defaultPrompt,
                fg,
                bg,
                ref lineLen,
                lineLenMax);
        }

        private void WriteChoiceHelper(string text, ConsoleColor fg, ConsoleColor bg, ref int lineLen, int lineLenMax)
        {
            int textLen = RawUI.LengthInBufferCells(text);
            bool trimEnd = false;

            if (lineLen + textLen > lineLenMax)
            {
                WriteLineToConsole();
                trimEnd = true;
                lineLen = textLen;
            }
            else
            {
                lineLen += textLen;
            }

            WriteToConsole(fg, bg, trimEnd ? text.TrimEnd(null) : text);
        }

        private string ReadChoiceResponse(out ReadLineResult result)
        {
            result = ReadLineResult.endedOnEnter;
            return InternalTestHooks.ForcePromptForChoiceDefaultOption
                   ? string.Empty
                   : ReadLine(
                       endOnTab: false,
                       initialContent: string.Empty,
                       result: out result,
                       calledFromPipeline: true,
                       transcribeResult: true);
        }

        private void ShowChoiceHelp(Collection<ChoiceDescription> choices, string[,] hotkeysAndPlainLabels)
        {
            Dbg.Assert(choices != null, "choices: expected a value");
            Dbg.Assert(hotkeysAndPlainLabels != null, "hotkeysAndPlainLabels: expected a value");

            for (int i = 0; i < choices.Count; ++i)
            {
                string s;

                // If there's no hotkey, use the label as the help

                if (hotkeysAndPlainLabels[0, i].Length > 0)
                {
                    s = hotkeysAndPlainLabels[0, i];
                }
                else
                {
                    s = hotkeysAndPlainLabels[1, i];
                }

                WriteLineToConsole(
                    WrapToCurrentWindowWidth(
                        string.Format(CultureInfo.InvariantCulture, $"{s} - {choices[i].HelpMessage}")));
            }
        }

        /// <summary>
        /// Guarantee a contrasting color for the prompt...
        /// </summary>
        private ConsoleColor PromptColor
        {
            get
            {
                switch (RawUI.BackgroundColor)
                {
                    case ConsoleColor.White: return ConsoleColor.Black;
                    case ConsoleColor.Cyan: return ConsoleColor.Black;
                    case ConsoleColor.DarkYellow: return ConsoleColor.Black;
                    case ConsoleColor.Yellow: return ConsoleColor.Black;
                    case ConsoleColor.Gray: return ConsoleColor.Black;
                    case ConsoleColor.Green: return ConsoleColor.Black;
                    default: return ConsoleColor.White;
                }
            }
        }
        /// <summary>
        /// Guarantee a contrasting color for the default prompt that is slightly
        /// different from the other prompt elements.
        /// </summary>
        private ConsoleColor DefaultPromptColor
        {
            get
            {
                if (PromptColor == ConsoleColor.White)
                    return ConsoleColor.Yellow;
                else
                    return ConsoleColor.Blue;
            }
        }
    }
}   // namespace
