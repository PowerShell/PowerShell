/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Language;
#if !CORECLR
using System.Windows.Forms;
#endif

namespace Microsoft.PowerShell
{
    public partial class PSConsoleReadLine
    {
        // Yank/Kill state
        private List<string> _killRing;
        private int _killIndex;
        private int _killCommandCount;
        private int _yankCommandCount;
        private int _yankStartPoint;
        private int _yankLastArgCommandCount;
        class YankLastArgState
        {
            internal int argument;
            internal int historyIndex;
            internal int historyIncrement;
            internal int startPoint = -1;
        }
        private YankLastArgState _yankLastArgState;
        private int _visualSelectionCommandCount;

        /// <summary>
        /// Mark the current loction of the cursor for use in a subsequent editing command.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void SetMark(ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton._mark = _singleton._current;
        }

        /// <summary>
        /// The cursor is placed at the location of the mark and the mark is moved
        /// to the location of the cursor.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void ExchangePointAndMark(ConsoleKeyInfo? key = null, object arg = null)
        {
            var tmp = _singleton._mark;
            _singleton._mark = _singleton._current;
            _singleton._current = tmp;
            _singleton.PlaceCursor();
        }

        /// <summary>
        /// The contents of the kill ring are cleared.
        /// </summary>
        public static void ClearKillRing()
        {
            if (_singleton._killRing != null)
            {
                _singleton._killRing.Clear();
            }
            _singleton._killIndex = -1;    // So first add indexes 0.
        }

        private void Kill(int start, int length, bool prepend)
        {
            if (length > 0)
            {
                var killText = _buffer.ToString(start, length);
                SaveEditItem(EditItemDelete.Create(killText, start));
                _buffer.Remove(start, length);
                _current = start;
                Render();
                if (_killCommandCount > 0)
                {
                    if (prepend)
                    {
                        _killRing[_killIndex] = killText + _killRing[_killIndex];
                    }
                    else
                    {
                        _killRing[_killIndex] += killText;
                    }
                }
                else
                {
                    if (_killRing.Count < Options.MaximumKillRingCount)
                    {
                        _killRing.Add(killText);
                        _killIndex = _killRing.Count - 1;
                    }
                    else
                    {
                        _killIndex += 1;
                        if (_killIndex == _killRing.Count)
                        {
                            _killIndex = 0;
                        }
                        _killRing[_killIndex] = killText;
                    }
                }
            }
            _killCommandCount += 1;
        }

