// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.ScheduledJob
{
    /// <summary>
    /// Base class for the DisableScheduledJobCommand, EnableScheduledJobCommand cmdlets.
    /// </summary>
    public abstract class DisableScheduledJobDefinitionBase : ScheduleJobCmdletBase
    {
        #region Parameters

        /// <summary>
        /// DefinitionIdParameterSet.
        /// </summary>
        protected const string DefinitionIdParameterSet = "DefinitionId";

        /// <summary>
        /// DefinitionNameParameterSet.
        /// </summary>
        protected const string DefinitionNameParameterSet = "DefinitionName";

        /// <summary>
        /// DefinitionParameterSet.
        /// </summary>
        protected const string DefinitionParameterSet = "Definition";

        /// <summary>
        /// ScheduledJobDefinition.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true,
                   ParameterSetName = DisableScheduledJobDefinitionBase.DefinitionParameterSet)]
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
                   ParameterSetName = DisableScheduledJobDefinitionBase.DefinitionIdParameterSet)]
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
                   ParameterSetName = DisableScheduledJobDefinitionBase.DefinitionNameParameterSet)]
        [ValidateNotNullOrEmpty]
        public string Name
        {
            get { return _definitionName; }

            set { _definitionName = value; }
        }

        private string _definitionName;

        /// <summary>
        /// Pass through ScheduledJobDefinition object.
        /// </summary>
        [Parameter(ParameterSetName = DisableScheduledJobDefinitionBase.DefinitionParameterSet)]
        [Parameter(ParameterSetName = DisableScheduledJobDefinitionBase.DefinitionIdParameterSet)]
        [Parameter(ParameterSetName = DisableScheduledJobDefinitionBase.DefinitionNameParameterSet)]
        public SwitchParameter PassThru
        {
            get { return _passThru; }

            set { _passThru = value; }
        }

        private SwitchParameter _passThru;

        #endregion

        #region Cmdlet Overrides

        /// <summary>
        /// Process input.
        /// </summary>
        protected override void ProcessRecord()
        {
            ScheduledJobDefinition definition = null;

            switch (ParameterSetName)
            {
                case DefinitionParameterSet:
                    definition = _definition;
                    break;

                case DefinitionIdParameterSet:
                    definition = GetJobDefinitionById(_definitionId);
                    break;

                case DefinitionNameParameterSet:
                    definition = GetJobDefinitionByName(_definitionName);
                    break;
            }

            string verbName = Enabled ? VerbsLifecycle.Enable : VerbsLifecycle.Disable;

            if (definition != null &&
                ShouldProcess(definition.Name, verbName))
            {
                try
                {
                    definition.SetEnabled(Enabled, true);
                }
                catch (ScheduledJobException e)
                {
                    string msg = StringUtil.Format(ScheduledJobErrorStrings.CantSetEnableOnJobDefinition, definition.Name);
                    Exception reason = new RuntimeException(msg, e);
                    ErrorRecord errorRecord = new ErrorRecord(reason, "CantSetEnableOnScheduledJobDefinition", ErrorCategory.InvalidOperation, definition);
                    WriteError(errorRecord);
                }

                if (_passThru)
                {
                    WriteObject(definition);
                }
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns true if scheduled job definition should be enabled,
        /// false otherwise.
        /// </summary>
        protected abstract bool Enabled
        {
            get;
        }

        #endregion
    }
}
