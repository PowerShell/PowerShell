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
    internal sealed class CimMethodQualifierDeclarationOfMethod : CimQualifier
    {
        private readonly Native.ClassHandle classHandle;
        private readonly int qualifierIndex;
        private readonly int methodIndex;

        internal CimMethodQualifierDeclarationOfMethod(Native.ClassHandle classHandle, int methodIndex, int qualifierIndex)
        {
            this.classHandle = classHandle;
            this.qualifierIndex = qualifierIndex;
            this.methodIndex = methodIndex;
        }

        public override string Name
        {
            get
            {
                string name;
                Native.MiResult result = Native.ClassMethods.GetMethodQualifierElementAt_GetName(
                    this.classHandle,
                    this.methodIndex,
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
                Native.MiResult result = Native.ClassMethods.GetMethodQualifierElementAt_GetValue(
                    this.classHandle,
                    this.methodIndex,
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
                Native.MiResult result = Native.ClassMethods.GetMethodQualifierElementAt_GetType(
                    this.classHandle,
                    this.methodIndex,
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
                Native.MiResult result = Native.ClassMethods.GetMethodQualifierElementAt_GetFlags(
                    this.classHandle,
                    this.methodIndex,
                    this.qualifierIndex,
                    out flags);
                CimException.ThrowIfMiResultFailure(result);
                return flags.ToCimFlags();
            }
        }
    }
}