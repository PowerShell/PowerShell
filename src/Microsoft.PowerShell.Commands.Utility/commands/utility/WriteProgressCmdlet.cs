// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Management.Automation;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implements the write-progress cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommunications.Write, "Progress", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113428", RemotingCapability = RemotingCapability.None)]
    public sealed class WriteProgressCommand : PSCmdlet
    {
        /// <summary>
        /// Describes the activity for which progress is being reported.
        /// </summary>
        [Parameter(
            Position = 0,
            Mandatory = true,
            HelpMessageBaseName = HelpMessageBaseName,
            HelpMessageResourceId = "ActivityParameterHelpMessage")]
        public string Activity { get; set; }

        /// <summary>
        /// Describes the current state of the activity.
        /// </summary>
        [Parameter(
            Position = 1,
            HelpMessageBaseName = HelpMessageBaseName,
            HelpMessageResourceId = "StatusParameterHelpMessage")]
        [ValidateNotNullOrEmpty]
        public string Status { get; set; } = WriteProgressResourceStrings.Processing;

        /// <summary>
        /// Uniquely identifies this activity for purposes of chaining subordinate activities.
        /// </summary>
        [Parameter(Position = 2)]
        [ValidateRange(0, Int32.MaxValue)]
        public int Id { get; set; } = 0;

        /// <summary>
        /// Percentage completion of the activity, or -1 if n/a.
        /// </summary>
        [Parameter]
        [ValidateRange(-1, 100)]
        public int PercentComplete { get; set; } = -1;

        /// <summary>
        /// Seconds remaining to complete the operation, or -1 if n/a.
        /// </summary>
        [Parameter]
        public int SecondsRemaining { get; set; } = -1;

        /// <summary>
        /// Description of current operation in activity, empty if n/a.
        /// </summary>
        [Parameter]
        public string CurrentOperation { get; set; }

        /// <summary>
        /// Identifies the parent Id of this activity, or -1 if none.
        /// </summary>
        [Parameter]
        [ValidateRange(-1, Int32.MaxValue)]
        public int ParentId { get; set; } = -1;

        /// <summary>
        /// Identifies whether the activity has completed (and the display for it should be removed),
        /// or if it is proceeding (and the display for it should be shown).
        /// </summary>
        [Parameter]
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
            ProgressRecord pr = new ProgressRecord(Id, Activity, Status);
            pr.ParentActivityId = ParentId;
            pr.PercentComplete = PercentComplete;
            pr.SecondsRemaining = SecondsRemaining;
            pr.CurrentOperation = CurrentOperation;
            pr.RecordType = this.Completed ? ProgressRecordType.Completed : ProgressRecordType.Processing;

            WriteProgress(SourceId, pr);
        }

        private bool _completed;

        private const string HelpMessageBaseName = "WriteProgressResourceStrings";
    }
}
