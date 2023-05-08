// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.ScheduledJob
{
    /// <summary>
    /// This cmdlet gets scheduled job option object from a provided ScheduledJobDefinition object.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "ScheduledJobOption", DefaultParameterSetName = GetScheduledJobOptionCommand.JobDefinitionParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=223920")]
    [OutputType(typeof(ScheduledJobOptions))]
    public sealed class GetScheduledJobOptionCommand : ScheduleJobCmdletBase
    {
        #region Parameters

        private const string JobDefinitionParameterSet = "JobDefinition";
        private const string JobDefinitionIdParameterSet = "JobDefinitionId";
        private const string JobDefinitionNameParameterSet = "JobDefinitionName";

        /// <summary>
        /// ScheduledJobDefinition Id.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true,
                   ParameterSetName = GetScheduledJobOptionCommand.JobDefinitionIdParameterSet)]
        public Int32 Id
        {
            get { return _id; }

            set { _id = value; }
        }

        private Int32 _id;

        /// <summary>
        /// ScheduledJobDefinition Name.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true,
                   ParameterSetName = GetScheduledJobOptionCommand.JobDefinitionNameParameterSet)]
        [ValidateNotNullOrEmpty]
        public string Name
        {
            get { return _name; }

            set { _name = value; }
        }

        private string _name;

        /// <summary>
        /// ScheduledJobDefinition.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true,
                   ParameterSetName = GetScheduledJobOptionCommand.JobDefinitionParameterSet)]
        [ValidateNotNull]
        public ScheduledJobDefinition InputObject
        {
            get { return _definition; }

            set { _definition = value; }
        }

        private ScheduledJobDefinition _definition;

        #endregion

        #region Cmdlet Overrides

        /// <summary>
        /// Process input.
        /// </summary>
        protected override void ProcessRecord()
        {
            // Get ScheduledJobDefinition object.
            ScheduledJobDefinition definition = null;
            switch (ParameterSetName)
            {
                case JobDefinitionParameterSet:
                    definition = _definition;
                    break;

                case JobDefinitionIdParameterSet:
                    definition = GetJobDefinitionById(_id);
                    break;

                case JobDefinitionNameParameterSet:
                    definition = GetJobDefinitionByName(_name);
                    break;
            }

            // Return options from the definition object.
            if (definition != null)
            {
                WriteObject(definition.Options);
            }
        }

        #endregion
    }
}
