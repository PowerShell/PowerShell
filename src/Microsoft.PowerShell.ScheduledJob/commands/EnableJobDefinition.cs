// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.ScheduledJob
{
    /// <summary>
    /// This cmdlet enables the specified ScheduledJobDefinition.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Enable, "ScheduledJob", SupportsShouldProcess = true, DefaultParameterSetName = DisableScheduledJobDefinitionBase.DefinitionParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=223926")]
    [OutputType(typeof(ScheduledJobDefinition))]
    public sealed class EnableScheduledJobCommand : DisableScheduledJobDefinitionBase
    {
        #region Properties

        /// <summary>
        /// Returns true if scheduled job definition should be enabled,
        /// false otherwise.
        /// </summary>
        protected override bool Enabled
        {
            get { return true; }
        }

        #endregion
    }
}
