/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Globalization;
using System.Management.Automation;
using Microsoft.Management.Infrastructure;
using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Cmdletization.Cim
{
    /// <summary>
    /// Job that handles executing a WQL (in the future CQL?) query on a remote CIM server
    /// </summary>
    internal class EnumerateAssociatedInstancesJob : QueryJobBase
    {
        private readonly CimInstance associatedObject;
        private readonly string associationName;
        private readonly string resultRole;
        private readonly string sourceRole;

        internal EnumerateAssociatedInstancesJob(CimJobContext jobContext, CimQuery cimQuery, CimInstance associatedObject, string associationName, string resultRole, string sourceRole)
                : base(jobContext, cimQuery)
        {
            this.associatedObject = associatedObject;
            Dbg.Assert(this.associatedObject != null, "Caller should verify that associatedObject is not null");

            this.associationName = associationName;
            Dbg.Assert(this.associationName != null, "Caller should verify that associationName is not null");

            this.resultRole = resultRole;
            Dbg.Assert(this.resultRole != null, "Caller should verify that resultRole is not null");

            this.sourceRole = sourceRole;
            Dbg.Assert(this.sourceRole != null, "Caller should verify that sourceRole is not null");
        }

        internal override IObservable<CimInstance> GetCimOperation()
        {
            this.WriteVerboseStartOfCimOperation();
            IObservable<CimInstance> observable = this.JobContext.Session.EnumerateAssociatedInstancesAsync(
                this.JobContext.Namespace,
                this.associatedObject,
                this.associationName,
                this.JobContext.ClassNameOrNullIfResourceUriIsUsed,
                this.sourceRole,
                this.resultRole,
                this.CreateOperationOptions());

            return observable;
        }

        internal override string Description
        {
            get 
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    CmdletizationResources.CimJob_AssociationDescription,
                    this.JobContext.CmdletizationClassName,
                    this.JobContext.Session.ComputerName,
                    this.associatedObject.ToString());
            }
        }

        internal override string FailSafeDescription
        {
            get 
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    CmdletizationResources.CimJob_SafeAssociationDescription,
                    this.JobContext.CmdletizationClassName,
                    this.JobContext.Session.ComputerName);
            }
        }

        internal override CimCustomOptionsDictionary CalculateJobSpecificCustomOptions()
        {
            return CimCustomOptionsDictionary.MergeOptions(
                base.CalculateJobSpecificCustomOptions(),
                this.associatedObject);
        }

        internal override void WriteObject(object outputObject)
        {
            if (IsShowComputerNameMarkerPresent(this.associatedObject))
            {
                PSObject pso = PSObject.AsPSObject(outputObject);
                AddShowComputerNameMarker(pso);
            }
            base.WriteObject(outputObject);
        }

        internal override string GetProviderVersionExpectedByJob()
        {
            // CDXML doesn't allow expressing of separate "ClassVersion" attribute for association operations - Windows 8 Bugs: #642140
            return null;
        }
    }
}
