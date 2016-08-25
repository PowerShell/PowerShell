/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

namespace Microsoft.PowerShell.Workflow
{
    using System;
    using System.Activities;
    using System.Activities.Statements;
    using System.Activities.Validation;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Diagnostics.CodeAnalysis;
    using System.Management.Automation.Tracing;
    using Microsoft.PowerShell.Activities;
    using System.Activities.Tracking;
    using System.Text;

    /// <summary>
    /// Contains members that allow the addition of custom extension to 
    /// the PowerShell workflow engine.
    /// </summary>
    public static class PSWorkflowExtensions
    {
        /// <summary>
        /// The custom workflow extensions delegate to use in this engine
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is needed for providing custom powershell extensions dynamically.")]
        static public Func<IEnumerable<object>> CustomHandler { get; set; }
    }

    /// <summary>
    /// Implementing the workflow tracking participant, this will established with communication between activities and hosting
    /// engine to perform additional task like persistence and logging etc.
    /// </summary>
    internal class PSWorkflowTrackingParticipant : TrackingParticipant
    {
        private readonly PowerShellTraceSource Tracer = PowerShellTraceSourceFactory.GetTraceSource();
        private readonly Tracer _structuredTracer = new Tracer();

        private const String participantName = "WorkflowTrackingParticipant";

        private PSWorkflowDebugger _debugger;
        private const string debugBreakActivity = "PowerShellValue<Object>";

        /// <summary>
        /// Default constructor.
        /// </summary>
        internal PSWorkflowTrackingParticipant()
        {
            Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "{0} Created", participantName));
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="debugger">PSWorkflowDebugger</param>
        internal PSWorkflowTrackingParticipant(PSWorkflowDebugger debugger)
        {
            _debugger = debugger;
            Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "{0} Created", participantName));
        }

        /// <summary>
        /// Retrieve each type of tracking record and perform the corresponding functionality.
        /// </summary>
        /// <param name="record">Represents the tracking record.</param>
        /// <param name="timeout">Time out for the tracking to be completed.</param>
        protected override void Track(TrackingRecord record, TimeSpan timeout)
        {
            Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "{0} Emitted trackRecord: {1}  Level: {2}, RecordNumber: {3}", participantName, record.GetType().FullName, record.Level, record.RecordNumber));

            WorkflowInstanceRecord workflowInstanceRecord = record as WorkflowInstanceRecord;
            if (workflowInstanceRecord != null)
            {
                if (_structuredTracer.IsEnabled)
                {
                    if (string.Equals(WorkflowInstanceStates.Persisted, workflowInstanceRecord.State, StringComparison.OrdinalIgnoreCase))
                    {
                        _structuredTracer.WorkflowPersisted(workflowInstanceRecord.InstanceId);
                    }
                    else if (string.Equals(WorkflowInstanceStates.UnhandledException, workflowInstanceRecord.State, StringComparison.OrdinalIgnoreCase))
                    {
                        WorkflowInstanceUnhandledExceptionRecord unhandledRecord = workflowInstanceRecord as WorkflowInstanceUnhandledExceptionRecord;
                        if (unhandledRecord != null)
                        {
                            _structuredTracer.WorkflowActivityExecutionFailed(unhandledRecord.InstanceId,
                                unhandledRecord.FaultSource != null ? unhandledRecord.FaultSource.Name : unhandledRecord.ActivityDefinitionId,
                                System.Management.Automation.Tracing.Tracer.GetExceptionString(unhandledRecord.UnhandledException));
                        }
                    }
                }
                this.ProcessWorkflowInstanceRecord(workflowInstanceRecord);
            }

            ActivityStateRecord activityStateRecord = record as ActivityStateRecord;
            if (activityStateRecord != null)
            {
                if (_structuredTracer.IsEnabled)
                {
                    ActivityInstanceState activityState = ActivityInstanceState.Executing;
                    if (!string.IsNullOrEmpty(activityStateRecord.State)
                        && Enum.TryParse<ActivityInstanceState>(activityStateRecord.State, out activityState))
                    {
                        if (activityState == ActivityInstanceState.Executing)
                        {
                            _structuredTracer.ActivityExecutionQueued(activityStateRecord.InstanceId, activityStateRecord.Activity.Name);
                        }
                    }
                }
                this.ProcessActivityStateRecord(activityStateRecord);
            }

            CustomTrackingRecord customTrackingRecord = record as CustomTrackingRecord;

            if ((customTrackingRecord != null) && (customTrackingRecord.Data.Count > 0))
            {
                this.ProcessCustomTrackingRecord(customTrackingRecord);
            }
        }

        /// <summary>
        /// Process the workflow instance record.
        /// </summary>
        /// <param name="record">Record representing workflow instance record.</param>
        private void ProcessWorkflowInstanceRecord(WorkflowInstanceRecord record)
        {
            Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, " Workflow InstanceID: {0} Workflow instance state: {1}", record.InstanceId, record.State));
        }

        /// <summary>
        /// process the activity state record.
        /// </summary>
        /// <param name="record">Record representing activity state record.</param>
        private void ProcessActivityStateRecord(ActivityStateRecord record)
        {
            IDictionary<String, object> variables = record.Variables;
            StringBuilder vars = new StringBuilder();

            if (variables.Count > 0)
            {
                vars.AppendLine("\n\tVariables:");
                foreach (KeyValuePair<string, object> variable in variables)
                {
                    vars.AppendLine(String.Format(CultureInfo.InvariantCulture, "\t\tName: {0} Value: {1}", variable.Key, variable.Value));
                }
            }

            Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, " :Activity DisplayName: {0} :ActivityInstanceState: {1} {2}", record.Activity.Name, record.State, ((variables.Count > 0) ? vars.ToString() : String.Empty)));
        }

        /// <summary>
        /// Process the custom tracking record. This record will contain the persistence detail.
        /// </summary>
        /// <param name="record">Record representing custom tracking record.</param>
        private void ProcessCustomTrackingRecord(CustomTrackingRecord record)
        {
            Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "\n\tUser Data:"));

            if (_debugger != null &&
                record.Data != null)
            {
                // Get breakpoint info if available.
                object value;
                string bp = null;
                if (record.Data.TryGetValue("DebugSequencePoint", out value))
                {
                    bp = value as string;
                    record.Data.Remove("DebugSequencePoint");
                }

                // Update debugger variables.
                _debugger.UpdateVariables(record.Data);

                // Pass breakpoint info to debugger, which will optionally stop WF activity execution here.
                if (!string.IsNullOrEmpty(bp))
                {
                    try
                    {
                        string[] symbols = bp.Trim('\'', '\"').Split(':');
                        ActivityPosition debuggerBP = new ActivityPosition(
                            symbols[2],                                                 // WF Name
                            Convert.ToInt32(symbols[0], CultureInfo.InvariantCulture),  // Line number
                            Convert.ToInt32(symbols[1], CultureInfo.InvariantCulture)); // Col number

                        // Debugger blocking call if breakpoint hit or debugger stepping is active.
                        _debugger.DebuggerCheck(debuggerBP);
                    }
                    catch (FormatException)
                    { }
                    catch (OverflowException)
                    { }
                }
            }

            foreach (string data in record.Data.Keys)
            {
                Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, " \t\t {0} : {1}", data, record.Data[data]));
            }
        }
    }
}
