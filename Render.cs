/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation.Language;
using Microsoft.PowerShell.Internal;

namespace Microsoft.PowerShell
{
    public partial class PSConsoleReadLine
    {
        private CHAR_INFO[] _consoleBuffer;
        private int _initialX;
        private int _initialY;
        private int _bufferWidth;
        private ConsoleColor _initialBackgroundColor;
        private ConsoleColor _initialForegroundColor;
        private CHAR_INFO _space;
        private int _current;
        private int _emphasisStart;
        private int _emphasisLength;

        private class SavedTokenState
        {
            internal Token[] Tokens { get; set; }
            internal int Index { get; set; }
            internal ConsoleColor BackgroundColor { get; set; }
            internal ConsoleColor ForegroundColor { get; set; }
        }

        private void MaybeParseInput()
        {
            if (_tokens == null)
            {
                ParseInput();
            }
        }

        private string ParseInput()
        {
            var text = _buffer.ToString();
            _ast = Parser.ParseInput(text, out _tokens, out _parseErrors);
            return text;
        }

        private void ClearStatusMessage(bool render)
        {
            _statusBuffer.Clear();
            _statusLinePrompt = null;
            _statusIsErrorMessage = false;
            if (render)
            {
                Render();
            }
        }

        private void Render()
        {
            // If there are a bunch of keys queued up, skip rendering if we've rendered
            // recently.
            if (_queuedKeys.Count > 10 && (_lastRenderTime.ElapsedMilliseconds < 50))
            {
                // We won't render, but most likely the tokens will be different, so make
                // sure we don't use old tokens.
                _tokens = null;
                _ast = null;
                return;
            }

            ReallyRender();
        }

        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults")]
        private void ReallyRender()
        {
            var text = ParseInput();

            int statusLineCount = GetStatusLineCount();
            int j = _initialX + (_bufferWidth * Options.ExtraPromptLineCount);
            var backgroundColor = _initialBackgroundColor;
            var foregroundColor = _initialForegroundColor;
            bool afterLastToken = false;
            int totalBytes = j;
            int bufferWidth = _console.BufferWidth;

            var tokenStack = new Stack<SavedTokenState>();
            tokenStack.Push(new SavedTokenState
            {
                Tokens = _tokens,
                Index = 0,
                BackgroundColor = _initialBackgroundColor,
                ForegroundColor = _initialForegroundColor
            });

            int bufferLineCount;

            try
            {
#if !CORECLR
                _console.StartRender();
#endif

                bufferLineCount = ConvertOffsetToCoordinates(text.Length).Y - _initialY + 1 + statusLineCount;
                if (_consoleBuffer.Length != bufferLineCount * bufferWidth)
                {
                    var newBuffer = new CHAR_INFO[bufferLineCount * bufferWidth];
                    Array.Copy(_consoleBuffer, newBuffer, _initialX + (Options.ExtraPromptLineCount * _bufferWidth));
                    if (_consoleBuffer.Length > bufferLineCount * bufferWidth)
                    {
                        int consoleBufferOffset = ConvertOffsetToConsoleBufferOffset(text.Length, _initialX + (Options.ExtraPromptLineCount * _bufferWidth));
                        // Need to erase the extra lines that we won't draw again
                        for (int i = consoleBufferOffset; i < _consoleBuffer.Length; i++)
                        {
                            _consoleBuffer[i] = _space;
                        }
                        _console.WriteBufferLines(_consoleBuffer, ref _initialY);
                    }
                    _consoleBuffer = newBuffer;
                }

                for (int i = 0; i < text.Length; i++)
                {
                    totalBytes = totalBytes % bufferWidth;
                    if (!afterLastToken)
                    {
                        // Figure out the color of the character - if it's in a token,
                        // use the tokens color otherwise use the initial color.
                        var state = tokenStack.Peek();
                        var token = state.Tokens[state.Index];

                        if (i == token.Extent.EndOffset)
                        {
                            if (token == state.Tokens[state.Tokens.Length - 1])
                            {
                                tokenStack.Pop();
                                if (tokenStack.Count == 0)
                                {
                                    afterLastToken = true;
                                    token = null;
                                    foregroundColor = _initialForegroundColor;
                                    backgroundColor = _initialBackgroundColor;
                                }
                                else
                                {
                                    state = tokenStack.Peek();
                                }
                            }

                            if (!afterLastToken)
                            {
                                foregroundColor = state.ForegroundColor;
                                backgroundColor = state.BackgroundColor;
                                token = state.Tokens[++state.Index];
                            }
                        }

                        if (!afterLastToken && i == token.Extent.StartOffset)
                        {
                            GetTokenColors(token, out foregroundColor, out backgroundColor);

                            var stringToken = token as StringExpandableToken;
                            if (stringToken != null)
                            {
                                // We might have nested tokens.
                                if (stringToken.NestedTokens != null && stringToken.NestedTokens.Any())
                                {
                                    var tokens = new Token[stringToken.NestedTokens.Count + 1];
                                    stringToken.NestedTokens.CopyTo(tokens, 0);
                                    // NestedTokens doesn't have an "EOS" token, so we use
                                    // the string literal token for that purpose.
                                    tokens[tokens.Length - 1] = stringToken;

                                    tokenStack.Push(new SavedTokenState
                                    {
                                        Tokens = tokens,
                                        Index = 0,
                                        BackgroundColor = backgroundColor,
                                        ForegroundColor = foregroundColor
                                    });

                                    if (i == tokens[0].Extent.StartOffset)
                                    {
                                        GetTokenColors(tokens[0], out foregroundColor, out backgroundColor);
                                    }
                                }
                            }
                        }
                    }

                    var charToRender = text[i];
                    if (charToRender == '\n')
                    {
                        while ((j % bufferWidth) != 0)
                        {
                            _consoleBuffer[j++] = _space;
                        }

                        for (int k = 0; k < Options.ContinuationPrompt.Length; k++, j++)
                        {
                            _consoleBuffer[j].UnicodeChar = Options.ContinuationPrompt[k];
                            _consoleBuffer[j].ForegroundColor = Options.ContinuationPromptForegroundColor;
                            _consoleBuffer[j].BackgroundColor = Options.ContinuationPromptBackgroundColor;
                        }
                    }
                    else
                    {
                        int size = LengthInBufferCells(charToRender);
                        totalBytes += size;

                        //if there is no enough space for the character at the edge, fill in spaces at the end and 
                        //put the character to next line.
                        int filling = totalBytes > bufferWidth ? (totalBytes - bufferWidth) % size : 0;
                        for (int f = 0; f < filling; f++)
                        {
                            _consoleBuffer[j++] = _space;
                            totalBytes++;
                        }

                        if (char.IsControl(charToRender))
                        {
                            _consoleBuffer[j].UnicodeChar = '^';
                            MaybeEmphasize(ref _consoleBuffer[j++], i, foregroundColor, backgroundColor);
                            _consoleBuffer[j].UnicodeChar = (char)('@' + charToRender);
                            MaybeEmphasize(ref _consoleBuffer[j++], i, foregroundColor, backgroundColor);

                        }
                        else if (size > 1)
                        {
                            _consoleBuffer[j].UnicodeChar = charToRender;
#if !CORECLR
                            _consoleBuffer[j].Attributes = (ushort)(_consoleBuffer[j].Attributes |
                                                           (uint)CHAR_INFO_Attributes.COMMON_LVB_LEADING_BYTE);
#endif
                            MaybeEmphasize(ref _consoleBuffer[j++], i, foregroundColor, backgroundColor);
                            _consoleBuffer[j].UnicodeChar = charToRender;
#if !CORECLR
                            _consoleBuffer[j].Attributes = (ushort)(_consoleBuffer[j].Attributes |
                                                           (uint)CHAR_INFO_Attributes.COMMON_LVB_TRAILING_BYTE);
#endif
                            MaybeEmphasize(ref _consoleBuffer[j++], i, foregroundColor, backgroundColor);
                        }
                        else
                        {
                            _consoleBuffer[j].UnicodeChar = charToRender;
                            MaybeEmphasize(ref _consoleBuffer[j++], i, foregroundColor, backgroundColor);
                        }
                    }
                }
            }
            finally
            {
#if !CORECLR
                _console.EndRender();
#endif
            }

            for (; j < (_consoleBuffer.Length - (statusLineCount * _bufferWidth)); j++)
            {
                _consoleBuffer[j] = _space;
            }

            if (_statusLinePrompt != null)
            {
                foregroundColor = _statusIsErrorMessage ? Options.ErrorForegroundColor : _console.ForegroundColor;
                backgroundColor = _statusIsErrorMessage ? Options.ErrorBackgroundColor : _console.BackgroundColor;

                for (int i = 0; i < _statusLinePrompt.Length; i++, j++)
                {
                    _consoleBuffer[j].UnicodeChar = _statusLinePrompt[i];
                    _consoleBuffer[j].ForegroundColor = foregroundColor;
                    _consoleBuffer[j].BackgroundColor = backgroundColor;
                }
                for (int i = 0; i < _statusBuffer.Length; i++, j++)
                {
                    _consoleBuffer[j].UnicodeChar = _statusBuffer[i];
                    _consoleBuffer[j].ForegroundColor = foregroundColor;
                    _consoleBuffer[j].BackgroundColor = backgroundColor;
                }

                for (; j < _consoleBuffer.Length; j++)
                {
                    _consoleBuffer[j] = _space;
                }
            }

            bool rendered = false;
            if (_parseErrors.Length > 0)
            {
                int promptChar = _initialX - 1 + (_bufferWidth * Options.ExtraPromptLineCount);

                while (promptChar >= 0)
                {
                    var c = (char)_consoleBuffer[promptChar].UnicodeChar;
                    if (char.IsWhiteSpace(c))
                    {
                        promptChar -= 1;
                        continue;
                    }

                    ConsoleColor prevColor = _consoleBuffer[promptChar].ForegroundColor;
                    _consoleBuffer[promptChar].ForegroundColor = ConsoleColor.Red;
                    _console.WriteBufferLines(_consoleBuffer, ref _initialY);
                    rendered = true;
                    _consoleBuffer[promptChar].ForegroundColor = prevColor;
                    break;
                }
            }

            if (!rendered)
            {
                _console.WriteBufferLines(_consoleBuffer, ref _initialY);
            }

            PlaceCursor();

            if ((_initialY + bufferLineCount) > (_console.WindowTop + _console.WindowHeight))
            {
#if !CORECLR               
                _console.WindowTop = _initialY + bufferLineCount - _console.WindowHeight;
#endif
            }

            _lastRenderTime.Restart();
        }

