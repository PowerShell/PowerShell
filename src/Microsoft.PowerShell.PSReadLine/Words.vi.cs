/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Collections.Generic;
using System.Management.Automation.Language;

namespace Microsoft.PowerShell
{
    public partial class PSConsoleReadLine
    {
        private char _lastWordDelimiter = char.MinValue;
        private bool _shouldAppend = false;

        /// <summary>
        /// Returns the position of the beginning of the next word as delimited by white space and delimiters.
        /// </summary>
        private int ViFindNextWordPoint(string wordDelimiters)
        {
            return ViFindNextWordPoint(_current, wordDelimiters);
        }

        /// <summary>
        /// Returns the position of the beginning of the next word as delimited by white space and delimiters.
        /// </summary>
        private int ViFindNextWordPoint(int i, string wordDelimiters)
        {
            if (IsAtEndOfLine(i))
            {
                return i;
            }
            if (InWord(i, wordDelimiters))
            {
                return ViFindNextWordFromWord(i, wordDelimiters);
            }
            if (IsDelimiter(i, wordDelimiters))
            {
                return ViFindNextWordFromDelimiter(i, wordDelimiters);
            }
            return ViFindNextWordFromWhiteSpace(i, wordDelimiters);
        }

        private int ViFindNextWordFromWhiteSpace(int i, string wordDelimiters)
        {
            while (!IsAtEndOfLine(i) && IsWhiteSpace(i))
            {
                i++;
            }
            return i;
        }

        private int ViFindNextWordFromDelimiter(int i, string wordDelimiters)
        {
            while (!IsAtEndOfLine(i) && IsDelimiter(i, wordDelimiters))
            {
                i++;
            }
            if (IsAtEndOfLine(i))
            {
                if (IsDelimiter(i, wordDelimiters))
                {
                    _shouldAppend = true;
                    return i + 1;
                }
                return i;
            }
            while (!IsAtEndOfLine(i) && IsWhiteSpace(i))
            {
                i++;
            }
            return i;
        }

        private bool IsAtEndOfLine(int i)
        {
            return i >= (_buffer.Length - 1);
        }

        private bool IsPastEndOfLine(int i)
        {
            return i > (_buffer.Length - 1);
        }

        private int ViFindNextWordFromWord(int i, string wordDelimiters)
        {
            while (!IsAtEndOfLine(i) && InWord(i, wordDelimiters))
            {
                i++;
            }
            if (IsAtEndOfLine(i) && InWord(i, wordDelimiters))
            {
                _shouldAppend = true;
                return i + 1;
            }
            if (IsDelimiter(i, wordDelimiters))
            {
                _lastWordDelimiter = _buffer[i];
                return i;
            }
            while (!IsAtEndOfLine(i) && IsWhiteSpace(i))
            {
                i++;
            }
            if (IsAtEndOfLine(i) && !InWord(i, wordDelimiters))
            {
                return i + 1;
            }
            _lastWordDelimiter = _buffer[i-1];
            return i;
        }

        /// <summary>
        /// Returns true of the character at the given position is white space.
        /// </summary>
        private bool IsWhiteSpace(int i)
        {
            return char.IsWhiteSpace(_buffer[i]);
        }

        /// <summary>
        /// Returns the beginning of the current/next word as defined by wordDelimiters and whitespace.
        /// </summary>
        private int ViFindPreviousWordPoint(string wordDelimiters)
        {
            return ViFindPreviousWordPoint(_current, wordDelimiters);
        }

        /// <summary>
        /// Returns the beginning of the current/next word as defined by wordDelimiters and whitespace.
        /// </summary>
        /// <param name="i">Current cursor location.</param>
        /// <param name="wordDelimiters">Characters used to deliminate words.</param>
        /// <returns>Location of the beginning of the previous word.</returns>
        private int ViFindPreviousWordPoint(int i, string wordDelimiters)
        {
            if (i == 0)
            {
                return i;
            }

            if (IsWhiteSpace(i)) 
            {
                return FindPreviousWordFromWhiteSpace(i, wordDelimiters);
            }
            else if (InWord(i, wordDelimiters))
            {
                return FindPreviousWordFromWord(i, wordDelimiters);
            }
            return FindPreviousWordFromDelimiter(i, wordDelimiters);
        }

