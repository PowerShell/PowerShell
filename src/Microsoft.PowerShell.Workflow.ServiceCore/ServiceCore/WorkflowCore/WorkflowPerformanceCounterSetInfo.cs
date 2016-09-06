/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Diagnostics.PerformanceData;
using System.Management.Automation;
using System.Management.Automation.PerformanceData;

namespace Microsoft.PowerShell.Workflow
{
    /// <summary>
    /// PSWorkflowPerformanceCounterSetInfo class contains
    /// the data essential to create an
    /// instance of PSCounterSetRegistrar for monitoring
    /// workflow performance
    /// </summary>
    internal static class PSWorkflowPerformanceCounterSetInfo
    {
        internal static Guid ProviderId = new Guid("{5db760bc-64b2-4da7-b4ef-7dab105fbb8c}");
        /// <summary>
        /// If some other assemblies (e.g. Microsoft.PowerShell.Workflow.Activities) need
        /// access to the counters, then they would need to specify the CounterSetId,
        /// alongwith the counterId. Hence, CounterSetId is public.
        /// </summary>
        internal static Guid CounterSetId = new Guid("{faa17411-9025-4b86-8b5e-ce2f32b06e13}");
        internal static CounterSetInstanceType CounterSetType = CounterSetInstanceType.Multiple;
        internal static CounterInfo[] CounterInfoArray =
            new CounterInfo[]{
                new CounterInfo(PSWorkflowPerformanceCounterIds.FailedWorkflowJobsCount,CounterType.RawData64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.FailedWorkflowJobsPerSec,CounterType.RateOfCountPerSecond64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.ResumedWorkflowJobsCount, CounterType.RawData64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.ResumedWorkflowJobsPerSec, CounterType.RateOfCountPerSecond64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.RunningWorkflowJobsCount, CounterType.RawData64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.RunningWorkflowJobsPerSec, CounterType.RateOfCountPerSecond64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.StoppedWorkflowJobsCount, CounterType.RawData64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.StoppedWorkflowJobsPerSec, CounterType.RateOfCountPerSecond64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.SucceededWorkflowJobsCount, CounterType.RawData64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.SucceededWorkflowJobsPerSec, CounterType.RateOfCountPerSecond64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.SuspendedWorkflowJobsCount, CounterType.RawData64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.SuspendedWorkflowJobsPerSec, CounterType.RateOfCountPerSecond64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.TerminatedWorkflowJobsCount, CounterType.RawData64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.TerminatedWorkflowJobsPerSec, CounterType.RateOfCountPerSecond64),
                //new CounterInfo(PSWorkflowPerformanceCounterIds.TerminatedExceptionWorkflowJobsCount, CounterType.RawData64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.WaitingWorkflowJobsCount, CounterType.RawData64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.ActivityHostMgrBusyProcessesCount, CounterType.RawData64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.ActivityHostMgrFailedRequestsPerSec, CounterType.RateOfCountPerSecond64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.ActivityHostMgrFailedRequestsQueueLength, CounterType.RawData64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.ActivityHostMgrIncomingRequestsPerSec, CounterType.RateOfCountPerSecond64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.ActivityHostMgrPendingRequestsQueueLength, CounterType.RawData64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.ActivityHostMgrCreatedProcessesCount, CounterType.RawData64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.ActivityHostMgrDisposedProcessesCount, CounterType.RawData64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.ActivityHostMgrProcessesPoolSize, CounterType.RawData64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.PSRemotingPendingRequestsQueueLength, CounterType.RawData64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.PSRemotingRequestsBeingServicedCount, CounterType.RawData64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.PSRemotingForcedToWaitRequestsQueueLength, CounterType.RawData64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.PSRemotingConnectionsCreatedCount, CounterType.RawData64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.PSRemotingConnectionsDisposedCount, CounterType.RawData64),
                new CounterInfo(PSWorkflowPerformanceCounterIds.PSRemotingConnectionsClosedReopenedCount, CounterType.RawData64)
            };
    }

