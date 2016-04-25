/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Microsoft.PowerShell
{
    public partial class PSConsoleReadLine
    {
        private static void ViReplaceUntilEsc(ConsoleKeyInfo? key, object arg)
        {
            if (_singleton._current >= _singleton._buffer.Length)
            {
                Ding();
                return;
            }

            int startingCursor = _singleton._current;
            int maxDeleteLength = _singleton._buffer.Length - _singleton._current;
            StringBuilder deletedStr = new StringBuilder();

            ConsoleKeyInfo nextKey = ReadKey();
            while (nextKey.Key != ConsoleKey.Escape && nextKey.Key != ConsoleKey.Enter)
            {
                if (nextKey.Key != ConsoleKey.Backspace && nextKey.KeyChar != '\u0000')
                {
                    if (_singleton._current >= _singleton._buffer.Length)
                    {
                        _singleton._buffer.Append(nextKey.KeyChar);
                    }
                    else
                    {
                        deletedStr.Append(_singleton._buffer[_singleton._current]);
                        _singleton._buffer[_singleton._current] = nextKey.KeyChar;
                    }
                    _singleton._current++;
                    _singleton.Render();
                }
                if (nextKey.Key == ConsoleKey.Backspace)
                {
                    if (_singleton._current == startingCursor)
                    {
                        Ding();
                    }
                    else
                    {
                        if (deletedStr.Length == _singleton._current - startingCursor)
                        {
                            _singleton._buffer[_singleton._current - 1] = deletedStr[deletedStr.Length - 1];
                            deletedStr.Remove(deletedStr.Length - 1, 1);
                        }
                        else
                        {
                            _singleton._buffer.Remove(_singleton._current - 1, 1);
                        }
                        _singleton._current--;
                        _singleton.Render();
                    }
                }
                nextKey = ReadKey();
            }

            if (_singleton._current > startingCursor)
            {
                _singleton.StartEditGroup();
                string insStr = _singleton._buffer.ToString(startingCursor, _singleton._current - startingCursor);
                _singleton.SaveEditItem(EditItemDelete.Create(deletedStr.ToString(), startingCursor));
                _singleton.SaveEditItem(EditItemInsertString.Create(insStr, startingCursor));
                _singleton.EndEditGroup();
            }

            if (nextKey.Key == ConsoleKey.Enter)
            {
                ViAcceptLine(nextKey);
            }
        }

        private static void ViReplaceBrace(ConsoleKeyInfo? key, object arg)
        {
            _singleton._groupUndoHelper.StartGroup(ViReplaceBrace, arg);
            ViDeleteBrace(key, arg);
            ViInsertMode(key, arg);
        }

        private static void ViBackwardReplaceLineToFirstChar(ConsoleKeyInfo? key, object arg)
        {
            _singleton._groupUndoHelper.StartGroup(ViBackwardReplaceLineToFirstChar, arg);
            DeleteLineToFirstChar(key, arg);
            ViInsertMode(key, arg);
        }

        private static void ViBackwardReplaceLine(ConsoleKeyInfo? key, object arg)
        {
            _singleton._groupUndoHelper.StartGroup(ViBackwardReplaceLine, arg);
            BackwardDeleteLine(key, arg);
            ViInsertMode(key, arg);
        }

        private static void BackwardReplaceChar(ConsoleKeyInfo? key, object arg)
        {
            _singleton._groupUndoHelper.StartGroup(BackwardReplaceChar, arg);
            BackwardDeleteChar(key, arg);
            ViInsertMode(key, arg);
        }

        private static void ViBackwardReplaceWord(ConsoleKeyInfo? key, object arg)
        {
            _singleton._groupUndoHelper.StartGroup(ViBackwardReplaceWord, arg);
            BackwardDeleteWord(key, arg);
            ViInsertMode(key, arg);
        }

        private static void ViBackwardReplaceGlob(ConsoleKeyInfo? key, object arg)
        {
            _singleton._groupUndoHelper.StartGroup(ViBackwardReplaceGlob, arg);
            ViBackwardDeleteGlob(key, arg);
            ViInsertMode(key, arg);
        }

        private static void ViReplaceToEnd(ConsoleKeyInfo? key, object arg)
        {
            _singleton._groupUndoHelper.StartGroup(ViReplaceToEnd, arg);
            DeleteToEnd(key, arg);
            _singleton._current = Math.Min(_singleton._buffer.Length, _singleton._current + 1);
            _singleton.PlaceCursor();
            ViInsertMode(key, arg);
        }

        private static void ViReplaceLine(ConsoleKeyInfo? key, object arg)
        {
            _singleton._groupUndoHelper.StartGroup(ViReplaceLine, arg);
            DeleteLine(key, arg);
            ViInsertMode(key, arg);
        }

        private static void ViReplaceWord(ConsoleKeyInfo? key, object arg)
        {
            _singleton._groupUndoHelper.StartGroup(ViReplaceWord, arg);
            _singleton._lastWordDelimiter = char.MinValue;
            _singleton._shouldAppend = false;
            DeleteWord(key, arg);
            if (_singleton._current < _singleton._buffer.Length - 1)
            {
                if (char.IsWhiteSpace(_singleton._lastWordDelimiter))
                {
                    Insert(_singleton._lastWordDelimiter);
                    _singleton._current--;
                }
                _singleton._lastWordDelimiter = char.MinValue;
                _singleton.PlaceCursor();
            }
            if (_singleton._current == _singleton._buffer.Length - 1 
                && !_singleton.IsDelimiter(_singleton._lastWordDelimiter, _singleton.Options.WordDelimiters)
                && _singleton._shouldAppend)
            {
                ViInsertWithAppend(key, arg);
            }
            else
            {
                ViInsertMode(key, arg);
            }
        }

        private static void ViReplaceGlob(ConsoleKeyInfo? key, object arg)
        {
            _singleton._groupUndoHelper.StartGroup(ViReplaceGlob, arg);
            ViDeleteGlob(key, arg);
            if (_singleton._current < _singleton._buffer.Length - 1)
            {
                Insert(' ');
                _singleton._current--;
                _singleton.PlaceCursor();
            }
            if (_singleton._current == _singleton._buffer.Length - 1)
            {
                ViInsertWithAppend(key, arg);
            }
            else
            {
                ViInsertMode(key, arg);
            }
        }

        private static void ViReplaceEndOfWord(ConsoleKeyInfo? key, object arg)
        {
            _singleton._groupUndoHelper.StartGroup(ViReplaceEndOfWord, arg);
            DeleteEndOfWord(key, arg);
            if (_singleton._current == _singleton._buffer.Length - 1)
            {
                ViInsertWithAppend(key, arg);
            }
            else
            {
                ViInsertMode(key, arg);
            }
        }

        private static void ViReplaceEndOfGlob(ConsoleKeyInfo? key, object arg)
        {
            _singleton._groupUndoHelper.StartGroup(ViReplaceEndOfGlob, arg);
            ViDeleteEndOfGlob(key, arg);
            if (_singleton._current == _singleton._buffer.Length - 1)
            {
                ViInsertWithAppend(key, arg);
            }
            else
            {
                ViInsertMode(key, arg);
            }
        }

        private static void ReplaceChar(ConsoleKeyInfo? key, object arg)
        {
            _singleton._groupUndoHelper.StartGroup(ReplaceChar, arg);
            DeleteChar(key, arg);
            ViInsertMode(key, arg);
        }

        /// <summary>
        /// Replaces the current character with the next character typed.
        /// </summary>
        private static void ReplaceCharInPlace(ConsoleKeyInfo? key, object arg)
        {
            ConsoleKeyInfo nextKey = ReadKey();
            if (_singleton._buffer.Length > 0 && nextKey.KeyChar > 0 && nextKey.Key != ConsoleKey.Escape && nextKey.Key != ConsoleKey.Enter)
            {
                _singleton.StartEditGroup();
                _singleton.SaveEditItem(EditItemDelete.Create(_singleton._buffer[_singleton._current].ToString(), _singleton._current));
                _singleton.SaveEditItem(EditItemInsertString.Create(nextKey.KeyChar.ToString(), _singleton._current));
                _singleton.EndEditGroup();

                _singleton._buffer[_singleton._current] = nextKey.KeyChar;
                _singleton.Render();
            }
            else
            {
                Ding();
            }
        }

        /// <summary>
        /// Deletes until given character
        /// </summary>
        /// <param name="key"></param>
        /// <param name="arg"></param>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        private static void ViReplaceToChar(ConsoleKeyInfo? key = null, object arg = null)
        {
            var keyChar = ReadKey().KeyChar;
            ViReplaceToChar(keyChar, key, arg);
        }

        private static void ViReplaceToChar(char keyChar, ConsoleKeyInfo? key = null, object arg = null)
        {
            bool shouldAppend = _singleton._current > 0;

            _singleton._groupUndoHelper.StartGroup(ReplaceChar, arg);
            ViCharacterSearcher.Set(keyChar, isBackward: false, isBackoff: false);
            if (ViCharacterSearcher.SearchDelete(keyChar, arg, backoff: false, instigator: (_key, _arg) => ViReplaceToChar(keyChar, _key, _arg)))
            {
                if (shouldAppend)
                {
                    ViInsertWithAppend(key, arg);
                }
                else
                {
                    ViInsertMode(key, arg);
                }
            }
        }

        /// <summary>
        /// Replaces until given character
        /// </summary>
        /// <param name="key"></param>
        /// <param name="arg"></param>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        private static void ViReplaceToCharBackward(ConsoleKeyInfo? key = null, object arg = null)
        {
            var keyChar = ReadKey().KeyChar;
            ViReplaceToCharBack(keyChar, key, arg);
        }

        private static void ViReplaceToCharBack(char keyChar, ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton._groupUndoHelper.StartGroup(ReplaceChar, arg);
            if (ViCharacterSearcher.SearchBackwardDelete(keyChar, arg, backoff: false, instigator: (_key, _arg) => ViReplaceToCharBack(keyChar, _key, _arg)))
            {
                ViInsertMode(key, arg);
            }
        }

        /// <summary>
        /// Replaces until given character
        /// </summary>
        /// <param name="key"></param>
        /// <param name="arg"></param>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        private static void ViReplaceToBeforeChar(ConsoleKeyInfo? key = null, object arg = null)
        {
            var keyChar = ReadKey().KeyChar;
            ViReplaceToBeforeChar(keyChar, key, arg);
        }

        private static void ViReplaceToBeforeChar(char keyChar, ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton._groupUndoHelper.StartGroup(ReplaceChar, arg);
            ViCharacterSearcher.Set(keyChar, isBackward: false, isBackoff: true);
            if (ViCharacterSearcher.SearchDelete(keyChar, arg, backoff: true, instigator: (_key, _arg) => ViReplaceToBeforeChar(keyChar, _key, _arg)))
            {
                ViInsertMode(key, arg);
            }
        }

        /// <summary>
        /// Replaces until given character
        /// </summary>
        /// <param name="key"></param>
        /// <param name="arg"></param>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        private static void ViReplaceToBeforeCharBackward(ConsoleKeyInfo? key = null, object arg = null)
        {
            var keyChar = ReadKey().KeyChar;
            ViReplaceToBeforeCharBack(keyChar, key, arg);
        }

        private static void ViReplaceToBeforeCharBack(char keyChar, ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton._groupUndoHelper.StartGroup(ReplaceChar, arg);
            if (ViCharacterSearcher.SearchBackwardDelete(keyChar, arg, backoff: true, instigator: (_key, _arg) => ViReplaceToBeforeCharBack(keyChar, _key, _arg)))
            {
                ViInsertMode(key, arg);
            }
        }


    }
}
