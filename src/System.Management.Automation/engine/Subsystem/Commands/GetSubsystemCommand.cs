// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation.Subsystem
{
    /// <summary>
    /// Implementation of 'Get-Subsystem' cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Subsystem", DefaultParameterSetName = AllSet)]
    [OutputType(typeof(SubsystemInfo))]
    public sealed class GetSubsystemCommand : PSCmdlet
    {
        private const string AllSet = "GetAllSet";
        private const string TypeSet = "GetByTypeSet";
        private const string KindSet = "GetByKindSet";

        /// <summary>
        /// Gets or sets a concrete subsystem kind.
        /// </summary>
        [Parameter(ParameterSetName = KindSet, ValueFromPipeline = true)]
        public SubsystemKind Kind { get; set; }

        /// <summary>
        /// Gets or sets the interface or abstract class type of a concrete subsystem.
        /// </summary>
        [Parameter(ParameterSetName = TypeSet, ValueFromPipeline = true)]
        public Type SubsystemType { get; set; }

        /// <summary>
        /// ProcessRecord implementation.
        /// </summary>
        protected override void ProcessRecord()
        {
            switch (ParameterSetName)
            {
                case AllSet:
                    WriteObject(SubsystemManager.GetAllSubsystemInfo());
                    break;
                case KindSet:
                    WriteObject(SubsystemManager.GetSubsystemInfo(Kind));
                    break;
                case TypeSet:
                    WriteObject(SubsystemManager.GetSubsystemInfo(SubsystemType));
                    break;

                default:
                    throw new InvalidOperationException("Unreachable code");
            }
        }
    }
}
