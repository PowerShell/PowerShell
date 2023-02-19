// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Cmdlet to get available list of results.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Job", DefaultParameterSetName = JobCmdletBase.SessionIdParameterSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096582")]
    [OutputType(typeof(Job))]
    public class GetJobCommand : JobCmdletBase
    {
        #region Parameters

        /// <summary>
        /// IncludeChildJob parameter.
        /// </summary>
        [Parameter(ParameterSetName = JobCmdletBase.SessionIdParameterSet)]
        [Parameter(ParameterSetName = JobCmdletBase.InstanceIdParameterSet)]
        [Parameter(ParameterSetName = JobCmdletBase.NameParameterSet)]
        [Parameter(ParameterSetName = JobCmdletBase.StateParameterSet)]
        [Parameter(ParameterSetName = JobCmdletBase.CommandParameterSet)]
        public SwitchParameter IncludeChildJob { get; set; }

        /// <summary>
        /// ChildJobState parameter.
        /// </summary>
        [Parameter(ParameterSetName = JobCmdletBase.SessionIdParameterSet)]
        [Parameter(ParameterSetName = JobCmdletBase.InstanceIdParameterSet)]
        [Parameter(ParameterSetName = JobCmdletBase.NameParameterSet)]
        [Parameter(ParameterSetName = JobCmdletBase.StateParameterSet)]
        [Parameter(ParameterSetName = JobCmdletBase.CommandParameterSet)]
        public JobState ChildJobState { get; set; }

        /// <summary>
        /// HasMoreData parameter.
        /// </summary>
        [Parameter(ParameterSetName = JobCmdletBase.SessionIdParameterSet)]
        [Parameter(ParameterSetName = JobCmdletBase.InstanceIdParameterSet)]
        [Parameter(ParameterSetName = JobCmdletBase.NameParameterSet)]
        [Parameter(ParameterSetName = JobCmdletBase.StateParameterSet)]
        [Parameter(ParameterSetName = JobCmdletBase.CommandParameterSet)]
        public bool HasMoreData { get; set; }

        /// <summary>
        /// Before time filter.
        /// </summary>
        [Parameter(ParameterSetName = JobCmdletBase.SessionIdParameterSet)]
        [Parameter(ParameterSetName = JobCmdletBase.InstanceIdParameterSet)]
        [Parameter(ParameterSetName = JobCmdletBase.NameParameterSet)]
        [Parameter(ParameterSetName = JobCmdletBase.StateParameterSet)]
        [Parameter(ParameterSetName = JobCmdletBase.CommandParameterSet)]
        public DateTime Before { get; set; }

        /// <summary>
        /// After time filter.
        /// </summary>
        [Parameter(ParameterSetName = JobCmdletBase.SessionIdParameterSet)]
        [Parameter(ParameterSetName = JobCmdletBase.InstanceIdParameterSet)]
        [Parameter(ParameterSetName = JobCmdletBase.NameParameterSet)]
        [Parameter(ParameterSetName = JobCmdletBase.StateParameterSet)]
        [Parameter(ParameterSetName = JobCmdletBase.CommandParameterSet)]
        public DateTime After { get; set; }

        /// <summary>
        /// Newest returned count.
        /// </summary>
        [Parameter(ParameterSetName = JobCmdletBase.SessionIdParameterSet)]
        [Parameter(ParameterSetName = JobCmdletBase.InstanceIdParameterSet)]
        [Parameter(ParameterSetName = JobCmdletBase.NameParameterSet)]
        [Parameter(ParameterSetName = JobCmdletBase.StateParameterSet)]
        [Parameter(ParameterSetName = JobCmdletBase.CommandParameterSet)]
        public int Newest { get; set; }

        /// <summary>
        /// SessionId for which job
        /// need to be obtained.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true, Position = 0,
                  ParameterSetName = JobCmdletBase.SessionIdParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public override int[] Id
        {
            get
            {
                return base.Id;
            }

            set
            {
                base.Id = value;
            }
        }

        #endregion Parameters

        #region Overrides

        /// <summary>
        /// Extract result objects corresponding to the specified
        /// names or expressions.
        /// </summary>
        protected override void ProcessRecord()
        {
            List<Job> jobList = FindJobs();

            jobList.Sort(static (x, y) => x != null ? x.Id.CompareTo(y != null ? y.Id : 1) : -1);
            WriteObject(jobList, true);
        }

        #endregion Overrides

        #region Protected Members

        /// <summary>
        /// Helper method to find jobs based on parameter set.
        /// </summary>
        /// <returns>Matching jobs.</returns>
        protected List<Job> FindJobs()
        {
            List<Job> jobList = new List<Job>();

            switch (ParameterSetName)
            {
                case NameParameterSet:
                    {
                        jobList.AddRange(FindJobsMatchingByName(true, false, true, false));
                    }

                    break;

                case InstanceIdParameterSet:
                    {
                        jobList.AddRange(FindJobsMatchingByInstanceId(true, false, true, false));
                    }

                    break;

                case SessionIdParameterSet:
                    {
                        if (Id != null)
                        {
                            jobList.AddRange(FindJobsMatchingBySessionId(true, false, true, false));
                        }
                        else
                        {
                            // Get-Job with no filter.
                            jobList.AddRange(JobRepository.Jobs);
                            jobList.AddRange(JobManager.GetJobs(this, true, false, null));
                        }
                    }

                    break;

                case CommandParameterSet:
                    {
                        jobList.AddRange(FindJobsMatchingByCommand(false));
                    }

                    break;

                case StateParameterSet:
                    {
                        jobList.AddRange(FindJobsMatchingByState(false));
                    }

                    break;

                case FilterParameterSet:
                    {
                        jobList.AddRange(FindJobsMatchingByFilter(false));
                    }

                    break;

                default:
                    break;
            }

            jobList.AddRange(FindChildJobs(jobList));

            jobList = ApplyHasMoreDataFiltering(jobList);

            return ApplyTimeFiltering(jobList);
        }

        #endregion

        #region Private Members

        /// <summary>
        /// Filter jobs based on HasMoreData.
        /// </summary>
        /// <param name="jobList"></param>
        /// <returns>Return the list of jobs after applying HasMoreData filter.</returns>
        private List<Job> ApplyHasMoreDataFiltering(List<Job> jobList)
        {
            bool hasMoreDataParameter = MyInvocation.BoundParameters.ContainsKey(nameof(HasMoreData));

            if (!hasMoreDataParameter)
            {
                return jobList;
            }

            List<Job> matches = new List<Job>();

            foreach (Job job in jobList)
            {
                if (job.HasMoreData == HasMoreData)
                {
                    matches.Add(job);
                }
            }

            return matches;
        }

        /// <summary>
        /// Find the all child jobs with specified ChildJobState in the job list.
        /// </summary>
        /// <param name="jobList"></param>
        /// <returns>Returns job list including all child jobs with ChildJobState or all if IncludeChildJob is specified.</returns>
        private List<Job> FindChildJobs(List<Job> jobList)
        {
            bool childJobStateParameter = MyInvocation.BoundParameters.ContainsKey(nameof(ChildJobState));
            bool includeChildJobParameter = MyInvocation.BoundParameters.ContainsKey(nameof(IncludeChildJob));

            List<Job> matches = new List<Job>();

            if (!childJobStateParameter && !includeChildJobParameter)
            {
                return matches;
            }

            // add all child jobs if ChildJobState is not specified
            //
            if (!childJobStateParameter && includeChildJobParameter)
            {
                foreach (Job job in jobList)
                {
                    if (job.ChildJobs != null && job.ChildJobs.Count > 0)
                    {
                        matches.AddRange(job.ChildJobs);
                    }
                }
            }
            else
            {
                foreach (Job job in jobList)
                {
                    foreach (Job childJob in job.ChildJobs)
                    {
                        if (childJob.JobStateInfo.State != ChildJobState) continue;

                        matches.Add(childJob);
                    }
                }
            }

            return matches;
        }

        /// <summary>
        /// Applies the appropriate time filter to each job in the job list.
        /// Only Job2 type jobs can be time filtered so older Job types are skipped.
        /// </summary>
        /// <param name="jobList"></param>
        /// <returns></returns>
        private List<Job> ApplyTimeFiltering(List<Job> jobList)
        {
            bool beforeParameter = MyInvocation.BoundParameters.ContainsKey(nameof(Before));
            bool afterParameter = MyInvocation.BoundParameters.ContainsKey(nameof(After));
            bool newestParameter = MyInvocation.BoundParameters.ContainsKey(nameof(Newest));

            if (!beforeParameter && !afterParameter && !newestParameter)
            {
                return jobList;
            }

            // Apply filtering.
            List<Job> filteredJobs;
            if (beforeParameter || afterParameter)
            {
                filteredJobs = new List<Job>();
                foreach (Job job in jobList)
                {
                    if (job.PSEndTime == DateTime.MinValue)
                    {
                        // Skip invalid dates.
                        continue;
                    }

                    if (beforeParameter && afterParameter)
                    {
                        if (job.PSEndTime < Before &&
                            job.PSEndTime > After)
                        {
                            filteredJobs.Add(job);
                        }
                    }
                    else if ((beforeParameter &&
                              job.PSEndTime < Before) ||
                             (afterParameter &&
                              job.PSEndTime > After))
                    {
                        filteredJobs.Add(job);
                    }
                }
            }
            else
            {
                filteredJobs = jobList;
            }

            if (!newestParameter ||
                filteredJobs.Count == 0)
            {
                return filteredJobs;
            }

            //
            // Apply Newest count.
            //

            // Sort filtered jobs
            filteredJobs.Sort((firstJob, secondJob) =>
                {
                    if (firstJob.PSEndTime > secondJob.PSEndTime)
                    {
                        return -1;
                    }
                    else if (firstJob.PSEndTime < secondJob.PSEndTime)
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                });

            List<Job> newestJobs = new List<Job>();
            int count = 0;
            foreach (Job job in filteredJobs)
            {
                if (++count > Newest)
                {
                    break;
                }

                if (!newestJobs.Contains(job))
                {
                    newestJobs.Add(job);
                }
            }

            return newestJobs;
        }

        #endregion Private Members
    }
}
