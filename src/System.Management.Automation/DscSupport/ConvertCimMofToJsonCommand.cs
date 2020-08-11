// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Text;

namespace Microsoft.PowerShell.DesiredStateConfiguration.Internal
{
    /// <summary>
    /// Convert-CimMofToJson cmdlet implementation
    /// </summary>
    [Cmdlet(VerbsData.Convert, "CimMofToJson")]
    public sealed class ConvertCimMofToJsonCommand : Cmdlet
    {
        /// <summary>
        /// Top level directory to serach for .mof files
        /// </summary>
        /// <value>Test</value>
        [Parameter(ValueFromPipeline = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public string Directory { get; set; }

        /// <summary>
        /// Main cmdlet method
        /// </summary>
        protected override void ProcessRecord()
        {
            // Mof parser uses DSC_HOME env var which is normally set by PSDesiredStateConfiguration module
            // Because this cmlet can be run without loading PSDesiredStateConfiguration module, we are setting this env var here.
            string varName = "DSC_HOME";
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(varName, EnvironmentVariableTarget.Process)))
            {
                var pshome = Utils.DefaultPowerShellAppBase;
                var dsc_home = Path.Combine(pshome, "Modules", "PSDesiredStateConfiguration", "Configuration");
                Environment.SetEnvironmentVariable(varName, dsc_home, EnvironmentVariableTarget.Process);
            }

            Mof.DscClassCache.Initialize();
            foreach(var mofPath in System.IO.Directory.GetFiles(this.Directory, "*.mof", SearchOption.AllDirectories))
            {
                Mof.DscClassCache.ConvertCimMofToJson(mofPath);
            }
        }
    }
}