        private int LengthInBufferCells(char c)
        {
            int length = Char.IsControl(c) ? 1 : 0;
            if (c < 256)
            {
                return length + 1;
            }
            return _console.LengthInBufferCells(c);
        }

        private static void WriteBlankLines(int count, int top)
        {
            var console = _singleton._console;
            var blanks = new CHAR_INFO[count * console.BufferWidth];
            for (int i = 0; i < blanks.Length; i++)
            {
                blanks[i].BackgroundColor = console.BackgroundColor;
                blanks[i].ForegroundColor = console.ForegroundColor;
                blanks[i].UnicodeChar = ' ';
            }
            console.WriteBufferLines(blanks, ref top);
        }

        private static CHAR_INFO[] ReadBufferLines(int top, int count)
        {
            return _singleton._console.ReadBufferLines(top, count);
        }

        private void GetTokenColors(Token token, out ConsoleColor foregroundColor, out ConsoleColor backgroundColor)
        {
            switch (token.Kind)
            {
            case TokenKind.Comment:
                foregroundColor = _options.CommentForegroundColor;
                backgroundColor = _options.CommentBackgroundColor;
                return;

            case TokenKind.Parameter:
                foregroundColor = _options.ParameterForegroundColor;
                backgroundColor = _options.ParameterBackgroundColor;
                return;

            case TokenKind.Variable:
            case TokenKind.SplattedVariable:
                foregroundColor = _options.VariableForegroundColor;
                backgroundColor = _options.VariableBackgroundColor;
                return;

            case TokenKind.StringExpandable:
            case TokenKind.StringLiteral:
            case TokenKind.HereStringExpandable:
            case TokenKind.HereStringLiteral:
                foregroundColor = _options.StringForegroundColor;
                backgroundColor = _options.StringBackgroundColor;
                return;

            case TokenKind.Number:
                foregroundColor = _options.NumberForegroundColor;
                backgroundColor = _options.NumberBackgroundColor;
                return;
            }

            if ((token.TokenFlags & TokenFlags.CommandName) != 0)
            {
                foregroundColor = _options.CommandForegroundColor;
                backgroundColor = _options.CommandBackgroundColor;
                return;
            }

            if ((token.TokenFlags & TokenFlags.Keyword) != 0)
            {
                foregroundColor = _options.KeywordForegroundColor;
                backgroundColor = _options.KeywordBackgroundColor;
                return;
            }

            if ((token.TokenFlags & (TokenFlags.BinaryOperator | TokenFlags.UnaryOperator | TokenFlags.AssignmentOperator)) != 0)
            {
                foregroundColor = _options.OperatorForegroundColor;
                backgroundColor = _options.OperatorBackgroundColor;
                return;
            }

            if ((token.TokenFlags & TokenFlags.TypeName) != 0)
            {
                foregroundColor = _options.TypeForegroundColor;
                backgroundColor = _options.TypeBackgroundColor;
                return;
            }

            if ((token.TokenFlags & TokenFlags.MemberName) != 0)
            {
                foregroundColor = _options.MemberForegroundColor;
                backgroundColor = _options.MemberBackgroundColor;
                return;
            }

            foregroundColor = _options.DefaultTokenForegroundColor;
            backgroundColor = _options.DefaultTokenBackgroundColor;
        }

