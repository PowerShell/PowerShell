
/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The implementation of the "new-guid" cmdlet
    /// </summary>
    [Cmdlet(VerbsCommon.New, "Guid", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=526920")]
    [OutputType(typeof(Guid))]
    public class NewGuidCommand : Cmdlet
    {
        /// <summary>
        /// returns a guid
        /// </summary>
        protected override void EndProcessing()
        {
            WriteObject(Guid.NewGuid());
        }
    }
}
