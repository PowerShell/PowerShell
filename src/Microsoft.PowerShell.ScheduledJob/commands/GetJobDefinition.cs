// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;

namespace Microsoft.PowerShell.ScheduledJob
{
    /// <summary>
    /// This cmdlet gets scheduled job definition objects from the local repository.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "ScheduledJob", DefaultParameterSetName = GetScheduledJobCommand.DefinitionIdParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=223923")]
    [OutputType(typeof(ScheduledJobDefinition))]
    public sealed class GetScheduledJobCommand : ScheduleJobCmdletBase
    {
        #region Parameters

        private const string DefinitionIdParameterSet = "DefinitionId";
        private const string DefinitionNameParameterSet = "DefinitionName";

        /// <summary>
        /// ScheduledJobDefinition Id.
        /// </summary>
        [Parameter(Position = 0,
                   ParameterSetName = GetScheduledJobCommand.DefinitionIdParameterSet)]
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
                   ParameterSetName = GetScheduledJobCommand.DefinitionNameParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Name
        {
            get { return _definitionNames; }

            set { _definitionNames = value; }
        }

        private string[] _definitionNames;

        #endregion

        #region Cmdlet Overrides

        /// <summary>
        /// Process input.
        /// </summary>
        protected override void ProcessRecord()
        {
            switch (ParameterSetName)
            {
                case DefinitionIdParameterSet:
                    if (_definitionIds == null)
                    {
                        FindAllJobDefinitions(
                            (definition) =>
                            {
                                WriteObject(definition);
                            });
                    }
                    else
                    {
                        FindJobDefinitionsById(
                            _definitionIds,
                            (definition) =>
                            {
                                WriteObject(definition);
                            });
                    }

                    break;

                case DefinitionNameParameterSet:
                    FindJobDefinitionsByName(
                        _definitionNames,
                        (definition) =>
                        {
                            WriteObject(definition);
                        });
                    break;
            }
        }

        #endregion
    }
}
