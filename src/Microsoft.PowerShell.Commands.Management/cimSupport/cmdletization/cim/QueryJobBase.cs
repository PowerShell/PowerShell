// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

using Microsoft.Management.Infrastructure;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Cmdletization.Cim
{
    /// <summary>
    /// Base job for queries.
    /// </summary>
    internal abstract class QueryJobBase : CimChildJobBase<CimInstance>
    {
        private readonly CimQuery _cimQuery;

        internal QueryJobBase(CimJobContext jobContext, CimQuery cimQuery)
                : base(jobContext)
        {
            Dbg.Assert(cimQuery != null, "Caller should verify cimQuery != null");
            _cimQuery = cimQuery;
        }

        public override void OnNext(CimInstance item)
        {
            this.ExceptionSafeWrapper(
                    delegate
                    {
                        Dbg.Assert(item != null, "When OnNext is called from our IObservable, item parameter should always be != null");
                        if (item == null)
                        {
                            return;
                        }

                        if (!_cimQuery.IsMatchingResult(item))
                        {
                            return;
                        }

                        this.WriteObject(item);
                    });
        }

        public override void OnCompleted()
        {
            this.ExceptionSafeWrapper(
                    delegate
                    {
                        foreach (ClientSideQuery.NotFoundError notFoundError in _cimQuery.GenerateNotFoundErrors())
                        {
                            string errorId = "CmdletizationQuery_NotFound";
                            if (!string.IsNullOrEmpty(notFoundError.PropertyName))
                            {
                                errorId = errorId + "_" + notFoundError.PropertyName;
                            }

                            CimJobException cimJobException = CimJobException.CreateWithFullControl(
                                this.JobContext,
                                notFoundError.ErrorMessageGenerator(this.Description, this.JobContext.ClassName),
                                errorId,
                                ErrorCategory.ObjectNotFound);
                            if (!string.IsNullOrEmpty(notFoundError.PropertyName))
                            {
                                cimJobException.ErrorRecord.SetTargetObject(notFoundError.PropertyValue);
                            }

                            this.WriteError(cimJobException.ErrorRecord);
                        }
                    });
            base.OnCompleted();
        }

        internal override CimCustomOptionsDictionary CalculateJobSpecificCustomOptions()
        {
            return CimCustomOptionsDictionary.Create(_cimQuery.queryOptions);
        }
    }
}
