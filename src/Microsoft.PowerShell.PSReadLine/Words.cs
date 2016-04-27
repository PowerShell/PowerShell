/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Collections.Generic;
using System.Management.Automation.Language;

namespace Microsoft.PowerShell
{
    public partial class PSConsoleReadLine
    {
        private enum FindTokenMode
        {
            CurrentOrNext,
            Next,
            Previous,
        }

        static bool OffsetWithinToken(int offset, Token token)
        {
           return offset < token.Extent.EndOffset && offset >= token.Extent.StartOffset;
        }

        private Token FindNestedToken(int offset, IList<Token> tokens, FindTokenMode mode)
        {
            Token token = null;
            bool foundNestedToken = false;
            int i;
            for (i = tokens.Count - 1; i >= 0; i--)
            {
                if (OffsetWithinToken(offset, tokens[i]))
                {
                    token = tokens[i];
                    var strToken = token as StringExpandableToken;
                    if (strToken != null && strToken.NestedTokens != null)
                    {
                        var nestedToken = FindNestedToken(offset, strToken.NestedTokens, mode);
                        if (nestedToken != null)
                        {
                            token = nestedToken;
                            foundNestedToken = true;
                        }
                    }
                    break;
                }
                if (offset >= tokens[i].Extent.EndOffset)
                {
                    break;
                }
            }

            switch (mode)
            {
            case FindTokenMode.CurrentOrNext:
                if (token == null && (i + 1) < tokens.Count)
                {
                    token = tokens[i + 1];
                }
                break;
            case FindTokenMode.Next:
                if (!foundNestedToken)
                {
                    // If there is no next token, return null (happens with nested
                    // tokens where there is no EOF/EOS token).
                    token = ((i + 1) < tokens.Count) ? tokens[i + 1] : null;
                }
                break;
            case FindTokenMode.Previous:
                if (token == null)
                {
                    if (i >= 0)
                    {
                        token = tokens[i];
                    }
                }
                else if (offset == token.Extent.StartOffset)
                {
                    token = i > 0 ? tokens[i - 1] : null;
                }
                break;
            }

            return token;
        }

        private Token FindToken(int current, FindTokenMode mode)
        {
            MaybeParseInput();
            return FindNestedToken(current, _tokens, mode);
        }

        private bool InWord(int index, string wordDelimiters)
        {
            char c = _buffer[index];
            return !char.IsWhiteSpace(c) && wordDelimiters.IndexOf(c) < 0;
        }

        /// <summary>
        /// Find the end of the current/next word as defined by wordDelimiters and whitespace.
        /// </summary>
        private int FindForwardWordPoint(string wordDelimiters)
        {
            int i = _current;
            if (i == _buffer.Length)
            {
                return i;
            }

            if (!InWord(i, wordDelimiters))
            {
                // Scan to end of current non-word region
                while (i < _buffer.Length)
                {
                    if (InWord(i, wordDelimiters))
                    {
                        break;
                    }
                    i += 1;
                }
            }
            while (i < _buffer.Length)
            {
                if (!InWord(i, wordDelimiters))
                {
                    break;
                }
                i += 1;
            }
            return i;
        }

        /// <summary>
        /// Find the start of the next word.
        /// </summary>
        private int FindNextWordPoint(string wordDelimiters)
        {
            int i = _singleton._current;
            if (i == _singleton._buffer.Length)
            {
                return i;
            }

            if (InWord(i, wordDelimiters))
            {
                // Scan to end of current word region
                while (i < _singleton._buffer.Length)
                {
                    if (!InWord(i, wordDelimiters))
                    {
                        break;
                    }
                    i += 1;
                }
            }

            while (i < _singleton._buffer.Length)
            {
                if (InWord(i, wordDelimiters))
                {
                    break;
                }
                i += 1;
            }
            return i;
        }

        /// <summary>
        /// Find the beginning of the previous word.
        /// </summary>
        private int FindBackwardWordPoint(string wordDelimiters)
        {
            int i = _current - 1;
            if (i < 0)
            {
                return 0;
            }

            if (!InWord(i, wordDelimiters))
            {
                // Scan backwards until we are at the end of the previous word.
                while (i > 0)
                {
                    if (InWord(i, wordDelimiters))
                    {
                        break;
                    }
                    i -= 1;
                }
            }
            while (i > 0)
            {
                if (!InWord(i, wordDelimiters))
                {
                    i += 1;
                    break;
                }
                i -= 1;
            }
            return i;
        }


    }
}
