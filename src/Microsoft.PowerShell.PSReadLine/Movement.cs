/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Language;
using Microsoft.PowerShell.Internal;

namespace Microsoft.PowerShell
{
    public partial class PSConsoleReadLine
    {
        private int _moveToLineCommandCount;
        private int _moveToLineDesiredColumn;

        /// <summary>
        /// If the input has multiple lines, move to the end of the current line,
        /// or if already at the end of the line, move to the end of the input.
        /// If the input has a single line, move to the end of the input.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void EndOfLine(ConsoleKeyInfo? key = null, object arg = null)
        {
            if (_singleton.LineIsMultiLine())
            {
                int i = _singleton._current;
                for (; i < _singleton._buffer.Length; i++)
                {
                    if (_singleton._buffer[i] == '\n')
                    {
                        break;
                    }
                }

                _singleton._current = (i == _singleton._current) ? _singleton._buffer.Length : i;
            }
            else
            {
                _singleton._current = _singleton._buffer.Length;
            }
            _singleton.PlaceCursor();
        }

        /// <summary>
        /// If the input has multiple lines, move to the start of the current line,
        /// or if already at the start of the line, move to the start of the input.
        /// If the input has a single line, move to the start of the input.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void BeginningOfLine(ConsoleKeyInfo? key = null, object arg = null)
        {
            if (_singleton.LineIsMultiLine())
            {
                int i = Math.Max(0, _singleton._current - 1);
                for (; i > 0; i--)
                {
                    if (_singleton._buffer[i] == '\n')
                    {
                        i += 1;
                        break;
                    }
                }

                _singleton._current = (i == _singleton._current) ? 0 : i;
            }
            else
            {
                _singleton._current = 0;
            }
            _singleton.PlaceCursor();
        }

        /// <summary>
        /// Move the cursor one character to the right.  This may move the cursor to the next
        /// line of multi-line input.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void ForwardChar(ConsoleKeyInfo? key = null, object arg = null)
        {
            int numericArg;
            if (TryGetArgAsInt(arg, out numericArg, 1))
            {
                SetCursorPosition(_singleton._current + numericArg);
            }
        }

        /// <summary>
        /// Move the cursor one character to the left.  This may move the cursor to the previous
        /// line of multi-line input.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void BackwardChar(ConsoleKeyInfo? key = null, object arg = null)
        {
            int numericArg;
            if (TryGetArgAsInt(arg, out numericArg, 1))
            {
                SetCursorPosition(_singleton._current - numericArg);
            }
        }

        private void MoveToLine(int numericArg)
        {
            const int endOfLine = int.MaxValue;

            _moveToLineCommandCount += 1;
            var coords = ConvertOffsetToCoordinates(_current);
            if (_moveToLineCommandCount == 1)
            {
                _moveToLineDesiredColumn =
                    (_current == _buffer.Length || _buffer[_current] == '\n')
                        ? endOfLine
                        : coords.X;
            }

            var topLine = _initialY + Options.ExtraPromptLineCount;

            var newY = coords.Y + numericArg;
            coords.Y = (short)Math.Max(newY, topLine);
            if (_moveToLineDesiredColumn != endOfLine)
            {
                coords.X = (short)_moveToLineDesiredColumn;
            }

            var newCurrent = ConvertLineAndColumnToOffset(coords);
            if (newCurrent != -1)
            {
                _current = newCurrent;
                if (_moveToLineDesiredColumn == endOfLine)
                {
                    while (_current < _buffer.Length && _buffer[_current] != '\n')
                    {
                        _current += 1;
                    }
                }
                PlaceCursor();
            }
        }

        /// <summary>
        /// Move the cursor to the previous line.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void PreviousLine(ConsoleKeyInfo? key = null, object arg = null)
        {
            int numericArg;
            if (TryGetArgAsInt(arg, out numericArg, 1))
            {
                _singleton.MoveToLine(-numericArg);
            }
        }

        /// <summary>
        /// Move the cursor to the next line.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void NextLine(ConsoleKeyInfo? key = null, object arg = null)
        {
            int numericArg;
            if (TryGetArgAsInt(arg, out numericArg, 1))
            {
                _singleton.MoveToLine(numericArg);
            }
        }

        /// <summary>
        /// Move the cursor forward to the start of the next word.
        /// Word boundaries are defined by a configurable set of characters.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void NextWord(ConsoleKeyInfo? key = null, object arg = null)
        {
            int numericArg;
            if (!TryGetArgAsInt(arg, out numericArg, 1))
            {
                return;
            }

            if (numericArg < 0)
            {
                BackwardWord(key, -numericArg);
                return;
            }

            while (numericArg-- > 0)
            {
                int i = _singleton.FindNextWordPoint(_singleton.Options.WordDelimiters);
                _singleton._current = i;
                _singleton.PlaceCursor();
            }
        }

