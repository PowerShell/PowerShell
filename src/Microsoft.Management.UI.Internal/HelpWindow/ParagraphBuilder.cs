// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Documents;
using System.Windows.Media;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Builds a paragraph based on Text + Bold + Highlight information.
    /// Bold are the segments of the text that should be bold, and Highlight are
    /// the segments of the text that should be highlighted (like search results).
    /// </summary>
    internal class ParagraphBuilder : INotifyPropertyChanged
    {
        /// <summary>
        /// The text spans that should be bold.
        /// </summary>
        private readonly List<TextSpan> boldSpans;

        /// <summary>
        /// The text spans that should be highlighted.
        /// </summary>
        private readonly List<TextSpan> highlightedSpans;

        /// <summary>
        /// The text displayed.
        /// </summary>
        private readonly StringBuilder textBuilder;

        /// <summary>
        /// Paragraph built in BuildParagraph.
        /// </summary>
        private readonly Paragraph paragraph;

        /// <summary>
        /// Initializes a new instance of the ParagraphBuilder class.
        /// </summary>
        /// <param name="paragraph">Paragraph we will be adding lines to in BuildParagraph.</param>
        internal ParagraphBuilder(Paragraph paragraph)
        {
            ArgumentNullException.ThrowIfNull(paragraph);

            this.paragraph = paragraph;
            this.boldSpans = new List<TextSpan>();
            this.highlightedSpans = new List<TextSpan>();
            this.textBuilder = new StringBuilder();
        }

        #region INotifyPropertyChanged Members
        /// <summary>
        /// Used to notify of property changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        /// <summary>
        /// Gets the number of highlights.
        /// </summary>
        internal int HighlightCount
        {
            get { return this.highlightedSpans.Count; }
        }

        /// <summary>
        /// Gets the paragraph built in BuildParagraph.
        /// </summary>
        internal Paragraph Paragraph
        {
            get { return this.paragraph; }
        }

        /// <summary>
        /// Called after all the AddText calls have been made to build the paragraph
        /// based on the current text.
        /// This method goes over 3 collections simultaneously:
        ///    1) characters in this.textBuilder
        ///    2) spans in this.boldSpans
        ///    3) spans in this.highlightedSpans
        /// And adds the minimal number of Inlines to the paragraph so that all
        /// characters that should be bold and/or highlighted are.
        /// </summary>
        internal void BuildParagraph()
        {
            this.paragraph.Inlines.Clear();

            int currentBoldIndex = 0;
            TextSpan? currentBoldSpan = this.boldSpans.Count == 0 ? (TextSpan?)null : this.boldSpans[0];
            int currentHighlightedIndex = 0;
            TextSpan? currentHighlightedSpan = this.highlightedSpans.Count == 0 ? (TextSpan?)null : this.highlightedSpans[0];

            bool currentBold = false;
            bool currentHighlighted = false;

            StringBuilder sequence = new StringBuilder();
            int i = 0;
            foreach (char c in this.textBuilder.ToString())
            {
                bool newBold = false;
                bool newHighlighted = false;

                ParagraphBuilder.MoveSpanToPosition(ref currentBoldIndex, ref currentBoldSpan, i, this.boldSpans);
                newBold = currentBoldSpan == null ? false : currentBoldSpan.Value.Contains(i);

                ParagraphBuilder.MoveSpanToPosition(ref currentHighlightedIndex, ref currentHighlightedSpan, i, this.highlightedSpans);
                newHighlighted = currentHighlightedSpan == null ? false : currentHighlightedSpan.Value.Contains(i);

                if (newBold != currentBold || newHighlighted != currentHighlighted)
                {
                    ParagraphBuilder.AddInline(this.paragraph, currentBold, currentHighlighted, sequence);
                }

                sequence.Append(c);

                currentHighlighted = newHighlighted;
                currentBold = newBold;
                i++;
            }

            ParagraphBuilder.AddInline(this.paragraph, currentBold, currentHighlighted, sequence);
        }

        /// <summary>
        /// Highlights all occurrences of <paramref name="search"/>.
        /// This is called after all calls to AddText have been made.
        /// </summary>
        /// <param name="search">Search string.</param>
        /// <param name="caseSensitive">True if search should be case sensitive.</param>
        /// <param name="wholeWord">True if we should search whole word only.</param>
        internal void HighlightAllInstancesOf(string search, bool caseSensitive, bool wholeWord)
        {
            this.highlightedSpans.Clear();

            if (search == null || search.Trim().Length == 0)
            {
                this.BuildParagraph();
                this.OnNotifyPropertyChanged("HighlightCount");
                return;
            }

            string text = this.textBuilder.ToString();
            StringComparison comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            int start = 0;
            int match;
            while ((match = text.IndexOf(search, start, comparison)) != -1)
            {
                // false loop
                do
                {
                    if (wholeWord)
                    {
                        if (match > 0 && char.IsLetterOrDigit(text[match - 1]))
                        {
                            break;
                        }

                        if ((match + search.Length <= text.Length - 1) && char.IsLetterOrDigit(text[match + search.Length]))
                        {
                            break;
                        }
                    }

                    this.AddHighlight(match, search.Length);
                }
                while (false);

                start = match + search.Length;
            }

            this.BuildParagraph();
            this.OnNotifyPropertyChanged("HighlightCount");
        }

        /// <summary>
        /// Adds text to the paragraph later build with BuildParagraph.
        /// </summary>
        /// <param name="str">Text to be added.</param>
        /// <param name="bold">True if the text should be bold.</param>
        internal void AddText(string str, bool bold)
        {
            ArgumentNullException.ThrowIfNull(str);

            if (str.Length == 0)
            {
                return;
            }

            if (bold)
            {
                this.boldSpans.Add(new TextSpan(this.textBuilder.Length, str.Length));
            }

            this.textBuilder.Append(str);
        }

        /// <summary>
        /// Called before a derived class starts adding text
        /// to reset the current content.
        /// </summary>
        internal void ResetAllText()
        {
            this.boldSpans.Clear();
            this.highlightedSpans.Clear();
            this.textBuilder.Clear();
        }

        /// <summary>
        /// Adds an inline to <paramref name="currentParagraph"/> based on the remaining parameters.
        /// </summary>
        /// <param name="currentParagraph">Paragraph to add Inline to.</param>
        /// <param name="currentBold">True if text should be added in bold.</param>
        /// <param name="currentHighlighted">True if the text should be added with highlight.</param>
        /// <param name="sequence">The text to add and clear.</param>
        private static void AddInline(Paragraph currentParagraph, bool currentBold, bool currentHighlighted, StringBuilder sequence)
        {
            if (sequence.Length == 0)
            {
                return;
            }

            Run run = new Run(sequence.ToString());
            if (currentHighlighted)
            {
                run.Background = ParagraphSearcher.HighlightBrush;
            }

            Inline inline = currentBold ? (Inline)new Bold(run) : run;
            currentParagraph.Inlines.Add(inline);
            sequence.Clear();
        }

        /// <summary>
        /// This is an auxiliar method in BuildParagraph to move the current bold or highlighted spans
        /// according to the <paramref name="caracterPosition"/>
        /// The current bold and highlighted span should be ending ahead of the current position.
        /// Moves <paramref name="currentSpanIndex"/> and <paramref name="currentSpan"/> to the
        /// proper span in <paramref name="allSpans"/> according to the <paramref name="caracterPosition"/>
        /// This is an auxiliar method in BuildParagraph.
        /// </summary>
        /// <param name="currentSpanIndex">Current index within <paramref name="allSpans"/>.</param>
        /// <param name="currentSpan">Current span within <paramref name="allSpans"/>.</param>
        /// <param name="caracterPosition">Character position. This comes from a position within this.textBuilder.</param>
        /// <param name="allSpans">The collection of spans. This is either this.boldSpans or this.highlightedSpans.</param>
        private static void MoveSpanToPosition(ref int currentSpanIndex, ref TextSpan? currentSpan, int caracterPosition, List<TextSpan> allSpans)
        {
            if (currentSpan == null || caracterPosition <= currentSpan.Value.End)
            {
                return;
            }

            for (int newBoldIndex = currentSpanIndex + 1; newBoldIndex < allSpans.Count; newBoldIndex++)
            {
                TextSpan newBoldSpan = allSpans[newBoldIndex];
                if (caracterPosition <= newBoldSpan.End)
                {
                    currentSpanIndex = newBoldIndex;
                    currentSpan = newBoldSpan;
                    return;
                }
            }

            // there is no span ending ahead of current position, so
            // we set the current span to null to prevent unnecessary comparisons against the currentSpan
            currentSpan = null;
        }

        /// <summary>
        /// Adds one individual text highlight
        /// This is called after all calls to AddText have been made.
        /// </summary>
        /// <param name="start">Highlight start.</param>
        /// <param name="length">Highlight length.</param>
        private void AddHighlight(int start, int length)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(start);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(this.textBuilder.Length, start + length, nameof(length));

            this.highlightedSpans.Add(new TextSpan(start, length));
        }

        /// <summary>
        /// Called internally to notify when a property changed.
        /// </summary>
        /// <param name="propertyName">Property name.</param>
        private void OnNotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// A text span used to mark bold and highlighted segments.
        /// </summary>
        internal struct TextSpan
        {
            /// <summary>
            /// Index of the first character in the span.
            /// </summary>
            private readonly int start;

            /// <summary>
            /// Index of the last character in the span.
            /// </summary>
            private readonly int end;

            /// <summary>
            /// Initializes a new instance of the TextSpan struct.
            /// </summary>
            /// <param name="start">Index of the first character in the span.</param>
            /// <param name="length">Index of the last character in the span.</param>
            internal TextSpan(int start, int length)
            {
                ArgumentOutOfRangeException.ThrowIfNegative(start);
                ArgumentOutOfRangeException.ThrowIfLessThan(1, length);

                this.start = start;
                this.end = start + length - 1;
            }

            /// <summary>
            /// Gets the index of the first character in the span.
            /// </summary>
            internal int Start
            {
                get { return this.start; }
            }

            /// <summary>
            /// Gets the index of the first character in the span.
            /// </summary>
            internal int End
            {
                get
                {
                    return this.end;
                }
            }

            /// <summary>
            /// Returns true if the <paramref name="position"/> is between start and end (inclusive).
            /// </summary>
            /// <param name="position">Position to verify if is in the span.</param>
            /// <returns>True if the <paramref name="position"/> is between start and end (inclusive).</returns>
            internal bool Contains(int position)
            {
                return (position >= this.start) && (position <= this.end);
            }
        }
    }
}