        /// <summary>
        /// Knowing that you're starting with a word, find the previous start of the next word.
        /// </summary>
        private int FindPreviousWordFromWord(int i, string wordDelimiters)
        {
            i--;
            if (InWord(i, wordDelimiters))
            {
                while (i > 0 && InWord(i, wordDelimiters))
                {
                    i--;
                }
                if (i == 0 && InWord(i, wordDelimiters))
                {
                    return i;
                }
                return i + 1;
            }
            if (IsWhiteSpace(i))
            {
                while (i > 0 && IsWhiteSpace(i))
                {
                    i--;
                }
                if (i == 0)
                {
                    return i;
                }
                if (InWord(i, wordDelimiters) && InWord(i-1, wordDelimiters))
                {
                    return FindPreviousWordFromWord(i, wordDelimiters);
                }
                if (IsDelimiter(i - 1, wordDelimiters))
                {
                    FindPreviousWordFromDelimiter(i, wordDelimiters);
                }
                return i;
            }
            while (i > 0 && IsDelimiter(i, wordDelimiters))
            {
                i--;
            }
            if (i == 0 && IsDelimiter(i, wordDelimiters))
            {
                return i;
            }
            return i + 1;
        }

        /// <summary>
        /// Returns true if the cursor is on a word delimiter
        /// </summary>
        private bool IsDelimiter(int i, string wordDelimiters)
        {
            return wordDelimiters.IndexOf(_buffer[i]) >= 0;
        }