        private void GetRegion(out int start, out int length)
        {
            if (_mark < _current)
            {
                start = _mark;
                length = _current - start;
            }
            else
            {
                start = _current;
                length = _mark - start;
            }
        }

        private bool InRegion(int i)
        {
            int start, end;
            if (_mark > _current)
            {
                start = _current;
                end = _mark;
            }
            else
            {
                start = _mark;
                end = _current;
            }
            return i >= start && i < end;
        }

        private void MaybeEmphasize(ref CHAR_INFO charInfo, int i, ConsoleColor foregroundColor, ConsoleColor backgroundColor)
        {
            if (i >= _emphasisStart && i < (_emphasisStart + _emphasisLength))
            {
                backgroundColor = _options.EmphasisBackgroundColor;
                foregroundColor = _options.EmphasisForegroundColor;
            }
            else if (_visualSelectionCommandCount > 0 && InRegion(i))
            {
                // We can't quite emulate real console selection because it inverts
                // based on actual screen colors, our pallete is limited.  The choice
                // to invert only the lower 3 bits to change the color is somewhat
                // but looks best with the 2 default color schemes - starting PowerShell
                // from it's shortcut or from a cmd shortcut.
                foregroundColor = (ConsoleColor)((int)foregroundColor ^ 7);
                backgroundColor = (ConsoleColor)((int)backgroundColor ^ 7);
            }

            charInfo.ForegroundColor = foregroundColor;
            charInfo.BackgroundColor = backgroundColor;
        }

