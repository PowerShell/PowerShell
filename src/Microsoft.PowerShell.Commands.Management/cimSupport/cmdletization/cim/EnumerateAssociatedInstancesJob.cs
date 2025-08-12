// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Management.Automation;

using Microsoft.Management.Infrastructure;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Cmdletization.Cim
{
    /// <summary>
    /// Job that handles executing a WQL (in the future CQL?) query on a remote CIM server.
    /// </summary>
    internal sealed class EnumerateAssociatedInstancesJob : QueryJobBase
    {
        private readonly CimInstance _associatedObject;
        private readonly string _associationName;
        private readonly string _resultRole;
        private readonly string _sourceRole;

        internal EnumerateAssociatedInstancesJob(CimJobContext jobContext, CimQuery cimQuery, CimInstance associatedObject, string associationName, string resultRole, string sourceRole)
                : base(jobContext, cimQuery)
        {
            _associatedObject = associatedObject;
            Dbg.Assert(_associatedObject != null, "Caller should verify that associatedObject is not null");

            _associationName = associationName;
            Dbg.Assert(_associationName != null, "Caller should verify that associationName is not null");

            _resultRole = resultRole;
            Dbg.Assert(_resultRole != null, "Caller should verify that resultRole is not null");

            _sourceRole = sourceRole;
            Dbg.Assert(_sourceRole != null, "Caller should verify that sourceRole is not null");
        }

        internal override IObservable<CimInstance> GetCimOperation()
        {
            this.WriteVerboseStartOfCimOperation();
            IObservable<CimInstance> observable = this.JobContext.Session.EnumerateAssociatedInstancesAsync(
                this.JobContext.Namespace,
                _associatedObject,
                _associationName,
                this.JobContext.ClassNameOrNullIfResourceUriIsUsed,
                _sourceRole,
                _resultRole,
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
                    _associatedObject.ToString());
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
                _associatedObject);
        }

        internal override void WriteObject(object outputObject)
        {
            if (IsShowComputerNameMarkerPresent(_associatedObject))
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
