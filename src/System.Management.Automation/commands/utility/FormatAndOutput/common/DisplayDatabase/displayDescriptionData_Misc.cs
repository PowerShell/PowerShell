/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

// this file contains the data structures for the in memory database
// containing display and formatting information

using System;
using System.Collections;
using System.Collections.ObjectModel;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    /// <summary>
    /// in line definition of a format string control
    /// </summary>
    internal sealed class FieldControlBody : ControlBody
    {
        internal FieldFormattingDirective fieldFormattingDirective = new FieldFormattingDirective();
    }
}
