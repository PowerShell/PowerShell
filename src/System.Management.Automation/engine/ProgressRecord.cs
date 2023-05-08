// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// Defines a data structure used to represent the status of an ongoing operation at a point in time.
    /// </summary>
    /// <remarks>
    /// ProgressRecords are passed to <see cref="System.Management.Automation.Cmdlet.WriteProgress(ProgressRecord)"/>,
    /// which, according to user preference, forwards that information on to the host for rendering to the user.
    /// </remarks>
    /// <seealso cref="System.Management.Automation.Cmdlet.WriteProgress(ProgressRecord)"/>
    [DataContract()]
    public
    class ProgressRecord
    {
        #region Public API

        /// <summary>
        /// Initializes a new instance of the ProgressRecord class and defines the activity Id,
        /// activity description, and status description.
        /// </summary>
        /// <param name="activityId">
        /// A unique numeric key that identifies the activity to which this record applies.
        /// </param>
        /// <param name="activity">
        /// A description of the activity for which progress is being reported.
        /// </param>
        /// <param name="statusDescription">
        /// A description of the status of the activity.
        /// </param>
        public
        ProgressRecord(int activityId, string activity, string statusDescription)
        {
            if (activityId < 0)
            {
                // negative Ids are reserved to indicate "no id" for parent Ids.

                throw PSTraceSource.NewArgumentOutOfRangeException(nameof(activityId), activityId, ProgressRecordStrings.ArgMayNotBeNegative, "activityId");
            }

            if (string.IsNullOrEmpty(activity))
            {
                throw PSTraceSource.NewArgumentException(nameof(activity), ProgressRecordStrings.ArgMayNotBeNullOrEmpty, "activity");
            }

            if (string.IsNullOrEmpty(statusDescription))
            {
                throw PSTraceSource.NewArgumentException(nameof(activity), ProgressRecordStrings.ArgMayNotBeNullOrEmpty, "statusDescription");
            }

            this.id = activityId;
            this.activity = activity;
            this.status = statusDescription;
        }

        /// <summary>
        /// Cloning constructor (all fields are value types - can treat our implementation of cloning as "deep" copy)
        /// </summary>
        /// <param name="other"></param>
        internal ProgressRecord(ProgressRecord other)
        {
            this.activity = other.activity;
            this.currentOperation = other.currentOperation;
            this.id = other.id;
            this.parentId = other.parentId;
            this.percent = other.percent;
            this.secondsRemaining = other.secondsRemaining;
            this.status = other.status;
            this.type = other.type;
        }

        /// <summary>
        /// Gets the Id of the activity to which this record corresponds.  Used as a 'key' for the
        /// linking of subordinate activities.
        /// </summary>
        public
        int
        ActivityId
        {
            get
            {
                return id;
            }
        }

        /// <summary>
        /// Gets and sets the Id of the activity for which this record is a subordinate.
        /// </summary>
        /// <remarks>
        /// Used to allow chaining of progress records (such as when one installation invokes a child installation). UI:
        /// normally not directly visible except as already displayed as its own activity. Usually a sub-activity will be
        /// positioned below and to the right of its parent.
        ///
        /// A negative value (the default) indicates that the activity is not a subordinate.
        ///
        /// May not be the same as ActivityId.
        /// <!--NTRAID#Windows OS Bugs-1161549 the default value for this should be picked up from a variable in the
        /// shell so that a script can set that variable, and have all subsequent calls to WriteProgress (the API) be
        /// subordinate to the "current parent id".-->
        /// </remarks>
        public
        int
        ParentActivityId
        {
            get
            {
                return parentId;
            }

            set
            {
                if (value == ActivityId)
                {
                    throw PSTraceSource.NewArgumentException("value", ProgressRecordStrings.ParentActivityIdCantBeActivityId);
                }

                parentId = value;
            }
        }

        /// <summary>
        /// Gets and sets the description of the activity for which progress is being reported.
        /// </summary>
        /// <remarks>
        /// States the overall intent of whats being accomplished, such as "Recursively removing item c:\temp." Typically
        /// displayed in conjunction with a progress bar.
        /// </remarks>
        public
        string
        Activity
        {
            get
            {
                return activity;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw PSTraceSource.NewArgumentException("value", ProgressRecordStrings.ArgMayNotBeNullOrEmpty, "value");
                }

                activity = value;
            }
        }

        /// <summary>
        /// Gets and sets the current status of the operation, e.g., "35 of 50 items Copied." or "95% completed." or "100 files purged."
        /// </summary>
        public
        string
        StatusDescription
        {
            get
            {
                return status;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw PSTraceSource.NewArgumentException("value", ProgressRecordStrings.ArgMayNotBeNullOrEmpty, "value");
                }

                status = value;
            }
        }

        /// <summary>
        /// Gets and sets the current operation of the many required to accomplish the activity (such as "copying foo.txt"). Normally displayed
        /// below its associated progress bar, e.g., "deleting file foo.bar"
        /// Set to null or empty in the case a sub-activity will be used to show the current operation.
        /// </summary>
        public
        string
        CurrentOperation
        {
            get
            {
                return currentOperation;
            }

            set
            {
                // null or empty string is allowed

                currentOperation = value;
            }
        }

        /// <summary>
        /// Gets and sets the estimate of the percentage of total work for the activity that is completed.  Typically displayed as a progress bar.
        /// Set to a negative value to indicate that the percentage completed should not be displayed.
        /// </summary>
        public
        int
        PercentComplete
        {
            get
            {
                return percent;
            }

            set
            {
                // negative values are allowed

                if (value > 100)
                {
                    throw
                        PSTraceSource.NewArgumentOutOfRangeException(
                            "value", value, ProgressRecordStrings.PercentMayNotBeMoreThan100, "PercentComplete");
                }

                percent = value;
            }
        }

        /// <summary>
        /// Gets and sets the estimate of time remaining until this activity is completed.  This can be based upon a measurement of time since
        /// started and the percent complete or another approach deemed appropriate by the caller.
        ///
        /// Normally displayed beside the progress bar, as "N seconds remaining."
        /// </summary>
        /// <remarks>
        /// A value less than 0 means "don't display a time remaining."
        /// </remarks>
        public
        int
        SecondsRemaining
        {
            get
            {
                return secondsRemaining;
            }

            set
            {
                // negative values are allowed

                secondsRemaining = value;
            }
        }

        /// <summary>
        /// Gets and sets the type of record represented by this instance.
        /// </summary>
        public
        ProgressRecordType
        RecordType
        {
            get
            {
                return type;
            }

            set
            {
                if (value != ProgressRecordType.Completed && value != ProgressRecordType.Processing)
                {
                    throw PSTraceSource.NewArgumentException("value");
                }

                type = value;
            }
        }

        /// <summary>
        /// Overrides <see cref="System.Object.ToString"/>
        /// </summary>
        /// <returns>
        /// "parent = a id = b act = c stat = d cur = e pct = f sec = g type = h" where
        /// a, b, c, d, e, f, and g are the values of ParentActivityId, ActivityId, Activity, StatusDescription,
        /// CurrentOperation, PercentComplete, SecondsRemaining and RecordType properties.
        /// </returns>
        public override
        string
        ToString()
        {
            return
                string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    "parent = {0} id = {1} act = {2} stat = {3} cur = {4} pct = {5} sec = {6} type = {7}",
                    parentId,
                    id,
                    activity,
                    status,
                    currentOperation,
                    percent,
                    secondsRemaining,
                    type);
        }

        #endregion

        #region Helper methods

        internal static int? GetSecondsRemaining(DateTime startTime, double percentageComplete)
        {
            Dbg.Assert(percentageComplete >= 0.0, "Caller should verify percentageComplete >= 0.0");
            Dbg.Assert(percentageComplete <= 1.0, "Caller should verify percentageComplete <= 1.0");
            Dbg.Assert(
                startTime.Kind == DateTimeKind.Utc,
                "DateTime arithmetic should always be done in utc mode [to avoid problems when some operands are calculated right before and right after switching to /from a daylight saving time");

            if ((percentageComplete < 0.00001) || double.IsNaN(percentageComplete))
            {
                return null;
            }

            DateTime now = DateTime.UtcNow;
            Dbg.Assert(startTime <= now, "Caller should pass a valid startTime");
            TimeSpan elapsedTime = now - startTime;

            TimeSpan totalTime;
            try
            {
                totalTime = TimeSpan.FromMilliseconds(elapsedTime.TotalMilliseconds / percentageComplete);
            }
            catch (OverflowException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }

            TimeSpan remainingTime = totalTime - elapsedTime;

            return (int)(remainingTime.TotalSeconds);
        }

        /// <summary>
        /// Returns percentage complete when it is impossible to predict how long an operation might take.
        /// The percentage complete will slowly converge toward 100%.
        /// At the <paramref name="expectedDuration"/> the percentage complete will be 90%.
        /// </summary>
        /// <param name="startTime">When did the operation start.</param>
        /// <param name="expectedDuration">How long does the operation usually take.</param>
        /// <returns>Estimated percentage complete of the operation (always between 0 and 99% - never returns 100%).</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when
        /// 1) <paramref name="startTime"/> is in the future
        /// 2) <paramref name="expectedDuration"/> is negative or zero
        /// </exception>
        internal static int GetPercentageComplete(DateTime startTime, TimeSpan expectedDuration)
        {
            DateTime now = DateTime.UtcNow;

            Dbg.Assert(
                startTime.Kind == DateTimeKind.Utc,
                "DateTime arithmetic should always be done in utc mode [to avoid problems when some operands are calculated right before and right after switching to /from a daylight saving time");

            if (startTime > now)
            {
                throw new ArgumentOutOfRangeException(nameof(startTime));
            }

            if (expectedDuration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(expectedDuration));
            }

            /*
             * According to the spec of Checkpoint-Computer
             * (http://cmdletdesigner/SpecViewer/Default.aspx?Project=PowerShell&Cmdlet=Checkpoint-Computer)
             * we have percentage remaining = f(t) where
             * f(inf) = 0%
             * f(0) = 100%
             * f(90) = <something small> = 10%
             *
             * The spec talks about exponential decay, but function based on 1/x seems better:
             * f(t) = a / (T + b)
             *
             * This by definition has f(inf) = 0, so we have to find a and b for the last 2 cases:
             * E1: f(0) = a / (0 + b) = 100
             * E2: f(T = 90) = a / (T + b) = 10
             *
             * From E1 we have a = 100 * b, which we can use in E2:
             * (100 * b) / (T + b) = 10
             * 100 * b = 10 * T + 10 * b
             * 90 * b = 10 * T
             * b = T / 9
             *
             * Some sample values (for T=90):
             * t   | %rem
             * -----------
             * 0   | 100.0%
             * 5   |  66.6%
             * 10  |  50.0%
             * 30  |  25.0%
             * 70  |  12.5%
             * 90  |  10.0%
             * 300 |   3.2%
             * 600 |   1.6%
             * 3600|   0.2%
             */
            TimeSpan timeElapsed = now - startTime;
            double b = expectedDuration.TotalSeconds / 9.0;
            double a = 100.0 * b;
            double percentageRemaining = a / (timeElapsed.TotalSeconds + b);
            double percentageCompleted = 100.0 - percentageRemaining;

            return (int)Math.Floor(percentageCompleted);
        }

        #endregion

        #region DO NOT REMOVE OR RENAME THESE FIELDS - it will break remoting compatibility with Windows PowerShell

        [DataMemberAttribute()]
        private readonly int id;

        [DataMemberAttribute()]
        private int parentId = -1;

        [DataMemberAttribute()]
        private string activity;

        [DataMemberAttribute()]
        private string status;

        [DataMemberAttribute()]
        private string currentOperation;

        [DataMemberAttribute()]
        private int percent = -1;

        [DataMemberAttribute()]
        private int secondsRemaining = -1;

        [DataMemberAttribute()]
        private ProgressRecordType type = ProgressRecordType.Processing;

        #endregion

        #region Serialization / deserialization for remoting

        /// <summary>
        /// Creates a ProgressRecord object from a PSObject property bag.
        /// PSObject has to be in the format returned by ToPSObjectForRemoting method.
        /// </summary>
        /// <param name="progressAsPSObject">PSObject to rehydrate.</param>
        /// <returns>
        /// ProgressRecord rehydrated from a PSObject property bag
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the PSObject is null.
        /// </exception>
        /// <exception cref="System.Management.Automation.Remoting.PSRemotingDataStructureException">
        /// Thrown when the PSObject is not in the expected format
        /// </exception>
        internal static ProgressRecord FromPSObjectForRemoting(PSObject progressAsPSObject)
        {
            if (progressAsPSObject == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(progressAsPSObject));
            }

            string activity = RemotingDecoder.GetPropertyValue<string>(progressAsPSObject, RemoteDataNameStrings.ProgressRecord_Activity);
            int activityId = RemotingDecoder.GetPropertyValue<int>(progressAsPSObject, RemoteDataNameStrings.ProgressRecord_ActivityId);
            string statusDescription = RemotingDecoder.GetPropertyValue<string>(progressAsPSObject, RemoteDataNameStrings.ProgressRecord_StatusDescription);

            ProgressRecord result = new ProgressRecord(activityId, activity, statusDescription);

            result.CurrentOperation = RemotingDecoder.GetPropertyValue<string>(progressAsPSObject, RemoteDataNameStrings.ProgressRecord_CurrentOperation);
            result.ParentActivityId = RemotingDecoder.GetPropertyValue<int>(progressAsPSObject, RemoteDataNameStrings.ProgressRecord_ParentActivityId);
            result.PercentComplete = RemotingDecoder.GetPropertyValue<int>(progressAsPSObject, RemoteDataNameStrings.ProgressRecord_PercentComplete);
            result.RecordType = RemotingDecoder.GetPropertyValue<ProgressRecordType>(progressAsPSObject, RemoteDataNameStrings.ProgressRecord_Type);
            result.SecondsRemaining = RemotingDecoder.GetPropertyValue<int>(progressAsPSObject, RemoteDataNameStrings.ProgressRecord_SecondsRemaining);

            return result;
        }

        /// <summary>
        /// Returns this object as a PSObject property bag
        /// that can be used in a remoting protocol data object.
        /// </summary>
        /// <returns>This object as a PSObject property bag.</returns>
        internal PSObject ToPSObjectForRemoting()
        {
            PSObject progressAsPSObject = RemotingEncoder.CreateEmptyPSObject();

            progressAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ProgressRecord_Activity, this.Activity));
            progressAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ProgressRecord_ActivityId, this.ActivityId));
            progressAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ProgressRecord_StatusDescription, this.StatusDescription));

            progressAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ProgressRecord_CurrentOperation, this.CurrentOperation));
            progressAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ProgressRecord_ParentActivityId, this.ParentActivityId));
            progressAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ProgressRecord_PercentComplete, this.PercentComplete));
            progressAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ProgressRecord_Type, this.RecordType));
            progressAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ProgressRecord_SecondsRemaining, this.SecondsRemaining));

            return progressAsPSObject;
        }

        #endregion
    }

    /// <summary>
    /// Defines two types of progress record that refer to the beginning (or middle) and end of an operation.
    /// </summary>
    public
    enum ProgressRecordType
    {
        /// <summary>
        /// Operation just started or is not yet complete.
        /// </summary>
        /// <remarks>
        /// A cmdlet can call WriteProgress with ProgressRecordType.Processing
        /// as many times as it wishes.  However, at the end of the operation,
        /// it should call once more with ProgressRecordType.Completed.
        ///
        /// The first time that a host receives a progress record
        /// for a given activity, it will typically display a progress
        /// indicator for that activity.  For each subsequent record
        /// of the same Id, the host will update that display.
        /// Finally, when the host receives a 'completed' record
        /// for that activity, it will remove the progress indicator.
        /// </remarks>
        Processing,

        /// <summary>
        /// Operation is complete.
        /// </summary>
        /// <remarks>
        /// If a cmdlet uses WriteProgress, it should use
        /// ProgressRecordType.Completed exactly once, in the last call
        /// to WriteProgress.
        /// </remarks>
        Completed
    }
}
