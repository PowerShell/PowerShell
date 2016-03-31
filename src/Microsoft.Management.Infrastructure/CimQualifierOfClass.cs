/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Collections;
using Microsoft.Management.Infrastructure.Options.Internal;

namespace Microsoft.Management.Infrastructure.Internal.Data
{
    internal sealed class CimQualifierOfClass : CimQualifier
    {
        private readonly Native.ClassHandle classHandle;
        private readonly int index;

        internal CimQualifierOfClass(Native.ClassHandle classHandle, int index)
        {
            this.classHandle = classHandle;
            this.index = index;
        }

        public override string Name
        {
            get
            {
                string name;
                Native.MiResult result = Native.ClassMethods.GetQualifierElementAt_GetName(
                    this.classHandle,
                    this.index,
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
                Native.MiResult result = Native.ClassMethods.GetQualifierElementAt_GetValue(
                    this.classHandle,
                    this.index,
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
                Native.MiResult result = Native.ClassMethods.GetQualifierElementAt_GetType(
                    this.classHandle,
                    this.index,
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
                Native.MiResult result = Native.ClassMethods.GetQualifierElementAt_GetFlags(
                    this.classHandle,
                    this.index,
                    out flags);
                CimException.ThrowIfMiResultFailure(result);
                return flags.ToCimFlags();
            }
        }
    }
}