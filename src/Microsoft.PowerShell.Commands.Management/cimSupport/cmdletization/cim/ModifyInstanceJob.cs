// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using Microsoft.Management.Infrastructure;
using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Cmdletization.Cim
{
    /// <summary>
    /// Job wrapping invocation of a ModifyInstance intrinsic CIM method.
    /// </summary>
    internal class ModifyInstanceJob : PropertySettingJob<CimInstance>
    {
        private CimInstance _resultFromModifyInstance;
        private bool _resultFromModifyInstanceHasBeenPassedThru;
        private readonly CimInstance _originalInstance;
        private CimInstance _temporaryInstance;

        internal ModifyInstanceJob(CimJobContext jobContext, bool passThru, CimInstance managementObject, MethodInvocationInfo methodInvocationInfo)
                : base(jobContext, passThru, managementObject, methodInvocationInfo)
        {
            Dbg.Assert(this.MethodSubject != null, "Caller should verify managementObject != null");
            _originalInstance = managementObject;
        }

        internal override IObservable<CimInstance> GetCimOperation()
        {
            if (!this.ShouldProcess())
            {
                return null;
            }

            _temporaryInstance = new CimInstance(_originalInstance);
            ModifyLocalCimInstance(_temporaryInstance);

            IObservable<CimInstance> observable = this.JobContext.Session.ModifyInstanceAsync(
                this.JobContext.Namespace,
                _temporaryInstance,
                this.CreateOperationOptions());
            return observable;
        }

        public override void OnNext(CimInstance item)
        {
            Dbg.Assert(item != null, "ModifyInstance and GetInstance should not return a null instance");
            _resultFromModifyInstance = item;
        }

        public override void OnCompleted()
        {
            Dbg.Assert(_resultFromModifyInstance != null, "ModifyInstance should return an instance over DCOM and WSMan");
            ModifyLocalCimInstance(_originalInstance); /* modify input CimInstance only upon success (fix for bug WinBlue #) */
            base.OnCompleted();
        }

        internal override object PassThruObject
        {
            get
            {
                Dbg.Assert(_resultFromModifyInstance != null, "ModifyInstance should return an instance over DCOM and WSMan");
                if (IsShowComputerNameMarkerPresent(_originalInstance))
                {
                    PSObject pso = PSObject.AsPSObject(_resultFromModifyInstance);
                    AddShowComputerNameMarker(pso);
                }

                _resultFromModifyInstanceHasBeenPassedThru = true;
                return _resultFromModifyInstance;
            }
        }

        internal override CimCustomOptionsDictionary CalculateJobSpecificCustomOptions()
        {
            return CimCustomOptionsDictionary.MergeOptions(
                base.CalculateJobSpecificCustomOptions(),
                _originalInstance);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_resultFromModifyInstanceHasBeenPassedThru && _resultFromModifyInstance != null)
            {
                _resultFromModifyInstance.Dispose();
                _resultFromModifyInstance = null;
            }

            if (_temporaryInstance != null)
            {
                _temporaryInstance.Dispose();
                _temporaryInstance = null;
            }

            base.Dispose(disposing);
        }
    }
}