        /// <summary>
        /// Clear the input from the cursor to the end of the input.  The cleared text is placed
        /// in the kill ring.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void KillLine(ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton.Kill(_singleton._current, _singleton._buffer.Length - _singleton._current, false);
        }

        /// <summary>
        /// Clear the input from the start of the input to the cursor.  The cleared text is placed
        /// in the kill ring.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void BackwardKillLine(ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton.Kill(0, _singleton._current, true);
        }

        /// <summary>
        /// Clear the input from the cursor to the end of the current word.  If the cursor
        /// is between words, the input is cleared from the cursor to the end of the next word.
        /// The cleared text is placed in the kill ring.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void KillWord(ConsoleKeyInfo? key = null, object arg = null)
        {
            int i = _singleton.FindForwardWordPoint(_singleton.Options.WordDelimiters);
            _singleton.Kill(_singleton._current, i - _singleton._current, false);
        }

        /// <summary>
        /// Clear the input from the cursor to the end of the current word.  If the cursor
        /// is between words, the input is cleared from the cursor to the end of the next word.
        /// The cleared text is placed in the kill ring.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void ShellKillWord(ConsoleKeyInfo? key = null, object arg = null)
        {
            var token = _singleton.FindToken(_singleton._current, FindTokenMode.CurrentOrNext);
            var end = (token.Kind == TokenKind.EndOfInput)
                ? _singleton._buffer.Length 
                : token.Extent.EndOffset;
            _singleton.Kill(_singleton._current, end - _singleton._current, false);
        }

        /// <summary>
        /// Clear the input from the start of the current word to the cursor.  If the cursor
        /// is between words, the input is cleared from the start of the previous word to the
        /// cursor.  The cleared text is placed in the kill ring.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void BackwardKillWord(ConsoleKeyInfo? key = null, object arg = null)
        {
            int i = _singleton.FindBackwardWordPoint(_singleton.Options.WordDelimiters);
            _singleton.Kill(i, _singleton._current - i, true);
        }

        /// <summary>
        /// Clear the input from the start of the current word to the cursor.  If the cursor
        /// is between words, the input is cleared from the start of the previous word to the
        /// cursor.  The cleared text is placed in the kill ring.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void UnixWordRubout(ConsoleKeyInfo? key = null, object arg = null)
        {
            int i = _singleton.FindBackwardWordPoint("");
            _singleton.Kill(i, _singleton._current - i, true);
        }

        /// <summary>
        /// Clear the input from the start of the current word to the cursor.  If the cursor
        /// is between words, the input is cleared from the start of the previous word to the
        /// cursor.  The cleared text is placed in the kill ring.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void ShellBackwardKillWord(ConsoleKeyInfo? key = null, object arg = null)
        {
            var token = _singleton.FindToken(_singleton._current, FindTokenMode.Previous);
            var start = token == null 
                ? 0
                : token.Extent.StartOffset;
            _singleton.Kill(start, _singleton._current - start, true);
        }

        /// <summary>
        /// Kill the text between the cursor and the mark.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void KillRegion(ConsoleKeyInfo? key = null, object arg = null)
        {
            int start, length;
            _singleton.GetRegion(out start, out length);
            _singleton.Kill(start, length, true);
        }

        private void YankImpl()
        {
            if (_killRing.Count == 0)
                return;

            // Starting a yank session, yank the last thing killed and
            // remember where we started.
            _mark = _yankStartPoint = _current;
            Insert(_killRing[_killIndex]);
            
            _yankCommandCount += 1;
        }

        /// <summary>
        /// Add the most recently killed text to the input.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void Yank(ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton.YankImpl();
        }

        private void YankPopImpl()
        {
            if (_yankCommandCount == 0)
                return;

            _killIndex -= 1;
            if (_killIndex < 0)
            {
                _killIndex = _killRing.Count - 1;
            }
            var yankText = _killRing[_killIndex];
            Replace(_yankStartPoint, _current - _yankStartPoint, yankText);
            _yankCommandCount += 1;
        }

        /// <summary>
        /// If the previous operation was Yank or YankPop, replace the previously yanked
        /// text with the next killed text from the kill ring.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void YankPop(ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton.YankPopImpl();
        }

        void YankArgImpl(YankLastArgState yankLastArgState)
        {
            if (yankLastArgState.historyIndex < 0 || yankLastArgState.historyIndex >= _history.Count)
            {
                Ding();
                return;
            }

            Token[] tokens;
            ParseError[] errors;
            var buffer = _history[yankLastArgState.historyIndex];
            Parser.ParseInput(buffer._line, out tokens, out errors);

            int arg = (yankLastArgState.argument < 0)
                          ? tokens.Length + yankLastArgState.argument - 1
                          : yankLastArgState.argument;
            if (arg < 0 || arg >= tokens.Length)
            {
                Ding();
                return;
            }

            var argText = tokens[arg].Text;
            if (yankLastArgState.startPoint < 0)
            {
                yankLastArgState.startPoint = _current;
                Insert(argText);
            }
            else
            {
                Replace(yankLastArgState.startPoint, _current - yankLastArgState.startPoint, argText);
            }
        }

        /// <summary>
        /// Yank the first argument (after the command) from the previous history line.
        /// With an argument, yank the nth argument (starting from 0), if the argument
        /// is negative, start from the last argument.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void YankNthArg(ConsoleKeyInfo? key = null, object arg = null)
        {
            var yankLastArgState = new YankLastArgState
            {
                argument = (arg is int) ? (int)arg : 1,
                historyIndex = _singleton._currentHistoryIndex - 1,
            };
            _singleton.YankArgImpl(yankLastArgState);
        }

        /// <summary>
        /// Yank the last argument from the previous history line.  With an argument,
        /// the first time it is invoked, behaves just like YankNthArg.  If invoked
        /// multiple times, instead it iterates through history and arg sets the direction
        /// (negative reverses the direction.)
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void YankLastArg(ConsoleKeyInfo? key = null, object arg = null)
        {
            if (arg != null && !(arg is int))
            {
                Ding();
                return;
            }

            _singleton._yankLastArgCommandCount += 1;

            if (_singleton._yankLastArgCommandCount == 1)
            {
                _singleton._yankLastArgState = new YankLastArgState
                {
                    argument = (arg != null) ? (int)arg : -1,
                    historyIncrement = -1,
                    historyIndex = _singleton._currentHistoryIndex - 1
                };

                _singleton.YankArgImpl(_singleton._yankLastArgState);
                return;
            }

            var yankLastArgState = _singleton._yankLastArgState;

            if (arg != null)
            {
                if ((int)arg < 0)
                {
                    yankLastArgState.historyIncrement = -yankLastArgState.historyIncrement;
                }
            }

            yankLastArgState.historyIndex += yankLastArgState.historyIncrement;

            // Don't increment more than 1 out of range so it's quick to get back to being in range.
            if (yankLastArgState.historyIndex < 0)
            {
                Ding();
                yankLastArgState.historyIndex = 0;
            }
            else if (yankLastArgState.historyIndex >= _singleton._history.Count)
            {
                Ding();
                yankLastArgState.historyIndex = _singleton._history.Count - 1;
            }
            else
            {
                _singleton.YankArgImpl(yankLastArgState);
            }
        }

        private void VisualSelectionCommon(Action action)
        {
            if (_singleton._visualSelectionCommandCount == 0)
            {
                SetMark();
            }
            _singleton._visualSelectionCommandCount += 1;
            action();
            _singleton.Render();
        }

        /// <summary>
        /// Adjust the current selection to include the previous character
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void SelectBackwardChar(ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton.VisualSelectionCommon(() => BackwardChar(key, arg));
        }

        /// <summary>
        /// Adjust the current selection to include the next character
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void SelectForwardChar(ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton.VisualSelectionCommon(() => ForwardChar(key, arg));
        }

        /// <summary>
        /// Adjust the current selection to include the previous word
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void SelectBackwardWord(ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton.VisualSelectionCommon(() => BackwardWord(key, arg));
        }

        /// <summary>
        /// Adjust the current selection to include the next word
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void SelectNextWord(ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton.VisualSelectionCommon(() => NextWord(key, arg));
        }

        /// <summary>
        /// Adjust the current selection to include the next word using ForwardWord
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void SelectForwardWord(ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton.VisualSelectionCommon(() => ForwardWord(key, arg));
        }

        /// <summary>
        /// Adjust the current selection to include the next word using ShellForwardWord
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void SelectShellForwardWord(ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton.VisualSelectionCommon(() => ShellForwardWord(key, arg));
        }

        /// <summary>
        /// Adjust the current selection to include the next word using ShellNextWord
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void SelectShellNextWord(ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton.VisualSelectionCommon(() => ShellNextWord(key, arg));
        }

        /// <summary>
        /// Adjust the current selection to include the previous word using ShellBackwardWord
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void SelectShellBackwardWord(ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton.VisualSelectionCommon(() => ShellBackwardWord(key, arg));
        }

        /// <summary>
        /// Select the entire line
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void SelectAll(ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton._visualSelectionCommandCount += 1;
            _singleton._mark = 0;
            _singleton._current = _singleton._buffer.Length;
            _singleton.Render();
        }

        /// <summary>
        /// Adjust the current selection to include from the cursor to the end of the line
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void SelectLine(ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton.VisualSelectionCommon(() => EndOfLine(key, arg));
        }

        /// <summary>
        /// Adjust the current selection to include from the cursor to the start of the line
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void SelectBackwardsLine(ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton.VisualSelectionCommon(() => BeginningOfLine(key, arg));
        }

        /// <summary>
        /// Paste text from the system clipboard.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void Paste(ConsoleKeyInfo? key = null, object arg = null)
        {
#if !CORECLR
            string textToPaste = null;
            ExecuteOnSTAThread(() => {
                if (Clipboard.ContainsText())
                {
                    textToPaste = Clipboard.GetText();
                }
            });

            if (textToPaste != null)
            {
                textToPaste = textToPaste.Replace("\r", "");
                if (_singleton._visualSelectionCommandCount > 0)
                {
                    int start, length;
                    _singleton.GetRegion(out start, out length);
                    Replace(start, length, textToPaste);
                }
                else
                {
                    Insert(textToPaste);
                }
            }
#endif
        }

        /// <summary>
        /// Copy selected region to the system clipboard.  If no region is selected, copy the whole line.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void Copy(ConsoleKeyInfo? key = null, object arg = null)
        {
#if !CORECLR
            string textToSet;
            if (_singleton._visualSelectionCommandCount > 0)
            {
                int start, length;
                _singleton.GetRegion(out start, out length);
                textToSet = _singleton._buffer.ToString(start, length);
            }
            else
            {
                textToSet = _singleton._buffer.ToString(); 
            }
            if (!string.IsNullOrEmpty(textToSet))
            {
                ExecuteOnSTAThread(() => Clipboard.SetText(textToSet));
            }
#endif
        }

        /// <summary>
        /// If text is selected, copy to the clipboard, otherwise cancel the line.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void CopyOrCancelLine(ConsoleKeyInfo? key = null, object arg = null)
        {
            if (_singleton._visualSelectionCommandCount > 0)
            {
                Copy(key, arg);
            }
            else
            {
                CancelLine(key, arg);
            }
        }

        /// <summary>
        /// Delete selected region placing deleted text in the system clipboard.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void Cut(ConsoleKeyInfo? key = null, object arg = null)
        {
#if !CORECLR
            if (_singleton._visualSelectionCommandCount > 0)
            {
                int start, length;
                _singleton.GetRegion(out start, out length);
                ExecuteOnSTAThread(() => Clipboard.SetText(_singleton._buffer.ToString(start, length)));
                Delete(start, length);
            }
#endif
        }
    }
}
