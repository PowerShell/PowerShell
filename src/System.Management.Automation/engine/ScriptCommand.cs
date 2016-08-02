/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Reflection;
using System.Text;

namespace System.Management.Automation
{
    /// <summary>
    /// Derives InternalCommand for ScriptCommand.
    /// </summary>
    internal sealed class ScriptCommand : InternalCommand
    {
        // This class just needs to exist so we have something to instantiate
        // to hold the pipeline connectors...
    }
}