        private void PlaceCursor(int x, ref int y)
        {
            int statusLineCount = GetStatusLineCount();
            if ((y + statusLineCount) >= _console.BufferHeight)
            {
                _console.ScrollBuffer((y + statusLineCount) - _console.BufferHeight + 1);
                y = _console.BufferHeight - 1;
            }
            _console.SetCursorPosition(x, y);
        }

        private void PlaceCursor()
        {
            var coordinates = ConvertOffsetToCoordinates(_current);
            int y = coordinates.Y;
            PlaceCursor(coordinates.X, ref y);
        }

        private COORD ConvertOffsetToCoordinates(int offset)
        {
            int x = _initialX;
            int y = _initialY + Options.ExtraPromptLineCount;

            int bufferWidth = _console.BufferWidth;
            var continuationPromptLength = Options.ContinuationPrompt.Length;

            for (int i = 0; i < offset; i++)
            {
                char c = _buffer[i];
                if (c == '\n')
                {
                    y += 1;
                    x = continuationPromptLength;
                }
                else
                {
                    int size = LengthInBufferCells(c);
                    x += size;
                    // Wrap?  No prompt when wrapping
                    if (x >= bufferWidth)
                    {
                        int offsize = x - bufferWidth;
                        if (offsize % size == 0)
                        {
                            x -= bufferWidth;
                        }
                        else
                        {
                            x = size;
                        }
                        y += 1;
                    }
                }
            }

            //if the next character has bigger size than the remain space on this line,
            //the cursor goes to next line where the next character is.
            if (_buffer.Length > offset)
            {
                int size = LengthInBufferCells(_buffer[offset]);
                // next one is Wrapped to next line
                if (x + size > bufferWidth && (x + size - bufferWidth) % size != 0)
                {
                    x = 0;
                    y++;
                }
            }
            
            return new COORD {X = (short)x, Y = (short)y};
        }

