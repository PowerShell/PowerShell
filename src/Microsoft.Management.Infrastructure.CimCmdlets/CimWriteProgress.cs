// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System;
using System.Management.Automation;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    /// <summary>
    /// <para>
    /// Write progress record of given activity
    /// </para>
    /// </summary>
    internal sealed class CimWriteProgress : CimBaseAction
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="activity">
        ///  Activity identifier of the given activity
        /// </param>
        /// <param name="currentOperation">
        /// current operation description of the given activity
        /// </param>
        /// <param name="statusDescription">
        /// current status description of the given activity
        /// </param>
        /// <param name="percentageCompleted">
        /// percentage completed of the given activity
        /// </param>
        /// <param name="secondsRemaining">
        /// how many seconds remained for the given activity
        /// </param>
        public CimWriteProgress(
            string theActivity,
            int theActivityID,
            string theCurrentOperation,
            string theStatusDescription,
            UInt32 thePercentageCompleted,
            UInt32 theSecondsRemaining)
        {
            this.Activity = theActivity;
            this.activityID = theActivityID;
            this.CurrentOperation = theCurrentOperation;
            if (string.IsNullOrEmpty(theStatusDescription))
            {
                this.StatusDescription = CimCmdletStrings.DefaultStatusDescription;
            }
            else
            {
                this.StatusDescription = theStatusDescription;
            }

            this.percentageCompleted = thePercentageCompleted;
            this.SecondsRemaining = theSecondsRemaining;
        }

        /// <summary>
        /// <para>
        /// Write progress record to powershell
        /// </para>
        /// </summary>
        /// <param name="cmdlet"></param>
        public override void Execute(CmdletOperationBase cmdlet)
        {
            DebugHelper.WriteLog(
                "...Activity {0}: id={1}, remain seconds ={2}, percentage completed = {3}",
                4,
                this.Activity,
                this.activityID,
                this.SecondsRemaining,
                this.percentageCompleted);

            ValidationHelper.ValidateNoNullArgument(cmdlet, "cmdlet");
            ProgressRecord record = new(
                this.activityID,
                this.Activity,
                this.StatusDescription);
            record.Activity = this.Activity;
            record.ParentActivityId = 0;
            record.SecondsRemaining = (int)this.SecondsRemaining;
            record.PercentComplete = (int)this.percentageCompleted;
            cmdlet.WriteProgress(record);
        }

        #region members

        /// <summary>
        /// Activity of the given activity.
        /// </summary>

        /// <summary>
        /// Activity identifier of the given activity.
        /// </summary>
        private readonly int activityID;

        /// <summary>
        /// Percentage completed of the given activity.
        /// </summary>
        private readonly UInt32 percentageCompleted;

        internal string Activity { get; }

        internal int ActivityID
        {
            get { return activityID; }
        }

        internal string CurrentOperation { get; }

        internal string StatusDescription { get; }

        internal UInt32 PercentageCompleted
        {
            get { return percentageCompleted; }
        }

        internal UInt32 SecondsRemaining { get; }

        #endregion
    }
}
