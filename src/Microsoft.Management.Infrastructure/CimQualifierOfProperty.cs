/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Collections;
using Microsoft.Management.Infrastructure.Options.Internal;

namespace Microsoft.Management.Infrastructure.Internal.Data
{
    internal sealed class CimQualifierOfProperty : CimQualifier
    {
        private readonly Native.ClassHandle classHandle;
        private readonly int index;
        private readonly string propertyName;

        internal CimQualifierOfProperty(Native.ClassHandle classHandle, string propertyName, int index)
        {
            this.classHandle = classHandle;
            this.index = index;
            this.propertyName = propertyName;
        }

        public override string Name
        {
            get
            {
                string name;
                Native.MiResult result = Native.ClassMethods.GetPropertyQualifierElementAt_GetName(
                    this.classHandle,
                    this.index,
                    this.propertyName,
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
                Native.MiResult result = Native.ClassMethods.GetPropertyQualifierElementAt_GetValue(
                    this.classHandle,
                    this.index,
                    this.propertyName,
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
                Native.MiResult result = Native.ClassMethods.GetPropertyQualifierElementAt_GetType(
                    this.classHandle,
                    this.index,
                    this.propertyName,
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
                Native.MiResult result = Native.ClassMethods.GetPropertyQualifierElementAt_GetFlags(
                    this.classHandle,
                    this.index,
                    this.propertyName,
                    out flags);
                CimException.ThrowIfMiResultFailure(result);
                return flags.ToCimFlags();
            }
        }
    }
}