/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Reflection;
using System.Threading;
using Microsoft.PowerShell.Internal;

namespace Microsoft.PowerShell
{
    public partial class PSConsoleReadLine
    {
        private readonly PSConsoleReadlineOptions _options;
        private PSConsoleReadlineOptions Options
        {
            get { return _options; }
        }

        private void SetOptionsInternal(SetPSReadlineOption options)
        {
            if (options.ContinuationPrompt != null)
            {
                Options.ContinuationPrompt = options.ContinuationPrompt;
            }
            if (options._continuationPromptForegroundColor.HasValue)
            {
                Options.ContinuationPromptForegroundColor = options.ContinuationPromptForegroundColor;
            }
            if (options._continuationPromptBackgroundColor.HasValue)
            {
                Options.ContinuationPromptBackgroundColor = options.ContinuationPromptBackgroundColor;
            }
            if (options._emphasisBackgroundColor.HasValue)
            {
                Options.EmphasisBackgroundColor = options.EmphasisBackgroundColor;
            }
            if (options._emphasisForegroundColor.HasValue)
            {
                Options.EmphasisForegroundColor = options.EmphasisForegroundColor;
            }
            if (options._errorBackgroundColor.HasValue)
            {
                Options.ErrorBackgroundColor = options.ErrorBackgroundColor;
            }
            if (options._errorForegroundColor.HasValue)
            {
                Options.ErrorForegroundColor = options.ErrorForegroundColor;
            }
            if (options._historyNoDuplicates.HasValue)
            {
                Options.HistoryNoDuplicates = options.HistoryNoDuplicates;
            }
            if (options._historySearchCursorMovesToEnd.HasValue)
            {
                Options.HistorySearchCursorMovesToEnd = options.HistorySearchCursorMovesToEnd;
            }
            if (options._addToHistoryHandlerSpecified)
            {
                Options.AddToHistoryHandler = options.AddToHistoryHandler;
            }
            if (options._commandValidationHandlerSpecified)
            {
                Options.CommandValidationHandler = options.CommandValidationHandler;
            }
            if (options._maximumHistoryCount.HasValue)
            {
                Options.MaximumHistoryCount = options.MaximumHistoryCount;
                if (_history != null)
                {
                    var newHistory = new HistoryQueue<HistoryItem>(Options.MaximumHistoryCount);
                    while (_history.Count > Options.MaximumHistoryCount)
                    {
                        _history.Dequeue();
                    }
                    while (_history.Count > 0)
                    {
                        newHistory.Enqueue(_history.Dequeue());
                    }
                    _history = newHistory;
                    _currentHistoryIndex = _history.Count;
                }
            }
            if (options._maximumKillRingCount.HasValue)
            {
                Options.MaximumKillRingCount = options.MaximumKillRingCount;
                // TODO - make _killRing smaller
            }
            if (options._editMode.HasValue)
            {
                Options.EditMode = options.EditMode;

                // Switching/resetting modes - clear out chord dispatch table
                _chordDispatchTable.Clear();

                switch (options._editMode)
                {
                case EditMode.Emacs:
                    SetDefaultEmacsBindings();
                    break;
                case EditMode.Vi:
                    SetDefaultViBindings();
                    break;
                case EditMode.Windows:
                    SetDefaultWindowsBindings();
                    break;
                }
            }
            if (options._showToolTips.HasValue)
            {
                Options.ShowToolTips = options.ShowToolTips;
            }
            if (options._extraPromptLineCount.HasValue)
            {
                Options.ExtraPromptLineCount = options.ExtraPromptLineCount;
            }
            if (options._dingTone.HasValue)
            {
                Options.DingTone = options.DingTone;
            }
            if (options._dingDuration.HasValue)
            {
                Options.DingDuration = options.DingDuration;
            }
            if (options._bellStyle.HasValue)
            {
                Options.BellStyle = options.BellStyle;
            }
            if (options._completionQueryItems.HasValue)
            {
                Options.CompletionQueryItems = options.CompletionQueryItems;
            }
            if (options.WordDelimiters != null)
            {
                Options.WordDelimiters = options.WordDelimiters;
            }
            if (options._historySearchCaseSensitive.HasValue)
            {
                Options.HistorySearchCaseSensitive = options.HistorySearchCaseSensitive;
            }
            if (options._historySaveStyle.HasValue)
            {
                Options.HistorySaveStyle = options.HistorySaveStyle;
            }
            #region vi
            if (options._viModeIndicator.HasValue)
            {
                Options.ViModeIndicator = options.ViModeIndicator;
            }
            #endregion
            if (options.HistorySavePath != null)
            {
                Options.HistorySavePath = options.HistorySavePath;
                if (_historyFileMutex != null)
                {
                    _historyFileMutex.Dispose();
                }
                _historyFileMutex = new Mutex(false, GetHistorySaveFileMutexName());
                _historyFileLastSavedSize = 0;
            }
            if (options.ResetTokenColors)
            {
                Options.ResetColors();
            }
            if (options._tokenKind.HasValue)
            {
                if (options._foregroundColor.HasValue)
                {
                    Options.SetForegroundColor(options.TokenKind, options.ForegroundColor);
                }
                if (options._backgroundColor.HasValue)
                {
                    Options.SetBackgroundColor(options.TokenKind, options.BackgroundColor);
                }
            }
        }

