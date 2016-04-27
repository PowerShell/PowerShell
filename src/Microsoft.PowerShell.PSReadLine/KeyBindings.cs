/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Text;
using Microsoft.PowerShell.Internal;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// The class is used as the output type for the cmdlet Get-PSReadlineKeyHandler
    /// </summary>
    public class KeyHandler
    {
        /// <summary>
        /// The key that is bound or unbound.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The name of the function that a key is bound to, if any.
        /// </summary>
        public string Function { get; set; }

        /// <summary>
        /// A short description of the behavior of the function.
        /// </summary>
        public string Description
        {
            get
            {
                var result = _description;
                if (string.IsNullOrWhiteSpace(result))
                    result = PSReadLineResources.ResourceManager.GetString(Function + "Description");
                if (string.IsNullOrWhiteSpace(result))
                    result = Function;
                return result;
            }
            set { _description = value; }
        }
        private string _description;
    }

    public partial class PSConsoleReadLine
    {
        class KeyHandler
        {
            // Each key handler will be passed 2 arguments.  Most will ignore these arguments,
            // but having a consistent signature greatly simplifies dispatch.  Defaults
            // should be included on all handlers that ignore their parameters so they
            // can be called from PowerShell without passing anything.
            //
            // The first arugment is the key that caused the action to be called
            // (the second key when it's a 2 key chord).  The default is null (it's nullable)
            // because PowerShell can't handle default(ConsoleKeyInfo) as a default.
            // Most actions will ignore this argument.
            //
            // The second argument is an arbitrary object.  It will usually be either a number
            // (e.g. as a repeat count) or a string.  Most actions will ignore this argument.
            public Action<ConsoleKeyInfo?, object> Action;
            public string BriefDescription;
            public string LongDescription
            {
                get
                {
                    return _longDescription ??
                           (_longDescription =
                               PSReadLineResources.ResourceManager.GetString(BriefDescription + "Description"));
                }
                set { _longDescription = value; }
            }
            private string _longDescription;
            public ScriptBlock ScriptBlock;
        }

        internal class ConsoleKeyInfoComparer : IEqualityComparer<ConsoleKeyInfo>
        {
            public bool Equals(ConsoleKeyInfo x, ConsoleKeyInfo y)
            {
                return x.Key == y.Key && x.KeyChar == y.KeyChar && x.Modifiers == y.Modifiers;
            }

            public int GetHashCode(ConsoleKeyInfo obj)
            {
                return obj.GetHashCode();
            }
        }

        static KeyHandler MakeKeyHandler(Action<ConsoleKeyInfo?, object> action, string briefDescription, string longDescription = null, ScriptBlock scriptBlock = null)
        {
            return new KeyHandler
            {
                Action = action,
                BriefDescription = briefDescription,
                LongDescription = longDescription,
                ScriptBlock = scriptBlock,
            };
        }

        private Dictionary<ConsoleKeyInfo, KeyHandler> _dispatchTable;
        private Dictionary<ConsoleKeyInfo, Dictionary<ConsoleKeyInfo, KeyHandler>> _chordDispatchTable; 

        void SetDefaultWindowsBindings()
        {
            _dispatchTable = new Dictionary<ConsoleKeyInfo, KeyHandler>(new ConsoleKeyInfoComparer())
            {
                { Keys.Enter,                  MakeKeyHandler(AcceptLine,                "AcceptLine") },
                { Keys.ShiftEnter,             MakeKeyHandler(AddLine,                   "AddLine") },
                { Keys.CtrlEnter,              MakeKeyHandler(InsertLineAbove,           "InsertLineAbove") },
                { Keys.CtrlShiftEnter,         MakeKeyHandler(InsertLineBelow,           "InsertLineBelow") },
                { Keys.Escape,                 MakeKeyHandler(RevertLine,                "RevertLine") },
                { Keys.LeftArrow,              MakeKeyHandler(BackwardChar,              "BackwardChar") },
                { Keys.RightArrow,             MakeKeyHandler(ForwardChar,               "ForwardChar") },
                { Keys.CtrlLeftArrow,          MakeKeyHandler(BackwardWord,              "BackwardWord") },
                { Keys.CtrlRightArrow,         MakeKeyHandler(NextWord,                  "NextWord") },
                { Keys.ShiftLeftArrow,         MakeKeyHandler(SelectBackwardChar,        "SelectBackwardChar") },
                { Keys.ShiftRightArrow,        MakeKeyHandler(SelectForwardChar,         "SelectForwardChar") },
                { Keys.ShiftCtrlLeftArrow,     MakeKeyHandler(SelectBackwardWord,        "SelectBackwardWord") },
                { Keys.ShiftCtrlRightArrow,    MakeKeyHandler(SelectNextWord,            "SelectNextWord") },
                { Keys.UpArrow,                MakeKeyHandler(PreviousHistory,           "PreviousHistory") },
                { Keys.DownArrow,              MakeKeyHandler(NextHistory,               "NextHistory") },
                { Keys.Home,                   MakeKeyHandler(BeginningOfLine,           "BeginningOfLine") },
                { Keys.End,                    MakeKeyHandler(EndOfLine,                 "EndOfLine") },
                { Keys.ShiftHome,              MakeKeyHandler(SelectBackwardsLine,       "SelectBackwardsLine") },
                { Keys.ShiftEnd,               MakeKeyHandler(SelectLine,                "SelectLine") },
                { Keys.Delete,                 MakeKeyHandler(DeleteChar,                "DeleteChar") },
                { Keys.Backspace,              MakeKeyHandler(BackwardDeleteChar,        "BackwardDeleteChar") },
                { Keys.CtrlSpace,              MakeKeyHandler(MenuComplete,              "MenuComplete") },
                { Keys.Tab,                    MakeKeyHandler(TabCompleteNext,           "TabCompleteNext") },
                { Keys.ShiftTab,               MakeKeyHandler(TabCompletePrevious,       "TabCompletePrevious") },
                { Keys.VolumeDown,             MakeKeyHandler(Ignore,                    "Ignore") },
                { Keys.VolumeUp,               MakeKeyHandler(Ignore,                    "Ignore") },
                { Keys.VolumeMute,             MakeKeyHandler(Ignore,                    "Ignore") },
                { Keys.CtrlA,                  MakeKeyHandler(SelectAll,                 "SelectAll") },
                { Keys.CtrlC,                  MakeKeyHandler(CopyOrCancelLine,          "CopyOrCancelLine") },
                { Keys.CtrlShiftC,             MakeKeyHandler(Copy,                      "Copy") },
                { Keys.CtrlL,                  MakeKeyHandler(ClearScreen,               "ClearScreen") },
                { Keys.CtrlR,                  MakeKeyHandler(ReverseSearchHistory,      "ReverseSearchHistory") },
                { Keys.CtrlS,                  MakeKeyHandler(ForwardSearchHistory,      "ForwardSearchHistory") },
                { Keys.CtrlV,                  MakeKeyHandler(Paste,                     "Paste") },
                { Keys.CtrlX,                  MakeKeyHandler(Cut,                       "Cut") },
                { Keys.CtrlY,                  MakeKeyHandler(Redo,                      "Redo") },
                { Keys.CtrlZ,                  MakeKeyHandler(Undo,                      "Undo") },
                { Keys.CtrlBackspace,          MakeKeyHandler(BackwardKillWord,          "BackwardKillWord") },
                { Keys.CtrlDelete,             MakeKeyHandler(KillWord,                  "KillWord") },
                { Keys.CtrlEnd,                MakeKeyHandler(ForwardDeleteLine,         "ForwardDeleteLine") },
                { Keys.CtrlHome,               MakeKeyHandler(BackwardDeleteLine,        "BackwardDeleteLine") },
                { Keys.CtrlRBracket,           MakeKeyHandler(GotoBrace,                 "GotoBrace") },
                { Keys.CtrlAltQuestion,        MakeKeyHandler(ShowKeyBindings,           "ShowKeyBindings") },
                { Keys.AltPeriod,              MakeKeyHandler(YankLastArg,               "YankLastArg") },
                { Keys.Alt0,                   MakeKeyHandler(DigitArgument,             "DigitArgument") },
                { Keys.Alt1,                   MakeKeyHandler(DigitArgument,             "DigitArgument") },
                { Keys.Alt2,                   MakeKeyHandler(DigitArgument,             "DigitArgument") },
                { Keys.Alt3,                   MakeKeyHandler(DigitArgument,             "DigitArgument") },
                { Keys.Alt4,                   MakeKeyHandler(DigitArgument,             "DigitArgument") },
                { Keys.Alt5,                   MakeKeyHandler(DigitArgument,             "DigitArgument") },
                { Keys.Alt6,                   MakeKeyHandler(DigitArgument,             "DigitArgument") },
                { Keys.Alt7,                   MakeKeyHandler(DigitArgument,             "DigitArgument") },
                { Keys.Alt8,                   MakeKeyHandler(DigitArgument,             "DigitArgument") },
                { Keys.Alt9,                   MakeKeyHandler(DigitArgument,             "DigitArgument") },
                { Keys.AltMinus,               MakeKeyHandler(DigitArgument,             "DigitArgument") },
                { Keys.AltQuestion,            MakeKeyHandler(WhatIsKey,                 "WhatIsKey") },
                { Keys.AltF7,                  MakeKeyHandler(ClearHistory,              "ClearHistory") },
                { Keys.F3,                     MakeKeyHandler(CharacterSearch,           "CharacterSearch") },
                { Keys.ShiftF3,                MakeKeyHandler(CharacterSearchBackward,   "CharacterSearchBackward") },
                { Keys.F8,                     MakeKeyHandler(HistorySearchBackward,     "HistorySearchBackward") },
                { Keys.ShiftF8,                MakeKeyHandler(HistorySearchForward,      "HistorySearchForward") },
                { Keys.PageUp,                 MakeKeyHandler(ScrollDisplayUp,           "ScrollDisplayUp") },
                { Keys.PageDown,               MakeKeyHandler(ScrollDisplayDown,         "ScrollDisplayDown") },
                { Keys.CtrlPageUp,             MakeKeyHandler(ScrollDisplayUpLine,       "ScrollDisplayUpLine") },
                { Keys.CtrlPageDown,           MakeKeyHandler(ScrollDisplayDownLine,     "ScrollDisplayDownLine") },
            };

            _chordDispatchTable = new Dictionary<ConsoleKeyInfo, Dictionary<ConsoleKeyInfo, KeyHandler>>();
        }

        void SetDefaultEmacsBindings()
        {
            _dispatchTable = new Dictionary<ConsoleKeyInfo, KeyHandler>(new ConsoleKeyInfoComparer())
            {
                { Keys.Backspace,       MakeKeyHandler(BackwardDeleteChar,   "BackwardDeleteChar") },
                { Keys.Enter,           MakeKeyHandler(AcceptLine,           "AcceptLine") },
                { Keys.ShiftEnter,      MakeKeyHandler(AddLine,              "AddLine") },
                { Keys.LeftArrow,       MakeKeyHandler(BackwardChar,         "BackwardChar") },
                { Keys.RightArrow,      MakeKeyHandler(ForwardChar,          "ForwardChar") },
                { Keys.ShiftLeftArrow,  MakeKeyHandler(SelectBackwardChar,   "SelectBackwardChar") },
                { Keys.ShiftRightArrow, MakeKeyHandler(SelectForwardChar,    "SelectForwardChar") },
                { Keys.UpArrow,         MakeKeyHandler(PreviousHistory,      "PreviousHistory") },
                { Keys.DownArrow,       MakeKeyHandler(NextHistory,          "NextHistory") },
                { Keys.AltLess,         MakeKeyHandler(BeginningOfHistory,   "BeginningOfHistory") },
                { Keys.AltGreater,      MakeKeyHandler(EndOfHistory,         "EndOfHistory") },
                { Keys.Home,            MakeKeyHandler(BeginningOfLine,      "BeginningOfLine") },
                { Keys.End,             MakeKeyHandler(EndOfLine,            "EndOfLine") },
                { Keys.ShiftHome,       MakeKeyHandler(SelectBackwardsLine,  "SelectBackwardsLine") },
                { Keys.ShiftEnd,        MakeKeyHandler(SelectLine,           "SelectLine") },
                { Keys.Escape,          MakeKeyHandler(Chord,                "ChordFirstKey") },
                { Keys.Delete,          MakeKeyHandler(DeleteChar,           "DeleteChar") },
                { Keys.Tab,             MakeKeyHandler(Complete,             "Complete") },
                { Keys.CtrlA,           MakeKeyHandler(BeginningOfLine,      "BeginningOfLine") },
                { Keys.CtrlB,           MakeKeyHandler(BackwardChar,         "BackwardChar") },
                { Keys.CtrlC,           MakeKeyHandler(CopyOrCancelLine,     "CopyOrCancelLine") },
                { Keys.CtrlD,           MakeKeyHandler(DeleteCharOrExit,     "DeleteCharOrExit") },
                { Keys.CtrlE,           MakeKeyHandler(EndOfLine,            "EndOfLine") },
                { Keys.CtrlF,           MakeKeyHandler(ForwardChar,          "ForwardChar") },
                { Keys.CtrlG,           MakeKeyHandler(Abort,                "Abort") },
                { Keys.CtrlH,           MakeKeyHandler(BackwardDeleteChar,   "BackwardDeleteChar") },
                { Keys.CtrlL,           MakeKeyHandler(ClearScreen,          "ClearScreen") },
                { Keys.CtrlK,           MakeKeyHandler(KillLine,             "KillLine") },
                { Keys.CtrlM,           MakeKeyHandler(ValidateAndAcceptLine,"ValidateAndAcceptLine") },
                { Keys.CtrlN,           MakeKeyHandler(NextHistory,          "NextHistory") },
                { Keys.CtrlO,           MakeKeyHandler(AcceptAndGetNext,     "AcceptAndGetNext") },
                { Keys.CtrlP,           MakeKeyHandler(PreviousHistory,      "PreviousHistory") },
                { Keys.CtrlR,           MakeKeyHandler(ReverseSearchHistory, "ReverseSearchHistory") },
                { Keys.CtrlS,           MakeKeyHandler(ForwardSearchHistory, "ForwardSearchHistory") },
                { Keys.CtrlU,           MakeKeyHandler(BackwardKillLine,     "BackwardKillLine") },
                { Keys.CtrlX,           MakeKeyHandler(Chord,                "ChordFirstKey") },
                { Keys.CtrlW,           MakeKeyHandler(UnixWordRubout,       "UnixWordRubout") },
                { Keys.CtrlY,           MakeKeyHandler(Yank,                 "Yank") },
                { Keys.CtrlAt,          MakeKeyHandler(SetMark,              "SetMark") },
                { Keys.CtrlUnderbar,    MakeKeyHandler(Undo,                 "Undo") },
                { Keys.CtrlRBracket,    MakeKeyHandler(CharacterSearch,      "CharacterSearch") },
                { Keys.AltCtrlRBracket, MakeKeyHandler(CharacterSearchBackward,"CharacterSearchBackward") },
                { Keys.Alt0,            MakeKeyHandler(DigitArgument,        "DigitArgument") },
                { Keys.Alt1,            MakeKeyHandler(DigitArgument,        "DigitArgument") },
                { Keys.Alt2,            MakeKeyHandler(DigitArgument,        "DigitArgument") },
                { Keys.Alt3,            MakeKeyHandler(DigitArgument,        "DigitArgument") },
                { Keys.Alt4,            MakeKeyHandler(DigitArgument,        "DigitArgument") },
                { Keys.Alt5,            MakeKeyHandler(DigitArgument,        "DigitArgument") },
                { Keys.Alt6,            MakeKeyHandler(DigitArgument,        "DigitArgument") },
                { Keys.Alt7,            MakeKeyHandler(DigitArgument,        "DigitArgument") },
                { Keys.Alt8,            MakeKeyHandler(DigitArgument,        "DigitArgument") },
                { Keys.Alt9,            MakeKeyHandler(DigitArgument,        "DigitArgument") },
                { Keys.AltMinus,        MakeKeyHandler(DigitArgument,        "DigitArgument") },
                { Keys.AltB,            MakeKeyHandler(BackwardWord,         "BackwardWord") },
                { Keys.AltShiftB,       MakeKeyHandler(SelectBackwardWord,   "SelectBackwardWord") },
                { Keys.AltD,            MakeKeyHandler(KillWord,             "KillWord") },
                { Keys.AltF,            MakeKeyHandler(ForwardWord,          "ForwardWord") },
                { Keys.AltShiftF,       MakeKeyHandler(SelectForwardWord,    "SelectForwardWord") },
                { Keys.AltR,            MakeKeyHandler(RevertLine,           "RevertLine") },
                { Keys.AltY,            MakeKeyHandler(YankPop,              "YankPop") },
                { Keys.AltBackspace,    MakeKeyHandler(BackwardKillWord,     "BackwardKillWord") },
                { Keys.AltEquals,       MakeKeyHandler(PossibleCompletions,  "PossibleCompletions") },
                { Keys.CtrlSpace,       MakeKeyHandler(MenuComplete,         "MenuComplete") },
                { Keys.CtrlAltQuestion, MakeKeyHandler(ShowKeyBindings,      "ShowKeyBindings") },
                { Keys.AltQuestion,     MakeKeyHandler(WhatIsKey,            "WhatIsKey") },
                { Keys.AltSpace,        MakeKeyHandler(SetMark,              "SetMark") },  // useless entry here for completeness - brings up system menu on Windows
                { Keys.AltPeriod,       MakeKeyHandler(YankLastArg,          "YankLastArg") },
                { Keys.AltUnderbar,     MakeKeyHandler(YankLastArg,          "YankLastArg") },
                { Keys.AltCtrlY,        MakeKeyHandler(YankNthArg,           "YankNthArg") },
                { Keys.VolumeDown,      MakeKeyHandler(Ignore,               "Ignore") },
                { Keys.VolumeUp,        MakeKeyHandler(Ignore,               "Ignore") },
                { Keys.VolumeMute,      MakeKeyHandler(Ignore,               "Ignore") },
                { Keys.PageUp,          MakeKeyHandler(ScrollDisplayUp,      "ScrollDisplayUp") },
                { Keys.CtrlPageUp,      MakeKeyHandler(ScrollDisplayUpLine,  "ScrollDisplayUpLine") },
                { Keys.PageDown,        MakeKeyHandler(ScrollDisplayDown,    "ScrollDisplayDown") },
                { Keys.CtrlPageDown,    MakeKeyHandler(ScrollDisplayDownLine,"ScrollDisplayDownLine") },
                { Keys.CtrlHome,        MakeKeyHandler(ScrollDisplayTop,     "ScrollDisplayTop") },
                { Keys.CtrlEnd,         MakeKeyHandler(ScrollDisplayToCursor,"ScrollDisplayToCursor") },
            };

            _chordDispatchTable = new Dictionary<ConsoleKeyInfo, Dictionary<ConsoleKeyInfo, KeyHandler>>();

            // Escape,<key> table (meta key)
            _chordDispatchTable[Keys.Escape] = new Dictionary<ConsoleKeyInfo, KeyHandler>(new ConsoleKeyInfoComparer())
            {
                { Keys.B,               MakeKeyHandler(BackwardWord,         "BackwardWord") },
                { Keys.D,               MakeKeyHandler(KillWord,             "KillWord") },
                { Keys.F,               MakeKeyHandler(ForwardWord,          "ForwardWord") },
                { Keys.R,               MakeKeyHandler(RevertLine,           "RevertLine") },
                { Keys.Y,               MakeKeyHandler(YankPop,              "YankPop") },
                { Keys.CtrlY,           MakeKeyHandler(YankNthArg,           "YankNthArg") },
                { Keys.Backspace,       MakeKeyHandler(BackwardKillWord,     "BackwardKillWord") },
                { Keys.Period,          MakeKeyHandler(YankLastArg,          "YankLastArg") },
                { Keys.Underbar,        MakeKeyHandler(YankLastArg,          "YankLastArg") },
            };

            // Ctrl+X,<key> table
            _chordDispatchTable[Keys.CtrlX] = new Dictionary<ConsoleKeyInfo, KeyHandler>(new ConsoleKeyInfoComparer())
            {
                { Keys.Backspace,       MakeKeyHandler(BackwardKillLine,     "BackwardKillLine") },
                { Keys.CtrlU,           MakeKeyHandler(Undo,                 "Undo") },
                { Keys.CtrlX,           MakeKeyHandler(ExchangePointAndMark, "ExchangePointAndMark") },
            };
        }

        /// <summary>
        /// Show all bound keys
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void ShowKeyBindings(ConsoleKeyInfo? key = null, object arg = null)
        {
            var buffer = new StringBuilder();
            buffer.AppendFormat(CultureInfo.InvariantCulture, "{0,-20} {1,-24} {2}\n", "Key", "Function", "Description");
            buffer.AppendFormat(CultureInfo.InvariantCulture, "{0,-20} {1,-24} {2}\n", "---", "--------", "-----------");
            var boundKeys = GetKeyHandlers(includeBound: true, includeUnbound: false);
            var console = _singleton._console;
            var maxDescriptionLength = console.WindowWidth - 20 - 24 - 2;
            foreach (var boundKey in boundKeys)
            {
                var description = boundKey.Description;
                var newline = "\n";
                if (description.Length >= maxDescriptionLength)
                {
                    description = description.Substring(0, maxDescriptionLength - 3) + "...";
                    newline = "";
                }
                buffer.AppendFormat(CultureInfo.InvariantCulture, "{0,-20} {1,-24} {2}{3}", boundKey.Key, boundKey.Function, description, newline);
            }

            // Don't overwrite any of the line - so move to first line after the end of our buffer.
            var coords = _singleton.ConvertOffsetToCoordinates(_singleton._buffer.Length);
            var y = coords.Y + 1;
            _singleton.PlaceCursor(0, ref y);

            console.WriteLine(buffer.ToString());
            _singleton._initialY = console.CursorTop;
            _singleton.Render();
        }

        /// <summary>
        /// Read a key and tell me what the key is bound to.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void WhatIsKey(ConsoleKeyInfo? key = null, object arg = null)
        {
            _singleton._statusLinePrompt = "what-is-key: ";
            _singleton.Render();
            var toLookup = ReadKey();
            KeyHandler keyHandler;
            var buffer = new StringBuilder();
            _singleton._dispatchTable.TryGetValue(toLookup, out keyHandler);
            buffer.Append(toLookup.ToGestureString());
            if (keyHandler != null)
            {
                if (keyHandler.BriefDescription == "ChordFirstKey")
                {
                    Dictionary<ConsoleKeyInfo, KeyHandler> secondKeyDispatchTable;
                    if (_singleton._chordDispatchTable.TryGetValue(toLookup, out secondKeyDispatchTable))
                    {
                        toLookup = ReadKey();
                        secondKeyDispatchTable.TryGetValue(toLookup, out keyHandler);
                        buffer.Append(",");
                        buffer.Append(toLookup.ToGestureString());
                    }
                }
            }
            buffer.Append(": ");
            if (keyHandler != null)
            {
                buffer.Append(keyHandler.BriefDescription);
                if (!string.IsNullOrWhiteSpace(keyHandler.LongDescription))
                {
                    buffer.Append(" - ");
                    buffer.Append(keyHandler.LongDescription);
                }
            }
            else if (toLookup.KeyChar != 0)
            {
                buffer.Append("SelfInsert");
                buffer.Append(" - ");
                buffer.Append(PSReadLineResources.SelfInsertDescription);
            }
            else
            {
                buffer.Append(PSReadLineResources.KeyIsUnbound);
            }

            _singleton.ClearStatusMessage(render: false);

            // Don't overwrite any of the line - so move to first line after the end of our buffer.
            var coords = _singleton.ConvertOffsetToCoordinates(_singleton._buffer.Length);
            var y = coords.Y + 1;
            _singleton.PlaceCursor(0, ref y);

            _singleton._console.WriteLine(buffer.ToString());
            _singleton._initialY = _singleton._console.CursorTop;
            _singleton.Render();
        }
    }
}
