// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implements the write-progress cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommunications.Write, "Progress", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097036", RemotingCapability = RemotingCapability.None)]
    public sealed class WriteProgressCommand : PSCmdlet
    {
        /// <summary>
        /// Describes the activity for which progress is being reported.
        /// </summary>
        [Parameter(
            Position = 0,
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
        [ValidateRange(0, int.MaxValue)]
        public int Id { get; set; }

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
        [ValidateRange(-1, int.MaxValue)]
        public int ParentId { get; set; } = -1;

        /// <summary>
        /// Specifies that the activity is non-essential, and may be discarded by the host to improve performance.
        /// </summary>
        [Parameter]
        public SwitchParameter NonEssential { get; set; }

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
            ProgressRecord pr;
            if (string.IsNullOrEmpty(Activity))
            {
                if (!Completed)
                {
                    ThrowTerminatingError(new ErrorRecord(
                    new ArgumentException("Missing value for mandatory parameter.", nameof(Activity)),
                    "MissingActivity",
                    ErrorCategory.InvalidArgument,
                    Activity));
                    return;
                }
                else
                {
                    pr = new(Id);
                    pr.StatusDescription = Status;
                }
            }
            else
            {
                pr = new(Id, Activity, Status);
            }

            pr.ParentActivityId = ParentId;
            pr.PercentComplete = PercentComplete;
            pr.SecondsRemaining = SecondsRemaining;
            pr.CurrentOperation = CurrentOperation;
            pr.IsEssential = !NonEssential;
            pr.RecordType = this.Completed ? ProgressRecordType.Completed : ProgressRecordType.Processing;            

            WriteProgress(SourceId, pr);
        }

        private bool _completed;

        private const string HelpMessageBaseName = "WriteProgressResourceStrings";
    }
}
