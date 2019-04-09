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
using System.Globalization;

namespace Microsoft.PowerShell.ScheduledJob
{
    /// <summary>
    /// This cmdlet removes ScheduledJobTriggers from ScheduledJobDefinition objects.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "JobTrigger", DefaultParameterSetName = RemoveJobTriggerCommand.JobDefinitionParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=223914")]
    public sealed class RemoveJobTriggerCommand : ScheduleJobCmdletBase
    {
        #region Parameters

        private const string JobDefinitionParameterSet = "JobDefinition";
        private const string JobDefinitionIdParameterSet = "JobDefinitionId";
        private const string JobDefinitionNameParameterSet = "JobDefinitionName";

        /// <summary>
        /// Trigger number to remove.
        /// </summary>
        [Parameter(ParameterSetName = RemoveJobTriggerCommand.JobDefinitionParameterSet)]
        [Parameter(ParameterSetName = RemoveJobTriggerCommand.JobDefinitionIdParameterSet)]
        [Parameter(ParameterSetName = RemoveJobTriggerCommand.JobDefinitionNameParameterSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Int32[] TriggerId
        {
            get { return _triggerIds; }

            set { _triggerIds = value; }
        }

        private Int32[] _triggerIds;

        /// <summary>
        /// ScheduledJobDefinition Id.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true,
                   ParameterSetName = RemoveJobTriggerCommand.JobDefinitionIdParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Int32[] Id
        {
            get { return _definitionIds; }

            set { _definitionIds = value; }
        }

        private Int32[] _definitionIds;

        /// <summary>
        /// ScheduledJobDefinition Name.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true,
                   ParameterSetName = RemoveJobTriggerCommand.JobDefinitionNameParameterSet)]
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
                   ParameterSetName = RemoveJobTriggerCommand.JobDefinitionParameterSet)]
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
                    RemoveFromJobDefinition(_definitions);
                    break;

                case JobDefinitionIdParameterSet:
                    RemoveFromJobDefinition(GetJobDefinitionsById(_definitionIds));
                    break;

                case JobDefinitionNameParameterSet:
                    RemoveFromJobDefinition(GetJobDefinitionsByName(_names));
                    break;
            }
        }

        #endregion

        #region Private Methods

        private void RemoveFromJobDefinition(IEnumerable<ScheduledJobDefinition> definitions)
        {
            foreach (ScheduledJobDefinition definition in definitions)
            {
                List<Int32> notFoundIds = new List<int>();
                try
                {
                    notFoundIds = definition.RemoveTriggers(_triggerIds, true);
                }
                catch (ScheduledJobException e)
                {
                    string msg = StringUtil.Format(ScheduledJobErrorStrings.CantRemoveTriggersFromDefinition, definition.Name);
                    Exception reason = new RuntimeException(msg, e);
                    ErrorRecord errorRecord = new ErrorRecord(reason, "CantRemoveTriggersFromScheduledJobDefinition", ErrorCategory.InvalidOperation, definition);
                    WriteError(errorRecord);
                }

                // Report not found errors.
                foreach (Int32 idNotFound in notFoundIds)
                {
                    WriteTriggerNotFoundError(idNotFound, definition.Name, definition);
                }
            }
        }

        #endregion
    }
}
