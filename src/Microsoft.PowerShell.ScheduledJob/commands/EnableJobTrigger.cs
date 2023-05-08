// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Threading;

namespace Microsoft.PowerShell.ScheduledJob
{
    /// <summary>
    /// This cmdlet disables triggers on a ScheduledJobDefinition object.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Enable, "JobTrigger", SupportsShouldProcess = true, DefaultParameterSetName = EnableJobTriggerCommand.EnabledParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=223917")]
    public sealed class EnableJobTriggerCommand : EnableDisableScheduledJobCmdletBase
    {
       #region Enabled Implementation

        /// <summary>
        /// Property to determine if trigger should be enabled or disabled.
        /// </summary>
        internal override bool Enabled
        {
            get { return true; }
        }

        #endregion
    }
}
