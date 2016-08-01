/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/


using System;
using System.Management.Automation;

using Dbg = System.Management.Automation.Diagnostics;



namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// 
    /// Implements the write-progress cmdlet
    /// 
    /// </summary>

    [Cmdlet("Write", "Progress", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113428", RemotingCapability = RemotingCapability.None)]
    public sealed class WriteProgressCommand : PSCmdlet
    {
        /// <summary>
        /// 
        /// Describes the activity for which progress is being reported.
        /// 
        /// </summary>
        /// <value></value>

        [Parameter(
            Position = 0,
            Mandatory = true,
            HelpMessageBaseName = HelpMessageBaseName,
            HelpMessageResourceId = "ActivityParameterHelpMessage")]
        public
        string
        Activity
        {
            get
            {
                return _activity;
            }
            set
            {
                _activity = value;
            }
        }


        /// <summary>
        /// 
        /// Describes the current state of the activity.
        /// 
        /// </summary>
        /// <value></value>

        [Parameter(
            Position = 1,
            HelpMessageBaseName = HelpMessageBaseName,
            HelpMessageResourceId = "StatusParameterHelpMessage")]
        [ValidateNotNullOrEmpty]
        public
        string
        Status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
            }
        }



        /// <summary>
        /// 
        /// Uniquely identifies this activity for purposes of chaining subordinate activities.
        /// 
        /// </summary>
        /// <value></value>

        [Parameter(Position = 2)]
        [ValidateRange(0, Int32.MaxValue)]
        public
        int
        Id
        {
            get
            {
                return _activityId;
            }
            set
            {
                _activityId = value;
            }
        }



        /// <summary>
        /// 
        /// Percentage completion of the activity, or -1 if n/a
        /// 
        /// </summary>
        /// <value></value>

        [Parameter]
        [ValidateRange(-1, 100)]
        public
        int
        PercentComplete
        {
            get
            {
                return _percentComplete;
            }
            set
            {
                _percentComplete = value;
            }
        }




        /// <summary>
        /// 
        /// Seconds remaining to complete the operation, or -1 if n/a
        /// 
        /// </summary>
        /// <value></value>

        [Parameter]
        public
        int
        SecondsRemaining
        {
            get
            {
                return _secondsRemaining;
            }
            set
            {
                _secondsRemaining = value;
            }
        }



        /// <summary>
        /// 
        /// Description of current operation in activity, empty if n/a
        /// 
        /// </summary>
        /// <value></value>

        [Parameter]
        public
        string
        CurrentOperation
        {
            get
            {
                return _currentOperation;
            }
            set
            {
                _currentOperation = value;
            }
        }



        /// <summary>
        /// 
        /// Identifies the parent Id of this activity, or -1 if none.
        /// 
        /// </summary>
        /// <value></value>

        [Parameter]
        [ValidateRange(-1, Int32.MaxValue)]
        public
        int
        ParentId
        {
            get
            {
                return _parentId;
            }
            set
            {
                _parentId = value;
            }
        }



        /// <summary>
        /// 
        /// Identifies whether the activity has completed (and the display for it should be removed),
        /// or if it is proceededing (and the display for it should be shown).
        /// 
        /// </summary>
        /// <value></value>

        [Parameter]
        public
        SwitchParameter
        Completed
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
        /// 
        /// Identifies the source of the record.
        /// 
        /// </summary>
        /// <value></value>

        [Parameter]
        public
        int
        SourceId
        {
            get
            {
                return _sourceId;
            }
            set
            {
                _sourceId = value;
            }
        }



        /// <summary>
        /// 
        /// Writes a ProgressRecord created from the parameters.
        /// 
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



        private int _activityId = 0;
        private string _activity;
        private string _status = WriteProgressResourceStrings.Processing;
        private int _percentComplete = -1;
        private int _secondsRemaining = -1;
        private string _currentOperation;
        private int _parentId = -1;
        private int _sourceId;
        private bool _completed;


        private const string HelpMessageBaseName = "WriteProgressResourceStrings";
    }
}



