// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Management.Infrastructure;
using Microsoft.PowerShell.Cim;

namespace Microsoft.PowerShell.Cmdletization.Cim
{
    /// <summary>
    /// Job wrapping invocation of a CreateInstance or ModifyInstance intrinsic CIM method.
    /// </summary>
    internal abstract class PropertySettingJob<T> : MethodInvocationJobBase<T>
    {
        internal PropertySettingJob(CimJobContext jobContext, bool passThru, CimInstance objectToModify, MethodInvocationInfo methodInvocationInfo)
                : base(
                    jobContext,
                    passThru,
                    objectToModify.ToString(),
                    methodInvocationInfo)
        {
        }

        internal void ModifyLocalCimInstance(CimInstance cimInstance)
        {
            foreach (MethodParameter methodParameter in this.GetMethodInputParameters())
            {
                CimValueConverter.AssertIntrinsicCimType(methodParameter.ParameterType);
                CimProperty propertyBeingModified = cimInstance.CimInstanceProperties[methodParameter.Name];
                if (propertyBeingModified != null)
                {
                    propertyBeingModified.Value = methodParameter.Value;
                }
                else
                {
                    CimProperty propertyBeingAdded = CimProperty.Create(
                        methodParameter.Name,
                        methodParameter.Value,
                        CimValueConverter.GetCimTypeEnum(methodParameter.ParameterType),
                        CimFlags.None);
                    cimInstance.CimInstanceProperties.Add(propertyBeingAdded);
                }
            }
        }
    }
}