        private void SetKeyHandlerInternal(string[] keys, Action<ConsoleKeyInfo?, object> handler, string briefDescription, string longDescription, ScriptBlock scriptBlock)
        {
            foreach (var key in keys)
            {
                var chord = ConsoleKeyChordConverter.Convert(key);
                if (chord.Length == 1)
                {
                    _dispatchTable[chord[0]] = MakeKeyHandler(handler, briefDescription, longDescription, scriptBlock);
                }
                else
                {
                    _dispatchTable[chord[0]] = MakeKeyHandler(Chord, "ChordFirstKey");
                    Dictionary<ConsoleKeyInfo, KeyHandler> secondDispatchTable;
                    if (!_chordDispatchTable.TryGetValue(chord[0], out secondDispatchTable))
                    {
                        secondDispatchTable = new Dictionary<ConsoleKeyInfo, KeyHandler>();
                        _chordDispatchTable[chord[0]] = secondDispatchTable;
                    }
                    secondDispatchTable[chord[1]] = MakeKeyHandler(handler, briefDescription, longDescription, scriptBlock);
                }
            }
        }

        private void RemoveKeyHandlerInternal(string[] keys)
        {
            foreach (var key in keys)
            {
                var chord = ConsoleKeyChordConverter.Convert(key);
                if (chord.Length == 1)
                {
                    _dispatchTable.Remove(chord[0]);
                }
                else
                {
                    Dictionary<ConsoleKeyInfo, KeyHandler> secondDispatchTable;
                    if (_chordDispatchTable.TryGetValue(chord[0], out secondDispatchTable))
                    {
                        secondDispatchTable.Remove(chord[1]);
                        if (secondDispatchTable.Count == 0)
                        {
                            _dispatchTable.Remove(chord[0]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Helper function for the Set-PSReadlineOption cmdlet.
        /// </summary>
        public static void SetOptions(SetPSReadlineOption options)
        {
            _singleton.SetOptionsInternal(options);
        }

        /// <summary>
        /// Helper function for the Get-PSReadlineOption cmdlet.
        /// </summary>
        public static PSConsoleReadlineOptions GetOptions()
        {
            // Should we copy?  It doesn't matter much, everything can be tweaked from
            // the cmdlet anyway.
            return _singleton._options;
        }

        class CustomHandlerException : Exception
        {
            internal CustomHandlerException(Exception innerException)
                : base("", innerException)
            {
            }
        }

        /// <summary>
        /// Helper function for the Set-PSReadlineKeyHandler cmdlet.
        /// </summary>
        public static void SetKeyHandler(string[] key, ScriptBlock scriptBlock, string briefDescription, string longDescription)
        {
            Action<ConsoleKeyInfo?, object> handler = (k, arg) =>
            {
                try
                {
                    scriptBlock.Invoke(k, arg);
                }
                catch (Exception e)
                {
                    throw new CustomHandlerException(e);
                }
            };

            _singleton.SetKeyHandlerInternal(key, handler, briefDescription, longDescription, scriptBlock);
        }

        /// <summary>
        /// Helper function for the Set-PSReadlineKeyHandler cmdlet.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public static void SetKeyHandler(string[] key, Action<ConsoleKeyInfo?, object> handler, string briefDescription, string longDescription)
        {
            _singleton.SetKeyHandlerInternal(key, handler, briefDescription, longDescription, null);
        }

        /// <summary>
        /// Helper function for the Remove-PSReadlineKeyHandler cmdlet.
        /// </summary>
        /// <param name="key"></param>
        public static void RemoveKeyHandler(string[] key)
        {
            _singleton.RemoveKeyHandlerInternal(key);
        }

        /// <summary>
        /// Helper function for the Get-PSReadlineKeyHandler cmdlet.
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static IEnumerable<Microsoft.PowerShell.KeyHandler> GetKeyHandlers(bool includeBound = true, bool includeUnbound = false)
        {
            var boundFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in _singleton._dispatchTable)
            {
                if (entry.Value.BriefDescription == "Ignore"
                    || entry.Value.BriefDescription == "ChordFirstKey")
                {
                    continue;
                }
                boundFunctions.Add(entry.Value.BriefDescription);
                if (includeBound)
                {
                    yield return new Microsoft.PowerShell.KeyHandler
                    {
                        Key = entry.Key.ToGestureString(),
                        Function = entry.Value.BriefDescription,
                        Description = entry.Value.LongDescription,
                    };
                }
            }

            // Added to support vi command mode mappings
            if (_singleton._options.EditMode == EditMode.Vi)
            {
                foreach (var entry in _viCmdKeyMap)
                {
                    if (entry.Value.BriefDescription == "Ignore"
                        || entry.Value.BriefDescription == "ChordFirstKey")
                    {
                        continue;
                    }
                    boundFunctions.Add(entry.Value.BriefDescription);
                    if (includeBound)
                    {
                        yield return new Microsoft.PowerShell.KeyHandler
                        {
                            Key = "<" + entry.Key.ToGestureString() + ">",
                            Function = entry.Value.BriefDescription,
                            Description = entry.Value.LongDescription,
                        };
                    }
                }
            }

            foreach( var entry in _singleton._chordDispatchTable )
            {
                foreach( var secondEntry in entry.Value )
                {
                    boundFunctions.Add( secondEntry.Value.BriefDescription );
                    if (includeBound)
                    {
                        yield return new Microsoft.PowerShell.KeyHandler
                        {
                            Key = entry.Key.ToGestureString() + "," + secondEntry.Key.ToGestureString(),
                            Function = secondEntry.Value.BriefDescription,
                            Description = secondEntry.Value.LongDescription,
                        };
                    }
                }
            }

            // Added to support vi command mode chorded mappings
            if (_singleton._options.EditMode == EditMode.Vi)
            {
                foreach (var entry in _viCmdChordTable)
                {
                    foreach (var secondEntry in entry.Value)
                    {
                        if (secondEntry.Value.BriefDescription == "Ignore")
                        {
                            continue;
                        }
                        boundFunctions.Add(secondEntry.Value.BriefDescription);
                        if (includeBound)
                        {
                            yield return new Microsoft.PowerShell.KeyHandler
                            {
                                Key = "<" + entry.Key.ToGestureString() + "," + secondEntry.Key.ToGestureString() + ">",
                                Function = secondEntry.Value.BriefDescription,
                                Description = secondEntry.Value.LongDescription
                            };
                        }
                    }
                }
            }

            if (includeUnbound)
            {
                // SelfInsert isn't really unbound, but we don't want UI to show it that way
                boundFunctions.Add("SelfInsert");

                var methods = typeof (PSConsoleReadLine).GetMethods(BindingFlags.Public | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length != 2 ||
                        parameters[0].ParameterType != typeof (ConsoleKeyInfo?) ||
                        parameters[1].ParameterType != typeof (object))
                    {
                        continue;
                    }

                    if (!boundFunctions.Contains(method.Name))
                    {
                        yield return new Microsoft.PowerShell.KeyHandler
                        {
                            Key = "Unbound",
                            Function = method.Name,
                            Description = null,
                        };
                    }
                }
            }
        }
    }
}
