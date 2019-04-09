// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Host;
using System.Threading;
using System.Diagnostics;

namespace Microsoft.PowerShell.ScheduledJob
{
    /// <summary>
    /// This cmdlet adds ScheduledJobTriggers to ScheduledJobDefinition objects.
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "JobTrigger", DefaultParameterSetName = AddJobTriggerCommand.JobDefinitionParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=223913")]
    public sealed class AddJobTriggerCommand : ScheduleJobCmdletBase
    {
        #region Parameters

        private const string JobDefinitionParameterSet = "JobDefinition";
        private const string JobDefinitionIdParameterSet = "JobDefinitionId";
        private const string JobDefinitionNameParameterSet = "JobDefinitionName";

        /// <summary>
        /// ScheduledJobTrigger.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true, ValueFromPipeline = true,
                   ParameterSetName = AddJobTriggerCommand.JobDefinitionParameterSet)]
        [Parameter(Position = 1, Mandatory = true, ValueFromPipeline = true,
                   ParameterSetName = AddJobTriggerCommand.JobDefinitionIdParameterSet)]
        [Parameter(Position = 1, Mandatory = true, ValueFromPipeline = true,
                   ParameterSetName = AddJobTriggerCommand.JobDefinitionNameParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public ScheduledJobTrigger[] Trigger
        {
            get { return _triggers; }

            set { _triggers = value; }
        }

        private ScheduledJobTrigger[] _triggers;

        /// <summary>
        /// ScheduledJobDefinition Id.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true,
                   ParameterSetName = AddJobTriggerCommand.JobDefinitionIdParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Int32[] Id
        {
            get { return _ids; }

            set { _ids = value; }
        }

        private Int32[] _ids;

        /// <summary>
        /// ScheduledJobDefinition Name.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true,
                   ParameterSetName = AddJobTriggerCommand.JobDefinitionNameParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Name
        {
            get { return _names; }

            set { _names = value; }
        }

        private string[] _names;

        /// <summary>
        /// ScheduledJobDefinition.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true,
                   ParameterSetName = AddJobTriggerCommand.JobDefinitionParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public ScheduledJobDefinition[] InputObject
        {
            get { return _definitions; }

            set { _definitions = value; }
        }

        private ScheduledJobDefinition[] _definitions;

        #endregion

        #region Cmdlet Overrides

        /// <summary>
        /// Process input.
        /// </summary>
        protected override void ProcessRecord()
        {
            switch (ParameterSetName)
            {
                case JobDefinitionParameterSet:
                    AddToJobDefinition(_definitions);
                    break;

                case JobDefinitionIdParameterSet:
                    AddToJobDefinition(GetJobDefinitionsById(_ids));
                    break;

                case JobDefinitionNameParameterSet:
                    AddToJobDefinition(GetJobDefinitionsByName(_names));
                    break;
            }
        }

        #endregion

        #region Private Methods

        private void AddToJobDefinition(IEnumerable<ScheduledJobDefinition> jobDefinitions)
        {
            foreach (ScheduledJobDefinition definition in jobDefinitions)
            {
                try
                {
                    definition.AddTriggers(_triggers, true);
                }
                catch (ScheduledJobException e)
                {
                    string msg = StringUtil.Format(ScheduledJobErrorStrings.CantAddJobTriggersToDefinition, definition.Name);
                    Exception reason = new RuntimeException(msg, e);
                    ErrorRecord errorRecord = new ErrorRecord(reason, "CantAddJobTriggersToScheduledJobDefinition", ErrorCategory.InvalidOperation, definition);
                    WriteError(errorRecord);
                }
            }
        }

        #endregion
    }
}
