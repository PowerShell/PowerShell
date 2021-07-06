// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;

using Microsoft.Management.Infrastructure;
using Microsoft.PowerShell.Cim;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Cmdletization.Cim
{
    /// <summary>
    /// Job wrapping invocation of an extrinsic CIM method.
    /// </summary>
    internal abstract class ExtrinsicMethodInvocationJob : MethodInvocationJobBase<CimMethodResultBase>
    {
        internal ExtrinsicMethodInvocationJob(CimJobContext jobContext, bool passThru, string methodSubject, MethodInvocationInfo methodInvocationInfo)
                : base(jobContext, passThru, methodSubject, methodInvocationInfo)
        {
        }

        #region Processing of "in" parameters

        internal CimMethodParametersCollection GetCimMethodParametersCollection()
        {
            var methodParameters = new CimMethodParametersCollection();
            foreach (MethodParameter parameter in this.GetMethodInputParameters())
            {
                CimValueConverter.AssertIntrinsicCimType(parameter.ParameterType);
                var methodParameter = CimMethodParameter.Create(
                    parameter.Name,
                    parameter.Value,
                    CimValueConverter.GetCimTypeEnum(parameter.ParameterType),
                    CimFlags.None);
                methodParameters.Add(methodParameter);
            }

            return methodParameters;
        }

        #endregion

        #region Processing of "out" parameters

        private void ProcessOutParameter(CimMethodResult methodResult, MethodParameter methodParameter, IDictionary<string, MethodParameter> cmdletOutput)
        {
            Dbg.Assert(methodResult != null, "Caller should verify methodResult != null");
            Dbg.Assert(methodParameter != null, "Caller should verify methodParameter != null");
            Dbg.Assert((methodParameter.Bindings & (MethodParameterBindings.Out | MethodParameterBindings.Error)) != 0, "Caller should verify that this is an out parameter");
            Dbg.Assert(cmdletOutput != null, "Caller should verify cmdletOutput != null");

            Dbg.Assert(this.MethodSubject != null, "MethodSubject property should be initialized before starting main job processing");

            CimMethodParameter outParameter = methodResult.OutParameters[methodParameter.Name];
            object valueReturnedFromMethod = outParameter?.Value;

            object dotNetValue = CimValueConverter.ConvertFromCimToDotNet(valueReturnedFromMethod, methodParameter.ParameterType);
            if ((methodParameter.Bindings & MethodParameterBindings.Out) == MethodParameterBindings.Out)
            {
                methodParameter.Value = dotNetValue;
                cmdletOutput.Add(methodParameter.Name, methodParameter);

                var cimInstances = dotNetValue as CimInstance[];
                if (cimInstances != null)
                {
                    foreach (var instance in cimInstances)
                    {
                        CimCmdletAdapter.AssociateSessionOfOriginWithInstance(instance, this.JobContext.Session);
                    }
                }

                var cimInstance = dotNetValue as CimInstance;
                if (cimInstance != null)
                {
                    CimCmdletAdapter.AssociateSessionOfOriginWithInstance(cimInstance, this.JobContext.Session);
                }
            }
            else if ((methodParameter.Bindings & MethodParameterBindings.Error) == MethodParameterBindings.Error)
            {
                var gotError = (bool)LanguagePrimitives.ConvertTo(dotNetValue, typeof(bool), CultureInfo.InvariantCulture);
                if (gotError)
                {
                    var errorCodeAsString = (string)LanguagePrimitives.ConvertTo(dotNetValue, typeof(string), CultureInfo.InvariantCulture);
                    CimJobException cje = CimJobException.CreateFromMethodErrorCode(this.GetDescription(), this.JobContext, this.MethodName, errorCodeAsString);
                    throw cje;
                }
            }
        }

        private void OnNext(CimMethodResult methodResult)
        {
            Dbg.Assert(this.MethodSubject != null, "MethodSubject property should be initialized before starting main job processing");

            var cmdletOutput = new Dictionary<string, MethodParameter>(StringComparer.OrdinalIgnoreCase);
            foreach (MethodParameter methodParameter in this.GetMethodOutputParameters())
            {
                ProcessOutParameter(methodResult, methodParameter, cmdletOutput);
            }

            if (cmdletOutput.Count == 1)
            {
                var singleOutputParameter = cmdletOutput.Values.First();
                if (singleOutputParameter.Value == null)
                {
                    return;
                }

                IEnumerable enumerable = LanguagePrimitives.GetEnumerable(singleOutputParameter.Value);
                if (enumerable != null)
                {
                    foreach (object o in enumerable)
                    {
                        this.WriteObject(o, singleOutputParameter);
                    }
                }
                else
                {
                    this.WriteObject(singleOutputParameter.Value, singleOutputParameter);
                }
            }
            else if (cmdletOutput.Count > 1)
            {
                var propertyBag = new PSObject();
                foreach (var element in cmdletOutput)
                {
                    var tmp = new PSNoteProperty(element.Key, element.Value.Value);
                    propertyBag.Properties.Add(tmp);
                }

                this.WriteObject(propertyBag);
            }
        }

        private void OnNext(CimMethodStreamedResult streamedResult)
        {
            MethodParameter methodParameter = this.GetMethodOutputParameters()
                .SingleOrDefault(p => p.Name.Equals(streamedResult.ParameterName, StringComparison.OrdinalIgnoreCase));
            if (methodParameter == null)
            {
                string errorMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    CmdletizationResources.CimJob_InvalidOutputParameterName,
                    this.MethodSubject,
                    this.MethodName,
                    streamedResult.ParameterName);

                throw CimJobException.CreateWithFullControl(
                    this.JobContext,
                    errorMessage,
                    "CimJob_InvalidOutputParameterName",
                    ErrorCategory.MetadataError);
            }

            var array = LanguagePrimitives.GetEnumerable(streamedResult.ItemValue);
            if (array != null)
            {
                foreach (var element in array)
                {
                    this.WriteObject(element, methodParameter);
                }
            }
            else
            {
                this.WriteObject(streamedResult.ItemValue, methodParameter);
            }
        }

        private void WriteObject(object cmdletOutput, MethodParameter methodParameter)
        {
            Dbg.Assert(methodParameter != null, "Caller should verify that methodParameter != null");
            if ((cmdletOutput != null) && (!string.IsNullOrEmpty(methodParameter.ParameterTypeName)))
            {
                PSObject pso = PSObject.AsPSObject(cmdletOutput);
                if (!pso.TypeNames.Contains(methodParameter.ParameterTypeName, StringComparer.OrdinalIgnoreCase))
                {
                    pso.TypeNames.Insert(0, methodParameter.ParameterTypeName);
                }
            }

            this.WriteObject(cmdletOutput);
        }

        public override void OnNext(CimMethodResultBase item)
        {
            this.ExceptionSafeWrapper(
                    delegate
                    {
                        var methodResult = item as CimMethodResult;
                        if (methodResult != null)
                        {
                            this.OnNext(methodResult);
                            return;
                        }

                        var streamedResult = item as CimMethodStreamedResult;
                        if (streamedResult != null)
                        {
                            this.OnNext(streamedResult);
                            return;
                        }

                        Dbg.Assert(false, "CimMethodResultBase has to be either a CimMethodResult or CimMethodStreamedResult");
                    });
        }

        #endregion
    }
}
