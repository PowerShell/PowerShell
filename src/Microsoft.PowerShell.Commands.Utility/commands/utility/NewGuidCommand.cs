// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The implementation of the "new-guid" cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "Guid", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=526920")]
    [OutputType(typeof(Guid))]
    public class NewGuidCommand : Cmdlet
    {
        /// <summary>
        /// Returns a guid.
        /// </summary>
        protected override void EndProcessing()
        {
            WriteObject(Guid.NewGuid());
        }
    }
}
