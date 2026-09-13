// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands {
    // --- The Class of Module: Show the License ---
    [ Cmdlet(VerbsCommon.Show, "PowerShellLicense") ]
    public class ShowLicense : Cmdlet {
        protected override void ProcessRecord() {
            string SystemLicense = "Copyright (C) Microsoft Corporation and licensed under the MIT License.";
            WriteObject(SystemLicense);
        }
    }
    // --- End of Class, Module: Show the License ---
}


/** Notes:
 * I´m create this File for contribute with Microsoft,
 * and if the Workflow mescle my branch with principal branch,
 * I´m HAPPY! And: My Graphics Engine use the DirectX 12.
 * Link of my Engine: https://github.com/dev12124/QMX */