        /// <summary>
        /// Returns true if <paramref name="c"/> is in the set of <paramref name="wordDelimiters"/>.
        /// </summary>
        private bool IsDelimiter(char c, string wordDelimiters)
        {
            foreach (char delimiter in wordDelimiters)
            {
                if (c == delimiter)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns the cursor position of the beginning of the previous word when starting on a delimiter
        /// </summary>
        private int FindPreviousWordFromDelimiter(int i, string wordDelimiters)
        {
            i--;
            if (IsDelimiter(i, wordDelimiters))
            {
                while (i > 0 && IsDelimiter(i, wordDelimiters))
                {
                    i--;
                }
                if (i == 0 && !IsDelimiter(i, wordDelimiters))
                {
                    return i + 1;
                }
                if (!IsWhiteSpace(i))
                {
                    return i + 1;
                }
                return i;
            }
            return ViFindPreviousWordPoint(i, wordDelimiters);
        }


        /// <summary>
        /// Returns the cursor position of the beginning of the previous word when starting on white space
        /// </summary>
        private int FindPreviousWordFromWhiteSpace(int i, string wordDelimiters)
        {
            while (IsWhiteSpace(i) && i > 0)
            {
                i--;
            }
            int j = i - 1;
            if (j < 0 || !InWord(i, wordDelimiters) || char.IsWhiteSpace(_buffer[j]))
            {
                return i;
            }
            return (ViFindPreviousWordPoint(i, wordDelimiters));
        }

        /// <summary>
        /// Returns the cursor position of the previous word, ignoring all delimiters other what white space
        /// </summary>
        private int ViFindPreviousGlob()
        {
            int i = _current;
            if (i == 0)
            {
                return 0;
            }
            i--;

            return ViFindPreviousGlob(i);
        }

        /// <summary>
        /// Returns the cursor position of the previous word from i, ignoring all delimiters other what white space
        /// </summary>
        private int ViFindPreviousGlob(int i)
        {
            if (i <= 0)
            {
                return 0;
            }

            if (!IsWhiteSpace(i))
            {
                while (i > 0 && !IsWhiteSpace(i))
                {
                    i--;
                }
                if (!IsWhiteSpace(i))
                {
                    return i;
                }
                return i + 1;
            }
            while (i > 0 && IsWhiteSpace(i))
            {
                i--;
            }
            if (i == 0)
            {
                return i;
            }
            return ViFindPreviousGlob(i);
        }

        /// <summary>
        /// Finds the next work, using only white space as the word delimiter.
        /// </summary>
        private int ViFindNextGlob()
        {
            int i = _current;
            return ViFindNextGlob(i);
        }

        private int ViFindNextGlob(int i)
        {
            if (i >= _buffer.Length)
            {
                return i;
            }
            while (!IsAtEndOfLine(i) && !IsWhiteSpace(i))
            {
                i++;
            }
            if (IsAtEndOfLine(i) && !IsWhiteSpace(i))
            {
                return i + 1;
            }
            while (!IsAtEndOfLine(i) && IsWhiteSpace(i))
            {
                i++;
            }
            if (IsAtEndOfLine(i) && IsWhiteSpace(i))
            {
                return i + 1;
            }
            return i;
        }

        /// <summary>
        /// Finds the end of the current/next word as defined by whitespace.
        /// </summary>
        private int ViFindEndOfGlob()
        {
            return ViFindGlobEnd(_current);
        }

        /// <summary>
        /// Find the end of the current/next word as defined by wordDelimiters and whitespace.
        /// </summary>
        private int ViFindNextWordEnd(string wordDelimiters)
        {
            int i = _current;

            return ViFindNextWordEnd(i, wordDelimiters);
        }

        /// <summary>
        /// Find the end of the current/next word as defined by wordDelimiters and whitespace.
        /// </summary>
        private int ViFindNextWordEnd(int i, string wordDelimiters)
        {
            if (IsAtEndOfLine(i))
            {
                return i;
            }

            if (IsDelimiter(i, wordDelimiters) && !IsDelimiter(i + 1, wordDelimiters))
            {
                i++;
                if (IsAtEndOfLine(i))
                {
                    return i;
                }
            }
            else if (InWord(i, wordDelimiters) && !InWord(i + 1, wordDelimiters))
            {
                i++;
                if (IsAtEndOfLine(i))
                {
                    return i;
                }
            }

            while (!IsAtEndOfLine(i) && IsWhiteSpace(i))
            {
                i++;
            }

            if (IsAtEndOfLine(i))
            {
                return i;
            }

            if (IsDelimiter(i, wordDelimiters))
            {
                while (!IsAtEndOfLine(i) && IsDelimiter(i, wordDelimiters))
                {
                    i++;
                }
                if (!IsDelimiter(i, wordDelimiters))
                {
                    return i - 1;
                }
            }
            else
            {
                while (!IsAtEndOfLine(i) && InWord(i, wordDelimiters))
                {
                    i++;
                }
                if (!InWord(i, wordDelimiters))
                {
                    return i - 1;
                }
            }

            return i;
        }

        /// <summary>
        /// Return the last character in a white space defined word after skipping contiguous white space.
        /// </summary>
        private int ViFindGlobEnd(int i)
        {
            if (IsAtEndOfLine(i))
            {
                return i;
            }
            i++;
            if (IsAtEndOfLine(i))
            {
                return i;
            }
            while (!IsAtEndOfLine(i) && IsWhiteSpace(i))
            {
                i++;
            }
            if (IsAtEndOfLine(i))
            {
                return i;
            }
            while (!IsAtEndOfLine(i) && !IsWhiteSpace(i))
            {
                i++;
            }
            if (IsWhiteSpace(i))
            {
                return i - 1;
            }
            return i;
        }

        private int ViFindEndOfPreviousGlob()
        {
            int i = _current;

            return ViFindEndOfPreviousGlob(i);
        }

        private int ViFindEndOfPreviousGlob(int i)
        {
            if (IsWhiteSpace(i))
            {
                while (i > 0 && IsWhiteSpace(i))
                {
                    i--;
                }
                return i;
            }

            while (i > 0 && !IsWhiteSpace(i))
            {
                i--;
            }
            return ViFindEndOfPreviousGlob(i);
        }
    }
}
