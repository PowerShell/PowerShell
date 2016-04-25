/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.PowerShell
{
    public partial class PSConsoleReadLine
    {
        private class GroupUndoHelper
        {
            public Action<ConsoleKeyInfo?, object> _instigator = null;
            public object _instigatorArg = null;

            public GroupUndoHelper()
            {
                _instigator = null;
                _instigatorArg = null;
            }

            public void StartGroup(Action<ConsoleKeyInfo?, object> instigator, object instigatorArg)
            {
                _instigator = instigator;
                _instigatorArg = instigatorArg;
                _singleton.StartEditGroup();
            }

            public void Clear()
            {
                _instigator = null;
                _instigatorArg = null;
            }

            public void EndGroup()
            {
                if (_singleton._editGroupStart >= 0)
                {
                    _singleton.EndEditGroup(_instigator, _instigatorArg);
                }
                Clear();
            }
        }
        private GroupUndoHelper _groupUndoHelper = new GroupUndoHelper();

        /// <summary>
        /// Undo all previous edits for line.
        /// </summary>
        public static void UndoAll(ConsoleKeyInfo? key = null, object arg = null)
        {
            if (_singleton._undoEditIndex > 0)
            {
                while (_singleton._undoEditIndex > 0)
                {
                    _singleton._edits[_singleton._undoEditIndex - 1].Undo();
                    _singleton._undoEditIndex--;
                }
                _singleton.Render();
            }
            else
            {
                Ding();
            }
        }
    }
}
