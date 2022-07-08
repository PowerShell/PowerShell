/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.PowerShell
{
    public partial class PSConsoleReadLine
    {
        /// <summary>
        /// Move the cursor forward to the start of the next word.
        /// Word boundaries are defined by a configurable set of characters.
        /// </summary>
        public static void ViNextWord(ConsoleKeyInfo? key = null, object arg = null)
        {
            int numericArg;
            if (!TryGetArgAsInt(arg, out numericArg, 1))
            {
                return;
            }

            if (numericArg < 0)
            {
                ViBackwardWord(key, -numericArg);
                return;
            }

            while (numericArg-- > 0)
            {
                int i = _singleton.ViFindNextWordPoint(_singleton.Options.WordDelimiters);
                if (i >= _singleton._buffer.Length)
                {
                    i += ViEndOfLineFactor;
                }
                _singleton._current = i;
                _singleton.PlaceCursor();
            }
        }

        /// <summary>
        /// Move the cursor back to the start of the current word, or if between words,
        /// the start of the previous word.  Word boundaries are defined by a configurable
        /// set of characters.
        /// </summary>
        public static void ViBackwardWord(ConsoleKeyInfo? key = null, object arg = null)
        {
            int numericArg;
            if (!TryGetArgAsInt(arg, out numericArg, 1))
            {
                return;
            }

            if (numericArg < 0)
            {
                ViNextWord(key, -numericArg);
                return;
            }

            while (numericArg-- > 0)
            {
                int i = _singleton.ViFindPreviousWordPoint(_singleton.Options.WordDelimiters);
                _singleton._current = i;
                _singleton.PlaceCursor();
            }
        }

        /// <summary>
        /// Moves the cursor back to the beginning of the previous word, using only white space as delimiters.
        /// </summary>
        public static void ViBackwardGlob(ConsoleKeyInfo? key = null, object arg = null)
        {
            int numericArg;
            if (!TryGetArgAsInt(arg, out numericArg, 1))
            {
                return;
            }

            int i = _singleton._current;
            while (numericArg-- > 0)
            {
                i = _singleton.ViFindPreviousGlob(i - 1);
            }
            _singleton._current = i;
            _singleton.PlaceCursor();
        }

        /// <summary>
        /// Moves to the next word, using only white space as a word delimiter.
        /// </summary>
        private static void ViNextGlob(ConsoleKeyInfo? key = null, object arg = null)
        {
            int numericArg;
            if (!TryGetArgAsInt(arg, out numericArg, 1))
            {
                return;
            }

            int i = _singleton._current;
            while (numericArg-- > 0)
            {
                i = _singleton.ViFindNextGlob(i);
            }

            _singleton._current = Math.Min(i, _singleton._buffer.Length - 1);
            _singleton.PlaceCursor();
        }

        private static void ViEndOfGlob(ConsoleKeyInfo? key = null, object arg = null)
        {
            int numericArg;
            if (!TryGetArgAsInt(arg, out numericArg, 1))
            {
                return;
            }

            if (numericArg < 0)
            {
                ViEndOfPreviousGlob(key, -numericArg);
                return;
            }

            while (numericArg-- > 0)
            {
                int i = _singleton.ViFindEndOfGlob();
                _singleton._current = i;
                _singleton.PlaceCursor();
            }
        }

        private static void ViEndOfPreviousGlob(ConsoleKeyInfo? key = null, object arg = null)
        {
            int numericArg;
            if (!TryGetArgAsInt(arg, out numericArg, 1))
            {
                return;
            }

            if (numericArg < 0)
            {
                ViEndOfGlob(key, -numericArg);
                return;
            }

            while (numericArg-- > 0)
            {
                int i = _singleton.ViFindEndOfPreviousGlob();
                _singleton._current = i;
                _singleton.PlaceCursor();
            }
        }

        /// <summary>
        /// Returns 0 if the cursor is allowed to go past the last character in the line, -1 otherwise.
        /// </summary>
        /// <seealso cref="ForwardChar"/>
        private static int ViEndOfLineFactor
        {
            get
            {
                if (_singleton._dispatchTable == _viCmdKeyMap)
                {
                    return -1;
                }
                return 0;
            }
        }

        /// <summary>
        /// Move the cursor to the end of the input.
        /// </summary>
        public static void MoveToEndOfLine(ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton._current = Math.Max(0, _singleton._buffer.Length + ViEndOfLineFactor);
            _singleton.PlaceCursor();
        }

        /// <summary>
        /// Move the cursor forward to the end of the current word, or if between words,
        /// to the end of the next word.  Word boundaries are defined by a configurable
        /// set of characters.
        /// </summary>
        public static void NextWordEnd(ConsoleKeyInfo? key = null, object arg = null)
        {
            int qty = (arg is int) ? (int)arg : 1;
            for (; qty > 0 && _singleton._current < _singleton._buffer.Length - 1; qty--)
            {
                int i = _singleton.ViFindNextWordEnd(_singleton.Options.WordDelimiters);
                _singleton._current = i;
                _singleton.PlaceCursor();
            }
        }

        /// <summary>
        /// Move to the column indicated by arg.
        /// </summary>
        public static void GotoColumn(ConsoleKeyInfo? key = null, object arg = null)
        {
            int col = (arg is int) ? (int) arg : -1;
            if (col < 0 ) {
                Ding();
                return;
            }

            if (col < _singleton._buffer.Length + ViEndOfLineFactor)
            {
                _singleton._current = Math.Min(col, _singleton._buffer.Length) - 1;
            }
            else
            {
                _singleton._current = _singleton._buffer.Length + ViEndOfLineFactor;
                Ding();
            }
            _singleton.PlaceCursor();
        }

        /// <summary>
        /// Move the cursor to the first non-blank character in the line.
        /// </summary>
        public static void GotoFirstNonBlankOfLine(ConsoleKeyInfo? key = null, object arg = null)
        {
            for (int i = 0; i < _singleton._buffer.Length; i++)
            {
                if (!Char.IsWhiteSpace(_singleton._buffer[i]))
                {
                    _singleton._current = i;
                    _singleton.PlaceCursor();
                    return;
                }
            }
        }

        /// <summary>
        /// Similar to <see cref="GotoBrace"/>, but is character based instead of token based.
        /// </summary>
        public static void ViGotoBrace(ConsoleKeyInfo? key = null, object arg = null)
        {
            int i = _singleton.ViFindBrace(_singleton._current);
            if (i == _singleton._current)
            {
                Ding();
                return;
            }
            _singleton._current = i;
            _singleton.PlaceCursor();
        }

        private int ViFindBrace(int i)
        {
            switch (_buffer[i])
            {
                case '{':
                    return ViFindForward(i, '}', withoutPassing: '{');
                case '[':
                    return ViFindForward(i, ']', withoutPassing: '[');
                case '(':
                    return ViFindForward(i, ')', withoutPassing: '(');
                case '}':
                    return ViFindBackward(i, '{', withoutPassing: '}');
                case ']':
                    return ViFindBackward(i, '[', withoutPassing: ']');
                case ')':
                    return ViFindBackward(i, '(', withoutPassing: ')');
                default:
                    return i;
            }
        }

        private int ViFindBackward(int start, char target, char withoutPassing)
        {
            if (start == 0)
            {
                return start;
            }
            int i = start - 1;
            int withoutPassingCount = 0;
            while (i != 0 && !(_buffer[i] == target && withoutPassingCount == 0))
            {
                if (_buffer[i] == withoutPassing)
                {
                    withoutPassingCount++;
                }
                if (_buffer[i] == target)
                {
                    withoutPassingCount--;
                }
                i--;
            }
            if (_buffer[i] == target && withoutPassingCount == 0)
            {
                return i;
            }
            return start;
        }

        private int ViFindForward(int start, char target, char withoutPassing)
        {
            if (IsAtEndOfLine(start))
            {
                return start;
            }
            int i = start + 1;
            int withoutPassingCount = 0;
            while (!IsAtEndOfLine(i) && !(_buffer[i] == target && withoutPassingCount == 0))
            {
                if (_buffer[i] == withoutPassing)
                {
                    withoutPassingCount++;
                }
                if (_buffer[i] == target)
                {
                    withoutPassingCount--;
                }
                i++;
            }
            if (_buffer[i] == target && withoutPassingCount == 0)
            {
                return i;
            }
            return start;
        }
    }
}
