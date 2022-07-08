/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Microsoft.PowerShell
{
    public partial class PSConsoleReadLine
    {
        [DebuggerDisplay("{_line}")]
        class HistoryItem
        {
            public string _line;
            public List<EditItem> _edits;
            public int _undoEditIndex;
            public bool _saved;
            public bool _fromDifferentLiveSession;
        }

        // History state
        private HistoryQueue<HistoryItem> _history;
        private Dictionary<string, int> _hashedHistory;
        private int _currentHistoryIndex;
        private int _getNextHistoryIndex;
        private int _searchHistoryCommandCount;
        private int _recallHistoryCommandCount;
        private string _searchHistoryPrefix;
        // When cycling through history, the current line (not yet added to history)
        // is saved here so it can be restored.
        private readonly HistoryItem _savedCurrentLine;

        private Mutex _historyFileMutex;
        private long _historyFileLastSavedSize;

        private const string _forwardISearchPrompt = "fwd-i-search: ";
        private const string _backwardISearchPrompt = "bck-i-search: ";
        private const string _failedForwardISearchPrompt = "failed-fwd-i-search: ";
        private const string _failedBackwardISearchPrompt = "failed-bck-i-search: ";

        private string MaybeAddToHistory(string result, List<EditItem> edits, int undoEditIndex, bool readingHistoryFile, bool fromDifferentSession)
        {
            bool addToHistory = !string.IsNullOrWhiteSpace(result) && ((Options.AddToHistoryHandler == null) || Options.AddToHistoryHandler(result));
            if (addToHistory)
            {
                _history.Enqueue(new HistoryItem
                {
                    _line = result,
                    _edits = edits,
                    _undoEditIndex = undoEditIndex,
                    _saved = readingHistoryFile,
                    _fromDifferentLiveSession = fromDifferentSession,
                });
                _currentHistoryIndex = _history.Count;

                if (_options.HistorySaveStyle == HistorySaveStyle.SaveIncrementally && !readingHistoryFile)
                {
                    IncrementalHistoryWrite();
                }
            }

            // Clear the saved line unless we used AcceptAndGetNext in which
            // case we're really still in middle of history and might want
            // to recall the saved line.
            if (_getNextHistoryIndex == 0)
            {
                _savedCurrentLine._line = null;
                _savedCurrentLine._edits = null;
                _savedCurrentLine._undoEditIndex = 0;
            }
            return result;
        }

        private string GetHistorySaveFileMutexName()
        {
            // Return a reasonably unique name - it's not too important as there will rarely
            // be any contention.
            return "PSReadlineHistoryFile_" + _options.HistorySavePath.GetHashCode();
        }

        private void IncrementalHistoryWrite()
        {
            var i = _currentHistoryIndex - 1;
            while (i >= 0)
            {
                if (_history[i]._saved)
                {
                    break;
                }
                i -= 1;
            }

            WriteHistoryRange(i + 1, _history.Count - 1, File.AppendText);
        }

        private void SaveHistoryAtExit()
        {
            WriteHistoryRange(0, _history.Count - 1, File.CreateText);
        }


        private int historyErrorReportedCount;
        private void ReportHistoryFileError(Exception e)
        {
            if (historyErrorReportedCount == 2)
                return;

            historyErrorReportedCount += 1;
            var fgColor = Console.ForegroundColor;
            var bgColor = Console.BackgroundColor;
            Console.ForegroundColor = Options.ErrorForegroundColor;
            Console.WriteLine(PSReadLineResources.HistoryFileErrorMessage, Options.HistorySavePath, e.Message);
            if (historyErrorReportedCount == 2)
            {
                Console.WriteLine(PSReadLineResources.HistoryFileErrorFinalMessage);
            }
            Console.ForegroundColor = fgColor;
            Console.BackgroundColor = bgColor;
        }

        private bool WithHistoryFileMutexDo(int timeout, Action action)
        {
            int retryCount = 0;
            do
            {
                try
                {
                    if (_historyFileMutex.WaitOne(timeout))
                    {
                        try
                        {
                            action();
                        }
                        catch (UnauthorizedAccessException uae)
                        {
                            ReportHistoryFileError(uae);
                            return false;
                        }
                        catch (IOException ioe)
                        {
                            ReportHistoryFileError(ioe);
                            return false;
                        }
                        finally
                        {
                            _historyFileMutex.ReleaseMutex();
                        }
                    }
                }
                catch (AbandonedMutexException)
                {
                    retryCount += 1;
                }
            } while (retryCount > 0 && retryCount < 3);

            // No errors to report, so consider it a success even if we timed out on the mutex.
            return true;
        }

        private void WriteHistoryRange(int start, int end, Func<string, StreamWriter> fileOpener)
        {
            WithHistoryFileMutexDo(100, () =>
            {
                if (!MaybeReadHistoryFile())
                    return;

                bool retry = true;
                retry_after_creating_directory:
                try
                {
                    using (var file = fileOpener(Options.HistorySavePath))
                    {
                        for (var i = start; i <= end; i++)
                        {
                            _history[i]._saved = true;
                            var line = _history[i]._line.Replace("\n", "`\n");
                            file.WriteLine(line);
                        }
                    }
                    var fileInfo = new FileInfo(Options.HistorySavePath);
                    _historyFileLastSavedSize = fileInfo.Length;
                }
                catch (DirectoryNotFoundException)
                {
                    // Try making the directory, but just once
                    if (retry)
                    {
                        retry = false;
                        Directory.CreateDirectory(Path.GetDirectoryName(Options.HistorySavePath));
                        goto retry_after_creating_directory;
                    }
                }
            });
        }

        private bool MaybeReadHistoryFile()
        {
            if (Options.HistorySaveStyle == HistorySaveStyle.SaveIncrementally)
            {
                return WithHistoryFileMutexDo(1000, () =>
                {
                    var fileInfo = new FileInfo(Options.HistorySavePath);
                    if (fileInfo.Exists && fileInfo.Length != _historyFileLastSavedSize)
                    {
                        var historyLines = new List<string>();
                        using (var fs = new FileStream(Options.HistorySavePath, FileMode.Open))
                        using (var sr = new StreamReader(fs))
                        {
                            fs.Seek(_historyFileLastSavedSize, SeekOrigin.Begin);

                            while (!sr.EndOfStream)
                            {
                                historyLines.Add(sr.ReadLine());
                            }
                        }
                        UpdateHistoryFromFile(historyLines, fromDifferentSession: true);

                        _historyFileLastSavedSize = fileInfo.Length;
                    }
                });
            }

            // true means no errors, not that we actually read the file
            return true;
        }

        private void ReadHistoryFile()
        {
            WithHistoryFileMutexDo(1000, () =>
            {
                if (!File.Exists(Options.HistorySavePath))
                {
                    return;
                }

                var historyLines = File.ReadAllLines(Options.HistorySavePath);
                UpdateHistoryFromFile(historyLines, fromDifferentSession: false);
                var fileInfo = new FileInfo(Options.HistorySavePath);
                _historyFileLastSavedSize = fileInfo.Length;
            });
        }

        void UpdateHistoryFromFile(IEnumerable<string> historyLines, bool fromDifferentSession)
        {
            var sb = new StringBuilder();
            foreach (var line in historyLines)
            {
                if (line.EndsWith("`", StringComparison.Ordinal))
                {
                    sb.Append(line, 0, line.Length - 1);
                    sb.Append('\n');
                }
                else if (sb.Length > 0)
                {
                    sb.Append(line);
                    var l = sb.ToString();
                    var editItems = new List<EditItem> {EditItemInsertString.Create(l, 0)};
                    MaybeAddToHistory(l, editItems, 1, /*readingHistoryFile*/ true, fromDifferentSession);
                    sb.Clear();
                }
                else
                {
                    var editItems = new List<EditItem> {EditItemInsertString.Create(line, 0)};
                    MaybeAddToHistory(line, editItems, 1, /*readingHistoryFile*/ true, fromDifferentSession);
                }
            }
        }

        /// <summary>
        /// Add a command to the history - typically used to restore
        /// history from a previous session.
        /// </summary>
        public static void AddToHistory(string command)
        {
            command = command.Replace("\r\n", "\n");
            var editItems = new List<EditItem> {EditItemInsertString.Create(command, 0)};
            _singleton.MaybeAddToHistory(command, editItems, 1, readingHistoryFile: false, fromDifferentSession: false);
        }

        /// <summary>
        /// Clears history in PSReadline.  This does not affect PowerShell history.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void ClearHistory(ConsoleKeyInfo? key = null, object arg = null)
        {
            if (_singleton._history != null)
            {
                _singleton._history.Clear();
            }
            _singleton._currentHistoryIndex = 0;
        }

        private void UpdateFromHistory(bool moveCursor)
        {
            string line;
            if (_currentHistoryIndex == _history.Count)
            {
                line = _savedCurrentLine._line;
                _edits = _savedCurrentLine._edits;
                _undoEditIndex = _savedCurrentLine._undoEditIndex;
            }
            else
            {
                line = _history[_currentHistoryIndex]._line;
                _edits = new List<EditItem>(_history[_currentHistoryIndex]._edits);
                _undoEditIndex = _history[_currentHistoryIndex]._undoEditIndex;
            }
            _buffer.Clear();
            _buffer.Append(line);
            if (moveCursor)
            {
                _current = _options.EditMode == EditMode.Vi ? 0 : Math.Max(0, _buffer.Length + ViEndOfLineFactor);
            }
            else if (_current > _buffer.Length)
            {
                _current = Math.Max(0, _buffer.Length + ViEndOfLineFactor);
            }
            Render();
        }

        private void SaveCurrentLine()
        {
            // We're called before any history operation - so it's convenient
            // to check if we need to load history from another sessions now.
            MaybeReadHistoryFile();

            if (_savedCurrentLine._line == null)
            {
                _savedCurrentLine._line = _buffer.ToString();
                _savedCurrentLine._edits = _edits;
                _savedCurrentLine._undoEditIndex = _undoEditIndex;
            }
        }

        private void HistoryRecall(int direction)
        {
            if (_recallHistoryCommandCount == 0 && LineIsMultiLine())
            {
                MoveToLine(direction);
                return;
            }

            if (Options.HistoryNoDuplicates && _recallHistoryCommandCount == 0)
            {
                _hashedHistory = new Dictionary<string, int>();
            }

            int count = Math.Abs(direction);
            direction = direction < 0 ? -1 : +1;
            int newHistoryIndex = _currentHistoryIndex;
            while (count > 0)
            {
                newHistoryIndex += direction;
                if (newHistoryIndex < 0 || newHistoryIndex >= _history.Count)
                {
                    break;
                }

                if (_history[newHistoryIndex]._fromDifferentLiveSession)
                {
                    continue;
                }

                if (Options.HistoryNoDuplicates)
                {
                    var line = _history[newHistoryIndex]._line;
                    int index;
                    if (!_hashedHistory.TryGetValue(line, out index))
                    {
                        _hashedHistory.Add(line, newHistoryIndex);
                        --count;
                    }
                    else if (newHistoryIndex == index)
                    {
                        --count;
                    }
                }
                else
                {
                    --count;
                }
            }
            _recallHistoryCommandCount += 1;
            if (newHistoryIndex >= 0 && newHistoryIndex <= _history.Count)
            {
                _currentHistoryIndex = newHistoryIndex;
                UpdateFromHistory(moveCursor: true);
            }
        }

        /// <summary>
        /// Replace the current input with the 'previous' item from PSReadline history.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void PreviousHistory(ConsoleKeyInfo? key = null, object arg = null)
        {
            int numericArg;
            TryGetArgAsInt(arg, out numericArg, -1);
            if (numericArg > 0)
            {
                numericArg = -numericArg;
            }

            _singleton.SaveCurrentLine();
            _singleton.HistoryRecall(numericArg);
        }

        /// <summary>
        /// Replace the current input with the 'next' item from PSReadline history.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void NextHistory(ConsoleKeyInfo? key = null, object arg = null)
        {
            int numericArg;
            TryGetArgAsInt(arg, out numericArg, +1);

            _singleton.SaveCurrentLine();
            _singleton.HistoryRecall(numericArg);
        }

        private void HistorySearch(int direction)
        {
            if (_searchHistoryCommandCount == 0)
            {
                if (LineIsMultiLine())
                {
                    MoveToLine(direction);
                    return;
                }

                _searchHistoryPrefix = _buffer.ToString(0, _current);
                _emphasisStart = 0;
                _emphasisLength = _current;
                if (Options.HistoryNoDuplicates)
                {
                    _hashedHistory = new Dictionary<string, int>();
                }
            }
            _searchHistoryCommandCount += 1;

            int count = Math.Abs(direction);
            direction = direction < 0 ? -1 : +1;
            int newHistoryIndex = _currentHistoryIndex;
            while (count > 0)
            {
                newHistoryIndex += direction;
                if (newHistoryIndex < 0 || newHistoryIndex >= _history.Count)
                {
                    break;
                }

                if (_history[newHistoryIndex]._fromDifferentLiveSession && _searchHistoryPrefix.Length == 0)
                {
                    continue;
                }

                var line = newHistoryIndex == _history.Count ? _savedCurrentLine._line : _history[newHistoryIndex]._line;
                if (line.StartsWith(_searchHistoryPrefix, Options.HistoryStringComparison))
                {
                    if (Options.HistoryNoDuplicates)
                    {
                        int index;
                        if (!_hashedHistory.TryGetValue(line, out index))
                        {
                            _hashedHistory.Add(line, newHistoryIndex);
                            --count;
                        }
                        else if (index == newHistoryIndex)
                        {
                            --count;
                        }
                    }
                    else
                    {
                        --count;
                    }
                }
            }

            if (newHistoryIndex >= 0 && newHistoryIndex <= _history.Count)
            {
                _currentHistoryIndex = newHistoryIndex;
                UpdateFromHistory(moveCursor: true);
            }
        }

        /// <summary>
        /// Move to the first item in the history.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void BeginningOfHistory(ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton.SaveCurrentLine();
            _singleton._currentHistoryIndex = 0;
            _singleton.UpdateFromHistory(moveCursor: true);
        }

        /// <summary>
        /// Move to the last item (the current input) in the history.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void EndOfHistory(ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton._currentHistoryIndex = _singleton._history.Count;
            _singleton.UpdateFromHistory(moveCursor: true);
        }

        /// <summary>
        /// Replace the current input with the 'previous' item from PSReadline history
        /// that matches the characters between the start and the input and the cursor.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void HistorySearchBackward(ConsoleKeyInfo? key = null, object arg = null)
        {
            int numericArg;
            TryGetArgAsInt(arg, out numericArg, -1);
            if (numericArg > 0)
            {
                numericArg = -numericArg;
            }

            _singleton.SaveCurrentLine();
            _singleton.HistorySearch(numericArg);
        }

        /// <summary>
        /// Replace the current input with the 'next' item from PSReadline history
        /// that matches the characters between the start and the input and the cursor.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void HistorySearchForward(ConsoleKeyInfo? key = null, object arg = null)
        {
            int numericArg;
            TryGetArgAsInt(arg, out numericArg, +1);

            _singleton.SaveCurrentLine();
            _singleton.HistorySearch(numericArg);
        }

        private void UpdateHistoryDuringInteractiveSearch(string toMatch, int direction, ref int searchFromPoint)
        {
            searchFromPoint += direction;
            for (; searchFromPoint >= 0 && searchFromPoint < _history.Count; searchFromPoint += direction)
            {
                var line = _history[searchFromPoint]._line;
                var startIndex = line.IndexOf(toMatch, Options.HistoryStringComparison);
                if (startIndex >= 0)
                {
                    if (Options.HistoryNoDuplicates)
                    {
                        int index;
                        if (!_hashedHistory.TryGetValue(line, out index))
                        {
                            _hashedHistory.Add(line, searchFromPoint);
                        }
                        else if (index != searchFromPoint)
                        {
                            continue;
                        }
                    }
                    _statusLinePrompt = direction > 0 ? _forwardISearchPrompt : _backwardISearchPrompt;
                    _current = startIndex;
                    _emphasisStart = startIndex;
                    _emphasisLength = toMatch.Length;
                    _currentHistoryIndex = searchFromPoint;
                    UpdateFromHistory(moveCursor: Options.HistorySearchCursorMovesToEnd);
                    return;
                }
            }

            // Make sure we're never more than 1 away from being in range so if they
            // reverse direction, the first time they reverse they are back in range.
            if (searchFromPoint < 0)
                searchFromPoint = -1;
            else if (searchFromPoint >= _history.Count)
                searchFromPoint = _history.Count;

            _emphasisStart = -1;
            _emphasisLength = 0;
            _statusLinePrompt = direction > 0 ? _failedForwardISearchPrompt : _failedBackwardISearchPrompt;
            Render();
        }

        private void InteractiveHistorySearchLoop(int direction)
        {
            var searchFromPoint = _currentHistoryIndex;
            var searchPositions = new Stack<int>();
            searchPositions.Push(_currentHistoryIndex);

            if (Options.HistoryNoDuplicates)
            {
                _hashedHistory = new Dictionary<string, int>();
            }

            var toMatch = new StringBuilder(64);
            while (true)
            {
                var key = ReadKey();
                KeyHandler handler;
                _dispatchTable.TryGetValue(key, out handler);
                var function = handler != null ? handler.Action : null;
                if (function == ReverseSearchHistory)
                {
                    UpdateHistoryDuringInteractiveSearch(toMatch.ToString(), -1, ref searchFromPoint);
                }
                else if (function == ForwardSearchHistory)
                {
                    UpdateHistoryDuringInteractiveSearch(toMatch.ToString(), +1, ref searchFromPoint);
                }
                else if (function == BackwardDeleteChar || key == Keys.Backspace || key == Keys.CtrlH)
                {
                    if (toMatch.Length > 0)
                    {
                        toMatch.Remove(toMatch.Length - 1, 1);
                        _statusBuffer.Remove(_statusBuffer.Length - 2, 1);
                        searchPositions.Pop();
                        searchFromPoint = _currentHistoryIndex = searchPositions.Peek();
                        UpdateFromHistory(moveCursor: Options.HistorySearchCursorMovesToEnd);

                        if (_hashedHistory != null)
                        {
                            // Remove any entries with index < searchFromPoint because
                            // we are starting the search from this new index - we always
                            // want to find the latest entry that matches the search string
                            foreach (var pair in _hashedHistory.ToArray())
                            {
                                if (pair.Value < searchFromPoint)
                                {
                                    _hashedHistory.Remove(pair.Key);
                                }
                            }
                        }

                        // Prompt may need to have 'failed-' removed.
                        var toMatchStr = toMatch.ToString();
                        var startIndex = _buffer.ToString().IndexOf(toMatchStr, Options.HistoryStringComparison);
                        if (startIndex >= 0)
                        {
                            _statusLinePrompt = direction > 0 ? _forwardISearchPrompt : _backwardISearchPrompt;
                            _current = startIndex;
                            _emphasisStart = startIndex;
                            _emphasisLength = toMatch.Length;
                            Render();
                        }
                    }
                    else
                    {
                        Ding();
                    }
                }
                else if (key == Keys.Escape)
                {
                    // End search
                    break;
                }
                else if (function == Abort)
                {
                    // Abort search
                    EndOfHistory();
                    break;
                }
                else if (EndInteractiveHistorySearch(key, function))
                {
                    PrependQueuedKeys(key);
                    break;
                }
                else
                {
                    toMatch.Append(key.KeyChar);
                    _statusBuffer.Insert(_statusBuffer.Length - 1, key.KeyChar);

                    var toMatchStr = toMatch.ToString();
                    var startIndex = _buffer.ToString().IndexOf(toMatchStr, Options.HistoryStringComparison);
                    if (startIndex < 0)
                    {
                        UpdateHistoryDuringInteractiveSearch(toMatchStr, direction, ref searchFromPoint);
                    }
                    else
                    {
                        _current = startIndex;
                        _emphasisStart = startIndex;
                        _emphasisLength = toMatch.Length;
                        Render();
                    }
                    searchPositions.Push(_currentHistoryIndex);
                }
            }
        }

        private static bool EndInteractiveHistorySearch(ConsoleKeyInfo key, Action<ConsoleKeyInfo?, object> function)
        {
            // Keys < ' ' are control characters
            if (key.KeyChar < ' ')
            {
                return true;
            }

            if ((key.Modifiers & (ConsoleModifiers.Alt | ConsoleModifiers.Control)) != 0)
            {
                return true;
            }

            return false;
        }

        private void InteractiveHistorySearch(int direction)
        {
            SaveCurrentLine();

            // Add a status line that will contain the search prompt and string
            _statusLinePrompt = direction > 0 ? _forwardISearchPrompt : _backwardISearchPrompt;
            _statusBuffer.Append("_");

            Render(); // Render prompt
            InteractiveHistorySearchLoop(direction);

            _hashedHistory = null;
            _currentHistoryIndex = _history.Count;

            _emphasisStart = -1;
            _emphasisLength = 0;

            // Remove our status line, this will render
            ClearStatusMessage(render: true);
        }

        /// <summary>
        /// Perform an incremental forward search through history
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void ForwardSearchHistory(ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton.InteractiveHistorySearch(+1);
        }

        /// <summary>
        /// Perform an incremental backward search through history
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void ReverseSearchHistory(ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton.InteractiveHistorySearch(-1);
        }
    }
}
