// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

using Microsoft.Management.Infrastructure;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Cmdletization.Cim
{
    /// <summary>
    /// Job wrapping invocation of a CreateInstance intrinsic CIM method.
    /// </summary>
    internal sealed class CreateInstanceJob : PropertySettingJob<CimInstance>
    {
        private CimInstance _resultFromCreateInstance;
        private CimInstance _resultFromGetInstance;

        private static CimInstance GetEmptyInstance(CimJobContext jobContext)
        {
            Dbg.Assert(jobContext != null, "Caller should verify jobContext != null");

            var result = new CimInstance(jobContext.ClassName, jobContext.Namespace);
            return result;
        }

        internal CreateInstanceJob(CimJobContext jobContext, MethodInvocationInfo methodInvocationInfo)
                : base(jobContext, true /* passThru */, GetEmptyInstance(jobContext), methodInvocationInfo)
        {
        }

        private IObservable<CimInstance> GetCreateInstanceOperation()
        {
            CimInstance instanceToCreate = GetEmptyInstance(JobContext);
            ModifyLocalCimInstance(instanceToCreate);

            IObservable<CimInstance> observable = this.JobContext.Session.CreateInstanceAsync(
                this.JobContext.Namespace,
                instanceToCreate,
                this.CreateOperationOptions());
            return observable;
        }

        private IObservable<CimInstance> GetGetInstanceOperation()
        {
            Dbg.Assert(_resultFromCreateInstance != null, "GetInstance should only be called after CreteInstance came back with a keyed instance");
            IObservable<CimInstance> observable = this.JobContext.Session.GetInstanceAsync(
                this.JobContext.Namespace,
                _resultFromCreateInstance,
                this.CreateOperationOptions());
            return observable;
        }

#if DEBUG
        private bool _createInstanceOperationGotStarted;
        private bool _getInstanceOperationGotStarted;
#endif

        internal override IObservable<CimInstance> GetCimOperation()
        {
            if (_resultFromCreateInstance == null)
            {
                if (!this.ShouldProcess())
                {
                    return null;
                }

#if DEBUG
                Dbg.Assert(!_getInstanceOperationGotStarted, "CreateInstance should be started *before* GetInstance");
                Dbg.Assert(!_createInstanceOperationGotStarted, "Should not start CreateInstance operation twice");
                _createInstanceOperationGotStarted = true;
#endif
                return GetCreateInstanceOperation();
            }
            else
            {
#if DEBUG
                Dbg.Assert(_createInstanceOperationGotStarted, "GetInstance should be started *after* CreateInstance");
                Dbg.Assert(!_getInstanceOperationGotStarted, "Should not start GetInstance operation twice");
                Dbg.Assert(_resultFromGetInstance == null, "GetInstance operation shouldn't happen twice");
                _getInstanceOperationGotStarted = true;
#endif
                return GetGetInstanceOperation();
            }
        }

        public override void OnNext(CimInstance item)
        {
            Dbg.Assert(item != null, "CreateInstance and GetInstance should never return null");
            if (_resultFromCreateInstance == null)
            {
                _resultFromCreateInstance = item;
            }
            else
            {
                Dbg.Assert(_resultFromGetInstance == null, "GetInstance operation shouldn't happen twice");
                _resultFromGetInstance = item;
            }
        }

        public override void OnError(Exception exception)
        {
            if (this.DidUserSuppressTheOperation)
            {
                // If user suppressed CreateInstance operation, then no instance should be returned by the cmdlet
                // If the provider's CreateInstance implementation doesn't post an instance and returns a success, then WMI infra will error out to flag an incorrect implementation of CreateInstance (by design)
                // Therefore cmdletization layer has to suppress the error and treat this as normal/successful completion
                this.OnCompleted();
            }
            else
            {
                base.OnError(exception);
            }
        }

        public override void OnCompleted()
        {
            Dbg.Assert(this.DidUserSuppressTheOperation || (_resultFromCreateInstance != null), "OnNext should always be called before OnComplete by CreateInstance");
#if DEBUG
            Dbg.Assert(
                !_getInstanceOperationGotStarted || this.DidUserSuppressTheOperation || (_resultFromGetInstance != null),
                // <=> (this._getInstanceOperationGotStarted => (this._resultFromGetInstance != null))
                "GetInstance should cause OnNext to be called which should set this._resultFromGetInstance to non-null");
#endif
            if (this.IsPassThruObjectNeeded() && (_resultFromGetInstance == null))
            {
                IObservable<CimInstance> observable = this.GetGetInstanceOperation();
                observable.Subscribe(this);
                return;
            }
            else
            {
                base.OnCompleted();
            }
        }

        internal override object PassThruObject
        {
            get
            {
                return _resultFromGetInstance;
            }
        }
    }
}