        private int ConvertOffsetToConsoleBufferOffset(int offset, int startIndex)
        {
            int j = startIndex;
            for (int i = 0; i < offset; i++)
            {
                var c = _buffer[i];
                if (c == '\n')
                {
                    for (int k = 0; k < Options.ContinuationPrompt.Length; k++)
                    {
                        j++;
                    }
                }
                else if (LengthInBufferCells(c) > 1)
                {
                    j += 2;
                }
                else
                {
                    j++;
                }
            }
            return j;
        }

        private int ConvertLineAndColumnToOffset(COORD coord)
        {
            int offset;
            int x = _initialX;
            int y = _initialY + Options.ExtraPromptLineCount;

            int bufferWidth = _console.BufferWidth;
            var continuationPromptLength = Options.ContinuationPrompt.Length;
            for (offset = 0; offset < _buffer.Length; offset++)
            {
                // If we are on the correct line, return when we find
                // the correct column
                if (coord.Y == y && coord.X <= x)
                {
                    return offset;
                }
                char c = _buffer[offset];
                if (c == '\n')
                {
                    // If we are about to move off of the correct line,
                    // the line was shorter than the column we wanted so return.
                    if (coord.Y == y)
                    {
                        return offset;
                    }
                    y += 1;
                    x = continuationPromptLength;
                }
                else
                {
                    int size = LengthInBufferCells(c);
                    x += size;
                    // Wrap?  No prompt when wrapping
                    if (x >= bufferWidth)
                    {
                        int offsize = x - bufferWidth;
                        if (offsize % size == 0)
                        {
                            x -= bufferWidth;
                        }
                        else
                        {
                            x = size;
                        }
                        y += 1;
                    }
                }
            }

            // Return -1 if y is out of range, otherwise the last line was shorter
            // than we wanted, but still in range so just return the last offset.
            return (coord.Y == y) ? offset : -1;
        }

        private bool LineIsMultiLine()
        {
            for (int i = 0; i < _buffer.Length; i++)
            {
                if (_buffer[i] == '\n')
                    return true;
            }
            return false;
        }

        private int GetStatusLineCount()
        {
            if (_statusLinePrompt == null)
                return 0;

            return (_statusLinePrompt.Length + _statusBuffer.Length) / _console.BufferWidth + 1;
        }

#if !CORECLR
        [ExcludeFromCodeCoverage]
#endif
        void IPSConsoleReadLineMockableMethods.Ding()
        {
#if !CORECLR
            switch (Options.BellStyle)
            {
            case BellStyle.None:
                break;
            case BellStyle.Audible:
                Console.Beep(Options.DingTone, Options.DingDuration);
                break;
            case BellStyle.Visual:
                // TODO: flash prompt? command line?
                break;
            }
#endif
        }

