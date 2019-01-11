// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;
using Microsoft.PowerShell.Cim;
using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Cmdletization.Cim
{
    /// <summary>
    /// Job wrapping invocation of an extrinsic CIM method.
    /// </summary>
    internal abstract class MethodInvocationJobBase<T> : CimChildJobBase<T>
    {
        internal MethodInvocationJobBase(CimJobContext jobContext, bool passThru, string methodSubject, MethodInvocationInfo methodInvocationInfo)
                : base(jobContext)
        {
            Dbg.Assert(methodInvocationInfo != null, "Caller should verify methodInvocationInfo != null");
            Dbg.Assert(methodSubject != null, "Caller should verify methodSubject != null");

            _passThru = passThru;
            MethodSubject = methodSubject;
            _methodInvocationInfo = methodInvocationInfo;
        }

        private readonly bool _passThru;
        private readonly MethodInvocationInfo _methodInvocationInfo;

        internal string MethodName
        {
            get { return _methodInvocationInfo.MethodName; }
        }

        private const string CustomOperationOptionPrefix = "cim:operationOption:";

        private IEnumerable<MethodParameter> GetMethodInputParametersCore(Func<MethodParameter, bool> filter)
        {
            IEnumerable<MethodParameter> inputParameters = _methodInvocationInfo.Parameters.Where(filter);

            var result = new List<MethodParameter>();
            foreach (MethodParameter inputParameter in inputParameters)
            {
                object cimValue = CimSensitiveValueConverter.ConvertFromDotNetToCim(inputParameter.Value);
                Type cimType = CimSensitiveValueConverter.GetCimType(inputParameter.ParameterType);
                CimValueConverter.AssertIntrinsicCimType(cimType);
                result.Add(new MethodParameter
                {
                    Name = inputParameter.Name,
                    ParameterType = cimType,
                    Bindings = inputParameter.Bindings,
                    Value = cimValue,
                    IsValuePresent = inputParameter.IsValuePresent
                });
            }

            return result;
        }

        internal IEnumerable<MethodParameter> GetMethodInputParameters()
        {
            var allMethodParameters = this.GetMethodInputParametersCore(p => !p.Name.StartsWith(CustomOperationOptionPrefix, StringComparison.OrdinalIgnoreCase));
            var methodParametersWithInputValue = allMethodParameters.Where(p => p.IsValuePresent);
            return methodParametersWithInputValue;
        }

        internal IEnumerable<CimInstance> GetCimInstancesFromArguments()
        {
            return _methodInvocationInfo.GetArgumentsOfType<CimInstance>();
        }

        internal override CimCustomOptionsDictionary CalculateJobSpecificCustomOptions()
        {
            IDictionary<string, object> result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            IEnumerable<MethodParameter> customOptions = this
                .GetMethodInputParametersCore(p => p.Name.StartsWith(CustomOperationOptionPrefix, StringComparison.OrdinalIgnoreCase));
            foreach (MethodParameter customOption in customOptions)
            {
                if (customOption.Value == null)
                {
                    continue;
                }

                result.Add(customOption.Name.Substring(CustomOperationOptionPrefix.Length), customOption.Value);
            }

            return CimCustomOptionsDictionary.Create(result);
        }

        internal IEnumerable<MethodParameter> GetMethodOutputParameters()
        {
            IEnumerable<MethodParameter> allParameters_plus_returnValue = _methodInvocationInfo.Parameters;
            if (_methodInvocationInfo.ReturnValue != null)
            {
                allParameters_plus_returnValue = allParameters_plus_returnValue.Append(_methodInvocationInfo.ReturnValue);
            }

            var outParameters = allParameters_plus_returnValue
                .Where(p => (0 != (p.Bindings & (MethodParameterBindings.Out | MethodParameterBindings.Error))));

            return outParameters;
        }

        internal string MethodSubject { get; }

        internal bool ShouldProcess()
        {
            Dbg.Assert(this.MethodSubject != null, "MethodSubject property should be initialized before starting main job processing");
            if (!this.JobContext.CmdletInvocationContext.CmdletDefinitionContext.ClientSideShouldProcess)
            {
                return true;
            }

            bool shouldProcess;
            if (!this.JobContext.SupportsShouldProcess)
            {
                shouldProcess = true;
                this.WriteVerboseStartOfCimOperation();
            }
            else
            {
                string target = this.MethodSubject;
                string action = this.MethodName;
                CimResponseType cimResponseType = this.ShouldProcess(target, action);
                switch (cimResponseType)
                {
                    case CimResponseType.Yes:
                    case CimResponseType.YesToAll:
                        shouldProcess = true;
                        break;
                    default:
                        shouldProcess = false;
                        break;
                }
            }

            if (!shouldProcess)
            {
                this.SetCompletedJobState(JobState.Completed, null);
            }

            return shouldProcess;
        }

        #region PassThru functionality

        internal abstract object PassThruObject { get; }

        internal bool IsPassThruObjectNeeded()
        {
            return (_passThru) && (!this.DidUserSuppressTheOperation) && (!this.JobHadErrors);
        }

        public override void OnCompleted()
        {
            this.ExceptionSafeWrapper(
                    delegate
                    {
                        Dbg.Assert(this.MethodSubject != null, "MethodSubject property should be initialized before starting main job processing");

                        if (this.IsPassThruObjectNeeded())
                        {
                            object passThruObject = this.PassThruObject;
                            if (passThruObject != null)
                            {
                                this.WriteObject(passThruObject);
                            }
                        }
                    });

            base.OnCompleted();
        }

        #endregion

        #region Job descriptions

        internal override string Description
        {
            get
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    CmdletizationResources.CimJob_MethodDescription,
                    this.MethodSubject,
                    this.MethodName);
            }
        }

        internal override string FailSafeDescription
        {
            get
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    CmdletizationResources.CimJob_SafeMethodDescription,
                    this.JobContext.CmdletizationClassName,
                    this.JobContext.Session.ComputerName,
                    this.MethodName);
            }
        }

        #endregion
    }
}
