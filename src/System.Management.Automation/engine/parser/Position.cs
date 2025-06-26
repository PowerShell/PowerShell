// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
#if !STANDALONE
using System.Management.Automation.Internal;
#endif

namespace System.Management.Automation.Language
{
    #region Public Interfaces

    /// <summary>
    /// Represents a single point in a script.  The script may come from a file or interactive input.
    /// </summary>
#nullable enable
    public interface IScriptPosition
    {
        /// <summary>
        /// The name of the file, or if the script did not come from a file, then null.
        /// </summary>
        string? File { get; }

        /// <summary>
        /// The line number of the position, with the value 1 being the first line.
        /// </summary>
        int LineNumber { get; }

        /// <summary>
        /// The column number of the position, with the value 1 being the first column.
        /// </summary>
        int ColumnNumber { get; }

        /// <summary>
        /// The offset from the beginning of the script.
        /// </summary>
        int Offset { get; }

        /// <summary>
        /// The complete text of the line that this position is included on.
        /// </summary>
        string Line { get; }

        /// <summary>
        /// The complete script that this position is included in.
        /// </summary>
        string? GetFullScript();
    }
#nullable restore

    /// <summary>
    /// Represents the a span of text in a script.
    /// </summary>
#nullable enable
    public interface IScriptExtent
    {
        /// <summary>
        /// The filename the extent includes, or null if the extent is not included in any file.
        /// </summary>
        string? File { get; }

        /// <summary>
        /// The starting position of the extent.
        /// </summary>
        IScriptPosition StartScriptPosition { get; }

        /// <summary>
        /// The end position of the extent.  This position is actually 1 character past the end of the extent.
        /// </summary>
        IScriptPosition EndScriptPosition { get; }

        /// <summary>
        /// The line number at the beginning of the extent, with the value 1 being the first line.
        /// </summary>
        int StartLineNumber { get; }

        /// <summary>
        /// The column number at the beginning of the extent, with the value 1 being the first column.
        /// </summary>
        int StartColumnNumber { get; }

        /// <summary>
        /// The line number at the end of the extent, with the value 1 being the first line.
        /// </summary>
        int EndLineNumber { get; }

        /// <summary>
        /// The column number at the end of the extent, with the value 1 being the first column.
        /// </summary>
        int EndColumnNumber { get; }

        /// <summary>
        /// The script text that the extent includes.
        /// </summary>
        string Text { get; }

        /// <summary>
        /// The starting offset of the extent.
        /// </summary>
        int StartOffset { get; }

        /// <summary>
        /// The ending offset of the extent.
        /// </summary>
        int EndOffset { get; }
    }
#nullable restore

    /// <summary>
    /// A few utility functions for script positions.
    /// </summary>
    internal static class PositionUtilities
    {
        /// <summary>
        /// Return a unique position representing an empty or missing position.
        /// </summary>
        public static IScriptPosition EmptyPosition { get; } = new EmptyScriptPosition();

        /// <summary>
        /// Return a unique extent representing an empty or missing extent.
        /// </summary>
        public static IScriptExtent EmptyExtent { get; } = new EmptyScriptExtent();

        /// <summary>
        /// Return a message that looks like:
        ///
        ///     At {filename}:{line} char:{column}
        ///     + $x + @y
        ///     +    ~
        /// </summary>
        internal static string VerboseMessage(IScriptExtent position)
        {
            if (PositionUtilities.EmptyExtent.Equals(position))
            {
                return string.Empty;
            }

            string fileName = position.File;
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = ParserStrings.TextForWordLine;
            }

            string sourceLine = position.StartScriptPosition.Line.TrimEnd();

