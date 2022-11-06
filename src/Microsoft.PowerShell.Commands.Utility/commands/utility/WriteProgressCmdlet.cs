// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implements the write-progress cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommunications.Write, "Progress", DefaultParameterSetName = "Processing", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097036", RemotingCapability = RemotingCapability.None)]
    public sealed class WriteProgressCommand : PSCmdlet
    {
        /// <summary>
        /// Describes the activity for which progress is being reported.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Processing", HelpMessageBaseName = HelpMessageBaseName, HelpMessageResourceId = "ActivityParameterHelpMessage")]
        [Parameter(Position = 0, ParameterSetName = "Completed", HelpMessageBaseName = HelpMessageBaseName, HelpMessageResourceId = "ActivityParameterHelpMessage")]
        public string Activity { get; set; }

        /// <summary>
        /// Describes the current state of the activity.
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = "Processing", HelpMessageBaseName = HelpMessageBaseName, HelpMessageResourceId = "StatusParameterHelpMessage")]
        [ValidateNotNullOrEmpty]
        public string Status { get; set; } = WriteProgressResourceStrings.Processing;

        /// <summary>
        /// Uniquely identifies this activity for purposes of chaining subordinate activities.
        /// </summary>
        [Parameter(Position = 2, ParameterSetName = "Processing")]
        [Parameter(Position = 1, ParameterSetName = "Completed")]
        [ValidateRange(0, int.MaxValue)]
        public int Id { get; set; }

        /// <summary>
        /// Percentage completion of the activity, or -1 if n/a.
        /// </summary>
        [Parameter(ParameterSetName = "Processing")]
        [ValidateRange(-1, 100)]
        public int PercentComplete { get; set; } = -1;

        /// <summary>
        /// Seconds remaining to complete the operation, or -1 if n/a.
        /// </summary>
        [Parameter(ParameterSetName = "Processing")]
        public int SecondsRemaining { get; set; } = -1;

        /// <summary>
        /// Description of current operation in activity, empty if n/a.
        /// </summary>
        [Parameter(ParameterSetName = "Processing")]
        public string CurrentOperation { get; set; }

        /// <summary>
        /// Identifies the parent Id of this activity, or -1 if none.
        /// </summary>
        [Parameter(ParameterSetName = "Processing")]
        [ValidateRange(-1, int.MaxValue)]
        public int ParentId { get; set; } = -1;

        /// <summary>
        /// Identifies whether the activity has completed (and the display for it should be removed),
        /// or if it is proceeding (and the display for it should be shown).
        /// </summary>
        [Parameter(ParameterSetName = "Processing")]
        [Parameter(Mandatory = true, ParameterSetName = "Completed")]
        public SwitchParameter Completed
        {
            get
            {
                return _completed;
            }

            set
            {
                _completed = value;
            }
        }

        /// <summary>
        /// Identifies the source of the record.
        /// </summary>
        [Parameter]
        public int SourceId { get; set; }

        /// <summary>
        /// Writes a ProgressRecord created from the parameters.
        /// </summary>
        protected override
        void
        ProcessRecord()
        {
            ProgressRecord pr;
            if (Completed || ParameterSetName == "Completed")
            {
                pr = new(Id, "null", Status);
                pr.RecordType = ProgressRecordType.Completed;
            }
            else
            {
                pr = new(Id, Activity, Status);
                pr.ParentActivityId = ParentId;
                pr.PercentComplete = PercentComplete;
                pr.SecondsRemaining = SecondsRemaining;
                pr.CurrentOperation = CurrentOperation;
                pr.RecordType = ProgressRecordType.Processing;
            }

            WriteProgress(SourceId, pr);
        }

        private bool _completed;

        private const string HelpMessageBaseName = "WriteProgressResourceStrings";
    }
}
