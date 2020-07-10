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
    /// This cmdlet gets ScheduledJobTriggers for the specified ScheduledJobDefinition object.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "JobTrigger", DefaultParameterSetName = GetJobTriggerCommand.JobDefinitionParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=223915")]
    [OutputType(typeof(ScheduledJobTrigger))]
    public sealed class GetJobTriggerCommand : ScheduleJobCmdletBase
    {
        #region Parameters

        private const string JobDefinitionParameterSet = "JobDefinition";
        private const string JobDefinitionIdParameterSet = "JobDefinitionId";
        private const string JobDefinitionNameParameterSet = "JobDefinitionName";

        /// <summary>
        /// Trigger number to get.
        /// </summary>
        [Parameter(Position = 1,
                   ParameterSetName = GetJobTriggerCommand.JobDefinitionParameterSet)]
        [Parameter(Position = 1,
                   ParameterSetName = GetJobTriggerCommand.JobDefinitionIdParameterSet)]
        [Parameter(Position = 1,
                   ParameterSetName = GetJobTriggerCommand.JobDefinitionNameParameterSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Int32[] TriggerId
        {
            get { return _triggerIds; }

            set { _triggerIds = value; }
        }

        private Int32[] _triggerIds;

        /// <summary>
        /// ScheduledJobDefinition.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true,
                   ParameterSetName = GetJobTriggerCommand.JobDefinitionParameterSet)]
        [ValidateNotNull]
        public ScheduledJobDefinition InputObject
        {
            get { return _definition; }

            set { _definition = value; }
        }

        private ScheduledJobDefinition _definition;

        /// <summary>
        /// ScheduledJobDefinition Id.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true,
                   ParameterSetName = GetJobTriggerCommand.JobDefinitionIdParameterSet)]
        public Int32 Id
        {
            get { return _definitionId; }

            set { _definitionId = value; }
        }

        private Int32 _definitionId;

        /// <summary>
        /// ScheduledJobDefinition Name.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true,
                   ParameterSetName = GetJobTriggerCommand.JobDefinitionNameParameterSet)]
        [ValidateNotNullOrEmpty]
        public string Name
        {
            get { return _name; }

            set { _name = value; }
        }

        private string _name;

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
                    WriteTriggers(_definition);
                    break;

                case JobDefinitionIdParameterSet:
                    WriteTriggers(GetJobDefinitionById(_definitionId));
                    break;

                case JobDefinitionNameParameterSet:
                    WriteTriggers(GetJobDefinitionByName(_name));
                    break;
            }
        }

        #endregion

        #region Private Methods

        private void WriteTriggers(ScheduledJobDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            List<Int32> notFoundIds;
            List<ScheduledJobTrigger> triggers = definition.GetTriggers(_triggerIds, out notFoundIds);

            // Write found trigger objects.
            foreach (ScheduledJobTrigger trigger in triggers)
            {
                WriteObject(trigger);
            }

            // Report any triggers that were not found.
            foreach (Int32 notFoundId in notFoundIds)
            {
                WriteTriggerNotFoundError(notFoundId, definition.Name, definition);
            }
        }

        #endregion
    }
}