            string message = string.Empty;
            if (!string.IsNullOrEmpty(sourceLine))
            {
                int spacesBeforeError = position.StartColumnNumber - 1;
                int errorLength = (position.StartLineNumber == position.EndLineNumber && position.EndColumnNumber <= sourceLine.Length + 1)
                                      ? position.EndColumnNumber - position.StartColumnNumber
                                      : sourceLine.Length - position.StartColumnNumber + 1;

                // Expand tabs before figuring out if we need to truncate the line
                if (sourceLine.Contains('\t'))
                {
                    var copyLine = new StringBuilder(sourceLine.Length * 2);

                    var beforeError = sourceLine.Substring(0, spacesBeforeError).Replace("\t", "    ");
                    var error = sourceLine.Substring(spacesBeforeError, errorLength).Replace("\t", "    ");

                    copyLine.Append(beforeError);
                    copyLine.Append(error);
                    copyLine.Append(sourceLine.Substring(spacesBeforeError + errorLength).Replace("\t", "    "));

                    spacesBeforeError = beforeError.Length;
                    errorLength = error.Length;
                    sourceLine = copyLine.ToString();
                }

                // Max width is 69 because:
                //   * sometimes PowerShell is opened with width 80
                //   * we always prepend "+ "
                //   * we sometimes prepend "... "
                //   * we sometimes append " ..."
                //   * wrapping kicks in if we hit the width exactly, so -1 to avoid that.
                const int maxLineLength = 69;
                bool needsPrefixDots = false;
                bool needsSuffixDots = false;

                int lineLength = sourceLine.Length;
                var sb = new StringBuilder(sourceLine.Length * 2 + 4);
                if (lineLength > maxLineLength)
                {
                    // Need to truncate - include as much of the error as we can, but with
                    // some preceding context if possible.

                    int totalPrefix = spacesBeforeError;
                    int prefix = Math.Min(totalPrefix, 12);

                    int totalSuffix = lineLength - errorLength - spacesBeforeError;
                    int suffix = Math.Min(totalSuffix, 8);

                    int candidateLength = prefix + errorLength + suffix;
                    if (candidateLength >= maxLineLength)
                    {
                        // Too long.  The suffix is truncated automatically by
                        // the Substring call, but we might need some of the
                        // squiggles removed as well.
                        if (prefix + errorLength >= maxLineLength)
                        {
                            errorLength = maxLineLength - prefix;
                        }

                        needsSuffixDots = true;
                    }
                    else
                    {
                        // We can shift prefix to suffix or vice versa to fill in
                        // more of the line.  Prefer shifting to prefix.
                        int prefixAvailable = totalPrefix - prefix;
                        if (prefixAvailable > 0)
                        {
                            prefix += Math.Min(prefixAvailable, maxLineLength - candidateLength);
                            candidateLength = prefix + errorLength + suffix;
                        }

                        if (candidateLength < maxLineLength && totalSuffix > 0)
                        {
                            suffix += Math.Min(totalSuffix, maxLineLength - candidateLength);
                        }

                        needsSuffixDots = (suffix < totalSuffix);
                    }

                    needsPrefixDots = (prefix < totalPrefix);

                    var startIndex = Math.Max(spacesBeforeError - prefix, 0);
                    sourceLine = sourceLine.Substring(startIndex, maxLineLength);
                    spacesBeforeError = Math.Min(spacesBeforeError, prefix);
                    errorLength = Math.Min(errorLength, maxLineLength - spacesBeforeError);
                }

                if (needsPrefixDots)
                {
                    sb.Append("\u2026 "); // Unicode ellipsis character
                }

                sb.Append(sourceLine);

                if (needsSuffixDots)
                {
                    sb.Append(" \u2026"); // Unicode ellipsis character
                }

                sb.Append(Environment.NewLine);
                sb.Append("+ ");
                sb.Append(' ', spacesBeforeError + (needsPrefixDots ? 2 : 0));
                // errorLength of 0 happens at EOF - always write out 1.
                sb.Append('~', errorLength > 0 ? errorLength : 1);

                message = sb.ToString();
            }

            return StringUtil.Format(
                ParserStrings.TextForPositionMessage,
                fileName,
                position.StartLineNumber,
                position.StartColumnNumber,
                message);
        }

