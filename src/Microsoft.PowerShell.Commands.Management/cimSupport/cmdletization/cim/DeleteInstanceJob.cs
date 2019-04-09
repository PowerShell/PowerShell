// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Management.Infrastructure;
using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Cmdletization.Cim
{
    /// <summary>
    /// Job wrapping invocation of a DeleteInstance intrinsic CIM method.
    /// </summary>
    internal class DeleteInstanceJob : MethodInvocationJobBase<object>
    {
        private readonly CimInstance _objectToDelete;

        internal DeleteInstanceJob(CimJobContext jobContext, bool passThru, CimInstance objectToDelete, MethodInvocationInfo methodInvocationInfo)
                : base(
                    jobContext,
                    passThru,
                    objectToDelete.ToString(),
                    methodInvocationInfo)
        {
            Dbg.Assert(objectToDelete != null, "Caller should verify objectToDelete != null");
            _objectToDelete = objectToDelete;
        }

        internal override IObservable<object> GetCimOperation()
        {
            if (!this.ShouldProcess())
            {
                return null;
            }

            IObservable<object> observable = this.JobContext.Session.DeleteInstanceAsync(
                this.JobContext.Namespace,
                _objectToDelete,
                this.CreateOperationOptions());
            return observable;
        }

        public override void OnNext(object item)
        {
            Dbg.Assert(false, "DeleteInstance should not result in ObjectReady callbacks");
        }

        internal override object PassThruObject
        {
            get { return _objectToDelete; }
        }

        internal override CimCustomOptionsDictionary CalculateJobSpecificCustomOptions()
        {
            return CimCustomOptionsDictionary.MergeOptions(
                base.CalculateJobSpecificCustomOptions(),
                _objectToDelete);
        }
    }
}
