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
    /// Base class for DisableJobTrigger, EnableJobTrigger cmdlets.
    /// </summary>
    public abstract class EnableDisableScheduledJobCmdletBase : ScheduleJobCmdletBase
    {
        #region Parameters

        /// <summary>
        /// JobDefinition parameter set.
        /// </summary>
        protected const string EnabledParameterSet = "JobEnabled";

        /// <summary>
        /// ScheduledJobTrigger objects to set properties on.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true,
                   ParameterSetName = EnableDisableScheduledJobCmdletBase.EnabledParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public ScheduledJobTrigger[] InputObject
        {
            get { return _triggers; }

            set { _triggers = value; }
        }

        /// <summary>
        /// Pass through for scheduledjobtrigger object.
        /// </summary>
        [Parameter(ParameterSetName = EnableDisableScheduledJobCmdletBase.EnabledParameterSet)]
        public SwitchParameter PassThru
        {
            get { return _passThru; }

            set { _passThru = value; }
        }

        private SwitchParameter _passThru;

        private ScheduledJobTrigger[] _triggers;

        #endregion

        #region Cmdlet Overrides

        /// <summary>
        /// Process input.
        /// </summary>
        protected override void ProcessRecord()
        {
            // Update each trigger with the current enabled state.
            foreach (ScheduledJobTrigger trigger in _triggers)
            {
                trigger.Enabled = Enabled;
                if (trigger.JobDefinition != null)
                {
                    trigger.UpdateJobDefinition();
                }

                if (_passThru)
                {
                    WriteObject(trigger);
                }
            }
        }

        #endregion

        #region Internal Properties

        /// <summary>
        /// Property to determine if trigger should be enabled or disabled.
        /// </summary>
        internal abstract bool Enabled
        {
            get;
        }

        #endregion
    }
}