        /// <summary>
        /// Move the cursor forward to the end of the current word, or if between words,
        /// to the end of the next word.  Word boundaries are defined by PowerShell tokens.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void ShellNextWord(ConsoleKeyInfo? key = null, object arg = null)
        {
            int numericArg;
            if (!TryGetArgAsInt(arg, out numericArg, 1))
            {
                return;
            }

            if (numericArg < 0)
            {
                ShellBackwardWord(key, -numericArg);
                return;
            }

            while (numericArg-- > 0)
            {
                var token = _singleton.FindToken(_singleton._current, FindTokenMode.Next);

                Debug.Assert(token != null, "We'll always find EOF");

                _singleton._current = token.Kind == TokenKind.EndOfInput
                                          ? _singleton._buffer.Length
                                          : token.Extent.StartOffset;
                _singleton.PlaceCursor();
            }
        }

        /// <summary>
        /// Move the cursor forward to the end of the current word, or if between words,
        /// to the end of the next word.  Word boundaries are defined by a configurable
        /// set of characters.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void ForwardWord(ConsoleKeyInfo? key = null, object arg = null)
        {
            int numericArg;
            if (!TryGetArgAsInt(arg, out numericArg, 1))
            {
                return;
            }

            if (numericArg < 0)
            {
                BackwardWord(key, -numericArg);
                return;
            }

            while (numericArg-- > 0)
            {
                int i = _singleton.FindForwardWordPoint(_singleton.Options.WordDelimiters);
                _singleton._current = i;
                _singleton.PlaceCursor();
            }
        }

        /// <summary>
        /// Move the cursor forward to the start of the next word.
        /// Word boundaries are defined by PowerShell tokens.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void ShellForwardWord(ConsoleKeyInfo? key = null, object arg = null)
        {
            int numericArg;
            if (!TryGetArgAsInt(arg, out numericArg, 1))
            {
                return;
            }

            if (numericArg < 0)
            {
                ShellBackwardWord(key, -numericArg);
                return;
            }

            while (numericArg-- > 0)
            {
                var token = _singleton.FindToken(_singleton._current, FindTokenMode.CurrentOrNext);

                Debug.Assert(token != null, "We'll always find EOF");

                _singleton._current = token.Kind == TokenKind.EndOfInput
                                          ? _singleton._buffer.Length
                                          : token.Extent.EndOffset;
                _singleton.PlaceCursor();
            }
        }

