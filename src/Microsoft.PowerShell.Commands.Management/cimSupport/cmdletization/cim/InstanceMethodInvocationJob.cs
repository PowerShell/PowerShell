/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;
using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Cmdletization.Cim
{
    /// <summary>
    /// Job wrapping invocation of an extrinsic CIM method
    /// </summary>
    internal class InstanceMethodInvocationJob : ExtrinsicMethodInvocationJob
    {
        private readonly CimInstance targetInstance;

        internal InstanceMethodInvocationJob(CimJobContext jobContext, bool passThru, CimInstance targetInstance, MethodInvocationInfo methodInvocationInfo)
                : base(
                    jobContext, 
                    passThru, 
                    targetInstance.ToString(),
                    methodInvocationInfo)
        {
            Dbg.Assert(targetInstance != null, "Caller should verify targetInstance != null");
            this.targetInstance = targetInstance;
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
                this.targetInstance,
                this.MethodName,
                methodParameters,
                operationOptions);
            return observable;
        }

        internal override object PassThruObject
        {
            get { return this.targetInstance; }
        }

        internal override CimCustomOptionsDictionary CalculateJobSpecificCustomOptions()
        {
            return CimCustomOptionsDictionary.MergeOptions(
                base.CalculateJobSpecificCustomOptions(),
                this.targetInstance);
        }
    }
}
