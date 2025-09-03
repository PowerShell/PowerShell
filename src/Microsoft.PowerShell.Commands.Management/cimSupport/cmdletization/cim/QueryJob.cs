// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Text;

using Microsoft.Management.Infrastructure;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Cmdletization.Cim
{
    /// <summary>
    /// Job that handles executing a WQL (in the future CQL?) query on a remote CIM server.
    /// </summary>
    internal sealed class QueryInstancesJob : QueryJobBase
    {
        private readonly string _wqlQuery;
        private readonly bool _useEnumerateInstances;

        internal QueryInstancesJob(CimJobContext jobContext, CimQuery cimQuery, string wqlCondition)
                : base(jobContext, cimQuery)
        {
            Dbg.Assert(wqlCondition != null, "Caller should verify that wqlCondition is not null");

            var wqlQueryBuilder = new StringBuilder();
            wqlQueryBuilder.Append("SELECT * FROM ");
            wqlQueryBuilder.Append(this.JobContext.ClassName);
            wqlQueryBuilder.Append(' ');
            wqlQueryBuilder.Append(wqlCondition);
            _wqlQuery = wqlQueryBuilder.ToString();

            if (string.IsNullOrWhiteSpace(wqlCondition))
            {
                _useEnumerateInstances = true;
            }
            else
            {
                if (jobContext.CmdletInvocationContext.CmdletDefinitionContext.UseEnumerateInstancesInsteadOfWql)
                {
                    _useEnumerateInstances = true;
                }
            }
        }

        internal override IObservable<CimInstance> GetCimOperation()
        {
            this.WriteVerboseStartOfCimOperation();

            IObservable<CimInstance> observable;
            if (_useEnumerateInstances)
            {
                observable = this.JobContext.Session.EnumerateInstancesAsync(
                    this.JobContext.Namespace,
                    this.JobContext.ClassNameOrNullIfResourceUriIsUsed,
                    this.CreateOperationOptions());
            }
            else
            {
                observable = this.JobContext.Session.QueryInstancesAsync(
                    this.JobContext.Namespace,
                    "WQL",
                    _wqlQuery,
                    this.CreateOperationOptions());
            }

            return observable;
        }

        internal override string Description
        {
            get
            {
                return this.FailSafeDescription;
            }
        }

        internal override string FailSafeDescription
        {
            get
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    CmdletizationResources.CimJob_SafeQueryDescription,
                    this.JobContext.CmdletizationClassName,
                    this.JobContext.Session.ComputerName,
                    _wqlQuery);
            }
        }
    }
}
