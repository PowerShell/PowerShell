// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

namespace System.Management.Automation.Subsystem
{
    /// <summary>
    /// Implementation of 'Get-PSSubsystem' cmdlet.
    /// </summary>
    [Experimental("PSSubsystemPluginModel", ExperimentAction.Show)]
    [Cmdlet(VerbsCommon.Get, "PSSubsystem", DefaultParameterSetName = AllSet)]
    [OutputType(typeof(SubsystemInfo))]
    public sealed class GetPSSubsystemCommand : PSCmdlet
    {
        private const string AllSet = "GetAllSet";
        private const string TypeSet = "GetByTypeSet";
        private const string KindSet = "GetByKindSet";

        /// <summary>
        /// Gets or sets a concrete subsystem kind.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = KindSet, ValueFromPipeline = true)]
        public SubsystemKind Kind { get; set; }

        /// <summary>
        /// Gets or sets the interface or abstract class type of a concrete subsystem.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = TypeSet, ValueFromPipeline = true)]
        public Type? SubsystemType { get; set; }

        /// <summary>
        /// ProcessRecord implementation.
        /// </summary>
        protected override void ProcessRecord()
        {
            switch (ParameterSetName)
            {
                case AllSet:
                    WriteObject(SubsystemManager.GetAllSubsystemInfo(), enumerateCollection: true);
                    break;
                case KindSet:
                    WriteObject(SubsystemManager.GetSubsystemInfo(Kind));
                    break;
                case TypeSet:
                    WriteObject(SubsystemManager.GetSubsystemInfo(SubsystemType!));
                    break;

                default:
                    throw new InvalidOperationException("New parameter set is added but the switch statement is not updated.");
            }
        }
    }
}