        private static bool CheckIsBound(Action<ConsoleKeyInfo?, object> action)
        {
            foreach (var entry in _singleton._dispatchTable)
            {
                if (entry.Value.Action == action)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Move the cursor back to the start of the current word, or if between words,
        /// the start of the previous word.  Word boundaries are defined by a configurable
        /// set of characters.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void BackwardWord(ConsoleKeyInfo? key = null, object arg = null)
        {
            int numericArg;
            if (!TryGetArgAsInt(arg, out numericArg, 1))
            {
                return;
            }

            if (numericArg < 0)
            {
                if (CheckIsBound(ForwardWord))
                {
                    ForwardWord(key, -numericArg);
                }
                else
                {
                    NextWord(key, -numericArg);
                }
                return;
            }

            while (numericArg-- > 0)
            {
                int i = _singleton.FindBackwardWordPoint(_singleton.Options.WordDelimiters);
                _singleton._current = i;
                _singleton.PlaceCursor();
            }
        }

        /// <summary>
        /// Move the cursor back to the start of the current word, or if between words,
        /// the start of the previous word.  Word boundaries are defined by PowerShell tokens.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void ShellBackwardWord(ConsoleKeyInfo? key = null, object arg = null)
        {
            int numericArg;
            if (!TryGetArgAsInt(arg, out numericArg, 1))
            {
                return;
            }

            if (numericArg < 0)
            {
                if (CheckIsBound(ShellForwardWord))
                {
                    ShellForwardWord(key, -numericArg);
                }
                else
                {
                    ShellNextWord(key, -numericArg);
                }
                return;
            }

            while (numericArg-- > 0)
            {
                var token = _singleton.FindToken(_singleton._current, FindTokenMode.Previous);

                _singleton._current = (token != null) ? token.Extent.StartOffset : 0;
                _singleton.PlaceCursor();
            }
        }

        /// <summary>
        /// Go to the matching brace, paren, or square bracket
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void GotoBrace(ConsoleKeyInfo? key = null, object arg = null)
        {
            if (_singleton._current >= _singleton._buffer.Length)
            {
                Ding();
                return;
            }

            _singleton.MaybeParseInput();

            Token token = null;
            var index = 0;
            for (; index < _singleton._tokens.Length; index++)
            {
                token = _singleton._tokens[index];
                if (token.Extent.StartOffset == _singleton._current)
                    break;
            }

            TokenKind toMatch;
            int direction;
            switch (token.Kind)
            {
            case TokenKind.LParen:   toMatch = TokenKind.RParen; direction = 1; break;
            case TokenKind.LCurly:   toMatch = TokenKind.RCurly; direction = 1; break;
            case TokenKind.LBracket: toMatch = TokenKind.RBracket; direction = 1; break;

            case TokenKind.RParen:   toMatch = TokenKind.LParen; direction = -1; break;
            case TokenKind.RCurly:   toMatch = TokenKind.LCurly; direction = -1; break;
            case TokenKind.RBracket: toMatch = TokenKind.LBracket; direction = -1; break;

            default:
                // Nothing to match (don't match inside strings/comments)
                Ding();
                return;
            }

            var matchCount = 0;
            var limit = (direction > 0) ? _singleton._tokens.Length - 1 : -1;
            for (; index != limit; index += direction)
            {
                var t = _singleton._tokens[index];
                if (t.Kind == token.Kind)
                {
                    matchCount++;
                }
                else if (t.Kind == toMatch)
                {
                    matchCount--;
                    if (matchCount == 0)
                    {
                        _singleton._current = t.Extent.StartOffset;
                        _singleton.PlaceCursor();
                        return;
                    }
                }
            }
            Ding();
        }

        /// <summary>
        /// Clear the screen and draw the current line at the top of the screen.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void ClearScreen(ConsoleKeyInfo? key = null, object arg = null)
        {
            var console = _singleton._console;
#if CORECLR
            console.Clear();
            _singleton._initialY = 0; 
            _singleton.Render();
#else
            if (_singleton._initialY + console.WindowHeight > console.BufferHeight)
            {
                var scrollCount = _singleton._initialY - console.WindowTop;
                console.ScrollBuffer(scrollCount);
                _singleton._initialY -= scrollCount;
                console.SetCursorPosition(console.CursorLeft, console.CursorTop - scrollCount);
            }
            else
            {
                console.SetWindowPosition(0, _singleton._initialY);
            }
#endif                
        }

        // Try to convert the arg to a char, return 0 for failure
        private static char TryGetArgAsChar(object arg)
        {
            if (arg is char)
            {
                return (char)arg;
            }

            var s = arg as string;
            if (s != null && s.Length == 1)
            {
                return s[0];
            }

            return (char)0;
        }

        /// <summary>
        /// Read a character and search forward for the next occurence of that character.
        /// If an argument is specified, search forward (or backward if negative) for the
        /// nth occurence.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void CharacterSearch(ConsoleKeyInfo? key = null, object arg = null)
        {
            int occurence = (arg is int) ? (int)arg : 1;
            if (occurence < 0)
            {
                CharacterSearchBackward(key, -occurence);
                return;
            }

            char toFind = TryGetArgAsChar(arg);
            if (toFind == (char)0)
            {
                // Should we prompt?
                toFind = ReadKey().KeyChar;
            }
            for (int i = _singleton._current + 1; i < _singleton._buffer.Length; i++)
            {
                if (_singleton._buffer[i] == toFind)
                {
                    occurence -= 1;
                    if (occurence == 0)
                    {
                        _singleton._current = i;
                        _singleton.PlaceCursor();
                        break;
                    }
                }
            }
            if (occurence > 0)
            {
                Ding();
            }
        }

        /// <summary>
        /// Read a character and search backward for the next occurence of that character.
        /// If an argument is specified, search backward (or forward if negative) for the
        /// nth occurence.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void CharacterSearchBackward(ConsoleKeyInfo? key = null, object arg = null)
        {
            int occurence = (arg is int) ? (int)arg : 1;
            if (occurence < 0)
            {
                CharacterSearch(key, -occurence);
                return;
            }

            char toFind = TryGetArgAsChar(arg);
            if (toFind == (char)0)
            {
                // Should we prompt?
                toFind = ReadKey().KeyChar;
            }
            for (int i = _singleton._current - 1; i >= 0; i--)
            {
                if (_singleton._buffer[i] == toFind)
                {
                    occurence -= 1;
                    if (occurence == 0)
                    {
                        _singleton._current = i;
                        _singleton.PlaceCursor();
                        return;
                    }
                }
            }
            Ding();
        }
    }
}