        /// <summary>
        /// Return a message that looks like:
        ///     12+ $x + &lt;&lt;&lt;&lt; $b.
        /// </summary>
        internal static string BriefMessage(IScriptPosition position)
        {
            StringBuilder message = new StringBuilder(position.Line);
            if (position.ColumnNumber > message.Length)
            {
                message.Append(" <<<< ");
            }
            else
            {
                message.Insert(position.ColumnNumber - 1, " >>>> ");
            }

            return StringUtil.Format(ParserStrings.TraceScriptLineMessage, position.LineNumber, message.ToString());
        }

        internal static IScriptExtent NewScriptExtent(IScriptExtent start, IScriptExtent end)
        {
            if (start == end)
            {
                return start;
            }

            if (start == EmptyExtent)
            {
                return end;
            }

            if (end == EmptyExtent)
            {
                return start;
            }

            InternalScriptExtent startExtent = start as InternalScriptExtent;
            InternalScriptExtent endExtent = end as InternalScriptExtent;
            Diagnostics.Assert(startExtent != null && endExtent != null, "This function only handles internal and empty extents");
            Diagnostics.Assert(startExtent.PositionHelper == endExtent.PositionHelper, "Extents must be from same source");

            return new InternalScriptExtent(startExtent.PositionHelper, startExtent.StartOffset, endExtent.EndOffset);
        }

        internal static bool IsBefore(this IScriptExtent extentToTest, IScriptExtent startExtent)
        {
            if (extentToTest.EndLineNumber < startExtent.StartLineNumber)
            {
                return true;
            }

            if (extentToTest.EndLineNumber == startExtent.StartLineNumber)
            {
                return extentToTest.EndColumnNumber <= startExtent.StartColumnNumber;
            }

            return false;
        }

        internal static bool IsAfter(this IScriptExtent extentToTest, IScriptExtent endExtent)
        {
            if (extentToTest.StartLineNumber > endExtent.EndLineNumber)
            {
                return true;
            }

            if (extentToTest.StartLineNumber == endExtent.EndLineNumber)
            {
                return extentToTest.StartColumnNumber >= endExtent.EndColumnNumber;
            }

            return false;
        }

        internal static bool IsWithin(this IScriptExtent extentToTest, IScriptExtent extent)
        {
            return extentToTest.StartOffset >= extent.StartOffset && extentToTest.EndOffset <= extent.EndOffset;
        }

        internal static bool IsAfter(this IScriptExtent extent, int line, int column)
        {
            if (line < extent.StartLineNumber)
                return true;

            return (line == extent.StartLineNumber && column < extent.StartColumnNumber);
        }