        /// <summary>
        /// Notify the user based on their preference for notification.
        /// </summary>
        public static void Ding()
        {
            _singleton._mockableMethods.Ding();
        }

        private bool PromptYesOrNo(string s)
        {
            _statusLinePrompt = s;
            Render();

            var key = ReadKey();

            _statusLinePrompt = null;
            Render();
            return key.Key == ConsoleKey.Y;
        }

        #region Screen scrolling

        /// <summary>
        /// Scroll the display up one screen.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void ScrollDisplayUp(ConsoleKeyInfo? key = null, object arg = null)
        {
            int numericArg;
            TryGetArgAsInt(arg, out numericArg, +1);
            var console = _singleton._console;
            var newTop = console.WindowTop - (numericArg * console.WindowHeight);
            if (newTop < 0)
            {
                newTop = 0;
            }
            console.SetWindowPosition(0, newTop);
        }

        /// <summary>
        /// Scroll the display up one line.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void ScrollDisplayUpLine(ConsoleKeyInfo? key = null, object arg = null)
        {
            int numericArg;
            TryGetArgAsInt(arg, out numericArg, +1);
            var console = _singleton._console;
            var newTop = console.WindowTop - numericArg;
            if (newTop < 0)
            {
                newTop = 0;
            }
            console.SetWindowPosition(0, newTop);
        }

        /// <summary>
        /// Scroll the display down one screen.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void ScrollDisplayDown(ConsoleKeyInfo? key = null, object arg = null)
        {
            int numericArg;
            TryGetArgAsInt(arg, out numericArg, +1);
            var console = _singleton._console;
            var newTop = console.WindowTop + (numericArg * console.WindowHeight);
            if (newTop > (console.BufferHeight - console.WindowHeight))
            {
                newTop = (console.BufferHeight - console.WindowHeight);
            }
            console.SetWindowPosition(0, newTop);
        }

        /// <summary>
        /// Scroll the display down one line.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void ScrollDisplayDownLine(ConsoleKeyInfo? key = null, object arg = null)
        {
            int numericArg;
            TryGetArgAsInt(arg, out numericArg, +1);
            var console = _singleton._console;
            var newTop = console.WindowTop + numericArg;
            if (newTop > (console.BufferHeight - console.WindowHeight))
            {
                newTop = (console.BufferHeight - console.WindowHeight);
            }
            console.SetWindowPosition(0, newTop);
        }

        /// <summary>
        /// Scroll the display to the top.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void ScrollDisplayTop(ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton._console.SetWindowPosition(0, 0);
        }

        /// <summary>
        /// Scroll the display to the cursor.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void ScrollDisplayToCursor(ConsoleKeyInfo? key = null, object arg = null)
        {
            // Ideally, we'll put the last input line at the bottom of the window
            var coordinates = _singleton.ConvertOffsetToCoordinates(_singleton._buffer.Length);

            var console = _singleton._console;
            var newTop = coordinates.Y - console.WindowHeight + 1;

            // If the cursor is already visible, and we're on the first
            // page-worth of the buffer, then just scroll to the top (we can't
            // scroll to before the beginning of the buffer).
            //
            // Note that we don't want to just return, because the window may
            // have been scrolled way past the end of the content, so we really
            // do need to set the new window top to 0 to bring it back into
            // view.
            if (newTop < 0)
            {
                newTop = 0;
            }

            // But if the cursor won't be visible, make sure it is.
            if (newTop > console.CursorTop)
            {
                // Add 10 for some extra context instead of putting the
                // cursor on the bottom line.
                newTop = console.CursorTop - console.WindowHeight + 10;
            }

            // But we can't go past the end of the buffer.
            if (newTop > (console.BufferHeight - console.WindowHeight))
            {
                newTop = (console.BufferHeight - console.WindowHeight);
            }
            console.SetWindowPosition(0, newTop);
        }

        #endregion Screen scrolling
    }
}
