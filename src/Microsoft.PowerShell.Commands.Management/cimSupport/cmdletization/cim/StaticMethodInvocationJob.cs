// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;

namespace Microsoft.PowerShell.Cmdletization.Cim
{
    /// <summary>
    /// Job wrapping invocation of a static CIM method.
    /// </summary>
    internal sealed class StaticMethodInvocationJob : ExtrinsicMethodInvocationJob
    {
        internal StaticMethodInvocationJob(CimJobContext jobContext, MethodInvocationInfo methodInvocationInfo)
                : base(jobContext, false /* passThru */, jobContext.CmdletizationClassName, methodInvocationInfo)
        {
        }

        internal override IObservable<CimMethodResultBase> GetCimOperation()
        {
            if (!this.ShouldProcess())
            {
                return null;
            }

            CimMethodParametersCollection methodParameters = this.GetCimMethodParametersCollection();

            CimOperationOptions operationOptions = this.CreateOperationOptions();
            operationOptions.EnableMethodResultStreaming = true;

            IObservable<CimMethodResultBase> observable = this.JobContext.Session.InvokeMethodAsync(
                this.JobContext.Namespace,
                this.JobContext.ClassNameOrNullIfResourceUriIsUsed,
                this.MethodName,
                methodParameters,
                operationOptions);
            return observable;
        }

        internal override object PassThruObject
        {
            get { return null; }
        }

        internal override CimCustomOptionsDictionary CalculateJobSpecificCustomOptions()
        {
            return CimCustomOptionsDictionary.MergeOptions(
                base.CalculateJobSpecificCustomOptions(),
                this.GetCimInstancesFromArguments());
        }
    }
}
