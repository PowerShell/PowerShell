/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Collections;
using Microsoft.Management.Infrastructure.Generic;
using Microsoft.Management.Infrastructure.Options.Internal;

namespace Microsoft.Management.Infrastructure.Internal.Data
{
    internal sealed class CimQualifierOfMethodParameter : CimQualifier
    {
        private readonly Native.ClassHandle classHandle;
        private readonly int qualifierIndex;
        private readonly int parameterName;
        private readonly int methodIndex;

        internal CimQualifierOfMethodParameter(Native.ClassHandle classHandle, int methodIndex, int parameterName, int index)
        {
            this.classHandle = classHandle;
            this.qualifierIndex = index;
            this.parameterName = parameterName;
            this.methodIndex = methodIndex;
        }

        public override string Name
        {
            get
            {
                string name;
                Native.MiResult result = Native.ClassMethods.GetMethodParameterGetQualifierElementAt_GetName(
                    this.classHandle,
                    this.methodIndex,
                    this.parameterName,
                    this.qualifierIndex, 
                    out name);
                CimException.ThrowIfMiResultFailure(result);
                return name;
            }
        }

        public override object Value
        {
            get
            {
                object value;
                Native.MiResult result = Native.ClassMethods.GetMethodParameterGetQualifierElementAt_GetValue(
                    this.classHandle,
                    this.methodIndex,
                    this.parameterName,
                    this.qualifierIndex,
                    out value);
                CimException.ThrowIfMiResultFailure(result);
                return CimInstance.ConvertFromNativeLayer(value);
            }
        }

        public override CimType CimType
        {
            get
            {
                Native.MiType type;
                Native.MiResult result = Native.ClassMethods.GetMethodParameterGetQualifierElementAt_GetType(
                    this.classHandle,
                    this.methodIndex,
                    this.parameterName,
                    this.qualifierIndex,
                    out type);
                CimException.ThrowIfMiResultFailure(result);
                return type.ToCimType();
            }
        }

        public override CimFlags Flags
        {
            get
            {
                Native.MiFlags flags;
                Native.MiResult result = Native.ClassMethods.GetMethodParameterGetQualifierElementAt_GetFlags(
                    this.classHandle,
                    this.methodIndex,
                    this.parameterName,
                    this.qualifierIndex,
                    out flags);
                CimException.ThrowIfMiResultFailure(result);
                return flags.ToCimFlags();
            }
        }
    }
}