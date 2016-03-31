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
    internal sealed class CimMethodParameterDeclarationOfMethod : CimMethodParameterDeclaration
    {
        private readonly Native.ClassHandle classHandle;
        private readonly int index;
        private readonly int parameterName;

        internal CimMethodParameterDeclarationOfMethod(Native.ClassHandle classHandle, int index, int name)
        {
            this.classHandle = classHandle;
            this.index = index;
            this.parameterName = name;
        }

        public override string Name
        {
            get
            {
                string name;
                Native.MiResult result = Native.ClassMethods.GetMethodAt_GetName(
                    this.classHandle,
                    this.index,
                    this.parameterName,
                    out name);
                CimException.ThrowIfMiResultFailure(result);
                return name;
            }
        }

        public override CimType CimType
        {
            get
            {
                Native.MiType type;
                Native.MiResult result = Native.ClassMethods.GetMethodAt_GetType(
                    this.classHandle,
                    this.index,
                    this.parameterName,
                    out type);
                CimException.ThrowIfMiResultFailure(result);
                return type.ToCimType();
            }
        }

        public override CimReadOnlyKeyedCollection<CimQualifier> Qualifiers 
        {
            get
            {
                return new CimMethodParameterQualifierCollection(classHandle, this.index, this.parameterName);
            }
        }

        public override string ReferenceClassName
        {
            get
            {
                string referenceClass;
                Native.MiResult result = Native.ClassMethods.GetMethodAt_GetReferenceClass(
                    this.classHandle,
                    this.index,
                    this.parameterName,
                    out referenceClass);
                CimException.ThrowIfMiResultFailure(result);
                return referenceClass;
            }
        }
    }
}