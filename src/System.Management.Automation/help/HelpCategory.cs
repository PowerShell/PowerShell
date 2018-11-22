// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Language;

namespace System.Management.Automation
{
    /// <summary>
    /// Help categories.
    /// </summary>
    [Flags]
    internal enum HelpCategory
    {
        /// <summary>
        /// Undefined help category.
        /// </summary>
        None = 0x00,

        /// <summary>
        /// Alias help.
        /// </summary>
        Alias = 0x01,

        /// <summary>
        /// Cmdlet help.
        /// </summary>
        Cmdlet = 0x02,

        /// <summary>
        /// Provider help.
        /// </summary>
        Provider = 0x04,

        /// <summary>
        /// General keyword help.
        /// </summary>
        General = 0x10,

        /// <summary>
        /// FAQ's.
        /// </summary>
        FAQ = 0x20,

        /// <summary>
        /// Glossary and term definitions.
        /// </summary>
        Glossary = 0x40,

        /// <summary>
        /// Help that is contained in help file.
        /// </summary>
        HelpFile = 0x80,

        /// <summary>
        /// Help from a script block.
        /// </summary>
        ScriptCommand = 0x100,

        /// <summary>
        /// Help for a function.
        /// </summary>
        Function = 0x200,

        /// <summary>
        /// Help for a filter.
        /// </summary>
        Filter = 0x400,

        /// <summary>
        /// Help for an external script (i.e. for a *.ps1 file).
        /// </summary>
        ExternalScript = 0x800,

        /// <summary>
        /// All help categories.
        /// </summary>
        All = 0xFFFFF,

        ///<summary>
        /// Default Help.
        /// </summary>
        DefaultHelp = 0x1000,

        ///<summary>
        /// Help for a Workflow.
        /// </summary>
        Workflow = 0x2000,

        ///<summary>
        /// Help for a Configuration.
        /// </summary>
        Configuration = 0x4000,

        /// <summary>
        /// Help for DSC Resource.
        /// </summary>
        DscResource = 0x8000,

        /// <summary>
        /// Help for PS Classes.
        /// </summary>
        Class = 0x10000
    }
}
