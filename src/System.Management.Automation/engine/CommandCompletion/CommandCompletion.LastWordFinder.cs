// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    public partial class CommandCompletion
    {
        /// <summary>
        /// LastWordFinder implements the algorithm we use to search for the last word in a line of input taken from the console.
        /// This class exists for legacy purposes only - V3 and forward uses a slightly different interface.
        /// </summary>
        private sealed class LastWordFinder
        {
            internal static string FindLastWord(string sentence, out int replacementIndexOut, out char closingQuote)
            {
                return (new LastWordFinder(sentence)).FindLastWord(out replacementIndexOut, out closingQuote);
            }

            private LastWordFinder(string sentence)
            {
                _replacementIndex = 0;
                Diagnostics.Assert(sentence != null, "need to provide an instance");
                _sentence = sentence;
            }

            /// <summary>
            /// Locates the last "word" in a string of text.  A word is a conguous sequence of characters that are not
            /// whitespace, or a contiguous set grouped by single or double quotes.  Can be called by at most 1 thread at a time
            /// per LastWordFinder instance.
            /// </summary>
            /// <param name="replacementIndexOut">
            /// Receives the character index (from the front of the string) of the starting point of the located word, or 0 if
            /// the word starts at the beginning of the sentence.
            /// </param>
            /// <param name="closingQuote">
            /// Receives the quote character that would be needed to end the sentence with a balanced pair of quotes.  For
            /// instance, if sentence is "foo then " is returned, if sentence if "foo" then nothing is returned, if sentence is
            /// 'foo then ' is returned, if sentence is 'foo' then nothing is returned.
            /// </param>
            /// <returns>The last word located, or the empty string if no word could be found.</returns>
            private string FindLastWord(out int replacementIndexOut, out char closingQuote)
            {
                bool inSingleQuote = false;
                bool inDoubleQuote = false;

                ReplacementIndex = 0;

                for (_sentenceIndex = 0; _sentenceIndex < _sentence.Length; ++_sentenceIndex)
                {
                    Diagnostics.Assert(!(inSingleQuote && inDoubleQuote),
                        "Can't be in both single and double quotes");

                    char c = _sentence[_sentenceIndex];

                    // there are 3 possibilities:
                    // 1) a new sequence is starting,
                    // 2) a sequence is ending, or
                    // 3) a sequence is due to end on the next matching quote, end-of-sentence, or whitespace

                    if (c == '\'')
                    {
                        HandleQuote(ref inSingleQuote, ref inDoubleQuote, c);
                    }
                    else if (c == '"')
                    {
                        HandleQuote(ref inDoubleQuote, ref inSingleQuote, c);
                    }
                    else if (c == '`')
                    {
                        Consume(c);
                        if (++_sentenceIndex < _sentence.Length)
                        {
                            Consume(_sentence[_sentenceIndex]);
                        }
                    }
                    else if (IsWhitespace(c))
                    {
                        if (_sequenceDueToEnd)
                        {
                            // we skipped a quote earlier, now end that sequence

                            _sequenceDueToEnd = false;
                            if (inSingleQuote)
                            {
                                inSingleQuote = false;
                            }

                            if (inDoubleQuote)
                            {
                                inDoubleQuote = false;
                            }

                            ReplacementIndex = _sentenceIndex + 1;
                        }
                        else if (inSingleQuote || inDoubleQuote)
                        {
                            // a sequence is started and we're in quotes

                            Consume(c);
                        }
                        else
                        {
                            // no sequence is started, so ignore c

                            ReplacementIndex = _sentenceIndex + 1;
                        }
                    }
                    else
                    {
                        // a sequence is started and we're in it

                        Consume(c);
                    }
                }

                string result = new string(_wordBuffer, 0, _wordBufferIndex);

                closingQuote = inSingleQuote ? '\'' : inDoubleQuote ? '"' : '\0';
                replacementIndexOut = ReplacementIndex;
                return result;
            }

            private void HandleQuote(ref bool inQuote, ref bool inOppositeQuote, char c)
            {
                if (inOppositeQuote)
                {
                    // a sequence is started, and we're in it.
                    Consume(c);
                    return;
                }

                if (inQuote)
                {
                    if (_sequenceDueToEnd)
                    {
                        // I've ended a sequence and am starting another; don't consume c, update replacementIndex
                        ReplacementIndex = _sentenceIndex + 1;
                    }

                    _sequenceDueToEnd = !_sequenceDueToEnd;
                }
                else
                {
                    // I'm starting a sequence; don't consume c, update replacementIndex
                    inQuote = true;
                    ReplacementIndex = _sentenceIndex;
                }
            }

            private void Consume(char c)
            {
                Diagnostics.Assert(_wordBuffer != null, "wordBuffer is not initialized");
                Diagnostics.Assert(_wordBufferIndex < _wordBuffer.Length, "wordBufferIndex is out of range");

                _wordBuffer[_wordBufferIndex++] = c;
            }

            private int ReplacementIndex
            {
                get
                {
                    return _replacementIndex;
                }

                set
                {
                    Diagnostics.Assert(value >= 0 && value < _sentence.Length + 1, "value out of range");

                    // when we set the replacement index, that means we're also resetting our word buffer. we know wordBuffer
                    // will never be longer than sentence.

                    _wordBuffer = new char[_sentence.Length];
                    _wordBufferIndex = 0;
                    _replacementIndex = value;
                }
            }

            private static bool IsWhitespace(char c)
            {
                return (c == ' ') || (c == '\x0009');
            }

            private readonly string _sentence;
            private char[] _wordBuffer;
            private int _wordBufferIndex;
            private int _replacementIndex;
            private int _sentenceIndex;
            private bool _sequenceDueToEnd;
        }
    }
}
