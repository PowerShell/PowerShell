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
        /// Initializes a new instance of the <see cref="CimWriteProgress"/> class.
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
            uint thePercentageCompleted,
            uint theSecondsRemaining)
        {
            this.Activity = theActivity;
            this.ActivityID = theActivityID;
            this.CurrentOperation = theCurrentOperation;
            if (string.IsNullOrEmpty(theStatusDescription))
            {
                this.StatusDescription = CimCmdletStrings.DefaultStatusDescription;
            }
            else
            {
                this.StatusDescription = theStatusDescription;
            }

            this.PercentageCompleted = thePercentageCompleted;
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
                this.ActivityID,
                this.SecondsRemaining,
                this.PercentageCompleted);

            ValidationHelper.ValidateNoNullArgument(cmdlet, "cmdlet");
            ProgressRecord record = new(
                this.ActivityID,
                this.Activity,
                this.StatusDescription);
            record.Activity = this.Activity;
            record.ParentActivityId = 0;
            record.SecondsRemaining = (int)this.SecondsRemaining;
            record.PercentComplete = (int)this.PercentageCompleted;
            cmdlet.WriteProgress(record);
        }

        #region members

        /// <summary>
        /// Gets the activity of the given activity.
        /// </summary>
        internal string Activity { get; }

        /// <summary>
        /// Gets the activity identifier of the given activity.
        /// </summary>
        internal int ActivityID { get; }

        /// <summary>
        /// Gets the current operation text of the given activity.
        /// </summary>
        internal string CurrentOperation { get; }

        /// <summary>
        /// Gets the status description of the given activity.
        /// </summary>
        internal string StatusDescription { get; }

        /// <summary>
        /// Gets the percentage completed of the given activity.
        /// </summary>
        internal uint PercentageCompleted { get; }

        /// <summary>
        /// Gets the number of seconds remaining for the given activity.
        /// </summary>
        internal uint SecondsRemaining { get; }

        #endregion
    }
}
