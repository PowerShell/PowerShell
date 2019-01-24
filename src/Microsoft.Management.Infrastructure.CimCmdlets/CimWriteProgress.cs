// Copyright (c) Microsoft Corporation. All rights reserved.
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
            this.activity = theActivity;
            this.activityID = theActivityID;
            this.currentOperation = theCurrentOperation;
            if (string.IsNullOrEmpty(theStatusDescription))
            {
                this.statusDescription = Strings.DefaultStatusDescription;
            }
            else
            {
                this.statusDescription = theStatusDescription;
            }

            this.percentageCompleted = thePercentageCompleted;
            this.secondsRemaining = theSecondsRemaining;
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
                this.activity,
                this.activityID,
                this.secondsRemaining,
                this.percentageCompleted);

            ValidationHelper.ValidateNoNullArgument(cmdlet, "cmdlet");
            ProgressRecord record = new ProgressRecord(
                this.activityID,
                this.activity,
                this.statusDescription);
            record.Activity = this.activity;
            record.ParentActivityId = 0;
            record.SecondsRemaining = (int)this.secondsRemaining;
            record.PercentComplete = (int)this.percentageCompleted;
            cmdlet.WriteProgress(record);
        }

        #region members

        /// <summary>
        /// Activity of the given activity.
        /// </summary>
        private string activity;

        /// <summary>
        /// Activity identifier of the given activity.
        /// </summary>
        private int activityID;

        /// <summary>
        /// Current operation text of the given activity.
        /// </summary>
        private string currentOperation;

        /// <summary>
        /// Status description of the given activity.
        /// </summary>
        private string statusDescription;

        /// <summary>
        /// Percentage completed of the given activity.
        /// </summary>
        private UInt32 percentageCompleted;

        /// <summary>
        /// How many seconds remained for the given activity.
        /// </summary>
        private UInt32 secondsRemaining;

        internal string Activity
        {
            get { return activity; }
        }

        internal int ActivityID
        {
            get { return activityID; }
        }

        internal string CurrentOperation
        {
            get { return currentOperation; }
        }

        internal string StatusDescription
        {
            get { return statusDescription; }
        }

        internal UInt32 PercentageCompleted
        {
            get { return percentageCompleted; }
        }

        internal UInt32 SecondsRemaining
        {
            get { return secondsRemaining; }
        }

        #endregion
    }
}
