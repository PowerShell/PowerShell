/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;

namespace Microsoft.PowerShell
{
    public partial class PSConsoleReadLine
    {
        /// <summary>
        /// Ends the current edit group, if needed, and invokes TabCompleteNext.
        /// </summary>
        public static void ViTabCompleteNext(ConsoleKeyInfo? key = null, object arg = null)
        {
            if (_singleton._editGroupStart >= 0)
            {
                _singleton._groupUndoHelper.EndGroup();
            }
            TabCompleteNext(key, arg);
        }

        /// <summary>
        /// Ends the current edit group, if needed, and invokes TabCompletePrevious.
        /// </summary>
        public static void ViTabCompletePrevious(ConsoleKeyInfo? key = null, object arg = null)
        {
            if (_singleton._editGroupStart >= 0)
            {
                _singleton._groupUndoHelper.EndGroup();
            }
            TabCompletePrevious(key, arg);
        }
    }
}
