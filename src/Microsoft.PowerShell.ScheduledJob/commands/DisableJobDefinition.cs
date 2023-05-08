// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.ScheduledJob
{
    /// <summary>
    /// This cmdlet disables the specified ScheduledJobDefinition.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Disable, "ScheduledJob", SupportsShouldProcess = true, DefaultParameterSetName = DisableScheduledJobDefinitionBase.DefinitionParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=223927")]
    [OutputType(typeof(ScheduledJobDefinition))]
    public sealed class DisableScheduledJobCommand : DisableScheduledJobDefinitionBase
    {
        #region Properties

        /// <summary>
        /// Returns true if scheduled job definition should be enabled,
        /// false otherwise.
        /// </summary>
        protected override bool Enabled
        {
            get { return false; }
        }

        #endregion
    }
}
