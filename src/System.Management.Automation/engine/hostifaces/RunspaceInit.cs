/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Management.Automation;
using System.Management.Automation.Host;
using Microsoft.PowerShell.Commands;
using System.Management.Automation.Internal;
using System.Reflection;
using System.Runtime.InteropServices;

using Dbg = System.Management.Automation.Diagnostics;
using DWORD = System.UInt32;
using Microsoft.Win32;
using System.Security;



namespace System.Management.Automation.Runspaces
{
    /// <summary>
    /// Runspace class for local runspace
    /// </summary>

    internal sealed partial
    class LocalRunspace : RunspaceBase
    {
        /// <summary>
        /// initialize default values of preference vars
        /// </summary>
        ///
        /// <returns> Does not return a value </returns>
        ///
        /// <remarks>  </remarks>
        ///

        private void InitializeDefaults()
        {
            SessionStateInternal ss = _engine.Context.EngineSessionState;
            Dbg.Assert(ss != null, "SessionState should not be null");

            // Add the variables that must always be there...
            ss.InitializeFixedVariables();

            // If this is being built from a runspace configuration, then
            // add all of the default entries. When initializing from an InitialSessionState
            // object, it will contain the defaults if so desired.
            if (this.RunspaceConfiguration != null)
            {
                bool addSetStrictMode = true;
                foreach (RunspaceConfigurationEntry entry in this.RunspaceConfiguration.Cmdlets)
                {
                    if (entry.Name.Equals("Set-StrictMode", StringComparison.OrdinalIgnoreCase))
                    {
                        addSetStrictMode = false;
                        break;
                    }
                }
                // Add all of the built-in variable, function and alias definitions...
                ss.AddBuiltInEntries(addSetStrictMode);
            }
        }
    }
}