    /// <summary>
    /// PSWorkflowPerformanceCounterIds enumerates the 
    /// list of valid performance counter ids related to Powershell Workflow.
    /// NOTE: The prime reason for making this not an enum are as follows:
    /// (1) Everytime the enum will have to be typecasted to an int before invoking any
    /// of the Performance Counters API.
    /// (2) In case of M3P, its possible that some of the perf counters might be updated
    /// by ActivityBase assembly, in which such perf counter ids might need to have
    /// public access modifier, instead of internal.
    /// </summary>
    internal static class PSWorkflowPerformanceCounterIds
    {
        internal const int FailedWorkflowJobsCount = 1;
        internal const int FailedWorkflowJobsPerSec = FailedWorkflowJobsCount + 1;
        internal const int ResumedWorkflowJobsCount = FailedWorkflowJobsPerSec + 1;
        internal const int ResumedWorkflowJobsPerSec = ResumedWorkflowJobsCount + 1;
        internal const int RunningWorkflowJobsCount = ResumedWorkflowJobsPerSec + 1;
        internal const int RunningWorkflowJobsPerSec = RunningWorkflowJobsCount + 1;
        internal const int StoppedWorkflowJobsCount = RunningWorkflowJobsPerSec + 1;
        internal const int StoppedWorkflowJobsPerSec = StoppedWorkflowJobsCount + 1;
        internal const int SucceededWorkflowJobsCount = StoppedWorkflowJobsPerSec + 1;
        internal const int SucceededWorkflowJobsPerSec = SucceededWorkflowJobsCount + 1;
        internal const int SuspendedWorkflowJobsCount = SucceededWorkflowJobsPerSec + 1;
        internal const int SuspendedWorkflowJobsPerSec = SuspendedWorkflowJobsCount + 1;
        internal const int TerminatedWorkflowJobsCount = SuspendedWorkflowJobsPerSec + 1;
        internal const int TerminatedWorkflowJobsPerSec = TerminatedWorkflowJobsCount + 1;
        internal const int WaitingWorkflowJobsCount = TerminatedWorkflowJobsPerSec + 1;
        internal const int ActivityHostMgrBusyProcessesCount = WaitingWorkflowJobsCount + 1;
        internal const int ActivityHostMgrFailedRequestsPerSec = ActivityHostMgrBusyProcessesCount + 1;
        internal const int ActivityHostMgrFailedRequestsQueueLength = ActivityHostMgrFailedRequestsPerSec + 1;
        internal const int ActivityHostMgrIncomingRequestsPerSec = ActivityHostMgrFailedRequestsQueueLength + 1;
        internal const int ActivityHostMgrPendingRequestsQueueLength = ActivityHostMgrIncomingRequestsPerSec + 1;
        internal const int ActivityHostMgrCreatedProcessesCount = ActivityHostMgrPendingRequestsQueueLength + 1;
        internal const int ActivityHostMgrDisposedProcessesCount = ActivityHostMgrCreatedProcessesCount + 1;
        internal const int ActivityHostMgrProcessesPoolSize = ActivityHostMgrDisposedProcessesCount + 1;
        internal const int PSRemotingPendingRequestsQueueLength = ActivityHostMgrProcessesPoolSize + 1;
        internal const int PSRemotingRequestsBeingServicedCount = PSRemotingPendingRequestsQueueLength + 1;
        internal const int PSRemotingForcedToWaitRequestsQueueLength = PSRemotingRequestsBeingServicedCount + 1;
        internal const int PSRemotingConnectionsCreatedCount = PSRemotingForcedToWaitRequestsQueueLength + 1;
        internal const int PSRemotingConnectionsDisposedCount = PSRemotingConnectionsCreatedCount + 1;
        internal const int PSRemotingConnectionsClosedReopenedCount = PSRemotingConnectionsDisposedCount + 1;
    };


}
