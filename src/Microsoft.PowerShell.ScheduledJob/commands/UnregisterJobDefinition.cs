// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerShell.ScheduledJob
{
    /// <summary>
    /// This cmdlet removes the specified ScheduledJobDefinition objects from the
    /// Task Scheduler, job store, and local repository.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Unregister, "ScheduledJob", SupportsShouldProcess = true, DefaultParameterSetName = UnregisterScheduledJobCommand.DefinitionParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=223925")]
    public sealed class UnregisterScheduledJobCommand : ScheduleJobCmdletBase
    {
        #region Parameters

        private const string DefinitionIdParameterSet = "DefinitionId";
        private const string DefinitionNameParameterSet = "DefinitionName";
        private const string DefinitionParameterSet = "Definition";

        /// <summary>
        /// ScheduledJobDefinition Id.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true,
                   ParameterSetName = UnregisterScheduledJobCommand.DefinitionIdParameterSet)]
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
                   ParameterSetName = UnregisterScheduledJobCommand.DefinitionNameParameterSet)]
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
                   ParameterSetName = UnregisterScheduledJobCommand.DefinitionParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public ScheduledJobDefinition[] InputObject
        {
            get { return _definitions; }

            set { _definitions = value; }
        }

        private ScheduledJobDefinition[] _definitions;

        /// <summary>
        /// When true this will stop any running instances of this job definition before
        /// removing the definition.
        /// </summary>
        [Parameter(ParameterSetName = UnregisterScheduledJobCommand.DefinitionIdParameterSet)]
        [Parameter(ParameterSetName = UnregisterScheduledJobCommand.DefinitionNameParameterSet)]
        [Parameter(ParameterSetName = UnregisterScheduledJobCommand.DefinitionParameterSet)]
        public SwitchParameter Force
        {
            get { return _force; }

            set { _force = value; }
        }

        private SwitchParameter _force;

        #endregion

        #region Cmdlet Overrides

        /// <summary>
        /// Process input.
        /// </summary>
        protected override void ProcessRecord()
        {
            List<ScheduledJobDefinition> definitions = null;
            switch (ParameterSetName)
            {
                case DefinitionParameterSet:
                    definitions = new List<ScheduledJobDefinition>(_definitions);
                    break;

                case DefinitionNameParameterSet:
                    definitions = GetJobDefinitionsByName(_names);
                    break;

                case DefinitionIdParameterSet:
                    definitions = GetJobDefinitionsById(_definitionIds);
                    break;
            }

            if (definitions != null)
            {
                foreach (ScheduledJobDefinition definition in definitions)
                {
                    string targetString = StringUtil.Format(ScheduledJobErrorStrings.DefinitionWhatIf, definition.Name);
                    if (ShouldProcess(targetString, VerbsLifecycle.Unregister))
                    {
                        // Removes the ScheduledJobDefinition from the job store,
                        // Task Scheduler, and disposes the object.
                        try
                        {
                            definition.Remove(_force);
                        }
                        catch (ScheduledJobException e)
                        {
                            string msg = StringUtil.Format(ScheduledJobErrorStrings.CantUnregisterDefinition, definition.Name);
                            Exception reason = new RuntimeException(msg, e);
                            ErrorRecord errorRecord = new ErrorRecord(reason, "CantUnregisterScheduledJobDefinition", ErrorCategory.InvalidOperation, definition);
                            WriteError(errorRecord);
                        }
                    }
                }
            }

            // Check for unknown definition names.
            if ((_names != null && _names.Length > 0) &&
                (_definitions == null || _definitions.Length < _names.Length))
            {
                // Make sure there is no PowerShell task in Task Scheduler with removed names.
                // This covers the case where the scheduled job definition was manually removed from
                // the job store but remains as a PowerShell task in Task Scheduler.
                using (ScheduledJobWTS taskScheduler = new ScheduledJobWTS())
                {
                    foreach (string name in _names)
                    {
                        taskScheduler.RemoveTaskByName(name, true, true);
                    }
                }
            }
        }

        #endregion
    }
}