        internal static bool ContainsLineAndColumn(this IScriptExtent extent, int line, int column)
        {
            if (extent.StartLineNumber == line)
            {
                if (column == 0)
                {
                    return true;
                }

                if (column >= extent.StartColumnNumber)
                {
                    if (extent.EndLineNumber != extent.StartLineNumber)
                    {
                        return true;
                    }

                    return (column < extent.EndColumnNumber);
                }

                return false;
            }

            if (extent.StartLineNumber > line)
                return false;

            if (line > extent.EndLineNumber)
                return false;

            if (extent.EndLineNumber == line)
                return column < extent.EndColumnNumber;

            return true;
        }
    }

    #endregion Public Interfaces

    #region Internal Position

    internal class PositionHelper
    {
        private int[] _lineStartMap;

        internal PositionHelper(string filename, string scriptText)
        {
            File = filename;
            ScriptText = scriptText;
        }

        internal string ScriptText { get; }

        internal int[] LineStartMap
        {
            set { _lineStartMap = value; }
        }

        public string File { get; }

        internal int LineFromOffset(int offset)
        {
            int line = Array.BinarySearch<int>(_lineStartMap, offset);
            if (line < 0)
            {
                line = ~line - 1;
            }

            return line + 1;
        }

        internal int ColumnFromOffset(int offset)
        {
            return offset - _lineStartMap[LineFromOffset(offset) - 1] + 1;
        }

        internal string Text(int line)
        {
            int start = _lineStartMap[line - 1];
            if (line < _lineStartMap.Length)
            {
                int length = _lineStartMap[line] - start;
                return ScriptText.Substring(start, length);
            }

            return ScriptText.Substring(start);
        }
    }

    internal sealed class InternalScriptPosition : IScriptPosition
    {
        private readonly PositionHelper _positionHelper;

        internal InternalScriptPosition(PositionHelper _positionHelper, int offset)
        {
            this._positionHelper = _positionHelper;
            Offset = offset;
        }

        public string File { get { return _positionHelper.File; } }

        public int LineNumber { get { return _positionHelper.LineFromOffset(Offset); } }

        public int ColumnNumber { get { return _positionHelper.ColumnFromOffset(Offset); } }

        public string Line { get { return _positionHelper.Text(LineNumber); } }

        public int Offset { get; }

        internal InternalScriptPosition CloneWithNewOffset(int offset)
        {
            return new InternalScriptPosition(_positionHelper, offset);
        }

        public string GetFullScript()
        {
            return _positionHelper.ScriptText;
        }
    }

    internal sealed class InternalScriptExtent : IScriptExtent
    {
        internal InternalScriptExtent(PositionHelper _positionHelper, int startOffset, int endOffset)
        {
            this.PositionHelper = _positionHelper;
            StartOffset = startOffset;
            EndOffset = endOffset;
        }

        public string File
        {
            get { return PositionHelper.File; }
        }

        public IScriptPosition StartScriptPosition
        {
            get { return new InternalScriptPosition(PositionHelper, StartOffset); }
        }

        public IScriptPosition EndScriptPosition
        {
            get { return new InternalScriptPosition(PositionHelper, EndOffset); }
        }

        public int StartLineNumber
        {
            get { return PositionHelper.LineFromOffset(StartOffset); }
        }

        public int StartColumnNumber
        {
            get { return PositionHelper.ColumnFromOffset(StartOffset); }
        }

        public int EndLineNumber
        {
            get { return PositionHelper.LineFromOffset(EndOffset); }
        }

        public int EndColumnNumber
        {
            get { return PositionHelper.ColumnFromOffset(EndOffset); }
        }

        public string Text
        {
            get
            {
                // StartOffset can be > the length for the EOF token.
                if (StartOffset > PositionHelper.ScriptText.Length)
                {
                    return string.Empty;
                }

                return PositionHelper.ScriptText.Substring(StartOffset, EndOffset - StartOffset);
            }
        }

        public override string ToString()
        {
            return Text;
        }

        internal PositionHelper PositionHelper { get; }

        public int StartOffset { get; }

        public int EndOffset { get; }
    }

    #endregion Internal Position

    #region "Empty" Positions

    internal sealed class EmptyScriptPosition : IScriptPosition
    {
        public string File { get { return null; } }

        public int LineNumber { get { return 0; } }

        public int ColumnNumber { get { return 0; } }

        public int Offset { get { return 0; } }

        public string Line { get { return string.Empty; } }

        public string GetFullScript() { return null; }
    }

    internal sealed class EmptyScriptExtent : IScriptExtent
    {
        public string File { get { return null; } }

        public IScriptPosition StartScriptPosition { get { return PositionUtilities.EmptyPosition; } }

        public IScriptPosition EndScriptPosition { get { return PositionUtilities.EmptyPosition; } }

        public int StartLineNumber { get { return 0; } }

        public int StartColumnNumber { get { return 0; } }

        public int EndLineNumber { get { return 0; } }

        public int EndColumnNumber { get { return 0; } }

        public int StartOffset { get { return 0; } }

        public int EndOffset { get { return 0; } }

        public string Text { get { return string.Empty; } }

        public override bool Equals(object obj)
        {
            if (!(obj is IScriptExtent otherPosition))
            {
                return false;
            }

            if ((string.IsNullOrEmpty(otherPosition.File)) &&
                (otherPosition.StartLineNumber == StartLineNumber) &&
                (otherPosition.StartColumnNumber == StartColumnNumber) &&
                (otherPosition.EndLineNumber == EndLineNumber) &&
                (otherPosition.EndColumnNumber == EndColumnNumber) &&
                (string.IsNullOrEmpty(otherPosition.Text)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    #endregion "Empty" Positions

    /// <summary>
    /// Represents a single point in a script.  The script may come from a file or interactive input.
    /// </summary>
    public sealed class ScriptPosition : IScriptPosition
    {
        private readonly string _fullScript;

        /// <summary>
        /// Creates a new script position, which represents a point in a script.
        /// </summary>
        /// <param name="scriptName">The name of the file, or if the script did not come from a file, then null.</param>
        /// <param name="scriptLineNumber">The line number of the position, with the value 1 being the first line.</param>
        /// <param name="offsetInLine">The column number of the position, with the value 1 being the first column.</param>
        /// <param name="line">The complete text of the line that this position is included on.</param>
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        public ScriptPosition(string scriptName, int scriptLineNumber, int offsetInLine, string line)
        {
            File = scriptName;
            LineNumber = scriptLineNumber;
            ColumnNumber = offsetInLine;

            if (string.IsNullOrEmpty(line))
            {
                Line = string.Empty;
            }
            else
            {
                Line = line;
            }
        }

        /// <summary>
        /// Creates a new script position, which represents a point in a script.
        /// </summary>
        /// <param name="scriptName">The name of the file, or if the script did not come from a file, then null.</param>
        /// <param name="scriptLineNumber">The line number of the position, with the value 1 being the first line.</param>
        /// <param name="offsetInLine">The column number of the position, with the value 1 being the first column.</param>
        /// <param name="line">The complete text of the line that this position is included on.</param>
        /// <param name="fullScript">The complete script text.  Optional, can be null.</param>
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        public ScriptPosition(
            string scriptName,
            int scriptLineNumber,
            int offsetInLine,
            string line,
            string fullScript)
            : this(scriptName, scriptLineNumber, offsetInLine, line)
        {
            _fullScript = fullScript;
        }

        /// <summary>
        /// The name of the file, or if the script did not come from a file, then null.
        /// </summary>
        public string File { get; }

        /// <summary>
        /// The line number of the position, with the value 1 being the first line.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// The column number of the position, with the value 1 being the first column.
        /// </summary>
        public int ColumnNumber { get; }

        /// <summary>
        /// The offset from the beginning of the script, always return 0.
        /// </summary>
        public int Offset { get { return 0; } }

        /// <summary>
        /// The complete text of the line that this position is included on.
        /// </summary>
        public string Line { get; }

        /// <summary>
        /// The complete script that this position is included in.
        /// </summary>
        public string GetFullScript() { return _fullScript; }
    }

    /// <summary>
    /// A script extent used to customize the display of error location information.
    /// </summary>
    public sealed class ScriptExtent : IScriptExtent
    {
        private ScriptPosition _startPosition;
        private ScriptPosition _endPosition;

        private ScriptExtent()
        {
        }

        /// <summary>
        /// Creates a new ScriptExtent class.
        /// </summary>
        public ScriptExtent(ScriptPosition startPosition, ScriptPosition endPosition)
        {
            _startPosition = startPosition;
            _endPosition = endPosition;
        }

        /// <summary>
        /// The name of the file, or if the script did not come from a file, then null.
        /// </summary>
        public string File { get { return _startPosition.File; } }

        /// <summary>
        /// The starting position of the extent.
        /// </summary>
        public IScriptPosition StartScriptPosition { get { return _startPosition; } }

        /// <summary>
        /// The end position of the extent.  This position is actually 1 character past the end of the extent.
        /// </summary>
        public IScriptPosition EndScriptPosition { get { return _endPosition; } }

        /// <summary>
        /// The line number at the beginning of the extent, with the value 1 being the first line.
        /// </summary>
        public int StartLineNumber { get { return _startPosition.LineNumber; } }

        /// <summary>
        /// The column number at the beginning of the extent, with the value 1 being the first column.
        /// </summary>
        public int StartColumnNumber { get { return _startPosition.ColumnNumber; } }

        /// <summary>
        /// The line number at the end of the extent, with the value 1 being the first line.
        /// </summary>
        public int EndLineNumber { get { return _endPosition.LineNumber; } }

        /// <summary>
        /// The column number at the end of the extent, with the value 1 being the first column.
        /// </summary>
        public int EndColumnNumber { get { return _endPosition.ColumnNumber; } }

        /// <summary>
        /// The start offset (always returns 0)
        /// </summary>
        public int StartOffset { get { return 0; } }

        /// <summary>
        /// The end offset (always returns 0)
        /// </summary>
        public int EndOffset { get { return 0; } }

        /// <summary>
        /// The script text that the extent includes.
        /// </summary>
        public string Text
        {
            get
            {
                if (EndColumnNumber > 0)
                {
                    if (StartLineNumber == EndLineNumber)
                    {
                        return _startPosition.Line.Substring(_startPosition.ColumnNumber - 1,
                                                             _endPosition.ColumnNumber - _startPosition.ColumnNumber);
                    }

                    var start = _startPosition.Line.AsSpan(_startPosition.ColumnNumber);
                    var end = _endPosition.Line.AsSpan(0, _endPosition.ColumnNumber);
                    return string.Create(CultureInfo.InvariantCulture, $"{start}...{end}");
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        internal void ToPSObjectForRemoting(PSObject dest)
        {
            RemotingEncoder.AddNoteProperty(dest, "ScriptExtent_File", () => File);
            RemotingEncoder.AddNoteProperty(dest, "ScriptExtent_StartLineNumber", () => StartLineNumber);
            RemotingEncoder.AddNoteProperty(dest, "ScriptExtent_StartColumnNumber", () => StartColumnNumber);
            RemotingEncoder.AddNoteProperty(dest, "ScriptExtent_EndLineNumber", () => EndLineNumber);
            RemotingEncoder.AddNoteProperty(dest, "ScriptExtent_EndColumnNumber", () => EndColumnNumber);
        }

        private void PopulateFromSerializedInfo(PSObject serializedScriptExtent)
        {
            string file = RemotingDecoder.GetPropertyValue<string>(serializedScriptExtent, "ScriptExtent_File");
            int startLineNumber = RemotingDecoder.GetPropertyValue<int>(serializedScriptExtent, "ScriptExtent_StartLineNumber");
            int startColumnNumber = RemotingDecoder.GetPropertyValue<int>(serializedScriptExtent, "ScriptExtent_StartColumnNumber");
            int endLineNumber = RemotingDecoder.GetPropertyValue<int>(serializedScriptExtent, "ScriptExtent_EndLineNumber");
            int endColumnNumber = RemotingDecoder.GetPropertyValue<int>(serializedScriptExtent, "ScriptExtent_EndColumnNumber");

            ScriptPosition startPosition = new ScriptPosition(file, startLineNumber, startColumnNumber, null);
            ScriptPosition endPosition = new ScriptPosition(file, endLineNumber, endColumnNumber, null);

            _startPosition = startPosition;
            _endPosition = endPosition;
        }

        internal static ScriptExtent FromPSObjectForRemoting(PSObject serializedScriptExtent)
        {
            ScriptExtent extent = new ScriptExtent();
            extent.PopulateFromSerializedInfo(serializedScriptExtent);
            return extent;
        }
    }
}
